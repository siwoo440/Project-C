using System.Collections; // 실제 카드 연출과 모드 전환 대기
using System.Linq; // 살아 있는 실제 아군 화면 선택
using NUnit.Framework; // 카드 수치와 턴 결과의 검증
using UnityEngine; // 전투 화면과 다음 프레임 경계
using UnityEngine.TestTools; // 실제 손패 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 원래 전투 입력과 자원의 공통 환경

public sealed class WinterfallSageRuneTests // 현자의 룬의 실제 행동력과 공용 카드 효과 검증
{ // 원본 룬 검사 시작
    [UnityTest] // 행동력과 피해 및 같은 턴의 팀 전체 강화 확인
    public IEnumerator SageRuneAmplifiesThePartyOnceUntilTheActualTurnEnds() // 반복 사용과 다음 턴의 기준 수치 대조
    { // 실제 룬과 피해 카드 검사 시작
        yield return new EnterPlayMode(); // 정상 카드 입력이 있는 전투 환경 시작
        using (var fight = new Fight("SAGE_RUNE", "SAGE_RUNE", "RUNE_BURST", "RUNE_BURST")) // 룬 두 장과 실제 피해 원본 두 장 구성
        { // 같은 턴과 다른 시전자 및 종료 확인 시작
            var damageCard = fight.View("RUNE_BURST").RuntimeCard; // 기본 피해의 실제 손패 카드
            int printed = damageCard.EffectValue; // 룬을 쓰기 전 원래 피해 원본
            int cost = damageCard.ApCost; // 별도로 보존할 원래 실제 비용
            var other = CardInstance.Create(damageCard.SourceData, fight.Allies[1], 0, 0); // 전투 참가 중인 두 번째 실제 아군의 같은 원본
            Assert.AreEqual(printed, other.EffectValue); // 사용 전 공용 강화 없음 확인
            yield return Click(fight, "SAGE_RUNE"); // 원래 비용을 쓰고 룬의 실제 효과 정산
            Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 비용 다섯 뒤 기존 상한 안에서 행동력 두 점 회복
            Assert.AreEqual(17, damageCard.EffectValue); // 홀수 원래 피해 열하나의 오십 퍼센트 반올림
            Assert.AreEqual(17, other.EffectValue); // 같은 전투의 다른 실제 아군도 동일한 카드 강화
            Assert.AreEqual(cost, damageCard.ApCost); // 카드 효과 강화와 별개인 원래 AP 지불 기준
            yield return Click(fight, "SAGE_RUNE"); // 같은 턴 룬 두 번째 단일 정산
            Assert.AreEqual(14, fight.Points.CurrentActionPoints); // 반복 룬도 자기 행동력 두 점만 회복
            Assert.AreEqual(17, damageCard.EffectValue); // 같은 턴 반복 룬의 비중첩 확인
            yield return fight.Use("RUNE_BURST", 0); // 소유자의 실제 기본 마법 명중
            Assert.AreEqual(183, fight.Enemies[0].CurrentHealth); // 실제 원래 마법 피해에 오십 퍼센트 적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 종료와 다음 아군 턴의 실제 공통 정리
            Assert.AreEqual(printed, other.EffectValue); // 새 아군 턴에 다른 소유자의 원래 기본 효과 복귀
            yield return fight.Use("RUNE_BURST", 1); // 새 턴의 같은 정상 마법 명중
            Assert.AreEqual(189, fight.Enemies[1].CurrentHealth); // 계속되는 전투에도 이전 룬 미이월
        } // 같은 턴과 다른 시전자 및 종료 확인 끝
        yield return new ExitPlayMode(); // 실제 카드 테스트 환경 정리
    } // 실제 룬과 피해 카드 검사 종료

