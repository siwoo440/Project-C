using UnityEngine; // Trigger와 플레이어 탐지 기능 사용

public sealed class ExplorationFloorStairs : MonoBehaviour, IExplorationInteractable // F 키로 이동하는 탐사 계단
{
    private ExplorationMapRuntime mapRuntime; // 탐사 맵 런타임 참조
    public bool CanInteract => isActiveAndEnabled && mapRuntime != null && mapRuntime.CurrentMap != null && !mapRuntime.HasEncounterAt(mapRuntime.CurrentMap.StairsCoordinate); // 길을 막는 조우를 끝낸 계단만 안내

    public void Initialize(ExplorationMapRuntime runtime) // 계단 런타임 연결
    {
        mapRuntime = runtime; // 맵 런타임 저장
    }

    public void Interact(ExplorationPlayerController player) // 계단 앞에서 누른 F 키의 이동 처리
    { // 계단 상호작용 시작
        if (player == null || !CanInteract) // 유효한 플레이어와 열린 다음 층 관문 확인
        { // 관문 조건 미충족 처리 시작
            return; // 미연결 계단과 전투 미클리어 이동 차단
        } // 관문 조건 미충족 처리 종료

        mapRuntime.TryDescendFloor(player); // 다음 층 진행 요청
    } // 계단 상호작용 종료
}
