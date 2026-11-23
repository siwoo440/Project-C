using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using System.Linq; // 개막 후보 목록과 첫 생존 아군 탐색
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
    private int completedLordOrderAllyTurns; // 명령서의 방어 대가 전에 완료해야 할 실제 아군 턴 수
    private bool lordOrderDefenseResolved; // 두 아군 턴 뒤 방어 약화의 전투당 한 번 처리 기록
    private int lastBanquetHeartRound; // 심장 잔뿌리의 마지막 실제 아군 턴
    private int lastChandelierRound; // 샹들리에가 마지막으로 처리한 실제 아군 턴
    private int lastBanquetOilRound; // 기름등이 마지막으로 확률을 확인한 아군 턴
    private int lastBlackCutleryDrawRound; // 검은 식기함이 마지막으로 드로우를 지급한 아군 턴
    private int pendingBlackCutleryDrawRound; // 사용 카드가 손패를 떠난 뒤 지급할 출혈 보상 턴
    private int lastDustClockRound; // 어비스 먼지시계의 마지막 발동 아군 턴
    private int ballroomEvadesThisEnemyTurn; // 이번 적 턴의 실제 아군 회피 수
    private bool dryWaterskinSettled; // 실제 종료 저장 전 물주머니 회복 처리 여부
    private int firstActualAllyTurnRound; // 실제 첫 아군 차례의 라운드 번호
    private bool firstAllyAttackDeclared; // 실제 첫 아군 턴의 최초 공격 선언 여부
    private BattleUnitRuntime firstAttackBonusOwner; // 확정한 최초 공격의 실제 카드 소유자
    private int pendingFirstAttackBonus; // 현재 첫 공격 카드의 첫 명중에 남은 가산량
    private bool sandBoneUsed; // 이번 전투에서 출혈 적에게 실제 직접 피해 가산 사용 여부
    private bool firstHostileDebuffReflectAttempted; // 이번 전투의 첫 적 약화 반사 확률 사용 여부
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
    private bool openingTemporaryCardChecked; // 첫 아군 손패의 단추 생성 기회 처리 여부
    internal IReadOnlyList<CardInstance> OpeningBookmarkChoices { get; private set; } // 현재 선택에 보여 준 실제 드로우 더미의 개막 후보
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
    public ExplorationRegion Region { get; } // 시작할 때 확정한 실제 조우 지역
    public BattleResult Result { get; private set; } = BattleResult.None; // 현재 전투 결과 조회
    public bool IsPlayerTurn => CurrentPhase == BattleTurnPhase.PlayerTurn; // 플레이어 턴 여부
    private bool playerCardDecisionPending; // 실제 손패 선택의 중복 사용 잠금
    public bool CanAcceptPlayerInput => !disposed && IsPlayerTurn && !IsBattleEnded && OpeningBookmarkChoices == null && !playerCardDecisionPending; // 개막 선택과 카드 결정을 모두 마친 일반 턴 입력
    public bool IsBattleEnded => Result != BattleResult.None; // 전투 종료 여부
    private bool IsActiveExplorationBattle => !disposed && !IsBattleEnded && CurrentRound > 0 && explorationRun != null && activeRunEncounter != null && ReferenceEquals(activeRunEncounter, explorationRun.ActiveEncounterIdentity); // 종료 전 실제 탐사 조우와 전투의 유효한 연결
    internal int PermanentAttackPower => IsActiveExplorationBattle ? explorationRun.PermanentAttackPower : 0; // 실제 탐사 전투에 출전한 아군의 영구 공격력
    public bool HasPendingBossPhaseTransition { get; private set; } // 보스 페이즈 전환 예약 여부
    public bool CanEscape => CanAcceptPlayerInput && (BattleType == BattleType.Normal || BattleType == BattleType.Elite); // 개막 선택을 마친 일반과 정예 전투의 도주 가능 여부
    private bool CanSelectDuringPlayerTurn => CanAcceptPlayerInput && !HasPendingBossPhaseTransition; // 개막 선택과 보스 전환을 마친 아군 대상 선택 가능 여부
    internal bool HasSelectableEnemy => CanSelectDuringPlayerTurn && HasLivingUnit(enemyUnits); // 현재 적 대상으로 고를 생존 참가자 확인
    internal bool HasSelectableEnemyWithStatus(BattleStatusEffectType status) // 지정 상태가 남은 생존 적의 선택 가능 여부
    { // 지정 활성 상태의 적 후보 확인 시작
        if (!CanSelectDuringPlayerTurn) // 현재 적을 지정할 수 있는 아군 차례인지 확인
        { // 전투 선택 불가 처리 시작
            return false; // 종료나 페이즈 전환에는 후보 없음
        } // 전투 선택 불가 처리 종료
        foreach (var enemy in enemyUnits) // 실제 전투의 현재 적 명단 순회
        { // 생존 적의 활성 상태 확인 시작
            if (enemy?.Team == BattleTeam.Enemy && !enemy.IsDead && enemy.GetStatusEffect(status) is { IsExpired: false }) // 살아 있고 만료되지 않은 지정 상태 확인
            { // 유효 후보 존재 시작
                return true; // 원래 상태 조건의 카드 선택 허용
            } // 유효 후보 존재 종료
        } // 생존 적의 활성 상태 확인 종료
        return false; // 사용할 수 있는 활성 상태 대상 없음
    } // 지정 활성 상태의 적 후보 확인 종료
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
    public BattleTurnRuntime(BattleDeckRuntime battleDeck, BattleActionPointRuntime actionPoints, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies, int drawCountPerTurn, BattleType battleType, ExplorationSessionManager exploration = null, ExplorationRegion region = ExplorationRegion.Winterfall) // 실제 탐사 조우와 원본 지역을 선택 연결하는 턴 관리자
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
        Region = region; // 실제 원본 전투에서 확정한 지역 저장
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
        ApplyLordsWaxOpening(); // 선제 및 첫 적 의도 전에 일반전과 정예전의 생존 적 약화
        ApplyOpeningAllySpeed(); // 전투 시작 시 생존 아군 속도 증가
        ResolveOpeningBottle(); // 첫 행동 준비 전에 바닥 없는 병의 전체 정신력 비용 결제
        openingEffects?.Invoke(); // 전투 시작 성공 확인 뒤 첫 행동 전에 탐사 효과 소비
        ResolveOpeningDie(); // 실제 선제 여부가 적용되기 전에 주사위 보상 한 번 선정
        ApplyOpeningCorrosion(); // 선제 공격 전에 실제 생존 적에게 실금석의 마법 저항 약화 적용
        ApplyOpeningStethoscopeMark(); // 기존 첫 턴 준비 중 최대 현재 체력 적에게 청진기 표식 적용
        ApplyOpeningServantContract(); // 적 선제보다 먼저 첫 생존 아군의 두 턴 계약 표식 적용
        ApplyOpeningPortraitFear(); // 적 행동 예고보다 먼저 한 번 확률로 생존 적 전체의 공포 부여
        ApplyOpeningEnemyStatus(RelicEffectType.DesertKingBraceletOpeningWeakness); // 첫 행동 전에 팔찌로 생존 적 한 명의 공격력 약화
        ApplyOpeningHiddenNameWard(); // 실제 첫 적 행동 전에 각 생존 아군의 개별 치명 피해 보호 부여
        ResolveOpeningPulse(); // 현재 실제 생존 아군 한 명과 시작 정신력 비용 확정
        ApplyAbyssLampOpening(); // 첫 진영 시작 전에 등잔의 적 약화와 아군 정신력 비용 정산
        ApplyFortressOpeningBlock(); // 시작 효과 정리 완료 뒤 적 선제와 첫 예고 전에 성채 방어도 지급
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
        DrawOpeningCaravanReward(); // 첫 손패 완성 뒤 실제 상점 구매 예약을 남은 자리에 지급
        CurrentPhase = BattleTurnPhase.PlayerTurn; // 플레이어 턴 설정
        NotifyPhaseStarted(); // 첫 플레이어 턴 시작 알림
        PrepareOpeningBookmark(); // 첫 턴 효과와 실제 완성된 손패 이후 개막 후보 확보
        StateChanged?.Invoke(); // 턴 상태 변경 알림
        return true; // 전투 시작 성공 반환
    } // 전투 시작 처리 종료
    internal bool HasHandPaymentOptions(int count) // 사용할 원본 한 장 이외에 현재 비용만큼 실물 카드 보유 여부
    { // 손패 수량 검사 시작
        return count > 0 && runtimeDeck.HandCount > count; // 양수 실제 비용에 더해 사용할 원본까지 있는지 확인
    } // 손패 수량 검사 종료
    internal bool TryBeginPlayerCardDecision() // 아군의 카드 선택 중 다른 일반 입력을 잠그는 요청
    { // 카드 입력 잠금 시작
        if (!CanAcceptPlayerInput || HasPendingBossPhaseTransition) // 실제 아군 턴과 현재 다른 선택 및 전환 확인
        { // 잠글 수 없는 카드 입력 처리 시작
            return false; // 이미 시작된 다른 입력 상태 유지
        } // 잠글 수 없는 카드 입력 처리 종료
        playerCardDecisionPending = true; // 실제 비용 후보가 대기하는 일반 입력 잠금 기록
        StateChanged?.Invoke(); // 현재 턴의 사용할 수 있는 카드와 버튼 상태 알림
        return true; // 요청한 카드 선택의 잠금 성공
    } // 카드 입력 잠금 종료
    internal void EndPlayerCardDecision() // 방금 끝난 카드 선택의 일반 입력 잠금 해제
    { // 카드 선택 잠금 복구 시작
        if (playerCardDecisionPending) // 이번 선택의 실제 잠금 여부 확인
        { // 진행 중인 선택 복구 시작
            playerCardDecisionPending = false; // 이벤트 전에 일반 전투 입력 복원
            StateChanged?.Invoke(); // 손패와 턴 종료 버튼의 실제 상태 갱신
        } // 진행 중인 선택 복구 종료
    } // 카드 선택 잠금 복구 종료
    public bool EndPlayerTurn() // 플레이어 턴 종료
    { // 플레이어 턴 종료 처리 시작
        if (!CanAcceptPlayerInput) // 실제 개막 선택을 마친 플레이어 턴 확인
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
    internal int SetEfficientUseDiscount(int amount) // 현재 전투 아군 전체의 독립 펜타클 턴 할인 변경
    { // 현재 아군과 이후 생성 카드의 비용 갱신 시작
        amount = Math.Max(0, amount); // 실제 기본 할인과 초기화에 허용할 값
        bool changed = false; // 현재 비용 변경 및 손패 갱신 필요 여부
        foreach (var ally in allyUnits) // 이 전투의 아군 소유자마다 같은 턴 할인 확인
        { // 현재 소유자 갱신 시작
            if (ally != null && ally.EfficientUseApDiscount != amount) // 같은 효율적 사용이 중첩 없이 유지되는 조건
            { // 새 할인 적용 또는 초기화 시작
                ally.EfficientUseApDiscount = amount; // 이 턴의 이전 및 새 카드에 공통인 기본 할인
                changed = true; // 실제 값 차이가 있는지 기록
            } // 새 할인 적용 또는 초기화 종료
        } // 현재 소유자 갱신 종료
        if (changed) // 실제 비용이 바뀐 때만 손패 표시 확인
        { // 손패의 현재 비용 갱신 시작
            runtimeDeck.NotifyCardCostsChanged(); // 새 손패 비용과 사용 조건을 기존 화면에 알림
        } // 손패의 현재 비용 갱신 종료
        return changed ? amount : 0; // 실제 변경된 현재 턴의 새 할인량
    } // 현재 아군과 이후 생성 카드의 비용 갱신 종료

    private void CleanupPlayerTurn() // 일반 플레이어 턴 종료 공통 정리
    { // 플레이어 턴 정리 시작
        SetEfficientUseDiscount(0); // 일반 종료와 강제 턴 전환에서 펜타클 기본 할인 복원
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
        ballroomEvadesThisEnemyTurn = 0; // 적 턴 종료 뒤 다음 실제 적 턴의 집계 초기화
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
            DrawOpeningCaravanReward(); // 적 선제 뒤에도 실제 첫 아군 손패에서만 적립 보상 지급 확인
            PrepareOpeningBookmark(); // 적 선제가 끝난 실제 첫 아군의 완성 손패 이후 선택 준비
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
    private void ApplyOpeningPortraitFear() // 초상화 속 붉은 눈의 전투당 한 번 전체 공포
    { // 초상화의 개막 공포 판정 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.PortraitEyeOpeningFear); // 실제 회차에 보유한 초상화 유물 원본
        int chance = relic?.TriggerChancePercent ?? 0; // 대상마다 나누지 않는 원본 전투당 확률
        if (!IsBattleEnded && chance > 0 && HasLivingUnit(enemyUnits) && (chance >= 100 || UnityEngine.Random.Range(0, 100) < chance)) // 살아 있는 적이 있을 때 원본 한 번의 실제 확률 확인
        { // 성공 시의 적 진영 공포 적용 시작
            ApplyRelicStatusToLivingEnemies(relic); // 면역과 적 진영의 원래 한 턴 수명을 따르는 생존 적별 공포 시도
        } // 성공 시의 적 진영 공포 적용 종료
    } // 초상화의 개막 공포 판정 종료
    private void ApplyOpeningHiddenNameWard() // 고해실의 숨은 이름으로 생존 아군별 치명 피해 보호 부여
    { // 아군 개별 생존 보호의 개막 적용 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.HiddenNamePartyDefeatWard); // 실제 보유한 고해실 유물 원본 조회
        if (relic == null) // 개막 보호의 보유 여부 확인
        { // 미보유 효과 확인 시작
            return; // 별도 생존 상태 부여 생략
        } // 미보유 효과 확인 종료
        foreach (var ally in allyUnits) // 시작 전투에 참가한 실제 아군 순회
        { // 아직 살아 있는 각 아군과 기존 보호 확인 시작
            if (ally is { Team: BattleTeam.Ally, IsDead: false } && ally.GetStatusEffect(relic.StatusEffectType) == null) // 각 생존 아군의 중복 없는 치명 보호 조건
            { // 아직 보호가 없는 실제 대상의 원본 상태 부여 시작
                ally.ApplyStatusEffect(relic.StatusEffectType, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks, ally); // 첫 실제 치명 피해까지 유지되는 아군별 한 번의 기존 보호
            } // 아직 보호가 없는 실제 대상의 원본 상태 부여 종료
        } // 아직 살아 있는 각 아군과 기존 보호 확인 종료
    } // 아군 개별 생존 보호의 개막 적용 종료
    private void ApplyOpeningServantContract() // 낡은 종복 계약서의 첫 생존 아군 전투 시작 효과
    { // 초기 계약 확인 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.ServantContractOpeningMark); // 현재 회차에 실제 보유한 개막 계약서
        if (relic != null) // 보유한 계약서의 문서 조건 확인
        { // 배치상 첫 생존 아군 선택 시작
            var ally = allyUnits.FirstOrDefault(unit => unit is { Team: BattleTeam.Ally, IsDead: false }); // 전투 명단 맨 앞의 실제 생존 아군
            ally?.ApplyStatusEffect(BattleStatusEffectType.ServantContractMark, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks, ally); // 실제 두 아군 턴에 유지되는 독립 공격력 표식
        } // 배치상 첫 생존 아군 선택 종료
    } // 초기 계약 확인 종료
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
            ally?.SpendBattleMentalCost(5); // 사망을 제외하며 방어로 막을 수 없는 정신력 비용 다섯 점
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

    private void ApplyLordsWaxOpening() // 영주의 봉랍의 일반전 및 정예전 개막 약화
    { // 전투 등급 확인 시작
        if (BattleType is BattleType.Normal or BattleType.Elite) // 우두머리를 제외한 원래 조우 등급 확인
        { // 생존 적 전체 약화 시작
            ApplyRelicStatusToLivingEnemies(openingRelics?.FindEffect(RelicEffectType.LordsWaxOpeningEnemyWeakness)); // 시작 적의 원래 공격력 감소 부여
        } // 생존 적 전체 약화 종료
    } // 전투 등급 확인 종료

    private void ApplyBanquetOilPulse() // 실제 아군 턴마다 확인하는 기름등의 주기 중독
    { // 지정 턴과 적 생존 조건 확인 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.BanquetOilPoisonPulse); // 현재 전투에 보유한 기름등 원본
        int interval = relic?.PlayerTurnInterval ?? 0; // 실제 원본의 아군 턴 주기
        if (interval <= 0 || CurrentRound == lastBanquetOilRound || CurrentRound % interval != 0 || !HasLivingUnit(enemyUnits)) // 잘못된 주기와 같은 턴 중복 및 대상 없는 전투 제외
        { // 기름등 발동 보류 시작
            return; // 영향을 받을 실제 지정 턴까지 기존 난수 보존
        } // 기름등 발동 보류 종료
        lastBanquetOilRound = CurrentRound; // 난수 및 상태 알림 이전에 한 번의 턴 기회 기록
        int chance = relic.TriggerChancePercent; // 전체 적에 함께 적용할 원본 확률
        if (chance > 0 && (chance >= 100 || UnityEngine.Random.Range(0, 100) < chance)) // 각 적이 아닌 이번 아군 턴에 단 한 번 판정
        { // 중독 일괄 시도 시작
            ApplyRelicStatusToLivingEnemies(relic); // 생존 적 각각에 기존 상태 면역을 거친 중독 시도
        } // 중독 일괄 시도 종료
    } // 지정 턴과 적 생존 조건 확인 종료

    private void ApplyRelicStatusToLivingEnemies(RelicData relic) // 개막과 아군 턴 유물의 실제 생존 적 전체 상태 적용
    { // 원본 효과와 현재 적 명단 확인 시작
        if (relic == null) // 현재 실제로 보유하는 유물 원본 확인
        { // 미보유 효과 생략 시작
            return; // 적 상태와 실제 난수 유지
        } // 미보유 효과 생략 종료
        foreach (var enemy in new List<BattleUnitRuntime>(enemyUnits)) // 효과 신호로 명단이 변해도 유지되는 실제 배치 순회
        { // 개별 실제 적 후보 검사 시작
            if (!IsBattleEnded && !HasPendingBossPhaseTransition && enemy?.Team == BattleTeam.Enemy && !enemy.IsDead && ContainsUnit(enemyUnits, enemy)) // 현재 단계와 실제 생존 적 참가 여부 확인
            { // 기존 적 상태 적용 시작
                enemy.ApplyStatusEffect(relic.StatusEffectType, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks); // 원본 수치와 기존 상태 면역 및 자연 적 턴 적용
            } // 기존 적 상태 적용 종료
        } // 개별 실제 적 후보 검사 종료
    } // 원본 효과와 현재 적 명단 확인 종료

    private int TryDrawBlackCutlery(bool preparingPlayerHand = false) // 현재 출혈 누적 여부와 실제 아군 턴 확인
    { // 식기함의 손패 보상 검사 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.BlackCutleryBleedStackDraw); // 보유 검은 식기함 원본 조회
        if (relic == null || disposed || IsBattleEnded || HasPendingBossPhaseTransition || CurrentRound <= 0 || lastBlackCutleryDrawRound == CurrentRound || (!IsPlayerTurn && !preparingPlayerHand)) // 실제 아군 턴과 아직 지급하지 않은 보상 확인
        { // 출혈 집계 제외 시작
            return 0; // 종료 또는 중복 상황의 손패 보존
        } // 출혈 집계 제외 종료
        long stackCount = 0; // 서로 다른 생존 적에 걸린 출혈 중첩의 합계
        foreach (var enemy in enemyUnits) // 현재 전투에 참가한 적 명단 순회
        { // 개별 적의 출혈 수량 확인 시작
            if (enemy?.Team != BattleTeam.Enemy || enemy.IsDead || enemy.GetStatusEffect(BattleStatusEffectType.Bleed) is not { IsExpired: false } bleed) // 활성 출혈 상태를 가진 생존 적 여부 확인
            { // 무효 출혈 집계 생략 시작
                continue; // 다음 현재 생존 후보 조회
            } // 무효 출혈 집계 생략 종료
            stackCount += bleed.StackCount; // 피해량과 독립인 실제 출혈 중첩만 합산
            if (stackCount >= 3) // 생존 적 전체에 활성 출혈 세 중첩 도달 확인
            { // 보상 조건이 성립한 시점 정산 시작
                if (resolvingCard) // 현재 사용 원본이 아직 손패를 떠나지 않았는지 확인
                { // 사용 카드 정산 뒤 보상 예약 시작
                    pendingBlackCutleryDrawRound = CurrentRound; // 같은 실제 아군 턴의 한 번 출혈 보상 예약
                    return 0; // 카드가 떠나 생긴 빈자리에서 보상을 평가
                } // 사용 카드 정산 뒤 보상 예약 종료
                return DrawBlackCutleryCards(relic.EffectValue); // 현재 손패 공간에 실제 보상 카드 지급
            } // 보상 조건이 성립한 시점 정산 종료
        } // 개별 적의 출혈 수량 확인 종료
        return 0; // 실제 중첩 조건 미달의 손패 보존
    } // 식기함의 손패 보상 검사 종료

    private int DrawBlackCutleryCards(int count) // 카드 사용 전후의 출혈 보상 드로우
    { // 실제 손패 도착에 따른 발동 기록 시작
        if (count <= 0 || disposed || IsBattleEnded || HasPendingBossPhaseTransition || lastBlackCutleryDrawRound == CurrentRound) // 유효 보상과 종료 및 중복 발동 확인
        { // 출혈 보상 제외 시작
            return 0; // 같은 턴의 중복 또는 종료 뒤 드로우 방지
        } // 출혈 보상 제외 종료
        int previousRound = lastBlackCutleryDrawRound; // 드로우 실패 시 보존할 기존 발동 기록
        lastBlackCutleryDrawRound = CurrentRound; // 손패 이벤트의 같은 턴 재진입 차단
        int drawn = runtimeDeck.DrawRelicCardsIntoHand(count); // 실제 빈자리에서만 시작하는 기존 유물 드로우
        if (drawn == 0) // 손패 상한 등으로 실제 지급되지 않았는지 확인
        { // 유일한 턴 기회 복구 시작
            lastBlackCutleryDrawRound = previousRound; // 실제 드로우가 없는 턴을 미지급으로 유지
        } // 유일한 턴 기회 복구 종료
        return drawn; // 실제 손패에 도착한 보상 카드 수
    } // 실제 손패 도착에 따른 발동 기록 종료

    private void ApplyFirstPlayerTurnEnemyStatuses() // 첫 실제 아군 턴의 적 약화 공통 시작
    { // 첫 아군 턴 전용 약화 시작
        firstActualAllyTurnRound = CurrentRound; // 일반전과 적 선제 모두에서 실제 첫 아군 차례 기록
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
    private void ApplyFortressOpeningBlock() // 성채 조우에서만 첫 행동 전에 생존 아군 방어도 지급
    { // 현재 실제 성채 지역과 유물 확인 시작
        int block = Region == ExplorationRegion.Fortress ? openingRelics?.FindEffect(RelicEffectType.ManorWickFortressOpeningBlock)?.EffectValue ?? 0 : 0; // 성채 전용 보유 유물의 원본 방어도
        if (block <= 0) // 성채 유물 또는 적용 가능한 방어도 확인
        { // 다른 지역이나 미보유 전투의 적용 제외 시작
            return; // 이전 원래 개막 방어도 유지
        } // 다른 지역이나 미보유 전투의 적용 제외 종료
        foreach (var ally in allyUnits) // 실제 조우에서 시작하는 아군 명단 순회
        { // 개별 생존 아군의 개막 방어도 지급 시작
            if (ally?.Team == BattleTeam.Ally && !ally.IsDead) // 실제 출전한 생존 아군 확인
            { // 기존 방어도 갱신 시작
                ally.GainBlock(block); // 원래 방어 처리와 알림을 따르는 시작 방어도 증가
            } // 기존 방어도 갱신 종료
        } // 개별 생존 아군의 개막 방어도 지급 종료
    } // 현재 실제 성채 지역과 유물 확인 종료

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
        if (CurrentRound == 1 && BattleType == BattleType.Boss) // 선택 보스를 포함한 우두머리 전투의 첫 실제 아군 드로우 확인
        { // 우두머리 전투 개막 행동력 지급 시작
            int bossActionPoints = openingRelics?.FindEffect(RelicEffectType.ThroneScrewBossOpeningActionPoints)?.EffectValue ?? 0; // 실제 보유한 왕좌 나사의 원본 행동력 수치
            sharedActionPoints.AddTurnCapacity(bossActionPoints); // 바닥 없는 병과 독립으로 더하고 현재 턴 종료에 회수할 상한과 현재 행동력
        } // 우두머리 전투 개막 행동력 지급 종료
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
            LastDrawnCardCount += TryDrawBlackCutlery(preparingPlayerHand: true); // 기본 지급 뒤 이미 충족한 출혈의 첫 기회 확인
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
        ApplyOpeningTemporaryThrust(); // 선제 유무와 관계없이 기존 첫 손패 드로우 뒤 임시 찌르기 한 장 확인
        LastDrawnCardCount += TryDrawBlackCutlery(preparingPlayerHand: true); // 첫 손패의 원래 생성 뒤 남은 자리에서 식기함 보상 확인
    } // 턴 손패 처리 종료
    private void DrawOpeningCaravanReward() // 처음 아군 손패에서 실제 구입으로 받은 카드 지급
    { // 상단 저울의 적립 드로우 정산 시작
        if (CurrentRound != 1 || !IsActiveExplorationBattle || explorationRun.PendingCaravanDraws <= 0) // 실제 탐사 조우의 첫 아군 손패와 남은 적립 확인
        { // 지급할 수 없는 전투 및 턴 시작
            return; // 비탐사 및 다른 아군 턴의 예약 보존
        } // 지급할 수 없는 전투 및 턴 종료
        int drawn = runtimeDeck.DrawRelicCardsIntoHand(explorationRun.PendingCaravanDraws); // 기존 드로우의 손패 상한과 유효 더미를 반영한 실제 도착 장수
        explorationRun.SpendCaravanDraws(drawn); // 슬롯 부족이나 드로우 실패분을 다음 실제 교전으로 이월
        LastDrawnCardCount += drawn; // 이번 아군 턴에 실제로 도착한 카드 수에 보상 합산
    } // 상단 저울의 적립 드로우 정산 종료
    private void PrepareOpeningBookmark() // 첫 실제 아군 손패 뒤 서고 먼지갈피 선택 후보
    { // 처음 손패 개막 선택 확인 시작
        if (CurrentRound != 1 || !IsPlayerTurn || IsBattleEnded || runtimeDeck.HandCount >= runtimeDeck.MaxHandSize) // 전투가 계속되는 첫 정상 아군 턴과 손패 공간 확인
        { // 선택 준비 불가 시작
            return; // 처음 손패를 벗어난 다른 시점에서의 지급 생략
        } // 선택 준비 불가 종료
        var relic = openingRelics?.FindEffect(RelicEffectType.LibraryBookmarkOpeningPick); // 정상 탐사 보관함에 실제 등록된 서고 먼지갈피
        if (relic == null) // 실제 유물 보유 조건 확인
        { // 개막 효과 미보유 시작
            return; // 일반 개막의 손패 선택 생략
        } // 개막 효과 미보유 종료
        var choices = runtimeDeck.PrepareDrawChoices(relic.EffectValue, recycleDiscard: false); // 현재 실제 뽑기 더미에서만 최대 세 후보 확인
        OpeningBookmarkChoices = choices.Count > 0 ? choices : null; // 정상 손패에 넣을 후보가 있는 경우에만 입력 잠금
    } // 처음 손패 개막 선택 확인 종료
    internal void ResolveOpeningBookmark(CardInstance choice) // 표시 중인 개막 후보 선택 또는 취소
    { // 개막 선택 정산 시작
        if (OpeningBookmarkChoices == null) // 실제 선택이 이미 끝났거나 생성되지 않은 경우 확인
        { // 중복 또는 무효 결과 차단 시작
            return; // 재선택과 중복 입력 알림 생략
        } // 중복 또는 무효 결과 차단 종료
        var offered = OpeningBookmarkChoices; // 화면에 표시한 실제 유물 선택 후보 보존
        OpeningBookmarkChoices = null; // 덱 알림이 발생하기 전 단일 입력의 기회 종료
        if (IsPlayerTurn && !IsBattleEnded && choice != null && offered.Contains(choice)) // 기존 후보 중 하나를 실제 아군 턴에서 선택했는지 확인
        { // 실제 선택 카드 이동 시작
            runtimeDeck.TryMoveDrawCardToHand(choice); // 다른 카드의 순서를 유지하면서 지정 원본만 손패로 이동
        } // 실제 선택 카드 이동 종료
        StateChanged?.Invoke(); // 선택 또는 취소 이후 일반 전투 입력 복구 알림
    } // 개막 선택 정산 종료
    private void ApplyOpeningTemporaryThrust() // 귀족의 검은 단추가 생성하는 첫 생존 아군의 찌르기
    { // 첫 손패 생성 유물 처리 시작
        if (openingTemporaryCardChecked) // 같은 전투의 최초 손패 기회를 이미 처리했는지 확인
        { // 반복 발동 차단 시작
            return; // 중복 생성과 나중 턴으로 기회 이월 생략
        } // 반복 발동 차단 종료
        openingTemporaryCardChecked = true; // 손패 상태 알림이나 상한 실패 이전 한 번의 첫 손패 기회 기록
        if (openingRelics.FindEffect(RelicEffectType.NobleButtonOpeningTemporaryCard) == null || runtimeDeck.HandCount >= runtimeDeck.MaxHandSize) // 실제 보유 단추와 기존 드로우 이후 남은 손패 확인
        { // 생성할 수 없는 첫 손패 처리 시작
            return; // 가득 찬 정상 손패의 기존 카드 보존
        } // 생성할 수 없는 첫 손패 처리 종료
        var catalog = UnityEngine.Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 기본 찌르기의 공용 원본 식별자 카탈로그
        if (catalog == null) // 실제 발급에 필요한 원본 목록 존재 확인
        { // 카탈로그 누락 처리 시작
            return; // 다른 효과나 다른 카드로 대체하지 않는 생성 중단
        } // 카탈로그 누락 처리 종료
        foreach (var card in catalog.Cards) // 실제 저장 식별자에 등록된 기존 카드 자료 순회
        { // 카드 원본 일치 확인 시작
            if (card == null || card.CardId != "CRD_SHEET_THRUST") // 원본의 표준 찌르기와 다른 카드 구분
            { // 다른 원본 처리 시작
                continue; // 다음 등록 원본 확인
            } // 다른 원본 처리 종료
            foreach (var ally in allyUnits) // 실제 아군의 기존 전투 배치 순서 확인
            { // 첫 생존 아군 판정 시작
                if (ally is { Team: BattleTeam.Ally, IsDead: false, CharacterSource: not null }) // 실제 카드 소유자가 가능한 정상 생존 아군 확인
                { // 첫 생존 아군의 무료 카드 생성 시작
                    runtimeDeck.TryAddOpeningTemporaryCard(card, ally); // 원래 손패 상한을 유지한 전투 한정 카드 생성
                    return; // 다른 아군의 추가 사본 없이 첫 생성 정산
                } // 첫 생존 아군의 무료 카드 생성 종료
            } // 첫 생존 아군 판정 종료
            return; // 정상 원본 확인 뒤 살아 있는 카드 주인이 없는 전투의 생성 생략
        } // 카드 원본 일치 확인 종료
    } // 첫 손패 생성 유물 처리 종료
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
    private void ApplyLordOrderOpeningAttack() // 첫 실제 두 아군 턴의 일반 공격력 증가
    { // 명령서의 첫 아군 턴 조건 확인 시작
        var relic = completedLordOrderAllyTurns == 0 ? openingRelics?.FindEffect(RelicEffectType.LordsFinalOrderAttackThenDefenseLoss) : null; // 실제 첫 아군 턴의 보유 명령서 조회
        if (relic == null) // 최초 실제 순서 및 원본 유물 보유 확인
        { // 명령 강화 적용 제외 시작
            return; // 두 번째 턴에는 남은 원래 강화만 유지
        } // 명령 강화 적용 제외 종료
        foreach (var ally in allyUnits) // 현재 전투에 배치된 아군 순회
        { // 개별 아군의 생존 확인 시작
            if (ally?.Team == BattleTeam.Ally && !ally.IsDead) // 실제 첫 아군 턴의 생존 대상 확인
            { // 일반 공격력과 별도로 저장할 명령 강화 시작
                var result = ally.ApplyStatusEffect(BattleStatusEffectType.LordFinalOrderAttack, relic.EffectValue, relic.StatusDuration, 1); // 현재 아군 턴을 포함한 두 턴의 공격력 명령
                if (result is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked) // 실제 생존 아군의 강화 적용 확인
                { // 이번 아군 턴의 자연 차감 준비 시작
                    ally.GetStatusEffect(BattleStatusEffectType.LordFinalOrderAttack)?.ConsumeDurationDecreaseSkip(); // 시전 뒤 첫 종료부터 원래 두 아군 턴 차감
                } // 이번 아군 턴의 자연 차감 준비 종료
            } // 일반 공격력과 별도로 저장할 명령 강화 종료
        } // 개별 아군의 생존 확인 종료
    } // 명령서의 첫 아군 턴 조건 확인 종료

    private void ApplyLordOrderDefenseCost() // 두 아군 턴 뒤 처음 실제 적 턴의 방어 약화
    { // 별도 방어 대가 차례 확인 시작
        if (completedLordOrderAllyTurns < 2 || lordOrderDefenseResolved || openingRelics?.FindEffect(RelicEffectType.LordsFinalOrderAttackThenDefenseLoss) == null) // 선제와 예정 생략에 관계없는 두 실제 종료 확인
        { // 방어 약화 시점 제외 시작
            return; // 정해진 두 아군 턴과 남은 발동 기회 보존
        } // 방어 약화 시점 제외 종료
        lordOrderDefenseResolved = true; // 상태 적용 이벤트 전에 다음 적 턴 중복 처리 차단
        foreach (var ally in allyUnits) // 실제로 남아 있는 아군 명단 순회
        { // 현재 적 턴의 개별 약화 확인 시작
            if (ally?.Team == BattleTeam.Ally && !ally.IsDead) // 앞선 강화 제거와 별개인 생존 아군 확인
            { // 적 턴에 한 번의 물리 방어 약화 부여 시작
                ally.ApplyStatusEffect(BattleStatusEffectType.LordFinalOrderWeakenedDefense, 2, 1, 1); // 기존 상태 면역과 자연 적 턴 만료를 쓰는 방어력 두 점 감소
            } // 적 턴에 한 번의 물리 방어 약화 부여 종료
        } // 현재 적 턴의 개별 약화 확인 종료
    } // 별도 방어 대가 차례 확인 종료

    private void ApplyBanquetHeartHealing() // 현재 중독 상태에 따른 아군 턴 시작 회복
    { // 살아 있는 중독 적의 실제 인원 계산 시작
        int perEnemy = openingRelics?.FindEffect(RelicEffectType.BanquetHeartPoisonCountHealing)?.EffectValue ?? 0; // 보유 잔뿌리의 마리당 회복량
        if (perEnemy <= 0 || lastBanquetHeartRound == CurrentRound || IsBattleEnded || HasPendingBossPhaseTransition) // 실제 유물과 같은 턴 및 전투 단계 확인
        { // 회복 제외 시작
            return; // 중복 및 전환 중 아군 체력 유지
        } // 회복 제외 종료
        lastBanquetHeartRound = CurrentRound; // 체력 회복 이벤트 전에 현재 아군 턴의 시도 기록
        int poisonedEnemies = 0; // 살아 있고 활성 중독이 있는 실제 적 인원
        foreach (var enemy in enemyUnits) // 현재 적 명단 순회 시작
        { // 마리별 중독 상태 검사
            if (enemy?.Team == BattleTeam.Enemy && !enemy.IsDead && enemy.GetStatusEffect(BattleStatusEffectType.Poison) is { IsExpired: false }) // 상태 중첩과 무관한 살아 있는 중독 적 확인
            { // 실제 회복량에 포함할 적 처리 시작
                poisonedEnemies++; // 각 적의 출전 상태를 한 번만 합산
            } // 실제 회복량에 포함할 적 처리 종료
        } // 마리별 중독 상태 검사 종료
        if (poisonedEnemies == 0) // 실제 회복을 일으킬 적 중독 상태 확인
        { // 회복 대상 없음 처리 시작
            return; // 아군에게 불필요한 회복 신호 생략
        } // 회복 대상 없음 처리 종료
        int healing = (int)Math.Min(int.MaxValue, (long)perEnemy * poisonedEnemies); // 정수 범위 내 중독 적 인원별 실제 회복량
        foreach (var ally in allyUnits) // 현재 실제 아군 명단 순회
        { // 개별 생존 아군 회복 시작
            if (ally?.Team == BattleTeam.Ally && !ally.IsDead) // 전투불능을 제외한 회복 대상 확인
            { // 기존 회복량과 최대 체력 처리 시작
                ally.RestoreHealth(healing); // 받는 회복 효과와 최대 체력 및 실제 회복 신호의 기존 경로
            } // 기존 회복량과 최대 체력 처리 종료
        } // 개별 생존 아군 회복 종료
    } // 살아 있는 중독 적의 실제 인원 계산 종료

    private void ApplyChandelierPulse() // 실제 아군 턴의 광역 피해와 그 턴 명중률 약화
    { // 원본 유물의 실제 주기 조건 확인 시작
        var relic = openingRelics?.FindEffect(RelicEffectType.ChandelierThirdTurnPulse); // 보유 검은 샹들리에 핵의 현재 원본
        int interval = relic?.PlayerTurnInterval ?? 0; // 아군 턴에만 반복하는 원래 유물 주기
        if (interval <= 0 || CurrentRound % interval != 0 || lastChandelierRound == CurrentRound || !HasLivingUnit(enemyUnits)) // 실제 생존 적과 지정 주기 및 중복 점검
        { // 유효 광역 발동 제외 시작
            return; // 지정 차례가 아닌 경우 피해 및 약화 유지
        } // 유효 광역 발동 제외 종료
        lastChandelierRound = CurrentRound; // 실제 피해 신호 전에 같은 턴의 재발동 차단
        foreach (var enemy in new List<BattleUnitRuntime>(enemyUnits)) // 피해 도중 전투 참여 명단 변경을 고려한 적 목록
        { // 실제 생존 대상의 일반 피해 처리 시작
            if (IsBattleEnded || HasPendingBossPhaseTransition) // 직전 피해가 이번 전투나 보스 페이즈를 마쳤는지 확인
            { // 후속 광역 처리 취소 시작
                return; // 이전 전투 단계가 끝나면 남은 대상 및 약화 생략
            } // 후속 광역 처리 취소 종료
            if (enemy?.Team == BattleTeam.Enemy && !enemy.IsDead && ContainsUnit(enemyUnits, enemy)) // 현재 전투의 살아 있는 적만 대상으로 확인
            { // 실제 일반 광역 피해 적용 시작
                enemy.TakeDamage(relic.EffectValue, BattleDamageType.None); // 기존 방어도를 따르는 출처 없는 일반 피해
            } // 실제 일반 광역 피해 적용 종료
        } // 실제 생존 대상의 일반 피해 처리 종료
        if (IsBattleEnded || HasPendingBossPhaseTransition) // 마지막 광역 피해 뒤 실제 전투 지속 확인
        { // 전투 진행 불가 처리 시작
            return; // 끝난 전투와 다른 페이즈의 명중률 불변
        } // 전투 진행 불가 처리 종료
        foreach (var ally in allyUnits) // 실제 남은 아군 명단 순회
        { // 현재 아군 턴의 유효 대상 확인 시작
            if (ally?.Team == BattleTeam.Ally && !ally.IsDead) // 현재 턴에 행동할 수 있는 생존 아군 확인
            { // 기존 명중률 약화 상태의 실제 적용 시작
                var result = ally.ApplyStatusEffect(BattleStatusEffectType.Disorientation, 5, relic.StatusDuration, 1); // 현재 아군 턴 명중률 오 퍼센트포인트 감소
                if (result is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked) // 현재 실제로 남은 약화 확인
                { // 이번 턴을 포함한 약화 수명 준비 시작
                    ally.GetStatusEffect(BattleStatusEffectType.Disorientation)?.ConsumeDurationDecreaseSkip(); // 이후 적 턴에 샹들리에 명중 감소를 이월하지 않음
                } // 이번 턴을 포함한 약화 수명 준비 종료
            } // 기존 명중률 약화 상태의 실제 적용 종료
        } // 현재 아군 턴의 유효 대상 확인 종료
    } // 원본 유물의 실제 주기 조건 확인 종료

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
            ApplyLordOrderDefenseCost(); // 두 완료 아군 턴 뒤 첫 실제 적 턴에 방어 대가 적용
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
            ApplyLordOrderOpeningAttack(); // 선제와 관계없는 첫 실제 아군 턴에 명령서 강화 적용
            ApplyDustClock(); // 지정 주기의 실제 아군 턴에 생존 적 정신력 감소
            ApplyBanquetOilPulse(); // 선제 이후의 실제 아군 턴 번호로 한 번의 전체 중독 판정
            ApplyBanquetHeartHealing(); // 현재 생존 중독 적 수에 맞춘 전체 아군 회복
            ApplyChandelierPulse(); // 세 번째 아군 턴마다 일반 광역 피해와 한 턴의 명중률 약화
            if (IsBattleEnded) // 샹들리에의 실제 적 전멸로 전투가 종료됐는지 확인
            { // 이전 아군 턴 효과 중단 시작
                return; // 종료된 전투에 차례 시작과 추가 상태 효과 발행 방지
            } // 이전 아군 턴 효과 중단 종료
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
            completedLordOrderAllyTurns = Math.Min(2, completedLordOrderAllyTurns + 1); // 적 페이즈와 별개인 완료 아군 턴 두 개 집계
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
        if (pendingBlackCutleryDrawRound == CurrentRound && IsPlayerTurn) // 같은 실제 아군 턴의 카드 사용이 만든 출혈 보상 확인
        { // 카드가 비운 손패의 출혈 보상 처리 시작
            pendingBlackCutleryDrawRound = 0; // 손패 이벤트 전에 이번 카드의 예약 해제
            DrawBlackCutleryCards(openingRelics?.FindEffect(RelicEffectType.BlackCutleryBleedStackDraw)?.EffectValue ?? 0); // 확보된 손패 자리에서 검은 식기함 보상 지급
        } // 카드가 비운 손패의 출혈 보상 처리 종료
    } // 카드 정산 완료 처리 종료

    private void HandleUnitDefeated(BattleUnitRuntime source, BattleUnitRuntime target) // 아군이 적을 실제로 처치한 경우 확인
    { // 처치 드로우 예약 시작
        RecordPermanentAttackDefeat(target); // 턴별 카드 드로우와 별개로 실제 정예·보스 처치 보상 먼저 정산
        if (!disposed && !IsBattleEnded && target is { Team: BattleTeam.Enemy, IsDead: true, DefeatedWhileBurning: true } && CurrentPhase is (BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn)) // 두 실제 턴의 화상 중 적 전투불능만 검사
        { // 화상 처치의 실제 회복 시작
            int healing = openingRelics?.FindEffect(RelicEffectType.SilverSnufferBurnKillHealing)?.EffectValue ?? 0; // 망가진 은촛불집게의 원본 회복량 확인
            if (healing > 0) // 실제 장착한 유물의 양수 회복 확인
            { // 생존 아군 한 명 회복 시작
                GetLowestHealthLivingAlly()?.RestoreHealth(healing); // 기존 현재 체력 및 명단 순서와 실제 회복 규칙으로 승리 전에 지급
            } // 생존 아군 한 명 회복 종료
        } // 화상 처치의 실제 회복 종료
        ApplyAshstormPouchBurn(target); // 화상 상태에서 실제로 쓰러진 적의 남은 불길 이전
        ApplyOperaMaskMentalLoss(source, target); // 공포 상태에서 실제로 쓰러진 적의 생존 적 전체 반응
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

    private void ApplyAshstormPouchBurn(BattleUnitRuntime defeated) // 실제 화상 적의 처치와 다른 생존 적의 일반 화상 부여
    { // 화상 피해 수치와 유효한 현재 전투 확인 시작
        if (disposed || IsBattleEnded || HasPendingBossPhaseTransition || CurrentPhase is not (BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn) || // 끝난 전투나 생존한 보스 단계 전환 제외
            defeated is not { Team: BattleTeam.Enemy, IsDead: true, DefeatedBurnAmount: > 0 }) // 실제 죽은 적의 사망 직전 화상 수치 확인
        { // 유효한 화상 사망이 아닌 경우 시작
            return; // 살아 있는 적의 화상과 원래 난수 상태 유지
        } // 유효한 화상 사망이 아닌 경우 종료
        var relic = openingRelics?.FindEffect(RelicEffectType.AshstormAshPouchBurnOnDefeat); // 실제 보유한 재 주머니의 원본 기간
        if (relic == null) // 보유하지 않은 해당 전투 확인
        { // 원래 상태 그대로 유지 시작
            return; // 추가 대상 선정과 난수 판정 생략
        } // 원래 상태 그대로 유지 종료
        var candidates = new List<BattleUnitRuntime>(); // 같은 확률로 고를 실제 다른 생존 적들
        foreach (var enemy in enemyUnits) // 현재 전투의 실제 배치와 소환 명단 순회
        { // 다른 생존 적인지 확인 시작
            if (enemy != defeated && enemy is { Team: BattleTeam.Enemy, IsDead: false }) // 실제 죽은 적이나 무효 대상 제외
            { // 살아 있는 적 후보에 추가 시작
                candidates.Add(enemy); // 현재 실제 다른 적 한 명의 가능성 확보
            } // 살아 있는 적 후보에 추가 종료
        } // 다른 생존 적인지 확인 종료
        if (candidates.Count > 0) // 다른 생존 적이 있을 때만 한 번 추첨
        { // 이미 있던 화상 규칙을 따르는 효과 시작
            candidates[UnityEngine.Random.Range(0, candidates.Count)].ApplyStatusEffect(BattleStatusEffectType.Burn, defeated.DefeatedBurnAmount, relic.StatusDuration, relic.StatusMaximumStacks); // 죽은 적의 실제 화상량을 다른 적에게 원래 면역과 두 턴으로 적용
        } // 이미 있던 화상 규칙을 따르는 효과 종료
    } // 화상 피해 수치와 유효한 현재 전투 확인 종료

    private void ApplyOperaMaskMentalLoss(BattleUnitRuntime source, BattleUnitRuntime defeated) // 공포 상태의 실제 처치에 따른 생존 적 정신 피해
    { // 오페라 가면의 실제 처치 반응 시작
        if (disposed || IsBattleEnded || CurrentPhase is not (BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn) || // 진행 중인 두 진영의 실제 처치 확인
            defeated is not { Team: BattleTeam.Enemy, IsDead: true, DefeatedWhileAfraid: true }) // 일반 생존 경계와 달리 공포 적의 전투불능으로 제한
        { // 공포 처치 이외의 사건 시작
            return; // 남아 있는 적에게 정신 변화 없음
        } // 공포 처치 이외의 사건 종료
        int loss = openingRelics?.FindEffect(RelicEffectType.OperaMaskAfraidKillMentalLoss)?.EffectValue ?? 0; // 실제 보유한 오페라 가면의 원본 정신력 감소량
        if (loss <= 0) // 보유 유물과 양의 효과 수치 확인
        { // 원본 정신 피해 미적용 시작
            return; // 유물 없이 적 정신력 유지
        } // 원본 정신 피해 미적용 종료
        foreach (var enemy in enemyUnits) // 현재 전투에 살아 있는 적 확인
        { // 생존 원본 대상 검사 시작
            if (enemy is { Team: BattleTeam.Enemy, IsDead: false }) // 실제 사망자와 잘못된 진영 제외
            { // 기존 정신력 피해 적용 시작
                enemy.ChangeMental(-loss, BattleMentalChangeReason.RelicEffect, source); // 공격자 참조의 유무와 무관한 실제 정신력 감소
            } // 기존 정신력 피해 적용 종료
        } // 생존 원본 대상 검사 종료
    } // 오페라 가면의 실제 처치 반응 종료

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
        ApplyDryWaterskinHealing(); // 승리와 도주의 실제 생존 팀 회복을 결과 저장 전에 적용
        SettleNamelessOath(); // 종료 회복 뒤 현재 손실 체력 기준의 서약 비용 저장
        return new BattleResultData(Result, BattleType, CurrentRound, allyUnits, defeatedEnemyIds); // 회복 반영 전투 결과
    } // 결과 생성 끝

    private void ApplyDryWaterskinHealing() // 정상 종료의 생존 아군에게 한 번 적용할 물주머니 회복
    { // 현재 전투 결과와 유물 확인 시작
        if (dryWaterskinSettled || Result is not (BattleResult.Victory or BattleResult.Escape)) // 실제 승리나 도주가 아닌 결과와 반복 저장 제외
        { // 지급하지 않을 결과 처리 시작
            return; // 패배와 미종료 및 이미 저장한 결과에서는 회복 없음
        } // 지급하지 않을 결과 처리 종료
        dryWaterskinSettled = true; // 회복 사건과 저장 재시도 앞서 중복 발동 방지
        int healing = openingRelics?.FindEffect(RelicEffectType.DryWaterskinBattleEndHealing)?.EffectValue ?? 0; // 실제 보유한 물주머니의 기본 회복량
        if (healing <= 0) // 보유하지 않았거나 유효한 원본 수치가 없는 경우
        { // 미보유 회복 제외 시작
            return; // 아군의 현재 체력 유지
        } // 미보유 회복 제외 종료
        foreach (var ally in allyUnits) // 현재 전투에 참가했던 각 아군 확인
        { // 생존 아군의 기존 회복 처리 시작
            if (ally is { IsDead: false }) // 실제 생존 동료만 회복
            { // 일반 체력 회복 시작
                ally.RestoreHealth(healing); // 원래 최대 체력과 회복 효과가 반영되는 실제 치유
            } // 일반 체력 회복 종료
        } // 생존 아군의 기존 회복 처리 종료
    } // 현재 전투 결과와 유물 확인 종료

    private bool CanResolveFirstAllyAttack => !disposed && IsPlayerTurn && !IsBattleEnded && !HasPendingBossPhaseTransition && firstActualAllyTurnRound > 0 && CurrentRound == firstActualAllyTurnRound; // 최초 실제 아군 차례의 진행 상태

    internal void BeginFirstAllyAttack(CardInstance card) // 확정 사용한 일반 피해 카드의 첫 실제 공격 선언
    { // 첫 아군 공격 가산 준비 시작
        if (!CanResolveFirstAllyAttack || firstAllyAttackDeclared || card.EffectType != CardEffectType.Damage || card.OwnerUnit is not { Team: BattleTeam.Ally, IsDead: false }) // 현재 아군 차례의 실제 첫 피해 카드 자격 확인
        { // 선언 자격 없는 카드 제외 시작
            return; // 지원 카드와 뒤 이은 공격의 새 기회 없음
        } // 선언 자격 없는 카드 제외 종료
        firstAllyAttackDeclared = true; // 명중 여부와 관계없이 실제 최초 공격 선언 사용
        firstAttackBonusOwner = card.OwnerUnit; // 현재 공격의 실제 카드 소유자 저장
        pendingFirstAttackBonus = openingRelics?.FindEffect(RelicEffectType.AmbushBugAntennaFirstTurnFirstAttackDamage)?.EffectValue ?? 0; // 등록된 첫 타격 가산 원본 확인
    } // 첫 아군 공격 가산 준비 종료

    internal int ResolveFirstAllyAttackBonus(BattleUnitRuntime source, bool preview) // 미사용 예고와 선언한 카드의 첫 실제 명중 구분
    { // 최초 아군 공격 기회 처리 시작
        if (!CanResolveFirstAllyAttack || !registeredUnits.Contains(source)) // 같은 전투의 등록 공격자와 실제 처음 차례 확인
        { // 이미 끝난 첫 아군 차례 제외 시작
            return 0; // 전투 단계가 바뀐 뒤에는 가산 없음
        } // 이미 끝난 첫 아군 차례 제외 종료
        if (preview) // 사용 전에 확인하는 순수 첫 공격 예고
        { // 선언하지 않은 원래 가산의 예고 시작
            return firstAllyAttackDeclared ? 0 : openingRelics?.FindEffect(RelicEffectType.AmbushBugAntennaFirstTurnFirstAttackDamage)?.EffectValue ?? 0; // 실제로 예약할 수 있는 값의 상태 미변경 조회
        } // 선언하지 않은 원래 가산의 예고 종료
        if (source != firstAttackBonusOwner) // 가산을 선언한 현재 카드의 실제 소유자 확인
        { // 다른 아군 행동이나 직전 카드 제외 시작
            return 0; // 불일치 공격에는 예약 값 미사용
        } // 다른 아군 행동이나 직전 카드 제외 종료
        int bonus = pendingFirstAttackBonus; // 실제 첫 명중으로 확정할 가산량 복사
        EndFirstAllyAttack(); // 같은 카드의 다른 타격에 남은 가산 없음
        return bonus; // 실제 방어 전 첫 타격에 가산 반환
    } // 최초 아군 공격 기회 처리 종료

    internal void EndFirstAllyAttack() // 사용한 공격이 끝났을 때 가산 기회 해제
    { // 현재 공격 카드에만 예약된 자료 초기화 시작
        firstAttackBonusOwner = null; // 이후 공격의 실제 소유자에게 기회 이전 차단
        pendingFirstAttackBonus = 0; // 전부 빗나간 공격 이후에도 남은 가산 초기화
    } // 현재 공격 카드에만 예약된 자료 초기화 종료

    internal int GetSandBoneDamageBonus(BattleUnitRuntime source, BattleUnitRuntime target) // 순수 예고와 실제 카드 타격이 공유하는 첫 활성 출혈 가산
    { // 현재 전투의 한 번 제한과 양 진영 조건 확인 시작
        if (disposed || IsBattleEnded || HasPendingBossPhaseTransition || CurrentRound <= 0 || sandBoneUsed || // 진행 중 실제 전투의 아직 남은 첫 가산 확인
            source is not { Team: BattleTeam.Ally, IsDead: false } || !registeredUnits.Contains(source) || // 같은 전투에 등록된 생존 아군의 직접 타격 확인
            target is not { Team: BattleTeam.Enemy, IsDead: false } || !registeredUnits.Contains(target) || target.GetStatusEffect(BattleStatusEffectType.Bleed) is not { IsExpired: false }) // 실제 생존 적과 타격 직전의 활성 출혈 조건
        { // 가산 조건에 맞지 않는 타격 처리 시작
            return 0; // 이미 사용하거나 활성 출혈이 없는 경우 가산 없음
        } // 가산 조건에 맞지 않는 타격 처리 종료
        return openingRelics?.FindEffect(RelicEffectType.SandBoneFirstBleedingEnemyDamage)?.EffectValue ?? 0; // 이번 전투에 보유한 원본 방어 전 피해 두 점
    } // 현재 전투의 한 번 제한과 양 진영 조건 확인 종료

    internal void ConsumeSandBoneDamage() // 실제 체력 또는 방어도 피해가 확정한 첫 가산 소모
    { // 전투당 첫 유효 가산 기록 시작
        sandBoneUsed = true; // 다음 직접 명중과 예고에서 같은 가산 재사용 방지
    } // 전투당 첫 유효 가산 기록 종료

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
    private void ClearDistributionStatuses() // 이번 전투의 원본과 복사 분배 상태를 종료 시 해제
    { // 참가한 모든 아군의 지속 상태 해제 시작
        foreach (var ally in allyUnits) // 선택 대상과 강화를 복사한 생존 여부 무관 아군 조회
        { // 아군별 실제 전투 지속 상태 제거 시작
            ally?.RemoveStatusEffect(BattleStatusEffectType.DistributionAttackUp); // 종료 전의 남은 분배 공격 강화 제거
            ally?.RemoveStatusEffect(BattleStatusEffectType.DistributionPhysicalDefenseDown); // 종료 전의 남은 분배 물리 방어 약화 제거
        } // 아군별 실제 전투 지속 상태 제거 종료
    } // 참가한 모든 아군의 지속 상태 해제 종료

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
        ClearDistributionStatuses(); // 승리와 패배 및 도주 결과 확정 뒤 분배 상태 종료
        SetEfficientUseDiscount(0); // 승리 및 패배와 도주 뒤 남은 턴 할인 제거
        nextPlayerTurnActionPointLoss = 0; // 전투 종료 후 미사용 행동력 차감 제거
        pendingSwordThenShieldBlock = 0; // 같은 전투에서 사용하지 않은 메달 조각 방어도 정리
        OpeningBookmarkChoices = null; // 이미 끝난 전투에서는 대기 중인 개막 선택 제거
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
            ally?.SpendBattleMentalCost(10); // 생존 아군에게 직접 정신 피해 보호와 별개인 고정 대가 적용
        } // 개별 아군의 정신력 대가 처리 종료
    } // 어비스 성배 조각의 부활 확인 종료
    private void RegisterUnitEvents(IReadOnlyList<BattleUnitRuntime> units) // 유닛 사망 이벤트 등록
    { // 이벤트 등록 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 유닛 이벤트 등록 시작
            RegisterUnitEvent(runtimeUnit); // 개별 유닛 사망 이벤트 등록
        } // 유닛 이벤트 등록 종료
    } // 이벤트 등록 종료
    private void HandleAlliedPhysicalDefenseReduced(BattleUnitRuntime source, BattleUnitRuntime target) // 실제 적 물리 방어가 내려간 뒤 검은 모래의 중독 추첨
    { // 현재 전투와 실제 적격 부여자 확인 시작
        if (disposed || IsBattleEnded || CurrentRound <= 0 || HasPendingBossPhaseTransition || target is not { Team: BattleTeam.Enemy, IsDead: false } || source?.Team != BattleTeam.Ally) // 회복 불가 단계나 적용하지 못한 원래 상태 제외
        { // 물리 방어 감소 유물이 발동하지 않는 상황 시작
            return; // 관련 없는 상태의 난수와 대상 보존
        } // 물리 방어 감소 유물이 발동하지 않는 상황 종료
        var relic = openingRelics?.FindEffect(RelicEffectType.MineBlackSandPoisonOnDefenseDown); // 실제 보유한 검은 모래의 고정 중독과 확률
        int chance = relic?.TriggerChancePercent ?? 0; // 해당 적격 약화 한 번의 실제 확률 조회
        if (chance > 0 && (chance >= 100 || UnityEngine.Random.Range(0, 100) < chance)) // 성공한 실제 물리 방어 감소에만 한 번 판정
        { // 정상 중독 적용과 발생자 유지 시작
            target.ApplyStatusEffect(BattleStatusEffectType.Poison, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks, source); // 같은 약화의 실제 아군 출처를 가진 두 턴의 적 중독
        } // 정상 중독 적용과 발생자 유지 종료
    } // 현재 전투와 실제 적격 부여자 확인 종료

    private void HandleRelicAppliedStatus(BattleUnitRuntime source, BattleUnitRuntime target, BattleStatusEffectType type, BattleStatusEffectApplyResult result) // 실제 적용 또는 갱신이 남은 전투 상태 유물 처리
    { // 상태 적용 유물 정산 시작
        if (disposed || IsBattleEnded || CurrentRound <= 0 || target == null || target.IsDead || result is not (BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked)) // 활성 전투에서 남은 유효한 신규 또는 갱신 상태 확인
        { // 무효 상태 신호 제외 시작
            return; // 저항, 즉시 정화 및 전투 밖 상태의 발동 제외
        } // 무효 상태 신호 제외 종료
        if (type == BattleStatusEffectType.Bleed && target.Team == BattleTeam.Enemy && target.GetStatusEffect(type) is { IsExpired: false }) // 실제 활성 출혈이 성공한 생존 적 확인
        { // 전체 적의 현재 출혈 누적 검사 시작
            TryDrawBlackCutlery(); // 실제 아군 턴의 출혈 중첩으로 한 번만 카드 보상 시도
        } // 전체 적의 현재 출혈 누적 검사 종료
        if (type == BattleStatusEffectType.Fear && source?.Team == BattleTeam.Ally && target.Team == BattleTeam.Enemy && target.GetStatusEffect(type) is { IsExpired: false }) // 생존 적에게 남은 실제 아군 공포 확인
        { // 초대장의 실제 회피 감소 처리 시작
            var invitation = openingRelics?.FindEffect(RelicEffectType.FearGrantedEvasionLoss); // 현재 보유한 찢어진 초대장 조회
            if (invitation != null) // 현재 전투에서 원본 효과를 사용할 수 있는지 확인
            { // 초대장의 정상 약화 적용 시작
                target.ApplyStatusEffect(BattleStatusEffectType.EvasionDown, invitation.EffectValue, invitation.StatusDuration, 1, source); // 한 적 턴의 회피 감소를 기존 면역과 상태 수명에 연결
            } // 초대장의 정상 약화 적용 종료
            return; // 같은 적 대상의 원래 공포 효과 처리 유지
        } // 초대장의 실제 회피 감소 처리 종료
        if (target.Team != BattleTeam.Ally || source is not { Team: BattleTeam.Enemy, IsDead: false } || !registeredUnits.Contains(source)) // 실제 전투의 살아 있는 적이 부여한 아군 약화 확인
        { // 반사 원본이 없는 상태 시작
            return; // 실제 적 발생자와 관련 없는 상태의 난수 미소모
        } // 반사 원본이 없는 상태 종료
        var debuff = target.GetStatusEffect(type); // 보정과 상태 면역을 거친 활성 원본 약화
        if (!BattleUnitRuntime.IsRemovableActiveDebuff(debuff)) // 실제 지속 중이며 해제 가능한 약화 여부
        { // 반사 불가 상태 제외 시작
            return; // 보호 정신 상태와 일반 강화 및 즉시 제거된 약화 제외
        } // 반사 불가 상태 제외 종료
        int firstChance = firstHostileDebuffReflectAttempted ? 0 : openingRelics?.FindEffect(RelicEffectType.ReflectFirstAllyDebuff)?.TriggerChancePercent ?? 0; // 첫 유효 약화에 남아 있는 흐린 거울못 확률
        if (firstChance > 0) // 기존 최초 반사 기회가 남은 실제 보유 유물 확인
        { // 흐린 거울못의 첫 기회 처리 시작
            firstHostileDebuffReflectAttempted = true; // 결과 알림 전에 이번 전투의 첫 시도 소비
            if (TryReflectHostileDebuff(source, target, debuff, firstChance)) // 원래 적에게 이미 적용한 반사 결과 확인
            { // 두 번째 동일 복사 제외 시작
                return; // 중복 적용과 불필요한 두 번째 난수 생략
            } // 두 번째 동일 복사 제외 종료
        } // 흐린 거울못의 첫 기회 처리 종료
        int laughChance = openingRelics?.FindEffect(RelicEffectType.LaughMirrorDebuffReflection)?.TriggerChancePercent ?? 0; // 웃는 거울의 기존 매 사건 반사 확률
        if (TryReflectHostileDebuff(source, target, debuff, laughChance)) // 두 번째 우선순위의 실제 반사 성공 확인
        { // 이미 복제된 약화 처리 시작
            return; // 같은 약화의 왕관 난수와 중복 복제 제외
        } // 이미 복제된 약화 처리 종료
        int crownChance = openingRelics?.FindEffect(RelicEffectType.MirrorDukeCrownDebuffReflection)?.TriggerChancePercent ?? 0; // 보유 왕관의 같은 사건 최종 반사 확률
        TryReflectHostileDebuff(source, target, debuff, crownChance); // 앞선 거울이 복제하지 않은 원본 상태만 시도
    } // 상태 적용 유물 정산 종료

    private static bool TryReflectHostileDebuff(BattleUnitRuntime enemy, BattleUnitRuntime ally, BattleStatusEffectInstance debuff, int chance) // 활성 원본의 실제 적 반사와 성공 결과 확인
    { // 약화 반사 판정 시작
        return chance > 0 && (chance >= 100 || UnityEngine.Random.Range(0, 100) < chance) && // 각 유물의 한 번 확률 또는 확정 여부
            enemy.ApplyReflectedDebuff(debuff, ally) is (BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked); // 원래 발생자에게 기존 면역을 거쳐 실제 적용한 복사만 성공
    } // 약화 반사 판정 종료

    private void HandleEnemyAttackEvaded(BattleUnitRuntime source, BattleUnitRuntime target) // 현재 적 턴에 피해 공격을 실제 피한 아군 집계
    { // 무도회의 마지막 박자 정산 시작
        if (disposed || IsBattleEnded || HasPendingBossPhaseTransition || CurrentPhase != BattleTurnPhase.EnemyTurn || ballroomEvadesThisEnemyTurn >= 2 || // 유효한 현재 적 턴의 미지급 회피 보상 확인
            source is not { Team: BattleTeam.Enemy, IsDead: false } || !registeredUnits.Contains(source) || target is not { Team: BattleTeam.Ally, IsDead: false } || !registeredUnits.Contains(target)) // 실제 살아 있는 공격 적과 피한 아군의 등록 확인
        { // 유효하지 않은 회피 보상 제외 시작
            return; // 실전 참가자 없는 사건과 이미 지급한 현재 적 턴 제외
        } // 유효하지 않은 회피 보상 제외 종료
        int damage = openingRelics?.FindEffect(RelicEffectType.BallroomLastBeatSecondEvade)?.EffectValue ?? 0; // 실제 보유한 마지막 박자의 고정 일반 피해
        if (damage <= 0 || ++ballroomEvadesThisEnemyTurn != 2) // 보유 중 실제 두 번째 회피에서 먼저 지급 기록
        { // 아직 지급하지 않는 회피 처리 시작
            return; // 유물 부재와 첫 실제 회피의 피해 없음
        } // 아직 지급하지 않는 회피 처리 종료
        foreach (var enemy in new List<BattleUnitRuntime>(enemyUnits)) // 현재 전투 적 목록을 복사해 같은 발동의 대상 보존
        { // 대상별 원래 일반 피해 적용 시작
            if (IsBattleEnded || HasPendingBossPhaseTransition) // 앞선 피해로 일어날 실제 승리 또는 보스 전환 확인
            { // 처리 중 바뀐 전투 단계 확인 시작
                break; // 변경된 전투에 이전 광역 피해 전달 중단
            } // 처리 중 바뀐 전투 단계 확인 종료
            if (enemy is { Team: BattleTeam.Enemy, IsDead: false } && registeredUnits.Contains(enemy)) // 현재 그대로 참여하는 생존 적만 확인
            { // 기존 일반 피해 및 방어도 경로 적용 시작
                enemy.TakeDamage(damage, BattleDamageType.None, target); // 실제 회피 아군을 발생자로 기록하는 고정 일반 피해
            } // 기존 일반 피해 및 방어도 경로 적용 종료
        } // 대상별 원래 일반 피해 적용 종료
    } // 무도회의 마지막 박자 정산 종료

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
        runtimeUnit.StatusEffectResolved += HandleRelicAppliedStatus; // 양 진영에서 실제로 성공한 상태 부여 유물 처리 구독
        runtimeUnit.AlliedPhysicalDefenseReduced += HandleAlliedPhysicalDefenseReduced; // 실제 적 방어 변화와 아군 출처의 전투 유물 연결
        if (runtimeUnit.Team == BattleTeam.Ally) // 실제 아군의 손실과 부활 정산 연결 확인
        { // 아군 전투 유물 연결 시작
            runtimeUnit.AllyDefeatReady += HandleUnitDied; // 모든 사망 알림 뒤 아군 부활과 승패 정산
            runtimeUnit.EnemyAttackEvaded += HandleEnemyAttackEvaded; // 등록된 아군의 실제 적 공격 회피 연결
            runtimeUnit.DamageTaken += HandleAllyHealthLoss; // 체력에 도달한 단일 실제 피해 신호 연결
        } // 아군 전투 유물 연결 종료
        else // 기존 적 처치 처리 유지
        { // 적 사망 판정 연결 시작
            runtimeUnit.AlliedDirectDamageBattle = this; // 현재 전투의 적 직접 피해 예고와 실제 가산 연결
            runtimeUnit.Died += HandleUnitDied; // 기존 적 사망과 승리 판정 연결
        } // 적 사망 판정 연결 종료
        if (runtimeUnit.Team == BattleTeam.Ally) // 공용 유물의 보호를 받을 아군 확인
        { // 공용 보호 연결 시작
            runtimeUnit.PreventIncomingMentalLoss = TryNullifyFirstMentalDamage; // 현재 전투의 공용 보호 연결
            runtimeUnit.GetHostileBlindResistance = GetCombinedHostileBlindResistance; // 현재 실제 조우 및 첫 적 턴의 실명 저항 연결
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
        if (runtimeUnit.AlliedDirectDamageBattle == this) // 현재 해제하는 전투의 연결만 초기화
        { // 적의 직접 가산 연결 해제 시작
            runtimeUnit.AlliedDirectDamageBattle = null; // 이탈과 전투 해제 후 이전 전투 가산 차단
        } // 적의 직접 가산 연결 해제 종료
        runtimeUnit.Died -= HandleUnitDied; // 사망 이벤트 해제
        runtimeUnit.AllyDefeatReady -= HandleUnitDied; // 전투를 떠난 아군의 부활 정산 구독 해제
        runtimeUnit.EnemyAttackEvaded -= HandleEnemyAttackEvaded; // 전투를 떠난 아군의 적 공격 회피 구독 해제
        runtimeUnit.DamageTaken -= HandleAllyHealthLoss; // 전투를 떠난 아군의 체력 손실 구독 해제
        runtimeUnit.StealthEnded -= HandleAllyStealthEnded; // 전투를 떠난 아군의 은신 종료 구독 해제
        runtimeUnit.SeedRevivalSettled -= HandleUnitDied; // 전투 밖으로 나간 유닛의 부활 판정 해제
        runtimeUnit.Defeated -= HandleUnitDefeated; // 회전 깃발의 처치 구독 해제
        runtimeUnit.StatusEffectResolved -= HandleRelicAppliedStatus; // 전투를 떠난 개체의 상태 유물 구독 해제
        runtimeUnit.AlliedPhysicalDefenseReduced -= HandleAlliedPhysicalDefenseReduced; // 해당 전투 외 물리 방어 변화의 유물 연결 정리
        if (runtimeUnit.PreventIncomingMentalLoss == (Func<BattleMentalChangeReason, bool>)TryNullifyFirstMentalDamage) // 현재 전투가 연결한 보호인지 확인
        { // 공용 보호 연결 해제 시작
            runtimeUnit.PreventIncomingMentalLoss = null; // 전투를 떠난 아군의 공용 보호 해제
        } // 공용 보호 연결 해제 종료
        if (runtimeUnit.GetHostileBlindResistance == (Func<int>)GetCombinedHostileBlindResistance) // 이 전투에서 연결한 개막 효과인지 확인
        { // 동일 전투의 개막 효과 해제 시작
            runtimeUnit.GetHostileBlindResistance = null; // 전투를 떠난 아군의 첫 적 턴 저항 조회 해제
            runtimeUnit.NamelessOathAttackPower = 0; // 실제 전투 씬에서만 쓴 서약의 공격력 회수
        } // 동일 전투의 개막 효과 해제 종료
        return true; // 이벤트 해제 성공 반환
    } // 개별 사망 이벤트 해제 종료
    private int GetCombinedHostileBlindResistance() // 첫 적 턴 및 실제 사막 조우의 적 실명 저항 합계
    { // 실제 전투 중에만 적용되는 두 저항 계산 시작
        if (disposed || IsBattleEnded || CurrentRound <= 0) // 현재 실제 조우의 진행 상태 확인
        { // 조우 전후의 기존 계산 보존 시작
            return 0; // 활성 전투가 없을 때 실명 저항 추가 없음
        } // 조우 전후의 기존 계산 보존 종료
        int handkerchief = !firstEnemyTurnFinished && CurrentPhase == BattleTurnPhase.EnemyTurn // 선제 포함 첫 실제 적 턴만 적용할 기존 저항
            ? openingRelics?.FindEffect(RelicEffectType.FirstTurnBlindResistance)?.EffectValue ?? 0 : 0; // 보유 손수건의 첫 적 턴 보정
        int bracelet = Region == ExplorationRegion.Desert ? openingRelics?.FindEffect(RelicEffectType.DesertGlassBraceletBlindResistance)?.EffectValue ?? 0 : 0; // 사막의 적 실명에만 추가하는 팔찌 저항
        return (int)Math.Min(100, (long)handkerchief + bracelet); // 기존 확률 범위 안에서 두 실명 저항 퍼센트포인트 계산
    } // 실제 전투 중에만 적용되는 두 저항 계산 종료
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
        ClearDistributionStatuses(); // 장면 해제나 새 전투에도 현재 분배 수치가 남지 않는 정리
        SetEfficientUseDiscount(0); // 전투 관리자 해제 후 아군 소유자 할인 회수
        OpeningBookmarkChoices = null; // 해제 뒤 이전 전투의 개막 선택 후보 회수
        runtimeDeck.ClearBattleRelicCards(); // 결과 이전에 전투 해제할 때도 덱의 모든 전투 한정 카드 회수
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
