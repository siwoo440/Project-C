using System.Collections.Generic; // 콘텐츠 데이터 목록 사용
using UnityEngine; // 런타임 오브젝트와 리소스 사용

public sealed class ExplorationRoomRoleRuntime : MonoBehaviour // 57일차 방 역할 콘텐츠 통합 런타임
{
    private const int ContentSeedSalt = 2047189321; // 방 콘텐츠 선택 난수 분리값

    private readonly List<GameObject> specialRoomObjects = new List<GameObject>(); // 현재 층 특수 방 표시 목록

    private ExplorationMapRuntime mapRuntime; // 기존 탐사 맵 런타임
    private int appliedSeed = int.MinValue; // 마지막 역할 적용 Seed
    private int appliedFloor = int.MinValue; // 마지막 역할 적용 층
    private ExpeditionZoneId? appliedZone; // 같은 시드와 층이라도 다른 출정의 지역 구분

    public void Initialize(ExplorationMapRuntime runtime) // 기존 탐사 맵 런타임 연결
    {
        mapRuntime = runtime; // 탐사 맵 런타임 저장
        ApplyIfNeeded(); // 연결 즉시 현재 층 역할 적용 시도
    }

    private void Start() // 방 역할 런타임 시작 처리
    {
        ApplyIfNeeded(); // 기존 탐사 맵 Start 이후 역할 적용 보장
    }

    private void LateUpdate() // F9 재생성과 층 이동 후 역할 갱신
    {
        ApplyIfNeeded(); // 현재 Seed·층 변경 여부 확인
        TryVisitCurrentRoom(); // 실제로 들어선 첫 일반 방의 나침반 판정
    }

    internal void TryVisitCurrentRoom() // 실제 플레이어가 들어선 일반 방 확인
    { // 첫 방문 확인 시작
        ExplorationSessionManager session = ExplorationSessionManager.Instance; // 현재 탐사 회차 조회
        if (session == null || !session.Compass.Pending || mapRuntime?.CurrentMap == null || !mapRuntime.TryGetPlayerRoomCoordinate(out Vector2Int coordinate) || !mapRuntime.CurrentMap.TryGetCell(coordinate, out ExplorationMapCell cell)) // 기회와 실제 방 좌표 확인
        { // 방문 대상 없는 경우 시작
            return; // 기본 방 배치 유지
        } // 방문 대상 없는 경우 종료
        ApplyIfNeeded(); // 물리 접촉이 앞선 프레임의 원래 방 역할 배치 보장
        if (session.Compass.Visit(mapRuntime.CurrentFloor, mapRuntime.CurrentMap.Seed, cell, session.EventPoolVersion, RelicRunManager.Instance?.Inventory, session.CurrentZone)) // 첫 일반 방의 보유 여부와 확률 판정
        { // 첫 방문 결과 반영 시작
            RestoreCompassEvent(session); // 성공한 방의 전투를 실제 사건으로 교체
            if (!ExplorationAutoSave.SaveCurrentExploration()) // 첫 방문 결과 자동 저장 확인
            { // 자동 저장 실패 안내 시작
                Debug.LogWarning("[ExplorationAutoSave] 나침반 판정을 저장하지 못했습니다."); // 같은 시드의 결과 유지 안내
            } // 자동 저장 실패 안내 종료
        } // 첫 방문 결과 반영 종료
    } // 첫 방문 확인 종료

    private void RestoreCompassEvent(ExplorationSessionManager session) // 같은 지도에 보관한 발견 사건 배치
    { // 나침반 사건 배치 시작
        CrackedCompassRunState compass = session.Compass; // 이번 탐사에서 보관한 사건 조회
        if (!compass.MatchesMap(mapRuntime.CurrentFloor, mapRuntime.CurrentMap.Seed) || !mapRuntime.CurrentMap.TryGetCell(compass.FoundRoom, out ExplorationMapCell room) || room.RoomType != ExplorationRoomType.Normal) // 원래 일반 방과 중복 배치 확인
        { // 복원 대상 없는 경우 시작
            return; // 원래 배치 유지
        } // 복원 대상 없는 경우 종료
        foreach (ExplorationEventData data in ExplorationEventCatalog.LoadZoneEvents(session.CurrentZone, session.EventPoolVersion)) // 저장된 버전의 사건 후보 순회
        { // 저장 원본 확인 시작
            if (data != null && data.EventId == compass.EventId) // 최초 추첨과 같은 사건 확인
            { // 발견 방 복원 시작
                room.SetRoomType(ExplorationRoomType.Event); // 일반 방의 역할을 사건으로 변경
                mapRuntime.CreateEventObject(room, data); // 미완료 사건만 플레이어 앞에 배치
                return; // 동일 위치 중복 생성 방지
            } // 발견 방 복원 종료
        } // 저장 원본 확인 종료
    } // 나침반 사건 배치 종료

