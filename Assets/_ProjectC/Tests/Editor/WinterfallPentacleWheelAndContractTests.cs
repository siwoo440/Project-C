using System.Collections; // 실제 모드와 카드 연출의 프레임 대기
using System.Linq; // 정확히 선택한 실물 카드 식별
using NUnit.Framework; // 체력과 유효 선택 및 실제 이동 검증
using UnityEngine; // 실제 전투 모달이 연결된 캔버스 탐색
using UnityEngine.TestTools; // 실제 Unity 모드와 카드 회귀 검사
using UnityEngine.UI; // 활성 선택 모달의 실제 버튼 입력
using Fight = WinterfallSwordCombatTests.Fight; // 실제 전투 카드와 손패 화면 연결 재사용

public sealed class WinterfallPentacleWheelAndContractTests // 펜타클 카드의 실제 선택 정산과 개별 자원 비용 검사
{ // 펜타클 지불 회귀 검사 시작
    private const string Wheel = "WHEEL_OF_FATE"; // 실제 펜타클 가변 회복의 보물 원본
    private const string Contract = "CONTRACT"; // 실제 체력 비용과 행동력 환급의 보물 원본

    [UnityTest] // 부분 선택 취소와 실제 지불을 비교하는 카드 검사
    public IEnumerator WheelMovesExactPhysicalCardsAndHealsOnlyAfterAllChoices() // 실제 X 카드 선택과 비용 및 전체 생존 회복 확인
    { // 가변 지불의 실제 검사 시작
        yield return new EnterPlayMode(); // 손패와 선택 화면 및 실제 전투 환경 진입
        yield return VerifyWheel(); // 선택 잠금과 취소 및 최종 실제 결제 대조
        yield return new ExitPlayMode(); // 실제 전투 객체를 해제한 뒤 편집 모드로 복귀
    } // 가변 지불의 실제 검사 종료

    [UnityTest] // 선택이 불가능하거나 행동력이 변한 경우의 실제 취소 검사
    public IEnumerator WheelRejectsInsufficientOrChangedPaymentWithoutMovingCards() // 유효하지 않은 비용의 카드 이동과 결제 방지
    { // 선택 비용 경계 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 및 선택 모달이 동작하는 모드 시작
        VerifyRejectedWheelCosts(); // 부족한 손패와 선택 중 바뀐 실제 행동력 검증
        yield return new ExitPlayMode(); // 선택 해제와 상태 초기화 뒤 편집 모드 복귀
    } // 선택 비용 경계 검사 종료

    [UnityTest] // 생존 경계와 실체력 및 행동력 환급 검증
    public IEnumerator ContractRequiresSurvivingTenHealthAndRefundsActualActionPoints() // 방어도를 거치지 않는 생존 비용과 환급
    { // 계약 카드 실제 지불 검사 시작
        yield return new EnterPlayMode(); // 실제 소유자와 카드 손패를 쓸 수 있는 환경 진입
        yield return VerifyContract(); // 체력 비용 불가와 생존할 정산 및 환급 대조
        yield return new ExitPlayMode(); // 사용한 실제 전투 정상 해제 뒤 편집 모드 복귀
    } // 계약 카드 실제 지불 검사 종료

