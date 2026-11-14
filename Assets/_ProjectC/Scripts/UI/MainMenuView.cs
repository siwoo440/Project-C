using TMPro; // 메뉴 글자 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.Events; // 버튼 이벤트 기능 사용
using UnityEngine.UI; // 메뉴 UI 기능 사용
public sealed class MainMenuView : MonoBehaviour // 메인 메뉴 화면 구성
{ // 클래스 시작
    [SerializeField] private Button newGameButton; // 새로하기 버튼
    [SerializeField] private Button continueButton; // 이어하기 버튼
    [SerializeField] private Button settingsButton; // 설정 버튼
    [SerializeField] private Button quitButton; // 나가기 버튼
    public Button NewGameButton => newGameButton; // 첫 선택 버튼 조회
    public static MainMenuView Build(Canvas canvas) // 메인 메뉴 화면 생성
    { // 생성 시작
        GameObject root = new GameObject("MainMenuUI", typeof(RectTransform), typeof(MainMenuView)); // 메뉴 화면 생성
        root.transform.SetParent(canvas.transform, false); // 캔버스 연결
        RectTransform rootRect = root.GetComponent<RectTransform>(); // 메뉴 사각형 조회
        rootRect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 앵커
        rootRect.anchorMax = Vector2.one; // 화면 오른쪽 위 앵커
        rootRect.offsetMin = Vector2.zero; // 왼쪽 아래 여백 제거
        rootRect.offsetMax = Vector2.zero; // 오른쪽 위 여백 제거
        MainMenuView view = root.GetComponent<MainMenuView>(); // 메뉴 컴포넌트 조회
        view.CreateArtwork(); // 배경과 로고 생성
        Sprite buttonArt = Resources.Load<Sprite>("UI/MainMenu/ButtonFrame"); // 버튼 장식 조회
        view.newGameButton = view.CreateButton("NewGameButton", "새로하기", 0f, buttonArt); // 새로하기 버튼 생성
        view.continueButton = view.CreateButton("ContinueButton", "이어하기", -105f, buttonArt); // 이어하기 버튼 생성
        view.settingsButton = view.CreateButton("SettingsButton", "설정", -210f, buttonArt); // 설정 버튼 생성
        view.quitButton = view.CreateButton("QuitButton", "나가기", -315f, buttonArt); // 나가기 버튼 생성
        view.continueButton.interactable = false; // 저장 기능 연결 전 이어하기 비활성
        view.SetContinueLabel(false); // 비활성 글자 표시
        return view; // 생성 화면 반환
    } // 생성 종료
    public void Bind(UnityAction newGame, UnityAction continueGame, UnityAction settings, UnityAction quit, bool canContinue) // 메뉴 동작 연결
    { // 연결 시작
        BindButton(newGameButton, newGame); // 새로하기 동작 연결
        BindButton(continueButton, continueGame); // 이어하기 동작 연결
        BindButton(settingsButton, settings); // 설정 동작 연결
        BindButton(quitButton, quit); // 나가기 동작 연결
        continueButton.interactable = canContinue && continueGame != null; // 실제 이어하기 가능 여부 적용
        SetContinueLabel(continueButton.interactable); // 이어하기 글자 상태 적용
    } // 연결 종료
    private static void BindButton(Button button, UnityAction action) // 개별 버튼 동작 연결
    { // 연결 시작
        button.onClick.RemoveAllListeners(); // 이전 실행 연결 제거
        if (action != null) // 연결 동작 존재 확인
        { // 실행 연결 시작
            button.onClick.AddListener(action); // 버튼 실행 연결
        } // 실행 연결 종료
    } // 연결 종료
    private void CreateArtwork() // 메뉴 배경과 로고 생성
    { // 이미지 생성 시작
        Image background = CreateImage("MenuBackground", Resources.Load<Sprite>("UI/MainMenu/Background")); // 배경 이미지 생성
        background.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // 배경 최소 앵커
        background.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); // 배경 최대 앵커
        AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>(); // 배경 비율 유지 기능 추가
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // 화면 전체 채우기
        fitter.aspectRatio = 1672f / 941f; // 배경 원본 비율 설정
        Image logo = CreateImage("GameLogo", Resources.Load<Sprite>("UI/MainMenu/Logo")); // 수정 로고 생성
        logo.rectTransform.anchorMin = new Vector2(0.205f, 0.76f); // 로고 최소 앵커
        logo.rectTransform.anchorMax = new Vector2(0.205f, 0.76f); // 로고 최대 앵커
        logo.rectTransform.sizeDelta = new Vector2(720f, 368f); // 로고 표시 크기
        logo.preserveAspect = true; // 로고 원본 비율 유지
    } // 이미지 생성 종료
    private Image CreateImage(string objectName, Sprite sprite) // 메뉴 이미지 생성
    { // 이미지 생성 시작
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(transform, false); // 메뉴 화면 자식 배치
        Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
        image.sprite = sprite; // 전용 이미지 연결
        image.raycastTarget = false; // 배경과 장식 입력 차단 해제
        image.color = Color.white; // 원본 이미지 색상 유지
        return image; // 생성 이미지 반환
    } // 이미지 생성 종료
    private Button CreateButton(string objectName, string label, float verticalOffset, Sprite sprite) // 메뉴 버튼 생성
    { // 버튼 생성 시작
        Image image = CreateImage(objectName, sprite); // 버튼 장식 생성
        image.raycastTarget = true; // 버튼 입력 활성화
        image.preserveAspect = true; // 버튼 장식 비율 유지
        RectTransform rect = image.rectTransform; // 버튼 사각형 조회
        rect.anchorMin = new Vector2(0.205f, 0.48f); // 버튼 최소 앵커
        rect.anchorMax = new Vector2(0.205f, 0.48f); // 버튼 최대 앵커
        rect.anchoredPosition = new Vector2(0f, verticalOffset); // 버튼 세로 위치
        rect.sizeDelta = new Vector2(560f, 86f); // 버튼 크기
        Button button = image.gameObject.AddComponent<Button>(); // 클릭 기능 추가
        button.targetGraphic = image; // 선택 상태 표시 이미지 연결
        ColorBlock colors = button.colors; // 버튼 상태 색상 조회
        colors.highlightedColor = new Color(1f, 0.92f, 0.72f, 1f); // 강조 상태 색상
        colors.selectedColor = new Color(1f, 0.92f, 0.72f, 1f); // 키보드 선택 상태 색상
        colors.pressedColor = new Color(0.72f, 0.67f, 0.56f, 1f); // 눌림 상태 색상
        colors.disabledColor = new Color(0.48f, 0.48f, 0.54f, 0.8f); // 비활성 상태 색상
        button.colors = colors; // 버튼 상태 색상 적용
        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 버튼 문구 생성
        textObject.transform.SetParent(image.transform, false); // 버튼 문구 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 문구 사각형 조회
        textRect.anchorMin = Vector2.zero; // 문구 최소 앵커
        textRect.anchorMax = Vector2.one; // 문구 최대 앵커
        textRect.offsetMin = new Vector2(70f, 0f); // 왼쪽 장식 여백
        textRect.offsetMax = new Vector2(-70f, 0f); // 오른쪽 장식 여백
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 문구 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 연결
        text.fontSize = 28f; // 메뉴 글자 크기
        text.fontStyle = FontStyles.Bold; // 메뉴 글자 굵기
        text.color = new Color(1f, 0.93f, 0.76f, 1f); // 메뉴 글자 색상
        text.alignment = TextAlignmentOptions.Center; // 문구 가운데 정렬
        text.text = label; // 실제 한글 문구 적용
        text.raycastTarget = false; // 글자의 버튼 입력 차단 해제
        return button; // 생성 버튼 반환
    } // 버튼 생성 종료
    private void SetContinueLabel(bool available) // 이어하기 문구 상태 적용
    { // 상태 적용 시작
        TMP_Text text = continueButton.GetComponentInChildren<TMP_Text>(); // 이어하기 문구 조회
        text.alpha = available ? 1f : 0.45f; // 이어하기 문구 밝기 적용
    } // 상태 적용 종료
} // 클래스 종료
