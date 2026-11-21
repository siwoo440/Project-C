using System.Collections; // 실제 전투 연출 대기 및 도메인 전환
using System.Collections.Generic; // 원래 피해 및 반향의 실제 적용 내역 수집
using NUnit.Framework; // 정상 카드와 저항 수치의 실제 경계 확인
using UnityEngine; // 플레이 모드 타이밍 및 기존 유물 자료 사용
using UnityEngine.TestTools; // 실제 전투 카드 정산의 플레이 모드 실행
using Fight = WinterfallSwordCombatTests.Fight; // 기존 손패와 실제 전투 카드 입력 재사용

public sealed class WinterfallWandOverloadAndPenetrationTests // 마력 과부하와 역류 및 비전 쇄도의 실제 정산 회귀 검사
{ // 지팡이 산술 검사 시작
    [UnityTest] // 고정·가변 피해와 실제 포스트 시전 정신력 비용의 공용 경계 확인
    public IEnumerator OverloadPaysAfterUseAndBoostsDirectWandsForCastingTurn() // 일반 지팡이와 X에서 반향을 제외한 피해 증가 검증
    { // 과부하 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 리소스와 상태 흐름이 작동하는 모드 시작
        yield return VerifyOverload(); // 비용, 반향, 고정·가변 피해 및 지속 턴 확인
        yield return new ExitPlayMode(); // 독립 전투 자료 정리 뒤 편집 모드 복귀
    } // 과부하 실제 전투 검사 종료

