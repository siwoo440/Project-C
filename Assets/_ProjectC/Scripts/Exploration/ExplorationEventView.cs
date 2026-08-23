using UnityEngine; // 트리거 처리와 유니티 기능 사용

public sealed class ExplorationEventView : MonoBehaviour // 탐사 이벤트 오브젝트 표시와 UI 호출 처리
{
    private ExplorationEventData eventData; // 현재 이벤트 데이터
    private string runtimeEventId; // 현재 배치 이벤트 런타임 ID
    private bool interactionLocked; // 이벤트 상호작용 잠금 상태

    public string RuntimeEventId => runtimeEventId; // 런타임 이벤트 ID 조회
    public ExplorationEventData EventData => eventData; // 이벤트 데이터 조회

    public void Initialize(
        ExplorationEventData data,
        string eventRuntimeId) // 이벤트 오브젝트 초기화
    {
        eventData = data; // 이벤트 데이터 저장
        runtimeEventId = eventRuntimeId; // 이벤트 런타임 ID 저장

        string safeRuntimeId =
            string.IsNullOrWhiteSpace(runtimeEventId)
                ? "Unknown"
                : runtimeEventId; // 이름용 런타임 ID 보정

        gameObject.name =
            $"ExplorationEvent_{safeRuntimeId}"; // 이벤트 오브젝트 이름 설정
    }

    public void LockInteraction() // 이벤트 상호작용 잠금
    {
        interactionLocked = true; // 이벤트 상호작용 잠금 저장
    }

    private void OnTriggerEnter2D(Collider2D other) // 플레이어 이벤트 접촉 처리
    {
        if (interactionLocked ||
            eventData == null ||
            string.IsNullOrWhiteSpace(runtimeEventId))
        {
            return; // 잠금 상태 또는 데이터 누락 시 중단
        }

        ExplorationPlayerController player =
            other.GetComponent<ExplorationPlayerController>(); // 플레이어 접촉 여부 확인

        if (player == null)
        {
            return; // 플레이어가 아니면 중단
        }

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        if (sessionManager.IsEventResolved(runtimeEventId))
        {
            interactionLocked = true; // 이미 처리한 이벤트는 재실행 방지
            return;
        }

        ExplorationEventPanelView panelView =
            ExplorationEventPanelView.EnsureInstance(); // 이벤트 패널 UI 준비

        if (panelView == null)
        {
            Debug.LogWarning(
                "[ExplorationEventView] 이벤트 패널을 준비하지 못해 이벤트를 열 수 없습니다.",
                this); // 이벤트 패널 누락 경고

            return;
        }

        interactionLocked = true; // 이벤트 중복 실행 방지 잠금

        panelView.ShowEvent(
            this,
            eventData,
            runtimeEventId); // 이벤트 패널 표시 요청
    }
}
