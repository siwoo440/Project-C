using System.Reflection; // 카드 정산과 같은 내부 행동력 예약 호출
using NUnit.Framework; // 실제 전투 피해와 행동력 검사
using UnityEngine; // 검사 전용 적과 아군 원본 생성

public sealed class WinterfallRetaliationAndGiantShieldTests // 방벽 반격과 다음 아군 행동력 검사
{ // 전투 규칙 검사 시작
    private CharacterData allySource; // 실제 피격 대상 아군 원본
    private EnemyData enemySource; // 높은 물리 방어의 실제 공격자 원본
    private DeckData sourceDeck; // 드로우 없이 턴을 진행할 덱 원본
    private BattleUnitRuntime ally; // 반격 상태를 가진 아군
    private BattleUnitRuntime enemy; // 공격을 명중시킬 적
    private BattleActionPointRuntime points; // 전투의 실제 공용 행동력
    private BattleTurnRuntime turns; // 아군과 적의 실제 단계
    private BattleStatusEffectController statuses; // 턴 경계에 상태 지속 적용

    [SetUp] // 각 검사의 독립 전투 준비
    public void SetUp() // 실제 아군과 적 및 턴 초기화
    { // 검사 전투 시작
        allySource = ScriptableObject.CreateInstance<CharacterData>(); // 아군 원본 생성
        enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        JsonUtility.FromJsonOverwrite("{\"physicalDefense\":99}", enemySource); // 방벽 반격의 고정 피해 검사 준비
        sourceDeck = ScriptableObject.CreateInstance<DeckData>(); // 빈 검사 덱 생성
        ally = BattleUnitRuntime.CreateAlly(allySource); // 타격을 실제로 받을 아군 생성
        enemy = BattleUnitRuntime.CreateEnemy(enemySource); // 직접 공격을 실행할 적 생성
        points = new BattleActionPointRuntime(3); // 비교 기준의 기본 행동력 세 점
        var allies = new[] { ally }; // 단일 아군 전투 배치
        var enemies = new[] { enemy }; // 단일 적 전투 배치
        turns = new BattleTurnRuntime(BattleDeckRuntime.Create(sourceDeck, allies), points, allies, enemies, 0, BattleType.Boss); // 강제 보스 전환이 가능한 실제 턴
        statuses = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 적 종료 시의 상태 만료 연결
        Assert.IsTrue(turns.StartBattle(0)); // 첫 아군 턴 정상 진입 확인
    } // 검사 전투 시작 종료

    [TearDown] // 각 검사의 연결 및 원본 해제
    public void TearDown() // 검사에 사용한 모든 자원 정리
    { // 검사 자원 정리 시작
        statuses?.Dispose(); // 진영별 상태 지속 처리 해제
        turns?.Dispose(); // 아군과 적의 전투 이벤트 해제
        Object.DestroyImmediate(sourceDeck); // 검사 원본 덱 제거
        Object.DestroyImmediate(enemySource); // 검사 적 원본 제거
        Object.DestroyImmediate(allySource); // 검사 아군 원본 제거
    } // 검사 자원 정리 종료

