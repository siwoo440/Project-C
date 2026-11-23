using System; // 검사 전투의 자원 해제 계약
using System.Collections; // 실제 플레이 모드 전환 대기
using System.Collections.Generic; // 회수 카드 기록 목록
using System.Linq; // 카드와 유물 검사 원본 조회
using NUnit.Framework; // 실제 카드 위치 검증
using UnityEngine; // 카드와 유닛 원본 생성
using UnityEngine.TestTools; // 유니티 전투 검사
using Object = UnityEngine.Object; // 생성한 검사 원본 정리

public sealed class WinterfallDeckRelicTests // 드로우와 손패 정리 유물 검사
{ // 유물 검사 시작
    [UnityTest] // 봉인테이프와 가죽 수첩의 실제 드로우 검사
    public IEnumerator SealTapeConsumesOneDrawAndCannotImmediatelyRecycleTheCard() // 같은 요청에서 봉인 카드 재드로우 차단 확인
    { // 단일 저주 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 보관함 활성화
        Hold(RelicEffectType.DiscardFirstUnplayableDraw); // 이번 전투의 봉인테이프 획득
        Hold(RelicEffectType.FirstDrawCardDiscount); // 첫 손패 드로우의 가죽 수첩 획득
        using (var fight = new DeckFight(3, BattleType.Normal, MakeCard(CardEffectType.Unplayable))) // 저주만 든 전투 준비
        { // 드로우 검사 시작
            var curse = fight.Deck.Cards[0]; // 처음 드로우할 저주의 원래 객체
            Assert.AreEqual(0, fight.Deck.DrawCards(2)); // 같은 요청에서 저주 대체와 재드로우 차단 확인
            Assert.AreSame(curse, fight.Deck.DiscardPile.Single()); // 봉인한 저주의 정상 버림 위치 확인
            Assert.AreEqual(3, curse.ApCost); // 버림으로 이동한 카드에 첫 드로우 할인 미적용 확인
            Assert.AreEqual(1, fight.Deck.DrawCards(1)); // 다음 드로우에서 전투당 봉인 기회 소진 확인
            Assert.AreSame(curse, fight.Deck.Hand.Single()); // 재순환한 저주의 객체 보존 확인
            Assert.AreEqual(2, curse.ApCost); // 실제 손패에 처음 도착한 카드만 할인 확인
            Assert.IsTrue(fight.Deck.IsStateValid()); // 전체 카드 객체 및 영역 보존 확인
        } // 드로우 검사 종료
        yield return new ExitPlayMode(); // 검사 유물 회차 해제
    } // 단일 저주 검사 종료

    [UnityTest] // 가득 찬 손패와 선택 드로우의 봉인 검사
    public IEnumerator OverflowKeepsSealTapeReadyForTheFirstEligibleChoice() // 초과로 버린 저주의 봉인 기회 보존 확인
    { // 손패 초과 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 보관함 활성화
        Hold(RelicEffectType.DiscardFirstUnplayableDraw); // 이번 전투의 봉인테이프 획득
        var normal = MakeCard(CardEffectType.GainBlock); // 손패를 채울 일반 카드 원본
        var curse = MakeCard(CardEffectType.Unplayable); // 사용불가 카드 원본
        using (var fight = new DeckFight(1, BattleType.Normal, normal, curse, curse, curse)) // 한 장 제한과 저주 세 장의 전투 준비
        { // 손패 초과 검증 시작
            var deck = fight.Deck; // 카드 영역 조회
            PutOnTop(deck, deck.Cards[1]); // 일반 카드 다음의 초과 저주 배치
            PutOnTop(deck, deck.Cards[0]); // 일반 카드를 가장 먼저 드로우하도록 배치
            Assert.AreEqual(1, deck.DrawCards(2)); // 한 장 입장과 한 장 초과 버림 확인
            CollectionAssert.Contains(deck.DiscardPile, deck.Cards[1]); // 첫 저주의 일반 초과 버림 확인
            Assert.IsTrue(deck.DiscardCard(deck.Hand.Single())); // 선택 드로우용 손패 공간 확보
            var choice = deck.PeekDrawTop(1).Single(); // 봉인 발동이 가능한 저주 선택
            Assert.IsTrue(deck.TryDrawChoice(choice, 1)); // 선택 드로우 처리 완료 확인
            CollectionAssert.Contains(deck.DiscardPile, choice); // 빈자리가 열린 첫 저주의 봉인 확인
            Assert.AreEqual(0, deck.HandCount); // 선택한 저주의 손패 유입 차단 확인
            Assert.AreEqual(1, deck.DrawCards(1)); // 세 번째 저주의 일반 손패 드로우 확인
            Assert.AreEqual(CardEffectType.Unplayable, deck.Hand.Single().EffectType); // 같은 전투의 한 번 제한 확인
            Assert.IsTrue(deck.IsStateValid()); // 초과 및 선택 처리 후 영역 보존 확인
        } // 손패 초과 검증 종료
        yield return new ExitPlayMode(); // 검사 유물 회차 해제
    } // 손패 초과 검사 종료

