using System; // 기본 예외 기능 사용

public sealed class BattleEnemyAction // 적 예정 행동 정보
{ // 행동 정보 시작
    private Func<BattleEnemyAction, bool> canPreviewPreparation; // 방어 준비 예고 순서 확인
    private Func<BattleEnemyAction, int> getBootReduction; // 현재 예고 순서에서 사용할 수 있는 장화 경감량
    private Func<BattleEnemyAction, int> getPreviewBlock; // 공격 순서별 남은 방어도 조회
    private Func<BattleEnemyAction, BattleUnitRuntime> getBodyGuardPreviewTarget; // 대신 받을 아군 예고 조회
    private BattleUnitRuntime executedAttackTarget; // 실행 당시의 실제 공격 대상
    private BattleUnitRuntime damageShareRecipient; // 원래 대상의 피해 분담 동료
    private readonly Func<int> rollPercent; // 명중과 추가 상태 확률 난수
    private readonly int statusChancePercent; // 공격 후 상태 부여 확률
    private readonly int statusValue; // 공격 후 상태 효과 수치
    public BattleUnitRuntime Actor { get; } // 행동 적 조회
    internal BattleUnitRuntime IntendedTarget { get; private set; } // 전환 효과 전의 지정 대상
    public BattleUnitRuntime Target => executedAttackTarget ?? getBodyGuardPreviewTarget?.Invoke(this) ?? IntendedTarget; // 실제 또는 예고 피격 대상
    public BattleUnitRuntime SharedDamageRecipient => Target == IntendedTarget ? damageShareRecipient : null; // 현재 피격 대상의 분담 동료
    public EnemyActionType ActionType { get; } // 행동 종류 조회
    public BattleDamageType DamageType { get; } // 피해 유형 조회
    public int Amount { get; } // 층 난이도 포함 예정 수치 조회
    public BattleStatusEffectType StatusEffectType { get; } // 적용 상태 이상 종류 조회
    public int StatusDuration { get; } // 상태 지속 횟수 조회
    public int StatusMaximumStacks { get; } // 상태 최대 중첩 조회
    public int AdditionalStatusChancePercent => statusChancePercent; // 공격 후 상태 부여 확률 조회
    public int AdditionalStatusValue => statusValue; // 공격 후 상태 효과 수치 조회
    public int AdditionalMentalChange { get; } // 공격 적중 시 정신력 변화 조회
    public EnemyTargetRule TargetRule { get; } // 대상 선택 규칙 조회
    public string PatternDisplayName { get; } // 패턴 행동 이름 조회
    public int PatternIndex { get; } // 현재 패턴 순번 조회
    public int PatternCount { get; } // 전체 패턴 수 조회
    public int ActionSpeed { get; } // 결정된 행동 속도 조회
    public int CreationOrder { get; } // 행동 생성 순서 조회
    public int ActionOrder { get; private set; } // 최종 행동 순번 조회

