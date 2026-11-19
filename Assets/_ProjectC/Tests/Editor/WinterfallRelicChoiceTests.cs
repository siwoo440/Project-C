using System.Collections; // 실제 손패 입력과 전투 연출 대기
using System.Linq; // 게임에서 사용 중인 실제 원본 검색
using NUnit.Framework; // 보상과 전투 효과 검사
using UnityEngine; // 실제 전투 유닛과 오브젝트 조회
using UnityEngine.TestTools; // 플레이 모드 실행
using UnityEngine.UI; // 확인 카드의 실제 버튼 입력
using Fight = WinterfallSwordCombatTests.Fight; // 실제 손패와 턴이 연결된 기존 전투 검사 환경

public sealed class WinterfallRelicChoiceTests // 시트 이십오 행 이후 유물의 실제 게임 동작
{ // 유물 검사 시작
    [Test] // 실제 상점과 저장 및 시트 확인
    public void EightRelicsHaveDistinctArtAndMatchingOfficialDescriptions() // 여덟 유물의 설명과 소유 원본 대조
    { // 원본 대조 시작
        var names = new[] { "WornCoinPurse", "RiftCollectorsTweezers", "GraphitePencil", "ResolveRibbon", "HeavyIronBoots", "WhiteRoseCorsage", "BlackRoseCorsage", "SealedMap" }; // 시트 행 순서의 여덟 원본
        var relics = names.Select(name => Resources.Load<RelicData>("Relics/" + name)).ToArray(); // 탐사에서 사용하는 실물 유물
        var shop = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점과 보물 원본
        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 소유 저장 원본
        foreach (var relic in relics) // 전체 여덟 원본 대조
        { // 원본 확인 시작
            Assert.IsNotNull(relic); // 원본 등록
            Assert.IsNotNull(relic.Icon); // 실제 원화 연결
            Assert.AreEqual(1, catalog.Relics.Count(item => item == relic)); // 저장 원본에 하나만 등록
            var row = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 원본 이름과 시트 행 대조
            Assert.AreEqual(relic.Description, row.Description); // 상점과 보유 목록의 동일한 확정 설명
            Assert.That(shop.Offers.Single(offer => offer.Relic == relic).Price, Is.InRange(row.BuyMin, row.BuyMax)); // 실제 등록 가격 범위
        } // 원본 확인 종료
        Assert.AreEqual(8, relics.Select(relic => relic.Icon).Distinct().Count()); // 여덟 가지 독립 삽화
    } // 원본 대조 종료
    [Test] // 현재 결정 원본이 없는 유물의 전용 성공 이벤트 검사
    public void RibbonNeedsSuccessfulDecisionEffectAndOnlyGrantsOnce() // 단순 선택이나 사용과 결정 성공 구별
    { // 결정 조건 검사 시작
        var member = BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]); // 카드 소유 아군
        var card = CardInstance.Create(Resources.Load<CardData>("StarterCards/FoolChoice"), member, 1, 0); // 소유자와 선택 카드 참조
        var points = new BattleActionPointRuntime(5); // 실제 공용 행동력 지갑
        var wallet = new RelicGoldRuntime(); // 독립 유물 지갑
        var inventory = new RelicInventoryRuntime(wallet); // 독립 보관함
        Assert.IsTrue(points.Spend(4)); // 행동력 회복을 관찰할 빈자리
        using (var events = new BattleEventDispatcher()) // 실제 전투 이벤트 발행기
        using (var effects = new BattleRelicEffectController(events, inventory, wallet, new[] { member }, new BattleUnitRuntime[0], null, points)) // 전투와 같은 유물 트리거
        { // 유물 소유와 전용 신호 비교 시작
            events.Publish(new BattleEventContext(BattleEventType.DecisionResolved, 1, BattleTurnPhase.PlayerTurn, member, card: card)); // 미보유 상태의 결정 성공 재현
            Assert.AreEqual(1, points.CurrentActionPoints); // 미보유 상태 유지
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(Resources.Load<RelicData>("Relics/ResolveRibbon"))); // 실제 리본 획득
            events.Publish(new BattleEventContext(BattleEventType.CardUsed, 1, BattleTurnPhase.PlayerTurn, member, card: card)); // 바보의 선택 일반 사용 신호
            events.Publish(new BattleEventContext(BattleEventType.DecisionResolved, 1, BattleTurnPhase.EnemyTurn, member, card: card)); // 아군 행동이 아닌 잘못된 성공 신호
            Assert.AreEqual(1, points.CurrentActionPoints); // 별도 추가 효과 이전 행동력 보존
            events.Publish(new BattleEventContext(BattleEventType.DecisionResolved, 1, BattleTurnPhase.PlayerTurn, member, card: card)); // 향후 결정 추가 효과의 실제 성공 신호 재현
            events.Publish(new BattleEventContext(BattleEventType.DecisionResolved, 1, BattleTurnPhase.PlayerTurn, member, card: card)); // 같은 턴 두 번째 성공 재현
            Assert.AreEqual(2, points.CurrentActionPoints); // 파티 공용 행동력 한 번만 증가
            events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 2, BattleTurnPhase.PlayerTurn)); // 다음 아군 턴 시작
            events.Publish(new BattleEventContext(BattleEventType.DecisionResolved, 2, BattleTurnPhase.PlayerTurn, member, card: card)); // 새 턴 결정 성공 재현
            Assert.AreEqual(3, points.CurrentActionPoints); // 새 아군 턴 한 번 증가
        } // 유물 소유와 전용 신호 비교 종료
    } // 결정 조건 검사 종료
    [UnityTest] // 고정 후보와 조사 비용 및 복원 검사
    public IEnumerator TweezersGiveExactlyOneOfTwoItemsAndCancelWithoutPaying() // 실제 이벤트에서 취소와 보상 선택 확인
    { // 이벤트 검사 시작
        yield return new EnterPlayMode(); // 실제 이벤트 화면 시작
        var session = AshPileEventTests.PrepareParty(); // 저장 가능한 탐사 파티와 덱
        var tweezers = Acquire("RiftCollectorsTweezers"); // 실제 핀셋 획득
        var resources = PlayerResourceManager.EnsureInstance(); // 실제 철판 보관함
        resources.AddResources(0, 0, 1, 0); // 조사에 쓸 철판 한 개
        int iron = resources.IronPlate; // 조사 전 철판 수
        string id = Enumerable.Range(0, 1000).Select(index => "tweezers-" + index).First(room => AshPileEvent.GetRewardChoices(room, AshPileEvent.UseTool, out _).Count == 2); // 추가 후보가 발생하는 고정 방
        var options = AshPileEvent.GetRewardChoices(id, AshPileEvent.UseTool, out bool found); // 두 후보의 원본 보관
        Assert.IsTrue(found); // 기존 조사 성공 뒤에만 추가 선택
        Assert.AreNotSame(options[0], options[1]); // 서로 다른 실제 미보유 원본
        Assert.AreEqual(30, tweezers.TriggerChancePercent); // 확정된 추가 후보 확률
        Assert.IsFalse(AshPileEvent.TryResolve(id, AshPileEvent.UseTool, null, out _, out _)); // 선택 전 지급과 비용 차단
        Assert.IsFalse(AshPileEvent.TryResolve(id, AshPileEvent.UseTool, null, tweezers, out _, out _)); // 후보 외 보유 유물 차단
        var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == AshPileEvent.Id); // 실제 잿더미 원본
        var marker = new GameObject("TweezersAshPile").AddComponent<ExplorationEventView>(); // 검사할 이벤트 표식
        marker.Initialize(data, id); // 실제 이벤트와 방 연결
        var panel = ExplorationEventPanelView.EnsureInstance(); // 일반 이벤트 패널
        panel.ShowEvent(marker, data, id); // 조사 원문 표시
        panel.SendMessage("HandleChoiceSelected", (object)AshPileEvent.UseTool); // 도구 선택으로 두 후보 표시
        panel.GetComponentsInChildren<Button>(true).Single(button => button.GetComponentInChildren<TMPro.TMP_Text>(true)?.text == "[선택 취소]").onClick.Invoke(); // 배치 검사의 블러 대기 중에도 실제 후보 취소 버튼 입력
        Assert.IsFalse(session.IsEventResolved(id)); // 취소한 방 미완료 확인
        Assert.AreEqual(iron, resources.IronPlate); // 선택 전 철판 미지출
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N")); // 독립 저장 파일
        try // 임시 저장 정리 보장
        { // 저장과 재선택 검사 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 후보 취소 후 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 미확정 조사 다시 불러오기
            CollectionAssert.AreEqual(options, AshPileEvent.GetRewardChoices(id, AshPileEvent.UseTool, out found)); // 다시 연 동일한 두 선택지
            Assert.IsTrue(AshPileEvent.TryResolve(id, AshPileEvent.UseTool, null, options[1], out _, out _)); // 두 번째 원본으로 확정
            Assert.IsTrue(session.IsEventResolved(id)); // 실제 처리 방 완료
            Assert.AreEqual(iron - 1, resources.IronPlate); // 단 한 번 철판 지불
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(options[1].RelicId)); // 고른 유물 획득
            Assert.IsFalse(RelicRunManager.Instance.Inventory.ContainsRelic(options[0].RelicId)); // 고르지 않은 후보 보존
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 고른 유물과 철판 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 거래 완료 복원
            Assert.IsFalse(AshPileEvent.TryResolve(id, AshPileEvent.UseTool, null, options[0], out _, out _)); // 완료 방 재지급 차단
        } // 저장과 재선택 검사 종료
        finally // 검사 저장 정리
        { // 정리 시작
            ExplorationSaveService.Remove(path); // 독립 검사 저장과 복구본 제거
        } // 정리 종료
        yield return new ExitPlayMode(); // 실제 이벤트 화면 종료
    } // 이벤트 검사 종료
    [UnityTest] // 실제 출발 방 귀환과 소모 및 재획득 검사
    public IEnumerator SealedMapUsesOneReturnPerExpeditionAndPersistsTheDestination() // 실제 탐사 유물 버튼과 저장 위치 확인
    { // 지도 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 기본 자동 저장 전용 검사 프로필 확인
        { // 사용자 저장 보호 시작
            Assert.Ignore("격리된 탐사 자동 저장 경로에서 실행"); // 일반 사용자 저장 보호
        } // 사용자 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 씬 구동
        yield return CheckMap(); // 플레이 모드 재시작 후 실제 지도 검증
        yield return new ExitPlayMode(); // 탐사 화면 종료
    } // 지도 검사 종료
    private static IEnumerator CheckMap() // 실제 층 이동과 유물 목록 검사
    { // 지도 이동 검사 시작
        var session = AshPileEventTests.PrepareParty(); // 새 탐사 파티와 층 생성
        if (SceneFlowManager.Instance == null) // 일반 씬 전환기 확인
        { // 씬 전환기 준비 시작
            new GameObject("SealedMapFlow").AddComponent<SceneFlowManager>(); // 탐사 씬 일반 진입 경로 준비
        } // 씬 전환기 준비 종료
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 실제 탐사 씬 불러오기
        float limit = Time.realtimeSinceStartup + 15f; // 실제 씬 로딩 대기 마감
        while ((Object.FindFirstObjectByType<ExplorationMapRuntime>()?.CurrentMap == null || Object.FindFirstObjectByType<ExplorationRelicWindow>() == null || SceneFlowManager.Instance.IsLoadingScene) && Time.realtimeSinceStartup < limit) // 실제 방과 유물 화면 생성 대기
        { // 탐사 대기 시작
            yield return null; // 탐사 씬 프레임 진행
        } // 탐사 대기 종료
        var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 실제 생성한 층
        var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 이동 캐릭터
        var window = Object.FindFirstObjectByType<ExplorationRelicWindow>(); // 실제 유물 목록
        Assert.IsNotNull(map?.CurrentMap); // 출발 방이 있는 실제 맵 확인
        var layout = map.CurrentMap; // 동일한 맵 유지 여부 비교
        var start = map.GetWorldPosition(layout.StartCoordinate); // 출발 방 중심 위치
        var elsewhere = map.GetWorldPosition(layout.Cells.First(cell => cell.Coordinate != layout.StartCoordinate).Coordinate); // 같은 층의 다른 방 중심
        var relic = Acquire("SealedMap"); // 귀환에 사용할 지도 획득
        player.Teleport(start); // 출발 방에서 무의미한 소모 확인
        Assert.IsFalse(session.TryUseSealedMap(map)); // 이미 출발 방이면 비용 없이 실패
        session.MarkEventResolved("previous-relic-room"); // 이미 처리한 방 기록
        int gold = PlayerResourceManager.EnsureInstance().Gold; // 현재 지갑 기록
        player.Teleport(elsewhere); // 실제 탐사 플레이어를 다른 방으로 이동
        Assert.IsTrue(session.CanUseSealedMap(map)); // 다른 방에서 귀환 가능
        Assert.IsFalse(session.TryUseSealedMap(map, "")); // 저장 실패 시 귀환 미확정
        Assert.IsFalse(session.UsedSealedMap); // 저장 실패 시 사용 기회 보존
        Assert.AreEqual(elsewhere, (Vector2)player.transform.position); // 실패한 이동의 기존 위치 보존
        Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(relic.RelicId)); // 저장 실패 시 원본 보유 유지
        window.transform.Find("RelicButton").GetComponent<Button>().onClick.Invoke(); // 실제 보유 유물 창 열기
        var mapSlot = window.GetComponentsInChildren<Button>().First(button => button.name == "RelicSlot" && button.transform.Find("Icon").GetComponent<Image>().sprite == relic.Icon); // 원본 아이콘의 지도 칸 조회
        mapSlot.onClick.Invoke(); // 실제 지도 상세 선택
        var use = window.GetComponentsInChildren<Button>().Single(button => button.name == "UseSealedMap"); // 출발 방 귀환 버튼
        Assert.IsTrue(use.interactable); // 정상 탐사 위치에서 버튼 활성화
        use.onClick.Invoke(); // 실사용 경로로 지도 소비
        Assert.AreEqual(start, (Vector2)player.transform.position); // 같은 층의 출발 방 중심 도착
        Assert.AreSame(layout, map.CurrentMap); // 층 재생성 방지
        Assert.IsTrue(session.IsEventResolved("previous-relic-room")); // 이전 방과 보상 기록 보존
        Assert.AreEqual(gold, PlayerResourceManager.Instance.Gold); // 귀환으로 추가 보상 없음
        Assert.IsFalse(RelicRunManager.Instance.Inventory.ContainsRelic(relic.RelicId)); // 귀환 성공 후 지도 소모
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 귀환 직후 자동 저장 복원
        Assert.IsTrue(session.UsedSealedMap && session.IsEventResolved("previous-relic-room")); // 소모와 기존 방 상태 함께 복원
        Acquire("SealedMap"); // 같은 탐사에서 지도 재획득
        player.Teleport(elsewhere); // 다시 다른 방으로 이동
        Assert.IsFalse(session.TryUseSealedMap(map)); // 같은 탐사의 두 번째 귀환 차단
        Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(relic.RelicId)); // 재사용 불가 지도의 추가 소모 방지
        session.AdvanceFloor(); // 다음 층으로 이동
        Assert.IsTrue(session.UsedSealedMap); // 새 층에서도 탐사당 제한 유지
        session.ResetExploration(); // 완전히 새 탐사 시작
        session.SetCurrentFloorSeed(layout.Seed); // 같은 층 구조로 새 탐사 비교
        Assert.IsTrue(session.CanUseSealedMap(map)); // 새 탐사에서는 보유 지도의 기회 복원
    } // 지도 이동 검사 종료
    private static RelicData Acquire(string asset) // 검증 회차에 등록한 실물 유물 획득
    { // 실제 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + asset); // 저장과 상점에 등록한 실제 원본
        Assert.IsNotNull(relic, asset); // 유물 원본 존재 확인
        var inventory = RelicRunManager.EnsureInstance().Inventory; // 현재 회차 보유 유물 목록
        inventory.TryRemove(relic.RelicId); // 앞선 획득 기록 없는 독립 조건
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 정상 보관함 획득 처리
        return relic; // 지급된 원본 반환
    } // 실제 획득 종료
    [UnityTest] // 전투 종료 후 실제 지갑과 회차 합계 확인
    public IEnumerator CoinPurseOnlyIncreasesActualVictoryGoldOnce() // 승리 기본 골드에만 십 퍼센트 적용
    { // 승리 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실사용자 자원 저장 보호
        { // 검증 사본 확인 시작
            Assert.Ignore("플레이 저장 경로가 격리된 검증 사본에서 실행"); // 실제 게임의 골드 보존
        } // 검증 사본 확인 종료
        yield return new EnterPlayMode(); // 실제 게임 관리자 사용 시작
        var session = ExplorationSessionManager.EnsureInstance(); // 실제 탐사 결과 정산 경로
        session.ResetExploration(); // 독립된 신규 탐사 시작
        var purse = Acquire("WornCoinPurse"); // 실제 동전지갑 획득
        var encounter = Object.Instantiate(Resources.Load<EncounterData>("Encounters/Encounter_WF_AshWanderer")); // 실제 일반 조우의 복사본
        JsonUtility.FromJsonOverwrite("{\"goldReward\":57}", encounter); // 소수점 절사를 확인할 기본 골드
        var wallet = PlayerResourceManager.EnsureInstance(); // 실제 골드 지갑
        int original = wallet.Gold; // 승리 전 골드
        var allies = new[] { BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]) }; // 전투 승리의 생존 파티
        var win = new BattleResultData(BattleResult.Victory, BattleType.Normal, 1, allies, new string[0]); // 실제 전투 결과
        Assert.IsTrue(session.BeginEncounter("purse-one", encounter, Vector2.zero, Vector2.zero)); // 첫 일반 조우 시작
        session.ResolveBattleResult(win); // 실제 클리어 보상 지급
        session.ResolveBattleResult(win); // 같은 결과 재전달
        Assert.AreEqual(original + 62, wallet.Gold); // 오십칠의 십 퍼센트 내림 다섯만 한 번 지급
        Assert.AreEqual(62, session.RunGoldGained); // 실제 입금과 같은 탐사 골드 합계
        Assert.AreEqual(62, session.LastClearReward.Gold); // 복귀 화면에 같은 보상 골드 기록
        Assert.IsTrue(RelicRunManager.Instance.Inventory.TryRemove(purse.RelicId)); // 소유하지 않은 비교 준비
        Assert.IsTrue(session.BeginEncounter("purse-two", encounter, Vector2.zero, Vector2.zero)); // 별개의 일반 조우 시작
        session.ResolveBattleResult(win); // 미보유 상태의 실제 승리
        Assert.AreEqual(original + 119, wallet.Gold); // 기본 오십칠만 추가 지급
        Assert.IsTrue(session.BeginEncounter("purse-escape", encounter, Vector2.zero, Vector2.zero)); // 도주 결과 확인용 조우
        session.ResolveBattleResult(new BattleResultData(BattleResult.Escape, BattleType.Normal, 1, allies, new string[0])); // 정상 도주 정산
        Assert.AreEqual(original + 119, wallet.Gold); // 도주 시 승리 보상 미지급
        Object.Destroy(encounter); // 임시 조우 원본 정리
        yield return new ExitPlayMode(); // 실제 게임 관리자 사용 종료
    } // 승리 검사 종료

    [UnityTest] // 장화와 군번줄 및 방어도 공존 검사
    public IEnumerator BootsReduceThreeRealEnemyHitsAcrossThePartyAndResetWithTurn() // 실제 적 공격 순서와 파티 공용 횟수 확인
    { // 장화 실전 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 턴과 보관함 생성
        yield return CheckBoots(); // 실제 적 예고 처리와 비용 확인
        yield return new ExitPlayMode(); // 전투 검사 종료
    } // 장화 실전 검사 종료
    private static IEnumerator CheckBoots() // 실제 적 예고 실행 및 턴 전환
    { // 장화 검사 시작
        using (var fight = new Fight("THRUST")) // 생존 적 네 명과 실제 전투 턴
        using (var actions = new BattleEnemyActionRuntime(fight.Enemies, fight.Allies, fight.Turns)) // 게임과 동일한 공유 횟수의 적 행동 관리자
        { // 전투 진행 시작
            Acquire("HeavyIronBoots"); // 실제 철제 장화 획득
            var dogTag = Acquire("FadedDogTag"); // 전투 첫 체력 손실 방지 유물 동시 보유
            for (int round = 0; round < 3; round++) // 군번줄과 장화 한도 및 재충전까지 세 차례 검사
            { // 예고 실행 시작
                Assert.AreEqual(4, actions.PrepareActions()); // 네 명의 실제 적 공격 예고 생성
                var planned = actions.PlannedActions.ToArray(); // 실제 공격 순서 기록
                for (int index = 0; index < planned.Length; index++) // 서로 다른 아군을 번갈아 노리도록 설정
                { // 실제 대상 선택 시작
                    Assert.IsTrue(planned[index].ChangeTarget(fight.Allies[index % 3])); // 주 대상 세 명에게 파티 공용 횟수 적용
                } // 실제 대상 선택 종료
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴 도착
                for (int index = 0; index < planned.Length; index++) // 실제 예고마다 피해와 예고 일치 검사
                { // 체력 손실 확인 시작
                    var action = planned[index]; // 현재 처리할 적 예고
                    var victim = action.Target; // 예정된 공격의 실제 아군
                    if (round == 0 && index == 0) // 첫 공격의 체력 손실을 전부 흡수할 방어도 준비
                    { // 방어도 준비 시작
                        victim.GainBlock(action.PreviewDamage().FinalDamage); // 원래 공격 피해만큼 방어도 지급
                    } // 방어도 준비 종료
                    int before = victim.CurrentHealth; // 실제 공격 전 체력
                    var preview = action.PreviewDamage(); // 실제 실행 처리와 같은 예정 피해
                    int raw = preview.FinalDamage; // 물리 방어까지 거친 원래 피해
                    var result = actions.ExecuteAction(action).DamageResult; // 적의 실제 공격 실행
                    int expected = round == 0 && index < 2 ? 0 : round > 0 && index == 3 ? raw : Mathf.Max(0, raw - 1); // 첫 흡수와 군번줄 및 파티 세 번 한도 대조
                    Assert.AreEqual(expected, preview.AppliedDamage, "예고 " + round + "/" + index); // 실제 순서에서 소모할 유물과 동일한 예고
                    Assert.AreEqual(expected, result.AppliedDamage, "피해 " + round + "/" + index); // 실제 체력 손실 경감 확인
                    Assert.AreEqual(before - expected, victim.CurrentHealth); // 지급 기록과 실제 아군 체력 비교
                } // 체력 손실 확인 종료
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴부터 파티 횟수 복구
            } // 예고 실행 종료
            Assert.IsTrue(RelicRunManager.Instance.Inventory.TryRemove(dogTag.RelicId)); // 비교를 끝낸 군번줄 제거
        } // 전투 진행 종료
        yield return null; // 유닛 화면 정리 대기
    } // 장화 검사 종료

    [UnityTest] // 정신력 경계의 실제 카드 적중과 회복 검사
    public IEnumerator RoseCorsagesFollowCardOwnerAndOnlyTheFirstHitEachTurn() // 정신력 오십과 삼십 경계 및 범위 타격 확인
    { // 코사지 실전 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 화면 시작
        yield return CheckRoses(); // 직접 카드와 소유자 정신력 비교
        yield return new ExitPlayMode(); // 실제 손패 화면 종료
    } // 코사지 실전 검사 종료
    private static IEnumerator CheckRoses() // 실제 회복 및 고정 인접 공격 카드 사용
    { // 코사지 검사 시작
        using (var fight = new Fight("CLEAR_WATER", "CLEAR_WATER", "HORIZONTAL_SLASH", "HORIZONTAL_SLASH", "CHECK_SLASH")) // 회복과 범위 공격이 들어간 손패
        { // 실제 카드 입력 시작
            Acquire("WhiteRoseCorsage"); // 회복 코사지 획득
            Acquire("BlackRoseCorsage"); // 공격 코사지 획득
            var owner = fight.Allies[0]; // 두 카드를 사용하는 실제 소유자
            var target = fight.Allies[1]; // 회복자와 다른 회복 대상
            target.ApplyPersistentMental(30); // 회복받는 사람의 정신력은 낮게 유지
            target.ApplyPersistentHealth(20); // 실제 회복할 체력 마련
            var portrait = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).Single(view => view.RuntimeUnit == target); // 해당 대상의 실제 화면
            owner.ApplyPersistentMental(49); // 흰 코사지 조건 바로 아래
            yield return WinterfallCommonCardExpansionTests.Play(fight.View("CLEAR_WATER").RuntimeCard.SourceData, fight.Hand, portrait, fight.Runner, fight.Deck, fight.Points); // 실제 회복 카드와 아군 선택
            Assert.AreEqual(23, target.CurrentHealth); // 정신력 사십구에서는 기본 회복 셋
            owner.ApplyPersistentMental(50); // 정신력 오십 경계
            yield return WinterfallCommonCardExpansionTests.Play(fight.View("CLEAR_WATER").RuntimeCard.SourceData, fight.Hand, portrait, fight.Runner, fight.Deck, fight.Points); // 같은 원본으로 두 번째 회복
            Assert.AreEqual(27, target.CurrentHealth); // 받는 아군과 무관하게 사용자의 코사지 보정 한 점
            owner.ApplyPersistentMental(31); // 검은 코사지 조건 바로 위
            yield return fight.Use("CHECK_SLASH", 2); // 정신력 삼십일에서 단일 공격
            Assert.AreEqual(196, fight.Enemies[2].CurrentHealth); // 조건 불충족 시 일반 피해 넷
            owner.ApplyPersistentMental(30); // 검은 코사지 경계 정신력
            yield return fight.Use("HORIZONTAL_SLASH", 0); // 실제 인접 두 칸 범위 공격
            Assert.AreEqual(193, fight.Enemies[0].CurrentHealth); // 최초 적중 한 타격에만 기본 다섯과 추가 둘
            Assert.AreEqual(195, fight.Enemies[1].CurrentHealth); // 같은 카드 다음 대상의 기본 피해 유지
            owner.ApplyPersistentMental(30); // 같은 턴 두 번째 유효 공격 준비
            yield return fight.Use("HORIZONTAL_SLASH", 0); // 다시 두 명 공격
            Assert.AreEqual(188, fight.Enemies[0].CurrentHealth); // 두 번째 카드에는 같은 턴 보너스 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 턴 실제 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 새 아군 턴 시작
            owner.ApplyPersistentMental(30); // 다음 턴 같은 정신력 준비
            yield return fight.Use("HORIZONTAL_SLASH", 0); // 새 아군 턴의 첫 직접 타격
            Assert.AreEqual(181, fight.Enemies[0].CurrentHealth); // 새 턴 추가 피해 재충전
        } // 실제 카드 입력 종료
    } // 코사지 검사 종료

    [UnityTest] // 카드 이름이나 덱 재배치로 발동하지 않는 확인 검사
    public IEnumerator PencilDrawsOnlyAfterSelectingACardAndPreservesFoolPassive() // 실제 덱 선택 및 남은 카드 처리와 중복 지급 확인
    { // 연필 실전 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 선택 UI 시작
        yield return CheckPencil(); // 발동과 남은 카드 선택 및 턴당 제한 검사
        yield return new ExitPlayMode(); // 실제 카드 선택 UI 종료
    } // 연필 실전 검사 종료
    private static IEnumerator CheckPencil() // 실제 바보의 선택 카드 사용
    { // 선택 드로우 검사 시작
        using (var fight = new Fight("StarterCards/FoolChoice", "StarterCards/FoolChoice", "THRUST", "KNEE_SLASH", "POMMEL_STRIKE", "INTERCEPT_SLASH", "CHECK_SLASH", "BLOOD_EDGE")) // 시작 카드 두 장과 선택할 실제 카드 여섯 장
        { // 손패 입력 시작
            Acquire("GraphitePencil"); // 실제 덱 확인 보너스 획득
            var source = Resources.Load<CardData>("StarterCards/FoolChoice"); // 덱 위 두 장 중 한 장을 고르는 실제 원본
            foreach (var card in fight.Deck.Hand.Where(card => card.SourceData != source).ToArray()) // 선택할 카드들을 버린 더미로 이동
            { // 덱 준비 시작
                Assert.IsTrue(fight.Deck.DiscardCard(card)); // 기존 덱 섞기 규칙 재사용
            } // 덱 준비 종료
            Assert.AreEqual(1, fight.Deck.DrawCards(1)); // 일반 드로우는 항상 한 장만 처리
            for (int use = 0; use < 2; use++) // 같은 턴 두 번의 확인 선택 비교
            { // 확인 카드 준비 시작
                var view = fight.Hand.GetComponentsInChildren<BattleCardView>().First(item => item.RuntimeCard?.SourceData == source && fight.Deck.Hand.Contains(item.RuntimeCard)); // 사용 가능한 실제 확인 카드 화면
                int initial = fight.Deck.HandCount; // 사용 전 실제 손패 수
                view.OnPointerClick(null); // 바보의 선택 원본 카드 사용
                float deadline = Time.realtimeSinceStartup + 10f; // 실제 카드 선택 패널 대기 기한
                while (Object.FindFirstObjectByType<BattleDrawChoiceView>() == null && Time.realtimeSinceStartup < deadline) // 충돌 및 손패 복귀 연출 대기
                { // 선택 창 대기 시작
                    yield return null; // 실제 전투 프레임 진행
                } // 선택 창 대기 종료
                var panel = Object.FindFirstObjectByType<BattleDrawChoiceView>(); // 덱 위에서 실제로 고를 후보 화면
                Assert.IsNotNull(panel); // 선택 창 정상 생성
                Assert.AreEqual(initial - 1, fight.Deck.HandCount); // 선택 전에는 사용한 한 장만 소모
                panel.transform.Find("Choice_0").GetComponent<Button>().onClick.Invoke(); // 첫 후보 카드를 실제 손패로 이동
                if (use == 0) // 바보의 이번 턴 최초 확인 처리
                { // 남은 카드 보존 확인 시작
                    var remaining = Object.FindFirstObjectByType<BattleRemainingCardChoiceView>(); // 기존 바보 패시브의 남은 카드 선택
                    Assert.IsNotNull(remaining); // 연필이 덱 위 카드를 먼저 빼앗지 않음
                    Assert.AreEqual(initial, fight.Deck.HandCount); // 최종 결정을 기다리는 동안 기본 한 장만 이동
                    remaining.transform.Find("KeepRemaining").GetComponent<Button>().onClick.Invoke(); // 남은 카드를 덱 위에 둔 뒤 연필 보너스 지급
                } // 남은 카드 보존 확인 종료
                Assert.AreEqual(initial + (use == 0 ? 1 : 0), fight.Deck.HandCount); // 같은 턴 추가 드로우 한 번만 지급
                yield return null; // 닫힌 선택 패널 제거 대기
            } // 확인 카드 준비 종료
        } // 손패 입력 종료
    } // 선택 드로우 검사 종료
} // 유물 검사 종료
