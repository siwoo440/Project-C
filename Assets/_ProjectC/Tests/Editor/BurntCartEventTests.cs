using System; // 고정 조건과 대기 함수 사용
using System.Collections; // 실제 씬 이동 대기
using System.IO; // 독립 저장 경로와 내용 확인
using System.Linq; // 실제 적과 상품 목록 확인
using NUnit.Framework; // 결과 검증
using TMPro; // 실제 가격 문구 확인
using UnityEngine; // 원본 자료와 게임 객체 조회
using UnityEngine.SceneManagement; // 실제 씬 이름 확인
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 선택 버튼 조작
using Object = UnityEngine.Object; // 게임 객체 검색

public sealed class BurntCartEventTests // 수레 선택과 매복 및 표식 상점 검사
{ // 수레 검사 시작
    [Test] // 기획 원본과 추첨 경계 검사
    public void VersionSixAddsAuthoredCartAndSplitsAtSixty() // 새 층 삽화와 상자 추첨 경계 확인
    { // 원본 확인 시작
        var prior = ExplorationEventCatalog.LoadEvents(5); // 수레 추가 전 층 후보
        var current = ExplorationEventCatalog.LoadEvents(6); // 수레 추가 후 층 후보
        CollectionAssert.AreEqual(prior.Select(item => item.EventId).Append(BurntCartEvent.Id).OrderBy(id => id, StringComparer.Ordinal), current.Select(item => item.EventId)); // 기존 층 후보와 새 수레 한 곳 확인
        var cart = current.Single(item => item.EventId == BurntCartEvent.Id); // 실제 원문 사건 조회
        Assert.AreEqual(Resources.Load<Sprite>("Exploration/BurntCart"), cart.IllustrationSprite); // 수레 원화 연결 확인
        Assert.IsNotNull(cart.IllustrationSprite); // 실제 원화 존재 확인
        CollectionAssert.AreEqual(new[] { "상자를 연다", "수레를 해체한다", "표식을 남기고 간다" }, cart.Choices.Select(item => item.ChoiceText)); // 원문 행동 순서 확인
        Assert.IsTrue(BurntCartEvent.HasSupplies("s17")); // 고정 난수 오십구의 마지막 물자 확인
        Assert.IsFalse(BurntCartEvent.HasSupplies("s44")); // 고정 난수 육십의 첫 매복 확인
    } // 원본 확인 종료

