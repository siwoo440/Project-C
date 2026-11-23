public enum BattleStatusEffectRemovalReason // 상태 제거 원인
{ // 열거형 시작
    None, // 제거 없음
    DurationExpired, // 지속시간 만료
    Cleansed, // 정화 제거
    UnitDied // 사망 제거
} // 열거형 종료
public sealed class BattleStatusEffectProcessResult // 상태 처리 결과
{ // 클래스 시작
    public BattleStatusEffectType EffectType { get; } // 처리 상태 종류
    public int Round { get; } // 처리 라운드
    public int EffectiveValue { get; } // 처리 시점 전체 수치
    public int AppliedAmount { get; } // 실제 피해 또는 회복량
    public int PreviousRemainingTurns { get; } // 처리 전 남은 횟수
    public int RemainingTurns { get; } // 처리 후 남은 횟수
    public int StackCount { get; } // 처리 시점 중첩 수
    public bool WasTriggered { get; } // 발동 효과 여부
    public BattleStatusEffectRemovalReason RemovalReason { get; } // 상태 제거 원인
    public bool WasRemoved => RemovalReason != BattleStatusEffectRemovalReason.None; // 상태 제거 여부
    public bool WasCleansed => RemovalReason == BattleStatusEffectRemovalReason.Cleansed; // 정화 제거 여부
    public bool WasExpired => RemovalReason == BattleStatusEffectRemovalReason.DurationExpired; // 자연 만료 여부
    public BattleStatusEffectProcessResult(BattleStatusEffectType effectType, int round, int effectiveValue, int appliedAmount, int previousRemainingTurns, int remainingTurns, int stackCount, bool wasTriggered, BattleStatusEffectRemovalReason removalReason) // 상태 결과 생성
    { // 생성자 시작
        EffectType = effectType; // 상태 종류 저장
        Round = round; // 처리 라운드 저장
        EffectiveValue = effectiveValue; // 전체 상태 수치 저장
        AppliedAmount = appliedAmount; // 실제 적용 수치 저장
        PreviousRemainingTurns = previousRemainingTurns; // 처리 전 횟수 저장
        RemainingTurns = remainingTurns; // 처리 후 횟수 저장
        StackCount = stackCount; // 상태 중첩 수 저장
        WasTriggered = wasTriggered; // 발동 여부 저장
        RemovalReason = removalReason; // 제거 원인 저장
    } // 생성자 종료
} // 클래스 종료
