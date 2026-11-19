using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 판매 흐름 검사 사용
using TMPro; // 실제 가격 글자 확인 사용
using UnityEngine; // 실제 게임 객체 사용
using UnityEngine.SceneManagement; // 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 판매 버튼 사용

public sealed class ShopRelicSaleTests // 상점 유물 판매 검사
{ // 판매 검사 시작
    [UnityTest] // 실제 판매와 저장 검사 지정
    public IEnumerator SellOwnedRelicPaysSheetPriceAndRestoresSale() // 실제 화면과 저장 복원 확인
    { // 판매 검사 본문 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 경로 생성
        try // 독립 저장 파일 정리 보장
        { // 실제 판매 검사 시작
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
            Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), PartyLoadoutRuntime.Active.Deck)); // 상점 데이터 준비
            shop.BeginRoomVisit("relic_sale_room_test"); // 독립 상점 방문
            RelicData relic = Resources.Load<RelicData>("Relics/GateKey"); // 실제 판매 유물 조회
            RelicData keep = Resources.Load<RelicData>("Relics/AshSeal"); // 보유 유지 유물 조회
            Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 판매 유물 획득
            Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.Instance.TryAcquire(keep)); // 유지 유물 획득
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 조회
            int beforeGold = resources.Gold; // 판매 전 골드 보관
            int beforeRunGold = resources.ExplorationGold; // 판매 전 탐사 골드 보관
            int price = shop.GetRelicSellPrice(relic); // 실제 판매 가격 조회
            Assert.That(price, Is.InRange(116, 147)); // 시트 판매 범위 확인
            RelicData copy = Object.Instantiate(relic); // 같은 식별자의 미보유 원본 생성
            Assert.AreNotEqual(ShopPurchaseResult.Success, shop.TrySellRelic(copy)); // 미보유 원본 판매 차단 확인
            Assert.AreEqual(beforeGold, resources.Gold); // 잘못된 판매의 골드 유지 확인
            Object.DestroyImmediate(copy); // 임시 유물 원본 정리
            ShopPrototypeView view = Object.FindFirstObjectByType<ShopPrototypeView>(); // 실제 상점 화면 조회
            if (view == null) // 상점 화면 부재 확인
            { // 상점 화면 생성 시작
                view = ShopPrototypeView.Create(shop); // 실제 상점 화면 생성
            } // 상점 화면 생성 종료
            Assert.IsTrue(view.TryOpenFromRoom(shop.ActiveRoomRuntimeId)); // 상점 방 방문 성공 확인
            Transform content = view.transform.Find("ShopOverlay/ShopPanel/ProductScroll/Viewport/Content"); // 실제 상품 목록 조회
            Assert.IsNotNull(content); // 상품 목록 확인
            content.Find("RelicSellService").GetComponent<Button>().onClick.Invoke(); // 유물 판매 목록 열기
            Transform sale = content.Find("SellRelic_RLC_GATE_KEY"); // 실제 인증키 판매 항목 조회
            Assert.IsNotNull(sale); // 판매 항목 표시 확인
            Assert.AreEqual($"+{price} Gold", sale.Find("Price").GetComponent<TMP_Text>().text); // 표시 판매 가격 확인
            sale.GetComponent<Button>().onClick.Invoke(); // 실제 판매 실행
            Assert.IsFalse(RelicRunManager.Instance.Inventory.ContainsRelic(relic.RelicId)); // 판매 유물 제거 확인
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(keep.RelicId)); // 다른 유물 유지 확인
            Assert.AreEqual(beforeGold + price, resources.Gold); // 판매 골드 지급 확인
            Assert.AreEqual(beforeRunGold + price, resources.ExplorationGold); // 탐사 획득 골드 기록 확인
            Assert.AreNotEqual(ShopPurchaseResult.Success, shop.TrySellRelic(relic)); // 중복 판매 차단 확인
            Assert.AreEqual(beforeGold + price, resources.Gold); // 중복 판매 골드 유지 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 판매 후 독립 저장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 판매 후 저장 복원 확인
            Assert.IsFalse(RelicRunManager.Instance.Inventory.ContainsRelic(relic.RelicId)); // 판매 유물 미복원 확인
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(keep.RelicId)); // 다른 유물 복원 확인
            Assert.AreEqual(beforeGold + price, PlayerResourceManager.Instance.Gold); // 판매 골드 복원 확인
            Assert.AreEqual(price, ShopRunManager.Instance.GetRelicSellPrice(relic)); // 같은 상점 판매 가격 유지 확인
            PlayerResourceManager.Instance.RestoreResources(int.MaxValue, 0, 0, 0); // 골드가 가득 찬 상태 설정
            Assert.AreNotEqual(ShopPurchaseResult.Success, ShopRunManager.Instance.TrySellRelic(keep)); // 골드 초과 판매 차단 확인
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(keep.RelicId)); // 골드 초과 시 유물 유지 확인
            Assert.AreEqual(int.MaxValue, PlayerResourceManager.Instance.Gold); // 골드 초과 방지 확인
        } // 실제 판매 검사 종료
        finally // 독립 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 판매 검사 본문 종료
} // 판매 검사 종료
