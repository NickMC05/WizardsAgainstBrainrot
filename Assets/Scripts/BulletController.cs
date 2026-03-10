using UnityEngine;

public class BulletController : MonoBehaviour
{
    // Damage value for the bullet
    public float damage = 20f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the bullet hit an enemy
        if (other.CompareTag("Enemy"))
        {
            // Get the EnemyController component from the enemy
            EnemyController enemy = other.GetComponent<EnemyController>();

            if (enemy != null)
            {
                // Apply damage to the enemy
                enemy.TakeDamage(damage);
            }

            // Destroy the bullet after hitting
            Destroy(gameObject);
        }
    }
}