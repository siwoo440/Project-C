using System; // 검사 임시 식별자 생성
using System.Collections; // 플레이 모드 전환 대기
using System.Collections.Generic; // 실제 상점 재고 수집
using System.IO; // 임시 탐사 저장 입출력
using System.Linq; // 실제 카드와 재고 원본 조회
using System.Reflection; // 내부 재고와 고정 방 확률 조회
using System.Text.RegularExpressions; // 이전 탐사 저장 형식 재현
using NUnit.Framework; // 실제 상점 결과 대조
using TMPro; // 상점 가격 문구 조회
using UnityEngine; // Unity 실제 회차 원본 이용
using UnityEngine.TestTools; // 실제 플레이 모드 검사 등록
using UnityEngine.UI; // 상점과 소유자 선택 버튼 사용
using Object = UnityEngine.Object; // 임시 상점 화면 정리

public sealed class WinterfallShopStockRelicTests // 금속 손잡이와 잿빛 도장의 상점 검증
{ // 상점 유물 검사 시작
    private const BindingFlags HiddenInstance = BindingFlags.Instance | BindingFlags.NonPublic; // 실제 내부 재고 메서드 조회 조건
    private const string RegularOfferId = "SHOP_SHEET_HEAVY_STRIKE"; // 실제 팔십 골드 공용 카드 상품

    [UnityTest] // 표시 금액과 결제 및 회차 상태 검사
    public IEnumerator DrawerHandleDiscountSurvivesFailuresAndServicesThenSpendsOnce() // 첫 유료 상품 구매의 실결제와 저장 확인
    { // 금속 손잡이 검사 시작
        RequireIsolatedPlay(); // 실제 저장 경로와 분리 확인
        yield return new EnterPlayMode(); // 정식 상점 관리자 준비 환경 진입
        yield return DrawerHandleDiscountSurvivesFailuresAndServicesThenSpendsOnceInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 손잡이 검사 회차 종료
    } // 금속 손잡이 검사 종료

