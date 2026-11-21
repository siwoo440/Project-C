using System; // 지도 버튼의 공용 동작 형식
using System.Collections.Generic; // 실제 지역 핀의 배치 목록
using System.Globalization; // 플레이어 골드의 일관된 자릿수 표시
using TMPro; // 지도 지역과 상태의 한글 문구
using UnityEngine; // 실제 월드맵 리소스와 안전 영역
using UnityEngine.EventSystems; // 도움말의 키보드 초점 전환
using UnityEngine.InputSystem; // 기존 사용자 취소 키
using UnityEngine.UI; // 지도 그림과 입력 및 진행률 구성

public sealed class ExpeditionMapView : MonoBehaviour // 월드맵을 표시하고 실제 탐사 지역을 고르는 화면
{ // 월드맵 화면 시작
    private const float ArtworkAspect = 16f / 9f; // 지도 원화의 고정 가로세로 비율
    private static readonly Color Ink = new Color32(18, 15, 18, 255); // 지도 바깥과 패널의 어두운 색
    private static readonly Color PanelInk = new Color32(27, 21, 22, 248); // 지도 정보 패널의 따뜻한 짙은 색
    private static readonly Color Cream = new Color32(250, 236, 201, 255); // 중요한 지도 문구의 밝은 색
    private static readonly Color MutedCream = new Color32(203, 188, 159, 255); // 지역 안내와 보조 숫자의 색
    private static readonly Color Brass = new Color32(183, 139, 78, 255); // 진행률과 테두리의 금속 색
    private static readonly Color FocusGold = new Color32(251, 204, 114, 255); // 현재 선택한 핀의 금색 강조
    private static readonly Color LockedText = new Color32(216, 207, 195, 255); // 아직 진입할 수 없는 지역의 가독성 있는 색
    private readonly List<RegionPin> pins = new List<RegionPin>(); // 원본 지역과 해당 화면 버튼의 관계
    private Canvas canvas; // 기존 편성 화면으로 이어지는 부모 캔버스
    private PlayerResourceManager resources; // 로비와 같은 실제 플레이어 골드
    private RectTransform root; // 전체 지도와 안전 영역의 기준 크기
    private RectTransform artwork; // 종횡비가 고정된 실제 지도 그림
    private RectTransform topBar; // 안전 영역을 기준으로 둔 상단 안내
    private RectTransform dock; // 현재 선택의 지역 상세 영역
    private RectTransform helpPanel; // 안전 영역 가운데의 도움말 본문
    private TMP_Text title; // 뒤로 옆에 배치한 탐사 제목
    private TMP_Text gold; // 실제 플레이어가 현재 가진 골드
    private TMP_Text idlePrompt; // 아직 선택하지 않았을 때의 지도 조작 안내
    private TMP_Text selectedName; // 선택 지역의 실제 원본 이름
    private TMP_Text recommended; // 해당 지역의 권장 캐릭터 레벨 범위
    private TMP_Text progress; // 실제 선택 지역의 탐사 진행률
    private TMP_Text summary; // 선택 지역의 실제 소개
    private TMP_Text rewards; // 선택 지역이 제공하는 보상 정보
    private TMP_Text gate; // 시나리오 해금 조건과 보스 대기
    private TMP_Text levelText; // 현재 고른 지역 난이도
    private RectTransform levelControls; // 해금한 난이도만 고르는 버튼 묶음
    private Button back; // 기존 로비 복귀 버튼
    private Button confirm; // 선택 지역과 난이도의 실제 편성 진입
    private Button helpOpen; // 월드맵 사용 안내 열기
    private Button helpClose; // 도움말의 명시적인 닫기 조작
    private Button previousLevel; // 현재 선택한 난이도를 한 단계 내리는 버튼
    private Button nextLevel; // 해금한 범위에서 난이도를 한 단계 높이는 버튼
    private GameObject helpOverlay; // 지도 입력을 막는 도움말 화면
    private ExpeditionZoneId? selectedZone; // 새 지도에서는 아직 선택하지 않은 지역
    private int selectedLevel = 1; // 현재 지역 내의 실제 출발 난이도
    private Rect lastSafeArea; // 해상도 갱신 시 다시 적용할 직전 안전 영역

    public static ExpeditionMapView Open(Canvas targetCanvas, ExpeditionZoneId? selectedZone = null, int selectedLevel = 1) // 로비 진입과 편성에서 돌아오는 실제 지도 열기
    { // 화면과 중복 생성 조건 확인 시작
        if (targetCanvas == null || FindFirstObjectByType<ExpeditionMapView>() != null || FindFirstObjectByType<PartyFormationView>() != null) // 기존 지도와 실제 편성의 동시 입력 차단
        { // 새 화면을 만들 수 없는 경우 시작
            return null; // 현재 화면의 상태 보존
        } // 새 화면을 만들 수 없는 경우 종료
        RectTransform mapRoot = PreparationUi.CreateOverlay(targetCanvas, "MapSelection"); // 기존 로비가 여는 전체 지도 화면
        mapRoot.GetComponent<Image>().color = Ink; // 지도 종이 밖의 안정된 배경
        var view = mapRoot.gameObject.AddComponent<ExpeditionMapView>(); // 전체 지도 조작을 한 화면으로 연결
        view.canvas = targetCanvas; // 실제 편성 화면으로 넘길 부모 캔버스
        view.root = mapRoot; // 창 크기가 바뀔 때 다시 쓸 지도 영역
        view.selectedZone = selectedZone; // 편성에서 돌아온 지역 선택의 복원
        view.selectedLevel = Mathf.Max(1, selectedLevel); // 복귀할 때의 최소 실제 난이도
        view.Build(); // 지도 배경과 위치 핀 및 실제 진행 버튼 생성
        return view; // 현재 연 지도 화면 반환
    } // 화면과 중복 생성 조건 확인 종료

