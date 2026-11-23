using System; // 현재 시간 기반 시드 생성
using UnityEngine; // 런타임 오브젝트 기능 사용
using UnityEngine.InputSystem; // 디버그 재생성 입력 사용
using UnityEngine.SceneManagement; // 탐사 Scene 감지 기능 사용

public sealed class ExplorationMapRuntime : MonoBehaviour // 37일차 절차 탐사 맵과 층 진행 런타임
{
    private const int DefaultCellCount = 14; // 기본 생성 셀 수
    private const float WorldMin = -3.4f; // 임시 맵 월드 최소 좌표
    private const float WorldMax = 3.4f; // 임시 맵 월드 최대 좌표
    private const float FloorChangeCooldown = 0.5f; // 연속 층 이동 방지 시간

    private static Sprite runtimeSquareSprite; // 임시 계단 표시 스프라이트

    private ExplorationSessionManager sessionManager; // 탐사 세션 관리자
    private GameObject stairsObject; // 현재 계단 오브젝트
    private float nextFloorChangeAllowedTime; // 다음 층 이동 허용 시각
    private bool initialPlayerPlacementHandled; // 첫 플레이어 위치 처리 여부

    public ExplorationMapData CurrentMap { get; private set; } // 현재 생성된 논리 맵
    public int CurrentFloor => sessionManager != null ? sessionManager.CurrentFloor : 1; // 현재 층 조회

    private void Awake() // 탐사 맵 런타임 초기화
    {
        sessionManager = ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비
        EnsureRuntimeSquareSprite(); // 임시 계단 스프라이트 준비
        GenerateNewMap(); // 최초 절차 맵 생성
        EnsureDebugView(); // 디버그 맵 화면 추가
    }

    private void Start() // Scene 시작 후 플레이어 초기 위치 처리
    {
        TryHandleInitialPlayerPlacement(); // 시작 셀 위치 배치 시도
    }

