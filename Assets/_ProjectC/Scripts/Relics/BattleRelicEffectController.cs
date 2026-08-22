using System; // 기본 인터페이스 기능 사용
using System.Collections.Generic; // 목록과 사전 자료형 사용
using UnityEngine; // 유니티 로그 기능 사용

public sealed class BattleRelicEffectController : IDisposable // 전투 유물 효과 순차 실행 관리자
{
    private readonly BattleEventDispatcher dispatcher; // 전투 공용 이벤트 발행기
    private readonly RelicInventoryRuntime inventory; // 획득 순서 기반 유물 보관함
    private readonly RelicGoldRuntime goldRuntime; // 임시 골드 지갑
    private readonly IReadOnlyList<BattleUnitRuntime> allyUnits; // 현재 아군 목록
    private readonly IReadOnlyList<BattleUnitRuntime> enemyUnits; // 현재 적 목록
    private readonly List<IDisposable> subscriptions = new List<IDisposable>(); // 전투 이벤트 구독 목록
    private readonly Dictionary<string, int> turnTriggerCounts = new Dictionary<string, int>(); // 현재 턴 유물 발동 횟수
    private readonly Dictionary<string, int> battleTriggerCounts = new Dictionary<string, int>(); // 현재 전투 유물 발동 횟수
    private readonly Queue<KeyValuePair<RelicTriggerType, BattleEventContext>> pendingTriggers = new Queue<KeyValuePair<RelicTriggerType, BattleEventContext>>(); // 중첩 유물 발동 대기열
    private bool processingTriggers; // 현재 유물 순차 처리 여부
    private bool disposed; // 관리자 종료 여부

