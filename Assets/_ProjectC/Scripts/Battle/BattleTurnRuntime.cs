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
    private BattleUnitRuntime openingVulnerableEnemy; // 첫 아군 턴까지 취약을 적용한 적
    private readonly List<(BattleUnitRuntime Enemy, BattleStatusEffectInstance Fear)> openingFearedEnemies = new List<(BattleUnitRuntime Enemy, BattleStatusEffectInstance Fear)>(); // 포스터가 새로 공포를 부여한 적과 상태
    private int openingSpecimenActionPoints; // 첫 아군 턴에 지급할 탐사 표본병 행동력
    private int nextPlayerTurnActionPointLoss; // 거인의 방패 사용으로 다음 아군 턴에 차감할 행동력
    private int lastVolatileDrawRound; // 안전핀의 마지막 발동 라운드
    private int lastExcludedBlockRound; // 봉인왁스의 마지막 발동 라운드
    private int lastFlagDrawRound; // 회전 깃발의 마지막 드로우 라운드
    private int lastSpringRound; // 붉은 스프링의 마지막 할인 준비 라운드
    private int lastEmptyHandDrawRound; // 패치 노끈의 마지막 예약 라운드
    private bool springReady; // 다음 유료 고정 카드에 남은 할인
    private bool resolvingCard; // 사용 카드가 손패를 떠날 때까지의 정산 상태
    private int pendingDefeatDraw; // 카드 정산 후 지급할 회전 깃발 드로우
    private long reflectionDamageNumerator; // 이번 적 턴에 누적한 체력 피해와 반사 비율의 곱
    private BattleUnitRuntime reflectionSource; // 처음 반사 대상으로 기록한 피격 아군
    private bool disposed; // 연결 해제 여부
    public int CurrentRound { get; private set; } // 현재 라운드
    public int LastDrawnCardCount { get; private set; } // 최근 드로우 수
    internal BattleDeckRuntime Deck => runtimeDeck; // 손패 버림 신호를 발행하는 현재 전투 덱
    public BattleTurnPhase CurrentPhase { get; private set; } = BattleTurnPhase.NotStarted; // 현재 턴 단계
    public BattleType BattleType { get; } // 현재 전투 유형 조회
    public BattleResult Result { get; private set; } = BattleResult.None; // 현재 전투 결과 조회
    public bool IsPlayerTurn => CurrentPhase == BattleTurnPhase.PlayerTurn; // 플레이어 턴 여부
    public bool IsBattleEnded => Result != BattleResult.None; // 전투 종료 여부
    public bool HasPendingBossPhaseTransition { get; private set; } // 보스 페이즈 전환 예약 여부
    public bool CanEscape => !disposed && (BattleType == BattleType.Normal || BattleType == BattleType.Elite) && CurrentPhase == BattleTurnPhase.PlayerTurn && !IsBattleEnded; // 일반과 정예 전투의 도주 가능 여부
    private bool CanReviveInCurrentPhase => !disposed && IsPlayerTurn && !IsBattleEnded && !HasPendingBossPhaseTransition; // 진행 중인 아군 턴의 부활 가능 여부
    internal bool HasFallenAlly() // 손패 카드가 부활시킬 수 있는 아군 존재 확인
    { // 전투불능 아군 검사 시작
        if (CanReviveInCurrentPhase) // 이번 턴 부활 가능 여부 확인
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
        return CanReviveInCurrentPhase && ally?.Team == BattleTeam.Ally && ally.IsDead && ContainsUnit(allyUnits, ally); // 전투 단계와 현재 참가 명단의 사망 상태 확인
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
    public event Action StateChanged; // 턴 상태 변경 이벤트
    public event Action<BattleTurnPhase, int> PhaseStarted; // 진영 턴 시작 이벤트
    public event Action<BattleTurnPhase, int> PhaseEnded; // 진영 턴 종료 이벤트
    public event Action<BattleResult> BattleCompleted; // 전투 종료 처리 완료 이벤트
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
        runtimeDeck.CardExcluded += HandleExcludedCard; // 실제 카드 제외 결과 연결
        RegisterUnitEvents(allyUnits); // 아군 사망 이벤트 등록
        RegisterUnitEvents(enemyUnits); // 적 사망 이벤트 등록
    } // 생성자 종료
    public bool StartBattle(int initialHandSize, bool enemyFirst = false, Action openingEffects = null) // 먼저 행동할 진영과 탐사 효과를 반영한 시작
    { // 전투 시작 처리 시작
        if (disposed || CurrentPhase != BattleTurnPhase.NotStarted || initialHandSize < 0) // 시작 가능 상태 확인
        { // 시작 불가 처리 시작
            return false; // 전투 시작 실패 반환
        } // 시작 불가 처리 종료
        CurrentRound = 1; // 첫 라운드 설정
        openingRelics = RelicRunManager.Instance?.Inventory; // 전투 진입 시 실제 탐사 유물 조회
        ApplyOpeningEnemyStatus(RelicEffectType.OpeningSpeedLoss); // 안테나의 첫 적 속도 약화 적용
        ApplyOpeningEnemyStatus(RelicEffectType.OpeningDroneWeakness); // 드론 약화를 첫 공격 예고 전에 적용
        ApplyOpeningAllySpeed(); // 전투 시작 시 생존 아군 속도 증가
        openingEffects?.Invoke(); // 전투 시작 성공 확인 뒤 첫 행동 전에 탐사 효과 소비
        if (enemyFirst) // 방랑자의 고정 선제 판정 확인
        { // 적의 첫 행동 시작
            openingHandAfterAmbush = initialHandSize; // 첫 아군 턴의 정상 시작 손패 보관
            sharedActionPoints.Spend(sharedActionPoints.CurrentActionPoints); // 적 선제 중 아군 행동력 소모 방지용 비활성화
            CurrentPhase = BattleTurnPhase.EnemyTurn; // 아군 턴을 건너뛰지 않고 적부터 시작
            NotifyPhaseStarted(); // 적 진영 첫 턴 알림
            StateChanged?.Invoke(); // 실제 적 행동 실행
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
        sharedActionPoints.AddTurnCapacity(specimenActionPoints); // 끝나는 턴에 회수할 현재 및 상한 행동력 지급
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
    private void NotifyPhaseStarted() // 진영 시작 효과를 반영한 턴 알림
    { // 시작 효과 적용 시작
        if (CurrentPhase == BattleTurnPhase.EnemyTurn) // 적 행동과 지속 피해 발생 전 확인
        { // 체력 집계 시작
            foreach (var ally in allyUnits) // 아군별 체력 손실 기록 준비
            { // 개별 기록 시작
                ally?.StartEnemyDamageWindow(); // 직전 적 턴 기준점 갱신
            } // 개별 기록 종료
        } // 체력 집계 종료
        else if (CurrentPhase == BattleTurnPhase.PlayerTurn) // 다음 아군 턴 도착 확인
        { // 지연 방어 감소 적용 시작
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
                enemy?.RemoveStatusEffect(BattleStatusEffectType.ChargeDefenseDown); // 이번 턴의 임시 방어 감소 제거
            } // 개별 약화 해제 종료
        } // 돌진 효과 해제 종료
    } // 종료 효과 적용 종료
    internal void RecordCardPayment(CardInstance card, int paid) // 실제 지불에 따른 스프링 예약과 소비
    { // 다음 카드 할인 처리 시작
        if (springReady && card.SourceData.SwordRule != SwordCardRule.Finisher && card.ApCostBeforeNextCardDiscount > 0) // 예약을 쓸 수 있는 유료 고정 카드 확인
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

    internal void BeginCardResolution() // 카드가 손패에 남아 있는 정산 범위 시작
    { // 카드 정산 표시 시작
        resolvingCard = true; // 처치 드로우를 카드 사용 뒤로 미루기
    } // 카드 정산 표시 종료

    internal void EndCardResolution() // 사용 카드가 빠진 뒤 예약 처치 드로우 정산
    { // 카드 정산 완료 처리 시작
        resolvingCard = false; // 독립 처치에 즉시 드로우 허용
        DrawDefeatReward(); // 카드가 떠난 손패 공간을 기준으로 지급
    } // 카드 정산 완료 처리 종료

    private void HandleUnitDefeated(BattleUnitRuntime source, BattleUnitRuntime target) // 아군이 적을 실제로 처치한 경우 확인
    { // 처치 드로우 예약 시작
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
        ClearOpeningAllySpeed(); // 첫 적 턴을 마치지 않은 전투의 배지 상태 회수
        ClearFirstPlayerTurnEnemyStatuses(); // 전투 종료 시의 첫 아군 턴 전용 약화 회수
        ClearReflectionCitadel(); // 승리와 패배 및 도주 이후의 반사 상태 초기화
        nextPlayerTurnActionPointLoss = 0; // 전투 종료 후 미사용 행동력 차감 제거
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
        runtimeUnit.Defeated += HandleUnitDefeated; // 발생자와 대상을 포함한 처치 등록
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
        runtimeUnit.Defeated -= HandleUnitDefeated; // 회전 깃발의 처치 구독 해제
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
        runtimeDeck.CardExcluded -= HandleExcludedCard; // 카드 제외 연결 해제
        disposed = true; // 연결 해제 상태 저장
        ClearOpeningAllySpeed(); // 전투 관리자 해제 시 배지 상태 회수
        ClearFirstPlayerTurnEnemyStatuses(); // 전투 관리자 해제 시 시작 약화 회수
        ClearReflectionCitadel(); // 전투 관리자 해제 시 성채 집계와 임시 상태 정리
        for (int unitIndex = registeredUnits.Count - 1; unitIndex >= 0; unitIndex--) // 등록 유닛 역순 순회
        { // 이벤트 일괄 해제 시작
            UnregisterUnitEvent(registeredUnits[unitIndex]); // 개별 사망 이벤트 해제
        } // 이벤트 일괄 해제 종료
    } // 연결 해제 종료
} // 클래스 종료
