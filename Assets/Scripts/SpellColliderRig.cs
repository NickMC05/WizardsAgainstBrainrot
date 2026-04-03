using UnityEngine;

public class SpellColliderRig : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The wand tip transform to follow when hidden")]
    [SerializeField] private Transform wandTip;

    [Header("Materials (REQUIRED — create in Editor)")]
    [Tooltip("Transparent URP/Lit material for the circle nodes")]
    [SerializeField] private Material nodeMaterial;

    [Header("Pentagon Layout")]
    [SerializeField] private float pentagonRadius = 0.12f;
    [SerializeField] private float nodeSize       = 0.06f;

    [Tooltip("Degrees to rotate all five points. 0 = node 1 at top.")]
    [SerializeField] private float angleOffset = 0f;

    [Header("Collider Depth")]
    [Tooltip("Half-height of each cylinder collider — extends this far on each side of the rig face")]
    [SerializeField] private float colliderDepth = 0.05f;

    [Header("Collider Visuals")]
    [Tooltip("Toggle ON to see the collider cylinders (debug). Toggle OFF to hide them.")]
    [SerializeField] private bool showColliderVisuals = false;

    [Header("Touch Particles")]
    [SerializeField] private Color touchParticleColor = new Color(0.3f, 0.7f, 1.0f, 1f);

    [Header("Background Image")]
    [Tooltip("Drag your PNG here (import as Sprite)")]
    [SerializeField] private Sprite pentagonSprite;
    [SerializeField] private float imageSize = 0.35f;
    [SerializeField] private float imageDepthOffset = 0.005f;

    [Header("Follow Behaviour")]
    [SerializeField] private float followSpeed = 12f;
    [Tooltip("Offset from wand tip in wand-local space")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    private SpellCollider[] nodes;
    private bool isShowing;
    private GameObject visualRoot;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        if (wandTip == null)
        {
            Debug.LogError("[SpellColliderRig] wandTip is not assigned.");
            enabled = false;
            return;
        }

        if (nodeMaterial == null)
            Debug.LogError("[SpellColliderRig] nodeMaterial is not assigned!");

        visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(transform);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale    = Vector3.one;

        BuildBackgroundImage();
        BuildPentagon();
        SetVisible(false);
    }

    void Update()
    {
        if (!isShowing)
            FollowWandTip();
    }

    /// <summary>
    /// Allows toggling showColliderVisuals in the Inspector at edit-time or play-time.
    /// </summary>
    void OnValidate()
    {
        if (nodes == null) return;
        foreach (var node in nodes)
        {
            if (node != null)
                node.SetVisualVisible(showColliderVisuals);
        }
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
        bg.transform.SetParent(visualRoot.transform);
        bg.transform.localPosition = new Vector3(0f, 0f, imageDepthOffset);
        bg.transform.localRotation = Quaternion.identity;

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite        = pentagonSprite;
        sr.sortingOrder  = -1;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows    = false;

        float spriteWorldWidth = pentagonSprite.bounds.size.x;
        float scaleFactor      = imageSize / spriteWorldWidth;
        bg.transform.localScale = Vector3.one * scaleFactor;
    }

    // ─────────────────────────────────────────────
    //  Five cylinder nodes
    // ─────────────────────────────────────────────

    void BuildPentagon()
    {
        nodes = new SpellCollider[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = $"SpellNode_{i + 1}";
            disc.transform.SetParent(visualRoot.transform);

            // nodeSize = diameter, colliderDepth = half-height
            disc.transform.localScale    = new Vector3(nodeSize, colliderDepth, nodeSize);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            float angleDeg = 90f - (i * 72f) + angleOffset;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            disc.transform.localPosition = new Vector3(
                Mathf.Cos(angleRad) * pentagonRadius,
                Mathf.Sin(angleRad) * pentagonRadius,
                0f);

            SpellCollider sc = disc.AddComponent<SpellCollider>();
            sc.Initialize(i + 1, nodeMaterial, showColliderVisuals, touchParticleColor);
            nodes[i] = sc;
        }
    }

    // ─────────────────────────────────────────────
    //  Follow wand tip while hidden
    // ─────────────────────────────────────────────

    private void FollowWandTip()
    {
        if (wandTip == null) return;

        Vector3 targetPos = wandTip.position + wandTip.rotation * positionOffset;

        float yaw = wandTip.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, yaw, 0f);

        float t = Time.deltaTime * followSpeed;
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }

    // ─────────────────────────────────────────────
    //  Public Show / Hide — called by SpellManager
    // ─────────────────────────────────────────────

    public void Show()
    {
        if (isShowing) return;
        isShowing = true;

        if (wandTip != null)
        {
            transform.position = wandTip.position + wandTip.rotation * positionOffset;
            float yaw = wandTip.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        if (nodes != null)
        {
            foreach (var node in nodes)
                node?.ResetNode();
        }

        SetVisible(true);
    }

    public void Hide()
    {
        if (!isShowing) return;
        isShowing = false;
        SetVisible(false);
    }

    public bool IsShowing => isShowing;

    private void SetVisible(bool visible)
    {
        visualRoot.SetActive(visible);
    }
}