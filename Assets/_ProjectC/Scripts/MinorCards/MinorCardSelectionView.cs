using System; // 기본 인터페이스 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 UI 기본 기능 사용
using UnityEngine.Events; // 카드 선택 버튼 동작 연결
using UnityEngine.UI; // 유니티 UI 기능 사용

public sealed class MinorCardSelectionView : MonoBehaviour, IDisposable // 마이너 카드 레벨업 선택 화면
{
    private BattleMinorCardController controller; // 전투 마이너 카드 관리자
    private PlayerLevelRunManager levelManager; // 플레이어 레벨 관리자
    private GameObject overlayObject; // 전체 화면 입력 차단과 선택 화면
    private RectTransform cardsRoot; // 선택 카드 배치 부모
    private TMP_Text titleText; // 선택 상태 제목
    private TMP_Text pendingText; // 남은 카드 선택 횟수 표시
    private bool disposed; // 화면 종료 여부

    public static MinorCardSelectionView Create(Canvas parentCanvas, BattleMinorCardController battleController, PlayerLevelRunManager playerLevelManager) // 선택 화면 코드 생성
    {
        if (parentCanvas == null) // 부모 Canvas 확인
        {
            throw new ArgumentNullException(nameof(parentCanvas)); // Canvas 누락 예외
        }

        GameObject rootObject = new GameObject("MinorCardSelectionView", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(MinorCardSelectionView)); // 선택 화면 루트 생성
        rootObject.transform.SetParent(parentCanvas.transform, false); // 전투 Canvas 아래 배치
        RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 루트 사각형 조회
        Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 전체 화면 배치

        Canvas selectionCanvas = rootObject.GetComponent<Canvas>(); // 선택 전용 Canvas 조회
        selectionCanvas.overrideSorting = true; // 부모 정렬과 분리
        selectionCanvas.sortingOrder = 600; // 모든 일반 전투 UI 위에 표시

        MinorCardSelectionView view = rootObject.GetComponent<MinorCardSelectionView>(); // 선택 화면 컴포넌트 조회
        view.Initialize(battleController, playerLevelManager); // 화면 연결
        return view; // 생성 화면 반환
    }

    private void Initialize(BattleMinorCardController battleController, PlayerLevelRunManager playerLevelManager) // 선택 화면 초기화
    {
        controller = battleController ?? throw new ArgumentNullException(nameof(battleController)); // 전투 관리자 저장
        levelManager = playerLevelManager ?? throw new ArgumentNullException(nameof(playerLevelManager)); // 레벨 관리자 저장
        CreateVisuals(); // 선택 화면 구조 생성
        controller.StateChanged += Refresh; // 선택지 변경 화면 연결
        levelManager.ProgressChanged += Refresh; // 레벨과 남은 선택권 화면 연결
        Refresh(); // 초기 화면 상태 적용
    }

