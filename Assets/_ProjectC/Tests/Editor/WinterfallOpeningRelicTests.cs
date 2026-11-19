using System.Collections; // 플레이 모드 전환 대기
using System.Linq; // 카드와 상점 목록 조회
using System.Reflection; // 프로젝트 내부 상점 저장 진입점 조회
using NUnit.Framework; // 실제 동작 결과 비교
using UnityEngine; // 실물 데이터와 임시 검사 객체
using UnityEngine.TestTools; // 플레이 모드 검사

public sealed class WinterfallOpeningRelicTests // 신규 유물 등록과 실제 처리 검사
{ // 검사 클래스 시작
    private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static; // 비공개 검사 대상 범위
    [TestCase("BrokenAntenna", 41)] // 파손된 안테나 원본 행
    [TestCase("SafetyPinBundle", 42)] // 안전핀 묶음 원본 행
    [TestCase("RiftSealingWax", 43)] // 틈새 봉인왁스 원본 행
    [TestCase("DebrisPouch", 44)] // 잔해 주머니 원본 행
    [TestCase("SmallSafeKey", 45)] // 작은 금고 열쇠 원본 행
    [TestCase("HourglassShard", 46)] // 모래시계 파편 원본 행
    [TestCase("CrumpledLetter", 48)] // 구겨진 편지 원본 행
    [TestCase("TornEnvelope", 49)] // 찢긴 우편봉투 원본 행
    public void RelicMatchesSourceAndIsRegistered(string file, int row) // 유물 원화와 구매 및 복원 등록 확인
    { // 데이터 검사 시작
        var relic = Resources.Load<RelicData>("Relics/" + file); // 실제 유물 원본 조회
        Assert.IsNotNull(relic, file); // 등록된 유물 확인
        var sheet = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 게임의 원본 시트 항목
        Assert.AreEqual(row, sheet.SourceRow); // 지정한 원본 행 확인
        Assert.AreEqual(sheet.Description, relic.Description); // 설명과 게임 원본 일치
        Assert.IsNotNull(relic.Icon); // 개별 투명 원화 연결 확인
        Assert.AreEqual(file, relic.Icon.texture.name); // 다른 유물 그림 참조 방지
        var shop = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점 목록
        Assert.AreEqual(1, shop.Offers.Count(offer => offer.Relic == relic)); // 유일한 상품 원본 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics, relic); // 저장 복원 대상 확인
    } // 데이터 검사 종료

