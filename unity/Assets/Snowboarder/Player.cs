using System;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    SnowboardPhysics m_physics;
    Snowboarder m_snowboarder;

    Transform m_camera;
    Vector3 m_cameraOffset;
    Quaternion m_cameraRotation;

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();
        m_snowboarder = GetComponent<Snowboarder>();

        m_camera = transform.Find("OverheadCam");
        m_cameraOffset = m_camera.localPosition;
        m_cameraRotation = m_camera.localRotation;
    }

    void Update()
    {
        // not raw?
        m_physics.InputMovePower = Input.GetAxisRaw("Move");
        m_physics.InputTurnPower = Input.GetAxisRaw("Turn");
        m_physics.InputFlipPower = Input.GetAxisRaw("Flip");

#if DEBUG
            if (Input.GetKey(KeyCode.F)) // FAST mode
            {
                m_physics.InputMovePower *= 3;
            }
#endif // DEBUG

        if (Input.GetButtonDown("Respawn"))
        {
            m_snowboarder.Respawn();
        }
        if (Input.GetButtonDown("NextSpawn"))
        {
            m_snowboarder.NextSpawn();
        }

        // camera
        {
            Quaternion cameraRotation;
            if (m_physics.State == RiderState.Grounded)
            {
                cameraRotation = m_physics.TravelRotation;
            }
            else
            {
                var decomposed = Quats.DecomposeTS(m_physics.TravelRotation, Vector3.up);
                //cameraRotation = decomposed.twist * m_cameraRotation;
                var dir = (-m_cameraOffset).normalized;
                cameraRotation = decomposed.twist * Quaternion.LookRotation(dir);
            }

            m_camera.SetPositionAndRotation(
                Vector3.Lerp(m_camera.position, m_physics.transform.position + cameraRotation * m_cameraOffset, 3.0f * Time.deltaTime),
                Quaternion.Lerp(m_camera.rotation, cameraRotation, 2.0f * Time.deltaTime) // lerp if already projecting above
            );
        }
    }


    static GUIStyle s_debugStyle;
    static GUIStyle s_turboStyle;
    static GUIStyle s_fpsStyle;
    static GUIStyle s_trickStyle;

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

            s_fpsStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    background = Texture2D.grayTexture,
                }
            };

            s_trickStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                wordWrap = true,
                normal =
                {
                    textColor = new Color(1.0f, 0.3f, 0),
                }
            };
        }

        GUILayout.BeginVertical();

        GUILayout.Label($"Euler: rider={m_physics.RiderRotation.eulerAngles} travel={m_physics.TravelRotation.eulerAngles}", s_debugStyle);
        GUILayout.Label($"Forward speed: {m_physics.ForwardSpeed:N1}", s_debugStyle);
        GUILayout.Label($"Rider state: {m_physics.State}, dist to ground: {m_physics.DetectedDistanceToGround}", s_debugStyle);
        GUILayout.Label($"Switch: {m_physics.IsRidingSwitch}", s_debugStyle); // TODO
        GUILayout.Label($"Turbo: {Input.GetKey(KeyCode.F)}", s_turboStyle);

        var rail = m_physics.Rail;
        if (rail != null)
        {
            GUILayout.Label($"Rail: {rail.m_position}", s_debugStyle);
        }

        if (m_snowboarder.GrabTrick.HasValue)
        {
            GUILayout.Label($"Grab: {m_snowboarder.GrabTrick.Value.Trick.name}", s_debugStyle);
        }

        // if (ActiveSlalom != null)
        // {
        //     GUILayout.Label($"Slalom: {ActiveSlalom}", s_debugStyle);
        // }

        GUILayout.EndVertical();

        if (m_snowboarder.TricksCombo.Count > 0)
        {
            GUI.Label(new Rect((Screen.width - 500) / 2, 30, 500, 200), string.Join(" + ", m_snowboarder.TricksCombo), s_trickStyle);
        }

        GUI.Label(new Rect(Screen.width - 90, 10, 80, 20), $"FPS: {1.0f / Time.deltaTime:N1}", s_fpsStyle);
    }
}