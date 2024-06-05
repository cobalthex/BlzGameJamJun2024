using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class HitTest : MonoBehaviour
{
    private HashSet<Collider> m_colliders = new HashSet<Collider>();

    public bool IsColliding => m_colliders.Count > 0;

    private void OnTriggerEnter(Collider collider)
    {
        float dot = Vector3.Dot(transform.up, (collider.transform.position - transform.position).normalized);
        // ignore any collisions from above the collider
        if (dot >= 0)
        {
            return;
        }

        m_colliders.Add(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        m_colliders.Remove(collider);
    }
}
