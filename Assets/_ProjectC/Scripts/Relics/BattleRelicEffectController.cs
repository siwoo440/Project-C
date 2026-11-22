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
    private readonly BattleDeckRuntime battleDeck; // 유물 효과 연결 전투 덱
    private readonly BattleActionPointRuntime actionPoints; // 처치 유물이 회복할 공용 행동력
    private readonly Func<int, bool> eventRelicDraw; // 사용 및 추가 선택과 양립하는 선택적 유물 드로우 연결
    private readonly List<IDisposable> subscriptions = new List<IDisposable>(); // 전투 이벤트 구독 목록
    private readonly Dictionary<string, int> turnTriggerCounts = new Dictionary<string, int>(); // 현재 턴 유물 발동 횟수
    private readonly Dictionary<string, int> battleTriggerCounts = new Dictionary<string, int>(); // 현재 전투 유물 발동 횟수
    private readonly Queue<KeyValuePair<RelicTriggerType, BattleEventContext>> pendingTriggers = new Queue<KeyValuePair<RelicTriggerType, BattleEventContext>>(); // 중첩 유물 발동 대기열
    private bool journalVictoryChecked; // 한 전투 승리에서 이미 시도한 일지 추첨
    private bool endCleanseChecked; // 실제 전투 종료에서 이미 시도한 손수건 추첨
    private bool startCleanseChecked; // 전투 시작의 이미 사용한 향 주머니 판정
    private const int DebuffApplicationsPerHealing = 5; // 공허의 잔뿌리 발동에 필요한 실제 적 약화 횟수
    private int enemyDebuffApplications; // 현재 전투에서 다섯 회 단위로 쌓이는 적 약화 기록
    private bool processingTriggers; // 현재 유물 순차 처리 여부
    private bool disposed; // 관리자 종료 여부

    public BattleRelicEffectController(BattleEventDispatcher eventDispatcher, RelicInventoryRuntime relicInventory, RelicGoldRuntime wallet, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<BattleUnitRuntime> enemies, BattleDeckRuntime deck = null, BattleActionPointRuntime points = null, Func<int, bool> eventRelicDraw = null) // 안전한 추가 드로우를 포함한 전투 유물 관리자 생성
    {
        dispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher)); // 전투 이벤트 발행기 저장
        inventory = relicInventory ?? throw new ArgumentNullException(nameof(relicInventory)); // 유물 보관함 저장
        goldRuntime = wallet ?? throw new ArgumentNullException(nameof(wallet)); // 골드 지갑 저장
        allyUnits = allies ?? throw new ArgumentNullException(nameof(allies)); // 아군 목록 저장
        enemyUnits = enemies ?? throw new ArgumentNullException(nameof(enemies)); // 적 목록 저장
        battleDeck = deck; // 실제 전투 덱 저장
        actionPoints = points; // 실제 공용 행동력 연결
        this.eventRelicDraw = eventRelicDraw; // 카드 해결과 선택 뒤의 상태 유물 드로우 연결
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
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.BeforeHandDiscard, HandleBattleEvent)); // 손패 버림 직전 이벤트 구독
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.DecisionResolved, HandleBattleEvent)); // 선택 카드의 결정 추가 효과 성공 구독
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
            bool victory = triggerType == RelicTriggerType.BattleEnded && eventContext?.Result == BattleResult.Victory; // 도주와 패배를 제외한 승리 확인
            if (relicData == null || relicData.TriggerType != triggerType && !(victory && relicData.TriggerType == RelicTriggerType.BattleVictory)) // 같은 시점 또는 승리 전용 유물 선택
            {
                continue; // 다음 유물 확인
            }
            if (relicData.EffectType == RelicEffectType.GainActionPoints && (eventContext?.Phase != BattleTurnPhase.PlayerTurn || eventContext.SourceUnit?.Team != BattleTeam.Ally || eventContext.TargetUnit?.Team != BattleTeam.Enemy)) // 아군 턴의 아군 직접 처치 확인
            { // 무관한 처치 제외 시작
                continue; // 턴당 발동 횟수 보존
            } // 무관한 처치 제외 종료
            if (relicData.EffectType is RelicEffectType.FirstDefensiveCardDiscount or RelicEffectType.CorrosionDebuffDamage or RelicEffectType.MarkPhysicalDefenseLoss) // 실제 지불과 명시 상태 경로가 처리하는 유물 확인
            { // 일반 사용 이벤트 제외 시작
                continue; // 카드나 상태 성공을 확인하지 못하는 공용 신호의 가짜 발동 방지
            } // 일반 사용 이벤트 제외 종료
            int interval = relicData.PlayerTurnInterval; // 유물이 요구하는 아군 턴 간격 조회
            if (interval > 0 && (eventContext == null || eventContext.Phase != BattleTurnPhase.PlayerTurn || eventContext.Round <= 0 || eventContext.Round % interval != 0)) // 지정 라운드의 아군 턴 여부 확인
            { // 발동 주기 확인 시작
                continue; // 해당 아군 턴까지 회복 대기
            } // 발동 주기 확인 종료

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
        if (relicData.EffectType is RelicEffectType.RockfallWarningPlaqueCanyonTrapReduction or RelicEffectType.ObsidianBladeShardSwordBleed or // 실제 탐사 함정과 검 직접 명중 유물
            RelicEffectType.FlameVeinDebrisBurnResistance or RelicEffectType.AshDustStatueEyeEnemyAccuracyDown or // 상태 실제 부여와 공용 명중 계산 유물
            RelicEffectType.StoneThornBundleRetaliation or RelicEffectType.CanyonWindWhistleOpeningEvasion or // 실제 직접 피격과 전투 개시 유물
            RelicEffectType.WetReedKnotPoisonDamageReduction or RelicEffectType.BlackSwampWaterBottlePoisonChance) // 실제 중독 피해와 카드 부여 확률 유물
        { // 전용 효과를 공용 이벤트에서 건너뛰는 분기 시작
            return false; // 실제 판정 한 번과 유물 발동 기록 보존
        } // 전용 효과를 공용 이벤트에서 건너뛰는 분기 종료

        if (relicData.EffectType == RelicEffectType.BattleStartCleanseOne) // 전투 시작 향 주머니의 단일 판정 확인
        { // 시작 정화 횟수 확인 시작
            if (startCleanseChecked) // 시작 이벤트 또는 기존 보정 경로의 반복 여부 확인
            { // 반복된 전투 시작 처리 시작
                return false; // 같은 전투에서 향 주머니 확률 재시도 차단
            } // 반복된 전투 시작 처리 종료
            startCleanseChecked = true; // 실제 판정에서 성공과 실패 및 대상 없음 모두 기록
        } // 시작 정화 횟수 확인 종료
        if (relicData.EffectType is (RelicEffectType.GlassSandLensMagicVsBlind or RelicEffectType.AmbushBugAntennaFirstTurnFirstAttackDamage or RelicEffectType.RuinedLeatherStrapLowHealthEnemyDamage)) // 실제 공동 직접 피해 경로가 정산하는 사막 유물 확인
        { // 일반 피해 유물 중복 제외 시작
            return false; // 별도 확률과 피해 재정산 없는 전용 효과 유지
        } // 일반 피해 유물 중복 제외 종료
        if (relicData.EffectType is (RelicEffectType.DryWaterskinBattleEndHealing or RelicEffectType.SandBoneFirstBleedingEnemyDamage or RelicEffectType.BlackSandBlindChanceBonus or RelicEffectType.DesertKingBraceletOpeningWeakness)) // 실제 저장과 명중 및 확률과 전투 시작 경로의 전용 유물
        { // 별도 전투 경로 유물의 일반 처리 제외 시작
            return false; // 기본 확률과 대상 처리를 거치지 않고 원래 전용 정산 유지
        } // 별도 전투 경로 유물의 일반 처리 제외 종료
        if (relicData.EffectType is RelicEffectType.GainOnHealthLoss or RelicEffectType.AbyssGrailAllyRevive or RelicEffectType.PortraitNailMarkedBleedDamage or RelicEffectType.SilverSnufferBurnKillHealing or RelicEffectType.OperaMaskAfraidKillMentalLoss or RelicEffectType.BallroomLastBeatSecondEvade) // 실제 체력과 처치 및 두 번째 회피의 전용 유물 정산 확인
        { // 공용 발동에서 전용 유물 제외 시작
            return false; // 상태 알림에서 행동력과 부활을 중복 처리하지 않는 반환
        } // 공용 발동에서 전용 유물 제외 종료
        if (relicData.EffectType is RelicEffectType.MistFlowerPollenOpeningBlind or RelicEffectType.LavaVeinGuardianCoreBurningEnemiesActionPoints or RelicEffectType.RedCanyonBulwarkStoneOpeningBlockDrawTrade or // 실제 교전 개막과 아군 차례에서 직접 처리하는 효과
            RelicEffectType.LeechFangNecklaceSingleTargetHealing or RelicEffectType.MudCharmActionPointLossBlock or RelicEffectType.ObsidianExecutionMaskBleedingDefeatDamage) // 실제 명중과 행동력 감소 및 전투불능에서 직접 처리하는 효과
        { // 직접 처리한 개발 일차 유물의 공용 발동 제외 시작
            return false; // 공용 난수와 거짓 발동 기록의 중복 차단
        } // 직접 처리한 개발 일차 유물의 공용 발동 제외 종료
        if (relicData.EffectType == RelicEffectType.PeriodicEnemyMentalLoss) // 실제 아군 턴 런타임이 이미 처리한 먼지시계 확인
        { // 공용 발동 제외 시작
            return false; // 상태 알림과 실제 감소의 중복 처리 방지
        } // 공용 발동 제외 종료
        if (relicData.EffectType == RelicEffectType.BattleEndCleanseOne) // 순백 손수건의 실제 종료 추첨 확인
        { // 전투 종료 정화 판정 시작
            if (endCleanseChecked || eventContext == null || eventContext.Result == BattleResult.None) // 유효한 실제 종료와 한 번의 판정 확인
            { // 잘못된 정화 판정 시작
                return false; // 같은 결과 또는 결과 없는 신호의 무작위 소비 차단
            } // 잘못된 정화 판정 종료
            endCleanseChecked = true; // 성공과 실패 모두 한 전투의 추첨 기회 기록
        } // 전투 종료 정화 판정 종료
        if (relicData.EffectType is RelicEffectType.CleanseReceivedDebuff or RelicEffectType.FearGrantedEvasionLoss or RelicEffectType.BleedApplicationChanceBonus or RelicEffectType.ReflectFirstAllyDebuff or RelicEffectType.LaughMirrorDebuffReflection or RelicEffectType.MirrorDukeCrownDebuffReflection) // 실제 상태와 카드 확률 및 세 거울의 전용 판정 확인
        { // 공용 상태 유물 중복 정산 차단 시작
            return false; // 관련 없는 상태 이벤트와 별개 공용 추첨의 난수 소모 방지
        } // 공용 상태 유물 중복 정산 차단 종료
        if (relicData.EffectType == RelicEffectType.SutureCorrosionHealing) // 전투 결과 생성 시 적용하는 전용 회복 유물
        { // 일반 실행 제외 시작
            return false; // 일반 유물 실행과 성공 로그 생략
        } // 일반 실행 제외 끝

        if (relicData.EffectType is RelicEffectType.DriedCactusSapPoisonDamageBonus or RelicEffectType.MineBlackSandPoisonOnDefenseDown or RelicEffectType.AshstormAshPouchBurnOnDefeat) // 상태 원본의 실제 변화와 특정 지속 피해를 직접 확인하는 유물
        { // 실제 효과의 공용 신호 재처리 제외 시작
            return false; // 원래 부여 여부나 사망 수치와 별개인 두 번째 확률과 발동 금지
        } // 실제 효과의 공용 신호 재처리 제외 종료
        if (relicData.EffectType is RelicEffectType.ManorWickFortressOpeningBlock or RelicEffectType.DesertGlassBraceletBlindResistance) // 전투 원본 지역과 적 실명 경로가 직접 처리하는 유물 확인
        { // 유효한 지역이 있는 전용 효과의 공용 중복 처리 제외 시작
            return false; // 별도 공용 효과나 발동 횟수 없이 실제 발생점에서만 처리
        } // 유효한 지역이 있는 전용 효과의 공용 중복 처리 제외 종료
        if (relicData.EffectType is RelicEffectType.LordsFinalOrderAttackThenDefenseLoss or RelicEffectType.BanquetHeartPoisonCountHealing or RelicEffectType.ChandelierThirdTurnPulse) // 실제 턴 처리기의 명령서 및 두 시작 유물 확인
        { // 공용 시작 이벤트의 중복 발동 차단 시작
            return false; // 별도 횟수와 난수 없이 실제 아군과 적 턴에서 처리
        } // 공용 시작 이벤트의 중복 발동 차단 종료
        if (relicData.EffectType is RelicEffectType.LordsWaxOpeningEnemyWeakness or RelicEffectType.BanquetOilPoisonPulse or RelicEffectType.BlackCutleryBleedStackDraw) // 실제 전투 턴 처리기의 전용 효과 확인
        { // 같은 시작 또는 상태 신호의 공용 중복 처리 차단 시작
            return false; // 별도 난수와 중복 지급 없이 실제 전투 턴에서만 정산
        } // 같은 시작 또는 상태 신호의 공용 중복 처리 차단 종료
        if (relicData.EffectType == RelicEffectType.PortraitEyeOpeningFear) // 실제 전투 진입에서 한 번만 판정하는 초상화 유물
        { // 일반 유물 확률 경로의 중복 추첨 차단 시작
            return false; // 개별 공용 신호에서 또 다른 난수 사용 방지
        } // 일반 유물 확률 경로의 중복 추첨 차단 종료
        if (relicData.EffectType == RelicEffectType.JournalCardRemoval) // 승리 시 일지의 확률 판정 확인
        { // 승리 추첨 시작
            if (journalVictoryChecked || eventContext?.Result != BattleResult.Victory) // 도주와 패배 및 같은 승리의 중복 신호 확인
            { // 중복 추첨 차단 시작
                return false; // 회차 덱과 일지 보존
            } // 중복 추첨 차단 종료
            journalVictoryChecked = true; // 성공과 실패 모두 이번 전투의 기회 기록
        } // 승리 추첨 종료
        if (relicData.EffectType == RelicEffectType.EnemyBleedDamageBonus) // 적 출혈 종료 처리기가 직접 적용하는 붉은 균열섬유 확인
        { // 턴 종료 신호의 중복 처리 차단 시작
            return false; // 일반 유물 경로의 중복 수치와 표시 없이 상태 처리기에 위임
        } // 턴 종료 신호의 중복 처리 차단 종료
        if (relicData.EffectType == RelicEffectType.MagicalHitMentalLoss && // 검은 별가루의 피해 이벤트 확인
            (eventContext?.SourceUnit?.Team != BattleTeam.Ally || eventContext.TargetUnit?.Team != BattleTeam.Enemy || eventContext.TargetUnit.IsDead || // 확률 소비 전 실제 아군 출처와 살아 있는 적 대상 확인
             eventContext.DamageResult is not { DamageType: BattleDamageType.Magical, AppliedDamage: > 0, IsWandEcho: false })) // 실제 원본 마법 피해에만 추가 정신력 추첨 허용
        { // 별가루와 무관한 피해 제외 시작
            return false; // 전투의 기존 무작위 판정과 발동 기록 보존
        } // 별가루와 무관한 피해 제외 종료
        if (!CanTrigger(relicData)) // 발동 횟수 제한 확인
        {
            return false; // 유물 실행 실패 반환
        }
        if (!MatchesEventRelicTrigger(relicData.EffectType, eventContext)) // 세 공용 신호 유물의 실제 대상과 출처 및 효과 검사
        { // 무관한 공용 신호 제외 시작
            return false; // 실제 발동 대상이 없을 때 난수와 발동 제한 보존
        } // 무관한 공용 신호 제외 종료
        if (relicData.TriggerChancePercent <= 0 || (relicData.TriggerChancePercent < 100 && UnityEngine.Random.Range(0, 100) >= relicData.TriggerChancePercent)) // 확률형 유물 발동 판정
        { // 확률 발동 실패 시작
            if (relicData.TriggerType == RelicTriggerType.BattleStarted && relicData.MaximumTriggersPerBattle > 0) // 전투 시작 단회 판정 확인
            { // 시작 판정 기록 시작
                IncreaseTriggerCount(relicData); // 실패한 시작 판정의 재시도 방지
            } // 시작 판정 기록 종료
            return false; // 확률 발동 실패 반환
        } // 확률 발동 실패 종료

        if (!TryApplyTriggeredEffect(relicData, eventContext)) // 손패 빈자리와 턴 조건을 포함한 실제 발동 확인
        { // 유물 발동 보류 시작
            return false; // 유효한 다음 기회까지 횟수 보존
        } // 유물 발동 보류 종료
        IncreaseTriggerCount(relicData); // 유물 발동 횟수 기록
        Debug.Log($"[Relic] #{orderNumber} {relicData.DisplayName} 발동 / {relicData.TriggerType} / {relicData.EffectType} {relicData.EffectValue}"); // 유물 발동 순서 로그 출력
        return true; // 유물 실행 성공 반환
    }

    private bool TryApplyTriggeredEffect(RelicData relic, BattleEventContext context) // 조건을 충족한 카드와 행동력 유물 반영
    { // 조건부 효과 시작
        switch (relic.EffectType) // 획득한 유물의 실제 효과 선택
        { // 효과 선택 시작
            case RelicEffectType.BattleStartCleanseOne: // 향 주머니의 전투 시작 디버프 정화
            case RelicEffectType.BattleEndCleanseOne: // 손수건의 실제 종료 디버프 정화
                return TryCleanseOneAllyStatus(); // 모든 생존 아군의 제거 가능한 일반 상태에서 한 개 추첨
            case RelicEffectType.PianoWireActionLossDamage: // 실제로 적용된 경직에만 생기는 일반 피해
                var pianoHit = context.TargetUnit.TakeDamage(relic.EffectValue, BattleDamageType.None); // 일반 방어도는 따르되 추가 직접 공격을 만들지 않는 피해
                return pianoHit.AppliedDamage > 0 || pianoHit.AbsorbedDamage > 0; // 실제로 체력이나 방어도에 피해를 남긴 경우만 기록
            case RelicEffectType.ConfessionMentalDamageMark: // 실제 적 정신력 손실의 일회 피해 표식
                var mark = context.TargetUnit.ApplyStatusEffect(BattleStatusEffectType.ConfessionDamageMark, relic.EffectValue, 1, 1, context.SourceUnit); // 일반 약화 면역에 따르는 턴 독립 표식 한 개 부여
                return mark is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked; // 실제 부여 결과만 발동으로 기록
            case RelicEffectType.MagicalHitMentalLoss: // 실제 유효 마법 피해의 검은 별가루 확률 성공
                return context.TargetUnit.ChangeMental(-relic.EffectValue, BattleMentalChangeReason.RelicEffect).WasApplied; // 실제 적 정신력의 음수 효과와 정상 하한 적용
            case RelicEffectType.ServantGloveHealBlock: // 체력을 실제 회복한 아군의 방어도
                return context.TargetUnit.GainBlock(relic.EffectValue) > 0; // 적용된 실제 회복 대상에게 보유 유물 방어도 지급
            case RelicEffectType.HealthDamageBlock: // 파열된 방울의 실제 체력 피해 반응
                var injured = context?.TargetUnit; // 방어도를 받을 실제 피격 아군
                return injured != null && injured.Team == BattleTeam.Ally && !injured.IsDead && context.AppliedAmount > 0 && injured.GainBlock(relic.EffectValue) > 0; // 생존자에게 방어도를 지급한 경우만 전투 횟수 소모
            case RelicEffectType.DecisionActionPoint: // 확인으로 고른 카드의 별도 결정 효과 성공
                var decisionOwner = context?.Card?.OwnerUnit; // 결정 효과가 발동한 카드 소유자
                return context?.Phase == BattleTurnPhase.PlayerTurn && decisionOwner != null && decisionOwner == context.SourceUnit && !decisionOwner.IsDead && decisionOwner.Team == BattleTeam.Ally && actionPoints != null && actionPoints.Gain(relic.EffectValue) > 0; // 실제 회복했을 때만 턴당 횟수 소모
            case RelicEffectType.EnemyTurnAttackLoss: // 적 턴 시작 무작위 적 한 명의 공격 약화
                if (context?.Phase != BattleTurnPhase.EnemyTurn) // 적 턴 확인
                { // 아군 턴 제외 시작
                    return false; // 무작위 추첨과 발동 횟수 보존
                } // 아군 턴 제외 종료
                var enemies = ResolveTargets(relic.TargetType, context); // 실제 생존 적 중 하나만 추첨
                return enemies.Count > 0 && enemies[0].ApplyStatusEffect(relic.StatusEffectType, relic.EffectValue, relic.StatusDuration, relic.StatusMaximumStacks) != BattleStatusEffectApplyResult.Invalid; // 면역과 지속 규칙을 따르는 약화
            case RelicEffectType.HealingCardBlock: // 회복 카드를 사용한 아군의 방어도 획득
            case RelicEffectType.ChaliceCardHealing: // 성배 카드를 사용한 아군의 체력 회복
            case RelicEffectType.ChaliceMentalOnUse: // 첫 성배 카드 사용자의 정신력 회복
                var card = context?.Card; // 실제 사용이 확정된 카드 원본
                var owner = card?.OwnerUnit; // 피해를 받은 대상과 구별한 실제 사용자
                if (context?.Phase != BattleTurnPhase.PlayerTurn || owner == null || owner.IsDead || owner.Team != BattleTeam.Ally) // 생존한 아군 사용자 확인
                { // 무효 사용 제외 시작
                    return false; // 발동 횟수 보존
                } // 무효 사용 제외 종료
                if (relic.EffectType == RelicEffectType.HealingCardBlock && (card.EffectType == CardEffectType.Heal || card.SourceData.FollowUpEffect == CardFollowUpEffect.HealAllAllies)) // 직접 또는 명시적 후속 체력 회복 확인
                { // 봉합사 발동 시작
                    owner.GainBlock(relic.EffectValue); // 회복 대상의 최대 체력과 무관하게 사용자 방어도 지급
                    return true; // 이번 턴 사용 횟수 기록
                } // 봉합사 발동 종료
                if (relic.EffectType == RelicEffectType.ChaliceCardHealing && card.CardType == CardType.Cup) // 실제 성배 계열 확인
                { // 성배 조각 발동 시작
                    owner.RestoreHealth(relic.EffectValue); // 사용자의 최대 체력까지만 회복
                    return true; // 이번 턴 사용 횟수 기록
                } // 성배 조각 발동 종료
                if (relic.EffectType == RelicEffectType.ChaliceMentalOnUse && card.CardType == CardType.Cup) // 아군 턴 최초 성배 사용 확인
                { // 성배 사용 정신력 지급 시작
                    owner.ChangeMental(relic.EffectValue, BattleMentalChangeReason.RelicEffect); // 카드 사용자의 정신력 회복
                    return true; // 회복 상한에 닿아도 첫 성배 사용 기회 기록
                } // 성배 사용 정신력 지급 종료
                return false; // 무관한 카드의 발동 횟수 보존
            case RelicEffectType.ActionPreventingStatusDraw: // 실제 적 제어 뒤 밀랍인장의 안전한 한 장 드로우
                return eventRelicDraw?.Invoke(relic.EffectValue) ?? (battleDeck?.DrawRelicCardsIntoHand(relic.EffectValue) > 0); // 실전 선택 예약 또는 선택 화면 없는 전투의 손패 여유 사용
            case RelicEffectType.FifthEnemyDebuffHeal: // 다섯 번째 약화 뒤 살아 있는 전체 아군 회복
                bool restored = false; // 실제 회복 발생 여부 초기화
                foreach (var ally in allyUnits) // 실제 전투의 아군 명단 순회
                { // 아군별 회복 적용 시작
                    if (ally != null && !ally.IsDead) // 현재 살아 있는 아군만 회복 대상으로 확인
                    { // 생존 아군 회복 시작
                        restored |= ally.RestoreHealth(relic.EffectValue) > 0; // 기존 회복량과 체력 상한을 사용하는 아군별 실제 회복
                    } // 생존 아군 회복 종료
                } // 아군별 회복 적용 종료
                return restored; // 한 명 이상 실제로 회복했을 때만 일반 발동 기록
            case RelicEffectType.EnemyMentalLossActionPoint: // 피해와 성공 확률이 확인된 공용 행동력 회복
                return actionPoints.Gain(relic.EffectValue) > 0; // 현재 행동력 상한 안에서 실제 한 점 지급
            case RelicEffectType.JournalCardRemoval: // 승리 확률 판정 후 회차 카드 한 장 정제
                var runDeck = RunDeckManager.Instance; // 현재 탐사의 실제 회차 덱
                if (runDeck == null || runDeck.CardCount <= RunDeckManager.MinimumCardsAfterRemoval) // 열 장 하한과 실제 덱 확인
                { // 제거 불가 확인 시작
                    return false; // 제거하지 못한 일지 보존
                } // 제거 불가 확인 종료
                var eligible = new List<int>(); // 실제 제거 가능한 카드 위치
                for (int index = 0; index < runDeck.CardCount; index++) // 출전 덱의 슬롯별 원본 확인
                { // 카드 위치 검사 시작
                    if (runDeck.CanRemoveAt(index)) // 기존 회차 덱 제거 조건 검사
                    { // 후보 등록 시작
                        eligible.Add(index); // 같은 원본이라도 개별 카드 위치 보존
                    } // 후보 등록 종료
                } // 카드 위치 검사 종료
                return eligible.Count > 0 && runDeck.TryRemoveAt(eligible[UnityEngine.Random.Range(0, eligible.Count)]) && inventory.TryRemove(relic.RelicId); // 실제 한 장 제거에 성공한 경우에만 일지 소진
            case RelicEffectType.CopyPlayedCard: // 첫 사용 카드 복사
                return context?.Phase == BattleTurnPhase.PlayerTurn && battleDeck != null && battleDeck.TryAddBattleCopy(context.Card); // 손패가 가득 차면 복사 기회 보존
            case RelicEffectType.QueueNextTurnDraw: // 종료 전 손패에 따른 다음 턴 드로우
                if (context?.Phase != BattleTurnPhase.PlayerTurn || battleDeck == null || battleDeck.HandCountBeforeCleanup == 0) // 자동 버림 전 손패와 아군 턴 확인
                { // 드로우 없음 시작
                    return false; // 빈 손패나 적 턴의 예약 제외
                } // 드로우 없음 종료
                battleDeck.QueuePlayerTurnDraw(relic.EffectValue); // 다음 아군 턴의 기본 드로우에 한 장 추가
                return true; // 보너스 드로우 예약 완료
            case RelicEffectType.RetainDiscardedHandCard: // 효과나 턴 종료로 버릴 첫 손패 유지
                return context?.Phase == BattleTurnPhase.PlayerTurn && battleDeck != null && battleDeck.RetainHandCard(context.Card); // 실제 버리려는 카드 한 장 유지
            case RelicEffectType.GainFirstTurnCapacity: // 첫 아군 턴에만 최대 및 현재 행동력 증가
                if (context?.Phase != BattleTurnPhase.PlayerTurn || context.Round != 1 || actionPoints == null) // 첫 아군 턴과 행동력 지갑 확인
                { // 행동력 지급 보류 시작
                    return false; // 적 선제 중 첫 아군 턴까지 보존
                } // 행동력 지급 보류 종료
                actionPoints.AddTurnCapacity(relic.EffectValue); // 첫 아군 턴에 행동력 상한과 현재 수치 증가
                return true; // 한 전투의 최초 행동력 지급 완료
            default: // 기존 전투 유물 경로
                ApplyEffect(relic, context); // 원래 효과 그대로 적용
                return true; // 기존 유물 발동 완료
        } // 효과 선택 종료
    } // 조건부 효과 종료
    private bool TryCleanseOneAllyStatus() // 전투 시작과 종료에서 생존 아군의 일반 약화 한 가지 정화
    { // 생존 아군 정화 후보 준비 시작
        var candidates = new List<(BattleUnitRuntime Ally, BattleStatusEffectType Type)>(); // 아군별 약화가 각각 같은 확률을 갖는 후보 목록
        foreach (var ally in allyUnits) // 전투에 참가한 아군 순회
        { // 정화 대상 확인 시작
            if (ally == null || ally.Team != BattleTeam.Ally || ally.IsDead) // 제거 가능한 생존 아군인지 확인
            { // 무효 아군 건너뛰기 시작
                continue; // 다음 생존 아군 조회
            } // 무효 아군 건너뛰기 종료
            foreach (var effect in ally.StatusEffects) // 해당 아군의 현재 상태 순회
            { // 보호 상태 구분 시작
                if (effect.IsDebuff && !effect.IsProtectedMentalState && !effect.IsExpired) // 제외 가능한 활성 일반 약화 확인
                { // 실제 정화 후보 보관 시작
                    candidates.Add((ally, effect.EffectType)); // 무작위 정화의 대상과 상태 기록
                } // 실제 정화 후보 보관 종료
            } // 보호 상태 구분 종료
        } // 정화 대상 확인 종료
        if (candidates.Count == 0) // 실제 제거할 일반 약화 존재 확인
        { // 후보 없음 처리 시작
            return false; // 전투 상태 보존
        } // 후보 없음 처리 종료
        var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)]; // 현재 모든 약화 인스턴스 가운데 하나 균등 선택
        return selected.Ally.RemoveStatusEffect(selected.Type); // 기존 정화 경로에 따른 한 가지 제거
    } // 생존 아군 정화 후보 준비 종료
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
        if (relicData.EffectType == RelicEffectType.GainActionPoints) // 처치에 따른 공용 행동력 회복
        { // 공용 행동력 처리 시작
            actionPoints?.Gain(relicData.EffectValue); // 최대 행동력 이내로 한 번 회복
            return; // 공용 효과 적용 완료
        } // 공용 행동력 처리 종료
        if (relicData.EffectType == RelicEffectType.DrawCards) // 전투 덱 드로우 효과 확인
        { // 드로우 효과 처리 시작
            battleDeck?.DrawCards(relicData.EffectValue); // 실제 전투 덱에서 카드 드로우
            return; // 대상별 중복 처리 방지
        } // 드로우 효과 처리 종료
        if (relicData.EffectType == RelicEffectType.GainGold) // 골드 획득 효과 확인
        {
            goldRuntime.AddGold(relicData.EffectValue); // 유물 효과 골드 지급
            return; // 대상 처리 없이 종료
        }

        List<BattleUnitRuntime> targets = ResolveTargets(relicData.TargetType, eventContext); // 유물 효과 대상 목록 계산
        if (relicData.EffectType == RelicEffectType.DistributeBlock) // 고정 방어도 배분 효과 확인
        { // 전체 방어도 배분 시작
            BattleBlockDistribution.Apply(targets, relicData.EffectValue); // 생존 대상에게 전체 방어도 순서대로 배분
            return; // 개별 효과 중복 적용 방지
        } // 전체 방어도 배분 종료
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
            case RelicEffectType.OpeningMark: // 수신기로 생존 적 전체에 공명 표식 적용
            case RelicEffectType.ApplyStatusEffect: // 상태 이상 적용 효과
                BattleStatusEffectApplyResult result = targetUnit.ApplyStatusEffect(relicData.StatusEffectType, relicData.EffectValue, relicData.StatusDuration, relicData.StatusMaximumStacks); // 상태 이상 효과 적용 결과 확인
                if (relicData.TriggerType == RelicTriggerType.BattleStarted && (result == BattleStatusEffectApplyResult.Applied || result == BattleStatusEffectApplyResult.Stacked)) // 실제 전투 시작 상태 효과 확인
                { // 첫 턴 시작 효과 처리 시작
                    targetUnit.GetStatusEffect(relicData.StatusEffectType)?.ConsumeDurationDecreaseSkip(); // 전투 시작 전 효과의 첫 턴 지속 감소 허용
                } // 첫 턴 시작 효과 처리 종료
                break; // 상태 이상 효과 종료
            case RelicEffectType.GainBlock: // 개별 방어도 획득 효과
                targetUnit.GainBlock(relicData.EffectValue); // 지정 대상의 방어도 획득
                break; // 방어도 효과 종료
            case RelicEffectType.ChangeMental: // 정신력 직접 증가 효과
                targetUnit.ChangeMental(relicData.EffectValue, BattleMentalChangeReason.RelicEffect); // 지정 대상의 정신력 증가
                break; // 정신력 효과 종료
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
            case RelicTargetType.LowestHealthLivingAlly: // 현재 체력이 가장 낮은 아군 대상
                BattleUnitRuntime lowest = null; // 최저 체력 후보 준비
                foreach (BattleUnitRuntime ally in allyUnits) // 앞쪽 편성부터 아군 순회
                { // 생존 아군 비교 시작
                    if (ally != null && !ally.IsDead && (lowest == null || ally.CurrentHealth < lowest.CurrentHealth)) // 생존 여부와 현재 체력 비교
                    { // 최저 체력 후보 교체 시작
                        lowest = ally; // 같은 체력에서는 먼저 찾은 아군 유지
                    } // 최저 체력 후보 교체 종료
                } // 생존 아군 비교 종료
                AddUnit(lowest, targets); // 선택된 아군 한 명 추가
                break; // 최저 체력 아군 선택 종료
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
            case RelicTargetType.RandomLivingAlly: // 무작위 생존 아군 대상
                List<BattleUnitRuntime> livingAllies = new List<BattleUnitRuntime>(); // 생존 아군 후보 목록 생성
                foreach (BattleUnitRuntime ally in allyUnits) // 실제 아군 목록 순회
                { // 생존 후보 확인 시작
                    if (ally != null && !ally.IsDead) // 유효한 생존 아군 확인
                    { // 생존 후보 추가 시작
                        livingAllies.Add(ally); // 무작위 선택 후보 추가
                    } // 생존 후보 추가 종료
                } // 생존 후보 확인 종료
                if (livingAllies.Count > 0) // 생존 후보 존재 확인
                { // 무작위 대상 선택 시작
                    targets.Add(livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)]); // 생존 아군 한 명 무작위 선택
                } // 무작위 대상 선택 종료
                break; // 무작위 아군 처리 종료
            case RelicTargetType.RandomLivingEnemy: // 무작위 생존 적 대상
                List<BattleUnitRuntime> livingEnemies = new List<BattleUnitRuntime>(); // 생존 적 후보 목록 생성
                foreach (BattleUnitRuntime enemy in enemyUnits) // 실제 적 목록 순회
                { // 적 후보 확인 시작
                    if (enemy != null && !enemy.IsDead) // 유효한 생존 적 확인
                    { // 생존 적 추가 시작
                        livingEnemies.Add(enemy); // 무작위 선택 후보 추가
                    } // 생존 적 추가 종료
                } // 적 후보 확인 종료
                if (livingEnemies.Count > 0) // 생존 적 존재 확인
                { // 무작위 적 선택 시작
                    targets.Add(livingEnemies[UnityEngine.Random.Range(0, livingEnemies.Count)]); // 생존 적 한 명 무작위 선택
                } // 무작위 적 선택 종료
                break; // 무작위 적 처리 종료
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

    private bool MatchesEventRelicTrigger(RelicEffectType effect, BattleEventContext context) // 실제 적용 여부를 추첨보다 앞서 확인
    { // 공용 상태와 정신력 시점 검사 시작
        switch (effect) // 데이터의 유물 효과별 실제 발동 조건 선택
        { // 세 신규 공용 신호 유물 분기 시작
            case RelicEffectType.ServantGloveHealBlock: // 실제 회복의 유효 대상 확인
                return context is { EventType: BattleEventType.HealingApplied, AppliedAmount: > 0, TargetUnit: { Team: BattleTeam.Ally, IsDead: false } }; // 양수 회복을 실제로 받은 생존 아군만 발동 허용
            case RelicEffectType.ActionPreventingStatusDraw: // 어비스 밀랍인장의 실제 적 행동 제어 확인
                return IsEnemyDebuffApplication(context) && context.SourceUnit?.Team == BattleTeam.Ally && context.StatusEffectType is BattleStatusEffectType.Stun or BattleStatusEffectType.Stagger; // 아군이 성공시킨 기절과 경직으로 제한
            case RelicEffectType.PianoWireActionLossDamage: // 행동을 잃는 실제 경직 부여 사건의 적과 발생자 확인
                return IsEnemyDebuffApplication(context) && context.Phase is (BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn) && // 현재 전투의 신규 또는 중첩 성공만 인정
                    context.StatusEffectType == BattleStatusEffectType.Stagger && context.SourceUnit is { Team: BattleTeam.Ally, IsDead: false } && // 기존 기절과 속도 감소를 제외한 아군의 경직 확인
                    context.TargetUnit is { IsDead: false } && context.TargetUnit.GetStatusEffect(BattleStatusEffectType.Stagger) is { IsExpired: false }; // 실제로 다음 행동을 한 번 막는 살아 있는 적의 활성 상태
            case RelicEffectType.FifthEnemyDebuffHeal: // 공허의 잔뿌리의 실제 약화 횟수 확인
                if (!IsEnemyDebuffApplication(context)) // 일반 강화와 면역 및 무효 상태 제외
                { // 횟수 미변경 처리 시작
                    return false; // 성공하지 않은 약화는 횟수 제외
                } // 횟수 미변경 처리 종료
                enemyDebuffApplications = (enemyDebuffApplications + 1) % DebuffApplicationsPerHealing; // 전투 내 적 약화의 다섯 회마다 주기 초기화
                return enemyDebuffApplications == 0; // 다섯 번째 실제 적용 때에만 전체 회복 기회
            case RelicEffectType.ConfessionMentalDamageMark: // 고해실의 못을 발동시킨 실제 전투 정신력 손실 확인
                return context is { EventType: BattleEventType.MentalChanged, Phase: BattleTurnPhase.PlayerTurn or BattleTurnPhase.EnemyTurn } && // 진행 중인 실제 진영의 정신력 변화
                    context.SourceUnit is { Team: BattleTeam.Ally, IsDead: false } && context.TargetUnit is { Team: BattleTeam.Enemy, IsDead: false } && // 정신력 비용이나 발생자가 없는 지속 피해 제외
                    context.MentalResult is { AppliedDelta: < 0, Reason: BattleMentalChangeReason.CardEffect or BattleMentalChangeReason.ReceiveDamage } && // 아군 명시 카드 효과와 실피격으로 실제 감소한 정신력
                    context.TargetUnit.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark) is not { IsExpired: false }; // 기존 표식을 중복 부여하거나 갱신하지 않는 확인
            case RelicEffectType.EnemyMentalLossActionPoint: // 침식 방울목걸이의 명시적 적 정신력 피해 확인
                return context?.Phase == BattleTurnPhase.PlayerTurn && context.SourceUnit?.Team == BattleTeam.Ally && context.TargetUnit?.Team == BattleTeam.Enemy && // 동료의 적 대상 실제 아군 턴 원인 확인
                    context.MentalResult is { AppliedDelta: < 0, Reason: BattleMentalChangeReason.CardEffect or BattleMentalChangeReason.DisruptReceived } && // 단순 피격과 턴 유지 등 자동 정신력 변화 제외
                    actionPoints != null && actionPoints.CurrentActionPoints < actionPoints.MaxActionPoints; // 실제 증가 공간이 있을 때만 25퍼센트 판정 가능
            default: // 기존 모든 유물의 선언된 효과 유지
                return true; // 나머지 유물은 원래 실행 규칙 사용
        } // 세 신규 공용 신호 유물 분기 종료
    } // 공용 상태와 정신력 시점 검사 종료

    private static bool IsEnemyDebuffApplication(BattleEventContext context) // 실제 적의 성공한 약화 부여와 중첩만 확인
    { // 상태 이벤트 유효성 검사 시작
        return context?.EventType == BattleEventType.StatusApplied && context.TargetUnit?.Team == BattleTeam.Enemy && // 실제 적에게 도착한 상태 적용 시점 확인
            context.Phase is not (BattleTurnPhase.Victory or BattleTurnPhase.Defeat or BattleTurnPhase.Escaped) && // 전투 종료 이후의 적용 제외
            context.StatusApplyResult is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked && // 새 적용 또는 성공한 중첩 확인
            BattleStatusEffectInstance.IsDebuffType(context.StatusEffectType); // 공통 상태 분류를 사용해 강화 제외
    } // 상태 이벤트 유효성 검사 종료

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
            case BattleEventType.DecisionResolved: // 단순 선택 뒤 별도 결정 효과까지 성공한 이벤트
                return RelicTriggerType.DecisionDraw; // 결정 성공 유물 발동
            case BattleEventType.BeforeHandDiscard: // 손패 버림 직전 이벤트
                return RelicTriggerType.BeforeHandDiscard; // 효과와 턴 종료의 버림 직전 반환
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
