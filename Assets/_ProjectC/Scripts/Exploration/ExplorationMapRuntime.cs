using System; // 현재 시간과 난수 기능 사용
using System.Collections.Generic; // 조우 목록과 좌표 집합 사용
using UnityEngine; // 런타임 오브젝트 기능 사용
using UnityEngine.InputSystem; // 디버그 재생성 입력 사용
using UnityEngine.SceneManagement; // 탐사 Scene 감지 기능 사용

public sealed class ExplorationMapRuntime : MonoBehaviour // 44일차 탐사 성공 상태 연동 런타임
{
    private const int DefaultCellCount = 14; // 기본 생성 셀 수
    private const int DefaultEncounterCount = 3; // 층당 기본 절차 조우 수
    private const int DefaultEventCount = 2; // 층당 기본 탐사 이벤트 수
    private const int DefaultHazardRoomCount = 3; // 층당 기본 퇴색 위험 방 수
    private const int HazardSeedSalt = 135791113; // 퇴색 방 선택 난수 분리값
    private const int BossFloorInterval = 5; // 43일차 테스트용 보스층 간격
    private const float FloorChangeCooldown = 0.5f; // 연속 층 이동 방지 시간

    private static Sprite runtimeSquareSprite; // 런타임 표시용 사각형 스프라이트

    private readonly List<GameObject> encounterObjects =
        new List<GameObject>(); // 현재 층 조우 오브젝트 목록

    private readonly Dictionary<Vector2Int, BattleType> encounterTypes =
        new Dictionary<Vector2Int, BattleType>(); // 현재 층 좌표별 조우 등급

    private readonly Dictionary<GameObject, string> encounterRuntimeIds =
        new Dictionary<GameObject, string>(); // 조우 오브젝트별 런타임 ID

    private readonly Dictionary<string, Vector2Int> encounterCoordinates =
        new Dictionary<string, Vector2Int>(); // 런타임 ID별 조우 좌표

    private readonly List<GameObject> eventObjects =
        new List<GameObject>(); // 현재 층 이벤트 오브젝트 목록

    private readonly Dictionary<GameObject, string> eventRuntimeIds =
        new Dictionary<GameObject, string>(); // 이벤트 오브젝트별 런타임 ID

    private readonly Dictionary<string, Vector2Int> eventCoordinates =
        new Dictionary<string, Vector2Int>(); // 런타임 ID별 이벤트 좌표

    private readonly Dictionary<Vector2Int, ExplorationHazardRoomState> hazardRooms =
        new Dictionary<Vector2Int, ExplorationHazardRoomState>(); // 현재 층 퇴색 위험 방 상태

    private ExplorationSessionManager sessionManager; // 탐사 세션 관리자
    private ExplorationTilemapView tilemapView; // 논리 맵 Tilemap 표시기
    private ExplorationHazardRuntime hazardRuntime; // 퇴색 노출과 환경 피해 처리기
    private ExplorationHazardOverlayView hazardOverlayView; // 퇴색 방 시각 표시기
    private ExplorationHazardView hazardView; // 퇴색 위험 HUD 표시기
    private ExplorationPartyStatusView partyStatusView; // 좌하단 출전 파티 상태 HUD
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

        EnsurePartyStateAndView(); // 출전 파티 영구 상태와 좌하단 HUD 준비

