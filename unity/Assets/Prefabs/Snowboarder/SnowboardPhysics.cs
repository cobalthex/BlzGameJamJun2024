using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public readonly struct Orientation
{
    public readonly Vector3 position;
    public readonly Quaternion rotation;

    public Orientation(Rigidbody rigidbody)
    {
        position = rigidbody.position;
        rotation = rigidbody.rotation;
    }
    public Orientation(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
    }

    public void ApplyTo(Rigidbody rigidbody)
    {
        rigidbody.Move(position, rotation);
    }
}

public class SnowboardPhysics : MonoBehaviour
{
    // TODO: make these curves based on speed
    public float CrawlForce = 30.0f;
    public float BrakeForce = 60.0f;
    public float TurnForce = 160.0f;
    public float JumpForce = 10.0f;

    public float MaxJumpTimeSec = 0.25f;

    public Rigidbody Rigidbody { get; private set; }

    public float ForwardSpeed => Vector3.Dot(Rigidbody.velocity, Forward);

    public bool IsGrounded => m_colliders.Count > 0;

    public Quaternion Rotation { get; private set; }

    public Vector3 Forward => Rotation * Vector3.forward;

    private float m_jumpTimeLimit = 0;

    public void TeleportTo(Orientation orientation)
    {
        orientation.ApplyTo(Rigidbody);
        Rigidbody.velocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;
    }

    //////// Unity messages ////////

    // Start is called before the first frame update
    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Rotation = transform.rotation;
    }

    void Start()
    {
        //Rotation = transform.rotation;
        Rigidbody.rotation = Rotation;
    }

    // Update is called once per frame
    void Update()
    {
        bool isGrounded = IsGrounded;
        float forwardSpeed = ForwardSpeed;

        float speed = Rigidbody.velocity.magnitude;
        if (speed != 0)
        {
            Rotation = Quaternion.LookRotation(Rigidbody.velocity / speed);
        }

        if (isGrounded)
        {
            var turnInput = Input.GetAxis("Turn");
            float lowSpeedCap = Mathf.Min(10, speed) / 10f; // todo: improve this, add to rotation
            float turnStrength = 90.0f * turnInput * lowSpeedCap;
            if (turnInput != 0)
            {
                Quaternion turn = Rotation * Quaternion.AngleAxis(90 * turnInput, Vector3.up);
                Rigidbody.AddForce(turn * Vector3.forward * TurnForce);
            }

            var moveInput = Input.GetAxis("Move");
            if (moveInput > 0 || forwardSpeed <= 0) // todo: fix reverse crawl
            {
                Rigidbody.AddForce(Rotation * new Vector3(0, 0, CrawlForce * moveInput));
            }
            else if (moveInput < 0)
            {
                Rigidbody.AddForce(Rotation * new Vector3(0, 0, BrakeForce * Mathf.Clamp(ForwardSpeed, -1, 1) * moveInput));
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
            Rigidbody.AddForce(new Vector3(0, JumpForce, 0), ForceMode.Impulse); // todo: proper 'ollie' force
        }
    }

    void OnDrawGizmos()
    {
        var rotation = Rotation * Vector3.forward;

        EditorDraw.DrawArrow(
            10,
            transform.position + rotation / 2,
            transform.position + rotation * 2,
            Vector3.up,
            Color.magenta);

        var fwd = Rigidbody.velocity.normalized;
        EditorDraw.DrawArrow(
            10,
            transform.position + fwd / 2,
            transform.position + fwd * 2,
            Vector3.up,
            Color.cyan);
    }

    private HashSet<Collider> m_colliders = new HashSet<Collider>();
    void OnCollisionEnter(Collision collision)
    {
        bool any = false;
        for (var i = 0; i < collision.contactCount; ++i)
        {
            var contact = collision.GetContact(i);
            if (contact.point.z < transform.position.z) // todo: this should use normals too
            {
                any = true;
                break;
            }
        }

        if (any)
        {
            m_colliders.Add(collision.collider);
        }
    }
    void OnCollisionExit(Collision collision)
    {
        m_colliders.Remove(collision.collider);
    }
}