    [UnityTest] // 턴 경계의 실제 회수와 드로우 순서 검사
    public IEnumerator MagnetWaitsForRoomAndRecoversBeforeTheNormalDrawOnce() // 자석의 손패 상한과 회수 한도 확인
    { // 회수 자석 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 보관함 활성화
        Hold(RelicEffectType.RecoverRandomDiscard); // 회수용 자석 획득
        var onlyNormal = MakeCard(CardEffectType.GainBlock); // 빈 버림 더미에서 시작할 일반 카드 원본
        using (var fight = new DeckFight(2, BattleType.Normal, onlyNormal, onlyNormal)) // 회수 후보가 없는 시작 전투 준비
        { // 빈 버림 더미 검사 시작
            Assert.IsTrue(fight.Turns.StartBattle(1)); // 버린 카드 없이 아군 턴 시작
            var first = fight.Deck.Hand.Single(); // 다음 턴에 회수할 기존 카드
            Advance(fight); // 첫 카드의 턴 종료 버림과 다음 아군 턴 시작
            Assert.AreEqual(2, fight.Deck.HandCount); // 다음 턴의 회수 한 장과 일반 드로우 한 장 확인
            Assert.AreSame(first, fight.Deck.Hand[0]); // 빈 더미에서 사용 기회가 보존된 원래 카드 회수 확인
        } // 빈 버림 더미 검사 종료
        var normal = MakeCard(CardEffectType.GainBlock); // 일반 드로우와 버림에 쓸 카드 원본
        var retained = MakeCard(CardEffectType.GainBlock, true); // 턴 종료를 통과하는 유지 카드 원본
        using (var fight = new DeckFight(1, BattleType.Normal, retained, normal, normal, normal)) // 유지 손패와 남은 후보가 있는 전투 준비
        { // 회수 순서 검사 시작
            var deck = fight.Deck; // 손패와 버림 더미 조회
            var saved = deck.Cards[0]; // 첫 턴 종료 후 남아 있어야 할 유지 카드
            PutOnTop(deck, saved); // 첫 드로우가 유지 카드가 되도록 배치
            Assert.AreEqual(1, deck.DrawCards(2)); // 유지 손패와 버린 후보 준비
            var recovered = new List<CardInstance>(); // 실제 회수 카드의 순서와 수 기록
            deck.CardRecovered += recovered.Add; // 카드 회수 흐름 연결
            Assert.IsTrue(fight.Turns.StartBattle(0, true)); // 적 선제로 시작하는 실제 턴 흐름 진입
            Assert.IsEmpty(recovered); // 적 선제 중 자석 발동 차단 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 가득 찬 첫 아군 손패 진입
            Assert.IsEmpty(recovered); // 손패 상한에서 기회 미소비 확인
            Advance(fight); // 유지 손패를 통과하는 일반 턴 정리
            Assert.AreSame(saved, deck.Hand.Single()); // 유지 카드의 다음 아군 턴 생존 확인
            Assert.IsEmpty(recovered); // 다시 가득 찬 손패에서 자석 발동 유예 확인
            Assert.IsTrue(deck.DiscardCard(saved)); // 유지 카드도 효과 버림으로 이동할 수 있는지 확인
            var eligible = deck.DiscardPile.ToArray(); // 다음 턴에 회수할 기존 카드 객체 모음
            var nextDraw = deck.PeekDrawTop(1).Single(); // 자석 뒤 일반 드로우로 처리할 카드
            Advance(fight); // 빈자리가 있는 다음 아군 턴 진입
            Assert.AreEqual(1, recovered.Count); // 전투당 첫 회수 완료 확인
            CollectionAssert.Contains(eligible, recovered[0]); // 버린 후보의 원래 객체 회수 확인
            Assert.AreSame(recovered[0], deck.Hand.Single()); // 일반 드로우보다 먼저 열린 손패 채움 확인
            CollectionAssert.Contains(deck.DiscardPile, nextDraw); // 뒤에 뽑은 일반 카드의 초과 처리 확인
            Assert.AreEqual(0, fight.Turns.LastDrawnCardCount); // 회수는 이번 턴의 실제 드로우 수에서 제외
            Assert.IsTrue(deck.DiscardCard(deck.Hand.Single())); // 다음 자석 시도 확인을 위한 손패 공간 확보
            Advance(fight); // 또 다른 아군 턴 진입
            Assert.AreEqual(1, recovered.Count); // 전투당 한 번을 넘는 회수 금지 확인
            Assert.AreEqual(1, fight.Turns.LastDrawnCardCount); // 후속 턴의 일반 드로우 복원 확인
            Assert.IsTrue(deck.IsStateValid()); // 턴과 회수 뒤 카드 위치 보존 확인
        } // 회수 순서 검사 종료
        yield return new ExitPlayMode(); // 검사 유물 회차 해제
    } // 회수 자석 검사 종료

