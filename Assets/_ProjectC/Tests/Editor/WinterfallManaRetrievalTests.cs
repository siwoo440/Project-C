using System; // 실제 카드 선택 버튼의 이름 비교와 콜백 사용
using System.Collections; // 게임 실행 검사와 전투 연출 대기
using System.Linq; // 실제 선택 화면의 버튼 모음 조회
using System.Reflection; // 기본 버림 방어 유물을 재현할 기존 전투 메서드 조회
using NUnit.Framework; // 행동력과 카드 이동의 실제 결과 검증
using UnityEngine; // 전투 객체와 프레임 시간 접근
using UnityEngine.TestTools; // 실제 실행 모드 전환
using UnityEngine.UI; // 사용자가 눌러 확인하는 실제 버튼
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 카드 UI와 런타임 구성

public sealed class WinterfallManaRetrievalTests // 마력 회수의 실제 손패 선택과 카드 결제 검사
{ // 마력 회수 플레이 회귀 검사 시작
    [UnityTest] // 실제 지팡이 후보와 버림 및 공용 행동력 회복 확인
    public IEnumerator ValidSelectionDiscardsOneOtherWandAndRestoresExactlyOneActionPoint() // 정상 선택의 원래 카드 이동과 현재 행동력 비교
    { // 별도 지팡이 버림 성공 검사 시작
        yield return new EnterPlayMode(); // 손패와 실제 카드 선택 버튼을 사용할 실행 모드
        using (var fight = new Fight("MANA_RETRIEVAL", "ICE_ARROW", "THRUST")) // 정상 원본과 별도 지팡이 및 검 카드로 실제 전투 구성
        { // 원본 선택과 유효한 버림 비교 시작
            var source = fight.View("MANA_RETRIEVAL"); // 실제 손패의 마력 회수 카드 화면
            var sourceCard = source.RuntimeCard; // 다 쓰고 버린 원본 카드 비교용 참조
            var wand = fight.View("ICE_ARROW").RuntimeCard; // 실제로 선택 가능한 다른 물리 손패 지팡이
            var sword = fight.View("THRUST").RuntimeCard; // 선택 후보에서 제외할 실제 검 카드
            Assert.Zero(sourceCard.ApCost); // 공용 행동력을 요구하지 않는 시트 비용 확인
            fight.Points.Spend(2); // 명시적인 원래 행동력 회복 공간 준비
            int points = fight.Points.CurrentActionPoints; // 무료 카드 선택을 시작할 실제 행동력
            int nativeDiscards = 0; // 사용하지 않고 손에서 버린 별도 지팡이 효과 횟수
            int usedCards = 0; // 버림 카드가 사용으로 취급되지 않는지 확인할 사용 신호 횟수
            fight.Deck.BeforeHandDiscard += card => nativeDiscards += card == wand ? 1 : 0; // 기존 회차 유물과 손패 효과의 실제 버림 경로 관찰
            fight.Controller.CardUsed += (card, _) => usedCards += card == sourceCard ? 1 : 100; // 버린 대상은 사용 신호를 내지 않도록 실제 카드 이벤트 구독
            source.OnPointerClick(null); // 손패의 마력 회수 원본을 실제 클릭
            var overlay = Overlay(fight); // 선택 완료 전에 손패기를 가린 실제 모달
            var buttons = Choices(overlay); // 실제로 등록된 버릴 카드 후보 버튼 모음
            Assert.AreEqual(1, buttons.Length); // 원본 마력 회수와 검 카드를 빼면 남는 유일한 지팡이 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 대상을 고르는 동안 행동력 변화 없음 확인
            Assert.AreEqual(3, fight.Deck.Hand.Count); // 대상을 고르는 동안 손패가 그대로인지 확인
            Assert.Zero(usedCards); // 후보 화면이 실제 마력 회수 사용을 먼저 발생시키지 않는지 확인
            buttons[0].onClick.Invoke(); // 실제 별도 지팡이 버리기 확인 클릭
            yield return WaitForImpact(fight); // 실제 카드의 효과와 손패 이동 연출 완료 대기
            Assert.AreEqual(points + 1, fight.Points.CurrentActionPoints); // 실제 버린 뒤 현재 행동력 한 점 회복 확인
            Assert.AreEqual(1, nativeDiscards); // 별도 지팡이를 일반 효과 버림으로 한 번만 처리 확인
            Assert.AreEqual(1, usedCards); // 마력 회수 한 장만 정상 사용으로 처리되는지 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, sourceCard); // 마력 회수 원본의 일반 사용 카드 이동 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, wand); // 버리기로 선택한 지팡이의 기존 버림 더미 이동 확인
            CollectionAssert.AreEqual(new[] { sword }, fight.Deck.Hand); // 추가 드로우 없이 다른 검 한 장만 남음 확인
        } // 원본 선택과 유효한 버림 비교 종료
        yield return new ExitPlayMode(); // 실제 회수 검사 환경 종료
    } // 별도 지팡이 버림 성공 검사 종료

