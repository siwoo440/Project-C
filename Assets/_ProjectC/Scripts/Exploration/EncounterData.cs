using System.Collections.Generic; // 적 목록 자료형 사용
using UnityEngine; // ScriptableObject와 수치 보정 기능 사용
using UnityEngine.SceneManagement; // 전투 Scene 자동 감지 기능 사용

[CreateAssetMenu(
    fileName = "Encounter_New",
    menuName = "Project C/Exploration/Encounter")]
public sealed class EncounterData : ScriptableObject // 탐사 조우 원본 데이터
{
    [Header("기본 정보")]
    [SerializeField] private string encounterId; // 조우 고유 ID
    [SerializeField] private string displayName; // 조우 표시 이름
    [SerializeField] private BattleType battleType = BattleType.Normal; // 전투 유형

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

    private static int GetScaledReward(
        int baseReward) // 현재 탐사 층 보상 계산
    {
        int currentFloor =
            ExplorationDifficultyCalculator.GetCurrentExplorationFloor(); // 현재 탐사 층 조회

        return ExplorationDifficultyCalculator.ScaleReward(
            Mathf.Max(
                0,
                baseReward),
            currentFloor); // 직렬화 원본을 변경하지 않고 최종 보상 반환
    }
}

public static class ExplorationDifficultyCalculator // 42일차 탐사 층 난이도 계산
{
    private const float HealthGrowthPerFloor = 0.12f; // 층당 적 체력 증가율
    private const float AttackGrowthPerFloor = 0.08f; // 층당 적 공격 증가율
    private const float RewardGrowthPerFloor = 0.05f; // 층당 보상 증가율

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
        int floor) // 기본 체력에 층 배율 적용
    {
        int safeBaseHealth =
            Mathf.Max(
                1,
                baseHealth); // 최소 기본 체력 보정

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                safeBaseHealth *
                GetHealthMultiplier(floor))); // 보정된 최대 체력 반환
    }

    public static int ScaleAttack(
        int baseAttack,
        int floor) // 기본 공격력에 층 배율 적용
    {
        if (baseAttack <= 0)
        {
            return 0;
        }

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseAttack *
                GetAttackMultiplier(floor))); // 보정된 공격 수치 반환
    }

    public static int ScaleReward(
        int baseReward,
        int floor) // 기본 보상에 층 배율 적용
    {
        if (baseReward <= 0)
        {
            return 0;
        }

        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                baseReward *
                GetRewardMultiplier(floor))); // 보정된 보상 수치 반환
    }

    private static int NormalizeFloor(
        int floor) // 탐사 층 최소값 보정
    {
        return Mathf.Max(
            1,
            floor); // 1층 이상 값 반환
    }
}

[DefaultExecutionOrder(500)]
public sealed class ExplorationBattleDifficultyRuntime : MonoBehaviour // 탐사 전투 적 체력 난이도 적용
{
    private readonly System.Collections.Generic.HashSet<BattleUnitRuntime> scaledEnemies =
        new System.Collections.Generic.HashSet<BattleUnitRuntime>(); // 이미 난이도를 적용한 적 목록

    private BattleSceneSetup battleSceneSetup; // 현재 전투 구성 관리자
    private bool profileLogged; // 현재 층 난이도 로그 출력 여부

    private void Start() // 전투 난이도 초기 적용
    {
        TryApplyDifficulty(); // 초기 적 난이도 적용 시도
    }

    private void Update() // 소환 적 포함 난이도 적용 유지
    {
        TryApplyDifficulty(); // 새로 생성된 적 난이도 적용 시도
    }

