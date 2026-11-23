public readonly struct BattleDamageResult // 피해 계산 결과
{
    public int RawDamage { get; } // 원본 피해 조회
    public int DefenseValue { get; } // 적용 방어값 조회
    public int DefenseAdjustedDamage { get; } // 방어 적용 후 피해 조회
    public int ReducedDamage { get; } // 방어 감소 피해 조회
    public int FinalDamage { get; } // 약점 포함 최종 피해 조회
    public int AppliedDamage { get; } // 실제 체력 피해 조회
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
        float weaknessMultiplier) // 약점 정보 포함 피해 결과 생성
    {
        RawDamage = rawDamage; // 원본 피해 저장
        DefenseValue = defenseValue; // 적용 방어값 저장
        DefenseAdjustedDamage = defenseAdjustedDamage; // 방어 적용 피해 저장
        FinalDamage = finalDamage; // 최종 피해 저장
        AppliedDamage = appliedDamage; // 실제 피해 저장
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
            WeaknessMultiplier); // 실제 피해 포함 결과 반환
    }
}
