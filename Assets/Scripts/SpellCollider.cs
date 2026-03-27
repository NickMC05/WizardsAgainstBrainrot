using UnityEngine;

public class SpellCollider : MonoBehaviour
{
    [SerializeField] private int colliderIndex = 1;

    [Header("Visuals")]
    [SerializeField] private Color inactiveColor = new Color(0.15f, 0.15f, 0.4f, 0.35f);
    [SerializeField] private Color activeColor = new Color(0.3f, 0.7f, 1.0f, 0.9f);
    [SerializeField] private float emissionIntensity = 3f;

    private Material mat;
    private bool isSetUp;

    public int ColliderIndex => colliderIndex;

    /// <summary>Called by SpellColliderRig immediately after AddComponent.</summary>
    public void Initialize(int index)
    {
        colliderIndex = index;
        EnsureSetup();
    }

    void Awake() => EnsureSetup();

    void OnEnable()
    {
        if (SpellManager.Instance != null)
            SpellManager.Instance.OnCastingEnded += Deactivate;
    }

    void OnDisable()
    {
        if (SpellManager.Instance != null)
            SpellManager.Instance.OnCastingEnded -= Deactivate;
    }

    private void EnsureSetup()
    {
        if (isSetUp) return;

        // ------- collider swap -------
        // Cylinder primitives ship with a CapsuleCollider; replace it with a
        // SphereCollider so trigger detection is a clean circle around the disc.
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null) DestroyImmediate(capsule);

        // Also destroy any MeshCollider Unity might add
        MeshCollider mesh = GetComponent<MeshCollider>();
        if (mesh != null) DestroyImmediate(mesh);

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null) sphere = gameObject.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        // 0.5 matches the cylinder's default 1-unit diameter.
        // Because the transform has non-uniform scale Unity uses the largest
        // axis, which keeps the trigger volume comfortably bigger than the
        // paper-thin disc — intentional for VR comfort.
        sphere.radius = 0.5f;

        // ------- transparent material -------
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            mat = new Material(shader);

            // URP transparency
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            // Standard shader fallback
            mat.SetFloat("_Mode", 3f);

            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        SetColor(inactiveColor);
        isSetUp = true;
    }

    public void Activate() => SetColor(activeColor);
    public void Deactivate() => SetColor(inactiveColor);

    private void SetColor(Color color)
    {
        if (mat == null) return;
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b, 1f) * emissionIntensity);
    }
}