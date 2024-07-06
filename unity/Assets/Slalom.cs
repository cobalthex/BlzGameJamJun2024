using UnityEngine;

public class Slalom : MonoBehaviour
{
    public float CheckpointMissedGracePeriodSec = 5;

    void Awake()
    {
        for (int i = 0; i < transform.childCount - 1; ++i)
        {
            // var checkpoint = transform.GetChild(i).GetComponent<MeshRenderer>();
            // var propertyBlock = new MaterialPropertyBlock();
            // propertyBlock.SetVector("_Checkpoint", new Vector4(i, transform.childCount - 1, 0, 0));
            // checkpoint.SetPropertyBlock(propertyBlock, 0); // TODO: not working
        }
    }

    void OnDrawGizmos()
    {
        for (int i = 0; i < transform.childCount - 1; ++i)
        {
            var a = transform.GetChild(i);
            var b = transform.GetChild(i + 1);

            var hue = Mathf.Lerp(0.0f, 0.4f, i / (float)transform.childCount);
            var color = Color.HSVToRGB(hue, 0.8f, 1f);
            Debug.DrawLine(
                a.position + new Vector3(0, 0.05f, 0),
                b.position + new Vector3(0, 0.05f, 0),
                color,
                1);

            // // TODO: this should be c - a
            // if (i == 0)
            // {
            //     var dir = Quaternion.LookRotation(b.position - a.position);
            //     DebugDraw.DrawCircle(a.position, a.localScale.z / 2, dir, color);
            // }
            // else if (i == transform.childCount - 2)
            // {
            //     var dir = Quaternion.LookRotation(b.position - a.position);
            //     DebugDraw.DrawCircle(b.position, b.localScale.z / 2, dir, color);
            // }

            // if (i > 0)
            // {
            //     var prev = transform.GetChild(i - 1).position;
            //     var dir = Quaternion.LookRotation(b.position - prev);

            //     DebugDraw.DrawCircle(a.position, a.localScale.z / 2, dir, color);
            // }
        }
    }
}