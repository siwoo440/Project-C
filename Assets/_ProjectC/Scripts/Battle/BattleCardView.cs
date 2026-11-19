using System; // 기본 이벤트 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // 유니티 포인터 이벤트 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleCardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler // 전투 카드 화면 표시
{ // 클래스 시작
    public const float CardWidth = 160f; // 카드 기본 너비
    public const float CardHeight = 220f; // 카드 기본 높이
    public const float HoverScale = 1.08f; // 카드 마우스 확대 배율
    public const float HoverLift = 24f; // 카드 마우스 진입 상승 높이
    private static readonly Color DefaultBackgroundColor = new Color(0.08f, 0.09f, 0.13f, 0.98f); // 카드 기본 배경 색상
    private static readonly Color SelectedBackgroundColor = new Color(0.72f, 0.45f, 0.08f, 1f); // 카드 선택 배경 색상
    private Image backgroundImage; // 카드 배경 이미지
    private CanvasGroup canvasGroup; // 카드 전체 투명도 제어
    private Image artworkImage; // 카드 일러스트 이미지
    private TMP_Text nameText; // 카드 이름 텍스트
    private TMP_Text descriptionText; // 카드 설명 텍스트
    private Image costBadgeImage; // 카드 종류와 비용 배경 아이콘
    private TMP_Text ownerText; // 카드 소유자 텍스트
    private TMP_Text costText; // 카드 비용 텍스트
    private bool selected; // 카드 선택 여부
    private bool interactable = true; // 카드 사용 가능 여부
    private bool hovered; // 카드 마우스 진입 여부
    private bool pointerInside; // 사용 불가 카드를 포함한 마우스 진입 여부
    private bool visualStructureCreated; // 화면 구조 생성 여부
    private Vector2 layoutPosition; // 손패 안의 기본 카드 위치
    private float layoutScale = 1f; // 화면 크기에 맞춘 기본 배율
    private float layoutAngle; // 부채꼴 배치 회전 각도
    private int originalSiblingIndex = -1; // 강조 전 카드 순서
    public CardInstance RuntimeCard { get; private set; } // 연결된 카드 인스턴스
    public event Action<BattleCardView> Clicked; // 카드 클릭 이벤트
    public event Action<BattleCardView> HoverEntered; // 카드 마우스 진입 이벤트
    public event Action<BattleCardView> HoverExited; // 카드 마우스 이탈 이벤트
    private void Awake() // 카드 화면 준비
    { // 화면 준비 시작
        EnsureVisualStructure(); // 카드 내부 UI 자동 생성
    } // 화면 준비 종료
    public void Bind(CardInstance cardInstance) // 카드 인스턴스 연결
    { // 카드 연결 시작
        if (cardInstance == null) // 카드 누락 확인
        { // 카드 누락 처리 시작
            Debug.LogError("[BattleCardView] 연결할 카드 인스턴스가 없습니다.", this); // 카드 누락 출력
            return; // 카드 연결 중단
        } // 카드 누락 처리 종료
        EnsureVisualStructure(); // 카드 내부 UI 확인
        SetHovered(false); // 카드 확대 상태 초기화
        pointerInside = false; // 이전 카드 마우스 진입 상태 초기화
        RuntimeCard = cardInstance; // 카드 인스턴스 저장
        nameText.text = RuntimeCard.DisplayName; // 카드 이름 적용
        descriptionText.text = RuntimeCard.SourceData.Description; // 카드 설명 적용
        if (!BattleUiArt.ApplyCardType(costBadgeImage, RuntimeCard.CardType)) // 카드 종류 이미지 누락 확인
        { // 기본 비용 배경 복원 시작
            BattleUiArt.Apply(costBadgeImage, "CostBadge"); // 이미지 누락 시 기존 비용 배경 적용
        } // 기본 비용 배경 복원 종료
        ownerText.text = RuntimeCard.OwnerUnit.DisplayName; // 카드 소유자 적용
        costText.text = RuntimeCard.CostLabel; // 카드 비용 적용
        artworkImage.sprite = RuntimeCard.Artwork; // 카드 일러스트 적용
        artworkImage.color = RuntimeCard.Artwork != null ? Color.white : new Color(0.22f, 0.22f, 0.26f, 1f); // 일러스트 유무 색상 적용
    } // 카드 연결 종료
    public void SetSelected(bool selected) // 카드 선택 표시 설정
    { // 선택 표시 시작
        EnsureVisualStructure(); // 카드 내부 UI 확인
        this.selected = selected; // 카드 선택 상태 저장
        RefreshVisualState(); // 카드 상태 화면 갱신
    } // 선택 표시 종료
    public void SetInteractable(bool interactable) // 카드 사용 가능 표시 설정
    { // 사용 가능 표시 시작
        EnsureVisualStructure(); // 카드 내부 UI 확인
        this.interactable = interactable; // 카드 사용 가능 상태 저장
        if (!interactable && hovered) // 비활성 카드 확대 여부 확인
        { // 확대 해제 처리 시작
            SetHovered(false); // 카드 확대 상태 해제
            pointerInside = false; // 이전 카드 마우스 진입 상태 해제
            HoverExited?.Invoke(this); // 카드 마우스 이탈 알림
        } // 확대 해제 처리 종료
        RefreshVisualState(); // 카드 상태 화면 갱신
    } // 사용 가능 표시 종료
    public void SetHandLayout(Vector2 position, float scale, float angle = 0f) // 손패 기준 위치와 배율 및 회전 설정
    { // 손패 위치 설정 시작
        layoutPosition = position; // 기본 카드 위치 저장
        layoutScale = Mathf.Clamp01(scale); // 화면 범위 내 기본 배율 저장
        layoutAngle = angle; // 부채꼴 기준 회전 저장
        ApplyHandLayout(); // 현재 강조 상태 포함 위치 적용
    } // 손패 위치 설정 종료
    public void OnPointerClick(PointerEventData eventData) // 카드 포인터 클릭 처리
    { // 포인터 클릭 처리 시작
        if (RuntimeCard == null || !interactable) // 카드 연결과 사용 가능 상태 확인
        { // 카드 미연결 처리 시작
            return; // 클릭 처리 중단
        } // 카드 미연결 처리 종료
        Clicked?.Invoke(this); // 카드 클릭 이벤트 알림
    } // 포인터 클릭 처리 종료
    public void OnPointerEnter(PointerEventData eventData) // 카드 포인터 진입 처리
    { // 포인터 진입 처리 시작
        if (RuntimeCard == null) // 카드 연결 상태 확인
        { // 포인터 진입 불가 처리 시작
            return; // 포인터 진입 처리 중단
        } // 포인터 진입 불가 처리 종료
        pointerInside = true; // 현재 카드 마우스 진입 저장
        if (interactable) // 실제 사용 가능한 카드 확인
        { // 사용 가능한 카드 강조 시작
            SetHovered(true); // 카드 확대 상태 적용
        } // 사용 가능한 카드 강조 종료
        HoverEntered?.Invoke(this); // 카드 마우스 진입 알림
    } // 포인터 진입 처리 종료
    public void OnPointerExit(PointerEventData eventData) // 카드 포인터 이탈 처리
    { // 포인터 이탈 처리 시작
        if (!hovered && !pointerInside) // 카드 마우스 진입 상태 확인
        { // 확대 없음 처리 시작
            return; // 포인터 이탈 처리 중단
        } // 확대 없음 처리 종료
        pointerInside = false; // 카드 마우스 진입 상태 해제
        SetHovered(false); // 카드 확대 상태 해제
        HoverExited?.Invoke(this); // 카드 마우스 이탈 알림
    } // 포인터 이탈 처리 종료
    private void SetHovered(bool hovered) // 카드 확대 상태 설정
    { // 확대 상태 설정 시작
        if (hovered && !this.hovered) // 새로운 마우스 진입 확인
        { // 카드 최상단 배치 시작
            originalSiblingIndex = transform.GetSiblingIndex(); // 강조 전 카드 순서 저장
            transform.SetAsLastSibling(); // 강조 카드를 다른 카드 위에 배치
        } // 카드 최상단 배치 종료
        else if (!hovered && this.hovered && originalSiblingIndex >= 0) // 기존 강조 해제 확인
        { // 카드 순서 복원 시작
            transform.SetSiblingIndex(originalSiblingIndex); // 강조 전 카드 순서 복원
            originalSiblingIndex = -1; // 저장된 순서 초기화
        } // 카드 순서 복원 종료
        this.hovered = hovered; // 카드 확대 상태 저장
        ApplyHandLayout(); // 기본 위치와 강조 상태 적용
    } // 확대 상태 설정 종료
    private void ApplyHandLayout() // 손패 위치와 강조 상태 적용
    { // 카드 배치 적용 시작
        RectTransform rootRect = transform as RectTransform; // 카드 사각형 조회
        if (rootRect != null) // 사각형 존재 확인
        { // 카드 위치 적용 시작
            rootRect.anchoredPosition = layoutPosition + (hovered ? Vector2.up * HoverLift * layoutScale : Vector2.zero); // 강조 시 카드 위쪽 이동
        } // 카드 위치 적용 종료
        transform.localScale = Vector3.one * layoutScale * (hovered ? HoverScale : 1f); // 화면 크기와 강조 배율 적용
        transform.localRotation = Quaternion.Euler(0f, 0f, hovered ? 0f : layoutAngle); // 강조 카드는 세우고 나머지는 부채꼴 회전
    } // 카드 배치 적용 종료
    private void EnsureVisualStructure() // 카드 내부 UI 준비
    { // 화면 구조 준비 시작
        if (visualStructureCreated) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        RectTransform rootRect = transform as RectTransform; // 카드 루트 RectTransform 조회
        rootRect.sizeDelta = new Vector2(CardWidth, CardHeight); // 카드 루트 크기 설정
        backgroundImage = GetComponent<Image>(); // 카드 배경 이미지 조회
        if (backgroundImage == null) // 카드 배경 누락 확인
        { // 카드 배경 추가 시작
            backgroundImage = gameObject.AddComponent<Image>(); // 카드 배경 이미지 추가
        } // 카드 배경 추가 종료
        backgroundImage.color = DefaultBackgroundColor; // 카드 배경 색상 설정
        BattleUiArt.Apply(backgroundImage, "CardFrame"); // 새로 제작한 전투 카드 프레임 적용
        backgroundImage.raycastTarget = false; // 겹친 카드의 독립 입력 영역 사용
        Image cardFace = CreateImage("CardFace", transform, new Color(0.025f, 0.04f, 0.055f, 0.98f)); // 뒤 카드 내용이 비치지 않는 카드 바탕 생성
        SetStretchRect(cardFace.rectTransform, 6f, 6f, 6f, 6f); // 금속 테두리 안쪽에 카드 바탕 배치
        CanvasGroup faceGroup = cardFace.gameObject.AddComponent<CanvasGroup>(); // 카드 바탕의 독립 투명도 설정 생성
        faceGroup.ignoreParentGroups = true; // 사용 불가 카드도 뒤 카드 내용을 가리도록 설정
        faceGroup.blocksRaycasts = false; // 카드 바탕의 포인터 차단 해제
        faceGroup.interactable = false; // 카드 바탕의 별도 상호작용 해제
        canvasGroup = GetComponent<CanvasGroup>(); // 카드 투명도 컴포넌트 조회
        if (canvasGroup == null) // 카드 투명도 컴포넌트 누락 확인
        { // 투명도 컴포넌트 추가 시작
            canvasGroup = gameObject.AddComponent<CanvasGroup>(); // 카드 투명도 컴포넌트 추가
        } // 투명도 컴포넌트 추가 종료
        canvasGroup.blocksRaycasts = true; // 비활성 카드 클릭 안내 허용
        LayoutElement layoutElement = GetComponent<LayoutElement>(); // 카드 레이아웃 크기 조회
        if (layoutElement == null) // 레이아웃 크기 누락 확인
        { // 레이아웃 크기 추가 시작
            layoutElement = gameObject.AddComponent<LayoutElement>(); // 레이아웃 크기 추가
        } // 레이아웃 크기 추가 종료
        layoutElement.minWidth = CardWidth; // 카드 최소 너비 설정
        layoutElement.minHeight = CardHeight; // 카드 최소 높이 설정
        layoutElement.preferredWidth = CardWidth; // 카드 권장 너비 설정
        layoutElement.preferredHeight = CardHeight; // 카드 권장 높이 설정
        artworkImage = CreateImage("Artwork", transform, new Color(0.22f, 0.22f, 0.26f, 1f)); // 카드 일러스트 영역 생성
        SetCenteredRect(artworkImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -51f), new Vector2(132f, 82f)); // 카드 일러스트 위치 설정
        nameText = CreateText("NameText", transform, "Card Name", 18f, Color.white); // 카드 이름 생성
        SetCenteredRect(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -103f), new Vector2(146f, 24f)); // 카드 이름 위치 설정
        descriptionText = CreateText("DescriptionText", transform, "Card Description", 12f, new Color(0.86f, 0.86f, 0.9f, 1f)); // 카드 설명 생성
        SetCenteredRect(descriptionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(142f, 40f)); // 카드 설명 위치 설정
        ownerText = CreateText("OwnerText", transform, "Owner", 11f, new Color(0.72f, 0.72f, 0.78f, 1f)); // 카드 소유자 생성
        SetCenteredRect(ownerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(142f, 20f)); // 카드 소유자 위치 설정
        costBadgeImage = CreateImage("CostBadge", transform, new Color(0.18f, 0.5f, 0.95f, 1f)); // 카드 비용 배지 생성
        BattleUiArt.Apply(costBadgeImage, "CostBadge"); // 새로 제작한 정사각형 행동력 배지 적용
        SetCenteredRect(costBadgeImage.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(34f, 34f)); // 카드 비용 배지 위치 설정
        costText = CreateText("CostText", costBadgeImage.transform, "0", 18f, Color.white); // 카드 비용 숫자 생성
        costText.outlineWidth = 0.25f; // 밝은 아이콘 위 비용 숫자 외곽선 설정
        costText.outlineColor = Color.black; // 비용 숫자 어두운 외곽선 설정
        SetStretchRect(costText.rectTransform, 0f, 0f, 0f, 0f); // 카드 비용 숫자 영역 설정
        visualStructureCreated = true; // 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
    private void RefreshVisualState() // 카드 상태 화면 갱신
    { // 화면 상태 갱신 시작
        backgroundImage.color = backgroundImage.sprite != null ? selected ? new Color(1f, 0.8f, 0.48f, 1f) : Color.white : selected ? SelectedBackgroundColor : DefaultBackgroundColor; // 제작 이미지 또는 기존 색상의 선택 상태 구분
        canvasGroup.alpha = interactable ? 1f : 0.45f; // 사용 가능 상태 투명도 적용
    } // 화면 상태 갱신 종료
    private static Image CreateImage(string objectName, Transform parent, Color imageColor) // 이미지 생성
    { // 이미지 생성 시작
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(parent, false); // 이미지 부모 연결
        Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
        image.color = imageColor; // 이미지 색상 설정
        image.raycastTarget = false; // 이미지 입력 차단 해제
        return image; // 생성 이미지 반환
    } // 이미지 생성 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string initialText, float fontSize, Color textColor) // 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        text.text = initialText; // 초기 텍스트 설정
        text.fontSize = fontSize; // 글자 크기 설정
        text.color = textColor; // 글자 색상 설정
        text.alignment = TextAlignmentOptions.Center; // 텍스트 중앙 정렬
        text.overflowMode = TextOverflowModes.Ellipsis; // 넘친 텍스트 생략 설정
        text.raycastTarget = false; // 텍스트 입력 차단 해제
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        return text; // 생성 텍스트 반환
    } // 텍스트 생성 종료
    private static void SetCenteredRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta) // 중앙 기준 RectTransform 설정
    { // 중앙 RectTransform 설정 시작
        rectTransform.anchorMin = anchor; // 최소 앵커 설정
        rectTransform.anchorMax = anchor; // 최대 앵커 설정
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        rectTransform.anchoredPosition = anchoredPosition; // 기준 위치 설정
        rectTransform.sizeDelta = sizeDelta; // 크기 설정
    } // 중앙 RectTransform 설정 종료
    private static void SetStretchRect(RectTransform rectTransform, float left, float right, float top, float bottom) // 전체 채움 RectTransform 설정
    { // 전체 채움 설정 시작
        rectTransform.anchorMin = Vector2.zero; // 최소 앵커 설정
        rectTransform.anchorMax = Vector2.one; // 최대 앵커 설정
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        rectTransform.offsetMin = new Vector2(left, bottom); // 왼쪽 아래 여백 설정
        rectTransform.offsetMax = new Vector2(-right, -top); // 오른쪽 위 여백 설정
    } // 전체 채움 설정 종료
} // 클래스 종료
