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
    private float currentSpeed;

    private Vector3 launchDirection;
    private bool directionSet = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // No drag so it never slows down
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // Freeze rotation so it doesn't tumble
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        col = GetComponent<SphereCollider>();
        // TRIGGER so Unity physics never bounces or deflects us
        col.isTrigger = true;

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

        // If SpellManager already set our velocity before re-enabling
        // this component, skip auto-launch — we are already flying.
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            currentSpeed = rb.linearVelocity.magnitude;
            Destroy(gameObject, lifetime);
            return;
        }

        if (!directionSet)
        {
            launchDirection = -transform.up;
        }

        transform.position += launchDirection * spawnForwardOffset;
        rb.linearVelocity = launchDirection * speed;
        currentSpeed = speed;

        Debug.Log("Fireball launched direction: " + launchDirection);

        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Lock velocity to a constant speed every physics step.
    /// Prevents any drift, slowdown, or directional wobble.
    /// </summary>
    void FixedUpdate()
    {
        if (hasExploded) return;
        if (rb.isKinematic) return;

        // Force constant speed in whatever direction we are going
        Vector3 dir = rb.linearVelocity.normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            rb.linearVelocity = dir * currentSpeed;
        }
    }

    /// <summary>
    /// Trigger-based detection — no physics bounce, no deflection.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;
        if (Time.time - spawnTime < spawnGracePeriod) return;

        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();

        TutorialTarget tutorialTarget = other.GetComponentInParent<TutorialTarget>();
        if (tutorialTarget != null)
        {
            if (audioMgr != null)
            {
                audioMgr.PlaySpellExplodeSFX();
            }

            tutorialTarget.OnFireballHit();
            Explode();
            return;
        }

        bool hitEnemy = other.GetComponent<EnemyController>() != null;
        bool hitGround = other.gameObject.name == "Ground plane"
                      || other.gameObject.name.Contains("Ground");

        if (!hitGround && !hitEnemy) return;

        if (audioMgr != null)
        {
            audioMgr.PlaySpellExplodeSFX();
        }

        if (hitEnemy)
        {
            EnemyController directHit = other.GetComponent<EnemyController>();
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