public readonly struct BattleEnemyActionResult // 적 행동 실행 결과
{ // 구조체 시작
    public EnemyActionType ActionType { get; } // 실행 행동 종류 조회
    public BattleDamageResult DamageResult { get; } // 피해 실행 결과 조회
    public BattleStatusEffectApplyResult StatusApplyResult { get; } // 상태 적용 결과 조회
    public bool WasMissed { get; } // 공격 빗나감 여부
    public bool HasAdditionalStatus { get; } // 공격 후 상태 적용 시도 여부
    public bool IsStatusAction => ActionType == EnemyActionType.ApplyStatusEffect; // 상태 행동 여부 조회
    private BattleEnemyActionResult(EnemyActionType actionType, BattleDamageResult damageResult, BattleStatusEffectApplyResult statusApplyResult, bool wasMissed = false, bool hasAdditionalStatus = false) // 적 행동 결과 생성
    { // 생성자 시작
        ActionType = actionType; // 행동 종류 저장
        DamageResult = damageResult; // 피해 결과 저장
        StatusApplyResult = statusApplyResult; // 상태 적용 결과 저장
        WasMissed = wasMissed; // 공격 빗나감 여부 저장
        HasAdditionalStatus = hasAdditionalStatus; // 공격 후 상태 시도 여부 저장
    } // 생성자 종료
    public static BattleEnemyActionResult Empty(EnemyActionType actionType, BattleDamageType damageType) // 빈 행동 결과 생성
    { // 빈 결과 생성 시작
        return new BattleEnemyActionResult(actionType, BattleDamageResult.Empty(damageType), BattleStatusEffectApplyResult.Invalid); // 빈 행동 결과 반환
    } // 빈 결과 생성 종료
    public static BattleEnemyActionResult FromDamage(BattleDamageResult damageResult, BattleStatusEffectApplyResult statusApplyResult = BattleStatusEffectApplyResult.Invalid, bool hasAdditionalStatus = false) // 피해 행동 결과 생성
    { // 피해 결과 생성 시작
        return new BattleEnemyActionResult(EnemyActionType.Attack, damageResult, statusApplyResult, false, hasAdditionalStatus); // 피해와 추가 상태 결과 반환
    } // 피해 결과 생성 종료
    public static BattleEnemyActionResult Missed(BattleDamageType damageType) // 공격 빗나감 결과 생성
    { // 빗나감 결과 시작
        return new BattleEnemyActionResult(EnemyActionType.Attack, BattleDamageResult.Empty(damageType), BattleStatusEffectApplyResult.Invalid, true); // 빗나감 결과 반환
    } // 빗나감 결과 종료
    public static BattleEnemyActionResult FromStatus(BattleStatusEffectApplyResult statusApplyResult) // 상태 행동 결과 생성
    { // 상태 결과 생성 시작
        return new BattleEnemyActionResult(EnemyActionType.ApplyStatusEffect, BattleDamageResult.Empty(BattleDamageType.None), statusApplyResult); // 상태 행동 결과 반환
    } // 상태 결과 생성 종료
} // 구조체 종료
