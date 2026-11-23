using System.Collections; // 플레이 모드 대기
using System.Linq; // 대상과 상품 조회
using NUnit.Framework; // 효과와 거래 검증
using UnityEditor; // 적과 덱 원본 조회
using UnityEngine; // 전투 데이터와 자원 조회
using UnityEngine.TestTools; // 실제 플레이 모드 실행

public sealed class DebuffRelicTests // 적 방어 능력 감소 유물 검사
{ // 유물 검사 시작
    [Test] // 실제 전투 시작과 지속 턴 검사
    public void StartingRelicsWeakenLivingEnemiesForOneAndTwoEnemyTurns() // 유물별 대상과 약화 만료 확인
    { // 전투 효과 검사 시작
        EnemyData source = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_WF_AshSentinel.asset"); // 방어와 저항을 지닌 재의 파수꾼 조회
        Assert.IsNotNull(source); // 적 원본 연결 확인
        BattleUnitRuntime[] enemies = { BattleUnitRuntime.CreateEnemy(source), BattleUnitRuntime.CreateEnemy(source), BattleUnitRuntime.CreateEnemy(source) }; // 적 세 명 생성
        enemies[1].TakeDamage(enemies[1].MaxHealth, BattleDamageType.None); // 가운데 적을 방어 무시 피해로 처치
        Assert.IsTrue(enemies[1].IsDead); // 사망 적 준비 확인
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]); // 검사에 참여할 아군 생성
        RelicGoldRuntime wallet = new RelicGoldRuntime(); // 독립 유물 지갑 생성
        RelicInventoryRuntime inventory = new RelicInventoryRuntime(wallet); // 검사 전용 유물 보관함 생성
        foreach (string name in new[] { "MetalCenser", "VintageRadio" }) // 실제 약화 유물 두 종류 순회
        { // 유물 획득 시작
            RelicData relic = Resources.Load<RelicData>("Relics/" + name); // 실제 유물 원본 조회
            Assert.IsNotNull(relic, name); // 데이터 연결 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 실제 보관함 획득 확인
        } // 유물 획득 종료
        Random.State previousRandom = Random.state; // 검사 전 난수 상태 보관
        try // 외부 난수 상태 복구 보장
        { // 약화 처리 검사 시작
            using (BattleEventDispatcher events = new BattleEventDispatcher()) // 독립 전투 알림 생성
            using (BattleRelicEffectController effects = new BattleRelicEffectController(events, inventory, wallet, new[] { ally }, enemies)) // 실제 유물 효과 연결
            { // 실제 전투 시작 검사
                effects.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 실제 전투의 시작 효과 적용
                BattleUnitRuntime[] living = enemies.Where(enemy => !enemy.IsDead).ToArray(); // 살아 있는 적 두 명 조회
                Assert.IsTrue(living.All(enemy => enemy.MagicalResistance == source.MagicalResistance - 1)); // 생존 적 전체의 저항력 1 감소 확인
                BattleUnitRuntime[] weakened = living.Where(enemy => enemy.PhysicalDefense == source.PhysicalDefense - 1).ToArray(); // 방어력이 1 감소한 적 검색
                Assert.AreEqual(1, weakened.Length); // 무전기의 단일 대상 확인
                BattleUnitRuntime target = weakened[0]; // 무작위로 선택된 생존 적 저장
                Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown).RemainingTurns); // 최초 방어 약화 2턴 확인
                Assert.IsEmpty(enemies[1].StatusEffects); // 사망 적의 약화 제외 확인
                Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.MagicalResistanceDown)); // 아군 저항력 보존 확인
                Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 아군 방어력 보존 확인
                BattleStatusEffectProcessor statuses = new BattleStatusEffectProcessor(); // 실제 상태 지속시간 처리기 생성
                foreach (BattleUnitRuntime enemy in living) // 생존 적의 첫 번째 턴 진행
                { // 첫 적 턴 검사 시작
                    statuses.ProcessPhaseStart(enemy, 1); // 첫 적 턴 시작
                    Assert.AreEqual(source.MagicalResistance - 1, enemy.MagicalResistance); // 첫 적 턴까지 저항 약화 유지 확인
                    statuses.ProcessPhaseEnd(enemy, 1); // 첫 적 턴 종료
                    Assert.AreEqual(source.MagicalResistance, enemy.MagicalResistance); // 첫 적 턴 종료 후 저항 회복 확인
                } // 첫 적 턴 검사 종료
                Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown).RemainingTurns); // 방어 약화 한 턴 잔여 확인
                events.Publish(new BattleEventContext(BattleEventType.BattleStarted, 2, BattleTurnPhase.PlayerTurn)); // 중복 전투 시작 알림 전달
                Assert.IsTrue(living.All(enemy => enemy.MagicalResistance == source.MagicalResistance)); // 향로 한 전투 한 번 발동 확인
                statuses.ProcessPhaseStart(target, 2); // 선택된 적의 두 번째 턴 시작
                Assert.AreEqual(source.PhysicalDefense - 1, target.PhysicalDefense); // 두 번째 적 턴까지 방어 약화 유지 확인
                statuses.ProcessPhaseEnd(target, 2); // 선택된 적의 두 번째 턴 종료
                Assert.IsTrue(living.All(enemy => enemy.PhysicalDefense == source.PhysicalDefense)); // 중복 발동 없이 모든 적의 방어 회복 확인
            } // 실제 전투 시작 검사 종료
        } // 약화 처리 검사 종료
        finally // 기존 난수 상태 복구
        { // 난수 복구 시작
            Random.state = previousRandom; // 다른 검사의 난수 상태 보존
        } // 난수 복구 종료
    } // 전투 효과 검사 종료

    [UnityTest] // 실제 유물 구매와 판매 검사
    public IEnumerator BothRelicsTradeAtSheetPricesAndUseDistinctIcons() // 두 유물의 거래 금액과 원본 등록 확인
    { // 실제 상점 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 분리된 검사 프로필 확인
        { // 일반 프로필 보호 시작
            Assert.Ignore("ProjectCVerification 검증 복제본에서 실행하세요."); // 실제 플레이어 프로필 보호
        } // 일반 프로필 보호 종료
        yield return new EnterPlayMode(); // 실제 상점 실행 준비
        ShopRunManager shop = ShopRunManager.EnsureInstance(); // 독립 회차 상점 연결
        DeckData deck = AssetDatabase.LoadAssetAtPath<DeckData>("Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset"); // 게임의 시험 출전 덱 조회
        Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), deck)); // 실제 판매 목록 연결 확인
        shop.BeginRoomVisit("day203-debuff"); // 검사 상점 방문
        PlayerResourceManager.EnsureInstance().RestoreResources(1000, 0, 0, 0); // 두 유물을 위한 구매 골드 준비
        SaveAssetCatalog save = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 복원용 원본 목록 조회
        RelicData censer = Resources.Load<RelicData>("Relics/MetalCenser"); // 금속 향로 원본 조회
        RelicData radio = Resources.Load<RelicData>("Relics/VintageRadio"); // 구형 무전기 원본 조회
        Assert.IsNotNull(censer, "금속 향로"); // 향로 리소스 연결 확인
        Assert.IsNotNull(radio, "구형 무전기"); // 무전기 리소스 연결 확인
        Assert.IsNotNull(censer.Icon); // 향로 아이콘 연결 확인
        Assert.IsNotNull(radio.Icon); // 무전기 아이콘 연결 확인
        Assert.AreNotSame(censer.Icon, radio.Icon); // 유물별 그림 구분 확인
        Assert.AreEqual(RelicTargetType.RandomLivingEnemy, radio.TargetType); // 무전기의 임시 무작위 대상 규칙 확인
        VerifyTrade(shop, save, censer, 264, 312, 134, 178); // 금속 향로의 원본 시트 가격 대조
        VerifyTrade(shop, save, radio, 322, 370, 164, 211); // 구형 무전기의 원본 시트 가격 대조
        yield return new ExitPlayMode(); // 검사 전용 회차 종료
    } // 실제 상점 검사 종료

    private static void VerifyTrade(ShopRunManager shop, SaveAssetCatalog save, RelicData relic, int buyMin, int buyMax, int sellMin, int sellMax) // 한 유물의 구매와 판매 검증
    { // 거래 검증 시작
        CollectionAssert.Contains(save.Relics, relic); // 저장 복원 목록 등록 확인
        ShopOfferData offer = shop.Catalog.Offers.Single(item => item.Relic == relic); // 상점과 보물이 공유하는 원본 상품 조회
        int price = shop.GetOfferPrice(offer); // 해당 상점의 구매 가격 조회
        Assert.That(price, Is.InRange(buyMin, buyMax)); // 구매 가격 범위 확인
        int before = shop.Gold; // 거래 전 골드 보관
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(offer)); // 실제 상점 구매 확인
        Assert.AreEqual(before - price, shop.Gold); // 실제 구매 대금 확인
        CollectionAssert.Contains(RelicRunManager.Instance.Inventory.OwnedRelics, relic); // 구매한 유물의 소유권 확인
        int sale = shop.GetRelicSellPrice(relic); // 해당 상점의 판매 가격 조회
        Assert.That(sale, Is.InRange(sellMin, sellMax)); // 판매 가격 범위 확인
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(relic)); // 실제 보유 유물 판매 확인
        Assert.AreEqual(before - price + sale, shop.Gold); // 판매한 유물의 골드 지급 확인
        Assert.IsFalse(RelicRunManager.Instance.Inventory.ContainsRelic(relic.RelicId)); // 판매한 유물의 소유권 해제 확인
    } // 거래 검증 종료
} // 유물 검사 종료
