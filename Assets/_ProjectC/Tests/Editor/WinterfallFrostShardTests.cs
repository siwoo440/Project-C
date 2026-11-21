using System.Collections; // 실제 플레이 모드와 충돌 연출 대기
using NUnit.Framework; // 카드 선택 상태와 실제 피해 확인
using TMPro; // 손패의 실제 적 선택 안내 조회
using UnityEngine; // 드래그 처리기와 전투 화면 사용
using UnityEngine.EventSystems; // 실제 클릭과 드롭 입력 생성
using UnityEngine.TestTools; // 실제 전투 플레이 검사 실행
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 화면과 카드 입력 준비 환경

public sealed class WinterfallFrostShardTests // 서리 파편의 서로 다른 선택 대상과 비용 검증
{ // 서리 파편 실전 검사 시작
    [UnityTest] // 클릭으로 지정한 두 적의 실제 독립 타격 검사
    public IEnumerator ClickConfirmsTwoDistinctEnemiesForOneActionPoint() // 두 번째 적을 확인한 뒤 한 번 지불하는 카드 검사
    { // 클릭 순차 선택 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 입력 모드 진입
        using (var fight = new Fight("FROST_SHARD")) // 서리 파편 원본과 생존 적 네 명의 실제 전투
        { // 두 대상의 실제 지정과 가산 없는 마법 피해 확인 시작
            var card = fight.View("FROST_SHARD"); // 손패에 실제로 표시된 서리 파편 원본
            var used = card.RuntimeCard; // 정상 한 번의 카드 이동 확인용 참조
            int points = fight.Points.CurrentActionPoints; // 첫 선택 전에 보유한 공용 행동력
            int hitEvents = 0; // 네 적에게 실제로 적용된 타격의 총 횟수
            foreach (var enemy in fight.Enemies) // 실제로 명중 피해를 받을 후보 네 명 순회
            { // 명중 이벤트 관찰 연결 시작
                enemy.DamageTaken += (_, result) => hitEvents += result.AppliedDamage > 0 ? 1 : 0; // 양수 체력 피해를 받은 실제 타격만 기록
            } // 명중 이벤트 관찰 연결 종료
            Assert.AreEqual(1, used.ApCost); // 서리 파편 원본 행동력 한 점 확인
            Assert.AreEqual(BattleDamageType.Magical, used.DamageType); // 일반 타격과 구분되는 원본 마법 피해 분류 확인
            card.OnPointerClick(null); // 실제 손패에서 카드 대상 선택 개시
            StringAssert.Contains("첫 번째 적 선택", Prompt(fight)); // 전투 화면의 첫 적 선택 문구 확인
            fight.EnemyViews[2].OnPointerClick(null); // 인접 규칙과 무관하게 세 번째 칸 적을 먼저 선택
            StringAssert.Contains("두 번째 적 선택", Prompt(fight)); // 화면에서 두 번째 적 선택 안내 확인
            fight.EnemyViews[2].OnPointerClick(null); // 같은 적을 반복 클릭하는 잘못된 두 번째 입력
            Assert.AreSame(used, fight.Controller.SelectedCard); // 같은 적을 골라도 원래 선택을 유지하는지 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 아직 두 대상 미확정이라 행동력이 차감되지 않는지 확인
            Assert.Zero(hitEvents); // 첫 선택만으로 실제 공격이 발동하지 않는지 확인
            fight.EnemyViews[0].OnPointerClick(null); // 다른 칸의 생존 적을 두 번째 대상으로 확정
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 두 대상 확정 뒤 전체 비용 한 점 확인
            yield return WaitForImpact(fight); // 실제 공격과 카드 이동을 처리할 프레임 대기
            Assert.AreEqual(2, hitEvents); // 서로 다른 두 대상에게 각 한 번의 실제 타격 확인
            Assert.AreEqual(196, fight.Enemies[0].CurrentHealth); // 두 번째 지정 적에게 마법 피해 네 점 확인
            Assert.AreEqual(200, fight.Enemies[1].CurrentHealth); // 지정하지 않은 인접 적의 원래 체력 보존 확인
            Assert.AreEqual(196, fight.Enemies[2].CurrentHealth); // 첫 번째 지정 적에게 마법 피해 네 점 확인
            Assert.AreEqual(200, fight.Enemies[3].CurrentHealth); // 무작위로 고르지 않은 다른 적 피해 없음 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, used); // 대상마다 별도 사용 없이 카드 한 장만 버림 확인
        } // 두 대상의 실제 지정과 가산 없는 마법 피해 확인 종료
        yield return new ExitPlayMode(); // 독립 실제 전투 모드 종료
    } // 클릭 순차 선택 검사 종료

