using System; // 항목 이름 비교 사용
using System.Collections.Generic; // 도감 항목 목록 사용
using TMPro; // 도감 문구 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.UI; // 도감 버튼과 목록 사용

public sealed class CodexView : MonoBehaviour // 로비 도감 화면
{ // 도감 화면 시작
    private static readonly string[] CategoryNames = { "상호작용", "포션", "적", "유물", "상태 이상", "찢겨진 기록", "상호작용 오브젝트" }; // 도감 분류 이름
    private const int Columns = 6; // 기존 도감의 가로 항목 수
    private const float Width = 195f; // 원래 항목 너비
    private const float Height = 235f; // 원래 항목 높이
    private const float Gap = 18f; // 항목 사이 여백
    private const float Margin = 20f; // 목록 가장자리 여백
    private const float RowHeight = Height + Gap; // 한 줄 이동 거리
    private readonly List<Cell> pool = new List<Cell>(); // 현재 도감에서 재사용할 항목 셀
    private ScrollRect scroll; // 실제 위치와 속도를 가진 스크롤
    private int firstRow = -1; // 마지막으로 연결한 첫 행
    private float viewportHeight = -1f; // 화면 크기 변경 감지용 높이
    private RectTransform categories; // 분류 선택 화면
    private RectTransform list; // 항목 목록 화면
    private RectTransform grid; // 항목 카드 격자
    private TMP_Text title; // 현재 도감 제목
    private TMP_Text empty; // 빈 목록 안내
    private TMP_Text sortLabel; // 이름 정렬 상태
    private List<CodexEntry> entries; // 현재 분류 항목
    private bool reverse; // 이름 역순 여부
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
        for (int index = 0; index < CategoryNames.Length; index++) // 일곱 개의 도감 분류 순회
        { // 분류 버튼 생성 시작
            int category = index; // 버튼별 분류 번호 보관
            float x = index < 4 ? 0.185f + index * 0.21f : 0.29f + (index - 4) * 0.21f; // 두 줄 기준 가로 위치 계산
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
        Button sort = PreparationUi.CreateButton("SortButton", list, "이름순", new Vector2(0.92f, 0.89f), Vector2.zero, new Vector2(190f, 70f), ToggleSort); // 항목 이름 정렬 버튼 생성
        sortLabel = sort.GetComponentInChildren<TMP_Text>(); // 정렬 버튼 문구 조회
        empty = PreparationUi.CreateText("Empty", list, "표시할 항목이 없습니다.", 30f); // 빈 목록 안내 생성
        PreparationUi.Place(empty.rectTransform, new Vector2(0.43f, 0.5f), Vector2.zero, new Vector2(700f, 80f)); // 빈 목록 안내 위치 설정
    } // 항목 목록 구성 종료
    private void OpenCategory(int category) // 선택 분류의 목록 열기
    { // 분류 목록 열기 시작
        entries = CodexData.Load(category); // 실제 분류 데이터 조회
        reverse = false; // 기본 이름순 설정
        title.text = "도감 · " + CategoryNames[category]; // 현재 분류 제목 표시
        categories.gameObject.SetActive(false); // 분류 선택 화면 숨김
        list.gameObject.SetActive(true); // 항목 목록 화면 표시
        RebuildEntries(); // 선택 분류의 카드 생성
    } // 분류 목록 열기 종료
    private void ToggleSort() // 이름순 정렬 방향 전환
    { // 정렬 전환 시작
        reverse = !reverse; // 이름 정렬 방향 변경
        RebuildEntries(); // 변경 순서로 카드 갱신
    } // 정렬 전환 종료
    private void RebuildEntries() // 현재 분류의 항목 카드 갱신
    { // 항목 카드 갱신 시작
        entries.Sort((left, right) => reverse ? string.Compare(right.Name, left.Name, StringComparison.Ordinal) : string.Compare(left.Name, right.Name, StringComparison.Ordinal)); // 실제 이름 기준 정렬
        sortLabel.text = reverse ? "이름 역순" : "이름순"; // 현재 이름 정렬 방향 표시
        empty.gameObject.SetActive(entries.Count == 0); // 항목이 없을 때 안내 표시
        int rows = (entries.Count + Columns - 1) / Columns; // 여섯 열 기준 전체 행 수
        grid.sizeDelta = new Vector2(0f, Margin * 2f + rows * Height + Mathf.Max(0, rows - 1) * Gap); // 스크롤할 전체 높이 보존
        scroll.StopMovement(); // 정렬 전 관성이 새 첫 행을 밀지 않도록 정지
        grid.anchoredPosition = Vector2.zero; // 분류와 정렬 변경 시 맨 위로 복귀
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
        int rows = (entries.Count + Columns - 1) / Columns; // 데이터 전체의 행 수
        int visibleRows = Mathf.Min(rows, Mathf.CeilToInt(Mathf.Max(0f, height) / RowHeight) + 2); // 일부만 보이는 행과 위아래 여유분 확보
        int start = Mathf.Clamp(Mathf.FloorToInt((grid.anchoredPosition.y - Margin) / RowHeight) - 1, 0, rows - visibleRows); // 목록 범위 안에서 첫 표시 행 결정
        if (start == firstRow && Mathf.Approximately(height, viewportHeight)) // 같은 행 안의 스크롤 확인
        { // 기존 연결 유지 시작
            return; // 같은 항목과 그림의 중복 연결 생략
        } // 기존 연결 유지 종료
        firstRow = start; // 갱신한 첫 행 보관
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
                cell.Bind(index, index < entries.Count ? entries[index] : null); // 마지막 줄의 빈 칸은 숨김
            } // 보이는 행 처리 종료
            else // 이전 분류 또는 큰 창에서 남은 셀 확인
            { // 여유 셀 정리 시작
                pool[offset].Bind(-1, null); // 다음 사용을 위해 숨긴 채 유지
            } // 여유 셀 정리 종료
        } // 개별 셀 연결 종료
    } // 화면 행 연결 종료
    private sealed class Cell // 이름과 그림과 버튼을 한 번 생성하는 도감 셀
    { // 재사용 셀 시작
        private readonly RectTransform area; // 셀 위치와 크기
        private readonly Image icon; // 현재 항목의 그림
        private readonly TMP_Text label; // 현재 항목의 이름
        private CodexEntry entry; // 버튼이 열 현재 항목
        private int index = -1; // 현재 셀이 표시하는 논리적 순번
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
            var imageArea = PreparationUi.CreatePanel("Image", area, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.92f)); // 기존 원화 위치
            icon = imageArea.GetComponent<Image>(); // 재사용할 그림 표시
            icon.color = Color.white; // 원화 색상 유지
            icon.preserveAspect = true; // 원화 비율 유지
            icon.raycastTarget = false; // 상세 버튼 입력 허용
            label = PreparationUi.CreateText("Name", area, string.Empty, 23f); // 재사용할 이름 표시
        } // 셀 생성 종료
        public void Bind(int nextIndex, CodexEntry nextEntry) // 보이는 자료가 바뀐 셀만 다시 연결
        { // 셀 내용 변경 시작
            if (nextEntry == null) // 빈 분류 또는 마지막 행의 빈 칸 확인
            { // 비사용 셀 처리 시작
                if (area.gameObject.activeSelf) // 실제 표시 중인 셀 확인
                { // 비사용 화면 숨김 시작
                    entry = null; // 이전 상세 대상 해제
                    icon.sprite = null; // 이전 항목 그림 연결 해제
                    area.gameObject.SetActive(false); // 표시와 클릭 차단
                } // 비사용 화면 숨김 종료
                return; // 빈 항목 배치 생략
            } // 비사용 셀 처리 종료
            if (index == nextIndex && entry == nextEntry) // 스크롤 전부터 보이던 같은 항목 확인
            { // 같은 셀 유지 시작
                return; // 이름과 이미지 재배치 생략
            } // 같은 셀 유지 종료
            index = nextIndex; // 실제 항목 순번 저장
            entry = nextEntry; // 현재 클릭 시 열 항목 저장
            area.name = "Entry_" + index; // 기존 도감의 실제 항목 식별 이름
            area.anchoredPosition = new Vector2(Margin + index % Columns * (Width + Gap), -Margin - index / Columns * RowHeight); // 전체 목록 안의 원래 위치
            icon.sprite = entry.Image; // 새 항목의 실제 그림 연결
            bool hasImage = entry.Image != null; // 원화가 있는 항목 확인
            icon.gameObject.SetActive(hasImage); // 원화가 없는 항목의 이전 이미지 숨김
            label.text = entry.Name; // 현재 실제 항목 이름
            PreparationUi.Stretch(label.rectTransform, hasImage ? new Vector2(0.06f, 0.05f) : new Vector2(0.06f, 0.3f), hasImage ? new Vector2(0.94f, 0.23f) : new Vector2(0.94f, 0.7f)); // 이미지 유무에 맞춘 기존 이름 위치
            area.gameObject.SetActive(true); // 새 항목의 표시와 클릭 허용
        } // 셀 내용 변경 종료
    } // 재사용 셀 종료
    private void ShowDetail(CodexEntry entry) // 실제 항목 상세 정보 표시
    { // 상세 화면 표시 시작
        if (transform.Find("Detail") != null) // 기존 상세 화면 존재 확인
        { // 중복 상세 화면 처리 시작
            return; // 중복 상세 생성 중단
        } // 중복 상세 화면 처리 종료
        RectTransform detail = PreparationUi.CreatePanel("Detail", transform, Vector2.zero, Vector2.one); // 상세 화면 배경 생성
        detail.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 상세 화면 뒤 어두운 배경 설정
        RectTransform panel = PreparationUi.CreatePanel("Panel", detail, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f)); // 상세 정보 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 기존 패널 디자인 적용
        TMP_Text heading = PreparationUi.CreateText("Name", detail, entry.Name, 36f); // 항목 상세 이름 생성
        PreparationUi.Place(heading.rectTransform, new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(800f, 70f)); // 상세 이름 위치 설정
        TMP_Text description = PreparationUi.CreateText("Description", detail, string.IsNullOrWhiteSpace(entry.Description) ? "등록된 설명이 없습니다." : entry.Description, 26f); // 실제 항목 설명 생성
        PreparationUi.Place(description.rectTransform, new Vector2(0.5f, 0.49f), Vector2.zero, new Vector2(760f, 230f)); // 상세 설명 위치 설정
        PreparationUi.CreateButton("CloseButton", detail, "닫기", new Vector2(0.5f, 0.31f), Vector2.zero, new Vector2(200f, 70f), () => CloseDetail(detail)); // 상세 화면 닫기 버튼 생성
    } // 상세 화면 표시 종료
    private static void CloseDetail(RectTransform detail) // 상세 정보 화면 닫기
    { // 상세 정보 닫기 시작
        detail.gameObject.SetActive(false); // 상세 화면 입력 차단
        Destroy(detail.gameObject); // 상세 화면 제거
    } // 상세 정보 닫기 종료
    private void Back() // 도감 이전 화면 복귀
    { // 이전 화면 복귀 시작
        if (list.gameObject.activeSelf) // 항목 목록 표시 여부 확인
        { // 분류 화면 복귀 시작
            list.gameObject.SetActive(false); // 항목 목록 숨김
            categories.gameObject.SetActive(true); // 분류 선택 화면 표시
            title.text = "도감"; // 기본 도감 제목 복원
            return; // 도감 화면 유지
        } // 분류 화면 복귀 종료
        gameObject.SetActive(false); // 도감 화면 입력 차단
        Destroy(gameObject); // 도감 화면 제거
    } // 이전 화면 복귀 종료
} // 도감 화면 종료
