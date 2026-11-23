using System; // 예외 처리 사용
using System.Collections.Generic; // 콘텐츠 데이터 목록 사용
using System.Reflection; // 기존 맵 런타임 생성 기능 재사용
using UnityEngine; // 런타임 오브젝트와 리소스 사용

public sealed class ExplorationRoomRoleRuntime : MonoBehaviour // 56일차 방 역할 콘텐츠 통합 런타임
{
    private const int ContentSeedSalt = 2047189321; // 방 콘텐츠 선택 난수 분리값

    private static readonly BindingFlags PrivateInstanceFlags =
        BindingFlags.Instance | BindingFlags.NonPublic; // 기존 맵 비공개 기능 조회 범위

    private static readonly MethodInfo ClearEncounterObjectsMethod =
        typeof(ExplorationMapRuntime).GetMethod("ClearEncounterObjects", PrivateInstanceFlags); // 기존 조우 전체 정리 기능

    private static readonly MethodInfo ClearEventObjectsMethod =
        typeof(ExplorationMapRuntime).GetMethod("ClearEventObjects", PrivateInstanceFlags); // 기존 이벤트 전체 정리 기능

    private static readonly MethodInfo CreateEncounterObjectMethod =
        typeof(ExplorationMapRuntime).GetMethod("CreateEncounterObject", PrivateInstanceFlags); // 기존 조우 생성 기능

    private static readonly MethodInfo CreateEventObjectMethod =
        typeof(ExplorationMapRuntime).GetMethod("CreateEventObject", PrivateInstanceFlags); // 기존 이벤트 생성 기능

    private readonly List<GameObject> specialRoomObjects = new List<GameObject>(); // 현재 층 특수 방 표시 목록

