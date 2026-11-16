using System.Text; // 문자열 조합 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleStatusEffectTooltipView : MonoBehaviour // 상태 이상 상세 툴팁 화면
{ // 클래스 시작
    private GameObject tooltipRoot; // 툴팁 표시 루트
    private TMP_Text detailText; // 상태 상세 텍스트
    private readonly Vector3[] canvasCorners = new Vector3[4]; // 화면 위치 검사에 재사용할 꼭짓점
    public void Show(BattleUnitRuntime runtimeUnit) // 상태 이상 상세 표시
    { // 상세 표시 시작
        EnsureVisualStructure(); // 툴팁 화면 구조 준비
        if (runtimeUnit == null || runtimeUnit.StatusEffects.Count < 1) // 표시 상태 존재 확인
        { // 표시 불가 처리 시작
            Hide(); // 툴팁 숨김
            return; // 상세 표시 중단
        } // 표시 불가 처리 종료
        StringBuilder detailBuilder = new StringBuilder(); // 상세 문자열 생성
        detailBuilder.AppendLine($"<b>{runtimeUnit.DisplayName} 상태 상세</b>"); // 유닛 이름 제목 추가
        for (int effectIndex = 0; effectIndex < runtimeUnit.StatusEffects.Count; effectIndex++) // 상태 이상 목록 순회
        { // 개별 상태 상세 생성 시작
            BattleStatusEffectInstance statusEffect = runtimeUnit.StatusEffects[effectIndex]; // 현재 상태 이상 조회
            string colorCode = statusEffect.IsDebuff ? "#FF7777" : "#72E69A"; // 버프와 디버프 색상 선택
            string categoryLabel = statusEffect.IsDebuff ? "디버프" : "버프"; // 상태 분류 문구 선택
            detailBuilder.AppendLine($"<color={colorCode}>[{statusEffect.IconLabel}] {statusEffect.DisplayName} · {categoryLabel}</color>"); // 상태 이름과 분류 추가
            detailBuilder.AppendLine($"{statusEffect.Description} · {statusEffect.StackCount}/{statusEffect.MaximumStacks}중첩 · {statusEffect.RemainingTurns}T"); // 상태 효과와 수치 추가
        } // 개별 상태 상세 생성 종료
        detailText.text = detailBuilder.ToString().TrimEnd(); // 상태 상세 텍스트 적용
        tooltipRoot.SetActive(true); // 툴팁 표시
        tooltipRoot.transform.SetAsLastSibling(); // 툴팁 최상단 배치
        ((RectTransform)tooltipRoot.transform).anchoredPosition = new Vector2(8f, 0f); // 새 표시 시 유닛 오른쪽 상세 위치 복원
        KeepWithinCanvas(); // 첫 표시 프레임부터 화면 범위 보정
    } // 상세 표시 종료
    public void Hide() // 상태 이상 상세 숨김
    { // 상세 숨김 시작
        if (tooltipRoot != null) // 툴팁 화면 존재 확인
        { // 툴팁 숨김 처리 시작
            tooltipRoot.SetActive(false); // 툴팁 비활성화
        } // 툴팁 숨김 처리 종료
    } // 상세 숨김 종료
    private void LateUpdate() // 화면 크기와 유닛 위치에 따른 상세 범위 보정
    { // 툴팁 위치 갱신 시작
        if (tooltipRoot != null && tooltipRoot.activeSelf) // 표시 중인 상태 상세 확인
        { // 상세 위치 보정 시작
            KeepWithinCanvas(); // 현재 캔버스 안쪽으로 상세 위치 이동
        } // 상세 위치 보정 종료
    } // 툴팁 위치 갱신 종료
    private void KeepWithinCanvas() // 상태 상세창의 화면 잘림 방지
    { // 화면 범위 보정 시작
        Canvas canvas = GetComponentInParent<Canvas>(); // 부모 캔버스 조회
        RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null; // 최상위 화면 영역 조회
        RectTransform tooltipRect = tooltipRoot.transform as RectTransform; // 상태 상세 영역 조회
        if (canvasRect == null || tooltipRect == null || tooltipRect.parent == null) // 필수 화면 영역 확인
        { // 화면 범위 누락 처리 시작
            return; // 위치 보정 중단
        } // 화면 범위 누락 처리 종료
        tooltipRect.GetWorldCorners(canvasCorners); // 상세 영역의 실제 화면 위치 조회
        Vector3 minimum = canvasRect.InverseTransformPoint(canvasCorners[0]); // 상세 왼쪽 아래 화면 좌표 변환
        Vector3 maximum = canvasRect.InverseTransformPoint(canvasCorners[2]); // 상세 오른쪽 위 화면 좌표 변환
        float deltaX = minimum.x < canvasRect.rect.xMin ? canvasRect.rect.xMin - minimum.x : maximum.x > canvasRect.rect.xMax ? canvasRect.rect.xMax - maximum.x : 0f; // 화면 좌우 잘림 보정량 계산
        float deltaY = minimum.y < canvasRect.rect.yMin ? canvasRect.rect.yMin - minimum.y : maximum.y > canvasRect.rect.yMax ? canvasRect.rect.yMax - maximum.y : 0f; // 화면 상하 잘림 보정량 계산
        if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f)) // 화면 안쪽 위치 유지 확인
        { // 추가 이동 불필요 처리 시작
            return; // 불필요한 화면 재배치 중단
        } // 추가 이동 불필요 처리 종료
        Vector3 worldDelta = canvasRect.TransformVector(new Vector3(deltaX, deltaY, 0f)); // 화면 보정량을 실제 좌표로 변환
        Vector3 localDelta = tooltipRect.parent.InverseTransformVector(worldDelta); // 실제 보정량을 유닛 기준 좌표로 변환
        tooltipRect.anchoredPosition += new Vector2(localDelta.x, localDelta.y); // 화면 안쪽으로 상세 위치 이동
    } // 화면 범위 보정 종료
    private void EnsureVisualStructure() // 툴팁 화면 구조 준비
    { // 화면 구조 준비 시작
        if (tooltipRoot != null) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        tooltipRoot = new GameObject("StatusEffectTooltip", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup)); // 툴팁 배경 오브젝트 생성
        tooltipRoot.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        Canvas tooltipCanvas = tooltipRoot.GetComponent<Canvas>(); // 툴팁 전용 Canvas 조회
        tooltipCanvas.overrideSorting = true; // 상위 Canvas 정렬 순서 분리
        tooltipCanvas.sortingOrder = 100; // 툴팁 최상단 정렬 순서 적용
        RectTransform rootRect = tooltipRoot.GetComponent<RectTransform>(); // 툴팁 배경 사각형 조회
        rootRect.anchorMin = new Vector2(1f, 0.5f); // 오른쪽 중앙 최소 앵커
        rootRect.anchorMax = new Vector2(1f, 0.5f); // 오른쪽 중앙 최대 앵커
        rootRect.pivot = new Vector2(0f, 0.5f); // 왼쪽 중앙 기준점
        rootRect.anchoredPosition = new Vector2(8f, 0f); // 유닛 오른쪽 위치
        rootRect.sizeDelta = new Vector2(360f, 180f); // 읽기 쉬운 상세 툴팁 크기
        Image backgroundImage = tooltipRoot.GetComponent<Image>(); // 툴팁 배경 이미지 조회
        backgroundImage.color = new Color(0.025f, 0.035f, 0.06f, 0.98f); // 툴팁 배경색 적용
        backgroundImage.raycastTarget = false; // 툴팁 입력 차단 해제
        CanvasGroup canvasGroup = tooltipRoot.GetComponent<CanvasGroup>(); // 툴팁 입력 제어 조회
        canvasGroup.interactable = false; // 툴팁 상호작용 해제
        canvasGroup.blocksRaycasts = false; // 툴팁 레이캐스트 해제
        GameObject textObject = new GameObject("DetailText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 상세 텍스트 오브젝트 생성
        textObject.transform.SetParent(tooltipRoot.transform, false); // 툴팁 배경 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 상세 텍스트 사각형 조회
        textRect.anchorMin = Vector2.zero; // 상세 텍스트 최소 앵커
        textRect.anchorMax = Vector2.one; // 상세 텍스트 최대 앵커
        textRect.offsetMin = new Vector2(10f, 8f); // 상세 텍스트 왼쪽 아래 여백
        textRect.offsetMax = new Vector2(-10f, -8f); // 상세 텍스트 오른쪽 위 여백
        detailText = textObject.GetComponent<TextMeshProUGUI>(); // 상세 텍스트 컴포넌트 조회
        detailText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        detailText.fontSize = 16f; // 읽기 쉬운 상세 글자 크기
        detailText.color = new Color(0.9f, 0.93f, 1f, 1f); // 상세 기본 글자색 적용
        detailText.alignment = TextAlignmentOptions.TopLeft; // 상세 글자 왼쪽 위 정렬
        detailText.textWrappingMode = TextWrappingModes.Normal; // 상세 자동 줄바꿈 허용
        detailText.overflowMode = TextOverflowModes.Ellipsis; // 넘친 상세 생략 설정
        detailText.richText = true; // 상세 색상 태그 허용
        detailText.raycastTarget = false; // 상세 텍스트 입력 차단 해제
        tooltipRoot.SetActive(false); // 툴팁 기본 숨김
    } // 화면 구조 준비 종료
    private void OnDisable() // 화면 비활성화 처리
    { // 비활성화 처리 시작
        Hide(); // 툴팁 숨김
    } // 비활성화 처리 종료
} // 클래스 종료
