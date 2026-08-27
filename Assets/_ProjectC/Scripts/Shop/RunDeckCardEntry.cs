public sealed class RunDeckCardEntry : IDeckCardEntry // 탐사 회차용 카드 보유 항목
{ // 회차 카드 항목 시작
    public CardData Card { get; } // 카드 원본 조회
    public CharacterData Owner { get; } // 카드 소유자 조회

    public RunDeckCardEntry(CardData cardData, CharacterData ownerData) // 회차 카드 항목 생성
    { // 회차 카드 생성 시작
        Card = cardData; // 카드 원본 저장
        Owner = ownerData; // 카드 소유자 저장
    } // 회차 카드 생성 종료

    public bool IsValid() // 회차 카드 유효성 검사
    { // 회차 카드 검사 시작
        return Card != null && Owner != null; // 카드와 소유자 존재 반환
    } // 회차 카드 검사 종료
} // 회차 카드 항목 종료
