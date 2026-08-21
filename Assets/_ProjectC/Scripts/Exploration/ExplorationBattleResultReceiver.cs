using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.SceneManagement; // Scene 정보 기능 사용
public sealed class ExplorationBattleResultReceiver : MonoBehaviour // 탐사 전투 결과 수신기
{ // 클래스 시작
    private const string ExplorationSceneName = "30_Exploration"; // 탐사 Scene 이름
    public BattleResultData ReceivedResult { get; private set; } // 마지막 수신 전투 결과 조회
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Scene 로드 후 자동 실행
    private static void EnsureExplorationReceiver() // 탐사 수신기 자동 준비
    { // 수신기 준비 시작
        if (SceneManager.GetActiveScene().name != ExplorationSceneName) // 현재 탐사 Scene 확인
        { // 다른 Scene 처리 시작
            return; // 수신기 준비 중단
        } // 다른 Scene 처리 종료
        if (Object.FindFirstObjectByType<ExplorationBattleResultReceiver>() != null) // 기존 수신기 확인
        { // 기존 수신기 처리 시작
            return; // 중복 생성 중단
        } // 기존 수신기 처리 종료
        new GameObject("ExplorationBattleResultReceiver", typeof(ExplorationBattleResultReceiver)); // 탐사 결과 수신 오브젝트 생성
    } // 수신기 준비 종료
    private void Start() // 탐사 결과 수신 시작
    { // 결과 수신 시작
        BattleResultManager resultManager = BattleResultManager.EnsureInstance(); // 영구 결과 관리자 준비
        if (!resultManager.TryConsumeResult(out BattleResultData battleResultData)) // 대기 결과 소비 확인
        { // 대기 결과 없음 처리 시작
            Debug.Log("[ExplorationBattleResultReceiver] 전달된 전투 결과가 없습니다.", this); // 결과 없음 로그 출력
            return; // 결과 수신 종료
        } // 대기 결과 없음 처리 종료
        ReceivedResult = battleResultData; // 수신 전투 결과 저장
        string rewardLabel = ReceivedResult.CanReceiveReward ? "보상 가능" : "보상 없음"; // 보상 상태 문구 계산
        Debug.Log($"[ExplorationBattleResultReceiver] 전투 결과 수신 - {ReceivedResult.Result} / 라운드 {ReceivedResult.CompletedRound} / 생존 아군 {ReceivedResult.LivingAllyCount}명 / {rewardLabel}", this); // 수신 결과 로그 출력
        foreach (BattleUnitResultData allyState in ReceivedResult.AllyStates) // 아군 상태 목록 순회
        { // 아군 상태 출력 시작
            Debug.Log($"[ExplorationBattleResultReceiver] 아군 상태 유지 - {allyState.DisplayName} / HP {allyState.CurrentHealth} / {allyState.MaximumHealth}", this); // 저장 아군 체력 출력
        } // 아군 상태 출력 종료
    } // 결과 수신 종료
} // 클래스 종료
