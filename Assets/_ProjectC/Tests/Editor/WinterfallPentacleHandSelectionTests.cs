using System.Collections; // 실제 플레이모드와 카드 연출 대기
using System.Linq; // 실제 손패 카드와 공용 모달 조회
using NUnit.Framework; // 공용 행동력과 실제 카드 영역 확인
using UnityEngine; // 저장 원본 카탈로그와 실제 화면 사용
using UnityEngine.TestTools; // 게임의 실제 입력과 전투 회귀 검사
using UnityEngine.UI; // 공용 선택 모달의 실제 버튼 조작
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 손패와 덱을 사용하는 검사 도구

public sealed class WinterfallPentacleHandSelectionTests // 교환 제안과 손패 정리 및 즉흥 조달의 실제 전투 검사
{ // 세 펜타클 지원 카드 회귀 검사 시작
    [UnityTest] // 취소와 함께 한 장 비용 및 원래 덱 드로우 확인
    public IEnumerator ExchangeProposalOnlyPaysOnARealOtherCardAndDrawsTwoWithoutItsOwnCard() // 유효 다른 한 장과 공유 보충 두 장 확인
    { // 교환 제안의 실제 선택 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 입력과 카드 사용 연출 환경 진입
        using (var fight = new Fight("EXCHANGE_PROPOSAL", "THRUST", "KNEE_SLASH", "BLOOD_EDGE")) // 재순환할 카드와 다른 비용 실물이 있는 원래 전투
        { // 취소와 확인된 한 장 및 실제 두 장 보충 비교 시작
            var source = fight.View("EXCHANGE_PROPOSAL").RuntimeCard; // 실제 선택을 여는 현재 물리 원본
            Assert.IsTrue(fight.Deck.DiscardCard(fight.View("KNEE_SLASH").RuntimeCard)); // 재순환에서 사용할 실제 버림 더미 준비
            Assert.IsTrue(fight.Deck.DiscardCard(fight.View("BLOOD_EDGE").RuntimeCard)); // 실제 새 보충을 확인할 다른 원본 추가
            var chosen = fight.View("THRUST").RuntimeCard; // 물리 손패의 유일한 다른 비용 후보
            int points = fight.Points.CurrentActionPoints; // 유효한 확인 전 유지할 행동력
            fight.View("EXCHANGE_PROPOSAL").OnPointerClick(null); // 한 장을 골라야 하는 실제 모달 열기
            Assert.IsNull(Choice().transform.Find("Choice_1")); // 원본 카드 자기 자신은 고를 수 없음 확인
            Choose("CancelChoice"); // 카드나 행동력이 이동하지 않는 실제 취소
            yield return null; // 닫힌 기존 화면과 선택 입력 종료 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 취소의 무지불 유지 확인
            CollectionAssert.AreEquivalent(new[] { source, chosen }, fight.Deck.Hand); // 취소 후 물리 손패 유지
            fight.View("EXCHANGE_PROPOSAL").OnPointerClick(null); // 같은 원래 실물로 정상 확인 다시 시작
            Choose("Choice_0"); // 물리 손패의 다른 실제 카드 한 장을 확인
            yield return WaitForCard(fight); // 카드 이동과 실제 사용 연출 완료 대기
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 실제 확인된 교환 제안의 행동력 한 점 확인
            Assert.AreEqual(2, fight.Deck.HandCount); // 선택한 카드와 사용 카드를 이동하고 보충 두 장 확인
            CollectionAssert.DoesNotContain(fight.Deck.Hand, source); // 사용 중인 교환 제안 자체의 재순환 제외 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, source); // 원래 사용 카드의 정상 버림 위치
        } // 취소와 확인된 한 장 및 실제 두 장 보충 비교 종료
        yield return new ExitPlayMode(); // 실제 교환 제안 검사 환경 종료
    } // 교환 제안의 실제 선택 검사 종료

