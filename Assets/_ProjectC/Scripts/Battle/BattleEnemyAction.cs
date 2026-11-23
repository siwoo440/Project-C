using System; // 기본 예외 기능 사용
using System.Collections.Generic; // 광역 공격의 대상과 개별 결과 목록
using System.Linq; // 광역 공격의 대상 소속 조회

public sealed class BattleEnemyAction // 적 예정 행동 정보
{ // 행동 정보 시작
    private Func<BattleEnemyAction, BattleUnitRuntime, (int Block, bool Preparation, bool Interception, bool Nullification)> getPreviewDefense; // 피격 대상별 순서와 남은 방어 조회
    private Func<BattleEnemyAction, BattleUnitRuntime, int> getBootReduction; // 현재 생존 대상의 순서별 장화 경감량
    private IReadOnlyList<BattleUnitRuntime> areaTargets = Array.Empty<BattleUnitRuntime>(); // 광역 공격의 실제 전투 대상 목록
    private readonly List<(BattleUnitRuntime Target, BattleEnemyActionResult Result)> areaHitResults = new List<(BattleUnitRuntime Target, BattleEnemyActionResult Result)>(); // 이번 광역 공격의 대상별 실제 결과
    private Func<BattleEnemyAction, BattleUnitRuntime> getBodyGuardPreviewTarget; // 대신 받을 아군 예고 조회
    private BattleUnitRuntime executedAttackTarget; // 실행 당시의 실제 공격 대상
    private BattleUnitRuntime shieldTauntTarget; // 이번 공격의 살아 있는 도발 대상
    private BattleUnitRuntime damageShareTarget; // 피해 분담을 설정한 공격 대상
    private BattleUnitRuntime damageShareRecipient; // 지정 대상의 피해 분담 동료
    private readonly Func<int> rollPercent; // 명중과 추가 상태 확률 난수
    private readonly int statusChancePercent; // 공격 후 상태 부여 확률
    private readonly int statusValue; // 공격 후 상태 효과 수치
    public BattleUnitRuntime Actor { get; } // 행동 적 조회
    internal BattleUnitRuntime IntendedTarget { get; private set; } // 전환 효과 전의 지정 대상
    internal BattleUnitRuntime PrioritizedTarget => shieldTauntTarget != null && !shieldTauntTarget.IsDead ? shieldTauntTarget : IntendedTarget; // 대신 받기 전의 우선 공격 대상
    public BattleUnitRuntime Target => executedAttackTarget ?? getBodyGuardPreviewTarget?.Invoke(this) ?? PrioritizedTarget; // 실제 또는 예고 피격 대상
    public BattleUnitRuntime SharedDamageRecipient => Target == damageShareTarget ? damageShareRecipient : null; // 현재 피격 대상의 분담 동료
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
    public bool IsAreaAttack => ActionType == EnemyActionType.Attack && TargetRule == EnemyTargetRule.AllLiving; // 생존 인원과 무관한 명시적 광역 공격 의도
    public IReadOnlyList<BattleUnitRuntime> AreaTargets => areaTargets; // 광역 공격의 현재 배치 파티 조회
    public IReadOnlyList<(BattleUnitRuntime Target, BattleEnemyActionResult Result)> AreaHitResults => areaHitResults; // 광역 피격과 부가 효과 결과 조회
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
        PrioritizedTarget != null && // 우선 공격 대상 확인
        !PrioritizedTarget.IsDead && // 우선 공격 대상 생존 확인
        IsActionConfigurationValid(); // 실행 가능 여부 조회