    public static bool CanDescendCurrentFloor(ExplorationMapRuntime runtime) // 현재 층 진행 관문 클리어 여부 확인
    {
        if (runtime == null || runtime.CurrentMap == null) // 맵 준비 여부 확인
        {
            return true; // 기존 동작 유지
        }

        return !runtime.HasEncounterAt(runtime.CurrentMap.StairsCoordinate); // Elite·Boss 관문 조우 제거 후 이동 허용
    }

    private void ApplyIfNeeded() // 현재 층 방 역할 콘텐츠 적용
    {
        if (mapRuntime == null || mapRuntime.CurrentMap == null) // 맵 준비 여부 확인
        {
            return; // 준비 전 적용 중단
        }

        int currentSeed = mapRuntime.CurrentMap.Seed; // 현재 층 Seed 조회
        int currentFloor = mapRuntime.CurrentFloor; // 현재 탐사 층 조회

        var session = ExplorationSessionManager.EnsureInstance(); // 현재 실제 출정과 지역 콘텐츠 버전
        var zone = mapRuntime.CurrentZone; // 맵에 그린 실제 저장 지역
        if (appliedSeed == currentSeed && appliedFloor == currentFloor && appliedZone == zone) // 같은 지역과 같은 층 및 시드의 역할 적용 여부
        {
            return; // 중복 역할 적용 방지
        }

        ClearSpecialRoomObjects(); // 이전 특수 방 표시 정리
        mapRuntime.ClearEncounterObjects(); // 기존 절차 조우 전체 정리
        mapRuntime.ClearEventObjects(); // 기존 절차 이벤트 전체 정리

        List<EncounterData> allEncounters = ExpeditionZoneEncounters.LoadRandom(zone, session.EncounterPoolVersion); // 현재 지역의 일반·정예 및 기존 조우 후보
        List<EncounterData> normalEncounters = FilterEncounters(allEncounters, BattleType.Normal); // 일반 조우 분리
        List<EncounterData> eliteEncounters = FilterEncounters(allEncounters, BattleType.Elite); // 엘리트 조우 분리
        List<EncounterData> bossEncounters = FilterEncounters(allEncounters, BattleType.Boss); // 보스 조우 분리
        List<ExplorationEventData> events = LoadValidEvents(zone, session.EventPoolVersion); // 저장 당시 현재 지역의 유효 사건 후보
        System.Random random = new System.Random(currentSeed ^ ContentSeedSalt ^ (currentFloor * 733)); // 층별 콘텐츠 난수 준비

        int normalCount = 0; // 일반 방 생성 수
        int eliteCount = 0; // 엘리트 방 생성 수
        int eventCount = 0; // 이벤트 방 생성 수
        bool sawWellEye = false; // 이번 실제 배치에서 첫 재폭풍 확인 여부
        int treasureCount = 0; // 보물 방 생성 수
        int restCount = 0; // 휴식 방 생성 수
        int shopCount = 0; // 상점 방 생성 수
        int bossCount = 0; // 보스 방 생성 수

        foreach (ExplorationMapCell cell in mapRuntime.CurrentMap.Cells) // 전체 논리 방 순회
        {
            if (cell.Type == ExplorationCellType.Start) // 시작 방 확인
            {
                continue; // 시작 방 콘텐츠 생성 제외
            }

            switch (cell.RoomType) // 방 역할별 콘텐츠 생성
            {
                case ExplorationRoomType.Normal:
                    CreateEncounterForRoom(cell, BattleType.Normal, normalEncounters, allEncounters, random); // 일반 전투 생성
                    normalCount += 1; // 일반 방 수 증가
                    break;

                case ExplorationRoomType.Elite:
                    EncounterData finalElite = cell.Coordinate == mapRuntime.CurrentMap.StairsCoordinate && ExplorationFloorRules.IsFinalBossFloor(currentFloor) // 현재 지역에서만 사용할 최종 계단 관문 확인
                        ? ExpeditionZoneEncounters.GetRegionalEliteGate(zone, session.EncounterPoolVersion) : null; // 미예약 최종 출정 정산을 알리는 독립 조우
                    CreateEncounterForRoom(cell, BattleType.Elite, eliteEncounters, allEncounters, random, finalElite); // 일반 정예 또는 실제 마지막 계단 전투 배치
                    eliteCount += 1; // 엘리트 방 수 증가
                    break;

                case ExplorationRoomType.Event:
                    if (events.Count > 0 || zone == ExpeditionZoneId.WinterfallAshPlains) // 실제 지역에 유효한 사건 원본 존재 확인
                    { // 현재 지역의 사건 또는 기존 윈터폴 사건 연결 시작
                        CreateEventForRoom(cell, events, random, ref sawWellEye); // 원본 사건 및 윈터폴의 첫 재폭풍 우물 봉인 유지
                    } // 현재 지역의 사건 또는 기존 윈터폴 사건 연결 종료
                    else // 해당 지역에서 아직 작성되지 않은 사건 대조
                    { // 일반 지역 전투 대체 시작
                        cell.SetRoomType(ExplorationRoomType.Normal); // 없는 사건을 표시하지 않는 실제 일반 방 역할
                        CreateEncounterForRoom(cell, BattleType.Normal, normalEncounters, allEncounters, random); // 같은 지역의 유효한 일반 조우만 선택
                    } // 일반 지역 전투 대체 종료
                    eventCount += 1; // 이벤트 방 수 증가
                    break;

                case ExplorationRoomType.Treasure:
                    CreateSpecialRoomObject(cell, ExplorationRoomType.Treasure); // 보물 상호작용 생성
                    treasureCount += 1; // 보물 방 수 증가
                    break;

                case ExplorationRoomType.Rest:
                    CreateSpecialRoomObject(cell, ExplorationRoomType.Rest); // 휴식 상호작용 생성
                    restCount += 1; // 휴식 방 수 증가
                    break;

                case ExplorationRoomType.Shop:
                    CreateSpecialRoomObject(cell, ExplorationRoomType.Shop); // 상점 상호작용 생성
                    shopCount += 1; // 상점 방 수 증가
                    break;

                case ExplorationRoomType.Boss:
                    EncounterData optionalBoss = mapRuntime.GetOptionalBossEncounter(cell.Coordinate); // 현재 방에 확정한 선택 보스 여부
                    if (optionalBoss != null) // 재의 파수꾼 전용 선택 원본 확인
                    { // 선택 조우 배치 시작
                        CreateEncounterForRoom(cell, BattleType.Normal, normalEncounters, allEncounters, random, optionalBoss); // 원래 일반 방 난수와 전용 조우 연결
                    } // 선택 조우 배치 종료
                    else // 기존 필수 관문 보스 배치 경로
                    { // 보스 관문 조우 배치 시작
                        EncounterData regionalBoss = zone == ExpeditionZoneId.WinterfallAshPlains ? null : ExpeditionZoneEncounters.GetMandatoryBoss(zone, session.EncounterPoolVersion); // 기존 윈터폴 추첨 또는 예약한 새 지역 보스
                        CreateEncounterForRoom(cell, BattleType.Boss, bossEncounters, allEncounters, random, regionalBoss); // 같은 지역의 실제 필수 관문 조우
                    } // 보스 관문 조우 배치 종료
                    bossCount += 1; // 보스 방 수 증가
                    break;
            }
        }

        RestoreCompassEvent(ExplorationSessionManager.EnsureInstance()); // 원래 방의 추첨 순서를 유지한 뒤 발견 사건 복원
        appliedSeed = currentSeed; // 현재 층 Seed 적용 완료 기록
        appliedFloor = currentFloor; // 현재 층 번호 적용 완료 기록
        appliedZone = zone; // 현재 출정 지역 적용 완료 기록

        Debug.Log(
            $"[Exploration][Day57] 방 역할 통합 완료 - Floor {currentFloor} / " +
            $"Seed {currentSeed} / 일반 {normalCount} / 엘리트 {eliteCount} / 이벤트 {eventCount} / " +
            $"보물 {treasureCount} / 휴식 {restCount} / 상점 {shopCount} / 보스 {bossCount}"); // 방 역할 결과 로그
    }

