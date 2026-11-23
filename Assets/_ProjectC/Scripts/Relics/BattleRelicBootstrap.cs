using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
public sealed class BattleRelicBootstrap : MonoBehaviour
{
    [Header("전투 연결")]
    [SerializeField] private BattleSceneSetup battleSceneSetup;
    [SerializeField] private Canvas debugCanvas;

    [Header("유물 디버그")]
    [SerializeField] private List<RelicData> debugStartingRelics = new List<RelicData>();

    private BattleRelicEffectController relicEffectController;
    private RelicDebugWindow relicDebugWindow;
    private Coroutine initializeCoroutine;

    private void Start()
    {
        initializeCoroutine = StartCoroutine(InitializeWhenBattleReady());
    }

    private IEnumerator InitializeWhenBattleReady()
    {
        if (battleSceneSetup == null)
        {
            battleSceneSetup = GetComponent<BattleSceneSetup>();
        }

        if (battleSceneSetup == null)
        {
            Debug.LogError("[BattleRelicBootstrap] BattleSceneSetup을 찾을 수 없습니다.", this);
            yield break;
        }

        while (!battleSceneSetup.IsInitialized)
        {
            yield return null;
        }

        RelicRunManager runManager = RelicRunManager.EnsureInstance();
        relicEffectController = new BattleRelicEffectController(
            battleSceneSetup.BattleEvents,
            runManager.Inventory,
            runManager.Gold,
            battleSceneSetup.AllyUnits,
            battleSceneSetup.EnemyUnits);

        if (runManager.Inventory.Count == 0)
        {
            AcquireDebugStartingRelics(runManager);
        }

        relicEffectController.ProcessInitialBattleState(
            battleSceneSetup.BattleTurn.CurrentPhase,
            battleSceneSetup.BattleTurn.CurrentRound);

        Canvas targetCanvas = debugCanvas == null ? FindBattleCanvas() : debugCanvas;
        if (targetCanvas != null)
        {
            relicDebugWindow = RelicDebugWindow.Create(targetCanvas, runManager, false);
        }
        else
        {
            Debug.LogError("[BattleRelicBootstrap] 유물 디버그 창을 배치할 Canvas를 찾을 수 없습니다.", this);
        }

        initializeCoroutine = null;
    }

    private void AcquireDebugStartingRelics(RelicRunManager runManager)
    {
        foreach (RelicData relicData in debugStartingRelics)
        {
            if (relicData == null)
            {
                continue;
            }

            RelicAcquireResult acquireResult = runManager.TryAcquire(relicData);
            Debug.Log(
                $"[BattleRelicBootstrap] 테스트 유물 처리 - {relicData.DisplayName} / {acquireResult}",
                this);
        }
    }

    private static Canvas FindBattleCanvas()
    {
        BattleHandView handView = FindFirstObjectByType<BattleHandView>();
        if (handView == null)
        {
            return null;
        }

        return handView.GetComponentInParent<Canvas>();
    }

    private void OnDestroy()
    {
        if (initializeCoroutine != null)
        {
            StopCoroutine(initializeCoroutine);
            initializeCoroutine = null;
        }

        relicEffectController?.Dispose();
        relicEffectController = null;

        if (relicDebugWindow != null)
        {
            relicDebugWindow.Dispose();
            Destroy(relicDebugWindow.gameObject);
            relicDebugWindow = null;
        }
    }
}
