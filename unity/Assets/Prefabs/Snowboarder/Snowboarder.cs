using UnityEngine;

public enum RiderState
{
    InAir,
    Grounded,
    Grinding,
    Crashed,
}

public class Snowboarder : MonoBehaviour
{
    SnowboardPhysics m_physics;
    Transform m_rider;
    Vector3 m_riderOffset;

    Transform m_camera;
    Vector3 m_cameraOffset;

    RailInfluence m_railInfluence;

    int m_nextRespawn;
    GameObject[] m_respawns;

    bool m_isGrounded;

    float m_lastTurnStrength= 0;

    //////// Unity messages ////////

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();

        m_rider = transform.Find("Rider");
        m_riderOffset = m_rider.localPosition;
        m_railInfluence = m_rider.Find("RailInfluence").GetComponent<RailInfluence>();

        m_camera = transform.Find("OverheadCam");
        m_cameraOffset = m_camera.localPosition;
    }

    void Start()
    {
        m_respawns = GameObject.FindGameObjectsWithTag("Respawn");
    }

    void Update()
    {
        bool wasGrouned = m_isGrounded;
        m_isGrounded = m_physics.IsGrounded;

        m_rider.position = m_physics.transform.position + m_riderOffset;

        const float c_maxLeanDegrees = 10;
        m_lastTurnStrength = Mathf.MoveTowards(m_lastTurnStrength, -m_physics.TurnStrength, c_maxLeanDegrees * Time.deltaTime);

        m_rider.rotation = m_physics.RiderRotation * Quaternion.AngleAxis(m_lastTurnStrength * c_maxLeanDegrees, Vector3.forward);

        // TODO: this better
        m_camera.SetPositionAndRotation(
            Vector3.Lerp(m_camera.position, m_physics.transform.position + m_physics.TravelRotation * m_cameraOffset, 3.0f * Time.deltaTime),
            Quaternion.Lerp(m_camera.rotation, m_physics.TravelRotation, 3.0f * Time.deltaTime) // todo: clamp the amount
        );

        if (Input.GetKeyDown(KeyCode.R))
        {
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
            m_nextRespawn = (m_nextRespawn + 1) % m_respawns.Length;
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
                    textColor = new Color(1, 0.5f, 0.2f),
                }
            };
        }

        GUILayout.BeginVertical();

        GUILayout.Label($"Forward speed: {m_physics.ForwardSpeed:N1}", s_debugStyle);
        GUILayout.Label($"Is grounded: phys={m_isGrounded}", s_debugStyle);

        GUILayout.Label($"Rail nearby: {m_railInfluence.IsColliding}", s_debugStyle);

        GUILayout.Label($"Switch: {m_physics.ForwardSpeed < 0}", s_debugStyle);

        GUILayout.Label($"Turbo: {Input.GetKey(KeyCode.F)}", s_turboStyle);

        GUILayout.EndVertical();
    }

    void OnCollisionEnter(Collision collision)
    {
        //m_physics.Rigidbody.velocity -= collision.impulse;
    }
}
