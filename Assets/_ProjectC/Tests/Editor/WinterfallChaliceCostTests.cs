using System.Collections; // 실제 카드 연출과 플레이 모드 전환 대기
using System.Collections.Generic; // 실제 비용과 회복 순서 기록
using System.Linq; // 지정 아군의 실제 대상 화면 조회
using NUnit.Framework; // 카드 비용과 적용 수치 비교
using UnityEngine; // 전투 화면과 실제 연출 시간 사용
using UnityEngine.TestTools; // 플레이 모드의 실제 카드 사용 검사
using Fight = WinterfallSwordCombatTests.Fight; // 공용 전투 손패와 대상 선택 환경

public sealed class WinterfallChaliceCostTests // 신규 성배 카드의 실제 비용과 회복 검사
{ // 성배 비용 검사 시작
    [UnityTest] // 생명의 계약의 정신력 지불과 대상 선택 검증
    public IEnumerator LifeContractPaysCasterMentalBeforeHealingAndAllowsLowMental() // 시전자만 비용을 내고 정신력 영까지 지불하는지 확인
    { // 실제 플레이 검사 시작
        yield return new EnterPlayMode(); // 카드와 손패 UI가 활성화되는 검사 모드 진입
        yield return VerifyLifeContract(); // 실제 선택과 정신력 감소 및 회복의 순서 검사
        yield return new ExitPlayMode(); // 편집 모드로 복귀하여 격리 환경 해제
    } // 실제 플레이 검사 종료

