using UnityEngine; // 수치 보정 기능 사용

public static class RestRoomRecoveryService // Prototype 휴식 회복 계산 서비스
{
    public const int NormalHealthRecoveryPercent = 25; // 일반 지역 HP 회복 비율
    public const int HighRiskHealthRecoveryPercent = 15; // 고위험 지역 HP 회복 비율
    public const int MentalRecoveryAmount = 15; // 정신력 고정 회복량

    public static int CalculateRecoveredHealth(int currentHealth, int maximumHealth, bool isHighRisk) // 휴식 후 체력 계산
    {
        int safeMaximumHealth = Mathf.Max(1, maximumHealth); // 최대 체력 최소값 보정
        int safeCurrentHealth = Mathf.Clamp(currentHealth, 0, safeMaximumHealth); // 현재 체력 범위 보정

        if (safeCurrentHealth <= 0) // 사망 캐릭터 확인
        {
            return 0; // 휴식 부활 차단
        }

        int recoveryPercent = isHighRisk
            ? HighRiskHealthRecoveryPercent
            : NormalHealthRecoveryPercent; // 위험도별 회복 비율 선택

        int recoveryAmount = Mathf.CeilToInt(safeMaximumHealth * recoveryPercent / 100f); // 최대 HP 기준 회복량 계산
        return Mathf.Min(safeMaximumHealth, safeCurrentHealth + recoveryAmount); // 최대 체력 제한 회복 결과 반환
    }

    public static int CalculateRecoveredMental(int currentMental, bool isDead) // 휴식 후 정신력 계산
    {
        int safeCurrentMental = Mathf.Clamp(
            currentMental,
            BattleMentalRuntime.MinimumMental,
            BattleMentalRuntime.MaximumMental); // 현재 정신력 범위 보정

        if (isDead) // 사망 캐릭터 확인
        {
            return safeCurrentMental; // 사망 캐릭터 정신력 유지
        }

        return Mathf.Clamp(
            safeCurrentMental + MentalRecoveryAmount,
            BattleMentalRuntime.MinimumMental,
            BattleMentalRuntime.MaximumMental); // 정신력 +15와 최대치 제한
    }
}
