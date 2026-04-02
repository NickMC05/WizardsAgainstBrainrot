using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpellDefinition
{
    public string spellName = "New Spell";
    public string pattern = "";
    public Color spellColor = Color.cyan;
    public GameObject effectPrefab;
}

public class SpellManager : MonoBehaviour
{
    public static SpellManager Instance { get; private set; }

    [Header("Spells")]
    [SerializeField] private List<SpellDefinition> spells = new List<SpellDefinition>();

    [Header("References")]
    [Tooltip("Transform at the wand tip where spell effects originate")]
    [SerializeField] private Transform castOrigin;

    [Header("Projectile")]
    [SerializeField] private float launchSpeed = 20f;
    [SerializeField] private float projectileLifetime = 8f;
    [SerializeField] private bool useGravityAfterLaunch = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private string currentPattern = "";
    private bool isCasting;
    private SpellDefinition loadedSpell;

    // Held projectile state
    private GameObject heldProjectile;
    private Rigidbody heldProjectileRb;

    public bool IsCasting => isCasting;
    public string CurrentPattern => currentPattern;
    public SpellDefinition LoadedSpell => loadedSpell;

    /// <summary>Fired when the player begins drawing a pattern.</summary>
    public event Action OnCastingStarted;

    /// <summary>Fired after the pattern has been evaluated and reset.</summary>
    public event Action OnCastingEnded;

    /// <summary>Fired with the matched spell when a pattern succeeds.</summary>
    public event Action<SpellDefinition> OnSpellCast;

    /// <summary>Fired with the unmatched pattern string on failure.</summary>
    public event Action<string> OnSpellFailed;

    /// <summary>Fired when a registered spell pattern is detected inside the current drawing.</summary>
    public event Action<SpellDefinition> OnSpellLoaded;

    /// <summary>Fired when no spell pattern is detected any more.</summary>
    public event Action OnSpellUnloaded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ????????????????????????? Casting lifecycle ?????????????????????????

    public void StartCasting()
    {
        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        audioMgr.PlayMagicPlaySFX();

        isCasting = true;
        currentPattern = "";
        loadedSpell = null;
        DestroyHeldProjectile();
        OnCastingStarted?.Invoke();

        if (debugLog)
            Debug.Log("[SpellManager] Casting started.");
    }

    public void AddToPattern(int colliderIndex)
    {
        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        audioMgr.PlayClickSFX();

        if (!isCasting) return;

        string indexStr = colliderIndex.ToString();

        // Prevent the same node from being registered twice in a row
        if (currentPattern.Length > 0 &&
            currentPattern[currentPattern.Length - 1].ToString() == indexStr)
            return;

        currentPattern += indexStr;

        if (debugLog)
            Debug.Log($"[SpellManager] Pattern so far: {currentPattern}");

        EvaluatePattern();
    }

    public void FinishCasting()
    {
        if (!isCasting) return;
        isCasting = false;

        if (debugLog)
            Debug.Log($"[SpellManager] Casting finished. Final pattern: {currentPattern}");

        if (loadedSpell != null)
        {
            Debug.Log($"[SpellManager] >>> SPELL CAST: {loadedSpell.spellName} <<<");
            LaunchProjectile();
            OnSpellCast?.Invoke(loadedSpell);
        }
        else
        {
            DestroyHeldProjectile();

            if (debugLog)
                Debug.Log($"[SpellManager] No spell matches '{currentPattern}'.");
            OnSpellFailed?.Invoke(currentPattern);
        }

        loadedSpell = null;
        currentPattern = "";
        OnCastingEnded?.Invoke();
    }

    // ????????????????????????? Pattern evaluation ?????????????????????????

    /// <summary>
    /// Finds the longest registered spell pattern that appears as a
    /// substring of the current drawing. Spawns / swaps the held
    /// projectile when the result changes.
    /// </summary>
    private void EvaluatePattern()
    {
        // Once a spell has been loaded, lock it in � no switching
        if (loadedSpell != null) return;

        SpellDefinition bestMatch = null;
        int bestLength = 0;

        for (int i = 0; i < spells.Count; i++)
        {
            if (string.IsNullOrEmpty(spells[i].pattern)) continue;

            if (currentPattern.Contains(spells[i].pattern) &&
                spells[i].pattern.Length > bestLength)
            {
                bestMatch = spells[i];
                bestLength = spells[i].pattern.Length;
            }
        }

        if (bestMatch != null)
        {
            loadedSpell = bestMatch;
            SpawnHeldProjectile(loadedSpell);

            if (debugLog)
                Debug.Log($"[SpellManager] Spell loaded (locked): {loadedSpell.spellName}");
            OnSpellLoaded?.Invoke(loadedSpell);
        }
    }

    // ????????????????????????? Projectile management ?????????????????????????

    private void SpawnHeldProjectile(SpellDefinition spell)
    {
        if (spell.effectPrefab == null || castOrigin == null) return;

        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        audioMgr.PlaySpellCastedSFX();

        heldProjectile = Instantiate(spell.effectPrefab, castOrigin.position, castOrigin.rotation);
        heldProjectile.transform.SetParent(castOrigin);
        heldProjectile.transform.localPosition = Vector3.zero;
        heldProjectile.transform.localRotation = Quaternion.identity;

        // Ensure a Rigidbody exists for the launch phase
        heldProjectileRb = heldProjectile.GetComponent<Rigidbody>();
        if (heldProjectileRb == null)
            heldProjectileRb = heldProjectile.AddComponent<Rigidbody>();

        heldProjectileRb.isKinematic = true;
        heldProjectileRb.useGravity = false;
        heldProjectileRb.interpolation = RigidbodyInterpolation.Interpolate;

        if (debugLog)
            Debug.Log($"[SpellManager] Projectile spawned for {spell.spellName}.");
    }

    private void LaunchProjectile()
    {
        if (heldProjectile == null) return;

        // Unparent so it flies freely
        heldProjectile.transform.SetParent(null);

        if (heldProjectileRb != null)
        {
            heldProjectileRb.isKinematic = false;
            heldProjectileRb.useGravity = useGravityAfterLaunch;
            heldProjectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            heldProjectileRb.linearVelocity = castOrigin.forward * launchSpeed;
        }

        Destroy(heldProjectile, projectileLifetime);

        if (debugLog)
            Debug.Log($"[SpellManager] Projectile launched at {launchSpeed} m/s.");

        heldProjectile = null;
        heldProjectileRb = null;
    }

    private void DestroyHeldProjectile()
    {
        if (heldProjectile != null)
        {
            Destroy(heldProjectile);
            heldProjectile = null;
            heldProjectileRb = null;
        }
    }
}