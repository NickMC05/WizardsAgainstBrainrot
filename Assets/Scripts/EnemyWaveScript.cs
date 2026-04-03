    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;


    public class EnemyWaveScript : MonoBehaviour
    {
        [System.Serializable]
        public class MonsterSpawnData
        {
            public GameObject monsterPrefab;
            [Range(0f, 100f), Tooltip("Relative spawn weight. Higher = more likely to spawn.")]
            public float spawnChance = 50f;
        }

        private enum Stage
        {
            Tutorial,
            Wave1,
            Wave2,
            Wave3,
            Completed
        }

        [Header("Spell Unlock UI")]
        [SerializeField] private Image spellUnlockImage;
        [SerializeField] private Sprite lightningSpellSprite;
        [SerializeField] private Sprite waveSpellSprite;
        [SerializeField] private Image lightningUnlockedImage;
        [SerializeField] private Image waveUnlockedImage;

        [Header("Monster Pool")]
        [SerializeField, Tooltip("List of monsters with spawn weights. Chances are relative (don't need to sum to 100).")]
        private List<MonsterSpawnData> monsterPool = new List<MonsterSpawnData>();

        [Header("Spawn Points")]
        [SerializeField, Tooltip("List of GameObjects that serve as spawn points. Enemies will spawn randomly around these points.")]
        private List<GameObject> spawnPoints = new List<GameObject>();

        [Header("Wave Settings")]
        [SerializeField, Tooltip("Enemy count for Wave 1")]
        private int wave1EnemyCount = 3;
        [SerializeField, Tooltip("Enemy count for Wave 2")]
        private int wave2EnemyCount = 5;
        [SerializeField, Tooltip("Enemy count for Wave 3")]
        private int wave3EnemyCount = 8;

        [Header("Spawn Settings")]
        public float spawnRange = 20f;
        public float minRange = 5f;
        [SerializeField, Tooltip("Random range in units around each spawn point (default: 5)")]
        private float spawnPointRadius = 5f;

        [Header("Incremental Spawn Settings")]
        [SerializeField, Tooltip("Minimum number of enemies to spawn at once")]
        private int minSpawnBatchSize = 3;
        [SerializeField, Tooltip("Maximum number of enemies to spawn at once")]
        private int maxSpawnBatchSize = 5;
        [SerializeField, Tooltip("Delay in seconds between spawning batches")]
        private float spawnBatchDelay = 1f;
        [SerializeField, Tooltip("Delay in seconds between spawning individual enemies within a batch")]
        private float spawnIndividualDelay = 0.5f;

        [Header("References")]
        public Transform playerTransform;
        public TMP_Text killAndWaveCounter;
        public GameObject waveOverScreen;
        [SerializeField] private SpellManager spellManager;

        [Header("Spell Unlock Names")]
        [SerializeField] private string lightningSpellName = "Lightning";
        [SerializeField] private string waveSpellName = "Wave";

        [Header("Tutorial Stage")]
        [SerializeField] private GameObject tutorialMobPrefab;
        [SerializeField] private Transform tutorialMobSpawnPoint;
        [SerializeField] private Vector3 tutorialSpawnOffset = new Vector3(0f, 0f, 3f);
        [SerializeField] private GameObject tutorialUIPanel;
        private GameObject tutorialMobInstance;
        private bool tutorialCompleted = false;

        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameStartUI gameStartUI;

        [HideInInspector]
        public List<GameObject> aliveEnemies = new List<GameObject>();

        public int enemiesKilled = 0;

        private Stage currentStage = Stage.Tutorial;
        private bool isGameOver = false;
        private int currentWaveEnemyCount = 0;
        private int enemiesRemainingToSpawn = 0;
        private int enemiesSpawnedSoFar = 0;
        private bool isSpawningWave = false;
        private Coroutine spawnCoroutine = null;

        private readonly List<GameObject> pendingEnemiesToSpawn = new List<GameObject>();
        private int currentPendingIndex = 0;

        void Start()
        {
            ValidateSpawnPoints();
            ValidateMonsterPool();

            if (spellManager == null)
                spellManager = SpellManager.Instance;

            StartTutorialStage();
            UpdateUI();

            if (gameOverScreen != null)
                gameOverScreen.SetActive(false);
        }

        private void StartTutorialStage()
        {
            currentStage = Stage.Tutorial;
            tutorialCompleted = false;
            currentWaveEnemyCount = 0;

            if (waveOverScreen != null)
                waveOverScreen.SetActive(false);

            if (tutorialUIPanel != null)
                tutorialUIPanel.SetActive(true);

            ResetWaveRuntimeState();
            UpdateUI();

            if (spellManager != null)
                spellManager.SetUnlockedSpellCount(1);

            SpawnTutorialMob();
            Debug.Log("Tutorial stage started. Cast a fireball to continue.");
            UpdateUnlockedSpellUIVisibility();

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
                spawnAnchor = transform;

            Vector3 spawnPos = spawnAnchor.position + spawnAnchor.forward * tutorialSpawnOffset.z
                             + spawnAnchor.right * tutorialSpawnOffset.x
                             + spawnAnchor.up * tutorialSpawnOffset.y;

            tutorialMobInstance = Instantiate(tutorialMobPrefab, spawnPos, Quaternion.LookRotation(-spawnAnchor.forward));
            TutorialTarget tutorialTarget = tutorialMobInstance.GetComponent<TutorialTarget>();
            if (tutorialTarget != null)
                tutorialTarget.Initialize(this);

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

            if (gameStartUI != null)
                gameStartUI.FadeImageInAndOut();

            Debug.Log("Tutorial completed. Starting Wave 1.");
            StartStageWave(Stage.Wave1);
        }

        void Update()
        {
            if (isGameOver) return;
            if (!tutorialCompleted) return;
            if (!IsCombatStage(currentStage)) return;

            if (!isSpawningWave &&
                aliveEnemies.Count == 0 &&
                enemiesRemainingToSpawn == 0 &&
                enemiesKilled >= currentWaveEnemyCount &&
                currentWaveEnemyCount > 0)
            {
                OnWaveCleared();
            }
        }

        public void NextWave()
        {
            if (isGameOver) return;
            if (!tutorialCompleted)
            {
                Debug.Log("Tutorial has not been completed yet.");
                return;
            }

            if (waveOverScreen != null)
                waveOverScreen.SetActive(false);

            if (currentStage == Stage.Wave2)
            {
                StartStageWave(Stage.Wave2);
            }
            else if (currentStage == Stage.Wave3)
            {
                StartStageWave(Stage.Wave3);
            }
            else if (currentStage == Stage.Completed)
            {
                Debug.Log("All stages complete.");
            }
            else
            {
                Debug.Log("No wave is currently queued.");
            }
        }

        private void StartStageWave(Stage stage)
        {
            currentStage = stage;
            currentWaveEnemyCount = GetEnemyCountForStage(stage);

            ResetWaveRuntimeState();
            enemiesRemainingToSpawn = currentWaveEnemyCount;

            UpdateUI();
            StartWaveSpawning();

            Debug.Log($"Starting {GetStageLabel()} with {currentWaveEnemyCount} enemies.");
            UpdateUnlockedSpellUIVisibility();
        }

    private void OnWaveCleared()
    {
        ResetWaveRuntimeState();

        if (currentStage == Stage.Wave1)
        {
            UnlockSpell(lightningSpellName);
            DisplaySpellUnlockImage(lightningSpellSprite);
            currentStage = Stage.Wave2;
            ShowWaveOverScreen();
            Debug.Log("Wave 1 cleared. Lightning unlocked.");
            UpdateUnlockedSpellUIVisibility();
            return;
        }

        if (currentStage == Stage.Wave2)
        {
            UnlockSpell(waveSpellName);
            DisplaySpellUnlockImage(waveSpellSprite);
            currentStage = Stage.Wave3;
            ShowWaveOverScreen();
            Debug.Log("Wave 2 cleared. Wave spell unlocked.");
            UpdateUnlockedSpellUIVisibility();
            return;
        }

        if (currentStage == Stage.Wave3)
        {
            currentStage = Stage.Completed;
            ClearSpellUnlockImage();
            ShowWaveOverScreen();
            Debug.Log("Wave 3 cleared. All stages complete.");
        }
    }

private void DisplaySpellUnlockImage(Sprite sprite)
{
    if (spellUnlockImage != null && sprite != null)
    {
        spellUnlockImage.sprite = sprite;
        spellUnlockImage.gameObject.SetActive(true);
        Debug.Log($"Displaying spell unlock image: {sprite.name}");
    }
}

private void ClearSpellUnlockImage()
{
    if (spellUnlockImage != null)
    {
        spellUnlockImage.sprite = null;
        spellUnlockImage.gameObject.SetActive(false);
    }
}

private void UpdateUnlockedSpellUIVisibility()
{
    // Show Lightning after Wave 1 is complete
    bool showLightning = currentStage == Stage.Wave2 || currentStage == Stage.Wave3 || currentStage == Stage.Completed;
    if (lightningUnlockedImage != null)
        lightningUnlockedImage.gameObject.SetActive(showLightning);

    // Show Wave after Wave 2 is complete
    bool showWave = currentStage == Stage.Wave3 || currentStage == Stage.Completed;
    if (waveUnlockedImage != null)
        waveUnlockedImage.gameObject.SetActive(showWave);
}

        private void UnlockSpell(string spellName)
        {
            if (spellManager == null)
                spellManager = SpellManager.Instance;

            if (spellManager == null)
            {
                Debug.LogWarning("No SpellManager reference found. Could not unlock spell.");
                return;
            }

            bool unlockedByName = spellManager.UnlockSpellByName(spellName);
            if (!unlockedByName)
                spellManager.UnlockNextSpell();
        }

        private void ShowWaveOverScreen()
        {
            if (waveOverScreen != null)
                waveOverScreen.SetActive(true);
        }

        private bool IsCombatStage(Stage stage)
        {
            return stage == Stage.Wave1 || stage == Stage.Wave2 || stage == Stage.Wave3;
        }

        private int GetEnemyCountForStage(Stage stage)
        {
            if (stage == Stage.Wave1) return wave1EnemyCount;
            if (stage == Stage.Wave2) return wave2EnemyCount;
            if (stage == Stage.Wave3) return wave3EnemyCount;
            return 0;
        }

        private string GetStageLabel()
        {
            if (currentStage == Stage.Tutorial) return "Tutorial";
            if (currentStage == Stage.Wave1) return "Wave 1";
            if (currentStage == Stage.Wave2) return "Wave 2";
            if (currentStage == Stage.Wave3) return "Wave 3";
            return "Completed";
        }

        private int GetCurrentWaveNumber()
        {
            if (currentStage == Stage.Wave1) return 1;
            if (currentStage == Stage.Wave2) return 2;
            if (currentStage == Stage.Wave3) return 3;
            return 0;
        }

        private void ResetWaveRuntimeState()
        {
            enemiesKilled = 0;
            enemiesSpawnedSoFar = 0;
            enemiesRemainingToSpawn = 0;
            currentPendingIndex = 0;
            pendingEnemiesToSpawn.Clear();
            aliveEnemies.Clear();
        }

        private void StartWaveSpawning()
        {
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);

            spawnCoroutine = StartCoroutine(SpawnWaveIncrementally());
        }

        private IEnumerator SpawnWaveIncrementally()
        {
            isSpawningWave = true;

            aliveEnemies.Clear();
            enemiesSpawnedSoFar = 0;
            currentPendingIndex = 0;

            BuildWaveEnemyList();

            if (pendingEnemiesToSpawn.Count == 0)
            {
                Debug.LogWarning("No enemies to spawn.");
                isSpawningWave = false;
                yield break;
            }

            Debug.Log($"Wave {GetCurrentWaveNumber()}: Starting incremental spawn. Total enemies: {currentWaveEnemyCount}");

            while (currentPendingIndex < pendingEnemiesToSpawn.Count)
            {
                int batchSize = Random.Range(minSpawnBatchSize, maxSpawnBatchSize + 1);
                batchSize = Mathf.Min(batchSize, pendingEnemiesToSpawn.Count - currentPendingIndex);

                Debug.Log($"Spawning batch of {batchSize} enemies ({currentPendingIndex + 1}-{currentPendingIndex + batchSize} of {pendingEnemiesToSpawn.Count})");

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

                if (currentPendingIndex < pendingEnemiesToSpawn.Count && spawnBatchDelay > 0)
                    yield return new WaitForSeconds(spawnBatchDelay);
            }

            isSpawningWave = false;
            Debug.Log($"Wave {GetCurrentWaveNumber()}: All {enemiesSpawnedSoFar} enemies have been queued for spawning.");
        }

        private void BuildWaveEnemyList()
        {
            pendingEnemiesToSpawn.Clear();

            if (monsterPool == null || monsterPool.Count == 0)
            {
                Debug.LogWarning("Monster pool is empty. No enemies spawned.");
                return;
            }

            var cumulativeChances = BuildCumulativeChances(out float totalChance);

            for (int i = 0; i < currentWaveEnemyCount; i++)
            {
                GameObject selected = SelectMonsterByWeight(cumulativeChances, totalChance);
                if (selected != null)
                    pendingEnemiesToSpawn.Add(selected);
            }

            Debug.Log($"Built wave enemy list with {pendingEnemiesToSpawn.Count} enemies");
        }

        private void SpawnSingleEnemy(GameObject enemyPrefab)
        {
            if (enemyPrefab == null)
                return;

            Vector3 spawnPos = FindValidSpawnLocationAroundSpawnPoint();
            GameObject spawned = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            spawned.name = enemyPrefab.name;

            var enemyCtrl = spawned.GetComponent<EnemyController>();
            if (enemyCtrl != null)
            {
                enemyCtrl.playerTransform = playerTransform;
                enemyCtrl.EnemyWaveController = this;
            }

            aliveEnemies.Add(spawned);
            UpdateUI();

            Debug.Log($"Spawned {enemyPrefab.name} at {spawnPos} ({aliveEnemies.Count} alive, {pendingEnemiesToSpawn.Count - currentPendingIndex} remaining to spawn)");
        }

        public void SpawnNextBatch()
        {
            if (isSpawningWave)
            {
                Debug.Log("Wave is already spawning automatically.");
                return;
            }

            if (currentPendingIndex >= pendingEnemiesToSpawn.Count)
            {
                Debug.Log("No more enemies to spawn.");
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

        private GameObject GetRandomSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
                return null;

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
                Debug.LogWarning($"Could not find valid spawn location after {MAX_ATTEMPTS} attempts");
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

            GameObject selectedSpawnPoint = GetRandomSpawnPoint();

            if (selectedSpawnPoint == null)
                return FindSpawnLocWithRetries(attempts + 1);

            Vector2 randomCircle = Random.insideUnitCircle * spawnPointRadius;
            Vector3 candidate = new Vector3(
                selectedSpawnPoint.transform.position.x + randomCircle.x,
                selectedSpawnPoint.transform.position.y,
                selectedSpawnPoint.transform.position.z + randomCircle.y
            );

            if (Physics.Raycast(candidate, Vector3.down, 5f))
                return candidate;

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

            return monsterPool[monsterPool.Count - 1].monsterPrefab;
        }

        private Vector3 FindValidSpawnLocation()
        {
            return FindSpawnLocWithRetries(0);
        }

        private Vector3 FindSpawnLocWithRetries(int attempts)
        {
            const int MAX_ATTEMPTS = 25;

            if (attempts >= MAX_ATTEMPTS)
            {
                Debug.LogWarning($"Could not find valid spawn location after {MAX_ATTEMPTS} attempts");
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

            if (Physics.Raycast(candidate, Vector3.down, 5f) &&
                Vector3.Distance(candidate, transform.position) >= minRange)
            {
                return candidate;
            }

            return FindSpawnLocWithRetries(attempts + 1);
        }

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

        private void UpdateUI()
        {
            if (killAndWaveCounter != null)
            {
                int totalRemaining = (pendingEnemiesToSpawn.Count - currentPendingIndex) + aliveEnemies.Count;
                killAndWaveCounter.text = $"Kills: {enemiesKilled}/{currentWaveEnemyCount}\nStage: {GetStageLabel()}\nRemaining: {totalRemaining}";
            }
        }

        private void ValidateMonsterPool()
        {
            if (monsterPool.Count == 0)
            {
                Debug.LogWarning("Monster pool is empty. Add monsters in the Inspector.");
                return;
            }

            float total = 0f;
            foreach (var m in monsterPool)
            {
                if (m.monsterPrefab == null)
                    Debug.LogWarning($"Monster entry has null prefab at index {monsterPool.IndexOf(m)}");
                total += m.spawnChance;
            }

            if (total <= 0f)
                Debug.LogWarning("Total spawn chance is 0. No monsters will spawn.");
            else if (total != 100f)
                Debug.Log($"Spawn chances sum to {total} (relative weights work fine; no need to equal 100)");
        }

        private void ValidateSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning("No spawn points assigned. Enemies will spawn using fallback method around the wave controller.");
                return;
            }

            int validCount = 0;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                    validCount++;
                else
                    Debug.LogWarning("Spawn point entry is null.");
            }

            if (validCount == 0)
                Debug.LogError("All spawn points are null. Please assign valid GameObjects.");
            else
                Debug.Log($"Found {validCount} valid spawn points");
        }

        public void TriggerGameOver()
        {
            if (isGameOver)
                return;

            isGameOver = true;
            currentStage = Stage.Completed;
            tutorialCompleted = true;

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            isSpawningWave = false;
            ResetWaveRuntimeState();

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

        public bool IsGameComplete()
        {
            return currentStage == Stage.Completed;
        }

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

            Debug.Log("Spawn Distribution Test (1000 samples):");
            foreach (var kvp in counts)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} ({kvp.Value * 100f / samples:F1}%)");
            }
        }

        [ContextMenu("Debug Spawn Points")]
        private void DebugSpawnPoints()
        {
            Debug.Log("=== Spawn Points Debug ===");
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