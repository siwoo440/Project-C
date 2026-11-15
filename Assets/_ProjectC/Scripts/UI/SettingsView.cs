using System; // 설정 화면 복귀 동작 사용
using TMPro; // 설정 화면 한글 문구 사용
using UnityEngine; // 설정 화면 객체 사용
using UnityEngine.UI; // 설정 탭과 목록 사용

public sealed class SettingsView : MonoBehaviour // 설정 탭과 항목 화면
{ // 설정 화면 시작
    private RectTransform rows; // 현재 설정 항목 목록
    private readonly Button[] tabs = new Button[5]; // 다섯 설정 탭
    private ScrollRect scroll; // 설정 항목 스크롤
    public static SettingsView Create(Canvas canvas, Action back) // 설정 화면 생성
    { // 설정 화면 생성 시작
        if (canvas == null || FindFirstObjectByType<SettingsView>() != null) // 캔버스와 중복 설정 화면 확인
        { // 설정 생성 불가 처리 시작
            return null; // 설정 화면 생성 중단
        } // 설정 생성 불가 처리 종료
        Texture2D background = Resources.Load<Texture2D>("UI/LobbyBackground"); // 기존 로비 배경 이미지 조회
        if (background != null) // 배경 원본 존재 확인
        { // 설정 배경 연결 시작
            LobbyBackgroundInstaller.Install(canvas, background); // 설정 씬 배경 이미지 적용
        } // 설정 배경 연결 종료
        RectTransform root = PreparationUi.CreateOverlay(canvas, "SettingsView"); // 전체 설정 화면 생성
        root.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.055f, 0.83f); // 배경이 보이는 어두운 설정 화면 적용
        SettingsView view = root.gameObject.AddComponent<SettingsView>(); // 설정 화면 동작 연결
        view.Build(back); // 설정 탭과 목록 구성
        return view; // 생성된 설정 화면 반환
    } // 설정 화면 생성 종료
    private void Build(Action back) // 설정 화면 배치
    { // 화면 배치 시작
        TMP_Text title = PreparationUi.CreateText("Title", transform, "설정", 42f); // 설정 화면 제목 생성
        PreparationUi.Place(title.rectTransform, new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(600f, 70f)); // 설정 제목 위치 설정
        PreparationUi.CreateButton("BackButton", transform, "뒤로", new Vector2(0.085f, 0.94f), Vector2.zero, new Vector2(200f, 70f), back); // 이전 화면 복귀 버튼 생성
        GameObject tabObject = new GameObject("Tabs", typeof(RectTransform)); // 설정 탭 전체 영역 생성
        tabObject.transform.SetParent(transform, false); // 설정 화면에 탭 연결
        RectTransform tabArea = tabObject.GetComponent<RectTransform>(); // 설정 탭 영역 조회
        PreparationUi.Stretch(tabArea, Vector2.zero, Vector2.one); // 설정 탭 화면 영역 설정
        for (int index = 0; index < SettingsCatalog.Tabs.Length; index++) // 다섯 설정 분류 순회
        { // 설정 탭 생성 시작
            int category = index; // 탭별 분류 번호 보관
            tabs[index] = PreparationUi.CreateButton("Tab_" + index, tabArea, SettingsCatalog.Tabs[index], new Vector2(0.17f + index * 0.165f, 0.83f), Vector2.zero, new Vector2(270f, 76f), () => SelectTab(category)); // 설정 분류 버튼 생성
        } // 설정 탭 생성 종료
        TMP_Text notice = PreparationUi.CreateText("Notice", transform, "표시된 값은 기획 기본값이며 아직 변경할 수 없습니다.", 20f); // 기본값 안내 문구 생성
        PreparationUi.Place(notice.rectTransform, new Vector2(0.5f, 0.755f), Vector2.zero, new Vector2(1100f, 42f)); // 기본값 안내 위치 설정
        BuildOptions(); // 스크롤 가능한 설정 항목 영역 생성
        SelectTab(0); // 그래픽 탭 기본 선택
    } // 화면 배치 종료
    private void BuildOptions() // 설정 항목 목록 구성
    { // 설정 목록 구성 시작
        RectTransform panel = PreparationUi.CreatePanel("Options", transform, new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.73f)); // 설정 항목 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 기존 설정 패널 디자인 적용
        scroll = panel.gameObject.AddComponent<ScrollRect>(); // 설정 항목 스크롤 추가
        RectTransform viewport = PreparationUi.CreatePanel("Viewport", panel, new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f)); // 설정 목록 표시 영역 생성
        viewport.GetComponent<Image>().color = new Color(0.025f, 0.05f, 0.06f, 0.92f); // 설정 목록 배경 설정
        viewport.gameObject.AddComponent<RectMask2D>(); // 목록 영역 밖 설정 가리기
        GameObject rowObject = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup)); // 설정 항목 목록 객체 생성
        rowObject.transform.SetParent(viewport, false); // 항목 목록 부모 연결
        rows = rowObject.GetComponent<RectTransform>(); // 설정 항목 영역 조회
        rows.anchorMin = new Vector2(0f, 1f); // 항목 시작 앵커 설정
        rows.anchorMax = Vector2.one; // 항목 끝 앵커 설정
        rows.pivot = new Vector2(0.5f, 1f); // 목록 상단 기준 설정
        rows.anchoredPosition = Vector2.zero; // 목록 시작 위치 설정
        VerticalLayoutGroup layout = rowObject.GetComponent<VerticalLayoutGroup>(); // 항목 세로 배치 조회
        layout.padding = new RectOffset(28, 28, 16, 16); // 설정 목록 여백 설정
        layout.spacing = 8f; // 설정 항목 간격 설정
        layout.childControlWidth = true; // 항목 너비 자동 조정
        layout.childControlHeight = true; // 항목 높이 자동 조정
        layout.childForceExpandWidth = true; // 항목 너비 채우기
        layout.childForceExpandHeight = false; // 항목 높이 임의 확대 방지
        scroll.viewport = viewport; // 목록 표시 영역 연결
        scroll.content = rows; // 스크롤 내용 영역 연결
        scroll.horizontal = false; // 가로 스크롤 차단
        scroll.vertical = true; // 세로 스크롤 허용
        scroll.movementType = ScrollRect.MovementType.Clamped; // 목록 범위 밖 이동 차단
        scroll.scrollSensitivity = 36f; // 마우스 휠 이동 폭 설정
    } // 설정 목록 구성 종료
    private void SelectTab(int category) // 선택 설정 분류 표시
    { // 설정 분류 선택 시작
        for (int index = 0; index < tabs.Length; index++) // 설정 탭 순회
        { // 선택 탭 강조 시작
            TMP_Text label = tabs[index].GetComponentInChildren<TMP_Text>(); // 탭 이름 문구 조회
            label.color = index == category ? new Color(1f, 0.78f, 0.38f, 1f) : new Color(0.95f, 0.92f, 0.83f, 1f); // 선택 탭 색상 구분
        } // 선택 탭 강조 종료
        for (int index = rows.childCount - 1; index >= 0; index--) // 기존 설정 항목 순회
        { // 이전 설정 항목 정리 시작
            GameObject old = rows.GetChild(index).gameObject; // 기존 설정 항목 조회
            old.SetActive(false); // 기존 항목 즉시 숨김
            old.transform.SetParent(null, false); // 목록에서 기존 항목 분리
            Destroy(old); // 기존 항목 제거
        } // 이전 설정 항목 정리 종료
        SettingItem[] items = SettingsCatalog.Items[category]; // 선택 분류의 기획 설정 항목 조회
        rows.sizeDelta = new Vector2(0f, 32f + items.Length * 90f + Mathf.Max(0, items.Length - 1) * 8f); // 전체 설정 목록 높이 설정
        rows.anchoredPosition = Vector2.zero; // 설정 목록 맨 위로 이동
        for (int index = 0; index < items.Length; index++) // 선택 분류 설정 항목 순회
        { // 설정 항목 생성 시작
            CreateRow(items[index], index); // 설정 이름과 기획 기본값 표시
        } // 설정 항목 생성 종료
        scroll.verticalNormalizedPosition = 1f; // 설정 목록 시작 위치 복원
    } // 설정 분류 선택 종료
    private void CreateRow(SettingItem item, int index) // 개별 설정 항목 생성
    { // 설정 항목 생성 시작
        RectTransform row = PreparationUi.CreatePanel("Row_" + index, rows, Vector2.zero, Vector2.one); // 설정 항목 배경 생성
        row.GetComponent<Image>().color = new Color(0.055f, 0.1f, 0.11f, 0.9f); // 설정 항목 배경 색상 설정
        LayoutElement size = row.gameObject.AddComponent<LayoutElement>(); // 항목 배치 크기 추가
        size.preferredHeight = 90f; // 항목 높이 설정
        TMP_Text name = PreparationUi.CreateText("Name", row, item.Name, 26f); // 설정 항목 이름 생성
        PreparationUi.Stretch(name.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.48f, 0.92f)); // 항목 이름 영역 설정
        name.alignment = TextAlignmentOptions.MidlineLeft; // 항목 이름 왼쪽 정렬
        PreparationUi.CreateButton("Previous", row, "◀", new Vector2(0.59f, 0.5f), Vector2.zero, new Vector2(65f, 55f), null); // 연결 전 이전 값 버튼 표시
        TMP_Text value = PreparationUi.CreateText("Value", row, item.DefaultValue, 25f); // 기획서 기본값 생성
        PreparationUi.Place(value.rectTransform, new Vector2(0.74f, 0.5f), Vector2.zero, new Vector2(320f, 65f)); // 설정 기본값 위치 설정
        PreparationUi.CreateButton("Next", row, "▶", new Vector2(0.89f, 0.5f), Vector2.zero, new Vector2(65f, 55f), null); // 연결 전 다음 값 버튼 표시
    } // 설정 항목 생성 종료
} // 설정 화면 종료
