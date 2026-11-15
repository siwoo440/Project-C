using TMPro; // 버튼 한글 문구 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.UI; // 저장 화면 버튼 사용

public sealed class SaveMenuEntry : MonoBehaviour // 게임 화면의 저장 메뉴 진입 버튼
{ // 저장 메뉴 진입 시작
    private Canvas canvas; // 저장 화면을 표시할 캔버스
    private bool exploration; // 탐사 화면 여부
    public static void Install(Canvas targetCanvas, bool inExploration) // 저장 메뉴 버튼 설치
    { // 진입 버튼 설치 시작
        if (targetCanvas == null) // 대상 캔버스 존재 확인
        { // 캔버스 누락 처리 시작
            return; // 버튼 설치 중단
        } // 캔버스 누락 처리 종료
        GameObject target = new GameObject("SaveMenuEntry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(SaveMenuEntry)); // 저장 메뉴 버튼 객체 생성
        target.transform.SetParent(targetCanvas.transform, false); // 게임 화면 자식 배치
        RectTransform rect = target.GetComponent<RectTransform>(); // 버튼 영역 조회
        rect.anchorMin = new Vector2(1f, 0f); // 버튼 시작 앵커 설정
        rect.anchorMax = new Vector2(1f, 0f); // 버튼 끝 앵커 설정
        rect.anchoredPosition = new Vector2(-150f, 62f); // 화면 오른쪽 아래 버튼 배치
        rect.sizeDelta = new Vector2(250f, 64f); // 버튼 크기 설정
        Image background = target.GetComponent<Image>(); // 버튼 배경 조회
        background.color = new Color(0.15f, 0.15f, 0.14f, 0.92f); // 금속 계열 어두운 배경 설정
        Button button = target.GetComponent<Button>(); // 버튼 입력 조회
        button.targetGraphic = background; // 버튼 색상 대상 연결
        SaveMenuEntry entry = target.GetComponent<SaveMenuEntry>(); // 버튼 동작 컴포넌트 조회
        entry.canvas = targetCanvas; // 저장 화면 캔버스 보관
        entry.exploration = inExploration; // 탐사 화면 여부 보관
        button.onClick.AddListener(entry.Open); // 저장 화면 열기 연결
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 버튼 글자 객체 생성
        labelObject.transform.SetParent(target.transform, false); // 버튼 글자 자식 배치
        RectTransform labelRect = labelObject.GetComponent<RectTransform>(); // 글자 영역 조회
        labelRect.anchorMin = Vector2.zero; // 글자 시작 앵커 설정
        labelRect.anchorMax = Vector2.one; // 글자 끝 앵커 설정
        labelRect.offsetMin = Vector2.zero; // 글자 시작 여백 제거
        labelRect.offsetMax = Vector2.zero; // 글자 끝 여백 제거
        TMP_Text text = labelObject.GetComponent<TMP_Text>(); // 글자 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 연결
        text.fontSize = 24f; // 버튼 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 버튼 글자 가운데 정렬
        text.text = "저장 · 불러오기"; // 버튼 문구 설정
        text.color = Color.white; // 버튼 글자 색상 설정
        text.raycastTarget = false; // 글자 입력 차단 해제
    } // 진입 버튼 설치 종료
    private void Open() // 게임 화면에서 저장 메뉴 열기
    { // 저장 메뉴 열기 시작
        if (FindFirstObjectByType<SaveMenuController>() != null || exploration && ExplorationPlayerController.InputBlocked) // 열린 저장 화면과 다른 탐사 화면 확인
        { // 중복 화면 처리 시작
            return; // 저장 화면 열기 중단
        } // 중복 화면 처리 종료
        SaveSlotService service = SaveSlotService.CreateDefault(); // 게임 저장 경로 서비스 생성
        SaveMenuController.Open(canvas, service, true, exploration, index => SaveSlotNavigator.Load(service, index)); // 저장 가능한 슬롯 화면 표시
    } // 저장 메뉴 열기 종료
} // 저장 메뉴 진입 종료
