using System.Collections; // 실제 손패 선택 및 카드 연출 대기
using System.Linq; // 정확한 실물 카드와 선택 모달 조회
using NUnit.Framework; // 카드 영역과 실제 피해 및 지불 확인
using UnityEngine; // 실제 전투의 손패와 선택 화면 조회
using UnityEngine.TestTools; // 플레이 모드 동작 검사
using UnityEngine.UI; // 실제 선택 화면의 버튼 입력
using Fight = WinterfallSwordCombatTests.Fight; // 실제 원본 카드와 대상 화면의 공용 검사 환경

public sealed class WinterfallPentacleHandChoiceTests // 전술 교환과 카드 강화의 실제 손패 회귀 검사
{ // 두 펜타클 입력의 실제 검사 시작
    [UnityTest] // 원래 카드의 부족 후보와 두 단계의 취소 및 별개 손패 우선 드로우 검사
    public IEnumerator TacticalExchangePaysAfterTwoDistinctChoicesAndReplacesWithOtherCards() // 두 신체 카드의 버림 및 실제 다른 우선 결과 확인
    { // 실제 전술 교환 버튼 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 선택 창 입력 환경 진입
        using (var shortHand = new Fight("TACTICAL_EXCHANGE", "THRUST")) // 두 번째 실제 후보가 부족한 짧은 손패
        { // 선택 수 부족의 조작 처리 시작
            int points = shortHand.Points.CurrentActionPoints; // 부족한 선택 시 유지할 공용 행동력
            shortHand.View("TACTICAL_EXCHANGE").OnPointerClick(null); // 아직 다른 카드가 한 장뿐인 실제 원본 클릭
            Assert.IsNull(Object.FindFirstObjectByType<BattleDrawChoiceView>()); // 부족한 다른 실물에 모달을 열지 않음 확인
            Assert.AreEqual(points, shortHand.Points.CurrentActionPoints); // 부족한 후보에 비용 미지불 확인
            Assert.AreEqual(2, shortHand.Deck.HandCount); // 원래 두 실물 카드의 손패 보존 확인
        } // 선택 수 부족의 조작 처리 종료
        using (var fight = new Fight("TACTICAL_EXCHANGE", "THRUST", "KNEE_SLASH", "BLOOD_EDGE", "POMMEL_STRIKE")) // 다른 교체 후보가 버림 더미에도 있는 실제 전투
        { // 원래 선택과 두 카드의 실제 이동 확인 시작
            var source = fight.View("TACTICAL_EXCHANGE").RuntimeCard; // 실제 전술 교환의 인스턴스 확인
            var alternatives = new[] { fight.View("THRUST").RuntimeCard, fight.View("KNEE_SLASH").RuntimeCard }; // 새로 보충될 별도의 두 카드
            foreach (var card in alternatives) // 원래 현재 손패의 두 실물에서 비교 후보 확보
            { // 실제 버림 더미의 후보 준비 시작
                Assert.IsTrue(fight.Deck.DiscardCard(card)); // 기존 효과 버림으로 미래 보충 후보 등록
            } // 실제 버림 더미의 후보 준비 종료
            var costs = fight.Deck.Hand.Where(card => card != source).ToArray(); // 정확히 두 장 남은 실제 손패 비용
            int points = fight.Points.CurrentActionPoints; // 드로우와 버림 후 비교할 실제 공용 행동력
            fight.View("TACTICAL_EXCHANGE").OnPointerClick(null); // 실제 전술 교환의 첫 단계 열기
            Assert.IsFalse(fight.Turns.EndPlayerTurn()); // 실제 미확정 모달 동안 턴 종료 거절 확인
            Choose("Choice_0"); // 원본과 다른 실물 첫 장 확인
            yield return null; // 닫힌 이전 모달의 정리 이후 실제 다음 화면 확인
            Choose("CancelChoice"); // 비용을 지불하기 전에 두 번째 후보 화면 취소
            yield return null; // 선택 잠금 해제와 화면 정리 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 두 번째 화면 취소 시 공용 지불 없음 확인
            CollectionAssert.AreEquivalent(new[] { source, costs[0], costs[1] }, fight.Deck.Hand); // 첫 선택도 버리지 않은 원래 손패 확인
            fight.View("TACTICAL_EXCHANGE").OnPointerClick(null); // 똑같은 물리 손패에서 다시 시도
            Choose("Choice_0"); // 먼저 선택한 실물을 이번에는 최종 선택까지 보관
            yield return null; // 이전 모달 대신 두 번째 단계 대기
            Assert.IsNull(Choice().transform.Find("Choice_1")); // 두 번째 단계에서 같은 실물과 원본 제외 확인
            Choose("Choice_0"); // 비교할 두 번째 별개 실물 카드 선택 완료
            yield return WaitForCard(fight); // 실제 플레이로 두 장 버림 및 공용 두 장 드로우 완료
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 유효 두 장의 최종 사용에서만 행동력 한 점 지불 확인
            CollectionAssert.AreEquivalent(alternatives, fight.Deck.Hand); // 충분한 별도 후보 두 장을 우선 뽑았는지 확인
            CollectionAssert.IsSubsetOf(new[] { source, costs[0], costs[1] }, fight.Deck.DiscardPile); // 원본의 정상 사용 및 다른 두 장의 실제 버림 위치 확인
            Assert.IsTrue(fight.Deck.IsStateValid()); // 공유 전투 덱의 실제 유일 카드 위치 확인
        } // 원래 선택과 두 카드의 실제 이동 확인 종료
        yield return new ExitPlayMode(); // 실제 전술 교환 검사 종료
    } // 실제 전술 교환 버튼 검사 종료

