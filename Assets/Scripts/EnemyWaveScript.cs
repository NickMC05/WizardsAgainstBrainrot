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

    // ==================== SPAWN POINTS ====================
    [Header("📍 Spawn Points")]
    [SerializeField, Tooltip("List of GameObjects that serve as spawn points. Enemies will spawn randomly around these points.")]
    private List<GameObject> spawnPoints = new List<GameObject>();

    [Header("🌊 Fibonacci Wave Settings")]
    [SerializeField, Tooltip("Enemy count for Wave 1 (F₁)")]
    private int fibWave1Count = 3;

    [SerializeField, Tooltip("Enemy count for Wave 2 (F₂)")]
    private int fibWave2Count = 5;

    [Header("⚙️ Spawn Settings")]
    public float spawnRange = 20f;
    public float minRange = 5f;
    [SerializeField, Tooltip("Random range in units around each spawn point (default: 5)")]
    private float spawnPointRadius = 5f;

    [Header("🔄 Incremental Spawn Settings")]
    [SerializeField, Tooltip("Minimum number of enemies to spawn at once")]
    private int minSpawnBatchSize = 3;
    [SerializeField, Tooltip("Maximum number of enemies to spawn at once")]
    private int maxSpawnBatchSize = 5;
    [SerializeField, Tooltip("Delay in seconds between spawning batches")]
    private float spawnBatchDelay = 1f;
    [SerializeField, Tooltip("Delay in seconds between spawning individual enemies within a batch")]
    private float spawnIndividualDelay = 0.5f;

    [Header("🔗 References")]
    public Transform playerTransform;
    public TMP_Text killAndWaveCounter;
    public GameObject waveOverScreen;

    [Header("Tutorial Stage")]
    [SerializeField] private GameObject tutorialMobPrefab;
    [SerializeField] private Transform tutorialMobSpawnPoint;
    [SerializeField] private Vector3 tutorialSpawnOffset = new Vector3(0f, 0f, 3f);
    [SerializeField] private GameObject tutorialUIPanel; // New: panel shown during tutorial
    private GameObject tutorialMobInstance;
    private bool tutorialCompleted = false;

    [SerializeField] private GameObject gameOverScreen; // leave empty for now, assign later if wanted

    private bool isGameOver = false;

    [SerializeField] private GameStartUI gameStartUI;


    // ==================== RUNTIME DATA ====================
    [HideInInspector]
    public List<GameObject> aliveEnemies = new List<GameObject>();

    private int currentWave = 0;          // 0-indexed internally
    private int fibPrev, fibCurr;         // Fibonacci tracking
    private int currentWaveEnemyCount = 0;
    public int enemiesKilled = 0;         // Made public for external access if needed

    private int enemiesRemainingToSpawn = 0;
    private int enemiesSpawnedSoFar = 0;
    private bool isSpawningWave = false;
    private Coroutine spawnCoroutine = null;

    private List<GameObject> pendingEnemiesToSpawn = new List<GameObject>(); // Pre-selected enemies for the wave
    private int currentPendingIndex = 0;

    // ==================== UNITY MESSAGES ====================
    void Start()
    {
        fibPrev = fibWave1Count;
        fibCurr = fibWave2Count;

        ValidateSpawnPoints();
        ValidateMonsterPool();

        StartTutorialStage();
        UpdateUI();
        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }

        private void StartTutorialStage()
    {
        tutorialCompleted = false;

        if (waveOverScreen != null)
            waveOverScreen.SetActive(false);

        if (tutorialUIPanel != null)
            tutorialUIPanel.SetActive(true);

        if (currentWave <= 0)
            currentWave = 0;

        enemiesKilled = 0;
        enemiesSpawnedSoFar = 0;
        enemiesRemainingToSpawn = 0;
        currentPendingIndex = 0;
        pendingEnemiesToSpawn.Clear();
        aliveEnemies.Clear();
        UpdateUI();

        SpawnTutorialMob();
        Debug.Log("Tutorial stage started. Cast a fireball to continue.");
    }

    private void SpawnTutorialMob()
    {
        if (tutorialMobPrefab == null)
        {
            Debug.LogWarning("Tutorial mob prefab is not assigned.");
            return;
        }

        Transform spawnAnchor = tutorialMobSpawnPoint != null ? tutorialMobSpawnPoint : playerTransform;
        if (spawnAnchor == null)
        {
            spawnAnchor = transform;
        }

        Vector3 spawnPos = spawnAnchor.position + spawnAnchor.forward * tutorialSpawnOffset.z
                         + spawnAnchor.right * tutorialSpawnOffset.x
                         + spawnAnchor.up * tutorialSpawnOffset.y;

        tutorialMobInstance = Instantiate(tutorialMobPrefab, spawnPos, Quaternion.LookRotation(-spawnAnchor.forward));
        TutorialTarget tutorialTarget = tutorialMobInstance.GetComponent<TutorialTarget>();
        if (tutorialTarget != null)
        {
            tutorialTarget.Initialize(this);
        }

        Debug.Log($"Tutorial mob spawned at {spawnPos}");
    }

    public void CompleteTutorialStage()
    {
        if (isGameOver) return;
        if (tutorialCompleted) return;

        tutorialCompleted = true;

        if (tutorialUIPanel != null)
            tutorialUIPanel.SetActive(false);

        if (tutorialMobInstance != null)
        {
            Destroy(tutorialMobInstance);
            tutorialMobInstance = null;
        }

        // Trigger the GameStartUI fade effect
        if (gameStartUI != null)
            gameStartUI.FadeImageInAndOut();

        Debug.Log("Tutorial completed. Starting main waves.");

        CalculateWaveEnemyCount();
        UpdateUI();
        StartWaveSpawning();
    }

    void Update()
    {
        if (isGameOver) return;
        if (!tutorialCompleted) return;

        if (!isSpawningWave && aliveEnemies.Count == 0 && enemiesRemainingToSpawn == 0 && enemiesKilled >= currentWaveEnemyCount && currentWaveEnemyCount > 0)
        {
            OnWaveCleared();
        }
    }

    // ==================== WAVE MANAGEMENT ====================
    public void NextWave()
    {
        if (isGameOver) return;
        if (!tutorialCompleted)
        {
            Debug.Log("Tutorial has not been completed yet.");
            return;
        }

        waveOverScreen.SetActive(false);
        currentWave++;

        CalculateWaveEnemyCount();
        UpdateUI();
        StartWaveSpawning();
    }

    private void OnWaveCleared()
    {
        enemiesKilled = 0;
        enemiesSpawnedSoFar = 0;
        enemiesRemainingToSpawn = 0;
        currentPendingIndex = 0;
        pendingEnemiesToSpawn.Clear();

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

        enemiesRemainingToSpawn = currentWaveEnemyCount;
    }

    private int GetNextFibonacciCount()
    {
        // Preview next wave count for logging
        if (currentWave == 0) return fibCurr;
        if (currentWave == 1) return fibPrev + fibCurr;
        return fibPrev + fibCurr;
    }

    // ==================== INCREMENTAL SPAWN SYSTEM ====================
    private void StartWaveSpawning()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnWaveIncrementally());
    }

    private IEnumerator SpawnWaveIncrementally()
    {
        isSpawningWave = true;

        // Clear existing enemies and lists
        aliveEnemies.Clear();
        enemiesSpawnedSoFar = 0;
        currentPendingIndex = 0;

        // Pre-select all enemies for this wave (to maintain weighted distribution)
        BuildWaveEnemyList();

        if (pendingEnemiesToSpawn.Count == 0)
        {
            Debug.LogWarning("⚠️ No enemies to spawn!");
            isSpawningWave = false;
            yield break;
        }

        Debug.Log($"🌊 Wave {currentWave + 1}: Starting incremental spawn. Total enemies: {currentWaveEnemyCount}");

        // Spawn in batches
        while (currentPendingIndex < pendingEnemiesToSpawn.Count)
        {
            // Determine batch size (3-5, but not exceeding remaining enemies)
            int batchSize = Random.Range(minSpawnBatchSize, maxSpawnBatchSize + 1);
            batchSize = Mathf.Min(batchSize, pendingEnemiesToSpawn.Count - currentPendingIndex);

            Debug.Log($"📦 Spawning batch of {batchSize} enemies ({currentPendingIndex + 1}-{currentPendingIndex + batchSize} of {pendingEnemiesToSpawn.Count})");

            // Spawn each enemy in the batch with individual delays
            for (int i = 0; i < batchSize; i++)
            {
                if (currentPendingIndex >= pendingEnemiesToSpawn.Count)
                    break;

                // Spawn the next enemy
                SpawnSingleEnemy(pendingEnemiesToSpawn[currentPendingIndex]);
                currentPendingIndex++;
                enemiesRemainingToSpawn--;
                enemiesSpawnedSoFar++;

                // Wait between individual enemy spawns
                if (i < batchSize - 1 && spawnIndividualDelay > 0)
                    yield return new WaitForSeconds(spawnIndividualDelay);
            }

            // Wait before spawning the next batch (if there are more enemies)
            if (currentPendingIndex < pendingEnemiesToSpawn.Count && spawnBatchDelay > 0)
                yield return new WaitForSeconds(spawnBatchDelay);
        }

        isSpawningWave = false;
        Debug.Log($"✅ Wave {currentWave + 1}: All {enemiesSpawnedSoFar} enemies have been queued for spawning!");
    }

    private void BuildWaveEnemyList()
    {
        pendingEnemiesToSpawn.Clear();

        if (monsterPool == null || monsterPool.Count == 0)
        {
            Debug.LogWarning("⚠️ Monster pool is empty! No enemies spawned.");
            return;
        }

        // Build cumulative distribution for weighted random selection
        var cumulativeChances = BuildCumulativeChances(out float totalChance);

        // Pre-select all enemies for the wave
        for (int i = 0; i < currentWaveEnemyCount; i++)
        {
            GameObject selected = SelectMonsterByWeight(cumulativeChances, totalChance);
            if (selected != null)
            {
                pendingEnemiesToSpawn.Add(selected);
            }
        }

        Debug.Log($"📋 Built wave enemy list with {pendingEnemiesToSpawn.Count} enemies");
    }

    private void SpawnSingleEnemy(GameObject enemyPrefab)
    {
        if (enemyPrefab == null)
            return;

        Vector3 spawnPos = FindValidSpawnLocationAroundSpawnPoint();
        GameObject spawned = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        spawned.name = enemyPrefab.name;

        // Inject dependencies
        var enemyCtrl = spawned.GetComponent<EnemyController>();
        if (enemyCtrl != null)
        {
            enemyCtrl.playerTransform = playerTransform;
            enemyCtrl.EnemyWaveController = this;
        }

        aliveEnemies.Add(spawned);
        UpdateUI();

        Debug.Log($"✨ Spawned {enemyPrefab.name} at {spawnPos} ({aliveEnemies.Count} alive, {pendingEnemiesToSpawn.Count - currentPendingIndex} remaining to spawn)");
    }

    // Optional: Method to manually trigger next batch (if you want button control instead of automatic)
    public void SpawnNextBatch()
    {
        if (isSpawningWave)
        {
            Debug.Log("Wave is already spawning automatically!");
            return;
        }

        if (currentPendingIndex >= pendingEnemiesToSpawn.Count)
        {
            Debug.Log("No more enemies to spawn!");
            return;
        }

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnNextBatchManually());
    }

    private IEnumerator SpawnNextBatchManually()
    {
        int batchSize = Random.Range(minSpawnBatchSize, maxSpawnBatchSize + 1);
        batchSize = Mathf.Min(batchSize, pendingEnemiesToSpawn.Count - currentPendingIndex);

        for (int i = 0; i < batchSize; i++)
        {
            if (currentPendingIndex >= pendingEnemiesToSpawn.Count)
                break;

            SpawnSingleEnemy(pendingEnemiesToSpawn[currentPendingIndex]);
            currentPendingIndex++;
            enemiesRemainingToSpawn--;
            enemiesSpawnedSoFar++;

            if (i < batchSize - 1 && spawnIndividualDelay > 0)
                yield return new WaitForSeconds(spawnIndividualDelay);
        }

        UpdateUI();
    }

    // ==================== SPAWN POINT HELPERS ====================
    private GameObject GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
            return null;

        // Filter out null references
        List<GameObject> validPoints = new List<GameObject>();
        foreach (var point in spawnPoints)
        {
            if (point != null)
                validPoints.Add(point);
        }

        if (validPoints.Count == 0)
            return null;

        return validPoints[Random.Range(0, validPoints.Count)];
    }

    private Vector3 FindValidSpawnLocationAroundSpawnPoint()
    {
        return FindSpawnLocAroundPointWithRetries(0);
    }

    private Vector3 FindSpawnLocAroundPointWithRetries(int attempts)
    {
        const int MAX_ATTEMPTS = 25;

        if (attempts >= MAX_ATTEMPTS)
        {
            Debug.LogWarning($"⚠️ Could not find valid spawn location after {MAX_ATTEMPTS} attempts");
            // Return a position around the first spawn point as fallback
            if (spawnPoints.Count > 0 && spawnPoints[0] != null)
            {
                return spawnPoints[0].transform.position + new Vector3(
                    Random.Range(-spawnPointRadius, spawnPointRadius),
                    0,
                    Random.Range(-spawnPointRadius, spawnPointRadius)
                );
            }
            return transform.position;
        }

        // Select a random spawn point from the list
        GameObject selectedSpawnPoint = GetRandomSpawnPoint();

        if (selectedSpawnPoint == null)
        {
            return FindSpawnLocWithRetries(attempts + 1);
        }

        // Generate random position within radius around spawn point
        Vector2 randomCircle = Random.insideUnitCircle * spawnPointRadius;
        Vector3 candidate = new Vector3(
            selectedSpawnPoint.transform.position.x + randomCircle.x,
            selectedSpawnPoint.transform.position.y,
            selectedSpawnPoint.transform.position.z + randomCircle.y
        );

        // Optional: Ground check and distance validation
        if (Physics.Raycast(candidate, Vector3.down, 5f))
        {
            return candidate;
        }

        return FindSpawnLocAroundPointWithRetries(attempts + 1);
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

    // ==================== SPAWN LOCATION (LEGACY/FALLBACK) ====================
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
        Debug.Log($"[WaveScript] Killed: {enemiesKilled}/{currentWaveEnemyCount} | Alive: {aliveEnemies.Count} | Remaining to spawn: {pendingEnemiesToSpawn.Count - currentPendingIndex}");
#endif
    }

    // ==================== UI & UTILITIES ====================
    private void UpdateUI()
    {
        if (killAndWaveCounter != null)
        {
            int totalRemaining = (pendingEnemiesToSpawn.Count - currentPendingIndex) + aliveEnemies.Count;
            killAndWaveCounter.text = $"Kills: {enemiesKilled}/{currentWaveEnemyCount}\nWave: {currentWave + 1}\nRemaining: {totalRemaining}";
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

    private void ValidateSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("⚠️ No spawn points assigned! Enemies will spawn using fallback method around the wave controller.");
            return;
        }

        int validCount = 0;
        foreach (var point in spawnPoints)
        {
            if (point != null)
                validCount++;
            else
                Debug.LogWarning("⚠️ Spawn point entry is null!");
        }

        if (validCount == 0)
            Debug.LogError("❌ All spawn points are null! Please assign valid GameObjects.");
        else
            Debug.Log($"✅ Found {validCount} valid spawn points");
    }

    public void TriggerGameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        tutorialCompleted = true; // prevent tutorial from starting waves after death condition

        // Stop any active spawn coroutine
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        isSpawningWave = false;
        enemiesRemainingToSpawn = 0;
        enemiesSpawnedSoFar = 0;
        currentPendingIndex = 0;
        pendingEnemiesToSpawn.Clear();

        // Optional cleanup of tutorial state
        if (tutorialMobInstance != null)
        {
            Destroy(tutorialMobInstance);
            tutorialMobInstance = null;
        }

        if (tutorialUIPanel != null)
            tutorialUIPanel.SetActive(false);

        if (waveOverScreen != null)
            waveOverScreen.SetActive(false);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        Debug.Log("Game Over: a mob reached the fort.");
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

    [ContextMenu("Debug Spawn Points")]
    private void DebugSpawnPoints()
    {
        Debug.Log($"=== Spawn Points Debug ===");
        Debug.Log($"Total spawn points in list: {spawnPoints.Count}");
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
                Debug.Log($"  Point {i}: {spawnPoints[i].name} at {spawnPoints[i].transform.position}");
            else
                Debug.Log($"  Point {i}: NULL");
        }
    }
#endif
}