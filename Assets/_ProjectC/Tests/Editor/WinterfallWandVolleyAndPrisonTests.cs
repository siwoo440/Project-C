using System; // 검사의 정수 난수 범위 지정
using System.Collections; // 실제 전투 애니메이션 대기
using System.Collections.Generic; // 실제 타격 대상 순서 기록
using System.Linq; // 전투에서 남은 실제 적 순회
using NUnit.Framework; // 전투 상태의 실제 값 검증
using UnityEngine; // 전투의 실제 시간 조회
using UnityEngine.TestTools; // 플레이 모드 카드 입력 검사
using Fight = WinterfallSwordCombatTests.Fight; // 공용 전투와 실제 손패 검사 환경
using Random = UnityEngine.Random; // 카드 상태와 대상의 확률 난수

public sealed class WinterfallWandVolleyAndPrisonTests // 연쇄 번개와 화염 쇄도 및 서리 감옥의 실제 검사
{ // 지팡이 연타 상태 검사 시작
    [UnityTest] // 타격 대상 재추첨과 살아 있는 같은 적의 반복 선택
    public IEnumerator ChainLightningCanRepeatALivingTargetAndRecalculatesAfterItFalls() // 적 클릭 없는 카드와 세 독립 타격 확인
    { // 연쇄 번개 대상 검사 시작
        yield return new EnterPlayMode(); // 실제 클릭과 일반 전투 진행 준비
        yield return CheckChainTargets(); // 실제 카드의 반복 타격 및 사망 뒤 후보 갱신
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 연쇄 번개 대상 검사 종료

    private static IEnumerator CheckChainTargets() // 같은 적의 반복과 사망 후 실제 후보 재계산
    { // 세 타격 후보 준비 시작
        using var fight = new Fight("CHAIN_LIGHTNING"); // 실제 연쇄 번개 원본 한 장
        fight.Enemies[0].TakeDamage(188, BattleDamageType.None); // 두 타격으로 정확히 쓰러지는 첫 적
        fight.Enemies[3].TakeDamage(200, BattleDamageType.None); // 사용 전에 이미 쓰러진 네 번째 적
        int seed = FindSeed(() => Random.Range(0, 3) == 0 && Random.Range(0, 3) == 0); // 첫 적을 두 번 고르는 실제 시드
        Random.InitState(seed); // 시간과 무관하게 정해진 두 첫 선택 복구
        Random.Range(0, 3); // 첫 타격의 실제 생존 후보 세 명
        Random.Range(0, 3); // 살아 있는 첫 적을 다시 고르는 실제 후보 세 명
        int next = 1 + Random.Range(0, 2); // 첫 적 사망 후 남은 두 명의 기대 위치
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 비용 차감 다음 실제 타격 전에 난수 설정
        var hitSlots = new List<int>(); // 각 실제 피해 사건의 대상 위치
        for (int index = 0; index < fight.Enemies.Length; index++) // 모든 배치 적의 피해 사건 확인
        { // 대상별 실제 피해 순서 연결 시작
            int slot = index; // 이번에 확인할 실제 적 위치
            fight.Enemies[index].DamageResolved += (source, target, damage) => hitSlots.Add(slot); // 적용된 실제 피해의 대상 기록
        } // 대상별 실제 피해 순서 연결 종료
        int before = fight.Points.CurrentActionPoints; // 사용 전 실제 공용 행동력
        yield return fight.Use("CHAIN_LIGHTNING"); // 적을 클릭하지 않고 손패에서 실제 사용
        Assert.AreEqual(before - 2, fight.Points.CurrentActionPoints); // 세 타격의 행동력 두 점 한 번 차감
        CollectionAssert.AreEqual(new[] { 0, 0, next }, hitSlots); // 첫 적 반복 뒤 죽은 적을 제외한 새 후보
        Assert.IsTrue(fight.Enemies[0].IsDead); // 마법 피해 여섯 두 번의 실제 첫 처치
        Assert.AreEqual(194, fight.Enemies[next].CurrentHealth); // 재추첨한 적에게 적용된 마법 피해 여섯
        Assert.AreEqual(200, fight.Enemies[next == 1 ? 2 : 1].CurrentHealth); // 선택되지 않은 다른 후보의 체력 보존
    } // 세 타격 후보 준비 종료

    [UnityTest] // 실제 마력 집중과 회피 및 최종 승리 정산 검증
    public IEnumerator ChainLightningUsesConcentrationPerHitAndStopsOnTheLastEnemy() // 세 독립 피해와 마지막 적 이후 난수 미소모 확인
    { // 마력과 승리 처리 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 상태와 카드 충돌의 플레이 모드
        yield return CheckChainSettlement(); // 집중과 빗나감 및 마지막 카드 이동 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 마력과 승리 처리 검사 종료

    private static IEnumerator CheckChainSettlement() // 실제 마력 보정과 승리 직전 카드 이동 대조
    { // 연쇄 번개 정산 경계 검사 시작
        using var fight = new Fight("CHAIN_LIGHTNING", "CHAIN_LIGHTNING", "CHAIN_LIGHTNING"); // 한 적에게 반복할 실제 카드 세 장
        foreach (var fallen in fight.Enemies.Skip(1)) // 한 명을 제외한 실제 적 배치 확인
        { // 추가 후보 제거 시작
            fallen.TakeDamage(fallen.CurrentHealth, BattleDamageType.None); // 실제 사망 처리를 거친 제거
        } // 추가 후보 제거 종료
        var enemy = fight.Enemies[0]; // 연쇄 타격을 모두 받는 유일한 생존 적
        fight.Allies[0].ApplyStatusEffect(BattleStatusEffectType.ManaConcentration, 4, 1, 1); // 다음 지팡이의 타격마다 피해 넷을 더할 실제 집중
        yield return fight.Use("CHAIN_LIGHTNING"); // 실제 집중을 사용할 지팡이 연속 타격
        Assert.AreEqual(170, enemy.CurrentHealth); // 세 타격 각각에 넣은 마법 피해 열
        Assert.IsNull(fight.Allies[0].GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 일회 마력 집중의 소모 확인
        enemy.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 직접 공격이 모두 빗나가는 기존 회피
        yield return fight.Use("CHAIN_LIGHTNING"); // 같은 적에 대한 세 정상 명중 판정
        Assert.AreEqual(170, enemy.CurrentHealth); // 회피하는 적에게 독립 피해를 강제하지 않는지 확인
        Assert.IsTrue(enemy.RemoveStatusEffect(BattleStatusEffectType.Evasion)); // 마지막 타격에 앞선 실제 회피 해제
        enemy.TakeDamage(enemy.CurrentHealth - 6, BattleDamageType.None); // 연쇄의 첫 명중이면 끝날 체력
        const int seed = 790; // 마지막 카드의 실제 추첨 난수 시작값
        Random.InitState(seed); // 첫 추첨만 진행한 후의 기준 상태 확인
        Random.Range(0, 1); // 기존 연타에서 한 후보에게 쓰는 한 번의 추첨
        var afterFirstTarget = Random.state; // 다음 두 타격에 난수 미소모일 예상 상태
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 실제 마지막 사용의 충돌 직전 상태 복원
        bool discardedAtVictory = false; // 결과 확정 전에 카드 이동 확인
        fight.Turns.BattleCompleted += result => discardedAtVictory = result == BattleResult.Victory && fight.Deck.HandCount == 0; // 실제 승리 시점의 손패 확인
        yield return fight.Use("CHAIN_LIGHTNING"); // 마지막 적을 한 번만 타격해 실제 승리
        Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 일반 승리 경로 이용 확인
        Assert.IsTrue(discardedAtVictory); // 사용한 카드가 손을 떠난 뒤 확정한 실제 결과
        Assert.AreEqual(afterFirstTarget, Random.state); // 마지막 적 이후 무작위 대상 추가 추첨 없음 확인
    } // 연쇄 번개 정산 경계 검사 종료

    [UnityTest] // 서로 다른 두 적의 독립 화상 부여 확인
    public IEnumerator FlameSurgeAccountsForBothTargetsAndRollsBurnIndependently() // 실제 순차 선택과 손패 정산의 별도 타격 검증
    { // 화염 쇄도 검사 시작
        yield return new EnterPlayMode(); // 실제 원본 카드와 입력의 플레이 모드
        yield return CheckFlameSurge(); // 두 명의 별도 확률과 한 명 대조 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 화염 쇄도 검사 종료

    private static IEnumerator CheckFlameSurge() // 실제 서로 다른 적과 독립적인 화상 비교
    { // 두 적의 순차 입력 검사 시작
        using var fight = new Fight("FLAME_SURGE", "FLAME_SURGE"); // 두 적과 마지막 한 적을 검사할 실제 원본
        int seed = FindSeed(() => Random.Range(0, 100) < 35 && Random.Range(0, 100) >= 35); // 먼저 성공하고 다음 타격은 실패하는 실제 확률 묶음
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 두 타격 처리 직전 난수 기록 복원
        int points = fight.Points.CurrentActionPoints; // 두 대상 확정 전의 공용 행동력
        fight.View("FLAME_SURGE").OnPointerClick(null); // 게임과 같은 손패의 순차 적 선택
        fight.EnemyViews[0].OnPointerClick(null); // 실제 첫 생존 적 지정
        fight.EnemyViews[0].OnPointerClick(null); // 같은 적을 다시 선택할 수 없는 실제 입력
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 서로 다른 두 번째 적을 고르기 전 결제 없음
        fight.EnemyViews[1].OnPointerClick(null); // 실제로 다른 두 번째 생존 적 선택
        float until = Time.realtimeSinceStartup + 10f; // 전투 카드 연출의 정상 종료 한도
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 두 대상 타격과 카드 이동의 실제 실행 대기
        { // 한 프레임 더 진행 시작
            yield return null; // 실제 연출과 피해가 진행할 프레임
        } // 한 프레임 더 진행 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 사용 연출의 완료 확인
        Assert.AreEqual(points - 2, fight.Points.CurrentActionPoints); // 두 타격이 같은 카드의 행동력을 한 번만 차감
        Assert.AreEqual(191, fight.Enemies[0].CurrentHealth); // 첫 적의 마법 피해 아홉
        Assert.AreEqual(191, fight.Enemies[1].CurrentHealth); // 둘째 적의 독립 마법 피해 아홉
        Assert.AreEqual(200, fight.Enemies[2].CurrentHealth); // 선택하지 않은 적의 원래 체력
        var burn = fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Burn); // 첫 명중에 실제로 부여된 화상
        Assert.AreEqual(2, burn?.EffectiveValue); // 실제 최초 화상 수치 둘
        Assert.AreEqual(2, burn?.RemainingTurns); // 부여 직후 화상 두 적 턴 확인
        Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Burn)); // 두 번째 명중의 독립 확률 실패 확인
        foreach (var other in fight.Enemies.Skip(1)) // 단일 적 타격 대조를 위한 나머지 후보
        { // 추가 선택을 제거할 실제 사망 시작
            other.TakeDamage(other.CurrentHealth, BattleDamageType.None); // 첫 적 이외의 적 전투불능 처리
        } // 추가 선택을 제거할 실제 사망 종료
        yield return fight.Use("FLAME_SURGE", 0); // 적 하나만 남을 때 한 번 실제 선택
        Assert.AreEqual(182, fight.Enemies[0].CurrentHealth); // 남은 한 적에 두 피해를 반복하지 않는 결과
    } // 두 적의 순차 입력 검사 종료

    [UnityTest] // 실제 다음 행동 한 번을 잃는 경직 상태 검증
    public IEnumerator FrostPrisonBlocksOneEnemyActionOnlyWhenItsFortyPercentRollSucceeds() // 속도 하락과 구분되는 실제 행동 차단 확인
    { // 서리 감옥 실제 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 타격과 적 턴 사용
        yield return CheckFrostPrison(); // 상태 확률과 다음 행동의 한 번 차단 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 서리 감옥 실제 검사 종료

    private static IEnumerator CheckFrostPrison() // 직접 명중과 적 행동 차단 및 해제를 실제로 비교
    { // 서리 감옥 경계 검사 시작
        using var fight = new Fight("FROST_PRISON", "FROST_PRISON"); // 실제 원본 카드의 확률 성공·실패 대조
        int seed = FindSeed(() => Random.Range(0, 100) >= 40); // 첫 직접 타격의 경직 실패 난수
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 사용한 실제 카드의 명중 직전 난수
        yield return fight.Use("FROST_PRISON", 0); // 상태 부여에 실패하는 정상 마법 타격
        Assert.AreEqual(192, fight.Enemies[0].CurrentHealth); // 경직 실패와 별개인 마법 피해 여덟
        Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Stagger)); // 확률 실패의 경직 없음
        seed = FindSeed(() => Random.Range(0, 100) < 40); // 두 번째 직접 명중의 성공 난수
        yield return fight.Use("FROST_PRISON", 1); // 실제로 다음 행동을 차단하는 명중
        var enemy = fight.Enemies[1]; // 경직이 적용된 실제 행동 주체
        Assert.AreEqual(192, enemy.CurrentHealth); // 경직 성공과 별개인 피해 여덟
        Assert.AreEqual(1, enemy.GetStatusEffect(BattleStatusEffectType.Stagger)?.RemainingTurns); // 기존 상태의 한 적 턴
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 단순 속도 변경을 대신 적용하지 않는 규칙 확인
        var action = new BattleEnemyAction(enemy, fight.Allies[0], EnemyActionType.Attack, BattleDamageType.Physical, 8, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "경직 검사", 1, 1, 5, 0); // 정상 다음 적 공격의 실제 행동 객체
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 서리 감옥 다음의 실제 적 턴 시작
        Assert.AreEqual(BattleStatusEffectType.Stagger, action.BlockingStatus); // 예고와 실행이 쓰는 기존 행동 차단 종류
        Assert.IsFalse(action.IsExecutable); // 남은 한 적 행동의 실제 실행 차단
        Assert.AreEqual(0, action.Execute().DamageResult.AppliedDamage); // 원래 실행 경로에서 막힌 한 행동의 피해 없음
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.Stagger)); // 턴 경과와 구분한 실제 행동 한 번의 경직 소모
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 차단된 적 턴의 실제 종료 정산
        Assert.IsTrue(action.IsExecutable); // 이후 정상 적 행동이 다시 실행 가능한 상태
    } // 서리 감옥 경계 검사 종료

    private static int FindSeed(Func<bool> accepts) // 실제 확률 경계를 재현하는 게임 시드 검색
    { // 난수 시드 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 원본 난수의 한정된 시드 비교
        { // 이번 실제 난수 시작점 설정
            Random.InitState(seed); // 원하는 결과에 대한 게임 난수 초기화
            if (accepts()) // 타격 확률 또는 대상 추첨의 실제 조건 검사
            { // 일치하는 난수 시드 반환 시작
                return seed; // 다음 실제 카드 명중에 쓸 시드 반환
            } // 일치하는 난수 시드 반환 종료
        } // 이번 실제 난수 시작점 종료
        throw new InvalidOperationException("No matching seed found."); // 원하는 확률과 대상의 확인 실패
    } // 난수 시드 검색 종료
} // 지팡이 연타 상태 검사 종료
