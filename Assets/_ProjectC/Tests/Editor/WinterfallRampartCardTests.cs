using System.Collections; // 실제 카드 입력과 연출 대기
using System.Linq; // 생존 아군과 적의 효과 수치 확인
using NUnit.Framework; // 전투 규칙의 정수 결과 비교
using UnityEngine.TestTools; // 실제 플레이 모드 전투 진입
using Fight = WinterfallSwordCombatTests.Fight; // 기존 일반 전투 손패 검증 환경

public sealed class WinterfallRampartCardTests // 전열 보호와 성벽 및 방패 파동의 실제 카드 흐름 검사
{ // 신규 방패 카드 검사 시작
    [UnityTest] // 아군별 방어와 추가 비용 및 방패 파동의 수치 확인
    public IEnumerator RampartAndFrontlineGrantBlockIndividuallyAndWaveKeepsIt() // 다중 아군 및 적 대상의 정산 확인
    { // 실제 플레이 검사 시작
        yield return new EnterPlayMode(); // 정상 카드 핸들러가 작동하는 플레이 모드 진입
        yield return CheckRampartAndWave(); // 네 방패 카드의 실제 사용과 결과 확인
        yield return new ExitPlayMode(); // 게임 상태를 마친 뒤 에디터로 복귀
    } // 실제 플레이 검사 종료

    private static IEnumerator CheckRampartAndWave() // 방어도와 피해의 정상 카드 정산 검사
    { // 카드 정산 과정 확인 시작
        using var fight = new Fight("FRONTLINE_PROTECTION", "RAMPART_STANCE", "SHIELD_WAVE", "SUPPRESSION_STRIKE"); // 실제 보물 카드 네 종류로 손패 준비
        var owner = fight.Allies[0]; // 네 카드의 소유자 조회
        int points = fight.Points.CurrentActionPoints; // 실제 초기 행동력 수치 보관
        var rampart = fight.View("RAMPART_STANCE").RuntimeCard; // 추가 행동력 대가를 지불하는 성벽 자세
        Assert.AreEqual(3, rampart.SourceData.ApCost); // 원본 시트의 기본 비용 세 점 확인
        Assert.AreEqual(4, rampart.ApCost); // 필수 추가 비용을 포함한 실제 행동력 네 점 확인
        yield return fight.Use("FRONTLINE_PROTECTION"); // 아군 전체에게 방어와 출혈 저항을 주는 전열 보호 사용
        Assert.IsTrue(fight.Allies.All(unit => unit.CurrentBlock == 7)); // 생존 아군 각자에게 방어도 일곱 지급 확인
        Assert.IsTrue(fight.Allies.All(unit => unit.GetStatusEffect(BattleStatusEffectType.BleedResistance)?.EffectiveValue == 20)); // 생존 아군 각자의 출혈 저항 스무 퍼센트 확인
        yield return fight.Use("RAMPART_STANCE"); // 모든 생존 아군의 방어도를 추가하는 성벽 자세 사용
        Assert.AreEqual(points - 6, fight.Points.CurrentActionPoints); // 두 카드의 실제 총 행동력 여섯 확인
        Assert.IsTrue(fight.Allies.All(unit => unit.CurrentBlock == 22)); // 각 생존 아군의 방어도 일곱과 열다섯 합산 확인
        yield return fight.Use("SHIELD_WAVE"); // 현재 방어도의 절반을 기본 피해로 모든 적에게 적용
        Assert.IsTrue(fight.Enemies.All(unit => unit.CurrentHealth == 189)); // 모든 적의 방어도 절반에 해당하는 피해 열한 점 확인
        Assert.AreEqual(22, owner.CurrentBlock); // 기준으로 쓴 소유자의 방어도 보존 확인
        yield return fight.Use("SUPPRESSION_STRIKE", 1); // 선택한 적 한 명에게만 억제 타격 사용
        Assert.AreEqual(180, fight.Enemies[1].CurrentHealth); // 선택한 적에만 추가 피해 아홉 적용 확인
        Assert.AreEqual(3, fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)?.EffectiveValue); // 명중한 적의 공격력 세 점 감소 확인
        Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 다른 생존 적의 공격력 약화 제외 확인
    } // 카드 정산 과정 확인 종료
} // 신규 방패 카드 검사 종료
