using System; // 버튼 동작 사용
using TMPro; // 한글 문구 사용
using UnityEngine; // 로비 화면 객체 사용
using UnityEngine.UI; // 로비 버튼과 배경 사용

public sealed class LobbyLayoutView : MonoBehaviour // 로비 자원과 주요 메뉴 화면
{ // 로비 화면 시작
    private PlayerResourceManager resources; // 현재 자원 관리자
    private TMP_Text goldText; // 골드 표시 문구
    private TMP_Text resourceText; // 물자 표시 문구
    public static LobbyLayoutView Create(Canvas canvas, Action openSettings, Action openFacility) // 로비 화면 생성
    { // 로비 화면 생성 시작
        GameObject root = new GameObject("LobbyLayout", typeof(RectTransform), typeof(LobbyLayoutView)); // 로비 화면 객체 생성
        root.transform.SetParent(canvas.transform, false); // 로비 캔버스 연결
        RectTransform rect = root.GetComponent<RectTransform>(); // 로비 화면 영역 조회
        rect.anchorMin = Vector2.zero; // 화면 시작 앵커 설정
        rect.anchorMax = Vector2.one; // 화면 끝 앵커 설정
        rect.offsetMin = Vector2.zero; // 화면 시작 여백 제거
        rect.offsetMax = Vector2.zero; // 화면 끝 여백 제거
        LobbyLayoutView view = root.GetComponent<LobbyLayoutView>(); // 로비 화면 컴포넌트 조회
        view.Build(openSettings, openFacility); // 로비 자원과 버튼 배치
        return view; // 생성 화면 반환
    } // 로비 화면 생성 종료
    private void Build(Action openSettings, Action openFacility) // 로비 화면 요소 배치
    { // 화면 요소 배치 시작
        CreatePlate("GoldBackdrop", new Vector2(0.76f, 0.93f), new Vector2(330f, 76f)); // 골드 표시 배경 배치
        goldText = CreateText("Gold", transform, string.Empty, 28f); // 골드 표시 생성
        Place(goldText.rectTransform, new Vector2(0.76f, 0.93f), Vector2.zero, new Vector2(310f, 70f)); // 골드 상단 위치 설정
        goldText.alignment = TextAlignmentOptions.MidlineRight; // 골드 오른쪽 정렬
        CreatePlate("ResourceBackdrop", new Vector2(0.23f, 0.055f), new Vector2(700f, 74f)); // 물자 표시 배경 배치
        resourceText = CreateText("Resources", transform, string.Empty, 23f); // 물자 표시 생성
        Place(resourceText.rectTransform, new Vector2(0.23f, 0.055f), Vector2.zero, new Vector2(680f, 68f)); // 물자 하단 위치 설정
        CreateButton("HelpButton", "?", new Vector2(0.91f, 0.93f), new Vector2(70f, 70f), null); // 도움말 버튼 배치
        CreateButton("SettingsButton", "설정", new Vector2(0.965f, 0.93f), new Vector2(120f, 70f), openSettings); // 설정 버튼 배치
        CreateButton("CharacterButton", "캐릭터", new Vector2(0.88f, 0.73f), new Vector2(300f, 140f), null); // 캐릭터 버튼 배치
        CreateButton("FormationButton", "편성", new Vector2(0.88f, 0.55f), new Vector2(300f, 140f), null); // 편성 버튼 배치
        CreateButton("ExpeditionButton", "탐사", new Vector2(0.87f, 0.33f), new Vector2(350f, 180f), null); // 탐사 버튼 배치
        CreateButton("CodexButton", "도감", new Vector2(0.085f, 0.15f), new Vector2(230f, 65f), null); // 도감 버튼 배치
        CreateButton("FacilityButton", "설비 강화", new Vector2(0.51f, 0.055f), new Vector2(250f, 68f), openFacility); // 설비 강화 버튼 배치
        resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
        resources.ResourcesChanged += RefreshResources; // 자원 변경 이벤트 연결
        RefreshResources(); // 현재 자원 표시
    } // 화면 요소 배치 종료
    private void RefreshResources() // 실제 보유 자원 표시
    { // 자원 표시 시작
        goldText.text = "골드  " + resources.Gold; // 현재 골드 표시
        resourceText.text = "나사  " + resources.Screw + "     철판  " + resources.IronPlate + "     전선  " + resources.Wire; // 현재 물자 수량 표시
    } // 자원 표시 종료
    private void OnDestroy() // 로비 화면 정리
    { // 로비 정리 시작
        if (resources != null) // 자원 관리자 존재 확인
        { // 자원 이벤트 해제 시작
            resources.ResourcesChanged -= RefreshResources; // 자원 변경 이벤트 연결 해제
        } // 자원 이벤트 해제 종료
    } // 로비 정리 종료
    private void CreatePlate(string objectName, Vector2 anchor, Vector2 size) // 자원 표시 배경 생성
    { // 자원 배경 생성 시작
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 자원 배경 객체 생성
        target.transform.SetParent(transform, false); // 로비 화면 자식 배치
        Place(target.GetComponent<RectTransform>(), anchor, Vector2.zero, size); // 자원 배경 위치 설정
        Image image = target.GetComponent<Image>(); // 자원 배경 이미지 조회
        image.color = new Color(0.07f, 0.09f, 0.10f, 0.82f); // 어두운 자원 배경 설정
        image.raycastTarget = false; // 자원 배경 입력 차단 해제
    } // 자원 배경 생성 종료
    private void CreateButton(string objectName, string label, Vector2 anchor, Vector2 size, Action action) // 로비 메뉴 버튼 생성
    { // 메뉴 버튼 생성 시작
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 메뉴 버튼 객체 생성
        target.transform.SetParent(transform, false); // 로비 화면 자식 배치
        Place(target.GetComponent<RectTransform>(), anchor, Vector2.zero, size); // 메뉴 버튼 위치 설정
        Image image = target.GetComponent<Image>(); // 버튼 배경 조회
        image.color = new Color(0.13f, 0.16f, 0.18f, 0.92f); // 어두운 버튼 배경 설정
        Button button = target.GetComponent<Button>(); // 버튼 입력 조회
        button.targetGraphic = image; // 버튼 색상 대상 연결
        ColorBlock colors = button.colors; // 버튼 상태별 색상 조회
        colors.disabledColor = new Color(0.12f, 0.15f, 0.17f, 0.85f); // 미연결 버튼 배경 가독성 설정
        button.colors = colors; // 버튼 상태 색상 적용
        button.interactable = action != null; // 구현된 기능만 입력 허용
        if (action != null) // 연결할 버튼 동작 확인
        { // 버튼 동작 연결 시작
            button.onClick.AddListener(() => action()); // 메뉴 버튼 동작 연결
        } // 버튼 동작 연결 종료
        TMP_Text text = CreateText("Label", target.transform, label, 32f); // 버튼 문구 생성
        RectTransform textRect = text.rectTransform; // 버튼 문구 영역 조회
        textRect.anchorMin = Vector2.zero; // 문구 시작 앵커 설정
        textRect.anchorMax = Vector2.one; // 문구 끝 앵커 설정
        textRect.offsetMin = Vector2.zero; // 문구 시작 여백 제거
        textRect.offsetMax = Vector2.zero; // 문구 끝 여백 제거
    } // 메뉴 버튼 생성 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string value, float fontSize) // 로비 한글 문구 생성
    { // 로비 문구 생성 시작
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 로비 문구 객체 생성
        target.transform.SetParent(parent, false); // 부모 화면 연결
        TMP_Text text = target.GetComponent<TMP_Text>(); // 문구 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 연결
        text.fontSize = fontSize; // 글자 크기 설정
        text.enableAutoSizing = true; // 긴 문구 자동 크기 조정
        text.fontSizeMin = 16f; // 최소 글자 크기 설정
        text.fontSizeMax = fontSize; // 최대 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 문구 가운데 정렬
        text.color = new Color(0.95f, 0.92f, 0.83f, 1f); // 밝은 문구 색상 설정
        text.text = value; // 표시 문구 설정
        text.raycastTarget = false; // 글자 입력 차단 해제
        return text; // 생성 문구 반환
    } // 로비 문구 생성 종료
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) // 화면 위치와 크기 설정
    { // 화면 위치 설정 시작
        rect.anchorMin = anchor; // 시작 앵커 설정
        rect.anchorMax = anchor; // 끝 앵커 설정
        rect.anchoredPosition = position; // 화면 위치 설정
        rect.sizeDelta = size; // 화면 크기 설정
    } // 화면 위치 설정 종료
} // 로비 화면 종료
