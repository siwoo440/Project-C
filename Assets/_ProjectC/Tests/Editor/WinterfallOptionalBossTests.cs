using System; // 독립 저장 파일 식별자
using System.Collections; // 플레이 모드 전환 대기
using System.Collections.Generic; // 독립 지도 셀 목록
using System.IO; // 실제 저장 파일 읽기
using System.Linq; // 실제 조우 목록과 기존 파티 원본 비교
using System.Reflection; // 게임 내부 배치 경로 접근
using System.Text.RegularExpressions; // 이전 저장 필드 재현
using NUnit.Framework; // 추첨과 저장 검증
using UnityEngine; // 지도 좌표와 리소스
using UnityEngine.TestTools; // 플레이 모드 검사

public sealed class WinterfallOptionalBossTests // 녹슨 표지판의 층 생성과 저장 검사
{ // 선택 보스 검사 시작
    private static readonly MethodInfo SelectRoom = typeof(ExplorationMapRuntime).Assembly.GetType("OptionalBossPlacement")?.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic); // 격리된 선택 보스 추첨 연결
    private const string OptionalEncounterResource = "OptionalEncounters/Encounter_WF_AshSentinelOptionalBoss"; // 기본 관문 풀과 분리된 실제 보스 원본
    private static readonly MethodInfo BuildFloor = typeof(ExplorationMapRuntime).GetMethod("BuildCurrentFloor", BindingFlags.Instance | BindingFlags.NonPublic); // 신규 층의 실제 지도 생성 경로 연결
    private static readonly PropertyInfo FrozenRoom = typeof(ExplorationSessionManager).GetProperty("CurrentFloorOptionalBossRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 저장되는 선택 보스 위치 접근

    [Test] // 정확한 백분율 경계와 선택 방 검증
    public void RollUsesFivePointBoundaryAndLeavesTraversalUntouched() // 필수 진행 방과 다른 일반 방 유지 확인
    { // 고정 시드 추첨 검사 시작
        var success = BranchMap(40); // 추첨값 사인 고정 맵
        var failure = BranchMap(7); // 추첨값 오인 고정 맵
        Assert.AreEqual((Vector2Int?)Vector2Int.right, Select(success, 1, 5)); // 오 퍼센트 미만인 안전한 말단 방 선택
        Assert.IsNull(Select(failure, 1, 5)); // 같은 경계 오에서 추첨 실패
        Assert.IsNull(Select(success, 1, 0)); // 유물 없는 생성의 기존 배치
        Assert.IsNull(Select(success, 5, 100)); // 오 층 보스 관문에서 전체 추첨 제외
        Assert.IsNull(Select(success, 10, 100)); // 십 층 최종 관문에서 전체 추첨 제외
        Assert.IsNull(Select(ExplorationMapGenerator.Generate(2, 40, 1), 1, 100)); // 출발과 계단뿐인 지도에서 선택 불가
        Assert.IsTrue(success.TryGetCell(success.StairsCoordinate, out var gate)); // 실제 진행 관문 조회
        Assert.AreEqual(ExplorationRoomType.Elite, gate.RoomType); // 일반 층의 정예 관문 보존
        Assert.IsTrue(success.TryGetCell(Vector2Int.up, out var passage)); // 진행 통로의 일반 방 조회
        Assert.AreEqual(ExplorationRoomType.Normal, passage.RoomType); // 계단으로 이어지는 일반 방 유지
    } // 고정 시드 추첨 검사 종료

    [UnityTest] // 기존 탐사 저장과 새 선택 보스 위치 왕복 검증
    public IEnumerator GeneratedChoiceSurvivesSaleAndOldSavesRemainUnchanged() // 판매 이후와 유물 보유 이전 저장 확인
    { // 실제 탐사 상태 검사 시작
        yield return new EnterPlayMode(); // 실제 저장 관리자 동작 준비
        yield return GeneratedChoiceSurvivesSaleAndOldSavesRemainUnchangedInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 독립 플레이 모드 종료
    } // 실제 탐사 상태 검사 종료

    private IEnumerator GeneratedChoiceSurvivesSaleAndOldSavesRemainUnchangedInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".json"); // 사용자 저장과 독립된 파일
        var roots = new List<GameObject>(); // 실제 탐사 지도와 관련된 검사 객체 목록
        try // 실제 파티와 유물 저장 검사
        { // 저장 검사 시작
            var relic = Resources.Load<RelicData>("Relics/RustedSignpost"); // 정식 녹슨 표지판 원본 조회
            Assert.IsNotNull(relic); // 유물 리소스 배치 확인
            Assert.AreEqual(5, relic.EffectValue); // 실제 자산의 오 퍼센트포인트 확인
            var relics = RelicRunManager.EnsureInstance(); // 회차 유물 관리자 조회
            var session = PrepareParty(); // 정상 저장이 가능한 독립 탐사 파티
            const int seed = 5; // 실제 열네 셀 지도에서 성공하는 고정 시드
            session.SetCurrentFloorSeed(seed); // 실제 최초 지도 생성 시드 적용
            var baseline = BuildMap(session, roots, seed, true); // 유물과 선택 결과가 없는 원래 지도의 실제 콘텐츠
            relics.TryAcquire(relic); // 정식 인벤토리에 표지판 등록
            var failure = BuildMap(session, roots, 7, false); // 성공 경계 바로 위인 시드의 실제 신규 지도
            Assert.IsNull(GetFrozenRoom(session)); // 실패한 새 층의 선택 위치 없음 확인
            Assert.IsTrue(failure.CurrentMap.Cells.All(cell => cell.RoomType != ExplorationRoomType.Boss)); // 실패한 실제 층에 보스 표시 없음 확인
            var map = BuildMap(session, roots, seed, false); // 보유 유물을 반영한 실제 지도와 콘텐츠 생성
            var selected = GetFrozenRoom(session); // 최초 선택 방 위치 보관
            Assert.AreEqual((Vector2Int?)new Vector2Int(-3, 0), selected); // 보유한 신규 층의 재현되는 선택 방 확인
            string selectedName = "Encounter_" + EncounterId(map, selected.Value); // 원래 일반 방과 비교할 런타임 조우 이름
            Assert.AreEqual(baseline.CurrentMap.StairsCoordinate, map.CurrentMap.StairsCoordinate); // 지도 생성에서 필수 계단 좌표 유지
            Assert.AreEqual(BattleType.Normal, Encounter(baseline, selected.Value).BattleType); // 효과 전 선택 방의 일반 조우 확인
            Assert.AreEqual(BattleType.Boss, Encounter(map, selected.Value).BattleType); // 실제 선택 방에 나타난 보스 등급 확인
            AssertEquivalentOtherEncounters(baseline, map, selectedName); // 나머지 조우와 관문 원본의 동일성 확인
            Assert.IsTrue(relics.TryRemove(relic.RelicId)); // 해당 층에서 획득 유물 판매를 대신할 제거
            Assert.AreEqual(selected, GetFrozenRoom(session)); // 판매가 이미 생성된 방에 영향 없음 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero) && ExplorationSaveService.Load(path)); // 판매 뒤 실제 파일 저장과 복원
            Assert.AreEqual(selected, GetFrozenRoom(session)); // 유물 없는 저장에서도 선택 위치 유지
            Assert.IsNull(relics.Inventory.FindEffect(RelicEffectType.OptionalBossNodeChance)); // 복원에 유물 보유가 필요 없는지 확인
            var restored = BuildMap(session, roots, seed, true); // 실제 저장 후 다시 생성하는 탐사 지도
            Assert.AreSame(Encounter(map, selected.Value), Encounter(restored, selected.Value)); // 판매한 저장에서도 원래 보스 조우 원본 재배치
            relics.TryAcquire(relic); // 같은 층에서 표지판 재획득
            Assert.AreEqual(selected, GetFrozenRoom(session)); // 재구매에서 이미 선택된 층 유지
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 표지판을 보유한 실제 저장 파일 생성
            string modern = File.ReadAllText(path); // 새 저장 형태 조회
            string legacy = Regex.Replace(modern, "\"(?:hasOptionalBossRoom|optionalBossRoom)\":(?:true|false|\\{[^{}]*\\}),", string.Empty); // 선택 방 필드가 없던 이전 저장 재현
            Assert.AreNotEqual(modern, legacy); // 최신 저장과 이전 저장 형식의 차이 확인
            File.WriteAllText(path, legacy); // 표지판이 있지만 기존 맵을 사용하는 파일 구성
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 형식도 유효하게 읽는지 확인
            Assert.IsNotNull(relics.Inventory.FindEffect(RelicEffectType.OptionalBossNodeChance)); // 레거시 복원에서도 보유 유물 유지 확인
            Assert.IsNull(GetFrozenRoom(session)); // 성공 시드와 보유 유물에도 이전 지도 유지
            var older = BuildMap(session, roots, seed, true); // 구버전 복원 경로의 실제 탐사 지도
            Assert.AreEqual(BattleType.Normal, Encounter(older, selected.Value).BattleType); // 이전 저장에서 선택 보스 대신 원래 일반 조우 유지
            session.AdvanceFloor(); // 새 층 진입으로 이전 지도 상태 초기화
            Assert.IsNull(GetFrozenRoom(session)); // 새 층의 생성 전 선택 위치 초기값 확인
        } // 저장 검사 종료
        finally // 독립 파일 정리
        { // 정리 시작
            ExplorationSaveService.Remove(path); // 이번 검사 파일과 복구본 제거
            foreach (GameObject root in roots) // 실제 지도 검사 객체 순회
            { // 지도 검사 객체 정리 시작
                UnityEngine.Object.Destroy(root); // 생성한 지도와 콘텐츠 제거
            } // 지도 검사 객체 정리 종료
        } // 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 전용 보스 조우와 기존 관문 동작 검증
    public IEnumerator OptionalBossIsAnAuthoredBattleAndDoesNotGateTheStairs() // 선택 조우 피해 결과와 실제 지도 진행 검증
    { // 선택 방과 관문 비교 시작
        yield return new EnterPlayMode(); // 실제 지도 및 방 접촉 구성 준비
        yield return OptionalBossIsAnAuthoredBattleAndDoesNotGateTheStairsInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 탐사 경로 검사 종료
    } // 선택 방과 관문 비교 종료

    private IEnumerator OptionalBossIsAnAuthoredBattleAndDoesNotGateTheStairsInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var roots = new List<GameObject>(); // 검사 전용 지도 표시 목록
        try // 선택 보스와 필수 관문의 실제 결과 적용
        { // 실제 진행 검사 시작
            var session = PrepareParty(); // 정상 탐사와 승리 정산이 가능한 파티
            var relic = Resources.Load<RelicData>("Relics/RustedSignpost"); // 녹슨 표지판 원본
            RelicRunManager.EnsureInstance().TryAcquire(relic); // 새 층 생성 시점의 실제 유물 보유
            var map = BuildMap(session, roots, 5, false); // 선택 보스가 생성되는 일반 층 지도
            Vector2Int selected = GetFrozenRoom(session).Value; // 관문에서 분리된 실제 선택 좌표
            Vector2Int gate = map.CurrentMap.StairsCoordinate; // 기존 필수 계단 방 좌표
            var optional = Resources.Load<EncounterData>(OptionalEncounterResource); // 추가한 전용 보스 조우 원본
            var elite = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshSentinel"); // 유지한 재의 파수꾼 정예 조우 원본
            Assert.AreSame(optional, Encounter(map, selected)); // 선택 위치에 실제 보스 조우 표식 표시 확인
            Assert.AreEqual(BattleType.Boss, optional.BattleType); // 전투와 상태 화면에서 사용하는 보스 등급 확인
            Assert.AreEqual(BattleType.Elite, elite.BattleType); // 원래 정예 조우의 등급 유지 확인
            CollectionAssert.AreEqual(elite.Enemies, optional.Enemies); // 원래 재의 파수꾼 적 편성 참조 확인
            CollectionAssert.DoesNotContain(Resources.LoadAll<EncounterData>("Encounters"), optional); // 필수 보스 및 일반 조우 풀에서 전용 원본 격리 확인
            Assert.IsTrue(map.TryGetEncounterTypeAt(selected, out var visibleType) && visibleType == BattleType.Boss); // 지도 등급 표시에 실제 선택 보스 사용
            Assert.IsTrue(map.CurrentMap.TryGetCell(selected, out var room) && room.Type == ExplorationCellType.Normal && room.RoomType == ExplorationRoomType.Boss); // 선택 보스 방의 논리 역할과 관문 구조 분리 확인
            Assert.IsFalse(ExplorationRoomRoleRuntime.CanDescendCurrentFloor(map)); // 기존 필수 관문 전투만 계단 이동 차단
            WinEncounter(session, EncounterId(map, selected), optional); // 선택 보스의 정상 승리와 보상 정산
            map.SendMessage("RemoveClearedEncounterObjects"); // 게임의 실제 클리어된 지도 표식 제거 호출
            Assert.IsFalse(map.HasEncounterAt(selected)); // 완료한 같은 선택 방 보스 재도전 차단
            Assert.IsTrue(map.HasEncounterAt(gate)); // 선택 보스 승리로 필수 관문 전투 보존
            Assert.IsFalse(ExplorationRoomRoleRuntime.CanDescendCurrentFloor(map)); // 선택 보스 승리만으로 계단 미개방 확인
            Assert.IsFalse(session.IsExplorationCompleted); // 추가 보스의 정산으로 탐사 미종료 확인
            int secondSeed = Enumerable.Range(6, 250).First(seed => Select(ExplorationMapGenerator.Generate(14, seed, 1), 1, 5) != null); // 첫 지도와 다른 성공 시드 조회
            var secondMap = BuildMap(session, roots, secondSeed, false); // 별도 성공 시드에서 선택 방 생존과 관문 승리 대조
            Vector2Int secondSelected = GetFrozenRoom(session).Value; // 두 번째 지도에 존재하는 선택 보스 방
            Vector2Int secondGate = secondMap.CurrentMap.StairsCoordinate; // 두 번째 지도의 필수 정예 관문
            WinEncounter(session, EncounterId(secondMap, secondGate), Encounter(secondMap, secondGate)); // 기존 필수 관문 승리만 실제 정산
            secondMap.SendMessage("RemoveClearedEncounterObjects"); // 필수 관문 전투 완료를 실제 지도에 반영
            Assert.IsTrue(secondMap.HasEncounterAt(secondSelected)); // 계단을 먼저 정리한 때에도 별도 보스 유지
            Assert.IsTrue(ExplorationRoomRoleRuntime.CanDescendCurrentFloor(secondMap)); // 선택 보스를 지나지 않고도 정상 계단 이용 가능 확인
            while (session.CurrentFloor < 10) // 최종 승리 보호 규칙의 대조 실험
            { // 판정용 최종 층으로 이동 시작
                session.AdvanceFloor(); // 다음 층의 기존 상태 초기화 호출
            } // 판정용 최종 층으로 이동 종료
            WinEncounter(session, "optional-final-guard", optional); // 선택 조우로 잘못 저장된 최종층 입력 재현
            Assert.IsFalse(session.IsExplorationCompleted); // 최종층에서도 선택 조우가 탐사 승리를 완료하지 않음 확인
        } // 실제 진행 검사 종료
        finally // 검사에서 추가한 지도 오브젝트 제거
        { // 검사 오브젝트 정리 시작
            foreach (GameObject root in roots) // 실제 지도 검사 객체 순회
            { // 지도 검사 객체 정리 시작
                UnityEngine.Object.Destroy(root); // 생성한 지도와 콘텐츠 제거
            } // 지도 검사 객체 정리 종료
        } // 검사 오브젝트 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    private static ExplorationMapRuntime BuildMap(ExplorationSessionManager session, List<GameObject> roots, int seed, bool restored) // 실제 생성과 방 콘텐츠 배치 연결
    { // 검사 지도 생성 시작
        Assert.IsNotNull(BuildFloor); // 실제 탐사 지도 생성 메서드 확인
        if (restored) // 저장된 실제 시드를 따라가는 경우 확인
        { // 복원 시드 검증 시작
            Assert.AreEqual(seed, session.CurrentFloorSeed); // 이미 저장된 시드로만 복원 확인
        } // 복원 시드 검증 종료
        else // 신규 지도에 재현 가능한 시드 사용
        { // 고정 시드 지정 시작
            session.SetCurrentFloorSeed(seed); // 검사 재현용 새 시드 지정
        } // 고정 시드 지정 종료
        var root = new GameObject("OptionalBossMap"); // 실서비스 맵과 격리된 검사 오브젝트
        roots.Add(root); // 테스트 종료용 지도 등록
        var map = root.AddComponent<ExplorationMapRuntime>(); // 실제 저장 지도 복원 초기화
        if (!restored) // 신규 층의 실제 최초 생성 경로 확인
        { // 고정 시드 지도 새로 생성 시작
            BuildFloor.Invoke(map, new object[] { seed, false }); // 유물 보유를 포함하여 신규 지도 생성
        } // 고정 시드 지도 새로 생성 종료
        root.AddComponent<ExplorationRoomRoleRuntime>().Initialize(map); // 실제 등급별 전투와 상점·관문 배치 시작
        return map; // 정상 탐사 표시와 상태가 반영된 지도 반환
    } // 검사 지도 생성 종료

    private static string EncounterId(ExplorationMapRuntime map, Vector2Int coordinate) // 기존 지도에서 사용하는 위치별 조우 식별자
    { // 조우 식별자 생성 시작
        return $"F{map.CurrentFloor}_X{coordinate.x}_Y{coordinate.y}_S{map.CurrentMap.Seed}"; // 실제 조우 표시와 저장에 공통인 식별자
    } // 조우 식별자 생성 종료

    private static EncounterData Encounter(ExplorationMapRuntime map, Vector2Int coordinate) // 실제 지도에 나타난 조우 원본 조회
    { // 지도 조우 조회 시작
        var marker = map.GetComponentsInChildren<ExplorationEncounterView>().SingleOrDefault(view => view.name == "Encounter_" + EncounterId(map, coordinate)); // 임시 배치를 제거한 활성 전투 표식 조회
        Assert.IsNotNull(marker); // 맵에서 확인 가능한 실제 조우 배치 확인
        return marker.Data; // 실제 전투로 전달되는 조우 원본 반환
    } // 지도 조우 조회 종료

    private static void AssertEquivalentOtherEncounters(ExplorationMapRuntime before, ExplorationMapRuntime after, string selectedName) // 원래 지도에서 선택 방 이외의 조우 유지 확인
    { // 일반 조우의 고정 난수 비교 시작
        var expected = before.GetComponentsInChildren<ExplorationEncounterView>().Where(view => view.name != selectedName).ToDictionary(view => view.name, view => view.Data); // 선택 방 외의 원래 원본 목록
        var actual = after.GetComponentsInChildren<ExplorationEncounterView>().Where(view => view.name != selectedName).ToDictionary(view => view.name, view => view.Data); // 선택 방 외의 변경 뒤 원본 목록
        CollectionAssert.AreEquivalent(expected, actual); // 기존 필수 관문과 다른 모든 조우 원본 비교
    } // 일반 조우의 고정 난수 비교 종료

    private static void WinEncounter(ExplorationSessionManager session, string id, EncounterData data) // 실제 조우 시작과 전투 승리 정산
    { // 전투 결과 정산 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 캐릭터 원본 조회
        var ally = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 실제 승리 결과에 기록할 생존 아군
        Assert.IsTrue(session.BeginEncounter(id, data, Vector3.zero, Vector3.up)); // 조우 접촉 뒤 일반 전투 진입과 같은 경로
        session.ResolveBattleResult(new BattleResultData(BattleResult.Victory, data.BattleType, 1, new[] { ally }, Array.Empty<string>())); // 실제 전투 승리로 보상과 지도 진행 적용
    } // 전투 결과 정산 종료

    private static Vector2Int? Select(ExplorationMapData map, int floor, int chance) // 실제 맵 추첨 호출
    { // 추첨 연결 시작
        Assert.IsNotNull(SelectRoom); // 선택 보스 추첨 구현 확인
        return (Vector2Int?)SelectRoom.Invoke(null, new object[] { map, floor, chance }); // 고정 맵과 층 및 확률 반환
    } // 추첨 연결 종료

    private static Vector2Int? GetFrozenRoom(ExplorationSessionManager session) // 탐사 층의 저장된 선택 위치 조회
    { // 상태 조회 시작
        Assert.IsNotNull(FrozenRoom); // 신규 층의 저장 위치 구현 확인
        return (Vector2Int?)FrozenRoom.GetValue(session); // 현재 층의 선택 결과 반환
    } // 상태 조회 종료

    private static ExplorationMapData BranchMap(int seed) // 진행 경로와 막다른 일반 방이 있는 검사 지도
    { // 지도 조립 시작
        var start = new ExplorationMapCell(Vector2Int.zero); // 출발 방 원점
        var passage = new ExplorationMapCell(Vector2Int.up); // 계단 직전 일반 통로 방
        var stairs = new ExplorationMapCell(Vector2Int.up * 2); // 일반 층의 필수 정예 관문
        var optional = new ExplorationMapCell(Vector2Int.right); // 보스가 될 수 있는 막다른 일반 전투 방
        var shop = new ExplorationMapCell(Vector2Int.left); // 보스가 될 수 없는 막다른 상점 방
        start.SetType(ExplorationCellType.Start); // 시작 지점 구조 지정
        start.SetConnections(true, true, false, true); // 통로와 두 막다른 방의 연결
        passage.SetConnections(true, false, true, false); // 시작과 계단 사이 통과 경로
        stairs.SetType(ExplorationCellType.Stairs); // 마지막 층 진행 지점 구조 지정
        stairs.SetRoomType(ExplorationRoomType.Elite); // 원래 층의 정예 방 지정
        stairs.SetConnections(false, false, true, false); // 계단에서 시작 쪽 연결
        optional.SetConnections(false, false, false, true); // 되돌아갈 한 갈래만 존재
        shop.SetRoomType(ExplorationRoomType.Shop); // 상점 역할 부여
        shop.SetConnections(false, true, false, false); // 상점에서 시작 쪽만 연결
        var cells = new List<ExplorationMapCell> { start, passage, stairs, optional, shop }; // 원래 생성과 같은 저장 순서
        return new ExplorationMapData(seed, cells, start.Coordinate, stairs.Coordinate); // 완성된 검사 지도 반환
    } // 지도 조립 종료

    private static ExplorationSessionManager PrepareParty() // 실제 저장 유효성에 맞는 회차 상태 준비
    { // 회차 조립 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 후보 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 정상 출전 조건 적용
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 출전자 선택
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 둘째 출전자 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 탐사 덱과 편성 생성
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 덱 연결
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 세션 조회
        session.ResetExploration(); // 독립 검사 진행 상태로 초기화
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 저장에 필요한 파티 지속 상태 등록
        return session; // 정상 상태의 탐사 회차 반환
    } // 회차 조립 종료
} // 선택 보스 검사 종료
