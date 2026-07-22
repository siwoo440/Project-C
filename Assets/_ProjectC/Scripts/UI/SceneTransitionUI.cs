using System.Collections; // 코루틴 열거자 기능
using TMPro; // TextMesh Pro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DefaultExecutionOrder(-800)] // 부트 씬 시작 전 실행 순서
public sealed class SceneTransitionUI : MonoBehaviour // 공용 씬 전환 화면 관리자
{
    [Header("화면 참조")] // 화면 참조 구분
    [SerializeField] private CanvasGroup canvasGroup; // 전체 로딩 화면 그룹
    [SerializeField] private Image progressFillImage; // 진행률 표시 이미지
    [SerializeField] private TMP_Text progressText; // 진행률 표시 문구

    [Header("전환 설정")] // 전환 설정 구분
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f; // 페이드 진행 시간

    private void Awake() // 공용 UI 초기화
    {
        HideImmediate(); // 로딩 화면 즉시 숨김
        SetProgress(0f); // 진행률 초기화
    }

    public IEnumerator FadeIn() // 로딩 화면 표시
    {
        canvasGroup.blocksRaycasts = true; // 하위 화면 입력 차단
        canvasGroup.interactable = false; // 로딩 화면 상호작용 차단
        yield return FadeCanvasGroup(1f); // 불투명 상태 전환 대기
    }

    public IEnumerator FadeOut() // 로딩 화면 숨김
    {
        yield return FadeCanvasGroup(0f); // 투명 상태 전환 대기
        canvasGroup.blocksRaycasts = false; // 하위 화면 입력 차단 해제
        canvasGroup.interactable = false; // 로딩 화면 상호작용 차단 유지
    }

    public void HideImmediate() // 로딩 화면 즉시 숨김
    {
        canvasGroup.alpha = 0f; // 전체 화면 투명 처리
        canvasGroup.blocksRaycasts = false; // 하위 화면 입력 허용
        canvasGroup.interactable = false; // 로딩 화면 상호작용 차단
    }

    public void SetProgress(float normalizedProgress) // 로딩 진행률 표시
    {
        float clampedProgress = Mathf.Clamp01(normalizedProgress); // 진행률 범위 제한
        progressFillImage.fillAmount = clampedProgress; // 진행률 이미지 적용
        int percent = Mathf.RoundToInt(clampedProgress * 100f); // 백분율 정수 변환
        progressText.text = $"{percent}%"; // 진행률 문구 적용
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha) // 화면 투명도 전환
    {
        float startAlpha = canvasGroup.alpha; // 시작 투명도 저장
        float elapsedTime = 0f; // 경과 시간 초기화

        if (fadeDuration <= 0f) // 즉시 전환 설정 확인
        {
            canvasGroup.alpha = targetAlpha; // 목표 투명도 즉시 적용
            yield break; // 페이드 처리 종료
        }

        while (elapsedTime < fadeDuration) // 페이드 시간 진행 확인
        {
            elapsedTime += Time.unscaledDeltaTime; // 실제 경과 시간 누적
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration); // 페이드 진행률 계산
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress); // 투명도 보간 적용
            yield return null; // 다음 프레임 대기
        }

        canvasGroup.alpha = targetAlpha; // 최종 투명도 보정
    }
}