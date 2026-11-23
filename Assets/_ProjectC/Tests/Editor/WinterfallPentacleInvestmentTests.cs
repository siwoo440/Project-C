using System.Collections; // 실제 유니티 카드 실행 대기
using System.Linq; // 현재 손패의 실제 인스턴스 확인
using NUnit.Framework; // 행동력과 정상 상태 변화 검사
using UnityEngine; // 실제 카드를 훈련 상태에서 실행
using UnityEngine.TestTools; // 플레이 모드 전환 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투와 입력의 실제 시험 도구
using Object = UnityEngine.Object; // 대조 원본의 안전한 해제
using Random = UnityEngine.Random; // 실제 확률의 두 결과 재현

public sealed class WinterfallPentacleInvestmentTests // 투자와 도박 및 기계식 계산의 전투 회귀 검사
{ // 펜타클 경제 실전 검사 시작
    [UnityTest] // 실제 손패에서 결제 하한과 다음 턴의 결과 검사
    public IEnumerator CalculatedInvestmentRequiresTwoAndReturnsTheActualEntirePaymentNextAllyTurn() // 최소 행동력과 다음 아군 턴의 임시 확장
    { // 실제 전투 모드 시작
        yield return new EnterPlayMode(); // 실제 행동 카드와 공용 행동력 준비
        using (var fight = new Fight("CALCULATED_INVESTMENT", "EMERGENCY_FUNDS")) // 다음 턴 카드 둘을 가진 실제 회차 덱
        { // 두 행동력 지불 조건의 실제 비교 시작
            var investment = fight.View("CALCULATED_INVESTMENT").RuntimeCard; // 현재 실제 손패의 투자 원본
            Assert.AreEqual("2+", investment.CostLabel); // 실제 카드에 표시하는 고정 하한과 전액 비용
            Assert.IsTrue(fight.Points.Spend(19)); // 일반 하한보다 한 점 낮은 실제 행동력 설정
            fight.View("CALCULATED_INVESTMENT").OnPointerClick(null); // 최소 미달 상태의 실제 카드 클릭
            yield return null; // 화면 선택의 즉시 처리 완료 대기
            Assert.AreEqual(1, fight.Points.CurrentActionPoints); // 최소 미달의 지불과 행동력 보존
            CollectionAssert.Contains(fight.Deck.Hand, investment); // 하한 미달에 사용 원본 미소모
            Assert.AreEqual(4, fight.Points.Gain(4)); // 다음 행동에 쓸 가용 행동력을 다섯으로 조정
            yield return UseFinished(fight, "CALCULATED_INVESTMENT"); // 실제 행동력 전액 결제와 사용 정산
            Assert.Zero(fight.Points.CurrentActionPoints); // 인쇄 비용을 따로 중복 지불하지 않은 전액 계산
            Assert.AreEqual(20, fight.Points.MaxActionPoints); // 현재 턴에는 투자 배분 없음
            yield return UseFinished(fight, "EMERGENCY_FUNDS"); // 행동력이 영이어도 실제 무료 예약의 병용
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴 진입
            Assert.AreEqual(20, fight.Points.MaxActionPoints); // 적 턴에는 이전 투자 보상 대기
            Assert.Zero(fight.Points.CurrentActionPoints); // 적 턴에는 공용 행동력 지급 없음
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴의 회복과 투자 보상
            Assert.AreEqual(26, fight.Points.MaxActionPoints); // 실제 다섯 점 투자와 예비 자금 한 점 합산
            Assert.AreEqual(26, fight.Points.CurrentActionPoints); // 공용 현재 행동력의 동일 금액 지급
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 수령 턴의 임시 상한 회수 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 턴에서 이중 지급 검사
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 일시 보상을 한 번만 지급
        } // 두 행동력 지불 조건의 실제 비교 종료
        yield return new ExitPlayMode(); // 실제 전투의 정리를 마친 편집 모드 복귀
    } // 투자 카드의 실제 하한과 재지급 회귀 종료