    [UnityTest] // 실제 드롭의 취소와 손패 및 행동력 보존 검사
    public IEnumerator DragCancelsIfChosenEnemyDiesAndCanRestartWithAnotherPair() // 첫 번째 대상으로 시작한 드래그가 무효화되는 경계 확인
    { // 실제 드래그와 취소 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 드롭 위치를 연결하는 모드 진입
        using (var fight = new Fight("FROST_SHARD")) // 실제 생존 적이 넷인 서리 파편 드래그 전투
        { // 드래그 취소와 새 순차 입력 확인 시작
            var card = fight.View("FROST_SHARD"); // 같은 카드 재시도를 검증할 실제 손패 원본
            var drag = DragFor(card); // 카드에 연결된 실제 드래그 처리기
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; // 왼쪽 드래그 입력의 실제 포인터 상태
            int points = fight.Points.CurrentActionPoints; // 취소 시 유지할 실제 공용 행동력
            drag.OnBeginDrag(pointer); // 첫 드롭 전 살아 있는 적 수 기준 선택 시작
            FinishDrop(drag, pointer, fight.EnemyViews[0]); // 첫 번째 적에게 실제 서리 파편 드롭
            StringAssert.Contains("두 번째 적 선택", Prompt(fight)); // 실제 드롭에 따른 두 번째 대상 안내 확인
            drag.OnBeginDrag(pointer); // 같은 카드에서 두 번째 드롭 입력을 열기
            fight.Enemies[0].TakeDamage(1000, BattleDamageType.None); // 두 번째 드롭 확인 전 고른 적의 실제 사망
            FinishDrop(drag, pointer, fight.EnemyViews[1]); // 이미 취소된 선택의 늦은 드롭 실행
            Assert.IsNull(fight.Controller.SelectedCard); // 먼저 고른 적이 사망하면 미완료 선택 해제 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 무효화된 드롭의 실제 행동력 보존 확인
            Assert.AreEqual(200, fight.Enemies[1].CurrentHealth); // 늦은 드롭이 새 공격을 시작하지 않음을 확인
            drag.OnBeginDrag(pointer); // 유효한 다른 조합을 다시 선택할 드래그 시작
            FinishDrop(drag, pointer, fight.EnemyViews[1]); // 첫 번째 적을 두 번째 칸으로 새 선택
            drag.OnBeginDrag(pointer); // 두 번째 드롭을 준비한 기존 선택 유지
            FinishDrop(drag, pointer, null); // 적 밖의 잘못된 위치에 놓아 실제 취소
            Assert.IsNull(fight.Controller.SelectedCard); // 무효 위치에 놓으면 카드 선택을 되돌리는지 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 위치 취소 뒤에도 비용 없음 확인
            card.OnPointerClick(null); // 실제 클릭으로 별도 서리 파편 선택을 다시 시작
            fight.EnemyViews[1].OnPointerClick(null); // 첫 적을 클릭 입력으로 지정
            drag.OnBeginDrag(pointer); // 같은 카드에서 클릭에 이어 드래그 입력 시작
            FinishDrop(drag, pointer, fight.EnemyViews[3]); // 두 번째 적을 드롭해서 사용 확정
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 연속 클릭과 드래그로 한 번의 비용 지불 확인
            yield return WaitForImpact(fight); // 실제 명중 효과와 사용 카드 이동 정산 대기
            Assert.AreEqual(196, fight.Enemies[1].CurrentHealth); // 새로 고른 첫 적의 명중 확인
            Assert.AreEqual(196, fight.Enemies[3].CurrentHealth); // 새로 고른 두 번째 적의 명중 확인
        } // 드래그 취소와 새 순차 입력 확인 종료
        yield return new ExitPlayMode(); // 실제 전투 종료와 에디터 복귀
    } // 실제 드래그와 취소 검사 종료