    private void Build() // 지역 조회와 입력을 실제 지도 그림 위에 배치
    { // 전체 지도 요소 생성 시작
        var picture = new GameObject("WorldMap", typeof(RectTransform), typeof(RawImage)); // 가져온 월드맵 원화를 표시할 객체
        picture.transform.SetParent(root, false); // 전체 지도 창의 뒤쪽에 먼저 배치
        var image = picture.GetComponent<RawImage>(); // 실제 종이 질감의 원화 컴포넌트
        image.texture = Resources.Load<Texture2D>("UI/Expedition/WorldMap"); // 월드맵 전용 16대9 지도 원화
        image.color = image.texture == null ? new Color32(89, 71, 54, 255) : Color.white; // 리소스 로드 전후의 화면 바탕
        image.raycastTarget = false; // 지도 위 지역 핀만 클릭을 받는 처리
        artwork = image.rectTransform; // 지도 위치 기준이 되는 정확한 그림 영역
        var regions = ExpeditionZones.All; // 실제로 등록한 월드맵 지역의 원본 순서
        for (int index = 0; index < regions.Count; index++) // 전체 지역 정의의 실제 핀 생성
        { // 개별 지역 핀이 받은 원본 등록 시작
            pins.Add(BuildPin(index, regions[index])); // 지도 이름과 진행률이 같은 정식 지역의 핀
        } // 개별 지역 핀이 받은 원본 등록 종료
        BuildHeader(); // 기존 뒤로 이동과 실제 골드 및 도움말
        BuildDock(); // 선택한 지역과 난이도의 실제 상세 표시
        BuildHelp(); // 지도 입력을 가리는 독립 안내 창
        resources = PlayerResourceManager.EnsureInstance(); // 로비와 같은 영구 자원 관리자 연결
        resources.ResourcesChanged += RefreshGold; // 실제 골드 변경 시 화면 동기화
        RefreshGold(); // 지도에 진입한 즉시 현재 골드 표시
        RefreshSelection(); // 처음 또는 돌아온 상태에서 올바른 편성 조건 표시
        Layout(); // 실제 해상도에 맞춘 최초 안전 영역 배치
    } // 전체 지도 요소 생성 종료

