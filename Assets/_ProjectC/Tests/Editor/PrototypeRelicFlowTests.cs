using System.Collections; // 플레이 모드 코루틴 사용
using NUnit.Framework; // 상점 구매와 저장 복원 검증 사용
using UnityEngine; // 실제 게임 리소스 사용
using UnityEngine.SceneManagement; // 실제 게임 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
public sealed class PrototypeRelicFlowTests // 시제품 유물 획득과 저장 검사
{ // 유물 흐름 검사 시작
    [UnityTest] // 상점 구매와 저장 복원 검사 지정
    public IEnumerator ShopPurchases_RestoreAllPrototypeRelics() // 실제 상점 구매와 탐사 저장 복원 확인
    { // 유물 흐름 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 테스트 저장 경로 생성
        try // 독립 저장 파일 정리 보장
        { // 구매와 저장 복원 검사 시작
            SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 준비 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 시작 메뉴 확인
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 준비 대기
            { // 거점 대기 시작
                yield return null; // 다음 프레임 대기
            } // 거점 대기 종료
            Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 거점 존재 확인
            PartyDepartureTestSupport.StartExploration(0); // 실제 파티로 탐사 시작
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 준비 대기
            { // 탐사 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 확인
            yield return null; // 탐사와 상점 초기화 대기
            ShopCatalogData catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점 상품 목록 조회
            ShopRunManager shop = ShopRunManager.EnsureInstance(); // 실제 상점 관리자 준비
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 실제 파티 덱으로 상점 준비
            shop.BeginRoomVisit("prototype_relic_shop_test"); // 독립 테스트 상점 방문
            PlayerResourceManager.EnsureInstance().AddResources(2000, 0, 0, 0); // 독립 테스트 구매 골드 준비
            string[] ids = { "RLC_ASH_SEAL", "RLC_BROKEN_MUSIC_BOX", "RLC_BRONZE_CENSER", "RLC_FLOATING_STONE" }; // 검사할 실제 유물 식별자
            foreach (string id in ids) // 실제 유물 식별자 순회
            { // 개별 유물 구매 시작
                ShopOfferData found = null; // 상품 검색 결과 초기화
                foreach (ShopOfferData offer in catalog.Offers) // 실제 상품 목록 순회
                { // 상품 식별자 확인 시작
                    if (offer.Relic != null && offer.Relic.RelicId == id) // 실제 유물 상품 확인
                    { // 상품 발견 처리 시작
                        found = offer; // 구매할 실제 상품 저장
                        break; // 상품 검색 종료
                    } // 상품 발견 처리 종료
                } // 상품 식별자 확인 종료
                Assert.IsNotNull(found, id); // 구매 상품 존재 확인
                Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(found), id); // 실제 골드 지불과 유물 구매 확인
                Assert.AreEqual(ShopPurchaseResult.AlreadyPurchased, shop.TryPurchase(found), id); // 같은 상점 중복 구매 차단 확인
                Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(id), id); // 구매 유물 보관함 등록 확인
            } // 개별 유물 구매 종료
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 네 유물 포함 독립 탐사 저장 확인
            foreach (string id in ids) // 저장한 유물 식별자 순회
            { // 기존 보관함 제거 시작
                Assert.IsTrue(RelicRunManager.Instance.TryRemove(id)); // 실제 유물 제거 확인
            } // 기존 보관함 제거 종료
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 독립 탐사 저장 복원 확인
            foreach (string id in ids) // 복원할 유물 식별자 순회
            { // 유물 복원 확인 시작
                Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(id), id); // 실제 유물 원본 복원 확인
            } // 유물 복원 확인 종료
            EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 윈터폴 조우 조회
            Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("prototype_relic_battle_test", encounter, player.transform.position, player.transform.position)); // 독립 전투 조우 시작
            SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 전투 씬 진입
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 전투 준비 대기
            { // 전투 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전투 대기 종료
            BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
            Assert.IsNotNull(battle); // 전투 관리자 존재 확인
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && battle.AllyUnits[0].CurrentBlock < 14;) // 전투 시작 유물 발동 대기
            { // 유물 발동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 유물 발동 대기 종료
            Assert.AreEqual(14, battle.AllyUnits[0].CurrentBlock); // 단독 아군의 인장 6과 부유석 8 적용 확인
            Assert.AreEqual(1, battle.AllyUnits[0].GetStatusEffect(BattleStatusEffectType.MagicalResistanceUp).EffectiveValue); // 실제 전투의 청동 향로 저항 확인
        } // 구매와 저장 복원 검사 종료
        finally // 독립 테스트 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 독립 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 유물 흐름 검사 종료
} // 유물 흐름 검사 종료
