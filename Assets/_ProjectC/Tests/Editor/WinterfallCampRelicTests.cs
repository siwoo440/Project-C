using System; // 저장 경로 식별자 생성
using System.Collections; // 실제 씬과 전투 프레임 대기
using System.IO; // 독립 임시 저장 파일 정리
using System.Linq; // 실제 원본과 적 행동 조회
using NUnit.Framework; // 유물 결과 검증
using UnityEngine; // 실제 게임 원본과 회차 관리자 사용
using UnityEngine.SceneManagement; // 실제 시작 씬 이동
using UnityEngine.UI; // 실제 카드 제거 서비스 버튼 조작
using UnityEngine.TestTools; // 전투와 상점의 플레이 모드 검증
using Object = UnityEngine.Object; // 임시 게임 객체 정리

public sealed class WinterfallCampRelicTests // 캠프와 상점 및 직접 피격 유물 네 종 검사
{ // 유물 검사 시작
    private static RelicData Relic(string name) // 보물과 상점에서 쓰는 실제 유물 원본
    { // 원본 조회 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 정식 자산 경로에서 원본 조회
        Assert.IsNotNull(relic, name); // 실물 유물 존재 확인
        return relic; // 실제 등록 유물 반환
    } // 원본 조회 종료
    [TestCase("EmergencyRationPack", 11)] // 캠프 회복량 증가 원본
    [TestCase("GoldenExchangeVoucher", 13)] // 소모성 카드 제거 원본
    [TestCase("RedRemnantCharm", 14)] // 생존 아군 정신력 증가 원본
    [TestCase("FadedDogTag", 15)] // 파티 첫 체력 피해 무효 원본
    public void OriginalsAreRegisteredForTradingSavingAndDisplay(string name, int row) // 시트와 상점 및 저장 원본 등록 확인
    { // 원본 검사 시작
        var relic = Relic(name); // 실제 게임에 쓰는 유물 조회
        var sheet = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 원문 시트의 같은 유물 이름 검색
        Assert.AreEqual(row, sheet.SourceRow); // 원문 행 연결 확인
        Assert.IsNotNull(relic.Icon); // 도감에서 사용할 실제 원화 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics, relic); // 저장과 복원에 같은 원본 사용
        Assert.IsTrue(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Any(offer => offer != null && offer.Relic == relic)); // 정상 상점 보상 원본 연결 확인
    } // 원본 검사 종료
    [Test] // 전투 시작 부적 횟수와 대상 확인
    public void CharmGivesTwoMentalToEachLivingAllyOnce() // 생존 아군별 이 점과 전투당 한 번 검증
    { // 정신력 검사 시작
        var members = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters; // 실제 출전 명단
        var allies = members.Take(3).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 아군 세 명 생성
        allies[0].ApplyPersistentMental(50); // 일반 정신력의 생존자 설정
        allies[1].ApplyPersistentMental(99); // 최대치 근처의 생존자 설정
        allies[2].ApplyPersistentMental(27); // 쓰러진 아군의 기준 정신력 설정
        allies[2].ApplyPersistentHealth(0); // 사망한 아군 설정
        var wallet = new RelicGoldRuntime(); // 독립 유물 지갑 생성
        var inventory = new RelicInventoryRuntime(wallet); // 독립 보관함 생성
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(Relic("RedRemnantCharm"))); // 실제 부적 획득
        using (var events = new BattleEventDispatcher()) // 정식 전투 이벤트 발행기 생성
        using (var effects = new BattleRelicEffectController(events, inventory, wallet, allies, Array.Empty<BattleUnitRuntime>())) // 실제 전투 유물 처리기 연결
        { // 전투 시작 검증
            effects.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 정상 전투 시작 처리
            effects.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 중복 시작 신호 재현
            CollectionAssert.AreEqual(new[] { 52, 100, 27 }, allies.Select(unit => unit.CurrentMental)); // 생존자만 각각 이 점과 상한 및 한 번 발동 확인
        } // 전투 시작 검증 종료
    } // 정신력 검사 종료
    [UnityTest] // 실제 예정 공격과 손실 경계 확인
    public IEnumerator DogTagStopsFirstDirectHealthLossOncePerPartyPerBattle() // 직접 공격과 지속 피해 및 대가 구분 확인
    { // 실전 피격 검사 시작
        yield return new EnterPlayMode(); // 실제 회차 유물 환경 진입
        CheckGuard(); // 전투당 한 번과 피해 경계 확인
        yield return new ExitPlayMode(); // 검사 전투 종료
    } // 실전 피격 검사 종료
    private static void CheckGuard() // 실제 적 행동 준비와 명중 및 방어 계산 확인
    { // 군번줄 검사 시작
        var manager = RelicRunManager.EnsureInstance(); // 실제 회차 보관함 준비
        var tag = Relic("FadedDogTag"); // 군번줄 정식 원본 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(tag)); // 실제 군번줄 획득
        var data = ScriptableObject.CreateInstance<EnemyData>(); // 매번 정상 공격하는 비교 적 생성
        var foe = BattleUnitRuntime.CreateEnemy(data); // 실제 적 전투 상태 생성
        var members = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters; // 실제 파티원 조회
        var allies = members.Take(2).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 보호를 공유하는 두 아군 생성
        try // 검사 자산 정리 보장
        { // 전투 행동 검사 시작
            using (var enemy = new BattleEnemyActionRuntime(new[] { foe }, allies)) // 하나의 전투가 공유하는 예정 행동 관리자
            { // 최초 전투 검사 시작
                var hit = NextAttack(enemy); // 첫 적 직접 공격 준비
                int baseHealth = allies[0].CurrentHealth; // 예상 피해 무소모 확인용 체력 보관
                Assert.Greater(hit.PreviewDamage().AppliedDamage, 0); // 공격 예고가 유물 횟수를 쓰지 않는지 확인
                allies[0].GainBlock(100); // 첫 공격 전액 흡수 준비
                Assert.AreEqual(0, enemy.ExecuteAction(hit).DamageResult.AppliedDamage); // 방어도 전액 흡수
                allies[0].ClearBlock(); // 남은 방어도 정리
                allies[0].ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1); // 기존 지속 피해 연결
                new BattleStatusEffectProcessor().ProcessPhaseEnd(allies[0], 1); // 실제 화상 피해 처리
                Assert.IsTrue(allies[0].TrySpendHealth(3)); // 체력 비용도 정상 지불
                Assert.AreEqual(baseHealth - 5, allies[0].CurrentHealth); // 지속 피해와 체력 비용에는 군번줄이 동작하지 않음 확인
                hit = NextAttack(enemy); // 두 번째 직접 공격 준비
                allies[0].GainBlock(1); // 일부만 방어도로 흡수하는 조건
                var stopped = enemy.ExecuteAction(hit).DamageResult; // 실제 명중과 최초 손실 무효 처리
                Assert.AreEqual(1, stopped.AbsorbedDamage); // 소모한 방어도 유지
                Assert.AreEqual(0, stopped.AppliedDamage); // 처음 잃을 체력만 무효
                Assert.AreEqual(baseHealth - 5, allies[0].CurrentHealth); // 무효화된 공격의 체력 보존
                hit = NextAttack(enemy); // 같은 전투의 다른 공격 준비
                Assert.IsTrue(enemy.TryRedirectAttack(foe, allies[1])); // 대상 변경으로 다른 아군 피격
                Assert.Greater(enemy.ExecuteAction(hit).DamageResult.AppliedDamage, 0); // 파티원별 추가 횟수가 생기지 않음 확인
            } // 최초 전투 검사 종료
            using (var next = new BattleEnemyActionRuntime(new[] { foe }, allies)) // 새 전투의 별도 적 행동 관리자
            { // 다음 전투 검사 시작
                Assert.AreEqual(0, next.ExecuteAction(NextAttack(next)).DamageResult.AppliedDamage); // 다음 전투에서 한 번 다시 보호
            } // 다음 전투 검사 종료
            Assert.IsTrue(manager.TryRemove(tag.RelicId)); // 판매와 같은 보관함 제거
            using (var sold = new BattleEnemyActionRuntime(new[] { foe }, allies)) // 군번줄 미보유 전투
            { // 미보유 대조 시작
                Assert.Greater(sold.ExecuteAction(NextAttack(sold)).DamageResult.AppliedDamage, 0); // 미보유 시 정상 첫 피해 확인
            } // 미보유 대조 종료
        } // 전투 행동 검사 종료
        finally // 검사 원본 정리
        { // 정리 시작
            Object.Destroy(data); // 임시 적 원본 제거
        } // 정리 종료
    } // 군번줄 검사 종료
    private static BattleEnemyAction NextAttack(BattleEnemyActionRuntime enemy) // 실제 AI의 다음 공격 한 개 준비
    { // 예정 공격 조회 시작
        Assert.AreEqual(1, enemy.PrepareActions()); // 생존 적의 정상 공격 생성
        return enemy.PlannedActions.Single(); // 실제 예정 행동 반환
    } // 예정 공격 조회 종료
    [UnityTest] // 회차 원본 소유와 캠프 및 교환권 저장 검사
    public IEnumerator CampAndVoucherSurviveSaveUntilConsumedOrSold() // 실제 휴식 회복과 무료 제거 및 재방문 검증
    { // 탐사 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 실행 진입
        yield return PlayCampAndShop(); // 런타임 전환 뒤 실제 회차 시나리오 진행
        yield return new ExitPlayMode(); // 검사 회차 종료
    } // 탐사 검사 종료
    private static IEnumerator PlayCampAndShop() // 실제 파티와 상점 및 저장 복원 검사
    { // 탐사 흐름 시작
        SceneManager.LoadScene("00_Boot"); // 독립 게임 시작 씬 진입
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null); // 실제 메뉴 준비 대기
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
        yield return WaitFor(() => Object.FindFirstObjectByType<LobbyLayoutView>() != null); // 거점 준비 대기
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 네 명 파티 출전
        yield return WaitFor(() => Object.FindFirstObjectByType<ExplorationPlayerController>() != null && !SceneFlowManager.Instance.IsLoadingScene); // 실제 탐사 주인공 배치 대기
        var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 저장 위치
        var catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 정식 상점 목록 조회
        var shop = ShopRunManager.EnsureInstance(); // 실제 회차 상점 준비
        Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 파티 덱과 상점 연결
        shop.BeginRoomVisit("winterfall_camp_relics"); // 독립 상점 방문
        var pack = Relic("EmergencyRationPack"); // 실제 식량팩 조회
        var voucher = Relic("GoldenExchangeVoucher"); // 실제 교환권 조회
        var relics = RelicRunManager.EnsureInstance(); // 현재 회차 보관함 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(pack)); // 회차에 식량팩 획득
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(voucher)); // 회차에 교환권 획득
        var result = BattleResultManager.EnsureInstance(); // 출전 파티 영속 상태 조회
        var members = result.ActiveParty.Members.ToArray(); // 실제 출전자 원본 보관
        result.RestoreSavedAllyState(members[0], 1, 30, 0); // 회복량과 내림 비교용 생존자
        result.RestoreSavedAllyState(members[1], members[1].MaxHealth - 1, 98, 0); // 최대 체력과 정신력 상한 비교 대상
        result.RestoreSavedAllyState(members[2], 0, 12, 1); // 부활 금지 확인 대상
        var room = RestRoomRunManager.EnsureInstance(); // 실제 휴식 방 관리자 준비
        room.Prepare(PartyLoadoutRuntime.Active.Deck); // 출전 덱의 실제 강화 가능 카드 연결
        Assert.IsTrue(room.TryUseRest(0, 1, out var note), note); // 위험도 일 휴식과 카드 강화 실행
        StringAssert.Contains("+30%", note); // 실제 사용 결과의 식량팩 추가 회복 표시
        int ordinary = RestRoomRecoveryService.CalculateRecoveredHealth(1, members[0].MaxHealth, 1) - 1; // 미보유 상태의 원래 정수 회복량
        Assert.IsTrue(result.TryGetSavedAllyState(members[0], out int hp, out int mental, out _)); // 회복 뒤 실제 영속 상태 조회
        Assert.AreEqual(1 + ordinary * 130 / 100, hp); // 개인별 원래 회복량에서 삼십 퍼센트 추가 후 내림
        Assert.AreEqual(45, mental); // 정신력 회복에 식량팩 증가 미적용
        Assert.IsTrue(result.TryGetSavedAllyState(members[1], out hp, out mental, out _)); // 최대치 근처 동료 결과 조회
        Assert.AreEqual(members[1].MaxHealth, hp); // 최대 체력 상한 확인
        Assert.AreEqual(100, mental); // 원래 정신력 상한 확인
        Assert.IsTrue(result.TryGetSavedAllyState(members[2], out hp, out mental, out _)); // 사망자 결과 조회
        Assert.AreEqual(0, hp); // 휴식 중 부활 차단
        Assert.AreEqual(12, mental); // 사망자의 정신력 보존
        var path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 실제 저장과 분리한 검사 파일
        try // 저장 파일 정리 보장
        { // 거래와 복원 검사 시작
            Assert.AreEqual(0, shop.GetCardRemovalPrice()); // 교환권 보유 시 표시도 무료
            var storeView = Object.FindFirstObjectByType<ShopPrototypeView>(); // 탐사 씬의 실제 상점 화면 조회
            Assert.IsNotNull(storeView); // 카드 제거를 선택할 실제 상점 확인
            Assert.IsTrue(storeView.TryOpenFromRoom("winterfall_voucher_cancel")); // 교환권 보유 상태로 서비스 목록 열기
            var service = storeView.GetComponentsInChildren<Button>().Single(button => button.name == "CardRemovalService"); // 실제 카드 제거 서비스 칸 조회
            service.onClick.Invoke(); // 제거할 카드를 고르는 목록 열기
            storeView.GetComponentsInChildren<Button>().Single(button => button.name == "BackToProducts").onClick.Invoke(); // 카드를 제거하지 않고 정상 취소
            storeView.GetComponentsInChildren<Button>().Single(button => button.name == "CloseButton").onClick.Invoke(); // 상점 입력 종료
            Assert.IsTrue(relics.Inventory.ContainsRelic(voucher.RelicId)); // 화면에서 취소한 교환권 유지
            Assert.AreNotEqual(ShopPurchaseResult.Success, shop.TryRemoveCard(-1)); // 잘못된 카드 선택 실패
            Assert.IsTrue(relics.Inventory.ContainsRelic(voucher.RelicId)); // 실패 후 교환권 보존
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 사용하지 않은 쿠폰과 식량팩 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 원본 ID로 실제 회차 복원
            shop = ShopRunManager.EnsureInstance(); // 복원 뒤 실제 상점 조회
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 복원된 출전 덱을 상점에 연결
            shop.BeginRoomVisit("winterfall_after_reload"); // 다른 상점 재방문
            var currency = PlayerResourceManager.EnsureInstance(); // 실제 자원 지갑 조회
            currency.RestoreResources(0, 0, 0, 0); // 무일푼에서도 가능한 제거 서비스 준비
            var deck = shop.RunDeck; // 현재 회차 실제 카드 목록 조회
            while (deck.CardCount < RunDeckManager.MinimumCardsAfterRemoval + 2) // 중복 진입 때도 두 장 제거 가능한 덱 크기 준비
            { // 검사 덱 확장 시작
                Assert.IsTrue(deck.TryAddCard(deck.Cards[0].Card, members[0])); // 제거 가능한 실제 카드 한 장 추가
            } // 검사 덱 확장 종료
            int count = deck.CardCount; // 성공 전 덱 크기 보관
            Assert.AreEqual(0, shop.GetCardRemovalPrice()); // 복원 후 다른 상점에서도 무료 표시
            ShopPurchaseResult nested = ShopPurchaseResult.Success; // 거래 중 중복 제거의 반환 결과 보관
            Action repeat = () => nested = shop.TryRemoveCard(0); // 실제 덱 변경 알림에서 중복 거래 시도
            deck.Changed += repeat; // 덱 변경 중 재진입 조건 연결
            try // 덱 변경 구독 해제 보장
            { // 무료 제거 결제 검사 시작
                Assert.AreEqual(ShopPurchaseResult.Success, shop.TryRemoveCard(0)); // 무일푼 상태에서 실제 제거 한 번 성공
            } // 무료 제거 결제 검사 종료
            finally // 거래 검사 구독 정리
            { // 구독 해제 시작
                deck.Changed -= repeat; // 이후 정상 거래의 입력 복원
            } // 구독 해제 종료
            Assert.AreEqual(ShopPurchaseResult.Unavailable, nested); // 같은 교환권으로 두 장 제거 차단
            Assert.AreEqual(count - 1, deck.CardCount); // 제거 서비스 카드 한 장 감소
            Assert.AreEqual(0, currency.Gold); // 무료 서비스에서 실제 골드 차감 없음
            Assert.IsFalse(relics.Inventory.ContainsRelic(voucher.RelicId)); // 성공한 교환권 한 개 소모
            Assert.AreEqual(catalog.CardRemovalPrice, shop.GetCardRemovalPrice()); // 다음 거래부터 원래 가격 표시
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 소진 상태 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 소진 이후 저장 복원
            Assert.IsFalse(relics.Inventory.ContainsRelic(voucher.RelicId)); // 이미 쓴 교환권 복원 방지
            Assert.IsTrue(relics.Inventory.ContainsRelic(pack.RelicId)); // 지속 유물 식량팩은 보유 유지
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 복원 뒤 판매용 상점 준비
            shop.BeginRoomVisit("winterfall_ration_sale"); // 식량팩 판매 상점 방문
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(pack)); // 시트 판매가를 반영한 실제 식량팩 판매
            Assert.AreEqual(0, RestRoomRecoveryService.CampRecoveryBonus); // 판매 후 캠프 회복량 보너스 종료
        } // 거래와 복원 검사 종료
        finally // 독립 저장 파일 정리
        { // 파일 정리 시작
            if (File.Exists(path)) // 현재 검사에서 생성한 파일 확인
            { // 실제 파일 정리 시작
                File.Delete(path); // 검사 전용 임시 파일 삭제
            } // 실제 파일 정리 종료
        } // 파일 정리 종료
    } // 탐사 흐름 종료
    private static IEnumerator WaitFor(Func<bool> ready) // 실제 씬 초기화 완료 대기
    { // 준비 대기 시작
        float deadline = Time.realtimeSinceStartup + 30f; // 초기화 지연 검사 기한
        while (!ready() && Time.realtimeSinceStartup < deadline) // 필요한 게임 객체 준비 확인
        { // 다음 프레임 대기 시작
            yield return null; // 실제 게임 진행 한 프레임 대기
        } // 다음 프레임 대기 종료
        Assert.IsTrue(ready()); // 필요한 씬과 객체의 준비 완료 확인
    } // 준비 대기 종료
} // 유물 검사 종료
