using System; // 버튼 동작 사용
using TMPro; // 한글 문구 사용
using UnityEngine; // 로비 화면 객체 사용
using UnityEngine.UI; // 로비 버튼과 배경 사용

public sealed class LobbyLayoutView : MonoBehaviour // 로비 자원과 주요 메뉴 화면
{ // 로비 화면 시작
    private PlayerResourceManager resources; // 현재 자원 관리자
    private TMP_Text goldText; // 골드 표시 문구
    private TMP_Text screwText; // 나사 표시 문구
    private TMP_Text ironText; // 철판 표시 문구
    private TMP_Text wireText; // 전선 표시 문구
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
        Image goldIcon = CreateIcon("GoldIcon", transform, "Gold"); // 골드 아이콘 생성
        Place(goldIcon.rectTransform, new Vector2(0.76f, 0.93f), new Vector2(-120f, 0f), new Vector2(60f, 60f)); // 골드 아이콘 위치 설정
        goldText = CreateText("Gold", transform, string.Empty, 28f); // 골드 표시 생성
        Place(goldText.rectTransform, new Vector2(0.76f, 0.93f), new Vector2(35f, 0f), new Vector2(220f, 70f)); // 골드 상단 위치 설정
        goldText.alignment = TextAlignmentOptions.MidlineRight; // 골드 오른쪽 정렬
        GameObject group = new GameObject("Resources", typeof(RectTransform)); // 물자 표시 전체 영역 생성
        group.transform.SetParent(transform, false); // 로비 화면 자식 배치
        Place(group.GetComponent<RectTransform>(), new Vector2(0.23f, 0.055f), Vector2.zero, new Vector2(700f, 74f)); // 물자 하단 위치 설정
        screwText = CreateResource(group.transform, "Screw", "Screw", 0); // 독립 나사 영역 생성
        ironText = CreateResource(group.transform, "IronPlate", "Iron", 1); // 독립 철판 영역 생성
        wireText = CreateResource(group.transform, "Wire", "Wire", 2); // 독립 전선 영역 생성
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
        screwText.text = "나사  " + resources.Screw; // 현재 나사 수량 표시
        ironText.text = "철판  " + resources.IronPlate; // 현재 철판 수량 표시
        wireText.text = "전선  " + resources.Wire; // 현재 전선 수량 표시
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
        LobbyUiArt.Apply(image, "Panel"); // 제작한 자원 배경 이미지 적용
        image.raycastTarget = false; // 자원 배경 입력 차단 해제
    } // 자원 배경 생성 종료
    private TMP_Text CreateResource(Transform parent, string objectName, string imageName, int index) // 개별 물자 영역 생성
    { // 개별 물자 생성 시작
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 개별 물자 배경 생성
        target.transform.SetParent(parent, false); // 물자 전체 영역 연결
        RectTransform rect = target.GetComponent<RectTransform>(); // 개별 물자 영역 조회
        rect.anchorMin = new Vector2(index / 3f, 0f); // 물자 시작 앵커 설정
        rect.anchorMax = new Vector2((index + 1) / 3f, 1f); // 물자 끝 앵커 설정
        rect.offsetMin = new Vector2(3f, 0f); // 물자 왼쪽 간격 설정
        rect.offsetMax = new Vector2(-3f, 0f); // 물자 오른쪽 간격 설정
        Image background = target.GetComponent<Image>(); // 물자 배경 조회
        LobbyUiArt.Apply(background, "Panel"); // 제작한 물자 배경 적용
        background.raycastTarget = false; // 물자 배경 입력 차단 해제
        Image icon = CreateIcon("Icon", target.transform, imageName); // 물자 종류별 아이콘 생성
        Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(56f, 56f)); // 정사각형 아이콘 위치 설정
        TMP_Text value = CreateText("Value", target.transform, string.Empty, 22f); // 물자 수량 문구 생성
        RectTransform valueRect = value.rectTransform; // 물자 수량 영역 조회
        valueRect.anchorMin = new Vector2(0f, 0f); // 수량 시작 앵커 설정
        valueRect.anchorMax = new Vector2(1f, 1f); // 수량 끝 앵커 설정
        valueRect.offsetMin = new Vector2(67f, 0f); // 아이콘 오른쪽 문구 여백 설정
        valueRect.offsetMax = new Vector2(-8f, 0f); // 수량 오른쪽 여백 설정
        return value; // 물자 수량 문구 반환
    } // 개별 물자 생성 종료
    private static Image CreateIcon(string objectName, Transform parent, string imageName) // 정사각형 자원 아이콘 생성
    { // 자원 아이콘 생성 시작
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 아이콘 화면 객체 생성
        target.transform.SetParent(parent, false); // 부모 자원 영역 연결
        Image icon = target.GetComponent<Image>(); // 아이콘 이미지 조회
        LobbyUiArt.Apply(icon, imageName); // 자원 종류별 이미지 적용
        icon.preserveAspect = true; // 아이콘 원본 비율 유지
        icon.raycastTarget = false; // 아이콘 입력 차단 해제
        return icon; // 생성 아이콘 반환
    } // 자원 아이콘 생성 종료
    private void CreateButton(string objectName, string label, Vector2 anchor, Vector2 size, Action action) // 로비 메뉴 버튼 생성
    { // 메뉴 버튼 생성 시작
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 메뉴 버튼 객체 생성
        target.transform.SetParent(transform, false); // 로비 화면 자식 배치
        Place(target.GetComponent<RectTransform>(), anchor, Vector2.zero, size); // 메뉴 버튼 위치 설정
        Image image = target.GetComponent<Image>(); // 버튼 배경 조회
        LobbyUiArt.Apply(image, "Button"); // 제작한 로비 버튼 이미지 적용
        Button button = target.GetComponent<Button>(); // 버튼 입력 조회
        button.targetGraphic = image; // 버튼 색상 대상 연결
        ColorBlock colors = button.colors; // 버튼 상태별 색상 조회
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f); // 미연결 버튼 이미지 밝기 설정
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
