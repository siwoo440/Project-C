using System; // 기본 인터페이스 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.InputSystem; // 새 입력 시스템 사용
using UnityEngine.UI; // 유니티 UI 기능 사용

public sealed class ConsumableSlotBarView : MonoBehaviour, IDisposable // 왼쪽 위 공용 소모품 슬롯 화면
{
    private static readonly Color PanelColor = new Color(0.035f, 0.035f, 0.045f, 0.94f); // 소모품 패널 배경색
    private static readonly Color EmptySlotColor = new Color(0.09f, 0.09f, 0.11f, 0.94f); // 빈 슬롯 배경색
    private static readonly Color OccupiedSlotColor = new Color(0.15f, 0.15f, 0.18f, 0.98f); // 점유 슬롯 배경색
    private static readonly Color MoveSelectedColor = new Color(0.75f, 0.58f, 0.12f, 1f); // 이동 선택 강조색
    private static readonly Color UseSelectedColor = new Color(0.18f, 0.55f, 0.85f, 1f); // 사용 선택 강조색

    private readonly Button[] slotButtons = new Button[ConsumableInventoryRuntime.SlotCount]; // 슬롯 버튼 목록
    private readonly Image[] slotBackgrounds = new Image[ConsumableInventoryRuntime.SlotCount]; // 슬롯 배경 목록
    private readonly Image[] slotIcons = new Image[ConsumableInventoryRuntime.SlotCount]; // 슬롯 아이콘 목록
    private readonly TMP_Text[] slotNames = new TMP_Text[ConsumableInventoryRuntime.SlotCount]; // 슬롯 이름 목록
    private BattleConsumableController controller; // 전투 소모품 관리자
    private ConsumableInventoryRuntime inventory; // 공용 소모품 보관함
    private TMP_Text modeText; // Alt 정리 상태 안내
    private bool altRearrangeMode; // 현재 Alt 정리 모드 여부
    private bool savedCursorVisible; // Alt 전 커서 표시 상태
    private CursorLockMode savedCursorLockMode; // Alt 전 커서 잠금 상태
    private bool cursorStateCaptured; // 커서 상태 저장 여부
    private bool disposed; // 화면 종료 여부

    public static ConsumableSlotBarView Create(Canvas parentCanvas, BattleConsumableController battleController, ConsumableInventoryRuntime consumableInventory) // 전투 Canvas 아래 슬롯 화면 생성
    {
        if (parentCanvas == null) // 부모 Canvas 확인
        {
            throw new ArgumentNullException(nameof(parentCanvas)); // 부모 Canvas 누락 예외
        }

        GameObject rootObject = new GameObject("ConsumableSlotBar", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(ConsumableSlotBarView)); // 소모품 화면 루트 생성
        rootObject.transform.SetParent(parentCanvas.transform, false); // 전투 Canvas 자식 배치
        RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 루트 사각형 조회
        Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 루트 전체 화면 배치
        Canvas slotCanvas = rootObject.GetComponent<Canvas>(); // 소모품 전용 Canvas 조회
        slotCanvas.overrideSorting = true; // 부모 Canvas 정렬 분리
        slotCanvas.sortingOrder = 420; // 전투 일반 UI 위 정렬
        ConsumableSlotBarView slotBarView = rootObject.GetComponent<ConsumableSlotBarView>(); // 소모품 화면 컴포넌트 조회
        slotBarView.Initialize(battleController, consumableInventory); // 소모품 화면 초기화
        return slotBarView; // 생성 화면 반환
    }

    private void Initialize(BattleConsumableController battleController, ConsumableInventoryRuntime consumableInventory) // 슬롯 화면 초기화
    {
        controller = battleController ?? throw new ArgumentNullException(nameof(battleController)); // 전투 관리자 저장
        inventory = consumableInventory ?? throw new ArgumentNullException(nameof(consumableInventory)); // 보관함 저장
        CreatePanel(); // 왼쪽 위 5 곱하기 2 패널 생성
        inventory.Changed += RefreshAllSlots; // 슬롯 변경 화면 갱신 연결
        controller.SelectionChanged += RefreshAllSlots; // 선택 상태 화면 갱신 연결
        RefreshAllSlots(); // 첫 슬롯 표시
    }

