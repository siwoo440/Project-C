using System.Collections; // 코루틴 테스트 사용
using System.Collections.Generic; // 전투 적 목록 복사 사용
using System.Linq; // 손패 카드 포함 확인 사용
using NUnit.Framework; // 테스트 검증 기능 사용
using UnityEngine; // 유니티 객체 조회 사용
using UnityEngine.SceneManagement; // 씬 상태 조회 사용
using UnityEngine.TestTools; // 플레이 모드 전환 테스트 사용
using UnityEngine.UI; // 출발 버튼 조회 사용
public sealed class PartyDeparturePlayModeTests // 파티 출발 실제 씬 흐름 테스트
{ // 테스트 클래스 시작
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator SelectedParty_EncounterVictoryReturnsToExploration() // 선택 파티의 조우와 탐사 복귀 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 전환 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메인 메뉴 진입 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(0); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "30_Exploration"; frame++) // 탐사 씬 전환 대기
        { // 탐사 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 대기 종료
        Assert.AreEqual("30_Exploration", SceneManager.GetActiveScene().name); // 탐사 진입 확인
        yield return null; // 탐사 초기화 대기
        ExplorationPartyLoadoutProvider provider = Object.FindFirstObjectByType<ExplorationPartyLoadoutProvider>(); // 탐사 편성 제공자 조회
        Assert.IsNotNull(provider); // 탐사 편성 제공자 존재 확인
        Assert.AreEqual("ARC_00", provider.BattleLoadout.Party.Members[0].CharacterId); // 선택 캐릭터 전달 확인
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 생성 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        ExplorationEncounterView encounter = Object.FindFirstObjectByType<ExplorationEncounterView>(); // 탐사 조우 조회
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 조회
        Assert.IsNotNull(encounter); // 탐사 조우 존재 확인
        Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
        encounter.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>()); // 실제 조우 접촉 처리 실행
        string encounterId = ExplorationSessionManager.Instance.ActiveRuntimeEncounterId; // 현재 조우 식별자 기록
        Assert.IsFalse(string.IsNullOrEmpty(encounterId)); // 조우 시작 상태 확인
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 전환 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsNotNull(battle); // 전투 관리자 존재 확인
        Assert.IsTrue(battle.IsInitialized); // 전투 초기화 완료 확인
        Assert.AreEqual("ARC_00", battle.AllyUnits[0].CharacterSource.CharacterId); // 선택 캐릭터 전투 출전 확인
        Assert.IsNotNull(battle.AllyUnits[0].Portrait); // 선택 캐릭터 전투 초상화 확인
        List<BattleUnitRuntime> enemies = new List<BattleUnitRuntime>(battle.EnemyUnits); // 전투 적 목록 복사
        foreach (BattleUnitRuntime enemy in enemies) // 전투 적 순회
        { // 적 처치 시작
            enemy.TakeDamage(100000, BattleDamageType.Physical); // 테스트용 적 처치
        } // 적 처치 종료
        yield return null; // 전투 결과 화면 생성 대기
        BattleResultView resultView = Object.FindFirstObjectByType<BattleResultView>(); // 전투 결과 화면 조회
        Assert.IsNotNull(resultView); // 전투 결과 화면 존재 확인
        resultView.GetComponentInChildren<Button>().onClick.Invoke(); // 전투 결과 확인
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "30_Exploration"; frame++) // 탐사 복귀 대기
        { // 탐사 복귀 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 복귀 대기 종료
        Assert.AreEqual("30_Exploration", SceneManager.GetActiveScene().name); // 전투 후 탐사 복귀 확인
        yield return null; // 전투 결과 반영 대기
        Assert.IsTrue(ExplorationSessionManager.Instance.IsEncounterCleared(encounterId)); // 조우 클리어 상태 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator FinalBossVictory_UnlocksWorldOnlyAfterFinalClear() // 최종 보스 승리 해금 조건 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string testKey = "ProjectC.Test.FinalClear." + System.Guid.NewGuid().ToString("N"); // 고유 테스트 저장 키 생성
        try // 테스트 정리 보장 시작
        { // 정리 보장 시작
            CharacterUnlockProgress progress = new CharacterUnlockProgress(testKey); // 테스트 전용 해금 상태 생성
            CharacterData world = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[21]; // 세계 캐릭터 조회
            ExplorationSessionManager manager = ExplorationSessionManager.EnsureInstance(); // 탐사 관리자 생성
            typeof(ExplorationSessionManager).GetField("unlockProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(manager, progress); // 테스트 해금 상태 주입
            EncounterData normal = Resources.Load<EncounterData>("Encounters/Encounter_Test_A"); // 일반 조우 조회
            EncounterData boss = Resources.Load<EncounterData>("Encounters/Encounter_Test_C"); // 보스 조우 조회
            BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]); // 생존 아군 생성
            BattleResultData victory = new BattleResultData(BattleResult.Victory, BattleType.Boss, 1, new[] { ally }, new string[0]); // 승리 결과 생성
            BattleResultData escape = new BattleResultData(BattleResult.Escape, BattleType.Boss, 1, new[] { ally }, new string[0]); // 도주 결과 생성
            manager.ResetExploration(); // 탐사 상태 초기화
            typeof(ExplorationSessionManager).GetField("currentFloor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(manager, 10); // 최종 층 설정
            Assert.IsTrue(manager.BeginEncounter("normal", normal, Vector3.zero, Vector3.right)); // 일반 조우 시작
            manager.ResolveBattleResult(victory); // 일반 전투 승리 처리
            Assert.IsFalse(progress.IsUnlocked(world)); // 일반 전투 해금 제외 확인
            manager.ResetExploration(); // 탐사 상태 초기화
            typeof(ExplorationSessionManager).GetField("currentFloor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(manager, 5); // 중간 보스 층 설정
            Assert.IsTrue(manager.BeginEncounter("middle", boss, Vector3.zero, Vector3.right)); // 중간 보스 조우 시작
            manager.ResolveBattleResult(victory); // 중간 보스 승리 처리
            Assert.IsFalse(progress.IsUnlocked(world)); // 중간 보스 해금 제외 확인
            manager.ResetExploration(); // 탐사 상태 초기화
            typeof(ExplorationSessionManager).GetField("currentFloor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(manager, 10); // 최종 층 설정
            Assert.IsTrue(manager.BeginEncounter("escaped", boss, Vector3.zero, Vector3.right)); // 최종 보스 조우 시작
            manager.ResolveBattleResult(escape); // 최종 보스 도주 처리
            Assert.IsFalse(progress.IsUnlocked(world)); // 도주 시 해금 제외 확인
            Assert.IsTrue(manager.BeginEncounter("final", boss, Vector3.zero, Vector3.right)); // 최종 보스 재조우 시작
            manager.ResolveBattleResult(victory); // 최종 보스 승리 처리
            Assert.IsTrue(manager.IsExplorationSuccess); // 최종 탐사 성공 확인
            Assert.IsTrue(progress.IsUnlocked(world)); // 최종 승리 세계 해금 확인
        } // 테스트 본문 종료
        finally // 테스트 데이터 정리 시작
        { // 정리 시작
            PlayerPrefs.DeleteKey(testKey); // 테스트 전용 저장 키 삭제
            PlayerPrefs.Save(); // 테스트 키 삭제 저장
        } // 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator EmperorCard_RedirectsAttackAndRaisesDefense() // 황제 전용 카드 실제 사용 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(0, 4); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        ExplorationEncounterView encounter = Object.FindFirstObjectByType<ExplorationEncounterView>(); // 탐사 조우 조회
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 플레이어 조회
        encounter.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        BattleUnitRuntime emperor = battle.AllyUnits[1]; // 황제 전투 유닛 조회
        BattleUnitRuntime enemy = battle.EnemyUnits[0]; // 첫 적 조회
        Assert.AreSame(battle.AllyUnits[0], battle.EnemyActionRuntime.FindAction(enemy).Target); // 기존 공격 대상 확인
        int emperorCardCount = 0; // 황제 전용 카드 장수 초기화
        foreach (CardInstance card in battle.BattleDeck.Cards) // 시작 덱 카드 순회
        { // 전용 카드 확인 시작
            if (card.SourceData.CardId == "CRD_ARC_04_GUARD") // 황제 전용 카드 확인
            { // 전용 카드 검사 시작
                emperorCardCount++; // 전용 카드 장수 증가
                Assert.AreSame(emperor, card.OwnerUnit); // 황제 카드 소유자 확인
            } // 전용 카드 검사 종료
        } // 전용 카드 확인 종료
        Assert.AreEqual(1, emperorCardCount); // 황제 전용 카드 한 장 확인
        battle.BattleDeck.DrawCards(12); // 남은 시작 덱 드로우
        BattleCardView emperorCard = null; // 황제 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 카드 화면 순회
        { // 전용 카드 검색 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.SourceData.CardId == "CRD_ARC_04_GUARD") // 황제 전용 카드 확인
            { // 전용 카드 발견 시작
                emperorCard = candidate; // 황제 카드 저장
                break; // 카드 검색 종료
            } // 전용 카드 발견 종료
        } // 전용 카드 검색 종료
        Assert.IsNotNull(emperorCard); // 황제 카드 손패 존재 확인
        BattleUnitView enemyView = null; // 적 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 전투 유닛 화면 순회
        { // 대상 화면 검색 시작
            if (candidate.RuntimeUnit == enemy) // 첫 적 화면 확인
            { // 적 화면 발견 시작
                enemyView = candidate; // 적 화면 저장
                break; // 화면 검색 종료
            } // 적 화면 발견 종료
        } // 대상 화면 검색 종료
        Assert.IsNotNull(enemyView); // 적 화면 존재 확인
        int previousDefense = emperor.PhysicalDefense; // 황제 기존 방어력 저장
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        emperorCard.OnPointerClick(null); // 전용 카드 선택
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 아군 화면 순회
        { // 잘못된 대상 확인 시작
            if (candidate.RuntimeUnit == battle.AllyUnits[0]) // 바보 화면 확인
            { // 잘못된 대상 선택 시작
                candidate.OnPointerClick(null); // 아군을 잘못 선택
                break; // 아군 화면 검색 종료
            } // 잘못된 대상 선택 종료
        } // 잘못된 대상 확인 종료
        Assert.AreEqual(previousPoints, battle.SharedActionPoints.CurrentActionPoints); // 잘못된 대상 행동력 유지 확인
        enemyView.OnPointerClick(null); // 공격 중인 적 선택
        for (int frame = 0; frame < 300 && emperor.PhysicalDefense == previousDefense; frame++) // 카드 연출과 효과 대기
        { // 카드 효과 대기 시작
            yield return null; // 다음 프레임 대기
        } // 카드 효과 대기 종료
        Assert.AreSame(emperor, battle.EnemyActionRuntime.FindAction(enemy).Target); // 황제에게 공격 유도 확인
        Assert.AreEqual(previousDefense + 3, emperor.PhysicalDefense); // 임시 방어력 증가 확인
        Assert.AreEqual(previousPoints - 1, battle.SharedActionPoints.CurrentActionPoints); // 행동력 소비 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator PriestessCard_ProtectsAllyTargetedByAttack() // 여사제 전용 보호 카드 실제 사용 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(0, 2); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        BattleUnitRuntime target = battle.AllyUnits[0]; // 공격 예고 대상 조회
        BattleUnitRuntime priestess = battle.AllyUnits[1]; // 여사제 전투 유닛 조회
        Assert.AreSame(target, battle.EnemyActionRuntime.FindAction(battle.EnemyUnits[0]).Target); // 공격 예고 대상 확인
        battle.BattleDeck.DrawCards(12); // 남은 시작 덱 드로우
        BattleCardView priestessCard = null; // 여사제 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 카드 화면 순회
        { // 전용 카드 검색 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.SourceData.CardId == "CRD_ARC_02_WARD") // 여사제 카드 확인
            { // 전용 카드 발견 시작
                priestessCard = candidate; // 여사제 카드 저장
                Assert.AreSame(priestess, candidate.RuntimeCard.OwnerUnit); // 카드 소유자 확인
                break; // 카드 검색 종료
            } // 전용 카드 발견 종료
        } // 전용 카드 검색 종료
        Assert.IsNotNull(priestessCard); // 여사제 카드 손패 존재 확인
        BattleUnitView targetView = null; // 공격 대상 화면 초기화
        BattleUnitView priestessView = null; // 여사제 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 유닛 화면 순회
        { // 아군 화면 검색 시작
            if (candidate.RuntimeUnit == target) // 공격 대상 화면 확인
            { // 대상 화면 저장 시작
                targetView = candidate; // 대상 화면 저장
            } // 대상 화면 저장 종료
            if (candidate.RuntimeUnit == priestess) // 여사제 화면 확인
            { // 여사제 화면 저장 시작
                priestessView = candidate; // 여사제 화면 저장
            } // 여사제 화면 저장 종료
        } // 아군 화면 검색 종료
        Assert.IsNotNull(targetView); // 대상 화면 존재 확인
        Assert.IsNotNull(priestessView); // 여사제 화면 존재 확인
        BattleEnemyAction plannedAttack = battle.EnemyActionRuntime.FindAction(battle.EnemyUnits[0]); // 예정 공격 조회
        int previousDamage = plannedAttack.PreviewDamage().AppliedDamage; // 보호 전 예상 피해 저장
        int previousPhysical = target.PhysicalDefense; // 기존 물리 방어력 저장
        int previousMagical = target.MagicalResistance; // 기존 마법 저항력 저장
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        priestessCard.OnPointerClick(null); // 여사제 카드 선택
        priestessView.OnPointerClick(null); // 공격 예고가 없는 아군 선택
        Assert.AreEqual(previousPoints, battle.SharedActionPoints.CurrentActionPoints); // 잘못된 대상 행동력 유지 확인
        targetView.OnPointerClick(null); // 공격 예고 대상 선택
        for (int frame = 0; frame < 300 && target.PhysicalDefense == previousPhysical; frame++) // 카드 효과 대기
        { // 카드 효과 대기 시작
            yield return null; // 다음 프레임 대기
        } // 카드 효과 대기 종료
        Assert.AreEqual(previousPhysical + 2, target.PhysicalDefense); // 물리 방어 증가 확인
        Assert.AreEqual(previousMagical + 2, target.MagicalResistance); // 마법 저항 증가 확인
        Assert.Less(plannedAttack.PreviewDamage().AppliedDamage, previousDamage); // 예정 공격 피해 감소 확인
        Assert.AreEqual(previousPoints - 1, battle.SharedActionPoints.CurrentActionPoints); // 행동력 소비 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator FoolCard_OffersTwoCardsAndDrawsChosenCard() // 바보 선택 드로우 실제 사용 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(0); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        CardInstance foolCard = null; // 바보 전용 카드 초기화
        foreach (CardInstance card in battle.BattleDeck.Cards) // 전체 시작 카드 순회
        { // 전용 카드 검색 시작
            if (card.SourceData.CardId == "CRD_ARC_00_CHOICE") // 바보 전용 카드 확인
            { // 전용 카드 발견 시작
                foolCard = card; // 바보 카드 저장
                break; // 카드 검색 종료
            } // 전용 카드 발견 종료
        } // 전용 카드 검색 종료
        Assert.IsNotNull(foolCard); // 바보 전용 카드 존재 확인
        while (!battle.BattleDeck.Hand.Contains(foolCard) && battle.BattleDeck.DrawPileCount > 0) // 바보 카드 드로우 대기
        { // 카드 드로우 시작
            battle.BattleDeck.DrawCards(1); // 카드 한 장 드로우
        } // 카드 드로우 종료
        Assert.IsTrue(battle.BattleDeck.Hand.Contains(foolCard)); // 바보 카드 손패 존재 확인
        for (int index = 0; index < 2; index++) // 선택 후보용 카드 두 장 준비
        { // 후보 준비 시작
            foreach (CardInstance card in new List<CardInstance>(battle.BattleDeck.Hand)) // 손패 복사본 순회
            { // 버릴 카드 검색 시작
                if (card != foolCard) // 바보 카드 제외 확인
                { // 후보 확보 시작
                    battle.BattleDeck.DiscardCard(card); // 일반 카드 버리기
                    break; // 다음 후보 준비
                } // 후보 확보 종료
            } // 버릴 카드 검색 종료
        } // 후보 준비 종료
        BattleCardView foolView = null; // 바보 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 화면 순회
        { // 바보 카드 화면 검색 시작
            if (candidate.RuntimeCard == foolCard && candidate.gameObject.activeInHierarchy) // 바보 카드 화면 확인
            { // 화면 발견 시작
                foolView = candidate; // 바보 카드 화면 저장
                break; // 화면 검색 종료
            } // 화면 발견 종료
        } // 바보 카드 화면 검색 종료
        Assert.IsNotNull(foolView); // 바보 카드 화면 존재 확인
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        foolView.OnPointerClick(null); // 바보 카드 사용
        GameObject panel = null; // 선택 화면 초기화
        float choiceDeadline = Time.realtimeSinceStartup + 5f; // 선택 화면 대기 제한 시간 설정
        while (Time.realtimeSinceStartup < choiceDeadline && panel == null) // 선택 화면 생성 대기
        { // 화면 대기 시작
            yield return null; // 다음 프레임 대기
            panel = GameObject.Find("DrawChoicePanel"); // 선택 화면 검색
        } // 화면 대기 종료
        Assert.IsNotNull(panel); // 선택 화면 존재 확인
        Button[] choices = panel.GetComponentsInChildren<Button>(); // 선택 후보 버튼 조회
        Assert.AreEqual(2, choices.Length); // 두 개 선택 후보 확인
        Button endTurn = GameObject.Find("EndTurnButton").GetComponent<Button>(); // 턴 종료 버튼 조회
        Assert.IsFalse(endTurn.interactable); // 선택 중 턴 종료 차단 확인
        CardInstance firstCandidate = battle.BattleDeck.DrawPile[battle.BattleDeck.DrawPileCount - 1]; // 첫 후보 카드 저장
        CardInstance secondCandidate = battle.BattleDeck.DrawPile[battle.BattleDeck.DrawPileCount - 2]; // 두 번째 후보 카드 저장
        choices[1].onClick.Invoke(); // 두 번째 후보 선택
        Assert.IsTrue(battle.BattleDeck.Hand.Contains(secondCandidate)); // 선택 카드 손패 이동 확인
        Assert.IsFalse(endTurn.interactable); // 남은 카드 결정 중 턴 종료 차단 확인
        GameObject remainder = GameObject.Find("RemainingCardChoicePanel"); // 남은 카드 선택 화면 조회
        Assert.IsNotNull(remainder); // 남은 카드 선택 화면 존재 확인
        remainder.transform.Find("DiscardRemaining").GetComponent<Button>().onClick.Invoke(); // 남은 카드 버리기 선택
        Assert.IsTrue(battle.BattleDeck.DiscardPile.Contains(firstCandidate)); // 선택하지 않은 카드 버림 확인
        Assert.IsTrue(endTurn.interactable); // 모든 선택 후 턴 종료 복구 확인
        Assert.AreEqual(previousPoints - 1, battle.SharedActionPoints.CurrentActionPoints); // 행동력 소비 확인
        Assert.IsTrue(battle.BattleDeck.TryRecoverDiscard(foolCard)); // 같은 턴 바보 카드 회수
        BattleCardView secondView = null; // 두 번째 바보 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 갱신된 손패 화면 순회
        { // 바보 카드 화면 검색 시작
            if (candidate.RuntimeCard == foolCard && candidate.gameObject.activeInHierarchy) // 바보 카드 화면 확인
            { // 카드 화면 발견 시작
                secondView = candidate; // 두 번째 카드 화면 저장
                break; // 화면 검색 종료
            } // 카드 화면 발견 종료
        } // 바보 카드 화면 검색 종료
        Assert.IsNotNull(secondView); // 두 번째 바보 카드 화면 존재 확인
        secondView.OnPointerClick(null); // 같은 턴 바보 카드 재사용
        GameObject secondPanel = null; // 두 번째 드로우 선택 화면 초기화
        float secondDeadline = Time.realtimeSinceStartup + 5f; // 두 번째 화면 제한 시간 설정
        while (Time.realtimeSinceStartup < secondDeadline && secondPanel == null) // 두 번째 화면 대기
        { // 두 번째 화면 대기 시작
            yield return null; // 다음 프레임 대기
            secondPanel = GameObject.Find("DrawChoicePanel"); // 두 번째 선택 화면 조회
        } // 두 번째 화면 대기 종료
        Assert.IsNotNull(secondPanel); // 두 번째 선택 화면 존재 확인
        secondPanel.transform.Find("Choice_0").GetComponent<Button>().onClick.Invoke(); // 두 번째 드로우 후보 선택
        Assert.IsNull(GameObject.Find("RemainingCardChoicePanel")); // 같은 턴 패시브 재발동 방지 확인
        Assert.IsTrue(endTurn.interactable); // 두 번째 선택 후 턴 종료 복구 확인
        Assert.AreEqual(previousPoints - 2, battle.SharedActionPoints.CurrentActionPoints); // 두 번의 카드 비용 확인
        Assert.IsTrue(battle.BattleDeck.IsStateValid()); // 전체 카드 보존 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator ChariotCard_MarksEnemyAndStrengthensFollowupAttack() // 전차 표식과 후속 공격 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(7); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        BattleUnitRuntime enemy = battle.EnemyUnits[0]; // 첫 적 조회
        battle.BattleDeck.DrawCards(12); // 남은 시작 덱 드로우
        BattleCardView markView = null; // 표식 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 화면 순회
        { // 표식 카드 검색 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.SourceData.CardId == "CRD_ARC_07_MARK" && candidate.gameObject.activeInHierarchy) // 전차 표식 카드 확인
            { // 표식 카드 발견 시작
                markView = candidate; // 표식 카드 화면 저장
                break; // 카드 검색 종료
            } // 표식 카드 발견 종료
        } // 표식 카드 검색 종료
        Assert.IsNotNull(markView); // 전차 전용 카드 존재 확인
        BattleUnitView enemyView = null; // 적 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 유닛 화면 순회
        { // 적 화면 검색 시작
            if (candidate.RuntimeUnit == enemy) // 첫 적 화면 확인
            { // 적 화면 발견 시작
                enemyView = candidate; // 적 화면 저장
                break; // 화면 검색 종료
            } // 적 화면 발견 종료
        } // 적 화면 검색 종료
        Assert.IsNotNull(enemyView); // 적 화면 존재 확인
        markView.OnPointerClick(null); // 전차 표식 카드 선택
        enemyView.OnPointerClick(null); // 적 대상 선택
        float markDeadline = Time.realtimeSinceStartup + 5f; // 표식 적용 제한 시간 설정
        while (Time.realtimeSinceStartup < markDeadline && enemy.GetStatusEffect(BattleStatusEffectType.ChariotMark) == null) // 표식 적용 대기
        { // 표식 대기 시작
            yield return null; // 다음 프레임 대기
        } // 표식 대기 종료
        Assert.IsNotNull(enemy.GetStatusEffect(BattleStatusEffectType.ChariotMark)); // 전차 표식 적용 확인
        Assert.AreEqual(3, enemy.GetStatusEffect(BattleStatusEffectType.ChariotMark).EffectiveValue); // 임시 표식 추가 피해 확인
        BattleActionSequenceRunner runner = battle.GetComponent<BattleActionSequenceRunner>(); // 전투 연출 실행기 조회
        float sequenceDeadline = Time.realtimeSinceStartup + 5f; // 전투 연출 제한 시간 설정
        while (Time.realtimeSinceStartup < sequenceDeadline && runner.IsBusy) // 카드 연출 종료 대기
        { // 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 연출 대기 종료
        BattleCardView attackView = null; // 후속 공격 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 화면 순회
        { // 후속 카드 검색 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.SourceData.CardId == "CRD_TEMP_ATTACK" && candidate.gameObject.activeInHierarchy) // 공용 공격 카드 확인
            { // 후속 카드 발견 시작
                attackView = candidate; // 후속 카드 화면 저장
                break; // 카드 검색 종료
            } // 후속 카드 발견 종료
        } // 후속 카드 검색 종료
        Assert.IsNotNull(attackView); // 후속 공격 카드 존재 확인
        int ordinaryDamage = Mathf.Max(1, attackView.RuntimeCard.EffectValue - enemy.PhysicalDefense); // 표식 없는 기본 피해 계산
        if (BattleWeaknessCalculator.IsWeakness(attackView.RuntimeCard.CardType, enemy.EnemySource)) // 카드 약점 여부 확인
        { // 약점 피해 계산 시작
            ordinaryDamage = Mathf.RoundToInt(ordinaryDamage * BattleWeaknessCalculator.WeaknessDamageMultiplier); // 약점 피해 배율 적용
        } // 약점 피해 계산 종료
        int previousHealth = enemy.CurrentHealth; // 후속 공격 전 적 체력 저장
        attackView.OnPointerClick(null); // 후속 공격 카드 선택
        enemyView.OnPointerClick(null); // 동일 적 대상 선택
        float attackDeadline = Time.realtimeSinceStartup + 5f; // 후속 공격 제한 시간 설정
        while (Time.realtimeSinceStartup < attackDeadline && enemy.CurrentHealth == previousHealth) // 후속 공격 피해 대기
        { // 피해 대기 시작
            yield return null; // 다음 프레임 대기
        } // 피해 대기 종료
        Assert.Greater(previousHealth - enemy.CurrentHealth, ordinaryDamage); // 표식 대상 추가 피해 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator EmpressCard_ProtectsAllLivingAllies() // 여황제 전체 아군 방어 카드 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(0, 3); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        battle.BattleDeck.DrawCards(12); // 남은 시작 덱 드로우
        BattleCardView empressCard = null; // 여황제 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 카드 화면 순회
        { // 전용 카드 검색 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.SourceData.CardId == "CRD_ARC_03_FEAST" && candidate.gameObject.activeInHierarchy) // 여황제 카드 확인
            { // 전용 카드 발견 시작
                empressCard = candidate; // 여황제 카드 저장
                Assert.AreSame(battle.AllyUnits[1], candidate.RuntimeCard.OwnerUnit); // 카드 소유자 확인
                break; // 카드 검색 종료
            } // 전용 카드 발견 종료
        } // 전용 카드 검색 종료
        Assert.IsNotNull(empressCard); // 여황제 카드 손패 존재 확인
        int firstDefense = battle.AllyUnits[0].PhysicalDefense; // 첫 아군 기존 방어력 저장
        int secondDefense = battle.AllyUnits[1].PhysicalDefense; // 여황제 기존 방어력 저장
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        empressCard.OnPointerClick(null); // 여황제 전체 방어 카드 사용
        float effectDeadline = Time.realtimeSinceStartup + 5f; // 카드 효과 제한 시간 설정
        while (Time.realtimeSinceStartup < effectDeadline && battle.AllyUnits[0].PhysicalDefense == firstDefense) // 방어 효과 적용 대기
        { // 효과 대기 시작
            yield return null; // 다음 프레임 대기
        } // 효과 대기 종료
        Assert.AreEqual(firstDefense + 3, battle.AllyUnits[0].PhysicalDefense); // 첫 아군 방어 증가 확인
        Assert.AreEqual(secondDefense + 3, battle.AllyUnits[1].PhysicalDefense); // 여황제 방어 증가 확인
        Assert.AreEqual(previousPoints - 2, battle.SharedActionPoints.CurrentActionPoints); // 행동력 소비 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator MagicianCard_RecoversChosenDiscardedCard() // 마법사 버린 카드 선택 회수 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(1); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        battle.BattleDeck.DrawCards(12); // 남은 시작 덱 드로우
        CardInstance magicianCard = null; // 마법사 전용 카드 초기화
        foreach (CardInstance card in battle.BattleDeck.Hand) // 손패 카드 순회
        { // 전용 카드 검색 시작
            if (card.SourceData.CardId == "CRD_ARC_01_REPAIR") // 마법사 카드 확인
            { // 전용 카드 발견 시작
                magicianCard = card; // 마법사 카드 저장
                break; // 카드 검색 종료
            } // 전용 카드 발견 종료
        } // 전용 카드 검색 종료
        Assert.IsNotNull(magicianCard); // 마법사 전용 카드 존재 확인
        BattleCardView initialView = null; // 최초 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 화면 순회
        { // 최초 카드 화면 검색 시작
            if (candidate.RuntimeCard == magicianCard && candidate.gameObject.activeInHierarchy) // 마법사 카드 화면 확인
            { // 카드 화면 발견 시작
                initialView = candidate; // 최초 카드 화면 저장
                break; // 화면 검색 종료
            } // 카드 화면 발견 종료
        } // 최초 카드 화면 검색 종료
        Assert.IsNotNull(initialView); // 최초 카드 화면 존재 확인
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        initialView.OnPointerClick(null); // 빈 버린 더미에서 카드 사용 시도
        Assert.AreEqual(previousPoints, battle.SharedActionPoints.CurrentActionPoints); // 빈 버린 더미 행동력 유지 확인
        List<CardInstance> discarded = new List<CardInstance>(); // 회수 후보 목록 생성
        foreach (CardInstance card in new List<CardInstance>(battle.BattleDeck.Hand)) // 손패 복사본 순회
        { // 회수 후보 준비 시작
            if (card == magicianCard) // 전용 카드 여부 확인
            { // 전용 카드 제외 시작
                continue; // 다음 카드 확인
            } // 전용 카드 제외 종료
            discarded.Add(card); // 회수 후보 기록
            battle.BattleDeck.DiscardCard(card); // 회수 후보 버리기
            if (discarded.Count == 3) // 후보 세 장 준비 확인
            { // 후보 준비 완료 시작
                break; // 후보 준비 종료
            } // 후보 준비 완료 종료
        } // 회수 후보 준비 종료
        Assert.AreEqual(3, discarded.Count); // 버린 카드 세 장 준비 확인
        BattleCardView magicianView = null; // 갱신된 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 갱신된 손패 화면 순회
        { // 마법사 화면 검색 시작
            if (candidate.RuntimeCard == magicianCard && candidate.gameObject.activeInHierarchy) // 마법사 카드 화면 확인
            { // 카드 화면 발견 시작
                magicianView = candidate; // 갱신된 카드 화면 저장
                break; // 화면 검색 종료
            } // 카드 화면 발견 종료
        } // 마법사 화면 검색 종료
        Assert.IsNotNull(magicianView); // 마법사 카드 화면 존재 확인
        magicianView.OnPointerClick(null); // 마법사 회수 카드 사용
        GameObject panel = null; // 선택 화면 초기화
        float choiceDeadline = Time.realtimeSinceStartup + 5f; // 선택 화면 제한 시간 설정
        while (Time.realtimeSinceStartup < choiceDeadline && panel == null) // 선택 화면 생성 대기
        { // 화면 대기 시작
            yield return null; // 다음 프레임 대기
            panel = GameObject.Find("DrawChoicePanel"); // 선택 화면 검색
        } // 화면 대기 종료
        Assert.IsNotNull(panel); // 회수 선택 화면 존재 확인
        Button next = panel.transform.Find("NextPage").GetComponent<Button>(); // 다음 페이지 버튼 조회
        next.onClick.Invoke(); // 세 번째 후보 페이지 이동
        panel.transform.Find("Choice_2").GetComponent<Button>().onClick.Invoke(); // 세 번째 버린 카드 선택
        Assert.IsTrue(battle.BattleDeck.Hand.Contains(discarded[2])); // 선택 카드 손패 회수 확인
        Assert.AreEqual(0, discarded[2].ApCost); // 마법사 패시브 회수 카드 할인 확인
        BattleCardView recoveredView = null; // 회수 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 갱신된 손패 화면 순회
        { // 회수 카드 화면 검색 시작
            if (candidate.RuntimeCard == discarded[2] && candidate.gameObject.activeInHierarchy) // 회수된 카드 화면 확인
            { // 회수 화면 발견 시작
                recoveredView = candidate; // 회수 카드 화면 저장
                break; // 화면 검색 종료
            } // 회수 화면 발견 종료
        } // 회수 카드 화면 검색 종료
        Assert.IsNotNull(recoveredView); // 회수 카드 화면 존재 확인
        Assert.AreEqual("0", recoveredView.transform.Find("CostBadge/CostText").GetComponent<TMPro.TMP_Text>().text); // 손패의 할인 비용 표시 확인
        Assert.IsTrue(battle.BattleDeck.DiscardPile.Contains(discarded[0])); // 선택하지 않은 카드 유지 확인
        Assert.IsTrue(battle.BattleDeck.DiscardPile.Contains(magicianCard)); // 사용한 마법사 카드 버림 확인
        Assert.AreEqual(previousPoints - 1, battle.SharedActionPoints.CurrentActionPoints); // 회수 카드 행동력 소비 확인
        BattleUnitView targetView = null; // 적 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 유닛 화면 순회
        { // 적 화면 검색 시작
            if (candidate.RuntimeUnit == battle.EnemyUnits[0]) // 첫 적 화면 확인
            { // 적 화면 발견 시작
                targetView = candidate; // 적 화면 저장
                break; // 화면 검색 종료
            } // 적 화면 발견 종료
        } // 적 화면 검색 종료
        Assert.IsNotNull(targetView); // 적 화면 존재 확인
        recoveredView.OnPointerClick(null); // 할인된 회수 카드 선택
        targetView.OnPointerClick(null); // 회수 카드의 적 대상 선택
        float useDeadline = Time.realtimeSinceStartup + 5f; // 카드 사용 제한 시간 설정
        while (Time.realtimeSinceStartup < useDeadline && !battle.BattleDeck.DiscardPile.Contains(discarded[2])) // 회수 카드 사용 대기
        { // 카드 사용 대기 시작
            yield return null; // 다음 프레임 대기
        } // 카드 사용 대기 종료
        Assert.IsTrue(battle.BattleDeck.DiscardPile.Contains(discarded[2])); // 할인된 카드 사용 확인
        Assert.AreEqual(previousPoints - 1, battle.SharedActionPoints.CurrentActionPoints); // 할인 카드 추가 행동력 미소비 확인
        Assert.IsTrue(battle.BattleDeck.IsStateValid()); // 전체 카드 보존 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator HierophantCard_PreparesAllyForFirstEnemyAttack() // 교황 사전 방어 카드 실제 사용 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(0, 5); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        BattleEnemyAction firstAttack = battle.EnemyActionRuntime.PlannedActions[0]; // 첫 예정 적 공격 조회
        BattleUnitRuntime target = firstAttack.Target; // 공격 예고 대상 조회
        int previousDamage = firstAttack.PreviewDamage().AppliedDamage; // 준비 전 예상 피해 저장
        battle.BattleDeck.DrawCards(12); // 남은 시작 덱 드로우
        BattleCardView cardView = null; // 교황 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 화면 순회
        { // 전용 카드 검색 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.SourceData.CardId == "CRD_ARC_05_PREPARE" && candidate.gameObject.activeInHierarchy) // 교황 카드 확인
            { // 전용 카드 발견 시작
                cardView = candidate; // 교황 카드 화면 저장
                Assert.AreSame(battle.AllyUnits[1], candidate.RuntimeCard.OwnerUnit); // 교황 카드 소유자 확인
                break; // 카드 검색 종료
            } // 전용 카드 발견 종료
        } // 전용 카드 검색 종료
        Assert.IsNotNull(cardView); // 교황 전용 카드 존재 확인
        BattleUnitView targetView = null; // 보호 대상 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 유닛 화면 순회
        { // 아군 화면 검색 시작
            if (candidate.RuntimeUnit == target) // 공격 대상 화면 확인
            { // 대상 화면 발견 시작
                targetView = candidate; // 대상 화면 저장
                break; // 화면 검색 종료
            } // 대상 화면 발견 종료
        } // 아군 화면 검색 종료
        Assert.IsNotNull(targetView); // 보호 대상 화면 존재 확인
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        cardView.OnPointerClick(null); // 교황 카드 선택
        targetView.OnPointerClick(null); // 방어 준비 대상 선택
        float effectDeadline = Time.realtimeSinceStartup + 5f; // 상태 적용 제한 시간 설정
        while (Time.realtimeSinceStartup < effectDeadline && target.GetStatusEffect(BattleStatusEffectType.GuardPreparation) == null) // 방어 준비 대기
        { // 준비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 준비 대기 종료
        Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.GuardPreparation)); // 방어 준비 적용 확인
        int preparedDamage = firstAttack.PreviewDamage().AppliedDamage; // 준비 적용 예상 피해 조회
        Assert.Less(preparedDamage, previousDamage); // 적 공격 예상 피해 감소 확인
        Assert.AreEqual(previousPoints - 1, battle.SharedActionPoints.CurrentActionPoints); // 카드 행동력 소비 확인
        BattleEnemyActionResult result = battle.EnemyActionRuntime.ExecuteAction(firstAttack); // 예정 적 공격 실행
        Assert.AreEqual(preparedDamage, result.DamageResult.AppliedDamage); // 예고와 실제 피해 일치 확인
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.GuardPreparation)); // 첫 공격 후 준비 소모 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator StarCard_ReservesNextTurnHealing() // 별 캐릭터 예약 회복 카드 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(0, 17); // 테스트 파티 준비 후 탐사 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<ExplorationEncounterView>() == null; frame++) // 탐사 조우 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 조우 접촉 처리
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 전투 준비 확인
        BattleUnitRuntime target = battle.AllyUnits[0]; // 회복 대상 아군 조회
        target.TakeDamage(20, BattleDamageType.None); // 테스트용 아군 체력 감소
        int healthBefore = target.CurrentHealth; // 회복 전 체력 저장
        target.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 2, 2, 1); // 기존 재생 효과 적용
        battle.BattleDeck.DrawCards(12); // 남은 시작 덱 드로우
        BattleCardView cardView = null; // 별 카드 화면 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 손패 화면 순회
        { // 전용 카드 검색 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.SourceData.CardId == "CRD_ARC_17_HEAL" && candidate.gameObject.activeInHierarchy) // 별 카드 확인
            { // 전용 카드 발견 시작
                cardView = candidate; // 별 카드 화면 저장
                Assert.AreSame(battle.AllyUnits[1], candidate.RuntimeCard.OwnerUnit); // 별 카드 소유자 확인
                break; // 카드 검색 종료
            } // 전용 카드 발견 종료
        } // 전용 카드 검색 종료
        Assert.IsNotNull(cardView); // 별 전용 카드 존재 확인
        BattleUnitView targetView = null; // 회복 대상 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 유닛 화면 순회
        { // 아군 화면 검색 시작
            if (candidate.RuntimeUnit == target) // 회복 대상 화면 확인
            { // 대상 화면 발견 시작
                targetView = candidate; // 대상 화면 저장
                break; // 화면 검색 종료
            } // 대상 화면 발견 종료
        } // 아군 화면 검색 종료
        Assert.IsNotNull(targetView); // 회복 대상 화면 존재 확인
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        cardView.OnPointerClick(null); // 별 카드 선택
        targetView.OnPointerClick(null); // 회복 예약 대상 선택
        float deadline = Time.realtimeSinceStartup + 5f; // 예약 적용 제한 시간 설정
        while (Time.realtimeSinceStartup < deadline && target.GetStatusEffect(BattleStatusEffectType.StarReservedHealing) == null) // 회복 예약 적용 대기
        { // 예약 대기 시작
            yield return null; // 다음 프레임 대기
        } // 예약 대기 종료
        Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.StarReservedHealing)); // 회복 예약 상태 확인
        Assert.AreEqual(healthBefore, target.CurrentHealth); // 카드 사용 시 즉시 회복 없음 확인
        Assert.AreEqual(previousPoints - 1, battle.SharedActionPoints.CurrentActionPoints); // 카드 행동력 소비 확인
        new BattleStatusEffectProcessor().ProcessPhaseStart(target, 2); // 다음 아군 턴 시작 상태 처리
        Assert.AreEqual(healthBefore + 8, target.CurrentHealth); // 기존 재생과 예약 회복 합계 확인
        Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.Regeneration)); // 기존 재생 효과 독립 유지 확인
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.StarReservedHealing)); // 한 번 발동 후 예약 소멸 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
} // 테스트 클래스 종료
