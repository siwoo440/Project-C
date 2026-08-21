using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleTurnRuntime : IDisposable // 전투 턴 흐름 관리
{ // 클래스 시작
    private readonly BattleDeckRuntime runtimeDeck; // 연결된 런타임 덱
    private readonly BattleActionPointRuntime sharedActionPoints; // 연결된 공용 행동력
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 아군 런타임 목록
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 적 런타임 목록
    private readonly int cardsPerPlayerTurn; // 플레이어 턴당 드로우 수
    private bool disposed; // 연결 해제 여부
    public int CurrentRound { get; private set; } // 현재 라운드
    public int LastDrawnCardCount { get; private set; } // 최근 드로우 수
    public BattleTurnPhase CurrentPhase { get; private set; } = BattleTurnPhase.NotStarted; // 현재 턴 단계
    public bool IsPlayerTurn => CurrentPhase == BattleTurnPhase.PlayerTurn; // 플레이어 턴 여부
    public bool IsBattleEnded => CurrentPhase == BattleTurnPhase.Victory || CurrentPhase == BattleTurnPhase.Defeat; // 전투 종료 여부
    public event Action StateChanged; // 턴 상태 변경 이벤트
    public BattleTurnRuntime(BattleDeckRuntime battleDeck, BattleActionPointRuntime actionPoints, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies, int drawCountPerTurn) // 턴 관리자 생성
    { // 생성자 시작
        runtimeDeck = battleDeck ?? throw new ArgumentNullException(nameof(battleDeck)); // 런타임 덱 저장
        sharedActionPoints = actionPoints ?? throw new ArgumentNullException(nameof(actionPoints)); // 공용 행동력 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        if (drawCountPerTurn < 0) // 턴 드로우 범위 확인
        { // 잘못된 드로우 처리 시작
            throw new ArgumentOutOfRangeException(nameof(drawCountPerTurn)); // 드로우 범위 예외
        } // 잘못된 드로우 처리 종료
        cardsPerPlayerTurn = drawCountPerTurn; // 턴당 드로우 수 저장
        RegisterUnitEvents(allyUnits); // 아군 사망 이벤트 등록
        RegisterUnitEvents(enemyUnits); // 적 사망 이벤트 등록
    } // 생성자 종료
    public bool StartBattle(int initialHandSize) // 전투 시작
    { // 전투 시작 처리 시작
        if (disposed || CurrentPhase != BattleTurnPhase.NotStarted || initialHandSize < 0) // 시작 가능 상태 확인
        { // 시작 불가 처리 시작
            return false; // 전투 시작 실패 반환
        } // 시작 불가 처리 종료
        CurrentRound = 1; // 첫 라운드 설정
        sharedActionPoints.Restore(); // 시작 공용 행동력 회복
        LastDrawnCardCount = runtimeDeck.DrawCards(initialHandSize); // 시작 손패 드로우
        CurrentPhase = BattleTurnPhase.PlayerTurn; // 플레이어 턴 설정
        StateChanged?.Invoke(); // 턴 상태 변경 알림
        return true; // 전투 시작 성공 반환
    } // 전투 시작 처리 종료
    public bool EndPlayerTurn() // 플레이어 턴 종료
    { // 플레이어 턴 종료 처리 시작
        if (disposed || CurrentPhase != BattleTurnPhase.PlayerTurn) // 플레이어 턴 여부 확인
        { // 종료 불가 처리 시작
            return false; // 턴 종료 실패 반환
        } // 종료 불가 처리 종료
        CurrentPhase = BattleTurnPhase.EnemyTurn; // 적 턴 설정
        StateChanged?.Invoke(); // 턴 상태 변경 알림
        return true; // 턴 종료 성공 반환
    } // 플레이어 턴 종료 처리 종료
    public bool CompleteEnemyTurn() // 적 턴 완료
    { // 적 턴 완료 처리 시작
        if (disposed || CurrentPhase != BattleTurnPhase.EnemyTurn) // 적 턴 여부 확인
        { // 완료 불가 처리 시작
            return false; // 적 턴 완료 실패 반환
        } // 완료 불가 처리 종료
        CurrentRound++; // 다음 라운드 증가
        sharedActionPoints.Restore(); // 공용 행동력 최대 회복
        LastDrawnCardCount = runtimeDeck.DrawCards(cardsPerPlayerTurn); // 턴 시작 카드 드로우
        CurrentPhase = BattleTurnPhase.PlayerTurn; // 플레이어 턴 설정
        StateChanged?.Invoke(); // 턴 상태 변경 알림
        return true; // 적 턴 완료 성공 반환
    } // 적 턴 완료 처리 종료
    private void HandleUnitDied(BattleUnitRuntime runtimeUnit) // 유닛 사망 처리
    { // 유닛 사망 처리 시작
        if (disposed || IsBattleEnded) // 전투 종료 상태 확인
        { // 판정 불가 처리 시작
            return; // 사망 판정 중단
        } // 판정 불가 처리 종료
        bool alliesDefeated = !HasLivingUnit(allyUnits); // 아군 전멸 여부 계산
        bool enemiesDefeated = !HasLivingUnit(enemyUnits); // 적 전멸 여부 계산
        if (!alliesDefeated && !enemiesDefeated) // 전투 지속 여부 확인
        { // 전투 지속 처리 시작
            return; // 종료 판정 중단
        } // 전투 지속 처리 종료
        CurrentPhase = enemiesDefeated ? BattleTurnPhase.Victory : BattleTurnPhase.Defeat; // 승패 단계 설정
        StateChanged?.Invoke(); // 전투 종료 알림
    } // 유닛 사망 처리 종료
    private static bool HasLivingUnit(IReadOnlyList<BattleUnitRuntime> units) // 생존 유닛 존재 확인
    { // 생존 유닛 검사 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 생존 상태 확인 시작
            if (runtimeUnit != null && !runtimeUnit.IsDead) // 생존 유닛 확인
            { // 생존 유닛 처리 시작
                return true; // 생존 유닛 존재 반환
            } // 생존 유닛 처리 종료
        } // 생존 상태 확인 종료
        return false; // 생존 유닛 없음 반환
    } // 생존 유닛 검사 종료
    private void RegisterUnitEvents(IReadOnlyList<BattleUnitRuntime> units) // 유닛 사망 이벤트 등록
    { // 이벤트 등록 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 이벤트 등록 시작
            if (runtimeUnit != null) // 유닛 존재 확인
            { // 유닛 존재 처리 시작
                runtimeUnit.Died += HandleUnitDied; // 사망 이벤트 등록
            } // 유닛 존재 처리 종료
        } // 유닛 이벤트 등록 종료
    } // 이벤트 등록 종료
    private void UnregisterUnitEvents(IReadOnlyList<BattleUnitRuntime> units) // 유닛 사망 이벤트 해제
    { // 이벤트 해제 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 이벤트 해제 시작
            if (runtimeUnit != null) // 유닛 존재 확인
            { // 유닛 존재 처리 시작
                runtimeUnit.Died -= HandleUnitDied; // 사망 이벤트 해제
            } // 유닛 존재 처리 종료
        } // 유닛 이벤트 해제 종료
    } // 이벤트 해제 종료
    public void Dispose() // 턴 관리자 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 연결 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 연결 해제 상태 저장
        UnregisterUnitEvents(allyUnits); // 아군 사망 이벤트 해제
        UnregisterUnitEvents(enemyUnits); // 적 사망 이벤트 해제
    } // 연결 해제 종료
} // 클래스 종료
