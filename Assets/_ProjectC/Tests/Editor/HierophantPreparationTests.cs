using System.Collections.Generic; // 빈 카드 목록 사용
using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
public sealed class HierophantPreparationTests // 교황 사전 방어 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void Preparation_ReducesFirstEnemyAttackAndMatchesPreview() // 첫 적 공격 감소와 예고 일치 확인
    { // 테스트 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 유닛 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 유닛 생성
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.GuardPreparation, 3, 1, 1)); // 방어 준비 적용 확인
        ally.TakeDamage(2, BattleDamageType.None); // 공격자가 없는 환경 피해 적용
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.GuardPreparation)); // 환경 피해에 준비 유지 확인
        BattleDamageResult preview = ally.PreviewDamage(10, BattleDamageType.Physical, enemy); // 첫 적 공격 예상 피해 조회
        Assert.AreEqual(7, preview.AppliedDamage); // 준비 방어도 적용 예상 피해 확인
        Assert.AreEqual(3, preview.AbsorbedDamage); // 준비 방어도의 예상 흡수량 확인
        Assert.AreEqual(0, ally.CurrentBlock); // 예상 단계의 방어도 미획득 확인
        BattleDamageResult first = ally.TakeDamage(10, BattleDamageType.Physical, enemy); // 첫 적 공격 적용
        Assert.AreEqual(preview.AppliedDamage, first.AppliedDamage); // 예상 피해와 실제 피해 일치 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.GuardPreparation)); // 발동 후 준비 제거 확인
        BattleDamageResult second = ally.TakeDamage(10, BattleDamageType.Physical, enemy); // 두 번째 적 공격 적용
        Assert.AreEqual(10, second.AppliedDamage); // 두 번째 공격에 준비 미적용 확인
        Object.DestroyImmediate(allyData); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void Preparation_ExpiresAtEndOfNextEnemyTurn() // 미사용 준비의 적 턴 종료 만료 확인
    { // 테스트 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 유닛 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 유닛 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), new[] { ally }, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { ally }, new[] { enemy }, 0, BattleType.Normal); // 턴 관리자 생성
        BattleStatusEffectController statuses = new BattleStatusEffectController(turns, new[] { ally }, new[] { enemy }, new BattleStatusEffectProcessor()); // 상태 처리 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.GuardPreparation, 3, 1, 1); // 방어 준비 적용
        Assert.IsTrue(turns.EndPlayerTurn()); // 적 턴 시작 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.GuardPreparation)); // 적 턴 중 준비 유지 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 적 턴 종료 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.GuardPreparation)); // 미사용 준비 만료 확인
        statuses.Dispose(); // 상태 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(deckData); // 덱 원본 정리
        Object.DestroyImmediate(allyData); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void PlannedAttacks_OnlyFirstPreviewIncludesPreparation() // 여러 공격 중 첫 공격만 방어 준비 예고 확인
    { // 테스트 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 유닛 생성
        BattleUnitRuntime firstEnemy = BattleUnitRuntime.CreateEnemy(enemyData); // 첫 적 유닛 생성
        BattleUnitRuntime secondEnemy = BattleUnitRuntime.CreateEnemy(enemyData); // 두 번째 적 유닛 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { firstEnemy, secondEnemy }, new[] { ally }); // 적 행동 관리자 생성
        Assert.AreEqual(2, actions.PrepareActions()); // 두 적의 공격 준비 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.GuardPreparation, 3, 1, 1); // 방어 준비 적용
        BattleEnemyAction firstAttack = actions.PlannedActions[0]; // 첫 예정 공격 조회
        BattleEnemyAction secondAttack = actions.PlannedActions[1]; // 두 번째 예정 공격 조회
        Assert.AreEqual(5, firstAttack.PreviewDamage().AppliedDamage); // 첫 공격의 준비 방어도 예고 확인
        Assert.AreEqual(8, secondAttack.PreviewDamage().AppliedDamage); // 두 번째 공격의 준비 미적용 예고 확인
        actions.ExecuteAction(firstAttack); // 첫 적 공격 실행
        Assert.AreEqual(8, secondAttack.PreviewDamage().AppliedDamage); // 준비 소모 후 두 번째 공격 예고 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(allyData); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 테스트 종료
    [Test] // 준비 방어도 잔여량 검사 지정
    public void PlannedAttacks_PreparationAddsBlockBeforeFirstDamage() // 준비 발동 후 남은 방어도 예고 확인
    { // 준비 방어도 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 유닛 생성
        BattleUnitRuntime firstEnemy = BattleUnitRuntime.CreateEnemy(enemyData); // 첫 적 생성
        BattleUnitRuntime secondEnemy = BattleUnitRuntime.CreateEnemy(enemyData); // 두 번째 적 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { firstEnemy, secondEnemy }, new[] { ally }); // 적 행동 관리자 생성
        Assert.AreEqual(2, actions.PrepareActions()); // 적 공격 준비 확인
        ally.GainBlock(6); // 기존 방어도 획득
        ally.ApplyStatusEffect(BattleStatusEffectType.GuardPreparation, 3, 1, 1); // 준비 방어도 설치
        BattleEnemyAction first = actions.PlannedActions[0]; // 첫 공격 조회
        BattleEnemyAction second = actions.PlannedActions[1]; // 두 번째 공격 조회
        Assert.AreEqual(0, first.PreviewDamage().AppliedDamage); // 첫 공격 완전 흡수 예고 확인
        Assert.AreEqual(7, second.PreviewDamage().AppliedDamage); // 준비 후 잔여 방어도 반영 확인
        Assert.AreEqual(6, ally.CurrentBlock); // 예고 시 기존 방어도 유지 확인
        actions.ExecuteAction(first); // 첫 공격 실행
        Assert.AreEqual(1, ally.CurrentBlock); // 첫 공격 후 잔여 방어도 확인
        Assert.AreEqual(7, second.PreviewDamage().AppliedDamage); // 실제 소모 후 예고 일치 확인
        Assert.AreEqual(7, actions.ExecuteAction(second).DamageResult.AppliedDamage); // 두 번째 실제 피해 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(allyData); // 아군 원본 정리
    } // 준비 방어도 검사 종료
} // 테스트 클래스 종료
