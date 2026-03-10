using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform playerTransform; // Reference to the player's Transform
    public float maxHealth = 100f;    // Maximum health of the enemy
    public float health;               // Current health of the enemy
    public float moveSpeed = 5f;      // Speed at which the enemy moves
    public float rotationSpeed = 5f;   // Speed of rotation to face the player

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
        // Check if the playerTransform is set
        if (playerTransform != null)
        {
            // Calculate the direction to the player
            Vector3 direction = (playerTransform.position - transform.position).normalized;

            // Rotate the enemy to face the player
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);

            // Move the enemy towards the player
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}