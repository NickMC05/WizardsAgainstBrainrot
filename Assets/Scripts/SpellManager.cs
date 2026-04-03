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

        [Header("Progression")]
        [SerializeField, Tooltip("How many spells from the top of the list are currently unlocked")]
        private int unlockedSpellCount = 1;

        [Header("References")]
        [Tooltip("Transform at the wand tip where spell effects originate")]
        [SerializeField] private Transform castOrigin;
        [Tooltip("The pentagon collider rig that appears while casting")]
        [SerializeField] private SpellColliderRig spellColliderRig;

        [Header("Projectile")]
        [SerializeField] private float launchSpeed = 20f;
        [SerializeField] private float projectileLifetime = 8f;
        [SerializeField] private bool useGravityAfterLaunch = false;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private string currentPattern = "";
        private bool isCasting;
        private SpellDefinition loadedSpell;

        private GameObject heldProjectile;
        private Rigidbody heldProjectileRb;

        private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
        private readonly List<Collider> disabledColliders = new List<Collider>();

        public bool IsCasting => isCasting;
        public string CurrentPattern => currentPattern;
        public SpellDefinition LoadedSpell => loadedSpell;
        public int UnlockedSpellCount => unlockedSpellCount;

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
            SetUnlockedSpellCount(unlockedSpellCount);
        }

        void LateUpdate()
        {
            if (heldProjectile != null && castOrigin != null)
            {
                heldProjectile.transform.position = castOrigin.position;
                heldProjectile.transform.rotation = castOrigin.rotation;
            }
        }

        public void StartCasting()
        {
            BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
            if (audioMgr != null)
                audioMgr.PlayMagicPlaySFX();

            isCasting = true;
            currentPattern = "";
            loadedSpell = null;
            DestroyHeldProjectile();
            OnCastingStarted?.Invoke();

            spellColliderRig?.Show();

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

            spellColliderRig?.Hide();

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
                    Debug.Log($"[SpellManager] No unlocked spell matches '{currentPattern}'.");
                OnSpellFailed?.Invoke(currentPattern);
            }

            loadedSpell = null;
            currentPattern = "";
            OnCastingEnded?.Invoke();
        }

        public void SetUnlockedSpellCount(int count)
        {
            int max = spells != null ? spells.Count : 0;
            if (max <= 0)
            {
                unlockedSpellCount = 0;
                if (debugLog)
                    Debug.LogWarning("[SpellManager] No spells configured. Unlock count is 0.");
                return;
            }

            unlockedSpellCount = Mathf.Clamp(count, 1, max);

            if (debugLog)
                Debug.Log($"[SpellManager] Unlocked spells: {unlockedSpellCount}/{max}");
        }

        public void UnlockNextSpell()
        {
            SetUnlockedSpellCount(unlockedSpellCount + 1);
        }

        public bool UnlockSpellByName(string spellName)
        {
            if (string.IsNullOrWhiteSpace(spellName) || spells == null || spells.Count == 0)
                return false;

            int idx = spells.FindIndex(s =>
                s != null &&
                !string.IsNullOrWhiteSpace(s.spellName) &&
                string.Equals(s.spellName, spellName, StringComparison.OrdinalIgnoreCase));

            if (idx < 0)
            {
                Debug.LogWarning($"[SpellManager] Could not find spell '{spellName}' to unlock.");
                return false;
            }

            SetUnlockedSpellCount(idx + 1);
            return true;
        }

        private void EvaluatePattern()
        {
            if (loadedSpell != null) return;
            if (spells == null || spells.Count == 0 || unlockedSpellCount <= 0) return;

            SpellDefinition bestMatch = null;
            int bestLength = 0;

            int available = Mathf.Min(unlockedSpellCount, spells.Count);

            for (int i = 0; i < available; i++)
            {
                SpellDefinition candidate = spells[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.pattern)) continue;

                if (currentPattern.Contains(candidate.pattern) &&
                    candidate.pattern.Length > bestLength)
                {
                    bestMatch = candidate;
                    bestLength = candidate.pattern.Length;
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

        private void SpawnHeldProjectile(SpellDefinition spell)
        {
            if (spell.effectPrefab == null || castOrigin == null) return;

            BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
            if (audioMgr != null)
                audioMgr.PlaySpellCastedSFX();

            heldProjectile = Instantiate(spell.effectPrefab, castOrigin.position, castOrigin.rotation);

            heldProjectileRb = heldProjectile.GetComponent<Rigidbody>();
            if (heldProjectileRb == null)
                heldProjectileRb = heldProjectile.AddComponent<Rigidbody>();

            heldProjectileRb.isKinematic = true;
            heldProjectileRb.useGravity = false;
            heldProjectileRb.interpolation = RigidbodyInterpolation.Interpolate;

            disabledScripts.Clear();
            foreach (var mb in heldProjectile.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb.enabled)
                {
                    mb.enabled = false;
                    disabledScripts.Add(mb);
                }
            }

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

            if (heldProjectileRb != null)
            {
                heldProjectileRb.isKinematic = false;
                heldProjectileRb.useGravity = useGravityAfterLaunch;
                heldProjectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                heldProjectileRb.linearVelocity = castOrigin.forward * launchSpeed;
            }

            foreach (var col in disabledColliders)
            {
                if (col != null)
                    col.enabled = true;
            }
            disabledColliders.Clear();

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
            OnSpellUnloaded?.Invoke();
        }
    }