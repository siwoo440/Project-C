using UnityEngine; // 전투 결과와 탐사 세션 기능 사용

[DefaultExecutionOrder(-500)]
public sealed class ExplorationBattleResultReceiver : MonoBehaviour // 전투 결과 탐사 반영
{
    public BattleResultData ReceivedResult { get; private set; } // 수신한 전투 결과 조회

    private void Awake() // 탐사 맵 생성 전에 전투 결과 우선 반영
    {
        BattleResultManager resultManager =
            BattleResultManager.EnsureInstance(); // 전투 결과 관리자 준비

        if (!resultManager.TryConsumeResult(
                out BattleResultData battleResultData))
        {
            Debug.Log(
                "[ExplorationBattleResultReceiver] 전달된 전투 결과가 없습니다.",
                this); // 전투 결과 없음 로그

            return;
        }

        ReceivedResult =
            battleResultData; // 수신 결과 저장

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        sessionManager.ResolveBattleResult(
            ReceivedResult); // 맵 생성 전에 클리어 상태 반영

        string rewardLabel =
            ReceivedResult.CanReceiveReward
                ? "보상 가능"
                : "보상 없음"; // 보상 가능 여부 문구 결정

        Debug.Log(
            $"[ExplorationBattleResultReceiver] 전투 결과 수신 - " +
            $"{ReceivedResult.Result} / " +
            $"라운드 {ReceivedResult.CompletedRound} / " +
            $"생존 아군 {ReceivedResult.LivingAllyCount}명 / " +
            $"{rewardLabel}",
            this); // 전투 결과 로그

        foreach (BattleUnitResultData allyState in ReceivedResult.AllyStates)
        {
            Debug.Log(
                $"[ExplorationBattleResultReceiver] 아군 상태 유지 - " +
                $"{allyState.DisplayName} / " +
                $"HP {allyState.CurrentHealth} / {allyState.MaximumHealth}",
                this); // 아군 상태 유지 로그
        }
    }
}
