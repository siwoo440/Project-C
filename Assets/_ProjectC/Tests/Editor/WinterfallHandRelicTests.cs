using System.Collections; // 전투 연출과 플레이 모드 전환 대기
using System.Linq; // 실제 카드와 유물 원본 조회
using NUnit.Framework; // 검사 결과 대조
using UnityEngine; // 실제 전투 유닛과 화면 사용
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 손패 입력 화면 생성

public sealed class WinterfallHandRelicTests // 전투 손패와 행동력 유물 네 종 검사
{ // 검사 시작
    [TestCase(8, "FragmentedMirror", "FRAGMENTED_MIRROR")] // 파편화된 거울 원본
    [TestCase(9, "ErosionEchoBottle", "EROSION_ECHO_BOTTLE")] // 침식 잔향병 원본
    [TestCase(12, "BlackSpool", "BLACK_SPOOL")] // 검은 실타래 원본
    [TestCase(16, "BrokenMedal", "BROKEN_MEDAL")] // 부서진 메달 원본
    public void RegisteredRelicHasSheetTextAndArtwork(int row, string file, string id) // 시트·저장·획득의 유물 원본 확인
    { // 원본 검사 시작
        var relic = Resources.Load<RelicData>("Relics/" + file); // 실제 전투용 유물 원본 조회
        Assert.IsNotNull(relic); // 유물 등록 확인
        Assert.AreEqual("RLC_" + id, relic.RelicId); // 고정 저장 식별자 확인
        var sheet = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 같은 이름의 기획 행 조회
        Assert.AreEqual(row, sheet.SourceRow); // 시트 원본 행 확인
        Assert.AreEqual(sheet.Description, relic.Description); // 확정 효과 원문 일치 확인
        Assert.IsNotNull(relic.Icon); // 실제 아이콘 연결 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics, relic); // 저장 복원 유물 확인
        Assert.IsTrue(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Any(offer => offer.Relic == relic)); // 실제 상점과 보물 획득 원본 확인
    } // 원본 검사 종료

    [UnityTest] // 네 유물의 실제 손패 사용과 턴 진행 검사 지정
    public IEnumerator FullHandRetriesMirrorAndTurnsRetainDrawAndExpireCapacity() // 손패 상한과 다음 턴 및 전투 종료 확인
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 실행 환경 진입
        yield return PlayBattle(false); // 일반 전투의 모든 유물 경로 검사
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 플레이 검사 종료

