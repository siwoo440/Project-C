using System; // 화면 대상과 위치 조회 사용
using TMPro; // 한글 안내 문구 사용
using UnityEngine; // 안내 화면과 위치 사용
using UnityEngine.UI; // 안내 버튼과 강조 영역 사용
public sealed class WorldTutorialView : MonoBehaviour // 거점과 탐사 안내 화면
{ // 안내 화면 시작
    private readonly RectTransform[] borders = new RectTransform[4]; // 강조 대상 금색 테두리
    private readonly Vector3[] corners = new Vector3[4]; // 강조 대상 화면 꼭짓점
    private WorldTutorialProgress progress; // 현재 안내 진행 상태
    private Func<Vector2> getPosition; // 현재 탐사 위치 조회
    private Func<int, RectTransform> findTarget; // 단계별 강조 화면 조회
    private RectTransform guide; // 안내 전체 화면 영역
    private TMP_Text title; // 현재 안내 제목
    private TMP_Text description; // 현재 안내 내용
    private Button nextButton; // 다음 안내 버튼
    private Button replayButton; // 탐사 안내 다시 보기 버튼
    private TutorialPanelLayout panelLayout; // 예시 이미지와 설명 공통 배치
    private ExplorationPlayerController trackedPlayer; // 자동 위치 변경을 감시할 탐사 플레이어
    public static WorldTutorialView Create(Canvas canvas, WorldTutorialProgress tutorial, Func<Vector2> position, Func<int, RectTransform> target, bool showReplayButton) // 거점 또는 탐사 안내 생성
    { // 안내 화면 생성 시작
        if (canvas == null || tutorial == null || position == null || target == null) // 필수 안내 연결 확인
        { // 필수 연결 누락 시작
            throw new ArgumentNullException(); // 잘못된 안내 생성 차단
        } // 필수 연결 누락 종료
        GameObject root = new GameObject("WorldTutorial", typeof(RectTransform), typeof(WorldTutorialView)); // 안내 전체 화면 생성
        root.transform.SetParent(canvas.transform, false); // 현재 화면 캔버스에 연결
        PreparationUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one); // 화면 전체 영역 설정
        WorldTutorialView view = root.GetComponent<WorldTutorialView>(); // 생성한 안내 화면 조회
        view.progress = tutorial; // 안내 진행 상태 연결
        view.getPosition = position; // 탐사 위치 조회 연결
        view.findTarget = target; // 강조 대상 조회 연결
        view.Build(showReplayButton); // 안내 화면 요소 생성
        tutorial.Changed += view.Refresh; // 단계 변경 화면 갱신 연결
        view.Refresh(); // 현재 안내 상태 표시
        return view; // 생성한 안내 화면 반환
    } // 안내 화면 생성 종료
    public void Replay() // 현재 위치에서 안내 다시 보기
    { // 안내 다시 보기 시작
        progress.Replay(getPosition()); // 실제 위치 기준 처음부터 안내 시작
        transform.SetAsLastSibling(); // 안내 화면을 현재 캔버스 위에 배치
    } // 안내 다시 보기 종료
    public void BindPlayer(ExplorationPlayerController player) // 탐사 자동 위치 변경 감시 연결
    { // 탐사 플레이어 연결 시작
        if (trackedPlayer != null) // 이전 탐사 플레이어 존재 확인
        { // 이전 위치 감시 해제 시작
            trackedPlayer.Teleported -= progress.RebasePosition; // 이전 자동 배치 감시 해제
        } // 이전 위치 감시 해제 종료
        trackedPlayer = player; // 새 탐사 플레이어 보관
        if (trackedPlayer != null) // 새 탐사 플레이어 존재 확인
        { // 새 위치 감시 연결 시작
            trackedPlayer.Teleported += progress.RebasePosition; // 맵 자동 배치를 이동 기준에 반영
        } // 새 위치 감시 연결 종료
    } // 탐사 플레이어 연결 종료
    private void Build(bool showReplayButton) // 안내 화면 구성
    { // 안내 구성 시작
        if (showReplayButton) // 탐사 화면 전용 다시 보기 확인
        { // 다시 보기 생성 시작
            replayButton = PreparationUi.CreateButton("ReplayButton", transform, "탐사 도움말", new Vector2(0.64f, 0.98f), new Vector2(0f, -8f), new Vector2(150f, 42f), Replay); // 탐사 안내 다시 보기 생성
        } // 다시 보기 생성 종료
        guide = PreparationUi.CreatePanel("Guide", transform, Vector2.zero, Vector2.one); // 안내 전체 배경 생성
        Image background = guide.GetComponent<Image>(); // 안내 전체 배경 조회
        background.color = new Color(0f, 0f, 0f, 0.7f); // 참고 화면과 유사한 어두운 배경
        background.raycastTarget = false; // 거점 버튼과 탐사 이동 입력 통과
        for (int index = 0; index < borders.Length; index++) // 네 방향 강조 테두리 순회
        { // 테두리 생성 시작
            GameObject line = new GameObject("Border" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 강조 테두리 객체 생성
            line.transform.SetParent(guide, false); // 안내 화면 자식 연결
            borders[index] = line.GetComponent<RectTransform>(); // 강조 테두리 영역 보관
            borders[index].anchorMin = Vector2.zero; // 왼쪽 아래 최소 앵커
            borders[index].anchorMax = Vector2.zero; // 왼쪽 아래 최대 앵커
            borders[index].pivot = Vector2.zero; // 왼쪽 아래 기준점 설정
            Image image = line.GetComponent<Image>(); // 강조 테두리 이미지 조회
            image.color = new Color(1f, 0.75f, 0.28f, 1f); // 금색 강조 색상 설정
            image.raycastTarget = false; // 강조 테두리 입력 통과
        } // 테두리 생성 종료
        panelLayout = TutorialPanelLayout.Create(guide, () => progress.Advance(), () => progress.Skip(), () => progress.GoBack()); // 예시와 페이지 이동 안내 배치 생성
        title = panelLayout.Title; // 오른쪽 안내 제목 연결
        description = panelLayout.Description; // 오른쪽 안내 설명 연결
        nextButton = panelLayout.NextButton; // 오른쪽 확인 버튼 연결
    } // 안내 구성 종료
    private void Refresh() // 현재 안내 내용과 버튼 갱신
    { // 안내 내용 갱신 시작
        bool active = progress != null && progress.IsActive; // 진행 중인 안내 확인
        guide.gameObject.SetActive(active); // 안내 진행 상태에 따른 화면 표시
        if (replayButton != null) // 별도 다시 보기 버튼 존재 확인
        { // 다시 보기 표시 설정 시작
            replayButton.gameObject.SetActive(!active); // 안내 종료 후 다시 보기 표시
        } // 다시 보기 표시 설정 종료
        if (!active) // 표시 중인 안내 확인
        { // 안내 없음 처리 시작
            return; // 안내 문구 갱신 중단
        } // 안내 없음 처리 종료
        if (progress.Kind == WorldTutorialKind.Lobby) // 거점 안내 종류 확인
        { // 거점 안내 내용 설정 시작
            SetLobbyText(progress.StepIndex); // 거점 단계별 문구 표시
            panelLayout.SetExample(progress.StepIndex == 1 ? "Formation" : progress.StepIndex == 3 ? "Facility" : "Lobby"); // 단계에 맞는 실제 거점 화면 예시 표시
        } // 거점 안내 내용 설정 종료
        else // 탐사 안내 종류 확인
        { // 탐사 안내 내용 설정 시작
            var page = progress.ExplorationPage; // 현재 탐사 안내 한 장 조회
            title.text = page.Title; // 방 종류와 이동 안내 제목 표시
            description.text = page.Text; // 현재 화면에서 가능한 행동 안내
            panelLayout.SetExample(page.Example); // 해당 방의 실제 화면 표시
        } // 탐사 안내 내용 설정 종료
        nextButton.interactable = progress.CanAdvance; // 실제 이동을 반영한 다음 버튼 설정
        panelLayout.SetPage(progress.StepIndex + 1, progress.StepCount, progress.CanGoBack, progress.CanAdvance); // 현재 안내 페이지와 이동 버튼 갱신
        nextButton.image.raycastTarget = progress.CanAdvance; // 이동 대기 중 비활성 확인 버튼의 화면 입력 통과
        UpdateHighlight(); // 현재 화면 대상 강조 갱신
    } // 안내 내용 갱신 종료
    private void SetLobbyText(int step) // 거점 단계별 안내 문구 설정
    { // 거점 문구 설정 시작
        switch (step) // 거점 안내 단계 분기
        { // 거점 단계 분기 시작
            case 0: // 캐릭터 관리 안내
                title.text = "거점과 캐릭터"; // 첫 거점 안내 제목 설정
                description.text = "이곳에서 캐릭터 상태를 확인하고 다음 탐사를 준비합니다. 캐릭터 버튼에서 보유 캐릭터를 볼 수 있습니다."; // 캐릭터 안내 설명 설정
                break; // 캐릭터 안내 종료
            case 1: // 파티 편성 안내
                title.text = "탐사 파티 편성"; // 편성 안내 제목 설정
                description.text = "탐사를 누르고 구역을 선택한 뒤 파티를 편성합니다. 캐릭터를 빼도 보유 전용 카드와 강화는 사라지지 않습니다."; // 실제 편성 경로 설명 설정
                break; // 편성 안내 종료
            case 2: // 회복 대기 안내
                title.text = "치료와 부활 대기"; // 회복 안내 제목 설정
                description.text = "치료 대기는 인정된 탐사 1회, 부활 대기는 2회입니다. 전투 턴 수와는 다릅니다."; // 실제 회복 기준 설명 설정
                break; // 회복 안내 종료
            case 3: // 설비 강화 안내
                title.text = "설비 강화"; // 설비 안내 제목 설정
                description.text = "탐사에서 얻은 자원으로 설비를 강화합니다. 설비별 현재 효과와 다음 효과, 필요 자원을 확인하세요."; // 설비 강화 설명 설정
                break; // 설비 안내 종료
            default: // 탐사 출발 안내
                title.text = "다음 탐사 준비"; // 탐사 안내 제목 설정
                description.text = "캐릭터 상태와 설비를 확인한 뒤 탐사를 눌러 구역을 선택하세요."; // 탐사 출발 설명 설정
                break; // 탐사 출발 안내 종료
        } // 거점 단계 분기 종료
    } // 거점 문구 설정 종료
    private void LateUpdate() // 실제 탐사 이동과 화면 위치 감시
    { // 늦은 안내 갱신 시작
        if (progress != null && progress.IsActive) // 진행 중인 안내 확인
        { // 실제 안내 갱신 시작
            progress.ObservePosition(getPosition()); // 실제 탐사 위치 변경 확인
            UpdateHighlight(); // 강조 대상 위치 갱신
        } // 실제 안내 갱신 종료
    } // 늦은 안내 갱신 종료
    private void UpdateHighlight() // 거점 메뉴 강조 위치 갱신
    { // 강조 위치 계산 시작
        RectTransform target = findTarget(progress.StepIndex); // 현재 단계 강조 대상 조회
        bool visible = target != null && target.gameObject.activeInHierarchy; // 실제 강조 대상 표시 확인
        for (int index = 0; index < borders.Length; index++) // 네 방향 테두리 순회
        { // 테두리 표시 시작
            borders[index].gameObject.SetActive(visible); // 대상이 있을 때만 테두리 표시
        } // 테두리 표시 종료
        if (!visible) // 강조할 대상 존재 확인
        { // 대상 없음 처리 시작
            return; // 강조 위치 계산 중단
        } // 대상 없음 처리 종료
        target.GetWorldCorners(corners); // 실제 화면 대상 꼭짓점 조회
        Vector3 minimum = guide.InverseTransformPoint(corners[0]); // 대상 왼쪽 아래 좌표 변환
        Vector3 maximum = guide.InverseTransformPoint(corners[2]); // 대상 오른쪽 위 좌표 변환
        float left = minimum.x - guide.rect.xMin - 5f; // 안내 영역 기준 왼쪽 위치
        float bottom = minimum.y - guide.rect.yMin - 5f; // 안내 영역 기준 아래 위치
        float width = maximum.x - minimum.x + 10f; // 강조 영역 너비
        float height = maximum.y - minimum.y + 10f; // 강조 영역 높이
        SetRect(borders[0], left, bottom, 3f, height); // 왼쪽 강조선 배치
        SetRect(borders[1], left + width - 3f, bottom, 3f, height); // 오른쪽 강조선 배치
        SetRect(borders[2], left, bottom, width, 3f); // 아래 강조선 배치
        SetRect(borders[3], left, bottom + height - 3f, width, 3f); // 위 강조선 배치
    } // 강조 위치 계산 종료
    private static void SetRect(RectTransform rect, float x, float y, float width, float height) // 화면 사각형 배치
    { // 사각형 배치 시작
        rect.anchoredPosition = new Vector2(x, y); // 왼쪽 아래 위치 설정
        rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height)); // 양수 영역 크기 설정
    } // 사각형 배치 종료
    private void OnDestroy() // 안내 화면 제거 처리
    { // 안내 화면 정리 시작
        BindPlayer(null); // 탐사 자동 위치 변경 감시 해제
        if (progress != null) // 연결된 진행 상태 확인
        { // 화면 갱신 연결 해제 시작
            progress.Changed -= Refresh; // 단계 변경 화면 갱신 해제
        } // 화면 갱신 연결 해제 종료
    } // 안내 화면 정리 종료
} // 안내 화면 종료
