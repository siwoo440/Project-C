using System; // 기본 인터페이스 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleStatusEffectController : IDisposable // 상태 이상 발동 시점 관리
{ // 클래스 시작
    private readonly BattleTurnRuntime turnRuntime; // 연결된 턴 관리자
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 아군 유닛 목록
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 적 유닛 목록
    private bool disposed; // 연결 해제 여부
    public BattleStatusEffectController(BattleTurnRuntime battleTurn, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies) // 상태 이상 관리자 생성
    { // 생성자 시작
        turnRuntime = battleTurn ?? throw new ArgumentNullException(nameof(battleTurn)); // 턴 관리자 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        turnRuntime.PhaseStarted += HandlePhaseStarted; // 진영 턴 시작 이벤트 등록
    } // 생성자 종료
    private void HandlePhaseStarted(BattleTurnPhase phase, int round) // 진영 턴 시작 처리
    { // 턴 시작 처리 시작
        if (disposed || turnRuntime.IsBattleEnded) // 처리 가능 상태 확인
        { // 처리 불가 처리 시작
            return; // 상태 이상 처리 중단
        } // 처리 불가 처리 종료
        if (phase == BattleTurnPhase.PlayerTurn) // 플레이어 턴 확인
        { // 아군 상태 처리 시작
            ProcessUnits(allyUnits, round); // 아군 상태 이상 발동
        } // 아군 상태 처리 종료
        else if (phase == BattleTurnPhase.EnemyTurn) // 적 턴 확인
        { // 적 상태 처리 시작
            ProcessUnits(enemyUnits, round); // 적 상태 이상 발동
        } // 적 상태 처리 종료
    } // 턴 시작 처리 종료
    private void ProcessUnits(IReadOnlyList<BattleUnitRuntime> units, int round) // 진영 상태 이상 일괄 처리
    { // 일괄 처리 시작
        for (int unitIndex = 0; unitIndex < units.Count; unitIndex++) // 유닛 목록 순회
        { // 유닛 상태 처리 시작
            if (turnRuntime.IsBattleEnded) // 전투 종료 여부 확인
            { // 전투 종료 처리 시작
                return; // 남은 상태 처리 중단
            } // 전투 종료 처리 종료
            BattleUnitRuntime runtimeUnit = units[unitIndex]; // 현재 유닛 조회
            if (runtimeUnit == null || runtimeUnit.IsDead) // 유효한 생존 유닛 확인
            { // 처리 제외 시작
                continue; // 다음 유닛 이동
            } // 처리 제외 종료
            runtimeUnit.ProcessStatusEffectsAtPhaseStart(round); // 현재 유닛 상태 이상 발동
        } // 유닛 상태 처리 종료
    } // 일괄 처리 종료
    public void Dispose() // 상태 이상 관리자 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 연결 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 연결 해제 상태 저장
        turnRuntime.PhaseStarted -= HandlePhaseStarted; // 진영 턴 시작 이벤트 해제
    } // 연결 해제 종료
} // 클래스 종료
