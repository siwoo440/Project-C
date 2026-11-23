using System.Collections; // 실제 카드와 턴 연출 대기
using System.Linq; // 실제 지정 대상의 화면 조회
using System.Reflection; // 드래그 대상 검증의 실제 코드 호출
using NUnit.Framework; // 회복량과 실제 대상 검사
using UnityEngine.TestTools; // 독립 플레이 모드 정산 테스트
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드 입력과 턴 검사 환경

public sealed class WinterfallRecoveryCardTests // 회복 기도와 대상별 지속 회복 검사
{ // 회복 카드 검사 시작
    [UnityTest] // 실제 클릭과 드래그의 자동 회복 대상 검사
    public IEnumerator HealingPrayerUsesCurrentHealthAndStableRosterTies() // 현재 체력과 출전 순서에 따른 자동 선택
    { // 회복 기도 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 환경으로 전환
        yield return VerifyHealingPrayer(); // 플레이 모드의 클릭과 드래그 검사
        yield return new ExitPlayMode(); // 독립 검사 환경 종료
    } // 회복 기도 검사 종료

    private static IEnumerator VerifyHealingPrayer() // 실제 사용의 고정 대상과 원본 회복량 확인
    { // 자동 회복 검사 시작
        using var fight = new Fight("HEALING_PRAYER", "HEALING_PRAYER", "HEALING_PRAYER"); // 같은 정식 보상 카드 세 장 준비
        var first = fight.Allies[0]; // 명단 첫 아군이며 체력 비율이 더 낮은 대상
        var second = fight.Allies[1]; // 현재 체력 수치가 더 낮은 생존 아군
        var fallen = fight.Allies[2]; // 영의 체력이지만 자동 대상이 될 수 없는 아군
        first.ModifyMaxHealth(100 - first.MaxHealth); // 실제 비교의 첫 최대 체력 백 고정
        second.ModifyMaxHealth(50 - second.MaxHealth); // 실제 비교의 둘째 최대 체력 오십 고정
        first.ApplyPersistentHealth(10); // 낮은 비율이지만 체력 수치가 더 높은 첫 아군
        second.ApplyPersistentHealth(9); // 실제 최저 현재 체력인 둘째 아군
        fallen.ApplyPersistentHealth(0); // 자동 회복 대상에서 제외할 전투불능 아군
        var prayer = fight.View("HEALING_PRAYER"); // 실제 손패의 회복 기도 화면
        Assert.AreEqual(1, prayer.RuntimeCard.ApCost); // 정식 데이터의 행동력 한 점 확인
        var drag = prayer.GetComponent<BattleCardDragHandler>() ?? prayer.gameObject.AddComponent<BattleCardDragHandler>(); // 전투 씬의 실제 드래그 구성 재현
        drag.Initialize(prayer); // 손패와 정상 드래그 화면의 실제 연결
        var valid = typeof(BattleCardDragHandler).GetMethod("IsValidTarget", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 유효 드롭 검사 연결
        Assert.IsNotNull(drag); // 실제 손패 카드의 드래그 처리 확인
        Assert.IsNotNull(valid); // 드래그 가능 대상 판정 메서드 확인
        Assert.IsTrue((bool)valid.Invoke(drag, new object[] { prayer.RuntimeCard, second })); // 실제 최저 체력 아군의 드롭 가능 확인
        Assert.IsFalse((bool)valid.Invoke(drag, new object[] { prayer.RuntimeCard, first })); // 다른 체력 비율이 드래그 대상으로 사용되지 않음 확인
        BattleUnitRuntime selected = null; // 실제 카드 사용 시 정해진 단일 대상 기록
        fight.Controller.CardUsed += (_, targets) => selected = targets.Single(); // 충돌과 회복 이전의 실제 대상 확인
        int before = fight.Points.CurrentActionPoints; // 세 번 사용 전 공용 행동력 확인
        yield return fight.Use("HEALING_PRAYER"); // 대상 클릭이 없는 실제 자동 회복 사용
        Assert.AreSame(second, selected); // 남은 현재 체력 수치와 사망 제외 확인
        Assert.AreEqual(17, second.CurrentHealth); // 둘째 생존 아군의 원본 회복 여덟 적용
        Assert.AreEqual(10, first.CurrentHealth); // 체력 비율만 낮은 첫 아군의 체력 보존
        second.ApplyPersistentHealth(10); // 같은 현재 체력에서 출전 순서 기준 준비
        yield return fight.Use("HEALING_PRAYER"); // 현재 체력 동률의 자동 회복 사용
        Assert.AreSame(first, selected); // 동률에서 고정된 첫 출전석 선택 확인
        Assert.AreEqual(18, first.CurrentHealth); // 동률 처리 뒤 첫 생존 아군의 여덟 회복
        first.ModifyMaxHealth(10 - first.MaxHealth); // 최대 체력 열로 체력을 줄이는 실제 경로
        second.ModifyMaxHealth(10 - second.MaxHealth); // 둘째 아군도 최대 체력 열로 변경
        yield return fight.Use("HEALING_PRAYER"); // 모든 생존 대상이 최대 체력이더라도 정상 사용
        Assert.AreSame(first, selected); // 최대 체력 동률의 정상 대상 선택 확인
        Assert.AreEqual(10, first.CurrentHealth); // 최대 체력을 넘는 회복 없음 확인
        Assert.AreEqual(before - 3, fight.Points.CurrentActionPoints); // 모든 유효 사용의 고정 행동력 합계 확인
    } // 자동 회복 검사 종료

    [UnityTest] // 대상의 수신 회복 비율과 실제 턴 종료 재생 검사
    public IEnumerator QuietBlessingAndRenewalUseTheirOwnAllyTurnLifetimes() // 회복 증폭과 재생의 종료 시각 확인
    { // 잔잔한 축복과 재생 검사 시작
        yield return new EnterPlayMode(); // 실제 회복과 상태 전투로 전환
        yield return VerifyRenewal(); // 플레이 모드 안에서 카드와 턴 종료 실행
        yield return new ExitPlayMode(); // 독립 회복 검사 종료
    } // 잔잔한 축복과 재생 검사 종료

    private static IEnumerator VerifyRenewal() // 실제 회복 공식과 두 상태의 개별 유지 검사
    { // 수신 보정 회복 검사 시작
        using var fight = new Fight("QUIET_BLESSING", "REGENERATION"); // 발동할 정식 성배 카드 두 장
        var target = fight.Allies[1]; // 두 효과를 받을 생존 아군
        var other = fight.Allies[2]; // 상태 적용과 주기 회복에서 제외되는 대조군
        target.ModifyMaxHealth(100 - target.MaxHealth); // 증폭과 일반 회복에 충분한 최대 체력
        target.ApplyPersistentHealth(40); // 초과 회복 없이 정량을 비교할 부상
        int before = fight.Points.CurrentActionPoints; // 적용 전 실제 행동력
        yield return PlayOn(fight, "QUIET_BLESSING", target); // 실제 지정 아군에게 잔잔한 축복 사용
        Assert.AreEqual(before - 1, fight.Points.CurrentActionPoints); // 잔잔한 축복의 행동력 한 점 확인
        Assert.AreEqual(20, target.GetStatusEffect(BattleStatusEffectType.HealingReceivedUp)?.EffectiveValue); // 수신 회복의 백분율 이십 확인
        Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.HealingReceivedUp)?.RemainingTurns); // 즉시 적용된 두 아군 턴의 지속 확인
        Assert.IsNull(other.GetStatusEffect(BattleStatusEffectType.HealingReceivedUp)); // 지정하지 않은 아군의 회복 보정 보존
        Assert.AreEqual(12, target.RestoreHealth(10)); // 모든 회복에 공통으로 한 번만 이십 퍼센트 증가
        target.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 25, 2, 1); // 기존 받는 회복 감소의 독립 보정 설치
        Assert.AreEqual(9, target.RestoreHealth(11)); // 받는 증가와 감소를 함께 적용한 단일 내림 검증
        target.RemoveStatusEffect(BattleStatusEffectType.HealingReceivedDown); // 이후 재생의 비교에 남길 회복 증가만 유지
        target.ApplyPersistentHealth(50); // 정량 재생을 쉽게 읽는 체력 기준
        yield return PlayOn(fight, "REGENERATION", target); // 같은 아군의 실제 재생 카드 취득
        Assert.AreEqual(before - 3, fight.Points.CurrentActionPoints); // 재생의 행동력 두 점 추가 소모 확인
        Assert.AreEqual(50, target.CurrentHealth); // 재생 카드 부여 즉시 회복 없음 확인
        Assert.AreEqual(3, target.GetStatusEffect(BattleStatusEffectType.Renewal)?.RemainingTurns); // 정식 재생의 독립 세 턴 지속 확인
        Assert.IsNull(other.GetStatusEffect(BattleStatusEffectType.Renewal)); // 지정 이외 아군에게 재생 미적용
        int[] healing = { 6, 6, 5, 0 }; // 축복 두 턴과 일반 재생 한 턴 및 만료 대조
        for (int index = 0; index < healing.Length; index++) // 두 진영의 실제 턴 종료와 시작 모두 반복
        { // 아군 종료 한 번의 상태와 수치 비교 시작
            int previous = target.CurrentHealth; // 현재 아군 턴 종료 직전 체력
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴 종료로 재생 발동
            Assert.AreEqual(previous + healing[index], target.CurrentHealth); // 종료 시점에만 예상 회복 한 번
            Assert.AreEqual(System.Math.Max(0, 1 - index), target.GetStatusEffect(BattleStatusEffectType.HealingReceivedUp)?.RemainingTurns ?? 0); // 해당 턴의 재생 이후 축복 기간 차감
            Assert.AreEqual(System.Math.Max(0, 2 - index), target.GetStatusEffect(BattleStatusEffectType.Renewal)?.RemainingTurns ?? 0); // 발동한 세 실제 아군 턴만 차감
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 적 종료와 다음 아군 턴 시작 실행
            Assert.AreEqual(previous + healing[index], target.CurrentHealth); // 적 종료나 아군 시작에서 중복 재생 없음 확인
        } // 아군 종료 한 번의 상태와 수치 비교 종료
    } // 수신 보정 회복 검사 종료

    private static IEnumerator PlayOn(Fight fight, string id, BattleUnitRuntime ally) // 실제 지정 아군 클릭으로 카드 사용
    { // 카드 대상 화면 조회 시작
        var view = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(candidate => candidate.RuntimeUnit == ally); // 기존 전투의 생존 아군 선택 화면
        return WinterfallCommonCardExpansionTests.Play(fight.View(id).RuntimeCard.SourceData, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 실제 비용과 카드 이동 처리 포함 사용
    } // 카드 대상 화면 조회 종료
} // 회복 카드 검사 종료