    [UnityTest] // 실제 물자와 파티원 해체 경로 검사
    public IEnumerator BoxSalvageCancellationDeathAndLegacySave() // 상자 일회 보상과 생존자 선택 및 전멸 검사
    { // 물자와 해체 검사 시작
        RequireVerificationProfile(); // 자동 저장 격리 확인
        yield return new EnterPlayMode(); // 실제 게임 실행
        yield return PrepareMap(); // 정상 출전과 현재 층 준비
        var session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
        var party = BattleResultManager.Instance; // 현재 파티 체력 조회
        var one = party.ActiveParty.Members[0]; // 해체할 생존자 선택
        var two = party.ActiveParty.Members[1]; // 나머지 파티원 선택
        party.RestoreSavedAllyState(one, 10, 60, 0); // 해체 전 생존자 수치 지정
        party.RestoreSavedAllyState(two, 0, 40, 1); // 선택할 수 없는 사망자 지정
        var wallet = PlayerResourceManager.Instance; // 현재 재화 조회
        Assert.IsTrue(BurntCartEvent.TryResolve("s17", BurntCartEvent.Box, null, out _)); // 물자 상자 확인
        Assert.IsFalse(BurntCartEvent.TryResolve("s17", BurntCartEvent.Box, null, out _)); // 같은 상자의 반복 지급 차단
        CollectionAssert.AreEqual(new[] { 1, 1, 0 }, new[] { wallet.Screw, wallet.IronPlate, wallet.Wire }); // 철판과 나사만 한 개씩 확인
        var panel = OpenCart("salvage"); // 실제 방 선택지 표시
        panel.SendMessage("HandleChoiceSelected", (object)BurntCartEvent.Salvage); // 해체 행동자 목록 열기
        var chooser = panel.transform.Find("TreasureOwnerCanvas/Panel"); // 실제 파티원 버튼 목록 조회
        Assert.IsFalse(chooser.Find("Owner_" + two.CharacterId).GetComponent<Button>().interactable); // 사망자 버튼 비활성 확인
        chooser.Find("CancelButton").GetComponent<Button>().onClick.Invoke(); // 해체 취소
        Assert.IsFalse(session.IsEventResolved("salvage")); // 취소 후 수레 미사용 확인
        Assert.IsFalse(BurntCartEvent.TryResolve("salvage", BurntCartEvent.Salvage, two, out _)); // 사망자 직접 지정 차단
        panel.SendMessage("HandleChoiceSelected", (object)BurntCartEvent.Salvage); // 해체 재선택
        panel.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + one.CharacterId).GetComponent<Button>().onClick.Invoke(); // 첫 생존자의 실제 해체 실행
        Assert.IsTrue(party.TryGetSavedAllyState(one, out int hp, out int mental, out _)); // 해체 후 상태 조회
        CollectionAssert.AreEqual(new[] { 7, 60, 2, 2, 1 }, new[] { hp, mental, wallet.Screw, wallet.IronPlate, wallet.Wire }); // 체력 세 점과 부품 세 종류 확인
        string checkpoint = File.ReadAllText(ExplorationSaveService.DefaultPath); // 해체 후 정상 자동 저장 읽기
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 빈 문자열 매복 필드를 기록한 최신 저장 복원
        var normal = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 복원 직후 일반 전투 원본 조회
        Assert.IsNull(session.Cart.AmbushRoom); // 직렬화된 빈 방과 초기 미예약 상태 일치 확인
        Assert.IsTrue(session.BeginEncounter("empty-cart-normal", normal, Vector3.zero, Vector3.zero)); // 최신 빈 수레 상태 복원 뒤 정상 전투 시작 확인
        string legacy = System.Text.RegularExpressions.Regex.Replace(checkpoint, "\"cart\":\\{[^}]*\\},", string.Empty).Replace("\"eventPoolVersion\":" + ExplorationEventCatalog.CurrentVersion + ",", "\"eventPoolVersion\":5,"); // 이전 층의 수레 필드 없는 저장 재현
        Assert.AreNotEqual(checkpoint, legacy); // 이전 저장 형식 재현 확인
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 독립 이전 저장 경로
        try // 이전 저장 정리 보장
        { // 복원 검사 시작
            File.WriteAllText(path, legacy); // 이전 버전 저장 작성
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 누락된 수레 상태의 정상 복원
            Assert.AreEqual(5, session.EventPoolVersion); // 이전 층 원래 후보 보존 확인
            Assert.IsFalse(session.Cart.HasMark); // 이전 저장 표식 없음 확인
            Assert.IsTrue(session.BeginEncounter("legacy-cart-normal", normal, Vector3.zero, Vector3.zero)); // 구버전의 수레 필드 없이 일반 전투 시작 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 해체 전멸 검사를 위한 전투 전 상태 복원
        } // 복원 검사 종료
        finally // 이전 저장 정리
        { // 정리 시작
            ExplorationSaveService.Remove(path); // 독립 이전 저장 정리
        } // 정리 종료
        party.RestoreSavedAllyState(one, 3, 60, 0); // 마지막 생존자의 해체 경계 지정
        Assert.IsTrue(BurntCartEvent.TryResolve("last", BurntCartEvent.Salvage, one, out _)); // 마지막 생존자 해체 확정
        Assert.IsTrue(session.IsExplorationCompleted && !session.IsExplorationSuccess); // 정상 탐사 전멸 결과 확인
        Assert.AreEqual(0, party.GetLivingAllyCount()); // 남은 생존자 없음 확인
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 물자와 해체 검사 종료

