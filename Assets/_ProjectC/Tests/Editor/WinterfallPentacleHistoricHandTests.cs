using System.Collections; // 실제 카드 입력 및 플레이 모드 대기
using System.Linq; // 현재 실물 카드와 실제 선택 화면 조회
using NUnit.Framework; // 실제 비용과 복제 결과 확인
using UnityEngine; // 원본 손패와 검사 카드의 자료 설정
using UnityEngine.TestTools; // 실제 전투의 플레이 모드 검사
using UnityEngine.UI; // 기존 선택 모달 버튼 실행
using Fight = WinterfallSwordCombatTests.Fight; // 실제 전투 화면과 덱의 공용 검사 환경

public sealed class WinterfallPentacleHistoricHandTests // 복제 주문과 룬 힘의 실제 실물 손패 검사
{ // 실제 카드 사용 및 룬 예약 회귀 검사 시작
    [UnityTest] // 현재 턴 실제 사용만 선택하고 복사 원본의 소유권과 단계 확인
    public IEnumerator CardCopyOffersOnlyCompletedCurrentTurnCardsAndCreatesAnIndependentBattleCopy() // 이전 턴과 취소 및 실제 사용 원본 비교
    { // 실제 복제 주문 모달 및 손패 검사 시작
        yield return new EnterPlayMode(); // 원본 카드와 선택 화면의 실제 전투 구동 시작
        using (var fight = new Fight("CARD_COPY", "THRUST", "KNEE_SLASH")) // 복제 주문과 서로 다른 두 원본의 실제 처음 손패
        { // 유효하지 않은 이전 이력과 실제 선택 확인 시작
            var copyCard = fight.View("CARD_COPY").RuntimeCard; // 실제 카드 복제 원본 인스턴스
            int before = fight.Points.CurrentActionPoints; // 사용하지 않은 첫 시도 이전 실제 행동력
            fight.View("CARD_COPY").OnPointerClick(null); // 어떤 카드도 쓰지 않은 실제 미달 선택 시도
            Assert.IsNull(Object.FindFirstObjectByType<BattleDrawChoiceView>()); // 미사용 카드의 현재 선택 후보 제외 확인
            Assert.AreEqual(before, fight.Points.CurrentActionPoints); // 복제할 실제 사용 내역 없이 행동력 미사용
            yield return fight.Use("KNEE_SLASH", 1); // 다음 실제 아군 턴에서는 보이면 안 되는 이전 턴 카드 사용
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴을 열어 새 이력으로 전환
            Assert.AreSame(copyCard, fight.View("CARD_COPY").RuntimeCard); // 정상 덱 재순환으로 돌아온 같은 복제 원본 확인
            var original = fight.View("THRUST").RuntimeCard; // 이번 턴에 실제 사용하고 복제할 물리 카드
            fight.View("CARD_COPY").OnPointerClick(null); // 이전 아군 턴 기록만 있는 새로운 턴의 선택 시도
            Assert.IsNull(Object.FindFirstObjectByType<BattleDrawChoiceView>()); // 지난 턴의 실제 사용 내역이 새 선택에 나오지 않음 확인
            yield return fight.Use("THRUST", 0); // 현재 실제 아군 턴에 복제할 원본 사용 완료
            int points = fight.Points.CurrentActionPoints; // 취소 이후 비교할 실제 남은 행동력
            int hand = fight.Deck.HandCount; // 복제 카드가 사용 전에 보존할 물리 손패 수
            fight.View("CARD_COPY").OnPointerClick(null); // 실제 사용 기록에서 한 장을 고르는 모달 진입
            Assert.IsFalse(fight.Turns.EndPlayerTurn()); // 미확정 복제 선택에서 실제 턴 종료 차단
            Assert.IsNull(Choice().transform.Find("Choice_1")); // 이번 턴에 안 쓴 물리 카드와 자기 자신 제외 확인
            Choose("CancelChoice"); // 실제 모달이 제공하는 무지불 취소 입력
            yield return null; // 닫힌 화면 정리와 다른 입력 가능 상태 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 취소한 복제 주문의 행동력 미지불 확인
            Assert.AreEqual(hand, fight.Deck.HandCount); // 취소한 복제 주문과 정상 후보의 손패 보존 확인
            fight.View("CARD_COPY").OnPointerClick(null); // 동일한 실제 사용 카드의 복제 선택 다시 열기
            Choose("Choice_0"); // 이전 턴이 아닌 이번 턴 원래 카드의 실제 선택 확정
            yield return WaitForCard(fight); // 복제 주문의 실제 사용과 복제 이동 완료 대기
            var copied = fight.Deck.Hand.Single(card => card.SourceData == original.SourceData); // 같은 원본 자료를 사용하는 새 실제 손패 한 장 확인
            Assert.AreEqual(points - 4, fight.Points.CurrentActionPoints); // 원래 복제 주문의 확정된 행동력 네 점 확인
            Assert.AreEqual(hand, fight.Deck.HandCount); // 사용 원본 한 장이 새 복제 한 장으로 교체된 실제 손패 확인
            Assert.AreNotSame(original, copied); // 원래 사용 실물과 별도의 실제 새 카드 확인
            Assert.AreNotEqual(original.InstanceId, copied.InstanceId); // 새 카드의 독립 고유 식별자 확인
            Assert.AreSame(original.OwnerUnit, copied.OwnerUnit); // 원래 사용자의 실제 소유권 그대로 유지 확인
            Assert.AreEqual(original.UpgradeLevel, copied.UpgradeLevel); // 원래 카드의 기존 강화 단계 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, original); // 실제 원본 사용 실물의 버림 위치 그대로 유지 확인
            Assert.IsTrue(fight.Deck.DiscardCard(copied)); // 새 물리 복제의 정상 효과 버림 허용 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, copied); // 별도 새 카드를 원래 재순환 카드 영역에 정상 등록
            foreach (var enemy in fight.Enemies) // 현재 실제 전투를 정상 사망으로 끝낼 적 전체 순회
            { // 실제 마지막 승리 정산 준비
                enemy.TakeDamage(1000, BattleDamageType.None); // 게임의 기존 승리 분기로 각 실제 적 제거
            } // 실제 마지막 승리 정산 준비 종료
            Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 정상 실제 전투 종료 확인
            CollectionAssert.DoesNotContain(fight.Deck.DiscardPile, copied); // 모든 전투 상태에서 일시 복제만 정리됐는지 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, original); // 원래 회차 소유 카드의 위치 보존 확인
        } // 유효하지 않은 이전 이력과 실제 선택 확인 종료
        yield return new ExitPlayMode(); // 실제 복제 주문 검증 모드 종료
    } // 실제 복제 주문 모달 및 손패 검사 종료

