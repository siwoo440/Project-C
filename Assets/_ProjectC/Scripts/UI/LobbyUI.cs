using UnityEngine; // Unity 기본 기능

public sealed class LobbyUI : MonoBehaviour // 로비 화면 버튼 관리자
{
    public void OnExpeditionButtonClicked() // 탐사 버튼 처리
    {
        if (!TryGetSceneFlowManager(out SceneFlowManager sceneFlowManager)) // 씬 관리자 확인
        {
            return; // 탐사 이동 중단
        }

        sceneFlowManager.LoadScene(SceneFlowManager.ExpeditionSceneName); // 탐사 씬 이동
    }

    public void OnMainMenuButtonClicked() // 메인 메뉴 버튼 처리
    {
        if (!TryGetSceneFlowManager(out SceneFlowManager sceneFlowManager)) // 씬 관리자 확인
        {
            return; // 메인 메뉴 이동 중단
        }

        sceneFlowManager.LoadMainMenu(); // 메인 메뉴 씬 이동
    }

    private bool TryGetSceneFlowManager(out SceneFlowManager sceneFlowManager) // 씬 관리자 참조 확인
    {
        sceneFlowManager = SceneFlowManager.Instance; // 공용 씬 관리자 가져오기

        if (sceneFlowManager != null) // 관리자 존재 확인
        {
            return true; // 관리자 확인 성공
        }

        Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
        return false; // 관리자 확인 실패
    }
}