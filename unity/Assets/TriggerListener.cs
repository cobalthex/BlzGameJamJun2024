using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerListener : MonoBehaviour
{
    void OnTriggerEnter(Collider collider)
    {
        transform.parent?.SendMessage("OnTriggerEnter", collider, SendMessageOptions.DontRequireReceiver);
    }
}
