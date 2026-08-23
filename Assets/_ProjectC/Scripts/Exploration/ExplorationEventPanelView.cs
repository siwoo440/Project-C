using System.Collections; // 코루틴 사용
using System.Collections.Generic; // 버튼 목록 사용
using TMPro; // 텍스트 메시 프로 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // uGUI 마우스 이벤트 시스템 사용
using UnityEngine.InputSystem.UI; // New Input System UI 입력 모듈 사용
using UnityEngine.UI; // UI 구성 요소 사용

public sealed class ExplorationEventPanelView : MonoBehaviour // 탐사 이벤트 패널 UI 표시기
{
    private const int BlurDownsample = 12; // 블러용 축소 비율

    private static ExplorationEventPanelView instance; // 싱글톤 인스턴스

    private readonly List<Button> choiceButtons =
        new List<Button>(); // 현재 선택지 버튼 목록

    private Canvas rootCanvas; // 루트 캔버스
    private RawImage blurBackgroundImage; // 블러 배경 이미지
    private Image dimOverlayImage; // 어두운 오버레이 이미지
    private RectTransform panelRoot; // 이벤트 패널 루트
    private TMP_Text titleText; // 이벤트 제목 텍스트
    private TMP_Text bodyText; // 이벤트 설명 텍스트
    private Image illustrationFrameImage; // 그림 프레임 이미지
    private Image illustrationImage; // 이벤트 그림 이미지
    private TMP_Text illustrationFallbackText; // 그림 대체 텍스트
    private RectTransform choiceContainer; // 선택지 버튼 컨테이너

    private Coroutine showCoroutine; // 현재 표시 코루틴
    private Texture2D blurTexture; // 현재 블러 텍스처
    private ExplorationEventView currentEventView; // 현재 열린 이벤트 뷰
    private ExplorationEventData currentEventData; // 현재 열린 이벤트 데이터
    private string currentRuntimeEventId; // 현재 열린 이벤트 런타임 ID
    private bool choiceCommitted; // 선택 완료 여부

    public static ExplorationEventPanelView Instance => instance; // 현재 이벤트 패널 조회

    public static ExplorationEventPanelView EnsureInstance() // 이벤트 패널 존재 보장
    {
        if (instance != null)
        {
            return instance; // 기존 이벤트 패널 반환
        }

        instance =
            FindFirstObjectByType<ExplorationEventPanelView>(); // Scene 기존 이벤트 패널 탐색

        if (instance != null)
        {
            return instance; // 탐색한 이벤트 패널 반환
        }

        GameObject panelObject =
            new GameObject("ExplorationEventPanelView"); // 이벤트 패널 오브젝트 생성

        instance =
            panelObject.AddComponent<ExplorationEventPanelView>(); // 이벤트 패널 컴포넌트 추가

        return instance; // 생성한 이벤트 패널 반환
    }

