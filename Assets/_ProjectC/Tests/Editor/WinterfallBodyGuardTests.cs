using NUnit.Framework; // 전투 결과 비교
using UnityEngine; // 임시 원본 생성

public sealed class WinterfallBodyGuardTests // 몸으로 막기 전투 검사
{ // 검사 시작
    private CharacterData characterData; // 공용 아군 원본
    private EnemyData enemyData; // 공용 적 원본
    private BattleUnitRuntime protectedAlly; // 보호 대상 아군
    private BattleUnitRuntime guard; // 피해를 대신 받을 아군
    private BattleEnemyActionRuntime actions; // 적 행동 관리자

    [SetUp] // 각 검사 전 독립 전투 준비
    public void SetUp() // 임시 아군과 적 구성
    { // 구성 시작
        characterData = ScriptableObject.CreateInstance<CharacterData>(); // 기본 방어력의 아군 원본 생성
        enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 첫 생존자에게 피해 8점 설정
        protectedAlly = BattleUnitRuntime.CreateAlly(characterData); // 첫 배치에 보호 대상 생성
        guard = BattleUnitRuntime.CreateAlly(characterData); // 두 번째 배치에 보호자 생성
        BattleUnitRuntime[] enemies = { BattleUnitRuntime.CreateEnemy(enemyData), BattleUnitRuntime.CreateEnemy(enemyData) }; // 적 두 마리 생성
        actions = new BattleEnemyActionRuntime(enemies, new[] { protectedAlly, guard }); // 보호 대상 우선 전투 구성
    } // 구성 종료

    [TearDown] // 각 검사 후 자원 정리
    public void TearDown() // 전투와 임시 원본 해제
    { // 정리 시작
        actions?.Dispose(); // 적 행동 관리자 해제
        Object.DestroyImmediate(characterData); // 임시 아군 원본 제거
        Object.DestroyImmediate(enemyData); // 임시 적 원본 제거
    } // 정리 종료

    [Test] // 예고 전에 설정한 단일 대신 받기 검사
    public void BodyGuardSpendsOnceAndUsesGuardBlock() // 보호자 방어도와 첫 명중 소모 확인
    { // 검사 시작
        guard.GainBlock(5); // 보호자 방어도 5점 부여
        protectedAlly.GainBlock(2); // 보호 대상 방어도 2점 부여
        Assert.IsTrue(actions.TryBodyGuard(guard, protectedAlly)); // 예정 공격 전 보호 설정 확인
        Assert.AreEqual(2, actions.PrepareActions()); // 두 적의 공격 생성 확인
        BattleEnemyAction first = actions.PlannedActions[0]; // 첫 공격 조회
        BattleEnemyAction second = actions.PlannedActions[1]; // 두 번째 공격 조회
        Assert.AreSame(guard, first.Target); // 첫 공격의 보호자 예고 확인
        Assert.AreSame(protectedAlly, second.Target); // 두 번째 공격의 기존 대상 예고 확인
        Assert.AreEqual(3, first.PreviewDamage().AppliedDamage); // 보호자 방어도 적용 예고 확인
        Assert.AreEqual(3, actions.ExecuteAction(first).DamageResult.AppliedDamage); // 보호자에게 실제 피해 3점 확인
        Assert.AreEqual(0, guard.CurrentBlock); // 보호자 방어도 소모 확인
        Assert.AreEqual(2, protectedAlly.CurrentBlock); // 첫 공격 뒤 보호 대상 방어도 보존 확인
        Assert.AreEqual(protectedAlly.MaxHealth, protectedAlly.CurrentHealth); // 첫 공격 뒤 보호 대상 체력 보존 확인
        Assert.AreEqual(6, actions.ExecuteAction(second).DamageResult.AppliedDamage); // 다음 공격은 원래 아군에게 피해 6점 확인
        Assert.AreEqual(guard.MaxHealth - 3, guard.CurrentHealth); // 보호자의 체력 피해 한 번 확인
        Assert.AreEqual(protectedAlly.MaxHealth - 6, protectedAlly.CurrentHealth); // 보호 대상의 두 번째 피해 확인
    } // 검사 종료

