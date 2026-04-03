using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class LightningProjectileScript : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private float projectileScale = 0.15f;

    [Header("Range")]
    [SerializeField] private float initialRange = 15f;
    [SerializeField] private float bounceRange = 8f;

    [Header("Damage")]
    [SerializeField] private float directDamage = 40f;
    [SerializeField] private float bounceDamage = 20f;

    [Header("Bounce")]
    [SerializeField] private int maxBounces = 1;
    [SerializeField] private float bounceDelay = 0.08f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 1.5f;

    private Rigidbody rb;
    private Vector3 launchDirection;
    private bool directionSet = false;

    public void SetDirection(Vector3 direction)
    {
        launchDirection = direction.normalized;
        directionSet = true;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SphereCollider col = GetComponent<SphereCollider>();
        col.enabled = false;

        FireballTrail trail = GetComponent<FireballTrail>();
        if (trail != null) trail.DetachAndFade();
    }

    void Start()
    {
        if (!directionSet)
            launchDirection = transform.forward;

        // Stay exactly where we were spawned (the wand tip)
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localScale *= projectileScale;

        StartCoroutine(ChainLightning());
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        // Force the projectile to never move
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private IEnumerator ChainLightning()
    {
        Vector3 origin = transform.position;

        EnemyController firstTarget = FindFirstTarget(origin, launchDirection, initialRange);

        if (firstTarget == null)
        {
            Debug.Log("Lightning: No target in range");
            yield break;
        }

        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        if (audioMgr != null) audioMgr.PlaySpellExplodeSFX();

        LightningArcTrail.Spawn(origin, firstTarget.transform.position);
        firstTarget.TakeDamage(directDamage);
        Debug.Log("Lightning hit: " + firstTarget.name);

        EnemyController current = firstTarget;
        HashSet<EnemyController> alreadyHit = new HashSet<EnemyController> { firstTarget };

        for (int i = 0; i < maxBounces; i++)
        {
            yield return new WaitForSeconds(bounceDelay);

            EnemyController next = FindNearestAliveEnemy(current, bounceRange, alreadyHit);
            if (next == null)
            {
                Debug.Log("Lightning: No bounce target found");
                break;
            }

            if (audioMgr != null) audioMgr.PlaySpellExplodeSFX();

            LightningArcTrail.Spawn(current.transform.position, next.transform.position);
            next.TakeDamage(bounceDamage);
            Debug.Log("Lightning bounced to: " + next.name);

            alreadyHit.Add(next);
            current = next;
        }
    }

    private EnemyController FindFirstTarget(Vector3 origin, Vector3 direction, float range)
    {
        Collider[] hits = Physics.OverlapSphere(origin, range);
        EnemyController best = null;
        float bestScore = float.MaxValue;

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.health <= 0f) continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            float dist = toEnemy.magnitude;
            if (dist < 0.01f) continue;

            float dot = Vector3.Dot(direction, toEnemy.normalized);
            if (dot < 0f) continue;

            float score = dist * (1f - dot * 0.5f);
            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    private EnemyController FindNearestAliveEnemy(EnemyController current, float range, HashSet<EnemyController> exclude)
    {
        EnemyWaveScript wave = current.EnemyWaveController;

        if (wave != null && wave.aliveEnemies != null)
        {
            EnemyController nearest = null;
            float bestSqr = range * range;
            Vector3 origin = current.transform.position;

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
                if (exclude.Contains(enemy)) continue;
                if (!enemy.gameObject.activeInHierarchy) continue;
                if (enemy.health <= 0f) continue;

                float sqr = (enemy.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = enemy;
                }
            }

            if (nearest != null) return nearest;
        }

        Collider[] hits = Physics.OverlapSphere(current.transform.position, range);
        EnemyController best = null;
        float bestDist = range * range;

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy == null) continue;
            if (exclude.Contains(enemy)) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.health <= 0f) continue;

            float sqr = (enemy.transform.position - current.transform.position).sqrMagnitude;
            if (sqr < bestDist)
            {
                bestDist = sqr;
                best = enemy;
            }
        }

        return best;
    }
}