    [UnityTest] // 손패 자동 정리 전 실제 카드 수 검사
    public IEnumerator PatchCordAddsTwoDrawsOnlyAfterAnActuallyEmptyHand() // 다음 아군 턴 한 번의 드로우 보너스 확인
    { // 패치 노끈 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 보관함 활성화
        Hold(RelicEffectType.EmptyHandNextTurnDraw); // 낡은 패치 노끈 획득
        foreach (var battleType in new[] { BattleType.Normal, BattleType.Boss }) // 정상 라운드와 보스 강제 전환 진행
        { // 턴 전환별 검사 시작
            var normal = MakeCard(CardEffectType.GainBlock); // 순서에 영향을 주지 않는 일반 카드 원본
            using var fight = new DeckFight(5, battleType, normal, normal, normal, normal, normal); // 기본 드로우 한 장의 전투 준비
            Assert.IsTrue(fight.Turns.StartBattle(1)); // 실제 시작 손패 한 장 지급
            Advance(fight); // 한 장을 실제로 남기고 종료
            Assert.AreEqual(1, fight.Turns.LastDrawnCardCount); // 자동 버림으로 비워진 손패의 보너스 차단 확인
            Assert.IsTrue(fight.Deck.DiscardCard(fight.Deck.Hand.Single())); // 종료 전에 직접 버려 손패 비우기
            Advance(fight); // 실제 빈손으로 다음 아군 턴 이동
            Assert.AreEqual(3, fight.Turns.LastDrawnCardCount); // 기본 한 장과 보너스 두 장 확인
            Advance(fight); // 손패를 남긴 턴 종료
            Assert.AreEqual(1, fight.Turns.LastDrawnCardCount); // 다음 턴 한 번으로 예약 소비 확인
            Assert.IsTrue(fight.Deck.IsStateValid()); // 여러 턴 이후 카드 영역 보존 확인
        } // 턴 전환별 검사 종료
        yield return new ExitPlayMode(); // 검사 유물 회차 해제
    } // 패치 노끈 검사 종료

