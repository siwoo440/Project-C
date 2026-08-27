using UnityEngine; // 수치 보정 기능 사용

public static class RestRoomRecoveryService // 57일차 위험도별 휴식 회복 계산
{
    public const int SafeHealthRecoveryPercent = 25; // 안전 방 HP 회복률
    public const int HazardLevelOneHealthRecoveryPercent = 20; // 퇴색 Lv1 HP 회복률
    public const int HazardLevelTwoHealthRecoveryPercent = 15; // 퇴색 Lv2 HP 회복률
    public const int HazardLevelThreeHealthRecoveryPercent = 10; // 퇴색 Lv3 HP 회복률
    public const int MentalRecoveryAmount = 15; // 공통 정신력 회복량

    public static int GetHealthRecoveryPercent(int hazardLevel) // 위험도별 HP 회복률 조회
    {
        switch (Mathf.Clamp(hazardLevel, 0, 3)) // 위험도 0~3 보정
        {
            case 1:
                return HazardLevelOneHealthRecoveryPercent; // Lv1 회복률 반환

            case 2:
                return HazardLevelTwoHealthRecoveryPercent; // Lv2 회복률 반환

            case 3:
                return HazardLevelThreeHealthRecoveryPercent; // Lv3 회복률 반환

            default:
                return SafeHealthRecoveryPercent; // 안전 회복률 반환
        }
    }

    public static int CalculateRecoveredHealth(
        int currentHealth,
        int maximumHealth,
        int hazardLevel) // 위험도별 HP 회복 결과 계산
    {
        int safeMaximumHealth = Mathf.Max(1, maximumHealth); // 최대 HP 최소값 보정
        int safeCurrentHealth = Mathf.Clamp(currentHealth, 0, safeMaximumHealth); // 현재 HP 범위 보정

        if (safeCurrentHealth <= 0) // 사망 상태 확인
        {
            return 0; // 휴식 부활 차단
        }

        int recoveryPercent = GetHealthRecoveryPercent(hazardLevel); // 위험도별 회복률 조회
        int recoveryAmount = Mathf.CeilToInt(safeMaximumHealth * recoveryPercent / 100f); // 최대 HP 기준 회복량 계산
        return Mathf.Min(safeMaximumHealth, safeCurrentHealth + recoveryAmount); // 최대 HP 제한 결과 반환
    }

    public static int CalculateRecoveredHealth(
        int currentHealth,
        int maximumHealth,
        bool isHighRisk) // 기존 bool 호출 호환 회복 계산
    {
        return CalculateRecoveredHealth(
            currentHealth,
            maximumHealth,
            isHighRisk ? 2 : 0); // 기존 고위험을 Lv2로 호환
    }

    public static int CalculateRecoveredMental(
        int currentMental,
        bool isDead) // 정신력 회복 결과 계산
    {
        if (isDead) // 사망 상태 확인
        {
            return Mathf.Clamp(
                currentMental,
                BattleMentalRuntime.MinimumMental,
                BattleMentalRuntime.MaximumMental); // 사망자 정신력 변경 없음
        }

        return Mathf.Clamp(
            currentMental + MentalRecoveryAmount,
            BattleMentalRuntime.MinimumMental,
            BattleMentalRuntime.MaximumMental); // 생존자 정신력 +15 적용
    }
}
