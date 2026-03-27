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
    [SerializeField] private float spawnForwardOffset = 0.3f;

    [Header("Damage")]
    [SerializeField] private float directDamage = 40f;
    [SerializeField] private float explosionRadius = 0.6f;
    [SerializeField] private float explosionDamage = 20f;

    [Header("Explosion Visual")]
    [SerializeField] private float explosionScaleMultiplier = 2.5f;
    [SerializeField] private float explosionDuration = 0.3f;

    private Rigidbody rb;
    private SphereCollider col;
    private FireballTrail trail;
    private bool hasExploded;
    private float spawnTime;

    private Vector3 launchDirection;
    private bool directionSet = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<SphereCollider>();
        col.isTrigger = false;

        trail = GetComponent<FireballTrail>();
    }

    /// <summary>
    /// Call this right after Instantiate to set the launch direction.
    /// </summary>
    public void SetDirection(Vector3 direction)
    {
        launchDirection = direction.normalized;
        directionSet = true;
    }

    void Start()
    {
        spawnTime = Time.time;

        // If no direction was set externally, try common wand axes
        if (!directionSet)
        {
            // Most wand tips point forward along -Z or the up axis
            // Change this line if your wand tip uses a different axis
            launchDirection = -transform.up;
        }

        // Push the fireball out so it doesn't clip the wand
        transform.position += launchDirection * spawnForwardOffset;

        rb.linearVelocity = launchDirection * speed;

        Debug.Log("Fireball launched direction: " + launchDirection);

        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        if (Time.time - spawnTime < spawnGracePeriod) return;

        string hitName = collision.gameObject.name;
        bool hitGround = hitName == "Ground plane" || hitName.Contains("Ground");
        bool hitEnemy = collision.gameObject.GetComponent<EnemyController>() != null;

        Debug.Log("Fireball hit: " + hitName);

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

        if (trail != null)
        {
            trail.DetachAndFade();
        }

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

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Color explosionTint = new Color(1f, 0.4f, 0f, 1f);
            rend.material.SetColor("_BaseColor", explosionTint);
            rend.material.SetColor("_Color", explosionTint);
        }

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