using System; // 기본 이벤트 기능 사용
public sealed class BattleActionPointRuntime // 전투 공용 행동력 관리
{ // 클래스 시작
    private int turnCapacityAdjustment; // 이번 아군 턴 한정의 행동력 상한 변화
    public int MaxActionPoints { get; private set; } // 현재 턴의 최대 공용 행동력
    public int CurrentActionPoints { get; private set; } // 현재 공용 행동력
    public event Action StateChanged; // 행동력 상태 변경 이벤트
    public BattleActionPointRuntime(int maxActionPoints) // 공용 행동력 생성
    { // 생성자 시작
        if (maxActionPoints < 1) // 최대 행동력 범위 확인
        { // 잘못된 행동력 처리 시작
            throw new ArgumentOutOfRangeException(nameof(maxActionPoints)); // 행동력 범위 예외
        } // 잘못된 행동력 처리 종료
        MaxActionPoints = maxActionPoints; // 최대 행동력 저장
        CurrentActionPoints = MaxActionPoints; // 현재 행동력 최대값 초기화
    } // 생성자 종료
    internal void AddTurnCapacity(int amount) // 현재와 최대 행동력을 이번 턴만 함께 증가
    { // 추가 행동력 시작
        int added = Math.Min(amount, int.MaxValue - MaxActionPoints); // 정수 범위 안의 증가량 계산
        if (added > 0) // 실제 증가량 확인
        { // 행동력 확장 시작
            turnCapacityAdjustment += added; // 턴 종료에 제거할 상한 증가량 기록
            MaxActionPoints += added; // 현재 최대 행동력 확장
            CurrentActionPoints += added; // 사용할 수 있는 공용 행동력 지급
            StateChanged?.Invoke(); // 현재 수치와 최대 수치 갱신
        } // 행동력 확장 종료
    } // 추가 행동력 종료
    internal void ReduceTurnCapacity(int amount) // 현재와 최대 행동력을 이번 턴만 함께 감소
    { // 임시 행동력 감소 시작
        int removed = Math.Clamp(amount, 0, MaxActionPoints); // 행동력 상한을 넘지 않는 실제 차감량
        if (removed > 0) // 줄일 행동력 존재 여부 확인
        { // 행동력 감소 반영 시작
            turnCapacityAdjustment -= removed; // 턴 종료에 되돌릴 상한 감소량 기록
            MaxActionPoints -= removed; // 이번 턴의 일시 행동력 상한 감소
            CurrentActionPoints = Math.Max(0, CurrentActionPoints - removed); // 사용할 수 있는 공용 행동력의 동일 차감
            StateChanged?.Invoke(); // 현재 수치와 최대 수치 갱신
        } // 행동력 감소 반영 종료
    } // 임시 행동력 감소 종료
    internal void ClearTurnCapacity() // 끝난 턴의 임시 행동력 상한 변화 회수
    { // 임시 상한 정리 시작
        if (turnCapacityAdjustment != 0) // 증가 또는 감소한 실제 임시 상한 확인
        { // 임시 상한 회수 시작
            MaxActionPoints -= turnCapacityAdjustment; // 이번 턴 이전의 최대 행동력 복원
            CurrentActionPoints = Math.Min(CurrentActionPoints, MaxActionPoints); // 남은 행동력을 원래 범위로 보정
            turnCapacityAdjustment = 0; // 중복 회수 방지
            StateChanged?.Invoke(); // 원래 최대 행동력 표시
        } // 임시 상한 회수 종료
    } // 임시 상한 정리 종료
    public bool CanSpend(int actionPointCost) // 공용 행동력 지불 가능 확인
    { // 지불 가능 검사 시작
        if (actionPointCost < 0) // 음수 비용 확인
        { // 음수 비용 처리 시작
            return false; // 지불 불가 반환
        } // 음수 비용 처리 종료
        return CurrentActionPoints >= actionPointCost; // 보유 행동력 비교 결과 반환
    } // 지불 가능 검사 종료
    public bool Spend(int actionPointCost) // 공용 행동력 지불
    { // 행동력 지불 시작
        if (!CanSpend(actionPointCost)) // 지불 가능 여부 확인
        { // 지불 불가 처리 시작
            return false; // 행동력 지불 실패 반환
        } // 지불 불가 처리 종료
        if (actionPointCost == 0) // 무료 카드 확인
        { // 무료 카드 처리 시작
            return true; // 행동력 지불 성공 반환
        } // 무료 카드 처리 종료
        CurrentActionPoints -= actionPointCost; // 현재 공용 행동력 차감
        StateChanged?.Invoke(); // 행동력 상태 변경 알림
        return true; // 행동력 지불 성공 반환
    } // 행동력 지불 종료
    public int Gain(int amount) // 최대값을 넘지 않는 공용 행동력 회복
    { // 행동력 회복 시작
        int gained = Math.Min(Math.Max(0, amount), MaxActionPoints - CurrentActionPoints); // 실제 회복 가능한 양 계산
        if (gained > 0) // 회복 여유 확인
        { // 행동력 갱신 시작
            CurrentActionPoints += gained; // 실제 회복량 반영
            StateChanged?.Invoke(); // 행동력 표시 갱신
        } // 행동력 갱신 종료
        return gained; // 실제 회복량 반환
    } // 행동력 회복 종료
    public bool Restore() // 공용 행동력 최대 회복
    { // 행동력 회복 시작
        if (CurrentActionPoints == MaxActionPoints) // 최대 행동력 상태 확인
        { // 회복 불필요 처리 시작
            return false; // 행동력 회복 없음 반환
        } // 회복 불필요 처리 종료
        CurrentActionPoints = MaxActionPoints; // 현재 행동력 최대 회복
        StateChanged?.Invoke(); // 행동력 상태 변경 알림
        return true; // 행동력 회복 성공 반환
    } // 행동력 회복 종료
} // 클래스 종료
