using System.Text; // 문자열 조합 기능 사용
using TMPro; // 상세 제목과 본문 글자 사용
using UnityEngine; // 화면 위치와 좌표 계산 사용
using UnityEngine.UI; // 스크롤과 화면 클리핑 사용

public sealed class BattleStatusEffectTooltipView : MonoBehaviour // 전투 상태 이상 상세 화면
{ // 상태 이상 상세 화면 시작
    private const float SidePadding = 44f; // 프레임 모서리 장식을 피하는 좌우 여백
    private const float FramePadding = 26f; // 프레임 모서리 장식을 피하는 상하 여백
    private const float TitleHeight = 27f; // 한글 제목 한 줄의 전체 글자 높이
    private const float TitleGap = 6f; // 제목과 본문 사이 여백
    private const float BarWidth = 14f; // 마우스로 쉽게 잡을 수 있는 스크롤바 폭
    private const float BarGap = 8f; // 본문과 스크롤바 사이 여백
    private const float BodyTop = FramePadding + TitleHeight + TitleGap; // 프레임 상단에서 본문 시작까지의 거리
    private const int TooltipSortingOrder = 450; // 전투 인물과 화살표 위, 선택 정보창 아래 순서

    private GameObject tooltipRoot; // 상태 상세 표시 루트
    private BattleUnitRuntime shownUnit; // 마지막으로 상세를 표시한 유닛
    private TMP_Text titleText; // 스크롤 영역 밖의 고정 제목
    private TMP_Text detailText; // 스크롤 영역 안의 본문
    private ScrollRect scroll; // 휠과 스크롤바 입력 연결
    private readonly Vector3[] canvasCorners = new Vector3[4]; // 화면 위치 검사에 재사용할 꼭짓점

    internal RectTransform PanelRect => tooltipRoot != null ? (RectTransform)tooltipRoot.transform : null; // 입력과 화면 검사가 참조하는 실제 상세 패널

    public void Show(BattleUnitRuntime runtimeUnit) // 상태 이상 상세 표시
    { // 상세 표시 시작
        if (runtimeUnit == null || runtimeUnit.StatusEffects.Count < 1) // 표시 상태 존재 확인
        { // 표시 불가 처리 시작
            Hide(); // 툴팁 숨김
            return; // 상세 표시 중단
        } // 표시 불가 처리 종료

        EnsureVisualStructure(); // 처음 열 때 상태 상세 화면 준비
        bool resetScroll = !tooltipRoot.activeSelf || shownUnit != runtimeUnit; // 새로 연 화면 또는 다른 유닛 확인
        shownUnit = runtimeUnit; // 상세 화면에 표시할 유닛 기록
        ProjectCLanguageDisplay.SetCaption(titleText, $"{runtimeUnit.DisplayName} 상태 상세"); // 높이 계산 전에 현재 언어의 제목 적용
        StringBuilder detailBuilder = new StringBuilder(); // 상태 효과 본문 문자열 생성
        for (int effectIndex = 0; effectIndex < runtimeUnit.StatusEffects.Count; effectIndex++) // 상태 이상 목록 순회
        { // 개별 상태 상세 생성 시작
            BattleStatusEffectInstance statusEffect = runtimeUnit.StatusEffects[effectIndex]; // 현재 상태 이상 조회
            string colorCode = statusEffect.IsDebuff ? "#FF7777" : "#72E69A"; // 버프와 디버프 색상 선택
            string categoryLabel = statusEffect.IsDebuff ? "디버프" : "버프"; // 상태 분류 문구 선택
            detailBuilder.AppendLine($"<color={colorCode}>[{statusEffect.IconLabel}] {statusEffect.DisplayName} · {categoryLabel}</color>"); // 상태 이름과 분류 추가
            string duration = statusEffect.LastsUntilBattleEnd ? "전투 종료까지" : statusEffect.WaitsForAction ? "1회" : $"{statusEffect.RemainingTurns}T"; // 전투 수명과 행동 및 일반 지속 턴 구분
            string stacks = statusEffect.MaximumStacks == int.MaxValue ? $"{statusEffect.StackCount}중첩" : $"{statusEffect.StackCount}/{statusEffect.MaximumStacks}중첩"; // 제한 없는 상태와 일반 중첩 표시
            detailBuilder.AppendLine($"{statusEffect.Description} · {stacks} · {duration}"); // 실제 소모 조건과 함께 상태 효과 표시
        } // 개별 상태 상세 생성 종료

        ProjectCLanguageDisplay.SetCaption(detailText, detailBuilder.ToString().TrimEnd()); // 말줄임 없는 현재 언어 본문의 높이 계산 준비
        tooltipRoot.SetActive(true); // 상태 상세 표시
        tooltipRoot.transform.SetAsLastSibling(); // 유닛 위에 상태 상세 배치
        LayoutBody(resetScroll); // 본문 길이에 맞춰 스크롤과 표시 범위 갱신
        KeepWithinCanvas(); // 첫 표시 프레임의 화면 범위 보정
    } // 상세 표시 종료

