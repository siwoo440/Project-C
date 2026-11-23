using System; // 기본 인터페이스 기능 사용
using System.Collections; // 코루틴 자료형 사용
using System.Collections.Generic; // 대기열 자료형 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleMentalStateCutInView : MonoBehaviour, IDisposable // 각성·붕괴 전체 화면 컷인
{ // 클래스 시작
    private const float FadeInDuration = 0.18f; // 등장 시간
    private const float HoldDuration = 0.8f; // 유지 시간
    private const float FadeOutDuration = 0.32f; // 퇴장 시간
    private static readonly Color AwakeningColor = new Color(1f, 0.68f, 0.08f, 1f); // 각성 금색
    private static readonly Color CollapseColor = new Color(0.88f, 0.06f, 0.08f, 1f); // 붕괴 적색
    private readonly Queue<BattleEventContext> pendingCutIns = new Queue<BattleEventContext>(); // 연속 발동 컷인 대기열
    private IDisposable mentalSubscription; // 정신력 이벤트 구독
    private CanvasGroup canvasGroup; // 전체 투명도와 입력 제어
    private Image panelImage; // 중앙 색상 패널
    private Image portraitImage; // 발동 캐릭터 초상화
    private RectTransform portraitRect; // 초상화 애니메이션 사각형
    private Image leftAccent; // 왼쪽 강조선
    private Image rightAccent; // 오른쪽 강조선
    private TMP_Text nameText; // 캐릭터 이름 텍스트
    private TMP_Text titleText; // 각성·붕괴 제목 텍스트
    private TMP_Text subtitleText; // 상태 안내 텍스트
    private TMP_Text missingPortraitText; // 초상화 누락 안내 텍스트
    private Coroutine animationCoroutine; // 실행 중인 컷인 코루틴
    private float previousTimeScale = 1f; // 컷인 전 시간 배율
    private bool ownsTimePause; // 시간 정지 소유 여부
    private bool disposed; // 컷인 종료 여부
    public static BattleMentalStateCutInView Create(Transform canvasTransform, BattleEventDispatcher dispatcher) // Canvas 아래 컷인 생성
    { // 컷인 생성 시작
        if (canvasTransform == null) // Canvas 부모 누락 확인
        { // 부모 누락 처리 시작
            throw new ArgumentNullException(nameof(canvasTransform)); // Canvas 부모 누락 예외
        } // 부모 누락 처리 종료
        if (dispatcher == null) // 이벤트 발행기 누락 확인
        { // 발행기 누락 처리 시작
            throw new ArgumentNullException(nameof(dispatcher)); // 이벤트 발행기 누락 예외
        } // 발행기 누락 처리 종료
        GameObject cutInObject = new GameObject("MentalStateCutIn", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster), typeof(BattleMentalStateCutInView)); // 컷인 루트 오브젝트 생성
        cutInObject.transform.SetParent(canvasTransform, false); // 전투 Canvas 자식 배치
        RectTransform rootRect = cutInObject.GetComponent<RectTransform>(); // 컷인 루트 사각형 조회
        rootRect.anchorMin = Vector2.zero; // 전체 화면 최소 앵커
        rootRect.anchorMax = Vector2.one; // 전체 화면 최대 앵커
        rootRect.offsetMin = Vector2.zero; // 전체 화면 최소 여백
        rootRect.offsetMax = Vector2.zero; // 전체 화면 최대 여백
        Canvas cutInCanvas = cutInObject.GetComponent<Canvas>(); // 컷인 전용 Canvas 조회
        cutInCanvas.overrideSorting = true; // 상위 Canvas 정렬 분리
        cutInCanvas.sortingOrder = 500; // 최상단 컷인 정렬 순서
        BattleMentalStateCutInView cutInView = cutInObject.GetComponent<BattleMentalStateCutInView>(); // 컷인 화면 컴포넌트 조회
        cutInView.Initialize(dispatcher); // 공용 이벤트와 화면 초기화
        return cutInView; // 생성 컷인 화면 반환
    } // 컷인 생성 종료
    private void Initialize(BattleEventDispatcher dispatcher) // 컷인 화면 초기화
    { // 초기화 시작
        canvasGroup = GetComponent<CanvasGroup>(); // 전체 투명도 제어 조회
        canvasGroup.alpha = 0f; // 초기 투명 상태 적용
        canvasGroup.interactable = false; // 초기 상호작용 차단 해제
        canvasGroup.blocksRaycasts = false; // 초기 포인터 차단 해제
        CreateVisuals(); // 컷인 시각 요소 생성
        mentalSubscription = dispatcher.Subscribe(BattleEventType.MentalChanged, HandleMentalEvent, 100); // 정신력 상태 발동 우선 구독
        gameObject.SetActive(false); // 초기 컷인 숨김
    } // 초기화 종료
    private void CreateVisuals() // 컷인 시각 요소 생성
    { // 시각 요소 생성 시작
        Image backdropImage = CreateImage("Backdrop", transform, new Color(0f, 0f, 0f, 0.92f)); // 전체 화면 암전 생성
        Stretch(backdropImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 암전 전체 화면 배치
        panelImage = CreateImage("StateBand", transform, CollapseColor); // 중앙 상태 색상 띠 생성
        Stretch(panelImage.rectTransform, new Vector2(0f, 0.16f), new Vector2(1f, 0.84f), Vector2.zero, Vector2.zero); // 중앙 상태 띠 배치
        leftAccent = CreateImage("LeftAccent", transform, CollapseColor); // 왼쪽 강조선 생성
        ConfigureAccent(leftAccent.rectTransform, new Vector2(0.22f, 0.5f), -14f); // 왼쪽 강조선 배치
        rightAccent = CreateImage("RightAccent", transform, CollapseColor); // 오른쪽 강조선 생성
        ConfigureAccent(rightAccent.rectTransform, new Vector2(0.78f, 0.5f), 14f); // 오른쪽 강조선 배치
        Image portraitFrame = CreateImage("PortraitFrame", transform, new Color(0.04f, 0.04f, 0.05f, 0.92f)); // 초상화 어두운 프레임 생성
        RectTransform frameRect = portraitFrame.rectTransform; // 초상화 프레임 사각형 조회
        frameRect.anchorMin = new Vector2(0.5f, 0.5f); // 프레임 중앙 최소 앵커
        frameRect.anchorMax = new Vector2(0.5f, 0.5f); // 프레임 중앙 최대 앵커
        frameRect.sizeDelta = new Vector2(390f, 390f); // 초상화 프레임 크기
        frameRect.anchoredPosition = Vector2.zero; // 초상화 프레임 중앙 위치
        portraitImage = CreateImage("Portrait", portraitFrame.transform, Color.white); // 캐릭터 초상화 이미지 생성
        portraitRect = portraitImage.rectTransform; // 초상화 사각형 저장
        Stretch(portraitRect, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f)); // 프레임 내부 초상화 배치
        portraitImage.preserveAspect = true; // 초상화 원본 비율 유지
        portraitImage.raycastTarget = false; // 초상화 포인터 차단 해제
        missingPortraitText = CreateText("MissingPortrait", portraitFrame.transform, 24f, new Color(0.75f, 0.75f, 0.75f, 1f), TextAlignmentOptions.Center); // 초상화 누락 안내 생성
        Stretch(missingPortraitText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 누락 안내 프레임 전체 배치
        missingPortraitText.text = "초상화 미지정"; // 초상화 누락 안내 문구 적용
        subtitleText = CreateText("Subtitle", transform, 20f, Color.white, TextAlignmentOptions.Center); // 상단 상태 안내 생성
        Stretch(subtitleText.rectTransform, new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.96f), Vector2.zero, Vector2.zero); // 상단 상태 안내 배치
        nameText = CreateText("CharacterName", transform, 22f, Color.white, TextAlignmentOptions.Center); // 캐릭터 이름 생성
        Stretch(nameText.rectTransform, new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.16f), Vector2.zero, Vector2.zero); // 캐릭터 이름 배치
        titleText = CreateText("StateTitle", transform, 46f, CollapseColor, TextAlignmentOptions.Center); // 특수 상태 제목 생성
        Stretch(titleText.rectTransform, new Vector2(0.2f, 0.0f), new Vector2(0.8f, 0.09f), Vector2.zero, Vector2.zero); // 특수 상태 제목 배치
        titleText.fontStyle = FontStyles.Bold; // 특수 상태 제목 굵게 적용
    } // 시각 요소 생성 종료
    private void HandleMentalEvent(BattleEventContext eventContext) // 정신력 공용 이벤트 처리
    { // 정신력 이벤트 처리 시작
        if (disposed || eventContext == null || eventContext.MentalResult == null || !eventContext.MentalResult.StateStarted || eventContext.TargetUnit == null) // 컷인 발동 조건 확인
        { // 발동 제외 처리 시작
            return; // 컷인 처리 중단
        } // 발동 제외 처리 종료
        pendingCutIns.Enqueue(eventContext); // 특수 상태 컷인 대기열 추가
        if (animationCoroutine == null) // 기존 컷인 실행 여부 확인
        { // 새 컷인 실행 시작
            gameObject.SetActive(true); // 코루틴 실행을 위한 컷인 오브젝트 활성화
            BeginTimePause(); // 전투 시간 일시 정지
            animationCoroutine = StartCoroutine(PlayPendingCutIns()); // 대기 컷인 순차 실행
        } // 새 컷인 실행 종료
    } // 정신력 이벤트 처리 종료
    private IEnumerator PlayPendingCutIns() // 대기 컷인 순차 재생
    { // 순차 재생 시작
        gameObject.SetActive(true); // 컷인 전체 화면 표시
        canvasGroup.blocksRaycasts = true; // 컷인 동안 포인터 차단
        canvasGroup.interactable = true; // 컷인 입력 영역 활성화
        transform.SetAsLastSibling(); // 컷인 Canvas 최상단 배치
        while (pendingCutIns.Count > 0) // 대기 컷인 순회
        { // 개별 컷인 재생 시작
            BattleEventContext eventContext = pendingCutIns.Dequeue(); // 다음 컷인 이벤트 조회
            ApplyEventVisual(eventContext); // 상태별 시각 요소 적용
            yield return Fade(0f, 1f, FadeInDuration, true); // 컷인 빠른 등장
            yield return new WaitForSecondsRealtime(HoldDuration); // 컷인 상태 유지
            yield return Fade(1f, 0f, FadeOutDuration, false); // 컷인 부드러운 퇴장
        } // 개별 컷인 재생 종료
        canvasGroup.blocksRaycasts = false; // 포인터 차단 해제
        canvasGroup.interactable = false; // 컷인 입력 영역 비활성화
        gameObject.SetActive(false); // 컷인 전체 화면 숨김
        animationCoroutine = null; // 실행 코루틴 참조 제거
        EndTimePause(); // 전투 시간 복구
    } // 순차 재생 종료
    private void ApplyEventVisual(BattleEventContext eventContext) // 상태별 컷인 정보 적용
    { // 시각 정보 적용 시작
        BattleUnitRuntime runtimeUnit = eventContext.TargetUnit; // 발동 유닛 조회
        BattleMentalState state = eventContext.MentalResult.CurrentState; // 발동 정신 상태 조회
        bool awakening = state == BattleMentalState.Awakening; // 각성 여부 계산
        Color stateColor = awakening ? AwakeningColor : CollapseColor; // 상태별 강조 색상 선택
        panelImage.color = new Color(stateColor.r * 0.45f, stateColor.g * 0.45f, stateColor.b * 0.45f, 0.9f); // 중앙 패널 상태색 적용
        leftAccent.color = stateColor; // 왼쪽 강조선 상태색 적용
        rightAccent.color = stateColor; // 오른쪽 강조선 상태색 적용
        titleText.color = stateColor; // 상태 제목 색상 적용
        titleText.text = awakening ? "각성" : "붕괴"; // 특수 상태 제목 적용
        subtitleText.text = awakening ? "정신력이 극한에 도달했다" : "정신력이 한계에 도달했다"; // 상태별 안내 문구 적용
        nameText.text = runtimeUnit.DisplayName; // 발동 캐릭터 이름 적용
        portraitImage.sprite = runtimeUnit.Portrait; // 발동 캐릭터 초상화 적용
        portraitImage.enabled = runtimeUnit.Portrait != null; // 초상화 연결 여부 표시
        missingPortraitText.gameObject.SetActive(runtimeUnit.Portrait == null); // 초상화 누락 안내 표시
        portraitRect.localScale = new Vector3(0.82f, 0.82f, 1f); // 등장 전 초상화 축소
    } // 시각 정보 적용 종료
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration, bool scaleUp) // 컷인 투명도와 초상화 크기 변화
    { // 전환 재생 시작
        float elapsed = 0f; // 경과 시간 초기화
        while (elapsed < duration) // 전환 시간 순회
        { // 프레임 전환 처리 시작
            elapsed += Time.unscaledDeltaTime; // 시간 배율 무시 경과 시간 증가
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration); // 전환 진행률 계산
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress); // 전체 투명도 보간
            float scale = scaleUp ? Mathf.Lerp(0.82f, 1.04f, progress) : Mathf.Lerp(1.04f, 1f, progress); // 초상화 확대 비율 계산
            portraitRect.localScale = new Vector3(scale, scale, 1f); // 초상화 확대 적용
            yield return null; // 다음 프레임 대기
        } // 프레임 전환 처리 종료
        canvasGroup.alpha = endAlpha; // 최종 투명도 고정
    } // 전환 재생 종료
    private void BeginTimePause() // 컷인 전투 시간 정지
    { // 시간 정지 시작
        if (Time.timeScale <= 0f) // 기존 시간 정지 확인
        { // 기존 정지 처리 시작
            ownsTimePause = false; // 시간 정지 비소유 저장
            return; // 시간 정지 중단
        } // 기존 정지 처리 종료
        previousTimeScale = Time.timeScale; // 기존 시간 배율 저장
        Time.timeScale = 0f; // 전투 시간 정지
        ownsTimePause = true; // 시간 정지 소유 저장
    } // 시간 정지 종료
    private void EndTimePause() // 컷인 전투 시간 복구
    { // 시간 복구 시작
        if (!ownsTimePause) // 시간 정지 소유 확인
        { // 복구 불필요 처리 시작
            return; // 시간 복구 중단
        } // 복구 불필요 처리 종료
        Time.timeScale = previousTimeScale; // 컷인 전 시간 배율 복구
        ownsTimePause = false; // 시간 정지 소유 해제
    } // 시간 복구 종료
    private static Image CreateImage(string objectName, Transform parent, Color color) // 공용 이미지 생성
    { // 이미지 생성 시작
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(parent, false); // 지정 부모 자식 배치
        Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
        image.color = color; // 이미지 색상 적용
        image.raycastTarget = objectName == "Backdrop"; // 암전 배경만 포인터 차단
        return image; // 생성 이미지 반환
    } // 이미지 생성 종료
    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment) // 공용 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 지정 부모 자식 배치
        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>(); // 텍스트 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        text.fontSize = fontSize; // 글자 크기 적용
        text.color = color; // 글자 색상 적용
        text.alignment = alignment; // 글자 정렬 적용
        text.raycastTarget = false; // 텍스트 포인터 차단 해제
        return text; // 생성 텍스트 반환
    } // 텍스트 생성 종료
    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // 사각형 앵커 배치
    { // 사각형 배치 시작
        rectTransform.anchorMin = anchorMin; // 최소 앵커 적용
        rectTransform.anchorMax = anchorMax; // 최대 앵커 적용
        rectTransform.offsetMin = offsetMin; // 최소 여백 적용
        rectTransform.offsetMax = offsetMax; // 최대 여백 적용
    } // 사각형 배치 종료
    private static void ConfigureAccent(RectTransform accentRect, Vector2 anchor, float rotation) // 강조선 배치
    { // 강조선 배치 시작
        accentRect.anchorMin = anchor; // 강조선 최소 앵커 적용
        accentRect.anchorMax = anchor; // 강조선 최대 앵커 적용
        accentRect.sizeDelta = new Vector2(12f, 520f); // 강조선 크기 적용
        accentRect.anchoredPosition = Vector2.zero; // 강조선 기준 위치 적용
        accentRect.localRotation = Quaternion.Euler(0f, 0f, rotation); // 강조선 기울기 적용
    } // 강조선 배치 종료
    public void Dispose() // 컷인 화면 연결 해제
    { // 연결 해제 시작
        if (disposed) // 기존 해제 확인
        { // 중복 해제 처리 시작
            return; // 연결 해제 중단
        } // 중복 해제 처리 종료
        disposed = true; // 컷인 종료 상태 저장
        mentalSubscription?.Dispose(); // 정신력 이벤트 구독 해제
        mentalSubscription = null; // 정신력 구독 참조 제거
        pendingCutIns.Clear(); // 대기 컷인 전체 제거
        if (animationCoroutine != null) // 실행 코루틴 확인
        { // 실행 코루틴 중단 시작
            StopCoroutine(animationCoroutine); // 컷인 코루틴 중단
            animationCoroutine = null; // 컷인 코루틴 참조 제거
        } // 실행 코루틴 중단 종료
        EndTimePause(); // 전투 시간 배율 복구
    } // 연결 해제 종료
    private void OnDestroy() // 컷인 오브젝트 제거 처리
    { // 제거 처리 시작
        Dispose(); // 이벤트와 시간 상태 정리
    } // 제거 처리 종료
} // 클래스 종료
