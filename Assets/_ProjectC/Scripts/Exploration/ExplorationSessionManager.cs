using System.Collections.Generic; // 조우 클리어 목록 사용
using UnityEngine; // 영구 오브젝트와 위치 기능 사용

public sealed class ExplorationSessionManager : MonoBehaviour // 탐사 진행 상태 관리자
{
    private static ExplorationSessionManager instance; // 탐사 관리자 인스턴스

    private readonly HashSet<string> clearedEncounterIds =
        new HashSet<string>(); // 클리어 런타임 조우 ID 목록

    private EncounterData activeEncounter; // 현재 전투 조우 데이터
    private string activeRuntimeEncounterId; // 현재 전투 런타임 조우 ID
    private Vector3 returnPosition; // 전투 후 탐사 복귀 위치
    private bool hasReturnPosition; // 복귀 위치 존재 여부
    private int currentFloor = 1; // 현재 탐사 층
    private int currentFloorSeed; // 현재 층 절차 생성 Seed
    private bool hasCurrentFloorSeed; // 현재 층 Seed 존재 여부

    public static ExplorationSessionManager Instance => instance; // 현재 탐사 관리자 조회
    public EncounterData ActiveEncounter => activeEncounter; // 현재 조우 데이터 조회
    public string ActiveRuntimeEncounterId => activeRuntimeEncounterId; // 현재 런타임 조우 ID 조회
    public int CurrentFloor => currentFloor; // 현재 층 조회
    public bool HasReturnPosition => hasReturnPosition; // 전투 복귀 위치 존재 여부
    public int CurrentFloorSeed => currentFloorSeed; // 현재 층 Seed 조회
    public bool HasCurrentFloorSeed => hasCurrentFloorSeed; // 현재 층 Seed 존재 여부 조회

    public IReadOnlyCollection<string> ClearedEncounterIds =>
        clearedEncounterIds; // 클리어 조우 목록 조회

    public ExplorationClearRewardResult LastClearReward
    {
        get;
        private set;
    }

    public static ExplorationSessionManager EnsureInstance() // 탐사 관리자 존재 보장
    {
        if (instance != null)
        {
            return instance;
        }

        instance =
            FindFirstObjectByType<ExplorationSessionManager>(); // Scene 기존 관리자 탐색

        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject =
            new GameObject("ExplorationSessionManager"); // 탐사 관리자 오브젝트 생성

        instance =
            managerObject.AddComponent<ExplorationSessionManager>(); // 탐사 관리자 컴포넌트 추가

        return instance;
    }

