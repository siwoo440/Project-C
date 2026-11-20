using System.Collections; // 실제 카드 입력 후 연출 대기
using NUnit.Framework; // 전투 규칙의 결과 비교
using UnityEngine.TestTools; // 플레이 모드 손패 입력 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 일반 전투의 실제 카드 검사 환경

public sealed class WinterfallAdvancedShieldCardTests // 방패 카드의 비용과 가산 방어도 및 피해 검사
{ // 방패 카드 검사 시작
    [UnityTest] // 균형 잡기와 방어 전진 그리고 파열 방패의 실전 확인
    public IEnumerator BalanceDiscountsOnlyOnePaidShieldAndRendingUsesTheCurrentBlock() // 할인 소비 및 다른 방패 카드의 정확한 수치 검증
    { // 손패 소비 검사 시작
        yield return new EnterPlayMode(); // 전투 카드와 소유자 상태가 있는 플레이 모드 진입
        yield return VerifyBlockAndDiscount(); // 카드 이동과 방어도 산출을 포함한 실제 카드 입력
        yield return new ExitPlayMode(); // 전투 상태를 정리한 뒤 에디터로 복귀
    } // 손패 소비 검사 종료

    private static IEnumerator VerifyBlockAndDiscount() // 네 카드의 실제 비용과 방어도 및 피해 확인
    { // 실제 전투 카드 검사 시작
        using var fight = new Fight("BALANCE_GUARD", "HOLD_BACK", "ADVANCING_GUARD", "RENDING_SHIELD"); // 보물에서 얻는 방패 카드 네 종 준비
        var actor = fight.Allies[0]; // 네 카드의 소유자 조회
        int actionPoints = fight.Points.CurrentActionPoints; // 십진 행동력 상한 대신 실제 초기값 보관
        yield return fight.Use("BALANCE_GUARD"); // 비용 없는 균형 잡기로 방어도와 할인 준비
        Assert.AreEqual(3, actor.CurrentBlock); // 기본 방어도 세 점 확인
        Assert.AreEqual(actionPoints, fight.Points.CurrentActionPoints); // 비용 없는 첫 카드의 행동력 보존
        Assert.AreEqual(0, fight.View("HOLD_BACK").RuntimeCard.ApCost); // 다음 유료 소유 방패의 비용 영 확인
        yield return fight.Use("HOLD_BACK", 0); // 다음 방패 카드에만 비용 할인 소모
        Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.ShieldBalance)); // 사용한 할인 예약 제거 확인
        Assert.AreEqual(actionPoints, fight.Points.CurrentActionPoints); // 할인한 방패 카드의 비용 영 지불 확인
        Assert.AreEqual(2, fight.View("ADVANCING_GUARD").RuntimeCard.ApCost); // 두 번째 유료 방패에 원래 비용 표시
        yield return fight.Use("ADVANCING_GUARD", 1); // 공격과 소유자의 방어 지급 실행
        Assert.AreEqual(13, actor.CurrentBlock); // 균형의 세 점에 방어 전진 열 점 합산
        Assert.AreEqual(192, fight.Enemies[1].CurrentHealth); // 방어 전진의 물리 피해 여덟 확인
        yield return fight.Use("RENDING_SHIELD", 2); // 현재 홀수 방어도를 쓰는 파열 방패 입력
        Assert.AreEqual(194, fight.Enemies[2].CurrentHealth); // 방어도 열세의 절반을 내린 여섯 피해 확인
        Assert.AreEqual(13, actor.CurrentBlock); // 피해의 기준으로 사용한 방어도 보존
        Assert.AreEqual(actionPoints - 4, fight.Points.CurrentActionPoints); // 두 유료 공격의 행동력 각 두 점 확인
    } // 실제 전투 카드 검사 종료

    [UnityTest] // 자기 대상으로 도발을 사용할 때의 방어 확인
    public IEnumerator TauntAcceptsItsOwnCasterAndAddsFourPhysicalDefense() // 실제 손패 도발의 시전자와 강화 수치 검증
    { // 도발 카드 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 UI 입력 환경 생성
        yield return VerifyTauntDefense(); // 자기 대상과 반영된 물리 방어 확인
        yield return new ExitPlayMode(); // 도발 검사의 전투 상태 정리
    } // 도발 카드 검사 종료

    private static IEnumerator VerifyTauntDefense() // 기존 적 대상 유도와 다른 자기 대상 도발 검사
    { // 도발의 실제 카드 사용 검사 시작
        using var fight = new Fight("TAUNT"); // 실제 보물에서 등록한 도발 카드 구성
        var actor = fight.Allies[0]; // 도발 카드 소유자 확인
        var ally = fight.Allies[1]; // 방어 수치를 바꾸지 않는 다른 아군
        int baseDefense = actor.PhysicalDefense; // 소유자의 원래 물리 방어값 보관
        int allyDefense = ally.PhysicalDefense; // 비교 아군의 원래 물리 방어값 보관
        yield return fight.Use("TAUNT"); // 적 선택 없이 소유자에게 도발 적용
        Assert.AreEqual(baseDefense + 4, actor.PhysicalDefense); // 소유자 한 명의 물리 방어력 네 점 증가
        Assert.AreEqual(allyDefense, ally.PhysicalDefense); // 다른 아군의 방어력 보존
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 도발이 적용되는 첫 적 턴 진입
        Assert.AreEqual(baseDefense + 4, actor.PhysicalDefense); // 적 공격 차례에 남아 있는 방어력 네 점
    } // 도발의 실제 카드 사용 검사 종료
} // 방패 카드 검사 종료
