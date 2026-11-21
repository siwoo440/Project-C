public readonly struct BattleDamageResult // 피해 계산 결과
{
    public int RawDamage { get; } // 원본 피해 조회
    public int DefenseValue { get; } // 적용 방어값 조회
    public int DefenseAdjustedDamage { get; } // 방어 적용 후 피해 조회
    public int ReducedDamage { get; } // 방어 감소 피해 조회
    public int FinalDamage { get; } // 약점 포함 최종 피해 조회
    public int AppliedDamage { get; } // 실제 체력 피해 조회
    public int AbsorbedDamage { get; } // 방어도 흡수 피해 조회
    public int PreparedBlock { get; } // 공격 직전 준비로 획득할 방어도
    public bool WasDamageNullified { get; } // 양수 피해가 한 번 무효화된 결과
    internal bool WasTargetDefeated { get; } // 부활 여부와 별개인 이 피해의 실제 전투불능 기록
    internal bool FromEnemyDirectAttack { get; } // 실제 적의 직접 명중과 연결된 체력 피해
    internal bool IsWandEcho { get; } // 일반 피해와 분리하는 마법 반향의 실제 출처
    public BattleDamageType DamageType { get; } // 피해 유형 조회
    public bool IsWeakness { get; } // 약점 적중 여부 조회
    public float WeaknessMultiplier { get; } // 약점 피해 배율 조회
    public int WeaknessBonusDamage =>
        FinalDamage > DefenseAdjustedDamage
            ? FinalDamage - DefenseAdjustedDamage
            : 0; // 약점 추가 피해 조회
    public bool HasDamage => AppliedDamage > 0; // 유효 피해 여부 조회

    public BattleDamageResult(
        int rawDamage,
        int defenseValue,
        int finalDamage,
        int appliedDamage,
        BattleDamageType damageType) // 기존 피해 결과 생성
        : this(
            rawDamage,
            defenseValue,
            finalDamage,
            finalDamage,
            appliedDamage,
            damageType,
            false,
            1f)
    {
    }

    public BattleDamageResult(
        int rawDamage,
        int defenseValue,
        int defenseAdjustedDamage,
        int finalDamage,
        int appliedDamage,
        BattleDamageType damageType,
        bool isWeakness,
        float weaknessMultiplier, // 약점 피해 배율 전달
        int absorbedDamage = 0, // 방어도 흡수량 전달
        int preparedBlock = 0, // 준비 방어도 전달
        bool wasDamageNullified = false, // 피해 무효화 여부 전달
        bool fromEnemyDirectAttack = false, // 직접 적 공격에서 발생한 실제 피해 여부 전달
        bool wasTargetDefeated = false, // 이 피해가 일으킨 실제 전투불능 여부 전달
        bool isWandEcho = false) // 상태와 정신력 추가 적용을 제외하는 실제 반향 출처 전달
    {
        RawDamage = rawDamage; // 원본 피해 저장
        DefenseValue = defenseValue; // 적용 방어값 저장
        DefenseAdjustedDamage = defenseAdjustedDamage; // 방어 적용 피해 저장
        FinalDamage = finalDamage; // 최종 피해 저장
        AppliedDamage = appliedDamage; // 실제 피해 저장
        AbsorbedDamage = absorbedDamage; // 방어도 흡수량 저장
        PreparedBlock = preparedBlock; // 준비 방어도 저장
        WasDamageNullified = wasDamageNullified; // 피해 무효화 여부 저장
        WasTargetDefeated = wasTargetDefeated; // 최종 체력과 별개인 실제 전투불능 기록 저장
        FromEnemyDirectAttack = fromEnemyDirectAttack; // 실제 직접 명중 출처 저장
        IsWandEcho = isWandEcho; // 공용 피해 사건에서 쓸 실제 반향 출처 저장
        ReducedDamage =
            rawDamage > defenseAdjustedDamage
                ? rawDamage - defenseAdjustedDamage
                : 0; // 방어 감소 피해 계산
        DamageType = damageType; // 피해 유형 저장
        IsWeakness = isWeakness; // 약점 여부 저장
        WeaknessMultiplier =
            weaknessMultiplier < 1f
                ? 1f
                : weaknessMultiplier; // 약점 배율 저장
    }

    public static BattleDamageResult Empty(
        BattleDamageType damageType) // 빈 피해 결과 생성
    {
        return new BattleDamageResult(
            0,
            0,
            0,
            0,
            0,
            damageType,
            false,
            1f); // 피해 없음 결과 반환
    }

    public BattleDamageResult WithAppliedDamage(
        int appliedDamage) // 실제 체력 피해 적용
    {
        int safeAppliedDamage =
            appliedDamage < 0
                ? 0
                : appliedDamage > FinalDamage
                    ? FinalDamage
                    : appliedDamage; // 실제 피해 범위 보정

        return new BattleDamageResult(
            RawDamage,
            DefenseValue,
            DefenseAdjustedDamage,
            FinalDamage,
            safeAppliedDamage,
            DamageType,
            IsWeakness,
            WeaknessMultiplier, // 약점 배율 유지
            AbsorbedDamage, // 기존 흡수량 유지
            PreparedBlock, // 기존 준비 방어도 유지
            WasDamageNullified, // 기존 피해 무효 기록 유지
            FromEnemyDirectAttack, // 체력 피해 조정에서 기존 공격 출처 유지
            WasTargetDefeated, // 같은 피해의 확정된 전투불능 기록 유지
            IsWandEcho); // 피해 조정 뒤에도 반향 출처 유지
    }
    internal BattleDamageResult WithWandEcho() // 마법 반향에서 나온 실제 피해 표시
    { // 반향 출처 표시 시작
        return new BattleDamageResult(RawDamage, DefenseValue, DefenseAdjustedDamage, FinalDamage, AppliedDamage, DamageType, IsWeakness, WeaknessMultiplier, AbsorbedDamage, PreparedBlock, WasDamageNullified, FromEnemyDirectAttack, WasTargetDefeated, isWandEcho: true); // 전체 기존 계산 및 실제 사망과 새로운 피해 출처 보존
    } // 반향 출처 표시 종료
    internal BattleDamageResult FromResolvedEnemyAttack() // 확정된 적 공격 출처가 포함된 실제 피해 결과 생성
    { // 직접 공격 피해 출처 지정 시작
        return new BattleDamageResult(RawDamage, DefenseValue, DefenseAdjustedDamage, FinalDamage, AppliedDamage, DamageType, IsWeakness, WeaknessMultiplier, AbsorbedDamage, PreparedBlock, WasDamageNullified, fromEnemyDirectAttack: true, wasTargetDefeated: WasTargetDefeated, isWandEcho: IsWandEcho); // 전투불능과 독립적인 실제 피해 출처 보존
    } // 직접 공격 피해 출처 지정 종료
    internal BattleDamageResult WithDefeatedTarget() // 이번 피해로 실제 전투불능이 된 결과 기록
    { // 전투불능 피해 기록 시작
        return new BattleDamageResult(RawDamage, DefenseValue, DefenseAdjustedDamage, FinalDamage, AppliedDamage, DamageType, IsWeakness, WeaknessMultiplier, AbsorbedDamage, PreparedBlock, WasDamageNullified, FromEnemyDirectAttack, wasTargetDefeated: true, isWandEcho: IsWandEcho); // 기존 출처 및 피해 내역과 실제 전투불능 저장
    } // 전투불능 피해 기록 종료
    internal BattleDamageResult WithoutDamage() // 체력과 방어도 손실이 모두 없는 피해 무효 결과
    { // 피해 무효 결과 작성 시작
        return new BattleDamageResult(RawDamage, DefenseValue, DefenseAdjustedDamage, 0, 0, DamageType, IsWeakness, WeaknessMultiplier, wasDamageNullified: true, fromEnemyDirectAttack: FromEnemyDirectAttack, isWandEcho: IsWandEcho); // 원본 정보와 무효 상태 및 기존 출처의 피해 반환
    } // 피해 무효 결과 작성 종료
    public BattleDamageResult WithBlock(int block, int currentHealth, int preparedBlock = 0, int beforeBlockReduction = 0) // 보호 효과와 방어도 및 체력 적용
    { // 방어도 반영 시작
        int final = System.Math.Max(0, FinalDamage - System.Math.Max(0, beforeBlockReduction)); // 물리 방어와 저항 계산 뒤 코일의 고정 감소
        int absorbed = System.Math.Min(System.Math.Max(0, block), final); // 실제 흡수량 계산
        int applied = System.Math.Min(System.Math.Max(0, currentHealth), final - absorbed); // 실제 체력 피해 계산
        return new BattleDamageResult(RawDamage, DefenseValue, DefenseAdjustedDamage, final, applied, DamageType, IsWeakness, WeaknessMultiplier, absorbed, preparedBlock, fromEnemyDirectAttack: FromEnemyDirectAttack, isWandEcho: IsWandEcho); // 기존 출처, 반향 및 흡수량과 준비 방어도 포함
    } // 방어도 반영 종료
}
