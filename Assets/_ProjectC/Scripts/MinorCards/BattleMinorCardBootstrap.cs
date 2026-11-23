using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(240)]
public sealed class BattleMinorCardBootstrap : MonoBehaviour
{
    [Header("전투 연결")]
    [SerializeField] private BattleSceneSetup battleSceneSetup;
    [SerializeField] private Canvas targetCanvas;

    [Header("플레이어 레벨")]
    [SerializeField] private PlayerLevelConfig levelConfig;

    [Header("마이너 카드 풀")]
    [SerializeField] private List<MinorCardData> minorCardPool = new List<MinorCardData>();

    private BattleMinorCardController minorCardController;
    private MinorCardSelectionView selectionView;
    private PlayerLevelHudView levelHudView;
    private MinorCardBuffWindowView buffWindowView;
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
            Debug.LogError("[BattleMinorCardBootstrap] BattleSceneSetup을 찾을 수 없습니다.", this);
            yield break;
        }

        if (levelConfig == null)
        {
            levelConfig = PlayerLevelConfig.GetRuntimeDefault();
            Debug.LogWarning(
                "[BattleMinorCardBootstrap] PlayerLevelConfig가 연결되지 않아 기본 설정을 사용합니다. " +
                "시작 Lv.1 / 첫 필요 EXP 5 / 레벨당 필요 EXP +3 / 카드 사용 EXP +1 / 선택지 3장",
                this);
        }

        if (minorCardPool == null)
        {
            minorCardPool = new List<MinorCardData>();
        }

        if (minorCardPool.Count == 0)
        {
            Debug.LogWarning(
                "[BattleMinorCardBootstrap] Minor Card Pool이 비어 있습니다. " +
                "레벨업은 가능하지만 마이너 카드 선택지를 만들 수 없습니다.",
                this);
        }

        while (!battleSceneSetup.IsInitialized)
        {
            yield return null;
        }

        Canvas canvas = targetCanvas == null ? FindBattleCanvas() : targetCanvas;
        if (canvas == null)
        {
            Debug.LogError("[BattleMinorCardBootstrap] 마이너 카드 UI를 배치할 Canvas를 찾을 수 없습니다.", this);
            yield break;
        }

        BattleMinorCardEffectRegistry.BeginBattle();

        PlayerLevelRunManager levelManager = PlayerLevelRunManager.EnsureInstance(levelConfig);
        levelManager.BeginBattle();

        minorCardController = new BattleMinorCardController(
            battleSceneSetup,
            levelManager,
            levelConfig,
            minorCardPool);

        selectionView = MinorCardSelectionView.Create(canvas, minorCardController, levelManager);
        levelHudView = PlayerLevelHudView.Create(canvas, levelManager);
        buffWindowView = MinorCardBuffWindowView.Create(canvas, minorCardController);

        minorCardController.ProcessCurrentTurn();
        initializeCoroutine = null;

        Debug.Log(
            $"[BattleMinorCardBootstrap] 전투 단위 마이너 카드 시스템 준비 완료 / " +
            $"Lv.{levelManager.Level} / 카드 풀 {minorCardPool.Count}장",
            this);
    }

    private static Canvas FindBattleCanvas()
    {
        BattleHandView handView = FindFirstObjectByType<BattleHandView>();
        return handView == null ? null : handView.GetComponentInParent<Canvas>();
    }

    private void OnDestroy()
    {
        if (initializeCoroutine != null)
        {
            StopCoroutine(initializeCoroutine);
            initializeCoroutine = null;
        }

        if (buffWindowView != null)
        {
            buffWindowView.Dispose();
            Destroy(buffWindowView.gameObject);
            buffWindowView = null;
        }

        if (selectionView != null)
        {
            selectionView.Dispose();
            Destroy(selectionView.gameObject);
            selectionView = null;
        }

        if (levelHudView != null)
        {
            levelHudView.Dispose();
            Destroy(levelHudView.gameObject);
            levelHudView = null;
        }

        minorCardController?.Dispose();
        minorCardController = null;
    }
}
