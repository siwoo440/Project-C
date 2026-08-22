using System;
using UnityEngine;

public sealed class PlayerResourceManager : MonoBehaviour
{
    private static PlayerResourceManager instance;

    private RelicGoldRuntime goldRuntime;
    private bool suppressGoldEvent;

    public static PlayerResourceManager Instance => instance;

    public int Gold
    {
        get
        {
            EnsureGoldRuntime();
            return goldRuntime.Gold;
        }
    }

    public int Screw { get; private set; }
    public int IronPlate { get; private set; }
    public int Wire { get; private set; }

    public event Action ResourcesChanged;

    public static PlayerResourceManager EnsureInstance()
    {
        if (instance != null)
        {
            instance.EnsureGoldRuntime();
            return instance;
        }

        PlayerResourceManager existingManager =
            FindFirstObjectByType<PlayerResourceManager>();

        if (existingManager != null)
        {
            instance = existingManager;
            instance.EnsureGoldRuntime();
            return instance;
        }

        GameObject managerObject =
            new GameObject("PlayerResourceManager");

        instance =
            managerObject.AddComponent<PlayerResourceManager>();

        instance.EnsureGoldRuntime();
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
        EnsureGoldRuntime();
    }

    public void AddClearReward(
        int gold,
        int screw,
        int ironPlate,
        int wire)
    {
        EnsureGoldRuntime();

        int safeGold = Mathf.Max(0, gold);
        int safeScrew = Mathf.Max(0, screw);
        int safeIronPlate = Mathf.Max(0, ironPlate);
        int safeWire = Mathf.Max(0, wire);

        suppressGoldEvent = true;
        goldRuntime.AddGold(safeGold);
        suppressGoldEvent = false;

        Screw += safeScrew;
        IronPlate += safeIronPlate;
        Wire += safeWire;

        ResourcesChanged?.Invoke();
    }

    public bool CanAfford(
        int gold,
        int screw,
        int ironPlate,
        int wire)
    {
        EnsureGoldRuntime();

        int safeGold = Mathf.Max(0, gold);
        int safeScrew = Mathf.Max(0, screw);
        int safeIronPlate = Mathf.Max(0, ironPlate);
        int safeWire = Mathf.Max(0, wire);

        return goldRuntime.CanAfford(safeGold) &&
               Screw >= safeScrew &&
               IronPlate >= safeIronPlate &&
               Wire >= safeWire;
    }

    public bool TrySpend(
        int gold,
        int screw,
        int ironPlate,
        int wire)
    {
        if (!CanAfford(gold, screw, ironPlate, wire))
        {
            return false;
        }

        int safeGold = Mathf.Max(0, gold);
        int safeScrew = Mathf.Max(0, screw);
        int safeIronPlate = Mathf.Max(0, ironPlate);
        int safeWire = Mathf.Max(0, wire);

        suppressGoldEvent = true;

        if (!goldRuntime.TrySpend(safeGold))
        {
            suppressGoldEvent = false;
            return false;
        }

        suppressGoldEvent = false;

        Screw -= safeScrew;
        IronPlate -= safeIronPlate;
        Wire -= safeWire;

        ResourcesChanged?.Invoke();
        return true;
    }

    public void ResetResources()
    {
        EnsureGoldRuntime();

        suppressGoldEvent = true;
        goldRuntime.ResetGold();
        suppressGoldEvent = false;

        Screw = 0;
        IronPlate = 0;
        Wire = 0;

        ResourcesChanged?.Invoke();
    }

    private void EnsureGoldRuntime()
    {
        RelicGoldRuntime currentGoldRuntime =
            RelicRunManager.EnsureInstance().Gold;

        if (goldRuntime == currentGoldRuntime)
        {
            return;
        }

        if (goldRuntime != null)
        {
            goldRuntime.GoldChanged -= HandleGoldChanged;
        }

        goldRuntime = currentGoldRuntime;

        if (goldRuntime != null)
        {
            goldRuntime.GoldChanged += HandleGoldChanged;
        }
    }

    private void HandleGoldChanged(int currentGold)
    {
        if (!suppressGoldEvent)
        {
            ResourcesChanged?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (goldRuntime != null)
        {
            goldRuntime.GoldChanged -= HandleGoldChanged;
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}
