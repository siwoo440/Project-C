using UnityEngine;

public sealed class LobbyController : MonoBehaviour
{
    private Canvas lobbyCanvas; // 현재 로비 캔버스
    private void Start() // 거점 화면 시작
    { // 시작 처리 시작
        Canvas canvas = FindFirstObjectByType<Canvas>(); // 거점 캔버스 조회
        if (canvas == null) // 화면 누락 확인
        { // 누락 처리 시작
            Debug.LogError("[LobbyController] 로비 캔버스가 없습니다."); // 화면 누락 안내
            return; // 로비 화면 생성 중단
        } // 누락 처리 종료
        lobbyCanvas = canvas; // 로비 캔버스 보관
        Texture2D background = Resources.Load<Texture2D>("UI/LobbyBackground"); // 거점 배경 이미지 조회
        if (background != null) // 배경 이미지 존재 확인
        { // 배경 연결 시작
            LobbyBackgroundInstaller.Install(canvas, background); // 거점 배경 표시
        } // 배경 연결 종료
        CharacterRecoveryManager.EnsureInstance(); // 회복 상태 관리자 준비
        BattleResultManager.EnsureInstance(); // 파티 상태 관리자 준비
        LobbyAutosaveController autosave = gameObject.AddComponent<LobbyAutosaveController>(); // 거점 자동 저장 관리자 추가
        autosave.Initialize(LobbySaveService.DefaultPath); // 기본 저장 경로 연결
        Transform oldTitle = canvas.transform.Find("LobbyTitle"); // 기존 임시 제목 조회
        if (oldTitle != null) // 기존 임시 제목 존재 확인
        { // 이전 제목 숨김 시작
            oldTitle.gameObject.SetActive(false); // 기존 임시 제목 숨김
        } // 이전 제목 숨김 종료
        Transform oldSettings = canvas.transform.Find("SettingsButton"); // 기존 설정 버튼 조회
        if (oldSettings != null) // 기존 설정 버튼 존재 확인
        { // 이전 버튼 숨김 시작
            oldSettings.gameObject.SetActive(false); // 기존 설정 버튼 숨김
        } // 이전 버튼 숨김 종료
        LobbyLayoutView.Create(canvas, OpenSettings, OpenFacility, OpenExpedition); // 새 로비 화면 구성
        SaveMenuEntry.Install(canvas, false); // 거점 저장 메뉴 버튼 설치
    } // 시작 처리 종료
    private void OpenFacility() // 설비 강화 화면 열기
    { // 설비 화면 열기 시작
        FacilityUpgradeDebugView view = FacilityUpgradeManager.EnsureInstance().GetComponent<FacilityUpgradeDebugView>(); // 기존 설비 강화 화면 조회
        view?.OpenPanel(); // 설비 강화 화면 표시
    } // 설비 화면 열기 종료
    private void OpenExpedition() // 탐사 맵 선택 화면 열기
    { // 맵 선택 열기 시작
        if (FindFirstObjectByType<SaveMenuController>() != null) // 저장 화면 열림 확인
        { // 중복 화면 처리 시작
            return; // 맵 선택 열기 중단
        } // 중복 화면 처리 종료
        ExpeditionMapView.Open(lobbyCanvas); // 탐사 맵 선택 화면 표시
    } // 맵 선택 열기 종료
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
