using System.Collections; // 플레이 모드 대기
using System.Collections.Generic; // 보상 후보 목록
using System.IO; // 독립 저장 파일 경로
using System.Linq; // 카드와 유물 목록 대조
using System.Reflection; // 보물 추첨 후보 조회
using NUnit.Framework; // 테스트 검증
using UnityEngine; // 실제 파티와 방 객체
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 보물 소유자 선택 버튼

public sealed class AcquisitionPersistenceTests // 획득 경로와 탐사 저장 통합 검사
{ // 통합 검사 시작
    [UnityTest] // 고정 소유자 상품 검사 지정
    public IEnumerator FixedOwnerOffersFollowTheCurrentDeck() // 출전 소유자별 상품과 보물 후보 확인
    { // 소유자 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        ShopRunManager shop = PrepareExpedition(); // 일반 파티의 상점 준비
        ShopOfferData[] fixedCards = shop.Catalog.Offers.Where(offer => offer.CardOwner != null && offer.CardOwner.CharacterId == "CHR_TEST" && !offer.RequiresOwnerChoice).ToArray(); // 시험 캐릭터 전용 상품 수집
        CollectionAssert.IsSubsetOf(new[] { "SHOP_CARD_ATTACK", "SHOP_CARD_ARCANE_BOLT", "SHOP_CARD_SELF_HEAL" }, fixedCards.Select(offer => offer.OfferId)); // 기존 시험 상품 보존 확인
        ShopPrototypeView view = ShopPrototypeView.Create(shop); // 실제 상품 목록 생성
        Assert.IsTrue(view.TryOpenFromRoom("fixed-owner-test")); // 상점 방 방문
        yield return null; // 이전 목록 제거 대기
        AssertFixedOffers(view, fixedCards, false); // 일반 파티에서 불가능한 카드 제외 확인
        view.transform.Find("ShopOverlay/ShopPanel/CloseButton").GetComponent<Button>().onClick.Invoke(); // 상점 닫기
        ShopOfferData first = fixedCards[0]; // 기존 시험 캐릭터 상품 조회
        DeckData testDeck = DeckData.CreateRuntime(new[] { new DeckCardEntry(first.Card, first.CardOwner) }); // 고정 소유자가 있는 덱 생성
        Assert.IsTrue(shop.Prepare(shop.Catalog, testDeck)); // 시험 덱으로 상점 준비
        Assert.IsTrue(view.TryOpenFromRoom("fixed-owner-present")); // 시험 덱의 상점 방문
        yield return null; // 이전 목록 제거 대기
        AssertFixedOffers(view, fixedCards, true); // 해당 소유자의 상품과 보물 후보 유지 확인
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(first)); // 기존 고정 소유자 구매 확인
        view.transform.Find("ShopOverlay/ShopPanel/CloseButton").GetComponent<Button>().onClick.Invoke(); // 상점 닫기
        Object.Destroy(testDeck); // 독립 시험 덱 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 소유자 검사 종료

    private static void AssertFixedOffers(ShopPrototypeView view, ShopOfferData[] offers, bool expected) // 고정 소유자 상품 노출 확인
    { // 상품 노출 검사 시작
        Transform products = view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 상점 상품 목록 조회
        MethodInfo candidates = typeof(ExplorationTreasureRewardService).GetMethod("GetValidOffers", BindingFlags.Static | BindingFlags.NonPublic); // 실제 추첨 후보 조회 메서드
        var rewards = (List<ShopOfferData>)candidates.Invoke(null, new object[] { ShopOfferType.Card }); // 현재 덱의 추첨 후보 계산
        foreach (ShopOfferData offer in offers) // 기존 고정 소유자 상품 순회
        { // 개별 상품 검사 시작
            Assert.AreEqual(expected, products.Find("Offer_" + offer.OfferId) != null, offer.OfferId); // 상점 표시 여부 확인
            Assert.AreEqual(expected, rewards.Contains(offer), offer.OfferId); // 보물 추첨 포함 여부 확인
        } // 개별 상품 검사 종료
    } // 상품 노출 검사 종료

