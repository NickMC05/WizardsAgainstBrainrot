using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    public Transform playerTransform; // Reference to the player's Transform
    public float maxHealth = 100f; // Maximum health of the enemy
    public float health; // Current health of the enemy
    public float moveSpeed = 5f; // Speed at which the enemy moves
    public float rotationSpeed = 5f; // Speed of rotation to face the player
    // Reference to the wave controller script (set when spawned)
    public EnemyWaveScript EnemyWaveController;

    [Header("HP Bar Settings")]
    public float hpBarHeight = 2.2f; // How far above the enemy the bar floats
    public float hpBarWidth = 1.2f; // Width of the bar in world units
    public float hpBarScaleHeight = 0.15f; // Height of the bar in world units

    [Header("Damage Display Timing")]
    public float damageDisplayDelay = 0.3f; // Pause before yellow starts shrinking
    public float damageLerpDuration = 0.7f; // Time for yellow to catch up after the pause

    private Canvas hpCanvas;
    private Image hpFillImage;         // Green - current health
    private Image hpDelayedFillImage;  // Yellow - delayed health
    private Transform hpBarTransform;

    private float delayedHealth;
    private float damageDisplayTimer;

    void Start()
    {
        health = maxHealth;
        delayedHealth = maxHealth;
        CreateHPBar();
    }

    void Update()
    {
        MoveTowardsPlayer();
        UpdateHPBarOrientation();
        UpdateDelayedHealth();
    }

    void CreateHPBar()
    {
        // Create a GameObject for the canvas
        GameObject canvasObj = new GameObject("HPBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0f, hpBarHeight, 0f);

        // Set up world-space canvas
        hpCanvas = canvasObj.AddComponent<Canvas>();
        hpCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = hpCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(hpBarWidth * 100f, hpBarScaleHeight * 100f);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        hpBarTransform = canvasObj.transform;

        // Layer 1: Red background (represents lost health)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.grey;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // Layer 2: Yellow delayed fill (shows recent damage)
        GameObject delayedFillObj = new GameObject("DelayedFill");
        delayedFillObj.transform.SetParent(canvasObj.transform, false);
        hpDelayedFillImage = delayedFillObj.AddComponent<Image>();
        hpDelayedFillImage.color = Color.yellow;
        RectTransform delayedFillRect = delayedFillObj.GetComponent<RectTransform>();
        delayedFillRect.anchorMin = Vector2.zero;
        delayedFillRect.anchorMax = Vector2.one;
        delayedFillRect.sizeDelta = Vector2.zero;
        delayedFillRect.anchoredPosition = Vector2.zero;
        delayedFillRect.pivot = new Vector2(0f, 0.5f);

        // Layer 3: Green fill (represents current health)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        hpFillImage = fillObj.AddComponent<Image>();
        hpFillImage.color = Color.green;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.pivot = new Vector2(0f, 0.5f);

        UpdateHPBarFill();
    }

    void UpdateDelayedHealth()
    {
        if (delayedHealth > health)
        {
            if (damageDisplayTimer > 0f)
            {
                damageDisplayTimer -= Time.deltaTime;
            }
            else
            {
                float speed = maxHealth / damageLerpDuration;
                delayedHealth = Mathf.MoveTowards(delayedHealth, health, speed * Time.deltaTime);
                UpdateHPBarFill();
            }
        }
    }

    void UpdateHPBarOrientation()
    {
        if (hpBarTransform == null) return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            hpBarTransform.forward = cam.transform.forward;
        }
        else if (playerTransform != null)
        {
            Vector3 dir = (hpBarTransform.position - playerTransform.position).normalized;
            if (dir != Vector3.zero)
                hpBarTransform.forward = dir;
        }
    }

    void UpdateHPBarFill()
    {
        if (hpFillImage != null)
        {
            float ratio = Mathf.Clamp01(health / maxHealth);
            RectTransform fillRect = hpFillImage.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(ratio, 1f);
        }

        if (hpDelayedFillImage != null)
        {
            float delayedRatio = Mathf.Clamp01(delayedHealth / maxHealth);
            RectTransform delayedFillRect = hpDelayedFillImage.GetComponent<RectTransform>();
            delayedFillRect.anchorMax = new Vector2(delayedRatio, 1f);
        }
    }

    void MoveTowardsPlayer()
    {
        if (playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    public void TakeDamage(float damage)
    {
        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        audioMgr.PlayVoiceLine(gameObject.name);

        health -= damage;
        damageDisplayTimer = damageDisplayDelay; // Reset the delay so yellow pauses
        Debug.Log("Enemy hit! Remaining health: " + health);

        UpdateHPBarFill(); // Immediately update green bar

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died!");

        if (EnemyWaveController != null)
        {
            EnemyWaveController.RemoveEnemyReference(gameObject);
        }
        else
        {
            var waveScript = FindObjectOfType<EnemyWaveScript>();
            if (waveScript != null)
            {
                waveScript.RemoveEnemyReference(gameObject);
            }
        }

        Destroy(gameObject);
    }
}