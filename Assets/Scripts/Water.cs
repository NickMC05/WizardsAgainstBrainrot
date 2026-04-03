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

    [Header("Foam Spheres (Waterline)")]
    [SerializeField] private int foamEmissionRate = 80;
    [SerializeField] private float foamLifetime = 0.7f;
    [SerializeField] private float foamSphereMinSize = 0.15f;
    [SerializeField] private float foamSphereMaxSize = 0.35f;
    [SerializeField] private Color foamColorStart = new Color(0.9f, 0.97f, 1f, 0.95f);
    [SerializeField] private Color foamColorEnd = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float foamSpreadWidth = 0.6f;
    [SerializeField] private float foamSpreadLength = 0.3f;

    [Header("Edge Foam Spheres (Leading & Side Edges)")]
    [SerializeField] private int edgeFoamRate = 50;
    [SerializeField] private float edgeFoamLifetime = 0.5f;
    [SerializeField] private float edgeFoamMinSize = 0.1f;
    [SerializeField] private float edgeFoamMaxSize = 0.25f;
    [SerializeField] private Color edgeFoamColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Spray Droplets")]
    [SerializeField] private int sprayEmissionRate = 30;
    [SerializeField] private float sprayLifetime = 0.5f;
    [SerializeField] private float spraySphereMinSize = 0.06f;
    [SerializeField] private float spraySphereMaxSize = 0.12f;
    [SerializeField] private Color sprayColor = new Color(0.9f, 0.97f, 1f, 0.7f);
    [SerializeField] private float sprayUpwardSpeed = 2f;

    [Header("Mist")]
    [SerializeField] private int mistEmissionRate = 15;
    [SerializeField] private float mistLifetime = 1.0f;
    [SerializeField] private float mistParticleSize = 0.15f;
    [SerializeField] private Color mistColor = new Color(0.8f, 0.92f, 1f, 0.25f);

    private Rigidbody rb;
    private BoxCollider col;
    private float spawnTime;
    private Vector3 launchDirection;
    private bool directionSet = false;

    private Dictionary<EnemyController, float> lastHitTimes = new Dictionary<EnemyController, float>();
    private Vector3 originalScale;

    private ParticleSystem foamParticles;
    private ParticleSystem edgeFoamParticles;
    private ParticleSystem sprayParticles;
    private ParticleSystem mistParticles;

    private Mesh sphereMesh;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        col = GetComponent<BoxCollider>();
        col.isTrigger = true;

        originalScale = transform.localScale;

        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempSphere);

        CreateFoamParticles();
        CreateEdgeFoamParticles();
        CreateSprayParticles();
        CreateMistParticles();
    }

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
    //  Shared particle material
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

    // ──────────────────────────────────────────────
    //  Main foam: sphere meshes along the waterline
    // ──────────────────────────────────────────────

    private void CreateFoamParticles()
    {
        GameObject obj = new GameObject("FoamSpheres");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.zero;

        foamParticles = obj.AddComponent<ParticleSystem>();
        foamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = foamParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(foamLifetime * 0.6f, foamLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        // Uniform size so they stay perfectly spherical
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(foamSphereMinSize, foamSphereMaxSize);
        main.startColor = foamColorStart;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.gravityModifier = 0.3f;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = foamParticles.emission;
        emission.rateOverTime = foamEmissionRate;

        var shape = foamParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(foamSpreadWidth, 0.04f, foamSpreadLength);
        shape.position = Vector3.zero;

        var sol = foamParticles.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colModule = foamParticles.colorOverLifetime;
        colModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(foamColorStart, 0f),
                new GradientColorKey(foamColorEnd, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.8f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colModule.color = grad;

        var noise = foamParticles.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 2f;
        noise.scrollSpeed = 1f;
        noise.damping = true;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = sphereMesh;
        renderer.material = CreateParticleMaterial(foamColorStart);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        foamParticles.Play();
    }

    // ──────────────────────────────────────────────
    //  Edge foam: sphere meshes at the leading edge
    // ──────────────────────────────────────────────

    private void CreateEdgeFoamParticles()
    {
        GameObject obj = new GameObject("EdgeFoamSpheres");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 0f, foamSpreadLength * 0.5f);

        edgeFoamParticles = obj.AddComponent<ParticleSystem>();
        edgeFoamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = edgeFoamParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(edgeFoamLifetime * 0.5f, edgeFoamLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(edgeFoamMinSize, edgeFoamMaxSize);
        main.startColor = edgeFoamColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = 0.5f;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = edgeFoamParticles.emission;
        emission.rateOverTime = edgeFoamRate;

        var shape = edgeFoamParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(foamSpreadWidth * 1.1f, 0.03f, 0.05f);

        var vel = edgeFoamParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);

        var sol = edgeFoamParticles.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 0.7f);
        sizeCurve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colModule = edgeFoamParticles.colorOverLifetime;
        colModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(edgeFoamColor, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colModule.color = grad;

        var noise = edgeFoamParticles.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 3f;
        noise.damping = true;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = sphereMesh;
        renderer.material = CreateParticleMaterial(edgeFoamColor);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        edgeFoamParticles.Play();
    }

    // ──────────────────────────────────────────────
    //  Spray: small sphere droplets shooting upward
    // ──────────────────────────────────────────────

    private void CreateSprayParticles()
    {
        GameObject obj = new GameObject("SprayDroplets");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 0.05f, foamSpreadLength * 0.3f);

        sprayParticles = obj.AddComponent<ParticleSystem>();
        sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = sprayParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sprayLifetime * 0.5f, sprayLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sprayUpwardSpeed * 0.5f, sprayUpwardSpeed);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(spraySphereMinSize, spraySphereMaxSize);
        main.startColor = sprayColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;
        main.gravityModifier = 1.2f;

        var emission = sprayParticles.emission;
        emission.rateOverTime = sprayEmissionRate;

        var shape = sprayParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(foamSpreadWidth * 0.8f, 0.01f, 0.08f);
        shape.rotation = new Vector3(-30f, 0f, 0f);

        var sol = sprayParticles.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.3f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colModule = sprayParticles.colorOverLifetime;
        colModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(sprayColor, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0.3f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colModule.color = grad;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = sphereMesh;
        renderer.material = CreateParticleMaterial(sprayColor);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        sprayParticles.Play();
    }

    // ──────────────────────────────────────────────
    //  Mist: soft billboard particles trailing behind
    // ──────────────────────────────────────────────

    private void CreateMistParticles()
    {
        GameObject obj = new GameObject("MistParticles");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 0.02f, -foamSpreadLength * 0.3f);

        mistParticles = obj.AddComponent<ParticleSystem>();
        mistParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = mistParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(mistLifetime * 0.7f, mistLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(mistParticleSize * 0.6f, mistParticleSize);
        main.startColor = mistColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;
        main.gravityModifier = -0.05f;

        var emission = mistParticles.emission;
        emission.rateOverTime = mistEmissionRate;

        var shape = mistParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(foamSpreadWidth * 0.9f, 0.05f, 0.15f);

        var sol = mistParticles.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 1.3f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colModule = mistParticles.colorOverLifetime;
        colModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(mistColor, 0f),
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.25f, 0.2f),
                new GradientAlphaKey(0.15f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colModule.color = grad;

        var noise = mistParticles.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 1f;
        noise.scrollSpeed = 0.5f;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial(mistColor);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        mistParticles.Play();
    }
}