using System; // 안내 버튼 동작 사용
using TMPro; // 한글 안내 문구 사용
using UnityEngine; // 안내 화면 객체 사용
using UnityEngine.UI; // 예시 이미지와 버튼 사용
public sealed class TutorialPanelLayout // 예시 이미지와 설명을 나란히 표시하는 공통 안내 배치
{ // 공통 안내 배치 시작
    public TMP_Text Title { get; private set; } // 오른쪽 안내 제목 조회
    public TMP_Text Description { get; private set; } // 오른쪽 안내 설명 조회
    public Button NextButton { get; private set; } // 오른쪽 확인 버튼 조회
    private RawImage exampleImage; // 왼쪽 실제 화면 예시 이미지
    private TMP_Text exampleCaption; // 예시 이미지 상태 문구
    public static TutorialPanelLayout Create(RectTransform guide, Action next, Action skip) // 공통 안내 화면 생성
    { // 공통 안내 화면 생성 시작
        TutorialPanelLayout layout = new TutorialPanelLayout(); // 새 공통 안내 배치 생성
        RectTransform panel = PreparationUi.CreatePanel("Panel", guide, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.84f)); // 화면 중앙의 넓은 안내 영역 생성
        Image panelImage = panel.GetComponent<Image>(); // 공통 안내 영역 배경 조회
        panelImage.color = Color.clear; // 기존 패널 테두리와 배경 제거
        panelImage.raycastTarget = false; // 안내 영역의 게임 입력 통과
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
        PreparationUi.CreateButton("SkipButton", panel, "건너뛰기", new Vector2(0.91f, 0.98f), Vector2.zero, new Vector2(150f, 55f), skip); // 오른쪽 위 건너뛰기 버튼 생성
        layout.NextButton = PreparationUi.CreateButton("NextButton", panel, "확인", new Vector2(0.91f, 0.12f), Vector2.zero, new Vector2(150f, 55f), next); // 오른쪽 아래 확인 버튼 생성
        return layout; // 완성한 안내 배치 반환
    } // 공통 안내 화면 생성 종료
    public void SetExample(string name) // 단계에 맞는 실제 게임 화면 예시 설정
    { // 예시 이미지 설정 시작
        Texture2D image = Resources.Load<Texture2D>("UI/Tutorial/" + name); // 현재 단계의 실제 화면 이미지 조회
        bool missing = image == null; // 예시 이미지 누락 확인
        exampleImage.texture = missing ? Resources.Load<Texture2D>("UI/Settings/ImageUnavailable") : image; // 누락 시 기존 임시 이미지 사용
        exampleImage.color = Color.white; // 원본 예시 이미지 색상 유지
        exampleCaption.text = missing ? "화면 예시 준비 중" : string.Empty; // 이미지 누락 상태 안내
    } // 예시 이미지 설정 종료
} // 공통 안내 배치 종료
