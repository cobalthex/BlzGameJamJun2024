using System.Collections.Generic;
using UnityEngine;

public class RailInfluence : MonoBehaviour
{
    private HashSet<Collider> m_colliders = new HashSet<Collider>();

    public bool IsColliding => m_colliders.Count > 0;

    // TODO: influence cone

    private void OnTriggerEnter(Collider collider)
    {
        m_colliders.Add(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        m_colliders.Remove(collider);
    }
}
