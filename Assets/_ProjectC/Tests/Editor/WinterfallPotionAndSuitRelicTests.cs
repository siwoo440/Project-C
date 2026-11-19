using System; // 임시 저장 식별자와 대기 조건
using System.Collections; // 실제 카드 연출 대기
using System.IO; // 검사 전용 저장 파일
using System.Linq; // 보관 원본과 생존자 조회
using NUnit.Framework; // 기대 결과 비교
using UnityEngine; // 실제 전투 자산과 화면
using UnityEngine.SceneManagement; // 실제 출발 씬
using UnityEngine.TestTools; // 플레이 모드 실행
using Object = UnityEngine.Object; // 현재 씬의 실제 화면 조회

public sealed class WinterfallPotionAndSuitRelicTests // 시트 유물 여덟 종의 효과와 저장 경로 검사
{ // 유물 검사 시작
    private static RelicData Relic(string name) // 정식 유물 원본 조회
    { // 유물 조회 시작
        return Resources.Load<RelicData>("Relics/" + name); // 실제 획득 원본 반환
    } // 유물 조회 종료
    private static ShopCatalogData Shop => Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 거래 목록
    private static CardData Card(string id) // 실제 보물 카드 원본 조회
    { // 카드 조회 시작
        return Shop.Offers.Single(item => item.OfferId == "REWARD_SHEET_" + id).Card; // 정상 보상 카드 반환
    } // 카드 조회 종료
    [TestCase("ExperimentalPotionRack", 17)] // 포션 보관대 시트 행
    [TestCase("GreasyWrench", 18)] // 회복 포션 렌치 시트 행
    [TestCase("BronzeAlarm", 19)] // 적 턴 경보 시트 행
    [TestCase("BlueSuture", 20)] // 회복 방어도 시트 행
    [TestCase("BrokenClock", 21)] // 세 턴 드로우 시트 행
    [TestCase("FaintChaliceShard", 22)] // 성배 회복 시트 행
    [TestCase("RustyScabbard", 23)] // 첫 검 적중 시트 행
    [TestCase("GlassWandCore", 24)] // 첫 지팡이 마법 시트 행
    public void VerifySource(string name, int row) // 원문과 실제 상점·저장·아이콘 연결
    { // 원본 대조 시작
        var relic = Relic(name); // 실제 유물
        Assert.IsNotNull(relic); // 자산 존재 확인
        var entry = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 원문 행 조회
        Assert.AreEqual(row, entry.SourceRow); // 원문 위치 확인
        Assert.AreEqual(entry.Description, relic.Description); // 확정 설명 일치
        Assert.IsNotNull(relic.Icon); // 원화 연결
        Assert.IsTrue(Shop.Offers.Any(item => item.Relic == relic)); // 정상 획득 경로 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics, relic); // 복원 원본 확인
    } // 원본 대조 종료
    [UnityTest] // 실제 손패 입력과 피해 검사
    public IEnumerator SwordAndWandHitOnceAcrossClicksAndTurns() // 다단·광역 첫 명중과 다음 턴 확인
    { // 실제 카드 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 연출 진입
        var manager = RelicRunManager.EnsureInstance(); // 회차 유물 보관함
        Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(Relic("RustyScabbard"))); // 실제 검집 획득
        Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(Relic("GlassWandCore"))); // 실제 지팡이 심 획득
        using (var fight = new WinterfallSwordCombatTests.Fight("SWORD_DANCE", "SLASH", "FIREBALL", "MAGIC_MISSILE")) // 정상 손패와 전투 입력
        { // 실제 사용 시작
            int total = fight.Enemies.Sum(unit => unit.CurrentHealth); // 범위 공격 전 전체 적 체력
            yield return fight.Use("SWORD_DANCE", 1); // 실제 무작위 세 번 공격
            Assert.AreEqual(20, total - fight.Enemies.Sum(unit => unit.CurrentHealth)); // 기본 여섯씩 세 번과 추가 두 점 한 번
            int hp = fight.Enemies[0].CurrentHealth; // 다음 검의 기준 체력
            yield return fight.Use("SLASH", 0); // 같은 턴 다음 검 사용
            Assert.AreEqual(Card("SLASH").EffectValue, hp - fight.Enemies[0].CurrentHealth); // 후속 검의 기본 피해
            hp = fight.Enemies[0].CurrentHealth; // 지팡이 첫 공격 전 체력
            yield return fight.Use("FIREBALL", 0); // 실제 첫 지팡이 마법 사용
            Assert.AreEqual(Card("FIREBALL").EffectValue + 2, hp - fight.Enemies[0].CurrentHealth); // 검과 독립된 지팡이 보너스
            hp = fight.Enemies[0].CurrentHealth; // 후속 마법 기준 체력
            yield return fight.Use("MAGIC_MISSILE", 0); // 같은 턴 다음 지팡이 사용
            Assert.AreEqual(Card("MAGIC_MISSILE").EffectValue * Card("MAGIC_MISSILE").DamageHitCount, hp - fight.Enemies[0].CurrentHealth); // 후속 마법 기본 피해
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 턴 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 진입
            hp = fight.Enemies[0].CurrentHealth; // 새 턴 기준 체력
            yield return fight.Use("SLASH", 0); // 새 턴의 첫 검
            Assert.AreEqual(Card("SLASH").EffectValue + 2, hp - fight.Enemies[0].CurrentHealth); // 턴별 검집 복구 확인
        } // 실제 사용 종료
        yield return new ExitPlayMode(); // 전투 종료
    } // 실제 카드 검사 종료
    [TestCase(BattleDamageType.Physical, 9)] // 물리 직접 공격 감소
    [TestCase(BattleDamageType.Magical, 9)] // 마법 직접 공격 감소
    [TestCase(BattleDamageType.None, 10)] // 무속성 피해는 기존 수치 유지
    public void AlarmWeakensEachEligibleHitAndStacksWithNextAttack(BattleDamageType type, int expected) // 경보 약화와 일회성 약화의 실제 적 타격 검사
    { // 직접 공격 검사 시작
        var raw = ScriptableObject.CreateInstance<EnemyData>(); // 기본 적 전투 원본
        try // 생성한 원본 해제 보장
        { // 전투 원본 검사 시작
            var foe = BattleUnitRuntime.CreateEnemy(raw); // 실제 적 공격자
            var ally = BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]); // 공격을 받을 아군
            var relic = Relic("BronzeAlarm"); // 경보 유물의 실제 효과 수치
            foe.ApplyStatusEffect(relic.StatusEffectType, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks); // 적 한 턴의 공격 약화
            foe.ApplyStatusEffect(BattleStatusEffectType.NextAttackDown, 2, 1, 1); // 이번 공격에만 남아 있는 검 카드의 약화
            var action = new BattleEnemyAction(foe, ally, EnemyActionType.Attack, type, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, string.Empty, 0, 1, 0, 0); // 실제 공격의 예고와 실행 객체
            Assert.AreEqual(expected - 2, action.PreviewDamage().RawDamage); // 두 약화가 겹친 예고 피해
            Assert.AreEqual(expected - 2, action.Execute().DamageResult.RawDamage); // 예고와 같은 첫 적중 수치
            Assert.AreEqual(expected, action.Execute().DamageResult.RawDamage); // 같은 턴 두 번째 공격에도 경보 약화 유지
            new BattleStatusEffectProcessor().ProcessPhaseEnd(foe, 1); // 적 턴 종료 시 실제 지속 처리
            Assert.AreEqual(10, action.PreviewDamage().RawDamage); // 다음 턴의 기본 피해 복구
        } // 전투 원본 검사 종료
        finally // 임시 적 원본 정리
        { // 해제 시작
            Object.DestroyImmediate(raw); // 전투 검사 원본 해제
        } // 해제 종료
    } // 직접 공격 검사 종료
    [Test] // 빗나감과 비공격 소비 검사
    public void MissAndSupportLeaveOnlyTheSameCardEligible() // 현재 카드의 첫 적중과 다음 카드의 기회 구별
    { // 첫 적중 경계 검사 시작
        var inv = new RelicInventoryRuntime(new RelicGoldRuntime()); // 독립 보관함
        inv.TryAcquire(Relic("RustyScabbard")); // 실제 검집 획득
        var unit = BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]); // 카드 사용자
        var help = CardInstance.Create(Card("INSPIRE_MORALE"), unit, 1, 0); // 비공격 검 카드
        var attack = CardInstance.Create(Card("SWORD_DANCE"), unit, 2, 0); // 다단 검 카드
        var hits = new FirstSuitHitRelics(); // 실제 공격 처리기의 명중 기록
        hits.BeginCard(help, 1, inv); // 비공격 검 사용
        hits.BeginCard(attack, 1, inv); // 첫 피해 카드 사용
        Assert.AreEqual(8, hits.ApplyHitBonus(6)); // 첫 적중까지 남은 두 점
        Assert.AreEqual(6, hits.ApplyHitBonus(6)); // 같은 카드 후속 타격 기본량
        hits.BeginCard(attack, 2, inv); // 새 턴 첫 카드의 모든 타격 빗나감
        hits.BeginCard(attack, 2, inv); // 다음 카드 사용
        Assert.AreEqual(6, hits.ApplyHitBonus(6)); // 다른 카드로 첫 명중 이월 차단
    } // 첫 적중 경계 검사 종료
    [Test] // 사용자와 회복 대상 및 턴별 횟수 검사
    public void HealingCardsAndClockRespectOwnerTurnAndHandLimit() // 만피 회복 카드·후속 회복과 시계 드로우 검사
    { // 조건부 유물 검사 시작
        var allies = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters.Take(2).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 사용자와 회복 대상
        var wallet = new RelicGoldRuntime(); // 독립 지갑
        var inv = new RelicInventoryRuntime(wallet); // 검사 보관함
        foreach (var name in new[] { "BlueSuture", "FaintChaliceShard", "BrokenClock" }) // 검사할 카드와 턴 유물
        { // 원본 지급 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, inv.TryAcquire(Relic(name))); // 실제 유물 획득
        } // 원본 지급 종료
        var source = DeckData.CreateRuntime(Enumerable.Repeat(new DeckCardEntry(Card("SMALL_HEAL"), allies[0].CharacterSource), 4).ToArray()); // 드로우할 회복 카드 네 장
        try // 덱 정리 보장
        { // 카드 사용 조건 시작
            var deck = BattleDeckRuntime.Create(source, allies, 1, 228); // 한 장 손패 상한
            using (var events = new BattleEventDispatcher()) // 공용 전투 신호
            using (var effects = new BattleRelicEffectController(events, inv, wallet, allies, Array.Empty<BattleUnitRuntime>(), deck)) // 실제 유물 처리기
            { // 전투 신호 시작
                var owner = allies[1]; // 첫 편성원과 다른 카드 사용자
                var target = allies[0]; // 최대 체력의 별도 회복 대상
                owner.ApplyPersistentHealth(owner.MaxHealth - 5); // 사용자 체력 회복 여지
                events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 1, BattleTurnPhase.PlayerTurn)); // 첫 아군 턴
                var heal = CardInstance.Create(Card("SMALL_HEAL"), owner, 1, 0); // 직접 회복 성배 카드
                for (int i = 0; i < 3; i++) // 턴별 발동 제한 이상으로 사용
                { // 회복 카드 사용 시작
                    events.Publish(new BattleEventContext(BattleEventType.CardUsed, 1, BattleTurnPhase.PlayerTurn, owner, target, null, heal)); // 실제 카드 사용 신호
                } // 회복 카드 사용 종료
                Assert.AreEqual(3, owner.CurrentBlock); // 만피 대상에도 사용자 방어도 한 번
                Assert.AreEqual(0, target.CurrentBlock); // 대상에게 방어도 미지급
                Assert.AreEqual(owner.MaxHealth - 3, owner.CurrentHealth); // 사용자에게 두 번 회복
                events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 2, BattleTurnPhase.PlayerTurn)); // 다음 아군 턴 시작
                var follow = CardInstance.Create(Card("HOLY_STRIKE"), owner, 2, 0); // 피해 뒤 생존 아군을 회복하는 카드
                events.Publish(new BattleEventContext(BattleEventType.CardUsed, 2, BattleTurnPhase.PlayerTurn, owner, target, null, follow)); // 명시적 후속 체력 회복 카드 사용
                Assert.AreEqual(6, owner.CurrentBlock); // 후속 회복도 봉합사 발동
                Assert.AreEqual(0, deck.HandCount); // 첫 두 턴의 추가 드로우 없음
                events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 3, BattleTurnPhase.EnemyTurn)); // 세 번째 적 턴
                Assert.AreEqual(0, deck.HandCount); // 적 턴 추가 드로우 제외
                events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 3, BattleTurnPhase.PlayerTurn)); // 세 번째 아군 턴
                Assert.AreEqual(1, deck.HandCount); // 추가 드로우 한 장
                events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 6, BattleTurnPhase.PlayerTurn)); // 손패가 찬 여섯 번째 아군 턴
                Assert.AreEqual(1, deck.HandCount); // 최대 손패 규칙 보존
            } // 전투 신호 종료
        } // 카드 사용 조건 종료
        finally // 임시 덱 정리
        { // 덱 제거 시작
            Object.DestroyImmediate(source); // 검사 전용 원본 해제
        } // 덱 제거 종료
    } // 조건부 유물 검사 종료
    [UnityTest] // 실제 탐사부터 전투까지 검사
    public IEnumerator CheckpointSalePotionAndEnemyTurnPreserveRules() // 저장 복원과 판매 및 포션 소비와 적 턴 검사
    { // 회차 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        yield return CheckRun(); // 출전과 실제 전투 입력 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 회차 검사 종료
    private static IEnumerator CheckRun() // 실제 회차 거래와 탐사 저장 검증
    { // 탐사 시작
        SceneManager.LoadScene("00_Boot"); // 게임 시작 씬 진입
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null); // 메뉴 준비
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 시작
        yield return WaitFor(() => Object.FindFirstObjectByType<LobbyLayoutView>() != null); // 거점 편성 준비
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 네 명 탐사 출전
        yield return WaitFor(() => Object.FindFirstObjectByType<ExplorationPlayerController>() != null && !SceneFlowManager.Instance.IsLoadingScene); // 실제 탐사 배치 대기
        var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 저장 위치
        var rack = Relic("ExperimentalPotionRack"); // 실제 포션 보관대
        var relics = RelicRunManager.EnsureInstance(); // 회차 보관함
        var items = ConsumableRunManager.EnsureInstance().Inventory; // 소모품 보관함
        var potion = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Consumables.OfType<PotionData>().First(item => item.EffectType == PotionEffectType.RestoreHealth); // 등록된 실제 체력 포션
        var path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 검사 전용 임시 저장
        try // 저장 정리 보장
        { // 실제 회차 검사 시작
            items.Clear(); // 검사 파티 소모품 초기화
            Assert.AreEqual(10, items.Capacity); // 보관대 없는 기본 열 칸
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 기존 열 칸 저장 형식 생성
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 보관대 없는 열 칸 저장 복원
            Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(rack)); // 정식 유물 획득
            Assert.AreEqual(11, items.Capacity); // 열한 번째 칸 개방
            for (int index = 0; index < items.Capacity; index++) // 기본칸과 추가칸 모두 채우기
            { // 포션 획득 시작
                Assert.IsTrue(items.TryAcquire(potion, out int slot)); // 실제 포션 획득
                Assert.AreEqual(index, slot); // 추가칸 포함 획득 순서
            } // 포션 획득 종료
            var view = Object.FindFirstObjectByType<ExplorationInventoryView>(FindObjectsInactive.Include); // 탐사 보관함 화면
            Assert.IsTrue(view.transform.Find("Slots/Slot_11").gameObject.activeSelf); // 실제 화면에 추가칸 표시
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 전용칸까지 채운 회차 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 복원 전에도 가득 찬 보관함 복구
            Assert.AreSame(potion, items.GetItem(10)); // 추가칸 원본 유지
            Assert.AreEqual(11, items.Count); // 아이템 유실과 중복 방지
            var shop = ShopRunManager.EnsureInstance(); // 실제 상점
            Assert.IsTrue(shop.Prepare(Shop, PartyLoadoutRuntime.Active.Deck)); // 출전 덱과 상점 연결
            shop.BeginRoomVisit("potion_rack_sale"); // 거래 방 방문
            int gold = shop.Gold; // 판매 실패 전 지갑
            Assert.AreEqual(ShopPurchaseResult.Unavailable, shop.TrySellRelic(rack)); // 가득 찬 보관대 판매 차단
            Assert.AreEqual(gold, shop.Gold); // 판매 대금 미지급
            Assert.AreEqual(11, items.Count); // 포션 보존
            Assert.IsTrue(items.TryRemoveAt(3)); // 기본칸 하나 비우기
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(rack)); // 실제 상점 판매 성공
            Assert.AreEqual(10, items.Capacity); // 기본 용량으로 축소
            Assert.AreEqual(10, items.Count); // 기본칸으로 이전한 포션까지 보존
            Assert.AreSame(potion, items.GetItem(3)); // 포션 원본이 빈 기본칸으로 이전
            Assert.IsFalse(view.transform.Find("Slots/Slot_11").gameObject.activeSelf); // 추가칸 숨김
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 판매 후 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 판매 완료 상태 복원
            Assert.AreEqual(10, items.Count); // 복원 후 소모품 수 보존
            Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(Relic("GreasyWrench"))); // 회복 포션 렌치 획득
            Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(Relic("BronzeAlarm"))); // 경보장치 획득
            SceneFlowManager.Instance.LoadScene("40_Battle"); // 편성 그대로 전투 진입
            yield return WaitFor(() => Object.FindFirstObjectByType<BattleSceneSetup>()?.IsInitialized == true); // 실제 전투 준비 확인
            var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 현재 전투
            var ally = battle.AllyUnits[0]; // 원래 체력 포션 대상
            ally.ApplyPersistentHealth(1); // 충분한 회복 여지
            using (var controller = new BattleConsumableController(items, battle)) // 실제 포션 클릭 경로
            { // 포션 사용 시작
                controller.HandleSlotClick(0, false); // 포션 사용 선택
                Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).First(unit => unit.RuntimeUnit == ally).OnPointerClick(null); // 실제 대상 클릭
                Assert.AreEqual(Math.Min(ally.MaxHealth, 1 + potion.EffectValue + 2), ally.CurrentHealth); // 기존 포션 회복량과 추가 두 점
                Assert.AreEqual(9, items.Count); // 정상 소비 한 병
            } // 포션 사용 종료
            Assert.IsTrue(battle.BattleTurn.EndPlayerTurn()); // 적 실제 턴 시작
            var weakened = battle.EnemyUnits.Single(enemy => enemy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown) != null); // 생존 적 중 한 명에게만 약화
            Assert.IsFalse(weakened.IsDead); // 대상 생존 확인
            Assert.IsTrue(battle.BattleTurn.CompleteEnemyTurn()); // 적 턴 종료
            Assert.IsNull(weakened.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 한 턴 뒤 해제
        } // 실제 회차 검사 종료
        finally // 임시 파일 정리
        { // 파일 정리 시작
            if (File.Exists(path)) // 검사 파일 존재 확인
            { // 파일 제거 시작
                File.Delete(path); // 검사에서 생성한 파일 제거
            } // 파일 제거 종료
        } // 파일 정리 종료
    } // 탐사 종료
    private static IEnumerator WaitFor(Func<bool> ready) // 실제 씬 초기화 대기
    { // 준비 대기 시작
        float end = Time.realtimeSinceStartup + 30f; // 초기화 기한
        while (!ready() && Time.realtimeSinceStartup < end) // 준비 상태 확인
        { // 프레임 대기 시작
            yield return null; // 다음 프레임 대기
        } // 프레임 대기 종료
        Assert.IsTrue(ready()); // 초기화 완료 확인
    } // 준비 대기 종료
} // 유물 검사 종료
