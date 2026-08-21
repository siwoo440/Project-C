public sealed class BattleMentalChangeResult // 정신력 변화 결과
{ // 클래스 시작
    public int PreviousMental { get; } // 변경 전 정신력
    public int CurrentMental { get; } // 변경 후 정신력
    public int RequestedDelta { get; } // 요청 변화량
    public int AppliedDelta { get; } // 실제 변화량
    public BattleMentalState PreviousState { get; } // 변경 전 상태
    public BattleMentalState CurrentState { get; } // 변경 후 상태
    public int PreviousRemainingTurns { get; } // 변경 전 남은 턴
    public int RemainingTurns { get; } // 변경 후 남은 턴
    public BattleMentalChangeReason Reason { get; } // 변화 원인
    public bool WasApplied => AppliedDelta != 0 || PreviousState != CurrentState || PreviousRemainingTurns != RemainingTurns; // 실제 변화 여부
    public bool StateStarted => PreviousState == BattleMentalState.Neutral && CurrentState != BattleMentalState.Neutral; // 특수 상태 시작 여부
    public bool StateEnded => PreviousState != BattleMentalState.Neutral && CurrentState == BattleMentalState.Neutral; // 특수 상태 종료 여부
    public BattleMentalChangeResult(int previousMental, int currentMental, int requestedDelta, int appliedDelta, BattleMentalState previousState, BattleMentalState currentState, int previousRemainingTurns, int remainingTurns, BattleMentalChangeReason reason) // 변화 결과 생성자
    { // 생성자 시작
        PreviousMental = previousMental; // 변경 전 정신력 저장
        CurrentMental = currentMental; // 변경 후 정신력 저장
        RequestedDelta = requestedDelta; // 요청 변화량 저장
        AppliedDelta = appliedDelta; // 실제 변화량 저장
        PreviousState = previousState; // 변경 전 상태 저장
        CurrentState = currentState; // 변경 후 상태 저장
        PreviousRemainingTurns = previousRemainingTurns; // 변경 전 남은 턴 저장
        RemainingTurns = remainingTurns; // 변경 후 남은 턴 저장
        Reason = reason; // 변화 원인 저장
    } // 생성자 종료
    public static BattleMentalChangeResult NoChange(int mental, BattleMentalState state, int remainingTurns, int requestedDelta, BattleMentalChangeReason reason) // 변화 없음 결과 생성
    { // 변화 없음 생성 시작
        return new BattleMentalChangeResult(mental, mental, requestedDelta, 0, state, state, remainingTurns, remainingTurns, reason); // 변화 없음 결과 반환
    } // 변화 없음 생성 종료
} // 클래스 종료
