using System.Collections.Generic;
using UnityEngine;

public enum RiderState
{
    InAir,
    Grounded,
    Grinding,
    Crashed,
}

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

public class RailRider
{
    public Mesh m_rail;
    public Transform m_railTransform;
    public Meshes.PositionOnMesh m_position;

    public float m_startTime;
}

public class SnowboardPhysics : MonoBehaviour
{
    // values tuned based on rigidbody mass of 50
    // TODO: make these curves based on speed
    public float CrawlForce = 40.0f;
    public float BrakeForce = 80.0f;
    public float GroundedTurnForce = 200.0f;
    public float InAirTurnDegPerSec = 180.0f;
    public float JumpForce = 400.0f;
    public float MaxLateJumpTimeSec = 0.25f; // How long you can jump after leaving a ledge, TODO: rename
    public float SteeringCorrectionDegresPerSec = 180.0f;
    public float InAirGroundDetectionDistance = 20.0f;

    public Rigidbody Rigidbody { get; private set; }

    private Vector3 m_bodyOffset;

    public float TurnStrength { get; private set; }

    public float ForwardSpeed => Vector3.Dot(Rigidbody.velocity, TravelRotation * Vector3.forward);

    public bool IsRidingSwitch { get; private set; }

    public RiderState State
    {
        get
        {
            if (Rail != null) return RiderState.Grinding;
            if (m_colliders.Count > 0) return RiderState.Grounded;
            return RiderState.InAir;
        }
    }

    public Quaternion RiderRotation { get; private set; }

    public Quaternion TravelRotation { get; private set; }