    [UnityTest] // 한 카드로 받는 각각의 실제 50퍼센트 결과 검사
    public IEnumerator GambleMoveMakesOneSuccessOrFailureRollAfterAnActualUse() // 행동력 회복과 명확한 정신력 비용 비교
    { // 실제 카드 입력이 가능한 모드 시작
        yield return new EnterPlayMode(); // 원래 손패와 시전자의 자원 준비
        using (var fight = new Fight("GAMBLE_MOVE", "GAMBLE_MOVE")) // 다른 실물로 두 가지 유효 결과 비교
        { // 도박의 실제 정산 비교 시작
            Assert.IsTrue(fight.Points.Spend(5)); // 성공에서 행동력 상한 미충돌
            var owner = fight.Allies[0]; // 도박수를 사용할 실제 소유 아군
            int mental = owner.CurrentMental; // 고정 비용 정책을 확인할 원래 정신력
            var success = ChooseSeed(true); // 한 번의 실제 확률 성공 난수 준비
            Random.InitState(success.Seed); // 성공의 원래 난수 위치 복원
            yield return UseFinished(fight, "GAMBLE_MOVE"); // 첫 실제 도박 성공 정산
            Assert.AreEqual(16, fight.Points.CurrentActionPoints); // 행동력 한 점 사용 뒤 두 점 회복
            Assert.AreEqual(mental, owner.CurrentMental); // 성공 때 시전자 정신력 대가 없음
            Assert.AreEqual(success.After, Random.state); // 성공의 불필요한 재추첨과 추가 비용 없음
            var failure = ChooseSeed(false); // 한 번의 실제 확률 실패 난수 준비
            Random.InitState(failure.Seed); // 실패의 원래 난수 위치 복원
            yield return UseFinished(fight, "GAMBLE_MOVE"); // 둘째 실제 도박 실패 정산
            Assert.AreEqual(15, fight.Points.CurrentActionPoints); // 실패에서는 행동력 한 점만 결제
            Assert.AreEqual(mental - 5, owner.CurrentMental); // 시전자의 독립 직접 정신력 대가
            Assert.AreEqual(failure.After, Random.state); // 실패에서 한 번만 사용한 확률 확인
        } // 도박의 실제 정산 비교 종료
        yield return new ExitPlayMode(); // 다른 검사에 결과가 남지 않는 편집 모드 복귀
    } // 도박의 실제 공격과 확률 회귀 종료

    [UnityTest] // 실제 재가격과 한 턴 수명을 조합하는 검사
    public IEnumerator MechanicalCalculationOnlyDiscountsFixedPentacleBaseAndDoesNotStackOrCarry() // 일반 펜타클 기본 비용 범위와 복원
    { // 실제 손패가 있는 비교 전투 시작
        yield return new EnterPlayMode(); // 실제 카드와 턴 경계를 쓰는 모드
        using (var fight = new Fight("MECHANICAL_CALCULATION", "MECHANICAL_CALCULATION", "CONTRACT", "CHARGE", "MANA_RELEASE", "WHEEL_OF_FATE", "CALCULATED_INVESTMENT")) // 같은 타입과 다른 타입 및 전액 비용의 실제 비교
        { // 일반 펜타클 비용 변경 시작
            var contract = fight.View("CONTRACT").RuntimeCard; // 일반 비용을 쓰는 펜타클
            var charge = fight.View("CHARGE").RuntimeCard; // 이번 효과 범위 밖의 검 카드
            int normalPentacle = contract.ApCost; // 할인 전 펜타클의 기본 결제 기준
            int normalSword = charge.ApCost; // 할인 전 검 카드의 결제 기준
            yield return UseFinished(fight, "MECHANICAL_CALCULATION"); // 실제 첫 기계식 계산을 끝낸 비용
            Assert.AreEqual(18, fight.Points.CurrentActionPoints); // 자신의 결제 전에는 할인 미적용
            Assert.AreEqual(normalPentacle - 1, contract.ApCost); // 같은 실물의 일반 펜타클 기본 비용 한 점 감소
            Assert.AreEqual(normalSword, charge.ApCost); // 다른 속성 카드의 원래 비용 유지
            Assert.AreEqual("X", fight.View("MANA_RELEASE").RuntimeCard.CostLabel); // 가변 지팡이의 전액 비용 보존
            Assert.AreEqual("X", fight.View("WHEEL_OF_FATE").RuntimeCard.CostLabel); // 펜타클의 가변 비용 보존
            Assert.AreEqual("2+", fight.View("CALCULATED_INVESTMENT").RuntimeCard.CostLabel); // 투자 전액 사용의 고정 최소 비용 유지
            CheckOrdinaryCostForAnotherOwner(fight.Allies[1]); // 다른 아군의 펜타클 비용과 별도 대가 비교
            yield return UseFinished(fight, "MECHANICAL_CALCULATION"); // 할인 중 실제 두 번째 같은 원본 사용
            Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 두 번째 원본에도 일반 한 점 할인만 적용
            Assert.AreEqual(normalPentacle - 1, contract.ApCost); // 같은 효과 반복의 중복 할인 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴 종료의 비용 초기화
            Assert.AreEqual(normalPentacle, contract.ApCost); // 적 턴부터 기존 펜타클 비용 복원
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴에서 이전 할인 확인
            Assert.AreEqual(normalPentacle, contract.ApCost); // 다음 드로우에 이전 턴 할인 전달 없음
        } // 일반 펜타클 비용 변경 종료
        yield return new ExitPlayMode(); // 독립 회귀 검사 종료
    } // 펜타클 비용 할인 회귀 종료

