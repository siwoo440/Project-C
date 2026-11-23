using System; // 현재 시간과 난수 기능 사용
using System.Collections.Generic; // 조우 목록과 좌표 집합 사용
using UnityEngine; // 런타임 오브젝트 기능 사용
using UnityEngine.InputSystem; // 디버그 재생성 입력 사용
using UnityEngine.SceneManagement; // 탐사 Scene 감지 기능 사용

public sealed class ExplorationMapRuntime : MonoBehaviour // 38일차 절차 맵·층·조우 런타임
{
    private const int DefaultCellCount = 14; // 기본 생성 셀 수
    private const int DefaultEncounterCount = 3; // 층당 기본 절차 조우 수
    private const float WorldMin = -3.4f; // 임시 맵 월드 최소 좌표
    private const float WorldMax = 3.4f; // 임시 맵 월드 최대 좌표
    private const float FloorChangeCooldown = 0.5f; // 연속 층 이동 방지 시간

    private static Sprite runtimeSquareSprite; // 런타임 표시용 사각형 스프라이트

    private readonly List<GameObject> encounterObjects =
        new List<GameObject>(); // 현재 층 조우 오브젝트 목록

    private readonly HashSet<Vector2Int> encounterCoordinates =
        new HashSet<Vector2Int>(); // 현재 층 조우 셀 좌표 집합

    private ExplorationSessionManager sessionManager; // 탐사 세션 관리자
    private GameObject stairsObject; // 현재 계단 오브젝트
    private float nextFloorChangeAllowedTime; // 다음 층 이동 허용 시각
    private bool initialPlayerPlacementHandled; // 첫 플레이어 위치 처리 여부

    public ExplorationMapData CurrentMap { get; private set; } // 현재 생성된 논리 맵
    public int CurrentFloor => sessionManager != null ? sessionManager.CurrentFloor : 1; // 현재 층 조회
    public int CurrentEncounterCount => encounterCoordinates.Count; // 현재 층 조우 개수 조회

    private void Awake() // 탐사 맵 런타임 초기화
    {
        sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        EnsureRuntimeSquareSprite(); // 런타임 사각형 스프라이트 준비
        GenerateNewMap(); // 최초 절차 맵과 조우 생성
        EnsureDebugView(); // 절차 맵 디버그 화면 준비
    }

    private void Start() // Scene 시작 후 플레이어 위치 처리
    {
        TryHandleInitialPlayerPlacement(); // 시작 셀 위치 배치 시도
    }

    private void Update() // 디버그 입력과 초기 배치 처리
    {
        TryHandleInitialPlayerPlacement(); // 플레이어 생성 지연 대응

        Keyboard keyboard =
            Keyboard.current; // 현재 키보드 조회

        if (keyboard != null &&
            keyboard.f9Key.wasPressedThisFrame)
        {
            GenerateNewMap(); // 현재 층 맵·조우 재생성

            ExplorationPlayerController player =
                FindFirstObjectByType<ExplorationPlayerController>(); // 현재 플레이어 조회

            MovePlayerToStart(player); // 재생성 후 시작 셀 이동
        }
    }

    public void GenerateNewMap() // 새 탐사 논리 맵과 조우 생성
    {
        ClearEncounterObjects(); // 이전 조우 오브젝트 정리

        int seed =
            unchecked((int)DateTime.UtcNow.Ticks ^ Time.frameCount); // 현재 시점 기반 임시 시드 생성

        CurrentMap =
            ExplorationMapGenerator.Generate(
                DefaultCellCount,
                seed); // 기본 셀 수 절차 맵 생성

        RefreshStairs(); // 새 계단 위치 갱신
        GenerateEncounters(); // 일반 셀 절차 조우 배치

        Debug.Log(
            $"[Exploration][Day38] 절차 층 생성 완료 - " +
            $"Floor {CurrentFloor}F / " +
            $"Seed {CurrentMap.Seed} / " +
            $"Cells {CurrentMap.Cells.Count} / " +
            $"Encounters {CurrentEncounterCount} / " +
            $"Start {CurrentMap.StartCoordinate} / " +
            $"Stairs {CurrentMap.StairsCoordinate}"); // 생성 결과 로그
    }

    public bool TryDescendFloor(
        ExplorationPlayerController player) // 계단을 통한 다음 층 이동 시도
    {
        if (player == null ||
            Time.time < nextFloorChangeAllowedTime)
        {
            return false;
        }

        nextFloorChangeAllowedTime =
            Time.time + FloorChangeCooldown; // 다음 실행 대기 시간 설정

        sessionManager.AdvanceFloor(); // 현재 탐사 층 증가
        GenerateNewMap(); // 다음 층 맵과 조우 생성
        MovePlayerToStart(player); // 새 층 시작 셀로 이동

        Debug.Log(
            $"[Exploration][Day38] 계단 이동 완료 - " +
            $"{CurrentFloor}F / " +
            $"조우 {CurrentEncounterCount}개"); // 계단 이동 완료 로그

        return true;
    }