    private static IEnumerator VerifyWheel() // 물리 카드 두 장의 선택 및 회복량 검증
    { // 실제 펜타클 가변 정산 확인 시작
        using var fight = new Fight(Wheel, "MANA_RELEASE", "CHARGE", "MAGIC_ECHO"); // 서로 다른 실제 비용 카드가 포함된 손패
        var source = fight.View(Wheel).RuntimeCard; // 실제 선택을 움직일 사용 원본 카드
        var otherCards = fight.Deck.Hand.Where(card => card != source).ToArray(); // 비용이 될 수 있는 실제 순서의 다른 카드
        var who = fight.Allies[0]; // 원래 카드 비용을 지불하는 실제 시전자
        var friend = fight.Allies[1]; // 회복 적용을 확인할 생존 동료
        var fallen = fight.Allies[2]; // 회복되지 않아야 할 전투불능 동료
        Assert.IsTrue(who.ApplyPersistentMental(50)); // 기존 정신력 보정과 구별되는 실제 회복 기본값
        Assert.IsTrue(who.ApplyPersistentHealth(10)); // 상한과 무관하게 여섯 점을 실제 회복할 시전자
        Assert.IsTrue(friend.ApplyPersistentHealth(10)); // 같은 양을 받아야 할 다른 실제 생존 동료
        Assert.IsTrue(fallen.ApplyPersistentHealth(0)); // 부활 규칙이 아닌 실제 회복 대상의 경계
        Assert.IsTrue(fight.Points.Spend(18)); // 사용 가능한 실제 행동력을 두 점으로 설정
        Assert.AreEqual("X", source.CostLabel); // 원래 실물 카드의 가변 비용 기호 확인
        fight.View(Wheel).OnPointerClick(null); // 별도 비용 지불 전 실제 모달 열기
        CollectionAssert.AreEqual(new[] { "Choice_0", "Choice_1" }, Choices(fight).Select(button => button.name)); // 공용 첫 페이지에 실제 후보 두 장 표시 확인
        Choose(fight, "NextPage"); // 공용 선택창에서 나머지 실제 후보 페이지 열기
        CollectionAssert.AreEqual(new[] { "Choice_2" }, Choices(fight).Select(button => button.name)); // 자기 카드가 빠진 나머지 세 번째 후보 확인
        Choose(fight, "PreviousPage"); // 실제 첫 페이지로 돌아와 비용 카드 선택
        Choose(fight, "Choice_0"); // 원래 비용 후보 한 장만 먼저 선택
        Assert.AreEqual(2, fight.Points.CurrentActionPoints); // 부분 선택에 전액 비용 미지급 확인
        CollectionAssert.AreEqual(otherCards, fight.Deck.Hand.Where(card => card != source)); // 실제 선택 비용 카드가 아직 움직이지 않음을 확인
        Assert.IsFalse(fight.Turns.EndPlayerTurn()); // 미완성 선택 중 원래 전투 입력 잠금 확인
        Assert.IsFalse(fight.Turns.TryEscape()); // 개별 효과 선택 중 원래 도주 잠금 확인
        Choose(fight, "CancelChoice"); // 하나 고른 선택을 실제로 취소하는 버튼 누름
        Assert.IsNull(Overlay(fight)); // 기본 선택 창의 즉시 닫힘 확인
        Assert.AreEqual(2, fight.Points.CurrentActionPoints); // 취소로 행동력 유지 확인
        Assert.Zero(fight.Deck.ExcludedPile.Count); // 취소로 개별 카드 제외 없음 확인
        Assert.AreEqual(10, friend.CurrentHealth); // 취소한 회복이 실제 체력에 적용되지 않음 확인
        int committed = 0; // 실제 카드 사용 알림 횟수 기록
        fight.Controller.CardUsed += (used, _) => committed += used == source ? 1 : 0; // 정확한 원본 사용의 추가 실행 감시
        fight.View(Wheel).OnPointerClick(null); // 완성 가능한 두 장의 실제 선택 재개
        Choose(fight, "Choice_0"); // 원래 다른 카드 순서의 첫 카드 선택
        Choose(fight, "Choice_1"); // 남아 있는 두 후보 중 두 번째인 원래 세 번째 선택
        yield return AwaitImpact(fight); // 실제 일반 카드 회복과 손패 이동이 완료될 때까지 대기
        Assert.AreEqual(1, committed); // 전체 비용을 확정한 시전에만 실제 사용 신호 발생 확인
        Assert.Zero(fight.Points.CurrentActionPoints); // 선택한 두 장과 같은 실제 행동력 전액 지불 확인
        CollectionAssert.AreEqual(new[] { otherCards[0], otherCards[2] }, fight.Deck.ExcludedPile); // 고른 정확히 두 실제 카드만 전투 제외
        CollectionAssert.Contains(fight.Deck.Hand, otherCards[1]); // 고르지 않은 실물 카드는 원래 손패에 남는지 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, source); // 사용 원본의 일반적인 버린 더미 정산 확인
        Assert.AreEqual(4, fight.Deck.SourceData.Cards.Count); // 원래 저장 덱의 카드 소유까지 없어지지 않음 확인
        Assert.AreEqual(16, who.CurrentHealth); // 실제 두 점 곱하기 셋의 시전자 체력 회복 확인
        Assert.AreEqual(16, friend.CurrentHealth); // 다른 생존 아군의 동일한 기본 회복 확인
        Assert.Zero(fallen.CurrentHealth); // 회복만으로 쓰러진 아군을 되살리지 않음 확인
    } // 실제 펜타클 가변 정산 확인 종료

