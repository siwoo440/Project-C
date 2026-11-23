using System.Collections; // 플레이 모드 대기 사용
using System.Reflection; // 할인 계산 경계 검사 사용
using NUnit.Framework; // 상점 할인 검사 사용
using TMPro; // 상점 가격 문구 검사 사용
using UnityEngine; // 실제 게임 객체 사용
using UnityEngine.SceneManagement; // 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 상점 버튼 사용

public sealed class ShopDiscountRelicTests // 상점 할인 유물 검사
{ // 할인 검사 시작
    [UnityTest] // 실제 구매와 복원 검사 지정
    public IEnumerator BadgeDiscountsPurchasesAndRemovalUntilSold() // 획득·할인·저장·판매 흐름 검사
    { // 할인 검사 본문 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 경로 생성
        try // 독립 저장 파일 정리 보장
        { // 실제 상점 검사 시작
            SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
            for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 시작 메뉴 확인
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
            for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 대기
            { // 거점 대기 시작
                yield return null; // 다음 프레임 대기
            } // 거점 대기 종료
            Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 거점 화면 확인
            PartyDepartureTestSupport.StartExploration(0); // 실제 파티 탐사 시작
            for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 대기
            { // 탐사 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 확인
            yield return null; // 상점 초기화 대기
            ShopRunManager shop = ShopRunManager.EnsureInstance(); // 실제 상점 관리자 조회
            ShopCatalogData catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점 원본 조회
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 실제 상점 준비
            shop.BeginRoomVisit("badge_discount_test"); // 독립 상점 방문
            RelicData badge = Resources.Load<RelicData>("Relics/CerullionBadge"); // 실제 세룰리온 배지 조회
            Assert.IsNotNull(badge); // 배지 원본 존재 확인
            Assert.IsNotNull(badge.Icon); // 배지 아이콘 존재 확인
            ShopOfferData badgeOffer = null; // 배지 상품 참조 준비
            ShopOfferData cardOffer = null; // 공용 카드 상품 참조 준비
            ShopOfferData otherRelicOffer = null; // 가격 범위 유물 상품 참조 준비
            foreach (ShopOfferData offer in catalog.Offers) // 실제 상품 순회
            { // 검사 상품 검색 시작
                if (offer.OfferId == "SHOP_RLC_CERULLION_BADGE") // 배지 상품 확인
                { // 배지 상품 저장 시작
                    badgeOffer = offer; // 실제 배지 상품 저장
                } // 배지 상품 저장 종료
                if (offer.OfferId == "SHOP_SHEET_HEAVY_STRIKE") // 강타 상품 확인
                { // 강타 상품 저장 시작
                    cardOffer = offer; // 실제 강타 상품 저장
                } // 강타 상품 저장 종료
                if (offer.OfferId == "SHOP_RLC_GATE_KEY") // 범위 가격 유물 확인
                { // 다른 유물 저장 시작
                    otherRelicOffer = offer; // 실제 다른 유물 상품 저장
                } // 다른 유물 저장 종료
            } // 검사 상품 검색 종료
            Assert.IsNotNull(badgeOffer); // 배지 상점 상품 확인
            Assert.IsNotNull(cardOffer); // 기존 카드 상품 확인
            Assert.IsNotNull(otherRelicOffer); // 기존 유물 상품 확인
            int badgePrice = shop.GetOfferPrice(badgeOffer); // 할인 전 배지 구매 가격 보관
            int otherPrice = shop.GetOfferPrice(otherRelicOffer); // 할인 전 유물 가격 보관
            int salePrice = shop.GetRelicSellPrice(badge); // 할인 전 배지 판매 가격 보관
            Assert.That(badgePrice, Is.InRange(214, 259)); // 시트 배지 구매 범위 확인
            Assert.That(salePrice, Is.InRange(109, 148)); // 시트 배지 판매 범위 확인
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 조회
            resources.AddResources(2000, 0, 0, 0); // 구매 검사 골드 준비
            int beforeGold = resources.Gold; // 배지 구매 전 골드 보관
            ShopPrototypeView view = Object.FindFirstObjectByType<ShopPrototypeView>(); // 실제 상점 화면 조회
            if (view == null) // 상점 화면 부재 확인
            { // 상점 화면 생성 시작
                view = ShopPrototypeView.Create(shop); // 실제 상점 화면 생성
            } // 상점 화면 생성 종료
            view.transform.Find("OpenShopButton").GetComponent<Button>().onClick.Invoke(); // 실제 상점 열기
            Transform content = view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 실제 상품 목록 조회
            Assert.AreEqual("80 Gold", content.Find("Offer_SHOP_SHEET_HEAVY_STRIKE/Price").GetComponent<TMP_Text>().text); // 할인 전 카드 표시 가격 확인
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(badgeOffer)); // 실제 배지 구매 확인
            Assert.AreEqual(beforeGold - badgePrice, resources.Gold); // 배지 구입에 할인 미적용 확인
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(badge.RelicId)); // 배지 보유 확인
            Assert.AreEqual(72, shop.GetOfferPrice(cardOffer)); // 80골드 카드의 할인 가격 확인
            MethodInfo discount = typeof(ShopRunManager).GetMethod("ApplyShopDiscount", BindingFlags.Static | BindingFlags.NonPublic); // 실제 할인 계산 조회
            Assert.AreEqual(77, discount.Invoke(null, new object[] { 85 })); // 할인액 소수점 버림 확인
            Assert.AreEqual(1, discount.Invoke(null, new object[] { 1 })); // 한 골드 가격 보존 확인
            Assert.AreEqual(1932735283, discount.Invoke(null, new object[] { int.MaxValue })); // 최대 가격 곱셈 범위 확인
            Assert.AreEqual(otherPrice - otherPrice / 10, shop.GetOfferPrice(otherRelicOffer)); // 유물 범위 가격의 할인 확인
            Assert.AreEqual(salePrice, shop.GetRelicSellPrice(badge)); // 유물 판매 가격 유지 확인
            yield return null; // 기존 상품 표시 제거 대기
            Assert.AreEqual("판매 완료", content.Find("Offer_SHOP_RLC_CERULLION_BADGE/Price").GetComponent<TMP_Text>().text); // 구매한 배지의 오해 없는 판매 완료 표시 확인
            Assert.AreEqual("72 Gold", content.Find("Offer_SHOP_SHEET_HEAVY_STRIKE/Price").GetComponent<TMP_Text>().text); // 갱신된 카드 표시 가격 확인
            Assert.AreEqual("135 Gold", content.Find("CardRemovalService/Price").GetComponent<TMP_Text>().text); // 카드 제거 서비스 할인 표시 확인
            CharacterData owner = PartyLoadoutRuntime.Active.Party.Members[0]; // 직접 선택할 파티원 조회
            int afterBadgeGold = resources.Gold; // 카드 구매 전 골드 보관
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(cardOffer, owner)); // 할인된 공용 카드 구매 확인
            Assert.AreEqual(afterBadgeGold - 72, resources.Gold); // 표시된 카드 비용 차감 확인
            RunDeckManager deck = shop.RunDeck; // 실제 탐사 덱 조회
            CardData firstCard = deck.Cards[0].Card; // 카드 제거 검사 원본 조회
            for (int index = 0; index < RunDeckManager.MinimumCardsAfterRemoval; index++) // 최소 카드 수 확보
            { // 제거 검사 카드 준비 시작
                Assert.IsTrue(deck.TryAddCard(firstCard, owner)); // 제거 가능 카드 추가
            } // 제거 검사 카드 준비 종료
            int beforeRemoval = resources.Gold; // 카드 제거 전 골드 보관
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryRemoveCard(0)); // 실제 카드 제거 확인
            Assert.AreEqual(beforeRemoval - 135, resources.Gold); // 할인된 제거 비용 차감 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 배지 보유 상태 저장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 배지 보유 상태 복원 확인
            shop = ShopRunManager.Instance; // 복원된 상점 조회
            resources = PlayerResourceManager.Instance; // 복원된 자원 조회
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(badge.RelicId)); // 배지 보유 복원 확인
            Assert.AreEqual(otherPrice - otherPrice / 10, shop.GetOfferPrice(otherRelicOffer)); // 복원 후 할인 가격 유지 확인
            Assert.AreEqual(salePrice, shop.GetRelicSellPrice(badge)); // 복원 후 판매 가격 유지 확인
            int beforeSale = resources.Gold; // 배지 판매 전 골드 보관
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(badge)); // 실제 배지 판매 확인
            Assert.AreEqual(beforeSale + salePrice, resources.Gold); // 배지 판매 대금 확인
            Assert.AreEqual(otherPrice, shop.GetOfferPrice(otherRelicOffer)); // 판매 직후 할인 해제 확인
        } // 실제 상점 검사 종료
        finally // 독립 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 할인 검사 본문 종료
} // 할인 검사 종료