    [UnityTest] // 첫 선택 뒤 지워진 후보를 골라도 결제와 부분 버림 없음 확인
    public IEnumerator TacticalExchangeRejectsAStaleSecondChoiceWithoutPartiallyDiscarding() // 실제 카드 위치 변경 뒤 원래 선택의 비용 보호 확인
    { // 두 번째 카드의 오래된 버튼 검사 시작
        yield return new EnterPlayMode(); // 선택 화면의 실제 전투 환경 진입
        using (var fight = new Fight("TACTICAL_EXCHANGE", "THRUST", "KNEE_SLASH")) // 서로 다른 두 후보만 있는 원래 손패
        { // 후보가 이동한 뒤 오래된 선택 조작 시작
            var source = fight.View("TACTICAL_EXCHANGE").RuntimeCard; // 실제 사용 전 원본 카드
            var candidates = fight.Deck.Hand.Where(card => card != source).ToArray(); // 모달에 같은 순서로 표시될 두 실제 후보
            int points = fight.Points.CurrentActionPoints; // 무효 선택에서 보존할 실제 행동력
            fight.View("TACTICAL_EXCHANGE").OnPointerClick(null); // 원래 두 후보의 첫 실제 선택 시작
            Choose("Choice_0"); // 실제 손패 첫 후보 기억
            yield return null; // 두 번째 선택 화면의 실제 오브젝트 대기
            var secondButton = Choice().transform.Find("Choice_0").GetComponent<Button>(); // 후속 입력을 흉내 낼 실제 오래된 버튼
            Assert.IsTrue(fight.Deck.DiscardCard(candidates[1])); // 외부 효과로 두 번째 실제 선택의 손패 이탈
            secondButton.onClick.Invoke(); // 이동하기 전 화면에 남은 실제 입력 시도
            yield return null; // 거부 뒤 카드 선택 창 정리 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 유효 후보가 하나로 줄어든 뒤의 결제 금지 확인
            CollectionAssert.AreEquivalent(new[] { source, candidates[0] }, fight.Deck.Hand); // 남은 첫 선택과 원래 교환 카드의 물리 보존 확인
            Assert.IsNull(Object.FindFirstObjectByType<BattleDrawChoiceView>()); // 무효 선택 이후 원래 입력 잠금 해제 확인
        } // 후보가 이동한 뒤 오래된 선택 조작 종료
        yield return new ExitPlayMode(); // 선택 무효 사례의 플레이 모드 종료
    } // 두 번째 카드의 오래된 버튼 검사 종료

