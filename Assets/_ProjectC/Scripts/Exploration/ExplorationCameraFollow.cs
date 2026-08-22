using UnityEngine; // 카메라와 Transform 기능 사용

[RequireComponent(typeof(Camera))]
public sealed class ExplorationCameraFollow : MonoBehaviour // 탐사 플레이어 카메라 추적
{
    private ExplorationPlayerController target; // 추적할 탐사 플레이어

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
    }
}
