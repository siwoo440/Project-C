using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 전투 적 목록 복사 사용
using System.IO; // 실제 저장 파일 확인 사용
using NUnit.Framework; // 통합 흐름 검증 사용
using TMPro; // 결과 화면 문구 확인 사용
using UnityEngine; // 씬 객체 조회 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 확인 사용
using UnityEngine.EventSystems; // UI 입력 관리자 확인 사용
using UnityEngine.InputSystem.UI; // 신규 입력 모듈 확인 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 버튼 선택 사용

public sealed class ExpeditionCompleteFlowTests // 거점부터 최종 귀환까지 통합 검사
{ // 통합 검사 시작
    [UnityTest] // 실제 플레이 모드 검사 지정
    public IEnumerator Expedition_FinalBossVictoryReturnsRewardsToLobby() // 실제 출발과 보스 승리 후 귀환 확인
    { // 전체 흐름 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 준비 대기
        { // 메뉴 준비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 준비 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 시작 메뉴 존재 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 새 게임 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 준비 대기
        { // 거점 준비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 준비 대기 종료
        LobbyLayoutView lobby = Object.FindFirstObjectByType<LobbyLayoutView>(); // 실제 거점 화면 조회
        Assert.IsNotNull(lobby); // 거점 화면 존재 확인
        lobby.transform.Find("ExpeditionButton").GetComponent<Button>().onClick.Invoke(); // 거점 탐사 버튼 선택
        GameObject map = GameObject.Find("MapSelection"); // 실제 지역 선택 화면 조회
        Assert.IsNotNull(map); // 지역 선택 화면 존재 확인
        map.transform.Find("MapOption_0").GetComponent<Button>().onClick.Invoke(); // 첫 지역 선택
        map.transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 파티 편성 화면 진입
        GameObject formation = GameObject.Find("FormationSetup"); // 실제 편성 화면 조회
        Assert.IsNotNull(formation); // 편성 화면 존재 확인
        Transform grid = formation.transform.Find("CharacterList/Viewport/Grid"); // 실제 캐릭터 목록 조회
        for (int index = 0; index < 4; index++) // 시제품 파티 캐릭터 순회
        { // 실제 캐릭터 편성 시작
            grid.Find("Character_" + index).GetComponent<Button>().onClick.Invoke(); // 캐릭터 선택 버튼 실행
        } // 실제 캐릭터 편성 종료
        Button start = formation.transform.Find("StartButton").GetComponent<Button>(); // 탐사 출발 버튼 조회
        Assert.IsTrue(start.interactable); // 편성 완료 후 출발 가능 확인
        start.onClick.Invoke(); // 실제 탐사 출발
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationEncounterView>() == null;) // 일반 조우 생성 대기
        { // 일반 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 일반 조우 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        ExplorationEncounterView encounter = Object.FindFirstObjectByType<ExplorationEncounterView>(); // 실제 탐사 조우 조회
        Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
        Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length); // 최초 탐사의 입력 관리자 단일 확인
        Assert.IsNotNull(EventSystem.current.GetComponent<InputSystemUIInputModule>()); // 탐사 중 신규 입력 모듈 확인
        Assert.IsNotNull(encounter); // 탐사 조우 존재 확인
        encounter.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>()); // 실제 조우 접촉 처리
        string normalId = ExplorationSessionManager.Instance.ActiveRuntimeEncounterId; // 일반 조우 식별자 보관
        Assert.IsFalse(string.IsNullOrEmpty(normalId)); // 일반 조우 시작 확인
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 일반 전투 준비 대기
        { // 일반 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 일반 전투 대기 종료
        BattleSceneSetup normalBattle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 일반 전투 조회
        Assert.IsNotNull(normalBattle); // 일반 전투 존재 확인
        Assert.AreEqual(4, normalBattle.AllyUnits.Count); // 선택한 네 명 출전 확인
        foreach (BattleUnitRuntime enemy in new List<BattleUnitRuntime>(normalBattle.EnemyUnits)) // 일반 전투 적 순회
        { // 일반 전투 신속 진행 시작
            enemy.TakeDamage(100000, BattleDamageType.None); // 검사용 일반 적 처치
        } // 일반 전투 신속 진행 종료
        yield return null; // 일반 전투 결과 표시 대기
        BattleResultView normalResult = Object.FindFirstObjectByType<BattleResultView>(); // 일반 전투 결과 화면 조회
        Assert.IsNotNull(normalResult); // 일반 전투 결과 화면 확인
        normalResult.GetComponentInChildren<Button>().onClick.Invoke(); // 일반 전투 결과 확인
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && (SceneManager.GetActiveScene().name != "30_Exploration" || Object.FindFirstObjectByType<ExplorationPlayerController>() == null);) // 일반 전투 후 탐사 복귀 대기
        { // 탐사 복귀 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 복귀 대기 종료
        ExplorationSessionManager session = ExplorationSessionManager.Instance; // 실제 탐사 상태 조회
        Assert.IsTrue(session.IsEncounterCleared(normalId)); // 일반 조우 클리어 확인
        Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length); // 전투 후 탐사 입력 관리자 단일 확인
        Assert.IsNotNull(EventSystem.current.GetComponent<InputSystemUIInputModule>()); // 복귀한 탐사의 입력 모듈 확인
        Assert.Greater(PlayerResourceManager.Instance.Gold, 0); // 일반 전투 보상 골드 확인
        int normalGold = PlayerResourceManager.Instance.Gold; // 일반 전투 보상 보관
        while (session.CurrentFloor < 10) // 최종 보스 층 도달 여부 확인
        { // 반복 탐사 단축 시작
            session.AdvanceFloor(); // 실제 층 진행 상태를 다음 층으로 변경
        } // 반복 탐사 단축 종료
        Assert.AreEqual(10, session.CurrentFloor); // 최종 보스 층 확인
        player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 복귀한 탐사 플레이어 조회
        EncounterData bossEncounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 실제 최종 보스 조우 조회
        Assert.IsNotNull(bossEncounter); // 보스 조우 원본 존재 확인
        Assert.IsTrue(session.BeginEncounter("complete-flow-boss", bossEncounter, player.transform.position, player.transform.position)); // 검사용 최종 보스 조우 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && SceneFlowManager.Instance.IsLoadingScene;) // 앞선 씬 이동 완료 대기
        { // 씬 이동 완료 대기 시작
            yield return null; // 다음 프레임 대기
        } // 씬 이동 완료 대기 종료
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 보스 전투 씬 진입
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 보스 전투 준비 대기
        { // 보스 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 보스 전투 대기 종료
        BattleSceneSetup bossBattle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 보스 전투 조회
        Assert.IsNotNull(bossBattle); // 보스 전투 존재 확인
        Assert.AreEqual("ENM_WF_ASHSTORM_CORE", bossBattle.EnemyUnits[0].EnemySource.EnemyId); // 실제 최종 보스 확인
        yield return null; // 실제 층과 보스 배율 반영 대기
        EnemyData bossSource = bossBattle.EnemyUnits[0].EnemySource; // 보스 원본 수치 조회
        Assert.AreEqual(ExplorationDifficultyCalculator.ScaleHealth(bossSource.MaxHealth, 10, BattleType.Boss), bossBattle.EnemyUnits[0].MaxHealth); // 실제 보스 체력의 층과 등급 배율 확인
        Assert.AreEqual(ExplorationDifficultyCalculator.ScaleAttack(bossSource.GetActionPattern(0).ActionAmount, 10, BattleType.Boss), bossBattle.EnemyActionRuntime.FindAction(bossBattle.EnemyUnits[0]).Amount); // 실제 첫 예고 공격의 층과 등급 배율 확인
        CardData basicAttack = Resources.Load<CardData>("StarterCards/Attack"); // 비교용 기본 공격 카드 조회
        Assert.IsNotNull(basicAttack); // 비교 카드 존재 확인
        int sampleDamage = BattleDamageCalculator.Calculate(basicAttack.EffectValue, basicAttack.DamageType, bossSource.PhysicalDefense, bossSource.MagicalResistance).FinalDamage; // 보스에 대한 기본 공격 실피해 계산
        TestContext.WriteLine($"BALANCE_BOSS hp={bossBattle.EnemyUnits[0].MaxHealth} firstAttack={bossBattle.EnemyActionRuntime.FindAction(bossBattle.EnemyUnits[0]).Amount} ap={bossBattle.SharedActionPoints.MaxActionPoints} cardCost={basicAttack.ApCost} damage={sampleDamage}"); // 재현 가능한 비교 수치 기록
        bossBattle.EnemyUnits[0].TakeDamage(100000, BattleDamageType.None); // 검사용 보스 처치
        yield return null; // 보스 전투 결과 표시 대기
        BattleResultView bossResult = Object.FindFirstObjectByType<BattleResultView>(); // 보스 전투 결과 화면 조회
        Assert.IsNotNull(bossResult); // 보스 전투 결과 화면 확인
        Assert.AreEqual(BattleResult.Victory, bossBattle.BattleTurn.Result); // 보스 전투 승리 확인
        List<BattleUnitRuntime> bossAllies = new List<BattleUnitRuntime>(bossBattle.AllyUnits); // 씬 이동 전 아군 결과 보관
        bossResult.GetComponentInChildren<Button>().onClick.Invoke(); // 보스 전투 결과 확인
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && GameObject.Find("ExplorationResult") == null;) // 탐사 성공 화면 생성 대기
        { // 탐사 성공 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 성공 화면 대기 종료
        GameObject result = GameObject.Find("ExplorationResult"); // 실제 탐사 정산 화면 조회
        Assert.IsNotNull(result); // 탐사 정산 화면 존재 확인
        Assert.IsTrue(session.IsExplorationSuccess); // 최종 보스 처치 후 탐사 성공 확인
        Assert.AreEqual(10, session.CompletedFloor); // 정산 완료 층 확인
        Assert.IsTrue(session.IsEncounterCleared("complete-flow-boss")); // 보스 조우 클리어 확인
        Assert.Greater(PlayerResourceManager.Instance.Gold, normalGold); // 보스 보상 추가 확인
        Assert.AreEqual("탐사 성공", result.transform.Find("Title").GetComponent<TMP_Text>().text); // 실제 성공 안내 확인
        int rewardedGold = PlayerResourceManager.Instance.Gold; // 최종 보상 골드 보관
        session.ResolveBattleResult(new BattleResultData(BattleResult.Victory, BattleType.Boss, 1, bossAllies, new string[0])); // 동일 결과 재전달 시도
        Assert.AreEqual(rewardedGold, PlayerResourceManager.Instance.Gold); // 보스 보상 중복 지급 방지 확인
        result.transform.Find("ReturnButton").GetComponent<Button>().onClick.Invoke(); // 실제 거점 복귀 버튼 선택
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && (SceneManager.GetActiveScene().name != "20_Lobby" || Object.FindFirstObjectByType<LobbyLayoutView>() == null);) // 거점 복귀 대기
        { // 거점 복귀 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 복귀 대기 종료
        Assert.AreEqual("20_Lobby", SceneManager.GetActiveScene().name); // 실제 거점 복귀 확인
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None); // 복귀 후 입력 관리자 조회
        foreach (EventSystem eventSystem in eventSystems) // 실제 입력 관리자 순회
        { // 입력 관리자 기록 시작
            TestContext.WriteLine($"EVENT_SYSTEM name={eventSystem.name} scene={eventSystem.gameObject.scene.name} parent={eventSystem.transform.parent?.name}"); // 입력 관리자 출처 기록
        } // 입력 관리자 기록 종료
        Assert.AreEqual(1, eventSystems.Length); // 거점 복귀 후 입력 관리자 단일 유지 확인
        Assert.AreEqual(rewardedGold, PlayerResourceManager.Instance.Gold); // 거점 복귀 후 보상 유지 확인
        Assert.IsTrue(LobbySaveService.CanLoad(LobbySaveService.DefaultPath)); // 탐사 성공 후 거점 저장 유효성 확인
        Assert.IsFalse(File.Exists(ExplorationSaveService.DefaultPath)); // 성공 정산 후 이전 탐사 저장 제거 확인
        PlayerResourceManager.Instance.ResetResources(); // 거점 저장 복원 검증 준비
        Assert.IsTrue(LobbySaveService.Load(LobbySaveService.DefaultPath)); // 실제 거점 저장 복원 확인
        Assert.AreEqual(rewardedGold, PlayerResourceManager.Instance.Gold); // 저장된 최종 보상 복원 확인
        Object.FindFirstObjectByType<LobbyLayoutView>().transform.Find("ExpeditionButton").GetComponent<Button>().onClick.Invoke(); // 귀환 후 탐사 메뉴 다시 열기
        Assert.IsNotNull(GameObject.Find("MapSelection")); // 귀환 후 재출발 화면 접근 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전체 흐름 검사 종료
} // 통합 검사 종료
