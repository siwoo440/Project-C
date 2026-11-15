using System.Collections; // 탐사 복귀 초기화 대기 사용
using UnityEngine; // 전투 결과와 탐사 세션 기능 사용

[DefaultExecutionOrder(-500)]
public sealed class ExplorationBattleResultReceiver : MonoBehaviour // 전투 결과 탐사 반영
{
    public BattleResultData ReceivedResult { get; private set; } // 수신한 전투 결과 조회

    private IEnumerator Start() // 전투 결과 반영 후 자동 저장
    { // 전투 후 자동 저장 시작
        if (ReceivedResult == null) // 실제 전투 결과 수신 여부 확인
        { // 저장 불필요 처리 시작
            yield break; // 일반 탐사 진입 시 자동 저장 생략
        } // 저장 불필요 처리 종료
        yield return null; // 탐사 플레이어와 맵 초기화 대기
        ExplorationSessionManager session = ExplorationSessionManager.Instance; // 결과 반영된 탐사 세션 조회
        if (session == null) // 탐사 세션 존재 확인
        { // 세션 누락 처리 시작
            yield break; // 불완전한 상태 저장 중단
        } // 세션 누락 처리 종료
        ExplorationPlayerController player = FindFirstObjectByType<ExplorationPlayerController>(); // 복귀한 탐사 플레이어 조회
        if (!session.IsExplorationCompleted && player == null) // 진행 중 탐사의 플레이어 존재 확인
        { // 플레이어 누락 처리 시작
            Debug.LogWarning("[ExplorationAutoSave] 탐사 위치를 찾지 못해 전투 후 저장을 건너뛰었습니다."); // 저장 불가 이유 안내
            yield break; // 잘못된 위치 저장 중단
        } // 플레이어 누락 처리 종료
        Vector3 position = player != null ? player.transform.position : Vector3.zero; // 현재 탐사 위치 또는 종료 위치 결정
        if (!ExplorationAutoSave.SaveAfterBattle(position)) // 전투 결과 반영 후 저장 확인
        { // 저장 실패 처리 시작
            Debug.LogWarning("[ExplorationAutoSave] 전투 후 상태를 저장하지 못했습니다."); // 기존 저장 유지와 저장 실패 안내
        } // 저장 실패 처리 종료
    } // 전투 후 자동 저장 종료

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

        if (resultManager.IsActivePartyWiped() &&
            !sessionManager.IsExplorationCompleted)
        {
            sessionManager.CompleteExplorationFailure(
                "저장된 파티 HP 기준 출전 파티가 전멸했습니다."); // 영속 상태 기준 전멸 누락 방지
        }

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
