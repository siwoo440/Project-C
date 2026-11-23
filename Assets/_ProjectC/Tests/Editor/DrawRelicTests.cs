using System.Collections.Generic; // 테스트 덱 목록 사용
using NUnit.Framework; // 전투 유물 검사 사용
using UnityEngine; // 유니티 원본 데이터 사용

public sealed class DrawRelicTests // 전투 시작 드로우 유물 검사
{ // 유물 검사 시작
    [TestCase("CrackedLens")] // 균열의 렌즈 검사
    [TestCase("SealedEnvelope")] // 봉인된 봉투 검사
    [TestCase("FadedRune")] // 흐린 룬 조각 검사
    public void StartRelicDrawsOnceWithoutRepeatedTurnTrigger(string path) // 시작 드로우와 중복 발동 방지 확인
    { // 유물 검사 본문 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 테스트 카드 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 테스트 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(owner); // 생존 아군 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 테스트 카드 목록 생성
        for (int index = 0; index < 4; index++) // 카드 네 장 준비
        { // 카드 추가 시작
            entries.Add(new RunDeckCardEntry(card, owner)); // 소유 카드 추가
        } // 카드 추가 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { ally }, 2, 1); // 손패 두 장 제한 덱 생성
        Assert.AreEqual(1, deck.DrawCards(1)); // 기존 시작 카드 한 장 준비
        RelicData relic = Resources.Load<RelicData>("Relics/" + path); // 실제 유물 원본 조회
        Assert.IsNotNull(relic); // 유물 원본 확인
        RelicGoldRuntime wallet = new RelicGoldRuntime(); // 독립 지갑 생성
        RelicInventoryRuntime inventory = new RelicInventoryRuntime(wallet); // 독립 유물 보관함 생성
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 실제 유물 획득 확인
        BattleEventDispatcher events = new BattleEventDispatcher(); // 독립 이벤트 발행기 생성
        BattleRelicEffectController controller = new BattleRelicEffectController(events, inventory, wallet, new[] { ally }, new BattleUnitRuntime[0], deck); // 실제 전투 덱 연결
        controller.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 전투 시작 효과 실행
        Assert.AreEqual(2, deck.HandCount); // 유물의 추가 카드 한 장 확인
        events.Publish(new BattleEventContext(BattleEventType.TurnStarted, 2, BattleTurnPhase.PlayerTurn)); // 다음 아군 턴 알림
        events.Publish(new BattleEventContext(BattleEventType.BattleStarted, 2, BattleTurnPhase.PlayerTurn)); // 시작 중복 알림
        Assert.AreEqual(2, deck.HandCount); // 반복 발동 없는 손패 확인
        Assert.AreEqual(0, deck.DiscardPileCount); // 중복 드로우 없는 버림 확인
        controller.Dispose(); // 유물 관리자 정리
        events.Dispose(); // 이벤트 발행기 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 유물 검사 본문 종료
    [Test] // 손패 가득 찬 유물 드로우 검사
    public void StartRelicDiscardsOverflowWhenHandIsFull() // 가득 찬 손패의 초과 드로우 확인
    { // 손패 초과 검사 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 테스트 카드 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 테스트 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(owner); // 생존 아군 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(card, owner), new RunDeckCardEntry(card, owner) }; // 카드 두 장 준비
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { ally }, 1, 1); // 손패 한 장 제한 덱 생성
        deck.DrawCards(1); // 기존 카드로 손패 채우기
        RelicGoldRuntime wallet = new RelicGoldRuntime(); // 독립 지갑 생성
        RelicInventoryRuntime inventory = new RelicInventoryRuntime(wallet); // 유물 보관함 생성
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(Resources.Load<RelicData>("Relics/CrackedLens"))); // 실제 렌즈 획득 확인
        BattleEventDispatcher events = new BattleEventDispatcher(); // 독립 이벤트 발행기 생성
        BattleRelicEffectController controller = new BattleRelicEffectController(events, inventory, wallet, new[] { ally }, new BattleUnitRuntime[0], deck); // 실제 덱 연결
        controller.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 전투 시작 유물 발동
        Assert.AreEqual(1, deck.HandCount); // 손패 상한 유지 확인
        Assert.AreEqual(1, deck.DiscardPileCount); // 초과 카드 버림 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 보존 확인
        controller.Dispose(); // 유물 관리자 정리
        events.Dispose(); // 이벤트 발행기 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 손패 초과 검사 종료
} // 유물 검사 종료