    [UnityTest] // 실제 획득 및 파일 복원 검사 지정
    public IEnumerator PurchasedAndRewardedItemsSurviveClearingTheRun() // 구입·보물 획득 후 빈 상태에서 복원 확인
    { // 획득 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string path = Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 파일 경로
        try // 테스트 저장 파일 정리 보장
        { // 획득과 복원 검사 시작
            ShopRunManager shop = PrepareExpedition(); // 두 캐릭터의 탐사 준비
            shop.BeginRoomVisit("acquisition-test-shop"); // 독립 상점 방문
            CharacterData buyer = PartyLoadoutRuntime.Active.Party.Members[0]; // 구매 카드 소유자 조회
            CharacterData recipient = PartyLoadoutRuntime.Active.Party.Members[1]; // 보물 카드 소유자 조회
            ShopOfferData[] cards = shop.Catalog.Offers.Where(offer => offer.Card != null && (offer.RequiresOwnerChoice || shop.RunDeck.ContainsOwner(offer.CardOwner))).ToArray(); // 현재 파티에 지급 가능한 카드 수집
            ShopOfferData[] relics = shop.Catalog.Offers.Where(offer => offer.OfferType == ShopOfferType.Relic).ToArray(); // 실제 유물 상품 수집
            CollectionAssert.IsSubsetOf(new[] { "SHOP_SHEET_HEAVY_STRIKE", "SHOP_SHEET_BLADE_STORM", "SHOP_SHEET_CLEANSE" }, cards.Where(offer => !offer.RewardOnly).Select(offer => offer.OfferId)); // 기존 판매용 공용 카드 포함 확인
            CollectionAssert.IsSubsetOf(new[] { "REWARD_SHEET_SMALL_HEAL", "REWARD_SHEET_FIREBALL", "REWARD_SHEET_MAGIC_MISSILE" }, cards.Where(offer => offer.RewardOnly).Select(offer => offer.OfferId)); // 기존 보상 전용 공용 카드 포함 확인
            CollectionAssert.IsSubsetOf(new[] { "RLC_ASH_SEAL", "RLC_CERULLION_BADGE", "RELIC_TEST_HEAL", "RELIC_TEST_MAX_HP" }, relics.Select(offer => offer.Relic.RelicId)); // 시트와 초기 시험 유물 포함 확인
            int startingCards = shop.RunDeck.CardCount; // 시작 덱 장수 보관
            foreach (ShopOfferData offer in cards.Concat(relics)) // 카드와 유물 상품 순회
            { // 상품 거래 검사 시작
                int gold = shop.Gold; // 거래 전 골드 보관
                int cost = shop.GetOfferPrice(offer); // 실제 거래 가격 조회
                ShopPurchaseResult result = shop.TryPurchase(offer, buyer); // 실제 구매 요청
                Assert.AreEqual(offer.RewardOnly ? ShopPurchaseResult.InvalidOffer : ShopPurchaseResult.Success, result, offer.OfferId); // 판매 가능 여부 확인
                Assert.AreEqual(offer.RewardOnly ? gold : gold - cost, shop.Gold, offer.OfferId); // 거래 금액 확인
                Assert.AreEqual(!offer.RewardOnly, shop.IsPurchased(offer), offer.OfferId); // 성공한 상품만 판매 완료 확인
            } // 상품 거래 검사 종료
            CollectionAssert.AreEquivalent(cards.Where(offer => !offer.RewardOnly).Select(offer => offer.Card), shop.RunDeck.Cards.Skip(startingCards).Select(entry => entry.Card)); // 구매 카드 전량 확인
            CollectionAssert.AreEqual(cards.Where(offer => !offer.RewardOnly).Select(offer => offer.RequiresOwnerChoice ? buyer : offer.CardOwner), shop.RunDeck.Cards.Skip(startingCards).Select(entry => entry.Owner)); // 구매 카드별 소유자 확인
            CollectionAssert.AreEquivalent(relics.Select(offer => offer.Relic), RelicRunManager.Instance.Inventory.OwnedRelics); // 구매 유물 전량 확인
            AssertFileRoundTrip(path, shop); // 구매 상태 저장과 복원 확인
            int goldBeforeFallback = shop.Gold; // 모든 유물을 보유한 시점의 골드 보관
            string relicRoom = FindRoomsWithReward(ExplorationTreasureRewardType.Relic).First(); // 실제 추첨이 유물인 방 조회
            Assert.IsTrue(ExplorationTreasureRewardService.TryGrantReward(relicRoom, out _, out _)); // 유물 소진 후 대체 보상 추첨
            Assert.AreEqual(goldBeforeFallback, shop.Gold); // 후보에서 탈락한 유물의 골드 미지급 확인
            foreach (ShopOfferData offer in relics) // 구매 유물 순회
            { // 보물 지급 준비 시작
                Assert.IsTrue(RelicRunManager.Instance.TryRemove(offer.Relic.RelicId)); // 보물로 다시 획득할 유물 제거
            } // 보물 지급 준비 종료
            HashSet<CardData> remaining = new HashSet<CardData>(cards.Select(offer => offer.Card)); // 아직 확인할 보물 카드 원본
            foreach (string room in FindRoomsWithReward(ExplorationTreasureRewardType.Card)) // 서로 다른 카드 보물 순회
            { // 공용 카드 보상 검사 시작
                int count = shop.RunDeck.CardCount; // 보물 열기 전 카드 수 보관
                OpenTreasure(room, recipient); // 실제 보물 카드 획득
                Assert.AreEqual(count + 1, shop.RunDeck.CardCount, room); // 방마다 카드 한 장 지급 확인
                RunDeckCardEntry added = shop.RunDeck.Cards[count]; // 새 보물 카드 조회
                Assert.IsTrue(cards.Any(offer => offer.Card == added.Card && added.Owner == (offer.RequiresOwnerChoice ? recipient : offer.CardOwner)), room); // 카드와 지정 소유자 확인
                remaining.Remove(added.Card); // 확인한 카드 제외
                if (remaining.Count == 0) // 모든 보물 카드 획득 확인
                { // 카드 검사 완료 시작
                    break; // 보물 방 순회 종료
                } // 카드 검사 완료 종료
            } // 공용 카드 보상 검사 종료
            CollectionAssert.IsEmpty(remaining.Select(card => card.CardId)); // 획득하지 못한 카드 확인
            foreach (string room in FindRoomsWithReward(ExplorationTreasureRewardType.Relic).Take(relics.Length)) // 현재 유물 수만큼 다른 유물 방 순회
            { // 유물 보상 검사 시작
                int count = RelicRunManager.Instance.Inventory.Count; // 보물 열기 전 유물 수 보관
                OpenTreasure(room, null); // 실제 보물 유물 지급
                Assert.AreEqual(count + 1, RelicRunManager.Instance.Inventory.Count, room); // 방마다 신규 유물 한 종 지급 확인
            } // 유물 보상 검사 종료
            CollectionAssert.AreEquivalent(relics.Select(offer => offer.Relic), RelicRunManager.Instance.Inventory.OwnedRelics); // 보물 유물 전량 확인
            AssertFileRoundTrip(path, shop); // 보물 획득과 판매 완료 상태 복원 확인
        } // 획득과 복원 검사 종료
        finally // 저장 파일과 선택 창 정리
        { // 정리 시작
            ExplorationRewardOwnerView.ClosePending(); // 미완료 보상 선택 창 닫기
            ExplorationSaveService.Remove(path); // 독립 저장 파일과 복구본 제거
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 획득 검사 종료

    private static ShopRunManager PrepareExpedition() // 실제 파티와 독립 탐사 준비
    { // 탐사 준비 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 명단 조회
        PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 출전 조건 준비
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 번째 파티원 선택
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 두 번째 파티원 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 편성과 시작 덱 생성
        ExplorationSessionManager session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 조회
        session.ResetExploration(); // 이전 탐사 상태 초기화
        session.SetCurrentFloorSeed(193); // 독립 탐사 층 시드 설정
        PlayerResourceManager.EnsureInstance().RestoreResources(1000000, 0, 0, 0); // 전체 상품 구매 골드 준비
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 편성의 시작 덱 적용
        ShopRunManager shop = ShopRunManager.EnsureInstance(); // 실제 상점 관리자 조회
        Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), PartyLoadoutRuntime.Active.Deck)); // 프로젝트 상품 원본 연결
        return shop; // 준비된 상점 반환
    } // 탐사 준비 종료

    private static IEnumerable<string> FindRoomsWithReward(ExplorationTreasureRewardType type) // 실제 보상 종류별 방 식별자 검색
    { // 보물 방 검색 시작
        MethodInfo hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 게임의 보물 방 시드 계산 조회
        for (int number = 0; number < 4096; number++) // 재현 가능한 서로 다른 방 순회
        { // 방 종류 확인 시작
            string id = "t193-" + number; // 독립 방 식별자 생성
            int seed = (int)hash.Invoke(null, new object[] { id }); // 실제 방 시드 조회
            int roll = new System.Random(seed).Next(100); // 방의 첫 보상 종류 추첨
            if (ExplorationTreasureRewardService.RollRewardType(roll) == type) // 요청한 보상 종류 확인
            { // 해당 보물 방 선택 시작
                yield return id; // 해당 방 식별자 반환
            } // 해당 보물 방 선택 종료
        } // 방 종류 확인 종료
    } // 보물 방 검색 종료

    private static void OpenTreasure(string id, CharacterData owner) // 탐사 보물 방의 실제 보상 획득
    { // 보물 지급 시작
        GameObject room = new GameObject(id); // 독립 보물 방 생성
        room.AddComponent<ExplorationSpecialRoomView>().Initialize(null, Vector2Int.zero, ExplorationRoomType.Treasure, id); // 실제 보물 동작 연결
        room.SendMessage("OpenTreasure"); // 보물 열기
        Transform chooser = room.transform.Find("TreasureOwnerCanvas"); // 공용 카드 선택 화면 조회
        if (chooser != null) // 카드 소유자 선택 필요 여부 확인
        { // 카드 소유자 선택 시작
            Assert.IsNotNull(owner, id); // 보물 카드 소유자 지정 확인
            Transform button = chooser.Find("Panel/Owner_" + owner.CharacterId); // 실제 소유자 버튼 조회
            Assert.IsNotNull(button, id); // 카드 선택 창 표시 확인
            button.GetComponent<Button>().onClick.Invoke(); // 파티원에게 카드 지급
        } // 카드 소유자 선택 종료
        Assert.IsTrue(ExplorationSessionManager.Instance.IsEventResolved(id), id); // 보물 지급과 사용 처리 확인
        Object.Destroy(room); // 독립 보물 방 정리
    } // 보물 지급 종료

    private static void AssertFileRoundTrip(string path, ShopRunManager shop) // 현재 상태 삭제 후 실제 파일 복원 검사
    { // 파일 복원 검사 시작
        RunDeckManager deck = shop.RunDeck; // 현재 덱 조회
        RelicRunManager relics = RelicRunManager.EnsureInstance(); // 현재 유물 보관함 조회
        ExplorationSessionManager session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
        string room = shop.ActiveRoomRuntimeId; // 상점 방 식별자 보관
        int gold = shop.Gold; // 거래 잔액 보관
        CardData[] cards = deck.Cards.Select(entry => entry.Card).ToArray(); // 현재 카드 순서 보관
        CharacterData[] owners = deck.Cards.Select(entry => entry.Owner).ToArray(); // 카드별 소유자 보관
        RelicData[] owned = relics.Inventory.OwnedRelics.ToArray(); // 유물 획득 순서 보관
        string[] events = session.ResolvedEventIds.ToArray(); // 사용한 보물 방 보관
        ShopOfferData[] sold = shop.Catalog.Offers.Where(shop.IsPurchased).ToArray(); // 구매한 상품 보관
        Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 실제 탐사 파일 작성
        Assert.IsTrue(File.Exists(path)); // 지정 경로의 파일 확인
        shop.ResetSession(); // 기존 판매 기록 제거
        session.ResetExploration(); // 기존 보물 방 기록 제거
        deck.ResetToSource(null); // 기존 카드와 소유자 제거
        PartyLoadoutRuntime.Clear(); // 기존 편성 제거
        PlayerResourceManager.Instance.ResetResources(); // 기존 골드 제거
        foreach (RelicData relic in owned) // 기존 유물 순회
        { // 기존 유물 제거 시작
            Assert.IsTrue(relics.TryRemove(relic.RelicId)); // 저장 전 보유 상태 제거
        } // 기존 유물 제거 종료
        Assert.IsTrue(ExplorationSaveService.Load(path)); // 파일에서 탐사 상태 복원
        CollectionAssert.AreEqual(cards, deck.Cards.Select(entry => entry.Card)); // 카드 종류와 순서 복원 확인
        CollectionAssert.AreEqual(owners, deck.Cards.Select(entry => entry.Owner)); // 카드별 소유자 복원 확인
        CollectionAssert.AreEqual(owned, relics.Inventory.OwnedRelics); // 유물 종류와 획득 순서 복원 확인
        CollectionAssert.AreEquivalent(events, session.ResolvedEventIds); // 사용한 보물 방 복원 확인
        CollectionAssert.AreEquivalent(sold, shop.Catalog.Offers.Where(shop.IsPurchased)); // 판매 완료 상품 복원 확인
        Assert.AreEqual(room, shop.ActiveRoomRuntimeId); // 상점 방 복원 확인
        Assert.AreEqual(gold, shop.Gold); // 구매 잔액 복원 확인
    } // 파일 복원 검사 종료
} // 통합 검사 종료
