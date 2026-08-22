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

    private void OnGUI() // 탐사 성공·거점 정산 개발 UI 출력
    {
        string sceneName =
            SceneManager.GetActiveScene().name; // 현재 Scene 이름 조회

        if (sceneName == ExplorationSceneName)
        {
            DrawExplorationReturnPanel(); // 탐사 성공 후 거점 복귀 UI 출력
            return;
        }

        if (sceneName == LobbySceneName)
        {
            DrawLobbySettlementPanel(); // 거점 정산·다음 탐사 UI 출력
        }
    }

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

    private void DrawLobbySettlementPanel() // Lobby 정산과 다음 탐사 패널
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        CharacterProgressionManager progressionManager =
            CharacterProgressionManager.EnsureInstance(); // 영구 성장 관리자 준비

        CharacterAffinityManager affinityManager =
            CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance(); // 영구 자원 관리자 준비

        Rect panelRect =
            new Rect(
                Screen.width - 610f,
                55f,
                570f,
                610f); // 오른쪽 거점 정산 영역 계산

        GUILayout.BeginArea(
            panelRect,
            GUI.skin.window); // 거점 정산 패널 시작

        GUILayout.Label(
            "45일차 - 탐사 정산 / 거점 루프"); // 정산 패널 제목 출력

        GUILayout.Space(8f); // 제목 여백 추가

        if (sessionManager.IsExplorationCompleted &&
            sessionManager.IsExplorationSuccess)
        {
            DrawCompletedRunSummary(
                sessionManager); // 지난 탐사 성공 정산 출력
        }
        else
        {
            GUILayout.Label(
                "정산할 완료 탐사가 없습니다."); // 신규 게임 또는 새 탐사 준비 상태 출력

            GUILayout.Label(
                "새 탐사를 시작하면 1F와 새 Seed로 시작합니다."); // 새 탐사 안내 출력
        }

        GUILayout.Space(12f); // 정산과 영구 상태 사이 여백 추가

        GUILayout.Label(
            "현재 영구 진행"); // 영구 진행 제목 출력

        GUILayout.Label(
            $"캐릭터 Lv.{progressionManager.Level}  " +
            $"EXP {progressionManager.CurrentExperience}" +
            $"/{progressionManager.RequiredExperience}"); // 현재 캐릭터 성장 출력

        GUILayout.Label(
            $"호감도 {affinityManager.Affinity}"); // 현재 누적 호감도 출력

        GUILayout.Label(
            $"Gold {resourceManager.Gold}"); // 현재 보유 Gold 출력

        GUILayout.Label(
            $"나사 {resourceManager.Screw} / " +
            $"철판 {resourceManager.IronPlate} / " +
            $"전선 {resourceManager.Wire}"); // 현재 보유 재료 출력

        GUILayout.Space(12f); // 시설 안내 전 여백 추가

        GUILayout.Label(
            "F7 : 기존 Facility Upgrade 화면"); // 기존 설비 강화 기능 안내

        GUILayout.Label(
            "정산 자원을 사용해 시설을 강화한 뒤 다음 탐사를 시작합니다."); // 거점 성장 루프 안내

        GUILayout.Space(12f); // 다음 탐사 버튼 전 여백 추가

        GUI.enabled =
            !sceneLoadRequested; // Scene 이동 중 중복 입력 차단

        string startButtonLabel =
            sessionManager.IsExplorationCompleted
                ? "정산 완료 · 다음 탐사 시작"
                : "새 탐사 시작"; // 현재 상태별 시작 버튼 문구 결정

        if (GUILayout.Button(
                startButtonLabel,
                GUILayout.Height(52f)))
        {
            StartNewExploration(); // 런 초기화 후 새 탐사 이동
        }

        GUI.enabled = true; // GUI 활성 상태 복구
        GUILayout.EndArea(); // 거점 정산 패널 종료
    }

    private static void DrawCompletedRunSummary(
        ExplorationSessionManager sessionManager) // 완료 탐사 정산 정보 출력
    {
        GUILayout.Label(
            "탐사 성공"); // 성공 결과 제목 출력

        GUILayout.Label(
            $"최종 도달 층 : {sessionManager.CompletedFloor}F"); // 완료 층 출력

        GUILayout.Label(
            $"클리어 조우 : {sessionManager.CompletedEncounterCount}개"); // 클리어 조우 수 출력

        GUILayout.Space(6f); // 보상 구역 여백 추가

        GUILayout.Label(
            "이번 탐사 실제 획득량"); // 탐사 보상 합계 제목 출력

        GUILayout.Label(
            $"EXP +{sessionManager.RunExperienceGained}"); // 런 경험치 합계 출력

        GUILayout.Label(
            $"Gold +{sessionManager.RunGoldGained}"); // 런 골드 합계 출력

        GUILayout.Label(
            $"나사 +{sessionManager.RunScrewGained}"); // 런 나사 합계 출력

        GUILayout.Label(
            $"철판 +{sessionManager.RunIronPlateGained}"); // 런 철판 합계 출력

        GUILayout.Label(
            $"전선 +{sessionManager.RunWireGained}"); // 런 전선 합계 출력

        GUILayout.Label(
            $"호감도 +{sessionManager.LastExplorationSuccessAffinity}"); // 탐사 성공 호감도 출력
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

    private void StartNewExploration() // 다음 탐사 상태 초기화 후 이동
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        sessionManager.ResetExploration(); // 지난 런 상태와 정산 합계 초기화

        Debug.Log(
            "[Exploration][Day45] 거점에서 다음 탐사를 시작합니다."); // 새 탐사 시작 로그

        LoadScene(
            ExplorationSceneName); // 새 1F 탐사 Scene 이동
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