    public void Hide() // 상태 이상 상세 숨김
    { // 상세 숨김 시작
        if (tooltipRoot != null) // 툴팁 화면 존재 확인
        { // 툴팁 숨김 처리 시작
            tooltipRoot.SetActive(false); // 상세 화면과 입력 비활성화
        } // 툴팁 숨김 처리 종료
    } // 상세 숨김 종료

    private void LateUpdate() // 화면 크기와 유닛 위치에 따른 상세 범위 보정
    { // 툴팁 위치 갱신 시작
        if (tooltipRoot != null && tooltipRoot.activeSelf) // 표시 중인 상태 상세 확인
        { // 상세 위치 보정 시작
            KeepWithinCanvas(); // 현재 캔버스 안쪽으로 상세 위치 이동
        } // 상세 위치 보정 종료
    } // 툴팁 위치 갱신 종료

    private void KeepWithinCanvas() // 유닛과 연결된 상태로 화면 안쪽에 상세 배치
    { // 화면 범위 보정 시작
        Canvas canvas = GetComponentInParent<Canvas>(); // 부모 캔버스 조회
        RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null; // 최상위 화면 영역 조회
        RectTransform sourceRect = transform as RectTransform; // 상태 상세의 유닛 영역 조회
        if (canvasRect == null || sourceRect == null) // 필수 화면 영역 확인
        { // 화면 영역 누락 처리 시작
            return; // 위치 보정 중단
        } // 화면 영역 누락 처리 종료

        RectTransform tooltipRect = PanelRect; // 상세창 사각형 조회
        tooltipRect.GetWorldCorners(canvasCorners); // 현재 상세창의 하단과 상단 위치 조회
        Vector3 minimum = canvasRect.InverseTransformPoint(canvasCorners[0]); // 캔버스 기준 상세창 왼쪽 아래 좌표
        Vector3 maximum = canvasRect.InverseTransformPoint(canvasCorners[2]); // 캔버스 기준 상세창 오른쪽 위 좌표
        Vector3 sourceLeft = canvasRect.InverseTransformPoint(sourceRect.TransformPoint(new Vector2(sourceRect.rect.xMin, sourceRect.rect.center.y))); // 캔버스 기준 유닛 왼쪽 중앙 좌표
        Vector3 sourceRight = canvasRect.InverseTransformPoint(sourceRect.TransformPoint(new Vector2(sourceRect.rect.xMax, sourceRect.rect.center.y))); // 캔버스 기준 유닛 오른쪽 중앙 좌표
        float width = maximum.x - minimum.x; // 캔버스 기준 상세창 너비
        float height = maximum.y - minimum.y; // 캔버스 기준 상세창 높이
        float spaceRight = canvasRect.rect.xMax - sourceRight.x; // 유닛 오른쪽에 남은 화면 너비
        float spaceLeft = sourceLeft.x - canvasRect.rect.xMin; // 유닛 왼쪽에 남은 화면 너비
        float attachedX = spaceRight < width && spaceLeft > spaceRight ? sourceLeft.x - width : sourceRight.x; // 포인터가 바로 이동하도록 유닛 옆에 붙일 위치
        float x = Mathf.Clamp(attachedX, canvasRect.rect.xMin, Mathf.Max(canvasRect.rect.xMin, canvasRect.rect.xMax - width)); // 화면 좌우를 벗어나지 않는 위치
        float y = Mathf.Clamp(sourceRight.y - height * 0.5f, canvasRect.rect.yMin, Mathf.Max(canvasRect.rect.yMin, canvasRect.rect.yMax - height)); // 유닛 중앙 높이와 화면 상하 범위를 반영한 위치
        Vector3 screenDelta = new Vector3(x - minimum.x, y - minimum.y, 0f); // 상세창을 화면 안에 배치할 보정량
        Vector3 localDelta = tooltipRect.parent.InverseTransformVector(canvasRect.TransformVector(screenDelta)); // 현재 상세창 부모 기준 위치 계산
        tooltipRect.anchoredPosition += (Vector2)localDelta; // 화면 안쪽 유닛 측면으로 상세창 배치
    } // 화면 범위 보정 종료

