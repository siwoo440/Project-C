using System.Collections; // 실제 손패 카드 선택 검사 코루틴
using System.Linq; // 아군 화면과 상태 검사 검색
using NUnit.Framework; // 전투 수치와 정상 상태 확인
using UnityEngine.TestTools; // 실제 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 실제 공유 덱과 아군 전투 구성

public sealed class WinterfallPentacleDistributionTests // 분배의 실제 전투 강화와 약화 검사
{ // 분배 검사 시작
    [UnityTest] // 대상 지정과 동일 전투의 재시전 및 전투 종료 확인
    public IEnumerator DistributionUsesSelectedAllyNonstackingBattleDurationAndNativeStatusRules() // 실제 공격력과 물리 방어 및 일반 상태 처리 비교
    { // 분배 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 상태와 카드 대상 선택 실행
        yield return CheckDistribution(); // 생성과 수명 및 상태 면역과 종료 확인
        yield return new ExitPlayMode(); // 실제 격리 전투 종료
    } // 분배 플레이 모드 검사 종료

    private static IEnumerator CheckDistribution() // 등록 원본과 세 명 아군의 실제 상태 변화
    { // 대상 및 면역 비교 준비 시작
        using var fight = new Fight("POWER_DISTRIBUTION", "POWER_DISTRIBUTION", "POWER_DISTRIBUTION"); // 실제 분배 원본 세 장의 독립 전투
        var caster = fight.Allies[0]; // 원본을 사용한 실제 아군
        var target = fight.Allies[1]; // 선택할 실제 동료 한 명
        var immune = fight.Allies[2]; // 일반 약화만 면역인 비교 아군
        target.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 20, 8, 1); // 감소 수치의 영 하한 영향을 제한할 일반 방어 강화
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 8, 1); // 새로운 표준 약화를 막는 실제 상태 면역
        int targetAttack = target.AttackPowerBonus; // 선택된 아군의 원래 공격 증가
        int defense = target.PhysicalDefense; // 별도 방어 강화 이후의 원래 물리 방어
        int resistance = target.MagicalResistance; // 물리 약화와 다른 마법 저항 상태
        int casterAttack = caster.AttackPowerBonus; // 선택되지 않은 실제 시전자의 원래 수치
        int immuneDefense = immune.PhysicalDefense; // 면역 대상을 판별할 원래 물리 방어
        yield return Cast(fight, target); // 정상 카드 선택으로 한 아군에게 분배 사용
        Assert.AreEqual(targetAttack + 8, target.AttackPowerBonus); // 실제 일반 공격력에 등록 증가량 반영
        Assert.AreEqual(defense - 4, target.PhysicalDefense); // 실제 물리 방어에만 등록 비용 반영
        Assert.AreEqual(resistance, target.MagicalResistance); // 마법 저항은 원래 수치 유지
        Assert.AreEqual(casterAttack, caster.AttackPowerBonus); // 선택하지 않은 시전자에게 분배 효과 없음
        yield return Cast(fight, target); // 같은 아군에게 실제 두 번째 분배 사용
        var attack = target.GetStatusEffect(BattleStatusEffectType.DistributionAttackUp); // 실제 독립 분배 공격 강화 조회
        var armorLoss = target.GetStatusEffect(BattleStatusEffectType.DistributionPhysicalDefenseDown); // 실제 독립 물리 방어 약화 조회
        Assert.AreEqual(1, attack.StackCount); // 같은 강화가 한 번만 유지되는 중첩 제한
        Assert.AreEqual(1, armorLoss.StackCount); // 같은 약화도 별개 중첩을 만들지 않는 규칙
        Assert.AreEqual(targetAttack + 8, target.AttackPowerBonus); // 두 번째 시전 뒤에도 실제 증가량 한 번
        Assert.AreEqual(defense - 4, target.PhysicalDefense); // 두 번째 시전 뒤 실제 물리 감소 한 번
        yield return Cast(fight, immune); // 일반 면역이 있는 다른 아군에게 실제 카드 사용
        Assert.IsNotNull(immune.GetStatusEffect(BattleStatusEffectType.DistributionAttackUp)); // 일반 약화 면역에 관계없는 강화 적용
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.DistributionPhysicalDefenseDown)); // 기존 면역이 실제 약화 부여만 차단
        Assert.AreEqual(immuneDefense, immune.PhysicalDefense); // 다른 아군의 원래 물리 방어 보존
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, caster.ApplyBlessingCopy(attack, target)); // 일반 강화 복사로 분배 수치 이동
        Assert.AreEqual(casterAttack + 8, caster.AttackPowerBonus); // 전투 동안 복사한 독립 공격 수치 확인
        Assert.IsNull(caster.GetStatusEffect(BattleStatusEffectType.DistributionPhysicalDefenseDown)); // 강화 복사에 분배의 약화는 전달하지 않는 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴 종료 및 적 진영 시작
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 적 턴 종료 및 다음 아군 준비
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 아군 턴 종료
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 여러 턴의 상태 감소 경로 처리
        Assert.AreSame(attack, target.GetStatusEffect(BattleStatusEffectType.DistributionAttackUp)); // 전투 중 턴 감소에도 유지되는 원래 강화
        Assert.AreSame(armorLoss, target.GetStatusEffect(BattleStatusEffectType.DistributionPhysicalDefenseDown)); // 전투 중 턴 감소에도 유지되는 원래 약화
        Assert.IsTrue(attack.LastsUntilBattleEnd && armorLoss.LastsUntilBattleEnd); // 전투 종료까지의 명시된 수명 표기
        var processor = new BattleStatusEffectProcessor(); // 원래 정화의 일반 전투 처리기
        Assert.AreEqual(1, processor.CleanseDebuffs(target, 1).Count); // 분배의 일반 디버프만 해제할 수 있는 확인
        Assert.AreEqual(defense, target.PhysicalDefense); // 정화 뒤 물리 방어 복원
        Assert.AreEqual(targetAttack + 8, target.AttackPowerBonus); // 정화로 공격 강화를 지우지 않는 분류
        Assert.IsTrue(fight.Turns.TryEscape()); // 현재 조우의 실제 전투 종료
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.DistributionAttackUp)); // 전투 결과 확정 때 선택 대상의 공격 강화 해제
        Assert.IsNull(caster.GetStatusEffect(BattleStatusEffectType.DistributionAttackUp)); // 복사된 강화도 실제 종료 때 해제
    } // 대상 및 면역 비교 준비 종료

    private static IEnumerator Cast(Fight fight, BattleUnitRuntime recipient) // 원본 손패와 선택 아군을 클릭하는 실제 카드 사용
    { // 아군 화면 대상 찾기 시작
        var view = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(item => item.RuntimeUnit == recipient); // 현재 살아 있는 동료의 실제 전투 대상 화면
        var source = fight.View("POWER_DISTRIBUTION").RuntimeCard.SourceData; // 해당 실제 손패의 등록 카드 원본
        return WinterfallCommonCardExpansionTests.Play(source, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 정상 선택과 실제 비용 및 버림 검사
    } // 아군 화면 대상 찾기 종료
} // 분배 검사 종료
