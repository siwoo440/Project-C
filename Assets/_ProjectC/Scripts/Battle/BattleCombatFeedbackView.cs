using System.Collections; // 코루틴 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleCombatFeedbackView : MonoBehaviour // 전투 결과 시각 피드백
{ // 클래스 시작
    private const float FlashDuration = 0.18f; // 강조 표시 지속 시간
    private Image flashOverlay; // 강조 색상 오버레이
    private Coroutine flashCoroutine; // 실행 중인 강조 코루틴
    private int floatingSpawnIndex; // 플로팅 숫자 위치 순번
    public void ShowDamage(BattleDamageResult damageResult) // 피해 피드백 표시
    { // 피해 피드백 시작
        if (!damageResult.HasDamage) // 유효 피해 확인
        { // 피해 없음 처리 시작
            return; // 피해 피드백 중단
        } // 피해 없음 처리 종료
        Color feedbackColor = SelectDamageColor(damageResult.DamageType); // 피해 유형별 색상 선택
        PlayFlash(feedbackColor); // 피격 강조 표시
        CreateFloatingText($"-{damageResult.AppliedDamage}", feedbackColor); // 피해 플로팅 숫자 생성
    } // 피해 피드백 종료
    public void ShowHealing(int appliedHealing) // 회복 피드백 표시
    { // 회복 피드백 시작
        if (appliedHealing <= 0) // 유효 회복 확인
        { // 회복 없음 처리 시작
            return; // 회복 피드백 중단
        } // 회복 없음 처리 종료
        Color healingColor = new Color(0.35f, 1f, 0.5f, 1f); // 회복 색상 생성
        PlayFlash(healingColor); // 회복 강조 표시
        CreateFloatingText($"+{appliedHealing}", healingColor); // 회복 플로팅 숫자 생성
    } // 회복 피드백 종료
    public void ShowStatusMessage(string message, bool isDebuff) // 상태 이상 문구 피드백 표시
    { // 상태 문구 피드백 시작
        if (string.IsNullOrWhiteSpace(message)) // 표시 문구 확인
        { // 문구 없음 처리 시작
            return; // 상태 문구 표시 중단
        } // 문구 없음 처리 종료
        Color statusColor = isDebuff ? new Color(1f, 0.38f, 0.45f, 1f) : new Color(0.4f, 0.95f, 0.68f, 1f); // 버프와 디버프 색상 선택
        PlayFlash(statusColor); // 상태 적용 강조 표시
        CreateFloatingText(message, statusColor); // 상태 결과 플로팅 문구 생성
    } // 상태 문구 피드백 종료
    private void CreateFloatingText(string message, Color textColor) // 플로팅 숫자 생성
    { // 숫자 생성 시작
        BattleFloatingTextView.Create(transform, message, textColor, floatingSpawnIndex); // 유닛 내부 숫자 생성
        floatingSpawnIndex = (floatingSpawnIndex + 1) % 6; // 다음 숫자 위치 순번 갱신
    } // 숫자 생성 종료
    private void PlayFlash(Color feedbackColor) // 강조 색상 재생
    { // 강조 재생 시작
        EnsureFlashOverlay(); // 강조 오버레이 준비
        if (flashCoroutine != null) // 기존 강조 실행 확인
        { // 기존 강조 중단 시작
            StopCoroutine(flashCoroutine); // 기존 강조 코루틴 중단
        } // 기존 강조 중단 종료
        flashCoroutine = StartCoroutine(AnimateFlash(feedbackColor)); // 새 강조 코루틴 시작
    } // 강조 재생 종료
    private void EnsureFlashOverlay() // 강조 오버레이 준비
    { // 오버레이 준비 시작
        if (flashOverlay != null) // 기존 오버레이 확인
        { // 기존 오버레이 처리 시작
            return; // 중복 생성 중단
        } // 기존 오버레이 처리 종료
        GameObject overlayObject = new GameObject("CombatFlashOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 강조 오버레이 생성
        overlayObject.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>(); // 오버레이 사각형 조회
        overlayRect.anchorMin = Vector2.zero; // 전체 영역 최소 앵커
        overlayRect.anchorMax = Vector2.one; // 전체 영역 최대 앵커
        overlayRect.offsetMin = new Vector2(6f, 6f); // 오버레이 왼쪽 아래 여백
        overlayRect.offsetMax = new Vector2(-6f, -6f); // 오버레이 오른쪽 위 여백
        flashOverlay = overlayObject.GetComponent<Image>(); // 강조 오버레이 이미지 조회
        flashOverlay.color = Color.clear; // 오버레이 투명 상태 초기화
        flashOverlay.raycastTarget = false; // 오버레이 클릭 차단 해제
        overlayObject.SetActive(false); // 오버레이 기본 숨김
    } // 오버레이 준비 종료
    private IEnumerator AnimateFlash(Color feedbackColor) // 강조 페이드 애니메이션
    { // 강조 애니메이션 시작
        flashOverlay.gameObject.SetActive(true); // 강조 오버레이 표시
        flashOverlay.transform.SetAsLastSibling(); // 강조 오버레이 최상단 배치
        float elapsedTime = 0f; // 경과 시간 초기화
        while (elapsedTime < FlashDuration) // 강조 시간 반복
        { // 강조 프레임 처리 시작
            float normalizedTime = Mathf.Clamp01(elapsedTime / FlashDuration); // 강조 진행 비율 계산
            float alpha = Mathf.Lerp(0.3f, 0f, normalizedTime); // 강조 투명도 계산
            flashOverlay.color = new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b, alpha); // 강조 색상 적용
            elapsedTime += Time.unscaledDeltaTime; // 실제 시간 누적
            yield return null; // 다음 프레임 대기
        } // 강조 프레임 처리 종료
        flashOverlay.color = Color.clear; // 강조 색상 초기화
        flashOverlay.gameObject.SetActive(false); // 강조 오버레이 숨김
        flashCoroutine = null; // 강조 코루틴 참조 제거
    } // 강조 애니메이션 종료
    private static Color SelectDamageColor(BattleDamageType damageType) // 피해 유형별 색상 선택
    { // 피해 색상 선택 시작
        if (damageType == BattleDamageType.Physical) // 물리 피해 확인
        { // 물리 색상 처리 시작
            return new Color(1f, 0.42f, 0.18f, 1f); // 주황빛 물리 색상 반환
        } // 물리 색상 처리 종료
        if (damageType == BattleDamageType.Magical) // 마법 피해 확인
        { // 마법 색상 처리 시작
            return new Color(0.4f, 0.72f, 1f, 1f); // 하늘빛 마법 색상 반환
        } // 마법 색상 처리 종료
        return Color.white; // 흰색 일반 피해 반환
    } // 피해 색상 선택 종료
    private void OnDisable() // 피드백 화면 비활성화 처리
    { // 비활성화 처리 시작
        if (flashCoroutine != null) // 실행 중인 강조 확인
        { // 강조 중단 시작
            StopCoroutine(flashCoroutine); // 강조 코루틴 중단
            flashCoroutine = null; // 강조 코루틴 참조 제거
        } // 강조 중단 종료
        if (flashOverlay != null) // 강조 오버레이 확인
        { // 오버레이 초기화 시작
            flashOverlay.color = Color.clear; // 강조 색상 초기화
            flashOverlay.gameObject.SetActive(false); // 강조 오버레이 숨김
        } // 오버레이 초기화 종료
    } // 비활성화 처리 종료
} // 클래스 종료
