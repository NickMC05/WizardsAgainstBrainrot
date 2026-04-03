using UnityEngine;
using System.Collections;

public class LightningArcTrail : MonoBehaviour
{
    [Header("Arc Shape")]
    [SerializeField] private int segments = 12;
    [SerializeField] private float jaggedness = 0.35f;

    [Header("Core Line")]
    [SerializeField] private float coreWidth = 0.06f;

    [Header("Glow Line")]
    [SerializeField] private float glowWidth = 0.28f;

    [Header("Color")]
    [SerializeField] private Color coreColor = new Color(1f, 1f, 0.85f, 1f);       // near-white hot center
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.1f, 0.45f);  // electric yellow glow
    [SerializeField] private float emissionIntensity = 4f;

    [Header("Lifetime")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float flickerInterval = 0.04f;

    private LineRenderer coreLine;
    private LineRenderer glowLine;

    public static void Spawn(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("LightningArc");
        go.transform.position = start;
        LightningArcTrail arc = go.AddComponent<LightningArcTrail>();
        arc.Init(start, end);
    }

    private void Init(Vector3 start, Vector3 end)
    {
        // --- Build materials ---
        Material coreMat = CreateArcMaterial(coreColor, emissionIntensity);
        Material glowMat = CreateArcMaterial(glowColor, emissionIntensity * 0.6f);

        // --- Glow (behind) ---
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform);
        glowLine = glowObj.AddComponent<LineRenderer>();
        SetupLine(glowLine, glowMat, glowWidth, -1);

        // --- Core (front) ---
        GameObject coreObj = new GameObject("Core");
        coreObj.transform.SetParent(transform);
        coreLine = coreObj.AddComponent<LineRenderer>();
        SetupLine(coreLine, coreMat, coreWidth, 0);

        // Generate initial arc
        Vector3[] points = GenerateArcPoints(start, end);
        ApplyPoints(points);

        StartCoroutine(FlickerAndDie(start, end));
    }

    private Material CreateArcMaterial(Color baseColor, float intensity)
    {
        // Use the built-in particle additive shader for nice glow blending
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);

        // Try to set to additive blending
        mat.SetFloat("_Mode", 0); // additive if particles shader
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3100;

        // HDR emission color for bloom
        Color hdrColor = baseColor * intensity;
        mat.SetColor("_Color", hdrColor);
        mat.SetColor("_EmissionColor", hdrColor);
        mat.EnableKeyword("_EMISSION");

        // Also set main color for fallback shaders
        if (mat.HasProperty("_TintColor"))
            mat.SetColor("_TintColor", hdrColor);

        return mat;
    }

    private void SetupLine(LineRenderer lr, Material mat, float width, int sortOrder)
    {
        lr.material = mat;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = sortOrder;

        // Width curve: thicker in center, thinner at ends
        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.8f, 1f),
            new Keyframe(1f, 0.4f)
        );
        lr.widthCurve = widthCurve;
        lr.widthMultiplier = width;
    }

    private Vector3[] GenerateArcPoints(Vector3 start, Vector3 end)
    {
        Vector3[] points = new Vector3[segments + 1];
        points[0] = start;
        points[segments] = end;

        Vector3 forward = end - start;
        float length = forward.magnitude;
        Vector3 dir = forward / length;

        // Build a perpendicular basis
        Vector3 up = Vector3.Cross(dir, Vector3.right);
        if (up.sqrMagnitude < 0.01f)
            up = Vector3.Cross(dir, Vector3.up);
        up.Normalize();
        Vector3 right = Vector3.Cross(dir, up).normalized;

        for (int i = 1; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector3 basePos = Vector3.Lerp(start, end, t);

            // Offset fades near endpoints so the arc connects cleanly
            float envelope = Mathf.Sin(t * Mathf.PI);
            float offsetX = Random.Range(-jaggedness, jaggedness) * envelope * length * 0.15f;
            float offsetY = Random.Range(-jaggedness, jaggedness) * envelope * length * 0.15f;

            points[i] = basePos + right * offsetX + up * offsetY;
        }

        return points;
    }

    private void ApplyPoints(Vector3[] points)
    {
        coreLine.positionCount = points.Length;
        coreLine.SetPositions(points);

        glowLine.positionCount = points.Length;
        glowLine.SetPositions(points);
    }

    private IEnumerator FlickerAndDie(Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;

            // Re-randomize the arc each flicker for electric feel
            Vector3[] points = GenerateArcPoints(start, end);
            ApplyPoints(points);

            // Fade out in the last 40% of life
            float fadeT = Mathf.Clamp01((elapsed - duration * 0.6f) / (duration * 0.4f));
            float alpha = 1f - fadeT;

            coreLine.widthMultiplier = coreWidth * alpha;
            glowLine.widthMultiplier = glowWidth * alpha;
        }

        Destroy(gameObject);
    }
}