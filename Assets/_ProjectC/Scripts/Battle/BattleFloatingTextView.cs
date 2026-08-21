using System.Collections; // 코루틴 자료형 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
public sealed class BattleFloatingTextView : MonoBehaviour // 전투 플로팅 숫자 표시
{ // 클래스 시작
    private const float DisplayDuration = 0.75f; // 표시 지속 시간
    private const float RiseDistance = 55f; // 상승 이동 거리
    private RectTransform rectTransform; // 숫자 위치 사각형
    private CanvasGroup canvasGroup; // 숫자 투명도 제어
    private Vector2 startPosition; // 숫자 시작 위치
    public static BattleFloatingTextView Create(Transform parent, string message, Color textColor, int spawnIndex) // 플로팅 숫자 생성
    { // 숫자 생성 시작
        GameObject textObject = new GameObject("FloatingCombatText", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(BattleFloatingTextView)); // 숫자 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 유닛 화면 자식 배치
        BattleFloatingTextView floatingView = textObject.GetComponent<BattleFloatingTextView>(); // 숫자 화면 컴포넌트 조회
        floatingView.Initialize(message, textColor, spawnIndex); // 숫자 화면 초기화
        return floatingView; // 생성 숫자 화면 반환
    } // 숫자 생성 종료
    private void Initialize(string message, Color textColor, int spawnIndex) // 숫자 화면 초기화
    { // 초기화 시작
        rectTransform = GetComponent<RectTransform>(); // 숫자 사각형 조회
        canvasGroup = GetComponent<CanvasGroup>(); // 숫자 투명도 제어 조회
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 최소 앵커 적용
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 최대 앵커 적용
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // 중앙 기준점 적용
        rectTransform.sizeDelta = new Vector2(150f, 45f); // 숫자 표시 크기
        float horizontalOffset = ((spawnIndex % 3) - 1) * 24f; // 연속 숫자 가로 위치 계산
        float verticalOffset = (spawnIndex % 2) * 10f; // 연속 숫자 세로 위치 계산
        startPosition = new Vector2(horizontalOffset, 30f + verticalOffset); // 숫자 시작 위치 저장
        rectTransform.anchoredPosition = startPosition; // 숫자 시작 위치 적용
        TextMeshProUGUI valueText = GetComponent<TextMeshProUGUI>(); // 숫자 텍스트 조회
        valueText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        valueText.text = message; // 숫자 내용 적용
        valueText.fontSize = 28f; // 숫자 글자 크기 적용
        valueText.fontStyle = FontStyles.Bold; // 숫자 굵게 적용
        valueText.color = textColor; // 효과별 숫자 색상 적용
        valueText.alignment = TextAlignmentOptions.Center; // 숫자 가운데 정렬
        valueText.textWrappingMode = TextWrappingModes.NoWrap; // 숫자 자동 줄바꿈 해제
        valueText.raycastTarget = false; // 숫자 클릭 차단 해제
        canvasGroup.alpha = 1f; // 숫자 불투명도 초기화
        canvasGroup.interactable = false; // 숫자 상호작용 해제
        canvasGroup.blocksRaycasts = false; // 숫자 레이캐스트 해제
        transform.SetAsLastSibling(); // 숫자 최상단 배치
        StartCoroutine(Animate()); // 숫자 상승 애니메이션 시작
    } // 초기화 종료
    private IEnumerator Animate() // 숫자 상승과 페이드 처리
    { // 애니메이션 시작
        float elapsedTime = 0f; // 경과 시간 초기화
        while (elapsedTime < DisplayDuration) // 표시 시간 반복
        { // 프레임 처리 시작
            float normalizedTime = Mathf.Clamp01(elapsedTime / DisplayDuration); // 진행 비율 계산
            rectTransform.anchoredPosition = startPosition + Vector2.up * Mathf.Lerp(0f, RiseDistance, normalizedTime); // 숫자 상승 위치 적용
            canvasGroup.alpha = 1f - normalizedTime; // 숫자 투명도 적용
            elapsedTime += Time.unscaledDeltaTime; // 실제 시간 누적
            yield return null; // 다음 프레임 대기
        } // 프레임 처리 종료
        Destroy(gameObject); // 표시 완료 숫자 제거
    } // 애니메이션 종료
} // 클래스 종료
