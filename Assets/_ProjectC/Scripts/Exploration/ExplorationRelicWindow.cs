using System.Collections.Generic; // 유물 격자 화면 목록 사용
using TMPro; // 유물 이름과 설명 글자 사용
using UnityEngine; // 실제 탐사 UI 객체 사용
using UnityEngine.UI; // 유물 버튼과 스크롤 사용
public sealed class ExplorationRelicWindow : MonoBehaviour // 탐사 중 보유 유물 목록 화면
{ // 탐사 유물 화면 시작
    private RelicInventoryRuntime inventory; // 실제 탐사 회차 유물 보관함
    private readonly Dictionary<RelicData, Image> slotFrames = new Dictionary<RelicData, Image>(); // 실제 유물별 선택 테두리 목록
    private GameObject overlay; // 유물 화면과 입력 차단 배경
    private RectTransform content; // 실제 유물 격자 내용
    private Image detailIcon; // 선택한 유물의 큰 이미지
    private TMP_Text detailName; // 선택한 유물 이름
    private TMP_Text detailDescription; // 선택한 유물 효과 설명
    private TMP_Text detailRarity; // 선택한 유물 희귀도
    private Button returnButton; // 봉인된 지도의 출발 방 이동 버튼
    private TMP_Text emptyIcon; // 이미지가 없는 유물 안내
    private RelicData selected; // 현재 선택한 실제 유물
    private bool previousInputBlocked; // 화면 열기 전 탐사 입력 상태
    private bool isOpen; // 실제 유물 화면 열림 여부
    private bool dirty = true; // 닫힌 동안 유물 목록 변경 여부
    public static ExplorationRelicWindow Create(Canvas canvas, RelicInventoryRuntime source) // 실제 탐사 유물 화면 생성
    { // 유물 화면 생성 시작
        if (canvas == null || source == null) // 실제 캔버스와 보관함 확인
        { // 잘못된 연결 처리 시작
            return null; // 유물 화면 생성 실패 반환
        } // 잘못된 연결 처리 종료
        GameObject root = new GameObject("ExplorationRelicWindow", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(ExplorationRelicWindow)); // 유물 화면 루트 생성
        root.transform.SetParent(canvas.transform, false); // 실제 탐사 캔버스에 유물 화면 연결
        Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 전체 화면 기준 영역 설정
        Canvas relicCanvas = root.GetComponent<Canvas>(); // 유물 화면 독립 캔버스 조회
        relicCanvas.overrideSorting = true; // 탐사 HUD와 분리된 표시 순서 적용
        relicCanvas.sortingOrder = 210; // 탐사 정보 위쪽에 유물 화면 표시
        ExplorationRelicWindow view = root.GetComponent<ExplorationRelicWindow>(); // 유물 화면 컴포넌트 조회
        view.Initialize(source); // 실제 유물 보관함 연결
        return view; // 생성한 유물 화면 반환
    } // 유물 화면 생성 종료
    private void Initialize(RelicInventoryRuntime source) // 유물 화면 구성과 보관함 연결
    { // 유물 화면 초기화 시작
        inventory = source; // 실제 유물 보관함 저장
        Button toggle = CreateButton("RelicButton", transform, "유물", 24f); // 아이템 칸 오른쪽 유물 버튼 생성
        RectTransform toggleRect = toggle.GetComponent<RectTransform>(); // 유물 버튼 영역 조회
        toggleRect.anchorMin = new Vector2(0f, 1f); // 유물 버튼 왼쪽 위 최소 앵커
        toggleRect.anchorMax = new Vector2(0f, 1f); // 유물 버튼 왼쪽 위 최대 앵커
        toggleRect.pivot = new Vector2(0f, 1f); // 유물 버튼 왼쪽 위 기준점
        toggleRect.anchoredPosition = new Vector2(348f, -20f); // 실제 아이템 칸 오른쪽 배치
        toggleRect.sizeDelta = new Vector2(150f, 60f); // 유물 버튼 크기 설정
        toggle.onClick.AddListener(Open); // 유물 창 열기 연결
        CreateOverlay(); // 유물 상세와 격자 화면 생성
        inventory.Changed += HandleInventoryChanged; // 실제 유물 획득과 제거 연결
        overlay.SetActive(false); // 최초 유물 화면 숨김
    } // 유물 화면 초기화 종료
    private void CreateOverlay() // 유물 목록과 상세 화면 생성
    { // 유물 목록 생성 시작
        Image shade = CreateImage("Overlay", transform, new Color(0f, 0f, 0f, 0.78f), true); // 전체 탐사 화면 뒤 어두운 배경 생성
        overlay = shade.gameObject; // 유물 화면 배경 저장
        Stretch(shade.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 전체 화면 입력 차단 영역 설정
        Image frame = CreateImage("Window", overlay.transform, new Color(0.02f, 0.04f, 0.05f, 1f), true); // 유물 목록 프레임 생성
        LobbyUiArt.Apply(frame, "FormationPanel"); // 기존 금속 패널 디자인 적용
        RectTransform frameRect = frame.rectTransform; // 유물 목록 프레임 영역 조회
        frameRect.anchorMin = new Vector2(0.5f, 0.5f); // 유물 창 중앙 최소 앵커
        frameRect.anchorMax = new Vector2(0.5f, 0.5f); // 유물 창 중앙 최대 앵커
        frameRect.pivot = new Vector2(0.5f, 0.5f); // 유물 창 중앙 기준점
        frameRect.sizeDelta = new Vector2(1100f, 690f); // 유물 상세와 격자 전체 크기
        TMP_Text title = CreateText("Title", frame.transform, "보유 유물", 34f, TextAlignmentOptions.Center); // 유물 창 제목 생성
        Stretch(title.rectTransform, new Vector2(0.05f, 0.89f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero); // 유물 창 위쪽 제목 배치
        CreateDetail(frame.transform); // 왼쪽 유물 상세 화면 생성
        CreateGrid(frame.transform); // 오른쪽 유물 격자 화면 생성
        returnButton = CreateButton("UseSealedMap", frame.transform, "출발 방으로 이동", 21f); // 실제 지도 사용 버튼 생성
        Stretch(returnButton.GetComponent<RectTransform>(), new Vector2(0.05f, 0.025f), new Vector2(0.34f, 0.105f), Vector2.zero, Vector2.zero); // 유물 상세 아래 왼쪽에 배치
        returnButton.onClick.AddListener(UseSealedMap); // 실제 이동과 지도 소모 연결
        Button close = CreateButton("Close", frame.transform, "확인", 24f); // 유물 창 닫기 버튼 생성
        Stretch(close.GetComponent<RectTransform>(), new Vector2(0.4f, 0.025f), new Vector2(0.6f, 0.105f), Vector2.zero, Vector2.zero); // 창 아래 중앙 닫기 버튼 배치
        close.onClick.AddListener(Close); // 유물 창 닫기 연결
    } // 유물 목록 생성 종료
    private void CreateDetail(Transform parent) // 선택 유물 상세 화면 생성
    { // 유물 상세 생성 시작
        Image detail = CreateImage("Detail", parent, new Color(0.015f, 0.035f, 0.04f, 0.96f), false); // 왼쪽 유물 상세 배경 생성
        Stretch(detail.rectTransform, new Vector2(0.035f, 0.14f), new Vector2(0.415f, 0.88f), Vector2.zero, Vector2.zero); // 왼쪽 상세 영역 배치
        detailName = CreateText("Name", detail.transform, "보유한 유물이 없습니다", 27f, TextAlignmentOptions.Center); // 선택 유물 이름 생성
        Stretch(detailName.rectTransform, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero); // 상세 위쪽 이름 배치
        detailIcon = CreateImage("Icon", detail.transform, Color.white, false); // 선택 유물 큰 이미지 생성
        detailIcon.preserveAspect = true; // 유물 원본 비율 유지
        Stretch(detailIcon.rectTransform, new Vector2(0.19f, 0.45f), new Vector2(0.81f, 0.83f), Vector2.zero, Vector2.zero); // 상세 상단 이미지 배치
        emptyIcon = CreateText("MissingIcon", detail.transform, "?", 62f, TextAlignmentOptions.Center); // 이미지 누락 유물 안내 생성
        Stretch(emptyIcon.rectTransform, new Vector2(0.19f, 0.45f), new Vector2(0.81f, 0.83f), Vector2.zero, Vector2.zero); // 누락 이미지 안내 위치 설정
        detailRarity = CreateText("Rarity", detail.transform, "", 20f, TextAlignmentOptions.Center); // 실제 유물 희귀도 글자 생성
        detailRarity.color = new Color(0.92f, 0.72f, 0.35f, 1f); // 유물 희귀도 금색 설정
        Stretch(detailRarity.rectTransform, new Vector2(0.05f, 0.37f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero); // 이미지 아래 희귀도 배치
        detailDescription = CreateText("Description", detail.transform, "", 20f, TextAlignmentOptions.TopLeft); // 실제 유물 효과 설명 생성
        detailDescription.textWrappingMode = TextWrappingModes.Normal; // 긴 유물 설명 줄바꿈 허용
        Stretch(detailDescription.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.36f), Vector2.zero, Vector2.zero); // 상세 아래쪽 효과 설명 배치
    } // 유물 상세 생성 종료
    private void CreateGrid(Transform parent) // 실제 보유 유물 스크롤 격자 생성
    { // 유물 격자 생성 시작
        GameObject gridObject = new GameObject("Grid", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect)); // 유물 목록 스크롤 영역 생성
        gridObject.transform.SetParent(parent, false); // 유물 창 오른쪽에 격자 연결
        RectTransform gridRect = gridObject.GetComponent<RectTransform>(); // 격자 스크롤 영역 조회
        Stretch(gridRect, new Vector2(0.44f, 0.14f), new Vector2(0.965f, 0.88f), Vector2.zero, Vector2.zero); // 오른쪽 유물 격자 영역 배치
        gridObject.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.04f, 0.96f); // 격자 바탕 색상 설정
        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D)); // 유물 격자 표시 범위 생성
        viewportObject.transform.SetParent(gridObject.transform, false); // 스크롤 영역에 표시 범위 연결
        RectTransform viewport = viewportObject.GetComponent<RectTransform>(); // 표시 범위 사각형 조회
        Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f)); // 격자 내부 여백 설정
        viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // 휠 입력용 투명한 표시 범위
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // 실제 유물 칸 격자 생성
        contentObject.transform.SetParent(viewportObject.transform, false); // 표시 범위 안쪽에 유물 목록 연결
        content = contentObject.GetComponent<RectTransform>(); // 실제 유물 격자 영역 저장
        content.anchorMin = new Vector2(0f, 1f); // 유물 목록 왼쪽 위 최소 앵커
        content.anchorMax = new Vector2(1f, 1f); // 유물 목록 오른쪽 위 최대 앵커
        content.pivot = new Vector2(0.5f, 1f); // 유물 목록 위쪽 중앙 기준점
        content.sizeDelta = Vector2.zero; // 유물 목록 기본 여백 제거
        GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>(); // 실제 유물 격자 배치 조회
        grid.cellSize = new Vector2(94f, 94f); // 유물 아이콘 한 칸 크기 설정
        grid.spacing = new Vector2(10f, 10f); // 유물 아이콘 사이 간격 설정
        grid.padding = new RectOffset(8, 8, 8, 8); // 유물 격자 내부 여백 설정
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 유물 격자 가로 칸 수 고정
        grid.constraintCount = 5; // 유물 다섯 칸 가로 배치
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 유물 수에 따른 스크롤 높이 계산
        ScrollRect scroll = gridObject.GetComponent<ScrollRect>(); // 실제 유물 스크롤 조회
        scroll.viewport = viewport; // 유물 표시 범위 연결
        scroll.content = content; // 실제 유물 격자 연결
        scroll.horizontal = false; // 가로 스크롤 비활성화
        scroll.vertical = true; // 세로 스크롤 활성화
        scroll.scrollSensitivity = 35f; // 마우스 휠 이동량 설정
    } // 유물 격자 생성 종료
    private void Open() // 유물 목록 열기
    { // 유물 목록 열기 시작
        if (isOpen) // 이미 열린 화면 확인
        { // 중복 열기 처리 시작
            return; // 중복 열기 중단
        } // 중복 열기 처리 종료
        previousInputBlocked = ExplorationPlayerController.InputBlocked; // 기존 탐사 이동 잠금 상태 저장
        ExplorationPlayerController.SetInputBlocked(true); // 유물 목록 중 탐사 이동 잠금
        isOpen = true; // 실제 유물 화면 열림 기록
        overlay.SetActive(true); // 유물 목록 화면 표시
        if (dirty) // 닫힌 동안 유물 목록 변경 확인
        { // 유물 목록 갱신 시작
            Refresh(); // 실제 보유 유물 목록 표시
        } // 유물 목록 갱신 종료
        RefreshDetail(); // 직전 이동과 차단 상태에 맞게 사용 버튼 갱신
    } // 유물 목록 열기 종료
    private void Close() // 유물 목록 닫기
    { // 유물 목록 닫기 시작
        if (!isOpen) // 이미 닫힌 화면 확인
        { // 중복 닫기 처리 시작
            return; // 중복 닫기 중단
        } // 중복 닫기 처리 종료
        isOpen = false; // 실제 유물 화면 닫힘 기록
        overlay.SetActive(false); // 유물 목록 화면 숨김
        ExplorationPlayerController.SetInputBlocked(previousInputBlocked); // 기존 탐사 이동 잠금 상태 복원
    } // 유물 목록 닫기 종료
    private void HandleInventoryChanged() // 실제 보유 유물 변경 처리
    { // 유물 변경 처리 시작
        dirty = true; // 유물 격자 갱신 필요 기록
        if (isOpen) // 유물 창 표시 상태 확인
        { // 열린 유물 화면 갱신 시작
            Refresh(); // 실제 유물 목록과 상세 갱신
        } // 열린 유물 화면 갱신 종료
    } // 유물 변경 처리 종료
    private void Refresh() // 실제 보유 유물 목록과 상세 갱신
    { // 유물 목록 갱신 시작
        dirty = false; // 목록 갱신 완료 기록
        slotFrames.Clear(); // 이전 유물 테두리 참조 정리
        for (int index = content.childCount - 1; index >= 0; index--) // 이전 유물 칸 역순 순회
        { // 이전 유물 칸 제거 시작
            Transform child = content.GetChild(index); // 이전 유물 칸 조회
            child.SetParent(null, false); // 이전 유물 칸 즉시 격자에서 분리
            Destroy(child.gameObject); // 이전 유물 칸 객체 제거
        } // 이전 유물 칸 제거 종료
        IReadOnlyList<RelicData> owned = inventory.OwnedRelics; // 실제 획득 순서 유물 목록 조회
        bool selectedExists = false; // 현재 선택 유물 보유 여부 초기화
        foreach (RelicData relic in owned) // 실제 보유 유물 순회
        { // 선택 유물 존재 확인 시작
            selectedExists |= relic == selected; // 선택 유물 보유 여부 누적
        } // 선택 유물 존재 확인 종료
        if (!selectedExists) // 기존 선택 유물 미보유 확인
        { // 기본 선택 처리 시작
            selected = owned.Count > 0 ? owned[0] : null; // 첫 보유 유물 또는 빈 화면 선택
        } // 기본 선택 처리 종료
        foreach (RelicData relic in owned) // 실제 획득 순서 유물 순회
        { // 개별 유물 칸 생성 시작
            CreateRelicSlot(relic); // 실제 유물 정보로 격자 칸 생성
        } // 개별 유물 칸 생성 종료
        RefreshDetail(); // 선택 유물의 실제 상세 표시
    } // 유물 목록 갱신 종료
    private void CreateRelicSlot(RelicData relic) // 실제 보유 유물 한 칸 생성
    { // 유물 칸 생성 시작
        GameObject slot = new GameObject("RelicSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 선택 가능한 유물 칸 생성
        slot.transform.SetParent(content, false); // 실제 유물 격자에 칸 연결
        Image frame = slot.GetComponent<Image>(); // 유물 칸 테두리 이미지 조회
        BattleUiArt.Apply(frame, "CostBadge"); // 기존 금속 정사각형 테두리 적용
        frame.color = relic == selected ? new Color(1f, 0.82f, 0.45f, 1f) : Color.white; // 현재 선택 유물 테두리 강조
        slotFrames[relic] = frame; // 실제 유물과 선택 테두리 연결
        Button button = slot.GetComponent<Button>(); // 실제 유물 선택 버튼 조회
        button.onClick.AddListener(() => Select(relic)); // 현재 유물의 실제 선택 연결
        Image icon = CreateImage("Icon", slot.transform, Color.white, false); // 실제 유물 아이콘 이미지 생성
        icon.sprite = relic.Icon; // 실제 유물 이미지 연결
        icon.enabled = relic.Icon != null; // 이미지 존재 시에만 아이콘 표시
        icon.preserveAspect = true; // 실제 유물 이미지 비율 유지
        Stretch(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f)); // 테두리 안쪽에 유물 이미지 배치
        if (relic.Icon == null) // 실제 유물 이미지 누락 확인
        { // 누락 유물 이미지 안내 시작
            TMP_Text missing = CreateText("MissingIcon", slot.transform, "?", 36f, TextAlignmentOptions.Center); // 이미지 누락 기호 생성
            Stretch(missing.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 유물 칸 중앙 기호 배치
        } // 누락 유물 이미지 안내 종료
    } // 유물 칸 생성 종료
    private void Select(RelicData relic) // 유물 격자에서 실제 유물 선택
    { // 유물 선택 시작
        selected = relic; // 선택한 실제 유물 저장
        foreach (KeyValuePair<RelicData, Image> entry in slotFrames) // 현재 유물 선택 테두리 순회
        { // 유물 선택 강조 갱신 시작
            entry.Value.color = entry.Key == selected ? new Color(1f, 0.82f, 0.45f, 1f) : Color.white; // 선택 유물만 테두리 강조
        } // 유물 선택 강조 갱신 종료
        RefreshDetail(); // 선택 유물의 실제 상세 정보 갱신
    } // 유물 선택 종료
    private void RefreshDetail() // 선택한 실제 유물 상세 표시
    { // 유물 상세 갱신 시작
        detailName.text = selected != null ? selected.DisplayName : "보유한 유물이 없습니다"; // 실제 유물 이름 또는 빈 목록 안내
        detailDescription.text = selected != null ? selected.Description : ""; // 실제 유물 효과 설명 적용
        detailIcon.sprite = selected != null ? selected.Icon : null; // 실제 유물 이미지 적용
        detailIcon.enabled = selected != null && selected.Icon != null; // 실제 유물 이미지 존재 시 표시
        emptyIcon.gameObject.SetActive(selected != null && selected.Icon == null); // 이미지 누락 유물의 기호 표시
        detailRarity.text = selected != null ? GetRarityName(selected.Rarity) : ""; // 실제 유물 희귀도 표시
        bool mapSelected = selected != null && selected.EffectType == RelicEffectType.ReselectExplorationRoute; // 봉인된 지도 선택 여부
        returnButton.gameObject.SetActive(mapSelected); // 해당 유물의 상세에서만 이동 허용
        returnButton.interactable = mapSelected && !previousInputBlocked && ExplorationSessionManager.Instance?.CanUseSealedMap(FindFirstObjectByType<ExplorationMapRuntime>()) == true; // 출발 방이나 전투 전환 중 중복 이동 차단
    } // 유물 상세 갱신 종료
    private void UseSealedMap() // 목록에서 봉인된 지도 사용
    { // 지도 버튼 처리 시작
        if (!isOpen || !returnButton.interactable || selected?.EffectType != RelicEffectType.ReselectExplorationRoute) // 오래된 버튼 클릭과 미보유 차단
        { // 사용 중단 시작
            return; // 유물과 위치 보존
        } // 사용 중단 종료
        if (ExplorationSessionManager.Instance.TryUseSealedMap(FindFirstObjectByType<ExplorationMapRuntime>())) // 현재 층으로 귀환 성공 여부
        { // 귀환 성공 처리 시작
            Close(); // 이동이 끝난 탐사 화면으로 복귀
        } // 귀환 성공 처리 종료
        else // 저장 실패 또는 위치 변경
        { // 귀환 실패 처리 시작
            RefreshDetail(); // 재시도 가능 여부 표시
            detailDescription.text += "\n이동하지 못했습니다. 현재 위치와 지도를 유지합니다."; // 지도 보존 결과 안내
        } // 귀환 실패 처리 종료
    } // 지도 버튼 처리 종료
    private static string GetRarityName(RelicRarity rarity) // 유물 희귀도 한글 이름 조회
    { // 희귀도 이름 조회 시작
        switch (rarity) // 실제 유물 희귀도 구분
        { // 희귀도 분기 시작
            case RelicRarity.Rare: return "희귀"; // 희귀 유물 이름 반환
            case RelicRarity.Epic: return "특수"; // 특수 유물 이름 반환
            case RelicRarity.Legendary: return "전설"; // 전설 유물 이름 반환
            default: return "일반"; // 일반 유물 이름 반환
        } // 희귀도 분기 종료
    } // 희귀도 이름 조회 종료
    private static Image CreateImage(string objectName, Transform parent, Color color, bool blocksInput) // 공용 유물 화면 이미지 생성
    { // 이미지 생성 시작
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 객체 생성
        imageObject.transform.SetParent(parent, false); // 지정 화면 아래 이미지 연결
        Image image = imageObject.GetComponent<Image>(); // 생성 이미지 컴포넌트 조회
        image.color = color; // 이미지 기본 색상 설정
        image.raycastTarget = blocksInput; // 이미지 포인터 차단 여부 설정
        return image; // 생성한 이미지 반환
    } // 이미지 생성 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string value, float size, TextAlignmentOptions alignment) // 공용 유물 화면 글자 생성
    { // 글자 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 글자 객체 생성
        textObject.transform.SetParent(parent, false); // 지정 화면 아래 글자 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 생성 글자 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 프로젝트 한글 글꼴 적용
        text.text = value; // 실제 글자 내용 설정
        text.fontSize = size; // 글자 크기 설정
        text.color = Color.white; // 글자 색상 설정
        text.alignment = alignment; // 글자 정렬 설정
        text.raycastTarget = false; // 글자의 유물 입력 차단 방지
        return text; // 생성한 글자 반환
    } // 글자 생성 종료
    private static Button CreateButton(string objectName, Transform parent, string label, float size) // 공용 유물 화면 버튼 생성
    { // 버튼 생성 시작
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 객체 생성
        buttonObject.transform.SetParent(parent, false); // 지정 화면 아래 버튼 연결
        Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 조회
        LobbyUiArt.Apply(image, "Button"); // 기존 금속 버튼 디자인 적용
        Button button = buttonObject.GetComponent<Button>(); // 실제 버튼 기능 조회
        TMP_Text text = CreateText("Label", buttonObject.transform, label, size, TextAlignmentOptions.Center); // 버튼 중앙 글자 생성
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 버튼 전체 영역에 글자 배치
        return button; // 생성한 버튼 반환
    } // 버튼 생성 종료
    private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum, Vector2 offsetMinimum, Vector2 offsetMaximum) // UI 영역의 앵커와 여백 설정
    { // UI 영역 설정 시작
        rect.anchorMin = minimum; // UI 최소 앵커 설정
        rect.anchorMax = maximum; // UI 최대 앵커 설정
        rect.offsetMin = offsetMinimum; // UI 왼쪽 아래 여백 설정
        rect.offsetMax = offsetMaximum; // UI 오른쪽 위 여백 설정
    } // UI 영역 설정 종료
    private void OnDestroy() // 유물 화면 종료 처리
    { // 유물 화면 종료 시작
        if (inventory != null) // 실제 유물 보관함 연결 확인
        { // 유물 이벤트 해제 시작
            inventory.Changed -= HandleInventoryChanged; // 실제 유물 변경 이벤트 해제
        } // 유물 이벤트 해제 종료
        if (isOpen) // 화면이 열린 채 종료된 경우 확인
        { // 탐사 입력 복원 시작
            ExplorationPlayerController.SetInputBlocked(previousInputBlocked); // 기존 탐사 이동 잠금 상태 복원
        } // 탐사 입력 복원 종료
    } // 유물 화면 종료 종료
} // 탐사 유물 화면 종료
