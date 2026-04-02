using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class WandTipDetector : MonoBehaviour
{
    [SerializeField] private SpellManager spellManager;
    [SerializeField] private WandController wandController;

    [Header("Spell Indicator")]
    [Tooltip("Transparent URP/Lit material (create in Editor to avoid purple on Quest)")]
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private float indicatorSize = 0.04f;
    [SerializeField] private float emissionIntensity = 5f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseMin = 0.7f;
    [SerializeField] private float pulseMax = 1.3f;

    private SphereCollider sphereCol;
    private GameObject indicatorObj;
    private Material indicatorMatInst;
    private Color currentSpellColor;
    private bool indicatorVisible;

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = 0.02f;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        CreateIndicator();
    }

    void OnEnable()
    {
        if (spellManager != null)
        {
            spellManager.OnSpellLoaded += HandleSpellLoaded;
            spellManager.OnSpellUnloaded += HandleSpellUnloaded;
            spellManager.OnCastingEnded += HandleCastingEnded;
        }
    }

    void OnDisable()
    {
        if (spellManager != null)
        {
            spellManager.OnSpellLoaded -= HandleSpellLoaded;
            spellManager.OnSpellUnloaded -= HandleSpellUnloaded;
            spellManager.OnCastingEnded -= HandleCastingEnded;
        }
    }

    void Update()
    {
        if (!indicatorVisible || indicatorObj == null) return;

        // Pulsing scale
        float pulse = Mathf.Lerp(pulseMin, pulseMax,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        float s = indicatorSize * pulse;
        indicatorObj.transform.localScale = new Vector3(s, s, s);

        // Pulsing emission
        if (indicatorMatInst != null)
        {
            float ep = Mathf.Lerp(emissionIntensity * 0.4f, emissionIntensity,
                (Mathf.Sin(Time.time * pulseSpeed * 1.5f) + 1f) * 0.5f);
            indicatorMatInst.SetColor("_EmissionColor", currentSpellColor * ep);
        }
    }

    // ───────────────────────── Indicator ─────────────────────────

    private void CreateIndicator()
    {
        indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicatorObj.name = "SpellIndicator";
        indicatorObj.transform.SetParent(transform);
        indicatorObj.transform.localPosition = Vector3.zero;
        indicatorObj.transform.localScale = Vector3.one * indicatorSize;

        // Remove collider so it doesn't interfere with spell node detection
        Collider col = indicatorObj.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        Renderer rend = indicatorObj.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        if (indicatorMaterial != null)
        {
            indicatorMatInst = new Material(indicatorMaterial);
            rend.material = indicatorMatInst;
        }

        indicatorObj.SetActive(false);
        indicatorVisible = false;
    }

    private void ShowIndicator(Color color)
    {
        currentSpellColor = color;
        indicatorVisible = true;

        if (indicatorMatInst != null)
        {
            indicatorMatInst.SetColor("_BaseColor", color);
            indicatorMatInst.SetColor("_Color", color);
            indicatorMatInst.EnableKeyword("_EMISSION");
            indicatorMatInst.SetColor("_EmissionColor", color * emissionIntensity);
        }

        if (indicatorObj != null)
            indicatorObj.SetActive(true);
    }

    private void HideIndicator()
    {
        indicatorVisible = false;
        if (indicatorObj != null)
            indicatorObj.SetActive(false);
    }

    // ───────────────────────── Event handlers ─────────────────────────

    private void HandleSpellLoaded(SpellDefinition spell)
    {
        ShowIndicator(spell.spellColor);

        // Extra haptic kick so the player knows a spell locked in
        if (wandController != null)
            wandController.SendHapticPulse(0.6f, 0.1f);
    }

    private void HandleSpellUnloaded()
    {
        HideIndicator();
    }

    private void HandleCastingEnded()
    {
        HideIndicator();
    }

    // ───────────────────────── Overlap / Trigger ─────────────────────────

    /// <summary>
    /// Checks whether the wand tip is already overlapping a spell node
    /// at the moment the player starts casting.
    /// </summary>
    public void CheckInitialOverlaps()
    {
        if (spellManager == null || !spellManager.IsCasting) return;

        float worldRadius = sphereCol.radius * Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z);

        Collider[] hits = Physics.OverlapSphere(transform.position, worldRadius);
        foreach (Collider hit in hits)
        {
            SpellCollider sc = hit.GetComponent<SpellCollider>();
            if (sc != null)
            {
                sc.Activate();
                spellManager.AddToPattern(sc.ColliderIndex);

                if (wandController != null)
                    wandController.SendHapticPulse(0.4f, 0.05f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (spellManager == null || !spellManager.IsCasting) return;

        SpellCollider sc = other.GetComponent<SpellCollider>();
        if (sc != null)
        {
            sc.Activate();
            spellManager.AddToPattern(sc.ColliderIndex);

            if (wandController != null)
                wandController.SendHapticPulse(0.4f, 0.05f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        SpellCollider sc = other.GetComponent<SpellCollider>();
        if (sc != null)
            sc.Deactivate();
    }
}