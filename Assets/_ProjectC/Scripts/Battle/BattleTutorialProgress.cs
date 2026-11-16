using System; // 저장소 예외 기능 사용
public enum BattleTutorialStep // 전투 안내 단계
{ // 안내 단계 시작
    Inactive, // 안내 미시작
    Overview, // 전투 화면 안내
    PlayCard, // 카드 사용 대기
    ActionPoints, // 행동력 안내
    Mental, // 정신력 안내
    EnemyIntent, // 적 행동 예고 안내
    EndTurn, // 턴 종료 대기
    ObserveEnemy, // 적 행동 관찰
    Complete // 안내 완료
} // 안내 단계 종료
public sealed class BattleTutorialProgress // 실제 행동에 따른 전투 안내 진행
{ // 안내 진행 시작
    private const string CompletedKey = "battleTutorialCompleted"; // 기기별 완료 기록 이름
    private readonly ISettingsStore store; // 기기별 설정 저장소
    private bool cardUsed; // 실제 카드 사용 기록
    private bool enemyStarted; // 실제 적 턴 시작 기록
    private bool enemyFinished; // 적 턴 종료 기록
    public BattleTutorialStep Step { get; private set; } = BattleTutorialStep.Inactive; // 현재 안내 단계 조회
    public bool IsActive => Step != BattleTutorialStep.Inactive && Step != BattleTutorialStep.Complete; // 안내 표시 여부 조회
    public bool CanAdvance => Step == BattleTutorialStep.Overview || Step == BattleTutorialStep.ActionPoints || Step == BattleTutorialStep.Mental || Step == BattleTutorialStep.EnemyIntent || Step == BattleTutorialStep.ObserveEnemy && enemyFinished; // 다음 안내 버튼 사용 가능 여부
    public event Action Changed; // 안내 단계 변경 알림
    public BattleTutorialProgress(ISettingsStore settingsStore) // 안내 진행 상태 생성
    { // 진행 상태 생성 시작
        store = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore)); // 유효한 저장소 보관
    } // 진행 상태 생성 종료
    public bool StartIfNeeded(bool enabled) // 설정과 완료 기록에 따른 자동 시작
    { // 자동 시작 검사 시작
        if (!enabled || store.Read(CompletedKey, 0) == 1 || IsActive) // 안내 설정과 완료 상태 확인
        { // 자동 시작 불가 처리 시작
            return false; // 자동 시작 거부
        } // 자동 시작 불가 처리 종료
        Replay(); // 처음부터 안내 시작
        return true; // 자동 시작 성공 반환
    } // 자동 시작 검사 종료
    public void Replay() // 명시적인 안내 다시 보기
    { // 다시 보기 시작
        cardUsed = false; // 이전 카드 사용 기록 초기화
        enemyStarted = false; // 이전 적 턴 시작 기록 초기화
        enemyFinished = false; // 이전 적 턴 종료 기록 초기화
        SetStep(BattleTutorialStep.Overview); // 첫 화면 안내 표시
    } // 다시 보기 종료
    public bool Advance() // 일반 설명의 다음 단계 이동
    { // 다음 단계 이동 시작
        switch (Step) // 현재 안내 단계 분기
        { // 단계 분기 시작
            case BattleTutorialStep.Overview: // 첫 전투 화면 안내
                SetStep(cardUsed ? BattleTutorialStep.ActionPoints : BattleTutorialStep.PlayCard); // 카드 사용 기록에 따른 다음 단계
                return true; // 단계 이동 성공 반환
            case BattleTutorialStep.ActionPoints: // 행동력 안내
                SetStep(BattleTutorialStep.Mental); // 정신력 안내 이동
                return true; // 단계 이동 성공 반환
            case BattleTutorialStep.Mental: // 정신력 안내
                SetStep(BattleTutorialStep.EnemyIntent); // 적 예고 안내 이동
                return true; // 단계 이동 성공 반환
            case BattleTutorialStep.EnemyIntent: // 적 예고 안내
                SetStep(enemyStarted ? BattleTutorialStep.ObserveEnemy : BattleTutorialStep.EndTurn); // 기존 적 턴 기록 반영
                return true; // 단계 이동 성공 반환
            case BattleTutorialStep.ObserveEnemy: // 적 행동 관찰 안내
                if (enemyFinished) // 실제 적 턴 종료 확인
                { // 관찰 완료 처리 시작
                    Finish(); // 안내 완료 기록
                    return true; // 완료 성공 반환
                } // 관찰 완료 처리 종료
                return false; // 적 행동 종료 대기
            default: // 실제 행동 대기 또는 안내 종료
                return false; // 수동 단계 이동 거부
        } // 단계 분기 종료
    } // 다음 단계 이동 종료
    public void OnCardUsed() // 실제 카드 사용 감지
    { // 카드 사용 감지 시작
        if (!IsActive) // 진행 중인 안내 확인
        { // 안내 없음 처리 시작
            return; // 카드 사용 기록 중단
        } // 안내 없음 처리 종료
        cardUsed = true; // 카드 사용 기록
        if (Step == BattleTutorialStep.PlayCard) // 카드 사용 대기 단계 확인
        { // 카드 사용 완료 처리 시작
            SetStep(BattleTutorialStep.ActionPoints); // 행동력 안내 이동
        } // 카드 사용 완료 처리 종료
    } // 카드 사용 감지 종료
    public void OnPhaseStarted(BattleTurnPhase phase) // 실제 진영 턴 시작 감지
    { // 턴 시작 감지 시작
        if (!IsActive) // 진행 중인 안내 확인
        { // 안내 없음 처리 시작
            return; // 턴 기록 중단
        } // 안내 없음 처리 종료
        if (phase == BattleTurnPhase.EnemyTurn) // 실제 적 턴 시작 확인
        { // 적 턴 시작 처리 시작
            enemyStarted = true; // 적 턴 시작 기록
            if (Step == BattleTutorialStep.EndTurn) // 턴 종료 대기 단계 확인
            { // 적 턴 관찰 이동 시작
                SetStep(BattleTutorialStep.ObserveEnemy); // 적 행동 관찰 안내 이동
            } // 적 턴 관찰 이동 종료
        } // 적 턴 시작 처리 종료
        else if (phase == BattleTurnPhase.PlayerTurn && enemyStarted) // 적 턴 이후 아군 턴 확인
        { // 적 턴 종료 처리 시작
            enemyFinished = true; // 적 턴 종료 기록
            Changed?.Invoke(); // 관찰 완료 상태 화면 갱신
        } // 적 턴 종료 처리 종료
    } // 턴 시작 감지 종료
    public void Skip() // 안내 건너뛰기
    { // 안내 건너뛰기 시작
        if (IsActive) // 진행 중인 안내 확인
        { // 진행 안내 종료 시작
            Finish(); // 완료 기록과 화면 종료
        } // 진행 안내 종료 종료
    } // 안내 건너뛰기 종료
    private void Finish() // 완료 상태 저장
    { // 완료 저장 시작
        store.Write(CompletedKey, 1); // 기기별 완료 여부 저장
        SetStep(BattleTutorialStep.Complete); // 안내 완료 단계 표시
    } // 완료 저장 종료
    private void SetStep(BattleTutorialStep next) // 현재 안내 단계 변경
    { // 단계 변경 시작
        Step = next; // 새 안내 단계 저장
        Changed?.Invoke(); // 화면 갱신 알림
    } // 단계 변경 종료
} // 안내 진행 종료
