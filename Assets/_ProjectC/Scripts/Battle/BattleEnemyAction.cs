using System; // 기본 예외 기능 사용
public sealed class BattleEnemyAction // 적 예정 행동 정보
{ // 클래스 시작
    public BattleUnitRuntime Actor { get; } // 행동 적 조회
    public BattleUnitRuntime Target { get; private set; } // 대상 아군 조회
    public EnemyActionType ActionType { get; } // 행동 종류 조회
    public BattleDamageType DamageType { get; } // 피해 유형 조회
    public int Amount { get; } // 예정 수치 조회
    public BattleStatusEffectType StatusEffectType { get; } // 적용 상태 이상 종류 조회
    public int StatusDuration { get; } // 상태 지속 횟수 조회
    public int StatusMaximumStacks { get; } // 상태 최대 중첩 조회
    public EnemyTargetRule TargetRule { get; } // 대상 선택 규칙 조회
    public string PatternDisplayName { get; } // 패턴 행동 이름 조회
    public int PatternIndex { get; } // 현재 패턴 순번 조회
    public int PatternCount { get; } // 전체 패턴 수 조회
    public int ActionSpeed { get; } // 결정된 행동 속도 조회
    public int CreationOrder { get; } // 행동 생성 순서 조회
    public int ActionOrder { get; private set; } // 최종 행동 순번 조회
    public bool IsExecutable => Actor != null && !Actor.IsDead && Target != null && !Target.IsDead && IsActionConfigurationValid(); // 실행 가능 여부 조회
    public BattleEnemyAction(BattleUnitRuntime actor, BattleUnitRuntime target, EnemyActionType actionType, BattleDamageType damageType, int amount, BattleStatusEffectType statusEffectType, int statusDuration, int statusMaximumStacks, EnemyTargetRule targetRule, string patternDisplayName, int patternIndex, int patternCount, int actionSpeed, int creationOrder) // 예정 행동 생성
    { // 생성자 시작
        Actor = actor ?? throw new ArgumentNullException(nameof(actor)); // 행동 적 저장
        Target = target ?? throw new ArgumentNullException(nameof(target)); // 대상 아군 저장
        ActionType = actionType; // 행동 종류 저장
        DamageType = damageType; // 피해 유형 저장
        Amount = amount; // 예정 수치 저장
        StatusEffectType = statusEffectType; // 상태 이상 종류 저장
        StatusDuration = Math.Max(1, statusDuration); // 보정된 상태 지속 횟수 저장
        StatusMaximumStacks = Math.Max(1, statusMaximumStacks); // 보정된 상태 최대 중첩 저장
        TargetRule = targetRule; // 대상 선택 규칙 저장
        PatternDisplayName = string.IsNullOrWhiteSpace(patternDisplayName) ? "행동" : patternDisplayName; // 보정된 패턴 행동 이름 저장
        PatternIndex = Math.Max(1, patternIndex); // 보정된 현재 패턴 순번 저장
        PatternCount = Math.Max(PatternIndex, patternCount); // 보정된 전체 패턴 수 저장
        ActionSpeed = Math.Max(1, actionSpeed); // 보정된 행동 속도 저장
        CreationOrder = Math.Max(0, creationOrder); // 보정된 생성 순서 저장
    } // 생성자 종료
    public void SetActionOrder(int actionOrder) // 최종 행동 순번 지정
    { // 순번 지정 시작
        ActionOrder = Math.Max(1, actionOrder); // 보정된 행동 순번 저장
    } // 순번 지정 종료
    public bool ChangeTarget(BattleUnitRuntime target) // 대상 변경
    { // 대상 변경 시작
        if (target == null || target.IsDead || target.Team != BattleTeam.Ally) // 대상 유효성 확인
        { // 잘못된 대상 처리 시작
            return false; // 대상 변경 실패 반환
        } // 잘못된 대상 처리 종료
        Target = target; // 새 대상 저장
        return true; // 대상 변경 성공 반환
    } // 대상 변경 종료
    public BattleDamageResult PreviewDamage() // 예정 피해 계산
    { // 예정 피해 계산 시작
        if (Target == null || Target.IsDead) // 대상 유효성 확인
        { // 대상 없음 처리 시작
            return BattleDamageResult.Empty(DamageType); // 피해 없음 결과 반환
        } // 대상 없음 처리 종료
        return Target.PreviewDamage(Amount, DamageType); // 대상 방어력 포함 예상 피해 반환
    } // 예정 피해 계산 종료
    public BattleEnemyActionResult Execute() // 예정 행동 실행
    { // 행동 실행 시작
        if (!IsExecutable) // 실행 가능 여부 확인
        { // 실행 불가 처리 시작
            return BattleEnemyActionResult.Empty(ActionType, DamageType); // 적용 수치 없음 반환
        } // 실행 불가 처리 종료
        if (ActionType == EnemyActionType.ApplyStatusEffect) // 상태 적용 행동 확인
        { // 상태 적용 처리 시작
            BattleStatusEffectApplyResult applyResult = Target.ApplyStatusEffect(StatusEffectType, Amount, StatusDuration, StatusMaximumStacks); // 대상 상태 이상 적용
            return BattleEnemyActionResult.FromStatus(applyResult); // 상태 적용 결과 반환
        } // 상태 적용 처리 종료
        BattleDamageResult damageResult = Target.TakeDamage(Amount, DamageType); // 대상 피해 적용
        return BattleEnemyActionResult.FromDamage(damageResult); // 피해 행동 결과 반환
    } // 행동 실행 종료
    private bool IsActionConfigurationValid() // 행동 설정 유효성 확인
    { // 설정 확인 시작
        if (ActionType == EnemyActionType.Attack) // 공격 행동 확인
        { // 공격 설정 처리 시작
            return Amount > 0; // 공격 수치 유효성 반환
        } // 공격 설정 처리 종료
        return ActionType == EnemyActionType.ApplyStatusEffect && StatusEffectType != BattleStatusEffectType.None && Amount > 0 && StatusDuration > 0 && StatusMaximumStacks > 0; // 상태 행동 설정 유효성 반환
    } // 설정 확인 종료
} // 클래스 종료
