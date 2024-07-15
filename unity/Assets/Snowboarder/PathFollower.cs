using UnityEngine;
using UnityEngine.Splines;

public class PathFollower : MonoBehaviour
{
    public SplineContainer Spline;
    private int m_segment = 0;

    SnowboardPhysics m_physics;
    Snowboarder m_snowboarder;


    public void Reset()
    {
        m_segment = 0;
        Spline.Evaluate(m_segment, 0, out var pos, out var tangent, out var up);
        m_physics.TeleportTo(new Orientation(pos, Quaternion.LookRotation(tangent, up)));
    }

    void Awake()
    {
        m_physics = transform.Find("Physics").GetComponent<SnowboardPhysics>();
        m_snowboarder = GetComponent<Snowboarder>();
    }

    void Update()
    {
        var segment = Spline.Spline[m_segment];
        Spline.Spline.GetCurveLength(m_segment);

        //var orientation = Quats.AngleBetween(m_physics.RiderRotation, Quaternion.LookRotation())
    }
}