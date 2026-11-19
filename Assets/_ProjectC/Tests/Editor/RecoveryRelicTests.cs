using System.Collections; // 실제 씬 전환 대기
using System.Linq; // 등록된 상품 조회
using NUnit.Framework; // 회복 결과 검증
using UnityEngine; // 전투 원본과 객체 조회
using UnityEngine.SceneManagement; // 시작 씬 열기
using UnityEngine.TestTools; // 플레이 모드 검사

public sealed class RecoveryRelicTests // 최저 체력 아군 회복 유물 검사
{ // 회복 유물 검사 시작
    [Test] // 실제 유물과 전투 이벤트 검사
    public void HealingRetargetsLivingAlliesAtStartEveryThirdRoundAndEnd() // 세 발동 시점과 동률 대상 확인
    { // 전투 효과 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        BattleUnitRuntime[] allies = roster.Characters.Take(4).Select(character => BattleUnitRuntime.CreateAlly(character)).ToArray(); // 편성 순서의 아군 네 명 생성
        allies[0].ApplyPersistentHealth(0); // 첫 위치에 사망 아군 배치
        allies[1].ApplyPersistentHealth(7); // 첫 생존 아군 체력 설정
        allies[2].ApplyPersistentHealth(6); // 가장 낮은 체력의 아군 설정
        allies[3].ApplyPersistentHealth(7); // 동률인 뒤쪽 아군 설정
        RelicGoldRuntime wallet = new RelicGoldRuntime(); // 독립 중복 보상 지갑 생성
        RelicInventoryRuntime inventory = new RelicInventoryRuntime(wallet); // 독립 유물 보관함 생성
        foreach (string name in new[] { "RepairDroneWing", "DriedOasisLeaf", "EmptyHerbTin" }) // 실제 회복 유물 순회
        { // 회복 유물 획득 시작
            RelicData relic = Resources.Load<RelicData>("Relics/" + name); // 상점과 공유하는 원본 조회
            Assert.IsNotNull(relic, name); // 실제 유물 존재 확인
            Assert.IsNotNull(relic.Icon, name); // 개별 유물 아이콘 연결 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 실제 보관함 획득 확인
        } // 회복 유물 획득 종료
        using (BattleEventDispatcher events = new BattleEventDispatcher()) // 독립 전투 이벤트 생성
        using (BattleRelicEffectController effects = new BattleRelicEffectController(events, inventory, wallet, allies, new BattleUnitRuntime[0])) // 실제 유물 관리자 연결
        { // 발동 순서 검사 시작
            effects.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 실제 초기화 시점 재현
            Assert.AreEqual(12, allies[2].CurrentHealth); // 시작 시 최저 체력 아군 6 회복 확인
            for (int round = 1; round <= 6; round++) // 두 번의 세 번째 아군 턴 순회
            { // 양 진영 턴 검사 시작
                events.Publish(new BattleEventContext(BattleEventType.TurnStarted, round, BattleTurnPhase.PlayerTurn)); // 아군 턴 시작 전달
                events.Publish(new BattleEventContext(BattleEventType.TurnStarted, round, BattleTurnPhase.EnemyTurn)); // 같은 라운드 적 턴 시작 전달
                Assert.AreEqual(round < 3 ? 7 : 12, allies[1].CurrentHealth); // 세 번째 아군 턴의 앞쪽 동률 대상 회복 확인
                Assert.AreEqual(round < 6 ? 7 : 12, allies[3].CurrentHealth); // 여섯 번째 아군 턴의 새 최저 체력 대상 회복 확인
                Assert.AreEqual(12, allies[2].CurrentHealth); // 다른 턴과 적 턴의 추가 회복 차단 확인
            } // 양 진영 턴 검사 종료
            events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 6, BattleTurnPhase.Victory, result: BattleResult.Victory)); // 전투 종료 전달
            Assert.AreEqual(15, allies[1].CurrentHealth); // 종료 시 첫 동률 생존 아군 3 회복 확인
            events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 6, BattleTurnPhase.Victory, result: BattleResult.Victory)); // 같은 전투 종료 알림 재전달
            Assert.AreEqual(12, allies[2].CurrentHealth); // 종료 효과 중복 발동 방지 확인
            Assert.AreEqual(0, allies[0].CurrentHealth); // 사망 아군 부활 방지 확인
        } // 발동 순서 검사 종료
    } // 전투 효과 검사 종료

    [UnityTest] // 실제 상점과 탐사 저장 및 전투 결과 검사
    public IEnumerator ShopSaveSaleAndBattleResultPreserveHealingRelics() // 구매와 저장 복원 및 전투 종료 체력 확인
    { // 통합 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 자동 저장 전용 프로필 확인
        { // 실제 플레이어 저장 보호 시작
            Assert.Ignore("자동 저장을 격리한 ProjectCVerification 복제본에서 실행하세요."); // 일반 프로필 검사 생략
        } // 실제 플레이어 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 실행
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로 생성
        try // 시험 저장 정리 보장
        { // 실제 게임 흐름 검사 시작
            SceneManager.LoadScene("00_Boot"); // 시작 씬 열기
            yield return WaitFor<MainMenuController>(); // 씬 전환 관리자 준비 대기
            PartyDepartureTestSupport.StartExploration(0, 1); // 두 아군으로 탐사 출발
            yield return WaitFor<ExplorationEncounterView>(); // 실제 탐사 조우 생성 대기
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 현재 탐사 캐릭터 조회
            ShopCatalogData catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상점 목록 조회
            ShopRunManager shop = ShopRunManager.EnsureInstance(); // 회차 상점 준비
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 현재 파티의 상점 연결 확인
            shop.BeginRoomVisit("day198-recovery-room"); // 독립 상점 방문 기록
            PlayerResourceManager.EnsureInstance().AddResources(1500, 0, 0, 0); // 유물 구매 금액 준비
            RelicData[] relics = new[] { "EmptyHerbTin", "DriedOasisLeaf", "RepairDroneWing" }.Select(name => Resources.Load<RelicData>("Relics/" + name)).ToArray(); // 구매 대상 세 유물 조회
            int[] buyMin = { 200, 230, 225 }; // 시트 구매 최저 가격
            int[] buyMax = { 236, 266, 268 }; // 시트 구매 최고 가격
            int[] sellMin = { 104, 117, 118 }; // 시트 판매 최저 가격
            int[] sellMax = { 149, 162, 158 }; // 시트 판매 최고 가격
            for (int index = 0; index < relics.Length; index++) // 각 유물의 구매 경로 확인
            { // 유물 구매 시작
                RelicData relic = relics[index]; // 현재 유물 원본 조회
                Assert.IsNotNull(relic); // 원본 누락 확인
                ShopOfferData offer = catalog.Offers.Single(item => item.Relic == relic); // 실제 유물 상품 조회
                int price = shop.GetOfferPrice(offer); // 방문한 상점의 구매 금액 조회
                Assert.That(price, Is.InRange(buyMin[index], buyMax[index])); // 원본 시트의 구매 범위 확인
                int oldGold = shop.Gold; // 거래 전 금액 보관
                Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(offer)); // 실제 유물 구매 확인
                Assert.AreEqual(oldGold - price, shop.Gold); // 구매 금액 차감 확인
                Assert.That(shop.GetRelicSellPrice(relic), Is.InRange(sellMin[index], sellMax[index])); // 원본 시트의 판매 범위 확인
            } // 유물 구매 종료
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 구매 직후 독립 탐사 저장
            foreach (RelicData relic in relics) // 저장 전 유물 세 종류 순회
            { // 현재 보관함 비우기 시작
                Assert.IsTrue(RelicRunManager.Instance.TryRemove(relic.RelicId)); // 저장 복구 전 실제 보관함에서 제거
            } // 현재 보관함 비우기 종료
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 저장한 탐사 회차 복구
            foreach (RelicData relic in relics) // 복구된 유물 세 종류 순회
            { // 복원 원본 확인 시작
                CollectionAssert.Contains(RelicRunManager.Instance.Inventory.OwnedRelics, relic); // 실제 원본으로 복원 확인
            } // 복원 원본 확인 종료
            int sale = shop.GetRelicSellPrice(relics[1]); // 말린 잎의 판매 금액 조회
            int beforeSale = shop.Gold; // 판매 전 금액 보관
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TrySellRelic(relics[1])); // 복원한 말린 잎 판매 확인
            Assert.AreEqual(beforeSale + sale, shop.Gold); // 판매 대금 지급 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 판매 후 상태 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 판매 후 저장 복원
            Assert.IsFalse(RelicRunManager.Instance.Inventory.ContainsRelic(relics[1].RelicId)); // 판매한 유물 제외 확인
            Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>()); // 실제 조우 접촉과 전투 시작
            yield return WaitFor<BattleSceneSetup>(); // 전투 관리자 준비 대기
            BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 원본 흐름 조회
            Assert.IsTrue(battle.IsInitialized); // 실제 전투 준비 확인
            battle.AllyUnits[0].ApplyPersistentHealth(4); // 첫 아군을 최저 체력으로 설정
            Assert.IsTrue(battle.BattleTurn.TryEscape()); // 일반 전투 도주로 종료 이벤트 발생
            Assert.AreEqual(7, battle.AllyUnits[0].CurrentHealth); // 전투 종료 회복량 확인
            Assert.AreEqual(1, Object.FindObjectsByType<BattleResultView>(FindObjectsSortMode.None).Length); // 단일 종료 결과 화면 표시 확인
            Assert.IsTrue(BattleResultManager.Instance.TryConsumeResult(out BattleResultData result)); // 탐사에 전달되는 결과 스냅샷 조회
            Assert.AreEqual(7, result.AllyStates[0].CurrentHealth); // 탐사 복귀용 아군 체력에 종료 회복 반영 확인
            Assert.IsFalse(BattleResultManager.Instance.TryConsumeResult(out _)); // 같은 도주 결과 재소비 방지 확인
        } // 실제 게임 흐름 검사 종료
        finally // 독립 저장 정리
        { // 시험 파일 정리 시작
            ExplorationSaveService.Remove(path); // 시험 저장과 복구본 제거
        } // 시험 파일 정리 종료
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 통합 검사 종료

    private static IEnumerator WaitFor<T>() where T : Component // 현재 씬에 필요한 객체 생성 대기
    { // 객체 대기 시작
        double deadline = Time.realtimeSinceStartupAsDouble + 30d; // 씬 준비 제한 시각
        while (Object.FindFirstObjectByType<T>() == null) // 실제 게임 객체 생성 확인
        { // 다음 프레임 대기 시작
            Assert.Less(Time.realtimeSinceStartupAsDouble, deadline, typeof(T).Name); // 누락된 게임 객체 진단
            yield return null; // 다음 게임 프레임 대기
        } // 다음 프레임 대기 종료
        yield return null; // 시작 함수 실행 대기
    } // 객체 대기 종료
} // 회복 유물 검사 종료
