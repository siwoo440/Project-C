using System; // 기본 이벤트 기능 사용
using UnityEngine; // 수치 범위 기능 사용
public sealed class BattleMentalRuntime // 개별 유닛 정신력 상태
{ // 클래스 시작
    public const int MinimumMental = 0; // 최소 정신력
    public const int MaximumMental = 100; // 최대 정신력
    public const int NeutralMental = 50; // 특수 상태 종료 정신력
    public const int SpecialStateDuration = 3; // 특수 상태 지속 턴
    private int heavyDamageCountThisTurn; // 턴 내 큰 피해 횟수
    private int healingCountThisTurn; // 턴 내 회복 정신력 횟수
    private int disruptInflictedCountThisTurn; // 턴 내 교란 가함 횟수
    public int CurrentMental { get; private set; } // 현재 정신력
    public BattleMentalState CurrentState { get; private set; } // 현재 정신 상태
    public int RemainingTurns { get; private set; } // 특수 상태 남은 턴
    public bool IsSpecialStateActive => CurrentState != BattleMentalState.Neutral; // 특수 상태 활성 여부
    public event Action<BattleMentalChangeResult> Changed; // 정신력 변화 이벤트
    public BattleMentalRuntime(int initialMental) // 정신력 상태 생성자
    { // 생성자 시작
        CurrentMental = Mathf.Clamp(initialMental, MinimumMental, MaximumMental); // 초기 정신력 범위 보정
        CurrentState = BattleMentalState.Neutral; // 초기 일반 상태 설정
        RemainingTurns = 0; // 초기 남은 턴 설정
        EvaluateThreshold(); // 초기 임계 상태 판정
    } // 생성자 종료
    public bool CanChangeMental(int delta) // 정신력 변경 가능 여부
    { // 변경 가능 확인 시작
        return delta != 0 && !IsSpecialStateActive && Mathf.Clamp(CurrentMental + delta, MinimumMental, MaximumMental) != CurrentMental; // 실제 변경 가능 여부 반환
    } // 변경 가능 확인 종료
    public BattleMentalChangeResult ChangeMental(int delta, BattleMentalChangeReason reason) // 정신력 증감 적용
    { // 정신력 변경 시작
        int adjustedDelta = ApplyTurnLimit(delta, reason); // 원인별 턴 제한 적용
        if (!CanChangeMental(adjustedDelta)) // 변경 가능 여부 확인
        { // 변경 불가 처리 시작
            return BattleMentalChangeResult.NoChange(CurrentMental, CurrentState, RemainingTurns, delta, reason); // 변화 없음 반환
        } // 변경 불가 처리 종료
        int previousMental = CurrentMental; // 변경 전 정신력 저장
        BattleMentalState previousState = CurrentState; // 변경 전 상태 저장
        int previousRemainingTurns = RemainingTurns; // 변경 전 남은 턴 저장
        CurrentMental = Mathf.Clamp(CurrentMental + adjustedDelta, MinimumMental, MaximumMental); // 정신력 범위 내 변경
        EvaluateThreshold(); // 임계 상태 즉시 판정
        BattleMentalChangeResult result = new BattleMentalChangeResult(previousMental, CurrentMental, delta, CurrentMental - previousMental, previousState, CurrentState, previousRemainingTurns, RemainingTurns, reason); // 정신력 변화 결과 생성
        Changed?.Invoke(result); // 정신력 변화 알림
        return result; // 정신력 변화 결과 반환
    } // 정신력 변경 종료
    public BattleMentalChangeResult AdvanceStateTurn() // 특수 상태 턴 감소
    { // 특수 상태 턴 진행 시작
        if (!IsSpecialStateActive || RemainingTurns <= 0) // 진행 대상 확인
        { // 진행 불가 처리 시작
            return BattleMentalChangeResult.NoChange(CurrentMental, CurrentState, RemainingTurns, 0, BattleMentalChangeReason.None); // 변화 없음 반환
        } // 진행 불가 처리 종료
        int previousMental = CurrentMental; // 진행 전 정신력 저장
        BattleMentalState previousState = CurrentState; // 진행 전 상태 저장
        int previousRemainingTurns = RemainingTurns; // 진행 전 남은 턴 저장
        RemainingTurns--; // 남은 턴 감소
        if (RemainingTurns <= 0) // 특수 상태 종료 확인
        { // 종료 처리 시작
            CurrentMental = NeutralMental; // 정신력 중간값 복귀
            CurrentState = BattleMentalState.Neutral; // 일반 상태 복귀
            RemainingTurns = 0; // 남은 턴 초기화
        } // 종료 처리 종료
        BattleMentalChangeReason reason = CurrentState == BattleMentalState.Neutral ? BattleMentalChangeReason.StateDurationEnded : BattleMentalChangeReason.None; // 변화 원인 계산
        BattleMentalChangeResult result = new BattleMentalChangeResult(previousMental, CurrentMental, 0, CurrentMental - previousMental, previousState, CurrentState, previousRemainingTurns, RemainingTurns, reason); // 턴 진행 결과 생성
        Changed?.Invoke(result); // 상태 턴 변화 알림
        return result; // 턴 진행 결과 반환
    } // 특수 상태 턴 진행 종료
    public BattleMentalChangeResult ResolveAtBattleEnd() // 전투 종료 특수 상태 해제
    { // 전투 종료 해제 시작
        if (!IsSpecialStateActive) // 특수 상태 확인
        { // 해제 불필요 처리 시작
            return BattleMentalChangeResult.NoChange(CurrentMental, CurrentState, RemainingTurns, 0, BattleMentalChangeReason.BattleEnded); // 변화 없음 반환
        } // 해제 불필요 처리 종료
        int previousMental = CurrentMental; // 해제 전 정신력 저장
        BattleMentalState previousState = CurrentState; // 해제 전 상태 저장
        int previousRemainingTurns = RemainingTurns; // 해제 전 남은 턴 저장
        CurrentMental = NeutralMental; // 정신력 중간값 복귀
        CurrentState = BattleMentalState.Neutral; // 일반 상태 복귀
        RemainingTurns = 0; // 남은 턴 초기화
        BattleMentalChangeResult result = new BattleMentalChangeResult(previousMental, CurrentMental, 0, CurrentMental - previousMental, previousState, CurrentState, previousRemainingTurns, RemainingTurns, BattleMentalChangeReason.BattleEnded); // 전투 종료 해제 결과 생성
        Changed?.Invoke(result); // 전투 종료 해제 알림
        return result; // 해제 결과 반환
    } // 전투 종료 해제 종료
    public bool ApplyPersistentMental(int currentMental) // 저장 정신력 적용
    { // 저장 정신력 적용 시작
        CurrentMental = Mathf.Clamp(currentMental, MinimumMental, MaximumMental); // 저장 정신력 범위 적용
        CurrentState = BattleMentalState.Neutral; // 저장 시 일반 상태 적용
        RemainingTurns = 0; // 저장 시 남은 턴 초기화
        return true; // 저장 정신력 적용 성공 반환
    } // 저장 정신력 적용 종료
    public void ResetTurnLimits() // 턴별 정신력 횟수 초기화
    { // 횟수 초기화 시작
        heavyDamageCountThisTurn = 0; // 큰 피해 횟수 초기화
        healingCountThisTurn = 0; // 회복 횟수 초기화
        disruptInflictedCountThisTurn = 0; // 교란 가함 횟수 초기화
    } // 횟수 초기화 종료
    private int ApplyTurnLimit(int delta, BattleMentalChangeReason reason) // 원인별 턴 제한 적용
    { // 턴 제한 처리 시작
        if (reason == BattleMentalChangeReason.HeavyDamage) // 큰 피해 원인 확인
        { // 큰 피해 제한 처리 시작
            if (heavyDamageCountThisTurn >= 2) // 최대 횟수 확인
            { // 최대 횟수 처리 시작
                return 0; // 변화 차단 반환
            } // 최대 횟수 처리 종료
            heavyDamageCountThisTurn++; // 큰 피해 횟수 증가
        } // 큰 피해 제한 처리 종료
        else if (reason == BattleMentalChangeReason.HealingReceived) // 회복 원인 확인
        { // 회복 제한 처리 시작
            if (healingCountThisTurn >= 3) // 최대 횟수 확인
            { // 최대 횟수 처리 시작
                return 0; // 변화 차단 반환
            } // 최대 횟수 처리 종료
            healingCountThisTurn++; // 회복 횟수 증가
        } // 회복 제한 처리 종료
        else if (reason == BattleMentalChangeReason.DisruptInflicted) // 교란 가함 원인 확인
        { // 교란 제한 처리 시작
            if (disruptInflictedCountThisTurn >= 2) // 최대 횟수 확인
            { // 최대 횟수 처리 시작
                return 0; // 변화 차단 반환
            } // 최대 횟수 처리 종료
            disruptInflictedCountThisTurn++; // 교란 가함 횟수 증가
        } // 교란 제한 처리 종료
        return delta; // 허용 변화량 반환
    } // 턴 제한 처리 종료
    private void EvaluateThreshold() // 정신력 임계 상태 판정
    { // 임계 상태 판정 시작
        if (CurrentMental >= MaximumMental) // 최대 정신력 확인
        { // 각성 처리 시작
            CurrentMental = MaximumMental; // 최대 정신력 고정
            CurrentState = BattleMentalState.Awakening; // 각성 상태 설정
            RemainingTurns = SpecialStateDuration; // 각성 지속 턴 설정
        } // 각성 처리 종료
        else if (CurrentMental <= MinimumMental) // 최소 정신력 확인
        { // 붕괴 처리 시작
            CurrentMental = MinimumMental; // 최소 정신력 고정
            CurrentState = BattleMentalState.Collapse; // 붕괴 상태 설정
            RemainingTurns = SpecialStateDuration; // 붕괴 지속 턴 설정
        } // 붕괴 처리 종료
    } // 임계 상태 판정 종료
} // 클래스 종료
