using System; // 기본 인터페이스 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleStatusEffectController : IDisposable // 상태 이상 발동 시점 관리
{ // 클래스 시작
    private readonly BattleTurnRuntime turnRuntime; // 연결된 턴 관리자
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 아군 유닛 목록
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 적 유닛 목록
    private readonly BattleStatusEffectProcessor statusEffectProcessor; // 공통 상태 처리기
    private readonly HashSet<BattleUnitRuntime> registeredUnits = new HashSet<BattleUnitRuntime>(); // 상태 적용 감시 유닛 목록
    private readonly HashSet<BattleUnitRuntime> endProcessedUnits = new HashSet<BattleUnitRuntime>(); // 턴 종료 처리를 시작한 유닛 목록
    private bool processingEndUnits; // 진영 종료 상태 처리 진행 여부
    private BattleTurnPhase processedEndPhase = BattleTurnPhase.NotStarted; // 지속 처리를 마친 최근 진영
    private int processedEndRound = -1; // 지속 처리를 마친 최근 라운드
    private bool disposed; // 연결 해제 여부
    public event Action<BattleUnitRuntime, IReadOnlyList<BattleStatusEffectProcessResult>> StatusEffectsProcessed; // 유닛 상태 처리 완료 이벤트
    public BattleStatusEffectController(BattleTurnRuntime battleTurn, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies, BattleStatusEffectProcessor processor) // 상태 이상 관리자 생성
    { // 생성자 시작
        turnRuntime = battleTurn ?? throw new ArgumentNullException(nameof(battleTurn)); // 턴 관리자 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        statusEffectProcessor = processor ?? throw new ArgumentNullException(nameof(processor)); // 공통 상태 처리기 저장
        RegisterUnits(allyUnits); // 아군 상태 적용 감시 등록
        RegisterUnits(enemyUnits); // 적 상태 적용 감시 등록
        turnRuntime.PhaseStarted += HandlePhaseStarted; // 진영 턴 시작 이벤트 등록
        turnRuntime.PhaseEnded += HandlePhaseEnded; // 진영 턴 종료 이벤트 등록
    } // 생성자 종료
    public bool RegisterSummonedEnemy(BattleUnitRuntime unit) // 소환 적 상태 적용 감시 등록
    { // 소환 적 감시 시작
        if (disposed || unit == null || unit.Team != BattleTeam.Enemy || !ContainsUnit(enemyUnits, unit)) // 실제 소환 적 유효성 확인
        { // 감시 불가 처리 시작
            return false; // 등록 실패 반환
        } // 감시 불가 처리 종료
        return RegisterUnit(unit); // 개별 적 감시 등록 결과 반환
    } // 소환 적 감시 종료
    public bool UnregisterEnemy(BattleUnitRuntime unit) // 제거 적 상태 적용 감시 해제
    { // 적 감시 해제 시작
        if (unit == null || !registeredUnits.Remove(unit)) // 등록 상태 확인
        { // 해제 불가 처리 시작
            return false; // 해제 실패 반환
        } // 해제 불가 처리 종료
        unit.StatusEffectResolved -= HandleStatusEffectResolved; // 해당 적 상태 적용 감시 해제
        return true; // 해제 성공 반환
    } // 적 감시 해제 종료
    private void RegisterUnits(IReadOnlyList<BattleUnitRuntime> units) // 초기 진영 상태 감시 등록
    { // 진영 감시 등록 시작
        foreach (BattleUnitRuntime unit in units) // 진영 유닛 순회
        { // 개별 감시 등록 시작
            RegisterUnit(unit); // 유효 유닛 상태 감시 등록
        } // 개별 감시 등록 종료
    } // 진영 감시 등록 종료
    private bool RegisterUnit(BattleUnitRuntime unit) // 개별 유닛 상태 감시 등록
    { // 개별 감시 시작
        if (unit == null || !registeredUnits.Add(unit)) // 유닛 존재와 중복 등록 확인
        { // 등록 불가 처리 시작
            return false; // 등록 실패 반환
        } // 등록 불가 처리 종료
        unit.StatusEffectResolved += HandleStatusEffectResolved; // 상태 적용 이벤트 연결
        return true; // 등록 성공 반환
    } // 개별 감시 종료
    private static bool ContainsUnit(IReadOnlyList<BattleUnitRuntime> units, BattleUnitRuntime target) // 진영 목록의 유닛 존재 확인
    { // 유닛 검색 시작
        foreach (BattleUnitRuntime unit in units) // 진영 목록 순회
        { // 개별 유닛 비교 시작
            if (unit == target) // 지정 유닛 일치 확인
            { // 일치 유닛 처리 시작
                return true; // 유닛 존재 반환
            } // 일치 유닛 처리 종료
        } // 개별 유닛 비교 종료
        return false; // 유닛 없음 반환
    } // 유닛 검색 종료
    private void HandleStatusEffectResolved(BattleUnitRuntime source, BattleUnitRuntime target, BattleStatusEffectType type, BattleStatusEffectApplyResult result) // 자기 턴 상태 적용 감시
    { // 상태 적용 감시 시작
        if (disposed || target == null || result != BattleStatusEffectApplyResult.Applied && result != BattleStatusEffectApplyResult.Stacked) // 실제 상태 적용 확인
        { // 적용 제외 처리 시작
            return; // 적용 감시 종료
        } // 적용 제외 처리 종료
        bool ownPhase = target.Team == BattleTeam.Ally ? turnRuntime.CurrentPhase == BattleTurnPhase.PlayerTurn : turnRuntime.CurrentPhase == BattleTurnPhase.EnemyTurn; // 대상의 자기 턴 여부 확인
        bool alreadyProcessed = processedEndPhase == turnRuntime.CurrentPhase && processedEndRound == turnRuntime.CurrentRound; // 현재 턴의 지속 처리 완료 확인
        bool targetAwaitingEnd = alreadyProcessed && processingEndUnits && !endProcessedUnits.Contains(target); // 종료 처리 대기 중인 대상 확인
        if (type != BattleStatusEffectType.BladeFrenzy && type != BattleStatusEffectType.BloodOath && type != BattleStatusEffectType.FinishingStance && type != BattleStatusEffectType.BladeMaintenance && type != BattleStatusEffectType.AttackPowerDown && ownPhase && (!alreadyProcessed || targetAwaitingEnd)) // 자기 턴 대상의 지속 처리 전 상태 적용 확인
        { // 첫 감소 생략 예약 시작
            target.GetStatusEffect(type)?.SkipNextDurationDecrease(); // 적용한 상태의 첫 지속 감소 생략
        } // 첫 감소 생략 예약 종료
    } // 상태 적용 감시 종료
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
    private void HandlePhaseEnded(BattleTurnPhase phase, int round) // 진영 턴 종료 처리
    { // 턴 종료 처리 시작
        if (disposed || turnRuntime.IsBattleEnded) // 상태 처리 가능 여부 확인
        { // 준비 유지 처리 시작
            return; // 상태 처리 중단
        } // 준비 유지 처리 종료
        processedEndPhase = phase; // 현재 진영 지속 처리 시작 기록
        processedEndRound = round; // 현재 라운드 지속 처리 시작 기록
        endProcessedUnits.Clear(); // 이전 턴의 유닛 처리 기록 정리
        processingEndUnits = true; // 진영 종료 처리 시작 표시
        try // 진영 종료 처리 보호
        { // 진영 종료 처리 시작
            if (phase == BattleTurnPhase.PlayerTurn) // 아군 턴 종료 확인
            { // 아군 종료 상태 처리 시작
                ProcessEndUnits(allyUnits, round); // 아군 종료 상태 처리
            } // 아군 종료 상태 처리 종료
            else if (phase == BattleTurnPhase.EnemyTurn) // 적 턴 종료 확인
            { // 적 종료 상태 처리 시작
                ProcessEndUnits(enemyUnits, round); // 적 종료 상태 처리
            } // 적 종료 상태 처리 종료
        } // 진영 종료 처리 종료
        finally // 처리 종료 상태 복구
        { // 종료 상태 복구 시작
            processingEndUnits = false; // 진영 종료 처리 완료 표시
        } // 종료 상태 복구 종료
        if (phase != BattleTurnPhase.EnemyTurn || turnRuntime.IsBattleEnded) // 적 턴 종료와 전투 지속 확인
        { // 방어 준비 유지 또는 전투 종료 시작
            return; // 방어 준비 정리 중단
        } // 방어 준비 유지 또는 전투 종료 종료
        foreach (BattleUnitRuntime ally in allyUnits) // 아군 유닛 순회
        { // 준비 만료 처리 시작
            if (ally != null) // 아군 존재 확인
            { // 준비 제거 시작
                ally.RemoveStatusEffect(BattleStatusEffectType.GuardPreparation); // 사용하지 않은 방어 준비 만료
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.Evasion); // 이 적 턴에 적용된 회피의 기간 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.ShockAbsorption); // 지정 적 턴의 충격 흡수 기간 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.ResistanceStance); // 지정 적 턴의 저항 태세 기간 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.ShieldCounter); // 한 번의 적 턴 동안 적용되는 반격 종료
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.RetributionBulwark); // 응징 방벽의 한 번의 적 턴 감소
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.GuardianCircle); // 수호의 원의 남은 적 턴 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.FocusedDefense); // 집중 방어의 한 번의 적 턴 감소
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.BleedResistance); // 출혈 부여 저항의 남은 적 턴 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.UndyingGuard); // 사용하지 않은 불굴의 남은 적 턴 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.DistributedDefense); // 광역 방어가 지속될 남은 적 턴 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.EternalShield); // 영원의 방패 직접 피해 경감의 남은 적 턴 차감
                AdvanceEnemyTurnDefense(ally, BattleStatusEffectType.EternalShieldResistance); // 영원의 방패 마법 저항의 남은 적 턴 차감
            } // 준비 제거 종료
        } // 준비 만료 처리 종료
    } // 턴 종료 처리 종료
    private static void AdvanceEnemyTurnDefense(BattleUnitRuntime ally, BattleStatusEffectType type) // 적 공격을 받는 방어 상태의 남은 기간 계산
    { // 방어 지속 시간 처리 시작
        var effect = ally.GetStatusEffect(type); // 해당 방어 상태 조회
        if (effect == null) // 남은 방어 상태가 있는지 확인
        { // 미적용 상태 처리 시작
            return; // 갱신할 상태 없음
        } // 미적용 상태 처리 종료
        effect.AdvanceDuration(); // 적이 모두 행동한 뒤 한 턴 감소
        if (effect.IsExpired) // 마지막 적 턴이 끝났는지 확인
        { // 방어 상태 만료 처리 시작
            ally.RemoveStatusEffect(type); // 기간이 지난 방어 상태 제거
        } // 방어 상태 만료 처리 종료
        else // 남은 방어 기간 표시
        { // 상태 화면 갱신 시작
            ally.NotifyStatusEffectsChanged(); // 아이콘의 남은 턴 표시 갱신
        } // 상태 화면 갱신 종료
    } // 방어 지속 시간 처리 종료
    private void ProcessEndUnits(IReadOnlyList<BattleUnitRuntime> units, int round) // 진영의 턴 종료 상태 처리
    { // 종료 상태 일괄 처리 시작
        foreach (BattleUnitRuntime unit in units) // 진영 유닛 순회
        { // 개별 종료 상태 처리 시작
            if (turnRuntime.IsBattleEnded) // 전투 종료 확인
            { // 종료 처리 중단 시작
                return; // 남은 유닛 처리 중단
            } // 종료 처리 중단 종료
            endProcessedUnits.Add(unit); // 현재 유닛의 종료 처리 시작 기록
            IReadOnlyList<BattleStatusEffectProcessResult> results = statusEffectProcessor.ProcessPhaseEnd(unit, round); // 유닛의 종료 상태 처리
            if (results.Count > 0) // 처리 결과 존재 확인
            { // 처리 결과 알림 시작
                StatusEffectsProcessed?.Invoke(unit, results); // 상태 처리 결과 알림
            } // 처리 결과 알림 종료
        } // 개별 종료 상태 처리 종료
    } // 종료 상태 일괄 처리 종료
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
            IReadOnlyList<BattleStatusEffectProcessResult> processResults = statusEffectProcessor.ProcessPhaseStart(runtimeUnit, round); // 현재 유닛 상태 통합 처리
            if (processResults.Count > 0) // 실제 처리 결과 확인
            { // 처리 완료 알림 시작
                StatusEffectsProcessed?.Invoke(runtimeUnit, processResults); // 상태 처리 결과 전달
            } // 처리 완료 알림 종료
        } // 유닛 상태 처리 종료
    } // 일괄 처리 종료
    public void Dispose() // 상태 이상 관리자 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 연결 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 연결 해제 상태 저장
        foreach (BattleUnitRuntime unit in registeredUnits) // 등록된 상태 감시 유닛 순회
        { // 개별 상태 감시 해제 시작
            unit.StatusEffectResolved -= HandleStatusEffectResolved; // 상태 적용 감시 연결 해제
        } // 개별 상태 감시 해제 종료
        registeredUnits.Clear(); // 등록 유닛 목록 정리
        turnRuntime.PhaseStarted -= HandlePhaseStarted; // 진영 턴 시작 이벤트 해제
        turnRuntime.PhaseEnded -= HandlePhaseEnded; // 진영 턴 종료 이벤트 해제
    } // 연결 해제 종료
} // 클래스 종료
