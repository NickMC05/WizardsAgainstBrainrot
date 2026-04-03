using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
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

    [Header("Foam Particles")]
    [SerializeField] private int foamEmissionRate = 60;
    [SerializeField] private float foamLifetime = 0.6f;
    [SerializeField] private float foamParticleSize = 0.06f;
    [SerializeField] private Color foamColorStart = new Color(0.85f, 0.95f, 1f, 0.9f);
    [SerializeField] private Color foamColorEnd = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float foamSpreadWidth = 0.5f;
    [SerializeField] private float foamSpreadHeight = 0.2f;

    [Header("Spray Particles")]
    [SerializeField] private int sprayEmissionRate = 25;
    [SerializeField] private float sprayLifetime = 0.5f;
    [SerializeField] private float sprayParticleSize = 0.03f;
    [SerializeField] private Color sprayColor = new Color(0.9f, 0.97f, 1f, 0.7f);
    [SerializeField] private float sprayUpwardSpeed = 1.5f;

    [Header("Mist Particles")]
    [SerializeField] private int mistEmissionRate = 15;
    [SerializeField] private float mistLifetime = 1.0f;
    [SerializeField] private float mistParticleSize = 0.15f;
    [SerializeField] private Color mistColor = new Color(0.8f, 0.92f, 1f, 0.3f);

    private Rigidbody rb;
    private BoxCollider col;
    private float spawnTime;
    private Vector3 launchDirection;
    private bool directionSet = false;

    private Dictionary<EnemyController, float> lastHitTimes = new Dictionary<EnemyController, float>();
    private Vector3 originalScale;

    private ParticleSystem foamParticles;
    private ParticleSystem sprayParticles;
    private ParticleSystem mistParticles;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Lock rotation on X and Z axes
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        col = GetComponent<BoxCollider>();
        col.isTrigger = true;

        originalScale = transform.localScale;

        CreateFoamParticles();
        CreateSprayParticles();
        CreateMistParticles();
    }

    /// <summary>
    /// Call this right after Instantiate to set the launch direction.
    /// </summary>
    public void SetDirection(Vector3 direction)
    {
        launchDirection = direction.normalized;
        launchDirection.y = 0;
        if (launchDirection.magnitude < 0.01f)
        {
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

        Vector3 spawnPos = transform.position + launchDirection * spawnForwardOffset;
        spawnPos.y = fixedYPosition;
        transform.position = spawnPos;

        if (launchDirection != Vector3.zero)
        {
            // Only rotate around Y axis
            Quaternion targetRot = Quaternion.LookRotation(launchDirection, Vector3.up);
            transform.rotation = Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f);
        }

        Vector3 velocity = launchDirection * speed;
        velocity.y = 0;
        rb.linearVelocity = velocity;

        Debug.Log("Wave spell launched direction: " + launchDirection + " at fixed Y: " + fixedYPosition);

        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector3 velocity = launchDirection * speed;
            velocity.y = 0;
            rb.linearVelocity = velocity;
        }

        Vector3 pos = transform.position;
        pos.y = fixedYPosition;
        transform.position = pos;

        // Enforce flat rotation every physics step
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }

    void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * pulse;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time - spawnTime < spawnGracePeriod) return;

        EnemyController hitEnemy = other.GetComponent<EnemyController>();

        if (hitEnemy != null)
        {
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

    // ──────────────────────────────────────────────
    //  Particle creation
    // ──────────────────────────────────────────────

    private Material CreateParticleMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");

        return mat;
    }

    private void CreateFoamParticles()
    {
        GameObject obj = new GameObject("FoamParticles");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.zero;

        foamParticles = obj.AddComponent<ParticleSystem>();
        foamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = foamParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(foamLifetime * 0.5f, foamLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(foamParticleSize * 0.5f, foamParticleSize);
        main.startColor = foamColorStart;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.gravityModifier = -0.05f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = foamParticles.emission;
        emission.rateOverTime = foamEmissionRate;

        // Emit from a box shape matching the wave's front face
        var shape = foamParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(foamSpreadWidth, foamSpreadHeight, 0.05f);
        shape.position = new Vector3(0f, 0f, 0f);

        // Size over lifetime: grow slightly then shrink
        var sizeOverLifetime = foamParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: white/blue foam fading out
        var colorOverLifetime = foamParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(foamColorStart, 0f),
                new GradientColorKey(new Color(1f, 1f, 1f), 0.3f),
                new GradientColorKey(foamColorEnd, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.7f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Rotation over lifetime for a tumbling foam look
        var rotOverLifetime = foamParticles.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

        // Noise for organic foam movement
        var noise = foamParticles.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 3f;
        noise.scrollSpeed = 1f;
        noise.octaveCount = 2;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(foamColorStart);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        foamParticles.Play();
    }

    private void CreateSprayParticles()
    {
        GameObject obj = new GameObject("SprayParticles");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        sprayParticles = obj.AddComponent<ParticleSystem>();
        sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = sprayParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sprayLifetime * 0.4f, sprayLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sprayUpwardSpeed * 0.5f, sprayUpwardSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(sprayParticleSize * 0.3f, sprayParticleSize);
        main.startColor = sprayColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;
        main.gravityModifier = 0.4f; // spray arcs up then falls

        var emission = sprayParticles.emission;
        emission.rateOverTime = sprayEmissionRate;

        // Emit from a line across the top of the wave
        var shape = sprayParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(foamSpreadWidth * 0.8f, 0.02f, 0.02f);
        shape.rotation = new Vector3(-30f, 0f, 0f); // angled upward and forward

        // Size over lifetime
        var sizeOverLifetime = sprayParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 0.6f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime
        var colorOverLifetime = sprayParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(sprayColor, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(sprayColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0.4f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(sprayColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        sprayParticles.Play();
    }

    private void CreateMistParticles()
    {
        GameObject obj = new GameObject("MistParticles");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 0f, -0.2f); // behind the wave

        mistParticles = obj.AddComponent<ParticleSystem>();
        mistParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = mistParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(mistLifetime * 0.6f, mistLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(mistParticleSize * 0.5f, mistParticleSize);
        main.startColor = mistColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.gravityModifier = -0.02f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = mistParticles.emission;
        emission.rateOverTime = mistEmissionRate;

        var shape = mistParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(foamSpreadWidth * 1.2f, foamSpreadHeight * 0.5f, 0.1f);

        // Size over lifetime: grow then fade
        var sizeOverLifetime = mistParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0.8f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime
        var colorOverLifetime = mistParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(mistColor, 0f),
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.3f, 0f),
                new GradientAlphaKey(0.15f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Noise for drifting mist
        var noise = mistParticles.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 1.5f;
        noise.scrollSpeed = 0.5f;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(mistColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        mistParticles.Play();
    }

    void OnDestroy()
    {
        // Detach particles so they fade out naturally when the wave is destroyed
        DetachParticleSystem(foamParticles, foamLifetime);
        DetachParticleSystem(sprayParticles, sprayLifetime);
        DetachParticleSystem(mistParticles, mistLifetime);
    }

    private void DetachParticleSystem(ParticleSystem ps, float cleanupDelay)
    {
        if (ps != null && ps.gameObject != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ps.transform.SetParent(null);
            Destroy(ps.gameObject, cleanupDelay + 0.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        center.y = fixedYPosition;
        Gizmos.DrawWireSphere(center, 0.5f);

        if (directionSet && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, launchDirection * 2f);
        }
    }
}