    private void Awake() // 탐사 관리자 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 관리자 제거
            return;
        }

        instance = this; // 현재 관리자 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 상태 유지
    }

    public bool BeginEncounter(
        string runtimeEncounterId,
        EncounterData encounterData,
        Vector3 playerPosition,
        Vector3 encounterPosition) // 절차 조우 시작
    {
        if (string.IsNullOrWhiteSpace(runtimeEncounterId) ||
            encounterData == null ||
            !encounterData.IsValidData() ||
            activeEncounter != null ||
            IsEncounterCleared(runtimeEncounterId))
        {
            return false;
        }

        activeEncounter = encounterData; // 현재 전투 데이터 저장
        activeRuntimeEncounterId = runtimeEncounterId; // 런타임 조우 ID 저장
        LastClearReward = null; // 이전 보상 표시 초기화

        Vector2 escapeDirection =
            (Vector2)(playerPosition - encounterPosition); // 조우 반대 방향 계산

        if (escapeDirection.sqrMagnitude < 0.01f)
        {
            escapeDirection = Vector2.left; // 겹친 위치 기본 복귀 방향 지정
        }

        escapeDirection.Normalize(); // 복귀 방향 정규화

        returnPosition =
            playerPosition +
            (Vector3)(escapeDirection * 1.25f); // 전투 후 복귀 위치 계산

        returnPosition.z = 0f; // Z 위치 초기화
        hasReturnPosition = true; // 복귀 위치 활성화

        Debug.Log(
            $"[Exploration][Day39] 조우 시작 - " +
            $"{runtimeEncounterId} / " +
            $"{encounterData.DisplayName} / " +
            $"적 {encounterData.Enemies.Count}명"); // 절차 조우 시작 로그

        return true;
    }

    public bool BeginEncounter(
        EncounterData encounterData,
        Vector3 playerPosition,
        Vector3 encounterPosition) // 기존 호출 호환 조우 시작
    {
        string fallbackRuntimeId =
            encounterData != null
                ? encounterData.EncounterId
                : string.Empty; // 기존 Encounter ID를 임시 런타임 ID로 사용

        return BeginEncounter(
            fallbackRuntimeId,
            encounterData,
            playerPosition,
            encounterPosition); // 신규 조우 시작 기능 호출
    }

    public void ResolveBattleResult(BattleResultData resultData) // 전투 결과 처리
    {
        if (resultData == null || activeEncounter == null)
        {
            return;
        }

        string runtimeEncounterId =
            activeRuntimeEncounterId; // 현재 런타임 조우 ID 보관

        string encounterName =
            activeEncounter.DisplayName; // 현재 조우 이름 보관

        if (resultData.Result == BattleResult.Victory)
        {
            GrantVictoryRewards(activeEncounter); // 승리 보상 지급

            if (!string.IsNullOrWhiteSpace(runtimeEncounterId))
            {
                clearedEncounterIds.Add(runtimeEncounterId); // 해당 배치 조우 클리어 기록
            }

            Debug.Log(
                $"[Exploration][Day39] 조우 클리어 - " +
                $"{runtimeEncounterId} / {encounterName}"); // 조우 클리어 로그
        }
        else
        {
            LastClearReward = null; // 비승리 보상 제거

            Debug.Log(
                $"[Exploration][Day39] 조우 유지 - " +
                $"{runtimeEncounterId} / " +
                $"{encounterName} / " +
                $"결과 {resultData.Result}"); // 조우 유지 로그
        }

        activeEncounter = null; // 현재 전투 데이터 초기화
        activeRuntimeEncounterId = null; // 현재 런타임 조우 ID 초기화
    }

    public bool IsEncounterCleared(string runtimeEncounterId) // 런타임 조우 클리어 여부 확인
    {
        return !string.IsNullOrWhiteSpace(runtimeEncounterId) &&
               clearedEncounterIds.Contains(runtimeEncounterId); // 클리어 목록 포함 여부 반환
    }

    public Vector3 GetPlayerSpawnPosition(
        Vector3 defaultPosition) // 탐사 플레이어 생성 위치 조회
    {
        return hasReturnPosition
            ? returnPosition
            : defaultPosition; // 전투 복귀 위치 우선 반환
    }

    public bool TryGetCurrentFloorSeed(
        out int seed) // 현재 층 Seed 조회 시도
    {
        seed = currentFloorSeed; // 현재 Seed 반환값 지정
        return hasCurrentFloorSeed; // Seed 존재 여부 반환
    }

    public void SetCurrentFloorSeed(
        int seed) // 현재 층 Seed 저장
    {
        currentFloorSeed = seed; // 현재 층 Seed 저장
        hasCurrentFloorSeed = true; // Seed 존재 상태 활성화
    }

    public void ClearReturnPosition() // 전투 복귀 위치 초기화
    {
        returnPosition = Vector3.zero; // 복귀 위치 초기화
        hasReturnPosition = false; // 복귀 위치 사용 해제
    }

    private void ClearCurrentFloorSeed() // 현재 층 Seed 초기화
    {
        currentFloorSeed = 0; // Seed 값 초기화
        hasCurrentFloorSeed = false; // Seed 존재 상태 해제
    }

    public int AdvanceFloor() // 다음 층 진행
    {
        currentFloor += 1; // 현재 층 증가
        activeEncounter = null; // 진행 중 조우 초기화
        activeRuntimeEncounterId = null; // 진행 중 런타임 조우 초기화
        returnPosition = Vector3.zero; // 이전 층 복귀 위치 초기화
        hasReturnPosition = false; // 이전 층 복귀 위치 해제
        LastClearReward = null; // 이전 보상 표시 초기화
        ClearCurrentFloorSeed(); // 다음 층용 Seed 생성 준비

        Debug.Log(
            $"[Exploration][Day39] 다음 층 진입 - {currentFloor}F"); // 층 진행 로그

        return currentFloor; // 변경된 층 반환
    }

    public void ResetExploration() // 탐사 전체 초기화
    {
        clearedEncounterIds.Clear(); // 클리어 조우 목록 초기화
        activeEncounter = null; // 현재 조우 초기화
        activeRuntimeEncounterId = null; // 런타임 조우 초기화
        returnPosition = Vector3.zero; // 복귀 위치 초기화
        hasReturnPosition = false; // 복귀 위치 해제
        LastClearReward = null; // 마지막 보상 초기화
        currentFloor = 1; // 탐사 층 초기화
        ClearCurrentFloorSeed(); // 현재 층 Seed 초기화
    }

    private void GrantVictoryRewards(
        EncounterData encounterData) // 승리 보상 지급
    {
        CharacterProgressionManager progressionManager =
            CharacterProgressionManager.EnsureInstance(); // 캐릭터 성장 관리자 준비

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance(); // 영구 자원 관리자 준비

        int previousLevel =
            progressionManager.Level; // 보상 전 레벨 저장

        int appliedExperience =
            progressionManager.AddExperience(
                encounterData.CharacterExperienceReward); // 캐릭터 경험치 지급

        resourceManager.AddClearReward(
            encounterData.GoldReward,
            encounterData.ScrewReward,
            encounterData.IronPlateReward,
            encounterData.WireReward); // 클리어 자원 지급

        LastClearReward =
            new ExplorationClearRewardResult(
                encounterData.DisplayName,
                appliedExperience,
                encounterData.GoldReward,
                encounterData.ScrewReward,
                encounterData.IronPlateReward,
                encounterData.WireReward,
                previousLevel,
                progressionManager.Level); // 마지막 클리어 보상 생성

        Debug.Log(
            $"[Exploration] 클리어 보상 - " +
            $"EXP +{appliedExperience}, " +
            $"Gold +{encounterData.GoldReward}, " +
            $"나사 +{encounterData.ScrewReward}, " +
            $"철판 +{encounterData.IronPlateReward}, " +
            $"전선 +{encounterData.WireReward}"); // 클리어 보상 로그
    }

    private void OnDestroy() // 탐사 관리자 제거 처리
    {
        if (instance == this)
        {
            instance = null; // 정적 관리자 참조 초기화
        }
    }
}
