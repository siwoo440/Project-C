using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleHandView : MonoBehaviour // 전투 손패 화면 관리
{ // 클래스 시작
    private readonly List<BattleCardView> spawnedCardViews = new List<BattleCardView>(); // 생성된 카드 화면 목록
    private readonly List<RectTransform> cardHitRegions = new List<RectTransform>(); // 카드별 독립 입력 영역 목록
    private RectTransform handLayoutRoot; // 손패 가로 배치 영역
    private RectTransform handHitRoot; // 손패 입력 영역 배치
    private float lastLayoutWidth = -1f; // 최근 손패 배치 너비
    private TMP_Text deckStatusText; // 카드 영역 수량 텍스트
    private TMP_Text turnStatusText; // 라운드와 턴 상태 텍스트
    private RectTransform turnStatusFrame; // 화면 위쪽 턴 상태 프레임
    private Button endTurnButton; // 플레이어 턴 종료 버튼
    private Button escapeButton; // 전투 도주 버튼
    private TMP_Text actionPointText; // 공용 행동력 텍스트
    private Image actionPointFill; // 행동력 충전 구체 이미지
    private Image drawPileImage; // 왼쪽 뽑기 카드 더미 이미지
    private Image discardPileImage; // 오른쪽 버리기 카드 더미 이미지
    private static Sprite actionPointFrameSprite; // 공용 행동력 테두리 이미지
    private static Sprite actionPointFillSprite; // 공용 행동력 구체 이미지
    private BattleCardTooltipView tooltipView; // 카드 상세 툴팁 화면
    private BattleCardPreviewView previewView; // 선택 카드 확대 화면
    private BattleDeckRuntime runtimeDeck; // 연결된 런타임 덱
    private BattleActionPointRuntime sharedActionPoints; // 연결된 공용 행동력
    private BattleTurnRuntime turnRuntime; // 연결된 전투 턴 관리자
    private CardInstance selectedCard; // 현재 선택 카드
    private BattleCardView hoveredCardView; // 현재 마우스 진입 카드 화면
    private BattleCardView draggedCardView; // 현재 꺼내는 카드 화면
    private bool visualStructureCreated; // 손패 화면 구조 생성 여부
    private bool interactionLocked; // 행동 연출 중 입력 잠금 여부
    public BattleDeckRuntime RuntimeDeck => runtimeDeck; // 연결된 런타임 덱 조회
    public BattleActionPointRuntime SharedActionPoints => sharedActionPoints; // 연결된 공용 행동력 조회
    public BattleTurnRuntime TurnRuntime => turnRuntime; // 연결된 전투 턴 관리자 조회
    public RectTransform DrawPileAnchor => drawPileImage != null ? drawPileImage.rectTransform : null; // 카드 뽑기 연출 시작 위치 조회
    public RectTransform DiscardPileAnchor => discardPileImage != null ? discardPileImage.rectTransform : null; // 카드 버리기 연출 도착 위치 조회
    public event Action<CardInstance> CardClicked; // 손패 카드 클릭 이벤트
    public event Action EscapeClicked; // 전투 도주 클릭 이벤트
    private void Awake() // 손패 화면 준비
    { // 화면 준비 시작
        EnsureVisualStructure(); // 손패 내부 UI 자동 생성
        Refresh(); // 초기 화면 갱신
    } // 화면 준비 종료
    private void LateUpdate() // 화면 크기 변경 감지
    { // 크기 변경 감지 시작
        if (turnStatusFrame == null) // 턴 상태 프레임 연결 확인
        { // 턴 상태 프레임 연결 시작
            EnsureTurnStatusAtTop(); // 화면 상단에 턴 상태 배치
        } // 턴 상태 프레임 연결 종료
        if (handLayoutRoot != null && !Mathf.Approximately(lastLayoutWidth, handLayoutRoot.rect.width)) // 손패 너비 변경 확인
        { // 손패 재배치 시작
            LayoutHand(); // 변경된 화면 너비 기준 카드 재배치
            RefreshCardPreview(); // 변경된 손패 너비의 확대 카드 갱신
        } // 손패 재배치 종료
        RefreshHoveredTooltip(); // 마우스와 임시 상세 키 상태에 따른 툴팁 갱신
    } // 크기 변경 감지 종료
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
        EnsureTurnStatusAtTop(); // 화면 상단 턴 상태 프레임 연결
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
        BattleUiArt.Apply(backgroundImage, "HandPanel"); // 새로 제작한 손패 배경 이미지 적용
        backgroundImage.raycastTarget = false; // 카드 영역 입력 차단 해제
        deckStatusText = CreateText("DeckStatusText", transform, "Waiting for deck", 20f, Color.white); // 카드 수량 텍스트 생성
        RectTransform statusRect = deckStatusText.rectTransform; // 수량 텍스트 RectTransform 조회
        statusRect.anchorMin = new Vector2(0.05f, 1f); // 수량 텍스트 최소 앵커 설정
        statusRect.anchorMax = new Vector2(0.23f, 1f); // 수량 텍스트 최대 앵커 설정
        statusRect.pivot = new Vector2(0.5f, 1f); // 수량 텍스트 위쪽 피벗 설정
        statusRect.anchoredPosition = new Vector2(0f, -42f); // 손패 테두리 안쪽의 수량 텍스트 위치 설정
        statusRect.sizeDelta = new Vector2(0f, 34f); // 수량 텍스트 높이 설정
        escapeButton = CreateButton("EscapeButton", transform, "도주"); // 전투 도주 버튼 생성
        RectTransform escapeRect = escapeButton.transform as RectTransform; // 도주 RectTransform 조회
        escapeRect.anchorMin = new Vector2(0.75f, 1f); // 도주 최소 앵커 설정
        escapeRect.anchorMax = new Vector2(0.85f, 1f); // 도주 최대 앵커 설정
        escapeRect.pivot = new Vector2(0.5f, 1f); // 도주 위쪽 피벗 설정
        escapeRect.anchoredPosition = new Vector2(0f, -42f); // 손패 테두리 안쪽의 도주 위치 설정
        escapeRect.sizeDelta = new Vector2(0f, 44f); // 도주 높이 설정
        escapeButton.image.color = new Color(0.9f, 0.72f, 0.72f, 1f); // 도주 버튼의 부드러운 붉은 구분 적용
        escapeButton.onClick.AddListener(HandleEscapeClicked); // 도주 클릭 이벤트 등록
        turnStatusText = CreateText("TurnStatusText", transform, "전투 준비", 20f, new Color(1f, 0.82f, 0.35f, 1f)); // 턴 상태 텍스트 생성
        RectTransform turnStatusRect = turnStatusText.rectTransform; // 턴 상태 RectTransform 조회
        turnStatusRect.anchorMin = new Vector2(0.42f, 1f); // 턴 상태 최소 앵커 설정
        turnStatusRect.anchorMax = new Vector2(0.62f, 1f); // 턴 상태 최대 앵커 설정
        turnStatusRect.pivot = new Vector2(0.5f, 1f); // 턴 상태 위쪽 피벗 설정
        turnStatusRect.anchoredPosition = new Vector2(0f, -18f); // 손패 테두리 안쪽의 턴 상태 위치 설정
        turnStatusRect.sizeDelta = new Vector2(0f, 34f); // 턴 상태 높이 설정
        endTurnButton = CreateButton("EndTurnButton", transform, "턴 종료"); // 턴 종료 버튼 생성
        RectTransform endTurnRect = endTurnButton.transform as RectTransform; // 턴 종료 RectTransform 조회
        endTurnRect.anchorMin = new Vector2(0.86f, 1f); // 턴 종료 최소 앵커 설정
        endTurnRect.anchorMax = new Vector2(0.97f, 1f); // 턴 종료 최대 앵커 설정
        endTurnRect.pivot = new Vector2(0.5f, 1f); // 턴 종료 위쪽 피벗 설정
        endTurnRect.anchoredPosition = new Vector2(0f, -42f); // 손패 테두리 안쪽의 턴 종료 위치 설정
        endTurnRect.sizeDelta = new Vector2(0f, 44f); // 턴 종료 높이 설정
        endTurnButton.onClick.AddListener(HandleEndTurnClicked); // 턴 종료 클릭 이벤트 등록
        CreateActionPointOrb(); // 행동력 구체와 중앙 수치 생성
        CreateCardPiles(); // 양쪽 카드 더미 이미지 생성
        GameObject handRootObject = new GameObject("HandLayoutRoot", typeof(RectTransform)); // 손패 배치 오브젝트 생성
        handRootObject.transform.SetParent(transform, false); // 손패 배치 부모 연결
        handLayoutRoot = handRootObject.GetComponent<RectTransform>(); // 손패 배치 RectTransform 조회
        handLayoutRoot.anchorMin = Vector2.zero; // 손패 배치 최소 앵커 설정
        handLayoutRoot.anchorMax = Vector2.one; // 손패 배치 최대 앵커 설정
        handLayoutRoot.pivot = new Vector2(0.5f, 0.5f); // 손패 배치 중앙 피벗 설정
        handLayoutRoot.offsetMin = new Vector2(170f, 8f); // 행동력 구체와 겹치지 않는 왼쪽 여백 설정
        handLayoutRoot.offsetMax = new Vector2(-170f, -92f); // 오른쪽 카드 더미와 겹치지 않는 손패 여백 설정
        GameObject hitRootObject = new GameObject("HandHitRoot", typeof(RectTransform)); // 독립 카드 입력 영역 생성
        hitRootObject.transform.SetParent(transform, false); // 손패 화면 자식 배치
        handHitRoot = hitRootObject.GetComponent<RectTransform>(); // 입력 영역 사각형 조회
        handHitRoot.anchorMin = handLayoutRoot.anchorMin; // 카드 배치와 같은 최소 앵커
        handHitRoot.anchorMax = handLayoutRoot.anchorMax; // 카드 배치와 같은 최대 앵커
        handHitRoot.pivot = handLayoutRoot.pivot; // 카드 배치와 같은 기준점
        handHitRoot.offsetMin = handLayoutRoot.offsetMin; // 카드 배치와 같은 아래 여백
        handHitRoot.offsetMax = handLayoutRoot.offsetMax; // 카드 배치와 같은 위 여백
        GameObject tooltipObject = new GameObject("CardTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleCardTooltipView)); // 카드 툴팁 오브젝트 생성
        tooltipObject.transform.SetParent(transform, false); // 카드 툴팁 부모 연결
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>(); // 카드 툴팁 RectTransform 조회
        tooltipRect.anchorMin = new Vector2(0.5f, 1f); // 카드 툴팁 위쪽 최소 앵커 설정
        tooltipRect.anchorMax = new Vector2(0.5f, 1f); // 카드 툴팁 위쪽 최대 앵커 설정
        tooltipRect.pivot = new Vector2(0.5f, 0f); // 카드 툴팁 아래 중앙 기준점
        tooltipRect.anchoredPosition = new Vector2(0f, 8f); // 손패 위쪽에 상세 설명 배치
        tooltipRect.sizeDelta = new Vector2(420f, 145f); // 카드 툴팁 크기 설정
        tooltipView = tooltipObject.GetComponent<BattleCardTooltipView>(); // 카드 툴팁 화면 조회
        tooltipView.Hide(); // 카드 툴팁 시작 숨김
        GameObject previewObject = new GameObject("CardPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleCardPreviewView)); // 선택 카드 확대 화면 생성
        previewObject.transform.SetParent(transform, false); // 확대 화면을 손패에 연결
        previewView = previewObject.GetComponent<BattleCardPreviewView>(); // 확대 카드 화면 조회
        previewView.Hide(); // 확대 카드 시작 숨김
        escapeButton.transform.SetAsLastSibling(); // 카드 입력 영역보다 도주 버튼 우선 배치
        endTurnButton.transform.SetAsLastSibling(); // 카드 입력 영역보다 턴 종료 버튼 우선 배치
        visualStructureCreated = true; // 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
    private void Refresh() // 손패 화면 갱신
    { // 화면 갱신 시작
        EnsureVisualStructure(); // 손패 내부 UI 확인
        ClearSpawnedCardViews(); // 기존 카드 화면 제거
        RefreshPileImages(); // 실제 덱 상태에 맞춘 카드 더미 표시 갱신
        if (runtimeDeck == null) // 런타임 덱 연결 확인
        { // 덱 미연결 처리 시작
            deckStatusText.text = "Waiting for deck"; // 덱 미연결 상태 표시
            RefreshActionPointStatus(); // 행동력 미연결 상태 표시
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
            cardHitRegions.Add(CreateHitRegion(cardView)); // 카드별 독립 입력 영역 등록
        } // 카드 화면 생성 종료
        LayoutHand(); // 손패 장수와 화면 너비 기준 카드 배치
        RefreshCardPreview(); // 선택 중인 카드 확대 화면 갱신
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
    private RectTransform CreateHitRegion(BattleCardView cardView) // 카드별 독립 입력 영역 생성
    { // 입력 영역 생성 시작
        GameObject hitObject = new GameObject("CardHitRegion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleCardHitRegion)); // 투명 입력 영역 생성
        hitObject.transform.SetParent(handHitRoot, false); // 입력 영역 부모 연결
        Image hitImage = hitObject.GetComponent<Image>(); // 입력 이미지 조회
        hitImage.color = Color.clear; // 입력 영역 투명 표시
        hitImage.raycastTarget = true; // 포인터 입력 허용
        hitObject.GetComponent<CanvasRenderer>().cullTransparentMesh = false; // 투명 입력 영역의 적중 유지
        hitObject.GetComponent<BattleCardHitRegion>().Bind(cardView); // 대응 카드 화면 연결
        return hitObject.GetComponent<RectTransform>(); // 생성된 입력 영역 반환
    } // 입력 영역 생성 종료
    private void LayoutHand() // 손패 카드와 입력 영역 배치
    { // 손패 배치 시작
        if (handLayoutRoot == null || handHitRoot == null) // 배치 영역 준비 확인
        { // 배치 불가 처리 시작
            return; // 손패 배치 중단
        } // 배치 불가 처리 종료
        lastLayoutWidth = handLayoutRoot.rect.width; // 현재 손패 너비 저장
        int count = spawnedCardViews.Count; // 현재 손패 카드 수 조회
        if (count == 0 || cardHitRegions.Count != count || lastLayoutWidth <= 0f) // 카드와 입력 영역 및 화면 너비 확인
        { // 배치 불가 처리 시작
            return; // 손패 배치 중단
        } // 배치 불가 처리 종료
        float availableWidth = Mathf.Max(1f, lastLayoutWidth - 16f); // 양쪽 여백 제외 너비 계산
        const float fanAngle = 7f; // 손패 양쪽 최대 회전 각도
        float radians = fanAngle * Mathf.Deg2Rad; // 회전 범위의 라디안 계산
        float projectedWidth = BattleCardView.CardWidth * Mathf.Cos(radians) + BattleCardView.CardHeight * Mathf.Sin(radians); // 회전한 카드의 최대 가로 너비
        float minimumSpace = count > 1 ? 32f + (count - 1) * 4f : 0f; // 선택 간격과 최소 입력 너비 확보
        float scale = Mathf.Min(1f, availableWidth / (projectedWidth * BattleCardView.HoverScale + minimumSpace)); // 좁은 화면과 다수 카드의 배율 계산
        float width = BattleCardView.CardWidth * scale; // 기본 카드 표시 너비 계산
        float visualWidth = projectedWidth * scale * BattleCardView.HoverScale; // 강조 카드까지 포함한 안전 너비
        float spreadLimit = count > 1 ? 16f * scale : 0f; // 선택 시 양쪽으로 벌릴 최대 간격
        float step = count == 1 ? 0f : Mathf.Min(width * 0.7f, Mathf.Max(0f, (availableWidth - visualWidth - spreadLimit * 2f) / (count - 1))); // 겹친 카드와 벌어짐 여백 계산
        float start = -step * (count - 1) * 0.5f; // 전체 카드 중앙 정렬 시작 위치
        int focusedIndex = spawnedCardViews.IndexOf(draggedCardView != null ? draggedCardView : hoveredCardView); // 꺼내거나 가리키는 카드 순서 조회
        float spread = focusedIndex < 0 ? 0f : Mathf.Min(spreadLimit, Mathf.Max(0f, (availableWidth - visualWidth - step * (count - 1)) * 0.5f)); // 선택 카드 양옆의 안전한 벌어짐 계산
        float bottom = -BattleCardView.CardHeight * scale * 0.5f - 12f * scale; // 부채꼴 아래쪽 입력 위치
        float top = BattleCardView.CardHeight * scale * BattleCardView.HoverScale * 0.5f + BattleCardView.HoverLift * scale; // 강조 카드 위쪽 위치
        for (int index = 0; index < count; index++) // 카드 순서대로 배치
        { // 개별 카드 배치 시작
            float x = start + step * index; // 현재 카드 중앙 위치 계산
            float normalized = count == 1 ? 0f : 2f * index / (count - 1f) - 1f; // 손패 가운데 기준 상대 위치 계산
            float spreadOffset = focusedIndex < 0 || index == focusedIndex ? 0f : index < focusedIndex ? -spread : spread; // 선택 카드 양옆의 바깥 이동량 계산
            float fanHeight = -12f * scale * normalized * normalized; // 양끝 카드가 살짝 낮은 부채꼴 높이
            spawnedCardViews[index].SetHandLayout(new Vector2(x + spreadOffset, fanHeight), scale, -normalized * fanAngle); // 카드 겹침과 부채꼴 회전 적용
            float left = index == 0 ? x - width * 0.5f : x - step * 0.5f; // 카드 입력 영역 왼쪽 경계
            float right = index == count - 1 ? x + width * 0.5f : x + step * 0.5f; // 카드 입력 영역 오른쪽 경계
            RectTransform hitRect = cardHitRegions[index]; // 대응 카드 입력 영역 조회
            hitRect.anchoredPosition = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f); // 독립 입력 영역 중심 위치
            hitRect.sizeDelta = new Vector2(Mathf.Max(0.01f, right - left), top - bottom); // 겹치지 않는 입력 영역 크기
        } // 개별 카드 배치 종료
    } // 손패 배치 종료
    public void SetCardDragging(BattleCardView cardView, bool dragging) // 꺼내는 카드의 주변 간격 설정
    { // 카드 드래그 간격 설정 시작
        if (dragging && cardView != null && spawnedCardViews.Contains(cardView)) // 현재 손패 카드의 드래그 시작 확인
        { // 드래그 카드 저장 시작
            draggedCardView = cardView; // 꺼내는 카드 저장
        } // 드래그 카드 저장 종료
        else if (!dragging && draggedCardView == cardView) // 현재 카드의 드래그 종료 확인
        { // 드래그 카드 해제 시작
            draggedCardView = null; // 꺼낸 카드 참조 제거
        } // 드래그 카드 해제 종료
        LayoutHand(); // 드래그 상태에 맞춘 손패 배치 갱신
        RefreshCardPreview(); // 드래그 상태에 맞춘 확대 카드 갱신
    } // 카드 드래그 간격 설정 종료
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
        RefreshCardPreview(); // 선택 또는 선택 해제한 확대 카드 갱신
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
        RefreshCardPreview(); // 입력 잠금 상태에 맞춘 확대 카드 갱신
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
        if (DevilBloodPrice.IsBloodPriceCard(cardInstance) && !DevilBloodPrice.CanPay(cardInstance)) // 악마 전용 카드의 체력 비용 확인
        { // 체력 비용 부족 처리 시작
            return false; // 체력 비용 카드 사용 차단
        } // 체력 비용 부족 처리 종료
        if (runtimeDeck != null && cardInstance.EffectType == CardEffectType.ChooseDraw && !runtimeDeck.HasDrawableCards) // 생존 소유자의 선택 가능 카드 확인
        { // 선택 후보 없음 처리 시작
            return false; // 선택 드로우 카드 사용 차단
        } // 선택 후보 없음 처리 종료
        if (runtimeDeck != null && cardInstance.EffectType == CardEffectType.ArrangeDrawTop && runtimeDeck.PeekDrawTop(1).Count == 0) // 현재 덱 위 정리 가능 카드 확인
        { // 정리 후보 없음 처리 시작
            return false; // 덱 순서 정리 카드 사용 차단
        } // 정리 후보 없음 처리 종료
        if (runtimeDeck != null && cardInstance.EffectType == CardEffectType.DrawThenDiscard && !runtimeDeck.HasDrawableCards && runtimeDeck.HandCount <= 1) // 드로우와 사용 후 남을 손패 확인
        { // 버릴 손패 없음 처리 시작
            return false; // 드로우 후 버리기 카드 사용 차단
        } // 버릴 손패 없음 처리 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack && !turnRuntime.HasOtherLivingAlly(cardInstance.OwnerUnit)) // 보호할 생존 동료 확인
        { // 보호 대상 없음 처리 시작
            return false; // 동료 보호 카드 사용 차단
        } // 보호 대상 없음 처리 종료
        if (cardInstance.EffectType == CardEffectType.ShareAllyAttack && !turnRuntime.HasOtherLivingAlly(cardInstance.OwnerUnit)) // 피해를 나눌 생존 동료 확인
        { // 분담 대상 없음 처리 시작
            return false; // 피해 분담 카드 사용 차단
        } // 분담 대상 없음 처리 종료
        if (cardInstance.EffectType == CardEffectType.TransferAllyDebuff && !turnRuntime.HasOtherLivingAlly(cardInstance.OwnerUnit)) // 디버프를 넘길 생존 동료 확인
        { // 디버프 이전 대상 없음 시작
            return false; // 디버프 이전 카드 사용 차단
        } // 디버프 이전 대상 없음 종료
        if (runtimeDeck != null && cardInstance.EffectType == CardEffectType.RecoverDiscard && runtimeDeck.DiscardPileCount == 0) // 회수할 버린 카드 확인
        { // 회수 후보 없음 처리 시작
            return false; // 버린 카드 회수 사용 차단
        } // 회수 후보 없음 처리 종료
        if (cardInstance.EffectType == CardEffectType.SelectPartner && !turnRuntime.HasOtherLivingAlly(cardInstance.OwnerUnit)) // 생존 파트너 후보 확인
        { // 파트너 후보 없음 처리 시작
            return false; // 파트너 지정 카드 사용 차단
        } // 파트너 후보 없음 처리 종료
        return turnRuntime.IsPlayerTurn && sharedActionPoints.CanSpend(cardInstance.ApCost); // 턴과 공용 행동력 검사 결과 반환
    } // 카드 사용 가능 검사 종료
    private void ClearSpawnedCardViews() // 생성 카드 화면 제거
    { // 카드 화면 제거 시작
        draggedCardView = null; // 기존 드래그 카드 참조 제거
        previewView?.Hide(); // 기존 카드의 확대 화면 숨김
        foreach (RectTransform hitRegion in cardHitRegions) // 기존 카드 입력 영역 순회
        { // 개별 입력 영역 제거 시작
            if (hitRegion != null) // 입력 영역 존재 확인
            { // 유효 입력 영역 제거 시작
                hitRegion.gameObject.SetActive(false); // 기존 입력 즉시 차단
                Destroy(hitRegion.gameObject); // 기존 입력 영역 제거
            } // 유효 입력 영역 제거 종료
        } // 개별 입력 영역 제거 종료
        cardHitRegions.Clear(); // 입력 영역 목록 초기화
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
        previewView?.Hide(); // 제거된 카드의 확대 화면 최종 숨김
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
        LayoutHand(); // 가리킨 카드 주변 간격 확장
        RefreshCardPreview(); // 가리킨 카드의 확대 화면 표시
        tooltipView.Hide(); // 이전 카드의 상세 툴팁 숨김
        RefreshHoveredTooltip(); // 현재 카드의 상세 표시 조건 적용
    } // 마우스 진입 처리 종료
    private void RefreshHoveredTooltip() // 카드 마우스와 키 상태에 따른 상세 화면 갱신
    { // 카드 상세 갱신 시작
        if (hoveredCardView == null || tooltipView == null) // 현재 카드와 상세 화면 존재 확인
        { // 상세 갱신 불가 처리 시작
            return; // 상세 갱신 중단
        } // 상세 갱신 불가 처리 종료
        bool visible = !interactionLocked && BattleDisplaySettings.ShowTooltip(GraphicsAudioSettingsRuntime.Current, true, BattleDisplaySettings.IsTooltipKeyPressed()); // 설정과 임시 키를 반영한 상세 표시 여부
        if (visible && !tooltipView.gameObject.activeSelf) // 상세 표시 필요 확인
        { // 카드 상세 표시 시작
            tooltipView.Show(hoveredCardView.RuntimeCard); // 현재 카드의 상세 정보 표시
        } // 카드 상세 표시 종료
        else if (!visible && tooltipView.gameObject.activeSelf) // 상세 숨김 필요 확인
        { // 카드 상세 숨김 시작
            tooltipView.Hide(); // 마우스 상태는 보존하고 상세 화면만 숨김
        } // 카드 상세 숨김 종료
    } // 카드 상세 갱신 종료
    private void HandleCardHoverExited(BattleCardView cardView) // 카드 마우스 이탈 처리
    { // 마우스 이탈 처리 시작
        if (cardView != hoveredCardView) // 현재 마우스 진입 카드 확인
        { // 다른 카드 처리 시작
            return; // 마우스 이탈 처리 중단
        } // 다른 카드 처리 종료
        HideTooltip(); // 카드 상세 툴팁 숨김
        LayoutHand(); // 손패의 기본 겹침 간격 복원
        RefreshCardPreview(); // 선택 중인 카드 또는 빈 확대 화면 갱신
    } // 마우스 이탈 처리 종료
    private void RefreshCardPreview() // 현재 카드의 확대 화면 갱신
    { // 확대 화면 갱신 시작
        if (previewView == null) // 확대 화면 존재 확인
        { // 확대 화면 누락 처리 시작
            return; // 확대 화면 갱신 중단
        } // 확대 화면 누락 처리 종료
        if (interactionLocked || draggedCardView != null) // 입력 잠금과 드래그 상태 확인
        { // 확대 화면 숨김 시작
            previewView.Hide(); // 대상 선택을 가리지 않도록 확대 화면 숨김
            return; // 확대 화면 갱신 종료
        } // 확대 화면 숨김 종료
        BattleCardView displayed = hoveredCardView; // 마우스가 가리킨 카드 우선 조회
        if (displayed == null && selectedCard != null) // 마우스가 없고 카드가 선택된 상태 확인
        { // 선택 카드 화면 탐색 시작
            foreach (BattleCardView cardView in spawnedCardViews) // 실제 손패 카드 순회
            { // 선택 카드 확인 시작
                if (cardView != null && cardView.RuntimeCard == selectedCard) // 현재 선택 카드 확인
                { // 선택 카드 저장 시작
                    displayed = cardView; // 실제 선택 카드 화면 저장
                    break; // 선택 카드 탐색 종료
                } // 선택 카드 저장 종료
            } // 선택 카드 확인 종료
        } // 선택 카드 화면 탐색 종료
        if (displayed == null || displayed.RuntimeCard == null) // 표시할 카드 존재 확인
        { // 표시 카드 없음 처리 시작
            previewView.Hide(); // 확대 카드 화면 숨김
            return; // 확대 화면 갱신 종료
        } // 표시 카드 없음 처리 종료
        previewView.Show(displayed.RuntimeCard, displayed.transform as RectTransform, transform as RectTransform); // 실제 카드의 확대 이미지와 전체 설명 표시
        previewView.transform.SetAsLastSibling(); // 확대 카드를 손패 위쪽에 표시
        escapeButton.transform.SetAsLastSibling(); // 도주 버튼을 확대 카드 위에 표시
        endTurnButton.transform.SetAsLastSibling(); // 턴 종료 버튼을 확대 카드 위에 표시
        tooltipView.transform.SetAsLastSibling(); // 카드 상세 툴팁 최상단 표시
    } // 확대 화면 갱신 종료
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
    private void HandleEscapeClicked() // 도주 버튼 클릭 처리
    { // 도주 클릭 처리 시작
        if (interactionLocked || turnRuntime == null || !turnRuntime.CanEscape) // 입력 잠금과 도주 가능 여부 확인
        { // 도주 불가 처리 시작
            return; // 도주 클릭 처리 중단
        } // 도주 불가 처리 종료
        EscapeClicked?.Invoke(); // 전투 도주 요청 알림
    } // 도주 클릭 처리 종료
    private void RefreshTurnStatus() // 라운드와 턴 상태 갱신
    { // 턴 상태 갱신 시작
        if (turnStatusText == null || endTurnButton == null || escapeButton == null) // 턴 UI 존재 확인
        { // 턴 UI 누락 처리 시작
            return; // 턴 상태 갱신 중단
        } // 턴 UI 누락 처리 종료
        if (turnRuntime == null) // 턴 관리자 연결 확인
        { // 미연결 처리 시작
            turnStatusText.text = "전투 준비"; // 전투 준비 상태 표시
            endTurnButton.interactable = false; // 턴 종료 버튼 비활성화
            escapeButton.interactable = false; // 도주 버튼 비활성화
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
            case BattleTurnPhase.Escaped: // 도주 단계
                turnStatusText.text = "전투 도주"; // 도주 상태 표시
                break; // 도주 분기 종료
            default: // 전투 시작 전 단계
                turnStatusText.text = "전투 준비"; // 전투 준비 상태 표시
                break; // 기본 분기 종료
        } // 턴 단계 분기 종료
        endTurnButton.interactable = turnRuntime.IsPlayerTurn && !interactionLocked; // 플레이어 턴과 입력 잠금 버튼 상태 적용
        escapeButton.interactable = turnRuntime.CanEscape && !interactionLocked; // 도주 규칙과 입력 잠금 버튼 상태 적용
    } // 턴 상태 갱신 종료
    private void RefreshActionPointStatus() // 공용 행동력 수량 갱신
    { // 행동력 수량 갱신 시작
        if (actionPointText == null) // 행동력 텍스트 존재 확인
        { // 텍스트 누락 처리 시작
            return; // 행동력 수량 갱신 중단
        } // 텍스트 누락 처리 종료
        actionPointText.text = sharedActionPoints == null ? "AP --/--" : $"AP {sharedActionPoints.CurrentActionPoints}/{sharedActionPoints.MaxActionPoints}"; // 공용 행동력 수량 표시
        if (actionPointFill != null) // 행동력 구체 존재 확인
        { // 행동력 충전량 갱신 시작
            actionPointFill.fillAmount = sharedActionPoints == null ? 0f : (float)sharedActionPoints.CurrentActionPoints / sharedActionPoints.MaxActionPoints; // 실제 행동력 비율 반영
        } // 행동력 충전량 갱신 종료
    } // 행동력 수량 갱신 종료
    private void EnsureTurnStatusAtTop() // 턴 상태를 화면 상단에 배치
    { // 턴 상태 배치 시작
        Canvas canvas = GetComponentInParent<Canvas>(); // 실제 전투 캔버스 조회
        if (canvas == null || turnStatusText == null || turnStatusFrame != null) // 상단 배치 가능 여부 확인
        { // 상단 배치 불가 처리 시작
            return; // 기존 턴 상태 위치 유지
        } // 상단 배치 불가 처리 종료
        GameObject frameObject = new GameObject("TurnStatusFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 턴 상태 배경 생성
        frameObject.transform.SetParent(canvas.transform, false); // 전투 화면 상단에 프레임 연결
        turnStatusFrame = frameObject.GetComponent<RectTransform>(); // 턴 상태 프레임 영역 조회
        turnStatusFrame.anchorMin = new Vector2(0.5f, 1f); // 화면 위 중앙 최소 앵커 설정
        turnStatusFrame.anchorMax = new Vector2(0.5f, 1f); // 화면 위 중앙 최대 앵커 설정
        turnStatusFrame.pivot = new Vector2(0.5f, 1f); // 화면 위 중앙 기준점 설정
        turnStatusFrame.anchoredPosition = new Vector2(0f, -16f); // 기존 레벨 표시 위치 적용
        turnStatusFrame.sizeDelta = new Vector2(360f, 48f); // 기존 레벨 표시 크기 적용
        Image frameImage = frameObject.GetComponent<Image>(); // 턴 상태 프레임 이미지 조회
        LobbyUiArt.Apply(frameImage, "Button"); // 기존 금속 프레임 이미지 적용
        frameImage.raycastTarget = false; // 전투 화면 입력 차단 방지
        turnStatusText.transform.SetParent(frameObject.transform, false); // 실제 턴 상태 텍스트 이동
        RectTransform textRect = turnStatusText.rectTransform; // 턴 상태 글자 영역 조회
        textRect.anchorMin = Vector2.zero; // 턴 글자 최소 앵커 설정
        textRect.anchorMax = Vector2.one; // 턴 글자 최대 앵커 설정
        textRect.offsetMin = Vector2.zero; // 턴 글자 왼쪽 아래 여백 제거
        textRect.offsetMax = Vector2.zero; // 턴 글자 오른쪽 위 여백 제거
    } // 턴 상태 배치 종료
    private void CreateActionPointOrb() // 행동력 구체 화면 생성
    { // 행동력 구체 생성 시작
        GameObject orb = new GameObject("ActionPointOrb", typeof(RectTransform)); // 행동력 구체 영역 생성
        orb.transform.SetParent(transform, false); // 손패 화면에 구체 연결
        RectTransform orbRect = orb.GetComponent<RectTransform>(); // 구체 영역 조회
        orbRect.anchorMin = Vector2.zero; // 구체 왼쪽 아래 최소 앵커 설정
        orbRect.anchorMax = Vector2.zero; // 구체 왼쪽 아래 최대 앵커 설정
        orbRect.pivot = Vector2.zero; // 구체 왼쪽 아래 기준점 설정
        orbRect.anchoredPosition = new Vector2(20f, 10f); // 왼쪽 카드 더미 아래 구체 위치 설정
        orbRect.sizeDelta = new Vector2(190f, 190f); // 구체 크기 설정
        orbRect.localScale = Vector3.one * 0.75f; // 카드 더미와 겹치지 않는 구체 배율 설정
        if (actionPointFillSprite == null) // 행동력 충전 이미지 준비 확인
        { // 충전 이미지 준비 시작
            actionPointFillSprite = LoadActionPointSprite("ActionPointFill"); // 생성한 파란 구체 이미지 조회
        } // 충전 이미지 준비 종료
        if (actionPointFrameSprite == null) // 행동력 테두리 준비 확인
        { // 테두리 이미지 준비 시작
            actionPointFrameSprite = LoadActionPointSprite("ActionPointFrame"); // 생성한 금속 테두리 이미지 조회
        } // 테두리 이미지 준비 종료
        Image empty = CreateOrbImage("APEmpty", orb.transform, actionPointFillSprite, 0.83f); // 비어 있는 구체 배경 생성
        empty.color = new Color(0.12f, 0.16f, 0.2f, 1f); // 비어 있는 행동력의 어두운 색상
        actionPointFill = CreateOrbImage("APFill", orb.transform, actionPointFillSprite, 0.83f); // 행동력 충전 구체 생성
        actionPointFill.type = Image.Type.Filled; // 충전량에 따른 이미지 표시 설정
        actionPointFill.fillMethod = Image.FillMethod.Vertical; // 아래에서 위로 충전 설정
        actionPointFill.fillOrigin = 0; // 아래쪽 충전 시작 설정
        CreateOrbImage("APFrame", orb.transform, actionPointFrameSprite, 1f); // 구체 위쪽 금속 테두리 생성
        actionPointText = CreateText("ActionPointText", orb.transform, "AP -- / --", 24f, Color.white); // 중앙 행동력 글자 생성
        RectTransform textRect = actionPointText.rectTransform; // 행동력 글자 영역 조회
        textRect.anchorMin = Vector2.zero; // 글자 최소 앵커 설정
        textRect.anchorMax = Vector2.one; // 글자 최대 앵커 설정
        textRect.offsetMin = Vector2.zero; // 글자 왼쪽 아래 여백 제거
        textRect.offsetMax = Vector2.zero; // 글자 오른쪽 위 여백 제거
        actionPointText.enableAutoSizing = true; // 긴 행동력 글자의 자동 크기 조정
        actionPointText.fontSizeMin = 14f; // 행동력 글자의 최소 크기 설정
        actionPointText.fontSizeMax = 24f; // 행동력 글자의 최대 크기 설정
        actionPointText.outlineWidth = 0.25f; // 밝은 구체 위 글자 구분
    } // 행동력 구체 생성 종료
    private void CreateCardPiles() // 손패 양쪽의 카드 더미 이미지 생성
    { // 카드 더미 화면 생성 시작
        drawPileImage = CreatePileImage("DrawPile", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 160f), new Vector2(140f, 100f)); // 왼쪽 행동력 구체 위 뽑기 더미 생성
        discardPileImage = CreatePileImage("DiscardPile", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(150f, 150f)); // 오른쪽 아래 묘지 카드 더미 생성
    } // 카드 더미 화면 생성 종료
    private Image CreatePileImage(string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size) // 실제 카드 더미 이미지 생성
    { // 카드 더미 이미지 생성 시작
        GameObject pile = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 카드 더미 이미지 객체 생성
        pile.transform.SetParent(transform, false); // 실제 손패 패널에 카드 더미 연결
        Image image = pile.GetComponent<Image>(); // 카드 더미 이미지 조회
        image.color = new Color(0.15f, 0.18f, 0.2f, 1f); // 이미지 누락 시 대체 색상
        BattleUiArt.Apply(image, name); // 제작한 카드 더미 이미지 적용
        image.preserveAspect = true; // 카드 더미 원본 비율 유지
        image.raycastTarget = false; // 카드 더미의 손패 입력 차단 방지
        RectTransform area = image.rectTransform; // 카드 더미 위치 영역 조회
        area.anchorMin = anchor; // 카드 더미 최소 앵커 설정
        area.anchorMax = anchor; // 카드 더미 최대 앵커 설정
        area.pivot = pivot; // 카드 더미 위치 기준점 설정
        area.anchoredPosition = position; // 카드 더미의 실제 위치 설정
        area.sizeDelta = size; // 카드 더미 이미지 크기 설정
        return image; // 생성한 카드 더미 이미지 반환
    } // 카드 더미 이미지 생성 종료
    private void RefreshPileImages() // 실제 카드 수에 따른 더미 표시 갱신
    { // 카드 더미 표시 갱신 시작
        Color empty = new Color(0.6f, 0.6f, 0.6f, 0.65f); // 카드가 없는 더미의 흐린 표시 색상
        drawPileImage.color = runtimeDeck != null && runtimeDeck.DrawPileCount > 0 ? Color.white : empty; // 실제 뽑을 카드 존재 여부 반영
        discardPileImage.color = runtimeDeck != null && runtimeDeck.DiscardPileCount > 0 ? Color.white : empty; // 실제 버린 카드 존재 여부 반영
    } // 카드 더미 표시 갱신 종료
    private static Sprite LoadActionPointSprite(string imageName) // 생성한 행동력 이미지 조회
    { // 행동력 이미지 조회 시작
        Texture2D texture = Resources.Load<Texture2D>("UI/Battle/" + imageName); // 행동력 텍스처 조회
        if (texture == null) // 행동력 이미지 누락 확인
        { // 이미지 누락 처리 시작
            return null; // 이미지 없는 상태 반환
        } // 이미지 누락 처리 종료
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 전체 이미지 스프라이트 생성
        sprite.hideFlags = HideFlags.HideAndDontSave; // 런타임 공용 이미지 저장 제외
        return sprite; // 생성한 스프라이트 반환
    } // 행동력 이미지 조회 종료
    private static Image CreateOrbImage(string objectName, Transform parent, Sprite sprite, float scale) // 행동력 구체 이미지 생성
    { // 구체 이미지 생성 시작
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 구체 이미지 객체 생성
        imageObject.transform.SetParent(parent, false); // 구체 이미지 부모 연결
        Image image = imageObject.GetComponent<Image>(); // 구체 이미지 조회
        image.sprite = sprite; // 구체 스프라이트 적용
        image.raycastTarget = false; // 카드와 버튼 입력 차단 방지
        RectTransform rect = image.rectTransform; // 구체 이미지 영역 조회
        rect.anchorMin = new Vector2(0.5f, 0.5f); // 구체 중앙 최소 앵커 설정
        rect.anchorMax = new Vector2(0.5f, 0.5f); // 구체 중앙 최대 앵커 설정
        rect.sizeDelta = new Vector2(190f * scale, 190f * scale); // 구체 이미지 크기 설정
        rect.anchoredPosition = Vector2.zero; // 구체 중앙 정렬
        return image; // 생성한 구체 이미지 반환
    } // 구체 이미지 생성 종료
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
        LobbyUiArt.Apply(buttonImage, "Button"); // 기존 제작 금속 버튼 이미지 적용
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
        if (turnStatusFrame != null) // 별도 턴 상태 프레임 확인
        { // 별도 턴 상태 프레임 제거 시작
            Destroy(turnStatusFrame.gameObject); // 손패 제거 시 상단 프레임 정리
        } // 별도 턴 상태 프레임 제거 종료
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
        if (escapeButton != null) // 도주 버튼 확인
        { // 버튼 이벤트 해제 시작
            escapeButton.onClick.RemoveListener(HandleEscapeClicked); // 도주 클릭 이벤트 해제
        } // 버튼 이벤트 해제 종료
    } // 제거 처리 종료
} // 클래스 종료
