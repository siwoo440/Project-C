using System; // 항목 이름 비교 사용
using System.Collections; // 상세 원화 로드 완료 대기
using System.Collections.Generic; // 도감 항목 목록 사용
using TMPro; // 도감 문구 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.UI; // 도감 버튼과 목록 사용

public sealed class CodexView : MonoBehaviour // 로비 도감 화면
{ // 도감 화면 시작
    private static readonly string[] CategoryNames = { "상호작용", "포션", "적", "유물", "상태 이상", "찢겨진 기록", "상호작용 오브젝트", "카드" }; // 실제 게임 자료를 보는 여덟 분류
    private const int CardCategory = 7; // 카드 전용 정렬이 필요한 도감 분류
    private enum EntrySort // 도감 목록에서 선택할 정렬 기준
    { // 정렬 기준 시작
        Name, // 이름 순서
        Type, // 카드 종류 순서
        Cost // 행동력 비용 순서
    } // 정렬 기준 종료
    private const int Columns = 6; // 기존 도감의 가로 항목 수
    private const float Width = 195f; // 원래 항목 너비
    private const float Height = 235f; // 원래 항목 높이
    private const float Gap = 18f; // 항목 사이 여백
    private const float Margin = 20f; // 목록 가장자리 여백
    private const float RowHeight = Height + Gap; // 한 줄 이동 거리
    private readonly List<Cell> pool = new List<Cell>(); // 현재 도감에서 재사용할 항목 셀
    private ScrollRect scroll; // 실제 위치와 속도를 가진 스크롤
    private Scrollbar listScrollbar; // 모든 분류에서 사용하는 세로 이동 막대
    private RectTransform categoryMenu; // 목록 오른쪽에 펼치는 분류 메뉴
    private readonly Button[] categoryOptions = new Button[CategoryNames.Length]; // 사용 가능한 분류 바로가기
    private readonly Image[] sortArrows = new Image[3]; // 정렬 버튼의 방향 표시 이미지
    private int firstRow = -1; // 마지막으로 연결한 첫 행
    private int firstImageRow = -1; // 화면에 걸린 첫 이미지 행
    private int lastImageRow = -1; // 화면에 걸린 마지막 이미지 행
    private float viewportHeight = -1f; // 화면 크기 변경 감지용 높이
    private RectTransform categories; // 분류 선택 화면
    private RectTransform list; // 항목 목록 화면
    private RectTransform grid; // 항목 카드 격자
    private TMP_Text title; // 현재 도감 제목
    private TMP_Text empty; // 빈 목록 안내
    private Sprite ascendingArrow; // 오름차순으로 향하는 위쪽 화살표
    private Sprite descendingArrow; // 내림차순으로 향하는 아래쪽 화살표
    private List<CodexEntry> entries; // 현재 분류 항목
    private EntrySort sortMode; // 현재 적용한 정렬 기준
    private bool reverse; // 현재 정렬 기준의 내림차순 여부
    public static CodexView Open(Canvas canvas) // 도감 화면 열기
    { // 도감 열기 시작
        if (canvas == null || FindFirstObjectByType<CodexView>() != null) // 캔버스와 중복 화면 확인
        { // 화면 생성 불가 처리 시작
            return null; // 중복 도감 생성 중단
        } // 화면 생성 불가 처리 종료
        RectTransform root = PreparationUi.CreateOverlay(canvas, "Codex"); // 전체 도감 화면 생성
        CodexView view = root.gameObject.AddComponent<CodexView>(); // 도감 동작 연결
        view.Build(); // 분류와 목록 화면 구성
        return view; // 생성 화면 반환
    } // 도감 열기 종료
    private void Build() // 도감 화면 요소 배치
    { // 도감 화면 구성 시작
        title = PreparationUi.CreateText("Title", transform, "도감", 42f); // 도감 제목 생성
        PreparationUi.Place(title.rectTransform, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(800f, 70f)); // 제목 위치 설정
        PreparationUi.CreateButton("BackButton", transform, "뒤로", new Vector2(0.085f, 0.93f), Vector2.zero, new Vector2(200f, 70f), Back); // 이전 화면 버튼 생성
        GameObject categoryObject = new GameObject("Categories", typeof(RectTransform)); // 분류 버튼 영역 생성
        categoryObject.transform.SetParent(transform, false); // 도감 화면 연결
        categories = categoryObject.GetComponent<RectTransform>(); // 분류 영역 조회
        PreparationUi.Stretch(categories, Vector2.zero, Vector2.one); // 분류 영역 화면 채우기
        for (int index = 0; index < CategoryNames.Length; index++) // 여덟 개의 도감 분류 순회
        { // 분류 버튼 생성 시작
            int category = index; // 버튼별 분류 번호 보관
            float x = 0.185f + index % 4 * 0.21f; // 두 줄 네 칸의 분류 위치 계산
            float y = index < 4 ? 0.62f : 0.40f; // 분류별 세로 위치 계산
            PreparationUi.CreateButton("Category_" + index, categories, CategoryNames[index], new Vector2(x, y), Vector2.zero, new Vector2(320f, 140f), () => OpenCategory(category)); // 분류 선택 버튼 생성
        } // 분류 버튼 생성 종료
        BuildList(); // 분류별 목록 화면 생성
        list.gameObject.SetActive(false); // 초기 분류 목록 숨김
    } // 도감 화면 구성 종료
    private void BuildList() // 도감 항목 목록 구성
    { // 항목 목록 구성 시작
        list = PreparationUi.CreatePanel("ListPanel", transform, new Vector2(0.035f, 0.055f), new Vector2(0.965f, 0.86f)); // 항목 목록 패널 생성
        LobbyUiArt.Apply(list.GetComponent<Image>(), "FormationPanel"); // 기존 도감 패널 디자인 적용
        scroll = list.gameObject.AddComponent<ScrollRect>(); // 도감 항목 스크롤 추가
        RectTransform viewport = PreparationUi.CreatePanel("Viewport", list, new Vector2(0.025f, 0.025f), new Vector2(0.85f, 0.975f)); // 목록 표시 영역 생성
        viewport.GetComponent<Image>().color = new Color(0.02f, 0.045f, 0.05f, 0.88f); // 목록 안쪽 배경 설정
        viewport.gameObject.AddComponent<RectMask2D>(); // 목록 영역 밖 항목 가리기
        GameObject gridObject = new GameObject("Grid", typeof(RectTransform)); // 실제 위치에 셀을 배치할 목록 생성
        gridObject.transform.SetParent(viewport, false); // 표시 영역에 격자 연결
        grid = gridObject.GetComponent<RectTransform>(); // 항목 격자 영역 조회
        grid.anchorMin = new Vector2(0f, 1f); // 격자 시작 앵커 설정
        grid.anchorMax = Vector2.one; // 격자 끝 앵커 설정
        grid.pivot = new Vector2(0.5f, 1f); // 격자 위쪽 기준 설정
        grid.anchoredPosition = Vector2.zero; // 격자 시작 위치 설정
        scroll.viewport = viewport; // 스크롤 표시 영역 연결
        scroll.content = grid; // 스크롤 내용 영역 연결
        scroll.horizontal = false; // 가로 스크롤 차단
        scroll.onValueChanged.AddListener(position => RefreshVisible()); // 위치가 바뀐 경우 보이는 행 갱신
        scroll.vertical = true; // 세로 스크롤 허용
        scroll.movementType = ScrollRect.MovementType.Clamped; // 목록 범위 밖 이동 차단
        scroll.scrollSensitivity = GameplaySpeedSettings.ScrollSensitivity(36f); // 선택한 스크롤 속도 적용
        BuildScrollbar(); // 스크롤 내용과 연결한 오른쪽 이동 막대 생성
        ascendingArrow = Resources.Load<Sprite>("UI/Lobby/SortAscending"); // 오름차순 화살표 원화 연결
        descendingArrow = Resources.Load<Sprite>("UI/Lobby/SortDescending"); // 내림차순 화살표 원화 연결
        BuildSortButton("SortButton", list, "이름순", new Vector2(0.92f, 0.89f), new Vector2(190f, 70f), EntrySort.Name); // 모든 도감의 이름 정렬 버튼
        PreparationUi.CreateButton("CategoryButton", list, "분류", new Vector2(0.92f, 0.785f), Vector2.zero, new Vector2(190f, 60f), () => categoryMenu.gameObject.SetActive(!categoryMenu.gameObject.activeSelf)); // 정렬 아래의 분류 메뉴 토글
        categoryMenu = PreparationUi.CreatePanel("CategoryMenu", list, new Vector2(0.858f, 0.06f), new Vector2(0.982f, 0.738f)); // 스크롤바 오른쪽의 분류 메뉴 영역
        categoryMenu.GetComponent<Image>().color = new Color(0.018f, 0.07f, 0.08f, 0.97f); // 목록과 구별되는 짙은 청록 배경
        BuildSortButton("SortByType", categoryMenu, "종류순", new Vector2(0.5f, 0.95f), new Vector2(176f, 48f), EntrySort.Type); // 검부터 저주까지 종류별 순서 선택
        BuildSortButton("SortByCost", categoryMenu, "AP 비용순", new Vector2(0.5f, 0.85f), new Vector2(176f, 48f), EntrySort.Cost); // 카드 행동력 비용순 선택
        for (int index = 0; index < CategoryNames.Length; index++) // 모든 도감 분류 바로가기 설치
        { // 목록 안 분류 선택지 생성 시작
            int category = index; // 버튼에서 사용할 현재 분류 번호
            float row = 1f - (index + 0.5f) / CategoryNames.Length; // 오른쪽 메뉴에 맞춘 일정한 세로 간격
            Button button = PreparationUi.CreateButton("Category_" + index, categoryMenu, CategoryNames[index], new Vector2(0.5f, row), Vector2.zero, new Vector2(176f, 52f), () => OpenCategory(category)); // 실제 도감 목록으로 전환하는 항목
            TMP_Text caption = button.GetComponentInChildren<TMP_Text>(); // 분류 선택지의 표시 이름
            caption.fontSizeMax = 20f; // 긴 분류도 안쪽에 들어오는 글자 크기
            categoryOptions[index] = button; // 현재 분류에 해당하는 버튼 상태 제어용 참조
        } // 목록 안 분류 선택지 생성 종료
        categoryMenu.gameObject.SetActive(false); // 처음 목록에 들어올 때 선택지 숨김
        empty = PreparationUi.CreateText("Empty", list, "표시할 항목이 없습니다.", 30f); // 빈 목록 안내 생성
        PreparationUi.Place(empty.rectTransform, new Vector2(0.43f, 0.5f), Vector2.zero, new Vector2(700f, 80f)); // 빈 목록 안내 위치 설정
    } // 항목 목록 구성 종료
    private void BuildSortButton(string name, RectTransform parent, string caption, Vector2 anchor, Vector2 size, EntrySort mode) // 방향 그림이 붙은 정렬 버튼 생성
    { // 정렬 버튼 생성 시작
        Button button = PreparationUi.CreateButton(name, parent, caption, anchor, Vector2.zero, size, () => ToggleSort(mode)); // 같은 기준으로 방향을 바꾸는 실제 버튼
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(); // 정렬 기준 글자 조회
        PreparationUi.Stretch(label.rectTransform, new Vector2(0.07f, 0f), new Vector2(0.81f, 1f)); // 글자와 오른쪽 화살표 자리 분리
        Image arrow = PreparationUi.CreatePanel("Direction", button.transform, Vector2.zero, Vector2.one).GetComponent<Image>(); // 현재 방향을 보여줄 독립 그림
        PreparationUi.Place(arrow.rectTransform, new Vector2(0.86f, 0.5f), Vector2.zero, new Vector2(24f, 24f)); // 정렬 버튼 오른쪽 안쪽의 방향 표시
        arrow.color = Color.white; // 화살표 원화의 금색 유지
        arrow.preserveAspect = true; // 위·아래 화살표 비율 보존
        arrow.raycastTarget = false; // 화살표 부분의 클릭을 정렬 버튼으로 전달
        sortArrows[(int)mode] = arrow; // 정렬 기준으로 찾을 화살표 저장
    } // 정렬 버튼 생성 종료
    private void BuildScrollbar() // 도감 목록 오른쪽의 공용 세로 이동 막대 설치
    { // 도감 스크롤바 만들기 시작
        RectTransform track = PreparationUi.CreatePanel("VerticalScrollbar", list, new Vector2(0.815f, 0.045f), new Vector2(0.838f, 0.955f)); // 마지막 카드 열과 정렬 메뉴 사이의 이동 영역
        track.GetComponent<Image>().color = new Color(0.047f, 0.12f, 0.14f, 1f); // 손잡이와 대비되는 어두운 트랙
        RectTransform slideArea = new GameObject("SlidingArea", typeof(RectTransform)).GetComponent<RectTransform>(); // 손잡이의 이동 범위
        slideArea.SetParent(track, false); // 자체 입력을 가로채지 않는 트랙 자식
        PreparationUi.Stretch(slideArea, Vector2.zero, Vector2.one); // 트랙을 채우는 손잡이 이동 범위
        slideArea.offsetMin = new Vector2(4f, 4f); // 테두리 왼쪽과 아래쪽 여백
        slideArea.offsetMax = new Vector2(-4f, -4f); // 테두리 오른쪽과 위쪽 여백
        RectTransform grip = PreparationUi.CreatePanel("Handle", slideArea, Vector2.zero, Vector2.one); // 드래그와 현재 위치에 쓰는 손잡이
        Image gripImage = grip.GetComponent<Image>(); // 손잡이의 표시와 입력 이미지
        gripImage.color = new Color(0.66f, 0.49f, 0.26f, 1f); // 기존 도감 테두리에 맞춘 금속 강조색
        listScrollbar = track.gameObject.AddComponent<Scrollbar>(); // 트랙 클릭과 손잡이 드래그 입력
        listScrollbar.handleRect = grip; // 세로 위치와 크기를 조정할 손잡이 사각형
        listScrollbar.targetGraphic = gripImage; // 활성과 비활성 상태를 표시할 그림
        listScrollbar.direction = Scrollbar.Direction.BottomToTop; // 목록의 맨 위를 값 1로 설정
        listScrollbar.value = 1f; // 최초 표시 위치를 목록 맨 위로 초기화
        scroll.verticalScrollbar = listScrollbar; // 기존 휠과 이동 막대의 양방향 위치 연결
    } // 도감 스크롤바 만들기 종료
    private void OpenCategory(int category) // 선택 분류의 목록 열기
    { // 분류 목록 열기 시작
        entries = CodexData.Load(category); // 실제 분류 데이터 조회
        categoryMenu.gameObject.SetActive(false); // 분류를 고른 뒤 바로가기 닫기
        bool isCard = category == CardCategory; // 카드 전용 정렬 선택지 표시 여부
        sortArrows[(int)EntrySort.Type].transform.parent.gameObject.SetActive(isCard); // 카드 분류의 종류순 노출
        sortArrows[(int)EntrySort.Cost].transform.parent.gameObject.SetActive(isCard); // 카드 분류의 AP 비용순 노출
        int sortRows = isCard ? 2 : 0; // 분류 버튼 위의 카드 정렬 행 수
        int menuRows = CategoryNames.Length + sortRows; // 패널에 들어가는 전체 버튼 행 수
        for (int index = 0; index < categoryOptions.Length; index++) // 현재 선택 여부를 각 버튼에 반영
        { // 분류 바로가기 상태 갱신 시작
            Button button = categoryOptions[index]; // 이번 행의 분류 버튼
            button.interactable = index != category; // 현재 분류를 비활성 색으로 구분
            float row = 1f - (index + sortRows + 0.5f) / menuRows; // 정렬 버튼 아래에 배치할 위치 계산
            PreparationUi.Place((RectTransform)button.transform, new Vector2(0.5f, row), Vector2.zero, new Vector2(176f, isCard ? 48f : 52f)); // 패널 높이에 맞춘 분류 버튼 배치
        } // 분류 바로가기 상태 갱신 종료
        sortMode = EntrySort.Name; // 분류를 열 때 기본 이름 정렬 선택
        reverse = false; // 기본 오름차순 설정
        title.text = "도감 · " + CategoryNames[category]; // 현재 분류 제목 표시
        categories.gameObject.SetActive(false); // 분류 선택 화면 숨김
        list.gameObject.SetActive(true); // 항목 목록 화면 표시
        RebuildEntries(); // 선택 분류의 카드 생성
    } // 분류 목록 열기 종료
    private void ToggleSort(EntrySort mode) // 목록 정렬 기준 또는 방향 전환
    { // 정렬 전환 시작
        reverse = mode == sortMode && !reverse; // 같은 기준을 다시 누른 경우에만 방향 반전
        sortMode = mode; // 선택한 정렬 기준 저장
        RebuildEntries(); // 변경 순서로 카드 갱신
    } // 정렬 전환 종료
    private int CompareEntries(CodexEntry left, CodexEntry right) // 같은 기준과 방향으로 두 도감 항목을 비교
    { // 도감 항목 비교 시작
        int comparison = sortMode switch // 현재 기준의 우선 비교값 조회
        { // 정렬 기준별 비교 시작
            EntrySort.Type => Nullable.Compare(left.CardKind, right.CardKind), // 검·지팡이·성배·펜타클·방패·저주의 원본 순서
            EntrySort.Cost => left.CostSortKey.CompareTo(right.CostSortKey), // 고정과 가변 행동력 비용 순서
            _ => 0 // 이름 정렬은 아래 이름 비교 사용
        }; // 정렬 기준별 비교 종료
        if (comparison == 0) // 이름순이거나 앞선 비용 또는 종류가 같은 경우 확인
        { // 동률 처리 시작
            comparison = string.Compare(left.Name, right.Name, StringComparison.Ordinal); // 동률 카드의 이름 순서 유지
        } // 동률 처리 종료
        return reverse ? -comparison : comparison; // 오름차순 또는 전체 역순 적용
    } // 도감 항목 비교 종료
    private void RebuildEntries() // 현재 분류의 항목 카드 갱신
    { // 항목 카드 갱신 시작
        entries.Sort(CompareEntries); // 선택한 기준과 방향으로 실제 목록 정렬
        for (int index = 0; index < sortArrows.Length; index++) // 각 정렬 기준의 방향 그림 순회
        { // 정렬 방향 갱신 시작
            Image arrow = sortArrows[index]; // 이번 정렬 기준의 방향 그림
            arrow.enabled = index == (int)sortMode; // 현재 선택된 기준에만 방향 표시
            arrow.sprite = reverse ? descendingArrow : ascendingArrow; // 현재 정렬 방향의 위·아래 원화 적용
        } // 정렬 방향 갱신 종료
        empty.gameObject.SetActive(entries.Count == 0); // 항목이 없을 때 안내 표시
        int rows = (entries.Count + Columns - 1) / Columns; // 여섯 열 기준 전체 행 수
        grid.sizeDelta = new Vector2(0f, Margin * 2f + rows * Height + Mathf.Max(0, rows - 1) * Gap); // 스크롤할 전체 높이 보존
        scroll.StopMovement(); // 정렬 전 관성이 새 첫 행을 밀지 않도록 정지
        grid.anchoredPosition = Vector2.zero; // 분류와 정렬 변경 시 맨 위로 복귀
        listScrollbar.SetValueWithoutNotify(1f); // 위치 초기화와 함께 손잡이를 맨 위로 이동
        firstRow = -1; // 새 항목 순서를 다시 연결할 표시
        RefreshVisible(); // 현재 화면과 인접한 행 즉시 표시
    } // 항목 카드 갱신 종료
    private void LateUpdate() // 창 크기나 캔버스 배율에 따른 표시 범위 조정
    { // 표시 크기 확인 시작
        if (entries != null && list.gameObject.activeInHierarchy && !Mathf.Approximately(viewportHeight, scroll.viewport.rect.height)) // 실제 보이는 영역의 세로 변화 확인
        { // 변경된 화면 갱신 시작
            RefreshVisible(); // 새 화면에 들어오는 행만 추가
        } // 변경된 화면 갱신 종료
    } // 표시 크기 확인 종료
    private void RefreshVisible() // 현재 화면의 셀만 실제 항목에 연결
    { // 화면 행 연결 시작
        if (entries == null || !list.gameObject.activeInHierarchy) // 닫힌 도감과 초기 화면 확인
        { // 불필요한 갱신 제외 시작
            return; // 비활성 목록 재배치 생략
        } // 불필요한 갱신 제외 종료
        float height = scroll.viewport.rect.height; // 현재 화면에서 사용할 세로 길이
        listScrollbar.interactable = height > 0f && grid.rect.height > height + 0.5f; // 화면을 넘는 분류에서만 스크롤바 이동 허용
        int rows = (entries.Count + Columns - 1) / Columns; // 데이터 전체의 행 수
        int visibleRows = Mathf.Min(rows, Mathf.CeilToInt(Mathf.Max(0f, height) / RowHeight) + 2); // 일부만 보이는 행과 위아래 여유분 확보
        float top = grid.anchoredPosition.y; // 현재 표시 영역 상단의 목록 좌표
        int start = Mathf.Clamp(Mathf.FloorToInt((top - Margin) / RowHeight) - 1, 0, rows - visibleRows); // 여유 셀을 포함한 첫 재사용 행
        int imageStart = Mathf.Max(0, Mathf.FloorToInt((top - Margin - Height) / RowHeight) + 1); // 아랫변이 표시 영역 안에 들어오는 첫 행
        int imageEnd = height > 0f ? Mathf.Min(rows - 1, Mathf.CeilToInt((top + height - Margin) / RowHeight) - 1) : -1; // 윗변이 표시 영역 안에 들어오는 마지막 행
        if (start == firstRow && imageStart == firstImageRow && imageEnd == lastImageRow && Mathf.Approximately(height, viewportHeight)) // 셀 창과 실제 이미지 영역의 변경 여부 확인
        { // 기존 연결 유지 시작
            return; // 같은 항목과 그림의 중복 연결 생략
        } // 기존 연결 유지 종료
        firstRow = start; // 갱신한 첫 행 보관
        firstImageRow = imageStart; // 실제 이미지 요청의 첫 행 보관
        lastImageRow = imageEnd; // 실제 이미지 요청의 마지막 행 보관
        viewportHeight = height; // 이번 크기 보관
        int capacity = Mathf.Min(entries.Count, visibleRows * Columns); // 현재 목록에 필요한 최대 셀 수
        while (pool.Count < capacity) // 더 큰 화면이나 분류의 첫 표시 확인
        { // 재사용 셀 추가 시작
            pool.Add(new Cell(grid, ShowDetail)); // 새로 필요한 셀만 한 번 생성
        } // 재사용 셀 추가 종료
        for (int offset = 0; offset < pool.Count; offset++) // 현재 도감이 소유한 셀 순회
        { // 개별 셀 연결 시작
            int index = start * Columns + offset; // 표시할 실제 항목 순번
            if (offset < capacity) // 현재 화면 범위의 셀 확인
            { // 보이는 행 처리 시작
                var cell = pool[index % capacity]; // 겹치는 항목은 같은 셀에 남는 순환 위치
                int row = index / Columns; // 현재 재사용 셀의 실제 목록 행
                cell.Bind(index, index < entries.Count ? entries[index] : null, row >= imageStart && row <= imageEnd); // 표시 영역에 들어온 행만 썸네일 연결
            } // 보이는 행 처리 종료
            else // 이전 분류 또는 큰 창에서 남은 셀 확인
            { // 여유 셀 정리 시작
                pool[offset].Bind(-1, null, false); // 다음 사용을 위해 이미지 연결을 해제하고 숨김
            } // 여유 셀 정리 종료
        } // 개별 셀 연결 종료
    } // 화면 행 연결 종료
    private sealed class Cell // 이름과 그림과 버튼을 한 번 생성하는 도감 셀
    { // 재사용 셀 시작
        private readonly RectTransform area; // 셀 위치와 크기
        private readonly RectTransform imageArea; // 카드 배지 유무에 따라 배치할 원화 영역
        private readonly Image icon; // 현재 항목의 그림
        private readonly TMP_Text label; // 현재 항목의 이름
        private RectTransform cardMeta; // 카드 분류에서만 표시하는 왼쪽 위 배지
        private Image typeIcon; // 전투와 동일한 카드 종류 아이콘
        private TMP_Text costLabel; // 기본 행동력 비용 표기
        private CodexEntry entry; // 버튼이 열 현재 항목
        private int index = -1; // 현재 셀이 표시하는 논리적 순번
        private bool thumbnailVisible; // 이전 갱신에서 실제로 보인 셀 이미지 여부
        public Cell(RectTransform parent, Action<CodexEntry> showDetail) // 공통 셀 화면과 단일 클릭 연결 생성
        { // 셀 생성 시작
            area = PreparationUi.CreatePanel("Entry", parent, Vector2.up, Vector2.up); // 상단 왼쪽 좌표를 사용하는 카드 영역
            area.pivot = Vector2.up; // 카드의 왼쪽 위 기준점
            area.sizeDelta = new Vector2(Width, Height); // 기존 도감의 카드 크기
            var background = area.GetComponent<Image>(); // 카드 바탕 이미지
            LobbyUiArt.Apply(background, "FormationCard"); // 기존 카드 바탕 적용
            var button = area.gameObject.AddComponent<Button>(); // 상세 열기 버튼
            button.targetGraphic = background; // 기존 버튼 강조 대상 유지
            button.onClick.AddListener(() => showDetail(entry)); // 교체된 현재 항목을 여는 단일 연결
            imageArea = PreparationUi.CreatePanel("Image", area, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.92f)); // 원화 영역과 기존 기본 위치
            icon = imageArea.GetComponent<Image>(); // 재사용할 그림 표시
            icon.color = Color.white; // 원화 색상 유지
            icon.preserveAspect = true; // 원화 비율 유지
            icon.raycastTarget = false; // 상세 버튼 입력 허용
            label = PreparationUi.CreateText("Name", area, string.Empty, 23f); // 재사용할 이름 표시
        } // 셀 생성 종료
        public void Bind(int nextIndex, CodexEntry nextEntry, bool imageVisible) // 자료와 표시 영역이 바뀐 셀 재연결
        { // 셀 내용 변경 시작
            if (nextEntry == null) // 빈 분류 또는 마지막 행의 빈 칸 확인
            { // 비사용 셀 처리 시작
                if (area.gameObject.activeSelf) // 실제 표시 중인 셀 확인
                { // 비사용 화면 숨김 시작
                    entry = null; // 이전 상세 대상 해제
                    icon.sprite = null; // 이전 항목 그림 연결 해제
                    thumbnailVisible = false; // 이 썸네일을 표시 영역 밖으로 기록
                    area.gameObject.SetActive(false); // 표시와 클릭 차단
                } // 비사용 화면 숨김 종료
                return; // 빈 항목 배치 생략
            } // 비사용 셀 처리 종료
            if (index == nextIndex && entry == nextEntry) // 스크롤 전부터 보이던 같은 항목 확인
            { // 같은 셀 유지 시작
                if (thumbnailVisible != imageVisible) // 셀 위치를 유지한 채 표시 영역만 바뀐 경우 확인
                { // 실제 이미지 상태 갱신 시작
                    SetThumbnailVisible(imageVisible); // 화면에 들어온 셀의 썸네일만 연결
                } // 실제 이미지 상태 갱신 종료
                return; // 이름과 셀 위치의 중복 배치 생략
            } // 같은 셀 유지 종료
            index = nextIndex; // 실제 항목 순번 저장
            entry = nextEntry; // 현재 클릭 시 열 항목 저장
            area.name = "Entry_" + index; // 기존 도감의 실제 항목 식별 이름
            area.anchoredPosition = new Vector2(Margin + index % Columns * (Width + Gap), -Margin - index / Columns * RowHeight); // 전체 목록 안의 원래 위치
            label.text = entry.Name; // 현재 실제 항목 이름
            BindCardMetadata(); // 실제 카드의 종류와 기본 비용 표시
            SetThumbnailVisible(imageVisible); // 새 항목의 작은 그림 연결과 이름 위치 갱신
            area.gameObject.SetActive(true); // 새 항목의 표시와 클릭 허용
        } // 셀 내용 변경 종료
        private void BindCardMetadata() // 현재 셀의 원화 범위와 카드 배지 갱신
        { // 카드 배지 연결 시작
            bool card = entry.CardKind.HasValue; // 카드 분류로 연결한 셀인지 확인
            PreparationUi.Stretch(imageArea, new Vector2(0.08f, 0.25f), new Vector2(0.92f, card ? 0.81f : 0.92f)); // 카드 배지와 원화가 겹치지 않는 안쪽 공간
            if (!card) // 카드 외 항목으로 재사용됐는지 확인
            { // 카드 배지 감춤 시작
                cardMeta?.gameObject.SetActive(false); // 다른 분류에서 이전 AP 정보 제거
                return; // 카드 정보 배치 생략
            } // 카드 배지 감춤 종료
            if (cardMeta == null) // 이 재사용 셀의 카드 배지 최초 연결 확인
            { // 카드 종류와 비용 UI 생성 시작
                cardMeta = PreparationUi.CreatePanel("CardMeta", area, Vector2.up, Vector2.up); // 카드 왼쪽 위의 작은 배지 배경
                cardMeta.pivot = Vector2.up; // 테두리에서 왼쪽 위 여백을 측정할 기준
                PreparationUi.Place(cardMeta, Vector2.up, new Vector2(12f, -11f), new Vector2(104f, 30f)); // 아이콘과 AP에 맞춘 카드 왼쪽 위 배지
                Image plate = cardMeta.GetComponent<Image>(); // 기존 도감 금속 배지에 연결할 이미지
                LobbyUiArt.Apply(plate, "Button"); // 이름순 버튼과 어울리는 테두리 적용
                plate.raycastTarget = false; // 배지 너머의 카드 상세 입력 유지
                typeIcon = PreparationUi.CreatePanel("Icon", cardMeta, Vector2.zero, Vector2.one).GetComponent<Image>(); // 카드 종류에 대응하는 전투 아이콘 영역
                PreparationUi.Place(typeIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(25f, 25f)); // 배지 맨 왼쪽의 종류 그림
                typeIcon.preserveAspect = true; // 카드 종류 그림의 원본 비율 유지
                typeIcon.raycastTarget = false; // 전투 아이콘의 카드 입력 차단 해제
                costLabel = PreparationUi.CreateText("Cost", cardMeta, string.Empty, 17f); // 카드 기본 AP 표시
                costLabel.fontStyle = FontStyles.Bold; // AP 값을 강조하는 굵은 글자
                costLabel.color = new Color(1f, 0.8f, 0.48f, 1f); // 종류와 구별되는 밝은 금색 비용
                costLabel.textWrappingMode = TextWrappingModes.NoWrap; // 가변 AP도 한 줄로 표시
            } // 카드 종류와 비용 UI 생성 종료
            CardType kind = entry.CardKind.Value; // 현재 카드 원본 종류
            typeIcon.enabled = BattleUiArt.ApplyCardType(typeIcon, kind); // 지원되는 종류에는 전투의 기존 그림 사용
            PreparationUi.Stretch(costLabel.rectTransform, new Vector2(typeIcon.enabled ? 0.33f : 0.05f, 0f), new Vector2(0.95f, 1f)); // 아이콘 유무에 맞춘 비용 문구 위치
            costLabel.text = "AP " + entry.CostLabel; // 원본 기본 비용 또는 가변 비용 표시
            cardMeta.gameObject.SetActive(true); // 새로 연결된 실제 카드 배지 표시
        } // 카드 배지 연결 종료
        private void SetThumbnailVisible(bool visible) // 현재 표시 영역에 해당하는 작은 그림만 연결
        { // 썸네일 표시 처리 시작
            thumbnailVisible = visible; // 이미지 표시 범위 변경 기록
            icon.sprite = visible ? entry.LoadThumbnail() : null; // 화면 밖 항목은 원본과 작은 그림을 모두 읽지 않음
            icon.gameObject.SetActive(icon.sprite != null); // 표시할 작은 그림이 없는 영역 숨김
            bool hasImage = visible ? icon.sprite != null : entry.HasArtwork; // 화면 밖에서도 이미지 슬롯이 있는 이름 자리 보존
            PreparationUi.Stretch(label.rectTransform, hasImage ? new Vector2(0.06f, 0.05f) : new Vector2(0.06f, 0.3f), hasImage ? new Vector2(0.94f, 0.23f) : new Vector2(0.94f, 0.7f)); // 이미지 유무에 맞춘 기존 이름 위치
        } // 썸네일 표시 처리 종료
    } // 재사용 셀 종료
    private void ShowDetail(CodexEntry entry) // 실제 항목 상세 정보 표시
    { // 상세 화면 표시 시작
        if (entry == null || transform.Find("Detail") != null) // 실제 항목과 중복 상세 화면 확인
        { // 중복 상세 화면 처리 시작
            return; // 중복 상세 생성 중단
        } // 중복 상세 화면 처리 종료
        RectTransform detail = PreparationUi.CreatePanel("Detail", transform, Vector2.zero, Vector2.one); // 상세 화면 배경 생성
        detail.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 상세 화면 뒤 어두운 배경 설정
        RectTransform panel = PreparationUi.CreatePanel("Panel", detail, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f)); // 원화와 설명을 담을 상세 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 기존 패널 디자인 적용
        Vector2 titleAnchor = new Vector2(0.5f, 0.75f); // 제목판의 위쪽 중앙 기준점
        RectTransform nameplate = CreateDetailBackdrop(detail, "Nameplate", titleAnchor, titleAnchor, 2f); // 금테 이름판 생성
        TMP_Text heading = PreparationUi.CreateText("Name", detail, entry.Name, 36f); // 항목 상세 이름
        const float titlePadding = 96f; // 양쪽 테두리와 제목 사이의 총 여백
        float titleWidth = Mathf.Clamp(Mathf.Ceil(heading.preferredWidth) + titlePadding, 400f, 760f); // 현재 언어의 글자 폭에 맞춘 이름판
        PreparationUi.Place(nameplate, titleAnchor, Vector2.zero, new Vector2(titleWidth, 100f)); // 이름판의 폭과 고정 높이
        PreparationUi.Place(heading.rectTransform, titleAnchor, Vector2.zero, new Vector2(titleWidth - titlePadding, 64f)); // 이름판 안쪽 제목 여백
        float descriptionLeft = entry.HasArtwork ? 0.49f : 0.218f; // 원화 유무에 따른 설명 시작점
        RectTransform descriptionPanel = CreateDetailBackdrop(detail, "DescriptionPanel", new Vector2(descriptionLeft, 0.315f), new Vector2(0.782f, 0.695f), 2.5f); // 오른쪽 설명 배경
        TMP_Text description = PreparationUi.CreateText("Description", detail, string.IsNullOrWhiteSpace(entry.Description) ? "등록된 설명이 없습니다." : entry.Description, 26f); // 등록 설명과 자동 글자 크기
        InsetDetailContent(description.rectTransform, descriptionPanel, new Vector2(26f, 22f)); // 테두리 안쪽의 설명 여백
        if (entry.HasArtwork) // 실제 원화가 있는 항목 확인
        { // 상세 이미지 배치 시작
            RectTransform artworkFrame = CreateDetailBackdrop(detail, "ArtworkFrame", new Vector2(0.218f, 0.315f), new Vector2(0.477f, 0.695f), 3.5f); // 왼쪽 원화 액자
            RectTransform area = PreparationUi.CreatePanel("Artwork", detail, artworkFrame.anchorMin, artworkFrame.anchorMax); // 상세 원화 표시 영역
            InsetDetailContent(area, artworkFrame, new Vector2(22f, 22f)); // 액자 안쪽의 원화 여백
            Image artwork = area.GetComponent<Image>(); // 상세 원화 표시 요소
            artwork.color = Color.white; // 원본 이미지 색상 유지
            artwork.preserveAspect = true; // 원본 이미지 비율 유지
            artwork.raycastTarget = false; // 원화의 입력 가로채기 방지
            artwork.enabled = false; // 로드 전 빈 이미지 숨김
            StartCoroutine(ShowArtwork(entry, artwork)); // 상세 원화 비동기 로드
        } // 상세 이미지 배치 종료
        PreparationUi.CreateButton("CloseButton", detail, "닫기", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(200f, 70f), () => CloseDetail(detail)); // 상세 화면 닫기 버튼 생성
    } // 상세 화면 표시 종료
    private static RectTransform CreateDetailBackdrop(RectTransform detail, string name, Vector2 minimum, Vector2 maximum, float borderScale) // 도감 상세 배경 생성
    { // 상세 배경 생성 시작
        RectTransform area = PreparationUi.CreatePanel(name, detail, minimum, maximum); // 배경의 위치와 크기
        Image backdrop = area.GetComponent<Image>(); // 배경 스프라이트 표시 요소
        backdrop.sprite = Resources.Load<Sprite>("UI/Lobby/CodexDetail" + name); // 영역별 제작 배경
        backdrop.enabled = backdrop.sprite != null; // 리소스 누락 시 기본 패널 표시
        backdrop.color = Color.white; // 제작 배경 원본 색상
        backdrop.type = Image.Type.Sliced; // 해상도에 따른 모서리 변형 방지
        backdrop.pixelsPerUnitMultiplier = borderScale; // 테두리의 화면 두께 조정
        backdrop.raycastTarget = false; // 배경의 입력 가로채기 방지
        return area; // 패딩 기준 배경 영역
    } // 상세 배경 생성 종료
    private static void InsetDetailContent(RectTransform content, RectTransform backdrop, Vector2 padding) // 상세 배경 안쪽 콘텐츠 배치
    { // 콘텐츠 여백 적용 시작
        PreparationUi.Stretch(content, backdrop.anchorMin, backdrop.anchorMax); // 배경과 같은 배치 기준
        content.offsetMin = padding; // 왼쪽과 아래쪽 안쪽 여백
        content.offsetMax = -padding; // 오른쪽과 위쪽 안쪽 여백
    } // 콘텐츠 여백 적용 종료
    private static IEnumerator ShowArtwork(CodexEntry entry, Image artwork) // 상세 화면 한 곳에 요청 결과 표시
    { // 상세 원화 조회 시작
        ResourceRequest request = entry.RequestFullArtwork(); // 선택한 항목의 단일 원화 요청
        if (request != null) // 별도의 원화가 있는 항목 확인
        { // 원화 비동기 대기 시작
            yield return request; // 표시 스레드를 막지 않고 로드 완료 대기
        } // 원화 비동기 대기 종료
        if (artwork == null || !artwork.gameObject.activeInHierarchy) // 완료 전에 닫거나 바꾼 상세 확인
        { // 닫힌 화면 처리 시작
            yield break; // 이미 닫힌 화면에 늦은 결과 미적용
        } // 닫힌 화면 처리 종료
        artwork.sprite = (request?.asset as CodexArtworkResource)?.Artwork ?? entry.LoadStatusArtwork(); // 등록 원화나 동일 상태의 실제 아이콘 표시
        artwork.enabled = artwork.sprite != null; // 실제 이미지가 있는 경우 표시
    } // 상세 원화 조회 종료
    private static void CloseDetail(RectTransform detail) // 상세 정보 화면 닫기
    { // 상세 정보 닫기 시작
        detail.gameObject.SetActive(false); // 상세 화면 입력 차단
        detail.SetParent(null, false); // 같은 프레임에 다음 항목을 열 수 있도록 비활성 상세 분리
        Destroy(detail.gameObject); // 상세 화면 제거
    } // 상세 정보 닫기 종료
    private void Back() // 도감 이전 화면 복귀
    { // 이전 화면 복귀 시작
        if (list.gameObject.activeSelf) // 항목 목록 표시 여부 확인
        { // 분류 화면 복귀 시작
            categoryMenu.gameObject.SetActive(false); // 목록을 닫을 때 펼친 분류 메뉴 정리
            list.gameObject.SetActive(false); // 항목 목록 숨김
            foreach (Cell cell in pool) // 숨긴 분류의 재사용 셀 순회
            { // 이미지 참조 정리 시작
                cell.Bind(-1, null, false); // 이전 항목과 작은 그림 참조 해제
            } // 이미지 참조 정리 종료
            entries = null; // 이전 분류의 항목 목록 참조 해제
            categories.gameObject.SetActive(true); // 분류 선택 화면 표시
            title.text = "도감"; // 기본 도감 제목 복원
            return; // 도감 화면 유지
        } // 분류 화면 복귀 종료
        gameObject.SetActive(false); // 도감 화면 입력 차단
        Destroy(gameObject); // 도감 화면 제거
    } // 이전 화면 복귀 종료
} // 도감 화면 종료
