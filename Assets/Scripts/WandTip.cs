using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class WandTipDetector : MonoBehaviour
{
    [SerializeField] private SpellManager spellManager;
    [SerializeField] private WandController wandController;

    private SphereCollider sphereCol;

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = 0.02f;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    /// <summary>
    /// Checks whether the wand tip is already overlapping a spell node
    /// at the moment the player starts casting. Without this, a node that
    /// the tip is resting inside would be missed because OnTriggerEnter
    /// already fired before the cast began.
    /// </summary>
    public void CheckInitialOverlaps()
    {
        if (spellManager == null || !spellManager.IsCasting) return;

        float worldRadius = sphereCol.radius * Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z);

        Collider[] hits = Physics.OverlapSphere(transform.position, worldRadius);
        foreach (Collider hit in hits)
        {
            SpellCollider sc = hit.GetComponent<SpellCollider>();
            if (sc != null)
            {
                sc.Activate();
                spellManager.AddToPattern(sc.ColliderIndex);

                if (wandController != null)
                    wandController.SendHapticPulse(0.4f, 0.05f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (spellManager == null || !spellManager.IsCasting) return;

        SpellCollider sc = other.GetComponent<SpellCollider>();
        if (sc != null)
        {
            sc.Activate();
            spellManager.AddToPattern(sc.ColliderIndex);

            if (wandController != null)
                wandController.SendHapticPulse(0.4f, 0.05f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        SpellCollider sc = other.GetComponent<SpellCollider>();
        if (sc != null)
            sc.Deactivate();
    }
}