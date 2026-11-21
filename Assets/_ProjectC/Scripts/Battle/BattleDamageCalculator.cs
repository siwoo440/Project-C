using System; // 기본 수학 기능 사용

public static class BattleDamageCalculator // 공통 피해 계산기
{
    public static BattleDamageResult Calculate(
        int rawDamage,
        BattleDamageType damageType,
        int physicalDefense,
        int magicalResistance, // 일반 마법 저항 수치 전달
        int magicalResistanceLossPoints = 0) // 일반 저항과 독립인 마법 피해 경감 퍼센트포인트 감소
    {
        int safeRawDamage = Math.Max(0, rawDamage); // 원본 피해 음수 방지
        int safePhysicalDefense = Math.Max(0, physicalDefense); // 물리 방어력 음수 방지
        int safeMagicalResistance = Math.Max(0, magicalResistance); // 마법 저항력 음수 방지
        int defenseValue =
            SelectDefense(
                damageType,
                safePhysicalDefense,
                safeMagicalResistance); // 피해 유형별 방어값 선택

        double defenseAdjustedValue = (double)safeRawDamage * 100d / (100d + defenseValue); // 확정 방어 비율 적용값 계산
        if (damageType == BattleDamageType.Magical && magicalResistanceLossPoints > 0) // 실금석의 마법 피해 전용 퍼센트포인트 확인
        { // 마법 피해 경감 비율 감소 시작
            long denominator = 100L + defenseValue; // 일반 마법 저항이 결정한 기존 피해 비율의 분모
            long numerator = 10_000L + denominator * Math.Min(100, magicalResistanceLossPoints); // 기존 비율에 원본 대비 손실된 퍼센트포인트 합산
            defenseAdjustedValue = (double)safeRawDamage * numerator / (100L * denominator); // 저항 영에서도 추가 피해를 허용하는 마법 피해 계산
        } // 마법 피해 경감 비율 감소 종료
        int defenseAdjustedDamage = safeRawDamage > 0 ? (int)Math.Clamp(Math.Floor(defenseAdjustedValue), 1d, int.MaxValue) : 0; // 정수 상한을 보존한 표시용 방어 적용 피해

        CardInstance card; // 카드 피해 문맥 카드
        BattleUnitRuntime target; // 카드 피해 문맥 대상
        bool hasCardContext =
            BattleCardDamageContext.TryConsume(
                out card,
                out target); // 현재 카드 피해 문맥 조회

        bool isWeakness =
            hasCardContext &&
            card != null &&
            target != null &&
            BattleWeaknessCalculator.IsWeakness(
                card.CardType,
                target.EnemySource); // 카드 속성과 적 약점 비교

        float weaknessMultiplier =
            isWeakness
                ? BattleWeaknessCalculator.WeaknessDamageMultiplier
                : 1f; // 약점 피해 배율 결정

        double multipliedDamage = defenseAdjustedValue * weaknessMultiplier; // 반올림 전 약점 배율 적용
        int finalDamage = safeRawDamage <= 0 ? 0 : multipliedDamage >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)Math.Floor(multipliedDamage)); // 확정 피해식의 최종 버림과 최소 피해 적용

        BattleDamageResult result =
            new BattleDamageResult(
                safeRawDamage,
                defenseValue,
                defenseAdjustedDamage,
                finalDamage,
                finalDamage,
                damageType,
                isWeakness,
                weaknessMultiplier); // 피해 계산 결과 생성

        if (hasCardContext && card != null && target != null)
        {
            BattleDamageDebugView.EnsureInstance().RecordCardDamage(
                card,
                target,
                result); // 카드 피해 계산식 기록
        }

        return result; // 피해 계산 결과 반환
    }

    private static int SelectDefense(
        BattleDamageType damageType,
        int physicalDefense,
        int magicalResistance) // 피해 유형별 방어값 선택
    {
        if (damageType == BattleDamageType.Physical)
        {
            return physicalDefense; // 물리 방어력 반환
        }

        if (damageType == BattleDamageType.Magical)
        {
            return magicalResistance; // 마법 저항력 반환
        }

        return 0; // 일반 피해 방어 없음 반환
    }
}
