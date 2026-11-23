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
    private CardInstance pendingSkillRelicCard; // 카드 이동과 마지막 선택이 끝난 뒤 검사할 스킬 카드
    private bool usedFirstCardDrawBonus; // 이번 전투 최초 카드 드로우 증가 사용 기록
    private bool usedFirstHealingCard; // 파티가 이번 전투에 첫 회복 카드를 사용한 기록
    private int bandageBonus; // 현재 카드의 원래 회복 대상별 추가량
    private readonly FirstSuitHitRelics suitHitRelics = new FirstSuitHitRelics(); // 전투별 첫 검과 지팡이 명중 기록
    private readonly BattleSupplyRelics supplyRelics = new BattleSupplyRelics(); // 이번 전투의 심장핀과 구슬 발동 기록
    private int preparedSwordDamage; // 사용이 확정된 검 한 장의 모든 타격에 더할 피해
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
    private BattleUnitRuntime splitBarrierFirstTarget; // 분할 방벽에서 먼저 고른 아군
    private IReadOnlyList<CardInstance> pendingCardChoices; // 카드 선택 대기 후보
    private CardEffectType pendingChoiceEffect; // 대기 중인 카드 선택 효과
    private BattleDrawChoiceView drawChoiceView; // 현재 선택 드로우 화면
    private BattleRemainingCardChoiceView remainingChoiceView; // 남은 카드 처리 선택 화면
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
        handView.RefreshCardAvailability(); // 시작 카드 사용 가능 상태 갱신
    } // 생성자 종료
    private void HandleCardClicked(CardInstance cardInstance) // 카드 클릭 처리
    { // 카드 클릭 처리 시작
        if (!turnRuntime.IsPlayerTurn || actionPending || actionSequenceRunner.IsBusy) // 플레이어 턴과 행동 연출 여부 확인
        { // 플레이어 턴 아님 처리 시작
            return; // 카드 클릭 처리 중단
        } // 플레이어 턴 아님 처리 종료
        if (cardInstance == null || cardInstance.EffectType == CardEffectType.Unplayable || !ContainsHandCard(cardInstance)) // 사용할 수 있는 손패 카드 확인
        { // 잘못된 카드 처리 시작
            return; // 카드 클릭 처리 중단
        } // 잘못된 카드 처리 종료
        if (cardInstance.EffectType == CardEffectType.ReviveAlly && !turnRuntime.HasFallenAlly()) // 손패 선택 중 전투불능 대상 확인
        { // 부활 대상 없음 시작
            return; // 대상 선택 시작 차단
        } // 부활 대상 없음 종료
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
        if (selectedCard == cardInstance) // 동일 카드 재선택 확인
        { // 재선택 처리 시작
            CancelSelection(); // 카드 선택 취소
            return; // 카드 클릭 처리 종료
        } // 재선택 처리 종료
        splitBarrierFirstTarget = null; // 새 카드 선택 전 분할 방벽 대상 초기화
        selectedCard = cardInstance; // 새 선택 카드 저장
        handView.SetSelectedCard(selectedCard); // 손패 선택 표시 적용
        if (selectedCard.TargetType == CardTargetType.Self) // 자신 대상 카드 확인
        { // 자신 대상 처리 시작
            if (IsValidTarget(selectedCard, selectedCard.OwnerUnit)) // 자신 대상 유효성 확인
            { // 자신 대상 실행 시작
                ExecuteCard(selectedCard, CreateSingleTargetList(selectedCard.OwnerUnit)); // 소유자에게 카드 사용
            } // 자신 대상 실행 종료
            else // 자신 대상 실행 불가
            { // 자신 대상 실패 처리 시작
                CancelSelection(); // 카드 선택 상태 초기화
            } // 자신 대상 실패 처리 종료
            return; // 카드 클릭 처리 종료
        } // 자신 대상 처리 종료
        if (selectedCard.TargetType == CardTargetType.AllAllies) // 전체 아군 대상 확인
        { // 전체 아군 처리 시작
            ExecuteCard(selectedCard, CollectValidTargets(selectedCard, allyUnitViews)); // 전체 유효 아군에게 카드 사용
            return; // 카드 클릭 처리 종료
        } // 전체 아군 처리 종료
        if (selectedCard.TargetType == CardTargetType.AllEnemies) // 전체 적 대상 확인
        { // 전체 적 처리 시작
            ExecuteCard(selectedCard, CollectValidTargets(selectedCard, enemyUnitViews)); // 전체 유효 적에게 카드 사용
            return; // 카드 클릭 처리 종료
        } // 전체 적 처리 종료
        UpdateTargetHighlights(); // 첫 번째 대상 후보 강조
    } // 카드 클릭 처리 종료
    private void HandleUnitClicked(BattleUnitRuntime runtimeUnit) // 유닛 클릭 처리
    { // 유닛 클릭 처리 시작
        if (!turnRuntime.IsPlayerTurn || actionPending || actionSequenceRunner.IsBusy) // 플레이어 턴과 행동 연출 여부 확인
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
        if (selectedCard.SourceData.ShieldRule == ShieldCardRule.SplitBarrier) // 분할 방벽의 순차 대상 선택 확인
        { // 두 아군 대상 선택 시작
            if (runtimeUnit == splitBarrierFirstTarget) // 먼저 선택한 아군 반복 입력 확인
            { // 같은 대상 처리 시작
                return; // 행동력과 첫 번째 선택 보존
            } // 같은 대상 처리 종료
            if (splitBarrierFirstTarget == null) // 첫 번째 대상 선택 여부 확인
            { // 첫 번째 대상 보관 시작
                splitBarrierFirstTarget = runtimeUnit; // 최초 입력 아군 저장
                UpdateTargetHighlights(); // 먼저 선택한 아군을 제외한 후보 갱신
                return; // 두 번째 입력까지 카드 사용 보류
            } // 첫 번째 대상 보관 종료
            ExecuteCard(selectedCard, new[] { splitBarrierFirstTarget, runtimeUnit }); // 두 아군 대상으로 한 번만 카드 실행
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
        if (selectedCard != null && !ContainsHandCard(selectedCard)) // 선택 카드의 손패 이탈 확인
        { // 손패 이탈 처리 시작
            CancelSelection(); // 카드 선택 상태 초기화
        } // 손패 이탈 처리 종료
    } // 덱 상태 처리 종료
    private void HandleTurnStateChanged() // 턴 상태 변경 처리
    { // 턴 상태 처리 시작
        if (!turnRuntime.IsPlayerTurn) // 플레이어 턴 종료 확인
        { // 입력 잠금 처리 시작
            CancelSelection(); // 카드 선택 상태 초기화
            CloseCardChoice(); // 대기 선택 화면 정리
        } // 입력 잠금 처리 종료
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
    } // 턴 상태 처리 종료
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
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
        if (selectedCard == null) // 현재 대상 입력을 기다리는 카드 확인
        { // 미선택 처리 시작
            return; // 대상 선택 갱신 생략
        } // 미선택 처리 종료
        bool selectingSplitBarrier = selectedCard.SourceData.ShieldRule == ShieldCardRule.SplitBarrier; // 분할 방벽 선택 중인지 확인
        if (selectedCard.OwnerUnit == runtimeUnit || selectingSplitBarrier && // 소유자 사망과 분할 대상 조건 확인
            (splitBarrierFirstTarget == runtimeUnit || !turnRuntime.HasOtherLivingAlly(selectedCard.OwnerUnit))) // 최초 대상 사망 또는 생존자 부족 확인
        { // 카드 선택 취소 시작
            CancelSelection(); // 미확정 대상과 카드 선택 초기화
            return; // 대상 강조 재적용 생략
        } // 카드 선택 취소 종료
        if (selectingSplitBarrier && runtimeUnit.Team == BattleTeam.Ally) // 아직 선택하지 않은 아군 사망 확인
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
        if (selectedCard?.TargetType == CardTargetType.FallenAlly || selectedCard?.SourceData.SwordRule == SwordCardRule.Finisher || selectedCard?.SourceData.SwordRule == SwordCardRule.ExecutionSlash || selectedCard?.SourceData.SwordRule == SwordCardRule.BloodTrail || selectedCard?.SourceData.SwordRule == SwordCardRule.JudgmentSlash) // 체력과 사망에 따라 바뀌는 카드 대상 확인
        { // 대상 범위 갱신 시작
            UpdateTargetHighlights(); // 현재 체력에 맞춰 선택 가능한 적 표시
        } // 대상 범위 갱신 종료
        handView.RefreshCardAvailability(); // 변경된 체력 기준 손패 사용 가능 상태 갱신
    } // 아군 체력 변경 처리 종료
    private void ExecuteCard(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits) // 카드 효과 실행
    { // 카드 실행 시작
        if (!turnRuntime.IsPlayerTurn || actionPending || actionSequenceRunner.IsBusy) // 플레이어 턴과 연출 상태 확인
        { // 실행 불가 처리 시작
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 실행 불가 처리 종료
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
        if (cardInstance.SourceData.ShieldRule == ShieldCardRule.SplitBarrier && // 분할 방벽의 비용 지불 직전 검사
            (cardInstance.OwnerUnit.IsDead || targetUnits.Count != 2 || targetUnits[0] == targetUnits[1] || // 소유자 생존과 서로 다른 대상 수 확인
            !IsValidTarget(cardInstance, targetUnits[0]) || !IsValidTarget(cardInstance, targetUnits[1]))) // 두 대상의 진영과 생존 상태 확인
        { // 두 대상 미확정 처리 시작
            CancelSelection(); // 비용 차감 전 카드와 대상 선택 취소
            return; // 분할 방벽 효과 실행 중단
        } // 두 대상 미확정 처리 종료
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
        int cost = cardInstance.SourceData.SwordRule == SwordCardRule.Finisher ? sharedActionPoints.CurrentActionPoints : cardInstance.ApCost; // 실제 사용 순간의 행동력 비용 결정
        if (!SwordCardCombat.CanUse(cardInstance, sharedActionPoints, turnRuntime) || !sharedActionPoints.Spend(cost)) // 현재 대상 및 비용 재확인 후 한 번 지불
        { // 비용 차감 실패 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 카드 비용 차감에 실패했습니다: {cardInstance.DisplayName}"); // 비용 차감 실패 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 비용 차감 실패 처리 종료
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
        paidActionPoints = cost; // 충돌 시점에 사용할 실제 지불 비용 보관
        if (cardInstance.SourceData.SwordRule == SwordCardRule.DoomBlade || cardInstance.SourceData.SwordRule == SwordCardRule.BloodOath) // 생존 체력을 지불하는 검 카드 확인
        { // 체력 대가 지불 시작
            cardInstance.OwnerUnit.TrySpendHealth(cardInstance.SourceData.SecondaryEffectValue); // 방어도를 건너뛰고 생존 가능한 체력 대가 지불
            actorView.ShowStatusFeedback($"체력 -{cardInstance.SourceData.SecondaryEffectValue}", true); // 소유자에게 체력 소모 표시
        } // 체력 대가 지불 종료
        List<BattleUnitRuntime> targetSnapshot = new List<BattleUnitRuntime>(targetUnits); // 연출 중 유지할 대상 목록 복사
        actionPending = true; // 카드 행동 진행 상태 저장
        CancelSelection(); // 카드 선택과 대상 강조 해제
        turnRuntime.RecordCardPayment(cardInstance, cost); // 실제 카드 사용의 행동력으로 스프링 예약 정산
        if (cardInstance.ShieldBalanceDiscount > 0) // 실제로 사용한 유료 방패의 균형 할인 확인
        { // 사용이 확정된 방패 할인 제거 시작
            cardInstance.OwnerUnit.RemoveStatusEffect(BattleStatusEffectType.ShieldBalance); // 시전자의 예약 할인 한 번 소모
        } // 사용이 확정된 방패 할인 제거 종료
        Action impactAction = () => ResolveCardImpact(cardInstance, targetSnapshot); // 충돌 시 카드 효과 처리 생성
        bool started = actionSequenceRunner.TryStartPlayerAction(actorView, targetViews, cardInstance.EffectType, cardInstance.StatusEffectType, impactAction, HandleActionSequenceCompleted); // 카드 행동 연출 시작
        if (started) // 연출 시작 결과 확인
        { // 연출 시작 성공 처리 시작
            return; // 충돌 시점까지 카드 실행 대기
        } // 연출 시작 성공 처리 종료
        actionPending = false; // 카드 행동 진행 상태 해제
        Debug.LogWarning($"[BattleCardActionController] 카드 행동 연출 시작 실패로 효과를 즉시 적용합니다: {cardInstance.DisplayName}"); // 즉시 적용 안내 출력
        impactAction.Invoke(); // 카드 효과 즉시 적용
        HandleActionSequenceCompleted(); // 즉시 적용 후 선택 화면과 상태 갱신
    } // 카드 실행 종료
    private void ResolveCardImpact(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits) // 손패 사용이 끝날 때까지 처치 드로우 보류
    { // 카드 정산 범위 시작
        turnRuntime.BeginCardResolution(); // 손패에 아직 사용 카드가 남았음을 기록
        try // 카드 효과와 이동 완료까지 정산 범위 유지
        { // 카드 충돌 처리 시작
            ApplyCardImpact(cardInstance, targetUnits); // 실제 전투 효과와 사용 카드 이동 처리
        } // 카드 충돌 처리 종료
        finally // 충돌 처리 종료에 예약 드로우 정리 보장
        { // 카드 정산 범위 정리 시작
            turnRuntime.EndCardResolution(); // 비워진 손패 자리에 처치 보상 지급
        } // 카드 정산 범위 정리 종료
    } // 카드 정산 범위 종료

    private void ApplyCardImpact(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits) // 카드 충돌 시 효과 적용
    { // 카드 충돌 처리 시작
        bandageBonus = 0; // 직전 카드의 추가 회복 해제
        if (!usedFirstHealingCard && (cardInstance.EffectType == CardEffectType.Heal || cardInstance.SourceData.FollowUpEffect == CardFollowUpEffect.HealAllAllies)) // 첫 직접 회복 또는 명시적 후속 회복 카드 사용 확인
        { // 첫 회복 카드 처리 시작
            usedFirstHealingCard = true; // 대상이 최대 체력이어도 첫 사용 기록
            bandageBonus = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.FirstHealingCardBonus)?.EffectValue ?? 0; // 실제 보유한 붕대의 대상별 가산
        } // 첫 회복 카드 처리 종료
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
        int totalAppliedAmount = 0; // 전체 실제 적용량 초기화
        bool defensiveCard = HasDefensivePrimaryEffect(cardInstance); // 방어 성격인 기본 카드 효과 확인
        bool appliedDefense = false; // 살아 있는 아군에게 실제 방어 효과를 주었는지 기록
        if (cardInstance.EffectType == CardEffectType.GainBlock && cardInstance.TargetType == CardTargetType.AllAllies && !cardInstance.SourceData.GrantsBlockToEachAlly && cardInstance.SourceData.ShieldRule != ShieldCardRule.FinalRampart) // 공용 방어도만 생존 아군에게 분배
        { // 효과 적용 시작
            totalAppliedAmount = BattleBlockDistribution.Apply(targetUnits, cardInstance.EffectValue); // 고정 방어도를 생존 아군에게 배분
            appliedDefense = totalAppliedAmount > 0; // 실제 공용 방어도 분배 성공 확인
        } // 효과 적용 종료
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
        starRecoveryPassive.TryApply(cardInstance, targetUnits); // 별의 회복 카드 사용 시 추가 회복 예약
        bool discarded = ApplyFollowUpAndDiscard(cardInstance, targetUnits, out bool followUpDefense); // 후속 효과와 실제 방어 및 사용 카드 이동 확인
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
            if (cardInstance.SourceData.ShieldRule == ShieldCardRule.GiantsShield) // 정산을 끝낸 거인의 방패 확인
            { // 다음 턴 행동력 감소 예약 시작
                turnRuntime.QueueNextPlayerTurnActionPointLoss(cardInstance.SourceData.SecondaryEffectValue); // 다음 아군 턴 한 번의 공용 행동력 감소 예약
            } // 다음 턴 행동력 감소 예약 종료
            bool movedFromHand = !ContainsHandCard(cardInstance); // 기존 보너스로 다시 뽑기 전 실제 손패 이탈 확인
            QueuePentacleUseDraw(cardInstance); // 카드 사용 위치 정산 뒤 펜타클 보너스 확인
            if (movedFromHand) // 독립 보너스 지급 전 확인한 실제 사용 정산
            { // 스킬 드로우 예약 시작
                QueueSkillRelicDraw(cardInstance); // 기존 카드 이동을 마친 스킬의 잉크병 판정 예약
            } // 스킬 드로우 예약 종료
            if ((appliedDefense || followUpDefense) && turnRuntime.IsPlayerTurn && !cardInstance.OwnerUnit.IsDead) // 완료된 방어 카드와 생존한 원래 소유자 확인
            { // 방어 카드 장갑 처리 시작
                int block = BattleSupplyRelics.Find(RelicEffectType.ResolvedDefenseBlock)?.EffectValue ?? 0; // 장갑이 주는 소유자 전용 추가 방어도
                cardInstance.OwnerUnit.GainBlock(block); // 여러 대상이어도 카드 소유자에게 한 번만 지급
            } // 방어 카드 장갑 처리 종료
        } // 사용 후 드로우 유물 처리 종료
        string effectLabel = GetEffectLabel(cardInstance); // 카드 효과 로그 문구 생성
        Debug.Log($"[BattleCardActionController] 카드 사용 완료 - {cardInstance.DisplayName} / 대상 {targetUnits.Count}명 / {effectLabel} 적용량 {totalAppliedAmount} / 남은 공용 AP {sharedActionPoints.CurrentActionPoints}"); // 카드 사용 결과 출력
    } // 카드 충돌 처리 종료
    private static bool HasDefensivePrimaryEffect(CardInstance card) // 실제 적용량을 방어 효과로 셀 수 있는 카드 확인
    { // 카드 방어 성격 확인 시작
        return card.EffectType == CardEffectType.GainBlock || card.EffectType == CardEffectType.GuardAllyAttack || // 실제 방어도 또는 동료 직접 보호 효과
            card.EffectType == CardEffectType.ApplyStatusEffect && card.CardType == CardType.Shield && card.StatusEffectType != BattleStatusEffectType.None && !BattleStatusEffectInstance.IsDebuffType(card.StatusEffectType); // 아군에게 적용한 양의 방패 상태
    } // 카드 방어 성격 확인 종료
    private bool ApplyFollowUpAndDiscard(CardInstance card, IReadOnlyList<BattleUnitRuntime> targets, out bool appliedDefense) // 보조 방어 처리 결과를 포함한 카드 이동
    { // 후속 효과 처리 시작
        appliedDefense = false; // 명시적 후속 방어도의 성공 여부 초기화
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
            ApplyPendingSkillRelicDraw(); // 선택 없이 최종 정산된 스킬 카드의 예약 처리
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
        } // 선택 실패 처리 종료
        pendingDebuffChoices = null; // 디버프 후보 정리
        pendingDebuffSource = null; // 원래 동료 참조 정리
        pendingDebuffRecipient = null; // 디버프 이전 대상 참조 정리
        debuffChoiceView = null; // 닫힌 선택 화면 참조 정리
        ApplyPendingSkillRelicDraw(); // 디버프 이전 선택까지 마친 스킬 드로우 처리
        handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
    } // 선택 디버프 처리 종료
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
        ApplyPendingSkillRelicDraw(); // 일반 카드 선택까지 완료한 스킬의 드로우 판정
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
        ApplyPendingSkillRelicDraw(); // 남은 카드에 대한 마지막 선택까지 완료한 스킬 확인
        handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
    } // 남은 카드 처리 종료
    private void CloseCardChoice() // 대기 선택 화면 정리
    { // 선택 화면 정리 시작
        pendingInspectionDraw = 0; // 전투 종료나 선택 강제 해제 뒤 남은 드로우 예약 정리
        pendingPentacleDraw = 0; // 전투 종료나 선택 강제 해제 뒤 펜타클 예약 정리
        pendingSkillRelicCard = null; // 종료나 강제 취소된 선택의 잉크병 예약 정리
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
    private bool IsValidTarget(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 대상 유효성 검사
    { // 대상 검사 시작
        if (cardInstance == null || targetUnit == null) // 카드와 대상 연결 확인
        { // 선택 불가 처리 시작
            return false; // 잘못된 대상 반환
        } // 선택 불가 처리 종료
        if (cardInstance.TargetType == CardTargetType.FallenAlly || cardInstance.EffectType == CardEffectType.ReviveAlly) // 전투불능 아군 전용 카드 조합 확인
        { // 부활 대상 검사 시작
            return cardInstance.TargetType == CardTargetType.FallenAlly && cardInstance.EffectType == CardEffectType.ReviveAlly && turnRuntime.CanReviveFallenAlly(targetUnit); // 정확한 부활 효과와 현재 명단 검사 결과 반환
        } // 부활 대상 검사 종료
        if (targetUnit.IsDead) // 일반 카드 대상의 생존 여부 확인
        { // 일반 카드 전투불능 대상 차단 시작
            return false; // 일반 카드 대상으로 선택 불가
        } // 일반 카드 전투불능 대상 차단 종료
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
        if ((cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) && !targetUnit.HasRemovableDebuff) // 실제 정화 가능한 디버프 확인
        { // 정화 불필요 처리 시작
            return false; // 정화 대상 불가 반환
        } // 정화 불필요 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect && cardInstance.StatusEffectType == BattleStatusEffectType.None) // 상태 이상 카드 설정 확인
        { // 상태 설정 오류 처리 시작
            return false; // 상태 이상 대상 불가 반환
        } // 상태 설정 오류 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental && !targetUnit.CanChangeMental(cardInstance.MentalChangeValue) && !(cardInstance.SourceData.FollowUpEffect == CardFollowUpEffect.DrawCards && runtimeDeck.HasDrawableCards)) // 정신력 변화 또는 추가 드로우 가능 여부 확인
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
            case CardTargetType.SingleAlly: // 단일 아군 대상 종류
            case CardTargetType.AllAllies: // 전체 아군 대상 종류
                return targetUnit.Team == BattleTeam.Ally; // 아군 여부 반환
            case CardTargetType.SingleEnemy: // 단일 적 대상 종류
            case CardTargetType.AllEnemies: // 전체 적 대상 종류
                return targetUnit.Team == BattleTeam.Enemy; // 적 여부 반환
            default: // 알 수 없는 대상 종류
                return false; // 잘못된 대상 반환
        } // 대상 분기 종료
    } // 대상 검사 종료
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
            bool targetable = unitView != null && unitView.RuntimeUnit != splitBarrierFirstTarget && IsValidTarget(selectedCard, unitView.RuntimeUnit); // 아직 고르지 않은 유효 대상 확인
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
        return effectType == CardEffectType.Damage || effectType == CardEffectType.Heal || effectType == CardEffectType.ReviveAlly || effectType == CardEffectType.ApplyStatusEffect || effectType == CardEffectType.RemoveDebuffs || effectType == CardEffectType.RemoveOneDebuff || effectType == CardEffectType.ChangeMental || effectType == CardEffectType.RedirectAttackAndDefend || effectType == CardEffectType.ProtectAttackTarget || effectType == CardEffectType.ChooseDraw || effectType == CardEffectType.RecoverDiscard || effectType == CardEffectType.GainBlock || effectType == CardEffectType.SelectPartner || effectType == CardEffectType.GuardAllyAttack || effectType == CardEffectType.ArrangeDrawTop || effectType == CardEffectType.RerollRandomAttack || effectType == CardEffectType.ShareAllyAttack || effectType == CardEffectType.TransferAllyDebuff || effectType == CardEffectType.DrawThenDiscard; // 지원 카드 효과 결과 반환
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
    private int ApplyCardEffect(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 효과 단일 대상 적용
    { // 단일 효과 적용 시작
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
            BattleStatusEffectType removed = cardInstance.SourceData.StatusRemovedByHealing; // 회복 전에 제거할 상태 조회
            RemoveCardTargetStatus(targetUnit, removed); // 응급 처치와 기존 카드 원본이 지정한 상태 해제
            int baseHealing = cardInstance.SourceData.CardId == "CRD_ARC_20_RESCUE" ? JudgementRecovery.GetHealingAmount(targetUnit, cardInstance.EffectValue, cardInstance.SourceData.SecondaryEffectValue) : cardInstance.EffectValue; // 심판의 위험 구간 추가 회복 계산
            int modifiedHealing = GetDirectCardHealing(cardInstance, baseHealing); // 정신 상태 포함 회복량 계산
            int restored = RestoreDirectCardHealth(cardInstance, targetUnit, modifiedHealing); // 기존 회복 유물과 실제 체력 상한 반영
            if (cardInstance.SourceData.CupRule == CupCardRule.CelestialGrace) // 최대 체력과 무관한 은총의 긍정 강화 확인
            { // 이번 턴 아군 공격력 처리 시작
                var result = targetUnit.ApplyCurrentAllyTurnAttackPower(cardInstance.SourceData.SecondaryEffectValue, cardInstance.OwnerUnit); // 장기 공격력과 독립인 실제 이번 턴 강화
                FindUnitView(targetUnit)?.ShowStatusApplyFeedback(BattleStatusEffectType.AttackPowerUp, result); // 적용된 공격력 강화 표시
            } // 이번 턴 아군 공격력 처리 종료
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
                return cardInstance.EffectValue; // 실제 결투 피해 가산량 반환
            } // 시전자 표식 표시 종료
            return 0; // 면역 또는 불가 시 효과 미적용
        } // 결투 표식 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 처리 시작
            BattleStatusEffectApplyResult applyResult = targetUnit.ApplyStatusEffect(cardInstance.StatusEffectType, cardInstance.EffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit); // 대상 상태 이상 적용
            if (cardInstance.StatusEffectType == BattleStatusEffectType.SwordSaintStance && (applyResult == BattleStatusEffectApplyResult.Applied || applyResult == BattleStatusEffectApplyResult.Stacked)) // 시전 턴을 포함하는 검성의 자세 적용 확인
            { // 시전 턴부터 차감 시작
                targetUnit.GetStatusEffect(BattleStatusEffectType.SwordSaintStance).ConsumeDurationDecreaseSkip(); // 이번 시전 턴도 지속 시간에 포함
            } // 시전 턴부터 차감 종료
            BattleUnitView targetView = FindUnitView(targetUnit); // 대상 유닛 화면 조회
            targetView?.ShowStatusApplyFeedback(cardInstance.StatusEffectType, applyResult); // 상태 이상 적용 결과 표시
            return applyResult == BattleStatusEffectApplyResult.Applied || applyResult == BattleStatusEffectApplyResult.Stacked ? cardInstance.EffectValue : 0; // 상태 이상 적용 수치 반환
        } // 상태 이상 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) // 디버프 해제 효과 확인
        { // 디버프 해제 처리 시작
            int maximumCount = cardInstance.EffectType == CardEffectType.RemoveOneDebuff ? 1 : int.MaxValue; // 카드별 정화 개수 결정
            IReadOnlyList<BattleStatusEffectProcessResult> cleanseResults = statusEffectProcessor.CleanseDebuffs(targetUnit, maximumCount); // 지정 개수의 대상 디버프 정화
            int removedCount = cleanseResults.Count; // 제거된 디버프 수 계산
            BattleUnitView targetView = FindUnitView(targetUnit); // 대상 유닛 화면 조회
            targetView?.ShowStatusFeedback(removedCount > 0 ? $"정화 {removedCount}" : "정화 실패", false); // 정화 결과 플로팅 문구 표시
            LogCleanseResults(targetUnit, cleanseResults); // 정화 상세 결과 출력
            return removedCount; // 제거된 디버프 수 반환
        } // 디버프 해제 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental) // 정신력 직접 효과 확인
        { // 정신력 효과 처리 시작
            BattleMentalChangeResult mentalResult = targetUnit.ChangeMental(cardInstance.MentalChangeValue, BattleMentalChangeReason.CardEffect); // 대상 정신력 직접 변경
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

    private int ApplyDanceHits(CardInstance card, IReadOnlyList<BattleUnitRuntime> area) // 선택 범위 안에서 독립 타격 처리
    { // 무작위 연타 시작
        int damage = 0; // 이번 카드가 입힌 체력 피해 합계
        var hit = new BattleUnitRuntime[1]; // 타격마다 사용할 약점 판정 대상
        try // 조기 종료 시 피해 문맥 정리 보장
        { // 각 타격 처리 시작
            for (int index = 0; index < card.SourceData.DamageHitCount && !card.OwnerUnit.IsDead && !turnRuntime.IsBattleEnded && !turnRuntime.HasPendingBossPhaseTransition; index++) // 남은 타격과 전투 상태 검사
            { // 생존 대상 추첨 시작
                hit[0] = SwordCardCombat.PickDanceTarget(area); // 같은 두 칸 안에서 생존자 재선택
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

    private int ApplyIsolatedDamageHit(CardInstance card, BattleUnitRuntime target, int? additionalBase = null) // 후속 타격의 개별 명중과 약점 적용
    { // 독립 피해 문맥 시작
        BattleCardDamageContext.Begin(card, new[] { target }); // 다시 고른 대상의 약점 문맥 준비
        try // 후속 타격 뒤 문맥 정리 보장
        { // 독립 후속 타격 시작
            return ApplyDamageHit(card, target, additionalBase); // 명중과 방어 및 약점을 적용한 후속 타격
        } // 독립 후속 타격 종료
        finally // 추가 공격 정리
        { // 약점 문맥 정리 시작
            BattleCardDamageContext.Clear(); // 다음 카드로 넘어가는 약점 문맥 해제
        } // 약점 문맥 정리 종료
    } // 추가 공격 종료

    private int ApplyDamageHit(CardInstance cardInstance, BattleUnitRuntime targetUnit, int? additionalBase = null) // 카드 공격 한 타격 적용
    { // 타격 판정 시작
        if (!BattleHitChance.Roll(cardInstance.OwnerUnit, targetUnit)) // 실명 상태의 공격 명중 확인
        { // 카드 공격 빗나감 처리 시작
            BattleCardDamageContext.TryConsume(out _, out _); // 빗나간 대상의 카드 피해 문맥 소비
            FindUnitView(targetUnit)?.ShowStatusFeedback("빗나감", false); // 선택 대상의 빗나감 표시
            if (SunChainAttack.IsChainCard(cardInstance)) // 태양의 독립 연속 공격 확인
            { // 다음 공격 판정 시작
                ApplySunChain(cardInstance, targetUnit); // 첫 공격과 독립적인 두 번째 공격 실행
            } // 다음 공격 판정 종료
            return 0; // 첫 공격 피해 없음 반환
        } // 카드 공격 빗나감 처리 종료
        int? baseOverride = additionalBase; // 명시적인 추가 타격의 피해 수치 우선 사용
        if (!baseOverride.HasValue && cardInstance.SourceData.ShieldRule == ShieldCardRule.RendingShield) // 현재 방어도를 기본 피해로 쓰는 파열 방패 확인
        { // 방어도 기반 기본 피해 처리 시작
            baseOverride = cardInstance.OwnerUnit.CurrentBlock / 2; // 현재 방어도의 절반을 정수로 내린 기본 피해
        } // 방어도 기반 기본 피해 처리 종료
        if (!baseOverride.HasValue && cardInstance.SourceData.HasRandomDamage) // 실제로 두 피해 중 하나를 고르는 원본 확인
        { // 무작위 피해 굴림 시작
            baseOverride = BattleSupplyRelics.RollDamage(cardInstance); // 흑철 구슬과 강화가 반영된 정수 피해 선택
        } // 무작위 피해 굴림 종료
        long printedDamage = baseOverride.HasValue ? (long)baseOverride.Value + cardInstance.OwnerUnit.AttackPowerBonus : SwordCardCombat.BaseDamage(cardInstance, paidActionPoints, targetUnit); // 가변 또는 고정 카드의 공격력 합산
        int baseDamage = (int)Math.Min(int.MaxValue, printedDamage + preparedSwordDamage); // 이번 타격의 원본 피해에 정비한 검 피해 가산
        if (cardInstance.SourceData.CardId == "CRD_ARC_13_FINISH" && targetUnit.HasDebuff) // 죽음 전용 카드와 약화된 적 확인
        { // 약화 대상 추가 피해 시작
            baseDamage = (int)Math.Min(int.MaxValue, (long)baseDamage * 3 / 2); // 임시 추가 피해 오십 퍼센트 적용
        } // 약화 대상 추가 피해 종료
        baseDamage = SwordCardCombat.ApplyAttackStances(cardInstance, targetUnit, baseDamage); // 타격 순간의 검 강화와 마무리 체력 조건 적용
        int modifiedDamage = cardInstance.OwnerUnit.ModifyOutgoingDamage(baseDamage); // 정신 상태 포함 최종 원본 피해 계산
        modifiedDamage = SwordCardCombat.RollCritical(cardInstance, modifiedDamage, out bool critical); // 명중한 카드 타격에 치명타 확률 적용
        if (critical) // 치명타 발동 확인
        { // 치명타 표시 시작
            FindUnitView(targetUnit)?.ShowStatusFeedback("치명타", true); // 피격 대상에 치명타 표시
        } // 치명타 표시 종료
        modifiedDamage = ApplyDirectHitRelics(cardInstance, targetUnit, modifiedDamage); // 방어 판정 전 첫 직접 명중에만 가산
        modifiedDamage = ApplyFirstAndRetainedDamage(cardInstance, targetUnit, modifiedDamage); // 치명타 직후 첫 피해와 유지 피해 가산
        int bypass = cardInstance.SourceData.SwordRule == SwordCardRule.PiercingLunge ? cardInstance.SourceData.SecondaryEffectValue : 0; // 파고들기의 타격 한정 방어도 우회 비율
        int healthBefore = targetUnit.CurrentHealth; // 카드 타격 직전 대상 체력 보관
        if (cardInstance.SourceData.CupRule == CupCardRule.HolyJudgment) // 실제 명중한 심판의 기존 불사 해제 확인
        { // 명중 대상의 생존 보호 제거 시작
            RemoveCardTargetStatus(targetUnit, BattleStatusEffectType.UndyingGuard); // 피해 직전에 불굴의 일회 생존 보호 제거
            RemoveCardTargetStatus(targetUnit, BattleStatusEffectType.DefeatWard); // 일반 피해 무효와 별개인 전투불능 방지 제거
        } // 명중 대상의 생존 보호 제거 종료
        BattleDamageResult damageResult = targetUnit.TakeDamage(modifiedDamage, cardInstance.DamageType, cardInstance.OwnerUnit, bypassBlockPercent: bypass); // 실제로 흡수한 방어도와 체력 피해 적용
        if (cardInstance.SourceData.SwordRule == SwordCardRule.ExecutionerSword && healthBefore > 0 && damageResult.AppliedDamage == healthBefore && targetUnit.IsDead) // 이 카드의 직접 체력 피해로 적 처치 확인
        { // 처치 환급 시작
            sharedActionPoints.Gain(cardInstance.SourceData.SecondaryEffectValue); // 공용 행동력 최대치 안에서 환급
        } // 처치 환급 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.JudgmentSlash && !targetUnit.IsDead) // 명중 후 생존 대상의 강화 하나 해제
        { // 단죄 강화 해제 시작
            var buff = targetUnit.FirstRemovableBuff(); // 타격 직후 가장 먼저 부여된 일반 강화 조회
            if (buff != null && targetUnit.RemoveStatusEffect(buff.EffectType)) // 보호 정신 상태를 제외한 실제 해제 확인
            { // 해제 피드백 시작
                FindUnitView(targetUnit)?.ShowStatusFeedback(buff.DisplayName + " 해제", false); // 해제된 강화 이름 표시
            } // 해제 피드백 종료
        } // 단죄 강화 해제 종료
        if (!targetUnit.IsDead && cardInstance.SourceData.SwordRule == SwordCardRule.Charge) // 명중한 돌진과 생존 대상 확인
        { // 돌진 후속 효과 시작
            targetUnit.QueueChargeDefense(cardInstance.OwnerUnit, cardInstance.SourceData.SecondaryEffectValue); // 피격 대상의 다음 턴 물리 방어 약화 예약
        } // 돌진 후속 효과 종료
        if (!targetUnit.IsDead && cardInstance.SourceData.SwordRule == SwordCardRule.FlameSlash) // 명중한 화염 참격과 생존 대상 확인
        { // 화상 부여 시작
            var result = targetUnit.ApplyStatusEffect(cardInstance.StatusEffectType, cardInstance.SourceData.SecondaryEffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit); // 기존 면역과 지속 피해 처리에 화상 전달
            FindUnitView(targetUnit)?.ShowStatusApplyFeedback(cardInstance.StatusEffectType, result); // 화상 부여 결과 표시
        } // 화상 부여 종료
        var appliedStatus = SwordCardCombat.ApplyHitStatus(cardInstance, targetUnit, supplyRelics, out var statusResult); // 실제 명중과 보유 구슬을 반영한 후속 상태 확인
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
        chariotConcentration.RecordAttack(cardInstance, targetUnit); // 전차의 대상별 공격과 표식 처리
        string damageLabel = cardInstance.DamageType == BattleDamageType.Magical ? "마법" : cardInstance.DamageType == BattleDamageType.Physical ? "물리" : "일반"; // 피해 유형 이름 계산
        Debug.Log($"[BattleDamage] 카드 / {cardInstance.DisplayName} / 대상 {targetUnit.DisplayName} / {damageLabel} / 원본 {damageResult.RawDamage} / 방어 {damageResult.DefenseValue} / 감소 {damageResult.ReducedDamage} / 최종 {damageResult.FinalDamage} / 실제 {damageResult.AppliedDamage}"); // 카드 피해 상세 출력
        return damageResult.AppliedDamage; // 실제 피해량 반환
    } // 타격 판정 종료
    private int GetDirectCardHealing(CardInstance card, int healing) // 회복자의 정신력 기준 직접 카드 회복 총량
    { // 직접 회복 보정 시작
        int modified = card.OwnerUnit.ModifyOutgoingHealing(healing); // 기존 정신 상태의 비율 보정
        int bonus = healing > 0 && card.OwnerUnit.CurrentMental >= 50 ? RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.HighMentalCardHealing)?.EffectValue ?? 0 : 0; // 정신력 오십 이상인 회복자의 흰 장미 가산량
        return (int)Math.Min(int.MaxValue, (long)modified + bonus + (healing > 0 ? bandageBonus : 0)); // 최대 체력을 적용하기 전 직접 회복 수치
    } // 직접 회복 보정 종료
    private int RestoreDirectCardHealth(CardInstance card, BattleUnitRuntime target, int healing) // 직접 카드 회복과 아군 턴의 첫 유지 회복 처리
    { // 실제 카드 회복 시작
        if (healing > 0 && target != null && target.Team == BattleTeam.Ally && !target.IsDead && target.CurrentHealth < target.MaxHealth && turnRuntime.IsPlayerTurn && lastRetainedHealingRound != turnRuntime.CurrentRound && runtimeDeck.HasRetainedHandCardOtherThan(card)) // 실제 회복 대상과 사용 후 남는 유지 손패 및 턴 제한 확인
        { // 흰 깃털 펜던트 처리 시작
            int bonus = BattleSupplyRelics.Find(RelicEffectType.RetainedHandHealing)?.EffectValue ?? 0; // 보유한 유지 회복 유물의 수치
            if (bonus > 0) // 양수 가산량 확인
            { // 유지 회복 보너스 적용 시작
                lastRetainedHealingRound = turnRuntime.CurrentRound; // 회복 이벤트 전에 같은 턴 재발동 차단
                healing = (int)Math.Min(int.MaxValue, (long)healing + bonus); // 이번 대상의 회복량 증가
            } // 유지 회복 보너스 적용 종료
        } // 흰 깃털 펜던트 처리 종료
        return target?.RestoreHealth(healing, card.OwnerUnit) ?? 0; // 체력 상한을 반영한 실제 카드 회복량 반환
    } // 실제 카드 회복 종료
    private int ApplyFirstAndRetainedDamage(CardInstance card, BattleUnitRuntime target, int damage) // 명중한 아군 카드의 직접 적 피해 가산
    { // 직접 명중 유물 확인 시작
        if (damage <= 0 || card.OwnerUnit.Team != BattleTeam.Ally || target == null || target.IsDead || target.Team != BattleTeam.Enemy) // 양수 피해와 살아 있는 적의 직접 명중 확인
        { // 명중 가산 제외 시작
            return damage; // 현재 타격 수치 유지
        } // 명중 가산 제외 종료
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
        return (int)Math.Min(int.MaxValue, (long)damage + Math.Max(0, firstBonus) + Math.Max(0, retainedBonus)); // 방어와 방어도 앞에서 직접 타격 가산
    } // 직접 명중 유물 확인 종료
    private int ApplyDirectHitRelics(CardInstance card, BattleUnitRuntime target, int damage) // 명중한 한 타격의 소유 유물 보정
    { // 실제 명중 보정 시작
        bool marked = target?.Team == BattleTeam.Enemy && (target.GetStatusEffect(BattleStatusEffectType.PublicMark) != null || target.GetStatusEffect(BattleStatusEffectType.ChariotMark) != null); // 실제 피격 대상의 공명 또는 전차 표식 확인
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
        if ((pendingCardChoices == null || pendingCardChoices.Count == 0) && (pendingDebuffChoices == null || pendingDebuffChoices.Count == 0)) // 기존 카드 및 효과 선택을 기다릴지 확인
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
    private void ApplySunChain(CardInstance card, BattleUnitRuntime primary) // 태양의 다른 적 연속 공격 처리
    { // 태양 연속 공격 시작
        BattleUnitRuntime target = SunChainAttack.TryAttack(card, primary, enemyActions.EnemyUnits, out bool missed, // 다른 적의 독립 명중 처리
            preparedSwordDamage: preparedSwordDamage, // 정비 보너스가 남아 있는 검 피해
            applyTargetBonus: (hit, damage) => ApplyDirectHitRelics(card, hit, damage), // 태양 연속 공격의 기존 명중 유물
            applyFlatTargetBonus: (hit, damage) => ApplyFirstAndRetainedDamage(card, hit, damage)); // 치명타 뒤 한 번 제한의 직접 타격 가산
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
        if (cardInstance.EffectType == CardEffectType.ReviveAlly) // 전투불능 아군의 부활 효과 확인
        { // 부활 문구 처리 시작
            return "부활"; // 실제 효과 로그 이름 반환
        } // 부활 문구 처리 종료
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
        splitBarrierFirstTarget = null; // 미확정 분할 방벽 대상 제거
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
        } // 손패 이벤트 해제 종료
        runtimeDeck.StateChanged -= HandleDeckStateChanged; // 덱 상태 변경 이벤트 해제
        turnRuntime.StateChanged -= HandleTurnStateChanged; // 턴 상태 변경 이벤트 해제
        UnregisterUnitViewEvents(allyUnitViews); // 아군 클릭 이벤트 해제
        UnregisterUnitViewEvents(enemyUnitViews); // 적 클릭 이벤트 해제
        selectedCard = null; // 선택 카드 참조 제거
        splitBarrierFirstTarget = null; // 연결 해제 시 분할 방벽 대상 제거
        actionPending = false; // 카드 행동 진행 상태 해제
        ClearTargetHighlights(allyUnitViews); // 아군 대상 강조 해제
        ClearTargetHighlights(enemyUnitViews); // 적 대상 강조 해제
    } // 연결 해제 종료
} // 클래스 종료
