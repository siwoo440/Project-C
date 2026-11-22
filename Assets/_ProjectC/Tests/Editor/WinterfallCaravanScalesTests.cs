using System; // 독립 저장 파일의 임시 이름 생성
using System.Collections; // 실제 Unity 환경 전환
using System.IO; // 저장과 구버전 필드 제거 검증
using System.Linq; // 실제 상점 상품과 출전 카드 검색
using NUnit.Framework; // 거래와 드로우 및 구형 저장 단언
using UnityEngine; // 실제 리소스 및 월드 좌표 소스
using UnityEngine.TestTools; // 저장과 거래가 가능한 실제 플레이 모드 진입
using Object = UnityEngine.Object; // 이번 검사에서 만든 원본 덱 해제

public sealed class WinterfallCaravanScalesTests // 상단 저울의 실제 구입 보상과 다음 전투의 드로우 검사
{ // 상점과 탐사의 교차 규칙 검사 시작
    [UnityTest] // 실제 구매 성공과 표시 및 실패와 판매 검증
    public IEnumerator OnlySuccessfulRelicPurchasesWithScalesAlreadyOwnedEarnOpeningDraws() // 기본 상점 구매의 실제 보상과 원래 재고 비교
    { // 상점 거래 회귀 검사 시작
        yield return new EnterPlayMode(); // 회차와 상점의 실제 원본 메타를 쓸 수 있는 모드 진입
        VerifyPurchases(); // 저울 자체와 실패와 재판매 및 일반 상품의 무보상 검사
        yield return new ExitPlayMode(); // 원래 테스트 환경 복귀
    } // 상점 거래 회귀 검사 종료

    [UnityTest] // 적 선제와 최대 손패 및 카드 부족의 실제 드로우 검사
    public IEnumerator OpeningDrainsOnlyCardsActuallyDeliveredAndCarriesTheRemainderAcrossBattles() // 첫 정상 지급과 다른 전투로 이어지는 남은 보상 확인
    { // 실제 탐사 교전의 지불 검사 시작
        yield return new EnterPlayMode(); // 탐사 조우와 턴 드로우를 실제 사용하는 모드 진입
        VerifyDraws(); // 정확한 생존 진영 턴과 손패 품절의 차감 결과 확인
        yield return new ExitPlayMode(); // 실제 두 세션 검사 뒤 편집 모드 복귀
    } // 실제 탐사 교전의 지불 검사 종료

    [UnityTest] // 실제 저장 파일의 숫자와 구버전 형식을 검사
    public IEnumerator EarnedDrawsSurviveSaveAndRelicRemovalAndOldSavesStartAtZero() // 보유와 독립된 예약 수치의 왕복과 새 회차 기본값 검증
    { // 실제 파티 저장의 흡수 경계 검사 시작
        yield return new EnterPlayMode(); // 원본 저장 카탈로그와 탐사 세션을 사용할 준비
        VerifyPersistence(); // 실제 저장 스키마와 이전 버전 기본값 확인
        yield return new ExitPlayMode(); // 파일 해제 뒤 정상 플레이 모드 종료
    } // 실제 파티 저장의 흡수 경계 검사 종료

