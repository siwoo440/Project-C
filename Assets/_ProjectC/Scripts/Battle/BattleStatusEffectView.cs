using System.Text; // 문자열 조합 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleStatusEffectView : MonoBehaviour // 유닛 상태 이상 화면 표시
{ // 클래스 시작
    private GameObject statusRoot; // 상태 이상 표시 루트
    private TMP_Text statusText; // 상태 이상 요약 텍스트
    private BattleStatusEffectTooltipView tooltipView; // 상태 이상 상세 툴팁
    private BattleUnitRuntime runtimeUnit; // 연결된 런타임 유닛
    private bool tooltipVisible; // 상세 툴팁 표시 여부
    public void Bind(BattleUnitRuntime unit) // 런타임 유닛 연결
    { // 연결 시작
        Unbind(); // 기존 연결 해제
        EnsureVisualStructure(); // 상태 화면 구조 준비
        runtimeUnit = unit; // 새 런타임 유닛 저장
        if (runtimeUnit != null) // 런타임 유닛 존재 확인
        { // 이벤트 등록 시작
            runtimeUnit.StatusEffectsChanged += HandleStatusEffectsChanged; // 상태 변경 이벤트 등록
        } // 이벤트 등록 종료
        Refresh(); // 상태 화면 갱신
    } // 연결 종료
    public void Unbind() // 런타임 유닛 연결 해제
    { // 연결 해제 시작
        if (runtimeUnit != null) // 기존 런타임 유닛 확인
        { // 이벤트 해제 시작
            runtimeUnit.StatusEffectsChanged -= HandleStatusEffectsChanged; // 상태 변경 이벤트 해제
        } // 이벤트 해제 종료
        runtimeUnit = null; // 런타임 참조 제거
        tooltipVisible = false; // 상세 툴팁 표시 상태 해제
        tooltipView?.Hide(); // 상세 툴팁 숨김
        if (statusRoot != null) // 상태 화면 존재 확인
        { // 상태 화면 숨김 시작
            statusRoot.SetActive(false); // 상태 화면 비활성화
        } // 상태 화면 숨김 종료
    } // 연결 해제 종료
    public void ShowTooltip() // 상태 이상 상세 툴팁 표시
    { // 툴팁 표시 시작
        EnsureTooltipView(); // 상세 툴팁 준비
        tooltipVisible = runtimeUnit != null && runtimeUnit.StatusEffects.Count > 0; // 표시 가능 상태 저장
        if (tooltipVisible) // 툴팁 표시 가능 확인
        { // 툴팁 표시 처리 시작
            tooltipView.Show(runtimeUnit); // 현재 상태 상세 표시
        } // 툴팁 표시 처리 종료
    } // 툴팁 표시 종료
    public void HideTooltip() // 상태 이상 상세 툴팁 숨김
    { // 툴팁 숨김 시작
        tooltipVisible = false; // 툴팁 표시 상태 해제
        tooltipView?.Hide(); // 상세 툴팁 숨김
    } // 툴팁 숨김 종료
    private void HandleStatusEffectsChanged(BattleUnitRuntime changedUnit) // 상태 이상 변경 처리
    { // 상태 변경 처리 시작
        if (changedUnit != runtimeUnit) // 연결 유닛 확인
        { // 다른 유닛 처리 시작
            return; // 상태 변경 무시
        } // 다른 유닛 처리 종료
        Refresh(); // 상태 화면 갱신
    } // 상태 변경 처리 종료
    private void Refresh() // 상태 이상 요약 갱신
    { // 요약 갱신 시작
        if (statusRoot == null || statusText == null) // 상태 화면 구조 확인
        { // 화면 없음 처리 시작
            return; // 요약 갱신 중단
        } // 화면 없음 처리 종료
        if (runtimeUnit == null || runtimeUnit.StatusEffects.Count < 1) // 표시 상태 존재 확인
        { // 상태 없음 처리 시작
            statusRoot.SetActive(false); // 상태 화면 숨김
            HideTooltip(); // 상태 상세 툴팁 숨김
            return; // 요약 갱신 종료
        } // 상태 없음 처리 종료
        StringBuilder summaryBuilder = new StringBuilder(); // 상태 요약 문자열 생성
        for (int effectIndex = 0; effectIndex < runtimeUnit.StatusEffects.Count; effectIndex++) // 상태 목록 순회
        { // 상태 문구 생성 시작
            BattleStatusEffectInstance statusEffect = runtimeUnit.StatusEffects[effectIndex]; // 현재 상태 조회
            if (effectIndex > 0) // 첫 상태 여부 확인
            { // 상태 구분 처리 시작
                summaryBuilder.Append("  "); // 상태 사이 여백 추가
            } // 상태 구분 처리 종료
            string colorCode = statusEffect.IsDebuff ? "#FF7777" : "#72E69A"; // 버프와 디버프 색상 선택
            summaryBuilder.Append($"<color={colorCode}>[{statusEffect.IconLabel}]x{statusEffect.StackCount} {statusEffect.RemainingTurns}T</color>"); // 아이콘형 상태 요약 추가
        } // 상태 문구 생성 종료
        statusText.text = summaryBuilder.ToString(); // 상태 요약 텍스트 적용
        statusRoot.SetActive(true); // 상태 화면 표시
        statusRoot.transform.SetAsLastSibling(); // 상태 화면 최상단 배치
        if (tooltipVisible) // 기존 툴팁 표시 여부 확인
        { // 툴팁 내용 갱신 시작
            tooltipView.Show(runtimeUnit); // 변경된 상태 상세 갱신
        } // 툴팁 내용 갱신 종료
    } // 요약 갱신 종료
    private void EnsureVisualStructure() // 상태 화면 구조 준비
    { // 화면 구조 준비 시작
        if (statusRoot != null) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        statusRoot = new GameObject("StatusEffects", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 상태 배경 오브젝트 생성
        statusRoot.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        RectTransform rootRect = statusRoot.GetComponent<RectTransform>(); // 상태 배경 사각형 조회
        rootRect.anchorMin = new Vector2(0f, 0f); // 왼쪽 아래 최소 앵커
        rootRect.anchorMax = new Vector2(1f, 0f); // 오른쪽 아래 최대 앵커
        rootRect.pivot = new Vector2(0.5f, 0f); // 아래 중앙 기준점
        rootRect.anchoredPosition = new Vector2(0f, 3f); // 유닛 하단 위치
        rootRect.sizeDelta = new Vector2(-8f, 24f); // 상태 표시 크기
        Image backgroundImage = statusRoot.GetComponent<Image>(); // 상태 배경 이미지 조회
        backgroundImage.color = new Color(0.025f, 0.035f, 0.055f, 0.94f); // 상태 배경색 적용
        backgroundImage.raycastTarget = false; // 상태 배경 입력 차단 해제
        GameObject textObject = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 상태 텍스트 오브젝트 생성
        textObject.transform.SetParent(statusRoot.transform, false); // 상태 배경 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 상태 텍스트 사각형 조회
        textRect.anchorMin = Vector2.zero; // 텍스트 최소 앵커 적용
        textRect.anchorMax = Vector2.one; // 텍스트 최대 앵커 적용
        textRect.offsetMin = new Vector2(3f, 1f); // 텍스트 왼쪽 아래 여백
        textRect.offsetMax = new Vector2(-3f, -1f); // 텍스트 오른쪽 위 여백
        statusText = textObject.GetComponent<TextMeshProUGUI>(); // 상태 텍스트 컴포넌트 조회
        statusText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        statusText.fontSize = 10f; // 상태 글자 크기
        statusText.color = Color.white; // 기본 글자색 적용
        statusText.alignment = TextAlignmentOptions.Center; // 상태 글자 중앙 정렬
        statusText.textWrappingMode = TextWrappingModes.NoWrap; // 상태 자동 줄바꿈 해제
        statusText.overflowMode = TextOverflowModes.Ellipsis; // 넘친 상태 생략 설정
        statusText.richText = true; // 상태 색상 태그 허용
        statusText.raycastTarget = false; // 상태 텍스트 입력 차단 해제
        statusRoot.SetActive(false); // 상태 화면 기본 숨김
    } // 화면 구조 준비 종료
    private void EnsureTooltipView() // 상태 상세 툴팁 준비
    { // 툴팁 준비 시작
        if (tooltipView != null) // 기존 툴팁 확인
        { // 기존 툴팁 처리 시작
            return; // 툴팁 생성 중단
        } // 기존 툴팁 처리 종료
        tooltipView = GetComponent<BattleStatusEffectTooltipView>(); // 기존 툴팁 컴포넌트 조회
        if (tooltipView == null) // 툴팁 컴포넌트 누락 확인
        { // 툴팁 컴포넌트 추가 시작
            tooltipView = gameObject.AddComponent<BattleStatusEffectTooltipView>(); // 런타임 툴팁 컴포넌트 추가
        } // 툴팁 컴포넌트 추가 종료
    } // 툴팁 준비 종료
    private void OnDestroy() // 오브젝트 제거 처리
    { // 제거 처리 시작
        Unbind(); // 런타임 이벤트 연결 해제
    } // 제거 처리 종료
} // 클래스 종료
