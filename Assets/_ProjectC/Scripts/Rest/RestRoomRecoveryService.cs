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

    public static int CampMentalRecovery => MentalRecoveryAmount + (RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.CampMentalBonus)?.EffectValue ?? 0); // 정수기 보유 중 실제 캠프 정신력 회복량
    public static int CampRecoveryBonus => RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.CampHealthRecoveryBonus)?.EffectValue ?? 0; // 실제 보유한 식량팩의 캠프 회복 보너스
    internal static int CampFlatHealthBonus => RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.FoldingCotRecovery)?.EffectValue ?? 0; // 접이식 침대 보유 시 고정 체력 회복량
    public static string HealthRecoveryLabel(int hazardLevel) // 캠프의 기본 회복률과 유물 보너스 표시
    { // 캠프 회복 안내 시작
        int percentBonus = CampRecoveryBonus; // 식량팩의 회복 비율 보너스 조회
        int flatBonus = CampFlatHealthBonus; // 접이식 침대의 고정 회복량 조회
        string label = GetHealthRecoveryPercent(hazardLevel) + "%"; // 최대 체력 기준 기본 회복률 문구
        if (percentBonus > 0) // 비율 회복 보너스 보유 확인
        { // 비율 회복 안내 시작
            label += $" (회복량 +{percentBonus}%)"; // 식량팩의 추가 회복 비율 표시
        } // 비율 회복 안내 종료
        return flatBonus > 0 ? $"{label} +{flatBonus}" : label; // 접이식 침대의 추가 회복량 표시
    } // 캠프 회복 안내 종료
    public static int CalculateRecoveredHealth( // 실제 휴식 회복 체력 계산
        int currentHealth, // 회복 전 보유 체력
        int maximumHealth, // 출전자의 최대 체력
        int hazardLevel, // 휴식 방의 위험도
        int bonusPercent = 0, // 기존 회복량의 비율 보너스
        int flatBonus = 0) // 비율 계산 뒤 더하는 고정 회복량
    {
        int safeMaximumHealth = Mathf.Max(1, maximumHealth); // 최대 HP 최소값 보정
        int safeCurrentHealth = Mathf.Clamp(currentHealth, 0, safeMaximumHealth); // 현재 HP 범위 보정

        if (safeCurrentHealth <= 0) // 사망 상태 확인
        {
            return 0; // 휴식 부활 차단
        }

        int recoveryPercent = GetHealthRecoveryPercent(hazardLevel); // 위험도별 회복률 조회
        int recoveryAmount = Mathf.CeilToInt(safeMaximumHealth * recoveryPercent / 100f); // 최대 HP 기준 회복량 계산
        long enhanced = (long)recoveryAmount * (100L + Mathf.Max(0, bonusPercent)) / 100L; // 기존 정수 회복량의 보너스를 파티원별로 내림
        return (int)System.Math.Min(safeMaximumHealth, safeCurrentHealth + enhanced + Mathf.Max(0, flatBonus)); // 고정 회복량 합산과 최대 체력 제한
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
        bool isDead, int recoveryAmount = MentalRecoveryAmount) // 캠프 정신력 회복 결과 계산
    {
        if (isDead) // 사망 상태 확인
        {
            return Mathf.Clamp(
                currentMental,
                BattleMentalRuntime.MinimumMental,
                BattleMentalRuntime.MaximumMental); // 사망자 정신력 변경 없음
        }

        return Mathf.Clamp(
            currentMental + Mathf.Max(0, recoveryAmount),
            BattleMentalRuntime.MinimumMental,
            BattleMentalRuntime.MaximumMental); // 생존자 정신력 +15 적용
    }
}
