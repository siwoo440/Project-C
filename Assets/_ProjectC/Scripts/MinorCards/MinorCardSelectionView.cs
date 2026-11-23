using System; // 기본 인터페이스 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 UI 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용

public sealed class MinorCardSelectionView : MonoBehaviour, IDisposable // 마이너 카드 레벨업 선택 화면
{
    private BattleMinorCardController controller; // 전투 마이너 카드 관리자
    private PlayerLevelRunManager levelManager; // 플레이어 레벨 관리자
    private GameObject overlayObject; // 전체 화면 입력 차단과 선택 화면
    private RectTransform cardsRoot; // 선택 카드 배치 부모
    private TMP_Text titleText; // 선택 상태 제목
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

        Image panelImage = CreateImage("Panel", overlayObject.transform, new Color(0.055f, 0.06f, 0.085f, 0.98f)); // 중앙 선택 패널 생성
        RectTransform panelRect = panelImage.rectTransform; // 패널 사각형 조회
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 설정
        panelRect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 설정
        panelRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        panelRect.sizeDelta = new Vector2(1050f, 520f); // 패널 크기 설정
        panelRect.anchoredPosition = Vector2.zero; // 화면 중앙 배치

        titleText = CreateText("Title", panelImage.transform, 30f, Color.white, TextAlignmentOptions.Center); // 선택 제목 생성
        SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 60f), new Vector2(0f, -24f), new Vector2(0.5f, 1f)); // 제목 상단 배치

        GameObject cardsObject = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 카드 가로 배치 부모 생성
        cardsObject.transform.SetParent(panelImage.transform, false); // 패널 아래 배치
        cardsRoot = cardsObject.GetComponent<RectTransform>(); // 카드 부모 저장
        Stretch(cardsRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, 36f), new Vector2(-36f, -105f)); // 카드 영역 패널 내부 배치

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

        titleText.text = $"LEVEL {levelManager.Level} · 마이너 카드 선택  |  남은 선택 {levelManager.PendingMinorCardChoices}"; // 현재 레벨과 선택권 표시
        ClearCards(); // 이전 선택 카드 UI 제거

        for (int index = 0; index < controller.CurrentChoices.Count; index++) // 현재 선택지 순회
        {
            CreateCardButton(controller.CurrentChoices[index]); // 선택 카드 버튼 생성
        }
    }

    private void CreateCardButton(MinorCardData cardData) // 마이너 카드 선택 버튼 생성
    {
        Button cardButton = CreateButton($"Minor_{cardData.MinorCardId}", cardsRoot, new Color(0.13f, 0.14f, 0.19f, 1f)); // 카드 버튼 생성
        RectTransform cardRect = cardButton.GetComponent<RectTransform>(); // 카드 사각형 조회
        cardRect.sizeDelta = new Vector2(290f, 330f); // 카드 크기 설정
        cardButton.onClick.AddListener(() => controller.TrySelectCard(cardData)); // 카드 선택 처리 연결

        TMP_Text nameText = CreateText("Name", cardButton.transform, 24f, new Color(1f, 0.84f, 0.38f, 1f), TextAlignmentOptions.Center); // 카드 이름 생성
        Stretch(nameText.rectTransform, new Vector2(0f, 0.76f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -12f)); // 이름 상단 배치
        nameText.text = cardData.DisplayName; // 카드 이름 적용

        Image iconImage = CreateImage("Icon", cardButton.transform, new Color(0.09f, 0.1f, 0.14f, 1f)); // 카드 아이콘 영역 생성
        RectTransform iconRect = iconImage.rectTransform; // 아이콘 사각형 조회
        iconRect.anchorMin = new Vector2(0.5f, 0.72f); // 아이콘 상단 중앙 앵커
        iconRect.anchorMax = new Vector2(0.5f, 0.72f); // 아이콘 상단 중앙 앵커
        iconRect.pivot = new Vector2(0.5f, 1f); // 아이콘 상단 피벗
        iconRect.sizeDelta = new Vector2(110f, 110f); // 아이콘 크기
        iconRect.anchoredPosition = Vector2.zero; // 아이콘 기준 위치
        iconImage.sprite = cardData.Icon; // 카드 아이콘 적용
        iconImage.preserveAspect = true; // 아이콘 비율 유지
        iconImage.raycastTarget = false; // 버튼 클릭 방해 방지

        TMP_Text descriptionText = CreateText("Description", cardButton.transform, 18f, Color.white, TextAlignmentOptions.Top); // 카드 설명 생성
        Stretch(descriptionText.rectTransform, new Vector2(0f, 0.14f), new Vector2(1f, 0.46f), new Vector2(16f, 0f), new Vector2(-16f, 0f)); // 설명 중앙 배치
        descriptionText.text = cardData.Description; // 카드 설명 적용
        descriptionText.textWrappingMode = TextWrappingModes.Normal; // 설명 줄바꿈 허용

        TMP_Text effectText = CreateText("Effect", cardButton.transform, 16f, new Color(0.65f, 0.82f, 1f, 1f), TextAlignmentOptions.Center); // 효과 요약 생성
        Stretch(effectText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.15f), new Vector2(10f, 5f), new Vector2(-10f, -5f)); // 효과 하단 배치
        effectText.text = GetEffectLabel(cardData); // 효과 요약 적용
    }

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
        text.fontSize = fontSize; // 글자 크기 적용
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
        rectTransform.sizeDelta = sizeDelta; // 크기 적용
        rectTransform.anchoredPosition = anchoredPosition; // 위치 적용
        rectTransform.pivot = pivot; // 피벗 적용
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
