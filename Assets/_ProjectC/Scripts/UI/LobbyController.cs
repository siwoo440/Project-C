using UnityEngine;

public sealed class LobbyController : MonoBehaviour
{
    public void OpenSettings()
    {
        if (SceneFlowManager.Instance == null) // SceneFlowManager 존재 여부 확인
        {
            Debug.LogError("[LobbyController] SceneFlowManager를 찾을 수 없습니다."); // 관리자 누락 출력
            return; // 설정 이동 중단
        }

        SceneFlowManager.Instance.LoadScene("90_Settings"); // 설정 씬 이동
    }
}
