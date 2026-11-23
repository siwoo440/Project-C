using System.Collections; // 실제 손패 카드 실행 대기
using System.Linq; // 실제 덱 속 카드와 영역 검색
using NUnit.Framework; // 실제 사용과 포인트 비교
using UnityEngine; // 검사 비용 카드 및 원본 설정
using UnityEngine.TestTools; // 실제 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 손패와 공용 행동력을 쓰는 실제 전투 구성

public sealed class WinterfallPentacleUtilityTests // 기본 펜타클 자원 효과 검사
{ // 펜타클 지원 카드 검사 시작
    [UnityTest] // 무료 금화의 실제 공용 행동력 회복 검사
    public IEnumerator GoldenCoinRestoresSharedActionPointOnlyUpToCurrentCap() // 최대치와 부족 상태의 실제 카드 사용 비교
    { // 금화의 실제 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 카드 연출과 행동력 접근
        using (var fight = new Fight("GOLDEN_COIN", "GOLDEN_COIN")) // 실제 금화 두 장을 사용한 독립 전투
        { // 최대치 및 회복 경로 시작
            int cap = fight.Points.MaxActionPoints; // 현재 전투의 실제 행동력 상한
            yield return ClickCoin(fight); // 최대치에서 무료 금화 사용
            Assert.AreEqual(cap, fight.Points.CurrentActionPoints); // 최대 행동력 초과 없이 현재 상태 유지
            Assert.IsTrue(fight.Points.Spend(3)); // 두 번째 금화 전 회복 여유 생성
            yield return ClickCoin(fight); // 부족한 실제 행동력의 금화 회복
            Assert.AreEqual(cap - 2, fight.Points.CurrentActionPoints); // 원본 행동력 한 점만 회복
            Assert.AreEqual(2, fight.Deck.DiscardPileCount); // 정상 사용된 두 원본의 실제 카드 이동
        } // 최대치 및 회복 경로 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 금화의 실제 플레이 모드 종료

    private static IEnumerator ClickCoin(Fight fight) // 즉시 회복도 허용하는 실제 금화 입력
    { // 금화 클릭과 행동 완료 확인 시작
        var view = fight.View("GOLDEN_COIN"); // 현재 손패의 실제 금화 화면
        var card = view.RuntimeCard; // 원본 이동을 확인할 실제 카드
        view.OnPointerClick(null); // 실제 무료 금화 선택
        float deadline = Time.realtimeSinceStartup + 10f; // 기존 행동 연출 대기의 검사 기한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 진행하는 카드 연출만 대기
        { // 연출 다음 프레임 대기 시작
            yield return null; // 실제 카드 연출의 다음 프레임
        } // 연출 다음 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 카드 연출의 끝 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, card); // 실제 금화 원본의 카드 이동
    } // 금화 클릭과 행동 완료 확인 종료

    [UnityTest] // 실제 손패에서 사용한 원본을 즉시 다시 뽑지 않는 검사
    public IEnumerator FocusAndAncientRuneDrawSelectionOnceFromTheSharedDeck() // 자기 카드와 아군 수에 따른 중복 드로우 배제
    { // 실제 공용 드로우 검사 시작
        yield return new EnterPlayMode(); // 원본 카드와 실제 손패 실행
        using (var focus = new Fight("FOCUS")) // 따로 뽑을 카드가 없는 단일 원본 전투
        { // 자신을 다시 뽑지 않는 카드 사용 시작
            int points = focus.Points.CurrentActionPoints; // 집중 사용 전 실제 행동력
            yield return focus.Use("FOCUS"); // 원본 집중으로 공용 드로우 시도
            Assert.AreEqual(0, focus.Deck.HandCount); // 빈 다른 후보에서 원본 자신은 드로우 제외
            Assert.AreEqual(1, focus.Deck.DiscardPileCount); // 드로우 후보가 없어도 실제 카드 사용 완료
            Assert.AreEqual(points - 1, focus.Points.CurrentActionPoints); // 일반 집중 사용의 고정 기본 비용
        } // 자신을 다시 뽑지 않는 카드 사용 종료
        using (var fight = new Fight("ANCIENT_RUNE", "GOLDEN_COIN", "FOCUS")) // 실제 생존 아군 세 명의 공용 덱
        { // 전체 아군이 공유하는 한 번의 드로우 준비 시작
            var coin = fight.View("GOLDEN_COIN").RuntimeCard; // 공용 후보의 첫 원본 카드
            var focus = fight.View("FOCUS").RuntimeCard; // 공용 후보의 둘째 원본 카드
            Assert.IsTrue(fight.Deck.DiscardPlayedCard(coin)); // 실제 다시 순환할 후보 한 장 이동
            Assert.IsTrue(fight.Deck.DiscardPlayedCard(focus)); // 실제 다시 순환할 후보 둘째 이동
            int points = fight.Points.CurrentActionPoints; // 고대 룬 사용 전 실제 행동력
            yield return fight.Use("ANCIENT_RUNE"); // 생존 아군 전체의 공용 손패에 카드 지급
            CollectionAssert.AreEquivalent(new[] { coin, focus }, fight.Deck.Hand); // 현재 손패에 다른 두 카드만 실제 지급
            Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 여러 명을 대상으로 해도 카드 한 번의 기본 비용
            Assert.AreEqual("CRD_SHEET_ANCIENT_RUNE", fight.Deck.DiscardPile.Single().SourceData.CardId); // 실제 사용한 원본은 정상 버림 더미에 한 장만 보관
        } // 전체 아군이 공유하는 한 번의 드로우 준비 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 실제 공용 드로우 검사 종료

    [UnityTest] // 실제 같은 아군 턴의 고정 비용 감소 검사
    public IEnumerator EfficientUseRefreshesOneDiscountForSharedCurrentAndLaterCardsAndClearsNextTurn() // 중복 시전과 다음 턴 및 별도 비용 검증
    { // 실제 카드 비용 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 손패 소유자 준비
        using (var fight = new Fight("EFFICIENT_USE", "EFFICIENT_USE", "MANA_RELEASE", "RUNE_BURST")) // 같은 원본 두 장과 고정 및 X 카드
        { // 아군별 같은 턴 할인 준비 시작
            var late = fight.View("RUNE_BURST").RuntimeCard; // 할인을 쓰기 전에 손패를 떠나는 카드
            var x = fight.View("MANA_RELEASE").RuntimeCard; // 고정 비용 할인과 별개인 가변 지팡이
            int lateCost = late.ApCost; // 늦게 들어올 카드의 고정 원본 비용
            int xCost = x.ApCost; // 가변 카드가 유지할 기존 기본 비용 조회
            Assert.IsTrue(fight.Deck.DiscardPlayedCard(late)); // 실제로 손패 밖에 두고 늦게 들어올 상태 구성
            var extraCostSource = ScriptableObject.CreateInstance<CardData>(); // 별도 필수 비용 하나가 있는 검사 원본
            JsonUtility.FromJsonOverwrite("{\"apCost\":1,\"additionalActionPointCost\":3}", extraCostSource); // 기본 한 점 및 필수 세 점의 원본 값
            try // 정리 가능한 Unity 검사 카드 범위
            { // 실제 행동력과 다른 아군 비용 검증 시작
                int points = fight.Points.CurrentActionPoints; // 원본 두 장의 실제 합계 비용 비교
                yield return fight.Use("EFFICIENT_USE"); // 효율적 사용의 현재 턴 할인 시작
                Assert.AreEqual(1, fight.View("EFFICIENT_USE").RuntimeCard.ApCost); // 같은 턴 두 번째 원본도 기본 한 점 감소
                var laterAlly = CardInstance.Create(extraCostSource, fight.Allies[1], 5, 0); // 이미 할인된 실제 턴에 다른 아군의 새 카드 생성
                Assert.AreEqual(3, laterAlly.ApCost); // 기본 비용만 빠지고 별도 필수 세 점 보존
                Assert.AreEqual("X", x.CostLabel); // 모든 현재 행동력을 쓰는 표시 유지
                Assert.AreEqual(xCost, x.ApCost); // 고정 기본 비용 검사에서도 X의 원래 값 유지
                yield return fight.Use("EFFICIENT_USE"); // 같은 턴에 두 번째 효율적 사용
                Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 두 번의 실제 원래 비용 둘과 할인 비용 하나
                Assert.IsTrue(fight.Deck.TryRecoverDiscard(late)); // 할인 뒤 손패에 들어오는 실제 카드
                Assert.AreEqual(lateCost - 1, late.ApCost); // 동일한 할인 갱신이 중첩되지 않는 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴의 종료 정리 실행
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴으로 전환
                Assert.AreEqual(lateCost, late.ApCost); // 기존 원본 카드 비용으로 복원
                Assert.AreEqual(4, laterAlly.ApCost); // 다른 아군의 카드도 본래 전체 비용 복원
            } // 실제 행동력과 다른 아군 비용 검증 종료
            finally // 별도 검사 카드 해제 보장
            { // 원본 검사 객체 정리 시작
                Object.Destroy(extraCostSource); // 원본 덱에 없는 검사 생성 카드 제거
            } // 원본 검사 객체 정리 종료
        } // 아군별 같은 턴 할인 준비 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 실제 카드 비용 검사 종료
} // 펜타클 지원 카드 검사 종료
