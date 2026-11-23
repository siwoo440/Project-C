public interface IDeckCardEntry // 전투 덱 카드 항목 규약
{ // 카드 항목 규약 시작
    CardData Card { get; } // 카드 원본 조회
    CharacterData Owner { get; } // 카드 소유자 조회
    bool IsValid(); // 카드 항목 유효성 검사
} // 카드 항목 규약 종료
