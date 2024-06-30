using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grindable : MonoBehaviour
{
    void OnDrawGizmos()
    {
        var grindMesh = GetComponent<MeshFilter>();
        for (int i = 1; i < grindMesh.mesh.vertexCount; ++i)
        {
            var a = grindMesh.mesh.vertices[i - 1];
            a = transform.position + transform.rotation * a;
            var b = grindMesh.mesh.vertices[i];
            b = transform.position + transform.rotation * b;

            Debug.DrawLine(
                a,
                b,
                Color.black,
                1);
            var hue = Mathf.Lerp(0.0f, 0.3f, i / (float)grindMesh.mesh.vertexCount);
            Debug.DrawLine(
                a + new Vector3(0, 0.05f, 0),
                b + new Vector3(0, 0.05f, 0),
                Color.HSVToRGB(hue, 0.7f, 0.7f),
                1);
        }
    }
}
