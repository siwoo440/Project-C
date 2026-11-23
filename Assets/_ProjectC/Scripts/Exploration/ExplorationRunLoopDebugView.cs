using UnityEngine; // 런타임 정산 UI와 오브젝트 기능 사용
using UnityEngine.SceneManagement; // 현재 Scene과 Scene 이동 기능 사용
using UnityEngine.UI; // 결과 패널 이미지 사용
using TMPro; // 결과 안내 문구 사용

public sealed class ExplorationRunLoopDebugView : MonoBehaviour // 45일차 탐사·거점 순환 UI
{
    private const string ExplorationSceneName = "30_Exploration"; // 탐사 Scene 이름
    private const string LobbySceneName = "20_Lobby"; // 거점 Scene 이름

    private static ExplorationRunLoopDebugView instance; // 런 루프 UI 인스턴스
    private bool sceneLoadRequested; // 중복 Scene 이동 방지
    private GameObject resultPanel; // 현재 탐사 결과 화면

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

    private void Update() // 탐사 종료 결과 화면 표시
    { // 탐사 결과 화면 확인 시작
        ExplorationSessionManager session = ExplorationSessionManager.Instance; // 현재 탐사 세션 조회
        if (SceneManager.GetActiveScene().name != ExplorationSceneName || session == null || !session.IsExplorationCompleted || resultPanel != null) // 탐사 종료와 화면 표시 상태 확인
        { // 화면 생성 대기 시작
            return; // 중복 화면 생성 방지
        } // 화면 생성 대기 종료
        GameObject canvasObject = new GameObject("ExplorationResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 결과 전용 캔버스 생성
        Canvas canvas = canvasObject.GetComponent<Canvas>(); // 결과 전용 캔버스 조회
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 결과 표시 설정
        canvas.sortingOrder = 6000; // 탐사 화면 위와 로딩 화면 아래 배치
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 결과 화면 배율 조회
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기준 배율 설정
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 결과 화면 기준 해상도 설정
        scaler.matchWidthOrHeight = 0.5f; // 가로와 세로 배율 균형 설정
        RectTransform overlay = PreparationUi.CreateOverlay(canvas, "ExplorationResult"); // 결과 전체 화면 생성
        resultPanel = overlay.gameObject; // 현재 결과 화면 보관
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 어두운 결과 배경 설정
        RectTransform backdrop = PreparationUi.CreatePanel("Backdrop", overlay, Vector2.zero, Vector2.one); // 결과 배경 영역 생성
        PreparationUi.Place(backdrop, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 480f)); // 결과 배경 크기와 위치 설정
        WarningUiArt.ApplyBackdrop(backdrop.GetComponent<Image>()); // 공통 결과 배경 이미지 적용
        string titleText = session.IsExplorationSuccess ? "탐사 성공" : "탐사 실패"; // 결과별 제목 결정
        TMP_Text title = PreparationUi.CreateText("Title", overlay, titleText, 42f); // 탐사 결과 제목 생성
        PreparationUi.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(850f, 80f)); // 결과 제목 위치 설정
        string detailText = session.IsExplorationSuccess ? $"완료 층 {session.CompletedFloor}F · 클리어 조우 {session.CompletedEncounterCount}개\n호감도 +{session.LastExplorationSuccessAffinity}" : session.LastExplorationFailureReason + "\n사망한 캐릭터는 거점에서 회복합니다."; // 성공 정산과 실패 회복 안내 결정
        TMP_Text detail = PreparationUi.CreateText("Detail", overlay, detailText, 27f); // 결과 상세 안내 생성
        PreparationUi.Place(detail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(850f, 120f)); // 상세 안내 위치 설정
        PreparationUi.CreateButton("ReturnButton", overlay, "거점으로 복귀", new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(360f, 100f), ReturnToLobby); // 성공과 실패 공통 복귀 버튼 생성
    } // 탐사 결과 화면 확인 종료

    private void ReturnToLobby() // 탐사 결과 유지 후 거점 이동
    { // 거점 복귀 시작
        ExplorationSessionManager sessionManager = ExplorationSessionManager.Instance; // 탐사 종료 상태 조회
        if (sceneLoadRequested || sessionManager == null || !sessionManager.IsExplorationCompleted) // 중복 요청과 탐사 종료 상태 확인
        { // 잘못된 복귀 요청 처리 시작
            return; // 복귀 요청 중단
        } // 잘못된 복귀 요청 처리 종료
        if (!ExplorationAutoSave.SaveAfterBattle(Vector3.zero)) // 거점 복귀 전 종료 상태 저장 확인
        { // 저장 실패 처리 시작
            Debug.LogWarning("[ExplorationAutoSave] 거점 복귀 전 상태를 저장하지 못했습니다."); // 저장 실패 안내
        } // 저장 실패 처리 종료
        Debug.Log($"[Exploration] 거점 복귀 - {sessionManager.CompletedFloor}F / 조우 {sessionManager.CompletedEncounterCount}개 / 골드 +{sessionManager.RunGoldGained}"); // 거점 복귀 정산 로그
        LoadScene(LobbySceneName); // 탐사 결과를 유지한 채 거점 이동
    } // 거점 복귀 종료

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
