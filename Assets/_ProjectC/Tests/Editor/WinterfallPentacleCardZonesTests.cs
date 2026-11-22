using System.Collections; // 실제 플레이 모드 진입과 프레임 대기
using System.Linq; // 실제 손패 카드 위치와 선택 모달 조회
using NUnit.Framework; // 카드 물리 영역과 행동력 검증
using UnityEngine; // 실제 카드 원본 데이터와 선택 화면
using UnityEngine.TestTools; // 실제 게임의 플레이 모드 실행
using UnityEngine.UI; // 기존 선택 및 취소 버튼의 실제 입력
using Fight = WinterfallSwordCombatTests.Fight; // 공용 전투와 손패 원본 생성

public sealed class WinterfallPentacleCardZonesTests // 전투 제외 회수와 실제 손패 보관 검사
{ // 두 펜타클 카드 영역의 회귀 검사 시작
    [UnityTest] // 실제 전투에 제외된 같은 카드의 회수 검사
    public IEnumerator ResourceCycleRecoversTheSameLiveOwnedBattleExclusion() // 지불 전 빈 더미 거절과 실제 한 장 회수
    { // 카드 제외와 한 번의 확정 사용 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 카드와 원래 덱 준비
        using (var fight = new Fight("RESOURCE_CYCLE", "PICK_UP_COIN", "THRUST")) // 제외 규칙이 등록된 원본 카드 사용
        { // 빈 더미 시도 및 정상 회수 비교 시작
            var cycle = fight.View("RESOURCE_CYCLE").RuntimeCard; // 자원 순환의 실제 원본 실물
            var coin = fight.View("PICK_UP_COIN").RuntimeCard; // 사용하면 전투에서 제외되는 동전 줍기 실물
            int initial = fight.Points.CurrentActionPoints; // 실제 카드 사용 조건 확인 전 행동력
            fight.View("RESOURCE_CYCLE").OnPointerClick(null); // 아직 제외된 카드가 없는 실제 사용 시도
            Assert.AreEqual(initial, fight.Points.CurrentActionPoints); // 빈 제외 더미에서 원래 행동력 유지
            Assert.IsFalse(fight.Runner.IsBusy); // 실제 효과 연출 시작 없음
            Assert.IsTrue(fight.Deck.Hand.Contains(cycle)); // 사용 불가한 원본 실물의 손패 보존
            fight.View("PICK_UP_COIN").OnPointerClick(null); // 실제 동전 사용 입력과 전투 제외 시작
            float deadline = Time.realtimeSinceStartup + 10f; // 동전 정산의 완료 대기 시간
            while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 동전 연출과 손패 이동 진행 확인
            { // 동전 효과 정산 대기 시작
                yield return null; // 다음 게임 프레임 대기
            } // 동전 효과 정산 대기 종료
            Assert.IsFalse(fight.Runner.IsBusy); // 실제 동전 사용 완료 확인
            CollectionAssert.AreEquivalent(new[] { coin }, fight.Deck.ExcludedPile); // 버림 더미가 아닌 이번 전투의 실제 제외 한 장
            int before = fight.Points.CurrentActionPoints; // 자원 순환의 실제 행동력 지불 기준
            int hand = fight.Deck.HandCount; // 사용 원본과 회수 카드가 교체할 손패 인원
            yield return fight.Use("RESOURCE_CYCLE", 0); // 현재 하나의 실제 제외 대상 무작위 회수
            Assert.AreSame(coin, fight.View("PICK_UP_COIN").RuntimeCard); // 원본 단계와 소유자를 유지한 같은 물리 카드 반납
            Assert.AreEqual(before - 2, fight.Points.CurrentActionPoints); // 실제 사용의 기본 행동력 두 점 확인
            Assert.AreEqual(hand, fight.Deck.HandCount); // 사용 원본과 한 장 회수 이후의 실제 카드 수
            Assert.IsEmpty(fight.Deck.ExcludedPile); // 회수한 한 카드가 두 영역에 중복되지 않는 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, cycle); // 정상 사용 원본의 버림 위치
            Assert.IsTrue(fight.Deck.IsStateValid()); // 실제 전투 내 카드 수와 고유 영역 유지
        } // 빈 더미 시도 및 정상 회수 비교 종료
        yield return new ExitPlayMode(); // 실제 회수 검사 자원 종료
    } // 카드 제외와 한 번의 확정 사용 검사 종료

