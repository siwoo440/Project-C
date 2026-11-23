using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleEnemyActionRuntime : IDisposable // 적 행동 흐름 관리
{ // 클래스 시작
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 적 런타임 목록
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 아군 런타임 목록
    private readonly List<BattleEnemyAction> plannedActions = new List<BattleEnemyAction>(); // 예정 행동 목록
    private readonly List<(BattleUnitRuntime Guard, BattleUnitRuntime Ally)> bodyGuards = new List<(BattleUnitRuntime Guard, BattleUnitRuntime Ally)>(); // 사용 순서대로 등록한 보호 목록
    private readonly List<(BattleUnitRuntime Guard, BattleUnitRuntime Ally, int RemainingTurns)> shieldOaths = new List<(BattleUnitRuntime Guard, BattleUnitRuntime Ally, int RemainingTurns)>(); // 보호 대상별 맹세와 남은 적 턴
    private readonly Dictionary<BattleUnitRuntime, int> nextPatternIndices = new Dictionary<BattleUnitRuntime, int>(); // 적별 다음 패턴 순번
    private readonly Dictionary<BattleUnitRuntime, int> phasePatternStarts = new Dictionary<BattleUnitRuntime, int>(); // 적별 현재 페이즈 시작 순번
    private readonly Random random = new Random(); // 무작위 대상 선택기
    private BattleEnemyAction executingAction; // 현재 실행 행동
    private BattleUnitRuntime shieldTaunter; // 이번 적 턴의 전체 도발 아군
    private readonly BattleTurnRuntime turns; // 실제 아군 턴 시작과 장화 횟수 초기화 연결
    private int bootUses; // 이번 턴 파티가 사용한 철제 장화 횟수
    private bool dogTagUsed; // 이번 전투에서 파티가 군번줄을 사용한 여부
    private bool disposed; // 연결 해제 여부
    public IReadOnlyList<BattleEnemyAction> PlannedActions => plannedActions; // 예정 행동 목록 조회
    public IReadOnlyList<BattleUnitRuntime> AllyUnits => allyUnits; // 전투 아군 목록 조회
    public IReadOnlyList<BattleUnitRuntime> EnemyUnits => enemyUnits; // 전투 적 목록 조회
    public event Action StateChanged; // 예정 행동 변경 이벤트
    public BattleEnemyActionRuntime(IReadOnlyList<BattleUnitRuntime> enemies, IReadOnlyList<BattleUnitRuntime> allies, BattleTurnRuntime battleTurns = null) // 적 행동 관리자 생성
    { // 생성자 시작
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        turns = battleTurns; // 현재 전투의 턴 원본 저장
        if (turns != null) // 실제 턴 관리자를 받은 전투 확인
        { // 장화 횟수 연결 시작
            turns.PhaseStarted += ResetBootUses; // 새 아군 턴부터 파티 공용 횟수 복구
            turns.PhaseEnded += HandleEnemyPhaseEnded; // 적 턴 종료에 도발과 맹세 기간 정리
        } // 장화 횟수 연결 종료
        RegisterDeathEvents(enemyUnits, HandleEnemyDied); // 적 사망 이벤트 등록
        RegisterDeathEvents(allyUnits, HandleAllyDied); // 아군 사망 이벤트 등록
        foreach (BattleUnitRuntime ally in allyUnits) // 현재 전투 아군 순회
        { // 부활 이벤트 등록 시작
            if (ally != null) // 연결할 아군 확인
            { // 유효 아군 등록 시작
                ally.Revived += HandleAllyRevived; // 아군 부활 때 광역 예고 갱신 연결
            } // 유효 아군 등록 종료
        } // 부활 이벤트 등록 종료
    } // 생성자 종료
    public bool RegisterSummonedEnemy(BattleUnitRuntime enemyUnit) // 소환 적 행동 연결
    { // 소환 적 연결 시작
        if (disposed || enemyUnit == null || enemyUnit.IsDead || enemyUnit.Team != BattleTeam.Enemy || !ContainsUnit(enemyUnits, enemyUnit)) // 소환 적 연결 조건 확인
        { // 연결 불가 처리 시작
            return false; // 소환 적 연결 실패 반환
        } // 연결 불가 처리 종료
        enemyUnit.Died -= HandleEnemyDied; // 기존 중복 사망 연결 제거
        enemyUnit.Died += HandleEnemyDied; // 소환 적 사망 이벤트 등록
        nextPatternIndices[enemyUnit] = 0; // 소환 적 첫 패턴 순번 저장
        return true; // 소환 적 연결 성공 반환
    } // 소환 적 연결 종료
    public bool UnregisterEnemy(BattleUnitRuntime enemyUnit) // 제거 적 행동 연결 해제
    { // 제거 적 해제 시작
        if (enemyUnit == null) // 제거 적 존재 확인
        { // 적 없음 처리 시작
            return false; // 연결 해제 실패 반환
        } // 적 없음 처리 종료
        enemyUnit.Died -= HandleEnemyDied; // 적 사망 이벤트 해제
        nextPatternIndices.Remove(enemyUnit); // 제거 적 패턴 순번 정리
        phasePatternStarts.Remove(enemyUnit); // 제거 적 페이즈 시작점 정리
        plannedActions.RemoveAll(action => action.Actor == enemyUnit); // 제거 적 예정 행동 정리
        ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
        StateChanged?.Invoke(); // 예정 행동 변경 알림
        return true; // 연결 해제 성공 반환
    } // 제거 적 해제 종료
    public int PrepareActions() // 다음 적 행동 준비
    { // 행동 준비 시작
        if (disposed) // 연결 해제 상태 확인
        { // 준비 불가 처리 시작
            return 0; // 준비 행동 없음 반환
        } // 준비 불가 처리 종료
        plannedActions.Clear(); // 기존 예정 행동 제거
        for (int enemyIndex = 0; enemyIndex < enemyUnits.Count; enemyIndex++) // 적 배치 목록 순회
        { // 적 행동 생성 시작
            BattleUnitRuntime enemyUnit = enemyUnits[enemyIndex]; // 현재 배치 적 조회
            BattleEnemyAction action = CreateAction(enemyUnit, enemyIndex); // 적 예정 행동 생성
            if (action != null) // 생성 행동 확인
            { // 유효 행동 처리 시작
                if (action.IsAreaAttack) // 명시적 광역 공격 대상 연결 여부 확인
                { // 광역 대상 연결 시작
                    action.SetAreaTargets(allyUnits); // 실제 아군 전체를 향한 광역 공격 설정
                } // 광역 대상 연결 종료
                action.SetPainDistributionParty(allyUnits); // 일반과 광역 피해에 실제 출전 명단을 고정 연결
                action.SetDefensePreviewRule(GetDefenseBeforeAttack); // 방어도와 일회성 보호의 예고 순서 연결
                action.SetBootPreviewRule(GetBootPreviewReduction); // 광역 순서를 포함한 남은 장화 횟수 반영
                action.SetBodyGuardPreviewRule(GetBodyGuardPreviewTarget); // 대신 받을 대상 예고 연결
                action.SetShieldTauntTarget(shieldTaunter); // 새 예정 공격에도 현재 도발 반영
                plannedActions.Add(action); // 예정 행동 목록 추가
            } // 유효 행동 처리 종료
        } // 적 행동 생성 종료
        RefreshActionOrder(); // 새 계획을 기존 행동 순서와 같은 비교 규칙으로 정렬
        return plannedActions.Count; // 생성 행동 수 반환
    } // 행동 준비 종료
    internal void RefreshActionOrder() // 이미 선택된 대상과 패턴을 유지하면서 행동 순서만 갱신
    { // 현재 예정 행동 정렬 시작
        if (disposed) // 전투 밖의 오래된 예정 행동 갱신 차단
        { // 해제 상태 처리 시작
            return; // 해제된 화면의 예정 행동 변경 생략
        } // 해제 상태 처리 종료
        plannedActions.Sort(CompareActionOrder); // 현재 속도와 기존 배치 순서 기준 정렬
        ApplyActionOrderNumbers(); // 행동 순서를 실제 실행 목록과 예고에 동시에 반영
        StateChanged?.Invoke(); // 원래 예정 행동 데이터에 대한 화면 갱신 알림
    } // 현재 예정 행동 정렬 종료
    private (int Block, bool Preparation, bool Interception, bool Nullification, bool Illusion) GetDefenseBeforeAttack(BattleEnemyAction action, BattleUnitRuntime target) // 개별 아군의 예정 피격 전 보호 상태 계산
    { // 보호 순서 검사 시작
        int block = target.CurrentBlock; // 현재 대상의 실제 방어도
        bool preparation = true; // 첫 피해 적용 전 방어 준비 확인
        bool interception = true; // 지정 대상의 첫 직접 공격 경감 확인
        bool nullification = true; // 지정 대상의 첫 양수 피해 무효 확인
        bool illusionAvailable = true; // 지정 대상의 첫 적격 명중까지 보존할 환영
        foreach (BattleEnemyAction candidate in plannedActions) // 남은 적 행동 순서대로 예고 확인
        { // 앞선 공격 검사 시작
            if (candidate == action) // 이번 예고의 실제 순서 도달 확인
            { // 예고 중단 시작
                break; // 이전 공격만 반영
            } // 예고 중단 종료
            if (candidate == executingAction || !CanPreviewAttackImpact(candidate, target)) // 현재 실행 중이거나 이 대상을 공격하지 않는 예고 확인
            { // 피해 미포함 처리 시작
                continue; // 다음 공격 확인
            } // 피해 미포함 처리 종료
            var preview = candidate.PreviewDamageBeforeShare(target, block, preparation, interception, nullification, illusionAvailable, out var illusion); // 이전 타격의 방어 결과와 환영 소모 예측
            int areaBlock = candidate.GetStormguardAreaBlock(target, candidate.AttackAmount, block); // 이전 실제 광역 명중의 사전 방어도가 남을 경우 확인
            block = (int)Math.Clamp((long)block + areaBlock + preview.PreparedBlock - preview.AbsorbedDamage, 0L, int.MaxValue); // 올바른 공격 순서의 추가 방어와 흡수까지 포함한 남은 방어도
            preparation &= preview.WasDamageNullified; // 실제 무효화된 공격에서만 방어 준비 보존
            interception = false; // 앞선 양수 직접 공격의 위험 차단 소모 반영
            nullification &= !preview.WasDamageNullified; // 실제 무효화 이후의 보호 소모 반영
            illusionAvailable &= illusion == null; // 처음으로 경감에 기여하는 순서 뒤 환영 제외
        } // 앞선 공격 검사 종료
        return (block, preparation, interception, nullification, illusionAvailable); // 이번 대상에 남은 순서별 방어와 환영 반환
    } // 보호 순서 검사 종료
    private static bool CanPreviewAttackImpact(BattleEnemyAction action, BattleUnitRuntime target) // 지정 대상에게 명중 가능한 직접 공격 확인
    { // 유효 명중 예고 시작
        return action.IsPositiveDirectAttack && action.HitsTarget(target) && BattleHitChance.GetPercent(action.Actor, target) > 0; // 광역 포함 실제 대상과 양수 명중률 확인
    } // 유효 명중 예고 종료
    public BattleEnemyActionResult ExecuteAction(BattleEnemyAction action) // 적 행동 실행
    { // 행동 실행 시작
        if (disposed || executingAction != null || action == null || !plannedActions.Contains(action)) // 중첩 실행 차단과 실행 대상 확인
        { // 실행 불가 처리 시작
            return BattleEnemyActionResult.Empty(action == null ? EnemyActionType.None : action.ActionType, action == null ? BattleDamageType.None : action.DamageType); // 적용 행동 없음 반환
        } // 실행 불가 처리 종료
        if (action.Actor.IsDead) // 행동 적 사망 확인
        { // 사망 적 처리 시작
            plannedActions.Remove(action); // 예정 행동 제거
            ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
            StateChanged?.Invoke(); // 예정 행동 변경 알림
            return BattleEnemyActionResult.Empty(action.ActionType, action.DamageType); // 적용 행동 없음 반환
        } // 사망 적 처리 종료
        if (action.IntendedTarget == null || action.IntendedTarget.IsDead) // 기존 지정 대상 상태 확인
        { // 대상 재선택 시작
            BattleUnitRuntime replacementTarget = SelectTarget(action.TargetRule); // 행동 규칙으로 새 생존 대상 선택
            if (!action.ChangeTarget(replacementTarget)) // 대상 변경 결과 확인
            { // 대상 없음 처리 시작
                plannedActions.Remove(action); // 실행 불가 행동 제거
                ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
                StateChanged?.Invoke(); // 예정 행동 변경 알림
                return BattleEnemyActionResult.Empty(action.ActionType, action.DamageType); // 적용 행동 없음 반환
            } // 대상 없음 처리 종료
        } // 대상 재선택 종료
        bool canIntercept = action.IsPositiveDirectAttack && action.IsSingleTargetEnemyAttack; // 보호로 바꿀 수 있는 양수 단일 공격 확인
        BattleUnitRuntime oathGuard = canIntercept ? FindShieldOathGuard(action) : null; // 우선 대상으로 향한 공격의 맹세 보호자
        int guardIndex = canIntercept && oathGuard == null ? FindBodyGuardIndex(action.PrioritizedTarget) : -1; // 맹세가 없는 공격의 한 차례 보호 검색
        BattleEnemyActionResult actionResult; // 적 행동 적용 결과
        executingAction = action; // 현재 실행 행동 저장
        try // 실행 여부 초기화 보장
        { // 행동 처리 시작
            if (guardIndex < 0) // 한 차례만 대신 받을 보호자 유무 확인
            { // 일반 공격과 지속 보호 처리 시작
                actionResult = action.Execute(TryPreventFirstHealthDamage, ReduceBootHealthDamage, oathGuard, null); // 맹세 기간을 유지하는 실제 적 행동 처리
            } // 일반 공격과 지속 보호 처리 종료
            else // 대신 받을 보호자 확인
            { // 보호 적용 시작
                var bodyGuard = bodyGuards[guardIndex]; // 보호자와 보호 대상 저장
                actionResult = action.Execute(TryPreventFirstHealthDamage, ReduceBootHealthDamage, bodyGuard.Guard, () => bodyGuards.Remove(bodyGuard)); // 양수 공격 명중 때 보호 한 건 소모
            } // 보호 적용 종료
        } // 행동 처리 종료
        finally // 행동 처리 뒤 실행 표시 정리
        { // 실행 표시 정리 시작
            executingAction = null; // 현재 실행 행동 제거
        } // 실행 표시 정리 종료
        plannedActions.Remove(action); // 실행 완료 행동 제거
        ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
        StateChanged?.Invoke(); // 예정 행동 변경 알림
        return actionResult; // 행동 실행 결과 반환
    } // 행동 실행 종료
    public bool CanRedirectAttack(BattleUnitRuntime enemyUnit, BattleUnitRuntime targetUnit) // 예정 공격 유도 가능 확인
    { // 유도 가능 확인 시작
        if (disposed || enemyUnit == null || targetUnit == null || targetUnit.IsDead || targetUnit.Team != BattleTeam.Ally || !ContainsUnit(allyUnits, targetUnit)) // 대상과 관리자 유효성 확인
        { // 유도 불가 처리 시작
            return false; // 유도 실패 반환
        } // 유도 불가 처리 종료
        BattleEnemyAction action = FindAction(enemyUnit); // 해당 적의 예정 행동 조회
        return action != null && action != executingAction && action.IsExecutable && action.IsSingleTargetEnemyAttack && action.PrioritizedTarget != targetUnit; // 현재 단일 공격의 우선 대상 변경 가능 확인
    } // 유도 가능 확인 종료
    public bool TryRedirectAttack(BattleUnitRuntime enemyUnit, BattleUnitRuntime targetUnit) // 예정 공격 대상 변경 시도
    { // 공격 유도 시작
        if (!CanRedirectAttack(enemyUnit, targetUnit)) // 유도 가능 여부 확인
        { // 잘못된 공격 처리 시작
            return false; // 유도 실패 반환
        } // 잘못된 공격 처리 종료
        BattleEnemyAction action = FindAction(enemyUnit); // 유도할 예정 행동 조회
        if (!OverridePlannedAttackTarget(action, targetUnit)) // 선택한 공격만 직접 유도 시도
        { // 변경 실패 처리 시작
            return false; // 유도 실패 반환
        } // 변경 실패 처리 종료
        StateChanged?.Invoke(); // 변경된 공격 예고 알림
        return true; // 유도 성공 반환
    } // 공격 유도 종료
    public bool CanRerollRandomAttack(BattleUnitRuntime enemyUnit) // 무작위 예정 공격 재굴림 가능 확인
    { // 재굴림 가능 확인 시작
        if (disposed || enemyUnit == null || enemyUnit.IsDead || CollectLivingAllies().Count < 2) // 관리자와 복수 생존 대상 확인
        { // 재굴림 불가 처리 시작
            return false; // 재굴림 불가 반환
        } // 재굴림 불가 처리 종료
        BattleEnemyAction action = FindAction(enemyUnit); // 선택 적의 예정 행동 조회
        return action != null && action != executingAction && action.IsExecutable && action.ActionType == EnemyActionType.Attack && action.TargetRule == EnemyTargetRule.RandomLiving; // 무작위 공격 재굴림 가능 여부 반환
    } // 재굴림 가능 확인 종료
    public bool TryRerollRandomAttack(BattleUnitRuntime enemyUnit) // 무작위 예정 공격 대상 다시 선택
    { // 무작위 공격 재굴림 시작
        if (!CanRerollRandomAttack(enemyUnit)) // 재굴림 가능 여부 확인
        { // 재굴림 불가 처리 시작
            return false; // 재굴림 실패 반환
        } // 재굴림 불가 처리 종료
        BattleEnemyAction action = FindAction(enemyUnit); // 선택 적의 예정 공격 조회
        List<BattleUnitRuntime> living = CollectLivingAllies(); // 운명의 카드로 다시 정할 생존 대상 목록
        BattleUnitRuntime target = living[RelicRandomCardTarget.PickIndex(living.Count, random.Next)]; // 카드의 무작위 재지정에 보유 주사위 반영
        if (!OverridePlannedAttackTarget(action, target)) // 재추첨 결과의 개별 적용 확인
        { // 대상 적용 실패 시작
            return false; // 재굴림 실패 반환
        } // 대상 적용 실패 종료
        StateChanged?.Invoke(); // 재굴림된 공격 예고 알림
        return true; // 재굴림 성공 반환
    } // 무작위 공격 재굴림 종료
    public bool CanShareAllyAttack(BattleUnitRuntime protector, BattleUnitRuntime ally) // 동료 공격 피해 분담 가능 확인
    { // 피해 분담 가능 확인 시작
        return CanProtectAlly(protector, ally); // 다른 생존 아군의 예정 공격 존재 여부 반환
    } // 피해 분담 가능 확인 종료
    public bool TryShareAllyAttack(BattleUnitRuntime protector, BattleUnitRuntime ally) // 동료의 첫 예정 공격 피해 분담 설정
    { // 피해 분담 설정 시작
        if (!CanShareAllyAttack(protector, ally)) // 분담 가능 여부 확인
        { // 분담 불가 처리 시작
            return false; // 분담 실패 반환
        } // 분담 불가 처리 종료
        foreach (BattleEnemyAction action in plannedActions) // 예정 행동 순서대로 순회
        { // 첫 동료 공격 검색 시작
            if (action != executingAction && action.IsExecutable && action.IsSingleTargetEnemyAttack && action.PrioritizedTarget == ally && action.SetDamageShare(protector)) // 지정 동료를 향한 첫 단일 공격의 분담자 설정
            { // 분담 설정 완료 시작
                StateChanged?.Invoke(); // 변경된 피해 예고 알림
                return true; // 분담 설정 성공 반환
            } // 분담 설정 완료 종료
        } // 첫 동료 공격 검색 종료
        return false; // 설정할 공격 없음 반환
    } // 피해 분담 설정 종료
    public bool TryShieldTaunt(BattleUnitRuntime owner) // 단일 적 공격 전체의 도발 설정
    { // 전체 도발 시작
        if (disposed || owner == null || owner.Team != BattleTeam.Ally || owner.IsDead || !ContainsUnit(allyUnits, owner)) // 전투 중 생존 아군 확인
        { // 도발 불가 처리 시작
            return false; // 잘못된 도발자 거부
        } // 도발 불가 처리 종료
        SetShieldTaunter(owner); // 이번 적 턴의 도발 대상 교체
        StateChanged?.Invoke(); // 우선 공격 대상 예고 갱신
        return true; // 도발 설정 성공
    } // 전체 도발 종료
    public void ClearShieldTaunt() // 이번 적 턴의 전체 도발 해제
    { // 도발 해제 시작
        if (shieldTaunter == null) // 남아 있는 도발자 확인
        { // 해제 대상 없음 처리 시작
            return; // 도발 해제 생략
        } // 해제 대상 없음 처리 종료
        SetShieldTaunter(null); // 남은 예정 공격의 원래 대상 복원
        StateChanged?.Invoke(); // 복원된 공격 예고 알림
    } // 도발 해제 종료
    private void HandleEnemyPhaseEnded(BattleTurnPhase phase, int round) // 적 턴 전용 도발과 지속 맹세 정리
    { // 적 턴 정리 시작
        if (phase != BattleTurnPhase.EnemyTurn) // 적 진영 행동 완료 여부 확인
        { // 다른 턴 처리 시작
            return; // 아군 턴과 강제 보스 전환의 보호 기간 보존
        } // 다른 턴 처리 종료
        bool changed = shieldTaunter != null; // 이번 적 턴에 남아 있는 도발 확인
        if (changed) // 도발 종료 후 대상 복원 확인
        { // 도발 대상 해제 시작
            SetShieldTaunter(null); // 다음 적 턴 전 원래 우선 공격 대상 복원
        } // 도발 대상 해제 종료
        for (int index = shieldOaths.Count - 1; index >= 0; index--) // 만료한 보호를 지울 수 있도록 맹세 역순 조회
        { // 개별 맹세 기간 처리 시작
            var oath = shieldOaths[index]; // 현재 맹세와 남은 적 턴 조회
            oath.RemainingTurns--; // 완료된 적 행동 한 라운드 반영
            if (oath.RemainingTurns == 0) // 마지막 보호 턴 완료 확인
            { // 만료한 맹세 정리 시작
                shieldOaths.RemoveAt(index); // 끝난 대신 받기 제거
                changed = true; // 변경된 실제 피격 대상 알림 예약
            } // 만료한 맹세 정리 종료
            else // 다음 적 턴까지 남은 맹세 확인
            { // 남은 기간 반영 시작
                shieldOaths[index] = oath; // 한 턴 감소한 보호 기간 저장
            } // 남은 기간 반영 종료
        } // 개별 맹세 기간 처리 종료
        if (changed) // 우선 또는 최종 피격 대상 변경 확인
        { // 갱신 알림 시작
            StateChanged?.Invoke(); // 두 종류의 보호 상태 정리 후 예고 갱신
        } // 갱신 알림 종료
    } // 적 턴 정리 종료
    private void SetShieldTaunter(BattleUnitRuntime owner) // 현재와 이후 공격의 도발 대상 저장
    { // 현재 도발자 갱신 시작
        shieldTaunter = owner; // 다음 예정 공격에도 쓸 도발 대상 저장
        foreach (BattleEnemyAction action in plannedActions) // 현재 적의 예정 행동 순회
        { // 남은 행동 확인 시작
            if (action != executingAction) // 실행 중인 행동 제외
            { // 도발 예고 적용 시작
                action.SetShieldTauntTarget(owner); // 아직 실행하지 않은 공격만 갱신
            } // 도발 예고 적용 종료
        } // 남은 행동 확인 종료
    } // 현재 도발자 갱신 종료
    private static bool OverridePlannedAttackTarget(BattleEnemyAction action, BattleUnitRuntime target) // 명시적으로 선택한 개별 공격의 대상 변경
    { // 개별 대상 변경 시작
        if (!action.ChangeTarget(target)) // 새 개별 공격 대상 적용 확인
        { // 변경 불가 처리 시작
            return false; // 대상 변경 실패 반환
        } // 변경 불가 처리 종료
        action.SetShieldTauntTarget(null); // 개별 대상 변경이 적용된 공격의 도발 우선순위 해제
        return true; // 대상 변경 성공 반환
    } // 개별 대상 변경 종료
    public bool CanBodyGuard(BattleUnitRuntime guard, BattleUnitRuntime ally) // 다음 피해 대신 받기 가능 확인
    { // 대신 받기 조건 확인 시작
        return !disposed && guard != null && ally != null && guard != ally && !guard.IsDead && !ally.IsDead && guard.Team == BattleTeam.Ally && ally.Team == BattleTeam.Ally && ContainsUnit(allyUnits, guard) && ContainsUnit(allyUnits, ally); // 서로 다른 생존 전투 아군 확인
    } // 대신 받기 조건 확인 종료
    public bool TryBodyGuard(BattleUnitRuntime guard, BattleUnitRuntime ally) // 다음 직접 공격 대신 받기 설정
    { // 대신 받기 설정 시작
        if (!CanBodyGuard(guard, ally)) // 두 아군의 보호 가능 상태 확인
        { // 보호 불가 처리 시작
            return false; // 잘못된 보호 대상 거부
        } // 보호 불가 처리 종료
        bodyGuards.Add((guard, ally)); // 다음 양수 직접 공격을 위한 보호 한 건 등록
        StateChanged?.Invoke(); // 피격 대상 예고 갱신
        return true; // 대신 받기 설정 성공
    } // 대신 받기 설정 종료
    public bool CanShieldOath(BattleUnitRuntime guard, BattleUnitRuntime target) // 적 턴 동안 동료를 보호할 수 있는지 확인
    { // 맹세 조건 확인 시작
        return turns != null && !turns.IsBattleEnded && CanBodyGuard(guard, target); // 적 턴 경계와 서로 다른 생존 아군 확인
    } // 맹세 조건 확인 종료
    public bool TryShieldOath(BattleUnitRuntime guard, BattleUnitRuntime target, int enemyTurns) // 지정한 적 턴 동안 직접 공격 대신 받기
    { // 수호 맹세 등록 시작
        if (enemyTurns <= 0 || !CanShieldOath(guard, target)) // 양수 기간과 보호 가능한 아군 검증
        { // 등록 불가 처리 시작
            return false; // 유효하지 않은 맹세 거부
        } // 등록 불가 처리 종료
        shieldOaths.RemoveAll(oath => oath.Ally == target); // 같은 보호 대상의 이전 맹세 제거
        shieldOaths.Add((guard, target, enemyTurns)); // 새 보호자와 적 턴 지속 기간 등록
        StateChanged?.Invoke(); // 맹세가 반영된 피격 대상 예고 갱신
        return true; // 수호 맹세 적용 성공
    } // 수호 맹세 등록 종료
    private BattleUnitRuntime FindShieldOathGuard(BattleEnemyAction action) // 우선 대상의 지속 맹세 보호자 조회
    { // 맹세 보호자 검색 시작
        if (shieldOaths.Count == 0 || !action.IsSingleTargetEnemyAttack) // 등록된 맹세와 단일 대상 공격 확인
        { // 보호 대상 제외 시작
            return null; // 맹세로 대신 받을 수 없는 공격
        } // 보호 대상 제외 종료
        BattleUnitRuntime target = action.PrioritizedTarget; // 도발과 대상 변경을 반영한 보호 대상
        var oath = shieldOaths.Find(entry => entry.Ally == target); // 우선 대상에게 등록된 한 건의 맹세
        return CanShieldOath(oath.Guard, target) ? oath.Guard : null; // 현재 살아 있는 보호자만 한 번 적용
    } // 맹세 보호자 검색 종료
    private int FindBodyGuardIndex(BattleUnitRuntime ally, int startIndex = 0) // 지정 아군의 다음 보호 위치 조회
    { // 보호 검색 시작
        for (int index = startIndex; index < bodyGuards.Count; index++) // 등록 순서대로 보호 목록 순회
        { // 개별 보호 확인 시작
            var bodyGuard = bodyGuards[index]; // 보호자와 지정 아군 조회
            if (bodyGuard.Ally == ally && CanBodyGuard(bodyGuard.Guard, ally)) // 지정 대상과 보호 유효성 확인
            { // 유효 보호 확인 시작
                return index; // 가장 앞선 보호 위치 반환
            } // 유효 보호 확인 종료
        } // 개별 보호 확인 종료
        return -1; // 유효한 보호 없음 반환
    } // 보호 검색 종료
    private BattleUnitRuntime GetBodyGuardPreviewTarget(BattleEnemyAction action) // 순서에 맞는 보호자 예고 조회
    { // 보호자 예고 시작
        if (!action.IsPositiveDirectAttack || !action.IsSingleTargetEnemyAttack) // 보호할 수 있는 양수 단일 공격 확인
        { // 보호 예고 제외 시작
            return null; // 기존 대상 예고 유지
        } // 보호 예고 제외 종료
        BattleUnitRuntime oathGuard = FindShieldOathGuard(action); // 실제 공격과 같은 지속 보호자 조회
        if (oathGuard != null || bodyGuards.Count == 0) // 먼저 적용할 맹세나 한 차례 보호 없음 확인
        { // 지속 보호 예고 처리 시작
            return oathGuard; // 이번 공격의 맹세 보호자 또는 원래 대상 사용
        } // 지속 보호 예고 처리 종료
        BattleUnitRuntime target = action.PrioritizedTarget; // 도발이 반영된 공격 대상 조회
        int guardIndex = FindBodyGuardIndex(target); // 우선 대상의 첫 보호 조회
        foreach (BattleEnemyAction candidate in plannedActions) // 공격 예고 순서대로 순회
        { // 앞선 보호 소모 확인 시작
            if (guardIndex < 0 || candidate == action) // 보호 소진 또는 현재 공격 도달 확인
            { // 보호자 예고 반환 시작
                return guardIndex < 0 ? null : bodyGuards[guardIndex].Guard; // 남은 첫 보호자 반환
            } // 보호자 예고 반환 종료
            if (candidate != executingAction && candidate.PrioritizedTarget == target && candidate.IsPositiveDirectAttack && candidate.IsSingleTargetEnemyAttack && BattleHitChance.GetPercent(candidate.Actor, bodyGuards[guardIndex].Guard) > 0) // 같은 우선 대상의 앞선 단일 보호 소모 예측
            { // 예고용 보호 이동 시작
                guardIndex = FindBodyGuardIndex(target, guardIndex + 1); // 다음 보호 위치 조회
            } // 예고용 보호 이동 종료
        } // 앞선 보호 소모 확인 종료
        return null; // 예정 목록 밖의 보호자 예고 제외
    } // 보호자 예고 종료
    public bool CanProtectAlly(BattleUnitRuntime guard, BattleUnitRuntime ally) // 동료 대신 공격받기 가능 여부 확인
    { // 동료 보호 가능 확인 시작
        if (!CanBodyGuard(guard, ally)) // 보호자와 동료의 전투 상태 확인
        { // 보호 불가 처리 시작
            return false; // 보호 불가 반환
        } // 보호 불가 처리 종료
        foreach (BattleEnemyAction action in plannedActions) // 예정 행동 순서대로 순회
        { // 공격 가능 확인 시작
            if (action != executingAction && action.IsExecutable && action.IsSingleTargetEnemyAttack && action.PrioritizedTarget == ally) // 동료를 향한 유효 단일 공격 확인
            { // 보호 가능 처리 시작
                return true; // 대신 받을 공격 존재 반환
            } // 보호 가능 처리 종료
        } // 공격 가능 확인 종료
        return false; // 예정 공격 없음 반환
    } // 동료 보호 가능 확인 종료
    public bool TryProtectAlly(BattleUnitRuntime guard, BattleUnitRuntime ally) // 동료의 첫 예정 공격을 대신 받기
    { // 동료 보호 시작
        if (!CanProtectAlly(guard, ally)) // 보호 가능 상태 확인
        { // 보호 불가 처리 시작
            return false; // 보호 실패 반환
        } // 보호 불가 처리 종료
        foreach (BattleEnemyAction action in plannedActions) // 예정 행동 순서대로 순회
        { // 첫 공격 검색 시작
            if (action != executingAction && action.IsExecutable && action.IsSingleTargetEnemyAttack && action.PrioritizedTarget == ally && OverridePlannedAttackTarget(action, guard)) // 동료의 첫 단일 공격만 보호자로 전환
            { // 공격 전환 처리 시작
                StateChanged?.Invoke(); // 변경된 공격 예고 알림
                return true; // 보호 성공 반환
            } // 공격 전환 처리 종료
        } // 첫 공격 검색 종료
        return false; // 전환할 공격 없음 반환
    } // 동료 보호 종료
    public BattleEnemyAction FindAction(BattleUnitRuntime enemyUnit) // 적 예정 행동 조회
    { // 행동 조회 시작
        foreach (BattleEnemyAction action in plannedActions) // 예정 행동 순회
        { // 행동 비교 시작
            if (action.Actor == enemyUnit) // 행동 적 일치 확인
            { // 일치 행동 처리 시작
                return action; // 예정 행동 반환
            } // 일치 행동 처리 종료
        } // 행동 비교 종료
        return null; // 예정 행동 없음 반환
    } // 행동 조회 종료
    public void ClearActions() // 예정 행동 전체 제거
    { // 행동 제거 시작
        if (plannedActions.Count < 1) // 예정 행동 존재 확인
        { // 빈 목록 처리 시작
            return; // 행동 제거 중단
        } // 빈 목록 처리 종료
        plannedActions.Clear(); // 예정 행동 목록 비우기
        StateChanged?.Invoke(); // 예정 행동 변경 알림
    } // 행동 제거 종료
    public void SetBossPhaseStart(BattleUnitRuntime enemyUnit, int patternIndex) // 보스의 다음 패턴 시작점 지정
    { // 보스 패턴 시작점 설정 시작
        if (!disposed && enemyUnit != null && ContainsUnit(enemyUnits, enemyUnit)) // 등록된 적 확인
        { // 등록 적 처리 시작
            nextPatternIndices[enemyUnit] = Math.Max(0, patternIndex); // 다음 페이즈의 첫 패턴 순번 저장
            phasePatternStarts[enemyUnit] = Math.Max(0, patternIndex); // 현재 페이즈의 패턴 시작점 저장
        } // 등록 적 처리 종료
    } // 보스 패턴 시작점 설정 종료
    private BattleEnemyAction CreateAction(BattleUnitRuntime enemyUnit, int creationOrder) // 적 예정 행동 생성
    { // 행동 생성 시작
        if (enemyUnit == null || enemyUnit.IsDead || enemyUnit.EnemySource == null) // 적 데이터 유효성 확인
        { // 잘못된 적 처리 시작
            return null; // 행동 생성 실패 반환
        } // 잘못된 적 처리 종료
        EnemyData enemyData = enemyUnit.EnemySource; // 적 원본 데이터 조회
        EnemyActionPatternData patternData = SelectNextActionPattern(enemyUnit, enemyData, out int patternIndex, out int patternCount); // 다음 유효 패턴 행동 선택
        if (patternData != null) // 패턴 행동 존재 확인
        { // 패턴 행동 생성 시작
            BattleUnitRuntime patternTarget = SelectTarget(patternData.TargetRule); // 패턴 대상 규칙으로 대상 선택
            if (patternTarget == null) // 패턴 대상 존재 확인
            { // 패턴 대상 없음 처리 시작
                return null; // 행동 생성 실패 반환
            } // 패턴 대상 없음 처리 종료
            int patternSpeed = RollActionSpeed(patternData.MinimumActionSpeed, patternData.MaximumActionSpeed); // 패턴 행동 속도 결정
            return new BattleEnemyAction(enemyUnit, patternTarget, patternData.ActionType, patternData.DamageType, patternData.ActionAmount, patternData.StatusEffectType, patternData.StatusEffectDuration, patternData.StatusEffectMaximumStacks, patternData.TargetRule, patternData.DisplayName, patternIndex, patternCount, patternSpeed, creationOrder, patternData.StatusEffectChancePercent, patternData.StatusEffectValue, null, patternData.MentalChangeOnHit, turns?.BattleType); // 순차 패턴의 상태와 정신력 및 실제 조우 등급 전달
        } // 패턴 행동 생성 종료
        if (!IsActionConfigurationValid(enemyData)) // 지원 행동 확인
        { // 미지원 행동 처리 시작
            return null; // 행동 생성 실패 반환
        } // 미지원 행동 처리 종료
        BattleUnitRuntime targetUnit = SelectTarget(enemyData.TargetRule); // 행동 대상 선택
        if (targetUnit == null) // 대상 존재 확인
        { // 대상 없음 처리 시작
            return null; // 행동 생성 실패 반환
        } // 대상 없음 처리 종료
        int actionSpeed = RollActionSpeed(enemyData.MinimumActionSpeed, enemyData.MaximumActionSpeed); // 이번 턴 행동 속도 결정
        int actionAmount = enemyData.ActionType == EnemyActionType.Attack ? enemyData.BasicAttackPower : enemyData.StatusEffectValue; // 행동 종류별 수치 결정
        string actionDisplayName = GetLegacyActionDisplayName(enemyData); // 호환 행동 이름 계산
        return new BattleEnemyAction(enemyUnit, targetUnit, enemyData.ActionType, enemyData.DamageType, actionAmount, enemyData.StatusEffectType, enemyData.StatusEffectDuration, enemyData.StatusEffectMaximumStacks, enemyData.TargetRule, actionDisplayName, 1, 1, actionSpeed, creationOrder, battleType: turns?.BattleType); // 일반 예정 행동과 실제 조우 등급 전달
    } // 행동 생성 종료
    private EnemyActionPatternData SelectNextActionPattern(BattleUnitRuntime enemyUnit, EnemyData enemyData, out int patternIndex, out int patternCount) // 다음 유효 패턴 선택
    { // 패턴 선택 시작
        patternIndex = 1; // 기본 화면 순번 초기화
        patternCount = enemyData.ActionPatternCount; // 전체 패턴 수 조회
        if (patternCount < 1) // 패턴 존재 확인
        { // 패턴 없음 처리 시작
            return null; // 호환 행동 사용 반환
        } // 패턴 없음 처리 종료
        if (!nextPatternIndices.TryGetValue(enemyUnit, out int nextPatternIndex)) // 저장 순번 존재 확인
        { // 첫 패턴 처리 시작
            nextPatternIndex = 0; // 첫 패턴 순번 적용
        } // 첫 패턴 처리 종료
        int phaseStart = phasePatternStarts.TryGetValue(enemyUnit, out int configuredStart) ? Math.Min(configuredStart, patternCount - 1) : 0; // 현재 페이즈의 시작 순번 조회
        int secondPhaseStart = enemyData.SecondPhasePatternStartIndex; // 원본의 두 번째 페이즈 시작점 조회
        int phaseEnd = phaseStart == 0 && secondPhaseStart > 0 && secondPhaseStart < patternCount ? secondPhaseStart : patternCount; // 현재 페이즈 패턴 종료점 계산
        int phaseCount = phaseEnd - phaseStart; // 현재 페이즈 패턴 개수 계산
        nextPatternIndex = phaseStart + Math.Max(0, nextPatternIndex - phaseStart) % phaseCount; // 현재 페이즈 안의 순번 보정
        for (int patternOffset = 0; patternOffset < phaseCount; patternOffset++) // 현재 페이즈 패턴 후보 순회
        { // 유효 패턴 검색 시작
            int candidateIndex = phaseStart + (nextPatternIndex - phaseStart + patternOffset) % phaseCount; // 현재 페이즈 순환 후보 계산
            EnemyActionPatternData candidatePattern = enemyData.GetActionPattern(candidateIndex); // 후보 패턴 조회
            if (candidatePattern == null || !candidatePattern.IsValid) // 후보 설정 유효성 확인
            { // 잘못된 후보 처리 시작
                continue; // 다음 패턴 후보 이동
            } // 잘못된 후보 처리 종료
            patternIndex = candidateIndex + 1; // 화면용 현재 패턴 순번 저장
            nextPatternIndices[enemyUnit] = phaseStart + (candidateIndex - phaseStart + 1) % phaseCount; // 같은 페이즈의 다음 패턴 저장
            return candidatePattern; // 유효 패턴 반환
        } // 유효 패턴 검색 종료
        patternCount = 1; // 호환 행동 전체 순번 적용
        return null; // 유효 패턴 없음 반환
    } // 패턴 선택 종료
    private void ResetBootUses(BattleTurnPhase phase, int round) // 새 아군 턴의 파티 공용 횟수 복원
    { // 장화 초기화 시작
        if (phase == BattleTurnPhase.PlayerTurn) // 적 선제 뒤의 첫 아군 턴 포함
        { // 횟수 복원 시작
            bootUses = 0; // 다음 적 직접 공격 세 번까지 경감 가능
        } // 횟수 복원 종료
    } // 장화 초기화 종료
    private int ReduceBootHealthDamage(int incoming) // 적의 실제 체력 피해 경감
    { // 경감 정산 시작
        var boots = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.ReduceIncomingAttack); // 현재 파티가 보유한 철제 장화 확인
        if (boots == null || bootUses >= boots.MaximumTriggersPerTurn || incoming < 1) // 실제 소유와 잔여 횟수 및 체력 손실 확인
        { // 경감 제외 시작
            return incoming; // 방어도 흡수나 한도 소진 시 추가 경감 없음
        } // 경감 제외 종료
        bootUses++; // 주 대상의 실제 체력 피해 한 건에만 기회 소모
        return Math.Max(0, incoming - boots.EffectValue); // 영 미만으로 내려가지 않는 피해 반환
    } // 경감 정산 종료
    private int GetBootPreviewReduction(BattleEnemyAction current, BattleUnitRuntime currentTarget) // 피격 순서별 파티 피해 보호 횟수 계산
    { // 장화와 군번줄 예고 시작
        var inventory = RelicRunManager.Instance?.Inventory; // 현재 회차의 실제 유물 조회
        var boots = inventory?.FindEffect(RelicEffectType.ReduceIncomingAttack); // 현재 장화 보호 원본 조회
        bool dogTag = !dogTagUsed && inventory?.FindEffect(RelicEffectType.PreventFirstHealthDamage) != null; // 군번줄의 남은 일회 보호 확인
        int available = (boots?.MaximumTriggersPerTurn ?? 0) - bootUses; // 파티의 실제 장화 잔여 횟수
        if (!dogTag && available <= 0) // 아직 남은 파티 피해 보호 확인
        { // 피해 보호 없음 시작
            return 0; // 유물 경감 없는 예상 피해 반환
        } // 피해 보호 없음 종료
        foreach (var action in plannedActions) // 실제 적 행동의 계획 순서 순회
        { // 개별 행동의 피격 순서 시작
            if (action == executingAction) // 이미 현재 피해를 적용 중인 예고 확인
            { // 실행 중인 행동 제외 시작
                continue; // 이전 실제 피해는 현재 상태에서 반영
            } // 실행 중인 행동 제외 종료
            IReadOnlyList<BattleUnitRuntime> targets = action.IsAreaAttack ? action.AreaTargets : new[] { action.Target }; // 광역 배치 또는 기존 단일 피격 순서
            foreach (var target in targets) // 같은 행동의 개별 피격 대상 순회
            { // 개별 대상의 공용 보호 조회 시작
                if (!CanPreviewAttackImpact(action, target)) // 대상별 양수 명중 가능성 확인
                { // 피해 없는 대상 제외 시작
                    continue; // 다음 피격 대상 확인
                } // 피해 없는 대상 제외 종료
                var preview = action.PreviewWithDefense(target); // 해당 대상의 실제 방어도와 무효 상태 반영
                if (action.GetPrimaryHealthDamage(preview) <= 0) // 주 대상의 실제 체력 피해 여부 확인
                { // 유물 기회 보존 시작
                    continue; // 남은 유물 보호 기회 유지
                } // 유물 기회 보존 종료
                if (action == current && target == currentTarget) // 이번 예고의 정확한 행동과 대상 확인
                { // 현재 대상 결과 시작
                    return dogTag ? preview.FinalDamage : available > 0 ? boots.EffectValue : 0; // 군번줄 우선 또는 남은 장화 감소량
                } // 현재 대상 결과 종료
                if (dogTag) // 앞선 대상의 군번줄 보호 확인
                { // 군번줄 소모 예고 시작
                    dogTag = false; // 다음 피해에 적용할 군번줄 기회 소모
                } // 군번줄 소모 예고 종료
                else // 앞선 대상의 장화 보호 차감
                { // 장화 소모 예고 시작
                    available--; // 이미 예정된 실제 피해의 장화 기회 소모
                } // 장화 소모 예고 종료
            } // 개별 대상의 공용 보호 조회 종료
        } // 개별 행동의 피격 순서 종료
        return 0; // 예고 목록의 현재 유효 유물 보호 없음
    } // 장화와 군번줄 예고 종료
    private bool TryPreventFirstHealthDamage() // 전투 중 처음 발생한 적 직접 체력 피해 차단
    { // 군번줄 소비 검사 시작
        if (dogTagUsed || RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.PreventFirstHealthDamage) == null) // 이번 전투의 잔여 횟수와 실제 보유 확인
        { // 방어 불가 처리 시작
            return false; // 정상 체력 피해 진행
        } // 방어 불가 처리 종료
        dogTagUsed = true; // 파티 전체의 이번 전투 한 번 소진
        return true; // 이번 대상의 체력 손실 무효
    } // 군번줄 소비 검사 종료
    private static bool IsActionConfigurationValid(EnemyData enemyData) // 적 행동 설정 유효성 확인
    { // 설정 확인 시작
        if (enemyData.ActionType == EnemyActionType.Attack) // 공격 행동 확인
        { // 공격 설정 처리 시작
            return enemyData.BasicAttackPower > 0; // 공격 수치 유효성 반환
        } // 공격 설정 처리 종료
        return enemyData.ActionType == EnemyActionType.ApplyStatusEffect && enemyData.StatusEffectType != BattleStatusEffectType.None && enemyData.StatusEffectValue > 0 && enemyData.StatusEffectDuration > 0 && enemyData.StatusEffectMaximumStacks > 0; // 상태 행동 설정 유효성 반환
    } // 설정 확인 종료
    private static string GetLegacyActionDisplayName(EnemyData enemyData) // 호환 행동 이름 조회
    { // 호환 이름 조회 시작
        if (enemyData.ActionType == EnemyActionType.ApplyStatusEffect) // 상태 행동 확인
        { // 상태 이름 처리 시작
            return BattleStatusEffectInstance.GetDisplayName(enemyData.StatusEffectType); // 상태 이상 이름 반환
        } // 상태 이름 처리 종료
        return enemyData.ActionType == EnemyActionType.Attack ? "기본 공격" : "행동 없음"; // 공격 또는 빈 행동 이름 반환
    } // 호환 이름 조회 종료
    private int RollActionSpeed(int minimumSpeed, int maximumSpeed) // 적 행동의 원래 속도 한 번 결정
    { // 속도 결정 시작
        int safeMinimumSpeed = Math.Max(1, minimumSpeed); // 최소 행동 속도 보정
        int safeMaximumSpeed = Math.Max(safeMinimumSpeed, maximumSpeed); // 최대 행동 속도 보정
        return random.Next(safeMinimumSpeed, safeMaximumSpeed + 1); // 원래 계획 속도의 한 번만 굴리는 난수
    } // 속도 결정 종료
    private static int CompareActionOrder(BattleEnemyAction leftAction, BattleEnemyAction rightAction) // 적 행동 순서 비교
    { // 행동 비교 시작
        int speedComparison = rightAction.ActionSpeed.CompareTo(leftAction.ActionSpeed); // 높은 속도 우선 비교
        if (speedComparison != 0) // 속도 차이 확인
        { // 속도 우선 처리 시작
            return speedComparison; // 속도 비교 결과 반환
        } // 속도 우선 처리 종료
        return leftAction.CreationOrder.CompareTo(rightAction.CreationOrder); // 동속도 배치 순서 비교 결과 반환
    } // 행동 비교 종료
    private void ApplyActionOrderNumbers() // 최종 행동 순번 적용
    { // 순번 적용 시작
        for (int actionIndex = 0; actionIndex < plannedActions.Count; actionIndex++) // 정렬 행동 목록 순회
        { // 개별 순번 적용 시작
            plannedActions[actionIndex].SetActionOrder(actionIndex + 1); // 일부터 시작하는 순번 지정
        } // 개별 순번 적용 종료
    } // 순번 적용 종료
    private BattleUnitRuntime SelectTarget(EnemyTargetRule targetRule) // 규칙별 아군 대상 선택
    { // 대상 선택 시작
        List<BattleUnitRuntime> livingAllies = CollectLivingAllies(); // 생존 아군 목록 생성
        if (livingAllies.Count < 1) // 생존 아군 존재 확인
        { // 대상 없음 처리 시작
            return null; // 대상 없음 반환
        } // 대상 없음 처리 종료
        if (targetRule == EnemyTargetRule.LowestHealth) // 최저 체력 규칙 확인
        { // 최저 체력 선택 시작
            BattleUnitRuntime selectedUnit = livingAllies[0]; // 첫 생존 아군 초기 선택
            foreach (BattleUnitRuntime livingAlly in livingAllies) // 생존 아군 순회
            { // 체력 비교 시작
                if (livingAlly.CurrentHealth < selectedUnit.CurrentHealth) // 더 낮은 체력 확인
                { // 대상 교체 시작
                    selectedUnit = livingAlly; // 최저 체력 아군 저장
                } // 대상 교체 종료
            } // 체력 비교 종료
            return selectedUnit; // 최저 체력 아군 반환
        } // 최저 체력 선택 종료
        if (targetRule == EnemyTargetRule.RandomLiving) // 무작위 규칙 확인
        { // 무작위 선택 시작
            return livingAllies[random.Next(livingAllies.Count)]; // 무작위 생존 아군 반환
        } // 무작위 선택 종료
        return livingAllies[0]; // 첫 생존 아군 반환
    } // 대상 선택 종료
    private List<BattleUnitRuntime> CollectLivingAllies() // 생존 아군 목록 생성
    { // 생존 목록 생성 시작
        List<BattleUnitRuntime> livingAllies = new List<BattleUnitRuntime>(); // 생존 아군 목록 준비
        foreach (BattleUnitRuntime allyUnit in allyUnits) // 아군 목록 순회
        { // 아군 생존 확인 시작
            if (allyUnit != null && !allyUnit.IsDead) // 생존 아군 확인
            { // 생존 아군 처리 시작
                livingAllies.Add(allyUnit); // 생존 아군 목록 추가
            } // 생존 아군 처리 종료
        } // 아군 생존 확인 종료
        return livingAllies; // 생존 아군 목록 반환
    } // 생존 목록 생성 종료
    private void HandleEnemyDied(BattleUnitRuntime enemyUnit) // 적 사망 처리
    { // 적 사망 처리 시작
        int removedCount = plannedActions.RemoveAll(action => action.Actor == enemyUnit); // 사망 적 행동 제거
        nextPatternIndices.Remove(enemyUnit); // 사망 적 패턴 순번 정리
        if (removedCount > 0) // 제거 행동 확인
        { // 변경 알림 시작
            ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
            StateChanged?.Invoke(); // 예정 행동 변경 알림
        } // 변경 알림 종료
    } // 적 사망 처리 종료
    private void HandleAllyDied(BattleUnitRuntime allyUnit) // 아군 사망 처리
    { // 아군 사망 처리 시작
        bool changed = bodyGuards.RemoveAll(bodyGuard => bodyGuard.Guard == allyUnit || bodyGuard.Ally == allyUnit) > 0; // 사망 아군이 참여한 보호 제거
        changed |= shieldOaths.RemoveAll(oath => oath.Guard == allyUnit || oath.Ally == allyUnit) > 0; // 사망 아군이 참여한 지속 맹세 제거
        if (shieldTaunter == allyUnit) // 현재 도발자 사망 확인
        { // 사망 도발자 해제 시작
            SetShieldTaunter(null); // 남은 적 공격의 기존 대상 복원
            changed = true; // 도발 예고 갱신 예약
        } // 사망 도발자 해제 종료
        foreach (BattleEnemyAction action in plannedActions) // 예정 행동 순회
        { // 대상 확인 시작
            if (action == executingAction) // 현재 실행 행동 확인
            { // 실행 행동 처리 시작
                continue; // 대상 재선택 제외
            } // 실행 행동 처리 종료
            changed |= action.ClearDamageShare(allyUnit); // 사망한 분담자의 연결 해제
            changed |= action.IsAreaAttack; // 광역 예고의 생존 대상 목록 갱신 예약
            if (action.IntendedTarget != allyUnit) // 사망한 지정 대상 일치 확인
            { // 다른 대상 처리 시작
                continue; // 다음 행동 이동
            } // 다른 대상 처리 종료
            BattleUnitRuntime replacementTarget = SelectTarget(action.TargetRule); // 행동 규칙으로 대체 대상 선택
            changed |= action.ChangeTarget(replacementTarget); // 대상 변경 결과 저장
        } // 대상 확인 종료
        if (changed) // 대상 변경 확인
        { // 변경 알림 시작
            StateChanged?.Invoke(); // 예정 행동 변경 알림
        } // 변경 알림 종료
    } // 아군 사망 처리 종료
    private void HandleAllyRevived(BattleUnitRuntime ally) // 부활 이후 예정 공격 표시 갱신
    { // 아군 부활 예고 처리 시작
        if (plannedActions.Exists(action => action.IsAreaAttack && action.HitsTarget(ally))) // 부활 아군을 다시 포함하는 광역 공격 확인
        { // 광역 예정 대상 갱신 시작
            StateChanged?.Invoke(); // 공격 순서와 단일 지정 대상을 보존한 예고 갱신
        } // 광역 예정 대상 갱신 종료
    } // 아군 부활 예고 처리 종료
    private static void RegisterDeathEvents(IReadOnlyList<BattleUnitRuntime> units, Action<BattleUnitRuntime> handler) // 사망 이벤트 일괄 등록
    { // 이벤트 등록 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 등록 시작
            if (runtimeUnit != null) // 유닛 존재 확인
            { // 유닛 존재 처리 시작
                runtimeUnit.Died += handler; // 사망 이벤트 등록
            } // 유닛 존재 처리 종료
        } // 유닛 등록 종료
    } // 이벤트 등록 종료
    private static void UnregisterDeathEvents(IReadOnlyList<BattleUnitRuntime> units, Action<BattleUnitRuntime> handler) // 사망 이벤트 일괄 해제
    { // 이벤트 해제 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 해제 시작
            if (runtimeUnit != null) // 유닛 존재 확인
            { // 유닛 존재 처리 시작
                runtimeUnit.Died -= handler; // 사망 이벤트 해제
            } // 유닛 존재 처리 종료
        } // 유닛 해제 종료
    } // 이벤트 해제 종료
    private static bool ContainsUnit(IReadOnlyList<BattleUnitRuntime> units, BattleUnitRuntime targetUnit) // 런타임 목록 포함 확인
    { // 유닛 포함 검사 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 일치 확인 시작
            if (runtimeUnit == targetUnit) // 대상 유닛 일치 확인
            { // 일치 처리 시작
                return true; // 포함 상태 반환
            } // 일치 처리 종료
        } // 유닛 일치 확인 종료
        return false; // 미포함 상태 반환
    } // 유닛 포함 검사 종료
    public void Dispose() // 적 행동 관리자 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 연결 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 연결 해제 상태 저장
        if (turns != null) // 실제 턴 이벤트 연결 확인
        { // 장화 횟수 해제 시작
            turns.PhaseStarted -= ResetBootUses; // 끝난 전투의 공용 횟수 연결 해제
            turns.PhaseEnded -= HandleEnemyPhaseEnded; // 끝난 전투의 도발과 맹세 종료 연결 해제
        } // 장화 횟수 해제 종료
        UnregisterDeathEvents(enemyUnits, HandleEnemyDied); // 적 사망 이벤트 해제
        UnregisterDeathEvents(allyUnits, HandleAllyDied); // 아군 사망 이벤트 해제
        foreach (BattleUnitRuntime ally in allyUnits) // 현재 전투 아군 순회
        { // 부활 이벤트 해제 시작
            if (ally != null) // 해제할 아군 확인
            { // 유효 아군 해제 시작
                ally.Revived -= HandleAllyRevived; // 현재 전투의 아군 부활 연결 해제
            } // 유효 아군 해제 종료
        } // 부활 이벤트 해제 종료
        SetShieldTaunter(null); // 종료한 전투의 도발자 연결 해제
        plannedActions.Clear(); // 예정 행동 목록 비우기
        bodyGuards.Clear(); // 전투에 남아 있는 대신 받기 보호 제거
        shieldOaths.Clear(); // 종료된 전투의 지속 맹세 제거
        nextPatternIndices.Clear(); // 적별 패턴 순번 비우기
    } // 연결 해제 종료
} // 클래스 종료
