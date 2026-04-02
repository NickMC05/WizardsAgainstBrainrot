using System.Collections;
using UnityEngine;

public class LightningArcTrail : MonoBehaviour
{
[Header("Shape")]
[SerializeField] private int segments = 12;
[SerializeField] private float jitter = 0.25f;
[SerializeField] private float refreshInterval = 0.02f;

[Header("Look")]
[SerializeField] private float width = 0.08f;
[SerializeField] private Color coreColor = new Color(0.7f, 0.9f, 1f, 1f);
[SerializeField] private Color endColor = new Color(0.7f, 0.9f, 1f, 0f);

[Header("Lifetime")]
[SerializeField] private float life = 0.12f;

private LineRenderer lr;
private Vector3 startPoint;
private Vector3 endPoint;

public static void Spawn(Vector3 from, Vector3 to, Transform parent = null)
{
GameObject go = new GameObject("LightningArc");
if (parent != null) go.transform.SetParent(parent, true);

LightningArcTrail arc = go.AddComponent<LightningArcTrail>();
arc.Initialize(from, to);
}

private void Initialize(Vector3 from, Vector3 to)
{
startPoint = from;
endPoint = to;

lr = gameObject.AddComponent<LineRenderer>();
lr.useWorldSpace = true;
lr.positionCount = Mathf.Max(2, segments);
lr.startWidth = width;
lr.endWidth = width * 0.4f;
lr.numCornerVertices = 2;
lr.numCapVertices = 2;
lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
lr.receiveShadows = false;

Gradient g = new Gradient();
g.SetKeys(
new GradientColorKey[] {
new GradientColorKey(coreColor, 0f),
new GradientColorKey(coreColor, 0.7f),
new GradientColorKey(endColor, 1f)
},
new GradientAlphaKey[] {
new GradientAlphaKey(1f, 0f),
new GradientAlphaKey(0.8f, 0.6f),
new GradientAlphaKey(0f, 1f)
}
);
lr.colorGradient = g;

Shader shader = Shader.Find("Sprites/Default");
if (shader == null) shader = Shader.Find("Unlit/Color");
Material mat = new Material(shader);
mat.color = Color.white;
lr.material = mat;

StartCoroutine(AnimateArc());
}

private IEnumerator AnimateArc()
{
float elapsed = 0f;
while (elapsed < life)
{
DrawJaggedArc(startPoint, endPoint);
elapsed += refreshInterval;
yield return new WaitForSeconds(refreshInterval);
}

Destroy(gameObject);
}

private void DrawJaggedArc(Vector3 from, Vector3 to)
{
int count = Mathf.Max(2, segments);
lr.positionCount = count;

Vector3 dir = (to - from);
float distance = dir.magnitude;
Vector3 forward = distance > 0.001f ? dir.normalized : Vector3.forward;

Vector3 side = Vector3.Cross(forward, Vector3.up);
if (side.sqrMagnitude < 0.001f) side = Vector3.Cross(forward, Vector3.right);
side.Normalize();

Vector3 up = Vector3.Cross(forward, side).normalized;

for (int i = 0; i < count; i++)
{
float t = i / (float)(count - 1);
Vector3 p = Vector3.Lerp(from, to, t);

if (i != 0 && i != count - 1)
{
float envelope = 1f - Mathf.Abs(0.5f - t) * 2f;
float x = Random.Range(-jitter, jitter) * envelope;
float y = Random.Range(-jitter, jitter) * envelope;
p += side * x + up * y;
}

lr.SetPosition(i, p);
}
}
}