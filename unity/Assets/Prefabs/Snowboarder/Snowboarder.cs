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

    //////// Unity messages ////////

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();

        var notPhysics = transform.Find("NotPhysics");

        m_rider = notPhysics.Find("Rider");
        m_riderOffset = m_rider.localPosition + notPhysics.localPosition;

        m_camera = notPhysics.Find("OverheadCam");
        m_cameraOffset = m_camera.localPosition + notPhysics.localPosition;

        m_railInfluence = notPhysics.Find("RailInfluence").GetComponent<RailInfluence>();
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
        m_rider.rotation = m_physics.RiderRotation;

        m_camera.SetPositionAndRotation(
            Vector3.Lerp(m_camera.position, m_physics.transform.position + m_physics.TravelRotation * m_cameraOffset, 2.0f * Time.deltaTime),
            Quaternion.Lerp(m_camera.rotation, m_physics.TravelRotation, 2.0f * Time.deltaTime) // todo: clamp the amount
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
        }

        GUILayout.BeginVertical();

        GUILayout.Label($"Forward speed: {m_physics.ForwardSpeed:N1}", s_debugStyle);
        GUILayout.Label($"Is grounded: phys={m_isGrounded}", s_debugStyle);

        GUILayout.Label($"Rail nearby: {m_railInfluence.IsColliding}", s_debugStyle);

        GUILayout.Label($"Switch: {m_physics.ForwardSpeed < 0}", s_debugStyle);

        GUILayout.EndVertical();
    }

    void OnCollisionEnter(Collision collision)
    {
        //m_physics.Rigidbody.velocity -= collision.impulse;
    }
}
