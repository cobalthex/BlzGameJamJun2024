using UnityEngine;

public enum CameraPreference
{
    [HideInInspector]
    NotSet,
    Overhead,
    FirstPerson,
}

public class Snowboarder : MonoBehaviour
{
    public float CrawlSpeed = 3.0f;

    public CameraPreference CameraPreference
    {
        get => m_cameraPreference;
        set
        {
            if (m_cameraPreference == value)
            {
                return;
            }

            m_cameraPreference = value;
            foreach (var camera in transform.GetComponentsInChildren<Camera>())
            {
                camera.gameObject.SetActive(camera.name == $"{value}Cam");
            }
        }
    }
    [field: SerializeField]
    private CameraPreference m_cameraPreference = CameraPreference.NotSet;

    private Rigidbody m_rigidbody;

    public float ForwardSpeed
    {
        get
        {
            return Vector3.Dot(transform.forward, m_rigidbody.velocity);
        }
    }


    //////// Unity methods ////////


    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        CameraPreference = CameraPreference.Overhead;
    }

    void Update()
    {
        float curForwardSpeed = ForwardSpeed;

        var moveInput = Input.GetAxis("Move");
        m_rigidbody.AddRelativeForce(new Vector3(0, 0, CrawlSpeed * moveInput * m_rigidbody.mass));

        // TODO: keep upright
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 200, 50), $"Forward speed: {ForwardSpeed:N1}");
    }
}
