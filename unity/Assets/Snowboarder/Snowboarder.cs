using System;
using System.Collections.Generic;
using UnityEngine;

public class SlalomRace
{
    public Slalom m_slalom;
    public int m_nextCheckpointIndex;

    public float m_failTime;

    public override string ToString() => $"{m_slalom} gate:{m_nextCheckpointIndex}, fail time:{m_failTime}";

    public Transform GetNextCheckpoint() => GetCheckpoint(m_nextCheckpointIndex);

    public Transform GetCheckpoint(int index)
    {
        if (index < m_slalom.transform.childCount)
        {
            return m_slalom.transform.GetChild(index);
        }
        return null;
    }
}

public class Snowboarder : MonoBehaviour
{
    public GrabTrickScriptableObject[] AvailableGrabTricks;

    SnowboardPhysics m_physics;
    Transform m_rider;
    Vector3 m_riderOffset;

    Animation m_animation;

    Transform m_trail;
    Vector3 m_trailOffset;

    int m_nextRespawn;
    GameObject[] m_respawns;

    float m_lastTurnStrength = 0;

    public SlalomRace ActiveSlalom { get; private set; }

    public List<string> TricksCombo { get; private set; } = new();

    public GrabTrick? GrabTrick
    {
        get => _grabTrick;
        set
        {
            if (value.HasValue && _grabTrick.HasValue &&
                value.Value.Trick == _grabTrick.Value.Trick)
            {
                return;
            }

            _grabTrick = value;
            if (_grabTrick != null)
            {
                m_animation.AddClip(_grabTrick.Value.Trick.Animation, "ActiveTrick");
                m_animation.CrossFade("ActiveTrick");
                TricksCombo.Add(_grabTrick.Value.Trick.name); // todo: this needs to wait for previous trick to finish
                m_nextGrabTrickTime = Time.time + _grabTrick.Value.Trick.Animation.length;
            }
        }
    }
    private GrabTrick? _grabTrick;
    private float m_nextGrabTrickTime = 0;

    public void Respawn()
    {
        m_physics.Rigidbody.isKinematic = false;
        m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
        ActiveSlalom = null;
        m_trail.GetComponent<TrailRenderer>().Clear();
    }

    public void NextSpawn()
    {
        m_nextRespawn = (m_nextRespawn + 1) % m_respawns.Length;
        Respawn();
    }

    public void Crash()
    {
        m_physics.Rigidbody.isKinematic = true;

    }

    //////// Unity messages ////////

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();

        m_animation = GameObject.FindGameObjectWithTag("RiderAnimation")?.GetComponent<Animation>();

        m_rider = transform.Find("Rider");
        m_riderOffset = m_rider.localPosition;

