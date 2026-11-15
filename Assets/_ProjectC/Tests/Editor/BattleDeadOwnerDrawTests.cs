using System.Collections.Generic; // 카드 목록 사용
using NUnit.Framework; // 드로우 규칙 검증 사용
using UnityEngine; // 캐릭터와 카드 원본 사용

public sealed class BattleDeadOwnerDrawTests // 사망 소유자 카드 드로우 검증
{ // 드로우 테스트 시작
    private CharacterData firstOwner; // 첫 번째 카드 소유자 원본
    private CharacterData secondOwner; // 두 번째 카드 소유자 원본
    private CardData card; // 공통 테스트 카드 원본
    private DeckData source; // 테스트 덱 원본
    private BattleUnitRuntime firstUnit; // 첫 번째 소유자 전투 유닛
    private BattleUnitRuntime secondUnit; // 두 번째 소유자 전투 유닛
    [SetUp] // 테스트별 준비 지정
    public void SetUp() // 독립 카드 원본 준비
    { // 테스트 준비 시작
        firstOwner = ScriptableObject.CreateInstance<CharacterData>(); // 첫 번째 소유자 생성
        secondOwner = ScriptableObject.CreateInstance<CharacterData>(); // 두 번째 소유자 생성
        card = ScriptableObject.CreateInstance<CardData>(); // 테스트 카드 생성
        source = ScriptableObject.CreateInstance<DeckData>(); // 테스트 덱 원본 생성
        firstUnit = BattleUnitRuntime.CreateAlly(firstOwner); // 첫 번째 소유자 유닛 생성
        secondUnit = BattleUnitRuntime.CreateAlly(secondOwner); // 두 번째 소유자 유닛 생성
    } // 테스트 준비 종료
    [TearDown] // 테스트별 정리 지정
    public void TearDown() // 독립 카드 원본 정리
    { // 테스트 정리 시작
        Object.DestroyImmediate(source); // 테스트 덱 원본 제거
        Object.DestroyImmediate(card); // 테스트 카드 원본 제거
        Object.DestroyImmediate(secondOwner); // 두 번째 소유자 원본 제거
        Object.DestroyImmediate(firstOwner); // 첫 번째 소유자 원본 제거
    } // 테스트 정리 종료
    [Test] // 일반 드로우 검사 지정
    public void Draw_SkipsDeadOwnerWithoutDiscardingAndAllowsRevival() // 사망 카드 보존과 부활 후 드로우 확인
    { // 사망 카드 드로우 검사 시작
        BattleDeckRuntime deck = CreateDeck(3, 2); // 첫 소유자 세 장과 두 번째 소유자 두 장 준비
        firstUnit.ApplyPersistentHealth(0); // 첫 번째 소유자 사망 상태 설정
        Assert.AreEqual(2, deck.DrawCards(10)); // 생존 소유자 카드만 드로우 확인
        Assert.AreEqual(3, deck.DrawPileCount); // 사망 소유자 카드의 덱 내 보존 확인
        Assert.AreEqual(0, deck.DiscardPileCount); // 사망 카드의 버린 더미 이동 방지 확인
        foreach (CardInstance drawn in deck.Hand) // 드로우된 카드 순회
        { // 소유자 확인 시작
            Assert.AreSame(secondUnit, drawn.OwnerUnit); // 생존 소유자의 카드 확인
        } // 소유자 확인 종료
        firstUnit.ApplyPersistentHealth(firstUnit.MaxHealth); // 첫 번째 소유자 부활 상태 설정
        Assert.AreEqual(3, deck.DrawCards(10)); // 부활한 소유자의 카드 재드로우 확인
        Assert.IsTrue(deck.IsStateValid()); // 모든 카드 보존 확인
    } // 사망 카드 드로우 검사 종료
    [Test] // 사망 카드만 남은 덱 검사 지정
    public void Draw_AllOwnersDeadStopsWithoutMovingCards() // 사용 가능한 카드가 없을 때 종료 확인
    { // 사망 카드만 남은 덱 검사 시작
        BattleDeckRuntime deck = CreateDeck(2, 0); // 한 소유자의 카드 두 장 준비
        firstUnit.ApplyPersistentHealth(0); // 카드 소유자 사망 상태 설정
        Assert.AreEqual(0, deck.DrawCards(5)); // 무효 카드만 남을 때 드로우 종료 확인
        Assert.AreEqual(0, deck.PrepareDrawChoices(2).Count); // 바보 선택 후보에서 사망 카드 제외 확인
        Assert.AreEqual(2, deck.DrawPileCount); // 무효 카드 덱 내 보존 확인
        Assert.AreEqual(0, deck.DiscardPileCount); // 무효 카드 버림 방지 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 위치 무결성 확인
    } // 사망 카드만 남은 덱 검사 종료
    [Test] // 사망 카드만 있는 버린 더미 검사 지정
    public void Draw_LeavesDeadOnlyDiscardUntouchedUntilRevival() // 버린 더미의 사망 카드 보존과 부활 확인
    { // 사망 카드 버린 더미 검사 시작
        BattleDeckRuntime deck = CreateDeck(2, 0); // 한 소유자의 카드 두 장 준비
        Assert.AreEqual(2, deck.DrawCards(2)); // 생존 소유자의 카드 드로우
        Assert.IsTrue(deck.DiscardCard(deck.Hand[0])); // 첫 카드 버리기
        Assert.IsTrue(deck.DiscardCard(deck.Hand[0])); // 두 번째 카드 버리기
        firstUnit.ApplyPersistentHealth(0); // 카드 소유자 사망 상태 설정
        Assert.AreEqual(0, deck.DrawCards(2)); // 사망 카드만 있을 때 드로우 종료 확인
        Assert.AreEqual(2, deck.DiscardPileCount); // 사망 카드의 버린 더미 보존 확인
        Assert.AreEqual(0, deck.DrawPileCount); // 불필요한 덱 재구성 방지 확인
        firstUnit.ApplyPersistentHealth(firstUnit.MaxHealth); // 소유자 부활 상태 설정
        Assert.AreEqual(2, deck.DrawCards(2)); // 부활 후 버린 카드 재구성과 드로우 확인
        Assert.IsTrue(deck.IsStateValid()); // 부활 후 카드 위치 무결성 확인
    } // 사망 카드 버린 더미 검사 종료
    [Test] // 버린 더미 재구성 검사 지정
    public void Draw_RecyclesLivingDiscardWhenOnlyDeadCardsRemainOnTop() // 사망 카드 아래 버린 카드 재구성 확인
    { // 사망 카드와 재구성 검사 시작
        BattleDeckRuntime deck = CreateDeck(1, 1); // 소유자별 카드 한 장 준비
        Assert.AreEqual(2, deck.DrawCards(2)); // 두 소유자 카드 드로우
        CardInstance first = FindOwned(deck.Hand, firstUnit); // 첫 번째 소유자 카드 조회
        CardInstance second = FindOwned(deck.Hand, secondUnit); // 두 번째 소유자 카드 조회
        Assert.IsTrue(deck.DiscardCard(first)); // 첫 번째 카드 버리기
        Assert.IsTrue(deck.DiscardCard(second)); // 두 번째 카드 버리기
        firstUnit.ApplyPersistentHealth(0); // 첫 번째 소유자 사망 상태 설정
        IReadOnlyList<CardInstance> choices = deck.PrepareDrawChoices(1); // 생존 카드 선택 후보 준비
        Assert.AreEqual(1, choices.Count); // 생존 카드 한 장 후보 확인
        Assert.AreSame(second, choices[0]); // 생존 소유자 카드 후보 확인
        Assert.IsTrue(deck.TryDrawChoice(second, 1)); // 생존 카드 선택 드로우
        Assert.IsTrue(deck.DiscardCard(second)); // 생존 카드를 다시 버린 더미로 이동
        Assert.AreEqual(1, deck.DrawCards(1)); // 사망 카드만 있는 덱 아래 생존 카드 재구성 확인
        Assert.AreSame(second, deck.Hand[0]); // 재구성 후 생존 카드 드로우 확인
        Assert.AreSame(first, deck.DrawPile[0]); // 사망 카드의 기존 덱 보존 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 위치 무결성 확인
    } // 사망 카드와 재구성 검사 종료
    [Test] // 선택 드로우 검사 지정
    public void Choice_IgnoresDeadCardsAndCanDiscardRemainingLivingTop() // 사망 카드가 위에 있는 바보 선택 확인
    { // 선택 드로우 검사 시작
        BattleDeckRuntime deck = CreateDeck(2, 2); // 두 소유자의 카드 각각 두 장 준비
        CardInstance deadTop = deck.DrawPile[deck.DrawPileCount - 1]; // 실제 덱 맨 위 카드 저장
        deadTop.OwnerUnit.ApplyPersistentHealth(0); // 맨 위 카드의 소유자 사망 상태 설정
        IReadOnlyList<CardInstance> choices = deck.PrepareDrawChoices(2); // 바보의 두 유효 후보 준비
        Assert.AreEqual(2, choices.Count); // 생존 카드 두 장 후보 확인
        Assert.IsFalse(choices[0].OwnerUnit.IsDead); // 첫 후보의 생존 소유자 확인
        Assert.IsFalse(choices[1].OwnerUnit.IsDead); // 두 번째 후보의 생존 소유자 확인
        Assert.IsFalse(deck.TryDrawChoice(deadTop, 2)); // 사망 소유자 카드 선택 차단 확인
        Assert.IsTrue(deck.TryDrawChoice(choices[1], 2)); // 두 번째 유효 카드 선택 확인
        Assert.IsTrue(deck.TryDiscardDrawTop(choices[0])); // 사망 카드 아래 남은 유효 카드 버리기 확인
        Assert.Contains(deadTop, (System.Collections.ICollection)deck.DrawPile); // 사망 카드 덱 내 보존 확인
        Assert.IsTrue(deck.IsStateValid()); // 선택 후 카드 무결성 확인
    } // 선택 드로우 검사 종료
    [Test] // 선택 중 사망 검사 지정
    public void Choice_RejectsCandidateWhoseOwnerDiedAfterPreparation() // 후보 선택 중 소유자 사망 확인
    { // 선택 중 사망 검사 시작
        BattleDeckRuntime deck = CreateDeck(1, 1); // 소유자별 카드 한 장 준비
        IReadOnlyList<CardInstance> choices = deck.PrepareDrawChoices(2); // 생존 상태의 선택 후보 준비
        CardInstance selected = choices[0]; // 첫 번째 선택 후보 저장
        selected.OwnerUnit.ApplyPersistentHealth(0); // 선택 전 소유자 사망 상태 설정
        Assert.IsFalse(deck.TryDrawChoice(selected, 2)); // 사망 후 후보 카드 선택 차단 확인
        Assert.Contains(selected, (System.Collections.ICollection)deck.DrawPile); // 선택 실패 시 카드 덱 보존 확인
        Assert.IsTrue(deck.IsStateValid()); // 선택 실패 후 카드 무결성 확인
    } // 선택 중 사망 검사 종료
    private BattleDeckRuntime CreateDeck(int firstCount, int secondCount) // 소유자별 테스트 덱 생성
    { // 테스트 덱 생성 시작
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 빈 카드 항목 목록 생성
        for (int index = 0; index < firstCount; index++) // 첫 번째 소유자 카드 수 순회
        { // 첫 번째 소유자 카드 추가 시작
            entries.Add(new RunDeckCardEntry(card, firstOwner)); // 첫 번째 소유자 카드 추가
        } // 첫 번째 소유자 카드 추가 종료
        for (int index = 0; index < secondCount; index++) // 두 번째 소유자 카드 수 순회
        { // 두 번째 소유자 카드 추가 시작
            entries.Add(new RunDeckCardEntry(card, secondOwner)); // 두 번째 소유자 카드 추가
        } // 두 번째 소유자 카드 추가 종료
        return BattleDeckRuntime.Create(source, entries, new[] { firstUnit, secondUnit }, 20, 1); // 두 소유자의 실제 전투 덱 반환
    } // 테스트 덱 생성 종료
    private static CardInstance FindOwned(IReadOnlyList<CardInstance> cards, BattleUnitRuntime owner) // 지정 소유자의 카드 조회
    { // 소유 카드 조회 시작
        foreach (CardInstance candidate in cards) // 카드 목록 순회
        { // 카드 소유자 확인 시작
            if (candidate.OwnerUnit == owner) // 지정 소유자 일치 확인
            { // 일치 카드 반환 시작
                return candidate; // 찾은 카드 반환
            } // 일치 카드 반환 종료
        } // 카드 소유자 확인 종료
        return null; // 소유 카드 없음 반환
    } // 소유 카드 조회 종료
} // 드로우 테스트 종료
