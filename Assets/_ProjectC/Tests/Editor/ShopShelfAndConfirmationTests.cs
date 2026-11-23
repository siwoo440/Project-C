using System; // 시험 파일과 상태 조건 사용
using System.Collections; // 실제 장면 초기화 대기
using System.Collections.Generic; // 중복 상품 시험 목록
using System.IO; // 시험 저장 경로
using System.Linq; // 활성 상품 순서 비교
using System.Reflection; // 구버전 판매 번호와 카탈로그 재현
using NUnit.Framework; // 실제 거래 결과 검사
using TMPro; // 읽을 수 있는 효과와 금액 확인
using UnityEngine; // 실제 상품 원본과 화면 조회
using UnityEngine.SceneManagement; // 게임 시작 장면
using UnityEngine.TestTools; // 실제 플레이 검사
using UnityEngine.UI; // 상품과 확인 버튼 조작
using Object = UnityEngine.Object; // 게임 객체 조회

public sealed class ShopShelfAndConfirmationTests // 실제 상품 진열과 구매 확인 검사
{ // 상점 검사 시작
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic; // 시험용 원본 필드 접근 범위

    [UnityTest] // 실제 UI와 저장 복원 검사
    public IEnumerator ShopShowsPreviewChargesOnceAndKeepsSoldStockAtBottom() // 구매 취소와 실제 결제 및 품절 복원 확인
    { // 실제 거래 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실사용자 저장 경로 보호
        { // 사용자 프로필 제외 시작
            Assert.Ignore("자동 저장이 분리된 ProjectCVerification 검증 사본에서 실행"); // 검증용 프로필에서만 실행
        } // 사용자 프로필 제외 종료
        yield return new EnterPlayMode(); // 실제 게임 시작
        yield return PrepareShop(); // 실제 파티와 탐사 상점 준비
        yield return VerifyTransactions(); // 상점 버튼과 영수 기록 검증
        yield return new ExitPlayMode(); // 실제 게임 종료
    } // 실제 거래 검사 종료

    private static IEnumerator PrepareShop() // 기본 판매 카탈로그와 실제 플레이어 준비
    { // 상점 준비 시작
        SceneManager.LoadScene("00_Boot"); // 게임 초기화와 메뉴 장면 시작
        yield return WaitUntil(() => Object.FindFirstObjectByType<MainMenuController>() != null && !SceneFlowManager.Instance.IsLoadingScene); // 실제 메뉴 준비 대기
        PartyDepartureTestSupport.StartExploration(0, 1); // 두 명 파티로 탐사 출발
        yield return WaitUntil(() => Object.FindFirstObjectByType<ShopPrototypeView>() != null && Object.FindFirstObjectByType<ExplorationPlayerController>() != null && !SceneFlowManager.Instance.IsLoadingScene); // 상점과 플레이어 생성 완료
    } // 상점 준비 종료

