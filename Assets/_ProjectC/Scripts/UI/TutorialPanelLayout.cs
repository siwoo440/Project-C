using System; // 안내 버튼 동작 사용
using TMPro; // 한글 안내 문구 사용
using UnityEngine; // 안내 화면 객체 사용
using UnityEngine.UI; // 예시 이미지와 버튼 사용
public sealed class TutorialPanelLayout // 예시 이미지와 설명을 나란히 표시하는 공통 안내 배치
{ // 공통 안내 배치 시작
    public TMP_Text Title { get; private set; } // 오른쪽 안내 제목 조회
    public TMP_Text Description { get; private set; } // 오른쪽 안내 설명 조회
    public Button NextButton { get; private set; } // 다음 또는 완료 버튼 조회
    public Button CloseButton { get; private set; } // 즉시 닫기 버튼 조회
    private static Sprite pageButtonSprite; // 재사용할 페이지 버튼 이미지
    private TMP_Text pageLabel; // 현재 페이지 안내 문구
    private Button previousPage; // 하단 이전 버튼
    private TMP_Text nextLabel; // 다음과 완료를 전환할 버튼 문구
    private RawImage exampleImage; // 왼쪽 실제 화면 예시 이미지
    private TMP_Text exampleCaption; // 예시 이미지 상태 문구
    public static TutorialPanelLayout Create(RectTransform guide, Action next, Action close, Action previous, bool modal = false) // 공통 안내 화면 생성
    { // 공통 안내 화면 생성 시작
        TutorialPanelLayout layout = new TutorialPanelLayout(); // 새 공통 안내 배치 생성
        RectTransform panel = PreparationUi.CreatePanel("Panel", guide, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.84f)); // 화면 중앙의 넓은 안내 영역 생성
        Image panelImage = panel.GetComponent<Image>(); // 공통 안내 영역 배경 조회
        Sprite background = Resources.Load<Sprite>("UI/Tutorial/TutorialPanel"); // 세 튜토리얼에서 공유하는 제작 배경
        if (background != null) // 가져온 게임 UI 스프라이트 확인
        { // 제작 배경 적용 시작
            panelImage.sprite = background; // 청록색 표면과 금속 테두리 원화 사용
            panelImage.type = Image.Type.Sliced; // 패널 크기가 달라도 금속 모서리 형태 보존
            panelImage.color = Color.white; // 배경 원본의 색상과 투명도 유지
        } // 제작 배경 적용 종료
        else // 배경 에셋을 가져오지 못한 경우
        { // 안내 읽기 보조 배경 적용 시작
            panelImage.color = new Color(0.04f, 0.14f, 0.16f, modal ? 1f : 0.94f); // 글자를 읽을 수 있는 어두운 기본색
        } // 보조 배경 적용 종료
        panelImage.raycastTarget = modal; // 전투 안내에서 뒤쪽 버튼 입력 차단
        layout.pageLabel = PreparationUi.CreateText("PageLabel", panel, string.Empty, 25f); // 현재 페이지 번호 생성
        PreparationUi.Place(layout.pageLabel.rectTransform, new Vector2(0.5f, 0.95f), Vector2.zero, new Vector2(180f, 48f)); // 안내 상단 중앙 페이지 표시
        RectTransform preview = PreparationUi.CreatePanel("Preview", panel, new Vector2(0.01f, 0.20f), new Vector2(0.48f, 0.83f)); // 왼쪽 예시 이미지 영역 생성
        Image previewImage = preview.GetComponent<Image>(); // 예시 영역 배경 조회
        previewImage.color = new Color(0.015f, 0.025f, 0.03f, 0.98f); // 어두운 예시 영역 배경 설정
        previewImage.raycastTarget = false; // 예시 영역의 게임 입력 통과
        Outline previewOutline = preview.gameObject.AddComponent<Outline>(); // 예시 이미지 금색 테두리 생성
        previewOutline.effectColor = new Color(1f, 0.75f, 0.28f, 1f); // 예시 테두리 금색 설정
        previewOutline.effectDistance = new Vector2(3f, -3f); // 예시 테두리 두께 설정
        GameObject imageObject = new GameObject("ExampleImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); // 실제 게임 화면 예시 생성
        imageObject.transform.SetParent(preview, false); // 예시 영역에 이미지 연결
        layout.exampleImage = imageObject.GetComponent<RawImage>(); // 실제 예시 이미지 조회
        PreparationUi.Stretch(layout.exampleImage.rectTransform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f)); // 예시 영역 안쪽으로 이미지 배치
        layout.exampleImage.raycastTarget = false; // 예시 이미지의 게임 입력 통과
        layout.exampleCaption = PreparationUi.CreateText("ExampleCaption", preview, string.Empty, 20f); // 예시 준비 상태 문구 생성
        PreparationUi.Stretch(layout.exampleCaption.rectTransform, new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.16f)); // 예시 하단 문구 위치 설정
        layout.Title = PreparationUi.CreateText("Title", panel, string.Empty, 38f); // 오른쪽 안내 제목 생성
        PreparationUi.Stretch(layout.Title.rectTransform, new Vector2(0.53f, 0.72f), new Vector2(0.98f, 0.93f)); // 오른쪽 위 제목 영역 설정
        layout.Title.alignment = TextAlignmentOptions.MidlineLeft; // 제목 왼쪽 정렬
        layout.Title.color = new Color(0.98f, 0.65f, 0.29f, 1f); // 참고 화면과 유사한 주황색 제목 설정
        layout.Description = PreparationUi.CreateText("Description", panel, string.Empty, 27f); // 오른쪽 안내 설명 생성
        PreparationUi.Stretch(layout.Description.rectTransform, new Vector2(0.53f, 0.29f), new Vector2(0.98f, 0.72f)); // 오른쪽 설명 영역 설정
        layout.Description.alignment = TextAlignmentOptions.TopLeft; // 설명 왼쪽 위 정렬
        layout.Description.textWrappingMode = TextWrappingModes.Normal; // 긴 설명 줄바꿈 설정
        layout.previousPage = PreparationUi.CreateButton("PreviousPage", panel, "이전", new Vector2(0.79f, 0.12f), Vector2.zero, new Vector2(150f, 55f), previous); // 하단 왼쪽 이전 버튼
        layout.NextButton = PreparationUi.CreateButton("NextButton", panel, "다음", new Vector2(0.91f, 0.12f), Vector2.zero, new Vector2(150f, 55f), next); // 하단 오른쪽 다음 또는 완료 버튼
        layout.CloseButton = PreparationUi.CreateButton("SkipButton", panel, "X", Vector2.one, new Vector2(-55f, -46f), new Vector2(48f, 42f), close); // 우측 상단 금속 장식 안쪽의 즉시 닫기 버튼
        layout.nextLabel = layout.NextButton.GetComponentInChildren<TMP_Text>(); // 현재 장에 맞춰 표시할 문구
        ApplyPageButtonArt(layout.previousPage); // 제작한 이전 버튼 이미지 적용
        ApplyPageButtonArt(layout.NextButton); // 제작한 다음과 완료 버튼 이미지 적용
        ConnectNavigation(layout.previousPage, null, layout.NextButton, layout.CloseButton, null); // 이전 버튼에서 다음과 닫기로 이동
        ConnectNavigation(layout.NextButton, layout.previousPage, null, layout.CloseButton, null); // 다음 버튼에서 이전과 닫기로 이동
        ConnectNavigation(layout.CloseButton, null, null, null, layout.NextButton); // 닫기 버튼에서 하단 다음으로 이동
        return layout; // 완성한 안내 배치 반환
    } // 공통 안내 화면 생성 종료
    private static void ConnectNavigation(Button button, Button left, Button right, Button up, Button down) // 화면 안의 방향키 이동 대상 지정
    { // 키보드 이동 연결 시작
        var navigation = button.navigation; // 버튼 선택 이동 설정 조회
        navigation.mode = Navigation.Mode.Explicit; // 안내 버튼끼리만 선택 허용
        navigation.selectOnLeft = left; // 왼쪽 안내 버튼 연결
        navigation.selectOnRight = right; // 오른쪽 안내 버튼 연결
        navigation.selectOnUp = up; // 위쪽 안내 버튼 연결
        navigation.selectOnDown = down; // 아래쪽 안내 버튼 연결
        button.navigation = navigation; // 안내 내부 이동 경로 반영
    } // 키보드 이동 연결 종료
    public void SetPage(int current, int total, bool canGoBack, bool canContinue) // 현재 안내 페이지와 버튼 상태 표시
    { // 페이지 표시 갱신 시작
        pageLabel.text = "페이지 " + current + " / " + total; // 현재 페이지와 전체 페이지 표시
        previousPage.interactable = canGoBack; // 이전 페이지 이동 가능 여부 설정
        nextLabel.text = current == total ? "완료" : "다음"; // 마지막 장의 완료 문구 선택
        NextButton.interactable = canContinue; // 현재 안내의 다음 입력 허용 상태
        previousPage.image.raycastTarget = previousPage.interactable; // 비활성 이전 버튼의 게임 입력 통과
        NextButton.image.raycastTarget = canContinue; // 비활성 다음 버튼의 게임 입력 통과
    } // 페이지 표시 갱신 종료
    private static void ApplyPageButtonArt(Button button) // 제작한 페이지 이동 버튼 이미지 적용
    { // 페이지 버튼 이미지 적용 시작
        if (pageButtonSprite == null) // 기존 페이지 이미지 준비 여부 확인
        { // 페이지 이미지 생성 시작
            Texture2D texture = Resources.Load<Texture2D>("UI/Tutorial/PageButton"); // 제작한 페이지 버튼 텍스처 조회
            if (texture != null) // 제작 이미지 존재 확인
            { // 페이지 이미지 준비 시작
                Rect area = new Rect(texture.width * 0.034f, texture.height * 0.165f, texture.width * 0.932f, texture.height * 0.67f); // 바깥 투명 여백 제외 영역
                pageButtonSprite = Sprite.Create(texture, area, new Vector2(0.5f, 0.5f), 100f); // 재사용할 버튼 스프라이트 생성
                pageButtonSprite.hideFlags = HideFlags.HideAndDontSave; // 런타임 공용 이미지 유지 설정
            } // 페이지 이미지 준비 종료
        } // 페이지 이미지 생성 종료
        if (pageButtonSprite != null) // 제작 이미지 존재 확인
        { // 페이지 버튼 이미지 연결 시작
            button.image.sprite = pageButtonSprite; // 제작한 버튼 이미지 연결
            button.image.type = Image.Type.Simple; // 버튼 이미지 전체 표시
            button.image.color = Color.white; // 제작 이미지 원본 색상 유지
        } // 페이지 버튼 이미지 연결 종료
        button.GetComponentInChildren<TMP_Text>().richText = false; // 이전과 다음 및 완료 문구를 일반 글자로 표시
    } // 페이지 버튼 이미지 적용 종료
    public void SetExample(string name) // 단계에 맞는 실제 게임 화면 예시 설정
    { // 예시 이미지 설정 시작
        Texture2D image = Resources.Load<Texture2D>("UI/Tutorial/" + name); // 현재 단계의 실제 화면 이미지 조회
        bool missing = image == null; // 예시 이미지 누락 확인
        exampleImage.texture = missing ? Resources.Load<Texture2D>("UI/Settings/ImageUnavailable") : image; // 누락 시 기존 임시 이미지 사용
        exampleImage.color = Color.white; // 원본 예시 이미지 색상 유지
        exampleCaption.text = missing ? "화면 예시 준비 중" : string.Empty; // 이미지 누락 상태 안내
    } // 예시 이미지 설정 종료
} // 공통 안내 배치 종료