    public BattleStatusEffectType BlockingStatus => Actor.GetStatusEffect(BattleStatusEffectType.Stun) != null ? BattleStatusEffectType.Stun : Actor.GetStatusEffect(BattleStatusEffectType.Stagger) != null ? BattleStatusEffectType.Stagger : BattleStatusEffectType.None; // 이번 행동을 막는 실제 상태
    private int TurnAttackPenalty => DamageType == BattleDamageType.Physical || DamageType == BattleDamageType.Magical ? Actor.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)?.EffectiveValue ?? 0 : 0; // 물리·마법 공격에만 적용할 경보 약화
    public int AttackAmount => Math.Max(0, Actor.ModifyOutgoingDamage(Amount) - (Actor.GetStatusEffect(BattleStatusEffectType.NextAttackDown)?.EffectiveValue ?? 0) - TurnAttackPenalty); // 정신 상태와 다음 공격 약화를 반영한 원본 피해

    public bool IsExecutable =>
        Actor != null &&
        !Actor.IsDead &&
        BlockingStatus == BattleStatusEffectType.None && // 취소되거나 기절한 행동 제외
        IntendedTarget != null && // 지정 대상 확인
        !IntendedTarget.IsDead && // 지정 대상 생존 확인
        IsActionConfigurationValid(); // 실행 가능 여부 조회

    internal bool IsPositiveDirectAttack => IsExecutable && Actor.Team == BattleTeam.Enemy && ActionType == EnemyActionType.Attack && AttackAmount > 0; // 양수 직접 적 공격 확인

    public BattleEnemyAction(
        BattleUnitRuntime actor,
        BattleUnitRuntime target,
        EnemyActionType actionType,
        BattleDamageType damageType,
        int amount,
        BattleStatusEffectType statusEffectType,
        int statusDuration,
        int statusMaximumStacks,
        EnemyTargetRule targetRule,
        string patternDisplayName,
        int patternIndex,
        int patternCount,
        int actionSpeed,
        int creationOrder, // 생성 순서 전달
        int additionalStatusChancePercent = 0, // 공격 후 상태 부여 확률 전달
        int additionalStatusValue = 1, // 공격 후 상태 효과 수치 전달
        Func<int> percentRoll = null, // 테스트 가능한 확률 난수 전달
        int mentalChangeOnHit = 0) // 적중 시 정신력 변화 전달
    {
        Actor =
            actor ??
            throw new ArgumentNullException(nameof(actor)); // 행동 적 저장

        IntendedTarget = // 원래 대상 지정
            target ??
            throw new ArgumentNullException(nameof(target)); // 대상 아군 저장

        ActionType = actionType; // 행동 종류 저장
        DamageType = damageType; // 피해 유형 저장

        Amount =
            GetScaledActionAmount(
                actionType,
                amount); // 공격 행동에 현재 층 배율 적용

        StatusEffectType = statusEffectType; // 상태 이상 종류 저장
        StatusDuration = Math.Max(1, statusDuration); // 보정된 상태 지속 횟수 저장
        StatusMaximumStacks = Math.Max(1, statusMaximumStacks); // 보정된 상태 최대 중첩 저장
        TargetRule = targetRule; // 대상 선택 규칙 저장

        PatternDisplayName =
            string.IsNullOrWhiteSpace(patternDisplayName)
                ? "행동"
                : patternDisplayName; // 보정된 패턴 행동 이름 저장

        PatternIndex = Math.Max(1, patternIndex); // 보정된 현재 패턴 순번 저장
        PatternCount = Math.Max(PatternIndex, patternCount); // 보정된 전체 패턴 수 저장
        ActionSpeed = Math.Max(1, actionSpeed); // 보정된 행동 속도 저장
        CreationOrder = Math.Max(0, creationOrder); // 보정된 생성 순서 저장
        statusChancePercent = Math.Max(0, Math.Min(100, additionalStatusChancePercent)); // 상태 부여 확률 범위 보정
        statusValue = Math.Max(1, additionalStatusValue); // 상태 효과 수치 범위 보정
        rollPercent = percentRoll ?? (() => UnityEngine.Random.Range(0, 100)); // 기본 또는 테스트 난수 저장
        AdditionalMentalChange = Math.Max(-100, Math.Min(100, mentalChangeOnHit)); // 정신력 변화 범위 보정
    }

    public void SetActionOrder(
        int actionOrder) // 최종 행동 순번 지정
    {
        ActionOrder =
            Math.Max(
                1,
                actionOrder); // 보정된 행동 순번 저장
    }

    public bool ChangeTarget(
        BattleUnitRuntime target) // 대상 변경
    {
        if (target == null ||
            target.IsDead ||
            target.Team != BattleTeam.Ally)
        {
            return false;
        }

        if (IntendedTarget != target) // 지정 공격 대상 변경 확인
        { // 기존 분담 해제 시작
            damageShareRecipient = null; // 이전 지정 대상의 분담 해제
        } // 기존 분담 해제 종료
        IntendedTarget = target; // 새 지정 대상 저장
        executedAttackTarget = null; // 이전 실행 대상 초기화
        return true;
    }
    internal bool SetDamageShare(BattleUnitRuntime recipient) // 생존 동료를 피해 분담자로 설정
    { // 피해 분담 설정 시작
        if (recipient == null || recipient.IsDead || recipient == IntendedTarget || recipient.Team != BattleTeam.Ally || ActionType != EnemyActionType.Attack) // 지정 대상과 분담자 유효성 확인
        { // 분담 설정 불가 시작
            return false; // 설정 실패 반환
        } // 분담 설정 불가 종료
        damageShareRecipient = recipient; // 현재 공격의 피해 분담자 저장
        return true; // 설정 성공 반환
    } // 피해 분담 설정 종료
    internal bool ClearDamageShare(BattleUnitRuntime recipient) // 지정 분담자의 피해 분담 해제
    { // 피해 분담 해제 시작
        if (recipient == null || damageShareRecipient != recipient) // 지정 분담자 일치 확인
        { // 해제 대상 없음 시작
            return false; // 변경 없음 반환
        } // 해제 대상 없음 종료
        damageShareRecipient = null; // 지정 분담자 연결 해제
        return true; // 해제 성공 반환
    } // 피해 분담 해제 종료

    internal void SetPreparationPreviewRule(Func<BattleEnemyAction, bool> rule) // 방어 준비 예고 규칙 연결
    { // 예고 규칙 연결 시작
        canPreviewPreparation = rule; // 공격 순서 확인 규칙 저장
    } // 예고 규칙 연결 종료
    internal void SetBlockPreviewRule(Func<BattleEnemyAction, int> rule) // 방어도 예고 규칙 연결
    { // 방어도 규칙 연결 시작
        getPreviewBlock = rule; // 방어도 예측 규칙 저장
    } // 방어도 규칙 연결 종료
    internal void SetBootPreviewRule(Func<BattleEnemyAction, int> rule) // 장화 잔여 횟수를 반영한 예고 연결
    { // 경감 예고 연결 시작
        getBootReduction = rule; // 현재 파티의 이번 턴 경감 기회 조회
    } // 경감 예고 연결 종료
    internal void SetBodyGuardPreviewRule(Func<BattleEnemyAction, BattleUnitRuntime> rule) // 대신 받기 대상 예고 연결
    { // 보호 예고 연결 시작
        getBodyGuardPreviewTarget = rule; // 대신 받을 대상 조회 규칙 저장
    } // 보호 예고 연결 종료
    public BattleDamageResult PreviewDamage() // 예정 피해 계산
    { // 예정 피해 계산 시작
        return PreviewDamageWithBlock(Target == null ? 0 : getPreviewBlock == null ? Target.CurrentBlock : getPreviewBlock(this)); // 공격 순서별 방어도 반영
    } // 예정 피해 계산 종료
    public int PreviewSharedDamage() // 분담자가 받을 실제 피해 예고
    { // 분담 피해 예고 시작
        if (Target == null || Target.IsDead || SharedDamageRecipient == null || SharedDamageRecipient.IsDead) // 공격 대상과 생존 분담자 확인
        { // 분담 피해 없음 처리 시작
            return 0; // 분담 피해 없음 반환
        } // 분담 피해 없음 처리 종료
        int block = getPreviewBlock == null ? Target.CurrentBlock : getPreviewBlock(this); // 공격 전 대상의 예상 방어도 조회
        BattleDamageResult preview = PreviewDamageBeforeShare(block); // 분담 전 대상의 방어 적용 피해 계산
        int remaining = Math.Max(0, preview.FinalDamage - preview.AbsorbedDamage); // 방어 적용 후 남은 피해 계산
        return Math.Min(SharedDamageRecipient.CurrentHealth, remaining / 2); // 분담자의 남은 체력을 반영한 피해 반환
    } // 분담 피해 예고 종료
    internal BattleDamageResult PreviewDamageWithBlock(int block) // 지정 방어도 기준 예정 피해 계산
    { // 분담을 포함한 피해 예고 시작
        var preview = PreviewDamageBeforeShare(block); // 방어도와 물리·마법 방어를 적용한 예정 결과
        int primary = GetPrimaryHealthDamage(preview); // 분담 후 주 대상에게 남는 체력 피해
        int reduction = primary > 0 && getBootReduction != null ? getBootReduction(this) : 0; // 이번 순서의 장화 경감량
        return preview.WithAppliedDamage(Math.Min(Target?.CurrentHealth ?? 0, Math.Max(0, primary - reduction))); // 대상 유무와 현재 체력 및 장화를 적용한 예고
    } // 지정 방어도 예고 종료
    internal int GetPrimaryHealthDamage(BattleDamageResult preview) // 방어도와 분담 뒤 주 대상에게 남은 피해
    { // 주 대상 체력 피해 계산 시작
        int remaining = Math.Max(0, preview.FinalDamage - preview.AbsorbedDamage); // 방어도 흡수 뒤 남은 피해
        return SharedDamageRecipient != null && !SharedDamageRecipient.IsDead ? remaining - remaining / 2 : remaining; // 분담 시 홀수 한 점은 원래 대상 몫
    } // 분담을 포함한 피해 예고 종료
    internal BattleDamageResult PreviewDamageBeforeShare(int block) // 지정 방어도 기준 분담 전 예정 피해 계산
    { // 분담 전 피해 예고 시작
        if (BlockingStatus != BattleStatusEffectType.None || Target == null || // 행동 차단 또는 대상 누락 확인
            Target.IsDead)
        {
            return BattleDamageResult.Empty(
                DamageType); // 피해 없음 결과 반환
        }

        int modifiedAmount = AttackAmount; // 실제 공격 약화와 정신 상태를 반영한 피해

        return Target.PreviewDamage( // 적 공격 예상 피해 계산
            modifiedAmount, // 정신 상태 포함 피해량 전달
            DamageType, // 공격 피해 유형 전달
            canPreviewPreparation == null || canPreviewPreparation(this) ? Actor : null, // 첫 공격에만 방어 준비 포함
            block, enemyDirectAttack: true); // 현재 예고 순서의 잔여 방어도와 코일 반영
    }

    public BattleEnemyActionResult Execute(Func<bool> preventHealthDamage = null, Func<int, int> reduceHealthDamage = null) // 예정 행동 실행
    { // 일반 실행 시작
        return Execute(preventHealthDamage, reduceHealthDamage, null, null); // 기존 단독 실행 경로 연결
    } // 일반 실행 종료

    internal BattleEnemyActionResult Execute(Func<bool> preventHealthDamage, Func<int, int> reduceHealthDamage, BattleUnitRuntime guard, Action consumeBodyGuard) // 대신 받기를 포함한 행동 실행
    { // 보호 포함 실행 시작
        if (BlockingStatus == BattleStatusEffectType.Stagger) // 다음 행동 한 번을 취소하는 상태 확인
        { // 행동 한 번 취소 시작
            Actor.RemoveStatusEffect(BattleStatusEffectType.Stagger); // 취소한 예고 한 번에만 상태 소모
            return BattleEnemyActionResult.Empty(ActionType, DamageType); // 피해와 부가 효과 없는 결과
        } // 행동 한 번 취소 종료
        if (!IsExecutable)
        {
            return BattleEnemyActionResult.Empty(
                ActionType,
                DamageType); // 적용 수치 없음 반환
        }

        if (ActionType == EnemyActionType.ApplyStatusEffect)
        {
            BattleStatusEffectApplyResult applyResult =
                Target.ApplyStatusEffect(
                    StatusEffectType,
                    Amount,
                    StatusDuration,
                    StatusMaximumStacks,
                    Actor); // 대상 상태 이상 적용

            return BattleEnemyActionResult.FromStatus(
                applyResult); // 상태 적용 결과 반환
        }

        int attackDamage = AttackAmount; // 약화 소모 전 이번 공격 수치 보관
        Actor.RemoveStatusEffect(BattleStatusEffectType.NextAttackDown); // 적이 실제 공격을 시도하면 명중과 관계없이 한 번 소모
        BattleUnitRuntime attackTarget = attackDamage > 0 && guard != null && !guard.IsDead ? guard : IntendedTarget; // 현재 명중 판정 대상 선택
        executedAttackTarget = attackTarget; // 연출과 결과 표시용 피격 대상 고정
        int hitChance = BattleHitChance.GetPercent(Actor, attackTarget); // 실제 대상 회피를 포함한 명중률
        if (hitChance < 100 && !BattleHitChance.Hits(Actor, rollPercent(), attackTarget)) // 실제 대상에 대한 명중 판정
        { // 공격 빗나감 처리 시작
            return BattleEnemyActionResult.Missed(DamageType); // 피해와 상태가 없는 빗나감 반환
        } // 공격 빗나감 처리 종료

        if (attackTarget != IntendedTarget) // 보호자에게 명중한 공격 확인
        { // 대신 받기 소모 시작
            consumeBodyGuard?.Invoke(); // 피격 이벤트 전 이번 효과 소모
        } // 대신 받기 소모 종료

        BattleDamageResult damageResult = attackTarget.TakeDamage(attackDamage, DamageType, Actor, SharedDamageRecipient, preventHealthDamage, reduceHealthDamage: reduceHealthDamage, enemyDirectAttack: true); // 실제 피격 대상의 방어와 피해 처리

        bool applyAdditionalStatus = !attackTarget.IsDead && StatusEffectType != BattleStatusEffectType.None && statusChancePercent > 0 && rollPercent() < statusChancePercent; // 실제 피격 대상의 추가 상태 판정
        BattleStatusEffectApplyResult statusResult = applyAdditionalStatus ? attackTarget.ApplyStatusEffect(StatusEffectType, statusValue, StatusDuration, StatusMaximumStacks, Actor) : BattleStatusEffectApplyResult.Invalid; // 실제 피격 대상의 추가 상태 적용
        if (!attackTarget.IsDead && AdditionalMentalChange != 0) // 실제 피격 대상의 정신력 변화 확인
        { // 정신력 압박 적용 시작
            attackTarget.ChangeMental(AdditionalMentalChange, BattleMentalChangeReason.DisruptReceived); // 실제 피격 대상의 정신력 변화 적용
        } // 정신력 압박 적용 종료
        return BattleEnemyActionResult.FromDamage(damageResult, statusResult, applyAdditionalStatus); // 피해와 추가 상태 결과 반환
    } // 보호 포함 실행 종료

    private static int GetScaledActionAmount(
        EnemyActionType actionType,
        int baseAmount) // 행동 종류별 최종 예정 수치 계산
    {
        if (actionType != EnemyActionType.Attack)
        {
            return baseAmount; // 상태 효과 수치는 층 난이도 미적용
        }

        int battleFloor =
            ExplorationDifficultyCalculator.GetCurrentBattleFloor(); // 현재 탐사 전투 층 조회

        BattleType battleType =
            ExplorationDifficultyCalculator.GetCurrentBattleType(); // 현재 조우 등급 조회

        return ExplorationDifficultyCalculator.ScaleAttack(
            baseAmount,
            battleFloor,
            battleType); // 기본·패턴 공격 모두 층·등급 배율 적용
    }

    private bool IsActionConfigurationValid() // 행동 설정 유효성 확인
    {
        if (ActionType == EnemyActionType.Attack)
        {
            return Amount > 0; // 공격 수치 유효성 반환
        }

        return
            ActionType == EnemyActionType.ApplyStatusEffect &&
            StatusEffectType != BattleStatusEffectType.None &&
            Amount > 0 &&
            StatusDuration > 0 &&
            StatusMaximumStacks > 0; // 상태 행동 설정 유효성 반환
    }
}