    public RailRider Rail { get; private set; }

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
        Rail = null;
    }

    //////// Unity messages ////////

    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.velocity = new Vector3(0, 0, 0.000001f);
        RiderRotation = TravelRotation = transform.rotation;

        m_bodyOffset = new Vector3(0, GetComponent<SphereCollider>().radius, 0); // TODO: calculate on collision?
        m_bodyOffset = transform.TransformVector(m_bodyOffset);
    }

    void Update()
    {
#if DEBUG
        if (Input.GetKeyDown(KeyCode.F1))
        {
            transform.GetComponent<MeshRenderer>().enabled ^= true;
        }
#endif

        float forwardSpeed = ForwardSpeed;

        float speed = Rigidbody.velocity.magnitude;
        Vector3 travelDir = Rigidbody.velocity / speed;

        if (Input.GetKeyDown(KeyCode.G))
        {
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.velocity = Vector3.zero;
        }

        var moveInput = Input.GetAxisRaw("Move");
        float turnInput = Input.GetAxisRaw("Turn"); // todo: not raw

        var state = State;
        if (state == RiderState.Grounded)
        {
            if (speed != 0)
            {
                // TODO: if rotation is > 180deg (twist?) flip it
                TravelRotation = Quaternion.LookRotation(travelDir);

                var desiredRotation = TravelRotation;
                if (IsRidingSwitch)
                {
                    // TODO: This breaks lean
                    desiredRotation *= Quaternion.AngleAxis(180, Vector3.up);
                }

                // TODO: rider should always try to 'roll correct' (can maybe sterp for that?)

                RiderRotation = Quaternion.RotateTowards(
                    RiderRotation,
                    desiredRotation,
                    SteeringCorrectionDegresPerSec * Time.deltaTime);
            }

            float lowSpeedCap = Mathf.Min(10, speed) / 10f; // todo: improve this, add to rotation
            float turnForce = turnInput * lowSpeedCap * GroundedTurnForce;
            if (turnInput != 0)
            {
                Rigidbody.AddForce(TravelRotation * Vector3.right * turnForce);
            }

            const float c_maxLeanSpeed = 30;
            TurnStrength = turnInput * (Mathf.Clamp(speed, 0, c_maxLeanSpeed) / c_maxLeanSpeed);

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
        else if (state == RiderState.InAir)
        {
            TurnStrength = turnInput;
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
        else if (state == RiderState.Grinding)
        {
            var deltaSpeed = Rigidbody.velocity.magnitude * Time.deltaTime;
            // TODO: todo: change of direction doesn't work properly, probably should calculate direction on-the-fly

            // jumping onto rail while traveling in negative dir does weird stuff

            Vector3 curSegmentStart;
            Vector3 curSegmentDirection;
            Vector3 curSegmentNormal;
            float relativePosition = 0;
            while (true)
            {
                curSegmentStart = Rail.m_railTransform.TransformPoint(Rail.m_rail.vertices[Rail.m_position.m_vertex]);
                var nextSegmentStart = Rail.m_railTransform.TransformPoint(Rail.m_rail.vertices[Rail.m_position.m_vertex + Rail.m_position.m_direction]);
                var segment = nextSegmentStart - curSegmentStart;
                var segmentLength = segment.magnitude;

                var relPos = Rail.m_position.m_relativePosition + deltaSpeed;

                curSegmentDirection = (segment / segmentLength);// * Rail.m_position.m_direction;
                curSegmentNormal = Vector3.Cross(curSegmentDirection, Rail.m_railTransform.right);

                if (relPos < segmentLength)
                {
                    relativePosition = Rail.m_position.m_relativePosition = relPos;
                    break;
                }

                Rail.m_position.m_vertex += Rail.m_position.m_direction;
                if (Rail.m_position.m_vertex <= 0 ||
                    Rail.m_position.m_vertex >= Rail.m_rail.vertexCount - 1 ||
                    speed < 1f)
                {
                    // TODO: if speed < 1, nudge left or right off the rail
                    // also TODO: support gravity rails (check if gravity in same direction as rail?)
                    Rail = null;
                    relativePosition = segmentLength;
                    break;
                }

                deltaSpeed -= segmentLength - Rail.m_position.m_relativePosition;
                relativePosition = Rail.m_position.m_relativePosition = 0;
            }

            // TODO: this should lerp here
            transform.position = curSegmentStart + curSegmentDirection * relativePosition + m_bodyOffset;

            TravelRotation = Quaternion.LookRotation(curSegmentDirection);
            Rigidbody.velocity = TravelRotation * Vector3.forward * Rigidbody.velocity.magnitude;

            RiderRotation *= Quaternion.AngleAxis(turnInput * InAirTurnDegPerSec * Time.deltaTime, Vector3.up); // use rail's up
            Debug.DrawRay(transform.position, curSegmentNormal, Color.green, 1);
        }

        // allow jumping even if slightly past jumping
        if (Input.GetButtonUp("Jump") &&
            ((State is RiderState.Grounded or RiderState.Grinding) || Time.time < m_jumpTimeLimit))
        {
            Rail = null;

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
        // bool wasGrounded = State == RiderState.Grounded;

        bool above = false;

        var sumNormals = new Vector3(); // collision.impulse.normalized may work here instead?
        bool any = false;
        for (var i = 0; i < collision.contactCount; ++i)
        {
            var contact = collision.GetContact(i);
            sumNormals += contact.normal;

            // TODO: for grind detection, this probably should be more 'up' independent
            // 'up' being transformed by rail's rotation
            above |= Vecs.IsInFrontOf(transform.position, contact.point, Vector3.up);

            var dot = Vector3.Dot(contact.normal, Vector3.up);
            if (dot > c_maxSlopeAngleCos)
            {
                any = true;
            }
        }

        // Rail detection
        if (Rail == null &&
            collision.gameObject.layer == LayerMask.NameToLayer("Grindable") &&
            above)
        {
            // TODO: verify that the rider is 'above' the rail

            var grindMesh = collision.transform.GetComponent<MeshFilter>();
            if (grindMesh != null)
            {
                Debug.Log($"Found grindable: {grindMesh} num verts:{grindMesh.mesh.vertexCount} size:{collision.collider.bounds.size}");

                var pos = Meshes.FindPositionOnMesh(grindMesh.mesh, collision.transform, transform.position, Rigidbody.velocity);
                var next = pos.m_vertex + pos.m_direction;
                if (next >= 0 &&
                    next < grindMesh.mesh.vertexCount)
                {
                    Rail = new RailRider
                    {
                        m_rail = grindMesh.mesh,
                        m_railTransform = grindMesh.transform,
                        m_position = pos,

                        m_startTime = Time.time,
                    };

                    // TODO: should probably calculate m_bodyOffset here

                    return;
                }
            }
        }

        if (any)
        {
            m_colliders.Add(collision.collider);
        }

        bool isGrounded = m_colliders.Count > 0;
        ContactNormal = sumNormals.normalized;

        // TODO: should this be used at all times?
        // TODO: maybe don't slowdown, but rotate the rider towards the travel direction

        // TODO: when landing sideways, maybe impart a slight directional force?
        // if (!wasGrounded && isGrounded)
        // {
        //     //if (Rigidbody.velocity != Vector3.zero)
        //     float landingAngle = Quats.AngleBetween(
        //         RiderRotation,
        //         TravelRotation,
        //         ContactNormal);

        //     if (landingAngle > Mathf.PI)
        //     {
        //         IsRidingSwitch ^= true;
        //     }

        //     // float similarity = Mathf.Abs(2 * Mathf.Abs(landingAngle - Mathf.PI) - Mathf.PI) / Mathf.PI; // 1 is parallel, 0 is orthagonal
        //     // simulatiry can be used to detect 'clean' vs 'dirty' landing

        //     // Debug.Log($"Landing angle:{landingAngle} similarity:{similarity}");

        // //     Rigidbody.velocity *= similarity;
        // //     Rigidbody.MoveRotation(RiderRotation); // TODO: not working
        // }

        // TODO: crash if impulse too high (base on 'hardness'?)

    }
    void OnCollisionExit(Collision collision)
    {
        m_colliders.Remove(collision.collider);
        bool isGrounded = m_colliders.Count > 0;

        if (!isGrounded && m_jumpTimeLimit != 0)
        {
            m_jumpTimeLimit = Time.time + MaxLateJumpTimeSec;
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        var boostpad = collider.GetComponent<BoostPad>();
        if (boostpad != null)
        {
            Debug.Log("Boost!");
            // scale magnitude based on direction relative to boostpad direction?
            Rigidbody.AddForce(TravelRotation * Vector3.forward * boostpad.BoostAcceleration, ForceMode.VelocityChange);
        }
    }
}
