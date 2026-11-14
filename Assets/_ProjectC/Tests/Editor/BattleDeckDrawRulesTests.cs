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
} // 테스트 클래스 종료
