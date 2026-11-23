using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ExplorationSceneRuntimeRouter : MonoBehaviour
{
    private const string ExplorationSceneName = "30_Exploration";
    private const string BattleSceneName = "40_Battle";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<ExplorationSceneRuntimeRouter>() != null)
        {
            return;
        }

        GameObject routerObject = new GameObject(
            "ExplorationSceneRuntimeRouter");

        DontDestroyOnLoad(routerObject);
        routerObject.AddComponent<ExplorationSceneRuntimeRouter>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode)
    {
        if (scene.name == ExplorationSceneName)
        {
            PrepareExplorationScene();
            return;
        }

        if (scene.name == BattleSceneName)
        {
            ApplyActiveEncounterToBattle();
        }
    }

    private static void PrepareExplorationScene()
    {
        ExplorationSessionManager.EnsureInstance();
        EnsureSceneFlowManagerForDirectSceneTest();

        GameObject runtimeRoot =
            GameObject.Find("ExplorationRuntime");

        if (runtimeRoot == null)
        {
            runtimeRoot = new GameObject("ExplorationRuntime");
        }

        if (runtimeRoot.GetComponent<ExplorationBattleResultReceiver>() == null)
        {
            runtimeRoot.AddComponent<ExplorationBattleResultReceiver>();
        }

        if (runtimeRoot.GetComponent<ExplorationPrototypeBootstrap>() == null)
        {
            runtimeRoot.AddComponent<ExplorationPrototypeBootstrap>();
        }
    }

    private static void ApplyActiveEncounterToBattle()
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.Instance;

        if (sessionManager == null ||
            sessionManager.ActiveEncounter == null)
        {
            return;
        }

        BattleSceneSetup battleSceneSetup =
            FindFirstObjectByType<BattleSceneSetup>();

        if (battleSceneSetup == null)
        {
            Debug.LogError(
                "[ExplorationSceneRuntimeRouter] BattleSceneSetup을 찾을 수 없습니다.");
            return;
        }

        EncounterData encounterData =
            sessionManager.ActiveEncounter;

        FieldInfo enemiesField =
            typeof(BattleSceneSetup).GetField(
                "enemies",
                BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo battleTypeField =
            typeof(BattleSceneSetup).GetField(
                "battleType",
                BindingFlags.Instance | BindingFlags.NonPublic);

        if (enemiesField == null || battleTypeField == null)
        {
            Debug.LogError(
                "[ExplorationSceneRuntimeRouter] BattleSceneSetup의 조우 주입 필드를 찾지 못했습니다.");
            return;
        }

        List<EnemyData> encounterEnemies =
            new List<EnemyData>();

        for (int index = 0; index < encounterData.Enemies.Count; index++)
        {
            EnemyData enemyData = encounterData.Enemies[index];

            if (enemyData != null)
            {
                encounterEnemies.Add(enemyData);
            }
        }

        enemiesField.SetValue(
            battleSceneSetup,
            encounterEnemies);

        battleTypeField.SetValue(
            battleSceneSetup,
            encounterData.BattleType);

        Debug.Log(
            $"[Exploration] 전투 조우 적용 - {encounterData.DisplayName} / " +
            $"적 {encounterEnemies.Count}명");
    }

    private static void EnsureSceneFlowManagerForDirectSceneTest()
    {
        if (SceneFlowManager.Instance != null)
        {
            return;
        }

        GameObject managerObject =
            new GameObject("SceneFlowManager_Runtime");

        managerObject.AddComponent<SceneFlowManager>();
        DontDestroyOnLoad(managerObject);

        Debug.Log(
            "[Exploration] 직접 Scene 테스트용 SceneFlowManager를 생성했습니다.");
    }
}