    private void CreateVisuals() // 선택 화면 UI 생성
    {
        Image blockerImage = CreateImage("Overlay", transform, new Color(0f, 0f, 0f, 0.82f)); // 전체 화면 반투명 입력 차단 배경
        overlayObject = blockerImage.gameObject; // 오버레이 오브젝트 저장
        Stretch(blockerImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 화면 전체 배치
        blockerImage.raycastTarget = true; // 뒤 전투 UI 클릭 차단

        Image panelImage = CreateImage("Panel", overlayObject.transform, new Color(0.035f, 0.10f, 0.12f, 0.98f)); // 중앙 선택 패널 생성
        LobbyUiArt.Apply(panelImage, "FormationPanel"); // 기존 청록색 금속 테두리 패널 적용
        panelImage.pixelsPerUnitMultiplier = 2f; // 선택창 크기에 맞춘 금속 테두리 두께
        panelImage.raycastTarget = false; // 입력 차단을 전체 화면 오버레이에 일임
        RectTransform panelRect = panelImage.rectTransform; // 패널 사각형 조회
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 설정
        panelRect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 설정
        panelRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        panelRect.sizeDelta = new Vector2(1080f, 600f); // 세 선택지와 레벨 정보를 담을 중앙 패널 크기
        panelRect.anchoredPosition = Vector2.zero; // 화면 중앙 배치

        titleText = CreateText("Title", panelRect, 34f, new Color(1f, 0.92f, 0.76f), TextAlignmentOptions.MidlineLeft); // 레벨과 선택 제목 생성
        titleText.fontStyle = FontStyles.Bold; // 화면 제목의 굵은 글자 적용
        Stretch(titleText.rectTransform, Vector2.up, Vector2.one, new Vector2(42f, -88f), new Vector2(-278f, -28f)); // 왼쪽 제목과 오른쪽 횟수 영역 분리
        pendingText = CreateText("PendingCount", panelRect, 22f, new Color(0.77f, 0.87f, 0.86f), TextAlignmentOptions.MidlineRight); // 남은 선택 횟수 생성
        SetRect(pendingText.rectTransform, Vector2.one, Vector2.one, new Vector2(210f, 60f), new Vector2(-42f, -28f), Vector2.one); // 상단 오른쪽 횟수 표시 배치
        Image divider = CreateImage("HeaderDivider", panelRect, new Color(0.63f, 0.48f, 0.26f, 0.88f)); // 제목과 카드 행을 구분하는 금속선 생성
        divider.raycastTarget = false; // 구분선의 전투 입력 영향 제거
        Stretch(divider.rectTransform, Vector2.up, Vector2.one, new Vector2(42f, -98f), new Vector2(-42f, -96f)); // 카드 행 위의 얇은 구분선 배치

        GameObject cardsObject = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 카드 가로 배치 부모 생성
        cardsObject.transform.SetParent(panelImage.transform, false); // 패널 아래 배치
        cardsRoot = cardsObject.GetComponent<RectTransform>(); // 카드 부모 저장
        Stretch(cardsRoot, Vector2.zero, Vector2.one, new Vector2(40f, 32f), new Vector2(-40f, -114f)); // 제목 아래 세 선택지 영역 배치

        HorizontalLayoutGroup layout = cardsObject.GetComponent<HorizontalLayoutGroup>(); // 카드 가로 레이아웃 조회
        layout.spacing = 24f; // 카드 사이 간격
        layout.childAlignment = TextAnchor.MiddleCenter; // 카드 중앙 정렬
        layout.childControlWidth = false; // 카드 폭 자동 제어 해제
        layout.childControlHeight = false; // 카드 높이 자동 제어 해제
        layout.childForceExpandWidth = false; // 카드 폭 확장 해제
        layout.childForceExpandHeight = false; // 카드 높이 확장 해제
    }

    private void Refresh() // 현재 선택 상태 화면 갱신
    {
        if (disposed || overlayObject == null || controller == null) // 화면 갱신 가능 상태 확인
        {
            return;
        }

        overlayObject.SetActive(controller.SelectionActive); // 선택 중일 때만 전체 화면 표시
        if (!controller.SelectionActive) // 선택 비활성 상태 확인
        {
            return;
        }

        titleText.text = $"LEVEL {levelManager.Level} · 마이너 카드 선택"; // 현재 레벨과 카드 선택 제목 표시
        pendingText.text = $"남은 선택 {levelManager.PendingMinorCardChoices}회"; // 실제 남은 선택 횟수 표시
        ClearCards(); // 이전 선택 카드 UI 제거

        for (int index = 0; index < controller.CurrentChoices.Count; index++) // 현재 선택지 순회
        {
            MinorCardData card = controller.CurrentChoices[index]; // 현재 선택 가능한 카드 조회
            CreateCardButton(cardsRoot, card, () => controller.TrySelectCard(card)); // 기존 전투 선택 동작을 카드 버튼에 연결
        }
    }

    internal static Button CreateCardButton(RectTransform parent, MinorCardData cardData, UnityAction select) // 원본 데이터와 선택 동작으로 카드 선택지 생성
    { // 카드 선택지 구성 시작
        Button cardButton = CreateButton($"Minor_{cardData.MinorCardId}", parent, new Color(0.040f, 0.11f, 0.13f, 1f)); // 실제 선택 버튼 생성
        BattleUiArt.Apply(cardButton.image, "CardFrame"); // 기존 전투 카드 원화의 금속 테두리 적용
        ColorBlock buttonColors = cardButton.colors; // 상호작용별 프레임 색상 조회
        buttonColors.highlightedColor = new Color(1.16f, 1.13f, 1.06f, 1f); // 마우스가 가리킨 카드의 프레임 밝기 강조
        buttonColors.selectedColor = buttonColors.highlightedColor; // 키보드가 선택한 카드에 동일 강조 적용
        buttonColors.pressedColor = new Color(0.70f, 0.77f, 0.76f, 1f); // 눌린 카드의 명도 변화 적용
        cardButton.colors = buttonColors; // 상태별 선택 피드백 연결
        RectTransform cardRect = cardButton.GetComponent<RectTransform>(); // 카드 사각형 조회
        cardRect.sizeDelta = new Vector2(304f, 442f); // 제목·그림·설명·효과의 독립 카드 영역
        cardButton.onClick.AddListener(select); // 실제 카드 선택 처리 연결

        TMP_Text nameText = CreateText("Name", cardRect, 28f, new Color(1f, 0.86f, 0.59f, 1f), TextAlignmentOptions.Center); // 카드 상단의 금빛 이름 생성
        nameText.fontStyle = FontStyles.Bold; // 제목과 본문을 구분하는 굵은 글자
        Stretch(nameText.rectTransform, new Vector2(0f, 0.837f), new Vector2(1f, 0.95f), new Vector2(34f, 0f), new Vector2(-34f, 0f)); // 프레임 안쪽의 독립 제목 영역
        nameText.text = cardData.DisplayName; // 카드 이름 적용

        Image iconImage = CreateImage("Icon", cardRect, Color.white); // 배경 없는 카드 원화 영역 생성
        Vector2 artAnchor = new Vector2(0.5f, 0.665f); // 제목 아래의 그림 중앙 위치
        SetRect(iconImage.rectTransform, artAnchor, artAnchor, new Vector2(136f, 136f), Vector2.zero, new Vector2(0.5f, 0.5f)); // 본문과 떨어진 그림 표시 영역
        iconImage.sprite = cardData.Icon; // 카드에 지정된 원화 우선 적용
        iconImage.enabled = iconImage.sprite != null || BattleUiArt.Apply(iconImage, GetFallbackIcon(cardData.EffectType)); // 원화가 없을 때 효과 종류에 대응하는 기존 그림 적용
        iconImage.preserveAspect = true; // 카드 원화와 대체 그림의 원본 비율 유지
        iconImage.raycastTarget = false; // 그림 위에서도 카드 버튼 클릭 허용

        TMP_Text descriptionText = CreateText("Description", cardRect, 23f, new Color(0.91f, 0.94f, 0.92f), TextAlignmentOptions.Center); // 카드의 독립 상세 설명 생성
        Stretch(descriptionText.rectTransform, new Vector2(0f, 0.205f), new Vector2(1f, 0.49f), new Vector2(28f, 0f), new Vector2(-28f, 0f)); // 그림 아래와 효과 위의 넉넉한 설명 영역
        descriptionText.text = cardData.Description; // 원본 카드 설명 문구 적용
        descriptionText.textWrappingMode = TextWrappingModes.Normal; // 설명 너비에 따른 한국어 줄바꿈

        bool targetsAllies = cardData.TargetType == MinorCardTargetType.AllAllies; // 대상에 따른 효과 영역의 색상 선택
        Image effectBand = CreateImage("EffectBand", cardRect, targetsAllies ? new Color(0.065f, 0.23f, 0.20f, 1f) : new Color(0.25f, 0.10f, 0.13f, 1f)); // 아군 강화 또는 적 약화 요약 바탕
        Stretch(effectBand.rectTransform, new Vector2(0f, 0.075f), new Vector2(1f, 0.174f), new Vector2(35f, 0f), new Vector2(-35f, 0f)); // 하단 장식 안쪽 효과 바탕 배치
        effectBand.raycastTarget = false; // 효과 영역 위 카드 선택 클릭 허용
        Color effectColor = targetsAllies ? new Color(0.76f, 0.95f, 0.85f) : new Color(1f, 0.80f, 0.79f); // 배경 위에서 읽기 쉬운 대상별 효과 글자색
        TMP_Text effectText = CreateText("Effect", cardRect, 18f, effectColor, TextAlignmentOptions.Center); // 수치와 대상을 표시하는 효과 요약 생성
        effectText.fontStyle = FontStyles.Bold; // 독립 효과 수치 강조
        Stretch(effectText.rectTransform, new Vector2(0f, 0.075f), new Vector2(1f, 0.174f), new Vector2(42f, 3f), new Vector2(-42f, -3f)); // 효과 바탕 안쪽의 수치 영역
        effectText.text = GetEffectLabel(cardData); // 효과 요약 적용
        return cardButton; // 생성된 실제 카드 선택 버튼 반환
    } // 카드 선택지 구성 종료

    private static string GetFallbackIcon(MinorCardEffectType effect) // 지정 원화가 없을 때 의미가 맞는 기존 전투 이미지 선택
    { // 대체 원화 선택 시작
        return effect switch // 적용되는 효과의 종류 확인
        { // 효과별 원화 분기 시작
            MinorCardEffectType.IncreaseMaxHealth => "HealthGaugeIcon", // 최대 체력 효과의 하트 원화
            MinorCardEffectType.AttackPowerUp => "CardTypeSword", // 공격 강화 효과의 검 원화
            MinorCardEffectType.PhysicalDefenseUp or MinorCardEffectType.PhysicalDefenseDown => "CardTypeShield", // 물리 방어 변화의 방패 원화
            MinorCardEffectType.MagicalResistanceUp or MinorCardEffectType.MagicalResistanceDown => "CardTypeWand", // 마법 저항 변화의 완드 원화
            _ => "CardTypePentacle" // 추가 효과 종류에 사용할 공용 펜타클 원화
        }; // 효과별 원화 분기 종료
    } // 대체 원화 선택 종료

    private static string GetEffectLabel(MinorCardData cardData) // 카드 효과 표시 문구 생성
    {
        string targetLabel = cardData.TargetType == MinorCardTargetType.AllAllies ? "모든 아군" : "모든 적"; // 대상 문구 결정
        string effectLabel;

        switch (cardData.EffectType) // 효과 문구 분기
        {
            case MinorCardEffectType.IncreaseMaxHealth:
                effectLabel = $"최대 체력 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.AttackPowerUp:
                effectLabel = $"공격력 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.PhysicalDefenseUp:
                effectLabel = $"물리 방어 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.PhysicalDefenseDown:
                effectLabel = $"물리 방어 -{cardData.EffectValue}";
                break;
            case MinorCardEffectType.MagicalResistanceUp:
                effectLabel = $"마법 저항 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.MagicalResistanceDown:
                effectLabel = $"마법 저항 -{cardData.EffectValue}";
                break;
            default:
                effectLabel = "효과 없음";
                break;
        }

        return $"{targetLabel} · {effectLabel}"; // 대상과 효과 결합 반환
    }

    private void ClearCards() // 기존 선택 카드 버튼 제거
    {
        if (cardsRoot == null) // 카드 부모 생성 여부 확인
        {
            return;
        }

        for (int index = cardsRoot.childCount - 1; index >= 0; index--) // 기존 카드 역순 순회
        {
            GameObject childObject = cardsRoot.GetChild(index).gameObject; // 카드 오브젝트 조회
            childObject.SetActive(false); // 레이아웃에서 즉시 제외
            Destroy(childObject); // 카드 버튼 제거
        }
    }

    private static Image CreateImage(string objectName, Transform parent, Color color) // 기본 이미지 생성
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(parent, false); // 부모 연결
        Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
        image.color = color; // 이미지 색상 적용
        return image; // 생성 이미지 반환
    }

