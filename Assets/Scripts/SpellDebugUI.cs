using UnityEngine;
using TMPro;

public class SpellDebugUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI displayText;
    [SerializeField] float messageDuration = 2f;
    [SerializeField] Color successColor = Color.green;
    [SerializeField] Color failColor = Color.red;
    [SerializeField] Color drawingColor = Color.white;
    [SerializeField] Color loadedColor = Color.yellow;

    float messageTimer;
    bool showingResult;
    bool subscribed;

    void Update()
    {
        if (!subscribed && SpellManager.Instance != null)
        {
            SpellManager.Instance.OnCastingStarted += HandleCastingStarted;
            SpellManager.Instance.OnCastingEnded += HandleCastingEnded;
            SpellManager.Instance.OnSpellCast += HandleSpellCast;
            SpellManager.Instance.OnSpellFailed += HandleSpellFailed;
            subscribed = true;
            Debug.Log("[SpellDebugUI] Subscribed to SpellManager events.");
        }

        if (SpellManager.Instance != null && SpellManager.Instance.IsCasting && !showingResult)
        {
            string pattern = SpellManager.Instance.CurrentPattern;
            SpellDefinition loaded = SpellManager.Instance.LoadedSpell;

            if (loaded != null)
            {
                displayText.color = loadedColor;
                displayText.text = $"Release to cast: {loaded.spellName}\nPattern: {pattern}";
            }
            else
            {
                displayText.color = drawingColor;
                displayText.text = pattern.Length > 0
                    ? $"Drawing: {pattern}"
                    : "Drawing...";
            }
        }

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
    }

    void HandleSpellFailed(string pattern)
    {
        displayText.color = failColor;
        displayText.text = $"No match: {pattern}";
        showingResult = true;
        messageTimer = messageDuration;
    }
}