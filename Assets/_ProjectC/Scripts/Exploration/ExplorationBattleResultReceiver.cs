using UnityEngine;

[DefaultExecutionOrder(-500)]
public sealed class ExplorationBattleResultReceiver : MonoBehaviour
{
    public BattleResultData ReceivedResult { get; private set; }

    private void Start()
    {
        BattleResultManager resultManager =
            BattleResultManager.EnsureInstance();

        if (!resultManager.TryConsumeResult(
                out BattleResultData battleResultData))
        {
            Debug.Log(
                "[ExplorationBattleResultReceiver] 전달된 전투 결과가 없습니다.",
                this);
            return;
        }

        ReceivedResult = battleResultData;

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance();

        sessionManager.ResolveBattleResult(ReceivedResult);

        string rewardLabel =
            ReceivedResult.CanReceiveReward
                ? "보상 가능"
                : "보상 없음";

        Debug.Log(
            $"[ExplorationBattleResultReceiver] 전투 결과 수신 - " +
            $"{ReceivedResult.Result} / " +
            $"라운드 {ReceivedResult.CompletedRound} / " +
            $"생존 아군 {ReceivedResult.LivingAllyCount}명 / " +
            $"{rewardLabel}",
            this);

        foreach (BattleUnitResultData allyState in ReceivedResult.AllyStates)
        {
            Debug.Log(
                $"[ExplorationBattleResultReceiver] 아군 상태 유지 - " +
                $"{allyState.DisplayName} / " +
                $"HP {allyState.CurrentHealth} / {allyState.MaximumHealth}",
                this);
        }
    }
}
