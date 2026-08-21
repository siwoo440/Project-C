using System; // 기본 인터페이스 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 로그 기능 사용
public sealed class BattleCardActionController : IDisposable // 카드 선택과 사용 흐름 관리
{ // 클래스 시작
    private readonly BattleDeckRuntime runtimeDeck; // 연결된 런타임 덱
    private readonly BattleHandView handView; // 연결된 손패 화면
    private readonly IReadOnlyList<BattleUnitView> allyUnitViews; // 아군 유닛 화면 목록
    private readonly IReadOnlyList<BattleUnitView> enemyUnitViews; // 적 유닛 화면 목록
    private CardInstance selectedCard; // 현재 선택 카드
    private bool disposed; // 연결 해제 여부
    public CardInstance SelectedCard => selectedCard; // 현재 선택 카드 조회
    public BattleCardActionController(BattleDeckRuntime battleDeck, BattleHandView battleHandView, IReadOnlyList<BattleUnitView> allyViews, IReadOnlyList<BattleUnitView> enemyViews) // 카드 행동 관리자 생성
    { // 생성자 시작
        runtimeDeck = battleDeck ?? throw new ArgumentNullException(nameof(battleDeck)); // 런타임 덱 저장
        handView = battleHandView ?? throw new ArgumentNullException(nameof(battleHandView)); // 손패 화면 저장
        allyUnitViews = allyViews ?? throw new ArgumentNullException(nameof(allyViews)); // 아군 화면 목록 저장
        enemyUnitViews = enemyViews ?? throw new ArgumentNullException(nameof(enemyViews)); // 적 화면 목록 저장
        handView.CardClicked += HandleCardClicked; // 카드 클릭 이벤트 등록
        RegisterUnitViewEvents(allyUnitViews); // 아군 클릭 이벤트 등록
        RegisterUnitViewEvents(enemyUnitViews); // 적 클릭 이벤트 등록
        runtimeDeck.StateChanged += HandleDeckStateChanged; // 덱 상태 변경 이벤트 등록
    } // 생성자 종료
    private void HandleCardClicked(CardInstance cardInstance) // 카드 클릭 처리
    { // 카드 클릭 처리 시작
        if (cardInstance == null || !ContainsHandCard(cardInstance)) // 유효한 손패 카드 확인
        { // 잘못된 카드 처리 시작
            return; // 카드 클릭 처리 중단
        } // 잘못된 카드 처리 종료
        if (cardInstance.OwnerUnit.IsDead) // 카드 소유자 사망 확인
        { // 사망 소유자 처리 시작
            Debug.LogWarning($"[BattleCardActionController] 사망한 소유자의 카드는 사용할 수 없습니다: {cardInstance.OwnerUnit.DisplayName}"); // 사용 불가 출력
            return; // 카드 클릭 처리 중단
        } // 사망 소유자 처리 종료
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
            ExecuteCard(selectedCard, CollectLivingTargets(allyUnitViews)); // 전체 생존 아군에게 카드 사용
            return; // 카드 클릭 처리 종료
        } // 전체 아군 처리 종료
        if (selectedCard.TargetType == CardTargetType.AllEnemies) // 전체 적 대상 확인
        { // 전체 적 처리 시작
            ExecuteCard(selectedCard, CollectLivingTargets(enemyUnitViews)); // 전체 생존 적에게 카드 사용
            return; // 카드 클릭 처리 종료
        } // 전체 적 처리 종료
        UpdateTargetHighlights(); // 단일 대상 후보 강조
    } // 카드 클릭 처리 종료
    private void HandleUnitClicked(BattleUnitRuntime runtimeUnit) // 유닛 클릭 처리
    { // 유닛 클릭 처리 시작
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
    private void ExecuteCard(CardInstance cardInstance, IReadOnlyList<BattleUnitRuntime> targetUnits) // 카드 효과 실행
    { // 카드 실행 시작
        if (!ContainsHandCard(cardInstance) || targetUnits == null || targetUnits.Count < 1) // 카드와 대상 유효성 확인
        { // 실행 불가 처리 시작
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 실행 불가 처리 종료
        if (cardInstance.EffectType != CardEffectType.Damage) // 지원 효과 확인
        { // 미지원 효과 처리 시작
            Debug.LogError($"[BattleCardActionController] 지원하지 않는 카드 효과입니다: {cardInstance.EffectType}"); // 미지원 효과 출력
            CancelSelection(); // 선택 상태 초기화
            return; // 카드 실행 중단
        } // 미지원 효과 처리 종료
        foreach (BattleUnitRuntime targetUnit in targetUnits) // 대상 유닛 순회
        { // 효과 적용 시작
            targetUnit.TakeDamage(cardInstance.EffectValue); // 카드 피해 적용
        } // 효과 적용 종료
        bool discarded = runtimeDeck.DiscardCard(cardInstance); // 사용 카드를 버린 카드 더미로 이동
        if (!discarded) // 카드 이동 결과 확인
        { // 카드 이동 실패 처리 시작
            Debug.LogError($"[BattleCardActionController] 사용 카드 이동에 실패했습니다: {cardInstance.DisplayName}"); // 이동 실패 출력
        } // 카드 이동 실패 처리 종료
        Debug.Log($"[BattleCardActionController] 카드 사용 완료 - {cardInstance.DisplayName} / 대상 {targetUnits.Count}명 / 피해 {cardInstance.EffectValue}"); // 카드 사용 결과 출력
        CancelSelection(); // 카드 선택 상태 초기화
    } // 카드 실행 종료
    private bool IsValidTarget(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 대상 유효성 검사
    { // 대상 검사 시작
        if (cardInstance == null || targetUnit == null || targetUnit.IsDead) // 카드와 대상 상태 확인
        { // 선택 불가 처리 시작
            return false; // 잘못된 대상 반환
        } // 선택 불가 처리 종료
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
    private static List<BattleUnitRuntime> CollectLivingTargets(IReadOnlyList<BattleUnitView> unitViews) // 생존 대상 목록 생성
    { // 생존 대상 생성 시작
        List<BattleUnitRuntime> targetUnits = new List<BattleUnitRuntime>(); // 빈 대상 목록 생성
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 생존 대상 검사 시작
            if (unitView != null && unitView.RuntimeUnit != null && !unitView.RuntimeUnit.IsDead) // 생존 유닛 확인
            { // 생존 유닛 등록 시작
                targetUnits.Add(unitView.RuntimeUnit); // 생존 대상 목록 등록
            } // 생존 유닛 등록 종료
        } // 생존 대상 검사 종료
        return targetUnits; // 생존 대상 목록 반환
    } // 생존 대상 생성 종료
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
        UnregisterUnitViewEvents(allyUnitViews); // 아군 클릭 이벤트 해제
        UnregisterUnitViewEvents(enemyUnitViews); // 적 클릭 이벤트 해제
        selectedCard = null; // 선택 카드 참조 제거
        ClearTargetHighlights(allyUnitViews); // 아군 대상 강조 해제
        ClearTargetHighlights(enemyUnitViews); // 적 대상 강조 해제
    } // 연결 해제 종료
} // 클래스 종료
