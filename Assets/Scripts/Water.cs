using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class WaveSpell : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float spawnGracePeriod = 0.15f;
    [SerializeField] private float spawnForwardOffset = 0.3f;
    [SerializeField] private float fixedYPosition = 0.5f;

    [Header("Damage")]
    [SerializeField] private float directDamage = 40f;
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Visual")]
    [SerializeField] private float pulseSpeed = 5f;
    [SerializeField] private float pulseAmount = 0.2f;

    private Rigidbody rb;
    private SphereCollider col;
    private FireballTrail trail;
    private float spawnTime;
    private Vector3 launchDirection;
    private bool directionSet = false;

    private Dictionary<EnemyController, float> lastHitTimes = new Dictionary<EnemyController, float>();
    private Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false; // Make sure it's not kinematic
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<SphereCollider>();
        col.isTrigger = false;

        trail = GetComponent<FireballTrail>();
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Call this right after Instantiate to set the launch direction.
    /// </summary>
    public void SetDirection(Vector3 direction)
    {
        launchDirection = direction.normalized;
        launchDirection.y = 0; // Zero out Y component for horizontal movement
        if (launchDirection.magnitude < 0.01f)
        {
            // If direction was purely vertical, default to forward
            launchDirection = Vector3.forward;
        }
        else
        {
            launchDirection = launchDirection.normalized;
        }
        directionSet = true;
    }

    void Start()
    {
        spawnTime = Time.time;

        // If no direction was set externally, use the forward direction of the spawner/wand
        if (!directionSet)
        {
            launchDirection = transform.forward;
            launchDirection.y = 0;
            if (launchDirection.magnitude < 0.01f)
            {
                launchDirection = Vector3.forward;
            }
            else
            {
                launchDirection = launchDirection.normalized;
            }
        }

        // Push the wave out so it doesn't clip the wand
        Vector3 spawnPos = transform.position + launchDirection * spawnForwardOffset;
        spawnPos.y = fixedYPosition;
        transform.position = spawnPos;

        // Set the rotation to face the direction of movement
        if (launchDirection != Vector3.zero)
        {
            transform.forward = launchDirection;
        }

        // Set velocity (horizontal only)
        Vector3 velocity = launchDirection * speed;
        velocity.y = 0;
        rb.linearVelocity = velocity;

        // Double-check velocity was set
        Debug.Log("Wave spell velocity set to: " + rb.linearVelocity + " speed: " + speed);

        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        // Keep velocity constant (in case something tries to change it)
        if (!hasExploded && rb != null)
        {
            Vector3 velocity = launchDirection * speed;
            velocity.y = 0;
            rb.linearVelocity = velocity;
        }

        // Keep Y position fixed
        Vector3 pos = transform.position;
        pos.y = fixedYPosition;
        transform.position = pos;
    }

    void Update()
    {
        // Visual pulse effect
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * pulse;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Don't explode or destroy on collision
        if (Time.time - spawnTime < spawnGracePeriod) return;

        EnemyController hitEnemy = collision.gameObject.GetComponent<EnemyController>();

        if (hitEnemy != null)
        {
            // Check if enemy is on cooldown
            if (lastHitTimes.ContainsKey(hitEnemy))
            {
                if (Time.time - lastHitTimes[hitEnemy] < hitCooldown)
                    return;
                lastHitTimes[hitEnemy] = Time.time;
            }
            else
            {
                lastHitTimes.Add(hitEnemy, Time.time);
            }

            hitEnemy.TakeDamage(directDamage);
            Debug.Log("Wave spell hit enemy: " + hitEnemy.name + " for " + directDamage + " damage");
        }
    }

    private bool hasExploded = false; // Dummy variable for FixedUpdate check

    void OnDrawGizmosSelected()
    {
        // Visualize the fixed Y position in editor
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        center.y = fixedYPosition;
        Gizmos.DrawWireSphere(center, 0.5f);

        // Visualize launch direction if set
        if (directionSet && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, launchDirection * 2f);
        }
    }
}