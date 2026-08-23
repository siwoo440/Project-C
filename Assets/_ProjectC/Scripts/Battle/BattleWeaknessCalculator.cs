public static class BattleWeaknessCalculator // 카드 속성 약점 계산기
{
    public const float WeaknessDamageMultiplier = 1.5f; // 46일차 테스트용 약점 배율

    public static bool IsWeakness(
        CardType cardType,
        EnemyData enemyData) // 카드 속성 약점 판정
    {
        return enemyData != null &&
               enemyData.HasWeakness(cardType); // 적 약점 목록과 카드 속성 비교
    }

    public static float GetDamageMultiplier(
        CardType cardType,
        EnemyData enemyData) // 카드 속성 피해 배율 조회
    {
        return IsWeakness(cardType, enemyData)
            ? WeaknessDamageMultiplier
            : 1f; // 약점 여부별 피해 배율 반환
    }
}
