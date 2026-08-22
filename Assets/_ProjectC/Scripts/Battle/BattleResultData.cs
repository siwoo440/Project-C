using System.Collections.Generic;

public sealed class BattleResultData
{
    public BattleResult Result { get; }
    public BattleType BattleType { get; }
    public int CompletedRound { get; }
    public bool CanReceiveReward => Result == BattleResult.Victory;
    public IReadOnlyList<BattleUnitResultData> AllyStates { get; }
    public IReadOnlyList<string> DefeatedEnemyIds { get; }
    public int LivingAllyCount { get; }

    public BattleResultData(
        BattleResult result,
        BattleType battleType,
        int completedRound,
        IReadOnlyList<BattleUnitRuntime> allies,
        IReadOnlyList<string> defeatedEnemies)
    {
        BattleMinorCardEffectRegistry.ClearBattleEffects();

        Result = result;
        BattleType = battleType;
        CompletedRound = completedRound;

        List<BattleUnitResultData> allyStates = new List<BattleUnitResultData>();
        int livingAllyCount = 0;

        foreach (BattleUnitRuntime allyUnit in allies)
        {
            if (allyUnit == null)
            {
                continue;
            }

            BattleUnitResultData allyState = new BattleUnitResultData(allyUnit);
            allyStates.Add(allyState);

            if (!allyState.IsDead)
            {
                livingAllyCount++;
            }
        }

        AllyStates = allyStates;
        LivingAllyCount = livingAllyCount;

        List<string> defeatedEnemyIds = new List<string>();
        foreach (string enemyId in defeatedEnemies)
        {
            if (!string.IsNullOrWhiteSpace(enemyId) && !defeatedEnemyIds.Contains(enemyId))
            {
                defeatedEnemyIds.Add(enemyId);
            }
        }

        DefeatedEnemyIds = defeatedEnemyIds;
    }
}
