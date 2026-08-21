using System.Collections.Generic; // 목록 자료형 사용
public sealed class BattleStatusEffectProcessor // 상태 발동과 제거 통합 처리기
{ // 클래스 시작
    public IReadOnlyList<BattleStatusEffectProcessResult> ProcessPhaseStart(BattleUnitRuntime runtimeUnit, int round) // 진영 턴 시작 상태 처리
    { // 상태 처리 시작
        List<BattleStatusEffectProcessResult> processResults = new List<BattleStatusEffectProcessResult>(); // 빈 처리 결과 목록 생성
        if (runtimeUnit == null || runtimeUnit.IsDead || runtimeUnit.StatusEffects.Count < 1) // 처리 대상 유효성 확인
        { // 처리 불가 시작
            return processResults; // 빈 처리 결과 반환
        } // 처리 불가 종료
        List<BattleStatusEffectInstance> effectSnapshot = runtimeUnit.CreateStatusEffectSnapshot(); // 변경 대비 상태 목록 복사
        bool statusStateChanged = false; // 상태 목록 또는 지속시간 변경 여부
        foreach (BattleStatusEffectInstance statusEffect in effectSnapshot) // 상태 목록 순회
        { // 개별 상태 처리 시작
            if (runtimeUnit.IsDead) // 처리 중 사망 확인
            { // 사망 중단 시작
                break; // 남은 상태 처리 중단
            } // 사망 중단 종료
            if (!runtimeUnit.ContainsStatusEffectInstance(statusEffect)) // 현재 상태 존재 확인
            { // 제거 상태 제외 시작
                continue; // 다음 상태 이동
            } // 제거 상태 제외 종료
            int previousRemainingTurns = statusEffect.RemainingTurns; // 처리 전 지속 횟수 저장
            int appliedAmount = 0; // 실제 적용 수치 초기화
            bool wasTriggered = false; // 발동 여부 초기화
            if (statusEffect.EffectType == BattleStatusEffectType.Poison) // 중독 상태 확인
            { // 중독 처리 시작
                BattleDamageResult damageResult = runtimeUnit.TakeDamage(statusEffect.EffectiveValue, BattleDamageType.None); // 방어 무시 피해 적용
                appliedAmount = damageResult.AppliedDamage; // 실제 중독 피해 저장
                wasTriggered = true; // 중독 발동 저장
            } // 중독 처리 종료
            else if (statusEffect.EffectType == BattleStatusEffectType.Regeneration) // 재생 상태 확인
            { // 재생 처리 시작
                appliedAmount = runtimeUnit.RestoreHealth(statusEffect.EffectiveValue); // 실제 재생 회복 저장
                wasTriggered = true; // 재생 발동 저장
            } // 재생 처리 종료
            if (runtimeUnit.IsDead) // 상태 피해 사망 확인
            { // 사망 결과 처리 시작
                processResults.Add(CreateResult(statusEffect, round, appliedAmount, previousRemainingTurns, previousRemainingTurns, wasTriggered, BattleStatusEffectRemovalReason.UnitDied)); // 사망 제거 결과 추가
                break; // 사망 이후 상태 처리 중단
            } // 사망 결과 처리 종료
            statusEffect.AdvanceDuration(); // 상태 지속 횟수 감소
            statusStateChanged = true; // 상태 변경 여부 저장
            BattleStatusEffectRemovalReason removalReason = BattleStatusEffectRemovalReason.None; // 기본 제거 원인 생성
            if (statusEffect.IsExpired) // 지속시간 만료 확인
            { // 만료 처리 시작
                runtimeUnit.RemoveStatusEffectInstance(statusEffect); // 만료 상태 조용히 제거
                removalReason = BattleStatusEffectRemovalReason.DurationExpired; // 만료 제거 원인 저장
            } // 만료 처리 종료
            processResults.Add(CreateResult(statusEffect, round, appliedAmount, previousRemainingTurns, statusEffect.RemainingTurns, wasTriggered, removalReason)); // 상태 처리 결과 추가
        } // 개별 상태 처리 종료
        if (statusStateChanged && !runtimeUnit.IsDead) // 상태 일괄 알림 필요 확인
        { // 상태 알림 시작
            runtimeUnit.NotifyStatusEffectsChanged(); // 상태 UI 일괄 갱신 알림
        } // 상태 알림 종료
        return processResults; // 전체 처리 결과 반환
    } // 상태 처리 종료
    public IReadOnlyList<BattleStatusEffectProcessResult> CleanseDebuffs(BattleUnitRuntime runtimeUnit) // 디버프 정화 통합 처리
    { // 정화 처리 시작
        List<BattleStatusEffectProcessResult> processResults = new List<BattleStatusEffectProcessResult>(); // 빈 정화 결과 목록 생성
        if (runtimeUnit == null || runtimeUnit.IsDead || runtimeUnit.StatusEffects.Count < 1) // 정화 대상 유효성 확인
        { // 정화 불가 시작
            return processResults; // 빈 정화 결과 반환
        } // 정화 불가 종료
        List<BattleStatusEffectInstance> effectSnapshot = runtimeUnit.CreateStatusEffectSnapshot(); // 변경 대비 상태 목록 복사
        foreach (BattleStatusEffectInstance statusEffect in effectSnapshot) // 상태 목록 순회
        { // 정화 후보 처리 시작
            if (!statusEffect.IsDebuff || !runtimeUnit.ContainsStatusEffectInstance(statusEffect)) // 디버프 존재 확인
            { // 정화 제외 시작
                continue; // 다음 상태 이동
            } // 정화 제외 종료
            if (!runtimeUnit.RemoveStatusEffectInstance(statusEffect)) // 디버프 제거 결과 확인
            { // 제거 실패 시작
                continue; // 다음 상태 이동
            } // 제거 실패 종료
            processResults.Add(CreateResult(statusEffect, 0, 0, statusEffect.RemainingTurns, statusEffect.RemainingTurns, false, BattleStatusEffectRemovalReason.Cleansed)); // 정화 결과 추가
        } // 정화 후보 처리 종료
        if (processResults.Count > 0) // 실제 정화 여부 확인
        { // 정화 알림 시작
            runtimeUnit.NotifyStatusEffectsChanged(); // 상태 UI 한 번 갱신 알림
        } // 정화 알림 종료
        return processResults; // 전체 정화 결과 반환
    } // 정화 처리 종료
    private static BattleStatusEffectProcessResult CreateResult(BattleStatusEffectInstance statusEffect, int round, int appliedAmount, int previousRemainingTurns, int remainingTurns, bool wasTriggered, BattleStatusEffectRemovalReason removalReason) // 공통 처리 결과 생성
    { // 결과 생성 시작
        return new BattleStatusEffectProcessResult(statusEffect.EffectType, round, statusEffect.EffectiveValue, appliedAmount, previousRemainingTurns, remainingTurns, statusEffect.StackCount, wasTriggered, removalReason); // 상태 처리 결과 반환
    } // 결과 생성 종료
} // 클래스 종료