    public BattleRelicEffectController(BattleEventDispatcher eventDispatcher, RelicInventoryRuntime relicInventory, RelicGoldRuntime wallet, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies) // 전투 유물 관리자 생성
    {
        dispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher)); // 전투 이벤트 발행기 저장
        inventory = relicInventory ?? throw new ArgumentNullException(nameof(relicInventory)); // 유물 보관함 저장
        goldRuntime = wallet ?? throw new ArgumentNullException(nameof(wallet)); // 골드 지갑 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        inventory.RelicAcquired += HandleRelicAcquired; // 전투 중 신규 유물 획득 연결
        RegisterBattleEvents(); // 전투 공용 이벤트 구독
    }

    public void ProcessInitialBattleState(BattleTurnPhase currentPhase, int currentRound) // 연결 전 지나간 첫 전투 상태 보정
    {
        if (disposed) // 관리자 종료 여부 확인
        {
            return; // 초기 상태 처리 중단
        }

        ProcessTrigger(RelicTriggerType.BattleStarted, null); // 획득 순서대로 전투 시작 유물 처리
        turnTriggerCounts.Clear(); // 첫 턴 발동 횟수 초기화
        BattleEventContext turnContext = new BattleEventContext(BattleEventType.TurnStarted, currentRound, currentPhase); // 첫 턴 임시 이벤트 정보 생성
        ProcessTrigger(RelicTriggerType.TurnStarted, turnContext); // 획득 순서대로 첫 턴 유물 처리
    }

    private void RegisterBattleEvents() // 전투 이벤트 전체 구독
    {
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.BattleStarted, HandleBattleEvent)); // 전투 시작 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.TurnStarted, HandleBattleEvent)); // 턴 시작 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.TurnEnded, HandleBattleEvent)); // 턴 종료 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.CardUsed, HandleBattleEvent)); // 카드 사용 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.DamageApplied, HandleBattleEvent)); // 피해 적용 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.HealingApplied, HandleBattleEvent)); // 회복 적용 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.StatusApplied, HandleBattleEvent)); // 상태 적용 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.MentalChanged, HandleBattleEvent)); // 정신력 변화 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.UnitDefeated, HandleBattleEvent)); // 처치 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.BattleEnded, HandleBattleEvent)); // 전투 종료 이벤트 구독
    }

    private void HandleBattleEvent(BattleEventContext eventContext) // 전투 공용 이벤트 처리
    {
        if (disposed || eventContext == null) // 처리 가능 상태 확인
        {
            return; // 이벤트 처리 중단
        }

        RelicTriggerType triggerType = ConvertTrigger(eventContext.EventType); // 전투 이벤트를 유물 발동 시점으로 변환
        if (triggerType == RelicTriggerType.None) // 지원하지 않는 발동 시점 확인
        {
            return; // 유물 처리 중단
        }

        ProcessTrigger(triggerType, eventContext); // 획득 순서대로 일치 유물 처리
    }

    private void HandleRelicAcquired(RelicData relicData, int orderNumber) // 신규 유물 획득 처리
    {
        if (disposed || relicData == null || relicData.TriggerType != RelicTriggerType.OnAcquire) // 즉시 발동 유물 여부 확인
        {
            return; // 획득 효과 처리 중단
        }

        TryExecuteRelic(relicData, orderNumber, null); // 새 유물 즉시 효과 실행
    }

    private void ProcessTrigger(RelicTriggerType triggerType, BattleEventContext eventContext) // 동일 시점 유물 순차 처리
    {
        if (processingTriggers) // 기존 유물 순차 처리 진행 여부 확인
        {
            pendingTriggers.Enqueue(new KeyValuePair<RelicTriggerType, BattleEventContext>(triggerType, eventContext)); // 후속 발동 시점을 대기열에 추가
            return; // 현재 유물 순서 처리 완료까지 대기
        }

        processingTriggers = true; // 유물 순차 처리 시작 상태 저장
        try // 유물 처리 상태 보호
        {
            ExecuteTrigger(triggerType, eventContext); // 현재 발동 시점 처리
            while (pendingTriggers.Count > 0) // 후속 발동 대기열 순회
            {
                KeyValuePair<RelicTriggerType, BattleEventContext> pendingTrigger = pendingTriggers.Dequeue(); // 다음 후속 발동 조회
                ExecuteTrigger(pendingTrigger.Key, pendingTrigger.Value); // 이전 유물 순서 완료 후 후속 시점 처리
            }
        }
        finally // 유물 처리 상태 복구
        {
            processingTriggers = false; // 유물 순차 처리 종료 상태 저장
        }
    }

    private void ExecuteTrigger(RelicTriggerType triggerType, BattleEventContext eventContext) // 단일 발동 시점 실제 처리
    {
        if (triggerType == RelicTriggerType.TurnStarted) // 새 턴 시작 유물 처리 여부 확인
        {
            turnTriggerCounts.Clear(); // 턴당 발동 횟수 초기화
        }

        List<RelicData> orderedSnapshot = new List<RelicData>(inventory.OwnedRelics); // 현재 획득 순서 안전 복사
        foreach (RelicData relicData in orderedSnapshot) // 앞에서부터 유물 순회
        {
            if (relicData == null || relicData.TriggerType != triggerType) // 발동 시점 일치 여부 확인
            {
                continue; // 다음 유물 확인
            }

            int currentOrder = inventory.GetCurrentOrder(relicData.RelicId); // 제거와 순서 변경 반영 현재 순번 조회
            if (currentOrder < 1) // 처리 중 제거된 유물 여부 확인
            {
                continue; // 제거 유물 건너뛰기
            }

            TryExecuteRelic(relicData, currentOrder, eventContext); // 현재 유물 효과 즉시 실행
        }
    }

    private bool TryExecuteRelic(RelicData relicData, int orderNumber, BattleEventContext eventContext) // 단일 유물 실행 시도
    {
        if (!CanTrigger(relicData)) // 발동 횟수 제한 확인
        {
            return false; // 유물 실행 실패 반환
        }

        ApplyEffect(relicData, eventContext); // 유물 효과 즉시 적용
        IncreaseTriggerCount(relicData); // 유물 발동 횟수 기록
        Debug.Log($"[Relic] #{orderNumber} {relicData.DisplayName} 발동 / {relicData.TriggerType} / {relicData.EffectType} {relicData.EffectValue}"); // 유물 발동 순서 로그 출력
        return true; // 유물 실행 성공 반환
    }

    private bool CanTrigger(RelicData relicData) // 유물 발동 제한 검사
    {
        if (relicData == null || !relicData.IsValidData()) // 유물 데이터 유효성 확인
        {
            return false; // 발동 불가 반환
        }

        int turnCount = GetCount(turnTriggerCounts, relicData.RelicId); // 현재 턴 발동 횟수 조회
        if (relicData.MaximumTriggersPerTurn > 0 && turnCount >= relicData.MaximumTriggersPerTurn) // 턴당 제한 도달 여부 확인
        {
            return false; // 턴당 제한으로 발동 불가 반환
        }

        int battleCount = GetCount(battleTriggerCounts, relicData.RelicId); // 현재 전투 발동 횟수 조회
        if (relicData.MaximumTriggersPerBattle > 0 && battleCount >= relicData.MaximumTriggersPerBattle) // 전투당 제한 도달 여부 확인
        {
            return false; // 전투당 제한으로 발동 불가 반환
        }

        return true; // 발동 가능 반환
    }

    private void IncreaseTriggerCount(RelicData relicData) // 유물 발동 횟수 증가
    {
        if (relicData == null || string.IsNullOrWhiteSpace(relicData.RelicId)) // 유물 ID 유효성 확인
        {
            return; // 발동 횟수 기록 중단
        }

        turnTriggerCounts[relicData.RelicId] = GetCount(turnTriggerCounts, relicData.RelicId) + 1; // 현재 턴 발동 횟수 증가
        battleTriggerCounts[relicData.RelicId] = GetCount(battleTriggerCounts, relicData.RelicId) + 1; // 현재 전투 발동 횟수 증가
    }

    private void ApplyEffect(RelicData relicData, BattleEventContext eventContext) // 유물 효과 적용
    {
        if (relicData.EffectType == RelicEffectType.GainGold) // 골드 획득 효과 확인
        {
            goldRuntime.AddGold(relicData.EffectValue); // 유물 효과 골드 지급
            return; // 대상 처리 없이 종료
        }

        List<BattleUnitRuntime> targets = ResolveTargets(relicData.TargetType, eventContext); // 유물 효과 대상 목록 계산
        foreach (BattleUnitRuntime targetUnit in targets) // 대상 유닛 순회
        {
            if (targetUnit == null) // 빈 대상 확인
            {
                continue; // 다음 대상 확인
            }

            ApplyEffectToTarget(relicData, targetUnit); // 대상에게 유물 효과 적용
        }
    }

    private static void ApplyEffectToTarget(RelicData relicData, BattleUnitRuntime targetUnit) // 단일 대상 유물 효과 적용
    {
        switch (relicData.EffectType) // 유물 효과 종류 분기
        {
            case RelicEffectType.IncreaseMaxHealth: // 최대 체력 증가 효과
                targetUnit.ModifyMaxHealth(relicData.EffectValue); // 최대 체력 증가 적용
                break; // 최대 체력 효과 종료
            case RelicEffectType.RestoreHealth: // 체력 회복 효과
                targetUnit.RestoreHealth(relicData.EffectValue); // 현재 체력 회복 적용
                break; // 체력 회복 효과 종료
            case RelicEffectType.DealDamage: // 피해 적용 효과
                targetUnit.TakeDamage(relicData.EffectValue, relicData.DamageType); // 지정 유형 피해 적용
                break; // 피해 효과 종료
            case RelicEffectType.ApplyStatusEffect: // 상태 이상 적용 효과
                targetUnit.ApplyStatusEffect(relicData.StatusEffectType, relicData.EffectValue, relicData.StatusDuration, relicData.StatusMaximumStacks); // 상태 이상 효과 적용
                break; // 상태 이상 효과 종료
        }
    }

    private List<BattleUnitRuntime> ResolveTargets(RelicTargetType targetType, BattleEventContext eventContext) // 유물 대상 목록 계산
    {
        List<BattleUnitRuntime> targets = new List<BattleUnitRuntime>(); // 결과 대상 목록 생성
        switch (targetType) // 대상 종류 분기
        {
            case RelicTargetType.FirstAlly: // 첫 아군 대상
                AddFirstLivingUnit(allyUnits, targets); // 첫 생존 아군 추가
                break; // 첫 아군 처리 종료
            case RelicTargetType.AllAllies: // 모든 아군 대상
                AddUnits(allyUnits, targets); // 전체 아군 추가
                break; // 전체 아군 처리 종료
            case RelicTargetType.FirstEnemy: // 첫 적 대상
                AddFirstLivingUnit(enemyUnits, targets); // 첫 생존 적 추가
                break; // 첫 적 처리 종료
            case RelicTargetType.AllEnemies: // 모든 적 대상
                AddUnits(enemyUnits, targets); // 전체 적 추가
                break; // 전체 적 처리 종료
            case RelicTargetType.EventSource: // 이벤트 발생자 대상
                AddUnit(eventContext == null ? null : eventContext.SourceUnit, targets); // 이벤트 발생자 추가
                break; // 이벤트 발생자 처리 종료
            case RelicTargetType.EventTarget: // 이벤트 대표 대상
                AddUnit(eventContext == null ? null : eventContext.TargetUnit, targets); // 이벤트 대표 대상 추가
                break; // 이벤트 대상 처리 종료
        }
        return targets; // 계산된 대상 목록 반환
    }

    private static void AddFirstLivingUnit(IReadOnlyList<BattleUnitRuntime> sourceUnits, List<BattleUnitRuntime> targets) // 첫 생존 유닛 추가
    {
        foreach (BattleUnitRuntime runtimeUnit in sourceUnits) // 원본 유닛 순회
        {
            if (runtimeUnit == null || runtimeUnit.IsDead) // 생존 여부 확인
            {
                continue; // 다음 유닛 확인
            }

            targets.Add(runtimeUnit); // 첫 생존 유닛 추가
            return; // 첫 대상 추가 후 종료
        }
    }

    private static void AddUnits(IReadOnlyList<BattleUnitRuntime> sourceUnits, List<BattleUnitRuntime> targets) // 전체 유닛 추가
    {
        foreach (BattleUnitRuntime runtimeUnit in sourceUnits) // 원본 유닛 순회
        {
            AddUnit(runtimeUnit, targets); // 유효 유닛 대상 목록 추가
        }
    }

    private static void AddUnit(BattleUnitRuntime runtimeUnit, List<BattleUnitRuntime> targets) // 단일 유닛 추가
    {
        if (runtimeUnit == null) // 유닛 존재 여부 확인
        {
            return; // 대상 추가 중단
        }

        targets.Add(runtimeUnit); // 대상 목록에 유닛 추가
    }

    private static int GetCount(Dictionary<string, int> counts, string relicId) // 발동 횟수 조회
    {
        if (string.IsNullOrWhiteSpace(relicId)) // 유물 ID 유효성 확인
        {
            return 0; // 발동 횟수 영 반환
        }

        return counts.TryGetValue(relicId, out int count) ? count : 0; // 저장된 횟수 또는 영 반환
    }

    private static RelicTriggerType ConvertTrigger(BattleEventType eventType) // 전투 이벤트를 유물 발동 시점으로 변환
    {
        switch (eventType) // 전투 이벤트 종류 분기
        {
            case BattleEventType.BattleStarted: // 전투 시작 이벤트
                return RelicTriggerType.BattleStarted; // 전투 시작 유물 시점 반환
            case BattleEventType.TurnStarted: // 턴 시작 이벤트
                return RelicTriggerType.TurnStarted; // 턴 시작 유물 시점 반환
            case BattleEventType.TurnEnded: // 턴 종료 이벤트
                return RelicTriggerType.TurnEnded; // 턴 종료 유물 시점 반환
            case BattleEventType.CardUsed: // 카드 사용 이벤트
                return RelicTriggerType.CardUsed; // 카드 사용 유물 시점 반환
            case BattleEventType.DamageApplied: // 피해 적용 이벤트
                return RelicTriggerType.DamageApplied; // 피해 유물 시점 반환
            case BattleEventType.HealingApplied: // 회복 적용 이벤트
                return RelicTriggerType.HealingApplied; // 회복 유물 시점 반환
            case BattleEventType.StatusApplied: // 상태 적용 이벤트
                return RelicTriggerType.StatusApplied; // 상태 유물 시점 반환
            case BattleEventType.MentalChanged: // 정신력 변화 이벤트
                return RelicTriggerType.MentalChanged; // 정신력 유물 시점 반환
            case BattleEventType.UnitDefeated: // 처치 이벤트
                return RelicTriggerType.UnitDefeated; // 처치 유물 시점 반환
            case BattleEventType.BattleEnded: // 전투 종료 이벤트
                return RelicTriggerType.BattleEnded; // 전투 종료 유물 시점 반환
            default: // 지원하지 않는 이벤트
                return RelicTriggerType.None; // 발동 없음 반환
        }
    }

    public void Dispose() // 유물 효과 관리자 연결 해제
    {
        if (disposed) // 기존 종료 여부 확인
        {
            return; // 중복 종료 중단
        }

        disposed = true; // 관리자 종료 상태 저장
        inventory.RelicAcquired -= HandleRelicAcquired; // 신규 유물 획득 연결 해제
        foreach (IDisposable subscription in subscriptions) // 전투 이벤트 구독 순회
        {
            subscription?.Dispose(); // 개별 이벤트 구독 해제
        }
        subscriptions.Clear(); // 구독 목록 초기화
        turnTriggerCounts.Clear(); // 턴 발동 기록 초기화
        battleTriggerCounts.Clear(); // 전투 발동 기록 초기화
        pendingTriggers.Clear(); // 후속 발동 대기열 초기화
    }
}
