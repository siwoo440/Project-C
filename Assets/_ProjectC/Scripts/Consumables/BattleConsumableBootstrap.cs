using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(220)]
public sealed class BattleConsumableBootstrap : MonoBehaviour
{
    [Header("전투 연결")]
    [SerializeField] private BattleSceneSetup battleSceneSetup;
    [SerializeField] private Canvas targetCanvas;

    [Header("테스트 시작 소모품")]
    [SerializeField] private List<ConsumableItemData> debugStartingItems = new List<ConsumableItemData>();

    private BattleConsumableController consumableController;
    private ConsumableSlotBarView slotBarView;
    private ConsumableWindowToggleView windowToggleView;
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
            battleSceneSetup = FindFirstObjectByType<BattleSceneSetup>();
        }

        if (battleSceneSetup == null)
        {
            Debug.LogError("[BattleConsumableBootstrap] BattleSceneSetup을 찾을 수 없습니다.", this);
            yield break;
        }

        while (!battleSceneSetup.IsInitialized)
        {
            yield return null;
        }

        ConsumableRunManager runManager = ConsumableRunManager.EnsureInstance();
        if (runManager.Inventory.Count == 0)
        {
            AcquireDebugStartingItems(runManager);
        }

        consumableController = new BattleConsumableController(runManager.Inventory, battleSceneSetup);

        Canvas canvas = targetCanvas == null ? FindBattleCanvas() : targetCanvas;
        if (canvas == null)
        {
            Debug.LogError("[BattleConsumableBootstrap] 소모품 슬롯을 배치할 Canvas를 찾을 수 없습니다.", this);
            consumableController.Dispose();
            consumableController = null;
            yield break;
        }

        slotBarView = ConsumableSlotBarView.Create(canvas, consumableController, runManager.Inventory);
        windowToggleView = ConsumableWindowToggleView.Create(canvas, slotBarView);
        initializeCoroutine = null;
    }

    private void AcquireDebugStartingItems(ConsumableRunManager runManager)
    {
        for (int index = 0; index < debugStartingItems.Count; index++)
        {
            ConsumableItemData itemData = debugStartingItems[index];
            if (itemData == null)
            {
                continue;
            }

            bool acquired = runManager.TryAcquire(itemData, out int slotIndex);
            Debug.Log(
                $"[BattleConsumableBootstrap] 테스트 소모품 - {itemData.DisplayName} / 슬롯 {slotIndex + 1} / 성공 {acquired}",
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

        if (windowToggleView != null)
        {
            windowToggleView.Dispose();
            Destroy(windowToggleView.gameObject);
            windowToggleView = null;
        }

        if (slotBarView != null)
        {
            slotBarView.Dispose();
            Destroy(slotBarView.gameObject);
            slotBarView = null;
        }

        consumableController?.Dispose();
        consumableController = null;
    }
}