    internal bool IsPositiveDirectAttack => IsExecutable && Actor.Team == BattleTeam.Enemy && ActionType == EnemyActionType.Attack && AttackAmount > 0; // 양수 직접 적 공격 확인
    internal bool IsSingleTargetEnemyAttack => Actor.Team == BattleTeam.Enemy && ActionType == EnemyActionType.Attack && (TargetRule == EnemyTargetRule.FirstLiving || TargetRule == EnemyTargetRule.LowestHealth || TargetRule == EnemyTargetRule.RandomLiving); // 도발과 맹세가 적용되는 단일 대상 공격 확인

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
        if (IsAreaAttack) // 단독 생성에서도 명시된 광역 대상 확인
        { // 광역 대표 대상 준비 시작
            areaTargets = new[] { target }; // 관리자 연결 전의 기본 광역 대상
        } // 광역 대표 대상 준비 종료

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

        IntendedTarget = target; // 새 지정 대상 저장
        executedAttackTarget = null; // 이전 실행 대상 초기화
        ClearPreviousDamageShare(); // 공격 대상이 달라진 경우 분담 해제
        return true;
    }
    internal void SetShieldTauntTarget(BattleUnitRuntime target) // 전체 도발의 우선 대상 설정
    { // 도발 대상 설정 시작
        shieldTauntTarget = IsSingleTargetEnemyAttack ? target : null; // 현재 규칙의 단일 대상 공격에 도발 적용
        ClearPreviousDamageShare(); // 도발 전 대상으로 맺은 분담 해제
    } // 도발 대상 설정 종료
    private void ClearPreviousDamageShare() // 대상 변경으로 무효가 된 분담 정리
    { // 분담 확인 시작
        if (damageShareTarget != PrioritizedTarget) // 분담 당시와 현재 우선 대상 비교
        { // 분담 초기화 시작
            damageShareTarget = null; // 이전 공격 대상 정리
            damageShareRecipient = null; // 이전 분담 동료 정리
        } // 분담 초기화 종료
    } // 분담 확인 종료
    internal bool SetDamageShare(BattleUnitRuntime recipient) // 생존 동료를 피해 분담자로 설정
    { // 피해 분담 설정 시작
        BattleUnitRuntime target = PrioritizedTarget; // 현재 우선 공격 대상 조회
        if (target == null || recipient == null || recipient.IsDead || recipient == target || recipient.Team != BattleTeam.Ally || !IsSingleTargetEnemyAttack) // 단일 공격의 대상과 분담자 유효성 확인
        { // 분담 설정 불가 시작
            return false; // 설정 실패 반환
        } // 분담 설정 불가 종료
        damageShareTarget = target; // 분담이 적용될 우선 공격 대상 보관
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
        damageShareTarget = null; // 분담을 설정한 공격 대상 정리
        return true; // 해제 성공 반환
    } // 피해 분담 해제 종료

    internal void SetAreaTargets(IReadOnlyList<BattleUnitRuntime> allies) // 적의 광역 공격 대상 목록 연결
    { // 광역 대상 연결 시작
        areaTargets = allies; // 현재 탐사의 전체 전투 아군 보관
    } // 광역 대상 연결 종료
    internal bool HitsTarget(BattleUnitRuntime target) // 예정 공격의 현재 대상 포함 여부
    { // 공격 대상 조회 시작
        return target != null && !target.IsDead && (IsAreaAttack ? areaTargets.Contains(target) : Target == target); // 단일 보호 지정 또는 광역 목록의 생존 대상 확인
    } // 공격 대상 조회 종료
    internal void SetDefensePreviewRule(Func<BattleEnemyAction, BattleUnitRuntime, (int Block, bool Preparation, bool Interception, bool Nullification)> rule) // 대상별 방어 예고 규칙 연결
    { // 방어 순서 규칙 연결 시작
        getPreviewDefense = rule; // 현재 공격 순서의 방어 확인 규칙 저장
    } // 방어 순서 규칙 연결 종료
    internal void SetBootPreviewRule(Func<BattleEnemyAction, BattleUnitRuntime, int> rule) // 대상별 장화 잔여 횟수 예고 연결
    { // 경감 예고 연결 시작
        getBootReduction = rule; // 현재 파티의 이번 턴 경감 기회 조회
    } // 경감 예고 연결 종료
    internal void SetBodyGuardPreviewRule(Func<BattleEnemyAction, BattleUnitRuntime> rule) // 대신 받기 대상 예고 연결
    { // 보호 예고 연결 시작
        getBodyGuardPreviewTarget = rule; // 대신 받을 대상 조회 규칙 저장
    } // 보호 예고 연결 종료
    public BattleDamageResult PreviewDamage() // 예정 피해 계산
    { // 예정 피해 계산 시작
        return PreviewDamage(Target); // 기존 대표 대상의 피해 예고 반환
    } // 예정 피해 계산 종료
    public BattleDamageResult PreviewDamage(BattleUnitRuntime target) // 광역과 단일 대상의 개별 예상 피해 계산
    { // 개별 피해 예고 시작
        var preview = PreviewWithDefense(target); // 공격 순서별 방어와 일회성 상태 반영
        int primary = GetPrimaryHealthDamage(preview); // 분담 뒤 이 대상에 남는 체력 피해
        int reduction = primary > 0 && getBootReduction != null ? getBootReduction(this, target) : 0; // 대상 순서의 장화 경감량 조회
        return preview.WithAppliedDamage(Math.Min(target?.CurrentHealth ?? 0, Math.Max(0, primary - reduction))); // 대상 체력과 장화를 반영한 예고 반환
    } // 개별 피해 예고 종료
    public int PreviewSharedDamage() // 분담자가 받을 실제 피해 예고
    { // 분담 피해 예고 시작
        if (IsAreaAttack || Target == null || Target.IsDead || SharedDamageRecipient == null || SharedDamageRecipient.IsDead || SharedDamageRecipient.GetStatusEffect(BattleStatusEffectType.DamageNullification) != null) // 단일 공격의 생존 분담자와 피해 보호 확인
        { // 분담 피해 없음 처리 시작
            return 0; // 분담 피해 없음 반환
        } // 분담 피해 없음 처리 종료
        BattleDamageResult preview = PreviewWithDefense(Target); // 분담 전 대상의 순서별 방어 적용 피해 계산
        int remaining = Math.Max(0, preview.FinalDamage - preview.AbsorbedDamage); // 방어 적용 후 남은 피해 계산
        return Math.Min(SharedDamageRecipient.CurrentHealth, remaining / 2); // 분담자의 남은 체력을 반영한 피해 반환
    } // 분담 피해 예고 종료
    internal BattleDamageResult PreviewWithDefense(BattleUnitRuntime target) // 장화 이전의 대상별 예상 피해 계산
    { // 분담 이전 예고 시작
        if (!HitsTarget(target)) // 살아 있는 실제 지정 대상 여부 확인
        { // 잘못된 대상 예고 시작
            return BattleDamageResult.Empty(DamageType); // 현재 피격 대상이 아닌 유닛의 빈 피해
        } // 잘못된 대상 예고 종료
        var (block, preparation, interception, nullification) = getPreviewDefense?.Invoke(this, target) ?? (target.CurrentBlock, true, true, true); // 공격 직전 순서별 예상 방어 상태
        return PreviewDamageBeforeShare(target, block, preparation, interception, nullification); // 순서별 보호와 광역 의도를 반영한 예고
    } // 분담 이전 예고 종료
    internal int GetPrimaryHealthDamage(BattleDamageResult preview) // 방어도와 분담 뒤 주 대상에게 남은 피해
    { // 주 대상 체력 피해 계산 시작
        int remaining = Math.Max(0, preview.FinalDamage - preview.AbsorbedDamage); // 방어도 흡수 뒤 남은 피해
        return !IsAreaAttack && SharedDamageRecipient != null && !SharedDamageRecipient.IsDead ? remaining - remaining / 2 : remaining; // 단일 분담 시 홀수 한 점은 원래 대상 몫
    } // 분담을 포함한 피해 예고 종료
    internal BattleDamageResult PreviewDamageBeforeShare(BattleUnitRuntime target, int block, bool preparation, bool interception, bool nullification) // 순서별 방어 상태의 분담 전 예정 피해 계산
    { // 분담 전 피해 예고 시작
        if (BlockingStatus != BattleStatusEffectType.None || !HitsTarget(target)) // 행동 차단이나 대상 변경 상태 확인
        {
            return BattleDamageResult.Empty(
                DamageType); // 피해 없음 결과 반환
        }

        int modifiedAmount = AttackAmount; // 실제 공격 약화와 정신 상태를 반영한 피해
        return target.PreviewDamage( // 현재 대상의 예상 피해 계산
            modifiedAmount, // 정신 상태 포함 피해량 전달
            DamageType, // 공격 피해 유형 전달
            preparation ? Actor : null, // 아직 남은 방어 준비만 포함
            block, enemyDirectAttack: true, applyDangerInterception: interception, enemyAreaAttack: IsAreaAttack, applyDamageNullification: nullification); // 광역 의도와 남은 일회성 보호 반영
    }

    public BattleEnemyActionResult Execute(Func<bool> preventHealthDamage = null, Func<int, int> reduceHealthDamage = null) // 예정 행동 실행
    { // 일반 실행 시작
        return Execute(preventHealthDamage, reduceHealthDamage, null, null); // 기존 단독 실행 경로 연결
    } // 일반 실행 종료

    internal BattleEnemyActionResult Execute(Func<bool> preventHealthDamage, Func<int, int> reduceHealthDamage, BattleUnitRuntime guard, Action consumeBodyGuard) // 대신 받기를 포함한 행동 실행
    { // 보호 포함 실행 시작
        areaHitResults.Clear(); // 새 행동의 광역 실행 기록 초기화
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
        BattleUnitRuntime prioritizedTarget = PrioritizedTarget; // 명중 판정 전에 우선 공격 대상 저장
        Actor.RemoveStatusEffect(BattleStatusEffectType.NextAttackDown); // 적이 실제 공격을 시도하면 명중과 관계없이 한 번 소모
        if (IsAreaAttack) // 명시적으로 전체 생존자를 향한 공격 확인
        { // 광역 공격 시작
            foreach (BattleUnitRuntime target in areaTargets) // 현재 배치 순서의 모든 아군 확인
            { // 광역 대상 확인 시작
                if (target != null && !target.IsDead) // 각 피격 시점의 실제 생존 대상 확인
                { // 개별 광역 타격 시작
                    BattleEnemyActionResult hit = ExecuteHit(target, attackDamage, preventHealthDamage, reduceHealthDamage, null, out BattleUnitRuntime recipient); // 명중 뒤 수호 전환을 포함한 광역 개별 결과
                    areaHitResults.Add((recipient, hit)); // 실제로 피해와 후속 효과를 받은 아군 기록
                } // 개별 광역 타격 종료
            } // 광역 대상 확인 종료
            return areaHitResults.Count > 0 ? areaHitResults[0].Result : BattleEnemyActionResult.Empty(ActionType, DamageType); // 기존 호출용 대표 광역 결과
        } // 광역 공격 종료
        BattleUnitRuntime attackTarget = attackDamage > 0 && guard != null && !guard.IsDead ? guard : prioritizedTarget; // 도발 또는 보호 상태의 최종 대상 선택
        executedAttackTarget = attackTarget; // 연출과 결과 표시용 피격 대상 고정
        return ExecuteHit(attackTarget, attackDamage, preventHealthDamage, reduceHealthDamage, attackTarget != prioritizedTarget ? consumeBodyGuard : null, out _); // 단일 공격의 실제 피격 처리
    } // 보호 포함 실행 종료
    private BattleEnemyActionResult ExecuteHit(BattleUnitRuntime attackTarget, int attackDamage, Func<bool> preventHealthDamage, Func<int, int> reduceHealthDamage, Action consumeBodyGuard, out BattleUnitRuntime recipient) // 개별 명중과 실제 피격 대상의 피해 처리
    { // 개별 피격 처리 시작
        recipient = attackTarget; // 빗나가거나 전환되지 않은 공격의 대상 기록
        int hitChance = BattleHitChance.GetPercent(Actor, attackTarget); // 실제 대상 회피를 포함한 명중률
        if (hitChance < 100 && !BattleHitChance.Hits(Actor, rollPercent(), attackTarget)) // 실제 대상에 대한 명중 판정
        { // 공격 빗나감 처리 시작
            return BattleEnemyActionResult.Missed(DamageType); // 피해와 상태가 없는 빗나감 반환
        } // 공격 빗나감 처리 종료

        consumeBodyGuard?.Invoke(); // 대신 받기로 실제로 명중한 경우 보호 한 번 소모
        BattleUnitRuntime finalTarget = attackTarget; // 수호 전환이 없을 때의 실제 피격 대상
        void RecordRedirect(BattleUnitRuntime protector) // 치명 피해를 대신 받는 대상 기록
        { // 명중 후 대상 전환 시작
            finalTarget = protector; // 새로 피해와 후속 효과를 받는 수호자 기록
            if (!IsAreaAttack) // 단일 행동의 실제 대상 갱신이 필요한지 확인
            { // 단일 피격 대상 기록 시작
                executedAttackTarget = protector; // 피해 이벤트와 후속 연출에 실제 대상 전달
            } // 단일 피격 대상 기록 종료
        } // 명중 후 대상 전환 종료
        BattleDamageResult damageResult = attackTarget.TakeEnemyHit(attackDamage, DamageType, Actor, IsAreaAttack ? null : SharedDamageRecipient, preventHealthDamage, reduceHealthDamage, IsAreaAttack, RecordRedirect); // 실제 치명 피해에서만 수호 전환 처리
        attackTarget = finalTarget; // 추가 상태와 응징의 실제 피격 대상 복원
        recipient = finalTarget; // 광역 공격 결과에도 수호 전환된 대상 전달

        bool applyAdditionalStatus = !attackTarget.IsDead && StatusEffectType != BattleStatusEffectType.None && statusChancePercent > 0 && rollPercent() < statusChancePercent; // 실제 피격 대상의 추가 상태 판정
        BattleStatusEffectApplyResult statusResult = applyAdditionalStatus ? attackTarget.ApplyStatusEffect(StatusEffectType, statusValue, StatusDuration, StatusMaximumStacks, Actor) : BattleStatusEffectApplyResult.Invalid; // 실제 피격 대상의 추가 상태 적용
        if (!attackTarget.IsDead && AdditionalMentalChange != 0) // 실제 피격 대상의 정신력 변화 확인
        { // 정신력 압박 적용 시작
            attackTarget.ChangeMental(AdditionalMentalChange, BattleMentalChangeReason.DisruptReceived); // 실제 피격 대상의 정신력 변화 적용
        } // 정신력 압박 적용 종료
        var retaliation = attackTarget.GetStatusEffect(BattleStatusEffectType.RetributionBulwark); // 원래 타격의 실제 피격 아군에 남은 응징 방벽
        if (retaliation != null && !Actor.IsDead && Actor.Team == BattleTeam.Enemy && !attackTarget.IsDead && attackTarget.Team == BattleTeam.Ally && !damageResult.WasDamageNullified && (damageResult.AppliedDamage > 0 || damageResult.AbsorbedDamage > 0)) // 무효와 비명중을 제외한 실제 방어도 또는 체력 피해 확인
        { // 응징 방벽 반격 시작
            Actor.TakeUnblockedDamage(retaliation.EffectiveValue, BattleDamageType.Physical, attackTarget); // 원래 공격 정산 뒤 방어 계산 없는 고정 물리 반격
        } // 응징 방벽 반격 종료
        return BattleEnemyActionResult.FromDamage(damageResult, statusResult, applyAdditionalStatus); // 피해와 추가 상태 결과 반환
    } // 개별 피격 처리 종료

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
