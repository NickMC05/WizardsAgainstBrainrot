using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class LightningProjectileScript : MonoBehaviour
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

    [Header("Bounce")]
    [SerializeField] private float bounceRange = 8f;

    [Header("Bounce FX")]
    [SerializeField] private bool showBounceArc = true;
    [SerializeField] private bool allowSingleBounce = true;

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

    private bool hasBounced;

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

    public void SetDirection(Vector3 direction)
    {
        launchDirection = direction.normalized;
        directionSet = true;
    }

    void Start()
    {
        spawnTime = Time.time;

        if (!directionSet)
        {
            launchDirection = -transform.up;
        }

        transform.position += launchDirection * spawnForwardOffset;
        rb.linearVelocity = launchDirection * speed;

        Debug.Log("Fireball launched direction: " + launchDirection);
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        audioMgr.PlaySpellExplodeSFX();

        if (hasExploded) return;
        if (Time.time - spawnTime < spawnGracePeriod) return;

        string hitName = collision.gameObject.name;
        EnemyController hitEnemy = collision.gameObject.GetComponent<EnemyController>();
        bool isEnemyHit = hitEnemy != null;
        bool hitGround = hitName == "Ground plane" || hitName.Contains("Ground");

        Debug.Log("Fireball hit: " + hitName);

        if (!hitGround && !isEnemyHit) return;

        if (isEnemyHit)
        {
            hitEnemy.TakeDamage(directDamage);

            if (allowSingleBounce && !hasBounced)
            {
                bool bounced = TryBounceToNearestEnemy(hitEnemy);
                if (bounced) return;

                Debug.Log("No valid bounce target in range. Despawning projectile.");
                Destroy(gameObject);
                return;
            }

            Explode();
            return;
        }

        Explode();
    }

    private bool TryBounceToNearestEnemy(EnemyController currentEnemy)
    {
        if (currentEnemy == null) return false;

        EnemyWaveScript wave = currentEnemy.EnemyWaveController;
        if (wave == null) return false;

        EnemyController nearest = FindNearestAliveEnemy(wave, currentEnemy);
        if (nearest == null) return false;

        Vector3 from = currentEnemy.transform.position;
        Vector3 to = nearest.transform.position;
        Vector3 dir = (to - from).normalized;

        hasBounced = true;

        if (showBounceArc)
        {
            LightningArcTrail.Spawn(from, to);
        }

        transform.position = from + dir * spawnForwardOffset;
        transform.forward = dir;
        rb.isKinematic = false;
        col.enabled = true;
        rb.linearVelocity = dir * speed;
        spawnTime = Time.time;

        Debug.Log("Bounced to: " + nearest.name);
        return true;
    }

    private EnemyController FindNearestAliveEnemy(EnemyWaveScript wave, EnemyController currentEnemy)
    {
        if (wave == null || wave.aliveEnemies == null || wave.aliveEnemies.Count == 0) return null;

        EnemyController nearest = null;
        float bestSqr = bounceRange * bounceRange;
        Vector3 origin = currentEnemy.transform.position;

        for (int i = wave.aliveEnemies.Count - 1; i >= 0; i--)
        {
            GameObject go = wave.aliveEnemies[i];
            if (go == null)
            {
                wave.aliveEnemies.RemoveAt(i);
                continue;
            }

            EnemyController enemy = go.GetComponent<EnemyController>();
            if (enemy == null) continue;
            if (enemy == currentEnemy) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.health <= 0f) continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                nearest = enemy;
            }
        }

        return nearest;
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