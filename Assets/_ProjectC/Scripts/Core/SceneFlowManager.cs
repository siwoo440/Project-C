using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    public string CurrentSceneName => SceneManager.GetActiveScene().name;
    public int CurrentSceneBuildIndex => SceneManager.GetActiveScene().buildIndex;
    public string PreviousSceneName { get; private set; }
    public bool IsLoadingScene { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) // 기존 SceneFlowManager 확인
        {
            Destroy(gameObject); // 중복 관리자 제거
            return; // 중복 초기화 중단
        }

        Instance = this; // 현재 인스턴스 등록
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded; // 씬 로드 이벤트 등록
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded; // 씬 로드 이벤트 해제
    }

    public void LoadScene(string sceneName)
    {
        if (IsLoadingScene) // 씬 전환 진행 여부 확인
        {
            return; // 중복 씬 전환 중단
        }

        if (string.IsNullOrWhiteSpace(sceneName)) // 씬 이름 유효성 확인
        {
            Debug.LogError("[SceneFlowManager] 씬 이름이 비어 있습니다."); // 잘못된 씬 이름 출력
            return; // 잘못된 씬 전환 중단
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName)) // Build 목록 등록 여부 확인
        {
            Debug.LogError($"[SceneFlowManager] Build 목록에서 씬을 찾을 수 없습니다: {sceneName}"); // 미등록 씬 출력
            return; // 미등록 씬 전환 중단
        }

        PreviousSceneName = CurrentSceneName; // 이전 씬 이름 저장
        IsLoadingScene = true; // 씬 전환 상태 시작
        SceneManager.LoadScene(sceneName); // 이름 기준 씬 로드
    }

    public void LoadScene(int buildIndex)
    {
        if (IsLoadingScene) // 씬 전환 진행 여부 확인
        {
            return; // 중복 씬 전환 중단
        }

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings) // Build Index 범위 확인
        {
            Debug.LogError($"[SceneFlowManager] 잘못된 Build Index입니다: {buildIndex}"); // 잘못된 인덱스 출력
            return; // 잘못된 씬 전환 중단
        }

        PreviousSceneName = CurrentSceneName; // 이전 씬 이름 저장
        IsLoadingScene = true; // 씬 전환 상태 시작
        SceneManager.LoadScene(buildIndex); // 인덱스 기준 씬 로드
    }

    public void LoadPreviousScene()
    {
        if (string.IsNullOrWhiteSpace(PreviousSceneName)) // 이전 씬 존재 여부 확인
        {
            Debug.LogWarning("[SceneFlowManager] 돌아갈 이전 씬이 없습니다."); // 이전 씬 없음 출력
            return; // 이전 씬 이동 중단
        }

        string targetSceneName = PreviousSceneName; // 복귀 대상 씬 저장
        LoadScene(targetSceneName); // 이전 씬 로드
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        IsLoadingScene = false; // 씬 전환 상태 종료
        Debug.Log($"[SceneFlowManager] 씬 로드 완료: {scene.name}"); // 현재 씬 확인 로그
    }

    private void OnDestroy()
    {
        if (Instance == this) // 현재 인스턴스 확인
        {
            Instance = null; // 인스턴스 참조 해제
        }
    }

#if UNITY_EDITOR
    [ContextMenu("테스트/00_Boot 로드")]
    private void TestLoadBoot()
    {
        LoadScene("00_Boot"); // 부트 씬 전환 테스트
    }

    [ContextMenu("테스트/10_MainMenu 로드")]
    private void TestLoadMainMenu()
    {
        LoadScene("10_MainMenu"); // 메인 메뉴 전환 테스트
    }

    [ContextMenu("테스트/20_Lobby 로드")]
    private void TestLoadLobby()
    {
        LoadScene("20_Lobby"); // 로비 전환 테스트
    }

    [ContextMenu("테스트/30_Exploration 로드")]
    private void TestLoadExploration()
    {
        LoadScene("30_Exploration"); // 탐사 전환 테스트
    }

    [ContextMenu("테스트/40_Battle 로드")]
    private void TestLoadBattle()
    {
        LoadScene("40_Battle"); // 전투 전환 테스트
    }

    [ContextMenu("테스트/90_Settings 로드")]
    private void TestLoadSettings()
    {
        LoadScene("90_Settings"); // 설정 전환 테스트
    }
#endif
}