    [UnityTest] // 실제 상점 상품과 소유자 및 재방문 검사
    public IEnumerator MarkPersistsThroughCancelInsufficientGoldRevisitAndFirstPurchase() // 상점 취소와 복원 및 첫 실제 차감 확인
    { // 상점 경로 검사 시작
        RequireVerificationProfile(); // 자동 저장 격리 확인
        yield return new EnterPlayMode(); // 실제 게임 실행
        yield return PrepareMap(); // 상점이 있는 실제 층 준비
        var session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
        var shop = ShopRunManager.Instance; // 실제 거래 관리자 조회
        var owner = BattleResultManager.Instance.ActiveParty.Members[0]; // 공용 카드 구매자 지정
        var offer = shop.Catalog.Offers.Single(item => item.OfferId == "SHOP_SHEET_HEAVY_STRIKE"); // 대상 지정 공용 카드 조회
        var potion = shop.Catalog.Offers.Single(item => item.OfferId == "SHOP_POTION_MENTAL"); // 후속 구매 물약 조회
        int normal = shop.GetOfferPrice(offer); // 표식 전 기본 가격 보관
        int discounted = normal - normal / 10; // 할인액 내림 후 지불 금액 계산
        int removal = shop.GetCardRemovalPrice(); // 카드 제거 서비스 기존 가격 보관
        Assert.IsTrue(BurntCartEvent.TryResolve("mark", BurntCartEvent.Mark, null, out _)); // 다음 상점 표식 획득
        Assert.IsFalse(BurntCartEvent.TryResolve("mark-two", BurntCartEvent.Mark, null, out _)); // 미사용 표식 중첩 차단
        var room = FindShopRoom(); // 지도에 배치된 실제 상점 조회
        room.SendMessage("OpenShop"); // 일반 상점 방 방문 처리 실행
        string shopId = shop.ActiveRoomRuntimeId; // 할인 대상 방 확인
        Assert.IsTrue(session.Cart.IsMarkedShop(shopId)); // 실제 첫 방문 상점 연결 확인
        Assert.AreEqual(removal, shop.GetCardRemovalPrice()); // 카드 제거 서비스 할인 제외 확인
        var product = Clickable("Offer_" + offer.OfferId); // 실제 상품 목록의 카드 버튼 조회
        AssertPrice(product, discounted); // 실제 상품 목록 가격 확인
        product.onClick.Invoke(); // 실제 구매 확인 화면 열기
        Assert.AreEqual($"구매 가격   {discounted:N0} Gold", GameObject.Find("PurchasePrice").GetComponent<TMP_Text>().text); // 확인 화면의 표식 할인 가격
        Assert.IsFalse(Clickable("ConfirmPurchase").interactable); // 잔액 부족 시 실제 결제 차단
        Clickable("CancelPurchase").onClick.Invoke(); // 골드가 없는 상품 확인 취소
        Assert.AreEqual(ShopPurchaseResult.InsufficientGold, shop.TryPurchase(offer, owner)); // 지갑 부족 구매 실패 확인
        Assert.AreEqual(0, shop.Gold); // 실패와 취소 후 차감 없음 확인
        Object.FindFirstObjectByType<ShopPrototypeView>().SendMessage("CloseShop"); // 구매 없이 상점 이탈
        Assert.IsTrue(room.gameObject.activeSelf); // 구매 전 상점 재입장 가능 확인
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 첫 입장 때 자동 저장한 표식 상점 복원
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 같은 층 다시 구성
        yield return WaitForMap(); // 지도와 상점 표시 복원 대기
        room = FindShopRoom(); // 복원된 실제 상점 조회
        room.SendMessage("OpenShop"); // 같은 상점 다시 방문
        Assert.AreEqual(discounted, shop.GetOfferPrice(offer)); // 재입장 후 첫 상품 할인 유지 확인
        shop.AddDebugGold(1000); // 실제 두 상품 결제를 위한 테스트 지갑 준비
        owner = BattleResultManager.Instance.ActiveParty.Members[0]; // 복원한 구매자 조회
        Clickable("Offer_" + offer.OfferId).onClick.Invoke(); // 실제 공용 상품 선택
        Clickable("ConfirmPurchase").onClick.Invoke(); // 할인 가격 확인 뒤 소유자 선택
        AssertPrice(Clickable("ChooseCardOwner_" + owner.CharacterId), discounted); // 결제 직전까지 같은 할인 가격 확인
        Clickable("ChooseCardOwner_" + owner.CharacterId).onClick.Invoke(); // 구매자 지정과 최초 결제
        Assert.IsTrue(shop.IsPurchased(offer)); // 첫 상품 지급과 판매 완료 확인
        Assert.AreEqual(1000 - discounted, shop.Gold); // 최초 한 상품만 할인한 차감 확인
        Assert.IsFalse(session.Cart.HasMark); // 실제 구매 뒤 표식 소모 확인
        Assert.AreEqual(potion.Price, shop.GetOfferPrice(potion)); // 두 번째 상품의 정상가 표시 확인
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(potion)); // 두 번째 상품 정상 구매
        int expected = 1000 - discounted - potion.Price; // 결제 후 잔액 계산
        Assert.AreEqual(expected, shop.Gold); // 두 번째 상품 원가 차감 확인
        Object.FindFirstObjectByType<ShopPrototypeView>().SendMessage("CloseShop"); // 구매 완료 상점 닫기
        yield return null; // 구매 완료 상점 표식 숨김 대기
        Assert.IsFalse(room.gameObject.activeSelf); // 구매한 일회성 상점 재방문 차단 확인
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 거래 자동 저장 복원
        Assert.AreEqual(expected, shop.Gold); // 저장 후 최종 결제 금액 유지 확인
        Assert.IsFalse(session.Cart.HasMark); // 저장 후 할인 재발급 차단
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 상점 경로 검사 종료

