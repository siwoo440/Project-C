using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleHandView : MonoBehaviour // 전투 손패 화면 관리
{ // 클래스 시작
    private readonly List<BattleCardView> spawnedCardViews = new List<BattleCardView>(); // 생성된 카드 화면 목록
    private RectTransform handLayoutRoot; // 손패 가로 배치 영역
    private TMP_Text deckStatusText; // 카드 영역 수량 텍스트
    private TMP_Text turnStatusText; // 라운드와 턴 상태 텍스트
    private Button endTurnButton; // 플레이어 턴 종료 버튼
    private TMP_Text actionPointText; // 공용 행동력 텍스트
    private BattleCardTooltipView tooltipView; // 카드 상세 툴팁 화면
    private BattleDeckRuntime runtimeDeck; // 연결된 런타임 덱
    private BattleActionPointRuntime sharedActionPoints; // 연결된 공용 행동력
    private BattleTurnRuntime turnRuntime; // 연결된 전투 턴 관리자
    private CardInstance selectedCard; // 현재 선택 카드
    private BattleCardView hoveredCardView; // 현재 마우스 진입 카드 화면
    private bool visualStructureCreated; // 손패 화면 구조 생성 여부
    private bool interactionLocked; // 행동 연출 중 입력 잠금 여부
    public BattleDeckRuntime RuntimeDeck => runtimeDeck; // 연결된 런타임 덱 조회
    public BattleActionPointRuntime SharedActionPoints => sharedActionPoints; // 연결된 공용 행동력 조회
    public BattleTurnRuntime TurnRuntime => turnRuntime; // 연결된 전투 턴 관리자 조회
    public event Action<CardInstance> CardClicked; // 손패 카드 클릭 이벤트
    private void Awake() // 손패 화면 준비
    { // 화면 준비 시작
        EnsureVisualStructure(); // 손패 내부 UI 자동 생성
        Refresh(); // 초기 화면 갱신
    } // 화면 준비 종료
    public bool Bind(BattleDeckRuntime battleDeck, BattleActionPointRuntime actionPoints, BattleTurnRuntime battleTurn) // 전투 런타임 상태 연결
    { // 덱 연결 시작
        if (battleDeck == null) // 런타임 덱 누락 확인
        { // 덱 누락 처리 시작
            Debug.LogError("[BattleHandView] 연결할 런타임 덱이 없습니다.", this); // 덱 누락 출력
            return false; // 덱 연결 실패 반환
        } // 덱 누락 처리 종료
        if (actionPoints == null) // 공용 행동력 누락 확인
        { // 행동력 누락 처리 시작
            Debug.LogError("[BattleHandView] 연결할 공용 행동력이 없습니다.", this); // 행동력 누락 출력
            return false; // 화면 연결 실패 반환
        } // 행동력 누락 처리 종료
        if (battleTurn == null) // 전투 턴 관리자 누락 확인
        { // 턴 관리자 누락 처리 시작
            Debug.LogError("[BattleHandView] 연결할 전투 턴 관리자가 없습니다.", this); // 턴 관리자 누락 출력
            return false; // 화면 연결 실패 반환
        } // 턴 관리자 누락 처리 종료
        Unbind(); // 기존 런타임 덱 연결 해제
        runtimeDeck = battleDeck; // 새 런타임 덱 저장
        sharedActionPoints = actionPoints; // 새 공용 행동력 저장
        turnRuntime = battleTurn; // 새 전투 턴 관리자 저장
        runtimeDeck.StateChanged += HandleDeckStateChanged; // 덱 상태 변경 이벤트 등록
        sharedActionPoints.StateChanged += HandleActionPointStateChanged; // 행동력 상태 변경 이벤트 등록
        turnRuntime.StateChanged += HandleTurnStateChanged; // 턴 상태 변경 이벤트 등록
        Refresh(); // 현재 덱 상태 표시
        return true; // 덱 연결 성공 반환
    } // 덱 연결 종료
    public void Unbind() // 런타임 덱 연결 해제
    { // 덱 연결 해제 시작
        if (runtimeDeck != null) // 기존 런타임 덱 확인
        { // 이벤트 해제 시작
            runtimeDeck.StateChanged -= HandleDeckStateChanged; // 덱 상태 변경 이벤트 해제
        } // 이벤트 해제 종료
        if (sharedActionPoints != null) // 기존 공용 행동력 확인
        { // 행동력 이벤트 해제 시작
            sharedActionPoints.StateChanged -= HandleActionPointStateChanged; // 행동력 상태 변경 이벤트 해제
        } // 행동력 이벤트 해제 종료
        if (turnRuntime != null) // 기존 전투 턴 관리자 확인
        { // 턴 이벤트 해제 시작
            turnRuntime.StateChanged -= HandleTurnStateChanged; // 턴 상태 변경 이벤트 해제
        } // 턴 이벤트 해제 종료
        runtimeDeck = null; // 런타임 덱 참조 제거
        sharedActionPoints = null; // 공용 행동력 참조 제거
        turnRuntime = null; // 전투 턴 관리자 참조 제거
        selectedCard = null; // 선택 카드 제거
        HideTooltip(); // 카드 툴팁 숨김
        Refresh(); // 빈 손패 화면 표시
    } // 덱 연결 해제 종료
    private void EnsureVisualStructure() // 손패 내부 UI 준비
    { // 화면 구조 준비 시작
        if (visualStructureCreated) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        Image backgroundImage = GetComponent<Image>(); // 카드 영역 배경 조회
        if (backgroundImage == null) // 카드 영역 배경 누락 확인
        { // 카드 영역 배경 추가 시작
            backgroundImage = gameObject.AddComponent<Image>(); // 카드 영역 배경 추가
        } // 카드 영역 배경 추가 종료
        backgroundImage.color = new Color(0.035f, 0.04f, 0.06f, 0.92f); // 카드 영역 배경 색상 설정
        backgroundImage.raycastTarget = false; // 카드 영역 입력 차단 해제
        deckStatusText = CreateText("DeckStatusText", transform, "Waiting for deck", 20f, Color.white); // 카드 수량 텍스트 생성
        RectTransform statusRect = deckStatusText.rectTransform; // 수량 텍스트 RectTransform 조회
        statusRect.anchorMin = new Vector2(0f, 1f); // 수량 텍스트 최소 앵커 설정
        statusRect.anchorMax = new Vector2(0.42f, 1f); // 수량 텍스트 최대 앵커 설정
        statusRect.pivot = new Vector2(0.5f, 1f); // 수량 텍스트 위쪽 피벗 설정
        statusRect.anchoredPosition = new Vector2(0f, -4f); // 수량 텍스트 위치 설정
        statusRect.sizeDelta = new Vector2(0f, 34f); // 수량 텍스트 높이 설정
        turnStatusText = CreateText("TurnStatusText", transform, "전투 준비", 20f, new Color(1f, 0.82f, 0.35f, 1f)); // 턴 상태 텍스트 생성
        RectTransform turnStatusRect = turnStatusText.rectTransform; // 턴 상태 RectTransform 조회
        turnStatusRect.anchorMin = new Vector2(0.42f, 1f); // 턴 상태 최소 앵커 설정
        turnStatusRect.anchorMax = new Vector2(0.62f, 1f); // 턴 상태 최대 앵커 설정
        turnStatusRect.pivot = new Vector2(0.5f, 1f); // 턴 상태 위쪽 피벗 설정
        turnStatusRect.anchoredPosition = new Vector2(0f, -4f); // 턴 상태 위치 설정
        turnStatusRect.sizeDelta = new Vector2(0f, 34f); // 턴 상태 높이 설정
        endTurnButton = CreateButton("EndTurnButton", transform, "턴 종료"); // 턴 종료 버튼 생성
        RectTransform endTurnRect = endTurnButton.transform as RectTransform; // 턴 종료 RectTransform 조회
        endTurnRect.anchorMin = new Vector2(0.64f, 1f); // 턴 종료 최소 앵커 설정
        endTurnRect.anchorMax = new Vector2(0.78f, 1f); // 턴 종료 최대 앵커 설정
        endTurnRect.pivot = new Vector2(0.5f, 1f); // 턴 종료 위쪽 피벗 설정
        endTurnRect.anchoredPosition = new Vector2(0f, -4f); // 턴 종료 위치 설정
        endTurnRect.sizeDelta = new Vector2(0f, 34f); // 턴 종료 높이 설정
        endTurnButton.onClick.AddListener(HandleEndTurnClicked); // 턴 종료 클릭 이벤트 등록
        actionPointText = CreateText("ActionPointText", transform, "AP -- / --", 24f, new Color(0.45f, 0.8f, 1f, 1f)); // 공용 행동력 텍스트 생성
        RectTransform actionPointRect = actionPointText.rectTransform; // 행동력 텍스트 RectTransform 조회
        actionPointRect.anchorMin = new Vector2(0.8f, 1f); // 행동력 텍스트 최소 앵커 설정
        actionPointRect.anchorMax = new Vector2(1f, 1f); // 행동력 텍스트 최대 앵커 설정
        actionPointRect.pivot = new Vector2(1f, 1f); // 행동력 텍스트 오른쪽 피벗 설정
        actionPointRect.anchoredPosition = new Vector2(-8f, -4f); // 행동력 텍스트 오른쪽 위치 설정
        actionPointRect.sizeDelta = new Vector2(0f, 34f); // 행동력 텍스트 높이 설정
        GameObject handRootObject = new GameObject("HandLayoutRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 손패 배치 오브젝트 생성
        handRootObject.transform.SetParent(transform, false); // 손패 배치 부모 연결
        handLayoutRoot = handRootObject.GetComponent<RectTransform>(); // 손패 배치 RectTransform 조회
        handLayoutRoot.anchorMin = Vector2.zero; // 손패 배치 최소 앵커 설정
        handLayoutRoot.anchorMax = Vector2.one; // 손패 배치 최대 앵커 설정
        handLayoutRoot.pivot = new Vector2(0.5f, 0.5f); // 손패 배치 중앙 피벗 설정
        handLayoutRoot.offsetMin = new Vector2(8f, 8f); // 손패 배치 왼쪽 아래 여백 설정
        handLayoutRoot.offsetMax = new Vector2(-8f, -42f); // 손패 배치 오른쪽 위 여백 설정
        HorizontalLayoutGroup layoutGroup = handRootObject.GetComponent<HorizontalLayoutGroup>(); // 손패 가로 배치 조회
        layoutGroup.padding = new RectOffset(8, 8, 8, 8); // 손패 내부 여백 설정
        layoutGroup.spacing = 12f; // 카드 사이 간격 설정
        layoutGroup.childAlignment = TextAnchor.MiddleCenter; // 카드 중앙 정렬 설정
        layoutGroup.childControlWidth = false; // 카드 너비 자동 제어 해제
        layoutGroup.childControlHeight = false; // 카드 높이 자동 제어 해제
        layoutGroup.childForceExpandWidth = false; // 카드 너비 확장 해제
        layoutGroup.childForceExpandHeight = false; // 카드 높이 확장 해제
        GameObject tooltipObject = new GameObject("CardTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleCardTooltipView)); // 카드 툴팁 오브젝트 생성
        tooltipObject.transform.SetParent(transform, false); // 카드 툴팁 부모 연결
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>(); // 카드 툴팁 RectTransform 조회
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f); // 카드 툴팁 최소 앵커 설정
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f); // 카드 툴팁 최대 앵커 설정
        tooltipRect.pivot = new Vector2(0.5f, 0.5f); // 카드 툴팁 중앙 피벗 설정
        tooltipRect.anchoredPosition = new Vector2(0f, 18f); // 카드 툴팁 카드 영역 내부 위치 설정
        tooltipRect.sizeDelta = new Vector2(420f, 145f); // 카드 툴팁 크기 설정
        tooltipView = tooltipObject.GetComponent<BattleCardTooltipView>(); // 카드 툴팁 화면 조회
        tooltipView.Hide(); // 카드 툴팁 시작 숨김
        visualStructureCreated = true; // 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
    private void Refresh() // 손패 화면 갱신
    { // 화면 갱신 시작
        EnsureVisualStructure(); // 손패 내부 UI 확인
        ClearSpawnedCardViews(); // 기존 카드 화면 제거
        if (runtimeDeck == null) // 런타임 덱 연결 확인
        { // 덱 미연결 처리 시작
            deckStatusText.text = "Waiting for deck"; // 덱 미연결 상태 표시
            actionPointText.text = "AP -- / --"; // 행동력 미연결 상태 표시
            RefreshTurnStatus(); // 전투 준비 상태 표시
            return; // 화면 갱신 중단
        } // 덱 미연결 처리 종료
        deckStatusText.text = $"Deck {runtimeDeck.DrawPileCount} | Hand {runtimeDeck.HandCount}/{runtimeDeck.MaxHandSize} | Discard {runtimeDeck.DiscardPileCount}"; // 카드 영역 수량 표시
        RefreshActionPointStatus(); // 공용 행동력 수량 표시
        RefreshTurnStatus(); // 라운드와 턴 상태 표시
        foreach (CardInstance cardInstance in runtimeDeck.Hand) // 현재 손패 카드 순회
        { // 카드 화면 생성 시작
            BattleCardView cardView = CreateCardView(cardInstance); // 카드 화면 코드 생성
            cardView.SetSelected(cardInstance == selectedCard); // 현재 카드 선택 표시 적용
            cardView.SetInteractable(IsCardUsable(cardInstance)); // 카드 사용 가능 표시 적용
            spawnedCardViews.Add(cardView); // 생성 카드 화면 목록 등록
        } // 카드 화면 생성 종료
    } // 화면 갱신 종료
    private BattleCardView CreateCardView(CardInstance cardInstance) // 카드 화면 코드 생성
    { // 카드 화면 생성 시작
        string objectName = $"Card_{cardInstance.InstanceId}"; // 카드 오브젝트 이름 생성
        GameObject cardObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(BattleCardView)); // 카드 오브젝트 생성
        cardObject.transform.SetParent(handLayoutRoot, false); // 카드 부모 연결
        BattleCardView cardView = cardObject.GetComponent<BattleCardView>(); // 카드 화면 컴포넌트 조회
        cardView.Bind(cardInstance); // 카드 인스턴스 화면 연결
        cardView.Clicked += HandleCardViewClicked; // 카드 클릭 이벤트 등록
        cardView.HoverEntered += HandleCardHoverEntered; // 카드 마우스 진입 이벤트 등록
        cardView.HoverExited += HandleCardHoverExited; // 카드 마우스 이탈 이벤트 등록
        return cardView; // 생성 카드 화면 반환
    } // 카드 화면 생성 종료
    public void SetSelectedCard(CardInstance cardInstance) // 선택 카드 표시 설정
    { // 선택 카드 설정 시작
        selectedCard = cardInstance; // 선택 카드 저장
        foreach (BattleCardView cardView in spawnedCardViews) // 생성 카드 화면 순회
        { // 선택 표시 적용 시작
            bool selected = cardView != null && cardView.RuntimeCard == selectedCard; // 현재 카드 선택 여부 계산
            if (cardView != null) // 카드 화면 존재 확인
            { // 선택 표시 적용 시작
                cardView.SetSelected(selected); // 카드 선택 표시 적용
            } // 선택 표시 적용 종료
        } // 선택 표시 적용 종료
    } // 선택 카드 설정 종료
    public void RefreshCardAvailability() // 카드 사용 가능 상태 갱신
    { // 사용 가능 상태 갱신 시작
        foreach (BattleCardView cardView in spawnedCardViews) // 생성 카드 화면 순회
        { // 사용 가능 상태 적용 시작
            if (cardView != null && cardView.RuntimeCard != null) // 카드 화면 연결 확인
            { // 카드 사용 가능 적용 시작
                cardView.SetInteractable(IsCardUsable(cardView.RuntimeCard)); // 카드 사용 가능 표시 적용
            } // 카드 사용 가능 적용 종료
        } // 사용 가능 상태 적용 종료
    } // 사용 가능 상태 갱신 종료
    public void SetInteractionLocked(bool locked) // 손패와 턴 종료 입력 잠금 설정
    { // 입력 잠금 설정 시작
        if (interactionLocked == locked) // 동일 잠금 상태 확인
        { // 동일 상태 처리 시작
            return; // 입력 잠금 갱신 중단
        } // 동일 상태 처리 종료
        interactionLocked = locked; // 입력 잠금 상태 저장
        if (interactionLocked) // 입력 잠금 확인
        { // 잠금 처리 시작
            HideTooltip(); // 카드 상세 툴팁 숨김
        } // 잠금 처리 종료
        RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
        RefreshTurnStatus(); // 턴 종료 버튼 상태 갱신
    } // 입력 잠금 설정 종료
    private bool IsCardUsable(CardInstance cardInstance) // 카드 사용 가능 여부 확인
    { // 카드 사용 가능 검사 시작
        if (interactionLocked) // 행동 연출 입력 잠금 확인
        { // 입력 잠금 처리 시작
            return false; // 카드 사용 불가 반환
        } // 입력 잠금 처리 종료
        if (cardInstance == null || cardInstance.OwnerUnit == null) // 카드와 소유자 확인
        { // 카드 연결 오류 처리 시작
            return false; // 카드 사용 불가 반환
        } // 카드 연결 오류 처리 종료
        if (cardInstance.OwnerUnit.IsDead || sharedActionPoints == null || turnRuntime == null) // 카드 사용 기본 조건 확인
        { // 카드 사용 조건 오류 처리 시작
            return false; // 카드 사용 불가 반환
        } // 카드 사용 조건 오류 처리 종료
        return turnRuntime.IsPlayerTurn && sharedActionPoints.CanSpend(cardInstance.ApCost); // 턴과 공용 행동력 검사 결과 반환
    } // 카드 사용 가능 검사 종료
    private void ClearSpawnedCardViews() // 생성 카드 화면 제거
    { // 카드 화면 제거 시작
        foreach (BattleCardView cardView in spawnedCardViews) // 생성 카드 화면 순회
        { // 카드 화면 제거 시작
            if (cardView == null) // 제거된 카드 화면 확인
            { // 제거된 화면 처리 시작
                continue; // 다음 카드 화면 이동
            } // 제거된 화면 처리 종료
            cardView.Clicked -= HandleCardViewClicked; // 카드 클릭 이벤트 해제
            cardView.HoverEntered -= HandleCardHoverEntered; // 카드 마우스 진입 이벤트 해제
            cardView.HoverExited -= HandleCardHoverExited; // 카드 마우스 이탈 이벤트 해제
            cardView.gameObject.SetActive(false); // 카드 화면 즉시 숨김
            Destroy(cardView.gameObject); // 카드 화면 오브젝트 제거
        } // 카드 화면 제거 종료
        spawnedCardViews.Clear(); // 생성 카드 화면 목록 비우기
        HideTooltip(); // 카드 툴팁 숨김
    } // 카드 화면 제거 종료
    private void HandleCardViewClicked(BattleCardView cardView) // 카드 화면 클릭 처리
    { // 카드 클릭 처리 시작
        if (interactionLocked || cardView == null || cardView.RuntimeCard == null) // 입력 잠금과 카드 화면 연결 확인
        { // 잘못된 화면 처리 시작
            return; // 카드 클릭 처리 중단
        } // 잘못된 화면 처리 종료
        CardClicked?.Invoke(cardView.RuntimeCard); // 손패 카드 클릭 이벤트 알림
    } // 카드 클릭 처리 종료
    private void HandleCardHoverEntered(BattleCardView cardView) // 카드 마우스 진입 처리
    { // 마우스 진입 처리 시작
        if (interactionLocked || cardView == null || cardView.RuntimeCard == null || tooltipView == null) // 입력 잠금과 카드 연결 확인
        { // 마우스 진입 불가 처리 시작
            return; // 마우스 진입 처리 중단
        } // 마우스 진입 불가 처리 종료
        hoveredCardView = cardView; // 현재 마우스 진입 카드 저장
        tooltipView.Show(cardView.RuntimeCard); // 카드 상세 툴팁 표시
    } // 마우스 진입 처리 종료
    private void HandleCardHoverExited(BattleCardView cardView) // 카드 마우스 이탈 처리
    { // 마우스 이탈 처리 시작
        if (cardView != hoveredCardView) // 현재 마우스 진입 카드 확인
        { // 다른 카드 처리 시작
            return; // 마우스 이탈 처리 중단
        } // 다른 카드 처리 종료
        HideTooltip(); // 카드 상세 툴팁 숨김
    } // 마우스 이탈 처리 종료
    private void HideTooltip() // 카드 툴팁 숨김
    { // 툴팁 숨김 시작
        hoveredCardView = null; // 현재 마우스 진입 카드 제거
        if (tooltipView != null) // 카드 툴팁 존재 확인
        { // 카드 툴팁 처리 시작
            tooltipView.Hide(); // 카드 툴팁 오브젝트 숨김
        } // 카드 툴팁 처리 종료
    } // 툴팁 숨김 종료
    private void HandleDeckStateChanged() // 덱 상태 변경 처리
    { // 덱 상태 처리 시작
        Refresh(); // 손패 화면 자동 갱신
    } // 덱 상태 처리 종료
    private void HandleActionPointStateChanged() // 공용 행동력 변경 처리
    { // 행동력 변경 처리 시작
        RefreshActionPointStatus(); // 행동력 수량 표시 갱신
        RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
    } // 행동력 변경 처리 종료
    private void HandleTurnStateChanged() // 전투 턴 변경 처리
    { // 턴 변경 처리 시작
        RefreshTurnStatus(); // 라운드와 턴 상태 갱신
        RefreshCardAvailability(); // 카드 사용 가능 상태 갱신
        if (turnRuntime == null || !turnRuntime.IsPlayerTurn) // 플레이어 턴 종료 확인
        { // 툴팁 정리 시작
            HideTooltip(); // 카드 툴팁 숨김
        } // 툴팁 정리 종료
    } // 턴 변경 처리 종료
    private void HandleEndTurnClicked() // 턴 종료 버튼 클릭 처리
    { // 턴 종료 클릭 처리 시작
        if (interactionLocked || turnRuntime == null) // 입력 잠금과 턴 관리자 연결 확인
        { // 미연결 처리 시작
            return; // 턴 종료 처리 중단
        } // 미연결 처리 종료
        turnRuntime.EndPlayerTurn(); // 플레이어 턴 종료 요청
    } // 턴 종료 클릭 처리 종료
    private void RefreshTurnStatus() // 라운드와 턴 상태 갱신
    { // 턴 상태 갱신 시작
        if (turnStatusText == null || endTurnButton == null) // 턴 UI 존재 확인
        { // 턴 UI 누락 처리 시작
            return; // 턴 상태 갱신 중단
        } // 턴 UI 누락 처리 종료
        if (turnRuntime == null) // 턴 관리자 연결 확인
        { // 미연결 처리 시작
            turnStatusText.text = "전투 준비"; // 전투 준비 상태 표시
            endTurnButton.interactable = false; // 턴 종료 버튼 비활성화
            return; // 턴 상태 갱신 중단
        } // 미연결 처리 종료
        switch (turnRuntime.CurrentPhase) // 현재 턴 단계 분기
        { // 턴 단계 분기 시작
            case BattleTurnPhase.PlayerTurn: // 플레이어 턴 단계
                turnStatusText.text = $"라운드 {turnRuntime.CurrentRound} | 플레이어 턴"; // 플레이어 턴 표시
                break; // 플레이어 턴 분기 종료
            case BattleTurnPhase.EnemyTurn: // 적 턴 단계
                turnStatusText.text = $"라운드 {turnRuntime.CurrentRound} | 적 턴"; // 적 턴 표시
                break; // 적 턴 분기 종료
            case BattleTurnPhase.Victory: // 승리 단계
                turnStatusText.text = "전투 승리"; // 승리 상태 표시
                break; // 승리 분기 종료
            case BattleTurnPhase.Defeat: // 패배 단계
                turnStatusText.text = "전투 패배"; // 패배 상태 표시
                break; // 패배 분기 종료
            default: // 전투 시작 전 단계
                turnStatusText.text = "전투 준비"; // 전투 준비 상태 표시
                break; // 기본 분기 종료
        } // 턴 단계 분기 종료
        endTurnButton.interactable = turnRuntime.IsPlayerTurn && !interactionLocked; // 플레이어 턴과 입력 잠금 버튼 상태 적용
    } // 턴 상태 갱신 종료
    private void RefreshActionPointStatus() // 공용 행동력 수량 갱신
    { // 행동력 수량 갱신 시작
        if (actionPointText == null) // 행동력 텍스트 존재 확인
        { // 텍스트 누락 처리 시작
            return; // 행동력 수량 갱신 중단
        } // 텍스트 누락 처리 종료
        actionPointText.text = sharedActionPoints == null ? "AP -- / --" : $"AP {sharedActionPoints.CurrentActionPoints} / {sharedActionPoints.MaxActionPoints}"; // 공용 행동력 수량 표시
    } // 행동력 수량 갱신 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string initialText, float fontSize, Color textColor) // 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        text.text = initialText; // 초기 텍스트 설정
        text.fontSize = fontSize; // 글자 크기 설정
        text.color = textColor; // 글자 색상 설정
        text.alignment = TextAlignmentOptions.Center; // 텍스트 중앙 정렬
        text.raycastTarget = false; // 텍스트 입력 차단 해제
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        return text; // 생성 텍스트 반환
    } // 텍스트 생성 종료
    private static Button CreateButton(string objectName, Transform parent, string label) // 공용 버튼 생성
    { // 버튼 생성 시작
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 오브젝트 생성
        buttonObject.transform.SetParent(parent, false); // 버튼 부모 연결
        Image buttonImage = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 조회
        buttonImage.color = new Color(0.2f, 0.32f, 0.5f, 1f); // 버튼 배경 색상 설정
        Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회
        button.targetGraphic = buttonImage; // 버튼 대상 그래픽 설정
        Navigation navigation = button.navigation; // 버튼 이동 설정 조회
        navigation.mode = Navigation.Mode.None; // 키보드 자동 이동 해제
        button.navigation = navigation; // 버튼 이동 설정 적용
        TMP_Text labelText = CreateText("Label", buttonObject.transform, label, 18f, Color.white); // 버튼 글자 생성
        RectTransform labelRect = labelText.rectTransform; // 버튼 글자 RectTransform 조회
        labelRect.anchorMin = Vector2.zero; // 버튼 글자 최소 앵커 설정
        labelRect.anchorMax = Vector2.one; // 버튼 글자 최대 앵커 설정
        labelRect.pivot = new Vector2(0.5f, 0.5f); // 버튼 글자 중앙 피벗 설정
        labelRect.offsetMin = Vector2.zero; // 버튼 글자 왼쪽 아래 여백 제거
        labelRect.offsetMax = Vector2.zero; // 버튼 글자 오른쪽 위 여백 제거
        return button; // 생성 버튼 반환
    } // 버튼 생성 종료
    private void OnDestroy() // 손패 화면 제거 처리
    { // 제거 처리 시작
        if (runtimeDeck != null) // 연결된 런타임 덱 확인
        { // 이벤트 해제 시작
            runtimeDeck.StateChanged -= HandleDeckStateChanged; // 덱 상태 변경 이벤트 해제
        } // 이벤트 해제 종료
        if (sharedActionPoints != null) // 연결된 공용 행동력 확인
        { // 행동력 이벤트 해제 시작
            sharedActionPoints.StateChanged -= HandleActionPointStateChanged; // 행동력 상태 변경 이벤트 해제
        } // 행동력 이벤트 해제 종료
        if (turnRuntime != null) // 연결된 전투 턴 관리자 확인
        { // 턴 이벤트 해제 시작
            turnRuntime.StateChanged -= HandleTurnStateChanged; // 턴 상태 변경 이벤트 해제
        } // 턴 이벤트 해제 종료
        if (endTurnButton != null) // 턴 종료 버튼 확인
        { // 버튼 이벤트 해제 시작
            endTurnButton.onClick.RemoveListener(HandleEndTurnClicked); // 턴 종료 클릭 이벤트 해제
        } // 버튼 이벤트 해제 종료
    } // 제거 처리 종료
} // 클래스 종료
