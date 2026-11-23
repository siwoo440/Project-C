using System.Collections; // 코루틴 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용
public sealed class BattleUnitMotionView : MonoBehaviour // 전투 유닛 움직임 연출
{ // 클래스 시작
    private const float AttackDistance = 22f; // 공격 전진 거리
    private const float MoveDuration = 0.12f; // 전진과 복귀 시간
    private const float HitDuration = 0.18f; // 피격 흔들림 시간
    private const float HitDistance = 6f; // 피격 흔들림 거리
    private const float PulseDuration = 0.18f; // 확대 연출 시간
    private const float PulseScale = 1.08f; // 확대 연출 크기
    private RectTransform visualRect; // 움직일 초상화 사각형
    private Vector2 basePosition; // 초상화 기본 위치
    private Vector3 baseScale; // 초상화 기본 크기
    private Coroutine reactionCoroutine; // 실행 중인 반응 연출
    public bool IsReady => visualRect != null; // 움직임 준비 여부 조회
    public void Initialize(RectTransform targetVisual) // 움직임 대상 초기화
    { // 초기화 시작
        if (targetVisual == null) // 초상화 누락 확인
        { // 초상화 없음 처리 시작
            return; // 초기화 중단
        } // 초상화 없음 처리 종료
        visualRect = targetVisual; // 움직임 대상 저장
        basePosition = visualRect.anchoredPosition; // 기본 위치 저장
        baseScale = visualRect.localScale; // 기본 크기 저장
    } // 초기화 종료
    public IEnumerator PlayAttackAdvance(BattleTeam actorTeam) // 공격 전진 연출
    { // 공격 전진 시작
        if (!IsReady) // 움직임 준비 확인
        { // 준비 안 됨 처리 시작
            yield break; // 전진 연출 종료
        } // 준비 안 됨 처리 종료
        float direction = actorTeam == BattleTeam.Ally ? 1f : -1f; // 진영별 전진 방향 계산
        Vector2 targetPosition = basePosition + Vector2.right * AttackDistance * direction; // 전진 목표 위치 계산
        yield return MovePosition(visualRect.anchoredPosition, targetPosition, MoveDuration); // 목표 위치까지 이동
    } // 공격 전진 종료
    public IEnumerator PlayAttackReturn() // 공격 원위치 복귀
    { // 공격 복귀 시작
        if (!IsReady) // 움직임 준비 확인
        { // 준비 안 됨 처리 시작
            yield break; // 복귀 연출 종료
        } // 준비 안 됨 처리 종료
        yield return MovePosition(visualRect.anchoredPosition, basePosition, MoveDuration); // 기본 위치까지 이동
    } // 공격 복귀 종료
    public IEnumerator PlayCastAnticipation() // 회복 행동 준비 연출
    { // 행동 준비 시작
        if (!IsReady) // 움직임 준비 확인
        { // 준비 안 됨 처리 시작
            yield break; // 준비 연출 종료
        } // 준비 안 됨 처리 종료
        yield return MoveScale(baseScale, baseScale * 1.05f, MoveDuration * 0.5f); // 초상화 짧은 확대
        yield return MoveScale(visualRect.localScale, baseScale, MoveDuration * 0.5f); // 초상화 기본 크기 복귀
    } // 행동 준비 종료
    public void PlayHitReaction() // 피격 흔들림 시작
    { // 피격 반응 시작
        StartReaction(AnimateHit()); // 피격 흔들림 코루틴 시작
    } // 피격 반응 종료
    public void PlayHealReaction() // 회복 확대 시작
    { // 회복 반응 시작
        StartReaction(AnimateHeal()); // 회복 확대 코루틴 시작
    } // 회복 반응 종료
    public void ResetMotion() // 움직임 상태 초기화
    { // 움직임 초기화 시작
        StopAllCoroutines(); // 실행 중인 반응 연출 중단
        reactionCoroutine = null; // 반응 코루틴 참조 제거
        if (!IsReady) // 움직임 준비 확인
        { // 준비 안 됨 처리 시작
            return; // 움직임 초기화 종료
        } // 준비 안 됨 처리 종료
        visualRect.anchoredPosition = basePosition; // 초상화 기본 위치 복구
        visualRect.localScale = baseScale; // 초상화 기본 크기 복구
    } // 움직임 초기화 종료
    private void StartReaction(IEnumerator reaction) // 반응 연출 교체
    { // 반응 교체 시작
        if (!IsReady) // 움직임 준비 확인
        { // 준비 안 됨 처리 시작
            return; // 반응 연출 중단
        } // 준비 안 됨 처리 종료
        if (reactionCoroutine != null) // 기존 반응 연출 확인
        { // 기존 반응 중단 시작
            StopCoroutine(reactionCoroutine); // 기존 반응 코루틴 중단
        } // 기존 반응 중단 종료
        visualRect.anchoredPosition = basePosition; // 반응 시작 위치 초기화
        visualRect.localScale = baseScale; // 반응 시작 크기 초기화
        reactionCoroutine = StartCoroutine(reaction); // 새 반응 코루틴 시작
    } // 반응 교체 종료
    private IEnumerator AnimateHit() // 피격 흔들림 연출
    { // 흔들림 시작
        float elapsedTime = 0f; // 경과 시간 초기화
        while (elapsedTime < HitDuration) // 흔들림 시간 반복
        { // 흔들림 프레임 시작
            float normalizedTime = Mathf.Clamp01(elapsedTime / HitDuration); // 흔들림 진행 비율 계산
            float shakeOffset = Mathf.Sin(normalizedTime * Mathf.PI * 6f) * HitDistance * (1f - normalizedTime); // 감쇠 흔들림 위치 계산
            visualRect.anchoredPosition = basePosition + Vector2.right * shakeOffset; // 흔들림 위치 적용
            elapsedTime += Time.unscaledDeltaTime; // 실제 시간 누적
            yield return null; // 다음 프레임 대기
        } // 흔들림 프레임 종료
        visualRect.anchoredPosition = basePosition; // 기본 위치 복구
        reactionCoroutine = null; // 반응 코루틴 참조 제거
    } // 흔들림 종료
    private IEnumerator AnimateHeal() // 회복 확대 연출
    { // 회복 확대 시작
        float halfDuration = PulseDuration * 0.5f; // 절반 지속 시간 계산
        yield return MoveScale(baseScale, baseScale * PulseScale, halfDuration); // 회복 초상화 확대
        yield return MoveScale(visualRect.localScale, baseScale, halfDuration); // 회복 초상화 복귀
        reactionCoroutine = null; // 반응 코루틴 참조 제거
    } // 회복 확대 종료
    private IEnumerator MovePosition(Vector2 startPosition, Vector2 targetPosition, float duration) // 초상화 위치 이동
    { // 위치 이동 시작
        float elapsedTime = 0f; // 경과 시간 초기화
        while (elapsedTime < duration) // 이동 시간 반복
        { // 이동 프레임 시작
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration); // 이동 진행 비율 계산
            visualRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, normalizedTime); // 보간 위치 적용
            elapsedTime += Time.unscaledDeltaTime; // 실제 시간 누적
            yield return null; // 다음 프레임 대기
        } // 이동 프레임 종료
        visualRect.anchoredPosition = targetPosition; // 목표 위치 확정
    } // 위치 이동 종료
    private IEnumerator MoveScale(Vector3 startScale, Vector3 targetScale, float duration) // 초상화 크기 이동
    { // 크기 이동 시작
        float elapsedTime = 0f; // 경과 시간 초기화
        while (elapsedTime < duration) // 크기 이동 시간 반복
        { // 크기 이동 프레임 시작
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration); // 크기 진행 비율 계산
            visualRect.localScale = Vector3.Lerp(startScale, targetScale, normalizedTime); // 보간 크기 적용
            elapsedTime += Time.unscaledDeltaTime; // 실제 시간 누적
            yield return null; // 다음 프레임 대기
        } // 크기 이동 프레임 종료
        visualRect.localScale = targetScale; // 목표 크기 확정
    } // 크기 이동 종료
    private void OnDisable() // 움직임 화면 비활성화 처리
    { // 비활성화 처리 시작
        ResetMotion(); // 초상화 위치와 크기 복구
    } // 비활성화 처리 종료
} // 클래스 종료
