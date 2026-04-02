using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyWaveScript : MonoBehaviour
{
    [System.Serializable]
    public class WaveContent
    {
        [SerializeField] GameObject[] monsterSpawn;
        public GameObject[] GetMonsterSpawnList() => monsterSpawn;
    }
    [SerializeField] WaveContent[] waves;
    int currentWave = 0;
    public float spawnRange = 20;
    public float minRange = 5;
    public int enemiesKilled = 0;
    public Transform playerTransform;
    public TMP_Text killAndWaveCounter;
    public GameObject waveOverScreen;

    // Public list of currently alive enemies (hidden in inspector by default).
    [HideInInspector]
    public List<GameObject> aliveEnemies = new List<GameObject>();

    void Start()
    {
        SpawnWave();
        updateUI();
    }

    void Update()
    {
        // Safety: ensure currentWave is valid
        if (waves == null || waves.Length == 0) return;
        if (currentWave < 0 || currentWave >= waves.Length) return;

        // If no alive enemies OR the killed count matches wave size, show wave over.
        if (aliveEnemies.Count == 0 && enemiesKilled >= waves[currentWave].GetMonsterSpawnList().Length)
        {
            enemiesKilled = 0;
            Debug.Log("wave cleared!");
            waveOverScreen.SetActive(true);
        }
    }

    public void NextWave()
    {
        if (currentWave < waves.Length - 1)
        {
            waveOverScreen.SetActive(false);
            currentWave++;
            updateUI();
            SpawnWave();
        }
    }

    void SpawnWave()
    {
        // Clear any leftover references before spawning
        aliveEnemies.Clear();

        var list = waves[currentWave].GetMonsterSpawnList();
        if (list == null) return;

        foreach (var monster in list)
        {
            var spawnedMon = Instantiate(monster, FindSpawnLoc(), Quaternion.identity);
            // assign references on enemy
            var enemyController = spawnedMon.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.playerTransform = playerTransform;
                enemyController.EnemyWaveController = this; // assign script reference
            }

            // Keep track of alive enemies
            aliveEnemies.Add(spawnedMon);
        }

        updateUI();
    }

    Vector3 FindSpawnLoc()
    {
        Vector3 SpawnPos;

        float xLoc = Random.Range(-spawnRange, spawnRange) + transform.position.x;
        float yLoc = transform.position.y;
        float zLoc = Random.Range(-spawnRange, spawnRange) + transform.position.z;

        SpawnPos = new Vector3(xLoc, yLoc, zLoc);

        if (Physics.Raycast(SpawnPos, Vector3.down, 5) && SpawnPos.magnitude > minRange)
        {
            return SpawnPos;
        }
        else
        {
            return FindSpawnLoc();
        }
    }

    // Call this when an enemy dies. This removes the enemy from aliveEnemies and updates counters/UI.
    public void RemoveEnemyReference(GameObject enemy)
    {
        if (enemy == null) return;

        if (aliveEnemies.Contains(enemy))
        {
            aliveEnemies.Remove(enemy);
            enemiesKilled++;
            updateUI();
            Debug.Log($"[EnemyWaveScript] enemiesKilled incremented to {enemiesKilled}. Current aliveEnemies count: {aliveEnemies.Count}");
            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                var e = aliveEnemies[i];
                if (e == null)
                {
                    Debug.Log($"[EnemyWaveScript] aliveEnemies[{i}] = null (will be removed)");
                }
                else
                {
                    Debug.Log($"[EnemyWaveScript] aliveEnemies[{i}] = {e.name}");
                }
            }
        }
    }

        void updateUI()
    {
        killAndWaveCounter.text = $"Kills: {enemiesKilled}\nWave: {currentWave}";
    }
}