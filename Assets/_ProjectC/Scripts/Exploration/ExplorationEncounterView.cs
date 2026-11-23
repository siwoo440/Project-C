using UnityEngine; // Trigger와 Scene 전환 기능 사용

public sealed class ExplorationEncounterView : MonoBehaviour // 탐사 조우 표시와 전투 진입 처리
{
    private EncounterData encounterData; // 실제 전투 조우 데이터
    public EncounterData Data => encounterData; // 현재 층에 배치된 원본 적 편성 조회
    private string runtimeEncounterId; // 현재 배치의 런타임 조우 ID
    private bool transitionRequested; // 전투 Scene 이동 요청 여부
    private Collider2D deferredContact; // 다른 창을 닫은 뒤 처리할 실제 플레이어 접촉

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

        if (ExplorationPlayerController.InputBlocked) // 탐사 창이 열려 있을 때는 실제 전투 전환 보류
        { // 플레이어와 계속 맞닿아 있는 물리 접촉의 보류 시작
            deferredContact = other; // 원래 입력이 복구된 뒤 한 번 시도할 플레이어 접촉 보관
            return; // 상점과 사건 및 보상 창 뒤의 전투 진입 차단
        } // 플레이어와 계속 맞닿아 있는 물리 접촉의 보류 종료

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

    private void OnTriggerStay2D(Collider2D other) // 입력이 다시 열린 기존 접촉의 단일 전투 확인
    { // 보류한 접촉에만 재시도 시작
        if (other == deferredContact && !ExplorationPlayerController.InputBlocked) // 이전에 막힌 플레이어가 아직 닿아 있는지 확인
        { // 기존 전투 접촉 재사용 시작
            deferredContact = null; // 저장 실패 뒤 같은 프레임의 반복 처리를 막을 일회성 해제
            OnTriggerEnter2D(other); // 동일한 세션과 전투 저장 및 지도 조각 규칙으로 진행
        } // 기존 전투 접촉 재사용 종료
    } // 보류한 접촉에만 재시도 종료

    private void OnTriggerExit2D(Collider2D other) // 입력 복구 전에 실제 접촉에서 벗어난 경우 확인
    { // 더 이상 닿지 않는 플레이어의 접촉 정리 시작
        if (other == deferredContact) // 마지막에 보류한 바로 그 접촉인지 확인
        { // 보류한 접촉 취소 시작
            deferredContact = null; // 벗어난 플레이어에게 뒤늦게 전투가 시작되지 않도록 초기화
        } // 보류한 접촉 취소 종료
    } // 더 이상 닿지 않는 플레이어의 접촉 정리 종료
}
