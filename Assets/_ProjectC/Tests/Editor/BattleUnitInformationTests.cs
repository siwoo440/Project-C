using System.Collections; // 실제 전투 화면 대기 사용
using NUnit.Framework; // 전투 정보 표시 검증 사용
using TMPro; // 실제 전투 정보 글자 검사 사용
using UnityEngine; // 실제 전투 객체 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 정보 창 닫기 버튼 검사 사용
public sealed class BattleUnitInformationTests // 아군과 적 선택 정보 검사
{ // 유닛 정보 검사 시작
    [UnityTest] // 실제 전투 정보 화면 검사 지정
    public IEnumerator SelectionShowsLiveInfoWithoutInterruptingCardTargeting() // 진영별 정보 패널과 카드 대상 선택 확인
    { // 실제 전투 정보 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 플레이 시작
        SceneManager.LoadScene("00_Boot"); // 실제 게임 시작 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 로딩 대기
        { // 시작 메뉴 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 시작 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 실제 시작 메뉴 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 실제 거점 로딩 대기
        { // 거점 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 거점 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 네 명 파티 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 실제 탐사 로딩 대기
        { // 탐사 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 탐사 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        Assert.IsNotNull(player); // 실제 탐사 플레이어 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && SceneFlowManager.Instance.IsLoadingScene;) // 씬 전환 완료 대기
        { // 씬 전환 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 씬 전환 대기 종료
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 실제 보스 조우 조회
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("unit_information_test", encounter, player.transform.position, player.transform.position)); // 실제 보스 조우 시작
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 전투 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 실제 전투 초기화 대기
        { // 전투 초기화 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 전투 초기화 대기 종료
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
        Assert.IsNotNull(battle); // 실제 전투 관리자 확인
        Assert.IsTrue(battle.IsInitialized); // 실제 전투 초기화 확인
        BattleUnitInfoPanel panel = Object.FindFirstObjectByType<BattleUnitInfoPanel>(); // 실제 유닛 상세 패널 조회
        Assert.IsNotNull(panel); // 실제 유닛 상세 패널 확인
        BattleUnitView ally = FindView(battle.AllyUnits[0]); // 실제 첫 아군 화면 조회
        BattleUnitView enemy = FindView(battle.EnemyUnits[0]); // 실제 보스 적 화면 조회
        Assert.IsNotNull(ally); // 실제 아군 화면 확인
        Assert.IsNotNull(enemy); // 실제 적 화면 확인
        ally.OnPointerClick(null); // 실제 아군 클릭 처리
        Assert.AreSame(ally.RuntimeUnit, panel.SelectedUnit); // 아군 실제 선택 확인
        Assert.AreEqual(1f, panel.PanelRect.anchorMin.x); // 아군 선택 시 화면 오른쪽 배치 확인
        TMP_Text name = panel.transform.Find("Frame/Name").GetComponent<TMP_Text>(); // 실제 정보 패널 이름 조회
        TMP_Text stats = panel.transform.Find("Frame/Stats").GetComponent<TMP_Text>(); // 실제 정보 패널 능력치 조회
        Assert.AreEqual(ally.RuntimeUnit.DisplayName, name.text); // 선택한 아군 이름 일치 확인
        ally.RuntimeUnit.TakeDamage(10, BattleDamageType.None); // 실제 선택 아군 체력 감소
        Assert.IsTrue(stats.text.Contains(ally.RuntimeUnit.CurrentHealth.ToString())); // 실제 체력 변화 자동 반영 확인
        enemy.OnPointerClick(null); // 실제 적 클릭 처리
        Assert.AreSame(enemy.RuntimeUnit, panel.SelectedUnit); // 실제 적 선택 확인
        Assert.AreEqual(0f, panel.PanelRect.anchorMin.x); // 적 선택 시 화면 왼쪽 배치 확인
        Assert.AreEqual(enemy.RuntimeUnit.DisplayName, name.text); // 선택한 적 이름 일치 확인
        EncounterData summon = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 소환 적 원본 조회
        Assert.IsTrue(battle.TrySummonEnemy(summon.Enemies[0])); // 실제 적 소환 확인
        BattleUnitView summoned = FindView(battle.EnemyUnits[battle.EnemyUnits.Count - 1]); // 실제 소환 적 화면 조회
        Assert.IsNotNull(summoned); // 소환 적 화면 확인
        summoned.OnPointerClick(null); // 실제 소환 적 클릭 처리
        Assert.AreSame(summoned.RuntimeUnit, panel.SelectedUnit); // 소환 적 정보 연결 확인
        panel.transform.Find("Frame/Close").GetComponent<Button>().onClick.Invoke(); // 실제 정보 창 닫기 버튼 클릭
        Assert.IsNull(panel.SelectedUnit); // 정보 창 닫힘 확인
        BattleHandView hand = Object.FindFirstObjectByType<BattleHandView>(); // 실제 손패 화면 조회
        BattleCardView attack = null; // 실제 적 대상 공격 카드 초기화
        foreach (BattleCardView card in hand.GetComponentsInChildren<BattleCardView>()) // 현재 실제 손패 카드 순회
        { // 실제 적 대상 카드 검색 시작
            if (card.RuntimeCard != null && card.RuntimeCard.TargetType == CardTargetType.SingleEnemy && card.GetComponent<CanvasGroup>().alpha > 0.9f) // 사용 가능한 단일 적 대상 카드 확인
            { // 실제 공격 카드 저장 시작
                attack = card; // 실제 공격 카드 기록
                break; // 공격 카드 검색 종료
            } // 실제 공격 카드 저장 종료
        } // 실제 적 대상 카드 검색 종료
        Assert.IsNotNull(attack); // 실제 적 대상 공격 카드 확인
        attack.OnPointerClick(null); // 실제 공격 카드 선택
        enemy.OnPointerClick(null); // 실제 적 대상 선택
        Assert.IsNull(panel.SelectedUnit); // 카드 대상 선택 시 정보 창 열림 방지 확인
        yield return new ExitPlayMode(); // 실제 전투 플레이 종료
    } // 실제 전투 정보 검사 종료
    private static BattleUnitView FindView(BattleUnitRuntime unit) // 런타임 유닛의 실제 화면 조회
    { // 유닛 화면 조회 시작
        foreach (BattleUnitView view in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 현재 전투 유닛 화면 순회
        { // 유닛 화면 비교 시작
            if (view.RuntimeUnit == unit) // 실제 런타임 유닛 일치 확인
            { // 유닛 화면 반환 시작
                return view; // 일치하는 실제 유닛 화면 반환
            } // 유닛 화면 반환 종료
        } // 유닛 화면 비교 종료
        return null; // 유닛 화면 없음 반환
    } // 유닛 화면 조회 종료
} // 유닛 정보 검사 종료
