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
    private int openingHandAfterAmbush = -1; // 적 선제 직후 첫 아군에게 줄 시작 손패 수
    private RelicInventoryRuntime openingRelics; // 현재 탐사 유물 보관함
    private bool openingAllySpeedActive; // 첫 적 턴까지 유지할 배지 상태
    private bool openingDieResolved; // 흠집 난 주사위의 전투 시작 판정 여부
    private BattleUnitRuntime openingDieAttackAlly; // 첫 실제 아군 턴에 공격력 보상 받을 선택 대상
    private bool openingPulseResolved; // 침식된 맥박줄의 첫 추첨 완료 여부
    private (BattleUnitRuntime Target, RelicData Relic) openingPulse; // 첫 실제 아군 턴에 부여할 맥박줄의 아군과 원본
    private int lastDustClockRound; // 어비스 먼지시계의 마지막 발동 아군 턴
    private BattleUnitRuntime openingVulnerableEnemy; // 첫 아군 턴까지 취약을 적용한 적
    private readonly List<(BattleUnitRuntime Enemy, BattleStatusEffectInstance Fear)> openingFearedEnemies = new List<(BattleUnitRuntime Enemy, BattleStatusEffectInstance Fear)>(); // 포스터가 새로 공포를 부여한 적과 상태
    private int openingSpecimenActionPoints; // 첫 아군 턴에 지급할 탐사 표본병 행동력
    private int openingBottleActionPoints; // 첫 아군 턴에 지급할 바닥 없는 병 행동력
    private bool firstMentalDamageNullified; // 이번 전투의 검은 심연판 공용 보호 사용 여부
    private readonly ExplorationSessionManager explorationRun; // 현재 전투에 연결된 실제 탐사 회차
    private readonly object activeRunEncounter; // 전투를 시작한 고유 탐사 조우 토큰
    private readonly HashSet<BattleUnitRuntime> pendingPermanentAttackDefeats; // 아직 처리를 받지 않은 실제 초기 정예·보스 개체
    private int nextPlayerTurnActionPointLoss; // 거인의 방패 사용으로 다음 아군 턴에 차감할 행동력
    private int lastVolatileDrawRound; // 안전핀의 마지막 발동 라운드
    private int lastExcludedBlockRound; // 봉인왁스의 마지막 발동 라운드
    private int lastFlagDrawRound; // 회전 깃발의 마지막 드로우 라운드
    private int lastSpringRound; // 붉은 스프링의 마지막 할인 준비 라운드
    private int lastEmptyHandDrawRound; // 패치 노끈의 마지막 예약 라운드
    private bool springReady; // 다음 유료 고정 카드에 남은 할인
    private int pendingSwordThenShieldBlock; // 검 사용 뒤 다음 방패에 예약한 소유자 방어도
    private readonly BattleUnitRuntime authoredBoss; // 보스 조우에 배치된 첫 번째 적의 고정 참조
    private bool revivedWithAbyssGrail; // 이번 전투 성배 조각의 한 번 사용 기록
    private bool resolvingCard; // 사용 카드가 손패를 떠날 때까지의 정산 상태
    private bool deferVictoryUntilCardResolved; // 지정된 피해와 회복을 모두 적용할 때까지의 승리 정산 예약
    private int pendingDefeatDraw; // 카드 정산 후 지급할 회전 깃발 드로우
    private long reflectionDamageNumerator; // 이번 적 턴에 누적한 체력 피해와 반사 비율의 곱
    private BattleUnitRuntime reflectionSource; // 처음 반사 대상으로 기록한 피격 아군
    private bool corrosionSutureSettled; // 전투 종료 침식 회복 적용 여부
    private bool namelessOathSettled; // 결과 저장에 서약 비용을 적용한 여부
    private bool firstEnemyTurnFinished; // 선제와 라운드 번호에 관계없이 첫 적 턴을 마친 여부
    private bool disposed; // 연결 해제 여부
    public int CurrentRound { get; private set; } // 현재 라운드
    public int LastDrawnCardCount { get; private set; } // 최근 드로우 수
    internal BattleDeckRuntime Deck => runtimeDeck; // 손패 버림 신호를 발행하는 현재 전투 덱
    public BattleTurnPhase CurrentPhase { get; private set; } = BattleTurnPhase.NotStarted; // 현재 턴 단계
    public BattleType BattleType { get; } // 현재 전투 유형 조회
    public BattleResult Result { get; private set; } = BattleResult.None; // 현재 전투 결과 조회
    public bool IsPlayerTurn => CurrentPhase == BattleTurnPhase.PlayerTurn; // 플레이어 턴 여부
    public bool IsBattleEnded => Result != BattleResult.None; // 전투 종료 여부
    private bool IsActiveExplorationBattle => !disposed && !IsBattleEnded && CurrentRound > 0 && explorationRun != null && activeRunEncounter != null && ReferenceEquals(activeRunEncounter, explorationRun.ActiveEncounterIdentity); // 종료 전 실제 탐사 조우와 전투의 유효한 연결
    internal int PermanentAttackPower => IsActiveExplorationBattle ? explorationRun.PermanentAttackPower : 0; // 실제 탐사 전투에 출전한 아군의 영구 공격력
    public bool HasPendingBossPhaseTransition { get; private set; } // 보스 페이즈 전환 예약 여부
    public bool CanEscape => !disposed && (BattleType == BattleType.Normal || BattleType == BattleType.Elite) && CurrentPhase == BattleTurnPhase.PlayerTurn && !IsBattleEnded; // 일반과 정예 전투의 도주 가능 여부
    private bool CanSelectDuringPlayerTurn => !disposed && IsPlayerTurn && !IsBattleEnded && !HasPendingBossPhaseTransition; // 현재 아군 턴의 카드 대상 선택 가능 여부
    internal bool HasSelectableEnemy => CanSelectDuringPlayerTurn && HasLivingUnit(enemyUnits); // 현재 적 대상으로 고를 생존 참가자 확인
    internal IReadOnlyList<BattleUnitRuntime> BlessingAllies => allyUnits; // 축복 이전이 확인하는 실제 전투 아군 명단
    internal int CountSelectableEnemyPair() // 실제 적 고르기의 최대 두 생존 대상 수
    { // 선택 가능한 적 수 확인 시작
        if (!CanSelectDuringPlayerTurn) // 전투 진행 및 현재 아군의 대상 선택 가능 상태 확인
        { // 현재 선택 불가 시작
            return 0; // 선택 가능한 적 없음 반환
        } // 현재 선택 불가 종료
        BattleUnitRuntime first = null; // 서로 다른 두 개체를 비교할 첫 생존 적
        foreach (var enemy in enemyUnits) // 현재 전투의 실제 적 명단 순회
        { // 개별 적 대상 확인 시작
            if (enemy?.Team != BattleTeam.Enemy || enemy.IsDead || enemy == first) // 적 진영과 생존 상태 및 중복 참가자 확인
            { // 선택 불가 적 제외 시작
                continue; // 다른 적 후보 확인
            } // 선택 불가 적 제외 종료
            if (first != null) // 서로 다른 두 번째 생존 적 발견 확인
            { // 최대 선택 수 도달 시작
                return 2; // 서로 다른 생존 적 둘 표시
            } // 최대 선택 수 도달 종료
            first = enemy; // 첫 번째 실제 생존 적 기록
        } // 개별 적 대상 확인 종료
        return first == null ? 0 : 1; // 적 없음과 단일 적을 구분한 선택 수 반환
    } // 선택 가능한 적 수 확인 종료
    internal bool CanSelectLivingUnit(BattleUnitRuntime unit, BattleTeam team) // 현재 전투의 진영별 생존 대상 선택 확인
    { // 생존 참가자 검사 시작
        return CanSelectDuringPlayerTurn && unit?.Team == team && !unit.IsDead && // 카드 대상 선택이 가능한 진행 상태와 진영 확인
            ContainsUnit(team == BattleTeam.Ally ? allyUnits : enemyUnits, unit); // 실제 전투에 등록된 동일 유닛 확인
    } // 생존 참가자 검사 종료
    internal bool HasFallenAlly() // 손패 카드가 부활시킬 수 있는 아군 존재 확인
    { // 전투불능 아군 검사 시작
        if (CanSelectDuringPlayerTurn) // 이번 턴 부활 가능 여부 확인
        { // 전투 참가 명단 검사 시작
            foreach (BattleUnitRuntime ally in allyUnits) // 현재 전투 아군 순회
            { // 개별 아군 검사 시작
                if (ally?.Team == BattleTeam.Ally && ally.IsDead) // 현재 명단의 전투불능 아군 확인
                { // 부활 후보 발견 시작
                    return true; // 부활 가능 카드 표시
                } // 부활 후보 발견 종료
            } // 개별 아군 검사 종료
        } // 전투 참가 명단 검사 종료
        return false; // 현재 부활 가능한 대상 없음 반환
    } // 전투불능 아군 검사 종료
    internal bool CanReviveFallenAlly(BattleUnitRuntime ally) // 클릭과 드래그가 공유하는 실제 부활 조건
    { // 대상 검증 시작
        return CanSelectDuringPlayerTurn && ally?.Team == BattleTeam.Ally && ally.IsDead && ContainsUnit(allyUnits, ally); // 전투 단계와 현재 참가 명단의 사망 상태 확인
    } // 대상 검증 종료
    internal int TryReviveFallenAlly(BattleUnitRuntime ally, int healthPercent) // 기존 아군 런타임의 부활 처리
    { // 부활 요청 처리 시작
        return CanReviveFallenAlly(ally) ? ally.Revive(healthPercent) : 0; // 실제 충돌 시에도 같은 대상과 단계 재검사
    } // 부활 요청 처리 종료
    public bool HasOtherLivingAlly(BattleUnitRuntime owner) // 본인 외 생존 아군 확인
    { // 생존 동료 확인 시작
        foreach (BattleUnitRuntime ally in allyUnits) // 아군 목록 순회
        { // 생존 동료 검사 시작
            if (ally != null && ally != owner && !ally.IsDead) // 다른 생존 아군 확인
            { // 생존 동료 발견 시작
                return true; // 생존 동료 존재 반환
            } // 생존 동료 발견 종료
        } // 생존 동료 검사 종료
        return false; // 생존 동료 없음 반환
    } // 생존 동료 확인 종료
    internal bool AreAllLivingAlliesAtOrBelowHealthPercent(int percentage) // 모든 생존 아군의 개별 체력 비율 확인
    { // 생존 아군 체력 확인 시작
        bool foundLivingAlly = false; // 검사한 생존 아군 여부
        foreach (BattleUnitRuntime ally in allyUnits) // 현재 아군 목록 순회
        { // 아군별 현재 체력 검사 시작
            if (ally == null || ally.Team != BattleTeam.Ally || ally.IsDead) // 생존 아군 대상 여부
            { // 생존 아군 외 대상 제외 시작
                continue; // 다음 아군 확인
            } // 생존 아군 외 대상 제외 종료
            if ((long)ally.CurrentHealth * 100 > (long)ally.MaxHealth * percentage) // 소수점 반올림 없는 개별 체력 한도 비교
            { // 개별 체력 한도 초과 시작
                return false; // 파티 전체 조건 불충족 반환
            } // 개별 체력 한도 초과 종료
            foundLivingAlly = true; // 생존 아군 존재 기록
        } // 아군별 현재 체력 검사 종료
        return foundLivingAlly; // 한 명 이상 생존한 파티의 조건 충족 여부
    } // 생존 아군 체력 확인 종료
    internal BattleUnitRuntime GetLowestHealthLivingAlly() // 현재 체력이 가장 낮은 생존 아군 조회
    { // 자동 회복 대상 조회 시작
        BattleUnitRuntime selected = null; // 선택한 생존 아군의 초기값
        foreach (BattleUnitRuntime ally in allyUnits) // 처음 출전한 고정 명단 순서로 탐색
        { // 생존 아군과 현재 체력 비교 시작
            if (ally != null && !ally.IsDead && ally.Team == BattleTeam.Ally && (selected == null || ally.CurrentHealth < selected.CurrentHealth)) // 현재 수치가 더 낮을 때만 대상 변경
            { // 최소 체력의 생존 아군 갱신 시작
                selected = ally; // 동률의 앞선 명단을 유지한 새 최소 대상
            } // 최소 체력의 생존 아군 갱신 종료
        } // 생존 아군과 현재 체력 비교 종료
        return selected; // 최대 체력인 경우도 포함하는 유효한 자동 선택
    } // 자동 회복 대상 조회 종료
    public event Action StateChanged; // 턴 상태 변경 이벤트
    public event Action<BattleTurnPhase, int> PhaseStarted; // 진영 턴 시작 이벤트
    public event Action<BattleTurnPhase, int> PhaseEnded; // 진영 턴 종료 이벤트
    public event Action<BattleResult> BattleCompleted; // 전투 종료 처리 완료 이벤트
    public BattleTurnRuntime(BattleDeckRuntime battleDeck, BattleActionPointRuntime actionPoints, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies, int drawCountPerTurn, BattleType battleType, ExplorationSessionManager exploration = null) // 실제 탐사 조우를 선택 연결하는 턴 관리자 생성
    { // 생성자 시작
        runtimeDeck = battleDeck ?? throw new ArgumentNullException(nameof(battleDeck)); // 런타임 덱 저장
        sharedActionPoints = actionPoints ?? throw new ArgumentNullException(nameof(actionPoints)); // 공용 행동력 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        if (drawCountPerTurn < 0) // 턴 드로우 범위 확인
        { // 잘못된 드로우 처리 시작
            throw new ArgumentOutOfRangeException(nameof(drawCountPerTurn)); // 드로우 범위 예외
        } // 잘못된 드로우 처리 종료
        if (exploration != null && exploration.ActiveEncounter?.BattleType == battleType) // 실제 탐사에서 시작한 같은 등급의 활성 조우 확인
        { // 현재 탐사 조우 연결 시작
            explorationRun = exploration; // 현재 탐사에만 있는 영구 보상 상태 연결
            activeRunEncounter = exploration.ActiveEncounterIdentity; // 같은 조우 재입장과 구분할 실제 전투 토큰 보관
            foreach (BattleUnitRuntime ally in allyUnits) // 현재 출전한 실제 아군의 공격력 연결
            { // 개별 출전 아군 확인 시작
                if (ally?.Team == BattleTeam.Ally) // 공용 공격력을 받을 실제 아군 판정
                { // 개별 아군 공격력 연결 시작
                    ally.ExplorationAttackPowerBattle = this; // 현재 활성 탐사 전투에서만 쓰는 공격력 원본 연결
                } // 개별 아군 공격력 연결 종료
            } // 개별 출전 아군 확인 종료
            if (battleType == BattleType.Elite) // 정예 조우의 초기 정예 편성 확인
            { // 정예 처치 개체 기록 시작
                pendingPermanentAttackDefeats = new HashSet<BattleUnitRuntime>(enemyUnits); // 전투 도중 소환한 적을 제외한 초기 정예 개체 보관
            } // 정예 처치 개체 기록 종료
            else if (battleType == BattleType.Boss && enemyUnits.Count > 0) // 실제 보스 조우에 원래 편성한 첫 적 확인
            { // 보스 개체 기록 시작
                pendingPermanentAttackDefeats = new HashSet<BattleUnitRuntime> { enemyUnits[0] }; // 기존 보스 판정과 같은 첫 적으로 일반 동행 적 및 소환 적 제외
            } // 보스 개체 기록 종료
        } // 현재 탐사 조우 연결 종료
        cardsPerPlayerTurn = drawCountPerTurn; // 턴당 드로우 수 저장
        BattleType = battleType; // 전투 유형 저장
        runtimeDeck.CardExcluded += HandleExcludedCard; // 실제 카드 제외 결과 연결
        RegisterUnitEvents(allyUnits); // 아군 사망 이벤트 등록
        RegisterUnitEvents(enemyUnits); // 적 사망 이벤트 등록
        authoredBoss = battleType == BattleType.Boss && enemyUnits.Count > 0 ? enemyUnits[0] : null; // 원본 보스와 다른 배치 및 소환 적 구분
    } // 생성자 종료
    public bool StartBattle(int initialHandSize, bool enemyFirst = false, Action openingEffects = null) // 먼저 행동할 진영과 탐사 효과를 반영한 시작
    { // 전투 시작 처리 시작
        if (disposed || CurrentRound != 0 || CurrentPhase != BattleTurnPhase.NotStarted || initialHandSize < 0) // 개막 효과 진행 중인 재호출과 잘못된 시작 차단
        { // 시작 불가 처리 시작
            return false; // 전투 시작 실패 반환
        } // 시작 불가 처리 종료
        CurrentRound = 1; // 첫 라운드 설정
        openingRelics = RelicRunManager.Instance?.Inventory; // 전투 진입 시 실제 탐사 유물 조회
        runtimeDeck.SetFirstDefensiveRelicApDiscount(openingRelics?.FindEffect(RelicEffectType.FirstDefensiveCardDiscount)?.EffectValue ?? 0); // 이번 전투의 첫 유료 방어 할인 준비
        ApplyOpeningEnemyStatus(RelicEffectType.OpeningSpeedLoss); // 안테나의 첫 적 속도 약화 적용
        ApplyOpeningEnemyStatus(RelicEffectType.OpeningDroneWeakness); // 드론 약화를 첫 공격 예고 전에 적용
        ApplyOpeningAllySpeed(); // 전투 시작 시 생존 아군 속도 증가
        ResolveOpeningBottle(); // 첫 행동 준비 전에 바닥 없는 병의 전체 정신력 비용 결제
        openingEffects?.Invoke(); // 전투 시작 성공 확인 뒤 첫 행동 전에 탐사 효과 소비
        ResolveOpeningDie(); // 실제 선제 여부가 적용되기 전에 주사위 보상 한 번 선정
        ApplyOpeningCorrosion(); // 선제 공격 전에 실제 생존 적에게 실금석의 마법 저항 약화 적용
        ApplyOpeningStethoscopeMark(); // 기존 첫 턴 준비 중 최대 현재 체력 적에게 청진기 표식 적용
        ResolveOpeningPulse(); // 현재 실제 생존 아군 한 명과 시작 정신력 비용 확정
        ApplyAbyssLampOpening(); // 첫 진영 시작 전에 등잔의 적 약화와 아군 정신력 비용 정산
        ApplyNamelessOathOpening(); // 전투에 입장한 생존 아군의 전투용 일반 공격력 부여
        ApplyOpeningEnemyStatus(RelicEffectType.OpeningEnemyAttackReduction); // 금 간 접시 장식이 고른 생존 적에게만 약화 시도
        if (enemyFirst) // 방랑자의 고정 선제 판정 확인
        { // 적의 첫 행동 시작
            openingHandAfterAmbush = initialHandSize; // 첫 아군 턴의 정상 시작 손패 보관
            sharedActionPoints.Spend(sharedActionPoints.CurrentActionPoints); // 적 선제 중 아군 행동력 소모 방지용 비활성화
            CurrentPhase = BattleTurnPhase.EnemyTurn; // 아군 턴을 건너뛰지 않고 적부터 시작
            NotifyPhaseStarted(); // 적 진영 첫 턴 알림
            if (!IsBattleEnded) // 선제 적 턴에 일어난 적 전멸 확인
            { // 실제 적 행동 실행 가능 시작
                StateChanged?.Invoke(); // 실제 적 행동 실행
            } // 실제 적 행동 실행 가능 종료
            return true; // 선제 전투 시작 완료
        } // 적의 첫 행동 종료
        ApplyFirstPlayerTurnEnemyStatuses(); // 실제 첫 아군 턴을 위한 검은 왕관과 포스터 약화
        sharedActionPoints.Restore(); // 시작 공용 행동력 회복
        DrawPlayerHand(initialHandSize); // 실제 첫 아군 턴의 손패 드로우
        CurrentPhase = BattleTurnPhase.PlayerTurn; // 플레이어 턴 설정
        NotifyPhaseStarted(); // 첫 플레이어 턴 시작 알림
        StateChanged?.Invoke(); // 턴 상태 변경 알림
        return true; // 전투 시작 성공 반환
    } // 전투 시작 처리 종료
    public bool EndPlayerTurn() // 플레이어 턴 종료
    { // 플레이어 턴 종료 처리 시작
        if (disposed || CurrentPhase != BattleTurnPhase.PlayerTurn) // 플레이어 턴 여부 확인
        { // 종료 불가 처리 시작
            return false; // 턴 종료 실패 반환
        } // 종료 불가 처리 종료
        CleanupPlayerTurn(); // 일반 손패와 남은 행동력 정리
        NotifyPhaseEnded(); // 플레이어 턴 종료 알림
        if (IsBattleEnded) // 턴 종료 효과로 전투 종료 확인
        { // 종료된 전투 처리 시작
            return true; // 종료 결과 단계 유지
        } // 종료된 전투 처리 종료
        CurrentPhase = BattleTurnPhase.EnemyTurn; // 적 턴 설정
        NotifyPhaseStarted(); // 적 턴 시작 알림
        if (!IsBattleEnded) // 상태 이상 처리 후 전투 지속 확인
        { // 전투 지속 처리 시작
            StateChanged?.Invoke(); // 턴 상태 변경 알림
        } // 전투 지속 처리 종료
        return true; // 턴 종료 성공 반환
    } // 플레이어 턴 종료 처리 종료
    public void QueueBossPhaseTransition() // 보스 페이즈 전환 예약
    { // 전환 예약 시작
        if (!disposed && !IsBattleEnded && BattleType == BattleType.Boss) // 진행 중인 보스 전투 확인
        { // 전환 예약 처리 시작
            HasPendingBossPhaseTransition = true; // 미처리 타격 중단용 예약 저장
        } // 전환 예약 처리 종료
    } // 전환 예약 종료
    public bool ClearBossPhaseTransition() // 완료된 보스 전환 예약 해제
    { // 예약 해제 시작
        if (!HasPendingBossPhaseTransition) // 기존 예약 확인
        { // 예약 없음 처리 시작
            return false; // 해제 실패 반환
        } // 예약 없음 처리 종료
        HasPendingBossPhaseTransition = false; // 전환 예약 해제
        return true; // 예약 해제 성공 반환
    } // 예약 해제 종료
    public bool CompleteBossPhaseTransition() // 적 행동을 건너뛰고 다음 플레이어 턴 시작
    { // 강제 턴 전환 시작
        if (disposed || IsBattleEnded || !HasPendingBossPhaseTransition || CurrentPhase != BattleTurnPhase.PlayerTurn) // 전환 가능 상태 확인
        { // 전환 불가 처리 시작
            return false; // 전환 실패 반환
        } // 전환 불가 처리 종료
        HasPendingBossPhaseTransition = false; // 전환 예약 해제
        CleanupPlayerTurn(); // 일반 종료와 동일한 손패와 행동력 정리
        NotifyPhaseEnded(); // 아군 턴 종료 효과 한 번 처리
        if (IsBattleEnded) // 종료 효과로 전투 종료 확인
        { // 종료된 전투 처리 시작
            return true; // 확정된 전투 결과 유지
        } // 종료된 전투 처리 종료
        ClearRoundBlock(allyUnits); // 라운드 전환 아군 방어도 정리
        ClearRoundBlock(enemyUnits); // 라운드 전환 적 방어도 정리
        CurrentRound++; // 다음 라운드 진행
        CurrentPhase = BattleTurnPhase.PlayerTurn; // 다음 아군 턴 설정
        NotifyPhaseStarted(); // 다음 아군 턴 시작 효과 처리
        if (!IsBattleEnded) // 시작 효과 처리 후 전투 지속 확인
        { // 다음 턴 준비 시작
            sharedActionPoints.Restore(); // 공용 행동력 회복
            DrawPlayerHand(cardsPerPlayerTurn); // 다음 턴의 기본 및 예약 손패 드로우
            StateChanged?.Invoke(); // 전투 화면 갱신 알림
        } // 다음 턴 준비 종료
        return true; // 강제 턴 전환 성공 반환
    } // 강제 턴 전환 종료
    private void CleanupPlayerTurn() // 일반 플레이어 턴 종료 공통 정리
    { // 플레이어 턴 정리 시작
        if (runtimeDeck.HandCount == 0 && lastEmptyHandDrawRound != CurrentRound) // 자동 버림 전 빈손과 중복 발동 확인
        { // 패치 노끈 발동 검사 시작
            var cord = openingRelics?.FindEffect(RelicEffectType.EmptyHandNextTurnDraw); // 보유한 낡은 패치 노끈 조회
            if (cord != null) // 패치 노끈 보유 확인
            { // 다음 턴 드로우 예약 시작
                lastEmptyHandDrawRound = CurrentRound; // 같은 턴의 중복 예약 차단
                runtimeDeck.QueuePlayerTurnDraw(cord.EffectValue); // 다음 아군 턴의 추가 드로우 예약
            } // 다음 턴 드로우 예약 종료
        } // 패치 노끈 발동 검사 종료
        runtimeDeck.DiscardHand(); // 남은 일반 손패를 버림 더미로 이동
        runtimeDeck.SetRelicApDiscount(0); // 끝난 아군 턴의 유물 할인 해제
        runtimeDeck.ClearFirstDrawRelicDiscount(); // 종료된 턴의 첫 드로우 카드 비용 복원
        runtimeDeck.ClearOpeningRandomRelicDiscount(); // 첫 턴이 끝난 왕관 할인 복원
        springReady = false; // 지난 턴의 붉은 스프링 예약 해제
        runtimeDeck.SetNextCardRelicApDiscount(0); // 다음 카드에 남은 비용 표시 복구
        sharedActionPoints.Spend(sharedActionPoints.CurrentActionPoints); // 이월되지 않는 행동력 소멸
        sharedActionPoints.ClearTurnCapacity(); // 끝난 아군 턴의 임시 행동력 상한 회수
    } // 플레이어 턴 정리 종료
    public bool CompleteEnemyTurn() // 적 턴 완료
    { // 적 턴 완료 처리 시작
        if (disposed || CurrentPhase != BattleTurnPhase.EnemyTurn) // 적 턴 여부 확인
        { // 완료 불가 처리 시작
            return false; // 적 턴 완료 실패 반환
        } // 완료 불가 처리 종료
        NotifyPhaseEnded(); // 적 턴 종료 알림
        firstEnemyTurnFinished = true; // 실제 첫 적 턴이 끝나면 같은 라운드에도 실명 저항 종료
        ClearOpeningAllySpeed(); // 첫 적 턴이 끝나면 배지 속도 회수
        ResolveReflectionCitadel(); // 마지막 적 행동과 일반 턴 종료 효과 뒤 성채 전체 반사 정산
        if (IsBattleEnded) // 턴 종료 효과로 전투 종료 확인
        { // 종료된 전투 처리 시작
            return true; // 종료 결과 단계 유지
        } // 종료된 전투 처리 종료
        ClearRoundBlock(allyUnits); // 라운드 전환 시 아군 방어도 정리
        ClearRoundBlock(enemyUnits); // 라운드 전환 시 적 방어도 정리
        int drawCount = cardsPerPlayerTurn; // 보통 다음 라운드의 드로우 수
        if (openingHandAfterAmbush >= 0) // 첫 적 선제가 끝났는지 확인
        { // 첫 아군 턴 준비 시작
            drawCount = openingHandAfterAmbush; // 정상 시작 손패 수 복원
            openingHandAfterAmbush = -1; // 다음 적 턴부터 정상 라운드 진행
            ApplyFirstPlayerTurnEnemyStatuses(); // 적 선제 뒤 첫 실제 아군 턴 전용 약화 적용
        } // 첫 아군 턴 준비 종료
        else // 일반 적 턴 종료
        { // 새 라운드 준비 시작
            CurrentRound++; // 일반 적 턴이 끝나면 다음 라운드로 이동
        } // 새 라운드 준비 종료
        CurrentPhase = BattleTurnPhase.PlayerTurn; // 플레이어 턴 설정
        NotifyPhaseStarted(); // 플레이어 턴 시작 알림
        if (!IsBattleEnded) // 상태 이상 처리 후 전투 지속 확인
        { // 전투 지속 처리 시작
            sharedActionPoints.Restore(); // 공용 행동력 최대 회복
            DrawPlayerHand(drawCount); // 선제 뒤 시작 손패 또는 새 라운드와 예약 손패 지급
            StateChanged?.Invoke(); // 턴 상태 변경 알림
        } // 전투 지속 처리 종료
        return true; // 적 턴 완료 성공 반환
    } // 적 턴 완료 처리 종료
    internal void RecordReflectionCitadelDamage(BattleEventContext context) // 실제 적 턴의 체력 피해 공용 프레임 집계
    { // 성채 피해 집계 시작
        if (context.Phase != BattleTurnPhase.EnemyTurn || context.SourceUnit?.Team != BattleTeam.Enemy || context.TargetUnit?.Team != BattleTeam.Ally || context.AppliedAmount <= 0 || context.DamageResult?.FromEnemyDirectAttack != true) // 실제 직접 적중 및 연결된 아군 피해 확인
        { // 반사에 포함되지 않는 피해 처리 시작
            return; // 조기 피격과 흡수 및 지속 피해 제외
        } // 반사에 포함되지 않는 피해 처리 종료
        int percent = Math.Min(100, context.TargetUnit.GetStatusEffect(BattleStatusEffectType.ReflectionCitadel)?.EffectiveValue ?? 0); // 피격 아군이 이번 적 턴에 받은 성채의 반사 비율
        if (percent > 0) // 실제 아군 보호 상태 적용 확인
        { // 반사량 누적 시작
            reflectionDamageNumerator = Math.Min((long)int.MaxValue * 100L, reflectionDamageNumerator + (long)context.AppliedAmount * percent); // 소수점을 남겨 둔 파티 실제 체력 손실 누적
            reflectionSource ??= context.TargetUnit; // 반사의 최초 피격 아군 보관
        } // 반사량 누적 종료
    } // 성채 피해 집계 종료
    private void ResolveReflectionCitadel() // 적 행동을 마친 뒤 한 번에 반사 피해 정산
    { // 성채 반사 정산 시작
        int reflected = (int)(reflectionDamageNumerator / 100L); // 한 적 턴의 전체 피해를 합친 뒤 한 번만 내림
        BattleUnitRuntime source = reflectionSource; // 정산으로 상태를 제거하기 전 피격 아군 보관
        ClearReflectionCitadel(); // 반사 신호를 발행하기 전에 누적값과 상태 소비
        if (reflected <= 0 || IsBattleEnded) // 반사할 양수 피해와 진행 중인 전투 확인
        { // 반사 불가 처리 시작
            return; // 다음 턴의 정상 흐름 유지
        } // 반사 불가 처리 종료
        var recipients = new List<BattleUnitRuntime>(enemyUnits); // 정산 시작 시 배치된 적 목록 보관
        recipients.RemoveAll(enemy => enemy == null || enemy.Team != BattleTeam.Enemy || enemy.IsDead); // 이번 반사를 받을 실제 생존 적 확정
        foreach (var enemy in recipients) // 같은 파티 피해를 받을 생존 적 순회
        { // 적별 고정 반사 처리 시작
            enemy.TakeUnblockedDamage(reflected, BattleDamageType.Physical, source); // 방어력과 방어도를 우회하는 한 번의 고정 물리 체력 피해 적용
        } // 적별 고정 반사 처리 종료
    } // 성채 반사 정산 종료
    private void ClearReflectionCitadel() // 적 턴 한정 성채 효과와 미사용 누적 정리
    { // 성채 상태 정리 시작
        reflectionDamageNumerator = 0; // 다른 라운드와 반사 피해의 중복 집계 차단
        reflectionSource = null; // 지난 적 턴의 최초 피격 아군 참조 해제
        foreach (var ally in allyUnits) // 이번 적 턴이 끝난 아군 순회
        { // 개별 성채 해제 시작
            ally?.RemoveStatusEffect(BattleStatusEffectType.ReflectionCitadel); // 이미 정산했거나 종료된 반사 상태 제거
        } // 개별 성채 해제 종료
    } // 성채 상태 정리 종료
    private BattleUnitRuntime ApplyOpeningEnemyStatus(RelicEffectType effectType) // 무작위 적 상태 적용과 실제 대상 반환
    { // 시작 적 상태 처리 시작
        var relic = openingRelics?.FindEffect(effectType); // 지정 시작 유물의 보유 여부 조회
        if (relic == null) // 유물 미보유 확인
        { // 미발동 처리 시작
            return null; // 보유하지 않은 효과의 대상 없음
        } // 미발동 처리 종료
        var living = new List<BattleUnitRuntime>(); // 약화 대상 후보
        foreach (var enemy in enemyUnits) // 전투 배치의 적 순회
        { // 적별 생존 여부 확인
            if (enemy != null && enemy.Team == BattleTeam.Enemy && !enemy.IsDead) // 실제 생존 적만 후보 포함
            { // 후보 추가 시작
                living.Add(enemy); // 실제 생존 적 등록
            } // 후보 추가 종료
        } // 적별 생존 여부 확인 종료
        if (living.Count > 0) // 대상 존재 확인
        { // 하나의 적에 약화 적용
            var target = living[UnityEngine.Random.Range(0, living.Count)]; // 생존 적 하나 무작위 선택
            var result = target.ApplyStatusEffect(relic.StatusEffectType, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks); // 유물 상태 수치와 기존 면역 규칙 적용
            return result is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked ? target : null; // 상태 부여에 성공한 실제 적 반환
        } // 약화 적용 종료
        return null; // 생존 적이 없는 경우의 대상 없음
    } // 시작 적 상태 처리 종료
    private void ResolveOpeningBottle() // 바닥 없는 병의 실제 전투 개막 효과
    { // 개막 비용 처리 시작
        var bottle = openingRelics?.FindEffect(RelicEffectType.OpeningBottleActionPoints); // 현재 회차에 보유한 병과 행동력 보상 확인
        if (bottle == null || bottle.EffectValue <= 0) // 양수의 보상이 설정된 실제 유물 확인
        { // 유물 효과 생략 시작
            return; // 병을 보유하지 않은 전투의 시작 상태 유지
        } // 유물 효과 생략 종료
        openingBottleActionPoints = bottle.EffectValue; // 적 선제 뒤에도 유지할 첫 아군 턴 보상 예약
        foreach (BattleUnitRuntime ally in allyUnits) // 현재 전투에 참가한 모든 아군 조회
        { // 개별 생존 비용 확인 시작
            if (ally != null && !ally.IsDead) // 정신력 비용을 지불할 실제 생존 아군 검사
            { // 개별 개막 비용 적용 시작
                ally.ChangeMental(-4, BattleMentalChangeReason.RelicEffect); // 기존 정신력 범위와 상태 변화를 반영한 네 점 비용 차감
            } // 개별 개막 비용 적용 종료
        } // 개별 생존 비용 확인 종료
    } // 개막 비용 처리 종료
    private void ApplyOpeningStethoscopeMark() // 검게 물든 청진기가 정하는 첫 표식
    { // 첫 표식 부여 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.OpeningStethoscopeMark); // 정상 회차에 실제로 보유한 유물의 수치
        if (relic == null) // 시작 청진기 미보유 확인
        { // 유물이 없는 경로 시작
            return; // 같은 적 전투에 별도 상태 적용 생략
        } // 유물이 없는 경로 종료
        BattleUnitRuntime target = null; // 가장 높은 현재 체력의 첫 생존 적 보관
        foreach (var enemy in enemyUnits) // 전투의 실제 적 배치 순서
        { // 비교할 적 확인 시작
            if (enemy != null && !enemy.IsDead && (target == null || enemy.CurrentHealth > target.CurrentHealth)) // 살아 있는 적의 현재 체력과 기존 최대값 확인
            { // 더 높은 현재 체력의 적 확인 시작
                target = enemy; // 동률에는 기존 출전 순서의 앞 적 유지
            } // 더 높은 현재 체력의 적 확인 종료
        } // 비교할 적 확인 종료
        target?.ApplyStatusEffect(BattleStatusEffectType.StethoscopeMark, relic.EffectValue, relic.StatusDuration, 1); // 기존 면역을 적용해 한 적에만 상태 부여
    } // 첫 표식 부여 종료

    private void ApplyAbyssLampOpening() // 붉은 심연의 등잔의 두 적 턴 약화와 시작 비용 정산
    { // 등잔의 전투 개막 처리 시작
        var lamp = openingRelics?.FindEffect(RelicEffectType.AbyssLampOpening); // 현재 회차에 실제 보유한 등잔 원본
        if (lamp == null) // 등잔 보유 여부 확인
        { // 개막 효과 생략 시작
            return; // 등잔이 없는 아군과 적의 시작 상태 유지
        } // 개막 효과 생략 종료
        foreach (var enemy in enemyUnits) // 현재 전투의 초기 적 순회
        { // 개별 적 약화 적용 시작
            if (enemy?.Team == BattleTeam.Enemy && !enemy.IsDead) // 실제 전투에 참여한 생존 적 확인
            { // 생존 적 약화 시작
                enemy.ApplyStatusEffect(lamp.StatusEffectType, lamp.EffectValue, lamp.StatusDuration, lamp.StatusMaximumStacks); // 기존 약화 면역과 지속 규칙을 따르는 공격력 감소
            } // 생존 적 약화 종료
        } // 개별 적 약화 적용 종료
        foreach (var ally in allyUnits) // 현재 전투의 시작 아군 순회
        { // 등잔의 개막 정신력 비용 시작
            ally?.SpendRelicMentalCost(5); // 사망을 제외하며 방어로 막을 수 없는 정신력 비용 다섯 점
        } // 등잔의 개막 정신력 비용 종료
    } // 등잔의 전투 개막 처리 종료
    private void ApplyNamelessOathOpening() // 무명자의 검은 서약의 전투 전용 공격력 부여
    { // 서약의 시작 공격력 계산 시작
        int attack = openingRelics?.FindEffect(RelicEffectType.NamelessOathOpening)?.EffectValue ?? 0; // 이번 전투에 보유한 실제 서약 공격력
        if (attack <= 0) // 유효한 서약 공격 증가 보유 확인
        { // 서약 공격 미부여 시작
            return; // 다른 일반 공격력과 상태를 유지한 전투 시작
        } // 서약 공격 미부여 종료
        foreach (var ally in allyUnits) // 이번 전투의 실제 아군 참가자 순회
        { // 개별 생존 아군 공격력 부여 시작
            if (ally?.Team == BattleTeam.Ally && !ally.IsDead) // 시작에 살아 있는 아군 확인
            { // 기존 카드 공격력 가산 시작
                ally.NamelessOathAttackPower = attack; // 일반 공격력 합계에 포함할 전투 한정 수치
            } // 기존 카드 공격력 가산 종료
        } // 개별 생존 아군 공격력 부여 종료
    } // 서약의 시작 공격력 계산 종료
    private void ApplyOpeningCorrosion() // 전투 시작 시 생존 적 전원의 마법 저항 약화 적용
    { // 시작 실금석 처리 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.OpeningCorrosionResistanceDown); // 실제 보유한 네크레이트 실금석 조회
        if (relic == null) // 실금석 보유 여부 확인
        { // 실금석 미보유 처리 시작
            return; // 적 상태 변경 없이 전투 시작
        } // 실금석 미보유 처리 종료
        foreach (var enemy in enemyUnits) // 실제 전투 적 목록 순회
        { // 생존 적별 약화 확인 시작
            if (enemy?.Team == BattleTeam.Enemy && !enemy.IsDead) // 전투에 참가한 생존 적만 확인
            { // 개별 부식 약화 부여 시작
                enemy.ApplyStatusEffect(relic.StatusEffectType, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks); // 평소 상태 면역과 적 턴 만료 규칙의 약화 부여
            } // 개별 부식 약화 부여 종료
        } // 생존 적별 약화 확인 종료
    } // 시작 실금석 처리 종료

    private void ResolveOpeningDie() // 전투 시작 시 흠집 난 주사위의 독립 보상 결정
    { // 시작 주사위 처리 시작
        var die = openingRelics?.FindEffect(RelicEffectType.OpeningRandomAllyBoost); // 실제 이번 탐사에 보유한 주사위 원본
        if (openingDieResolved || die == null) // 전투당 중복 판정과 유물 부재 확인
        { // 시작 보상 없음 처리 시작
            return; // 무작위 소비 없이 원래 전투 시작
        } // 시작 보상 없음 처리 종료
        openingDieResolved = true; // 보상 지급 알림보다 먼저 전투당 한 번 기록
        var target = PickOpeningLivingAlly(); // 기존과 동일한 전투 시작 생존자 추첨
        if (target == null) // 전투 시작 보상을 받을 대상 확인
        { // 생존 후보 없음 처리 시작
            return; // 대상이 없을 때 무작위 추첨 미사용
        } // 생존 후보 없음 처리 종료
        if (UnityEngine.Random.Range(0, 2) == 0) // 같은 확률의 공격력 또는 방어도 선택
        { // 첫 실제 아군 턴의 공격력이 선택된 경우
            openingDieAttackAlly = target; // 선제가 끝나도 시작 때 고른 한 아군에만 보상 예약
        } // 첫 실제 아군 턴의 공격력 예약 종료
        else // 시작 방어도가 선택된 경우
        { // 전투 진입과 동시에 보호 시작
            target.GainBlock(die.EffectValue); // 첫 실제 공격부터 쓸 원래 방어도 획득
        } // 전투 진입과 동시에 보호 종료
    } // 시작 주사위 처리 종료

    private BattleUnitRuntime PickOpeningLivingAlly() // 시작 유물이 공유하는 무작위 생존 아군 선택
    { // 시작 생존 아군 선택 시작
        var livingAllies = new List<BattleUnitRuntime>(); // 균등하게 선택할 생존 아군 후보
        foreach (var ally in allyUnits) // 실제 현재 전투의 아군 명단 확인
        { // 후보별 실제 생존 확인 시작
            if (ally != null && ally.Team == BattleTeam.Ally && !ally.IsDead) // 쓰러진 대상과 잘못된 진영 제외
            { // 생존 아군 후보 추가 시작
                livingAllies.Add(ally); // 현재 전투에 생존한 개별 아군 등록
            } // 생존 아군 후보 추가 종료
        } // 후보별 실제 생존 확인 종료
        return livingAllies.Count == 0 ? null : livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)]; // 생존 후보가 있을 때만 균등한 한 번 선택
    } // 시작 생존 아군 선택 종료

    private void ResolveOpeningPulse() // 전투 시작 시 맥박줄의 실제 정신력 비용과 아군 확정
    { // 시작 맥박줄 처리 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.OpeningPulseCord); // 실제 보유한 침식된 맥박줄 원본 조회
        if (openingPulseResolved || relic == null) // 이미 시작 추첨을 끝냈거나 미보유인 경우 확인
        { // 시작 맥박줄 제외 시작
            return; // 다시 추첨하지 않는 전투 시작
        } // 시작 맥박줄 제외 종료
        openingPulseResolved = true; // 새 상태 알림 전에 전투 시작 한 번 기록
        var target = PickOpeningLivingAlly(); // 주사위와 같은 기준의 독립적인 생존 아군 선택
        openingPulse = (target, relic); // 적 선제 뒤에도 같은 아군에게만 공격력 부여 예약
        target?.ChangeMental(-3, BattleMentalChangeReason.RelicEffect); // 선택한 아군의 표준 정신력 비용 세 점 즉시 적용
    } // 시작 맥박줄 처리 종료

    private void ApplyOpeningPulseAttack() // 첫 실제 아군 턴에 독립 맥박줄 공격력 부여
    { // 첫 실제 아군 강화 준비 시작
        var (target, relic) = openingPulse; // 전투 시작 당시 선택한 아군과 유물 확보
        openingPulse = default; // 공격력 부여 알림 전에 시작 예약 소모
        if (target == null || target.IsDead) // 같은 대상의 실제 생존 확인
        { // 공격력을 줄 대상 없음 처리 시작
            return; // 선제 중 쓰러진 대상의 보상을 다른 아군에게 이전하지 않음
        } // 공격력을 줄 대상 없음 처리 종료
        var result = target.ApplyStatusEffect(relic.StatusEffectType, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks, target); // 기존 공격력 상승과 독립인 두 아군 턴의 강화 적용
        if (result is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked) // 실제 공격력 상태 적용 확인
        { // 첫 실제 아군 턴 포함 처리 시작
            target.GetStatusEffect(relic.StatusEffectType)?.ConsumeDurationDecreaseSkip(); // 첫 실제 아군 턴 종료부터 기간 차감
        } // 첫 실제 아군 턴 포함 처리 종료
    } // 첫 실제 아군 강화 준비 종료

    private void ApplyDustClock() // 세 번째마다 실제 아군 턴의 적 정신력 감소
    { // 어비스 먼지시계 처리 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.PeriodicEnemyMentalLoss); // 현재 보유한 먼지시계 원본 조회
        int interval = relic?.PlayerTurnInterval ?? 0; // 실제 원본의 아군 턴 간격 조회
        if (interval <= 0 || CurrentRound == lastDustClockRound || CurrentRound % interval != 0) // 미보유와 잘못된 간격 및 중복 턴 판정
        { // 정신력 감소 비발동 시작
            return; // 지정한 실제 아군 턴 외에는 정신력 유지
        } // 정신력 감소 비발동 종료
        lastDustClockRound = CurrentRound; // 변경 이벤트 전에 같은 실제 턴의 중복 발동 차단
        foreach (var enemy in enemyUnits) // 효과를 받을 실제 적 목록 순회
        { // 적별 생존 확인 시작
            if (enemy?.Team == BattleTeam.Enemy && !enemy.IsDead) // 생존한 잘못된 진영 없는 적만 확인
            { // 개별 적 정신력 감소 시작
                enemy.ChangeMental(-relic.EffectValue, BattleMentalChangeReason.RelicEffect); // 원본 감소량과 기존 정신력 하한 적용
            } // 개별 적 정신력 감소 종료
        } // 적별 생존 확인 종료
    } // 어비스 먼지시계 처리 종료

    private void ApplyFirstPlayerTurnEnemyStatuses() // 첫 실제 아군 턴의 적 약화 공통 시작
    { // 첫 아군 턴 전용 약화 시작
        openingVulnerableEnemy = ApplyOpeningEnemyStatus(RelicEffectType.OpeningVulnerable); // 기존 검은 왕관의 무작위 생존 적 약화
        var poster = openingRelics?.FindEffect(RelicEffectType.OpeningEnemyFear); // 포스터의 전체 적 공포 부여 조회
        if (poster == null) // 공포를 적용할 유물 보유 여부 확인
        { // 포스터 미보유 처리 시작
            return; // 새 공포 부여 생략
        } // 포스터 미보유 처리 종료
        foreach (var enemy in enemyUnits) // 전투에 등록한 적 전체 순회
        { // 현재 적의 생존 상태 확인 시작
            if (enemy == null || enemy.Team != BattleTeam.Enemy || enemy.IsDead) // 적용 가능한 살아 있는 적 확인
            { // 무효한 적 제외 시작
                continue; // 다음 생존 적 검사
            } // 무효한 적 제외 종료
            var applied = enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, poster.EffectValue, poster.StatusDuration, poster.StatusMaximumStacks); // 기존 상태 면역과 지속 규칙을 따르는 공포 부여
            if (applied == BattleStatusEffectApplyResult.Applied) // 기존 독립 상태와 구분한 포스터의 신규 공포 확인
            { // 실제 신규 공포 보관 시작
                openingFearedEnemies.Add((enemy, enemy.GetStatusEffect(BattleStatusEffectType.Fear))); // 포스터가 새로 만든 상태 인스턴스 저장
            } // 실제 신규 공포 보관 종료
        } // 현재 적의 생존 상태 확인 종료
    } // 첫 아군 턴 전용 약화 종료
    private void ClearFirstPlayerTurnEnemyStatuses() // 첫 아군 턴 한정의 취약과 포스터 공포 회수
    { // 검은 왕관 해제 시작
        var affected = openingVulnerableEnemy; // 취약의 실제 적용 대상 보관
        openingVulnerableEnemy = null; // 상태 알림 전에 취약 대상 참조 해제
        affected?.RemoveStatusEffect(BattleStatusEffectType.Vulnerable); // 첫 아군 턴이 끝난 대상의 취약 제거
        foreach (var entry in openingFearedEnemies) // 포스터가 실제 신규 부여에 성공한 적 순회
        { // 포스터 공포 확인 시작
            var current = entry.Enemy.GetStatusEffect(BattleStatusEffectType.Fear); // 상태 해제와 독립 재부여를 반영한 공포 조회
            if (current != null && ReferenceEquals(current, entry.Fear) && current.RemainingTurns <= 1) // 포스터의 처음 한 턴 상태만 회수 확인
            { // 첫 아군 턴 공포 해제 시작
                entry.Enemy.RemoveStatusEffect(BattleStatusEffectType.Fear); // 포스터의 첫 아군 턴 한정 공포 해제
            } // 첫 아군 턴 공포 해제 종료
        } // 포스터 공포 확인 종료
        openingFearedEnemies.Clear(); // 다음 턴으로 넘기지 않을 포스터 대상 참조 정리
    } // 검은 왕관 해제 종료
    private void ApplyOpeningAllySpeed() // 전투 시작 배지 속도 증가 적용
    { // 배지 발동 시작
        var badge = openingRelics?.FindEffect(RelicEffectType.OpeningAllySpeed); // 보유한 바람개비 배지 조회
        if (badge == null || badge.EffectValue <= 0) // 유효한 배지 효과 확인
        { // 배지 미발동 처리 시작
            return; // 속도 증가 생략
        } // 배지 미발동 처리 종료
        openingAllySpeedActive = true; // 첫 적 턴 종료 시 회수 예약
        foreach (var ally in allyUnits) // 전투 배치 아군 순회
        { // 개별 아군 검사 시작
            if (ally != null && ally.Team == BattleTeam.Ally && !ally.IsDead) // 생존 아군만 적용 확인
            { // 개별 속도 부여 시작
                ally.ApplyStatusEffect(BattleStatusEffectType.ActionSpeedUp, badge.EffectValue, badge.StatusDuration, badge.StatusMaximumStacks); // 배지의 속도 상태 부여
            } // 개별 속도 부여 종료
        } // 개별 아군 검사 종료
    } // 배지 발동 종료
    private void ClearOpeningAllySpeed() // 첫 적 턴의 배지 속도 제거
    { // 속도 효과 회수 시작
        if (!openingAllySpeedActive) // 남아 있는 배지 효과 확인
        { // 회수할 효과 없음 시작
            return; // 속도 제거 생략
        } // 회수할 효과 없음 종료
        openingAllySpeedActive = false; // 발동한 배지 상태 초기화
        foreach (var ally in allyUnits) // 배지 효과가 적용된 진영 순회
        { // 개별 상태 제거 시작
            ally?.RemoveStatusEffect(BattleStatusEffectType.ActionSpeedUp); // 첫 적 턴용 속도 상태 해제
        } // 개별 상태 제거 종료
    } // 속도 효과 회수 종료
    internal void QueueSpecimenOpeningRewards(int draw, int actionPoints) // 탐사에서 얻은 표본병 효과를 시작 손패에 예약
    { // 시작 표본병 예약 시작
        runtimeDeck.QueuePlayerTurnDraw(draw); // 첫 실제 아군 드로우의 추가량 예약
        openingSpecimenActionPoints = actionPoints; // 선제 적 턴에는 사용하지 않을 행동력 보관
    } // 시작 표본병 예약 종료
    internal void QueueNextPlayerTurnActionPointLoss(int amount) // 해결된 거인의 방패의 다음 아군 턴 차감 예약
    { // 다음 아군 턴 행동력 예약 시작
        if (!disposed && IsPlayerTurn && !IsBattleEnded && amount > 0) // 성공한 아군 행동과 양수 행동력 차감 확인
        { // 개별 카드의 차감 예약 시작
            nextPlayerTurnActionPointLoss = (int)Math.Min(int.MaxValue, (long)nextPlayerTurnActionPointLoss + amount); // 여러 사용의 행동력 차감을 정수 범위에서 누적
        } // 개별 카드의 차감 예약 종료
    } // 다음 아군 턴 행동력 예약 종료
    private void DrawPlayerHand(int count) // 아군 턴의 기본과 유물 드로우 적용
    { // 턴 손패 처리 시작
        int actionPointLoss = nextPlayerTurnActionPointLoss; // 현재 턴에 적용할 이전 카드의 차감량
        nextPlayerTurnActionPointLoss = 0; // 행동력 변경 알림 전에 예약 초기화
        sharedActionPoints.ReduceTurnCapacity(actionPointLoss); // 기본 회복 뒤 현재와 최대 행동력의 일시 차감
        int specimenActionPoints = openingSpecimenActionPoints; // 실제 아군 턴에 지급할 행동력 확보
        openingSpecimenActionPoints = 0; // 발동 알림 전에 재지급 방지
        int bottleActionPoints = openingBottleActionPoints; // 실제 첫 아군 턴에서 사용할 별도 유물 보상 확보
        openingBottleActionPoints = 0; // 행동력 변경 알림 이전 개막 보상 예약 초기화
        sharedActionPoints.AddTurnCapacity(specimenActionPoints); // 끝나는 턴에 회수할 현재 및 상한 행동력 지급
        sharedActionPoints.AddTurnCapacity(bottleActionPoints); // 첫 정상 드로우 전 병의 현재 및 상한 행동력 추가
        var pouch = openingRelics?.FindEffect(RelicEffectType.LargeDiscardDraw); // 보유한 잔해 주머니 조회
        int additionalDraw = 0; // 기본 드로우와 분리할 주머니 보너스 초기화
        if (pouch != null && runtimeDeck.DiscardPileCount >= 10) // 재섞기 전에 버린 더미 확인
        { // 추가 드로우 준비 시작
            additionalDraw = pouch.EffectValue; // 이번 턴의 유물 추가 드로우 한 장 기록
        } // 추가 드로우 준비 종료
        if (openingRelics?.FindEffect(RelicEffectType.RecoverRandomDiscard) != null) // 회수용 자석 보유 여부 확인
        { // 턴 드로우 전 자석 처리 시작
            runtimeDeck.TryRecoverRandomDiscardOnce(); // 가능한 경우 기존 버린 카드 한 장 회수
        } // 턴 드로우 전 자석 처리 종료
        LastDrawnCardCount = runtimeDeck.DrawForPlayerTurn(count, additionalDraw, out int basicDrawn); // 손패 상한과 예약을 유지하면서 실제 기본 도착 집계
        ApplyBaseDrawBlock(basicDrawn); // 먼저 지급된 실제 기본 드로우의 은빛 톱니 조건 적용
        if (CurrentRound != 1 || openingRelics == null) // 첫 아군 손패 처리 여부 확인
        { // 일반 아군 턴 처리 시작
            return; // 첫 손패 전용 효과 생략
        } // 일반 아군 턴 처리 종료
        bool hasPentacle = false; // 첫 손패의 펜타클 유무
        bool hasSword = false; // 첫 손패의 검 유무
        foreach (var card in runtimeDeck.Hand) // 추가 드로우 이전 첫 손패 순회
        { // 카드 속성 확인 시작
            hasPentacle |= card.CardType == CardType.Pentacle; // 첫 펜타클 손패 기록
            hasSword |= card.CardType == CardType.Sword; // 첫 검 손패 기록
        } // 카드 속성 확인 종료
        var envelope = openingRelics.FindEffect(RelicEffectType.OpeningSwordCapacity); // 찢긴 우편봉투 조회
        if (hasSword && envelope != null) // 첫 검 손패 조건 확인
        { // 추가 행동력 적용 시작
            sharedActionPoints.AddTurnCapacity(envelope.EffectValue); // 이번 턴 현재 및 최대 행동력 증가
        } // 추가 행동력 적용 종료
        var letter = openingRelics.FindEffect(RelicEffectType.OpeningPentacleDraw); // 구겨진 편지 조회
        if (hasPentacle && letter != null && runtimeDeck.HandCount < runtimeDeck.MaxHandSize) // 첫 펜타클 손패와 빈자리 확인
        { // 추가 드로우 시작
            LastDrawnCardCount += runtimeDeck.DrawCards(letter.EffectValue); // 실제 지급한 카드 수 합산
        } // 추가 드로우 종료
        var shard = openingRelics.FindEffect(RelicEffectType.OpeningCardDiscount); // 모래시계 파편 조회
        if (shard != null && openingRelics.TryRemove(shard.RelicId)) // 첫 아군 턴에 한 번만 실제 소모
        { // 비용 감소 시작
            runtimeDeck.SetRelicApDiscount(shard.EffectValue); // 이번 턴의 카드 비용 감소
        } // 비용 감소 종료
        var crown = openingRelics.FindEffect(RelicEffectType.OpeningRandomCardDiscount); // 실제 첫 손패에 적용할 퇴색한 왕관 조회
        runtimeDeck.DiscountRandomOpeningHandCard(crown?.EffectValue ?? 0); // 첫 손패 추첨과 개별 고정 비용 할인
        if (openingRelics.FindEffect(RelicEffectType.RetainRandomOpeningCard) != null) // 금속 핸드벨의 실제 보유 확인
        { // 첫 손패 유지 준비 시작
            runtimeDeck.RetainRandomOpeningHandCard(); // 모든 시작 드로우를 마친 실제 손패 한 장 예약
        } // 첫 손패 유지 준비 종료
    } // 턴 손패 처리 종료
    private void ApplyBaseDrawBlock(int basicDrawn) // 실제 기본 두 장 이상을 드로우한 턴의 방어도 지급
    { // 은빛 톱니 처리 시작
        var cog = basicDrawn >= 2 ? openingRelics?.FindEffect(RelicEffectType.BaseTurnDrawBlock) : null; // 예약과 초과를 제외한 기본 도착 두 장과 보유 유물 확인
        if (cog == null) // 유효한 실제 드로우와 은빛 톱니 확인
        { // 발동 생략 시작
            return; // 현재 방어도 보존
        } // 발동 생략 종료
        foreach (var ally in allyUnits) // 원래 전투 배치 순서대로 아군 조회
        { // 첫 생존 아군 조회 시작
            if (ally != null && ally.Team == BattleTeam.Ally && !ally.IsDead) // 실제 살아 있는 지급 대상 확인
            { // 톱니 방어도 지급 시작
                ally.GainBlock(cog.EffectValue); // 첫 살아 있는 한 명에게 방어도 지급
                return; // 뒤쪽 아군으로의 중복 지급 차단
            } // 톱니 방어도 지급 종료
        } // 첫 생존 아군 조회 종료
    } // 은빛 톱니 처리 종료
    private void HandleExcludedCard(CardInstance card, bool fromVolatileHand) // 실제 제외 및 손패 휘발 유물 처리
    { // 제외 유물 처리 시작
        if (!IsPlayerTurn || IsBattleEnded) // 진행 중인 아군 턴 확인
        { // 발동 불가 처리 시작
            return; // 다른 단계의 제외 효과 생략
        } // 발동 불가 처리 종료
        var wax = openingRelics?.FindEffect(RelicEffectType.ExcludedCardBlock); // 틈새 봉인왁스 조회
        if (wax != null && lastExcludedBlockRound != CurrentRound && !card.OwnerUnit.IsDead) // 생존 카드 소유자와 발동 기회 확인
        { // 방어도 지급 시작
            lastExcludedBlockRound = CurrentRound; // 같은 턴의 재발동 차단
            card.OwnerUnit.GainBlock(wax.EffectValue); // 제외된 카드 소유자에게 방어도 지급
        } // 방어도 지급 종료
        var pins = openingRelics?.FindEffect(RelicEffectType.VolatileCardDraw); // 안전핀 묶음 조회
        if (fromVolatileHand && pins != null && lastVolatileDrawRound != CurrentRound) // 실제 손패 휘발과 발동 기회 확인
        { // 추가 손패 지급 시작
            lastVolatileDrawRound = CurrentRound; // 같은 턴에 한 차례만 발동
            runtimeDeck.DrawCards(pins.EffectValue); // 손패 정리 이후 새 카드 지급
        } // 추가 손패 지급 종료
    } // 제외 유물 처리 종료

    public bool TryEscape() // 플레이어 도주 요청
    { // 도주 요청 시작
        if (!CanEscape) // 도주 가능 여부 확인
        { // 도주 불가 처리 시작
            return false; // 도주 실패 반환
        } // 도주 불가 처리 종료
        SetBattleResult(BattleResult.Escape); // 도주 결과 확정
        return true; // 도주 성공 반환
    } // 도주 요청 종료
    internal bool HasBuffedTarget() // 단죄가 제거할 강화 소유 적 확인
    { // 강화 대상 탐색 시작
        foreach (var enemy in enemyUnits) // 현재 적 전투 배치 순회
        { // 적 상태 확인 시작
            if (enemy != null && !enemy.IsDead && enemy.FirstRemovableBuff() != null) // 살아 있고 일반 강화가 있는 적 확인
            { // 유효 대상 시작
                return true; // 단죄 베기 사용 가능
            } // 유효 대상 종료
        } // 적 상태 확인 종료
        return false; // 제거 가능한 적 강화 없음
    } // 강화 대상 탐색 종료

    internal bool HasBleedingTarget() // 출혈을 가진 생존 적 확인
    { // 추적 후보 검색 시작
        foreach (var enemy in enemyUnits) // 현재 전투 배치 순회
        { // 생존 적 확인 시작
            if (enemy != null && !enemy.IsDead && enemy.GetStatusEffect(BattleStatusEffectType.Bleed) != null) // 실제 출혈이 남은 대상 확인
            { // 출혈 발견 시작
                return true; // 피의 추적 사용 가능
            } // 출혈 발견 종료
        } // 생존 적 확인 종료
        return false; // 출혈 대상 없음
    } // 추적 후보 검색 종료

    internal bool HasFinisherTarget(int healthDivisor = 5) // 처형 가능한 생존 적 확인
    { // 처형 후보 확인 시작
        foreach (var enemy in enemyUnits) // 현재 배치한 적 순회
        { // 적 체력 검사 시작
            if (SwordCardCombat.IsFinisherTarget(enemy, healthDivisor)) // 생존과 체력 비율 검사
            { // 유효 대상 확인 시작
                return true; // 처형 가능 대상 발견
            } // 유효 대상 확인 종료
        } // 적 체력 검사 종료
        return false; // 처형 가능 대상 없음
    } // 처형 후보 확인 종료
    private void ApplyDebuffedEnemyTurnDamage() // 실제 적 턴 시작의 새장 일괄 피해
    { // 생존 적의 약화 조건 처리 시작
        int damage = openingRelics?.FindEffect(RelicEffectType.DebuffedEnemyTurnDamage)?.EffectValue ?? 0; // 보유한 균열 먹는 새장의 일반 피해량
        if (damage <= 0) // 실제 양수 피해 효과 보유 확인
        { // 유물 미보유 처리 시작
            return; // 적 상태 조사 생략
        } // 유물 미보유 처리 종료
        foreach (var enemy in new List<BattleUnitRuntime>(enemyUnits)) // 전투 중 사망과 소환 정리에 안전한 실제 적 목록 복사
        { // 개별 적 상태 확인 시작
            if (IsBattleEnded || HasPendingBossPhaseTransition) // 새장 피해로 마지막 적 또는 보스 단계 종료 확인
            { // 이전 단계의 추가 피해 중단 시작
                break; // 남은 상대의 시작 피해 생략
            } // 이전 단계의 추가 피해 중단 종료
            if (enemy != null && !enemy.IsDead && ContainsUnit(enemyUnits, enemy) && enemy.HasAtLeastThreeRemovableDebuffs()) // 세 종류의 활성 제거 가능 약화를 가진 생존 적 확인
            { // 일반 피해 처리 시작
                enemy.TakeDamage(damage, BattleDamageType.None); // 직접 카드 효과가 없는 기존 방어도 적용 피해
            } // 일반 피해 처리 종료
        } // 개별 적 상태 확인 종료
    } // 생존 적의 약화 조건 처리 종료
    private void NotifyPhaseStarted() // 진영 시작 효과를 반영한 턴 알림
    { // 시작 효과 적용 시작
        if (CurrentPhase == BattleTurnPhase.EnemyTurn) // 적 행동과 지속 피해 발생 전 확인
        { // 체력 집계 시작
            foreach (var ally in allyUnits) // 아군별 체력 손실 기록 준비
            { // 개별 기록 시작
                ally?.StartEnemyDamageWindow(); // 직전 적 턴 기준점 갱신
            } // 개별 기록 종료
            ApplyDebuffedEnemyTurnDamage(); // 적 행동과 상태 이동 전 새장의 약화 조건 정산
            if (IsBattleEnded) // 마지막 적의 시작 피해 사망으로 승리가 확정됐는지 확인
            { // 끝난 전투의 적 턴 시작 중단 시작
                return; // 적 상태와 행동 이벤트의 후속 실행 방지
            } // 끝난 전투의 적 턴 시작 중단 종료
        } // 체력 집계 종료
        else if (CurrentPhase == BattleTurnPhase.PlayerTurn) // 다음 아군 턴 도착 확인
        { // 지연 방어 감소 적용 시작
            var openingAlly = openingDieAttackAlly; // 실제 첫 아군 턴에 도착한 공격력 대상 조회
            openingDieAttackAlly = null; // 상태 알림 전에 이번 시작 보상의 재발동 방지
            if (openingAlly != null && !openingAlly.IsDead) // 적 선제 중에도 선택한 아군이 살아 있는지 확인
            { // 첫 아군 턴의 독립 일시 공격력 적용 시작
                openingAlly.ApplyCurrentAllyTurnAttackPower(1, openingAlly); // 기존 한 턴 공격력 경로로 한 점 지급
            } // 첫 아군 턴의 독립 일시 공격력 적용 종료
            ApplyOpeningPulseAttack(); // 첫 실제 아군 턴의 독립 맥박줄 공격력 부여
            ApplyDustClock(); // 지정 주기의 실제 아군 턴에 생존 적 정신력 감소
            foreach (var enemy in enemyUnits) // 적별 돌진 예약 확인
            { // 개별 약화 시작
                enemy?.ApplyQueuedCharge(); // 피격한 적의 방어 감소 발동
            } // 개별 약화 종료
        } // 지연 방어 감소 적용 종료
        PhaseStarted?.Invoke(CurrentPhase, CurrentRound); // 기존 진영 시작 효과 전달
    } // 시작 효과 적용 종료
    private void NotifyPhaseEnded() // 진영 종료와 돌진 지속 시간 정리
    { // 종료 효과 적용 시작
        bool endCharge = CurrentPhase == BattleTurnPhase.PlayerTurn; // 아군 턴 종료 여부 보관
        PhaseEnded?.Invoke(CurrentPhase, CurrentRound); // 기존 진영 종료 효과 전달
        if (endCharge) // 돌진의 한 차례 아군 공격 기회 종료 확인
        { // 돌진 효과 해제 시작
            ClearFirstPlayerTurnEnemyStatuses(); // 첫 실제 아군 턴이 끝난 시작 약화 제거
            foreach (var ally in allyUnits) // 결투를 시작한 아군 확인
            { // 결투 기간 처리 시작
                ally?.ActiveDuel?.AdvancePlayerTurn(); // 양쪽 표식의 남은 턴을 함께 감소
            } // 결투 기간 처리 종료
            foreach (var enemy in enemyUnits) // 적별 활성 약화 확인
            { // 개별 약화 해제 시작
                enemy?.RemoveStatusEffect(BattleStatusEffectType.PublicMark); // 해당 아군 턴이 끝나면 공명 표식 해제
                var stethoscope = enemy?.GetStatusEffect(BattleStatusEffectType.StethoscopeMark); // 이번 아군 턴까지 적용된 독립 청진기 표식 조회
                if (stethoscope != null) // 해당 적에게 청진기 표식이 적용됐는지 확인
                { // 실제 청진기 기간 차감 시작
                    stethoscope.AdvanceDuration(); // 이번 실제 아군 턴 종료 한 번만 기간에서 차감
                    if (stethoscope.IsExpired) // 시트의 아군 턴 두 번을 모두 마쳤는지 확인
                    { // 청진기 표식 제거 시작
                        enemy.RemoveStatusEffect(BattleStatusEffectType.StethoscopeMark); // 실제 상태와 표식 피해 보너스 대상 해제
                    } // 청진기 표식 제거 종료
                    else // 두 번째 아군 턴까지 남는 표식 확인
                    { // 남은 표시 갱신 시작
                        enemy.NotifyStatusEffectsChanged(); // 새 잔여 턴을 실제 적 화면에 표시
                    } // 남은 표시 갱신 종료
                } // 실제 청진기 기간 차감 종료
                enemy?.RemoveStatusEffect(BattleStatusEffectType.ChargeDefenseDown); // 이번 턴의 임시 방어 감소 제거
            } // 개별 약화 해제 종료
        } // 돌진 효과 해제 종료
    } // 종료 효과 적용 종료
    internal void RecordCardPayment(CardInstance card, int paid) // 실제 지불에 따른 스프링 예약과 소비
    { // 다음 카드 할인 처리 시작
        if (springReady && !card.SourceData.SpendsAllActionPoints && card.ApCostBeforeNextCardDiscount > 0) // 예약을 쓸 수 있는 유료 고정 카드 확인
        { // 예약 할인 소비 시작
            springReady = false; // 같은 턴의 남은 카드 할인 종료
            runtimeDeck.SetNextCardRelicApDiscount(0); // 다른 손패의 실제 비용 복원
        } // 예약 할인 소비 종료
        var spring = paid > 0 && lastSpringRound != CurrentRound ? openingRelics?.FindEffect(RelicEffectType.DiscountNextPaidCard) : null; // 현재 턴의 첫 행동력 소모 확인
        if (spring != null) // 실제로 보유한 붉은 스프링 확인
        { // 다음 카드 할인 준비 시작
            lastSpringRound = CurrentRound; // 같은 턴 재충전 방지
            springReady = true; // 다음 사용에 적용할 할인 예약
            runtimeDeck.SetNextCardRelicApDiscount(spring.EffectValue); // 현재 손패에 다음 카드 표시 반영
        } // 다음 카드 할인 준비 종료
    } // 다음 카드 할인 처리 종료

    internal int ResolveDefenseCardRelics(CardInstance card, bool usedFirstDefenseDiscount) // 실제 카드 정산 뒤 방어 유물 소모와 지급 처리
    { // 실제 사용 방어 유물 처리 시작
        if (IsBattleEnded) // 카드가 전투를 종료했는지 확인
        { // 전투 종료 처리 시작
            return 0; // 종료된 전투에 새 방어도 예약 방지
        } // 전투 종료 처리 종료
        if (usedFirstDefenseDiscount) // 비용에 방패끈이 실제 반영된 카드 확인
        { // 첫 방어 할인 소모 시작
            runtimeDeck.SetFirstDefensiveRelicApDiscount(0); // 이번 전투의 남은 방어 카드 정상 비용 복원
        } // 첫 방어 할인 소모 종료
        if (card.CardType == CardType.Sword) // 실제 사용한 카드가 검인지 확인
        { // 다음 방패의 방어도 예약 시작
            pendingSwordThenShieldBlock = openingRelics?.FindEffect(RelicEffectType.SwordThenShieldBlock)?.EffectValue ?? 0; // 검을 여러 장 써도 보유 유물 수치 한 번만 예약
        } // 다음 방패의 방어도 예약 종료
        if (card.CardType != CardType.Shield || pendingSwordThenShieldBlock == 0) // 예약이 남은 실제 방패 카드 확인
        { // 지급 대상 아님 처리 시작
            return 0; // 기존 예약 유지 또는 추가 방어 없음 반환
        } // 지급 대상 아님 처리 종료
        int block = pendingSwordThenShieldBlock; // 다음 방패 보너스 방어도 확보
        pendingSwordThenShieldBlock = 0; // 방어도 알림 전에 다음 방패 예약 소모
        return card.OwnerUnit.GainBlock(block); // 방패 카드의 실제 소유자 한 명에게 추가 방어도 지급
    } // 실제 사용 방어 유물 처리 종료
    internal void BeginCardResolution(bool deferVictory = false) // 카드가 손패에 남아 있는 정산 범위 시작
    { // 카드 정산 표시 시작
        resolvingCard = true; // 처치 드로우를 카드 사용 뒤로 미루기
        deferVictoryUntilCardResolved = deferVictory; // 한 카드의 두 효과 뒤 승리 정산 여부 보관
    } // 카드 정산 표시 종료

    internal void EndCardResolution() // 사용 카드가 빠진 뒤 예약 처치 드로우 정산
    { // 카드 정산 완료 처리 시작
        resolvingCard = false; // 독립 처치에 즉시 드로우 허용
        bool finishVictory = deferVictoryUntilCardResolved && !IsBattleEnded && !HasPendingBossPhaseTransition && !HasLivingUnit(enemyUnits); // 복합 효과를 적용한 뒤 실제 적 전멸 확인
        deferVictoryUntilCardResolved = false; // 전투 종료 알림 전에 예약 해제
        if (finishVictory && !HasPendingSeedRevival()) // 복합 카드 정산과 예약 부활이 모두 끝났는지 확인
        { // 두 효과 이후 승리 정산 시작
            SetBattleResult(HasLivingUnit(allyUnits) ? BattleResult.Victory : BattleResult.Defeat); // 두 효과 뒤 생존 명단을 포함한 결과 저장
        } // 두 효과 이후 승리 정산 종료
        DrawDefeatReward(); // 카드가 떠난 손패 공간을 기준으로 지급
    } // 카드 정산 완료 처리 종료

    private void HandleUnitDefeated(BattleUnitRuntime source, BattleUnitRuntime target) // 아군이 적을 실제로 처치한 경우 확인
    { // 처치 드로우 예약 시작
        RecordPermanentAttackDefeat(target); // 턴별 카드 드로우와 별개로 실제 정예·보스 처치 보상 먼저 정산
        if (!IsPlayerTurn || HasPendingBossPhaseTransition || source?.Team != BattleTeam.Ally || target.Team != BattleTeam.Enemy || lastFlagDrawRound == CurrentRound) // 아군 턴의 유효 처치와 발동 제한 확인
        { // 발동 제외 시작
            return; // 적의 처치나 같은 턴의 재발동 제외
        } // 발동 제외 종료
        pendingDefeatDraw = openingRelics?.FindEffect(RelicEffectType.DefeatDraw)?.EffectValue ?? 0; // 회전 깃발로 받을 카드 수 예약
        if (!resolvingCard) // 사용할 카드가 손패를 비울 필요가 없는 시점 확인
        { // 독립 처치 지급 시작
            DrawDefeatReward(); // 카드 정산 외 처치의 즉시 드로우 처리
        } // 독립 처치 지급 종료
    } // 처치 드로우 예약 종료

    private void RecordPermanentAttackDefeat(BattleUnitRuntime target) // 실제 초기 정예 또는 보스 처치의 한 번 정산
    { // 심장 유물 처치 정산 시작
        if (!IsActiveExplorationBattle || target?.Team != BattleTeam.Enemy || !target.IsDead || pendingPermanentAttackDefeats?.Remove(target) != true) // 유효한 탐사에서 실제 패배한 새 등급 개체 확인
        { // 정산할 처치가 없는 경우 처리 시작
            return; // 일반, 소환, 미패배 또는 이미 처리한 개체의 효과 종료
        } // 정산할 처치가 없는 경우 처리 종료
        var relic = openingRelics?.FindEffect(RelicEffectType.PermanentAttackPerEliteBoss); // 현재 처치 순간에 보유한 작은 침식핵 심장 조회
        int gained = explorationRun.GrantPermanentAttackPower(relic?.EffectValue ?? 0); // 기존 회차의 모든 출전 아군에 유지할 실제 보너스 추가
        if (gained > 0) // 실제로 새 영구 수치를 받은 처치 확인
        { // 유물 발동 표시 시작
            UnityEngine.Debug.Log($"[Relic] {relic.DisplayName} 발동 / {target.DisplayName} 처치 / 회차 공격력 +{gained}"); // 실제 발동과 회차 보상 범위 기록
        } // 유물 발동 표시 종료
    } // 심장 유물 처치 정산 종료

    private void DrawDefeatReward() // 현재 가능한 회전 깃발 보상 지급
    { // 처치 드로우 처리 시작
        int count = pendingDefeatDraw; // 대기 중인 드로우 수 확보
        pendingDefeatDraw = 0; // 드로우 알림 전에 대기 상태 초기화
        if (count <= 0 || IsBattleEnded || HasPendingBossPhaseTransition || runtimeDeck.HandCount >= runtimeDeck.MaxHandSize || !runtimeDeck.HasDrawableCards) // 전투 진행과 손패 공간 및 실제 드로우 대상 확인
        { // 드로우 불가 처리 시작
            return; // 발동 횟수를 사용하지 않고 보상 처리 종료
        } // 드로우 불가 처리 종료
        lastFlagDrawRound = CurrentRound; // 같은 아군 턴의 추가 처치 드로우 차단
        runtimeDeck.DrawCards(Math.Min(count, runtimeDeck.MaxHandSize - runtimeDeck.HandCount)); // 열린 손패 공간까지 처치 드로우 지급
    } // 처치 드로우 처리 종료

    private static void ClearRoundBlock(IReadOnlyList<BattleUnitRuntime> units) // 진영의 남은 방어도 정리
    { // 방어도 정리 시작
        foreach (BattleUnitRuntime unit in units) // 진영 유닛 순회
        { // 개별 정리 시작
            unit?.ClearBlock(); // 유효한 유닛 방어도 제거
        } // 개별 정리 종료
    } // 방어도 정리 종료
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
        TryReviveWithAbyssGrail(runtimeUnit); // 실제 전투불능 처리 뒤 전멸 판정 전 첫 부활 정산
        bool alliesDefeated = !HasLivingUnit(allyUnits); // 부활까지 반영한 아군 전멸 여부 계산
        bool enemiesDefeated = !HasLivingUnit(enemyUnits); // 적 전멸 여부 계산
        if (!alliesDefeated && !enemiesDefeated) // 전투 지속 여부 확인
        { // 전투 지속 처리 시작
            return; // 종료 판정 중단
        } // 전투 지속 처리 종료
        if (HasPendingSeedRevival()) // 실제로 쓰러져 부활 정산을 기다리는 아군 확인
        { // 승패 판정 보류 시작
            return; // 부활 이후 실제 생존자 상태로 승패 재판정
        } // 승패 판정 보류 종료
        if (alliesDefeated) // 아군 전멸 우선 확인
        { // 패배 처리 시작
            SetBattleResult(BattleResult.Defeat); // 패배 결과 확정
            return; // 승패 판정 종료
        } // 패배 처리 종료
        if (enemiesDefeated && !deferVictoryUntilCardResolved) // 복합 카드를 제외한 일반 적 전멸 확인
        { // 승리 처리 시작
            SetBattleResult(BattleResult.Victory); // 승리 결과 확정
        } // 승리 처리 종료
    } // 유닛 사망 처리 종료
    private bool HasPendingSeedRevival() // 아직 실제 사망과 부활을 정산 중인 아군 검사
    { // 부활 대기 상태 검사 시작
        foreach (var ally in allyUnits) // 현재 전투의 전체 아군 명단 확인
        { // 개별 부활 여부 확인 시작
            if (ally?.HasPendingSeedRevival == true) // 먼저 받은 씨앗의 사망 정산 확인
            { // 실제 부활을 기다리는 경우 시작
                return true; // 전멸이나 보상 확정 대기 반환
            } // 실제 부활을 기다리는 경우 종료
        } // 개별 부활 여부 확인 종료
        return false; // 모든 실제 사망의 씨앗 정산 종료 반환
    } // 부활 대기 상태 검사 종료
    internal BattleResultData CaptureBattleResult(IReadOnlyList<string> defeatedEnemyIds) // 전투 종료 회복 반영 결과 생성
    { // 결과 생성 시작
        ApplyCorrosionSutureHealing(); // 저장 이전 침식 회복 적용
        SettleNamelessOath(); // 종료 회복 뒤 현재 손실 체력 기준의 서약 비용 저장
        return new BattleResultData(Result, BattleType, CurrentRound, allyUnits, defeatedEnemyIds); // 회복 반영 전투 결과
    } // 결과 생성 끝

    private void ApplyCorrosionSutureHealing() // 균열 봉합실 전투 종료 회복
    { // 유물 회복 처리 시작
        if (!IsBattleEnded || corrosionSutureSettled) // 미종료 전투 또는 이미 처리한 종료
        { // 처리 제외 시작
            return; // 회복 처리 생략
        } // 처리 제외 끝

        corrosionSutureSettled = true; // 재진입과 저장 재시도 중복 방지
        int healing = BattleSupplyRelics.Find(RelicEffectType.SutureCorrosionHealing)?.EffectValue ?? 0; // 보유 유물의 기본 회복량
        if (healing <= 0) // 유물 미보유 또는 회복량 없음
        { // 적용 제외 시작
            return; // 회복 대상 탐색 생략
        } // 적용 제외 끝

        foreach (BattleUnitRuntime ally in allyUnits) // 현재 전투 아군 순회
        { // 아군 확인 시작
            if (ally != null && !ally.IsDead && ally.HasLostHealthToCorrosion) // 실제 침식 체력 손실을 겪은 생존 아군
            { // 회복 적용 시작
                ally.RestoreHealth(healing); // 일반 회복 규칙으로 체력 복원
            } // 회복 적용 끝
        } // 아군 확인 끝
    } // 유물 회복 처리 끝

    private void SettleNamelessOath() // 승리나 도주 결과에 적용할 서약의 생존 체력 비용
    { // 전투 종료 서약 처리 시작
        if (namelessOathSettled || Result is not (BattleResult.Victory or BattleResult.Escape) || openingRelics?.FindEffect(RelicEffectType.NamelessOathOpening) == null) // 정상 종료와 실제 서약 보유 확인
        { // 결과 저장에 적용하지 않는 경우 시작
            return; // 패배와 종료 전 및 재저장 비용 제외
        } // 결과 저장에 적용하지 않는 경우 종료
        namelessOathSettled = true; // 후속 체력 이벤트와 저장 재시도보다 앞선 중복 처리 방지
        foreach (var ally in allyUnits) // 결과에 저장할 실제 아군 순회
        { // 서약의 생존 체력 비용 계산 시작
            if (ally?.Team == BattleTeam.Ally && !ally.IsDead) // 승리하거나 도주한 생존 아군 확인
            { // 현재 손실 체력의 정수 비용 지불 시작
                int missing = Math.Max(0, ally.MaxHealth - ally.CurrentHealth); // 종료 회복 이후 최대 체력 기준 손실량
                int cost = Math.Min(ally.CurrentHealth - 1, missing / 5); // 이십 퍼센트 내림과 체력 한 점 생존 하한
                if (cost > 0) // 유효한 체력 감소가 남았는지 확인
                { // 일반 피격 없는 종료 비용 처리 시작
                    ally.TrySpendHealth(cost); // 방어도와 피해 신호를 쓰지 않는 기존 체력 비용 지불
                } // 일반 피격 없는 종료 비용 처리 종료
            } // 현재 손실 체력의 정수 비용 지불 종료
        } // 서약의 생존 체력 비용 계산 종료
    } // 전투 종료 서약 처리 종료
    private void SetBattleResult(BattleResult battleResult) // 전투 결과 확정
    { // 결과 확정 시작
        Result = battleResult; // 최종 전투 결과 저장
        CurrentPhase = battleResult == BattleResult.Victory ? BattleTurnPhase.Victory : battleResult == BattleResult.Defeat ? BattleTurnPhase.Defeat : BattleTurnPhase.Escaped; // 결과별 종료 단계 설정
        ClearOpeningAllySpeed(); // 첫 적 턴을 마치지 않은 전투의 배지 상태 회수
        ClearFirstPlayerTurnEnemyStatuses(); // 전투 종료 시의 첫 아군 턴 전용 약화 회수
        ClearReflectionCitadel(); // 승리와 패배 및 도주 이후의 반사 상태 초기화
        foreach (var ally in allyUnits) // 전투가 종료된 뒤 남은 씨앗 정리 대상 확인
        { // 미사용 씨앗 제거 시작
            ally?.RemoveStatusEffect(BattleStatusEffectType.ResurrectionSeed); // 결과 확정 이후 부활을 예약하지 않는 전투 상태 정리
        } // 미사용 씨앗 제거 종료
        nextPlayerTurnActionPointLoss = 0; // 전투 종료 후 미사용 행동력 차감 제거
        pendingSwordThenShieldBlock = 0; // 같은 전투에서 사용하지 않은 메달 조각 방어도 정리
        runtimeDeck.ClearBattleRelicCards(); // 결과 화면 전 전투 복사본과 미사용 예약 제거
        sharedActionPoints.ClearTurnCapacity(); // 첫 턴에 끝난 전투의 임시 상한 회수
        StateChanged?.Invoke(); // 전투 종료 알림
        BattleCompleted?.Invoke(Result); // 후속 상태 정리 이후 전투 종료 완료 알림
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
    internal int ApplyCrownNailOutgoingDamage(BattleUnitRuntime target, int damage) // 직접 명중으로 확정된 보스 피해 비율 정산
    { // 왕관못의 직접 보스 피해 시작
        if (damage <= 0 || disposed || IsBattleEnded || target == null || target != authoredBoss) // 유효한 양수 피해와 원본 보스 확인
        { // 추가 피해 제외 시작
            return damage; // 일반 적과 보스 옆 적의 원래 피해 유지
        } // 추가 피해 제외 종료
        int percent = openingRelics?.FindEffect(RelicEffectType.CrownNailBossDamage)?.EffectValue ?? 0; // 실제 보유한 왕관못의 보스 피해 증가율
        return percent > 0 ? BattleUnitRuntime.ApplyPercentage(damage, percent) : damage; // 기존 퍼센트 계산의 반올림과 피해 한도 적용
    } // 왕관못의 직접 보스 피해 종료

    private void HandleAllyHealthLoss(BattleUnitRuntime ally, BattleDamageResult damage) // 실제 양수 아군 체력 손실 뒤 행동력 한 번 판정
    { // 공허 순환장치 확인 시작
        if (disposed || IsBattleEnded || CurrentPhase is not (BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn) || // 진행 중 실제 전투의 피해만 확인
            ally.Team != BattleTeam.Ally || damage.AppliedDamage <= 0 || sharedActionPoints.CurrentActionPoints >= sharedActionPoints.MaxActionPoints) // 실제 체력 손실과 회복 가능한 현재 행동력 확인
        { // 발동하지 않는 체력 손실 처리 시작
            return; // 차단된 피해와 전투 밖 변경 및 행동력 상한의 난수 미소모
        } // 발동하지 않는 체력 손실 처리 종료
        var relic = openingRelics?.FindEffect(RelicEffectType.GainOnHealthLoss); // 보유한 공허 순환장치의 확률과 행동력 회복 수치
        if (relic?.EffectValue > 0 && relic.TriggerChancePercent > 0 && (relic.TriggerChancePercent >= 100 || UnityEngine.Random.Range(0, 100) < relic.TriggerChancePercent)) // 살아 있을 때 시작한 실제 피해 한 건의 확률 판정
        { // 현재 행동력 회복 시작
            sharedActionPoints.Gain(relic.EffectValue); // 최대 행동력 변화 없는 기존 상한형 회복 사용
        } // 현재 행동력 회복 종료
    } // 공허 순환장치 확인 종료

    private void TryReviveWithAbyssGrail(BattleUnitRuntime fallen) // 첫 실제 아군 전투불능에 전투당 한 번 부활
    { // 어비스 성배 조각의 부활 확인 시작
        if (revivedWithAbyssGrail || fallen.Team != BattleTeam.Ally || !fallen.IsDead || CurrentPhase is not (BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn)) // 진행 중 실제 아군 사망과 전투당 한 번 조건 확인
        { // 성배 부활 제외 시작
            return; // 기존 사망 정산과 다른 전투 부활 유지
        } // 성배 부활 제외 종료
        int healthPercent = openingRelics?.FindEffect(RelicEffectType.AbyssGrailAllyRevive)?.EffectValue ?? 0; // 실제 보유한 조각의 최대 체력 부활 비율
        if (healthPercent <= 0) // 전투 중 실제 성배 조각을 보유했는지 확인
        { // 부활 유물 미보유 처리 시작
            return; // 일반 전투불능과 부활 씨앗 정산 유지
        } // 부활 유물 미보유 처리 종료
        revivedWithAbyssGrail = true; // 부활 및 정신력 변경 알림 전에 이번 전투 사용 확정
        fallen.Revive(healthPercent); // 실제 부활의 체력 올림과 전투 재참여 사용
        foreach (var ally in allyUnits) // 부활한 참가자를 포함한 현재 아군 목록 순회
        { // 개별 아군의 정신력 대가 처리 시작
            ally?.SpendRelicMentalCost(10); // 생존 아군에게 직접 정신 피해 보호와 별개인 고정 대가 적용
        } // 개별 아군의 정신력 대가 처리 종료
    } // 어비스 성배 조각의 부활 확인 종료
    private void RegisterUnitEvents(IReadOnlyList<BattleUnitRuntime> units) // 유닛 사망 이벤트 등록
    { // 이벤트 등록 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 이벤트 등록 시작
            RegisterUnitEvent(runtimeUnit); // 개별 유닛 사망 이벤트 등록
        } // 유닛 이벤트 등록 종료
    } // 이벤트 등록 종료
    private void HandleAllyStealthEnded(BattleUnitRuntime ally) // 살아 있는 아군의 실제 은신 종료 정산
    { // 팔찌 피해 예약 확인 시작
        if (disposed || CurrentPhase is not (BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn) || ally.IsDead) // 진행 중 전투의 생존 아군만 확인
        { // 페이즈 밖 처리 제외 시작
            return; // 사망이나 전투 결과 정리의 효과 제외
        } // 페이즈 밖 처리 제외 종료
        var bracelet = openingRelics?.FindEffect(RelicEffectType.StealthBreakDamage); // 현재 회차에 보유한 어둠에 잠긴 팔찌
        if (bracelet != null) // 실제 획득한 팔찌 원본 확인
        { // 다음 유효 명중의 피해 예약 시작
            ally.ApplyStatusEffect(BattleStatusEffectType.StealthBreakDamageUp, bracelet.EffectValue, bracelet.StatusDuration, 1, ally); // 활성 예약 하나의 피해 수치만 갱신
        } // 다음 유효 명중의 피해 예약 종료
    } // 팔찌 피해 예약 확인 종료
    private bool RegisterUnitEvent(BattleUnitRuntime runtimeUnit) // 개별 사망 이벤트 등록
    { // 개별 이벤트 등록 시작
        if (runtimeUnit == null || registeredUnits.Contains(runtimeUnit)) // 등록 유닛 중복 확인
        { // 중복 등록 처리 시작
            return false; // 이벤트 등록 실패 반환
        } // 중복 등록 처리 종료
        if (runtimeUnit.Team == BattleTeam.Ally) // 은신 해제는 생존 아군에게만 구독
        { // 아군 은신 종료 연결 시작
            runtimeUnit.StealthEnded += HandleAllyStealthEnded; // 실제 해제와 만료 경로의 공통 이벤트 연결
        } // 아군 은신 종료 연결 종료
        runtimeUnit.SeedRevivalSettled += HandleUnitDied; // 씨앗 부활 뒤 남은 생존 명단으로 승패 재판정 등록
        runtimeUnit.Defeated += HandleUnitDefeated; // 발생자와 대상을 포함한 처치 등록
        if (runtimeUnit.Team == BattleTeam.Ally) // 실제 아군의 손실과 부활 정산 연결 확인
        { // 아군 전투 유물 연결 시작
            runtimeUnit.AllyDefeatReady += HandleUnitDied; // 모든 사망 알림 뒤 아군 부활과 승패 정산
            runtimeUnit.DamageTaken += HandleAllyHealthLoss; // 체력에 도달한 단일 실제 피해 신호 연결
        } // 아군 전투 유물 연결 종료
        else // 기존 적 처치 처리 유지
        { // 적 사망 판정 연결 시작
            runtimeUnit.Died += HandleUnitDied; // 기존 적 사망과 승리 판정 연결
        } // 적 사망 판정 연결 종료
        if (runtimeUnit.Team == BattleTeam.Ally) // 공용 유물의 보호를 받을 아군 확인
        { // 공용 보호 연결 시작
            runtimeUnit.PreventIncomingMentalLoss = TryNullifyFirstMentalDamage; // 현재 전투의 공용 보호 연결
            runtimeUnit.GetHostileBlindResistance = GetOpeningBlindResistance; // 현재 전투의 첫 적 턴 실명 저항 조회 연결
        } // 공용 보호 연결 종료
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
        runtimeUnit.AllyDefeatReady -= HandleUnitDied; // 전투를 떠난 아군의 부활 정산 구독 해제
        runtimeUnit.DamageTaken -= HandleAllyHealthLoss; // 전투를 떠난 아군의 체력 손실 구독 해제
        runtimeUnit.StealthEnded -= HandleAllyStealthEnded; // 전투를 떠난 아군의 은신 종료 구독 해제
        runtimeUnit.SeedRevivalSettled -= HandleUnitDied; // 전투 밖으로 나간 유닛의 부활 판정 해제
        runtimeUnit.Defeated -= HandleUnitDefeated; // 회전 깃발의 처치 구독 해제
        if (runtimeUnit.PreventIncomingMentalLoss == (Func<BattleMentalChangeReason, bool>)TryNullifyFirstMentalDamage) // 현재 전투가 연결한 보호인지 확인
        { // 공용 보호 연결 해제 시작
            runtimeUnit.PreventIncomingMentalLoss = null; // 전투를 떠난 아군의 공용 보호 해제
        } // 공용 보호 연결 해제 종료
        if (runtimeUnit.GetHostileBlindResistance == (Func<int>)GetOpeningBlindResistance) // 이 전투에서 연결한 개막 효과인지 확인
        { // 동일 전투의 개막 효과 해제 시작
            runtimeUnit.GetHostileBlindResistance = null; // 전투를 떠난 아군의 첫 적 턴 저항 조회 해제
            runtimeUnit.NamelessOathAttackPower = 0; // 실제 전투 씬에서만 쓴 서약의 공격력 회수
        } // 동일 전투의 개막 효과 해제 종료
        return true; // 이벤트 해제 성공 반환
    } // 개별 사망 이벤트 해제 종료
    private int GetOpeningBlindResistance() // 먼지 먹은 손수건의 첫 실제 적 턴 실명 저항
    { // 적 턴 저항 가능 여부 검사 시작
        return !disposed && !IsBattleEnded && !firstEnemyTurnFinished && CurrentPhase == BattleTurnPhase.EnemyTurn // 적 선제 포함 실제 첫 적의 턴이 계속되는지 확인
            ? openingRelics?.FindEffect(RelicEffectType.FirstTurnBlindResistance)?.EffectValue ?? 0 // 현재 보유한 첫 턴 실명 저항 수치
            : 0; // 첫 적 턴 외에는 원래 실명 확률 유지
    } // 적 턴 저항 가능 여부 검사 종료
    private bool TryNullifyFirstMentalDamage(BattleMentalChangeReason reason) // 검은 심연판의 실제 첫 정신 피해 판정
    { // 공용 보호 판정 시작
        if (disposed || IsBattleEnded || firstMentalDamageNullified || reason is not (BattleMentalChangeReason.ReceiveDamage or BattleMentalChangeReason.HeavyDamage or BattleMentalChangeReason.DisruptReceived)) // 전투 유효성, 남은 보호 및 실제 피격·교란 원인 확인
        { // 공용 보호 제외 처리 시작
            return false; // 비용과 간접 손실 및 종료된 전투의 기존 변경 유지
        } // 공용 보호 제외 처리 종료
        var plate = openingRelics?.FindEffect(RelicEffectType.NullifyFirstMentalDamage); // 현재 회차에 실제 보유한 검은 심연판 조회
        if (plate == null || plate.EffectValue <= 0) // 보호 가능한 보유 유물 확인
        { // 유물 보호 미적용 처리 시작
            return false; // 보호 없는 아군의 일반 정신 피해 적용
        } // 유물 보호 미적용 처리 종료
        firstMentalDamageNullified = true; // 발동 알림 전에 이번 전투의 공용 보호 한 번 사용
        UnityEngine.Debug.Log($"[Relic] {plate.DisplayName} 발동 / 첫 정신 피해 무효"); // 실제 정신 피해 방어 기록
        return true; // 현재 진행하는 정신 피해 한 건의 무효 요청
    } // 공용 보호 판정 종료
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
        runtimeDeck.CardExcluded -= HandleExcludedCard; // 카드 제외 연결 해제
        disposed = true; // 연결 해제 상태 저장
        foreach (BattleUnitRuntime ally in allyUnits) // 이번 탐사 전투의 아군 공격력 연결 확인
        { // 개별 탐사 전투 연결 해제 시작
            if (ally?.ExplorationAttackPowerBattle == this) // 현재 전투가 설정한 아군만 확인
            { // 종료한 전투의 연결 해제 시작
                ally.ExplorationAttackPowerBattle = null; // 다른 전투나 탐사 밖에서 현재 공격력 조회 차단
            } // 종료한 전투의 연결 해제 종료
        } // 개별 탐사 전투 연결 해제 종료
        ClearOpeningAllySpeed(); // 전투 관리자 해제 시 배지 상태 회수
        ClearFirstPlayerTurnEnemyStatuses(); // 전투 관리자 해제 시 시작 약화 회수
        ClearReflectionCitadel(); // 전투 관리자 해제 시 성채 집계와 임시 상태 정리
        for (int unitIndex = registeredUnits.Count - 1; unitIndex >= 0; unitIndex--) // 등록 유닛 역순 순회
        { // 이벤트 일괄 해제 시작
            UnregisterUnitEvent(registeredUnits[unitIndex]); // 개별 사망 이벤트 해제
        } // 이벤트 일괄 해제 종료
    } // 연결 해제 종료
} // 클래스 종료
