using System.Collections; // 실제 카드 충돌과 전투 턴 대기
using System.Linq; // 전체 적 피해와 입은 횟수 조회
using NUnit.Framework; // 전격 대상 및 상태 판정 확인
using UnityEngine.TestTools; // 실제 플레이 모드 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 실제 손패와 적 전투 환경 재사용

public sealed class WinterfallWandStormTests // 실제 전격과 빙결 및 범위 지팡이 공격 검사
{ // 지팡이 대상과 상태 검사 시작
    [UnityTest] // 서로 다른 적의 전격과 한 생존 대상의 축소 처리 검사
    public IEnumerator LightningStrikeHitsTwoDistinctLivingTargetsOrOneSurvivor() // 손패 한 장과 행동력 지불 뒤의 실제 피해 검사
    { // 무작위 전격의 실제 검사 시작
        yield return new EnterPlayMode(); // 플레이 상태의 손패와 적 입력 환경 진입
        yield return CheckDistinctTargetHits(); // 실제 전격 두 사용에서 적 상태 확인
        yield return new ExitPlayMode(); // 독립 전투 과정 종료
    } // 무작위 전격의 실제 검사 종료

    private static IEnumerator CheckDistinctTargetHits() // 두 무작위 타격과 단일 생존 대상 검증
    { // 전격의 시험 전투 시작
        using var fight = new Fight("LIGHTNING_STRIKE", "LIGHTNING_STRIKE"); // 실제 카드 보상의 원본 전격 두 장 구성
        var first = fight.View("LIGHTNING_STRIKE").RuntimeCard; // 실제 손패에 등록된 첫 전격 인스턴스
        Assert.AreEqual(2, first.ApCost); // 원본 카드의 실제 고정 행동력 두 점 확인
        Assert.AreEqual(CardTargetType.RandomTwoEnemies, first.TargetType); // 전체 대상과 분리된 명시 무작위 대상 확인
        Assert.AreEqual(BattleDamageType.Magical, first.DamageType); // 두 대상 각각의 마법 피해 분류 확인
        int[] before = fight.Enemies.Select(enemy => enemy.CurrentHealth).ToArray(); // 각 적의 원래 체력 기록
        yield return fight.Use("LIGHTNING_STRIKE"); // 대상 클릭 없이 손패에서 직접 쓰는 전격 입력
        int[] losses = fight.Enemies.Select((enemy, index) => before[index] - enemy.CurrentHealth).ToArray(); // 모든 후보의 실제 체력 감소 비교
        CollectionAssert.AreEquivalent(new[] { 0, 0, 7, 7 }, losses); // 같은 적을 두 번 치지 않고 둘만 각각 마법 피해 칠 확인
        var survivor = fight.Enemies[2]; // 전투에 남길 실제 한 생존 적
        foreach (var enemy in fight.Enemies.Where(unit => unit != survivor)) // 다른 세 적을 전투불능으로 바꿀 대상 순회
        { // 다음 사용의 단일 후보 준비 시작
            enemy.TakeDamage(10_000, BattleDamageType.None); // 실전 피해로 해당 후보를 정상 전투불능 처리
        } // 다음 사용의 단일 후보 준비 종료
        int remaining = survivor.CurrentHealth; // 살아 있는 마지막 후보의 시전 전 실제 체력
        yield return fight.Use("LIGHTNING_STRIKE"); // 같은 카드의 자동 대상 확정으로 두 번째 실제 사용
        Assert.AreEqual(remaining - 7, survivor.CurrentHealth); // 후보가 하나일 때 그 적만 한 번 피해 적용
    } // 전격의 시험 전투 종료

    [UnityTest] // 기존 면역과 실제 한 적 턴의 빙결 검사
    public IEnumerator FreezeUsesNormalStunImmunityAndOneEnemyTurn() // 즉시 붙는 기존 기절과 실제 적 턴 종료 제거 확인
    { // 빙결 실전 검사 시작
        yield return new EnterPlayMode(); // 실제 상태의 턴 구독과 손패 검사 진입
        yield return CheckFreeze(); // 두 지정 대상에게 실제 빙결 사용
        yield return new ExitPlayMode(); // 독립 빙결 검사 종료
    } // 빙결 실전 검사 종료

