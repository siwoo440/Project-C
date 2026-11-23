public sealed class RunDeckCardEntry : IDeckCardEntry // 탐사 회차용 카드 보유 항목
{
    public CardData Card { get; }
    public CharacterData Owner { get; }
    public int UpgradeLevel { get; private set; }
    public bool IsUpgraded => UpgradeLevel > 0; // 강화 여부 조회
    public bool CanUpgrade => IsValid() && UpgradeLevel < 1; // Prototype 1회 강화 가능 여부

    public RunDeckCardEntry(CardData cardData, CharacterData ownerData) // 회차 카드 항목 생성
    {
        Card = cardData; // 카드 원본 저장
        Owner = ownerData; // 카드 소유자 저장
        UpgradeLevel = 0; // 시작 강화 단계 초기화
    }

    public bool TryUpgrade() // 카드 1회 강화 시도
    {
        if (!CanUpgrade) // 강화 가능 여부 확인
        {
            return false; // 강화 실패 반환
        }

        UpgradeLevel = 1; // Prototype 강화 단계 적용
        return true; // 강화 성공 반환
    }

    public bool IsValid() // 회차 카드 유효성 검사
    {
        return Card != null && Owner != null; // 카드와 소유자 존재 반환
    }
}