    private RegionPin BuildPin(int index, ExpeditionZoneDefinition region) // 이름과 실제 진행률을 읽을 수 있는 지도 핀 한 곳
    { // 원본 위치와 읽기 요소 배치 시작
        var button = CreateButton("MapOption_" + index, root, string.Empty, () => SelectZone(region.Id)); // 기존 지도 선택 경로와 같은 실제 클릭 진입점
        var rect = (RectTransform)button.transform; // 원화의 지역 좌표에 배치할 입력 영역
        rect.pivot = new Vector2(0.5f, 0.80f); // 핀 못 중심이 실제 지도 지점과 맞는 기준
        var background = button.GetComponent<Image>(); // 잠김과 선택 상태를 표시할 카드 표면
        background.GetComponent<Outline>().effectColor = new Color32(132, 96, 53, 225); // 지도 위에 가볍게 보이는 기존 핀 경계
        var marker = CreatePanel("PinMarker", rect, new Vector2(0.459f, 0.72f), new Vector2(0.541f, 0.88f), Brass); // 지역 지점의 금속 색 표시
        var markerImage = marker.GetComponent<Image>(); // 지역 선택에 따라 바뀌는 핀 표면
        marker.localEulerAngles = new Vector3(0f, 0f, 45f); // 방위를 표시하는 마름모 지도 핀
        AddOutline(markerImage, Ink, 1.4f); // 지도 위에서 작은 핀이 묻히지 않는 외곽
        var pinName = CreateText("RegionName", rect, region.DisplayName, 22f, 13f, new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.72f)); // 실제 지역 이름의 충분한 가로 폭
        var gauge = CreatePanel("ProgressGauge", rect, new Vector2(0.08f, 0.135f), new Vector2(0.63f, 0.235f), new Color32(68, 57, 53, 255)); // 지역별 일직선 진행률 바탕
        var fill = CreatePanel("Fill", gauge, Vector2.zero, Vector2.zero, Brass); // 원본 진행 비율이 결정하는 실제 채움 면
        var value = CreateText("ProgressValue", rect, string.Empty, 17f, 10f, new Vector2(0.65f, 0.035f), new Vector2(0.95f, 0.34f)); // 채우는 비율의 정확한 숫자와 잠김 상태
        value.alignment = TextAlignmentOptions.MidlineRight; // 숫자 길이와 무관한 핀 오른쪽 정렬
        return new RegionPin(region.Id, button, background, markerImage, pinName, fill, value); // 원본 지역과 상태별 화면 참조 보관
    } // 원본 위치와 읽기 요소 배치 종료

    private void BuildHeader() // 로비로 돌아가며 현재 골드와 지도 도움말을 확인하는 상단
    { // 상단 배경과 원래 입력 배치 시작
        topBar = CreatePanel("TopBar", root, Vector2.zero, Vector2.one, new Color32(20, 16, 19, 247)); // 종이와 구분되는 어두운 상단 띠
        CreatePanel("BottomRule", topBar, Vector2.zero, new Vector2(1f, 0.028f), Brass); // 지도와 상단 사이 금속 구분선
        back = CreateButton("BackButton", root, "뒤로", Close); // 기존 로비로 돌아가는 원래 버튼 경로
        title = PreparationUi.CreateText("Title", root, "탐사", 38f); // 탐사 메뉴의 큰 한글 제목
        title.alignment = TextAlignmentOptions.MidlineLeft; // 왼쪽 조작과 나란히 읽히는 제목
        title.fontStyle = FontStyles.Bold; // 화면 제목의 굵은 글자
        var goldPlate = CreatePanel("GoldPlate", topBar, new Vector2(0.815f, 0.16f), new Vector2(0.937f, 0.84f), PanelInk); // 다른 입력과 구분되는 실제 골드 표시 프레임
        AddOutline(goldPlate.GetComponent<Image>(), Brass, 1f); // 골드 프레임의 금속 테두리
        var goldIcon = CreatePanel("GoldIcon", goldPlate, new Vector2(0.062f, 0.10f), new Vector2(0.29f, 0.90f), Color.white); // 실제 로비와 같은 골드 아이콘 칸
        var goldIconImage = goldIcon.GetComponent<Image>(); // 골드 원래 그림의 이미지 컴포넌트
        LobbyUiArt.Apply(goldIconImage, "Gold"); // 기존 프로젝트의 원래 금화 원화 사용
        goldIconImage.preserveAspect = true; // 화면 비율과 무관한 원래 금화 그림
        gold = CreateText("Gold", goldPlate, string.Empty, 25f, 13f, new Vector2(0.31f, 0.09f), new Vector2(0.92f, 0.91f)); // 현재 실제 플레이어의 골드 표기
        gold.alignment = TextAlignmentOptions.MidlineRight; // 자릿수가 늘어도 안전한 오른쪽 정렬
        helpOpen = CreateButton("HelpButton", topBar, "?", () => SetHelpVisible(true)); // 탐사 월드맵의 실제 도움말 열기
        PreparationUi.Stretch((RectTransform)helpOpen.transform, new Vector2(0.953f, 0.16f), new Vector2(0.987f, 0.84f)); // 상단 오른쪽 끝의 독립 도움말 버튼
    } // 상단 배경과 원래 입력 배치 종료

    private void BuildDock() // 선택한 지역의 실제 정보를 읽고 편성으로 이동하는 아래 영역
    { // 지역 정보와 난이도 조작 초기 구성
        idlePrompt = PreparationUi.CreateText("SelectionStatus", root, "지도에서 탐사할 지역을 선택하세요", 25f); // 새 지도에서 필요한 첫 조작 안내
        idlePrompt.color = Cream; // 어두운 지도 하단에서 읽히는 도움글
        idlePrompt.fontStyle = FontStyles.Bold; // 그림의 질감과 겹쳐도 유지되는 읽기
        var idleOutline = idlePrompt.gameObject.AddComponent<Outline>(); // 밝은 지도 위에서도 첫 조작이 읽히는 외곽
        idleOutline.effectColor = Ink; // 조작 도움글의 선택 가능한 영역 구분
        idleOutline.effectDistance = new Vector2(1.2f, -1.2f); // 글자 주위의 실제 선 폭
        dock = CreatePanel("RegionDock", root, Vector2.zero, Vector2.one, PanelInk); // 지도 질감과 구분되는 하단 상세 영역
        AddOutline(dock.GetComponent<Image>(), Brass, 1f); // 어두운 카드의 얇은 금색 경계
        CreatePanel("ColumnDivider", dock, new Vector2(0.264f, 0.14f), new Vector2(0.266f, 0.86f), new Color32(102, 78, 51, 230)); // 지역 이름과 설명을 나누는 얇은 구분선
        selectedName = CreateDetail("SelectedName", 33f, new Vector2(0.025f, 0.50f), new Vector2(0.25f, 0.92f)); // 가장 먼저 읽는 선택 지역 이름
        selectedName.fontStyle = FontStyles.Bold; // 긴 지역명에도 유지할 주요 제목 강조
        recommended = CreateDetail("RecommendedLevels", 19f, new Vector2(0.026f, 0.26f), new Vector2(0.25f, 0.53f)); // 원본에 등록된 권장 레벨
        progress = CreateDetail("Progress", 18f, new Vector2(0.026f, 0.05f), new Vector2(0.25f, 0.29f)); // 현재 실제 해금에 연결하는 진척도
        progress.color = Brass; // 상세 영역 안의 진행률 강조
        summary = CreateDetail("Summary", 21f, new Vector2(0.288f, 0.55f), new Vector2(0.751f, 0.93f)); // 지역 원본 모험 소개
        summary.textWrappingMode = TextWrappingModes.Normal; // 읽기 좋은 실제 지역 설명의 줄바꿈
        rewards = CreateDetail("Rewards", 18f, new Vector2(0.288f, 0.30f), new Vector2(0.751f, 0.56f)); // 지역 원본의 실제 보상 안내
        rewards.color = MutedCream; // 제목과 구분한 보상 문구
        gate = CreateDetail("Gate", 18f, new Vector2(0.288f, 0.065f), new Vector2(0.751f, 0.32f)); // 잠금 조건과 보스 상태의 전용 표시
        gate.color = FocusGold; // 실제 진행을 가로막는 조건의 구분
        levelControls = CreatePanel("LevelControls", dock, new Vector2(0.78f, 0.52f), new Vector2(0.973f, 0.93f), Color.clear); // 펼칠 수 있는 실제 탐사 난이도 영역
        previousLevel = CreateButton("DecreaseLevel", levelControls, "‹", () => ChangeLevel(-1)); // 실제로 해금한 낮은 난이도 선택
        PreparationUi.Stretch((RectTransform)previousLevel.transform, new Vector2(0f, 0.07f), new Vector2(0.21f, 0.93f)); // 난이도 왼쪽의 이전 버튼
        nextLevel = CreateButton("IncreaseLevel", levelControls, "›", () => ChangeLevel(1)); // 실제 해금 최대값까지의 난이도 선택
        PreparationUi.Stretch((RectTransform)nextLevel.transform, new Vector2(0.79f, 0.07f), new Vector2(1f, 0.93f)); // 난이도 오른쪽의 다음 버튼
        levelText = CreateText("CurrentLevel", levelControls, string.Empty, 22f, 13f, new Vector2(0.22f, 0f), new Vector2(0.78f, 1f)); // 현재 출발에 쓰는 실제 난이도
        confirm = CreateButton("ConfirmButton", root, "편성하기", Confirm); // 기존 게임과 검사가 찾는 실제 편성 진입 버튼
        confirm.GetComponent<Image>().color = new Color32(91, 62, 32, 255); // 현재 가능한 탐사 진행의 금속색 주 동작
    } // 지역 정보와 난이도 조작 초기 구성 종료

    private void BuildHelp() // 월드맵에서 해금과 실제 출발을 설명하는 도움말
    { // 지도 위의 독립적인 도움말 배치 시작
        var shade = CreatePanel("MapHelp", root, Vector2.zero, Vector2.one, Color.clear); // 도움말 모달의 안전한 전체 화면 묶음
        helpOverlay = shade.gameObject; // 도움말 열기와 닫기의 실제 상태
        var backdrop = CreatePanel("DismissBackdrop", shade, Vector2.zero, Vector2.one, new Color32(12, 9, 13, 214)); // 본문과 분리된 실제 도움말 바깥 배경
        var closeOutside = backdrop.gameObject.AddComponent<Button>(); // 도움말 밖을 눌러 지도로 돌아가는 입력
        closeOutside.targetGraphic = backdrop.GetComponent<Image>(); // 도움말 바깥의 실제 클릭 면
        closeOutside.targetGraphic.raycastTarget = true; // 지도 뒤의 원래 버튼과 입력 분리
        closeOutside.transition = Selectable.Transition.None; // 배경을 눌러도 도움말의 명암 유지
        closeOutside.navigation = new Navigation { mode = Navigation.Mode.None }; // 팝업 바깥 배경을 키보드 목록에서 제외
        closeOutside.onClick.AddListener(() => SetHelpVisible(false)); // 배경 클릭으로 실제 안내 종료
        helpPanel = CreatePanel("HelpPanel", shade, Vector2.zero, Vector2.one, PanelInk); // 키보드와 터치에 맞는 별도 도움말 본문
        var panelImage = helpPanel.GetComponent<Image>(); // 본문과 배경 사이의 실제 입력 면
        panelImage.raycastTarget = true; // 본문을 읽는 중에는 바깥 종료 클릭을 전달하지 않는 차단
        AddOutline(panelImage, Brass, 2f); // 지도에 맞춘 도움말의 금색 외곽
        var helpTitle = CreateText("HelpTitle", helpPanel, "월드맵 도움말", 33f, 16f, new Vector2(0.07f, 0.74f), new Vector2(0.84f, 0.93f)); // 지금 보는 도움말의 제목
        helpTitle.alignment = TextAlignmentOptions.MidlineLeft; // 세 단계 안내와 한 기준으로 맞춘 제목
        helpTitle.fontStyle = FontStyles.Bold; // 화면 도움말 제목 강조
        var helpText = CreateText("Instructions", helpPanel, "지역 핀을 선택하면 권장 레벨, 보상, 해금 조건을 볼 수 있습니다.\n\n핀 아래의 막대는 해당 지역의 탐사 진행률입니다.\n\n해금된 지역과 난이도를 고른 뒤 편성하기를 누르세요.", 23f, 13f, new Vector2(0.07f, 0.10f), new Vector2(0.94f, 0.70f)); // 실제 현재 조작과 진행률 읽는 법
        helpText.alignment = TextAlignmentOptions.TopLeft; // 문장이 자연스럽게 이어지는 왼쪽 정렬
        helpText.textWrappingMode = TextWrappingModes.Normal; // 화면 폭에 맞는 도움말 자동 줄바꿈
        helpClose = CreateButton("CloseHelp", helpPanel, "X", () => SetHelpVisible(false)); // 도움말을 즉시 닫는 명시적 오른쪽 위 버튼
        PreparationUi.Stretch((RectTransform)helpClose.transform, new Vector2(0.87f, 0.75f), new Vector2(0.95f, 0.92f)); // 읽는 영역을 가리지 않는 독립 닫기 버튼
        helpOverlay.SetActive(false); // 처음 진입에서는 실제 월드맵을 바로 표시
    } // 지도 위의 독립적인 도움말 배치 종료

    private void Layout() // 그림과 핀의 좌표 및 나머지 조작 영역을 실제 화면에 맞춤
    { // 안전 영역과 원화의 가로세로 고정 배치 시작
        if (root == null || artwork == null || topBar == null || dock == null || helpPanel == null || root.rect.width <= 0f || root.rect.height <= 0f) // 생성 중과 크기가 아직 없는 화면 제외
        { // 아직 배치를 계산하지 않는 화면 시작
            return; // 기존 캔버스의 다음 크기 갱신까지 유지
        } // 아직 배치를 계산하지 않는 화면 종료
        lastSafeArea = Screen.safeArea; // 가로세로 안전 영역의 현재 값 보관
        Vector2 safeMin = new Vector2(lastSafeArea.xMin / Mathf.Max(1, Screen.width), lastSafeArea.yMin / Mathf.Max(1, Screen.height)); // 화면 가장자리 시작 안전 영역의 상대값
        Vector2 safeMax = new Vector2(lastSafeArea.xMax / Mathf.Max(1, Screen.width), lastSafeArea.yMax / Mathf.Max(1, Screen.height)); // 화면 가장자리 끝 안전 영역의 상대값
        if (safeMax.x <= safeMin.x || safeMax.y <= safeMin.y) // 일시적으로 안전 영역이 비어 있는 화면 정보 확인
        { // 화면 패널의 안전한 기본 크기 복원 시작
            safeMin = Vector2.zero; // 임시 화면의 안전 시작점
            safeMax = Vector2.one; // 임시 화면의 안전 끝점
        } // 화면 패널의 안전한 기본 크기 복원 종료
        Vector2 safeSize = safeMax - safeMin; // 지도의 조작에 실제 사용할 안전한 화면 비율
        Vector2 availableMin = safeMin + Vector2.Scale(safeSize, new Vector2(0.025f, 0.055f)); // 지도 한쪽 모서리와 화면 여백
        Vector2 availableMax = safeMin + Vector2.Scale(safeSize, new Vector2(0.975f, 0.986f)); // 지도 반대쪽 모서리와 화면 여백
        Vector2 available = Vector2.Scale(availableMax - availableMin, root.rect.size); // 실제 캔버스 단위의 지도 여유 크기
        float width = Mathf.Min(available.x, available.y * ArtworkAspect); // 현재 화면에서 원화를 자르지 않는 실제 최대 가로
        float height = width / ArtworkAspect; // 지도 원본 비율을 그대로 유지하는 실제 높이
        Vector2 center = (availableMin + availableMax) * 0.5f; // 원화를 놓을 안전 영역의 중앙
        Vector2 half = new Vector2(width / root.rect.width, height / root.rect.height) * 0.5f; // 원본 크기에 따른 실제 반쪽 상대 길이
        PreparationUi.Stretch(artwork, center - half, center + half); // 지도 그림을 확대해도 변형하거나 자르지 않는 실제 영역
        float scale = Mathf.Min(root.rect.width / 1920f, root.rect.height / 1080f); // 고정 참조 크기와 실제 화면의 작은 비율
        foreach (var pin in pins) // 모든 지역을 종이 그림의 실제 정규화 좌표에 올림
        { // 원화 위치별 클릭 상자의 실제 배치 시작
            Vector2 point = center - half + Vector2.Scale(half * 2f, GetPinPosition(pin.Zone)); // 원래 그린 각 지역의 실제 화면 위치
            PreparationUi.Place((RectTransform)pin.Button.transform, point, Vector2.zero, new Vector2(width * 0.132f, 95f * scale)); // 이름과 진행 막대에 맞는 비율별 핀 영역
            PreparationUi.Place(pin.Marker.rectTransform, new Vector2(0.5f, 0.80f), Vector2.zero, Vector2.one * (16f * scale)); // 실제 지도 좌표와 맞는 정사각형 마름모 못
        } // 원화 위치별 클릭 상자의 실제 배치 종료
        StretchSafe(topBar, safeMin, safeSize, new Vector2(0f, 0.914f), Vector2.one); // 화면 위 실제 안전 영역의 고정 상단 띠
        StretchSafe((RectTransform)back.transform, safeMin, safeSize, new Vector2(0.024f, 0.931f), new Vector2(0.093f, 0.983f)); // 왼쪽 끝 안전 영역에서 뒤로 이동
        StretchSafe(title.rectTransform, safeMin, safeSize, new Vector2(0.11f, 0.923f), new Vector2(0.32f, 0.989f)); // 뒤로 바로 오른쪽의 탐사 제목
        StretchSafe(idlePrompt.rectTransform, safeMin, safeSize, new Vector2(0.22f, 0.018f), new Vector2(0.78f, 0.078f)); // 지도를 가리지 않는 아래 조작 안내
        StretchSafe(dock, safeMin, safeSize, new Vector2(0.033f, 0.025f), new Vector2(0.967f, 0.185f)); // 핀보다 낮은 하단 앞쪽의 선택 정보
        StretchSafe((RectTransform)confirm.transform, safeMin, safeSize, new Vector2(0.764f, 0.049f), new Vector2(0.942f, 0.108f)); // 기존 직접 버튼과 실제 하단 카드의 정렬
        StretchSafe(helpPanel, safeMin, safeSize, new Vector2(0.266f, 0.29f), new Vector2(0.734f, 0.72f)); // 여러 화면 비율의 안전한 도움말 상자
    } // 안전 영역과 원화의 가로세로 고정 배치 종료

    private static Vector2 GetPinPosition(ExpeditionZoneId zone) // 지도 원화 안의 지역별 정규화된 실제 좌표
    { // 적힌 지도 위치 선택 시작
        return zone switch // 원본 지역 순서에 관계없는 각각의 정확한 지점
        { // 각 지도 지점 시작
            ExpeditionZoneId.WinterfallAshPlains => new Vector2(0.18f, 0.70f), // 북서쪽의 윈터폴 재평원
            ExpeditionZoneId.FracturedEarth => new Vector2(0.36f, 0.56f), // 둘 사이의 갈라진 대지
            ExpeditionZoneId.CorrodedIndustrialDistrict => new Vector2(0.18f, 0.40f), // 서쪽의 부식된 공업지대
            ExpeditionZoneId.FadedDunes => new Vector2(0.36f, 0.29f), // 아래쪽의 빛바랜 사막
            ExpeditionZoneId.ScorchedRavine => new Vector2(0.54f, 0.36f), // 남동쪽의 불타는 협곡
            ExpeditionZoneId.SubterraneanVault => new Vector2(0.77f, 0.38f), // 오른쪽의 지하 금고
            ExpeditionZoneId.RuinedCitadel => new Vector2(0.66f, 0.60f), // 북동쪽의 몰락한 성채
            ExpeditionZoneId.TheVoid => new Vector2(0.84f, 0.70f), // 동쪽 상단의 공허 지역
            _ => new Vector2(0.50f, 0.52f) // 추가된 지역의 임시 중앙 위치
        }; // 각 지도 지점 종료
    } // 적힌 지도 위치 선택 종료

    private void SelectZone(ExpeditionZoneId zone) // 해금에 관계없이 실제 지역의 정보를 선택
    { // 지역 핀이 결정한 세부 정보 확인 시작
        if (helpOverlay.activeSelf) // 도움말이 열려 있을 때 지도 뒤 선택 차단
        { // 조회할 수 없는 현재 화면 시작
            return; // 이전 지역 선택 보존
        } // 조회할 수 없는 현재 화면 종료
        if (selectedZone != zone) // 다른 지역으로 이동할 때 기존 난이도 혼입 차단
        { // 새 지역의 기본 난이도 설정 시작
            selectedLevel = 1; // 새 지역에서 가능한 첫 난이도부터 표시
        } // 새 지역의 기본 난이도 설정 종료
        selectedZone = zone; // 현재 클릭한 지역을 새 상세 데이터로 선택
        RefreshSelection(); // 실제 현재 진행과 진입 가능 상태 표시
    } // 지역 핀이 결정한 세부 정보 확인 종료

    private void RefreshSelection() // 정확한 원본 지역과 진행률을 현재 지도에 반영
    { // 실제 지역 핀과 선택 상세 갱신 시작
        bool modal = helpOverlay.activeSelf; // 열린 도움말 뒤의 조작 제한 상태
        foreach (var pin in pins) // 실제 모든 지역의 표시 상태를 원본과 동기화
        { // 개별 지도 핀의 진행 표시 시작
            bool unlocked = ExpeditionZoneProgress.IsUnlocked(pin.Zone); // 현재 원본의 실제 지역 해금 여부
            bool focused = selectedZone == pin.Zone; // 지도에서 지금 선택한 실제 위치
            float percent = Mathf.Clamp(ExpeditionZoneProgress.GetPercent(pin.Zone), 0f, 100f); // 현재 완료 상태의 안전한 백분율
            PreparationUi.Stretch(pin.Fill, Vector2.zero, new Vector2(percent / 100f, 1f)); // 진행 수치와 정확히 맞는 독립 영역의 채움 폭
            pin.Value.text = unlocked ? $"{percent:0}%" : $"{percent:0}% · 잠김"; // 원본 지역의 현재 수치와 해금 조건의 짧은 표시
            pin.Background.color = focused ? new Color32(81, 57, 34, 250) : unlocked ? new Color32(33, 25, 23, 240) : new Color32(26, 24, 28, 235); // 선택과 잠김 및 활성 핀의 분명한 표면
            pin.Marker.color = focused ? FocusGold : unlocked ? Brass : new Color32(153, 144, 130, 255); // 지역 진행이 가능한 핀을 구분하는 표시
            pin.Name.color = unlocked || focused ? Cream : LockedText; // 잠김 안내도 읽을 수 있는 이름 색
            pin.Value.color = unlocked ? FocusGold : LockedText; // 단계별 숫자의 읽기와 색 구분
            pin.Button.interactable = !modal; // 잠긴 핀도 안내를 위해 활성, 도움말 중만 차단
        } // 개별 지도 핀의 진행 표시 종료
        var region = selectedZone.HasValue ? ExpeditionZones.Get(selectedZone.Value) : null; // 현재 선택과 같은 실제 지역 정의 조회
        bool shown = region != null; // 현재 보여줄 실제 지역 정보의 존재
        idlePrompt.gameObject.SetActive(!shown); // 새 시작 시에만 지도 조작 안내
        dock.gameObject.SetActive(shown); // 지역을 선택한 경우에만 하단의 실제 정보 표시
        confirm.interactable = false; // 선택과 잠금이 확인되기 전에는 실제 진행 불가
        if (!shown) // 처음 지도나 잘못된 복귀 지역의 상세 제외
        { // 실제 선택 지역 데이터가 없는 경우 시작
            return; // 원래 초기 지도 유지
        } // 실제 선택 지역 데이터가 없는 경우 종료
        bool canEnter = ExpeditionZoneProgress.IsUnlocked(region.Id); // 같은 실제 지역의 출발 허용 상태
        int highest = Mathf.Max(1, ExpeditionZoneProgress.GetHighestLevel(region.Id)); // 실제 공개 난이도의 현재 최대값
        selectedLevel = Mathf.Clamp(selectedLevel, 1, highest); // 다시 진입 시에도 해금 범위에서만 현재 난이도 유지
        selectedName.text = region.DisplayName; // 선택한 원본 지역 이름
        recommended.text = "권장 레벨 " + region.RecommendedLevels; // 실제 탐사 지역에 지정된 캐릭터 레벨
        progress.text = $"탐사 진행률 {ExpeditionZoneProgress.GetPercent(region.Id):0}%"; // 현재 원본 지역의 실제 진척도
        summary.text = region.Summary; // 실제 원본 지역에 등록된 모험 설명
        rewards.text = "보상 " + region.RewardSummary; // 해당 지역에서 읽을 수 있는 실제 보상 안내
        bool bossPending = canEnter && ExpeditionZoneProgress.HasPendingBoss(region.Id); // 출전 가능한 해당 지역의 실제 다음 보스 대기
        gate.text = !canEnter ? "잠김 · " + region.UnlockDescription : string.Empty; // 잠긴 지역에 등록된 실제 선행 조건
        if (bossPending) // 탐사 진행률을 채운 다음의 실제 보스 진행 안내
        { // 현재 선택한 난이도와 보스 조건 비교
            gate.text = selectedLevel == highest ? "보스 조우 대기 중" : "보스 조우 대기 · 낮은 난이도 재도전"; // 최고 단계와 낮은 단계의 실제 조우 가능 표시
        } // 현재 선택한 난이도와 보스 조건 비교 종료
        levelControls.gameObject.SetActive(canEnter); // 잠긴 지역에는 난이도 조작을 제공하지 않는 표시
        levelText.text = $"난이도 {selectedLevel}"; // 실제 편성에 전달할 현재 난이도
        previousLevel.gameObject.SetActive(canEnter && highest > 1); // 선택 가능한 여러 난이도가 생겼을 때 이전 버튼 표시
        nextLevel.gameObject.SetActive(canEnter && highest > 1); // 선택 가능한 여러 난이도가 생겼을 때 다음 버튼 표시
        previousLevel.interactable = canEnter && !modal && selectedLevel > 1; // 해금된 아래 난이도와 모달 상태 확인
        nextLevel.interactable = canEnter && !modal && selectedLevel < highest; // 직접 선택할 수 있는 실제 최고 난이도 보호
        confirm.interactable = canEnter && !modal; // 실제 해금된 지역과 정상 지도에만 편성 허용
    } // 실제 지역 핀과 선택 상세 갱신 종료

    private void ChangeLevel(int direction) // 같은 지역의 실제 해금 난이도를 한 단계 이동
    { // 현재 선택한 지역과 도움말 여부 확인 시작
        if (!selectedZone.HasValue || !ExpeditionZoneProgress.IsUnlocked(selectedZone.Value) || helpOverlay.activeSelf) // 실제 지역 선택과 진입 가능 및 현재 지도 조작 확인
        { // 바꿀 수 없는 상태 시작
            return; // 원래 선택 난이도 보존
        } // 바꿀 수 없는 상태 종료
        selectedLevel += direction; // 실제 해금 상한으로 보정할 한 단계 이동
        RefreshSelection(); // 정식 범위와 버튼 표시를 바로 갱신
    } // 현재 선택한 지역과 도움말 여부 확인 종료

    private void SetHelpVisible(bool visible) // 도움말의 입력과 현재 지도 선택을 서로 분리
    { // 지도 조작과 도움말 창을 실제 모달 상태로 적용
        helpOverlay.SetActive(visible); // 실제 창과 입력 막는 배경의 열기 및 닫기
        back.interactable = !visible; // 모달 도움말의 뒤쪽 지도 복귀 입력 중단
        helpOpen.interactable = !visible; // 모달 상태에서 도움말 버튼의 중복 입력 중단
        RefreshSelection(); // 도움말 중 지역 및 편성 입력을 막고 종료 후 복구
        EventSystem.current?.SetSelectedGameObject((visible ? helpClose : helpOpen).gameObject); // 도움말 닫기와 지도 버튼으로 자연스러운 키보드 초점 이동
    } // 지도 조작과 도움말 창을 실제 모달 상태로 적용 종료

    private void Confirm() // 현재 지역과 난이도를 보존하여 실제 편성으로 진행
    { // 원래 선택과 실시간 출발 허용 재확인 시작
        if (!selectedZone.HasValue || helpOverlay.activeSelf || !ExpeditionZoneProgress.IsUnlocked(selectedZone.Value) || selectedLevel > ExpeditionZoneProgress.GetHighestLevel(selectedZone.Value)) // 직접 입력과 진행 변경 모두에서 잘못된 출발 차단
        { // 현재 지역을 출발할 수 없는 경우 시작
            RefreshSelection(); // 바뀐 잠금과 실제 현재 난이도 다시 표시
            return; // 실제 편성의 중복 또는 잘못된 지역 생성 중단
        } // 현재 지역을 출발할 수 없는 경우 종료
        gameObject.SetActive(false); // 새 편성 중 중복 지도 조작 차단
        if (PartyFormationView.Open(canvas, selectedZone.Value, selectedLevel) == null) // 원래 실제 편성이 선택 지역과 난이도를 받았는지 확인
        { // 편성 로딩이 실패했을 때의 지도 복귀 시작
            gameObject.SetActive(true); // 기존 지도와 선택을 다시 사용 가능하게 복원
            gate.text = "편성 화면을 열 수 없습니다."; // 실제 입력을 다시 할 수 있는 상태 안내
            return; // 현재 지도 선택을 그대로 보존
        } // 편성 로딩이 실패했을 때의 지도 복귀 종료
        Destroy(gameObject); // 새 편성 화면 뒤 기존 지도 수명 종료
    } // 원래 선택과 실시간 출발 허용 재확인 종료

    private void RefreshGold() // 거점의 실제 플레이어 골드를 즉시 표시
    { // 현재 자원 관리자의 게임 데이터 확인 시작
        gold.text = resources.Gold.ToString("N0", CultureInfo.InvariantCulture); // 실제 골드 증감과 큰 자릿수의 구분
    } // 현재 자원 관리자의 게임 데이터 확인 종료

    private void OnRectTransformDimensionsChange() // 실제 창 크기나 캔버스 배치가 변경된 경우
    { // 지도 전체의 현재 화면 배치 확인 시작
        Layout(); // 실제 지도 그림과 핀 및 상단·하단 조작 배치 갱신
    } // 지도 전체의 현재 화면 배치 확인 종료

    private void Update() // 화면의 안전 영역 변경과 모달 취소 키 처리
    { // 기존 입력 시스템의 실제 키 확인 시작
        if (lastSafeArea != Screen.safeArea) // 화면 크기와 별개로 안전 영역 변경 여부 확인
        { // 실제 화면 여백 갱신 시작
            Layout(); // 가장자리 영역을 다시 적용한 지도 배치
        } // 실제 화면 여백 갱신 종료
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) // 사용자 설정과 같은 원래 키보드 취소 입력
        { // 도움말 또는 지도 화면의 취소 처리 시작
            if (helpOverlay.activeSelf) // 취소 키를 먼저 받는 상단 도움말 확인
            { // 지도 복귀만 하는 모달 종료 시작
                SetHelpVisible(false); // 도움말만 닫고 실제 선택 복원
            } // 지도 복귀만 하는 모달 종료 종료
            else // 도움말이 없을 때 지도를 닫는 일반 취소
            { // 기존 로비 복귀 시작
                Close(); // 실제 지도 화면 숨김과 제거
            } // 기존 로비 복귀 종료
        } // 도움말 또는 지도 화면의 취소 처리 종료
    } // 기존 입력 시스템의 실제 키 확인 종료

    private void Close() // 실제 월드맵을 닫고 원래 로비로 복귀
    { // 원래 지도 입력과 화면 종료 시작
        gameObject.SetActive(false); // 같은 프레임에 남을 수 있는 지도 버튼의 입력 차단
        Destroy(gameObject); // 기존 로비 위의 지도 화면 제거
    } // 원래 지도 입력과 화면 종료

    private void OnDestroy() // 지도에서 골드 갱신 연결을 해제
    { // 현재 영구 자원 관리자에 남은 구독 정리 시작
        if (resources != null) // 화면 구성 전에 중단되거나 실제 파괴된 관리자 제외
        { // 현재 지도에서 설치한 이벤트 해제 시작
            resources.ResourcesChanged -= RefreshGold; // 닫힌 지도 화면의 유령 갱신 차단
        } // 현재 지도에서 설치한 이벤트 해제 종료
    } // 현재 영구 자원 관리자에 남은 구독 정리 종료

    private TMP_Text CreateDetail(string name, float size, Vector2 min, Vector2 max) // 하단 정보 패널의 왼쪽 정렬 문구
    { // 현재 지도에 읽기 좋은 상세 문구 배치 시작
        var text = CreateText(name, dock, string.Empty, size, 12f, min, max); // 원본 최대 글자 크기와 작은 화면의 최소값
        text.alignment = TextAlignmentOptions.MidlineLeft; // 모든 실제 지역 정보의 가로 시작 정렬
        return text; // 화면에 직접 연결할 실제 문구
    } // 현재 지도에 읽기 좋은 상세 문구 배치 종료

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, float minimum, Vector2 min, Vector2 max) // 실제 공통 한글 문구의 지도 배치
    { // 실제 한글 글꼴과 영역 연결 시작
        var text = PreparationUi.CreateText(name, parent, value, size); // 원래 프로젝트가 쓰는 한글 글꼴의 실제 문구
        text.fontSizeMin = minimum; // 작은 화면에서도 유지하는 실제 최소 글자 크기
        text.color = Cream; // 지도 화면에서 통일한 높은 대비의 색
        text.overflowMode = TextOverflowModes.Ellipsis; // 원본 문구가 매우 길어도 옆 UI와 겹치지 않는 표시
        PreparationUi.Stretch(text.rectTransform, min, max); // 실제 각 그룹 안의 맞춤 문구 크기
        return text; // 읽기 기능과 UI에 연결된 실제 문구
    } // 실제 한글 글꼴과 영역 연결 종료

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color) // 지도 UI에서 실제 사용할 바탕과 진행 면
    { // 화면 요소의 실제 배경 표시 시작
        var panel = PreparationUi.CreatePanel(name, parent, min, max); // 현재 원래 개발 도구와 같은 배치
        var image = panel.GetComponent<Image>(); // 실제 진행 면의 네모 이미지 연결
        image.color = color; // 지도 구성에 맞춘 정확한 배경색 적용
        image.raycastTarget = false; // 별도 버튼 외에는 입력 대상을 가로채지 않는 표시
        return panel; // 실제 배경의 위치와 크기 영역 반환
    } // 화면 요소의 실제 배경 표시 종료

    private static Button CreateButton(string name, Transform parent, string label, Action action) // 지도 화면에 어울리는 읽기 쉬운 실제 버튼
    { // 원래 버튼 동작과 배경의 상태 초기화 시작
        var target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 실제 읽기와 이벤트를 받는 Unity 버튼 객체
        target.transform.SetParent(parent, false); // 현재 지도 화면이나 관련 패널 아래 배치
        var image = target.GetComponent<Image>(); // 현재 버튼의 판금 배경
        image.color = PanelInk; // 따뜻한 색 계열의 어두운 지도 버튼
        AddOutline(image, Brass, 1f); // 잘 보이는 지도 버튼 테두리
        var button = target.GetComponent<Button>(); // 포인터와 키보드를 받는 실제 입력 컴포넌트
        button.targetGraphic = image; // 원래 입력 상태에 맞춰 버튼 표면의 색 연결
        ColorBlock colors = button.colors; // 실제 선택·비활성 상태의 변화 색 조회
        colors.highlightedColor = new Color32(255, 232, 190, 255); // 밝은 지도 위 입력 가능한 호버 표시
        colors.selectedColor = new Color32(255, 223, 165, 255); // 키보드 선택에 대응하는 따뜻한 밝기
        colors.disabledColor = new Color32(146, 135, 128, 225); // 잠금으로 실패하는 실제 조작의 비활성 표시
        colors.fadeDuration = 0.08f; // 지도 위 버튼의 짧은 색 전환
        button.colors = colors; // 실제 Unity 입력에 설정한 상태별 색 사용
        button.onClick.AddListener(() => action()); // 선택 또는 도움말과 실제 동작 연결
        if (!string.IsNullOrEmpty(label)) // 비어 있는 지도 핀에는 중복 문구 제외
        { // 실제 기능에 필요한 버튼 문구 생성 시작
            var text = CreateText("Label", target.transform, label, 25f, 13f, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f)); // 실제 조작 위치와 글자 읽기 범위
            text.fontStyle = FontStyles.Bold; // 진행과 도움말처럼 필요한 입력 이름 강조
        } // 실제 기능에 필요한 버튼 문구 생성 종료
        return button; // 이 화면이 관리하는 실제 직접 버튼
    } // 원래 버튼 동작과 배경의 상태 초기화 종료

    private static void AddOutline(Graphic graphic, Color color, float width) // 원화 위에 표시할 조작 패널의 얇은 경계
    { // 원래 UI 외곽 효과의 실제 설정
        var outline = graphic.gameObject.AddComponent<Outline>(); // 판금 버튼의 실제 가독성 경계
        outline.effectColor = color; // 원하는 지도 UI의 금색 또는 어두운 기준선
        outline.effectDistance = new Vector2(width, -width); // 화면 크기를 따라가는 선의 실제 두께
        outline.useGraphicAlpha = false; // 원래 패널 색에 가려지지 않는 고정 외곽
    } // 원래 UI 외곽 효과의 실제 설정 종료

    private static void StretchSafe(RectTransform rect, Vector2 safeMin, Vector2 safeSize, Vector2 min, Vector2 max) // 안전 영역 내부의 비율 기반 UI 배치
    { // 화면 끝을 침범하지 않는 실제 시작과 끝 위치 결정
        PreparationUi.Stretch(rect, safeMin + Vector2.Scale(safeSize, min), safeMin + Vector2.Scale(safeSize, max)); // 현재 안전 영역에 맞춘 눈에 보이는 조작 상자
    } // 화면 끝을 침범하지 않는 실제 시작과 끝 위치 결정 종료

    private sealed class RegionPin // 지역 하나의 실제 버튼과 진행률 표시 요소
    { // 개별 핀의 현재 화면 연결 시작
        public readonly ExpeditionZoneId Zone; // 원본 지역 식별자
        public readonly Button Button; // 실제 지도 클릭과 키보드 선택 입력
        public readonly Image Background; // 지역 현재 상태에 맞춘 핀 바탕
        public readonly Image Marker; // 지도 원화의 작은 지점 표시
        public readonly TMP_Text Name; // 지역의 실제 한글 이름
        public readonly RectTransform Fill; // 진행률에 따라 실제로 채울 막대 영역
        public readonly TMP_Text Value; // 실제 진행률과 현재 잠김 표기

        public RegionPin(ExpeditionZoneId zone, Button button, Image background, Image marker, TMP_Text name, RectTransform fill, TMP_Text value) // 실제 지역 원본과 화면 객체 연결
        { // 지역 하나의 화면 참조 보관 시작
            Zone = zone; // 선택과 좌표에 쓸 원본 지역 식별자
            Button = button; // 지역 클릭에 사용할 직접 버튼
            Background = background; // 현재 선택·잠김 상태의 배경 이미지
            Marker = marker; // 정규화된 실제 지도 지점의 작은 표시
            Name = name; // 현재 지역의 보이는 이름
            Fill = fill; // 원본 진행률과 폭이 같은 채움 영역
            Value = value; // 실제 완료 단계를 표시할 숫자 문구
        } // 지역 하나의 화면 참조 보관 종료
    } // 개별 핀의 현재 화면 연결 종료
} // 월드맵 화면 종료
