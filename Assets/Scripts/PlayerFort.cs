using UnityEngine;

public class PlayerFort : MonoBehaviour
{
    [SerializeField] private EnemyWaveScript enemyWaveScript;

    private void OnTriggerEnter(Collider other)
    {
        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null)
            return;

        if (enemyWaveScript != null)
            enemyWaveScript.TriggerGameOver();
        else
            Debug.LogWarning("PlayerFort has no EnemyWaveScript reference assigned.");
    }
}