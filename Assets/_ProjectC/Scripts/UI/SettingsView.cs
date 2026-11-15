using System; // 설정 화면 복귀 동작 사용
using TMPro; // 설정 화면 한글 문구 사용
using UnityEngine; // 설정 화면 객체 사용
using UnityEngine.UI; // 설정 탭과 목록 사용

public sealed class SettingsView : MonoBehaviour // 설정 탭과 항목 화면
{ // 설정 화면 시작
    private RectTransform rows; // 현재 설정 항목 목록
    private readonly Button[] tabs = new Button[5]; // 다섯 설정 탭
    private ScrollRect scroll; // 설정 항목 스크롤
    private Image selectedRow; // 현재 선택한 설정 행 배경
    private Outline selectedOutline; // 현재 선택한 설정 행 강조선
    private TMP_Text helpTitle; // 선택 설정 이름 안내
    private TMP_Text helpDescription; // 선택 설정의 쉬운 설명
    private Sprite placeholderSprite; // 사진 누락 시 사용할 임시 이미지
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
        TMP_Text notice = PreparationUi.CreateText("Notice", transform, "변경 가능한 항목은 즉시 적용됩니다. 비활성 항목은 준비 중입니다.", 20f); // 설정 적용 상태 안내 생성
        PreparationUi.Place(notice.rectTransform, new Vector2(0.5f, 0.755f), Vector2.zero, new Vector2(1100f, 42f)); // 기본값 안내 위치 설정
        BuildHelp(); // 오른쪽 이미지와 설정 설명 영역 생성
        BuildOptions(); // 스크롤 가능한 설정 항목 영역 생성
        SelectTab(0); // 그래픽 탭 기본 선택
    } // 화면 배치 종료
    private void BuildOptions() // 설정 항목 목록 구성
    { // 설정 목록 구성 시작
        RectTransform panel = PreparationUi.CreatePanel("Options", transform, new Vector2(0.04f, 0.07f), new Vector2(0.625f, 0.73f)); // 왼쪽 설정 항목 패널 생성
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
        scroll.scrollSensitivity = GameplaySpeedSettings.ScrollSensitivity(36f); // 선택한 스크롤 속도 적용
    } // 설정 목록 구성 종료
    private void BuildHelp() // 선택 설정의 이미지와 설명 영역 구성
    { // 설정 설명 영역 구성 시작
        RectTransform panel = PreparationUi.CreatePanel("HelpPanel", transform, new Vector2(0.645f, 0.07f), new Vector2(0.96f, 0.73f)); // 오른쪽 설정 설명 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 공용 패널 디자인 적용
        RectTransform illustration = PreparationUi.CreatePanel("Illustration", panel, new Vector2(0.09f, 0.52f), new Vector2(0.91f, 0.93f)); // 임시 이미지 표시 영역 생성
        Image image = illustration.GetComponent<Image>(); // 설정 이미지 조회
        Texture2D texture = Resources.Load<Texture2D>("UI/Settings/ImageUnavailable"); // 사진 누락 시 사용할 임시 이미지 조회
        if (texture != null) // 임시 이미지 원본 존재 확인
        { // 임시 이미지 연결 시작
            placeholderSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 임시 이미지 스프라이트 생성
            image.sprite = placeholderSprite; // 사진이 없는 설정에 임시 이미지 적용
            image.color = Color.white; // 임시 이미지 원본 색상 적용
            image.preserveAspect = true; // 임시 이미지 원본 비율 유지
        } // 임시 이미지 연결 종료
        else // 임시 이미지 원본 누락 확인
        { // 이미지 누락 안내 시작
            image.color = new Color(0.08f, 0.12f, 0.13f, 1f); // 이미지 누락 배경 설정
            TMP_Text missing = PreparationUi.CreateText("Missing", illustration, "이미지를 표시할 수 없습니다.", 20f); // 이미지 누락 문구 생성
            PreparationUi.Stretch(missing.rectTransform, Vector2.zero, Vector2.one); // 이미지 누락 문구 영역 설정
        } // 이미지 누락 안내 종료
        helpTitle = PreparationUi.CreateText("Title", panel, string.Empty, 29f); // 선택한 설정 이름 생성
        PreparationUi.Stretch(helpTitle.rectTransform, new Vector2(0.08f, 0.395f), new Vector2(0.92f, 0.50f)); // 선택 설정 이름 영역 설정
        helpDescription = PreparationUi.CreateText("Description", panel, string.Empty, 23f); // 선택한 설정 설명 생성
        PreparationUi.Stretch(helpDescription.rectTransform, new Vector2(0.09f, 0.08f), new Vector2(0.91f, 0.39f)); // 설정 설명 영역 설정
        helpDescription.alignment = TextAlignmentOptions.TopLeft; // 설명 문구 왼쪽 위 정렬
    } // 설정 설명 영역 구성 종료
    private void SelectTab(int category) // 선택 설정 분류 표시
    { // 설정 분류 선택 시작
        for (int index = 0; index < tabs.Length; index++) // 설정 탭 순회
        { // 선택 탭 강조 시작
            TMP_Text label = tabs[index].GetComponentInChildren<TMP_Text>(); // 탭 이름 문구 조회
            label.color = index == category ? new Color(1f, 0.78f, 0.38f, 1f) : new Color(0.95f, 0.92f, 0.83f, 1f); // 선택 탭 색상 구분
        } // 선택 탭 강조 종료
        selectedRow = null; // 이전 분류 선택 배경 해제
        selectedOutline = null; // 이전 분류 선택 강조선 해제
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
            CreateRow(items[index], category, index); // 설정 이름과 현재 값 표시
        } // 설정 항목 생성 종료
        if (rows.childCount > 0) // 표시할 설정 항목 확인
        { // 첫 설정 안내 시작
            SelectSetting(category, 0, rows.GetChild(0).GetComponent<RectTransform>()); // 첫 설정의 설명과 선택 강조 표시
        } // 첫 설정 안내 종료
        scroll.verticalNormalizedPosition = 1f; // 설정 목록 시작 위치 복원
    } // 설정 분류 선택 종료
    private void CreateRow(SettingItem item, int category, int index) // 개별 설정 항목 생성
    { // 설정 항목 생성 시작
        RectTransform row = PreparationUi.CreatePanel("Row_" + index, rows, Vector2.zero, Vector2.one); // 설정 항목 배경 생성
        Image rowImage = row.GetComponent<Image>(); // 설정 항목 배경 조회
        rowImage.color = new Color(0.055f, 0.1f, 0.11f, 0.9f); // 설정 항목 배경 색상 설정
        Outline outline = row.gameObject.AddComponent<Outline>(); // 설정 선택 강조선 생성
        outline.effectColor = new Color(1f, 0.76f, 0.35f, 1f); // 밝은 금색 선택 강조 설정
        outline.effectDistance = new Vector2(3f, -3f); // 선택 강조선 두께 설정
        outline.enabled = false; // 선택 전 강조선 숨김
        Button rowButton = row.gameObject.AddComponent<Button>(); // 설정 행 선택 버튼 생성
        rowButton.targetGraphic = rowImage; // 선택 행 배경 연결
        rowButton.transition = Selectable.Transition.None; // 직접 설정한 선택 배경 유지
        rowButton.onClick.AddListener(() => SelectSetting(category, index, row)); // 설정 선택 시 설명과 강조 갱신
        LayoutElement size = row.gameObject.AddComponent<LayoutElement>(); // 항목 배치 크기 추가
        size.preferredHeight = 90f; // 항목 높이 설정
        TMP_Text name = PreparationUi.CreateText("Name", row, item.Name, 26f); // 설정 항목 이름 생성
        PreparationUi.Stretch(name.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.48f, 0.92f)); // 항목 이름 영역 설정
        name.alignment = TextAlignmentOptions.MidlineLeft; // 항목 이름 왼쪽 정렬
        GraphicsAudioSettings settings = GraphicsAudioSettingsRuntime.Current; // 저장된 화면과 음량 설정 조회
        bool supported = settings.Supports(category, index); // 실제 설정 변경 지원 여부 확인
        TMP_Text value = PreparationUi.CreateText("Value", row, supported ? settings.GetLabel(category, index) : item.DefaultValue, 25f); // 현재 값 또는 기획 기본값 생성
        PreparationUi.Place(value.rectTransform, new Vector2(0.74f, 0.5f), Vector2.zero, new Vector2(210f, 65f)); // 설정 값 위치와 폭 설정
        Action previous = supported ? () => ChangeValue(settings, category, index, -1, value, row) : null; // 지원 항목의 이전 값 동작 생성
        Action next = supported ? () => ChangeValue(settings, category, index, 1, value, row) : null; // 지원 항목의 다음 값 동작 생성
        Button previousButton = PreparationUi.CreateButton("Previous", row, "◀", new Vector2(0.59f, 0.5f), Vector2.zero, new Vector2(65f, 55f), previous); // 이전 설정 값 버튼 생성
        Button nextButton = PreparationUi.CreateButton("Next", row, "▶", new Vector2(0.89f, 0.5f), Vector2.zero, new Vector2(65f, 55f), next); // 다음 설정 값 버튼 생성
        if (!supported) // 미연결 설정 확인
        { // 변경 불가 상태 표시 시작
            previousButton.GetComponentInChildren<TMP_Text>().color = new Color(0.45f, 0.45f, 0.45f, 0.7f); // 이전 버튼 비활성 표시
            nextButton.GetComponentInChildren<TMP_Text>().color = new Color(0.45f, 0.45f, 0.45f, 0.7f); // 다음 버튼 비활성 표시
        } // 변경 불가 상태 표시 종료
    } // 설정 항목 생성 종료
    private void SelectSetting(int category, int index, RectTransform row) // 설정 선택과 설명 표시
    { // 설정 선택 시작
        if (selectedRow != null) // 이전 선택 설정 존재 확인
        { // 이전 선택 해제 시작
            selectedRow.color = new Color(0.055f, 0.1f, 0.11f, 0.9f); // 이전 설정 배경 복원
        } // 이전 선택 해제 종료
        if (selectedOutline != null) // 이전 선택 강조선 존재 확인
        { // 이전 강조선 해제 시작
            selectedOutline.enabled = false; // 이전 설정 강조 숨김
        } // 이전 강조선 해제 종료
        selectedRow = row.GetComponent<Image>(); // 새 선택 설정 배경 보관
        selectedOutline = row.GetComponent<Outline>(); // 새 선택 설정 강조선 보관
        selectedRow.color = new Color(0.14f, 0.25f, 0.25f, 1f); // 선택 설정 밝은 배경 적용
        selectedOutline.enabled = true; // 선택 설정 금색 강조 표시
        helpTitle.text = SettingsCatalog.Items[category][index].Name; // 선택 설정 이름 표시
        helpDescription.text = SettingsHelpCatalog.Get(category, index); // 선택 설정의 쉬운 설명 표시
    } // 설정 선택 종료
    private void ChangeValue(GraphicsAudioSettings settings, int category, int index, int direction, TMP_Text value, RectTransform row) // 설정 변경과 표시 갱신
    { // 설정 값 변경 시작
        SelectSetting(category, index, row); // 변경하는 설정의 설명과 강조 표시
        if (settings.Change(category, index, direction)) // 설정 값 변경 성공 확인
        { // 변경 결과 표시 시작
            value.text = settings.GetLabel(category, index); // 실제 적용된 설정 값 표시
            scroll.scrollSensitivity = GameplaySpeedSettings.ScrollSensitivity(36f); // 변경한 스크롤 속도 즉시 반영
        } // 변경 결과 표시 종료
    } // 설정 값 변경 종료
    private void OnDestroy() // 설정 설명 이미지 정리
    { // 설정 설명 이미지 정리 시작
        if (placeholderSprite != null) // 직접 생성한 임시 이미지 존재 확인
        { // 임시 이미지 정리 시작
            Destroy(placeholderSprite); // 임시 스프라이트 메모리 해제
        } // 임시 이미지 정리 종료
    } // 설정 설명 이미지 정리 종료
} // 설정 화면 종료