    private void LayoutBody(bool resetScroll) // 본문 길이에 따른 스크롤바와 글자 영역 갱신
    { // 본문 배치 시작
        RectTransform viewport = scroll.viewport; // 본문 표시 영역 조회
        RectTransform content = scroll.content; // 실제로 이동하는 본문 영역 조회
        float previousOffset = resetScroll ? 0f : content.anchoredPosition.y; // 새 표시 여부에 따른 본문 위치 선택
        viewport.offsetMax = new Vector2(-SidePadding, -BodyTop); // 스크롤바 없는 상태의 본문 전체 너비 복원
        float height = Mathf.Ceil(detailText.GetPreferredValues(detailText.text, viewport.rect.width, Mathf.Infinity).y); // 줄바꿈을 반영한 전체 본문 높이 측정
        bool needsScroll = height > viewport.rect.height; // 본문이 표시 영역을 넘는지 확인
        if (needsScroll) // 본문과 스크롤바 분리 필요 확인
        { // 스크롤 가능한 본문 배치 시작
            viewport.offsetMax = new Vector2(-SidePadding - BarWidth - BarGap, -BodyTop); // 본문 오른쪽에 스크롤바 자리 확보
            height = Mathf.Ceil(detailText.GetPreferredValues(detailText.text, viewport.rect.width, Mathf.Infinity).y); // 좁아진 본문 너비로 줄바꿈 높이 재계산
        } // 스크롤 가능한 본문 배치 종료

        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height); // 전체 문구만큼 실제 본문 높이 설정
        content.anchoredPosition = new Vector2(0f, Mathf.Clamp(previousOffset, 0f, Mathf.Max(0f, height - viewport.rect.height))); // 기존 위치 유지 또는 첫 줄 복원과 끝 위치 제한
        scroll.scrollSensitivity = GameplaySpeedSettings.ScrollSensitivity(28f); // 사용자 설정을 반영한 휠 이동 거리
        scroll.verticalScrollbar.gameObject.SetActive(needsScroll); // 본문이 넘칠 때만 스크롤바 표시
    } // 본문 배치 종료

    private void EnsureVisualStructure() // 장식 프레임과 상세 화면 구성
    { // 상세 화면 구성 시작
        if (tooltipRoot != null) // 기존 화면 재사용 확인
        { // 기존 화면 처리 시작
            return; // 추가 화면 생성 중단
        } // 기존 화면 처리 종료

        tooltipRoot = new GameObject("StatusEffectTooltip", typeof(RectTransform), typeof(Canvas), typeof(Image), typeof(ScrollRect), typeof(BattleStatusEffectTooltipRaycaster)); // 입력이 가능한 상태 상세 프레임 생성
        Canvas battleCanvas = GetComponentInParent<Canvas>(); // 소유 유닛이 속한 실제 전투 캔버스
        tooltipRoot.transform.SetParent(battleCanvas != null ? battleCanvas.rootCanvas.transform : transform, false); // 다른 유닛 그림보다 앞에 배치할 전투 캔버스 직접 연결
        Canvas tooltipCanvas = tooltipRoot.GetComponent<Canvas>(); // 상태 상세 전용 캔버스 조회
        tooltipCanvas.overrideSorting = true; // 상위 캔버스 정렬 순서 분리
        tooltipCanvas.sortingOrder = TooltipSortingOrder; // 전투 도중 상세창의 실제 표시 우선순위
        tooltipRoot.GetComponent<BattleStatusEffectTooltipRaycaster>().Bind(GetComponent<BattleUnitView>()); // 상세 패널과 원래 유닛의 마우스 진입 연결
        tooltipRoot.SetActive(false); // 배치 중 상세창 비활성화
        RectTransform root = tooltipRoot.GetComponent<RectTransform>(); // 상세 프레임 영역 조회
        root.pivot = new Vector2(0f, 0.5f); // 유닛 옆에 붙는 왼쪽 중앙 기준 지정
        PreparationUi.Place(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 180f)); // 기본 크기의 상세 프레임 배치
        Image background = tooltipRoot.GetComponent<Image>(); // 장식 프레임 이미지 조회
        background.color = new Color(0.025f, 0.035f, 0.06f, 0.98f); // 프레임 누락 시 사용할 어두운 배경 지정
        LobbyUiArt.Apply(background, "Button"); // 기존 금속 장식 프레임 적용

        titleText = PreparationUi.CreateText("TitleText", root, string.Empty, 16f); // 장식 안쪽의 고정 제목 생성
        titleText.enableAutoSizing = false; // 제목을 항상 같은 크기로 표시
        titleText.fontStyle = FontStyles.Bold; // 본문과 구분되는 굵은 제목 설정
        titleText.alignment = TextAlignmentOptions.TopLeft; // 본문과 같은 왼쪽 정렬 적용
        titleText.textWrappingMode = TextWrappingModes.NoWrap; // 고정 제목 영역의 한 줄 배치
        titleText.overflowMode = TextOverflowModes.Ellipsis; // 제목이 지나치게 길 때 제목 영역 안에서 생략
        RectTransform titleRect = titleText.rectTransform; // 제목의 배치 영역 조회
        PreparationUi.Stretch(titleRect, Vector2.up, Vector2.one); // 프레임 상단 기준 제목 배치
        titleRect.offsetMin = new Vector2(SidePadding, -FramePadding - TitleHeight); // 제목의 왼쪽과 아래쪽 안쪽 여백
        titleRect.offsetMax = new Vector2(-SidePadding, -FramePadding); // 제목의 오른쪽과 위쪽 안쪽 여백

        RectTransform viewport = PreparationUi.CreatePanel("Viewport", root, Vector2.zero, Vector2.one); // 스크롤 본문이 보이는 영역 생성
        viewport.offsetMin = new Vector2(SidePadding, FramePadding); // 본문의 왼쪽과 아래쪽 안쪽 여백
        viewport.offsetMax = new Vector2(-SidePadding, -BodyTop); // 제목 아래에서 시작하는 본문의 오른쪽과 위쪽 여백
        viewport.GetComponent<Image>().color = Color.clear; // 프레임 배경을 보존하는 투명 입력 영역
        viewport.gameObject.AddComponent<RectMask2D>(); // 본문이 장식이나 제목 위로 그려지는 현상 방지
        detailText = PreparationUi.CreateText("DetailText", viewport, string.Empty, 16f); // 표시 영역 안에 스크롤 본문 생성
        detailText.enableAutoSizing = false; // 스크롤 길이와 무관한 본문 글자 크기 유지
        detailText.color = new Color(0.9f, 0.93f, 1f, 1f); // 기존 본문 글자색 유지
        detailText.alignment = TextAlignmentOptions.TopLeft; // 본문 왼쪽 위 정렬
        detailText.textWrappingMode = TextWrappingModes.Normal; // 너비에 맞춘 본문 줄바꿈
        detailText.overflowMode = TextOverflowModes.Overflow; // 본문 생략 없이 마스크에서만 화면 가림
        detailText.richText = true; // 버프와 디버프 색상 태그 허용
        RectTransform content = detailText.rectTransform; // 움직일 본문 사각형 조회
        content.pivot = new Vector2(0.5f, 1f); // 높이 변화 시 본문 상단 기준 유지
        PreparationUi.Stretch(content, Vector2.up, Vector2.one); // 본문이 표시 영역의 가로 폭 채움

        scroll = tooltipRoot.GetComponent<ScrollRect>(); // 상태 상세 스크롤 컴포넌트 조회
        scroll.viewport = viewport; // 볼 수 있는 본문 영역 지정
        scroll.content = content; // 실제로 움직이는 본문 지정
        scroll.horizontal = false; // 가로 이동 비활성화
        scroll.vertical = true; // 손잡이 연결을 유지하는 세로 스크롤 축
        scroll.inertia = false; // 스크롤 손을 뗀 뒤의 추가 이동 비활성화
        scroll.movementType = ScrollRect.MovementType.Clamped; // 첫 줄과 마지막 줄 범위에서만 스크롤 허용
        scroll.verticalScrollbar = CreateScrollbar(root); // 수직 스크롤바 조작과 본문 위치 동기화
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide; // 스크롤할 내용이 없을 때 스크롤바 숨김
    } // 상세 화면 구성 종료

    private static Scrollbar CreateScrollbar(RectTransform root) // 상세창 안쪽에 수직 스크롤바 구성
    { // 스크롤바 구성 시작
        RectTransform track = PreparationUi.CreatePanel("Scrollbar", root, Vector2.right, Vector2.one); // 프레임 오른쪽 안쪽에 세로 트랙 생성
        track.offsetMin = new Vector2(-SidePadding - BarWidth, FramePadding); // 트랙의 시작 폭과 아래 여백 설정
        track.offsetMax = new Vector2(-SidePadding, -BodyTop); // 트랙 높이를 본문 표시 범위에 맞춤
        track.GetComponent<Image>().color = new Color(0.045f, 0.09f, 0.1f, 0.95f); // 손잡이와 구분되는 짙은 트랙 색상
        RectTransform handle = PreparationUi.CreatePanel("Handle", track, Vector2.zero, Vector2.one); // 드래그 가능한 스크롤 손잡이 생성
        Image handleImage = handle.GetComponent<Image>(); // 손잡이 모양 조회
        handleImage.color = new Color(0.82f, 0.66f, 0.37f); // 장식 프레임과 어울리는 밝은 손잡이 색상
        Scrollbar bar = track.gameObject.AddComponent<Scrollbar>(); // 세로 스크롤바 조작 연결
        bar.direction = Scrollbar.Direction.BottomToTop; // 스크롤 값의 시작 1과 끝 0 방향 설정
        bar.targetGraphic = handleImage; // 포인터 상태에 따른 손잡이 강조 연결
        bar.handleRect = handle; // 스크롤바의 실제 이동 손잡이 지정
        bar.navigation = new Navigation { mode = Navigation.Mode.None }; // 전투 키보드 조작에서 스크롤바 포커스 제외
        track.gameObject.SetActive(false); // 본문 길이 확인 전 스크롤바 숨김
        return bar; // 동작이 연결된 스크롤바 반환
    } // 스크롤바 구성 종료

    private void OnDisable() // 상태 상세 화면 비활성화 처리
    { // 화면 비활성화 시작
        Hide(); // 상세 화면과 입력 숨김
    } // 화면 비활성화 종료

    private void OnDestroy() // 소유 유닛이 사라진 뒤 상세 패널 정리
    { // 전투 캔버스에 따로 연결된 상세창 정리 시작
        if (tooltipRoot != null && tooltipRoot.transform.parent != transform) // 유닛과 함께 제거되지 않는 외부 상세창 확인
        { // 외부 상세창 삭제 시작
            Destroy(tooltipRoot); // 소유 유닛 없이 남은 전투 캔버스 패널 제거
        } // 외부 상세창 삭제 종료
    } // 전투 캔버스에 따로 연결된 상세창 정리 종료
} // 상태 이상 상세 화면 종료
