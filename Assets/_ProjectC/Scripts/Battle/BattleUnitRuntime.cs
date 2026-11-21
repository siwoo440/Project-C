using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 자료형 사용
public sealed class BattleUnitRuntime // 전투 유닛 런타임 상태
{ // 클래스 시작
    private BattleUnitRuntime guardianHeartProtector; // 이 아군의 치명 피해를 대신 받을 시전자
    private readonly List<BattleStatusEffectInstance> statusEffects = new List<BattleStatusEffectInstance>(); // 현재 상태 이상 목록
    private BattleStatusEffectInstance currentTurnAttackPower; // 장기 강화와 구분할 아군 한 턴의 공격력 상태
    private bool preserveReportedStatusDuration; // 상태 알림의 원래 지속 시간 보존 여부
    private int pendingSeedRevivalPercent; // 전투불능 신호가 끝난 뒤 소비할 실제 부활 체력 비율
    private readonly BattleMentalRuntime mentalRuntime; // 개별 정신력 런타임
    private readonly int awakeningDamagePercent; // 각성 피해 변화율
    private readonly int awakeningHealingPercent; // 각성 회복 변화율
    private readonly int collapseDamagePercent; // 붕괴 피해 변화율
    private readonly int collapseHealingPercent; // 붕괴 회복 변화율
    public string UnitId { get; } // 유닛 고유 ID
    public string DisplayName { get; } // 유닛 표시 이름
    internal BattleTurnRuntime ExplorationAttackPowerBattle { get; set; } // 현재 출전한 탐사 전투의 영구 공격력 원본
    public BattleTeam Team { get; } // 유닛 진영
    public Sprite Portrait { get; } // 유닛 초상화
    public int MaxHealth { get; private set; } // 유물 효과 적용 가능한 최대 체력
    public int BasePhysicalDefense { get; } // 기본 물리 방어력
    public int BaseMagicalResistance { get; } // 기본 마법 저항력
    public int PhysicalDefense => CalculateDefense(BasePhysicalDefense, BattleStatusEffectType.PhysicalDefenseUp, BattleStatusEffectType.PhysicalDefenseDown); // 현재 물리 방어력
    public int MagicalResistance => CalculateDefense(BaseMagicalResistance, BattleStatusEffectType.MagicalResistanceUp, BattleStatusEffectType.MagicalResistanceDown); // 현재 마법 저항력
    private bool hasEnemyDamageWindow; // 첫 적 턴의 시작 여부
    private int queuedChargeDefense; // 다음 아군 턴의 방어 감소량
    private BattleUnitRuntime queuedChargeSource; // 돌진을 사용한 아군
    public int HealthLostSinceEnemyTurn { get; private set; } // 직전 적 턴부터 누적한 실제 체력 손실
    internal bool HasLostHealthToCorrosion { get; private set; } // 침식으로 실제 체력을 잃은 기록
    public int CurrentHealth { get; private set; } // 현재 체력
    public int CurrentBlock { get; private set; } // 현재 피해 흡수 방어도
    public bool IsDead { get; private set; } // 사망 여부
    public IReadOnlyList<BattleStatusEffectInstance> StatusEffects => statusEffects; // 현재 상태 이상 조회
    public int AttackPowerBonus => (int)Math.Min(int.MaxValue, (long)CalculateAttackPowerBonus() + NamelessOathAttackPower); // 상태 강화와 전투 서약을 더한 일반 공격력 증가
    public int ActionSpeedModifier => GetStatusEffectValue(BattleStatusEffectType.ActionSpeedUp) - GetStatusEffectValue(BattleStatusEffectType.ActionSpeedDown); // 행동 속도 증가와 감소의 차이
    public bool HasDebuff => ContainsDebuff(); // 현재 디버프 존재 여부
    public bool HasRemovableDebuff => ContainsRemovableDebuff(); // 정화 가능한 디버프 존재 여부
    internal bool HasActiveRemovableDebuff => ContainsRemovableDebuff(activeOnly: true); // 현재 만료되지 않은 정화 가능 디버프 확인
    internal bool HasActiveCardDamageMark => Team == BattleTeam.Enemy && (GetStatusEffect(BattleStatusEffectType.StethoscopeMark) is { IsExpired: false } || // 현재 활성 청진기 표식과 아래의 기존 카드 피해 표식 확인
        GetStatusEffect(BattleStatusEffectType.PublicMark) is { IsExpired: false } || GetStatusEffect(BattleStatusEffectType.ChariotMark) is { IsExpired: false }); // 실제 공명 및 전차 표식의 활성 여부
    public bool HasStatusImmunity => FindStatusEffect(BattleStatusEffectType.StatusImmunity) != null; // 현재 디버프 면역 여부
    public int CurrentMental => mentalRuntime.CurrentMental; // 현재 정신력
    public BattleMentalState MentalState => mentalRuntime.CurrentState; // 현재 정신 상태
    public int MentalRemainingTurns => mentalRuntime.RemainingTurns; // 특수 상태 남은 턴
    public CharacterData CharacterSource { get; } // 아군 원본 데이터
    public EnemyData EnemySource { get; } // 적 원본 데이터
    public event Action<BattleUnitRuntime> HealthChanged; // 체력 변경 이벤트
    public event Action<BattleUnitRuntime> BlockChanged; // 방어도 변경 이벤트
    internal int NamelessOathAttackPower { get; set; } // 현재 전투에서만 유지할 서약의 기본 공격력 증가
    internal Func<int> GetHostileBlindResistance { get; set; } // 현재 전투가 제공하는 첫 적 턴 실명 저항 조회
    public event Action<BattleUnitRuntime, BattleDamageResult> DamageTaken; // 피해 적용 이벤트
    internal event Action<BattleUnitRuntime> StealthEnded; // 살아 있는 유닛의 은신 해제 또는 정상 만료 이벤트
    public event Action<BattleUnitRuntime, int> HealthRestored; // 회복 적용 이벤트
    internal event Action<BattleUnitRuntime> Revived; // 전투불능 아군의 부활 알림
    internal event Action<BattleUnitRuntime> AllyDefeatReady; // 사망 알림을 마친 아군의 유물 부활 정산
    internal event Action<BattleUnitRuntime> SeedRevivalSettled; // 씨앗으로 인한 실제 전투불능과 부활의 결산 완료
    internal bool HasPendingSeedRevival => pendingSeedRevivalPercent > 0; // 패배나 승리를 잠시 미뤄야 할 씨앗 부활 여부
    internal bool DefeatedWhileBurning { get; private set; } // 일반 상태 정리 전 실제 쓰러질 때의 활성 화상
    internal BattleDuelRuntime ActiveDuel { get; set; } // 해당 유닛이 참여 중인 결투
    public event Action<BattleUnitRuntime> Died; // 사망 이벤트
    public event Action<BattleUnitRuntime> StatusEffectsChanged; // 상태 이상 변경 이벤트
    public event Action<BattleUnitRuntime, BattleMentalChangeResult> MentalChanged; // 정신력 변화 이벤트
    internal Func<BattleMentalChangeReason, bool> PreventIncomingMentalLoss { get; set; } // 실제 전투가 제공하는 공용 정신 피해 보호
    public event Action<BattleUnitRuntime, BattleUnitRuntime, BattleDamageResult> DamageResolved; // 발생자 포함 피해 완료 이벤트
    public event Action<BattleUnitRuntime, BattleUnitRuntime, int> HealingResolved; // 발생자 포함 회복 완료 이벤트
    public event Action<BattleUnitRuntime, BattleUnitRuntime, BattleStatusEffectType, BattleStatusEffectApplyResult> StatusEffectResolved; // 발생자 포함 상태 적용 이벤트
    public event Action<BattleUnitRuntime, BattleUnitRuntime> Defeated; // 발생자 포함 처치 완료 이벤트
    private BattleUnitRuntime(string unitId, string displayName, BattleTeam team, Sprite portrait, int maxHealth, int physicalDefense, int magicalResistance, int initialMental, int awakeningDamageRate, int awakeningHealingRate, int collapseDamageRate, int collapseHealingRate, CharacterData characterSource, EnemyData enemySource) // 런타임 상태 생성자
    { // 생성자 시작
        UnitId = unitId; // 유닛 ID 저장
        DisplayName = displayName; // 표시 이름 저장
        Team = team; // 진영 저장
        Portrait = portrait; // 초상화 저장
        MaxHealth = Mathf.Max(1, maxHealth); // 최소 최대 체력 보정
        BasePhysicalDefense = Mathf.Max(0, physicalDefense); // 기본 물리 방어력 보정
        BaseMagicalResistance = Mathf.Max(0, magicalResistance); // 기본 마법 저항력 보정
        mentalRuntime = new BattleMentalRuntime(initialMental); // 정신력 런타임 생성
        mentalRuntime.Changed += HandleMentalChanged; // 정신력 변화 이벤트 등록
        awakeningDamagePercent = Mathf.Clamp(awakeningDamageRate, -100, 100); // 각성 피해 변화율 보정
        awakeningHealingPercent = Mathf.Clamp(awakeningHealingRate, -100, 100); // 각성 회복 변화율 보정
        collapseDamagePercent = Mathf.Clamp(collapseDamageRate, -100, 100); // 붕괴 피해 변화율 보정
        collapseHealingPercent = Mathf.Clamp(collapseHealingRate, -100, 100); // 붕괴 회복 변화율 보정
        CurrentHealth = MaxHealth; // 현재 체력 초기화
        IsDead = false; // 생존 상태 초기화
        CharacterSource = characterSource; // 아군 원본 저장
        EnemySource = enemySource; // 적 원본 저장
        SyncMentalStatus(); // 초기 정신 상태를 상태 이상 목록에 반영
    } // 생성자 종료
    public static BattleUnitRuntime CreateAlly(CharacterData characterData) // 아군 런타임 생성
    { // 아군 생성 시작
        if (characterData == null) // 아군 원본 누락 확인
        { // 누락 처리 시작
            throw new ArgumentNullException(nameof(characterData)); // 잘못된 인수 예외
        } // 누락 처리 종료
        return new BattleUnitRuntime(characterData.CharacterId, characterData.DisplayName, BattleTeam.Ally, characterData.Portrait, characterData.MaxHealth, characterData.PhysicalDefense, characterData.MagicalResistance, characterData.InitialMental, characterData.AwakeningDamagePercent, characterData.AwakeningHealingPercent, characterData.CollapseDamagePercent, characterData.CollapseHealingPercent, characterData, null); // 아군 상태 반환
    } // 아군 생성 종료
    public static BattleUnitRuntime CreateEnemy(EnemyData enemyData) // 적 런타임 생성
    { // 적 생성 시작
        if (enemyData == null) // 적 원본 누락 확인
        { // 누락 처리 시작
            throw new ArgumentNullException(nameof(enemyData)); // 잘못된 인수 예외
        } // 누락 처리 종료
        return new BattleUnitRuntime(enemyData.EnemyId, enemyData.DisplayName, BattleTeam.Enemy, enemyData.Portrait, enemyData.MaxHealth, enemyData.PhysicalDefense, enemyData.MagicalResistance, enemyData.InitialMental, enemyData.AwakeningDamagePercent, enemyData.AwakeningHealingPercent, enemyData.CollapseDamagePercent, enemyData.CollapseHealingPercent, null, enemyData); // 적 상태 반환
    } // 적 생성 종료
    public bool ApplyPersistentHealth(int currentHealth) // Scene 간 저장 체력 적용
    { // 저장 체력 적용 시작
        if (Team != BattleTeam.Ally) // 아군 여부 확인
        { // 적용 불가 처리 시작
            return false; // 저장 체력 적용 실패 반환
        } // 적용 불가 처리 종료
        CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth); // 저장 현재 체력 범위 적용
        IsDead = CurrentHealth <= 0; // 저장 체력 기준 사망 상태 적용
        return true; // 저장 체력 적용 성공 반환
    } // 저장 체력 적용 종료
    internal int Revive(int healthPercent) // 사망 아군의 전투 체력 복구
    { // 부활 처리 시작
        if (Team != BattleTeam.Ally || !IsDead || healthPercent <= 0) // 부활 가능한 전투불능 아군과 체력 비율 확인
        { // 부활 불가 처리 시작
            return 0; // 변경 체력 없음 반환
        } // 부활 불가 처리 종료
        int revivedHealth = (int)(((long)MaxHealth * Math.Min(healthPercent, 100) + 99) / 100); // 최대 체력 비율의 소수점 올림
        CurrentHealth = revivedHealth; // 부활 체력 적용
        IsDead = false; // 실제 전투 재참여 상태 적용
        queuedChargeDefense = 0; // 사망 전 예약한 돌진 방어 감소 제거
        queuedChargeSource = null; // 사망 전 예약한 돌진 시전자 연결 제거
        HealthLostSinceEnemyTurn = 0; // 사망 전 피해의 반격 누적 제거
        ClearBlock(); // 사망 전에 남은 방어도 제거
        HealthChanged?.Invoke(this); // 체력과 사망 표시 갱신
        Revived?.Invoke(this); // 부활 전용 표시와 광역 예고 갱신
        return revivedHealth; // 실제 부활 체력 반환
    } // 부활 처리 종료
    public int ModifyMaxHealth(int delta) // 유물 효과용 최대 체력 변경
    {
        if (delta == 0) // 최대 체력 변화량 확인
        {
            return 0; // 변화 없음 반환
        }

        int previousMaxHealth = MaxHealth; // 변경 전 최대 체력 저장
        MaxHealth = Mathf.Max(1, MaxHealth + delta); // 최소 일 이상 최대 체력 변경
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth); // 감소 시 현재 체력 상한 보정
        int appliedDelta = MaxHealth - previousMaxHealth; // 실제 최대 체력 변화량 계산
        if (appliedDelta != 0) // 실제 변화 여부 확인
        {
            HealthChanged?.Invoke(this); // 최대 체력 포함 UI 갱신 알림
        }
        return appliedDelta; // 실제 최대 체력 변화량 반환
    }
    public bool ApplyPersistentMental(int currentMental) // Scene 간 저장 정신력 적용
    { // 저장 정신력 적용 시작
        if (Team != BattleTeam.Ally) // 아군 여부 확인
        { // 적용 불가 처리 시작
            return false; // 저장 정신력 적용 실패 반환
        } // 적용 불가 처리 종료
        return mentalRuntime.ApplyPersistentMental(currentMental); // 저장 정신력 적용 결과 반환
    } // 저장 정신력 적용 종료
    public BattleMentalChangeResult ChangeMental(int delta, BattleMentalChangeReason reason, BattleUnitRuntime sourceUnit = null) // 발생 유닛을 포함한 실제 정신력 증감 적용
    { // 정신력 증감 시작
        if (IsDead) // 사망 상태 확인
        { // 사망 상태 처리 시작
            return BattleMentalChangeResult.NoChange(CurrentMental, MentalState, MentalRemainingTurns, delta, reason); // 변화 없음 반환
        } // 사망 상태 처리 종료
        return mentalRuntime.ChangeMental(GetEncounterMentalChange(delta), reason, delta, sourceUnit, PreventIncomingMentalLoss); // 교전 보정과 원래 요청 및 공용 피해 보호 전달
    } // 정신력 증감 종료
    internal void SpendRelicMentalCost(int amount) // 전투 개막 유물의 명시 정신력 비용
    { // 직접 비용 지불 시작
        if (Team == BattleTeam.Ally && !IsDead && amount > 0) // 생존 아군과 양수인 실제 비용 확인
        { // 현재 정신력에서 정수 비용 처리 시작
            mentalRuntime.ChangeMental(-amount, BattleMentalChangeReason.RelicEffect); // 피해 방어와 감경을 건너뛰는 원래 정신력 범위 및 상태 처리
        } // 현재 정신력에서 정수 비용 처리 종료
    } // 직접 비용 지불 종료
    public bool CanChangeMental(int delta) // 정신력 변경 가능 여부
    { // 변경 가능 확인 시작
        return !IsDead && mentalRuntime.CanChangeMental(GetEncounterMentalChange(delta)); // 실제 정신력 감소 완화를 포함한 카드 사용 가능 여부
    } // 변경 가능 확인 종료
    public int PreviewMentalChange(int delta) // 현재 정신력과 퇴색 교전 완화를 반영한 변화량 예고
    { // 정신력 변화 예고 시작
        return IsDead ? 0 : mentalRuntime.PreviewDelta(GetEncounterMentalChange(delta)); // 이미 사망한 대상을 제외한 실제 변화 예고
    } // 정신력 변화 예고 종료
    private int GetEncounterMentalChange(int delta) // 실제 아군이 받는 퇴색 교전 정신력 손실 보정
    { // 퇴색 교전 정신력 확인 시작
        return delta < 0 && Team == BattleTeam.Ally && ExplorationSessionManager.Instance?.ActiveEncounterInFade == true ? ExplorationRelicEffects.AdjustFadeMentalChange(delta) : delta; // 퇴색 교전 아군의 음수 변화에만 유물 적용
    } // 퇴색 교전 정신력 확인 종료
    public BattleMentalChangeResult AdvanceMentalStateTurn() // 정신 특수 상태 턴 진행
    { // 상태 턴 진행 시작
        return mentalRuntime.AdvanceStateTurn(); // 상태 턴 진행 결과 반환
    } // 상태 턴 진행 종료
    public BattleMentalChangeResult ResolveMentalStateAtBattleEnd() // 전투 종료 정신 상태 해제
    { // 전투 종료 해제 시작
        return mentalRuntime.ResolveAtBattleEnd(); // 정신 상태 해제 결과 반환
    } // 전투 종료 해제 종료
    public void ResetMentalTurnLimits() // 정신력 턴 제한 초기화
    { // 제한 초기화 시작
        mentalRuntime.ResetTurnLimits(); // 정신력 원인별 횟수 초기화
    } // 제한 초기화 종료
    public int ModifyOutgoingDamage(int baseDamage) // 정신 상태 기반 피해량 계산
    { // 피해량 계산 시작
        int changePercent = MentalState == BattleMentalState.Awakening ? awakeningDamagePercent : MentalState == BattleMentalState.Collapse ? collapseDamagePercent : 0; // 현재 상태 피해 변화율 선택
        return ApplyPercentage(baseDamage, changePercent); // 보정 피해량 반환
    } // 피해량 계산 종료
    internal int ModifyCardOutgoingDamage(int baseDamage, BattleDamageType damageType) // 실제 마법 카드 피해의 전용 공격력과 기존 정신 상태 보정
    { // 카드별 마법 공격력 처리 시작
        var amplification = damageType == BattleDamageType.Magical ? FindStatusEffect(BattleStatusEffectType.MagicAttackPowerUp) : null; // 마법 카드에 남아 있는 실제 증폭 상태만 확인
        int bonus = amplification is { IsExpired: false } ? amplification.EffectiveValue : 0; // 만료되지 않은 마법 전용 공격력의 정수 가산량
        int adjusted = (int)Math.Clamp((long)baseDamage + bonus, 0L, int.MaxValue); // 원래 공격에 더해도 기존 정수 최대값과 영 한계 보존
        return ModifyOutgoingDamage(adjusted); // 모든 카드의 기존 정신 상태 기반 피해 계산 재사용
    } // 카드별 마법 공격력 처리 종료
    public int ModifyOutgoingHealing(int baseHealing) // 정신 상태 기반 회복량 계산
    { // 회복량 계산 시작
        int changePercent = MentalState == BattleMentalState.Awakening ? awakeningHealingPercent : MentalState == BattleMentalState.Collapse ? collapseHealingPercent : 0; // 현재 상태 회복 변화율 선택
        return ApplyPercentage(baseHealing, changePercent); // 보정 회복량 반환
    } // 회복량 계산 종료
    public BattleStatusEffectApplyResult ApplyStatusEffect(BattleStatusEffectType effectType, int value, int duration, int maximumStacks, BattleUnitRuntime sourceUnit = null, bool corrosionFamily = false) // 명시 침식 계열을 포함한 상태 이상 적용
    { // 일반 상태 적용 시작
        BattleStatusEffectApplyResult result = ApplyStatusEffectCore(effectType, value, duration, maximumStacks, sourceUnit, null); // 기존 면역과 신규 및 중첩의 실제 적용 결과 확인
        if (corrosionFamily && Team == BattleTeam.Enemy && !IsDead && sourceUnit?.Team == BattleTeam.Ally && // 명시 침식 상태의 생존 적과 아군 부여자 확인
            BattleStatusEffectInstance.IsDebuffType(effectType) && result is (BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked)) // 실제로 성공한 약화 적용만 확인
        { // 맥동핵의 성공 침식 피해 처리 시작
            int damage = BattleSupplyRelics.Find(RelicEffectType.CorrosionDebuffDamage)?.EffectValue ?? 0; // 실제 보유한 맥동핵의 원본 피해량
            if (damage > 0) // 실제 가산 피해가 존재하는지 확인
            { // 침식 부가 피해 시작
                TakeDamage(damage, BattleDamageType.None, sourceUnit); // 직접 명중 유물을 거치지 않는 일반 피해와 방어도 처리
            } // 침식 부가 피해 종료
        } // 맥동핵의 성공 침식 피해 처리 종료
        return result; // 부모 효과에 기존 실제 상태 결과 그대로 반환
    } // 일반 상태 적용 종료
    internal BattleStatusEffectApplyResult ApplyReflectedDebuff(BattleStatusEffectInstance debuff, BattleUnitRuntime source) // 흐린 거울못의 실제 원래 약화 복사
    { // 반사 약화 적용 시작
        return ApplyStatusEffectCore(debuff.EffectType, debuff.Value, debuff.RemainingTurns, debuff.MaximumStacks, source, null, allowOutgoingBurnExtension: false); // 다른 유물의 화상 연장 없이 현재 값과 기간을 기존 상태 및 면역 규칙에 전달
    } // 반사 약화 적용 종료
    internal bool IsTransientTurnBuff(BattleStatusEffectInstance status) // 일반 강화와 독립인 현재 턴 전용 상태 확인
    { // 독립 강화 확인 시작
        return status == currentTurnAttackPower; // 종료 예약이 분리된 은총 상태 확인
    } // 독립 강화 확인 종료
    internal bool CanReceiveBlessingCopy(BattleStatusEffectInstance blessing) // 활성 강화의 수령 가능 중첩 확인
    { // 수령 중첩 확인 시작
        var existing = FindStatusEffect(blessing.EffectType, includeCurrentTurnAttackPower: false); // 수령 아군의 같은 일반 강화 상태 확인
        return existing == null || existing.IsExpired || (long)existing.StackCount + blessing.StackCount <= Math.Max(existing.MaximumStacks, blessing.MaximumStacks); // 만료 상태 교체와 손실 없는 중첩만 허용
    } // 수령 중첩 확인 종료
    internal BattleStatusEffectApplyResult ApplyBlessingCopy(BattleStatusEffectInstance blessing, BattleUnitRuntime caster) // 기존 상태 적용 경로로 독립 강화 복사
    { // 복사한 강화 적용 시작
        return ApplyStatusEffectCore(blessing.EffectType, blessing.Value, blessing.RemainingTurns, blessing.MaximumStacks, caster, blessing); // 원래 강화의 현재 시간과 수치 전달
    } // 복사한 강화 적용 종료
    private BattleStatusEffectApplyResult ApplyStatusEffectCore(BattleStatusEffectType effectType, int value, int duration, int maximumStacks, BattleUnitRuntime sourceUnit, BattleStatusEffectInstance blessing, bool allowOutgoingBurnExtension = true) // 일반 상태와 복사한 실제 상태의 공통 면역 및 부여
    { // 상태 이상 적용 시작
        if (IsDead || effectType == BattleStatusEffectType.None || effectType == BattleStatusEffectType.Awakening || effectType == BattleStatusEffectType.Collapse || value <= 0 || duration <= 0 || maximumStacks <= 0) // 정신력 전용 상태와 적용 조건 확인
        { // 적용 불가 처리 시작
            return BattleStatusEffectApplyResult.Invalid; // 잘못된 적용 결과 반환
        } // 적용 불가 처리 종료
        if (BattleStatusEffectInstance.IsDebuffType(effectType) && HasStatusImmunity) // 디버프 면역 확인
        { // 면역 차단 처리 시작
            return BattleStatusEffectApplyResult.BlockedByImmunity; // 면역 차단 결과 반환
        } // 면역 차단 처리 종료
        if (effectType == BattleStatusEffectType.Fear && Team == BattleTeam.Ally) // 정상 면역 이후 살아 있는 아군에게의 유효 공포 확인
        { // 찬가와 유물의 공포 저항 시작
            int resistance = Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.FearResistance)); // 고요한 찬가의 유효 공포 저항률
            int chance = BattleSupplyRelics.Find(RelicEffectType.CancelIncomingFear)?.TriggerChancePercent ?? 0; // 실제 보유 유물의 공포 취소 확률
            if ((resistance > 0 && (resistance == 100 || UnityEngine.Random.Range(0, 100) < resistance)) || // 먼저 적용되는 찬가의 독립 저항
                (chance > 0 && (chance == 100 || UnityEngine.Random.Range(0, 100) < chance))) // 공포가 남은 경우의 독립 유물 저항
            { // 이번 공포의 확률 취소 시작
                return BattleStatusEffectApplyResult.BlockedByImmunity; // 원래 상태 차단 피드백 경로 재사용
            } // 이번 공포의 확률 취소 종료
        } // 찬가와 유물의 공포 저항 종료
        if (effectType == BattleStatusEffectType.Bleed && Team == BattleTeam.Ally && sourceUnit?.Team == BattleTeam.Enemy) // 적이 아군에게 주는 유효 출혈 시도 확인
        { // 출혈 저항 판정 시작
            int resistance = Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.BleedResistance)); // 출혈 저항의 유효 확률 조회
            if (resistance > 0 && (resistance == 100 || UnityEngine.Random.Range(0, 100) < resistance)) // 저항 보유 시 확정 또는 확률 차단 확인
            { // 출혈 차단 처리 시작
                return BattleStatusEffectApplyResult.BlockedByImmunity; // 기존 상태 차단 결과로 출혈 저항 표시
            } // 출혈 차단 처리 종료
        } // 출혈 저항 판정 종료
        if (Team == BattleTeam.Ally && value == 2 && (effectType == BattleStatusEffectType.PhysicalDefenseDown || effectType == BattleStatusEffectType.ChargeDefenseDown)) // 보정하기 전 아군의 물리 방어 두 점 감소 확인
        { // 원본 방어 감소 유물 처리 시작
            if (BattleSupplyRelics.Find(RelicEffectType.SoftenDefenseLoss) != null) // 보유 금속판의 감소 수치 완화 확인
            { // 금속판 완화 시작
                value = 1; // 새로 부여되는 약화 한 점으로 완화
            } // 금속판 완화 종료
            int emblemReduction = BattleSupplyRelics.Find(RelicEffectType.ShortenTwoPointDefenseLoss)?.EffectValue ?? 0; // 원래 두 점 약화의 문양 지속 감소량
            duration = Math.Max(1, duration - emblemReduction); // 방독면 보정과 독립인 약화 지속 감소
        } // 원본 방어 감소 유물 처리 종료
        if (Team == BattleTeam.Ally && duration > 1 && IsStandardTimedDebuff(effectType) && ExplorationSessionManager.Instance?.ActiveEncounterInFade == true) // 퇴색 교전에서 받는 아군의 지속 디버프 확인
        { // 방독면 지속 턴 보정 시작
            int reduction = BattleSupplyRelics.Find(RelicEffectType.ShorterFadeDebuffs)?.EffectValue ?? 0; // 실제 보유 방독면의 지속 감소량
            duration = Mathf.Max(1, duration - reduction); // 최소 한 턴을 보장한 이번 적용 지속
        } // 방독면 지속 턴 보정 종료
        if (allowOutgoingBurnExtension && effectType == BattleStatusEffectType.Burn && Team == BattleTeam.Enemy && sourceUnit?.Team == BattleTeam.Ally && duration < int.MaxValue) // 일반 아군 화상 부여와 연장 여유 확인
        { // 젖은 성냥개비의 추가 지속 판정 시작
            var matchstick = BattleSupplyRelics.Find(RelicEffectType.ExtendAllyAppliedBurn); // 보유한 화상 유물 원본 조회
            int addedTurns = matchstick?.EffectValue ?? 0; // 성공 시 이번 원본 화상의 연장량
            int chance = matchstick?.TriggerChancePercent ?? 0; // 유물의 실제 화상 연장 확률
            if (addedTurns > 0 && chance > 0 && (chance == 100 || UnityEngine.Random.Range(0, 100) < chance)) // 적용 가능한 화상만 별도 확률 확인
            { // 추가 화상 턴 적용 시작
                duration = (int)Math.Min(int.MaxValue, (long)duration + addedTurns); // 정수 상한과 상태 적용 원본 기간 유지
            } // 추가 화상 턴 적용 종료
        } // 젖은 성냥개비의 추가 지속 판정 종료
        BattleStatusEffectInstance existingEffect = FindStatusEffect(effectType, includeCurrentTurnAttackPower: false); // 한 턴 은총을 제외한 기존 상태 조회
        if (blessing != null && existingEffect?.IsExpired == true) // 아직 목록에 남아 있는 만료 효과 확인
        { // 만료된 효과 교체 시작
            statusEffects.Remove(existingEffect); // 만료 효과 중첩을 복사할 수치에서 제외
            existingEffect = null; // 복사한 상태를 새 독립 강화로 적용
        } // 만료된 효과 교체 종료
        if (existingEffect == null) // 신규 상태 확인
        { // 신규 상태 처리 시작
            var status = blessing?.CopyBlessingState() ?? new BattleStatusEffectInstance(effectType, value, duration, maximumStacks); // 복사한 독립 상태 또는 일반 신규 상태 생성
            statusEffects.Add(status); // 실제 첫 약화 또는 강화 등록
            return ResolveAppliedStatus(status, sourceUnit, BattleStatusEffectApplyResult.Applied, blessing != null); // 신규 등록 뒤 상태 정화 및 적용 결과 전달
        } // 신규 상태 처리 종료
        if (blessing == null) // 일반 상태 갱신과 강화 복사 구분
        { // 일반 상태 갱신 시작
            existingEffect.Refresh(value, duration, maximumStacks); // 일반 중첩과 지속 시간 갱신
        } // 일반 상태 갱신 종료
        else // 이미 있는 강화에 실제 중첩 복사 확인
        { // 복사 중첩 합산 시작
            existingEffect.MergeBlessingCopy(blessing); // 원본을 지키면서 수령자 쪽 중첩과 기존 지속 병합
        } // 복사 중첩 합산 종료
        return ResolveAppliedStatus(existingEffect, sourceUnit, BattleStatusEffectApplyResult.Stacked, blessing != null); // 중첩 갱신 뒤 상태 정화 및 적용 결과 전달
    } // 상태 이상 적용 종료
    private BattleStatusEffectApplyResult ResolveAppliedStatus(BattleStatusEffectInstance status, BattleUnitRuntime source, BattleStatusEffectApplyResult result, bool preserveDuration) // 기존 면역을 통과한 신규 또는 중첩 상태의 최종 정산
    { // 성공한 상태 정산 시작
        if (TryCleanseReceivedDebuff(status)) // 실제 아군에게 새로 들어온 일반 약화의 즉시 정화 확인
        { // 봉인핵의 기존 상태 전체 정화 시작
            result = BattleStatusEffectApplyResult.CleansedByRelic; // 화면에서 정화와 원래 면역을 구분할 결과
        } // 봉인핵의 기존 상태 전체 정화 종료
        StatusEffectsChanged?.Invoke(this); // 정화 또는 활성 상태의 최종 목록 알림
        ReportStatusEffectResolution(source, status.EffectType, result, preserveDuration); // 최종 상태와 지속 처리 상태의 공유 알림
        ApplyDisruptMental(status.EffectType, source); // 정상 부여된 공포와 교란의 원래 즉시 정신 변화 보존
        return result; // 신규 또는 중첩과 즉시 정화의 실제 결과 반환
    } // 성공한 상태 정산 종료
    internal BattleStatusEffectApplyResult ApplyCurrentAllyTurnAttackPower(int amount, BattleUnitRuntime source) // 장기 강화와 독립된 이번 아군 턴 공격력 적용
    { // 현재 턴 공격력 처리 시작
        if (IsDead || Team != BattleTeam.Ally || amount <= 0) // 살아 있는 아군과 실제 공격력 증가 확인
        { // 강화 적용 불가 시작
            return BattleStatusEffectApplyResult.Invalid; // 무효한 이번 턴 강화 반환
        } // 강화 적용 불가 종료
        bool refreshing = currentTurnAttackPower != null; // 이번 턴의 은총이 이미 남아 있는지 확인
        if (refreshing) // 이번 턴 강화의 중복 요청 확인
        { // 기존 은총 갱신 시작
            currentTurnAttackPower.Refresh(amount, 1, 1); // 같은 한 턴 상태의 수치만 갱신
        } // 기존 은총 갱신 종료
        else // 처음 부여하는 이번 턴 강화 확인
        { // 한 턴 강화 생성 시작
            currentTurnAttackPower = new BattleStatusEffectInstance(BattleStatusEffectType.AttackPowerUp, amount, 1, 1); // 장기 강화와 합쳐지지 않는 공격력 증가 생성
            statusEffects.Add(currentTurnAttackPower); // 일반 공격력 합계와 상태 화면에 한 턴 효과 추가
        } // 한 턴 강화 생성 종료
        var result = refreshing ? BattleStatusEffectApplyResult.Stacked : BattleStatusEffectApplyResult.Applied; // 일반 상태 적용과 같은 결과 표기
        StatusEffectsChanged?.Invoke(this); // 실제 공격력 수치와 상태 화면의 변경 알림
        ReportStatusEffectResolution(source, BattleStatusEffectType.AttackPowerUp, result, preserveDuration: true); // 이번 턴 강화의 기존 수명을 보존하는 상태 적용 이벤트
        return result; // 실제 한 턴 공격력 적용 결과 반환
    } // 현재 턴 공격력 처리 종료
    private void ReportStatusEffectResolution(BattleUnitRuntime source, BattleStatusEffectType type, BattleStatusEffectApplyResult result, bool preserveDuration = false) // 중첩과 지속 시간을 포함한 실제 상태 적용 신호 전달
    { // 상태 알림 범위 지정 시작
        bool previous = preserveReportedStatusDuration; // 겹친 상태 적용 알림의 이전 지속 시간 구분 보관
        preserveReportedStatusDuration = preserveDuration; // 이번 알림의 신규 지속 예약 여부 기록
        try // 상태 적용 알림 범위 복원 보장
        { // 한 턴 상태 알림 시작
            StatusEffectResolved?.Invoke(source, this, type, result); // 유닛과 공용 전투 상태 적용 신호 전달
        } // 한 턴 상태 알림 종료
        finally // 잠시 바꾼 상태 표시 복원
        { // 상태 알림 범위 복원 시작
            preserveReportedStatusDuration = previous; // 이전 상태 알림의 지속 구분 복원
        } // 상태 알림 범위 복원 종료
    } // 상태 알림 범위 지정 종료
    internal void DeferNewStatusDurationDecrease(BattleStatusEffectType type) // 적용된 일반 상태의 첫 지속 감소 생략
    { // 첫 지속 감소 확인 시작
        if (!preserveReportedStatusDuration) // 원래 수명을 가진 복사 강화와 은총을 제외한 새 상태 확인
        { // 일반 상태 지속 보호 시작
            FindStatusEffect(type, includeCurrentTurnAttackPower: false)?.SkipNextDurationDecrease(); // 같은 종류의 장기 강화만 첫 지속 감소 생략
        } // 일반 상태 지속 보호 종료
    } // 첫 지속 감소 확인 종료
    private static bool IsStandardTimedDebuff(BattleStatusEffectType effectType) // 턴으로 지속하는 일반 디버프 확인
    { // 턴 디버프 확인 시작
        return BattleStatusEffectInstance.IsDebuffType(effectType) // 기존 디버프 분류 사용
            && effectType != BattleStatusEffectType.Stagger // 다음 행동을 기다리는 경직 제외
            && effectType != BattleStatusEffectType.NextAttackDown // 다음 공격을 기다리는 약화 제외
            && effectType != BattleStatusEffectType.DuelMark // 공유 지속을 쓰는 결투 표식 제외
            && effectType != BattleStatusEffectType.ChariotMark // 지정 시점에 소모하는 전차 표식 제외
            && effectType != BattleStatusEffectType.PublicMark; // 첫 턴에 만료되는 공명 표식 제외
    } // 턴 디버프 확인 종료
    public bool RemoveStatusEffect(BattleStatusEffectType effectType) // 지정 상태 이상 제거
    { // 상태 이상 제거 시작
        BattleStatusEffectInstance existingEffect = FindStatusEffect(effectType); // 제거할 상태 이상 조회
        if (!RemoveStatusEffectInstance(existingEffect)) // 상태 존재와 강제 해제 가능 여부 확인
        { // 제거 불가 처리 시작
            return false; // 상태 이상 제거 실패 반환
        } // 제거 불가 처리 종료
        StatusEffectsChanged?.Invoke(this); // 상태 이상 변경 알림
        return true; // 상태 이상 제거 성공 반환
    } // 상태 이상 제거 종료
    public int RemoveAllDebuffs() // 모든 디버프 제거
    { // 디버프 전체 제거 시작
        BattleStatusEffectProcessor statusEffectProcessor = new BattleStatusEffectProcessor(); // 공통 상태 처리기 생성
        IReadOnlyList<BattleStatusEffectProcessResult> cleanseResults = statusEffectProcessor.CleanseDebuffs(this); // 통합 정화 처리 실행
        return cleanseResults.Count; // 제거된 디버프 수 반환
    } // 디버프 전체 제거 종료
    public BattleStatusEffectInstance GetStatusEffect(BattleStatusEffectType effectType) // 지정 상태 이상 공개 조회
    { // 공개 상태 조회 시작
        var existing = FindStatusEffect(effectType, includeCurrentTurnAttackPower: false); // 공용 호출이 사용하는 일반 상태 우선 조회
        return existing ?? (effectType == BattleStatusEffectType.AttackPowerUp ? currentTurnAttackPower : null); // 일반 강화가 없는 경우 이번 턴 공격력 반환
    } // 공개 상태 조회 종료
    internal List<BattleStatusEffectInstance> CreateStatusEffectSnapshot() // 상태 목록 안전 복사
    { // 상태 복사 시작
        return new List<BattleStatusEffectInstance>(statusEffects); // 현재 상태 목록 복사 반환
    } // 상태 복사 종료
    internal bool ContainsStatusEffectInstance(BattleStatusEffectInstance statusEffect) // 상태 인스턴스 존재 확인
    { // 상태 존재 확인 시작
        return statusEffect != null && statusEffects.Contains(statusEffect); // 유효 상태 포함 여부 반환
    } // 상태 존재 확인 종료
    internal bool RemoveStatusEffectInstance(BattleStatusEffectInstance statusEffect, bool expiredFromActive = false) // 상태 인스턴스와 실제 만료 경로 처리
    { // 상태 제거 시작
        if (statusEffect == null || statusEffect.IsProtectedMentalState || !statusEffects.Remove(statusEffect)) // 제거 가능한 실제 상태 확인
        { // 제거 불가 시작
            return false; // 상태가 보존된 결과 반환
        } // 제거 불가 종료
        if (statusEffect == currentTurnAttackPower) // 제거한 상태가 실제 이번 턴 공격력인지 확인
        { // 한 턴 강화 참조 정리 시작
            currentTurnAttackPower = null; // 턴 만료 또는 해제 뒤 더 이상 표시하지 않을 상태
        } // 한 턴 강화 참조 정리 종료
        if (statusEffect.EffectType == BattleStatusEffectType.GuardianHeart) // 수호자의 심장 연결 제거 확인
        { // 시전자 연결 해제 시작
            SetGuardianHeartProtector(null); // 소모나 정화에서 시전자 사망 감시 해제
        } // 시전자 연결 해제 종료
        if (statusEffect.EffectType == BattleStatusEffectType.Stealth && !IsDead && (!statusEffect.IsExpired || expiredFromActive)) // 활성 은신 해제 또는 실제 기간 감소로 만료된 생존 유닛 확인
        { // 은신 종료 발행 시작
            StealthEnded?.Invoke(this); // 목록에서 한 번 제거한 직후 현재 전투에 종료 알림
        } // 은신 종료 발행 종료
        return true; // 상태 제거 성공 반환
    } // 상태 제거 종료
    internal BattleStatusEffectApplyResult ApplyGuardianHeart(BattleUnitRuntime protector) // 지정 생존 아군의 다음 치명 피해 보호 예약
    { // 수호자의 심장 연결 시작
        if (IsDead || Team != BattleTeam.Ally || protector == null || protector == this || protector.IsDead || protector.Team != BattleTeam.Ally) // 서로 다른 두 생존 아군 확인
        { // 보호 불가 시작
            return BattleStatusEffectApplyResult.Invalid; // 잘못된 보호 연결 거부
        } // 보호 불가 종료
        SetGuardianHeartProtector(protector); // 최근 시전자의 한 번 보호 연결
        return ApplyStatusEffect(BattleStatusEffectType.GuardianHeart, 1, 1, 1, protector); // 턴 만료 대신 실제 치명 피해 한 번을 기다리는 상태
    } // 수호자의 심장 연결 종료
    private void SetGuardianHeartProtector(BattleUnitRuntime protector) // 보호 시전자의 사망 감시 교체
    { // 시전자 연결 교체 시작
        if (guardianHeartProtector != null) // 이전 보호 시전자 연결 확인
        { // 이전 시전자 정리 시작
            guardianHeartProtector.Died -= HandleGuardianHeartProtectorDied; // 기존 시전자 사망 감시 해제
        } // 이전 시전자 정리 종료
        guardianHeartProtector = protector; // 보호가 남은 실제 시전자 연결 저장
        if (guardianHeartProtector != null) // 새 보호 시전자 연결 확인
        { // 새 시전자 감시 시작
            guardianHeartProtector.Died += HandleGuardianHeartProtectorDied; // 시전자 사망 시 보호 즉시 해제
        } // 새 시전자 감시 종료
    } // 시전자 연결 교체 종료
    private void HandleGuardianHeartProtectorDied(BattleUnitRuntime protector) // 실제 보호 시전자 전투불능 처리
    { // 전투불능 시전자 처리 시작
        if (protector == guardianHeartProtector) // 현재 보호 연결을 가진 시전자 확인
        { // 끝난 보호 상태 제거 시작
            RemoveStatusEffect(BattleStatusEffectType.GuardianHeart); // 대신 맞을 수 없는 보호 판정과 표시 제거
        } // 끝난 보호 상태 제거 종료
    } // 전투불능 시전자 처리 종료
    internal bool MoveDebuffTo(BattleUnitRuntime recipient, BattleStatusEffectInstance effect) // 단일 디버프를 다른 아군에게 이전
    { // 디버프 이동 시작
        if (!BattleDebuffTransfer.CanTransfer(this, recipient, effect)) // 이전 조건과 원본 상태 재확인
        { // 이동 불가 처리 시작
            return false; // 원래 상태 유지와 실패 반환
        } // 이동 불가 처리 종료
        BattleStatusEffectInstance existing = recipient.FindStatusEffect(effect.EffectType); // 받는 쪽의 동일 상태 조회
        statusEffects.Remove(effect); // 원래 동료의 디버프 제거
        if (existing == null) // 받는 쪽의 기존 상태 존재 확인
        { // 신규 상태 이전 시작
            recipient.statusEffects.Add(effect); // 기존 수치와 지속을 유지한 상태 이동
        } // 신규 상태 이전 종료
        else // 받는 쪽의 동일 상태 확인
        { // 상태 중첩 병합 시작
            for (int stack = 0; stack < effect.StackCount; stack++) // 이전할 중첩 수만큼 순회
            { // 개별 중첩 병합 시작
                existing.Refresh(effect.Value, effect.RemainingTurns, effect.MaximumStacks); // 기존 상태 규칙대로 중첩과 지속 병합
            } // 개별 중첩 병합 종료
            existing.MergeTransferredState(effect); // 이전된 수치 감소와 지속 생략 상태 병합
        } // 상태 중첩 병합 종료
        StatusEffectsChanged?.Invoke(this); // 원래 동료의 상태 화면 갱신
        recipient.TryCleanseReceivedDebuff(existing ?? effect); // 정상 이전을 마친 동료의 받은 약화 정화
        recipient.StatusEffectsChanged?.Invoke(recipient); // 이전받은 동료의 상태 화면 갱신
        return true; // 디버프 이동 성공 반환
    } // 디버프 이동 종료
    internal void NotifyStatusEffectsChanged() // 상태 일괄 변경 알림
    { // 상태 알림 시작
        StatusEffectsChanged?.Invoke(this); // 상태 UI와 예고 갱신 알림
    } // 상태 알림 종료
    private BattleStatusEffectInstance FindStatusEffect(BattleStatusEffectType effectType, bool includeCurrentTurnAttackPower = true) // 일반 또는 이번 턴 상태에서 지정 종류 조회
    { // 상태 이상 조회 시작
        foreach (BattleStatusEffectInstance statusEffect in statusEffects) // 상태 이상 목록 순회
        { // 상태 이상 비교 시작
            if (statusEffect.EffectType == effectType && (includeCurrentTurnAttackPower || statusEffect != currentTurnAttackPower)) // 요청한 종류와 강화 수명 구분 확인
            { // 일치 상태 처리 시작
                return statusEffect; // 일치 상태 이상 반환
            } // 일치 상태 처리 종료
        } // 상태 이상 비교 종료
        return null; // 일치 상태 없음 반환
    } // 상태 이상 조회 종료
    private bool ContainsDebuff() // 현재 디버프 존재 확인
    { // 디버프 존재 확인 시작
        foreach (BattleStatusEffectInstance statusEffect in statusEffects) // 상태 이상 목록 순회
        { // 디버프 확인 시작
            if (statusEffect.IsDebuff) // 디버프 여부 확인
            { // 디버프 존재 처리 시작
                return true; // 디버프 존재 반환
            } // 디버프 존재 처리 종료
        } // 디버프 확인 종료
        return false; // 디버프 없음 반환
    } // 디버프 존재 확인 종료
    internal BattleStatusEffectInstance FirstRemovableBuff() // 먼저 부여된 일반 강화 조회
    { // 강화 탐색 시작
        foreach (var effect in statusEffects) // 상태가 부여된 순서로 확인
        { // 개별 상태 확인 시작
            if (!effect.IsDebuff && !effect.IsProtectedMentalState) // 디버프와 강제로 해제할 수 없는 정신 상태 제외
            { // 제거 대상 발견 시작
                return effect; // 첫 일반 강화 반환
            } // 제거 대상 발견 종료
        } // 개별 상태 확인 종료
        return null; // 제거 가능한 강화 없음
    } // 강화 탐색 종료

    private bool ContainsRemovableDebuff(bool activeOnly = false) // 정화 가능한 디버프 존재와 적용 시간 확인
    { // 정화 가능 디버프 확인 시작
        foreach (BattleStatusEffectInstance effect in statusEffects) // 현재 상태 목록 순회
        { // 정화 가능 상태 확인 시작
            if (effect.IsDebuff && !effect.IsProtectedMentalState && (!activeOnly || !effect.IsExpired)) // 보호 정신 상태 및 선택적 만료 상태 제외
            { // 정화 가능 상태 처리 시작
                return true; // 정화 가능 상태 존재 반환
            } // 정화 가능 상태 처리 종료
        } // 정화 가능 상태 확인 종료
        return false; // 정화 가능 상태 없음 반환
    } // 정화 가능 디버프 확인 종료
    internal static bool IsRemovableActiveDebuff(BattleStatusEffectInstance status) // 공용 정화 및 반사 대상의 제거 가능한 활성 약화 확인
    { // 일반 약화의 제거 가능 여부 검사 시작
        return status != null && status.IsDebuff && !status.IsExpired && !status.IsProtectedMentalState && status.RemainingTurns != int.MaxValue; // 유효한 기간제 약화만 정화와 새장 조건에 포함
    } // 일반 약화의 제거 가능 여부 검사 종료
    internal bool HasAtLeastThreeRemovableDebuffs() // 서로 다른 활성 제거 가능 약화 세 종류 확인
    { // 약화 종류 확인 시작
        var types = new HashSet<BattleStatusEffectType>(); // 중첩 개수와 종류 개수를 구분할 집합
        foreach (var status in statusEffects) // 실제로 남은 상태 항목 확인
        { // 새 활성 약화 검사 시작
            if (IsRemovableActiveDebuff(status) && types.Add(status.EffectType) && types.Count == 3) // 세 번째로 구별되는 활성 약화 확인
            { // 약화 조건 충족 시작
                return true; // 적 턴 시작의 새장 피해 대상 확인
            } // 약화 조건 충족 종료
        } // 새 활성 약화 검사 종료
        return false; // 서로 다른 활성 약화가 세 종류보다 적은 대상
    } // 약화 종류 확인 종료
    private bool TryCleanseReceivedDebuff(BattleStatusEffectInstance status) // 들어온 일반 약화의 즉시 유물 정화
    { // 봉인핵의 유효 확률 확인 시작
        if (Team != BattleTeam.Ally || IsDead || !IsRemovableActiveDebuff(status)) // 생존 아군의 활성 제거 가능 약화 여부
        { // 잘못된 정화 조건 제외 시작
            return false; // 정화 확률의 난수 사용 생략
        } // 잘못된 정화 조건 제외 종료
        int chance = BattleSupplyRelics.Find(RelicEffectType.CleanseReceivedDebuff)?.TriggerChancePercent ?? 0; // 현재 보유한 봉인핵의 유효 확률
        return chance > 0 && (chance == 100 || UnityEngine.Random.Range(0, 100) < chance) && RemoveStatusEffectInstance(status); // 실제로 성립한 약화의 전체 인스턴스 제거
    } // 봉인핵의 유효 확률 확인 종료
    private int CalculateAttackPowerBonus() // 공격력 증가 합계 계산
    { // 공격력 증가 계산 시작
        long totalBonus = ExplorationAttackPowerBattle?.PermanentAttackPower ?? 0; // 탐사 중인 실제 아군의 기존 영구 공격력과 일반 강화 합산 준비
        foreach (BattleStatusEffectInstance statusEffect in statusEffects) // 상태 이상 목록 순회
        { // 공격력 상태 확인 시작
            if (statusEffect.EffectType is BattleStatusEffectType.AttackPowerUp or BattleStatusEffectType.PulseCordAttackPowerUp) // 일반 강화와 맥박줄의 독립 공격력 증가 확인
            { // 공격력 증가 누적 시작
                totalBonus += statusEffect.EffectiveValue; // 전체 공격력 증가 누적
            } // 공격력 증가 누적 종료
        } // 공격력 상태 확인 종료
        return (int)Math.Min(int.MaxValue, totalBonus); // 누적 영구 수치와 기존 공격력 효과의 정수 범위 반환
    } // 공격력 증가 계산 종료
    private int CalculateDefense(int baseDefense, BattleStatusEffectType increaseType, BattleStatusEffectType decreaseType) // 현재 방어력 계산
    { // 방어력 계산 시작
        int increasedDefense = GetStatusEffectValue(increaseType); // 방어 증가 수치 조회
        int decreasedDefense = GetStatusEffectValue(decreaseType); // 방어 감소 수치 조회
        int charge = decreaseType == BattleStatusEffectType.PhysicalDefenseDown ? GetStatusEffectValue(BattleStatusEffectType.ChargeDefenseDown) : 0; // 돌진의 물리 방어 감소량 확인
        var crystal = decreaseType == BattleStatusEffectType.PhysicalDefenseDown ? FindStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown) : null; // 일반 물리 방어 약화와 독립인 결정눈 상태 조회
        int crystalLoss = crystal is { IsExpired: false } ? crystal.EffectiveValue : 0; // 아직 활성인 표식 연계 방어 감소량
        var ward = increaseType == BattleStatusEffectType.MagicalResistanceUp ? FindStatusEffect(BattleStatusEffectType.ArcaneWard) : null; // 마법 저항 계산에서만 실제 마력 보호막 상태 확인
        int wardBonus = ward is { IsExpired: false } ? ward.EffectiveValue : 0; // 활성 중인 정수 저항 증가량
        return (int)Math.Clamp((long)baseDefense + increasedDefense + wardBonus - decreasedDefense - charge - crystalLoss, 0L, int.MaxValue); // 기존 약화와 플랫 저항의 합을 한 번만 영 하한으로 제한
    } // 방어력 계산 종료
    private int GetStatusEffectValue(BattleStatusEffectType effectType) // 상태 이상 전체 수치 조회
    { // 상태 수치 조회 시작
        BattleStatusEffectInstance statusEffect = FindStatusEffect(effectType); // 지정 상태 이상 조회
        return statusEffect == null ? 0 : statusEffect.EffectiveValue; // 상태 이상 전체 수치 반환
    } // 상태 수치 조회 종료
    private void ClearStatusEffects() // 모든 상태 이상 제거
    { // 전체 상태 제거 시작
        if (statusEffects.Count < 1) // 상태 이상 존재 확인
        { // 상태 없음 처리 시작
            return; // 전체 상태 제거 중단
        } // 상태 없음 처리 종료
        int removed = statusEffects.RemoveAll(effect => !effect.IsProtectedMentalState); // 보호된 정신 상태를 제외한 상태 제거
        currentTurnAttackPower = null; // 사망으로 제거한 한 턴 공격력 참조 해제
        SetGuardianHeartProtector(null); // 가려진 아군 사망 시 보호 시전자 감시도 해제
        if (removed > 0) // 실제 상태 제거 여부 확인
        { // 상태 제거 알림 시작
            StatusEffectsChanged?.Invoke(this); // 상태 이상 제거 알림
        } // 상태 제거 알림 종료
    } // 전체 상태 제거 종료
    private void MarkDefeated() // 실제 체력 영의 전투불능과 씨앗 보존
    { // 전투불능 상태 기록 시작
        IsDead = true; // 피해 이벤트부터 쓰러진 아군 상태 확정
        DefeatedWhileBurning = Team == BattleTeam.Enemy && GetStatusEffect(BattleStatusEffectType.Burn) is { IsExpired: false }; // 일반 및 지속 피해의 실제 마지막 체력 시점에 화상 저장
        var seed = Team == BattleTeam.Ally ? GetStatusEffect(BattleStatusEffectType.ResurrectionSeed) : null; // 현재 아군의 부활 씨앗 조회
        pendingSeedRevivalPercent = seed is { IsExpired: false } ? seed.EffectiveValue : 0; // 처치와 사망 알림 뒤 소비할 유효 씨앗만 저장
    } // 전투불능 상태 기록 종료

    private void SettleDefeat(BattleUnitRuntime sourceUnit) // 공통 전투불능과 예약 부활 정산
    { // 전투불능 정산 시작
        ClearStatusEffects(); // 사망 시 일반 상태와 한 번 사용할 씨앗 제거
        Defeated?.Invoke(sourceUnit, this); // 발생자와 대상을 포함한 실제 처치 한 번 알림
        Died?.Invoke(this); // 기존 사망 수신자에게 체력 영과 전투불능 상태 전달
        if (Team == BattleTeam.Ally) // 실제 사망을 알린 아군의 즉시 부활 기회 확인
        { // 유물 부활 정산 신호 시작
            AllyDefeatReady?.Invoke(this); // 승패 확정과 씨앗보다 앞선 아군 부활 단계
        } // 유물 부활 정산 신호 종료
        int percent = pendingSeedRevivalPercent; // 사망 당시 예약한 한 번의 부활 비율 확보
        if (percent > 0) // 사망 당시에 활성 씨앗이 있었는지 확인
        { // 부활 씨앗의 실제 복귀 시작
            pendingSeedRevivalPercent = 0; // 부활과 화면 콜백 전 사용한 씨앗 소모
            if (IsDead) // 먼저 처리한 성배 조각으로 이미 돌아왔는지 확인
            { // 아직 쓰러진 아군의 씨앗 복귀 시작
                Revive(percent); // 기존 정신력과 체력 올림을 사용하는 동일 유닛 복귀
            } // 아직 쓰러진 아군의 씨앗 복귀 종료
            SeedRevivalSettled?.Invoke(this); // 부활 뒤 실제 생존 명단의 승패 판정 요청
        } // 부활 씨앗의 실제 복귀 종료
    } // 전투불능 정산 종료

    public int GainBlock(int amount) // 방어도 획득
    { // 방어도 획득 시작
        if (IsDead || amount <= 0) // 획득 불가 조건 확인
        { // 획득 불가 처리 시작
            return 0; // 획득량 없음 반환
        } // 획득 불가 처리 종료
        int gained = (int)Math.Min((long)amount, (long)int.MaxValue - CurrentBlock); // 정수 범위 내 획득량 계산
        if (gained > 0) // 실제 획득 확인
        { // 획득 처리 시작
            CurrentBlock += gained; // 현재 방어도 증가
            BlockChanged?.Invoke(this); // 방어도 변경 알림
        } // 획득 처리 종료
        return gained; // 실제 획득량 반환
    } // 방어도 획득 종료
    public bool CanSpendHealth(int amount) // 사망하지 않는 체력 비용 지불 가능 확인
    { // 체력 비용 확인 시작
        return amount > 0 && !IsDead && CurrentHealth > amount; // 양수 비용과 소모 후 생존 여부 반환
    } // 체력 비용 확인 종료
    internal void StartEnemyDamageWindow() // 직전 적 턴의 피해 집계 시작
    { // 체력 손실 집계 초기화 시작
        hasEnemyDamageWindow = true; // 적 턴 이후의 체력 손실 기록 시작
        HealthLostSinceEnemyTurn = 0; // 이전 턴의 누적 손실 제거
    } // 체력 손실 집계 초기화 종료
    private void LoseHealth(int amount) // 방어 처리 후 실제 체력 손실 반영
    { // 체력 차감 시작
        int lost = Math.Min(CurrentHealth, Math.Max(0, amount)); // 남은 체력을 초과한 피해 제외
        CurrentHealth -= lost; // 실제 체력 차감
        if (hasEnemyDamageWindow) // 첫 적 턴이 시작된 상태 확인
        { // 누적 피해 반영 시작
            HealthLostSinceEnemyTurn = (int)Math.Min(int.MaxValue, (long)HealthLostSinceEnemyTurn + lost); // 회복과 무관한 실제 손실 누적
        } // 누적 피해 반영 종료
    } // 체력 차감 종료
    internal void QueueChargeDefense(BattleUnitRuntime source, int amount) // 다음 아군 턴의 방어 감소 예약
    { // 다음 턴 약화 예약 시작
        queuedChargeDefense = Math.Max(queuedChargeDefense, amount); // 중복 사용 시 가장 큰 감소량 보관
        queuedChargeSource = source; // 약화를 부여한 소유자 보관
    } // 다음 턴 약화 예약 종료
    internal void ApplyQueuedCharge() // 예약한 돌진 약화 발동
    { // 예약 약화 적용 시작
        if (queuedChargeDefense > 0 && !IsDead) // 생존한 피격 대상과 예약 확인
        { // 방어 감소 부여 시작
            ApplyStatusEffect(BattleStatusEffectType.ChargeDefenseDown, queuedChargeDefense, 1, 1, queuedChargeSource); // 이번 아군 턴에 물리 방어 감소
        } // 방어 감소 부여 종료
        queuedChargeDefense = 0; // 이전 예약 소진
        queuedChargeSource = null; // 이전 소유자 해제
    } // 예약 약화 적용 종료
    public bool TrySpendHealth(int amount) // 방어도를 소모하지 않는 체력 비용 지불
    { // 체력 비용 지불 시작
        if (!CanSpendHealth(amount)) // 유효한 비용과 생존 가능 여부 확인
        { // 비용 지불 불가 시작
            return false; // 체력을 유지한 실패 반환
        } // 비용 지불 불가 종료
        LoseHealth(amount); // 방어도와 무관하게 현재 체력 소모
        HealthChanged?.Invoke(this); // 체력 화면과 영구 상태 갱신 알림
        return true; // 체력 비용 지불 성공 반환
    } // 체력 비용 지불 종료
    public void ClearBlock() // 남은 방어도 제거
    { // 방어도 제거 시작
        if (CurrentBlock <= 0) // 제거할 방어도 확인
        { // 제거 불필요 처리 시작
            return; // 중복 알림 방지
        } // 제거 불필요 처리 종료
        CurrentBlock = 0; // 방어도 초기화
        BlockChanged?.Invoke(this); // 방어도 변경 알림
    } // 방어도 제거 종료
    public int RemoveBlock(int amount) // 지정 수치만큼 방어도 제거
    { // 방어도 부분 제거 시작
        if (IsDead || amount <= 0 || CurrentBlock <= 0) // 생존 상태와 제거 가능한 방어도 확인
        { // 방어도 제거 불가 시작
            return 0; // 제거량 없음 반환
        } // 방어도 제거 불가 종료
        int removed = Math.Min(CurrentBlock, amount); // 남은 방어도를 넘지 않는 제거량 계산
        CurrentBlock -= removed; // 실제 방어도 감소
        BlockChanged?.Invoke(this); // 방어도 화면 갱신 알림
        return removed; // 실제 제거량 반환
    } // 방어도 부분 제거 종료
    public BattleDamageResult PreviewDamage(int damageAmount, BattleDamageType damageType, BattleUnitRuntime sourceUnit = null, int? blockOverride = null, bool enemyDirectAttack = false, bool alliedDirectAttack = false) // 직접 공격과 방어도를 반영한 순수 피해 예고
    { // 기본 피해 예고 시작
        damageAmount = alliedDirectAttack ? ApplyAlliedDirectDamageBonuses(damageAmount, sourceUnit, out _) : damageAmount; // 표식을 소비하지 않는 실제 직접 타격 보정 예고
        return PreviewDamage(damageAmount, damageType, sourceUnit, blockOverride, enemyDirectAttack, applyDangerInterception: true); // 현재 위험 차단을 포함한 피해 예고
    } // 기본 피해 예고 종료
    internal BattleDamageResult PreviewDamage(int damageAmount, BattleDamageType damageType, BattleUnitRuntime sourceUnit, int? blockOverride, bool enemyDirectAttack, bool applyDangerInterception, bool enemyAreaAttack = false, bool applyDamageNullification = true) // 공격 의도와 순서의 보호를 반영한 피해 예고
    { // 기존 피해 예고 시작
        return PreviewDamageWithParty(damageAmount, damageType, sourceUnit, blockOverride, enemyDirectAttack, applyDangerInterception, null, out _, enemyAreaAttack, applyDamageNullification); // 기존 호출 경로의 파티 분배 없는 예상 결과
    } // 기존 피해 예고 종료

    internal BattleDamageResult PreviewDamageWithParty(int damageAmount, BattleDamageType damageType, BattleUnitRuntime sourceUnit, int? blockOverride, bool enemyDirectAttack, bool applyDangerInterception, IReadOnlyList<BattleUnitRuntime> party, out PainDistributionPlan distribution, bool enemyAreaAttack = false, bool applyDamageNullification = true) // 파티 분산과 원래 대상의 실제 방어도를 함께 계산
    { // 피해와 파티 분산 예상 계산 시작
        distribution = null; // 무효 피해 및 단독 피격의 기본 분배 없음
        if (IsDead) // 사망 상태 확인
        { // 사망 유닛 처리 시작
            return BattleDamageResult.Empty(damageType); // 피해 없음 결과 반환
        } // 사망 유닛 처리 종료
        damageAmount = ApplyHeartShardDamageBonus(damageAmount, sourceUnit); // 출처가 있는 아군 피해의 심장파편 증가를 예고와 실제에 함께 반영
        BattleStatusEffectInstance preparation = GetApplicablePreparation(damageAmount, damageType, sourceUnit); // 해당 공격의 방어 준비 조회
        int currentBlock = Math.Max(0, blockOverride ?? CurrentBlock); // 예상 기존 방어도 조회
        int preparedBlock = preparation == null ? 0 : (int)Math.Min((long)preparation.EffectiveValue, (long)int.MaxValue - currentBlock); // 정수 범위 내 준비 방어도 계산
        long vulnerability = GetStatusEffectValue(BattleStatusEffectType.Vulnerable); // 대상에게 적용된 받는 피해 증가율
        vulnerability += damageType == BattleDamageType.Magical ? Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.IncomingMagicDamageUp)) : 0; // 마법 피해에만 별도 취약을 합산
        int vulnerableDamage = (int)Math.Clamp((long)damageAmount * (100L + vulnerability) / 100L, 0L, int.MaxValue); // 두 상태 합산 뒤 정수 범위의 실제 피해 내림
        int corrosion = damageType == BattleDamageType.Magical && GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown) is { IsExpired: false } corrosionStatus ? corrosionStatus.EffectiveValue : 0; // 실제 활성 실금석 약화의 경감 퍼센트포인트 조회
        BattleDamageResult damageResult = BattleDamageCalculator.Calculate(vulnerableDamage, damageType, PhysicalDefense, MagicalResistance, corrosion); // 예상과 실제 피해의 공통 마법 저항 감소 반영
        bool eligible = enemyDirectAttack && Team == BattleTeam.Ally && currentBlock > 0 && (damageType == BattleDamageType.Physical || damageType == BattleDamageType.Magical); // 명중 전 방어도가 남은 아군의 물리·마법 직접 피격
        int relicReduction = eligible ? RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.ShieldedDamageReduction)?.EffectValue ?? 0 : 0; // 보유 코일의 고정 피해 감소량
        int shieldReduction = enemyDirectAttack && Team == BattleTeam.Ally // 기존 적 직접 피해의 방패 경감 대상 확인
            ? GetShieldCardReduction(damageResult.FinalDamage, damageType, applyDangerInterception, enemyAreaAttack) // 기존 방패 보호와 성역을 한 번에 적용
            : damageResult.FinalDamage - ApplySanctuaryDamageReduction(damageResult.FinalDamage); // 일반 및 지속 피해의 성역 경감량
        int reduction = (int)Math.Min(int.MaxValue, (long)relicReduction + shieldReduction + GetLowHealthDamageReduction()); // 기존 비율 경감 뒤 저체력 고정 감소와 방어도 이전 정산
        if (damageResult.FinalDamage > reduction && applyDamageNullification && GetStatusEffect(BattleStatusEffectType.DamageNullification) != null) // 경감 뒤 체력이나 방어도에 닿을 양수 피해와 무효 상태 확인
        { // 피해 무효 예고 시작
            return damageResult.WithoutDamage(); // 방어도와 체력을 모두 보존하는 결과 반환
        } // 피해 무효 예고 종료
        if (enemyDirectAttack && party != null) // 현재 적의 직접 타격에 파티 분산을 사용할 수 있는지 확인
        { // 방어도 이전 피해 분배 시작
            distribution = PainDistributionPlan.Create(this, party, Math.Max(0, damageResult.FinalDamage - reduction)); // 경감 뒤 원래 방어도 적용 전의 파티 전체 몫
            reduction += distribution?.Total ?? 0; // 원래 대상의 몫에서 실제 분담 총량만 빼기
        } // 방어도 이전 피해 분배 종료
        return damageResult.WithBlock(currentBlock + preparedBlock, CurrentHealth, preparedBlock, reduction); // 분산 뒤 원래 대상의 준비 방어도와 체력 계산
    } // 예상 피해 계산 종료
    internal int GetLowHealthDamageReduction() // 일반·분담 피해가 공통으로 쓰는 저체력 감소량
    { // 빛 없는 목걸이 조건 확인 시작
        if (Team != BattleTeam.Ally || IsDead || (long)CurrentHealth * 10 > (long)MaxHealth * 3) // 피해 이전의 실제 생존 아군과 최대 체력 삼십 퍼센트 확인
        { // 목걸이 효과 범위 밖 처리 시작
            return 0; // 일반 피해량 보존
        } // 목걸이 효과 범위 밖 처리 종료
        return BattleSupplyRelics.Find(RelicEffectType.LowHealthDamageReduction)?.EffectValue ?? 0; // 실제 보유한 목걸이의 고정 감소량
    } // 빛 없는 목걸이 조건 확인 종료

    private int SanctuaryReductionPercent => Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.SanctuaryWard)); // 모든 피해를 경감하는 성역의 유효 비율

    private int ApplySanctuaryDamageReduction(int damage) // 다른 비율 경감이 없는 피해의 성역 적용
    { // 성역 직접 비율 계산 시작
        return (int)((long)damage * (100 - SanctuaryReductionPercent) / 100); // 정수 범위와 한 번의 내림을 적용한 남은 피해
    } // 성역 직접 비율 계산 종료

    internal int GetUnblockedDamageAfterReduction(int damage) // 실제 분담과 출혈 및 분담 예고의 공통 경감
    { // 방어도 없는 피해 경감 시작
        return Math.Max(0, ApplySanctuaryDamageReduction(damage) - GetLowHealthDamageReduction()); // 성역 비율 뒤 저체력 고정 감소와 영 하한 적용
    } // 방어도 없는 피해 경감 종료

    private int GetShieldCardReduction(int damage, BattleDamageType type, bool applyDangerInterception, bool enemyAreaAttack) // 방패 상태와 광역 의도의 직접 피해 경감 계산
    { // 방패 피해 경감 계산 시작
        int resistance = type == BattleDamageType.Magical ? Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.ResistanceStance)) : 0; // 마법 공격에만 적용할 추가 저항 비율
        int protection = Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.GuardianCircle)); // 수호의 원 피해 경감률
        int interception = applyDangerInterception ? Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.DangerInterception)) : 0; // 적용 가능한 위험 차단 비율
        int focus = Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.FocusedDefense)); // 현재 집중 방어의 직접 피해 경감률
        int spread = enemyAreaAttack ? Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.DistributedDefense)) : 0; // 적의 광역 의도에 적용할 분산 방어 비율
        int fortify = type == BattleDamageType.Magical ? Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.FortifyResistance)) : 0; // 요새화의 별도 마법 직접 피해 감소 비율
        int eternalShield = Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.EternalShield)); // 영원의 방패 직접 피해 경감률
        int eternalResistance = type == BattleDamageType.Magical ? Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.EternalShieldResistance)) : 0; // 영원의 방패 별도 마법 직접 피해 저항률
        int adjusted = (int)((decimal)damage * (100 - resistance) * (100 - protection) * (100 - interception) // 기존 마법 저항과 수호 및 위험 차단 비율
            * (100 - focus) * (100 - spread) * (100 - fortify) * (100 - eternalShield) * (100 - eternalResistance) // 집중·분산과 요새화 및 영원의 방패 비율
            * (100 - SanctuaryReductionPercent) / 1_000_000_000_000_000_000m); // 성역을 포함한 전체 비율 곱의 마지막 한 번 내림
        int remaining = Math.Max(0, adjusted - GetStatusEffectValue(BattleStatusEffectType.ShockAbsorption)); // 비율 계산 뒤 충격 흡수의 고정 피해 감소
        return damage - remaining; // 방어도에 반영할 총 피해 감소값 반환
    } // 방패 피해 경감 계산 종료
    public BattleDamageResult TakeDamage(int damageAmount, BattleDamageType damageType, BattleUnitRuntime sourceUnit = null, BattleUnitRuntime sharingAlly = null, Func<bool> preventHealthDamage = null, int bypassBlockPercent = 0, Func<int, int> reduceHealthDamage = null, bool enemyDirectAttack = false, bool enemyAreaAttack = false, bool alliedDirectAttack = false) // 적 광역과 아군 직접 타격을 포함한 실제 피해 처리
    { // 공통 피해 진입 시작
        return TakeDamageCore(damageAmount, damageType, sourceUnit, sharingAlly, preventHealthDamage, bypassBlockPercent, reduceHealthDamage, enemyDirectAttack, enemyAreaAttack, null, false, alliedDirectAttack: alliedDirectAttack); // 원본 직접 공격의 출처를 포함한 피해 정산
    } // 공통 피해 진입 종료
    internal BattleDamageResult TakeEnemyHit(int damageAmount, BattleDamageType damageType, BattleUnitRuntime source, BattleUnitRuntime sharingAlly, Func<bool> preventHealthDamage, Func<int, int> reduceHealthDamage, bool isAreaAttack, Action<BattleUnitRuntime> onRedirect, IReadOnlyList<BattleUnitRuntime> party = null) // 적 타격과 파티 분담에 실제로 연결된 피격 대상
    { // 실제 적 타격 처리 시작
        return TakeDamageCore(damageAmount, damageType, source, sharingAlly, preventHealthDamage, 0, reduceHealthDamage, true, isAreaAttack, onRedirect, false, party); // 실제 명중과 대상의 피해 분배 및 기존 보호 전환
    } // 실제 적 타격 처리 종료
    internal BattleDamageResult TakeWandEchoDamage(int amount, BattleUnitRuntime source) // 반향가 복제한 실제 마법 피해 접수
    { // 반향의 공용 피해 경로 시작
        return TakeDamageCore(amount, BattleDamageType.Magical, source, null, null, 0, null, false, false, null, false, wandEcho: true); // 기존 저항과 방어도 및 실제 사망 처리에 반향 출처 전달
    } // 반향의 공용 피해 경로 종료
    private void RecordCorrosionHealthDamage(BattleDamageResult damageResult) // 유물용 침식 체력 손실 기록
    { // 침식 기록 시작
        if (Team == BattleTeam.Ally && damageResult.DamageType == BattleDamageType.Corrosion && damageResult.AppliedDamage > 0) // 아군의 양수 침식 체력 손실
        { // 적격 피해 처리 시작
            HasLostHealthToCorrosion = true; // 전투 종료까지 피격 이력 유지
        } // 적격 피해 처리 끝
    } // 침식 기록 끝

    private BattleDamageResult TakeDamageCore(int damageAmount, BattleDamageType damageType, BattleUnitRuntime sourceUnit, BattleUnitRuntime sharingAlly, Func<bool> preventHealthDamage, int bypassBlockPercent, Func<int, int> reduceHealthDamage, bool enemyDirectAttack, bool enemyAreaAttack, Action<BattleUnitRuntime> onRedirect, bool guardianSurvival, IReadOnlyList<BattleUnitRuntime> party = null, bool alliedDirectAttack = false, bool wandEcho = false) // 직접 피해와 실제 파티 분배 및 독립 반향 정산
    { // 피해 처리 시작
        if (damageAmount <= 0 || IsDead) // 무효 피해 확인
        { // 무효 처리 시작
            return BattleDamageResult.Empty(damageType); // 적용 피해 없음
        } // 무효 처리 종료
        BattleStatusEffectInstance confession = null; // 명중 전에 확정할 이번 직접 피해의 표식 참조
        if (alliedDirectAttack) // 출혈과 반사 및 고정 피해를 제외한 직접 카드 명중 확인
        { // 고해실의 못 피해 가산 시작
            damageAmount = ApplyAlliedDirectDamageBonuses(damageAmount, sourceUnit, out confession); // 예고와 같은 방어 이전 고해 표식과 은신 보정
        } // 고해실의 못 피해 가산 종료
        bool consumePreparation = GetApplicablePreparation(damageAmount, damageType, sourceUnit) != null; // 방어 준비 발동 여부 확인
        int skippedBlock = (int)((long)CurrentBlock * Mathf.Clamp(bypassBlockPercent, 0, 100) / 100); // 현재 방어도에서 타격 한 번이 건너뛸 수량 내림
        bool linked = sharingAlly != null && sharingAlly != this && sharingAlly.Team == BattleTeam.Ally && Team == BattleTeam.Ally && !sharingAlly.IsDead; // 원래 피해 분담의 실제 생존 연결 우선
        var distributionParty = !linked && !guardianSurvival && sourceUnit?.Team == BattleTeam.Enemy ? party : null; // 단일 연결과 기존 수호자 전환에서 파티 분산 제외
        BattleDamageResult damageResult = PreviewDamageWithParty(damageAmount, damageType, sourceUnit, CurrentBlock - skippedBlock, enemyDirectAttack, true, distributionParty, out var distribution, enemyAreaAttack); // 다중 분배와 기존 원래 대상 방어도 계산
        if (wandEcho) // 실제 생성된 반향 피해의 원본 결과 확인
        { // 공용 피해 신호의 반향 표시 시작
            damageResult = damageResult.WithWandEcho(); // 방어 및 사망과 무효 결과에도 남는 반향 출처
        } // 공용 피해 신호의 반향 표시 종료
        bool consumeInterception = enemyDirectAttack && Team == BattleTeam.Ally; // 실제 피격자만 소모할 위험 차단 확인
        if (damageResult.WasDamageNullified) // 기존 경감 뒤 한 번 무효화된 실제 피해 확인
        { // 실제 피해 무효 시작
            if (consumeInterception) // 무효화를 발동한 실제 피격 여부
            { // 무효 피해의 위험 차단 처리 시작
                RemoveStatusEffect(BattleStatusEffectType.DangerInterception); // 무효화 경로의 기존 위험 차단 소모 유지
            } // 무효 피해의 위험 차단 처리 종료
            RemoveStatusEffect(BattleStatusEffectType.DamageNullification); // 양수 피해를 막은 한 번의 보호 소모
            return damageResult; // 방어도 흡수와 분담을 생략한 무효 결과 반환
        } // 실제 피해 무효 종료
        int sharedDamage = 0; // 동료에게 분담할 피해 초기화
        if (linked) // 생존한 기존 단일 피해 분담 링크 확인
        { // 피해 분담 계산 시작
            int remainingDamage = Math.Max(0, damageResult.FinalDamage - damageResult.AbsorbedDamage); // 방어와 방어도 적용 후 피해 계산
            sharedDamage = remainingDamage / 2; // 내림한 절반을 분담자에게 배정
            damageResult = damageResult.WithAppliedDamage(Math.Min(CurrentHealth, remainingDamage - sharedDamage)); // 홀수 한 점을 원래 대상에 배정
        } // 피해 분담 계산 종료
        int paidHealthReduction = 0; // 분담 뒤 한 번만 평가할 파티 체력 피해 고정 경감량
        if (damageResult.AppliedDamage > 0 && preventHealthDamage?.Invoke() == true) // 방어와 분담 뒤 주 대상에게 남은 실제 체력 피해 확인
        { // 최초 직접 피해 무효 처리 시작
            damageResult = damageResult.WithAppliedDamage(0); // 소모한 방어도와 명중 상태를 유지하고 체력 손실만 제거
        } // 최초 직접 피해 무효 처리 종료
        else if (damageResult.AppliedDamage > 0 && reduceHealthDamage != null) // 군번줄 미발동 및 실제 주 대상 체력 피해 확인
        { // 철제 장화 경감 시작
            int incoming = Math.Max(0, damageResult.FinalDamage - damageResult.AbsorbedDamage - sharedDamage); // 방어와 분담 뒤의 실제 원본 피해
            int reduced = reduceHealthDamage(incoming); // 실제 피해 경감 콜백의 한 번 판정
            damageResult = damageResult.WithAppliedDamage(Math.Min(CurrentHealth, reduced)); // 체력 상한 적용 전에 장화 한 점 경감
            paidHealthReduction = (int)Math.Clamp((long)incoming - reduced, 0L, incoming); // 이미 지불한 공용 고정 경감만 대신 받은 피해에 이전
        } // 철제 장화 경감 종료
        damageResult = damageResult.WithAppliedDamage(ApplySurvivalProtection(damageResult.AppliedDamage, allowDefeatWard: !guardianSurvival)); // 방어와 불굴 및 성배 뒤 수호자 대체 판정
        if (guardianSurvival) // 수호자가 대신 받는 바로 이 타격 확인
        { // 대신 받은 피해의 생존 상한 시작
            damageResult = damageResult.WithAppliedDamage(Math.Min(damageResult.AppliedDamage, CurrentHealth - 1)); // 원래 수호자의 체력 한 점 이상 보장
        } // 대신 받은 피해의 생존 상한 종료
        else if (enemyDirectAttack && sourceUnit?.Team == BattleTeam.Enemy && damageResult.AppliedDamage >= CurrentHealth && guardianHeartProtector != null && !guardianHeartProtector.IsDead) // 기존 경감과 불굴 뒤에도 치명적인 적 직접 타격 확인
        { // 실제 수호자 대체 피격 시작
            BattleUnitRuntime protector = guardianHeartProtector; // 피해 알림 전 살아 있는 수호자 보관
            if (RemoveStatusEffectInstance(FindStatusEffect(BattleStatusEffectType.GuardianHeart))) // 피해 적용 전에 보호 한 번 조용히 소모
            { // 수호자 피해 전환 시작
                Func<int, int> transferredReduction = paidHealthReduction > 0 ? amount => Math.Max(0, amount - paidHealthReduction) : null; // 공용 경감을 재소모하지 않는 이미 확정한 할인
                onRedirect?.Invoke(protector); // 피해 이벤트 전에 실제 공격 대상 전환 기록
                BattleDamageResult guarded = protector.TakeDamageCore(damageAmount, damageType, sourceUnit, null, null, bypassBlockPercent, transferredReduction, true, enemyAreaAttack, null, true); // 명중 재판정과 기존 분담 없이 같은 원본 공격 한 번 처리
                StatusEffectsChanged?.Invoke(this); // 원래 대상의 보존된 상태와 보호 소모 알림
                return guarded; // 원래 아군에게 피해와 방어도 소모 없는 실제 수호자 결과 반환
            } // 수호자 피해 전환 종료
        } // 실제 수호자 대체 피격 종료
        if (consumeInterception) // 전환되지 않은 실제 피격의 위험 차단 소모 확인
        { // 위험 차단 소모 시작
            RemoveStatusEffect(BattleStatusEffectType.DangerInterception); // 실제 맞은 아군의 위험 차단 소모
        } // 위험 차단 소모 종료
        if (consumePreparation) // 전환되지 않은 실제 대상의 방어 준비 확인
        { // 준비 소모 시작
            RemoveStatusEffect(BattleStatusEffectType.GuardPreparation); // 실제 발동한 방어 준비 제거
            GainBlock(damageResult.PreparedBlock); // 피해 적용 직전 준비 방어도 획득
        } // 준비 소모 종료
        if (confession != null && (damageResult.AppliedDamage > 0 || damageResult.AbsorbedDamage > 0) && RemoveStatusEffectInstance(confession)) // 실제 체력 또는 방어도 피해가 생긴 확정 명중 확인
        { // 실제 표식 사용 종료 알림 시작
            NotifyStatusEffectsChanged(); // 피해와 새 정신력 손실 신호 전에 한 번 사용한 표식의 제거 알림
        } // 실제 표식 사용 종료 알림 완료
        LoseHealth(damageResult.AppliedDamage); // 현재 체력 감소
        CurrentBlock -= damageResult.AbsorbedDamage; // 흡수한 방어도 소모
        bool diedNow = CurrentHealth == 0; // 이번 사망 여부 계산
        if (diedNow) // 사망 상태 확인
        { // 사망 처리 시작
            MarkDefeated(); // 실제 전투불능과 씨앗 사용 가능 여부 저장
            damageResult = damageResult.WithDefeatedTarget(); // 이후 부활과 독립된 이 타격의 전투불능 기록
        } // 사망 처리 종료
        if (enemyDirectAttack && sourceUnit?.Team == BattleTeam.Enemy) // 실제 적이 직접 명중한 피해 결과 확인
        { // 직접 피해 출처 지정 시작
            damageResult = damageResult.FromResolvedEnemyAttack(); // 실제 피해 신호 전에 직접 적 공격 출처 고정
        } // 직접 피해 출처 지정 종료
        RecordCorrosionHealthDamage(damageResult); // 일반 피해의 침식 체력 손실 기록
        DamageTaken?.Invoke(this, damageResult); // 피해 적용 결과 알림
        HealthChanged?.Invoke(this); // 체력 변경 알림
        if (damageResult.AbsorbedDamage > 0) // 방어도 소모 여부 확인
        { // 소모 알림 시작
            BlockChanged?.Invoke(this); // 방어도 변경 알림
        } // 소모 알림 종료
        DamageResolved?.Invoke(sourceUnit, this, damageResult); // 발생자 포함 피해 완료 알림
        if (!wandEcho) // 원래 사용의 실제 피해에만 정신력 가산과 감소 허용
        { // 일반 피격 정신력 처리 시작
            ApplyDamageMental(damageResult.AppliedDamage, diedNow, sourceUnit); // 원래 공격의 피해 및 처치 정신력 변화 적용
        } // 일반 피격 정신력 처리 종료
        if (diedNow) // 신규 사망 확인
        { // 사망 알림 시작
            SettleDefeat(sourceUnit); // 사망 신호와 가능한 씨앗 부활을 순서대로 정산
        } // 사망 알림 종료
        if (distribution != null) // 전환되지 않고 제대로 맞은 직접 타격의 파티 몫 확인
        { // 실제 고통 분산의 개별 조각 적용 시작
            for (int index = 0; index < distribution.Recipients.Count; index++) // 고정 출전 명단의 분담 순서 순회
            { // 분담받는 아군의 개별 무방어 피해 처리 시작
                distribution.Recipients[index].TakeUnblockedDamage(distribution.GetPortion(index), damageType, sourceUnit, fromEnemyDirectAttack: damageResult.FromEnemyDirectAttack); // 원래 타격과 연결하되 새 공격과 보호·분담을 발생시키지 않는 피해
            } // 분담받는 아군의 개별 무방어 피해 처리 종료
        } // 실제 고통 분산의 개별 조각 적용 종료
        if (sharedDamage > 0 && sharingAlly != null && !sharingAlly.IsDead) // 실제 분담 피해와 생존 상태 확인
        { // 분담 피해 적용 시작
            sharingAlly.TakeUnblockedDamage(sharedDamage, damageType, sourceUnit, fromEnemyDirectAttack: damageResult.FromEnemyDirectAttack); // 동일 직접 공격과 연결된 실제 분담 피해 적용
        } // 분담 피해 적용 종료
        if (!IsDead && damageResult.AppliedDamage > 0 && damageType == BattleDamageType.Physical && Team == BattleTeam.Ally && sourceUnit?.Team == BattleTeam.Enemy) // 직접 물리 공격의 실제 생존 피해 확인
        { // 검격 반사 시작
            var riposte = diedNow ? null : GetStatusEffect(BattleStatusEffectType.PhysicalRiposte); // 부활 전에 실제로 버틴 대상만 검격 반사 확인
            if (riposte != null) // 첫 유효 피해를 기다리는 상태 확인
            { // 반사 확정 시작
                int reflected = (int)((long)damageResult.AppliedDamage * riposte.EffectiveValue / 100); // 직접 잃은 체력 비율 내림
                RemoveStatusEffect(BattleStatusEffectType.PhysicalRiposte); // 반격 피해가 영이어도 첫 기회 소모
                sourceUnit.TakeUnblockedDamage(reflected, BattleDamageType.None, this); // 공격자 방어와 방어도를 건너뛴 고정 피해
            } // 반사 확정 종료
        } // 검격 반사 종료
        if (!diedNow && !IsDead && enemyDirectAttack && Team == BattleTeam.Ally && sourceUnit?.Team == BattleTeam.Enemy && damageResult.AbsorbedDamage > 0) // 부활에 의존하지 않은 생존과 실제 방어도 흡수 확인
        { // 방패 반격 시작
            sourceUnit.TakeUnblockedDamage(GetStatusEffectValue(BattleStatusEffectType.ShieldCounter), BattleDamageType.None, this); // 공격자 방어와 방어도를 무시한 고정 반격
        } // 방패 반격 종료
        return damageResult; // 피해 계산 결과 반환
    } // 피해 처리 종료
    internal int PreviewUnblockedHealthDamage(int amount) // 분담 예고에서 같은 방어도 없는 보호와 체력 경감 사용
    { // 분담받는 실제 체력 피해 예고 시작
        int reduced = GetUnblockedDamageAfterReduction(amount); // 기존 성역과 저체력 경감의 실제 무방어 값
        if (IsDead || reduced == 0 || GetStatusEffect(BattleStatusEffectType.DamageNullification) != null) // 죽은 분담자와 피해 없는 보호 조건 확인
        { // 이번 분담에서 실제 손실 없음 시작
            return 0; // 일회 보호를 소비하지 않는 체력 피해 없음 예고
        } // 이번 분담에서 실제 손실 없음 종료
        return ApplySurvivalProtection(Math.Min(CurrentHealth, reduced), consume: false); // 실제 불굴과 성배의 생존 경계를 상태 소모 없이 반영
    } // 분담받는 실제 체력 피해 예고 종료

    internal int TakeUnblockedDamage(int amount, BattleDamageType damageType, BattleUnitRuntime sourceUnit, bool fromEnemyDirectAttack = false) // 분담과 출혈 및 반사의 방어도 없는 실제 체력 피해
    { // 분담 피해 처리 시작
        if (amount <= 0 || IsDead) // 실제 피해와 생존 상태 확인
        { // 분담 피해 없음 처리 시작
            return 0; // 적용된 체력 피해 없음
        } // 분담 피해 없음 처리 종료
        amount = ApplyHeartShardDamageBonus(amount, sourceUnit); // 공격자 출처가 명시된 무방어 피해의 심장파편 원본 증가
        int reduced = GetUnblockedDamageAfterReduction(amount); // 성역과 고정 감소가 반영된 실제 무방어 피해
        if (reduced == 0 || RemoveStatusEffect(BattleStatusEffectType.DamageNullification)) // 충분히 줄어든 피해나 기존 양수 피해 무효 확인
        { // 무방어 피해 무효 시작
            return 0; // 체력 소모와 불굴 발동을 막은 피해 반환
        } // 무방어 피해 무효 종료
        int applied = ApplySurvivalProtection(Math.Min(CurrentHealth, reduced)); // 저체력 감소 뒤에 불굴과 성배 및 체력 상한 적용
        BattleDamageResult result = new BattleDamageResult(amount, 0, amount, reduced, applied, damageType, false, 1f); // 원래 피해와 고정 감소 뒤의 실제 피해 결과 기록
        if (fromEnemyDirectAttack && sourceUnit?.Team == BattleTeam.Enemy) // 현재 직접 적 공격에서 파생된 분담 피해 확인
        { // 분담 공격 출처 지정 시작
            result = result.FromResolvedEnemyAttack(); // 지속 피해와 구분한 직접 타격의 실제 분담 기록
        } // 분담 공격 출처 지정 종료
        LoseHealth(applied); // 분담자의 체력 감소
        bool diedNow = CurrentHealth == 0; // 분담자의 사망 여부 확인
        if (diedNow) // 새 사망 여부 확인
        { // 사망 상태 기록 시작
            MarkDefeated(); // 방어도 없는 피해의 실제 전투불능 상태 저장
            result = result.WithDefeatedTarget(); // 실제 전투불능을 피해 이벤트에 기록
        } // 사망 상태 기록 종료
        RecordCorrosionHealthDamage(result); // 방어도 미적용 침식 체력 손실 기록
        DamageTaken?.Invoke(this, result); // 분담 피해 화면 알림
        HealthChanged?.Invoke(this); // 분담자 체력 갱신 알림
        DamageResolved?.Invoke(sourceUnit, this, result); // 발생자 포함 분담 피해 알림
        ApplyDamageMental(applied, diedNow, sourceUnit); // 분담 피해의 정신력 변화 적용
        if (diedNow) // 신규 사망 확인
        { // 분담자 사망 처리 시작
            SettleDefeat(sourceUnit); // 동일한 처치와 씨앗 부활 처리
        } // 분담자 사망 처리 종료
        return applied; // 실제로 감소한 체력 반환
    } // 직접 체력 피해 처리 종료
    private int ApplyAlliedDirectDamageBonuses(int amount, BattleUnitRuntime sourceUnit, out BattleStatusEffectInstance mark) // 살아 있는 적의 실제 명중과 예고가 공유하는 방어 전 가산
    { // 아군 직접 피해 가산 시작
        mark = null; // 적용하지 않은 타격의 기본 고해 표식 없음
        if (amount <= 0 || IsDead || Team != BattleTeam.Enemy || sourceUnit is not { Team: BattleTeam.Ally, IsDead: false }) // 양수 원본 피해와 실제 살아 있는 적 및 아군 확인
        { // 가산 제외 시작
            return amount; // 일반 피해와 대상에 어긋난 원래 피해 유지
        } // 가산 제외 종료
        mark = GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark) is { IsExpired: false } activeMark ? activeMark : null; // 기존 고해의 다음 명중 소비 참조 보존
        int curtain = GetStatusEffect(BattleStatusEffectType.Stealth) is { IsExpired: false } ? BattleSupplyRelics.Find(RelicEffectType.CurtainPinStealthDamage)?.EffectValue ?? 0 : 0; // 실제 활성 은신 적의 커튼 고정핀 가산 조회
        return (int)Math.Min(int.MaxValue, (long)amount + (mark?.EffectiveValue ?? 0) + curtain); // 양쪽 독립 가산과 원래 정수 피해 상한 적용
    } // 아군 직접 피해 가산 종료

    private int ApplyHeartShardDamageBonus(int amount, BattleUnitRuntime sourceUnit) // 심장파편을 반영한 실제 아군 출처 피해
    { // 저체력 아군 피해 계산 시작
        if (amount <= 0 || Team != BattleTeam.Enemy || sourceUnit?.Team != BattleTeam.Ally || sourceUnit.IsDead || (long)sourceUnit.CurrentHealth * 2 > sourceUnit.MaxHealth) // 생존 아군이 적에게 준 양수 피해와 최대 체력 절반 확인
        { // 심장파편 적용 조건 제외 시작
            return amount; // 해당하지 않는 원래 피해 수치 보존
        } // 심장파편 적용 조건 제외 종료
        int bonus = BattleSupplyRelics.Find(RelicEffectType.LowHealthOutgoingDamage)?.EffectValue ?? 0; // 보유한 심장파편에서 원본 피해 증가율 조회
        return (int)Math.Min(int.MaxValue, (long)amount * (100L + bonus) / 100L); // 기존 원본 피해와 십오 퍼센트의 내림 및 정수 상한
    } // 저체력 아군 피해 계산 종료
    private int ApplySurvivalProtection(int incoming, bool allowDefeatWard = true, bool consume = true) // 실제 피해와 순수 예고의 불굴 및 치명 피해 성배의 적용 순서
    { // 실제 생존 보호 확인 시작
        int survivableDamage = Math.Max(0, CurrentHealth - 1); // 체력 한 점을 남기는 최대 손실량
        if (incoming > 0 && incoming >= survivableDamage && MatchesSurvivalProtection(BattleStatusEffectType.UndyingGuard, consume)) // 실제 피해와 체력 임계 및 불굴 소모 성공 확인
        { // 불굴 생존 처리 시작
            return survivableDamage; // 생존에 필요한 체력 손실 상한 반환
        } // 불굴 생존 처리 종료
        if (allowDefeatWard && incoming > survivableDamage && MatchesSurvivalProtection(BattleStatusEffectType.DefeatWard, consume)) // 처리 뒤 체력 영이 되는 피해와 성배 한 번 확인
        { // 성배 보호 발동 시작
            return survivableDamage; // 정확히 체력 한 점을 남기는 실제 피해량
        } // 성배 보호 발동 종료
        return incoming; // 보호 조건을 충족하지 않은 피해량 보존
    } // 실제 생존 보호 확인 종료

    private bool MatchesSurvivalProtection(BattleStatusEffectType type, bool consume) // 실제 소모 또는 순수 예고에서의 생존 상태 확인
    { // 개별 생존 상태 확인 시작
        return consume ? RemoveStatusEffect(type) : GetStatusEffect(type) != null; // 실피격에서만 일회 상태 제거
    } // 개별 생존 상태 확인 종료
    private BattleStatusEffectInstance GetApplicablePreparation(int damageAmount, BattleDamageType damageType, BattleUnitRuntime sourceUnit) // 공격별 방어 준비 적용 확인
    { // 준비 적용 확인 시작
        if (damageAmount <= 0 || Team != BattleTeam.Ally || sourceUnit == null || sourceUnit.Team != BattleTeam.Enemy) // 유효한 적의 피해 확인
        { // 준비 미적용 처리 시작
            return null; // 준비 효과 없음 반환
        } // 준비 미적용 처리 종료
        return GetStatusEffect(BattleStatusEffectType.GuardPreparation); // 현재 방어 준비 반환
    } // 준비 적용 확인 종료
    private bool HasActiveHealingBuff() // 회복 직전 살아 있는 기존 긍정 상태 확인
    { // 활성 강화 확인 시작
        foreach (var effect in statusEffects) // 원래 회복 대상의 정상 상태 목록 순회
        { // 개별 강화 확인 시작
            if (!effect.IsExpired && !effect.IsDebuff && (effect.EffectType == BattleStatusEffectType.Awakening || !effect.IsProtectedMentalState)) // 기존 강화 분류와 긍정 각성만 포함
            { // 실제 대상 강화 발견 시작
                return true; // 하나 이상으로 발동하는 금실 효과 확인
            } // 실제 대상 강화 발견 종료
        } // 개별 강화 확인 종료
        return false; // 활성 강화가 없는 기존 회복 확인
    } // 활성 강화 확인 종료
    public int RestoreHealth(int healAmount, BattleUnitRuntime sourceUnit = null) // 체력 회복 처리
    { // 체력 회복 시작
        if (healAmount <= 0 || IsDead || CurrentHealth >= MaxHealth) // 무효 회복 확인
        { // 무효 회복 처리 시작
            return 0; // 적용 회복 없음
        } // 무효 회복 처리 종료
        if (Team == BattleTeam.Ally && (long)CurrentHealth * 10 <= (long)MaxHealth * 3) // 전투 중 회복을 받는 아군의 체력 삼십 퍼센트 확인
        { // 저체력 회복 가산 시작
            int bonus = BattleSupplyRelics.Find(RelicEffectType.LowHealthHealing)?.EffectValue ?? 0; // 실제 보유한 은빛 심장핀의 수치 조회
            healAmount = (int)Math.Min(int.MaxValue, (long)healAmount + bonus); // 받는 회복 감소 처리 전 가산
        } // 저체력 회복 가산 종료
        var embroidery = Team == BattleTeam.Ally ? BattleSupplyRelics.Find(RelicEffectType.GoldEmbroideryBuffedHealing) : null; // 생존 아군의 실제 받는 회복 유물 조회
        int embroideryPercent = embroidery != null && HasActiveHealingBuff() ? Math.Max(0, embroidery.EffectValue) : 0; // 현재 회복 이벤트 이전 활성 강화와 실제 증가량 확보
        int increase = GetStatusEffectValue(BattleStatusEffectType.HealingReceivedUp); // 대상에 적용된 잔잔한 축복의 수신 회복 증가율
        int reduction = Math.Min(100, GetStatusEffectValue(BattleStatusEffectType.HealingReceivedDown)); // 기존 수신 회복 감소율과 최대 백 보정
        if (increase > 0 || reduction > 0 || embroideryPercent > 0) // 현재 대상의 기존 수신 보정과 금실 증가 확인
        { // 수신 회복 증가와 감소 공통 처리 시작
            healAmount = (int)Math.Min(int.MaxValue, healAmount * ((100L + increase) / 100m) * ((100 - reduction) / 100m) * ((100L + embroideryPercent) / 100m)); // 기존 수신 비율과 금실 비율의 곱을 한 번만 내림 및 정수 범위 적용
            if (healAmount == 0) // 실제 적용할 회복량이 없는 조건 확인
            { // 미회복 처리 시작
                return 0; // 기존 회복 알림과 정신력 미발동
            } // 미회복 처리 종료
        } // 수신 회복 증가와 감소 공통 처리 종료
        int previousHealth = CurrentHealth; // 변경 전 체력 저장
        CurrentHealth += Mathf.Min(healAmount, MaxHealth - CurrentHealth); // 남은 체력만큼만 회복
        int appliedHealing = CurrentHealth - previousHealth; // 실제 회복량 계산
        HealthRestored?.Invoke(this, appliedHealing); // 회복 적용 결과 알림
        HealthChanged?.Invoke(this); // 체력 변경 알림
        HealingResolved?.Invoke(sourceUnit, this, appliedHealing); // 발생자 포함 회복 완료 알림
        ChangeMental(1, BattleMentalChangeReason.HealingReceived); // 회복 정신력 증가 적용
        return appliedHealing; // 실제 회복량 반환
    } // 체력 회복 종료
    private void ApplyDamageMental(int appliedDamage, bool diedNow, BattleUnitRuntime sourceUnit) // 피해 정신력 변화 적용
    { // 피해 정신력 처리 시작
        if (appliedDamage <= 0) // 실제 피해 확인
        { // 피해 없음 처리 시작
            return; // 정신력 처리 중단
        } // 피해 없음 처리 종료
        if (sourceUnit != null && sourceUnit != this && !sourceUnit.IsDead) // 공격자 존재 확인
        { // 공격자 정신력 처리 시작
            sourceUnit.ChangeMental(1, BattleMentalChangeReason.DealDamage); // 피해 가함 정신력 증가
            if (diedNow) // 처치 여부 확인
            { // 처치 정신력 처리 시작
                sourceUnit.ChangeMental(5, BattleMentalChangeReason.DefeatEnemy); // 적 처치 정신력 증가
            } // 처치 정신력 처리 종료
        } // 공격자 정신력 처리 종료
        if (diedNow) // 사망 대상 확인
        { // 사망 대상 처리 시작
            return; // 피격 정신력 처리 중단
        } // 사망 대상 처리 종료
        ChangeMental(-1, BattleMentalChangeReason.ReceiveDamage, sourceUnit); // 실피격 정신력 감소의 실제 발생자 전달
        if (appliedDamage * 10 >= MaxHealth) // 최대 체력 십 퍼센트 피해 확인
        { // 큰 피해 처리 시작
            ChangeMental(-5, BattleMentalChangeReason.HeavyDamage); // 큰 피해 추가 감소
        } // 큰 피해 처리 종료
    } // 피해 정신력 처리 종료
    private void ApplyDisruptMental(BattleStatusEffectType effectType, BattleUnitRuntime sourceUnit) // 교란 정신력 변화 적용
    { // 교란 정신력 처리 시작
        if (effectType != BattleStatusEffectType.Disrupt && effectType != BattleStatusEffectType.Fear) // 교란 분류와 공포 상태 확인
        { // 교란 아님 처리 시작
            return; // 정신력 처리 중단
        } // 교란 아님 처리 종료
        ChangeMental(-4, BattleMentalChangeReason.DisruptReceived, sourceUnit); // 교란과 공포의 실제 발생자를 포함한 정신력 감소
        if (sourceUnit != null && sourceUnit != this && !sourceUnit.IsDead) // 교란 적용자 확인
        { // 교란 적용자 처리 시작
            sourceUnit.ChangeMental(2, BattleMentalChangeReason.DisruptInflicted); // 교란 가함 정신력 증가
        } // 교란 적용자 처리 종료
    } // 교란 정신력 처리 종료
    private void HandleMentalChanged(BattleMentalChangeResult changeResult) // 정신력 변화 전달
    { // 정신력 변화 전달 시작
        SyncMentalStatus(); // 정신 상태와 남은 턴을 상태 이상에 동기화
        MentalChanged?.Invoke(this, changeResult); // 유닛 포함 정신력 변화 알림
    } // 정신력 변화 전달 종료
    private void SyncMentalStatus() // 정신 상태를 실제 상태 이상 목록에 동기화
    { // 정신 상태 동기화 시작
        BattleStatusEffectType desired = MentalState == BattleMentalState.Awakening ? BattleStatusEffectType.Awakening : MentalState == BattleMentalState.Collapse ? BattleStatusEffectType.Collapse : BattleStatusEffectType.None; // 현재 정신 상태 종류 계산
        bool changed = false; // 상태 목록 변경 여부 초기화
        for (int index = statusEffects.Count - 1; index >= 0; index--) // 기존 상태 목록 역순 확인
        { // 기존 정신 상태 확인 시작
            BattleStatusEffectInstance effect = statusEffects[index]; // 현재 상태 조회
            if (effect.IsProtectedMentalState && effect.EffectType != desired) // 종료되거나 바뀐 정신 상태 확인
            { // 기존 정신 상태 종료 시작
                statusEffects.RemoveAt(index); // 정신 상태 만료 또는 전투 종료 반영
                changed = true; // 상태 목록 변경 기록
            } // 기존 정신 상태 종료 완료
        } // 기존 정신 상태 확인 종료
        if (desired != BattleStatusEffectType.None) // 유지할 정신 상태 확인
        { // 현재 정신 상태 동기화 시작
            BattleStatusEffectInstance current = FindStatusEffect(desired); // 기존 정신 상태 조회
            if (current == null) // 신규 정신 상태 확인
            { // 신규 정신 상태 추가 시작
                statusEffects.Add(new BattleStatusEffectInstance(desired, 1, MentalRemainingTurns, 1)); // 실제 정신 상태 이상 추가
                changed = true; // 상태 목록 변경 기록
            } // 신규 정신 상태 추가 종료
            else if (current.RemainingTurns != MentalRemainingTurns) // 남은 턴 변경 확인
            { // 정신 상태 지속 동기화 시작
                current.SyncMentalDuration(MentalRemainingTurns); // 실제 정신 상태 남은 턴 적용
                changed = true; // 상태 턴 변경 기록
            } // 정신 상태 지속 동기화 종료
        } // 현재 정신 상태 동기화 종료
        if (changed) // 실제 상태 목록 변경 확인
        { // 상태 목록 변경 알림 시작
            StatusEffectsChanged?.Invoke(this); // 상태 아이콘과 상세 갱신 알림
        } // 상태 목록 변경 알림 종료
    } // 정신 상태 동기화 종료
    internal static int ApplyPercentage(int baseAmount, int changePercent) // 직접 피해와 기존 전투 수치의 공통 비율 계산
    { // 비율 계산 시작
        int safeBaseAmount = Mathf.Max(0, baseAmount); // 음수 원본 수치 보정
        float adjusted = (long)safeBaseAmount * (100L + changePercent) / 100f; // 큰 가변 회복량도 중간 곱셈 범위 안에서 계산
        return adjusted >= int.MaxValue ? int.MaxValue : Mathf.Max(0, Mathf.RoundToInt(adjusted)); // 실제 정수 상한과 기존 반올림 보정 유지
    } // 비율 계산 종료
} // 클래스 종료
