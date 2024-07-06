using UnityEngine;

public class SnowboardTrail : MonoBehaviour
{
    public SnowboardPhysics Physics { get; set; }

    // TODO: calculate from mesh
    float m_boardLength = 3.0f;
    float m_boardWidth = 0.6f;

    TrailRenderer m_trail;

    void Start()
    {
        m_trail = GetComponent<TrailRenderer>();
        m_trail.widthMultiplier = m_boardWidth;
    }

    void Update()
    {
        var sunDir = RenderSettings.sun.transform.forward;
        m_trail.material.SetVector("_SunDirection", sunDir);

        m_trail.emitting = Physics.State == RiderState.Grounded;

        var angle = Quats.AngleBetween(Physics.TravelRotation, Physics.RiderRotation, Physics.ContactNormal);

        float similarity = Mathf.Abs(2 * Mathf.Abs(angle - Mathf.PI) - Mathf.PI) / Mathf.PI; // 1 is parallel, 0 is orthagonal (or backwards?)
        m_trail.widthMultiplier = Mathf.Lerp(m_boardLength, m_boardWidth, similarity);
    }
}