    private static IEnumerator CheckFreeze() // 지정 대상 면역과 실제 행동 가능 여부 비교
    { // 빙결의 시험 전투 시작
        using var fight = new Fight("FREEZE", "FREEZE"); // 원본 빙결의 같은 장수로 정상과 면역 조건 비교
        var normal = fight.Enemies[0]; // 직접 지정한 첫 빙결 대상
        var immune = fight.Enemies[1]; // 기존 상태 면역을 갖는 적
        var nextAction = Attack(normal, fight.Allies[0]); // 같은 정상 적의 기존 행동 가능 판정
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 정상 디버프 부여를 거부할 기존 면역 준비
        Assert.AreEqual(3, fight.View("FREEZE").RuntimeCard.ApCost); // 빙결의 세 행동력 원본 비용 확인
        yield return fight.Use("FREEZE", 1); // 기존 면역 적을 실제로 누른 빙결 사용
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Stun)); // 면역 대상에 빙결이 일반 기절을 부여하지 않음 확인
        Assert.IsTrue(Attack(immune, fight.Allies[0]).IsExecutable); // 면역으로 기절하지 않은 적의 행동 보존 확인
        yield return fight.Use("FREEZE", 0); // 정상 적에게 별개의 실제 빙결 입력
        Assert.AreEqual(BattleStatusEffectType.Stun, nextAction.BlockingStatus); // 기존 적 행동이 인식하는 실제 기절 종류
        Assert.IsFalse(nextAction.IsExecutable); // 첫 적 턴 이전의 정상 기절 상태에서 행동 차단
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 시전 아군 이후의 실제 적 턴 시작
        Assert.IsFalse(nextAction.IsExecutable); // 실제 적 턴 시작 후에도 기절로 행동 불가 유지
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 기절이 작용한 한 번의 실제 적 턴 종료
        Assert.IsNull(normal.GetStatusEffect(BattleStatusEffectType.Stun)); // 남은 적 턴이 없는 기절의 정상 소모 확인
        Assert.IsTrue(nextAction.IsExecutable); // 정상 적 행동이 다시 실행 가능한 다음 상태 확인
    } // 빙결의 시험 전투 종료

    [UnityTest] // 실제 범위 피해와 명중한 적만 받는 감전과 기절 검사
    public IEnumerator StormAndMeteorApplyTheirStatusesOnlyOnLivingHits() // 다른 면역과 회피 및 사망 결과를 판정하는 실제 지팡이 공격
    { // 범위 지팡이의 실제 검사 시작
        yield return new EnterPlayMode(); // 플레이 상태의 적 피해와 상태 구독 환경 진입
        yield return CheckAreaWandHits(); // 번개 폭풍과 운석 낙하를 실제 손패에서 사용
        yield return new ExitPlayMode(); // 독립 범위 지팡이 검사 종료
    } // 범위 지팡이의 실제 검사 종료

    private static IEnumerator CheckAreaWandHits() // 두 공격의 타격과 상태 적용 및 감전 소모 확인
    { // 범위 지팡이의 시험 전투 시작
        using var fight = new Fight("LIGHTNING_STORM", "METEOR_FALL"); // 시트 원본의 두 실제 범위 공격 카드 구성
        var normal = fight.Enemies[0]; // 첫 공격에 명중하고 감전될 일반 적
        var immune = fight.Enemies[1]; // 피해는 입지만 상태는 받지 않는 면역 적
        var evading = fight.Enemies[2]; // 명중과 상태 효과를 모두 피하는 회피 적
        var fragile = fight.Enemies[3]; // 번개 폭풍의 실제 피해로 전투불능이 될 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 두 명중 후 상태를 모두 막는 기존 면역 준비
        evading.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 직접 카드 타격의 실제 명중률을 영으로 만드는 회피
        fragile.TakeDamage(fragile.CurrentHealth - 1, BattleDamageType.None); // 실제 피해 처리로 체력 한 점만 남길 네 번째 적
        int health = normal.CurrentHealth; // 일반 적의 첫 공격 전 실제 체력
        int immuneHealth = immune.CurrentHealth; // 기존 면역 적의 첫 공격 전 실제 체력
        int evaderHealth = evading.CurrentHealth; // 회피 적의 첫 공격 전 실제 체력
        Assert.AreEqual(3, fight.View("LIGHTNING_STORM").RuntimeCard.ApCost); // 번개 폭풍의 세 행동력 실제 비용 확인
        yield return fight.Use("LIGHTNING_STORM"); // 모든 생존 적을 향한 원본 번개 폭풍 사용
        Assert.AreEqual(health - 8, normal.CurrentHealth); // 일반 적의 실제 마법 피해 팔 확인
        Assert.AreEqual(immuneHealth - 8, immune.CurrentHealth); // 상태 면역이 일반 피해를 막지 않는지 확인
        Assert.AreEqual(evaderHealth, evading.CurrentHealth); // 회피한 직접 타격의 마법 피해 미적용 확인
        Assert.AreEqual(1, normal.GetStatusEffect(BattleStatusEffectType.Shock)?.EffectiveValue); // 유효하게 명중한 생존 적의 감전 수치 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Shock)); // 기존 면역의 감전 차단 확인
        Assert.IsNull(evading.GetStatusEffect(BattleStatusEffectType.Shock)); // 빗나간 타격의 감전 미적용 확인
        Assert.IsTrue(fragile.IsDead); // 같은 공격의 살아 있던 마지막 후보 실제 처치 확인
        Assert.IsNull(fragile.GetStatusEffect(BattleStatusEffectType.Shock)); // 피해로 쓰러진 적에게 후속 감전 미부여 확인
        var action = Attack(normal, fight.Allies[0]); // 감전은 독립 효과인 일반 적의 실제 행동 판정
        Assert.IsTrue(action.IsExecutable); // 감전 자체가 기절을 중복 부여하지 않음 확인
        normal.GainBlock(20); // 감전에 앞선 다른 피해 방어도의 비교 준비
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 감전이 발동할 다음 적 턴을 실제로 시작
        Assert.AreEqual(health - 9, normal.CurrentHealth); // 방어도 보유와 무관한 감전의 기존 체력 피해 한 점 확인
        Assert.IsNull(normal.GetStatusEffect(BattleStatusEffectType.Shock)); // 감전이 다음 시작에서 한 번만 소모되는지 확인
        Assert.IsTrue(action.IsExecutable); // 감전 처리 이후에도 별도의 전투 행동 유지 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 일반 적 턴을 실제로 끝내고 두 번째 카드 사용 가능 상태 진입
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 재사용 여부를 따지지 않는 기존 면역의 정상 갱신
        evading.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 두 번째 범위 타격에도 실제 회피 조건 유지
        normal.ClearBlock(); // 이전 감전의 비교용 방어도를 운석 피해 검증 전에 정리
        Assert.AreEqual(4, fight.View("METEOR_FALL").RuntimeCard.ApCost); // 운석 낙하의 네 행동력 실제 비용 확인
        yield return fight.Use("METEOR_FALL"); // 다음 아군 턴에 원본 전역 운석 카드 사용
        Assert.AreEqual(health - 29, normal.CurrentHealth); // 감전이 반복 적용되지 않고 운석 피해 이십만 들어간 결과
        Assert.AreEqual(immuneHealth - 28, immune.CurrentHealth); // 면역 적도 두 종류의 범위 피해는 정상으로 받은 상태
        Assert.AreEqual(evaderHealth, evading.CurrentHealth); // 두 번 모두 회피한 적의 실제 체력 보존
        Assert.AreEqual(BattleStatusEffectType.Stun, action.BlockingStatus); // 정상 명중이 기존 한 적 턴 기절로 연결됐는지 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Stun)); // 실제 명중했어도 기존 면역의 운석 기절 차단 확인
        Assert.IsNull(evading.GetStatusEffect(BattleStatusEffectType.Stun)); // 회피 성공한 적에게 운석 기절 미적용 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 운석 기절이 유지되는 실제 적 턴 진입
        Assert.IsFalse(action.IsExecutable); // 정상 적이 이번 적 턴에 행동 불가 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 운석 기절의 실제 한 적 턴 종료
        Assert.IsTrue(action.IsExecutable); // 다음에는 감전이나 기절이 남지 않는 실제 행동 확인
        Assert.AreEqual(health - 29, normal.CurrentHealth); // 두 번째 적 턴 이후 감전이 재발동하지 않는 체력 확인
    } // 범위 지팡이의 시험 전투 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime enemy, BattleUnitRuntime target) // 정해진 적의 실제 실행 가능 공격 생성
    { // 기존 적 행동 원본 처리 시작
        return new BattleEnemyAction(enemy, target, EnemyActionType.Attack, BattleDamageType.Physical, 1, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "지팡이 행동 검사", 0, 1, 1, 0); // 기존 대상 선택과 기절 판정이 연결된 일반 행동 생성
    } // 기존 적 행동 원본 처리 종료
} // 지팡이 대상과 상태 검사 종료
