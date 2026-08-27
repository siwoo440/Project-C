using TMPro; // 상점 한글 텍스트 사용
using UnityEngine; // 상점 Canvas 오브젝트 사용
using UnityEngine.EventSystems; // UI 이벤트 시스템 사용
using UnityEngine.InputSystem.UI; // 신규 입력 시스템 UI 사용
using UnityEngine.UI; // Canvas UI 구성 요소 사용

public sealed class ShopPrototypeView : MonoBehaviour // 54일차 코드 기반 상점 화면
{
    private static readonly Color OverlayColor = new Color(0.02f, 0.025f, 0.035f, 0.94f); // 전체 배경 색상
    private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.12f, 1f); // 상점 패널 색상
    private static readonly Color ButtonColor = new Color(0.18f, 0.22f, 0.28f, 1f); // 기본 버튼 색상
    private static readonly Color AccentColor = new Color(0.82f, 0.64f, 0.24f, 1f); // 골드 강조 색상
    private static readonly Color ImagePlaceholderColor = new Color(0.11f, 0.13f, 0.17f, 1f); // 이미지 미지정 배경 색상
    private ShopRunManager manager; // 연결 상점 관리자
    private GameObject overlayObject; // 전체 상점 오버레이
    private TMP_Text goldText; // 현재 골드 텍스트
    private TMP_Text resultText; // 최근 거래 결과 텍스트
    private RectTransform contentRoot; // 상품 또는 카드 목록 부모
    private ScrollRect productScrollRect; // 상품 목록 스크롤 구성
    private bool showingCardRemoval; // 카드 제거 목록 표시 여부
    private float previousTimeScale = 1f; // 상점 열기 전 시간 배율
    private bool pausedByShop; // 상점 시간 정지 적용 여부

    public static ShopPrototypeView Create(ShopRunManager shopManager) // 상점 화면 코드 생성
    {
        EnsureEventSystem(); // UI 이벤트 시스템 보장
        GameObject canvasObject = new GameObject("Day54ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 상점 Canvas 오브젝트 생성
        Canvas canvas = canvasObject.GetComponent<Canvas>(); // 생성 Canvas 조회
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 방식 지정
        canvas.sortingOrder = 500; // 기존 탐사 HUD보다 위에 표시
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // Canvas 크기 조절기 조회
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 대응 방식 지정
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 지정
        scaler.matchWidthOrHeight = 0.5f; // 가로세로 중간 비율 지정
        ShopPrototypeView view = canvasObject.AddComponent<ShopPrototypeView>(); // 상점 화면 컴포넌트 추가
        view.Build(shopManager); // 상점 화면 구조 생성
        return view; // 생성 상점 화면 반환
    }

    private void Build(ShopRunManager shopManager) // 상점 화면 전체 구조 생성
    {
        manager = shopManager; // 상점 관리자 저장
        manager.Changed += HandleManagerChanged; // 상점 변경 이벤트 연결
        manager.TransactionCompleted += HandleTransactionCompleted; // 거래 결과 이벤트 연결
        CreateOpenButton(); // 상점 열기 버튼 생성
        CreateOverlay(); // 상점 전체 패널 생성
        RefreshMainView(); // 최초 상품 목록 생성
        overlayObject.SetActive(false); // 시작 시 상점 닫기
    }

    private void CreateOpenButton() // 탐사 상점 열기 버튼 생성
    {
        Button openButton = CreateButton("OpenShopButton", transform, "상점 열기", 26f, AccentColor); // 상점 열기 버튼 생성
        RectTransform openRect = openButton.GetComponent<RectTransform>(); // 열기 버튼 위치 정보 조회
        SetAnchoredRect(openRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(260f, 72f), new Vector2(-24f, -24f)); // 오른쪽 위 위치 지정
        openButton.onClick.AddListener(OpenShop); // 상점 열기 동작 연결
    }

    private void CreateOverlay() // 상점 오버레이 생성
    {
        Image overlayImage = CreateImage("ShopOverlay", transform, OverlayColor); // 전체 화면 배경 생성
        overlayObject = overlayImage.gameObject; // 오버레이 오브젝트 저장
        StretchFull(overlayImage.rectTransform); // 전체 화면 채우기
        Image panelImage = CreateImage("ShopPanel", overlayImage.transform, PanelColor); // 중앙 상점 패널 생성
        SetAnchoredRect(panelImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1380f, 900f), Vector2.zero); // 중앙 패널 위치 지정
        TMP_Text titleText = CreateText("Title", panelImage.transform, "Prototype v0.1 상점", 42f, TextAlignmentOptions.Center, AccentColor); // 상점 제목 생성
        SetAnchoredRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, 80f), new Vector2(0f, -24f)); // 제목 위치 지정
        goldText = CreateText("GoldText", panelImage.transform, string.Empty, 30f, TextAlignmentOptions.Left, AccentColor); // 현재 골드 텍스트 생성
        SetAnchoredRect(goldText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(320f, 70f), new Vector2(36f, -28f)); // 골드 위치 지정
        Button debugGoldButton = CreateButton("DebugGoldButton", panelImage.transform, "테스트 Gold +500", 21f, new Color(0.18f, 0.38f, 0.2f, 1f)); // 테스트 골드 버튼 생성
        SetAnchoredRect(debugGoldButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(230f, 60f), new Vector2(-290f, -32f)); // 테스트 골드 버튼 위치 지정
        debugGoldButton.onClick.AddListener(() => manager.AddDebugGold(500)); // 테스트 골드 지급 연결
        Button closeButton = CreateButton("CloseButton", panelImage.transform, "닫기", 23f, new Color(0.45f, 0.16f, 0.16f, 1f)); // 상점 닫기 버튼 생성
        SetAnchoredRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(180f, 60f), new Vector2(-36f, -32f)); // 닫기 버튼 위치 지정
        closeButton.onClick.AddListener(CloseShop); // 상점 닫기 동작 연결
        resultText = CreateText("ResultText", panelImage.transform, "상품을 선택하세요.", 23f, TextAlignmentOptions.Center, Color.white); // 거래 결과 텍스트 생성
        SetAnchoredRect(resultText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 60f), new Vector2(0f, 24f)); // 거래 결과 위치 지정
        CreateScrollArea(panelImage.transform); // 상품 스크롤 영역 생성
    }

    private void CreateScrollArea(Transform parent) // 상품 스크롤 영역 생성
    {
        GameObject scrollObject = new GameObject("ProductScroll", typeof(RectTransform), typeof(ScrollRect)); // 스크롤 오브젝트 생성
        scrollObject.transform.SetParent(parent, false); // 상점 패널 부모 연결
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>(); // 스크롤 위치 정보 조회
        SetAnchoredRect(scrollRectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-100f, -220f), new Vector2(0f, -20f)); // 스크롤 영역 지정
        Image viewportImage = CreateImage("Viewport", scrollObject.transform, new Color(0.04f, 0.045f, 0.06f, 1f)); // 스크롤 화면 배경 생성
        viewportImage.gameObject.AddComponent<RectMask2D>(); // 스크롤 영역 바깥 카드 잘라내기
        StretchFull(viewportImage.rectTransform); // 스크롤 화면 전체 채우기
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // 네 칸 상품 그리드 생성
        contentObject.transform.SetParent(viewportImage.transform, false); // 스크롤 화면 부모 연결
        contentRoot = contentObject.GetComponent<RectTransform>(); // 상품 목록 위치 정보 저장
        contentRoot.anchorMin = new Vector2(0f, 1f); // 목록 왼쪽 위 최소 앵커 지정
        contentRoot.anchorMax = new Vector2(1f, 1f); // 목록 오른쪽 위 최대 앵커 지정
        contentRoot.pivot = new Vector2(0.5f, 1f); // 목록 위쪽 기준점 지정
        contentRoot.anchoredPosition = Vector2.zero; // 목록 시작 위치 지정
        contentRoot.sizeDelta = Vector2.zero; // 목록 추가 여백 초기화
        GridLayoutGroup gridLayout = contentObject.GetComponent<GridLayoutGroup>(); // 상품 그리드 구성 조회
        ConfigureProductGrid(gridLayout); // 한 줄 네 칸 카드 배치 적용
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>(); // 목록 높이 자동 맞춤 구성 조회
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 내용 높이에 맞춤
        productScrollRect = scrollObject.GetComponent<ScrollRect>(); // 스크롤 구성 저장
        productScrollRect.viewport = viewportImage.rectTransform; // 스크롤 화면 연결
        productScrollRect.content = contentRoot; // 스크롤 내용 연결
        productScrollRect.horizontal = false; // 가로 스크롤 비활성화
        productScrollRect.vertical = true; // 세로 스크롤 활성화
        productScrollRect.movementType = ScrollRect.MovementType.Clamped; // 목록 범위 밖 이동 차단
        productScrollRect.scrollSensitivity = 30f; // 마우스 휠 감도 지정
    }

    private void OpenShop() // 상점 열기
    {
        if (!overlayObject.activeSelf) // 신규 상점 열기 확인
        {
            previousTimeScale = Time.timeScale; // 기존 시간 배율 저장
            Time.timeScale = 0f; // 상점 이용 중 탐사 진행 정지
            pausedByShop = true; // 상점 시간 정지 상태 저장
        }

        showingCardRemoval = false; // 상품 목록 모드 지정
        RefreshMainView(); // 최신 상품 목록 갱신
        overlayObject.SetActive(true); // 상점 오버레이 표시
    }

    private void CloseShop() // 상점 닫기
    {
        overlayObject.SetActive(false); // 상점 오버레이 숨김
        RestoreTimeScale(); // 상점 열기 전 시간 배율 복원
    }

    private void RefreshMainView() // 기본 상품 목록 갱신
    {
        if (manager == null || contentRoot == null) // 필수 연결 확인
        {
            return; // 목록 갱신 중단
        }

        showingCardRemoval = false; // 기본 상품 모드 저장
        ClearContent(); // 기존 목록 제거
        goldText.text = $"Gold  {manager.Gold}"; // 현재 골드 표시

        foreach (ShopOfferData offer in manager.Catalog.Offers) // 카탈로그 상품 순회
        {
            ShopOfferData capturedOffer = offer; // 버튼용 현재 상품 보관
            bool sold = manager.IsPurchased(capturedOffer); // 현재 상품 판매 완료 조회
            string typeLabel = GetOfferTypeLabel(capturedOffer.OfferType); // 상품 종류 한글 문구 생성
            Color cardColor = sold ? new Color(0.12f, 0.12f, 0.13f, 1f) : ButtonColor; // 판매 상태별 카드 색상 결정
            Button offerButton = CreateProductCard($"Offer_{capturedOffer.OfferId}", capturedOffer.Icon, typeLabel, capturedOffer.DisplayName, $"{capturedOffer.Price} Gold", typeLabel, sold, cardColor); // 이미지 포함 상품 카드 생성
            offerButton.interactable = !sold; // 판매 완료 상품 비활성화
            offerButton.onClick.AddListener(() => manager.TryPurchase(capturedOffer)); // 상품 구매 동작 연결
        }

        Button removalButton = CreateProductCard("CardRemovalService", null, "서비스", "보유 카드 1장 제거", $"{manager.Catalog.CardRemovalPrice} Gold", "카드\n제거", false, new Color(0.30f, 0.18f, 0.20f, 1f)); // 카드 제거 서비스 카드 생성
        removalButton.onClick.AddListener(ShowCardRemovalList); // 카드 제거 목록 열기 연결
        ResetScrollToTop(); // 상품 목록 시작 위치 복원
    }

    private void ShowCardRemovalList() // 카드 제거 선택 목록 표시
    {
        showingCardRemoval = true; // 카드 제거 모드 저장
        ClearContent(); // 기존 상품 목록 제거
        Button backButton = CreateProductCard("BackToProducts", null, "이동", "상품 목록", "돌아가기", "←", false, AccentColor); // 상품 목록 복귀 카드 생성
        backButton.onClick.AddListener(RefreshMainView); // 상품 목록 복귀 연결

        for (int index = 0; index < manager.RunDeck.CardCount; index++) // 현재 보유 카드 순회
        {
            int capturedIndex = index; // 버튼용 카드 위치 보관
            RunDeckCardEntry entry = manager.RunDeck.Cards[capturedIndex]; // 현재 카드 항목 조회
            string ownerName = entry.Owner != null ? entry.Owner.DisplayName : "소유자 미지정"; // 카드 소유자 이름 결정
            Button cardButton = CreateProductCard($"RemoveCard_{capturedIndex}", entry.Card.Artwork, "카드 제거", $"{entry.Card.DisplayName}\n{ownerName}", $"{manager.Catalog.CardRemovalPrice} Gold", "카드", false, ButtonColor); // 이미지 포함 카드 제거 카드 생성
            cardButton.interactable = manager.RunDeck.CanRemoveAt(capturedIndex); // 최소 한 장 유지 조건 적용
            cardButton.onClick.AddListener(() => RemoveCard(capturedIndex)); // 지정 카드 제거 연결
        }

        ResetScrollToTop(); // 카드 제거 목록 시작 위치 복원
    }

    private void RemoveCard(int cardIndex) // 선택 카드 제거 실행
    {
        ShopPurchaseResult result = manager.TryRemoveCard(cardIndex); // 카드 제거 거래 실행

        if (result == ShopPurchaseResult.Success) // 카드 제거 성공 확인
        {
            ShowCardRemovalList(); // 변경된 카드 목록 다시 생성
        }
    }

    private void HandleManagerChanged() // 상점 상태 변경 처리
    {
        if (goldText != null) // 골드 텍스트 존재 확인
        {
            goldText.text = $"Gold  {manager.Gold}"; // 현재 골드 갱신
        }

        if (overlayObject != null && overlayObject.activeSelf && !showingCardRemoval) // 열린 상품 목록 확인
        {
            RefreshMainView(); // 판매 상태 포함 상품 목록 갱신
        }
    }

    private void HandleTransactionCompleted(ShopPurchaseResult result, string message) // 거래 결과 표시 처리
    {
        if (resultText == null) // 거래 결과 텍스트 누락 확인
        {
            return; // 거래 결과 표시 중단
        }

        resultText.text = message; // 거래 결과 문구 표시
        resultText.color = result == ShopPurchaseResult.Success ? new Color(0.55f, 0.95f, 0.58f, 1f) : new Color(1f, 0.55f, 0.48f, 1f); // 성공과 실패 색상 구분
    }

    public static void ConfigureProductGrid(GridLayoutGroup gridLayout) // 상품 4열 그리드 설정
    {
        if (gridLayout == null) // 그리드 누락 확인
        {
            return; // 설정 중단
        }

        gridLayout.padding = new RectOffset(20, 20, 20, 20); // 그리드 안쪽 여백 지정
        gridLayout.cellSize = new Vector2(286f, 400f); // 세로형 상품 카드 크기 지정
        gridLayout.spacing = new Vector2(20f, 24f); // 카드 사이 겹침 방지 간격 지정
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft; // 왼쪽 위 배치 시작
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal; // 가로 우선 배치 지정
        gridLayout.childAlignment = TextAnchor.UpperCenter; // 그리드 위 중앙 정렬
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 배치 지정
        gridLayout.constraintCount = 4; // 한 줄 네 칸 지정
    }

    private Button CreateProductCard(string objectName, Sprite productSprite, string typeLabel, string productName, string priceLabel, string placeholderLabel, bool sold, Color color) // 이미지 포함 상품 카드 생성
    {
        Image cardImage = CreateImage(objectName, contentRoot, color); // 카드형 사각 배경 생성
        Button cardButton = cardImage.gameObject.AddComponent<Button>(); // 카드 클릭 기능 추가
        cardButton.targetGraphic = cardImage; // 카드 버튼 그래픽 연결
        ConfigureButtonColors(cardButton, color); // 카드 버튼 상태 색상 적용

        Image productImage = CreateImage("ProductImage", cardButton.transform, productSprite != null ? Color.white : ImagePlaceholderColor); // 카드 위쪽 상품 이미지 영역 생성
        SetAnchoredRect(productImage.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-32f, 220f), new Vector2(0f, -18f)); // 상품 이미지 영역 위치 지정
        productImage.sprite = productSprite; // 실제 상품 이미지 연결
        productImage.preserveAspect = true; // 이미지 비율 유지
        productImage.raycastTarget = false; // 이미지 클릭 가로채기 방지

        if (productSprite == null) // 상품 이미지 미지정 확인
        {
            TMP_Text placeholderText = CreateText("ImagePlaceholder", productImage.transform, placeholderLabel, 31f, TextAlignmentOptions.Center, new Color(0.72f, 0.74f, 0.78f, 1f)); // 이미지 대체 문구 생성
            StretchFull(placeholderText.rectTransform); // 대체 문구 이미지 영역 채우기
        }

        Image typeBackground = CreateImage("TypeBackground", cardButton.transform, AccentColor); // 상품 종류 배경 생성
        SetAnchoredRect(typeBackground.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, 34f), new Vector2(18f, -18f)); // 상품 종류 배경 배치
        typeBackground.raycastTarget = false; // 종류 배경 클릭 가로채기 방지
        TMP_Text typeText = CreateText("TypeLabel", cardButton.transform, typeLabel, 18f, TextAlignmentOptions.Center, Color.black); // 상품 종류 문구 생성
        SetAnchoredRect(typeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, 34f), new Vector2(18f, -18f)); // 상품 종류 왼쪽 위 배치

        TMP_Text nameText = CreateText("ProductName", cardButton.transform, productName, 25f, TextAlignmentOptions.Center, Color.white); // 상품 이름 생성
        SetAnchoredRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-28f, 78f), new Vector2(0f, -248f)); // 상품 이름 이미지 아래 배치
        nameText.fontStyle = FontStyles.Bold; // 상품 이름 굵게 표시
        nameText.enableAutoSizing = true; // 긴 상품 이름 자동 축소
        nameText.fontSizeMin = 18f; // 상품 이름 최소 글자 크기 지정
        nameText.fontSizeMax = 25f; // 상품 이름 최대 글자 크기 지정
        nameText.maxVisibleLines = 2; // 상품 이름 최대 두 줄 제한
        nameText.overflowMode = TextOverflowModes.Ellipsis; // 긴 상품 이름 말줄임 표시

        TMP_Text priceText = CreateText("Price", cardButton.transform, priceLabel, 25f, TextAlignmentOptions.Center, AccentColor); // 상품 가격 생성
        SetAnchoredRect(priceText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-28f, 54f), new Vector2(0f, 18f)); // 상품 가격 카드 아래 배치
        priceText.enableAutoSizing = true; // 긴 가격 문구 자동 축소
        priceText.fontSizeMin = 18f; // 가격 최소 글자 크기 지정
        priceText.fontSizeMax = 25f; // 가격 최대 글자 크기 지정
        priceText.maxVisibleLines = 1; // 가격 한 줄 제한
        priceText.overflowMode = TextOverflowModes.Ellipsis; // 긴 가격 문구 말줄임 표시

        if (sold) // 판매 완료 상품 확인
        {
            TMP_Text soldText = CreateText("SoldLabel", cardButton.transform, "판매 완료", 32f, TextAlignmentOptions.Center, new Color(1f, 0.55f, 0.48f, 1f)); // 판매 완료 문구 생성
            SetAnchoredRect(soldText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-32f, 220f), new Vector2(0f, -18f)); // 판매 완료 문구 이미지 위 배치
            soldText.fontStyle = FontStyles.Bold; // 판매 완료 문구 굵게 표시
        }

        return cardButton; // 생성 상품 카드 반환
    }

    private void ResetScrollToTop() // 목록 스크롤 맨 위 복원
    {
        if (productScrollRect == null || contentRoot == null) // 스크롤 연결 확인
        {
            return; // 복원 중단
        }

        Canvas.ForceUpdateCanvases(); // 갱신된 카드 배치 즉시 계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot); // 목록 높이 즉시 재계산
        productScrollRect.StopMovement(); // 기존 스크롤 관성 정지
        productScrollRect.verticalNormalizedPosition = 1f; // 목록 맨 위 위치 지정
    }

    private void ClearContent() // 현재 목록 오브젝트 제거
    {
        for (int index = contentRoot.childCount - 1; index >= 0; index--) // 뒤에서부터 자식 순회
        {
            Transform child = contentRoot.GetChild(index); // 현재 목록 자식 조회
            child.gameObject.SetActive(false); // 지연 제거 전 기존 항목 즉시 숨김
            Destroy(child.gameObject); // 기존 목록 항목 제거
        }
    }

    private static string GetOfferTypeLabel(ShopOfferType offerType) // 상품 종류 한글 문구 조회
    {
        switch (offerType) // 상품 종류 분기
        {
            case ShopOfferType.Card: // 카드 상품 분기
                return "카드"; // 카드 문구 반환
            case ShopOfferType.Relic: // 유물 상품 분기
                return "유물"; // 유물 문구 반환
            case ShopOfferType.Potion: // 포션 상품 분기
                return "포션"; // 포션 문구 반환
            default: // 알 수 없는 상품 분기
                return "상품"; // 기본 상품 문구 반환
        }
    }

    private static Button CreateButton(string objectName, Transform parent, string label, float fontSize, Color color) // 공용 버튼 생성
    {
        Image buttonImage = CreateImage(objectName, parent, color); // 버튼 배경 이미지 생성
        Button button = buttonImage.gameObject.AddComponent<Button>(); // 버튼 기능 추가
        button.targetGraphic = buttonImage; // 버튼 대상 그래픽 연결
        ConfigureButtonColors(button, color); // 버튼 상태 색상 적용
        TMP_Text labelText = CreateText("Label", button.transform, label, fontSize, TextAlignmentOptions.Center, Color.white); // 버튼 한글 글자 생성
        StretchFull(labelText.rectTransform); // 버튼 글자 전체 채우기
        labelText.margin = new Vector4(18f, 8f, 18f, 8f); // 버튼 글자 안쪽 여백 지정
        return button; // 생성 버튼 반환
    }

    private static void ConfigureButtonColors(Button button, Color color) // 버튼 상태 색상 설정
    {
        ColorBlock colors = button.colors; // 버튼 상태 색상 조회
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f); // 마우스 강조 색상 지정
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f); // 누름 색상 지정
        colors.disabledColor = new Color(0.15f, 0.15f, 0.16f, 0.75f); // 비활성 색상 지정
        button.colors = colors; // 버튼 상태 색상 적용
    }

    private static Image CreateImage(string objectName, Transform parent, Color color) // 공용 이미지 생성
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(parent, false); // 이미지 부모 연결
        Image image = imageObject.GetComponent<Image>(); // 이미지 구성 조회
        image.color = color; // 이미지 색상 지정
        return image; // 생성 이미지 반환
    }

    private static TMP_Text CreateText(string objectName, Transform parent, string value, float fontSize, TextAlignmentOptions alignment, Color color) // 공용 TMP 텍스트 생성
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // TMP 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // TMP 텍스트 구성 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 프로젝트 한글 폰트 지정
        text.text = value; // 초기 텍스트 지정
        text.fontSize = fontSize; // 텍스트 크기 지정
        text.alignment = alignment; // 텍스트 정렬 지정
        text.color = color; // 텍스트 색상 지정
        text.raycastTarget = false; // 텍스트 클릭 가로채기 방지
        text.textWrappingMode = TextWrappingModes.Normal; // 긴 한글 줄바꿈 활성화
        return text; // 생성 텍스트 반환
    }

    private static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition) // RectTransform 위치 지정
    {
        rectTransform.anchorMin = anchorMin; // 최소 앵커 지정
        rectTransform.anchorMax = anchorMax; // 최대 앵커 지정
        rectTransform.pivot = pivot; // 기준점 지정
        rectTransform.sizeDelta = sizeDelta; // 크기 지정
        rectTransform.anchoredPosition = anchoredPosition; // 앵커 기준 위치 지정
    }

    private static void StretchFull(RectTransform rectTransform) // 부모 전체 채우기
    {
        rectTransform.anchorMin = Vector2.zero; // 왼쪽 아래 앵커 지정
        rectTransform.anchorMax = Vector2.one; // 오른쪽 위 앵커 지정
        rectTransform.offsetMin = Vector2.zero; // 왼쪽 아래 여백 제거
        rectTransform.offsetMax = Vector2.zero; // 오른쪽 위 여백 제거
    }

    private static void EnsureEventSystem() // UI 이벤트 시스템 보장
    {
        if (FindFirstObjectByType<EventSystem>() != null) // 기존 이벤트 시스템 확인
        {
            return; // 신규 생성 중단
        }

        GameObject eventSystemObject = new GameObject("Day54ShopEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 신규 입력 이벤트 시스템 생성
        DontDestroyOnLoad(eventSystemObject); // Scene 전환 이벤트 시스템 유지
    }

    private void OnDestroy() // 상점 화면 제거 처리
    {
        RestoreTimeScale(); // Scene 종료 중 시간 배율 복원

        if (manager != null) // 상점 관리자 존재 확인
        {
            manager.Changed -= HandleManagerChanged; // 상점 변경 이벤트 해제
            manager.TransactionCompleted -= HandleTransactionCompleted; // 거래 결과 이벤트 해제
        }
    }

    private void RestoreTimeScale() // 상점 시간 정지 복원
    {
        if (!pausedByShop) // 상점 시간 정지 여부 확인
        {
            return; // 시간 배율 복원 중단
        }

        Time.timeScale = previousTimeScale; // 기존 시간 배율 적용
        pausedByShop = false; // 상점 시간 정지 상태 해제
    }
}