    [UnityTest] // 한 장과 두 장을 모두 직접 완료하며 사전 결제하지 않는 모달
    public IEnumerator HandSortingOnlyCommitsAfterFinishAndDrawsTheConfirmedCount() // 클릭한 후보와 실제 완료를 구분하는 손패 검사
    { // 각 선택 장수의 실제 모달 상태 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 완료 버튼을 구동할 플레이모드 시작
        foreach (int selected in new[] { 1, 2 }) // 첫 선택에서 완료와 두 번째 선택에서 완료를 각각 검증
        { // 선택 수와 실제 완료 여부 비교 시작
            using var fight = new Fight("HAND_SORTING", "THRUST", "KNEE_SLASH", "BLOOD_EDGE"); // 충분한 다른 실제 손패를 가진 원래 전투
            var source = fight.View("HAND_SORTING").RuntimeCard; // 실제 손패 정리 원본
            var original = fight.Deck.Hand.ToArray(); // 아직 손패를 움직이지 않을 원래 실물 배열
            int points = fight.Points.CurrentActionPoints; // 선택만 했을 때 지불하지 않을 현재 공용 행동력
            fight.View("HAND_SORTING").OnPointerClick(null); // 실제 시작 카드의 한 장 이상 선택 모달
            Assert.IsNull(Choice().transform.Find("FinishChoice")); // 영 장에서는 완료 버튼과 행동력 지불 없음 확인
            Choose("Choice_0"); // 비용 없이 첫 번째 실제 후보 선택
            yield return null; // 닫힌 첫 모달 이후 두 번째 화면 표시 대기
            if (selected == 2) // 두 번째 카드까지 확인할 이번 실제 테스트
            { // 두 번째 별개 실물의 선택만 진행
                Choose("Choice_0"); // 첫 번째 실물과 다른 원래 후보 선택
                yield return null; // 후보 없이 완료만 있는 마지막 선택 화면 대기
                Assert.IsNull(Choice().transform.Find("Choice_0")); // 최대 두 카드에서 다른 추가 후보가 없음 확인
                Choose("CancelChoice"); // 두 장을 골라도 명시 완료 이전의 실제 취소
                yield return null; // 전체 선택 취소와 원래 입력 잠금 해제 대기
                Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 두 선택 뒤 취소에도 무지불 확인
                CollectionAssert.AreEquivalent(original, fight.Deck.Hand); // 두 선택 모두 실제로 버리지 않음 확인
                fight.View("HAND_SORTING").OnPointerClick(null); // 두 장을 다시 고를 수 있는 같은 실제 원본
                Choose("Choice_0"); // 첫 번째 선택 다시 기록
                yield return null; // 단계 전환 뒤 실제 두 번째 화면
                Choose("Choice_0"); // 서로 다른 실제 두 번째 선택 기록
                yield return null; // 두 번째 선택 뒤 명시 완료 화면
            } // 두 번째 별개 실물의 선택만 종료
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 후보를 고르기만 한 단계에서는 원래 행동력 그대로 확인
            CollectionAssert.AreEquivalent(original, fight.Deck.Hand); // 확인 전 실제 카드 영역이 보존됐는지 확인
            Choose("FinishChoice"); // 실제 확인 입력으로 선택 묶음 최초 지불
            yield return WaitForCard(fight); // 확정한 장수만큼 버림과 재순환 완료 대기
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 명시 완료에만 원래 한 점 행동력 확인
            Assert.AreEqual(original.Length - 1, fight.Deck.HandCount); // 사용 카드 한 장을 제외하고 고른 수만큼 보충 확인
            CollectionAssert.DoesNotContain(fight.Deck.Hand, source); // 이 카드 자체는 같은 보충에서 뽑지 않음 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, source); // 사용 원본의 원래 정상 버림 위치 확인
        } // 선택 수와 실제 완료 여부 비교 종료
        yield return new ExitPlayMode(); // 실제 손패 정리 검사 종료
    } // 각 선택 장수의 실제 모달 상태 검사 종료

