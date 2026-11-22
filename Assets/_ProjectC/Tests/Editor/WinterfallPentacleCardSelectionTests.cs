using System.Collections; // 실제 전투와 카드 연출 대기
using System.Linq; // 실제 선택 후보와 카드 더미 조회
using NUnit.Framework; // 물리 카드와 행동력 비교
using UnityEngine; // 기존 선택 화면과 원본 카탈로그 조회
using UnityEngine.TestTools; // 실제 플레이 진입과 종료
using UnityEngine.UI; // 공용 카드 선택 버튼 입력
using Fight = WinterfallSwordCombatTests.Fight; // 실제 전투와 손패 입력 검사 환경

public sealed class WinterfallPentacleCardSelectionTests // 연금술과 운명 조작의 실제 입출력 검사
{ // 실제 카드 선택 검사 시작
    [UnityTest] // 손패 일반 카드 한 장을 희귀 카드 한 장으로 변환
    public IEnumerator AlchemyReplacesOnlyTheConfirmedCommonCardForItsOwner() // 원본, 희귀도, 비용과 전투 한정 범위 확인
    { // 원래 게임 플레이 진입
        yield return new EnterPlayMode(); // 실제 카드 모달을 사용하는 환경 진입
        using (var fight = new Fight("ALCHEMY", "THRUST", "DOUBLE_SLASH")) // 일반 한 장과 희귀 대조군이 있는 실제 덱
        { // 실제 변환과 취소 확인 시작
            var played = fight.View("ALCHEMY").RuntimeCard; // 연금술 원래 사용 카드
            var selected = fight.View("THRUST").RuntimeCard; // 유일한 사용 가능한 일반 실물
            var untouched = fight.View("DOUBLE_SLASH").RuntimeCard; // 변환 후보가 아닌 기존 희귀 실물
            int points = fight.Points.CurrentActionPoints; // 확인 전에 지불하지 않을 공용 행동력
            fight.View("ALCHEMY").OnPointerClick(null); // 다른 실물 일반 카드 선택 화면 열기
            Assert.IsNull(Choice().transform.Find("Choice_1")); // 자기 자신과 희귀 카드의 후보 제외 확인
            Choose("CancelChoice"); // 실제 공용 모달 취소
            yield return null; // 닫힌 화면 처리 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 취소에 행동력 지불 없음 확인
            CollectionAssert.AreEquivalent(new[] { played, selected, untouched }, fight.Deck.Hand); // 취소 전후 실제 손패 확인
            fight.View("ALCHEMY").OnPointerClick(null); // 다시 열어 확정 단계의 행동력 부족 준비
            Assert.IsTrue(fight.Points.Spend(points - 2)); // 버튼 확인 전에 공용 행동력을 두 점으로 감소
            Choose("Choice_0"); // 부족한 행동력으로 확정을 시도하는 실제 버튼
            yield return null; // 확정 실패 및 모달 닫힘 대기
            Assert.AreEqual(2, fight.Points.CurrentActionPoints); // 결제 실패의 추가 지출 없음 확인
            CollectionAssert.AreEquivalent(new[] { played, selected, untouched }, fight.Deck.Hand); // 결제 실패의 실제 카드 보존 확인
            fight.Points.Gain(points - 2); // 정상 변환에서 사용할 합법적인 행동력 복구
            fight.View("ALCHEMY").OnPointerClick(null); // 정상적으로 같은 실물로 옵션 표시
            Choose("Choice_0"); // 실제 일반 카드 한 장 확정
            yield return WaitForCard(fight); // 카드 효과와 손패 표시 갱신 대기
            var replacement = fight.Deck.Hand.Single(card => card != untouched); // 원래 희귀 카드를 제외한 유일한 변환 결과
            Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 정상 확인에만 세 점 공용 행동력 지불
            Assert.AreSame(selected.OwnerUnit, replacement.OwnerUnit); // 바뀐 실물의 기존 소유자 확인
            Assert.AreEqual(selected.CardType, replacement.CardType); // 바뀐 실물의 호환 분류 확인
            Assert.AreEqual("희귀", CardSheetCatalog.Load().FindByName(replacement.SourceData.DisplayName)?.Rarity); // 출시 시트에 등록된 희귀도 확인
            CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards, replacement.SourceData); // 실제 보상과 저장에 등록된 새 원본 확인
            CollectionAssert.DoesNotContain(fight.Deck.Cards, selected); // 바뀐 일반 실물의 모든 전투 영역 제거 확인
            CollectionAssert.Contains(fight.Deck.SourceData.Cards.Select(entry => entry.Card), selected.SourceData); // 원래 회차 덱에 일반 카드 유지 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, played); // 연금술 자체의 정상 사용 위치 확인
            fight.Deck.ClearBattleRelicCards(); // 전투가 끝날 때 사용하는 원래 임시 카드 정리
            CollectionAssert.DoesNotContain(fight.Deck.Cards, replacement); // 전투 밖으로 변환 실물 유출 없음 확인
        } // 실제 변환과 취소 확인 종료
        yield return new ExitPlayMode(); // 실제 변환 검사 환경 종료
    } // 원래 게임 플레이 검사 종료

    [UnityTest] // 현재 뽑을 더미 카드의 실물 선택과 이동 검사
    public IEnumerator FateManipulationSelectsTheActualDrawCardWithoutRefillingOrDrawing() // 현재 위치, 카드 강화와 선택 취소 확인
    { // 원래 카드 선택 화면을 실행할 검사 환경 준비
        yield return new EnterPlayMode(); // 실제 전투 플레이 환경 진입
        using (var fight = new Fight("FATE_MANIPULATION", "THRUST", "KNEE_SLASH", "CHARGE")) // 선택과 비선택 실제 카드의 서로 다른 원본 구성
        { // 기존 드로우와 구분되는 실제 선택과 영역 변화 측정 시작
            var played = fight.View("FATE_MANIPULATION").RuntimeCard; // 운명 조작 원래 사용 카드
            Assert.IsTrue(fight.Deck.DiscardCard(fight.View("THRUST").RuntimeCard)); // 기존 버림으로 뽑을 더미 첫 후보 준비
            Assert.IsTrue(fight.Deck.DiscardCard(fight.View("KNEE_SLASH").RuntimeCard)); // 기존 버림으로 뽑을 더미 두 번째 후보 준비
            fight.Deck.PrepareDrawChoices(2); // 기존 규칙으로 실제 뽑을 더미 구성
            var originalDraw = fight.Deck.DrawPile.ToArray(); // 사용 뒤 다른 카드의 순서와 실물 비교
            var blocked = fight.View("CHARGE").RuntimeCard; // 뽑을 더미 밖에 남겨 둘 다른 실물
            Assert.IsTrue(fight.Deck.DiscardCard(blocked)); // 기존 버림은 선택하거나 재구성하면 안 되는 상태
            int points = fight.Points.CurrentActionPoints; // 원래 사용 전 공용 행동력
            int recovered = 0; // 일반 회수의 잘못된 전달 횟수 초기화
            fight.Deck.CardRecovered += _ => recovered++; // 기존 버린 카드 회수 신호 감시
            fight.View("FATE_MANIPULATION").OnPointerClick(null); // 손패나 버림이 아닌 뽑을 더미 선택 화면 열기
            Assert.IsNotNull(Choice().transform.Find("Choice_1")); // 실제 더미의 두 번째 카드 후보도 표시 확인
            Assert.IsNull(Choice().transform.Find("Choice_2")); // 더미 바깥 원본과 버린 실물의 후보 제외 확인
            Choose("CancelChoice"); // 아무 이동을 만들지 않는 원래 취소 버튼
            yield return null; // 공용 모달 취소 종료 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 취소의 행동력 유지 확인
            CollectionAssert.AreEqual(originalDraw, fight.Deck.DrawPile); // 취소에 현재 뽑을 순서 보존 확인
            fight.View("FATE_MANIPULATION").OnPointerClick(null); // 실제 두 후보의 화면 다시 열기
            Choose("Choice_0"); // 현재 뽑을 더미의 맨 위 실제 카드 선택
            yield return WaitForCard(fight); // 실제 손패 이동과 기존 UI 갱신 대기
            var selected = fight.Deck.Hand.Single(); // 손패로 이동한 실제 카드 한 장
            Assert.AreSame(originalDraw[1], selected); // 선택한 실물 그대로 이동과 소유자 및 강화 보존 확인
            CollectionAssert.AreEqual(originalDraw.Take(1), fight.Deck.DrawPile); // 나머지 뽑을 카드 순서 보존 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, blocked); // 기존 버린 카드의 합치기 및 재순환 없음 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, played); // 실제 사용한 카드의 정상 사용 위치 확인
            Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 확인 한 번의 등록된 세 점 비용 확인
            Assert.Zero(recovered); // 버림 회수 효과와 독립된 이동 확인
        } // 기존 드로우와 구분되는 실제 선택과 영역 변화 측정 종료
        yield return new ExitPlayMode(); // 원래 전투 플레이 환경 종료
    } // 실제 운명 조작 검사 종료

    [Test] // 가득 찬 손패에서 사용 카드가 비운 자리에 실물 이동
    public void FateSelectionKeepsTheOtherOwnersUpgradedInstanceAtFullCapacity() // 소유자, 강화와 손패 상한의 실제 런타임 확인
    { // 실제 강화 카드와 서로 다른 소유자 구성 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 기존 출전 아군 원본 조회
        var allies = roster.Characters.Take(2).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 실제 서로 다른 두 아군 소유자
        var offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers; // 실제 저장된 두 카드 조회
        var fate = offers.Single(offer => offer.OfferId == "REWARD_SHEET_FATE_MANIPULATION").Card; // 운명 조작 실제 원본
        var thrust = offers.Single(offer => offer.OfferId == "REWARD_SHEET_THRUST").Card; // 강화할 다른 아군의 일반 카드 원본
        var entry = new RunDeckCardEntry(thrust, allies[1].CharacterSource); // 다른 아군이 소유한 실제 회차 항목
        Assert.IsTrue(entry.TryUpgrade()); // 회차 항목의 실제 카드 강화 확인
        var initial = new DeckCardEntry(fate, allies[0].CharacterSource); // 시전자의 원래 편성 카드 항목
        var source = DeckData.CreateRuntime(new[] { initial }); // 원래 회차 자료와 별도의 실제 편성 원본
        try // 생성한 검사 자료의 해제 보장
        { // 정상 사용으로 빈 자리만 만드는 검사 시작
            var deck = BattleDeckRuntime.Create(source, new IDeckCardEntry[] { initial, entry }, allies, 1, 19); // 실제 한 장 손패 제한과 강화 회차 항목 구성
            var played = deck.Cards.Single(card => card.SourceData == fate); // 뽑을 카드가 아닌 실제 사용 원본 조회
            Assert.IsTrue(deck.TryMoveDrawTop(played, deck.DrawPileCount)); // 실제 초기 덱 맨 위에 원래 시전 카드 배치
            Assert.AreEqual(1, deck.DrawCards(1)); // 실제 한 장의 꽉 찬 손패 구성
            Assert.AreEqual(deck.MaxHandSize, deck.HandCount); // 선택 전에 실제 빈자리가 없음을 확인
            var selected = deck.DrawPile.Single(); // 다른 아군의 현재 뽑을 더미 실물 하나
            Assert.AreEqual(1, selected.UpgradeLevel); // 시전 전에 실제 강화된 런타임 카드 확인
            Assert.IsTrue(deck.TryPlayAndTakeDrawCard(played, selected)); // 시전이 비운 자리에 즉시 다른 카드 이동
            Assert.AreSame(selected, deck.Hand.Single()); // 그대로 유지된 선택 카드 실물 참조 확인
            Assert.AreSame(allies[1], selected.OwnerUnit); // 다른 출전 아군에 속한 실제 소유권 확인
            Assert.AreEqual(1, selected.UpgradeLevel); // 이동 뒤에도 원래 회차 강화 한 단계 유지
            Assert.IsTrue(deck.IsStateValid()); // 사용 버림과 손패 및 뽑을 더미 전체 일관성 확인
        } // 정상 사용으로 빈 자리만 만드는 검사 종료
        finally // 검사 원본의 항상 정리
        { // 검사 자료 정리 시작
            Object.DestroyImmediate(source); // 격리 편성 원본 해제
        } // 검사 자료 정리 종료
    } // 실제 강화 카드와 서로 다른 소유자 구성 종료

    private static BattleDrawChoiceView Choice() // 게임이 실제 표시한 공용 선택 화면 조회
    { // 현재 활성 화면 확인 시작
        return Object.FindObjectsByType<BattleDrawChoiceView>(FindObjectsSortMode.None).Single(view => view.gameObject.activeInHierarchy); // 입력 가능한 화면 한 개 반환
    } // 현재 활성 화면 확인 종료

    private static void Choose(string button) // 실제 게임의 카드 선택이나 취소 버튼 입력
    { // 화면 입력 시작
        Choice().transform.Find(button).GetComponent<Button>().onClick.Invoke(); // 기존 공용 버튼과 실제 처리 코드 호출
    } // 화면 입력 종료

    private static IEnumerator WaitForCard(Fight fight) // 실제 카드 사용과 영역 이동 연출 종료 대기
    { // 실제 연출 상태 대기 시작
        float until = Time.realtimeSinceStartup + 10f; // 전투 연출 최대 대기 시각
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 실제 카드 연출 중인 프레임 확인
        { // 전투 연출 다음 프레임 대기 시작
            yield return null; // 실제 연출과 기본 손패 전개 대기
        } // 전투 연출 다음 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 사용 완료 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 한 실물 카드 한 위치의 기존 덱 검증 확인
    } // 실제 연출 상태 대기 종료
} // 실제 카드 선택 검사 종료
