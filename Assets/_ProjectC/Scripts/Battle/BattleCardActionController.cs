using System; // 기본 인터페이스 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 로그 기능 사용
public sealed class BattleCardActionController : IDisposable // 카드 선택과 사용 흐름 관리
{ // 클래스 시작
    private readonly BattleDeckRuntime runtimeDeck; // 연결된 런타임 덱
    private readonly BattleActionPointRuntime sharedActionPoints; // 연결된 공용 행동력
    private readonly BattleTurnRuntime turnRuntime; // 연결된 전투 턴 관리자
    private readonly BattleHandView handView; // 연결된 손패 화면
    private readonly BattleActionSequenceRunner actionSequenceRunner; // 전투 행동 연출 실행기
    private readonly BattleStatusEffectProcessor statusEffectProcessor; // 공통 상태 처리기
    private int lastBlackRoseRound = -1; // 검은 장미 코사지의 파티 공용 마지막 발동 라운드
    private int lastPencilRound = -1; // 흑연 연필의 마지막 성공 라운드
    private int pendingInspectionDraw; // 바보의 남은 카드 처리 뒤 받을 추가 드로우 수
    private int lastPentacleDrawRound = -1; // 펜타클 드로우 유물의 마지막 아군 라운드
    private int lastRetainedHealingRound = -1; // 흰 깃털 펜던트의 마지막 발동 라운드
    private int lastRetainedDamageRound = -1; // 검은 깃털 펜던트의 마지막 발동 라운드
    private bool usedFirstCardDamageBonus; // 붉은 잉크병의 전투당 한 번 발동 기록
    private int pendingPentacleDraw; // 카드 선택이 끝난 뒤 받을 펜타클 드로우 수
    private int pendingEventRelicDraw; // 카드 선택이나 사용 중 쌓인 공용 신호의 유물 드로우
    private bool pendingPentacleChanceDraw; // 실제 사용 후 최종 선택을 기다리는 펜촉 드로우
    private bool HasPendingEffectChoice => pendingCardChoices?.Count > 0 || pendingDebuffChoices?.Count > 0; // 카드나 상태 선택의 남은 효과 확인
    private CardInstance pendingSkillRelicCard; // 카드 이동과 마지막 선택이 끝난 뒤 검사할 스킬 카드
    private readonly HashSet<CardType> resolvedSuits = new HashSet<CardType>(); // 현재 아군 턴에 정산된 서로 다른 카드 속성
    private int resolvedSuitsRound = -1; // 속성 집계를 적용할 실제 아군 라운드
    private int resolvedWandsSincePulse; // 마지막 공명 뒤 완료한 지팡이 수
    private CardInstance pendingCardSuitRelics; // 이동과 필수 선택을 마칠 때 검사할 카드
    private int pendingCardSuitRound; // 사용 카드를 정산하기 시작한 아군 라운드
    private bool usedFirstCardDrawBonus; // 이번 전투 최초 카드 드로우 증가 사용 기록
    private bool usedFirstHealingCard; // 파티가 이번 전투에 첫 회복 카드를 사용한 기록
    private int bandageBonus; // 현재 카드의 원래 회복 대상별 추가량
    private bool usedFirstCupHealingBonus; // 전투에서 실제 회복으로 성수 마개를 사용한 여부
    private int cupHealingBonusForCurrentCard; // 먼저 성공한 성배 카드의 모든 회복 대상에게 주는 가산량
    private readonly FirstSuitHitRelics suitHitRelics = new FirstSuitHitRelics(); // 전투별 첫 검과 지팡이 명중 기록
    private readonly BattleSupplyRelics supplyRelics = new BattleSupplyRelics(); // 이번 전투의 심장핀과 구슬 발동 기록
    private int preparedSwordDamage; // 사용이 확정된 검 한 장의 모든 타격에 더할 피해
    private int preparedWandDamage; // 사용이 확정된 지팡이 한 장의 모든 명중에 더할 피해
    private int paidActionPoints; // 이번 카드 사용 시 실제로 지불한 행동력
    private readonly BattleEnemyActionRuntime enemyActions; // 적 예정 행동 관리자
    private readonly WinterfallBossPhaseRuntime bossPhaseRuntime; // 선택적인 보스 페이즈 관리자
    private readonly LoversPartnerRuntime loversPartner; // 연인 파트너와 행동 기록
    private readonly ChariotConcentrationRuntime chariotConcentration; // 전차 집중 공격과 표식 기록
    private readonly StarRecoveryPassiveRuntime starRecoveryPassive; // 별의 턴당 추가 예약 회복
    private readonly WorldCoordinationRuntime worldCoordination; // 세계의 동료 전용 카드 연계 기록
    private readonly IReadOnlyList<BattleUnitView> allyUnitViews; // 아군 유닛 화면 목록
    private readonly IReadOnlyList<BattleUnitView> enemyUnitViews; // 적 유닛 화면 목록
    private CardInstance selectedCard; // 현재 선택 카드
    private BattleUnitRuntime firstSequentialTarget; // 두 대상을 고르는 카드의 첫 유닛
    private int requiredSelectedEnemies; // 카드 입력을 시작한 순간의 고정 적 대상 수
    private IReadOnlyList<CardInstance> pendingCardChoices; // 카드 선택 대기 후보
    private CardEffectType pendingChoiceEffect; // 대기 중인 카드 선택 효과
    private BattleDrawChoiceView drawChoiceView; // 현재 선택 드로우 화면
    private readonly List<CardInstance> usedCardsThisTurn = new List<CardInstance>(); // 실제 사용을 완료한 서로 다른 이번 턴의 카드
    private int usedCardsRound; // 이전 아군 턴 기록과 현재 후보 분리
    private BattleDrawChoiceView pentacleHandChoiceView; // 두 손패 효과만 사용하는 별도 모달
    private CardInstance pentacleHandChoiceSource; // 실제 선택을 시작한 손패 원본
    private CardInstance firstTacticalDiscard; // 전술 교환에서 아직 결제하지 않은 첫 선택
    private CardInstance manaRetrievalSource; // 시전 전에 별도 지팡이 선택을 기다리는 회수 카드
    private CardInstance manaRetrievalDiscard; // 실제 충돌에서 다시 확인하고 버릴 별도 손패 지팡이
    private BattleRemainingCardChoiceView remainingChoiceView; // 남은 카드 처리 선택 화면
    private BattleDrawChoiceView openingBookmarkView; // 서고 먼지갈피의 실제 선택 화면
    private BattleDrawChoiceView wheelPaymentView; // 손패 비용을 순서대로 고르는 실제 선택 창
    private CardInstance wheelPaymentSource; // 비용을 고르는 동안 유지할 수레바퀴 원본 카드
    private readonly List<CardInstance> wheelPaymentCards = new List<CardInstance>(); // 아직 손패에 남아 있는 선택한 비용 카드
    private int wheelPaymentAmount; // 첫 선택 순간의 실제 행동력과 최종 필요 카드 장수
    private BattleDebuffChoiceView debuffChoiceView; // 디버프 이전 선택 화면
    private IReadOnlyList<BattleStatusEffectInstance> pendingDebuffChoices; // 이전할 디버프 선택 후보
    private BattleUnitRuntime pendingDebuffSource; // 디버프를 넘길 동료
    private BattleUnitRuntime pendingDebuffRecipient; // 디버프를 받을 카드 소유자
    private CardInstance remainingChoiceCard; // 남은 선택 후보 카드
    private bool pendingFoolPassive; // 바보 패시브 선택 대기 여부
    private int lastFoolPassiveRound = -1; // 바보 패시브 마지막 발동 턴
    private bool actionPending; // 카드 행동 연출 진행 여부
    private bool disposed; // 연결 해제 여부
    public CardInstance SelectedCard => selectedCard; // 현재 선택 카드 조회
    public event Action<CardInstance, IReadOnlyList<BattleUnitRuntime>> CardUsed; // 카드 효과 적용 시작 이벤트
    public BattleCardActionController(BattleDeckRuntime battleDeck, BattleActionPointRuntime actionPoints, BattleTurnRuntime battleTurn, BattleHandView battleHandView, BattleActionSequenceRunner sequenceRunner, BattleStatusEffectProcessor processor, IReadOnlyList<BattleUnitView> allyViews, IReadOnlyList<BattleUnitView> enemyViews, BattleEnemyActionRuntime enemyRuntime, WinterfallBossPhaseRuntime bossPhase = null) // 카드 행동 관리자 생성
    { // 생성자 시작
        runtimeDeck = battleDeck ?? throw new ArgumentNullException(nameof(battleDeck)); // 런타임 덱 저장
        sharedActionPoints = actionPoints ?? throw new ArgumentNullException(nameof(actionPoints)); // 공용 행동력 저장
        turnRuntime = battleTurn ?? throw new ArgumentNullException(nameof(battleTurn)); // 전투 턴 관리자 저장
        handView = battleHandView ?? throw new ArgumentNullException(nameof(battleHandView)); // 손패 화면 저장
        actionSequenceRunner = sequenceRunner ?? throw new ArgumentNullException(nameof(sequenceRunner)); // 행동 연출 실행기 저장
        statusEffectProcessor = processor ?? throw new ArgumentNullException(nameof(processor)); // 공통 상태 처리기 저장
        enemyActions = enemyRuntime ?? throw new ArgumentNullException(nameof(enemyRuntime)); // 적 행동 관리자 저장
        bossPhaseRuntime = bossPhase; // 선택적인 보스 페이즈 관리자 저장
        loversPartner = new LoversPartnerRuntime(turnRuntime, enemyActions.AllyUnits); // 연인 파트너 관리자 생성
        chariotConcentration = new ChariotConcentrationRuntime(turnRuntime, enemyActions.EnemyUnits); // 전차 집중 공격 관리자 생성
        starRecoveryPassive = new StarRecoveryPassiveRuntime(turnRuntime); // 별 추가 예약 회복 관리자 생성
        worldCoordination = new WorldCoordinationRuntime(turnRuntime, enemyActions.AllyUnits); // 세계의 동료 전용 카드 연계 관리자 생성
        loversPartner.Changed += RefreshPartnerMarkers; // 파트너 표시 갱신 연결
        allyUnitViews = allyViews ?? throw new ArgumentNullException(nameof(allyViews)); // 아군 화면 목록 저장
        enemyUnitViews = enemyViews ?? throw new ArgumentNullException(nameof(enemyViews)); // 적 화면 목록 저장
        handView.CardClicked += HandleCardClicked; // 카드 클릭 이벤트 등록
        RegisterUnitViewEvents(allyUnitViews); // 아군 클릭 이벤트 등록
        RegisterUnitViewEvents(enemyUnitViews); // 적 클릭 이벤트 등록
        runtimeDeck.StateChanged += HandleDeckStateChanged; // 덱 상태 변경 이벤트 등록
        turnRuntime.StateChanged += HandleTurnStateChanged; // 턴 상태 변경 이벤트 등록
        sharedActionPoints.StateChanged += RecheckWheelPayment; // 후보 선택 중 실제 행동력이 바뀌면 이전 입력 해제
        handView.RefreshCardAvailability(); // 시작 카드 사용 가능 상태 갱신
    } // 생성자 종료
    internal static BattleStatusEffectType RequiredWandEnemyStatus(WandCardRule rule) // 지팡이의 지정 적에게 이미 필요한 상태 종류
    { // 거르고 고르는 원소 카드 조건 시작
        return rule switch // 실제 카드의 원소 규칙 구분
        { // 원소 규칙 분류 시작
            WandCardRule.BurnAmplification => BattleStatusEffectType.Burn, // 활성 화상의 남은 기간 연장
            WandCardRule.ShockTransfer => BattleStatusEffectType.Shock, // 처음 맞힐 적의 기존 감전 필요
            _ => BattleStatusEffectType.None // 일반 카드의 기존 대상 규칙 유지
        }; // 원소 규칙 분류 종료
    } // 거르고 고르는 원소 카드 조건 종료
    private void HandleCardClicked(CardInstance cardInstance) // 카드 클릭 처리
    { // 카드 클릭 처리 시작
        if (!turnRuntime.CanAcceptPlayerInput || actionPending || actionSequenceRunner.IsBusy) // 개막 선택을 마친 플레이어 턴과 행동 연출 여부 확인
        { // 플레이어 턴 아님 처리 시작
            return; // 카드 클릭 처리 중단
        } // 플레이어 턴 아님 처리 종료
        if (cardInstance == null || cardInstance.EffectType == CardEffectType.Unplayable || !ContainsHandCard(cardInstance)) // 사용할 수 있는 손패 카드 확인
        { // 잘못된 카드 처리 시작
            return; // 카드 클릭 처리 중단
        } // 잘못된 카드 처리 종료
        var requiredState = RequiredWandEnemyStatus(cardInstance.SourceData.WandRule); // 두 원소 규칙이 실제로 지정한 최초 상태
        if (requiredState != BattleStatusEffectType.None && !turnRuntime.HasSelectableEnemyWithStatus(requiredState)) // 원래 지정 상태를 가진 생존 적이 있는지 확인
        { // 원소 카드의 유효 후보 없음 처리 시작
            return; // 비용을 지불하지 않고 원래 손패 유지
        } // 원소 카드의 유효 후보 없음 처리 종료
        if (cardInstance.EffectType == CardEffectType.ReviveAlly && !turnRuntime.HasFallenAlly()) // 손패 선택 중 전투불능 대상 확인
        { // 부활 대상 없음 시작
            return; // 대상 선택 시작 차단
        } // 부활 대상 없음 종료
        if (cardInstance.TargetType is (CardTargetType.EnemyAndAlly or CardTargetType.SelectedTwoEnemies) && // 적을 직접 선택하는 순차 피해 카드 확인
            (cardInstance.EffectType != CardEffectType.Damage || !turnRuntime.HasSelectableEnemy)) // 실제 선택 가능한 적과 유효한 피해 효과 확인
        { // 적 대상 없음 처리 시작
            return; // 두 대상 선택 시작 차단
        } // 적 대상 없음 처리 종료
        if (cardInstance.TargetType == CardTargetType.BlessingSourceAndAlly && (!BattleBlessingTransfer.IsTransferCard(cardInstance) || !BattleBlessingTransfer.HasTargets(turnRuntime))) // 유효한 강화 보유자와 수령자 조합 확인
        { // 축복 후보 없음 처리 시작
            return; // 실제 복사 대상이 없는 카드 선택 차단
        } // 축복 후보 없음 처리 종료
        if (cardInstance.OwnerUnit.IsDead) // 카드 소유자 사망 확인
        { // 사망 소유자 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 사망한 소유자의 카드는 사용할 수 없습니다: {cardInstance.OwnerUnit.DisplayName}"); // 사용 불가 출력
            return; // 카드 클릭 처리 중단
        } // 사망 소유자 처리 종료
        if (!SwordCardCombat.CanUse(cardInstance, sharedActionPoints, turnRuntime)) // 카드 비용 지불 가능 확인
        { // 행동력 부족 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 카드 사용 조건 부족: {cardInstance.DisplayName} / 현재 AP {sharedActionPoints.CurrentActionPoints}"); // 행동력 부족 출력
            handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
            return; // 카드 클릭 처리 중단
        } // 행동력 부족 처리 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.WheelOfFate) // 다른 실제 손패 카드를 비용으로 쓰는 원본 확인
        { // 전액 지불 전 수레바퀴의 카드 비용 선택 시작
            BeginWheelPayment(cardInstance); // 카드와 행동력을 아직 이동하지 않는 실제 선택창 열기
            return; // 아직 지불하지 않은 전체 아군 자동 사용 보류
        } // 전액 지불 전 수레바퀴의 카드 비용 선택 종료
        if (selectedCard == cardInstance) // 동일 카드 재선택 확인
        { // 재선택 처리 시작
            CancelSelection(); // 카드 선택 취소
            return; // 카드 클릭 처리 종료
        } // 재선택 처리 종료
        if (GetPentacleHandChoiceCount(cardInstance) > 0) // 손패 입력을 먼저 확정하는 두 펜타클 규칙 확인
        { // 실제 손패 선택 창 열기 시작
            ShowPentacleHandChoice(cardInstance); // 행동력과 원본 이동을 보류한 실제 후보 표시
            return; // 일반 자기 대상 즉시 사용 보류
        } // 실제 손패 선택 창 열기 종료
        if (cardInstance.EffectType == CardEffectType.DiscardWandForActionPoint) // 마력 회수의 확정 전 손패 선택 효과 확인
        { // 무료 카드의 버림 선택 시작
            ShowManaRetrievalChoices(cardInstance); // 비용과 사용 신호보다 먼저 별도 지팡이 선택 열기
            return; // 즉시 자신에게 사용하는 일반 카드 흐름 생략
        } // 무료 카드의 버림 선택 종료
        firstSequentialTarget = null; // 새 카드 선택 전 첫 유닛 초기화
        selectedCard = cardInstance; // 새 선택 카드 저장
        handView.SetSelectedCard(selectedCard); // 손패 선택 표시 적용
        handView.SetMixedTargetStage(selectedCard.TargetType == CardTargetType.EnemyAndAlly ? BattleTeam.Enemy : null); // 복합 카드의 첫 적 선택 안내 표시
        requiredSelectedEnemies = selectedCard.TargetType == CardTargetType.SelectedTwoEnemies ? turnRuntime.CountSelectableEnemyPair() : 0; // 선택 시작 순간 확정한 대상 수 보관
        if (requiredSelectedEnemies > 0) // 서리 파편의 새로운 적 선택 안내 확인
        { // 적 대상 단계 표시 시작
            handView.SetSelectedEnemyStage(null, requiredSelectedEnemies); // 손패에 현재 필요한 적 수와 첫 입력 안내 전달
        } // 적 대상 단계 표시 종료
        if (selectedCard.TargetType == CardTargetType.Self) // 자신 대상 카드 확인
        { // 자신 대상 처리 시작
            if (IsValidTarget(selectedCard, selectedCard.OwnerUnit) && (GetRandomWandHitCount(selectedCard) == 0 || turnRuntime.HasSelectableEnemy)) // 자체 시전 공격의 유효한 생존 적 여부 확인
            { // 자신 대상 실행 시작
                ExecuteCard(selectedCard, CreateSingleTargetList(selectedCard.OwnerUnit)); // 소유자에게 카드 사용
            } // 자신 대상 실행 종료
            else // 자신 대상 실행 불가
            { // 자신 대상 실패 처리 시작
                CancelSelection(); // 카드 선택 상태 초기화
            } // 자신 대상 실패 처리 종료
            return; // 카드 클릭 처리 종료
        } // 자신 대상 처리 종료
        if (selectedCard.TargetType == CardTargetType.LowestHealthAlly) // 가장 낮은 생존 아군의 자동 회복 확인
        { // 자동 회복 카드 실행 시작
            BattleUnitRuntime ally = turnRuntime.GetLowestHealthLivingAlly(); // 전체 명단에서만 결정하는 자동 대상 조회
            if (IsValidTarget(selectedCard, ally)) // 선택 시점의 실제 치유 대상 확인
            { // 자동 치유 카드 사용 시작
                ExecuteCard(selectedCard, CreateSingleTargetList(ally)); // 대상 클릭 없이 현재 최저 체력 아군 한 명에게 사용
            } // 자동 치유 카드 사용 종료
            else // 회복할 생존 대상이 없는 경우
            { // 무효한 카드 선택 해제 시작
                CancelSelection(); // 행동력 지불 전 선택 초기화
            } // 무효한 카드 선택 해제 종료
            return; // 사용자 지정 대상 선택 단계 생략
        } // 자동 회복 카드 실행 종료
        if (selectedCard.TargetType == CardTargetType.AllAllies) // 전체 아군 대상 확인
        { // 전체 아군 처리 시작
            ExecuteCard(selectedCard, CollectValidTargets(selectedCard, allyUnitViews)); // 전체 유효 아군에게 카드 사용
            return; // 카드 클릭 처리 종료
        } // 전체 아군 처리 종료
        if (selectedCard.TargetType is CardTargetType.AllEnemies or CardTargetType.RandomTwoEnemies) // 자동으로 전체 또는 무작위 적을 정하는 카드 확인
        { // 전체 적 처리 시작
            ExecuteCard(selectedCard, CollectValidTargets(selectedCard, enemyUnitViews)); // 전체 유효 적에게 카드 사용
            return; // 카드 클릭 처리 종료
        } // 전체 적 처리 종료
        UpdateTargetHighlights(); // 첫 번째 대상 후보 강조
    } // 카드 클릭 처리 종료
    private void HandleUnitClicked(BattleUnitRuntime runtimeUnit) // 유닛 클릭 처리
    { // 유닛 클릭 처리 시작
        if (!turnRuntime.CanAcceptPlayerInput || actionPending || actionSequenceRunner.IsBusy) // 개막 선택을 마친 플레이어 턴과 행동 연출 여부 확인
        { // 플레이어 턴 아님 처리 시작
            return; // 유닛 클릭 처리 중단
        } // 플레이어 턴 아님 처리 종료
        if (selectedCard == null || runtimeUnit == null) // 카드와 대상 존재 확인
        { // 선택 없음 처리 시작
            return; // 유닛 클릭 처리 중단
        } // 선택 없음 처리 종료
        if (!IsValidTarget(selectedCard, runtimeUnit)) // 대상 규칙 확인
        { // 잘못된 대상 처리 시작
            Debug.LogWarning($"[BattleCardActionController] {runtimeUnit.DisplayName}은 {selectedCard.DisplayName}의 대상이 아닙니다."); // 잘못된 대상 출력
            return; // 유닛 클릭 처리 중단
        } // 잘못된 대상 처리 종료
        bool selectsEnemyAndAlly = selectedCard.TargetType == CardTargetType.EnemyAndAlly; // 적과 아군을 차례로 선택하는 카드 확인
        bool selectsBlessingSource = selectedCard.TargetType == CardTargetType.BlessingSourceAndAlly; // 강화를 복사할 두 아군 선택 확인
        bool selectsEnemyPair = selectedCard.TargetType == CardTargetType.SelectedTwoEnemies && requiredSelectedEnemies == 2; // 시작 시 두 적 지정이 필요한 카드 확인
        if (selectsEnemyAndAlly || selectsBlessingSource || selectsEnemyPair || selectedCard.SourceData.ShieldRule == ShieldCardRule.SplitBarrier) // 두 유닛 순차 선택 카드 확인
        { // 순차 대상 선택 시작
            if (runtimeUnit == firstSequentialTarget) // 먼저 선택한 유닛의 반복 입력 확인
            { // 같은 대상 처리 시작
                return; // 행동력과 첫 번째 선택 보존
            } // 같은 대상 처리 종료
            if (firstSequentialTarget == null) // 첫 번째 대상 선택 여부 확인
            { // 첫 번째 대상 보관 시작
                firstSequentialTarget = runtimeUnit; // 첫 입력 유닛 저장
                if (selectsEnemyAndAlly) // 적을 먼저 고르는 복합 카드 확인
                { // 두 번째 대상 안내 시작
                    handView.SetMixedTargetStage(BattleTeam.Ally); // 피해 대상 확정 뒤 회복 대상 선택 안내
                } // 두 번째 대상 안내 종료
                if (selectsBlessingSource) // 첫 번째 강화 보유자를 선택했는지 확인
                { // 강화 수령 대상 안내 시작
                    handView.SetBlessingTransferSource(runtimeUnit); // 드롭과 클릭에 같은 수령 대상 단계 표시
                } // 강화 수령 대상 안내 종료
                if (selectsEnemyPair) // 먼저 고를 적의 선택 완료 확인
                { // 두 번째 적 선택 안내 시작
                    handView.SetSelectedEnemyStage(runtimeUnit, requiredSelectedEnemies); // 드래그와 클릭이 공유할 첫 적과 둘째 단계 설정
                } // 두 번째 적 선택 안내 종료
                UpdateTargetHighlights(); // 두 번째 단계의 유효 대상 강조 갱신
                return; // 두 번째 입력까지 카드 사용 보류
            } // 첫 번째 대상 보관 종료
            ExecuteCard(selectedCard, new[] { firstSequentialTarget, runtimeUnit }); // 두 대상 확정 후 카드 한 번 실행
            return; // 다른 단일 대상 처리 차단
        } // 두 아군 대상 선택 종료
        if (selectedCard.SourceData.SwordRule == SwordCardRule.BladeWhirlwind) // 소용돌이의 고정 세 칸 확인
        { // 세 칸 공격 시작
            ExecuteCard(selectedCard, SwordCardCombat.GetWhirlwindArea(runtimeUnit, enemyActions.EnemyUnits)); // 선택한 적과 바로 옆 생존 적에게 실제 카드 사용
            return; // 다른 범위의 중복 공격 차단
        } // 세 칸 공격 종료
        var targets = selectedCard.SourceData.SwordRule == SwordCardRule.SwordDance || selectedCard.SourceData.SwordRule == SwordCardRule.HorizontalSlash ? SwordCardCombat.GetDanceArea(runtimeUnit, enemyActions.EnemyUnits) : CreateSingleTargetList(runtimeUnit); // 범위 공격은 선택 위치의 두 칸으로 고정
        ExecuteCard(selectedCard, targets); // 선택 대상에게 카드 사용
    } // 유닛 클릭 처리 종료
    private void HandleDeckStateChanged() // 덱 상태 변경 처리
    { // 덱 상태 처리 시작
        RecheckWheelPayment(); // 외부 카드 이동 뒤 대기 중인 실제 출처와 비용 후보 확인
        if (pentacleHandChoiceSource != null && (!ContainsHandCard(pentacleHandChoiceSource) || firstTacticalDiscard != null && !ContainsHandCard(firstTacticalDiscard))) // 원본이나 먼저 고른 실물이 손패를 떠난 경우 확인
        { // 만료된 손패 선택 정리 시작
            ClosePentacleHandChoice(); // 행동력과 다른 카드 이동 없이 선택 종료
        } // 만료된 손패 선택 정리 종료
        if (manaRetrievalSource != null && !ContainsHandCard(manaRetrievalSource)) // 원래 회수 카드가 실제 손패에 아직 있는지 확인
        { // 선택 중인 원본 카드의 물리 이탈 시작
            CloseCardChoice(); // 비용과 효과 없이 대기 중 지팡이 선택 창 닫기
        } // 선택 중인 원본 카드의 물리 이탈 종료
        if (selectedCard != null && !ContainsHandCard(selectedCard)) // 선택 카드의 손패 이탈 확인
        { // 손패 이탈 처리 시작
            CancelSelection(); // 카드 선택 상태 초기화
        } // 손패 이탈 처리 종료
    } // 덱 상태 처리 종료
    private void HandleTurnStateChanged() // 턴 상태 변경 처리
    { // 턴 상태 처리 시작
        UpdateOpeningBookmarkView(); // 현재 개막 선택의 표시와 끝난 선택 화면 정리
        RecheckWheelPayment(); // 실제 턴 변경 뒤 수레바퀴의 이전 선택을 유지할 수 있는지 확인
        if (!turnRuntime.IsPlayerTurn) // 플레이어 턴 종료 확인
        { // 입력 잠금 처리 시작
            CancelSelection(); // 카드 선택 상태 초기화
            CloseCardChoice(); // 대기 선택 화면 정리
        } // 입력 잠금 처리 종료
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
    } // 턴 상태 처리 종료
    private void UpdateOpeningBookmarkView() // 실제 개막 선택의 모달 화면과 후보 연결
    { // 개막 쪽지 화면 정산 시작
        var choices = turnRuntime.OpeningBookmarkChoices; // 첫 손패가 끝난 실제 후보 조회
        if (choices == null) // 선택이 없거나 이미 끝난 전투 확인
        { // 이전 화면 정리 시작
            if (openingBookmarkView != null) // 활성 화면 객체의 남은 참조 확인
            { // 남은 모달 제거 시작
                openingBookmarkView.Close(); // 결과로 끝난 개막 화면 바로 닫기
                openingBookmarkView = null; // 이전 화면 객체의 참조 제거
            } // 남은 모달 제거 종료
            return; // 활성 개막 후보가 없을 때 새 화면 생성 생략
        } // 이전 화면 정리 종료
        if (openingBookmarkView != null) // 현재 선택 화면이 이미 열린 상태 확인
        { // 중복 생성 방지 시작
            return; // 다음 상태 알림까지 동일한 실제 화면 유지
        } // 중복 생성 방지 종료
        var canvas = handView.GetComponentInParent<Canvas>(); // 일반 선택 창을 열 실제 전투 캔버스
        if (canvas == null) // 실제 화면에 연결할 위치 누락 확인
        { // 모달을 열 수 없는 경로 시작
            turnRuntime.ResolveOpeningBookmark(null); // 다른 전투 입력이 영구 잠기지 않도록 선택 취소
            return; // 캔버스 없는 화면 생성 생략
        } // 모달을 열 수 없는 경로 종료
        openingBookmarkView = BattleDrawChoiceView.Show( // 실제 카드 후보를 유물 명패가 있는 선택 화면에 연결
            canvas.transform, // 전투 선택창의 부모 캔버스
            choices, // 현재 뽑기 더미의 실제 선택 후보
            ResolveOpeningBookmarkView, // 기존 카드 선택 정산
            titleText: "가져올 카드 선택", // 중앙 패널 안에 표시할 안내 문구
            cancelled: () => ResolveOpeningBookmarkView(null), // 기존 선택 취소와 전투 입력 복귀
            relicName: "서고 먼지갈피"); // 패널 위 개별 명패의 유물 이름
    } // 개막 쪽지 화면 정산 종료
    private void ResolveOpeningBookmarkView(CardInstance choice) // 기존 모달의 카드 선택 또는 취소 버튼 처리
    { // 모달 선택 결과 전달 시작
        openingBookmarkView = null; // 자체 닫힌 선택 창을 상태 갱신 전에 참조 제거
        turnRuntime.ResolveOpeningBookmark(choice); // 실제 카드 이동과 일반 전투 입력 복구 요청
    } // 모달 선택 결과 전달 종료
    private void BeginWheelPayment(CardInstance source) // 실제 가용 행동력 수만큼 다른 손패 선택 시작
    { // 수레바퀴 선택 준비 시작
        var canvas = handView.GetComponentInParent<Canvas>(); // 원래 카드 선택 모달이 연결될 실제 전투 화면
        if (canvas == null || !turnRuntime.TryBeginPlayerCardDecision()) // 현재 실제 아군 입력 및 모달 환경 확인
        { // 선택을 열 수 없는 경우 시작
            return; // 비용 차감 없이 이전 상태 유지
        } // 선택을 열 수 없는 경우 종료
        CancelSelection(); // 다른 카드의 대상 선택을 지운 뒤 실제 카드 비용 입력
        wheelPaymentSource = source; // 화면 후보와 실제 사용할 원본 연결
        wheelPaymentAmount = sharedActionPoints.CurrentActionPoints; // 후보를 열 실제 행동력과 필요 장수 보관
        if (!CanContinueWheelPayment()) // 입력 잠금 알림 뒤에도 같은 실제 비용과 후보가 유효한지 확인
        { // 알림 도중 바뀐 개시 후보 정리 시작
            CloseWheelPayment(); // 유효한 손패 대신 낡은 선택을 보여 주지 않는 취소
            return; // 변경된 가변 비용에서 모달 생성 생략
        } // 알림 도중 바뀐 개시 후보 정리 종료
        handView.SetInteractionLocked(true); // 선택 중 일반 손패 및 턴과 도주 입력 화면 잠금
        ShowWheelPaymentChoices(canvas); // 현재 실제 손패에 있는 별도 비용 카드 표시
    } // 수레바퀴 선택 준비 종료
    private void ShowWheelPaymentChoices(Canvas canvas) // 아직 고르지 않은 실물 카드만 선택 화면에 표시
    { // 다음 실제 비용 선택 시작
        var candidates = new List<CardInstance>(); // 현재 선택 가능한 실물 손패 카드 목록
        foreach (var card in runtimeDeck.Hand) // 지원 카드의 실제 손패 순서로 비용 후보 순회
        { // 별도의 실물 카드 확인 시작
            if (card != wheelPaymentSource && !wheelPaymentCards.Contains(card)) // 사용할 원본과 앞서 고른 카드의 중복 방지
            { // 실제 다른 카드 후보 추가 시작
                candidates.Add(card); // 손패의 물리 카드 한 장 등록
            } // 실제 다른 카드 후보 추가 종료
        } // 별도의 실물 카드 확인 종료
        int step = wheelPaymentCards.Count; // 현재 새 모달에 대응하는 실제 선택 단계 보관
        string title = $"운명의 수레바퀴 · 제외할 카드 {step + 1}/{wheelPaymentAmount}"; // 필요 장수와 현재 선택을 보여 주는 안내
        wheelPaymentView = BattleDrawChoiceView.Show(canvas.transform, candidates, choice => SelectWheelPaymentCard(canvas, choice, step), title, CloseWheelPayment); // 비용 선택과 취소를 기존 모달 입력에 연결
    } // 다음 실제 비용 선택 종료
    private void SelectWheelPaymentCard(Canvas canvas, CardInstance choice, int step) // 실제 단계의 손패 비용 카드 한 장 선택
    { // 다음 물리 카드 선택 정산 시작
        if (wheelPaymentSource == null || step != wheelPaymentCards.Count) // 이미 종료되거나 앞선 화면의 낡은 입력 확인
        { // 이전 선택 이벤트 차단 시작
            return; // 결제에 오래된 버튼 입력 사용 방지
        } // 이전 선택 이벤트 차단 종료
        wheelPaymentView = null; // 이미 자체 종료된 현재 화면 참조 초기화
        if (!CanContinueWheelPayment() || choice == wheelPaymentSource || !ContainsHandCard(choice) || wheelPaymentCards.Contains(choice)) // 현재 비용과 실제 다른 손패 후보의 유효성 확인
        { // 변경된 원본 또는 비용 입력 처리 시작
            CloseWheelPayment(); // 어떤 비용도 쓰지 않은 원래 손패 상태로 복귀
            return; // 전체 카드 비용 결제 방지
        } // 변경된 원본 또는 비용 입력 처리 종료
        wheelPaymentCards.Add(choice); // 실제로 움직이지 않은 후보를 선택 목록에만 추가
        if (wheelPaymentCards.Count < wheelPaymentAmount) // 아직 더 골라야 할 실제 비용 장수 확인
        { // 다음 카드 선택 시작
            ShowWheelPaymentChoices(canvas); // 앞서 고른 실제 카드를 제외한 후보로 다음 모달 표시
            return; // 전체 비용이 모이기 전 실제 결제 생략
        } // 다음 카드 선택 종료
        var source = wheelPaymentSource; // 입력 잠금을 해제한 뒤 사용할 실제 원본 보관
        var payment = wheelPaymentCards.ToArray(); // 완료한 정확한 실물 카드 선택을 제어 흐름에 고정
        CloseWheelPayment(); // 실제 카드 처리 전에 선택 상태와 모달 잠금 정리
        ExecuteCard(source, CollectValidTargets(source, allyUnitViews), payment); // 현재 유효 아군과 모두 고른 비용으로 원래 카드 사용 확정
    } // 다음 물리 카드 선택 정산 종료
    private bool CanContinueWheelPayment() // 비용 선택 중 실제 턴 및 손패 상태 확인
    { // 유효한 가변 비용 유지 확인 시작
        return turnRuntime.IsPlayerTurn && !turnRuntime.IsBattleEnded && !turnRuntime.HasPendingBossPhaseTransition && // 정상 사용이 가능한 전투 상태와 실제 아군 턴
            wheelPaymentSource != null && !wheelPaymentSource.OwnerUnit.IsDead && ContainsHandCard(wheelPaymentSource) && // 선택이 끝날 때도 기존 원본과 소유자 유지
            sharedActionPoints.CurrentActionPoints == wheelPaymentAmount && turnRuntime.HasHandPaymentOptions(wheelPaymentAmount) && // 여전히 같은 실제 행동력과 충분한 다른 손패
            wheelPaymentCards.TrueForAll(ContainsHandCard); // 이미 고른 모든 물리 카드의 현재 손패 확인
    } // 유효한 가변 비용 유지 확인 종료
    private void RecheckWheelPayment() // 비용 후보가 대기하는 동안의 실제 자원과 턴 변경 확인
    { // 중간 변경 확인 시작
        if (wheelPaymentSource != null && !CanContinueWheelPayment()) // 실제 선택을 마치기 전 자원 또는 원본이 변경되었는지 확인
        { // 더 이상 유효하지 않은 선택 정리 시작
            CloseWheelPayment(); // 부분 선택이나 선택 조건 변경에는 어떠한 비용도 지불하지 않는 취소
        } // 더 이상 유효하지 않은 선택 정리 종료
    } // 중간 변경 확인 종료
    private void CloseWheelPayment() // 실제 가변 비용 선택 또는 취소의 입력 잠금 정리
    { // 원래 일반 턴 입력 복구 시작
        if (wheelPaymentSource == null) // 이미 정리된 다른 카드 상태 확인
        { // 다른 모달 잠금 유지 시작
            return; // 수레바퀴 원본이 없으면 기존 다른 선택 상태 보존
        } // 다른 모달 잠금 유지 종료
        wheelPaymentSource = null; // 상태 이벤트의 재진입 전에 선택 원본 해제
        wheelPaymentAmount = 0; // 이전 선택의 실제 비용 수치 초기화
        wheelPaymentCards.Clear(); // 실제 비용 후보가 이미 넘어갔거나 취소된 뒤 선택 목록 제거
        if (wheelPaymentView != null && wheelPaymentView.gameObject.activeSelf) // 기존 모달에서 아직 열린 실제 선택창 확인
        { // 남은 모달 닫기 시작
            wheelPaymentView.Close(); // 더 이상 유효하지 않은 선택창 즉시 숨기기
        } // 남은 모달 닫기 종료
        wheelPaymentView = null; // 종료한 선택 모달 참조 해제
        turnRuntime.EndPlayerCardDecision(); // 다른 일반 전투 입력의 실제 잠금 해제
        handView.SetInteractionLocked(false); // 원래 손패와 턴 종료 및 도주 버튼 상태 복구
    } // 원래 일반 턴 입력 복구 종료
    private void RefreshPartnerMarkers() // 파트너 화면 표시 갱신
    { // 파트너 표시 갱신 시작
        foreach (BattleUnitView view in allyUnitViews) // 아군 화면 목록 순회
        { // 개별 파트너 표시 시작
            if (view != null) // 아군 화면 존재 확인
            { // 유효 화면 처리 시작
                view.SetPartnerMarker(view.RuntimeUnit == loversPartner.Partner, loversPartner.PartnerUsedCardThisTurn); // 현재 파트너와 카드 사용 상태 적용
            } // 유효 화면 처리 종료
        } // 개별 파트너 표시 종료
    } // 파트너 표시 갱신 종료
    private void HandleUnitDied(BattleUnitRuntime runtimeUnit) // 유닛 사망 처리
    { // 유닛 사망 처리 시작
        if (pentacleHandChoiceSource?.OwnerUnit == runtimeUnit) // 선택 중 실제 시전자의 전투불능 확인
        { // 미지불 손패 선택 종료 시작
            ClosePentacleHandChoice(); // 사망한 원본의 모달과 턴 입력 잠금 제거
        } // 미지불 손패 선택 종료 완료
        if (manaRetrievalSource?.OwnerUnit == runtimeUnit) // 아직 확정하지 않은 회수 카드 시전자의 실제 사망 확인
        { // 회수 선택을 계속할 수 없는 상황 시작
            CloseCardChoice(); // 행동력과 별도 지팡이 이동 없이 선택 닫기
        } // 회수 선택을 계속할 수 없는 상황 종료
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
        if (selectedCard == null) // 현재 대상 입력을 기다리는 카드 확인
        { // 미선택 처리 시작
            return; // 대상 선택 갱신 생략
        } // 미선택 처리 종료
        bool selectingSplitBarrier = selectedCard.SourceData.ShieldRule == ShieldCardRule.SplitBarrier; // 분할 방벽 선택 중인지 확인
        bool selectingEnemyAndAlly = selectedCard.TargetType == CardTargetType.EnemyAndAlly; // 적과 아군의 순차 선택 중인지 확인
        if (selectedCard.OwnerUnit == runtimeUnit || firstSequentialTarget == runtimeUnit || // 소유자 또는 확정한 첫 대상의 사망 확인
            selectingSplitBarrier && !turnRuntime.HasOtherLivingAlly(selectedCard.OwnerUnit)) // 분할 방벽의 생존 대상 부족 확인
        { // 카드 선택 취소 시작
            CancelSelection(); // 미확정 대상과 카드 선택 초기화
            return; // 대상 강조 재적용 생략
        } // 카드 선택 취소 종료
        if (selectedCard.TargetType == CardTargetType.BlessingSourceAndAlly) // 축복 이전의 남은 후보 확인
        { // 사망 후 축복 대상 갱신 시작
            RefreshBlessingTransferTargets(); // 생존한 실제 복사 조합에 맞게 단계 갱신
            return; // 다른 카드의 대상 단계 검사 생략
        } // 사망 후 축복 대상 갱신 종료
        if (selectedCard.TargetType == CardTargetType.SelectedTwoEnemies && runtimeUnit.Team == BattleTeam.Enemy) // 순차 적 선택 중에 적이 사망했는지 확인
        { // 시작 때 확정한 대상 수 유지 시작
            if (turnRuntime.CountSelectableEnemyPair() < requiredSelectedEnemies) // 현재 후보로 원래 필요했던 적 수를 채우지 못하는지 확인
            { // 불완전한 입력 취소 시작
                CancelSelection(); // 처음 고른 대상 수를 줄이지 않고 미결제 선택 종료
            } // 불완전한 입력 취소 종료
            else // 원래 필요한 생존 대상 수를 아직 충족하는 경우
            { // 남은 후보 강조 시작
                UpdateTargetHighlights(); // 다른 생존 적들의 현재 선택 가능 표시
            } // 남은 후보 강조 종료
            return; // 별도 순차 카드의 단계 처리 생략
        } // 시작 때 확정한 대상 수 유지 종료
        if (selectingEnemyAndAlly || selectingSplitBarrier && runtimeUnit.Team == BattleTeam.Ally) // 두 번째 대상 후보의 생존 변화 확인
        { // 남은 선택 대상 처리 시작
            UpdateTargetHighlights(); // 생존 후보 강조 갱신
        } // 남은 선택 대상 처리 종료
    } // 유닛 사망 처리 종료
    private void HandleUnitHealthChanged(BattleUnitRuntime runtimeUnit) // 체력 대가와 처형 대상에 따른 손패 갱신
    { // 아군 체력 변경 처리 시작
        if (selectedCard != null && (!SwordCardCombat.CanUse(selectedCard, sharedActionPoints, turnRuntime) || selectedCard.OwnerUnit == runtimeUnit && DevilBloodPrice.IsBloodPriceCard(selectedCard) && !DevilBloodPrice.CanPay(selectedCard))) // 선택된 체력 비용 카드의 지불 가능 여부 확인
        { // 선택 취소 처리 시작
            CancelSelection(); // 체력 부족 카드 선택 해제
        } // 선택 취소 처리 종료
        if (selectedCard?.TargetType == CardTargetType.FallenAlly || selectedCard?.SourceData.WandRule == WandCardRule.ShockTransfer || selectedCard?.SourceData.SwordRule == SwordCardRule.Finisher || selectedCard?.SourceData.SwordRule == SwordCardRule.ExecutionSlash || selectedCard?.SourceData.SwordRule == SwordCardRule.BloodTrail || selectedCard?.SourceData.SwordRule == SwordCardRule.JudgmentSlash) // 체력과 사망에 따라 바뀌는 카드 대상 확인
        { // 대상 범위 갱신 시작
            UpdateTargetHighlights(); // 현재 체력에 맞춰 선택 가능한 적 표시
        } // 대상 범위 갱신 종료
        if (selectedCard?.TargetType == CardTargetType.BlessingSourceAndAlly) // 축복 이전의 강화 변경이나 생존 변화 확인
        { // 활성 축복 대상 갱신 시작
            RefreshBlessingTransferTargets(); // 원본 강화의 만료와 수령 중첩 변화 적용
        } // 활성 축복 대상 갱신 종료
        handView.RefreshCardAvailability(); // 변경된 체력 기준 손패 사용 가능 상태 갱신
    } // 아군 체력 변경 처리 종료
    private void RefreshBlessingTransferTargets() // 두 단계의 현재 복사 가능 대상 갱신
    { // 축복 이전 후보 갱신 시작
        bool hasTargets = firstSequentialTarget == null ? BattleBlessingTransfer.HasTargets(turnRuntime) : BattleBlessingTransfer.CanChooseSource(turnRuntime, firstSequentialTarget); // 첫 단계와 확정한 원본의 유효한 조합 확인
        if (!hasTargets) // 복사 가능한 관계가 사라진 경우 확인
        { // 미확정 축복 이전 취소 시작
            CancelSelection(); // 사용 비용 없이 잘못된 대상 선택 해제
            return; // 무효 후보 강조 갱신 생략
        } // 미확정 축복 이전 취소 종료
        UpdateTargetHighlights(); // 남아 있는 실제 복사 후보만 강조
    } // 축복 이전 후보 갱신 종료
    internal static int GetPentacleHandChoiceCount(CardInstance card) // 두 실제 손패 선택 카드의 대상 수 확인
    { // 실제 원본 효과와 고를 장수 구분 시작
        return card?.EffectType == CardEffectType.PentacleUtility ? card.SourceData.PentacleRule switch // 원본의 명시된 펜타클 지원 효과 확인
        { // 손패 선택 규칙 구분 시작
            PentacleCardRule.TacticalExchange => 2, // 다른 실물 두 장을 고르는 전술 교환
            PentacleCardRule.EnchantCard => 1, // 다른 실물 한 장을 고르는 카드 강화
            PentacleCardRule.CardCopy => 1, // 이번 실제 턴에 완료한 다른 실물 한 장 복제
            _ => 0 // 즉시 사용하거나 각자 별도 입력을 하는 다른 카드
        } : 0; // 다른 종류 카드의 기존 사용 경로
    } // 실제 원본 효과와 고를 장수 구분 종료

