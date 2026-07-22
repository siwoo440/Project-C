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

    [Header("씬 전환 UI")] // 씬 전환 UI 구분
    [SerializeField] private SceneTransitionUI transitionUI; // 공용 씬 전환 화면
    [SerializeField, Min(0f)] private float minimumLoadingDuration = 0.35f; // 최소 로딩 표시 시간


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

        if (transitionUI != null) // 전환 화면 연결 확인
        {
            transitionUI.SetProgress(0f); // 진행률 초기화
            yield return transitionUI.FadeIn(); // 로딩 화면 표시 대기
        }
        else // 전환 화면 누락 처리
        {
            Debug.LogWarning("SceneTransitionUI가 연결되지 않았습니다.", this); // 참조 누락 경고
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // 비동기 씬 로딩 생성

        if (loadOperation == null) // 로딩 작업 생성 확인
        {
            if (transitionUI != null) // 전환 화면 연결 확인
            {
                transitionUI.HideImmediate(); // 로딩 화면 즉시 숨김
            }

            IsLoading = false; // 로딩 상태 해제
            Debug.LogError($"씬 로딩 작업 생성 실패: {sceneName}", this); // 생성 실패 출력
            yield break; // 코루틴 종료
        }

        loadOperation.allowSceneActivation = false; // 자동 씬 활성화 보류
        float loadingElapsedTime = 0f; // 로딩 경과 시간 초기화

        while (loadOperation.progress < 0.9f) // 씬 활성화 준비 상태 대기
        {
            loadingElapsedTime += Time.unscaledDeltaTime; // 실제 로딩 시간 누적
            float normalizedProgress = Mathf.Clamp01(loadOperation.progress / 0.9f); // 표시용 진행률 계산

            if (transitionUI != null) // 전환 화면 연결 확인
            {
                transitionUI.SetProgress(normalizedProgress); // 로딩 진행률 표시
            }

            yield return null; // 다음 프레임 대기
        }

        float remainingDuration = Mathf.Max(0f, minimumLoadingDuration - loadingElapsedTime); // 남은 최소 표시 시간 계산

        if (remainingDuration > 0f) // 추가 표시 시간 확인
        {
            yield return new WaitForSecondsRealtime(remainingDuration); // 최소 표시 시간 대기
        }

        if (transitionUI != null) // 전환 화면 연결 확인
        {
            transitionUI.SetProgress(1f); // 진행률 완료 표시
        }

        loadOperation.allowSceneActivation = true; // 대상 씬 활성화 허용
        yield return loadOperation; // 대상 씬 활성화 완료 대기
        yield return null; // 새 씬 첫 프레임 대기

        if (transitionUI != null) // 전환 화면 연결 확인
        {
            yield return transitionUI.FadeOut(); // 로딩 화면 숨김 대기
        }

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