    private void Update() // 디버그 입력과 초기 배치 처리
    {
        TryHandleInitialPlayerPlacement(); // 플레이어 생성 지연 대응

        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회

        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) // F9 재생성 입력 확인
        {
            GenerateNewMap(); // 현재 층 맵만 새로 생성
        }
    }

    public void GenerateNewMap() // 새 탐사 논리 맵 생성
    {
        int seed = unchecked((int)DateTime.UtcNow.Ticks ^ Time.frameCount); // 현재 시점 기반 임시 시드 생성
        CurrentMap = ExplorationMapGenerator.Generate(DefaultCellCount, seed); // 기본 셀 수 절차 맵 생성
        RefreshStairs(); // 새 계단 위치 갱신

        Debug.Log(
            $"[Exploration][Day37] 논리 맵 생성 완료 - " +
            $"Floor {CurrentFloor}F / " +
            $"Seed {CurrentMap.Seed} / " +
            $"Cells {CurrentMap.Cells.Count} / " +
            $"Start {CurrentMap.StartCoordinate} / " +
            $"Stairs {CurrentMap.StairsCoordinate}"); // 생성 결과 로그 출력
    }

    public bool TryDescendFloor(ExplorationPlayerController player) // 계단을 통한 다음 층 이동 시도
    {
        if (player == null || Time.time < nextFloorChangeAllowedTime) // 플레이어와 연속 실행 여부 확인
        {
            return false; // 층 이동 실패 반환
        }

        nextFloorChangeAllowedTime = Time.time + FloorChangeCooldown; // 다음 실행 대기 시간 설정
        sessionManager.AdvanceFloor(); // 현재 탐사 층 증가
        GenerateNewMap(); // 다음 층용 새 논리 맵 생성
        MovePlayerToStart(player); // 새 층 시작 셀로 플레이어 이동

        Debug.Log(
            $"[Exploration][Day37] 계단 이동 완료 - {CurrentFloor}F"); // 계단 이동 완료 로그

        return true; // 층 이동 성공 반환
    }

    public Vector2 GetWorldPosition(Vector2Int coordinate) // 논리 셀 좌표를 임시 월드 좌표로 변환
    {
        if (CurrentMap == null || CurrentMap.Cells.Count == 0) // 현재 맵 존재 확인
        {
            return Vector2.zero; // 맵 미생성 시 원점 반환
        }

        GetMapBounds(
            out int minX,
            out int maxX,
            out int minY,
            out int maxY); // 현재 맵 좌표 범위 계산

        float normalizedX = maxX == minX
            ? 0.5f
            : Mathf.InverseLerp(minX, maxX, coordinate.x); // X 좌표 정규화

        float normalizedY = maxY == minY
            ? 0.5f
            : Mathf.InverseLerp(minY, maxY, coordinate.y); // Y 좌표 정규화

        return new Vector2(
            Mathf.Lerp(WorldMin, WorldMax, normalizedX),
            Mathf.Lerp(WorldMin, WorldMax, normalizedY)); // 플레이 가능 범위 내 월드 위치 반환
    }

    private void TryHandleInitialPlayerPlacement() // 최초 탐사 진입 플레이어 시작 위치 처리
    {
        if (initialPlayerPlacementHandled) // 이미 초기 배치 완료 여부 확인
        {
            return; // 중복 배치 방지
        }

        ExplorationPlayerController player =
            FindFirstObjectByType<ExplorationPlayerController>(); // 현재 탐사 플레이어 조회

        if (player == null) // 플레이어 생성 완료 여부 확인
        {
            return; // 다음 프레임 재시도
        }

        initialPlayerPlacementHandled = true; // 초기 배치 처리 완료 기록

        if (sessionManager.HasReturnPosition) // 전투 후 복귀 여부 확인
        {
            return; // 기존 전투 복귀 위치 유지
        }

        MovePlayerToStart(player); // 새 탐사 시작 셀로 이동
    }

    private void MovePlayerToStart(ExplorationPlayerController player) // 플레이어를 현재 맵 시작 셀로 이동
    {
        if (player == null || CurrentMap == null) // 필요한 데이터 존재 확인
        {
            return; // 이동 처리 중단
        }

        Vector2 startWorldPosition =
            GetWorldPosition(CurrentMap.StartCoordinate); // 시작 셀 월드 위치 계산

        player.Teleport(startWorldPosition); // 플레이어 시작 위치 이동
    }

    private void RefreshStairs() // 현재 맵 계단 오브젝트 갱신
    {
        EnsureStairsObject(); // 계단 오브젝트 존재 보장

        if (stairsObject == null || CurrentMap == null) // 계단과 맵 데이터 확인
        {
            return; // 갱신 처리 중단
        }

        Vector2 stairsWorldPosition =
            GetWorldPosition(CurrentMap.StairsCoordinate); // 계단 셀 월드 위치 계산

        stairsObject.name = $"FloorStairs_{CurrentFloor}F"; // 현재 층 계단 이름 갱신
        stairsObject.transform.position = new Vector3(
            stairsWorldPosition.x,
            stairsWorldPosition.y,
            0f); // 계단 월드 위치 갱신
    }

    private void EnsureStairsObject() // 임시 계단 오브젝트 생성 보장
    {
        if (stairsObject != null) // 기존 계단 존재 확인
        {
            return; // 재생성 생략
        }

        stairsObject = new GameObject(
            "FloorStairs",
            typeof(SpriteRenderer),
            typeof(CircleCollider2D),
            typeof(ExplorationFloorStairs)); // 계단 오브젝트 생성

        stairsObject.transform.SetParent(transform); // 런타임 오브젝트 하위 배치
        stairsObject.transform.localScale = new Vector3(
            0.8f,
            0.8f,
            1f); // 계단 임시 표시 크기 설정

        SpriteRenderer spriteRenderer =
            stairsObject.GetComponent<SpriteRenderer>(); // 계단 SpriteRenderer 조회

        spriteRenderer.sprite = runtimeSquareSprite; // 임시 사각형 스프라이트 지정
        spriteRenderer.color = new Color(
            1f,
            0.85f,
            0.15f,
            1f); // 계단 노란색 표시
        spriteRenderer.sortingOrder = 3; // 탐사 오브젝트 표시 순서 설정

        CircleCollider2D collider =
            stairsObject.GetComponent<CircleCollider2D>(); // 계단 Collider 조회

        collider.isTrigger = true; // Trigger 방식 활성화
        collider.radius = 0.55f; // 계단 접촉 범위 설정

        ExplorationFloorStairs stairs =
            stairsObject.GetComponent<ExplorationFloorStairs>(); // 계단 동작 컴포넌트 조회

        stairs.Initialize(this); // 현재 맵 런타임 연결
    }

    private void EnsureDebugView() // 임시 절차 맵 UI 존재 보장
    {
        if (GetComponent<ExplorationMapDebugView>() == null) // 디버그 화면 존재 확인
        {
            gameObject.AddComponent<ExplorationMapDebugView>(); // 디버그 화면 컴포넌트 추가
        }
    }

    private void GetMapBounds(
        out int minX,
        out int maxX,
        out int minY,
        out int maxY) // 현재 논리 맵 좌표 범위 계산
    {
        ExplorationMapCell firstCell = CurrentMap.Cells[0]; // 첫 셀 기준값 조회
        minX = firstCell.Coordinate.x; // 최소 X 초기화
        maxX = firstCell.Coordinate.x; // 최대 X 초기화
        minY = firstCell.Coordinate.y; // 최소 Y 초기화
        maxY = firstCell.Coordinate.y; // 최대 Y 초기화

        foreach (ExplorationMapCell cell in CurrentMap.Cells) // 전체 셀 순회
        {
            minX = Mathf.Min(minX, cell.Coordinate.x); // 최소 X 갱신
            maxX = Mathf.Max(maxX, cell.Coordinate.x); // 최대 X 갱신
            minY = Mathf.Min(minY, cell.Coordinate.y); // 최소 Y 갱신
            maxY = Mathf.Max(maxY, cell.Coordinate.y); // 최대 Y 갱신
        }
    }

    private static void EnsureRuntimeSquareSprite() // 임시 계단 스프라이트 존재 보장
    {
        if (runtimeSquareSprite != null) // 기존 스프라이트 존재 확인
        {
            return; // 재생성 생략
        }

        runtimeSquareSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f); // 흰색 사각형 런타임 스프라이트 생성
    }
}

public static class ExplorationMapRuntimeBootstrap // 탐사 Scene 절차 맵 자동 생성기
{
    private const string ExplorationSceneName = "30_Exploration"; // 탐사 Scene 이름

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 Scene 로드 전 자동 등록
    private static void InitializeRuntime() // Scene 로드 이벤트 초기화
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded; // 중복 Scene 이벤트 등록 제거
        SceneManager.sceneLoaded += HandleSceneLoaded; // Scene 로드 이벤트 등록
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) // Scene 로드 완료 처리
    {
        if (scene.name != ExplorationSceneName) // 탐사 Scene 여부 확인
        {
            return; // 다른 Scene 처리 생략
        }

        if (UnityEngine.Object.FindFirstObjectByType<ExplorationMapRuntime>() != null) // 기존 맵 런타임 존재 확인
        {
            return; // 중복 런타임 생성 생략
        }

        GameObject runtimeObject = new GameObject("ExplorationMapRuntime"); // 절차 맵 런타임 오브젝트 생성
        runtimeObject.AddComponent<ExplorationMapRuntime>(); // 절차 맵 런타임 컴포넌트 추가
    }
}
