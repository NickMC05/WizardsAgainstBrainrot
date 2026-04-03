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
    private MeshRenderer meshRenderer;
    private ParticleSystem touchParticles;

    public int ColliderIndex => colliderIndex;

    // ───────────────────── Initialisation ─────────────────────

    /// <summary>Called by SpellColliderRig after AddComponent.</summary>
    public void Initialize(int index, Material sourceMaterial)
    {
        colliderIndex = index;
        meshRenderer = GetComponent<MeshRenderer>();
        EnsureSetup(sourceMaterial);
    }

    /// <summary>Extended initialisation with visibility toggle and particle color.</summary>
    public void Initialize(int index, Material sourceMaterial, bool showVisual, Color particleColor)
    {
        colliderIndex = index;
        meshRenderer = GetComponent<MeshRenderer>();
        EnsureSetup(sourceMaterial);
        SetVisualVisible(showVisual);
        BuildTouchParticles(particleColor);
    }

    // ───────────────────── Events ─────────────────────

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

    // ───────────────────── Setup ─────────────────────

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
            mat = new Material(sourceMaterial);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        SetColor(inactiveColor);
        isSetUp = true;
    }

    // ───────────────────── Activate / Deactivate (original API) ─────────────────────

    public void Activate()
    {
        SetColor(activeColor);

        // Play touch particles on activation
        if (touchParticles != null)
            touchParticles.Play();
    }

    public void Deactivate() => SetColor(inactiveColor);

    /// <summary>Called by SpellColliderRig at the start of every cast.</summary>
    public void ResetNode()
    {
        Deactivate();
    }

    // ───────────────────── Visibility toggle ─────────────────────

    public void SetVisualVisible(bool visible)
    {
        if (meshRenderer != null)
            meshRenderer.enabled = visible;
    }

    // ───────────────────── Color helpers ─────────────────────

    private void SetColor(Color color)
    {
        if (mat == null) return;
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b, 1f) * emissionIntensity);
    }

    // ───────────────────── Touch Particles (programmatic) ─────────────────────

    private void BuildTouchParticles(Color color)
    {
        GameObject go = new GameObject("TouchFX");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // Counteract cylinder non-uniform scale so particles aren't squished
        Vector3 ps = transform.localScale;
        if (ps.x > 0f && ps.y > 0f && ps.z > 0f)
            go.transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f / ps.z);

        touchParticles = go.AddComponent<ParticleSystem>();
        touchParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ── Main ──
        var main = touchParticles.main;
        main.duration        = 0.4f;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.006f, 0.015f);
        main.startColor      = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode     = ParticleSystemScalingMode.Local;
        main.gravityModifier = -0.03f;
        main.maxParticles    = 20;

        // ── Emission ── single burst
        var emission = touchParticles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 10)
        });

        // ── Shape ── tiny sphere
        var shape = touchParticles.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.012f;

        // ── Colour over lifetime ── fade out
        var col = touchParticles.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 0.4f),
                new GradientColorKey(color * 0.4f, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.7f, 0.3f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        col.color = grad;

        // ── Size over lifetime ── shrink
        var sol = touchParticles.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

        // ── Renderer ──
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material   = CreateParticleMaterial(color);
    }

    private Material CreateParticleMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");

        if (shader == null)
        {
            Debug.LogWarning("[SpellCollider] No particle shader found.");
            return null;
        }

        Material mat = new Material(shader);

        if (shader.name.Contains("Universal"))
        {
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 1f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3000;
        }
        else if (shader.name.Contains("Standard"))
        {
            mat.SetColor("_Color", color);
            mat.SetFloat("_Mode", 1f);
        }
        else
        {
            mat.SetColor("_TintColor", new Color(color.r, color.g, color.b, 0.5f));
        }

        return mat;
    }
}