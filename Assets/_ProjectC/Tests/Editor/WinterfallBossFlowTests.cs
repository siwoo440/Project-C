using System.Collections; // 플레이 모드 코루틴 사용
using System.Reflection; // 실제 카드 충돌 처리 호출 사용
using NUnit.Framework; // 실제 보스 전투 검증 사용
using UnityEngine; // 게임 씬 객체 사용
using UnityEngine.SceneManagement; // 실제 게임 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
public sealed class WinterfallBossFlowTests // 실제 윈터폴 보스 전투 검사
{ // 보스 전투 검사 시작
    [UnityTest] // 실제 탐사와 보스 전투 검사 지정
    public IEnumerator BossBattle_ScalesTransitionsAndStoresVictory() // 보스 배율과 페이즈 및 승리 결과 확인
    { // 실제 보스 전투 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 준비 대기
        { // 시작 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 시작 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 시작 메뉴 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 준비 대기
        { // 거점 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 네 명 파티로 실제 탐사 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 준비 대기
        { // 탐사 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        Assert.IsNotNull(player); // 플레이어 존재 확인
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && SceneFlowManager.Instance.IsLoadingScene;) // 이전 씬 이동 완료 대기
        { // 씬 이동 대기 시작
            yield return null; // 다음 프레임 대기
        } // 씬 이동 대기 종료
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 실제 보스 조우 조회
        Assert.IsNotNull(encounter); // 보스 조우 존재 확인
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("boss_flow_test", encounter, player.transform.position, player.transform.position)); // 독립 테스트 조우 시작
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 보스 전투 씬 진입
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 전투 준비 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
        Assert.IsNotNull(battle); // 실제 전투 관리자 확인
        BattleUnitRuntime boss = battle.EnemyUnits[0]; // 실제 보스 유닛 조회
        Assert.AreEqual("ENM_WF_ASHSTORM_CORE", boss.EnemySource.EnemyId); // 실제 보스 식별자 확인
        Assert.AreEqual(438, boss.MaxHealth); // 기존 난이도 체력 875의 절반 올림 확인
        Assert.AreEqual(438, boss.CurrentHealth); // 첫 프레임부터 새 최대 체력으로 시작 확인
        Assert.AreEqual(51, boss.CurrentMental); // 기존 난이도에서 얻던 시작 정신력 유지 확인
        Assert.IsFalse(battle.BattleTurn.HasPendingBossPhaseTransition); // 조우 시작 시 페이즈 전환 예약 없음 확인
        Assert.IsFalse(battle.BattleTurn.CanEscape); // 실제 보스전 도주 불가 확인
        Assert.AreEqual(24, battle.EnemyActionRuntime.FindAction(boss).Amount); // 사용자 확인 보스 공격 배율 확인
        battle.BattleDeck.DrawCards(20); // 손패에 실제 공격 카드 확보
        CardInstance attack = null; // 공격 카드 후보 초기화
        foreach (CardInstance candidate in battle.BattleDeck.Hand) // 현재 손패 순회
        { // 공격 카드 확인 시작
            if (candidate.EffectType == CardEffectType.Damage && candidate.TargetType == CardTargetType.SingleEnemy) // 단일 적 공격 카드 확인
            { // 공격 카드 선택 시작
                attack = candidate; // 실제 공격 카드 보관
                break; // 카드 검색 종료
            } // 공격 카드 선택 종료
        } // 공격 카드 확인 종료
        Assert.IsNotNull(attack); // 사용할 공격 카드 존재 확인
        boss.TakeDamage(boss.CurrentHealth - (boss.MaxHealth / 2 + 1), BattleDamageType.None, battle.AllyUnits[0]); // 전환 경계 바로 위까지 보스 체력 감소
        Assert.IsFalse(battle.BattleTurn.HasPendingBossPhaseTransition); // 경계 이전 전환 방지 확인
        BattleCardActionController controller = (BattleCardActionController)typeof(BattleSceneSetup).GetField("cardActionController", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle); // 실제 카드 처리기 조회
        typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { attack, new[] { boss }, false, (0, 0, 0), null }); // 현재 전투의 비용과 반향 인수를 포함한 실제 공격 카드 정산
        Assert.IsTrue(battle.BattleTurn.HasPendingBossPhaseTransition); // 카드 공격의 페이즈 전환 예약 확인
        typeof(BattleCardActionController).GetMethod("HandleActionSequenceCompleted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null); // 실제 카드 행동 완료 처리
        Assert.AreEqual(2, battle.BattleTurn.CurrentRound); // 적 행동 취소 후 다음 라운드 확인
        Assert.IsTrue(battle.BattleTurn.IsPlayerTurn); // 다음 아군 턴 확인
        Assert.AreEqual(3, battle.EnemyActionRuntime.FindAction(boss).PatternIndex); // 두 번째 페이즈 첫 행동 예고 확인
        EncounterData ordinary = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshWanderer"); // 실제 일반 소환 적 원본 조우 조회
        Assert.IsNotNull(ordinary); // 소환할 일반 원본 조우 확인
        Assert.IsTrue(battle.TrySummonEnemy(ordinary.Enemies[0])); // 보스 조우에 일반 적 소환
        BattleUnitRuntime summoned = battle.EnemyUnits[1]; // 실제 소환된 적 조회
        Assert.AreEqual(63, summoned.MaxHealth); // 소환 즉시 기존 조우 체력 125의 절반 올림 확인
        Assert.AreEqual(63, summoned.CurrentHealth); // 소환 즉시 최대 체력으로 시작 확인
        Assert.AreEqual(51, summoned.CurrentMental); // 기존 소환 적의 난이도 정신력 보존 확인
        summoned.TakeDamage(1, BattleDamageType.None); // 소환과 같은 프레임에 체력 피해 적용
        yield return null; // 모든 난이도와 화면 갱신 시점 통과
        Assert.AreEqual(63, summoned.MaxHealth); // 다음 프레임에 난이도 중복 적용 없음 확인
        Assert.AreEqual(62, summoned.CurrentHealth); // 첫 프레임에 받은 실제 피해 유지 확인
        summoned.TakeDamage(10000, BattleDamageType.None); // 원래 승리 조건 확인을 위해 소환 적 정리
        boss.TakeDamage(10000, BattleDamageType.None, battle.AllyUnits[0]); // 실제 보스 처치
        Assert.AreEqual(BattleResult.Victory, battle.BattleTurn.Result); // 실제 전투 승리 판정 확인
        Assert.IsTrue(BattleResultManager.Instance.HasPendingResult); // 탐사로 전달할 승리 결과 저장 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 보스 전투 검사 종료
} // 보스 전투 검사 종료