    [UnityTest] // 같은 원본의 다른 실물 및 가격과 변경 후 실제 첫 사용 비교
    public IEnumerator EnchantCardChangesOnlyTheChosenPhysicalCardsNextNumericUse() // 실제 두 카드의 원본 피해와 사용 뒤 강화 제거 확인
    { // 단발 카드 강화의 실제 손패 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 적 대상 입력 환경 진입
        using (var fight = new Fight("ENCHANT_CARD", "ICE_ARROW", "ICE_ARROW")) // 같은 원본의 서로 다른 지팡이 실물 두 장
        { // 취소와 확정 및 실제 두 타격 비교 시작
            var source = fight.View("ENCHANT_CARD").RuntimeCard; // 실제 카드 강화 원본 인스턴스
            var wands = fight.Deck.Hand.Where(card => card != source).ToArray(); // 같은 원본을 독립 비교할 실제 지팡이 두 장
            var marked = wands[0]; // 실제 강화 선택의 첫 번째 지팡이
            Assert.AreSame(marked.SourceData, wands[1].SourceData); // 두 실제 인스턴스가 같은 원본 자료를 쓰는지 확인
            int effect = marked.EffectValue; // 변경 전 실제 카드 숫자
            int cost = marked.ApCost; // 강화와 독립인 원래 사용 비용
            int points = fight.Points.CurrentActionPoints; // 취소 및 실제 원본 사용의 행동력 비교 기준
            fight.View("ENCHANT_CARD").OnPointerClick(null); // 실제 단일 손패 선택 열기
            Choose("CancelChoice"); // 아직 선택하지 않은 카드 강화 취소
            yield return null; // 취소 화면과 실제 손패 잠금 정리 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 취소 시 실제 원래 행동력 보존 확인
            Assert.AreEqual(0, marked.EnchantmentPercent); // 취소된 카드 강화로 예약 미생성 확인
            fight.View("ENCHANT_CARD").OnPointerClick(null); // 같은 원래 손패에서 정상 카드 강화 선택
            Choose("Choice_0"); // 실제 같은 원본 두 장 중 첫 실물만 선택
            yield return WaitForCard(fight); // 원래 카드의 실제 사용과 손패 이동 완료 대기
            Assert.AreEqual(points - 2, fight.Points.CurrentActionPoints); // 카드 강화의 실제 비용 두 점 확인
            Assert.AreEqual(50, marked.EnchantmentPercent); // 선택한 실물의 독립 오십 퍼센트 예약 확인
            Assert.AreEqual(effect * 3 / 2, marked.EffectValue); // 실제 원래 기본 효과의 내림 오십 퍼센트 증가 확인
            Assert.AreEqual(effect, wands[1].EffectValue); // 같은 원본의 다른 실물 수치 보존 확인
            Assert.AreEqual(cost, marked.ApCost); // 강화와 무관한 원래 행동력 비용 확인
            int actualRaw = 0; // 실제 명중이 사용한 원본 수치 검사 변수
            fight.Enemies[0].DamageTaken += (_, result) => actualRaw = result.RawDamage; // 실제 명중 이벤트에서 피해 계산 수치 기록
            PlayOnEnemy(fight, marked); // 선택했던 정확한 실제 강화 지팡이로 적 공격
            yield return WaitForCard(fight); // 정상 명중 및 실제 원본의 손패 이탈 완료
            Assert.AreEqual(effect * 3 / 2, actualRaw); // 이번 실제 공격의 원래 기본 수치에만 강화가 사용됐는지 확인
            Assert.AreEqual(0, marked.EnchantmentPercent); // 실제 사용한 한 장의 예약 제거 확인
            Assert.AreEqual(effect, marked.EffectValue); // 다음 재드로우에서 사용할 원래 수치 복원 확인
            PlayOnEnemy(fight, wands[1]); // 동일한 원본이지만 강화하지 않은 실물 사용
            yield return WaitForCard(fight); // 원래 두 번째 카드의 실제 피해 정산
            Assert.AreEqual(effect, actualRaw); // 처리하지 않은 두 번째 실물의 정상 기본 피해 확인
        } // 취소와 확정 및 실제 두 타격 비교 종료
        yield return new ExitPlayMode(); // 실제 카드 강화의 플레이 모드 검사 종료
    } // 단발 카드 강화의 실제 손패 검사 종료

    private static BattleDrawChoiceView Choice() // 현재 전투의 실제 손패 선택 화면 조회
    { // 하나의 유효 모달 검사 시작
        return Object.FindObjectsByType<BattleDrawChoiceView>(FindObjectsSortMode.None).Single(view => view.gameObject.activeInHierarchy); // 현재 입력 가능한 원래 손패 선택 창 하나 반환
    } // 하나의 유효 모달 검사 종료

    private static void Choose(string button) // 실제 손패 후보나 취소 버튼 확인
    { // 기존 모달 버튼 입력 시작
        Choice().transform.Find(button).GetComponent<Button>().onClick.Invoke(); // 해당 실제 원래 화면의 버튼 누름 전달
    } // 기존 모달 버튼 입력 종료

    private static void PlayOnEnemy(Fight fight, CardInstance card) // 실제 지정한 손패 인스턴스의 공격 입력
    { // 원래 플레이용 화면과 적 입력 시작
        fight.Hand.GetComponentsInChildren<BattleCardView>().Single(view => view.RuntimeCard == card).OnPointerClick(null); // 같은 원본과 구분되는 정확한 실물 카드 화면 클릭
        fight.EnemyViews[0].OnPointerClick(null); // 실제 일인 대상 적으로 공격 확정
    } // 원래 플레이용 화면과 적 입력 종료

    private static IEnumerator WaitForCard(Fight fight) // 실제 카드 사용의 정상 연출 종료 대기
    { // 프레임 대기와 원래 카드 영역 확인 시작
        float until = Time.realtimeSinceStartup + 10f; // 기존 카드 연출의 최대 검사 대기 시간
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 실제 카드의 원래 연출이 남아 있는지 확인
        { // 다음 실제 모드 프레임 시작
            yield return null; // 실제 카드 충돌과 카드 영역 변경 대기
        } // 다음 실제 모드 프레임 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 연출과 정산 완료 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 사용 이후 전체 카드의 정상 위치 확인
    } // 프레임 대기와 원래 카드 영역 확인 종료
} // 두 펜타클 입력의 실제 검사 종료
