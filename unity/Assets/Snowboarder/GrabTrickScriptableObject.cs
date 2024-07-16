using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Snowboarding/Grab Trick", order = 1)]
public class GrabTrickScriptableObject : ScriptableObject
{
    public AnimationClip Animation;
    public int PointsPerSecond = 100;
}

public struct GrabTrick
{
    public GrabTrickScriptableObject Trick { get; }

    public float StartTime { get; }

    public GrabTrick(GrabTrickScriptableObject trick)
    {
        Trick = trick;
        StartTime = Time.time;
    }
}
