using System; // 기본 인터페이스 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용

public sealed class RelicDebugWindow : MonoBehaviour, IDisposable // 유물 획득 순서 디버그 창
{
    private static readonly Color PanelColor = new Color(0.035f, 0.035f, 0.045f, 0.96f); // 디버그 패널 배경색
    private static readonly Color CellColor = new Color(0.12f, 0.12f, 0.15f, 0.98f); // 유물 칸 배경색
    private static readonly Color AccentColor = new Color(0.78f, 0.62f, 0.2f, 1f); // 순서 강조색
    private RelicRunManager runManager; // 탐사 회차 유물 관리자
    private GameObject panelObject; // 유물 목록 패널 오브젝트
    private RectTransform contentRect; // 스크롤 유물 목록 부모
    private TMP_Text goldText; // 현재 골드 표시 텍스트
    private bool disposed; // 디버그 창 종료 여부

    public static RelicDebugWindow Create(Canvas parentCanvas, RelicRunManager manager, bool initiallyVisible = true) // 전투 Canvas 아래 디버그 창 생성
    {
        if (parentCanvas == null) // 부모 Canvas 존재 확인
        {
            throw new ArgumentNullException(nameof(parentCanvas)); // 부모 Canvas 누락 예외
        }

        if (manager == null) // 유물 관리자 존재 확인
        {
            throw new ArgumentNullException(nameof(manager)); // 유물 관리자 누락 예외
        }

        GameObject rootObject = new GameObject("RelicDebugWindow", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(RelicDebugWindow)); // 디버그 창 루트 생성
        rootObject.transform.SetParent(parentCanvas.transform, false); // 전투 Canvas 자식 배치
        RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 루트 사각형 조회
        Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 루트 전체 화면 배치
        Canvas debugCanvas = rootObject.GetComponent<Canvas>(); // 디버그 전용 Canvas 조회
        debugCanvas.overrideSorting = true; // 부모 Canvas 정렬 분리
        debugCanvas.sortingOrder = 450; // 전투 UI 위 디버그 정렬
        RelicDebugWindow debugWindow = rootObject.GetComponent<RelicDebugWindow>(); // 디버그 창 컴포넌트 조회
        debugWindow.Initialize(manager, initiallyVisible); // 유물 관리자와 화면 초기화
        return debugWindow; // 생성 디버그 창 반환
    }

    private void Initialize(RelicRunManager manager, bool initiallyVisible) // 디버그 창 초기화
    {
        runManager = manager; // 탐사 회차 유물 관리자 저장
        CreateToggleButton(); // 디버그 창 열기 버튼 생성
        CreatePanel(); // 유물 목록 패널 생성
        runManager.Inventory.Changed += RefreshRelics; // 유물 목록 변경 연결
        runManager.Gold.GoldChanged += HandleGoldChanged; // 골드 변경 연결
        panelObject.SetActive(initiallyVisible); // 초기 패널 표시 상태 적용
        RefreshRelics(); // 현재 유물 목록 첫 표시
        HandleGoldChanged(runManager.Gold.Gold); // 현재 골드 첫 표시
    }

    private void CreateToggleButton() // 디버그 창 표시 전환 버튼 생성
    {
        Button toggleButton = CreateButton("RelicDebugToggle", transform, "유물 DEBUG", new Color(0.16f, 0.16f, 0.2f, 0.98f)); // 우측 상단 토글 버튼 생성
        RectTransform buttonRect = toggleButton.GetComponent<RectTransform>(); // 토글 버튼 사각형 조회
        buttonRect.anchorMin = Vector2.one; // 우측 상단 최소 앵커 적용
        buttonRect.anchorMax = Vector2.one; // 우측 상단 최대 앵커 적용
        buttonRect.pivot = Vector2.one; // 우측 상단 피벗 적용
        buttonRect.sizeDelta = new Vector2(140f, 42f); // 토글 버튼 크기 적용
        buttonRect.anchoredPosition = new Vector2(-20f, -20f); // 토글 버튼 화면 위치 적용
        toggleButton.onClick.AddListener(TogglePanel); // 패널 표시 전환 연결
    }