    private static void VerifyPurchases() // 실제 방의 상점 거래와 메시지를 통해 보상 조건 확인
    { // 네 가지 실제 거래 분류 검사 시작
        var shop = PrepareShop(out var session); // 실제 파티와 초기 탐사 및 진열표 준비
        var wallet = PlayerResourceManager.EnsureInstance(); // 실제 지갑 재화의 부족 및 구매 비교
        shop.BeginRoomVisit("day270-caravan-shop"); // 방별 원래 거래 서비스의 정상 방문 시작
        var scales = FindScales(shop); // 게임 상점에서 거래 가능한 실제 저울 상품
        var other = OtherRelicOffers(shop).Take(2).ToArray(); // 저울 이외에 별도로 팔고 살 실제 두 유물 원본
        Assert.AreEqual(2, other.Length); // 각 실제 구매를 대조할 독립 원본 확보 확인
        wallet.RestoreResources(3000, 0, 0, 0); // 초기 저울을 구입할 넉넉한 실제 탐사 골드
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(scales)); // 기존 진열표의 저울 자체 구입 실행
        Assert.Zero(session.PendingCaravanDraws); // 획득 전 보유가 없던 같은 저울 구매는 예약하지 않음 확인
        wallet.RestoreResources(0, 0, 0, 0); // 골드 부족으로 같은 원본의 실제 저렴한 시도 차단
        Assert.AreEqual(ShopPurchaseResult.InsufficientGold, shop.TryPurchase(other[0])); // 기존 거래 서비스가 거절한 실제 실패
        Assert.Zero(session.PendingCaravanDraws); // 실패 구매에는 드로우가 없음을 확인
        wallet.RestoreResources(4000, 0, 0, 0); // 두 실제 상품을 구입할 충분한 골드 복구
        string purchaseMessage = null; // 실제 거래 완료 그래픽에 전하는 피드백 문구 보관
        shop.TransactionCompleted += (_, message) => purchaseMessage = message; // 저장과 안내가 받는 동일한 거래 완료 메시지 연결
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(other[0])); // 이미 저울 보유 중 첫 다른 유물 실제 구입
        Assert.AreEqual(1, session.PendingCaravanDraws); // 첫 다른 구매 보상 한 장 기록
        StringAssert.Contains("다음 전투 첫 손패", purchaseMessage); // 거래 완료 화면의 실제 예약 공지 확인
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(other[1])); // 이미 저울 보유 중 두 번째 다른 원본 실제 구매
        Assert.AreEqual(2, session.PendingCaravanDraws); // 별도 실제 유물 구매마다 한 장씩 누적 확인
        Assert.AreEqual(ShopPurchaseResult.AlreadyPurchased, shop.TryPurchase(other[1])); // 같은 방의 원본을 중복 결제하려는 정상 차단
        Assert.AreEqual(2, session.PendingCaravanDraws); // 원래 같은 재고의 반복 시도에 추가 드로우 없음 확인
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(other[0].Relic)); // 실제 판매 가능한 원본의 네이티브 유물 재판매
        Assert.AreEqual(2, session.PendingCaravanDraws); // 판매는 예약을 보태거나 이전 구매 보상을 취소하지 않음을 확인
        var card = shop.Catalog.Offers.First(offer => offer.OfferType == ShopOfferType.Card && offer.RequiresOwnerChoice && !offer.RewardOnly); // 일반 판매 공용 카드 원본
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(card, PartyLoadoutRuntime.Active.Party.Members[0])); // 같은 상점의 유물이 아닌 일반 카드 실제 구매
        Assert.AreEqual(2, session.PendingCaravanDraws); // 일반 카드 결제는 저울의 유물 보상과 무관함 확인
        var potion = shop.Catalog.Offers.First(offer => offer.OfferType == ShopOfferType.Potion && !offer.RewardOnly); // 일반 소모품 원본 상품
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(potion)); // 같은 실제 거래 서비스의 포션 구매 성공 확인
        Assert.AreEqual(2, session.PendingCaravanDraws); // 포션이 보상 수에 포함되지 않음 확인
    } // 네 가지 실제 거래 분류 검사 종료

    private static void VerifyDraws() // 실제 적과 기본 드로우 수 및 품절 차감 검증
    { // 다음 실제 첫 턴과 남은 드로우 이동 시작
        PrepareShop(out var session); // 실제 탐사 조우가 입장할 유효 회차 및 파티 구성
        Assert.AreEqual(3, session.AddCaravanDraws(3)); // 실제 세 번 구매한 것과 같은 보관 수량 설정
        using (var full = new RunBattle(session, "caravan-full", 1, 4)) // 기본 한 장으로 이미 가득 찬 첫 손패 전투
        { // 상한으로 보상 전체 이월 시작
            Assert.IsTrue(full.Turns.StartBattle(1)); // 아군부터 실제 첫 턴 진행
            Assert.AreEqual(1, full.Deck.HandCount); // 기본 손패 상한에서 보상 카드가 넘치지 않음 확인
            Assert.AreEqual(3, session.PendingCaravanDraws); // 실제로 손패에 도착하지 않은 보상 횟수 유지 확인
            full.CloseEncounter(); // 원래 탐사 승리 결과로 다음 조우 허용
        } // 상한으로 보상 전체 이월 종료
        using (var ambush = new RunBattle(session, "caravan-ambush", 4, 2)) // 적 선제 뒤 정확히 두 실제 카드만 가능한 전투
        { // 적 턴과 드로우 더미 부족 경계 확인 시작
            Assert.IsTrue(ambush.Turns.StartBattle(1, true)); // 적 선제의 실제 적 턴 진입
            Assert.AreEqual(3, session.PendingCaravanDraws); // 첫 적 턴에는 드로우 보상을 미리 차감하지 않음 확인
            Assert.Zero(ambush.Deck.HandCount); // 적 선제 시 첫 손패 지급 없음 확인
            Assert.IsTrue(ambush.Turns.CompleteEnemyTurn()); // 적 선제 뒤 정상 첫 아군 드로우 지급
            Assert.AreEqual(2, ambush.Deck.HandCount); // 원래 한 장과 가능한 보상 한 장만 실제 지급 확인
            Assert.AreEqual(2, session.PendingCaravanDraws); // 실제 도착한 한 장만 차감된 두 장 이월 확인
            Assert.IsTrue(ambush.Turns.EndPlayerTurn()); // 다음 아군 턴에 새 손패 공간을 만드는 실제 진행
            Assert.IsTrue(ambush.Turns.CompleteEnemyTurn()); // 이전 카드가 돌아올 두 번째 아군 턴 진입
            Assert.AreEqual(2, session.PendingCaravanDraws); // 같은 전투의 두 번째 턴에는 예약 소모 없음 확인
            ambush.CloseEncounter(); // 같은 회차의 새 실제 전투 준비
        } // 적 턴과 드로우 더미 부족 경계 확인 종료
        using var next = new RunBattle(session, "caravan-next", 4, 5); // 남은 보상을 모두 받을 다음 실제 전투
        Assert.IsTrue(next.Turns.StartBattle(1)); // 대기 드로우를 낼 다음 정상 아군 턴 시작
        Assert.AreEqual(3, next.Deck.HandCount); // 기본 카드에 남은 두 수량이 모두 손패에 실제로 도착 확인
        Assert.Zero(session.PendingCaravanDraws); // 마지막으로 실제 지급한 보상 두 장의 최종 소진 확인
        next.CloseEncounter(); // 저장할 수 있는 탐사 상태로 전투 종료
    } // 다음 실제 첫 턴과 남은 드로우 이동 종료

    private static void VerifyPersistence() // 실제 원본 세이브의 추가 수치와 이전 파일 형식
    { // 독립 탐사 파일 저장 준비 시작
        PrepareShop(out var session); // 정상 저장 가능한 실제 파티와 탐사 시작
        var scales = Resources.Load<RelicData>("Relics/CaravanOldScales"); // 저장 원본과 같은 실제 상단 저울 자료
        Assert.IsNotNull(scales); // 저장 검사의 실제 구매 보상 유물 존재 확인
        var relicRun = RelicRunManager.EnsureInstance(); // 실제 보유 유물과 예약 수치를 따로 비교할 보관함
        Assert.AreEqual(RelicAcquireResult.Acquired, relicRun.TryAcquire(scales)); // 실제 보유를 시작한 회차 상태 준비
        Assert.AreEqual(2, session.AddCaravanDraws(2)); // 저울의 두 실제 구매를 대조할 유지 보상 설정
        Assert.IsTrue(relicRun.TryRemove(scales.RelicId)); // 실제 소유를 종료하고 이미 얻은 예약 보상 유지 확인
        string directory = Path.Combine(Application.temporaryCachePath, "caravan-scales-" + Guid.NewGuid().ToString("N")); // 이번 검사에만 해당하는 새로운 파일 경로
        string current = Path.Combine(directory, "current.json"); // 실제 현재 필드가 들어갈 탐사 저장본
        string legacy = Path.Combine(directory, "legacy.json"); // 추가 필드가 없는 실제 과거 형태의 사본
        try // 이번 저장 경로의 독립 정리 보장
        { // 추가 필드 저장과 복원 시작
            Assert.IsTrue(ExplorationSaveService.Save(current, Vector3.zero)); // 실제 서비스가 조사한 현재 런 상태 저장
            string json = File.ReadAllText(current); // 원본 저장 형식의 실제 JSON 조회
            StringAssert.Contains("\"pendingCaravanDraws\":2", json); // 실제 예약 값의 선택 저장 필드를 확인
            session.ResetExploration(); // 다음 실제 신규 탐사로 램의 예약 수치 초기화
            Assert.Zero(session.PendingCaravanDraws); // 새로운 회차에서 이전 회차의 보상 소멸 확인
            Assert.IsTrue(ExplorationSaveService.Load(current)); // 같은 회차의 정상 파일 불러오기 확인
            Assert.AreEqual(2, session.PendingCaravanDraws); // 별도 유물 보유가 없어도 실제 보상 저장값 복원 확인
            File.WriteAllText(legacy, json.Replace(",\"pendingCaravanDraws\":2", string.Empty)); // 새 선택 필드가 없던 이전 탐사 파일 형태 재구성
            Assert.IsTrue(ExplorationSaveService.Load(legacy)); // 기존 버전의 원본 형식으로도 정상 복원 확인
            Assert.Zero(session.PendingCaravanDraws); // 이전 파일의 생략 필드가 영으로 초기화되는 호환 확인
        } // 추가 필드 저장과 복원 종료
        finally // 다음 실제 검사 파일에 영향을 주지 않는 정리
        { // 검사 파일 해제 시작
            ExplorationSaveService.Remove(legacy); // 기존 형식으로 만든 독립 검사본 삭제
            ExplorationSaveService.Remove(current); // 새 보상으로 저장 만든 검사용 파일 삭제
            if (Directory.Exists(directory)) // 중간 실패가 있어도 실제로 만든 독립 경로만 확인
            { // 테스트 폴더 삭제 시작
                Directory.Delete(directory); // 다른 임시 파일의 영향을 주지 않는 이번 빈 폴더만 제거
            } // 테스트 폴더 삭제 종료
        } // 검사 파일 해제 종료
    } // 독립 탐사 파일 저장 준비 종료

    private static ShopRunManager PrepareShop(out ExplorationSessionManager session) // 원래 실제 상점과 정상 출전 파티 준비
    { // 규칙 검사의 새로운 탐사 세션 구성 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 현재 출전 가능한 실제 원본 명단
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 파티 선택 조건과 회복 상태 연결
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 상점 카드와 조우에 사용할 두 명의 실제 아군
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 탐사 파티와 카드 소유자 원본 준비
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 실제 상점 카드와 저장에서 쓰는 원본 덱
        var state = BattleResultManager.EnsureInstance(); // 출전 아군과 실제 저장 상태 관리자
        state.ResetSavedPartyState(); // 직전 검사 파티의 기록을 지우는 원래 경로
        Assert.IsTrue(state.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 현재 회차에서 실제 출전자의 원본 상태 등록
        session = ExplorationSessionManager.EnsureInstance(); // 실제 런 상태 관리자 조회
        session.ResetExploration(); // 저울과 예약에 이전 회차의 수치가 섞이지 않도록 초기화
        session.SetCurrentFloorSeed(270); // 일반 게임과 같은 실제 진행 층과 저장 가능 상태
        var shop = ShopRunManager.EnsureInstance(); // 현재 실행하는 실제 상점 관리자
        shop.ResetSession(); // 별도 테스트의 실제 거래 기록 삭제
        Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), PartyLoadoutRuntime.Active.Deck)); // 실제 상점 상품 원본과 회차 덱 연결 확인
        return shop; // 실제 구매와 메시지를 읽을 상점 반환
    } // 규칙 검사의 새로운 탐사 세션 구성 종료

    private static ShopOfferData FindScales(ShopRunManager shop) // 실제 방에 구매 가능한 동일 저울 상품 확인
    { // 저울 재고 검사 시작
        for (int index = 0; index < 40; index++) // 원래 유물 유보가 발생하지 않는 유효한 상점 방의 진열
        { // 새로운 정상 상점 방 검사 시작
            shop.BeginRoomVisit($"day270-caravan-shop-{index}"); // 실제 방 방문과 상품 준비 실행
            shop.CollectStock(out var available, out _); // 유보 또는 상점 보상 전용을 제외하는 기존 진열 조회
            var scales = available.FirstOrDefault(offer => offer.Relic?.EffectType == RelicEffectType.CaravanScalesShopRelicDraw); // 실제 같은 규칙으로 거래 가능한 저울 원본 확인
            if (scales != null) // 실제 구매할 수 있는 저울 상품이 현재 방에 있는지 확인
            { // 원래 정식 상품의 독립 조회 시작
                return scales; // 포장된 조건이 아닌 실제 해당 방의 진열 상품 반환
            } // 원래 정식 상품의 독립 조회 종료
        } // 새로운 정상 상점 방 검사 종료
        Assert.Fail("상점에서 유효한 상단 저울 구입 원본 누락"); // 실제 게임 메타에 이어지지 않은 회귀 오류
        return null; // 검사 실패 이후의 형식상 반환
    } // 저울 재고 검사 종료

    private static ShopOfferData[] OtherRelicOffers(ShopRunManager shop) // 판매 가능한 다른 실제 유물의 대조품 확보
    { // 현재 방의 유물 원본 평가 시작
        shop.CollectStock(out var available, out _); // 실제 선반의 유보와 판매 기록을 포함한 원래 목록
        return available.Where(offer => offer.Relic != null && offer.Relic.EffectType != RelicEffectType.CaravanScalesShopRelicDraw && // 같은 효과 원본 제외와 정상 유물 진열 확인
            shop.GetOfferPrice(offer) > 0 && shop.GetRelicSellPrice(offer.Relic) >= 0).ToArray(); // 부족한 골드와 실제 재판매 대조가 가능한 기존 유물만 선택
    } // 현재 방의 유물 원본 평가 종료

    private sealed class RunBattle : IDisposable // 일반 탐사 교전의 실제 손패와 시작 턴 검사 환경
    { // 독립 탐사 교전 환경 시작
        private readonly ExplorationSessionManager session; // 현재 전투에 네이티브로 연결하는 같은 실제 회차
        private readonly DeckData source; // 손패 크기와 보상 차감을 위한 실제 카드 원본 덱
        private readonly BattleUnitRuntime[] allies; // 일차의 전투 결과가 요구하는 유효한 아군 목록
        private readonly BattleUnitRuntime enemy; // 실제 조우 종료를 만들 원래 적 목록
        internal readonly BattleDeckRuntime Deck; // 손패에 실제로 드로우한 카드 이동 확인
        internal readonly BattleTurnRuntime Turns; // 선제와 실제 최초 아군 턴의 기본 흐름

        internal RunBattle(ExplorationSessionManager run, string identity, int capacity, int cards) // 다른 실제 손패 상한과 카드 수의 전투 원본 구성
        { // 현재 탐사 조우 연결 시작
            session = run; // 같은 전투를 실행하는 실제 현재 세션 저장
            var encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 일반 조우와 적 목록 조회
            Assert.IsTrue(session.BeginEncounter(identity, encounter, Vector3.zero, Vector3.up)); // 실제 탐사 접촉 경로의 신규 교전 토큰 등록
            var character = PartyLoadoutRuntime.Active.Party.Members[0]; // 실제 현재 출전한 생존 아군 소유자 원본
            allies = new[] { BattleUnitRuntime.CreateAlly(character) }; // 테스트 전투에서 기본 체력을 유지하는 실제 생존 아군
            enemy = BattleUnitRuntime.CreateEnemy(encounter.Enemies[0]); // 일반 조우의 실제 대조 적 생성
            var thrust = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards.Single(card => card.CardId == "CRD_SHEET_THRUST"); // 기존 실제 드로우 카드의 확정 원본
            source = DeckData.CreateRuntime(Enumerable.Range(0, cards).Select(_ => new DeckCardEntry(thrust, character)).ToArray()); // 실제 개별 손패와 원본 저장 위치를 가진 검사 덱
            Deck = BattleDeckRuntime.Create(source, allies, capacity, 270); // 실제 전투 손패 제한과 카드 순서 유지
            Turns = new BattleTurnRuntime(Deck, new BattleActionPointRuntime(3), allies, new[] { enemy }, 1, encounter.BattleType, session); // 조우 소유를 확인하는 실제 드로우 흐름 구성
        } // 현재 탐사 조우 연결 종료

        internal void CloseEncounter() // 일반 결과와 탐사 귀환을 모두 실제로 마치기
        { // 다음 독립 조우 반환 준비 시작
            enemy.TakeDamage(10000, BattleDamageType.None, allies[0]); // 원래 적의 실제 처치로 정상 전투 승리 생성
            Assert.AreEqual(BattleResult.Victory, Turns.Result); // 실제 전투가 정상 승리한 뒤 저장 가능 확인
            session.ResolveBattleResult(new BattleResultData(Turns.Result, BattleType.Normal, Turns.CurrentRound, allies, new[] { enemy.UnitId })); // 같은 탐사에서 다음 다른 조우를 시작하도록 원래 결과 연결
        } // 다음 독립 조우 반환 준비 종료

        public void Dispose() // 현재 독립 전투와 실제 생성 덱만 해제
        { // 실제 전투 종료 정리 시작
            Turns.Dispose(); // 조우의 전투 수명과 이벤트 정리
            Object.Destroy(source); // 이 테스트에서 만든 원본 덱만 정리
        } // 실제 전투 종료 정리 종료
    } // 독립 탐사 교전 환경 종료
} // 상점과 탐사의 교차 규칙 검사 종료
