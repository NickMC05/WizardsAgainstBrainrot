using UnityEngine;

/// <summary>
/// WASD + mouse movement for testing in the Unity Editor.
/// Attach to the XR Origin root. Automatically disables itself in builds.
/// </summary>
public class DesktopLocomotion : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float sprintMultiplier = 2f;

    [Header("Look (hold right mouse button)")]
    [SerializeField] float lookSensitivity = 2f;

    [Header("Vertical")]
    [SerializeField] KeyCode ascendKey = KeyCode.E;
    [SerializeField] KeyCode descendKey = KeyCode.Q;

    Transform cam;
    float pitch;
    float yaw;

    void Awake()
    {
#if !UNITY_EDITOR
        enabled = false;
        return;
#endif
    }

    void Start()
    {
        cam = Camera.main?.transform;
        if (cam == null)
        {
            Debug.LogWarning("[DesktopLocomotion] No Main Camera found. Disabling.");
            enabled = false;
            return;
        }

        yaw = cam.eulerAngles.y;
        pitch = cam.eulerAngles.x;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        if (!Input.GetMouseButton(1)) return;

        yaw += Input.GetAxis("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        cam.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        float h = 0f;
        float v = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(ascendKey)) y += 1f;
        if (Input.GetKey(descendKey)) y -= 1f;

        if (h == 0f && v == 0f && y == 0f) return;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        Vector3 move = (forward * v + right * h + Vector3.up * y).normalized * speed * Time.deltaTime;
        transform.position += move;
    }
}