    private void Update() // Alt 정리 모드와 커서 상태 갱신
    {
        bool currentAlt = Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed); // 현재 Alt 입력 확인
        if (currentAlt == altRearrangeMode) // Alt 상태 변화 여부 확인
        {
            return; // 변경 없음 종료
        }

        altRearrangeMode = currentAlt; // 현재 Alt 정리 상태 저장
        if (altRearrangeMode) // Alt 정리 모드 시작 확인
        {
            CaptureAndUnlockCursor(); // 커서 상태 저장 후 활성화
        }
        else // Alt 정리 모드 종료 처리
        {
            controller?.CancelMoveSelection(); // 미완료 이동 선택 취소
            RestoreCursor(); // 기존 커서 상태 복구
        }

        RefreshModeText(); // 정리 모드 안내 갱신
        RefreshAllSlots(); // 슬롯 강조 상태 갱신
    }

    private void CreatePanel() // 소모품 패널 생성
    {
        Image panelImage = CreateImage("ConsumablePanel", transform, PanelColor); // 패널 배경 생성
        RectTransform panelRect = panelImage.rectTransform; // 패널 사각형 조회
        panelRect.anchorMin = new Vector2(0f, 1f); // 왼쪽 위 최소 앵커 적용
        panelRect.anchorMax = new Vector2(0f, 1f); // 왼쪽 위 최대 앵커 적용
        panelRect.pivot = new Vector2(0f, 1f); // 왼쪽 위 피벗 적용
        panelRect.sizeDelta = new Vector2(590f, 225f); // 전체 패널 크기 적용
        panelRect.anchoredPosition = new Vector2(20f, -20f); // 왼쪽 위 화면 여백 적용

        TMP_Text titleText = CreateText("Title", panelImage.transform, 19f, Color.white, TextAlignmentOptions.Left); // 소모품 제목 생성
        titleText.text = "소모품"; // 소모품 제목 적용
        SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(180f, 30f), new Vector2(12f, -8f), new Vector2(0f, 1f)); // 제목 위치 적용

        modeText = CreateText("Mode", panelImage.transform, 15f, new Color(0.85f, 0.72f, 0.25f, 1f), TextAlignmentOptions.Right); // Alt 안내 텍스트 생성
        SetRect(modeText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(360f, 30f), new Vector2(-12f, -8f), new Vector2(1f, 1f)); // Alt 안내 위치 적용
        RefreshModeText(); // 초기 Alt 안내 적용

        GameObject gridObject = new GameObject("Slots", typeof(RectTransform), typeof(GridLayoutGroup)); // 10칸 슬롯 부모 생성
        gridObject.transform.SetParent(panelImage.transform, false); // 패널 자식 배치
        RectTransform gridRect = gridObject.GetComponent<RectTransform>(); // 슬롯 부모 사각형 조회
        Stretch(gridRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 12f), new Vector2(-12f, -42f)); // 슬롯 영역 패널 내부 배치
        GridLayoutGroup gridLayout = gridObject.GetComponent<GridLayoutGroup>(); // 슬롯 GridLayout 조회
        gridLayout.cellSize = new Vector2(103f, 78f); // 슬롯 칸 크기 적용
        gridLayout.spacing = new Vector2(10f, 8f); // 슬롯 간격 적용
        gridLayout.padding = new RectOffset(0, 0, 0, 0); // 슬롯 내부 여백 제거
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft; // 왼쪽 위부터 배치
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal; // 가로 우선 배치
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 수 사용
        gridLayout.constraintCount = ConsumableInventoryRuntime.ColumnCount; // 가로 다섯 칸 적용

        for (int index = 0; index < ConsumableInventoryRuntime.SlotCount; index++) // 열 개 슬롯 생성
        {
            CreateSlot(gridObject.transform, index); // 현재 슬롯 UI 생성
        }
    }

    private void CreateSlot(Transform parent, int slotIndex) // 단일 소모품 슬롯 생성
    {
        Button slotButton = CreateButton($"Slot_{slotIndex + 1}", parent, OccupiedSlotColor); // 슬롯 버튼 생성
        int capturedIndex = slotIndex; // 클릭 인덱스 안전 복사
        slotButton.onClick.AddListener(() => controller.HandleSlotClick(capturedIndex, altRearrangeMode)); // 슬롯 클릭 처리 연결
        slotButtons[slotIndex] = slotButton; // 슬롯 버튼 저장
        slotBackgrounds[slotIndex] = slotButton.GetComponent<Image>(); // 슬롯 배경 저장

        TMP_Text numberText = CreateText("Number", slotButton.transform, 12f, new Color(0.75f, 0.75f, 0.78f, 1f), TextAlignmentOptions.TopLeft); // 슬롯 번호 텍스트 생성
        numberText.text = (slotIndex + 1).ToString(); // 일부터 십 슬롯 번호 적용
        Stretch(numberText.rectTransform, new Vector2(0f, 0.7f), new Vector2(0.3f, 1f), new Vector2(4f, 0f), new Vector2(0f, -2f)); // 번호 왼쪽 위 배치

        Image iconImage = CreateImage("Icon", slotButton.transform, new Color(0.16f, 0.16f, 0.19f, 1f)); // 소모품 아이콘 영역 생성
        RectTransform iconRect = iconImage.rectTransform; // 아이콘 사각형 조회
        iconRect.anchorMin = new Vector2(0.5f, 1f); // 아이콘 상단 중앙 앵커 적용
        iconRect.anchorMax = new Vector2(0.5f, 1f); // 아이콘 상단 중앙 앵커 적용
        iconRect.pivot = new Vector2(0.5f, 1f); // 아이콘 상단 중앙 피벗 적용
        iconRect.sizeDelta = new Vector2(48f, 48f); // 아이콘 크기 적용
        iconRect.anchoredPosition = new Vector2(0f, -6f); // 아이콘 위치 적용
        iconImage.raycastTarget = false; // 아이콘 클릭 차단 해제
        iconImage.preserveAspect = true; // 아이콘 비율 유지
        slotIcons[slotIndex] = iconImage; // 슬롯 아이콘 저장

        TMP_Text nameText = CreateText("Name", slotButton.transform, 11f, Color.white, TextAlignmentOptions.Center); // 소모품 이름 텍스트 생성
        Stretch(nameText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.28f), new Vector2(3f, 1f), new Vector2(-3f, -1f)); // 이름 하단 배치
        nameText.textWrappingMode = TextWrappingModes.NoWrap; // 한 줄 이름 표시 적용
        nameText.overflowMode = TextOverflowModes.Ellipsis; // 긴 이름 말줄임 적용
        slotNames[slotIndex] = nameText; // 슬롯 이름 저장
    }

    private void RefreshAllSlots() // 열 개 슬롯 전체 갱신
    {
        if (disposed || inventory == null) // 갱신 가능 상태 확인
        {
            return; // 슬롯 갱신 중단
        }

        for (int index = 0; index < ConsumableInventoryRuntime.SlotCount; index++) // 전체 슬롯 순회
        {
            ConsumableItemData itemData = inventory.GetItem(index); // 현재 슬롯 소모품 조회
            Image backgroundImage = slotBackgrounds[index]; // 현재 슬롯 배경 조회
            if (backgroundImage == null) // 슬롯 UI 생성 여부 확인
            {
                continue; // 다음 슬롯 이동
            }

            backgroundImage.color = itemData == null ? EmptySlotColor : OccupiedSlotColor; // 빈칸과 점유칸 배경 적용
            if (controller.SelectedMoveSlot == index) // Alt 이동 원본 선택 확인
            {
                backgroundImage.color = MoveSelectedColor; // 이동 선택 강조 적용
            }
            else if (controller.SelectedUseSlot == index) // 포션 대상 선택 확인
            {
                backgroundImage.color = UseSelectedColor; // 사용 선택 강조 적용
            }

            Image iconImage = slotIcons[index]; // 현재 슬롯 아이콘 조회
            TMP_Text nameText = slotNames[index]; // 현재 슬롯 이름 조회
            iconImage.sprite = itemData == null ? null : itemData.Icon; // 소모품 아이콘 적용
            iconImage.color = itemData == null ? new Color(0.16f, 0.16f, 0.19f, 1f) : Color.white; // 빈칸과 아이콘 색상 적용
            nameText.text = itemData == null ? string.Empty : itemData.DisplayName; // 소모품 이름 적용
        }
    }

    private void RefreshModeText() // Alt 정리 안내 갱신
    {
        if (modeText == null) // 안내 텍스트 생성 여부 확인
        {
            return; // 안내 갱신 중단
        }

        modeText.text = altRearrangeMode ? "ALT 정리 모드 · 아이템 → 대상 칸" : "Alt를 누른 채 슬롯 이동"; // 현재 정리 모드 안내 적용
    }

    private void CaptureAndUnlockCursor() // Alt 커서 활성화
    {
        if (!cursorStateCaptured) // 기존 커서 상태 저장 여부 확인
        {
            savedCursorVisible = Cursor.visible; // 기존 커서 표시 상태 저장
            savedCursorLockMode = Cursor.lockState; // 기존 커서 잠금 상태 저장
            cursorStateCaptured = true; // 커서 상태 저장 완료 표시
        }

        Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
        Cursor.visible = true; // 커서 화면 표시
    }

    private void RestoreCursor() // Alt 이전 커서 상태 복구
    {
        if (!cursorStateCaptured) // 저장 커서 상태 존재 확인
        {
            return; // 커서 복구 중단
        }

        Cursor.lockState = savedCursorLockMode; // 기존 커서 잠금 복구
        Cursor.visible = savedCursorVisible; // 기존 커서 표시 복구
        cursorStateCaptured = false; // 커서 상태 저장 해제
    }

    private static Image CreateImage(string objectName, Transform parent, Color color) // 공용 이미지 생성
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(parent, false); // 지정 부모 자식 배치
        Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
        image.color = color; // 이미지 색상 적용
        return image; // 생성 이미지 반환
    }

    private static Button CreateButton(string objectName, Transform parent, Color color) // 공용 버튼 생성
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 오브젝트 생성
        buttonObject.transform.SetParent(parent, false); // 지정 부모 자식 배치
        Image buttonImage = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 조회
        buttonImage.color = color; // 버튼 배경색 적용
        Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회
        button.targetGraphic = buttonImage; // 버튼 그래픽 연결
        return button; // 생성 버튼 반환
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

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // 사각형 앵커 배치
    {
        rectTransform.anchorMin = anchorMin; // 최소 앵커 적용
        rectTransform.anchorMax = anchorMax; // 최대 앵커 적용
        rectTransform.offsetMin = offsetMin; // 최소 여백 적용
        rectTransform.offsetMax = offsetMax; // 최대 여백 적용
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Vector2 pivot) // 고정 사각형 배치
    {
        rectTransform.anchorMin = anchorMin; // 최소 앵커 적용
        rectTransform.anchorMax = anchorMax; // 최대 앵커 적용
        rectTransform.sizeDelta = sizeDelta; // 사각형 크기 적용
        rectTransform.anchoredPosition = anchoredPosition; // 사각형 위치 적용
        rectTransform.pivot = pivot; // 사각형 피벗 적용
    }

    public void Dispose() // 슬롯 화면 이벤트 연결 해제
    {
        if (disposed) // 기존 해제 여부 확인
        {
            return; // 중복 해제 중단
        }

        disposed = true; // 화면 종료 상태 저장
        if (inventory != null) // 보관함 존재 확인
        {
            inventory.Changed -= RefreshAllSlots; // 슬롯 변경 연결 해제
        }
        if (controller != null) // 전투 관리자 존재 확인
        {
            controller.SelectionChanged -= RefreshAllSlots; // 선택 변경 연결 해제
        }
        RestoreCursor(); // Alt 커서 상태 복구
    }

    private void OnDestroy() // 슬롯 화면 오브젝트 제거 처리
    {
        Dispose(); // 이벤트와 커서 상태 정리
    }
}
