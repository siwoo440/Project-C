using System.Collections; // 실제 카드 선택과 정산 프레임 대기
using System.Linq; // 공용 손패와 실제 적 상태 비교
using NUnit.Framework; // 전투 결과와 공유 자원 검증
using UnityEngine.TestTools; // 실제 플레이 모드 검사 전환
using static WinterfallSwordCombatTests; // 기존 게임 입력으로 전투하는 검사 환경

public sealed class WinterfallCompletedRitualTests // 실제 완성된 의식의 공유 자원과 적 약화 확인
{ // 완성된 의식의 전투 검사 시작
    [UnityTest] // 실제 손패 입력에서 공용 드로우와 상태 적용의 원본 확인
    public IEnumerator DrawsThreeOnceAndWeakensOnlyAvailableEnemies() // 적 수와 관계없는 공유 카드와 약화 검사
    { // 실제 전투 입력 시작
        yield return new EnterPlayMode(); // 정상 게임 플레이 환경 진입
        yield return VerifyRitual(); // 실제 UI, 카드 정산과 수치 확인
        yield return new ExitPlayMode(); // 독립 전투 검사 종료
    } // 실제 전투 입력 종료

    private static IEnumerator VerifyRitual() // 카드의 공용 드로우 및 약화 효과 검증
    { // 실제 정산 검증 시작
        using var fight = new Fight("COMPLETED_RITUAL", "THRUST", "THRUST", "THRUST"); // 실제 보물 카드와 드로우용 카드 세 장
        var ritual = fight.View("COMPLETED_RITUAL").RuntimeCard; // 현재 손패에 있는 전설 펜타클 원본
        var toDraw = fight.Deck.Hand.Where(card => card != ritual).ToArray(); // 공용 손패에 다시 뽑힐 같은 실물 세 장
        foreach (var card in toDraw) // 세 드로우의 실제 공급 구성
        { // 버린 더미의 실제 카드 준비 시작
            Assert.True(fight.Deck.DiscardCard(card)); // 사용 카드와 구분되는 실제 카드 버림
        } // 버린 더미의 실제 카드 준비 종료
        Assert.That(fight.Deck.HandCount, Is.EqualTo(1)); // 완성된 의식만 남고 세 장을 뽑을 수 있는지 확인
        var immune = fight.Enemies[2]; // 정상 상태 면역을 유지할 적
        var fallen = fight.Enemies[3]; // 전투에 약화를 받지 않을 사망 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 원래 상태 면역 효과 준비
        fallen.TakeDamage(fallen.MaxHealth * 3, BattleDamageType.None, fight.Allies[0]); // 실제 전투에서 쓰러뜨려 대상 후보에서 제외
        Assert.True(fallen.IsDead); // 죽은 대상을 가진 검사 환경 확인
        int before = fight.Points.CurrentActionPoints; // 실제 카드 사용 전 공용 행동력
        yield return fight.Use("COMPLETED_RITUAL"); // 실제 손패 클릭으로 적 전체 의식 발동
        Assert.That(fight.Points.CurrentActionPoints, Is.EqualTo(before - 6 + 3)); // 비용 이후 적 수와 독립인 한 번의 공유 행동력
        CollectionAssert.AreEquivalent(toDraw, fight.Deck.Hand); // 사용 원본을 제외한 실제 세 카드만 공유 손패로 이동
        foreach (var target in fight.Enemies.Take(2)) // 같은 의식에서 약화되는 생존 적 둘 확인
        { // 서로 다른 적의 상태 확인 시작
            var effect = target.GetStatusEffect(BattleStatusEffectType.AttackPowerDown); // 다른 종류의 상태와 구분할 실제 약화
            Assert.That(effect?.EffectiveValue, Is.EqualTo(1)); // 각각의 공격력 감소량 1 확인
            Assert.That(effect?.RemainingTurns, Is.EqualTo(2)); // 다음 실제 두 적 턴의 유지 시간 확인
        } // 서로 다른 적의 상태 확인 종료
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 원래 상태 면역을 존중하는지 확인
        Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 사망 적에게 상태를 추가하지 않음 확인
    } // 실제 정산 검증 종료
} // 완성된 의식의 전투 검사 종료
