using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Deck_New", menuName = "Project C/Data/Deck")]
public sealed class DeckData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string deckId; // 덱 고유 ID
    [SerializeField] private string displayName; // 덱 표시 이름

    [Header("공용 덱")]
    [SerializeField] private List<DeckCardEntry> cards = new List<DeckCardEntry>(); // 공용 덱 카드 목록

    public string DeckId => deckId; // 덱 ID 조회
    public string DisplayName => displayName; // 덱 이름 조회
    public IReadOnlyList<DeckCardEntry> Cards => cards; // 덱 카드 목록 조회
    public int CardCount => cards.Count; // 현재 덱 카드 수 조회

    public bool IsValidDeck()
    {
        if (cards.Count < 1) // 빈 덱 확인
        {
            return false; // 잘못된 덱 반환
        }

        foreach (DeckCardEntry entry in cards) // 덱 카드 순회
        {
            if (entry == null || !entry.IsValid()) // 카드 항목 유효성 확인
            {
                return false; // 잘못된 덱 반환
            }
        }

        return true; // 정상 덱 반환
    }

    public bool AreAllOwnersInParty(PartyData party)
    {
        if (party == null) // 파티 존재 여부 확인
        {
            return false; // 소유자 검사 실패 반환
        }

        foreach (DeckCardEntry entry in cards) // 덱 카드 순회
        {
            if (entry == null || !party.ContainsCharacter(entry.Owner)) // 카드 소유자 출전 여부 확인
            {
                return false; // 출전하지 않은 소유자 발견
            }
        }

        return true; // 모든 소유자 출전 확인
    }
}
