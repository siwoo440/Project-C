using System; // 안내 대상 조회 기능 사용
using TMPro; // 한글 안내 문구 사용
using UnityEngine; // 전투 안내 화면 구성 사용
using UnityEngine.UI; // 안내 버튼과 강조 이미지 사용
public sealed class BattleTutorialView : MonoBehaviour // 전투 튜토리얼 안내 화면
{ // 안내 화면 시작
    private readonly RectTransform[] shades = new RectTransform[4]; // 강조 영역 주변 어두운 화면
    private readonly RectTransform[] borders = new RectTransform[4]; // 강조 영역 금색 테두리
    private readonly Vector3[] corners = new Vector3[4]; // 강조 대상 화면 꼭짓점
    private BattleTutorialProgress progress; // 연결된 안내 진행 상태
    private Func<BattleTutorialStep, RectTransform> findTarget; // 단계별 화면 대상 조회
    private RectTransform guide; // 안내와 강조 화면 영역
    private TMP_Text title; // 단계 제목 문구
    private TMP_Text description; // 단계별 설명 문구
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
        view.Build(); // 안내 화면 요소 생성
        tutorial.Changed += view.Refresh; // 단계 변경 화면 갱신 연결
        view.Refresh(); // 현재 안내 상태 표시
        return view; // 생성한 안내 화면 반환
    } // 안내 화면 생성 종료
    private void Build() // 안내 화면 요소 생성
    { // 안내 화면 구성 시작
        replayButton = PreparationUi.CreateButton("ReplayButton", transform, "전투 도움말", new Vector2(0.64f, 0.98f), new Vector2(0f, -8f), new Vector2(145f, 40f), () => progress.Replay()); // 전투 중 다시 보기 버튼 생성
        guide = PreparationUi.CreatePanel("Guide", transform, Vector2.zero, Vector2.one); // 안내 전체 영역 생성
        Image guideImage = guide.GetComponent<Image>(); // 전체 안내 배경 조회
        guideImage.color = Color.clear; // 전체 배경의 별도 색상 제거
        guideImage.raycastTarget = false; // 전체 안내 영역의 전투 입력 차단 해제
        for (int index = 0; index < shades.Length; index++) // 네 방향 주변 영역 순회
        { // 주변 화면 구성 시작
            shades[index] = CreateOverlayRect("Shade" + index, new Color(0f, 0f, 0f, 0.8f)); // 입력을 막지 않는 어두운 영역 생성
            borders[index] = CreateOverlayRect("Border" + index, new Color(1f, 0.75f, 0.28f, 1f)); // 금색 강조 테두리 생성
        } // 주변 화면 구성 종료
        panelLayout = TutorialPanelLayout.Create(guide, () => progress.Advance(), () => progress.Skip()); // 왼쪽 예시와 오른쪽 설명 배치 생성
        panelLayout.SetExample("Battle"); // 실제 전투 화면 예시 표시
        title = panelLayout.Title; // 오른쪽 안내 제목 연결
        description = panelLayout.Description; // 오른쪽 안내 설명 연결
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
    private void Refresh() // 현재 단계별 문구와 버튼 갱신
    { // 안내 화면 갱신 시작
        bool active = progress != null && progress.IsActive; // 안내 진행 상태 확인
        guide.gameObject.SetActive(active); // 안내 진행 중에만 화면 표시
        replayButton.gameObject.SetActive(!active); // 안내가 닫혀 있을 때 다시 보기 표시
        if (!active) // 표시할 안내 확인
        { // 안내 없음 처리 시작
            return; // 화면 내용 갱신 중단
        } // 안내 없음 처리 종료
        switch (progress.Step) // 현재 안내 단계 분기
        { // 단계별 문구 설정 시작
            case BattleTutorialStep.Overview: // 전투 화면 안내
                title.text = "전투 화면"; // 첫 안내 제목 설정
                description.text = "카드를 사용해 적을 공격하거나 아군을 지원합니다. 전투는 아군과 적이 번갈아 진행합니다."; // 전투 기본 설명 설정
                break; // 첫 안내 설정 종료
            case BattleTutorialStep.PlayCard: // 실제 카드 사용 대기
                title.text = "카드 사용"; // 카드 안내 제목 설정
                description.text = "손패의 카드를 누른 뒤 적이나 아군을 선택하세요. 전체 대상 카드는 선택하면 바로 사용됩니다."; // 실제 카드 조작 설명 설정
                break; // 카드 안내 설정 종료
            case BattleTutorialStep.ActionPoints: // 행동력 안내
                title.text = "행동력 AP"; // 행동력 안내 제목 설정
                description.text = "카드를 사용할 때 AP가 소모됩니다. 남은 AP는 다음 턴으로 이월되지 않습니다."; // 행동력 설명 설정
                break; // 행동력 안내 설정 종료
            case BattleTutorialStep.Mental: // 정신력 안내
                title.text = "정신력"; // 정신력 안내 제목 설정
                description.text = "행동과 전투 상황에 따라 정신력이 변합니다. 100은 각성, 0은 붕괴에 영향을 줍니다."; // 정신력 설명 설정
                break; // 정신력 안내 설정 종료
            case BattleTutorialStep.EnemyIntent: // 적 행동 예고 안내
                title.text = "적 행동 예고"; // 적 예고 안내 제목 설정
                description.text = "적의 예고를 확인해 다음 행동에 대비하세요. 적을 가리키면 상세 내용을 볼 수 있습니다."; // 행동 예고 설명 설정
                break; // 적 예고 안내 설정 종료
            case BattleTutorialStep.EndTurn: // 턴 종료 대기
                title.text = "턴 종료"; // 턴 종료 안내 제목 설정
                description.text = "행동을 마쳤다면 턴 종료 버튼을 누르세요. 적이 차례대로 행동합니다."; // 턴 종료 설명 설정
                break; // 턴 종료 안내 설정 종료
            case BattleTutorialStep.ObserveEnemy: // 적 행동 관찰 안내
                title.text = "적 행동 관찰"; // 적 행동 안내 제목 설정
                description.text = progress.CanAdvance ? "적 행동이 끝났습니다. 다음을 눌러 안내를 마치세요." : "적의 공격과 상태 변화를 확인하세요. 아군 턴이 시작되면 안내를 마칠 수 있습니다."; // 적 행동 진행 상태별 설명 설정
                break; // 적 행동 안내 설정 종료
        } // 단계별 문구 설정 종료
        nextButton.interactable = progress.CanAdvance; // 실제 진행 가능 여부에 맞춘 다음 버튼 설정
        nextButton.image.raycastTarget = progress.CanAdvance; // 행동 대기 중 비활성 확인 버튼의 전투 입력 통과
        UpdateHighlight(); // 현재 대상 화면 강조 갱신
    } // 안내 화면 갱신 종료
    private void LateUpdate() // 화면 크기와 대상 위치 반영
    { // 늦은 화면 갱신 시작
        if (progress != null && progress.IsActive) // 안내 진행 상태 확인
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
