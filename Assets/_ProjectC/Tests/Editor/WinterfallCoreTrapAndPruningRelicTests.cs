using System.Collections; // 플레이 모드 검사 전환
using System.IO; // 독립 탐사 저장 경로
using System.Linq; // 실제 상점 상품 조회
using System.Reflection; // 보상 시드 함수 확인
using NUnit.Framework; // 유물 효과 결과 대조
using TMPro; // 상점의 표시 가격 조회
using UnityEngine; // 실제 탐사와 게임 자료 사용
using UnityEngine.TestTools; // 플레이 모드 검사 등록
using UnityEngine.UI; // 실제 카드 제거 버튼 사용

public sealed class WinterfallCoreTrapAndPruningRelicTests // 침식핵과 함정 및 카드 제거 유물 검사
{ // 탐사 유물 검사 시작
    [UnityTest] // 실제 사건 해결과 영속 체력 검사
    public IEnumerator LanternReducesOnlyTheTwoShardTraps() // 파편 함정과 일반 비용 및 퇴색 피해 대조
    { // 잔광 등불 검사 시작
        RequireIsolatedPlay(); // 검사 전용 저장 환경 확인
        yield return new EnterPlayMode(); // 실제 탐사 관리자 생성 허용
        yield return LanternReducesOnlyTheTwoShardTrapsInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 등불 검사 회차 종료
    } // 잔광 등불 검사 종료

