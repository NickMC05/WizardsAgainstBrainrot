using UnityEngine;
using TMPro;

public class SpellDebugUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI displayText;
    [SerializeField] float messageDuration = 2f;
    [SerializeField] Color successColor = Color.green;
    [SerializeField] Color failColor = Color.red;
    [SerializeField] Color drawingColor = Color.white;

    float messageTimer;
    bool showingResult;
    bool subscribed;

    void Update()
    {
        // Late subscribe once SpellManager is ready
        if (!subscribed && SpellManager.Instance != null)
        {
            SpellManager.Instance.OnCastingStarted += HandleCastingStarted;
            SpellManager.Instance.OnCastingEnded += HandleCastingEnded;
            SpellManager.Instance.OnSpellCast += HandleSpellCast;
            SpellManager.Instance.OnSpellFailed += HandleSpellFailed;
            subscribed = true;
            Debug.Log("[SpellDebugUI] Subscribed to SpellManager events.");
        }

        // While drawing, show live pattern
        if (SpellManager.Instance != null && SpellManager.Instance.IsCasting && !showingResult)
        {
            string pattern = SpellManager.Instance.CurrentPattern;
            displayText.color = drawingColor;
            displayText.text = pattern.Length > 0
                ? $"Drawing: {pattern}"
                : "Drawing...";
        }

        // Count down result message
        if (showingResult)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f)
            {
                showingResult = false;
                displayText.text = "";
            }
        }
    }

    void OnDestroy()
    {
        if (subscribed && SpellManager.Instance != null)
        {
            SpellManager.Instance.OnCastingStarted -= HandleCastingStarted;
            SpellManager.Instance.OnCastingEnded -= HandleCastingEnded;
            SpellManager.Instance.OnSpellCast -= HandleSpellCast;
            SpellManager.Instance.OnSpellFailed -= HandleSpellFailed;
        }
    }

    void HandleCastingStarted()
    {
        showingResult = false;
        displayText.text = "Drawing...";
        displayText.color = drawingColor;
        Debug.Log("[SpellDebugUI] Casting started.");
    }

    void HandleCastingEnded()
    {
        Debug.Log("[SpellDebugUI] Casting ended.");
    }

    void HandleSpellCast(SpellDefinition spell)
    {
        displayText.color = successColor;
        displayText.text = $">> {spell.spellName} <<";
        showingResult = true;
        messageTimer = messageDuration;
        Debug.Log($"[SpellDebugUI] Spell cast: {spell.spellName}");
    }

    void HandleSpellFailed(string pattern)
    {
        displayText.color = failColor;
        displayText.text = $"No match: {pattern}";
        showingResult = true;
        messageTimer = messageDuration;
        Debug.Log($"[SpellDebugUI] Spell failed: {pattern}");
    }
}