    [Test] // 완전 방어 및 체력 피격의 고정 반격 순서 검사
    public void BulwarkRetaliatesAfterTheFullHitAndExpiresAfterThatEnemyRound() // 원래 피격 처리와 반격 및 상태 만료의 순서 확인
    { // 방벽 반격 검사 시작
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.RetributionBulwark, 5, 1, 1)); // 실제 응징 방벽의 한 적 턴 상태 적용
        ally.GainBlock(8); // 양수 직접 공격을 모두 받을 방어도
        enemy.GainBlock(30); // 고정 반격이 우회할 공격자의 방어도
        Assert.IsTrue(turns.EndPlayerTurn()); // 방벽이 남는 실제 첫 적 턴 진입
        BattleDamageType retaliationType = BattleDamageType.None; // 적에게 적용한 반격 피해 종류
        bool statusWasApplied = false; // 반격 이전 원래 공격 효과의 적용 기록
        enemy.DamageTaken += (_, damage) => // 실제 적 피격 이벤트 관찰
        { // 반격 이벤트 기록 시작
            retaliationType = damage.DamageType; // 고정 반격 결과의 물리 피해 확인 대상
            statusWasApplied |= ally.GetStatusEffect(BattleStatusEffectType.Bleed) != null; // 원래 공격의 상태 부여 선행 확인 대상
        }; // 반격 이벤트 기록 종료
        var result = Attack(BattleStatusEffectType.Bleed).Execute(); // 방어도에 막히지만 상태를 남기는 공격 실행
        Assert.AreEqual(8, result.DamageResult.AbsorbedDamage); // 명중 원래 피해가 완전히 막힌 상태 확인
        Assert.AreEqual(0, result.DamageResult.AppliedDamage); // 완전 방어의 체력 피해 없음 확인
        Assert.AreEqual(enemy.MaxHealth - 5, enemy.CurrentHealth); // 완전 방어에도 고정 반격 다섯 확인
        Assert.AreEqual(30, enemy.CurrentBlock); // 적의 기존 방어도 보존 확인
        Assert.AreEqual(BattleDamageType.Physical, retaliationType); // 실제 반격의 물리 피해 분류 확인
        Assert.IsTrue(statusWasApplied); // 원래 타격의 부가 상태가 반격보다 먼저 적용되는지 확인
        Assert.AreEqual(8, Attack().Execute().DamageResult.AppliedDamage); // 같은 적 턴의 두 번째 양수 체력 피해 확인
        Assert.AreEqual(enemy.MaxHealth - 10, enemy.CurrentHealth); // 같은 적 턴의 두 번째 명중에도 반격 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 적 턴 종료의 방벽 제거
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.RetributionBulwark)); // 다음 적 턴으로 반격 미이월 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 방벽이 없는 다음 적 턴 진입
        Attack().Execute(); // 다음 적 턴의 다시 명중한 공격
        Assert.AreEqual(enemy.MaxHealth - 10, enemy.CurrentHealth); // 만료 이후 다음 피격의 반격 없음 확인
    } // 방벽 반격 검사 종료

    [Test] // 비명중과 피해 무효 및 지속 피해의 구분 검사
    public void BulwarkSkipsMissesNullificationAndDamageOverTime() // 실제 적 직접 공격이 아닌 피해의 반격 차단 확인
    { // 반격 제외 검사 시작
        ally.ApplyStatusEffect(BattleStatusEffectType.RetributionBulwark, 5, 1, 1); // 한 적 턴의 실제 방벽 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 다음 피해의 한 차례 완전 무효 준비
        Assert.IsTrue(turns.EndPlayerTurn()); // 공격이 발생하는 실제 적 턴 진입
        enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 1, 1); // 첫 공격의 명중 가능성 제거
        Assert.IsTrue(Attack().Execute().WasMissed); // 첫 적 단일 공격의 확정 빗나감 확인
        Assert.AreEqual(enemy.MaxHealth, enemy.CurrentHealth); // 빗나간 공격에 반격 미발동 확인
        enemy.RemoveStatusEffect(BattleStatusEffectType.Blind); // 다음 공격의 실제 양수 명중 준비
        Assert.IsTrue(Attack().Execute().DamageResult.WasDamageNullified); // 성공한 명중의 완전 피해 무효 확인
        Assert.AreEqual(enemy.MaxHealth, enemy.CurrentHealth); // 명중했으나 피해가 무효인 공격에 반격 없음 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 4, 1, 1, enemy); // 직접 공격과 구분한 출혈 피해 준비
        new BattleStatusEffectProcessor().ProcessPhaseEnd(ally, turns.CurrentRound); // 피해 후 공용 신호를 포함한 실제 출혈 처리
        Assert.AreEqual(ally.MaxHealth - 4, ally.CurrentHealth); // 출혈의 실제 체력 손실 확인
        Assert.AreEqual(enemy.MaxHealth, enemy.CurrentHealth); // 직접 적 공격이 아닌 출혈의 반격 미발동 확인
        Attack().Execute(); // 제외 사례 뒤의 정상 양수 직접 공격
        Assert.AreEqual(enemy.MaxHealth - 5, enemy.CurrentHealth); // 후속 정상 명중의 반격 유지 확인
    } // 반격 제외 검사 종료

    [TestCase(false, 1, 2)] // 적 턴을 거친 일반 다음 플레이어 턴
    [TestCase(true, 1, 2)] // 적 턴을 생략한 보스 다음 플레이어 턴
    [TestCase(false, 4, 0)] // 반복 플레이 시 적용 행동력의 영 하한
    public void GiantShieldPenaltySurvivesRestoreAndIsSpentOnOnlyTheNextPlayerTurn(bool bossTransition, int uses, int expected) // 현재 턴과 적 턴 및 다음 아군 턴의 행동력 구분 확인
    { // 행동력 패널티 검사 시작
        var queue = typeof(BattleTurnRuntime).GetMethod("QueueNextPlayerTurnActionPointLoss", BindingFlags.Instance | BindingFlags.NonPublic); // 카드 정산에서 사용하는 예약 메서드 조회
        Assert.IsNotNull(queue); // 카드 라우팅과 실제 턴 관리자의 연결 확인
        for (int count = 0; count < uses; count++) // 같은 아군 턴의 거인 방패 반복 사용 대조
        { // 개별 카드 정산 시작
            queue.Invoke(turns, new object[] { 1 }); // 해결한 카드의 다음 턴 행동력 한 점 감소 예약
        } // 개별 카드 정산 종료
        Assert.AreEqual(3, points.CurrentActionPoints); // 예약이 현재 턴 행동력에 미리 적용되지 않는지 확인
        Assert.AreEqual(3, points.MaxActionPoints); // 예약이 현재 턴 상한에 미리 적용되지 않는지 확인
        if (bossTransition) // 적 턴을 생략할 실제 보스 강제 전환 선택
        { // 보스 전환 경로 시작
            turns.QueueBossPhaseTransition(); // 다음 아군 턴을 강제로 예약
            Assert.IsTrue(turns.CompleteBossPhaseTransition()); // 적 턴을 건너뛴 진짜 다음 아군 턴 확인
        } // 보스 전환 경로 종료
        else // 일반 아군 및 적 턴 진행 선택
        { // 일반 라운드 경로 시작
            Assert.IsTrue(turns.EndPlayerTurn()); // 현재 아군 턴과 현재 행동력 정리
            Assert.AreEqual(3, points.MaxActionPoints); // 적 턴에는 원래 행동력 상한 유지 확인
            Assert.IsTrue(turns.CompleteEnemyTurn()); // 다음 아군 행동력 회복 경로 진입
        } // 일반 라운드 경로 종료
        Assert.AreEqual(expected, points.CurrentActionPoints); // 다음 실제 아군 턴의 현재 행동력 차감량 확인
        Assert.AreEqual(expected, points.MaxActionPoints); // 임시 상한의 동일 차감과 영 하한 확인
        points.Spend(expected); // 사용 후 같은 턴 회복 검사용 행동력 소모
        points.Restore(); // 표준 회복도 줄어든 상한을 따르는지 확인
        Assert.AreEqual(expected, points.CurrentActionPoints); // 같은 턴의 회복으로 패널티 우회 방지 확인
        Assert.IsTrue(turns.EndPlayerTurn() && turns.CompleteEnemyTurn()); // 패널티 턴을 마치고 두 번째 아군 턴 진입
        Assert.AreEqual(3, points.MaxActionPoints); // 한 턴 뒤 원래 행동력 상한 복원 확인
        Assert.AreEqual(3, points.CurrentActionPoints); // 한 턴 뒤 원래 현재 행동력 회복 확인
    } // 행동력 패널티 검사 종료

    private BattleEnemyAction Attack(BattleStatusEffectType effect = BattleStatusEffectType.None) // 실제 직접 적 공격과 확정 부가 상태 준비
    { // 직접 공격 준비 시작
        int chance = effect == BattleStatusEffectType.None ? 0 : 100; // 지정한 부가 상태에만 항상 발동할 확률
        return new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, 8, effect, 1, 1, EnemyTargetRule.FirstLiving, "검사", 1, 1, 1, 0, chance, 1, () => 0); // 기존 적 공격 실행기로 실제 명중과 피해 발생
    } // 직접 공격 준비 종료
} // 전투 규칙 검사 종료
