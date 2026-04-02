using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyWaveScript : MonoBehaviour
{
    // ==================== MONSTER SPAWN DATA ====================
    [System.Serializable]
    public class MonsterSpawnData
    {
        public GameObject monsterPrefab;
        [Range(0f, 100f), Tooltip("Relative spawn weight. Higher = more likely to spawn.")]
        public float spawnChance = 50f;
    }

    [Header("🎮 Monster Pool")]
    [SerializeField, Tooltip("List of monsters with spawn weights. Chances are relative (don't need to sum to 100).")]
    private List<MonsterSpawnData> monsterPool = new List<MonsterSpawnData>();

    // ==================== WAVE CONFIGURATION ====================
    [Header("🌊 Fibonacci Wave Settings")]
    [SerializeField, Tooltip("Enemy count for Wave 1 (F₁)")]
    private int fibWave1Count = 3;

    [SerializeField, Tooltip("Enemy count for Wave 2 (F₂)")]
    private int fibWave2Count = 5;

    [Header("⚙️ Spawn Settings")]
    public float spawnRange = 20f;
    public float minRange = 5f;

    [Header("🔗 References")]
    public Transform playerTransform;
    public TMP_Text killAndWaveCounter;
    public GameObject waveOverScreen;

    // ==================== RUNTIME DATA ====================
    [HideInInspector]
    public List<GameObject> aliveEnemies = new List<GameObject>();

    private int currentWave = 0;          // 0-indexed internally
    private int fibPrev, fibCurr;         // Fibonacci tracking
    private int currentWaveEnemyCount = 0;
    public int enemiesKilled = 0;         // Made public for external access if needed

    // ==================== UNITY MESSAGES ====================
    void Start()
    {
        // Initialize Fibonacci sequence
        fibPrev = fibWave1Count;
        fibCurr = fibWave2Count;

        // Calculate enemy count for Wave 1
        CalculateWaveEnemyCount();

        SpawnWave();
        UpdateUI();

        // Optional: Validate monster pool on start
        ValidateMonsterPool();
    }

    void Update()
    {
        // Check if current wave is cleared
        if (aliveEnemies.Count == 0 && enemiesKilled >= currentWaveEnemyCount && currentWaveEnemyCount > 0)
        {
            OnWaveCleared();
        }
    }

    // ==================== WAVE MANAGEMENT ====================
    public void NextWave()
    {
        waveOverScreen.SetActive(false);
        currentWave++;

        CalculateWaveEnemyCount();
        UpdateUI();
        SpawnWave();
    }

    private void OnWaveCleared()
    {
        enemiesKilled = 0;
        Debug.Log($"✅ Wave {currentWave + 1} cleared! Next: {GetNextFibonacciCount()} enemies");
        waveOverScreen.SetActive(true);
    }

    private void CalculateWaveEnemyCount()
    {
        if (currentWave == 0)
        {
            currentWaveEnemyCount = fibPrev; // Wave 1
        }
        else if (currentWave == 1)
        {
            currentWaveEnemyCount = fibCurr; // Wave 2
        }
        else
        {
            // F(n) = F(n-1) + F(n-2)
            int nextFib = fibPrev + fibCurr;
            fibPrev = fibCurr;
            fibCurr = nextFib;
            currentWaveEnemyCount = nextFib;
        }
    }

    private int GetNextFibonacciCount()
    {
        // Preview next wave count for logging
        if (currentWave == 0) return fibCurr;
        if (currentWave == 1) return fibPrev + fibCurr;
        return fibPrev + fibCurr;
    }

    // ==================== SPAWN SYSTEM ====================
    private void SpawnWave()
    {
        aliveEnemies.Clear();

        if (monsterPool == null || monsterPool.Count == 0)
        {
            Debug.LogWarning("⚠️ Monster pool is empty! No enemies spawned.");
            return;
        }

        // Build cumulative distribution for weighted random selection
        var cumulativeChances = BuildCumulativeChances(out float totalChance);

        // Spawn enemies based on Fibonacci count
        for (int i = 0; i < currentWaveEnemyCount; i++)
        {
            GameObject selected = SelectMonsterByWeight(cumulativeChances, totalChance);

            if (selected != null)
            {
                Vector3 spawnPos = FindValidSpawnLocation();
                GameObject spawned = Instantiate(selected, spawnPos, Quaternion.identity);
                spawned.name = selected.name;

                // Inject dependencies
                var enemyCtrl = spawned.GetComponent<EnemyController>();
                if (enemyCtrl != null)
                {
                    enemyCtrl.playerTransform = playerTransform;
                    enemyCtrl.EnemyWaveController = this;
                }

                aliveEnemies.Add(spawned);
            }
        }

        UpdateUI();
        Debug.Log($"🌊 Wave {currentWave + 1}: Spawned {currentWaveEnemyCount} enemies");
    }

    private List<float> BuildCumulativeChances(out float totalChance)
    {
        var cumulative = new List<float>();
        totalChance = 0f;

        foreach (var data in monsterPool)
        {
            if (data.monsterPrefab != null && data.spawnChance > 0)
            {
                totalChance += data.spawnChance;
                cumulative.Add(totalChance);
            }
        }
        return cumulative;
    }

    private GameObject SelectMonsterByWeight(List<float> cumulativeChances, float totalChance)
    {
        if (cumulativeChances.Count == 0 || totalChance <= 0f) return null;

        float roll = Random.Range(0f, totalChance);

        for (int i = 0; i < cumulativeChances.Count; i++)
        {
            if (roll < cumulativeChances[i])
                return monsterPool[i].monsterPrefab;
        }

        // Fallback (should rarely happen)
        return monsterPool[monsterPool.Count - 1].monsterPrefab;
    }

    // ==================== SPAWN LOCATION ====================
    private Vector3 FindValidSpawnLocation()
    {
        return FindSpawnLocWithRetries(0);
    }

    private Vector3 FindSpawnLocWithRetries(int attempts)
    {
        const int MAX_ATTEMPTS = 25;

        if (attempts >= MAX_ATTEMPTS)
        {
            Debug.LogWarning($"⚠️ Could not find valid spawn location after {MAX_ATTEMPTS} attempts");
            return transform.position + new Vector3(
                Random.Range(-spawnRange, spawnRange),
                0,
                Random.Range(-spawnRange, spawnRange)
            );
        }

        Vector3 candidate = new Vector3(
            Random.Range(-spawnRange, spawnRange) + transform.position.x,
            transform.position.y,
            Random.Range(-spawnRange, spawnRange) + transform.position.z
        );

        // Check: has ground beneath AND far enough from center
        if (Physics.Raycast(candidate, Vector3.down, 5f) &&
            Vector3.Distance(candidate, transform.position) >= minRange)
        {
            return candidate;
        }

        return FindSpawnLocWithRetries(attempts + 1);
    }

    // ==================== ENEMY DEATH HANDLING ====================
    public void RemoveEnemyReference(GameObject enemy)
    {
        if (enemy == null || !aliveEnemies.Contains(enemy)) return;

        aliveEnemies.Remove(enemy);
        enemiesKilled++;
        UpdateUI();

#if UNITY_EDITOR
        Debug.Log($"[WaveScript] Killed: {enemiesKilled}/{currentWaveEnemyCount} | Alive: {aliveEnemies.Count}");
#endif
    }

    // ==================== UI & UTILITIES ====================
    private void UpdateUI()
    {
        if (killAndWaveCounter != null)
        {
            killAndWaveCounter.text = $"Kills: {enemiesKilled}/{currentWaveEnemyCount}\nWave: {currentWave + 1}";
        }
    }

    private void ValidateMonsterPool()
    {
        if (monsterPool.Count == 0)
        {
            Debug.LogWarning("⚠️ Monster pool is empty! Add monsters in the Inspector.");
            return;
        }

        float total = 0f;
        foreach (var m in monsterPool)
        {
            if (m.monsterPrefab == null)
                Debug.LogWarning($"⚠️ Monster entry has null prefab at index {monsterPool.IndexOf(m)}");
            total += m.spawnChance;
        }

        if (total <= 0f)
            Debug.LogWarning("⚠️ Total spawn chance is 0! No monsters will spawn.");
        else if (total != 100f)
            Debug.Log($"ℹ️ Spawn chances sum to {total} (relative weights work fine - no need to equal 100)");
    }

    // Optional: Call this from editor or debug menu to test spawn weights
#if UNITY_EDITOR
    [ContextMenu("Test Spawn Distribution")]
    private void TestSpawnDistribution()
    {
        var counts = new Dictionary<string, int>();
        int samples = 1000;

        var cumulative = BuildCumulativeChances(out float total);

        for (int i = 0; i < samples; i++)
        {
            var selected = SelectMonsterByWeight(cumulative, total);
            if (selected != null)
            {
                string name = selected.name;
                if (!counts.ContainsKey(name)) counts[name] = 0;
                counts[name]++;
            }
        }

        Debug.Log("🎲 Spawn Distribution Test (1000 samples):");
        foreach (var kvp in counts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} ({kvp.Value * 100f / samples:F1}%)");
        }
    }
#endif
}