using System; // 버튼에 연결할 동작 사용
using TMPro; // 휴식방 글꼴과 정렬 사용
using UnityEngine; // 캔버스 영역과 스프라이트 사용
using UnityEngine.EventSystems; // 실제 탐사 입력 관리자 조회
using UnityEngine.InputSystem.UI; // 새 입력 시스템 모듈 사용
using UnityEngine.UI; // 모달과 스크롤 구성 사용

public sealed partial class RestRoomPrototypeView // 실제 탐사 휴식방 캔버스 구성
{ // 휴식 모달 배치 시작
    private const float PanelWidth = 1224f; // 휴식 모달 기준 너비
    private const float PanelHeight = 794f; // 휴식 모달 기준 높이
    private const float RowStep = 72f; // 카드 행의 높이와 간격
    private static readonly Color FrameFallback = new Color32(13, 32, 35, 255); // 외곽 원화 누락 시 배경색
    private static readonly Color Surface = new Color32(14, 31, 34, 255); // 회복과 파티의 내부 바탕색
    private static readonly Color Border = new Color32(60, 80, 75, 255); // 내부 그룹 구분선 색상
    private static readonly Color Ink = new Color32(241, 234, 215, 255); // 주요 한글 및 숫자 색상
    private static readonly Color Mist = new Color32(170, 193, 188, 255); // 이름 아래 보조 문구 색상
    private static readonly Color Gold = new Color32(233, 193, 123, 255); // 따뜻한 황동 강조색
    private static readonly Color Mint = new Color32(153, 215, 192, 255); // 사용 가능과 정신력 강조색
    private static readonly Color Ember = new Color32(239, 162, 134, 255); // 퇴색 위험과 체력 강조색
    private static readonly Color CardSurface = new Color32(26, 52, 54, 255); // 기본 카드 행 바탕색
    private static readonly Color SelectedRow = new Color32(49, 77, 70, 255); // 선택한 카드 행의 따뜻한 녹색
    private static readonly Color LockedRow = new Color32(24, 39, 41, 255); // 이미 사용했거나 잠긴 행 바탕색
    private static readonly Vector2 TopLeft = new Vector2(0f, 1f); // 수치 배치의 왼쪽 위 기준
    private GameObject modal; // 전체 화면 모달 입력 차단막
    private TMP_Text riskValue; // 고정 방의 위험도 수치
    private TMP_Text statusValue; // 실제 휴식 사용 가능 표시
    private TMP_Text recoverHealth; // 방별 회복 비율과 고정 회복량
    private TMP_Text recoverBonus; // 유물이 더해 주는 회복 비율 보너스
    private TMP_Text recoverMental; // 실제 총 정신력 회복량
    private TMP_Text resultText; // 선택과 실행 결과 안내
    private TMP_Text partyEmpty; // 출전 파티가 비었을 때 안내
    private TMP_Text cardEmpty; // 회차 덱이 비었을 때 안내
    private RectTransform partyContent; // 최대 네 명의 파티 행 부모
    private RectTransform cardContent; // 실제 카드 행을 담는 스크롤 내용
    private ScrollRect cardScroll; // 카드 목록의 휠 및 드래그 입력
    private Button executeButton; // 휴식 실행 동작

