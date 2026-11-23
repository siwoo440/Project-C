using System; // 기본 이벤트 기능 사용
using System.Collections; // 코루틴 자료형 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용
public sealed class BattleActionSequenceRunner : MonoBehaviour // 전투 행동 순서 연출
{ // 클래스 시작
    private const float ReactionWait = 0.2f; // 대상 반응 대기 시간
    private readonly List<BattleUnitView> activeTargetViews = new List<BattleUnitView>(); // 현재 연출 대상 화면 목록
    private BattleUnitView activeActorView; // 현재 행동자 화면
    private Coroutine playerSequenceCoroutine; // 플레이어 행동 코루틴
    private Action playerCompletion; // 플레이어 행동 완료 처리
    public bool IsBusy { get; private set; } // 행동 연출 실행 여부
    public event Action<bool> BusyStateChanged; // 행동 연출 상태 변경 이벤트
    public bool CanStartAction(BattleUnitView actorView, IReadOnlyList<BattleUnitView> targetViews) // 행동 연출 시작 가능 확인
    { // 시작 가능 검사 시작
        return !IsBusy && actorView != null && actorView.RuntimeUnit != null && !actorView.RuntimeUnit.IsDead && HasValidTarget(targetViews); // 행동자와 대상 유효성 반환
    } // 시작 가능 검사 종료
    public bool TryStartPlayerAction(BattleUnitView actorView, IReadOnlyList<BattleUnitView> targetViews, CardEffectType effectType, Action impactAction, Action completionAction) // 플레이어 행동 연출 시작
    { // 플레이어 행동 시작
        if (!CanStartAction(actorView, targetViews) || impactAction == null) // 시작 조건 확인
        { // 시작 불가 처리 시작
            return false; // 플레이어 행동 시작 실패 반환
        } // 시작 불가 처리 종료
        SetBusy(true); // 행동 입력 잠금
        playerCompletion = completionAction; // 완료 처리 저장
        playerSequenceCoroutine = StartCoroutine(RunPlayerSequence(actorView, targetViews, effectType, impactAction)); // 플레이어 행동 코루틴 시작
        return true; // 플레이어 행동 시작 성공 반환
    } // 플레이어 행동 시작 종료
    public IEnumerator RunEnemyAction(BattleUnitView actorView, IReadOnlyList<BattleUnitView> targetViews, Action impactAction) // 적 행동 연출 실행
    { // 적 행동 연출 시작
        while (IsBusy) // 기존 연출 종료 대기
        { // 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 연출 대기 종료
        if (actorView == null || actorView.RuntimeUnit == null || actorView.RuntimeUnit.IsDead || impactAction == null || !HasValidTarget(targetViews)) // 적 행동 유효성 확인
        { // 적 행동 불가 처리 시작
            impactAction?.Invoke(); // 연출 없이 행동 적용
            yield break; // 적 행동 연출 종료
        } // 적 행동 불가 처리 종료
        SetBusy(true); // 적 행동 연출 상태 적용
        yield return RunSequence(actorView, targetViews, CardEffectType.Damage, impactAction); // 적 공격 순서 실행
    } // 적 행동 연출 종료
    public void CancelCurrentAction() // 현재 행동 연출 취소
    { // 행동 취소 시작
        if (playerSequenceCoroutine != null) // 플레이어 행동 실행 확인
        { // 플레이어 행동 중단 시작
            StopCoroutine(playerSequenceCoroutine); // 플레이어 행동 코루틴 중단
            playerSequenceCoroutine = null; // 플레이어 행동 코루틴 참조 제거
        } // 플레이어 행동 중단 종료
        Action cancelledCompletion = playerCompletion; // 취소 완료 처리 저장
        playerCompletion = null; // 저장 완료 처리 제거
        ResetActiveMotion(); // 현재 유닛 움직임 복구
        SetBusy(false); // 행동 입력 잠금 해제
        cancelledCompletion?.Invoke(); // 취소 완료 처리 실행
    } // 행동 취소 종료
    private IEnumerator RunPlayerSequence(BattleUnitView actorView, IReadOnlyList<BattleUnitView> targetViews, CardEffectType effectType, Action impactAction) // 플레이어 행동 순서 실행
    { // 플레이어 순서 시작
        yield return RunSequence(actorView, targetViews, effectType, impactAction); // 공통 행동 순서 실행
        playerSequenceCoroutine = null; // 플레이어 코루틴 참조 제거
        Action completedAction = playerCompletion; // 완료 처리 저장
        playerCompletion = null; // 저장 완료 처리 제거
        completedAction?.Invoke(); // 플레이어 행동 완료 알림
    } // 플레이어 순서 종료
    private IEnumerator RunSequence(BattleUnitView actorView, IReadOnlyList<BattleUnitView> targetViews, CardEffectType effectType, Action impactAction) // 공통 행동 순서 실행
    { // 공통 순서 시작
        StoreActiveViews(actorView, targetViews); // 현재 연출 유닛 저장
        try // 연출 복구 보장 시작
        { // 연출 실행 시작
            if (effectType == CardEffectType.Heal) // 회복 행동 확인
            { // 회복 순서 시작
                yield return actorView.PlayCastAnticipation(); // 행동자 회복 준비 연출
                impactAction.Invoke(); // 회복 효과 적용
                yield return new WaitForSecondsRealtime(ReactionWait); // 회복 반응 완료 대기
            } // 회복 순서 종료
            else // 공격 행동 처리
            { // 공격 순서 시작
                yield return actorView.PlayAttackAdvance(); // 행동자 전진 연출
                impactAction.Invoke(); // 피해 효과 적용
                yield return actorView.PlayAttackReturn(); // 행동자 원위치 복귀
                yield return new WaitForSecondsRealtime(ReactionWait); // 피격 반응 완료 대기
            } // 공격 순서 종료
        } // 연출 실행 종료
        finally // 연출 복구 보장 처리
        { // 연출 복구 시작
            ResetActiveMotion(); // 유닛 위치와 크기 복구
            SetBusy(false); // 행동 연출 상태 해제
        } // 연출 복구 종료
    } // 공통 순서 종료
    private void StoreActiveViews(BattleUnitView actorView, IReadOnlyList<BattleUnitView> targetViews) // 현재 연출 화면 저장
    { // 화면 저장 시작
        activeActorView = actorView; // 현재 행동자 저장
        activeTargetViews.Clear(); // 기존 대상 화면 제거
        foreach (BattleUnitView targetView in targetViews) // 대상 화면 목록 순회
        { // 대상 화면 저장 시작
            if (targetView != null && !activeTargetViews.Contains(targetView)) // 유효 대상 중복 확인
            { // 유효 대상 처리 시작
                activeTargetViews.Add(targetView); // 현재 대상 화면 추가
            } // 유효 대상 처리 종료
        } // 대상 화면 저장 종료
    } // 화면 저장 종료
    private void ResetActiveMotion() // 현재 연출 움직임 복구
    { // 움직임 복구 시작
        activeActorView?.ResetMotion(); // 행동자 움직임 복구
        foreach (BattleUnitView targetView in activeTargetViews) // 대상 화면 목록 순회
        { // 대상 움직임 복구 시작
            targetView?.ResetMotion(); // 대상 움직임 복구
        } // 대상 움직임 복구 종료
        activeActorView = null; // 현재 행동자 참조 제거
        activeTargetViews.Clear(); // 현재 대상 화면 제거
    } // 움직임 복구 종료
    private void SetBusy(bool busy) // 행동 연출 상태 설정
    { // 상태 설정 시작
        if (IsBusy == busy) // 동일 상태 확인
        { // 동일 상태 처리 시작
            return; // 상태 변경 중단
        } // 동일 상태 처리 종료
        IsBusy = busy; // 행동 연출 상태 저장
        BusyStateChanged?.Invoke(IsBusy); // 행동 연출 상태 변경 알림
    } // 상태 설정 종료
    private static bool HasValidTarget(IReadOnlyList<BattleUnitView> targetViews) // 유효 대상 화면 확인
    { // 대상 화면 검사 시작
        if (targetViews == null || targetViews.Count < 1) // 대상 목록 확인
        { // 대상 없음 처리 시작
            return false; // 유효 대상 없음 반환
        } // 대상 없음 처리 종료
        foreach (BattleUnitView targetView in targetViews) // 대상 화면 목록 순회
        { // 대상 화면 검사 시작
            if (targetView != null && targetView.RuntimeUnit != null && !targetView.RuntimeUnit.IsDead) // 생존 대상 확인
            { // 생존 대상 처리 시작
                return true; // 유효 대상 존재 반환
            } // 생존 대상 처리 종료
        } // 대상 화면 검사 종료
        return false; // 유효 대상 없음 반환
    } // 대상 화면 검사 종료
    private void OnDisable() // 시퀀스 실행기 비활성화 처리
    { // 비활성화 처리 시작
        CancelCurrentAction(); // 현재 연출과 입력 잠금 해제
    } // 비활성화 처리 종료
} // 클래스 종료
