using System.Collections.Generic;
using UnityEngine;

public sealed class ExplorationSessionManager : MonoBehaviour
{
    private static ExplorationSessionManager instance;

    private readonly HashSet<string> clearedEncounterIds =
        new HashSet<string>();

    private EncounterData activeEncounter;
    private Vector3 returnPosition;
    private bool hasReturnPosition;
    private int currentFloor = 1; // 현재 탐사 층

    public static ExplorationSessionManager Instance => instance;
    public EncounterData ActiveEncounter => activeEncounter;
    public int CurrentFloor => currentFloor; // 현재 층 조회
    public bool HasReturnPosition => hasReturnPosition; // 전투 복귀 위치 존재 여부

    public IReadOnlyCollection<string> ClearedEncounterIds =>
        clearedEncounterIds;

    public ExplorationClearRewardResult LastClearReward
    {
        get;
        private set;
    }

    public static ExplorationSessionManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance =
            FindFirstObjectByType<ExplorationSessionManager>();

        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject =
            new GameObject("ExplorationSessionManager");

        instance =
            managerObject.AddComponent<ExplorationSessionManager>();

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool BeginEncounter(
        EncounterData encounterData,
        Vector3 playerPosition,
        Vector3 encounterPosition)
    {
        if (encounterData == null ||
            !encounterData.IsValidData() ||
            activeEncounter != null)
        {
            return false;
        }

        activeEncounter = encounterData;
        LastClearReward = null;

        Vector2 escapeDirection =
            (Vector2)(playerPosition - encounterPosition);

        if (escapeDirection.sqrMagnitude < 0.01f)
        {
            escapeDirection = Vector2.left;
        }

        escapeDirection.Normalize();

        returnPosition =
            playerPosition +
            (Vector3)(escapeDirection * 1.25f);

        returnPosition.z = 0f;
        hasReturnPosition = true;

        Debug.Log(
            $"[Exploration] 조우 시작 - " +
            $"{encounterData.DisplayName} / " +
            $"적 {encounterData.Enemies.Count}명");

        return true;
    }

    public void ResolveBattleResult(BattleResultData resultData)
    {
        if (resultData == null || activeEncounter == null)
        {
            return;
        }

        string encounterId =
            activeEncounter.EncounterId;

        string encounterName =
            activeEncounter.DisplayName;

        if (resultData.Result == BattleResult.Victory)
        {
            GrantVictoryRewards(activeEncounter);
            clearedEncounterIds.Add(encounterId);

            Debug.Log(
                $"[Exploration] 조우 클리어 - {encounterName}");
        }
        else
        {
            LastClearReward = null;

            Debug.Log(
                $"[Exploration] 조우 유지 - " +
                $"{encounterName} / " +
                $"결과 {resultData.Result}");
        }

        activeEncounter = null;
    }

    public bool IsEncounterCleared(string encounterId)
    {
        return !string.IsNullOrWhiteSpace(encounterId) &&
               clearedEncounterIds.Contains(encounterId);
    }

    public Vector3 GetPlayerSpawnPosition(
        Vector3 defaultPosition)
    {
        return hasReturnPosition
            ? returnPosition
            : defaultPosition;
    }

    public int AdvanceFloor() // 다음 층 진행
    {
        currentFloor += 1; // 현재 층 증가
        activeEncounter = null; // 진행 중 조우 초기화
        returnPosition = Vector3.zero; // 이전 층 복귀 위치 초기화
        hasReturnPosition = false; // 이전 층 복귀 위치 해제
        LastClearReward = null; // 이전 보상 표시 초기화

        Debug.Log(
            $"[Exploration][Day37] 다음 층 진입 - {currentFloor}F"); // 층 진행 로그

        return currentFloor; // 변경된 층 반환
    }

    public void ResetExploration()
    {
        clearedEncounterIds.Clear();
        activeEncounter = null;
        returnPosition = Vector3.zero;
        hasReturnPosition = false;
        LastClearReward = null;
        currentFloor = 1; // 탐사 층 초기화
    }

    private void GrantVictoryRewards(
        EncounterData encounterData)
    {
        CharacterProgressionManager progressionManager =
            CharacterProgressionManager.EnsureInstance();

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance();

        int previousLevel =
            progressionManager.Level;

        int appliedExperience =
            progressionManager.AddExperience(
                encounterData.CharacterExperienceReward);

        resourceManager.AddClearReward(
            encounterData.GoldReward,
            encounterData.ScrewReward,
            encounterData.IronPlateReward,
            encounterData.WireReward);

        LastClearReward =
            new ExplorationClearRewardResult(
                encounterData.DisplayName,
                appliedExperience,
                encounterData.GoldReward,
                encounterData.ScrewReward,
                encounterData.IronPlateReward,
                encounterData.WireReward,
                previousLevel,
                progressionManager.Level);

        Debug.Log(
            $"[Exploration] 클리어 보상 - " +
            $"EXP +{appliedExperience}, " +
            $"Gold +{encounterData.GoldReward}, " +
            $"나사 +{encounterData.ScrewReward}, " +
            $"철판 +{encounterData.IronPlateReward}, " +
            $"전선 +{encounterData.WireReward}");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
