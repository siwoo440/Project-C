using System; // 선택 상태 확인 함수 사용
using System.Collections.Generic; // 연결된 유닛 화면 목록 사용
using System.Text; // 상태 이상 목록 글자 생성 사용
using TMPro; // 실제 전투 정보 글자 사용
using UnityEngine; // 전투 UI 객체 사용
using UnityEngine.UI; // 정보 패널 이미지와 버튼 사용
public sealed class BattleUnitInfoPanel : MonoBehaviour // 선택 유닛의 현재 전투 정보 화면
{ // 유닛 정보 화면 시작
    private readonly List<BattleUnitView> registeredViews = new List<BattleUnitView>(); // 정보 요청이 연결된 유닛 화면 목록
    private BattleHandView hand; // 실제 손패 화면
    private Func<bool> isChoosingTarget; // 카드 대상 선택 여부 확인 함수
    private RectTransform frame; // 진영별 정보 패널 영역
    private Image portrait; // 선택한 유닛 이미지
    private TMP_Text unitName; // 선택한 유닛 이름
    private TMP_Text stats; // 선택한 유닛 현재 능력치
    private TMP_Text status; // 선택한 유닛 현재 상태 이상
    private TMP_Text description; // 선택한 유닛 원본 설명
    public BattleUnitRuntime SelectedUnit { get; private set; } // 현재 정보가 표시된 유닛 조회
    public RectTransform PanelRect => frame; // 진영별 실제 패널 위치 조회
    public static BattleUnitInfoPanel Create(Canvas canvas, BattleHandView handView, Func<bool> choosingTarget) // 실제 전투 정보 패널 생성
    { // 유닛 정보 패널 생성 시작
        if (canvas == null || handView == null) // 전투 캔버스와 손패 확인
        { // 잘못된 전투 화면 처리 시작
            return null; // 유닛 정보 패널 생성 실패 반환
        } // 잘못된 전투 화면 처리 종료
        GameObject root = new GameObject("BattleUnitInfoPanel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(BattleUnitInfoPanel)); // 실제 유닛 정보 패널 루트 생성
        root.transform.SetParent(canvas.transform, false); // 실제 전투 캔버스에 정보 패널 연결
        Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 전투 화면 전체 기준 영역 설정
        Canvas infoCanvas = root.GetComponent<Canvas>(); // 정보 패널 전용 캔버스 조회
        infoCanvas.overrideSorting = true; // 전투 화면과 분리된 표시 순서 설정
        infoCanvas.sortingOrder = 470; // 일반 전투 UI 위 정보 패널 표시
        BattleUnitInfoPanel panel = root.GetComponent<BattleUnitInfoPanel>(); // 실제 정보 패널 컴포넌트 조회
        panel.Initialize(handView, choosingTarget); // 실제 손패와 대상 선택 상태 연결
        return panel; // 생성한 정보 패널 반환
    } // 유닛 정보 패널 생성 종료
    private void Initialize(BattleHandView handView, Func<bool> choosingTarget) // 실제 전투 정보 화면 초기화
    { // 정보 화면 초기화 시작
        hand = handView; // 실제 손패 화면 저장
        isChoosingTarget = choosingTarget; // 카드 대상 선택 상태 확인 연결
        hand.CardClicked += HandleCardClicked; // 카드 선택 시 정보 화면 정리 연결
        GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 유닛 정보 프레임 생성
        frameObject.transform.SetParent(transform, false); // 전투 정보 루트에 프레임 연결
        frame = frameObject.GetComponent<RectTransform>(); // 진영별 프레임 영역 저장
        frame.sizeDelta = new Vector2(480f, 500f); // 유닛 정보 프레임 크기 설정
        Image background = frameObject.GetComponent<Image>(); // 정보 프레임 배경 조회
        background.color = new Color(0.015f, 0.04f, 0.05f, 0.97f); // 이미지 누락 시 기본 배경
        LobbyUiArt.Apply(background, "FormationPanel"); // 기존 금속 테두리 패널 디자인 적용
        background.raycastTarget = true; // 정보 패널 내부 입력 차단
        unitName = CreateText("Name", frame, 28f, TextAlignmentOptions.Center); // 선택 유닛 이름 글자 생성
        Stretch(unitName.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.87f, 0.98f), Vector2.zero, Vector2.zero); // 정보 패널 위쪽 이름 배치
        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 선택 유닛 그림 생성
        portraitObject.transform.SetParent(frame, false); // 정보 패널에 그림 연결
        portrait = portraitObject.GetComponent<Image>(); // 유닛 그림 이미지 조회
        portrait.preserveAspect = true; // 유닛 그림 원본 비율 유지
        portrait.raycastTarget = false; // 그림의 버튼 입력 차단 방지
        Stretch(portrait.rectTransform, new Vector2(0.32f, 0.50f), new Vector2(0.68f, 0.86f), Vector2.zero, Vector2.zero); // 정보 패널 위쪽 유닛 그림 배치
        stats = CreateText("Stats", frame, 21f, TextAlignmentOptions.TopLeft); // 실제 전투 능력치 글자 생성
        Stretch(stats.rectTransform, new Vector2(0.07f, 0.23f), new Vector2(0.93f, 0.49f), Vector2.zero, Vector2.zero); // 정보 패널 중앙 능력치 배치
        status = CreateText("Status", frame, 17f, TextAlignmentOptions.TopLeft); // 실제 상태 이상 글자 생성
        status.textWrappingMode = TextWrappingModes.Normal; // 긴 상태 이상 목록 줄바꿈 허용
        Stretch(status.rectTransform, new Vector2(0.07f, 0.13f), new Vector2(0.93f, 0.23f), Vector2.zero, Vector2.zero); // 능력치 아래 상태 이상 배치
        description = CreateText("Description", frame, 16f, TextAlignmentOptions.TopLeft); // 실제 유닛 설명 글자 생성
        description.textWrappingMode = TextWrappingModes.Normal; // 긴 유닛 설명 줄바꿈 허용
        Stretch(description.rectTransform, new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.13f), Vector2.zero, Vector2.zero); // 정보 패널 아래쪽 설명 배치
        GameObject closeObject = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 정보 패널 닫기 버튼 생성
        closeObject.transform.SetParent(frame, false); // 정보 패널에 닫기 버튼 연결
        Image closeImage = closeObject.GetComponent<Image>(); // 닫기 버튼 배경 조회
        LobbyUiArt.Apply(closeImage, "Button"); // 기존 금속 버튼 디자인 적용
        RectTransform closeRect = closeObject.GetComponent<RectTransform>(); // 닫기 버튼 영역 조회
        Stretch(closeRect, new Vector2(0.87f, 0.88f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero); // 정보 패널 오른쪽 위 닫기 배치
        TMP_Text closeText = CreateText("Label", closeObject.transform, 24f, TextAlignmentOptions.Center); // 닫기 버튼 글자 생성
        closeText.text = "×"; // 정보 패널 닫기 기호 설정
        Stretch(closeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 닫기 버튼 중앙 기호 배치
        closeObject.GetComponent<Button>().onClick.AddListener(Hide); // 실제 정보 패널 닫기 연결
        frame.gameObject.SetActive(false); // 최초 정보 패널 숨김
    } // 정보 화면 초기화 종료
    public void Register(BattleUnitView view) // 실제 전투 유닛 화면의 정보 요청 연결
    { // 유닛 정보 요청 연결 시작
        if (view == null || registeredViews.Contains(view)) // 유닛 화면과 중복 연결 확인
        { // 연결 불가 처리 시작
            return; // 유닛 정보 요청 연결 중단
        } // 연결 불가 처리 종료
        registeredViews.Add(view); // 연결된 유닛 화면 기록
        view.InformationRequested += HandleInformationRequested; // 실제 유닛 클릭 정보 요청 연결
    } // 유닛 정보 요청 연결 종료
    public void Unregister(BattleUnitView view) // 제거되는 유닛 화면의 정보 요청 해제
    { // 유닛 정보 요청 해제 시작
        if (view == null || !registeredViews.Remove(view)) // 기존 유닛 화면 연결 확인
        { // 연결 없음 처리 시작
            return; // 유닛 정보 요청 해제 중단
        } // 연결 없음 처리 종료
        view.InformationRequested -= HandleInformationRequested; // 실제 유닛 클릭 정보 요청 해제
        if (view.RuntimeUnit == SelectedUnit) // 현재 표시 중인 유닛 제거 확인
        { // 제거 유닛 화면 정리 시작
            Hide(); // 제거 유닛 정보 패널 닫기
        } // 제거 유닛 화면 정리 종료
    } // 유닛 정보 요청 해제 종료
    private void HandleInformationRequested(BattleUnitRuntime unit) // 실제 유닛 클릭 정보 요청 처리
    { // 유닛 정보 요청 처리 시작
        if (unit == null || isChoosingTarget != null && isChoosingTarget()) // 유닛과 카드 대상 선택 상태 확인
        { // 정보 요청 무시 시작
            return; // 카드 대상 선택 중 정보 패널 열기 방지
        } // 정보 요청 무시 종료
        Show(unit); // 실제 선택 유닛의 정보 표시
    } // 유닛 정보 요청 처리 종료
    private void HandleCardClicked(CardInstance card) // 카드 선택 시 정보 패널 숨김
    { // 카드 선택 처리 시작
        Hide(); // 카드 대상과 정보 패널 겹침 방지
    } // 카드 선택 처리 종료
    private void Show(BattleUnitRuntime unit) // 실제 선택 유닛의 정보 열기
    { // 유닛 정보 열기 시작
        UnsubscribeSelected(); // 이전 선택 유닛의 상태 연결 해제
        SelectedUnit = unit; // 실제 선택한 전투 유닛 저장
        SelectedUnit.HealthChanged += HandleUnitChanged; // 실제 체력 변화 연결
        SelectedUnit.BlockChanged += HandleUnitChanged; // 실제 방어도 변화 연결
        SelectedUnit.StatusEffectsChanged += HandleUnitChanged; // 실제 상태 이상 변화 연결
        SelectedUnit.MentalChanged += HandleMentalChanged; // 실제 정신력 변화 연결
        bool ally = unit.Team == BattleTeam.Ally; // 선택 유닛의 아군 여부 확인
        frame.anchorMin = ally ? new Vector2(1f, 1f) : new Vector2(0f, 1f); // 진영별 화면 위쪽 최소 앵커
        frame.anchorMax = frame.anchorMin; // 진영별 화면 위쪽 최대 앵커
        frame.pivot = ally ? new Vector2(1f, 1f) : new Vector2(0f, 1f); // 진영별 화면 모서리 기준점
        frame.anchoredPosition = ally ? new Vector2(-20f, -100f) : new Vector2(20f, -100f); // 아군 오른쪽과 적 왼쪽 패널 배치
        frame.gameObject.SetActive(true); // 선택 유닛 정보 프레임 표시
        Refresh(); // 실제 유닛 현재 능력치와 상태 반영
    } // 유닛 정보 열기 종료
    private void Hide() // 유닛 정보 패널 닫기
    { // 유닛 정보 닫기 시작
        UnsubscribeSelected(); // 선택 유닛 상태 이벤트 연결 해제
        if (frame != null) // 정보 프레임 존재 확인
        { // 정보 프레임 숨김 시작
            frame.gameObject.SetActive(false); // 실제 정보 프레임 숨김
        } // 정보 프레임 숨김 종료
    } // 유닛 정보 닫기 종료
    private void UnsubscribeSelected() // 선택 유닛의 상태 이벤트 연결 해제
    { // 선택 유닛 연결 해제 시작
        if (SelectedUnit == null) // 기존 선택 유닛 확인
        { // 선택 유닛 없음 처리 시작
            return; // 이벤트 해제 중단
        } // 선택 유닛 없음 처리 종료
        SelectedUnit.HealthChanged -= HandleUnitChanged; // 체력 변화 연결 해제
        SelectedUnit.BlockChanged -= HandleUnitChanged; // 방어도 변화 연결 해제
        SelectedUnit.StatusEffectsChanged -= HandleUnitChanged; // 상태 이상 변화 연결 해제
        SelectedUnit.MentalChanged -= HandleMentalChanged; // 정신력 변화 연결 해제
        SelectedUnit = null; // 기존 선택 유닛 참조 제거
    } // 선택 유닛 연결 해제 종료
    private void HandleUnitChanged(BattleUnitRuntime unit) // 실제 능력치와 상태 변화 처리
    { // 실제 유닛 변화 처리 시작
        Refresh(); // 선택 유닛의 현재 정보 다시 표시
    } // 실제 유닛 변화 처리 종료
    private void HandleMentalChanged(BattleUnitRuntime unit, BattleMentalChangeResult result) // 실제 정신력 변화 처리
    { // 정신력 변화 처리 시작
        Refresh(); // 선택 유닛의 현재 정보 다시 표시
    } // 정신력 변화 처리 종료
    private void Refresh() // 실제 선택 유닛 정보 갱신
    { // 유닛 정보 갱신 시작
        if (SelectedUnit == null) // 현재 선택 유닛 확인
        { // 선택 유닛 없음 처리 시작
            return; // 유닛 정보 갱신 중단
        } // 선택 유닛 없음 처리 종료
        unitName.text = SelectedUnit.DisplayName; // 실제 유닛 이름 표시
        portrait.sprite = SelectedUnit.Portrait; // 실제 유닛 초상화 표시
        portrait.enabled = SelectedUnit.Portrait != null; // 초상화 존재 시에만 표시
        stats.text = $"체력  {SelectedUnit.CurrentHealth} / {SelectedUnit.MaxHealth}\n방어도  {SelectedUnit.CurrentBlock}\n물리 방어  {SelectedUnit.PhysicalDefense}   마법 저항  {SelectedUnit.MagicalResistance}\n정신력  {SelectedUnit.CurrentMental}"; // 실제 전투 능력치 표시
        StringBuilder effects = new StringBuilder("상태  "); // 실제 상태 이상 목록 시작
        if (SelectedUnit.StatusEffects.Count == 0) // 현재 상태 이상 없음 확인
        { // 빈 상태 이상 처리 시작
            effects.Append("없음"); // 상태 이상 없음 표시
        } // 빈 상태 이상 처리 종료
        foreach (BattleStatusEffectInstance effect in SelectedUnit.StatusEffects) // 실제 상태 이상 목록 순회
        { // 상태 이상 설명 추가 시작
            effects.Append(effect.DisplayName).Append(' ').Append(effect.EffectiveValue).Append(" · ").Append(effect.RemainingTurns).Append("턴  "); // 실제 상태 이름과 수치 및 지속 표시
        } // 상태 이상 설명 추가 종료
        status.text = effects.ToString(); // 실제 상태 이상 목록 적용
        description.text = SelectedUnit.CharacterSource != null ? SelectedUnit.CharacterSource.Description : SelectedUnit.EnemySource != null ? SelectedUnit.EnemySource.Description : ""; // 실제 유닛 원본 설명 적용
    } // 유닛 정보 갱신 종료
    private static TMP_Text CreateText(string objectName, Transform parent, float size, TextAlignmentOptions alignment) // 유닛 정보 글자 생성
    { // 정보 글자 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 유닛 정보 글자 객체 생성
        textObject.transform.SetParent(parent, false); // 지정 정보 패널에 글자 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 생성한 글자 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 프로젝트 한글 글꼴 적용
        text.fontSize = size; // 정보 글자 크기 설정
        text.color = Color.white; // 정보 글자 색상 설정
        text.alignment = alignment; // 정보 글자 정렬 설정
        text.raycastTarget = false; // 글자의 전투 입력 차단 방지
        return text; // 생성한 정보 글자 반환
    } // 정보 글자 생성 종료
    private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum, Vector2 offsetMinimum, Vector2 offsetMaximum) // 유닛 정보 영역 배치
    { // 정보 영역 배치 시작
        rect.anchorMin = minimum; // 정보 영역 최소 앵커 설정
        rect.anchorMax = maximum; // 정보 영역 최대 앵커 설정
        rect.offsetMin = offsetMinimum; // 정보 영역 왼쪽 아래 여백 설정
        rect.offsetMax = offsetMaximum; // 정보 영역 오른쪽 위 여백 설정
    } // 정보 영역 배치 종료
    private void OnDestroy() // 정보 화면 종료 처리
    { // 정보 화면 종료 시작
        UnsubscribeSelected(); // 현재 선택 유닛의 상태 연결 해제
        if (hand != null) // 실제 손패 화면 존재 확인
        { // 손패 이벤트 해제 시작
            hand.CardClicked -= HandleCardClicked; // 카드 선택 이벤트 해제
        } // 손패 이벤트 해제 종료
        foreach (BattleUnitView view in registeredViews) // 연결된 실제 유닛 화면 순회
        { // 유닛 정보 요청 해제 시작
            if (view != null) // 유효 유닛 화면 확인
            { // 유효 유닛 화면 처리 시작
                view.InformationRequested -= HandleInformationRequested; // 실제 유닛 정보 요청 해제
            } // 유효 유닛 화면 처리 종료
        } // 유닛 정보 요청 해제 종료
        registeredViews.Clear(); // 연결된 유닛 화면 목록 정리
    } // 정보 화면 종료 종료
} // 유닛 정보 화면 종료
