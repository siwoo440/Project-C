using System.Collections; // 실제 전투 연출과 턴 전환 대기
using System.Linq; // 실제 출전 대상과 생존 적 검색
using NUnit.Framework; // 성배 효과 수치와 실제 결과 검사
using UnityEngine.TestTools; // 플레이 모드의 입력 검사 실행
using static WinterfallSwordCombatTests; // 실제 손패와 카드 입력의 공용 환경 재사용

public sealed class WinterfallSalvationChaliceCombatTests // 저항과 회복과 디버프 정산 회귀 검사
{ // 성배 범위 카드 검사 시작
    [UnityTest] // 방벽과 회복 카드의 실제 연속 사용 검사
    public IEnumerator BulwarkAndMiracleResolveForEachSurvivingAlly() // 아군별 방어도와 저항 및 턴 회복 가산 확인
    { // 전투 입력 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 입력이 가능한 모드로 진입
        yield return VerifyAllyEffects(); // 실전 카드의 방어와 회복 묶음 확인
        yield return new ExitPlayMode(); // 격리 전투 검사 종료
    } // 전투 입력 검사 종료

    private static IEnumerator VerifyAllyEffects() // 실제 시트 원본과 각 대상의 효과 비교
    { // 생존 아군의 실제 카드 검증 시작
        using var fight = new Fight("CONSECRATED_BULWARK", "MIRACLE_ECHO", "SALVATION_CIRCLE", "SALVATION_CIRCLE"); // 정상 카드 네 개가 든 실제 전투의 첫 손패 구성
        var owner = fight.Allies[0]; // 부상 상태로 만들 첫 번째 시전자
        var ally = fight.Allies[1]; // 같은 광역 회복을 받을 다른 생존 아군
        var fallen = fight.Allies[2]; // 광역 강화와 회복을 받지 못할 세 번째 아군
        fallen.ApplyPersistentHealth(0); // 기존 전투불능 상태 준비
        Assert.IsTrue(owner.TrySpendHealth(30) && ally.TrySpendHealth(30)); // 두 아군 모두 회복을 받을 체력 준비
        int startOwnerHealth = owner.CurrentHealth; // 시전자의 회복 전 기준 체력
        int startAllyHealth = ally.CurrentHealth; // 다른 아군의 회복 전 기준 체력
        yield return fight.Use("CONSECRATED_BULWARK"); // 방벽의 아군별 저항과 후속 방어도 동시 적용
        Assert.AreEqual(12, owner.CurrentBlock); // 시전자를 포함한 개별 방어도 확인
        Assert.AreEqual(12, ally.CurrentBlock); // 갈라서 주지 않는 원본 방어도 확인
        Assert.AreEqual(0, fallen.CurrentBlock); // 전투불능 아군에 광역 방어도 미지급 확인
        Assert.AreEqual(10, ally.GetStatusEffect(BattleStatusEffectType.ResistanceStance).EffectiveValue); // 별도의 마법 피해 저항 수치 확인
        yield return fight.Use("MIRACLE_ECHO"); // 이번 아군 턴의 성배 회복 강화를 실제로 사용
        owner.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1); // 광역 회복이 해제할 공포 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1); // 두 번째 대상 공포도 함께 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 3, 1); // 공포 이외 상태를 보존하는지 비교
        yield return fight.Use("SALVATION_CIRCLE"); // 생존 아군 전체 공포 해제와 직접 회복
        Assert.AreEqual(startOwnerHealth + 14, owner.CurrentHealth); // 기본 회복 팔과 잔향 육을 첫 대상에 적용
        Assert.AreEqual(startAllyHealth + 14, ally.CurrentHealth); // 기본 회복과 시전자 잔향을 둘째 대상에 적용
        Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.Fear)); // 시전자의 공포 제거 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Fear)); // 동료의 공포 제거 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Bleed)); // 관계없는 출혈은 남는지 확인
        Assert.IsTrue(fallen.IsDead); // 일반 회복이 쓰러진 아군을 부활시키지 않는지 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 시전한 현재 아군 턴 종료
        Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.MiracleEcho)); // 실제 시전 턴에서만 회복 강화를 사용
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 저항의 첫 실제 적 턴 종료
        Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.ResistanceStance).RemainingTurns); // 마법 저항이 다음 적 턴까지 유지되는지 확인
        int before = owner.CurrentHealth; // 새 턴의 기본 회복량 기준 체력
        yield return fight.Use("SALVATION_CIRCLE"); // 새 아군 턴에 두 번째 정상 회복 카드 입력
        Assert.AreEqual(before + 8, owner.CurrentHealth); // 새 아군 턴에는 원래 회복량만 적용 확인
    } // 생존 아군의 실제 카드 검증 종료

    [UnityTest] // 광역 피해와 회복 및 적 강화 제거의 실제 카드 검증
    public IEnumerator LightHealsBeforeVictoryAndJudgmentRemovesOneBuff() // 마지막 적 타격과 회복 결과 및 강화 해제 개수 확인
    { // 두 마법 카드의 실제 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 연결이 되는 모드로 진입
        yield return VerifyAttackEffects(); // 명중한 대상과 아군의 최종 체력 확인
        yield return new ExitPlayMode(); // 전투 입력 검사 종료
    } // 두 마법 카드의 실제 검사 종료

    private static IEnumerator VerifyAttackEffects() // 적 마법 피해와 생존 동료의 회복 순서 확인
    { // 효과 비교 시작
        using var fight = new Fight("JUDGMENT_WATER", "DOWNPOUR_OF_LIGHT"); // 원본 성수와 폭우 둘로 구성한 실제 손패
        var first = fight.Enemies[0]; // 생존 상태에서 강화 해제를 확인할 적
        first.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 2, 1); // 첫 번째 해제 후보 강화
        first.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 2, 2, 1); // 남을 두 번째 강화
        first.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 강화 해제와 구분할 디버프
        int raw = 0; // 피해 이벤트의 마법 원본 저장값
        first.DamageTaken += (_, result) => raw = result.RawDamage; // 실제 적의 명중과 피해 이벤트에 연결
        yield return fight.Use("JUDGMENT_WATER", 0); // 강화 두 개가 붙은 적을 실제로 클릭
        Assert.AreEqual(18, raw); // 시트의 명중 원본 피해 확인
        Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 실제로 먼저 붙은 강화 해제 확인
        Assert.IsNotNull(first.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseUp)); // 또 다른 강화 한 개 보존 확인
        Assert.IsNotNull(first.GetStatusEffect(BattleStatusEffectType.Poison)); // 기존 디버프는 해제하지 않는지 확인
        foreach (var foe in fight.Enemies) // 마지막 타격 전에 실제 적을 낮은 체력으로 조정
        { // 광역 타격의 적별 조건 준비 시작
            foe.TakeDamage(foe.CurrentHealth - 1, BattleDamageType.None); // 적의 실제 피해 경로에서 체력 한 점 남기기
            Assert.AreEqual(1, foe.CurrentHealth); // 광역 시전 전에 각 적의 체력 확인
        } // 광역 타격의 적별 조건 준비 종료
        var wounded = fight.Allies[1]; // 마지막 타격 뒤 회복 여부를 확인할 동료
        Assert.IsTrue(wounded.TrySpendHealth(20)); // 실제 최대 회복을 수용할 체력 차이 준비
        int before = wounded.CurrentHealth; // 승리 전 아군 체력 기준
        yield return fight.Use("DOWNPOUR_OF_LIGHT"); // 실제 광역 마법과 아군 후속 회복 실행
        Assert.IsTrue(fight.Enemies.All(foe => foe.IsDead)); // 공격 대상 전체의 실제 전투불능 확인
        Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 두 효과가 끝난 후 실제 전투 승리 확인
        Assert.AreEqual(before + 12, wounded.CurrentHealth); // 마지막 적이 쓰러져도 정상 아군 회복 확인
    } // 효과 비교 종료
} // 성배 범위 카드 검사 종료