    private void CreateEncounterForRoom(
        ExplorationMapCell cell,
        BattleType requestedType,
        List<EncounterData> requestedPool,
        List<EncounterData> fallbackPool, // 현재 층의 대체 가능 조우 목록
        System.Random random, // 원래 방 역할의 추첨 난수
        EncounterData fixedEncounter = null) // 선택 방의 전용 고정 원본
    { // 방 역할 조우 생성 시작
        if (cell == null || fallbackPool.Count == 0 && fixedEncounter == null) // 방과 실제로 사용할 수 있는 조우 확인
        {
            return; // 조우 생성 불가 처리
        }

        List<EncounterData> sourcePool =
            requestedPool.Count > 0
                ? requestedPool
                : fallbackPool; // 요청 등급 데이터 누락 시 전체 유효 조우 대체

        EncounterData encounterData = sourcePool.Count > 0 ? sourcePool[random.Next(sourcePool.Count)] : null; // 기존 방에서 사용했을 난수와 조우 원본 선택
        if (fixedEncounter != null) // 선택된 방에만 전용 보스 사용 확인
        { // 선택 보스 실물 배치 시작
            mapRuntime.CreateEncounterObject(cell, fixedEncounter); // 같은 접촉과 전투 흐름의 보스 배치
            return; // 나머지 방의 기존 난수 순서 보존
        } // 선택 보스 실물 배치 종료

        if (requestedPool.Count == 0) // 요청 등급 대체 여부 확인
        {
            Debug.LogWarning(
                $"[Exploration][Day57] {requestedType} 전용 EncounterData가 없어 " +
                $"{encounterData.BattleType} 조우로 대체했습니다.",
                this); // 등급 데이터 누락 안내
        }

        mapRuntime.CreateEncounterObject(cell, encounterData); // 정식 맵 API로 조우 생성
    }

