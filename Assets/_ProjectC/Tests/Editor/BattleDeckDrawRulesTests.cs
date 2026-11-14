using System.Collections.Generic; // 카드 목록 사용
using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
public sealed class BattleDeckDrawRulesTests // 드로우 규칙 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void DrawCards_OverflowMovesToDiscard() // 초과 카드 버림 확인
    { // 테스트 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 카드 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 카드 항목 목록 생성
        for (int index = 0; index < 3; index++) // 카드 세 장 반복
        { // 카드 등록 시작
            entries.Add(new RunDeckCardEntry(card, owner)); // 소유 카드 추가
        } // 카드 등록 종료
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(owner); // 아군 전투 유닛 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { unit }, 2, 1); // 최대 손패 두 장 덱 생성
        Assert.AreEqual(2, deck.DrawCards(3)); // 손패에 들어간 카드 수 확인
        Assert.AreEqual(2, deck.HandCount); // 손패 상한 확인
        Assert.AreEqual(1, deck.DiscardPileCount); // 초과 카드 버림 확인
        Assert.AreEqual(0, deck.DrawPileCount); // 뽑을 더미 소진 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 보존 확인
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void Create_EmptyActiveDeckCanEnterBattleWithoutCards() // 사용 가능한 카드가 없는 전투 확인
    { // 테스트 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 생존 캐릭터 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(owner); // 생존 아군 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 사용 가능한 카드 없는 목록
        BattleDeckRuntime deck = null; // 전투 덱 참조 준비
        Assert.DoesNotThrow(() => deck = BattleDeckRuntime.Create(source, entries, new[] { unit }, 20, 1)); // 빈 전투 덱 생성 확인
        Assert.AreEqual(0, deck.DrawCards(8)); // 시작 카드 보충 없이 드로우 종료
        Assert.AreEqual(0, deck.HandCount); // 빈 손패 유지 확인
        Assert.IsTrue(deck.IsStateValid()); // 빈 덱 상태 유효성 확인
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { unit }, new BattleUnitRuntime[0], 4, BattleType.Normal); // 빈 덱 전투 턴 생성
        Assert.IsTrue(turns.StartBattle(8)); // 카드 보충 없는 전투 시작 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 플레이어 턴 종료 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 다음 플레이어 턴 진행 확인
        Assert.AreEqual(0, deck.HandCount); // 이후 턴에도 빈 손패 유지 확인
        turns.Dispose(); // 전투 이벤트 연결 해제
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void DrawChoice_RecyclesDiscardBelowRemainingTopCard() // 선택 드로우와 버린 카드 재구성 확인
    { // 테스트 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 카드 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 카드 항목 목록 생성
        for (int index = 0; index < 4; index++) // 카드 네 장 반복
        { // 카드 등록 시작
            entries.Add(new RunDeckCardEntry(card, owner)); // 소유 카드 추가
        } // 카드 등록 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { BattleUnitRuntime.CreateAlly(owner) }, 3, 1); // 테스트 전투 덱 생성
        deck.DrawCards(3); // 카드 세 장 드로우
        deck.DiscardCard(deck.Hand[0]); // 첫 카드 버리기
        deck.DiscardCard(deck.Hand[0]); // 두 번째 카드 버리기
        CardInstance oldTop = deck.DrawPile[deck.DrawPileCount - 1]; // 기존 덱 맨 위 카드 저장
        IReadOnlyList<CardInstance> choices = deck.PrepareDrawChoices(2); // 맨 위 두 장 선택 준비
        Assert.AreEqual(2, choices.Count); // 선택 후보 두 장 확인
        Assert.AreSame(oldTop, choices[0]); // 기존 맨 위 카드 유지 확인
        Assert.AreEqual(0, deck.DiscardPileCount); // 버린 카드 재구성 확인
        Assert.IsFalse(deck.TryDrawChoice(deck.Hand[0], 2)); // 후보 밖 카드 선택 차단 확인
        Assert.IsTrue(deck.TryDrawChoice(choices[1], 2)); // 두 번째 후보 드로우 확인
        Assert.AreSame(oldTop, deck.DrawPile[deck.DrawPileCount - 1]); // 선택하지 않은 맨 위 카드 유지 확인
        Assert.AreSame(choices[1], deck.Hand[deck.HandCount - 1]); // 선택한 카드 손패 이동 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 보존 확인
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void DrawChoice_FullHandDiscardsSelectedCard() // 손패가 가득 찬 선택 드로우 확인
    { // 테스트 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 카드 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(card, owner), new RunDeckCardEntry(card, owner) }; // 카드 두 장 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { BattleUnitRuntime.CreateAlly(owner) }, 1, 1); // 손패 한 장 제한 덱 생성
        deck.DrawCards(1); // 손패 채우기
        CardInstance choice = deck.PrepareDrawChoices(2)[0]; // 남은 선택 후보 조회
        Assert.IsTrue(deck.TryDrawChoice(choice, 2)); // 가득 찬 손패 선택 처리 확인
        Assert.AreEqual(1, deck.HandCount); // 손패 상한 유지 확인
        Assert.AreSame(choice, deck.DiscardPile[0]); // 초과 선택 카드 버림 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 보존 확인
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void RecoverDiscard_ReturnsSelectedCardAndPreservesOwnership() // 버린 카드 선택 회수 확인
    { // 테스트 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 카드 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(card, owner), new RunDeckCardEntry(card, owner), new RunDeckCardEntry(card, owner) }; // 카드 세 장 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(owner); // 아군 유닛 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { unit }, 3, 1); // 테스트 덱 생성
        deck.DrawCards(2); // 카드 두 장 드로우
        CardInstance recovered = deck.Hand[0]; // 회수할 카드 보관
        deck.DiscardCard(recovered); // 회수할 카드 버리기
        Assert.IsFalse(deck.TryRecoverDiscard(deck.Hand[0])); // 버린 더미 밖 카드 회수 차단 확인
        Assert.IsTrue(deck.TryRecoverDiscard(recovered)); // 버린 카드 회수 확인
        Assert.AreSame(recovered, deck.Hand[deck.HandCount - 1]); // 같은 카드 손패 이동 확인
        Assert.AreSame(unit, recovered.OwnerUnit); // 카드 소유자 유지 확인
        Assert.AreEqual(0, deck.DiscardPileCount); // 버린 더미 비움 확인
        Assert.IsFalse(deck.TryRecoverDiscard(recovered)); // 이미 회수한 카드 재회수 차단 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 보존 확인
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void RecoverDiscard_RejectsFullHandWithoutMovingCard() // 손패가 가득 찬 카드 회수 차단 확인
    { // 테스트 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 카드 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(card, owner), new RunDeckCardEntry(card, owner) }; // 카드 두 장 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { BattleUnitRuntime.CreateAlly(owner) }, 1, 1); // 손패 한 장 제한 덱 생성
        deck.DrawCards(2); // 손패 한 장과 초과 버림 생성
        CardInstance discarded = deck.DiscardPile[0]; // 버린 카드 조회
        Assert.IsFalse(deck.TryRecoverDiscard(discarded)); // 가득 찬 손패 회수 차단 확인
        Assert.AreSame(discarded, deck.DiscardPile[0]); // 버린 카드 위치 유지 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 보존 확인
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void DiscardDrawTop_RejectsOtherCardsAndPreservesDeck() // 맨 위 카드만 버리기 확인
    { // 테스트 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 카드 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(card, owner), new RunDeckCardEntry(card, owner) }; // 카드 두 장 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { BattleUnitRuntime.CreateAlly(owner) }, 2, 1); // 테스트 덱 생성
        IReadOnlyList<CardInstance> choices = deck.PrepareDrawChoices(2); // 덱 위 후보 두 장 조회
        Assert.IsFalse(deck.TryDiscardDrawTop(choices[1])); // 두 번째 카드 버리기 차단 확인
        Assert.IsTrue(deck.TryDiscardDrawTop(choices[0])); // 맨 위 카드 버리기 확인
        Assert.AreSame(choices[0], deck.DiscardPile[0]); // 버린 카드 위치 확인
        Assert.AreSame(choices[1], deck.DrawPile[0]); // 남은 카드 위치 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 보존 확인
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 캐릭터 원본 정리
    } // 테스트 종료
} // 테스트 클래스 종료
