using System; // 닫기 동작과 설비 열거형 사용
using System.Collections.Generic; // 설비 버튼 목록 사용
using TMPro; // 한글 설비 정보 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 설비 버튼과 스크롤 사용
public sealed class FacilityUpgradePanel : MonoBehaviour // 확대된 설비 강화 화면
{ // 설비 화면 시작
    private static Sprite panelSprite; // 공용 설비 패널 이미지
    private static readonly Color Ink = new Color32(22, 22, 25, 255); // 어두운 기계실과 오른쪽 상세의 기본색
    private static readonly Color Brass = new Color32(174, 129, 84, 255); // 세룰리온 설비 UI의 금속 느낌
    private static readonly Color Rust = new Color32(163, 54, 46, 255); // 도면 프레임과 선택한 설비의 붉은색
    private static readonly Color PaperInk = new Color32(67, 43, 38, 255); // 따뜻한 도면 위의 글자와 수치 색상
    private static readonly Color Cream = new Color32(240, 224, 194, 255); // 어두운 테두리와 설비 정보의 기본 글자색
    private readonly Dictionary<FacilityType, FacilityChoice> facilityButtons = new Dictionary<FacilityType, FacilityChoice>(); // 각 설비의 도면과 목록 입력 묶음
    private readonly FacilityType[] facilityTypes = (FacilityType[])Enum.GetValues(typeof(FacilityType)); // 원래 카탈로그와 같은 실제 설비 열 가지
    private ScrollRect ribbonScroll; // 설비 버튼 열의 가로 탐색 상태
    private RectTransform ribbonContent; // 각 설비 버튼을 같은 간격으로 놓을 영역
    private RectTransform ribbonViewport; // 가로 탐색에서 실제 화면에 보이는 안전 범위
    private float ribbonWidth = -1f; // 화면 가로 크기 변화와 실제 버튼 재배치 기준
    private FacilityUpgradeManager facilities; // 설비 강화 관리자
    private PlayerResourceManager resources; // 현재 자원 관리자
    private FacilityType selectedType = FacilityType.PowerSupply; // 현재 선택 설비
    private readonly TMP_Text[] resourceValues = new TMP_Text[4]; // 자원별 보유 수량 문구
    private TMP_Text facilityNameText; // 선택 설비 이름 문구
    private TMP_Text levelText; // 현재 설비 단계 문구
    private TMP_Text descriptionText; // 도면에서 찾은 실제 설비의 고유 설명
    private TMP_Text currentEffectText; // 현재 효과 문구
    private TMP_Text nextEffectText; // 다음 효과 문구
    private TMP_Text costText; // 다음 강화 비용 문구
    private Button upgradeButton; // 설비 강화 버튼
    private Action closed; // 화면 닫힘 알림
    public static FacilityUpgradePanel Create(Canvas canvas, Action onClosed) // 설비 강화 화면 생성
    { // 설비 화면 생성 시작
        if (canvas == null) // 로비 캔버스 확인
        { // 생성 불가 처리 시작
            return null; // 화면 생성 실패 반환
        } // 생성 불가 처리 종료
        GameObject root = new GameObject("FacilityUpgradeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FacilityUpgradePanel)); // 전체 설비 화면 생성
        root.transform.SetParent(canvas.transform, false); // 로비 캔버스 자식 배치
        root.transform.SetAsLastSibling(); // 로비 화면 위에 배치
        PreparationUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one); // 전체 화면 입력 영역 설정
        Image dim = root.GetComponent<Image>(); // 전체 배경 이미지 조회
        dim.color = new Color32(12, 13, 17, 230); // 실제 로비와 기술 설비 도면을 나누는 어두운 모달
        dim.raycastTarget = true; // 뒤쪽 로비 입력 차단
        FacilityUpgradePanel view = root.GetComponent<FacilityUpgradePanel>(); // 설비 화면 컴포넌트 조회
        view.closed = onClosed; // 닫힘 알림 저장
        view.Build(); // 설비 화면 구성
        return view; // 생성된 설비 화면 반환
    } // 설비 화면 생성 종료
    private void Build() // 설비 화면 요소 생성
    { // 도면과 실제 설비 선택 화면 초기 준비
        facilities = FacilityUpgradeManager.EnsureInstance(); // 기존 실제 설비 강화 관리자 준비
        resources = PlayerResourceManager.EnsureInstance(); // 현재 실제 보유 자원의 원래 관리자
        var panel = CreateSurface("FacilityPanel", transform, new Vector2(0.035f, 0.045f), new Vector2(0.965f, 0.955f), Ink); // 기존 게임 모달과 같은 기본 경로
        var panelImage = panel.GetComponent<Image>(); // 사용자 입력을 막는 기존 제작 배경
        panelImage.sprite = GetPanelSprite(); // 기존 설비 이미지 자산과 거점의 테두리 유지
        panelImage.color = panelImage.sprite == null ? Ink : new Color32(98, 79, 77, 255); // 설비 도면을 돋보이게 하는 짙은 둘레
        panelImage.raycastTarget = true; // 뒤쪽 로비의 실제 조작 차단
        var title = CreateText("Title", panel, "설비 강화", 42f, new Vector2(0.025f, 0.892f), new Vector2(0.28f, 0.968f)); // 실제 설비 화면의 제목
        title.color = Cream; // 어두운 화면 상단의 밝은 제목
        title.fontStyle = FontStyles.Bold; // 거점 설비의 현재 화면 명칭 강조
        title.alignment = TextAlignmentOptions.MidlineLeft; // 도면 왼쪽 끝과 함께 읽는 제목
        CreateSurface("HeaderRule", panel, new Vector2(0.025f, 0.875f), new Vector2(0.976f, 0.879f), Brass); // 보유 자원과 기술 도면 사이 얇은 구분선
        BuildResourceRow(panel); // 원래 과금과 연결된 네 보유 자원 표시
        var closeButton = CreateButton("CloseButton", panel, "X", new Vector2(0.914f, 0.888f), new Vector2(0.976f, 0.950f), Close); // 자원 프레임 오른쪽의 즉시 닫기 버튼
        LobbyUiArt.Apply(closeButton.image, "Button"); // 기존 거점의 금속 버튼 이미지 재사용
        closeButton.GetComponentInChildren<TMP_Text>().fontSizeMax = 29f; // 어두운 버튼 위의 읽기 쉬운 X 크기
        BuildBlueprint(panel); // 배경과 독립 버튼을 합친 열 개 설비 도면
        BuildFacilityDetail(panel); // 선택한 원래 설비의 설명과 효과 및 비용
        BuildFacilityList(panel); // 설비와 같은 순서의 아래 가로 목록
        facilities.FacilityChanged += HandleFacilityChanged; // 실제 강화 값 변경 이벤트 연결
        resources.ResourcesChanged += Refresh; // 현재 재화가 바뀌면 비용과 조작 상태 갱신
        Refresh(); // 원래 첫 설비와 실제 보유 자원 반영
        LayoutRibbon(); // 최초 부모와 현재 화면 폭에 맞춘 가로 선택 영역
    } // 도면과 실제 설비 선택 화면 초기 준비 종료