    private void CreateEventForRoom(
        ExplorationMapCell cell,
        List<ExplorationEventData> events,
        System.Random random, // 기존 방 콘텐츠 추첨 난수
        ref bool sawWellEye) // 이번 층의 첫 재폭풍 여부 공유
    {
        if (cell == null || events.Count == 0) // 방과 이벤트 데이터 존재 확인
        {
            return; // 이벤트 생성 불가 처리
        }

        ExplorationEventData eventData = events[random.Next(events.Count)]; // Seed 기반 이벤트 선택
        eventData = ExplorationSessionManager.EnsureInstance().Well.SelectEvent(eventData, events, mapRuntime.CurrentMap.Seed, ref sawWellEye, true); // 실제 첫 재폭풍에만 봉인 판정과 소모 기록
        mapRuntime.CreateEventObject(cell, eventData); // 정식 맵 API로 이벤트 생성
    }

    private void CreateSpecialRoomObject(
        ExplorationMapCell cell,
        ExplorationRoomType roomType) // 보물·휴식·상점 방 상호작용 생성
    {
        if (cell == null) // 방 데이터 존재 확인
        {
            return; // 특수 방 생성 중단
        }

        string runtimeId =
            $"SR_{roomType}_F{mapRuntime.CurrentFloor}_" +
            $"X{cell.Coordinate.x}_Y{cell.Coordinate.y}_" +
            $"S{mapRuntime.CurrentMap.Seed}"; // 특수 방 고유 런타임 ID 생성

        ExplorationSessionManager sessionManager = ExplorationSessionManager.EnsureInstance(); // 특수 방 상태 관리자 준비

        if (sessionManager.IsEventResolved(runtimeId) && !sessionManager.Cart.IsMarkedShop(runtimeId)) // 결제 전 표식 상점의 재방문 허용
        {
            return; // 일회성 특수 방 재생성 제외
        }

        Vector2 worldPosition = mapRuntime.GetWorldPosition(cell.Coordinate); // 방 중심 월드 위치 조회

        GameObject roomObject = new GameObject(
            $"SpecialRoom_{roomType}_{cell.Coordinate.x}_{cell.Coordinate.y}",
            typeof(SpriteRenderer),
            typeof(CircleCollider2D),
            typeof(ExplorationSpecialRoomView)); // 특수 방 상호작용 오브젝트 생성

        roomObject.transform.SetParent(transform); // 탐사 맵 런타임 하위 배치
        roomObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f); // 방 중심 위치 지정
        roomObject.transform.localScale = new Vector3(0.86f, 0.86f, 1f); // 특수 방 표시 크기 설정

