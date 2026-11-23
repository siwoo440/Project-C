using System.Collections; // 실제 전투 연출 대기
using System.Linq; // 생성된 아군 대상 화면 조회
using NUnit.Framework; // 두 대상 선택과 비용 검증
using UnityEngine; // 실제 프레임 시간 조회
using UnityEngine.EventSystems; // 클릭과 드롭 입력 생성
using UnityEngine.TestTools; // 플레이 모드 전투 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 환경 재사용

public sealed class WinterfallSplitBarrierTests // 분할 방벽의 두 아군 대상 검사
{ // 방벽 검사 시작
    [UnityTest] // 드롭 첫 선택과 중복 클릭 및 두 번째 대상 검사
    public IEnumerator SplitBarrierDropArmsOneAllyAndTheSecondDistinctClickPaysOnce() // 첫 드롭 뒤 소유자와 동료에게 각각 방어도 지급 확인
    { // 드롭 선택 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입
        yield return CheckDropAndClicks(); // 카드 드롭과 대상 클릭 검증
        yield return new ExitPlayMode(); // 실제 전투 입력 환경 종료
    } // 드롭 선택 검사 종료

    private static IEnumerator CheckDropAndClicks() // 실제 드래그와 공통 클릭 진입점 검사
    { // 두 대상 전투 시작
        using (var fight = new Fight("SPLIT_BARRIER")) // 실제 보물 분할 방벽 원본 준비
        { // 첫 대상 드롭 준비 시작
            var cardView = fight.View("SPLIT_BARRIER"); // 손패의 분할 방벽 화면
            var card = cardView.RuntimeCard; // 실제 사용을 확인할 카드 인스턴스
            var first = ViewFor(fight, 1); // 첫 번째로 고를 동료 화면
            var owner = ViewFor(fight, 0); // 두 번째로 고를 카드 소유자 화면
            var drag = cardView.GetComponent<BattleCardDragHandler>(); // 기존 손패의 실물 드래그 입력
            if (drag == null) // 독립 전투 화면에 드래그 입력 설치 여부 확인
            { // 드래그 연결 시작
                drag = cardView.gameObject.AddComponent<BattleCardDragHandler>(); // 실제 입력 컴포넌트 추가
                drag.Initialize(cardView); // 손패 카드에 드래그 입력 연결
            } // 드래그 연결 종료
            int points = fight.Points.CurrentActionPoints; // 사용 전 행동력 기준
            int cost = card.ApCost; // 실제 카드 비용 보관
            int uses = 0; // 카드 사용 이벤트 횟수
            fight.Controller.CardUsed += (used, targets) => uses++; // 최종 해결된 사용 한 번 집계
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; // 실제 왼쪽 마우스 드래그 입력
            drag.OnBeginDrag(pointer); // 분할 방벽 드래그 시작
            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = first.gameObject }; // 첫 번째 아군 위의 드롭 위치
            drag.OnEndDrag(pointer); // 카드가 첫 번째 아군만 선택하는 드롭
            Assert.AreSame(card, fight.Controller.SelectedCard); // 첫 드롭 뒤 카드 선택 유지 확인
            first.OnPointerClick(pointer); // 동일 대상 다시 클릭
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 최초 선택과 중복 클릭의 행동력 무차감 확인
            Assert.AreEqual(0, uses); // 두 번째 대상 선택 전 사용 이벤트 없음
            Assert.IsFalse(fight.Runner.IsBusy); // 두 번째 대상 선택 전 전투 연출 없음
            Assert.AreEqual(0, first.RuntimeUnit.CurrentBlock); // 첫 번째 대상 단독 효과 미적용 확인
            owner.OnPointerClick(pointer); // 다른 생존 대상인 소유자 클릭
            Assert.AreEqual(points - cost, fight.Points.CurrentActionPoints); // 최종 대상 확인 때 카드 비용 한 번 차감
            float deadline = Time.realtimeSinceStartup + 10f; // 전투 연출의 최대 대기 시간
            while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 연출과 카드 사용 위치 정산 대기
            { // 충돌 연출 대기 시작
                yield return null; // 다음 전투 프레임 대기
            } // 충돌 연출 대기 종료
            Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 연출 종료 확인
            Assert.AreEqual(1, uses); // 방어도 지급 대상이 둘이어도 사용 이벤트 한 번 확인
            CollectionAssert.AreEqual(new[] { 4, 4, 0 }, fight.Allies.Select(ally => ally.CurrentBlock)); // 선택한 두 아군만 각각 방어도 네 점 획득
            CollectionAssert.AreEqual(new[] { card }, fight.Deck.DiscardPile); // 같은 카드 한 장만 버린 더미 이동
            Assert.AreEqual(0, fight.Deck.HandCount); // 사용한 원본의 손패 이탈 확인
        } // 첫 대상 드롭 준비 종료
    } // 두 대상 전투 종료

    [UnityTest] // 죽은 대상과 턴 종료 및 생존 조건 검사
    public IEnumerator SplitBarrierClearsUnpaidSelectionAndRequiresTwoLivingAllies() // 취소와 사망 및 턴 전환 시 미확정 대상 초기화
    { // 선택 중단 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 입력 환경 진입
        yield return CheckInterruptedSelection(); // 선택 중단 상황의 미사용 확인
        yield return new ExitPlayMode(); // 실제 손패 입력 환경 종료
    } // 선택 중단 검사 종료

    private static IEnumerator CheckInterruptedSelection() // 카드 사용 전 선택 취소와 생존 조건 확인
    { // 선택 중단 전투 시작
        using (var fight = new Fight("SPLIT_BARRIER")) // 카드 한 장을 매 턴 다시 뽑을 독립 전투
        { // 아군 대상 준비 시작
            var card = fight.View("SPLIT_BARRIER"); // 실제 손패에서 선택할 분할 방벽
            var owner = ViewFor(fight, 0); // 생존 상태를 유지할 카드 소유자
            var first = ViewFor(fight, 1); // 선택한 뒤 전투불능으로 만들 아군
            var other = ViewFor(fight, 2); // 이후 생존자 수를 변경할 다른 아군
            int points = fight.Points.CurrentActionPoints; // 카드 선택 전 행동력 기준
            card.OnPointerClick(null); // 실제 손패로 카드 선택
            first.OnPointerClick(null); // 첫 번째 동료 선택
            card.OnPointerClick(null); // 같은 카드 재입력으로 미확정 선택 취소
            Assert.IsNull(fight.Controller.SelectedCard); // 손패 재입력으로 선택 상태 해제 확인
            card.OnPointerClick(null); // 같은 카드의 선택 재시작
            first.OnPointerClick(null); // 새 선택의 첫 대상 지정
            first.RuntimeUnit.TakeDamage(first.RuntimeUnit.MaxHealth, BattleDamageType.None); // 실제 사망 이벤트로 첫 대상 이탈
            Assert.IsNull(fight.Controller.SelectedCard); // 첫 대상 사망의 선택 취소 확인
            owner.OnPointerClick(null); // 취소 뒤 남은 아군 클릭
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 사망으로 중단된 선택의 행동력 보존 확인
            card.OnPointerClick(null); // 생존자 둘로 카드 재선택
            owner.OnPointerClick(null); // 카드 소유자를 첫 번째로 지정
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 대상 한 명만 고른 상태에서 다음 턴 이동
            Assert.IsNull(fight.Controller.SelectedCard); // 다음 턴의 미확정 선택 초기화 확인
            Assert.AreEqual(0, owner.RuntimeUnit.CurrentBlock); // 중단된 선택의 방어도 미적용 확인
            yield return null; // 새 턴 손패 화면과 이전 카드 정리 대기
            other.RuntimeUnit.TakeDamage(other.RuntimeUnit.MaxHealth, BattleDamageType.None); // 카드 소유자만 남기는 실제 사망 이벤트
            card = fight.View("SPLIT_BARRIER"); // 다음 턴에 다시 뽑힌 손패 화면 조회
            card.OnPointerClick(null); // 생존자 한 명에서 카드 선택 시도
            Assert.IsNull(fight.Controller.SelectedCard); // 아군 두 명이 없는 경우 선택 차단 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 사용할 수 없는 카드의 행동력 보존 확인
            Assert.AreEqual(1, fight.Deck.HandCount); // 사용할 수 없는 카드의 손패 보존 확인
        } // 아군 대상 준비 종료
    } // 선택 중단 전투 종료

    private static BattleUnitView ViewFor(Fight fight, int index) // 실제 전투가 연결한 지정 아군 화면 조회
    { // 아군 화면 검색 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == fight.Allies[index]); // 기존 카드 컨트롤러에 등록된 화면 반환
    } // 아군 화면 검색 종료
} // 방벽 검사 종료
