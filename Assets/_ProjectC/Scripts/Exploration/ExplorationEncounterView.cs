using UnityEngine;

public sealed class ExplorationEncounterView : MonoBehaviour
{
    private EncounterData encounterData;
    private bool transitionRequested;

    public void Initialize(EncounterData data)
    {
        encounterData = data;
        gameObject.name = $"Encounter_{data.EncounterId}";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transitionRequested || encounterData == null)
        {
            return;
        }

        ExplorationPlayerController player = other.GetComponent<ExplorationPlayerController>();
        if (player == null)
        {
            return;
        }

        ExplorationSessionManager sessionManager = ExplorationSessionManager.EnsureInstance();
        if (!sessionManager.BeginEncounter(
                encounterData,
                player.transform.position,
                transform.position))
        {
            return;
        }

        transitionRequested = true;

        SceneFlowManager sceneFlowManager = SceneFlowManager.Instance;
        if (sceneFlowManager == null)
        {
            Debug.LogError(
                "[ExplorationEncounterView] SceneFlowManager가 없어 전투 Scene으로 이동할 수 없습니다.",
                this);
            transitionRequested = false;
            return;
        }

        sceneFlowManager.LoadScene("40_Battle");

        if (!sceneFlowManager.IsLoadingScene)
        {
            transitionRequested = false;
        }
    }
}
