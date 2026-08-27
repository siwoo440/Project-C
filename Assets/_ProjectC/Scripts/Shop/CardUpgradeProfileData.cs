using System; // 직렬화 기능 사용
using System.Collections.Generic; // 강화 단계 목록 사용
using UnityEngine; // ScriptableObject와 수치 보정 사용

[CreateAssetMenu(
    fileName = "CardUpgradeProfile_New",
    menuName = "Project C/Data/Card Upgrade Profile")]
public sealed class CardUpgradeProfileData : ScriptableObject // 카드별 회차 강화 프로필
{
    [SerializeField] private CardData card; // 강화 대상 카드
    [SerializeField] private List<CardUpgradeLevelData> levels = new List<CardUpgradeLevelData>(); // 단계별 강화 규칙

    public CardData Card => card; // 강화 대상 카드 조회
    public int MaximumUpgradeLevel => levels != null ? levels.Count : 0; // 카드별 최대 강화 단계 조회

    public bool TryGetLevel(int upgradeLevel, out CardUpgradeLevelData levelData) // 지정 강화 단계 데이터 조회
    {
        levelData = null; // 실패 기본값

        if (upgradeLevel < 1 ||
            levels == null ||
            upgradeLevel > levels.Count) // 강화 단계 범위 확인
        {
            return false; // 조회 실패 반환
        }

        levelData = levels[upgradeLevel - 1]; // 1단계 기반 목록 데이터 조회
        return levelData != null; // 유효 단계 데이터 여부 반환
    }
}

[Serializable]
public sealed class CardUpgradeLevelData // 카드 단일 강화 단계 규칙
{
    [SerializeField] private int effectPercentBonus = 25; // 기본 효과 최종 증가율
    [SerializeField] private int mentalPercentBonus = 25; // 정신력 변화 절대값 최종 증가율
    [SerializeField] private int apCostDelta; // AP 비용 증감
    [SerializeField] private int statusDurationDelta; // 상태 지속 횟수 증감
    [SerializeField] private int statusMaximumStacksDelta; // 상태 최대 중첩 증감

    public int EffectPercentBonus => effectPercentBonus; // 효과 증가율 조회
    public int MentalPercentBonus => mentalPercentBonus; // 정신력 증가율 조회
    public int ApCostDelta => apCostDelta; // AP 증감 조회
    public int StatusDurationDelta => statusDurationDelta; // 상태 지속 증감 조회
    public int StatusMaximumStacksDelta => statusMaximumStacksDelta; // 상태 중첩 증감 조회
}

public static class CardUpgradeProfileCatalog // 카드 강화 프로필 런타임 조회기
{
    private const int FallbackMaximumUpgradeLevel = 1; // 프로필 없는 카드 Prototype 최대 강화
    private const int FallbackEffectPercentBonus = 25; // 프로필 없는 카드 Prototype 효과 증가율

    public static int GetMaximumUpgradeLevel(CardData cardData) // 카드별 최대 강화 단계 조회
    {
        CardUpgradeProfileData profile = FindProfile(cardData); // 카드 전용 프로필 조회

        if (profile == null || profile.MaximumUpgradeLevel < 1) // 프로필 또는 단계 데이터 누락 확인
        {
            return FallbackMaximumUpgradeLevel; // 기존 1강 Prototype 규칙 유지
        }

        return profile.MaximumUpgradeLevel; // 카드별 최대 강화 단계 반환
    }

    public static int CalculateEffectValue(
        CardData cardData,
        int baseValue,
        int upgradeLevel) // 카드별 강화 효과 수치 계산
    {
        if (baseValue <= 0 || upgradeLevel <= 0) // 강화 미적용 조건 확인
        {
            return Mathf.Max(0, baseValue); // 기본 효과 수치 반환
        }

        int percentBonus = GetEffectPercentBonus(cardData, upgradeLevel); // 현재 카드 단계별 효과 증가율 조회
        return Mathf.CeilToInt(baseValue * (1f + percentBonus / 100f)); // 프로필 기반 강화 수치 반환
    }

