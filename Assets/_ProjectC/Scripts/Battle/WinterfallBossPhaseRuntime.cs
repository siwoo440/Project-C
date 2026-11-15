using System; // 관리자 연결 해제 기능 사용
public sealed class WinterfallBossPhaseRuntime : IDisposable // 윈터폴 보스 임시 페이즈 관리자
{ // 보스 페이즈 관리자 시작
    private readonly BattleUnitRuntime boss; // 체력을 감시할 보스
    private readonly BattleTurnRuntime turns; // 전투 턴 관리자
    private readonly BattleEnemyActionRuntime actions; // 적 행동 예고 관리자
    private readonly int secondPhasePatternIndex; // 두 번째 페이즈 첫 패턴 순번
    private bool transitioned; // 전환 완료 여부
    private bool pendingDuringEnemyTurn; // 적 턴에 발생한 전환 예약 여부
    private bool disposed; // 연결 해제 여부
    public bool HasPendingTransition => !disposed && !transitioned && turns.HasPendingBossPhaseTransition; // 보스 전환 예약 여부 조회
    public event Action PhaseTransitioned; // 보스 단계 전환 알림
    public WinterfallBossPhaseRuntime(BattleUnitRuntime bossUnit, BattleTurnRuntime turnRuntime, BattleEnemyActionRuntime actionRuntime, int secondPatternIndex) // 보스 전환 관리자 생성
    { // 관리자 생성 시작
        boss = bossUnit ?? throw new ArgumentNullException(nameof(bossUnit)); // 보스 보관
        turns = turnRuntime ?? throw new ArgumentNullException(nameof(turnRuntime)); // 턴 관리자 보관
        actions = actionRuntime ?? throw new ArgumentNullException(nameof(actionRuntime)); // 적 행동 관리자 보관
        secondPhasePatternIndex = Math.Max(0, secondPatternIndex); // 다음 페이즈 패턴 범위 보정
        boss.DamageTaken += HandleDamageTaken; // 실제 피해만 감시
    } // 관리자 생성 종료
    private void HandleDamageTaken(BattleUnitRuntime unit, BattleDamageResult result) // 보스 피해 후 체력 확인
    { // 체력 경계 확인 시작
        if (!disposed && !transitioned && unit == boss && !boss.IsDead && (long)boss.CurrentHealth * 2 <= boss.MaxHealth) // 생존 보스의 절반 이하 체력 확인
        { // 전환 예약 시작
            pendingDuringEnemyTurn = turns.CurrentPhase == BattleTurnPhase.EnemyTurn; // 전환이 발생한 턴 종류 저장
            turns.QueueBossPhaseTransition(); // 미처리 타격 중단용 전환 예약
        } // 전환 예약 종료
    } // 체력 경계 확인 종료
    public bool CompletePendingTransition() // 예약된 보스 페이즈 전환 실행
    { // 보스 전환 실행 시작
        if (!HasPendingTransition || pendingDuringEnemyTurn || !turns.IsPlayerTurn || turns.IsBattleEnded) // 아군 턴에 발생한 전환 예약 확인
        { // 전환 불가 처리 시작
            return false; // 전환 실패 반환
        } // 전환 불가 처리 종료
        transitioned = true; // 중복 페이즈 전환 방지
        actions.ClearActions(); // 이전 페이즈 미처리 행동 취소
        actions.SetBossPhaseStart(boss, secondPhasePatternIndex); // 다음 페이즈의 패턴 시작점 설정
        bool advanced = turns.CompleteBossPhaseTransition(); // 기존 종료 정리와 다음 아군 턴 실행
        if (advanced && !turns.IsBattleEnded) // 다음 아군 턴 진행 확인
        { // 새 예고 준비 시작
            PhaseTransitioned?.Invoke(); // 두 번째 페이즈 화면 알림
            actions.PrepareActions(); // 다음 페이즈의 적 행동 예고 생성
        } // 새 예고 준비 종료
        return advanced; // 실제 전환 결과 반환
    } // 보스 전환 실행 종료
    public bool CompleteAfterEnemyTurn() // 적 턴 종료 중 발생한 전환 마무리
    { // 적 턴 전환 처리 시작
        if (!HasPendingTransition || !pendingDuringEnemyTurn || !turns.IsPlayerTurn || turns.IsBattleEnded) // 적 턴 종료 후 전환 조건 확인
        { // 전환 불가 처리 시작
            return false; // 전환 실패 반환
        } // 전환 불가 처리 종료
        transitioned = true; // 중복 페이즈 전환 방지
        turns.ClearBossPhaseTransition(); // 이미 종료된 적 턴의 전환 예약 해제
        actions.ClearActions(); // 이전 페이즈 미처리 행동 취소
        actions.SetBossPhaseStart(boss, secondPhasePatternIndex); // 다음 페이즈 패턴 시작점 설정
        PhaseTransitioned?.Invoke(); // 두 번째 페이즈 화면 알림
        actions.PrepareActions(); // 다음 페이즈 행동 예고 생성
        return true; // 전환 완료 반환
    } // 적 턴 전환 처리 종료
    public void Dispose() // 보스 체력 감시 해제
    { // 연결 해제 시작
        if (disposed) // 중복 해제 확인
        { // 중복 해제 처리 시작
            return; // 추가 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 해제 완료 기록
        boss.DamageTaken -= HandleDamageTaken; // 보스 피해 감시 해제
    } // 연결 해제 종료
} // 보스 페이즈 관리자 종료
