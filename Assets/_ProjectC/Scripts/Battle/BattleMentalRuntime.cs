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
        return PreviewDelta(delta) != 0; // 특수 상태를 포함한 실제 변경 가능 여부 반환
    } // 변경 가능 확인 종료
    internal int PreviewDelta(int delta) // 턴 제한 없는 정신력 변화의 실제 적용 범위 조회
    { // 정신력 변화 범위 시작
        return ClampMental((long)CurrentMental + delta) - CurrentMental; // 실제 정신력 범위로 잘린 변화량 반환
    } // 정신력 변화 범위 종료
    public BattleMentalChangeResult ChangeMental(int delta, BattleMentalChangeReason reason) // 정신력 증감 적용
    { // 직접 정신력 변화 요청 시작
        return ChangeMental(delta, reason, delta); // 별도 보정이 없는 기존 요청 연결
    } // 직접 정신력 변화 요청 종료
    internal BattleMentalChangeResult ChangeMental(int delta, BattleMentalChangeReason reason, int requestedDelta, BattleUnitRuntime sourceUnit = null, Func<BattleMentalChangeReason, bool> preventIncomingLoss = null) // 원래 요청과 실제 적용할 정신 손실의 보호 확인
    { // 정신력 변경 시작
        if (!CanChangeMental(delta) || !HasTurnAllowance(reason)) // 실제 수치 변화와 기존 턴 제한 확인
        { // 일반 변경 불가 처리 시작
            return BattleMentalChangeResult.NoChange(CurrentMental, CurrentState, RemainingTurns, requestedDelta, reason); // 수치 또는 턴 제한으로 차단된 원래 요청 반환
        } // 일반 변경 불가 처리 종료
        if (delta < 0 && preventIncomingLoss?.Invoke(reason) == true) // 실제 감소가 가능한 입력에만 전투 보호 확인
        { // 정신 피해 방어 처리 시작
            return BattleMentalChangeResult.NoChange(CurrentMental, CurrentState, RemainingTurns, requestedDelta, reason); // 원래 요청을 보존한 정신 피해 무효 처리
        } // 정신 피해 방어 처리 종료
        RecordTurnLimitedChange(reason); // 실제 적용하는 변화만 기존 턴 제한에 반영
        int previousMental = CurrentMental; // 변경 전 정신력 저장
        BattleMentalState previousState = CurrentState; // 변경 전 상태 저장
        int previousRemainingTurns = RemainingTurns; // 변경 전 남은 턴 저장
        CurrentMental = ClampMental((long)CurrentMental + delta); // 정신력 범위 내 실제 수치 변경
        if (!IsSpecialStateActive) // 기존 특수 상태 활성 여부 확인
        { // 일반 상태 임계 판정 시작
            EvaluateThreshold(); // 일반 상태에서만 새 특수 상태 판정
        } // 일반 상태 임계 판정 종료
        BattleMentalChangeResult result = new BattleMentalChangeResult(previousMental, CurrentMental, requestedDelta, CurrentMental - previousMental, previousState, CurrentState, previousRemainingTurns, RemainingTurns, reason, sourceUnit); // 원래 요청과 보정 변화량 및 실제 발생 유닛 기록
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
        int previousMental = CurrentMental; // 적용 전 정신력 보관
        BattleMentalState previousState = CurrentState; // 적용 전 정신 상태 보관
        int previousRemainingTurns = RemainingTurns; // 적용 전 남은 턴 보관
        CurrentMental = Mathf.Clamp(currentMental, MinimumMental, MaximumMental); // 저장 정신력 범위 적용
        CurrentState = BattleMentalState.Neutral; // 저장 시 일반 상태 적용
        RemainingTurns = 0; // 저장 시 남은 턴 초기화
        EvaluateThreshold(); // 전투 시작 정신력의 각성·붕괴 판정
        BattleMentalChangeResult result = new BattleMentalChangeResult(previousMental, CurrentMental, CurrentMental - previousMental, CurrentMental - previousMental, previousState, CurrentState, previousRemainingTurns, RemainingTurns, BattleMentalChangeReason.PersistentState); // 저장 적용 변화 결과 생성
        if (result.WasApplied) // 정신 상태 또는 수치 변화 확인
        { // 저장 변화 알림 시작
            Changed?.Invoke(result); // 저장 정신력과 특수 상태 변경 알림
        } // 저장 변화 알림 종료
        return true; // 저장 정신력 적용 성공 반환
    } // 저장 정신력 적용 종료
    private static int ClampMental(long value) // 큰 변화량을 포함한 정신력 범위 보정
    { // 정신력 범위 보정 시작
        return (int)Math.Max(MinimumMental, Math.Min(MaximumMental, value)); // 안전한 정신력 범위 반환
    } // 정신력 범위 보정 종료
    public void ResetTurnLimits() // 턴별 정신력 횟수 초기화
    { // 횟수 초기화 시작
        heavyDamageCountThisTurn = 0; // 큰 피해 횟수 초기화
        healingCountThisTurn = 0; // 회복 횟수 초기화
        disruptInflictedCountThisTurn = 0; // 교란 가함 횟수 초기화
    } // 횟수 초기화 종료
    private bool HasTurnAllowance(BattleMentalChangeReason reason) // 원인별 턴 사용 가능 횟수 조회
    { // 횟수 조회 시작
        return reason switch // 정신력 변화 원인별 기존 한도 검사
        { // 원인별 횟수 검사 시작
            BattleMentalChangeReason.HeavyDamage => heavyDamageCountThisTurn < 2, // 큰 피해 적용 횟수 두 번 제한
            BattleMentalChangeReason.HealingReceived => healingCountThisTurn < 3, // 회복 적용 횟수 세 번 제한
            BattleMentalChangeReason.DisruptInflicted => disruptInflictedCountThisTurn < 2, // 교란 가함 횟수 두 번 제한
            _ => true // 그 외 원인의 일반 수치 적용
        }; // 원인별 횟수 검사 종료
    } // 횟수 조회 종료
    private void RecordTurnLimitedChange(BattleMentalChangeReason reason) // 실제로 적용한 한도 대상 정신력 변화 기록
    { // 적용 횟수 기록 시작
        switch (reason) // 적용 횟수에 제한이 있는 원인 확인
        { // 원인별 적용 횟수 기록 시작
            case BattleMentalChangeReason.HeavyDamage: // 큰 피해 적용 확인
                heavyDamageCountThisTurn++; // 큰 피해 사용 횟수 증가
                break; // 큰 피해 횟수 기록 종료
            case BattleMentalChangeReason.HealingReceived: // 회복 적용 확인
                healingCountThisTurn++; // 회복 사용 횟수 증가
                break; // 회복 횟수 기록 종료
            case BattleMentalChangeReason.DisruptInflicted: // 교란 가함 적용 확인
                disruptInflictedCountThisTurn++; // 교란 가함 사용 횟수 증가
                break; // 교란 가함 횟수 기록 종료
        } // 원인별 적용 횟수 기록 종료
    } // 적용 횟수 기록 종료
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
