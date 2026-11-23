using System.Collections.Generic; // 적 목록 자료형 사용
using UnityEngine; // ScriptableObject와 수치 보정 기능 사용

[CreateAssetMenu(
    fileName = "Encounter_New",
    menuName = "Project C/Exploration/Encounter")]
public sealed class EncounterData : ScriptableObject // 탐사 조우 원본 데이터
{
    [Header("기본 정보")]
    [SerializeField] private string encounterId; // 조우 고유 ID
    [SerializeField] private string displayName; // 조우 표시 이름
    [SerializeField] private BattleType battleType = BattleType.Normal; // 전투 유형
    [SerializeField] private ExplorationRegion region = ExplorationRegion.Winterfall; // 기존 기록과 새 지역 자료의 실제 지역 지정
    [Min(0)] // 조우 목록 버전 최소값
    [SerializeField] private int minimumEncounterPoolVersion; // 조우가 처음 추가된 목록 버전
    [SerializeField] private bool legacyOnly; // 이전 탐사에서만 사용하는 임시 조우 표시
    [Min(0)] // 조우 제외 버전 최소값
    [SerializeField] private int removedAtEncounterPoolVersion; // 해당 버전부터 조우 목록에서 제외

    [Header("탐사 배치")]
    [SerializeField] private Vector2 explorationPosition; // 기존 탐사 위치 호환값

    [Header("전투 적")]
    [SerializeField] private List<EnemyData> enemies =
        new List<EnemyData>(); // 조우 적 목록

    [Header("클리어 보상")]
    [Min(0)]
    [SerializeField] private int characterExperienceReward = 10; // 기본 경험치 보상

    [Min(0)]
    [SerializeField] private int goldReward = 50; // 기본 골드 보상

    [Min(0)]
    [SerializeField] private int screwReward = 25; // 기본 나사 보상

    [Min(0)]
    [SerializeField] private int ironPlateReward = 20; // 기본 철판 보상

    [Min(0)]
    [SerializeField] private int wireReward = 15; // 기본 전선 보상

    public string EncounterId => encounterId; // 조우 ID 조회
    public string DisplayName => displayName; // 조우 이름 조회
    public BattleType BattleType => battleType; // 전투 유형 조회
    public ExplorationRegion Region => region; // 원본 자료가 지정한 지역 조회
    public bool IsAvailableForPoolVersion(int version) // 저장된 조우 목록 버전 호환 확인
    { // 조우 버전 확인 시작
        return version >= 0 && minimumEncounterPoolVersion <= version && (!legacyOnly || version == 0) && (removedAtEncounterPoolVersion == 0 || version < removedAtEncounterPoolVersion); // 저장 버전의 조우 추가와 제거 범위 확인
    } // 조우 버전 확인 종료
    public Vector2 ExplorationPosition => explorationPosition; // 기존 위치 조회
    public IReadOnlyList<EnemyData> Enemies => enemies; // 적 목록 조회

    public int CharacterExperienceReward =>
        GetScaledReward(
            characterExperienceReward); // 현재 층 경험치 보상 조회

    public int GoldReward =>
        GetScaledReward(
            goldReward); // 현재 층 골드 보상 조회

    public int ScrewReward =>
        GetScaledReward(
            screwReward); // 현재 층 나사 보상 조회

    public int IronPlateReward =>
        GetScaledReward(
            ironPlateReward); // 현재 층 철판 보상 조회

    public int WireReward =>
        GetScaledReward(
            wireReward); // 현재 층 전선 보상 조회

    public bool IsValidData() // 조우 데이터 유효성 확인
    {
        if (string.IsNullOrWhiteSpace(encounterId) ||
            enemies == null ||
            enemies.Count == 0)
        {
            return false;
        }

        for (int index = 0;
             index < enemies.Count;
             index++)
        {
            if (enemies[index] == null)
            {
                return false;
            }
        }

        return true;
    }

    private int GetScaledReward(
        int baseReward) // 현재 탐사 층·조우 등급 보상 계산
    {
        int currentFloor =
            ExplorationDifficultyCalculator.GetCurrentExplorationFloor(); // 현재 탐사 층 조회

        return ExplorationDifficultyCalculator.ScaleReward(
            Mathf.Max(
                0,
                baseReward),
            currentFloor,
            battleType); // 층과 조우 등급을 결합한 최종 보상 반환
    }
}

public static class ExplorationDifficultyCalculator // 43일차 층·조우 등급 난이도 계산
{
    private const float EliteHealthMultiplier = 1.50f; // 엘리트 체력 배율
    private const float EliteAttackMultiplier = 1.20f; // 엘리트 공격 배율
    private const float EliteRewardMultiplier = 1.50f; // 엘리트 보상 배율
    private const float BossHealthMultiplier = 2.50f; // 보스 체력 배율
    private const float BossAttackMultiplier = 1.35f; // 보스 공격 배율
    private const float BossRewardMultiplier = 2.50f; // 보스 보상 배율
    private const float HealthGrowthPerFloor = 0.12f; // 층당 적 체력 증가율
    private const float AttackGrowthPerFloor = 0.08f; // 층당 적 공격 증가율
    private const float RewardGrowthPerFloor = 0.05f; // 층당 보상 증가율
    private static readonly double[] ZoneLevelStatMultipliers = { 1d, 1.10d, 1.265d, 1.518d, 1.8975d }; // 지역 레벨 1에서 5까지 앞 단계에 누적하는 적 능력치 배율

