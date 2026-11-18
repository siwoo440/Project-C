using TMPro; // 아이템 이름 안내 글자 사용
using UnityEngine; // 탐사 화면 객체 사용
using UnityEngine.UI; // 아이템 칸 이미지와 배치 사용
public sealed class ExplorationInventoryView : MonoBehaviour // 탐사 중 보유 아이템 화면
{ // 탐사 아이템 화면 시작
    private readonly Image[] icons = new Image[ConsumableInventoryRuntime.SlotCount]; // 실제 아이템 이미지 목록
    private readonly TMP_Text[] missingIcons = new TMP_Text[ConsumableInventoryRuntime.SlotCount]; // 이미지 누락 아이템 안내 목록
    private ConsumableInventoryRuntime inventory; // 실제 회차 소모품 보관함
    private GameObject tooltip; // 마우스 위치의 아이템 안내 화면
    private TMP_Text tooltipText; // 아이템 이름과 설명 글자
    private int hoveredIndex = -1; // 현재 마우스가 가리킨 칸
    public static ExplorationInventoryView Create(Canvas canvas, ConsumableInventoryRuntime source) // 실제 탐사 아이템 화면 생성
    { // 아이템 화면 생성 시작
        GameObject root = new GameObject("ExplorationInventory", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ExplorationInventoryView)); // 탐사 아이템 화면 생성
        root.transform.SetParent(canvas.transform, false); // 탐사 화면 왼쪽 위에 연결
        RectTransform area = root.transform as RectTransform; // 아이템 화면 영역 조회
        area.anchorMin = new Vector2(0f, 1f); // 화면 왼쪽 위 최소 앵커
        area.anchorMax = new Vector2(0f, 1f); // 화면 왼쪽 위 최대 앵커
        area.pivot = new Vector2(0f, 1f); // 왼쪽 위 기준점 설정
        area.anchoredPosition = new Vector2(20f, -20f); // 탐사 화면 가장자리 여백
        area.sizeDelta = new Vector2(316f, 132f); // 다섯 칸과 두 줄의 화면 크기
        Image background = root.GetComponent<Image>(); // 아이템 패널 배경 조회
        background.color = new Color(0.01f, 0.03f, 0.04f, 0.86f); // 어두운 아이템 패널 바탕
        background.raycastTarget = false; // 빈 패널의 탐사 입력 차단 방지
        ExplorationInventoryView view = root.GetComponent<ExplorationInventoryView>(); // 생성한 아이템 화면 조회
        view.Initialize(source); // 실제 소모품 보관함 연결
        return view; // 생성한 아이템 화면 반환
    } // 아이템 화면 생성 종료
    private void Initialize(ConsumableInventoryRuntime source) // 실제 아이템 보관함 연결
    { // 아이템 보관함 연결 시작
        inventory = source; // 실제 회차 소모품 보관함 저장
        GameObject slots = new GameObject("Slots", typeof(RectTransform), typeof(GridLayoutGroup)); // 아이템 칸 격자 생성
        slots.transform.SetParent(transform, false); // 아이템 패널 안에 격자 연결
        RectTransform gridRect = slots.transform as RectTransform; // 격자 영역 조회
        gridRect.anchorMin = Vector2.zero; // 격자 왼쪽 아래 최소 앵커
        gridRect.anchorMax = Vector2.one; // 격자 오른쪽 위 최대 앵커
        gridRect.offsetMin = new Vector2(6f, 6f); // 격자 왼쪽 아래 여백
        gridRect.offsetMax = new Vector2(-6f, -6f); // 격자 오른쪽 위 여백
        GridLayoutGroup grid = slots.GetComponent<GridLayoutGroup>(); // 실제 아이템 칸 격자 조회
        grid.cellSize = new Vector2(58f, 58f); // 정사각형 아이템 칸 크기
        grid.spacing = new Vector2(4f, 4f); // 아이템 칸 사이 여백
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft; // 왼쪽 위부터 아이템 배치
        grid.startAxis = GridLayoutGroup.Axis.Horizontal; // 가로 방향 우선 배치
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 가로 칸 수 고정
        grid.constraintCount = ConsumableInventoryRuntime.ColumnCount; // 실제 보관함의 가로 다섯 칸 적용
        for (int index = 0; index < ConsumableInventoryRuntime.SlotCount; index++) // 실제 보관함의 열 칸 순회
        { // 아이템 칸 생성 시작
            CreateSlot(slots.transform, index); // 현재 순번의 아이템 칸 생성
        } // 아이템 칸 생성 종료
        CreateTooltip(); // 아이템 이름 안내 화면 생성
        inventory.Changed += Refresh; // 획득과 이동 및 삭제 화면 갱신 연결
        Refresh(); // 현재 보유 아이템 최초 표시
    } // 아이템 보관함 연결 종료
    private void CreateSlot(Transform parent, int index) // 단일 아이템 칸 생성
    { // 아이템 칸 생성 시작
        GameObject slot = new GameObject("Slot_" + (index + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ExplorationInventorySlotView)); // 단일 아이템 칸 생성
        slot.transform.SetParent(parent, false); // 실제 격자에 아이템 칸 연결
        Image frame = slot.GetComponent<Image>(); // 아이템 칸 배경 조회
        frame.color = new Color(0.04f, 0.07f, 0.09f, 1f); // 이미지 누락 시 대체 배경
        BattleUiArt.Apply(frame, "CostBadge"); // 기존 금속 정사각형 프레임 적용
        frame.raycastTarget = true; // 아이템 이름 확인용 마우스 진입 허용
        slot.GetComponent<ExplorationInventorySlotView>().Bind(this, index); // 칸 순번과 이름 안내 연결
        GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 실제 아이템 이미지 생성
        iconObject.transform.SetParent(slot.transform, false); // 아이템 칸 안에 이미지 연결
        Image icon = iconObject.GetComponent<Image>(); // 실제 아이템 이미지 조회
        icon.preserveAspect = true; // 아이템 원본 이미지 비율 유지
        icon.raycastTarget = false; // 아이템 칸 마우스 입력 통과
        Stretch(icon.rectTransform, 5f); // 아이템 칸 테두리 안쪽에 이미지 배치
        icons[index] = icon; // 현재 칸의 아이템 이미지 저장
        GameObject missingObject = new GameObject("MissingIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 이미지가 없는 아이템 표시 생성
        missingObject.transform.SetParent(slot.transform, false); // 아이템 칸 안에 대체 표시 연결
        TMP_Text missing = missingObject.GetComponent<TMP_Text>(); // 대체 표시 글자 조회
        missing.font = ProjectCFontProvider.KoreanFontAsset; // 기존 한글 글꼴 적용
        missing.fontSize = 30f; // 이미지 누락 안내 크기
        missing.text = "?"; // 이미지 누락 아이템 구분 표시
        missing.color = Color.white; // 이미지 누락 안내 색상
        missing.alignment = TextAlignmentOptions.Center; // 대체 표시 중앙 정렬
        missing.raycastTarget = false; // 아이템 칸 마우스 입력 통과
        Stretch(missing.rectTransform, 4f); // 대체 표시를 아이템 칸 안에 배치
        missingIcons[index] = missing; // 현재 칸의 대체 표시 저장
    } // 아이템 칸 생성 종료
    private void CreateTooltip() // 아이템 이름 안내 화면 생성
    { // 아이템 이름 안내 생성 시작
        tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 아이템 이름 안내 배경 생성
        tooltip.transform.SetParent(transform, false); // 아이템 패널 아래에 안내 연결
        RectTransform area = tooltip.transform as RectTransform; // 이름 안내 영역 조회
        area.anchorMin = new Vector2(0f, 0f); // 패널 왼쪽 아래 최소 앵커
        area.anchorMax = new Vector2(0f, 0f); // 패널 왼쪽 아래 최대 앵커
        area.pivot = new Vector2(0f, 1f); // 안내 화면 왼쪽 위 기준점
        area.anchoredPosition = new Vector2(0f, -6f); // 아이템 칸 바로 아래 안내 위치
        area.sizeDelta = new Vector2(316f, 70f); // 아이템 이름 안내 크기
        Image background = tooltip.GetComponent<Image>(); // 안내 배경 이미지 조회
        background.color = new Color(0.02f, 0.04f, 0.06f, 0.98f); // 안내 글자 대비 배경
        background.raycastTarget = false; // 탐사 화면 입력 통과
        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 아이템 안내 글자 생성
        label.transform.SetParent(tooltip.transform, false); // 안내 배경 안에 글자 연결
        tooltipText = label.GetComponent<TMP_Text>(); // 실제 아이템 안내 글자 조회
        tooltipText.font = ProjectCFontProvider.KoreanFontAsset; // 기존 한글 글꼴 적용
        tooltipText.fontSize = 18f; // 아이템 이름 안내 글자 크기
        tooltipText.color = Color.white; // 아이템 이름 안내 색상
        tooltipText.alignment = TextAlignmentOptions.MidlineLeft; // 아이템 안내 왼쪽 정렬
        tooltipText.textWrappingMode = TextWrappingModes.Normal; // 아이템 설명 줄바꿈 설정
        tooltipText.raycastTarget = false; // 탐사 화면 입력 통과
        Stretch(tooltipText.rectTransform, 8f); // 안내 배경 안쪽에 글자 배치
        tooltip.SetActive(false); // 아이템 안내 시작 숨김
    } // 아이템 이름 안내 생성 종료
    public void ShowTooltip(int index) // 마우스가 가리킨 아이템 이름 표시
    { // 아이템 이름 표시 시작
        hoveredIndex = index; // 현재 마우스 위치의 칸 순번 저장
        ConsumableItemData item = inventory.GetItem(index); // 실제 보관 아이템 조회
        if (item == null) // 빈 아이템 칸 확인
        { // 빈 칸 안내 처리 시작
            tooltip.SetActive(false); // 빈 칸의 아이템 안내 숨김
            return; // 아이템 이름 표시 중단
        } // 빈 칸 안내 처리 종료
        tooltipText.text = string.IsNullOrWhiteSpace(item.Description) ? item.DisplayName : item.DisplayName + "\n" + item.Description; // 실제 아이템 이름과 설명 표시
        tooltip.SetActive(true); // 실제 아이템 안내 표시
    } // 아이템 이름 표시 종료
    public void HideTooltip(int index) // 마우스가 벗어난 아이템 이름 숨김
    { // 아이템 이름 숨김 시작
        if (hoveredIndex != index) // 현재 가리킨 칸과 일치 여부 확인
        { // 다른 칸 이탈 처리 시작
            return; // 다른 아이템 안내 유지
        } // 다른 칸 이탈 처리 종료
        hoveredIndex = -1; // 현재 마우스 위치의 칸 정보 제거
        tooltip.SetActive(false); // 아이템 이름 안내 숨김
    } // 아이템 이름 숨김 종료
    private void Refresh() // 실제 보관함 변경에 따른 아이템 화면 갱신
    { // 아이템 화면 갱신 시작
        for (int index = 0; index < icons.Length; index++) // 전체 아이템 칸 순회
        { // 단일 아이템 칸 갱신 시작
            ConsumableItemData item = inventory.GetItem(index); // 현재 칸의 실제 아이템 조회
            icons[index].sprite = item != null ? item.Icon : null; // 실제 아이템 아이콘 연결
            icons[index].enabled = item != null && item.Icon != null; // 아이콘이 있는 보유 아이템만 표시
            missingIcons[index].gameObject.SetActive(item != null && item.Icon == null); // 이미지가 없는 보유 아이템만 대체 표시
        } // 단일 아이템 칸 갱신 종료
        if (hoveredIndex >= 0) // 현재 마우스가 가리킨 칸 확인
        { // 현재 아이템 안내 갱신 시작
            ShowTooltip(hoveredIndex); // 이동과 삭제에 따른 이름 안내 갱신
        } // 현재 아이템 안내 갱신 종료
    } // 아이템 화면 갱신 종료
    private static void Stretch(RectTransform area, float inset) // 아이템 칸 안쪽 전체 배치
    { // 전체 배치 시작
        area.anchorMin = Vector2.zero; // 왼쪽 아래 최소 앵커 설정
        area.anchorMax = Vector2.one; // 오른쪽 위 최대 앵커 설정
        area.offsetMin = new Vector2(inset, inset); // 왼쪽 아래 안쪽 여백
        area.offsetMax = new Vector2(-inset, -inset); // 오른쪽 위 안쪽 여백
    } // 전체 배치 종료
    private void OnDestroy() // 탐사 아이템 화면 제거
    { // 아이템 화면 정리 시작
        if (inventory != null) // 실제 보관함 연결 확인
        { // 보관함 연결 해제 시작
            inventory.Changed -= Refresh; // 보관함 변경 이벤트 연결 해제
        } // 보관함 연결 해제 종료
    } // 아이템 화면 정리 종료
} // 탐사 아이템 화면 종료
