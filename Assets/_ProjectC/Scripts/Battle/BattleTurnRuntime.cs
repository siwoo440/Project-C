using System; // 이벤트 자료형 기능
using System.Collections; // 코루틴 자료형
using System.Collections.Generic; // List 자료형 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleTurnRuntime : MonoBehaviour // 전투 턴 진행 관리자
{
    [Header("전투 시스템 연결")] // 전투 시스템 연결 구분
    [SerializeField] private BattleDeckRuntime deckRuntime; // 전투 덱 관리자
    [SerializeField] private BattleTargetSelectionRuntime targetSelectionRuntime; // 대상 선택 관리자
    [SerializeField] private BattleVictoryRuntime victoryRuntime; // 승리 판정 관리자
    [SerializeField] private BattleUnit playerUnit; // 플레이어 전투 유닛

    [Header("적 행동 연결")] // 적 행동 연결 구분
    [SerializeField] private List<BattleEnemyActionRuntime> enemyActions = new List<BattleEnemyActionRuntime>(); // 적 행동 목록

    [Header("진행 시간")] // 진행 시간 구분
    [SerializeField, Min(0f)] private float intentDisplayDuration = 1f; // 행동 예고 표시 시간
    [SerializeField, Min(0f)] private float enemyActionInterval = 0.4f; // 적 행동 사이 대기 시간
    [SerializeField, Min(0f)] private float statusProcessingDuration = 0.4f; // 상태 처리 표시 시간

    private Coroutine enemyTurnCoroutine; // 진행 중인 적 턴 코루틴
    private bool battleEnded; // 전투 종료 여부

    public BattleTurnPhase CurrentPhase { get; private set; } // 현재 전투 단계
    public int CurrentRound { get; private set; } // 현재 라운드
    public bool IsPlayerTurn => battleEnded == false && CurrentPhase == BattleTurnPhase.PlayerTurn; // 플레이어 입력 가능 여부
    public bool IsBattleEnded => battleEnded; // 전투 종료 여부 반환

    public event Action<BattleTurnPhase, int> TurnStateChanged; // 턴 상태 변경 알림
    public event Action BattleDefeated; // 전투 패배 알림

    private void Awake() // 전투 턴 상태 초기화
    {
        CurrentRound = 1; // 첫 라운드 설정
        CurrentPhase = BattleTurnPhase.BattleSetup; // 초기 준비 단계 설정
        battleEnded = false; // 전투 종료 상태 해제

        if (victoryRuntime == null) // 승리 관리자 연결 확인
        {
            victoryRuntime = FindFirstObjectByType<BattleVictoryRuntime>(); // 씬 승리 관리자 검색
        }
    }

    private void OnEnable() // 전투 승리 이벤트 연결
    {
        if (victoryRuntime != null) // 승리 관리자 연결 확인
        {
            victoryRuntime.BattleWon += HandleBattleWon; // 전투 승리 이벤트 등록
        }
    }

    private void Start() // 첫 플레이어 턴 시작
    {
        if (ValidateReferences() == false) // 필수 연결 상태 확인
        {
            battleEnded = true; // 전투 진행 차단
            SetPhase(BattleTurnPhase.BattleEnded); // 전투 종료 단계 설정
            return; // 첫 턴 시작 중단
        }

        SetPhase(BattleTurnPhase.PlayerTurn); // 첫 플레이어 턴 시작
        Debug.Log($"Player turn started: Round {CurrentRound}", this); // 첫 턴 시작 결과 출력
    }

    private void OnDisable() // 전투 턴 이벤트 해제
    {
        if (victoryRuntime != null) // 승리 관리자 연결 확인
        {
            victoryRuntime.BattleWon -= HandleBattleWon; // 전투 승리 이벤트 제거
        }

        if (enemyTurnCoroutine != null) // 적 턴 진행 여부 확인
        {
            StopCoroutine(enemyTurnCoroutine); // 진행 중인 적 턴 중단
            enemyTurnCoroutine = null; // 적 턴 코루틴 정보 제거
        }
    }

    public bool TryEndPlayerTurn() // 플레이어 턴 종료 시도
    {
        if (IsPlayerTurn == false) // 플레이어 턴 여부 확인
        {
            Debug.LogWarning("Player turn cannot be ended now.", this); // 잘못된 턴 종료 경고
            return false; // 턴 종료 실패 반환
        }

        if (deckRuntime == null) // 덱 관리자 연결 확인
        {
            Debug.LogError("Battle deck runtime is missing.", this); // 덱 관리자 누락 오류
            return false; // 턴 종료 실패 반환
        }

        if (targetSelectionRuntime != null) // 대상 선택 관리자 연결 확인
        {
            targetSelectionRuntime.CancelTargetSelection(); // 진행 중인 대상 선택 취소
        }

        deckRuntime.EndPlayerTurn(); // 남은 플레이어 손패 정리

        if (enemyTurnCoroutine != null) // 기존 적 턴 진행 여부 확인
        {
            StopCoroutine(enemyTurnCoroutine); // 기존 적 턴 진행 중단
        }

        enemyTurnCoroutine = StartCoroutine(RunEnemyTurn()); // 적 턴 진행 시작
        Debug.Log($"Player turn ended: Round {CurrentRound}", this); // 플레이어 턴 종료 결과 출력
        return true; // 턴 종료 성공 반환
    }

    public string GetEnemyIntentSummary() // 전체 적 행동 예고 생성
    {
        List<string> intentLines = new List<string>(); // 행동 예고 문구 목록

        for (int index = 0; index < enemyActions.Count; index++) // 적 행동 목록 순회
        {
            BattleEnemyActionRuntime enemyAction = enemyActions[index]; // 현재 적 행동 가져오기

            if (enemyAction == null) // 적 행동 연결 확인
            {
                continue; // 누락된 행동 건너뛰기
            }

            if (enemyAction.CanExecute() == false) // 행동 실행 가능 여부 확인
            {
                continue; // 실행 불가 행동 건너뛰기
            }

            intentLines.Add(enemyAction.GetIntentText()); // 행동 예고 문구 추가
        }

        if (intentLines.Count == 0) // 실행 가능한 적 행동 확인
        {
            return "NO ACTIVE ENEMY ACTION"; // 적 행동 없음 문구 반환
        }

        return string.Join("\n", intentLines); // 줄바꿈으로 행동 예고 결합
    }

    private IEnumerator RunEnemyTurn() // 적 턴 순차 진행
    {
        SetPhase(BattleTurnPhase.EnemyIntent); // 적 행동 예고 단계 설정

        if (intentDisplayDuration > 0f) // 행동 예고 시간 확인
        {
            yield return new WaitForSeconds(intentDisplayDuration); // 행동 예고 시간 대기
        }

        if (ShouldStopBattle()) // 전투 중단 조건 확인
        {
            enemyTurnCoroutine = null; // 적 턴 코루틴 정보 제거
            yield break; // 적 턴 진행 중단
        }

        SetPhase(BattleTurnPhase.EnemyAction); // 적 행동 단계 설정

        for (int index = 0; index < enemyActions.Count; index++) // 적 행동 목록 순회
        {
            BattleEnemyActionRuntime enemyAction = enemyActions[index]; // 현재 적 행동 가져오기

            if (enemyAction == null) // 적 행동 연결 확인
            {
                continue; // 누락된 행동 건너뛰기
            }

            if (enemyAction.CanExecute() == false) // 행동 실행 가능 여부 확인
            {
                continue; // 실행 불가 행동 건너뛰기
            }

            enemyAction.TryExecute(); // 현재 적 행동 실행

            if (playerUnit.IsDefeated) // 플레이어 전투 불능 확인
            {
                FinishBattleAsDefeat(); // 패배 상태 처리
                yield break; // 적 턴 진행 중단
            }

            if (enemyActionInterval > 0f) // 적 행동 대기 시간 확인
            {
                yield return new WaitForSeconds(enemyActionInterval); // 다음 적 행동 전 대기
            }

            if (ShouldStopBattle()) // 전투 중단 조건 확인
            {
                enemyTurnCoroutine = null; // 적 턴 코루틴 정보 제거
                yield break; // 적 턴 진행 중단
            }
        }

        SetPhase(BattleTurnPhase.StatusProcessing); // 상태 효과 처리 단계 설정

        if (statusProcessingDuration > 0f) // 상태 처리 시간 확인
        {
            yield return new WaitForSeconds(statusProcessingDuration); // 상태 처리 시간 대기
        }

        if (ShouldStopBattle()) // 다음 라운드 시작 가능 여부 확인
        {
            enemyTurnCoroutine = null; // 적 턴 코루틴 정보 제거
            yield break; // 다음 라운드 시작 중단
        }

        CurrentRound++; // 다음 라운드 증가
        deckRuntime.StartPlayerTurn(); // AP 초기화 및 카드 드로우
        enemyTurnCoroutine = null; // 적 턴 코루틴 정보 제거
        SetPhase(BattleTurnPhase.PlayerTurn); // 플레이어 턴 단계 설정
        Debug.Log($"Player turn started: Round {CurrentRound}", this); // 다음 턴 시작 결과 출력
    }

    private bool ShouldStopBattle() // 전투 중단 조건 확인
    {
        if (battleEnded) // 기존 전투 종료 확인
        {
            return true; // 전투 중단 반환
        }

        if (victoryRuntime != null && victoryRuntime.IsBattleWon) // 전투 승리 확인
        {
            return true; // 전투 중단 반환
        }

        return playerUnit != null && playerUnit.IsDefeated; // 플레이어 전투 불능 결과 반환
    }

    private void FinishBattleAsDefeat() // 플레이어 패배 처리
    {
        battleEnded = true; // 전투 종료 상태 설정
        enemyTurnCoroutine = null; // 적 턴 코루틴 정보 제거

        if (targetSelectionRuntime != null) // 대상 선택 관리자 연결 확인
        {
            targetSelectionRuntime.CancelTargetSelection(); // 대상 선택 상태 취소
        }

        SetPhase(BattleTurnPhase.BattleEnded); // 전투 종료 단계 설정
        Debug.Log("Battle defeat reached.", this); // 전투 패배 결과 출력
        BattleDefeated?.Invoke(); // 전투 패배 이벤트 전달
    }

    private void HandleBattleWon() // 전투 승리 처리
    {
        battleEnded = true; // 전투 종료 상태 설정

        if (enemyTurnCoroutine != null) // 적 턴 진행 여부 확인
        {
            StopCoroutine(enemyTurnCoroutine); // 진행 중인 적 턴 중단
            enemyTurnCoroutine = null; // 적 턴 코루틴 정보 제거
        }

        if (targetSelectionRuntime != null) // 대상 선택 관리자 연결 확인
        {
            targetSelectionRuntime.CancelTargetSelection(); // 대상 선택 상태 취소
        }

        SetPhase(BattleTurnPhase.BattleEnded); // 전투 종료 단계 설정
    }

    private void SetPhase(BattleTurnPhase nextPhase) // 전투 단계 변경
    {
        CurrentPhase = nextPhase; // 현재 전투 단계 저장
        Debug.Log($"Battle phase changed: {CurrentPhase} / Round {CurrentRound}", this); // 단계 변경 결과 출력
        TurnStateChanged?.Invoke(CurrentPhase, CurrentRound); // 턴 상태 변경 전달
    }

    private bool ValidateReferences() // 필수 연결 상태 검증
    {
        bool isValid = true; // 검증 결과 초기화

        if (deckRuntime == null) // 덱 관리자 연결 확인
        {
            Debug.LogError("Battle deck runtime is missing.", this); // 덱 관리자 누락 오류
            isValid = false; // 검증 실패 설정
        }

        if (playerUnit == null) // 플레이어 유닛 연결 확인
        {
            Debug.LogError("Player battle unit is missing.", this); // 플레이어 유닛 누락 오류
            isValid = false; // 검증 실패 설정
        }

        if (enemyActions.Count == 0) // 적 행동 등록 확인
        {
            Debug.LogError("No enemy action is registered.", this); // 적 행동 누락 오류
            isValid = false; // 검증 실패 설정
        }

        return isValid; // 전체 검증 결과 반환
    }
}