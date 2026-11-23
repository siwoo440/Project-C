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
    private static readonly (string Title, string Description)[] Pages = // 처음부터 끝까지 실제 전투 안내 목록
    { // 전투 안내 페이지 목록 시작
        ("전투 화면", "카드를 사용해 적을 공격하거나 아군을 지원합니다. 전투는 아군과 적이 번갈아 진행합니다."), // 전투 안내 1번째 설명
        ("카드 사용", "다음을 눌러 안내를 접은 뒤 손패의 카드를 적이나 아군에게 끌어다 놓으세요. 전체 대상 카드는 같은 진영의 한 명에게 놓으면 모두에게 적용됩니다."), // 전투 안내 2번째 설명
        ("행동력 AP", "카드를 사용할 때 AP가 소모됩니다. 남은 AP는 다음 턴으로 이월되지 않습니다."), // 전투 안내 3번째 설명
        ("정신력", "행동과 전투 상황에 따라 정신력이 변합니다. 100은 각성, 0은 붕괴에 영향을 줍니다."), // 전투 안내 4번째 설명
        ("적 행동 예고", "적의 예고를 확인해 다음 행동에 대비하세요. 적을 가리키면 상세 내용을 볼 수 있습니다."), // 전투 안내 5번째 설명
        ("턴 종료", "다음을 눌러 안내를 접고 턴 종료 버튼을 누르세요. 적이 차례대로 행동합니다."), // 전투 안내 6번째 설명
        ("적 행동 관찰", "적의 공격과 상태 변화를 확인하세요. 아군 턴이 시작되면 안내를 마칠 수 있습니다.") // 전투 안내 7번째 설명
    }; // 전투 안내 페이지 목록 종료
    private const string CompletedKey = "battleTutorialCompleted"; // 기기별 완료 기록 이름
    private readonly ISettingsStore store; // 기기별 설정 저장소
    private bool cardUsed; // 실제 카드 사용 기록
    private bool enemyStarted; // 실제 적 턴 시작 기록
    private bool enemyFinished; // 적 턴 종료 기록
    public BattleTutorialStep Step { get; private set; } = BattleTutorialStep.Inactive; // 현재 안내 단계 조회
    public bool IsActive => Step != BattleTutorialStep.Inactive && Step != BattleTutorialStep.Complete; // 안내 표시 여부 조회
    public int PageCount => Pages.Length; // 실제 설명 목록의 전체 장수
    internal (string Title, string Description) CurrentPage => Pages[PageNumber - 1]; // 번호가 가리키는 실제 전투 페이지
    public int PageNumber => IsActive ? (int)Step - (int)BattleTutorialStep.Overview + 1 : 0; // 현재 실제 안내의 장 번호
    public bool CanGoBack => IsActive && Step > BattleTutorialStep.Overview; // 이전 안내 페이지 이동 가능 여부
    public bool CanAdvance => Step == BattleTutorialStep.Overview || Step == BattleTutorialStep.PlayCard && cardUsed || Step == BattleTutorialStep.ActionPoints || Step == BattleTutorialStep.Mental || Step == BattleTutorialStep.EnemyIntent || Step == BattleTutorialStep.EndTurn && enemyStarted || Step == BattleTutorialStep.ObserveEnemy && enemyFinished; // 실제 행동 기록을 반영한 다음 안내 가능 여부
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
    public bool Advance() // 실제 설명의 다음 장 또는 안내 완료
    { // 페이지 진행 시작
        if (!CanAdvance) // 카드 사용이나 적 턴을 기다리는 실습 단계 확인
        { // 대기 중 내용 이동 거부 시작
            return false; // 실제 게임 입력 후 진행 가능한 상태
        } // 대기 중 내용 이동 거부 종료
        if (PageNumber == PageCount) // 실제 안내 목록의 마지막 장 확인
        { // 전체 안내 완료 시작
            Finish(); // 완료 기록과 화면 즉시 종료
        } // 전체 안내 완료 종료
        else // 실제 설명의 다음 장 존재 확인
        { // 한 장씩 이동 시작
            SetStep((BattleTutorialStep)((int)Step + 1)); // 이미 실습했어도 해당 설명을 포함한 순차 이동
        } // 한 장씩 이동 종료
        return true; // 안내 진행 성공
    } // 페이지 진행 종료
    public bool GoBack() // 이전 안내 페이지 이동
    { // 이전 안내 이동 시작
        if (!CanGoBack) // 이전 페이지 존재 확인
        { // 이전 페이지 없음 처리 시작
            return false; // 이전 안내 이동 거부
        } // 이전 페이지 없음 처리 종료
        SetStep((BattleTutorialStep)((int)Step - 1)); // 실제 행동 기록을 유지한 이전 페이지 선택
        return true; // 이전 안내 이동 성공 반환
    } // 이전 안내 이동 종료
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
