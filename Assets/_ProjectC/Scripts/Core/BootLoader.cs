using UnityEngine;

public sealed class BootLoader : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance == null) // GameManager 준비 여부 확인
        {
            Debug.LogError("[BootLoader] GameManager를 찾을 수 없습니다."); // GameManager 누락 출력
            return; // 부트 진행 중단
        }

        if (SceneFlowManager.Instance == null) // SceneFlowManager 준비 여부 확인
        {
            Debug.LogError("[BootLoader] SceneFlowManager를 찾을 수 없습니다."); // SceneFlowManager 누락 출력
            return; // 부트 진행 중단
        }

        if (!GameManager.Instance.IsInitialized) // 게임 초기화 완료 여부 확인
        {
            Debug.LogError("[BootLoader] GameManager 초기화가 완료되지 않았습니다."); // 초기화 실패 출력
            return; // 부트 진행 중단
        }

        SceneFlowManager.Instance.LoadScene("10_MainMenu"); // 메인 메뉴 자동 이동
    }
}
