using UnityEngine; // Trigger와 Scene 전환 기능 사용

public sealed class ExplorationEncounterView : MonoBehaviour // 탐사 조우 표시와 전투 진입 처리
{
    private EncounterData encounterData; // 실제 전투 조우 데이터
    private string runtimeEncounterId; // 현재 배치의 런타임 조우 ID
    private bool transitionRequested; // 전투 Scene 이동 요청 여부

    public void Initialize(
        EncounterData data,
        string encounterRuntimeId) // 절차 조우 초기화
    {
        encounterData = data; // 전투 조우 데이터 저장
        runtimeEncounterId = encounterRuntimeId; // 런타임 조우 ID 저장

        string displayRuntimeId =
            string.IsNullOrWhiteSpace(runtimeEncounterId)
                ? "Unknown"
                : runtimeEncounterId; // 오브젝트 이름용 ID 결정

        gameObject.name =
            $"Encounter_{displayRuntimeId}"; // 절차 조우 오브젝트 이름 지정
    }

    public void Initialize(EncounterData data) // 기존 초기화 호출 호환
    {
        string fallbackRuntimeId =
            data != null
                ? data.EncounterId
                : string.Empty; // 기존 Encounter ID 사용

        Initialize(data, fallbackRuntimeId); // 절차 조우 초기화 호출
    }

    private void OnTriggerEnter2D(Collider2D other) // 플레이어 조우 접촉 처리
    {
        if (transitionRequested || encounterData == null)
        {
            return;
        }

        ExplorationPlayerController player =
            other.GetComponent<ExplorationPlayerController>(); // 플레이어 접촉 확인

        if (player == null)
        {
            return;
        }

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        SceneFlowManager sceneFlowManager = SceneFlowManager.Instance; // 전투 씬 전환 관리자 조회
        if (sceneFlowManager == null || sceneFlowManager.IsLoadingScene || !Application.CanStreamedLevelBeLoaded("40_Battle") || !sessionManager.CanBeginEncounter(runtimeEncounterId, encounterData)) // 정상 조우와 씬 이동 가능 여부 확인
        { // 전투 진입 불가 처리 시작
            return; // 조우 상태와 기존 자동 저장 유지
        } // 전투 진입 불가 처리 종료
        if (!ExplorationAutoSave.SaveBeforeBattle(player.transform.position)) // 조우 시작 전 자동 저장 확인
        { // 자동 저장 실패 처리 시작
            Debug.LogWarning("[ExplorationAutoSave] 전투 전 상태를 저장하지 못했습니다."); // 기존 저장 유지와 저장 실패 안내
        } // 자동 저장 실패 처리 종료

        if (!sessionManager.BeginEncounter(
                runtimeEncounterId,
                encounterData,
                player.transform.position,
                transform.position))
        {
            return;
        }

        transitionRequested = true; // 전투 Scene 이동 요청 잠금

        sceneFlowManager.LoadScene("40_Battle"); // 전투 Scene 이동

        if (!sceneFlowManager.IsLoadingScene)
        {
            transitionRequested = false; // 이동 실패 시 재요청 허용
        }
    }
}