    private ExplorationMapRuntime mapRuntime; // 기존 탐사 맵 런타임
    private int appliedSeed = int.MinValue; // 마지막 역할 적용 Seed
    private int appliedFloor = int.MinValue; // 마지막 역할 적용 층
    private bool reflectionErrorLogged; // 기존 비공개 기능 오류 로그 여부

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
        ApplyIfNeeded(); // 현재 Seed 변경 여부 확인 후 재적용
    }

    public static bool CanDescendCurrentFloor(ExplorationMapRuntime runtime) // 현재 층 계단 이동 가능 여부 확인
    {
        if (runtime == null || runtime.CurrentMap == null) // 맵 준비 여부 확인
        {
            return true; // 기존 동작 유지
        }

        if (!runtime.CurrentMap.TryGetCell(
                runtime.CurrentMap.StairsCoordinate,
                out ExplorationMapCell stairsCell)) // 계단 방 데이터 조회
        {
            return true; // 계단 데이터 누락 시 기존 동작 유지
        }

        if (stairsCell.RoomType != ExplorationRoomType.Boss) // 계단 방 보스 역할 여부 확인
        {
            return true; // 비보스 계단 이동 허용
        }

        return !runtime.HasEncounterAt(runtime.CurrentMap.StairsCoordinate); // 보스 조우 제거 후 이동 허용
    }

    private void ApplyIfNeeded() // 현재 층 방 역할 콘텐츠 적용
    {
        if (mapRuntime == null || mapRuntime.CurrentMap == null) // 맵 준비 여부 확인
        {
            return; // 준비 전 적용 중단
        }

        int currentSeed = mapRuntime.CurrentMap.Seed; // 현재 층 Seed 조회
        int currentFloor = mapRuntime.CurrentFloor; // 현재 탐사 층 조회
        if (appliedSeed == currentSeed && appliedFloor == currentFloor) // 동일 층 적용 완료 여부 확인
        {
            return; // 중복 역할 적용 방지
        }

        if (!CanReuseMapRuntimeContentMethods()) // 기존 맵 생성 기능 재사용 가능 여부 확인
        {
            return; // 비공개 기능 누락 시 적용 중단
        }

        ClearSpecialRoomObjects(); // 이전 특수 방 표시 정리
        InvokeMapMethod(ClearEncounterObjectsMethod); // 기존 랜덤 조우 전체 정리
        InvokeMapMethod(ClearEventObjectsMethod); // 기존 랜덤 이벤트 전체 정리

        List<EncounterData> allEncounters = LoadValidEncounters(); // 전체 유효 조우 데이터 로드
        List<EncounterData> normalEncounters = FilterEncounters(allEncounters, BattleType.Normal); // 일반 조우 데이터 분리
        List<EncounterData> eliteEncounters = FilterEncounters(allEncounters, BattleType.Elite); // 엘리트 조우 데이터 분리
        List<EncounterData> bossEncounters = FilterEncounters(allEncounters, BattleType.Boss); // 보스 조우 데이터 분리
        List<ExplorationEventData> events = LoadValidEvents(); // 전체 유효 탐사 이벤트 로드
        System.Random random = new System.Random(currentSeed ^ ContentSeedSalt); // 현재 층 콘텐츠 선택 난수 준비

        int normalCount = 0; // 일반 방 생성 수
        int eliteCount = 0; // 엘리트 방 생성 수
        int eventCount = 0; // 이벤트 방 생성 수
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
                    CreateEncounterForRoom(cell, BattleType.Elite, eliteEncounters, allEncounters, random); // 엘리트 전투 생성
                    eliteCount += 1; // 엘리트 방 수 증가
                    break;

                case ExplorationRoomType.Event:
                    CreateEventForRoom(cell, events, random); // 탐사 이벤트 생성
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
                    CreateEncounterForRoom(cell, BattleType.Boss, bossEncounters, allEncounters, random); // 최장 거리 보스 전투 생성
                    bossCount += 1; // 보스 방 수 증가
                    break;
            }
        }

        appliedSeed = currentSeed; // 현재 층 Seed 적용 완료 기록
        appliedFloor = currentFloor; // 현재 층 번호 적용 완료 기록

        Debug.Log(
            $"[Exploration][Day56] 방 역할 적용 완료 - " +
            $"Seed {currentSeed} / 일반 {normalCount} / 엘리트 {eliteCount} / 이벤트 {eventCount} / " +
            $"보물 {treasureCount} / 휴식 {restCount} / 상점 {shopCount} / 보스 {bossCount}"); // 현재 층 방 역할 결과 로그
    }

    private bool CanReuseMapRuntimeContentMethods() // 기존 맵 콘텐츠 비공개 기능 존재 확인
    {
        bool valid =
            ClearEncounterObjectsMethod != null &&
            ClearEventObjectsMethod != null &&
            CreateEncounterObjectMethod != null &&
            CreateEventObjectMethod != null; // 필수 기존 기능 존재 여부 계산

        if (!valid && !reflectionErrorLogged) // 최초 기능 누락 확인
        {
            reflectionErrorLogged = true; // 중복 오류 로그 방지
            Debug.LogError(
                "[Exploration][Day56] ExplorationMapRuntime의 기존 콘텐츠 생성 기능을 찾지 못했습니다. " +
                "57일차 통합 전에 메서드 이름 변경 여부를 확인하세요.",
                this); // 기존 기능 연결 실패 안내
        }

        return valid; // 기존 기능 재사용 가능 여부 반환
    }

    private void InvokeMapMethod(MethodInfo methodInfo, params object[] parameters) // 기존 맵 비공개 기능 호출
    {
        try
        {
            methodInfo.Invoke(mapRuntime, parameters); // 현재 맵 런타임 기능 실행
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Exploration][Day56] 기존 맵 콘텐츠 기능 호출 실패 - {methodInfo.Name}\n{exception}",
                this); // Prototype 연결 실패 로그
        }
    }

    private void CreateEncounterForRoom(
        ExplorationMapCell cell,
        BattleType requestedType,
        List<EncounterData> requestedPool,
        List<EncounterData> fallbackPool,
        System.Random random) // 방 역할에 맞는 전투 조우 생성
    {
        if (cell == null || fallbackPool.Count == 0) // 방과 조우 데이터 존재 확인
        {
            return; // 조우 생성 불가 처리
        }

        List<EncounterData> sourcePool =
            requestedPool.Count > 0
                ? requestedPool
                : fallbackPool; // 요청 등급 데이터 누락 시 전체 유효 조우 대체

        EncounterData encounterData =
            sourcePool[random.Next(sourcePool.Count)]; // Seed 기반 조우 데이터 선택

        if (requestedPool.Count == 0) // 요청 등급 대체 여부 확인
        {
            Debug.LogWarning(
                $"[Exploration][Day56] {requestedType} 전용 EncounterData가 없어 {encounterData.BattleType} 조우로 대체했습니다.",
                this); // 등급 데이터 누락 안내
        }

        InvokeMapMethod(
            CreateEncounterObjectMethod,
            cell,
            encounterData); // 기존 조우 생성·세션 등록 흐름 재사용
    }

    private void CreateEventForRoom(
        ExplorationMapCell cell,
        List<ExplorationEventData> events,
        System.Random random) // 이벤트 방 콘텐츠 생성
    {
        if (cell == null || events.Count == 0) // 방과 이벤트 데이터 존재 확인
        {
            return; // 이벤트 생성 불가 처리
        }

        ExplorationEventData eventData =
            events[random.Next(events.Count)]; // Seed 기반 이벤트 데이터 선택

        InvokeMapMethod(
            CreateEventObjectMethod,
            cell,
            eventData); // 기존 이벤트 생성·세션 등록 흐름 재사용
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

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 특수 방 사용 상태 관리자 준비

        if ((roomType == ExplorationRoomType.Treasure ||
             roomType == ExplorationRoomType.Rest) &&
            sessionManager.IsEventResolved(runtimeId)) // 이미 사용한 일회성 특수 방 확인
        {
            return; // 사용 완료 특수 방 재생성 제외
        }

        Vector2 worldPosition =
            mapRuntime.GetWorldPosition(cell.Coordinate); // 방 중심 월드 위치 조회

        GameObject roomObject = new GameObject(
            $"SpecialRoom_{roomType}_{cell.Coordinate.x}_{cell.Coordinate.y}",
            typeof(SpriteRenderer),
            typeof(CircleCollider2D),
            typeof(ExplorationSpecialRoomView)); // 특수 방 상호작용 오브젝트 생성

        roomObject.transform.SetParent(transform); // 탐사 맵 런타임 하위 배치
        roomObject.transform.position = new Vector3(
            worldPosition.x,
            worldPosition.y,
            0f); // 방 중심 위치 지정

        roomObject.transform.localScale = new Vector3(
            0.86f,
            0.86f,
            1f); // 특수 방 표시 크기 설정

        SpriteRenderer spriteRenderer =
            roomObject.GetComponent<SpriteRenderer>(); // 특수 방 렌더러 조회

        spriteRenderer.sprite =
            ExplorationSpecialRoomView.GetRuntimeMarkerSprite(); // 공용 특수 방 임시 스프라이트 지정

        spriteRenderer.color =
            ExplorationSpecialRoomView.GetRoomColor(roomType); // 방 역할별 임시 색상 지정

        spriteRenderer.sortingOrder = 4; // 탐사 오브젝트 표시 순서 지정

        CircleCollider2D collider =
            roomObject.GetComponent<CircleCollider2D>(); // 특수 방 Trigger 조회

        collider.isTrigger = true; // 특수 방 Trigger 활성화
        collider.radius = 0.7f; // 특수 방 상호작용 범위 설정

        ExplorationSpecialRoomView specialRoomView =
            roomObject.GetComponent<ExplorationSpecialRoomView>(); // 특수 방 View 조회

        specialRoomView.Initialize(
            mapRuntime,
            cell.Coordinate,
            roomType,
            runtimeId); // 방 역할과 상태 연결

        specialRoomObjects.Add(roomObject); // 현재 층 특수 방 표시 등록
    }

    private static List<EncounterData> LoadValidEncounters() // 유효 조우 데이터 로드
    {
        EncounterData[] loadedData = Resources.LoadAll<EncounterData>("Encounters"); // 전체 조우 리소스 로드
        List<EncounterData> result = new List<EncounterData>(); // 유효 조우 목록 생성

        foreach (EncounterData data in loadedData) // 전체 조우 데이터 순회
        {
            if (data != null && data.IsValidData()) // 유효 조우 데이터 확인
            {
                result.Add(data); // 유효 조우 목록 등록
            }
        }

        result.Sort(
            (left, right) =>
                string.CompareOrdinal(left.EncounterId, right.EncounterId)); // Seed 재현용 ID 정렬

        return result; // 유효 조우 목록 반환
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

    private static List<ExplorationEventData> LoadValidEvents() // 유효 이벤트 데이터 로드
    {
        IReadOnlyList<ExplorationEventData> loadedData =
            ExplorationEventCatalog.LoadEvents(); // 기존 이벤트 카탈로그 로드

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
