using UnityEngine;

public class WandTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform wandTip;

    [Header("Trail Settings")]
    [SerializeField] float startWidth = 0.04f;
    [SerializeField] float endWidth = 0.03f;
    [SerializeField] Color trailCore = new Color(0.8f, 0.3f, 1f, 1f);
    [SerializeField] Color trailEdge = new Color(0.5f, 0f, 0.9f, 1f);

    [Header("Glow Settings")]
    [SerializeField] float glowWidth = 0.12f;
    [SerializeField] Color glowColor = new Color(0.6f, 0.1f, 1f, 0.25f);

    [Header("Particle Settings")]
    [SerializeField] float particleRate = 40f;
    [SerializeField] float particleSize = 0.04f;
    [SerializeField] float particleLifetime = 0.6f;
    [SerializeField] Color particleColor = new Color(0.8f, 0.4f, 1f, 1f);

    TrailRenderer coreTrail;
    TrailRenderer glowTrail;
    ParticleSystem sparkles;
    ParticleSystem ambientGlow;
    Light tipLight;
    bool subscribed;

    void Start()
    {
        if (wandTip == null)
        {
            GameObject tipObj = GameObject.Find("WandTip");
            if (tipObj != null)
                wandTip = tipObj.transform;
        }

        if (wandTip == null)
        {
            Debug.LogError("[WandTrail] No wandTip found. Disabling.");
            enabled = false;
            return;
        }

        CreateCoreTrail();
        CreateGlowTrail();
        CreateSparkles();
        CreateAmbientGlow();
        CreateTipLight();
    }

    void CreateCoreTrail()
    {
        GameObject coreObj = new GameObject("CoreTrail");
        coreObj.transform.SetParent(wandTip, false);
        coreObj.transform.localPosition = Vector3.zero;

        coreTrail = coreObj.AddComponent<TrailRenderer>();
        coreTrail.time = Mathf.Infinity;
        coreTrail.startWidth = startWidth;
        coreTrail.endWidth = endWidth;
        coreTrail.minVertexDistance = 0.01f;
        coreTrail.numCornerVertices = 8;
        coreTrail.numCapVertices = 8;
        coreTrail.autodestruct = false;
        coreTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        coreTrail.receiveShadows = false;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailCore, 0f),
                new GradientColorKey(trailEdge, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.9f, 1f)
            }
        );
        coreTrail.colorGradient = gradient;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        coreTrail.material = mat;

        coreTrail.emitting = false;
        coreTrail.Clear();
    }

    void CreateGlowTrail()
    {
        GameObject glowObj = new GameObject("GlowTrail");
        glowObj.transform.SetParent(wandTip, false);
        glowObj.transform.localPosition = Vector3.zero;

        glowTrail = glowObj.AddComponent<TrailRenderer>();
        glowTrail.time = Mathf.Infinity;
        glowTrail.startWidth = glowWidth;
        glowTrail.endWidth = glowWidth * 0.8f;
        glowTrail.minVertexDistance = 0.01f;
        glowTrail.numCornerVertices = 8;
        glowTrail.numCapVertices = 8;
        glowTrail.autodestruct = false;
        glowTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        glowTrail.receiveShadows = false;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(glowColor, 0f),
                new GradientColorKey(glowColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.3f, 0f),
                new GradientAlphaKey(0.2f, 1f)
            }
        );
        glowTrail.colorGradient = gradient;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        glowTrail.material = mat;

        glowTrail.emitting = false;
        glowTrail.Clear();
    }

    void CreateSparkles()
    {
        GameObject sparkObj = new GameObject("Sparkles");
        sparkObj.transform.SetParent(wandTip, false);
        sparkObj.transform.localPosition = Vector3.zero;

        sparkles = sparkObj.AddComponent<ParticleSystem>();
        var main = sparkles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0.3f;
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;
        main.loop = true;
        main.playOnAwake = false;

        var emission = sparkles.emission;
        emission.rateOverTime = particleRate;

        var shape = sparkles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;

        var sizeOverLifetime = sparkles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = sparkles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(particleColor, 0f),
                new GradientColorKey(new Color(1f, 1f, 1f, 1f), 0.5f),
                new GradientColorKey(particleColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGrad;

        // Particle material
        ParticleSystemRenderer rend = sparkObj.GetComponent<ParticleSystemRenderer>();
        Material pMat = new Material(Shader.Find("Sprites/Default"));
        pMat.color = Color.white;
        rend.material = pMat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;

        sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void CreateAmbientGlow()
    {
        GameObject glowObj = new GameObject("AmbientGlow");
        glowObj.transform.SetParent(wandTip, false);
        glowObj.transform.localPosition = Vector3.zero;

        ambientGlow = glowObj.AddComponent<ParticleSystem>();
        var main = ambientGlow.main;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 0.15f;
        main.startColor = new Color(0.6f, 0.1f, 1f, 0.15f);
        main.maxParticles = 5;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = false;

        var emission = ambientGlow.emission;
        emission.rateOverTime = 10f;

        var shape = ambientGlow.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        var sizeOverLifetime = ambientGlow.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ambientGlow.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.7f, 0.2f, 1f), 0f),
                new GradientColorKey(new Color(0.5f, 0f, 0.8f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.2f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGrad;

        ParticleSystemRenderer rend = glowObj.GetComponent<ParticleSystemRenderer>();
        Material pMat = new Material(Shader.Find("Sprites/Default"));
        pMat.color = Color.white;
        rend.material = pMat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;

        ambientGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void CreateTipLight()
    {
        GameObject lightObj = new GameObject("TipLight");
        lightObj.transform.SetParent(wandTip, false);
        lightObj.transform.localPosition = Vector3.zero;

        tipLight = lightObj.AddComponent<Light>();
        tipLight.type = LightType.Point;
        tipLight.color = new Color(0.7f, 0.2f, 1f);
        tipLight.intensity = 2f;
        tipLight.range = 0.5f;
        tipLight.enabled = false;
    }

    void Update()
    {
        if (!subscribed && SpellManager.Instance != null)
        {
            SpellManager.Instance.OnCastingStarted += HandleStart;
            SpellManager.Instance.OnCastingEnded += HandleEnd;
            subscribed = true;
            Debug.Log("[WandTrail] Subscribed to SpellManager events.");
        }

        // Pulse the tip light while casting
        if (tipLight != null && tipLight.enabled)
        {
            float pulse = 1.5f + Mathf.Sin(Time.time * 5f) * 0.5f;
            tipLight.intensity = pulse;
        }
    }

    void OnDestroy()
    {
        if (subscribed && SpellManager.Instance != null)
        {
            SpellManager.Instance.OnCastingStarted -= HandleStart;
            SpellManager.Instance.OnCastingEnded -= HandleEnd;
        }
    }

    void HandleStart()
    {
        coreTrail.Clear();
        glowTrail.Clear();
        coreTrail.emitting = true;
        glowTrail.emitting = true;
        sparkles.Play();
        ambientGlow.Play();
        tipLight.enabled = true;
        Debug.Log("[WandTrail] Trail ON");
    }

    void HandleEnd()
    {
        coreTrail.emitting = false;
        glowTrail.emitting = false;
        coreTrail.Clear();
        glowTrail.Clear();
        sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ambientGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        tipLight.enabled = false;
        Debug.Log("[WandTrail] Trail OFF");
    }
}