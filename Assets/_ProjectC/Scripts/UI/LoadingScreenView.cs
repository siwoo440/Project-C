using TMPro; // 진행 문구 사용
using UnityEngine; // 로딩 화면 객체 사용
using UnityEngine.UI; // 로딩 화면 이미지 사용

public sealed class LoadingScreenView : MonoBehaviour // 씬 전환 로딩 화면
{ // 로딩 화면 시작
    private Image fill; // 진행 막대 채움 이미지
    private TMP_Text percentage; // 진행률 숫자 문구
    private Sprite backgroundSprite; // 직접 생성한 배경 스프라이트
    private Sprite fillSprite; // 직접 생성한 진행 막대 스프라이트
    public static LoadingScreenView Show(Transform parent) // 로딩 화면 생성
    { // 로딩 화면 생성 시작
        GameObject root = new GameObject("LoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 독립 로딩 캔버스 생성
        root.transform.SetParent(parent, false); // 유지되는 씬 관리자에 연결
        Canvas canvas = root.GetComponent<Canvas>(); // 로딩 캔버스 조회
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 전체 화면 표시 방식 설정
        canvas.sortingOrder = 32000; // 다른 화면 위에 표시
        CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 화면 크기 조정 조회
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도별 비율 조정
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
        scaler.matchWidthOrHeight = 0.5f; // 너비와 높이 균형 설정
        LoadingScreenView view = root.AddComponent<LoadingScreenView>(); // 로딩 화면 동작 연결
        view.Build(); // 배경과 진행 표시 생성
        view.SetProgress(0f); // 초기 진행률 표시
        return view; // 생성 화면 반환
    } // 로딩 화면 생성 종료
    private void Build() // 로딩 화면 구성
    { // 화면 구성 시작
        RectTransform background = PreparationUi.CreatePanel("Background", transform, Vector2.zero, Vector2.one); // 전체 화면 배경 생성
        Image backgroundImage = background.GetComponent<Image>(); // 배경 이미지 조회
        Texture2D texture = Resources.Load<Texture2D>("UI/Loading/Background"); // 제작된 로딩 배경 조회
        if (texture != null) // 배경 원본 존재 확인
        { // 배경 이미지 적용 시작
            backgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 배경 스프라이트 생성
            backgroundImage.sprite = backgroundSprite; // 생성된 배경 스프라이트 적용
            backgroundImage.color = Color.white; // 원본 배경 색상 적용
        } // 배경 이미지 적용 종료
        RectTransform shade = PreparationUi.CreatePanel("Shade", transform, new Vector2(0f, 0f), new Vector2(1f, 0.3f)); // 하단 글자 배경 생성
        shade.GetComponent<Image>().color = new Color(0.01f, 0.025f, 0.03f, 0.76f); // 어두운 하단 배경 설정
        TMP_Text label = PreparationUi.CreateText("Label", transform, "화면을 불러오고 있습니다", 32f); // 로딩 안내 문구 생성
        PreparationUi.Place(label.rectTransform, new Vector2(0.5f, 0.19f), Vector2.zero, new Vector2(800f, 55f)); // 안내 문구 위치 설정
        RectTransform track = PreparationUi.CreatePanel("ProgressTrack", transform, new Vector2(0.28f, 0.112f), new Vector2(0.72f, 0.128f)); // 진행 막대 배경 생성
        track.GetComponent<Image>().color = new Color(0.35f, 0.28f, 0.17f, 1f); // 진행 막대 테두리 색상 설정
        RectTransform inner = PreparationUi.CreatePanel("ProgressFill", track, Vector2.zero, Vector2.one); // 진행 막대 채움 생성
        inner.offsetMin = new Vector2(3f, 3f); // 막대 안쪽 시작 여백
        inner.offsetMax = new Vector2(-3f, -3f); // 막대 안쪽 끝 여백
        fill = inner.GetComponent<Image>(); // 진행 막대 채움 조회
        fillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f)); // 진행률 채우기용 흰색 스프라이트 생성
        fill.sprite = fillSprite; // 진행 막대 스프라이트 적용
        fill.color = new Color(0.9f, 0.7f, 0.38f, 1f); // 금빛 진행 막대 설정
        fill.type = Image.Type.Filled; // 진행률 기준 채우기 설정
        fill.fillMethod = Image.FillMethod.Horizontal; // 가로 방향 채우기 설정
        percentage = PreparationUi.CreateText("Percentage", transform, "0%", 24f); // 진행률 숫자 생성
        PreparationUi.Place(percentage.rectTransform, new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(220f, 44f)); // 진행률 숫자 위치 설정
    } // 화면 구성 종료
    public void SetProgress(float value) // 로딩 진행률 갱신
    { // 진행률 갱신 시작
        float progress = Mathf.Clamp01(value); // 진행률 범위 제한
        fill.fillAmount = progress; // 진행 막대 길이 적용
        percentage.text = Mathf.RoundToInt(progress * 100f) + "%"; // 진행률 숫자 적용
    } // 진행률 갱신 종료
    private void OnDestroy() // 로딩 화면 정리
    { // 로딩 화면 정리 시작
        if (backgroundSprite != null) // 생성 배경 스프라이트 존재 확인
        { // 배경 스프라이트 정리 시작
            Destroy(backgroundSprite); // 배경 스프라이트 메모리 해제
        } // 배경 스프라이트 정리 종료
        if (fillSprite != null) // 생성 진행 막대 스프라이트 존재 확인
        { // 진행 막대 정리 시작
            Destroy(fillSprite); // 진행 막대 스프라이트 메모리 해제
        } // 진행 막대 정리 종료
    } // 로딩 화면 정리 종료
} // 로딩 화면 종료