    private static Button CreateButton(string objectName, Transform parent, Color color) // 기본 버튼 생성
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 오브젝트 생성
        buttonObject.transform.SetParent(parent, false); // 부모 연결
        Image buttonImage = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
        buttonImage.color = color; // 버튼 배경색 적용
        Button button = buttonObject.GetComponent<Button>(); // 버튼 기능 조회
        button.targetGraphic = buttonImage; // 버튼 대상 그래픽 연결
        return button; // 생성 버튼 반환
    }

    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment) // TMP 텍스트 생성
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI)); // TMP 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // TMP 텍스트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 프로젝트 한글 글꼴 연결
        text.fontSize = fontSize; // 글자 크기 적용
        text.enableAutoSizing = true; // 짧은 화면 영역에서도 기존 문구 크기 조정
        text.fontSizeMin = fontSize * 0.76f; // 지나친 축소를 막는 최소 글자 크기
        text.fontSizeMax = fontSize; // 원래 디자인의 최대 글자 크기
        text.textWrappingMode = TextWrappingModes.NoWrap; // 제목·수치의 한 줄 정렬
        text.overflowMode = TextOverflowModes.Ellipsis; // 폭이나 높이를 넘는 문구가 이웃 영역을 침범하지 않도록 제한
        text.color = color; // 글자 색 적용
        text.alignment = alignment; // 글자 정렬 적용
        text.raycastTarget = false; // 텍스트 클릭 차단 해제
        return text; // 생성 텍스트 반환
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // RectTransform 늘림 배치
    {
        rectTransform.anchorMin = anchorMin; // 최소 앵커 적용
        rectTransform.anchorMax = anchorMax; // 최대 앵커 적용
        rectTransform.offsetMin = offsetMin; // 최소 오프셋 적용
        rectTransform.offsetMax = offsetMax; // 최대 오프셋 적용
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Vector2 pivot) // 고정 RectTransform 배치
    {
        rectTransform.anchorMin = anchorMin; // 최소 앵커 적용
        rectTransform.anchorMax = anchorMax; // 최대 앵커 적용
        rectTransform.pivot = pivot; // 크기와 위치를 적용할 기준점 설정
        rectTransform.sizeDelta = sizeDelta; // 크기 적용
        rectTransform.anchoredPosition = anchoredPosition; // 위치 적용
    }

    public void Dispose() // 선택 화면 연결 해제
    {
        if (disposed) // 중복 해제 확인
        {
            return;
        }

        disposed = true; // 화면 종료 상태 저장
        if (controller != null) // 전투 관리자 존재 확인
        {
            controller.StateChanged -= Refresh; // 선택 상태 연결 해제
        }

        if (levelManager != null) // 레벨 관리자 존재 확인
        {
            levelManager.ProgressChanged -= Refresh; // 레벨 상태 연결 해제
        }

        ClearCards(); // 생성 카드 UI 정리
    }
}
