using System.Collections; // 코루틴 테스트 기능 사용
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
        Assert.IsFalse(view.transform.Find("ContinueButton").GetComponent<Button>().interactable); // 이어하기 비활성 확인
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
