using UnityEngine;

public class TutorialTarget : MonoBehaviour
{
    [SerializeField] private EnemyWaveScript waveController;
    [SerializeField] private bool destroyOnHit = false;

    private bool hasCompleted;

    public void Initialize(EnemyWaveScript controller)
    {
        waveController = controller;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCompleted)
            return;

        Fireball fireball = other.GetComponentInParent<Fireball>();
        if (fireball == null)
            return;

        hasCompleted = true;

        if (waveController != null)
            waveController.CompleteTutorialStage();

        if (destroyOnHit)
            Destroy(gameObject);
    }
    public void OnFireballHit()
    {
        if (hasCompleted)
            return;

        hasCompleted = true;

        if (waveController != null)
            waveController.CompleteTutorialStage();

        if (destroyOnHit)
            Destroy(gameObject);
    }
}