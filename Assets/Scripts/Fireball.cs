using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Fireball : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float spawnGracePeriod = 0.15f;

    [Tooltip("Local axis to launch along. Try (0,1,0) or (0,0,1) until it fires outward from the wand.")]
    [SerializeField] private Vector3 localLaunchDirection = Vector3.forward;

    [Header("Damage")]
    [SerializeField] private float directDamage = 40f;
    [SerializeField] private float explosionRadius = 0.6f;
    [SerializeField] private float explosionDamage = 20f;

    [Header("Explosion Visual")]
    [SerializeField] private float explosionScaleMultiplier = 3f;
    [SerializeField] private float explosionDuration = 0.3f;

    private Rigidbody rb;
    private SphereCollider col;
    private bool hasExploded;
    private float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<SphereCollider>();
        col.isTrigger = false;
    }

    void Start()
    {
        spawnTime = Time.time;

        // Convert the local launch direction to world space
        Vector3 worldDir = transform.TransformDirection(localLaunchDirection.normalized);

        // Offset forward slightly so we don't spawn inside the wand
        transform.position += worldDir * 0.15f;

        // Point the fireball along the launch direction and fire it
        transform.rotation = Quaternion.LookRotation(worldDir);
        rb.linearVelocity = worldDir * speed;

        Debug.Log("Fireball launched direction: " + worldDir + " from " + transform.position);

        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // Ignore collisions during grace period
        if (Time.time - spawnTime < spawnGracePeriod) return;

        string hitName = collision.gameObject.name;
        bool hitGround = hitName == "Ground plane" || hitName.Contains("Ground");
        bool hitEnemy = collision.gameObject.GetComponent<EnemyController>() != null;

        Debug.Log("Fireball collided with: " + hitName);

        if (!hitGround && !hitEnemy) return;

        if (hitEnemy)
        {
            EnemyController directHit = collision.gameObject.GetComponent<EnemyController>();
            directHit.TakeDamage(directDamage);
        }

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        col.enabled = false;

        // Splash damage
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        HashSet<EnemyController> damaged = new HashSet<EnemyController>();

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null && !damaged.Contains(enemy))
            {
                damaged.Add(enemy);
                enemy.TakeDamage(explosionDamage);
            }
        }

        Debug.Log("Fireball exploded at " + transform.position);

        StartCoroutine(ExplosionVisual());
    }

    private IEnumerator ExplosionVisual()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * explosionScaleMultiplier;
        float elapsed = 0f;

        while (elapsed < explosionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / explosionDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}