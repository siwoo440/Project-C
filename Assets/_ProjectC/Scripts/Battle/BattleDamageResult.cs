public readonly struct BattleDamageResult // 피해 계산 결과
{ // 구조체 시작
    public int RawDamage { get; } // 원본 피해 조회
    public int DefenseValue { get; } // 적용 방어값 조회
    public int ReducedDamage { get; } // 감소 피해 조회
    public int FinalDamage { get; } // 최종 피해 조회
    public int AppliedDamage { get; } // 실제 체력 피해 조회
    public BattleDamageType DamageType { get; } // 피해 유형 조회
    public bool HasDamage => AppliedDamage > 0; // 유효 피해 여부 조회
    public BattleDamageResult(int rawDamage, int defenseValue, int finalDamage, int appliedDamage, BattleDamageType damageType) // 피해 결과 생성
    { // 생성자 시작
        RawDamage = rawDamage; // 원본 피해 저장
        DefenseValue = defenseValue; // 적용 방어값 저장
        FinalDamage = finalDamage; // 최종 피해 저장
        AppliedDamage = appliedDamage; // 실제 체력 피해 저장
        ReducedDamage = rawDamage > finalDamage ? rawDamage - finalDamage : 0; // 감소 피해 계산
        DamageType = damageType; // 피해 유형 저장
    } // 생성자 종료
    public static BattleDamageResult Empty(BattleDamageType damageType) // 빈 피해 결과 생성
    { // 빈 결과 생성 시작
        return new BattleDamageResult(0, 0, 0, 0, damageType); // 피해 없음 결과 반환
    } // 빈 결과 생성 종료
    public BattleDamageResult WithAppliedDamage(int appliedDamage) // 실제 체력 피해 적용
    { // 체력 피해 적용 시작
        int safeAppliedDamage = appliedDamage < 0 ? 0 : appliedDamage > FinalDamage ? FinalDamage : appliedDamage; // 실제 피해 범위 보정
        return new BattleDamageResult(RawDamage, DefenseValue, FinalDamage, safeAppliedDamage, DamageType); // 실제 피해 포함 결과 반환
    } // 체력 피해 적용 종료
} // 구조체 종료