    [TestCase(BattleStatusEffectType.Stagger)] // 취소된 공격 확인
    [TestCase(BattleStatusEffectType.Blind)] // 빗나간 공격 확인
    [TestCase(BattleStatusEffectType.NextAttackDown)] // 피해 0점 공격 확인
    public void BodyGuardWaitsThroughAttacksWithoutPositiveHits(BattleStatusEffectType attackerStatus) // 유효 피해 전 보호 유지 확인
    { // 검사 시작
        Assert.IsTrue(actions.TryBodyGuard(guard, protectedAlly)); // 대신 받기 설정
        actions.PrepareActions(); // 공격 두 개 예고 생성
        BattleEnemyAction first = actions.PlannedActions[0]; // 상태를 받을 첫 공격 조회
        first.Actor.ApplyStatusEffect(attackerStatus, 100, 1, 1); // 행동 취소 또는 빗나감과 피해 감소 보장
        BattleEnemyActionResult result = actions.ExecuteAction(first); // 피해 없는 첫 행동 실행
        Assert.AreEqual(0, result.DamageResult.AppliedDamage); // 첫 행동의 실제 피해 없음 확인
        Assert.AreEqual(attackerStatus == BattleStatusEffectType.Blind, result.WasMissed); // 실명만 빗나감으로 처리 확인
        Assert.AreSame(guard, actions.PlannedActions[0].Target); // 두 번째 공격을 대신 받는 예고 확인
        actions.ExecuteAction(actions.PlannedActions[0]); // 두 번째 공격 실행
        Assert.AreEqual(protectedAlly.MaxHealth, protectedAlly.CurrentHealth); // 보호 대상 체력 유지 확인
        Assert.AreEqual(guard.MaxHealth - 8, guard.CurrentHealth); // 유효 공격만 보호자에게 적용 확인
    } // 검사 종료

    [Test] // 보호자 사망 시 예정 대상 복구 검사
    public void BodyGuardClearsWhenGuardDies() // 사망한 보호자 제외 확인
    { // 검사 시작
        Assert.IsFalse(actions.TryBodyGuard(protectedAlly, protectedAlly)); // 자기 자신 보호 거부 확인
        Assert.IsFalse(actions.TryBodyGuard(actions.EnemyUnits[0], protectedAlly)); // 적 보호자 거부 확인
        Assert.IsTrue(actions.TryBodyGuard(guard, protectedAlly)); // 살아 있는 동료 보호 설정
        actions.PrepareActions(); // 첫 공격 예고 생성
        Assert.AreSame(guard, actions.PlannedActions[0].Target); // 살아 있는 보호자로 예고 확인
        guard.TakeDamage(guard.MaxHealth, BattleDamageType.Physical); // 공격 전 보호자 사망 처리
        Assert.IsFalse(actions.TryBodyGuard(guard, protectedAlly)); // 사망 보호자 재등록 거부 확인
        Assert.AreSame(protectedAlly, actions.PlannedActions[0].Target); // 원래 공격 대상 예고 확인
        actions.ExecuteAction(actions.PlannedActions[0]); // 보호 없이 첫 공격 실행
        Assert.AreEqual(protectedAlly.MaxHealth - 8, protectedAlly.CurrentHealth); // 원래 대상으로 피해 적용 확인
    } // 검사 종료

    [Test] // 상호 보호의 연쇄 전환 방지 검사
    public void BodyGuardDoesNotRedirectAnInterceptedHitAgain() // 실제 피격 대상 전환 한 번 확인
    { // 검사 시작
        Assert.IsTrue(actions.TryBodyGuard(guard, protectedAlly)); // 첫 아군에 대한 보호 설정
        Assert.IsTrue(actions.TryBodyGuard(protectedAlly, guard)); // 두 번째 아군에 대한 보호 설정
        actions.PrepareActions(); // 현재 공격 예고 생성
        BattleEnemyAction first = actions.PlannedActions[0]; // 첫 공격 조회
        Assert.AreSame(guard, first.Target); // 상호 보호 중 첫 보호자 예고 확인
        actions.ExecuteAction(first); // 첫 보호자가 공격 받기
        Assert.AreEqual(guard.MaxHealth - 8, guard.CurrentHealth); // 한 번만 전환된 피해 확인
        BattleEnemyAction second = actions.PlannedActions[0]; // 남은 공격 조회
        Assert.IsTrue(actions.TryRedirectAttack(second.Actor, guard)); // 두 번째 공격의 원래 대상을 보호자로 변경
        Assert.AreSame(protectedAlly, second.Target); // 아직 사용하지 않은 두 번째 보호 확인
        actions.ExecuteAction(second); // 두 번째 보호자가 공격 받기
        Assert.AreEqual(protectedAlly.MaxHealth - 8, protectedAlly.CurrentHealth); // 독립된 둘째 전환 확인
    } // 검사 종료
} // 검사 종료