        SpriteRenderer spriteRenderer = roomObject.GetComponent<SpriteRenderer>(); // 특수 방 렌더러 조회
        spriteRenderer.sprite = ExplorationSpecialRoomView.GetRuntimeMarkerSprite(); // 임시 방 표시 스프라이트 지정
        spriteRenderer.color = ExplorationSpecialRoomView.GetRoomColor(roomType); // 방 역할별 임시 색상 지정
        spriteRenderer.sortingOrder = 4; // 탐사 오브젝트 표시 순서 지정

        CircleCollider2D collider = roomObject.GetComponent<CircleCollider2D>(); // 특수 방 Trigger 조회
        collider.isTrigger = true; // 특수 방 Trigger 활성화
        collider.radius = 0.7f; // 특수 방 상호작용 범위 설정

        ExplorationSpecialRoomView specialRoomView = roomObject.GetComponent<ExplorationSpecialRoomView>(); // 특수 방 View 조회
        specialRoomView.Initialize(mapRuntime, cell.Coordinate, roomType, runtimeId); // 방 역할과 상태 연결

        specialRoomObjects.Add(roomObject); // 현재 층 특수 방 표시 등록
    }

    private static List<EncounterData> FilterEncounters(
        List<EncounterData> source,
        BattleType battleType) // 전투 등급별 조우 데이터 분리
    {
        List<EncounterData> result = new List<EncounterData>(); // 등급별 조우 목록 생성

        foreach (EncounterData data in source) // 전체 조우 데이터 순회
        {
            if (data.BattleType == battleType) // 요청 등급 일치 확인
            {
                result.Add(data); // 등급별 조우 목록 등록
            }
        }

        return result; // 등급별 조우 목록 반환
    }

    private static List<ExplorationEventData> LoadValidEvents(ExpeditionZoneId zone, int poolVersion) // 현재 지역에서 사용할 유효 사건 원본
    {
        IReadOnlyList<ExplorationEventData> loadedData = ExplorationEventCatalog.LoadZoneEvents(zone, poolVersion); // 현재 지역과 저장 버전의 고정 사건 조회
        List<ExplorationEventData> result = new List<ExplorationEventData>(); // 유효 이벤트 목록 생성

        if (loadedData == null) // 이벤트 카탈로그 존재 확인
        {
            return result; // 빈 이벤트 목록 반환
        }

        foreach (ExplorationEventData data in loadedData) // 전체 이벤트 데이터 순회
        {
            if (data != null && data.IsValidData()) // 유효 이벤트 확인
            {
                result.Add(data); // 유효 이벤트 목록 등록
            }
        }

        result.Sort(
            (left, right) =>
                string.CompareOrdinal(left.EventId, right.EventId)); // Seed 재현용 이벤트 ID 정렬

        return result; // 유효 이벤트 목록 반환
    }

    private void ClearSpecialRoomObjects() // 현재 층 특수 방 표시 정리
    {
        foreach (GameObject roomObject in specialRoomObjects) // 특수 방 표시 순회
        {
            if (roomObject != null) // 남아 있는 특수 방 확인
            {
                roomObject.SetActive(false); // 같은 프레임 Trigger 재접촉 방지
                Destroy(roomObject); // 기존 특수 방 표시 제거
            }
        }

        specialRoomObjects.Clear(); // 특수 방 표시 목록 초기화
    }

    private void OnDestroy() // 방 역할 런타임 제거 처리
    {
        ClearSpecialRoomObjects(); // 남은 특수 방 표시 정리
    }
}
