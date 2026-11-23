using System.Collections; // 실제 전투 대기 사용
using NUnit.Framework; // 실제 전투 안내 확인 사용
using TMPro; // 안내 제목 확인 사용
using UnityEngine; // 실제 게임 객체 조회 사용
using UnityEngine.SceneManagement; // 게임 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 안내 버튼 사용
public sealed class BattleTutorialSceneTests // 실제 전투 튜토리얼 연결 검사
{ // 실제 전투 안내 검사 시작
    [UnityTest] // 실제 전투 장면 안내 검사 지정
    public IEnumerator RealBattle_CardAndEnemyTurnAdvanceTutorial() // 실제 카드와 턴 이벤트 연결 확인
    { // 전투 안내 연결 검사 시작
        yield return new EnterPlayMode(); // 실제 게임 플레이 시작
        SceneManager.LoadScene("00_Boot"); // 게임 시작 화면 열기
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 실제 메뉴 준비 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 열기
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 준비 대기
        { // 거점 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 거점 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 준비 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 네 명 파티로 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 대기
        { // 탐사 플레이어 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 탐사 플레이어 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && SceneFlowManager.Instance.IsLoadingScene;) // 탐사 씬 이동 완료 대기
        { // 씬 이동 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 씬 이동 대기 종료
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 충분한 체력의 실제 보스 조우 조회
        Assert.IsNotNull(encounter); // 실제 보스 조우 확인
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("tutorial_integration_test", encounter, player.transform.position, player.transform.position)); // 실제 전투 조우 준비
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 전투 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 전투 초기화 대기
        { // 전투 초기화 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 전투 초기화 대기 종료
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
        BattleTutorialView tutorial = Object.FindFirstObjectByType<BattleTutorialView>(); // 실제 전투 안내 화면 조회
        Assert.IsNotNull(battle); // 전투 관리자 존재 확인
        Assert.IsNotNull(tutorial); // 전투 안내 생성 확인
        Transform guide = tutorial.transform.Find("Guide"); // 실제 안내 화면 조회
        if (!guide.gameObject.activeSelf) // 자동 안내 미표시 확인
        { // 명시적 다시 보기 시작
            tutorial.transform.Find("ReplayButton").GetComponent<Button>().onClick.Invoke(); // 실제 다시 보기 버튼 클릭
        } // 명시적 다시 보기 종료
        TMP_Text title = guide.Find("Panel/Title").GetComponent<TMP_Text>(); // 안내 제목 조회
        Button next = guide.Find("Panel/NextButton").GetComponent<Button>(); // 안내 다음 버튼 조회
        next.onClick.Invoke(); // 카드 사용 안내 이동
        Assert.AreEqual("카드 사용", title.text); // 실제 카드 사용 안내 확인
        BattleCardView card = null; // 사용할 실제 카드 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 실제 손패 카드 순회
        { // 공격 카드 확인 시작
            if (candidate.RuntimeCard != null && candidate.RuntimeCard.TargetType == CardTargetType.SingleEnemy && candidate.RuntimeCard.ApCost <= battle.SharedActionPoints.CurrentActionPoints) // 사용 가능한 단일 적 공격 확인
            { // 공격 카드 선택 시작
                card = candidate; // 실제 공격 카드 보관
                break; // 공격 카드 검색 종료
            } // 공격 카드 선택 종료
        } // 공격 카드 확인 종료
        Assert.IsNotNull(card); // 실제 손패 공격 카드 확인
        BattleUnitView enemy = null; // 실제 적 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 실제 유닛 화면 순회
        { // 적 화면 확인 시작
            if (candidate.RuntimeUnit == battle.EnemyUnits[0]) // 보스 유닛 일치 확인
            { // 적 화면 보관 시작
                enemy = candidate; // 실제 보스 화면 보관
                break; // 적 화면 검색 종료
            } // 적 화면 보관 종료
        } // 적 화면 확인 종료
        Assert.IsNotNull(enemy); // 실제 적 화면 확인
        card.OnPointerClick(null); // 실제 손패 카드 클릭
        enemy.OnPointerClick(null); // 실제 적 대상 클릭
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && title.text != "행동력 AP";) // 실제 카드 사용 안내 이동 대기
        { // 카드 사용 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 카드 사용 대기 종료
        Assert.AreEqual("행동력 AP", title.text); // 실제 카드 사용 이벤트 안내 반영 확인
        next.onClick.Invoke(); // 정신력 안내 이동
        next.onClick.Invoke(); // 적 행동 예고 안내 이동
        next.onClick.Invoke(); // 턴 종료 안내 이동
        Assert.AreEqual("턴 종료", title.text); // 실제 턴 종료 안내 확인
        Button endTurn = Object.FindFirstObjectByType<BattleHandView>().transform.Find("EndTurnButton").GetComponent<Button>(); // 실제 턴 종료 버튼 조회
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && !endTurn.interactable;) // 카드 연출 완료 대기
        { // 카드 연출 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 카드 연출 대기 종료
        Assert.IsTrue(endTurn.interactable); // 실제 턴 종료 가능 확인
        endTurn.onClick.Invoke(); // 실제 턴 종료 버튼 클릭
        Assert.AreEqual("적 행동 관찰", title.text); // 실제 적 턴 시작 안내 반영 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && !next.interactable;) // 실제 적 행동 종료 대기
        { // 적 행동 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 적 행동 대기 종료
        Assert.IsTrue(next.interactable); // 다음 아군 턴에서 안내 완료 가능 확인
        yield return new ExitPlayMode(); // 실제 게임 플레이 종료
    } // 전투 안내 연결 검사 종료
} // 실제 전투 안내 검사 종료