    [UnityTest] // 미구매 표식의 층 이동과 저장 복원 검사
    public IEnumerator UnspentMarkMovesToNextFloorAndSurvivesSaveUntilPurchased() // 첫 상점 미구매 뒤 다음 층 결제 확인
    { // 다음 층 표식 검사 시작
        RequireVerificationProfile(); // 자동 저장 격리 확인
        yield return new EnterPlayMode(); // 실제 게임 실행
        yield return PrepareMap(); // 첫 층과 상점 준비
        var session = ExplorationSessionManager.Instance; // 현재 회차 조회
        var shop = ShopRunManager.Instance; // 실제 상점 관리자 조회
        Assert.IsTrue(BurntCartEvent.TryResolve("mark-floor-one", BurntCartEvent.Mark, null, out _)); // 첫 층에서 표식 획득
        FindShopRoom().SendMessage("OpenShop"); // 첫 층 상점 방문
        string oldShop = shop.ActiveRoomRuntimeId; // 방문 후 떠날 상점 식별자 저장
        Object.FindFirstObjectByType<ShopPrototypeView>().SendMessage("CloseShop"); // 상품 구매 없이 이탈
        session.AdvanceFloor(); // 정상 다음 층 진행 처리
        session.SetCurrentFloorSeed(208); // 새 층 검사 배치 고정
        Assert.IsTrue(session.Cart.HasMark && !session.Cart.IsMarkedShop(oldShop)); // 이전 층 연결만 해제 확인
        Assert.IsTrue(session.IsEventResolved("mark-floor-one")); // 이전 층에 표식을 남긴 방 기록 유지 확인
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 다음 층 실제 구성
        yield return WaitForMap(); // 두 번째 층 배치 대기
        Assert.IsTrue(ExplorationAutoSave.SaveCurrentExploration()); // 층 이동 후 미구매 표식 저장
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 다음 층의 미구매 표식 복원
        Assert.IsTrue(session.Cart.HasMark && !session.Cart.IsMarkedShop(oldShop)); // 저장 복원에서 이전 상점 재연결 차단
        var potion = shop.Catalog.Offers.Single(item => item.OfferId == "SHOP_POTION_MENTAL"); // 다음 층에서 구매할 실제 물약 조회
        int normal = shop.GetOfferPrice(potion); // 표식 적용 전 기본 가격 기록
        FindShopRoom().SendMessage("OpenShop"); // 다음 층의 첫 실제 상점 열기
        Assert.AreNotEqual(oldShop, shop.ActiveRoomRuntimeId); // 새 층 상점 식별자 확인
        Assert.IsTrue(session.Cart.IsMarkedShop(shop.ActiveRoomRuntimeId)); // 새 상점에 미구매 표식 연결 확인
        shop.AddDebugGold(normal); // 실제 물약 구매 비용 준비
        Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(potion)); // 다음 층에서 첫 상품 구매 성공
        Assert.AreEqual(normal / 10, shop.Gold); // 구매 시점의 정확한 할인 확인
        Assert.IsFalse(session.Cart.HasMark); // 첫 구매 성공 뒤 표식 소모 확인
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 다음 층 표식 검사 종료