    public Vector2 GetWorldPosition(
        Vector2Int coordinate) // 논리 셀 좌표를 임시 월드 좌표로 변환
    {
        if (CurrentMap == null ||
            CurrentMap.Cells.Count == 0)
        {
            return Vector2.zero;
        }

        GetMapBounds(
            out int minX,
            out int maxX,
            out int minY,
            out int maxY); // 현재 맵 좌표 범위 계산

        float normalizedX =
            maxX == minX
                ? 0.5f
                : Mathf.InverseLerp(
                    minX,
                    maxX,
                    coordinate.x); // X 좌표 정규화

        float normalizedY =
            maxY == minY
                ? 0.5f
                : Mathf.InverseLerp(
                    minY,
                    maxY,
                    coordinate.y); // Y 좌표 정규화

        return new Vector2(
            Mathf.Lerp(
                WorldMin,
                WorldMax,
                normalizedX),
            Mathf.Lerp(
                WorldMin,
                WorldMax,
                normalizedY)); // 플레이 가능 범위 내 월드 위치 반환
    }

    public bool HasEncounterAt(
        Vector2Int coordinate) // 지정 셀 조우 존재 여부 확인
    {
        return encounterCoordinates.Contains(coordinate); // 조우 셀 포함 여부 반환
    }

    private void GenerateEncounters() // 현재 맵 일반 셀에 절차 조우 배치
    {
        if (CurrentMap == null)
        {
            return;
        }

        EncounterData[] loadedData =
            Resources.LoadAll<EncounterData>("Encounters"); // 전체 조우 데이터 로드

        List<EncounterData> validData =
            new List<EncounterData>(); // 유효 조우 데이터 목록 생성

        foreach (EncounterData data in loadedData)
        {
            if (data != null &&
                data.IsValidData())
            {
                validData.Add(data); // 유효 조우 데이터 등록
            }
        }

        if (validData.Count == 0)
        {
            Debug.LogWarning(
                "[Exploration][Day38] Resources/Encounters에 유효한 EncounterData가 없어 절차 조우를 생성하지 않았습니다."); // 조우 데이터 없음 경고

            return;
        }

        List<ExplorationMapCell> availableCells =
            new List<ExplorationMapCell>(); // 조우 배치 가능 일반 셀 목록

        foreach (ExplorationMapCell cell in CurrentMap.Cells)
        {
            if (cell.Type == ExplorationCellType.Normal)
            {
                availableCells.Add(cell); // 일반 셀만 배치 후보 등록
            }
        }

        if (availableCells.Count == 0)
        {
            return;
        }

        System.Random random =
            new System.Random(
                CurrentMap.Seed ^ 1597463007); // 현재 맵 Seed 기반 조우 난수 생성

        ShuffleCells(
            availableCells,
            random); // 배치 후보 셀 순서 무작위화

        int encounterCount =
            Mathf.Min(
                DefaultEncounterCount,
                availableCells.Count); // 실제 생성 조우 수 계산

        for (int index = 0;
             index < encounterCount;
             index++)
        {
            ExplorationMapCell cell =
                availableCells[index]; // 조우 배치 셀 선택

            EncounterData data =
                validData[random.Next(validData.Count)]; // 조우 템플릿 무작위 선택

            CreateEncounterObject(
                cell,
                data,
                index); // 절차 조우 오브젝트 생성
        }
    }

    private void CreateEncounterObject(
        ExplorationMapCell cell,
        EncounterData data,
        int index) // 절차 조우 오브젝트 생성
    {
        string runtimeEncounterId =
            $"F{CurrentFloor}_" +
            $"X{cell.Coordinate.x}_" +
            $"Y{cell.Coordinate.y}_" +
            $"S{CurrentMap.Seed}"; // 층·셀·Seed 기반 런타임 조우 ID 생성

        if (sessionManager.IsEncounterCleared(runtimeEncounterId))
        {
            return;
        }

        Vector2 worldPosition =
            GetWorldPosition(cell.Coordinate); // 조우 셀 월드 좌표 계산

        GameObject encounterObject =
            new GameObject(
                $"Encounter_{runtimeEncounterId}",
                typeof(SpriteRenderer),
                typeof(CircleCollider2D),
                typeof(ExplorationEncounterView)); // 조우 오브젝트 생성

        encounterObject.transform.SetParent(transform); // 맵 런타임 하위 배치

        encounterObject.transform.position =
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f); // 조우 월드 위치 지정

