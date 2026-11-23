using UnityEngine; // 카메라와 Transform 기능 사용

[RequireComponent(typeof(Camera))]
public sealed class ExplorationCameraFollow : MonoBehaviour // 탐사 플레이어 카메라 추적
{
    private ExplorationPlayerController target; // 추적할 탐사 플레이어
    private Camera worldCamera; // 현재 탐사 위치를 화면으로 옮길 실제 카메라

    private void Awake() // 동작하는 탐사 카메라 확인
    { // 화면 좌표 기준 연결 시작
        worldCamera = GetComponent<Camera>(); // 추적과 안내 배치에 함께 쓰는 카메라
    } // 화면 좌표 기준 연결 종료

    private void LateUpdate() // 플레이어 이동 이후 카메라 갱신
    {
        if (target == null)
        {
            target =
                FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 자동 탐색
        }

        if (target == null)
        {
            return;
        }

        Vector3 cameraPosition =
            transform.position; // 현재 카메라 위치 조회

        cameraPosition.x =
            target.transform.position.x; // 플레이어 X 추적

        cameraPosition.y =
            target.transform.position.y; // 플레이어 Y 추적

        transform.position =
            cameraPosition; // 기존 Z를 유지한 채 카메라 이동
        target.PlaceInteractionPrompt(worldCamera); // 이동을 반영한 캐릭터 오른쪽 안내 위치 갱신
    }
}
