using System; // 기본 수학 기능 사용
public static class BattleDamageCalculator // 공통 피해 계산기
{ // 클래스 시작
    public static BattleDamageResult Calculate(int rawDamage, BattleDamageType damageType, int physicalDefense, int magicalResistance) // 최종 피해 계산
    { // 피해 계산 시작
        int safeRawDamage = Math.Max(0, rawDamage); // 원본 피해 음수 방지
        int safePhysicalDefense = Math.Max(0, physicalDefense); // 물리 방어력 음수 방지
        int safeMagicalResistance = Math.Max(0, magicalResistance); // 마법 저항력 음수 방지
        int defenseValue = SelectDefense(damageType, safePhysicalDefense, safeMagicalResistance); // 피해 유형별 방어값 선택
        int finalDamage = safeRawDamage > 0 ? Math.Max(1, safeRawDamage - defenseValue) : 0; // 최소 피해 포함 최종값 계산
        return new BattleDamageResult(safeRawDamage, defenseValue, finalDamage, finalDamage, damageType); // 피해 계산 결과 반환
    } // 피해 계산 종료
    private static int SelectDefense(BattleDamageType damageType, int physicalDefense, int magicalResistance) // 피해 유형별 방어값 선택
    { // 방어값 선택 시작
        if (damageType == BattleDamageType.Physical) // 물리 피해 확인
        { // 물리 방어 처리 시작
            return physicalDefense; // 물리 방어력 반환
        } // 물리 방어 처리 종료
        if (damageType == BattleDamageType.Magical) // 마법 피해 확인
        { // 마법 저항 처리 시작
            return magicalResistance; // 마법 저항력 반환
        } // 마법 저항 처리 종료
        return 0; // 일반 피해 방어 없음 반환
    } // 방어값 선택 종료
} // 클래스 종료
