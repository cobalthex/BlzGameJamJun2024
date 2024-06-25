using System;
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
    public float CrawlForce = 40.0f;
    public float BrakeForce = 80.0f;
    public float GroundedTurnForce = 150.0f;
    public float InAirTurnDegPerSec = 180.0f;
    public float JumpForce = 400.0f;
    public float MaxJumpTimeSec = 0.25f; // How long you can jump after leaving a ledge, TODO: rename
    public float SteeringCorrectionDegresPerSec = 180.0f;
    public float InAirGroundDetectionDistance = 10.0f;

    public Rigidbody Rigidbody { get; private set; }

    public float TurnStrength { get; private set; }

    public float ForwardSpeed => Vector3.Dot(Rigidbody.velocity, TravelRotation * Vector3.forward);

    public bool IsRidingSwitch => Quaternion.Dot(RiderRotation, TravelRotation) < 0;

    public bool IsGrounded { get; private set; }

    public Quaternion RiderRotation { get; private set; }

    public Quaternion TravelRotation { get; private set; }

    /// <summary>
    /// Can detect the ground (while in air)? Not updated while grounded
    /// </summary>
    public bool CanDetectGroundWhileInAir { get; private set; } = true;
    public float InAirAlignToGroundDegreesPerSec = 45;

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
        RiderRotation = TravelRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
#if DEBUG
        if (Input.GetKeyDown(KeyCode.F1))
        {
            transform.GetComponent<MeshRenderer>().enabled ^= true;
        }
#endif

        bool isGrounded = IsGrounded;

        float forwardSpeed = ForwardSpeed;

        float speed = Rigidbody.velocity.magnitude;
        Vector3 travelDir = Rigidbody.velocity / speed;

        if (Input.GetKeyDown(KeyCode.G))
        {
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.velocity = Vector3.zero;
        }

        if (isGrounded)
        {
            if (speed != 0)
            {
                TravelRotation = Quaternion.LookRotation(travelDir);
                // TODO: this needs to rotate in shortest path
                RiderRotation = Quaternion.RotateTowards(RiderRotation, TravelRotation, SteeringCorrectionDegresPerSec * Time.deltaTime);
            }

            float turnInput = Input.GetAxisRaw("Turn"); // todo: not raw

            float lowSpeedCap = Mathf.Min(10, speed) / 10f; // todo: improve this, add to rotation
            float turnForce = turnInput * lowSpeedCap * GroundedTurnForce;
            if (turnInput != 0)
            {
                Rigidbody.AddForce(TravelRotation * Vector3.right * turnForce);
            }

            const float c_maxLeanSpeed = 30;
            TurnStrength = turnInput * (Mathf.Clamp(speed, 0, c_maxLeanSpeed) / c_maxLeanSpeed);

            var moveInput = Input.GetAxisRaw("Move");

#if DEBUG
            if (Input.GetKey(KeyCode.F)) // fast mode -- TODO: dev only
            {
                moveInput *= 3;
        }
#endif // DEBUG

            if (moveInput > 0 || forwardSpeed <= 0) // todo: fix reverse crawl
            {
                Rigidbody.AddForce(TravelRotation * new Vector3(0, 0, CrawlForce * moveInput));
            }
            else if (moveInput < 0)
            {
                Rigidbody.AddForce(TravelRotation * new Vector3(0, 0, BrakeForce * Mathf.Clamp(ForwardSpeed, -1, 1) * moveInput));
            }
        }
        else
        {
            var turnInput = Input.GetAxisRaw("Turn"); // todo: not raw

            TurnStrength = turnInput * 0.3f;

            RiderRotation *= Quaternion.AngleAxis(turnInput * InAirTurnDegPerSec * Time.deltaTime, Vector3.up);

            Vector3 alignDir = Vector3.up;

            int terrainMask = LayerMask.GetMask("Terrain");
            if (CanDetectGroundWhileInAir = Physics.Raycast(
                transform.position,
                Vector3.down,
                out var groundTest,
                InAirGroundDetectionDistance,
                terrainMask))
            {
                alignDir = groundTest.normal;
                Debug.DrawRay(transform.position, alignDir, Color.red, 0.5f);
            }

            var riderUp = RiderRotation * Vector3.up;

            var alignQuat = Quaternion.FromToRotation(riderUp, alignDir);

            // can get unstable at low speeds
            RiderRotation = Quaternion.RotateTowards(
                RiderRotation,
                alignQuat * RiderRotation,
                InAirAlignToGroundDegreesPerSec * Time.deltaTime);
        }

        // allow jumping even if slightly past jumping
        if (Input.GetButtonUp("Jump") &&
            (IsGrounded || Time.time < m_jumpTimeLimit))
        {
            // TODO: can occasionally double jump

            // jump vector mid way between ground normal and up?
            Rigidbody.AddForce(new Vector3(0, JumpForce, 0), ForceMode.Impulse);
            m_jumpTimeLimit = 0;
        }
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!Rigidbody)
        {
            return;
        }
#endif // UNITY_EDITOR

        var rotation = TravelRotation * Vector3.forward;

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
        bool wasGrounded = IsGrounded;

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

        bool isGrounded = m_colliders.Count > 0;

        // TODO: should this be used at all times?
        // TODO: maybe don't slowdown, but rotate the rider towards the travel direction

        // TODO: when landing sideways, maybe impart a slight directional force?
        // if (!wasGrounded && IsGrounded &&
        //     Rigidbody.velocity != Vector3.zero)
        // {
        //     // lots of math here
        //     var relQuat = Quaternion.Inverse(RiderRotation) * TravelRotation;
        //     float landingAngle = 2 * Mathf.Atan2(new Vector3(relQuat.x, relQuat.y, relQuat.z).magnitude, relQuat.w);
        //     float similarity = Math.Abs(2 * Mathf.Abs(landingAngle - Mathf.PI) - Mathf.PI) / Mathf.PI; // 1 is parallel, 0 is orthagonal

        //     Debug.Log($"Landing angle:{landingAngle} similarity:{similarity}");

        //     Rigidbody.velocity *= similarity;
        //     Rigidbody.MoveRotation(RiderRotation); // TODO: not working
        // }

        // TODO: crash if impulse too high (base on 'hardness'?)

        ContactNormal = sumNormals.normalized;
        IsGrounded = isGrounded;
    }
    void OnCollisionExit(Collision collision)
    {
        m_colliders.Remove(collision.collider);
        IsGrounded = m_colliders.Count > 0;

        if (!IsGrounded)
        {
            m_jumpTimeLimit = Time.time + MaxJumpTimeSec;
        }
    }
}
