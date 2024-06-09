using System.Collections.Generic;
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
    // values tuned based on rigidbody mass of 50
    // TODO: make these curves based on speed
    public float CrawlForce = 20.0f;
    public float BrakeForce = 40.0f;
    public float GroundedTurnForce = 100.0f;
    public float InAirTurnDegPerSec = 90.0f;
    public float JumpForce = 6.0f;
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

    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.velocity = new Vector3(0, 0, 0.000001f);
        Rotation = transform.rotation;
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
            var turnInput = Input.GetAxisRaw("Turn"); // todo: not raw
            float lowSpeedCap = Mathf.Min(10, speed) / 10f; // todo: improve this, add to rotation
            float turnStrength = turnInput * lowSpeedCap * GroundedTurnForce;
            if (turnInput != 0)
            {
                Rigidbody.AddForce(Rotation * Vector3.right * turnStrength);
            }

            var moveInput = Input.GetAxisRaw("Move");
            if (Input.GetKey(KeyCode.F)) // fast mode
            {
                moveInput *= 3;
            }
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
        else
        {
            var turnInput = Input.GetAxisRaw("Turn"); // todo: not raw
            //Rotation *= Quaternion.AngleAxis(turnInput * InAirTurnDegPerSec * Time.deltaTime, Vector3.up);
            float turnStrength = turnInput * InAirTurnDegPerSec;
            Rigidbody.AddTorque(Vector3.up * turnStrength);

            // TODO: align up?
        }

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

        EditorDraw.DrawArrow(
            20,
            transform.position + ContactNormal,
            transform.position + ContactNormal,
            Vector3.back,
            Color.yellow);
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
