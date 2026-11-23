using System.Collections;
using UnityEngine;

public sealed class BattleUnitMotionView : MonoBehaviour
{
    private const float AttackDistance = 22f;
    private const float MoveDuration = 0.12f;
    private const float HitDuration = 0.18f;
    private const float HitDistance = 6f;
    private const float PulseDuration = 0.18f;
    private const float PulseScale = 1.08f;

    private RectTransform visualRect;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private Coroutine reactionCoroutine;

    public bool IsReady =>
        this != null &&
        visualRect != null;

    public void Initialize(RectTransform targetVisual)
    {
        if (this == null || targetVisual == null)
        {
            return;
        }

        visualRect = targetVisual;
        basePosition = visualRect.anchoredPosition;
        baseScale = visualRect.localScale;
    }

    public IEnumerator PlayAttackAdvance(BattleTeam actorTeam)
    {
        if (!IsReady)
        {
            yield break;
        }

        float direction =
            actorTeam == BattleTeam.Ally ? 1f : -1f;

        Vector2 targetPosition =
            basePosition +
            Vector2.right * AttackDistance * direction;

        yield return MovePosition(
            visualRect.anchoredPosition,
            targetPosition,
            MoveDuration);
    }

    public IEnumerator PlayAttackReturn()
    {
        if (!IsReady)
        {
            yield break;
        }

        yield return MovePosition(
            visualRect.anchoredPosition,
            basePosition,
            MoveDuration);
    }

    public IEnumerator PlayCastAnticipation()
    {
        if (!IsReady)
        {
            yield break;
        }

        yield return MoveScale(
            baseScale,
            baseScale * 1.05f,
            MoveDuration * 0.5f);

        if (!IsReady)
        {
            yield break;
        }

        yield return MoveScale(
            visualRect.localScale,
            baseScale,
            MoveDuration * 0.5f);
    }

    public void PlayHitReaction()
    {
        if (!IsReady)
        {
            return;
        }

        StartReaction(AnimateHit());
    }

    public void PlayHealReaction()
    {
        if (!IsReady)
        {
            return;
        }

        StartReaction(AnimateHeal());
    }

    public void ResetMotion()
    {
        // Unity Object는 Destroy된 뒤에도 C# 참조가 남아 있을 수 있다.
        // 네이티브 함수를 호출하기 전에 Unity식 null 판정을 먼저 수행한다.
        if (this == null)
        {
            return;
        }

        StopAllCoroutines();
        reactionCoroutine = null;

        if (!IsReady)
        {
            return;
        }

        visualRect.anchoredPosition = basePosition;
        visualRect.localScale = baseScale;
    }

    private void StartReaction(IEnumerator reaction)
    {
        if (!IsReady)
        {
            return;
        }

        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
        }

        visualRect.anchoredPosition = basePosition;
        visualRect.localScale = baseScale;

        reactionCoroutine =
            StartCoroutine(reaction);
    }

    private IEnumerator AnimateHit()
    {
        float elapsedTime = 0f;

        while (elapsedTime < HitDuration)
        {
            if (!IsReady)
            {
                reactionCoroutine = null;
                yield break;
            }

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / HitDuration);

            float shakeOffset =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI *
                    6f) *
                HitDistance *
                (1f - normalizedTime);

            visualRect.anchoredPosition =
                basePosition +
                Vector2.right * shakeOffset;

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        if (IsReady)
        {
            visualRect.anchoredPosition =
                basePosition;
        }

        reactionCoroutine = null;
    }

    private IEnumerator AnimateHeal()
    {
        if (!IsReady)
        {
            yield break;
        }

        float halfDuration =
            PulseDuration * 0.5f;

        yield return MoveScale(
            baseScale,
            baseScale * PulseScale,
            halfDuration);

        if (!IsReady)
        {
            reactionCoroutine = null;
            yield break;
        }

        yield return MoveScale(
            visualRect.localScale,
            baseScale,
            halfDuration);

        reactionCoroutine = null;
    }

    private IEnumerator MovePosition(
        Vector2 startPosition,
        Vector2 targetPosition,
        float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (!IsReady)
            {
                yield break;
            }

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / duration);

            visualRect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    normalizedTime);

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        if (IsReady)
        {
            visualRect.anchoredPosition =
                targetPosition;
        }
    }

    private IEnumerator MoveScale(
        Vector3 startScale,
        Vector3 targetScale,
        float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (!IsReady)
            {
                yield break;
            }

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / duration);

            visualRect.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    normalizedTime);

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        if (IsReady)
        {
            visualRect.localScale =
                targetScale;
        }
    }

    private void OnDisable()
    {
        ResetMotion();
    }
}
