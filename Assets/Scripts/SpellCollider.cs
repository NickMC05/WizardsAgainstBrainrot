using UnityEngine;

public class SpellCollider : MonoBehaviour
{
    [SerializeField] private int colliderIndex = 1;

    [Header("Visuals")]
    [SerializeField] private Color inactiveColor = new Color(0.15f, 0.15f, 0.4f, 0.35f);
    [SerializeField] private Color activeColor   = new Color(0.3f, 0.7f, 1.0f, 0.9f);
    [SerializeField] private float emissionIntensity = 3f;

    private Material mat;
    private bool isSetUp;

    public int ColliderIndex => colliderIndex;

    /// <summary>Called by SpellColliderRig after AddComponent.</summary>
    public void Initialize(int index, Material sourceMaterial)
    {
        colliderIndex = index;
        EnsureSetup(sourceMaterial);
    }

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

    private void EnsureSetup(Material sourceMaterial)
    {
        if (isSetUp) return;

        // ------- collider swap -------
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null) DestroyImmediate(capsule);

        MeshCollider mesh = GetComponent<MeshCollider>();
        if (mesh != null) DestroyImmediate(mesh);

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null) sphere = gameObject.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 0.5f;

        // ------- material (clone of editor-assigned asset) -------
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && sourceMaterial != null)
        {
            mat = new Material(sourceMaterial); // instance clone
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        SetColor(inactiveColor);
        isSetUp = true;
    }

    public void Activate()  => SetColor(activeColor);
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