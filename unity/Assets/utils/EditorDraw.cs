using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class EditorDraw
{
    // Must be claled from OnDrawGizmos or OnDrawGizmosSelected
    public static void DrawArrow(float width, Vector3 tail, Vector3 nose, Vector3 wingPlaneNormal, Color color)
    {
#if UNITY_EDITOR
        Handles.color = color;
        Handles.DrawAAPolyLine(width, tail, nose);

        var wingLength = (tail - nose).magnitude;
        var wingTangent = (tail - nose) / 3;

        var left = nose + Quaternion.AngleAxis(-30, wingPlaneNormal) * wingTangent;
        var right = nose + Quaternion.AngleAxis(30, wingPlaneNormal) * wingTangent;
        Handles.DrawAAPolyLine(width, left, nose, right);
#endif
    }
}

public static class DebugDraw
{
    public static void DrawArrow(Vector3 tail, Vector3 nose, Vector3 wingPlaneNormal, Color color)
    {
        Debug.DrawLine(tail, nose, color);

        var wingLength = (tail - nose).magnitude;
        var wingTangent = (tail - nose) / 3;

        var left = nose + Quaternion.AngleAxis(-30, wingPlaneNormal) * wingTangent;
        var right = nose + Quaternion.AngleAxis(30, wingPlaneNormal) * wingTangent;
        Debug.DrawLine(left, nose, color);
        Debug.DrawLine(nose, right, color);
    }
}