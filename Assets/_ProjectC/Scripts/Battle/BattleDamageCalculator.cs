using System; // 기본 수학 기능 사용
using UnityEngine; // 반올림 기능 사용

public static class BattleDamageCalculator // 공통 피해 계산기
{
    public static BattleDamageResult Calculate(
        int rawDamage,
        BattleDamageType damageType,
        int physicalDefense,
        int magicalResistance) // 최종 피해 계산
    {
        int safeRawDamage = Math.Max(0, rawDamage); // 원본 피해 음수 방지
        int safePhysicalDefense = Math.Max(0, physicalDefense); // 물리 방어력 음수 방지
        int safeMagicalResistance = Math.Max(0, magicalResistance); // 마법 저항력 음수 방지
        int defenseValue =
            SelectDefense(
                damageType,
                safePhysicalDefense,
                safeMagicalResistance); // 피해 유형별 방어값 선택

        int defenseAdjustedDamage =
            safeRawDamage > 0
                ? Math.Max(1, safeRawDamage - defenseValue)
                : 0; // 방어 적용 피해 계산

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

        int finalDamage =
            defenseAdjustedDamage > 0
                ? Mathf.RoundToInt(
                    defenseAdjustedDamage *
                    weaknessMultiplier)
                : 0; // 약점 포함 최종 피해 계산

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
