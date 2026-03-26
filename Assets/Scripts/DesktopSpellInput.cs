using UnityEngine;

/// <summary>
/// Simulates wand trigger + rune touches via keyboard for Editor testing.
/// Hold Space = trigger, then tap 1–5 to register runes.
/// Attach to the SpellManager GameObject.
/// Disables itself in builds.
/// </summary>
public class DesktopSpellInput : MonoBehaviour
{
    [SerializeField] KeyCode triggerKey = KeyCode.Space;

    bool isHolding;

    void Awake()
    {
#if !UNITY_EDITOR
        enabled = false;
        return;
#endif
    }

    void Start()
    {
        if (SpellManager.Instance == null)
        {
            Debug.LogWarning("[DesktopSpellInput] No SpellManager found. Disabling.");
            enabled = false;
        }
    }

    void Update()
    {
        if (SpellManager.Instance == null) return;

        if (Input.GetKeyDown(triggerKey))
        {
            isHolding = true;
            SpellManager.Instance.StartCasting();
            Debug.Log("[DesktopSpellInput] Trigger held — start casting");
        }

        if (isHolding)
        {
            for (int i = 1; i <= 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
                {
                    SpellManager.Instance.AddToPattern(i);
                    Debug.Log($"[DesktopSpellInput] Rune {i} registered");
                }
            }
        }

        if (Input.GetKeyUp(triggerKey) && isHolding)
        {
            isHolding = false;
            SpellManager.Instance.FinishCasting();
            Debug.Log("[DesktopSpellInput] Trigger released — finish casting");
        }
    }
}