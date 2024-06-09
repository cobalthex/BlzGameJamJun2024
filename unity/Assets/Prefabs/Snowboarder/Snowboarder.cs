using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

    bool isGroundedRaycast = false;


    //////// Unity messages ////////

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();

        var notPhysics = transform.Find("NotPhysics");

        m_rider = notPhysics.Find("Rider");
        m_riderOffset = m_rider.localPosition;

        m_camera = notPhysics.Find("OverheadCam");
        m_cameraOffset = m_camera.localPosition;
    }

    void Start()
    {
        m_respawns = GameObject.FindGameObjectsWithTag("Respawn");


    }

    void Update()
    {
        m_rider.position = m_physics.transform.position + m_riderOffset;
        m_rider.rotation = m_physics.Rotation;

        bool isGrounded = m_physics.IsGrounded;
        var rotation = m_physics.Rotation; // todo: follow velocity

        var forward = m_physics.Forward;
        var forwardRotation = Quaternion.LookRotation(forward);
        m_camera.SetPositionAndRotation(
            Vector3.Lerp(m_camera.position, m_physics.transform.position + rotation * m_cameraOffset, 2.0f * Time.deltaTime),
            Quaternion.Lerp(m_camera.rotation, forwardRotation, 1.5f * Time.deltaTime) // todo: clamp the amount
        );

        int layerMask = ~0 & ~LayerMask.NameToLayer("SnowboardPhysics");

        if (Input.GetKeyDown(KeyCode.R))
        {
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
            m_nextRespawn = (m_nextRespawn + 1) % m_respawns.Length;
        }
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
        GUILayout.Label($"Is grounded: phys={m_physics.IsGrounded} ray={isGroundedRaycast}", s_debugStyle);

        GUILayout.Label($"Switch: {m_physics.ForwardSpeed < 0}");

        GUILayout.EndVertical();
    }

    void OnCollisionEnter(Collision collision)
    {
        //m_physics.Rigidbody.velocity -= collision.impulse;
    }
}
