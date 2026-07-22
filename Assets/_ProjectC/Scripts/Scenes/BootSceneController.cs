using UnityEngine; // Unity 기본 기능

public sealed class BootSceneController : MonoBehaviour // 부트 씬 시작 관리자
{
    private void Start() // 첫 프레임 시작 처리
    {
        if (SceneFlowManager.Instance == null) // 씬 관리자 존재 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 자동 이동 중단
        }

        SceneFlowManager.Instance.LoadMainMenu(); // 메인 메뉴 자동 이동
    }
}