    private void CreatePanel() // 유물 목록 패널 생성
    {
        Image panelImage = CreateImage("RelicDebugPanel", transform, PanelColor); // 디버그 패널 배경 생성
        panelObject = panelImage.gameObject; // 패널 오브젝트 저장
        RectTransform panelRect = panelImage.rectTransform; // 패널 사각형 조회
        panelRect.anchorMin = Vector2.one; // 패널 우측 상단 최소 앵커 적용
        panelRect.anchorMax = Vector2.one; // 패널 우측 상단 최대 앵커 적용
        panelRect.pivot = Vector2.one; // 패널 우측 상단 피벗 적용
        panelRect.sizeDelta = new Vector2(720f, 560f); // 패널 전체 크기 적용
        panelRect.anchoredPosition = new Vector2(-20f, -72f); // 패널 화면 위치 적용

        TMP_Text titleText = CreateText("Title", panelObject.transform, 24f, Color.white, TextAlignmentOptions.Left); // 디버그 제목 생성
        titleText.text = "획득 유물 순서"; // 디버그 제목 문구 적용
        SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 42f), new Vector2(18f, -12f), new Vector2(0f, 1f)); // 제목 위치 적용

        goldText = CreateText("Gold", panelObject.transform, 20f, AccentColor, TextAlignmentOptions.Right); // 골드 표시 생성
        SetRect(goldText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(230f, 42f), new Vector2(-18f, -12f), new Vector2(1f, 1f)); // 골드 표시 위치 적용

        CreateScrollView(panelObject.transform); // 유물 순서 스크롤 영역 생성
    }

    private void CreateScrollView(Transform parent) // 유물 목록 ScrollView 생성
    {
        GameObject scrollObject = new GameObject("RelicScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect)); // ScrollView 루트 생성
        scrollObject.transform.SetParent(parent, false); // 패널 자식 배치
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>(); // ScrollView 사각형 조회
        Stretch(scrollRectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -70f)); // 패널 내부 스크롤 영역 배치
        Image scrollBackground = scrollObject.GetComponent<Image>(); // ScrollView 배경 이미지 조회
        scrollBackground.color = new Color(0.02f, 0.02f, 0.025f, 0.9f); // ScrollView 배경색 적용

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D)); // ScrollView 보이는 영역 생성
        viewportObject.transform.SetParent(scrollObject.transform, false); // ScrollView 자식 배치
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>(); // Viewport 사각형 조회
        Stretch(viewportRect, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f)); // Viewport 내부 여백 적용
        Image viewportImage = viewportObject.GetComponent<Image>(); // Viewport 이미지 조회
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f); // 거의 투명한 마스크 배경 적용

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // 유물 셀 목록 부모 생성
        contentObject.transform.SetParent(viewportObject.transform, false); // Viewport 자식 배치
        contentRect = contentObject.GetComponent<RectTransform>(); // Content 사각형 저장
        contentRect.anchorMin = new Vector2(0f, 1f); // Content 상단 최소 앵커 적용
        contentRect.anchorMax = new Vector2(1f, 1f); // Content 상단 최대 앵커 적용
        contentRect.pivot = new Vector2(0.5f, 1f); // Content 상단 중앙 피벗 적용
        contentRect.anchoredPosition = Vector2.zero; // Content 기준 위치 초기화
        contentRect.sizeDelta = Vector2.zero; // Content 추가 크기 초기화

        GridLayoutGroup gridLayout = contentObject.GetComponent<GridLayoutGroup>(); // GridLayoutGroup 조회
        gridLayout.cellSize = new Vector2(120f, 126f); // 유물 셀 크기 적용
        gridLayout.spacing = new Vector2(10f, 10f); // 유물 셀 간격 적용
        gridLayout.padding = new RectOffset(10, 10, 10, 10); // 유물 목록 내부 여백 적용
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft; // 왼쪽 위부터 유물 배치
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal; // 가로 방향 우선 배치
        gridLayout.childAlignment = TextAnchor.UpperLeft; // 유물 셀 왼쪽 위 정렬
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 수 사용
        gridLayout.constraintCount = 5; // 한 줄에 유물 다섯 개 배치

        ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>(); // Content 자동 크기 조절 조회
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 가로 크기 Viewport 유지
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 유물 행 수에 맞춰 세로 확장

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>(); // ScrollRect 기능 조회
        scrollRect.viewport = viewportRect; // 보이는 영역 연결
        scrollRect.content = contentRect; // 스크롤 대상 Content 연결
        scrollRect.horizontal = false; // 가로 스크롤 비활성화
        scrollRect.vertical = true; // 세로 스크롤 활성화
        scrollRect.movementType = ScrollRect.MovementType.Clamped; // 스크롤 범위 제한
        scrollRect.scrollSensitivity = 34f; // 마우스 휠 감도 적용
    }

    private void RefreshRelics() // 보유 유물 목록 다시 그리기
    {
        if (disposed || contentRect == null || runManager == null) // 갱신 가능 상태 확인
        {
            return; // 목록 갱신 중단
        }

        for (int index = contentRect.childCount - 1; index >= 0; index--) // 기존 유물 셀 역순 순회
        {
            GameObject childObject = contentRect.GetChild(index).gameObject; // 기존 셀 오브젝트 조회
            childObject.SetActive(false); // 기존 셀 즉시 레이아웃 제외
            Destroy(childObject); // 기존 셀 제거 예약
        }

        for (int index = 0; index < runManager.Inventory.OwnedRelics.Count; index++) // 현재 유물 순서 순회
        {
            RelicData relicData = runManager.Inventory.OwnedRelics[index]; // 현재 순번 유물 데이터 조회
            if (relicData == null) // 빈 유물 데이터 확인
            {
                continue; // 다음 유물 확인
            }

            CreateRelicCell(relicData, index + 1); // 현재 유물 셀과 순번 생성
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect); // 유물 제거 후 순번 당김 레이아웃 즉시 갱신
    }

    private void CreateRelicCell(RelicData relicData, int orderNumber) // 단일 유물 셀 생성
    {
        Image cellImage = CreateImage($"Relic_{orderNumber}_{relicData.RelicId}", contentRect, CellColor); // 유물 셀 배경 생성
        RectTransform cellRect = cellImage.rectTransform; // 유물 셀 사각형 조회
        cellRect.sizeDelta = new Vector2(120f, 126f); // 유물 셀 고정 크기 적용

        TMP_Text orderText = CreateText("Order", cellImage.transform, 16f, AccentColor, TextAlignmentOptions.TopLeft); // 획득 순서 텍스트 생성
        orderText.text = orderNumber.ToString(); // 왼쪽 위 순서 숫자 적용
        Stretch(orderText.rectTransform, new Vector2(0f, 0.76f), new Vector2(0.32f, 1f), new Vector2(5f, 0f), new Vector2(0f, -2f)); // 순서 숫자 왼쪽 위 배치

        Image iconImage = CreateImage("Icon", cellImage.transform, new Color(0.2f, 0.2f, 0.23f, 1f)); // 유물 아이콘 영역 생성
        RectTransform iconRect = iconImage.rectTransform; // 유물 아이콘 사각형 조회
        iconRect.anchorMin = new Vector2(0.5f, 1f); // 아이콘 상단 중앙 최소 앵커 적용
        iconRect.anchorMax = new Vector2(0.5f, 1f); // 아이콘 상단 중앙 최대 앵커 적용
        iconRect.pivot = new Vector2(0.5f, 1f); // 아이콘 상단 중앙 피벗 적용
        iconRect.sizeDelta = new Vector2(72f, 72f); // 아이콘 크기 적용
        iconRect.anchoredPosition = new Vector2(0f, -18f); // 아이콘 위치 적용
        iconImage.sprite = relicData.Icon; // 유물 아이콘 Sprite 연결
        iconImage.preserveAspect = true; // 아이콘 비율 유지

        TMP_Text nameText = CreateText("Name", cellImage.transform, 13f, Color.white, TextAlignmentOptions.Center); // 유물 이름 텍스트 생성
        nameText.text = relicData.DisplayName; // 유물 표시 이름 적용
        Stretch(nameText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.25f), new Vector2(5f, 3f), new Vector2(-5f, -1f)); // 유물 이름 하단 배치
        nameText.textWrappingMode = TextWrappingModes.Normal; // 긴 유물 이름 줄바꿈 허용

        Button removeButton = CreateButton("Remove", cellImage.transform, "×", new Color(0.5f, 0.12f, 0.12f, 0.95f)); // 디버그 제거 버튼 생성
        RectTransform removeRect = removeButton.GetComponent<RectTransform>(); // 제거 버튼 사각형 조회
        removeRect.anchorMin = Vector2.one; // 제거 버튼 우측 상단 최소 앵커 적용
        removeRect.anchorMax = Vector2.one; // 제거 버튼 우측 상단 최대 앵커 적용
        removeRect.pivot = Vector2.one; // 제거 버튼 우측 상단 피벗 적용
        removeRect.sizeDelta = new Vector2(24f, 24f); // 제거 버튼 크기 적용
        removeRect.anchoredPosition = new Vector2(-3f, -3f); // 제거 버튼 위치 적용
        string relicId = relicData.RelicId; // 제거 콜백용 유물 ID 복사
        removeButton.onClick.AddListener(() => runManager.TryRemove(relicId)); // 클릭 시 현재 유물 제거 연결
    }

    private void HandleGoldChanged(int currentGold) // 골드 변경 화면 처리
    {
        if (goldText == null) // 골드 텍스트 존재 확인
        {
            return; // 골드 표시 중단
        }

        goldText.text = $"중복 변환 골드  {currentGold}"; // 현재 임시 골드 표시
    }

    private void TogglePanel() // 디버그 패널 표시 전환
    {
        if (panelObject == null) // 패널 존재 확인
        {
            return; // 표시 전환 중단
        }

        panelObject.SetActive(!panelObject.activeSelf); // 현재 표시 상태 반전
    }

    private static Image CreateImage(string objectName, Transform parent, Color color) // 공용 이미지 생성
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(parent, false); // 지정 부모 자식 배치
        Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
        image.color = color; // 이미지 색상 적용
        return image; // 생성 이미지 반환
    }

    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment) // 공용 텍스트 생성
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 지정 부모 자식 배치
        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>(); // 텍스트 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 프로젝트 한글 글꼴 적용
        text.fontSize = fontSize; // 글자 크기 적용
        text.color = color; // 글자 색상 적용
        text.alignment = alignment; // 글자 정렬 적용
        text.raycastTarget = false; // 텍스트 포인터 차단 해제
        return text; // 생성 텍스트 반환
    }

    private static Button CreateButton(string objectName, Transform parent, string label, Color backgroundColor) // 공용 버튼 생성
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 오브젝트 생성
        buttonObject.transform.SetParent(parent, false); // 지정 부모 자식 배치
        Image buttonImage = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 조회
        buttonImage.color = backgroundColor; // 버튼 배경색 적용
        Button button = buttonObject.GetComponent<Button>(); // 버튼 기능 조회
        TMP_Text labelText = CreateText("Label", buttonObject.transform, 15f, Color.white, TextAlignmentOptions.Center); // 버튼 글자 생성
        labelText.text = label; // 버튼 문구 적용
        Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 버튼 글자 전체 영역 배치
        return button; // 생성 버튼 반환
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // 사각형 늘림 배치
    {
        rectTransform.anchorMin = anchorMin; // 최소 앵커 적용
        rectTransform.anchorMax = anchorMax; // 최대 앵커 적용
        rectTransform.offsetMin = offsetMin; // 최소 여백 적용
        rectTransform.offsetMax = offsetMax; // 최대 여백 적용
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Vector2 pivot) // 고정 크기 사각형 배치
    {
        rectTransform.anchorMin = anchorMin; // 최소 앵커 적용
        rectTransform.anchorMax = anchorMax; // 최대 앵커 적용
        rectTransform.sizeDelta = sizeDelta; // 사각형 크기 적용
        rectTransform.anchoredPosition = anchoredPosition; // 사각형 위치 적용
        rectTransform.pivot = pivot; // 사각형 피벗 적용
    }

    public void Dispose() // 디버그 창 연결 해제
    {
        if (disposed) // 기존 종료 여부 확인
        {
            return; // 중복 종료 중단
        }

        disposed = true; // 디버그 창 종료 상태 저장
        if (runManager != null) // 유물 관리자 존재 확인
        {
            runManager.Inventory.Changed -= RefreshRelics; // 유물 목록 변경 연결 해제
            runManager.Gold.GoldChanged -= HandleGoldChanged; // 골드 변경 연결 해제
        }
    }

    private void OnDestroy() // 디버그 창 오브젝트 제거 처리
    {
        Dispose(); // 이벤트 연결 해제
    }
}