    private void BuildResourceRow(Transform parent) // 보유 자원 아이콘과 수량 생성
    { // 보유 자원 생성 시작
        var group = CreateSurface("Resources", parent, new Vector2(0.595f, 0.882f), new Vector2(0.875f, 0.940f), Ink); // 표시된 오른쪽 상단의 네 칸 보유 자원 영역
        var frame = group.GetComponent<Image>(); // 네 아이콘과 수량 뒤의 공용 배경 컴포넌트
        LobbyUiArt.Apply(frame, "ExplorationResourceFrame"); // 기존 탐사 화면의 네 칸 자원 프레임 재사용
        string[] iconNames = { "Gold", "Screw", "Iron", "Wire" }; // 기존 자원 아이콘 이름 목록
        for (int index = 0; index < iconNames.Length; index++) // 네 자원 종류 순회
        { // 개별 자원 표시 생성 시작
            GameObject itemObject = new GameObject("Resource_" + iconNames[index], typeof(RectTransform)); // 개별 자원 영역 생성
            itemObject.transform.SetParent(group, false); // 자원 묶음 자식 배치
            RectTransform item = itemObject.GetComponent<RectTransform>(); // 개별 자원 위치 조회
            PreparationUi.Stretch(item, new Vector2(index / 4f, 0f), new Vector2((index + 1) / 4f, 1f)); // 자원별 독립 영역 배치
            var icon = CreateSurface("Icon", item, new Vector2(0.075f, 0.16f), new Vector2(0.39f, 0.84f), Color.clear).GetComponent<Image>(); // 프레임 각 칸 왼쪽의 아이콘 자리
            LobbyUiArt.Apply(icon, iconNames[index]); // 기존 자원 아이콘 이미지 적용
            icon.preserveAspect = true; // 아이콘 원본 비율 유지
            TMP_Text value = CreateText("Value", item, string.Empty, 23f, new Vector2(0.42f, 0.12f), new Vector2(0.91f, 0.88f)); // 아이콘 오른쪽의 현재 보유 수량
            value.color = Cream; // 공용 어두운 자원 프레임에 맞춘 밝은 수량 색
            value.alignment = TextAlignmentOptions.MidlineRight; // 프레임 칸마다 오른쪽 수량 정렬
            value.textWrappingMode = TextWrappingModes.NoWrap; // 자원 수량을 한 줄로 표시
            value.fontSizeMin = 8f; // 작은 창에서도 긴 수량을 한 칸에 표시
            resourceValues[index] = value; // 자원별 수량 문구 보관
        } // 개별 자원 표시 생성 종료
    } // 보유 자원 생성 종료
    private void BuildBlueprint(Transform parent) // 실제 설비 원화와 조작을 배치할 큰 기술 도면
    { // 원화의 정식 표시와 각 위치 구성 시작
        var holder = CreateSurface("Blueprint", parent, new Vector2(0.025f, 0.188f), new Vector2(0.671f, 0.855f), Ink); // 왼쪽 설비 도면의 독립 화면
        AddOutline(holder.GetComponent<Image>(), Brass, 1f); // 두 화면 영역을 구분하는 실제 가장자리
        var board = CreateSurface("Board", holder, Vector2.zero, Vector2.one, new Color32(225, 215, 188, 255)); // 따뜻한 종이와 설비 클릭 좌표를 공유할 실제 보드
        var paper = Resources.Load<Texture2D>("UI/Facility/Blueprint"); // 독립 세룰리온 기술 도면 배경
        float boardAspect = paper == null ? 16f / 9f : (float)paper.width / paper.height; // 그림의 원래 가로세로 비율
        var fit = board.gameObject.AddComponent<AspectRatioFitter>(); // 창 크기가 달라도 도면 원화를 자르지 않는 비율 맞춤
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 도면 전부와 실제 노드 좌표를 같은 범위에 유지
        fit.aspectRatio = boardAspect; // 실제 가져온 도면의 가로세로 비율 사용
        CreateRaw("BlueprintTexture", board, paper, Vector2.zero, Vector2.one); // 입력과 독립된 따뜻한 도면 그림
        var boardTitle = CreateText("BlueprintTitle", board, "세룰리온 설비 배치도", 24f, new Vector2(0.065f, 0.87f), new Vector2(0.55f, 0.953f)); // 지금 조작할 기술 도면의 짧은 한글 제목
        boardTitle.alignment = TextAlignmentOptions.MidlineLeft; // 원화의 실제 설비 정렬 시작점
        boardTitle.color = PaperInk; // 도면 위에 또렷이 읽는 어두운 잉크
        boardTitle.fontStyle = FontStyles.Bold; // 지도 제목과 원래 조작의 시각 구분
        var nodes = CreateSurface("BlueprintNodes", board, Vector2.zero, Vector2.one, Color.clear); // 실제 열 설비의 도면 위 입력 레이어
        var frame = Resources.Load<Texture2D>("UI/Facility/BlueprintFrame"); // 설비 위치를 표시하는 독립 붉은 네모 원화
        for (int index = 0; index < facilityTypes.Length; index++) // 실제 원래 설비를 두 줄에 각각 다섯 개씩 배치
        { // 원래 각 설비의 실제 도면 조작 연결 시작
            FacilityType type = facilityTypes[index]; // 현재 설비의 원본 카탈로그 식별자
            var choice = new FacilityChoice(); // 나중에 아래에서 연결할 같은 설비 참조
            facilityButtons.Add(type, choice); // 동일한 설비에 두 개의 입력이 이어지는 관계 시작
            BuildBlueprintNode(nodes, frame, boardAspect, index, type, choice); // 각 설비 그림과 붉은 위치 및 실제 클릭
        } // 원래 각 설비의 실제 도면 조작 연결 종료
    } // 원화의 정식 표시와 각 위치 구성 종료

