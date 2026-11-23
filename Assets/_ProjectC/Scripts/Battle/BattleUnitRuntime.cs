using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 자료형 사용
public sealed class BattleUnitRuntime // 전투 유닛 런타임 상태
{ // 클래스 시작
    private readonly List<BattleStatusEffectInstance> statusEffects = new List<BattleStatusEffectInstance>(); // 현재 상태 이상 목록
    public string UnitId { get; } // 유닛 고유 ID
    public string DisplayName { get; } // 유닛 표시 이름
    public BattleTeam Team { get; } // 유닛 진영
    public Sprite Portrait { get; } // 유닛 초상화
    public int MaxHealth { get; } // 최대 체력
    public int PhysicalDefense { get; } // 물리 방어력
    public int MagicalResistance { get; } // 마법 저항력
    public int CurrentHealth { get; private set; } // 현재 체력
    public bool IsDead { get; private set; } // 사망 여부
    public IReadOnlyList<BattleStatusEffectInstance> StatusEffects => statusEffects; // 현재 상태 이상 조회
    public int AttackPowerBonus => CalculateAttackPowerBonus(); // 현재 공격력 증가 수치
    public CharacterData CharacterSource { get; } // 아군 원본 데이터
    public EnemyData EnemySource { get; } // 적 원본 데이터
    public event Action<BattleUnitRuntime> HealthChanged; // 체력 변경 이벤트
    public event Action<BattleUnitRuntime, BattleDamageResult> DamageTaken; // 피해 적용 이벤트
    public event Action<BattleUnitRuntime, int> HealthRestored; // 회복 적용 이벤트
    public event Action<BattleUnitRuntime> Died; // 사망 이벤트
    public event Action<BattleUnitRuntime> StatusEffectsChanged; // 상태 이상 변경 이벤트
    private BattleUnitRuntime(string unitId, string displayName, BattleTeam team, Sprite portrait, int maxHealth, int physicalDefense, int magicalResistance, CharacterData characterSource, EnemyData enemySource) // 런타임 상태 생성자
    { // 생성자 시작
        UnitId = unitId; // 유닛 ID 저장
        DisplayName = displayName; // 표시 이름 저장
        Team = team; // 진영 저장
        Portrait = portrait; // 초상화 저장
        MaxHealth = Mathf.Max(1, maxHealth); // 최소 최대 체력 보정
        PhysicalDefense = Mathf.Max(0, physicalDefense); // 물리 방어력 보정
        MagicalResistance = Mathf.Max(0, magicalResistance); // 마법 저항력 보정
        CurrentHealth = MaxHealth; // 현재 체력 초기화
        IsDead = false; // 생존 상태 초기화
        CharacterSource = characterSource; // 아군 원본 저장
        EnemySource = enemySource; // 적 원본 저장
    } // 생성자 종료
    public static BattleUnitRuntime CreateAlly(CharacterData characterData) // 아군 런타임 생성
    { // 아군 생성 시작
        if (characterData == null) // 아군 원본 누락 확인
        { // 누락 처리 시작
            throw new ArgumentNullException(nameof(characterData)); // 잘못된 인수 예외
        } // 누락 처리 종료
        return new BattleUnitRuntime(characterData.CharacterId, characterData.DisplayName, BattleTeam.Ally, characterData.Portrait, characterData.MaxHealth, characterData.PhysicalDefense, characterData.MagicalResistance, characterData, null); // 아군 상태 반환
    } // 아군 생성 종료
    public static BattleUnitRuntime CreateEnemy(EnemyData enemyData) // 적 런타임 생성
    { // 적 생성 시작
        if (enemyData == null) // 적 원본 누락 확인
        { // 누락 처리 시작
            throw new ArgumentNullException(nameof(enemyData)); // 잘못된 인수 예외
        } // 누락 처리 종료
        return new BattleUnitRuntime(enemyData.EnemyId, enemyData.DisplayName, BattleTeam.Enemy, enemyData.Portrait, enemyData.MaxHealth, enemyData.PhysicalDefense, enemyData.MagicalResistance, null, enemyData); // 적 상태 반환
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
    public bool ApplyStatusEffect(BattleStatusEffectType effectType, int value, int duration, int maximumStacks) // 상태 이상 적용
    { // 상태 이상 적용 시작
        if (IsDead || effectType == BattleStatusEffectType.None || value <= 0 || duration <= 0 || maximumStacks <= 0) // 적용 조건 확인
        { // 적용 불가 처리 시작
            return false; // 상태 이상 적용 실패 반환
        } // 적용 불가 처리 종료
        BattleStatusEffectInstance existingEffect = FindStatusEffect(effectType); // 동일 상태 이상 조회
        if (existingEffect == null) // 신규 상태 확인
        { // 신규 상태 처리 시작
            statusEffects.Add(new BattleStatusEffectInstance(effectType, value, duration, maximumStacks)); // 신규 상태 이상 추가
        } // 신규 상태 처리 종료
        else // 기존 상태 재적용
        { // 재적용 처리 시작
            existingEffect.Refresh(value, duration, maximumStacks); // 중첩과 지속 시간 갱신
        } // 재적용 처리 종료
        StatusEffectsChanged?.Invoke(this); // 상태 이상 변경 알림
        return true; // 상태 이상 적용 성공 반환
    } // 상태 이상 적용 종료
    public bool RemoveStatusEffect(BattleStatusEffectType effectType) // 지정 상태 이상 제거
    { // 상태 이상 제거 시작
        BattleStatusEffectInstance existingEffect = FindStatusEffect(effectType); // 제거할 상태 이상 조회
        if (existingEffect == null) // 상태 이상 존재 확인
        { // 제거 불가 처리 시작
            return false; // 상태 이상 제거 실패 반환
        } // 제거 불가 처리 종료
        statusEffects.Remove(existingEffect); // 상태 이상 목록 제거
        StatusEffectsChanged?.Invoke(this); // 상태 이상 변경 알림
        return true; // 상태 이상 제거 성공 반환
    } // 상태 이상 제거 종료
    public void ProcessStatusEffectsAtPhaseStart(int round) // 진영 턴 시작 상태 이상 발동
    { // 상태 이상 발동 시작
        if (IsDead || statusEffects.Count < 1) // 발동 가능 상태 확인
        { // 발동 불가 처리 시작
            return; // 상태 이상 발동 중단
        } // 발동 불가 처리 종료
        List<BattleStatusEffectInstance> effectSnapshot = new List<BattleStatusEffectInstance>(statusEffects); // 처리 중 변경 대비 목록 복사
        foreach (BattleStatusEffectInstance statusEffect in effectSnapshot) // 상태 이상 목록 순회
        { // 개별 상태 발동 시작
            if (IsDead) // 처리 중 사망 확인
            { // 사망 처리 시작
                break; // 남은 상태 발동 중단
            } // 사망 처리 종료
            if (statusEffect.EffectType == BattleStatusEffectType.Poison) // 중독 상태 확인
            { // 중독 발동 시작
                BattleDamageResult damageResult = TakeDamage(statusEffect.EffectiveValue, BattleDamageType.None); // 방어 무시 중독 피해 적용
                Debug.Log($"[BattleStatus] {DisplayName} / 라운드 {round} / 중독 피해 {damageResult.AppliedDamage}"); // 중독 발동 결과 출력
            } // 중독 발동 종료
            else if (statusEffect.EffectType == BattleStatusEffectType.Regeneration) // 재생 상태 확인
            { // 재생 발동 시작
                int restoredHealth = RestoreHealth(statusEffect.EffectiveValue); // 재생 회복 적용
                Debug.Log($"[BattleStatus] {DisplayName} / 라운드 {round} / 재생 회복 {restoredHealth}"); // 재생 발동 결과 출력
            } // 재생 발동 종료
            statusEffect.AdvanceDuration(); // 상태 이상 지속 횟수 감소
            if (statusEffect.IsExpired) // 상태 이상 종료 확인
            { // 상태 이상 종료 처리 시작
                statusEffects.Remove(statusEffect); // 만료 상태 이상 제거
            } // 상태 이상 종료 처리 종료
        } // 개별 상태 발동 종료
        StatusEffectsChanged?.Invoke(this); // 상태 이상 발동 결과 알림
    } // 상태 이상 발동 종료
    private BattleStatusEffectInstance FindStatusEffect(BattleStatusEffectType effectType) // 지정 상태 이상 조회
    { // 상태 이상 조회 시작
        foreach (BattleStatusEffectInstance statusEffect in statusEffects) // 상태 이상 목록 순회
        { // 상태 이상 비교 시작
            if (statusEffect.EffectType == effectType) // 상태 종류 일치 확인
            { // 일치 상태 처리 시작
                return statusEffect; // 일치 상태 이상 반환
            } // 일치 상태 처리 종료
        } // 상태 이상 비교 종료
        return null; // 일치 상태 없음 반환
    } // 상태 이상 조회 종료
    private int CalculateAttackPowerBonus() // 공격력 증가 합계 계산
    { // 공격력 증가 계산 시작
        int totalBonus = 0; // 전체 공격력 증가 초기화
        foreach (BattleStatusEffectInstance statusEffect in statusEffects) // 상태 이상 목록 순회
        { // 공격력 상태 확인 시작
            if (statusEffect.EffectType == BattleStatusEffectType.AttackPowerUp) // 공격력 증가 상태 확인
            { // 공격력 증가 누적 시작
                totalBonus += statusEffect.EffectiveValue; // 전체 공격력 증가 누적
            } // 공격력 증가 누적 종료
        } // 공격력 상태 확인 종료
        return totalBonus; // 전체 공격력 증가 반환
    } // 공격력 증가 계산 종료
    private void ClearStatusEffects() // 모든 상태 이상 제거
    { // 전체 상태 제거 시작
        if (statusEffects.Count < 1) // 상태 이상 존재 확인
        { // 상태 없음 처리 시작
            return; // 전체 상태 제거 중단
        } // 상태 없음 처리 종료
        statusEffects.Clear(); // 상태 이상 목록 초기화
        StatusEffectsChanged?.Invoke(this); // 상태 이상 제거 알림
    } // 전체 상태 제거 종료
    public BattleDamageResult PreviewDamage(int damageAmount, BattleDamageType damageType) // 예상 피해 계산
    { // 예상 피해 계산 시작
        if (IsDead) // 사망 상태 확인
        { // 사망 유닛 처리 시작
            return BattleDamageResult.Empty(damageType); // 피해 없음 결과 반환
        } // 사망 유닛 처리 종료
        BattleDamageResult damageResult = BattleDamageCalculator.Calculate(damageAmount, damageType, PhysicalDefense, MagicalResistance); // 방어력 포함 피해 계산
        int expectedAppliedDamage = Mathf.Min(CurrentHealth, damageResult.FinalDamage); // 남은 체력 기준 실제 피해 계산
        return damageResult.WithAppliedDamage(expectedAppliedDamage); // 예상 체력 피해 포함 결과 반환
    } // 예상 피해 계산 종료
    public BattleDamageResult TakeDamage(int damageAmount, BattleDamageType damageType) // 피해 처리
    { // 피해 처리 시작
        if (damageAmount <= 0 || IsDead) // 무효 피해 확인
        { // 무효 처리 시작
            return BattleDamageResult.Empty(damageType); // 적용 피해 없음
        } // 무효 처리 종료
        BattleDamageResult damageResult = PreviewDamage(damageAmount, damageType); // 최종 피해 계산
        CurrentHealth = Mathf.Max(0, CurrentHealth - damageResult.AppliedDamage); // 현재 체력 감소
        bool diedNow = CurrentHealth == 0; // 이번 사망 여부 계산
        if (diedNow) // 사망 상태 확인
        { // 사망 처리 시작
            IsDead = true; // 사망 상태 저장
        } // 사망 처리 종료
        DamageTaken?.Invoke(this, damageResult); // 피해 적용 결과 알림
        HealthChanged?.Invoke(this); // 체력 변경 알림
        if (diedNow) // 신규 사망 확인
        { // 사망 알림 시작
            ClearStatusEffects(); // 사망 유닛 상태 이상 제거
            Died?.Invoke(this); // 사망 이벤트 알림
        } // 사망 알림 종료
        return damageResult; // 피해 계산 결과 반환
    } // 피해 처리 종료
    public int RestoreHealth(int healAmount) // 체력 회복 처리
    { // 체력 회복 시작
        if (healAmount <= 0 || IsDead || CurrentHealth >= MaxHealth) // 무효 회복 확인
        { // 무효 회복 처리 시작
            return 0; // 적용 회복 없음
        } // 무효 회복 처리 종료
        int previousHealth = CurrentHealth; // 변경 전 체력 저장
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healAmount); // 현재 체력 회복
        int appliedHealing = CurrentHealth - previousHealth; // 실제 회복량 계산
        HealthRestored?.Invoke(this, appliedHealing); // 회복 적용 결과 알림
        HealthChanged?.Invoke(this); // 체력 변경 알림
        return appliedHealing; // 실제 회복량 반환
    } // 체력 회복 종료
} // 클래스 종료
