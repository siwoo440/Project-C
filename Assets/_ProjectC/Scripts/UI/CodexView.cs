using System; // 항목 이름 비교 사용
using System.Collections.Generic; // 도감 항목 목록 사용
using TMPro; // 도감 문구 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.UI; // 도감 버튼과 목록 사용

public sealed class CodexView : MonoBehaviour // 로비 도감 화면
{ // 도감 화면 시작
    private static readonly string[] CategoryNames = { "상호작용", "포션", "적", "유물", "상태 이상", "찢겨진 기록", "상호작용 오브젝트" }; // 도감 분류 이름
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
        ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>(); // 도감 항목 스크롤 추가
        RectTransform viewport = PreparationUi.CreatePanel("Viewport", list, new Vector2(0.025f, 0.025f), new Vector2(0.85f, 0.975f)); // 목록 표시 영역 생성
        viewport.GetComponent<Image>().color = new Color(0.02f, 0.045f, 0.05f, 0.88f); // 목록 안쪽 배경 설정
        viewport.gameObject.AddComponent<RectMask2D>(); // 목록 영역 밖 항목 가리기
        GameObject gridObject = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup)); // 항목 격자 객체 생성
        gridObject.transform.SetParent(viewport, false); // 표시 영역에 격자 연결
        grid = gridObject.GetComponent<RectTransform>(); // 항목 격자 영역 조회
        grid.anchorMin = new Vector2(0f, 1f); // 격자 시작 앵커 설정
        grid.anchorMax = Vector2.one; // 격자 끝 앵커 설정
        grid.pivot = new Vector2(0.5f, 1f); // 격자 위쪽 기준 설정
        grid.anchoredPosition = Vector2.zero; // 격자 시작 위치 설정
        GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>(); // 항목 격자 배치 조회
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 수 설정
        layout.constraintCount = 6; // 도감 항목 여섯 열 배치
        layout.cellSize = new Vector2(195f, 235f); // 도감 카드 크기 설정
        layout.spacing = new Vector2(18f, 18f); // 도감 카드 간격 설정
        layout.padding = new RectOffset(20, 20, 20, 20); // 도감 목록 여백 설정
        layout.childAlignment = TextAnchor.UpperLeft; // 항목 격자 왼쪽 정렬
        scroll.viewport = viewport; // 스크롤 표시 영역 연결
        scroll.content = grid; // 스크롤 내용 영역 연결
        scroll.horizontal = false; // 가로 스크롤 차단
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
        for (int index = grid.childCount - 1; index >= 0; index--) // 이전 항목 카드 순회
        { // 이전 카드 정리 시작
            GameObject old = grid.GetChild(index).gameObject; // 이전 항목 카드 조회
            old.SetActive(false); // 이전 카드 즉시 숨김
            old.transform.SetParent(null, false); // 격자에서 이전 카드 분리
            Destroy(old); // 이전 카드 제거
        } // 이전 카드 정리 종료
        entries.Sort((left, right) => reverse ? string.Compare(right.Name, left.Name, StringComparison.Ordinal) : string.Compare(left.Name, right.Name, StringComparison.Ordinal)); // 실제 이름 기준 정렬
        sortLabel.text = reverse ? "이름 역순" : "이름순"; // 현재 이름 정렬 방향 표시
        empty.gameObject.SetActive(entries.Count == 0); // 항목이 없을 때 안내 표시
        int rows = (entries.Count + 5) / 6; // 여섯 열 기준 행 수 계산
        grid.sizeDelta = new Vector2(0f, Mathf.Max(40f, 40f + rows * 235f + Mathf.Max(0, rows - 1) * 18f)); // 전체 항목 격자 높이 설정
        grid.anchoredPosition = Vector2.zero; // 목록 상단으로 이동
        for (int index = 0; index < entries.Count; index++) // 현재 분류의 항목 순회
        { // 도감 카드 생성 시작
            CreateEntry(entries[index], index); // 실제 항목 카드 생성
        } // 도감 카드 생성 종료
    } // 항목 카드 갱신 종료
    private void CreateEntry(CodexEntry entry, int index) // 실제 도감 항목 카드 생성
    { // 도감 카드 생성 시작
        RectTransform card = PreparationUi.CreatePanel("Entry_" + index, grid, Vector2.zero, Vector2.one); // 항목 카드 배경 생성
        Image background = card.GetComponent<Image>(); // 카드 배경 이미지 조회
        LobbyUiArt.Apply(background, "FormationCard"); // 기존 카드 디자인 적용
        Button button = card.gameObject.AddComponent<Button>(); // 항목 상세 버튼 추가
        button.targetGraphic = background; // 카드 버튼 이미지 연결
        button.onClick.AddListener(() => ShowDetail(entry)); // 실제 항목 상세 연결
        if (entry.Image != null) // 실제 이미지 존재 여부 확인
        { // 항목 이미지 표시 시작
            RectTransform icon = PreparationUi.CreatePanel("Image", card, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.92f)); // 항목 그림 영역 생성
            Image image = icon.GetComponent<Image>(); // 항목 이미지 조회
            image.sprite = entry.Image; // 실제 항목 이미지 적용
            image.color = Color.white; // 원본 이미지 색상 적용
            image.preserveAspect = true; // 원본 이미지 비율 유지
            image.raycastTarget = false; // 항목 버튼 입력 허용
        } // 항목 이미지 표시 종료
        TMP_Text name = PreparationUi.CreateText("Name", card, entry.Name, 23f); // 실제 항목 이름 생성
        PreparationUi.Stretch(name.rectTransform, entry.Image != null ? new Vector2(0.06f, 0.05f) : new Vector2(0.06f, 0.3f), entry.Image != null ? new Vector2(0.94f, 0.23f) : new Vector2(0.94f, 0.7f)); // 이미지 유무별 이름 영역 설정
    } // 도감 카드 생성 종료
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