    [UnityTest] // 적 선제와 강제 라운드 전환 검사 지정
    public IEnumerator EnemyOpeningWaitsForFirstPlayerAndBossTransitionCleansBonuses() // 적 선제와 보스 전환 시 실제 손패 정리 확인
    { // 선제 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        yield return PlayBattle(true); // 선제 후 첫 아군 턴과 다음 라운드 검사
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 선제 검사 종료

    private static IEnumerator PlayBattle(bool ambush) // 실제 전투와 덱 및 유물 신호 연결
    { // 전투 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 카드 소유자 명단 조회
        var owner = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 검 카드 소유자의 전투 유닛 생성
        var allies = new[] { owner }; // 전투 파티 준비
        var enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 독립 적 원본 생성
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemySource) }; // 체력 오십인 검사 적 준비
        var card = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards.Single(item => item.CardId == "CRD_SHEET_SLASH"); // 실제 소유자 선택 카드 조회
        var entries = Enumerable.Range(0, 7).Select(i => new RunDeckCardEntry(card, owner.CharacterSource)).ToArray(); // 영구 덱 카드 일곱 장 준비
        foreach (var entry in entries) // 원본 카드별 강화
        { // 강화 시작
            Assert.IsTrue(entry.TryUpgrade()); // 각 카드 한 단계 강화 확인
        } // 강화 종료
        var source = DeckData.CreateRuntime(entries.Select(entry => new DeckCardEntry(entry.Card, entry.Owner)).ToArray()); // 실제 편성 덱 원본 준비
        var deck = BattleDeckRuntime.Create(source, entries, allies, 3, 226); // 손패 상한 세 장의 전투 덱 생성
        var points = new BattleActionPointRuntime(6); // 기존 공용 행동력 여섯 준비
        var wallet = new RelicGoldRuntime(); // 독립 유물 지갑 생성
        var inventory = new RelicInventoryRuntime(wallet); // 독립 유물 보관함 준비
        foreach (string file in new[] { "FragmentedMirror", "ErosionEchoBottle", "BlackSpool", "BrokenMedal" }) // 네 유물 원본 순회
        { // 유물 지급 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(Resources.Load<RelicData>("Relics/" + file))); // 실제 유물 보유 확인
        } // 유물 지급 종료
        var canvas = new GameObject("HandRelicBattle", typeof(RectTransform), typeof(Canvas)); // 독립 전투 화면 준비
        var handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 입력 생성
        handObject.transform.SetParent(canvas.transform, false); // 전투 화면에 손패 연결
        var hand = handObject.GetComponent<BattleHandView>(); // 실제 손패 화면 조회
        var allyView = WinterfallCommonCardExpansionTests.CreateView(canvas.transform, owner); // 소유자 화면 연결
        var enemyView = WinterfallCommonCardExpansionTests.CreateView(canvas.transform, enemies[0]); // 단일 적 대상 화면 연결
        var runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 연출과 카드 정산 연결
        try // 전투 자원 정리 보장
        { // 전투 검사 시작
            using (var turns = new BattleTurnRuntime(deck, points, allies, enemies, 1, ambush ? BattleType.Boss : BattleType.Normal)) // 기본 드로우 한 장의 보스 또는 일반 전투 생성
            using (var enemyActions = new BattleEnemyActionRuntime(enemies, allies)) // 적 행동 관리자 준비
            using (var actions = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { allyView }, new[] { enemyView }, enemyActions)) // 실제 전투 카드 사용기 연결
            using (var dispatcher = new BattleEventDispatcher()) // 정식 전투 이벤트 발행기 생성
            using (var signals = new BattleEventController(dispatcher, turns, actions, allies, enemies)) // 실제 사용과 버림 및 턴 신호 연결
            using (var effects = new BattleRelicEffectController(dispatcher, inventory, wallet, allies, enemies, deck, points)) // 실제 유물 발동 순서 연결
            { // 전투 진행 시작
                Assert.IsTrue(turns.StartBattle(3, ambush)); // 세 장 손패의 정상 전투 시작
                if (ambush) // 적 선제 전투 확인
                { // 적 선제 검사 시작
                    Assert.AreEqual(6, points.MaxActionPoints); // 적 선제 중 기존 상한 유지 확인
                    Assert.AreEqual(0, points.CurrentActionPoints); // 적 선제 중 아군 행동력 미지급 확인
                    Assert.IsTrue(turns.CompleteEnemyTurn()); // 대기하던 첫 아군 턴 진입
                } // 적 선제 검사 종료
                Assert.AreEqual(7, points.MaxActionPoints); // 첫 아군 턴의 행동력 상한 일곱 확인
                Assert.AreEqual(7, points.CurrentActionPoints); // 첫 아군 턴의 실제 행동력 일곱 확인
                Assert.IsTrue(hand.Bind(deck, points, turns)); // 최초 손패 입력 연결
                var originals = deck.Cards.ToArray(); // 전투 복사 이전의 카드 객체 보관
                yield return PlayFirstHandCard(deck, points, hand, enemyView, runner, false); // 가득 찬 손패에서 첫 카드 사용
                Assert.AreEqual(7, deck.CardCount); // 빈자리 없어 복사와 기회 소비 보류 확인
                Assert.AreEqual(2, deck.HandCount); // 사용 정산에 실타래가 개입하지 않는지 확인
                var copiedFrom = FirstHandView(deck, hand).RuntimeCard; // 다음 사용과 복사를 확인할 원래 카드
                int originalCost = copiedFrom.ApCost; // 임시 할인 전 강화 카드 비용 보관
                yield return PlayFirstHandCard(deck, points, hand, enemyView, runner, true); // 실제 할인 카드 사용으로 거울 재시도
                var copy = deck.Cards.Single(item => !originals.Contains(item)); // 새로 생성된 한 장의 전투 복사본 조회
                Assert.AreNotSame(copiedFrom, copy); // 복사본 독립 객체 확인
                Assert.AreSame(copiedFrom.SourceData, copy.SourceData); // 카드 원본 보존 확인
                Assert.AreSame(copiedFrom.OwnerUnit, copy.OwnerUnit); // 원래 카드 소유자 보존 확인
                Assert.AreEqual(1, copy.UpgradeLevel); // 원본 카드 강화 단계 보존 확인
                Assert.AreEqual(originalCost, copy.ApCost); // 사용 턴의 임시 할인 제외 확인
                CollectionAssert.Contains(deck.Hand, copy); // 복사본 실제 손패 지급 확인
                var drawnTop = deck.PeekDrawTop(1).Single(); // 덱에서 직접 버릴 카드 조회
                Assert.IsTrue(deck.TryDiscardDrawTop(drawnTop)); // 덱 위 버림은 실타래 기회 미소비
                var kept = deck.Hand.First(item => item != copy); // 실제 효과로 버릴 손패 선택
                Assert.IsTrue(deck.DiscardCard(kept)); // 첫 효과 버림 처리
                CollectionAssert.Contains(deck.Hand, kept); // 첫 효과 버림의 카드 유지 확인
                Assert.IsTrue(deck.DiscardCard(copy)); // 다음 버림으로 전투 복사본 선택
                CollectionAssert.Contains(deck.DiscardPile, copy); // 턴당 한 장만 유지 확인
                if (ambush) // 강제 보스 라운드 전환 확인
                { // 강제 전환 시작
                    Assert.IsTrue(deck.DiscardCard(kept)); // 이미 한 번 유지한 같은 카드의 두 번째 효과 버림
                    CollectionAssert.Contains(deck.DiscardPile, kept); // 같은 카드에 재차 실타래 적용 방지 확인
                    turns.QueueBossPhaseTransition(); // 다음 플레이어 턴 예약
                    Assert.IsTrue(turns.CompleteBossPhaseTransition()); // 적 턴 없이 손패와 행동력 정리
                } // 강제 전환 종료
                else // 정상 라운드 전환 확인
                { // 정상 전환 시작
                    Assert.IsTrue(turns.EndPlayerTurn()); // 실제 턴 종료의 손패와 첫 턴 상한 정리
                    Assert.AreEqual(0, deck.HandCount); // 첫 효과 버림 이후 턴 종료에서 남는 카드 없음 확인
                    CollectionAssert.Contains(deck.DiscardPile, kept); // 앞서 한 번 보호한 카드의 정상 턴 종료 버림 확인
                    Assert.AreEqual(6, points.MaxActionPoints); // 적 턴 진입 시 기존 상한 복원 확인
                    Assert.IsTrue(turns.CompleteEnemyTurn()); // 다음 아군 턴 이동
                } // 정상 전환 종료
                Assert.AreEqual(6, points.MaxActionPoints); // 두 번째 아군 턴 기존 상한 확인
                Assert.AreEqual(6, points.CurrentActionPoints); // 두 번째 아군 턴 기존 행동력 확인
                Assert.AreEqual(ambush ? 1 : 2, turns.LastDrawnCardCount); // 빈 손패와 한 장 손패에 따른 다음 턴 드로우 확인
                var firstAtCleanup = deck.Hand[0]; // 특별한 효과 버림이 없는 다음 턴의 첫 카드 보관
                Assert.IsTrue(turns.EndPlayerTurn()); // 실타래가 자동 버림에서 한 장 선택
                Assert.AreEqual(1, deck.HandCount); // 턴 정리 중 한 장만 유지 확인
                Assert.AreSame(firstAtCleanup, deck.Hand[0]); // 손패 순서의 첫 카드 유지 확인
                Assert.IsTrue(turns.CompleteEnemyTurn()); // 세 번째 아군 턴 진입
                CollectionAssert.Contains(deck.Hand, firstAtCleanup); // 턴 종료가 첫 버림이면 같은 카드의 다음 턴 유지 확인
                while (deck.HandCount > 0) // 모든 남은 손패 직접 사용
                { // 빈 손패 준비 시작
                    yield return PlayFirstHandCard(deck, points, hand, enemyView, runner, false); // 사용한 카드는 실타래 없이 버림 정산
                } // 빈 손패 준비 종료
                Assert.AreEqual(8, deck.CardCount); // 이후 카드 사용 때 거울 재발동 방지 확인
                Assert.IsTrue(turns.EndPlayerTurn() && turns.CompleteEnemyTurn()); // 빈 손패에서 정상 다음 턴 진입
                Assert.AreEqual(1, turns.LastDrawnCardCount); // 빈 손패로 종료한 뒤 기본 드로우만 적용 확인
                if (ambush) // 도주할 수 없는 보스 전투 확인
                { // 전투 승리 시작
                    enemies[0].TakeDamage(999, BattleDamageType.Physical, owner); // 마지막 적 처치
                    Assert.AreEqual(BattleResult.Victory, turns.Result); // 실제 보스 전투 승리 확인
                } // 전투 승리 종료
                else // 일반 전투 확인
                { // 전투 도주 시작
                    Assert.IsTrue(turns.TryEscape()); // 실제 일반 전투 도주 경로 실행
                } // 전투 도주 종료
                Assert.AreEqual(7, deck.CardCount); // 전투 종료 즉시 복사본 제거 확인
                CollectionAssert.DoesNotContain(deck.Cards, copy); // 모든 전투 카드에서 임시 복사본 제거 확인
                Assert.IsTrue(deck.IsStateValid()); // 손패와 뽑을 더미 및 버린 더미의 카드 수 보존 확인
                Assert.AreEqual(7, source.Cards.Count); // 회차에 쓸 원본 덱의 영구 카드 수 보존 확인
                hand.Unbind(); // 손패 입력 연결 해제
            } // 전투 진행 종료
        } // 전투 검사 종료
        finally // 임시 객체 정리
        { // 정리 시작
            Object.Destroy(canvas); // 실제 전투 화면 제거
            Object.Destroy(source); // 독립 덱 원본 제거
            Object.Destroy(enemySource); // 독립 적 원본 제거
        } // 정리 종료
    } // 전투 준비 종료

    private static BattleCardView FirstHandView(BattleDeckRuntime deck, BattleHandView hand) // 사용할 수 있는 실제 손패 하나 조회
    { // 손패 조회 시작
        return hand.GetComponentsInChildren<BattleCardView>().First(view => view.RuntimeCard != null && deck.Hand.Contains(view.RuntimeCard)); // 현재 손패 객체와 맞는 화면 반환
    } // 손패 조회 종료

    private static IEnumerator PlayFirstHandCard(BattleDeckRuntime deck, BattleActionPointRuntime points, BattleHandView hand, BattleUnitView target, BattleActionSequenceRunner runner, bool discount) // 실제 손패 클릭과 적 선택
    { // 손패 입력 시작
        var view = FirstHandView(deck, hand); // 실제 손패 카드 조회
        var card = view.RuntimeCard; // 지불 비용 비교에 사용할 현재 원본 보관
        if (discount) // 일시 할인 카드 확인
        { // 할인 준비 시작
            card.ApplyTemporaryApDiscount(1); // 사용 카드에게만 이번 턴 할인 적용
        } // 할인 준비 종료
        int cost = card.ApCost; // 실제 할인 포함 비용 보관
        int before = points.CurrentActionPoints; // 사용 전 공용 행동력 보관
        view.OnPointerClick(null); // 실제 손패 카드 클릭
        target.OnPointerClick(null); // 실제 적 한 명 선택
        Assert.AreEqual(before - cost, points.CurrentActionPoints); // 현재 카드의 비용 한 번 지불 확인
        float until = Time.realtimeSinceStartup + 8f; // 실제 연출 대기 기한 설정
        while (runner.IsBusy && Time.realtimeSinceStartup < until) // 사용과 피해 및 카드 정산 대기
        { // 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 연출 대기 종료
        Assert.IsFalse(runner.IsBusy); // 연출과 정산 종료 확인
        CollectionAssert.Contains(deck.DiscardPile, card); // 이미 사용한 카드의 정상 버림 확인
    } // 손패 입력 종료
} // 검사 종료
