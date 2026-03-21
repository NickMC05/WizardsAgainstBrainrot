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

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private string currentPattern = "";
    private bool isCasting;

    public bool IsCasting => isCasting;
    public string CurrentPattern => currentPattern;

    /// <summary>Fired when the player begins drawing a pattern.</summary>
    public event Action OnCastingStarted;

    /// <summary>Fired after the pattern has been evaluated and reset.</summary>
    public event Action OnCastingEnded;

    /// <summary>Fired with the matched spell when a pattern succeeds.</summary>
    public event Action<SpellDefinition> OnSpellCast;

    /// <summary>Fired with the unmatched pattern string on failure.</summary>
    public event Action<string> OnSpellFailed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartCasting()
    {
        isCasting = true;
        currentPattern = "";
        OnCastingStarted?.Invoke();

        if (debugLog)
            Debug.Log("[SpellManager] Casting started.");
    }

    public void AddToPattern(int colliderIndex)
    {
        if (!isCasting) return;

        string indexStr = colliderIndex.ToString();

        // Prevent the same node from being registered twice in a row
        // (guards against physics jitter at collider boundaries).
        if (currentPattern.Length > 0 &&
            currentPattern[currentPattern.Length - 1].ToString() == indexStr)
            return;

        currentPattern += indexStr;

        if (debugLog)
            Debug.Log($"[SpellManager] Pattern so far: {currentPattern}");
    }

    public void FinishCasting()
    {
        if (!isCasting) return;
        isCasting = false;

        if (debugLog)
            Debug.Log($"[SpellManager] Casting finished. Final pattern: {currentPattern}");

        // Attempt to match
        SpellDefinition matched = null;
        for (int i = 0; i < spells.Count; i++)
        {
            if (spells[i].pattern == currentPattern)
            {
                matched = spells[i];
                break;
            }
        }

        if (matched != null)
            CastSpell(matched);
        else
        {
            if (debugLog)
                Debug.Log($"[SpellManager] No spell matches '{currentPattern}'.");
            OnSpellFailed?.Invoke(currentPattern);
        }

        currentPattern = "";
        OnCastingEnded?.Invoke();
    }

    private void CastSpell(SpellDefinition spell)
    {
        Debug.Log($"[SpellManager] >>> SPELL CAST: {spell.spellName} <<<");

        if (spell.effectPrefab != null && castOrigin != null)
        {
            GameObject fx = Instantiate(spell.effectPrefab, castOrigin.position, castOrigin.rotation);
            Destroy(fx, 5f);
        }

        OnSpellCast?.Invoke(spell);
    }
}