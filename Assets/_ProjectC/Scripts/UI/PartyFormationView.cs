using System.Collections.Generic; // 캐릭터 버튼 목록 사용
using TMPro; // 편성 화면 한글 문구 사용
using UnityEngine; // 캐릭터 이미지와 화면 사용
using UnityEngine.UI; // 편성 버튼과 목록 사용

public sealed class PartyFormationView : MonoBehaviour // 탐사 전 새 파티 편성 화면
{ // 파티 편성 화면 시작
    private Canvas canvas; // 맵 선택 복귀 캔버스
    private CharacterRosterData roster; // 전체 캐릭터 원본 목록
    private PartySelection selection; // 현재 출전 파티 선택
    private IPartyDeploymentStatusProvider status; // 캐릭터 출전 상태 제공자
    private CharacterRecoveryManager recovery; // 회복 상태 변경 관리자
    private BattleResultManager results; // 캐릭터 전투 상태 관리자
    private readonly List<Button> characterButtons = new List<Button>(); // 캐릭터 선택 버튼 목록
    private readonly List<TMP_Text> profileNumbers = new List<TMP_Text>(); // 프로필별 편성 번호 문구
    private readonly Image[] slotImages = new Image[PartyData.MaxPartySize]; // 편성 칸 캐릭터 이미지
    private readonly TMP_Text[] slotNames = new TMP_Text[PartyData.MaxPartySize]; // 편성 칸 캐릭터 이름
    private readonly Outline[] slotHighlights = new Outline[PartyData.MaxPartySize]; // 선택된 편성 칸 강조 테두리
    private readonly Button[] presetButtons = new Button[PartyPresetStore.PresetCount]; // 네 개의 편성 선택 버튼
    private TMP_Text detailName; // 선택 캐릭터 상세 이름
    private TMP_Text detailStats; // 선택 캐릭터 상세 능력치
    private TMP_Text detailDescription; // 선택 캐릭터 상세 설명
    private TMP_Text message; // 출전 안내 문구
    private Button startButton; // 탐사 출발 버튼
    private CharacterData focused; // 현재 상세 정보 캐릭터
    private RectTransform profileViewport; // 프로필 격자 표시 영역
    private RectTransform profileGrid; // 프로필 격자 내용 영역
    private GridLayoutGroup profileLayout; // 프로필 세 열 배치
    private float lastProfileWidth = -1f; // 최근 프로필 표시 영역 너비
    public static PartyFormationView Open(Canvas targetCanvas) // 새 편성 화면 열기
    { // 편성 화면 열기 시작
        if (targetCanvas == null || FindFirstObjectByType<PartyFormationView>() != null) // 캔버스와 중복 화면 확인
        { // 화면 열기 불가 처리 시작
            return null; // 편성 화면 생성 중단
        } // 화면 열기 불가 처리 종료
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        if (roster == null) // 캐릭터 목록 존재 확인
        { // 캐릭터 목록 누락 처리 시작
            Debug.LogWarning("[Formation] 캐릭터 목록을 찾을 수 없습니다."); // 목록 누락 안내
            return null; // 편성 화면 생성 중단
        } // 캐릭터 목록 누락 처리 종료
        RectTransform root = PreparationUi.CreateOverlay(targetCanvas, "FormationSetup"); // 편성 전체 화면 생성
        PartyFormationView view = root.gameObject.AddComponent<PartyFormationView>(); // 편성 화면 동작 추가
        view.canvas = targetCanvas; // 부모 캔버스 보관
        view.roster = roster; // 캐릭터 목록 보관
        view.recovery = CharacterRecoveryManager.EnsureInstance(); // 회복 상태 관리자 준비
        view.results = BattleResultManager.EnsureInstance(); // 전투 결과 관리자 준비
        view.status = new CharacterRecoveryDeploymentStatusProvider(view.recovery); // 실제 출전 상태 연결
        view.selection = new PartySelection(view.status); // 빈 출전 파티 생성
        view.Build(); // 캐릭터 목록과 네 칸 구성
        view.recovery.RecoveryStateChanged += view.Refresh; // 회복 변경 시 화면 갱신
        view.results.PartyStateChanged += view.Refresh; // 전투 상태 변경 시 화면 갱신
        view.LoadPreset(PartyPresetStore.Selected); // 저장된 선택 편성 표시
        return view; // 생성 편성 화면 반환
    } // 편성 화면 열기 종료
    private void LateUpdate() // 프로필 화면 크기 변경 감지
    { // 크기 변경 감지 시작
        if (profileViewport != null && !Mathf.Approximately(lastProfileWidth, profileViewport.rect.width)) // 프로필 표시 영역 너비 변경 확인
        { // 프로필 크기 조정 시작
            RefreshProfileLayout(); // 세 열 너비에 맞춘 프로필 크기 갱신
        } // 프로필 크기 조정 종료
    } // 크기 변경 감지 종료
    private void Build() // 새 편성 화면 배치
    { // 편성 화면 배치 시작
        TMP_Text title = PreparationUi.CreateText("Title", transform, "탐사 파티 편성", 42f); // 편성 화면 제목 생성
        PreparationUi.Place(title.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(900f, 80f)); // 제목 위치 설정
        PreparationUi.CreateButton("BackButton", transform, "뒤로", new Vector2(0.085f, 0.92f), Vector2.zero, new Vector2(200f, 70f), Back); // 맵 선택 복귀 버튼 생성
        BuildCharacterList(); // 왼쪽 캐릭터 목록 생성
        BuildSlots(); // 가운데 네 편성 칸 생성
        BuildDetails(); // 오른쪽 캐릭터 정보 생성
        for (int index = 0; index < PartyPresetStore.PresetCount; index++) // 네 개의 편성 선택 영역 순회
        { // 편성 선택 버튼 배치 시작
            int presetIndex = index; // 버튼별 편성 번호 보관
            presetButtons[index] = PreparationUi.CreateButton("Preset_" + index, transform, "편성 #" + (index + 1), new Vector2(0.36f + index * 0.105f, 0.15f), Vector2.zero, new Vector2(180f, 62f), () => LoadPreset(presetIndex)); // 편성 선택 버튼 배치
        } // 편성 선택 버튼 배치 종료
        message = PreparationUi.CreateText("Message", transform, "출전할 캐릭터를 선택하세요.", 22f); // 편성 안내 문구 생성
        PreparationUi.Place(message.rectTransform, new Vector2(0.53f, 0.075f), Vector2.zero, new Vector2(900f, 58f)); // 편성 안내 위치 설정
        startButton = PreparationUi.CreateButton("StartButton", transform, "탐사 출발", new Vector2(0.89f, 0.08f), Vector2.zero, new Vector2(280f, 80f), StartExploration); // 탐사 출발 버튼 생성
    } // 편성 화면 배치 종료
    private void BuildCharacterList() // 왼쪽 캐릭터 목록 생성
    { // 캐릭터 목록 생성 시작
        RectTransform panel = PreparationUi.CreatePanel("CharacterList", transform, new Vector2(0.02f, 0.14f), new Vector2(0.29f, 0.86f)); // 왼쪽 목록 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 제작한 캐릭터 목록 패널 적용
        ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>(); // 목록 스크롤 기능 추가
        RectTransform viewport = PreparationUi.CreatePanel("Viewport", panel, new Vector2(0.015f, 0.015f), new Vector2(0.985f, 0.985f)); // 목록 표시 영역 생성
        viewport.GetComponent<Image>().color = new Color(0.025f, 0.05f, 0.06f, 0.65f); // 목록 내부 배경 설정
        viewport.gameObject.AddComponent<RectMask2D>(); // 표시 영역 밖 항목 가리기
        GameObject gridObject = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup)); // 캐릭터 격자 객체 생성
        gridObject.transform.SetParent(viewport, false); // 목록 표시 영역 연결
        RectTransform grid = gridObject.GetComponent<RectTransform>(); // 캐릭터 격자 영역 조회
        grid.anchorMin = new Vector2(0f, 1f); // 격자 시작 앵커 설정
        grid.anchorMax = Vector2.one; // 격자 끝 앵커 설정
        grid.pivot = new Vector2(0.5f, 1f); // 격자 위쪽 기준 설정
        grid.sizeDelta = Vector2.zero; // 격자 크기 초기화
        grid.anchoredPosition = Vector2.zero; // 격자 상단 위치 설정
        GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>(); // 격자 배치 조회
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 수 설정
        layout.constraintCount = 3; // 캐릭터 세 열 배치
        layout.cellSize = new Vector2(150f, 150f); // 초기 프로필 크기 설정
        layout.spacing = new Vector2(12f, 12f); // 캐릭터 버튼 간격 확대
        layout.padding = new RectOffset(10, 10, 10, 10); // 캐릭터 목록 여백 설정
        profileViewport = viewport; // 프로필 표시 영역 저장
        profileGrid = grid; // 프로필 격자 영역 저장
        profileLayout = layout; // 프로필 격자 배치 저장
        scroll.viewport = viewport; // 스크롤 표시 영역 연결
        scroll.content = grid; // 스크롤 내용 영역 연결
        scroll.horizontal = false; // 가로 스크롤 차단
        scroll.vertical = true; // 세로 스크롤 허용
        scroll.movementType = ScrollRect.MovementType.Clamped; // 목록 범위 밖 이동 차단
        scroll.scrollSensitivity = GameplaySpeedSettings.ScrollSensitivity(36f); // 선택한 편성 목록 스크롤 속도 적용
        for (int index = 0; index < roster.Characters.Count; index++) // 전체 캐릭터 순회
        { // 캐릭터 버튼 생성 시작
            CreateCharacterButton(grid, roster.Characters[index], index); // 캐릭터별 선택 버튼 생성
        } // 캐릭터 버튼 생성 종료
        RefreshProfileLayout(); // 현재 화면 너비 기준 프로필 크기 설정
    } // 캐릭터 목록 생성 종료
    private void RefreshProfileLayout() // 세 열에 맞춘 프로필 크기와 목록 높이 설정
    { // 프로필 크기 설정 시작
        if (profileViewport == null || profileGrid == null || profileLayout == null || roster == null) // 격자 구성 상태 확인
        { // 격자 미준비 처리 시작
            return; // 크기 조정 중단
        } // 격자 미준비 처리 종료
        lastProfileWidth = profileViewport.rect.width; // 현재 프로필 표시 너비 저장
        if (lastProfileWidth <= 0f) // 표시 영역 너비 확인
        { // 너비 미준비 처리 시작
            return; // 다음 프레임에 크기 조정
        } // 너비 미준비 처리 종료
        float available = lastProfileWidth - profileLayout.padding.left - profileLayout.padding.right - profileLayout.spacing.x * 2f; // 여백과 간격 제외 너비 계산
        float size = Mathf.Max(1f, available / 3f); // 세 열에 맞춘 정사각형 크기 계산
        profileLayout.cellSize = new Vector2(size, size); // 프로필 너비와 높이 적용
        int rows = (roster.Characters.Count + 2) / 3; // 세 열 기준 전체 행 수 계산
        profileGrid.sizeDelta = new Vector2(0f, profileLayout.padding.top + profileLayout.padding.bottom + rows * size + Mathf.Max(0, rows - 1) * profileLayout.spacing.y); // 스크롤 가능한 전체 격자 높이 설정
    } // 프로필 크기 설정 종료
    private void CreateCharacterButton(Transform parent, CharacterData character, int index) // 개별 캐릭터 선택 버튼 생성
    { // 캐릭터 버튼 생성 시작
        GameObject target = new GameObject("Character_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 캐릭터 버튼 객체 생성
        target.transform.SetParent(parent, false); // 캐릭터 격자 연결
        Image portrait = target.GetComponent<Image>(); // 캐릭터 초상화 이미지 조회
        portrait.sprite = character != null ? character.Portrait : null; // 실제 캐릭터 초상화 연결
        portrait.preserveAspect = true; // 초상화 원본 비율 유지
        portrait.color = character != null ? Color.white : Color.clear; // 캐릭터 존재별 이미지 표시
        Button button = target.GetComponent<Button>(); // 캐릭터 버튼 입력 조회
        button.targetGraphic = portrait; // 버튼 상태를 초상화에 연결
        button.onClick.AddListener(() => ToggleCharacter(character)); // 캐릭터 선택 동작 연결
        target.AddComponent<PartyProfileDrag>().Bind(this, character); // 캐릭터 드래그 입력 연결
        RectTransform labelBackground = PreparationUi.CreatePanel("NameBackground", target.transform, Vector2.zero, new Vector2(1f, 0.23f)); // 캐릭터 이름 배경 생성
        labelBackground.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f); // 캐릭터 이름 배경 색상 설정
        labelBackground.GetComponent<Image>().raycastTarget = false; // 이름 배경 입력 차단 해제
        TMP_Text label = PreparationUi.CreateText("Name", target.transform, character != null ? character.DisplayName : string.Empty, 16f); // 캐릭터 이름 생성
        PreparationUi.Stretch(label.rectTransform, Vector2.zero, new Vector2(1f, 0.23f)); // 초상화 아래 이름 배치
        CreateBorder(target.transform); // 프로필 흰색 구분선 생성
        TMP_Text number = CreateNumberBadge(target.transform, string.Empty, 38f); // 프로필용 작은 편성 번호 생성
        number.transform.parent.gameObject.SetActive(false); // 미편성 프로필 번호 숨김
        profileNumbers.Add(number); // 프로필 번호 문구 보관
        characterButtons.Add(button); // 캐릭터 버튼 보관
    } // 캐릭터 버튼 생성 종료
    private static void CreateBorder(Transform parent) // 프로필의 흰색 구분선 생성
    { // 구분선 생성 시작
        CreateBorderLine("BorderTop", parent, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -2f), Vector2.zero); // 프로필 위쪽 구분선 생성
        CreateBorderLine("BorderBottom", parent, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f)); // 프로필 아래쪽 구분선 생성
        CreateBorderLine("BorderLeft", parent, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f)); // 프로필 왼쪽 구분선 생성
        CreateBorderLine("BorderRight", parent, new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero); // 프로필 오른쪽 구분선 생성
    } // 구분선 생성 종료
    private static TMP_Text CreateNumberBadge(Transform parent, string number, float size) // 편성 번호 표시 생성
    { // 편성 번호 표시 생성 시작
        GameObject target = new GameObject("NumberBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline)); // 번호 배경 객체 생성
        target.transform.SetParent(parent, false); // 번호 표시 부모 연결
        RectTransform rect = target.GetComponent<RectTransform>(); // 번호 표시 영역 조회
        rect.anchorMin = new Vector2(0f, 1f); // 왼쪽 위 시작 앵커 설정
        rect.anchorMax = new Vector2(0f, 1f); // 왼쪽 위 끝 앵커 설정
        rect.pivot = new Vector2(0f, 1f); // 왼쪽 위 기준 설정
        rect.anchoredPosition = new Vector2(7f, -7f); // 왼쪽 위 여백 설정
        rect.sizeDelta = new Vector2(size, size); // 번호 배경 크기 설정
        Image background = target.GetComponent<Image>(); // 번호 배경 이미지 조회
        background.color = new Color(0.04f, 0.12f, 0.14f, 0.96f); // 짙은 청록색 배경 적용
        background.raycastTarget = false; // 번호 배경 입력 차단 해제
        Outline outline = target.GetComponent<Outline>(); // 번호 테두리 조회
        outline.effectColor = new Color(0.86f, 0.67f, 0.33f, 1f); // 금색 테두리 적용
        outline.effectDistance = new Vector2(2f, -2f); // 번호 테두리 두께 설정
        TMP_Text label = PreparationUi.CreateText("Label", target.transform, number, size * 0.65f); // 번호 문구 생성
        PreparationUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one); // 배경 전체에 번호 배치
        label.raycastTarget = false; // 번호 문구 입력 차단 해제
        return label; // 번호 문구 반환
    } // 편성 번호 표시 생성 종료
    private static void CreateBorderLine(string name, Transform parent, Vector2 minimum, Vector2 maximum, Vector2 lower, Vector2 upper) // 개별 흰색 구분선 생성
    { // 개별 구분선 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 구분선 화면 객체 생성
        target.transform.SetParent(parent, false); // 프로필 버튼 자식 배치
        RectTransform rect = target.GetComponent<RectTransform>(); // 구분선 영역 조회
        rect.anchorMin = minimum; // 구분선 시작 앵커 설정
        rect.anchorMax = maximum; // 구분선 끝 앵커 설정
        rect.offsetMin = lower; // 구분선 시작 여백 설정
        rect.offsetMax = upper; // 구분선 끝 여백 설정
        Image image = target.GetComponent<Image>(); // 구분선 이미지 조회
        image.color = Color.white; // 흰색 구분선 설정
        image.raycastTarget = false; // 구분선 입력 차단 해제
    } // 개별 구분선 생성 종료
    private void BuildSlots() // 가운데 네 편성 칸 생성
    { // 편성 칸 생성 시작
        GameObject group = new GameObject("Slots", typeof(RectTransform)); // 편성 칸 전체 영역 생성
        group.transform.SetParent(transform, false); // 편성 화면 자식 배치
        RectTransform groupRect = group.GetComponent<RectTransform>(); // 편성 칸 전체 영역 조회
        PreparationUi.Stretch(groupRect, new Vector2(0.30f, 0.25f), new Vector2(0.79f, 0.79f)); // 가운데 편성 칸 위치 설정
        for (int index = 0; index < PartyData.MaxPartySize; index++) // 네 개의 편성 칸 순회
        { // 개별 편성 칸 생성 시작
            RectTransform slot = PreparationUi.CreatePanel("Slot_" + index, group.transform, new Vector2(index / 4f, 0f), new Vector2((index + 1) / 4f, 1f)); // 개별 편성 칸 생성
            LobbyUiArt.Apply(slot.GetComponent<Image>(), "FormationCard"); // 제작한 편성 칸 이미지 적용
            Outline highlight = slot.gameObject.AddComponent<Outline>(); // 선택된 편성 칸 강조 테두리 생성
            highlight.effectColor = new Color(1f, 0.82f, 0.38f, 1f); // 밝은 금색 선택 테두리 설정
            highlight.effectDistance = new Vector2(4f, -4f); // 선택 테두리 두께 설정
            highlight.enabled = false; // 선택 전 강조 테두리 숨김
            slotHighlights[index] = highlight; // 칸별 강조 테두리 저장
            slot.offsetMin = new Vector2(5f, 0f); // 편성 칸 왼쪽 간격 설정
            slot.offsetMax = new Vector2(-5f, 0f); // 편성 칸 오른쪽 간격 설정
            slot.gameObject.AddComponent<PartyFormationSlot>().Bind(this, index); // 드래그 대상 편성 칸 연결
            GameObject artObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 캐릭터 그림 객체 생성
            artObject.transform.SetParent(slot, false); // 편성 칸 자식 배치
            Image art = artObject.GetComponent<Image>(); // 캐릭터 그림 이미지 조회
            PreparationUi.Stretch(art.rectTransform, new Vector2(0.03f, 0.18f), new Vector2(0.97f, 0.98f)); // 그림 표시 영역 설정
            art.preserveAspect = true; // 그림 원본 비율 유지
            art.raycastTarget = false; // 그림 입력 차단 해제
            TMP_Text name = PreparationUi.CreateText("Name", slot, "빈 슬롯", 24f); // 편성 칸 이름 생성
            PreparationUi.Stretch(name.rectTransform, Vector2.zero, new Vector2(1f, 0.17f)); // 편성 칸 아래 이름 배치
            CreateNumberBadge(slot, (PartyData.MaxPartySize - index).ToString(), 52f); // 왼쪽부터 네 번째에서 첫 번째까지 표시
            slotImages[index] = art; // 편성 그림 보관
            slotNames[index] = name; // 편성 이름 보관
        } // 개별 편성 칸 생성 종료
    } // 편성 칸 생성 종료
    private void BuildDetails() // 오른쪽 캐릭터 정보 패널 생성
    { // 상세 정보 생성 시작
        RectTransform panel = PreparationUi.CreatePanel("DetailPanel", transform, new Vector2(0.805f, 0.22f), new Vector2(0.975f, 0.79f)); // 축소된 오른쪽 정보 패널 생성
        LobbyUiArt.Apply(panel.GetComponent<Image>(), "FormationPanel"); // 제작한 상세 정보 패널 이미지 적용
        detailName = PreparationUi.CreateText("SelectedName", panel, "캐릭터 정보", 30f); // 상세 이름 생성
        PreparationUi.Stretch(detailName.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f)); // 상세 이름 위치 설정
        detailStats = PreparationUi.CreateText("Stats", panel, string.Empty, 21f); // 상세 능력치 생성
        PreparationUi.Stretch(detailStats.rectTransform, new Vector2(0.07f, 0.55f), new Vector2(0.93f, 0.80f)); // 상세 능력치 위치 설정
        detailStats.alignment = TextAlignmentOptions.TopLeft; // 능력치 왼쪽 위 정렬
        detailDescription = PreparationUi.CreateText("Description", panel, "왼쪽에서 캐릭터를 선택하세요.", 20f); // 상세 설명 생성
        PreparationUi.Stretch(detailDescription.rectTransform, new Vector2(0.07f, 0.05f), new Vector2(0.93f, 0.52f)); // 상세 설명 위치 설정
        detailDescription.alignment = TextAlignmentOptions.TopLeft; // 설명 왼쪽 위 정렬
    } // 상세 정보 생성 종료
    private void ToggleCharacter(CharacterData character) // 캐릭터 편성 선택 전환
    { // 캐릭터 선택 전환 시작
        if (character == null) // 캐릭터 데이터 존재 확인
        { // 캐릭터 누락 처리 시작
            return; // 선택 전환 중단
        } // 캐릭터 누락 처리 종료
        focused = character; // 상세 정보 대상 변경
        bool changed; // 편성 변경 여부 준비
        if (Contains(character)) // 이미 선택한 캐릭터 확인
        { // 캐릭터 선택 해제 시작
            changed = selection.TryRemove(character); // 현재 파티에서 캐릭터 제거
        } // 캐릭터 선택 해제 종료
        else // 아직 선택하지 않은 캐릭터 확인
        { // 캐릭터 선택 시작
            changed = selection.TryAdd(character); // 출전 규칙에 맞는 캐릭터 추가
        } // 캐릭터 선택 종료
        if (changed) // 실제 편성 변경 확인
        { // 변경 편성 저장 시작
            PartyPresetStore.SetSlots(PartyPresetStore.Selected, selection.Slots); // 빈칸을 포함한 현재 편성 저장
        } // 변경 편성 저장 종료
        Refresh(); // 선택과 상세 정보 갱신
    } // 캐릭터 선택 전환 종료
    private void LoadPreset(int index) // 선택한 편성 불러오기
    { // 편성 불러오기 시작
        if (!PartyPresetStore.Select(index)) // 편성 번호 유효성 확인
        { // 잘못된 편성 처리 시작
            return; // 편성 전환 중단
        } // 잘못된 편성 처리 종료
        PartySelection restored = new PartySelection(status); // 저장 편성 복원용 파티 생성
        string[] savedSlots = PartyPresetStore.GetSlots(index); // 저장된 위치별 캐릭터 조회
        for (int slotIndex = 0; slotIndex < savedSlots.Length; slotIndex++) // 저장된 네 편성 칸 순회
        { // 저장 캐릭터 복원 시작
            CharacterData character = PartyPresetStore.FindCharacter(roster, savedSlots[slotIndex]); // 캐릭터 원본 조회
            if (character != null) // 원본 캐릭터 존재 확인
            { // 캐릭터 편성 복원 시작
                restored.TryRestoreAtSlot(character, slotIndex); // 출전 제한과 빈칸을 유지한 편성 복원
            } // 캐릭터 편성 복원 종료
        } // 저장 캐릭터 복원 종료
        selection = restored; // 복원된 파티 선택 적용
        focused = selection.Members.Count > 0 ? selection.Members[0] : null; // 첫 캐릭터 상세 정보 선택
        Refresh(); // 편성 전환 상태 표시
    } // 편성 불러오기 종료
    private bool Contains(CharacterData character) // 현재 파티 선택 여부 확인
    { // 선택 여부 확인 시작
        foreach (CharacterData member in selection.Members) // 선택 파티 순회
        { // 선택 캐릭터 비교 시작
            if (member == character) // 동일 캐릭터 확인
            { // 선택된 캐릭터 처리 시작
                return true; // 선택 상태 반환
            } // 선택된 캐릭터 처리 종료
        } // 선택 캐릭터 비교 종료
        return false; // 미선택 상태 반환
    } // 선택 여부 확인 종료
    private int FindSlot(CharacterData character) // 선택 캐릭터의 물리적 위치 조회
    { // 캐릭터 위치 조회 시작
        for (int index = 0; index < selection.Slots.Count; index++) // 전체 편성 칸 순회
        { // 편성 칸 비교 시작
            if (selection.Slots[index] == character && character != null) // 같은 캐릭터 확인
            { // 캐릭터 발견 처리 시작
                return index; // 현재 편성 칸 반환
            } // 캐릭터 발견 처리 종료
        } // 편성 칸 비교 종료
        return -1; // 미편성 표시 반환
    } // 캐릭터 위치 조회 종료
    public bool CanDragProfile(CharacterData character) // 프로필 드래그 가능 여부 확인
    { // 드래그 가능 확인 시작
        return character != null && selection != null && (Contains(character) || CharacterUnlockProgress.Shared.IsUnlocked(character) && !status.IsDead(character) && !status.IsRecovering(character) && status.CanDeploy(character)); // 기존 편성 또는 출전 가능 캐릭터 허용
    } // 드래그 가능 확인 종료
    public void DropProfile(CharacterData character, int index) // 캐릭터를 지정한 편성 칸에 배치
    { // 드롭 처리 시작
        if (!CanDragProfile(character) || !selection.TryPlaceAtSlot(character, index)) // 드래그 대상과 배치 가능 여부 확인
        { // 배치 실패 처리 시작
            return; // 편성 변경 중단
        } // 배치 실패 처리 종료
        focused = character; // 배치한 캐릭터 상세 정보 선택
        PartyPresetStore.SetSlots(PartyPresetStore.Selected, selection.Slots); // 빈칸과 위치를 포함한 편성 저장
        Refresh(); // 편성 칸과 프로필 번호 갱신
    } // 드롭 처리 종료
    public void SelectSlot(int index) // 편성된 캐릭터의 상세 정보 선택
    { // 편성 칸 선택 시작
        if (selection == null || index < 0 || index >= selection.Slots.Count || selection.Slots[index] == null) // 유효한 편성 캐릭터 확인
        { // 빈 칸과 잘못된 선택 처리 시작
            return; // 상세 정보 변경 중단
        } // 빈 칸과 잘못된 선택 처리 종료
        focused = selection.Slots[index]; // 클릭한 캐릭터 상세 정보 선택
        Refresh(); // 선택 테두리와 상세 정보 갱신
    } // 편성 칸 선택 종료
    private void Refresh() // 캐릭터 목록과 편성 칸 갱신
    { // 편성 상태 갱신 시작
        if (selection == null || startButton == null) // 화면 초기화 상태 확인
        { // 초기화 전 처리 시작
            return; // 상태 갱신 중단
        } // 초기화 전 처리 종료
        for (int index = 0; index < characterButtons.Count; index++) // 전체 캐릭터 버튼 순회
        { // 캐릭터 상태 표시 시작
            CharacterData character = roster.Characters[index]; // 해당 캐릭터 원본 조회
            int selectedSlot = FindSlot(character); // 캐릭터 편성 위치 조회
            bool chosen = selectedSlot >= 0; // 현재 캐릭터 선택 여부 확인
            bool available = character != null && CharacterUnlockProgress.Shared.IsUnlocked(character) && status.CanDeploy(character); // 캐릭터 해금과 출전 가능 여부 확인
            characterButtons[index].interactable = chosen || available && selection.Members.Count < PartyData.MaxPartySize; // 출전 가능 상태별 선택 허용
            profileNumbers[index].transform.parent.gameObject.SetActive(chosen); // 편성된 프로필에만 번호 표시
            if (chosen) // 편성 번호 표시 필요 확인
            { // 프로필 번호 갱신 시작
                profileNumbers[index].text = (PartyData.MaxPartySize - selectedSlot).ToString(); // 오른쪽 첫 번째부터 번호 적용
            } // 프로필 번호 갱신 종료
        } // 캐릭터 상태 표시 종료
        for (int index = 0; index < PartyData.MaxPartySize; index++) // 네 개의 편성 칸 순회
        { // 편성 칸 표시 시작
            CharacterData character = selection.Slots[index]; // 해당 위치에 배치된 캐릭터 조회
            slotNames[index].text = character != null ? character.DisplayName : "빈 슬롯"; // 캐릭터 이름 또는 빈 칸 표시
            slotImages[index].sprite = character != null ? character.BattleIllustration != null ? character.BattleIllustration : character.Portrait : null; // 실제 캐릭터 일러스트 표시
            slotImages[index].enabled = character != null && slotImages[index].sprite != null; // 캐릭터 그림 존재 시 표시
            slotHighlights[index].enabled = character != null && character == focused; // 상세 선택 캐릭터의 편성 칸 강조
        } // 편성 칸 표시 종료
        bool canDeploy = selection.Validate().CanDeploy; // 현재 편성의 출전 가능 여부 확인
        startButton.interactable = canDeploy; // 출전 가능 파티에만 탐사 허용
        message.text = selection.Members.Count == 0 ? "출전할 캐릭터를 선택하세요." : canDeploy ? "선택한 캐릭터  " + selection.Members.Count + "/" + PartyData.MaxPartySize : "출전할 수 없는 캐릭터가 포함되어 있습니다."; // 현재 편성 상태 안내
        for (int index = 0; index < presetButtons.Length; index++) // 전체 편성 선택 버튼 순회
        { // 현재 편성 강조 시작
            TMP_Text label = presetButtons[index].GetComponentInChildren<TMP_Text>(); // 편성 버튼 문구 조회
            label.color = index == PartyPresetStore.Selected ? new Color(1f, 0.78f, 0.38f, 1f) : new Color(0.95f, 0.92f, 0.83f, 1f); // 선택 편성 문구 색상 구분
        } // 현재 편성 강조 종료
        detailName.text = focused != null ? focused.DisplayName : "캐릭터 정보"; // 선택 캐릭터 이름 표시
        detailStats.text = focused != null ? "체력  " + focused.MaxHealth + "\n물리 방어  " + focused.PhysicalDefense + "\n마법 저항  " + focused.MagicalResistance + "\n정신력  " + focused.InitialMental : string.Empty; // 캐릭터 실제 능력치 표시
        detailDescription.text = focused != null ? DisplayDescription(focused.Description) : "왼쪽에서 캐릭터를 선택하세요."; // 캐릭터 역할 설명 표시
    } // 편성 상태 갱신 종료
    private static string DisplayDescription(string description) // 캐릭터 역할 설명 정리
    { // 캐릭터 설명 정리 시작
        const string prefix = "임시 능력치 · 기획 역할: "; // 기존 데이터의 개발용 접두어
        if (!string.IsNullOrEmpty(description) && description.StartsWith(prefix)) // 개발용 접두어 존재 확인
        { // 역할 설명 추출 시작
            return description.Substring(prefix.Length); // 실제 역할 설명 반환
        } // 역할 설명 추출 종료
        return description ?? string.Empty; // 나머지 캐릭터 설명 반환
    } // 캐릭터 설명 정리 종료
    private void StartExploration() // 선택 파티의 탐사 출발
    { // 탐사 출발 시작
        SceneFlowManager flow = SceneFlowManager.Instance; // 씬 전환 관리자 조회
        if (!selection.Validate().CanDeploy || flow == null || flow.IsLoadingScene || !Application.CanStreamedLevelBeLoaded("30_Exploration")) // 파티와 씬 이동 가능 여부 확인
        { // 탐사 출발 불가 처리 시작
            message.text = "출전할 파티와 탐사 화면을 확인해 주세요."; // 출발 불가 안내
            return; // 탐사 출발 중단
        } // 탐사 출발 불가 처리 종료
        if (!PartyLoadoutRuntime.Prepare(selection)) // 선택 파티와 시작 덱 준비 확인
        { // 편성 준비 실패 처리 시작
            message.text = "선택한 파티를 준비하지 못했습니다."; // 편성 준비 실패 안내
            return; // 탐사 출발 중단
        } // 편성 준비 실패 처리 종료
        ExplorationSessionManager.EnsureInstance().ResetExploration(); // 이전 탐사 회차 상태 초기화
        PlayerResourceManager.EnsureInstance().BeginExplorationResourceTracking(); // 새 탐사 획득 재화 구분 시작
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 선택 파티의 시작 덱 적용
        flow.LoadScene("30_Exploration"); // 선택한 파티로 탐사 씬 이동
    } // 탐사 출발 종료
    private void Back() // 맵 선택 화면으로 복귀
    { // 맵 선택 복귀 시작
        gameObject.SetActive(false); // 편성 화면 입력 차단
        ExpeditionMapView.Open(canvas); // 맵 선택 화면 다시 열기
        Destroy(gameObject); // 이전 편성 화면 제거
    } // 맵 선택 복귀 종료
    private void OnDestroy() // 편성 화면 이벤트 정리
    { // 편성 화면 정리 시작
        if (recovery != null) // 회복 관리자 존재 확인
        { // 회복 이벤트 해제 시작
            recovery.RecoveryStateChanged -= Refresh; // 회복 상태 연결 해제
        } // 회복 이벤트 해제 종료
        if (results != null) // 전투 결과 관리자 존재 확인
        { // 전투 이벤트 해제 시작
            results.PartyStateChanged -= Refresh; // 전투 상태 연결 해제
        } // 전투 이벤트 해제 종료
    } // 편성 화면 정리 종료
} // 파티 편성 화면 종료
