using System.Collections;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    public void Reset()
    {
        this.enabled = true;
        for (int i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);
            var childBody = child.GetComponent<Rigidbody>();
            if (childBody != null)
            {
                childBody.isKinematic = true;
                child.gameObject.SetActive(true);
            }
        }
    }

    void Start()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);
            var childBody = child.GetComponent<Rigidbody>();
            if (childBody != null)
            {
                childBody.isKinematic = true;
            }
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.transform.parent == transform)
        {
            return;
        }

        this.enabled = false;
        for (int i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);
            var childBody = child.GetComponent<Rigidbody>();
            if (childBody != null)
            {
                childBody.isKinematic = false;
                StartCoroutine("FadeOutBroken", childBody);
            }
        }
    }

    IEnumerator FadeOutBroken(Rigidbody body)
    {
        var initialScale = body.transform.localScale;
        while (true)
        {
            float scale = 1 - 0.1f * Time.deltaTime;
            body.transform.localScale = Vector3.Scale(body.transform.localScale, new Vector3(scale, scale, scale));

            if (Vecs.Approx(body.transform.localScale, Vector3.zero, 0.05f))
            {
                break;
            }

            yield return null;
        }

        body.gameObject.SetActive(false);
        body.transform.localScale = initialScale;
    }
}