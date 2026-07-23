using TMPro; // TextMesh Pro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class BattleTurnUI : MonoBehaviour // 전투 턴 UI 관리자
{
    [Header("전투 턴 연결")] // 전투 턴 연결 구분
    [SerializeField] private BattleTurnRuntime turnRuntime; // 전투 턴 관리자

    [Header("턴 UI 연결")] // 턴 UI 연결 구분
    [SerializeField] private TMP_Text roundText; // 라운드 문구
    [SerializeField] private TMP_Text phaseText; // 현재 단계 문구
    [SerializeField] private TMP_Text enemyIntentText; // 적 행동 예고 문구
    [SerializeField] private Button endTurnButton; // 턴 종료 버튼

    private void Awake() // 턴 UI 초기화
    {
        if (turnRuntime == null) // 턴 관리자 연결 확인
        {
            turnRuntime = FindFirstObjectByType<BattleTurnRuntime>(); // 씬 턴 관리자 검색
        }

        if (endTurnButton != null) // 턴 종료 버튼 연결 확인
        {
            endTurnButton.onClick.AddListener(HandleEndTurnClicked); // 턴 종료 버튼 이벤트 연결
        }
    }

    private void OnEnable() // 턴 상태 이벤트 연결
    {
        if (turnRuntime != null) // 턴 관리자 연결 확인
        {
            turnRuntime.TurnStateChanged += HandleTurnStateChanged; // 턴 상태 변경 이벤트 등록
        }
    }

    private void Start() // 첫 턴 UI 표시
    {
        RefreshDisplay(); // 현재 턴 상태 표시
    }

    private void OnDisable() // 턴 상태 이벤트 해제
    {
        if (turnRuntime != null) // 턴 관리자 연결 확인
        {
            turnRuntime.TurnStateChanged -= HandleTurnStateChanged; // 턴 상태 변경 이벤트 제거
        }
    }

    private void OnDestroy() // 턴 UI 제거
    {
        if (endTurnButton != null) // 턴 종료 버튼 연결 확인
        {
            endTurnButton.onClick.RemoveListener(HandleEndTurnClicked); // 턴 종료 버튼 이벤트 제거
        }
    }

    private void HandleEndTurnClicked() // 턴 종료 버튼 처리
    {
        if (turnRuntime == null) // 턴 관리자 연결 확인
        {
            Debug.LogError("Battle turn runtime is missing.", this); // 턴 관리자 누락 오류
            return; // 턴 종료 처리 중단
        }

        turnRuntime.TryEndPlayerTurn(); // 플레이어 턴 종료 요청
        RefreshDisplay(); // 턴 UI 즉시 갱신
    }

    private void HandleTurnStateChanged(BattleTurnPhase phase, int round) // 턴 상태 변경 처리
    {
        RefreshDisplay(); // 변경된 턴 UI 표시
    }

    private void RefreshDisplay() // 전체 턴 UI 갱신
    {
        if (turnRuntime == null) // 턴 관리자 연결 확인
        {
            return; // UI 갱신 중단
        }

        if (roundText != null) // 라운드 문구 연결 확인
        {
            roundText.text = $"ROUND {turnRuntime.CurrentRound}"; // 현재 라운드 표시
        }

        if (phaseText != null) // 단계 문구 연결 확인
        {
            phaseText.text = GetPhaseLabel(turnRuntime.CurrentPhase); // 현재 단계 표시
        }

        if (enemyIntentText != null) // 적 행동 예고 문구 연결 확인
        {
            bool showIntent = turnRuntime.CurrentPhase == BattleTurnPhase.EnemyIntent || turnRuntime.CurrentPhase == BattleTurnPhase.EnemyAction; // 행동 예고 표시 여부 계산
            enemyIntentText.text = showIntent ? $"ENEMY INTENT\n{turnRuntime.GetEnemyIntentSummary()}" : "ENEMY INTENT: HIDDEN"; // 행동 예고 표시
        }

        if (endTurnButton != null) // 턴 종료 버튼 연결 확인
        {
            endTurnButton.interactable = turnRuntime.IsPlayerTurn; // 플레이어 턴에만 버튼 활성화
        }
    }

    private string GetPhaseLabel(BattleTurnPhase phase) // 전투 단계 표시 문구 반환
    {
        switch (phase) // 현재 전투 단계 확인
        {
            case BattleTurnPhase.BattleSetup: // 전투 준비 단계
                return "BATTLE SETUP"; // 준비 문구 반환

            case BattleTurnPhase.PlayerTurn: // 플레이어 턴 단계
                return "PLAYER TURN"; // 플레이어 턴 문구 반환

            case BattleTurnPhase.EnemyIntent: // 적 행동 예고 단계
                return "ENEMY INTENT"; // 적 예고 문구 반환

            case BattleTurnPhase.EnemyAction: // 적 행동 단계
                return "ENEMY ACTION"; // 적 행동 문구 반환

            case BattleTurnPhase.StatusProcessing: // 상태 처리 단계
                return "STATUS PROCESSING"; // 상태 처리 문구 반환

            case BattleTurnPhase.BattleEnded: // 전투 종료 단계
                return "BATTLE ENDED"; // 전투 종료 문구 반환

            default: // 알 수 없는 단계
                return "UNKNOWN PHASE"; // 알 수 없음 문구 반환
        }
    }
}