using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleActionSequenceRunner : MonoBehaviour
{
    private const float ReactionWait = 0.2f;

    private readonly List<BattleUnitView> activeTargetViews =
        new List<BattleUnitView>();

    private BattleUnitView activeActorView;
    private Coroutine playerSequenceCoroutine;
    private Action playerCompletion;

    public bool IsBusy { get; private set; }

    public event Action<bool> BusyStateChanged;

    public bool CanStartAction(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews)
    {
        return
            !IsBusy &&
            actorView != null &&
            actorView.RuntimeUnit != null &&
            !actorView.RuntimeUnit.IsDead &&
            HasValidTarget(targetViews);
    }

    public bool TryStartPlayerAction(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews,
        CardEffectType effectType,
        BattleStatusEffectType statusEffectType,
        Action impactAction,
        Action completionAction)
    {
        if (!CanStartAction(actorView, targetViews) ||
            impactAction == null)
        {
            return false;
        }

        SetBusy(true);
        playerCompletion = completionAction;

        playerSequenceCoroutine =
            StartCoroutine(
                RunPlayerSequence(
                    actorView,
                    targetViews,
                    effectType,
                    statusEffectType,
                    impactAction));

        return true;
    }

    public IEnumerator RunEnemyAction(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews,
        Action impactAction)
    {
        while (IsBusy)
        {
            yield return null;
        }

        if (actorView == null ||
            actorView.RuntimeUnit == null ||
            actorView.RuntimeUnit.IsDead ||
            impactAction == null ||
            !HasValidTarget(targetViews))
        {
            impactAction?.Invoke();
            yield break;
        }

        SetBusy(true);

        yield return RunSequence(
            actorView,
            targetViews,
            CardEffectType.Damage,
            BattleStatusEffectType.None,
            impactAction);
    }

    public void CancelCurrentAction()
    {
        if (playerSequenceCoroutine != null)
        {
            StopCoroutine(
                playerSequenceCoroutine);

            playerSequenceCoroutine = null;
        }

        Action cancelledCompletion =
            playerCompletion;

        playerCompletion = null;

        ResetActiveMotion();
        SetBusy(false);

        cancelledCompletion?.Invoke();
    }

    private IEnumerator RunPlayerSequence(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews,
        CardEffectType effectType,
        BattleStatusEffectType statusEffectType,
        Action impactAction)
    {
        yield return RunSequence(
            actorView,
            targetViews,
            effectType,
            statusEffectType,
            impactAction);

        playerSequenceCoroutine = null;

        Action completedAction =
            playerCompletion;

        playerCompletion = null;

        completedAction?.Invoke();
    }

    private IEnumerator RunSequence(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews,
        CardEffectType effectType,
        BattleStatusEffectType statusEffectType,
        Action impactAction)
    {
        StoreActiveViews(
            actorView,
            targetViews);

        try
        {
            bool isSupportAction =
                effectType == CardEffectType.Heal ||
                effectType == CardEffectType.RemoveDebuffs ||
                effectType == CardEffectType.ApplyStatusEffect &&
                !BattleStatusEffectInstance.IsDebuffType(
                    statusEffectType);

            if (isSupportAction)
            {
                yield return
                    actorView.PlayCastAnticipation();

                impactAction.Invoke();

                yield return
                    new WaitForSecondsRealtime(
                        ReactionWait);
            }
            else
            {
                yield return
                    actorView.PlayAttackAdvance();

                impactAction.Invoke();

                yield return
                    actorView.PlayAttackReturn();

                yield return
                    new WaitForSecondsRealtime(
                        ReactionWait);
            }
        }
        finally
        {
            ResetActiveMotion();
            SetBusy(false);
        }
    }

    private void StoreActiveViews(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews)
    {
        activeActorView = actorView;
        activeTargetViews.Clear();

        foreach (
            BattleUnitView targetView
            in targetViews)
        {
            if (targetView != null &&
                !activeTargetViews.Contains(
                    targetView))
            {
                activeTargetViews.Add(
                    targetView);
            }
        }
    }

    private void ResetActiveMotion()
    {
        // Unity의 파괴된 Object에는 ?. 대신 Unity식 null 검사를 사용한다.
        if (activeActorView != null)
        {
            activeActorView.ResetMotion();
        }

        foreach (
            BattleUnitView targetView
            in activeTargetViews)
        {
            if (targetView != null)
            {
                targetView.ResetMotion();
            }
        }

        activeActorView = null;
        activeTargetViews.Clear();
    }

    private void SetBusy(bool busy)
    {
        if (IsBusy == busy)
        {
            return;
        }

        IsBusy = busy;
        BusyStateChanged?.Invoke(IsBusy);
    }

    private static bool HasValidTarget(
        IReadOnlyList<BattleUnitView> targetViews)
    {
        if (targetViews == null ||
            targetViews.Count < 1)
        {
            return false;
        }

        foreach (
            BattleUnitView targetView
            in targetViews)
        {
            if (targetView != null &&
                targetView.RuntimeUnit != null &&
                !targetView.RuntimeUnit.IsDead)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        CancelCurrentAction();
    }
}
