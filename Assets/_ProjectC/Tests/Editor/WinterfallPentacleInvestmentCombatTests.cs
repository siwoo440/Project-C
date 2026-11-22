using System.Collections; // Unity 모드 전환과 실제 카드 연출 대기
using NUnit.Framework; // 체력과 행동력 및 손패의 실제 결과 확인
using TMPro; // 원래 전투 툴팁의 수치 읽기
using UnityEngine; // 기존 전투 프레임 경계
using UnityEngine.TestTools; // 실제 전투 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 실제 손패와 아군 및 적의 공통 시험 전투

public sealed class WinterfallPentacleInvestmentCombatTests // 지불 후 공용 행동력과 체력 거래 검증
{ // 펜타클 자원 전투 검사 시작
    [UnityTest] // 남은 행동력이 여러 대상에 고정되는 실제 공격
    public IEnumerator RunawayRuneDeviceKeepsItsPostPaymentDamageAcrossTargets() // 피해 중 증가한 AP와 별개인 기본 피해 확인
    { // 광역 마법 피해 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 툴팁의 플레이 모드 준비
        using (var fight = new Fight("RUNAWAY_RUNE_DEVICE")) // 고정 원본과 생존 적 네 명으로 구성
        { // 실제 툴팁과 타격 수치 대조 시작
            Assert.IsTrue(fight.Points.Spend(13)); // 행동력 일곱 중 카드 비용 셋을 지불할 준비
            var card = fight.View("RUNAWAY_RUNE_DEVICE").RuntimeCard; // 실제 보유한 광역 공격 카드
            var tooltip = fight.Hand.transform.Find("CardTooltip").GetComponent<BattleCardTooltipView>(); // 실제 전투 손패의 상세 화면
            tooltip.Show(card, fight.Points.CurrentActionPoints); // 현재 상태가 알려진 정상 상세 표시
            var detail = tooltip.transform.Find("DetailText").GetComponent<TMP_Text>(); // 게임 화면의 실제 상세 문구
            StringAssert.Contains("현재 기본 피해 16", detail.text); // 지불 뒤 행동력 넷에 원본 배수 넷을 적용한 예상값
            fight.Enemies[0].DamageTaken += (_, __) => fight.Points.Gain(3); // 첫 실제 타격에서 공용 AP가 바뀌는 게임 상태 재현
            yield return Play(fight, "RUNAWAY_RUNE_DEVICE"); // 실제 광역 카드 사용과 정상 마법 타격 정산
            foreach (var enemy in fight.Enemies) // 살아 있는 모든 적의 타격 결과 비교
            { // 대상별 체력 결과 시작
                Assert.AreEqual(184, enemy.CurrentHealth); // 첫 적과 나머지 적 모두 동일한 고정 기본 피해 열여섯
            } // 대상별 체력 결과 종료
            Assert.AreEqual(7, fight.Points.CurrentActionPoints); // 카드 지불 뒤 잔여와 타격 반응의 실제 AP 유지
        } // 실제 툴팁과 타격 수치 대조 종료
        yield return new ExitPlayMode(); // 독립 검사 전투 종료
    } // 광역 마법 피해 검사 종료

    [UnityTest] // 회복 중 AP 변경과 별개인 생존 아군 직접 회복
    public IEnumerator GoldenRuleHealsUsingOnePostPaymentBalanceWithoutSpendingIt() // 회복량과 공용 AP 잔여 및 전투불능 제외 확인
    { // 전체 직접 회복 검사 시작
        yield return new EnterPlayMode(); // 실제 회복 경로와 손패 입력 활성화
        using (var fight = new Fight("GOLDEN_RULE")) // 실제 전설 펜타클과 아군 세 명
        { // 지불 전 현재 행동력 및 아군 체력 준비 시작
            Assert.IsTrue(fight.Points.Spend(13)); // AP 일곱에서 황금률 비용 넷을 지불할 비교값 준비
            var first = fight.Allies[0]; // 먼저 직접 회복할 실제 카드 소유자
            var second = fight.Allies[1]; // 첫 생존 회복 뒤 실제로 처리할 동료
            var fallen = fight.Allies[2]; // 일반 회복에 포함되지 않는 전투불능 아군
            first.ApplyPersistentHealth(5); // 기본 체력과 상한을 분리할 원래 부상
            second.ApplyPersistentHealth(5); // 두 대상의 실제 회복량 비교 기준
            fallen.ApplyPersistentHealth(0); // 일반 전체 회복을 받을 수 없는 상태
            first.HealthChanged += _ => fight.Points.Gain(3); // 첫 실제 회복 중 공용 행동력이 증가하는 상태 재현
            yield return Play(fight, "GOLDEN_RULE"); // 정상 비용을 지불하고 생존 대상만 회복
            Assert.AreEqual(20, first.CurrentHealth); // 지불 뒤 공용 AP 셋과 배수 다섯의 회복
            Assert.AreEqual(20, second.CurrentHealth); // 앞선 회복 반응으로 행동력이 늘어도 같은 기본 회복량
            Assert.AreEqual(0, fallen.CurrentHealth); // 전투불능 아군에게 부활이나 직접 회복 없음
            Assert.AreEqual(6, fight.Points.CurrentActionPoints); // 비용만 지불하고 이전 잔여와 회복 반응을 그대로 유지
        } // 지불 전 현재 행동력 및 아군 체력 준비 종료
        yield return new ExitPlayMode(); // 독립 검사 전투 종료
    } // 전체 직접 회복 검사 종료

