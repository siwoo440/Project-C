using TMPro; // 메뉴 글자 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.Events; // 버튼 이벤트 기능 사용
using UnityEngine.EventSystems; // 키보드 선택 기능 사용
using UnityEngine.InputSystem; // 취소 키 입력 사용
using UnityEngine.UI; // 메뉴 UI 기능 사용
public sealed class MainMenuView : MonoBehaviour // 메인 메뉴 화면 구성
{ // 클래스 시작
    [SerializeField] private Button newGameButton; // 새로하기 버튼
    [SerializeField] private Button continueButton; // 이어하기 버튼
    [SerializeField] private Button settingsButton; // 설정 버튼
    [SerializeField] private Button quitButton; // 나가기 버튼
    private GameObject confirmationPanel; // 새 게임 경고 패널
    private Button confirmButton; // 새 게임 확인 버튼
    private Button cancelButton; // 새 게임 취소 버튼
    private UnityAction confirmedAction; // 확인 시 실행할 동작
    private readonly bool[] previousButtonStates = new bool[4]; // 경고 전 메뉴 버튼 상태
    public Button NewGameButton => newGameButton; // 첫 선택 버튼 조회
    public void ShowNewGameConfirmation(UnityAction action) // 새 게임 경고 표시
    { // 경고 표시 시작
        if (confirmationPanel == null) // 경고 화면 생성 여부 확인
        { // 경고 화면 생성 시작
            CreateConfirmationPanel(); // 경고 화면 생성
        } // 경고 화면 생성 종료
        if (confirmationPanel.activeSelf) // 이미 열린 경고 확인
        { // 중복 열기 처리 시작
            return; // 중복 실행 중단
        } // 중복 열기 처리 종료
        confirmedAction = action; // 확인 동작 보관
        Button[] buttons = { newGameButton, continueButton, settingsButton, quitButton }; // 메뉴 버튼 목록 준비
        for (int index = 0; index < buttons.Length; index++) // 전체 메뉴 버튼 순회
        { // 메뉴 입력 차단 시작
            previousButtonStates[index] = buttons[index].interactable; // 기존 입력 상태 보관
            buttons[index].interactable = false; // 경고 중 메뉴 입력 차단
        } // 메뉴 입력 차단 종료
        confirmationPanel.SetActive(true); // 경고 패널 표시
        confirmationPanel.transform.SetAsLastSibling(); // 경고 화면 최상단 배치
        if (EventSystem.current != null) // 입력 관리자 존재 확인
        { // 취소 버튼 선택 시작
            EventSystem.current.SetSelectedGameObject(cancelButton.gameObject); // 취소 버튼 기본 선택
        } // 취소 버튼 선택 종료
    } // 경고 표시 종료
    private void Update() // 경고 취소 입력 확인
    { // 취소 입력 확인 시작
        if (confirmationPanel != null && confirmationPanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // 열린 경고와 취소 키 확인
        { // 키보드 취소 시작
            HideNewGameConfirmation(); // 경고 패널 닫기
        } // 키보드 취소 종료
    } // 취소 입력 확인 종료
    private void CreateConfirmationPanel() // 새 게임 경고 화면 생성
    { // 경고 화면 생성 시작
        confirmationPanel = new GameObject("NewGameConfirmation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 전체 화면 경고 배경 생성
        confirmationPanel.transform.SetParent(transform, false); // 메뉴 화면 자식 배치
        RectTransform overlay = confirmationPanel.GetComponent<RectTransform>(); // 경고 배경 영역 조회
        overlay.anchorMin = Vector2.zero; // 배경 시작 앵커 설정
        overlay.anchorMax = Vector2.one; // 배경 끝 앵커 설정
        overlay.offsetMin = Vector2.zero; // 배경 시작 여백 제거
        overlay.offsetMax = Vector2.zero; // 배경 끝 여백 제거
        confirmationPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 화면 차단 배경 색상
        GameObject backdropObject = new GameObject("WarningBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 경고 상자 배경 생성
        backdropObject.transform.SetParent(confirmationPanel.transform, false); // 경고 배경 자식 배치
        RectTransform backdrop = backdropObject.GetComponent<RectTransform>(); // 경고 상자 영역 조회
        backdrop.anchorMin = new Vector2(0.5f, 0.5f); // 상자 시작 앵커 설정
        backdrop.anchorMax = new Vector2(0.5f, 0.5f); // 상자 끝 앵커 설정
        backdrop.sizeDelta = new Vector2(740f, 340f); // 경고 상자 크기 설정
        backdropObject.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.18f, 1f); // 경고 상자 색상
        GameObject messageObject = new GameObject("WarningMessage", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 경고 문구 생성
        messageObject.transform.SetParent(confirmationPanel.transform, false); // 경고 문구 자식 배치
        RectTransform messageRect = messageObject.GetComponent<RectTransform>(); // 경고 문구 영역 조회
        messageRect.anchorMin = new Vector2(0.5f, 0.5f); // 문구 시작 앵커 설정
        messageRect.anchorMax = new Vector2(0.5f, 0.5f); // 문구 끝 앵커 설정
        messageRect.anchoredPosition = new Vector2(0f, 42f); // 문구 위치 설정
        messageRect.sizeDelta = new Vector2(670f, 200f); // 문구 영역 크기 설정
        TMP_Text message = messageObject.GetComponent<TMP_Text>(); // 경고 문구 컴포넌트 조회
        message.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 연결
        message.fontSize = 26f; // 경고 글자 크기 설정
        message.enableAutoSizing = true; // 작은 화면 글자 자동 조정
        message.fontSizeMin = 18f; // 최소 글자 크기 설정
        message.fontSizeMax = 26f; // 최대 글자 크기 설정
        message.alignment = TextAlignmentOptions.Center; // 경고 문구 가운데 정렬
        message.text = "새로 시작하시겠습니까?\n현재 진행 상황과 저장되지 않은 데이터가 사라지고 처음부터 시작합니다.\n기존 저장 데이터도 새 게임으로 덮어씁니다."; // 새 게임 초기화 경고 문구
        message.raycastTarget = false; // 문구의 입력 차단 해제
        confirmButton = CreateConfirmationButton("ConfirmNewGame", "새로 시작", -120f); // 새 게임 확인 버튼 생성
        cancelButton = CreateConfirmationButton("CancelNewGame", "취소", 120f); // 새 게임 취소 버튼 생성
        confirmButton.onClick.AddListener(ConfirmNewGame); // 확인 동작 연결
        cancelButton.onClick.AddListener(HideNewGameConfirmation); // 취소 동작 연결
        confirmationPanel.SetActive(false); // 생성 직후 경고 숨김
    } // 경고 화면 생성 종료
    private Button CreateConfirmationButton(string objectName, string label, float horizontalOffset) // 경고 선택 버튼 생성
    { // 경고 버튼 생성 시작
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 화면 객체 생성
        buttonObject.transform.SetParent(confirmationPanel.transform, false); // 경고 화면 자식 배치
        RectTransform rect = buttonObject.GetComponent<RectTransform>(); // 버튼 영역 조회
        rect.anchorMin = new Vector2(0.5f, 0.5f); // 버튼 시작 앵커 설정
        rect.anchorMax = new Vector2(0.5f, 0.5f); // 버튼 끝 앵커 설정
        rect.anchoredPosition = new Vector2(horizontalOffset, -110f); // 버튼 위치 설정
        rect.sizeDelta = new Vector2(200f, 64f); // 버튼 크기 설정
        Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
        image.color = new Color(0.32f, 0.40f, 0.45f, 1f); // 버튼 배경 색상
        Button button = buttonObject.GetComponent<Button>(); // 버튼 입력 기능 조회
        button.targetGraphic = image; // 버튼 상태 색상 연결
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 버튼 글자 생성
        labelObject.transform.SetParent(buttonObject.transform, false); // 버튼 글자 자식 배치
        RectTransform labelRect = labelObject.GetComponent<RectTransform>(); // 버튼 글자 영역 조회
        labelRect.anchorMin = Vector2.zero; // 글자 시작 앵커 설정
        labelRect.anchorMax = Vector2.one; // 글자 끝 앵커 설정
        labelRect.offsetMin = Vector2.zero; // 글자 시작 여백 제거
        labelRect.offsetMax = Vector2.zero; // 글자 끝 여백 제거
        TMP_Text text = labelObject.GetComponent<TMP_Text>(); // 버튼 글자 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 연결
        text.fontSize = 24f; // 버튼 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 버튼 글자 가운데 정렬
        text.text = label; // 버튼 문구 설정
        text.raycastTarget = false; // 글자의 입력 차단 해제
        return button; // 생성 버튼 반환
    } // 경고 버튼 생성 종료
    private void ConfirmNewGame() // 새 게임 확인 처리
    { // 새 게임 확인 시작
        UnityAction action = confirmedAction; // 확인 동작 임시 보관
        HideNewGameConfirmation(); // 경고 패널 닫기
        action?.Invoke(); // 새 게임 동작 실행
    } // 새 게임 확인 종료
    private void HideNewGameConfirmation() // 새 게임 경고 닫기
    { // 경고 닫기 시작
        confirmationPanel.SetActive(false); // 경고 패널 숨김
        confirmedAction = null; // 확인 동작 제거
        Button[] buttons = { newGameButton, continueButton, settingsButton, quitButton }; // 메뉴 버튼 목록 준비
        for (int index = 0; index < buttons.Length; index++) // 전체 메뉴 버튼 순회
        { // 메뉴 입력 복원 시작
            buttons[index].interactable = previousButtonStates[index]; // 이전 입력 상태 복원
        } // 메뉴 입력 복원 종료
        if (EventSystem.current != null) // 입력 관리자 존재 확인
        { // 메뉴 버튼 선택 시작
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject); // 새로하기 버튼 선택
        } // 메뉴 버튼 선택 종료
    } // 경고 닫기 종료
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