    [UnityTest] // 선공과 첫 아군 손패 검증
    public IEnumerator OpeningRelicsFollowTheActualFirstPlayerTurn() // 선공 속도와 첫 턴 비용·손패·행동력 확인
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 보관함 활성화
        VerifyOpeningRelics(); // 실제 턴과 행동 순서 검증
        yield return new ExitPlayMode(); // 독립 전투 종료
    } // 플레이 검사 종료

    private static void VerifyOpeningRelics() // 시작 시점과 종료 경계 확인
    { // 시작 효과 검사 시작
        Hold("BrokenAntenna", "HourglassShard", "CrumpledLetter", "TornEnvelope"); // 시작 유물 보관
        var sword = MakeCard(CardType.Sword); // 첫 검 카드
        var pentacle = MakeCard(CardType.Pentacle); // 첫 펜타클 카드
        using (var fight = new TestFight(sword, pentacle, sword, pentacle)) // 시작과 다음 턴의 실제 더미
        { // 선공 전투 검사 시작
            foreach (var card in fight.Deck.Cards.Take(2)) // 서로 다른 시작 카드 순회
            { // 시작 손패 구성 시작
                Assert.IsTrue(fight.Deck.TryMoveDrawTop(card, 4)); // 처음 두 장의 속성 고정
            } // 시작 손패 구성 종료
            Assert.IsTrue(fight.Turn.StartBattle(2, true, () => fight.Actions.PrepareActions())); // 약화가 적용된 적 선제 행동 준비
            Assert.AreEqual(0, fight.Deck.HandCount); // 선공 중에는 아군 손패 없음
            Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 선공 중에는 행동력 미지급
            Assert.IsNotNull(RelicRunManager.Instance.Inventory.FindEffect(RelicEffectType.OpeningCardDiscount)); // 첫 아군 턴까지 파편 보존
            var slowed = fight.Enemies.Single(enemy => enemy.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown) != null); // 유일한 약화 대상 확인
            Assert.AreEqual(3, fight.Actions.PlannedActions.Single(action => action.Actor == slowed).ActionSpeed); // 선제 속도에 감소 적용
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 첫 아군 턴 진입
            Assert.AreEqual(3, fight.Turn.LastDrawnCardCount); // 펜타클 조건으로 한 장 추가
            Assert.AreEqual(7, fight.Points.CurrentActionPoints); // 검 조건으로 행동력 증가
            Assert.IsTrue(fight.Deck.Cards.All(card => card.ApCost == 1)); // 첫 턴 고정 비용 감소
            Assert.IsNull(RelicRunManager.Instance.Inventory.FindEffect(RelicEffectType.OpeningCardDiscount)); // 파편 실제 소모 확인
            fight.Actions.PrepareActions(); // 두 번째 적 행동 예고 작성
            Assert.AreEqual(3, fight.Actions.PlannedActions.Single(action => action.Actor == slowed).ActionSpeed); // 두 번째 적 행동까지 약화 유지
            Assert.IsTrue(fight.Turn.EndPlayerTurn() && fight.Turn.CompleteEnemyTurn()); // 다음 아군 턴 전환
            Assert.AreEqual(6, fight.Points.MaxActionPoints); // 추가 행동력 상한 회수
            Assert.IsTrue(fight.Deck.Cards.All(card => card.ApCost == 2)); // 모든 카드 원래 비용 복원
            fight.Actions.PrepareActions(); // 세 번째 적 행동 예고 작성
            Assert.AreEqual(4, fight.Actions.PlannedActions.Single(action => action.Actor == slowed).ActionSpeed); // 두 턴 이후 속도 복원
        } // 선공 전투 검사 종료
        Object.Destroy(sword); // 임시 검 카드 정리
        Object.Destroy(pentacle); // 임시 펜타클 카드 정리
    } // 시작 효과 검사 종료

    [UnityTest] // 실제 제외와 턴 드로우 경계 확인
    public IEnumerator ExclusionAndDiscardRelicsUseTheirOwnEvents() // 일반 버림과 사용 제외 및 손패 휘발 구분
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 보관함 사용
        VerifyDeckRelics(); // 독립 더미와 전투 턴 검증
        yield return new ExitPlayMode(); // 독립 전투 종료
    } // 플레이 검사 종료

    private static void VerifyDeckRelics() // 두 가지 카드 소멸과 버림 임계치 확인
    { // 덱 유물 검사 시작
        Hold("SafetyPinBundle", "RiftSealingWax", "DebrisPouch"); // 세 유물 획득
        var regular = MakeCard(CardType.Cup); // 일반 버림 카드
        var excluded = MakeCard(CardType.Cup, exclude: true); // 사용 후 제외 카드
        var volatileCard = MakeCard(CardType.Cup, volatileInHand: true); // 턴 종료 휘발 카드
        using (var fight = new TestFight(excluded, excluded, volatileCard, volatileCard, regular, regular, regular)) // 세 종류 실제 손패 준비
        { // 제외 검사 시작
            foreach (var card in fight.Deck.Cards.Take(5)) // 최초 손패 카드 위치 고정
            { // 시작 카드 이동 시작
                Assert.IsTrue(fight.Deck.TryMoveDrawTop(card, 7)); // 지정한 카드가 첫 손패에 포함되도록 조정
            } // 시작 카드 이동 종료
            Assert.IsTrue(fight.Turn.StartBattle(5)); // 휘발과 제외가 섞인 시작 손패
            Assert.IsTrue(fight.Deck.DiscardCard(fight.Deck.Hand.Single(card => card.SourceData == regular))); // 일반 버림 실제 실행
            Assert.AreEqual(0, fight.Ally.CurrentBlock); // 일반 버림의 방어도 미지급
            foreach (var card in fight.Deck.Hand.Where(card => card.SourceData == excluded).ToArray()) // 두 번의 사용 제외 수행
            { // 사용 카드 정산 시작
                Assert.IsTrue(fight.Deck.TryPlayAndDraw(card, 0, out _)); // 실제 사용한 카드의 위치 처리
            } // 사용 카드 정산 종료
            Assert.AreEqual(3, fight.Ally.CurrentBlock); // 두 번 제외해도 방어도 한 번 지급
            Assert.AreEqual(2, fight.Deck.ExcludedPile.Count); // 두 사용 카드는 제외 더미로 이동
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 나머지 휘발 카드 둘이 소멸하는 시점
            Assert.AreEqual(4, fight.Deck.ExcludedPile.Count); // 휘발 카드도 전투에서 제외
            Assert.AreEqual(1, fight.Deck.HandCount); // 안전핀의 다음 턴 손패 한 장만 확보
            Assert.AreEqual(3, fight.Ally.CurrentBlock); // 같은 턴의 추가 제외로 중복 지급 방지
            Assert.IsTrue(fight.Deck.IsStateValid()); // 네 종류 더미의 카드 보존 확인
        } // 제외 검사 종료
        foreach (int discarded in new[] { 9, 10 }) // 보너스 경계 전후 비교
        { // 임계치 비교 시작
            using var fight = new TestFight(Enumerable.Repeat(regular, 13).ToArray()); // 순환 가능한 덱 준비
            Assert.IsTrue(fight.Turn.StartBattle(discarded)); // 종료 시 필요한 버림 수의 손패
            Assert.IsTrue(fight.Turn.EndPlayerTurn() && fight.Turn.CompleteEnemyTurn()); // 버림 더미 재사용이 필요한 다음 턴
            Assert.AreEqual(discarded == 10 ? 5 : 4, fight.Turn.LastDrawnCardCount); // 버림 더미 10장부터 추가 드로우
        } // 임계치 비교 종료
        Object.Destroy(regular); // 일반 검사 카드 정리
        Object.Destroy(excluded); // 제외 검사 카드 정리
        Object.Destroy(volatileCard); // 휘발 검사 카드 정리
    } // 덱 유물 검사 종료

    [UnityTest] // 첫 진열의 성공과 미발동 및 복원 검증
    public IEnumerator SafeKeyRevealsOneStableShopOffer() // 첫 방문 결과가 재방문과 탐사 저장에 유지
    { // 상점 검사 시작
        yield return new EnterPlayMode(); // 유물과 상점 관리자 준비
        VerifyShop(); // 플레이 모드 안에서 상점 상태 검증
        yield return new ExitPlayMode(); // 상점 검사 종료
    } // 첫 진열 검사 종료

    private static void VerifyShop() // 첫 진열 및 복원 결과 확인
    { // 상점 확인 시작
        var shop = ShopRunManager.EnsureInstance(); // 실제 상점 관리자
        var offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 판매 목록 원본
        var owner = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 덱 소유자
        var testCard = MakeCard(CardType.Cup); // 상점 준비용 시작 카드
        var source = DeckData.CreateRuntime(new[] { new DeckCardEntry(testCard, owner) }); // 검사 덱 준비
        var key = Resources.Load<RelicData>("Relics/SmallSafeKey"); // 실제 열쇠 원본
        string[] rooms = Enumerable.Range(0, 200).Select(index => "day234_shop_" + index).ToArray(); // 성공과 실패 판정 방 목록
        var roll = typeof(RelicShopPricing).GetMethod("CalculateRange", Hidden); // 실제 방별 판정 함수
        int Roll(string room) => (int)roll.Invoke(null, new object[] { 0, 99, room, "EXTRA_RELIC_CHANCE" }); // 방마다 고정된 확률 값
        string success = rooms.First(room => Roll(room) < key.TriggerChancePercent); // 성공 판정 방
        string failure = rooms.First(room => Roll(room) >= key.TriggerChancePercent); // 확률 실패 방
        Assert.IsTrue(shop.Prepare(offers, source)); // 실제 진열 준비
        shop.BeginRoomVisit(success); // 열쇠를 얻기 전 상점 방문
        var original = Stock(shop); // 열쇠가 없을 때의 실제 상점 목록
        string reservedId = (string)ReserveField(shop, success, "offerId"); // 실제 보류 상품 식별자
        var reserved = offers.Offers.Single(offer => offer.OfferId == reservedId); // 실제 보류 상품
        CollectionAssert.DoesNotContain(original, reserved); // 열쇠 없는 첫 진열에서 보류 상품 숨김
        Assert.AreEqual(ShopPurchaseResult.Unavailable, shop.TryPurchase(reserved)); // 숨은 상품의 직접 구매 차단
        Hold("SmallSafeKey"); // 실제 열쇠 획득
        shop.BeginRoomVisit(failure); // 열쇠가 있어도 확률이 실패할 방 방문
        Assert.IsFalse(IsRevealed(shop, failure)); // 실패한 방의 추가 진열 없음
        shop.BeginRoomVisit(success); // 열쇠를 얻은 뒤 처음 상점 재방문
        Assert.AreEqual(ShopPurchaseResult.Unavailable, shop.TryPurchase(reserved)); // 첫 방문 결과 유지
        shop.ResetSession(); // 새 탐사 회차로 검증 시작
        Assert.IsTrue(shop.Prepare(offers, source)); // 상점 목록 다시 구성
        shop.BeginRoomVisit(success); // 보유 상태에서 확률이 성공할 방 첫 방문
        Assert.IsTrue(IsRevealed(shop, success)); // 성공 방의 추가 진열 확인
        string revealedId = (string)ReserveField(shop, success, "offerId"); // 추가 진열 원본 식별자
        Assert.AreEqual(1, Stock(shop).Count(offer => offer.OfferId == revealedId)); // 추가 상품 하나만 표시
        var snapshotType = typeof(ExplorationSaveService).GetNestedType("RunSnapshot", BindingFlags.NonPublic); // 실제 저장 형식 조회
        var snapshot = System.Activator.CreateInstance(snapshotType, true); // 탐사 저장 구조 생성
        var field = snapshotType.GetField("shopRelicReserves"); // 실제 진열 저장 필드
        field.SetValue(snapshot, ExportReserves(shop)); // 실제 저장에 쓰는 결과 연결
        var restored = JsonUtility.FromJson(JsonUtility.ToJson(snapshot), snapshotType); // 저장 직렬화와 역직렬화
        Assert.IsTrue(RelicRunManager.Instance.TryRemove(key.RelicId)); // 복원 시 현재 열쇠 유무가 달라진 경우
        typeof(ShopRunManager).GetMethod("RestoreSessionWithRelicReserves", Hidden).Invoke(shop, new object[] { offers, source, success, System.Array.Empty<string>(), field.GetValue(restored) }); // 저장된 진열을 실제 상점에 복원
        Assert.AreEqual(1, Stock(shop).Count(offer => offer.OfferId == revealedId)); // 열쇠 없이도 확정한 진열 유지
        Object.Destroy(source); // 임시 덱 정리
        Object.Destroy(testCard); // 임시 카드 정리
    } // 상점 확인 종료

    private static System.Array ExportReserves(ShopRunManager shop) // 상점의 실제 저장 데이터 조회
    { // 저장 결과 조회 시작
        return (System.Array)typeof(ShopRunManager).GetMethod("ExportRelicReserves", Hidden).Invoke(shop, null); // 기존 저장과 같은 진입점 호출
    } // 저장 결과 조회 종료

    private static object ReserveField(ShopRunManager shop, string room, string field) // 지정 방의 확정 진열 값 조회
    { // 방별 값 조회 시작
        var entry = ExportReserves(shop).Cast<object>().Single(value => (string)value.GetType().GetField("roomId").GetValue(value) == room); // 해당 방의 실제 진열 결과
        return entry.GetType().GetField(field).GetValue(entry); // 진열 거래 식별자 또는 판정 확인
    } // 방별 값 조회 종료

    private static bool IsRevealed(ShopRunManager shop, string room) => (bool)ReserveField(shop, room, "revealed"); // 추가 진열 플래그 조회

    private static ShopOfferData[] Stock(ShopRunManager shop) // 실제 화면의 구매 가능 상품 조회
    { // 상품 수집 시작
        object[] values = { null, null }; // 판매 가능 및 품절 반환 공간
        typeof(ShopRunManager).GetMethod("CollectStock", Hidden).Invoke(shop, values); // 실제 화면과 동일한 목록 생성
        return ((System.Collections.Generic.List<ShopOfferData>)values[0]).ToArray(); // 판매 가능 상품 반환
    } // 상품 수집 종료

    private static void Hold(params string[] files) // 실제 유물 원본 획득
    { // 유물 획득 시작
        var manager = RelicRunManager.EnsureInstance(); // 현재 격리 전투 유물 보관함
        foreach (var file in files) // 검증에 필요한 유물 순회
        { // 각 유물 획득 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(Resources.Load<RelicData>("Relics/" + file)), file); // 실제 유물 획득 확인
        } // 각 유물 획득 종료
    } // 유물 획득 종료

    private static CardData MakeCard(CardType type, bool exclude = false, bool volatileInHand = false) // 새 키워드를 가진 검사 카드 구성
    { // 카드 생성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 원본 카드와 같은 직렬화 타입
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"DAY234_CHECK\",\"cardType\":{(int)type},\"apCost\":2,\"excludeOnUse\":{exclude.ToString().ToLowerInvariant()},\"volatileInHand\":{volatileInHand.ToString().ToLowerInvariant()}}}", card); // 실제 전투 카드 규칙 주입
        return card; // 실제 덱에서 쓸 카드 반환
    } // 카드 생성 종료

    private sealed class TestFight : System.IDisposable // 실제 덱과 턴 및 상태를 묶은 검사 환경
    { // 검사 전투 시작
        private readonly DeckData source; // 검사 덱 원본
        private readonly EnemyData enemySource; // 고정 속도의 검사 적 원본
        private readonly BattleStatusEffectController statuses; // 턴별 상태 감소 처리기
        internal readonly BattleDeckRuntime Deck; // 실제 더미 관리자
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(6); // 실제 공용 행동력 관리
        internal readonly BattleTurnRuntime Turn; // 실제 턴 진행 관리
        internal readonly BattleUnitRuntime Ally; // 검사 카드 소유자
        internal readonly BattleUnitRuntime[] Enemies; // 서로 다른 위치의 생존 적
        internal readonly BattleEnemyActionRuntime Actions; // 실제 속도와 예고 생성

        internal TestFight(params CardData[] cards) // 지정 카드들로 독립 전투 준비
        { // 검사 전투 구성 시작
            var owner = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 플레이 캐릭터 원본
            Ally = BattleUnitRuntime.CreateAlly(owner); // 살아 있는 카드 소유자
            enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 약화 전 기본 속도 검사 적
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":120,\"minimumActionSpeed\":4,\"maximumActionSpeed\":4}", enemySource); // 난수와 상관없는 기본 속도
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemySource), BattleUnitRuntime.CreateEnemy(enemySource) }; // 실제 후보 두 마리
            source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, owner)).ToArray()); // 실제 카드 소유자 연결
            var allies = new[] { Ally }; // 전투 파티 구성
            Deck = BattleDeckRuntime.Create(source, allies, 20, 234); // 고정 순서의 실제 카드 더미
            Turn = new BattleTurnRuntime(Deck, Points, allies, Enemies, 4, BattleType.Normal); // 정상 턴과 드로우 연결
            statuses = new BattleStatusEffectController(Turn, allies, Enemies, new BattleStatusEffectProcessor()); // 첫 턴 및 지속 시간 연결
            Actions = new BattleEnemyActionRuntime(Enemies, allies); // 실제 적의 행동 속도 연결
        } // 검사 전투 구성 종료

        public void Dispose() // 현재 검사 전투 정리
        { // 검사 객체 정리 시작
            Actions.Dispose(); // 적 행동 연결 해제
            statuses.Dispose(); // 상태 처리 연결 해제
            Turn.Dispose(); // 덱과 턴 이벤트 연결 해제
            Object.Destroy(source); // 임시 덱 원본 제거
            Object.Destroy(enemySource); // 임시 적 원본 제거
        } // 검사 객체 정리 종료
    } // 검사 전투 종료
} // 검사 클래스 종료
