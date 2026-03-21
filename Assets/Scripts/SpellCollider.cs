using UnityEngine;

public class SpellCollider : MonoBehaviour
{
    [SerializeField] private int colliderIndex = 1;

    [Header("Visuals")]
    [SerializeField] private Color inactiveColor = new Color(0.12f, 0.12f, 0.35f);
    [SerializeField] private Color activeColor   = new Color(0.25f, 0.65f, 1.0f);
    [SerializeField] private float emissionIntensity = 2f;

    private Material mat;
    private bool isSetUp;

    public int ColliderIndex => colliderIndex;

    /// <summary>Called by SpellColliderRig right after AddComponent.</summary>
    public void Initialize(int index)
    {
        colliderIndex = index;
        EnsureSetup();
    }

    void Awake()  => EnsureSetup();

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

        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        rend.material = mat;

        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

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
        mat.SetColor("_EmissionColor", color * emissionIntensity);
    }
}