    [UnityTest] // 실제 취소와 무효 및 버림 보존으로 인한 실패 확인
    public IEnumerator CancelStaleChoiceAndPreventedDiscardKeepTheSourceAndActionPoints() // 취소·손패 이탈·버림 방어 시 부분 처리 방지 비교
    { // 취소와 보존된 실제 카드 비교 검사 시작
        yield return new EnterPlayMode(); // 실제 선택 화면과 버림 콜백이 연결될 실행 모드
        using (var fight = new Fight("MANA_RETRIEVAL", "ICE_ARROW", "THRUST")) // 한 개의 유효 후보가 있는 실제 손패 구성
        { // 취소 및 무효화의 무지불 결과 확인 시작
            var source = fight.View("MANA_RETRIEVAL").RuntimeCard; // 모든 실패 상황에 계속 남을 마력 회수
            var wand = fight.View("ICE_ARROW").RuntimeCard; // 손패가 바뀌며 무효화될 수 있는 후보
            fight.Points.Spend(1); // 실패 시 회복으로 증가하지 않을 현재 행동력 구분
            int points = fight.Points.CurrentActionPoints; // 명시 취소와 무효 선택의 원래 행동력
            fight.View("MANA_RETRIEVAL").OnPointerClick(null); // 정상적으로 후보 표시 시작
            var popup = Overlay(fight); // 실제 취소 버튼이 있는 선택 화면 조회
            popup.GetComponentsInChildren<Button>(true).Single(button => button.name == "CancelChoice").onClick.Invoke(); // 실제 포인터 취소 버튼의 클릭 이벤트 실행
            Assert.AreEqual(3, fight.Deck.Hand.Count); // 명시 취소가 원래 손패 전체를 보존 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 명시 취소가 행동력을 변경하지 않는지 확인
            Assert.Zero(fight.Deck.DiscardPileCount); // 명시 취소가 소스와 후보 모두 버리지 않음 확인
            fight.View("MANA_RETRIEVAL").OnPointerClick(null); // 취소 이후 같은 카드 재선택
            var staleChoice = Choices(Overlay(fight))[0]; // 카드 이동 전 열린 실제 후보 버튼 보관
            Assert.IsTrue(fight.Deck.DiscardCard(wand)); // 별도 효과로 후보만 먼저 물리 손패에서 이동
            staleChoice.onClick.Invoke(); // 원래 화면에 남아 있던 무효 후보의 늦은 확인 입력
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 이미 손패에서 나간 카드라 회복 없음 확인
            CollectionAssert.Contains(fight.Deck.Hand, source); // 무효 후보 이후에도 마력 회수 원본 보존 확인
            fight.View("MANA_RETRIEVAL").OnPointerClick(null); // 다른 지팡이가 없는 원본 재사용 시도
            Assert.IsNull(fight.Hand.transform.parent.GetComponentInChildren<BattleDrawChoiceView>()); // 후보 없음이 실제 손패 입력을 차단 확인
        } // 취소 및 무효화의 무지불 결과 확인 종료
        using (var fight = new Fight("MANA_RETRIEVAL", "ICE_ARROW")) // 효과 자체를 방어할 일반 버림 취소 흐름 확인
        { // 실제 버림이 막혔을 때의 원본 및 행동력 보존 시작
            var source = fight.View("MANA_RETRIEVAL").RuntimeCard; // 취소된 효과에서 남을 원래 마력 회수
            var wand = fight.View("ICE_ARROW").RuntimeCard; // 기존 손패 버림 보호에서 유지할 별도 지팡이
            var retain = typeof(BattleDeckRuntime).GetMethod("RetainHandCard", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 버림 방어 수단의 실제 런타임 API 조회
            Assert.IsNotNull(retain); // 유물 보존 경로를 확인할 테스트 호출 유효성
            fight.Deck.BeforeHandDiscard += card => retain.Invoke(fight.Deck, new object[] { card }); // 기존 보존 유물과 같은 실제 버림 전 유지 예약
            fight.Points.Spend(1); // 실효 없는 버림으로 행동력을 받을 수 없는 비교 상태
            int points = fight.Points.CurrentActionPoints; // 이전과 같아야 하는 현재 행동력
            fight.View("MANA_RETRIEVAL").OnPointerClick(null); // 버림 요청 전 실제 선택 화면 열기
            Choices(Overlay(fight))[0].onClick.Invoke(); // 보호된 지팡이 카드를 실제로 선택
            yield return WaitForImpact(fight); // 버림 처리 신호와 카드 충돌의 실제 시간 진행
            CollectionAssert.Contains(fight.Deck.Hand, source); // 실제로 다른 지팡이를 버리지 못했으므로 마력 회수도 사용하지 않음 확인
            CollectionAssert.Contains(fight.Deck.Hand, wand); // 기존 유물 경로로 버림 취소된 지팡이가 남음 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 버림 시도에 대한 불필요한 행동력 회복 없음 확인
            Assert.Zero(fight.Deck.DiscardPileCount); // 유지된 카드와 실패한 원본의 정상 손패 상태 확인
        } // 실제 버림이 막혔을 때의 원본 및 행동력 보존 종료
        yield return new ExitPlayMode(); // 독립 검사 환경 종료
    } // 취소와 보존된 실제 카드 비교 검사 종료

    [UnityTest] // 현재 행동력이 최대인 경우의 정당한 카드 버림 확인
    public IEnumerator ConfirmedDiscardAtFullActionPointsUsesTheCardWithoutExceedingTheCap() // 비용 없이 상한을 유지하는 명시 버림 검사
    { // 현재 행동력 최대 상태 처리 시작
        yield return new EnterPlayMode(); // 실제 카드 화면과 행동력 상한 로직 진입
        using (var fight = new Fight("MANA_RETRIEVAL", "ICE_ARROW")) // 후보 지팡이 하나만 있는 상한 행동력 전투
        { // 최대 행동력의 실제 선택과 버림 확인 시작
            int cap = fight.Points.MaxActionPoints; // 이번 턴의 실제 최대 공용 행동력 확인
            fight.View("MANA_RETRIEVAL").OnPointerClick(null); // 행동력이 가득 찬 상태에서 명시적으로 카드 선택
            Choices(Overlay(fight))[0].onClick.Invoke(); // 최대 행동력에서도 실제 다른 지팡이의 버리기 승인
            yield return WaitForImpact(fight); // 두 실제 카드가 모두 정상 구역에 이동할 때까지 대기
            Assert.AreEqual(cap, fight.Points.CurrentActionPoints); // 행동력을 상한보다 높이거나 잘못 소모하지 않음 확인
            Assert.AreEqual(cap, fight.Points.MaxActionPoints); // 마력 회수 자체가 최대 행동력을 변경하지 않음 확인
            Assert.Zero(fight.Deck.Hand.Count); // 상한이더라도 승인한 두 카드가 실제로 손패를 나감 확인
            Assert.AreEqual(2, fight.Deck.DiscardPileCount); // 하나는 효과 버림이고 하나는 정상 사용으로 이동 확인
        } // 최대 행동력의 실제 선택과 버림 확인 종료
        yield return new ExitPlayMode(); // 쌓인 검사용 화면과 카드 환경 종료
    } // 현재 행동력 최대 상태 처리 종료

    private static BattleDrawChoiceView Overlay(Fight fight) // 현재 전투 화면 안에 실제 표시된 카드 선택 창 조회
    { // 전투 선택 화면 찾기 시작
        var view = fight.Hand.transform.parent.GetComponentInChildren<BattleDrawChoiceView>(); // 전투 컨테이너의 활성 카드 선택 창
        Assert.IsNotNull(view); // 실제 카드 입력 이후 선택 창이 만들어졌는지 확인
        return view; // 실제 클릭할 원본 선택 창 반환
    } // 전투 선택 화면 찾기 종료

    private static Button[] Choices(BattleDrawChoiceView popup) // 실제 카드 후보 선택 버튼만 표시한 목록 조회
    { // 실제 후보 버튼 조회 시작
        return popup.GetComponentsInChildren<Button>(true).Where(button => button.name.StartsWith("Choice_", StringComparison.Ordinal)).ToArray(); // 선택 취소와 페이지 이동을 제외한 원래 카드 버튼 찾기
    } // 실제 후보 버튼 조회 종료

    private static IEnumerator WaitForImpact(Fight fight) // 정상 카드 사용과 보존 결과를 실제 프레임 경계에서 확인
    { // 유한 전투 연출 대기 시작
        float until = Time.realtimeSinceStartup + 10f; // 테스트 전투의 실제 대기 최대 시간
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 진행 중인 실제 행동 모션 확인
        { // 실행 장면 대기 시작
            yield return null; // 다음 실제 전투 프레임 진행
        } // 실행 장면 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 선택과 카드 정산이 제한 안에 끝났는지 확인
    } // 유한 전투 연출 대기 종료
} // 마력 회수 플레이 회귀 검사 종료
