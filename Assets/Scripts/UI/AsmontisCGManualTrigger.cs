using UnityEngine;

public class AsmontisCGManualTrigger : MonoBehaviour
{
    [SerializeField]
    private BossDefeatedCGSequence cgSequence;

    [SerializeField]
    private bool activateGameObjectBeforePlay = true;

    public void Play()
    {
        if (cgSequence == null)
        {
            Debug.LogError("[AsmontisCGManualTrigger] cgSequence is null");
            return;
        }

        if (activateGameObjectBeforePlay && cgSequence.gameObject != null && !cgSequence.gameObject.activeSelf)
        {
            cgSequence.gameObject.SetActive(true);
        }

        cgSequence.PlaySequence();
    }
}

