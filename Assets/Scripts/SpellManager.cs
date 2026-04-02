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

    // Components we disabled while the projectile is held
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private List<Collider> disabledColliders = new List<Collider>();

    public bool IsCasting => isCasting;
    public string CurrentPattern => currentPattern;
    public SpellDefinition LoadedSpell => loadedSpell;

    public event Action OnCastingStarted;
    public event Action OnCastingEnded;
    public event Action<SpellDefinition> OnSpellCast;
    public event Action<string> OnSpellFailed;
    public event Action<SpellDefinition> OnSpellLoaded;
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

    // ───────────────────── Follow held projectile ─────────────────────

    void LateUpdate()
    {
        if (heldProjectile != null && castOrigin != null)
        {
            heldProjectile.transform.position = castOrigin.position;
            heldProjectile.transform.rotation = castOrigin.rotation;
        }
    }

    // ───────────────────── Casting lifecycle ─────────────────────

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
        if (!isCasting) return;

        string indexStr = colliderIndex.ToString();

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

    // ───────────────────── Pattern evaluation ─────────────────────

    private void EvaluatePattern()
    {
        // Once a spell has been loaded, lock it in
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

    // ───────────────────── Projectile management ─────────────────────

    private void SpawnHeldProjectile(SpellDefinition spell)
    {
        if (spell.effectPrefab == null || castOrigin == null) return;

        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        audioMgr.PlaySpellCastedSFX();

        heldProjectile = Instantiate(spell.effectPrefab, castOrigin.position, castOrigin.rotation);

        // ── Do NOT parent ── LateUpdate tracks position instead,
        //    which avoids scale-shearing / egg-warping artefacts.

        // Ensure a Rigidbody exists and freeze it
        heldProjectileRb = heldProjectile.GetComponent<Rigidbody>();
        if (heldProjectileRb == null)
            heldProjectileRb = heldProjectile.AddComponent<Rigidbody>();

        heldProjectileRb.isKinematic = true;
        heldProjectileRb.useGravity = false;
        heldProjectileRb.interpolation = RigidbodyInterpolation.Interpolate;

        // Disable every MonoBehaviour on the prefab so their
        // Start / Update don't run while the spell is held
        disabledScripts.Clear();
        foreach (var mb in heldProjectile.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb.enabled)
            {
                mb.enabled = false;
                disabledScripts.Add(mb);
            }
        }

        // Disable colliders so the held spell doesn't bump into things
        disabledColliders.Clear();
        foreach (var col in heldProjectile.GetComponentsInChildren<Collider>())
        {
            if (col.enabled)
            {
                col.enabled = false;
                disabledColliders.Add(col);
            }
        }

        if (debugLog)
            Debug.Log($"[SpellManager] Projectile spawned (held) for {spell.spellName}.");
    }

    private void LaunchProjectile()
    {
        if (heldProjectile == null) return;

        // Physics setup
        if (heldProjectileRb != null)
        {
            heldProjectileRb.isKinematic = false;
            heldProjectileRb.useGravity = useGravityAfterLaunch;
            heldProjectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            heldProjectileRb.linearVelocity = castOrigin.forward * launchSpeed;
        }

        // Re-enable colliders
        foreach (var col in disabledColliders)
        {
            if (col != null)
                col.enabled = true;
        }
        disabledColliders.Clear();

        // Re-enable scripts (their Start() will run next frame)
        foreach (var mb in disabledScripts)
        {
            if (mb != null)
                mb.enabled = true;
        }
        disabledScripts.Clear();

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
        disabledScripts.Clear();
        disabledColliders.Clear();
    }
}