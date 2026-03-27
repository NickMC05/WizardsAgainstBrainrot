using UnityEngine;

public class FireballTrail : MonoBehaviour
{
    [Header("Trail Renderer")]
    [SerializeField] private float trailWidth = 0.15f;
    [SerializeField] private float trailTime = 0.4f;
    [SerializeField] private Color trailStartColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color trailEndColor = new Color(1f, 0.1f, 0f, 0f);

    [Header("Fire Particles")]
    [SerializeField] private int emissionRate = 40;
    [SerializeField] private float particleLifetime = 0.5f;
    [SerializeField] private float particleSize = 0.08f;
    [SerializeField] private Color particleStartColor = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private Color particleEndColor = new Color(1f, 0.1f, 0f, 0f);

    [Header("Ember Particles")]
    [SerializeField] private int emberRate = 15;
    [SerializeField] private float emberLifetime = 0.8f;
    [SerializeField] private float emberSize = 0.03f;
    [SerializeField] private Color emberColor = new Color(1f, 0.8f, 0.2f, 1f);

    private TrailRenderer trail;
    private ParticleSystem fireParticles;
    private ParticleSystem emberParticles;

    void Awake()
    {
        CreateTrailRenderer();
        CreateFireParticles();
        CreateEmberParticles();
    }

    private Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);

        // Enable transparency
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);   // Alpha
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");

        return mat;
    }

    private void CreateTrailRenderer()
    {
        GameObject trailObj = new GameObject("FireTrail");
        trailObj.transform.SetParent(transform, false);
        trailObj.transform.localPosition = Vector3.zero;

        trail = trailObj.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.02f;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;

        // Gradient for the trail color over its length
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailStartColor, 0f),
                new GradientColorKey(trailEndColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;

        // Width curve: starts full, tapers off
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 1f);
        widthCurve.AddKey(0.5f, 0.6f);
        widthCurve.AddKey(1f, 0f);
        trail.widthCurve = widthCurve;

        trail.material = CreateUnlitMaterial(trailStartColor);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
    }

    private void CreateFireParticles()
    {
        GameObject fireObj = new GameObject("FireParticles");
        fireObj.transform.SetParent(transform, false);
        fireObj.transform.localPosition = Vector3.zero;

        fireParticles = fireObj.AddComponent<ParticleSystem>();
        fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Main module
        var main = fireParticles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.5f, particleSize);
        main.startColor = particleStartColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = -0.1f; // slight upward drift

        // Emission
        var emission = fireParticles.emission;
        emission.rateOverTime = emissionRate;

        // Shape: emit from a small sphere behind the fireball
        var shape = fireParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        // Size over lifetime: shrink
        var sizeOverLifetime = fireParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: fade from start to end color
        var colorOverLifetime = fireParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(particleStartColor, 0f),
                new GradientColorKey(particleEndColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGrad;

        // Renderer
        var renderer = fireObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateUnlitMaterial(particleStartColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        fireParticles.Play();
    }

    private void CreateEmberParticles()
    {
        GameObject emberObj = new GameObject("EmberParticles");
        emberObj.transform.SetParent(transform, false);
        emberObj.transform.localPosition = Vector3.zero;

        emberParticles = emberObj.AddComponent<ParticleSystem>();
        emberParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Main
        var main = emberParticles.main;
        main.startLifetime = emberLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(emberSize * 0.5f, emberSize);
        main.startColor = emberColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.gravityModifier = 0.2f; // embers drift down slightly

        // Emission
        var emission = emberParticles.emission;
        emission.rateOverTime = emberRate;

        // Shape: random directions from a small cone behind
        var shape = emberParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.03f;
        // Point the cone backward relative to the fireball
        shape.rotation = new Vector3(0f, 180f, 0f);

        // Size over lifetime
        var sizeOverLifetime = emberParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.7f, 0.5f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: bright to dim
        var colorOverLifetime = emberParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(emberColor, 0f),
                new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGrad;

        // Renderer
        var renderer = emberObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateUnlitMaterial(emberColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        emberParticles.Play();
    }

    /// <summary>
    /// Call this from Fireball.cs when the fireball explodes.
    /// Detaches the trail and particles so they fade out naturally.
    /// </summary>
    public void DetachAndFade()
    {
        // Stop emitting new particles
        if (fireParticles != null)
        {
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            fireParticles.transform.SetParent(null);
            Destroy(fireParticles.gameObject, particleLifetime + 0.5f);
        }

        if (emberParticles != null)
        {
            emberParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            emberParticles.transform.SetParent(null);
            Destroy(emberParticles.gameObject, emberLifetime + 0.5f);
        }

        // Detach trail so it fades on its own
        if (trail != null)
        {
            trail.transform.SetParent(null);
            trail.autodestruct = true;
        }
    }
}