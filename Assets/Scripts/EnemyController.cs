using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform playerTransform; // Reference to the player's Transform
    public float maxHealth = 100f; // Maximum health of the enemy
    public float health; // Current health of the enemy
    public float moveSpeed = 5f; // Speed at which the enemy moves
    public float rotationSpeed = 5f; // Speed of rotation to face the player
                                     // Reference to the wave controller script (set when spawned)
    public EnemyWaveScript EnemyWaveController;

    void Start()
    {
        // Initialize health
        health = maxHealth;
    }

    void Update()
    {
        MoveTowardsPlayer();
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
        Debug.Log("Enemy hit! Remaining health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died!");

        // Inform the wave controller to remove this enemy from its alive list and update kills/UI.
        if (EnemyWaveController != null)
        {
            EnemyWaveController.RemoveEnemyReference(gameObject);
        }
        else
        {
            // Fallback to previous behavior if the reference wasn't set
            var waveObj = EnemyWaveController as UnityEngine.Object; // no-op, keeps intent clear
            var waveScript = FindObjectOfType<EnemyWaveScript>();
            if (waveScript != null)
            {
                waveScript.RemoveEnemyReference(gameObject);
            }
        }

        Destroy(gameObject);
    }

}