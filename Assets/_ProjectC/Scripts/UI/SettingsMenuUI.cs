using UnityEngine; // Unity 기본 기능

public sealed class SettingsMenuUI : MonoBehaviour // 설정 화면 버튼 관리자
{
    public void OnBackButtonClicked() // 뒤로 가기 버튼 처리
    {
        if (SceneFlowManager.Instance == null) // 씬 관리자 존재 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 메인 메뉴 이동 중단
        }

        SceneFlowManager.Instance.LoadMainMenu(); // 메인 메뉴 이동
    }
}