    private void Awake() // 이벤트 패널 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 이벤트 패널 제거
            return;
        }

        instance = this; // 현재 이벤트 패널 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 시 패널 유지
        EnsureUi(); // 이벤트 UI 구성 준비
        HidePanelImmediate(); // 시작 시 패널 숨김
    }

    public void ShowEvent(
        ExplorationEventView eventView,
        ExplorationEventData eventData,
        string runtimeEventId) // 이벤트 표시 요청
    {
        if (eventView == null ||
            eventData == null ||
            string.IsNullOrWhiteSpace(runtimeEventId))
        {
            return; // 잘못된 표시 요청 차단
        }

        EnsureUi(); // UI 구성 보장
        EnsureEventSystem(); // 선택지 버튼용 New Input System 이벤트 시스템 보장

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine); // 이전 표시 코루틴 중단
        }

        currentEventView = eventView; // 현재 이벤트 뷰 저장
        currentEventData = eventData; // 현재 이벤트 데이터 저장
        currentRuntimeEventId = runtimeEventId; // 현재 이벤트 ID 저장
        choiceCommitted = false; // 선택 완료 상태 초기화

        ExplorationPlayerController.SetInputBlocked(true); // 이벤트 중 플레이어 이동 잠금

        showCoroutine =
            StartCoroutine(ShowEventRoutine()); // 이벤트 표시 코루틴 실행
    }

    private IEnumerator ShowEventRoutine() // 블러 캡처 후 이벤트 패널 표시
    {
        panelRoot.gameObject.SetActive(false); // 패널 본문 임시 숨김
        rootCanvas.enabled = true; // 배경 캔버스 활성화
        dimOverlayImage.color = new Color(0f, 0f, 0f, 0.45f); // 캡처 전 어두운 오버레이 표시

        yield return new WaitForEndOfFrame(); // 현재 프레임 렌더 완료 대기

        ApplyBlurCapture(); // 화면 캡처 기반 블러 적용
        PopulateEventContent(); // 이벤트 패널 내용 구성

        panelRoot.gameObject.SetActive(true); // 패널 본문 표시
        dimOverlayImage.color = new Color(0f, 0f, 0f, 0.3f); // 블러 위 오버레이 완화
        showCoroutine = null; // 코루틴 참조 정리
    }

    private void PopulateEventContent() // 이벤트 텍스트와 버튼 구성
    {
        ClearChoiceButtons(); // 이전 선택지 버튼 제거

        titleText.text =
            currentEventData != null
                ? currentEventData.DisplayName
                : "이벤트"; // 이벤트 제목 설정

        bodyText.text =
            currentEventData != null
                ? currentEventData.Description
                : string.Empty; // 이벤트 설명 설정

        if (currentEventData != null &&
            currentEventData.IllustrationSprite != null)
        {
            illustrationImage.enabled = true; // 그림 이미지 사용 활성화
            illustrationImage.sprite = currentEventData.IllustrationSprite; // 그림 스프라이트 적용
            illustrationImage.color = Color.white; // 그림 색상 기본값 지정
            illustrationFallbackText.gameObject.SetActive(false); // 대체 문구 숨김
        }
        else
        {
            illustrationImage.enabled = false; // 그림 이미지 숨김
            illustrationFallbackText.gameObject.SetActive(true); // 대체 문구 표시
            illustrationFallbackText.text = "?"; // 대체 문구 지정
        }

        IReadOnlyList<ExplorationEventChoiceData> choices =
            currentEventData != null
                ? currentEventData.Choices
                : null; // 이벤트 선택지 목록 조회

        if (choices == null)
        {
            CreateCloseButton("[닫기]"); // 비정상 데이터용 닫기 버튼 생성
            return;
        }

        for (int index = 0;
             index < choices.Count;
             index++)
        {
            ExplorationEventChoiceData choiceData =
                choices[index]; // 현재 선택지 조회

            if (choiceData == null ||
                !choiceData.IsValidData())
            {
                continue; // 유효하지 않은 선택지 제외
            }

            int capturedIndex = index; // 클릭용 인덱스 복사

            CreateChoiceButton(
                choiceData.ChoiceText,
                () => HandleChoiceSelected(capturedIndex)); // 선택지 버튼 생성
        }

        if (choiceButtons.Count == 0)
        {
            CreateCloseButton("[닫기]"); // 버튼이 없으면 닫기 버튼 생성
        }
    }

    private void HandleChoiceSelected(int choiceIndex) // 선택지 클릭 처리
    {
        if (choiceCommitted ||
            currentEventData == null ||
            choiceIndex < 0 ||
            choiceIndex >= currentEventData.Choices.Count)
        {
            return; // 잘못된 클릭 또는 중복 선택 차단
        }

        choiceCommitted = true; // 선택 완료 상태 저장

        ExplorationEventChoiceData choiceData =
            currentEventData.Choices[choiceIndex]; // 선택한 선택지 조회

        string resultText =
            ResolveChoice(
                currentRuntimeEventId,
                choiceIndex,
                choiceData); // 선택 결과 계산

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        sessionManager.MarkEventResolved(currentRuntimeEventId); // 현재 이벤트 처리 완료 기록

        if (currentEventView != null)
        {
            currentEventView.LockInteraction(); // 현재 이벤트 상호작용 잠금 유지
        }

        bodyText.text = resultText; // 결과 문구로 본문 교체
        ClearChoiceButtons(); // 기존 선택지 버튼 제거
        CreateCloseButton("[확인]"); // 결과 확인 버튼 생성
    }

    private string ResolveChoice(
        string runtimeEventId,
        int choiceIndex,
        ExplorationEventChoiceData choiceData) // 선택지 결과 계산
    {
        if (choiceData == null)
        {
            return "아무 일도 일어나지 않았습니다."; // 잘못된 선택지 기본 문구 반환
        }

        if (!choiceData.HasRandomOutcome)
        {
            string directSummary =
                ApplyResourceChange(
                    choiceData.DirectChange); // 즉시 변화량 적용

            return BuildResultMessage(
                choiceData.ResultText,
                directSummary); // 즉시 결과 문구 반환
        }

        int seed =
            ComputeStableHash(runtimeEventId) ^
            (choiceIndex * 486187739); // 이벤트 ID 기반 확률 시드 생성

        System.Random random =
            new System.Random(seed); // 확률 판정 난수 생성기 준비

        int roll =
            random.Next(0, 100); // 0~99 범위 판정 값 생성

        bool isSuccess =
            roll < choiceData.SuccessChancePercent; // 성공 여부 판정

        string resultText =
            isSuccess
                ? choiceData.SuccessText
                : choiceData.FailureText; // 결과 문구 선택

        ExplorationEventResourceChange resultChange =
            isSuccess
                ? choiceData.SuccessChange
                : choiceData.FailureChange; // 결과 변화량 선택

        string randomSummary =
            ApplyResourceChange(resultChange); // 결과 변화량 적용

        return BuildResultMessage(resultText, randomSummary); // 최종 결과 문구 반환
    }

    private static int ComputeStableHash(string text) // 문자열 기반 고정 해시 계산
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0; // 빈 문자열 기본 해시 반환
        }

        int hash = 17; // 초기 해시값 설정

        for (int index = 0;
             index < text.Length;
             index++)
        {
            hash = unchecked(hash * 31 + text[index]); // 문자 누적 해시 계산
        }

        return hash; // 최종 해시 반환
    }

    private static string BuildResultMessage(
        string baseMessage,
        string resourceSummary) // 결과 문구와 자원 변화 문구 결합
    {
        string safeMessage =
            string.IsNullOrWhiteSpace(baseMessage)
                ? "이벤트가 마무리되었습니다."
                : baseMessage; // 기본 결과 문구 보정

        if (string.IsNullOrWhiteSpace(resourceSummary) ||
            resourceSummary == "변화 없음")
        {
            return safeMessage; // 자원 변화가 없으면 기본 문구만 반환
        }

        return safeMessage +
               "\n\n" +
               $"<color=#9FDFFF>결과 : {resourceSummary}</color>"; // 자원 변화 포함 결과 반환
    }

    private static string ApplyResourceChange(ExplorationEventResourceChange change) // 자원 변화량 적용
    {
        if (change == null ||
            change.IsEmpty())
        {
            return "변화 없음"; // 변화량이 없으면 기본 문구 반환
        }

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비

        int addGold = Mathf.Max(0, change.Gold); // 골드 증가량 계산
        int addScrew = Mathf.Max(0, change.Screw); // 나사 증가량 계산
        int addIronPlate = Mathf.Max(0, change.IronPlate); // 철판 증가량 계산
        int addWire = Mathf.Max(0, change.Wire); // 전선 증가량 계산

        if (addGold > 0 ||
            addScrew > 0 ||
            addIronPlate > 0 ||
            addWire > 0)
        {
            resourceManager.AddResources(
                addGold,
                addScrew,
                addIronPlate,
                addWire); // 증가 자원 즉시 지급
        }

        int spendGold = Mathf.Min(resourceManager.Gold, Mathf.Max(0, -change.Gold)); // 골드 감소량 계산
        int spendScrew = Mathf.Min(resourceManager.Screw, Mathf.Max(0, -change.Screw)); // 나사 감소량 계산
        int spendIronPlate = Mathf.Min(resourceManager.IronPlate, Mathf.Max(0, -change.IronPlate)); // 철판 감소량 계산
        int spendWire = Mathf.Min(resourceManager.Wire, Mathf.Max(0, -change.Wire)); // 전선 감소량 계산

        if (spendGold > 0 ||
            spendScrew > 0 ||
            spendIronPlate > 0 ||
            spendWire > 0)
        {
            resourceManager.TrySpend(
                spendGold,
                spendScrew,
                spendIronPlate,
                spendWire); // 보유량 기준 손실 자원 차감
        }

        return change.BuildSummary(); // 표시용 변화량 요약 반환
    }

    private void CreateCloseButton(string label) // 닫기 버튼 생성
    {
        CreateChoiceButton(label, CloseCurrentEvent); // 닫기 버튼 생성 호출
    }

    private void CloseCurrentEvent() // 현재 이벤트 패널 닫기
    {
        HidePanelImmediate(); // 패널 즉시 닫기
        ExplorationPlayerController.SetInputBlocked(false); // 플레이어 이동 잠금 해제
        currentEventView = null; // 현재 이벤트 뷰 참조 정리
        currentEventData = null; // 현재 이벤트 데이터 정리
        currentRuntimeEventId = null; // 현재 이벤트 ID 정리
        choiceCommitted = false; // 선택 상태 초기화
    }

    private void HidePanelImmediate() // 패널 즉시 숨김
    {
        ClearChoiceButtons(); // 기존 버튼 즉시 제거

        if (rootCanvas != null)
        {
            rootCanvas.enabled = false; // 캔버스 비활성화
        }

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false); // 패널 본문 비활성화
        }

        ReleaseBlurTexture(); // 블러 텍스처 해제
    }

    private void ApplyBlurCapture() // 현재 화면을 캡처해 블러 텍스처 생성
    {
        ReleaseBlurTexture(); // 이전 블러 텍스처 정리

        Texture2D sourceTexture =
            ScreenCapture.CaptureScreenshotAsTexture(); // 현재 화면 캡처

        if (sourceTexture == null)
        {
            blurBackgroundImage.texture = null; // 캡처 실패 시 텍스처 제거
            blurBackgroundImage.color = new Color(0f, 0f, 0f, 0.65f); // 대체 어두운 색상 적용
            return;
        }

        int width =
            Mathf.Max(1, sourceTexture.width / BlurDownsample); // 축소 블러 폭 계산

        int height =
            Mathf.Max(1, sourceTexture.height / BlurDownsample); // 축소 블러 높이 계산

        blurTexture =
            new Texture2D(width, height, TextureFormat.RGB24, false); // 축소 블러 텍스처 생성

        blurTexture.filterMode = FilterMode.Bilinear; // 확대 시 부드럽게 보이도록 설정

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                float u = (x + 0.5f) / width; // 원본 샘플 U 좌표 계산
                float v = (y + 0.5f) / height; // 원본 샘플 V 좌표 계산
                Color sampledColor = sourceTexture.GetPixelBilinear(u, v); // 원본 색상 샘플링
                blurTexture.SetPixel(x, y, sampledColor); // 축소 텍스처 픽셀 설정
            }
        }

        blurTexture.Apply(); // 축소 텍스처 적용
        blurBackgroundImage.texture = blurTexture; // 블러 텍스처 표시
        blurBackgroundImage.color = Color.white; // 블러 텍스처 색상 적용

        Destroy(sourceTexture); // 원본 캡처 텍스처 정리
    }

    private void ReleaseBlurTexture() // 블러 텍스처 메모리 해제
    {
        if (blurTexture != null)
        {
            Destroy(blurTexture); // 블러 텍스처 제거
            blurTexture = null; // 블러 텍스처 참조 정리
        }

        if (blurBackgroundImage != null)
        {
            blurBackgroundImage.texture = null; // 배경 텍스처 참조 제거
            blurBackgroundImage.color = new Color(0f, 0f, 0f, 0.65f); // 대체 색상 적용
        }
    }

    private void ClearChoiceButtons() // 현재 버튼 목록 제거
    {
        for (int index = 0;
             index < choiceButtons.Count;
             index++)
        {
            if (choiceButtons[index] != null)
            {
                Destroy(choiceButtons[index].gameObject); // 버튼 오브젝트 제거
            }
        }

        choiceButtons.Clear(); // 버튼 목록 초기화
    }

    private void CreateChoiceButton(
        string label,
        UnityEngine.Events.UnityAction onClick) // 선택지 버튼 생성
    {
        GameObject buttonObject =
            new GameObject(
                "ChoiceButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)); // 버튼 오브젝트 생성

        buttonObject.transform.SetParent(choiceContainer, false); // 버튼을 컨테이너 하위에 배치

        RectTransform buttonRect =
            buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회

        buttonRect.sizeDelta = new Vector2(0f, 58f); // 버튼 높이 설정

        Image buttonImage =
            buttonObject.GetComponent<Image>(); // 버튼 이미지 조회

        buttonImage.color = new Color(0.13f, 0.29f, 0.33f, 0.95f); // 버튼 배경 색상 지정

        Button button =
            buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회

        ColorBlock colors =
            button.colors; // 버튼 색상 블록 조회

        colors.normalColor = new Color(0.13f, 0.29f, 0.33f, 0.95f); // 기본 버튼 색상 지정
        colors.highlightedColor = new Color(0.18f, 0.38f, 0.43f, 1f); // 하이라이트 색상 지정
        colors.pressedColor = new Color(0.10f, 0.22f, 0.26f, 1f); // 눌림 색상 지정
        colors.selectedColor = colors.highlightedColor; // 선택 색상 지정
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.75f); // 비활성 색상 지정
        button.colors = colors; // 버튼 색상 블록 적용
        button.onClick.AddListener(onClick); // 버튼 클릭 이벤트 연결

        GameObject textObject =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 버튼 텍스트 오브젝트 생성

        textObject.transform.SetParent(buttonObject.transform, false); // 텍스트를 버튼 하위에 배치

        RectTransform textRect =
            textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 조회

        textRect.anchorMin = Vector2.zero; // 텍스트 왼쪽 아래 앵커 설정
        textRect.anchorMax = Vector2.one; // 텍스트 오른쪽 위 앵커 설정
        textRect.offsetMin = new Vector2(18f, 6f); // 왼쪽 아래 여백 설정
        textRect.offsetMax = new Vector2(-18f, -6f); // 오른쪽 위 여백 설정

        TMP_Text buttonText =
            textObject.GetComponent<TMP_Text>(); // 버튼 텍스트 조회

        buttonText.text = label; // 버튼 문구 지정
        buttonText.fontSize = 23f; // 버튼 글자 크기 지정
        buttonText.color = new Color(0.86f, 0.95f, 0.97f, 1f); // 버튼 글자 색상 지정
        buttonText.alignment = TextAlignmentOptions.MidlineLeft; // 버튼 글자 정렬 지정
        buttonText.textWrappingMode = TextWrappingModes.Normal; // 버튼 줄바꿈 모드 설정

        choiceButtons.Add(button); // 버튼 목록에 현재 버튼 등록
    }

    private void EnsureEventSystem() // New Input System 기반 UI 클릭 이벤트 시스템 보장
    {
        EventSystem eventSystem =
            EventSystem.current; // 현재 활성 EventSystem 조회

        if (eventSystem == null)
        {
            GameObject eventSystemObject =
                new GameObject(
                    "ExplorationEventSystem",
                    typeof(EventSystem)); // 런타임 EventSystem 오브젝트 생성

            eventSystemObject.transform.SetParent(
                transform,
                false); // 영구 이벤트 패널 하위에 배치

            eventSystem =
                eventSystemObject.GetComponent<EventSystem>(); // 생성 EventSystem 조회
        }

        InputSystemUIInputModule inputModule =
            eventSystem.GetComponent<InputSystemUIInputModule>(); // New Input System UI 모듈 조회

        if (inputModule != null)
        {
            inputModule.enabled = true; // 기존 New Input System UI 모듈 활성화
            return;
        }

        BaseInputModule existingInputModule =
            eventSystem.GetComponent<BaseInputModule>(); // 기존 입력 모듈 조회

        if (existingInputModule != null)
        {
            existingInputModule.enabled = false; // 구 Input Manager 기반 모듈 충돌 방지
        }

        inputModule =
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>(); // New Input System UI 모듈 추가

        inputModule.AssignDefaultActions(); // 마우스 Point·Click 등 기본 UI 액션 연결
    }

    private void EnsureUi() // 이벤트 패널 UI 구성 보장
    {
        if (rootCanvas != null)
        {
            return; // 이미 구성된 UI 재사용
        }

        rootCanvas =
            gameObject.GetComponent<Canvas>(); // 기존 Canvas 조회

        if (rootCanvas == null)
        {
            rootCanvas =
                gameObject.AddComponent<Canvas>(); // 루트 Canvas 추가
        }

        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 방식 사용
        rootCanvas.sortingOrder = 5000; // 다른 UI보다 위에 표시

        EnsureEventSystem(); // 런타임 생성 UI의 마우스 클릭 이벤트 시스템 준비

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>(); // UI 클릭 처리 추가
        }

        CanvasScaler scaler =
            gameObject.GetComponent<CanvasScaler>(); // 기존 CanvasScaler 조회

        if (scaler == null)
        {
            scaler =
                gameObject.AddComponent<CanvasScaler>(); // CanvasScaler 추가
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 비례 스케일 사용
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 지정
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 매치 모드 지정
        scaler.matchWidthOrHeight = 0.5f; // 가로·세로 절충값 지정

        GameObject blurObject =
            new GameObject(
                "BlurBackground",
                typeof(RectTransform),
                typeof(RawImage)); // 블러 배경 오브젝트 생성

        blurObject.transform.SetParent(transform, false); // 블러 배경을 루트 하위에 배치

        RectTransform blurRect =
            blurObject.GetComponent<RectTransform>(); // 블러 배경 RectTransform 조회

        StretchFullScreen(blurRect); // 블러 배경 전체 화면 확장

        blurBackgroundImage =
            blurObject.GetComponent<RawImage>(); // 블러 배경 이미지 저장

        blurBackgroundImage.color = new Color(0f, 0f, 0f, 0.65f); // 기본 어두운 배경 색상 지정
        blurBackgroundImage.raycastTarget = true; // 배경 클릭 차단 활성화

        GameObject dimObject =
            new GameObject(
                "DimOverlay",
                typeof(RectTransform),
                typeof(Image)); // 오버레이 오브젝트 생성

        dimObject.transform.SetParent(transform, false); // 오버레이를 루트 하위에 배치

        RectTransform dimRect =
            dimObject.GetComponent<RectTransform>(); // 오버레이 RectTransform 조회

        StretchFullScreen(dimRect); // 오버레이 전체 화면 확장

        dimOverlayImage =
            dimObject.GetComponent<Image>(); // 어두운 오버레이 이미지 저장

        dimOverlayImage.color = new Color(0f, 0f, 0f, 0.35f); // 오버레이 색상 지정
        dimOverlayImage.raycastTarget = true; // 오버레이 클릭 차단 활성화

        GameObject panelObject =
            new GameObject(
                "EventPanel",
                typeof(RectTransform),
                typeof(Image)); // 패널 루트 오브젝트 생성

        panelObject.transform.SetParent(transform, false); // 패널 루트를 캔버스 하위에 배치

        panelRoot =
            panelObject.GetComponent<RectTransform>(); // 패널 루트 RectTransform 저장

        panelRoot.anchorMin = new Vector2(0.5f, 0.5f); // 패널 중앙 앵커 설정
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f); // 패널 중앙 앵커 설정
        panelRoot.pivot = new Vector2(0.5f, 0.5f); // 패널 중앙 피벗 설정
        panelRoot.sizeDelta = new Vector2(1180f, 660f); // 패널 크기 지정
        panelRoot.anchoredPosition = Vector2.zero; // 패널 위치 중앙 정렬

        Image panelImage =
            panelObject.GetComponent<Image>(); // 패널 배경 이미지 조회

        panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.96f); // 패널 배경 색상 지정
        panelImage.raycastTarget = false; // 패널 배경이 선택지 버튼 Raycast를 가로채지 않도록 설정

        CreateTitleArea(panelObject.transform); // 상단 제목 영역 생성
        CreateContentArea(panelObject.transform); // 본문 내용 영역 생성
        CreateChoiceArea(panelObject.transform); // 하단 선택지 영역 생성
    }

    private void CreateTitleArea(Transform parent) // 상단 제목 영역 생성
    {
        GameObject titleObject =
            new GameObject(
                "Title",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 제목 텍스트 오브젝트 생성

        titleObject.transform.SetParent(parent, false); // 제목 텍스트를 패널 하위에 배치

        RectTransform titleRect =
            titleObject.GetComponent<RectTransform>(); // 제목 RectTransform 조회

        titleRect.anchorMin = new Vector2(0f, 1f); // 제목 상단 좌측 앵커 설정
        titleRect.anchorMax = new Vector2(1f, 1f); // 제목 상단 우측 앵커 설정
        titleRect.pivot = new Vector2(0.5f, 1f); // 제목 상단 피벗 설정
        titleRect.offsetMin = new Vector2(40f, -86f); // 제목 좌측·하단 여백 설정
        titleRect.offsetMax = new Vector2(-40f, -20f); // 제목 우측·상단 여백 설정

        titleText =
            titleObject.GetComponent<TMP_Text>(); // 제목 텍스트 저장

        titleText.text = "이벤트"; // 제목 기본 문구 지정
        titleText.fontSize = 36f; // 제목 글자 크기 지정
        titleText.color = new Color(0.95f, 0.84f, 0.42f, 1f); // 제목 글자 색상 지정
        titleText.alignment = TextAlignmentOptions.Center; // 제목 정렬 지정
    }

    private void CreateContentArea(Transform parent) // 본문 내용 영역 생성
    {
        GameObject contentObject =
            new GameObject(
                "Content",
                typeof(RectTransform)); // 내용 영역 오브젝트 생성

        contentObject.transform.SetParent(parent, false); // 내용 영역을 패널 하위에 배치

        RectTransform contentRect =
            contentObject.GetComponent<RectTransform>(); // 내용 영역 RectTransform 조회

        contentRect.anchorMin = new Vector2(0f, 0f); // 내용 영역 하단 좌측 앵커 설정
        contentRect.anchorMax = new Vector2(1f, 1f); // 내용 영역 상단 우측 앵커 설정
        contentRect.offsetMin = new Vector2(38f, 165f); // 내용 영역 좌하단 여백 설정
        contentRect.offsetMax = new Vector2(-38f, -92f); // 내용 영역 우상단 여백 설정

        GameObject frameObject =
            new GameObject(
                "IllustrationFrame",
                typeof(RectTransform),
                typeof(Image)); // 일러스트 프레임 생성

        frameObject.transform.SetParent(contentObject.transform, false); // 프레임을 내용 영역 하위에 배치

        RectTransform frameRect =
            frameObject.GetComponent<RectTransform>(); // 프레임 RectTransform 조회

        frameRect.anchorMin = new Vector2(0f, 0.5f); // 프레임 좌측 중앙 앵커 설정
        frameRect.anchorMax = new Vector2(0f, 0.5f); // 프레임 좌측 중앙 앵커 설정
        frameRect.pivot = new Vector2(0f, 0.5f); // 프레임 좌측 중앙 피벗 설정
        frameRect.sizeDelta = new Vector2(320f, 280f); // 프레임 크기 지정
        frameRect.anchoredPosition = new Vector2(8f, 0f); // 프레임 위치 지정

        illustrationFrameImage =
            frameObject.GetComponent<Image>(); // 일러스트 프레임 이미지 저장

        illustrationFrameImage.color = new Color(0.16f, 0.12f, 0.15f, 1f); // 프레임 배경 색상 지정

        GameObject illustrationObject =
            new GameObject(
                "Illustration",
                typeof(RectTransform),
                typeof(Image)); // 일러스트 이미지 생성

        illustrationObject.transform.SetParent(frameObject.transform, false); // 그림 이미지를 프레임 하위에 배치

        RectTransform illustrationRect =
            illustrationObject.GetComponent<RectTransform>(); // 그림 RectTransform 조회

        illustrationRect.anchorMin = new Vector2(0.5f, 0.5f); // 그림 중앙 앵커 설정
        illustrationRect.anchorMax = new Vector2(0.5f, 0.5f); // 그림 중앙 앵커 설정
        illustrationRect.pivot = new Vector2(0.5f, 0.5f); // 그림 중앙 피벗 설정
        illustrationRect.sizeDelta = new Vector2(280f, 240f); // 그림 영역 크기 지정
        illustrationRect.anchoredPosition = Vector2.zero; // 그림 위치 중앙 정렬

        illustrationImage =
            illustrationObject.GetComponent<Image>(); // 그림 이미지 저장

        illustrationImage.preserveAspect = true; // 그림 비율 유지 설정

        GameObject placeholderObject =
            new GameObject(
                "IllustrationFallback",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 대체 문자 생성

        placeholderObject.transform.SetParent(frameObject.transform, false); // 대체 문자를 프레임 하위에 배치

        RectTransform placeholderRect =
            placeholderObject.GetComponent<RectTransform>(); // 대체 문자 RectTransform 조회

        StretchInsideParent(placeholderRect, 24f); // 대체 문자를 프레임 안쪽으로 확장

        illustrationFallbackText =
            placeholderObject.GetComponent<TMP_Text>(); // 대체 문자 텍스트 저장

        illustrationFallbackText.text = "?"; // 대체 문자 기본값 지정
        illustrationFallbackText.fontSize = 110f; // 대체 문자 크기 지정
        illustrationFallbackText.color = new Color(0.92f, 0.46f, 0.28f, 1f); // 대체 문자 색상 지정
        illustrationFallbackText.alignment = TextAlignmentOptions.Center; // 대체 문자 정렬 지정

        GameObject bodyObject =
            new GameObject(
                "BodyText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 본문 텍스트 생성

        bodyObject.transform.SetParent(contentObject.transform, false); // 본문 텍스트를 내용 영역 하위에 배치

        RectTransform bodyRect =
            bodyObject.GetComponent<RectTransform>(); // 본문 RectTransform 조회

        bodyRect.anchorMin = new Vector2(0f, 0f); // 본문 하단 좌측 앵커 설정
        bodyRect.anchorMax = new Vector2(1f, 1f); // 본문 상단 우측 앵커 설정
        bodyRect.offsetMin = new Vector2(360f, 16f); // 본문 좌하단 여백 설정
        bodyRect.offsetMax = new Vector2(-8f, -8f); // 본문 우상단 여백 설정

        bodyText =
            bodyObject.GetComponent<TMP_Text>(); // 본문 텍스트 저장

        bodyText.text = string.Empty; // 본문 초기 문구 지정
        bodyText.fontSize = 29f; // 본문 글자 크기 지정
        bodyText.color = new Color(0.92f, 0.92f, 0.92f, 1f); // 본문 글자 색상 지정
        bodyText.alignment = TextAlignmentOptions.TopLeft; // 본문 글자 정렬 지정
        bodyText.textWrappingMode = TextWrappingModes.Normal; // 본문 줄바꿈 모드 설정
        bodyText.overflowMode = TextOverflowModes.Overflow; // 본문 오버플로 설정
        bodyText.lineSpacing = 8f; // 본문 줄 간격 설정
    }

    private void CreateChoiceArea(Transform parent) // 하단 선택지 영역 생성
    {
        GameObject choiceObject =
            new GameObject(
                "Choices",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)); // 선택지 영역 오브젝트 생성

        choiceObject.transform.SetParent(parent, false); // 선택지 영역을 패널 하위에 배치

        choiceContainer =
            choiceObject.GetComponent<RectTransform>(); // 선택지 컨테이너 저장

        choiceContainer.anchorMin = new Vector2(0f, 0f); // 선택지 영역 하단 좌측 앵커 설정
        choiceContainer.anchorMax = new Vector2(1f, 0f); // 선택지 영역 하단 우측 앵커 설정
        choiceContainer.pivot = new Vector2(0.5f, 0f); // 선택지 영역 하단 피벗 설정
        choiceContainer.offsetMin = new Vector2(56f, 34f); // 선택지 영역 좌하단 여백 설정
        choiceContainer.offsetMax = new Vector2(-56f, 132f); // 선택지 영역 우상단 여백 설정

        VerticalLayoutGroup layoutGroup =
            choiceObject.GetComponent<VerticalLayoutGroup>(); // 세로 레이아웃 그룹 조회

        layoutGroup.spacing = 12f; // 버튼 사이 간격 지정
        layoutGroup.childControlWidth = true; // 버튼 폭 자동 제어 활성화
        layoutGroup.childControlHeight = false; // 버튼 높이 자동 제어 비활성화
        layoutGroup.childForceExpandWidth = true; // 버튼 폭 확장 활성화
        layoutGroup.childForceExpandHeight = false; // 버튼 높이 확장 비활성화

        ContentSizeFitter fitter =
            choiceObject.GetComponent<ContentSizeFitter>(); // 컨텐츠 크기 조정기 조회

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 가로 자동 맞춤 비활성화
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 세로 자동 맞춤 활성화
    }

    private static void StretchFullScreen(RectTransform rectTransform) // 전체 화면 확장 설정
    {
        rectTransform.anchorMin = Vector2.zero; // 좌하단 앵커 설정
        rectTransform.anchorMax = Vector2.one; // 우상단 앵커 설정
        rectTransform.offsetMin = Vector2.zero; // 좌하단 오프셋 제거
        rectTransform.offsetMax = Vector2.zero; // 우상단 오프셋 제거
    }

    private static void StretchInsideParent(
        RectTransform rectTransform,
        float padding) // 부모 안쪽 여백 확장 설정
    {
        rectTransform.anchorMin = Vector2.zero; // 좌하단 앵커 설정
        rectTransform.anchorMax = Vector2.one; // 우상단 앵커 설정
        rectTransform.offsetMin = new Vector2(padding, padding); // 좌하단 여백 설정
        rectTransform.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 설정
    }

    private void OnDestroy() // 이벤트 패널 제거 처리
    {
        ReleaseBlurTexture(); // 블러 텍스처 정리

        if (instance == this)
        {
            instance = null; // 정적 인스턴스 정리
        }
    }
}
