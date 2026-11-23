using UnityEngine; // 트리거 처리와 유니티 기능 사용

public sealed class ExplorationEventView : MonoBehaviour, IExplorationInteractable // F 키로 여는 탐사 이벤트 오브젝트
{
    private ExplorationEventData eventData; // 현재 이벤트 데이터
    private string runtimeEventId; // 현재 배치 이벤트 런타임 ID
    private bool interactionLocked; // 이벤트 상호작용 잠금 상태

    public string RuntimeEventId => runtimeEventId; // 런타임 이벤트 ID 조회
    public ExplorationEventData EventData => eventData; // 이벤트 데이터 조회
    public bool CanInteract => isActiveAndEnabled && !interactionLocked && eventData != null && !string.IsNullOrWhiteSpace(runtimeEventId) && ExplorationSessionManager.Instance?.IsEventResolved(runtimeEventId) != true; // 열 수 있는 미처리 이벤트 확인

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

    internal void ResetInteraction() // 불러오기 전 이전 이벤트 잠금 해제
    { // 잠금 해제 시작
        interactionLocked = false; // 복원 후 미처리 이벤트 재접촉 허용
    } // 잠금 해제 종료

    public void Interact(ExplorationPlayerController player) // 키를 눌렀을 때만 실제 이벤트 열기
    { // 탐사 이벤트 열기 시작
        if (player == null || !CanInteract) // 현재 플레이어와 아직 열 수 있는 이벤트 재확인
        { // 사용할 수 없는 이벤트 처리 시작
            return; // 완료된 이벤트와 다른 입력에서의 중복 실행 차단
        } // 사용할 수 없는 이벤트 처리 종료

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
    } // 탐사 이벤트 열기 종료
}
