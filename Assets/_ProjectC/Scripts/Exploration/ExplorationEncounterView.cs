using UnityEngine; // Trigger와 Scene 전환 기능 사용

public sealed class ExplorationEncounterView : MonoBehaviour // 탐사 조우 표시와 전투 진입 처리
{
    private EncounterData encounterData; // 실제 전투 조우 데이터
    public EncounterData Data => encounterData; // 현재 층에 배치된 원본 적 편성 조회
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

        GetComponentInParent<ExplorationRoomRoleRuntime>()?.TryVisitCurrentRoom(); // 첫 방의 물리 접촉이 먼저인 경우에도 나침반 우선 판정
        SceneFlowManager sceneFlowManager = SceneFlowManager.Instance; // 전투 씬 전환 관리자 조회
        if (sceneFlowManager == null || sceneFlowManager.IsLoadingScene || !Application.CanStreamedLevelBeLoaded("40_Battle") || !sessionManager.CanBeginEncounter(runtimeEncounterId, encounterData)) // 정상 조우와 씬 이동 가능 여부 확인
        { // 전투 진입 불가 처리 시작
            return; // 조우 상태와 기존 자동 저장 유지
        } // 전투 진입 불가 처리 종료
        MapFragmentContact fragment = sessionManager.ResolveMapFragmentContact(runtimeEncounterId, encounterData, player.transform.position); // 실제 일반 조우 접촉에서만 보관 조각 판정
        if (fragment == MapFragmentContact.SaveFailed) // 판정 저장 실패와 조각 복구 확인
        { // 접촉 재시도 안내 시작
            ExplorationPartyStatusView hud = FindFirstObjectByType<ExplorationPartyStatusView>(); // 현재 탐사 화면 조회
            hud?.ShowMapFragmentNotice("저장 실패 · 지도 조각을 보관했습니다. 다시 접촉하세요."); // 보상과 상태 보존 안내
            Debug.LogWarning("[ExplorationAutoSave] 지도 조각 판정을 저장하지 못해 조우 진입을 보류했습니다."); // 저장 실패와 전투 보류 기록
            return; // 조각이나 조우를 잃지 않고 재시도 허용
        } // 접촉 재시도 안내 종료
        if (fragment == MapFragmentContact.Avoided) // 지도 조각으로 회피한 조우 확인
        { // 회피한 조우 정리 시작
            transitionRequested = true; // 같은 프레임의 재접촉 차단
            ExplorationPartyStatusView hud = FindFirstObjectByType<ExplorationPartyStatusView>(); // 현재 탐사 화면 조회
            hud?.ShowMapFragmentNotice("지도 조각을 따라 전투를 피했습니다."); // 회피 성공을 플레이어에게 표시
            gameObject.SetActive(false); // 실제 조우 표식과 접촉 즉시 해제
            return; // 전투 시작과 승리 보상 없이 탐사 계속
        } // 회피한 조우 정리 종료
        if (fragment == MapFragmentContact.NoEffect && !ExplorationAutoSave.SaveBeforeBattle(player.transform.position)) // 조각 판정으로 저장하지 않은 조우 자동 저장
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
