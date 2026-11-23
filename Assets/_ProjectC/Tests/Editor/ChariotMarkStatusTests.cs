using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
public sealed class ChariotMarkStatusTests // 전차 표식 상태 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void Mark_ExpiresOnEnemyTurnAndRespectsImmunity() // 표식 지속시간과 면역 확인
    { // 테스트 시작
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 전투 유닛 생성
        BattleStatusEffectProcessor processor = new BattleStatusEffectProcessor(); // 상태 처리기 생성
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, enemy.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 3, 1, 1)); // 표식 적용 확인
        Assert.IsTrue(enemy.HasDebuff); // 표식 디버프 분류 확인
        processor.ProcessPhaseStart(enemy, 1); // 적 차례 시작 상태 처리
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.ChariotMark)); // 한 턴 표식 만료 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, enemy.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1)); // 상태 면역 적용 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, enemy.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 3, 1, 1)); // 표식 면역 차단 확인
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.ChariotMark)); // 면역 대상 표식 없음 확인
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 테스트 종료
} // 테스트 클래스 종료