        EnsureRuntimeSquareSprite(); // 런타임 사각형 스프라이트 준비
        EnsureTilemapView(); // 실제 Tilemap 표시기 준비
        EnsureCameraFollow(); // 확장된 탐사 공간 카메라 추적 준비
        EnsureHazardComponents(); // 퇴색 환경 위험 런타임과 UI 준비
        RestoreOrCreateCurrentFloor(); // 현재 층 Seed 복원 또는 신규 생성
        EnsureDebugView(); // 절차 맵 디버그 화면 준비
        ExplorationEventPanelView.EnsureInstance(); // 탐사 이벤트 패널 준비
    }

    private void Start() // Scene 시작 후 플레이어 위치 처리
    {
        TryHandleInitialPlayerPlacement(); // 시작 셀 위치 배치 시도
    }

    private void Update() // 디버그 입력과 초기 배치 처리
    {
        TryHandleInitialPlayerPlacement(); // 플레이어 생성 지연 대응
        RemoveClearedEncounterObjects(); // 전투 승리 후 남은 조우 표시 제거
        RemoveResolvedEventObjects(); // 처리 완료 이벤트 표시 제거

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
        ClearEventObjects(); // 이전 이벤트 오브젝트 정리
        ClearHazards(); // 이전 퇴색 방 표시와 상태 정리

        CurrentMap =
            ExplorationMapGenerator.Generate(
                DefaultCellCount,
                seed); // 지정 Seed로 동일 논리 맵 생성

        RestoredFromSession =
            restoredFromSession; // 현재 생성이 복원인지 기록

        tilemapView.Build(CurrentMap); // 논리 맵을 실제 방·통로 Tilemap으로 변환
        GenerateHazards(); // 동일 Seed 기반 퇴색 위험 방 지정과 표시
        RefreshStairs(); // Tilemap 기준 계단 위치 복원 또는 생성
        GenerateEncounters(); // 동일 Seed 기반 조우 배치 복원 또는 생성
        GenerateEvents(); // 동일 Seed 기반 탐사 이벤트 배치 복원 또는 생성

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

    public bool TryGetHazardAt(
        Vector2Int coordinate,
        out ExplorationHazardRoomState hazardState) // 지정 방 퇴색 위험 상태 조회
    {
        return hazardRooms.TryGetValue(
            coordinate,
            out hazardState); // 현재 층 위험 방 상태 반환
    }

    public bool TryGetPlayerRoomCoordinate(
        out Vector2Int coordinate) // 플레이어가 실제로 서 있는 방 좌표 조회
    {
        coordinate = Vector2Int.zero; // 실패 기본 좌표 지정

        if (tilemapView == null)
        {
            return false;
        }

        ExplorationPlayerController player =
            FindFirstObjectByType<ExplorationPlayerController>(); // 현재 탐사 플레이어 조회

        if (player == null)
        {
            return false;
        }

        return tilemapView.TryGetRoomCoordinateAtWorldPosition(
            player.transform.position,
            out coordinate); // 실제 Floor 기반 현재 방 좌표 반환
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

    private void GenerateHazards() // 현재 맵에 Seed 기반 퇴색 위험 방 지정
    {
        hazardRooms.Clear(); // 기존 위험 방 상태 초기화

        if (CurrentMap == null)
        {
            return;
        }

        List<ExplorationMapCell> candidates =
            new List<ExplorationMapCell>(); // 퇴색 위험 방 후보 목록

        foreach (ExplorationMapCell cell in CurrentMap.Cells)
        {
            if (cell.Type != ExplorationCellType.Normal)
            {
                continue; // 시작 방과 계단 방은 안전 지역으로 유지
            }

            candidates.Add(
                cell); // 일반 방만 퇴색 후보 등록
        }

        if (candidates.Count == 0)
        {
            return;
        }

        System.Random random =
            new System.Random(
                CurrentMap.Seed ^
                HazardSeedSalt); // 현재 Seed 기반 위험 방 난수 생성

        ShuffleCells(
            candidates,
            random); // 위험 방 후보 순서 Seed 기반 무작위화

        int hazardCount =
            Mathf.Min(
                DefaultHazardRoomCount,
                candidates.Count); // 현재 층 실제 위험 방 수 계산

        for (int index = 0;
             index < hazardCount;
             index++)
        {
            ExplorationMapCell cell =
                candidates[index]; // 현재 위험 방 후보 선택

            int levelRoll =
                random.Next(100); // 위험도 결정용 0~99 난수 생성

            int hazardLevel =
                levelRoll < 55
                    ? 1
                    : levelRoll < 85
                        ? 2
                        : 3; // 위험도 1~3 가중치 선택

            ExplorationHazardRoomState hazardState =
                new ExplorationHazardRoomState(
                    ExplorationHazardType.Fade,
                    hazardLevel); // 퇴색 위험 방 상태 생성

            hazardRooms[cell.Coordinate] =
                hazardState; // 현재 방 위험 상태 등록
        }

        if (hazardOverlayView != null)
        {
            hazardOverlayView.Build(
                tilemapView,
                hazardRooms,
                runtimeSquareSprite); // 퇴색 방 Floor 시각 오버레이 생성
        }

        if (hazardRuntime != null)
        {
            hazardRuntime.ResetForCurrentFloor(); // 새 층 현재 위험 방 판정 초기화
        }
    }

    private void ClearHazards() // 현재 층 퇴색 방 상태와 표시 정리
    {
        hazardRooms.Clear(); // 위험 방 상태 초기화

        if (hazardOverlayView != null)
        {
            hazardOverlayView.Clear(); // 기존 위험 방 오버레이 제거
        }

        if (hazardRuntime != null)
        {
            hazardRuntime.ResetForCurrentFloor(); // 플레이어 현재 위험 판정 초기화
        }
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

    private void GenerateEvents() // 현재 맵에 탐사 이벤트 배치
    {
        if (CurrentMap == null)
        {
            return;
        }

        IReadOnlyList<ExplorationEventData> loadedEvents =
            ExplorationEventCatalog.LoadEvents(); // 탐사 이벤트 데이터 로드

        if (loadedEvents == null ||
            loadedEvents.Count == 0)
        {
            return;
        }

        List<ExplorationMapCell> availableCells =
            new List<ExplorationMapCell>(); // 이벤트 배치 가능 셀 목록

        foreach (ExplorationMapCell cell in CurrentMap.Cells)
        {
            if (cell.Type != ExplorationCellType.Normal)
            {
                continue;
            }

            if (encounterTypes.ContainsKey(cell.Coordinate))
            {
                continue;
            }

            availableCells.Add(cell); // 일반 빈 방만 이벤트 후보 등록
        }

        if (availableCells.Count == 0)
        {
            return;
        }

        int eventDiscoveryChance =
            Mathf.Clamp(
                55 +
                FacilityUpgradeManager.EnsureInstance().GetCommunicationEventDiscoveryBonusPercent(),
                0,
                95); // 통신 기지국 보너스 포함 이벤트 발견 확률 계산

        int maxEventCount =
            Mathf.Min(
                DefaultEventCount,
                availableCells.Count); // 현재 층 최대 이벤트 수 계산

        System.Random random =
            new System.Random(
                CurrentMap.Seed ^ 912367421); // 현재 맵 Seed 기반 이벤트 난수 생성

        ShuffleCells(
            availableCells,
            random); // 이벤트 후보 셀 순서 무작위화

        int createdEventCount = 0; // 생성 이벤트 수 초기화

        for (int index = 0;
             index < availableCells.Count &&
             createdEventCount < maxEventCount;
             index++)
        {
            bool shouldCreate =
                createdEventCount == 0 ||
                random.Next(100) < eventDiscoveryChance; // 최소 1개 보장 포함 생성 판정

            if (!shouldCreate)
            {
                continue;
            }

            ExplorationMapCell cell =
                availableCells[index]; // 이벤트 배치 셀 선택

            ExplorationEventData eventData =
                loadedEvents[
                    random.Next(loadedEvents.Count)]; // 이벤트 데이터 선택

            CreateEventObject(
                cell,
                eventData); // 탐사 이벤트 오브젝트 생성

            createdEventCount += 1; // 생성 이벤트 수 증가
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

        Vector2 worldPosition; // 조우 실제 배치 위치

        if (!tilemapView.TryGetRandomEncounterPosition(
                cell.Coordinate,
                CurrentMap.Seed,
                out worldPosition))
        {
            worldPosition =
                GetWorldPosition(
                    cell.Coordinate); // 안전 위치를 찾지 못하면 기존 방 중심 위치 사용
        }

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

        encounterRuntimeIds[encounterObject] =
            runtimeEncounterId; // 조우 오브젝트 런타임 ID 등록

        encounterCoordinates[runtimeEncounterId] =
            cell.Coordinate; // 런타임 ID별 조우 좌표 등록

        encounterTypes[cell.Coordinate] =
            data.BattleType; // 현재 좌표 조우 등급 등록
    }

    private void CreateEventObject(
        ExplorationMapCell cell,
        ExplorationEventData data) // 탐사 이벤트 오브젝트 생성
    {
        if (data == null ||
            !data.IsValidData())
        {
            return;
        }

        string runtimeEventId =
            $"EV_F{CurrentFloor}_" +
            $"X{cell.Coordinate.x}_" +
            $"Y{cell.Coordinate.y}_" +
            $"S{CurrentMap.Seed}"; // 층·셀·Seed 기반 런타임 이벤트 ID 생성

        if (sessionManager.IsEventResolved(runtimeEventId))
        {
            return;
        }

        Vector2 worldPosition; // 이벤트 실제 배치 위치

        if (!tilemapView.TryGetRandomEncounterPosition(
                cell.Coordinate,
                CurrentMap.Seed ^ 473269,
                out worldPosition))
        {
            worldPosition =
                GetWorldPosition(
                    cell.Coordinate); // 안전 위치를 찾지 못하면 방 중심 위치 사용
        }

        GameObject eventObject =
            new GameObject(
                $"ExplorationEvent_{runtimeEventId}",
                typeof(SpriteRenderer),
                typeof(CircleCollider2D),
                typeof(ExplorationEventView)); // 이벤트 오브젝트 생성

        eventObject.transform.SetParent(transform); // 맵 런타임 하위 배치

        eventObject.transform.position =
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f); // 이벤트 월드 위치 지정

        eventObject.transform.localScale =
            new Vector3(
                0.68f,
                0.68f,
                1f); // 이벤트 임시 표시 크기 설정

        SpriteRenderer spriteRenderer =
            eventObject.GetComponent<SpriteRenderer>(); // 이벤트 SpriteRenderer 조회

        bool hasFixedWorldSprite =
            data.WorldSprite != null; // 이벤트별 고정 맵 스프라이트 존재 여부 확인

        spriteRenderer.sprite =
            hasFixedWorldSprite
                ? data.WorldSprite
                : runtimeSquareSprite; // 고정 스프라이트 또는 임시 사각형 표시

        spriteRenderer.color =
            hasFixedWorldSprite
                ? Color.white
                : GetEventColor(); // 고정 스프라이트 원본 색상 또는 청록색 임시 표시

        spriteRenderer.sortingOrder = 4; // 이벤트 표시 순서 지정

        if (!hasFixedWorldSprite)
        {
            CreateEventQuestionMark(
                eventObject.transform); // 임시 이벤트는 청록색 사각형 위 ? 표시
        }

        CircleCollider2D collider =
            eventObject.GetComponent<CircleCollider2D>(); // 이벤트 Collider 조회

        collider.isTrigger = true; // 이벤트 Trigger 활성화
        collider.radius = 0.56f; // 이벤트 Trigger 범위 설정

        ExplorationEventView eventView =
            eventObject.GetComponent<ExplorationEventView>(); // 이벤트 View 조회

        eventView.Initialize(
            data,
            runtimeEventId); // 이벤트 데이터와 런타임 ID 연결

        eventObjects.Add(eventObject); // 현재 층 이벤트 오브젝트 등록
        eventRuntimeIds[eventObject] = runtimeEventId; // 이벤트 오브젝트 런타임 ID 등록
        eventCoordinates[runtimeEventId] = cell.Coordinate; // 런타임 ID별 이벤트 좌표 등록
    }

    private static void CreateEventQuestionMark(
        Transform eventTransform) // 임시 이벤트 물음표 표시 생성
    {
        if (eventTransform == null)
        {
            return;
        }

        GameObject questionObject =
            new GameObject(
                "EventQuestionMark",
                typeof(TextMesh)); // 이벤트 물음표 텍스트 오브젝트 생성

        questionObject.transform.SetParent(
            eventTransform,
            false); // 이벤트 표시 오브젝트 하위 배치

        questionObject.transform.localPosition =
            new Vector3(
                0f,
                0f,
                -0.1f); // 사각형 중앙에 물음표 배치

        TextMesh questionText =
            questionObject.GetComponent<TextMesh>(); // 물음표 TextMesh 조회

        questionText.text = "?"; // 이벤트 식별용 물음표 지정
        questionText.anchor = TextAnchor.MiddleCenter; // 물음표 중앙 기준 정렬
        questionText.alignment = TextAlignment.Center; // 물음표 중앙 정렬
        questionText.fontSize = 72; // 물음표 글자 해상도 설정
        questionText.characterSize = 0.08f; // 월드 공간 물음표 크기 설정
        questionText.color = Color.white; // 물음표 흰색 표시

        MeshRenderer textRenderer =
            questionObject.GetComponent<MeshRenderer>(); // 물음표 렌더러 조회

        if (textRenderer != null)
        {
            textRenderer.sortingOrder = 5; // 이벤트 사각형보다 앞에 표시
        }
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

    private void RemoveClearedEncounterObjects() // 클리어된 조우 런타임 표시 제거
    {
        if (sessionManager == null ||
            encounterObjects.Count == 0)
        {
            return;
        }

        for (int index = encounterObjects.Count - 1;
             index >= 0;
             index--)
        {
            GameObject encounterObject =
                encounterObjects[index]; // 현재 조우 오브젝트 조회

            if (encounterObject == null)
            {
                encounterObjects.RemoveAt(index); // 이미 제거된 조우 목록 정리
                continue;
            }

            if (!encounterRuntimeIds.TryGetValue(
                    encounterObject,
                    out string runtimeEncounterId))
            {
                continue;
            }

            if (!sessionManager.IsEncounterCleared(
                    runtimeEncounterId))
            {
                continue;
            }

            if (encounterCoordinates.TryGetValue(
                    runtimeEncounterId,
                    out Vector2Int coordinate))
            {
                encounterTypes.Remove(coordinate); // 좌표별 조우 정보 제거
            }

            encounterRuntimeIds.Remove(encounterObject); // 오브젝트별 런타임 ID 제거
            encounterCoordinates.Remove(runtimeEncounterId); // 런타임 ID별 좌표 제거
            encounterObjects.RemoveAt(index); // 현재 층 조우 목록 제거

            encounterObject.SetActive(false); // Trigger와 시각 표시 즉시 비활성화
            Destroy(encounterObject); // 클리어 조우 오브젝트 제거

            Debug.Log(
                $"[Exploration][Hotfix] 클리어 조우 표시 제거 - " +
                $"{runtimeEncounterId}"); // 클리어 표시 제거 로그
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
        encounterRuntimeIds.Clear(); // 오브젝트별 런타임 ID 초기화
        encounterCoordinates.Clear(); // 런타임 ID별 좌표 초기화
        encounterTypes.Clear(); // 좌표별 조우 등급 초기화
    }

    private void RemoveResolvedEventObjects() // 처리 완료 이벤트 표시 제거
    {
        if (sessionManager == null ||
            eventObjects.Count == 0)
        {
            return;
        }

        for (int index = eventObjects.Count - 1;
             index >= 0;
             index--)
        {
            GameObject eventObject =
                eventObjects[index]; // 현재 이벤트 오브젝트 조회

            if (eventObject == null)
            {
                eventObjects.RemoveAt(index); // 이미 제거된 이벤트 목록 정리
                continue;
            }

            if (!eventRuntimeIds.TryGetValue(
                    eventObject,
                    out string runtimeEventId))
            {
                continue;
            }

            if (!sessionManager.IsEventResolved(runtimeEventId))
            {
                continue;
            }

            eventRuntimeIds.Remove(eventObject); // 오브젝트별 런타임 ID 제거
            eventCoordinates.Remove(runtimeEventId); // 런타임 ID별 좌표 제거
            eventObjects.RemoveAt(index); // 현재 층 이벤트 목록 제거

            eventObject.SetActive(false); // Trigger와 시각 표시 즉시 비활성화
            Destroy(eventObject); // 처리 완료 이벤트 오브젝트 제거

            Debug.Log(
                $"[Exploration][Day49] 처리 이벤트 표시 제거 - " +
                $"{runtimeEventId}"); // 처리 이벤트 제거 로그
        }
    }

    private void ClearEventObjects() // 현재 층 이벤트 오브젝트 정리
    {
        foreach (GameObject eventObject in eventObjects)
        {
            if (eventObject == null)
            {
                continue;
            }

            eventObject.SetActive(false); // 같은 프레임 재접촉 방지
            Destroy(eventObject); // 기존 이벤트 오브젝트 제거
        }

        eventObjects.Clear(); // 이벤트 오브젝트 목록 초기화
        eventRuntimeIds.Clear(); // 이벤트 런타임 ID 초기화
        eventCoordinates.Clear(); // 이벤트 좌표 초기화
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

    private void EnsurePartyStateAndView() // 출전 파티 HUD 준비
    {
        BattleResultManager resultManager =
            BattleResultManager.EnsureInstance(); // 전투·탐사 공용 파티 상태 관리자 준비

        partyStatusView =
            GetComponent<ExplorationPartyStatusView>(); // 기존 파티 HUD 조회

        if (partyStatusView == null)
        {
            partyStatusView =
                gameObject.AddComponent<ExplorationPartyStatusView>(); // 좌하단 파티 HUD 런타임 추가
        }

        partyStatusView.Configure(
            resultManager,
            sessionManager); // 파티 상태와 탐사 상태 HUD 연결
    }

    private void EnsureHazardComponents() // 퇴색 환경 위험 구성 요소 준비
    {
        hazardRuntime =
            GetComponent<ExplorationHazardRuntime>(); // 기존 위험 런타임 조회

        if (hazardRuntime == null)
        {
            hazardRuntime =
                gameObject.AddComponent<ExplorationHazardRuntime>(); // 위험 런타임 추가
        }

        hazardOverlayView =
            GetComponent<ExplorationHazardOverlayView>(); // 기존 위험 오버레이 조회

        if (hazardOverlayView == null)
        {
            hazardOverlayView =
                gameObject.AddComponent<ExplorationHazardOverlayView>(); // 위험 오버레이 추가
        }

        hazardView =
            GetComponent<ExplorationHazardView>(); // 기존 위험 HUD 조회

        if (hazardView == null)
        {
            hazardView =
                gameObject.AddComponent<ExplorationHazardView>(); // 위험 HUD 추가
        }

        hazardRuntime.Configure(
            this,
            sessionManager); // 위험 런타임에 현재 맵과 세션 연결

        hazardView.Configure(
            hazardRuntime,
            sessionManager); // 위험 HUD에 런타임과 세션 연결
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

    private static Color GetEventColor() // 탐사 이벤트 임시 색상 조회
    {
        return new Color(
            0.14f,
            0.70f,
            0.80f,
            1f); // 이벤트 청록색 반환
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