        m_trail = transform.Find("Trail");
        if (m_trail != null)
        {
            m_trailOffset = m_trail.localPosition;
            m_trail.GetComponent<SnowboardTrail>().Physics = m_physics;
        }
    }

    void Start()
    {
        m_respawns = GameObject.FindGameObjectsWithTag("Respawn");
        Array.Sort(m_respawns, (a, b) => a.name.CompareTo(b.name));

        if (m_respawns.Length > m_nextRespawn)
        {
            m_physics.TeleportTo(new Orientation(m_respawns[m_nextRespawn].transform));
        }
    }

    RiderState m_lastRiderState;
    void Update()
    {
        // physics
        {
            m_rider.position = m_physics.transform.position + m_riderOffset;

            const float c_maxLeanDegrees = 10;
            m_lastTurnStrength = Mathf.MoveTowards(m_lastTurnStrength, -m_physics.TurnStrength, c_maxLeanDegrees * Time.deltaTime);

            // Try to keep the rider up-right in roll direction
            // note: this isn't working while in-air
            var riderRotation = m_physics.RiderRotation;
            riderRotation = Quats.Sterp(riderRotation, Quaternion.identity, m_physics.TravelRotation * Vector3.right, 1 * Time.deltaTime);

            // lean when turning
            m_rider.rotation = riderRotation * Quaternion.AngleAxis(m_lastTurnStrength * c_maxLeanDegrees, Vector3.forward);
        }

        var riderState = m_physics.State;
        bool trickStateChanged = false;

        if (riderState == RiderState.InAir &&
            (!m_physics.DetectedDistanceToGround.HasValue ||
             m_physics.DetectedDistanceToGround > 1.25f)) // tdoo: don't hard code this
        {
            if (Input.GetButtonDown("Grab") &&
                AvailableGrabTricks.Length > 0 &&
                Time.time >= m_nextGrabTrickTime)
            {
                int grabIndex = UnityEngine.Random.Range(0, AvailableGrabTricks.Length - 1);
                GrabTrick = new GrabTrick(AvailableGrabTricks[grabIndex]); // TODO: random index
                trickStateChanged = true;
            }
            if (Input.GetButtonUp("Grab") &&
                GrabTrick.HasValue)
            {
                GrabTrick = null;
                trickStateChanged = true;
            }
        }

        if (riderState != m_lastRiderState ||
            trickStateChanged)
        {
            switch (riderState)
            {
                // TODO: don't hard-code these
                case RiderState.InAir:
                    m_animation.CrossFade("Flying");
                    break;

                case RiderState.Grounded:
                    m_animation.CrossFade("Boarding_F_L");
                    TricksCombo.Clear();
                    break;

                case RiderState.Grinding:
                    m_animation.CrossFade("Yeti_Board_Balance_L");
                    TricksCombo.Add("Grind"); // name of rail?
                    break;

                case RiderState.Crashed:
                    m_animation.CrossFade("Boarding_Stand");
                    TricksCombo.Clear();
                    break;
            }

            if (riderState != RiderState.InAir)
            {
                // could detect crash here
                GrabTrick = null;
                m_nextGrabTrickTime = 0;
            }

            m_lastRiderState = riderState;
        }

        m_trail.position = m_physics.transform.position + m_trailOffset;


        /* slalom
        if (ActiveSlalom != null)
        {
            var gate = ActiveSlalom.GetNextCheckpoint();
            if (gate == null)
            {
                ActiveSlalom = null;
            }
            else if (ActiveSlalom.m_failTime > 0 &&
                Time.time >= ActiveSlalom.m_failTime)
            {
                new Orientation(ActiveSlalom.GetCheckpoint(ActiveSlalom.m_nextCheckpointIndex - 1)).ApplyTo(m_physics.Rigidbody); // halve velocity?
                ActiveSlalom.m_failTime = 0;
            }
            else
            {
                var velocity = m_physics.Rigidbody.velocity * Time.deltaTime; // this should maybe be done in FixedUpdate
                var dir = (m_physics.Position + velocity) - gate.position;
                var projection = Vecs.ScalarProject(dir, gate.forward);
                Debug.DrawLine(m_physics.Position, m_physics.Position + dir * projection, Color.red, 2);
                if (projection > 0 && projection < velocity.sqrMagnitude) // todo: This not working
                {
                    var planarProjection = dir - (projection * gate.forward);
                    if (planarProjection.magnitude < gate.localScale.x / 2)
                    {
                        ++ActiveSlalom.m_nextCheckpointIndex;
                        ActiveSlalom.m_failTime = 0;
                    }
                    else if (ActiveSlalom.m_failTime == 0)
                    {
                        ActiveSlalom.m_failTime = Time.time + ActiveSlalom.m_slalom.CheckpointMissedGracePeriodSec;
                    }
                }
            }
        }
        */
    }

    // void OnTriggerEnter(Collider collider)
    // {
    //     if (ActiveSlalom == null)
    //     {
    //         var slalom = collider.transform.parent.GetComponent<Slalom>();
    //         if (slalom != null)
    //         {
    //             ActiveSlalom = new SlalomRace
    //             {
    //                 m_slalom = slalom,
    //                 m_nextCheckpointIndex = 1,
    //                 m_failTime = 0,
    //             };
    //         }
    //     }
    // }

    void OnCollisionEnter(Collision c)
    {
        Debug.Log("Boarder collision: " + c);
    }
}
