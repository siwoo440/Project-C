using System.Collections; // 실제 전투 입력과 카드 연출 대기
using NUnit.Framework; // 기존 방어 및 명중 수치 검증
using UnityEngine.TestTools; // 실제 Unity 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 공용 카드 화면과 전투 턴 구성

public sealed class WinterfallFoundationalShieldCombatTests // 기본 방패 세 장의 기존 게임 입력 검증
{ // 방패 카드의 실제 사용 검사 시작
    [UnityTest] // 실제 손패에서 세 카드 사용을 순서대로 확인
    public IEnumerator NewShieldCardsUseNormalDefenseEvasionAndPhysicalDamage() // 비용과 방어도 및 회피의 실제 절차 확인
    { // 실제 전투 씬 입력 시작
        yield return new EnterPlayMode(); // 런타임에서 사용할 실제 카드 UI 경로 활성화
        yield return UseNewShields(); // 원본 카드 세 장의 해상도 독립적인 실제 입력
        yield return new ExitPlayMode(); // 검증용 실제 Unity 플레이 상태 종료
    } // 실제 전투 씬 입력 종료

    private static IEnumerator UseNewShields() // 실제 카드 원본의 회피와 방어도 및 피해 적용
    { // 비용과 전투 정산 검증 시작
        using var fight = new Fight("BLOCK", "PARRY", "PUSH_BLOCK"); // 보상으로 획득 가능한 실제 세 카드 준비
        var caster = fight.Allies[0]; // 원본 방패 카드의 실제 소유자
        var enemy = fight.Enemies[0]; // 실제 밀어 막기의 지정 적
        int ap = fight.Points.CurrentActionPoints; // 사용 전에 확인한 실제 공유 행동력
        int enemyHealth = enemy.CurrentHealth; // 타격 전의 실제 적 체력
        yield return fight.Use("BLOCK"); // 행동력 없는 막기를 정상 손패에서 사용
        Assert.That(fight.Points.CurrentActionPoints, Is.EqualTo(ap)); // 비용 0의 실제 행동력 유지
        Assert.That(caster.CurrentBlock, Is.EqualTo(4)); // 첫 방어도 4의 실제 적용
        yield return fight.Use("PARRY"); // 자신을 대상으로 회피와 방어 확보
        Assert.That(fight.Points.CurrentActionPoints, Is.EqualTo(ap - 2)); // 흘려내기의 정상 비용 2 차감
        Assert.That(caster.CurrentBlock, Is.EqualTo(10)); // 첫 방어도에 실제 흘려내기 6 합산
        var evasion = caster.GetStatusEffect(BattleStatusEffectType.Evasion); // 기존 일반 회피 상태의 실제 적용
        Assert.That(evasion?.EffectiveValue, Is.EqualTo(35)); // 기존 회피 계산에 들어갈 실제 35%p
        Assert.That(BattleHitChance.GetPercent(enemy, caster), Is.EqualTo(65)); // 목표의 조건에 따른 실제 적 명중률 반영
        yield return fight.Use("PUSH_BLOCK", 0); // 실제 지정 적을 클릭해 타격과 방어 발동
        Assert.That(fight.Points.CurrentActionPoints, Is.EqualTo(ap - 3)); // 마지막 방패 카드의 정상 비용 1 차감
        Assert.That(enemy.CurrentHealth, Is.EqualTo(enemyHealth - 3)); // 방어력이 없는 검사 적에 대한 물리 피해 3
        Assert.That(caster.CurrentBlock, Is.EqualTo(14)); // 사용 소유자가 실제로 얻은 추가 방어도 4
        Assert.True(fight.Turns.EndPlayerTurn()); // 실제 아군 차례 끝에 적 시작
        Assert.That(caster.GetStatusEffect(BattleStatusEffectType.Evasion)?.RemainingTurns, Is.EqualTo(1)); // 적 차례 동안 아직 회피 유지
        Assert.True(fight.Turns.CompleteEnemyTurn()); // 실제 적 턴 종료와 다음 아군 차례 진행
        Assert.IsNull(caster.GetStatusEffect(BattleStatusEffectType.Evasion)); // 흘려내기의 원래 한 적 턴 경과 후 삭제
    } // 비용과 전투 정산 검증 종료
} // 방패 카드의 실제 사용 검사 종료
