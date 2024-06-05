using UnityEngine;

public class EditorArrow : MonoBehaviour
{
    public float Width = 3;
    public Color Color = Color.green;

    void OnDrawGizmos()
    {
        EditorDraw.DrawArrow(Width, transform.position, transform.position + (Width * transform.forward), transform.up, Color);
    }
}