    private void TryApplyDifficulty() // 현재 전투의 미적용 적 체력 보정
    {
        if (battleSceneSetup == null)
        {
            battleSceneSetup =
                FindFirstObjectByType<BattleSceneSetup>(); // 현재 전투 구성 관리자 조회
        }

        if (battleSceneSetup == null ||
            !battleSceneSetup.IsInitialized)
        {
            return;
        }

        int floor =
            ExplorationDifficultyCalculator.GetCurrentBattleFloor(); // 현재 탐사 전투 층 조회

        LogDifficultyProfile(
            floor); // 현재 전투 난이도 정보 출력

        System.Collections.Generic.IReadOnlyList<BattleUnitRuntime> enemyUnits =
            battleSceneSetup.EnemyUnits; // 현재 전투 적 목록 조회

        for (int index = 0;
             index < enemyUnits.Count;
             index++)
        {
            BattleUnitRuntime enemyUnit =
                enemyUnits[index]; // 현재 적 조회

            if (enemyUnit == null ||
                enemyUnit.EnemySource == null ||
                scaledEnemies.Contains(enemyUnit))
            {
                continue;
            }

            ApplyHealthScaling(
                enemyUnit,
                floor); // 현재 적 최대 체력 배율 적용

            scaledEnemies.Add(
                enemyUnit); // 중복 적용 방지 등록
        }
    }

    private static void ApplyHealthScaling(
        BattleUnitRuntime enemyUnit,
        int floor) // 적 최대·현재 체력 층 보정
    {
        int scaledMaximumHealth =
            ExplorationDifficultyCalculator.ScaleHealth(
                enemyUnit.EnemySource.MaxHealth,
                floor); // 원본 EnemyData 기준 최대 체력 계산

        int additionalHealth =
            scaledMaximumHealth -
            enemyUnit.MaxHealth; // 현재 런타임 대비 추가 체력 계산

        if (additionalHealth <= 0)
        {
            return;
        }

        int appliedMaximumHealth =
            enemyUnit.ModifyMaxHealth(
                additionalHealth); // 런타임 최대 체력 증가 적용

        if (appliedMaximumHealth > 0)
        {
            enemyUnit.RestoreHealth(
                appliedMaximumHealth); // 증가한 최대 체력만큼 현재 체력 보충
        }

        Debug.Log(
            $"[Exploration][Day42] 적 체력 난이도 적용 - " +
            $"{enemyUnit.DisplayName} / " +
            $"{floor}F / " +
            $"HP {enemyUnit.MaxHealth}"); // 적별 체력 적용 결과 출력
    }

    private void LogDifficultyProfile(
        int floor) // 현재 전투 난이도 배율 로그
    {
        if (profileLogged)
        {
            return;
        }

        profileLogged = true; // 중복 로그 방지 기록

        Debug.Log(
            $"[Exploration][Day42] 층 난이도 적용 - " +
            $"{floor}F / " +
            $"HP x{ExplorationDifficultyCalculator.GetHealthMultiplier(floor):0.00} / " +
            $"ATK x{ExplorationDifficultyCalculator.GetAttackMultiplier(floor):0.00} / " +
            $"Reward x{ExplorationDifficultyCalculator.GetRewardMultiplier(floor):0.00}"); // 현재 층 배율 출력
    }
}

public static class ExplorationBattleDifficultyBootstrap // 전투 Scene 난이도 적용기 자동 설치
{
    private const string BattleSceneName =
        "40_Battle"; // 전투 Scene 이름

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // Scene 로드 이벤트 초기화
    {
        SceneManager.sceneLoaded -=
            HandleSceneLoaded; // 중복 Scene 이벤트 제거

        SceneManager.sceneLoaded +=
            HandleSceneLoaded; // Scene 로드 이벤트 등록
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode) // Scene 로드 완료 처리
    {
        if (scene.name != BattleSceneName)
        {
            return;
        }

        BattleSceneSetup battleSceneSetup =
            Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 구성 관리자 조회

        if (battleSceneSetup == null)
        {
            return;
        }

        if (battleSceneSetup.GetComponent<ExplorationBattleDifficultyRuntime>() == null)
        {
            battleSceneSetup.gameObject.AddComponent<ExplorationBattleDifficultyRuntime>(); // 층 난이도 적용기 자동 추가
        }
    }
}

