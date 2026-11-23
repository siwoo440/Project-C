using System; // 기본 이벤트 기능 사용
public sealed class BattleActionPointRuntime // 전투 공용 행동력 관리
{ // 클래스 시작
    public int MaxActionPoints { get; } // 최대 공용 행동력
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
