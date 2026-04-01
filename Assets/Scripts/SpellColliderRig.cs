using UnityEngine;

public class SpellColliderRig : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-find Camera.main at Start")]
    [SerializeField] private Transform headTransform;

    [Header("Materials (REQUIRED — create in Editor)")]
    [Tooltip("Transparent URP/Lit material for the circle nodes")]
    [SerializeField] private Material nodeMaterial;

    [Header("Pentagon Layout")]
    [SerializeField] private float distanceFromHead = 0.45f;
    [SerializeField] private float pentagonRadius   = 0.12f;
    [SerializeField] private float heightOffset     = -0.05f;
    [SerializeField] private float nodeSize         = 0.06f;

    [Tooltip("Degrees to rotate all five points. 0 = node 1 at top.")]
    [SerializeField] private float angleOffset = 0f;

    [Header("Background Image")]
    [Tooltip("Drag your PNG here (import as Sprite)")]
    [SerializeField] private Sprite pentagonSprite;
    [SerializeField] private float imageSize = 0.35f;
    [SerializeField] private float imageDepthOffset = 0.005f;

    [Header("Follow Behaviour")]
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

        if (nodeMaterial == null)
            Debug.LogError("[SpellColliderRig] nodeMaterial is not assigned! Circles will be pink/purple on Quest.");

        BuildBackgroundImage();
        BuildPentagon();
    }

    // ─────────────────────────────────────────────
    //  Background PNG via SpriteRenderer
    // ─────────────────────────────────────────────
    void BuildBackgroundImage()
    {
        if (pentagonSprite == null)
        {
            Debug.Log("[SpellColliderRig] No pentagonSprite assigned — skipping background.");
            return;
        }

        GameObject bg = new GameObject("PentagonBackground");
        bg.transform.SetParent(transform);
        bg.transform.localPosition = new Vector3(0f, 0f, imageDepthOffset);
        bg.transform.localRotation = Quaternion.identity;

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = pentagonSprite;
        sr.sortingOrder = -1;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;

        // Scale to desired world-space size
        float spriteWorldWidth = pentagonSprite.bounds.size.x;
        float scaleFactor = imageSize / spriteWorldWidth;
        bg.transform.localScale = Vector3.one * scaleFactor;
    }

    // ─────────────────────────────────────────────
    //  Five circle nodes
    // ─────────────────────────────────────────────
    void BuildPentagon()
    {
        nodes = new SpellCollider[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = $"SpellNode_{i + 1}";
            disc.transform.SetParent(transform);

            disc.transform.localScale = new Vector3(nodeSize, 0.001f, nodeSize);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            float angleDeg = 90f - (i * 72f) + angleOffset;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            disc.transform.localPosition = new Vector3(
                Mathf.Cos(angleRad) * pentagonRadius,
                Mathf.Sin(angleRad) * pentagonRadius,
                0f);

            SpellCollider sc = disc.AddComponent<SpellCollider>();
            sc.Initialize(i + 1, nodeMaterial);
            nodes[i] = sc;
        }
    }

    // ─────────────────────────────────────────────
    //  Follow head (yaw only)
    // ─────────────────────────────────────────────
    void Update()
    {
        if (headTransform == null) return;

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