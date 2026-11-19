using TMPro; // 상품 상세와 금액 표시
using UnityEngine; // 확인 화면 색상과 배치
using UnityEngine.EventSystems; // 확인 화면의 키보드 초점
using UnityEngine.UI; // 구매와 취소 버튼

public sealed partial class ShopPrototypeView // 상점 상품의 구매 전 상세 화면
{ // 구매 확인 화면 시작
    private GameObject confirmationRoot; // 뒷화면을 가리는 입력 차단 영역
    private CanvasGroup shopControls; // 확인 중 비활성화할 상점 목록
    private GameObject previousFocus; // 확인 전 선택했던 버튼
    private ShopOfferData pendingConfirmation; // 결제 전 확인 중인 상품
    private string confirmationRoom; // 확인을 시작한 상점 방
    private int quotedPrice; // 현재 확인 화면에 표시된 가격
    private int pendingOwnerPrice; // 소유자 선택 직전 확인한 카드 가격
    private TMP_Text balancePreview; // 구매 전후 골드
    private TMP_Text salePrice; // 실제 구매 가격
    private TMP_Text purchaseState; // 구매 불가 사유
    private Button confirmPurchase; // 중복 입력을 막을 구매 버튼

    private void OpenPurchaseConfirmation(ShopOfferData offer) // 현재 상품의 그림과 효과 및 결제 정보 표시
    { // 상세 화면 열기 시작
        if (offer == null || pendingConfirmation != null || !overlayObject.activeSelf || manager.IsPurchased(offer)) // 품절과 중복 확인 창 차단
        { // 상세 열기 중단 시작
            return; // 현재 상태 유지
        } // 상세 열기 중단 종료
        pendingConfirmation = offer; // 차감 전에 현재 상품만 보관
        confirmationRoom = manager.ActiveRoomRuntimeId; // 현재 상점 저장
        previousFocus = EventSystem.current?.currentSelectedGameObject; // 취소 후 복귀할 버튼 보관
        shopControls.interactable = false; // 뒤쪽 키보드·마우스 선택 차단
        shopControls.blocksRaycasts = false; // 확인 화면으로 포인터 제한
        Image shade = CreateImage("PurchaseConfirmation", overlayObject.transform, new Color(0.02f, 0.015f, 0.01f, 0.77f)); // 전체 상품판 위의 어두운 배경
        confirmationRoot = shade.gameObject; // 확인 화면 루트 보관
        var modalCanvas = confirmationRoot.AddComponent<Canvas>(); // 파티 초상화 위에 표시할 확인 화면
        modalCanvas.overrideSorting = true; // 뒤쪽 상점 패널의 정렬과 분리
        modalCanvas.sortingOrder = 1700; // 탐사 파티 HUD 위와 로딩 화면 아래 배치
        confirmationRoot.AddComponent<GraphicRaycaster>(); // 어두운 배경과 확인 버튼의 입력 우선 처리
        StretchFull(shade.rectTransform); // 전체 화면에서 뒤쪽 클릭 차단
        Image frame = CreateImage("PurchaseFrame", shade.transform, Color.white); // 금속 테두리 구매 패널
        LobbyUiArt.Apply(frame, "FormationPanel"); // 기존 금 테두리 원화 연결
        var area = shade.rectTransform.rect.size; // 현재 해상도의 사용 가능 영역
        var size = new Vector2(Mathf.Min(1120f, area.x - 48f), Mathf.Min(630f, area.y - 48f)); // 작은 화면 여백과 최대 창 크기
        SetAnchoredRect(frame.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, size, Vector2.zero); // 화면 중앙 구매 패널
        var body = CreateImage("PurchaseBody", frame.transform, new Color(0.105f, 0.072f, 0.053f, 1f)); // 진한 갈색 구매판
        Fit(body.rectTransform, 0.018f, 0.028f, 0.982f, 0.972f); // 금속 테두리 안쪽 배치
        var header = CreateText("PurchaseTitle", body.transform, GetOfferTypeLabel(offer.OfferType) + " 구매", 38f, TextAlignmentOptions.Left, new Color(1f, 0.9f, 0.72f)); // 상품 종류별 구매 창 이름
        Fit(header.rectTransform, 0.04f, 0.84f, 0.53f, 0.98f); // 창 왼쪽 위 제목
        var balanceGroup = new GameObject("Balance", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 금액에 맞춰 오른쪽에 모이는 금화와 숫자
        balanceGroup.transform.SetParent(body.transform, false); // 구매판 제목줄에 배치
        Fit(balanceGroup.GetComponent<RectTransform>(), 0.48f, 0.84f, 0.955f, 0.98f); // 오른쪽 잔액 영역
        var layout = balanceGroup.GetComponent<HorizontalLayoutGroup>(); // 금화와 금액 한 줄 묶음
        layout.childAlignment = TextAnchor.MiddleRight; // 금액 오른쪽 끝 고정
        layout.childControlWidth = true; // 금액 글자와 금화의 실제 폭 적용
        layout.childControlHeight = true; // 숫자와 금화의 실제 높이 적용
        layout.childForceExpandWidth = false; // 금화와 금액의 실제 폭 사용
        layout.childForceExpandHeight = false; // 아이콘과 글자 높이 유지
        layout.spacing = 9f; // 금화와 숫자 사이 여백
        var coin = CreateImage("GoldIcon", balanceGroup.transform, Color.white); // 보유 골드 원화
        LobbyUiArt.Apply(coin, "Gold"); // 기존 금화 그림 사용
        coin.preserveAspect = true; // 금화 원본 비율 유지
        var coinSize = coin.gameObject.AddComponent<LayoutElement>(); // 금화 원화의 표시 크기
        coinSize.preferredWidth = 42f; // 금화 폭
        coinSize.preferredHeight = 42f; // 금화 높이
        balancePreview = CreateText("BalancePreview", balanceGroup.transform, "", 30f, TextAlignmentOptions.Right, new Color(1f, 0.84f, 0.48f)); // 보유 골드와 구매 후 잔액 표시
        balancePreview.textWrappingMode = TextWrappingModes.NoWrap; // 잔액 변화를 한 줄로 표시
        var itemFrame = CreateImage("ItemFrame", body.transform, Color.white); // 정사각 상품 그림판
        BattleUiArt.Apply(itemFrame, "CostBadge"); // 기존 유물 슬롯의 금 테두리
        Fit(itemFrame.rectTransform, 0.044f, 0.343f, 0.326f, 0.817f); // 왼쪽 상품 영역
        itemFrame.preserveAspect = true; // 상품 틀의 정사각 비율 유지
        var icon = CreateImage("ItemIcon", itemFrame.transform, Color.white); // 실제 원본 그림
        icon.sprite = offer.Icon; // 카드와 유물 및 포션의 실제 아이콘
        if (icon.sprite == null && offer.OfferType == ShopOfferType.Card) // 초창기 카드의 전용 원화 누락 확인
        { // 카드 종류 그림 선택 시작
            BattleUiArt.ApplyCardType(icon, offer.Card.CardType); // 실제 카드 속성에 맞는 기존 그림 사용
        } // 카드 종류 그림 선택 종료
        if (icon.sprite == null && offer.OfferType == ShopOfferType.Relic) // 초창기 유물의 전용 아이콘 누락 확인
        { // 유물 종류 그림 선택 시작
            icon.sprite = Resources.Load<Sprite>("UI/Battle/HudRelic"); // 실제 유물 보관함의 기존 종류 아이콘 사용
        } // 유물 종류 그림 선택 종료
        icon.enabled = icon.sprite != null; // 선택한 원화 또는 종류 아이콘 표시
        icon.preserveAspect = true; // 원화 잘림 방지
        Fit(icon.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f); // 그림판 안쪽 원화 배치
        var name = CreateText("ItemName", body.transform, offer.DisplayName, 29f, TextAlignmentOptions.Center, Color.white); // 선택한 상품의 원본 이름
        Fit(name.rectTransform, 0.03f, 0.21f, 0.34f, 0.34f); // 왼쪽 그림 아래 이름
        var detail = CreateImage("DetailPanel", body.transform, new Color(0.061f, 0.04f, 0.029f, 1f)); // 설명을 읽을 짙은 내부 패널
        Fit(detail.rectTransform, 0.36f, 0.22f, 0.956f, 0.82f); // 오른쪽 효과와 가격 표시 영역
        string effect = offer.OfferType == ShopOfferType.Card ? offer.Card.Description : offer.OfferType == ShopOfferType.Relic ? offer.Relic.Description : offer.Potion.Description; // 구매할 실제 원본의 효과 설명
        var description = CreateText("ItemEffect", detail.transform, effect, 29f, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.92f, 0.83f)); // 실제 카드와 유물 및 포션 효과
        description.enableAutoSizing = true; // 긴 설명을 창 안에 맞춤
        description.fontSizeMin = 21f; // 작은 해상도의 본문 크기 하한
        description.fontSizeMax = 29f; // 기본 본문 크기
        Fit(description.rectTransform, 0.055f, 0.37f, 0.955f, 0.92f); // 본문과 가격 사이의 고정 여백
        salePrice = CreateText("PurchasePrice", detail.transform, "", 29f, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.42f)); // 할인 후 실결제 가격
        Fit(salePrice.rectTransform, 0.055f, 0.17f, 0.95f, 0.37f); // 설명 아래 구매가
        purchaseState = CreateText("PurchaseState", detail.transform, "", 23f, TextAlignmentOptions.Left, new Color(1f, 0.64f, 0.53f)); // 결제 불가 사유 또는 소유자 안내
        Fit(purchaseState.rectTransform, 0.055f, 0.02f, 0.96f, 0.17f); // 실제 상태 한 줄 영역
        Button cancel = CreateButton("CancelPurchase", body.transform, "취소", 30f, Color.white); // 결제 없는 취소 버튼
        LobbyUiArt.Apply(cancel.image, "Button"); // 기존 테두리 버튼 원화
        Fit(cancel.GetComponent<RectTransform>(), 0.20f, 0.042f, 0.48f, 0.166f); // 왼쪽 아래 취소
        cancel.onClick.AddListener(CancelPurchase); // 결제 없이 기존 목록 복귀
        confirmPurchase = CreateButton("ConfirmPurchase", body.transform, "구매", 30f, Color.white); // 실제 구입 진행 버튼
        LobbyUiArt.Apply(confirmPurchase.image, "Button"); // 취소와 같은 기존 원화
        Fit(confirmPurchase.GetComponent<RectTransform>(), 0.52f, 0.042f, 0.80f, 0.166f); // 오른쪽 아래 구매
        confirmPurchase.onClick.AddListener(ConfirmSelectedPurchase); // 확인한 상품만 한 번 진행
        PlayerResourceManager.EnsureInstance().ResourcesChanged += RefreshPurchaseConfirmation; // 잔액 변동 시 현재 금액 반영
        RelicRunManager.EnsureInstance().Inventory.Changed += RefreshPurchaseConfirmation; // 보유와 할인 변동 반영
        ConsumableRunManager.EnsureInstance().Inventory.Changed += RefreshPurchaseConfirmation; // 포션 공간 변동 반영
        RefreshPurchaseConfirmation(); // 보관 가능 상태와 실제 금액 초기화
        EventSystem.current?.SetSelectedGameObject(cancel.gameObject); // 키보드와 패드의 확인 창 초점
    } // 상세 화면 열기 종료

    private static void Fit(RectTransform rect, float left, float bottom, float right, float top) // 부모 크기에 맞춘 내부 영역 배치
    { // 상대 배치 시작
        rect.anchorMin = new Vector2(left, bottom); // 아래 왼쪽 경계
        rect.anchorMax = new Vector2(right, top); // 위 오른쪽 경계
        rect.offsetMin = Vector2.zero; // 고정 왼쪽 아래 여백 제거
        rect.offsetMax = Vector2.zero; // 고정 오른쪽 위 여백 제거
    } // 상대 배치 종료

    private void RefreshPurchaseConfirmation() // 현재 재고와 잔액의 실제 구매 가능 상태 갱신
    { // 구매 정보 갱신 시작
        if (pendingConfirmation == null || !manager.IsReady) // 닫혔거나 저장 복원 중인 화면 확인
        { // 정보 갱신 중단 시작
            return; // 미결제 상태 유지
        } // 정보 갱신 중단 종료
        if (confirmationRoom != manager.ActiveRoomRuntimeId) // 다른 상점으로 이동했는지 확인
        { // 다른 방의 확인 취소 시작
            CancelPurchase(); // 이전 방 가격으로 구매 차단
            return; // 이전 상품 갱신 종료
        } // 다른 방의 확인 취소 종료
        quotedPrice = manager.GetOfferPrice(pendingConfirmation); // 현재 방과 할인을 반영한 실결제 가격
        int gold = manager.Gold; // 동시에 비교할 실제 보유 골드
        var result = manager.PreviewPurchase(pendingConfirmation); // 차감 전 현재 재고와 지갑 검증
        confirmPurchase.interactable = result == ShopPurchaseResult.Success; // 결제 가능한 상태에서만 구매 허용
        balancePreview.text = gold < quotedPrice ? $"{gold:N0} → 부족" : $"{gold:N0} → {gold - quotedPrice:N0}"; // 가능할 때의 실제 잔액과 부족 상태
        salePrice.text = $"구매 가격   {quotedPrice:N0} Gold"; // 목록과 같은 최종 금액
        purchaseState.color = result == ShopPurchaseResult.Success ? new Color(0.89f, 0.78f, 0.59f) : new Color(1f, 0.64f, 0.53f); // 공용 카드 안내와 구매 불가 상태 색상 구분
        purchaseState.text = result switch // 결제에 필요한 상태 설명
        { // 상태 문구 선택 시작
            ShopPurchaseResult.Success => pendingConfirmation.RequiresOwnerChoice ? "구매를 누르면 소유자를 선택합니다." : "", // 공용 카드 후속 단계 안내
            ShopPurchaseResult.AlreadyPurchased => "판매 완료", // 판매 기록 상태
            ShopPurchaseResult.InsufficientGold => $"{quotedPrice - gold:N0} Gold 부족", // 실제 모자란 금액
            ShopPurchaseResult.Unavailable when pendingConfirmation.OfferType == ShopOfferType.Potion => "소모품 슬롯이 가득 찼습니다.", // 포션 보관 한도
            ShopPurchaseResult.Unavailable => "이미 보유했거나 판매가 끝난 상품입니다.", // 유물 보유 또는 품절
            _ => "현재 구매할 수 없습니다." // 지급 대상 없음
        }; // 상태 문구 선택 종료
    } // 구매 정보 갱신 종료

    private void ConfirmSelectedPurchase() // 현재 표시 금액을 확인한 상품만 구매 진행
    { // 구매 확인 시작
        ShopOfferData offer = pendingConfirmation; // 한 번만 사용할 상품 참조
        if (offer == null || !confirmPurchase.interactable) // 되풀이된 클릭과 구매 불가능한 상태 확인
        { // 구매 입력 제외 시작
            return; // 결제와 보상 유지
        } // 구매 입력 제외 종료
        int displayedPrice = quotedPrice; // 플레이어가 읽은 가격 보관
        RefreshPurchaseConfirmation(); // 결제 직전 방과 할인 및 잔액 재검사
        if (pendingConfirmation == null || !confirmPurchase.interactable || displayedPrice != quotedPrice) // 구매 불가 또는 방금 바뀐 가격 확인
        { // 확인 갱신 시작
            return; // 변경된 금액을 읽은 후 다시 확인
        } // 확인 갱신 종료
        ClosePurchaseConfirmation(); // 지급 이벤트 전에 중복 입력 잠금
        SelectOffer(offer); // 공용 카드는 소유자 선택 후 결제
    } // 구매 확인 종료

    private void PurchaseForOwner(ShopOfferData offer, CharacterData owner) // 확인한 가격으로 공용 카드 소유자 확정
    { // 소유자 결제 시작
        if (!showingCardOwners || pendingCardOffer != offer) // 닫힌 창의 버튼과 중복 클릭 확인
        { // 취소된 구매 처리 시작
            return; // 기존 덱과 잔액 유지
        } // 취소된 구매 처리 종료
        if (pendingOwnerPrice != manager.GetOfferPrice(offer) || manager.PreviewPurchase(offer) != ShopPurchaseResult.Success) // 소유자 선택 중 가격과 잔액 변경 확인
        { // 구매 조건 변경 시작
            RefreshMainView(); // 오래된 소유자 선택 상태 해제
            OpenPurchaseConfirmation(offer); // 현재 정보로 다시 확인
            return; // 이전의 구매 조건 폐기
        } // 구매 조건 변경 종료
        manager.TryPurchase(offer, owner); // 실제 소유자와 가격으로 한 번만 결제
    } // 소유자 결제 종료

    private void CancelPurchase() // 미결제 상품 확인 취소
    { // 확인 취소 시작
        if (pendingConfirmation == null) // 닫힌 취소 버튼의 중복 입력 확인
        { // 중복 취소 처리 시작
            return; // 현재 화면 그대로 유지
        } // 중복 취소 처리 종료
        string offerName = "Offer_" + pendingConfirmation.OfferId; // 돌아갈 상품 버튼 이름
        float scroll = productScrollRect.verticalNormalizedPosition; // 현재 목록의 스크롤 위치 보관
        ClosePurchaseConfirmation(); // 뒤쪽 상점 입력과 초점 복원
        RefreshMainView(); // 달라진 재고와 표시 가격 반영
        productScrollRect.verticalNormalizedPosition = scroll; // 확인하던 목록 위치 복원
        var original = contentRoot.Find(offerName); // 갱신된 실제 상품 칸 조회
        if (original != null && original.GetComponent<Button>().interactable) // 아직 구매할 수 있는 상품 확인
        { // 원래 초점 복구 시작
            EventSystem.current?.SetSelectedGameObject(original.gameObject); // 취소한 상품으로 키보드 초점 복원
        } // 원래 초점 복구 종료
    } // 확인 취소 종료

    private void ClosePurchaseConfirmation() // 확인 화면과 관찰 중인 재고 연결 정리
    { // 확인 화면 정리 시작
        if (pendingConfirmation == null) // 이미 닫힌 경우 확인
        { // 중복 정리 방지 시작
            return; // 기존 상점 상태 유지
        } // 중복 정리 방지 종료
        pendingConfirmation = null; // 이전 구매 요청 재실행 차단
        if (PlayerResourceManager.Instance != null) // 자원 관리자 생존 확인
        { // 지갑 연결 해제 시작
            PlayerResourceManager.Instance.ResourcesChanged -= RefreshPurchaseConfirmation; // 지갑 이벤트 해제
        } // 지갑 연결 해제 종료
        if (RelicRunManager.Instance != null) // 유물 관리자 생존 확인
        { // 유물 연결 해제 시작
            RelicRunManager.Instance.Inventory.Changed -= RefreshPurchaseConfirmation; // 유물 이벤트 해제
        } // 유물 연결 해제 종료
        if (ConsumableRunManager.Instance != null) // 소모품 관리자 생존 확인
        { // 소모품 연결 해제 시작
            ConsumableRunManager.Instance.Inventory.Changed -= RefreshPurchaseConfirmation; // 슬롯 이벤트 해제
        } // 소모품 연결 해제 종료
        if (shopControls != null) // 복구할 기존 상점 화면 확인
        { // 상점 입력 복원 시작
            shopControls.interactable = true; // 상품과 닫기 버튼 다시 허용
            shopControls.blocksRaycasts = true; // 목록 포인터 입력 복원
        } // 상점 입력 복원 종료
        if (confirmationRoot != null) // 현재 장면의 확인 창 생존 여부
        { // 확인 창 제거 시작
            confirmationRoot.SetActive(false); // 같은 프레임의 재클릭 차단
            Destroy(confirmationRoot); // 확인 창 정리
        } // 확인 창 제거 종료
        EventSystem.current?.SetSelectedGameObject(previousFocus); // 원래 선택 복구
        previousFocus = null; // 이전 버튼 참조 해제
    } // 확인 화면 정리 종료
} // 구매 확인 화면 종료
