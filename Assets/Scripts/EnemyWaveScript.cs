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

        public GameObject[] GetMonsterSpawnList()
        {
            return monsterSpawn;
        }
    }

    [SerializeField] WaveContent[] waves;
    int currentWave = 0;
    public float spawnRange = 20;
    public float minRange = 5;
    public int enemiesKilled = 0;
    public Transform playerTransform;
    public TMP_Text killAndWaveCounter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnWave();
    }

    // Update is called once per frame
    void Update()
    {
        if(enemiesKilled >= waves[currentWave].GetMonsterSpawnList().Length)
        {
            enemiesKilled = 0;
            Debug.Log("wave cleared!");
            if(currentWave < waves.Length-1)
            {
                currentWave++;
                updateUI();
                SpawnWave();
            }
        }
    }

    void SpawnWave()
    {
        foreach (var monster in waves[currentWave].GetMonsterSpawnList())
        {
            var spawnedMon = Instantiate(monster, FindSpawnLoc(), Quaternion.identity);
            spawnedMon.GetComponent<EnemyController>().playerTransform = playerTransform;
            spawnedMon.GetComponent<EnemyController>().EnemyWaveController = this.gameObject;
        }
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

    public void enemyKilled()
    {
        enemiesKilled++;
        updateUI();
    }

    void updateUI()
    {
        killAndWaveCounter.text = $"Kills: {enemiesKilled}\nWave: {currentWave}";
    }
}
