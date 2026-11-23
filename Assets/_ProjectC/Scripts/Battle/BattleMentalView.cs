using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleMentalView : MonoBehaviour // 유닛 정신력 게이지 화면
{ // 클래스 시작
    private static readonly Color CollapseColor = new Color(0.88f, 0.14f, 0.14f, 1f); // 붕괴 표시 색상
    private static readonly Color NeutralColor = new Color(0.92f, 0.92f, 0.92f, 1f); // 중립 표시 색상
    private static readonly Color AwakeningColor = new Color(0.13f, 0.48f, 0.98f, 1f); // 높은 정신력의 파란색
    private BattleUnitRuntime runtimeUnit; // 연결된 런타임 유닛
    private GameObject viewRoot; // 정신력 화면 루트
    private RectTransform gaugeFillRect; // 정신력 채움 사각형
    private Image gaugeFillImage; // 정신력 채움 이미지
    private Image gaugeTrackImage; // 정신력 색상을 표시하는 바탕 이미지
    private TMP_Text mentalText; // 정신력 수치 텍스트
    public void Bind(BattleUnitRuntime targetUnit) // 런타임 유닛 연결
    { // 연결 시작
        Unbind(); // 기존 연결 해제
        runtimeUnit = targetUnit; // 새 유닛 저장
        if (runtimeUnit == null) // 새 유닛 존재 확인
        { // 유닛 없음 처리 시작
            return; // 연결 중단
        } // 유닛 없음 처리 종료
        EnsureView(); // 정신력 화면 준비
        runtimeUnit.MentalChanged += HandleMentalChanged; // 정신력 변화 이벤트 등록
        Refresh(); // 정신력 표시 갱신
    } // 연결 종료
    public void Unbind() // 런타임 유닛 연결 해제
    { // 연결 해제 시작
        if (runtimeUnit != null) // 기존 유닛 확인
        { // 기존 유닛 처리 시작
            runtimeUnit.MentalChanged -= HandleMentalChanged; // 정신력 변화 이벤트 해제
        } // 기존 유닛 처리 종료
        runtimeUnit = null; // 유닛 참조 제거
    } // 연결 해제 종료
    private void EnsureView() // 정신력 화면 생성
    { // 화면 생성 시작
        if (viewRoot != null) // 기존 화면 확인
        { // 기존 화면 처리 시작
            return; // 중복 생성 중단
        } // 기존 화면 처리 종료
        viewRoot = new GameObject("MentalView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 정신력 배경 생성
        viewRoot.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        RectTransform rootRect = viewRoot.GetComponent<RectTransform>(); // 배경 사각형 조회
        rootRect.anchorMin = new Vector2(0f, 0f); // 왼쪽 아래 최소 앵커
        rootRect.anchorMax = new Vector2(1f, 0f); // 오른쪽 아래 최대 앵커
        rootRect.pivot = new Vector2(0.5f, 0f); // 아래 중앙 기준점
        rootRect.anchoredPosition = new Vector2(0f, 30f); // 상태 아이콘 위 위치
        rootRect.sizeDelta = new Vector2(-8f, 20f); // 정신력 게이지 크기
        Image backgroundImage = viewRoot.GetComponent<Image>(); // 배경 이미지 조회
        backgroundImage.color = new Color(0.04f, 0.04f, 0.06f, 0.9f); // 어두운 배경색 적용
        BattleUiArt.Apply(backgroundImage, "GaugeFrame"); // 새로 제작한 정신력 게이지 테두리 적용
        backgroundImage.raycastTarget = false; // 배경 클릭 차단 해제
        GameObject trackObject = new GameObject("GaugeTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 정신력 바탕 생성
        trackObject.transform.SetParent(viewRoot.transform, false); // 바탕을 테두리 안쪽에 배치
        RectTransform trackRect = trackObject.GetComponent<RectTransform>(); // 바탕 사각형 조회
        trackRect.anchorMin = Vector2.zero; // 바탕 왼쪽 아래 앵커
        trackRect.anchorMax = Vector2.one; // 바탕 오른쪽 위 앵커
        trackRect.offsetMin = new Vector2(5f, 4f); // 금속 테두리 안쪽 시작 여백
        trackRect.offsetMax = new Vector2(-5f, -4f); // 금속 테두리 안쪽 끝 여백
        gaugeTrackImage = trackObject.GetComponent<Image>(); // 바탕 이미지 조회
        gaugeTrackImage.raycastTarget = false; // 바탕 클릭 차단 해제
        GameObject fillObject = new GameObject("GaugeFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 정신력 채움 생성
        fillObject.transform.SetParent(viewRoot.transform, false); // 채움 배경 자식 배치
        gaugeFillRect = fillObject.GetComponent<RectTransform>(); // 채움 사각형 조회
        gaugeFillRect.anchorMin = new Vector2(0f, 0f); // 채움 왼쪽 아래 앵커
        gaugeFillRect.anchorMax = new Vector2(0.5f, 1f); // 초기 절반 채움 앵커
        gaugeFillRect.offsetMin = new Vector2(5f, 4f); // 금속 테두리 안쪽 채움 시작 여백
        gaugeFillRect.offsetMax = new Vector2(-5f, -4f); // 금속 테두리 안쪽 채움 끝 여백
        gaugeFillImage = fillObject.GetComponent<Image>(); // 채움 이미지 조회
        gaugeFillImage.color = NeutralColor; // 초기 중립 색상 적용
        gaugeFillImage.raycastTarget = false; // 채움 클릭 차단 해제
        GameObject centerObject = new GameObject("CenterMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 중앙 기준선 생성
        centerObject.transform.SetParent(viewRoot.transform, false); // 중앙선 배경 자식 배치
        RectTransform centerRect = centerObject.GetComponent<RectTransform>(); // 중앙선 사각형 조회
        centerRect.anchorMin = new Vector2(0.5f, 0f); // 중앙 아래 앵커
        centerRect.anchorMax = new Vector2(0.5f, 1f); // 중앙 위 앵커
        centerRect.sizeDelta = new Vector2(1f, -2f); // 중앙선 크기
        centerRect.anchoredPosition = Vector2.zero; // 중앙선 위치
        Image centerImage = centerObject.GetComponent<Image>(); // 중앙선 이미지 조회
        centerImage.color = new Color(1f, 1f, 1f, 0.65f); // 중앙선 색상 적용
        centerImage.raycastTarget = false; // 중앙선 클릭 차단 해제
        GameObject textObject = new GameObject("MentalText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 정신력 텍스트 생성
        textObject.transform.SetParent(viewRoot.transform, false); // 텍스트 배경 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 사각형 조회
        textRect.anchorMin = Vector2.zero; // 텍스트 최소 앵커
        textRect.anchorMax = Vector2.one; // 텍스트 최대 앵커
        textRect.offsetMin = Vector2.zero; // 텍스트 최소 여백
        textRect.offsetMax = Vector2.zero; // 텍스트 최대 여백
        mentalText = textObject.GetComponent<TextMeshProUGUI>(); // 정신력 텍스트 조회
        mentalText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        mentalText.fontSize = 11f; // 정신력 글자 크기
        mentalText.fontStyle = FontStyles.Bold; // 정신력 글자 굵기
        mentalText.alignment = TextAlignmentOptions.Center; // 정신력 글자 가운데 정렬
        mentalText.color = Color.black; // 정신력 글자 색상
        mentalText.raycastTarget = false; // 텍스트 클릭 차단 해제
        viewRoot.transform.SetAsLastSibling(); // 정신력 화면 최상단 배치
    } // 화면 생성 종료
    private void Refresh() // 정신력 표시 갱신
    { // 표시 갱신 시작
        if (runtimeUnit == null || viewRoot == null) // 연결 상태 확인
        { // 연결 없음 처리 시작
            return; // 갱신 중단
        } // 연결 없음 처리 종료
        float normalizedMental = runtimeUnit.CurrentMental / 100f; // 정신력 비율 계산
        gaugeFillRect.anchorMax = new Vector2(normalizedMental, 1f); // 정신력 채움 너비 적용
        Color mentalColor = GetMentalColor(runtimeUnit.CurrentMental); // 현재 수치에 따른 게이지 색상 계산
        gaugeFillImage.color = mentalColor; // 정신력 수치 색상 적용
        gaugeTrackImage.color = Color.Lerp(Color.black, mentalColor, 0.45f); // 빈 게이지에도 수치 방향 표시
        mentalText.text = GetMentalLabel(runtimeUnit); // 정신력 상태 문구 적용
        float brightness = mentalColor.r * 0.2126f + mentalColor.g * 0.7152f + mentalColor.b * 0.0722f; // 게이지 색상 밝기 계산
        mentalText.color = normalizedMental < 0.5f || brightness < 0.55f ? Color.white : Color.black; // 게이지 대비에 맞는 글자색 적용
        viewRoot.SetActive(!runtimeUnit.IsDead); // 생존 유닛 게이지 표시
    } // 표시 갱신 종료
    private void HandleMentalChanged(BattleUnitRuntime changedUnit, BattleMentalChangeResult changeResult) // 정신력 변화 처리
    { // 변화 처리 시작
        if (changedUnit == runtimeUnit) // 연결 유닛 확인
        { // 연결 유닛 처리 시작
            Refresh(); // 정신력 표시 갱신
        } // 연결 유닛 처리 종료
    } // 변화 처리 종료
    private static Color GetMentalColor(int mental) // 정신력 수치 색상 계산
    { // 색상 계산 시작
        if (mental <= BattleMentalRuntime.NeutralMental) // 중간 이하 정신력 확인
        { // 붉은색 보간 시작
            return Color.Lerp(CollapseColor, NeutralColor, Mathf.InverseLerp(BattleMentalRuntime.MinimumMental, BattleMentalRuntime.NeutralMental, mental)); // 빨강에서 중립색 보간 반환
        } // 붉은색 보간 종료
        return Color.Lerp(NeutralColor, AwakeningColor, Mathf.InverseLerp(BattleMentalRuntime.NeutralMental, BattleMentalRuntime.MaximumMental, mental)); // 중립색에서 파랑 보간 반환
    } // 색상 계산 종료
    private static string GetMentalLabel(BattleUnitRuntime targetUnit) // 정신력 문구 계산
    { // 문구 계산 시작
        if (targetUnit.MentalState == BattleMentalState.Awakening) // 각성 상태 확인
        { // 각성 문구 처리 시작
            return $"각성 {targetUnit.MentalRemainingTurns}T"; // 각성 남은 턴 반환
        } // 각성 문구 처리 종료
        if (targetUnit.MentalState == BattleMentalState.Collapse) // 붕괴 상태 확인
        { // 붕괴 문구 처리 시작
            return $"붕괴 {targetUnit.MentalRemainingTurns}T"; // 붕괴 남은 턴 반환
        } // 붕괴 문구 처리 종료
        return $"정신 {targetUnit.CurrentMental}"; // 일반 정신력 문구 반환
    } // 문구 계산 종료
    private void OnDestroy() // 화면 제거 처리
    { // 제거 처리 시작
        Unbind(); // 런타임 연결 해제
    } // 제거 처리 종료
} // 클래스 종료
