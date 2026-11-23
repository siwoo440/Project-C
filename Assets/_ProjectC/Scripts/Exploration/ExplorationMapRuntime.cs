using System; // 현재 시간과 난수 기능 사용
using System.Collections.Generic; // 조우 목록과 좌표 집합 사용
using UnityEngine; // 런타임 오브젝트 기능 사용
using UnityEngine.InputSystem; // 디버그 재생성 입력 사용
using UnityEngine.SceneManagement; // 탐사 Scene 감지 기능 사용

public sealed class ExplorationMapRuntime : MonoBehaviour // 44일차 탐사 성공 상태 연동 런타임
{
    private const int DefaultCellCount = 14; // 기본 생성 셀 수
    private const int DefaultEncounterCount = 3; // 층당 기본 절차 조우 수
    private const int BossFloorInterval = 5; // 43일차 테스트용 보스층 간격
    private const float FloorChangeCooldown = 0.5f; // 연속 층 이동 방지 시간

    private static Sprite runtimeSquareSprite; // 런타임 표시용 사각형 스프라이트

    private readonly List<GameObject> encounterObjects =
        new List<GameObject>(); // 현재 층 조우 오브젝트 목록

    private readonly Dictionary<Vector2Int, BattleType> encounterTypes =
        new Dictionary<Vector2Int, BattleType>(); // 현재 층 좌표별 조우 등급

    private ExplorationSessionManager sessionManager; // 탐사 세션 관리자
    private ExplorationTilemapView tilemapView; // 논리 맵 Tilemap 표시기
    private GameObject stairsObject; // 현재 계단 오브젝트
    private float nextFloorChangeAllowedTime; // 다음 층 이동 허용 시각
    private bool initialPlayerPlacementHandled; // 첫 플레이어 위치 처리 여부

    public ExplorationMapData CurrentMap { get; private set; } // 현재 생성된 논리 맵
    public int CurrentFloor => sessionManager != null ? sessionManager.CurrentFloor : 1; // 현재 층 조회
    public int CurrentEncounterCount => encounterTypes.Count; // 현재 층 조우 개수 조회
    public bool RestoredFromSession { get; private set; } // 저장된 층 상태 복원 여부
    public int CurrentFloorTileCount => tilemapView != null ? tilemapView.FloorTileCount : 0; // 현재 Floor 타일 개수
    public int CurrentWallTileCount => tilemapView != null ? tilemapView.WallTileCount : 0; // 현재 실제 Wall 타일 개수

