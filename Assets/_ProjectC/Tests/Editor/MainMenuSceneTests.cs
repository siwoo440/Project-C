using System.Collections; // 코루틴 테스트 기능 사용
using System.Collections.Generic; // 저장 카드 목록 테스트 사용
using NUnit.Framework; // 테스트 기능 사용
using TMPro; // 메뉴 글자 조회 기능 사용
using UnityEditor; // 테스트용 에셋 조회 기능 사용
using UnityEditor.SceneManagement; // 편집기 씬 조회 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.SceneManagement; // 씬 자료형 사용
using UnityEngine.TestTools; // 플레이 모드 전환 기능 사용
using UnityEngine.UI; // 메뉴 이미지와 버튼 조회 기능 사용
public sealed class MainMenuSceneTests // 메인 메뉴 씬 검증
{ // 테스트 클래스 시작
    [Test] // 편집기 테스트 지정
    public void SaveAssetCatalog_ContainsAllProjectItems() // 저장 원본 목록 누락 검사
    { // 저장 원본 검사 시작
        SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 원본 목록 조회
        Assert.IsNotNull(catalog); // 저장 원본 목록 존재 확인
        string[] roots = new string[] // 프로젝트 검색 폴더 생성
        { // 검색 폴더 설정 시작
            "Assets/_ProjectC" // 프로젝트 에셋 폴더 지정
        }; // 검색 폴더 설정 종료
        foreach (string guid in AssetDatabase.FindAssets("t:CardData", roots)) // 프로젝트 카드 에셋 순회
        { // 카드 원본 확인 시작
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid)); // 카드 원본 조회
            CollectionAssert.Contains(catalog.Cards, card); // 카드 복원 목록 포함 확인
        } // 카드 원본 확인 종료
        foreach (string guid in AssetDatabase.FindAssets("t:RelicData", roots)) // 프로젝트 유물 에셋 순회
        { // 유물 원본 확인 시작
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(AssetDatabase.GUIDToAssetPath(guid)); // 유물 원본 조회
            CollectionAssert.Contains(catalog.Relics, relic); // 유물 복원 목록 포함 확인
        } // 유물 원본 확인 종료
        foreach (string guid in AssetDatabase.FindAssets("t:PotionData", roots)) // 프로젝트 포션 에셋 순회
        { // 소모품 원본 확인 시작
            PotionData potion = AssetDatabase.LoadAssetAtPath<PotionData>(AssetDatabase.GUIDToAssetPath(guid)); // 포션 원본 조회
            CollectionAssert.Contains(catalog.Consumables, potion); // 소모품 복원 목록 포함 확인
        } // 소모품 원본 확인 종료
    } // 저장 원본 검사 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator RestoredEmptyRunDeck_StaysEmpty() // 빈 회차 덱 복원 유지 확인
    { // 빈 덱 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterData fool = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 테스트 캐릭터 조회
        PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 테스트 파티 선택 생성
        Assert.IsTrue(selection.TryAdd(fool)); // 테스트 파티원 추가
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 테스트 편성 준비
        DeckData sourceDeck = PartyLoadoutRuntime.Active.Deck; // 시작 덱 조회
        RunDeckManager manager = RunDeckManager.EnsureInstance(); // 회차 덱 관리자 준비
        System.Reflection.MethodInfo restore = typeof(RunDeckManager).GetMethod("RestoreCards", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 저장 회차 덱 복원 동작 조회
        Assert.IsNotNull(restore); // 회차 덱 복원 기능 확인
        restore.Invoke(manager, new object[] // 빈 저장 덱 복원 요청
        { // 복원 요청 인수 시작
            sourceDeck, // 원본 시작 덱 전달
            new List<RunDeckCardEntry>() // 빈 회차 카드 목록 전달
        }); // 복원 요청 인수 종료
        Assert.AreEqual(0, manager.GetActiveCards(sourceDeck).Count); // 빈 회차 덱 유지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 빈 덱 검사 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator ContinueButton_ResumesSavedExploration() // 이어하기 탐사 씬 복원 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로 생성
        string lobbyPath = path + ".lobby"; // 독립 거점 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 탐사 이어하기 검증 시작
            SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
            for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메뉴 진입 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 테스트용 거점 이동
            for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<PartyRosterView>() == null; frame++) // 거점 편성 화면 대기
            { // 거점 대기 시작
                yield return null; // 다음 프레임 대기
            } // 거점 대기 종료
            PartyRosterView rosterView = Object.FindFirstObjectByType<PartyRosterView>(); // 거점 편성 화면 조회
            Button[] rosterButtons = rosterView.GetComponentsInChildren<Button>(); // 캐릭터와 출발 버튼 조회
            rosterButtons[0].onClick.Invoke(); // 첫 캐릭터 선택
            rosterButtons[22].onClick.Invoke(); // 탐사 출발
            for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationPlayerController>() == null; frame++) // 탐사 플레이어 생성 대기
            { // 탐사 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 대기 종료
            yield return null; // 맵과 위치 초기화 대기
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 조회
            ExplorationSessionManager session = ExplorationSessionManager.Instance; // 탐사 상태 관리자 조회
            Vector3 savedPosition = player.transform.position; // 현재 플레이어 위치 보관
            int savedSeed = session.CurrentFloorSeed; // 현재 층 시드 보관
            Assert.IsTrue(session.MarkEventResolved("resume-event-test")); // 복원할 이벤트 상태 기록
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            resources.AddResources(77, 0, 0, 0); // 복원할 골드 설정
            int savedGold = resources.Gold; // 현재 골드 보관
            resources.ResetResources(); // 오래된 거점 저장용 자원 준비
            resources.AddResources(5, 0, 0, 0); // 오래된 거점 골드 설정
            Assert.IsTrue(LobbySaveService.Save(lobbyPath)); // 오래된 거점 상태 저장
            System.IO.File.SetLastWriteTimeUtc(lobbyPath, System.DateTime.UtcNow.AddMinutes(-1)); // 거점 저장 시간 조정
            resources.ResetResources(); // 탐사 저장용 자원 초기화
            resources.AddResources(savedGold, 0, 0, 0); // 탐사 저장 골드 복구
            Assert.IsTrue(ExplorationSaveService.Save(path, savedPosition)); // 탐사 상태 저장
            SceneManager.LoadScene("10_MainMenu"); // 메인 메뉴 복귀
            yield return null; // 메뉴 초기화 대기
            session.ResetExploration(); // 기존 탐사 상태 제거
            RunDeckManager.Instance.ResetToSource(null); // 기존 회차 덱 제거
            PartyLoadoutRuntime.Clear(); // 기존 파티 편성 제거
            resources.ResetResources(); // 기존 골드 제거
            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(); // 메뉴 관리자 조회
            typeof(MainMenuController).GetField("savePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(controller, lobbyPath); // 독립 거점 경로 지정
            System.Reflection.FieldInfo explorationPath = typeof(MainMenuController).GetField("explorationSavePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 탐사 경로 필드 조회
            Assert.IsNotNull(explorationPath); // 탐사 저장 경로 연결 확인
            explorationPath.SetValue(controller, path); // 독립 탐사 경로 지정
            controller.SendMessage("Start"); // 저장 경로 기준 메뉴 갱신
            MainMenuView view = Object.FindFirstObjectByType<MainMenuView>(); // 메뉴 화면 조회
            Button continueButton = view.transform.Find("ContinueButton").GetComponent<Button>(); // 이어하기 버튼 조회
            Assert.IsTrue(continueButton.interactable); // 탐사 저장 이어하기 활성 확인
            continueButton.onClick.Invoke(); // 탐사 이어하기 선택
            for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "30_Exploration"; frame++) // 탐사 씬 전환 대기
            { // 씬 전환 대기 시작
                yield return null; // 다음 프레임 대기
            } // 씬 전환 대기 종료
            for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationPlayerController>() == null; frame++) // 복원 플레이어 생성 대기
            { // 플레이어 대기 시작
                yield return null; // 다음 프레임 대기
            } // 플레이어 대기 종료
            player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 복원 플레이어 조회
            Assert.AreEqual("30_Exploration", SceneManager.GetActiveScene().name); // 탐사 씬 복귀 확인
            Assert.AreEqual(savedSeed, session.CurrentFloorSeed); // 저장 시드 복원 확인
            Assert.Less(Vector3.Distance(savedPosition, player.transform.position), 0.1f); // 저장 위치 복원 확인
            Assert.AreEqual(savedGold, resources.Gold); // 저장 골드 복원 확인
            Assert.IsTrue(session.IsEventResolved("resume-event-test")); // 이벤트 처리 상태 복원 확인
        } // 탐사 이어하기 검증 종료
        finally // 테스트 파일 정리
        { // 정리 시작
            if (System.IO.File.Exists(path)) // 탐사 테스트 파일 존재 확인
            { // 탐사 파일 제거 시작
                System.IO.File.Delete(path); // 탐사 테스트 파일 제거
            } // 탐사 파일 제거 종료
            if (System.IO.File.Exists(lobbyPath)) // 거점 테스트 파일 존재 확인
            { // 거점 파일 제거 시작
                System.IO.File.Delete(lobbyPath); // 거점 테스트 파일 제거
            } // 거점 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [System.Serializable] // 탐사 저장 테스트 역직렬화 지정
    private sealed class ExplorationTestSnapshot // 탐사 저장 검사 자료
    { // 탐사 검사 자료 시작
        public int version; // 저장 버전
        public int floor; // 탐사 층
        public int seed; // 현재 층 시드
        public Vector3 position; // 플레이어 위치
        public string[] party; // 파티 식별자 목록
        public ExplorationTestCard[] sourceCards; // 시작 카드 목록
        public ExplorationTestCard[] cards; // 회차 카드 목록
        public string[] cleared; // 클리어 조우 목록
        public string[] events; // 처리 이벤트 목록
        public string[] relics; // 유물 목록
        public string[] consumables; // 소모품 슬롯 목록
        public string shopRoom; // 상점 방 식별자
        public string[] purchasedOffers; // 판매 완료 상품 목록
        public int hazardHealth; // 대기 체력 피해
        public int hazardMental; // 대기 정신력 피해
        public string lobbyJson; // 거점 상태 저장 내용
    } // 탐사 검사 자료 종료
    [System.Serializable] // 카드 저장 테스트 역직렬화 지정
    private sealed class ExplorationTestCard // 회차 카드 검사 자료
    { // 카드 검사 자료 시작
        public string card; // 카드 식별자
        public string owner; // 카드 소유자 식별자
        public int upgrade; // 카드 강화 단계
    } // 카드 검사 자료 종료
    [System.Serializable] // 거점 저장 테스트 역직렬화 지정
    private sealed class LobbyTestSnapshot // 거점 저장 검사 자료
    { // 거점 검사 자료 시작
        public int gold; // 저장 골드
    } // 거점 검사 자료 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator ExplorationSave_WritesRunStateAndLobbySnapshot() // 탐사 저장 내용 검증
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        System.Type service = typeof(MainMenuController).Assembly.GetType("ExplorationSaveService"); // 탐사 저장 서비스 조회
        Assert.IsNotNull(service); // 탐사 저장 서비스 존재 확인
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 테스트 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 탐사 저장 검증 시작
            CharacterData fool = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 파티 캐릭터 조회
            PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 테스트 파티 선택 생성
            Assert.IsTrue(selection.TryAdd(fool)); // 테스트 파티원 추가
            Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 탐사 편성 생성
            RunDeckManager deck = RunDeckManager.EnsureInstance(); // 회차 덱 관리자 준비
            deck.ResetToSource(PartyLoadoutRuntime.Active.Deck); // 탐사 시작 덱 적용
            Assert.IsTrue(deck.TryUpgradeAt(0)); // 첫 카드 강화
            ExplorationSessionManager session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 관리자 준비
            session.ResetExploration(); // 이전 탐사 상태 제거
            session.AdvanceFloor(); // 두 번째 층 이동
            session.SetCurrentFloorSeed(12345); // 현재 층 시드 지정
            Assert.IsTrue(session.MarkEventResolved("event-test")); // 처리 이벤트 등록
            EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_Test_A"); // 테스트 조우 조회
            Assert.IsTrue(session.BeginEncounter("encounter-test", encounter, Vector3.zero, Vector3.right)); // 테스트 조우 시작
            BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(fool); // 생존 아군 생성
            BattleUnitRuntime[] allies = new BattleUnitRuntime[] // 테스트 아군 목록 생성
            { // 테스트 아군 목록 시작
                ally // 생존 아군 등록
            }; // 테스트 아군 목록 종료
            session.ResolveBattleResult(new BattleResultData(BattleResult.Victory, BattleType.Normal, 1, allies, new string[0])); // 테스트 조우 승리 처리
            session.AddPendingHazardPenalty(3, 4); // 대기 환경 피해 설정
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            resources.AddResources(100, 0, 0, 0); // 거점 자원 변경
            ShopCatalogData catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 테스트 상점 목록 조회
            ShopRunManager shop = ShopRunManager.EnsureInstance(); // 상점 관리자 준비
            Assert.IsTrue(shop.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 상점 준비
            shop.BeginRoomVisit("shop-test"); // 테스트 상점 방 방문
            Assert.AreEqual(ShopPurchaseResult.Success, shop.TryPurchase(catalog.Offers[5])); // 테스트 상품 구매
            int savedGold = resources.Gold; // 저장 골드 보관
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>("Assets/_ProjectC/Data/Relics/TEST_MAX_HP.asset"); // 테스트 유물 조회
            ConsumableItemData potion = AssetDatabase.LoadAssetAtPath<ConsumableItemData>("Assets/_ProjectC/Data/Consumables/POTION_TEST_HEAL.asset"); // 테스트 소모품 조회
            Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 유물 획득 확인
            Assert.IsTrue(ConsumableRunManager.EnsureInstance().Inventory.TrySetForDebug(3, potion)); // 지정 슬롯 소모품 배치
            Vector3 position = new Vector3(12f, -3f, 0f); // 저장할 플레이어 위치
            Assert.IsTrue((bool)service.GetMethod("Save").Invoke(null, new object[] { path, position })); // 탐사 상태 파일 저장
            ExplorationTestSnapshot snapshot = JsonUtility.FromJson<ExplorationTestSnapshot>(System.IO.File.ReadAllText(path)); // 탐사 저장 파일 읽기
            Assert.AreEqual(1, snapshot.version); // 저장 버전 확인
            Assert.AreEqual(2, snapshot.floor); // 탐사 층 확인
            Assert.AreEqual(12345, snapshot.seed); // 맵 시드 확인
            Assert.AreEqual(position, snapshot.position); // 플레이어 위치 확인
            Assert.AreEqual(1, snapshot.party.Length); // 파티 인원 확인
            Assert.AreEqual(fool.CharacterId, snapshot.party[0]); // 파티 순서 확인
            Assert.AreEqual(PartyLoadoutRuntime.Active.Deck.CardCount, snapshot.sourceCards.Length); // 탐사 시작 덱 카드 수 확인
            Assert.AreEqual(deck.CardCount, snapshot.cards.Length); // 회차 카드 수 확인
            Assert.AreEqual(deck.Cards[0].Card.CardId, snapshot.cards[0].card); // 첫 카드 식별자 확인
            Assert.AreEqual(fool.CharacterId, snapshot.cards[0].owner); // 첫 카드 소유자 확인
            Assert.AreEqual(1, snapshot.cards[0].upgrade); // 첫 카드 강화 확인
            CollectionAssert.Contains(snapshot.events, "event-test"); // 처리 이벤트 확인
            CollectionAssert.Contains(snapshot.cleared, "encounter-test"); // 클리어 조우 확인
            CollectionAssert.Contains(snapshot.relics, relic.RelicId); // 보유 유물 확인
            Assert.AreEqual(potion.ItemId, snapshot.consumables[3]); // 소모품 슬롯 확인
            Assert.AreEqual("shop-test", snapshot.shopRoom); // 상점 방 식별자 확인
            CollectionAssert.Contains(snapshot.purchasedOffers, catalog.Offers[5].OfferId); // 판매 완료 상품 확인
            Assert.AreEqual(3, snapshot.hazardHealth); // 대기 체력 피해 확인
            Assert.AreEqual(4, snapshot.hazardMental); // 대기 정신력 피해 확인
            Assert.AreEqual(savedGold, JsonUtility.FromJson<LobbyTestSnapshot>(snapshot.lobbyJson).gold); // 거점 자원 포함 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, position)); // 기존 탐사 저장 파일 교체 확인
            string previousJson = System.IO.File.ReadAllText(path); // 이전 저장 내용 보관
            Assert.IsFalse(ExplorationSaveService.Save(path, new Vector3(float.NaN, 0f, 0f))); // 잘못된 위치 저장 차단 확인
            Assert.AreEqual(previousJson, System.IO.File.ReadAllText(path)); // 기존 저장 파일 보존 확인
            Assert.IsTrue(session.BeginEncounter("pending-test", encounter, Vector3.zero, Vector3.right)); // 전투 진행 상태 생성
            Assert.IsFalse(ExplorationSaveService.Save(path, position)); // 진행 중 전투 저장 차단 확인
            Assert.AreEqual(previousJson, System.IO.File.ReadAllText(path)); // 전투 중 기존 저장 보존 확인
            System.Reflection.MethodInfo load = service.GetMethod("Load"); // 탐사 복원 동작 조회
            Assert.IsNotNull(load); // 탐사 복원 기능 존재 확인
            session.ResetExploration(); // 현재 탐사 상태 제거
            shop.ResetSession(); // 현재 상점 상태 제거
            deck.ResetToSource(null); // 현재 회차 덱 제거
            PartyLoadoutRuntime.Clear(); // 현재 탐사 편성 제거
            ConsumableRunManager.Instance.Inventory.Clear(); // 현재 소모품 제거
            RelicRunManager.Instance.TryRemove(relic.RelicId); // 현재 유물 제거
            resources.ResetResources(); // 현재 자원 제거
            Assert.IsTrue((bool)load.Invoke(null, new object[] { path })); // 파일에서 탐사 상태 복원
            Assert.AreEqual(2, session.CurrentFloor); // 복원 탐사 층 확인
            Assert.AreEqual(12345, session.CurrentFloorSeed); // 복원 맵 시드 확인
            Assert.AreEqual(position, session.GetPlayerSpawnPosition(Vector3.zero)); // 복원 플레이어 위치 확인
            Assert.IsTrue(session.IsEncounterCleared("encounter-test")); // 복원 조우 상태 확인
            Assert.IsTrue(session.IsEventResolved("event-test")); // 복원 이벤트 상태 확인
            Assert.AreEqual(3, session.PendingHazardHealthDamage); // 복원 대기 체력 피해 확인
            Assert.AreEqual(4, session.PendingHazardMentalDamage); // 복원 대기 정신력 피해 확인
            Assert.AreEqual(fool.CharacterId, PartyLoadoutRuntime.Active.Party.Members[0].CharacterId); // 복원 파티 확인
            Assert.AreEqual(snapshot.cards.Length, deck.CardCount); // 복원 회차 덱 수 확인
            Assert.AreEqual(1, deck.Cards[0].UpgradeLevel); // 복원 카드 강화 확인
            Assert.AreEqual(savedGold, resources.Gold); // 복원 골드 확인
            Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic(relic.RelicId)); // 복원 유물 확인
            Assert.AreEqual(potion.ItemId, ConsumableRunManager.Instance.Inventory.GetItem(3).ItemId); // 복원 소모품 슬롯 확인
            Assert.IsTrue(shop.IsPurchased(catalog.Offers[5])); // 복원 상점 판매 상태 확인
            System.IO.File.WriteAllText(path, previousJson.Replace(snapshot.cards[0].card, "INVALID_CARD_ID")); // 알 수 없는 카드 저장 작성
            Assert.IsFalse((bool)load.Invoke(null, new object[] { path })); // 잘못된 카드 복원 차단 확인
            Assert.AreEqual(savedGold, resources.Gold); // 복원 실패 시 기존 골드 유지 확인
            Assert.AreEqual(2, session.CurrentFloor); // 복원 실패 시 기존 탐사 상태 유지 확인
        } // 탐사 저장 검증 종료
        finally // 테스트 파일 정리
        { // 정리 시작
            if (System.IO.File.Exists(path)) // 테스트 파일 존재 확인
            { // 파일 제거 시작
                System.IO.File.Delete(path); // 테스트 파일 제거
            } // 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator NewGameWarning_CancelKeepsCurrentProgress() // 새 게임 취소 상태 유지 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메뉴 진입 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        yield return null; // 메뉴 연결 대기
        MainMenuView view = Object.FindFirstObjectByType<MainMenuView>(); // 메뉴 화면 조회
        PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
        resources.AddResources(73, 0, 0, 0); // 유지할 현재 자원 설정
        int originalGold = resources.Gold; // 현재 골드 보관
        view.NewGameButton.onClick.Invoke(); // 새로하기 선택
        Transform panel = view.transform.Find("NewGameConfirmation"); // 경고 패널 조회
        Assert.IsNotNull(panel); // 경고 패널 존재 확인
        Assert.IsTrue(panel.gameObject.activeSelf); // 경고 패널 표시 확인
        string warning = panel.Find("WarningMessage").GetComponent<TMP_Text>().text; // 경고 문구 조회
        StringAssert.Contains("저장", warning); // 저장 데이터 경고 확인
        StringAssert.Contains("처음", warning); // 처음부터 시작 경고 확인
        panel.Find("CancelNewGame").GetComponent<Button>().onClick.Invoke(); // 새 게임 취소
        Assert.IsFalse(panel.gameObject.activeSelf); // 경고 패널 닫힘 확인
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메인 메뉴 유지 확인
        Assert.AreEqual(originalGold, resources.Gold); // 기존 진행 상태 유지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator NewGameWarning_ConfirmDiscardsOldExplorationSave() // 새 게임 확인 시 이전 탐사 저장 정리 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 새 게임 저장 검증 시작
            System.IO.File.WriteAllText(path, "이전 탐사 저장"); // 이전 탐사 저장 준비
            SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
            for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메뉴 진입 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            yield return null; // 메뉴 연결 대기
            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(); // 메뉴 관리자 조회
            System.Reflection.FieldInfo explorationPath = typeof(MainMenuController).GetField("explorationSavePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 탐사 저장 경로 필드 조회
            Assert.IsNotNull(explorationPath); // 탐사 저장 경로 연결 확인
            explorationPath.SetValue(controller, path); // 독립 탐사 저장 경로 지정
            MainMenuView view = Object.FindFirstObjectByType<MainMenuView>(); // 메뉴 화면 조회
            view.NewGameButton.onClick.Invoke(); // 새 게임 경고 표시
            Transform panel = view.transform.Find("NewGameConfirmation"); // 경고 패널 조회
            panel.Find("CancelNewGame").GetComponent<Button>().onClick.Invoke(); // 새 게임 취소
            Assert.IsTrue(System.IO.File.Exists(path)); // 취소 시 이전 저장 유지 확인
            view.NewGameButton.onClick.Invoke(); // 새 게임 경고 재표시
            panel.Find("ConfirmNewGame").GetComponent<Button>().onClick.Invoke(); // 새 게임 확인
            yield return null; // 거점 씬 이동 대기
            Assert.IsFalse(System.IO.File.Exists(path)); // 확인 시 이전 탐사 저장 정리 확인
        } // 새 게임 저장 검증 종료
        finally // 테스트 파일 정리
        { // 정리 시작
            if (System.IO.File.Exists(path)) // 테스트 파일 존재 확인
            { // 파일 제거 시작
                System.IO.File.Delete(path); // 테스트 파일 제거
            } // 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator ContinueButton_ValidSaveRestoresLobby() // 이어하기 정상 저장 복원 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 테스트 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 이어하기 검증 시작
            SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
            for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메뉴 진입 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            yield return null; // 메뉴 연결 대기
            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(); // 메뉴 관리자 조회
            MainMenuView view = Object.FindFirstObjectByType<MainMenuView>(); // 메뉴 화면 조회
            System.Reflection.FieldInfo savePath = typeof(MainMenuController).GetField("savePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 테스트 경로 필드 조회
            Assert.IsNotNull(savePath); // 저장 경로 연결 확인
            savePath.SetValue(controller, path); // 독립 테스트 경로 지정
            typeof(MainMenuController).GetField("explorationSavePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(controller, path + ".run"); // 독립 탐사 경로 지정
            controller.SendMessage("Start"); // 테스트 경로 기준 메뉴 갱신
            Button continueButton = view.transform.Find("ContinueButton").GetComponent<Button>(); // 이어하기 버튼 조회
            Assert.IsFalse(continueButton.interactable); // 파일 없음 비활성 확인
            System.IO.File.WriteAllText(path, "{\"version\":1}"); // 잘못된 저장 작성
            controller.SendMessage("Start"); // 손상 저장 기준 메뉴 갱신
            Assert.IsFalse(continueButton.interactable); // 손상 저장 비활성 확인
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            resources.AddResources(41, 0, 0, 0); // 저장할 자원 설정
            int savedGold = resources.Gold; // 저장 골드 보관
            Assert.IsTrue(LobbySaveService.Save(path)); // 유효한 거점 저장 작성
            resources.ResetResources(); // 현재 자원 초기화
            ExplorationSessionManager.EnsureInstance().AdvanceFloor(); // 이전 탐사 상태 준비
            controller.SendMessage("Start"); // 유효 저장 기준 메뉴 갱신
            Assert.IsTrue(continueButton.interactable); // 유효 저장 활성 확인
            continueButton.onClick.Invoke(); // 이어하기 실행
            yield return null; // 거점 씬 전환 대기
            yield return null; // 거점 초기화 대기
            Assert.AreEqual("20_Lobby", SceneManager.GetActiveScene().name); // 거점 복원 진입 확인
            Assert.AreEqual(savedGold, resources.Gold); // 저장 자원 복원 확인
            Assert.AreEqual(1, ExplorationSessionManager.Instance.CurrentFloor); // 이전 탐사 상태 제거 확인
        } // 이어하기 검증 종료
        finally // 테스트 파일 정리
        { // 정리 시작
            if (System.IO.File.Exists(path)) // 테스트 파일 존재 확인
            { // 파일 제거 시작
                System.IO.File.Delete(path); // 테스트 파일 제거
            } // 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator ContinueButton_ChangedFileDoesNotReplaceCurrentState() // 클릭 전 저장 손상 처리 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 테스트 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 손상 저장 검증 시작
            SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
            for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메뉴 진입 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            yield return null; // 메뉴 연결 대기
            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(); // 메뉴 관리자 조회
            MainMenuView view = Object.FindFirstObjectByType<MainMenuView>(); // 메뉴 화면 조회
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            resources.AddResources(55, 0, 0, 0); // 현재 자원 설정
            int currentGold = resources.Gold; // 현재 골드 보관
            Assert.IsTrue(LobbySaveService.Save(path)); // 유효한 저장 파일 작성
            typeof(MainMenuController).GetField("savePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(controller, path); // 독립 테스트 경로 지정
            typeof(MainMenuController).GetField("explorationSavePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(controller, path + ".run"); // 독립 탐사 경로 지정
            controller.SendMessage("Start"); // 저장 기준 메뉴 갱신
            Button continueButton = view.transform.Find("ContinueButton").GetComponent<Button>(); // 이어하기 버튼 조회
            Assert.IsTrue(continueButton.interactable); // 정상 저장 활성 확인
            System.IO.File.WriteAllText(path, "{\"version\":1}"); // 클릭 전 저장 손상
            continueButton.onClick.Invoke(); // 손상된 저장 이어하기 시도
            yield return null; // 씬 전환 여부 대기
            Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메뉴 유지 확인
            Assert.AreEqual(currentGold, resources.Gold); // 현재 자원 유지 확인
            Assert.IsFalse(continueButton.interactable); // 이어하기 비활성 확인
        } // 손상 저장 검증 종료
        finally // 테스트 파일 정리
        { // 정리 시작
            if (System.IO.File.Exists(path)) // 테스트 파일 존재 확인
            { // 파일 제거 시작
                System.IO.File.Delete(path); // 테스트 파일 제거
            } // 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator LobbySave_RestoresProgressAndRecovery() // 거점 진행 상태 저장 복원 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        System.Type service = typeof(MainMenuController).Assembly.GetType("LobbySaveService"); // 저장 서비스 형식 조회
        Assert.IsNotNull(service); // 저장 서비스 존재 확인
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 테스트 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 저장 검증 시작
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            FacilityUpgradeManager facilities = FacilityUpgradeManager.EnsureInstance(); // 설비 관리자 준비
            CharacterProgressionManager progression = CharacterProgressionManager.EnsureInstance(); // 성장 관리자 준비
            CharacterAffinityManager affinity = CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비
            BattleResultManager result = BattleResultManager.EnsureInstance(); // 캐릭터 상태 관리자 준비
            CharacterRecoveryManager recovery = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
            resources.AddResources(10000, 500, 500, 500); // 저장할 자원 준비
            Assert.IsTrue(facilities.TryUpgrade(FacilityType.PowerSupply)); // 저장할 설비 강화
            int savedGold = resources.Gold; // 강화 후 골드 보관
            int savedScrew = resources.Screw; // 강화 후 나사 보관
            progression.AddExperience(100); // 저장할 성장 경험치 준비
            affinity.GrantExplorationSuccessAffinity(7); // 저장할 호감도 준비
            int savedLevel = progression.Level; // 현재 레벨 보관
            int savedExperience = progression.CurrentExperience; // 현재 경험치 보관
            CharacterData fool = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 테스트 캐릭터 조회
            BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(fool); // 전투 캐릭터 생성
            unit.TakeDamage(100000, BattleDamageType.Physical); // 테스트 캐릭터 사망 처리
            BattleResultData previous = new BattleResultData(BattleResult.Defeat, BattleType.Normal, 1, new[] { unit }, new string[0]); // 사망 결과 생성
            Assert.IsTrue(result.StoreResult(previous)); // 사망 상태 기록
            Assert.IsTrue(recovery.RegisterDeadCharacter(fool, result)); // 사망 캐릭터 회복 등록
            Assert.IsTrue((bool)service.GetMethod("Save").Invoke(null, new object[] { path })); // 거점 상태 파일 저장
            Assert.IsTrue((bool)service.GetMethod("Save").Invoke(null, new object[] { path })); // 기존 저장 파일 교체
            resources.ResetResources(); // 자원 초기화
            facilities.ResetFacilities(); // 설비 초기화
            progression.ResetProgress(); // 성장 초기화
            affinity.ResetAffinity(); // 호감도 초기화
            result.ResetSavedPartyState(); // 캐릭터 상태 초기화
            recovery.ResetRecoveryState(); // 회복 상태 초기화
            Assert.IsTrue((bool)service.GetMethod("Load").Invoke(null, new object[] { path })); // 파일에서 거점 상태 복원
            Assert.AreEqual(savedGold, resources.Gold); // 골드 복원 확인
            Assert.AreEqual(savedScrew, resources.Screw); // 나사 복원 확인
            Assert.AreEqual(1, facilities.GetLevel(FacilityType.PowerSupply)); // 설비 복원 확인
            Assert.AreEqual(savedLevel, progression.Level); // 성장 레벨 복원 확인
            Assert.AreEqual(savedExperience, progression.CurrentExperience); // 경험치 복원 확인
            Assert.AreEqual(7, affinity.Affinity); // 호감도 복원 확인
            Assert.IsTrue(recovery.IsDead(fool)); // 사망 상태 복원 확인
            Assert.AreEqual(2, recovery.GetRemainingRecoveryExpeditions(fool)); // 회복 횟수 복원 확인
            Assert.IsFalse(result.HasPendingResult); // 이전 전투 결과 제거 확인
        } // 저장 검증 종료
        finally // 테스트 파일 정리
        { // 정리 시작
            if (System.IO.File.Exists(path)) // 테스트 파일 존재 확인
            { // 파일 제거 시작
                System.IO.File.Delete(path); // 테스트 파일 제거
            } // 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator LobbySave_RejectsInvalidFileWithoutChangingState() // 잘못된 저장의 상태 보호 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 테스트 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 오류 검증 시작
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            resources.AddResources(37, 4, 0, 0); // 기존 자원 설정
            int originalGold = resources.Gold; // 기존 골드 보관
            Assert.IsFalse(LobbySaveService.Load(path)); // 없는 파일 복원 차단 확인
            System.IO.File.WriteAllText(path, "{\"version\":1,\"gold\":999}"); // 필수 항목이 누락된 저장 생성
            Assert.IsFalse(LobbySaveService.Load(path)); // 잘못된 저장 복원 차단 확인
            Assert.AreEqual(originalGold, resources.Gold); // 기존 자원 유지 확인
        } // 오류 검증 종료
        finally // 테스트 파일 정리
        { // 정리 시작
            if (System.IO.File.Exists(path)) // 테스트 파일 존재 확인
            { // 파일 제거 시작
                System.IO.File.Delete(path); // 테스트 파일 제거
            } // 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator LobbySave_WritesAfterResourceChange() // 거점 변경 후 자동 저장 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        System.Type autosaveType = typeof(MainMenuController).Assembly.GetType("LobbyAutosaveController"); // 자동 저장 컴포넌트 조회
        Assert.IsNotNull(autosaveType); // 자동 저장 컴포넌트 존재 확인
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 테스트 저장 경로 생성
        GameObject owner = new GameObject("LobbyAutosaveTest"); // 테스트 저장 관리자 생성
        try // 테스트 파일과 객체 정리 보장
        { // 자동 저장 검증 시작
            Component autosave = owner.AddComponent(autosaveType); // 자동 저장 컴포넌트 추가
            autosaveType.GetMethod("Initialize").Invoke(autosave, new object[] { path }); // 테스트 저장 경로 연결
            yield return null; // 최초 저장 프레임 대기
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 조회
            resources.AddResources(41, 0, 0, 0); // 자동 저장 대상 자원 변경
            int expectedGold = resources.Gold; // 변경 골드 보관
            yield return null; // 변경 저장 프레임 대기
            resources.ResetResources(); // 저장 후 자원 변경
            Assert.IsTrue(LobbySaveService.Load(path)); // 자동 저장 파일 복원
            Assert.AreEqual(expectedGold, resources.Gold); // 자동 저장 자원 확인
        } // 자동 저장 검증 종료
        finally // 테스트 파일과 객체 정리
        { // 정리 시작
            Object.Destroy(owner); // 테스트 관리자 제거
            if (System.IO.File.Exists(path)) // 테스트 파일 존재 확인
            { // 파일 제거 시작
                System.IO.File.Delete(path); // 테스트 파일 제거
            } // 파일 제거 종료
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 실행 흐름 테스트 지정
    public IEnumerator MenuButtons_OpenSettingsReturnAndStartLobby() // 실제 메뉴 이동 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메뉴 진입 대기
        { // 대기 시작
            yield return null; // 다음 프레임 대기
        } // 대기 종료
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메뉴 진입 확인
        yield return null; // 메뉴 이벤트 연결 대기
        MainMenuView view = Object.FindFirstObjectByType<MainMenuView>(); // 메뉴 화면 조회
        Assert.IsNotNull(view); // 새 메뉴 화면 존재 확인
        Assert.AreEqual(LobbySaveService.CanLoad(LobbySaveService.DefaultPath) || ExplorationSaveService.CanLoad(ExplorationSaveService.DefaultPath), view.transform.Find("ContinueButton").GetComponent<Button>().interactable); // 저장 상태별 이어하기 확인
        view.transform.Find("SettingsButton").GetComponent<Button>().onClick.Invoke(); // 설정 버튼 실행
        yield return null; // 설정 씬 전환 대기
        Assert.AreEqual("90_Settings", SceneManager.GetActiveScene().name); // 설정 진입 확인
        Object.FindFirstObjectByType<SettingsController>().Back(); // 메뉴 복귀 요청
        yield return null; // 메뉴 씬 복귀 대기
        yield return null; // 메뉴 이벤트 재연결 대기
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메뉴 복귀 확인
        view = Object.FindFirstObjectByType<MainMenuView>(); // 복귀 메뉴 화면 조회
        PlayerResourceManager.EnsureInstance().AddResources(23, 4, 5, 6); // 이전 게임 자원 준비
        CharacterProgressionManager.EnsureInstance().AddExperience(100); // 이전 게임 성장 준비
        CharacterAffinityManager.EnsureInstance().GrantExplorationSuccessAffinity(7); // 이전 게임 호감도 준비
        ExplorationSessionManager.EnsureInstance().AdvanceFloor(); // 이전 탐사 층 준비
        RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>("Assets/_ProjectC/Data/Relics/TEST_MAX_HP.asset"); // 테스트 유물 조회
        ConsumableItemData potion = AssetDatabase.LoadAssetAtPath<ConsumableItemData>("Assets/_ProjectC/Data/Consumables/POTION_TEST_HEAL.asset"); // 테스트 소모품 조회
        CharacterData world = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_ProjectC/ScriptableObjects/Characters/ARC_21.asset"); // 세계 캐릭터 조회
        bool worldWasUnlocked = CharacterUnlockProgress.Shared.IsUnlocked(world); // 기존 세계 해금 상태 보관
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 이전 유물 획득 확인
        Assert.IsTrue(ConsumableRunManager.EnsureInstance().TryAcquire(potion, out int potionSlot)); // 이전 소모품 획득 확인
        CharacterData fool = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_ProjectC/ScriptableObjects/Characters/ARC_00.asset"); // 바보 캐릭터 조회
        PartySelection previousSelection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 이전 파티 선택 생성
        Assert.IsTrue(previousSelection.TryAdd(fool)); // 이전 파티 캐릭터 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(previousSelection)); // 이전 파티 편성 준비
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 이전 파티 상태 등록
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 이전 회차 덱 준비
        BattleResultData previousResult = new BattleResultData(BattleResult.Victory, BattleType.Normal, 1, new BattleUnitRuntime[0], new string[0]); // 이전 전투 결과 준비
        Assert.IsTrue(BattleResultManager.Instance.StoreResult(previousResult)); // 이전 전투 결과 등록
        ShopCatalogData catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/_ProjectC/Resources/Shop/ShopCatalog_Test.asset"); // 테스트 상점 목록 조회
        Assert.IsTrue(ShopRunManager.EnsureInstance().Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 이전 상점 준비
        Assert.IsTrue(RestRoomRunManager.EnsureInstance().Prepare(PartyLoadoutRuntime.Active.Deck)); // 이전 휴식 준비
        view.NewGameButton.onClick.Invoke(); // 새로하기 버튼 실행
        Transform warningPanel = view.transform.Find("NewGameConfirmation"); // 새 게임 경고 패널 조회
        Assert.IsNotNull(warningPanel); // 경고 패널 존재 확인
        Assert.IsTrue(warningPanel.gameObject.activeSelf); // 경고 패널 표시 확인
        warningPanel.Find("ConfirmNewGame").GetComponent<Button>().onClick.Invoke(); // 새 게임 시작 확인
        yield return null; // 거점 씬 전환 대기
        yield return null; // 거점 초기화 대기
        Assert.AreEqual("20_Lobby", SceneManager.GetActiveScene().name); // 거점 진입 확인
        Assert.IsNotNull(Object.FindFirstObjectByType<PartyRosterView>()); // 편성 화면 연결 확인
        Assert.AreEqual(0, PlayerResourceManager.Instance.Gold); // 이전 골드 초기화 확인
        Assert.AreEqual(0, PlayerResourceManager.Instance.Screw); // 이전 나사 초기화 확인
        Assert.AreEqual(1, CharacterProgressionManager.Instance.Level); // 이전 성장 초기화 확인
        Assert.AreEqual(1, ExplorationSessionManager.Instance.CurrentFloor); // 이전 탐사 층 초기화 확인
        Assert.AreEqual(0, CharacterAffinityManager.Instance.Affinity); // 이전 호감도 초기화 확인
        Assert.AreEqual(0, RelicRunManager.Instance.Inventory.Count); // 이전 유물 초기화 확인
        Assert.AreEqual(0, ConsumableRunManager.Instance.Inventory.Count); // 이전 소모품 초기화 확인
        Assert.IsNull(BattleResultManager.Instance.ActiveParty); // 이전 전투 파티 해제 확인
        Assert.IsFalse(BattleResultManager.Instance.HasPendingResult); // 이전 전투 결과 제거 확인
        Assert.AreEqual(0, RunDeckManager.Instance.CardCount); // 이전 회차 덱 제거 확인
        Assert.IsNull(PartyLoadoutRuntime.Active); // 이전 파티 편성 제거 확인
        Assert.IsFalse(ShopRunManager.Instance.IsReady); // 이전 상점 상태 제거 확인
        Assert.IsNull(RestRoomRunManager.Instance.SourceDeck); // 이전 휴식 덱 제거 확인
        Assert.AreEqual(worldWasUnlocked, CharacterUnlockProgress.Shared.IsUnlocked(world)); // 기존 세계 해금 유지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void MenuBindings_InvokeEachActionOnceAfterRebinding() // 메뉴 중복 실행 방지 확인
    { // 테스트 시작
        GameObject canvasObject = new GameObject("TestCanvas", typeof(Canvas)); // 테스트 캔버스 생성
        try // 테스트 객체 정리 보장
        { // 검사 구역 시작
            MainMenuView view = MainMenuView.Build(canvasObject.GetComponent<Canvas>()); // 메뉴 화면 생성
            int[] counts = new int[4]; // 실행 횟수 목록
            view.Bind(() => counts[0]++, () => counts[1]++, () => counts[2]++, () => counts[3]++, true); // 첫 메뉴 동작 연결
            view.Bind(() => counts[0]++, () => counts[1]++, () => counts[2]++, () => counts[3]++, true); // 메뉴 동작 재연결
            foreach (Button button in view.GetComponentsInChildren<Button>()) // 메뉴 버튼 순회
            { // 버튼 실행 시작
                button.onClick.Invoke(); // 연결 동작 실행
            } // 버튼 실행 종료
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, counts); // 모든 동작 단일 실행 확인
        } // 검사 구역 종료
        finally // 객체 정리 처리
        { // 정리 시작
            Object.DestroyImmediate(canvasObject); // 테스트 캔버스 제거
        } // 정리 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void MainMenuScene_HasSeparateArtworkAndFourButtons() // 독립 이미지와 네 버튼 확인
    { // 테스트 시작
        Scene scene = SceneManager.GetSceneByPath("Assets/_ProjectC/Scenes/10_MainMenu.unity"); // 기존 메뉴 씬 조회
        bool openedForTest = !scene.IsValid() || !scene.isLoaded; // 테스트용 씬 열기 필요 확인
        if (openedForTest) // 미개방 씬 확인
        { // 씬 열기 시작
            scene = EditorSceneManager.OpenScene("Assets/_ProjectC/Scenes/10_MainMenu.unity", OpenSceneMode.Additive); // 메인 메뉴 씬 열기
        } // 씬 열기 종료
        try // 씬 검사 시작
        { // 검사 구역 시작
            Transform menu = Find(scene, "MainMenuUI"); // 새 메인 메뉴 화면 조회
            Assert.IsNotNull(menu); // 메뉴 화면 존재 확인
            Image background = menu.Find("MenuBackground").GetComponent<Image>(); // 배경 이미지 조회
            Image logo = menu.Find("GameLogo").GetComponent<Image>(); // 로고 이미지 조회
            Assert.IsNotNull(background.sprite); // 배경 이미지 연결 확인
            Assert.IsNotNull(logo.sprite); // 로고 이미지 연결 확인
            Assert.AreNotSame(background.sprite, logo.sprite); // 별도 이미지 사용 확인
            string[] names = { "NewGameButton", "ContinueButton", "SettingsButton", "QuitButton" }; // 버튼 이름 목록
            string[] labels = { "새로하기", "이어하기", "설정", "나가기" }; // 버튼 문구 목록
            Assert.AreEqual(4, menu.GetComponentsInChildren<Button>(true).Length); // 네 버튼 구성 확인
            for (int index = 0; index < names.Length; index++) // 버튼 목록 순회
            { // 개별 버튼 확인 시작
                Button button = menu.Find(names[index]).GetComponent<Button>(); // 개별 버튼 조회
                Assert.IsNotNull(button.image.sprite, names[index]); // 버튼 장식 이미지 연결 확인
                Assert.AreEqual(labels[index], button.GetComponentInChildren<TMP_Text>().text); // 실제 텍스트 문구 확인
                Assert.AreEqual(index != 1, button.interactable); // 저장 미구현 이어하기 비활성 확인
            } // 개별 버튼 확인 종료
        } // 검사 구역 종료
        finally // 씬 정리 처리
        { // 정리 시작
            if (openedForTest) // 테스트용으로 연 씬 확인
            { // 검사 씬 정리 시작
                EditorSceneManager.CloseScene(scene, true); // 검사 씬 닫기
            } // 검사 씬 정리 종료
        } // 정리 종료
    } // 테스트 종료
    private static Transform Find(Scene scene, string name) // 씬 내부 오브젝트 조회
    { // 조회 시작
        foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
        { // 루트 확인 시작
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) // 전체 자식 순회
            { // 자식 확인 시작
                if (child.name == name) // 이름 일치 확인
                { // 일치 처리 시작
                    return child; // 일치 오브젝트 반환
                } // 일치 처리 종료
            } // 자식 확인 종료
        } // 루트 확인 종료
        return null; // 조회 실패 반환
    } // 조회 종료
} // 테스트 클래스 종료
