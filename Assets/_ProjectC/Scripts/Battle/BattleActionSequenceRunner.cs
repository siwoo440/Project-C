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
    private BattleMagicMissileEffect magicMissile; // 현재 전투에서 재사용할 마력탄 효과

    public bool IsBusy { get; private set; }

    public event Action<bool> BusyStateChanged;

    public bool CanStartAction(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews, // 연출 대상 화면 목록
        bool fallenTarget = false) // 부활 행동의 전투불능 대상 지정
    {
        return
            !IsBusy &&
            actorView != null &&
            actorView.RuntimeUnit != null &&
            !actorView.RuntimeUnit.IsDead &&
            HasValidTarget(targetViews, fallenTarget); // 행동 종류에 맞는 대상 상태 확인
    }

    public bool TryStartPlayerAction(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews,
        CardEffectType effectType,
        BattleStatusEffectType statusEffectType,
        Action impactAction,
        Action completionAction,
        bool useMagicMissileEffect = false) // 원본 마력탄 시전과 비행 표시 여부
    {
        if (!CanStartAction(actorView, targetViews, effectType == CardEffectType.ReviveAlly) || // 부활 대상이 전투불능 상태인지 확인
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
                    impactAction,
                    useMagicMissileEffect)); // 전투 카드의 시각 효과 선택 전달

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
        magicMissile?.Reset(); // 중단된 카드의 투명 효과를 즉시 숨김

        ResetActiveMotion();
        SetBusy(false);

        cancelledCompletion?.Invoke();
    }

    private IEnumerator RunPlayerSequence(
        BattleUnitView actorView,
        IReadOnlyList<BattleUnitView> targetViews,
        CardEffectType effectType,
        BattleStatusEffectType statusEffectType,
        Action impactAction,
        bool useMagicMissileEffect) // 플레이어 마력탄 연출 선택
    {
        yield return RunSequence(
            actorView,
            targetViews,
            effectType,
            statusEffectType,
            impactAction,
            useMagicMissileEffect); // 마력탄의 연출 선택과 원래 정산 콜백

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
        Action impactAction,
        bool useMagicMissileEffect = false) // 적 행동의 기존 연출 기본값 유지
    {
        StoreActiveViews(
            actorView,
            targetViews);

        try
        {
            if (useMagicMissileEffect && targetViews.Count == 1 && PrepareMagicMissile(actorView)) // 원본 마력탄과 실제 전투 원화 확인
            { // 마력탄 전용 시전과 피해 확인 시작
                yield return actorView.PlayCastAnticipation(); // 시전자의 기존 준비 움직임 재사용
                yield return magicMissile.Travel(actorView, targetViews[0]); // 같은 실제 카드 대상에게 마력탄 비행
                impactAction.Invoke(); // 원래 카드의 명중과 피해 및 비용 정상 정산
                yield return magicMissile.React(); // 실제 명중 피드백 또는 빗나감 자막 대기
                yield break; // 공용 근접 공격 움직임 대신 전용 순서 종료
            } // 마력탄 전용 시전과 피해 확인 종료

            bool isSupportAction =
                effectType == CardEffectType.RecoverDiscard || // 버린 카드 회수 지원 연출
                effectType == CardEffectType.ChooseDraw || // 선택 드로우 지원 연출
                effectType == CardEffectType.GainBlock || // 방어도 획득 지원 연출
                effectType == CardEffectType.SelectPartner || // 파트너 지정 지원 연출
                effectType == CardEffectType.GuardAllyAttack || // 동료 보호 지원 연출
                effectType == CardEffectType.ArrangeDrawTop || // 덱 순서 정리 지원 연출
                effectType == CardEffectType.RerollRandomAttack || // 공격 재굴림 지원 연출
                effectType == CardEffectType.ShareAllyAttack || // 동료 피해 분담 지원 연출
                effectType == CardEffectType.TransferAllyDebuff || // 동료 디버프 이전 지원 연출
                effectType == CardEffectType.CopyAllyBlessing || // 축복 이전의 적 공격 없는 지원 연출
                effectType == CardEffectType.DrawThenDiscard || // 드로우 후 손패 정리 지원 연출
                effectType == CardEffectType.ProtectAttackTarget || // 공격 예고 보호 지원 연출
                effectType == CardEffectType.RedirectAttackAndDefend || // 공격 유도 지원 연출
                effectType == CardEffectType.ReviveAlly || // 전투불능 아군 부활 지원 연출
                effectType == CardEffectType.PentacleUtility || // 방어와 회복 및 비용 선택을 쓰는 펜타클 지원 연출
                effectType == CardEffectType.Heal ||
                effectType == CardEffectType.RemoveDebuffs ||
                effectType == CardEffectType.RemoveOneDebuff || // 디버프 하나 해제 지원 연출
                effectType == CardEffectType.ApplyStatusEffect &&
                !BattleStatusEffectInstance.IsDebuffType(
                    statusEffectType);

            if (isSupportAction)
            {
                yield return
                    actorView.PlayCastAnticipation();

                impactAction.Invoke();

                yield return new WaitForSecondsRealtime(GameplaySpeedSettings.BattleDuration(ReactionWait)); // 선택한 속도로 지원 반응 대기
            }
            else
            {
                yield return
                    actorView.PlayAttackAdvance();

                impactAction.Invoke();

                yield return
                    actorView.PlayAttackReturn();

                yield return new WaitForSecondsRealtime(GameplaySpeedSettings.BattleDuration(ReactionWait)); // 선택한 속도로 공격 반응 대기
            }
        }
        finally
        {
            magicMissile?.Reset(); // 전투 연출이 끝날 때 모든 마력탄 이미지 숨김
            ResetActiveMotion();
            SetBusy(false);
        }
    }

    internal void RecordMagicMissileImpact(BattleUnitView target) // 원본 카드의 실제 명중을 화면에 전달
    { // 현재 전투의 표시 기록 시작
        magicMissile?.RecordImpact(target); // 타격 판정에 통과한 적의 실제 위치 기록
    } // 현재 전투의 표시 기록 종료

    private bool PrepareMagicMissile(BattleUnitView actor) // 현재 캔버스에서 표시 층 조회 또는 한 번 설치
    { // 시각 효과 리소스 확인 시작
        if (magicMissile == null || !magicMissile.IsAttached) // 아직 없거나 이전 씬의 표시 층인지 확인
        { // 현재 전투 표시 층 설치 시작
            magicMissile = BattleMagicMissileEffect.Create(actor); // 실제 전투 원화와 좌표 계산 준비
        } // 현재 전투 표시 층 설치 종료
        return magicMissile != null; // 누락된 원화가 있으면 기존 움직임 사용
    } // 시각 효과 리소스 확인 종료

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
        IReadOnlyList<BattleUnitView> targetViews, // 확인할 대상 화면 목록
        bool fallenTarget = false) // 전투불능 대상 필요 여부
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
            BattleUnitRuntime target = targetView == null ? null : targetView.RuntimeUnit; // 화면 연결 유닛 조회
            if (target != null && (fallenTarget ? target.Team == BattleTeam.Ally && target.IsDead : !target.IsDead)) // 부활용 아군과 일반 행동의 생존 대상 구분
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