    private static void VerifyRejectedWheelCosts() // 실제 모달을 열기 전후의 검증 및 변경 경계
    { // 부족과 선택 비용 변경의 실제 검사 시작
        using var fight = new Fight(Wheel, "MANA_RELEASE", "MAGIC_ECHO"); // 두 장까지만 선택 가능한 실제 손패
        Assert.IsTrue(fight.Points.Spend(17)); // 실제 손패보다 하나 더 많은 필요 선택 수 준비
        fight.View(Wheel).OnPointerClick(null); // 부족한 손패로 가변 회복 카드 사용 시도
        Assert.IsNull(Overlay(fight)); // 명확히 선택할 수 없는 비용의 모달 미표시
        Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 초기 부족 경계에서 실제 비용 유지
        Assert.IsTrue(fight.Points.Spend(1)); // 현재 두 장을 고를 수 있는 실제 행동력 준비
        fight.View(Wheel).OnPointerClick(null); // 실제 유효 비용의 두 장 선택 시작
        Choose(fight, "Choice_0"); // 첫 장을 고른 상태로 나머지 선택 대기
        Assert.AreEqual(1, fight.Points.Gain(1)); // 외부 효과로 실제 행동력이 달라진 시점 재현
        Assert.IsNull(Overlay(fight)); // 비용이 달라진 기존 선택이 안전하게 취소되었는지 확인
        Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 외부 회복 효과 외 유물과 카드의 추가 비용 없음
        Assert.AreEqual(3, fight.Deck.HandCount); // 취소된 두 장과 사용 원본의 실제 손패 보존
        Assert.Zero(fight.Deck.ExcludedPile.Count); // 변경된 선택에서 부분 제외가 없음을 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 변경에 의해 해제된 일반 입력 잠금 확인
    } // 부족과 선택 비용 변경의 실제 검사 종료

