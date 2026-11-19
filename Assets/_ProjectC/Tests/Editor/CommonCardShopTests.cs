using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 보상 후보 목록 사용
using System.Reflection; // 보상 후보 검사 사용
using NUnit.Framework; // 실제 상점 검사 사용
using UnityEngine; // 유니티 객체 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 상점 선택 버튼 사용
using TMPro; // 상점 안내 문구 확인

public sealed class CommonCardShopTests // 공용 카드 상점 검사
{ // 검사 시작
    [UnityTest] // 실제 소유자 선택 검사 지정
    public IEnumerator PurchaseRequiresChosenPartyMemberAndRestoresOwner() // 실제 구매와 저장 복원 확인
    { // 검사 본문 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 경로 생성
        try // 독립 저장 파일 정리 보장
        { // 구매 검사 시작
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
            PartyDepartureTestSupport.StartExploration(0, 1); // 두 명 파티로 탐사 시작
            for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 대기
            { // 탐사 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 확인
            yield return null; // 상점 초기화 대기
            ShopRunManager shop = ShopRunManager.EnsureInstance(); // 실제 상점 관리자 조회
            ShopCatalogData catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점 상품 조회
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 상점 덱 준비
            shop.BeginRoomVisit("common_card_shop_test"); // 독립 상점 방문
            CharacterData first = PartyLoadoutRuntime.Active.Party.Members[0]; // 첫 파티 캐릭터 조회
            CharacterData second = PartyLoadoutRuntime.Active.Party.Members[1]; // 두 번째 파티 캐릭터 조회
            CharacterData outsider = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[2]; // 파티 외 캐릭터 조회
            ShopOfferData offer = null; // 강타 상품 참조 준비
            foreach (ShopOfferData item in catalog.Offers) // 실제 상품 순회
            { // 강타 상품 검색 시작
                if (item.OfferId == "SHOP_SHEET_HEAVY_STRIKE") // 공용 강타 상품 확인
                { // 상품 발견 시작
                    offer = item; // 실제 상품 보관
                    break; // 상품 검색 종료
                } // 상품 발견 종료
            } // 강타 상품 검색 종료
            Assert.IsNotNull(offer); // 공용 카드 상품 확인
            Assert.IsTrue(offer.RequiresOwnerChoice); // 직접 소유자 선택 표시 확인
            Assert.AreEqual(80, offer.Price); // 사용자 확인 임시 가격 확인
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 조회
            resources.AddResources(500, 0, 0, 0); // 구매 검사 골드 준비
            int beforeGold = resources.Gold; // 구매 전 골드 보관
            Assert.AreNotEqual(ShopPurchaseResult.Success, shop.TryPurchase(offer)); // 소유자 없는 구매 차단 확인
            Assert.AreNotEqual(ShopPurchaseResult.Success, shop.TryPurchase(offer, outsider)); // 파티 외 소유자 차단 확인
            Assert.AreEqual(beforeGold, resources.Gold); // 잘못된 선택의 골드 유지 확인
            RunDeckManager deck = shop.RunDeck; // 실제 탐사 덱 조회
            CardData firstCard = deck.Cards[0].Card; // 첫 캐릭터의 시험 카드 조회
            for (int index = 0; index < RunDeckManager.MinimumCardsAfterRemoval; index++) // 덱 최소 장수 확보
            { // 보조 카드 준비 시작
                Assert.IsTrue(deck.TryAddCard(firstCard, first)); // 첫 파티원 시험 카드 추가
            } // 보조 카드 준비 종료
            for (int index = deck.CardCount - 1; index >= 0; index--) // 기존 카드 역순 순회
            { // 두 번째 소유자 카드 제거 시작
                if (deck.Cards[index].Owner == second) // 두 번째 캐릭터 카드 확인
                { // 카드 제거 시작
                    Assert.IsTrue(deck.TryRemoveAt(index)); // 기존 카드 제거 확인
                } // 카드 제거 종료
            } // 기존 카드 제거 종료
            Assert.IsTrue(deck.ContainsOwner(second)); // 기존 카드가 없는 파티원 선택 가능 확인
            ShopPrototypeView view = Object.FindFirstObjectByType<ShopPrototypeView>(); // 실제 상점 화면 조회
            if (view == null) // 상점 화면 부재 확인
            { // 상점 화면 생성 시작
                view = ShopPrototypeView.Create(shop); // 실제 상점 화면 생성
            } // 상점 화면 생성 종료
            Assert.IsTrue(view.TryOpenFromRoom(shop.ActiveRoomRuntimeId)); // 상점 방 방문 성공 확인
            Transform content = view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 실제 상품 목록 조회
            content.Find("Offer_SHOP_SHEET_HEAVY_STRIKE").GetComponent<Button>().onClick.Invoke(); // 공용 카드 선택
            GameObject.Find("ConfirmPurchase").GetComponent<Button>().onClick.Invoke(); // 지불 조건 확인 뒤 소유자 선택
            TMP_Text resultText = view.transform.Find("ShopOverlay/ShopPanel/ResultText").GetComponent<TMP_Text>(); // 실제 안내 문구 조회
            StringAssert.Contains(offer.DisplayName, resultText.text); // 선택한 카드 이름 표시 확인
            Assert.AreEqual(beforeGold, resources.Gold); // 소유자 선택 전 골드 유지 확인
            Assert.IsNotNull(content.Find("ChooseCardOwner_" + first.CharacterId)); // 첫 파티원 선택 표시 확인
            content.Find("BackToProducts").GetComponent<Button>().onClick.Invoke(); // 소유자 선택 취소
            Assert.AreEqual("상품을 선택하세요.", resultText.text); // 상품 선택 안내 복원 확인
            Assert.AreEqual(beforeGold, resources.Gold); // 선택 취소 시 골드 유지 확인
            Assert.IsFalse(shop.IsPurchased(offer)); // 선택 취소 시 상품 유지 확인
            yield return null; // 이전 선택 버튼 정리 대기
            content.Find("Offer_SHOP_SHEET_HEAVY_STRIKE").GetComponent<Button>().onClick.Invoke(); // 공용 카드 다시 선택
            GameObject.Find("ConfirmPurchase").GetComponent<Button>().onClick.Invoke(); // 확인한 공용 카드의 소유자 다시 선택
            Transform secondButton = content.Find("ChooseCardOwner_" + second.CharacterId); // 두 번째 파티원 선택 조회
            Assert.IsNotNull(secondButton); // 두 번째 파티원 선택 표시 확인
            secondButton.GetComponent<Button>().onClick.Invoke(); // 직접 선택한 파티원에게 구매
            Assert.AreEqual(beforeGold - 80, resources.Gold); // 사용자 지정 가격 차감 확인
            Assert.IsTrue(shop.IsPurchased(offer)); // 상점 상품 판매 완료 확인
            RunDeckCardEntry added = deck.Cards[deck.CardCount - 1]; // 새 카드 항목 조회
            Assert.AreSame(offer.Card, added.Card); // 선택한 공용 카드 지급 확인
            Assert.AreSame(second, added.Owner); // 선택한 캐릭터 소유 확인
            MethodInfo candidates = typeof(ExplorationTreasureRewardService).GetMethod("GetValidOffers", BindingFlags.Static | BindingFlags.NonPublic); // 보물 후보 조회
            List<ShopOfferData> rewardOffers = (List<ShopOfferData>)candidates.Invoke(null, new object[] { ShopOfferType.Card }); // 실제 보물 카드 후보 계산
            Assert.IsTrue(rewardOffers.Exists(rewardOffer => rewardOffer.RequiresOwnerChoice)); // 공용 카드 보상 후보 포함 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 구매 후 독립 저장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 구매 후 저장 복원 확인
            RunDeckCardEntry restored = RunDeckManager.Instance.Cards[RunDeckManager.Instance.CardCount - 1]; // 복원된 카드 조회
            Assert.AreEqual(offer.Card.CardId, restored.Card.CardId); // 공용 카드 복원 확인
            Assert.AreEqual(second.CharacterId, restored.Owner.CharacterId); // 선택한 소유자 복원 확인
            Assert.AreEqual(beforeGold - 80, PlayerResourceManager.Instance.Gold); // 구매 골드 복원 확인
        } // 구매 검사 종료
        finally // 독립 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 검사 본문 종료
} // 검사 종료
