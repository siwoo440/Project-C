using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleEventController : IDisposable // 기존 전투 신호 공용 이벤트 중계기
{ // 클래스 시작
    private readonly BattleEventDispatcher dispatcher; // 공용 이벤트 발행기
    private readonly BattleTurnRuntime turnRuntime; // 전투 턴 관리자
    private readonly BattleCardActionController cardActionController; // 카드 행동 관리자
    private readonly List<BattleUnitRuntime> registeredUnits = new List<BattleUnitRuntime>(); // 이벤트 등록 유닛 목록
    private bool battleStartedPublished; // 전투 시작 발행 여부
    private bool battleEndedPublished; // 전투 종료 발행 여부
    private bool disposed; // 중계기 종료 여부
    public BattleEventController(BattleEventDispatcher eventDispatcher, BattleTurnRuntime battleTurn, BattleCardActionController cardController, IReadOnlyList<BattleUnitRuntime> allyUnits, IReadOnlyList<BattleUnitRuntime> enemyUnits) // 공용 이벤트 중계기 생성자
    { // 생성자 시작
        dispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher)); // 공용 이벤트 발행기 저장
        turnRuntime = battleTurn ?? throw new ArgumentNullException(nameof(battleTurn)); // 전투 턴 관리자 저장
        cardActionController = cardController ?? throw new ArgumentNullException(nameof(cardController)); // 카드 행동 관리자 저장
        turnRuntime.PhaseStarted += HandlePhaseStarted; // 턴 시작 신호 등록
        turnRuntime.PhaseEnded += HandlePhaseEnded; // 턴 종료 신호 등록
        turnRuntime.BattleCompleted += HandleBattleCompleted; // 전투 종료 완료 신호 등록
        cardActionController.CardUsed += HandleCardUsed; // 카드 효과 적용 시작 신호 등록
        RegisterUnits(allyUnits); // 아군 상세 신호 등록
        RegisterUnits(enemyUnits); // 적 상세 신호 등록
    } // 생성자 종료
    public bool RegisterSummonedEnemy(BattleUnitRuntime enemyUnit) // 소환 적 이벤트 등록
    { // 소환 적 등록 시작
        if (disposed || enemyUnit == null || enemyUnit.Team != BattleTeam.Enemy) // 등록 조건 확인
        { // 등록 불가 처리 시작
            return false; // 소환 적 등록 실패 반환
        } // 등록 불가 처리 종료
        return RegisterUnit(enemyUnit); // 개별 유닛 등록 결과 반환
    } // 소환 적 등록 종료
    public bool UnregisterEnemy(BattleUnitRuntime enemyUnit) // 제거 적 이벤트 해제
    { // 제거 적 해제 시작
        return UnregisterUnit(enemyUnit); // 개별 유닛 해제 결과 반환
    } // 제거 적 해제 종료
    private void HandlePhaseStarted(BattleTurnPhase phase, int round) // 진영 턴 시작 신호 처리
    { // 턴 시작 신호 처리 시작
        if (!battleStartedPublished) // 전투 시작 발행 여부 확인
        { // 전투 시작 발행 시작
            battleStartedPublished = true; // 전투 시작 발행 상태 저장
            dispatcher.Publish(new BattleEventContext(BattleEventType.BattleStarted, round, phase)); // 전투 시작 공용 이벤트 발행
        } // 전투 시작 발행 종료
        dispatcher.Publish(new BattleEventContext(BattleEventType.TurnStarted, round, phase)); // 진영 턴 시작 공용 이벤트 발행
    } // 턴 시작 신호 처리 종료
    private void HandlePhaseEnded(BattleTurnPhase phase, int round) // 진영 턴 종료 신호 처리
    { // 턴 종료 신호 처리 시작
        dispatcher.Publish(new BattleEventContext(BattleEventType.TurnEnded, round, phase)); // 진영 턴 종료 공용 이벤트 발행
    } // 턴 종료 신호 처리 종료
    private void HandleBattleCompleted(BattleResult result) // 전투 종료 완료 신호 처리
    { // 전투 종료 처리 시작
        if (battleEndedPublished) // 전투 종료 중복 여부 확인
        { // 발행 불필요 처리 시작
            return; // 전투 종료 발행 중단
        } // 발행 불필요 처리 종료
        battleEndedPublished = true; // 전투 종료 발행 상태 저장
        dispatcher.Publish(new BattleEventContext(BattleEventType.BattleEnded, turnRuntime.CurrentRound, turnRuntime.CurrentPhase, result: result)); // 전투 종료 공용 이벤트 발행
    } // 전투 상태 처리 종료
    private void HandleCardUsed(CardInstance card, IReadOnlyList<BattleUnitRuntime> targets) // 카드 사용 시작 신호 처리
    { // 카드 사용 신호 처리 시작
        BattleUnitRuntime targetUnit = targets == null || targets.Count < 1 ? null : targets[0]; // 대표 대상 조회
        dispatcher.Publish(new BattleEventContext(BattleEventType.CardUsed, turnRuntime.CurrentRound, turnRuntime.CurrentPhase, card == null ? null : card.OwnerUnit, targetUnit, targets, card)); // 카드 사용 공용 이벤트 발행
    } // 카드 사용 신호 처리 종료
    private void HandleDamageResolved(BattleUnitRuntime sourceUnit, BattleUnitRuntime targetUnit, BattleDamageResult damageResult) // 피해 완료 신호 처리
    { // 피해 신호 처리 시작
        int appliedAmount = damageResult.AppliedDamage; // 실제 피해량 조회
        dispatcher.Publish(new BattleEventContext(BattleEventType.DamageApplied, turnRuntime.CurrentRound, turnRuntime.CurrentPhase, sourceUnit, targetUnit, appliedAmount: appliedAmount, damageResult: damageResult)); // 피해 적용 공용 이벤트 발행
    } // 피해 신호 처리 종료
    private void HandleHealingResolved(BattleUnitRuntime sourceUnit, BattleUnitRuntime targetUnit, int appliedHealing) // 회복 완료 신호 처리
    { // 회복 신호 처리 시작
        dispatcher.Publish(new BattleEventContext(BattleEventType.HealingApplied, turnRuntime.CurrentRound, turnRuntime.CurrentPhase, sourceUnit, targetUnit, appliedAmount: appliedHealing)); // 회복 적용 공용 이벤트 발행
    } // 회복 신호 처리 종료
    private void HandleStatusResolved(BattleUnitRuntime sourceUnit, BattleUnitRuntime targetUnit, BattleStatusEffectType effectType, BattleStatusEffectApplyResult applyResult) // 상태 적용 신호 처리
    { // 상태 신호 처리 시작
        dispatcher.Publish(new BattleEventContext(BattleEventType.StatusApplied, turnRuntime.CurrentRound, turnRuntime.CurrentPhase, sourceUnit, targetUnit, statusEffectType: effectType, statusApplyResult: applyResult)); // 상태 적용 공용 이벤트 발행
    } // 상태 신호 처리 종료
    private void HandleMentalChanged(BattleUnitRuntime targetUnit, BattleMentalChangeResult mentalResult) // 정신력 변화 신호 처리
    { // 정신력 신호 처리 시작
        int appliedAmount = mentalResult == null ? 0 : mentalResult.AppliedDelta; // 실제 정신력 변화량 조회
        dispatcher.Publish(new BattleEventContext(BattleEventType.MentalChanged, turnRuntime.CurrentRound, turnRuntime.CurrentPhase, targetUnit: targetUnit, appliedAmount: appliedAmount, mentalResult: mentalResult)); // 정신력 변화 공용 이벤트 발행
    } // 정신력 신호 처리 종료
    private void HandleUnitDefeated(BattleUnitRuntime sourceUnit, BattleUnitRuntime defeatedUnit) // 유닛 처치 신호 처리
    { // 처치 신호 처리 시작
        dispatcher.Publish(new BattleEventContext(BattleEventType.UnitDefeated, turnRuntime.CurrentRound, turnRuntime.CurrentPhase, sourceUnit, defeatedUnit)); // 유닛 처치 공용 이벤트 발행
    } // 처치 신호 처리 종료
    private void RegisterUnits(IReadOnlyList<BattleUnitRuntime> units) // 유닛 목록 이벤트 등록
    { // 목록 등록 시작
        if (units == null) // 유닛 목록 존재 확인
        { // 목록 없음 처리 시작
            return; // 목록 등록 중단
        } // 목록 없음 처리 종료
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 개별 유닛 등록 시작
            RegisterUnit(runtimeUnit); // 개별 상세 신호 등록
        } // 개별 유닛 등록 종료
    } // 목록 등록 종료
    private bool RegisterUnit(BattleUnitRuntime runtimeUnit) // 개별 유닛 이벤트 등록
    { // 개별 등록 시작
        if (runtimeUnit == null || registeredUnits.Contains(runtimeUnit)) // 유닛 존재와 중복 확인
        { // 등록 불가 처리 시작
            return false; // 개별 등록 실패 반환
        } // 등록 불가 처리 종료
        runtimeUnit.DamageResolved += HandleDamageResolved; // 피해 완료 신호 등록
        runtimeUnit.HealingResolved += HandleHealingResolved; // 회복 완료 신호 등록
        runtimeUnit.StatusEffectResolved += HandleStatusResolved; // 상태 적용 신호 등록
        runtimeUnit.MentalChanged += HandleMentalChanged; // 정신력 변화 신호 등록
        runtimeUnit.Defeated += HandleUnitDefeated; // 처치 완료 신호 등록
        registeredUnits.Add(runtimeUnit); // 등록 유닛 목록 추가
        return true; // 개별 등록 성공 반환
    } // 개별 등록 종료
    private bool UnregisterUnit(BattleUnitRuntime runtimeUnit) // 개별 유닛 이벤트 해제
    { // 개별 해제 시작
        if (runtimeUnit == null || !registeredUnits.Remove(runtimeUnit)) // 등록 유닛 존재 확인
        { // 해제 불가 처리 시작
            return false; // 개별 해제 실패 반환
        } // 해제 불가 처리 종료
        runtimeUnit.DamageResolved -= HandleDamageResolved; // 피해 완료 신호 해제
        runtimeUnit.HealingResolved -= HandleHealingResolved; // 회복 완료 신호 해제
        runtimeUnit.StatusEffectResolved -= HandleStatusResolved; // 상태 적용 신호 해제
        runtimeUnit.MentalChanged -= HandleMentalChanged; // 정신력 변화 신호 해제
        runtimeUnit.Defeated -= HandleUnitDefeated; // 처치 완료 신호 해제
        return true; // 개별 해제 성공 반환
    } // 개별 해제 종료
    public void Dispose() // 공용 이벤트 중계기 종료
    { // 중계기 종료 시작
        if (disposed) // 기존 종료 확인
        { // 중복 종료 처리 시작
            return; // 중계기 종료 중단
        } // 중복 종료 처리 종료
        disposed = true; // 중계기 종료 상태 저장
        turnRuntime.PhaseStarted -= HandlePhaseStarted; // 턴 시작 신호 해제
        turnRuntime.PhaseEnded -= HandlePhaseEnded; // 턴 종료 신호 해제
        turnRuntime.BattleCompleted -= HandleBattleCompleted; // 전투 종료 완료 신호 해제
        cardActionController.CardUsed -= HandleCardUsed; // 카드 효과 적용 시작 신호 해제
        for (int unitIndex = registeredUnits.Count - 1; unitIndex >= 0; unitIndex--) // 등록 유닛 역순 순회
        { // 등록 유닛 해제 시작
            UnregisterUnit(registeredUnits[unitIndex]); // 개별 유닛 신호 해제
        } // 등록 유닛 해제 종료
    } // 중계기 종료 종료
} // 클래스 종료