    private static IEnumerator VerifyTransactions() // 화면 입력과 실제 보관함 비교
    { // 상품 거래 확인 시작
        var view = Object.FindFirstObjectByType<ShopPrototypeView>(); // 실제 상점 화면 조회
        var shop = ShopRunManager.Instance; // 실제 상점 재고 조회
        var inventory = RelicRunManager.EnsureInstance().Inventory; // 현재 보유 유물
        var catalog = shop.Catalog; // 등록된 원본 확인
        var compass = catalog.Offers.Single(item => item.OfferId == "SHOP_RLC_CRACKED_COMPASS"); // 구매할 나침반
        var hidden = catalog.Offers.Single(item => item.OfferId == "SHOP_RLC_BLOODSTAINED_SYRINGE"); // 다른 경로로 얻은 유물
        var potion = catalog.Offers.Single(item => item.OfferId == "SHOP_POTION_HEAL"); // 실제 포션
        var card = catalog.Offers.Single(item => item.OfferId == "SHOP_SHEET_HEAVY_STRIKE"); // 소유자를 고르는 공용 카드
        var room = "shop-shelf-confirmation"; // 동일 상점 복원 기준 방
        var resources = PlayerResourceManager.EnsureInstance(); // 실제 게임 지갑
        var unvisitedPath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 첫 상점 방문 전 탐사 저장 경로
        try // 방문 전 독립 저장 정리 보장
        { // 아직 상점을 방문하지 않은 탐사의 복원 확인 시작
            Assert.IsTrue(string.IsNullOrEmpty(shop.ActiveRoomRuntimeId)); // 최초 방문 기록이 비어 있는 출발 상태 확인
            Assert.IsTrue(ExplorationSaveService.Save(unvisitedPath, Object.FindFirstObjectByType<ExplorationPlayerController>().transform.position)); // 최초 방문 전 탐사 상태 저장
            Assert.IsTrue(ExplorationSaveService.Load(unvisitedPath)); // 현재 탐사 장면에서 첫 방문 전 저장 불러오기
            Assert.IsTrue(shop.IsReady); // 기존 장면에서도 다음 상점에 필요한 카탈로그 준비 확인
            Assert.IsTrue(string.IsNullOrEmpty(shop.ActiveRoomRuntimeId)); // 이전에 방문하지 않은 방을 생성하지 않음 확인
        } // 이전 탐사 상태 복원 확인 종료
        finally // 검사 전용 저장만 정리
        { // 임시 저장 자료 정리 시작
            ExplorationSaveService.Remove(unvisitedPath); // 첫 방문 전 임시 저장과 복구본 제거
        } // 임시 저장 자료 정리 종료
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(hidden.Relic)); // 상점 밖에서 얻은 실제 유물
        Assert.IsTrue(view.TryOpenFromRoom(room)); // 실제 상점 창 열기
        var content = view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 화면의 상품 목록
        var initial = Rows(content); // 이 방의 최초 상품과 서비스 순서
        Assert.IsFalse(initial.Contains("Offer_" + hidden.OfferId)); // 다른 경로로 보유한 유물 제외
        var external = catalog.Offers.Single(item => item.OfferId == "SHOP_RLC_WORN_DIE"); // 상점 화면이 열린 동안 지급될 유물
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(external.Relic)); // 현재 방의 구매 기록이 없는 외부 획득
        yield return null; // 보관함 변경의 진열 갱신 대기
        Assert.IsFalse(Rows(content).Contains("Offer_" + external.OfferId)); // 열린 상점에서도 추가 보유 유물 즉시 제외
        Assert.IsTrue(inventory.TryRemove(external.Relic.RelicId)); // 외부 유물 제거
        yield return null; // 실제 보유 목록의 변경 반영
        CollectionAssert.AreEqual(initial, Rows(content)); // 판매 기록 없는 유물의 원래 위치 복원
        int cost = shop.GetOfferPrice(compass); // 실제 방에 고정된 시트 가격
        Assert.IsTrue(resources.TrySpend(resources.Gold, 0, 0, 0)); // 잔액 부족 상태 준비
        ButtonAt("Offer_" + compass.OfferId).onClick.Invoke(); // 유물의 가격 확인 창 열기
        Assert.AreEqual(compass.Relic.Description, Label("ItemEffect")); // 실제 유물 효과 표시
        var icon = view.transform.Find("ShopOverlay/PurchaseConfirmation/PurchaseFrame/PurchaseBody/ItemFrame/ItemIcon"); // 앞쪽 구매 창의 상품 원화 조회
        Assert.AreEqual(compass.Icon, icon.GetComponent<Image>().sprite); // 실제 선택 유물의 아이콘 표시
        Assert.AreEqual($"구매 가격   {cost:N0} Gold", Label("PurchasePrice")); // 목록과 같은 고정 가격
        Assert.IsFalse(ButtonAt("ConfirmPurchase").interactable); // 골드 부족 시 구매 비활성
        Assert.IsTrue(Label("PurchaseState").Contains("부족")); // 부족 사유 표시
        Assert.IsFalse(inventory.ContainsRelic(compass.Relic.RelicId)); // 상세 열기만으로 지급되지 않음
        resources.AddResources(2000, 0, 0, 0); // 열린 창의 실제 지갑 증가
        Assert.AreEqual($"2,000 → {2000 - cost:N0}", Label("BalancePreview")); // 구매 후 예상 잔액 갱신
        Assert.IsTrue(ButtonAt("ConfirmPurchase").interactable); // 충분한 골드로 변경 시 활성화
        ButtonAt("CancelPurchase").onClick.Invoke(); // 유물 상세 취소
        Assert.AreEqual(2000, shop.Gold); // 취소 시 지갑 보존
        CollectionAssert.AreEqual(initial, Rows(content)); // 취소 시 상품 칸 순서 보존
        ButtonAt("Offer_" + compass.OfferId).onClick.Invoke(); // 같은 유물 확인 재개
        var sameButton = ButtonAt("ConfirmPurchase"); // 빠른 두 번째 클릭의 기존 버튼
        sameButton.onClick.Invoke(); // 실제 결제 확정
        sameButton.onClick.Invoke(); // 같은 클릭 재전달
        Assert.AreEqual(2000 - cost, shop.Gold); // 중복 결제 방지
        Assert.AreEqual(1, inventory.OwnedRelics.Count(item => item.RelicId == compass.Relic.RelicId)); // 유물 한 개 지급
        Assert.AreEqual("Offer_" + compass.OfferId, Rows(content).Last()); // 판매와 동시에 맨 아래로 이동
        Assert.IsFalse(ButtonAt("Offer_" + compass.OfferId).interactable); // 품절 칸 재입력 차단
        Assert.IsTrue(Label("ResultText").Contains(compass.DisplayName)); // 실제 거래 결과 표시 유지
        var potions = ConsumableRunManager.EnsureInstance().Inventory; // 실제 소모품 슬롯
        for (int index = 0; index < ConsumableInventoryRuntime.SlotCount; index++) // 소모품 전체 슬롯 채우기
        { // 포션 슬롯 준비 시작
            Assert.IsTrue(potions.TrySetForDebug(index, potion.Potion)); // 실제 슬롯에 포션 보관
        } // 포션 슬롯 준비 종료
        ButtonAt("Offer_" + potion.OfferId).onClick.Invoke(); // 막힌 포션의 효과와 사유 확인
        Assert.AreEqual(potion.Potion.Description, Label("ItemEffect")); // 실제 포션 설명 표시
        Assert.IsFalse(ButtonAt("ConfirmPurchase").interactable); // 보관함 포화 시 결제 차단
        Assert.IsTrue(Label("PurchaseState").Contains("슬롯")); // 정확한 보관 불가 사유 표시
        Assert.IsTrue(potions.TryRemoveAt(0)); // 확인 창에서 빈 공간 발생
        Assert.IsTrue(ButtonAt("ConfirmPurchase").interactable); // 지급 가능 상태 재검증
        ButtonAt("ConfirmPurchase").onClick.Invoke(); // 실제 포션 한 번 구매
        Assert.AreEqual(ConsumableInventoryRuntime.SlotCount, potions.Count); // 지불과 포션 지급 확인
        int beforeCard = shop.Gold; // 공용 카드 확인 전 잔액
        int deckCount = shop.RunDeck.CardCount; // 공용 카드 확인 전 덱 수량
        ButtonAt("Offer_" + card.OfferId).onClick.Invoke(); // 공용 카드의 설명 먼저 열기
        Assert.AreEqual(card.Card.Description, Label("ItemEffect")); // 실제 카드 효과 표시
        Assert.IsNull(GameObject.Find("ChooseCardOwner_ARC_00")); // 구매를 누르기 전 소유자 창 없음
        ButtonAt("ConfirmPurchase").onClick.Invoke(); // 가격 확인 후 실제 소유자 선택
        Assert.AreEqual(beforeCard, shop.Gold); // 소유자 확정 전 비용 보존
        ButtonAt("BackToProducts").onClick.Invoke(); // 파티원 선택 취소
        Assert.AreEqual(deckCount, shop.RunDeck.CardCount); // 소유자 취소 후 덱 유지
        Assert.IsFalse(shop.IsPurchased(card)); // 취소한 상품 재고 유지
        ButtonAt("Offer_" + card.OfferId).onClick.Invoke(); // 할인 변경 전에 확인할 카드 선택
        ButtonAt("ConfirmPurchase").onClick.Invoke(); // 할인 전 가격에 동의한 소유자 선택
        var badge = catalog.Offers.Single(item => item.OfferId == "SHOP_RLC_CERULLION_BADGE").Relic; // 상점 가격을 실제로 바꾸는 배지
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(badge)); // 확인 뒤 소유자 선택 중 할인 변경
        ButtonAt("ChooseCardOwner_ARC_01").onClick.Invoke(); // 이전 가격으로 결제 시도
        Assert.AreEqual(beforeCard, shop.Gold); // 바뀐 가격에 동의하기 전 차감 방지
        Assert.AreEqual(deckCount, shop.RunDeck.CardCount); // 가격 재확인 중 카드 중복 지급 방지
        Assert.AreEqual($"구매 가격   {shop.GetOfferPrice(card):N0} Gold", Label("PurchasePrice")); // 갱신된 할인 가격으로 확인 창 복귀
        Assert.IsTrue(inventory.TryRemove(badge.RelicId)); // 배지 할인 해제
        Assert.AreEqual("구매 가격   80 Gold", Label("PurchasePrice")); // 열린 확인 창에서 할인 해제 가격 반영
        ButtonAt("CancelPurchase").onClick.Invoke(); // 다시 확인 중인 상품 취소
        ButtonAt("Offer_" + card.OfferId).onClick.Invoke(); // 같은 공용 카드 다시 확인
        ButtonAt("ConfirmPurchase").onClick.Invoke(); // 구매가 확인
        ButtonAt("ChooseCardOwner_ARC_01").onClick.Invoke(); // 두 번째 생존 파티원에게 실제 지급
        Assert.AreEqual(beforeCard - shop.GetOfferPrice(card), shop.Gold); // 실제 카드 가격 차감
        Assert.AreEqual("ARC_01", shop.RunDeck.Cards.Last().Owner.CharacterId); // 실제 골랐던 카드 소유자 확인
        var sold = new[] { compass, potion, card }.Select(item => "Offer_" + item.OfferId).ToArray(); // 실제 판매를 끝낸 원본 세 개
        var expected = initial.Where(name => !sold.Contains(name)).Concat(initial.Where(name => sold.Contains(name))).ToArray(); // 서비스 뒤의 원래 상품 순서를 유지한 품절 목록
        CollectionAssert.AreEqual(expected, Rows(content)); // 구매 시간과 무관한 안정적인 최하단 배치
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(compass.Relic)); // 방금 구입한 유물 되팔기
        Assert.AreEqual(ShopPurchaseResult.AlreadyPurchased, shop.TryPurchase(compass)); // 되팔아도 같은 방에서 재구매 차단
        int savedGold = shop.Gold; // 판매와 구매가 모두 끝난 지갑
        var path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 독립 상점 저장 파일
        try // 독립 저장 정리 보장
        { // 방 재방문과 원본 복원 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, Object.FindFirstObjectByType<ExplorationPlayerController>().transform.position)); // 실제 방과 상품 식별자 저장
            view.SendMessage("CloseShop"); // 화면만 닫고 현재 방 기록 유지
            Assert.IsTrue(view.TryOpenFromRoom(room)); // 같은 방의 창 다시 열기
            CollectionAssert.AreEqual(expected, Rows(content)); // 재입장 순서 유지
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 현재 형식의 저장 파일 복원
            Assert.IsTrue(view.TryOpenFromRoom(room)); // 복원한 같은 방 열기
            CollectionAssert.AreEqual(expected, Rows(content)); // 판매 뒤 유물을 소유하지 않아도 품절 행 복원
            Assert.AreEqual(savedGold, shop.Gold); // 불러오기 재결제 방지
            view.SendMessage("CloseShop"); // 다음 방 준비
            Assert.IsTrue(view.TryOpenFromRoom(room + "-next")); // 새 방의 기본 재고 준비
            Assert.IsTrue(ButtonAt("Offer_" + potion.OfferId).interactable); // 다른 방에서 같은 포션 새로 판매
            Assert.IsTrue(ButtonAt("Offer_" + compass.OfferId).interactable); // 되판 유물을 새 상점에서 구매 가능
            Assert.IsNull(GameObject.Find("Offer_" + hidden.OfferId)); // 계속 보유한 유물은 다른 방에서도 제외
            view.SendMessage("CloseShop"); // 시험 화면 닫기
        } // 방 재방문과 원본 복원 종료
        finally // 저장 파일 정리
        { // 시험 파일 정리 시작
            ExplorationSaveService.Remove(path); // 시험 저장과 복구본 제거
        } // 시험 파일 정리 종료
        yield return null; // 남은 UI 제거 처리
    } // 상품 거래 확인 종료

    [UnityTest] // 구형 중복 유물 상품 번호와 같은 이름의 다른 유물 검사
    public IEnumerator DuplicateRelicOffersShareOneStockAndRestoreTheActualSoldAlias() // 판매 번호와 원본 식별자의 구분 확인
    { // 원본 중복 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 자동 저장 분리 확인
        { // 사용자 저장 보호 시작
            Assert.Ignore("자동 저장이 분리된 ProjectCVerification 검증 사본에서 실행"); // 검증 전용 계정 한정
        } // 사용자 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 회차 시작
        yield return PrepareShop(); // 정상 파티와 상점 준비
        var shop = ShopRunManager.Instance; // 현재 상점 상태 조회
        var originalCatalog = shop.Catalog; // 원래 상점 원본 보관
        var catalog = Object.Instantiate(originalCatalog); // 실물 원본을 보존하는 시험용 카탈로그
        var entries = (List<ShopOfferData>)typeof(ShopCatalogData).GetField("offers", Hidden).GetValue(catalog); // 시험용 판매 목록
        var original = entries.Single(offer => offer.OfferId == "SHOP_RLC_CRACKED_COMPASS"); // 정상 판매 원본
        var alias = JsonUtility.FromJson<ShopOfferData>(JsonUtility.ToJson(original)); // 같은 유물에 연결된 별도 상품
        typeof(ShopOfferData).GetField("offerId", Hidden).SetValue(alias, "SHOP_TEST_ALIAS"); // 구버전에 존재할 수 있는 독립 판매 번호
        entries.Add(alias); // 원본보다 뒤에 별도 상품 등록
        var sameName = Object.Instantiate(original.Relic); // 표시 이름이 같은 별개 유물
        typeof(RelicData).GetField("relicId", Hidden).SetValue(sameName, "RLC_TEST_DIFFERENT"); // 실제 유물 식별자만 다르게 지정
        var other = JsonUtility.FromJson<ShopOfferData>(JsonUtility.ToJson(alias)); // 별개 원본의 상품 만들기
        typeof(ShopOfferData).GetField("offerId", Hidden).SetValue(other, "SHOP_TEST_OTHER"); // 다른 유물의 상품 번호
        typeof(ShopOfferData).GetField("relic", Hidden).SetValue(other, sameName); // 이름이 같은 별개 유물 연결
        entries.Add(other); // 같은 이름의 다른 상품 판매 등록
        var view = Object.FindFirstObjectByType<ShopPrototypeView>(); // 실제 상점 UI
        var content = view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 실제 상품 칸
        string room = "shop-alias-restore"; // 기존 판매 기록에 쓰인 방
        try // 시험 원본 복구 보장
        { // 유물별 한 칸 검사 시작
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 여러 판매 번호를 가진 카탈로그 준비
            Assert.IsTrue(view.TryOpenFromRoom(room)); // 실제 목록 생성
            var initial = Rows(content); // 구입 전 진열 순서 확인
            CollectionAssert.Contains(initial, "Offer_" + original.OfferId); // 같은 유물의 첫 상품 선택
            CollectionAssert.DoesNotContain(initial, "Offer_" + alias.OfferId); // 같은 유물의 별도 상품 제외
            CollectionAssert.Contains(initial, "Offer_" + other.OfferId); // 같은 이름의 다른 유물은 정상 판매
            Assert.AreEqual(ShopPurchaseResult.Unavailable, shop.TryPurchase(alias)); // 숨은 번호의 직접 구매 방지
            view.SendMessage("CloseShop"); // 구형 판매 기록 복원 전 닫기
            var restore = typeof(ShopRunManager).GetMethod("RestoreSession", Hidden); // 실제 저장 복구의 동일 진입점 조회
            restore.Invoke(shop, new object[] { catalog, PartyLoadoutRuntime.Active.Deck, room, new[] { alias.OfferId } }); // 나중에 나오는 별도 상품만 구매한 구형 저장 재현
            Assert.IsTrue(view.TryOpenFromRoom(room)); // 복원한 실제 상품 번호로 목록 생성
            var restored = Rows(content); // 판매된 실제 상품 칸의 배치
            Assert.AreEqual("Offer_" + alias.OfferId, restored.Last()); // 저장된 상품 번호의 품절 칸 최하단 배치
            CollectionAssert.DoesNotContain(restored, "Offer_" + original.OfferId); // 원래 번호의 유물 재등장 방지
            CollectionAssert.Contains(restored, "Offer_" + other.OfferId); // 동명 별개 유물은 구매 가능
            Assert.AreEqual(ShopPurchaseResult.AlreadyPurchased, shop.TryPurchase(original)); // 미소지 상태에서도 동일 재고 중복 결제 방지
            view.SendMessage("CloseShop"); // 이전 상점 닫기
            restore.Invoke(shop, new object[] { catalog, PartyLoadoutRuntime.Active.Deck, room, new[] { alias.OfferId, original.OfferId } }); // 옛 저장이 두 번호를 모두 가진 상태 재현
            Assert.IsTrue(view.TryOpenFromRoom(room)); // 동일 유물 품절 한 칸 복원
            var both = Rows(content); // 중복 판매 기록 표시 확인
            Assert.AreEqual("Offer_" + original.OfferId, both.Last()); // 원본 순서가 앞선 품절 칸 하나만 표시
            CollectionAssert.DoesNotContain(both, "Offer_" + alias.OfferId); // 복수 영수 기록의 중복 칸 제외
        } // 유물별 한 칸 검사 종료
        finally // 시험 원본 정리
        { // 정상 상점 복구 시작
            view.SendMessage("CloseShop"); // 시험 상점 종료
            shop.Prepare(originalCatalog, PartyLoadoutRuntime.Active.Deck); // 실제 원본 카탈로그 복원
            Object.Destroy(catalog); // 임시 카탈로그 제거
            Object.Destroy(sameName); // 임시 동명 유물 제거
        } // 정상 상점 복구 종료
        yield return new ExitPlayMode(); // 실제 게임 종료
    } // 원본 중복 검사 종료

    private static string[] Rows(Transform content) // 화면의 실제 활성 상품 칸 순서
    { // 실제 목록 확인 시작
        return content.Cast<Transform>().Where(item => item.gameObject.activeSelf).Select(item => item.name).ToArray(); // 숨겨진 이전 항목 제외
    } // 실제 목록 확인 종료

    private static Button ButtonAt(string name) // 화면에 열린 버튼 한 개 조회
    { // 버튼 확인 시작
        var target = GameObject.Find(name); // 실제 표시된 버튼 이름 검색
        Assert.IsNotNull(target, name); // 버튼 누락 진단
        return target.GetComponent<Button>(); // 실제 클릭 동작 반환
    } // 버튼 확인 종료

    private static string Label(string name) // 현재 화면에 표시된 한글 문구 조회
    { // 표시 문구 확인 시작
        return GameObject.Find(name).GetComponent<TMP_Text>().text; // 실제 화면 텍스트
    } // 표시 문구 확인 종료

    private static IEnumerator WaitUntil(Func<bool> ready) // 실제 장면 전환 완료 대기
    { // 장면 대기 시작
        float deadline = Time.realtimeSinceStartup + 30f; // 장면 준비 제한 시각
        while (!ready() && Time.realtimeSinceStartup < deadline) // 실제 관리자 준비 확인
        { // 다음 프레임 대기 시작
            yield return null; // 장면 초기화 진행
        } // 다음 프레임 대기 종료
        Assert.IsTrue(ready()); // 시간 내 장면 준비 확인
        yield return null; // 실제 화면의 시작 함수 대기
    } // 장면 대기 종료
} // 상점 검사 종료
