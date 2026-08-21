using System; // 콜백 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleResultView : MonoBehaviour // 전투 종료 결과 화면
{ // 클래스 시작
    private TMP_Text titleText; // 전투 결과 제목 텍스트
    private TMP_Text summaryText; // 전투 결과 요약 텍스트
    private Button confirmButton; // 결과 확인 버튼
    private Func<bool> confirmationAction; // 확인 완료 처리
    private bool visualStructureCreated; // 결과 화면 구조 생성 여부
    public static BattleResultView Create(Transform canvasTransform) // Canvas 아래 결과 화면 생성
    { // 결과 화면 생성 시작
        GameObject resultObject = new GameObject("BattleResultView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 결과 화면 루트 생성
        resultObject.transform.SetParent(canvasTransform, false); // Canvas 자식 배치
        RectTransform resultRect = resultObject.GetComponent<RectTransform>(); // 결과 화면 사각형 조회
        resultRect.anchorMin = Vector2.zero; // 전체 화면 최소 앵커
        resultRect.anchorMax = Vector2.one; // 전체 화면 최대 앵커
        resultRect.offsetMin = Vector2.zero; // 전체 화면 왼쪽 아래 여백 제거
        resultRect.offsetMax = Vector2.zero; // 전체 화면 오른쪽 위 여백 제거
        BattleResultView resultView = resultObject.AddComponent<BattleResultView>(); // 결과 화면 컴포넌트 추가
        resultView.EnsureVisualStructure(); // 결과 화면 내부 구조 생성
        resultObject.SetActive(false); // 결과 화면 기본 숨김
        return resultView; // 생성 결과 화면 반환
    } // 결과 화면 생성 종료
    public void Show(BattleResultData resultData, Func<bool> confirmAction) // 전투 결과 화면 표시
    { // 결과 표시 시작
        if (resultData == null) // 결과 데이터 존재 확인
        { // 결과 없음 처리 시작
            return; // 결과 표시 중단
        } // 결과 없음 처리 종료
        EnsureVisualStructure(); // 결과 화면 구조 확인
        confirmationAction = confirmAction; // 확인 처리 저장
        titleText.text = GetResultTitle(resultData.Result); // 결과별 제목 표시
        string rewardText = resultData.CanReceiveReward ? "보상 획득 가능" : "보상 없음"; // 보상 가능 문구 계산
        summaryText.text = $"라운드 {resultData.CompletedRound}\n생존 아군 {resultData.LivingAllyCount}명\n{rewardText}"; // 결과 요약 표시
        confirmButton.interactable = true; // 확인 버튼 활성화
        gameObject.SetActive(true); // 결과 화면 표시
        transform.SetAsLastSibling(); // 결과 화면 최상단 배치
    } // 결과 표시 종료
    private void EnsureVisualStructure() // 결과 화면 내부 구조 준비
    { // 화면 구조 준비 시작
        if (visualStructureCreated) // 기존 구조 확인
        { // 기존 구조 처리 시작
            return; // 구조 생성 중단
        } // 기존 구조 처리 종료
        Image overlayImage = GetComponent<Image>(); // 전체 화면 배경 조회
        overlayImage.color = new Color(0.02f, 0.025f, 0.04f, 0.94f); // 결과 화면 어두운 배경 적용
        overlayImage.raycastTarget = true; // 배경 아래 입력 차단
        titleText = CreateText("ResultTitle", transform, 40f, new Color(1f, 0.82f, 0.35f, 1f)); // 결과 제목 생성
        RectTransform titleRect = titleText.rectTransform; // 결과 제목 사각형 조회
        titleRect.anchorMin = new Vector2(0.2f, 0.62f); // 제목 최소 앵커
        titleRect.anchorMax = new Vector2(0.8f, 0.75f); // 제목 최대 앵커
        titleRect.offsetMin = Vector2.zero; // 제목 왼쪽 아래 여백 제거
        titleRect.offsetMax = Vector2.zero; // 제목 오른쪽 위 여백 제거
        summaryText = CreateText("ResultSummary", transform, 25f, Color.white); // 결과 요약 생성
        RectTransform summaryRect = summaryText.rectTransform; // 결과 요약 사각형 조회
        summaryRect.anchorMin = new Vector2(0.25f, 0.38f); // 요약 최소 앵커
        summaryRect.anchorMax = new Vector2(0.75f, 0.6f); // 요약 최대 앵커
        summaryRect.offsetMin = Vector2.zero; // 요약 왼쪽 아래 여백 제거
        summaryRect.offsetMax = Vector2.zero; // 요약 오른쪽 위 여백 제거
        confirmButton = CreateButton("ConfirmButton", transform, "확인"); // 결과 확인 버튼 생성
        RectTransform confirmRect = confirmButton.transform as RectTransform; // 확인 버튼 사각형 조회
        confirmRect.anchorMin = new Vector2(0.4f, 0.24f); // 확인 버튼 최소 앵커
        confirmRect.anchorMax = new Vector2(0.6f, 0.32f); // 확인 버튼 최대 앵커
        confirmRect.offsetMin = Vector2.zero; // 확인 버튼 왼쪽 아래 여백 제거
        confirmRect.offsetMax = Vector2.zero; // 확인 버튼 오른쪽 위 여백 제거
        confirmButton.onClick.AddListener(HandleConfirmClicked); // 확인 버튼 클릭 이벤트 등록
        visualStructureCreated = true; // 결과 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
    private void HandleConfirmClicked() // 결과 확인 버튼 처리
    { // 확인 버튼 처리 시작
        if (confirmationAction == null || !confirmButton.interactable) // 확인 처리와 버튼 상태 확인
        { // 확인 불가 처리 시작
            return; // 확인 처리 중단
        } // 확인 불가 처리 종료
        if (confirmationAction.Invoke()) // Scene 전환 요청 성공 확인
        { // 전환 성공 처리 시작
            confirmButton.interactable = false; // 중복 확인 입력 차단
        } // 전환 성공 처리 종료
    } // 확인 버튼 처리 종료
    private static string GetResultTitle(BattleResult battleResult) // 결과별 제목 계산
    { // 제목 계산 시작
        if (battleResult == BattleResult.Victory) // 승리 결과 확인
        { // 승리 제목 처리 시작
            return "전투 승리"; // 승리 제목 반환
        } // 승리 제목 처리 종료
        if (battleResult == BattleResult.Defeat) // 패배 결과 확인
        { // 패배 제목 처리 시작
            return "전투 패배"; // 패배 제목 반환
        } // 패배 제목 처리 종료
        return "전투 도주"; // 도주 제목 반환
    } // 제목 계산 종료
    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, Color color) // 공용 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        text.fontSize = fontSize; // 글자 크기 설정
        text.color = color; // 글자 색상 설정
        text.alignment = TextAlignmentOptions.Center; // 텍스트 중앙 정렬
        text.raycastTarget = false; // 텍스트 입력 차단 해제
        return text; // 생성 텍스트 반환
    } // 텍스트 생성 종료
    private static Button CreateButton(string objectName, Transform parent, string label) // 공용 버튼 생성
    { // 버튼 생성 시작
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 오브젝트 생성
        buttonObject.transform.SetParent(parent, false); // 버튼 부모 연결
        Image buttonImage = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 조회
        buttonImage.color = new Color(0.2f, 0.38f, 0.62f, 1f); // 버튼 배경색 적용
        Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회
        button.targetGraphic = buttonImage; // 버튼 대상 그래픽 적용
        Navigation navigation = button.navigation; // 버튼 이동 설정 조회
        navigation.mode = Navigation.Mode.None; // 키보드 자동 이동 해제
        button.navigation = navigation; // 버튼 이동 설정 적용
        TMP_Text labelText = CreateText("Label", buttonObject.transform, 22f, Color.white); // 버튼 글자 생성
        labelText.text = label; // 버튼 글자 내용 적용
        RectTransform labelRect = labelText.rectTransform; // 버튼 글자 사각형 조회
        labelRect.anchorMin = Vector2.zero; // 버튼 글자 최소 앵커
        labelRect.anchorMax = Vector2.one; // 버튼 글자 최대 앵커
        labelRect.offsetMin = Vector2.zero; // 버튼 글자 왼쪽 아래 여백 제거
        labelRect.offsetMax = Vector2.zero; // 버튼 글자 오른쪽 위 여백 제거
        return button; // 생성 버튼 반환
    } // 버튼 생성 종료
    private void OnDestroy() // 결과 화면 제거 처리
    { // 제거 처리 시작
        if (confirmButton != null) // 확인 버튼 존재 확인
        { // 버튼 이벤트 해제 시작
            confirmButton.onClick.RemoveListener(HandleConfirmClicked); // 확인 버튼 이벤트 해제
        } // 버튼 이벤트 해제 종료
        confirmationAction = null; // 확인 처리 참조 제거
    } // 제거 처리 종료
} // 클래스 종료
