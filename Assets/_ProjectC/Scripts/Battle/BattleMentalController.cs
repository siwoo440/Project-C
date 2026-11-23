using System; // 연결 해제 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleMentalController : IDisposable // 전투 정신력 흐름 관리자
{ // 클래스 시작
    private const int AllyDeathMentalLoss = -6; // 아군 사망 정신력 감소
    private const int EnemyDeathMentalLoss = -2; // 적 동료 사망 정신력 감소
    private const int LastSurvivorMentalLoss = -3; // 최후 생존 정신력 감소
    private readonly BattleTurnRuntime turnRuntime; // 연결된 턴 관리자
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 아군 목록
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 적 목록
    private readonly List<BattleUnitRuntime> registeredUnits = new List<BattleUnitRuntime>(); // 이벤트 등록 유닛 목록
    private bool allyDeathAppliedThisPhase; // 현재 단계 아군 사망 반영 여부
    private bool enemyDeathAppliedThisPhase; // 현재 단계 적 사망 반영 여부
    private bool disposed; // 연결 해제 여부
    public BattleMentalController(BattleTurnRuntime battleTurn, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies) // 정신력 관리자 생성자
    { // 생성자 시작
        turnRuntime = battleTurn ?? throw new ArgumentNullException(nameof(battleTurn)); // 턴 관리자 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        turnRuntime.PhaseStarted += HandlePhaseStarted; // 턴 시작 이벤트 등록
        turnRuntime.PhaseEnded += HandlePhaseEnded; // 턴 종료 이벤트 등록
        turnRuntime.StateChanged += HandleTurnStateChanged; // 전투 종료 이벤트 등록
        RegisterUnitEvents(allyUnits); // 아군 사망 이벤트 등록
        RegisterUnitEvents(enemyUnits); // 적 사망 이벤트 등록
    } // 생성자 종료
    public bool RegisterSummonedEnemy(BattleUnitRuntime enemyUnit) // 소환 적 정신력 등록
    { // 소환 적 등록 시작
        if (disposed || enemyUnit == null || enemyUnit.Team != BattleTeam.Enemy || registeredUnits.Contains(enemyUnit)) // 등록 조건 확인
        { // 등록 불가 처리 시작
            return false; // 등록 실패 반환
        } // 등록 불가 처리 종료
        enemyUnit.Died += HandleUnitDied; // 소환 적 사망 이벤트 등록
        registeredUnits.Add(enemyUnit); // 등록 유닛 목록 추가
        return true; // 등록 성공 반환
    } // 소환 적 등록 종료
    public bool UnregisterEnemy(BattleUnitRuntime enemyUnit) // 제거 적 정신력 해제
    { // 제거 적 해제 시작
        if (enemyUnit == null || !registeredUnits.Remove(enemyUnit)) // 등록 여부 확인
        { // 해제 불가 처리 시작
            return false; // 해제 실패 반환
        } // 해제 불가 처리 종료
        enemyUnit.Died -= HandleUnitDied; // 적 사망 이벤트 해제
        return true; // 해제 성공 반환
    } // 제거 적 해제 종료
    private void HandlePhaseStarted(BattleTurnPhase phase, int round) // 진영 턴 시작 처리
    { // 턴 시작 처리 시작
        allyDeathAppliedThisPhase = false; // 아군 사망 반영 제한 초기화
        enemyDeathAppliedThisPhase = false; // 적 사망 반영 제한 초기화
        IReadOnlyList<BattleUnitRuntime> activeUnits = GetPhaseUnits(phase); // 현재 진영 목록 조회
        ResetTurnLimits(activeUnits); // 현재 진영 횟수 제한 초기화
        ApplyLastSurvivorMental(activeUnits); // 최후 생존 정신력 감소 적용
    } // 턴 시작 처리 종료
    private void HandlePhaseEnded(BattleTurnPhase phase, int round) // 진영 턴 종료 처리
    { // 턴 종료 처리 시작
        IReadOnlyList<BattleUnitRuntime> activeUnits = GetPhaseUnits(phase); // 종료 진영 목록 조회
        foreach (BattleUnitRuntime runtimeUnit in activeUnits) // 종료 진영 유닛 순회
        { // 특수 상태 진행 시작
            if (runtimeUnit != null && !runtimeUnit.IsDead) // 생존 유닛 확인
            { // 생존 유닛 처리 시작
                runtimeUnit.AdvanceMentalStateTurn(); // 특수 상태 남은 턴 감소
            } // 생존 유닛 처리 종료
        } // 특수 상태 진행 종료
    } // 턴 종료 처리 종료
    private void HandleTurnStateChanged() // 전투 상태 변경 처리
    { // 전투 상태 처리 시작
        if (!turnRuntime.IsBattleEnded) // 전투 종료 여부 확인
        { // 전투 진행 처리 시작
            return; // 종료 해제 중단
        } // 전투 진행 처리 종료
        ResolveBattleEndStates(allyUnits); // 아군 특수 상태 해제
        ResolveBattleEndStates(enemyUnits); // 적 특수 상태 해제
    } // 전투 상태 처리 종료
    private void HandleUnitDied(BattleUnitRuntime deadUnit) // 유닛 사망 정신력 처리
    { // 사망 처리 시작
        if (disposed || deadUnit == null || turnRuntime.IsBattleEnded) // 처리 가능 상태 확인
        { // 처리 불가 종료 시작
            return; // 사망 정신력 처리 중단
        } // 처리 불가 종료 종료
        if (deadUnit.Team == BattleTeam.Ally) // 아군 사망 확인
        { // 아군 사망 처리 시작
            if (allyDeathAppliedThisPhase) // 현재 단계 중복 확인
            { // 중복 처리 시작
                return; // 중복 감소 중단
            } // 중복 처리 종료
            allyDeathAppliedThisPhase = true; // 아군 사망 반영 저장
            ChangeTeamMental(allyUnits, AllyDeathMentalLoss, BattleMentalChangeReason.AllyDied); // 생존 아군 정신력 감소
            return; // 사망 처리 종료
        } // 아군 사망 처리 종료
        if (enemyDeathAppliedThisPhase) // 현재 단계 적 사망 중복 확인
        { // 중복 처리 시작
            return; // 중복 감소 중단
        } // 중복 처리 종료
        enemyDeathAppliedThisPhase = true; // 적 사망 반영 저장
        ChangeTeamMental(enemyUnits, EnemyDeathMentalLoss, BattleMentalChangeReason.TeammateDied); // 생존 적 정신력 감소
    } // 사망 처리 종료
    private static void ChangeTeamMental(IReadOnlyList<BattleUnitRuntime> units, int delta, BattleMentalChangeReason reason) // 진영 정신력 일괄 변경
    { // 일괄 변경 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 진영 유닛 순회
        { // 유닛 정신력 처리 시작
            if (runtimeUnit != null && !runtimeUnit.IsDead) // 생존 유닛 확인
            { // 생존 유닛 처리 시작
                runtimeUnit.ChangeMental(delta, reason); // 정신력 변화 적용
            } // 생존 유닛 처리 종료
        } // 유닛 정신력 처리 종료
    } // 일괄 변경 종료
    private static void ApplyLastSurvivorMental(IReadOnlyList<BattleUnitRuntime> units) // 최후 생존 정신력 적용
    { // 최후 생존 처리 시작
        BattleUnitRuntime lastSurvivor = null; // 최후 생존 후보 초기화
        int livingCount = 0; // 생존 수 초기화
        foreach (BattleUnitRuntime runtimeUnit in units) // 진영 유닛 순회
        { // 생존 유닛 확인 시작
            if (runtimeUnit == null || runtimeUnit.IsDead) // 비생존 유닛 확인
            { // 비생존 처리 시작
                continue; // 다음 유닛 이동
            } // 비생존 처리 종료
            lastSurvivor = runtimeUnit; // 생존 후보 저장
            livingCount++; // 생존 수 증가
        } // 생존 유닛 확인 종료
        if (livingCount == 1) // 단독 생존 확인
        { // 단독 생존 처리 시작
            lastSurvivor.ChangeMental(LastSurvivorMentalLoss, BattleMentalChangeReason.LastSurvivor); // 최후 생존 정신력 감소
        } // 단독 생존 처리 종료
    } // 최후 생존 처리 종료
    private static void ResetTurnLimits(IReadOnlyList<BattleUnitRuntime> units) // 진영 횟수 제한 초기화
    { // 제한 초기화 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 진영 유닛 순회
        { // 유닛 초기화 시작
            runtimeUnit?.ResetMentalTurnLimits(); // 정신력 횟수 제한 초기화
        } // 유닛 초기화 종료
    } // 제한 초기화 종료
    private static void ResolveBattleEndStates(IReadOnlyList<BattleUnitRuntime> units) // 전투 종료 상태 해제
    { // 상태 해제 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 진영 유닛 순회
        { // 유닛 상태 해제 시작
            runtimeUnit?.ResolveMentalStateAtBattleEnd(); // 특수 상태 종료와 정신력 복귀
        } // 유닛 상태 해제 종료
    } // 상태 해제 종료
    private IReadOnlyList<BattleUnitRuntime> GetPhaseUnits(BattleTurnPhase phase) // 단계 진영 목록 조회
    { // 진영 목록 조회 시작
        return phase == BattleTurnPhase.PlayerTurn ? allyUnits : phase == BattleTurnPhase.EnemyTurn ? enemyUnits : Array.Empty<BattleUnitRuntime>(); // 단계별 진영 목록 반환
    } // 진영 목록 조회 종료
    private void RegisterUnitEvents(IReadOnlyList<BattleUnitRuntime> units) // 유닛 사망 이벤트 일괄 등록
    { // 일괄 등록 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 개별 등록 시작
            if (runtimeUnit == null || registeredUnits.Contains(runtimeUnit)) // 유닛 중복 확인
            { // 중복 처리 시작
                continue; // 다음 유닛 이동
            } // 중복 처리 종료
            runtimeUnit.Died += HandleUnitDied; // 사망 이벤트 등록
            registeredUnits.Add(runtimeUnit); // 등록 유닛 목록 추가
        } // 개별 등록 종료
    } // 일괄 등록 종료
    public void Dispose() // 정신력 관리자 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 연결 해제 상태 저장
        turnRuntime.PhaseStarted -= HandlePhaseStarted; // 턴 시작 이벤트 해제
        turnRuntime.PhaseEnded -= HandlePhaseEnded; // 턴 종료 이벤트 해제
        turnRuntime.StateChanged -= HandleTurnStateChanged; // 전투 종료 이벤트 해제
        for (int unitIndex = registeredUnits.Count - 1; unitIndex >= 0; unitIndex--) // 등록 유닛 역순 순회
        { // 이벤트 해제 시작
            BattleUnitRuntime runtimeUnit = registeredUnits[unitIndex]; // 등록 유닛 조회
            runtimeUnit.Died -= HandleUnitDied; // 사망 이벤트 해제
        } // 이벤트 해제 종료
        registeredUnits.Clear(); // 등록 유닛 목록 초기화
    } // 연결 해제 종료
} // 클래스 종료
