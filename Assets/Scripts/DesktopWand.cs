using UnityEngine;

public class WandDesktopPosition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Position Offset (local to camera)")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0.3f, -0.25f, 0.5f);

    [Header("Rotation Offset (local to camera)")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0f, -5f, 0f);

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private float rotateSpeed = 15f;

    [Header("Bob / Sway")]
    [SerializeField] private bool enableIdleBob = true;
    [SerializeField] private float bobAmount = 0.002f;
    [SerializeField] private float bobSpeed = 2f;

    private bool isDesktopMode;

    void Start()
    {
        // Simple check: if no HMD is present, treat it as desktop preview
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        isDesktopMode = !IsHMDConnected();

        if (isDesktopMode)
        {
            // Unparent so we can smoothly follow
            transform.SetParent(null);
        }
    }

    void LateUpdate()
    {
        if (!isDesktopMode || cameraTransform == null) return;

        // Calculate target position in world space from camera-local offset
        Vector3 targetPos = cameraTransform.TransformPoint(positionOffset);

        // Optional idle bob
        if (enableIdleBob)
        {
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            targetPos += cameraTransform.up * bob;
        }

        // Calculate target rotation
        Quaternion targetRot = cameraTransform.rotation * Quaternion.Euler(rotationOffset);

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    private bool IsHMDConnected()
    {
        var xrDisplaySubsystems = new System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(xrDisplaySubsystems);

        foreach (var subsystem in xrDisplaySubsystems)
        {
            if (subsystem.running)
                return true;
        }

        return false;
    }
}