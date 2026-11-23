using UnityEngine; // Trigger와 플레이어 탐지 기능 사용

public sealed class ExplorationFloorStairs : MonoBehaviour // 다음 층 이동용 임시 계단
{
    private ExplorationMapRuntime mapRuntime; // 탐사 맵 런타임 참조

    public void Initialize(ExplorationMapRuntime runtime) // 계단 런타임 연결
    {
        mapRuntime = runtime; // 맵 런타임 저장
    }

    private void OnTriggerEnter2D(Collider2D other) // 계단 접촉 처리
    {
        if (mapRuntime == null) // 맵 런타임 연결 확인
        {
            return; // 미연결 상태 처리 중단
        }

        ExplorationPlayerController player =
            other.GetComponent<ExplorationPlayerController>(); // 플레이어 컴포넌트 확인

        if (player == null) // 플레이어 접촉 여부 확인
        {
            return; // 다른 오브젝트 접촉 무시
        }

        if (mapRuntime.CurrentMap != null &&
            mapRuntime.HasEncounterAt(mapRuntime.CurrentMap.StairsCoordinate)) // 마지막 진행 방 조우 잔존 여부 확인
        {
            Debug.Log(
                "[Exploration][Day57] 마지막 진행 전투를 클리어하기 전에는 다음 층으로 이동할 수 없습니다.",
                this); // 엘리트·보스 관문 안내

            return; // 관문 전투 미클리어 계단 이동 차단
        }

        mapRuntime.TryDescendFloor(player); // 다음 층 진행 요청
    }
}