    public static int GetCurrentExplorationFloor() // 현재 탐사 층 조회
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.Instance; // 현재 탐사 세션 조회

        if (sessionManager == null)
        {
            return 1;
        }

        return NormalizeFloor(
            sessionManager.CurrentFloor); // 최소 1층 보정 후 반환
    }

    public static int GetCurrentBattleFloor() // 현재 탐사 조우 전투 층 조회
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.Instance; // 현재 탐사 세션 조회

        if (sessionManager == null ||
            sessionManager.ActiveEncounter == null)
        {
            return 1;
        }

        return NormalizeFloor(
            sessionManager.CurrentFloor); // 활성 탐사 조우의 층 반환
    }

    public static int GetCurrentBattleZoneLevel() // 실제 탐사 조우의 저장된 출정 난이도
    { // 현재 전투 난이도 조회 시작
        var session = ExplorationSessionManager.Instance; // 실제 현재 탐사 회차와 전투 원본
        return session != null && session.ActiveEncounter != null ? Mathf.Max(1, session.CurrentZoneLevel) : 1; // 기존 직접 전투 및 이전 저장은 기본 레벨
    } // 현재 전투 난이도 조회 종료

    public static double GetZoneLevelStatMultiplier(int zoneLevel) // 기획서의 레벨별 누적 적 수치 배율
    { // 선택 레벨의 안전한 배율 계산 시작
        int level = System.Math.Max(1, zoneLevel); // 기존 기본값과 올바른 양수 출정 레벨
        int definedLevels = ZoneLevelStatMultipliers.Length; // 다섯 번째 레벨까지의 고정 증가 단계
        return level <= definedLevels ? ZoneLevelStatMultipliers[level - 1] : // 1·2·3·4·5레벨의 원래 누적 성장
            System.Math.Min(int.MaxValue, ZoneLevelStatMultipliers[definedLevels - 1] * System.Math.Pow(1.10d, level - definedLevels)); // 6레벨 이후 한 단계마다 증가와 안전한 수치 상한
    } // 선택 레벨의 안전한 배율 계산 종료

    public static BattleType GetCurrentBattleType() // 현재 탐사 조우 등급 조회
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.Instance; // 현재 탐사 세션 조회

        if (sessionManager == null ||
            sessionManager.ActiveEncounter == null)
        {
            return BattleType.Normal;
        }

        return sessionManager.ActiveEncounter.BattleType; // 활성 조우의 전투 등급 반환
    }

    public static float GetEncounterHealthMultiplier(
        BattleType battleType) // 조우 등급 체력 배율 조회
    {
        switch (battleType)
        {
            case BattleType.Elite:
                return EliteHealthMultiplier;

            case BattleType.Boss:
                return BossHealthMultiplier;

            default:
                return 1f;
        }
    }

    public static float GetEncounterAttackMultiplier(
        BattleType battleType) // 조우 등급 공격 배율 조회
    {
        switch (battleType)
        {
            case BattleType.Elite:
                return EliteAttackMultiplier;

            case BattleType.Boss:
                return BossAttackMultiplier;

            default:
                return 1f;
        }
    }

    public static float GetEncounterRewardMultiplier(
        BattleType battleType) // 조우 등급 보상 배율 조회
    {
        switch (battleType)
        {
            case BattleType.Elite:
                return EliteRewardMultiplier;

            case BattleType.Boss:
                return BossRewardMultiplier;

            default:
                return 1f;
        }
    }

    public static float GetCombinedHealthMultiplier(
        int floor,
        BattleType battleType) // 층·등급 최종 체력 배율 계산
    {
        return GetHealthMultiplier(floor) *
               GetEncounterHealthMultiplier(battleType);
    }

    public static float GetCombinedAttackMultiplier(
        int floor,
        BattleType battleType) // 층·등급 최종 공격 배율 계산
    {
        return GetAttackMultiplier(floor) *
               GetEncounterAttackMultiplier(battleType);
    }

    public static float GetCombinedRewardMultiplier(
        int floor,
        BattleType battleType) // 층·등급 최종 보상 배율 계산
    {
        return GetRewardMultiplier(floor) *
               GetEncounterRewardMultiplier(battleType);
    }

    public static float GetHealthMultiplier(
        int floor) // 적 최대 체력 배율 계산
    {
        int difficultyStep =
            NormalizeFloor(floor) - 1; // 1층 기준 난이도 단계 계산

        return 1f +
               difficultyStep *
               HealthGrowthPerFloor; // 층별 체력 배율 반환
    }

    public static float GetAttackMultiplier(
        int floor) // 적 공격력 배율 계산
    {
        int difficultyStep =
            NormalizeFloor(floor) - 1; // 1층 기준 난이도 단계 계산

        return 1f +
               difficultyStep *
               AttackGrowthPerFloor; // 층별 공격 배율 반환
    }

    public static float GetRewardMultiplier(
        int floor) // 클리어 보상 배율 계산
    {
        int difficultyStep =
            NormalizeFloor(floor) - 1; // 1층 기준 난이도 단계 계산

        return 1f +
               difficultyStep *
               RewardGrowthPerFloor; // 층별 보상 배율 반환
    }

    public static int ScaleHealth(
        int baseHealth,
        int floor) // 기존 층 체력 계산 호환
    {
        return ScaleHealth(
            baseHealth,
            floor,
            BattleType.Normal); // 일반 조우 기준 반환
    }

    public static int ScaleHealth(
        int baseHealth,
        int floor,
        BattleType battleType) // 기존 층과 등급 체력 호출 호환
    { // 기본 지역 난이도 체력 경로 시작
        return ScaleHealth(baseHealth, floor, battleType, 1); // 기존 레벨 1에서 같은 층 및 등급 스케일 사용
    } // 기본 지역 난이도 체력 경로 종료

    public static int ScaleHealth(int baseHealth, int floor, BattleType battleType, int zoneLevel) // 실제 층과 등급 및 출정 레벨의 적 체력
    { // 적 최대 체력의 단계 배율 계산 시작
        int safeBaseHealth =
            Mathf.Max(
                1,
                baseHealth); // 최소 기본 체력 보정

        float nativeHealth = safeBaseHealth * GetCombinedHealthMultiplier(floor, battleType); // 원래 층과 조우 등급의 반올림 전 체력
        return RoundScaledStat(nativeHealth, zoneLevel); // 지역 레벨을 적용한 실제 최대 체력과 정수 상한
    }

    public static int ScaleAttack(
        int baseAttack,
        int floor) // 기존 층 공격 계산 호환
    {
        return ScaleAttack(
            baseAttack,
            floor,
            BattleType.Normal); // 일반 조우 기준 반환
    }

    public static int ScaleAttack(
        int baseAttack,
        int floor,
        BattleType battleType) // 기존 층과 등급 공격 호출 호환
    { // 기본 지역 난이도 공격 경로 시작
        return ScaleAttack(baseAttack, floor, battleType, 1); // 기존 레벨 1의 일반 공격과 행동 패턴 보존
    } // 기본 지역 난이도 공격 경로 종료

    public static int ScaleAttack(int baseAttack, int floor, BattleType battleType, int zoneLevel) // 실제 층과 등급 및 지역 레벨의 공격 수치
    { // 적의 네이티브 공격 예고와 실제 수치 계산 시작
        if (baseAttack <= 0) // 행동 원본에 양수 공격이 설정되지 않은 경우
        { // 무피해 원본 처리 시작
            return 0; // 원래 영 공격을 배율로 늘려 표시하지 않는 결과
        } // 무피해 원본 처리 종료
        float nativeAttack = baseAttack * GetCombinedAttackMultiplier(floor, battleType); // 기존 층과 등급의 반올림 전 공격 값
        return RoundScaledStat(nativeAttack, zoneLevel); // 같은 배율과 상한을 적용하는 실제 공격 수치
    } // 적의 네이티브 공격 예고와 실제 수치 계산 종료

    private static int RoundScaledStat(float nativeStat, int zoneLevel) // 기존 원본 스케일 뒤 한 번 반올림한 안전한 적 수치
    { // 레벨 배율 정수 변환 시작
        double stat = nativeStat * GetZoneLevelStatMultiplier(zoneLevel); // 층 및 조우 등급의 원래 수치에 지역 레벨 증가율 적용
        return (int)System.Math.Clamp(System.Math.Round(stat), 1d, int.MaxValue); // 동률 반올림과 실제 정수 범위 보호
    } // 레벨 배율 정수 변환 종료

    public static int ScaleReward(
        int baseReward,
        int floor) // 기존 층 보상 계산 호환
    {
        return ScaleReward(
            baseReward,
            floor,
            BattleType.Normal); // 일반 조우 기준 반환
    }

    public static int ScaleReward(
        int baseReward,
        int floor,
        BattleType battleType) // 층·등급 최종 보상 적용
    {
        if (baseReward <= 0)
        {
            return 0;
        }

        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                baseReward *
                GetCombinedRewardMultiplier(
                    floor,
                    battleType))); // 층·등급 보정 보상 수치 반환
    }

    private static int NormalizeFloor(
        int floor) // 탐사 층 최소값 보정
    {
        return Mathf.Max(
            1,
            floor); // 1층 이상 값 반환
    }
}
