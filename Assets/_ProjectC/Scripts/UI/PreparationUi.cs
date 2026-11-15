using System; // 화면 버튼 동작 사용
using TMPro; // 한글 화면 문구 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.UI; // 화면 이미지와 버튼 사용

public static class PreparationUi // 탐사 준비 화면 공통 구성
{ // 탐사 준비 화면 도구 시작
    public static RectTransform CreateOverlay(Canvas canvas, string name) // 전체 화면 배경 생성
    { // 전체 화면 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 전체 화면 객체 생성
        target.transform.SetParent(canvas.transform, false); // 부모 캔버스 연결
        RectTransform rect = target.GetComponent<RectTransform>(); // 전체 화면 영역 조회
        Stretch(rect, Vector2.zero, Vector2.one); // 전체 화면 영역 채우기
        target.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.055f, 0.97f); // 어두운 탐사 준비 배경 설정
        return rect; // 생성 화면 영역 반환
    } // 전체 화면 생성 종료
    public static RectTransform CreatePanel(string name, Transform parent, Vector2 minimum, Vector2 maximum) // 화면 패널 생성
    { // 패널 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 화면 패널 객체 생성
        target.transform.SetParent(parent, false); // 부모 화면 연결
        RectTransform rect = target.GetComponent<RectTransform>(); // 패널 영역 조회
        Stretch(rect, minimum, maximum); // 패널 영역 설정
        target.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.15f, 0.95f); // 어두운 패널 색상 설정
        return rect; // 생성 패널 영역 반환
    } // 패널 생성 종료
    public static TMP_Text CreateText(string name, Transform parent, string value, float size) // 한글 화면 문구 생성
    { // 화면 문구 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 문구 객체 생성
        target.transform.SetParent(parent, false); // 부모 화면 연결
        TMP_Text text = target.GetComponent<TMP_Text>(); // 문구 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 연결
        text.fontSize = size; // 문구 크기 설정
        text.enableAutoSizing = true; // 긴 문구 자동 크기 조정
        text.fontSizeMin = 14f; // 최소 문구 크기 설정
        text.fontSizeMax = size; // 최대 문구 크기 설정
        text.color = new Color(0.95f, 0.92f, 0.83f, 1f); // 밝은 문구 색상 설정
        text.alignment = TextAlignmentOptions.Center; // 문구 가운데 정렬
        text.raycastTarget = false; // 문구 입력 차단 해제
        text.text = value; // 표시 문구 설정
        return text; // 생성 문구 반환
    } // 화면 문구 생성 종료
    public static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, Action action) // 준비 화면 버튼 생성
    { // 준비 버튼 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 객체 생성
        target.transform.SetParent(parent, false); // 부모 화면 연결
        Place(target.GetComponent<RectTransform>(), anchor, position, size); // 버튼 위치와 크기 설정
        Image image = target.GetComponent<Image>(); // 버튼 이미지 조회
        LobbyUiArt.Apply(image, "Button"); // 공통 금속 버튼 이미지 적용
        Button button = target.GetComponent<Button>(); // 버튼 입력 조회
        button.targetGraphic = image; // 버튼 색상 대상 연결
        ColorBlock colors = button.colors; // 버튼 상태별 색상 조회
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f); // 비활성 버튼 색상 설정
        button.colors = colors; // 버튼 상태 색상 적용
        button.interactable = action != null; // 버튼 동작 가능 여부 설정
        if (action != null) // 버튼 동작 존재 확인
        { // 버튼 동작 연결 시작
            button.onClick.AddListener(() => action()); // 버튼 클릭 동작 연결
        } // 버튼 동작 연결 종료
        TMP_Text text = CreateText("Label", target.transform, label, 26f); // 버튼 문구 생성
        Stretch(text.rectTransform, Vector2.zero, Vector2.one); // 버튼 문구 영역 채우기
        return button; // 생성 버튼 반환
    } // 준비 버튼 생성 종료
    public static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum) // 부모 기준 영역 설정
    { // 영역 설정 시작
        rect.anchorMin = minimum; // 시작 앵커 설정
        rect.anchorMax = maximum; // 끝 앵커 설정
        rect.offsetMin = Vector2.zero; // 시작 여백 제거
        rect.offsetMax = Vector2.zero; // 끝 여백 제거
    } // 영역 설정 종료
    public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) // 고정 화면 위치 설정
    { // 위치 설정 시작
        rect.anchorMin = anchor; // 시작 앵커 설정
        rect.anchorMax = anchor; // 끝 앵커 설정
        rect.anchoredPosition = position; // 화면 위치 설정
        rect.sizeDelta = size; // 화면 크기 설정
    } // 위치 설정 종료
} // 탐사 준비 화면 도구 종료