    private static IEnumerator VerifyContract() // 기존 보호에 영향 없는 실체력 비용 및 실제 AP 환급
    { // 펜타클 계약의 자원 정산 검사 시작
        using var fight = new Fight(Contract); // 실제 고정 행동력과 체력 비용 카드로 독립 전투 구성
        var card = fight.View(Contract).RuntimeCard; // 원래 손패에 보이는 실제 계약 카드
        var owner = card.OwnerUnit; // 자신의 체력을 직접 소모하는 실제 시전자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 대상과 피해의 별도 정신력 변화 관찰값
        Assert.IsTrue(owner.ApplyPersistentHealth(10)); // 기존 생존 불가 경계인 정확한 열 점 준비
        Assert.IsTrue(fight.Points.Spend(16)); // 실제 비용 후 순수 환급을 확인할 남은 네 점
        Assert.AreEqual("3", card.CostLabel); // 시트의 원래 고정 사용 비용 표시 확인
        fight.View(Contract).OnPointerClick(null); // 정확히 열 점 체력에서 생존 가정의 사용 시도
        Assert.AreEqual(10, owner.CurrentHealth); // 사용할 수 없는 시전자의 실제 체력 유지
        Assert.AreEqual(4, fight.Points.CurrentActionPoints); // 체력 조건에 실패한 경우 행동력 비용 없음
        CollectionAssert.Contains(fight.Deck.Hand, card); // 거절된 같은 계약 원본이 실제 손패에 유지
        Assert.IsTrue(owner.ApplyPersistentHealth(11)); // 살아남을 수 있는 최소 조건 준비
        owner.GainBlock(12); // 기존 피해 흡수 경로를 사용하지 않음을 대조할 실제 방어도
        owner.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1, owner); // 실제 피격에는 소비될 원래 피해 보호
        int rawDamageSignals = 0; // 진짜 피해 이벤트와 별도 체력 비용 비교 횟수
        owner.DamageTaken += (_, __) => rawDamageSignals++; // 카드 비용이 일반 공격으로 기록되는지 검사
        fight.View(Contract).OnPointerClick(null); // 실제 자신 대상 카드의 비용과 지원 연출 실행
        yield return AwaitImpact(fight); // 카드 이동과 행동력 환급의 실제 완료 대기
        Assert.AreEqual(1, owner.CurrentHealth); // 체력 열 점을 직접 써서 한 점으로 정상 생존
        Assert.AreEqual(4, fight.Points.CurrentActionPoints); // 세 점 소모 뒤 실제 현재 행동력에 세 점 환급 확인
        Assert.AreEqual(12, owner.CurrentBlock); // 카드 체력 비용에 기존 방어도 소모 없음 확인
        Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 실제 피격과 달리 피해 무효 보호가 남음 확인
        Assert.Zero(rawDamageSignals); // 실제 피해 이벤트를 발생시키지 않은 정상 체력 비용 확인
        Assert.AreEqual(50, owner.CurrentMental); // 일반 피격 정신력 페널티가 없음 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, card); // 실제 사용 원본 한 장의 정상 손패 이동 확인
    } // 펜타클 계약의 자원 정산 검사 종료

    private static BattleDrawChoiceView Overlay(Fight fight) // 실제 전투 화면의 활성 모달 탐색
    { // 실물 모달 조회 시작
        return fight.Hand.GetComponentInParent<Canvas>().GetComponentInChildren<BattleDrawChoiceView>(); // 비활성 정리 대상은 제외한 현재 선택 화면
    } // 실물 모달 조회 종료

    private static Button[] Choices(Fight fight) // 후보 카드에 해당하는 실제 버튼 조회
    { // 선택 카드 버튼 조회 시작
        return Overlay(fight).GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Choice_")).ToArray(); // 취소와 페이지 버튼 제외
    } // 선택 카드 버튼 조회 종료

    private static void Choose(Fight fight, string name) // 현재 활성 카드 후보나 취소의 실제 입력
    { // 실제 모달 선택 입력 시작
        Overlay(fight).GetComponentsInChildren<Button>().Single(button => button.name == name).onClick.Invoke(); // 일치하는 모달 버튼의 사용자 이벤트 실행
    } // 실제 모달 선택 입력 종료

    private static IEnumerator AwaitImpact(Fight fight) // 실제 지원 연출과 정산을 기다리는 공통 경계
    { // 정상 충돌 및 복귀 대기 시작
        float deadline = Time.realtimeSinceStartup + 12f; // 실제 다중 회복의 연출을 포함할 관찰 기간
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 정상 카드 효과 정산의 종료 조건
        { // 다음 실제 전투 화면 프레임 대기 시작
            yield return null; // 현재 프레임 이후 엔진 정산 대기
        } // 다음 실제 전투 화면 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 실행이 정상적으로 끝났는지 확인
    } // 정상 충돌 및 복귀 대기 종료
} // 펜타클 지불 회귀 검사 종료