    private static IEnumerator VerifyLifeContract() // 지정 아군 회복의 실제 비용과 실패 경계 검사
    { // 생명의 계약 검사 시작
        using var fight = new Fight("LIFE_CONTRACT", "LIFE_CONTRACT"); // 실제 지급 목록의 카드 2장으로 손패 구성
        var owner = fight.Allies[0]; // 정신력을 지불할 실제 시전자
        var target = fight.Allies[1]; // 체력을 받을 다른 아군
        var recipient = fight.Hand.GetComponentInParent<Canvas>().GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == target); // 실제 아군 클릭 화면 선택
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 붕괴가 없는 기본 회복 계산 조건 설정
        Assert.IsTrue(target.ApplyPersistentHealth(1)); // 두 번 회복할 체력 여유 준비
        var events = new List<string>(); // 실제 정신력과 체력 신호의 순서 기록
        owner.MentalChanged += (_, change) => // 실제 시전자 정신력 변경 신호 구독
        { // 카드 효과 정신력 변화 기록 시작
            if (change.Reason == BattleMentalChangeReason.CardEffect) // 다른 정신력 변화 이벤트 제외
            { // 계약의 정신력 기록 시작
                events.Add($"mind:{change.AppliedDelta}"); // 원본 정신력 효과의 실제 감소량 기록
            } // 계약의 정신력 기록 종료
        }; // 카드 효과 정신력 변화 기록 종료
        target.HealingResolved += (_, _, applied) => events.Add($"heal:{applied}"); // 아군의 실제 체력 회복 수치 기록
        var first = fight.View("LIFE_CONTRACT"); // 카드 소유자의 첫 계약 실제 손패 조회
        var usedFirst = first.RuntimeCard; // 실제 카드 이동 뒤에도 유지할 첫 인스턴스 보관
        first.OnPointerClick(null); // 카드만 선택하고 아군을 아직 지정하지 않은 입력
        Assert.AreEqual(50, owner.CurrentMental); // 선택만으로 정신력이 빠지지 않는지 확인
        Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 대상 선택 전에는 행동력을 보존하는지 확인
        recipient.OnPointerClick(null); // 아군 선택으로 사용 확정
        Assert.AreEqual(45, owner.CurrentMental); // 실제 사용 직후 시전자 정신력 5 지불 확인
        Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 계약 행동력 3 지불 확인
        yield return VerifyDiscard(fight, usedFirst); // 연출이 끝나고 이동한 당사 카드 확인
        Assert.AreEqual(26, target.CurrentHealth); // 기본 회복량 25의 정상 적용 확인
        CollectionAssert.AreEqual(new[] { "mind:-5", "heal:25" }, events); // 시전자 지불 이후 아군의 직접 회복 적용 확인
        Assert.IsTrue(owner.ApplyPersistentMental(2)); // 원본 정신력 지불량보다 현재 정신력이 적은 경우 설정
        var second = fight.View("LIFE_CONTRACT"); // 남은 실제 두 번째 계약 카드 조회
        var used = second.RuntimeCard; // 손패 이동 이후에도 확인할 카드 인스턴스 보관
        second.OnPointerClick(null); // 두 번째 원본 계약을 실제로 선택
        recipient.OnPointerClick(null); // 같은 생존 동료를 지정해 지불 확정
        int expectedHealing = owner.ModifyOutgoingHealing(25); // 지불에 따른 붕괴 이후의 기존 회복 보정 조회
        yield return VerifyDiscard(fight, used); // 회복이 적용될 때까지 실제 타격 연출 대기
        Assert.Zero(owner.CurrentMental); // 부족한 정신력의 정상 영 하한 처리 확인
        Assert.AreEqual(14, fight.Points.CurrentActionPoints); // 낮은 정신력에서도 적법한 행동력 지불 기록 확인
        Assert.AreEqual(26 + expectedHealing, target.CurrentHealth); // 회복 자체는 기존 시전자 정신 상태 보정을 따름 확인
        CollectionAssert.AreEqual(new[] { "mind:-5", "heal:25", "mind:-2", $"heal:{expectedHealing}" }, events); // 낮은 정신력에서도 지불부터 회복까지 한 번 실행 확인
    } // 생명의 계약 검사 종료

    [UnityTest] // 순교자의 빛의 체력 비용과 생존 아군 전체 회복 확인
    public IEnumerator MartyrsLightPaysOneHealthFloorBeforeHealingEveryLivingAlly() // 체력 비용이 피해로 계산되거나 방어를 소모하지 않는지 확인
    { // 순교자의 빛 검사 모드 시작
        yield return new EnterPlayMode(); // 카드 사용과 실제 대상 화면 초기화 대기
        yield return VerifyMartyrsLight(); // 생존 하한과 회복 계산 순서 검증
        yield return new ExitPlayMode(); // 전투 검증을 끝내고 편집 모드 복귀
    } // 순교자의 빛 검사 모드 종료

    private static IEnumerator VerifyMartyrsLight() // 실제 소유자 체력 소모와 모두의 회복 검증
    { // 순교자의 빛 효과 확인 시작
        using var fight = new Fight("MARTYRS_LIGHT", "MARTYRS_LIGHT"); // 두 번 사용할 실제 카드 원본과 배치된 유닛 구성
        var owner = fight.Allies[0]; // 체력으로 비용을 낼 시전자
        var ally = fight.Allies[1]; // 별개로 광역 회복을 받을 생존 동료
        var fallen = fight.Allies[2]; // 광역 체력 회복에서 제외할 전투불능 동료
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 회복 기본 수치를 확인할 일반 정신 상태 준비
        Assert.IsTrue(owner.ApplyPersistentHealth(47)); // 기존 체력에서 한 점으로 감소하는 경우 준비
        Assert.IsTrue(ally.ApplyPersistentHealth(5)); // 생존 동료의 회복 기준 체력 설정
        Assert.IsTrue(fallen.ApplyPersistentHealth(0)); // 부활 효과가 없는 회복의 제외 대상 설정
        owner.GainBlock(8); // 체력 비용이 소비하지 않을 기존 방어도 준비
        var protectedResult = owner.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 2, 1); // 비용에 영향을 받지 않는 실제 피해 무효 보호 적용
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, protectedResult); // 남아야 할 보호 상태의 실제 초기화 확인
        var ownerHealth = new List<int>(); // 비용과 회복의 시전자 체력 이벤트 기록
        int hits = 0; // 실제 피해 이벤트의 발생 횟수
        owner.HealthChanged += unit => ownerHealth.Add(unit.CurrentHealth); // 시전자의 실제 체력 변경 순서 구독
        owner.DamageTaken += (_, _) => hits++; // 체력 비용에 의한 일반 피해 발생 여부 확인
        yield return fight.Use("MARTYRS_LIGHT"); // 손패의 실제 광역 회복 카드 선택과 정산 대기
        CollectionAssert.AreEqual(new[] { 1, 31 }, ownerHealth); // 시전자 체력이 한 점으로 내려간 뒤 30 회복 확인
        Assert.AreEqual(35, ally.CurrentHealth); // 별개 생존 동료의 30 체력 회복 확인
        Assert.IsTrue(fallen.IsDead); // 광역 회복으로 쓰러진 동료가 부활하지 않음 확인
        Assert.AreEqual(8, owner.CurrentBlock); // 시전자의 기존 방어도 보존 확인
        Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 비용 지불이 피해 무효를 소모하지 않음 확인
        Assert.Zero(hits); // 지불한 체력이 전투 피해 이벤트를 만들지 않음 확인
        Assert.IsTrue(owner.ApplyPersistentHealth(1)); // 이미 한 점인 시전자도 규칙대로 사용할 수 있는지 준비
        ownerHealth.Clear(); // 두 번째 카드 효과에서 발생할 체력 기록만 남기기 위한 초기화
        yield return fight.Use("MARTYRS_LIGHT"); // 체력 한 점에서 두 번째 실제 광역 카드 사용
        CollectionAssert.AreEqual(new[] { 31 }, ownerHealth); // 체력이 한 점인 경우 비용 이벤트 없이 정상 회복 확인
        Assert.AreEqual(12, fight.Points.CurrentActionPoints); // 각 실제 사용의 4 행동력씩 차감 확인
    } // 순교자의 빛 효과 확인 종료

    [UnityTest] // 실제 지불한 행동력에 따라 치유량과 정화가 바뀌는 카드 확인
    public IEnumerator GrailMiracleUsesActualActionPointsAndCleansesAtFour() // 행동력 영 차단과 삼·사 경계 및 정화 순서 확인
    { // 성배 기적 검사 모드 시작
        yield return new EnterPlayMode(); // 전투 행동력과 자동 대상의 실물 화면 초기화 대기
        yield return VerifyGrailMiracle(0); // 행동력이 없을 때 카드 사용 차단 검사
        yield return VerifyGrailMiracle(3); // 정화 기준 바로 아래와 실제 24 회복 검사
        yield return VerifyGrailMiracle(4); // 정화 발동 기준과 실제 32 회복 검사
        yield return new ExitPlayMode(); // 성배 실제 카드 검사 마무리
    } // 성배 기적 검사 모드 종료

    private static IEnumerator VerifyGrailMiracle(int available) // 현재 행동력과 실제 체력 및 상태 적용 비교
    { // 가변 행동력 검사 시작
        using var fight = new Fight("GRAIL_MIRACLE"); // 실제 획득 성배의 기적 한 장으로 손패 구성
        var owner = fight.Allies[0]; // 광역 회복을 시전하고 받을 실제 소유자
        var ally = fight.Allies[1]; // 같은 효과의 다른 살아 있는 수령자
        var fallen = fight.Allies[2]; // 가변 비용 회복과 정화에서 제외할 대상
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 기본 가변 회복을 확인할 일반 정신 상태 준비
        Assert.IsTrue(owner.ApplyPersistentHealth(5)); // 두 경계 회복을 모두 수용할 낮은 체력 설정
        Assert.IsTrue(ally.ApplyPersistentHealth(5)); // 아군 전체 치유 총량 검증을 위한 둘째 기준 체력
        Assert.IsTrue(fallen.ApplyPersistentHealth(0)); // 부활과 구별되는 전체 회복 범위 준비
        foreach (var unit in new[] { owner, ally }) // 두 생존 아군에 각각 제거 가능 약화 설정
        { // 상태 경계 준비 시작
            unit.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 1); // 정화로 먼저 제거할 약화
            unit.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 3, 1); // 정화가 한 번만 적용된 뒤 남을 약화
            unit.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 1, 3, 1); // 기존 정화가 건드리지 않는 강화
        } // 상태 경계 준비 종료
        Assert.IsTrue(fight.Points.Spend(20 - available)); // 현재 남은 행동력에 정확히 영·삼·사 설정
        var view = fight.View("GRAIL_MIRACLE"); // 실제 손패에 렌더링한 가변 비용 카드 조회
        var card = view.RuntimeCard; // 카드가 손패를 떠난 뒤까지 참조할 원래 개별 카드
        Assert.AreEqual("X", card.CostLabel); // 고정 숫자가 없는 기존 손패 비용 기호 사용 확인
        view.OnPointerClick(null); // 원래 게임의 자동 대상 성배 카드 일반 입력
        if (available == 0) // 행동력이 남지 않은 카드의 실제 사용 실패 확인
        { // 영 행동력 금지 검사 시작
            Assert.IsFalse(fight.Runner.IsBusy); // 실패한 가변 카드에서 연출이 실행되지 않음 확인
            CollectionAssert.Contains(fight.Deck.Hand, card); // 행동력이 없는 사용에서 손패 보존 확인
            Assert.AreEqual(5, ally.CurrentHealth); // 사용할 수 없는 카드의 효과 미적용 확인
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 비용 영에서는 정화도 실행되지 않음 확인
            yield break; // 실패 사용에는 버린 더미 검사 생략
        } // 영 행동력 금지 검사 종료
        Assert.Zero(fight.Points.CurrentActionPoints); // 보유 행동력 실제 전액 한 번 지불 확인
        yield return VerifyDiscard(fight, card); // 실제 효과와 정상 버린 더미 이동 대기
        foreach (var unit in new[] { owner, ally }) // 생존 아군 모두에게 적용한 결과 검증
        { // 대상별 가변 회복 및 정화 검증 시작
            Assert.AreEqual(5 + available * 8, unit.CurrentHealth); // 원본 여덟 배 기준의 가변 직접 회복량 확인
            Assert.AreEqual(available >= 4, unit.GetStatusEffect(BattleStatusEffectType.Poison) == null); // 실제 지불한 행동력 사 경계에서만 첫 약화 제거 확인
            Assert.IsNotNull(unit.GetStatusEffect(BattleStatusEffectType.Bleed)); // 정화가 약화를 한 개만 없애는지 확인
            Assert.IsNotNull(unit.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 임의 제거가 강화에 작용하지 않는지 확인
        } // 대상별 가변 회복 및 정화 검증 종료
        Assert.IsTrue(fallen.IsDead); // 규칙상 광역 회복과 정화가 부활을 일으키지 않음 확인
    } // 가변 행동력 검사 종료

    private static IEnumerator VerifyDiscard(Fight fight, CardInstance used) // 실제 게임의 카드 연출과 버린 더미 이동 대기
    { // 카드 이동 대기 시작
        float deadline = Time.realtimeSinceStartup + 10f; // 정상 연출의 실제 시간 상한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 카드 사용의 타격과 정리 연출 완료 확인
        { // 다음 화면 대기 시작
            yield return null; // 원래 전투 프레임 진행 대기
        } // 다음 화면 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 연출이 실제로 완료됐는지 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, used); // 사용한 원래 카드 인스턴스가 실제로 버려졌는지 확인
    } // 카드 이동 대기 종료
} // 성배 비용 검사 종료