    private static void CheckOrdinaryCostForAnotherOwner(BattleUnitRuntime owner) // 원래 소유자가 다른 기본 및 추가 비용 비교
    { // 최소 대조 원본 생성 시작
        var data = ScriptableObject.CreateInstance<CardData>(); // 추가 비용이 있는 합법 게임 카드의 시험 원본
        try // 시험 원본 정리 보장
        { // 현재 손패 밖의 실제 소유자 비용 비교 시작
            JsonUtility.FromJsonOverwrite($"{{\"cardType\":{(int)CardType.Pentacle},\"apCost\":2,\"additionalActionPointCost\":3}}", data); // 일반 기본 비용 둘과 별도 대가 셋
            var instance = CardInstance.Create(data, owner, 0, 0); // 같은 아군팀의 다른 실제 소유자 인스턴스
            Assert.AreEqual(4, instance.ApCost); // 기본 비용 한 점만 할인되고 추가 행동력 셋 그대로
        } // 소유자 비용 비교 종료
        finally // 게임 원본과 별도의 시험 카드만 해제
        { // 임시 원본 정리 시작
            Object.Destroy(data); // 생성한 비교 객체 파기
        } // 임시 원본 정리 종료
    } // 최소 대조 원본 생성 종료

    private static IEnumerator UseFinished(Fight fight, string id) // 기존 실제 카드 UI로 사용하고 정산까지 대기
    { // 실제 지원 카드 입력 시작
        var view = fight.View(id); // 목적 카드의 현재 손패 화면
        var card = view.RuntimeCard; // 사용하는 실제 물리 카드
        view.OnPointerClick(null); // 실제 손패 사용 버튼과 행동력 결제
        float deadline = Time.realtimeSinceStartup + 12f; // 일반 연출의 실제 완료 한도
        while ((fight.Runner.IsBusy || fight.Deck.Hand.Contains(card)) && Time.realtimeSinceStartup < deadline) // 사용이 검증된 이동까지 비교
        { // 정상 연출 대기 시작
            yield return null; // 다음 유니티 프레임 대기
        } // 정상 연출 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 연출을 끝냈는지 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, card); // 사용이 확정된 실제 버린 카드 조회
    } // 실제 지원 카드 입력 종료

    private static (int Seed, Random.State After) ChooseSeed(bool success) // 도박 확률의 실제 난수 초기값 추적
    { // 실제 확률 후보 검증 시작
        for (int seed = 0; seed < 1000; seed++) // 정해진 범위의 실제 시드 순회
        { // 후보의 실제 오십 퍼센트 결과 확인
            Random.InitState(seed); // 실제 유니티 난수의 후보 초기화
            if ((Random.Range(0, 100) < 50) == success) // 원본과 같은 하나의 확률 판정 결과 비교
            { // 요구한 실제 판정의 상태 기록 시작
                return (seed, Random.state); // 판정 이전 시드와 한 번 이후 상태의 쌍
            } // 요구한 실제 판정의 상태 기록 종료
        } // 후보의 실제 오십 퍼센트 결과 종료
        throw new System.InvalidOperationException("Gamble fixture seed missing."); // 시험 시드의 누락을 확인
    } // 실제 확률 후보 검증 종료
} // 펜타클 경제 실전 검사 종료
