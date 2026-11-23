using System; // 기본 이벤트 기능 사용
public sealed class BattleActionPointRuntime // 전투 공용 행동력 관리
{ // 클래스 시작
    private int turnCapacityAdjustment; // 이번 아군 턴 한정의 행동력 상한 변화
    private int compoundGainBonus; // 이번 실제 아군 턴의 양수 행동력 지급당 복리 합산
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
    internal int ReduceTurnCapacity(int amount) // 이번 턴의 최대 행동력 감소와 현재 행동력에 실제 적용된 양
    { // 임시 행동력 감소 시작
        int removed = Math.Clamp(amount, 0, MaxActionPoints); // 행동력 상한을 넘지 않는 실제 최대치 차감
        if (removed == 0) // 감소가 모두 잘리거나 실제 요청이 없는 경우 확인
        { // 원래 행동력 유지 시작
            return 0; // 현재 사용할 수 있는 행동력에 적용된 감소 없음
        } // 원래 행동력 유지 종료
        int actualLoss = Math.Min(removed, CurrentActionPoints); // 최대치가 줄어도 가용 현재치에서 실제로 빠진 값만 기록
        turnCapacityAdjustment -= removed; // 턴 종료에 되돌릴 상한 감소량 기록
        MaxActionPoints -= removed; // 이번 턴의 일시 행동력 상한 감소
        CurrentActionPoints -= actualLoss; // 영 아래로 내려가지 않게 실제 현재치만 적용
        StateChanged?.Invoke(); // 원래 한 번의 현재치와 최대치 변경 알림
        return actualLoss; // 알림 사이의 다른 행동력 변경과 분리한 명시 감소 적용량
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
    internal bool TrySpendWithHealth(BattleUnitRuntime owner, int actionPointCost, int healthCost) // 계약의 실제 행동력과 비치명 체력을 함께 지불
    { // 두 자원 지불 가능성 확인 시작
        if (actionPointCost <= 0 || !CanSpend(actionPointCost) || owner == null || !owner.CanSpendHealth(healthCost)) // 양수 행동력과 생존 가능한 체력 선결제 검사
        { // 결제 불가 거래의 보존 시작
            return false; // 어느 자원도 지불하거나 알리지 않은 실패
        } // 결제 불가 거래의 보존 종료
        int previousActionPoints = CurrentActionPoints; // 기존 공용 행동력의 실제 값 보관
        CurrentActionPoints -= actionPointCost; // 체력 알림보다 먼저 함께 적용할 공용 행동력 결제
        if (!owner.TrySpendHealth(healthCost)) // 기존 생존과 방어도 보존 규칙으로 체력 지불
        { // 미완료 거래의 상태 복원 시작
            CurrentActionPoints = previousActionPoints; // 외부 알림 없이 원래 공용 행동력 유지
            return false; // 양쪽 모두 완료한 결제만 성공 표시
        } // 미완료 거래의 상태 복원 종료
        StateChanged?.Invoke(); // 공용 행동력과 체력이 모두 반영된 뒤 기존 갱신 알림
        return true; // 두 실제 비용을 지불한 카드 결과
    } // 두 자원 지불 가능성 확인 종료
    internal int AddCompoundGainBonus(int amount) // 이번 실제 아군 턴에 새 복리 효과를 누적
    { // 원본 복리 수치 누적 시작
        int added = Math.Clamp(amount, 0, int.MaxValue - compoundGainBonus); // 양수 원본과 정수 범위까지 누적
        compoundGainBonus += added; // 다음 양수 회복에 함께 적용할 이번 턴의 실제 수치
        return added; // 새로 활성화된 복리 효과 수치
    } // 원본 복리 수치 누적 종료
    internal void ClearCompoundGainBonus() // 실제 아군 턴과 전투의 복리 효과 종료
    { // 복리 효과 초기화 시작
        compoundGainBonus = 0; // 이후 지급에서 끝난 아군 턴의 효과 제거
    } // 복리 효과 초기화 종료
    public int Gain(int amount) // 실제 양수 공용 행동력 지급에 복리 합산
    { // 실제 행동력 회복 요청 시작
        long requested = amount > 0 ? (long)amount + compoundGainBonus : 0L; // 원래 양수 지급과 이번 턴 복리를 한 번 합산
        return GainWithinCapacity(requested); // 한 번의 실제 변화와 원래 상한 적용
    } // 실제 행동력 회복 요청 종료
    internal int GainWithTurnCapacity(int amount) // 무한 계약의 실제 지급에 필요한 이 턴 한정 상한 확보
    { // 원본 지급의 상한 확보 시작
        int needed = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)CurrentActionPoints + amount) - MaxActionPoints); // 기존 상한을 초과하는 원본 지급만 추가
        MaxActionPoints += needed; // 실제 지급 전에 그만큼만 현재 최대치 확장
        turnCapacityAdjustment += needed; // 아군 턴의 원래 정리에서 추가분을 함께 회수
        return Gain(amount); // 기존 복리 가산과 한 번의 상태 알림 및 적용 상한 유지
    } // 원본 지급의 상한 확보 종료
    internal int RestoreUncommittedSpend(int amount) // 무효 원본 카드의 잠정 결제만 취소
    { // 실패한 거래의 임시 행동력 복구 시작
        return GainWithinCapacity(amount); // 실제로 결제한 행동력 한도에서 그대로 되돌림
    } // 실패한 거래의 임시 행동력 복구 종료
    private int GainWithinCapacity(long amount) // 현재 최대값 아래의 공용 행동력 적용
    { // 상한이 있는 행동력 갱신 시작
        int gained = (int)Math.Clamp(amount, 0L, MaxActionPoints - CurrentActionPoints); // 정수 넘침 없이 실제 남은 상한만 적용
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
