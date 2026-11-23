using System.Collections.Generic;

public static class BattleMinorCardEffectRegistry
{
    private static readonly BattleStatusEffectType[] MinorCardStatusTypes =
    {
        BattleStatusEffectType.AttackPowerUp,
        BattleStatusEffectType.PhysicalDefenseUp,
        BattleStatusEffectType.PhysicalDefenseDown,
        BattleStatusEffectType.MagicalResistanceUp,
        BattleStatusEffectType.MagicalResistanceDown
    };

    private static readonly HashSet<BattleUnitRuntime> affectedUnits = new HashSet<BattleUnitRuntime>();
    private static readonly Dictionary<BattleUnitRuntime, int> maxHealthChanges = new Dictionary<BattleUnitRuntime, int>();

    public static void BeginBattle()
    {
        affectedUnits.Clear();
        maxHealthChanges.Clear();
    }

    public static bool Apply(MinorCardData cardData, BattleUnitRuntime targetUnit)
    {
        if (cardData == null || targetUnit == null || targetUnit.IsDead)
        {
            return false;
        }

        switch (cardData.EffectType)
        {
            case MinorCardEffectType.IncreaseMaxHealth:
                return ApplyMaxHealth(targetUnit, cardData.EffectValue);
            case MinorCardEffectType.AttackPowerUp:
                return ApplyStatus(targetUnit, BattleStatusEffectType.AttackPowerUp, cardData.EffectValue);
            case MinorCardEffectType.PhysicalDefenseUp:
                return ApplyStatus(targetUnit, BattleStatusEffectType.PhysicalDefenseUp, cardData.EffectValue);
            case MinorCardEffectType.PhysicalDefenseDown:
                return ApplyStatus(targetUnit, BattleStatusEffectType.PhysicalDefenseDown, cardData.EffectValue);
            case MinorCardEffectType.MagicalResistanceUp:
                return ApplyStatus(targetUnit, BattleStatusEffectType.MagicalResistanceUp, cardData.EffectValue);
            case MinorCardEffectType.MagicalResistanceDown:
                return ApplyStatus(targetUnit, BattleStatusEffectType.MagicalResistanceDown, cardData.EffectValue);
            default:
                return false;
        }
    }

    public static void ClearBattleEffects()
    {
        foreach (BattleUnitRuntime unit in affectedUnits)
        {
            if (unit == null)
            {
                continue;
            }

            for (int index = 0; index < MinorCardStatusTypes.Length; index++)
            {
                unit.RemoveStatusEffect(MinorCardStatusTypes[index]);
            }
        }

        foreach (KeyValuePair<BattleUnitRuntime, int> pair in maxHealthChanges)
        {
            if (pair.Key == null || pair.Value == 0)
            {
                continue;
            }

            pair.Key.ModifyMaxHealth(-pair.Value);
        }

        affectedUnits.Clear();
        maxHealthChanges.Clear();
    }

    public static void AbandonBattleTracking()
    {
        affectedUnits.Clear();
        maxHealthChanges.Clear();
    }

    private static bool ApplyMaxHealth(BattleUnitRuntime targetUnit, int value)
    {
        int appliedDelta = targetUnit.ModifyMaxHealth(value);
        if (appliedDelta == 0)
        {
            return false;
        }

        affectedUnits.Add(targetUnit);

        if (!maxHealthChanges.TryGetValue(targetUnit, out int currentDelta))
        {
            currentDelta = 0;
        }

        maxHealthChanges[targetUnit] = currentDelta + appliedDelta;
        return true;
    }

    private static bool ApplyStatus(BattleUnitRuntime targetUnit, BattleStatusEffectType effectType, int value)
    {
        const int battleLongDuration = 999999;
        const int maximumStacks = 99;

        BattleStatusEffectApplyResult result = targetUnit.ApplyStatusEffect(
            effectType,
            value,
            battleLongDuration,
            maximumStacks);

        bool applied = result == BattleStatusEffectApplyResult.Applied ||
                       result == BattleStatusEffectApplyResult.Stacked;

        if (applied)
        {
            affectedUnits.Add(targetUnit);
        }

        return applied;
    }
}
