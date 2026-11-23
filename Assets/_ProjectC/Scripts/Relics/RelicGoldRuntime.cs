using System;
using UnityEngine;

public sealed class RelicGoldRuntime
{
    public int Gold { get; private set; }

    public event Action<int> GoldChanged;

    public RelicGoldRuntime(int initialGold = 0)
    {
        Gold = Mathf.Max(0, initialGold);
    }

    public int AddGold(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (safeAmount == 0)
        {
            return 0;
        }

        Gold += safeAmount;
        GoldChanged?.Invoke(Gold);

        return safeAmount;
    }

    public bool CanAfford(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        return Gold >= safeAmount;
    }

    public bool TrySpend(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (!CanAfford(safeAmount))
        {
            return false;
        }

        if (safeAmount == 0)
        {
            return true;
        }

        Gold -= safeAmount;
        GoldChanged?.Invoke(Gold);

        return true;
    }

    public void ResetGold()
    {
        if (Gold == 0)
        {
            return;
        }

        Gold = 0;
        GoldChanged?.Invoke(Gold);
    }
}