    [UnityTest] // 유효 소유자의 고정 기본 비용만 영으로 두고 사용까지 승계
    public IEnumerator RunePowerPreservesExtraCostAndSurvivesPhysicalDiscardAndRedraw() // 소유자 없는 대상과 가변 카드 제외 및 정산 뒤 일회 소모 확인
    { // 룬 힘의 실제 고정 비용 검사 시작
        yield return new EnterPlayMode(); // 원본 손패와 다른 아군 대상의 실제 입력 시작
        var shop = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 게임의 실제 펜타클 및 지팡이 카드 지급 원본
        var rune = shop.Offers.Single(offer => offer.OfferId == "REWARD_SHEET_RUNE_POWER").Card; // 새 룬 힘의 실제 자산 확인
        var variable = shop.Offers.Single(offer => offer.OfferId == "REWARD_SHEET_MANA_RELEASE").Card; // 실제 전액 행동력 비용 원본
        var payload = ScriptableObject.CreateInstance<CardData>(); // 기본 비용과 별도 행동력을 함께 가진 검사 원본
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"CRD_SHEET_RUNE_TEST_PAYLOAD\",\"displayName\":\"룬 힘 비용 검사\",\"apCost\":3,\"additionalActionPointCost\":2,\"cardType\":{(int)CardType.Sword},\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)CardEffectType.Damage},\"damageType\":{(int)BattleDamageType.Physical},\"effectValue\":5}}", payload); // 실제 전투 카드의 고정 기본과 별도 행동력 수치 지정
        try // 실제 임시 검사 원본을 정리하는 범위
        { // 다른 아군과 가변 비용 및 재드로우 비교 시작
            using var fight = new Fight(new[] { rune, variable, payload }); // 후보가 한 장만 있는 실제 동일 소유자 공유 손패
            var targetable = fight.View("RUNE_TEST_PAYLOAD").RuntimeCard; // 기본과 별도 행동력을 가진 실제 원본 한 장
            var variableCard = fight.View("MANA_RELEASE").RuntimeCard; // 룬 힘의 적용에서 빠질 실제 가변 카드
            int points = fight.Points.CurrentActionPoints; // 무효 대상과 정산의 실제 행동력 비교 기준
            var allyViews = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None); // 현재 실제 전투의 아군 클릭 화면들
            var other = allyViews.Single(view => view.RuntimeUnit == fight.Allies[1]); // 적격 카드를 소유하지 않은 다른 실제 아군 화면
            var actor = allyViews.Single(view => view.RuntimeUnit == fight.Allies[0]); // 유일한 적격 실물 카드를 소유한 실제 아군 화면
            fight.View("RUNE_POWER").OnPointerClick(null); // 룬 힘의 실제 단일 아군 입력 시작
            other.OnPointerClick(null); // 소유한 적격 실물이 없는 아군을 골라 거절 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 적격 실물이 없는 아군 선택의 무지불 확인
            actor.OnPointerClick(null); // 선택된 펜타클 카드의 현재 유일한 타당 아군을 선택
            yield return WaitForCard(fight); // 현재 실제 룬 힘 사용과 다른 카드의 비용 예약 확인 대기
            Assert.AreEqual(points - 2, fight.Points.CurrentActionPoints); // 룬 힘 사용 자체의 고정 행동력 두 점 확인
            Assert.IsTrue(targetable.HasRunePower); // 실제 유효한 다른 카드 한 장에만 기본 비용 면제 예약 확인
            Assert.AreEqual(2, targetable.ApCost); // 원래 추가 행동력 두 점이 지불 조건에 남는지 확인
            Assert.IsFalse(variableCard.HasRunePower); // X 비용 카드의 원래 가변 비용 예약 건드리지 않음 확인
            Assert.AreEqual("X", variableCard.CostLabel); // 실제 손패의 기존 가변 비용 표시 유지 확인
            Assert.IsTrue(fight.Deck.DiscardCard(targetable)); // 룬을 적용했지만 쓰지 않고 실제 효과 버림
            Assert.AreEqual(2, fight.Deck.DrawCards(2)); // 해당 원래 물리 카드와 다른 버림의 실제 재순환 드로우
            CollectionAssert.Contains(fight.Deck.Hand, targetable); // 다시 뽑은 카드가 같은 실제 인스턴스인지 확인
            Assert.AreEqual(2, targetable.ApCost); // 실제 버림과 재드로우로 기본 면제가 사라지지 않음 확인
            yield return fight.Use("RUNE_TEST_PAYLOAD", 0); // 예약된 물리 카드의 원래 효과 정상 사용
            Assert.AreEqual(points - 4, fight.Points.CurrentActionPoints); // 처음 행동력에서 룬과 별도 추가 비용만 각각 두 점 지불 확인
            Assert.IsFalse(targetable.HasRunePower); // 실제 한 번의 사용에서 룬 예약 소모 확인
            Assert.AreEqual(5, targetable.ApCost); // 재사용에서 기본 세 점과 별도 두 점의 원래 합 복원 확인
        } // 다른 아군과 가변 비용 및 재드로우 비교 종료
        finally // 임시 검사 원본 정리 보장
        { // 임시 원본 제거 시작
            Object.Destroy(payload); // 출시 원본에 영향 없이 실제 테스트 카드 메모리 해제
        } // 임시 원본 제거 종료
        yield return new ExitPlayMode(); // 룬 힘의 전투 검사 종료
    } // 룬 힘의 실제 고정 비용 검사 종료

    private static BattleDrawChoiceView Choice() // 현재 실제 복제 선택 화면 조회
    { // 활성 기존 모달 확인 시작
        return Object.FindObjectsByType<BattleDrawChoiceView>(FindObjectsSortMode.None).Single(view => view.gameObject.activeInHierarchy); // 실제 입력 중인 모달 하나 반환
    } // 활성 기존 모달 확인 종료

    private static void Choose(string button) // 선택 또는 취소의 실제 버튼 입력
    { // 기존 실제 UI의 눌림 시작
        Choice().transform.Find(button).GetComponent<Button>().onClick.Invoke(); // 현재 모달의 실제 지정 버튼 클릭
    } // 기존 실제 UI의 눌림 종료

    private static IEnumerator WaitForCard(Fight fight) // 실제 카드 연출의 전투 콜백 완료 대기
    { // 현재 카드 정상 정산 확인 시작
        float until = Time.realtimeSinceStartup + 10f; // 기존 연출의 결정적 테스트 최대 시간
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 실제 충돌과 손패 영역 업데이트 완료 대기
        { // 다음 전투 프레임 시작
            yield return null; // 실제 사용 처리의 다음 프레임 진행
        } // 다음 전투 프레임 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 최종 카드 연출 완료 상태 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 유효한 실물과 각 카드 영역의 일관성 확인
    } // 현재 카드 정상 정산 확인 종료
} // 실제 카드 사용 및 룬 예약 회귀 검사 종료
