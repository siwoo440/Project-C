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

        if (!string.IsNullOrEmpty(ExplorationSessionManager.Instance.Cart.AmbushRoom)) // 불러온 저장에 미완료 수레 매복 확인
        { // 저장 매복 재개 시작
            runtimeRoot.GetComponent<ExplorationBattleResultReceiver>().StartCoroutine(BurntCartEvent.ResumeAmbush()); // 탐사 준비 후 같은 조우 재개
        } // 저장 매복 재개 종료
        if (runtimeRoot.GetComponent<ExplorationPrototypeBootstrap>() == null)
        {
            runtimeRoot.AddComponent<ExplorationPrototypeBootstrap>();
        }

        if (runtimeRoot.GetComponent<ShopPrototypeBootstrap>() == null) // 54일차 상점 연결기 존재 확인
        {
            runtimeRoot.AddComponent<ShopPrototypeBootstrap>(); // 탐사 상점 코드 기반 UI 추가
        }

        if (runtimeRoot.GetComponent<ExplorationRoomRoleBootstrap>() == null) // 56일차 방 역할 연결기 존재 확인
        {
            runtimeRoot.AddComponent<ExplorationRoomRoleBootstrap>(); // 절차 방 역할·특수 방 통합 연결
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
            EnemyData enemyData =
                encounterData.Enemies[index];

            if (enemyData != null)
            {
                encounterEnemies.Add(enemyData);
            }
        }

        if (sessionManager.Cart.AmbushBattleId != null && sessionManager.Cart.AmbushBattleId == sessionManager.ActiveRuntimeEncounterId) // 수레 매복 편성 확인
        { // 사냥꾼 추가 시작
            battleSceneSetup.HasCartAmbushSlot = true; // 정상 초기 편성에 매복 한 자리만 추가 허용
            encounterEnemies.Add(BurntCartEvent.Hunter); // 일반 조우 복사본에 사냥꾼 한 마리 추가
        } // 사냥꾼 추가 종료
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
