using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grindable : MonoBehaviour
{
    void OnDrawGizmosSelected()
    {
        var grindMesh = GetComponent<MeshFilter>();
        for (int i = 1; i < 100 && i < grindMesh.mesh.vertexCount; ++i)
        {
            var a = grindMesh.mesh.vertices[i - 1];
            a = transform.TransformPoint(a);
            var b = grindMesh.mesh.vertices[i];
            b = transform.TransformPoint(b);

            Debug.DrawLine(
                a,
                b,
                Color.black,
                1);
            var cross = Vector3.Cross((b - a), Vector3.up);
            Debug.DrawLine(a - cross, a + cross, Color.black, 1);

            var hue = Mathf.Lerp(0.0f, 0.3f, i / (float)grindMesh.mesh.vertexCount);
            Debug.DrawLine(
                a + new Vector3(0, 0.05f, 0),
                b + new Vector3(0, 0.05f, 0),
                Color.HSVToRGB(hue, 0.7f, 0.7f),
                1);
        }
    }
}
