using System; // 한계 시간과 판정 함수 사용
using System.Collections; // 씬 진행 대기
using System.Linq; // 실제 방과 원본 선택
using NUnit.Framework; // 유물 결과 검증
using UnityEngine; // 실제 원본과 플레이어 사용
using UnityEngine.SceneManagement; // 탐사 씬 복원
using UnityEngine.TestTools; // 실제 플레이 검사
using Object = UnityEngine.Object; // 게임 객체 검색

public sealed class WinterfallRelicEffectsTests // 윈터폴 유물 등록과 발동 규칙 검사
{ // 유물 검사 시작
    private static RelicData Relic(string name) // 실제 유물 원본 조회
    { // 유물 조회 시작
        return Resources.Load<RelicData>("Relics/" + name); // 탐사와 상점이 공유하는 원본 반환
    } // 유물 조회 종료

    [Test] // 원본과 시트 및 상점 등록 검사
    public void RelicsHaveSheetPricesSavedIdsAndDistinctArtwork() // 저장과 상점 및 도감 원화 확인
    { // 등록 검사 시작
        string[] names = // 검사할 유물 자원 이름
        { // 원본 목록 시작
            "CrackedCompass", "BloodstainedSyringe", "WornDie", "SealedDagger", // 나침반부터 단도까지
            "FragmentedMirror", "ErosionEchoBottle", "EmergencyRationPack", "BlackSpool", // 거울부터 실타래까지
            "GoldenExchangeVoucher", "RedRemnantCharm", "FadedDogTag", "BrokenMedal" // 교환권부터 메달까지
        }; // 원본 목록 종료
        var relics = names.Select(Relic).ToArray(); // 실제 원본 목록 조회
        var shop = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점 원본
        var save = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 복원용 원본
        foreach (var relic in relics) // 유물별 등록 상태 검사
        { // 개별 등록 검사 시작
            Assert.IsNotNull(relic); // 원본 존재 확인
            Assert.IsNotNull(relic.Icon); // 실제 표시 그림 연결 확인
            Assert.AreSame(relic, save.Relics.Single(item => item.RelicId == relic.RelicId)); // 같은 저장 번호에 연결된 원본 하나 확인
            var offer = shop.Offers.Single(row => row.Relic == relic); // 유물마다 실제 상품 하나 확인
            var sheet = RelicSheetCatalog.Load().Entries.Single(row => row.Name == relic.DisplayName); // 실제 시트 이름 연결 확인
            Assert.That(offer.Price, Is.InRange(sheet.BuyMin, sheet.BuyMax)); // 기본 가격의 시트 구매 범위 확인
        } // 개별 등록 검사 종료
        Assert.AreEqual(names.Length, relics.Select(item => item.Icon).Distinct().Count()); // 유물별 고유 그림 확인
    } // 등록 검사 종료

