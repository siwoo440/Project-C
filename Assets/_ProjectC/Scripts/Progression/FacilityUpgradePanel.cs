using System; // 닫기 동작과 설비 열거형 사용
using System.Collections.Generic; // 설비 버튼 목록 사용
using TMPro; // 한글 설비 정보 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 설비 버튼과 스크롤 사용
public sealed class FacilityUpgradePanel : MonoBehaviour // 확대된 설비 강화 화면
{ // 설비 화면 시작
    private static Sprite panelSprite; // 공용 설비 패널 이미지
    private readonly Dictionary<FacilityType, Button> facilityButtons = new Dictionary<FacilityType, Button>(); // 설비 종류별 선택 버튼
    private FacilityUpgradeManager facilities; // 설비 강화 관리자
    private PlayerResourceManager resources; // 현재 자원 관리자
    private FacilityType selectedType = FacilityType.PowerSupply; // 현재 선택 설비
    private readonly TMP_Text[] resourceValues = new TMP_Text[4]; // 자원별 보유 수량 문구
    private TMP_Text facilityNameText; // 선택 설비 이름 문구
    private TMP_Text levelText; // 현재 설비 단계 문구
    private TMP_Text currentEffectText; // 현재 효과 문구
    private TMP_Text nextEffectText; // 다음 효과 문구
    private TMP_Text costText; // 다음 강화 비용 문구
    private Button upgradeButton; // 설비 강화 버튼
    private Action closed; // 화면 닫힘 알림
    public static FacilityUpgradePanel Create(Canvas canvas, Action onClosed) // 설비 강화 화면 생성
    { // 설비 화면 생성 시작
        if (canvas == null) // 로비 캔버스 확인
        { // 생성 불가 처리 시작
            return null; // 화면 생성 실패 반환
        } // 생성 불가 처리 종료
        GameObject root = new GameObject("FacilityUpgradeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FacilityUpgradePanel)); // 전체 설비 화면 생성
        root.transform.SetParent(canvas.transform, false); // 로비 캔버스 자식 배치
        root.transform.SetAsLastSibling(); // 로비 화면 위에 배치
        PreparationUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one); // 전체 화면 입력 영역 설정
        Image dim = root.GetComponent<Image>(); // 전체 배경 이미지 조회
        dim.color = new Color(0.01f, 0.025f, 0.03f, 0.84f); // 로비를 어둡게 표시
        dim.raycastTarget = true; // 뒤쪽 로비 입력 차단
        FacilityUpgradePanel view = root.GetComponent<FacilityUpgradePanel>(); // 설비 화면 컴포넌트 조회
        view.closed = onClosed; // 닫힘 알림 저장
        view.Build(); // 설비 화면 구성
        return view; // 생성된 설비 화면 반환
    } // 설비 화면 생성 종료
    private void Build() // 설비 화면 요소 생성
    { // 화면 요소 생성 시작
        facilities = FacilityUpgradeManager.EnsureInstance(); // 설비 관리자 준비
        resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
        GameObject panelObject = new GameObject("FacilityPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 확대 설비 패널 생성
        panelObject.transform.SetParent(transform, false); // 전체 화면 자식 배치
        RectTransform panel = panelObject.GetComponent<RectTransform>(); // 설비 패널 사각형 조회
        PreparationUi.Stretch(panel, new Vector2(0.035f, 0.045f), new Vector2(0.965f, 0.955f)); // 화면 대부분을 차지하는 패널 배치
        Image panelImage = panelObject.GetComponent<Image>(); // 패널 이미지 조회
        panelImage.sprite = GetPanelSprite(); // 제작한 설비 패널 이미지 적용
        panelImage.color = panelImage.sprite == null ? new Color(0.04f, 0.1f, 0.12f, 1f) : Color.white; // 이미지 누락 시 기본 배경 적용
        panelImage.raycastTarget = true; // 패널 뒤쪽 입력 차단
        TMP_Text title = CreateText("Title", panel, "설비 강화", 40f, new Vector2(0.055f, 0.855f), new Vector2(0.32f, 0.92f)); // 화면 제목 생성
        title.alignment = TextAlignmentOptions.MidlineLeft; // 제목 왼쪽 정렬
        BuildResourceRow(panel); // 보유 자원 아이콘과 수량 생성
        Button close = CreateButton("CloseButton", panel, "닫기", new Vector2(0.855f, 0.855f), new Vector2(0.945f, 0.92f), Close); // 화면 닫기 버튼 생성
        close.GetComponentInChildren<TMP_Text>().fontSize = 24f; // 닫기 문구 크기 적용
        TMP_Text listTitle = CreateText("ListTitle", panel, "설비 목록", 26f, new Vector2(0.055f, 0.81f), new Vector2(0.34f, 0.855f)); // 설비 목록 제목 생성
        listTitle.alignment = TextAlignmentOptions.MidlineLeft; // 설비 목록 제목 왼쪽 정렬
        BuildFacilityList(panel); // 선택 가능한 설비 목록 생성
        BuildFacilityDetail(panel); // 설비 상세와 강화 영역 생성
        facilities.FacilityChanged += HandleFacilityChanged; // 설비 강화 변경 이벤트 연결
        resources.ResourcesChanged += Refresh; // 보유 자원 변경 이벤트 연결
        Refresh(); // 현재 설비와 자원 표시
    } // 화면 요소 생성 종료
    private void BuildResourceRow(Transform parent) // 보유 자원 아이콘과 수량 생성
    { // 보유 자원 생성 시작
        GameObject groupObject = new GameObject("Resources", typeof(RectTransform)); // 보유 자원 묶음 생성
        groupObject.transform.SetParent(parent, false); // 설비 패널 자식 배치
        RectTransform group = groupObject.GetComponent<RectTransform>(); // 자원 묶음 위치 조회
        PreparationUi.Stretch(group, new Vector2(0.34f, 0.855f), new Vector2(0.83f, 0.92f)); // 상단 자원 표시 영역 배치
        string[] iconNames = { "Gold", "Screw", "Iron", "Wire" }; // 기존 자원 아이콘 이름 목록
        for (int index = 0; index < iconNames.Length; index++) // 네 자원 종류 순회
        { // 개별 자원 표시 생성 시작
            GameObject itemObject = new GameObject("Resource_" + iconNames[index], typeof(RectTransform)); // 개별 자원 영역 생성
            itemObject.transform.SetParent(group, false); // 자원 묶음 자식 배치
            RectTransform item = itemObject.GetComponent<RectTransform>(); // 개별 자원 위치 조회
            PreparationUi.Stretch(item, new Vector2(index / 4f, 0f), new Vector2((index + 1) / 4f, 1f)); // 자원별 독립 영역 배치
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 자원 아이콘 객체 생성
            iconObject.transform.SetParent(item, false); // 개별 자원 자식 배치
            Image icon = iconObject.GetComponent<Image>(); // 자원 아이콘 이미지 조회
            PreparationUi.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(40f, 40f)); // 정사각형 아이콘 위치 설정
            LobbyUiArt.Apply(icon, iconNames[index]); // 기존 자원 아이콘 이미지 적용
            icon.preserveAspect = true; // 아이콘 원본 비율 유지
            icon.raycastTarget = false; // 아이콘 입력 차단 해제
            TMP_Text value = CreateText("Value", item, string.Empty, 22f, new Vector2(0f, 0f), Vector2.one); // 자원 수량 문구 생성
            value.rectTransform.offsetMin = new Vector2(44f, 0f); // 아이콘 옆 수량 간격 설정
            value.alignment = TextAlignmentOptions.MidlineLeft; // 수량 왼쪽 정렬
            resourceValues[index] = value; // 자원별 수량 문구 보관
        } // 개별 자원 표시 생성 종료
    } // 보유 자원 생성 종료
    private void BuildFacilityList(Transform parent) // 설비 목록과 스크롤 생성
    { // 설비 목록 생성 시작
        RectTransform list = PreparationUi.CreatePanel("FacilityList", parent, new Vector2(0.05f, 0.105f), new Vector2(0.36f, 0.81f)); // 설비 목록 영역 생성
        list.GetComponent<Image>().color = new Color(0.02f, 0.055f, 0.065f, 0.8f); // 목록 배경 색상 적용
        ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>(); // 목록 스크롤 기능 추가
        scroll.horizontal = false; // 가로 스크롤 차단
        scroll.vertical = true; // 세로 스크롤 허용
        scroll.movementType = ScrollRect.MovementType.Clamped; // 목록 경계 밖 이동 방지
        scroll.scrollSensitivity = 45f; // 마우스 휠 이동 속도 설정
        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); // 목록 표시 영역 생성
        viewportObject.transform.SetParent(list, false); // 목록 영역 자식 배치
        RectTransform viewport = viewportObject.GetComponent<RectTransform>(); // 표시 영역 사각형 조회
        PreparationUi.Stretch(viewport, Vector2.zero, Vector2.one); // 목록 전체 영역 채우기
        viewport.offsetMin = new Vector2(10f, 10f); // 목록 왼쪽 아래 여백 설정
        viewport.offsetMax = new Vector2(-10f, -10f); // 목록 오른쪽 위 여백 설정
        GameObject contentObject = new GameObject("Content", typeof(RectTransform)); // 설비 버튼 목록 생성
        contentObject.transform.SetParent(viewport, false); // 표시 영역 자식 배치
        RectTransform content = contentObject.GetComponent<RectTransform>(); // 목록 내용 사각형 조회
        content.anchorMin = new Vector2(0f, 1f); // 목록 왼쪽 위 앵커 설정
        content.anchorMax = new Vector2(1f, 1f); // 목록 오른쪽 위 앵커 설정
        content.pivot = new Vector2(0.5f, 1f); // 목록 위쪽 기준점 설정
        content.anchoredPosition = Vector2.zero; // 목록 시작 위치 설정
        Array types = Enum.GetValues(typeof(FacilityType)); // 전체 설비 종류 조회
        const float rowHeight = 62f; // 설비 버튼 높이
        const float rowGap = 6f; // 설비 버튼 간격
        content.sizeDelta = new Vector2(0f, types.Length * (rowHeight + rowGap)); // 전체 설비 목록 높이 설정
        scroll.viewport = viewport; // 스크롤 표시 영역 연결
        scroll.content = content; // 스크롤 내용 연결
        int index = 0; // 설비 버튼 순번 초기화
        foreach (FacilityType type in types) // 전체 설비 종류 순회
        { // 개별 설비 버튼 생성 시작
            FacilityType selected = type; // 버튼별 설비 종류 저장
            Button button = PreparationUi.CreateButton("Facility_" + type, content, string.Empty, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, () => SelectFacility(selected)); // 설비 선택 버튼 생성
            RectTransform buttonRect = button.GetComponent<RectTransform>(); // 설비 버튼 위치 조회
            buttonRect.anchorMin = new Vector2(0f, 1f); // 버튼 왼쪽 위 앵커 설정
            buttonRect.anchorMax = new Vector2(1f, 1f); // 버튼 오른쪽 위 앵커 설정
            buttonRect.pivot = new Vector2(0.5f, 1f); // 버튼 위쪽 기준점 설정
            buttonRect.anchoredPosition = new Vector2(0f, -index * (rowHeight + rowGap)); // 설비 순번별 세로 위치 설정
            buttonRect.sizeDelta = new Vector2(-4f, rowHeight); // 설비 버튼 크기 설정
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(); // 설비 버튼 문구 조회
            label.fontSize = 24f; // 설비 이름 글자 크기 설정
            label.fontSizeMax = 24f; // 설비 이름 최대 글자 크기 설정
            Outline outline = button.gameObject.AddComponent<Outline>(); // 선택 설비 강조 테두리 추가
            outline.effectColor = new Color(1f, 0.8f, 0.35f, 0.95f); // 선택 강조 색상 설정
            outline.effectDistance = new Vector2(2f, -2f); // 선택 강조 두께 설정
            outline.enabled = false; // 기본 선택 강조 해제
            facilityButtons.Add(type, button); // 설비 종류별 버튼 저장
            index++; // 다음 설비 순번 이동
        } // 개별 설비 버튼 생성 종료
    } // 설비 목록 생성 종료
    private void BuildFacilityDetail(Transform parent) // 설비 상세 정보 생성
    { // 설비 상세 생성 시작
        RectTransform detail = PreparationUi.CreatePanel("Detail", parent, new Vector2(0.39f, 0.105f), new Vector2(0.95f, 0.81f)); // 설비 상세 영역 생성
        detail.GetComponent<Image>().color = new Color(0.025f, 0.065f, 0.075f, 0.82f); // 상세 배경 색상 적용
        facilityNameText = CreateText("FacilityName", detail, string.Empty, 36f, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f)); // 선택 설비 이름 생성
        facilityNameText.alignment = TextAlignmentOptions.MidlineLeft; // 설비 이름 왼쪽 정렬
        levelText = CreateText("Level", detail, string.Empty, 26f, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.86f)); // 현재 강화 단계 생성
        levelText.alignment = TextAlignmentOptions.MidlineLeft; // 단계 문구 왼쪽 정렬
        TMP_Text currentHeader = CreateText("CurrentHeader", detail, "현재 효과", 25f, new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.77f)); // 현재 효과 제목 생성
        currentHeader.alignment = TextAlignmentOptions.MidlineLeft; // 현재 효과 제목 왼쪽 정렬
        currentEffectText = CreateText("CurrentEffect", detail, string.Empty, 25f, new Vector2(0.07f, 0.57f), new Vector2(0.93f, 0.7f)); // 현재 효과 설명 생성
        currentEffectText.alignment = TextAlignmentOptions.MidlineLeft; // 현재 효과 왼쪽 정렬
        TMP_Text nextHeader = CreateText("NextHeader", detail, "다음 효과", 25f, new Vector2(0.05f, 0.49f), new Vector2(0.95f, 0.56f)); // 다음 효과 제목 생성
        nextHeader.alignment = TextAlignmentOptions.MidlineLeft; // 다음 효과 제목 왼쪽 정렬
        nextEffectText = CreateText("NextEffect", detail, string.Empty, 25f, new Vector2(0.07f, 0.36f), new Vector2(0.93f, 0.49f)); // 다음 효과 설명 생성
        nextEffectText.alignment = TextAlignmentOptions.MidlineLeft; // 다음 효과 왼쪽 정렬
        costText = CreateText("Cost", detail, string.Empty, 22f, new Vector2(0.05f, 0.23f), new Vector2(0.95f, 0.34f)); // 강화 비용 문구 생성
        costText.alignment = TextAlignmentOptions.MidlineLeft; // 강화 비용 왼쪽 정렬
        upgradeButton = CreateButton("UpgradeButton", detail, "강화", new Vector2(0.3f, 0.055f), new Vector2(0.7f, 0.2f), UpgradeSelected); // 선택 설비 강화 버튼 생성
    } // 설비 상세 생성 종료
    private void SelectFacility(FacilityType type) // 설비 선택 처리
    { // 설비 선택 시작
        selectedType = type; // 선택 설비 저장
        Refresh(); // 선택 설비 상세 갱신
    } // 설비 선택 종료
    private void UpgradeSelected() // 선택 설비 강화 처리
    { // 설비 강화 시작
        facilities.TryUpgrade(selectedType); // 자원 확인 후 선택 설비 강화
        Refresh(); // 강화 결과와 자원 표시 갱신
    } // 설비 강화 종료
    private void HandleFacilityChanged(FacilityType type, int level) // 설비 단계 변경 처리
    { // 설비 변경 처리 시작
        Refresh(); // 변경된 설비와 버튼 표시 갱신
    } // 설비 변경 처리 종료
    private void Refresh() // 보유 자원과 선택 설비 표시 갱신
    { // 설비 표시 갱신 시작
        if (facilities == null || resources == null || facilityNameText == null) // 화면 초기화 상태 확인
        { // 갱신 불가 처리 시작
            return; // 화면 갱신 중단
        } // 갱신 불가 처리 종료
        resourceValues[0].text = resources.Gold.ToString(); // 현재 골드 수량 표시
        resourceValues[1].text = resources.Screw.ToString(); // 현재 나사 수량 표시
        resourceValues[2].text = resources.IronPlate.ToString(); // 현재 철판 수량 표시
        resourceValues[3].text = resources.Wire.ToString(); // 현재 전선 수량 표시
        foreach (KeyValuePair<FacilityType, Button> entry in facilityButtons) // 설비 버튼 목록 순회
        { // 설비 버튼 갱신 시작
            FacilityDefinition definition = facilities.GetDefinition(entry.Key); // 해당 설비 데이터 조회
            entry.Value.GetComponentInChildren<TMP_Text>().text = $"{definition.DisplayName}  Lv.{facilities.GetLevel(entry.Key)}"; // 설비 이름과 단계 표시
            entry.Value.GetComponent<Outline>().enabled = entry.Key == selectedType; // 선택 설비 강조 표시
        } // 설비 버튼 갱신 종료
        FacilityDefinition selected = facilities.GetDefinition(selectedType); // 선택 설비 데이터 조회
        FacilityLevelDefinition current = facilities.GetCurrentLevelDefinition(selectedType); // 현재 설비 효과 조회
        FacilityLevelDefinition next = facilities.GetNextLevelDefinition(selectedType); // 다음 강화 효과 조회
        facilityNameText.text = selected.DisplayName; // 선택 설비 이름 표시
        levelText.text = $"현재 단계  {facilities.GetLevel(selectedType)} / {FacilityUpgradeManager.MaximumLevel}"; // 현재 강화 단계 표시
        currentEffectText.text = current == null ? "적용 중인 효과 없음" : current.EffectDescription; // 현재 효과 설명 표시
        nextEffectText.text = next == null ? "최대 단계에 도달했습니다." : next.EffectDescription; // 다음 효과 설명 표시
        costText.text = next == null ? "더 이상 강화할 수 없습니다." : $"필요 자원  골드 {next.GoldCost}  ·  나사 {next.ScrewCost}  ·  철판 {next.IronPlateCost}  ·  전선 {next.WireCost}"; // 다음 강화 비용 표시
        upgradeButton.interactable = next != null && facilities.CanUpgrade(selectedType); // 자원과 최대 단계 기준 강화 가능 상태 적용
    } // 설비 표시 갱신 종료
    public void Close() // 설비 강화 화면 닫기
    { // 화면 닫기 시작
        gameObject.SetActive(false); // 화면과 입력 즉시 숨김
        Action callback = closed; // 닫힘 알림 임시 저장
        closed = null; // 닫힘 알림 중복 방지
        callback?.Invoke(); // 화면 관리자에 닫힘 알림
        Destroy(gameObject); // 설비 화면 객체 제거
    } // 화면 닫기 종료
    private void OnDestroy() // 설비 화면 이벤트 연결 해제
    { // 화면 정리 시작
        if (facilities != null) // 설비 관리자 존재 확인
        { // 설비 이벤트 해제 시작
            facilities.FacilityChanged -= HandleFacilityChanged; // 설비 변경 이벤트 해제
        } // 설비 이벤트 해제 종료
        if (resources != null) // 자원 관리자 존재 확인
        { // 자원 이벤트 해제 시작
            resources.ResourcesChanged -= Refresh; // 자원 변경 이벤트 해제
        } // 자원 이벤트 해제 종료
    } // 화면 정리 종료
    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Vector2 minimum, Vector2 maximum) // 비율 기반 설비 문구 생성
    { // 설비 문구 생성 시작
        TMP_Text text = PreparationUi.CreateText(name, parent, value, size); // 한글 문구 생성
        PreparationUi.Stretch(text.rectTransform, minimum, maximum); // 부모 비율 기준 위치 설정
        return text; // 생성 문구 반환
    } // 설비 문구 생성 종료
    private static Button CreateButton(string name, Transform parent, string label, Vector2 minimum, Vector2 maximum, Action action) // 비율 기반 설비 버튼 생성
    { // 설비 버튼 생성 시작
        Button button = PreparationUi.CreateButton(name, parent, label, Vector2.zero, Vector2.zero, Vector2.zero, action); // 공통 금속 버튼 생성
        PreparationUi.Stretch(button.GetComponent<RectTransform>(), minimum, maximum); // 부모 비율 기준 위치 설정
        return button; // 생성 버튼 반환
    } // 설비 버튼 생성 종료
    private static Sprite GetPanelSprite() // 제작한 설비 패널 이미지 조회
    { // 패널 이미지 조회 시작
        if (panelSprite != null) // 기존 이미지 존재 확인
        { // 기존 이미지 처리 시작
            return panelSprite; // 공용 패널 이미지 반환
        } // 기존 이미지 처리 종료
        Texture2D texture = Resources.Load<Texture2D>("UI/Facility/Panel"); // 제작 패널 텍스처 조회
        if (texture == null) // 제작 이미지 누락 확인
        { // 이미지 누락 처리 시작
            Debug.LogWarning("[FacilityUpgrade] 설비 패널 이미지를 찾을 수 없습니다."); // 이미지 누락 안내
            return null; // 기본 배경 사용 반환
        } // 이미지 누락 처리 종료
        panelSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 전체 텍스처를 패널 스프라이트로 생성
        panelSprite.hideFlags = HideFlags.HideAndDontSave; // 공용 이미지 임시 보관 설정
        return panelSprite; // 생성 패널 이미지 반환
    } // 패널 이미지 조회 종료
} // 설비 화면 종료