    private IEnumerator LanternReducesOnlyTheTwoShardTrapsInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var session = PrepareExpedition(); // 두 명의 실제 출전 파티 준비
        var party = BattleResultManager.EnsureInstance(); // 탐사의 실제 영속 체력 조회
        var actor = party.ActiveParty.Members[0]; // 선택 결과를 받을 아군 조회
        party.RestoreSavedAllyState(actor, 12, 30, 0); // 미보유 비교 체력 준비
        Assert.IsTrue(AshstormEyeEvent.TryResolve("lantern-eye-base", AshstormEyeEvent.Shatter, actor, null, out _)); // 미보유 상태의 눈 파괴
        Assert.AreEqual(5, Health(party, actor)); // 미보유 상태의 기본 피해 일곱 확인
        Acquire("AfterglowLantern"); // 실제 잔광 등불 보유
        party.RestoreSavedAllyState(actor, 12, 30, 0); // 등불 보유 비교 체력 준비
        Assert.IsTrue(AshstormEyeEvent.TryResolve("lantern-eye-protected", AshstormEyeEvent.Shatter, actor, null, out string eyeSummary)); // 등불 보유 상태의 눈 파괴
        Assert.AreEqual(7, Health(party, actor)); // 눈 파편 피해 다섯 확인
        StringAssert.Contains("12 → 7", eyeSummary); // 실제 경감 피해의 안내 확인
        party.RestoreSavedAllyState(actor, 5, 30, 0); // 기본 유리 피해로 쓰러질 체력 준비
        Assert.IsTrue(GlassFieldEvent.TryResolve("lantern-glass-protected", GlassFieldEvent.BreakGlass, actor, out string glassSummary)); // 실제 유리 파편 선택 실행
        Assert.AreEqual(1, Health(party, actor)); // 유리 파편 피해 네 점과 생존 확인
        StringAssert.Contains("5 → 1", glassSummary); // 실제 생존 체력의 결과 안내 확인
        Assert.IsTrue(session.IsEventResolved("lantern-glass-protected")); // 위험 선택의 완료 기록 확인
        Assert.IsFalse(GlassFieldEvent.TryResolve("lantern-glass-protected", GlassFieldEvent.BreakGlass, actor, out _)); // 완료 사건 반복 피해 차단
        party.RestoreSavedAllyState(actor, 12, 30, 0); // 일반 체력 비용 비교 체력 준비
        Assert.IsTrue(BurntCartEvent.TryResolve("lantern-cart-cost", BurntCartEvent.Salvage, actor, out _)); // 실제 수레 해체 비용 지불
        Assert.AreEqual(9, Health(party, actor)); // 수레 해체 체력 세 점 차감 확인
        party.ApplyExplorationHazardToActiveParty(4, 2); // 별도 퇴색 환경의 체력과 정신력 피해
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out int hp, out int mental, out _)); // 실제 환경 피해 뒤 상태 조회
        Assert.AreEqual(5, hp); // 퇴색 체력 피해 원래 네 점 확인
        Assert.AreEqual(28, mental); // 퇴색 정신력 피해 원래 두 점 확인
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 실제 승리 정산과 저장 검사
    public IEnumerator CoreShardAddsOneDeterministicDropOnlyToCoreVictory() // 실제 침식핵의 추첨 경계와 재지급 차단
    { // 코어 조각 검사 시작
        RequireIsolatedPlay(); // 검사 전용 저장 환경 확인
        yield return new EnterPlayMode(); // 실제 탐사 관리자 생성 허용
        yield return CoreShardAddsOneDeterministicDropOnlyToCoreVictoryInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 코어 조각 검사 회차 종료
    } // 코어 조각 검사 종료

    private IEnumerator CoreShardAddsOneDeterministicDropOnlyToCoreVictoryInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var session = PrepareExpedition(); // 실제 출전 파티와 층 자료 준비
        var core = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 침식핵의 실제 보스 조우 조회
        var normal = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshWanderer"); // 비교할 실제 일반 조우 조회
        var inventory = RelicRunManager.EnsureInstance().Inventory; // 실제 탐사 유물 보관함
        int initialCount = inventory.Count; // 미보유 비교 유물 수 보관
        Resolve(session, RoomWithRoll(19, "no-fragment"), core, BattleResult.Victory); // 미보유 상태의 당첨 시드 승리
        Assert.AreEqual(initialCount, inventory.Count); // 조각 미보유 중 기본 보상만 지급 확인
        var fragment = Acquire("DamagedCoreShard"); // 실제 손상된 코어 조각 획득
        Resolve(session, RoomWithRoll(19, "ordinary"), normal, BattleResult.Victory); // 일반 교전에서 당첨 시드 승리
        Assert.AreEqual(initialCount + 1, inventory.Count); // 비침식핵 조우의 유물 보상 없음 확인
        string room = RoomWithRoll(19, "core"); // 이십 퍼센트 첫 당첨 경계 확인용 방
        Resolve(session, room, core, BattleResult.Escape); // 같은 방에서 승리 전 도주
        Assert.AreEqual(initialCount + 1, inventory.Count); // 도주 중 새 유물 지급 없음 확인
        Resolve(session, RoomWithRoll(20, "core-miss"), core, BattleResult.Victory); // 이십 퍼센트 첫 실패 경계 승리
        Assert.AreEqual(initialCount + 1, inventory.Count); // 경계값 이십의 추가 유물 없음 확인
        var victory = Resolve(session, room, core, BattleResult.Victory); // 실제 조우 승리의 결정적 유물 지급
        session.ResolveBattleResult(victory); // 같은 승리 결과의 중복 전달 재현
        Assert.AreEqual(initialCount + 2, inventory.Count); // 미보유 유물 한 종 추가 확인
        Assert.AreEqual(core.GoldReward, session.LastClearReward.Gold); // 이전 기본 골드 보상 보존 확인
        var awarded = session.LastClearReward.CoreRelic; // 실제 클리어 보상으로 공개된 유물 조회
        Assert.IsNotNull(awarded); // 표시할 실제 유물 원본 확인
        Assert.AreNotSame(fragment, awarded); // 보유 유물 중복 지급 차단 확인
        string path = Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 파일 경로
        try // 테스트 저장 파일 정리 보장
        { // 조우 상태와 유물 저장 검사 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 코어 승리와 유물 획득을 같은 저장에 반영
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 일반 탐사 복원 경로 실행
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(awarded.RelicId)); // 실제 지급한 유물 보유 복원 확인
            Assert.IsFalse(session.BeginEncounter(room, core, Vector3.zero, Vector3.right)); // 저장을 복원한 동일 조우의 재지급 차단
        } // 조우 상태와 유물 저장 검사 종료
        finally // 테스트 저장 파일 정리
        { // 저장 정리 시작
            ExplorationSaveService.Remove(path); // 독립 저장과 백업 삭제
        } // 저장 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 카드 제거 가격과 실제 결제 검사
    public IEnumerator ChalkDiscountMatchesTheShopAndPreservesFreeVoucher() // 표시와 금액 경계 및 교환권 우선 확인
    { // 세공된 초크 검사 시작
        RequireIsolatedPlay(); // 검사 전용 저장 환경 확인
        yield return new EnterPlayMode(); // 실제 상점 관리자 생성 허용
        yield return ChalkDiscountMatchesTheShopAndPreservesFreeVoucherInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 초크 검사 회차 종료
    } // 세공된 초크 검사 종료

    private IEnumerator ChalkDiscountMatchesTheShopAndPreservesFreeVoucherInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        PrepareExpedition(); // 제거할 카드가 있는 출전 덱 준비
        var chalk = Acquire("CarvedChalk"); // 실제 세공된 초크 획득
        var shop = ShopRunManager.EnsureInstance(); // 정상 상점 관리자 준비
        var catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 운영 상점 원본 조회
        Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 회차 덱의 상점 연결
        var deck = shop.RunDeck; // 실제 탐사 카드 목록 조회
        while (deck.CardCount <= RunDeckManager.MinimumCardsAfterRemoval + 1) // 유료와 무료 제거에 필요한 두 장 여유 확보
        { // 제거 검사 카드 준비 시작
            Assert.IsTrue(deck.TryAddCard(deck.Cards[0].Card, deck.Cards[0].Owner)); // 실제 제거 가능 카드 한 장 준비
        } // 제거 검사 카드 준비 종료
        var wallet = PlayerResourceManager.EnsureInstance(); // 상점에서 차감할 실제 골드 지갑
        var view = ShopPrototypeView.Create(shop); // 기존 코드로 실제 상점 화면 생성
        string path = Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 상점 회차 저장 경로
        try // 검사 상점과 게임 속도 정리 보장
        { // 상점 표시와 거래 검사 시작
            Assert.IsTrue(view.TryOpenFromRoom("chalk-pruning-test")); // 일반 방문 경로로 상점 열기
            var regularOffer = catalog.Offers.Single(offer => offer.OfferId == "SHOP_SHEET_HEAVY_STRIKE"); // 공통 상품 가격 대조 대상
            Assert.AreEqual(80, shop.GetOfferPrice(regularOffer)); // 초크 보유 중 일반 상품 원가 유지 확인
            var content = view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 실제 상점 버튼 목록 조회
            var service = content.Find("CardRemovalService"); // 카드 제거 서비스 버튼 조회
            Assert.AreEqual("120 Gold", service.Find("Price").GetComponent<TMP_Text>().text); // 상품 목록의 할인 가격 확인
            service.GetComponent<Button>().onClick.Invoke(); // 실제 제거할 카드 목록 열기
            Assert.AreEqual("120 Gold", content.Find("RemoveCard_0/Price").GetComponent<TMP_Text>().text); // 선택 목록의 같은 할인 가격 확인
            wallet.RestoreResources(119, 0, 0, 0); // 할인 가격보다 한 골드 부족한 잔액 준비
            Assert.AreEqual(ShopPurchaseResult.InsufficientGold, shop.TryRemoveCard(0)); // 부족한 잔액으로 카드 제거 차단
            Assert.AreEqual(119, wallet.Gold); // 실패한 거래의 잔액 보존
            wallet.RestoreResources(120, 0, 0, 0); // 정확한 서비스 가격만 충전
            int count = deck.CardCount; // 정상 제거 전 카드 수 보관
            content.Find("RemoveCard_0").GetComponent<Button>().onClick.Invoke(); // 표시된 실제 카드 제거 버튼 사용
            Assert.AreEqual(0, wallet.Gold); // 표시된 백이십 골드 차감 확인
            Assert.AreEqual(count - 1, deck.CardCount); // 성공한 거래로 카드 한 장 제거 확인
            Acquire("CerullionBadge"); // 상점 공통 십 퍼센트 할인 추가
            Assert.AreEqual(108, shop.GetCardRemovalPrice()); // 기존 공통 할인 뒤 초크 할인 확인
            Assert.AreEqual(72, shop.GetOfferPrice(regularOffer)); // 일반 상품에는 공통 할인만 적용 확인
            for (float deadline = Time.realtimeSinceStartup + 2f; Time.realtimeSinceStartup < deadline && content.Find("RemoveCard_0/Price").GetComponent<TMP_Text>().text != "108 Gold"; ) // 다음 플레이어 프레임의 외부 유물 가격 반영 대기
            { // 실제 상점 갱신 대기 시작
                yield return null; // 플레이의 지연 가격 갱신 기회
            } // 실제 상점 갱신 대기 종료
            Assert.AreEqual("108 Gold", content.Find("RemoveCard_0/Price").GetComponent<TMP_Text>().text); // 열린 제거 목록의 가격 새로고침 확인
            var voucher = Acquire("GoldenExchangeVoucher"); // 한 번 무료 제거하는 실제 교환권 획득
            Assert.AreEqual(0, shop.GetCardRemovalPrice()); // 교환권의 무료 처리 우선 확인
            Assert.AreNotEqual(ShopPurchaseResult.Success, shop.TryRemoveCard(-1)); // 잘못된 제거에서 교환권 소비 차단
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(voucher.RelicId)); // 실패한 거래의 교환권 보존
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryRemoveCard(0)); // 무일푼 상태의 정상 무료 제거
            Assert.IsFalse(RelicRunManager.Instance.Inventory.ContainsRelic(voucher.RelicId)); // 성공한 무료 제거 뒤 교환권 소비 확인
            Assert.AreEqual(108, shop.GetCardRemovalPrice()); // 초크 지속 할인으로 복귀 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 남은 상점 할인 유물 저장
            Assert.IsTrue(RelicRunManager.Instance.TryRemove(chalk.RelicId)); // 실제 보관함에서 복원 대상 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 상점과 보유 유물의 실제 저장 복원
            Assert.AreEqual(108, shop.GetCardRemovalPrice()); // 복원 뒤 정상 제거 할인 확인
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(chalk)); // 실제 상점의 초크 판매
            Assert.AreEqual(135, shop.GetCardRemovalPrice()); // 초크 판매 후 공통 할인만 적용 확인
        } // 상점 표시와 거래 검사 종료
        finally // 임시 상점 객체 정리
        { // 상점 정리 시작
            Object.Destroy(view.gameObject); // 테스트 상점 화면 제거
            ExplorationSaveService.Remove(path); // 독립 저장 파일과 백업 정리
        } // 상점 정리 종료
    } // 플레이 모드 본문 종료

    private static void RequireIsolatedPlay() // 실제 사용자 저장과 분리된 검사 확인
    { // 저장 환경 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 검증용 저장 위치 여부 확인
        { // 외부 저장 보호 시작
            Assert.Ignore("저장 경로가 분리된 검증 환경에서 실행"); // 실제 사용자 회차의 저장 보호
        } // 외부 저장 보호 종료
    } // 저장 환경 검사 종료

    private static ExplorationSessionManager PrepareExpedition() // 실제 탐사 파티와 저장 가능한 덱 생성
    { // 탐사 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 원본 명단 조회
        var recovery = new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance()); // 출전 가능 캐릭터 검증기 준비
        var selection = new PartySelection(recovery); // 두 명 편성용 선택 목록 초기화
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 캐릭터 두 명 편성
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 출전용 파티와 덱 생성
        var party = BattleResultManager.EnsureInstance(); // 생존 파티와 영속 상태 준비
        party.ResetSavedPartyState(); // 이전 검사 파티와 체력 초기화
        Assert.IsTrue(party.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 실제 탐사 파티 등록
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 시작 덱 구성
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 흐름 관리자 조회
        session.ResetExploration(); // 독립된 진행 기록 초기화
        session.SetCurrentFloorSeed(242); // 저장 검사용 실제 층 시드 설정
        return session; // 검사에 사용할 탐사 관리자 반환
    } // 탐사 준비 종료

    private static RelicData Acquire(string resourceName) // 실제 유물 원본과 보유 확인
    { // 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + resourceName); // 획득에 등록된 원본 조회
        Assert.IsNotNull(relic, resourceName); // 실제 유물 원본 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 정상 보유함 획득 확인
        return relic; // 실제 보유 원본 유물 반환
    } // 유물 획득 종료

    private static int Health(BattleResultManager party, CharacterData actor) // 저장된 행동자의 현재 체력 조회
    { // 실제 체력 조회 시작
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out int hp, out _, out _)); // 현재 탐사의 영속 상태 확인
        return hp; // 읽은 실제 체력 반환
    } // 실제 체력 조회 종료

    private static BattleResultData Resolve(ExplorationSessionManager session, string room, EncounterData encounter, BattleResult outcome) // 실제 조우 정산 API 실행
    { // 조우 시작과 정산 시작
        Assert.IsTrue(session.BeginEncounter(room, encounter, Vector3.zero, Vector3.right)); // 실제 미완료 방 조우 활성화
        var ally = BattleUnitRuntime.CreateAlly(PartyLoadoutRuntime.Active.Party.Members[0]); // 결과에 포함할 출전 생존자 생성
        var result = new BattleResultData(outcome, encounter.BattleType, 1, new[] { ally }, System.Array.Empty<string>()); // 실제 조우 결과 생성
        session.ResolveBattleResult(result); // 정식 조우 승리나 도주 결과 적용
        return result; // 반복 전달에 사용할 원래 결과 반환
    } // 조우 시작과 정산 종료

    private static string RoomWithRoll(int expected, string prefix) // 실제 보상 추첨 경계에 도달할 방 조회
    { // 결정 시드 조회 시작
        var hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 공용 고정 보상 시드 함수 확인
        Assert.IsNotNull(hash); // 실제 해시 함수 조회 성공 확인
        var arguments = new object[1]; // 방 시드 해시의 인수 배열
        for (int index = 0; index < 10000; index++) // 검증용 방 식별 후보 탐색
        { // 방 추첨 조회 시작
            string room = prefix + "-" + index; // 서로 다른 비교용 추첨 식별자
            arguments[0] = room + ":core-relic"; // 침식핵의 고정 난수 식별자
            int seed = (int)hash.Invoke(null, arguments); // 기존 탐사 보상용 실제 해시 계산
            if (new System.Random(seed).Next(100) == expected) // 희망한 경계 추첨값 확인
            { // 결정 시드 발견 시작
                return room; // 같은 추첨 결과를 만드는 방 반환
            } // 결정 시드 발견 종료
        } // 방 추첨 조회 종료
        Assert.Fail("검사할 결정 추첨값을 찾을 수 없음"); // 유효한 시드 준비 실패 보고
        return null; // 검사 실패 분기 반환
    } // 결정 시드 조회 종료
} // 탐사 유물 검사 종료
