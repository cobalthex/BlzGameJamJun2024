using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snowboarder2 : MonoBehaviour
{
    public float CrawlForce = 30.0f;
    public float BrakeForce = 60.0f;
    public float TurnForce = 160.0f;
    public float JumpForce = 10.0f;
    public float MaxJumpTimeSec = 0.25f;

    private Rigidbody m_rigidbody;
    public Quaternion Rotation {get; private set;}

    public bool IsGrounded => m_colliders.Count > 0;
    public float ForwardSpeed => Vector3.Dot(m_rigidbody.velocity, Rotation * Vector3.forward);

    // Start is called before the first frame update
    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private float m_jumpTimeLimit;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        bool isGrounded = IsGrounded;

        float speed = m_rigidbody.velocity.magnitude;
        float forwardSpeed = ForwardSpeed;

        {
            var euler = transform.eulerAngles;
            euler.z = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), 0.25f);
        }

        if (isGrounded)
        {
            if (speed != 0)
            {
                //Rotation = Quaternion.LookRotation(m_rigidbody.velocity / speed);
            }
            Rotation = transform.rotation;

            {
                var velDir = m_rigidbody.velocity.normalized;
                var velCmpAngle = Mathf.Acos(Vector3.Dot(velDir, Vector3.zero)) * Mathf.Rad2Deg;
                var velQuat = Quaternion.AngleAxis(velCmpAngle, velDir);
                m_rigidbody.rotation = Quaternion.Lerp(m_rigidbody.rotation, velQuat, 0.25f);

                DebugDraw.DrawArrow(transform.position, transform.position + velDir * 2, Vector3.up, Color.white);
            }

            var turnInput = Input.GetAxis("Turn");
            float lowSpeedCap = Mathf.Min(10, speed) / 10f; // todo: improve this, add to rotation
            float turnStrength = turnInput * lowSpeedCap * TurnForce;
            if (turnInput != 0)
            {
                //Quaternion turn = Rotation * Quaternion.AngleAxis(-90, Vector3.up);
                //m_rigidbody.AddForce(turn * Vector3.forward * turnStrength);
                //m_rigidbody.AddTorque(turn * Vector3.forward * turnStrength);
                //m_rigidbody.AddTorque(Vector3.up * turnStrength, ForceMode.VelocityChange);
                m_rigidbody.AddForceAtPosition(Rotation * Vector3.right * turnStrength, transform.position + Rotation * new Vector3(0, 0, -0.5f));
            }

            var moveInput = Input.GetAxis("Move");
            if (moveInput > 0 || forwardSpeed <= 0) // todo: fix reverse crawl
            {
                m_rigidbody.AddForce(Rotation * new Vector3(0, 0, CrawlForce * moveInput));
            }
            else if (moveInput < 0)
            {
                m_rigidbody.AddForce(Rotation * new Vector3(0, 0, BrakeForce * Mathf.Clamp(ForwardSpeed, -1, 1) * moveInput));
            }

            if (Input.GetButtonDown("Jump"))
            {
                m_jumpTimeLimit = Time.time + MaxJumpTimeSec; // rational time?
            }
        }

        // todo: hold force
        if (Input.GetButton("Jump") &&
            Time.time < m_jumpTimeLimit)
        {
            m_rigidbody.AddForce(new Vector3(0, JumpForce, 0), ForceMode.Impulse); // todo: proper 'ollie' force
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

        GUILayout.Label($"Forward speed: {ForwardSpeed:N1}", s_debugStyle);
        GUILayout.Label($"Is grounded: {IsGrounded} norm:{ContactNormal}", s_debugStyle);

        // GUILayout.Label($"Switch: {ForwardSpeed < 0}");

        GUILayout.EndVertical();
    }

    public Vector3 ContactNormal { get; private set; } = Vector3.up;

    private readonly float c_maxSlopeAngleCos = Mathf.Cos(Mathf.Deg2Rad * 90);

    private HashSet<Collider> m_colliders = new HashSet<Collider>();
    void OnCollisionEnter(Collision collision)
    {
        var sumNormals = new Vector3();
        bool any = false;
        for (var i = 0; i < collision.contactCount; ++i)
        {
            var contact = collision.GetContact(i);
            sumNormals += contact.normal;

            var dot = Vector3.Dot(contact.normal, Vector3.up);
            if (dot > c_maxSlopeAngleCos)
            {
                any = true;
            }
        }

        if (any)
        {
            m_colliders.Add(collision.collider);
        }

        ContactNormal = sumNormals.normalized;
    }
    void OnCollisionExit(Collision collision)
    {
        m_colliders.Remove(collision.collider);
    }
}