    private void EnsureUi() // 처음 열 때 실제 캔버스 구성
    { // 휴식 캔버스 준비 시작
        if (modal != null) // 구성한 모달 재사용 여부 확인
        { // 모달 재사용 처리 시작
            return; // 기존 화면 구조 유지
        } // 모달 재사용 처리 종료
        if (EventSystem.current == null && FindFirstObjectByType<EventSystem>() == null) // 현재 탐사 씬에 입력 관리자 존재 확인
        { // 입력 관리자 초기화 시작
            GameObject input = new GameObject("RestEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 입력 시스템 기반 실제 버튼 입력 생성
            input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions(); // 포인터와 휠의 기본 입력 연결
        } // 입력 관리자 초기화 종료

        GameObject host = new GameObject("RestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 탐사 휴식 전용 Canvas 생성
        host.transform.SetParent(transform, false); // 화면 컴포넌트의 씬 수명에 연결
        Canvas canvas = host.GetComponent<Canvas>(); // 실제 휴식 화면 정렬 설정 조회
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 카메라와 무관한 정면 모달 사용
        canvas.sortingOrder = 4800; // 탐사 HUD 위와 별도 이벤트 모달 아래 배치
        CanvasScaler scaler = host.GetComponent<CanvasScaler>(); // 해상도에 맞춘 캔버스 배율 조회
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 비율에 따른 표시 크기 적용
        scaler.referenceResolution = new Vector2(1440f, 900f); // 기준 화면 해상도 설정
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // 좁은 창에서 모달 잘림 방지
        RectTransform dimmer = PreparationUi.CreateOverlay(canvas, "RestOverlay"); // 탐사 배경과 포인터를 덮는 차단막 생성
        dimmer.GetComponent<Image>().color = new Color32(6, 13, 16, 220); // 기존 탐사를 남기는 어두운 배경색 적용
        modal = dimmer.gameObject; // 재사용할 모달 루트 저장
        modal.SetActive(false); // 실제 방 상호작용까지만 화면 비표시

        Image frame = Box("RestPanel", dimmer, 0f, 0f, PanelWidth, PanelHeight, FrameFallback); // 모달 외곽과 배경 생성
        frame.rectTransform.pivot = new Vector2(0.5f, 0.5f); // 모달 중심 기준점 설정
        PreparationUi.Place(frame.rectTransform, frame.rectTransform.pivot, Vector2.zero, new Vector2(PanelWidth, PanelHeight)); // 화면 중심에 외곽 패널 배치
        LobbyUiArt.Apply(frame, "FormationPanel"); // 기존 게임의 청록 황동 프레임 적용
        RectTransform panel = frame.rectTransform; // 휴식 모달 내부의 배치 기준
        BuildHeader(panel); // 화면 제목과 위험도 및 사용 상태 영역 구성
        BuildRecovery(panel); // 캠프 그림과 회복 효과 영역 구성
        BuildParty(panel); // 탐사 파티 상태 영역 구성
        BuildCardList(panel); // 강화 카드 선택 목록 구성
        BuildFooter(panel); // 결과 안내와 휴식 실행 및 닫기 구성
    } // 휴식 캔버스 준비 종료

    private void BuildHeader(RectTransform parent) // 휴식 제목과 오른쪽 상태 표시 배치
    { // 상단 정보 영역 시작
        Label("Title", parent, "휴식 방", 48f, Ink, 32f, 20f, 570f, 66f).fontStyle = FontStyles.Bold; // 휴식방의 주 제목 표시
        RectTransform risk = Panel("Risk", parent, 811f, 31f, 184f, 43f); // 현재 방 위험도 칸 생성
        Label("RiskLabel", risk, "위험도", 17f, Mist, 12f, 0f, 68f, 43f); // 위험도 의미 표시
        riskValue = Label("RiskValue", risk, string.Empty, 21f, Gold, 70f, 0f, 101f, 43f, TextAlignmentOptions.Right); // 실제 방 위험도 수치 영역
        RectTransform status = Panel("UseStatus", parent, 1005f, 31f, 188f, 43f); // 휴식 사용 상태 칸 생성
        statusValue = Label("StatusValue", status, string.Empty, 19f, Mint, 8f, 0f, 172f, 43f, TextAlignmentOptions.Center); // 사용 가능 여부 문구 영역
        Box("HeaderRule", parent, 30f, 89f, 1164f, 1f, Border); // 제목과 본문 사이 가는 구분선 배치
    } // 상단 정보 영역 종료

    private void BuildRecovery(RectTransform parent) // 캠프 그림과 회복 수치 배치
    { // 회복 정보 영역 시작
        RectTransform artwork = Panel("CampArtwork", parent, 30f, 106f, 444f, 232f); // 캠프 그림 외곽 생성
        Sprite camp = Resources.Load<Sprite>("UI/Rest/RestBackdrop"); // 실제 휴식방 캠프 일러스트 조회
        if (camp != null) // 전용 캠프 일러스트 존재 확인
        { // 휴식 그림 표시 시작
            DrawArtwork(artwork, camp, 442f, 230f, 0.75f); // 모닥불이 보이는 넓은 범위의 원화 표시
        } // 휴식 그림 표시 종료
        else // 캠프 원화 누락 확인
        { // 휴식 그림 대체 표시 시작
            BattleIcon("CampFallback", artwork, "HealthGaugeIcon", 170f, 62f, 104f); // 기존 황동 회복 심벌로 대체
        } // 휴식 그림 대체 표시 종료

        RectTransform health = Panel("HealthRecovery", parent, 30f, 350f, 274f, 82f); // 방별 HP 회복 칸 생성
        BattleIcon("HealthIcon", health, "HealthGaugeIcon", 10f, 16f, 48f); // 기존 전투 HP 아이콘 표시
        Label("HealthCaption", health, "HP 회복", 18f, Mist, 64f, 7f, 194f, 22f); // HP 회복 효과 단위 표시
        recoverHealth = Label("RecoverHealth", health, string.Empty, 32f, Gold, 64f, 26f, 194f, 36f); // 실제 HP 회복량 영역
        recoverBonus = Label("RecoverBonus", health, string.Empty, 15f, Mist, 65f, 62f, 198f, 18f); // 장착 유물 보너스 영역
        RectTransform mental = Panel("MentalRecovery", parent, 314f, 350f, 160f, 82f); // 전체 정신력 회복 칸 생성
        BattleIcon("MentalIcon", mental, "MentalGaugeIcon", 8f, 16f, 46f); // 기존 전투 정신력 아이콘 표시
        Label("MentalCaption", mental, "정신력 회복", 17f, Mist, 57f, 8f, 95f, 22f); // 정신력 회복 효과 단위 표시
        recoverMental = Label("RecoverMental", mental, string.Empty, 32f, Mint, 57f, 27f, 94f, 36f); // 유물 효과를 합친 정신력 회복량 영역
    } // 회복 정보 영역 종료

    private void BuildParty(RectTransform parent) // 파티 상태 영역에 공통 머리말 표시
    { // 파티 상태 배치 시작
        RectTransform section = Panel("Party", parent, 30f, 444f, 444f, 257f); // 파티 상태를 담는 왼쪽 하단 칸
        Label("PartyTitle", section, "파티 상태", 25f, Ink, 14f, 6f, 178f, 36f).fontStyle = FontStyles.Bold; // 파티 상태 제목 표시
        Label("PartyHealthLabel", section, "HP", 16f, Ember, 211f, 9f, 100f, 32f); // 파티원 체력 열 머리말
        Label("PartyMentalLabel", section, "정신력", 16f, Mint, 321f, 9f, 102f, 32f); // 파티원 정신력 열 머리말
        partyContent = Area("PartyRows", section, 12f, 46f, 420f, 200f); // 최대 네 파티원 표시 영역
        partyEmpty = Label("NoParty", section, "출전 파티가 등록되지 않았습니다.", 21f, Mist, 12f, 49f, 420f, 74f, TextAlignmentOptions.Center); // 출전 파티가 없을 경우 안내
    } // 파티 상태 배치 종료

    private void BuildCardList(RectTransform parent) // 카드 목록과 실제 스크롤 입력 배치
    { // 카드 목록 배치 시작
        RectTransform section = Panel("Cards", parent, 494f, 106f, 700f, 595f); // 오른쪽 카드 그룹의 외곽 생성
        Label("CardsTitle", section, "강화할 카드 1장 선택", 29f, Ink, 16f, 6f, 658f, 42f).fontStyle = FontStyles.Bold; // 선택 가능한 장수와 카드 선택 목표 표시
        RectTransform area = Area("CardScroll", section, 14f, 53f, 672f, 525f); // 카드 휠 및 스크롤바 영역의 기준
        cardScroll = area.gameObject.AddComponent<ScrollRect>(); // 카드 목록 휠과 드래그 처리 추가
        Image viewport = Box("CardViewport", area, 0f, 0f, 651f, 525f, Surface); // 카드가 보이는 고정 영역 생성
        viewport.raycastTarget = true; // 빈 공간 위에서도 휠 입력 수신
        viewport.gameObject.AddComponent<RectMask2D>(); // 표시 영역 밖의 카드 그리기와 입력 차단
        cardContent = Area("CardContent", viewport.transform, 0f, 0f, 0f, 0f); // 실제 카드 스크롤 내용 생성
        cardContent.anchorMax = Vector2.one; // 뷰포트 너비와 동일한 스크롤 내용 너비
        cardScroll.viewport = viewport.rectTransform; // 고정 표시 영역 연결
        cardScroll.content = cardContent; // 이동할 카드 목록 연결
        cardScroll.horizontal = false; // 세로 스크롤에만 입력 적용
        cardScroll.movementType = ScrollRect.MovementType.Clamped; // 덱 목록 위아래 이동 범위 제한
        cardScroll.scrollSensitivity = GameplaySpeedSettings.ScrollSensitivity(58f); // 기존 환경설정 기반 휠 감도 적용
        cardScroll.verticalScrollbar = BuildScrollbar(area); // 실제 안내와 조작을 위한 스크롤바 연결
        cardScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide; // 모든 카드가 보이면 스크롤바 숨김
        cardEmpty = Label("NoCards", section, "현재 회차 덱에 카드가 없습니다.", 23f, Mist, 22f, 95f, 650f, 74f, TextAlignmentOptions.Center); // 강화할 회차 카드 누락 시 안내
    } // 카드 목록 배치 종료

    private static Scrollbar BuildScrollbar(RectTransform parent) // 실제 표시 범위와 드래그 손잡이 제공
    { // 카드 목록 스크롤바 시작
        Image track = Box("ScrollTrack", parent, 659f, 0f, 12f, 525f, new Color32(29, 50, 49, 255)); // 스크롤 레일 생성
        track.raycastTarget = true; // 레일 클릭과 드래그 허용
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>(); // 카드 표시 위치 제어 추가
        RectTransform lane = Area("SlidingArea", track.transform, 2f, 0f, 8f, 525f); // 손잡이가 이동할 안쪽 범위
        Image handle = Box("ScrollHandle", lane, 0f, 0f, 8f, 48f, Gold); // 현재 스크롤 위치의 황동 손잡이
        handle.rectTransform.pivot = new Vector2(0.5f, 0.5f); // 손잡이 중앙 기준점 설정
        PreparationUi.Stretch(handle.rectTransform, Vector2.zero, Vector2.one); // 스크롤바 앵커가 손잡이 크기를 직접 조절할 영역
        handle.raycastTarget = true; // 손잡이 드래그 활성화
        scrollbar.handleRect = handle.rectTransform; // 스크롤 이동 대상 연결
        scrollbar.targetGraphic = handle; // 마우스 입력 강조 표시 연결
        scrollbar.direction = Scrollbar.Direction.BottomToTop; // 목록 상단을 첫 값으로 연결
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None }; // 모달 밖의 선택 탐색 차단
        return scrollbar; // 목록 스크롤바 반환
    } // 카드 목록 스크롤바 종료

    private void BuildFooter(RectTransform parent) // 결과와 닫기 및 실행 버튼 배치
    { // 모달 하단 동작 시작
        Box("FooterRule", parent, 30f, 715f, 1164f, 1f, Border); // 본문 구역과 실행 영역의 구분선
        resultText = Label("Result", parent, string.Empty, 20f, Mist, 33f, 727f, 711f, 47f); // 선택 설명과 사용 결과 영역 생성
        resultText.textWrappingMode = TextWrappingModes.Normal; // 긴 결과 문구의 두 줄 처리 허용
        Button close = ActionButton("CloseRest", parent, "닫기", 765f, 725f, 157f, 52f, CloseWindow); // 탐사로 돌아가는 닫기 버튼 배치
        close.image.color = new Color32(192, 199, 188, 255); // 닫기 버튼의 황동 장식 밝기 낮춤
        executeButton = ActionButton("ExecuteRest", parent, "휴식 실행", 936f, 725f, 258f, 52f, ExecuteRest); // 휴식과 강화 동작을 수행할 주요 버튼 배치
        executeButton.GetComponentInChildren<TMP_Text>().color = Gold; // 주요 실행 버튼의 따뜻한 글자색 적용
    } // 모달 하단 동작 종료

    private void RefreshRecovery() // 실제 위험도와 회복 유물을 현재 모달에 반영
    { // 회복과 사용 가능 상태 표시 시작
        ProjectCLanguageDisplay.SetCaption(riskValue, hazardLevel == 0 ? "안전" : $"퇴색 Lv{hazardLevel}"); // 안전과 실제 퇴색 단계 표시
        riskValue.color = hazardLevel == 0 ? Mint : Ember; // 위험 단계의 색상 구분
        ProjectCLanguageDisplay.SetCaption(statusValue, manager.IsUsed ? "상태: 사용 완료" : "상태: 사용 가능"); // 실제 회차의 휴식 사용 상태 표시
        statusValue.color = manager.IsUsed ? Gold : Mint; // 사용 전후의 색상 구분
        int healthPercent = RestRoomRecoveryService.GetHealthRecoveryPercent(hazardLevel); // 고정 위험도별 회복률 조회
        int flatBonus = RestRoomRecoveryService.CampFlatHealthBonus; // 보유 유물에서 추가 고정 HP 조회
        int bonus = RestRoomRecoveryService.CampRecoveryBonus; // 보유 유물의 회복 비율 보너스 조회
        string health = flatBonus > 0 ? $"+{healthPercent}% +{flatBonus}" : $"+{healthPercent}%"; // 기본 회복률과 추가 고정량 조합
        ProjectCLanguageDisplay.SetCaption(recoverHealth, health); // 실제 HP 회복량 표시
        ProjectCLanguageDisplay.SetCaption(recoverBonus, bonus > 0 ? $"(회복량 +{bonus}%)" : string.Empty); // 실제 회복량에 적용되는 비율 보정 표시
        ProjectCLanguageDisplay.SetCaption(recoverMental, $"+{RestRoomRecoveryService.CampMentalRecovery}"); // 장착 유물을 포함한 정신력 회복량 표시
    } // 회복과 사용 가능 상태 표시 종료

    private static Button ActionButton(string name, RectTransform parent, string label, float x, float y, float width, float height, Action action) // 기존 황동 버튼 스타일의 실행 동작 생성
    { // 화면 동작 버튼 생성 시작
        Button button = PreparationUi.CreateButton(name, parent, label, TopLeft, Vector2.zero, new Vector2(width, height), action); // 공통 게임 버튼 스프라이트와 입력 연결
        Place((RectTransform)button.transform, x, y, width, height); // 버튼의 실제 행 위치 설정
        button.navigation = new Navigation { mode = Navigation.Mode.None }; // 오버레이 뒤 UI의 선택 이동 차단
        return button; // 구성한 버튼 반환
    } // 화면 동작 버튼 생성 종료

    private static RectTransform Panel(string name, Transform parent, float x, float y, float width, float height) // 내부 구역의 바탕과 선 생성
    { // 모달 구역 패널 시작
        Image rim = Box(name, parent, x, y, width, height, Border); // 얇은 패널 외곽 생성
        Box("Surface", rim.transform, 1f, 1f, width - 2f, height - 2f, Surface); // 외곽 안쪽의 청록 배경 생성
        return rim.rectTransform; // 내부 요소의 배치 기준 반환
    } // 모달 구역 패널 종료

    private static TMP_Text Label(string name, Transform parent, string value, float size, Color color, float x, float y, float width, float height, TextAlignmentOptions alignment = TextAlignmentOptions.Left) // 기준 좌표의 현지화 문구 생성
    { // 휴식 라벨 준비 시작
        TMP_Text text = PreparationUi.CreateText(name, parent, value, size); // 게임 글꼴과 현재 언어 적용
        text.textWrappingMode = TextWrappingModes.NoWrap; // 카드와 파티 표에서 줄바꿈 없이 표시
        text.overflowMode = TextOverflowModes.Ellipsis; // 폭보다 긴 캐릭터나 카드 이름 생략 표시
        text.color = color; // 라벨의 역할별 색 적용
        text.alignment = alignment; // 수치와 일반 글자의 정렬 적용
        Place(text.rectTransform, x, y, width, height); // 라벨의 실제 영역 지정
        return text; // 생성한 표시 라벨 반환
    } // 휴식 라벨 준비 종료

    private static Image BattleIcon(string name, Transform parent, string asset, float x, float y, float size) // 기존 전투 테마의 회복 아이콘 배치
    { // 회복 아이콘 생성 시작
        Image icon = Box(name, parent, x, y, size, size, Color.clear); // 투명 아이콘 표시 영역 생성
        BattleUiArt.Apply(icon, asset); // 지정된 전투 원화 연결
        icon.preserveAspect = true; // 원본 아이콘 비율 유지
        return icon; // 생성한 아이콘 반환
    } // 회복 아이콘 생성 종료

    private static void DrawArtwork(RectTransform parent, Sprite sprite, float width, float height, float horizontalCenter = 0.5f) // 사각형에 원본 비율을 유지한 그림 채우기
    { // 실제 원화 크기 조정 시작
        RectTransform crop = Area("ArtworkCrop", parent, 1f, 1f, width, height); // 원화 경계를 자를 실제 영역 생성
        crop.gameObject.AddComponent<RectMask2D>(); // 외곽과 닿는 원화만 표시
        float scale = Mathf.Max(width / sprite.rect.width, height / sprite.rect.height); // 그림 왜곡 없이 사각형을 채울 배율 계산
        float imageWidth = sprite.rect.width * scale; // 실제로 그릴 그림 너비 계산
        float imageHeight = sprite.rect.height * scale; // 실제로 그릴 그림 높이 계산
        Image image = Box("Artwork", crop, (width - imageWidth) * horizontalCenter, (height - imageHeight) * 0.5f, imageWidth, imageHeight, Color.white); // 초과 여백 가운데의 실제 그림 배치
        image.sprite = sprite; // 대응하는 휴식 또는 카드 일러스트 적용
    } // 실제 원화 크기 조정 종료

    private static Image Box(string name, Transform parent, float x, float y, float width, float height, Color color) // 지정한 사각형의 배경 이미지 생성
    { // 모달 이미지 생성 시작
        Image image = Area(name, parent, x, y, width, height).gameObject.AddComponent<Image>(); // 패널 위치에 이미지 표시 추가
        image.raycastTarget = false; // 실제 입력 표면만 명시적으로 포인터 수신
        image.color = color; // 패널 바탕색 적용
        return image; // 생성한 사각형 이미지 반환
    } // 모달 이미지 생성 종료

    private static RectTransform Area(string name, Transform parent, float x, float y, float width, float height) // 좌표 기반 컨테이너 생성
    { // 컨테이너 생성 시작
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); // 좌표를 가질 새 UI 요소 생성
        rect.SetParent(parent, false); // 이전 좌표 변경 없이 부모 화면 연결
        Place(rect, x, y, width, height); // 왼쪽 위를 기준으로 영역 배치
        return rect; // 생성한 컨테이너 반환
    } // 컨테이너 생성 종료

    private static void Place(RectTransform rect, float x, float y, float width, float height) // 부모 안의 왼쪽 위 기준 배치
    { // 모달 기준 영역 위치 계산 시작
        rect.pivot = TopLeft; // 좌표 및 크기를 읽을 기준점 설정
        PreparationUi.Place(rect, TopLeft, new Vector2(x, -y), new Vector2(width, height)); // 화면 왼쪽 위의 양수 거리로 배치
    } // 모달 기준 영역 위치 계산 종료
} // 휴식 모달 배치 종료
