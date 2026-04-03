using UnityEngine;

public class MaterialOffsetScroller : MonoBehaviour
{
    [Tooltip("Time in seconds to scroll the offset from 0 to -0.5")]
    [SerializeField] private float duration = 2f;

    [Tooltip("Assign the Renderer of the capsule here. If left empty the script will try to find one automatically.")]
    [SerializeField] private Renderer targetRenderer;

    private Material _material;
    private float _elapsed = 0f;

    void Start()
    {
        // 1. Use the manually assigned renderer if provided.
        // 2. Fall back to a Renderer on this same GameObject.
        // 3. Fall back to any Renderer found in children.
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError("MaterialOffsetScroller: No Renderer found.", this);
            enabled = false;
            return;
        }

        // Creates a per-instance material copy so the shared asset is never modified.
        _material = targetRenderer.material;
    }

    void Update()
    {
        if (_material == null) return;

        _elapsed += Time.deltaTime;

        if (_elapsed >= duration)
            _elapsed -= duration;

        float t = _elapsed / duration;

        Vector2 offset = _material.mainTextureOffset;
        offset.x = Mathf.Lerp(0f, -0.5f, t);
        _material.mainTextureOffset = offset;
    }

    void OnDestroy()
    {
        // Destroy the runtime material instance to prevent memory leaks.
        if (_material != null)
            Destroy(_material);
    }
}