    [UnityTest] // 원래 생존 조건과 같은 턴 행동력 상한 확장
    public IEnumerator InfiniteContractPaysBothCostsAndOnlyExtendsThisTurnsCapacityAsNeeded() // 실패 보존과 복리 및 정상 다음 턴 경계 확인
    { // 실제 X 계약 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 행동력 최대치의 플레이 모드 활성화
        using (var fight = new Fight("COMPOUND_EFFECT", "INFINITE_CONTRACT", "INFINITE_CONTRACT")) // 두 실제 계약과 현재 턴 한정 복리
        { // 체력 불가와 기존 상한 안의 회복부터 비교 시작
            yield return Play(fight, "COMPOUND_EFFECT"); // 회복당 행동력 한 점 복리 적용
            Assert.IsTrue(fight.Points.Spend(11)); // 행동력 여섯의 첫 계약 지불 준비
            var owner = fight.Allies[0]; // 원래 체력 비용의 실제 시전자
            owner.ApplyPersistentHealth(24); // 여섯의 체력 비용과 같은 상태
            owner.GainBlock(5); // 체력 거래와 별도의 원래 방어도
            var view = fight.View("INFINITE_CONTRACT"); // 실제 가변 행동력 카드
            Assert.AreEqual("X", view.RuntimeCard.CostLabel); // 정액 표기와 구분되는 실제 X 비용 표시
            view.OnPointerClick(null); // 생존이 불가능한 계약을 정상 카드 입력에서 시도
            yield return null; // 실패가 실제 화면에 반영되는 프레임 확인
            Assert.AreEqual(6, fight.Points.CurrentActionPoints); // 실패 카드의 실제 행동력 차감 없음
            Assert.AreEqual(24, owner.CurrentHealth); // 실패 카드의 실제 체력 결제 없음
            CollectionAssert.Contains(fight.Deck.Hand, view.RuntimeCard); // 비용을 내지 못한 실물 손패 유지
            Assert.AreEqual(1, owner.RestoreHealth(1)); // 손패에 알릴 회복으로 첫 계약의 생존 조건 충족
            int pointsAtHealthPayment = -1; // 두 지급이 함께 적용된 것을 확인할 이벤트 관찰 값
            owner.HealthChanged += _ => pointsAtHealthPayment = fight.Points.CurrentActionPoints; // 실체력 결제 알림이 보는 원래 행동력 잔여
            yield return Play(fight, "INFINITE_CONTRACT"); // 행동력 전액과 체력 비용을 같이 지불
            Assert.AreEqual(0, pointsAtHealthPayment); // 원래 체력 알림 시점에 전액 AP 결제 반영
            Assert.AreEqual(1, owner.CurrentHealth); // 넷 배의 실제 체력 지불 뒤 생존
            Assert.AreEqual(5, owner.CurrentBlock); // 체력 비용이 기존 방어도를 그대로 유지
            Assert.AreEqual(13, fight.Points.CurrentActionPoints); // 기존 상한 여유의 열두 점과 복리 한 점 지급
            Assert.AreEqual(20, fight.Points.MaxActionPoints); // 지급이 상한 안에 들어오면 최대 행동력 유지
            Assert.IsTrue(fight.Points.Spend(2)); // 행동력 열하나의 두 번째 계약 준비
            Assert.GreaterOrEqual(owner.MaxHealth, 45); // 실제 시전자가 두 번째 비용을 지불할 수 있는 검사 조건 확인
            Assert.AreEqual(44, owner.RestoreHealth(44)); // 두 번째 계약도 현재 손패에 표시되는 생존 가능 체력 준비
            yield return Play(fight, "INFINITE_CONTRACT"); // 원래 상한보다 큰 행동력 지급 정산
            Assert.AreEqual(22, fight.Points.CurrentActionPoints); // 두 배의 원본 지급과 상한을 넘는 복리 차단
            Assert.AreEqual(22, fight.Points.MaxActionPoints); // 이번 실제 턴에 필요한 두 점만 최대치 증가
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 최대치 확장을 회수하는 아군 턴 경계
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴의 원래 초기 행동력 복원
            Assert.AreEqual(20, fight.Points.MaxActionPoints); // 지난 턴 계약의 상한 이월 없음
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 다음 아군 턴의 기존 공용 행동력
        } // 체력 불가와 기존 상한 안의 회복 비교 종료
        yield return new ExitPlayMode(); // 독립 검사 전투 종료
    } // 실제 X 계약 검사 종료

    private static IEnumerator Play(Fight fight, string id) // 실제 손패를 통해 자기와 광역 원본 사용
    { // 실물 카드와 연출 결과 확인 시작
        var view = fight.View(id); // 사용할 원래 우선 손패 화면
        var original = view.RuntimeCard; // 실제 소모를 대조할 개별 카드 원본
        view.OnPointerClick(null); // 정상 게임의 실제 대상 규칙으로 카드 사용
        float deadline = Time.realtimeSinceStartup + 12f; // 원래 카드 연출과 정산 대기 한도
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 비용과 사용 효과를 정산할 실제 프레임 대기
        { // 전투 화면의 다음 프레임 진행 시작
            yield return null; // 원래 전투와 카드 사용의 실제 처리
        } // 전투 화면의 다음 프레임 진행 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 카드 사용 충돌과 후속 단계 완료
        CollectionAssert.DoesNotContain(fight.Deck.Hand, original); // 최초 손패 원본이 한 번만 실제 사용됐는지 확인
    } // 실물 카드와 연출 결과 확인 종료
} // 펜타클 자원 전투 검사 종료