    [UnityTest] // 실제 저장 재개 매복과 도주 검사
    public IEnumerator SavedAmbushAddsHunterAndPreservesMapFragmentAfterEscape() // 같은 편성 복원과 실제 도주 후 재생성 차단
    { // 매복 경로 검사 시작
        RequireVerificationProfile(); // 자동 저장 격리 확인
        yield return new EnterPlayMode(); // 실제 게임 실행
        yield return CheckSavedEscape(); // 도메인 재시작 후 실제 경로 검사 생성
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 매복 경로 검사 종료

    private static IEnumerator CheckSavedEscape() // 매복 경로 검사 실행
    { // 실제 경로 검사 시작
        yield return PrepareMap(); // 실제 일반 조우 배치 대기
        var session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
        Assert.IsTrue(AshMapFragmentEvent.TryResolve("fragment", AshMapFragmentEvent.PickUp, null, out _)); // 일반 표식 접촉용 지도 조각 획득
        var panel = OpenCart("s44"); // 매복 경계의 실제 수레 선택 화면 열기
        panel.SendMessage("HandleChoiceSelected", (object)BurntCartEvent.Box); // 상자에서 매복 결말 확정
        string battleId = session.Cart.AmbushBattleId; // 저장 전 매복 방 기록
        var originals = Object.FindFirstObjectByType<ExplorationMapRuntime>().GetComponentsInChildren<ExplorationEncounterView>().Where(view => view.Data.EncounterId == session.Cart.NormalEncounter).Select(view => view.Data).First(); // 실제 층에 배치된 일반 원본 조회
        Assert.AreEqual(0, session.RunGoldGained); // 매복 진입 전 보상 미지급 확인
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 결말 확인 전 저장으로 복원
        Assert.IsFalse(SceneFlowManager.Instance.IsLoadingScene); // 불러오기 중 이전 결말의 전투 씬 요청 차단
        Assert.IsNull(session.ActiveEncounter); // 현재 지도를 복구하기 전 과거 매복 전투 진입 차단
        Assert.AreEqual(battleId, session.Cart.AmbushBattleId); // 같은 매복 식별자 복원 확인
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 저장한 지도 진입
        yield return Wait(() => ReadyBattle(), "저장된 수레 매복 전투 진입"); // 표식 접촉 없이 실제 전투 시작 대기
        var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 생성된 전투 조회
        CollectionAssert.AreEqual(originals.Enemies.Append(BurntCartEvent.Hunter), battle.EnemyUnits.Select(unit => unit.EnemySource)); // 원래 편성 전원과 추가 사냥꾼 한 마리 확인
        Assert.AreEqual("fragment", session.PendingMapFragmentId); // 이벤트 매복에서 일반 표식용 조각 보존 확인
        Assert.IsTrue(battle.BattleTurn.TryEscape()); // 정상 일반 전투 도주 실행
        Object.FindFirstObjectByType<BattleResultView>().transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 결과를 확인하고 지도 복귀
        yield return WaitForMap(); // 전투 결과와 자동 저장을 적용한 지도 대기
        yield return null; // 전투 후 자동 저장 완료 대기
        Assert.IsNull(session.Cart.AmbushRoom); // 해결한 매복 재시작 차단 확인
        Assert.IsTrue(session.IsEventResolved("s44")); // 도주 후에도 상자 재추첨 차단
        Assert.AreEqual("fragment", session.PendingMapFragmentId); // 전투 귀환 후 지도 조각 유지
        Assert.AreEqual(0, session.RunGoldGained); // 도주 시 일반 승리 보상 미지급 확인
        Assert.IsTrue(ExplorationSaveService.Load(ExplorationSaveService.DefaultPath)); // 도주 결과 저장 검증
        Assert.IsTrue(string.IsNullOrEmpty(session.Cart.AmbushRoom)); // 저장 복원 후 매복 재생성 차단
    } // 실제 경로 검사 종료

    [UnityTest] // 최대 일반 편성에 매복 한 자리를 더한 실제 전투 검사
    public IEnumerator FourEnemyNormalFixtureAddsFifthAndPaysOriginalVictoryOnce() // 최대 편성 전원 출전과 원본 보상 확인
    { // 확장 편성 검사 시작
        RequireVerificationProfile(); // 자동 저장 격리 확인
        yield return new EnterPlayMode(); // 실제 게임 실행
        yield return CheckLargeVictory(); // 도메인 재시작 후 실제 경로 검사 생성
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 확장 편성 검사 종료

    private static IEnumerator CheckLargeVictory() // 확장 편성 검사 실행
    { // 실제 경로 검사 시작
        yield return PrepareMap(); // 현재 탐사 층 준비
        var session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
        Assert.IsTrue(BurntCartEvent.TryResolve("s44", BurntCartEvent.Box, null, out _)); // 현재 일반 원본으로 매복 예약
        var source = Resources.LoadAll<EncounterData>("Encounters").Single(item => item.EncounterId == session.Cart.NormalEncounter); // 실제 배치의 원본 조회
        var fixture = Object.Instantiate(source); // 원본 에셋 수정 없이 최대 편성 복사
        var group = Enumerable.Repeat(source.Enemies[0], 4).ToList(); // 일반 전투 최대 네 자리 재현
        typeof(EncounterData).GetField("enemies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(fixture, group); // 검사 복사본에 네 명 편성
        string id = session.Cart.AmbushBattleId; // 같은 매복 방 식별자 보관
        Assert.IsTrue(session.BeginEncounter(id, fixture, Vector3.zero, Vector3.zero)); // 일반 조우로 실제 매복 전투 준비
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 정상 전투 씬 진입
        yield return Wait(() => ReadyBattle(), "최대 일반 편성의 매복 전투"); // 실제 다섯 적 생성 대기
        var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 출전한 적 목록 조회
        CollectionAssert.AreEqual(group.Append(BurntCartEvent.Hunter), battle.EnemyUnits.Select(unit => unit.EnemySource)); // 기존 네 명과 추가 사냥꾼 전원 출전 확인
        Assert.AreEqual(4, fixture.Enemies.Count); // 원래 편성 목록 보존 확인
        foreach (var enemy in battle.EnemyUnits.ToArray()) // 결과 경로 검사용 적 전체 순회
        { // 승리 조건 준비 시작
            enemy.TakeDamage(100000, BattleDamageType.Physical); // 각 적을 쓰러뜨려 정상 승리 알림 실행
        } // 승리 조건 준비 종료
        Assert.AreEqual(BattleResult.Victory, battle.BattleTurn.Result); // 전원 처치 승리 확인
        Object.FindFirstObjectByType<BattleResultView>().transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 승리 결과 확인
        yield return WaitForMap(); // 승리 보상 적용 대기
        Assert.IsNull(session.Cart.AmbushRoom); // 승리한 매복 예약 제거 확인
        Assert.IsTrue(session.IsEncounterCleared(id)); // 매복 고유 방만 승리 기록 확인
        Assert.AreEqual(source.GoldReward, session.RunGoldGained); // 원래 일반 조우 보상 한 번 지급 확인
        Assert.IsFalse(BurntCartEvent.TryResolve("s44", BurntCartEvent.Box, null, out _)); // 승리 후 같은 상자 반복 차단
        Object.Destroy(fixture); // 테스트용 조우 복사본 정리
    } // 실제 경로 검사 종료

    [UnityTest] // 실제 결말 확인과 매복 전투 전멸 검사
    public IEnumerator ConfirmingBoxOpensAmbushAndWipeTerminatesRun() // 확인 버튼 전투 진입과 전멸 결과 확인
    { // 매복 전멸 검사 시작
        RequireVerificationProfile(); // 자동 저장 격리 확인
        yield return new EnterPlayMode(); // 실제 게임 실행
        yield return CheckEncounterWipe(); // 도메인 재시작 후 실제 경로 검사 생성
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 매복 전멸 검사 종료

    private static IEnumerator CheckEncounterWipe() // 매복 전멸 검사 실행
    { // 실제 경로 검사 시작
        yield return PrepareMap(); // 실제 탐사 지도 준비
        var session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
        var panel = OpenCart("s44"); // 매복 상자 원문 선택 표시
        panel.SendMessage("HandleChoiceSelected", (object)BurntCartEvent.Box); // 상자 매복 선택 확정
        panel.SendMessage("CloseCurrentEvent"); // 결말 확인 버튼 처리 실행
        yield return Wait(() => ReadyBattle(), "수레 결말 확인 후 실제 전투"); // 실제 매복 전환 대기
        var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 파티 조회
        foreach (var ally in battle.AllyUnits.ToArray()) // 전멸 검사용 전체 아군 순회
        { // 전멸 조건 준비 시작
            ally.TakeDamage(100000, BattleDamageType.Physical); // 각 아군을 쓰러뜨려 정상 패배 알림 실행
        } // 전멸 조건 준비 종료
        Assert.AreEqual(BattleResult.Defeat, battle.BattleTurn.Result); // 전투 패배 결과 확인
        Object.FindFirstObjectByType<BattleResultView>().transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 패배 결과 확인
        yield return Wait(() => session.IsExplorationCompleted && !SceneFlowManager.Instance.IsLoadingScene, "수레 매복 전멸 처리"); // 정상 탐사 실패 화면 대기
        Assert.IsFalse(session.IsExplorationSuccess); // 탐사 전멸 결과 확인
        Assert.IsTrue(string.IsNullOrEmpty(session.Cart.AmbushRoom)); // 전멸 매복 재시작 차단
        Assert.AreEqual(0, session.RunGoldGained); // 패배 보상 미지급 확인
    } // 실제 경로 검사 종료

    private static void RequireVerificationProfile() // 실제 이용자 저장과 자동 저장 분리 확인
    { // 저장 격리 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 분리된 프로젝트 설정 확인
        { // 사용자 저장 보호 시작
            Assert.Ignore("탐사 자동 저장이 분리된 ProjectCVerification 프로필에서 실행하세요."); // 일반 사용자 프로필 실행 제외
        } // 사용자 저장 보호 종료
    } // 저장 격리 검사 종료

    private static IEnumerator PrepareMap() // 실제 파티와 새 탐사 지도 준비
    { // 지도 준비 시작
        if (SceneFlowManager.Instance == null) // 씬 이동 관리자 존재 확인
        { // 씬 이동 관리자 준비 시작
            new GameObject("CartSceneFlow").AddComponent<SceneFlowManager>(); // 실제 비동기 씬 전환 연결
        } // 씬 이동 관리자 준비 종료
        PlayerResourceManager.EnsureInstance().ResetResources(); // 독립 검사 재화 초기화
        PartyDepartureTestSupport.StartExploration(0, 1); // 실제 두 명을 선택하여 탐사 출발
        ExplorationSessionManager.Instance.SetCurrentFloorSeed(208); // 일반 조우와 상점 배치 고정
        yield return WaitForMap(); // 캐릭터와 실제 지도 생성 대기
    } // 지도 준비 종료

    private static IEnumerator WaitForMap() // 실제 탐사 씬과 방 생성 대기
    { // 지도 대기 시작
        yield return Wait(() => SceneManager.GetActiveScene().name == "30_Exploration" && !SceneFlowManager.Instance.IsLoadingScene && Object.FindFirstObjectByType<ExplorationMapRuntime>() != null && Object.FindFirstObjectByType<ExplorationEncounterView>() != null && Object.FindFirstObjectByType<ShopPrototypeView>() != null, "탐사 지도와 일반 조우 및 상점 생성"); // 씬 전환과 콘텐츠 배치 완료 대기
    } // 지도 대기 종료

    private static bool ReadyBattle() // 전투 씬과 턴 상태 초기화 확인
    { // 전투 상태 검사 시작
        var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 현재 전투 관리자 조회
        return SceneManager.GetActiveScene().name == "40_Battle" && !SceneFlowManager.Instance.IsLoadingScene && battle != null && battle.IsInitialized && battle.BattleTurn != null; // 결과 버튼을 사용할 수 있는 전투 확인
    } // 전투 상태 검사 종료

    private static IEnumerator Wait(Func<bool> ready, string step) // 화면 전환 제한 시간 검사
    { // 대기 시작
        float end = Time.realtimeSinceStartup + 20f; // 개별 전환 제한 시각 설정
        while (!ready() && Time.realtimeSinceStartup < end) // 현재 단계 완료와 시간 제한 확인
        { // 프레임 대기 시작
            yield return null; // 실제 씬과 전투 갱신 대기
        } // 프레임 대기 종료
        Assert.IsTrue(ready(), step); // 시간 안에 실제 단계 도달 확인
    } // 대기 종료

    private static ExplorationEventPanelView OpenCart(string room) // 실제 이벤트 표식과 선택 화면 연결
    { // 수레 표시 시작
        var data = ExplorationEventCatalog.LoadEvents(6).Single(item => item.EventId == BurntCartEvent.Id); // 정식 층의 수레 원본 조회
        var marker = new GameObject("CartTestMarker").AddComponent<ExplorationEventView>(); // 실제 방에서 사용하는 표식 생성
        marker.Initialize(data, room); // 고정 결과 방 연결
        var panel = ExplorationEventPanelView.EnsureInstance(); // 게임 선택 화면 조회
        panel.ShowEvent(marker, data, room); // 원문 선택지 열기
        return panel; // 현재 사건 화면 반환
    } // 수레 표시 종료

    private static ExplorationSpecialRoomView FindShopRoom() // 생성 순서와 무관한 동일 상점 검색
    { // 상점 찾기 시작
        var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 현재 층 지도 조회
        var cell = map.CurrentMap.Cells.First(item => item.RoomType == ExplorationRoomType.Shop); // 같은 층의 첫 상점 좌표 검색
        return GameObject.Find($"SpecialRoom_Shop_{cell.Coordinate.x}_{cell.Coordinate.y}").GetComponent<ExplorationSpecialRoomView>(); // 복원과 재방문에 사용하는 동일 방 반환
    } // 상점 찾기 종료

    private static Button Clickable(string name) // 현재 화면의 상점 버튼 조회
    { // 버튼 찾기 시작
        var item = GameObject.Find(name); // 실제 화면에 표시된 이름 검색
        Assert.IsNotNull(item, name); // 표시된 상품이나 소유자 존재 확인
        return item.GetComponent<Button>(); // 실제 거래 버튼 반환
    } // 버튼 찾기 종료

    private static void AssertPrice(Button item, int price) // 실제 선택 버튼의 상품 가격 검증
    { // 표시 금액 검사 시작
        Assert.IsTrue(item.GetComponentsInChildren<TMP_Text>().Any(label => label.text == price + " Gold"), item.name); // 할인 후 실제 숫자와 단위 확인
    } // 표시 금액 검사 종료
} // 수레 검사 종료