    private void Awake() // 탐사 맵 런타임 초기화
    {
        sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        EnsureRuntimeSquareSprite(); // 런타임 사각형 스프라이트 준비
        EnsureTilemapView(); // 실제 Tilemap 표시기 준비
        EnsureCameraFollow(); // 확장된 탐사 공간 카메라 추적 준비
        RestoreOrCreateCurrentFloor(); // 현재 층 Seed 복원 또는 신규 생성
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
            !sessionManager.IsExplorationCompleted &&
            keyboard.f9Key.wasPressedThisFrame)
        {
            RegenerateCurrentFloorForDebug(); // 현재 층 Seed·맵·조우 새로 생성

            ExplorationPlayerController player =
                FindFirstObjectByType<ExplorationPlayerController>(); // 현재 플레이어 조회

            MovePlayerToStart(player); // 재생성 후 시작 셀 이동
        }
    }

    private void RestoreOrCreateCurrentFloor() // 현재 층 상태 복원 또는 신규 생성
    {
        if (sessionManager.TryGetCurrentFloorSeed(out int savedSeed))
        {
            BuildCurrentFloor(
                savedSeed,
                true); // 저장된 Seed로 동일 층 복원

            return;
        }

        int newSeed =
            CreateSeed(); // 신규 층 Seed 생성

        sessionManager.SetCurrentFloorSeed(newSeed); // 신규 층 Seed 세션 저장

        BuildCurrentFloor(
            newSeed,
            false); // 신규 Seed로 현재 층 생성
    }

    private void RegenerateCurrentFloorForDebug() // F9 현재 층 강제 재생성
    {
        if (sessionManager.IsExplorationCompleted)
        {
            return;
        }

        int newSeed =
            CreateSeed(); // 디버그용 새 Seed 생성

        sessionManager.SetCurrentFloorSeed(newSeed); // 새 Seed를 현재 층 상태로 교체
        sessionManager.ClearReturnPosition(); // 이전 맵 복귀 위치 제거

        BuildCurrentFloor(
            newSeed,
            false); // 새 Seed로 현재 층 재생성
    }

    private void BuildCurrentFloor(
        int seed,
        bool restoredFromSession) // 지정 Seed로 현재 층 구축
    {
        ClearEncounterObjects(); // 이전 조우 오브젝트 정리

        CurrentMap =
            ExplorationMapGenerator.Generate(
                DefaultCellCount,
                seed); // 지정 Seed로 동일 논리 맵 생성

        RestoredFromSession =
            restoredFromSession; // 현재 생성이 복원인지 기록

        tilemapView.Build(CurrentMap); // 논리 맵을 실제 방·통로 Tilemap으로 변환
        RefreshStairs(); // Tilemap 기준 계단 위치 복원 또는 생성
        GenerateEncounters(); // 동일 Seed 기반 조우 배치 복원 또는 생성

        string stateText =
            RestoredFromSession
                ? "복원"
                : "신규"; // 현재 층 생성 상태 문구 결정

        Debug.Log(
            $"[Exploration][Day43] 절차 층 {stateText} 완료 - " +
            $"Floor {CurrentFloor}F / " +
            $"Seed {CurrentMap.Seed} / " +
            $"Cells {CurrentMap.Cells.Count} / " +
            $"Encounters {CurrentEncounterCount} / " +
            $"FloorTiles {CurrentFloorTileCount} / " +
            $"Walls {CurrentWallTileCount} / " +
            $"Start {CurrentMap.StartCoordinate} / " +
            $"Stairs {CurrentMap.StairsCoordinate}"); // Tilemap 생성·복원 결과 로그
    }

    private static int CreateSeed() // 신규 절차 맵 Seed 생성
    {
        return unchecked(
            (int)DateTime.UtcNow.Ticks ^
            Time.frameCount); // 현재 시점 기반 Seed 반환
    }

    public bool TryDescendFloor(
        ExplorationPlayerController player) // 계단을 통한 다음 층 이동 시도
    {
        if (player == null ||
            sessionManager == null ||
            sessionManager.IsExplorationCompleted ||
            Time.time < nextFloorChangeAllowedTime)
        {
            return false;
        }

        nextFloorChangeAllowedTime =
            Time.time + FloorChangeCooldown; // 다음 실행 대기 시간 설정

        sessionManager.AdvanceFloor(); // 현재 탐사 층 증가와 이전 Seed 해제
        RestoreOrCreateCurrentFloor(); // 다음 층 신규 Seed 생성
        MovePlayerToStart(player); // 새 층 시작 셀로 이동

        Debug.Log(
            $"[Exploration][Day44] 계단 이동 완료 - " +
            $"{CurrentFloor}F / " +
            $"조우 {CurrentEncounterCount}개"); // 계단 이동 완료 로그

        return true;
    }

    public Vector2 GetWorldPosition(
        Vector2Int coordinate) // 논리 셀의 Tilemap 방 중심 World 좌표 반환
    {
        if (tilemapView == null)
        {
            return Vector2.zero;
        }

        return tilemapView.GetWorldPosition(
            coordinate); // Tilemap 기준 실제 방 중심 위치 반환
    }

    public bool HasEncounterAt(
        Vector2Int coordinate) // 지정 셀 조우 존재 여부 확인
    {
        return encounterTypes.ContainsKey(coordinate); // 조우 셀 포함 여부 반환
    }

    public bool TryGetEncounterTypeAt(
        Vector2Int coordinate,
        out BattleType battleType) // 지정 셀 조우 등급 조회
    {
        return encounterTypes.TryGetValue(
            coordinate,
            out battleType); // 좌표별 조우 등급 반환
    }

    public bool TryGetPlayerLogicalPosition(
        out Vector2 logicalPosition) // 미니맵용 플레이어 연속 논리 위치 조회
    {
        logicalPosition = Vector2.zero; // 실패 기본값 지정

        if (tilemapView == null ||
            CurrentMap == null)
        {
            return false;
        }

        ExplorationPlayerController player =
            FindFirstObjectByType<ExplorationPlayerController>(); // 현재 탐사 플레이어 조회

        if (player == null)
        {
            return false;
        }

        logicalPosition =
            tilemapView.GetLogicalPosition(
                player.transform.position); // 실제 Tilemap 위치를 논리 좌표 비율로 변환

        return true;
    }

    private void GenerateEncounters() // 현재 맵에 일반·엘리트·보스 조우 배치
    {
        if (CurrentMap == null)
        {
            return;
        }

        EncounterData[] loadedData =
            Resources.LoadAll<EncounterData>("Encounters"); // 전체 조우 데이터 로드

        List<EncounterData> validData =
            new List<EncounterData>(); // 전체 유효 조우 목록

        List<EncounterData> normalData =
            new List<EncounterData>(); // 일반 조우 목록

        List<EncounterData> eliteData =
            new List<EncounterData>(); // 엘리트 조우 목록

        List<EncounterData> bossData =
            new List<EncounterData>(); // 보스 조우 목록

        foreach (EncounterData data in loadedData)
        {
            if (data == null ||
                !data.IsValidData())
            {
                continue;
            }

            validData.Add(data); // 유효 조우 등록

            switch (data.BattleType)
            {
                case BattleType.Elite:
                    eliteData.Add(data); // 엘리트 목록 등록
                    break;

                case BattleType.Boss:
                    bossData.Add(data); // 보스 목록 등록
                    break;

                default:
                    normalData.Add(data); // 일반 목록 등록
                    break;
            }
        }

        if (validData.Count == 0)
        {
            Debug.LogWarning(
                "[Exploration][Day43] Resources/Encounters에 유효한 EncounterData가 없어 절차 조우를 생성하지 않았습니다."); // 조우 데이터 없음 경고

            return;
        }

        SortEncounterData(validData); // 전체 조우 순서 고정
        SortEncounterData(normalData); // 일반 조우 순서 고정
        SortEncounterData(eliteData); // 엘리트 조우 순서 고정
        SortEncounterData(bossData); // 보스 조우 순서 고정

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

        List<BattleType> encounterTypePlan =
            BuildEncounterTypePlan(
                encounterCount,
                normalData.Count > 0,
                eliteData.Count > 0,
                bossData.Count > 0); // 현재 층 조우 등급 계획 생성

        for (int index = 0;
             index < encounterCount;
             index++)
        {
            ExplorationMapCell cell =
                availableCells[index]; // 조우 배치 셀 선택

            BattleType plannedType =
                encounterTypePlan[index]; // 현재 슬롯 조우 등급 조회

            EncounterData data =
                SelectEncounterData(
                    plannedType,
                    normalData,
                    eliteData,
                    bossData,
                    validData,
                    random); // 등급에 맞는 조우 데이터 선택

            CreateEncounterObject(
                cell,
                data); // 등급 기반 절차 조우 오브젝트 생성
        }
    }

    private List<BattleType> BuildEncounterTypePlan(
        int encounterCount,
        bool hasNormal,
        bool hasElite,
        bool hasBoss) // 현재 층 조우 등급 배치 계획 생성
    {
        List<BattleType> plan =
            new List<BattleType>(); // 조우 등급 계획 목록

        bool isBossFloor =
            CurrentFloor > 0 &&
            CurrentFloor % BossFloorInterval == 0; // 5층 간격 테스트용 보스층 판정

        if (isBossFloor &&
            hasBoss &&
            plan.Count < encounterCount)
        {
            plan.Add(BattleType.Boss); // 보스층에 보스 조우 1개 우선 배치
        }

        if (hasElite &&
            plan.Count < encounterCount)
        {
            plan.Add(BattleType.Elite); // 매 층 엘리트 조우 1개 배치
        }

        while (plan.Count < encounterCount)
        {
            if (hasNormal)
            {
                plan.Add(BattleType.Normal); // 남은 슬롯 일반 조우 배치
                continue;
            }

            if (hasElite)
            {
                plan.Add(BattleType.Elite); // 일반 데이터 없을 때 엘리트 대체
                continue;
            }

            if (hasBoss)
            {
                plan.Add(BattleType.Boss); // 일반·엘리트 없을 때 보스 대체
                continue;
            }

            break;
        }

        return plan;
    }

    private static EncounterData SelectEncounterData(
        BattleType battleType,
        List<EncounterData> normalData,
        List<EncounterData> eliteData,
        List<EncounterData> bossData,
        List<EncounterData> fallbackData,
        System.Random random) // 요청 등급에 맞는 조우 데이터 선택
    {
        List<EncounterData> sourceData;

        switch (battleType)
        {
            case BattleType.Elite:
                sourceData = eliteData; // 엘리트 데이터 선택
                break;

            case BattleType.Boss:
                sourceData = bossData; // 보스 데이터 선택
                break;

            default:
                sourceData = normalData; // 일반 데이터 선택
                break;
        }

        if (sourceData.Count == 0)
        {
            sourceData = fallbackData; // 해당 등급 데이터 없을 때 전체 목록 대체
        }

        return sourceData[
            random.Next(
                sourceData.Count)]; // Seed 기반 조우 템플릿 반환
    }

    private static void SortEncounterData(
        List<EncounterData> data) // Seed 재현용 조우 데이터 정렬
    {
        data.Sort(
            (left, right) =>
                string.CompareOrdinal(
                    left.EncounterId,
                    right.EncounterId)); // EncounterId 순서 고정
    }

    private void CreateEncounterObject(
        ExplorationMapCell cell,
        EncounterData data) // 등급 기반 절차 조우 오브젝트 생성
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
                $"Encounter_{data.BattleType}_{runtimeEncounterId}",
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
            GetEncounterColor(
                data.BattleType); // 조우 등급별 임시 색상 지정

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

        encounterTypes[cell.Coordinate] =
            data.BattleType; // 현재 좌표 조우 등급 등록
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
        encounterTypes.Clear(); // 좌표별 조우 등급 초기화
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

    private void EnsureTilemapView() // 논리 맵 Tilemap 표시기 존재 보장
    {
        tilemapView =
            GetComponent<ExplorationTilemapView>(); // 기존 Tilemap 표시기 조회

        if (tilemapView == null)
        {
            tilemapView =
                gameObject.AddComponent<ExplorationTilemapView>(); // 런타임 Tilemap 표시기 추가
        }
    }

    private static void EnsureCameraFollow() // 탐사 카메라 추적 기능 존재 보장
    {
        Camera explorationCamera =
            Camera.main; // Main Camera 우선 조회

        if (explorationCamera == null)
        {
            explorationCamera =
                FindFirstObjectByType<Camera>(); // Main 태그가 없을 경우 Scene 카메라 조회
        }

        if (explorationCamera == null)
        {
            Debug.LogWarning(
                "[Exploration][Day40] 탐사 카메라를 찾을 수 없어 카메라 추적을 추가하지 않았습니다."); // 카메라 누락 경고

            return;
        }

        if (explorationCamera.GetComponent<ExplorationCameraFollow>() == null)
        {
            explorationCamera.gameObject.AddComponent<ExplorationCameraFollow>(); // 플레이어 카메라 추적 추가
        }
    }

    private void EnsureDebugView() // 임시 절차 맵 UI 존재 보장
    {
        if (GetComponent<ExplorationMapDebugView>() == null)
        {
            gameObject.AddComponent<ExplorationMapDebugView>(); // 절차 맵 디버그 화면 추가
        }
    }

    private static Color GetEncounterColor(
        BattleType battleType) // 조우 등급별 임시 색상 조회
    {
        switch (battleType)
        {
            case BattleType.Elite:
                return new Color(
                    0.72f,
                    0.22f,
                    0.90f,
                    1f); // 엘리트 보라색 반환

            case BattleType.Boss:
                return new Color(
                    0.95f,
                    0.12f,
                    0.12f,
                    1f); // 보스 붉은색 반환

            default:
                return new Color(
                    0.90f,
                    0.45f,
                    0.15f,
                    1f); // 일반 주황색 반환
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
