using UnityEngine;

public class SpellColliderRig : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-find Camera.main at Start")]
    [SerializeField] private Transform headTransform;

    [Header("Pentagon Layout")]
    [SerializeField] private float distanceFromHead = 0.45f;
    [SerializeField] private float pentagonRadius = 0.12f;
    [SerializeField] private float heightOffset = -0.05f;
    [SerializeField] private float nodeSize = 0.06f;

    [Tooltip("Degrees to rotate all five points. 0 = node 1 at top.")]
    [SerializeField] private float angleOffset = 0f;

    [Header("Background Image")]
    [Tooltip("Drag your pentagonal PNG here (import as Texture2D with Alpha Is Transparency)")]
    [SerializeField] private Texture2D pentagonImage;
    [SerializeField] private float imageSize = 0.35f;
    [Tooltip("Push the image slightly behind the circles so it never z-fights")]
    [SerializeField] private float imageDepthOffset = 0.005f;

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

        BuildBackgroundImage();
        BuildPentagon();
    }

    // ─────────────────────────────────────────────
    //  Background PNG quad
    // ─────────────────────────────────────────────
    void BuildBackgroundImage()
    {
        if (pentagonImage == null)
        {
            Debug.Log("[SpellColliderRig] No pentagonImage assigned — skipping background quad.");
            return;
        }

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "PentagonBackground";
        quad.transform.SetParent(transform);

        // Position the quad slightly behind the circle plane
        quad.transform.localPosition = new Vector3(0f, 0f, imageDepthOffset);
        quad.transform.localRotation = Quaternion.identity;

        // Preserve the image's aspect ratio
        float aspect = (float)pentagonImage.width / pentagonImage.height;
        quad.transform.localScale = new Vector3(imageSize * aspect, imageSize, 1f);

        // The quad should never interfere with wand trigger detection
        Collider col = quad.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        // --- transparent unlit material ---
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        // Assign texture to both URP and legacy properties
        mat.SetTexture("_BaseMap", pentagonImage);
        mat.mainTexture = pentagonImage;

        // URP transparency
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_Color", Color.white);

        Renderer rend = quad.GetComponent<Renderer>();
        rend.material = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
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

            // Flatten the cylinder into a thin disc
            disc.transform.localScale = new Vector3(nodeSize, 0.001f, nodeSize);

            // Rotate so the flat face points toward the player (-Z local)
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Place around the pentagon.
            // Node 1 starts at the top (90°), each subsequent node is 72° clockwise.
            float angleDeg = 90f - (i * 72f) + angleOffset;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            disc.transform.localPosition = new Vector3(
                Mathf.Cos(angleRad) * pentagonRadius,
                Mathf.Sin(angleRad) * pentagonRadius,
                0f);

            SpellCollider sc = disc.AddComponent<SpellCollider>();
            sc.Initialize(i + 1);
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

        Vector3 forward = yawRot * Vector3.forward;
        Vector3 targetPos = headTransform.position
                          + forward * distanceFromHead
                          + Vector3.up * heightOffset;

        float t = Time.deltaTime * followSpeed;
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, yawRot, t);
    }
}