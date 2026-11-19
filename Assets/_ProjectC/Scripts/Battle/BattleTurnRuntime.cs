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
    private int lastVolatileDrawRound; // 안전핀의 마지막 발동 라운드
    private int lastExcludedBlockRound; // 봉인왁스의 마지막 발동 라운드
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
        ApplyOpeningSpeedLoss(); // 적 예고의 첫 속도 추첨 전 적용
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
        runtimeDeck.DiscardHand(); // 남은 일반 손패를 버림 더미로 이동
        runtimeDeck.SetRelicApDiscount(0); // 끝난 아군 턴의 유물 할인 해제
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
    private void ApplyOpeningSpeedLoss() // 전투 시작 적 행동 속도 약화
    { // 안테나 발동 시작
        var antenna = openingRelics?.FindEffect(RelicEffectType.OpeningSpeedLoss); // 보유한 안테나 조회
        if (antenna == null) // 유물 미보유 확인
        { // 미발동 처리 시작
            return; // 속도 약화 생략
        } // 미발동 처리 종료
        var living = new List<BattleUnitRuntime>(); // 약화 대상 후보
        foreach (var enemy in enemyUnits) // 전투 배치의 적 순회
        { // 적별 생존 여부 확인
            if (enemy != null && !enemy.IsDead) // 생존 적만 후보 포함
            { // 후보 추가 시작
                living.Add(enemy); // 실제 생존 적 등록
            } // 후보 추가 종료
        } // 적별 생존 여부 확인 종료
        if (living.Count > 0) // 대상 존재 확인
        { // 하나의 적에 약화 적용
            var target = living[UnityEngine.Random.Range(0, living.Count)]; // 생존 적 하나 무작위 선택
            target.ApplyStatusEffect(antenna.StatusEffectType, antenna.EffectValue, antenna.StatusDuration, antenna.StatusMaximumStacks); // 기존 면역과 지속 시간 적용
        } // 약화 적용 종료
    } // 안테나 발동 종료
    private void DrawPlayerHand(int count) // 아군 턴의 기본과 유물 드로우 적용
    { // 턴 손패 처리 시작
        var pouch = openingRelics?.FindEffect(RelicEffectType.LargeDiscardDraw); // 보유한 잔해 주머니 조회
        if (pouch != null && runtimeDeck.DiscardPileCount >= 10) // 재섞기 전에 버린 더미 확인
        { // 추가 드로우 준비 시작
            count += pouch.EffectValue; // 이번 턴의 드로우 한 장 증가
        } // 추가 드로우 준비 종료
        LastDrawnCardCount = runtimeDeck.DrawForPlayerTurn(count); // 기존 손패 상한과 예약 드로우 적용
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
    } // 턴 손패 처리 종료
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
        runtimeDeck.CardExcluded -= HandleExcludedCard; // 카드 제외 연결 해제
        disposed = true; // 연결 해제 상태 저장
        for (int unitIndex = registeredUnits.Count - 1; unitIndex >= 0; unitIndex--) // 등록 유닛 역순 순회
        { // 이벤트 일괄 해제 시작
            UnregisterUnitEvent(registeredUnits[unitIndex]); // 개별 사망 이벤트 해제
        } // 이벤트 일괄 해제 종료
    } // 연결 해제 종료
} // 클래스 종료
