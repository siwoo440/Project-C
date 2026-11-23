using System; // 슬롯 불러오기 동작 사용
using TMPro; // 확인 문구 사용
using UnityEngine; // 화면 객체와 플레이어 조회 사용
using UnityEngine.UI; // 확인 버튼 사용

public sealed class SaveMenuController : MonoBehaviour // 슬롯 화면과 저장 기능 연결
{ // 저장 화면 연결 시작
    private SaveSlotService service; // 현재 슬롯 저장 서비스
    private SaveMenuView view; // 저장 화면 구성
    private bool canSave; // 현재 게임 저장 가능 상태
    private bool exploration; // 현재 탐사 화면 여부
    private Func<int, bool> onLoad; // 슬롯 불러오기 동작과 결과
    private GameObject confirmation; // 슬롯 동작 확인창
    private TMP_Text confirmationText; // 슬롯 확인 안내
    private TMP_Text message; // 저장 결과 안내
    private Action confirmedAction; // 확인 후 실행할 동작
    private bool previousInputBlocked; // 기존 탐사 입력 차단 상태
    private float previousTimeScale; // 기존 게임 시간 배율
    private bool pauseApplied; // 탐사 일시 정지 적용 여부
    public event Action Closed; // 화면 닫기 알림
    public static SaveMenuController Open(Canvas canvas, SaveSlotService slotService, bool allowSave, bool inExploration, Func<int, bool> loadAction) // 슬롯 화면 열기
    { // 저장 화면 열기 시작
        if (canvas == null || slotService == null) // 화면과 저장 서비스 확인
        { // 필수 정보 누락 처리 시작
            throw new ArgumentNullException(canvas == null ? nameof(canvas) : nameof(slotService)); // 누락 정보 오류
        } // 필수 정보 누락 처리 종료
        GameObject host = new GameObject("SaveMenuController", typeof(RectTransform), typeof(SaveMenuController)); // 저장 화면 연결 객체 생성
        host.transform.SetParent(canvas.transform, false); // 부모 캔버스 연결
        RectTransform rect = host.GetComponent<RectTransform>(); // 화면 연결 영역 조회
        rect.anchorMin = Vector2.zero; // 화면 시작 앵커 설정
        rect.anchorMax = Vector2.one; // 화면 끝 앵커 설정
        rect.offsetMin = Vector2.zero; // 화면 시작 여백 제거
        rect.offsetMax = Vector2.zero; // 화면 끝 여백 제거
        SaveMenuController controller = host.GetComponent<SaveMenuController>(); // 화면 연결 컴포넌트 조회
        controller.service = slotService; // 슬롯 서비스 보관
        controller.canSave = allowSave; // 저장 허용 여부 보관
        controller.exploration = inExploration; // 탐사 화면 여부 보관
        controller.onLoad = loadAction; // 불러오기 동작 보관
        controller.view = SaveMenuView.Create(canvas, controller.RequestSave, controller.RequestLoad, controller.RequestDelete, controller.Close); // 슬롯 화면 구성
        controller.view.transform.SetParent(host.transform, false); // 저장 화면 연결 객체 아래 배치
        controller.CreateMessage(); // 저장 결과 문구 생성
        controller.CreateConfirmation(); // 저장 동작 확인창 생성
        controller.RefreshSlots(); // 슬롯 저장 상태 표시
        if (inExploration) // 탐사 화면 여부 확인
        { // 탐사 일시 정지 시작
            controller.previousInputBlocked = ExplorationPlayerController.InputBlocked; // 기존 입력 차단 상태 보관
            controller.previousTimeScale = Time.timeScale; // 기존 시간 배율 보관
            ExplorationPlayerController.SetInputBlocked(true); // 탐사 이동 입력 차단
            Time.timeScale = 0f; // 저장 화면 중 게임 일시 정지
            controller.pauseApplied = true; // 일시 정지 적용 기록
        } // 탐사 일시 정지 종료
        return controller; // 생성 화면 연결 반환
    } // 저장 화면 열기 종료
    private void RefreshSlots() // 전체 슬롯 상태 갱신
    { // 슬롯 갱신 시작
        for (int index = 0; index < SaveMenuView.SlotCount; index++) // 전체 저장 슬롯 순회
        { // 슬롯 표시 시작
            bool valid = service.CanLoad(index); // 복원 가능한 저장 확인
            bool hasFile = service.HasFile(index); // 손상 파일 포함 저장 존재 확인
            bool unsupported = service.IsUnsupportedVersion(index); // 지원하지 않는 저장 버전 확인
            string description = valid ? (service.IsRecovery(index) ? "이전 저장 복구 · " : string.Empty) + (service.IsExploration(index) ? "탐사" : "거점") : unsupported ? "지원하지 않는 저장 버전" : "손상된 저장 데이터"; // 저장 종류와 복구 상태 표시
            view.SetSlotState(index, description, service.GetTimestamp(index), hasFile); // 슬롯 설명과 시간 표시
            view.SetSlotActions(index, canSave && !unsupported, valid && onLoad != null, hasFile); // 새 버전 덮어쓰기 차단과 슬롯 버튼 상태 적용
        } // 슬롯 표시 종료
    } // 슬롯 갱신 종료
    private void RequestSave(int index) // 슬롯 저장 요청
    { // 저장 요청 시작
        if (!canSave) // 게임 저장 가능 상태 확인
        { // 저장 차단 처리 시작
            return; // 저장 요청 무시
        } // 저장 차단 처리 종료
        if (service.HasFile(index)) // 기존 저장 파일 존재 확인
        { // 덮어쓰기 확인 시작
            ShowConfirmation("기존 저장 데이터를 덮어쓰시겠습니까?", () => Save(index)); // 덮어쓰기 확인 요청
            return; // 확인 전 저장 중단
        } // 덮어쓰기 확인 종료
        Save(index); // 빈 슬롯 즉시 저장
    } // 저장 요청 종료
    private void Save(int index) // 선택 슬롯 저장 실행
    { // 슬롯 저장 시작
        ExplorationPlayerController player = exploration ? FindFirstObjectByType<ExplorationPlayerController>() : null; // 탐사 플레이어 위치 조회
        bool saved = exploration ? player != null && service.SaveExploration(index, player.transform.position) : service.SaveLobby(index); // 현재 게임 상태 저장
        message.text = saved ? "저장했습니다." : "저장하지 못했습니다. 현재 상태를 확인해 주세요."; // 저장 결과 안내
        RefreshSlots(); // 저장 후 슬롯 상태 갱신
    } // 슬롯 저장 종료
    private void RequestLoad(int index) // 슬롯 불러오기 요청
    { // 불러오기 요청 시작
        if (!service.CanLoad(index) || onLoad == null) // 저장 파일과 불러오기 동작 확인
        { // 복원 불가 처리 시작
            message.text = "저장 데이터를 불러올 수 없습니다."; // 복원 실패 안내
            RefreshSlots(); // 변경된 슬롯 상태 갱신
            return; // 불러오기 중단
        } // 복원 불가 처리 종료
        if (service.IsRecovery(index)) // 이전 정상 복구본 사용 여부 확인
        { // 복구본 불러오기 확인 시작
            ShowConfirmation("현재 저장을 읽을 수 없어 이전 저장을 불러옵니다. 계속하시겠습니까?", () => LoadSelected(index)); // 이전 시점 복원 안내
            return; // 확인 전 복원 중단
        } // 복구본 불러오기 확인 종료
        LoadSelected(index); // 정상 저장 즉시 불러오기
    } // 불러오기 요청 종료
    private void LoadSelected(int index) // 확인한 슬롯 불러오기 실행
    { // 슬롯 불러오기 실행 시작
        if (!service.CanLoad(index) || onLoad == null) // 확인 후 저장 파일과 동작 재검사
        { // 복원 불가 처리 시작
            message.text = "저장 데이터를 불러올 수 없습니다."; // 변경된 저장 상태 안내
            RefreshSlots(); // 슬롯 표시 갱신
            return; // 복원 중단
        } // 복원 불가 처리 종료
        Func<int, bool> action = onLoad; // 불러오기 동작 보관
        ReleasePause(); // 씬 전환 전 게임 일시 정지 해제
        if (!action(index)) // 선택 슬롯 불러오기 실행과 결과 확인
        { // 불러오기 실패 처리 시작
            message.text = "저장 데이터를 불러올 수 없습니다."; // 복원 실패 안내
            RefreshSlots(); // 변경된 슬롯 상태 갱신
            if (exploration) // 탐사 화면 여부 확인
            { // 탐사 일시 정지 재설정 시작
                previousInputBlocked = ExplorationPlayerController.InputBlocked; // 현재 입력 상태 보관
                previousTimeScale = Time.timeScale; // 현재 시간 배율 보관
                ExplorationPlayerController.SetInputBlocked(true); // 탐사 이동 입력 차단
                Time.timeScale = 0f; // 저장 화면 중 게임 일시 정지
                pauseApplied = true; // 일시 정지 적용 기록
            } // 탐사 일시 정지 재설정 종료
        } // 불러오기 실패 처리 종료
    } // 슬롯 불러오기 실행 종료
    private void RequestDelete(int index) // 슬롯 삭제 요청
    { // 삭제 요청 시작
        if (service.HasFile(index)) // 저장 파일 존재 확인
        { // 삭제 확인 시작
            ShowConfirmation("선택한 저장 데이터를 삭제하시겠습니까?", () => Delete(index)); // 삭제 확인 요청
        } // 삭제 확인 종료
    } // 삭제 요청 종료
    private void Delete(int index) // 선택 슬롯 삭제 실행
    { // 슬롯 삭제 시작
        bool deleted = service.Delete(index); // 선택한 저장 파일 삭제
        message.text = deleted ? "삭제했습니다." : "삭제하지 못했습니다."; // 삭제 결과 안내
        RefreshSlots(); // 삭제 후 슬롯 상태 갱신
    } // 슬롯 삭제 종료
    private void ShowConfirmation(string text, Action action) // 슬롯 동작 확인 표시
    { // 확인 표시 시작
        confirmationText.text = text; // 확인 문구 설정
        confirmedAction = action; // 확인 후 동작 보관
        confirmation.SetActive(true); // 확인 화면 표시
        confirmation.transform.SetAsLastSibling(); // 확인 화면 최상단 배치
    } // 확인 표시 종료
    private void Confirm() // 슬롯 동작 확인
    { // 확인 처리 시작
        Action action = confirmedAction; // 확인 동작 임시 보관
        Cancel(); // 확인 화면 닫기
        action?.Invoke(); // 확인한 동작 실행
    } // 확인 처리 종료
    private void Cancel() // 슬롯 동작 취소
    { // 취소 처리 시작
        confirmedAction = null; // 대기 동작 제거
        confirmation.SetActive(false); // 확인 화면 숨김
    } // 취소 처리 종료
    private void Close() // 저장 화면 닫기
    { // 화면 닫기 시작
        ReleasePause(); // 게임 일시 정지 해제
        Closed?.Invoke(); // 화면 닫기 알림 전달
        if (Application.isPlaying) // 게임 실행 상태 확인
        { // 게임 화면 제거 시작
            Destroy(gameObject); // 프레임 종료 시 화면 제거
        } // 게임 화면 제거 종료
        else // 편집기 테스트 상태 확인
        { // 테스트 화면 제거 시작
            DestroyImmediate(gameObject); // 테스트 화면 즉시 제거
        } // 테스트 화면 제거 종료
    } // 화면 닫기 종료
    private void OnDestroy() // 화면 제거 처리
    { // 화면 제거 시작
        ReleasePause(); // 남아 있는 일시 정지 해제
    } // 화면 제거 종료
    private void ReleasePause() // 탐사 일시 정지 해제
    { // 일시 정지 해제 시작
        if (!pauseApplied) // 일시 정지 적용 상태 확인
        { // 적용 없음 처리 시작
            return; // 복원 중단
        } // 적용 없음 처리 종료
        ExplorationPlayerController.SetInputBlocked(previousInputBlocked); // 이전 입력 상태 복원
        Time.timeScale = previousTimeScale; // 이전 시간 배율 복원
        pauseApplied = false; // 일시 정지 해제 기록
    } // 일시 정지 해제 종료
    private void CreateMessage() // 저장 결과 안내 생성
    { // 결과 문구 생성 시작
        message = CreateText("SaveMessage", view.transform, string.Empty, 22f); // 저장 결과 문구 생성
        Place(message.rectTransform, new Vector2(0.5f, 0.04f), Vector2.zero, new Vector2(1000f, 44f)); // 저장 결과 위치 설정
        message.color = Color.white; // 결과 문구 색상 설정
    } // 결과 문구 생성 종료
    private void CreateConfirmation() // 슬롯 동작 확인창 생성
    { // 확인창 생성 시작
        confirmation = new GameObject("SlotConfirmation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 전체 화면 확인 배경 생성
        confirmation.transform.SetParent(view.transform, false); // 저장 화면 자식 배치
        RectTransform overlay = confirmation.GetComponent<RectTransform>(); // 확인 배경 영역 조회
        overlay.anchorMin = Vector2.zero; // 배경 시작 앵커 설정
        overlay.anchorMax = Vector2.one; // 배경 끝 앵커 설정
        overlay.offsetMin = Vector2.zero; // 배경 시작 여백 제거
        overlay.offsetMax = Vector2.zero; // 배경 끝 여백 제거
        confirmation.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f); // 어두운 확인 배경 설정
        GameObject backdropObject = new GameObject("WarningBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 확인창 배경 이미지 생성
        backdropObject.transform.SetParent(confirmation.transform, false); // 확인 화면 자식 배치
        Place(backdropObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 460f)); // 확인창 배경 위치 설정
        WarningUiArt.ApplyBackdrop(backdropObject.GetComponent<Image>()); // 제작 경고 배경 적용
        confirmationText = CreateText("Message", confirmation.transform, string.Empty, 30f); // 확인 안내 문구 생성
        Place(confirmationText.rectTransform, new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(900f, 120f)); // 확인 문구 위치 설정
        CreateConfirmationButton("ConfirmButton", "확인", -175f, Confirm); // 확인 버튼 생성
        CreateConfirmationButton("CancelButton", "취소", 175f, Cancel); // 취소 버튼 생성
        confirmation.SetActive(false); // 확인 화면 초기 숨김
    } // 확인창 생성 종료
    private void CreateConfirmationButton(string name, string label, float x, Action action) // 확인창 버튼 생성
    { // 확인 버튼 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 확인 버튼 객체 생성
        target.transform.SetParent(confirmation.transform, false); // 확인 화면 자식 배치
        Place(target.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), new Vector2(x, 0f), new Vector2(300f, 144f)); // 확인 버튼 위치와 세로 크기 확대
        Image background = target.GetComponent<Image>(); // 버튼 배경 조회
        background.color = new Color(0.28f, 0.29f, 0.30f, 1f); // 버튼 배경 색상 설정
        WarningUiArt.ApplyButton(background); // 제작 경고 버튼 이미지 적용
        Button button = target.GetComponent<Button>(); // 버튼 입력 조회
        button.targetGraphic = background; // 버튼 색상 대상 연결
        button.onClick.AddListener(() => action()); // 버튼 확인 동작 연결
        TMP_Text text = CreateText("Label", target.transform, label, 28f); // 버튼 문구 생성
        RectTransform rect = text.rectTransform; // 버튼 문구 영역 조회
        rect.anchorMin = Vector2.zero; // 문구 시작 앵커 설정
        rect.anchorMax = Vector2.one; // 문구 끝 앵커 설정
        rect.offsetMin = Vector2.zero; // 문구 시작 여백 제거
        rect.offsetMax = Vector2.zero; // 문구 끝 여백 제거
    } // 확인 버튼 생성 종료
    private static TMP_Text CreateText(string name, Transform parent, string value, float size) // 화면 문구 생성
    { // 화면 문구 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 문구 객체 생성
        target.transform.SetParent(parent, false); // 부모 화면 연결
        TMP_Text text = target.GetComponent<TMP_Text>(); // 문구 컴포넌트 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 연결
        text.fontSize = size; // 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 문구 가운데 정렬
        text.color = Color.white; // 문구 색상 설정
        text.text = value; // 표시 문구 설정
        text.raycastTarget = false; // 문구 입력 차단 해제
        return text; // 생성 문구 반환
    } // 화면 문구 생성 종료
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) // 고정 화면 영역 설정
    { // 화면 영역 설정 시작
        rect.anchorMin = anchor; // 시작 앵커 설정
        rect.anchorMax = anchor; // 끝 앵커 설정
        rect.anchoredPosition = position; // 화면 위치 설정
        rect.sizeDelta = size; // 화면 크기 설정
    } // 화면 영역 설정 종료
} // 저장 화면 연결 종료
