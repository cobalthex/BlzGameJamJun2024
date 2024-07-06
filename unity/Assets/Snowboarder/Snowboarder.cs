using System;
using UnityEngine;

public class SlalomRace
{
    public Slalom m_slalom;
    public int m_nextCheckpointIndex;

    public float m_failTime;

    public override string ToString() => $"{m_slalom} gate:{m_nextCheckpointIndex}, fail time:{m_failTime}";

    public Transform GetNextCheckpoint() => GetCheckpoint(m_nextCheckpointIndex);

    public Transform GetCheckpoint(int index)
    {
        if (index < m_slalom.transform.childCount)
        {
            return m_slalom.transform.GetChild(index);
        }
        return null;
    }
}

public class Snowboarder : MonoBehaviour
{
    SnowboardPhysics m_physics;
    Transform m_rider;
    Vector3 m_riderOffset;

    Transform m_camera;
    Vector3 m_cameraOffset;
    Quaternion m_cameraRotation;

    Transform m_trail;
    Vector3 m_trailOffset;

    int m_nextRespawn;
    GameObject[] m_respawns;

    float m_lastTurnStrength = 0;

    public SlalomRace ActiveSlalom { get; private set; }

    //////// Unity messages ////////

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();

        m_rider = transform.Find("Rider");
        m_riderOffset = m_rider.localPosition;

        m_camera = transform.Find("OverheadCam");
        m_cameraOffset = m_camera.localPosition;
        m_cameraRotation = m_camera.localRotation;

        m_trail = transform.Find("Trail");
        if (m_trail != null)
        {
            m_trailOffset = m_trail.localPosition;
            m_trail.GetComponent<SnowboardTrail>().Physics = m_physics;
        }
    }

    void Start()
    {
        m_respawns = GameObject.FindGameObjectsWithTag("Respawn");
        Array.Sort(m_respawns, (a, b) => a.name.CompareTo(b.name));

        if (m_respawns.Length > m_nextRespawn)
        {
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
        }
    }

    void Update()
    {
        m_rider.position = m_physics.transform.position + m_riderOffset;

        const float c_maxLeanDegrees = 10;
        m_lastTurnStrength = Mathf.MoveTowards(m_lastTurnStrength, -m_physics.TurnStrength, c_maxLeanDegrees * Time.deltaTime);

        // Try to keep the rider up-right in roll direction
        // note: this isn't working while in-air
        var riderRotation = Quats.Sterp(m_physics.RiderRotation, Quaternion.identity, Vector3.right, 1 * Time.deltaTime);
        // lean when turning
        m_rider.rotation = riderRotation * Quaternion.AngleAxis(m_lastTurnStrength * c_maxLeanDegrees, Vector3.forward);

        m_trail.position = m_physics.transform.position + m_trailOffset;

        Quaternion cameraRotation;
        {
            var decomposed = Quats.DecomposeTS(m_physics.TravelRotation, Vector3.up);
            //cameraRotation = decomposed.twist * m_cameraRotation;
            var dir = (-m_cameraOffset).normalized;
            cameraRotation = decomposed.twist * Quaternion.LookRotation(dir);
        }

        m_camera.SetPositionAndRotation(
            Vector3.Lerp(m_camera.position, m_physics.transform.position + cameraRotation * m_cameraOffset, 3.0f * Time.deltaTime),
            Quaternion.Lerp(m_camera.rotation, cameraRotation, 3.0f * Time.deltaTime) // lerp if already projecting above
        );

        if (Input.GetKeyDown(KeyCode.R))
        {
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
            ActiveSlalom = null;
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            m_nextRespawn = (m_nextRespawn + 1) % m_respawns.Length;
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
        }

        /*
        if (ActiveSlalom != null)
        {
            var gate = ActiveSlalom.GetNextCheckpoint();
            if (gate == null)
            {
                ActiveSlalom = null;
            }
            else if (ActiveSlalom.m_failTime > 0 &&
                Time.time >= ActiveSlalom.m_failTime)
            {
                new Orientation(ActiveSlalom.GetCheckpoint(ActiveSlalom.m_nextCheckpointIndex - 1)).ApplyTo(m_physics.Rigidbody); // halve velocity?
                ActiveSlalom.m_failTime = 0;
            }
            else
            {
                var velocity = m_physics.Rigidbody.velocity * Time.deltaTime; // this should maybe be done in FixedUpdate
                var dir = (m_physics.Position + velocity) - gate.position;
                var projection = Vecs.ScalarProject(dir, gate.forward);
                Debug.DrawLine(m_physics.Position, m_physics.Position + dir * projection, Color.red, 2);
                if (projection > 0 && projection < velocity.sqrMagnitude) // todo: This not working
                {
                    var planarProjection = dir - (projection * gate.forward);
                    if (planarProjection.magnitude < gate.localScale.x / 2)
                    {
                        ++ActiveSlalom.m_nextCheckpointIndex;
                        ActiveSlalom.m_failTime = 0;
                    }
                    else if (ActiveSlalom.m_failTime == 0)
                    {
                        ActiveSlalom.m_failTime = Time.time + ActiveSlalom.m_slalom.CheckpointMissedGracePeriodSec;
                    }
                }
            }
        }
        */
    }

    void OnTriggerEnter(Collider collider)
    {
        if (ActiveSlalom == null)
        {
            var slalom = collider.transform.parent.GetComponent<Slalom>();
            if (slalom != null)
            {
                ActiveSlalom = new SlalomRace
                {
                    m_slalom = slalom,
                    m_nextCheckpointIndex = 1,
                    m_failTime = 0,
                };
            }
        }
    }

    static GUIStyle s_debugStyle;
    static GUIStyle s_turboStyle;

    void OnGUI()
    {
        if (s_debugStyle == null)
        {
            s_debugStyle = new GUIStyle
            {
                normal =
                {
                    background = Texture2D.grayTexture,
                }
            };

            s_turboStyle = new GUIStyle
            {
                normal =
                {
                    background = Texture2D.grayTexture,
                    textColor = new Color(1, 0.3f, 0.0f),
                }
            };
        }

        GUILayout.BeginVertical();

        GUILayout.Label($"Euler: rider={m_physics.RiderRotation.eulerAngles} travel={m_physics.TravelRotation.eulerAngles}", s_debugStyle);
        GUILayout.Label($"Forward speed: {m_physics.ForwardSpeed:N1}", s_debugStyle);
        GUILayout.Label($"Rider state: {m_physics.State}, can detect ground: {m_physics.CanDetectGroundWhileInAir}", s_debugStyle);
        GUILayout.Label($"Switch: {m_physics.IsRidingSwitch}", s_debugStyle); // TODO
        GUILayout.Label($"Turbo: {Input.GetKey(KeyCode.F)}", s_turboStyle);

        var rail = m_physics.Rail;
        if (rail != null)
        {
            GUILayout.Label($"Rail: {rail.m_position}", s_debugStyle);
        }

        if (ActiveSlalom != null)
        {
            GUILayout.Label($"Slalom: {ActiveSlalom}", s_debugStyle);
        }

        GUILayout.EndVertical();
    }
}
