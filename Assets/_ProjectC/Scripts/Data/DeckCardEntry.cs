using System;
using UnityEngine;

[Serializable]
public sealed class DeckCardEntry : IDeckCardEntry // 공용 카드 항목 규약 구현
{
    [SerializeField] private CardData card; // 덱에 등록된 카드
    [SerializeField] private CharacterData owner; // 카드 소유 캐릭터

    public CardData Card => card; // 카드 데이터 조회
    public CharacterData Owner => owner; // 카드 소유자 조회

    public DeckCardEntry(CardData cardData, CharacterData ownerData) // 런타임 카드 항목 생성
    { // 생성자 시작
        card = cardData; // 카드 원본 저장
        owner = ownerData; // 카드 소유자 저장
    } // 생성자 종료

    public bool IsValid()
    {
        return card != null && owner != null; // 카드와 소유자 존재 여부 반환
    }
}