    private static IEnumerator VerifyOverload() // 원본 비용을 가리거나 다른 반향에 이전하지 않는 실제 카드 결과
    { // 과부하의 사용 시점과 후속 효과 확인 시작
        var run = RelicRunManager.EnsureInstance(); // 기존 정신 피해 보호 유물의 실제 보유 관리자
        var plate = Resources.Load<RelicData>("Relics/BlackAbyssPlate"); // 현재 탐사에서 획득할 실제 보호 유물
        Assert.IsNotNull(plate); // 실제 기존 유물의 원본 데이터 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, run.TryAcquire(plate)); // 직접 비용과 별개인 정신 피해 무효 기회 구성
        using (var fight = new Fight("MANA_OVERLOAD", "MAGIC_ECHO", "COLD_TOUCH", "MANA_RELEASE")) // 과부하가 적용할 실제 고정 및 X 원본 덱
        { // 정상 상태에서의 실제 과부하 증가 시작
            var owner = fight.Allies[0]; // 정신력 비용을 실제로 지불할 시전자
            Assert.IsTrue(owner.ApplyPersistentMental(50)); // 보호와 비용을 비교할 일반 정신 상태
            int beforeImpact = -1; // 카드 사용 신호 당시의 실제 정신
            fight.Controller.CardUsed += (card, _) => // 기존 충돌 시작에서 카드 상태와 정신력 대조
            { // 정신력 비용 시점 관찰 시작
                if (card.SourceData.CardId == "CRD_SHEET_MANA_OVERLOAD") // 실제 과부하만 원래 정신 상태를 기록
                { // 해당 카드의 비용 이전 기록 시작
                    beforeImpact = owner.CurrentMental; // 효과를 쓰기 시작할 때의 정상 비용 이전 값 보관
                } // 해당 카드의 비용 이전 기록 종료
            }; // 정신력 비용 시점 관찰 종료
            yield return fight.Use("MANA_OVERLOAD"); // 일반 행동력 세 점을 쓰고 현재 아군 턴에 버프 적용
            Assert.AreEqual(50, beforeImpact); // 원본 효과 적용 이후에 비용을 내는 카드임을 확인
            Assert.AreEqual(45, owner.CurrentMental); // 검은 심연판이 실제 정신력 비용을 무효화하지 않음 확인
            Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 정상 원본의 세 점 행동력 결제 확인
            Assert.AreEqual(50, owner.GetStatusEffect(BattleStatusEffectType.WandOverload)?.EffectiveValue); // 실제 활성 과부하의 피해 백분율 확인
            var protectedLoss = owner.ChangeMental(-4, BattleMentalChangeReason.DisruptReceived, fight.Enemies[0]); // 별도로 받을 실제 정신 피해 입력
            Assert.Zero(protectedLoss.AppliedDelta); // 비용 뒤에도 기존 무효화 기회가 실제로 남아 있음 확인
            var raw = new List<int>(); // 원래 마법 공격과 그 반향의 실제 피해 수치
            fight.Enemies[0].DamageTaken += (_, result) => raw.Add(result.RawDamage); // 대상에게 전달된 공용 원본 피해 이벤트 확인
            yield return fight.Use("MAGIC_ECHO"); // 현재 과부하와 함께 활성화되는 별도 반향 준비
            yield return fight.Use("COLD_TOUCH", 0); // 고정 피해 5를 과부하와 함께 지불해 실제 타격
            CollectionAssert.AreEqual(new[] { 7, 2 }, raw); // 원본의 50% 증가 내림과 반향의 별도 기본 반감 확인
            Assert.IsTrue(fight.Points.Spend(fight.Points.CurrentActionPoints - 3)); // 가변 카드가 지불할 실제 행동력을 세 점으로 고정
            yield return UseVariable(fight); // 실제 마력 해방의 남은 행동력 전액 지불 입력
            CollectionAssert.AreEqual(new[] { 7, 2, 31 }, raw); // 실제 가변 기본 피해 21에 과부하가 적용된 반내림 결과 확인
            Assert.Zero(fight.Points.CurrentActionPoints); // 원래 X 비용 전액을 한 번 지불 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 효과를 켰던 실제 아군 턴 종료
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.WandOverload)); // 남은 과부하 효과의 실제 시전 턴 만료 확인
        } // 정상 상태에서의 실제 과부하 증가 종료
        using var low = new Fight("MANA_OVERLOAD"); // 정신력 다섯 미만의 실제 카드 사용 허용 대조
        Assert.IsTrue(low.Allies[0].ApplyPersistentMental(2)); // 고정 비용보다 적은 실제 정신력 준비
        yield return low.Use("MANA_OVERLOAD"); // 최소 정신력을 별도 요구하지 않는 실제 사용
        Assert.Zero(low.Allies[0].CurrentMental); // 기존 영 하한에서 고정 비용을 제한한 결과 확인
        Assert.IsNotNull(low.Allies[0].GetStatusEffect(BattleStatusEffectType.WandOverload)); // 낮은 정신력에서도 정상 시전 상태 확인
        Assert.IsTrue(run.TryRemove(plate.RelicId)); // 다음 독립 검사에 임시로 추가한 기존 보호 유물 제거
    } // 과부하의 사용 시점과 후속 효과 확인 종료

    [UnityTest] // 전체 원본 타격 이후에만 걸리는 이번 턴 공격력 약화 검사
    public IEnumerator BackflowWeakensOnlyLaterAllyAttacksUntilTurnEnd() // 원래 광역 피해와 시전자만의 후속 약화 확인
    { // 역류 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 전체 적 받는 피해와 상태 정산 환경 시작
        yield return VerifyBackflow(); // 아군 턴 안에서 발생하고 끝나는 실제 약화 확인
        yield return new ExitPlayMode(); // 독립 전투를 정리한 뒤 편집 모드 복귀
    } // 역류 실제 전투 검사 종료

    private static IEnumerator VerifyBackflow() // 각 적의 원본 피해와 이후 일반 피해의 수치 차이 대조
    { // 역류의 정산 순서 검증 시작
        using var fight = new Fight("MANA_BACKFLOW", "COLD_TOUCH"); // 실제 광역 지팡이와 별도의 다음 단일 공격 준비
        var owner = fight.Allies[0]; // 원래 광역 피해 카드의 실제 소유자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 일반 공격력과 피해의 원본 값 비교 조건
        var raw = new List<int>(); // 전체 대상의 실제 광역 피해 순서
        foreach (var enemy in fight.Enemies) // 실제 전투의 모든 생존 적에 피해 관찰 연결
        { // 각 광역 대상의 실제 적용 관찰 시작
            enemy.DamageTaken += (_, result) => raw.Add(result.RawDamage); // 공격력 감소 전에 들어간 실제 원본 피해 기록
        } // 각 광역 대상의 실제 적용 관찰 종료
        yield return fight.Use("MANA_BACKFLOW"); // 지정 입력 없이 전체 생존 적에 정상 마법 피해 정산
        CollectionAssert.AreEqual(new[] { 12, 12, 12, 12 }, raw); // 해당 카드의 모든 실제 광역 피해가 열두 점으로 유지 확인
        Assert.AreEqual(-2, owner.AttackPowerBonus); // 실제 카드 이동 뒤 적용된 시전자 전용 공격력 감소 확인
        Assert.AreEqual(0, fight.Allies[1].AttackPowerBonus); // 다른 아군의 일반 공격력 유지 확인
        yield return fight.Use("COLD_TOUCH", 0); // 현재 턴의 같은 시전자가 고정 마법 피해를 이어 사용
        Assert.AreEqual(3, raw[raw.Count - 1]); // 이후 공격에만 시트 원본 다섯 점에서 두 점 감소 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 약화가 생긴 실제 아군 턴 종료
        Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 사용한 턴의 일시 약화 만료 확인
        Assert.AreEqual(0, owner.AttackPowerBonus); // 기존 공격력으로 실제 반환 확인
    } // 역류의 정산 순서 검증 종료

    [UnityTest] // 고정 비용, 실제 저항 관통과 반향의 일반 방어 경계 검사
    public IEnumerator ArcaneSurgeIgnoresTwentyPercentOfCurrentResistanceInPreviewAndHit() // 현재 마법 저항만 낮추는 비전 쇄도의 실제 계산 확인
    { // 저항 관통 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 및 방어도와 저항 상태가 동작하는 환경 진입
        yield return VerifyArcane(); // 관통 계산과 기존 방어 및 반향의 원래 저항 검증
        yield return new ExitPlayMode(); // 독립 테스트를 마친 뒤 편집 모드 복귀
    } // 저항 관통 실제 전투 검사 종료

    private static IEnumerator VerifyArcane() // 정상 원본과 반향의 현재 방어 수치 및 고정 AP 대조
    { // 비전 쇄도의 공용 예고와 실제 명중 검증 시작
        using var fight = new Fight("MAGIC_ECHO", "ARCANE_SURGE"); // 실제 관통을 쓰는 고정 공격과 독립 반향 준비
        var target = fight.Enemies[0]; // 실제 현재 마법 저항이 있는 대상
        var owner = fight.Allies[0]; // 실제 비전 쇄도의 원래 소유자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 고정 피해 원본을 비교할 일반 정신 상태
        target.ApplyStatusEffect(BattleStatusEffectType.MagicalResistanceUp, 37, 3, 1); // 원본 적 저항이 아닌 현재 상태로 늘린 실제 마법 저항
        target.GainBlock(2); // 저항을 관통해도 흡수할 수 있는 실제 방어도
        var attack = fight.View("ARCANE_SURGE").RuntimeCard; // 손패의 원본 고정 비용과 마법 피해 자료
        Assert.AreEqual(3, attack.ApCost); // 원본 고정 행동력 비용 확인
        Assert.AreEqual("3", attack.CostLabel); // 가변 비용 표시가 아닌 실제 원본 숫자 확인
        var preview = target.PreviewDamage(attack.EffectValue, attack.DamageType, owner, alliedDirectAttack: true, magicalResistancePenetrationPercent: attack.SourceData.SecondaryEffectValue); // 실제 대상에 대한 공통 순수 피해 예고
        Assert.AreEqual(29, preview.DefenseValue); // 현재 저항 37의 80% 내림 확인
        Assert.AreEqual(16, preview.AppliedDamage); // 관통 뒤 남은 실제 방어도까지 계산 확인
        Assert.AreEqual(2, target.CurrentBlock); // 예고에서 실제 방어도를 미리 사용하지 않음 확인
        var actual = new List<BattleDamageResult>(); // 원래 타격과 반향의 실제 피해 과정 기록
        target.DamageTaken += (_, result) => actual.Add(result); // 두 실제 타격의 저항 및 최종 피해 확인
        yield return fight.Use("MAGIC_ECHO"); // 한 번의 실제 피해에 독립 반향 준비
        yield return fight.Use("ARCANE_SURGE", 0); // 고정 비용 세 점으로 실제 적을 지정해 일반 마법 타격
        Assert.AreEqual(2, actual.Count); // 같은 카드의 관통 원본과 별도 반향의 실제 두 타격 확인
        Assert.AreEqual(preview.DefenseValue, actual[0].DefenseValue); // 예고와 실제 첫 타격에 동일한 현재 저항 관통 적용 확인
        Assert.AreEqual(preview.AppliedDamage, actual[0].AppliedDamage); // 공개 예고와 실제 HP 감소 경로의 수치 일치 확인
        Assert.AreEqual(37, actual[1].DefenseValue); // 반향에는 원래 현재 저항을 사용하는 값 확인
        Assert.AreEqual(12, actual[1].RawDamage); // 원본 24의 기본 반감만 쓰는 반향 확인
        Assert.AreEqual(15, fight.Points.CurrentActionPoints); // 두 카드의 원본 비용만 내고 추가 사용하지 않음을 확인
        Assert.AreEqual(37, target.MagicalResistance); // 저항 관통이 실제 대상의 지속 저항을 바꾸지 않음 확인
    } // 비전 쇄도의 공용 예고와 실제 명중 검증 종료

    private static IEnumerator UseVariable(Fight fight) // 실제 남은 행동력 결제의 정상 가변 카드 입력
    { // 마력 해방의 실제 클릭과 연출 실행 시작
        var view = fight.View("MANA_RELEASE"); // 가변 비용 카드의 현재 실제 손패 화면
        var source = view.RuntimeCard; // 전투에 정산된 실제 동일 카드 객체
        view.OnPointerClick(null); // 실제 가변 카드의 대상 선택 시작
        fight.EnemyViews[0].OnPointerClick(null); // 생존 적 한 명으로 실제 타격 확정
        float deadline = Time.realtimeSinceStartup + 10f; // 카드 연출의 실제 완료 대기 한도
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 피해 및 카드 사용 이동이 끝날 때까지 프레임 대기
        { // 다음 프레임 대기 시작
            yield return null; // 일반 전투 연출과 손패 갱신 대기
        } // 다음 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 정상 카드 행동의 실제 완료 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, source); // 가변 카드의 정상적인 실제 사용 이동 확인
    } // 마력 해방의 실제 클릭과 연출 실행 종료
} // 지팡이 산술 검사 종료
