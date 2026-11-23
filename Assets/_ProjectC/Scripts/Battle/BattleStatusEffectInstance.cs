using System; // 기본 수학 기능 사용
public sealed class BattleStatusEffectInstance // 개별 상태 이상 런타임 정보
{ // 클래스 시작
    public BattleStatusEffectType EffectType { get; } // 상태 이상 종류
    public int Value { get; private set; } // 중첩당 효과 수치
    public int RemainingTurns { get; private set; } // 남은 발동 횟수
    public int StackCount { get; private set; } // 현재 중첩 수
    public int MaximumStacks { get; private set; } // 최대 중첩 수
    public int EffectiveValue => Value * StackCount; // 전체 적용 수치
    public bool IsExpired => RemainingTurns <= 0; // 지속 종료 여부
    public string DisplayName => GetDisplayName(EffectType); // 화면 표시 이름
    public bool IsDebuff => IsDebuffType(EffectType); // 디버프 여부
    public BattleStatusEffectInstance(BattleStatusEffectType effectType, int value, int duration, int maximumStacks) // 상태 이상 생성
    { // 생성자 시작
        if (effectType == BattleStatusEffectType.None) // 상태 이상 종류 확인
        { // 잘못된 종류 처리 시작
            throw new ArgumentException("상태 이상 종류가 필요합니다.", nameof(effectType)); // 종류 누락 예외
        } // 잘못된 종류 처리 종료
        EffectType = effectType; // 상태 이상 종류 저장
        Value = Math.Max(1, value); // 최소 효과 수치 보정
        RemainingTurns = Math.Max(1, duration); // 최소 지속 횟수 보정
        MaximumStacks = Math.Max(1, maximumStacks); // 최소 최대 중첩 보정
        StackCount = 1; // 첫 중첩 적용
    } // 생성자 종료
    public void Refresh(int value, int duration, int maximumStacks) // 동일 상태 이상 재적용
    { // 재적용 시작
        Value = Math.Max(Value, Math.Max(1, value)); // 높은 효과 수치 유지
        RemainingTurns = Math.Max(RemainingTurns, Math.Max(1, duration)); // 긴 지속 횟수 유지
        MaximumStacks = Math.Max(MaximumStacks, Math.Max(1, maximumStacks)); // 높은 최대 중첩 유지
        StackCount = Math.Min(MaximumStacks, StackCount + 1); // 최대 범위 중첩 증가
    } // 재적용 종료
    public void AdvanceDuration() // 지속 횟수 감소
    { // 지속 감소 시작
        RemainingTurns = Math.Max(0, RemainingTurns - 1); // 남은 횟수 한 단계 감소
    } // 지속 감소 종료
    public static string GetDisplayName(BattleStatusEffectType effectType) // 상태 이상 표시 이름 조회
    { // 표시 이름 조회 시작
        switch (effectType) // 상태 이상 종류 분기
        { // 종류 분기 시작
            case BattleStatusEffectType.Poison: // 중독 종류
                return "중독"; // 중독 이름 반환
            case BattleStatusEffectType.Regeneration: // 재생 종류
                return "재생"; // 재생 이름 반환
            case BattleStatusEffectType.AttackPowerUp: // 공격력 증가 종류
                return "공격 증가"; // 공격력 증가 이름 반환
            default: // 알 수 없는 종류
                return "없음"; // 기본 이름 반환
        } // 종류 분기 종료
    } // 표시 이름 조회 종료
    public static bool IsDebuffType(BattleStatusEffectType effectType) // 디버프 종류 확인
    { // 디버프 확인 시작
        return effectType == BattleStatusEffectType.Poison; // 중독 디버프 결과 반환
    } // 디버프 확인 종료
} // 클래스 종료
