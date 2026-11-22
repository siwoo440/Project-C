using System; // 기본 수학 기능 사용

public static class BattleDamageCalculator // 공통 피해 계산기
{
    public static BattleDamageResult Calculate(
        int rawDamage,
        BattleDamageType damageType,
        int physicalDefense,
        int magicalResistance, // 일반 마법 저항 수치 전달
        int magicalResistanceLossPoints = 0, // 일반 저항과 독립인 마법 피해 경감의 부호 있는 퍼센트포인트
        int magicalResistancePenetrationPercent = 0, // 이번 마법 타격에서 무시하는 현재 마법 저항 비율
        int incomingDamagePercent = 0) // 방어 계산 뒤 실제 수신 유물의 별개 배율
    {
        int safeRawDamage = Math.Max(0, rawDamage); // 원본 피해 음수 방지
        int safePhysicalDefense = Math.Max(0, physicalDefense); // 물리 방어력 음수 방지
        int penetration = Math.Clamp(magicalResistancePenetrationPercent, 0, 100); // 현재 마법 저항에 적용할 안전한 백분율 범위
        int safeMagicalResistance = (int)((long)Math.Max(0, magicalResistance) * (100 - penetration) / 100); // 원래 대상의 현재 마법 저항에만 곱해 내림한 실제 저항
        int defenseValue =
            SelectDefense(
                damageType,
                safePhysicalDefense,
                safeMagicalResistance); // 피해 유형별 방어값 선택

        double defenseAdjustedValue = (double)safeRawDamage * 100d / (100d + defenseValue); // 확정 방어 비율 적용값 계산
        if (damageType == BattleDamageType.Magical && magicalResistanceLossPoints != 0) // 마법 피해에만 실제 원본 대비 부호 있는 퍼센트포인트 적용
        { // 일반 저항과 원본 대비 두 방향 피해 보정 시작
            long denominator = 100L + defenseValue; // 관통 뒤 현재 마법 저항이 결정한 실제 원래 피해 분모
            long numerator = 10_000L + denominator * Math.Clamp(magicalResistanceLossPoints, -100, 100); // 원본 손실과 증가 모두 정식 백 퍼센트포인트 범위 내 가감
            defenseAdjustedValue = (double)safeRawDamage * numerator / (100L * denominator); // 저항 영에도 마법 피해가 원본 수치 기준으로 가감되는 현재 실제 비율
        } // 일반 저항과 원본 대비 두 방향 피해 보정 종료
        defenseAdjustedValue *= Math.Max(0L, 100L + incomingDamagePercent) / 100d; // 기존 방어와 저항 뒤 아직 내림하지 않은 수신 피해의 별도 비율
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
