using UnityEngine;

public class Slalom : MonoBehaviour
{
    public float GateRadius = 10;

    void OnDrawGizmos()
    {
        for (int i = 0; i < transform.childCount - 1; ++i)
        {
            var a = transform.GetChild(i).position;
            var b = transform.GetChild(i + 1).position;

            var hue = Mathf.Lerp(0.0f, 0.4f, i / (float)transform.childCount);
            var color = Color.HSVToRGB(hue, 0.8f, 1f);
            Debug.DrawLine(
                a + new Vector3(0, 0.05f, 0),
                b + new Vector3(0, 0.05f, 0),
                color,
                1);

            // TODO: this should be c - a
            if (i == 0)
            {
                var dir = Quaternion.LookRotation(b - a);
                DebugDraw.DrawCircle(a, GateRadius, dir, color);
            }
            else if (i == transform.childCount - 2)
            {
                var dir = Quaternion.LookRotation(b - a);
                DebugDraw.DrawCircle(b, GateRadius, dir, color);
            }

            if (i > 0)
            {
                var prev = transform.GetChild(i - 1).position;
                var dir = Quaternion.LookRotation(b - prev);

                DebugDraw.DrawCircle(a, GateRadius, dir, color);
            }
        }
    }
}