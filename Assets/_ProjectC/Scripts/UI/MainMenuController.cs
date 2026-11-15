using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // 키보드 메뉴 선택 기능 사용
public sealed class MainMenuController : MonoBehaviour // 메인 메뉴 동작 연결
{ // 클래스 시작
    [SerializeField] private MainMenuView menuView; // 메인 메뉴 화면
    private string savePath; // 현재 거점 저장 경로
    private void Start() // 메뉴 초기화
    { // 초기화 시작
        if (menuView == null) // 씬 화면 연결 확인
        { // 화면 조회 시작
            menuView = FindFirstObjectByType<MainMenuView>(); // 기존 메뉴 화면 조회
        } // 화면 조회 종료
        if (menuView == null) // 메뉴 화면 누락 확인
        { // 화면 누락 처리 시작
            Debug.LogError("[MainMenu] 메인 메뉴 화면이 연결되지 않았습니다."); // 화면 누락 안내
            return; // 초기화 중단
        } // 화면 누락 처리 종료
        if (savePath == null) // 저장 경로 초기화 확인
        { // 기본 경로 설정 시작
            savePath = LobbySaveService.DefaultPath; // 기본 저장 경로 보관
        } // 기본 경로 설정 종료
        menuView.Bind(ShowNewGameWarning, ContinueGame, OpenSettings, QuitGame, LobbySaveService.CanLoad(savePath)); // 저장 상태에 맞춰 메뉴 동작 연결
        if (EventSystem.current != null) // 입력 관리자 존재 확인
        { // 첫 버튼 선택 시작
            EventSystem.current.SetSelectedGameObject(menuView.NewGameButton.gameObject); // 새로하기 기본 선택
        } // 첫 버튼 선택 종료
    } // 초기화 종료
    private void ShowNewGameWarning() // 새로하기 경고 요청
    { // 경고 요청 시작
        menuView.ShowNewGameConfirmation(StartGame); // 새 게임 확인 화면 표시
    } // 경고 요청 종료
    public void StartGame() // 게임 시작
    { // 게임 시작 처리
        if (SceneFlowManager.Instance == null) // 씬 관리자 존재 확인
        { // 관리자 누락 처리 시작
            Debug.LogError("[MainMenuController] SceneFlowManager를 찾을 수 없습니다."); // 관리자 누락 안내
            return; // 게임 시작 중단
        } // 관리자 누락 처리 종료
        PlayerResourceManager.Instance?.ResetResources(); // 이전 게임 자원 초기화
        FacilityUpgradeManager.Instance?.ResetFacilities(); // 이전 게임 설비 초기화
        CharacterProgressionManager.Instance?.ResetProgress(); // 이전 게임 성장 초기화
        CharacterAffinityManager.Instance?.ResetAffinity(); // 이전 게임 호감도 초기화
        CharacterRecoveryManager.Instance?.ResetRecoveryState(); // 이전 게임 회복 상태 초기화
        BattleResultManager.Instance?.DiscardPendingResult(); // 이전 전투 결과 제거
        BattleResultManager.Instance?.ResetSavedPartyState(); // 이전 게임 파티 상태 초기화
        ResetRunState(); // 이전 탐사 회차 상태 정리
        SceneFlowManager.Instance.LoadScene("20_Lobby"); // 거점 씬 이동
    } // 게임 시작 종료
    public void ContinueGame() // 저장된 거점 이어하기
    { // 이어하기 시작
        if (SceneFlowManager.Instance == null || SceneFlowManager.Instance.IsLoadingScene || !Application.CanStreamedLevelBeLoaded("20_Lobby")) // 거점 이동 가능 여부 확인
        { // 씬 이동 불가 처리 시작
            Debug.LogWarning("[MainMenu] 거점 화면을 열 수 없습니다."); // 씬 이동 실패 안내
            return; // 이어하기 중단
        } // 씬 이동 불가 처리 종료
        if (!LobbySaveService.Load(savePath)) // 저장 상태 복원 시도
        { // 저장 복원 실패 처리 시작
            menuView.Bind(ShowNewGameWarning, ContinueGame, OpenSettings, QuitGame, false); // 이어하기 버튼 비활성화
            Debug.LogWarning("[MainMenu] 저장 데이터를 불러올 수 없습니다."); // 복원 실패 안내
            return; // 이어하기 중단
        } // 저장 복원 실패 처리 종료
        ResetRunState(); // 이전 탐사 회차 상태 정리
        SceneFlowManager.Instance.LoadScene("20_Lobby"); // 복원된 거점 씬 이동
    } // 이어하기 종료
    private static void ResetRunState() // 탐사 회차 상태 정리
    { // 회차 상태 정리 시작
        ExplorationSessionManager.Instance?.ResetExploration(); // 이전 게임 탐사 상태 초기화
        ShopRunManager.Instance?.ResetSession(); // 이전 상점 회차 초기화
        RestRoomRunManager.Instance?.ResetSession(); // 이전 휴식 회차 초기화
        RunDeckManager.Instance?.ResetToSource(null); // 이전 회차 덱 제거
        PartyLoadoutRuntime.Clear(); // 이전 파티 편성 제거
        ConsumableRunManager.Instance?.Inventory.Clear(); // 이전 게임 소모품 제거
        while (RelicRunManager.Instance != null && RelicRunManager.Instance.Inventory.Count > 0) // 이전 게임 유물 존재 확인
        { // 유물 제거 시작
            string relicId = RelicRunManager.Instance.Inventory.OwnedRelics[0].RelicId; // 첫 유물 식별자 조회
            RelicRunManager.Instance.TryRemove(relicId); // 첫 유물 제거
        } // 유물 제거 종료
    } // 회차 상태 정리 종료
    public void OpenSettings() // 설정 화면 이동
    { // 설정 이동 시작
        if (SceneFlowManager.Instance == null) // 씬 관리자 존재 확인
        { // 관리자 누락 처리 시작
            Debug.LogError("[MainMenuController] SceneFlowManager를 찾을 수 없습니다."); // 관리자 누락 안내
            return; // 설정 이동 중단
        } // 관리자 누락 처리 종료
        SceneFlowManager.Instance.LoadScene("90_Settings"); // 설정 씬 이동
    } // 설정 이동 종료
    public void QuitGame() // 게임 종료
    { // 종료 시작
#if UNITY_EDITOR // 편집기 실행 구분
        UnityEditor.EditorApplication.isPlaying = false; // 편집기 재생 종료
#else // 독립 실행 환경 구분
        Application.Quit(); // 게임 프로그램 종료
#endif // 실행 환경 분기 종료
    } // 종료 처리 완료
} // 클래스 종료
