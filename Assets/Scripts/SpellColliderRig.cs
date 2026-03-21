using UnityEngine;

public class SpellColliderRig : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-find Camera.main at Start")]
    [SerializeField] private Transform headTransform;

    [Header("Pentagon Layout")]
    [SerializeField] private float distanceFromHead = 0.45f;
    [SerializeField] private float pentagonRadius   = 0.12f;
    [SerializeField] private float heightOffset      = -0.05f;
    [SerializeField] private float nodeSize          = 0.055f;

    [Header("Follow Behaviour")]
    [Tooltip("Higher = snappier tracking")]
    [SerializeField] private float followSpeed = 12f;

    private SpellCollider[] nodes;

    void Start()
    {
        if (headTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                headTransform = cam.transform;
            else
            {
                Debug.LogError("[SpellColliderRig] No head transform and no main camera found.");
                enabled = false;
                return;
            }
        }

        BuildPentagon();
    }

    void BuildPentagon()
    {
        nodes = new SpellCollider[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"SpellNode_{i + 1}";
            cube.transform.SetParent(transform);
            cube.transform.localScale = Vector3.one * nodeSize;

            // Vertex 1 sits at the top (90°), then clockwise at 72° intervals
            float angleDeg = 90f - (i * 72f);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            cube.transform.localPosition = new Vector3(
                Mathf.Cos(angleRad) * pentagonRadius,
                Mathf.Sin(angleRad) * pentagonRadius,
                0f);

            SpellCollider sc = cube.AddComponent<SpellCollider>();
            sc.Initialize(i + 1);
            nodes[i] = sc;
        }
    }

    void Update()
    {
        if (headTransform == null) return;

        // Derive a yaw-only rotation from the head
        float yaw = headTransform.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 forward   = yawRot * Vector3.forward;
        Vector3 targetPos = headTransform.position
                          + forward * distanceFromHead
                          + Vector3.up * heightOffset;

        float t = Time.deltaTime * followSpeed;
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, yawRot, t);
    }
}