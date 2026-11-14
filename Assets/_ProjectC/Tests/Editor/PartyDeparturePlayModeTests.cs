using System.Collections; // 코루틴 테스트 사용
using System.Collections.Generic; // 전투 적 목록 복사 사용
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
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<PartyRosterView>() == null; frame++) // 편성 화면 생성 대기
        { // 편성 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 편성 화면 대기 종료
        PartyRosterView view = Object.FindFirstObjectByType<PartyRosterView>(); // 거점 편성 화면 조회
        Assert.IsNotNull(view); // 편성 화면 생성 확인
        Button[] buttons = view.GetComponentsInChildren<Button>(); // 캐릭터와 출발 버튼 조회
        buttons[0].onClick.Invoke(); // 첫 캐릭터 선택
        Assert.IsTrue(buttons[22].interactable); // 유효한 파티 출발 가능 확인
        buttons[22].onClick.Invoke(); // 탐사 출발 요청
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
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<PartyRosterView>() == null; frame++) // 편성 화면 대기
        { // 편성 대기 시작
            yield return null; // 다음 프레임 대기
        } // 편성 대기 종료
        PartyRosterView rosterView = Object.FindFirstObjectByType<PartyRosterView>(); // 편성 화면 조회
        Button[] buttons = rosterView.GetComponentsInChildren<Button>(); // 선택 버튼 목록 조회
        buttons[0].onClick.Invoke(); // 바보 캐릭터 선택
        buttons[4].onClick.Invoke(); // 황제 캐릭터 선택
        buttons[22].onClick.Invoke(); // 탐사 출발
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
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<PartyRosterView>() == null; frame++) // 편성 화면 대기
        { // 편성 대기 시작
            yield return null; // 다음 프레임 대기
        } // 편성 대기 종료
        Button[] buttons = Object.FindFirstObjectByType<PartyRosterView>().GetComponentsInChildren<Button>(); // 선택 버튼 목록 조회
        buttons[0].onClick.Invoke(); // 바보 캐릭터 선택
        buttons[2].onClick.Invoke(); // 여사제 캐릭터 선택
        buttons[22].onClick.Invoke(); // 탐사 출발
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
} // 테스트 클래스 종료
