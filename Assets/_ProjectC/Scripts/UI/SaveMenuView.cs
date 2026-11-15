using System; // 슬롯 동작 위임 사용
using TMPro; // 한글 화면 문자 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 버튼과 이미지 사용

public sealed class SaveMenuView : MonoBehaviour // 저장·불러오기 화면
{ // 저장 화면 시작
    public const int SlotCount = 5; // 자동 저장과 수동 슬롯 수
    private readonly SlotElements[] slots = new SlotElements[SlotCount]; // 슬롯 화면 구성 목록
    private Sprite backgroundSprite; // 런타임 배경 스프라이트
    private Sprite saveSprite; // 런타임 저장 버튼 스프라이트
    private Sprite loadSprite; // 런타임 불러오기 버튼 스프라이트
    private Sprite deleteSprite; // 런타임 삭제 버튼 스프라이트
    private sealed class SlotElements // 슬롯 화면 요소
    { // 슬롯 화면 요소 시작
        public TMP_Text description; // 슬롯 상태 문구
        public TMP_Text timestamp; // 슬롯 저장 시각
        public Button loadButton; // 슬롯 불러오기 버튼
        public Button deleteButton; // 슬롯 삭제 버튼
    } // 슬롯 화면 요소 종료
    public static SaveMenuView Create(Canvas canvas, Action<int> onSave, Action<int> onLoad, Action<int> onDelete, Action onClose) // 저장 화면 생성
    { // 저장 화면 생성 시작
        if (canvas == null) // 부모 캔버스 존재 확인
        { // 캔버스 누락 처리 시작
            throw new ArgumentNullException(nameof(canvas)); // 캔버스 누락 오류
        } // 캔버스 누락 처리 종료
        GameObject root = new GameObject("SaveMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SaveMenuView)); // 전체 저장 화면 생성
        root.transform.SetParent(canvas.transform, false); // 캔버스 자식 배치
        RectTransform rect = root.GetComponent<RectTransform>(); // 전체 화면 영역 조회
        rect.anchorMin = Vector2.zero; // 화면 시작 앵커 설정
        rect.anchorMax = Vector2.one; // 화면 끝 앵커 설정
        rect.offsetMin = Vector2.zero; // 화면 시작 여백 제거
        rect.offsetMax = Vector2.zero; // 화면 끝 여백 제거
        root.GetComponent<Image>().color = Color.black; // 배경 바깥 검은 화면 설정
        SaveMenuView view = root.GetComponent<SaveMenuView>(); // 저장 화면 컴포넌트 조회
        view.Initialize(onSave, onLoad, onDelete, onClose); // 화면 이미지와 슬롯 구성
        return view; // 생성 화면 반환
    } // 저장 화면 생성 종료
    public void SetSlotState(int index, string description, string timestamp, bool occupied) // 슬롯 저장 상태 표시
    { // 슬롯 상태 표시 시작
        if (index < 0 || index >= slots.Length) // 슬롯 번호 범위 확인
        { // 잘못된 슬롯 처리 시작
            throw new ArgumentOutOfRangeException(nameof(index)); // 슬롯 번호 오류
        } // 잘못된 슬롯 처리 종료
        SlotElements slot = slots[index]; // 대상 슬롯 요소 조회
        slot.description.text = occupied ? description ?? string.Empty : "데이터 없음"; // 저장 상태 또는 빈 슬롯 문구 표시
        slot.timestamp.text = occupied ? timestamp ?? string.Empty : string.Empty; // 저장 시각 또는 빈 문구 표시
        slot.loadButton.interactable = occupied; // 저장이 있을 때 불러오기 활성화
        slot.deleteButton.interactable = occupied; // 저장이 있을 때 삭제 활성화
    } // 슬롯 상태 표시 종료
    private void Initialize(Action<int> onSave, Action<int> onLoad, Action<int> onDelete, Action onClose) // 저장 화면 내용 구성
    { // 화면 구성 시작
        backgroundSprite = CreateSprite("UI/SaveMenu/Background"); // 종이 배경 이미지 준비
        saveSprite = CreateSprite("UI/SaveMenu/Save"); // 저장 버튼 이미지 준비
        loadSprite = CreateSprite("UI/SaveMenu/Load"); // 불러오기 버튼 이미지 준비
        deleteSprite = CreateSprite("UI/SaveMenu/Delete"); // 삭제 버튼 이미지 준비
        RectTransform backgroundRect = CreateRect("Background", transform); // 배경 이미지 영역 생성
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f); // 배경 시작 앵커 설정
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f); // 배경 끝 앵커 설정
        Image background = backgroundRect.gameObject.AddComponent<Image>(); // 배경 이미지 컴포넌트 추가
        background.sprite = backgroundSprite; // 종이 배경 이미지 연결
        background.raycastTarget = false; // 배경 이미지 입력 차단 해제
        AspectRatioFitter aspect = backgroundRect.gameObject.AddComponent<AspectRatioFitter>(); // 배경 원본 비율 기능 추가
        aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // 화면 전체 채우기 설정
        aspect.aspectRatio = 1672f / 941f; // 제작 배경 원본 비율 설정
        TMP_Text title = CreateText("Title", transform, "여행자의 기록", 40f); // 저장 화면 제목 생성
        SetFixedRect(title.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(900f, 72f)); // 화면 제목 위치 설정
        title.fontStyle = FontStyles.Bold; // 화면 제목 굵기 설정
        for (int index = 0; index < SlotCount; index++) // 전체 저장 슬롯 순회
        { // 슬롯 생성 시작
            CreateSlot(index, onSave, onLoad, onDelete); // 개별 저장 슬롯 생성
            SetSlotState(index, null, null, false); // 슬롯 초기 빈 상태 표시
        } // 슬롯 생성 종료
        RectTransform closeRect = CreateRect("CloseButton", transform); // 닫기 버튼 영역 생성
        SetFixedRect(closeRect, new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(220f, 62f)); // 닫기 버튼 위치 설정
        Image closeImage = closeRect.gameObject.AddComponent<Image>(); // 닫기 버튼 배경 생성
        closeImage.color = new Color(0.11f, 0.10f, 0.09f, 0.86f); // 닫기 버튼 색상 설정
        Button closeButton = closeRect.gameObject.AddComponent<Button>(); // 닫기 버튼 기능 추가
        closeButton.targetGraphic = closeImage; // 닫기 버튼 배경 연결
        closeButton.onClick.AddListener(() => onClose?.Invoke()); // 닫기 동작 연결
        TMP_Text closeLabel = CreateText("Label", closeRect, "닫기", 26f); // 닫기 버튼 문구 생성
        Stretch(closeLabel.rectTransform); // 닫기 버튼 문구 영역 채우기
        closeLabel.color = new Color(0.90f, 0.87f, 0.80f, 1f); // 닫기 문구 색상 설정
    } // 화면 구성 종료
    private void CreateSlot(int index, Action<int> onSave, Action<int> onLoad, Action<int> onDelete) // 개별 저장 슬롯 생성
    { // 저장 슬롯 생성 시작
        float anchorY = 0.79f - index * 0.125f; // 슬롯별 세로 위치 계산
        RectTransform row = CreateRect("Slot_" + index, transform); // 저장 슬롯 영역 생성
        row.anchorMin = new Vector2(0.16f, anchorY); // 슬롯 시작 앵커 설정
        row.anchorMax = new Vector2(0.84f, anchorY); // 슬롯 끝 앵커 설정
        row.offsetMin = new Vector2(0f, -48f); // 슬롯 아래 여백 설정
        row.offsetMax = new Vector2(0f, 48f); // 슬롯 위 여백 설정
        TMP_Text nameText = CreateText("SlotName", row, index == 0 ? "자동 저장" : "슬롯 " + index, 26f); // 슬롯 이름 생성
        SetHorizontalRect(nameText.rectTransform, 0f, 0.12f); // 슬롯 이름 위치 설정
        RectTransform previewRect = CreateRect("Thumbnail", row); // 미리보기 영역 생성
        previewRect.anchorMin = new Vector2(0.12f, 0.17f); // 미리보기 시작 앵커 설정
        previewRect.anchorMax = new Vector2(0.22f, 0.83f); // 미리보기 끝 앵커 설정
        previewRect.offsetMin = new Vector2(4f, 2f); // 미리보기 시작 여백 설정
        previewRect.offsetMax = new Vector2(-4f, -2f); // 미리보기 끝 여백 설정
        Image preview = previewRect.gameObject.AddComponent<Image>(); // 미리보기 배경 추가
        preview.color = new Color(0.13f, 0.12f, 0.11f, 0.35f); // 빈 미리보기 색상 설정
        preview.raycastTarget = false; // 미리보기 입력 차단 해제
        TMP_Text description = CreateText("Description", row, "데이터 없음", 26f); // 저장 상태 문구 생성
        SetHorizontalRect(description.rectTransform, 0.24f, 0.50f); // 저장 상태 문구 위치 설정
        description.alignment = TextAlignmentOptions.MidlineLeft; // 저장 상태 왼쪽 정렬
        TMP_Text timestamp = CreateText("Timestamp", row, string.Empty, 22f); // 저장 시각 문구 생성
        SetHorizontalRect(timestamp.rectTransform, 0.50f, 0.70f); // 저장 시각 위치 설정
        timestamp.alignment = TextAlignmentOptions.MidlineLeft; // 저장 시각 왼쪽 정렬
        Button saveButton = CreateSlotButton("SaveButton", row, saveSprite, "저장", 0.75f); // 저장 버튼 생성
        Button loadButton = CreateSlotButton("LoadButton", row, loadSprite, "불러오기", 0.84f); // 불러오기 버튼 생성
        Button deleteButton = CreateSlotButton("DeleteButton", row, deleteSprite, "삭제", 0.93f); // 삭제 버튼 생성
        saveButton.onClick.AddListener(() => onSave?.Invoke(index)); // 저장 슬롯 동작 연결
        loadButton.onClick.AddListener(() => onLoad?.Invoke(index)); // 불러오기 슬롯 동작 연결
        deleteButton.onClick.AddListener(() => onDelete?.Invoke(index)); // 삭제 슬롯 동작 연결
        slots[index] = new SlotElements // 슬롯 화면 요소 생성
        { // 슬롯 화면 요소 설정 시작
            description = description, // 저장 상태 문구 연결
            timestamp = timestamp, // 저장 시각 문구 연결
            loadButton = loadButton, // 불러오기 버튼 연결
            deleteButton = deleteButton // 삭제 버튼 연결
        }; // 슬롯 화면 요소 설정 종료
    } // 저장 슬롯 생성 종료
    private static Button CreateSlotButton(string objectName, Transform parent, Sprite sprite, string label, float anchorX) // 슬롯 기능 버튼 생성
    { // 슬롯 버튼 생성 시작
        RectTransform rect = CreateRect(objectName, parent); // 슬롯 버튼 영역 생성
        SetFixedRect(rect, new Vector2(anchorX, 0.58f), Vector2.zero, new Vector2(86f, 78f)); // 슬롯 버튼 위치와 크기 설정
        Image image = rect.gameObject.AddComponent<Image>(); // 버튼 이미지 생성
        image.sprite = sprite; // 제작 버튼 이미지 연결
        image.preserveAspect = true; // 버튼 이미지 비율 유지
        Button button = rect.gameObject.AddComponent<Button>(); // 버튼 입력 기능 추가
        button.targetGraphic = image; // 버튼 상태 색상 연결
        TMP_Text labelText = CreateText("Label", rect, label, 17f); // 버튼 기능 문구 생성
        SetFixedRect(labelText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(100f, 24f)); // 버튼 문구 위치 설정
        return button; // 생성 버튼 반환
    } // 슬롯 버튼 생성 종료
    private static RectTransform CreateRect(string objectName, Transform parent) // 화면 영역 생성
    { // 화면 영역 생성 시작
        GameObject target = new GameObject(objectName, typeof(RectTransform)); // 화면 영역 객체 생성
        RectTransform rect = target.GetComponent<RectTransform>(); // 화면 영역 컴포넌트 조회
        rect.SetParent(parent, false); // 부모 화면 연결
        return rect; // 생성 영역 반환
    } // 화면 영역 생성 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string value, float fontSize) // 화면 문구 생성
    { // 화면 문구 생성 시작
        RectTransform rect = CreateRect(objectName, parent); // 화면 문구 영역 생성
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>(); // 문자 컴포넌트 추가
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 연결
        text.fontSize = fontSize; // 글자 크기 설정
        text.enableAutoSizing = true; // 긴 문구 크기 자동 조정
        text.fontSizeMin = 12f; // 최소 글자 크기 설정
        text.fontSizeMax = fontSize; // 최대 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 문구 가운데 정렬
        text.color = new Color(0.08f, 0.07f, 0.06f, 1f); // 종이 위 어두운 글자 색상
        text.text = value; // 표시 문구 설정
        text.raycastTarget = false; // 글자의 입력 차단 해제
        return text; // 생성 문구 반환
    } // 화면 문구 생성 종료
    private static void SetFixedRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) // 고정 크기 영역 설정
    { // 고정 영역 설정 시작
        rect.anchorMin = anchor; // 시작 앵커 설정
        rect.anchorMax = anchor; // 끝 앵커 설정
        rect.anchoredPosition = position; // 기준 위치 설정
        rect.sizeDelta = size; // 영역 크기 설정
    } // 고정 영역 설정 종료
    private static void SetHorizontalRect(RectTransform rect, float left, float right) // 슬롯 내부 가로 영역 설정
    { // 가로 영역 설정 시작
        rect.anchorMin = new Vector2(left, 0f); // 가로 시작 앵커 설정
        rect.anchorMax = new Vector2(right, 1f); // 가로 끝 앵커 설정
        rect.offsetMin = Vector2.zero; // 시작 여백 제거
        rect.offsetMax = Vector2.zero; // 끝 여백 제거
    } // 가로 영역 설정 종료
    private static void Stretch(RectTransform rect) // 부모 영역 전체 채우기
    { // 전체 채우기 시작
        rect.anchorMin = Vector2.zero; // 시작 앵커 설정
        rect.anchorMax = Vector2.one; // 끝 앵커 설정
        rect.offsetMin = Vector2.zero; // 시작 여백 제거
        rect.offsetMax = Vector2.zero; // 끝 여백 제거
    } // 전체 채우기 종료
    private static Sprite CreateSprite(string path) // 리소스 이미지로 스프라이트 생성
    { // 스프라이트 생성 시작
        Texture2D texture = Resources.Load<Texture2D>(path); // 제작 이미지 리소스 조회
        if (texture == null) // 이미지 누락 확인
        { // 이미지 누락 처리 시작
            throw new InvalidOperationException("저장 화면 이미지를 찾을 수 없습니다: " + path); // 이미지 누락 오류
        } // 이미지 누락 처리 종료
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 원본 이미지 스프라이트 반환
    } // 스프라이트 생성 종료
    private void OnDestroy() // 런타임 이미지 정리
    { // 이미지 정리 시작
        ReleaseSprite(backgroundSprite); // 배경 스프라이트 제거
        ReleaseSprite(saveSprite); // 저장 버튼 스프라이트 제거
        ReleaseSprite(loadSprite); // 불러오기 버튼 스프라이트 제거
        ReleaseSprite(deleteSprite); // 삭제 버튼 스프라이트 제거
    } // 이미지 정리 종료
    private static void ReleaseSprite(Sprite sprite) // 생성 스프라이트 제거
    { // 스프라이트 제거 시작
        if (sprite == null) // 스프라이트 존재 확인
        { // 스프라이트 없음 처리 시작
            return; // 정리 중단
        } // 스프라이트 없음 처리 종료
        if (Application.isPlaying) // 게임 실행 상태 확인
        { // 실행 중 제거 시작
            Destroy(sprite); // 프레임 종료 시 제거
        } // 실행 중 제거 종료
        else // 편집기 테스트 상태 확인
        { // 편집기 제거 시작
            DestroyImmediate(sprite); // 테스트 스프라이트 즉시 제거
        } // 편집기 제거 종료
    } // 스프라이트 제거 종료
} // 저장 화면 종료