    private IEnumerator DrawerHandleDiscountSurvivesFailuresAndServicesThenSpendsOnceInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var shop = PrepareShop(); // 실제 두 명 편성의 상점 구성
        var catalog = shop.Catalog; // 실제 상점의 공용 상품 원본 조회
        var regular = catalog.Offers.Single(offer => offer.OfferId == RegularOfferId); // 실결제 팔십 골드 공용 카드 조회
        var owner = PartyLoadoutRuntime.Active.Party.Members[1]; // 실제 지불 뒤 카드를 받을 둘째 아군
        var wallet = PlayerResourceManager.EnsureInstance(); // 결제에 연결된 실제 탐사 골드
        var view = ShopPrototypeView.Create(shop); // 실제 상품과 확인 UI 생성
        string path = TestPath(); // 저장 복원용 독립 파일 경로
        try // 상점과 저장 파일 정리 보장
        { // 첫 결제 흐름 확인 시작
            Hold("MetalDrawerHandle"); // 정식 금속 서랍손잡이 보유
            shop.BeginRoomVisit("day244-drawer-start"); // 할인 적용 첫 상점 방 방문
            Assert.AreEqual(76, shop.GetOfferPrice(regular)); // 팔십 골드의 오 퍼센트 할인 확인
            wallet.RestoreResources(75, 0, 0, 0); // 할인가보다 한 골드 적은 잔액 준비
            Assert.AreEqual(ShopPurchaseResult.InsufficientGold, shop.TryPurchase(regular, owner)); // 실제 부족한 상품 구매 시도
            Assert.AreEqual(75, wallet.Gold); // 실패 거래의 재화 보존
            Assert.IsFalse(shop.IsPurchased(regular)); // 미결제 상품의 판매 여부 보존
            Assert.AreEqual(76, shop.GetOfferPrice(regular)); // 결제 실패 뒤 남아 있는 한 번 할인
            while (shop.RunDeck.CardCount <= RunDeckManager.MinimumCardsAfterRemoval) // 카드 한 장 서비스 제거 가능 조건 확인
            { // 검사 제거 카드 준비 시작
                Assert.IsTrue(shop.RunDeck.TryAddCard(shop.RunDeck.Cards[0].Card, owner)); // 실제 회차 덱에 제거 여유 카드 추가
            } // 검사 제거 카드 준비 종료
            wallet.RestoreResources(150, 0, 0, 0); // 정상 카드 제거 서비스 금액 준비
            Assert.AreEqual(150, shop.GetCardRemovalPrice()); // 손잡이 할인 대상 외 서비스 원가 확인
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryRemoveCard(0)); // 일반 카드 제거 서비스로 실제 결제
            Assert.AreEqual(0, wallet.Gold); // 서비스 결제의 정상 비용 확인
            Assert.AreEqual(76, shop.GetOfferPrice(regular)); // 유료 카드 제거 뒤에도 상품 할인 유지
            wallet.RestoreResources(76, 0, 0, 0); // 첫 상품 표시 금액과 같은 잔액 준비
            Assert.IsTrue(view.TryOpenFromRoom("day244-drawer-start")); // 같은 상점 방의 실제 메뉴 열기
            Assert.AreEqual("76 Gold", OfferPriceText(view, regular)); // 실제 상품 목록의 할인 가격 확인
            CompleteShownCardPurchase(view, regular, owner, 76); // 확인 창과 소유자 선택의 같은 실결제 가격
            Assert.AreEqual(0, wallet.Gold); // 표시된 가격과 실제 차감액 일치 확인
            Assert.IsTrue(shop.IsPurchased(regular)); // 실제 유료 상품 판매 성공 확인
            Assert.AreSame(owner, shop.RunDeck.Cards.Last().Owner); // 선택한 아군의 실제 상품 지급 확인
            Click(view.transform.Find("ShopOverlay/ShopPanel/CloseButton")); // 현재 상품판을 정상 닫기
            shop.BeginRoomVisit("day244-drawer-next"); // 같은 회차의 별도 상점 방문
            Assert.AreEqual(80, shop.GetOfferPrice(regular)); // 첫 성공 결제 뒤 다음 상점의 원래 가격
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 소모한 할인 상태를 실제 탐사에 저장
            shop.ResetSession(); // 메모리에서 상점과 할인 상태 초기화
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 실제 저장에서 회차 상점 복원
            Assert.AreEqual(80, shop.GetOfferPrice(regular)); // 저장 복원 뒤에도 재할인 미발생 확인
            shop.ResetSession(); // 새 상점 회차를 시작하는 초기화 진입점 실행
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 승계된 카드와 상점 원본 연결
            shop.BeginRoomVisit("day244-drawer-new-run"); // 새 회차의 첫 상점 방문
            Assert.AreEqual(76, shop.GetOfferPrice(regular)); // 새 회차의 첫 상품 할인 재준비 확인
        } // 첫 결제 흐름 확인 종료
        finally // 임시 시각 자료와 독립 저장 정리
        { // 상점 검사 정리 시작
            Object.Destroy(view.gameObject); // 정식 상점 UI와 일시 정지 해제
            ExplorationSaveService.Remove(path); // 임시 저장과 자동 백업 제거
        } // 상점 검사 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 무료 상품과 할인 내림 경계 검사
    public IEnumerator DrawerHandleCountsPaidProductEvenWhenTheDiscountRoundsToZero() // 무료 상품 보존과 소액 결제 소모 대조
    { // 소액 상품 거래 검사 시작
        RequireIsolatedPlay(); // 실제 저장 경로와 분리 확인
        yield return new EnterPlayMode(); // 실제 상품 거래 검사 환경 진입
        yield return DrawerHandleCountsPaidProductEvenWhenTheDiscountRoundsToZeroInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 소액 거래 검사 회차 종료
    } // 소액 상품 거래 검사 종료

    private IEnumerator DrawerHandleCountsPaidProductEvenWhenTheDiscountRoundsToZeroInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var shop = PrepareShop(); // 공용 카드가 있는 실제 상점 준비
        var catalog = Object.Instantiate(shop.Catalog); // 가격 변경을 위한 임시 카탈로그 사본
        try // 임시 카탈로그 정리 보장
        { // 소액과 무료 결제 대조 시작
            var free = catalog.Offers.Single(offer => offer.OfferId == "SHOP_SHEET_CLEANSE"); // 독립 무료 거래로 사용할 공용 카드
            var cheap = catalog.Offers.Single(offer => offer.OfferId == "SHOP_SHEET_BLADE_STORM"); // 독립 소액 상품으로 사용할 공용 카드
            var regular = catalog.Offers.Single(offer => offer.OfferId == RegularOfferId); // 첫 구매 추가 할인 비교 상품
            JsonUtility.FromJsonOverwrite("{\"price\":0}", free); // 첫 구매를 소비하지 않을 무료 상품 가격
            JsonUtility.FromJsonOverwrite("{\"price\":19}", cheap); // 오 퍼센트 할인액을 내리면 영인 소액 상품 가격
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 수정된 유효 상품과 회차 덱 연결
            Hold("MetalDrawerHandle"); // 정식 첫 상품 할인 유물 획득
            shop.BeginRoomVisit("day244-drawer-small-price"); // 분리 상품으로 검사할 방 진입
            var wallet = PlayerResourceManager.EnsureInstance(); // 실제 골드 차감 대상
            var owner = PartyLoadoutRuntime.Active.Party.Members[0]; // 정상 공용 카드 소유자 지정
            wallet.RestoreResources(19, 0, 0, 0); // 소액 상품 가격과 같은 잔액 준비
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(free, owner)); // 무료 상품의 정상 지급 확인
            Assert.AreEqual(19, wallet.Gold); // 무료 거래에서 잔액 유지 확인
            Assert.AreEqual(76, shop.GetOfferPrice(regular)); // 무료 상품 직후에도 손잡이 할인 유지
            Assert.AreEqual(19, shop.GetOfferPrice(cheap)); // 소액 상품의 할인액 내림 확인
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(cheap, owner)); // 소액 유료 상품의 실제 성공 확인
            Assert.AreEqual(0, wallet.Gold); // 소액 상품의 정상 골드 차감 확인
            Assert.AreEqual(80, shop.GetOfferPrice(regular)); // 실제 첫 유료 상품 뒤 추가 할인의 소모 확인
        } // 소액과 무료 결제 대조 종료
        finally // 가격을 수정한 독립 원본 정리
        { // 검사 카탈로그 정리 시작
            shop.ResetSession(); // 임시 카탈로그의 실제 상점 연결 해제
            Object.Destroy(catalog); // 수정한 검사 전용 카탈로그 제거
        } // 검사 카탈로그 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 실제 진열 확률과 판매 완료 보존 검사
    public IEnumerator AshStampAddsOneSpecialOfferAndKeepsItSoldAcrossSave() // 방 고정 확률과 실제 소유자 선택 보존 확인
    { // 잿빛 도장 검사 시작
        RequireIsolatedPlay(); // 실제 저장 경로와 분리 확인
        yield return new EnterPlayMode(); // 정상 상점 조작 환경 준비
        yield return AshStampAddsOneSpecialOfferAndKeepsItSoldAcrossSaveInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 도장 검사 회차 종료
    } // 잿빛 도장 검사 종료

    private IEnumerator AshStampAddsOneSpecialOfferAndKeepsItSoldAcrossSaveInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var shop = PrepareShop(); // 실제 편성과 상점 카탈로그 준비
        var owner = PartyLoadoutRuntime.Active.Party.Members[1]; // 특수 상품을 받을 실제 둘째 아군
        var wallet = PlayerResourceManager.EnsureInstance(); // 구매 차감에 사용하는 실제 골드
        var view = ShopPrototypeView.Create(shop); // 특수 상품의 실제 확인 UI 생성
        string path = TestPath(); // 방별 판매 기록의 독립 저장 파일
        try // 상점과 저장 파일 정리 보장
        { // 결정적 진열과 판매 검사 시작
            string missedBeforeHold = RoomWithRoll(24, "stamp-before-hold"); // 미보유 상태에 처음 열 성공 경계 방
            string failed = RoomWithRoll(25, "stamp-failure"); // 도장 보유 중 첫 실패 경계 방
            string succeeded = RoomWithRoll(24, "stamp-success"); // 도장 보유 중 마지막 성공 경계 방
            shop.BeginRoomVisit(missedBeforeHold); // 도장을 얻기 전에 성공 수치 방 방문
            Assert.IsEmpty(SpecialStock(shop)); // 미보유 상태의 정식 진열 확인
            var seal = Hold("AshStamp"); // 실제 잿빛 도장 획득
            Assert.AreEqual(25, seal.TriggerChancePercent); // 시트 해석의 확정 확률 확인
            shop.BeginRoomVisit(failed); // 도장을 보유한 실패 수치 방 방문
            Assert.IsEmpty(SpecialStock(shop)); // 추첨값 이십오의 실패 확인
            shop.BeginRoomVisit(missedBeforeHold); // 도장 획득 뒤 기존 상점 재방문
            Assert.IsEmpty(SpecialStock(shop)); // 실제 첫 진열 판정의 고정 확인
            shop.BeginRoomVisit(succeeded); // 새 상점의 성공 수치 진열
            var special = SpecialStock(shop).Single(); // 일반 보상 원본 중 실제 추가된 카드 한 장
            Assert.AreEqual("특수", CardSheetCatalog.Load().FindByName(special.Card.DisplayName).Rarity); // 정식 카드 시트의 특수 희귀도 확인
            Assert.IsTrue(special.RequiresOwnerChoice); // 선택할 파티원이 있는 실제 카드 상품 확인
            Assert.AreEqual(0, special.Price); // 기존 보물 원본의 무료 지급 가격 보존
            Assert.AreEqual(100, shop.GetOfferPrice(special)); // 추가 상점에서만 적용되는 백 골드 값
            Assert.IsTrue(RelicRunManager.Instance.TryRemove(seal.RelicId)); // 성공 진열 뒤 도장 보유 해제
            shop.BeginRoomVisit(failed); // 이전 확률 실패 상점 재방문
            Assert.IsEmpty(SpecialStock(shop)); // 실패한 방의 고정 결과 확인
            shop.BeginRoomVisit(succeeded); // 도장을 제거한 뒤 성공 상점 재방문
            Assert.AreSame(special, SpecialStock(shop).Single()); // 첫 방문 때 고른 카드 원본 유지
            wallet.RestoreResources(100, 0, 0, 0); // 실제 특수 상품의 표시된 정상 가격 준비
            Assert.AreNotEqual(ShopPurchaseResult.Success, shop.TryPurchase(special)); // 소유자 선택 없는 직접 결제 차단
            Assert.AreEqual(100, wallet.Gold); // 소유자 누락 거래의 재화 보존
            Assert.IsTrue(view.TryOpenFromRoom(succeeded)); // 추가 상품이 있는 실제 상점 UI 진입
            Assert.AreEqual("100 Gold", OfferPriceText(view, special)); // 특수 진열 카드의 실제 판매가 확인
            CompleteShownCardPurchase(view, special, owner, 100); // 실제 특수 카드 확인과 소유자 선택 완료
            Assert.AreEqual(0, wallet.Gold); // 특수 상품의 정확한 결제 금액 확인
            Assert.AreSame(special.Card, shop.RunDeck.Cards.Last().Card); // 추첨한 실제 카드 한 장 지급 확인
            Assert.AreSame(owner, shop.RunDeck.Cards.Last().Owner); // 실제 선택한 둘째 소유자 적용 확인
            Assert.AreEqual("판매 완료", OfferPriceText(view, special)); // 성공한 추가 상품의 화면 품절 상태
            Click(view.transform.Find("ShopOverlay/ShopPanel/CloseButton")); // 화면 닫기와 정상 속도 복원
            shop.BeginRoomVisit(failed); // 품절 방과 다른 상점에서 저장 조건 준비
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 이전 방문 방의 판매 기록과 카드 저장
            shop.ResetSession(); // 참가중 상점과 예전 방 진열 초기화
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 진열에 사용한 도장 없이 탐사 저장 복원
            shop.BeginRoomVisit(succeeded); // 복원 뒤 앞서 구입한 상점 다시 방문
            Assert.IsEmpty(SpecialStock(shop)); // 구매한 특수 카드의 중복 재진열 차단
            Assert.IsTrue(shop.IsPurchased(special)); // 실제 방의 품절 결제 기록 복원 확인
            Assert.AreEqual(ShopPurchaseResult.AlreadyPurchased, shop.TryPurchase(special, owner)); // 판매 완료 상품의 반복 구매 차단
            var restored = shop.RunDeck.Cards.Last(); // 복원된 실제 카드 항목 조회
            Assert.AreSame(special.Card, restored.Card); // 구매한 특수 카드 원본 복원 확인
            Assert.AreEqual(owner.CharacterId, restored.Owner.CharacterId); // 선택한 파티원 소유 복원 확인
        } // 결정적 진열과 판매 검사 종료
        finally // 기존 사용자 데이터와 분리한 검사 자료 정리
        { // 특수 상점 정리 시작
            Object.Destroy(view.gameObject); // 검사 상점 화면 정리
            ExplorationSaveService.Remove(path); // 임시 저장 파일과 자동 백업 제거
        } // 특수 상점 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 이전 형식의 실제 탐사 파일 복원 검사
    public IEnumerator PreviousSaveDefaultsToUnusedDiscountAndNoSpecialStock() // 누락된 신규 필드의 호환 기본값 대조
    { // 이전 상점 저장 검사 시작
        RequireIsolatedPlay(); // 실제 저장 경로와 분리 확인
        yield return new EnterPlayMode(); // 정상 탐사 저장 환경 진입
        yield return PreviousSaveDefaultsToUnusedDiscountAndNoSpecialStockInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 구버전 검사 회차 종료
    } // 이전 상점 저장 검사 종료

    private IEnumerator PreviousSaveDefaultsToUnusedDiscountAndNoSpecialStockInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var shop = PrepareShop(); // 실제 저장 가능한 파티와 상점 구성
        Hold("MetalDrawerHandle"); // 이전 형식의 상점 할인 비교 유물 준비
        Hold("AshStamp"); // 기존에 연 방의 진열 유지 비교 유물 준비
        var regular = shop.Catalog.Offers.Single(offer => offer.OfferId == RegularOfferId); // 일반 카드의 원래 거래 가격 조회
        var owner = PartyLoadoutRuntime.Active.Party.Members[0]; // 첫 유료 상품의 실제 소유자
        string room = RoomWithRoll(24, "stamp-legacy"); // 기본값을 생략할 기존 성공 판정 방
        string path = TestPath(); // 구버전 형태를 만들 독립 탐사 저장
        try // 임시 저장 정리 보장
        { // 실제 신규 필드 제거 후 저장 복원 시작
            shop.BeginRoomVisit(room); // 판매 전에 실제 특수 카드가 진열된 방 방문
            Assert.AreEqual(1, SpecialStock(shop).Length); // 제거 대상인 새 진열 필드의 값 준비
            PlayerResourceManager.EnsureInstance().RestoreResources(76, 0, 0, 0); // 손잡이 할인을 실제로 사용할 잔액 준비
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(regular, owner)); // 사용 상태가 저장될 첫 상품 구매 성공
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 현재 형식으로 실제 탐사 저장 생성
            string current = File.ReadAllText(path); // 실제 저장 JSON 원문 조회
            var sheet = CardSheetCatalog.Load(); // 실제 보상 카드의 시트 희귀도 조회
            var ordinaryReward = shop.Catalog.Offers.First(offer => offer.RewardOnly && sheet.FindByName(offer.Card.DisplayName)?.Rarity != "특수"); // 잘못 저장된 백 골드 진열에 쓸 일반 보상 원본
            string invalid = Regex.Replace(current, @"(""specialCardOfferId""\s*:\s*"")[^""]+("")", "${1}" + ordinaryReward.OfferId + "${2}"); // 기존 등록 상품이지만 특수 카드가 아닌 잘못된 재고 재현
            Assert.AreNotEqual(current, invalid); // 신규 특수 재고 변경 지점의 존재 확인
            File.WriteAllText(path, invalid); // 잘못된 추가 진열 원본을 가진 파일 기록
            Assert.IsFalse(ExplorationSaveService.Load(path)); // 일반 보상은 특수 카드 판매 원본으로 복원 불가 확인
            string legacy = Regex.Replace(current, @"""shopDrawerDiscountUsed""\s*:\s*true\s*,", string.Empty); // 구버전 저장에 없는 회차 할인 사용 필드 제거
            legacy = Regex.Replace(legacy, @",\s*""specialCardOfferId""\s*:\s*(?:""[^""]*""|null)\s*,\s*""specialCardSold""\s*:\s*(?:true|false)", string.Empty); // 구버전에 없는 방별 특수 진열과 품절 필드 제거
            StringAssert.DoesNotContain("shopDrawerDiscountUsed", legacy); // 신규 할인 필드 누락 확인
            StringAssert.DoesNotContain("specialCardOfferId", legacy); // 신규 진열 필드 누락 확인
            StringAssert.DoesNotContain("specialCardSold", legacy); // 신규 품절 필드 누락 확인
            File.WriteAllText(path, legacy); // 새 필드 없이 실제 저장 파일 기록
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 기존 버전 검사와 정상 게임 복원 실행
            Assert.IsTrue(shop.IsPurchased(regular)); // 기존 저장의 일반 판매 상태 유지 확인
            Assert.IsEmpty(SpecialStock(shop)); // 기존 진열 방의 특수 카드 없는 기본값 확인
            shop.BeginRoomVisit(RoomWithRoll(25, "stamp-legacy-other")); // 후속 첫 방문 방에 대한 영향 확인
            Assert.AreEqual(76, shop.GetOfferPrice(regular)); // 다음 상점의 구매 가능한 상품에서 미사용 기본값 확인
            shop.BeginRoomVisit(room); // 구버전에서 복원한 기존 상점 방 재방문
            Assert.IsEmpty(SpecialStock(shop)); // 기존 방의 새 확률 재추첨 방지 확인
        } // 실제 신규 필드 제거 후 저장 복원 종료
        finally // 구버전 검사 저장 정리
        { // 구버전 저장 제거 시작
            ExplorationSaveService.Remove(path); // 독립 저장과 백업 제거
        } // 구버전 저장 제거 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    private static ShopRunManager PrepareShop() // 실제 두 명 편성과 저장 가능한 상점 구성
    { // 회차 상점 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 탐사 출전 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 유효 출전 편성 준비
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 두 명의 실제 파티원 편성
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 카드와 소유자를 포함한 실제 출전 덱 생성
        var allies = BattleResultManager.EnsureInstance(); // 탐사 저장의 실제 파티 상태 관리자
        allies.ResetSavedPartyState(); // 독립적인 아군 체력 상태 초기화
        Assert.IsTrue(allies.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 실제 출전 편성의 탐사 파티 등록
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 실제 시작 카드에 맞춘 회차 덱 설정
        var session = ExplorationSessionManager.EnsureInstance(); // 정상 저장에 필요한 탐사 세션 조회
        session.ResetExploration(); // 새로운 탐사 회차의 기록 준비
        session.SetCurrentFloorSeed(244); // 저장 복원에 필요한 고정 층 시드
        var shop = ShopRunManager.EnsureInstance(); // 실제 상점 런타임 관리자 준비
        shop.ResetSession(); // 이전 방문과 거래 및 진열 기록 초기화
        Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), PartyLoadoutRuntime.Active.Deck)); // 정식 상품 원본과 실제 회차 덱 연결
        return shop; // 실제 거래와 표시 검증에 쓸 상점 반환
    } // 회차 상점 준비 종료

    private static void CompleteShownCardPurchase(ShopPrototypeView view, ShopOfferData offer, CharacterData owner, int price) // 실제 구매 확인과 선택한 소유자 결제
    { // 같은 가격을 확인하는 상점 입력 시작
        Click(Content(view).Find("Offer_" + offer.OfferId)); // 정식 상품 버튼으로 확인 화면 열기
        var quote = view.GetComponentsInChildren<TMP_Text>().Single(text => text.name == "PurchasePrice"); // 실제 구입 확인 화면 가격 문구 조회
        StringAssert.Contains($"{price} Gold", quote.text); // 상품 목록과 같은 확인 창 실제 금액 대조
        var confirm = view.GetComponentsInChildren<Button>().Single(button => button.name == "ConfirmPurchase"); // 정식 구매 확인 버튼 조회
        Assert.IsTrue(confirm.interactable); // 보유 골드에 맞는 구매 가능 조건 확인
        confirm.onClick.Invoke(); // 결제 전에 실제 카드 소유자 선택 화면 열기
        Transform option = Content(view).Find("ChooseCardOwner_" + owner.CharacterId); // 선택할 파티원의 실제 상품 칸
        Assert.IsNotNull(option); // 캐릭터를 지정할 확인 항목 존재 확인
        Assert.AreEqual($"{price} Gold", option.Find("Price").GetComponent<TMP_Text>().text); // 소유자 선택 순간에도 동일한 결제 표시
        Click(option); // 해당 아군의 실제 구매와 결제
    } // 같은 가격을 확인하는 상점 입력 종료

    private static ShopOfferData[] SpecialStock(ShopRunManager shop) // 정식 화면에 진열되는 보상 전용 추가 상품 조회
    { // 실제 화면의 목록 추출 시작
        var method = typeof(ShopRunManager).GetMethod("CollectStock", HiddenInstance); // 화면의 내부 판매 재고 조회 함수
        Assert.IsNotNull(method); // 정식 판매 목록 조회 경로 존재 확인
        var stock = new object[2]; // 판매 가능과 품절 목록의 출력 공간
        method.Invoke(shop, stock); // 상점 UI와 동일한 기본 재고 계산
        return ((List<ShopOfferData>)stock[0]).Where(offer => offer.RewardOnly).ToArray(); // 현재 판매 가능한 추가 특수 카드 반환
    } // 실제 화면의 목록 추출 종료

    private static string RoomWithRoll(int expected, string prefix) // 실제 고정 확률의 양쪽 경계 방 조회
    { // 상점 추첨 시드 확인 시작
        var roll = typeof(RelicShopPricing).GetMethod("CalculateRange", BindingFlags.NonPublic | BindingFlags.Static); // 정식 상점 방의 고정 난수 함수
        Assert.IsNotNull(roll); // 실제 방 확률 조회 가능 여부 확인
        var arguments = new object[4]; // 고정 확률 계산 인수 공간
        arguments[0] = 0; // 확률 계산의 최소 굴림값
        arguments[1] = 99; // 확률 계산의 최대 굴림값
        arguments[3] = "EXTRA_SPECIAL_CARD_CHANCE"; // 특수 진열 판정의 독립 해시 구분자
        for (int index = 0; index < 12000; index++) // 결정적 경계가 맞는 방 후보 순회
        { // 현재 방 확률 조회 시작
            string room = prefix + "-" + index; // 각 후보 방의 독립 식별자 준비
            arguments[2] = room; // 이번 고정 확률에 필요한 방 지정
            if ((int)roll.Invoke(null, arguments) == expected) // 이십오 퍼센트 경계의 비교값 확인
            { // 비교용 방 반환 시작
                return room; // 같은 값이 재현되는 테스트 상점 방 반환
            } // 비교용 방 반환 종료
        } // 현재 방 확률 조회 종료
        Assert.Fail("비교할 상점 확률 방을 찾을 수 없음"); // 정식 확률 계산에서 입력 준비 실패 표시
        return null; // 테스트 실패 분기의 반환값
    } // 상점 추첨 시드 확인 종료

    private static RelicData Hold(string resourceName) // 정식 유물 원본의 보유함 획득
    { // 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + resourceName); // 정식 유물 저장 원본 조회
        Assert.IsNotNull(relic, resourceName); // 실제 유물 파일 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 보유함 획득 처리
        return relic; // 해당 유물의 원본 반환
    } // 유물 획득 종료

    private static Transform Content(ShopPrototypeView view) => view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 실제 상점 상품 또는 소유자 목록

    private static string OfferPriceText(ShopPrototypeView view, ShopOfferData offer) => Content(view).Find("Offer_" + offer.OfferId + "/Price").GetComponent<TMP_Text>().text; // 실제 상품 칸의 골드 또는 품절 문구

    private static void Click(Transform node) // 실제 상품과 서비스 버튼 입력
    { // 상점 클릭 시작
        Assert.IsNotNull(node); // 지정 상점 입력 요소 존재 확인
        node.GetComponent<Button>().onClick.Invoke(); // 실제 등록된 상점 입력 실행
    } // 상점 클릭 종료

    private static string TestPath() => Path.Combine(Application.temporaryCachePath, "day244-shop-" + Guid.NewGuid().ToString("N") + ".json"); // 사용자 저장과 분리된 독립 탐사 파일

    private static void RequireIsolatedPlay() // 사용자의 실제 저장을 건드리지 않는 검증 환경 확인
    { // 독립 Unity 검증 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 격리된 사용자 저장 경로 확인
        { // 일반 플레이 저장 보호 시작
            Assert.Ignore("저장 경로가 분리된 검증 환경에서 실행"); // 실제 사용자 데이터와 분리된 회차 검사 지정
        } // 일반 플레이 저장 보호 종료
    } // 독립 Unity 검증 검사 종료
} // 상점 유물 검사 종료