    [UnityTest] // 실제 손패 선택과 다음 아군 턴까지의 비용 및 수명 검사
    public IEnumerator StrategicStorageRetainsOnePhysicalCardAndPreservesItsAdditionalCost() // 취소 및 첫 보존과 둘째 정상 정리 확인
    { // 손패 선택과 비용 경계 검사 시작
        yield return new EnterPlayMode(); // 원본 카드와 실제 화면 조작 초기화
        var save = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 출시 에셋의 카드 저장 조회
        var storage = save.Cards.Single(card => card.CardId == "CRD_SHEET_STRATEGIC_STORAGE"); // 실제 전략 보관 원본
        var payload = ScriptableObject.CreateInstance<CardData>(); // 고정 기본 비용과 별도 지불 비용을 가진 카드
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"CRD_SHEET_STORAGE_COST_TEST\",\"displayName\":\"전략 보관 비용 확인\",\"apCost\":3,\"additionalActionPointCost\":2,\"cardType\":{(int)CardType.Sword},\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)CardEffectType.Damage},\"damageType\":{(int)BattleDamageType.Physical},\"effectValue\":5}}", payload); // 원래 기본 세 점과 별개 비용 두 점
        try // 실제 임시 원본의 독립 해제 보장
        { // 같은 손패의 취소 후 실제 확정 시작
            using var fight = new Fight(new[] { storage, payload }); // 현재 다른 실물 후보가 한 장인 실제 전투
            var selected = fight.View("STORAGE_COST_TEST").RuntimeCard; // 비용과 물리 손패 위치를 추적할 한 장
            int points = fight.Points.CurrentActionPoints; // 무지불 취소의 비교 행동력
            fight.View("STRATEGIC_STORAGE").OnPointerClick(null); // 실제 손패에서 보관할 다른 카드 창 열기
            Assert.IsNull(Choice().transform.Find("Choice_1")); // 보관을 시전하는 카드 자체는 선택 후보 제외
            Choose("CancelChoice"); // 별도 비용과 유지 변화 없는 실제 취소
            yield return null; // 이전 모달 정리 프레임 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 취소 후 실제 행동력 보존
            Assert.AreEqual(5, selected.ApCost); // 취소 후 기본 세 점과 별개 두 점 유지
            fight.View("STRATEGIC_STORAGE").OnPointerClick(null); // 실제 손패 보관 후보 다시 표시
            Choose("Choice_0"); // 별도 비용을 가진 실물을 실제 확정
            float until = Time.realtimeSinceStartup + 10f; // 정상 카드 연출의 최대 대기 시간
            while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 실제 원본 이동과 할인 적용 대기
            { // 현재 연출과 손패 갱신 처리 시작
                yield return null; // 다음 플레이 모드 프레임 진행
            } // 현재 연출과 손패 갱신 처리 종료
            Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 정산의 최종 완료 확인
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 확정한 전략 보관의 실제 행동력 한 점
            Assert.AreEqual(4, selected.ApCost); // 기본 비용 한 점만 차감한 현재 합계
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 처음 보관이 적용되는 실제 턴 종료
            CollectionAssert.Contains(fight.Deck.Hand, selected); // 원래 실물의 최초 정리 보관 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 다음 아군의 새 차례로 전환
            Assert.AreSame(selected, fight.View("STORAGE_COST_TEST").RuntimeCard); // 다음 실제 아군 정산에서 원본 손패 유지
            Assert.AreEqual(4, selected.ApCost); // 다음 실제 사용까지 같은 기본 할인을 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 아군 정리에서 보관 기회 소비
            CollectionAssert.Contains(fight.Deck.DiscardPile, selected); // 영구 유지가 아닌 두 번째 정리의 원래 버림 위치
            Assert.AreEqual(5, selected.ApCost); // 미사용 보관의 종료 후 원래 비용 복구
            Assert.IsTrue(fight.Deck.IsStateValid()); // 보관이나 비용이 카드 영역의 중복을 만들지 않는 확인
        } // 같은 손패의 취소 후 실제 확정 종료
        finally // 독립 검사 자원 해제의 실제 보장
        { // 임시 카드 삭제 시작
            Object.Destroy(payload); // 지급된 원본 자산과 별개인 검사 카드 해제
        } // 임시 카드 삭제 종료
        yield return new ExitPlayMode(); // 실제 손패 보관 검사 모드 종료
    } // 손패 선택과 비용 경계 검사 종료

    private static BattleDrawChoiceView Choice() // 실제 활성 선택 모달 조회
    { // 선택 화면 탐색 시작
        return Object.FindObjectsByType<BattleDrawChoiceView>(FindObjectsSortMode.None).Single(view => view.gameObject.activeInHierarchy); // 현재 실제 열려 있는 입력 화면 한 개
    } // 선택 화면 탐색 종료

    private static void Choose(string button) // 실제 선택 또는 취소 버튼 입력
    { // 원본 모달의 클릭 호출 시작
        Choice().transform.Find(button).GetComponent<Button>().onClick.Invoke(); // 현재 입력의 실제 활성 버튼
    } // 원본 모달의 클릭 호출 종료
} // 두 펜타클 카드 영역의 회귀 검사 종료