    public static int CalculateSignedMentalValue(
        CardData cardData,
        int baseValue,
        int upgradeLevel) // 카드별 부호 포함 정신력 변화 계산
    {
        if (baseValue == 0 || upgradeLevel <= 0) // 강화 미적용 조건 확인
        {
            return baseValue; // 기본 정신력 변화 반환
        }

        int sign = baseValue < 0 ? -1 : 1; // 원본 부호 저장
        int absoluteValue = Mathf.Abs(baseValue); // 원본 절대값 계산
        int percentBonus = GetMentalPercentBonus(cardData, upgradeLevel); // 정신력 증가율 조회
        int upgradedValue = Mathf.CeilToInt(absoluteValue * (1f + percentBonus / 100f)); // 강화 절대값 계산
        return sign * upgradedValue; // 원본 부호 유지 반환
    }

    public static int CalculateApCost(
        CardData cardData,
        int baseValue,
        int upgradeLevel) // 카드별 AP 비용 계산
    {
        CardUpgradeLevelData levelData = GetLevelData(cardData, upgradeLevel); // 강화 단계 데이터 조회
        return levelData == null
            ? Mathf.Max(0, baseValue)
            : Mathf.Max(0, baseValue + levelData.ApCostDelta); // AP 비용 최소 0 보정
    }

    public static int CalculateStatusDuration(
        CardData cardData,
        int baseValue,
        int upgradeLevel) // 카드별 상태 지속 횟수 계산
    {
        CardUpgradeLevelData levelData = GetLevelData(cardData, upgradeLevel); // 강화 단계 데이터 조회
        return levelData == null
            ? Mathf.Max(0, baseValue)
            : Mathf.Max(0, baseValue + levelData.StatusDurationDelta); // 상태 지속 최소 0 보정
    }

    public static int CalculateStatusMaximumStacks(
        CardData cardData,
        int baseValue,
        int upgradeLevel) // 카드별 상태 최대 중첩 계산
    {
        CardUpgradeLevelData levelData = GetLevelData(cardData, upgradeLevel); // 강화 단계 데이터 조회
        return levelData == null
            ? Mathf.Max(0, baseValue)
            : Mathf.Max(0, baseValue + levelData.StatusMaximumStacksDelta); // 최대 중첩 최소 0 보정
    }

    private static int GetEffectPercentBonus(CardData cardData, int upgradeLevel) // 현재 강화 단계 효과 증가율 조회
    {
        CardUpgradeLevelData levelData = GetLevelData(cardData, upgradeLevel); // 단계 데이터 조회
        return levelData != null
            ? levelData.EffectPercentBonus
            : FallbackEffectPercentBonus; // 프로필 누락 시 기존 +25% 사용
    }

    private static int GetMentalPercentBonus(CardData cardData, int upgradeLevel) // 현재 강화 단계 정신력 증가율 조회
    {
        CardUpgradeLevelData levelData = GetLevelData(cardData, upgradeLevel); // 단계 데이터 조회
        return levelData != null
            ? levelData.MentalPercentBonus
            : FallbackEffectPercentBonus; // 프로필 누락 시 기존 +25% 사용
    }

    private static CardUpgradeLevelData GetLevelData(CardData cardData, int upgradeLevel) // 카드 단계 데이터 조회
    {
        CardUpgradeProfileData profile = FindProfile(cardData); // 카드 전용 프로필 조회
        if (profile == null) // 프로필 존재 확인
        {
            return null; // 단계 데이터 없음 반환
        }

        return profile.TryGetLevel(upgradeLevel, out CardUpgradeLevelData levelData)
            ? levelData
            : null; // 지정 단계 데이터 반환
    }

    private static CardUpgradeProfileData FindProfile(CardData cardData) // Resources 카드 강화 프로필 조회
    {
        if (cardData == null) // 카드 원본 확인
        {
            return null; // 프로필 없음 반환
        }

        CardUpgradeProfileData[] profiles =
            Resources.LoadAll<CardUpgradeProfileData>("CardUpgrades"); // 카드 강화 프로필 전체 로드

        foreach (CardUpgradeProfileData profile in profiles) // 전체 프로필 순회
        {
            if (profile != null && profile.Card == cardData) // 동일 카드 프로필 확인
            {
                return profile; // 카드 전용 프로필 반환
            }
        }

        return null; // 등록 프로필 없음 반환
    }
}