        encounterObject.transform.localScale =
            new Vector3(
                0.72f,
                0.72f,
                1f); // 조우 임시 표시 크기 설정

        SpriteRenderer spriteRenderer =
            encounterObject.GetComponent<SpriteRenderer>(); // 조우 SpriteRenderer 조회

        spriteRenderer.sprite =
            runtimeSquareSprite; // 조우 임시 사각형 스프라이트 지정

        spriteRenderer.color =
            GetEncounterColor(index); // 조우 임시 색상 지정

        spriteRenderer.sortingOrder = 4; // 조우 표시 순서 지정

        CircleCollider2D collider =
            encounterObject.GetComponent<CircleCollider2D>(); // 조우 Collider 조회

        collider.isTrigger = true; // 조우 Trigger 활성화
        collider.radius = 0.6f; // 조우 Trigger 범위 설정

        ExplorationEncounterView encounterView =
            encounterObject.GetComponent<ExplorationEncounterView>(); // 조우 View 조회

        encounterView.Initialize(
            data,
            runtimeEncounterId); // 조우 데이터와 런타임 ID 연결

        encounterObjects.Add(encounterObject); // 현재 층 조우 오브젝트 등록
        encounterCoordinates.Add(cell.Coordinate); // 현재 층 조우 셀 등록
    }

    private static void ShuffleCells(
        List<ExplorationMapCell> cells,
        System.Random random) // 조우 후보 셀 무작위 섞기
    {
        for (int index = cells.Count - 1;
             index > 0;
             index--)
        {
            int swapIndex =
                random.Next(index + 1); // 교환 대상 인덱스 선택

            ExplorationMapCell temporary =
                cells[index]; // 현재 셀 임시 저장

            cells[index] =
                cells[swapIndex]; // 선택 셀 이동

            cells[swapIndex] =
                temporary; // 임시 셀 이동
        }
    }

    private void ClearEncounterObjects() // 현재 층 조우 오브젝트 정리
    {
        foreach (GameObject encounterObject in encounterObjects)
        {
            if (encounterObject == null)
            {
                continue;
            }

            encounterObject.SetActive(false); // 같은 프레임 Trigger 재접촉 방지
            Destroy(encounterObject); // 기존 조우 오브젝트 제거
        }

        encounterObjects.Clear(); // 조우 오브젝트 목록 초기화
        encounterCoordinates.Clear(); // 조우 셀 좌표 초기화
    }

    private void TryHandleInitialPlayerPlacement() // 최초 탐사 진입 플레이어 시작 위치 처리
    {
        if (initialPlayerPlacementHandled)
        {
            return;
        }

        ExplorationPlayerController player =
            FindFirstObjectByType<ExplorationPlayerController>(); // 현재 탐사 플레이어 조회

        if (player == null)
        {
            return;
        }

        initialPlayerPlacementHandled = true; // 초기 배치 완료 기록

        if (sessionManager.HasReturnPosition)
        {
            return;
        }

        MovePlayerToStart(player); // 새 탐사 시작 셀 이동
    }

    private void MovePlayerToStart(
        ExplorationPlayerController player) // 플레이어를 현재 맵 시작 셀로 이동
    {
        if (player == null ||
            CurrentMap == null)
        {
            return;
        }

        Vector2 startWorldPosition =
            GetWorldPosition(
                CurrentMap.StartCoordinate); // 시작 셀 월드 위치 계산

        player.Teleport(startWorldPosition); // 플레이어 시작 위치 이동
    }

    private void RefreshStairs() // 현재 맵 계단 오브젝트 갱신
    {
        EnsureStairsObject(); // 계단 오브젝트 존재 보장

        if (stairsObject == null ||
            CurrentMap == null)
        {
            return;
        }

        Vector2 stairsWorldPosition =
            GetWorldPosition(
                CurrentMap.StairsCoordinate); // 계단 셀 월드 위치 계산

        stairsObject.name =
            $"FloorStairs_{CurrentFloor}F"; // 현재 층 계단 이름 갱신

        stairsObject.transform.position =
            new Vector3(
                stairsWorldPosition.x,
                stairsWorldPosition.y,
                0f); // 계단 월드 위치 갱신
    }

    private void EnsureStairsObject() // 임시 계단 오브젝트 생성 보장
    {
        if (stairsObject != null)
        {
            return;
        }

        stairsObject =
            new GameObject(
                "FloorStairs",
                typeof(SpriteRenderer),
                typeof(CircleCollider2D),
                typeof(ExplorationFloorStairs)); // 계단 오브젝트 생성

        stairsObject.transform.SetParent(transform); // 런타임 오브젝트 하위 배치

        stairsObject.transform.localScale =
            new Vector3(
                0.8f,
                0.8f,
                1f); // 계단 임시 표시 크기 설정

        SpriteRenderer spriteRenderer =
            stairsObject.GetComponent<SpriteRenderer>(); // 계단 SpriteRenderer 조회

        spriteRenderer.sprite =
            runtimeSquareSprite; // 계단 임시 사각형 스프라이트 지정

        spriteRenderer.color =
            new Color(
                1f,
                0.85f,
                0.15f,
                1f); // 계단 노란색 표시

        spriteRenderer.sortingOrder = 3; // 계단 표시 순서 지정

        CircleCollider2D collider =
            stairsObject.GetComponent<CircleCollider2D>(); // 계단 Collider 조회

        collider.isTrigger = true; // 계단 Trigger 활성화
        collider.radius = 0.55f; // 계단 접촉 범위 지정

        ExplorationFloorStairs stairs =
            stairsObject.GetComponent<ExplorationFloorStairs>(); // 계단 동작 컴포넌트 조회

        stairs.Initialize(this); // 현재 맵 런타임 연결
    }

    private void EnsureDebugView() // 임시 절차 맵 UI 존재 보장
    {
        if (GetComponent<ExplorationMapDebugView>() == null)
        {
            gameObject.AddComponent<ExplorationMapDebugView>(); // 절차 맵 디버그 화면 추가
        }
    }

    private void GetMapBounds(
        out int minX,
        out int maxX,
        out int minY,
        out int maxY) // 현재 논리 맵 좌표 범위 계산
    {
        ExplorationMapCell firstCell =
            CurrentMap.Cells[0]; // 첫 셀 기준값 조회

        minX = firstCell.Coordinate.x; // 최소 X 초기화
        maxX = firstCell.Coordinate.x; // 최대 X 초기화
        minY = firstCell.Coordinate.y; // 최소 Y 초기화
        maxY = firstCell.Coordinate.y; // 최대 Y 초기화

        foreach (ExplorationMapCell cell in CurrentMap.Cells)
        {
            minX =
                Mathf.Min(
                    minX,
                    cell.Coordinate.x); // 최소 X 갱신

            maxX =
                Mathf.Max(
                    maxX,
                    cell.Coordinate.x); // 최대 X 갱신

            minY =
                Mathf.Min(
                    minY,
                    cell.Coordinate.y); // 최소 Y 갱신

            maxY =
                Mathf.Max(
                    maxY,
                    cell.Coordinate.y); // 최대 Y 갱신
        }
    }

    private static Color GetEncounterColor(
        int index) // 절차 조우 임시 색상 조회
    {
        switch (index % 3)
        {
            case 0:
                return new Color(
                    0.9f,
                    0.2f,
                    0.2f,
                    1f); // 빨간 조우 색상 반환

            case 1:
                return new Color(
                    0.85f,
                    0.45f,
                    0.15f,
                    1f); // 주황 조우 색상 반환

            default:
                return new Color(
                    0.75f,
                    0.2f,
                    0.65f,
                    1f); // 보라 조우 색상 반환
        }
    }

    private static void EnsureRuntimeSquareSprite() // 런타임 표시 스프라이트 준비
    {
        if (runtimeSquareSprite != null)
        {
            return;
        }

        runtimeSquareSprite =
            Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f); // 흰색 사각형 런타임 스프라이트 생성
    }
}

public static class ExplorationMapRuntimeBootstrap // 탐사 Scene 절차 맵 자동 생성기
{
    private const string ExplorationSceneName =
        "30_Exploration"; // 탐사 Scene 이름

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // Scene 로드 이벤트 초기화
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded; // 중복 Scene 이벤트 제거
        SceneManager.sceneLoaded += HandleSceneLoaded; // Scene 로드 이벤트 등록
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode) // Scene 로드 완료 처리
    {
        if (scene.name != ExplorationSceneName)
        {
            return;
        }

        if (UnityEngine.Object.FindFirstObjectByType<ExplorationMapRuntime>() != null)
        {
            return;
        }

        GameObject runtimeObject =
            new GameObject(
                "ExplorationMapRuntime"); // 절차 맵 런타임 오브젝트 생성

        runtimeObject.AddComponent<ExplorationMapRuntime>(); // 절차 맵 런타임 컴포넌트 추가
    }
}