    private void BuildBlueprintNode(Transform parent, Texture2D frame, float boardAspect, int index, FacilityType type, FacilityChoice choice) // 한 설비를 도면 위의 고유 위치에 표시
    { // 실제 원래 설비 한 곳의 조작 면과 그림 구성 시작
        int row = index / 5; // 위와 아래 두 줄의 도면 순서
        int column = index % 5; // 왼쪽에서 오른쪽으로 다섯 설비 순서
        Vector2 minimum = new Vector2(0.1035f + column * 0.163f, 0.531f - row * 0.425f); // 원화 설비 칸의 중심에 맞춘 실제 위치 시작
        Vector2 maximum = minimum + new Vector2(0.143f, 0.31f); // 실제 원화 설비 칸과 겹치도록 조정한 입력 면
        var node = CreateButton("Blueprint_" + type, parent, string.Empty, minimum, maximum, () => SelectFacility(type, true)); // 원본 설비 하나에 연결한 다이어그램 클릭
        var rect = (RectTransform)node.transform; // 부모의 실제 비율에 맞는 클릭 사각형
        choice.BlueprintBackground = node.GetComponent<Image>(); // 선택되면 도면 위에서 색이 변하는 뒷면
        choice.BlueprintBackground.color = Color.clear; // 종이 배경을 그대로 보는 클릭 면
        var illustration = Resources.Load<Texture2D>("UI/Facility/Facilities/" + type); // 실제 설비별 독립 정면 일러스트
        float nodeAspect = boardAspect * (maximum.x - minimum.x) / (maximum.y - minimum.y); // 원화를 원래 비율대로 표시할 도면 한 칸의 종횡비
        Vector2 artMin = new Vector2(0.11f, 0.23f); // 붉은 프레임 안의 일러스트 안전 시작
        Vector2 artMax = new Vector2(0.89f, 0.945f); // 붉은 프레임 안에서 이름과 겹치지 않는 끝
        FitIllustration(illustration, nodeAspect, ref artMin, ref artMax); // 각 정면 그림의 원래 모양으로 실제 위치 조정
        CreateRaw("Illustration", rect, illustration, artMin, artMax); // 버튼에서 입력을 빼앗지 않는 실제 설비 그림
        choice.BlueprintFrame = CreateRaw("Frame", rect, frame, new Vector2(0.016f, 0.212f), new Vector2(0.984f, 0.995f)); // 일러스트 위 쪽에 얹은 독립 빨간 그림선
        choice.BlueprintOutline = AddOutline(choice.BlueprintFrame, Rust, 1.35f); // 실제 붉은 원화의 선만 굵게 표시할 선택 강조
        choice.BlueprintOutline.useGraphicAlpha = true; // 프레임의 투명한 안쪽과 배관선 보존
        choice.BlueprintLevel = CreateText("Level", rect, string.Empty, 17f, new Vector2(0.05f, 0.785f), new Vector2(0.30f, 0.97f)); // 프레임 모서리에 표시한 실제 설비 단계
        choice.BlueprintLevel.color = PaperInk; // 실제 도면 단계의 적갈색 잉크
        choice.BlueprintLevel.fontStyle = FontStyles.Bold; // 그림 위에서도 쉽게 읽는 숫자
        string displayName = facilities.GetDefinition(type).DisplayName; // 실제 카탈로그의 현재 현지화 이름
        CreateSurface("NamePaper", rect, new Vector2(0.035f, 0.025f), new Vector2(0.965f, 0.19f), new Color32(248, 232, 202, 240)); // 배관선과 구분되는 설비 이름의 밝은 바탕
        var name = CreateText("Name", rect, displayName, 21f, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.21f)); // 도면 프레임 아래의 실제 설비 이름
        name.color = PaperInk; // 기술 도면 원화의 어두운 글씨
        name.fontStyle = FontStyles.Bold; // 설비 이름과 배경 안내선을 구분
    } // 실제 원래 설비 한 곳의 조작 면과 그림 구성 종료

    private void BuildFacilityList(Transform parent) // 도면과 같은 열 설비를 선택할 아래 가로 조작
    { // 가로 목록과 보이는 영역의 기존 경로 구성
        var list = CreateSurface("FacilityList", parent, new Vector2(0.026f, 0.025f), new Vector2(0.974f, 0.155f), new Color32(24, 21, 24, 255)); // 하단 중앙의 긴 설비 선택 리본
        AddOutline(list.GetComponent<Image>(), Brass, 1f); // 아래 조작과 도면 영역 사이의 가는 테두리
        list.GetComponent<Image>().raycastTarget = true; // 실제 가로 드래그와 스크롤 입력 표면
        ribbonScroll = list.gameObject.AddComponent<ScrollRect>(); // 가로 폭이 좁을 때의 실제 Unity 스크롤
        ribbonScroll.horizontal = true; // 가로 넘김으로 열 설비에 접근
        ribbonScroll.vertical = false; // 새 가로 조작에서 세로 이동 제거
        ribbonScroll.movementType = ScrollRect.MovementType.Clamped; // 실제 설비 영역 밖으로 넘기지 않는 스크롤
        ribbonScroll.scrollSensitivity = 58f; // 마우스 휠 입력의 적당한 가로 이동 거리
        ribbonViewport = CreateSurface("Viewport", list, new Vector2(0.009f, 0.14f), new Vector2(0.991f, 0.93f), Color.clear); // 아래 버튼이 보이는 가로 안전 범위
        ribbonViewport.gameObject.AddComponent<RectMask2D>(); // 보이는 실제 가로 범위 밖의 버튼 가림
        var content = new GameObject("Content", typeof(RectTransform)); // 옛 설비 목록과 같은 버튼 부모
        content.transform.SetParent(ribbonViewport, false); // 가로로 움직일 실제 구성의 부모
        ribbonContent = content.GetComponent<RectTransform>(); // 실제 목록 폭을 다시 정할 위치
        ribbonContent.anchorMin = Vector2.zero; // 가로 목록의 시작 앵커
        ribbonContent.anchorMax = Vector2.up; // 세로로 뷰포트 높이를 사용하는 왼쪽 앵커
        ribbonContent.pivot = new Vector2(0f, 0.5f); // 첫 설비가 보이는 실제 가로 움직임 기준
        ribbonContent.anchoredPosition = Vector2.zero; // 첫 설비부터 보이는 원래 초기 선택
        ribbonScroll.viewport = ribbonViewport; // 가리기와 실제 포인터의 표시 영역
        ribbonScroll.content = ribbonContent; // 가로 범위 안 움직이는 실제 설비 목록
        BuildRibbonScrollbar(list); // 화면을 줄여도 조작 가능한 가로 표시 막대
        for (int index = 0; index < facilityTypes.Length; index++) // 실제 열 설비 버튼을 도면 순서대로 구성
        { // 개별 아래 버튼과 동일 설비 선택 연결 시작
            FacilityType type = facilityTypes[index]; // 아래 조작에서 사용할 실제 원래 식별자
            Vector2 minimum = new Vector2(index / (float)facilityTypes.Length, 0.035f); // 아래 버튼 한 곳의 실제 가로 시작
            Vector2 maximum = new Vector2((index + 1) / (float)facilityTypes.Length, 0.965f); // 고른 폭을 쓰는 실제 가로 끝
            var button = CreateButton("Facility_" + type, ribbonContent, string.Empty, minimum, maximum, () => SelectFacility(type)); // 기존 설비 이름을 유지한 아래 실제 클릭
            var rect = (RectTransform)button.transform; // 서로 겹치지 않는 버튼별 가로 사각형
            rect.offsetMin = new Vector2(4f, 0f); // 버튼 왼쪽의 실제 구분 간격
            rect.offsetMax = new Vector2(-4f, 0f); // 버튼 오른쪽의 실제 구분 간격
            var choice = facilityButtons[type]; // 도면과 같은 실제 카탈로그 설비의 참조
            choice.RibbonBackground = button.GetComponent<Image>(); // 아래에서 강조될 설비 버튼 표면
            choice.RibbonOutline = AddOutline(choice.RibbonBackground, Brass, 2f); // 위 도면과 함께 선택하는 실제 아래 강조
            var name = CreateText("Name", rect, facilities.GetDefinition(type).DisplayName, 20f, new Vector2(0.045f, 0.38f), new Vector2(0.955f, 0.945f)); // 두 줄까지 표시할 실제 긴 설비 이름
            name.color = Cream; // 짙은 키 바탕에 또렷한 설비 이름
            name.fontStyle = FontStyles.Bold; // 어떤 설비인지 아래에서 바로 읽는 글자
            choice.RibbonLevel = CreateText("Level", rect, string.Empty, 16f, new Vector2(0.04f, 0.085f), new Vector2(0.96f, 0.39f)); // 실제 강화 단계를 보여 주는 아래 부가 숫자
            choice.RibbonLevel.color = Brass; // 밝은 설비 이름과 구분되는 레벨 표시
        } // 개별 아래 버튼과 동일 설비 선택 연결 종료
    } // 가로 목록과 보이는 영역의 기존 경로 구성 종료

    private void BuildRibbonScrollbar(Transform parent) // 좁은 화면에서 실제 아래 목록을 빠르게 이동
    { // 스크롤 막대와 조작 손잡이 구성 시작
        var rail = CreateSurface("Scrollbar", parent, new Vector2(0.023f, 0.034f), new Vector2(0.977f, 0.093f), new Color32(54, 43, 44, 255)); // 가로 목록 아래 얇은 실제 이동 표시
        rail.GetComponent<Image>().raycastTarget = true; // 막대 위 실제 클릭과 가로 드래그
        var area = CreateSurface("SlidingArea", rail, Vector2.zero, Vector2.one, Color.clear); // 끝에서 끝의 실제 손잡이 움직임 범위
        var handle = CreateSurface("Handle", area, Vector2.zero, Vector2.one, Brass); // 지도 설비 흐름을 표시하는 옅은 금속 손잡이
        var handleImage = handle.GetComponent<Image>(); // 가로 조작에서 실제 색 부여 대상
        handleImage.raycastTarget = true; // 막대 손잡이의 실제 여러 입력 감지
        var scrollbar = rail.gameObject.AddComponent<Scrollbar>(); // 화면 끝에서 고를 수 있는 원래 가로 조작
        scrollbar.direction = Scrollbar.Direction.LeftToRight; // 첫 설비에서 마지막 설비 방향
        scrollbar.targetGraphic = handleImage; // 실제 손잡이와 입력 상태 연결
        scrollbar.handleRect = handle; // 화면 가로 위치가 필요한 실제 손잡이
        ribbonScroll.horizontalScrollbar = scrollbar; // 가로 목록의 실제 위치와 현재 막대 묶음
        ribbonScroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide; // 모든 설비가 보이면 별도 표시 불필요
    } // 스크롤 막대와 조작 손잡이 구성 종료

    private void LateUpdate() // 실제 캔버스 가로 폭 변경에 대응
    { // 다음 화면 크기의 하단 목록 배치 시작
        LayoutRibbon(); // 보이는 목록의 양수 너비와 현재 실제 설비 수에 맞는 조정
    } // 다음 화면 크기의 하단 목록 배치 종료

    private void LayoutRibbon() // 실제 설비 열 개가 보여야 할 가로 영역 조정
    { // 표시할 목록 너비와 현재 설정 비교 시작
        if (ribbonViewport == null || ribbonViewport.rect.width <= 0f || Mathf.Approximately(ribbonViewport.rect.width, ribbonWidth)) // 아직 초기화되지 않았거나 화면 폭이 같은 경우 확인
        { // 현재 그대로 유지할 가로 화면 시작
            return; // 레이아웃을 반복 생성하지 않고 실제 값 보존
        } // 현재 그대로 유지할 가로 화면 종료
        ribbonWidth = ribbonViewport.rect.width; // 화면 너비가 바뀌었을 때의 실제 값 보관
        float minimum = facilityTypes.Length * 123f; // 긴 한글 설비 이름이 읽히는 버튼당 최소 폭
        ribbonContent.sizeDelta = new Vector2(Mathf.Max(minimum, ribbonWidth), 0f); // 가능한 영역을 모두 채우고 좁을 때는 가로 이동 허용
    } // 표시할 목록 너비와 현재 설정 비교 종료

    private void CenterRibbonOn(FacilityType type) // 도면에서 찾은 같은 설비를 하단에서 볼 수 있게 함
    { // 현재 가로 이동이 필요한지 검사 시작
        float travel = ribbonContent.rect.width - ribbonViewport.rect.width; // 실제 마지막 버튼까지 이동할 가로 길이
        if (travel <= 0f) // 모든 설비를 한 줄에서 볼 수 있을 때 확인
        { // 가로 위치의 기존 상태 보존 시작
            return; // 이미 보이는 가로 목록 위치 유지
        } // 가로 위치의 기존 상태 보존 종료
        int index = Array.IndexOf(facilityTypes, type); // 도면에서 선택한 설비의 실제 가로 순번
        float center = ribbonContent.rect.width * (index + 0.5f) / facilityTypes.Length; // 현재 아래 설비의 실제 가운데
        ribbonScroll.StopMovement(); // 설비 선택을 드래그의 기존 관성보다 먼저 반영
        ribbonScroll.horizontalNormalizedPosition = Mathf.Clamp01((center - ribbonViewport.rect.width * 0.5f) / travel); // 같은 설비 버튼이 보이는 실제 가로 값
    } // 현재 가로 이동이 필요한지 검사 종료

    private void BuildFacilityDetail(Transform parent) // 선택 설비의 기존 효과와 비용을 읽는 오른쪽
    { // 실제 설비 이름과 효과 및 강화 구역 구성 시작
        var detail = CreateSurface("Detail", parent, new Vector2(0.689f, 0.19f), new Vector2(0.974f, 0.855f), new Color32(27, 24, 29, 250)); // 기술 도면 옆의 어두운 명확한 정보판
        AddOutline(detail.GetComponent<Image>(), Brass, 1f); // 도면과 구분되는 얇은 금속 정보판 경계
        facilityNameText = CreateDetailText("FacilityName", detail, string.Empty, 34f, 0.875f, 0.975f); // 선택한 원본 설비의 제목
        facilityNameText.fontStyle = FontStyles.Bold; // 내용에서 가장 먼저 읽는 실제 설비 이름
        levelText = CreateDetailText("Level", detail, string.Empty, 20f, 0.805f, 0.877f); // 원래 현재 단계와 최대 단계
        levelText.color = Brass; // 앞으로 결정할 강화 단계 표시
        descriptionText = CreateDetailText("Description", detail, string.Empty, 22f, 0.674f, 0.81f); // 선택한 설비가 담당하는 기능
        descriptionText.color = new Color32(209, 192, 168, 255); // 효과 수치와 구분되는 한글 설명
        CreateSurface("EffectDivider", detail, new Vector2(0.057f, 0.655f), new Vector2(0.942f, 0.659f), Brass); // 보유한 현재 기능과 강화 효과의 구분
        var currentHeader = CreateDetailText("CurrentHeader", detail, "현재 효과", 18f, 0.594f, 0.647f); // 실제 지금 적용되는 설비 효과 제목
        currentHeader.color = Brass; // 산술 효과와 구분한 항목 제목
        currentEffectText = CreateDetailText("CurrentEffect", detail, string.Empty, 22f, 0.487f, 0.594f); // 현재 카탈로그의 수치 설명
        var nextHeader = CreateDetailText("NextHeader", detail, "다음 효과", 18f, 0.43f, 0.485f); // 다음 강화에서 받을 실제 설명 제목
        nextHeader.color = Brass; // 오른쪽 효과 비교에 쓰는 구분
        nextEffectText = CreateDetailText("NextEffect", detail, string.Empty, 22f, 0.295f, 0.43f); // 원래 다음 레벨의 실제 효과 안내
        costText = CreateDetailText("Cost", detail, string.Empty, 19f, 0.153f, 0.293f); // 현재 보유 여부를 판단할 실제 원본 비용 표시
        costText.color = new Color32(236, 199, 146, 255); // 실제로 소비할 자원 안내 강조
        upgradeButton = CreateButton("UpgradeButton", detail, "강화", new Vector2(0.058f, 0.046f), new Vector2(0.941f, 0.147f), UpgradeSelected); // 기존 강화의 실제 원래 버튼 동작
        upgradeButton.GetComponent<Image>().color = new Color32(121, 65, 47, 255); // 비용과 같은 읽기 방향에 배치한 강화 조작 강조
        AddOutline(upgradeButton.GetComponent<Image>(), Brass, 1f); // 원래 버튼의 실제 금속 테두리
    } // 실제 설비 이름과 효과 및 강화 구역 구성 종료

    private void SelectFacility(FacilityType type, bool fromBlueprint = false) // 두 입력 화면에서 같은 설비를 실제로 선택
    { // 설비 선택 시작
        selectedType = type; // 선택 설비 저장
        Refresh(); // 도면과 아래 선택 및 오른쪽 실제 상세의 동기화
        if (fromBlueprint) // 도면에서 화면 밖 아래 설비를 고른 경우 확인
        { // 같은 종류의 아래 가로 선택을 볼 수 있게 이동 시작
            CenterRibbonOn(type); // 선택한 설비의 아래 버튼 위치를 실제 표시
        } // 같은 종류의 아래 가로 선택을 볼 수 있게 이동 종료
    } // 설비 선택 종료
    private void UpgradeSelected() // 선택 설비 강화 처리
    { // 설비 강화 시작
        facilities.TryUpgrade(selectedType); // 자원 확인 후 선택 설비 강화
        Refresh(); // 강화 결과와 자원 표시 갱신
    } // 설비 강화 종료
    private void HandleFacilityChanged(FacilityType type, int level) // 설비 단계 변경 처리
    { // 설비 변경 처리 시작
        Refresh(); // 변경된 설비와 버튼 표시 갱신
    } // 설비 변경 처리 종료
    private void Refresh() // 보유 자원과 선택 설비 표시 갱신
    { // 설비 표시 갱신 시작
        if (facilities == null || resources == null || facilityNameText == null) // 화면 초기화 상태 확인
        { // 갱신 불가 처리 시작
            return; // 화면 갱신 중단
        } // 갱신 불가 처리 종료
        resourceValues[0].text = resources.Gold.ToString(); // 현재 골드 수량 표시
        resourceValues[1].text = resources.Screw.ToString(); // 현재 나사 수량 표시
        resourceValues[2].text = resources.IronPlate.ToString(); // 현재 철판 수량 표시
        resourceValues[3].text = resources.Wire.ToString(); // 현재 전선 수량 표시
        foreach (var entry in facilityButtons) // 같은 설비의 도면과 아래 버튼 모두 현재 단계 반영
        { // 실제 두 설비 입구의 선택과 단계 갱신 시작
            int level = facilities.GetLevel(entry.Key); // 기존 설비 관리자의 실제 강화 단계
            bool active = entry.Key == selectedType; // 원래 선택한 설비와 같은지 확인
            var choice = entry.Value; // 도면과 아래에 함께 배치한 설비 화면 요소
            choice.BlueprintLevel.text = $"Lv.{level}"; // 해당 설비의 도면 현재 단계 표시
            choice.RibbonLevel.text = $"Lv.{level}"; // 가로 조작에 보여주는 같은 단계
            choice.BlueprintOutline.enabled = active; // 도면에서 실제 선택된 붉은 강조
            choice.BlueprintBackground.color = active ? new Color32(165, 60, 42, 13) : Color.clear; // 도면 색 위에 더하는 낮은 불투명도의 강조
            choice.BlueprintFrame.color = active ? Color.white : new Color32(255, 228, 219, 255); // 비선택 상태에도 읽히는 붉은 프레임 원화
            choice.RibbonOutline.enabled = active; // 하단에서 같은 설비의 실제 금속 테두리
            choice.RibbonBackground.color = active ? new Color32(90, 54, 42, 255) : new Color32(37, 32, 35, 255); // 아래 선택에서 탭 구분
        } // 실제 두 설비 입구의 선택과 단계 갱신 종료
        FacilityDefinition selected = facilities.GetDefinition(selectedType); // 선택 설비 데이터 조회
        FacilityLevelDefinition current = facilities.GetCurrentLevelDefinition(selectedType); // 현재 설비 효과 조회
        FacilityLevelDefinition next = facilities.GetNextLevelDefinition(selectedType); // 다음 강화 효과 조회
        facilityNameText.text = selected.DisplayName; // 선택 설비 이름 표시
        descriptionText.text = GetDescription(selectedType); // 현재 설비가 하는 기능의 한글 설명
        levelText.text = $"현재 단계  {facilities.GetLevel(selectedType)} / {FacilityUpgradeManager.MaximumLevel}"; // 현재 강화 단계 표시
        currentEffectText.text = current == null ? "적용 중인 효과 없음" : current.EffectDescription; // 현재 효과 설명 표시
        nextEffectText.text = next == null ? "최대 단계에 도달했습니다." : next.EffectDescription; // 다음 효과 설명 표시
        costText.text = next == null ? "더 이상 강화할 수 없습니다." : $"필요 자원  골드 {next.GoldCost}  ·  나사 {next.ScrewCost}  ·  철판 {next.IronPlateCost}  ·  전선 {next.WireCost}"; // 다음 강화 비용 표시
        upgradeButton.interactable = next != null && facilities.CanUpgrade(selectedType); // 자원과 최대 단계 기준 강화 가능 상태 적용
        upgradeButton.GetComponentInChildren<TMP_Text>().text = next == null ? "최대 단계" : upgradeButton.interactable ? "강화" : "자원 부족"; // 현재 조작 결과와 다음 단계 표시
    } // 설비 표시 갱신 종료
    public void Close() // 설비 강화 화면 닫기
    { // 화면 닫기 시작
        gameObject.SetActive(false); // 화면과 입력 즉시 숨김
        Action callback = closed; // 닫힘 알림 임시 저장
        closed = null; // 닫힘 알림 중복 방지
        callback?.Invoke(); // 화면 관리자에 닫힘 알림
        Destroy(gameObject); // 설비 화면 객체 제거
    } // 화면 닫기 종료
    private void OnDestroy() // 설비 화면 이벤트 연결 해제
    { // 화면 정리 시작
        if (facilities != null) // 설비 관리자 존재 확인
        { // 설비 이벤트 해제 시작
            facilities.FacilityChanged -= HandleFacilityChanged; // 설비 변경 이벤트 해제
        } // 설비 이벤트 해제 종료
        if (resources != null) // 자원 관리자 존재 확인
        { // 자원 이벤트 해제 시작
            resources.ResourcesChanged -= Refresh; // 자원 변경 이벤트 해제
        } // 자원 이벤트 해제 종료
    } // 화면 정리 종료
    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Vector2 minimum, Vector2 maximum) // 비율 기반 설비 문구 생성
    { // 설비 문구 생성 시작
        TMP_Text text = PreparationUi.CreateText(name, parent, value, size); // 한글 문구 생성
        PreparationUi.Stretch(text.rectTransform, minimum, maximum); // 부모 비율 기준 위치 설정
        return text; // 생성 문구 반환
    } // 설비 문구 생성 종료
    private static TMP_Text CreateDetailText(string name, Transform parent, string text, float size, float bottom, float top) // 오른쪽 정보의 한글 기본 줄폭
    { // 실제 설비 상세 항목의 표시 범위 만들기
        var label = CreateText(name, parent, text, size, new Vector2(0.063f, bottom), new Vector2(0.937f, top)); // 같은 너비와 여백을 사용하는 원래 한글 글꼴
        label.color = Cream; // 어두운 기술 화면 위의 읽기 쉬운 문구
        label.alignment = TextAlignmentOptions.MidlineLeft; // 오른쪽의 길거나 짧은 효과를 같은 열로 정렬
        label.textWrappingMode = TextWrappingModes.Normal; // 최대 효과도 여러 줄로 물리적으로 맞는 문구
        return label; // 실제 카탈로그를 표시할 원래 문구
    } // 실제 설비 상세 항목의 표시 범위 만들기 종료

    private static RectTransform CreateSurface(string name, Transform parent, Vector2 minimum, Vector2 maximum, Color color) // 도면 및 버튼 뒤에 놓는 실제 UI 면
    { // 정식 화면 요소와 배경 색 지정 시작
        var panel = PreparationUi.CreatePanel(name, parent, minimum, maximum); // 기존 게임의 UI와 같은 그림 사각형
        var image = panel.GetComponent<Image>(); // 원래 배경 컴포넌트
        image.color = color; // 각 패널이 쓰는 어두운 색 또는 도면 종이색
        image.raycastTarget = false; // 화면 고유 버튼 외의 내용은 입력을 빼앗지 않게 구성
        return panel; // 원래 화면의 실제 표시 영역
    } // 정식 화면 요소와 배경 색 지정 종료

    private static RawImage CreateRaw(string name, Transform parent, Texture2D texture, Vector2 minimum, Vector2 maximum) // 원본 리소스와 실제 버튼을 분리한 도면 그림
    { // 실제 프레임 및 정면 그림의 격리 표시 시작
        var target = new GameObject(name, typeof(RectTransform), typeof(RawImage)); // 기존 스프라이트 아틀라스를 수정하지 않는 그림 표시
        target.transform.SetParent(parent, false); // 현재 도면이나 해당 설비 위치에 붙이는 원화
        var image = target.GetComponent<RawImage>(); // 실제 그림의 런타임 텍스처를 표시할 컴포넌트
        image.texture = texture; // 실제 Unity에 가져온 원래 그림
        image.enabled = texture != null; // 리소스가 없을 때 불필요한 하얀 가림 방지
        image.raycastTarget = false; // 도면 배경과 설비 프레임이 실제 설비 클릭을 가로채지 않는 구성
        PreparationUi.Stretch(image.rectTransform, minimum, maximum); // 실제 도면과 기능 버튼에 맞춘 그림 위치
        return image; // 선택 상태와 원본 리소스를 연결할 그림 컴포넌트
    } // 실제 프레임 및 정면 그림의 격리 표시 종료

    private static void FitIllustration(Texture2D texture, float parentAspect, ref Vector2 minimum, ref Vector2 maximum) // 정면 원화의 고유 비율을 작은 도면 칸에 보존
    { // 각 설비 공간과 원화의 실제 각도와 모양 유지 시작
        if (texture == null) // 표시할 원래 설비 원화가 아직 없는 경우
        { // 그림이 없는 위치 처리 시작
            return; // 원래 프레임과 클릭 위치 유지
        } // 그림이 없는 위치 처리 종료
        Vector2 center = (minimum + maximum) * 0.5f; // 클릭 위치에서 허용할 실제 그림 중심
        Vector2 half = (maximum - minimum) * 0.5f; // 도면 프레임 안에서 그림의 현재 반쪽 범위
        float pictureAspect = (float)texture.width / texture.height; // 가져온 정면 이미지의 실제 비율
        half.x = Mathf.Min(half.x, half.y * pictureAspect / parentAspect); // 가로가 넘치는 경우 실제 그림 축소
        half.y = Mathf.Min(half.y, half.x * parentAspect / pictureAspect); // 세로가 넘치는 경우 실제 그림 축소
        minimum = center - half; // 왜곡이 없는 실제 그림의 왼쪽 아래
        maximum = center + half; // 바로 위 실제 정면 그림의 오른쪽 위
    } // 각 설비 공간과 원화의 실제 각도와 모양 유지 종료

    private static Button CreateButton(string name, Transform parent, string label, Vector2 minimum, Vector2 maximum, Action action) // 기술 도면의 실제 반응형 선택 조작
    { // 설비 위치와 선택 버튼의 입력과 바탕을 서로 연결
        var rect = CreateSurface(name, parent, minimum, maximum, new Color32(44, 34, 36, 255)); // 실제 가로 및 도면 클릭 면
        var background = rect.GetComponent<Image>(); // 실제 현재 설비 선택의 버튼 표면
        background.raycastTarget = true; // 종이 위 설비에 투명 영역도 입력 허용
        var button = rect.gameObject.AddComponent<Button>(); // 실제 Unity 포인터와 키보드 접근
        button.targetGraphic = background; // UI 선택 상태를 실제 설비 버튼 표면에 반영
        var colors = button.colors; // 현재 버튼의 색 변화 값 조회
        colors.highlightedColor = new Color32(255, 235, 207, 255); // 커서가 올 때 실제 손에 닿는 강조
        colors.selectedColor = new Color32(255, 231, 193, 255); // 키보드와 게임패드 선택에 해당하는 설비 색
        colors.disabledColor = new Color32(147, 137, 133, 232); // 자원이 부족할 때 실제 강화 조작 구분
        button.colors = colors; // 실제 Unity 입력의 변화 색 지정
        button.onClick.AddListener(() => action()); // 도면과 원래 선택 설비 상태의 실제 입력 연결
        if (!string.IsNullOrEmpty(label)) // 이름 자체가 있는 아래나 도면 핀 외의 동작 안내
        { // 닫기 또는 강화의 실제 선택 버튼 문구 만들기
            var text = CreateText("Label", rect, label, 23f, new Vector2(0.04f, 0.055f), new Vector2(0.96f, 0.945f)); // 기존 한글 글꼴로 보이는 버튼 이름
            text.color = Cream; // 실제 모달의 조작이 읽히는 대비 색
            text.fontStyle = FontStyles.Bold; // 실제 클릭 동작의 읽기 강조
        } // 닫기 또는 강화의 실제 선택 버튼 문구 만들기 종료
        return button; // 필요한 여러 실제 입력으로 연결할 Unity 버튼
    } // 설비 위치와 선택 버튼의 입력과 바탕을 서로 연결 종료

    private static Outline AddOutline(Graphic graphic, Color color, float size) // 도면이나 정보판의 필요한 외곽 구분
    { // 기술 도면 크기에 맞는 얇은 원래 외곽 설정
        var outline = graphic.gameObject.AddComponent<Outline>(); // 실제 클릭 반경과 독립인 테두리 효과
        outline.effectColor = color; // 도면과 목록에 맞춘 붉거나 따뜻한 외곽색
        outline.effectDistance = new Vector2(size, -size); // 화면에 보여줄 셰이더 외곽의 실제 두께
        return outline; // 선택 상태에 맞춰 표시할 테두리
    } // 기술 도면 크기에 맞는 얇은 원래 외곽 설정 종료

    private static string GetDescription(FacilityType type) // 설비의 역할을 실제 기존 카탈로그 기준으로 설명
    { // 정식 열 설비 종류의 짧은 안내 선택
        return type switch // 현재 선택한 실제 설비의 원래 기능
        { // 해당 설비를 강화하는 이유의 선택 시작
            FacilityType.PowerSupply => "전투에 들어갈 때 아군의 정신력을 보강합니다.", // 기존 전력 공급기의 기능
            FacilityType.DefenseBarrier => "전투에서 아군이 받는 피해를 줄입니다.", // 기존 방어 장치의 기능
            FacilityType.MagicConverter => "마법과 스킬 카드의 피해를 높이는 설비입니다.", // 기존 마력 변환기의 기능
            FacilityType.AutoRecovery => "전투를 마친 뒤 아군의 체력을 회복합니다.", // 기존 자율 회복의 기능
            FacilityType.DataAnalyzer => "적의 정보와 약점을 확인해 전투를 지원합니다.", // 기존 데이터 분석기의 기능
            FacilityType.WarehouseExpansion => "탐사와 전투에서 얻는 일반 자원을 늘립니다.", // 기존 물자 창고의 기능
            FacilityType.CombatTraining => "전투에 나서는 아군의 공격력을 높입니다.", // 기존 전투 훈련실의 기능
            FacilityType.EmergencyRepair => "탐사에서 처음 쓰러진 아군의 부활을 돕습니다.", // 기존 응급 수복 장치의 기능
            FacilityType.CommunicationStation => "탐사 중 발견할 수 있는 사건을 늘립니다.", // 기존 통신 설비의 기능
            FacilityType.EnvironmentPurifier => "퇴색 지역에서 입는 환경 피해를 줄입니다.", // 기존 환경 정화의 기능
            _ => string.Empty // 원래 정의하지 않은 종류의 실제 빈 설명
        }; // 해당 설비를 강화하는 이유의 선택 종료
    } // 정식 열 설비 종류의 짧은 안내 선택 종료

    private sealed class FacilityChoice // 같은 원본 설비의 두 위치에서 보이는 실제 상태
    { // 실제 도면 위치와 아래 선택 표기의 관계 시작
        public Image BlueprintBackground; // 도면에서 시각적으로 고른 설비의 바탕
        public RawImage BlueprintFrame; // 실제 독립 붉은 프레임 이미지
        public Outline BlueprintOutline; // 고른 설비를 구분할 강한 붉은 테두리
        public TMP_Text BlueprintLevel; // 도면 위 현재 단계
        public Image RibbonBackground; // 맨 아래에 고른 같은 설비 선택 바탕
        public Outline RibbonOutline; // 하단에 고른 설비의 경계
        public TMP_Text RibbonLevel; // 하단에 표시한 같은 설비 단계
    } // 실제 도면 위치와 아래 선택 표기의 관계 종료

    private static Sprite GetPanelSprite() // 제작한 설비 패널 이미지 조회
    { // 패널 이미지 조회 시작
        if (panelSprite != null) // 기존 이미지 존재 확인
        { // 기존 이미지 처리 시작
            return panelSprite; // 공용 패널 이미지 반환
        } // 기존 이미지 처리 종료
        Texture2D texture = Resources.Load<Texture2D>("UI/Facility/Panel"); // 제작 패널 텍스처 조회
        if (texture == null) // 제작 이미지 누락 확인
        { // 이미지 누락 처리 시작
            Debug.LogWarning("[FacilityUpgrade] 설비 패널 이미지를 찾을 수 없습니다."); // 이미지 누락 안내
            return null; // 기본 배경 사용 반환
        } // 이미지 누락 처리 종료
        panelSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 전체 텍스처를 패널 스프라이트로 생성
        panelSprite.hideFlags = HideFlags.HideAndDontSave; // 공용 이미지 임시 보관 설정
        return panelSprite; // 생성 패널 이미지 반환
    } // 패널 이미지 조회 종료
} // 설비 화면 종료