    private void ShowPentacleHandChoice(CardInstance source) // 확정 전 다른 실제 손패 후보 선택 창
    { // 손패 선택의 모달 시작 전 조건 검사
        var canvas = handView.GetComponentInParent<Canvas>(); // 기존 선택 화면을 연결할 전투 캔버스
        if (canvas == null || GetPentacleChoiceCandidates(source).Count < GetPentacleHandChoiceCount(source) || !turnRuntime.TryBeginPlayerCardDecision()) // 실제 후보 수와 중첩 없는 공용 입력 잠금 확인
        { // 무효 손패 선택 시작 생략
            return; // 행동력과 원래 카드 목록 보존
        } // 무효 손패 선택 시작 생략 종료
        CancelSelection(); // 남아 있는 다른 카드의 대상 강조 초기화
        pentacleHandChoiceSource = source; // 잠금을 연 실제 원본을 현재 선택에 기록
        handView.SetInteractionLocked(true); // 다른 손패와 턴 종료 조작을 선택하는 동안 차단
        OpenPentacleHandChoice(canvas, source); // 첫 선택 또는 사전에 정해진 단일 선택 표시
    } // 손패 선택의 모달 시작 전 조건 검사 종료

    private void OpenPentacleHandChoice(Canvas canvas, CardInstance source) // 다른 인스턴스만 보여 주는 각 선택 단계
    { // 실제 현재 손패 후보 준비 시작
        var candidates = GetPentacleChoiceCandidates(source); // 현재 모달의 실제 손패 또는 이번 턴 사용 후보
        int stillNeeded = GetPentacleHandChoiceCount(source) - (firstTacticalDiscard == null ? 0 : 1); // 현재 단계에서 필요한 서로 다른 실물 수
        if (candidates.Count < stillNeeded) // 항상 시작할 때 정한 필요 장수를 채울 수 있는지 확인
        { // 현재 손패 후보 부족 처리 시작
            ClosePentacleHandChoice(); // 비용과 효과 없이 전체 선택 취소
            return; // 부족한 후보로 선택을 이어 가지 않음
        } // 현재 손패 후보 부족 처리 종료
        string title = source.SourceData.PentacleRule switch // 현재 모달에 필요한 실제 출처와 장수 표시
        { // 입력할 카드 종류별 원래 표시 시작
            PentacleCardRule.CardCopy => "이번 턴에 사용한 카드 선택", // 완료한 사용 기록의 실제 후보 안내
            PentacleCardRule.EnchantCard => "강화할 다른 카드 선택", // 다음 효과가 강화될 실제 손패 안내
            _ => firstTacticalDiscard == null ? "버릴 첫 번째 카드 선택" : "버릴 두 번째 카드 선택" // 서로 다른 두 비용 카드 단계 구분
        }; // 입력할 카드 종류별 원래 표시 종료
        BattleDrawChoiceView opened = null; // 뒤늦은 이전 선택 신호를 거를 현재 모달 참조
        opened = BattleDrawChoiceView.Show(canvas.transform, candidates, candidate => FinishPentacleHandChoice(opened, canvas, source, candidate), title, // 실제 선택한 손패의 현재 시점 확인과 단계 안내
            () => FinishPentacleHandChoice(opened, canvas, source, null)); // 선택 단계 양쪽의 무지불 취소 연결
        pentacleHandChoiceView = opened; // 실제 선택 창을 현재 전투에 연결
    } // 실제 현재 손패 후보 준비 종료

    private List<CardInstance> GetPentacleChoiceCandidates(CardInstance source) // 원래 손패와 실제 이번 턴 사용 후보 조회
    { // 원본에 맞는 후보 영역 선택 시작
        IReadOnlyList<CardInstance> possible = source.SourceData.PentacleRule == PentacleCardRule.CardCopy ? usedCardsThisTurn : runtimeDeck.Hand; // 사용 기록 카드는 손패에서 사라져도 원래 후보 유지
        var candidates = new List<CardInstance>(); // 현재 승인할 다른 실제 카드 목록
        foreach (var candidate in possible) // 실제 원본 영역의 기존 후보 순서 순회
        { // 현재 고를 수 있는 다른 실물인지 확인
            if (CanChoosePentacleCard(source, candidate)) // 후보의 현재 유효 영역과 원래 카드 제외 확인
            { // 모달에 실제 선택 후보 추가 시작
                candidates.Add(candidate); // 정상 완료하거나 실제 손패에 남은 같은 실물만 추가
            } // 모달에 실제 선택 후보 추가 종료
        } // 현재 고를 수 있는 다른 실물인지 확인 종료
        return candidates; // 원래 순서의 안전한 모달 후보 반환
    } // 원본에 맞는 후보 영역 선택 종료

    private bool CanChoosePentacleCard(CardInstance source, CardInstance candidate) // 카드별 확정 직전 실제 후보 재검증
    { // 원본과 다른 실물 조건 확인 시작
        if (candidate == null || candidate == source || candidate == firstTacticalDiscard) // 무효 카드와 원본 및 이미 고른 카드 제외
        { // 중복 및 빈 후보 처리 시작
            return false; // 같은 실물의 자기 선택 차단
        } // 중복 및 빈 후보 처리 종료
        return source.SourceData.PentacleRule == PentacleCardRule.CardCopy // 현재 카드가 사용 내역을 참조하는지 확인
            ? usedCardsRound == turnRuntime.CurrentRound && usedCardsThisTurn.Contains(candidate) // 이번 턴의 실제 완료 이력에 같은 실물인지 확인
            : ContainsHandCard(candidate); // 기존 다른 실물의 정상 손패 확인
    } // 원본과 다른 실물 조건 확인 종료

    private void FinishPentacleHandChoice(BattleDrawChoiceView opened, Canvas canvas, CardInstance source, CardInstance candidate) // 실제 모달 확인과 취소 및 다음 단계
    { // 닫힌 이전 모달의 이벤트를 제외하는 현재 단계 처리 시작
        if (opened != pentacleHandChoiceView || source != pentacleHandChoiceSource) // 현재 실제 모달과 원본 카드 일치 확인
        { // 오래된 모달 이벤트 생략 시작
            return; // 지금의 손패나 선택 상태 유지
        } // 오래된 모달 이벤트 생략 종료
        pentacleHandChoiceView = null; // 선택한 이전 창을 다시 닫지 않도록 참조 정리
        if (!CanChoosePentacleCard(source, candidate) || !ContainsHandCard(source) || source.OwnerUnit.IsDead) // 원본과 선택 실물 및 생존 시전자의 최신 상태 확인
        { // 취소 또는 무효 선택 처리 시작
            ClosePentacleHandChoice(); // 지불이나 버림 없이 입력 잠금 복구
            return; // 정상 카드 실행을 시작하지 않는 전체 취소
        } // 취소 또는 무효 선택 처리 종료
        if (GetPentacleHandChoiceCount(source) == 2 && firstTacticalDiscard == null) // 전술 교환 첫 장의 미지급 선택 확인
        { // 같은 화면 규칙으로 다른 두 번째 카드 선택 시작
            firstTacticalDiscard = candidate; // 원본 손패의 동일 실물과 다른 후보만 남기기
            OpenPentacleHandChoice(canvas, source); // 비용을 내지 않은 상태로 두 번째 후보 표시
            return; // 최종 확인 이후에만 실제 카드 사용
        } // 같은 화면 규칙으로 다른 두 번째 카드 선택 종료
        var choices = firstTacticalDiscard == null ? new[] { candidate } : new[] { firstTacticalDiscard, candidate }; // 실제 사용할 때 재검사할 고유 실물 선택 확보
        ClosePentacleHandChoice(); // 실제 공용 턴 입력 복구 후 일반 카드 사용 진입
        ExecuteCard(source, CreateSingleTargetList(source.OwnerUnit), pentacleHandCards: choices); // 모든 실제 후보를 확인한 정상 자기 대상 사용
    } // 닫힌 이전 모달의 이벤트를 제외하는 현재 단계 처리 종료

    private void ClosePentacleHandChoice() // 미지불 펜타클 손패 모달과 실제 입력 상태 해제
    { // 현재 모달을 소유한 경우만 잠금 해제 시작
        if (pentacleHandChoiceSource == null) // 다른 모달에는 손대지 않을 실제 소유 확인
        { // 우리 선택이 없을 때 처리 시작
            return; // 기존 카드 사용과 입력 상태 유지
        } // 우리 선택이 없을 때 처리 종료
        var opened = pentacleHandChoiceView; // 정상 닫을 화면 참조 확보
        pentacleHandChoiceView = null; // 잠금 변경 이벤트 전에 선택 화면 참조 제거
        pentacleHandChoiceSource = null; // 다시 호출돼도 현재 사용을 중복 종료하지 않는 원본 해제
        firstTacticalDiscard = null; // 첫 선택만 된 카드의 미정 상태 해제
        opened?.Close(); // 화면에서 이전 미지불 후보 숨기기
        turnRuntime.EndPlayerCardDecision(); // 현재 턴의 공유 카드 선택 잠금 반환
        handView.SetInteractionLocked(false); // 다른 손패와 정상 전투 입력 복구
    } // 현재 모달을 소유한 경우만 잠금 해제 종료

