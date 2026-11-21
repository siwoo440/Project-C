using System; // 안내 대상 조회 기능 사용
using UnityEngine; // 전투 안내 화면 구성 사용
using UnityEngine.EventSystems; // 안내와 전투 간 입력 초점 전환
using UnityEngine.UI; // 안내 버튼과 강조 이미지 사용
public sealed class BattleTutorialView : MonoBehaviour // 전투 튜토리얼 안내 화면
{ // 안내 화면 시작
    private readonly RectTransform[] shades = new RectTransform[4]; // 강조 영역 주변 어두운 화면
    private readonly RectTransform[] borders = new RectTransform[4]; // 강조 영역 금색 테두리
    private readonly Vector3[] corners = new Vector3[4]; // 강조 대상 화면 꼭짓점
    private BattleTutorialProgress progress; // 연결된 안내 진행 상태
    private Func<BattleTutorialStep, RectTransform> findTarget; // 단계별 화면 대상 조회
    private RectTransform guide; // 안내와 강조 화면 영역
    private BattleTutorialStep displayedStep; // 마지막으로 표시한 안내 단계
    private bool folded; // 예시를 접고 실제 전투를 보는 상태
    private bool awaitingCardInput; // 카드 연출과 후속 선택 완료 대기
    private Button battleEndTurn; // 실제 전투 입력 복귀 확인용 버튼
    private Button nextButton; // 다음 설명 버튼
    private Button replayButton; // 안내 다시 보기 버튼
    private TutorialPanelLayout panelLayout; // 예시 이미지와 설명 공통 배치
    public static BattleTutorialView Create(Canvas canvas, BattleTutorialProgress tutorial, Func<BattleTutorialStep, RectTransform> target) // 전투 화면에 안내 생성
    { // 안내 화면 생성 시작
        if (canvas == null || tutorial == null || target == null) // 필수 연결 확인
        { // 필수 연결 누락 시작
            throw new ArgumentNullException(); // 잘못된 안내 화면 생성 차단
        } // 필수 연결 누락 종료
        GameObject root = new GameObject("BattleTutorial", typeof(RectTransform), typeof(BattleTutorialView)); // 안내 전체 화면 생성
        root.transform.SetParent(canvas.transform, false); // 전투 캔버스에 안내 연결
        PreparationUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one); // 전체 화면 크기 설정
        BattleTutorialView view = root.GetComponent<BattleTutorialView>(); // 새 안내 화면 조회
        view.progress = tutorial; // 안내 진행 상태 연결
        view.findTarget = target; // 단계별 강조 대상 연결
        view.battleEndTurn = target(BattleTutorialStep.EndTurn)?.GetComponent<Button>(); // 실제 카드 선택과 연출 종료 확인용 버튼 연결
        view.Build(); // 안내 화면 요소 생성
        tutorial.Changed += view.Refresh; // 단계 변경 화면 갱신 연결
        view.Refresh(); // 현재 안내 상태 표시
        return view; // 생성한 안내 화면 반환
    } // 안내 화면 생성 종료
    private void Build() // 안내 화면 요소 생성
    { // 안내 화면 구성 시작
        replayButton = PreparationUi.CreateButton("ReplayButton", transform, string.Empty, new Vector2(0.64f, 0.98f), new Vector2(0f, -8f), new Vector2(54f, 50f), OpenHelp); // 접어 둔 안내와 완료한 도움말 열기
        BattleUiArt.SetHudIcon(replayButton, "HudHelp"); // 도움말 책 표시
        guide = PreparationUi.CreatePanel("Guide", transform, Vector2.zero, Vector2.one); // 안내 전체 영역 생성
        Canvas layer = guide.gameObject.AddComponent<Canvas>(); // 전투 손패와 레벨 표시 위의 안내 전용 캔버스
        layer.overrideSorting = true; // 전투 화면과 독립된 그리기 순서 사용
        layer.sortingOrder = 700; // 전투 카드와 행동 화살표 및 레벨 HUD 앞에 표시
        guide.gameObject.AddComponent<GraphicRaycaster>(); // 안내와 뒤쪽 게임 입력의 실제 화면 순서 적용
        Image guideImage = guide.GetComponent<Image>(); // 전체 안내 배경 조회
        guideImage.color = Color.clear; // 전체 배경의 별도 색상 제거
        guideImage.raycastTarget = true; // 안내를 읽는 동안 바깥쪽을 포함한 전투 입력 차단
        for (int index = 0; index < shades.Length; index++) // 네 방향 주변 영역 순회
        { // 주변 화면 구성 시작
            shades[index] = CreateOverlayRect("Shade" + index, new Color(0f, 0f, 0f, 0.8f)); // 입력을 막지 않는 어두운 영역 생성
            borders[index] = CreateOverlayRect("Border" + index, new Color(1f, 0.75f, 0.28f, 1f)); // 금색 강조 테두리 생성
        } // 주변 화면 구성 종료
        panelLayout = TutorialPanelLayout.Create(guide, ConfirmPage, () => progress.Skip(), () => progress.GoBack(), true); // 예시와 페이지 이동 안내 배치 생성
        panelLayout.SetExample("Battle"); // 실제 전투 화면 예시 표시
        nextButton = panelLayout.NextButton; // 오른쪽 확인 버튼 연결
    } // 안내 화면 구성 종료
    private RectTransform CreateOverlayRect(string name, Color color) // 입력을 막지 않는 강조 화면 생성
    { // 강조 화면 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 강조 화면 객체 생성
        target.transform.SetParent(guide, false); // 안내 화면 자식 배치
        RectTransform rect = target.GetComponent<RectTransform>(); // 강조 영역 조회
        rect.anchorMin = Vector2.zero; // 왼쪽 아래 최소 앵커
        rect.anchorMax = Vector2.zero; // 왼쪽 아래 최대 앵커
        rect.pivot = Vector2.zero; // 왼쪽 아래 기준 위치
        Image image = target.GetComponent<Image>(); // 강조 이미지 조회
        image.color = color; // 지정 화면 색상 적용
        image.raycastTarget = false; // 전투 입력 통과 설정
        return rect; // 생성 강조 영역 반환
    } // 강조 화면 생성 종료
    private void OpenHelp() // 접은 안내를 열거나 처음부터 다시 보기
    { // 도움말 열기 시작
        if (progress.IsActive) // 현재 진행 중인 실습 확인
        { // 현재 단계 복귀 시작
            folded = false; // 접어 둔 같은 예시 열기
            Refresh(); // 현재 페이지 다시 표시
        } // 현재 단계 복귀 종료
        else // 닫힌 도움말 다시 보기
        { // 첫 단계 복귀 시작
            progress.Replay(); // 완료 기록과 무관하게 첫 페이지 표시
        } // 첫 단계 복귀 종료
    } // 도움말 열기 종료
    private void ConfirmPage() // 일반 설명 진행 또는 실제 전투로 복귀
    { // 페이지 확인 시작
        if (progress.CanAdvance) // 설명 완료 또는 이전에 마친 실습 확인
        { // 다음 안내 표시 시작
            progress.Advance(); // 기존 안내 순서로 진행
        } // 다음 안내 표시 종료
        else if (progress.Step != BattleTutorialStep.ObserveEnemy) // 다음 버튼으로 실제 카드 또는 턴 종료 실습 열기
        { // 예시 접기 시작
            folded = true; // 현재 단계는 유지하고 실제 전투 노출
            Refresh(); // 게임의 실제 화면과 입력 복구
        } // 예시 접기 종료
    } // 페이지 확인 종료
    private void Refresh() // 현재 단계별 문구와 버튼 갱신
    { // 안내 화면 갱신 시작
        bool active = progress != null && progress.IsActive; // 안내 진행 상태 확인
        if (displayedStep != progress.Step) // 실제 행동 또는 이동으로 단계 변경 확인
        { // 안내 표시 상태 전환 시작
            awaitingCardInput = folded && displayedStep == BattleTutorialStep.PlayCard && progress.Step == BattleTutorialStep.ActionPoints && battleEndTurn != null && !battleEndTurn.interactable; // 후속 선택과 복귀 연출 중 안내 보류
            folded = progress.Step == BattleTutorialStep.ObserveEnemy && !progress.CanAdvance; // 적 행동 중 관찰을 위해 화면 열기
            displayedStep = progress.Step; // 다음 갱신을 위한 단계 기록
        } // 안내 표시 상태 전환 종료
        else if (displayedStep == BattleTutorialStep.ObserveEnemy && progress.CanAdvance) // 적 행동을 관찰한 뒤 아군 턴 복귀 확인
        { // 마지막 안내 열기 시작
            folded = false; // 관찰을 마친 뒤 마지막 페이지 자동 복귀
        } // 마지막 안내 열기 종료
        bool open = active && !folded && !awaitingCardInput; // 현재 설명 화면 표시 여부
        guide.gameObject.SetActive(open); // 설명과 실습 화면 교대
        var events = EventSystem.current; // 현재 실제 전투 입력 시스템 조회
        var selected = events?.currentSelectedGameObject; // 현재 키보드 또는 패드의 선택 항목
        var selectedInGuide = selected != null && selected.transform.IsChildOf(guide); // 현재 선택이 안내 영역에 속하는지 확인
        if (!open && selectedInGuide) // 안내를 닫았거나 접었을 때 남은 입력 확인
        { // 닫힌 창의 선택 해제 시작
            events.SetSelectedGameObject(null); // 숨은 안내 버튼의 확인 입력 제거
        } // 닫힌 창의 선택 해제 종료
        replayButton.gameObject.SetActive(!active || folded && !awaitingCardInput); // 실습 중 현재 페이지 다시 보기 허용
        if (!active) // 표시할 안내 확인
        { // 안내 없음 처리 시작
            return; // 화면 내용 갱신 중단
        } // 안내 없음 처리 종료
        var page = progress.CurrentPage; // 현재 번호가 가리키는 실제 전투 설명
        panelLayout.Title.text = page.Title; // 실제 전투 페이지 제목 표시
        bool final = progress.PageNumber == progress.PageCount; // 지금 표시할 안내 마지막 장 여부
        panelLayout.Description.text = final && progress.CanAdvance ? "적 행동이 끝났습니다. 완료를 눌러 안내를 마치세요." : page.Description; // 실습 결과와 실제 완료 문구 연결
        panelLayout.SetPage(progress.PageNumber, progress.PageCount, progress.CanGoBack, !final || progress.CanAdvance); // 실습을 여는 다음과 마지막 완료의 입력 가능 상태
        if (open && events != null && !selectedInGuide) // 열린 안내 바깥에 남은 키보드 선택 확인
        { // 안내 입력 선택 시작
            var focus = nextButton.interactable ? nextButton : panelLayout.CloseButton; // 진행 가능 상태에 맞는 안내 내부 입력
            events.SetSelectedGameObject(focus.gameObject); // 가려진 전투 버튼의 확인 입력 방지
        } // 안내 입력 선택 종료
        UpdateHighlight(); // 현재 대상 화면 강조 갱신
    } // 안내 화면 갱신 종료
    private void LateUpdate() // 화면 크기와 대상 위치 반영
    { // 늦은 화면 갱신 시작
        if (awaitingCardInput && (battleEndTurn == null || battleEndTurn.interactable)) // 실제 카드 연출과 추가 선택 완료 확인
        { // 후속 안내 복귀 시작
            awaitingCardInput = false; // 전투 입력 복귀 기록
            Refresh(); // 다음 행동력 안내 열기
        } // 후속 안내 복귀 종료
        if (guide != null && guide.gameObject.activeInHierarchy) // 실제 표시 중인 안내의 강조 위치 확인
        { // 강조 위치 갱신 시작
            UpdateHighlight(); // 실제 화면 크기와 강조 위치 갱신
        } // 강조 위치 갱신 종료
    } // 늦은 화면 갱신 종료
    private void UpdateHighlight() // 실제 UI 위치 기준 강조 영역 배치
    { // 강조 위치 계산 시작
        RectTransform target = findTarget(progress.Step); // 현재 단계의 대상 조회
        float width = Mathf.Max(0f, guide.rect.width); // 안내 화면 너비 조회
        float height = Mathf.Max(0f, guide.rect.height); // 안내 화면 높이 조회
        float left = 0f; // 강조 왼쪽 기본 위치
        float right = 0f; // 강조 오른쪽 기본 위치
        float bottom = 0f; // 강조 아래 기본 위치
        float top = 0f; // 강조 위 기본 위치
        bool visible = target != null && target.gameObject.activeInHierarchy; // 실제 강조 대상 표시 여부 확인
        if (visible) // 강조 대상 존재 확인
        { // 강조 대상 범위 계산 시작
            target.GetWorldCorners(corners); // 실제 화면 대상 꼭짓점 조회
            Vector3 minimum = guide.InverseTransformPoint(corners[0]); // 대상 왼쪽 아래 좌표 변환
            Vector3 maximum = guide.InverseTransformPoint(corners[2]); // 대상 오른쪽 위 좌표 변환
            left = Mathf.Clamp(minimum.x - guide.rect.xMin - 6f, 0f, width); // 화면 안쪽 강조 왼쪽 위치
            right = Mathf.Clamp(maximum.x - guide.rect.xMin + 6f, left, width); // 화면 안쪽 강조 오른쪽 위치
            bottom = Mathf.Clamp(minimum.y - guide.rect.yMin - 6f, 0f, height); // 화면 안쪽 강조 아래 위치
            top = Mathf.Clamp(maximum.y - guide.rect.yMin + 6f, bottom, height); // 화면 안쪽 강조 위 위치
        } // 강조 대상 범위 계산 종료
        SetRect(shades[0], 0f, 0f, left, height); // 강조 왼쪽 어두운 영역 배치
        SetRect(shades[1], right, 0f, width - right, height); // 강조 오른쪽 어두운 영역 배치
        SetRect(shades[2], left, 0f, right - left, bottom); // 강조 아래 어두운 영역 배치
        SetRect(shades[3], left, top, right - left, height - top); // 강조 위 어두운 영역 배치
        for (int index = 0; index < borders.Length; index++) // 네 방향 테두리 순회
        { // 테두리 표시 시작
            borders[index].gameObject.SetActive(visible); // 강조 대상 존재 시 테두리 표시
        } // 테두리 표시 종료
        SetRect(borders[0], left, bottom, 3f, top - bottom); // 강조 왼쪽 테두리 배치
        SetRect(borders[1], right - 3f, bottom, 3f, top - bottom); // 강조 오른쪽 테두리 배치
        SetRect(borders[2], left, bottom, right - left, 3f); // 강조 아래 테두리 배치
        SetRect(borders[3], left, top - 3f, right - left, 3f); // 강조 위 테두리 배치
    } // 강조 위치 계산 종료
    private static void SetRect(RectTransform rect, float x, float y, float width, float height) // 화면 사각형 위치와 크기 설정
    { // 사각형 배치 시작
        rect.anchoredPosition = new Vector2(x, y); // 왼쪽 아래 기준 위치 설정
        rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height)); // 음수를 제외한 영역 크기 설정
    } // 사각형 배치 종료
    private void OnDestroy() // 안내 화면 제거 처리
    { // 안내 화면 정리 시작
        if (progress != null) // 연결된 진행 상태 확인
        { // 진행 상태 연결 해제 시작
            progress.Changed -= Refresh; // 단계 변경 화면 갱신 해제
        } // 진행 상태 연결 해제 종료
    } // 안내 화면 정리 종료
} // 안내 화면 종료
