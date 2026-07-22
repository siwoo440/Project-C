using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 관리 기능
using System.Collections; // 코루틴 열거자 기능
[DefaultExecutionOrder(-900)] // GameManager 다음 실행 순서
public sealed class SceneFlowManager : MonoBehaviour // 씬 흐름 관리자
{
    public const string BootSceneName = "00_Boot"; // 부트 씬 이름
    public const string MainMenuSceneName = "01_MainMenu"; // 메인 메뉴 씬 이름
    public const string LobbySceneName = "02_Lobby"; // 로비 씬 이름
    public const string SettingsSceneName = "03_Settings"; // 설정 씬 이름
    public const string ExpeditionSceneName = "10_Expedition"; // 탐사 씬 이름
    public const string BattleSceneName = "20_Battle"; // 전투 씬 이름

    public static SceneFlowManager Instance { get; private set; } // 싱글턴 인스턴스

    public string CurrentSceneName => SceneManager.GetActiveScene().name; // 현재 씬 이름
    public bool IsLoading { get; private set; } // 씬 로딩 진행 상태
    private void Awake() // 오브젝트 생성 초기화
    {
        if (Instance != null && Instance != this) // 기존 관리자 존재 확인
        {
            Debug.LogWarning("중복된 SceneFlowManager를 제거합니다.", this); // 중복 경고 출력
            Destroy(gameObject); // 중복 관리자 제거
            return; // 중복 초기화 중단
        }

        Instance = this; // 현재 관리자 등록
        Debug.Log($"SceneFlowManager 초기화 완료: {CurrentSceneName}", this); // 현재 씬 출력
    }

    public bool IsSceneRegistered(string sceneName) // 씬 등록 여부 확인
    {
        return Application.CanStreamedLevelBeLoaded(sceneName); // 등록 결과 반환
    }

    public void LoadMainMenu() // 메인 메뉴 이동 요청
    {
        LoadScene(MainMenuSceneName); // 메인 메뉴 로딩 실행
    }

    public void LoadLobby() // 로비 이동 요청
    {
        LoadScene(LobbySceneName); // 로비 로딩 실행
    }

    public void LoadSettings() // 설정 화면 이동 요청
    {
        LoadScene(SettingsSceneName); // 설정 화면 로딩 실행
    }

    public void LoadScene(string sceneName) // 지정 씬 이동 요청
    {
        if (IsLoading) // 기존 로딩 진행 확인
        {
            Debug.LogWarning($"씬 로딩 중 요청 무시: {sceneName}", this); // 중복 요청 경고
            return; // 중복 로딩 중단
        }

        if (CurrentSceneName == sceneName) // 현재 씬과 대상 씬 비교
        {
            Debug.LogWarning($"현재 씬과 동일한 이동 요청: {sceneName}", this); // 동일 씬 요청 경고
            return; // 동일 씬 로딩 중단
        }

        if (!IsSceneRegistered(sceneName)) // 빌드 등록 상태 확인
        {
            Debug.LogError($"Build Profiles에 등록되지 않은 씬: {sceneName}", this); // 미등록 씬 오류
            return; // 미등록 씬 로딩 중단
        }

        StartCoroutine(LoadSceneRoutine(sceneName)); // 비동기 로딩 코루틴 시작
    }

    private IEnumerator LoadSceneRoutine(string sceneName) // 비동기 씬 로딩 처리
    {
        IsLoading = true; // 로딩 진행 상태 설정
        Debug.Log($"씬 로딩 시작: {sceneName}", this); // 로딩 시작 출력

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // 비동기 씬 로딩 생성

        if (loadOperation == null) // 로딩 작업 생성 확인
        {
            IsLoading = false; // 로딩 상태 해제
            Debug.LogError($"씬 로딩 작업 생성 실패: {sceneName}", this); // 생성 실패 출력
            yield break; // 코루틴 종료
        }

        yield return loadOperation; // 씬 로딩 완료 대기

        IsLoading = false; // 로딩 진행 상태 해제
        Debug.Log($"씬 로딩 완료: {CurrentSceneName}", this); // 로딩 완료 출력
    }

    [ContextMenu("등록 씬 점검")] // Inspector 점검 메뉴
    private void ValidateRegisteredScenes() // 전체 씬 등록 점검
    {
        LogSceneRegistration(BootSceneName); // 부트 씬 점검
        LogSceneRegistration(MainMenuSceneName); // 메인 메뉴 씬 점검
        LogSceneRegistration(LobbySceneName); // 로비 씬 점검
        LogSceneRegistration(SettingsSceneName); // 설정 씬 점검
        LogSceneRegistration(ExpeditionSceneName); // 탐사 씬 점검
        LogSceneRegistration(BattleSceneName); // 전투 씬 점검
    }

    private void LogSceneRegistration(string sceneName) // 개별 씬 등록 출력
    {
        bool isRegistered = IsSceneRegistered(sceneName); // 등록 상태 확인
        Debug.Log($"{sceneName} 등록 상태: {isRegistered}", this); // 등록 결과 출력
    }

    private void OnDestroy() // 오브젝트 제거 처리
    {
        if (Instance == this) // 현재 인스턴스 일치 확인
        {
            Instance = null; // 싱글턴 참조 해제
        }
    }
}