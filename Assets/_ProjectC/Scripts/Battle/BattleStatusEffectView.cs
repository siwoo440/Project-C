using System.Collections.Generic; // 상태 아이콘 재사용 목록 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleStatusEffectView : MonoBehaviour // 유닛 상태 이상 화면 표시
{ // 클래스 시작
    private GameObject statusRoot; // 상태 이상 표시 루트
    private readonly List<BattleStatusIconView> iconViews = new List<BattleStatusIconView>(); // 재사용할 상태 아이콘 목록
    private float lastLayoutWidth = -1f; // 마지막 상태 아이콘 배치 너비
    private BattleStatusEffectTooltipView tooltipView; // 상태 이상 상세 툴팁
    private BattleUnitRuntime runtimeUnit; // 연결된 런타임 유닛
    private bool tooltipVisible; // 상세 툴팁 표시 여부
    public bool IsTooltipVisible => tooltipVisible; // 현재 상세 툴팁 표시 상태 조회
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
        if (statusRoot == null) // 상태 화면 구조 확인
        { // 화면 없음 처리 시작
            return; // 요약 갱신 중단
        } // 화면 없음 처리 종료
        if (runtimeUnit == null || runtimeUnit.StatusEffects.Count < 1) // 표시 상태 존재 확인
        { // 상태 없음 처리 시작
            statusRoot.SetActive(false); // 상태 화면 숨김
            HideTooltip(); // 상태 상세 툴팁 숨김
            return; // 요약 갱신 종료
        } // 상태 없음 처리 종료
        for (int effectIndex = 0; effectIndex < runtimeUnit.StatusEffects.Count; effectIndex++) // 상태 목록 순회
        { // 상태 아이콘 갱신 시작
            BattleStatusEffectInstance statusEffect = runtimeUnit.StatusEffects[effectIndex]; // 현재 상태 조회
            if (effectIndex >= iconViews.Count) // 재사용 가능한 아이콘 존재 확인
            { // 새 상태 아이콘 생성 시작
                iconViews.Add(BattleStatusIconView.Create(statusRoot.transform, effectIndex)); // 새 정사각형 상태 아이콘 생성
            } // 새 상태 아이콘 생성 종료
            iconViews[effectIndex].Bind(statusEffect); // 실제 상태 이미지와 수치 갱신
        } // 상태 아이콘 갱신 종료
        for (int index = runtimeUnit.StatusEffects.Count; index < iconViews.Count; index++) // 사용하지 않는 아이콘 순회
        { // 남은 아이콘 숨김 시작
            iconViews[index].gameObject.SetActive(false); // 사라진 상태 아이콘 숨김
        } // 남은 아이콘 숨김 종료
        statusRoot.SetActive(true); // 상태 화면 표시
        statusRoot.transform.SetAsLastSibling(); // 상태 화면 최상단 배치
        LayoutIcons(); // 현재 화면 너비에 맞춰 상태 아이콘 배치
        if (tooltipVisible) // 기존 툴팁 표시 여부 확인
        { // 툴팁 내용 갱신 시작
            tooltipView.Show(runtimeUnit); // 변경된 상태 상세 갱신
        } // 툴팁 내용 갱신 종료
    } // 요약 갱신 종료
    private void LateUpdate() // 상태 아이콘 화면 크기 변화 감시
    { // 상태 배치 갱신 시작
        if (statusRoot != null && statusRoot.activeSelf && !Mathf.Approximately(lastLayoutWidth, ((RectTransform)statusRoot.transform).rect.width)) // 상태 영역 너비 변경 확인
        { // 상태 아이콘 재배치 시작
            LayoutIcons(); // 변경된 화면 너비에 맞춰 상태 아이콘 배치
        } // 상태 아이콘 재배치 종료
    } // 상태 배치 갱신 종료
    private void LayoutIcons() // 상태 아이콘을 정사각형 여러 줄로 배치
    { // 상태 아이콘 배치 시작
        if (runtimeUnit == null || statusRoot == null || runtimeUnit.StatusEffects.Count == 0) // 실제 표시 상태 확인
        { // 배치 불가 처리 시작
            return; // 상태 아이콘 배치 중단
        } // 배치 불가 처리 종료
        RectTransform rootRect = (RectTransform)statusRoot.transform; // 상태 아이콘 영역 조회
        float width = Mathf.Max(34f, rootRect.rect.width); // 아이콘을 배치할 실제 너비 계산
        int naturalColumns = Mathf.Max(1, Mathf.FloorToInt((width + 2f) / 36f)); // 기본 크기 기준 가로 칸 수 계산
        int columns = Mathf.Min(runtimeUnit.StatusEffects.Count, Mathf.Max(naturalColumns, (runtimeUnit.StatusEffects.Count + 1) / 2)); // 모든 상태를 최대 두 줄에 표시할 가로 칸 수
        float size = Mathf.Min(34f, (width - (columns - 1) * 2f) / columns); // 화면 너비에 맞춘 정사각형 아이콘 크기 계산
        int rows = (runtimeUnit.StatusEffects.Count + columns - 1) / columns; // 필요한 상태 아이콘 줄 수 계산
        rootRect.sizeDelta = new Vector2(-8f, rows * (size + 2f) - 2f); // 상태 줄 수에 맞춘 아래 방향 영역 높이 설정
        for (int index = 0; index < runtimeUnit.StatusEffects.Count; index++) // 현재 상태 아이콘 순회
        { // 개별 상태 아이콘 배치 시작
            RectTransform iconRect = (RectTransform)iconViews[index].transform; // 개별 상태 아이콘 사각형 조회
            iconRect.sizeDelta = new Vector2(size, size); // 동일한 가로와 세로 크기 적용
            iconRect.anchoredPosition = new Vector2(index % columns * (size + 2f), -(index / columns) * (size + 2f)); // 왼쪽 위부터 일정 간격 배치
        } // 개별 상태 아이콘 배치 종료
        lastLayoutWidth = rootRect.rect.width; // 현재 화면 너비 기록
    } // 상태 아이콘 배치 종료
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
        rootRect.pivot = new Vector2(0.5f, 1f); // 위쪽 중앙 기준점
        rootRect.anchoredPosition = new Vector2(0f, 72f); // 정신력 바 바로 아래 아이콘 위치
        rootRect.sizeDelta = new Vector2(-8f, 34f); // 기본 상태 아이콘 영역 크기
        Image backgroundImage = statusRoot.GetComponent<Image>(); // 상태 배경 이미지 조회
        backgroundImage.color = Color.clear; // 개별 아이콘만 표시하는 투명 배경
        backgroundImage.raycastTarget = false; // 상태 배경 입력 차단 해제
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