    [UnityTest] // 두 양수 카드 효과와 이미 있는 확률형 상태 및 비용 검사
    public IEnumerator SageRuneStrengthensAuthoredBlockDrawAndHealingWithoutScalingCostsOrPercentages() // 여러 효과 경로와 원래 비용 기간의 대조
    { // 실제 보급 및 회복 카드 검사 시작
        yield return new EnterPlayMode(); // 정상 손패와 아군 화면 준비
        using (var fight = new Fight("SAGE_RUNE", "SAGE_RUNE", "SECURE_SUPPLIES", "LIFE_CONTRACT", "WEAKNESS_ANALYSIS")) // 두 룬과 원래 수치가 다른 세 카드
        { // 직접 피해 이외의 실제 정산 확인 시작
            var owner = fight.Allies[0]; // 룬과 보급 원본을 쓰는 시전자
            var target = fight.Allies[1]; // 생명의 계약이 지정할 실제 동료
            target.ApplyPersistentHealth(5); // 치료량을 분명하게 확인할 부상 상태
            var contract = fight.View("LIFE_CONTRACT").RuntimeCard; // 기존 체력 회복량과 정신력 비용을 모두 가진 실제 원본
            int printedHealing = contract.EffectValue; // 룬 적용 전 일반 회복 숫자
            int printedCost = contract.ApCost; // 실제 원래 카드 비용
            int printedMentalCost = contract.SourceData.MentalChangeValue; // 별도 원래 정신력 비용
            int mentalBefore = owner.CurrentMental; // 지불 전 시전자의 실제 정신력
            yield return Click(fight, "SAGE_RUNE"); // 뽑을 버림 한 장을 남기는 실제 첫 룬
            yield return Click(fight, "SAGE_RUNE"); // 뽑을 버림 두 장을 준비하는 실제 두 번째 룬
            int handBefore = fight.Deck.HandCount; // 보급 사용과 추가 드로우를 대조할 원래 손패
            yield return Click(fight, "SECURE_SUPPLIES"); // 원본 보급의 실제 방어도와 카드 드로우
            Assert.AreEqual(5, owner.CurrentBlock); // 기본 방어도 셋에 반올림한 오십 퍼센트 적용
            Assert.AreEqual(handBefore + 1, fight.Deck.HandCount); // 보급 한 장 소비와 실제 두 장 드로우
            Assert.AreEqual(printedCost, contract.ApCost); // 생명의 계약 AP 비용 유지 확인
            Assert.AreEqual(printedMentalCost, contract.SourceData.MentalChangeValue); // 실제 정신력 대가 원본 유지 확인
            var allyView = fight.Hand.GetComponentInParent<Canvas>().GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == target); // 치료할 실제 생존 동료의 클릭 화면
            yield return Click(fight, "LIFE_CONTRACT", allyView); // 정상 원래 카드와 아군 선택 진행
            Assert.AreEqual(5 + (printedHealing * 3 + 1) / 2, target.CurrentHealth); // 카드 원본의 정수 반올림 회복량 확인
            Assert.AreEqual(mentalBefore + printedMentalCost, owner.CurrentMental); // 피격과 독립인 원래 정신력 비용 정확히 한 번 처리
            yield return fight.Use("WEAKNESS_ANALYSIS", 0); // 실제 취약 비율과 두 턴 원본 부여
            var vulnerable = fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Vulnerable); // 기존 비율 변형 상태 조회
            Assert.AreEqual(20, vulnerable.EffectiveValue); // 다른 피해 배율 자체의 원래 비율 유지
            Assert.AreEqual(2, vulnerable.RemainingTurns); // 카드가 정한 원래 기간 유지
        } // 직접 피해 이외의 실제 정산 확인 종료
        yield return new ExitPlayMode(); // 독립 전투 검사 정리
    } // 실제 보급 및 회복 카드 검사 종료

    private static IEnumerator Click(Fight fight, string id, BattleUnitView target = null) // 자원 이득이 있는 원래 카드 입력의 실제 완료 확인
    { // 원래 손패와 효과 정산 대기 시작
        var view = fight.View(id); // 현재 손패 실물의 실제 화면
        var card = view.RuntimeCard; // 사용 전 공식 실물 조회
        view.OnPointerClick(null); // 원래 카드 선택 또는 자기 효과 해결
        target?.OnPointerClick(null); // 필요한 경우 생존 실제 대상 선택
        float deadline = Time.realtimeSinceStartup + 12f; // 실제 카드 연출의 대기 한도
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 타격과 사용 완료까지 대기
        { // 다음 전투 프레임 진행 시작
            yield return null; // 실제 연출과 자원 처리 진행
        } // 다음 전투 프레임 진행 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 원래 전투 연출의 실제 완료 확인
        CollectionAssert.DoesNotContain(fight.Deck.Hand, card); // 실제 사용 원본이 정상 손패를 떠난 상태 확인
    } // 원래 손패와 효과 정산 대기 종료
} // 원본 룬 검사 종료
