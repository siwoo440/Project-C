using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleCardView : MonoBehaviour // 전투 카드 화면 표시
{ // 클래스 시작
    private const float CardWidth = 160f; // 카드 기본 너비
    private const float CardHeight = 220f; // 카드 기본 높이
    private Image artworkImage; // 카드 일러스트 이미지
    private TMP_Text nameText; // 카드 이름 텍스트
    private TMP_Text descriptionText; // 카드 설명 텍스트
    private TMP_Text typeText; // 카드 종류 텍스트
    private TMP_Text ownerText; // 카드 소유자 텍스트
    private TMP_Text costText; // 카드 비용 텍스트
    private bool visualStructureCreated; // 화면 구조 생성 여부
    public CardInstance RuntimeCard { get; private set; } // 연결된 카드 인스턴스
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
        RuntimeCard = cardInstance; // 카드 인스턴스 저장
        nameText.text = RuntimeCard.DisplayName; // 카드 이름 적용
        descriptionText.text = RuntimeCard.SourceData.Description; // 카드 설명 적용
        typeText.text = RuntimeCard.CardType.ToString(); // 카드 종류 적용
        ownerText.text = RuntimeCard.OwnerUnit.DisplayName; // 카드 소유자 적용
        costText.text = RuntimeCard.ApCost.ToString(); // 카드 비용 적용
        artworkImage.sprite = RuntimeCard.Artwork; // 카드 일러스트 적용
        artworkImage.color = RuntimeCard.Artwork != null ? Color.white : new Color(0.22f, 0.22f, 0.26f, 1f); // 일러스트 유무 색상 적용
    } // 카드 연결 종료
    private void EnsureVisualStructure() // 카드 내부 UI 준비
    { // 화면 구조 준비 시작
        if (visualStructureCreated) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        RectTransform rootRect = transform as RectTransform; // 카드 루트 RectTransform 조회
        rootRect.sizeDelta = new Vector2(CardWidth, CardHeight); // 카드 루트 크기 설정
        Image backgroundImage = GetComponent<Image>(); // 카드 배경 이미지 조회
        if (backgroundImage == null) // 카드 배경 누락 확인
        { // 카드 배경 추가 시작
            backgroundImage = gameObject.AddComponent<Image>(); // 카드 배경 이미지 추가
        } // 카드 배경 추가 종료
        backgroundImage.color = new Color(0.08f, 0.09f, 0.13f, 0.98f); // 카드 배경 색상 설정
        backgroundImage.raycastTarget = false; // 카드 배경 입력 차단 해제
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
        typeText = CreateText("TypeText", transform, "Type", 12f, new Color(0.45f, 0.8f, 1f, 1f)); // 카드 종류 생성
        SetCenteredRect(typeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(142f, 20f)); // 카드 종류 위치 설정
        ownerText = CreateText("OwnerText", transform, "Owner", 11f, new Color(0.72f, 0.72f, 0.78f, 1f)); // 카드 소유자 생성
        SetCenteredRect(ownerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(142f, 20f)); // 카드 소유자 위치 설정
        Image costBadgeImage = CreateImage("CostBadge", transform, new Color(0.18f, 0.5f, 0.95f, 1f)); // 카드 비용 배지 생성
        SetCenteredRect(costBadgeImage.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(34f, 34f)); // 카드 비용 배지 위치 설정
        costText = CreateText("CostText", costBadgeImage.transform, "0", 18f, Color.white); // 카드 비용 숫자 생성
        SetStretchRect(costText.rectTransform, 0f, 0f, 0f, 0f); // 카드 비용 숫자 영역 설정
        visualStructureCreated = true; // 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
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
        if (TMP_Settings.defaultFontAsset != null) // 기본 글꼴 존재 확인
        { // 기본 글꼴 적용 시작
            text.font = TMP_Settings.defaultFontAsset; // 기본 글꼴 적용
        } // 기본 글꼴 적용 종료
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
