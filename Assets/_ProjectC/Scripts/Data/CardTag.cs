using System; // 플래그 열거형 기능 사용

[Flags]
public enum CardTag // 카드 행동 분류
{
    None = 0, // 분류 없음
    Attack = 1 << 0, // 공격 카드
    Magic = 1 << 1, // 마법 카드
    Skill = 1 << 2 // 스킬 카드
}