    private static void Advance(DeckFight fight) // 지정 전투의 실제 다음 아군 턴 진입
    { // 턴 전환 시작
        if (fight.Turns.BattleType == BattleType.Boss) // 적 행동을 건너뛰는 보스 전환 확인
        { // 보스 전환 시작
            fight.Turns.QueueBossPhaseTransition(); // 다음 보스 라운드 예약
            Assert.IsTrue(fight.Turns.CompleteBossPhaseTransition()); // 공통 정리와 다음 아군 턴 진행 확인
            return; // 보스 전환 검사 종료
        } // 보스 전환 종료
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 일반 아군 턴 종료 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 턴을 마치고 다음 아군 턴 진행 확인
    } // 턴 전환 종료

    private static void Hold(RelicEffectType effect) // 실제 회차에 검사 유물 획득
    { // 유물 획득 시작
        var relic = Resources.LoadAll<RelicData>("Relics").Single(data => data.EffectType == effect); // 효과에 연결된 실제 유물 원본 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 회차 보관함에 검사 유물 등록 확인
    } // 유물 획득 종료

    private static void PutOnTop(BattleDeckRuntime deck, CardInstance card) // 지정 카드가 먼저 뽑히도록 배치
    { // 카드 배치 시작
        Assert.IsTrue(deck.TryMoveDrawTop(card, deck.DrawPileCount)); // 실제 뽑을 더미의 맨 위 이동 확인
    } // 카드 배치 종료

    private static CardData MakeCard(CardEffectType effect, bool retain = false) // 실제 덱에 등록할 독립 검사 카드 생성
    { // 카드 원본 준비 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 빈 검사 카드 원본
        JsonUtility.FromJsonOverwrite($"{{\"effectType\":{(int)effect},\"apCost\":3,\"retainInHand\":{(retain ? "true" : "false")}}}", card); // 고정 비용과 사용 및 유지 규칙 입력
        return card; // 생성한 카드 원본 반환
    } // 카드 원본 준비 종료

    private sealed class DeckFight : IDisposable // 실제 덱과 턴 경계용 검사 전투
    { // 검사 전투 시작
        private readonly CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 카드 소유자 원본
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 상대 전투 유닛 원본
        private readonly DeckData source; // 생성한 원본 덱
        private readonly CardData[] cards; // 검사 종료에서 해제할 카드 원본
        public BattleDeckRuntime Deck { get; } // 현재 전투의 실제 카드 위치
        public BattleTurnRuntime Turns { get; } // 현재 전투의 실제 진영과 라운드

        public DeckFight(int maxHand, BattleType battleType, params CardData[] sources) // 화면을 제외한 실제 전투 상태 준비
        { // 검사 전투 구성 시작
            cards = sources.Distinct().ToArray(); // 중복 없이 검사 원본 보관
            source = DeckData.CreateRuntime(sources.Select(card => new DeckCardEntry(card, owner)).ToArray()); // 각 카드 원본을 독립 덱 항목으로 생성
            var allies = new[] { BattleUnitRuntime.CreateAlly(owner) }; // 공용 소유자 한 명의 실제 아군
            var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy) }; // 라운드 완료가 가능한 실제 적
            Deck = BattleDeckRuntime.Create(source, allies, maxHand, 240); // 고정 셔플과 지정 손패 상한의 덱
            Turns = new BattleTurnRuntime(Deck, new BattleActionPointRuntime(6), allies, enemies, 1, battleType); // 기본 한 장 드로우의 공용 턴 관리자
        } // 검사 전투 구성 종료

        public void Dispose() // 테스트용 덱과 유닛 및 카드 원본 정리
        { // 검사 자원 정리 시작
            Turns.Dispose(); // 유닛과 턴 신호 연결 해제
            Object.Destroy(source); // 덱 원본 정리
            Object.Destroy(owner); // 아군 원본 정리
            Object.Destroy(enemy); // 적 원본 정리
            foreach (var card in cards) // 독립 카드 원본 순회
            { // 원본 해제 시작
                Object.Destroy(card); // 테스트 카드 원본 정리
            } // 원본 해제 종료
        } // 검사 자원 정리 종료
    } // 검사 전투 종료
} // 유물 검사 종료