    [UnityTest] // 저장 등록된 실제 일반 카드 하나의 독립 생성과 종료 정리
    public IEnumerator ImprovisationGeneratesASavedCommonCardForTheCasterOnlyForThisBattle() // 실제 보통 희귀도와 소유자 및 독립 실물 확인
    { // 즉흥 조달의 실제 원본과 덱 검사 시작
        yield return new EnterPlayMode(); // 실제 리소스와 공용 손패의 정상 플레이 시작
        using (var fight = new Fight("IMPROVISATION")) // 같은 사용자에게 새 카드 한 장만 받을 실제 전투
        { // 폐쇄된 출발 덱과 생성 원본의 실제 후보 검사 시작
            var source = fight.View("IMPROVISATION").RuntimeCard; // 실제 자기 대상 즉흥 조달 카드
            int points = fight.Points.CurrentActionPoints; // 실제 카드 생성 전 공용 행동력 확인
            Assert.IsNotEmpty(BattleCardActionController.GetImprovisationCandidates(source.SourceData)); // 출시 실제 공통 카드의 생성 후보 존재 확인
            yield return fight.Use("IMPROVISATION"); // 정상 자기 대상 카드로 공용 손패에 실제 새 카드 생성
            var generated = fight.Deck.Hand.Single(); // 유일한 원래 사용 뒤 같은 자리에 도착한 새 카드
            Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 유효 일반 후보가 있는 실제 사용의 한 점 비용 확인
            Assert.AreNotSame(source, generated); // 카드 복구가 아닌 새로운 물리 인스턴스 확인
            Assert.AreNotEqual(source.SourceData.CardId, generated.SourceData.CardId); // 현재 즉흥 조달 자체 재생성 제외 확인
            Assert.AreSame(source.OwnerUnit, generated.OwnerUnit); // 같은 시전자가 가진 새 카드 소유권 확인
            Assert.AreEqual(0, generated.UpgradeLevel); // 원래 영구 카드의 우연한 강화 상속 없이 생성 확인
            var saved = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 출시 저장에서 복원하는 실제 원본 목록
            CollectionAssert.Contains(saved.Cards, generated.SourceData); // 저장에 등록한 실제 원본으로 생성 확인
            Assert.AreEqual("일반", CardSheetCatalog.Load().FindByName(generated.SourceData.DisplayName)?.Rarity); // 실제 시트의 일반 카드인지 확인
            Assert.AreNotEqual(CardEffectType.Unplayable, generated.EffectType); // 사용 불가 효과가 아닌 실제 카드 확인
            foreach (var enemy in fight.Enemies) // 실제 전투 종료로 새 카드가 정리되는지 확인
            { // 실제 마지막 적 제거 시작
                enemy.TakeDamage(1000, BattleDamageType.None); // 정상 상태의 실제 승리 판정 실행
            } // 실제 마지막 적 제거 종료
            Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 기존 전투 종료 처리 확인
            CollectionAssert.DoesNotContain(fight.Deck.Hand, generated); // 생성한 실물이 전투 종료에서 정상 회수됐는지 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, source); // 출발 덱의 실제 원본은 계속 유지 확인
        } // 폐쇄된 출발 덱과 생성 원본의 실제 후보 검사 종료
        yield return new ExitPlayMode(); // 즉흥 조달의 실제 전투 종료
    } // 즉흥 조달의 실제 원본과 덱 검사 종료

    private static BattleDrawChoiceView Choice() // 실제 전투에서 표시한 선택 화면 하나 조회
    { // 활성 선택 모달의 조회 시작
        return Object.FindObjectsByType<BattleDrawChoiceView>(FindObjectsSortMode.None).Single(view => view.gameObject.activeInHierarchy); // 현재 입력 가능한 기존 실제 모달 반환
    } // 활성 선택 모달의 조회 종료

    private static void Choose(string name) // 실제 원래 후보 또는 하단 버튼 입력
    { // 실제 모달 하위 버튼 조작 시작
        Choice().transform.Find(name).GetComponent<Button>().onClick.Invoke(); // 실제 카드 후보와 완료 및 취소 버튼의 원래 이벤트 전달
    } // 실제 모달 하위 버튼 조작 종료

    private static IEnumerator WaitForCard(Fight fight) // 실제 카드 사용 연출 완료 확인
    { // 신규 지원 효과의 기존 연출 대기 시작
        float until = Time.realtimeSinceStartup + 10f; // 씬의 실제 완료를 확인하기 위한 검사 시간
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 실제 연출 대기 중인지 확인
        { // 다음 실제 프레임 진행 시작
            yield return null; // 실제 카드 사용의 효과와 손패 처리 대기
        } // 다음 실제 프레임 진행 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 사용과 표시 정상 완료 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 최종 실제 손패와 카드 위치 일관성 확인
    } // 신규 지원 효과의 기존 연출 대기 종료
} // 세 펜타클 지원 카드 회귀 검사 종료
