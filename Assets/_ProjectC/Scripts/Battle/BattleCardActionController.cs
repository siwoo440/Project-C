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
    private readonly IReadOnlyList<BattleUnitView> allyUnitViews; // 아군 유닛 화면 목록
    private readonly IReadOnlyList<BattleUnitView> enemyUnitViews; // 적 유닛 화면 목록
    private CardInstance selectedCard; // 현재 선택 카드
    private bool actionPending; // 카드 행동 연출 진행 여부
    private bool disposed; // 연결 해제 여부
    public CardInstance SelectedCard => selectedCard; // 현재 선택 카드 조회
    public event Action<CardInstance, IReadOnlyList<BattleUnitRuntime>> CardUsed; // 카드 효과 적용 시작 이벤트
    public BattleCardActionController(BattleDeckRuntime battleDeck, BattleActionPointRuntime actionPoints, BattleTurnRuntime battleTurn, BattleHandView battleHandView, BattleActionSequenceRunner sequenceRunner, BattleStatusEffectProcessor processor, IReadOnlyList<BattleUnitView> allyViews, IReadOnlyList<BattleUnitView> enemyViews) // 카드 행동 관리자 생성
    { // 생성자 시작
        runtimeDeck = battleDeck ?? throw new ArgumentNullException(nameof(battleDeck)); // 런타임 덱 저장
        sharedActionPoints = actionPoints ?? throw new ArgumentNullException(nameof(actionPoints)); // 공용 행동력 저장
        turnRuntime = battleTurn ?? throw new ArgumentNullException(nameof(battleTurn)); // 전투 턴 관리자 저장
        handView = battleHandView ?? throw new ArgumentNullException(nameof(battleHandView)); // 손패 화면 저장
        actionSequenceRunner = sequenceRunner ?? throw new ArgumentNullException(nameof(sequenceRunner)); // 행동 연출 실행기 저장
        statusEffectProcessor = processor ?? throw new ArgumentNullException(nameof(processor)); // 공통 상태 처리기 저장
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
        if (cardInstance == null || !ContainsHandCard(cardInstance)) // 유효한 손패 카드 확인
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
        } // 입력 잠금 처리 종료
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
    } // 턴 상태 처리 종료
    private void HandleUnitDied(BattleUnitRuntime runtimeUnit) // 유닛 사망 처리
    { // 유닛 사망 처리 시작
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
        if (selectedCard != null && selectedCard.OwnerUnit == runtimeUnit) // 선택 카드 소유자 사망 확인
        { // 소유자 사망 처리 시작
            CancelSelection(); // 카드 선택 상태 초기화
        } // 소유자 사망 처리 종료
    } // 유닛 사망 처리 종료
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
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
    } // 카드 실행 종료
    private void ResolveCardImpact(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits) // 카드 충돌 시 효과 적용
    { // 카드 충돌 처리 시작
        CardUsed?.Invoke(cardInstance, targetUnits); // 피해와 회복 전 카드 사용 상세 알림
        int totalAppliedAmount = 0; // 전체 실제 적용량 초기화
        foreach (BattleUnitRuntime targetUnit in targetUnits) // 대상 유닛 순회
        { // 효과 적용 시작
            totalAppliedAmount += ApplyCardEffect(cardInstance, targetUnit); // 카드 효과 적용량 누적
        } // 효과 적용 종료
        bool discarded = runtimeDeck.DiscardCard(cardInstance); // 사용 카드를 버린 카드 더미로 이동
        if (!discarded) // 카드 이동 결과 확인
        { // 카드 이동 실패 처리 시작
            Debug.LogError($"[BattleCardActionController] 사용 카드 이동에 실패했습니다: {cardInstance.DisplayName}"); // 이동 실패 출력
        } // 카드 이동 실패 처리 종료
        string effectLabel = GetEffectLabel(cardInstance); // 카드 효과 로그 문구 생성
        Debug.Log($"[BattleCardActionController] 카드 사용 완료 - {cardInstance.DisplayName} / 대상 {targetUnits.Count}명 / {effectLabel} 적용량 {totalAppliedAmount} / 남은 공용 AP {sharedActionPoints.CurrentActionPoints}"); // 카드 사용 결과 출력
    } // 카드 충돌 처리 종료
    private void HandleActionSequenceCompleted() // 카드 행동 연출 완료 처리
    { // 연출 완료 처리 시작
        actionPending = false; // 카드 행동 진행 상태 해제
        handView.RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
    } // 연출 완료 처리 종료
    private bool IsValidTarget(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 대상 유효성 검사
    { // 대상 검사 시작
        if (cardInstance == null || targetUnit == null || targetUnit.IsDead) // 카드와 대상 상태 확인
        { // 선택 불가 처리 시작
            return false; // 잘못된 대상 반환
        } // 선택 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.Heal && targetUnit.CurrentHealth >= targetUnit.MaxHealth) // 회복 대상 체력 확인
        { // 회복 불필요 처리 시작
            return false; // 회복 대상 불가 반환
        } // 회복 불필요 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs && !targetUnit.HasDebuff) // 정화 대상 디버프 확인
        { // 정화 불필요 처리 시작
            return false; // 정화 대상 불가 반환
        } // 정화 불필요 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect && cardInstance.StatusEffectType == BattleStatusEffectType.None) // 상태 이상 카드 설정 확인
        { // 상태 설정 오류 처리 시작
            return false; // 상태 이상 대상 불가 반환
        } // 상태 설정 오류 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental && !targetUnit.CanChangeMental(cardInstance.MentalChangeValue)) // 정신력 변경 가능 여부 확인
        { // 정신력 변경 불가 처리 시작
            return false; // 정신력 대상 불가 반환
        } // 정신력 변경 불가 처리 종료
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
        return effectType == CardEffectType.Damage || effectType == CardEffectType.Heal || effectType == CardEffectType.ApplyStatusEffect || effectType == CardEffectType.RemoveDebuffs || effectType == CardEffectType.ChangeMental; // 지원 카드 효과 결과 반환
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
        if (cardInstance.EffectType == CardEffectType.Heal) // 회복 효과 확인
        { // 회복 효과 처리 시작
            int modifiedHealing = cardInstance.OwnerUnit.ModifyOutgoingHealing(cardInstance.EffectValue); // 정신 상태 포함 회복량 계산
            return targetUnit.RestoreHealth(modifiedHealing, cardInstance.OwnerUnit); // 실제 회복량 반환
        } // 회복 효과 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 처리 시작
            BattleStatusEffectApplyResult applyResult = targetUnit.ApplyStatusEffect(cardInstance.StatusEffectType, cardInstance.EffectValue, cardInstance.StatusDuration, cardInstance.StatusMaximumStacks, cardInstance.OwnerUnit); // 대상 상태 이상 적용
            BattleUnitView targetView = FindUnitView(targetUnit); // 대상 유닛 화면 조회
            targetView?.ShowStatusApplyFeedback(cardInstance.StatusEffectType, applyResult); // 상태 이상 적용 결과 표시
            return applyResult == BattleStatusEffectApplyResult.Applied || applyResult == BattleStatusEffectApplyResult.Stacked ? cardInstance.EffectValue : 0; // 상태 이상 적용 수치 반환
        } // 상태 이상 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs) // 디버프 해제 효과 확인
        { // 디버프 해제 처리 시작
            IReadOnlyList<BattleStatusEffectProcessResult> cleanseResults = statusEffectProcessor.CleanseDebuffs(targetUnit); // 대상 디버프 통합 정화
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
        int baseDamage = cardInstance.EffectValue + cardInstance.OwnerUnit.AttackPowerBonus; // 공격력 증가 포함 원본 피해 계산
        int modifiedDamage = cardInstance.OwnerUnit.ModifyOutgoingDamage(baseDamage); // 정신 상태 포함 최종 원본 피해 계산
        BattleDamageResult damageResult = targetUnit.TakeDamage(modifiedDamage, cardInstance.DamageType, cardInstance.OwnerUnit); // 카드 피해 계산과 적용
        string damageLabel = cardInstance.DamageType == BattleDamageType.Magical ? "마법" : cardInstance.DamageType == BattleDamageType.Physical ? "물리" : "일반"; // 피해 유형 이름 계산
        Debug.Log($"[BattleDamage] 카드 / {cardInstance.DisplayName} / 대상 {targetUnit.DisplayName} / {damageLabel} / 원본 {damageResult.RawDamage} / 방어 {damageResult.DefenseValue} / 감소 {damageResult.ReducedDamage} / 최종 {damageResult.FinalDamage} / 실제 {damageResult.AppliedDamage}"); // 카드 피해 상세 출력
        return damageResult.AppliedDamage; // 실제 피해량 반환
    } // 단일 효과 적용 종료
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
        if (cardInstance.EffectType == CardEffectType.Heal) // 회복 효과 확인
        { // 회복 문구 처리 시작
            return "회복"; // 회복 문구 반환
        } // 회복 문구 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 문구 처리 시작
            return BattleStatusEffectInstance.GetDisplayName(cardInstance.StatusEffectType); // 상태 이상 이름 반환
        } // 상태 이상 문구 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs) // 디버프 해제 효과 확인
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
