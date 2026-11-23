using System.Collections; // 전투 연출과 플레이 모드 대기
using System.Linq; // 카드 위치와 유물 원본 조회
using NUnit.Framework; // 전투 규칙 검증
using UnityEngine; // 게임 자산과 검사 카드 생성
using UnityEngine.TestTools; // 실제 카드 입력 환경
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 화면과 런타임 재사용
using Object = UnityEngine.Object; // 검사용 카드 정리

public sealed class WinterfallBattleCardRelicTests // 카드 사용과 실제 드로우 유물 검사
{ // 유물 검사 시작
    [UnityTest] // 첫 핸드 할인과 턴별 성배 발동 검사
    public IEnumerator FirstDrawDiscountStacksWithOpeningDiscountAndChaliceOnlyTriggersOncePerTurn() // 한 장 한정 비용과 성배 사용자 정신력 확인
    { // 카드 비용과 정신력 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입
        yield return CheckCostAndChalice(); // 첫 손패와 아군 턴 전환 검증
        yield return new ExitPlayMode(); // 실제 전투 입력 환경 종료
    } // 카드 비용과 정신력 검사 종료

    private static IEnumerator CheckCostAndChalice() // 두 종류의 행동력 할인과 성배 사용 효과 확인
    { // 비용과 사용 시점 검사 시작
        var manager = Hold(RelicEffectType.FirstDrawCardDiscount, RelicEffectType.OpeningCardDiscount, RelicEffectType.ChaliceMentalOnUse); // 첫 드로우와 성배 유물 및 기존 파편 획득
        var cup = MakeBlockCard("DAY238_TEST_CUP", CardType.Cup); // 정신력과 무관한 고정 비용 성배 카드 준비
        try // 검사 카드 원본 정리 보장
        { // 검사 전투 실행 시작
            using var fight = new Fight(new[] { cup, cup }); // 같은 원본의 독립 카드 두 장을 드로우하는 전투
            using var events = new BattleEventDispatcher(); // 실제 카드 사용 공용 신호 준비
            using var effects = new BattleRelicEffectController(events, manager.Inventory, manager.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 보유 유물 실행 관리자 연결
            using var bridge = new BattleEventController(events, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 전투 손패 사용을 실제 유물 신호에 연결
            var owner = fight.Allies[0]; // 두 성배 카드의 사용자
            int before = owner.CurrentMental; // 성배 사용 전 정신력
            int otherBefore = fight.Allies[1].CurrentMental; // 카드 소유자가 아닌 동료의 정신력
            CollectionAssert.AreEqual(new[] { 1, 2 }, fight.Deck.Hand.Select(card => card.ApCost)); // 첫 카드에만 기존 파편과 드로우 할인이 합산된 비용
            yield return Play(cup, fight); // 첫 성배 카드 실제 사용
            Assert.AreEqual(before + 1, owner.CurrentMental); // 첫 성배 사용자의 정신력 한 점 증가
            yield return Play(cup, fight); // 같은 턴에 두 번째 성배 카드 사용
            Assert.AreEqual(before + 1, owner.CurrentMental); // 같은 턴의 두 번째 정신력 지급 차단
            Assert.AreEqual(otherBefore, fight.Allies[1].CurrentMental); // 카드 소유자가 아닌 동료 정신력 보존
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 손패 할인 만료와 성배 발동 새 기회
            CollectionAssert.AreEqual(new[] { 3, 3 }, fight.Deck.Hand.Select(card => card.ApCost)); // 재드로우한 같은 카드에도 첫 전투 턴 할인 미적용 확인
            yield return Play(cup, fight); // 두 번째 아군 턴의 첫 성배 사용
            Assert.AreEqual(before + 2, owner.CurrentMental); // 다음 아군 턴의 정신력 다시 지급
        } // 검사 전투 실행 종료
        finally // 검사 원본 해제
        { // 검사 카드 정리 시작
            Object.Destroy(cup); // 성배 카드 검사 원본 정리
        } // 검사 카드 정리 종료
    } // 비용과 사용 시점 검사 종료

    [UnityTest] // 펜타클 사용과 최초 실제 카드 효과 드로우 검사
    public IEnumerator PentacleUseDrawAndFirstCardEffectDrawHaveIndependentLimits() // 드로우 유물과 카드 드로우의 별도 발동 확인
    { // 두 드로우 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입
        yield return CheckDraws(); // 손패와 뽑을 더미의 카드 위치 확인
        yield return new ExitPlayMode(); // 실제 전투 입력 환경 종료
    } // 두 드로우 유물 검사 종료

    private static IEnumerator CheckDraws() // 사용 후 드로우 및 전투당 한 번의 효과 증가
    { // 드로우 제한 검사 시작
        Hold(RelicEffectType.PentacleDrawOnUse, RelicEffectType.FirstCardDrawBonus); // 독립 드로우 유물 두 종류 보유
        var pentacle = MakeBlockCard("DAY238_TEST_PENTACLE", CardType.Pentacle); // 자체 드로우가 없는 펜타클 카드 준비
        var shield = MakeBlockCard("DAY238_TEST_SHIELD", CardType.Shield, CardFollowUpEffect.DrawCards); // 한 장을 뽑는 방패 카드 준비
        var filler = MakeBlockCard("DAY238_TEST_FILLER", CardType.Shield); // 순서만 확인할 대조 카드 준비
        try // 검사 카드 원본 정리 보장
        { // 드로우 전투 실행 시작
            using var fight = new Fight(new[] { pentacle, pentacle, shield, shield, filler, filler, filler, filler, filler }); // 두 사용 경로와 드로우용 카드가 있는 전투
            foreach (var item in fight.Deck.Hand.Where(card => card.SourceData == filler).ToArray()) // 보너스로 가져올 대조 카드 순회
            { // 뽑을 카드 준비 시작
                Assert.IsTrue(fight.Deck.DiscardCard(item)); // 비교용 손패를 버린 더미로 이동
            } // 뽑을 카드 준비 종료
            var candidates = fight.Deck.PrepareDrawChoices(5); // 안정된 드로우 순서 확보
            Assert.AreEqual(5, candidates.Count); // 예정된 대조 카드 수 확인
            yield return Play(pentacle, fight); // 첫 펜타클의 손패 이탈과 보너스 지급
            CollectionAssert.Contains(fight.Deck.Hand, candidates[0]); // 펜타클이 떠나 만든 자리에 첫 카드 진입
            Assert.AreEqual(4, fight.Deck.HandCount); // 기본 카드 사용 한 장과 유물 드로우 한 장 확인
            yield return Play(pentacle, fight); // 같은 아군 턴 두 번째 펜타클 사용
            Assert.AreEqual(3, fight.Deck.HandCount); // 같은 턴 두 번째 펜타클 드로우 차단
            Assert.AreEqual(1, fight.Deck.DrawCards(1)); // 일반 드로우로 첫 카드 효과 보너스 비소모 확인
            yield return Play(shield, fight); // 처음으로 수량이 지정된 카드 효과 드로우 사용
            Assert.AreEqual(5, fight.Deck.HandCount); // 원래 한 장과 추가 한 장의 실제 드로우 확인
            CollectionAssert.Contains(fight.Deck.Hand, candidates[3]); // 첫 카드 효과의 추가 한 장까지 실제 손패 이동
            yield return Play(shield, fight); // 두 번째로 같은 카드 효과 사용
            Assert.AreEqual(5, fight.Deck.HandCount); // 두 번째 카드 효과에는 원래 한 장만 드로우
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 펜타클 턴 제한만 갱신
            foreach (var item in fight.Deck.Hand.Where(card => card.SourceData == filler).Take(3).ToArray()) // 다음 아군 턴에서 비교할 뽑기 후보 확보
            { // 뽑을 후보 준비 시작
                Assert.IsTrue(fight.Deck.DiscardCard(item)); // 다음 턴용 드로우 후보를 버린 더미로 이동
            } // 뽑을 후보 준비 종료
            var nextCandidates = fight.Deck.PrepareDrawChoices(3); // 다음 아군 턴의 뽑기 순서 확보
            yield return Play(pentacle, fight); // 다음 턴의 첫 펜타클로 다시 드로우
            CollectionAssert.Contains(fight.Deck.Hand, nextCandidates[0]); // 새 아군 턴의 펜타클 발동 갱신 확인
            yield return Play(shield, fight); // 다음 턴의 첫 카드 효과 드로우 사용
            CollectionAssert.AreEqual(new[] { nextCandidates[2] }, fight.Deck.DrawPile); // 카드 효과의 전투당 보너스 미갱신 확인
            Assert.IsTrue(fight.Deck.IsStateValid()); // 모든 사용과 드로우 뒤 카드 위치 보존
        } // 드로우 전투 실행 종료
        finally // 검사 카드 원본 해제
        { // 드로우 검사 정리 시작
            Object.Destroy(pentacle); // 펜타클 검사 카드 원본 정리
            Object.Destroy(shield); // 방패 검사 카드 원본 정리
            Object.Destroy(filler); // 드로우 대조 카드 원본 정리
        } // 드로우 검사 정리 종료
    } // 드로우 제한 검사 종료

    private static IEnumerator Play(CardData card, Fight fight) // 자체 대상 카드의 실제 손패 입력
    { // 카드 사용 전달 시작
        return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, null, fight.Runner, fight.Deck, fight.Points); // 실제 사용과 비용 및 더미 이동 검증 재사용
    } // 카드 사용 전달 종료

    private static CardData MakeBlockCard(string id, CardType type, CardFollowUpEffect followUp = CardFollowUpEffect.None) // 소유자 방어도와 선택적 한 장 드로우 원본 생성
    { // 검사 원본 생성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 임시 카드 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"{id}\",\"displayName\":\"{id}\",\"cardType\":{(int)type},\"targetType\":{(int)CardTargetType.Self},\"apCost\":3,\"effectType\":{(int)CardEffectType.GainBlock},\"effectValue\":1,\"secondaryEffectValue\":1,\"followUpEffect\":{(int)followUp}}}", card); // 수량과 사용 비용이 고정된 방어 카드 설정
        return card; // 실제 덱에서 사용할 원본 반환
    } // 검사 원본 생성 종료

    private static RelicRunManager Hold(params RelicEffectType[] types) // 실제 원본 효과의 유물을 회차에 획득
    { // 회차 유물 준비 시작
        var manager = RelicRunManager.EnsureInstance(); // 실제 회차 유물 관리자
        var originals = Resources.LoadAll<RelicData>("Relics"); // 실제 게임 유물 목록
        foreach (var type in types) // 이번 검사에 사용할 유물 효과 순회
        { // 유물 획득 시작
            var relic = originals.Single(item => item.EffectType == type); // 요청한 효과의 유일한 원본 조회
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic), relic.DisplayName); // 실제 회차 유물 획득 확인
        } // 유물 획득 종료
        return manager; // 발동 관리자에 연결할 회차 반환
    } // 회차 유물 준비 종료
} // 유물 검사 종료