    [UnityTest] // 드래그 시작 시점의 생존 적 수 고정 검사
    public IEnumerator AnInitiallyLoneEnemyIsPermittedButADeadSecondCandidateDoesNotLowerTheRequirement() // 처음부터 한 대상만 가능한 경우와 도중 후보 감소 비교
    { // 실제 적 수와 첫 드롭 확인 시작
        yield return new EnterPlayMode(); // 실제 전투와 드래그 이벤트가 있는 플레이 모드 시작
        using (var fight = new Fight("FROST_SHARD")) // 동일 카드로 두 번의 실전 대상 수 비교
        { // 선택 중 감소한 적 수와 새로 시작한 한 대상 선택 비교
            fight.Enemies[2].TakeDamage(1000, BattleDamageType.None); // 처음 두 적만 남길 실제 피해
            fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 처음 두 적만 남길 실제 피해
            var card = fight.View("FROST_SHARD"); // 두 대상에서 한 대상으로 재시도할 카드 참조
            var drag = DragFor(card); // 원래 손패 뷰의 실제 드래그 컴포넌트
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; // 실제 드래그 종료 위치를 담을 이벤트
            int points = fight.Points.CurrentActionPoints; // 실제 시작 공용 행동력
            drag.OnBeginDrag(pointer); // 두 적이 모두 살아 있는 순간에 투사체 대상 선택 시작
            fight.Enemies[1].TakeDamage(1000, BattleDamageType.None); // 첫 드롭 전에 유효한 적 하나가 사망
            FinishDrop(drag, pointer, fight.EnemyViews[0]); // 원래 두 대상이 필요했던 선택을 늦게 확정 시도
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 도중 바뀐 적 수로 비용을 쓰지 않음 확인
            Assert.AreEqual(200, fight.Enemies[0].CurrentHealth); // 살아 남은 적을 자동 단일 타격하지 않음을 확인
            drag.OnBeginDrag(pointer); // 처음부터 한 적만 살아 있는 상태에서 선택 재개
            StringAssert.Contains("남은 적 선택", Prompt(fight)); // 실제 전투 화면의 한 대상 선택 안내 확인
            FinishDrop(drag, pointer, fight.EnemyViews[0]); // 첫 드롭의 한 대상으로 카드 정상 확정
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 한 적이어도 원래 비용 한 점 사용 확인
            yield return WaitForImpact(fight); // 실제로 수행한 한 번의 카드 공격과 이동 대기
            Assert.AreEqual(196, fight.Enemies[0].CurrentHealth); // 생존 적 하나에게 마법 피해 네 점이 한 번 적용 확인
        } // 선택 중 감소한 적 수와 새로 시작한 한 대상 선택 비교 종료
        yield return new ExitPlayMode(); // 실제 입력 회귀 검사 종료
    } // 실제 적 수와 첫 드롭 확인 종료

    private static BattleCardDragHandler DragFor(BattleCardView card) // 실제 덱의 카드 뷰에 연결한 드래그 처리기 조회
    { // 드래그 연결 시작
        var drag = card.GetComponent<BattleCardDragHandler>() ?? card.gameObject.AddComponent<BattleCardDragHandler>(); // 기존 처리기나 실제 새 컴포넌트 사용
        drag.Initialize(card); // 손패 카드에 원래 드래그 초기화 연결
        return drag; // 실제 골라 놓을 카드의 입력 처리기 반환
    } // 드래그 연결 종료

    private static void FinishDrop(BattleCardDragHandler drag, PointerEventData pointer, BattleUnitView target) // 실제 포인터 화면 좌표의 유효 드롭 수행
    { // 대상 위치 연결 시작
        pointer.pointerCurrentRaycast = new RaycastResult { gameObject = target == null ? null : target.gameObject }; // 대상 화면에 올리거나 바닥에 내려놓는 포인터 결과
        drag.OnEndDrag(pointer); // 실제 카드 드롭 유효성 판정과 선택 전달 수행
    } // 대상 위치 연결 종료

    private static string Prompt(Fight fight) // 실제 손패가 만든 현재 턴 안내 찾기
    { // 실제 화면 문구 조회 시작
        foreach (var label in fight.Hand.transform.parent.GetComponentsInChildren<TMP_Text>(true)) // 손패와 같은 전투 화면 상단의 안내 표시 순회
        { // 전투 안내 텍스트 확인 시작
            if (label.name == "TurnStatusText") // 실제 두 단계 안내 문자열을 쓰는 라벨 확인
            { // 현재 문구 반환 시작
                return label.text; // 사용자에게 보이는 서리 파편 선택 안내 반환
            } // 현재 문구 반환 종료
        } // 전투 안내 텍스트 확인 종료
        Assert.Fail("전투 턴 안내 없음"); // 실제 입력 화면의 안내 누락 보고
        return string.Empty; // 단언 실패 뒤 필요한 문자열 반환 계약
    } // 실제 화면 문구 조회 종료

    private static IEnumerator WaitForImpact(Fight fight) // 카드 실행과 원래 충돌 시점의 유한 대기
    { // 충돌 정산 대기 시작
        float deadline = Time.realtimeSinceStartup + 10f; // 테스트가 대기할 연출 최대 시간 설정
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 적 피해와 카드 이동의 연출 실행 상태 확인
        { // 다음 렌더링 프레임 대기 시작
            yield return null; // 연출 시간을 진행할 실제 다음 프레임
        } // 다음 렌더링 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 정해진 시간 내 손패 입력이 다시 가능해졌는지 확인
    } // 충돌 정산 대기 종료
} // 서리 파편 실전 검사 종료