    private void ExecuteCard(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits, IReadOnlyList<CardInstance> wheelPayment = null, IReadOnlyList<CardInstance> pentacleHandCards = null) // 일반 사용과 확정한 두 종류 손패 선택 비용 전달
    { // 카드 실행 시작
        if (!turnRuntime.CanAcceptPlayerInput || actionPending || actionSequenceRunner.IsBusy) // 개막 선택 완료와 플레이어 턴과 연출 상태 확인
        { // 실행 불가 처리 시작
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 실행 불가 처리 종료
        if (cardInstance?.SourceData.WandRule == WandCardRule.BurnAmplification) // 화상 연장은 확정 시점의 모든 활성 화상 적 확인
        { // 현재 생존 화상 적 재수집 시작
            targetUnits = CollectValidTargets(cardInstance, enemyUnitViews); // 비용을 내기 전에 현재 상태로 다시 자동 지정
        } // 현재 생존 화상 적 재수집 종료
        if (!ContainsHandCard(cardInstance) || targetUnits == null || targetUnits.Count < 1) // 카드와 대상 유효성 확인
        { // 실행 불가 처리 시작
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 실행 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.ReviveAlly && (targetUnits.Count != 1 || !IsValidTarget(cardInstance, targetUnits[0]))) // 비용 지불 직전 부활 대상 한 명 재검사
        { // 부활 대상 변경 처리 시작
            CancelSelection(); // 비용 차감 전 카드 선택 취소
            return; // 부활 행동 중단
        } // 부활 대상 변경 처리 종료
        if (cardInstance.SourceData.WandRule is (WandCardRule.EmberSpread or WandCardRule.ShockTransfer or WandCardRule.FateFreeze) && // 한 적을 대상으로 하는 원소 카드의 확정 확인
            (targetUnits.Count != 1 || !IsValidTarget(cardInstance, targetUnits[0]))) // 비용 지불 직전 실제 대상과 필수 활성 상태 재확인
        { // 불씨 확산 대상 변경 처리 시작
            CancelSelection(); // 화상이 만료되거나 해제된 대상의 입력 취소
            return; // 비용 차감과 카드 이동 없이 실행 중단
        } // 불씨 확산 대상 변경 처리 종료
        if (cardInstance.TargetType == CardTargetType.EnemyAndAlly && // 적과 아군의 유효성을 비용 지불 직전에 재검사
            (cardInstance.EffectType != CardEffectType.Damage || cardInstance.OwnerUnit.IsDead || targetUnits.Count != 2 || // 올바른 카드 처리와 소유자 생존 및 두 대상 확인
            !turnRuntime.CanSelectLivingUnit(targetUnits[0], BattleTeam.Enemy) || !turnRuntime.CanSelectLivingUnit(targetUnits[1], BattleTeam.Ally))) // 서로 다른 진영의 두 생존 참가자 확인
        { // 적과 아군의 대상 변경 처리 시작
            CancelSelection(); // 비용 지불 전에 미확정 입력 제거
            return; // 잘못된 복합 카드 실행 중단
        } // 적과 아군의 대상 변경 처리 종료
        if ((cardInstance.TargetType == CardTargetType.BlessingSourceAndAlly || cardInstance.EffectType == CardEffectType.CopyAllyBlessing) && // 구성 요소 하나만 설정한 원본도 검사
            (!BattleBlessingTransfer.IsTransferCard(cardInstance) || cardInstance.OwnerUnit.IsDead || targetUnits.Count != 2 || BattleBlessingTransfer.FindBlessing(turnRuntime, targetUnits[0], targetUnits[1]) == null)) // 결제 전에 생존자와 실제 복사 가능 상태 재확인
        { // 복사 대상 검증 실패 시작
            CancelSelection(); // 지불 전에 잘못된 두 대상 입력 정리
            return; // 유효한 강화가 없는 카드 실행 중단
        } // 복사 대상 검증 실패 종료
        if (cardInstance.TargetType == CardTargetType.SelectedTwoEnemies && !CanConfirmSelectedEnemyPair(cardInstance, targetUnits)) // 지불 직전 지정한 적의 생존과 서로 다름 재확인
        { // 더 이상 대상이 유효하지 않은 경우 시작
            CancelSelection(); // 비용을 내지 않고 모든 미확정 입력 제거
            return; // 두 적 선택 카드의 실행과 비용 처리 중단
        } // 더 이상 대상이 유효하지 않은 경우 종료
        if (cardInstance.SourceData.ShieldRule == ShieldCardRule.SplitBarrier && // 분할 방벽의 비용 지불 직전 검사
            (cardInstance.OwnerUnit.IsDead || targetUnits.Count != 2 || targetUnits[0] == targetUnits[1] || // 소유자 생존과 서로 다른 대상 수 확인
            !IsValidTarget(cardInstance, targetUnits[0]) || !IsValidTarget(cardInstance, targetUnits[1]))) // 두 대상의 진영과 생존 상태 확인
        { // 두 대상 미확정 처리 시작
            CancelSelection(); // 비용 차감 전 카드와 대상 선택 취소
            return; // 분할 방벽 효과 실행 중단
        } // 두 대상 미확정 처리 종료
        if (cardInstance.EffectType == CardEffectType.DiscardWandForActionPoint && (!IsDistinctManaRetrievalWand(cardInstance, manaRetrievalDiscard) || !ContainsHandCard(manaRetrievalDiscard))) // 사용 확정 전에 지팡이 후보가 여전히 물리 손패에 있는지 확인
        { // 원래 회수 선택 무효화 처리 시작
            CancelSelection(); // 카드 이동과 결제 전 선택 정리
            return; // 유효한 별도 지팡이가 없는 회수 사용 거절
        } // 원래 회수 선택 무효화 처리 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.RunePower && (targetUnits.Count != 1 || !IsValidTarget(cardInstance, targetUnits[0]))) // 결제 직전에 실제 룬 대상과 손패 후보 재확인
        { // 대상을 잃은 경우의 무결제 취소 시작
            CancelSelection(); // 유효 후보가 없으면 공용 행동력 보존
            return; // 무효 룬 카드 사용 중단
        } // 대상을 잃은 경우의 무결제 취소 종료
        int handChoices = GetPentacleHandChoiceCount(cardInstance); // 결제 전에 실제로 필요한 펜타클 선택 수
        if (handChoices > 0 && (pentacleHandCards == null || pentacleHandCards.Count != handChoices || // 정상 모달을 통과한 정확한 수의 실제 입력 확인
            !CanChoosePentacleCard(cardInstance, pentacleHandCards[0]) || handChoices == 2 && // 원본을 제외한 실제 첫 카드 위치 검사
            (pentacleHandCards[1] == cardInstance || pentacleHandCards[1] == pentacleHandCards[0] || !ContainsHandCard(pentacleHandCards[1])))) // 두 번째 카드의 물리 손패와 고유성 재검사
        { // 유효한 원본 선택이 없는 지불 취소 시작
            CancelSelection(); // 다른 손패와 행동력 상태를 건드리지 않고 정리
            return; // 불완전한 선택의 카드 실행 생략
        } // 유효한 원본 선택이 없는 지불 취소 종료
        if (!IsSupportedEffect(cardInstance.EffectType)) // 지원 효과 확인
        { // 미지원 효과 처리 시작
            Debug.LogError($"[BattleCardActionController] 지원하지 않는 카드 효과입니다: {cardInstance.EffectType}"); // 미지원 효과 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 미지원 효과 처리 종료
        BattleUnitView actorView = FindUnitView(cardInstance.OwnerUnit); // 카드 행동자 화면 조회
        List<BattleUnitView> targetViews = CollectUnitViews(targetUnits); // 카드 대상 화면 목록 생성
        if (!actionSequenceRunner.CanStartAction(actorView, targetViews, cardInstance.EffectType == CardEffectType.ReviveAlly)) // 부활 대상 상태를 포함한 연출 시작 조건 확인
        { // 연출 시작 불가 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 카드 행동 연출을 시작할 수 없습니다: {cardInstance.DisplayName}"); // 연출 시작 실패 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 연출 시작 불가 처리 종료
        int cost = cardInstance.SourceData.SpendsAllActionPoints ? sharedActionPoints.CurrentActionPoints : cardInstance.ApCost; // 실제 확정 순간의 일반 또는 가변 비용 결정
        bool usedFirstDefenseDiscount = cardInstance.EffectiveFirstDefenseDiscount > 0; // 다른 유물 비용이 바뀌기 전에 실제 적용한 방어 할인 기록
        bool wheel = cardInstance.SourceData.PentacleRule == PentacleCardRule.WheelOfFate; // 실제 다른 손패와 공용 행동력의 동시 결제 여부
        if (!SwordCardCombat.CanUse(cardInstance, sharedActionPoints, turnRuntime)) // 현재 대상 및 원본 비용의 실제 결제 가능 여부 확인
        { // 실제 결제 불가 시 선택 정리 시작
            CancelSelection(); // 행동력과 카드 이동 없이 유효하지 않은 입력 정리
            return; // 남은 실제 효과와 비용 처리 생략
        } // 실제 결제 불가 시 선택 정리 종료
        actionPending = wheel || handChoices > 0; // 실제 일괄 손패 비용과 행동력 알림 동안 다른 카드 재진입 차단
        bool paid = wheel ? runtimeDeck.TrySpendAndExcludeHandPayment(cardInstance, wheelPayment, sharedActionPoints, cost) : // 수레바퀴의 고유 실제 제외 결제
            handChoices == 2 ? runtimeDeck.TrySpendAndDiscardHandPair(cardInstance, pentacleHandCards[0], pentacleHandCards[1], sharedActionPoints, cost) : // 전술 교환의 두 실물 버림과 행동력 결제
            sharedActionPoints.Spend(cost); // 카드 강화와 다른 기존 카드의 일반 행동력 지불
        if (!paid) // 현재 실제 비용이 정상적으로 지불됐는지 확인
        { // 비용 차감 실패 처리 시작
            actionPending = false; // 실패한 일괄 비용의 카드 실행 잠금 복구
            Debug.LogWarning($"[BattleCardActionController] 카드 비용 차감에 실패했습니다: {cardInstance.DisplayName}"); // 비용 차감 실패 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 비용 차감 실패 처리 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.EnchantCard && handChoices == 1) // 복제 선택과 별개인 용도의 실제 카드 강화 확인
        { // 확정한 다른 실물의 다음 효과 예약 시작
            pentacleHandCards[0].SetEffectEnchantment(cardInstance.EffectValue); // 현재 전투의 다른 실물 카드에만 단일 강화 저장
        } // 확정한 다른 실물의 다음 효과 예약 종료
        if (cardInstance.TargetType == CardTargetType.RandomTwoEnemies) // 사용을 확정한 카드의 명시적 무작위 적 대상 확인
        { // 실제 확정 대상 뽑기 시작
            targetUnits = PickDistinctRandomEnemies(targetUnits); // 현재 유효한 생존 후보만 최대 두 명 확정
            targetViews = CollectUnitViews(targetUnits); // 확정한 각 적의 화면에만 실제 연출 연결
        } // 실제 확정 대상 뽑기 종료
        int healthCost = cardInstance.SourceData.CupRule == CupCardRule.MartyrsLight // 순교자의 빛의 시전자 체력 한 점 규칙 확인
            ? cardInstance.OwnerUnit.CurrentHealth - 1 // 생존 하한까지의 실제 체력 비용 계산
            : SwordCardCombat.HasUpfrontHealthCost(cardInstance) ? cardInstance.SourceData.SecondaryEffectValue : 0; // 기존 고정 체력 비용과 일반 카드 구분
        if (healthCost > 0) // 지불할 실제 체력 대가 확인
        { // 실제 체력 대가 지불 시작
            if (!cardInstance.OwnerUnit.TrySpendHealth(healthCost)) // 방어도를 우회한 생존 체력 비용의 실제 지불 확인
            { // 체력 비용 지불 실패 시작
                sharedActionPoints.Gain(cost); // 지불 상태 변경으로 취소된 사용의 행동력 복원
                CancelSelection(); // 지불할 수 없는 카드와 대상 선택 정리
                return; // 효과나 카드 이동 시작 방지
            } // 체력 비용 지불 실패 종료
            actorView.ShowStatusFeedback($"체력 -{healthCost}", true); // 카드 소유자의 실제 체력 소모 표시
        } // 실제 체력 대가 지불 종료
        if (cardInstance.SourceData.CupRule == CupCardRule.LifeContract) // 생명의 계약을 실제로 사용한 시전자 확인
        { // 원본 정신력 비용 지불 시작
            cardInstance.OwnerUnit.ChangeMental(cardInstance.SourceData.MentalChangeValue, BattleMentalChangeReason.CardEffect); // 치유에 앞선 기존 정신력 하한과 상태 변화 적용
        } // 원본 정신력 비용 지불 종료
        preparedSwordDamage = 0; // 이전 카드의 칼날 정비 보너스 정리
        if (cardInstance.CardType == CardType.Sword && cardInstance.EffectType == CardEffectType.Damage) // 실제 비용을 지불한 검 피해 카드 확인
        { // 칼날 정비 소모 시작
            var preparation = cardInstance.OwnerUnit.GetStatusEffect(BattleStatusEffectType.BladeMaintenance); // 이번 카드 소유자의 준비 상태 조회
            if (preparation != null) // 준비된 검 확인
            { // 추가 피해 보관 시작
                preparedSwordDamage = preparation.EffectiveValue; // 명중과 무관하게 이 카드 전체에 쓸 피해 보관
                cardInstance.OwnerUnit.RemoveStatusEffect(BattleStatusEffectType.BladeMaintenance); // 한 번 사용한 칼날 정비 제거
            } // 추가 피해 보관 종료
        } // 칼날 정비 소모 종료
        preparedWandDamage = 0; // 다른 지팡이 공격에 사용한 집중 수치 초기화
        if (cardInstance.CardType == CardType.Wand && cardInstance.EffectType == CardEffectType.Damage) // 행동력과 대상 확인을 마친 지팡이 피해 카드
        { // 실제 집중 소모 시작
            var concentration = cardInstance.OwnerUnit.GetStatusEffect(BattleStatusEffectType.ManaConcentration); // 현재 피해 카드의 원래 소유자에게 남은 집중 조회
            if (concentration is { IsExpired: false } && cardInstance.OwnerUnit.RemoveStatusEffect(BattleStatusEffectType.ManaConcentration)) // 아직 사용하지 않은 실제 집중 한 장 소모
            { // 이번 카드의 집중 수치 확보 시작
                preparedWandDamage = concentration.EffectiveValue; // 빗나감과 관계없이 이 카드의 모든 명중 수치 예약
            } // 이번 카드의 집중 수치 확보 종료
        } // 실제 집중 소모 종료
        int echoPercent = 0; // 이번 실제 카드의 준비된 독립 반향 비율 초기화
        if (turnRuntime.IsPlayerTurn && cardInstance.CardType == CardType.Wand && cardInstance.EffectType == CardEffectType.Damage && cardInstance.DamageType == BattleDamageType.Magical) // 확정 비용을 지불한 현재 아군 턴의 마법 지팡이 피해 확인
        { // 현재 카드 소유자의 반향 준비 확인 시작
            var echo = cardInstance.OwnerUnit.GetStatusEffect(BattleStatusEffectType.WandEcho); // 그 소유자에게만 남은 실제 반향 상태 확인
            if (echo is { IsExpired: false } && cardInstance.OwnerUnit.RemoveStatusEffect(BattleStatusEffectType.WandEcho)) // 원본 명중 여부와 무관하게 확정 사용에서만 한 번 소모
            { // 실제 반향 비율 기록 시작
                echoPercent = echo.EffectiveValue; // 이번 확정 카드의 전체 타격에만 사용할 실제 비율 보관
            } // 실제 반향 비율 기록 종료
        } // 현재 카드 소유자의 반향 준비 확인 종료
        (int Percent, int Paid, int Round) echoPlan = (echoPercent, cost, turnRuntime.CurrentRound); // 원본의 실제 지불량과 시전 턴을 단일 충돌에 고정
        paidActionPoints = cost; // 충돌 시점에 사용할 실제 지불 비용 보관
        List<BattleUnitRuntime> targetSnapshot = new List<BattleUnitRuntime>(targetUnits); // 연출 중 유지할 대상 목록 복사
        actionPending = true; // 카드 행동 진행 상태 저장
        CancelSelection(); // 카드 선택과 대상 강조 해제
        turnRuntime.RecordCardPayment(cardInstance, cost); // 실제 카드 사용의 행동력으로 스프링 예약 정산
        if (cardInstance.ShieldBalanceDiscount > 0) // 실제로 사용한 유료 방패의 균형 할인 확인
        { // 사용이 확정된 방패 할인 제거 시작
            cardInstance.OwnerUnit.RemoveStatusEffect(BattleStatusEffectType.ShieldBalance); // 시전자의 예약 할인 한 번 소모
        } // 사용이 확정된 방패 할인 제거 종료
        Action impactAction = () => ResolveCardImpact(cardInstance, targetSnapshot, usedFirstDefenseDiscount, echoPlan, pentacleHandCards); // 충돌에 실제 대상과 할인 및 사용한 반향 자료 전달
        bool started = actionSequenceRunner.TryStartPlayerAction(actorView, targetViews, cardInstance.EffectType, cardInstance.StatusEffectType, impactAction, HandleActionSequenceCompleted, BattleMagicMissileEffect.Matches(cardInstance)); // 기존 카드 연출 중 실제 마력탄에만 전용 효과 연결
        if (started) // 연출 시작 결과 확인
        { // 연출 시작 성공 처리 시작
            return; // 충돌 시점까지 카드 실행 대기
        } // 연출 시작 성공 처리 종료
        actionPending = false; // 카드 행동 진행 상태 해제
        Debug.LogWarning($"[BattleCardActionController] 카드 행동 연출 시작 실패로 효과를 즉시 적용합니다: {cardInstance.DisplayName}"); // 즉시 적용 안내 출력
        impactAction.Invoke(); // 카드 효과 즉시 적용
        HandleActionSequenceCompleted(); // 즉시 적용 후 선택 화면과 상태 갱신
    } // 카드 실행 종료
    private void ResolveCardImpact(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits, bool usedFirstDefenseDiscount, (int Percent, int Paid, int Round) echoPlan, IReadOnlyList<CardInstance> pentacleHandCards) // 정상 충돌 대상과 방어 및 실제 손패 선택 전달
    { // 카드 정산 범위 시작
        bool resolveEffectsBeforeVictory = cardInstance.TargetType == CardTargetType.EnemyAndAlly || cardInstance.SourceData.FollowUpEffect == CardFollowUpEffect.HealAllAllies || // 피해와 회복을 함께 처리하는 카드 확인
            cardInstance.SourceData.WandRule == WandCardRule.TimeStop || // 시간 정지의 실제 상태 효과와 한 장 드로우까지 승리 보류
            cardInstance.SourceData.PentacleRule == PentacleCardRule.FusionOfPower; // 힘의 융합의 전체 회복과 마지막 공유 드로우까지 승리 보류
        turnRuntime.BeginCardResolution(resolveEffectsBeforeVictory || cardInstance.IsBattleOnlySingleUse || GetRandomWandHitCount(cardInstance) > 0 || cardInstance.SourceData.WandRule == WandCardRule.EyeOfStorm); // 복합 카드와 연속 타격의 실제 카드 이동 뒤 승리 정산
        try // 카드 효과와 이동 완료까지 정산 범위 유지
        { // 카드 충돌 처리 시작
            ApplyCardImpact(cardInstance, targetUnits, usedFirstDefenseDiscount, echoPlan, pentacleHandCards); // 실제 전투 효과와 독립 타격 및 사용 카드 이동 처리
        } // 카드 충돌 처리 종료
        finally // 충돌 처리 종료에 예약 드로우 정리 보장
        { // 카드 정산 범위 정리 시작
            turnRuntime.EndFirstAllyAttack(); // 전부 빗나간 최초 공격도 이번 카드 종료에 남은 가산 해제
            turnRuntime.EndCardResolution(); // 비워진 손패 자리에 처치 보상 지급
        } // 카드 정산 범위 정리 종료
    } // 카드 정산 범위 종료

    private void ApplyCardImpact(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits, bool usedFirstDefenseDiscount, (int Percent, int Paid, int Round) echoPlan, IReadOnlyList<CardInstance> pentacleHandCards) // 정상 충돌 대상과 방어 및 실제 손패 선택 전달
    { // 카드 충돌 처리 시작
        if (cardInstance.EffectType == CardEffectType.DiscardWandForActionPoint && !TryDiscardForManaRetrieval(cardInstance)) // 실제로 별도 지팡이를 버린 경우에만 마력 회수 사용
        { // 사용 전에 버림 실패 처리 시작
            return; // 행동력 회복과 카드 사용 이벤트 및 원본 소모 없음
        } // 사용 전에 버림 실패 처리 종료
        bandageBonus = 0; // 직전 카드의 추가 회복 해제
        cupHealingBonusForCurrentCard = 0; // 현재 성배 카드와 이전 사용의 가산량 분리
        bool damagesAndHeals = cardInstance.TargetType == CardTargetType.EnemyAndAlly; // 직접 피해와 지정 회복을 함께 처리하는 카드 확인
        if (!usedFirstHealingCard && (damagesAndHeals || cardInstance.EffectType == CardEffectType.Heal || cardInstance.SourceData.FollowUpEffect == CardFollowUpEffect.HealAllAllies || // 첫 직접 회복 또는 명시적 후속 회복 카드 확인
            cardInstance.SourceData.PentacleRule is PentacleCardRule.WheelOfFate or PentacleCardRule.FusionOfPower or PentacleCardRule.TemporarySupplies)) // 실제 일반 회복을 제공하는 세 펜타클 효과의 기본 회복 혜택
        { // 첫 회복 카드 처리 시작
            usedFirstHealingCard = true; // 대상이 최대 체력이어도 첫 사용 기록
            bandageBonus = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.FirstHealingCardBonus)?.EffectValue ?? 0; // 실제 보유한 붕대의 대상별 가산
        } // 첫 회복 카드 처리 종료
        turnRuntime.BeginFirstAllyAttack(cardInstance); // 실제 확정 피해 카드의 첫 아군 공격 선언과 한 번 가산 예약
        suitHitRelics.BeginCard(cardInstance, turnRuntime.CurrentRound, RelicRunManager.Instance?.Inventory); // 이번 피해 카드의 첫 직접 명중 보너스 예약
        CardUsed?.Invoke(cardInstance, targetUnits); // 피해와 회복 전 카드 사용 상세 알림
        loversPartner.RecordCardUsed(cardInstance); // 파트너의 이번 턴 카드 사용 기록
        worldCoordination.RecordCardUsed(cardInstance); // 세계 연계용 동료 전용 카드 사용 기록
        if (cardInstance.SourceData.CardId == "CRD_ARC_16_SHATTER") // 탑의 광역 방어도 파괴 카드 확인
        { // 전체 적 방어도 제거 시작
            foreach (BattleUnitRuntime targetUnit in targetUnits) // 실제 공격 대상 순회
            { // 적별 방어도 제거 시작
                if (targetUnit == null || targetUnit.IsDead || targetUnit.Team != BattleTeam.Enemy) // 생존한 적 대상 확인
                { // 유효하지 않은 대상 처리 시작
                    continue; // 다음 대상 이동
                } // 유효하지 않은 대상 처리 종료
                int removed = targetUnit.RemoveBlock(cardInstance.SourceData.SecondaryEffectValue); // 피해 적용 전 적 방어도 제거
                if (removed > 0) // 실제 방어도 제거 확인
                { // 방어도 파괴 표시 시작
                    FindUnitView(targetUnit)?.ShowStatusFeedback($"방어도 -{removed}", true); // 방어도 제거 결과 표시
                } // 방어도 파괴 표시 종료
            } // 적별 방어도 제거 종료
        } // 전체 적 방어도 제거 종료
        long totalAppliedAmount = 0; // 가변 광역 회복의 대상별 실제 적용량 합산 범위
        bool defensiveCard = cardInstance.HasDefensivePrimaryEffect; // 장갑과 방패끈이 공유하는 기본 방어 효과 확인
        bool appliedDefense = false; // 살아 있는 아군에게 실제 방어 효과를 주었는지 기록
        if (damagesAndHeals) // 적에게만 피해를 주고 지정 아군에게만 회복하는 효과 분리
        { // 두 대상 충돌 처리 시작
            totalAppliedAmount = ApplyCardEffect(cardInstance, targetUnits[0]); // 선택한 적 한 명의 일반 직접 피해 처리
            int healing = GetDirectCardHealing(cardInstance, cardInstance.SourceData.SecondaryEffectValue); // 기존 회복 보정을 포함한 원본 보조 회복량 계산
            totalAppliedAmount += RestoreDirectCardHealth(cardInstance, targetUnits[1], healing); // 명중 여부와 별개로 선택한 아군 한 명의 회복 처리
        } // 두 대상 충돌 처리 종료
        else if (cardInstance.EffectType == CardEffectType.CopyAllyBlessing) // 선택한 두 아군 사이의 축복 이전 확인
        { // 실제 강화 복사 처리 시작
            var recipient = targetUnits[1]; // 두 번째로 고른 강화 수령자 조회
            var blessing = BattleBlessingTransfer.FindBlessing(turnRuntime, targetUnits[0], recipient); // 실제 효과 시점의 첫 복사 가능 강화 재확인
            if (blessing != null && !cardInstance.OwnerUnit.IsDead) // 연출 사이 사망이나 만료가 없는 강화 확인
            { // 일반 상태 경로의 복사 시작
                var result = recipient.ApplyBlessingCopy(blessing, cardInstance.OwnerUnit); // 원본을 보존한 독립 강화 적용
                FindUnitView(recipient)?.ShowStatusApplyFeedback(blessing.EffectType, result); // 복사한 강화의 실제 적용 결과 표시
                totalAppliedAmount = result is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked ? 1 : 0; // 실제로 복사한 상태 한 종류 기록
            } // 일반 상태 경로의 복사 종료
        } // 실제 강화 복사 처리 종료
        else if (cardInstance.EffectType == CardEffectType.GainBlock && cardInstance.TargetType == CardTargetType.AllAllies && !cardInstance.SourceData.GrantsBlockToEachAlly && cardInstance.SourceData.ShieldRule != ShieldCardRule.FinalRampart) // 공용 방어도만 생존 아군에게 분배
        { // 효과 적용 시작
            totalAppliedAmount = BattleBlockDistribution.Apply(targetUnits, cardInstance.EffectValue); // 고정 방어도를 생존 아군에게 배분
            appliedDefense = totalAppliedAmount > 0; // 실제 공용 방어도 분배 성공 확인
        } // 효과 적용 종료
        else if (cardInstance.SourceData.PentacleRule == PentacleCardRule.FusionOfPower) // 힘의 융합은 현재 살아 있는 두 진영에 한 번씩 적용
        { // 원본 혼합 광역 효과 정산 시작
            totalAppliedAmount = ApplyFusionOfPower(cardInstance); // 시전 이후에도 살아 있는 아군과 적의 원래 효과 정산
        } // 원본 혼합 광역 효과 정산 종료
        else if (GetRandomWandHitCount(cardInstance) > 0) // 모든 생존 적에서 매 타격 대상을 다시 고르는 두 지팡이 규칙
        { // 지팡이 무작위 연속 타격 시작
            totalAppliedAmount = ApplyRandomWandHits(cardInstance); // 현재 살아 있는 적의 독립 명중과 피해 합계
        } // 지팡이 무작위 연속 타격 종료
        else if (cardInstance.SourceData.WandRule == WandCardRule.EyeOfStorm) // 생존 적 전체를 차례마다 타격하는 전설 지팡이 확인
        { // 폭풍의 눈 광역 정산 시작
            totalAppliedAmount = ApplyEyeOfStorm(cardInstance); // 현재 생존 명단의 두 차례 실제 피해 합계
        } // 폭풍의 눈 광역 정산 종료
        else if (cardInstance.SourceData.SwordRule == SwordCardRule.SwordDance || cardInstance.SourceData.SwordRule == SwordCardRule.TwinBladeFlurry) // 지정된 적 범위에서 매 타격 재추첨
        { // 무작위 연속 공격 시작
            totalAppliedAmount = ApplyDanceHits(cardInstance, targetUnits); // 선택된 범위에서 타격 직전마다 생존 적 재추첨
        } // 무작위 연속 공격 종료
        else // 일반 카드 효과 분기
        { // 일반 효과 처리 시작
            foreach (BattleUnitRuntime targetUnit in targetUnits) // 대상 유닛 순회
            { // 대상별 효과 적용 시작
                if (turnRuntime.HasPendingBossPhaseTransition) // 보스 페이즈 전환 예약 확인
                { // 남은 타격 취소 시작
                    break; // 미처리 대상 효과 취소
                } // 남은 타격 취소 종료
                int applied = ApplyCardEffect(cardInstance, targetUnit); // 현재 카드 대상의 실제 적용량
                totalAppliedAmount += applied; // 카드 효과 적용량 누적
                appliedDefense |= defensiveCard && applied > 0 && targetUnit?.Team == BattleTeam.Ally; // 실제 아군에게 적용한 기본 방어 효과 기록
            } // 대상별 효과 적용 종료
        } // 일반 효과 처리 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.KingsSlash && !turnRuntime.IsBattleEnded && !turnRuntime.HasPendingBossPhaseTransition && !cardInstance.OwnerUnit.IsDead) // 전체 공격이 끝난 뒤 추가 타격 가능 여부 확인
        { // 왕의 후속 타격 시작
            totalAppliedAmount += ApplyKingsSecondHit(cardInstance); // 살아 있는 적 중 현재 최대 체력이 가장 높은 적 재선택
        } // 왕의 후속 타격 종료
        if (echoPlan.Percent > 0) // 실제로 준비를 소모한 이번 피해 카드 확인
        { // 원본 타격 뒤의 독립 반향 시작
            totalAppliedAmount += ApplyWandEcho(cardInstance, targetUnits, echoPlan); // 원래 카드 이동 및 후속 정산 이전에 피해 방식만 한 번 재생
        } // 원본 타격 뒤의 독립 반향 종료
        starRecoveryPassive.TryApply(cardInstance, targetUnits); // 별의 회복 카드 사용 시 추가 회복 예약
        int pentacleDraw = cardInstance.SourceData.PentacleRule switch // 사용 정산과 한 번에 처리할 실제 펜타클 드로우 확인
        { // 공유 카드의 원본 드로우 수 확인 시작
            PentacleCardRule.Focus or PentacleCardRule.AncientRune => CardInstance.ApplyEnchantment(cardInstance.SourceData.EffectValue, cardInstance.EnchantmentPercent), // 기존 두 원본의 수치와 한 번 강화 기준 유지
            PentacleCardRule.FusionOfPower => 2, // 대상 수와 별개인 힘의 융합의 원본 공유 두 장
            PentacleCardRule.SecureSupplies => cardInstance.SourceData.SecondaryEffectValue, // 방어도와 별개인 보급 확보의 공유 한 장
            _ => 0 // 다른 카드의 기존 개별 후속 정산 유지
        }; // 공유 카드의 원본 드로우 수 확인 종료
        bool followUpDefense = false; // 드로우 카드에 별도 후속 방어가 없는 기본값
        int drawn = 0; // 실제 손패로 이동한 이번 카드 효과 드로우 수
        bool discarded = pentacleDraw > 0 // 하나의 사용 이동으로 처리할 펜타클 드로우인지 확인
            ? runtimeDeck.TryPlayAndDraw(cardInstance, GetCardEffectDrawCount(pentacleDraw), out drawn, limitToHandCapacity: true) // 사용한 카드를 먼저 손패에서 제거하고 기존 상한 안에서 공용 드로우를 한 번 처리
            : ApplyFollowUpAndDiscard(cardInstance, targetUnits, out followUpDefense, pentacleHandCards); // 기존 카드의 후속 효과와 방어 및 이동 보존
        totalAppliedAmount += drawn; // 대상 수와 무관한 실제 공유 손패 드로우 합산
        if (!discarded) // 카드 이동 결과 확인
        { // 카드 이동 실패 처리 시작
            Debug.LogError($"[BattleCardActionController] 사용 카드 이동에 실패했습니다: {cardInstance.DisplayName}"); // 이동 실패 출력
        } // 카드 이동 실패 처리 종료
        if (discarded && cardInstance.EffectType == CardEffectType.DrawThenDiscard) // 달의 드로우 후 손패 정리 확인
        { // 버릴 카드 후보 준비 시작
            pendingCardChoices = new List<CardInstance>(runtimeDeck.Hand); // 사용한 카드를 제외한 현재 손패 복사
        } // 버릴 카드 후보 준비 종료
        if (discarded) // 사용 카드의 손패 이탈 확인
        { // 사용 후 드로우 유물 처리 시작
            ResolveWandArithmeticAfterUse(cardInstance); // 실제 사용 카드 이동 뒤 과부하 비용과 역류의 공격력 약화 처리
            int medalBlock = turnRuntime.ResolveDefenseCardRelics(cardInstance, usedFirstDefenseDiscount); // 성공한 카드 사용의 방어 할인 소모와 메달 조각 지급
            if (medalBlock > 0) // 실제 메달 조각 방어도 지급 확인
            { // 메달 조각 방어 표시 시작
                FindUnitView(cardInstance.OwnerUnit)?.ShowStatusFeedback($"방어도 +{medalBlock}", false); // 방패를 쓴 아군에게 추가 방어도 표시
            } // 메달 조각 방어 표시 종료
            if (cardInstance.SourceData.ShieldRule == ShieldCardRule.GiantsShield) // 정산을 끝낸 거인의 방패 확인
            { // 다음 턴 행동력 감소 예약 시작
                turnRuntime.QueueNextPlayerTurnActionPointLoss(cardInstance.SourceData.SecondaryEffectValue); // 다음 아군 턴 한 번의 공용 행동력 감소 예약
            } // 다음 턴 행동력 감소 예약 종료
            bool movedFromHand = !ContainsHandCard(cardInstance); // 기존 보너스로 다시 뽑기 전 실제 손패 이탈 확인
            QueuePentacleUseDraw(cardInstance); // 카드 사용 위치 정산 뒤 펜타클 보너스 확인
            if (movedFromHand) // 독립 보너스 지급 전 확인한 실제 사용 정산
            { // 스킬 드로우 예약 시작
                if (cardInstance.SourceData.WandRule == WandCardRule.TimeStop && turnRuntime.IsPlayerTurn && !turnRuntime.IsBattleEnded) // 실제 사용을 완료한 시간 정지의 공용 손패 지급 확인
                { // 실제 사용 카드가 비운 공용 손패 드로우 시작
                    runtimeDeck.DrawRelicCardsIntoHand(cardInstance.SourceData.SecondaryEffectValue); // 기존 상한을 지키며 공용 손패에 실제 한 장 지급
                } // 실제 사용 카드가 비운 공용 손패 드로우 종료
                totalAppliedAmount += ResolveFixedPentacleUtility(cardInstance); // 손패를 떠난 원본의 공용 행동력 및 턴 비용 지원 정산
                pendingPentacleChanceDraw = cardInstance.CardType == CardType.Pentacle && BattleSupplyRelics.Find(RelicEffectType.PentacleResolvedChanceDraw) != null; // 손패를 실제로 떠난 펜타클의 독립 추가 드로우 예약
                QueueSkillRelicDraw(cardInstance); // 기존 카드 이동을 마친 스킬의 잉크병 판정 예약
                pendingCardSuitRelics = cardInstance; // 손패를 나간 카드의 속성 정산 예약
                pendingCardSuitRound = turnRuntime.CurrentRound; // 보스 전환 전 실제 사용 라운드 기록
                if (!HasPendingEffectChoice) // 완료 후 추가 결정이 없는 실제 사용 확인
                { // 즉시 처리 가능한 펜촉 판정 시작
                    ApplyPendingPentacleChanceDraw(); // 기존 즉시 드로우 처리 뒤 펜촉의 독립 판정
                } // 즉시 처리 가능한 펜촉 판정 종료
            } // 스킬 드로우 예약 종료
            if ((appliedDefense || followUpDefense) && turnRuntime.IsPlayerTurn && !cardInstance.OwnerUnit.IsDead) // 완료된 방어 카드와 생존한 원래 소유자 확인
            { // 방어 카드 장갑 처리 시작
                int block = BattleSupplyRelics.Find(RelicEffectType.ResolvedDefenseBlock)?.EffectValue ?? 0; // 장갑이 주는 소유자 전용 추가 방어도
                cardInstance.OwnerUnit.GainBlock(block); // 여러 대상이어도 카드 소유자에게 한 번만 지급
            } // 방어 카드 장갑 처리 종료
        } // 사용 후 드로우 유물 처리 종료
        if (discarded) // 해당 실물 원본의 정상 사용 완료 확인
        { // 이번 카드의 한 번 강화 종료 시작
            cardInstance.ConsumeEffectEnchantment(); // 물리 손패 버림이나 취소와 구분하는 사용 뒤 효과 초기화
            cardInstance.ConsumeRunePower(); // 확정 사용의 일반 기본 행동력 면제 한 번 소모
        } // 이번 카드의 한 번 강화 종료
        string effectLabel = GetEffectLabel(cardInstance); // 카드 효과 로그 문구 생성
        Debug.Log($"[BattleCardActionController] 카드 사용 완료 - {cardInstance.DisplayName} / 대상 {targetUnits.Count}명 / {effectLabel} 적용량 {totalAppliedAmount} / 남은 공용 AP {sharedActionPoints.CurrentActionPoints}"); // 카드 사용 결과 출력
    } // 카드 충돌 처리 종료
    private static void ApplyVoidCollapseFailureCost(CardInstance card, bool defeatedByHit) // 공허 붕괴의 원본 공격이 처치하지 못한 경우 확인
    { // 실제 시전자의 한 번 정신력 비용 처리 시작
        if (card.SourceData.WandRule == WandCardRule.VoidCollapse && !defeatedByHit) // 빗나감과 실제 생존 적 및 보스 전환을 실패로 확인
        { // 명시된 정신력 대가 결제 시작
            card.OwnerUnit.SpendBattleMentalCost(card.SourceData.SecondaryEffectValue, BattleMentalChangeReason.CardEffect); // 정신력 피해 보호를 소비하지 않는 실제 카드 비용과 하한
        } // 명시된 정신력 대가 결제 종료
    } // 실제 시전자의 한 번 정신력 비용 처리 종료

    private int ResolveFixedPentacleUtility(CardInstance card) // 정상 이동한 펜타클의 고정 공용 자원 효과
    { // 실제 원본 펜타클 지원 확인 시작
        if (card.EffectType != CardEffectType.PentacleUtility || !turnRuntime.IsPlayerTurn || turnRuntime.IsBattleEnded) // 현재 실제 아군 턴에 완료한 지원 카드 확인
        { // 끝난 전투와 관련 없는 카드 제외 시작
            return 0; // 현재 턴을 벗어난 행동력과 할인 미적용
        } // 끝난 전투와 관련 없는 카드 제외 종료
        return card.SourceData.PentacleRule switch // 카드별 정식 보유 자원 정산
        { // 펜타클 자원 효과 선택 시작
            PentacleCardRule.GoldenCoin => sharedActionPoints.Gain(CardInstance.ApplyEnchantment(card.SourceData.EffectValue, card.EnchantmentPercent)), // 현재 공용 최대치 안에서 원본 행동력 한 점 회복
            PentacleCardRule.Ritual or PentacleCardRule.PickUpCoin => sharedActionPoints.Gain(card.EffectValue), // 의식의 실체력 비용과 동전의 전투 제외를 완료한 뒤 현재 행동력 회복
            PentacleCardRule.EfficientUse => turnRuntime.SetEfficientUseDiscount(CardInstance.ApplyEnchantment(card.SourceData.EffectValue, card.EnchantmentPercent)), // 실제 현재 아군 턴의 기존 기본 비용 한 점 할인 갱신
            _ => 0 // 드로우와 선택 및 다른 자원 처리 규칙의 후속 정산 없음
        }; // 펜타클 자원 효과 선택 종료
    } // 실제 원본 펜타클 지원 확인 종료

    private long ApplyFusionOfPower(CardInstance card) // 힘의 융합의 두 진영을 대상으로 하는 혼합 효과
    { // 한 번의 광역 회복과 피해 정산 시작
        long applied = 0; // 각 생존 아군 회복과 실제 적 체력 피해의 합계
        int healing = GetDirectCardHealing(card, card.SourceData.SecondaryEffectValue); // 원본 열 점과 일반 시전자 회복 보정 계산
        foreach (var ally in CollectValidTargets(card, allyUnitViews)) // 실제 충돌 때 살아 있는 모든 아군을 다시 확인
        { // 개별 생존 아군의 회복 시작
            applied += RestoreDirectCardHealth(card, ally, healing); // 현재 최대 체력과 실제 회복 효과를 적용한 개별 지급
        } // 개별 생존 아군의 회복 종료
        return applied + ApplyGeneralDamageToLivingEnemies(card, card.EffectValue); // 물리와 마법 저항을 거치지 않는 원본 피해를 각 적의 방어도에 전달
    } // 한 번의 광역 회복과 피해 정산 종료
    private int ApplyTemporarySupplies(CardInstance card, BattleUnitRuntime target) // 지정된 아군의 실제 회복과 독립 행동력 회복
    { // 임시 보급의 실제 지급 시작
        if (target is not { Team: BattleTeam.Ally, IsDead: false }) // 충돌까지 유효하게 살아 있는 원래 지정 아군 확인
        { // 대상 상실 처리 시작
            return 0; // 대상이 없으면 효과와 난수 소비 생략
        } // 대상 상실 처리 종료
        int restored = RestoreDirectCardHealth(card, target, GetDirectCardHealing(card, card.EffectValue)); // 실제 부상 여부와 현재 상한을 따라 일반 회복 처리
        int chance = Math.Clamp(card.SourceData.SecondaryEffectValue, 0, 100); // 회복량과 독립인 원본 행동력 지급 확률
        if (chance > 0 && (chance >= 100 || UnityEngine.Random.Range(0, 100) < chance)) // 소유자의 진짜 행동력 지급을 별도로 한 번 굴림
        { // 독립 행동력 지급 시작
            int gained = sharedActionPoints.Gain(1); // 기존 최대 행동력 아래에서 실제 한 점 회복
            if (gained > 0) // 상한에 걸리지 않은 실제 회복량 확인
            { // 현재 행동력 피드백 시작
                FindUnitView(card.OwnerUnit)?.ShowStatusFeedback($"행동력 +{gained}", true); // 지급된 실제 행동력만 시전자 화면에 표시
            } // 현재 행동력 피드백 종료
        } // 독립 행동력 지급 종료
        return restored; // 실제 개별 회복 효과의 총량 반환
    } // 임시 보급의 실제 지급 종료
    private void ResolveWandArithmeticAfterUse(CardInstance card) // 사용을 정상 완료한 지팡이 카드의 시전자 후속 상태
    { // 지팡이 시전 결과 처리 시작
        var owner = card.OwnerUnit; // 실제 카드를 사용한 원래 시전자 조회
        if (card.SourceData.WandRule == WandCardRule.ManaOverload) // 정상 시전과 이동을 마친 과부하 확인
        { // 시전 직후의 정신력 비용 처리 시작
            owner.SpendBattleMentalCost(card.SourceData.SecondaryEffectValue, BattleMentalChangeReason.CardEffect); // 정신력 다섯 점을 고정 대가로 지불하며 기존 하한 적용
        } // 시전 직후의 정신력 비용 처리 종료
        else if (card.SourceData.WandRule == WandCardRule.ManaBackflow && turnRuntime.IsPlayerTurn && !turnRuntime.IsBattleEnded && !owner.IsDead) // 원래 모든 공격을 처리한 아군의 현재 턴 확인
        { // 실제 역류 후속 약화 처리 시작
            var result = owner.ApplyStatusEffect(BattleStatusEffectType.AttackPowerDown, card.SourceData.SecondaryEffectValue, 1, 1, owner); // 다른 원래 타격을 바꾸지 않고 현재 아군 턴 동안 공격력 감소 부여
            FindUnitView(owner)?.ShowStatusApplyFeedback(BattleStatusEffectType.AttackPowerDown, result); // 시전자를 약화한 실제 원래 상태 표시
        } // 실제 역류 후속 약화 처리 종료
    } // 지팡이 시전 결과 처리 종료

    private bool ApplyFollowUpAndDiscard(CardInstance card, IReadOnlyList<BattleUnitRuntime> targets, out bool appliedDefense, IReadOnlyList<CardInstance> pentacleHandCards) // 보조 방어 처리 결과를 포함한 카드 이동
    { // 후속 효과 처리 시작
        appliedDefense = false; // 명시적 후속 방어도의 성공 여부 초기화
        if (card.SourceData.PentacleRule == PentacleCardRule.CardCopy) // 실제 완료 사용 기록을 고른 복제 주문 확인
        { // 사용 원본을 움직이며 실제 복제 한 장 지급 시작
            return runtimeDeck.TryPlayAndCopyHistory(card, pentacleHandCards[0]); // 소유권과 강화 단계가 같은 독립 실물을 현재 전투에 삽입
        } // 사용 원본을 움직이며 실제 복제 한 장 지급 종료
        if (card.SourceData.PentacleRule == PentacleCardRule.TacticalExchange) // 이미 두 실물 손패를 버린 전술 교환 확인
        { // 사용 원본을 뽑지 않는 보충 드로우 시작
            return runtimeDeck.TryPlayAndDraw(card, card.EffectValue, out _, limitToHandCapacity: true, preferOtherThanFirst: pentacleHandCards[0], preferOtherThanSecond: pentacleHandCards[1]); // 두 원래 버림 카드보다 다른 실제 후보를 먼저 보충
        } // 사용 원본을 뽑지 않는 보충 드로우 종료
        int value = card.SourceData.SecondaryEffectValue; // 원본의 후속 수치 조회
        if (card.SourceData.SwordRule == SwordCardRule.CheckSlash) // 적을 견제한 소유자의 회피 증가 확인
        { // 자기 회피 처리 시작
            var result = card.OwnerUnit.ApplyStatusEffect(BattleStatusEffectType.Evasion, value, card.StatusDuration, 1, card.OwnerUnit); // 공격 명중과 무관하게 한 적 턴 회피율 증가
            FindUnitView(card.OwnerUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.Evasion, result); // 실제 회피 상승 표시
        } // 자기 회피 처리 종료
        if (card.SourceData.SwordRule == SwordCardRule.RageRelease || card.SourceData.SwordRule == SwordCardRule.BattlefieldResolve) // 이번 턴부터 세는 아군 공격력 강화 확인
        { // 아군별 보조 효과 시작
            foreach (var target in targets) // 실제 카드를 받은 생존 대상 순회
            { // 개별 아군 후속 처리 시작
                if (target.IsDead) // 카드 정산 사이 전투불능 확인
                { // 사망 대상 제외 시작
                    continue; // 다음 생존 아군 확인
                } // 사망 대상 제외 종료
                target.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)?.ConsumeDurationDecreaseSkip(); // 시전한 아군 턴부터 강화 기간 계산
                if (card.SourceData.SwordRule == SwordCardRule.RageRelease) // 분노 해방의 자기 물리 방어 약화 확인
                { // 방어 약화 시작
                    var result = target.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, value, card.StatusDuration, 1, card.OwnerUnit); // 같은 두 턴 동안 기존 물리 방어 감소 부여
                    if (result == BattleStatusEffectApplyResult.Applied || result == BattleStatusEffectApplyResult.Stacked) // 실제 약화가 부여된 대상 확인
                    { // 약화 기간 계산 시작
                        target.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown).ConsumeDurationDecreaseSkip(); // 시전 턴을 첫 번째 약화 턴으로 계산
                    } // 약화 기간 계산 종료
                    FindUnitView(target)?.ShowStatusApplyFeedback(BattleStatusEffectType.PhysicalDefenseDown, result); // 약화 적용 또는 면역 표시
                } // 방어 약화 종료
                else // 전장의 결의 방어 지급
                { // 두 시점 방어 지급 시작
                    target.GainBlock(value); // 카드 사용 직후 동일 대상 방어도 획득
                    target.ApplyStatusEffect(BattleStatusEffectType.BattlefieldResolve, value, 1, int.MaxValue, card.OwnerUnit); // 사용 횟수만큼 다음 아군 턴 방어도 예약
                } // 두 시점 방어 지급 종료
            } // 개별 아군 후속 처리 종료
        } // 아군별 보조 효과 종료
        switch (card.SourceData.FollowUpEffect) // 카드별 추가 대상과 효과 선택
        { // 효과 구분 시작
            case CardFollowUpEffect.GainOwnerBlock: // 공격한 소유자에게 방어도 지급
                appliedDefense = card.OwnerUnit.GainBlock(value) > 0; // 소유자에게 실제로 발생한 명시적 후속 방어도
                break; // 방어 처리 종료
            case CardFollowUpEffect.HealAllAllies: // 공격 후 모든 생존 아군 회복
                int healing = GetDirectCardHealing(card, value); // 일반 회복과 같은 소유자 보정 적용
                foreach (BattleUnitRuntime ally in enemyActions.AllyUnits) // 전투에 참여한 아군 순회
                { // 동료 회복 시작
                    RestoreDirectCardHealth(card, ally, healing); // 실제 부상 아군별 직접 카드 회복 적용
                } // 동료 회복 종료
                break; // 전체 회복 종료
            case CardFollowUpEffect.GainTargetBlock: // 선택한 아군에게 방어도 지급
                foreach (BattleUnitRuntime target in targets) // 치료한 대상 순회
                { // 대상 방어 시작
                    int gained = target.GainBlock(value); // 선택된 대상이 실제 얻은 방어도
                    appliedDefense |= target.Team == BattleTeam.Ally && gained > 0; // 아군에게 실제로 적용한 명시적 후속 방어도 기록
                } // 대상 방어 종료
                break; // 대상 방어 처리 종료
            case CardFollowUpEffect.DrawCards: // 사용한 카드 자리를 비운 뒤 드로우
                return runtimeDeck.TryPlayAndDraw(card, GetCardEffectDrawCount(value), out _); // 최초 카드 드로우 보너스를 포함한 후속 드로우
        } // 효과 구분 종료
        return card.EffectType == CardEffectType.DrawThenDiscard || (turnRuntime.IsBattleEnded && !ContainsHandCard(card)) || runtimeDeck.DiscardPlayedCard(card); // 선행 이동과 전투 종료 복사본 정리 및 일반 카드 사용 정산
    } // 후속 효과 처리 종료
    private void HandleActionSequenceCompleted() // 카드 행동 연출 완료 처리
    { // 연출 완료 처리 시작
        actionPending = false; // 카드 행동 진행 상태 해제
        bossPhaseRuntime?.CompletePendingTransition(); // 충돌과 카드 이동 완료 후 보스 페이즈 전환
        ShowPendingCardChoices(); // 대기 중인 드로우 선택 화면 표시
        if (drawChoiceView == null) // 별도 카드 선택을 기다리지 않는 경우 확인
        { // 대기 드로우 처리 시작
            ApplyPendingPentacleDraw(); // 후보 없이 끝난 카드 사용의 예약 지급
        } // 대기 드로우 처리 종료
        ShowPendingDebuffChoices(); // 대기 중인 디버프 선택 화면 표시
        if (drawChoiceView == null && debuffChoiceView == null && remainingChoiceView == null) // 선택을 더 기다릴 필요가 없는 실제 정산 확인
        { // 스킬 카드 추가 드로우 처리 시작
            ApplyPostSelectionRelicDraws(); // 선택 없이 끝난 카드의 남은 독립 드로우 처리
        } // 스킬 카드 추가 드로우 처리 종료
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
    } // 연출 완료 처리 종료
    private void ShowPendingDebuffChoices() // 대기 중인 디버프 이전 선택 표시
    { // 디버프 선택 표시 시작
        if (pendingDebuffChoices == null || pendingDebuffChoices.Count == 0 || !turnRuntime.IsPlayerTurn) // 후보와 플레이어 턴 확인
        { // 선택 불필요 처리 시작
            pendingDebuffChoices = null; // 대기 중인 디버프 후보 정리
            pendingDebuffSource = null; // 디버프 원래 대상 정리
            pendingDebuffRecipient = null; // 디버프 이전 대상 정리
            return; // 선택 화면 표시 중단
        } // 선택 불필요 처리 종료
        Canvas canvas = handView.GetComponentInParent<Canvas>(); // 전투 화면 캔버스 조회
        if (canvas == null) // 선택 화면 부모 존재 확인
        { // 캔버스 없음 처리 시작
            pendingDebuffChoices = null; // 대기 디버프 후보 정리
            pendingDebuffSource = null; // 디버프 원래 대상 정리
            pendingDebuffRecipient = null; // 디버프 이전 대상 정리
            Debug.LogError("[BattleCardActionController] 디버프 선택 화면을 표시할 캔버스가 없습니다."); // 선택 화면 누락 안내
            pendingSkillRelicCard = null; // 표시할 수 없는 선택을 정상 정산으로 집계하지 않도록 초기화
            pendingPentacleChanceDraw = false; // 표시 못 한 상태 선택의 펜촉 예약 해제
            pendingCardSuitRelics = null; // 완료하지 못한 카드의 속성 정산 예약 해제
            return; // 선택 화면 표시 중단
        } // 캔버스 없음 처리 종료
        debuffChoiceView = BattleDebuffChoiceView.Show(canvas.transform, pendingDebuffChoices, HandleDebuffChoiceSelected); // 디버프 한 가지 선택 화면 생성
        handView.SetInteractionLocked(true); // 선택 중 손패와 턴 종료 잠금
    } // 디버프 선택 표시 종료
    private void HandleDebuffChoiceSelected(BattleStatusEffectInstance effect) // 선택한 디버프 이전 처리
    { // 선택 디버프 처리 시작
        if (!BattleDebuffTransfer.TryTransfer(pendingDebuffSource, pendingDebuffRecipient, effect)) // 선택한 디버프의 실제 이전 여부 확인
        { // 선택 실패 처리 시작
            pendingSkillRelicCard = null; // 실패한 마지막 선택의 스킬 드로우 차단
            pendingPentacleChanceDraw = false; // 실패한 상태 이전의 펜촉 예약 해제
            pendingCardSuitRelics = null; // 완료하지 못한 카드의 속성 정산 예약 해제
        } // 선택 실패 처리 종료
        pendingDebuffChoices = null; // 디버프 후보 정리
        pendingDebuffSource = null; // 원래 동료 참조 정리
        pendingDebuffRecipient = null; // 디버프 이전 대상 참조 정리
        debuffChoiceView = null; // 닫힌 선택 화면 참조 정리
        ApplyPostSelectionRelicDraws(); // 상태 이전 선택까지 마친 카드의 추가 드로우 처리
        handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
    } // 선택 디버프 처리 종료
    internal static bool HasManaRetrievalCandidate(CardInstance source, BattleDeckRuntime deck) // 현재 물리 손패에 선택할 다른 지팡이 존재 여부
    { // 손패 카드의 현재 회수 가능 검사 시작
        if (deck == null || source?.CardType != CardType.Wand) // 실제 전투 덱과 올바른 지팡이 회수 카드 확인
        { // 유효 원본이 없는 경우 시작
            return false; // 별도 지팡이를 선택할 수 없음 반환
        } // 유효 원본이 없는 경우 종료
        foreach (var card in deck.Hand) // 현재 물리 손패의 실제 인스턴스 조회
        { // 카드의 회수 비용 자격 검사 시작
            if (IsDistinctManaRetrievalWand(source, card)) // 회수 원본과 다른 지팡이 카드 확인
            { // 유효 버림 후보 발견 시작
                return true; // 손패 선택을 개시할 수 있음 반환
            } // 유효 버림 후보 발견 종료
        } // 카드의 회수 비용 자격 검사 종료
        return false; // 다른 지팡이 없는 회수 사용 불가
    } // 손패 카드의 현재 회수 가능 검사 종료
    private static bool IsDistinctManaRetrievalWand(CardInstance source, CardInstance candidate) // 마력 회수에 가능한 별도 지팡이 카드 확인
    { // 후보 자격 판단 시작
        return candidate != source && candidate?.CardType == CardType.Wand; // 원본 카드와 같거나 다른 속성인 후보 제외
    } // 후보 자격 판단 종료
    private void ShowManaRetrievalChoices(CardInstance card) // 원래 카드 사용 전에 실제 버릴 지팡이 선택 열기
    { // 마력 회수 후보 표시 시작
        var canvas = handView.GetComponentInParent<Canvas>(); // 대상 카드 선택을 놓을 원래 전투 화면 조회
        if (canvas == null || drawChoiceView != null || !HasManaRetrievalCandidate(card, runtimeDeck)) // 선택 화면과 실제 물리 손패 후보 검증
        { // 선택을 개시할 수 없는 경우 시작
            return; // 비용과 카드 상태 그대로 유지
        } // 선택을 개시할 수 없는 경우 종료
        var candidates = new List<CardInstance>(); // 현재 선택에 올릴 실제 별도 지팡이 후보 목록
        foreach (var candidate in runtimeDeck.Hand) // 현재 손패의 각 카드 확인
        { // 별도 지팡이 후보 수집 시작
            if (IsDistinctManaRetrievalWand(card, candidate)) // 원본과 다른 지팡이 선별
            { // 실제 팝업 후보 추가 시작
                candidates.Add(candidate); // 현재 물리 인스턴스 그대로 등록
            } // 실제 팝업 후보 추가 종료
        } // 별도 지팡이 후보 수집 종료
        CancelSelection(); // 새 선택 창 전 다른 카드의 미확정 대상 강조 정리
        manaRetrievalSource = card; // 선택이 끝날 때까지 유지할 사용 전 원본 저장
        handView.SetInteractionLocked(true); // 카드와 턴 종료 입력을 선택 동안 잠금
        BattleDrawChoiceView opened = null; // 이전에 닫은 화면의 늦은 콜백을 구분할 창 참조
        opened = BattleDrawChoiceView.Show(canvas.transform, candidates, candidate => FinishManaRetrievalChoice(opened, card, candidate), // 원래 카드 선택 화면의 단일 후보 확인 연결
            "버릴 지팡이 카드 한 장 선택", () => FinishManaRetrievalChoice(opened, card, null)); // 지금 버리지 않고 닫는 취소 입력 제공
        drawChoiceView = opened; // 현재 회수 선택의 살아 있는 창 저장
    } // 마력 회수 후보 표시 종료
    private void FinishManaRetrievalChoice(BattleDrawChoiceView opened, CardInstance source, CardInstance candidate) // 실제 회수 선택의 확인 또는 취소 정산
    { // 선택 결과 검증 시작
        if (!ReferenceEquals(opened, drawChoiceView) || manaRetrievalSource != source) // 지금 연 회수 창과 현재 원본이 정확히 같은지 확인
        { // 이미 닫힌 창 입력 처리 시작
            return; // 이전이나 중복 클릭으로 원래 상태 변경 방지
        } // 이미 닫힌 창 입력 처리 종료
        manaRetrievalSource = null; // 닫힌 선택의 원본 참조 해제
        drawChoiceView = null; // 닫힌 선택 창 참조 해제
        handView.SetInteractionLocked(false); // 취소나 카드 연출에 앞선 일반 입력 복구
        if (disposed || !turnRuntime.IsPlayerTurn || turnRuntime.HasPendingBossPhaseTransition || source.OwnerUnit.IsDead || !ContainsHandCard(source) || // 씬 종료와 발동 불가 상태 및 원본 카드의 실제 소유 확인
            !IsDistinctManaRetrievalWand(source, candidate) || !ContainsHandCard(candidate)) // 취소나 더 이상 손패에 없는 실제 후보 제외
        { // 유효하지 않은 확인 정리 시작
            return; // 행동력 및 원본 카드와 후보의 추가 이동 없음
        } // 유효하지 않은 확인 정리 종료
        manaRetrievalDiscard = candidate; // 카드 충돌 시점의 물리 손패 검사에 넘길 후보
        ExecuteCard(source, CreateSingleTargetList(source.OwnerUnit)); // 정상 카드 결제와 사용 연출 흐름 시작
        if (!actionPending) // 정산을 이미 끝냈거나 연출을 시작하지 못한 경우 확인
        { // 실행되지 않을 후보 정리 시작
            manaRetrievalDiscard = null; // 후속 카드 입력에 사용되지 않을 회수 후보 제거
        } // 실행되지 않을 후보 정리 종료
    } // 선택 결과 검증 종료
    private bool TryDiscardForManaRetrieval(CardInstance source) // 마력 회수의 실제 손패 버림 성립 확인
    { // 별도 지팡이의 확정 버림 검사 시작
        var candidate = manaRetrievalDiscard; // 선택 중 확인한 실제 비용 카드 조회
        manaRetrievalDiscard = null; // 실제 덱 이벤트가 다시 호출해도 같은 선택을 두 번 쓰지 않는 초기화
        return turnRuntime.IsPlayerTurn && !turnRuntime.HasPendingBossPhaseTransition && !source.OwnerUnit.IsDead && ContainsHandCard(source) && // 실제 충돌까지 정상 아군 턴과 사용 원본 유지 확인
            IsDistinctManaRetrievalWand(source, candidate) && runtimeDeck.DiscardCard(candidate) && !ContainsHandCard(candidate); // 원래 버림 허용과 손패에 남는 보호 여부까지 확인
    } // 별도 지팡이의 확정 버림 검사 종료
    private void ShowPendingCardChoices() // 대기 중인 선택 드로우 표시
    { // 선택 화면 표시 시작
        if (pendingCardChoices == null || pendingCardChoices.Count == 0 || !turnRuntime.IsPlayerTurn) // 선택 후보와 플레이어 턴 확인
        { // 선택 불필요 처리 시작
            pendingCardChoices = null; // 대기 후보 정리
            return; // 선택 화면 표시 중단
        } // 선택 불필요 처리 종료
        Canvas canvas = handView.GetComponentInParent<Canvas>(); // 전투 화면 캔버스 조회
        if (canvas == null) // 캔버스 존재 확인
        { // 화면 누락 처리 시작
            pendingCardChoices = null; // 대기 후보 정리
            Debug.LogError("[BattleCardActionController] 선택 드로우 화면을 표시할 캔버스가 없습니다."); // 화면 생성 실패 출력
            pendingSkillRelicCard = null; // 요청한 카드 선택을 끝낼 수 없는 경우 잉크병 예약 해제
            pendingPentacleChanceDraw = false; // 표시 못 한 카드 선택의 펜촉 예약 해제
            pendingCardSuitRelics = null; // 완료하지 못한 카드의 속성 정산 예약 해제
            return; // 선택 화면 표시 중단
        } // 화면 누락 처리 종료
        string title = pendingChoiceEffect == CardEffectType.RecoverDiscard ? "회수할 버린 카드 선택" : pendingChoiceEffect == CardEffectType.ArrangeDrawTop ? "다음에 뽑을 카드 선택" : pendingChoiceEffect == CardEffectType.DrawThenDiscard ? "버릴 손패 한 장 선택" : "가져올 카드 한 장 선택"; // 카드 선택 안내 설정
        drawChoiceView = BattleDrawChoiceView.Show(canvas.transform, pendingCardChoices, HandleCardChoiceSelected, title); // 선택 화면 생성
        handView.SetInteractionLocked(true); // 선택 중 손패와 턴 종료 잠금
    } // 선택 화면 표시 종료
    private void HandleCardChoiceSelected(CardInstance choice) // 드로우 후보 선택 처리
    { // 선택 처리 시작
        int choiceCount = pendingCardChoices == null ? 0 : pendingCardChoices.Count; // 유물 반영 후 화면에 보여 준 후보 수
        CardInstance remaining = null; // 선택하지 않은 카드 초기화
        if (pendingFoolPassive && pendingCardChoices != null) // 바보 패시브 선택 여부 확인
        { // 남은 카드 검색 시작
            foreach (CardInstance candidate in pendingCardChoices) // 선택 후보 순회
            { // 남은 카드 확인 시작
                if (candidate != choice) // 선택하지 않은 카드 확인
                { // 남은 카드 저장 시작
                    remaining = candidate; // 남은 카드 저장
                    break; // 남은 카드 검색 종료
                } // 남은 카드 저장 종료
            } // 남은 카드 확인 종료
        } // 남은 카드 검색 종료
        bool processed = false; // 카드 선택 처리 결과 초기화
        if (pendingChoiceEffect == CardEffectType.RecoverDiscard) // 버린 카드 회수 선택 확인
        { // 버린 카드 회수 시작
            processed = runtimeDeck.TryRecoverDiscard(choice); // 선택한 버린 카드 회수
        } // 버린 카드 회수 종료
        else if (pendingChoiceEffect == CardEffectType.ArrangeDrawTop) // 덱 순서 정리 선택 확인
        { // 덱 순서 변경 시작
            processed = runtimeDeck.TryMoveDrawTop(choice, choiceCount); // 선택한 카드를 맨 위에 배치
        } // 덱 순서 변경 종료
        else if (pendingChoiceEffect == CardEffectType.DrawThenDiscard) // 드로우 후 손패 버리기 선택 확인
        { // 손패 버리기 시작
            processed = runtimeDeck.DiscardCard(choice); // 선택한 손패 카드 한 장 버리기
        } // 손패 버리기 종료
        else // 덱 위 카드 선택 확인
        { // 선택 드로우 시작
            processed = runtimeDeck.TryDrawChoice(choice, choiceCount); // 선택 카드를 손패로 이동
            if (processed && pendingChoiceEffect == CardEffectType.ChooseDraw && lastPencilRound != turnRuntime.CurrentRound) // 현재 턴의 실제 확인 드로우 성공
            { // 연필 보너스 준비 시작
                var pencil = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.InspectCardDraw); // 실제 보유한 흑연 연필
                if (pencil != null) // 보유 상태 확인
                { // 추가 드로우 예약 시작
                    lastPencilRound = turnRuntime.CurrentRound; // 이번 턴 추가 지급 기회 소모
                    pendingInspectionDraw = pencil.EffectValue; // 바보의 남은 카드 선택 우선 보존
                } // 추가 드로우 예약 종료
            } // 연필 보너스 준비 종료
        } // 선택 드로우 종료
        pendingCardChoices = null; // 대기 후보 정리
        drawChoiceView = null; // 닫힌 선택 화면 참조 정리
        if (!processed) // 실제 카드 선택 적용 실패 확인
        { // 실패한 카드 선택 처리 시작
            pendingSkillRelicCard = null; // 실패한 카드 선택의 스킬 드로우 차단
            pendingPentacleChanceDraw = false; // 실패한 카드 선택의 펜촉 예약 해제
            pendingCardSuitRelics = null; // 완료하지 못한 카드의 속성 정산 예약 해제
        } // 실패한 카드 선택 처리 종료
        if (processed && pendingFoolPassive && runtimeDeck.IsTopDrawable(remaining)) // 남은 유효 카드 패시브 발동 확인
        { // 남은 카드 처리 선택 시작
            pendingFoolPassive = false; // 대기 패시브 해제
            lastFoolPassiveRound = turnRuntime.CurrentRound; // 이번 턴 패시브 발동 기록
            remainingChoiceCard = remaining; // 남은 카드 저장
            Canvas canvas = handView.GetComponentInParent<Canvas>(); // 전투 화면 캔버스 조회
            remainingChoiceView = BattleRemainingCardChoiceView.Show(canvas.transform, remaining, HandleRemainingCardSelected); // 남은 카드 처리 화면 생성
            return; // 남은 카드 선택까지 입력 잠금 유지
        } // 남은 카드 처리 선택 종료
        pendingFoolPassive = false; // 대기 패시브 해제
        ApplyInspectionDraw(); // 남은 카드 처리 없는 확인 보너스 즉시 지급
        ApplyPendingPentacleDraw(); // 카드 선택과 기존 확인 드로우 뒤 펜타클 보너스 지급
        ApplyPostSelectionRelicDraws(); // 일반 카드 선택까지 완료한 추가 드로우 판정
        handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
    } // 선택 처리 종료
    private void HandleRemainingCardSelected(bool discard) // 남은 카드 처리 선택
    { // 남은 카드 처리 시작
        if (discard) // 버리기 선택 확인
        { // 남은 카드 버리기 시작
            runtimeDeck.TryDiscardDrawTop(remainingChoiceCard); // 남은 맨 위 카드 버리기
        } // 남은 카드 버리기 종료
        ApplyInspectionDraw(); // 남은 카드를 유지하거나 버린 뒤 확인 보너스 지급
        ApplyPendingPentacleDraw(); // 남은 카드 결정 뒤 펜타클 보너스 지급
        remainingChoiceCard = null; // 남은 카드 참조 정리
        remainingChoiceView = null; // 닫힌 선택 화면 참조 정리
        ApplyPostSelectionRelicDraws(); // 남은 카드 결정을 포함한 최종 추가 드로우 판정
        handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
    } // 남은 카드 처리 종료
    private void CloseCardChoice() // 대기 선택 화면 정리
    { // 선택 화면 정리 시작
        ClosePentacleHandChoice(); // 취소나 전투 종료에서 미지불 펜타클 손패 선택 잠금 정리
        CloseWheelPayment(); // 강제 정리 중인 실제 수레바퀴 비용 선택과 입력 잠금 해제
        manaRetrievalSource = null; // 전투 종료나 강제 정리에 회수 선택 원본 제거
        manaRetrievalDiscard = null; // 전투 종료나 강제 정리에 회수 버림 후보 제거
        pendingEventRelicDraw = 0; // 전투 종료나 선택 강제 해제 뒤 공용 신호 드로우 예약 해제
        pendingInspectionDraw = 0; // 전투 종료나 선택 강제 해제 뒤 남은 드로우 예약 정리
        pendingPentacleDraw = 0; // 전투 종료나 선택 강제 해제 뒤 펜타클 예약 정리
        pendingSkillRelicCard = null; // 종료나 강제 취소된 선택의 잉크병 예약 정리
        pendingPentacleChanceDraw = false; // 종료나 취소에 따른 펜촉의 대기 상태 정리
        pendingCardSuitRelics = null; // 완료하지 못한 카드의 속성 정산 예약 해제
        pendingCardChoices = null; // 대기 후보 정리
        pendingFoolPassive = false; // 대기 패시브 해제
        remainingChoiceCard = null; // 남은 카드 참조 정리
        pendingDebuffChoices = null; // 디버프 선택 후보 정리
        pendingDebuffSource = null; // 디버프 원래 대상 정리
        pendingDebuffRecipient = null; // 디버프 이전 대상 정리
        bool hadChoiceView = drawChoiceView != null || remainingChoiceView != null || debuffChoiceView != null; // 선택 화면 존재 여부 확인
        if (drawChoiceView != null) // 카드 선택 화면 존재 확인
        { // 카드 선택 화면 닫기 시작
            drawChoiceView.Close(); // 카드 선택 화면 닫기
            drawChoiceView = null; // 카드 선택 화면 참조 정리
        } // 카드 선택 화면 닫기 종료
        if (remainingChoiceView != null) // 남은 카드 화면 존재 확인
        { // 남은 카드 화면 닫기 시작
            remainingChoiceView.Close(); // 남은 카드 화면 닫기
            remainingChoiceView = null; // 남은 카드 화면 참조 정리
        } // 남은 카드 화면 닫기 종료
        if (debuffChoiceView != null) // 디버프 선택 화면 존재 확인
        { // 디버프 선택 화면 닫기 시작
            debuffChoiceView.Close(); // 디버프 선택 화면 닫기
            debuffChoiceView = null; // 디버프 선택 화면 참조 정리
        } // 디버프 선택 화면 닫기 종료
        if (hadChoiceView && handView != null && !actionSequenceRunner.IsBusy) // 선택 화면과 연출 상태 확인
        { // 입력 복구 시작
            handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
        } // 입력 복구 종료
    } // 선택 화면 정리 종료
    private bool CanConfirmSelectedEnemyPair(CardInstance card, IReadOnlyList<BattleUnitRuntime> targets) // 결제 전에 확인할 골라 놓은 적 조합
    { // 선택을 시작한 순간의 적 수와 현재 조건 비교 시작
        if (card.EffectType != CardEffectType.Damage || card.OwnerUnit.IsDead || requiredSelectedEnemies < 1 || targets.Count != requiredSelectedEnemies || // 원본 효과와 소유자 및 처음 필요한 적 수 확인
            !turnRuntime.CanSelectLivingUnit(targets[0], BattleTeam.Enemy)) // 실제 전투에 남아 있는 첫 생존 적 확인
        { // 미확정 대상 처리 시작
            return false; // 행동력 결제와 카드 사용 거절 반환
        } // 미확정 대상 처리 종료
        return targets.Count == 1 || targets[0] != targets[1] && turnRuntime.CanSelectLivingUnit(targets[1], BattleTeam.Enemy); // 한 대상 허용 상태 또는 서로 다른 두 생존 적 확인
    } // 선택을 시작한 순간의 적 수와 현재 조건 비교 종료
    private bool IsValidTarget(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 대상 유효성 검사
    { // 대상 검사 시작
        if (cardInstance == null || targetUnit == null) // 카드와 대상 연결 확인
        { // 선택 불가 처리 시작
            return false; // 잘못된 대상 반환
        } // 선택 불가 처리 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.RunePower) // 실제 손패에 다른 유료 고정 카드가 있는 아군인지 확인
        { // 룬 카드의 정상 지정 아군 검사 시작
            return turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Ally) && GetRunePowerCandidates(cardInstance, targetUnit).Count > 0; // 실제 생존 아군과 비어 있지 않은 본인 손패 후보 확인
        } // 룬 카드의 정상 지정 아군 검사 종료
        if (cardInstance.TargetType == CardTargetType.FallenAlly || cardInstance.EffectType == CardEffectType.ReviveAlly) // 전투불능 아군 전용 카드 조합 확인
        { // 부활 대상 검사 시작
            return cardInstance.TargetType == CardTargetType.FallenAlly && cardInstance.EffectType == CardEffectType.ReviveAlly && turnRuntime.CanReviveFallenAlly(targetUnit); // 정확한 부활 효과와 현재 명단 검사 결과 반환
        } // 부활 대상 검사 종료
        if (targetUnit.IsDead) // 일반 카드 대상의 생존 여부 확인
        { // 일반 카드 전투불능 대상 차단 시작
            return false; // 일반 카드 대상으로 선택 불가
        } // 일반 카드 전투불능 대상 차단 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.EmberSpread) // 불씨 확산의 화상 적 전용 대상 확인
        { // 불씨 확산 대상 검사 시작
            return cardInstance.TargetType == CardTargetType.SingleEnemy && turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy) && // 실제 선택할 수 있는 생존 적 한 명 확인
                targetUnit.GetStatusEffect(BattleStatusEffectType.Burn) is { IsExpired: false }; // 만료되지 않은 화상 상태 확인
        } // 불씨 확산 대상 검사 종료
        var requiredState = RequiredWandEnemyStatus(cardInstance.SourceData.WandRule); // 선택과 드롭이 공유하는 원소 상태 종류
        if (requiredState != BattleStatusEffectType.None) // 기존 활성 상태 조건을 사용하는 카드 확인
        { // 원소 카드의 적 후보 검사 시작
            return turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy) && targetUnit.GetStatusEffect(requiredState) is { IsExpired: false }; // 현재 등록된 생존 적과 유지 중인 상태 확인
        } // 원소 카드의 적 후보 검사 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.FateFreeze) // 운명 동결의 실제 단일 적 지정 확인
        { // 동결 가능한 적 확인 시작
            return turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy); // 현재 전투의 생존 적 한 명만 허용
        } // 동결 가능한 적 확인 종료
        if (cardInstance.TargetType == CardTargetType.BlessingSourceAndAlly) // 두 아군의 축복 이전 선택 확인
        { // 축복 이전 단계별 대상 검사 시작
            return firstSequentialTarget == null ? BattleBlessingTransfer.CanChooseSource(turnRuntime, targetUnit) : // 강화 보유자 선택 검사
                BattleBlessingTransfer.FindBlessing(turnRuntime, firstSequentialTarget, targetUnit) != null; // 두 번째로 고른 아군의 실제 강화 수령 가능 여부
        } // 축복 이전 단계별 대상 검사 종료
        if (cardInstance.TargetType == CardTargetType.SelectedTwoEnemies) // 서리 파편에서 지정할 생존 적 확인
        { // 현재 적 클릭 유효성 확인 시작
            return cardInstance.EffectType == CardEffectType.Damage && turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy); // 실제 전투 명단의 생존 적만 입력 허용
        } // 현재 적 클릭 유효성 확인 종료
        if (cardInstance.TargetType == CardTargetType.EnemyAndAlly) // 순서가 정해진 적과 아군의 선택 검사
        { // 현재 선택 단계 검사 시작
            BattleTeam team = firstSequentialTarget == null ? BattleTeam.Enemy : BattleTeam.Ally; // 첫 적 지정 전후의 대상 진영 구분
            return cardInstance.EffectType == CardEffectType.Damage && turnRuntime.CanSelectLivingUnit(targetUnit, team); // 자기 자신을 포함한 해당 진영의 생존 참가자 확인
        } // 현재 선택 단계 검사 종료
        if ((cardInstance.SourceData.SwordRule == SwordCardRule.BloodTrail || cardInstance.SourceData.SwordRule == SwordCardRule.BloodBackflow) && targetUnit.GetStatusEffect(BattleStatusEffectType.Bleed) == null) // 대상의 현재 출혈 여부 확인
        { // 추적 대상 제한 시작
            return false; // 출혈 없는 적의 선택 차단
        } // 추적 대상 제한 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.JudgmentSlash && targetUnit.FirstRemovableBuff() == null) // 단죄로 제거할 일반 강화 확인
        { // 단죄 대상 제외 시작
            return false; // 제거할 강화 없는 대상 입력 차단
        } // 단죄 대상 제외 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.Finisher && !SwordCardCombat.IsFinisherTarget(targetUnit)) // 선택한 적의 현재 체력 재검사
        { // 처형 대상 제한 시작
            return false; // 체력 오분의 일을 넘는 적 선택 차단
        } // 처형 대상 제한 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.ExecutionSlash && !SwordCardCombat.IsFinisherTarget(targetUnit, 2)) // 처형 베기의 실제 대상 체력 확인
        { // 처형 베기 제한 시작
            return false; // 체력이 절반보다 많은 적 선택 차단
        } // 처형 베기 제한 종료
        if (DevilBloodPrice.IsBloodPriceCard(cardInstance) && !DevilBloodPrice.CanPay(cardInstance)) // 악마 전용 카드의 체력 비용 확인
        { // 체력 비용 지불 불가 시작
            return false; // 선택 불가 반환
        } // 체력 비용 지불 불가 종료
        if ((cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) && !targetUnit.HasRemovableDebuff && // 제거할 디버프가 없는 정화 대상 확인
            !(cardInstance.SourceData.CupRule == CupCardRule.PurifyingTouch && cardInstance.SourceData.SecondaryEffectValue > 0)) // 정화할 약화가 없어도 적용할 저항 효과 확인
        { // 정화 불필요 처리 시작
            return false; // 정화 대상 불가 반환
        } // 정화 불필요 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect && cardInstance.StatusEffectType == BattleStatusEffectType.None) // 상태 이상 카드 설정 확인
        { // 상태 설정 오류 처리 시작
            return false; // 상태 이상 대상 불가 반환
        } // 상태 설정 오류 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental && !targetUnit.CanChangeMental(cardInstance.MentalChangeValue) && // 정신력이 실제로 바뀌지 않는 대상 확인
            !(cardInstance.SourceData.FollowUpEffect == CardFollowUpEffect.DrawCards && runtimeDeck.HasDrawableCards) && // 기존 보조 드로우 가능 여부 확인
            !(cardInstance.SourceData.CupRule == CupCardRule.Calming && targetUnit.GetStatusEffect(BattleStatusEffectType.Fear) != null)) // 정신력 상한에서도 제거할 공포 확인
        { // 정신력 변경 불가 처리 시작
            return false; // 정신력 대상 불가 반환
        } // 정신력 변경 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.RecoverDiscard && runtimeDeck.DiscardPileCount == 0) // 회수할 버린 카드 존재 확인
        { // 회수 불가 처리 시작
            return false; // 선택 불가 반환
        } // 회수 불가 처리 종료
        if (!CanUseDrawInspection(cardInstance, runtimeDeck)) // 유물이 반영된 덱 위 확인 가능 여부 검사
        { // 선택 불가 처리 시작
            return false; // 선택 불가 반환
        } // 선택 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard && !runtimeDeck.HasDrawableCards && runtimeDeck.HandCount <= 1) // 드로우와 사용 후 남을 손패 확인
        { // 드로우와 버리기 불가 시작
            return false; // 선택 불가 반환
        } // 드로우와 버리기 불가 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack && !CanGuardAllyWithCard(cardInstance, targetUnit)) // 이 카드로 보호할 동료의 적격 여부 확인
        { // 동료 보호 불가 시작
            return false; // 선택 불가 반환
        } // 동료 보호 불가 종료
        if (cardInstance.EffectType == CardEffectType.RerollRandomAttack && !enemyActions.CanRerollRandomAttack(targetUnit)) // 적 무작위 공격 재굴림 가능 확인
        { // 재굴림 불가 처리 시작
            return false; // 선택 불가 반환
        } // 재굴림 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.ShareAllyAttack && !enemyActions.CanShareAllyAttack(cardInstance.OwnerUnit, targetUnit)) // 동료의 예정 공격 분담 가능 확인
        { // 피해 분담 불가 처리 시작
            return false; // 선택 불가 반환
        } // 피해 분담 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.TransferAllyDebuff && BattleDebuffTransfer.GetChoices(targetUnit, cardInstance.OwnerUnit).Count == 0) // 동료의 이전 가능한 디버프 확인
        { // 디버프 이전 불가 처리 시작
            return false; // 선택 불가 반환
        } // 디버프 이전 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.SelectPartner && !loversPartner.CanSetPartner(cardInstance.OwnerUnit, targetUnit)) // 생존한 다른 파트너 후보 확인
        { // 파트너 지정 불가 처리 시작
            return false; // 선택 불가 반환
        } // 파트너 지정 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.ProtectAttackTarget && (targetUnit.Team != BattleTeam.Ally || !HasIncomingAttack(targetUnit))) // 공격 예고 보호 대상 확인
        { // 보호 불가 대상 처리 시작
            return false; // 선택 불가 반환
        } // 보호 불가 대상 처리 종료
        if (cardInstance.EffectType == CardEffectType.RedirectAttackAndDefend && cardInstance.SourceData.ShieldRule != ShieldCardRule.Taunt) // 한 적의 예정 공격을 바꾸는 기존 황제 카드 확인
        { // 공격 유도 대상 확인 시작
            if (targetUnit.Team != BattleTeam.Enemy || !enemyActions.CanRedirectAttack(targetUnit, cardInstance.OwnerUnit)) // 실제 공격 유도 가능 여부 확인
            { // 공격 유도 불가 처리 시작
                return false; // 선택 불가 반환
            } // 공격 유도 불가 처리 종료
        } // 공격 유도 대상 확인 종료
        switch (cardInstance.TargetType) // 카드 대상 종류 분기
        { // 대상 분기 시작
            case CardTargetType.Self: // 자신 대상 종류
                return targetUnit == cardInstance.OwnerUnit; // 카드 소유자 여부 반환
            case CardTargetType.LowestHealthAlly: // 현재 체력과 파티 순서로 자동 결정한 대상
                return targetUnit == turnRuntime.GetLowestHealthLivingAlly(); // 같은 생존 자동 대상만 실제 사용 허용
            case CardTargetType.SingleAlly: // 단일 아군 대상 종류
            case CardTargetType.AllAllies: // 전체 아군 대상 종류
                return targetUnit.Team == BattleTeam.Ally; // 아군 여부 반환
            case CardTargetType.SingleEnemy: // 단일 적 대상 종류
            case CardTargetType.RandomTwoEnemies: // 서로 다른 생존 적을 최대 두 명 고르는 대상 종류
            case CardTargetType.AllEnemies: // 전체 적 대상 종류
                return targetUnit.Team == BattleTeam.Enemy; // 적 여부 반환
            default: // 알 수 없는 대상 종류
                return false; // 잘못된 대상 반환
        } // 대상 분기 종료
    } // 대상 검사 종료
    private static IReadOnlyList<BattleUnitRuntime> PickDistinctRandomEnemies(IReadOnlyList<BattleUnitRuntime> candidates) // 무작위 전격의 서로 다른 최대 두 대상 선택
    { // 두 명의 무작위 적 선택 시작
        if (candidates.Count <= 2) // 두 명 이하로 모든 후보가 결과에 확정되는 경우 확인
        { // 확정된 적 모두 사용 시작
            return candidates; // 같은 적 반복이나 불필요한 난수 소비 없는 기존 순서 보존
        } // 확정된 적 모두 사용 종료
        int first = RelicRandomCardTarget.PickIndex(candidates.Count, count => UnityEngine.Random.Range(0, count)); // 기존 무작위 대상 유물 규칙을 지키는 첫 후보 위치
        int second = RelicRandomCardTarget.PickIndex(candidates.Count - 1, count => UnityEngine.Random.Range(0, count)); // 첫 후보를 제외한 나머지 후보 위치 선택
        if (second >= first) // 첫 후보의 자리를 건너뛸 위치 확인
        { // 두 번째 실제 후보 위치 보정 시작
            second++; // 누락된 첫 후보를 넘어 원래 후보 목록의 위치 복원
        } // 두 번째 실제 후보 위치 보정 종료
        return new[] { candidates[first], candidates[second] }; // 두 번의 명중 판정을 받을 서로 다른 생존 적 확정
    } // 두 명의 무작위 적 선택 종료

    private bool HasIncomingAttack(BattleUnitRuntime allyUnit) // 예정 공격 대상 여부 확인
    { // 공격 예고 확인 시작
        foreach (BattleEnemyAction action in enemyActions.PlannedActions) // 예정 적 행동 순회
        { // 공격 대상 검사 시작
            if (action.IsExecutable && action.ActionType == EnemyActionType.Attack && action.Target == allyUnit) // 실행 가능한 공격 예고 확인
            { // 공격 예고 발견 시작
                return true; // 보호 대상 반환
            } // 공격 예고 발견 종료
        } // 공격 대상 검사 종료
        return false; // 공격 예고 없음 반환
    } // 공격 예고 확인 종료
    private void UpdateTargetHighlights() // 선택 가능 대상 강조
    { // 대상 강조 시작
        UpdateTargetHighlights(allyUnitViews); // 아군 대상 강조 갱신
        UpdateTargetHighlights(enemyUnitViews); // 적 대상 강조 갱신
    } // 대상 강조 종료
    private void UpdateTargetHighlights(IReadOnlyList<BattleUnitView> unitViews) // 유닛 목록 대상 강조
    { // 목록 강조 시작
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 유닛 강조 시작
            bool targetable = unitView != null && unitView.RuntimeUnit != firstSequentialTarget && IsValidTarget(selectedCard, unitView.RuntimeUnit); // 아직 고르지 않은 현재 단계의 유효 대상 확인
            if (unitView != null) // 유닛 화면 존재 확인
            { // 대상 강조 적용 시작
                unitView.SetTargetable(targetable); // 대상 강조 적용
            } // 대상 강조 적용 종료
        } // 유닛 강조 종료
    } // 목록 강조 종료
    private List<BattleUnitRuntime> CollectValidTargets(CardInstance cardInstance, IReadOnlyList<BattleUnitView> unitViews) // 유효 대상 목록 생성
    { // 유효 대상 생성 시작
        List<BattleUnitRuntime> targetUnits = new List<BattleUnitRuntime>(); // 빈 대상 목록 생성
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 유효 대상 검사 시작
            if (unitView != null && IsValidTarget(cardInstance, unitView.RuntimeUnit)) // 유효 유닛 확인
            { // 유효 유닛 등록 시작
                targetUnits.Add(unitView.RuntimeUnit); // 유효 대상 목록 등록
            } // 유효 유닛 등록 종료
        } // 유효 대상 검사 종료
        return targetUnits; // 유효 대상 목록 반환
    } // 유효 대상 생성 종료
    private bool CanGuardAllyWithCard(CardInstance card, BattleUnitRuntime target) // 기존 힘 카드와 방패 카드의 다른 보호 조건 조회
    { // 보호 조건 조회 시작
        return card.SourceData.ShieldRule switch // 기본 보호와 방패의 직접 공격 보호 규칙 구분
        { // 보호 카드 종류 분기 시작
            ShieldCardRule.BodyGuard or ShieldCardRule.GuardianHeart => enemyActions.CanBodyGuard(card.OwnerUnit, target), // 이후 대신 받을 수 있는 다른 생존 동료 확인
            ShieldCardRule.ShieldOath => enemyActions.CanShieldOath(card.OwnerUnit, target), // 두 적 턴 동안 보호할 생존 동료 확인
            _ => enemyActions.CanProtectAlly(card.OwnerUnit, target) // 예정 공격을 바로 바꾸는 기존 힘 카드 확인
        }; // 보호 카드 종류 분기 종료
    } // 보호 조건 조회 종료
    internal static bool CanUseDrawInspection(CardInstance card, BattleDeckRuntime deck) // 전투 손패와 대상의 공통 확인 가능 조건
    { // 확인 가능 검사 시작
        switch (card.EffectType) // 덱 위 확인 방식 구분
        { // 확인 방식 검사 시작
            case CardEffectType.ChooseDraw: // 버린 더미 재구성이 가능한 선택 뽑기
                return GetInspectionChoiceLimit(card) > 0 && deck.HasDrawableCards; // 유효한 확인 수와 생존 소유자의 후보 존재 조건
            case CardEffectType.ArrangeDrawTop: // 현재 뽑을 더미에서만 순서 조정
                return deck.PeekDrawTop(GetInspectionChoiceLimit(card)).Count > 0; // 유물 한도 안에서 실제 재배치할 후보 확인
            default: // 덱 위 확인과 관련 없는 카드
                return true; // 다른 효과의 사용 조건 유지
        } // 확인 방식 검사 종료
    } // 확인 가능 검사 종료
    private static int GetInspectionChoiceLimit(CardInstance card) // 유물을 포함한 카드의 덱 위 확인 한도
    { // 확인 한도 계산 시작
        int bonus = BattleSupplyRelics.Find(RelicEffectType.InspectionChoiceBonus)?.EffectValue ?? 0; // 금박 책갈피의 후보 수 가산량
        return (int)Math.Min(int.MaxValue, (long)card.EffectValue + Math.Max(0, bonus)); // 정수 범위 안의 최종 확인 후보 수
    } // 확인 한도 계산 종료
    private static bool IsSupportedEffect(CardEffectType effectType) // 지원 효과 여부 확인
    { // 지원 효과 검사 시작
        if (effectType is CardEffectType.DiscardWandForActionPoint or CardEffectType.PentacleUtility) // 실제 별도의 확정된 비용 또는 보조 효과 카드를 일반 전투에서 지원
        { // 신규 카드 효과 지원 시작
            return true; // 정상 사용과 이동 흐름 실행 허용
        } // 신규 카드 효과 지원 종료
        return effectType == CardEffectType.CopyAllyBlessing || // 두 아군 사이의 독립 강화 복사 지원
            effectType == CardEffectType.Damage || effectType == CardEffectType.Heal || effectType == CardEffectType.ReviveAlly || effectType == CardEffectType.ApplyStatusEffect || effectType == CardEffectType.RemoveDebuffs || effectType == CardEffectType.RemoveOneDebuff || effectType == CardEffectType.ChangeMental || effectType == CardEffectType.RedirectAttackAndDefend || effectType == CardEffectType.ProtectAttackTarget || effectType == CardEffectType.ChooseDraw || effectType == CardEffectType.RecoverDiscard || effectType == CardEffectType.GainBlock || effectType == CardEffectType.SelectPartner || effectType == CardEffectType.GuardAllyAttack || effectType == CardEffectType.ArrangeDrawTop || effectType == CardEffectType.RerollRandomAttack || effectType == CardEffectType.ShareAllyAttack || effectType == CardEffectType.TransferAllyDebuff || effectType == CardEffectType.DrawThenDiscard; // 기존 지원 카드 효과 결과 반환
    } // 지원 효과 검사 종료
    private BattleUnitView FindUnitView(BattleUnitRuntime runtimeUnit) // 런타임 유닛 화면 조회
    { // 유닛 화면 조회 시작
        BattleUnitView allyView = FindUnitView(runtimeUnit, allyUnitViews); // 아군 화면에서 조회
        return allyView != null ? allyView : FindUnitView(runtimeUnit, enemyUnitViews); // 아군 또는 적 화면 반환
    } // 유닛 화면 조회 종료
    private static BattleUnitView FindUnitView(BattleUnitRuntime runtimeUnit, IReadOnlyList<BattleUnitView> unitViews) // 지정 목록 유닛 화면 조회
    { // 지정 목록 조회 시작
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 목록 순회
        { // 유닛 화면 비교 시작
            if (unitView != null && unitView.RuntimeUnit == runtimeUnit) // 런타임 유닛 일치 확인
            { // 일치 화면 처리 시작
                return unitView; // 일치 유닛 화면 반환
            } // 일치 화면 처리 종료
        } // 유닛 화면 비교 종료
        return null; // 일치 유닛 화면 없음 반환
    } // 지정 목록 조회 종료
    private List<BattleUnitView> CollectUnitViews(IReadOnlyList<BattleUnitRuntime> runtimeUnits) // 대상 런타임 화면 목록 생성
    { // 대상 화면 생성 시작
        List<BattleUnitView> unitViews = new List<BattleUnitView>(); // 빈 유닛 화면 목록 생성
        foreach (BattleUnitRuntime runtimeUnit in runtimeUnits) // 대상 런타임 목록 순회
        { // 대상 화면 조회 시작
            BattleUnitView unitView = FindUnitView(runtimeUnit); // 런타임 대응 화면 조회
            if (unitView != null) // 대응 화면 확인
            { // 대응 화면 처리 시작
                unitViews.Add(unitView); // 대상 화면 목록 추가
            } // 대응 화면 처리 종료
        } // 대상 화면 조회 종료
        return unitViews; // 대상 화면 목록 반환
    } // 대상 화면 생성 종료
    private List<CardInstance> GetRunePowerCandidates(CardInstance source, BattleUnitRuntime target) // 잡은 실제 손패 가운데 지정 아군의 다른 룬 후보
    { // 현재 손패와 실제 소유자의 개별 후보 검사 시작
        var candidates = new List<CardInstance>(); // 균등 추첨에 쓸 물리 카드 명단
        foreach (var candidate in runtimeDeck.Hand) // 현재 출전 아군의 공유 손패 원래 목록 순회
        { // 실제 후보 조건 확인 시작
            if (candidate != source && candidate.OwnerUnit == target && candidate.CanReceiveRunePower) // 다른 실물과 실제 같은 소유자 및 유효 고정 비용 확인
            { // 승인한 후보 추가 시작
                candidates.Add(candidate); // 기존 무작위 한 번의 후보에 더하기
            } // 승인한 후보 추가 종료
        } // 실제 후보 조건 확인 종료
        return candidates; // 생존 지정 아군이 가진 적격 실물 목록 반환
    } // 현재 손패와 실제 소유자의 개별 후보 검사 종료

    private int ApplyCardEffect(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 효과 단일 대상 적용
    { // 단일 효과 적용 시작
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.RunePower) // 현재 생존 아군을 지정한 룬 힘의 실물 대상 추첨
        { // 카드 해결 시 실제 다른 적격 손패 재조회 시작
            var candidates = GetRunePowerCandidates(cardInstance, targetUnit); // 효과 때 여전히 같은 소유자의 적격 실물 확인
            if (targetUnit.IsDead || candidates.Count == 0) // 빈 후보에는 무작위 판정 없이 종료 확인
            { // 유효한 실제 실물 없음 처리 시작
                return 0; // 더 이상 효력이 없는 대상의 비용 상태는 보존
            } // 유효한 실제 실물 없음 처리 종료
            candidates[UnityEngine.Random.Range(0, candidates.Count)].ApplyRunePower(); // 다른 유료 실물에만 한 번의 기본 행동력 면제 적용
            return 1; // 다음 사용에 실제 예약한 카드 한 장 결과
        } // 카드 해결 시 실제 다른 적격 손패 재조회 종료
        if (GetPentacleHandChoiceCount(cardInstance) > 0) // 손패 선택을 마치고 카드 이동에서 반영할 두 지원 카드 확인
        { // 일반 유닛 대상 효과 제외 시작
            return 0; // 실제 드로우와 실물 카드 강화는 원래 손패 이동과 함께 정산
        } // 일반 유닛 대상 효과 제외 종료
        if (cardInstance.EffectType == CardEffectType.DiscardWandForActionPoint) // 실제 별도 지팡이 버림을 먼저 완료한 회수 효과 확인
        { // 원래 공용 행동력의 현재 상한 회복 시작
            return sharedActionPoints.Gain(cardInstance.EffectValue); // 최대치 안에서 회복한 실제 행동력 반환
        } // 원래 공용 행동력의 현재 상한 회복 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.WheelOfFate) // 실제로 지불한 행동력만큼 전체 생존 회복의 개별 대상 확인
        { // 수레바퀴의 실제 회복 시작
            int baseHealing = (int)Math.Min(int.MaxValue, (long)cardInstance.EffectValue * paidActionPoints); // 받아들인 전액 비용마다 원본 회복량을 계산한 기본값
            return RestoreDirectCardHealth(cardInstance, targetUnit, GetDirectCardHealing(cardInstance, baseHealing)); // 원래 직접 회복의 시전자 및 대상 보정과 최대 체력 제한 적용
        } // 수레바퀴의 실제 회복 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.Contract) // 앞서 실제 체력 비용을 지불한 자기 대상 계약 확인
        { // 계약의 실제 행동력 회복 시작
            return sharedActionPoints.Gain(cardInstance.EffectValue); // 실제 턴 현재 상한까지 원본 세 점 회복
        } // 계약의 실제 행동력 회복 종료
        if (cardInstance.SourceData.PentacleRule is PentacleCardRule.Ritual or PentacleCardRule.PickUpCoin) // 카드 이동 후 행동력을 회복하는 지원 카드 확인
        { // 대상별 효과 생략 시작
            return 0; // 일반 타격을 건너뛰고 카드 이동 단계에서 공용 행동력 정산
        } // 대상별 효과 생략 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.SecureSupplies) // 실제 사용자에게 방어도를 지급하는 보급 확보 확인
        { // 자기 방어도 지급 시작
            return cardInstance.OwnerUnit.GainBlock(cardInstance.EffectValue); // 원래 시전자의 현재 방어도와 기존 보정 처리
        } // 자기 방어도 지급 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.TemporarySupplies) // 지정한 실제 아군의 임시 보급 확인
        { // 지정 회복과 독립 행동력 판정 시작
            return ApplyTemporarySupplies(cardInstance, targetUnit); // 실제 지정한 친구 한 명과 공용 행동력의 별도 효과 정산
        } // 지정 회복과 독립 행동력 판정 종료
        switch (cardInstance.SourceData.ShieldRule) // 아군에게 복합 효과를 주는 방패 카드의 대상별 분기
        { // 복합 방패 효과 시작
            case ShieldCardRule.Fortify: // 물리 방어력과 별도의 마법 피해 경감
                return ShieldCardCombat.ApplyFortify(cardInstance, targetUnit, FindUnitView(targetUnit)); // 선택된 생존 아군의 두 효과 처리
            case ShieldCardRule.IndomitableFront: // 아군의 방어도와 공포 해제
                return ShieldCardCombat.ApplyIndomitableFront(cardInstance, targetUnit, FindUnitView(targetUnit)); // 선택된 생존 아군의 방어도와 정화 처리
            case ShieldCardRule.Impenetrable: // 아군의 방어도와 다음 피해 무효
                return ShieldCardCombat.ApplyImpenetrable(cardInstance, targetUnit, FindUnitView(targetUnit)); // 선택된 생존 아군의 방어도와 개인 보호 적용
            case ShieldCardRule.EternalShield: // 영원의 방패 복합 경감 규칙
                return ShieldCardCombat.ApplyEternalShield(cardInstance, targetUnit, FindUnitView(targetUnit)); // 생존 아군에게 직접 피해 감소와 마법 저항 적용
        } // 복합 방패 효과 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.Distribution) // 실제 사용한 분배의 선택 아군 효과 확인
        { // 한 아군에게 독립 전투 강화 및 약화 시작
            var view = FindUnitView(targetUnit); // 실제 상태 결과를 표시할 선택 아군 화면
            int attack = cardInstance.SourceData.EffectValue; // 원본 분배가 정한 공격력 증가
            int defenseLoss = cardInstance.SourceData.SecondaryEffectValue; // 원본 분배가 정한 물리 방어 감소
            var attackResult = targetUnit.ApplyStatusEffect(BattleStatusEffectType.DistributionAttackUp, attack, 1, 1, cardInstance.OwnerUnit); // 일반 상태 분류를 따르는 비중첩 전투 공격 강화 부여
            view?.ShowStatusApplyFeedback(BattleStatusEffectType.DistributionAttackUp, attackResult); // 실제 강화의 부여 또는 갱신 결과 표시
            var defenseResult = targetUnit.ApplyStatusEffect(BattleStatusEffectType.DistributionPhysicalDefenseDown, defenseLoss, 1, 1, cardInstance.OwnerUnit); // 별도 상태 면역과 정화를 따르는 비중첩 물리 방어 약화
            view?.ShowStatusApplyFeedback(BattleStatusEffectType.DistributionPhysicalDefenseDown, defenseResult); // 실제 부여 또는 면역인 물리 약화 결과 표시
            return attackResult is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked ? attack : 0; // 선택된 아군에게 실제 적용한 강화량 반환
        } // 한 아군에게 독립 전투 강화 및 약화 종료
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard) // 달의 드로우와 손패 정리 확인
        { // 달 카드 드로우 처리 시작
            pendingChoiceEffect = CardEffectType.DrawThenDiscard; // 드로우 후 손패 선택 효과 저장
            return runtimeDeck.TryPlayAndDraw(cardInstance, GetCardEffectDrawCount(cardInstance.EffectValue), out int drawnCount) ? drawnCount : 0; // 카드 드로우 증가를 포함해 손패 선택 준비
        } // 달 카드 드로우 처리 종료
        if (cardInstance.EffectType == CardEffectType.TransferAllyDebuff) // 매달린 자의 디버프 이전 효과 확인
        { // 디버프 이전 선택 준비 시작
            pendingDebuffSource = targetUnit; // 디버프를 넘길 동료 저장
            pendingDebuffRecipient = cardInstance.OwnerUnit; // 디버프를 받을 소유자 저장
            pendingDebuffChoices = BattleDebuffTransfer.GetChoices(targetUnit, cardInstance.OwnerUnit); // 이전 가능한 디버프 목록 준비
            return pendingDebuffChoices.Count; // 선택 가능한 디버프 수 반환
        } // 디버프 이전 선택 준비 종료
        if (cardInstance.EffectType == CardEffectType.RerollRandomAttack) // 운명의 공격 대상 재굴림 확인
        { // 무작위 대상 재굴림 시작
            return enemyActions.TryRerollRandomAttack(targetUnit) ? 1 : 0; // 재굴림 성공 여부 반환
        } // 무작위 대상 재굴림 종료
        if (cardInstance.EffectType == CardEffectType.ShareAllyAttack) // 정의의 동료 피해 분담 확인
        { // 동료 피해 분담 시작
            return enemyActions.TryShareAllyAttack(cardInstance.OwnerUnit, targetUnit) ? 1 : 0; // 분담 설정 성공 여부 반환
        } // 동료 피해 분담 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack) // 힘의 동료 보호 효과 확인
        { // 동료 보호 처리 시작
            if (cardInstance.SourceData.ShieldRule == ShieldCardRule.GuardianHeart) // 지정 아군의 치명 피해 보호 카드 확인
            { // 수호자의 심장 사용 시작
                var result = targetUnit.ApplyGuardianHeart(cardInstance.OwnerUnit); // 이 아군이 받는 치명 피해 한 번에 시전자 연결
                FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.GuardianHeart, result); // 실제 보호를 받은 아군에게 표식 안내
                return result == BattleStatusEffectApplyResult.Applied || result == BattleStatusEffectApplyResult.Stacked ? 1 : 0; // 보호 예약의 실제 성공 여부 반환
            } // 수호자의 심장 사용 종료
            if (cardInstance.SourceData.ShieldRule == ShieldCardRule.ShieldOath) // 두 적 턴을 지키는 수호 맹세의 적용 확인
            { // 수호 맹세 처리 시작
                var defender = cardInstance.OwnerUnit; // 대신 피해를 받을 카드 소유자
                if (!enemyActions.TryShieldOath(defender, targetUnit, cardInstance.StatusDuration)) // 대상과 시전자의 생존 및 보호 기간 확인
                { // 보호 예약 실패 처리 시작
                    return 0; // 실질적으로 적용되지 않은 효과
                } // 보호 예약 실패 처리 종료
                var defense = defender.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, cardInstance.EffectValue, cardInstance.StatusDuration, 1, defender); // 대신 공격받는 시전자의 물리 방어력 증가
                FindUnitView(defender)?.ShowStatusApplyFeedback(BattleStatusEffectType.PhysicalDefenseUp, defense); // 시전자에게 실제 보호 방어력 표시
                return cardInstance.EffectValue; // 적용한 보호와 물리 방어 보너스 반환
            } // 수호 맹세 처리 종료
            bool guarded = cardInstance.SourceData.ShieldRule == ShieldCardRule.BodyGuard // 방패 카드의 지속 보호 여부 확인
                ? enemyActions.TryBodyGuard(cardInstance.OwnerUnit, targetUnit) // 대상의 이후 첫 유효 공격을 대신 받을 예약
                : enemyActions.TryProtectAlly(cardInstance.OwnerUnit, targetUnit); // 기존 힘 카드의 예정 공격 보호
            return guarded ? 1 : 0; // 실제 보호 예약 결과 반환
        } // 동료 보호 처리 종료
        if (cardInstance.EffectType == CardEffectType.ArrangeDrawTop) // 은둔자의 덱 순서 정리 확인
        { // 덱 순서 정리 준비 시작
            pendingChoiceEffect = CardEffectType.ArrangeDrawTop; // 덱 순서 정리 선택 효과 저장
            pendingCardChoices = runtimeDeck.PeekDrawTop(GetInspectionChoiceLimit(cardInstance)); // 유물을 반영한 현재 덱 위 후보 조회
            return pendingCardChoices.Count; // 확인 가능한 후보 수 반환
        } // 덱 순서 정리 준비 종료
        if (cardInstance.EffectType == CardEffectType.SelectPartner) // 연인 파트너 지정 효과 확인
        { // 파트너 지정 처리 시작
            return loversPartner.TrySetPartner(cardInstance.OwnerUnit, targetUnit) ? 1 : 0; // 지정 성공 여부 반환
        } // 파트너 지정 처리 종료
        if (cardInstance.EffectType == CardEffectType.GainBlock) // 방어도 획득 효과 확인
        { // 방어도 획득 시작
            if (cardInstance.SourceData.CupRule == CupCardRule.Warmth) // 온기의 지정 아군 화상 확인
            { // 방어 적용 전 화상 해제 시작
                RemoveCardTargetStatus(targetUnit, BattleStatusEffectType.Burn); // 선택한 아군에게 남은 화상만 먼저 제거
            } // 방어 적용 전 화상 해제 종료
            int gainedBlock = targetUnit.GainBlock(cardInstance.EffectValue); // 선택 대상의 실제 방어도 획득
            if (cardInstance.SourceData.ShieldRule == ShieldCardRule.FrontlineProtection) // 방어도를 받은 아군의 전열 보호 상태 확인
            { // 출혈 저항 설정 시작
                var result = targetUnit.ApplyStatusEffect(BattleStatusEffectType.BleedResistance, cardInstance.SourceData.SecondaryEffectValue, cardInstance.StatusDuration, 1, cardInstance.OwnerUnit); // 생존 아군별 출혈 저항 부여
                FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.BleedResistance, result); // 실제 아군의 출혈 저항 표시
            } // 출혈 저항 설정 종료
            if (cardInstance.SourceData.ShieldRule == ShieldCardRule.BalanceGuard) // 방어도를 얻은 카드가 균형 잡기인지 확인
            { // 다음 방패 행동력 할인 예약 시작
                var balance = targetUnit.ApplyStatusEffect(BattleStatusEffectType.ShieldBalance, cardInstance.SourceData.SecondaryEffectValue, 1, 1, targetUnit); // 다음 소유 방패 한 장의 할인 준비
                FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.ShieldBalance, balance); // 피드백과 아이콘 상태 갱신
            } // 다음 방패 행동력 할인 예약 종료
            if (cardInstance.SourceData.CardId == "CRD_ARC_21_LINK") // 세계 전용 연계 카드 확인
            { // 추가 아군 방어 시작
                BattleUnitRuntime additional = worldCoordination.FindAdditionalTarget(cardInstance.OwnerUnit, targetUnit); // 체력 비율이 가장 낮은 다른 생존 아군 조회
                if (additional != null) // 유효한 추가 아군 확인
                { // 추가 아군 방어 적용 시작
                    gainedBlock += additional.GainBlock(cardInstance.SourceData.SecondaryEffectValue); // 다른 아군의 실제 방어도 획득량 합산
                } // 추가 아군 방어 적용 종료
            } // 추가 아군 방어 종료
            return gainedBlock; // 전체 실제 방어도 획득량 반환
        } // 방어도 획득 종료
        if (cardInstance.EffectType == CardEffectType.RecoverDiscard) // 버린 카드 회수 효과 확인
        { // 마법사 전용 효과 시작
            pendingChoiceEffect = CardEffectType.RecoverDiscard; // 회수 선택 효과 저장
            pendingCardChoices = new List<CardInstance>(runtimeDeck.DiscardPile); // 사용 카드 제외 버린 카드 후보 복사
            return pendingCardChoices.Count; // 준비된 후보 수 반환
        } // 마법사 전용 효과 종료
        if (cardInstance.EffectType == CardEffectType.ChooseDraw) // 선택 드로우 효과 확인
        { // 바보 전용 효과 시작
            pendingFoolPassive = cardInstance.OwnerUnit.CharacterSource != null && cardInstance.OwnerUnit.CharacterSource.CharacterId == "ARC_00" && cardInstance.SourceData.CardId == "CRD_ARC_00_CHOICE" && lastFoolPassiveRound != turnRuntime.CurrentRound; // 바보의 첫 선택 드로우 확인
            pendingChoiceEffect = CardEffectType.ChooseDraw; // 선택 드로우 효과 저장
            pendingCardChoices = runtimeDeck.PrepareDrawChoices(GetInspectionChoiceLimit(cardInstance)); // 유물을 반영한 덱 위 선택 후보 준비
            return pendingCardChoices.Count; // 준비된 후보 수 반환
        } // 바보 전용 효과 종료
        if (cardInstance.EffectType == CardEffectType.ProtectAttackTarget) // 공격 예고 대상 보호 효과 확인
        { // 여사제 전용 효과 시작
            BattleStatusEffectApplyResult physicalResult = targetUnit.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, cardInstance.EffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit); // 대상 물리 방어력 증가
            BattleStatusEffectApplyResult magicalResult = targetUnit.ApplyStatusEffect(BattleStatusEffectType.MagicalResistanceUp, cardInstance.EffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit); // 대상 마법 저항력 증가
            BattleUnitView targetView = FindUnitView(targetUnit); // 보호 대상 화면 조회
            targetView?.ShowStatusApplyFeedback(BattleStatusEffectType.PhysicalDefenseUp, physicalResult); // 물리 방어 효과 표시
            targetView?.ShowStatusApplyFeedback(BattleStatusEffectType.MagicalResistanceUp, magicalResult); // 마법 저항 효과 표시
            int physicalAmount = physicalResult == BattleStatusEffectApplyResult.Applied || physicalResult == BattleStatusEffectApplyResult.Stacked ? cardInstance.EffectValue : 0; // 적용된 물리 방어 수치
            int magicalAmount = magicalResult == BattleStatusEffectApplyResult.Applied || magicalResult == BattleStatusEffectApplyResult.Stacked ? cardInstance.EffectValue : 0; // 적용된 마법 저항 수치
            return physicalAmount + magicalAmount; // 전체 보호 효과 수치 반환
        } // 여사제 전용 효과 종료
        if (cardInstance.EffectType == CardEffectType.RedirectAttackAndDefend) // 공격 유도와 방어 효과 확인
        { // 공격 유도 카드 효과 시작
            if (cardInstance.SourceData.ShieldRule == ShieldCardRule.Taunt) // 다음 적 턴에 적용되는 전역 도발 확인
            { // 전역 도발 처리 시작
                if (!enemyActions.TryShieldTaunt(targetUnit)) // 살아 있는 시전자의 도발 등록 확인
                { // 잘못된 도발 대상 처리 시작
                    return 0; // 적용되지 않은 도발의 반환
                } // 잘못된 도발 대상 처리 종료
                var defense = targetUnit.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, cardInstance.EffectValue, cardInstance.StatusDuration, 1, targetUnit); // 도발 시전자의 물리 방어력 증가
                FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.PhysicalDefenseUp, defense); // 방어력 증가의 실제 적용 표시
                return cardInstance.EffectValue; // 도발과 방어 적용 수치 반환
            } // 전역 도발 처리 종료
            if (!enemyActions.TryRedirectAttack(targetUnit, cardInstance.OwnerUnit)) // 선택 적의 공격 유도 확인
            { // 유도 실패 처리 시작
                return 0; // 방어도 미획득 반환
            } // 유도 실패 처리 종료
            return cardInstance.OwnerUnit.GainBlock(cardInstance.EffectValue); // 소유자의 실제 방어도 획득량 반환
        } // 황제 전용 효과 종료
        if (cardInstance.EffectType == CardEffectType.ReviveAlly) // 회복과 구별되는 부활 확인
        { // 전투 부활 적용 시작
            return turnRuntime.TryReviveFallenAlly(targetUnit, cardInstance.SourceData.EffectValue); // 강화와 무관한 원본 최대 체력 비율 적용
        } // 전투 부활 적용 종료
        if (cardInstance.EffectType == CardEffectType.Heal) // 회복 효과 확인
        { // 회복 효과 처리 시작
            if (cardInstance.SourceData.CupRule == CupCardRule.ArchangelBlessing) // 광역 회복 전에 모든 제거 가능한 디버프를 없애는 카드 확인
            { // 대천사의 축복 정화 시작
                int cleansed = statusEffectProcessor.CleanseDebuffs(targetUnit).Count; // 공통 상태 면역과 보호 규칙의 정화 처리
                if (cleansed > 0) // 실제 제거된 상태 개수 확인
                { // 아군 상태 피드백 시작
                    FindUnitView(targetUnit)?.ShowStatusFeedback($"정화 {cleansed}", false); // 해당 아군의 실제 제거 수 표시
                } // 아군 상태 피드백 종료
            } // 대천사의 축복 정화 종료
            BattleStatusEffectType removed = cardInstance.SourceData.StatusRemovedByHealing; // 회복 전에 제거할 상태 조회
            RemoveCardTargetStatus(targetUnit, removed); // 응급 처치와 기존 카드 원본이 지정한 상태 해제
            var bleed = targetUnit.GetStatusEffect(BattleStatusEffectType.Bleed); // 회복을 받는 생존 대상의 실제 출혈 확인
            if (bleed != null && BattleSupplyRelics.Find(RelicEffectType.ShortenBleedOnHeal)?.EffectValue > 0) // 출혈을 가진 회복 카드 대상과 유물 보유 확인
            { // 붕대 매듭의 대상별 출혈 감소 시작
                bleed.AdvanceDuration(); // 실제 체력 변화와 별개인 출혈 지속 한 턴 차감
                if (bleed.IsExpired) // 남은 출혈 기간이 소진되었는지 확인
                { // 마지막 출혈 턴 해제 시작
                    RemoveCardTargetStatus(targetUnit, BattleStatusEffectType.Bleed); // 기존 상태 제거와 전투 화면 알림 사용
                } // 마지막 출혈 턴 해제 종료
                else // 출혈이 아직 남는 대상 확인
                { // 남은 출혈 표시 갱신 시작
                    targetUnit.NotifyStatusEffectsChanged(); // 실제 남은 기간의 기존 상태 변경 알림
                } // 남은 출혈 표시 갱신 종료
            } // 붕대 매듭의 대상별 출혈 감소 종료
            int baseHealing = cardInstance.SourceData.CardId == "CRD_ARC_20_RESCUE" ? JudgementRecovery.GetHealingAmount(targetUnit, cardInstance.EffectValue, cardInstance.SourceData.SecondaryEffectValue) : cardInstance.EffectValue; // 심판의 위험 구간 추가 회복 계산
            bool grailMiracle = cardInstance.SourceData.CupRule == CupCardRule.GrailMiracle; // 실제 지불량을 읽는 성배 회복 확인
            if (grailMiracle) // 가변 회복의 배수 적용 여부 확인
            { // 성배의 기적 회복량 계산 시작
                baseHealing = (int)Math.Min(int.MaxValue, (long)baseHealing * paidActionPoints); // 실제로 지불한 행동력과 정수 상한을 반영한 기본 치유
            } // 성배의 기적 회복량 계산 종료
            int modifiedHealing = GetDirectCardHealing(cardInstance, baseHealing); // 정신 상태 포함 회복량 계산
            int restored = RestoreDirectCardHealth(cardInstance, targetUnit, modifiedHealing); // 기존 회복 유물과 실제 체력 상한 반영
            if (grailMiracle && paidActionPoints >= cardInstance.SourceData.SecondaryEffectValue) // 시트의 실제 지불 행동력 정화 조건 확인
            { // 실제 대상의 약화 제거 시작
                var removedEffects = statusEffectProcessor.CleanseDebuffs(targetUnit, 1); // 회복 대상마다 정화 가능 약화 한 개 제거
                if (removedEffects.Count > 0) // 실제로 약화가 제거된 경우 확인
                { // 성공한 정화 표시 시작
                    FindUnitView(targetUnit)?.ShowStatusFeedback("정화 1", false); // 제거한 디버프의 실제 화면 효과
                    LogCleanseResults(targetUnit, removedEffects); // 기존 정화 결과 출력 재사용
                } // 성공한 정화 표시 종료
            } // 실제 대상의 약화 제거 종료
            if (cardInstance.SourceData.CupRule == CupCardRule.CelestialGrace) // 최대 체력과 무관한 은총의 긍정 강화 확인
            { // 이번 턴 아군 공격력 처리 시작
                var result = targetUnit.ApplyCurrentAllyTurnAttackPower(cardInstance.SourceData.SecondaryEffectValue, cardInstance.OwnerUnit); // 장기 공격력과 독립인 실제 이번 턴 강화
                FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.AttackPowerUp, result); // 적용된 공격력 강화 표시
            } // 이번 턴 아군 공격력 처리 종료
            if (cardInstance.SourceData.CupRule == CupCardRule.WaterOfLife) // 생명수의 직접 회복과 별개인 다음 턴 효과 확인
            { // 생명수 예약 회복 처리 시작
                var result = targetUnit.ApplyStatusEffect(BattleStatusEffectType.DelayedHealing, cardInstance.SourceData.SecondaryEffectValue, 1, int.MaxValue, cardInstance.OwnerUnit); // 다음 아군 턴 시작에 한 번 지급할 회복량 예약
                FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.DelayedHealing, result); // 직접 회복량과 구별되는 예약 상태 표시
            } // 생명수 예약 회복 처리 종료
            return restored; // 카드의 실제 체력 회복량 반환
        } // 회복 효과 처리 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.BloodstainedDuel) // 시전자와 적 한 명의 결투 확인
        { // 결투 표식 처리 시작
            var source = cardInstance.OwnerUnit; // 선택한 카드의 실제 소유자
            var result = BattleDuelRuntime.Start(source, targetUnit, cardInstance.EffectValue, cardInstance.StatusDuration); // 양쪽 표식과 연결 수치 등록
            FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.DuelMark, result); // 적의 표식 적용 여부 표시
            if (result == BattleStatusEffectApplyResult.Applied) // 두 표식이 모두 붙은 경우 확인
            { // 시전자 표식 표시 시작
                FindUnitView(source)?.ShowStatusApplyFeedback(BattleStatusEffectType.DuelMark, result); // 결투를 시작한 아군 표시
                ApplyCrystalMarkDefenseLoss(cardInstance, targetUnit, BattleStatusEffectType.DuelMark, result); // 성립한 결투의 적 표식에만 결정눈 방어 감소
                return cardInstance.EffectValue; // 실제 결투 피해 가산량 반환
            } // 시전자 표식 표시 종료
            return 0; // 면역 또는 불가 시 효과 미적용
        } // 결투 표식 처리 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.BurnAmplification) // 기존 화상만 길게 하는 원소 카드 확인
        { // 실제 화상의 지속 시간 연장 시작
            var burn = targetUnit.GetStatusEffect(BattleStatusEffectType.Burn); // 현재 적에게 남아 있는 원래 화상 참조
            if (burn?.TryExtendActiveDuration(cardInstance.EffectValue) != true) // 만료된 화상이나 여전히 없는 상태 확인
            { // 화상 연장 불가 처리 시작
                return 0; // 새 화상이나 실제 피해를 적용하지 않음
            } // 화상 연장 불가 처리 종료
            targetUnit.NotifyStatusEffectsChanged(); // 기존 화상 피해 또는 중첩 알림 없이 남은 턴 표시 갱신
            FindUnitView(targetUnit)?.ShowStatusFeedback($"화상 +{cardInstance.EffectValue}턴", true); // 적에게 적용한 화상 시간 연장 표시
            return cardInstance.EffectValue; // 원래 화상에 덧붙인 실제 턴 수를 로그에 전달
        } // 실제 화상의 지속 시간 연장 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.FateFreeze) // 기절과 마법 저항 약화를 따로 부여하는 원소 카드 확인
        { // 동결의 두 상태 시도 시작
            var stun = targetUnit.ApplyStatusEffect(BattleStatusEffectType.Stun, cardInstance.EffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit); // 실제 한 적 턴 기절의 기존 면역 경로
            var resistance = targetUnit.ApplyStatusEffect(BattleStatusEffectType.MagicalResistanceDown, cardInstance.SourceData.SecondaryEffectValue, 2, 1, cardInstance.OwnerUnit); // 독립적인 두 적 턴 마법 저항 감소의 기존 면역 경로
            var targetView = FindUnitView(targetUnit); // 실제로 지정한 적의 상태 피드백 화면 조회
            targetView?.ShowStatusApplyFeedback(BattleStatusEffectType.Stun, stun); // 원래 기절의 실제 적용 또는 면역 표시
            targetView?.ShowStatusApplyFeedback(BattleStatusEffectType.MagicalResistanceDown, resistance); // 별도 저항 약화의 실제 결과 표시
            return (stun is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked ? 1 : 0) + // 정상 적용한 기절 한 종류를 정산에 포함
                (resistance is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked ? 1 : 0); // 정상 적용한 저항 약화 한 종류 추가
        } // 동결의 두 상태 시도 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 처리 시작
            if (cardInstance.StatusEffectType == BattleStatusEffectType.Blind && cardInstance.SourceData.OptionalBlindChancePercent >= 0 && // 확률 옵션을 별도로 설정한 직접 실명인지 확인
                !TryRollAuthoredBlind(cardInstance, targetUnit, cardInstance.EffectValue)) // 명시 확률과 살아 있는 대상의 조건 확인
            { // 선택 실명 미적용 시작
                return 0; // 별도 확률 효과를 적용하지 않고 카드 사용 정산으로 이동
            } // 선택 실명 미적용 종료
            BattleStatusEffectApplyResult applyResult = targetUnit.ApplyStatusEffect(cardInstance.StatusEffectType, cardInstance.EffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit, corrosionFamily: cardInstance.SourceData.IsCorrosionStatus); // 일반 상태와 명시 침식 상태의 실제 결과 적용
            ApplyCrystalMarkDefenseLoss(cardInstance, targetUnit, cardInstance.StatusEffectType, applyResult); // 명시한 실제 카드 표식의 적 방어 감소
            bool includesCastTurn = cardInstance.StatusEffectType is BattleStatusEffectType.SwordSaintStance or BattleStatusEffectType.MiracleEcho or BattleStatusEffectType.MagicAttackPowerUp or BattleStatusEffectType.WandEcho or BattleStatusEffectType.WandOverload; // 실제 시전 턴 종료부터 만료할 반향 및 과부하 포함
            if (includesCastTurn && (applyResult == BattleStatusEffectApplyResult.Applied || applyResult == BattleStatusEffectApplyResult.Stacked)) // 실제 신규 적용 또는 갱신 확인
            { // 시전 턴부터 차감 시작
                targetUnit.GetStatusEffect(cardInstance.StatusEffectType).ConsumeDurationDecreaseSkip(); // 시전 턴 종료 때 새 강화의 기간 감소
            } // 시전 턴부터 차감 종료
            BattleUnitView targetView = FindUnitView(targetUnit); // 대상 유닛 화면 조회
            targetView?.ShowStatusApplyFeedback(cardInstance.StatusEffectType, applyResult); // 상태 이상 적용 결과 표시
            if (cardInstance.SourceData.WandRule == WandCardRule.IllusionSplit && targetUnit.Team == BattleTeam.Ally && !targetUnit.IsDead) // 환영 분열의 생존 아군 확인
            { // 회피와 독립된 환영 보호 적용 시작
                var protection = targetUnit.ApplyStatusEffect(BattleStatusEffectType.IllusionHitReduction, cardInstance.SourceData.SecondaryEffectValue, 1, 1, cardInstance.OwnerUnit); // 다음 실제 적 공격에 예약할 고정 피해 감소
                targetView?.ShowStatusApplyFeedback(BattleStatusEffectType.IllusionHitReduction, protection); // 별도 환영 보호의 전투 표시
            } // 회피와 독립된 환영 보호 적용 종료
            if (cardInstance.SourceData.CupRule == CupCardRule.WhiteVow && applyResult is BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked) // 순백의 맹세의 실제 피해 무효 적용 확인
            { // 보호 이후 정신력 회복 처리 시작
                targetUnit.ChangeMental(cardInstance.MentalChangeValue, BattleMentalChangeReason.CardEffect); // 이미 적용한 보호와 별개의 지정 아군 정신력 회복
            } // 보호 이후 정신력 회복 처리 종료
            return applyResult == BattleStatusEffectApplyResult.Applied || applyResult == BattleStatusEffectApplyResult.Stacked ? cardInstance.EffectValue : 0; // 상태 이상 적용 수치 반환
        } // 상태 이상 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) // 디버프 해제 효과 확인
        { // 디버프 해제 처리 시작
            int maximumCount = cardInstance.EffectType == CardEffectType.RemoveOneDebuff ? 1 : int.MaxValue; // 카드별 정화 개수 결정
            IReadOnlyList<BattleStatusEffectProcessResult> cleanseResults = statusEffectProcessor.CleanseDebuffs(targetUnit, maximumCount); // 지정 개수의 대상 디버프 정화
            int removedCount = cleanseResults.Count; // 제거된 디버프 수 계산
            BattleUnitView targetView = FindUnitView(targetUnit); // 대상 유닛 화면 조회
            bool grantsResistance = cardInstance.SourceData.CupRule == CupCardRule.PurifyingTouch && cardInstance.SourceData.SecondaryEffectValue > 0; // 정화의 손길의 실제 저항 수치 확인
            if (removedCount > 0 || !grantsResistance) // 정화 결과 또는 정화 전용 카드 확인
            { // 정화 결과 표시 시작
                targetView?.ShowStatusFeedback(removedCount > 0 ? $"정화 {removedCount}" : "정화 실패", false); // 기존 정화 결과 플로팅 문구 표시
            } // 정화 결과 표시 종료
            LogCleanseResults(targetUnit, cleanseResults); // 정화 상세 결과 출력
            if (grantsResistance) // 약화 제거 여부와 독립된 마법 저항 효과 확인
            { // 마법 저항 부여 시작
                var resistance = targetUnit.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, cardInstance.SourceData.SecondaryEffectValue, cardInstance.StatusDuration, 1, cardInstance.OwnerUnit); // 기존 직접 마법 피해 경감 상태 부여
                targetView?.ShowStatusApplyFeedback(BattleStatusEffectType.ResistanceStance, resistance); // 실제 저항 상태 적용 결과 표시
            } // 마법 저항 부여 종료
            return removedCount; // 제거된 디버프 수 반환
        } // 디버프 해제 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental) // 정신력 직접 효과 확인
        { // 정신력 효과 처리 시작
            if (cardInstance.SourceData.CupRule == CupCardRule.Calming) // 선택한 진정 카드 확인
            { // 공포 해제 시작
                RemoveCardTargetStatus(targetUnit, BattleStatusEffectType.Fear); // 실제 정신력 변화와 별개로 공포 해제
            } // 공포 해제 종료
            BattleMentalChangeResult mentalResult = targetUnit.ChangeMental(cardInstance.MentalChangeValue, BattleMentalChangeReason.CardEffect, cardInstance.OwnerUnit); // 대상 정신력의 직접 변화와 실제 카드 소유자 전달
            return Mathf.Abs(mentalResult.AppliedDelta); // 실제 정신력 변화량 반환
        } // 정신력 효과 처리 종료
        if (DevilBloodPrice.IsBloodPriceCard(cardInstance)) // 악마 전용 체력 소모 공격 확인
        { // 체력 비용 지불 시작
            if (targetUnit == null || targetUnit.IsDead) // 공격 대상의 생존 상태 재확인
            { // 공격 대상 없음 처리 시작
                return 0; // 체력 비용 없이 공격 중단
            } // 공격 대상 없음 처리 종료
            if (!DevilBloodPrice.TryPay(cardInstance)) // 체력 비용 지불 가능 여부 재확인
            { // 체력 비용 실패 처리 시작
                return 0; // 체력과 공격 피해 미적용 반환
            } // 체력 비용 실패 처리 종료
            FindUnitView(cardInstance.OwnerUnit)?.ShowStatusFeedback($"체력 -{cardInstance.SourceData.SecondaryEffectValue}", true); // 실제 체력 비용 표시
        } // 체력 비용 지불 종료
        int hitCount = cardInstance.SourceData.DamageHitCount; // 카드 원본의 타격 수 조회
        if (hitCount == 1) // 기존 단일 공격 확인
        { // 단일 타격 시작
            int healthBefore = targetUnit.CurrentHealth; // 단일 공격 직전의 대상 체력
            int dealt = ApplyDamageHit(cardInstance, targetUnit); // 현재 대상의 명중과 피해 판정
            if (cardInstance.SourceData.SwordRule == SwordCardRule.LastBreakthrough && healthBefore > 0 && dealt == healthBefore && targetUnit.IsDead) // 첫 직접 타격으로만 처치 조건 확인
            { // 처치 후 추가 타격 시작
                dealt += ApplyBreakthroughFollowUp(cardInstance, targetUnit); // 다음 생존 적 한 번 더 타격
            } // 처치 후 추가 타격 종료
            return dealt; // 첫 타격과 가능한 후속 타격의 적용량
        } // 단일 타격 종료
        var target = new[] { targetUnit }; // 연타 대상의 약점 판정 준비
        int applied = 0; // 실제 체력 피해 합계
        try // 도중 중단 시 약점 판정 정리 보장
        { // 연타 시작
            for (int i = 0; i < hitCount && !targetUnit.IsDead && !cardInstance.OwnerUnit.IsDead && !turnRuntime.HasPendingBossPhaseTransition; i++) // 생존과 보스 전환을 확인하며 타격
            { // 개별 타격 시작
                BattleCardDamageContext.Begin(cardInstance, target); // 현재 타격의 검 약점 판정 준비
                applied += ApplyDamageHit(cardInstance, targetUnit); // 독립 명중과 방어 적용
            } // 개별 타격 종료
        } // 연타 종료
        finally // 종료된 타격 상태 정리
        { // 정리 시작
            BattleCardDamageContext.Clear(); // 남은 타격의 약점 판정 제거
        } // 정리 종료
        return applied; // 타격별 체력 피해 합계 반환
    } // 단일 효과 적용 종료

    private long ApplyWandEcho(CardInstance card, IReadOnlyList<BattleUnitRuntime> confirmedTargets, (int Percent, int Paid, int Round) echoPlan) // 실제 마법 피해 카드의 공격 방식만 한 번 복제
    { // 반향 대상과 타격 확인 시작
        if (!CanResolveWandEcho(card, echoPlan.Round)) // 시전한 현재 턴과 유효한 생존 적 확인
        { // 완료되거나 변한 전투의 재생 제외 시작
            return 0; // 승리와 보스 단계 변경 이후의 타격과 난수 없음
        } // 완료되거나 변한 전투의 재생 제외 종료
        if (GetRandomWandHitCount(card) > 0) // 연쇄 번개와 화염 폭우의 원래 무작위 반향 확인
        { // 원래 연속 타격 방식의 피해 복제 시작
            return ApplyRandomWandHits(card, echoPlan); // 원래 제한 횟수와 새 생존 후보의 피해만 재사용
        } // 원래 연속 타격 방식의 피해 복제 종료
        if (card.SourceData.WandRule == WandCardRule.EyeOfStorm) // 두 차례 전역 공격의 전용 반향 확인
        { // 원래 광역 차례의 반향 시작
            return ApplyEyeOfStorm(card, echoPlan); // 현재 상태에 따른 절반 피해로 같은 광역 순서 재생
        } // 원래 광역 차례의 반향 종료
        IReadOnlyList<BattleUnitRuntime> targets = confirmedTargets; // 지정한 적은 같은 확정 순서 사용
        if (card.TargetType is CardTargetType.AllEnemies or CardTargetType.RandomTwoEnemies) // 피해 시점에 전체 또는 임의 생존 적을 고르는 원본 확인
        { // 자동 타격 명단 준비 시작
            var living = CollectValidTargets(card, enemyUnitViews); // 실제 현재 화면에 남은 유효한 생존 적을 기존 조건으로 조회
            targets = card.TargetType == CardTargetType.RandomTwoEnemies ? PickDistinctRandomEnemies(living) : living; // 원래 자동 공격의 전체 또는 두 무작위 적 규칙 보존
        } // 자동 타격 명단 준비 종료
        long damage = 0; // 복제된 각각의 실제 체력 피해 합계 초기화
        foreach (BattleUnitRuntime target in targets) // 원본 카드의 고정 또는 자동 대상 순서 순회
        { // 현재 대상의 생존 타격 준비 시작
            if (!CanResolveWandEcho(card, echoPlan.Round)) // 앞선 복제 타격의 승리와 단계 변경 및 시전자 생존 재확인
            { // 이어갈 수 없는 반향 확정 시작
                break; // 남은 모든 타격과 피해 무작위 적용 중단
            } // 이어갈 수 없는 반향 확정 종료
            if (!turnRuntime.CanSelectLivingUnit(target, BattleTeam.Enemy) || !IsValidTarget(card, target)) // 이미 쓰러졌거나 원본 대상 조건을 잃은 적 확인
            { // 현재 무효 대상 생략 시작
                continue; // 수동 지정에서 다른 적으로 대체하지 않고 다음 선택 확인
            } // 현재 무효 대상 생략 종료
            for (int hit = 0; hit < card.SourceData.DamageHitCount && !target.IsDead && CanResolveWandEcho(card, echoPlan.Round); hit++) // 원본 타격 수와 직전 타격 후 다시 살아 있는 전투 재확인
            { // 같은 대상의 독립 피해 처리 시작
                damage += ApplyIsolatedDamageHit(card, target, echoPlan: echoPlan); // 원래 명중, 약점, 방어도에 절반 기본 피해 전달
            } // 같은 대상의 독립 피해 처리 종료
        } // 현재 대상의 생존 타격 준비 종료
        return damage; // 전체 반향의 실제 체력 피해 합산 반환
    } // 반향 대상과 타격 확인 종료

    private bool CanResolveWandEcho(CardInstance card, int round) // 시전한 아군 턴에서만 유효한 실제 반향 여부
    { // 동일 전투 단계 확인 시작
        return !card.OwnerUnit.IsDead && turnRuntime.CurrentRound == round && turnRuntime.HasSelectableEnemy; // 페이즈 전환과 종료가 없고 생존 대상과 시전자가 있는 조건
    } // 동일 전투 단계 확인 종료

    private static int GetRandomWandHitCount(CardInstance card) // 원본 무작위 지팡이 공격의 최대 타격 수
    { // 규칙별 타격 횟수 조회 시작
        return card.SourceData.WandRule switch // 실제 카드가 지원하는 현재 공격 방식
        { // 지팡이 연속 타격 규칙 선택 시작
            WandCardRule.ChainLightning => 3, // 기존 연쇄 번개의 세 번 타격
            WandCardRule.FireDownpour => 6, // 화염 폭우의 여섯 번 타격
            _ => 0 // 일반 지팡이 카드의 무작위 연타 미지정
        }; // 지팡이 연속 타격 규칙 선택 종료
    } // 규칙별 타격 횟수 조회 종료

    private long ApplyRandomWandHits(CardInstance card, (int Percent, int Paid, int Round) echoPlan = default) // 원본과 반향의 독립적인 지팡이 타격 적용
    { // 현재 생존 적의 타격 정산 시작
        long damage = 0; // 독립 타격이 실제로 잃게 한 체력 합계
        int hitCount = GetRandomWandHitCount(card); // 원본과 복제에 같은 카드의 타격 한도
        for (int hit = 0; hit < hitCount && !card.OwnerUnit.IsDead && !turnRuntime.IsBattleEnded && !turnRuntime.HasPendingBossPhaseTransition && (echoPlan.Percent == 0 || CanResolveWandEcho(card, echoPlan.Round)); hit++) // 남은 타격과 원본 또는 반향의 현재 전투 경계 확인
        { // 현재 생존 대상의 독립 추첨 시작
            var target = SwordCardCombat.PickRandomLivingTarget(enemyActions.EnemyUnits); // 이전 선택을 보존하지 않는 현재 생존 적의 무작위 추첨
            if (target == null) // 각 타격 전 실제로 남은 적 확인
            { // 생존 대상 소진 시작
                break; // 마지막 처치 이후 대상 추첨과 피해 중단
            } // 생존 대상 소진 종료
            damage += ApplyIsolatedDamageHit(card, target, echoPlan: echoPlan); // 현재 대상의 원래 명중과 마법 방어 및 독립 반향 지원
        } // 현재 생존 대상의 독립 추첨 종료
        return damage; // 실제 타격들의 오버플로 없는 체력 피해 합계
    } // 현재 생존 적의 타격 정산 종료

    private long ApplyEyeOfStorm(CardInstance card, (int Percent, int Paid, int Round) echoPlan = default) // 살아 있는 적 전체를 차례마다 독립 타격
    { // 폭풍의 눈 전체 타격 시작
        long damage = 0; // 실제 체력 피해의 오버플로 없는 합계
        for (int wave = 0; wave < card.SourceData.DamageHitCount; wave++) // 카드에 등록된 전체 공격 차례
        { // 현재 차례 광역 타격 시작
            foreach (var target in CollectValidTargets(card, enemyUnitViews)) // 새 차례 전 실제 생존 적 명단 재조회
            { // 대상 한 명의 독립 타격 시작
                if (card.OwnerUnit.IsDead || !turnRuntime.HasSelectableEnemy || (echoPlan.Percent > 0 && turnRuntime.CurrentRound != echoPlan.Round)) // 시전자와 전투 전환 및 현재 반향 차례 확인
                { // 더 진행할 수 없는 광역 정산 시작
                    return damage; // 보스 전환과 승리 또는 반향 종료 뒤 추가 타격 없음
                } // 더 진행할 수 없는 광역 정산 종료
                if (turnRuntime.CanSelectLivingUnit(target, BattleTeam.Enemy)) // 앞선 타격으로 이미 제거된 대상 제외
                { // 살아 있는 현재 적의 타격 계산 시작
                    damage += ApplyIsolatedDamageHit(card, target, echoPlan: echoPlan); // 대상별 실제 명중과 활성 감전의 현재 피해
                } // 살아 있는 현재 적의 타격 계산 종료
            } // 대상 한 명의 독립 타격 종료
        } // 현재 차례 광역 타격 종료
        return damage; // 원본 또는 반향의 전체 실제 체력 피해
    } // 폭풍의 눈 전체 타격 종료

    private int ApplyDanceHits(CardInstance card, IReadOnlyList<BattleUnitRuntime> area) // 선택 범위 안에서 독립 타격 처리
    { // 무작위 연타 시작
        int damage = 0; // 이번 카드가 입힌 체력 피해 합계
        var hit = new BattleUnitRuntime[1]; // 타격마다 사용할 약점 판정 대상
        try // 조기 종료 시 피해 문맥 정리 보장
        { // 각 타격 처리 시작
            for (int index = 0; index < card.SourceData.DamageHitCount && !card.OwnerUnit.IsDead && !turnRuntime.IsBattleEnded && !turnRuntime.HasPendingBossPhaseTransition; index++) // 남은 타격과 전투 상태 검사
            { // 생존 대상 추첨 시작
                hit[0] = SwordCardCombat.PickRandomLivingTarget(area); // 검 카드가 정한 범위 안에서 생존자 재선택
                if (hit[0] == null) // 범위 안의 적 전멸 확인
                { // 범위 소진 처리 시작
                    break; // 다른 칸으로 넘어가는 공격 방지
                } // 범위 소진 처리 종료
                BattleCardDamageContext.Begin(card, hit); // 실제 선택된 한 적의 약점 판정 준비
                damage += ApplyDamageHit(card, hit[0]); // 독립 명중과 피해 계산
            } // 생존 대상 추첨 종료
        } // 각 타격 처리 종료
        finally // 타격 문맥 정리
        { // 정리 시작
            BattleCardDamageContext.Clear(); // 다음 공격을 위한 피해 문맥 해제
        } // 정리 종료
        return damage; // 실제 체력 피해 합계 반환
    } // 무작위 연타 종료
    private int ApplyBreakthroughFollowUp(CardInstance card, BattleUnitRuntime defeated) // 처치 뒤 다음 생존 적을 고르는 추가 타격
    { // 후속 타격 시작
        if (turnRuntime.IsBattleEnded || turnRuntime.HasPendingBossPhaseTransition || card.OwnerUnit.IsDead) // 전투와 시전자의 진행 가능 여부 확인
        { // 후속 타격 불가 시작
            return 0; // 전투 종료와 보스 전환 및 사망 시 추가 공격 중단
        } // 후속 타격 불가 종료
        var enemies = enemyActions.EnemyUnits; // 실제 전투 배치 목록
        int defeatedIndex = 0; // 쓰러진 적의 배치 위치 초기값
        while (defeatedIndex < enemies.Count && enemies[defeatedIndex] != defeated) // 쓰러진 적의 정확한 배치 위치 확인
        { // 배치 검색 시작
            defeatedIndex++; // 다음 칸 확인
        } // 배치 검색 종료
        if (defeatedIndex == enemies.Count) // 전투 배치에 해당 대상이 있는지 확인
        { // 배치에 없는 대상 처리 시작
            return 0; // 다음 적을 결정할 수 없는 경우 종료
        } // 배치에 없는 대상 처리 종료
        for (int offset = 1; offset < enemies.Count; offset++) // 다음 칸부터 마지막을 지나 처음까지 순회
        { // 이후 배치 적 확인 시작
            var next = enemies[(defeatedIndex + offset) % enemies.Count]; // 실제 배치의 다음 칸
            if (next != null && !next.IsDead) // 한 번 더 타격할 생존 적 확인
            { // 생존 적 추가 타격 시작
                return ApplyIsolatedDamageHit(card, next); // 원래 비용으로 이미 처리된 카드의 독립 추가 타격
            } // 생존 적 추가 타격 종료
        } // 이후 배치 적 확인 종료
        return 0; // 남은 적이 없으면 추가 피해 없음
    } // 후속 타격 종료

    private int ApplyKingsSecondHit(CardInstance card) // 전체 타격 뒤 최대 체력 기준으로 추가 대상 결정
    { // 추가 공격 시작
        BattleUnitRuntime strongest = null; // 현재 가장 높은 최대 체력의 생존 적
        foreach (var enemy in enemyActions.EnemyUnits) // 실제 전투 배치 앞에서 뒤로 순회
        { // 적별 최대 체력 비교 시작
            if (enemy != null && !enemy.IsDead && (strongest == null || enemy.MaxHealth > strongest.MaxHealth)) // 사망 제외 및 동률이면 먼저 나온 적 유지
            { // 추가 대상 갱신 시작
                strongest = enemy; // 더 높은 최대 체력의 생존 적 보관
            } // 추가 대상 갱신 종료
        } // 적별 최대 체력 비교 종료
        if (strongest == null) // 전체 공격으로 전멸 확인
        { // 추가 타격 없음 시작
            return 0; // 타격 대상 없는 경우 종료
        } // 추가 타격 없음 종료
        return ApplyIsolatedDamageHit(card, strongest, card.SourceData.SecondaryEffectValue); // 추가 피해 수치를 사용하는 독립 타격
    } // 추가 공격 종료

    private int ApplyIsolatedDamageHit(CardInstance card, BattleUnitRuntime target, int? additionalBase = null, (int Percent, int Paid, int Round) echoPlan = default) // 별도 타격 원본과 독립 명중 및 약점 준비
    { // 독립 피해 문맥 시작
        BattleCardDamageContext.Begin(card, new[] { target }); // 다시 고른 대상의 약점 문맥 준비
        try // 후속 타격 뒤 문맥 정리 보장
        { // 독립 후속 타격 시작
            return ApplyDamageHit(card, target, additionalBase, echoPlan); // 원래 후속 타격과 반향의 공통 명중 및 방어 경로
        } // 독립 후속 타격 종료
        finally // 추가 공격 정리
        { // 약점 문맥 정리 시작
            BattleCardDamageContext.Clear(); // 다음 카드로 넘어가는 약점 문맥 해제
        } // 약점 문맥 정리 종료
    } // 추가 공격 종료

    private static int ScaleWandEchoDamage(CardInstance card, int? rolled, int paid, int percent, int printedBonus = 0) // 별도 한 타격에서 복제할 실제 원본 피해 계산
    { // 실제 기본 피해 비율 계산 시작
        long raw = rolled.HasValue ? rolled.Value : card.SourceData.SpendsAllActionPoints ? (long)paid * card.EffectValueBeforeEnchantment : card.EffectValueBeforeEnchantment; // 복제 피해에서 일회 강화와 구분한 원래 고정 및 가변 수치
        long bounded = Math.Clamp(raw + printedBonus, 0L, int.MaxValue); // 시트에 명시한 조건부 원본 피해와 정상 정수 범위 반영
        return (int)(bounded * Math.Clamp(percent, 0, 100) / 100L); // 원본과 준비 수치의 곱을 정수 피해로 내림
    } // 실제 기본 피해 비율 계산 종료

    private int ApplyDamageHit(CardInstance cardInstance, BattleUnitRuntime targetUnit, int? additionalBase = null, (int Percent, int Paid, int Round) echoPlan = default) // 원본 또는 반향 공격의 한 타격 적용
    { // 타격 판정 시작
        if (!BattleHitChance.Roll(cardInstance.OwnerUnit, targetUnit)) // 실명 상태의 공격 명중 확인
        { // 카드 공격 빗나감 처리 시작
            if (echoPlan.Percent == 0) // 준비한 반향과 다른 원래 공격의 빗나감 확인
            { // 공허 붕괴의 원본 실패 비용 시작
                ApplyVoidCollapseFailureCost(cardInstance, defeatedByHit: false); // 정상 사용한 원본 빗나감의 실제 정신력 비용 처리
            } // 공허 붕괴의 원본 실패 비용 종료
            BattleCardDamageContext.TryConsume(out _, out _); // 빗나간 대상의 카드 피해 문맥 소비
            FindUnitView(targetUnit)?.ShowStatusFeedback("빗나감", false); // 선택 대상의 빗나감 표시
            if (echoPlan.Percent == 0 && SunChainAttack.IsChainCard(cardInstance)) // 원본 태양 카드에만 별도 연속 공격 허용
            { // 다음 공격 판정 시작
                ApplySunChain(cardInstance, targetUnit); // 첫 공격과 독립적인 두 번째 공격 실행
            } // 다음 공격 판정 종료
            return 0; // 첫 공격 피해 없음 반환
        } // 카드 공격 빗나감 처리 종료
        if (BattleMagicMissileEffect.Matches(cardInstance)) // 실제 적에게 명중한 원본 마력탄 확인
        { // 해당 타격의 섬광 위치 기록 시작
            actionSequenceRunner.RecordMagicMissileImpact(FindUnitView(targetUnit)); // 원래 피해 계산과 같은 화면 좌표에 한 번의 표시 예약
        } // 해당 타격의 섬광 위치 기록 종료
        int? baseOverride = additionalBase; // 명시적인 추가 타격의 피해 수치 우선 사용
        if (!baseOverride.HasValue && cardInstance.SourceData.ShieldRule == ShieldCardRule.RendingShield) // 현재 방어도를 기본 피해로 쓰는 파열 방패 확인
        { // 방어도 기반 기본 피해 처리 시작
            baseOverride = cardInstance.OwnerUnit.CurrentBlock / 2; // 현재 방어도의 절반을 정수로 내린 기본 피해
        } // 방어도 기반 기본 피해 처리 종료
        if (!baseOverride.HasValue && cardInstance.SourceData.HasRandomDamage) // 실제로 두 피해 중 하나를 고르는 원본 확인
        { // 무작위 피해 굴림 시작
            baseOverride = BattleSupplyRelics.RollDamage(cardInstance); // 흑철 구슬과 강화가 반영된 정수 피해 선택
        } // 무작위 피해 굴림 종료
        if (echoPlan.Percent > 0) // 별도 명중에 성공한 순수 마법 반향 확인
        { // 원래 카드의 후속 효과를 거치지 않는 실제 반향 피해 시작
            int printedBonus = baseOverride.HasValue ? 0 : SwordCardCombat.GetConditionalWandDamageBonus(cardInstance, targetUnit); // 표식과 감전의 조건부 원본 및 굴린 원본 피해의 구분
            int echoedDamage = ScaleWandEchoDamage(cardInstance, baseOverride, echoPlan.Paid, echoPlan.Percent, printedBonus); // 실제 지불량과 확인한 원본 기본 피해의 비율 내림
            return targetUnit.TakeWandEchoDamage(echoedDamage, cardInstance.OwnerUnit).AppliedDamage; // 표준 저항과 방어도의 실제 체력 피해만 정산
        } // 원래 카드의 후속 효과를 거치지 않는 실제 반향 피해 종료
        if (baseOverride.HasValue && cardInstance.SourceData.HasRandomDamage && cardInstance.EnchantmentPercent > 0) // 일반 원본의 무작위 기본 피해도 일회 강화 대상으로 확인
        { // 이번 원래 피해 굴림의 일회 강화 시작
            baseOverride = CardInstance.ApplyEnchantment(baseOverride.Value, cardInstance.EnchantmentPercent); // 난수 횟수와 별개로 선택된 원래 기본 수치만 변경
        } // 이번 원래 피해 굴림의 일회 강화 종료
        long printedDamage = baseOverride.HasValue ? (long)baseOverride.Value + cardInstance.OwnerUnit.AttackPowerBonus : SwordCardCombat.BaseDamage(cardInstance, paidActionPoints, targetUnit); // 가변 또는 고정 카드의 공격력 합산
        if (cardInstance.SourceData.CupRule == CupCardRule.HolyWater && targetUnit.EnemySource?.IsUndead == true) // 불사형 추가 피해 카드가 실제 명중한 대상 확인
        { // 언데드 대상 추가 피해 시작
            printedDamage += cardInstance.SourceData.SecondaryEffectValue; // 방어와 저항 적용 전 같은 불사형 추가 피해 가산
        } // 언데드 대상 추가 피해 종료
        int baseDamage = (int)Math.Min(int.MaxValue, printedDamage + preparedSwordDamage); // 이번 타격의 원본 피해에 정비한 검 피해 가산
        if (cardInstance.SourceData.CardId == "CRD_ARC_13_FINISH" && targetUnit.HasDebuff) // 죽음 전용 카드와 약화된 적 확인
        { // 약화 대상 추가 피해 시작
            baseDamage = (int)Math.Min(int.MaxValue, (long)baseDamage * 3 / 2); // 임시 추가 피해 오십 퍼센트 적용
        } // 약화 대상 추가 피해 종료
        baseDamage = SwordCardCombat.ApplyAttackStances(cardInstance, targetUnit, baseDamage); // 타격 순간의 검 강화와 마무리 체력 조건 적용
        int modifiedDamage = cardInstance.OwnerUnit.ModifyCardOutgoingDamage(baseDamage, cardInstance.DamageType); // 마법 카드의 공격력 증폭과 기존 정신 상태 적용
        modifiedDamage = SwordCardCombat.RollCritical(cardInstance, targetUnit, modifiedDamage, out bool critical); // 현재 피격 대상의 활성 약화를 포함한 치명타 확률 적용
        if (critical) // 치명타 발동 확인
        { // 치명타 표시 시작
            FindUnitView(targetUnit)?.ShowStatusFeedback("치명타", true); // 피격 대상에 치명타 표시
        } // 치명타 표시 종료
        modifiedDamage = ApplyDirectHitRelics(cardInstance, targetUnit, modifiedDamage); // 방어 판정 전 첫 직접 명중에만 가산
        modifiedDamage = ApplyLandedFlatCardDamage(cardInstance, targetUnit, modifiedDamage); // 치명타 직후 첫 피해와 유지 피해 가산
        var overload = cardInstance.CardType == CardType.Wand && cardInstance.DamageType == BattleDamageType.Magical ? cardInstance.OwnerUnit.GetStatusEffect(BattleStatusEffectType.WandOverload) : null; // 실제 원래 지팡이 마법 타격의 현재 과부하 확인
        if (overload is { IsExpired: false } && modifiedDamage > 0) // 반향 이전에 분리한 양수 원본 피해의 보너스 조건
        { // 현재 아군 턴 과부하 피해 계산 시작
            modifiedDamage = (int)Math.Min(int.MaxValue, (long)modifiedDamage * (100L + overload.EffectiveValue) / 100); // 줄일 수 있는 일반 방어 앞에서 지팡이 원본 피해 증가와 내림
        } // 현재 아군 턴 과부하 피해 계산 종료
        int bypass = cardInstance.SourceData.SwordRule == SwordCardRule.PiercingLunge ? cardInstance.SourceData.SecondaryEffectValue : 0; // 파고들기의 타격 한정 방어도 우회 비율
        int healthBefore = targetUnit.CurrentHealth; // 카드 타격 직전 대상 체력 보관
        if (cardInstance.SourceData.CupRule == CupCardRule.HolyJudgment) // 실제 명중한 심판의 기존 불사 해제 확인
        { // 명중 대상의 생존 보호 제거 시작
            RemoveCardTargetStatus(targetUnit, BattleStatusEffectType.UndyingGuard); // 피해 직전에 불굴의 일회 생존 보호 제거
            RemoveCardTargetStatus(targetUnit, BattleStatusEffectType.DefeatWard); // 일반 피해 무효와 별개인 전투불능 방지 제거
        } // 명중 대상의 생존 보호 제거 종료
        int penetration = cardInstance.SourceData.WandRule == WandCardRule.ArcaneSurge ? cardInstance.SourceData.SecondaryEffectValue : 0; // 이번 비전 쇄도가 무시할 현재 마법 저항 비율
        BattleDamageResult damageResult = targetUnit.TakeDamage(modifiedDamage, cardInstance.DamageType, cardInstance.OwnerUnit, bypassBlockPercent: bypass, alliedDirectAttack: true, magicalResistancePenetrationPercent: penetration); // 직접 타격의 표식과 실제 저항 관통 및 기존 방어도 계산
        ApplyVoidCollapseFailureCost(cardInstance, damageResult.WasTargetDefeated && targetUnit.IsDead); // 이번 원래 타격에 실제로 쓰러진 적인지 판별한 실패 비용 처리
        if (cardInstance.SourceData.WandRule == WandCardRule.ManaSiphon && targetUnit.Team == BattleTeam.Enemy && healthBefore > 0 && UnityEngine.Random.Range(0, 100) < 30) // 피격 직전 생존한 적에게 실제 명중한 마력 흡수의 삼십 퍼센트 판정
        { // 마력 흡수 회복 시작
            int regained = sharedActionPoints.Gain(cardInstance.SourceData.SecondaryEffectValue); // 기존 최대치 안에서 실제 현재 행동력 한 점 회복
            if (regained > 0) // 실제로 행동력을 되찾았는지 확인
            { // 회복 표시 시작
                FindUnitView(cardInstance.OwnerUnit)?.ShowStatusFeedback($"행동력 +{regained}", true); // 소유자의 실제 행동력 회복 수치 표시
            } // 회복 표시 종료
        } // 마력 흡수 회복 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.MentalShock && targetUnit is { Team: BattleTeam.Enemy, IsDead: false } && // 직접 타격 뒤 실제 생존 적 확인
            !turnRuntime.IsBattleEnded && cardInstance.SourceData.SecondaryEffectValue > 0) // 진행 중인 원래 타격과 원본 정신력 감소 수치 확인
        { // 정신 충격의 명중 후 정신력 감소 시작
            targetUnit.ChangeMental(-cardInstance.SourceData.SecondaryEffectValue, BattleMentalChangeReason.CardEffect, cardInstance.OwnerUnit); // 카드 소유자가 실제로 준 정신력 피해를 기존 전투 신호로 전달
        } // 정신 충격의 명중 후 정신력 감소 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.ExecutionerSword && healthBefore > 0 && damageResult.AppliedDamage == healthBefore && targetUnit.IsDead) // 이 카드의 직접 체력 피해로 적 처치 확인
        { // 처치 환급 시작
            sharedActionPoints.Gain(cardInstance.SourceData.SecondaryEffectValue); // 공용 행동력 최대치 안에서 환급
        } // 처치 환급 종료
        bool voidHand = cardInstance.SourceData.WandRule == WandCardRule.VoidHand && targetUnit.Team == BattleTeam.Enemy && !damageResult.WasDamageNullified; // 살아 있는 적에게 들어간 공허의 손의 피해 무효 여부 확인
        if (!targetUnit.IsDead && (voidHand || cardInstance.SourceData.SwordRule == SwordCardRule.JudgmentSlash || cardInstance.SourceData.CupRule == CupCardRule.JudgmentWater)) // 살아 있는 적의 실제 명중 뒤 일반 강화 하나 해제
        { // 명중한 카드의 기존 강화 해제 시작
            var buff = targetUnit.FirstRemovableBuff(activeOnly: voidHand); // 공허의 손은 사라지지 않은 가장 오래된 일반 강화만 확인
            if (buff != null && targetUnit.RemoveStatusEffect(buff.EffectType)) // 보호 정신 상태를 제외한 실제 해제 확인
            { // 해제 피드백 시작
                FindUnitView(targetUnit)?.ShowStatusFeedback(buff.DisplayName + " 해제", false); // 해제된 강화 이름 표시
            } // 해제 피드백 종료
        } // 명중한 카드의 기존 강화 해제 종료
        if (!targetUnit.IsDead && cardInstance.SourceData.SwordRule == SwordCardRule.Charge) // 명중한 돌진과 생존 대상 확인
        { // 돌진 후속 효과 시작
            targetUnit.QueueChargeDefense(cardInstance.OwnerUnit, cardInstance.SourceData.SecondaryEffectValue); // 피격 대상의 다음 턴 물리 방어 약화 예약
        } // 돌진 후속 효과 종료
        bool hasAuthoredBlind = cardInstance.StatusEffectType == BattleStatusEffectType.Blind && cardInstance.SourceData.OptionalBlindChancePercent >= 0; // 별도 판정을 사용하도록 명시한 공격 실명 확인
        if (!targetUnit.IsDead && !hasAuthoredBlind && cardInstance.SourceData.SwordRule == SwordCardRule.FlameSlash) // 명중한 화염 참격의 일반 상태만 처리
        { // 화상 부여 시작
            var result = targetUnit.ApplyStatusEffect(cardInstance.StatusEffectType, cardInstance.SourceData.SecondaryEffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit, corrosionFamily: cardInstance.SourceData.IsCorrosionStatus); // 명시 침식 여부를 보존한 화염 카드 상태 적용
            FindUnitView(targetUnit)?.ShowStatusApplyFeedback(cardInstance.StatusEffectType, result); // 화상 부여 결과 표시
        } // 화상 부여 종료
        if (hasAuthoredBlind && TryRollAuthoredBlind(cardInstance, targetUnit, cardInstance.SourceData.SecondaryEffectValue)) // 실제 명중 카드에 명시한 확률 실명 한 번 확인
        { // 명중 후 선택 실명 처리 시작
            var result = targetUnit.ApplyStatusEffect(BattleStatusEffectType.Blind, cardInstance.SourceData.SecondaryEffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit, corrosionFamily: cardInstance.SourceData.IsCorrosionStatus); // 실제 확률 성공 뒤 기존 면역을 거친 실명 부여
            FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.Blind, result); // 명중 후 실명 결과 표시
        } // 명중 후 선택 실명 처리 종료
        var appliedStatus = SwordCardCombat.ApplyHitStatus(cardInstance, targetUnit, supplyRelics, out var statusResult); // 실제 명중과 보유 구슬을 반영한 후속 상태 확인
        if (cardInstance.CardType == CardType.Wand && cardInstance.SourceData.ApplyWandHitStatus && !hasAuthoredBlind && !targetUnit.IsDead && targetUnit.Team == BattleTeam.Enemy && // 피해가 명중한 생존 적과 명시된 지팡이 효과 확인
            TryRollWandHitStatus(cardInstance, targetUnit)) // 대상과 설정이 유효한 명중 상태만 실제 확률 확인
        { // 지팡이 명중 상태 부여 시작
            appliedStatus = cardInstance.StatusEffectType; // 원본이 지정한 감전과 기절 및 일반 지팡이 상태 종류 확인
            statusResult = targetUnit.ApplyStatusEffect(appliedStatus, cardInstance.SourceData.SecondaryEffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit, corrosionFamily: cardInstance.SourceData.IsCorrosionStatus); // 기존 상태 면역과 중첩 및 발생자 전달
            if (appliedStatus == BattleStatusEffectType.ActionSpeedDown && statusResult is (BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked)) // 실제 명중으로 적의 속도가 느려졌는지 확인
            { // 확정된 적 행동 순서 갱신 시작
                enemyActions.RefreshActionOrder(); // 같은 행동과 대상 및 원래 속도 난수를 보존한 순서 변경
            } // 확정된 적 행동 순서 갱신 종료
        } // 지팡이 명중 상태 부여 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.ShockTransfer) // 직접 타격이 빗나가지 않은 감전 전이 확인
        { // 다른 적의 감전 전이 시작
            TransferWandShock(cardInstance, targetUnit); // 처음 명중한 적과 다른 생존 적의 기존 감전 처리
        } // 다른 적의 감전 전이 종료
        if (cardInstance.SourceData.ShieldRule == ShieldCardRule.ShieldBash && !targetUnit.IsDead && supplyRelics.RollOptionalStatus(targetUnit, cardInstance.SourceData.SecondaryEffectValue)) // 방패 밀치기의 명중 후 행동 제한 확률 확인
        { // 방패로 적 행동 밀어내기 시작
            appliedStatus = BattleStatusEffectType.Stagger; // 적만 사용하는 다음 행동 취소 상태 선택
            statusResult = targetUnit.ApplyStatusEffect(appliedStatus, 1, 1, 1, cardInstance.OwnerUnit); // 다음 유효 적 행동을 한 번 취소
        } // 방패로 적 행동 밀어내기 종료
        if (cardInstance.SourceData.ShieldRule == ShieldCardRule.SuppressionStrike && !targetUnit.IsDead) // 억제 타격이 명중한 생존 적 확인
        { // 적 공격력 약화 처리 시작
            appliedStatus = BattleStatusEffectType.AttackPowerDown; // 기존 적 공격력 감소 상태 사용
            statusResult = targetUnit.ApplyStatusEffect(appliedStatus, cardInstance.SourceData.SecondaryEffectValue, cardInstance.StatusDuration, 1, cardInstance.OwnerUnit); // 대상의 두 적 턴 동안의 공격력 약화 부여
        } // 적 공격력 약화 처리 종료
        if (appliedStatus != BattleStatusEffectType.None) // 추가 상태 적용 또는 면역 확인
        { // 검 전용 상태 피드백 시작
            FindUnitView(targetUnit)?.ShowStatusApplyFeedback(appliedStatus, statusResult); // 명중 후 상태 부여 결과 표시
        } // 검 전용 상태 피드백 종료
        if (SunChainAttack.IsChainCard(cardInstance) && !turnRuntime.HasPendingBossPhaseTransition) // 보스 전환이 예약되지 않은 태양 연속 공격 확인
        { // 태양 추가 공격 시작
            ApplySunChain(cardInstance, targetUnit); // 다른 생존 적에게 두 번째 공격 적용
        } // 태양 추가 공격 종료
        var chariotMark = chariotConcentration.RecordAttack(cardInstance, targetUnit); // 자동 패시브와 구별한 전차 전용 카드 표식 결과
        ApplyCrystalMarkDefenseLoss(cardInstance, targetUnit, BattleStatusEffectType.ChariotMark, chariotMark); // 직접 명중한 전용 카드의 실제 표식에 결정눈 적용
        string damageLabel = cardInstance.DamageType == BattleDamageType.Magical ? "마법" : cardInstance.DamageType == BattleDamageType.Physical ? "물리" : "일반"; // 피해 유형 이름 계산
        Debug.Log($"[BattleDamage] 카드 / {cardInstance.DisplayName} / 대상 {targetUnit.DisplayName} / {damageLabel} / 원본 {damageResult.RawDamage} / 방어 {damageResult.DefenseValue} / 감소 {damageResult.ReducedDamage} / 최종 {damageResult.FinalDamage} / 실제 {damageResult.AppliedDamage}"); // 카드 피해 상세 출력
        return damageResult.AppliedDamage; // 실제 피해량 반환
    } // 타격 판정 종료
    private void TransferWandShock(CardInstance card, BattleUnitRuntime primary) // 실제로 명중한 감전 전이의 다른 적 적용
    { // 최초 감전과 다른 실제 생존 적 탐색 시작
        if (primary.Team != BattleTeam.Enemy || turnRuntime.IsBattleEnded || turnRuntime.HasPendingBossPhaseTransition) // 원래 피격 적과 계속 진행 중인 현재 전투 확인
        { // 실제 전이 대상 탐색 제외 시작
            return; // 종료나 이전 보스 페이즈에서 다른 대상 지정 방지
        } // 실제 전이 대상 탐색 제외 종료
        var candidates = new List<BattleUnitRuntime>(); // 최초 피격자와 별개로 뽑을 생존 대상 목록
        foreach (var enemy in enemyActions.EnemyUnits) // 명중 뒤의 현재 적 명단 재확인
        { // 다른 생존 적 조건 확인 시작
            if (enemy != primary && enemy?.Team == BattleTeam.Enemy && !enemy.IsDead) // 처음 명중한 적을 뺀 실제 생존자 확인
            { // 실제 전이 후보 등록 시작
                candidates.Add(enemy); // 난수로 고를 별개 적 후보 보관
            } // 실제 전이 후보 등록 종료
        } // 다른 생존 적 조건 확인 종료
        if (candidates.Count == 0) // 최초 적 밖에 실제 전이 후보가 있는지 확인
        { // 혼자 남은 적 처리 시작
            return; // 원래 마법 피해만 남기고 무작위 추첨 생략
        } // 혼자 남은 적 처리 종료
        var target = candidates[RelicRandomCardTarget.PickIndex(candidates.Count, count => UnityEngine.Random.Range(0, count))]; // 실제 카드의 기존 무작위 적 선택 규칙 적용
        var status = target.ApplyStatusEffect(BattleStatusEffectType.Shock, card.SourceData.SecondaryEffectValue, card.StatusDuration, card.StatusMaximumStacks, card.OwnerUnit); // 원래 감전을 그대로 두고 다른 적에게 정의된 감전 부여
        FindUnitView(target)?.ShowStatusApplyFeedback(BattleStatusEffectType.Shock, status); // 원래 면역 또는 성공의 기존 감전 결과 표시
    } // 최초 감전과 다른 실제 생존 적 탐색 종료
    private bool TryRollWandHitStatus(CardInstance card, BattleUnitRuntime target) // 지팡이 명중 상태의 선택 확률과 면역 확인
    { // 지팡이 상태 판정 시작
        if (card.SourceData.WandRule == WandCardRule.ArcaneLiberationRite && paidActionPoints < 4) // 마도 해방식이 실제로 지불한 행동력의 기절 임계 확인
        { // 원본 기절 판정 자격 없음 시작
            return false; // 면역과 확률 및 선택 상태 유물을 사용하기 전 기절 처리 생략
        } // 원본 기절 판정 자격 없음 종료
        var status = card.StatusEffectType; // 원본에 설정된 적용 상태 종류
        int chance = card.SourceData.WandHitStatusChancePercent; // 유효 범위의 원본 명중 상태 확률
        if (chance <= 0 || status is (BattleStatusEffectType.None or BattleStatusEffectType.Awakening or BattleStatusEffectType.Collapse) || // 확률 없음과 일반 카드로 줄 수 없는 상태 확인
            !Enum.IsDefined(typeof(BattleStatusEffectType), status) || card.SourceData.SecondaryEffectValue <= 0 || card.StatusDuration <= 0 || card.StatusMaximumStacks <= 0) // 실제 존재하는 상태와 양수 수치 및 지속 확인
        { // 적용 불가 상태 처리 시작
            return false; // 잘못된 설정으로 인한 유물 또는 난수 소모 차단
        } // 적용 불가 상태 처리 종료
        return chance >= 100 || (target.HasStatusImmunity && BattleStatusEffectInstance.IsDebuffType(status)) || supplyRelics.RollOptionalStatus(target, chance, status, card.OwnerUnit); // 확정과 면역에는 기존 경로를 쓰고 실제 확률형 상태의 출처 전달
    } // 지팡이 상태 판정 종료
    private bool TryRollAuthoredBlind(CardInstance card, BattleUnitRuntime target, int value) // 명시된 실명과 유효한 현재 대상의 선택 확률 확인
    { // 카드 실명 확률 검사 시작
        return value > 0 && !target.IsDead && card.SourceData.OptionalBlindChancePercent >= 0 && supplyRelics.RollAuthoredBlind(target, card.SourceData.OptionalBlindChancePercent, card.OwnerUnit); // 명시 실명과 실제 아군 출처의 기존 선택 확률 판정
    } // 카드 실명 확률 검사 종료
    private void ApplyCrystalMarkDefenseLoss(CardInstance card, BattleUnitRuntime target, BattleStatusEffectType type, BattleStatusEffectApplyResult result) // 카드 표식 성공으로 실제 적 방어도 약화 처리
    { // 결정눈 표식의 성공 여부 검사 시작
        if (result is not (BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked) || target.IsDead || target.Team != BattleTeam.Enemy || // 아군 카드의 생존 적에 대한 성공한 상태 확인
            type is not (BattleStatusEffectType.ChariotMark or BattleStatusEffectType.PublicMark or BattleStatusEffectType.DuelMark) || target.GetStatusEffect(type) is not { IsExpired: false }) // 합법 카드 표식의 실제 현재 상태 확인
        { // 결정눈 유효 표식 없음 시작
            return; // 일반 약화와 상태 반복 및 실패 표식 제외
        } // 결정눈 유효 표식 없음 종료
        var relic = BattleSupplyRelics.Find(RelicEffectType.MarkPhysicalDefenseLoss); // 실제 보유한 표식 방어 약화 유물 확인
        if (relic?.EffectValue > 0) // 활성 유물의 원본 방어 감소량 확인
        { // 실제 표식 방어 약화 적용 시작
            var applied = target.ApplyStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown, relic.EffectValue, relic.StatusDuration, 1, card.OwnerUnit); // 보통 물리 방어 감소와 별개로 적 차례 기준 상태 적용
            FindUnitView(target)?.ShowStatusApplyFeedback(BattleStatusEffectType.CrystalMarkDefenseDown, applied); // 실제 감소의 상태 적용 결과 표시
        } // 실제 표식 방어 약화 적용 종료
    } // 결정눈 표식의 성공 여부 검사 종료
    private int GetDirectCardHealing(CardInstance card, int healing) // 회복자의 정신력 기준 직접 카드 회복 총량
    { // 직접 회복 보정 시작
        int echo = healing > 0 && turnRuntime.IsPlayerTurn ? card.OwnerUnit.GetStatusEffect(BattleStatusEffectType.MiracleEcho)?.EffectiveValue ?? 0 : 0; // 이번 아군 턴 직접 회복 카드의 시전자 강화량
        int modified = (int)Math.Min(int.MaxValue, (long)card.OwnerUnit.ModifyOutgoingHealing(healing) + echo); // 정신 상태 보정 뒤 기적의 잔향 가산
        int bonus = healing > 0 && card.OwnerUnit.CurrentMental >= 50 ? RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.HighMentalCardHealing)?.EffectValue ?? 0 : 0; // 정신력 오십 이상인 회복자의 흰 장미 가산량
        int confession = healing > 0 && card.OwnerUnit.CurrentMental <= 50 ? BattleSupplyRelics.Find(RelicEffectType.LowMentalCardHealing)?.EffectValue ?? 0 : 0; // 정신력 오십 이하인 시전자의 작은 고백패 가산량
        return (int)Math.Min(int.MaxValue, (long)modified + bonus + confession + (healing > 0 ? bandageBonus : 0)); // 수신 보정과 최대 체력을 적용하기 전 직접 회복 수치
    } // 직접 회복 보정 종료
    private int RestoreDirectCardHealth(CardInstance card, BattleUnitRuntime target, int healing) // 직접 카드 회복과 아군 턴의 첫 유지 회복 처리
    { // 실제 카드 회복 시작
        bool hasInjuredAllyTarget = healing > 0 && target != null && target.Team == BattleTeam.Ally && !target.IsDead && target.CurrentHealth < target.MaxHealth; // 양의 직접 회복과 실제 부상 있는 생존 아군 확인
        if (hasInjuredAllyTarget && turnRuntime.IsPlayerTurn && lastRetainedHealingRound != turnRuntime.CurrentRound && runtimeDeck.HasRetainedHandCardOtherThan(card)) // 실제 회복 대상과 사용 후 남는 유지 손패 및 턴 제한 확인
        { // 흰 깃털 펜던트 처리 시작
            int bonus = BattleSupplyRelics.Find(RelicEffectType.RetainedHandHealing)?.EffectValue ?? 0; // 보유한 유지 회복 유물의 수치
            if (bonus > 0) // 양수 가산량 확인
            { // 유지 회복 보너스 적용 시작
                lastRetainedHealingRound = turnRuntime.CurrentRound; // 회복 이벤트 전에 같은 턴 재발동 차단
                healing = (int)Math.Min(int.MaxValue, (long)healing + bonus); // 이번 대상의 회복량 증가
            } // 유지 회복 보너스 적용 종료
        } // 흰 깃털 펜던트 처리 종료
        int cupBonus = 0; // 현재 직접 회복의 성수 마개 가산량
        if (hasInjuredAllyTarget && card.CardType == CardType.Cup) // 직접 회복이 가능한 실제 성배 대상인지 확인
        { // 성배 회복 가산 시작
            cupBonus = usedFirstCupHealingBonus ? cupHealingBonusForCurrentCard : BattleSupplyRelics.Find(RelicEffectType.FirstCupHealingBonus)?.EffectValue ?? 0; // 최초 후보 또는 현재 활성 카드에만 가산량 설정
        } // 성배 회복 가산 종료
        int amount = (int)Math.Min(int.MaxValue, (long)healing + cupBonus); // 받는 회복 보정 이전에 직접 가산을 합산
        int restored = target?.RestoreHealth(amount, card.OwnerUnit) ?? 0; // 받는 회복 보정과 체력 상한이 적용된 실제 회복량 확인
        if (cupBonus > 0 && restored > 0) // 부상 아군에게 실제 양의 성배 회복이 적용됐는지 확인
        { // 전투당 첫 성수 마개 회복 확정 시작
            usedFirstCupHealingBonus = true; // 같은 전투의 다른 카드에 대한 추가 발동 차단
            cupHealingBonusForCurrentCard = cupBonus; // 이 카드의 남은 부상 아군에게도 같은 가산 적용
        } // 전투당 첫 성수 마개 회복 확정 종료
        return restored; // 실제 카드 체력 회복량 반환
    } // 실제 카드 회복 종료
    private int ApplyLandedFlatCardDamage(CardInstance card, BattleUnitRuntime target, int damage) // 명중한 아군 카드의 직접 적 피해 가산
    { // 직접 명중 유물 확인 시작
        var owner = card.OwnerUnit; // 이번 실제 카드 타격의 소유자
        if (owner.Team != BattleTeam.Ally || owner.IsDead || target == null || target.IsDead || target.Team != BattleTeam.Enemy) // 정확한 생존 아군과 적의 직접 명중 확인
        { // 명중 가산 제외 시작
            return damage; // 현재 타격 수치 유지
        } // 명중 가산 제외 종료
        var stealthBonus = owner.GetStatusEffect(BattleStatusEffectType.StealthBreakDamageUp); // 은신 종료로 예약된 실제 피해 상태
        int stealthDamage = stealthBonus is { IsExpired: false } && owner.RemoveStatusEffect(BattleStatusEffectType.StealthBreakDamageUp) ? stealthBonus.EffectiveValue : 0; // 첫 유효 직접 명중의 일회 피해 수치 소모
        var danceBonus = owner.GetStatusEffect(BattleStatusEffectType.DanceShoeDamageUp); // 실제 회피에서 대기 중인 공격 가산 상태
        int danceDamage = danceBonus is { IsExpired: false } && owner.RemoveStatusEffect(BattleStatusEffectType.DanceShoeDamageUp) ? danceBonus.EffectiveValue : 0; // 처음 명중한 직접 적 공격에만 무도화 상태 소모
        long preparedDamage = (long)stealthDamage + danceDamage + (card.CardType == CardType.Wand ? preparedWandDamage : 0); // 같은 명중의 무도화와 팔찌 및 첫 지팡이 집중 가산 합계
        if (damage <= 0) // 기존 양수 피해가 필요한 추가 유물 확인
        { // 영 피해 카드 타격의 예약 보정 시작
            int preparedHit = (int)Math.Min(int.MaxValue, damage + preparedDamage); // 적 명중에서 실제 예약된 피해 합계
            return turnRuntime.ApplyCrownNailOutgoingDamage(target, preparedHit); // 실제 보스 타격인 경우에만 방어 전 피해 증가
        } // 영 피해 카드 타격의 예약 보정 종료
        int firstBonus = usedFirstCardDamageBonus ? 0 : BattleSupplyRelics.Find(RelicEffectType.FirstCardDamageBonus)?.EffectValue ?? 0; // 전투당 첫 명중에 쓸 붉은 잉크병 수치
        if (firstBonus > 0) // 붉은 잉크병의 유효한 가산량 확인
        { // 붉은 잉크병 발동 시작
            usedFirstCardDamageBonus = true; // 방어 처리 전 전투당 한 차례 기회 기록
        } // 붉은 잉크병 발동 종료
        bool canAddRetainedDamage = turnRuntime.IsPlayerTurn && lastRetainedDamageRound != turnRuntime.CurrentRound && runtimeDeck.HasRetainedHandCardOtherThan(card); // 아군 턴과 사용 후 남는 유지 카드 및 턴 제한 확인
        int retainedBonus = canAddRetainedDamage ? BattleSupplyRelics.Find(RelicEffectType.RetainedHandDamage)?.EffectValue ?? 0 : 0; // 보유한 검은 깃털 펜던트 수치
        if (retainedBonus > 0) // 유지 피해의 유효한 가산량 확인
        { // 검은 깃털 펜던트 발동 시작
            lastRetainedDamageRound = turnRuntime.CurrentRound; // 같은 아군 턴의 다음 타격 중복 방지
        } // 검은 깃털 펜던트 발동 종료
        int landedDamage = (int)Math.Min(int.MaxValue, damage + preparedDamage + Math.Max(0, firstBonus) + Math.Max(0, retainedBonus)); // 방어 전 명중한 이 카드의 전체 직접 피해
        return turnRuntime.ApplyCrownNailOutgoingDamage(target, landedDamage); // 추가 고정 피해와 별개인 실제 보스 타격의 비율 증가
    } // 직접 명중 유물 확인 종료
    private int ApplyDirectHitRelics(CardInstance card, BattleUnitRuntime target, int damage) // 명중한 한 타격의 소유 유물 보정
    { // 실제 명중 보정 시작
        bool marked = target?.HasActiveCardDamageMark == true; // 실제 출혈 확인과 같은 카드 피해 표식 조건
        int chalk = marked ? RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.MarkedCardDamage)?.EffectValue ?? 0 : 0; // 두 표식이 겹쳐도 한 번만 가산
        int pin = marked && card.DamageType == BattleDamageType.Magical ? BattleSupplyRelics.Find(RelicEffectType.MarkedMagicalCardDamage)?.EffectValue ?? 0 : 0; // 기존 표식이 있는 적을 명중한 직접 마법 타격의 가산 피해
        int nail = marked ? BattleSupplyRelics.Find(RelicEffectType.MarkedHitDamageBonus)?.EffectValue ?? 0 : 0; // 표식 적에게 명중한 각 직접 타격의 은못 추가 피해
        int result = suitHitRelics.ApplyHitBonus((int)Math.Min(int.MaxValue, (long)damage + chalk + pin + nail)); // 기존 표식 유물과 은못 및 속성 명중 효과 합산
        result = supplyRelics.AddAttackDamage(card, target, result, turnRuntime.CurrentRound); // 심장핀과 탄약띠의 실제 명중 피해 가산
        if (card.OwnerUnit.CurrentMental > 30 || lastBlackRoseRound == turnRuntime.CurrentRound) // 공격자의 정신력과 파티 공용 턴 횟수 확인
        { // 추가 피해 없음 처리 시작
            return result; // 원래 공격 수치 반환
        } // 추가 피해 없음 처리 종료
        var relic = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.LowMentalCardDamage); // 실제로 가진 검은 장미 조회
        if (relic == null) // 미보유 상태 확인
        { // 미보유 처리 시작
            return result; // 이번 턴 발동 기회 보존
        } // 미보유 처리 종료
        lastBlackRoseRound = turnRuntime.CurrentRound; // 이번 아군 턴 한 타격만 발동
        return (int)Math.Min(int.MaxValue, (long)result + relic.EffectValue); // 명중한 직접 카드 피해에만 가산
    } // 실제 명중 보정 종료
    private void ApplyInspectionDraw() // 바보의 남은 카드 선택까지 끝난 뒤 연필 보너스 적용
    { // 추가 드로우 처리 시작
        int count = pendingInspectionDraw; // 성공한 확인에 예약된 지급량
        pendingInspectionDraw = 0; // 덱 변경 이벤트보다 먼저 중복 지급 차단
        if (count > 0) // 실제 예약된 추가 카드 확인
        { // 추가 카드 지급 시작
            runtimeDeck.DrawCards(count); // 기존 덱과 손패 규칙으로 추가 드로우
        } // 추가 카드 지급 종료
    } // 추가 드로우 처리 종료
    private int GetCardEffectDrawCount(int count) // 수량이 정해진 카드 드로우의 최초 보너스 계산
    { // 카드 드로우 수량 처리 시작
        if (count <= 0 || usedFirstCardDrawBonus) // 실제 양수 요청과 전투 최초 기회 확인
        { // 보너스 제외 시작
            return count; // 원래 드로우 수량 유지
        } // 보너스 제외 종료
        int bonus = BattleSupplyRelics.Find(RelicEffectType.FirstCardDrawBonus)?.EffectValue ?? 0; // 보유한 카드 드로우 증가 유물 확인
        if (bonus <= 0) // 증가할 실제 수량 확인
        { // 미보유 또는 효과 없음 처리 시작
            return count; // 기본 카드 드로우 수량 유지
        } // 미보유 또는 효과 없음 처리 종료
        usedFirstCardDrawBonus = true; // 드로우 처리 전 전투당 한 번 기회 기록
        return (int)Math.Min(int.MaxValue, (long)count + bonus); // 정수 범위 내 증가한 카드 드로우 수 반환
    } // 카드 드로우 수량 처리 종료
    private void QueuePentacleUseDraw(CardInstance card) // 실제 사용한 펜타클의 추가 드로우 예약
    { // 펜타클 사용 확인 시작
        if (card.CardType != CardType.Pentacle || !turnRuntime.IsPlayerTurn || turnRuntime.IsBattleEnded || lastPentacleDrawRound == turnRuntime.CurrentRound) // 유효한 아군 턴의 첫 펜타클 사용 확인
        { // 유물 발동 제외 시작
            return; // 관련 없는 카드와 턴에서 드로우 생략
        } // 유물 발동 제외 종료
        int count = BattleSupplyRelics.Find(RelicEffectType.PentacleDrawOnUse)?.EffectValue ?? 0; // 보유한 펜타클 사용 유물 드로우 수
        if (count <= 0) // 보유 여부와 양수 보너스 확인
        { // 드로우 불가 처리 시작
            return; // 실제 유물 효과 없음 처리
        } // 드로우 불가 처리 종료
        lastPentacleDrawRound = turnRuntime.CurrentRound; // 이번 아군 턴의 첫 펜타클 사용 기록
        pendingPentacleDraw = count; // 현재 카드 이동 뒤 지급할 드로우 보관
        if (pendingCardChoices == null || pendingCardChoices.Count == 0) // 덱이나 손패의 기존 선택 후보 여부 확인
        { // 선택 없는 카드 보너스 처리 시작
            ApplyPendingPentacleDraw(); // 손패를 떠난 카드의 드로우 즉시 지급
        } // 선택 없는 카드 보너스 처리 종료
    } // 펜타클 사용 확인 종료
    private void ApplyPendingPentacleDraw() // 보관한 펜타클 드로우 한 번 지급
    { // 드로우 예약 처리 시작
        int count = pendingPentacleDraw; // 이번 펜타클 사용으로 예약한 수량
        pendingPentacleDraw = 0; // 실제 드로우와 덱 변경 전에 중복 지급 차단
        if (count > 0 && turnRuntime.IsPlayerTurn && !turnRuntime.IsBattleEnded) // 유효한 현재 턴의 예약 확인
        { // 펜타클 드로우 실행 시작
            runtimeDeck.DrawCards(count); // 기존 덱과 손패 규칙을 따르는 유물 드로우
        } // 펜타클 드로우 실행 종료
    } // 드로우 예약 처리 종료
    private void QueueSkillRelicDraw(CardInstance card) // 이동을 마친 스킬의 독립 드로우 판정 예약
    { // 스킬 카드 드로우 확인 시작
        if ((card.SourceData.CardTags & CardTag.Skill) == 0 || BattleSupplyRelics.Find(RelicEffectType.ResolvedSkillDraw) == null) // 실제 스킬 태그와 보유 잉크병 확인
        { // 미발동 처리 시작
            return; // 일반 카드나 유물 미보유 상태 유지
        } // 미발동 처리 종료
        pendingSkillRelicCard = card; // 가능한 선택이 끝난 뒤 한 번 판정할 카드 보관
        if (!HasPendingEffectChoice) // 기존 카드 및 효과 선택을 기다릴지 확인
        { // 즉시 최종 정산 처리 시작
            ApplyPendingSkillRelicDraw(); // 일반 카드 이동이 해결된 스킬의 확률 드로우 처리
        } // 즉시 최종 정산 처리 종료
    } // 스킬 카드 드로우 확인 종료
    private void ApplyPendingSkillRelicDraw() // 마지막 카드 선택 뒤 한 번만 잉크병 드로우 판정
    { // 보관한 스킬 드로우 처리 시작
        var card = pendingSkillRelicCard; // 정산할 스킬 카드와 예약 보관
        pendingSkillRelicCard = null; // 덱 알림과 후속 신호보다 먼저 중복 판정 차단
        if (card == null || !turnRuntime.IsPlayerTurn || turnRuntime.IsBattleEnded) // 실제 이동을 확인했던 스킬의 유효한 아군 턴 확인
        { // 실제 정산 제외 시작
            return; // 수동 효과와 전투 종료 및 미이동 카드 제외
        } // 실제 정산 제외 종료
        var ink = BattleSupplyRelics.Find(RelicEffectType.ResolvedSkillDraw); // 정산 시점에 보유한 마른 잉크병 조회
        if (ink != null && ink.TriggerChancePercent > 0 && (ink.TriggerChancePercent >= 100 || UnityEngine.Random.Range(0, 100) < ink.TriggerChancePercent)) // 실제 원본 확률로 스킬 드로우 한 번 판정
        { // 잉크병 드로우 지급 시작
            runtimeDeck.DrawCards(ink.EffectValue); // 기존 펜타클과 독립인 실제 드로우 지급
        } // 잉크병 드로우 지급 종료
    } // 보관한 스킬 드로우 처리 종료
    internal bool TryDrawEventRelicCards(int count) // 유물 신호 드로우를 카드 이동과 최종 선택 뒤로 보호
    { // 공용 효과 드로우 접수 시작
        if (disposed || count <= 0 || turnRuntime.IsBattleEnded || turnRuntime.HasPendingBossPhaseTransition) // 전투 종료와 페이즈 전환 및 유효 수량 검사
        { // 처리 불가 상태 시작
            return false; // 종료된 전투의 손패 변경 제외
        } // 처리 불가 상태 종료
        if (actionPending || HasPendingEffectChoice || drawChoiceView != null || debuffChoiceView != null || remainingChoiceView != null) // 현재 카드 해결 및 화면의 선택 대기 확인
        { // 최종 선택 후 드로우 예약 시작
            pendingEventRelicDraw = (int)Math.Min(int.MaxValue, (long)pendingEventRelicDraw + count); // 서로 다른 성공 이벤트의 추가 드로우 합산
            return true; // 현재 손패나 후보를 건드리지 않고 확정된 알림 보관
        } // 최종 선택 후 드로우 예약 종료
        return runtimeDeck.DrawRelicCardsIntoHand(count) > 0; // 일반 전투 시점에는 손패 여유 내 즉시 드로우
    } // 공용 효과 드로우 접수 종료

    private void ApplyPostSelectionRelicDraws() // 모든 효과 선택을 마친 보호된 드로우 처리
    { // 독립 유물 드로우 정산 시작
        ApplyPendingSkillRelicDraw(); // 먼저 기존 스킬 드로우 처리
        ApplyPendingPentacleChanceDraw(); // 남은 손패 공간에서 펜촉의 독립 판정
        int count = pendingEventRelicDraw; // 선택 완료로 수동 드로우 후보를 건드리지 않는 예약 수 확보
        pendingEventRelicDraw = 0; // 덱 상태 변경 알림 전에 중복 지급 차단
        if (count > 0 && !turnRuntime.IsBattleEnded && !turnRuntime.HasPendingBossPhaseTransition) // 실제 전투가 계속되는 시점의 예약 확인
        { // 공용 신호 유물 드로우 정산 시작
            runtimeDeck.DrawRelicCardsIntoHand(count); // 기존 우선 드로우 뒤 남은 실제 손패 공간에만 지급
        } // 공용 신호 유물 드로우 정산 종료
        ApplyResolvedCardSuitRelics(); // 기존 추가 카드와 필수 선택 뒤 확정된 속성 효과 처리
    } // 독립 유물 드로우 정산 종료

    private void ApplyResolvedCardSuitRelics() // 실제 사용한 카드의 속성과 지팡이 공명 정산
    { // 카드 속성 정산 시작
        if (HasPendingEffectChoice || drawChoiceView != null || debuffChoiceView != null || remainingChoiceView != null) // 아직 남아 있는 필수 선택 확인
        { // 필수 선택 대기 시작
            return; // 다음 최종 선택으로 속성 정산 보류
        } // 필수 선택 대기 종료
        var card = pendingCardSuitRelics; // 아직 처리하지 않은 사용 카드 확보
        pendingCardSuitRelics = null; // 후속 카드와 피해 알림 전에 정산 예약 해제
        if (card == null || !turnRuntime.IsPlayerTurn || turnRuntime.IsBattleEnded || card.EffectType == CardEffectType.Unplayable) // 유효하게 끝난 아군 사용 확인
        { // 정산 불가 처리 시작
            return; // 미사용 카드와 종료된 전투의 유물 효과 제외
        } // 정산 불가 처리 종료
        bool sameTurn = pendingCardSuitRound == turnRuntime.CurrentRound && !turnRuntime.HasPendingBossPhaseTransition; // 보스 전환을 거치지 않은 원래 사용 턴 확인
        if (sameTurn) // 필수 선택과 실제 사용 끝까지 마친 같은 아군 턴 확인
        { // 복제 주문의 유효한 이번 턴 사용 내역 갱신 시작
            if (usedCardsRound != turnRuntime.CurrentRound) // 지난 아군 턴의 실물 사용 내역 확인
            { // 현재 턴 사용 내역 초기화 시작
                usedCardsRound = turnRuntime.CurrentRound; // 현재 실제 아군 턴을 함께 보관
                usedCardsThisTurn.Clear(); // 이전 턴에는 플레이했지만 이제 유효하지 않은 후보 정리
            } // 현재 턴 사용 내역 초기화 종료
            if (!usedCardsThisTurn.Contains(card)) // 같은 실물을 같은 턴에 다시 쓰더라도 한 후보만 제공
            { // 현재 실제 카드 등록 시작
                usedCardsThisTurn.Add(card); // 현재 턴의 사용 완료만 복제 선택 후보로 저장
            } // 현재 실제 카드 등록 종료
        } // 복제 주문의 유효한 이번 턴 사용 내역 갱신 종료
        if (sameTurn && card.CardType == CardType.Cup && card.OwnerUnit is { Team: BattleTeam.Ally, IsDead: false } cupOwner) // 필수 선택을 마친 성배의 실제 생존 사용자 확인
        { // 검은 예배포의 성배 정산 시작
            int block = BattleSupplyRelics.Find(RelicEffectType.ChapelClothChaliceBlock)?.EffectValue ?? 0; // 보유한 검은 예배포의 원본 방어도 조회
            cupOwner.GainBlock(block); // 다른 대상이 아닌 방금 사용을 마친 성배 소유자에게 지급
        } // 검은 예배포의 성배 정산 종료
        if (sameTurn && card.CardType is (CardType.Sword or CardType.Wand or CardType.Cup or CardType.Pentacle or CardType.Shield)) // 소유자와 관계없는 실제 전투 카드 속성 확인
        { // 아군 턴 속성 기록 시작
            if (resolvedSuitsRound != turnRuntime.CurrentRound) // 이전 아군 턴의 속성 기록 확인
            { // 새 턴 속성 기록 시작
                resolvedSuitsRound = turnRuntime.CurrentRound; // 현재 아군 턴의 라운드 기록
                resolvedSuits.Clear(); // 지난 턴에 정산한 카드 속성 초기화
            } // 새 턴 속성 기록 종료
            if (resolvedSuits.Add(card.CardType) && resolvedSuits.Count == 3) // 이번 턴에 세 번째 서로 다른 속성이 성립했는지 확인
            { // 역별 문장의 단회 드로우 시작
                int draw = BattleSupplyRelics.Find(RelicEffectType.DrawOnThreeCardSuits)?.EffectValue ?? 0; // 실제 보유한 문장의 보상량 조회
                runtimeDeck.DrawRelicCardsIntoHand(draw); // 손패 한도와 기존 드로우 순서를 지키는 보상
            } // 역별 문장의 단회 드로우 종료
        } // 아군 턴 속성 기록 종료
        if (card.CardType != CardType.Wand) // 실제로 정산한 카드의 지팡이 속성 확인
        { // 지팡이 이외의 속성 처리 시작
            return; // 다른 속성은 전투 공명 집계에서 제외
        } // 지팡이 이외의 속성 처리 종료
        resolvedWandsSincePulse++; // 이번 전투의 실제 지팡이 사용 한 번 누적
        if (resolvedWandsSincePulse < 3) // 직전 공명 뒤 실제 지팡이 세 번째 사용 확인
        { // 지팡이 공명 대기 시작
            return; // 전투의 다음 지팡이 사용까지 기록 유지
        } // 지팡이 공명 대기 종료
        resolvedWandsSincePulse = 0; // 이어지는 피해 알림 전에 이번 세 장의 공명 소모
        int damage = sameTurn ? BattleSupplyRelics.Find(RelicEffectType.WandResonanceDamage)?.EffectValue ?? 0 : 0; // 새 보스 페이즈를 건너뛰지 않는 공명 피해량
        if (damage <= 0) // 유효한 공명석 피해와 기존 페이즈 유지 확인
        { // 미발동 공명 처리 시작
            return; // 유물 미보유 또는 전환된 페이즈의 피해 제외
        } // 미발동 공명 처리 종료
        ApplyGeneralDamageToLivingEnemies(card, damage); // 마법 공명의 기존 일반 방어도 적용 광역 피해 정산
        bossPhaseRuntime?.CompletePendingTransition(); // 공명 피해가 새로 예약한 보스 전환도 같은 정산에서 완료
    } // 카드 속성 정산 종료
    private long ApplyGeneralDamageToLivingEnemies(CardInstance card, int damage) // 기획의 일반 피해를 공유하는 실제 생존 적의 단일 타격
    { // 생존 적 일반 피해 정산 시작
        long applied = 0; // 각 대상이 실제로 잃은 체력의 합계
        foreach (var enemy in enemyActions.EnemyUnits) // 실제 적용 시점의 고유 생존 적 명단 순회
        { // 상태를 확인한 개별 적 공격 시작
            if (turnRuntime.IsBattleEnded || turnRuntime.HasPendingBossPhaseTransition) // 실제 결과 확정 또는 공격 중 보스 전환 예약 확인
            { // 이후 적 공격 취소 시작
                break; // 전투나 이전 페이즈에 남은 일반 피해 생략
            } // 이후 적 공격 취소 종료
            if (enemy != null && !enemy.IsDead) // 아직 살아 있는 정식 적 대상 확인
            { // 일반 피해의 실제 방어도 처리 시작
                applied += enemy.TakeDamage(damage, BattleDamageType.None, card.OwnerUnit).AppliedDamage; // 일반 방어도와 피격 및 처치 신호를 그대로 거치는 피해
            } // 일반 피해의 실제 방어도 처리 종료
        } // 상태를 확인한 개별 적 공격 종료
        return applied; // 현재 전투에서 적에게 실제 준 체력 피해만 반환
    } // 생존 적 일반 피해 정산 종료
    private void ApplyPendingPentacleChanceDraw() // 실제 펜타클 사용으로 예약된 드로우 한 번 처리
    { // 펜촉 드로우 처리 시작
        bool pending = pendingPentacleChanceDraw; // 해당 정산의 펜촉 예약 보관
        pendingPentacleChanceDraw = false; // 덱이나 화면 알림 전에 같은 사용의 재판정 차단
        int handSpace = runtimeDeck.MaxHandSize - runtimeDeck.HandCount; // 모든 우선 드로우 뒤 남은 실제 손패 공간
        if (!pending || !turnRuntime.IsPlayerTurn || turnRuntime.IsBattleEnded || handSpace <= 0 || !runtimeDeck.HasDrawableCards) // 실제 카드 사용 및 공간과 드로우 후보 확인
        { // 펜촉 발동 제외 처리 시작
            return; // 후보 없는 추가 드로우의 무작위 판정 생략
        } // 펜촉 발동 제외 처리 종료
        var nib = BattleSupplyRelics.Find(RelicEffectType.PentacleResolvedChanceDraw); // 정산 시점의 실제 펜촉 보유 여부 조회
        if (nib?.EffectValue > 0 && nib.TriggerChancePercent > 0 && (nib.TriggerChancePercent >= 100 || UnityEngine.Random.Range(0, 100) < nib.TriggerChancePercent)) // 펜타클 한 장당 원본 확률 한 번 판정
        { // 펜촉의 추가 드로우 지급 시작
            runtimeDeck.DrawCards(Math.Min(handSpace, nib.EffectValue)); // 기존 덱과 손패 제한을 지키는 추가 카드 지급
        } // 펜촉의 추가 드로우 지급 종료
    } // 펜촉 드로우 처리 종료
    private void ApplySunChain(CardInstance card, BattleUnitRuntime primary) // 태양의 다른 적 연속 공격 처리
    { // 태양 연속 공격 시작
        BattleUnitRuntime target = SunChainAttack.TryAttack(card, primary, enemyActions.EnemyUnits, out bool missed, // 다른 적의 독립 명중 처리
            preparedSwordDamage: preparedSwordDamage, // 정비 보너스가 남아 있는 검 피해
            applyTargetBonus: (hit, damage) => ApplyDirectHitRelics(card, hit, damage), // 태양 연속 공격의 기존 명중 유물
            applyFlatTargetBonus: (hit, damage) => ApplyLandedFlatCardDamage(card, hit, damage)); // 치명타 뒤 한 번 제한의 직접 타격 가산
        if (target != null && missed) // 추가 공격 빗나감 확인
        { // 추가 공격 피드백 시작
            FindUnitView(target)?.ShowStatusFeedback("빗나감", false); // 추가 대상의 빗나감 표시
        } // 추가 공격 피드백 종료
    } // 태양 연속 공격 종료
    private void RemoveCardTargetStatus(BattleUnitRuntime target, BattleStatusEffectType type) // 카드가 지정한 한 종류 상태를 실제로 해제
    { // 카드 상태 해제 시작
        if (type != BattleStatusEffectType.None && target.RemoveStatusEffect(type)) // 원본 상태 존재와 실제 해제 성공 확인
        { // 해제 결과 표시 시작
            FindUnitView(target)?.ShowStatusFeedback(BattleStatusEffectInstance.GetDisplayName(type) + " 해제", false); // 기존 상태 해제 화면 피드백
        } // 해제 결과 표시 종료
    } // 카드 상태 해제 종료
    private static void LogCleanseResults(BattleUnitRuntime targetUnit, IReadOnlyList<BattleStatusEffectProcessResult> cleanseResults) // 정화 상세 결과 출력
    { // 정화 로그 시작
        foreach (BattleStatusEffectProcessResult cleanseResult in cleanseResults) // 정화 결과 목록 순회
        { // 개별 정화 로그 시작
            string effectName = BattleStatusEffectInstance.GetDisplayName(cleanseResult.EffectType); // 정화 상태 이름 조회
            Debug.Log($"[BattleStatus] 정화 / 대상 {targetUnit.DisplayName} / {effectName} / 중첩 {cleanseResult.StackCount} / 남은 횟수 {cleanseResult.PreviousRemainingTurns}"); // 정화 결과 출력
        } // 개별 정화 로그 종료
    } // 정화 로그 종료
    private static string GetEffectLabel(CardInstance cardInstance) // 카드 효과 로그 문구 조회
    { // 효과 문구 조회 시작
        if (cardInstance.SourceData.PentacleRule is PentacleCardRule.WheelOfFate or PentacleCardRule.Contract or PentacleCardRule.Ritual or PentacleCardRule.PickUpCoin) // 체력과 행동력 회복의 전투 로그 분류 확인
        { // 펜타클 전투 로그의 효과명 설정 시작
            return cardInstance.SourceData.PentacleRule == PentacleCardRule.WheelOfFate ? "회복" : "행동력 회복"; // 물리 피해와 구분되는 실제 지원 효과명
        } // 펜타클 전투 로그의 효과명 설정 종료
        if (cardInstance.TargetType == CardTargetType.EnemyAndAlly) // 두 진영에 효과를 주는 복합 카드 확인
        { // 복합 효과 이름 시작
            return "피해 및 회복"; // 실제 피해와 회복의 합계 로그 문구 반환
        } // 복합 효과 이름 종료
        if (cardInstance.EffectType == CardEffectType.CopyAllyBlessing) // 두 아군의 축복 이전 효과 확인
        { // 축복 이전 문구 시작
            return "강화 복사"; // 전투 로그의 실제 복사 단위 표시
        } // 축복 이전 문구 종료
        if (cardInstance.EffectType == CardEffectType.ReviveAlly) // 전투불능 아군의 부활 효과 확인
        { // 부활 문구 처리 시작
            return "부활"; // 실제 효과 로그 이름 반환
        } // 부활 문구 처리 종료
        if (cardInstance.EffectType == CardEffectType.DiscardWandForActionPoint) // 전투 로그의 지팡이 버림 행동력 효과 확인
        { // 마력 회수 문구 시작
            return "행동력 회복"; // 실제 회복한 공용 행동력의 로그 이름
        } // 마력 회수 문구 종료
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard) // 드로우 후 손패 정리 효과 확인
        { // 달 카드 효과 문구 시작
            return "드로우와 손패 선택 버리기"; // 달 전용 효과 이름 반환
        } // 달 카드 효과 문구 종료
        if (cardInstance.EffectType == CardEffectType.TransferAllyDebuff) // 동료 디버프 이전 효과 확인
        { // 디버프 이전 문구 시작
            return "동료 디버프 이전 후보"; // 디버프 이전 효과 이름 반환
        } // 디버프 이전 문구 종료
        if (cardInstance.EffectType == CardEffectType.RerollRandomAttack) // 공격 대상 재굴림 확인
        { // 재굴림 문구 시작
            return "무작위 공격 대상 재굴림"; // 재굴림 효과 이름 반환
        } // 재굴림 문구 종료
        if (cardInstance.EffectType == CardEffectType.ShareAllyAttack) // 피해 분담 효과 확인
        { // 피해 분담 문구 시작
            return "동료 공격 피해 분담"; // 피해 분담 효과 이름 반환
        } // 피해 분담 문구 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack) // 동료 보호 효과 확인
        { // 동료 보호 문구 시작
            return "동료 공격 대신 받기"; // 동료 보호 이름 반환
        } // 동료 보호 문구 종료
        if (cardInstance.EffectType == CardEffectType.ArrangeDrawTop) // 덱 순서 정리 효과 확인
        { // 덱 정리 문구 시작
            return "덱 순서 정리 후보"; // 덱 정리 효과 이름 반환
        } // 덱 정리 문구 종료
        if (cardInstance.EffectType == CardEffectType.SelectPartner) // 파트너 지정 문구 확인
        { // 파트너 문구 시작
            return "파트너 지정"; // 파트너 효과 이름 반환
        } // 파트너 문구 종료
        if (cardInstance.EffectType == CardEffectType.GainBlock) // 방어도 획득 문구 확인
        { // 방어도 문구 시작
            return "방어도"; // 방어도 효과 이름 반환
        } // 방어도 문구 종료
        if (cardInstance.EffectType == CardEffectType.RecoverDiscard) // 버린 카드 회수 문구 확인
        { // 카드 회수 문구 시작
            return "버린 카드 회수 후보"; // 전용 효과 이름 반환
        } // 카드 회수 문구 종료
        if (cardInstance.EffectType == CardEffectType.ChooseDraw) // 선택 드로우 문구 확인
        { // 선택 드로우 문구 시작
            return "선택 드로우 후보"; // 전용 효과 이름 반환
        } // 선택 드로우 문구 종료
        if (cardInstance.EffectType == CardEffectType.ProtectAttackTarget) // 공격 예고 보호 문구 확인
        { // 보호 문구 시작
            return "예고 대상 보호"; // 전용 효과 이름 반환
        } // 보호 문구 종료
        if (cardInstance.EffectType == CardEffectType.RedirectAttackAndDefend) // 공격 유도 문구 확인
        { // 공격 유도 문구 시작
            return "공격 유도와 방어"; // 전용 효과 이름 반환
        } // 공격 유도 문구 종료
        if (cardInstance.EffectType == CardEffectType.Heal) // 회복 효과 확인
        { // 회복 문구 처리 시작
            return "회복"; // 회복 문구 반환
        } // 회복 문구 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 문구 처리 시작
            return BattleStatusEffectInstance.GetDisplayName(cardInstance.StatusEffectType); // 상태 이상 이름 반환
        } // 상태 이상 문구 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) // 디버프 해제 효과 확인
        { // 디버프 해제 문구 처리 시작
            return "디버프 해제"; // 디버프 해제 문구 반환
        } // 디버프 해제 문구 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental) // 정신력 효과 확인
        { // 정신력 문구 처리 시작
            return "정신력"; // 정신력 문구 반환
        } // 정신력 문구 처리 종료
        return cardInstance.DamageType == BattleDamageType.Magical ? "마법 피해" : "물리 피해"; // 피해 종류 문구 반환
    } // 효과 문구 조회 종료
    private static List<BattleUnitRuntime> CreateSingleTargetList(BattleUnitRuntime targetUnit) // 단일 대상 목록 생성
    { // 단일 대상 생성 시작
        return new List<BattleUnitRuntime> { targetUnit }; // 단일 대상 목록 반환
    } // 단일 대상 생성 종료
    private bool ContainsHandCard(CardInstance cardInstance) // 손패 카드 포함 확인
    { // 손패 포함 검사 시작
        foreach (CardInstance handCard in runtimeDeck.Hand) // 현재 손패 순회
        { // 손패 카드 비교 시작
            if (handCard == cardInstance) // 동일 카드 확인
            { // 동일 카드 처리 시작
                return true; // 손패 포함 반환
            } // 동일 카드 처리 종료
        } // 손패 카드 비교 종료
        return false; // 손패 미포함 반환
    } // 손패 포함 검사 종료
    public void CancelSelection() // 카드 선택 취소
    { // 선택 취소 시작
        selectedCard = null; // 선택 카드 제거
        firstSequentialTarget = null; // 미확정 첫 대상 제거
        requiredSelectedEnemies = 0; // 취소하거나 사용한 카드의 대상 수 예약 초기화
        handView.SetMixedTargetStage(null); // 적 또는 아군의 선택 단계 안내 제거
        handView.SetSelectedCard(null); // 카드 선택 표시 해제
        ClearTargetHighlights(allyUnitViews); // 아군 대상 강조 해제
        ClearTargetHighlights(enemyUnitViews); // 적 대상 강조 해제
    } // 선택 취소 종료
    private static void ClearTargetHighlights(IReadOnlyList<BattleUnitView> unitViews) // 대상 강조 일괄 해제
    { // 강조 해제 시작
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 강조 해제 처리 시작
            if (unitView != null) // 유닛 화면 존재 확인
            { // 강조 해제 적용 시작
                unitView.SetTargetable(false); // 유닛 대상 강조 해제
            } // 강조 해제 적용 종료
        } // 강조 해제 처리 종료
    } // 강조 해제 종료
    private void RegisterUnitViewEvents(IReadOnlyList<BattleUnitView> unitViews) // 유닛 클릭 이벤트 등록
    { // 이벤트 등록 시작
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 이벤트 등록 처리 시작
            if (unitView != null) // 유닛 화면 존재 확인
            { // 유닛 화면 연결 시작
                unitView.Clicked += HandleUnitClicked; // 유닛 클릭 이벤트 등록
                if (unitView.RuntimeUnit != null) // 런타임 유닛 연결 확인
                { // 런타임 이벤트 등록 시작
                    unitView.RuntimeUnit.Died += HandleUnitDied; // 유닛 사망 이벤트 등록
                    unitView.RuntimeUnit.StatusEffectsChanged += HandleUnitHealthChanged; // 피의 추적 대상의 출혈 변경 감시 등록
                    unitView.RuntimeUnit.HealthChanged += HandleUnitHealthChanged; // 체력 비용과 처형 대상 갱신 이벤트 등록
                } // 런타임 이벤트 등록 종료
            } // 유닛 화면 연결 종료
        } // 이벤트 등록 처리 종료
    } // 이벤트 등록 종료
    private void UnregisterUnitViewEvents(IReadOnlyList<BattleUnitView> unitViews) // 유닛 클릭 이벤트 해제
    { // 이벤트 해제 시작
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 이벤트 해제 처리 시작
            if (unitView != null) // 유닛 화면 존재 확인
            { // 유닛 화면 연결 해제 시작
                unitView.Clicked -= HandleUnitClicked; // 유닛 클릭 이벤트 해제
                if (unitView.RuntimeUnit != null) // 런타임 유닛 연결 확인
                { // 런타임 이벤트 해제 시작
                    unitView.RuntimeUnit.Died -= HandleUnitDied; // 유닛 사망 이벤트 해제
                    unitView.RuntimeUnit.StatusEffectsChanged -= HandleUnitHealthChanged; // 피의 추적 대상의 출혈 변경 감시 해제
                    unitView.RuntimeUnit.HealthChanged -= HandleUnitHealthChanged; // 체력 비용과 처형 대상 갱신 이벤트 해제
                } // 런타임 이벤트 해제 종료
            } // 유닛 화면 연결 해제 종료
        } // 이벤트 해제 처리 종료
    } // 이벤트 해제 종료
    public void Dispose() // 카드 행동 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 연결 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 연결 해제 상태 저장
        loversPartner.Changed -= RefreshPartnerMarkers; // 파트너 표시 갱신 해제
        loversPartner.Dispose(); // 파트너 관리자 연결 해제
        chariotConcentration.Dispose(); // 전차 표식 관리자 연결 해제
        starRecoveryPassive.Dispose(); // 별 추가 예약 회복 관리자 정리
        worldCoordination.Dispose(); // 세계 연계 관리자 정리
        CloseCardChoice(); // 대기 선택 화면 정리
        if (handView != null) // 손패 화면 존재 확인
        { // 손패 이벤트 해제 시작
            handView.CardClicked -= HandleCardClicked; // 카드 클릭 이벤트 해제
            handView.SetMixedTargetStage(null); // 연결 해제 때 남은 복합 대상 안내 제거
            if (selectedCard?.TargetType is CardTargetType.BlessingSourceAndAlly or CardTargetType.SelectedTwoEnemies) // 연결 해제 때 남은 순차 카드 입력 확인
            { // 축복 이전 입력 해제 시작
                handView.SetSelectedCard(null); // 남은 첫 대상과 단계 안내 및 카드 선택 제거
            } // 축복 이전 입력 해제 종료
        } // 손패 이벤트 해제 종료
        runtimeDeck.StateChanged -= HandleDeckStateChanged; // 덱 상태 변경 이벤트 해제
        turnRuntime.StateChanged -= HandleTurnStateChanged; // 턴 상태 변경 이벤트 해제
        sharedActionPoints.StateChanged -= RecheckWheelPayment; // 이전 전투의 실제 가변 지불 후보 관찰 종료
        if (openingBookmarkView != null) // 씬과 함께 사라질 실제 개막 선택 화면 확인
        { // 관리 중인 모달 제거 시작
            openingBookmarkView.Close(); // 전투를 떠날 때 개막 선택 화면 숨김
            openingBookmarkView = null; // 이전 화면의 실제 참조 해제
        } // 관리 중인 모달 제거 종료
        UnregisterUnitViewEvents(allyUnitViews); // 아군 클릭 이벤트 해제
        UnregisterUnitViewEvents(enemyUnitViews); // 적 클릭 이벤트 해제
        selectedCard = null; // 선택 카드 참조 제거
        firstSequentialTarget = null; // 연결 해제 시 순차 카드의 첫 대상 제거
        requiredSelectedEnemies = 0; // 전투 입력을 닫을 때 남아 있던 대상 수 제거
        actionPending = false; // 카드 행동 진행 상태 해제
        ClearTargetHighlights(allyUnitViews); // 아군 대상 강조 해제
        ClearTargetHighlights(enemyUnitViews); // 적 대상 강조 해제
    } // 연결 해제 종료
} // 클래스 종료
