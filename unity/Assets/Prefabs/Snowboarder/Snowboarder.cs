using System;
using UnityEngine;

public class Snowboarder : MonoBehaviour
{
    SnowboardPhysics m_physics;
    Transform m_rider;
    Vector3 m_riderOffset;

    Transform m_camera;
    Vector3 m_cameraOffset;

    int m_nextRespawn;
    GameObject[] m_respawns;

    float m_lastTurnStrength= 0;

    //////// Unity messages ////////

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();

        m_rider = transform.Find("Rider");
        m_riderOffset = m_rider.localPosition;

        m_camera = transform.Find("OverheadCam");
        m_cameraOffset = m_camera.localPosition;
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

        m_rider.rotation = m_physics.RiderRotation * Quaternion.AngleAxis(m_lastTurnStrength * c_maxLeanDegrees, Vector3.forward);

        Quaternion cameraRotation;
        {
            // TODO: This should generally match the player's travel angle, but capped at a certain angle
            var projection = Quats.Project(m_physics.TravelRotation, Vector3.up);
            var forwardDir = (projection * -m_cameraOffset).normalized;
            //var forwardDir = (m_physics.TravelRotation * -m_cameraOffset).normalized;
            cameraRotation = Quaternion.LookRotation(forwardDir);
        }

        m_camera.SetPositionAndRotation(
            Vector3.Lerp(m_camera.position, m_physics.transform.position + cameraRotation * m_cameraOffset, 3.0f * Time.deltaTime),
            Quaternion.Lerp(m_camera.rotation, cameraRotation, 3.0f * Time.deltaTime) // lerp if already projecting above
        );

        if (Input.GetKeyDown(KeyCode.R))
        {
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            m_nextRespawn = (m_nextRespawn + 1) % m_respawns.Length;
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
        }
    }

    void OnTriggerEnter(Collider c)
    {
        Debug.Log(c);
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

        GUILayout.EndVertical();
    }
}
