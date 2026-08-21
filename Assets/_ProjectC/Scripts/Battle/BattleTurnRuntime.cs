using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleTurnRuntime : IDisposable // 전투 턴 흐름 관리
{ // 클래스 시작
    private readonly BattleDeckRuntime runtimeDeck; // 연결된 런타임 덱
    private readonly BattleActionPointRuntime sharedActionPoints; // 연결된 공용 행동력
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 아군 런타임 목록
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 적 런타임 목록
    private readonly List<BattleUnitRuntime> registeredUnits = new List<BattleUnitRuntime>(); // 사망 이벤트 등록 유닛 목록
    private readonly int cardsPerPlayerTurn; // 플레이어 턴당 드로우 수
    private bool disposed; // 연결 해제 여부
    public int CurrentRound { get; private set; } // 현재 라운드
    public int LastDrawnCardCount { get; private set; } // 최근 드로우 수
    public BattleTurnPhase CurrentPhase { get; private set; } = BattleTurnPhase.NotStarted; // 현재 턴 단계
    public BattleType BattleType { get; } // 현재 전투 유형 조회
    public BattleResult Result { get; private set; } = BattleResult.None; // 현재 전투 결과 조회
    public bool IsPlayerTurn => CurrentPhase == BattleTurnPhase.PlayerTurn; // 플레이어 턴 여부
    public bool IsBattleEnded => Result != BattleResult.None; // 전투 종료 여부
    public bool CanEscape => !disposed && BattleType == BattleType.Normal && CurrentPhase == BattleTurnPhase.PlayerTurn && !IsBattleEnded; // 현재 도주 가능 여부
    public event Action StateChanged; // 턴 상태 변경 이벤트
    public event Action<BattleTurnPhase, int> PhaseStarted; // 진영 턴 시작 이벤트
    public BattleTurnRuntime(BattleDeckRuntime battleDeck, BattleActionPointRuntime actionPoints, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies, int drawCountPerTurn, BattleType battleType) // 턴 관리자 생성
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
        BattleType = battleType; // 전투 유형 저장
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
        PhaseStarted?.Invoke(CurrentPhase, CurrentRound); // 첫 플레이어 턴 시작 알림
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
        PhaseStarted?.Invoke(CurrentPhase, CurrentRound); // 적 턴 시작 알림
        if (!IsBattleEnded) // 상태 이상 처리 후 전투 지속 확인
        { // 전투 지속 처리 시작
            StateChanged?.Invoke(); // 턴 상태 변경 알림
        } // 전투 지속 처리 종료
        return true; // 턴 종료 성공 반환
    } // 플레이어 턴 종료 처리 종료
    public bool CompleteEnemyTurn() // 적 턴 완료
    { // 적 턴 완료 처리 시작
        if (disposed || CurrentPhase != BattleTurnPhase.EnemyTurn) // 적 턴 여부 확인
        { // 완료 불가 처리 시작
            return false; // 적 턴 완료 실패 반환
        } // 완료 불가 처리 종료
        CurrentRound++; // 다음 라운드 증가
        CurrentPhase = BattleTurnPhase.PlayerTurn; // 플레이어 턴 설정
        PhaseStarted?.Invoke(CurrentPhase, CurrentRound); // 플레이어 턴 시작 알림
        if (!IsBattleEnded) // 상태 이상 처리 후 전투 지속 확인
        { // 전투 지속 처리 시작
            sharedActionPoints.Restore(); // 공용 행동력 최대 회복
            LastDrawnCardCount = runtimeDeck.DrawCards(cardsPerPlayerTurn); // 턴 시작 카드 드로우
            StateChanged?.Invoke(); // 턴 상태 변경 알림
        } // 전투 지속 처리 종료
        return true; // 적 턴 완료 성공 반환
    } // 적 턴 완료 처리 종료
    public bool TryEscape() // 플레이어 도주 요청
    { // 도주 요청 시작
        if (!CanEscape) // 도주 가능 여부 확인
        { // 도주 불가 처리 시작
            return false; // 도주 실패 반환
        } // 도주 불가 처리 종료
        SetBattleResult(BattleResult.Escape); // 도주 결과 확정
        return true; // 도주 성공 반환
    } // 도주 요청 종료
    public bool RegisterSummonedEnemy(BattleUnitRuntime enemyUnit) // 소환 적 사망 판정 등록
    { // 소환 적 등록 시작
        if (disposed || IsBattleEnded || enemyUnit == null || enemyUnit.Team != BattleTeam.Enemy || !ContainsUnit(enemyUnits, enemyUnit)) // 소환 적 등록 조건 확인
        { // 등록 불가 처리 시작
            return false; // 소환 적 등록 실패 반환
        } // 등록 불가 처리 종료
        return RegisterUnitEvent(enemyUnit); // 소환 적 사망 이벤트 등록 결과 반환
    } // 소환 적 등록 종료
    public bool UnregisterEnemy(BattleUnitRuntime enemyUnit) // 제거 적 사망 판정 해제
    { // 제거 적 해제 시작
        return UnregisterUnitEvent(enemyUnit); // 적 사망 이벤트 해제 결과 반환
    } // 제거 적 해제 종료
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
        if (alliesDefeated) // 아군 전멸 우선 확인
        { // 패배 처리 시작
            SetBattleResult(BattleResult.Defeat); // 패배 결과 확정
            return; // 승패 판정 종료
        } // 패배 처리 종료
        if (enemiesDefeated) // 적 전멸 확인
        { // 승리 처리 시작
            SetBattleResult(BattleResult.Victory); // 승리 결과 확정
        } // 승리 처리 종료
    } // 유닛 사망 처리 종료
    private void SetBattleResult(BattleResult battleResult) // 전투 결과 확정
    { // 결과 확정 시작
        Result = battleResult; // 최종 전투 결과 저장
        CurrentPhase = battleResult == BattleResult.Victory ? BattleTurnPhase.Victory : battleResult == BattleResult.Defeat ? BattleTurnPhase.Defeat : BattleTurnPhase.Escaped; // 결과별 종료 단계 설정
        StateChanged?.Invoke(); // 전투 종료 알림
    } // 결과 확정 종료
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
            RegisterUnitEvent(runtimeUnit); // 개별 유닛 사망 이벤트 등록
        } // 유닛 이벤트 등록 종료
    } // 이벤트 등록 종료
    private bool RegisterUnitEvent(BattleUnitRuntime runtimeUnit) // 개별 사망 이벤트 등록
    { // 개별 이벤트 등록 시작
        if (runtimeUnit == null || registeredUnits.Contains(runtimeUnit)) // 등록 유닛 중복 확인
        { // 중복 등록 처리 시작
            return false; // 이벤트 등록 실패 반환
        } // 중복 등록 처리 종료
        runtimeUnit.Died += HandleUnitDied; // 사망 이벤트 등록
        registeredUnits.Add(runtimeUnit); // 등록 유닛 목록 추가
        return true; // 이벤트 등록 성공 반환
    } // 개별 사망 이벤트 등록 종료
    private bool UnregisterUnitEvent(BattleUnitRuntime runtimeUnit) // 개별 사망 이벤트 해제
    { // 개별 이벤트 해제 시작
        if (runtimeUnit == null || !registeredUnits.Remove(runtimeUnit)) // 등록 유닛 존재 확인
        { // 미등록 처리 시작
            return false; // 이벤트 해제 실패 반환
        } // 미등록 처리 종료
        runtimeUnit.Died -= HandleUnitDied; // 사망 이벤트 해제
        return true; // 이벤트 해제 성공 반환
    } // 개별 사망 이벤트 해제 종료
    private static bool ContainsUnit(IReadOnlyList<BattleUnitRuntime> units, BattleUnitRuntime targetUnit) // 런타임 목록 포함 확인
    { // 유닛 포함 검사 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 일치 확인 시작
            if (runtimeUnit == targetUnit) // 대상 유닛 일치 확인
            { // 일치 처리 시작
                return true; // 포함 상태 반환
            } // 일치 처리 종료
        } // 유닛 일치 확인 종료
        return false; // 미포함 상태 반환
    } // 유닛 포함 검사 종료
    public void Dispose() // 턴 관리자 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 연결 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 연결 해제 상태 저장
        for (int unitIndex = registeredUnits.Count - 1; unitIndex >= 0; unitIndex--) // 등록 유닛 역순 순회
        { // 이벤트 일괄 해제 시작
            UnregisterUnitEvent(registeredUnits[unitIndex]); // 개별 사망 이벤트 해제
        } // 이벤트 일괄 해제 종료
    } // 연결 해제 종료
} // 클래스 종료