    [Test] // 나침반 첫 방과 재추첨 방지 검사
    public void CompassChecksOnlyFirstNormalRoomAndPersistsHitOrMiss() // 출발과 실제 방문의 보유 상태 확인
    { // 방 판정 검사 시작
        var inventory = new RelicInventoryRuntime(new RelicGoldRuntime()); // 독립 보관함 준비
        var absent = CrackedCompassRunState.ForDeparture(inventory); // 나침반 없이 출발
        var compass = Relic("CrackedCompass"); // 실제 발견 확률 원본
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(compass)); // 출발 뒤 획득 확인
        int version = ExplorationEventCatalog.CurrentVersion; // 실제 사건 목록 사용
        var cell = new ExplorationMapCell(new Vector2Int(2, 3)); // 검사할 일반 방
        Assert.IsFalse(absent.Visit(1, 2, cell, version, inventory)); // 늦게 얻은 나침반의 소급 적용 방지
        var state = CrackedCompassRunState.ForDeparture(inventory); // 나침반을 보유한 출발
        cell.SetType(ExplorationCellType.Start); // 안전한 출발 방 설정
        Assert.IsFalse(state.Visit(1, 2, cell, version, inventory)); // 출발 방에서 기회 보존
        cell.SetType(ExplorationCellType.Normal); // 일반 구조 복구
        foreach (ExplorationRoomType type in Enum.GetValues(typeof(ExplorationRoomType))) // 전투와 특수 방의 모든 역할 확인
        { // 방 역할 검사 시작
            if (type != ExplorationRoomType.Normal) // 일반 전투 이외의 방 확인
            { // 특수 방 검사 시작
                cell.SetRoomType(type); // 현재 방 역할 적용
                Assert.IsFalse(state.Visit(1, 2, cell, version, inventory)); // 특수 방에서 기회 보존
            } // 특수 방 검사 종료
        } // 방 역할 검사 종료
        cell.SetRoomType(ExplorationRoomType.Normal); // 원래 일반 전투 복구
        Assert.IsFalse(state.Visit(2, 2, cell, version, inventory)); // 다음 층의 소급 판정 방지
        Assert.IsTrue(state.Visit(1, 2, cell, version, inventory)); // 성공 시드의 첫 방 판정
        var loaded = JsonUtility.FromJson<CrackedCompassRunState>(JsonUtility.ToJson(state)); // 발견 후 저장 내용 복원
        Assert.IsTrue(loaded.MatchesMap(1, 2)); // 원래 지도에 사건 유지
        Assert.IsFalse(loaded.MatchesMap(1, 4)); // 같은 층의 다른 지도에 잘못 배치되는 경우 방지
        Assert.AreEqual(cell.Coordinate, loaded.FoundRoom); // 방문한 방 위치 고정 확인
        Assert.IsTrue(loaded.IsValid(1, version)); // 실제 사건 원본 복원 가능 확인
        var next = new ExplorationMapCell(Vector2Int.left); // 두 번째 일반 방
        Assert.IsFalse(loaded.Visit(1, 4, next, version, inventory)); // 발견 뒤 두 번째 방 재판정 방지
        var miss = CrackedCompassRunState.ForDeparture(inventory); // 실패 시드로 출발
        Assert.IsTrue(miss.Visit(1, 1, cell, version, inventory)); // 실패한 첫 방문의 기회 소비
        Assert.IsNull(miss.EncounterId); // 실패한 일반 전투 유지
        Assert.IsFalse(JsonUtility.FromJson<CrackedCompassRunState>(JsonUtility.ToJson(miss)).Visit(1, 2, next, version, inventory)); // 실패 저장 복원 후 재추첨 방지
        var sold = CrackedCompassRunState.ForDeparture(inventory); // 판매 전에 보유한 나침반
        Assert.IsTrue(inventory.TryRemove(compass.RelicId)); // 방문 전 실제 판매 상태 반영
        Assert.IsTrue(sold.Visit(1, 2, cell, version, inventory)); // 보유 여부를 확인할 첫 방문
        Assert.IsNull(sold.EncounterId); // 판매한 나침반 효과 제외
        Assert.IsFalse(JsonUtility.FromJson<CrackedCompassRunState>("{}").Pending); // 이전 저장에 없는 기회 부여 방지
    } // 방 판정 검사 종료

    [Test] // 전투별 주사기와 턴별 단도 검사
    public void DaggerRequiresAlliedKillAndSyringeRequiresVictory() // 처치 원인과 승리 전용 회복 검사
    { // 전투 계산 검사 시작
        var allies = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters.Take(3).Select(member => BattleUnitRuntime.CreateAlly(member)).ToArray(); // 실제 아군 세 명 준비
        var enemy = BattleUnitRuntime.CreateEnemy(Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter").Enemies[0]); // 실제 적 원본 준비
        var points = new BattleActionPointRuntime(4); // 최대 행동력 네 점
        var wallet = new RelicGoldRuntime(); // 독립 지갑 준비
        var inventory = new RelicInventoryRuntime(wallet); // 보유 유물 준비
        using (var events = new BattleEventDispatcher()) // 실제 전투 발행기 준비
        using (var effects = new BattleRelicEffectController(events, inventory, wallet, allies, new[] { enemy }, null, points)) // 전투와 같은 유물 연결
        { // 발동 검사 시작
            allies[0].ApplyPersistentHealth(0); // 사망자를 맨 앞에 배치
            allies[1].ApplyPersistentHealth(5); // 첫 생존자 체력 설정
            allies[2].ApplyPersistentHealth(5); // 같은 체력인 다음 생존자 설정
            Assert.IsTrue(points.Spend(4)); // 처치 시 회복할 여유 준비
            events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 1, BattleTurnPhase.PlayerTurn, allies[1], enemy)); // 단도 없는 아군 처치
            Assert.AreEqual(0, points.CurrentActionPoints); // 미보유 행동력 회복 방지
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(Relic("SealedDagger"))); // 단도 획득
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(Relic("BloodstainedSyringe"))); // 주사기 획득
            events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 1, BattleTurnPhase.EnemyTurn, allies[1], enemy)); // 적 턴에서 발생한 처치
            events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 1, BattleTurnPhase.PlayerTurn, enemy, allies[1])); // 적이 아군을 쓰러뜨린 사건
            events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 1, BattleTurnPhase.PlayerTurn, allies[1], allies[1])); // 아군의 자해
            events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 1, BattleTurnPhase.PlayerTurn, enemy, enemy)); // 적의 자해
            events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 1, BattleTurnPhase.PlayerTurn, null, enemy)); // 공격자 없는 환경 피해
            Assert.AreEqual(0, points.CurrentActionPoints); // 무관한 사망의 발동과 횟수 소모 제외
            for (int index = 0; index < 2; index++) // 같은 턴 두 처치 전달
            { // 같은 턴 처치 검사 시작
                events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 1, BattleTurnPhase.PlayerTurn, allies[1], enemy)); // 정상 아군 처치 신호
            } // 같은 턴 처치 검사 종료
            Assert.AreEqual(1, points.CurrentActionPoints); // 한 턴에 한 번만 회복
            events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 2, BattleTurnPhase.PlayerTurn)); // 다음 아군 턴 진입
            events.Publish(new BattleEventContext(BattleEventType.UnitDefeated, 2, BattleTurnPhase.PlayerTurn, allies[2], enemy)); // 다음 턴의 아군 처치
            Assert.AreEqual(2, points.CurrentActionPoints); // 다음 아군 턴 재발동 확인
            Assert.AreEqual(2, points.Gain(10)); // 최대 행동력까지 실제 회복량 확인
            Assert.AreEqual(0, points.Gain(1)); // 행동력 상한 초과 방지
            events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 2, BattleTurnPhase.Escaped, result: BattleResult.Escape)); // 도주 종료 전달
            events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 2, BattleTurnPhase.Defeat, result: BattleResult.Defeat)); // 패배 종료 전달
            Assert.AreEqual(5, allies[1].CurrentHealth); // 도주와 패배의 회복 제외
            events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 2, BattleTurnPhase.Victory, result: BattleResult.Victory)); // 첫 승리 전달
            events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 2, BattleTurnPhase.Victory, result: BattleResult.Victory)); // 같은 결과 재전달
            Assert.AreEqual(0, allies[0].CurrentHealth); // 사망자의 부활 방지
            Assert.AreEqual(8, allies[1].CurrentHealth); // 편성 순서의 앞선 생존자 체력 세 점 회복
            Assert.AreEqual(5, allies[2].CurrentHealth); // 중복 승리 신호의 추가 회복 방지
        } // 발동 검사 종료
    } // 전투 계산 검사 종료

    [UnityTest] // 실제 출발과 방 발견 및 승리 결과 검사
    public IEnumerator OwnedRelicsSurviveSaveAndDriveRealMapAndBattle() // 보관함과 실제 탐사 및 전투 검증
    { // 실게임 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 사용자 기본 자동 저장 보호
        { // 사용자 프로필 제외 시작
            Assert.Ignore("자동 저장을 격리한 ProjectCVerification 복제본 검사"); // 별도 저장 계정만 실행
        } // 사용자 프로필 제외 종료
        yield return new EnterPlayMode(); // 실제 게임 시작
        yield return CheckMapAndVictory(); // 플레이 모드의 실제 상태 검사
        yield return new ExitPlayMode(); // 실제 게임 종료
    } // 실게임 검사 종료

    private static IEnumerator CheckMapAndVictory() // 실제 유물 보유 상태로 탐사 출발
    { // 탐사 통합 검사 시작
        SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 초기화
        yield return WaitUntil(() => Object.FindFirstObjectByType<MainMenuController>() != null && !SceneFlowManager.Instance.IsLoadingScene, "메인 메뉴 준비"); // 씬 전환 완료 대기
        var manager = RelicRunManager.EnsureInstance(); // 실제 보관함 준비
        var names = new[] { "CrackedCompass", "BloodstainedSyringe", "WornDie", "SealedDagger" }; // 실제 보관과 판매 원본
        int draws = 0; // 카드의 실제 난수 호출 수
        Func<int, int> draw = count => draws++ % count; // 첫째와 둘째 결과가 다른 추첨
        Assert.AreEqual(0, RelicRandomCardTarget.PickIndex(3, draw)); // 주사위 없는 카드의 첫 결과
        Assert.AreEqual(1, draws); // 미보유 시 한 번만 추첨
        foreach (string name in names) // 출발 전에 네 원본 획득
        { // 보관함 등록 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(Relic(name))); // 실제 보관함 획득 확인
        } // 보관함 등록 종료
        draws = 0; // 보유 시 호출 수 초기화
        Assert.AreEqual(1, RelicRandomCardTarget.PickIndex(3, draw)); // 같은 후보에서 두 번째 결과 채택
        Assert.AreEqual(2, draws); // 정확히 한 번 재추첨 확인
        Assert.AreEqual(0, RelicRandomCardTarget.PickIndex(3, count => 0)); // 같은 대상도 다시 선택 가능
        Assert.AreEqual(0, RelicRandomCardTarget.PickIndex(1, draw)); // 유일한 후보 유지
        Assert.AreEqual(3, draws); // 단일 후보의 불필요한 재추첨 방지
        CheckSunTargetRoll(); // 보유 주사위가 실제 태양 카드의 두 번째 적에 적용되는지 확인
        PartyDepartureTestSupport.StartExploration(0, 1); // 실제 출발용 파티와 탐사 저장 구성
        var session = ExplorationSessionManager.Instance; // 현재 회차 상태 조회
        session.SetCurrentFloorSeed(2); // 첫 일반 방 발견을 재현할 시드
        yield return WaitUntil(() => Object.FindFirstObjectByType<ExplorationRoomRoleRuntime>() != null && Object.FindFirstObjectByType<ExplorationPlayerController>() != null && !SceneFlowManager.Instance.IsLoadingScene, "탐사 방과 플레이어 준비"); // 최종 방 배치와 위치 대기
        var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 실제 지도 조회
        var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 물리 플레이어 조회
        var room = map.CurrentMap.Cells.First(cell => cell.Type == ExplorationCellType.Normal && cell.RoomType == ExplorationRoomType.Normal && map.HasEncounterAt(cell.Coordinate)); // 실제 처음 방문할 일반 조우
        string id = $"F1_X{room.Coordinate.x}_Y{room.Coordinate.y}_S2"; // 첫 일반 조우 식별자
        var original = Object.FindObjectsByType<ExplorationEventView>(FindObjectsSortMode.None).ToDictionary(view => view.RuntimeEventId, view => view.EventData.EventId); // 원래 지도 전체 사건 기록
        Assert.IsTrue(session.Compass.Pending); // 시작 방에서 기회 보존 확인
        player.Teleport(GameObject.Find("Encounter_" + id).transform.position); // 실제 적 접촉으로 첫 일반 방 진입
        yield return WaitUntil(() => session.Compass.EncounterId == id && !map.HasEncounterAt(room.Coordinate), "일반 전투의 사건 교체"); // 물리 판정과 표식 제거 대기
        string found = session.Compass.EventId; // 최초 선택한 사건 원본
        Assert.AreEqual(found, Object.FindObjectsByType<ExplorationEventView>(FindObjectsSortMode.None).Single(view => view.RuntimeEventId == "EV_" + id).EventData.EventId); // 실제 사건 배치 확인
        Assert.IsEmpty(session.ClearedEncounterIds); // 바뀐 방 전투 승리와 보상 제외
        Assert.IsTrue(ExplorationSaveService.CanLoad(ExplorationSaveService.DefaultPath)); // 첫 방 자동 저장 확인
        foreach (string name in names) // 복원 전 보관함 비우기
        { // 유물 제거 시작
            Assert.IsTrue(manager.TryRemove(Relic(name).RelicId)); // 실제 보관함 제거 확인
        } // 유물 제거 종료
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 자동 저장에서 네 보유 원본과 사건 복원
        var reacquiredOrder = new[] { Relic("CrackedCompass"), Relic("BloodstainedSyringe"), Relic("SealedDagger"), Relic("WornDie") }; // 뒤에서 다시 획득한 주사위의 저장 순서
        CollectionAssert.AreEqual(reacquiredOrder, manager.Inventory.OwnedRelics); // 판매와 재획득까지 반영된 순서 복원
        Assert.AreEqual(found, session.Compass.EventId); // 불러오기 뒤 최초 결과 유지
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 저장 화면과 같은 경로로 실제 탐사 재생성
        Assert.IsTrue(SceneFlowManager.Instance.IsLoadingScene); // 이전 지도를 검사하기 전 씬 이동 접수 확인
        yield return WaitUntil(() => !SceneFlowManager.Instance.IsLoadingScene && map == null && Object.FindFirstObjectByType<ExplorationRoomRoleRuntime>() != null && Object.FindFirstObjectByType<ExplorationPlayerController>() != null, "새 탐사 씬의 저장 지도 재구성"); // 이전 지도가 제거되고 새 배치가 끝날 때까지 대기
        var restored = Object.FindObjectsByType<ExplorationEventView>(FindObjectsSortMode.None).ToDictionary(view => view.RuntimeEventId, view => view.EventData.EventId); // 복원된 전체 사건 목록
        original.Add("EV_" + id, found); // 원래 지도에 나침반 사건 한 개만 추가
        CollectionAssert.AreEquivalent(original, restored); // 다른 모든 사건 원본과 위치 보존
        Assert.IsFalse(session.Compass.Pending); // 재입장 시 다른 방 재추첨 방지
        map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 불러온 지도의 실제 객체 조회
        var nextCell = map.CurrentMap.Cells.First(cell => cell.RoomType == ExplorationRoomType.Normal && map.HasEncounterAt(cell.Coordinate)); // 살아 있는 일반 전투 좌표 확인
        string nextId = $"F1_X{nextCell.Coordinate.x}_Y{nextCell.Coordinate.y}_S2"; // 두 번째 일반 조우의 고유 식별자
        var next = GameObject.Find("Encounter_" + nextId).GetComponent<ExplorationEncounterView>(); // 새 지도에 남아 있는 실제 적 표식
        Assert.IsTrue(session.CanBeginEncounter(nextId, next.Data)); // 복원한 다른 방의 전투 진입 허용 확인
        player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 복원된 플레이어 조회
        ExplorationEventPanelView.CloseForLoad(); // 복원 위치에 열렸을 수 있는 사건 닫기
        player.Teleport(next.transform.position); // 두 번째 일반 방에서 실제 적 접촉
        Physics2D.SyncTransforms(); // 새 지도의 충돌체와 플레이어 위치 동기화
        Assert.Greater(Time.timeScale, 0f); // 새 탐사의 실제 물리 진행 확인
        yield return WaitUntil(() => Object.FindFirstObjectByType<BattleSceneSetup>()?.IsInitialized == true && !SceneFlowManager.Instance.IsLoadingScene, "일반 조우의 실제 전투"); // 실제 전투 시작 대기
        var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 정상 전투 관리자 조회
        yield return WaitUntil(() => battle.BattleTurn.CurrentPhase == BattleTurnPhase.PlayerTurn, "아군 턴 진입"); // 처치 유물의 발동 턴 확인
        battle.AllyUnits[0].ApplyPersistentHealth(4); // 가장 낮은 생존 체력 설정
        Assert.IsTrue(battle.SharedActionPoints.Spend(battle.SharedActionPoints.CurrentActionPoints)); // 실제 공유 행동력 전부 사용
        foreach (var target in battle.EnemyUnits) // 실제 적 유닛 처치
        { // 전투 피해 적용 시작
            target.TakeDamage(9999, BattleDamageType.Physical, battle.AllyUnits[0]); // 아군 발생자를 가진 정상 피해와 승리
        } // 전투 피해 적용 종료
        Assert.AreEqual(1, battle.SharedActionPoints.CurrentActionPoints); // 실제 전투 최초 처치만 행동력 회복
        Assert.IsTrue(BattleResultManager.Instance.TryConsumeResult(out BattleResultData result)); // 탐사 귀환용 실제 결과 조회
        Assert.AreEqual(7, result.AllyStates[0].CurrentHealth); // 승리 전용 세 점 회복이 귀환 체력에 기록
        Assert.IsFalse(BattleResultManager.Instance.TryConsumeResult(out _)); // 같은 전투 결과 중복 수령 차단
    } // 탐사 통합 검사 종료

    private static void CheckSunTargetRoll() // 실제 태양 카드의 첫째와 둘째 추첨 구별
    { // 카드 대상 검사 시작
        var owner = BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters.Single(member => member.CharacterId == "ARC_19")); // 실제 태양 카드 소유자
        var card = CardInstance.Create(Resources.Load<CardData>("StarterCards/SunChain"), owner, 0); // 실제 연쇄 공격 인스턴스
        var data = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter").Enemies[0]; // 검사용 적 원본
        var enemies = Enumerable.Range(0, 4).Select(index => BattleUnitRuntime.CreateEnemy(data)).ToArray(); // 첫 대상과 세 추가 후보
        var savedRandom = UnityEngine.Random.state; // 다른 전투에 사용할 난수 보존
        try // 게임 난수 복원 보장
        { // 실제 카드 추첨 시작
            for (int seed = 0; seed < 32; seed++) // 두 추첨을 구별할 유한 시드 탐색
            { // 같은 후보 추첨 시작
                UnityEngine.Random.InitState(seed); // 독립 카드 시험 시드
                int first = UnityEngine.Random.Range(0, 3); // 첫 추첨의 추가 대상 번호
                int second = UnityEngine.Random.Range(0, 3); // 주사위의 두 번째 추가 대상 번호
                if (first != second) // 실제 원본의 두 후보가 다른 경우
                { // 유물 보유와 판매 비교 시작
                    UnityEngine.Random.InitState(seed); // 실제 카드의 두 번 추첨 재현
                    Assert.AreSame(enemies[second + 1], SunChainAttack.TryAttack(card, enemies[0], enemies)); // 주사위 보유 시 두 번째 대상 공격
                    var manager = RelicRunManager.Instance; // 실제 회차 보관함 조회
                    Assert.IsTrue(manager.TryRemove(Relic("WornDie").RelicId)); // 주사위 판매 상태 재현
                    UnityEngine.Random.InitState(seed); // 같은 카드의 미보유 시드 재현
                    Assert.AreSame(enemies[first + 1], SunChainAttack.TryAttack(card, enemies[0], enemies)); // 주사위 제거 시 원래 대상 공격
                    Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(Relic("WornDie"))); // 다음 탐사를 위한 주사위 재획득
                    return; // 보유와 판매 두 실제 카드 경로 확인 완료
                } // 유물 보유와 판매 비교 종료
            } // 같은 후보 추첨 종료
            Assert.Fail("태양 카드의 서로 다른 두 추첨을 재현할 시드 필요"); // 구분 가능한 시드 누락 진단
        } // 실제 카드 추첨 종료
        finally // 검사 뒤의 난수 복원
        { // 난수 정리 시작
            UnityEngine.Random.state = savedRandom; // 다른 전투와 탐사의 난수 보존
        } // 난수 정리 종료
    } // 카드 대상 검사 종료

    private static IEnumerator WaitUntil(Func<bool> ready, string step) // 실제 씬과 접촉 대기
    { // 상태 대기 시작
        float deadline = Time.realtimeSinceStartup + 25f; // 씬 초기화 제한 시각
        while (!ready() && Time.realtimeSinceStartup < deadline) // 상태와 제한 시각 확인
        { // 다음 프레임 대기 시작
            yield return null; // 씬과 물리 처리 진행
        } // 다음 프레임 대기 종료
        Assert.IsTrue(ready(), step); // 미도달 상태 표시
        yield return null; // 시작 함수와 지연 처리 마무리
    } // 상태 대기 종료
} // 유물 검사 종료
