using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // 키보드 메뉴 선택 기능 사용
using System.IO; // 저장 파일 시간 조회 사용
public sealed class MainMenuController : MonoBehaviour // 메인 메뉴 동작 연결
{ // 클래스 시작
    [SerializeField] private MainMenuView menuView; // 메인 메뉴 화면
    private string savePath; // 현재 거점 저장 경로
    private string explorationSavePath; // 현재 탐사 저장 경로
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
        if (explorationSavePath == null) // 탐사 저장 경로 초기화 확인
        { // 탐사 기본 경로 설정 시작
            explorationSavePath = ExplorationSaveService.DefaultPath; // 탐사 저장 경로 보관
        } // 탐사 기본 경로 설정 종료
        menuView.Bind(ShowNewGameWarning, OpenSaveMenu, OpenSettings, QuitGame, CanContinue()); // 저장 상태에 맞춰 메뉴 동작 연결
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
        string previousExploration = explorationSavePath ?? ExplorationSaveService.DefaultPath; // 이전 탐사 저장 경로 결정
        if (!ExplorationSaveService.Remove(previousExploration)) // 이전 탐사 저장 제거 확인
        { // 저장 제거 실패 처리 시작
            Debug.LogWarning("[MainMenu] 이전 탐사 저장을 정리하지 못했습니다."); // 새 게임 중단 안내
            return; // 이전 저장 유지 상태에서 시작 중단
        } // 저장 제거 실패 처리 종료
        PlayerResourceManager.Instance?.ResetResources(); // 이전 게임 자원 초기화
        FacilityUpgradeManager.Instance?.ResetFacilities(); // 이전 게임 설비 초기화
        CharacterProgressionManager.Instance?.ResetProgress(); // 이전 게임 성장 초기화
        CharacterAffinityManager.Instance?.ResetAffinity(); // 이전 게임 호감도 초기화
        CharacterRecoveryManager.Instance?.ResetRecoveryState(); // 이전 게임 회복 상태 초기화
        PartyPresetStore.Reset(); // 이전 게임 편성 초기화
        ExpeditionZoneProgress.Reset(); // 새 게임에서 이전 슬롯 탐사 진척과 식별 코드 초기화
        BattleResultManager.Instance?.DiscardPendingResult(); // 이전 전투 결과 제거
        BattleResultManager.Instance?.ResetSavedPartyState(); // 이전 게임 파티 상태 초기화
        ResetRunState(); // 이전 탐사 회차 상태 정리
        SceneFlowManager.Instance.LoadScene("20_Lobby"); // 거점 씬 이동
    } // 게임 시작 종료
    private SaveSlotService CreateSlotService() // 메뉴 저장 경로의 슬롯 서비스 생성
    { // 슬롯 서비스 생성 시작
        string directory = savePath == LobbySaveService.DefaultPath ? Path.Combine(Application.persistentDataPath, "save-slots") : savePath + ".slots"; // 기본 또는 독립 테스트 슬롯 경로 선택
        return new SaveSlotService(directory, savePath, explorationSavePath); // 메뉴 저장 경로 연결
    } // 슬롯 서비스 생성 종료
    private void OpenSaveMenu() // 저장 슬롯 화면 열기
    { // 슬롯 화면 열기 시작
        Canvas canvas = menuView.GetComponentInParent<Canvas>(); // 메인 메뉴 캔버스 조회
        if (canvas == null || FindFirstObjectByType<SaveMenuController>() != null) // 캔버스와 중복 화면 확인
        { // 화면 생성 불가 처리 시작
            return; // 슬롯 화면 열기 중단
        } // 화면 생성 불가 처리 종료
        SaveSlotService service = CreateSlotService(); // 메뉴 슬롯 저장 서비스 생성
        SaveMenuController slotMenu = SaveMenuController.Open(canvas, service, false, false, index => SaveSlotNavigator.Load(service, index)); // 메인 메뉴 슬롯 화면 표시
        slotMenu.Closed += RefreshMenu; // 저장 화면 닫기 후 메뉴 상태 갱신
    } // 슬롯 화면 열기 종료
    private void RefreshMenu() // 저장 변경 후 메뉴 상태 갱신
    { // 메뉴 상태 갱신 시작
        menuView.Bind(ShowNewGameWarning, OpenSaveMenu, OpenSettings, QuitGame, CanContinue()); // 저장 상태별 이어하기 동작 갱신
    } // 메뉴 상태 갱신 종료
    public void ContinueGame() // 저장된 게임 이어하기
    { // 이어하기 시작
        if (SaveFileStorage.IsUnsupportedVersion(savePath) || SaveFileStorage.IsUnsupportedVersion(explorationSavePath)) // 지원하지 않는 새 저장 버전 확인
        { // 새 저장 버전 보호 시작
            Debug.LogWarning("[MainMenu] 지원하지 않는 저장 버전입니다."); // 잘못된 이전 저장 사용 방지 안내
            return; // 이전 저장으로 임의 복원 차단
        } // 새 저장 버전 보호 종료
        bool resumeExploration = IsExplorationLatest(); // 최근 유효 탐사 저장 확인
        string targetScene = resumeExploration ? "30_Exploration" : "20_Lobby"; // 복원 대상 씬 결정
        if (SceneFlowManager.Instance == null || SceneFlowManager.Instance.IsLoadingScene || !Application.CanStreamedLevelBeLoaded(targetScene)) // 대상 씬 이동 가능 여부 확인
        { // 씬 이동 불가 처리 시작
            Debug.LogWarning("[MainMenu] 거점 화면을 열 수 없습니다."); // 씬 이동 실패 안내
            return; // 이어하기 중단
        } // 씬 이동 불가 처리 종료
        bool loaded = resumeExploration ? ExplorationSaveService.Load(explorationSavePath) : LobbySaveService.Load(savePath); // 선택한 저장 상태 복원
        if (!loaded) // 저장 상태 복원 확인
        { // 저장 복원 실패 처리 시작
            menuView.Bind(ShowNewGameWarning, OpenSaveMenu, OpenSettings, QuitGame, false); // 이어하기 버튼 비활성화
            Debug.LogWarning("[MainMenu] 저장 데이터를 불러올 수 없습니다."); // 복원 실패 안내
            return; // 이어하기 중단
        } // 저장 복원 실패 처리 종료
        if (!resumeExploration) // 거점 저장 복원 확인
        { // 이전 탐사 정리 시작
            ResetRunState(); // 이전 탐사 회차 상태 정리
        } // 이전 탐사 정리 종료
        SceneFlowManager.Instance.LoadScene(targetScene); // 복원된 게임 씬 이동
    } // 이어하기 종료
    private bool CanContinue() // 이어하기 가능 여부 확인
    { // 이어하기 가능 여부 확인 시작
        SaveSlotService service = CreateSlotService(); // 현재 저장 경로 슬롯 서비스 생성
        for (int index = 0; index < SaveMenuView.SlotCount; index++) // 전체 저장 슬롯 순회
        { // 저장 슬롯 검사 시작
            if (service.HasFile(index)) // 손상 파일을 포함한 슬롯 존재 확인
            { // 유효 슬롯 처리 시작
                return true; // 슬롯 관리 화면 진입 가능 반환
            } // 유효 슬롯 처리 종료
        } // 저장 슬롯 검사 종료
        return false; // 저장 파일이 있는 슬롯 없음 반환
    } // 이어하기 가능 여부 확인 종료
    private bool IsExplorationLatest() // 최근 유효 탐사 저장 확인
    { // 최근 탐사 저장 확인 시작
        if (!ExplorationSaveService.CanLoad(explorationSavePath)) // 유효한 탐사 저장 확인
        { // 탐사 저장 없음 처리 시작
            return false; // 탐사 복원 제외
        } // 탐사 저장 없음 처리 종료
        if (!LobbySaveService.CanLoad(savePath)) // 유효한 거점 저장 확인
        { // 거점 저장 없음 처리 시작
            return true; // 탐사 저장 우선 사용
        } // 거점 저장 없음 처리 종료
        try // 파일 시간 조회 오류 처리 시작
        { // 저장 시간 비교 시작
            SaveFileStorage.TryRead(explorationSavePath, ExplorationSaveService.CanLoadJson, out string runJson, out string runPath); // 실제 탐사 원본 또는 복구본 경로 확인
            SaveFileStorage.TryRead(savePath, LobbySaveService.CanLoadJson, out string lobbyJson, out string lobbyPath); // 실제 거점 원본 또는 복구본 경로 확인
            return File.GetLastWriteTimeUtc(runPath) >= File.GetLastWriteTimeUtc(lobbyPath); // 실제 파일 시각 기준 최근 탐사 저장 여부 반환
        } // 저장 시간 비교 종료
        catch (IOException) // 저장 시간 읽기 오류 확인
        { // 시간 읽기 오류 처리 시작
            return false; // 거점 저장 사용
        } // 시간 읽기 오류 처리 종료
        catch (System.UnauthorizedAccessException) // 저장 시간 접근 오류 확인
        { // 시간 접근 오류 처리 시작
            return false; // 거점 저장 사용
        } // 시간 접근 오류 처리 종료
        catch (System.ArgumentException) // 저장 경로 변경 오류 확인
        { // 저장 경로 오류 처리 시작
            return false; // 거점 저장 사용
        } // 저장 경로 오류 처리 종료
    } // 최근 탐사 저장 확인 종료
    public static void ResetRunState() // 탐사 회차 상태 정리
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
