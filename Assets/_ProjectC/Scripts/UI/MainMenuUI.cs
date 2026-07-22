using UnityEngine; // Unity 기본 기능

public sealed class MainMenuUI : MonoBehaviour // 메인 메뉴 버튼 관리자
{
    [SerializeField] private GameObject quitConfirmPanel; // 종료 확인 창
    private void Awake() // 메인 메뉴 UI 초기화
    {
        quitConfirmPanel.SetActive(false); // 종료 확인 창 숨김
    }
    public void OnStartButtonClicked() // 게임 시작 버튼 처리
    {
        if (!TryGetSceneFlowManager(out SceneFlowManager sceneFlowManager)) // 씬 관리자 확인
        {
            return; // 로비 이동 중단
        }

        sceneFlowManager.LoadLobby(); // 로비 씬 이동
    }

    public void OnSettingsButtonClicked() // 설정 버튼 처리
    {
        if (!TryGetSceneFlowManager(out SceneFlowManager sceneFlowManager)) // 씬 관리자 확인
        {
            return; // 설정 이동 중단
        }

        sceneFlowManager.LoadSettings(); // 설정 씬 이동
    }

    public void OnQuitButtonClicked() // 종료 버튼 처리
    {
        quitConfirmPanel.SetActive(true); // 종료 확인 창 표시
    }
    public void OnQuitConfirmButtonClicked() // 종료 확인 버튼 처리
    {
        Debug.Log("게임 종료 확인", this); // 종료 확인 출력
        Application.Quit(); // 실행 중인 게임 종료
    }

    public void OnQuitCancelButtonClicked() // 종료 취소 버튼 처리
    {
        quitConfirmPanel.SetActive(false); // 종료 확인 창 숨김
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