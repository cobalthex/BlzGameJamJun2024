using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Snowboarder : MonoBehaviour
{
    SnowboardPhysics m_physics;
    Transform m_notPhysics;
    Transform m_camera;

    int m_nextRespawn;
    GameObject[] m_respawns;

    bool isGroundedRaycast = false;

    Vector3 m_visualOffset;

    //////// Unity messages ////////

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();
        m_notPhysics = transform.Find("NotPhysics");
        m_visualOffset = m_notPhysics.localPosition;
        m_camera = m_notPhysics.Find("OverheadCam");
    }

    void Start()
    {
        m_respawns = GameObject.FindGameObjectsWithTag("Respawn");


    }

    void Update()
    {
        //m_notPhysics.position = m_physics.transform.position + m_visualOffset;

        var forward = m_physics.Forward;
        var forwardRotation = Quaternion.LookRotation(forward);
        m_notPhysics.SetPositionAndRotation(
            m_physics.transform.position - new Vector3(0, 0.5f, 0), //Vector3.Lerp(m_notPhysics.position, m_physics.position + forward * -8 + new Vector3(0, 3, 0), 1.0f * Time.deltaTime),
            Quaternion.Lerp(m_notPhysics.rotation, forwardRotation, 1.5f * Time.deltaTime) // todo: clamp the amount
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

        GUI.Label(new Rect(20, 20, 150, 20), $"Forward speed: {m_physics.ForwardSpeed:N1}", s_debugStyle);
        GUI.Label(new Rect(20, 40, 150, 20), $"Is grounded: phys={m_physics.IsGrounded} ray={isGroundedRaycast}", s_debugStyle);

        float roll = transform.localEulerAngles.z;
        GUI.Label(new Rect(20, 60, 150, 20), $"Roll: {roll}", s_debugStyle);
    }
}
