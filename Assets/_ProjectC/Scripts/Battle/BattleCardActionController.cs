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
    private readonly BattleEnemyActionRuntime enemyActions; // 적 예정 행동 관리자
    private readonly WinterfallBossPhaseRuntime bossPhaseRuntime; // 선택적인 보스 페이즈 관리자
    private readonly LoversPartnerRuntime loversPartner; // 연인 파트너와 행동 기록
    private readonly ChariotConcentrationRuntime chariotConcentration; // 전차 집중 공격과 표식 기록
    private readonly StarRecoveryPassiveRuntime starRecoveryPassive; // 별의 턴당 추가 예약 회복
    private readonly WorldCoordinationRuntime worldCoordination; // 세계의 동료 전용 카드 연계 기록
    private readonly IReadOnlyList<BattleUnitView> allyUnitViews; // 아군 유닛 화면 목록
    private readonly IReadOnlyList<BattleUnitView> enemyUnitViews; // 적 유닛 화면 목록
    private CardInstance selectedCard; // 현재 선택 카드
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
        if (cardInstance.OwnerUnit.IsDead) // 카드 소유자 사망 확인
        { // 사망 소유자 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 사망한 소유자의 카드는 사용할 수 없습니다: {cardInstance.OwnerUnit.DisplayName}"); // 사용 불가 출력
            return; // 카드 클릭 처리 중단
        } // 사망 소유자 처리 종료
        if (!sharedActionPoints.CanSpend(cardInstance.ApCost)) // 카드 비용 지불 가능 확인
        { // 행동력 부족 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 공용 AP가 부족합니다: 현재 {sharedActionPoints.CurrentActionPoints} / 필요 {cardInstance.ApCost}"); // 행동력 부족 출력
            handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
            return; // 카드 클릭 처리 중단
        } // 행동력 부족 처리 종료
        if (selectedCard == cardInstance) // 동일 카드 재선택 확인
        { // 재선택 처리 시작
            CancelSelection(); // 카드 선택 취소
            return; // 카드 클릭 처리 종료
        } // 재선택 처리 종료
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
        UpdateTargetHighlights(); // 단일 대상 후보 강조
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
        ExecuteCard(selectedCard, CreateSingleTargetList(runtimeUnit)); // 선택 대상에게 카드 사용
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
        if (selectedCard != null && selectedCard.OwnerUnit == runtimeUnit) // 선택 카드 소유자 사망 확인
        { // 소유자 사망 처리 시작
            CancelSelection(); // 카드 선택 상태 초기화
        } // 소유자 사망 처리 종료
    } // 유닛 사망 처리 종료
    private void HandleAllyHealthChanged(BattleUnitRuntime runtimeUnit) // 아군 체력 변경 시 카드 사용 상태 갱신
    { // 아군 체력 변경 처리 시작
        if (selectedCard != null && selectedCard.OwnerUnit == runtimeUnit && DevilBloodPrice.IsBloodPriceCard(selectedCard) && !DevilBloodPrice.CanPay(selectedCard)) // 선택된 체력 비용 카드의 지불 가능 여부 확인
        { // 선택 취소 처리 시작
            CancelSelection(); // 체력 부족 카드 선택 해제
        } // 선택 취소 처리 종료
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
        if (!IsSupportedEffect(cardInstance.EffectType)) // 지원 효과 확인
        { // 미지원 효과 처리 시작
            Debug.LogError($"[BattleCardActionController] 지원하지 않는 카드 효과입니다: {cardInstance.EffectType}"); // 미지원 효과 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 미지원 효과 처리 종료
        BattleUnitView actorView = FindUnitView(cardInstance.OwnerUnit); // 카드 행동자 화면 조회
        List<BattleUnitView> targetViews = CollectUnitViews(targetUnits); // 카드 대상 화면 목록 생성
        if (!actionSequenceRunner.CanStartAction(actorView, targetViews)) // 행동 연출 시작 가능 확인
        { // 연출 시작 불가 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 카드 행동 연출을 시작할 수 없습니다: {cardInstance.DisplayName}"); // 연출 시작 실패 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 연출 시작 불가 처리 종료
        if (!sharedActionPoints.Spend(cardInstance.ApCost)) // 카드 비용 차감 확인
        { // 비용 차감 실패 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 카드 비용 차감에 실패했습니다: {cardInstance.DisplayName}"); // 비용 차감 실패 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 비용 차감 실패 처리 종료
        List<BattleUnitRuntime> targetSnapshot = new List<BattleUnitRuntime>(targetUnits); // 연출 중 유지할 대상 목록 복사
        actionPending = true; // 카드 행동 진행 상태 저장
        CancelSelection(); // 카드 선택과 대상 강조 해제
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
    private void ResolveCardImpact(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits) // 카드 충돌 시 효과 적용
    { // 카드 충돌 처리 시작
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
        if (cardInstance.EffectType == CardEffectType.GainBlock && cardInstance.TargetType == CardTargetType.AllAllies && !cardInstance.SourceData.GrantsBlockToEachAlly) // 전체 합계를 나누는 방어 카드 확인
        { // 효과 적용 시작
            totalAppliedAmount = BattleBlockDistribution.Apply(targetUnits, cardInstance.EffectValue); // 고정 방어도를 생존 아군에게 배분
        } // 효과 적용 종료
        else // 일반 카드 효과 분기
        { // 일반 효과 처리 시작
            foreach (BattleUnitRuntime targetUnit in targetUnits) // 대상 유닛 순회
            { // 대상별 효과 적용 시작
                if (turnRuntime.HasPendingBossPhaseTransition) // 보스 페이즈 전환 예약 확인
                { // 남은 타격 취소 시작
                    break; // 미처리 대상 효과 취소
                } // 남은 타격 취소 종료
                totalAppliedAmount += ApplyCardEffect(cardInstance, targetUnit); // 카드 효과 적용량 누적
            } // 대상별 효과 적용 종료
        } // 일반 효과 처리 종료
        starRecoveryPassive.TryApply(cardInstance, targetUnits); // 별의 회복 카드 사용 시 추가 회복 예약
        bool discarded = ApplyFollowUpAndDiscard(cardInstance, targetUnits); // 후속 효과 한 번 처리 후 사용 카드 이동
        if (!discarded) // 카드 이동 결과 확인
        { // 카드 이동 실패 처리 시작
            Debug.LogError($"[BattleCardActionController] 사용 카드 이동에 실패했습니다: {cardInstance.DisplayName}"); // 이동 실패 출력
        } // 카드 이동 실패 처리 종료
        if (discarded && cardInstance.EffectType == CardEffectType.DrawThenDiscard) // 달의 드로우 후 손패 정리 확인
        { // 버릴 카드 후보 준비 시작
            pendingCardChoices = new List<CardInstance>(runtimeDeck.Hand); // 사용한 카드를 제외한 현재 손패 복사
        } // 버릴 카드 후보 준비 종료
        string effectLabel = GetEffectLabel(cardInstance); // 카드 효과 로그 문구 생성
        Debug.Log($"[BattleCardActionController] 카드 사용 완료 - {cardInstance.DisplayName} / 대상 {targetUnits.Count}명 / {effectLabel} 적용량 {totalAppliedAmount} / 남은 공용 AP {sharedActionPoints.CurrentActionPoints}"); // 카드 사용 결과 출력
    } // 카드 충돌 처리 종료
    private bool ApplyFollowUpAndDiscard(CardInstance card, IReadOnlyList<BattleUnitRuntime> targets) // 기본 효과 후 보조 대상과 카드 이동 처리
    { // 후속 효과 처리 시작
        int value = card.SourceData.SecondaryEffectValue; // 원본의 후속 수치 조회
        switch (card.SourceData.FollowUpEffect) // 카드별 추가 대상과 효과 선택
        { // 효과 구분 시작
            case CardFollowUpEffect.GainOwnerBlock: // 공격한 소유자에게 방어도 지급
                card.OwnerUnit.GainBlock(value); // 소유자만 방어도 획득
                break; // 방어 처리 종료
            case CardFollowUpEffect.HealAllAllies: // 공격 후 모든 생존 아군 회복
                int healing = card.OwnerUnit.ModifyOutgoingHealing(value); // 일반 회복과 같은 소유자 보정 적용
                foreach (BattleUnitRuntime ally in enemyActions.AllyUnits) // 전투에 참여한 아군 순회
                { // 동료 회복 시작
                    ally?.RestoreHealth(healing, card.OwnerUnit); // 생존 여부와 최대 체력에 맞춘 회복
                } // 동료 회복 종료
                break; // 전체 회복 종료
            case CardFollowUpEffect.GainTargetBlock: // 선택한 아군에게 방어도 지급
                foreach (BattleUnitRuntime target in targets) // 치료한 대상 순회
                { // 대상 방어 시작
                    target.GainBlock(value); // 치료 대상만 방어도 획득
                } // 대상 방어 종료
                break; // 대상 방어 처리 종료
            case CardFollowUpEffect.DrawCards: // 사용한 카드 자리를 비운 뒤 드로우
                return runtimeDeck.TryPlayAndDraw(card, value, out _); // 자가 재드로우 없이 한 번 버린 더미로 이동
        } // 효과 구분 종료
        return card.EffectType == CardEffectType.DrawThenDiscard || runtimeDeck.DiscardCard(card); // 기존 선행 드로우 이동 또는 일반 카드 버리기
    } // 후속 효과 처리 종료
    private void HandleActionSequenceCompleted() // 카드 행동 연출 완료 처리
    { // 연출 완료 처리 시작
        actionPending = false; // 카드 행동 진행 상태 해제
        bossPhaseRuntime?.CompletePendingTransition(); // 충돌과 카드 이동 완료 후 보스 페이즈 전환
        ShowPendingCardChoices(); // 대기 중인 드로우 선택 화면 표시
        ShowPendingDebuffChoices(); // 대기 중인 디버프 선택 화면 표시
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
            return; // 선택 화면 표시 중단
        } // 캔버스 없음 처리 종료
        debuffChoiceView = BattleDebuffChoiceView.Show(canvas.transform, pendingDebuffChoices, HandleDebuffChoiceSelected); // 디버프 한 가지 선택 화면 생성
        handView.SetInteractionLocked(true); // 선택 중 손패와 턴 종료 잠금
    } // 디버프 선택 표시 종료
    private void HandleDebuffChoiceSelected(BattleStatusEffectInstance effect) // 선택한 디버프 이전 처리
    { // 선택 디버프 처리 시작
        BattleDebuffTransfer.TryTransfer(pendingDebuffSource, pendingDebuffRecipient, effect); // 선택한 디버프 한 가지 이전
        pendingDebuffChoices = null; // 디버프 후보 정리
        pendingDebuffSource = null; // 원래 동료 참조 정리
        pendingDebuffRecipient = null; // 디버프 이전 대상 참조 정리
        debuffChoiceView = null; // 닫힌 선택 화면 참조 정리
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
            return; // 선택 화면 표시 중단
        } // 화면 누락 처리 종료
        string title = pendingChoiceEffect == CardEffectType.RecoverDiscard ? "회수할 버린 카드 선택" : pendingChoiceEffect == CardEffectType.ArrangeDrawTop ? "다음에 뽑을 카드 선택" : pendingChoiceEffect == CardEffectType.DrawThenDiscard ? "버릴 손패 한 장 선택" : "가져올 카드 한 장 선택"; // 카드 선택 안내 설정
        drawChoiceView = BattleDrawChoiceView.Show(canvas.transform, pendingCardChoices, HandleCardChoiceSelected, title); // 선택 화면 생성
        handView.SetInteractionLocked(true); // 선택 중 손패와 턴 종료 잠금
    } // 선택 화면 표시 종료
    private void HandleCardChoiceSelected(CardInstance choice) // 드로우 후보 선택 처리
    { // 선택 처리 시작
        int choiceCount = pendingCardChoices == null ? 0 : pendingCardChoices.Count; // 현재 후보 수 조회
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
        } // 선택 드로우 종료
        pendingCardChoices = null; // 대기 후보 정리
        drawChoiceView = null; // 닫힌 선택 화면 참조 정리
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
        handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
    } // 선택 처리 종료
    private void HandleRemainingCardSelected(bool discard) // 남은 카드 처리 선택
    { // 남은 카드 처리 시작
        if (discard) // 버리기 선택 확인
        { // 남은 카드 버리기 시작
            runtimeDeck.TryDiscardDrawTop(remainingChoiceCard); // 남은 맨 위 카드 버리기
        } // 남은 카드 버리기 종료
        remainingChoiceCard = null; // 남은 카드 참조 정리
        remainingChoiceView = null; // 닫힌 선택 화면 참조 정리
        handView.SetInteractionLocked(false); // 손패와 턴 종료 잠금 해제
    } // 남은 카드 처리 종료
    private void CloseCardChoice() // 대기 선택 화면 정리
    { // 선택 화면 정리 시작
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
        if (cardInstance == null || targetUnit == null || targetUnit.IsDead) // 카드와 대상 상태 확인
        { // 선택 불가 처리 시작
            return false; // 잘못된 대상 반환
        } // 선택 불가 처리 종료
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
        if (cardInstance.EffectType == CardEffectType.ChooseDraw && !runtimeDeck.HasDrawableCards) // 생존 소유자의 선택 가능 카드 확인
        { // 선택 불가 처리 시작
            return false; // 선택 불가 반환
        } // 선택 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.ArrangeDrawTop && runtimeDeck.PeekDrawTop(1).Count == 0) // 현재 덱 위 정리 가능 카드 확인
        { // 덱 순서 정리 불가 시작
            return false; // 선택 불가 반환
        } // 덱 순서 정리 불가 종료
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard && !runtimeDeck.HasDrawableCards && runtimeDeck.HandCount <= 1) // 드로우와 사용 후 남을 손패 확인
        { // 드로우와 버리기 불가 시작
            return false; // 선택 불가 반환
        } // 드로우와 버리기 불가 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack && !enemyActions.CanProtectAlly(cardInstance.OwnerUnit, targetUnit)) // 대신 받을 동료 공격 확인
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
        if (cardInstance.EffectType == CardEffectType.RedirectAttackAndDefend) // 공격 유도 카드 확인
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
            bool targetable = unitView != null && IsValidTarget(selectedCard, unitView.RuntimeUnit); // 대상 선택 가능 여부 계산
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
    private static bool IsSupportedEffect(CardEffectType effectType) // 지원 효과 여부 확인
    { // 지원 효과 검사 시작
        return effectType == CardEffectType.Damage || effectType == CardEffectType.Heal || effectType == CardEffectType.ApplyStatusEffect || effectType == CardEffectType.RemoveDebuffs || effectType == CardEffectType.RemoveOneDebuff || effectType == CardEffectType.ChangeMental || effectType == CardEffectType.RedirectAttackAndDefend || effectType == CardEffectType.ProtectAttackTarget || effectType == CardEffectType.ChooseDraw || effectType == CardEffectType.RecoverDiscard || effectType == CardEffectType.GainBlock || effectType == CardEffectType.SelectPartner || effectType == CardEffectType.GuardAllyAttack || effectType == CardEffectType.ArrangeDrawTop || effectType == CardEffectType.RerollRandomAttack || effectType == CardEffectType.ShareAllyAttack || effectType == CardEffectType.TransferAllyDebuff || effectType == CardEffectType.DrawThenDiscard; // 지원 카드 효과 결과 반환
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
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard) // 달의 드로우와 손패 정리 확인
        { // 달 카드 드로우 처리 시작
            pendingChoiceEffect = CardEffectType.DrawThenDiscard; // 드로우 후 손패 선택 효과 저장
            return runtimeDeck.TryPlayAndDraw(cardInstance, cardInstance.EffectValue, out int drawnCount) ? drawnCount : 0; // 사용 카드 공간을 확보하고 가능한 카드 드로우
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
            return enemyActions.TryProtectAlly(cardInstance.OwnerUnit, targetUnit) ? 1 : 0; // 첫 예정 공격 대신 받기 결과 반환
        } // 동료 보호 처리 종료
        if (cardInstance.EffectType == CardEffectType.ArrangeDrawTop) // 은둔자의 덱 순서 정리 확인
        { // 덱 순서 정리 준비 시작
            pendingChoiceEffect = CardEffectType.ArrangeDrawTop; // 덱 순서 정리 선택 효과 저장
            pendingCardChoices = runtimeDeck.PeekDrawTop(cardInstance.EffectValue); // 현재 덱 위의 후보만 조회
            return pendingCardChoices.Count; // 확인 가능한 후보 수 반환
        } // 덱 순서 정리 준비 종료
        if (cardInstance.EffectType == CardEffectType.SelectPartner) // 연인 파트너 지정 효과 확인
        { // 파트너 지정 처리 시작
            return loversPartner.TrySetPartner(cardInstance.OwnerUnit, targetUnit) ? 1 : 0; // 지정 성공 여부 반환
        } // 파트너 지정 처리 종료
        if (cardInstance.EffectType == CardEffectType.GainBlock) // 방어도 획득 효과 확인
        { // 방어도 획득 시작
            int gainedBlock = targetUnit.GainBlock(cardInstance.EffectValue); // 선택 대상의 실제 방어도 획득
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
            pendingCardChoices = runtimeDeck.PrepareDrawChoices(cardInstance.EffectValue); // 덱 위 선택 후보 준비
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
        { // 황제 전용 효과 시작
            if (!enemyActions.TryRedirectAttack(targetUnit, cardInstance.OwnerUnit)) // 선택 적의 공격 유도 확인
            { // 유도 실패 처리 시작
                return 0; // 방어도 미획득 반환
            } // 유도 실패 처리 종료
            return cardInstance.OwnerUnit.GainBlock(cardInstance.EffectValue); // 소유자의 실제 방어도 획득량 반환
        } // 황제 전용 효과 종료
        if (cardInstance.EffectType == CardEffectType.Heal) // 회복 효과 확인
        { // 회복 효과 처리 시작
            BattleStatusEffectType removed = cardInstance.SourceData.StatusRemovedByHealing; // 회복 전에 제거할 상태 조회
            if (removed != BattleStatusEffectType.None && targetUnit.RemoveStatusEffect(removed)) // 지정된 상태가 실제로 제거된 경우 확인
            { // 상태 해제 표시 시작
                FindUnitView(targetUnit)?.ShowStatusFeedback(BattleStatusEffectInstance.GetDisplayName(removed) + " 해제", false); // 지정한 상태 해제 표시
            } // 상태 해제 표시 종료
            int baseHealing = cardInstance.SourceData.CardId == "CRD_ARC_20_RESCUE" ? JudgementRecovery.GetHealingAmount(targetUnit, cardInstance.EffectValue, cardInstance.SourceData.SecondaryEffectValue) : cardInstance.EffectValue; // 심판의 위험 구간 추가 회복 계산
            int modifiedHealing = cardInstance.OwnerUnit.ModifyOutgoingHealing(baseHealing); // 정신 상태 포함 회복량 계산
            return targetUnit.RestoreHealth(modifiedHealing, cardInstance.OwnerUnit); // 실제 회복량 반환
        } // 회복 효과 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 처리 시작
            BattleStatusEffectApplyResult applyResult = targetUnit.ApplyStatusEffect(cardInstance.StatusEffectType, cardInstance.EffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit); // 대상 상태 이상 적용
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
            return ApplyDamageHit(cardInstance, targetUnit); // 현재 대상의 명중과 피해 판정
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

    private int ApplyDamageHit(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 공격 한 타격 적용
    { // 타격 판정 시작
        if (!BattleHitChance.Roll(cardInstance.OwnerUnit)) // 실명 상태의 공격 명중 확인
        { // 카드 공격 빗나감 처리 시작
            BattleCardDamageContext.TryConsume(out _, out _); // 빗나간 대상의 카드 피해 문맥 소비
            FindUnitView(targetUnit)?.ShowStatusFeedback("빗나감", false); // 선택 대상의 빗나감 표시
            if (SunChainAttack.IsChainCard(cardInstance)) // 태양의 독립 연속 공격 확인
            { // 다음 공격 판정 시작
                ApplySunChain(cardInstance, targetUnit); // 첫 공격과 독립적인 두 번째 공격 실행
            } // 다음 공격 판정 종료
            return 0; // 첫 공격 피해 없음 반환
        } // 카드 공격 빗나감 처리 종료
        int baseDamage = cardInstance.EffectValue + cardInstance.OwnerUnit.AttackPowerBonus; // 공격력 증가 포함 원본 피해 계산
        if (cardInstance.SourceData.CardId == "CRD_ARC_13_FINISH" && targetUnit.HasDebuff) // 죽음 전용 카드와 약화된 적 확인
        { // 약화 대상 추가 피해 시작
            baseDamage = (int)Math.Min(int.MaxValue, (long)baseDamage * 3 / 2); // 임시 추가 피해 오십 퍼센트 적용
        } // 약화 대상 추가 피해 종료
        int modifiedDamage = cardInstance.OwnerUnit.ModifyOutgoingDamage(baseDamage); // 정신 상태 포함 최종 원본 피해 계산
        BattleDamageResult damageResult = targetUnit.TakeDamage(modifiedDamage, cardInstance.DamageType, cardInstance.OwnerUnit); // 카드 피해 계산과 적용
        if (SunChainAttack.IsChainCard(cardInstance) && !turnRuntime.HasPendingBossPhaseTransition) // 보스 전환이 예약되지 않은 태양 연속 공격 확인
        { // 태양 추가 공격 시작
            ApplySunChain(cardInstance, targetUnit); // 다른 생존 적에게 두 번째 공격 적용
        } // 태양 추가 공격 종료
        chariotConcentration.RecordAttack(cardInstance, targetUnit); // 전차의 대상별 공격과 표식 처리
        string damageLabel = cardInstance.DamageType == BattleDamageType.Magical ? "마법" : cardInstance.DamageType == BattleDamageType.Physical ? "물리" : "일반"; // 피해 유형 이름 계산
        Debug.Log($"[BattleDamage] 카드 / {cardInstance.DisplayName} / 대상 {targetUnit.DisplayName} / {damageLabel} / 원본 {damageResult.RawDamage} / 방어 {damageResult.DefenseValue} / 감소 {damageResult.ReducedDamage} / 최종 {damageResult.FinalDamage} / 실제 {damageResult.AppliedDamage}"); // 카드 피해 상세 출력
        return damageResult.AppliedDamage; // 실제 피해량 반환
    } // 타격 판정 종료
    private void ApplySunChain(CardInstance card, BattleUnitRuntime primary) // 태양의 다른 적 연속 공격 처리
    { // 태양 연속 공격 시작
        BattleUnitRuntime target = SunChainAttack.TryAttack(card, primary, enemyActions.EnemyUnits, out bool missed); // 다른 적의 독립 명중과 피해 처리
        if (target != null && missed) // 추가 공격 빗나감 확인
        { // 추가 공격 피드백 시작
            FindUnitView(target)?.ShowStatusFeedback("빗나감", false); // 추가 대상의 빗나감 표시
        } // 추가 공격 피드백 종료
    } // 태양 연속 공격 종료
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
                    if (unitView.RuntimeUnit.Team == BattleTeam.Ally) // 아군 체력 이벤트 연결 확인
                    { // 아군 체력 연결 시작
                        unitView.RuntimeUnit.HealthChanged += HandleAllyHealthChanged; // 체력 비용 카드 갱신 이벤트 등록
                    } // 아군 체력 연결 종료
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
                    if (unitView.RuntimeUnit.Team == BattleTeam.Ally) // 아군 체력 이벤트 연결 확인
                    { // 아군 체력 연결 해제 시작
                        unitView.RuntimeUnit.HealthChanged -= HandleAllyHealthChanged; // 체력 비용 카드 갱신 이벤트 해제
                    } // 아군 체력 연결 해제 종료
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
        actionPending = false; // 카드 행동 진행 상태 해제
        ClearTargetHighlights(allyUnitViews); // 아군 대상 강조 해제
        ClearTargetHighlights(enemyUnitViews); // 적 대상 강조 해제
    } // 연결 해제 종료
} // 클래스 종료
