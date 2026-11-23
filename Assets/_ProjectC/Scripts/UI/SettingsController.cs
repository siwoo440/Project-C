using UnityEngine;

public sealed class SettingsController : MonoBehaviour
{
    public void Back()
    {
        if (SceneFlowManager.Instance == null) // SceneFlowManager 존재 여부 확인
        {
            Debug.LogError("[SettingsController] SceneFlowManager를 찾을 수 없습니다."); // 관리자 누락 출력
            return; // 이전 씬 이동 중단
        }

        SceneFlowManager.Instance.LoadPreviousScene(); // 이전 씬 복귀
    }
}
