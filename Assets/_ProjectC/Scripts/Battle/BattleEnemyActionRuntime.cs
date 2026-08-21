using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleEnemyActionRuntime : IDisposable // 적 행동 흐름 관리
{ // 클래스 시작
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 적 런타임 목록
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 아군 런타임 목록
    private readonly List<BattleEnemyAction> plannedActions = new List<BattleEnemyAction>(); // 예정 행동 목록
    private readonly Random random = new Random(); // 무작위 대상 선택기
    private BattleEnemyAction executingAction; // 현재 실행 행동
    private bool disposed; // 연결 해제 여부
    public IReadOnlyList<BattleEnemyAction> PlannedActions => plannedActions; // 예정 행동 목록 조회
    public event Action StateChanged; // 예정 행동 변경 이벤트
    public BattleEnemyActionRuntime(IReadOnlyList<BattleUnitRuntime> enemies, IReadOnlyList<BattleUnitRuntime> allies) // 적 행동 관리자 생성
    { // 생성자 시작
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        RegisterDeathEvents(enemyUnits, HandleEnemyDied); // 적 사망 이벤트 등록
        RegisterDeathEvents(allyUnits, HandleAllyDied); // 아군 사망 이벤트 등록
    } // 생성자 종료
    public bool RegisterSummonedEnemy(BattleUnitRuntime enemyUnit) // 소환 적 행동 연결
    { // 소환 적 연결 시작
        if (disposed || enemyUnit == null || enemyUnit.IsDead || enemyUnit.Team != BattleTeam.Enemy || !ContainsUnit(enemyUnits, enemyUnit)) // 소환 적 연결 조건 확인
        { // 연결 불가 처리 시작
            return false; // 소환 적 연결 실패 반환
        } // 연결 불가 처리 종료
        enemyUnit.Died -= HandleEnemyDied; // 기존 중복 사망 연결 제거
        enemyUnit.Died += HandleEnemyDied; // 소환 적 사망 이벤트 등록
        return true; // 소환 적 연결 성공 반환
    } // 소환 적 연결 종료
    public bool UnregisterEnemy(BattleUnitRuntime enemyUnit) // 제거 적 행동 연결 해제
    { // 제거 적 해제 시작
        if (enemyUnit == null) // 제거 적 존재 확인
        { // 적 없음 처리 시작
            return false; // 연결 해제 실패 반환
        } // 적 없음 처리 종료
        enemyUnit.Died -= HandleEnemyDied; // 적 사망 이벤트 해제
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
                plannedActions.Add(action); // 예정 행동 목록 추가
            } // 유효 행동 처리 종료
        } // 적 행동 생성 종료
        plannedActions.Sort(CompareActionOrder); // 속도와 배치 순서 기준 정렬
        ApplyActionOrderNumbers(); // 최종 행동 순번 지정
        StateChanged?.Invoke(); // 예정 행동 변경 알림
        return plannedActions.Count; // 생성 행동 수 반환
    } // 행동 준비 종료
    public BattleDamageResult ExecuteAction(BattleEnemyAction action) // 적 행동 실행
    { // 행동 실행 시작
        if (disposed || action == null || !plannedActions.Contains(action)) // 실행 대상 유효성 확인
        { // 실행 불가 처리 시작
            return BattleDamageResult.Empty(action == null ? BattleDamageType.None : action.DamageType); // 적용 피해 없음 반환
        } // 실행 불가 처리 종료
        if (action.Actor.IsDead) // 행동 적 사망 확인
        { // 사망 적 처리 시작
            plannedActions.Remove(action); // 예정 행동 제거
            ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
            StateChanged?.Invoke(); // 예정 행동 변경 알림
            return BattleDamageResult.Empty(action.DamageType); // 적용 피해 없음 반환
        } // 사망 적 처리 종료
        if (action.Target == null || action.Target.IsDead) // 기존 대상 상태 확인
        { // 대상 재선택 시작
            BattleUnitRuntime replacementTarget = SelectTarget(action.Actor.EnemySource.TargetRule); // 새 생존 대상 선택
            if (!action.ChangeTarget(replacementTarget)) // 대상 변경 결과 확인
            { // 대상 없음 처리 시작
                plannedActions.Remove(action); // 실행 불가 행동 제거
                ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
                StateChanged?.Invoke(); // 예정 행동 변경 알림
                return BattleDamageResult.Empty(action.DamageType); // 적용 피해 없음 반환
            } // 대상 없음 처리 종료
        } // 대상 재선택 종료
        executingAction = action; // 현재 실행 행동 저장
        BattleDamageResult damageResult = action.Execute(); // 적 공격 피해 적용
        executingAction = null; // 현재 실행 행동 제거
        plannedActions.Remove(action); // 실행 완료 행동 제거
        ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
        StateChanged?.Invoke(); // 예정 행동 변경 알림
        return damageResult; // 피해 계산 결과 반환
    } // 행동 실행 종료
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
    private BattleEnemyAction CreateAction(BattleUnitRuntime enemyUnit, int creationOrder) // 적 예정 행동 생성
    { // 행동 생성 시작
        if (enemyUnit == null || enemyUnit.IsDead || enemyUnit.EnemySource == null) // 적 데이터 유효성 확인
        { // 잘못된 적 처리 시작
            return null; // 행동 생성 실패 반환
        } // 잘못된 적 처리 종료
        EnemyData enemyData = enemyUnit.EnemySource; // 적 원본 데이터 조회
        if (enemyData.ActionType != EnemyActionType.Attack || enemyData.BasicAttackPower < 1) // 지원 행동 확인
        { // 미지원 행동 처리 시작
            return null; // 행동 생성 실패 반환
        } // 미지원 행동 처리 종료
        BattleUnitRuntime targetUnit = SelectTarget(enemyData.TargetRule); // 행동 대상 선택
        if (targetUnit == null) // 대상 존재 확인
        { // 대상 없음 처리 시작
            return null; // 행동 생성 실패 반환
        } // 대상 없음 처리 종료
        int actionSpeed = RollActionSpeed(enemyData); // 이번 턴 행동 속도 결정
        return new BattleEnemyAction(enemyUnit, targetUnit, enemyData.ActionType, enemyData.DamageType, enemyData.BasicAttackPower, actionSpeed, creationOrder); // 예정 공격 반환
    } // 행동 생성 종료
    private int RollActionSpeed(EnemyData enemyData) // 적 행동 속도 결정
    { // 속도 결정 시작
        int minimumSpeed = enemyData.MinimumActionSpeed; // 최소 행동 속도 조회
        int maximumSpeed = enemyData.MaximumActionSpeed; // 최대 행동 속도 조회
        return random.Next(minimumSpeed, maximumSpeed + 1); // 양끝 포함 무작위 속도 반환
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
        if (removedCount > 0) // 제거 행동 확인
        { // 변경 알림 시작
            ApplyActionOrderNumbers(); // 남은 행동 순번 재지정
            StateChanged?.Invoke(); // 예정 행동 변경 알림
        } // 변경 알림 종료
    } // 적 사망 처리 종료
    private void HandleAllyDied(BattleUnitRuntime allyUnit) // 아군 사망 처리
    { // 아군 사망 처리 시작
        bool changed = false; // 대상 변경 여부 초기화
        foreach (BattleEnemyAction action in plannedActions) // 예정 행동 순회
        { // 대상 확인 시작
            if (action == executingAction) // 현재 실행 행동 확인
            { // 실행 행동 처리 시작
                continue; // 대상 재선택 제외
            } // 실행 행동 처리 종료
            if (action.Target != allyUnit) // 사망 대상 일치 확인
            { // 다른 대상 처리 시작
                continue; // 다음 행동 이동
            } // 다른 대상 처리 종료
            BattleUnitRuntime replacementTarget = SelectTarget(action.Actor.EnemySource.TargetRule); // 대체 대상 선택
            changed |= action.ChangeTarget(replacementTarget); // 대상 변경 결과 저장
        } // 대상 확인 종료
        if (changed) // 대상 변경 확인
        { // 변경 알림 시작
            StateChanged?.Invoke(); // 예정 행동 변경 알림
        } // 변경 알림 종료
    } // 아군 사망 처리 종료
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
        UnregisterDeathEvents(enemyUnits, HandleEnemyDied); // 적 사망 이벤트 해제
        UnregisterDeathEvents(allyUnits, HandleAllyDied); // 아군 사망 이벤트 해제
        plannedActions.Clear(); // 예정 행동 목록 비우기
    } // 연결 해제 종료
} // 클래스 종료
