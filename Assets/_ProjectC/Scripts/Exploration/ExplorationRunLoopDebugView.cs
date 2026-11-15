using UnityEngine; // 런타임 정산 UI와 오브젝트 기능 사용
using UnityEngine.SceneManagement; // 현재 Scene과 Scene 이동 기능 사용

public sealed class ExplorationRunLoopDebugView : MonoBehaviour // 45일차 탐사·거점 순환 UI
{
    private const string ExplorationSceneName = "30_Exploration"; // 탐사 Scene 이름
    private const string LobbySceneName = "20_Lobby"; // 거점 Scene 이름

    private static ExplorationRunLoopDebugView instance; // 런 루프 UI 인스턴스
    private bool sceneLoadRequested; // 중복 Scene 이동 방지

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // 게임 시작 전 런 루프 UI 준비
    {
        if (instance != null)
        {
            return;
        }

        ExplorationRunLoopDebugView existing =
            FindFirstObjectByType<ExplorationRunLoopDebugView>(); // 기존 런 루프 UI 탐색

        if (existing != null)
        {
            instance = existing; // 기존 인스턴스 등록
            return;
        }

        GameObject viewObject =
            new GameObject("ExplorationRunLoopDebugView"); // 영구 런 루프 UI 오브젝트 생성

        instance =
            viewObject.AddComponent<ExplorationRunLoopDebugView>(); // 런 루프 UI 컴포넌트 추가

        DontDestroyOnLoad(viewObject); // Scene 전환 후에도 정산 UI 유지
    }

    private void Awake() // 런 루프 UI 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 런 루프 UI 제거
            return;
        }

        instance = this; // 현재 런 루프 UI 등록
        DontDestroyOnLoad(gameObject); // Scene 전환 상태 유지
    }

    private void OnEnable() // Scene 로드 이벤트 등록
    {
        SceneManager.sceneLoaded +=
            HandleSceneLoaded; // Scene 로드 완료 이벤트 등록
    }

    private void OnDisable() // Scene 로드 이벤트 해제
    {
        SceneManager.sceneLoaded -=
            HandleSceneLoaded; // Scene 로드 완료 이벤트 해제
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode) // Scene 이동 완료 처리
    {
        sceneLoadRequested = false; // 다음 Scene 이동 허용
    }

    private void OnGUI() // 탐사 성공 후 거점 복귀 화면 출력
    { // 탐사 복귀 화면 확인 시작
        string sceneName = SceneManager.GetActiveScene().name; // 현재 씬 이름 조회
        if (sceneName == ExplorationSceneName) // 탐사 씬 여부 확인
        { // 탐사 복귀 화면 출력 시작
            DrawExplorationReturnPanel(); // 탐사 성공 후 거점 복귀 화면 출력
        } // 탐사 복귀 화면 출력 종료
    } // 탐사 복귀 화면 확인 종료

    private void DrawExplorationReturnPanel() // 탐사 성공 후 거점 복귀 패널
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.Instance; // 현재 탐사 세션 조회

        if (sessionManager == null ||
            !sessionManager.IsExplorationCompleted ||
            !sessionManager.IsExplorationSuccess)
        {
            return;
        }

        Rect panelRect =
            new Rect(
                Screen.width * 0.5f - 260f,
                Screen.height * 0.5f + 170f,
                520f,
                130f); // 성공 화면 하단 복귀 영역 계산

        GUILayout.BeginArea(
            panelRect,
            GUI.skin.window); // 거점 복귀 패널 시작

        GUILayout.Label(
            "45일차 - 탐사 결과를 유지한 채 거점으로 복귀합니다."); // 복귀 안내 출력

        GUI.enabled =
            !sceneLoadRequested; // Scene 이동 중 버튼 중복 입력 차단

        if (GUILayout.Button(
                "거점으로 복귀",
                GUILayout.Height(48f)))
        {
            ReturnToLobby(); // 탐사 결과를 유지하고 Lobby 이동
        }

        GUI.enabled = true; // GUI 활성 상태 복구
        GUILayout.EndArea(); // 거점 복귀 패널 종료
    }

    private void ReturnToLobby() // 탐사 성공 결과 유지 후 거점 이동
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.Instance; // 탐사 성공 상태 확인

        if (sessionManager == null ||
            !sessionManager.IsExplorationCompleted ||
            !sessionManager.IsExplorationSuccess)
        {
            return;
        }

        Debug.Log(
            $"[Exploration][Day45] 거점 복귀 - " +
            $"{sessionManager.CompletedFloor}F / " +
            $"조우 {sessionManager.CompletedEncounterCount}개 / " +
            $"Gold +{sessionManager.RunGoldGained}"); // 거점 복귀 정산 로그

        LoadScene(
            LobbySceneName); // 탐사 결과를 초기화하지 않고 Lobby 이동
    }

    private void LoadScene(
        string sceneName) // SceneFlowManager 우선 Scene 이동
    {
        if (sceneLoadRequested)
        {
            return;
        }

        sceneLoadRequested = true; // 중복 Scene 이동 요청 차단

        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(
                sceneName); // 기존 SceneFlowManager를 통한 이동
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            sceneLoadRequested = false; // 실패 후 재시도 허용

            Debug.LogError(
                $"[Exploration][Day45] Build 목록에서 Scene을 찾을 수 없습니다: {sceneName}"); // Scene 누락 오류

            return;
        }

        SceneManager.LoadScene(
            sceneName); // 직접 Scene 테스트용 대체 이동
    }

    private void OnDestroy() // 런 루프 UI 제거 처리
    {
        if (instance == this)
        {
            instance = null; // 정적 인스턴스 해제
        }
    }
}
