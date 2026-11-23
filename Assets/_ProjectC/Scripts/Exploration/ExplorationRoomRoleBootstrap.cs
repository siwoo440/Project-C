using UnityEngine; // 런타임 컴포넌트 자동 연결 사용

[DefaultExecutionOrder(1000)] // 기존 탐사 맵 생성 이후 방 역할 통합 적용
public sealed class ExplorationRoomRoleBootstrap : MonoBehaviour // 56일차 방 역할 자동 연결기
{
    private void Update() // 탐사 맵 런타임 생성 대기
    {
        ExplorationMapRuntime mapRuntime = FindFirstObjectByType<ExplorationMapRuntime>(); // 현재 탐사 맵 런타임 조회
        if (mapRuntime == null) // 탐사 맵 생성 여부 확인
        {
            return; // 생성 완료까지 대기
        }

        ExplorationRoomRoleRuntime roomRoleRuntime =
            mapRuntime.GetComponent<ExplorationRoomRoleRuntime>(); // 기존 방 역할 런타임 조회

        if (roomRoleRuntime == null) // 방 역할 런타임 누락 확인
        {
            roomRoleRuntime = mapRuntime.gameObject.AddComponent<ExplorationRoomRoleRuntime>(); // 맵 오브젝트에 방 역할 런타임 추가
        }

        roomRoleRuntime.Initialize(mapRuntime); // 현재 맵 런타임 연결
        Destroy(this); // 연결 완료 부트스트랩 컴포넌트 제거
    }
}
