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
            if (statusEffect.EffectType == BattleStatusEffectType.PublicMark || statusEffect.EffectType == BattleStatusEffectType.Burn || statusEffect.EffectType == BattleStatusEffectType.Blind || statusEffect.EffectType == BattleStatusEffectType.AttackPowerDown) // 턴 종료에 처리하는 상태 확인
            { // 턴 종료 상태 유지 시작
                continue; // 턴 시작 지속 감소 생략
            } // 턴 종료 상태 유지 종료
            int previousRemainingTurns = statusEffect.RemainingTurns; // 처리 전 지속 횟수 저장
            int previousValue = statusEffect.EffectiveValue; // 처리 전 상태 전체 수치 저장
            int appliedAmount = 0; // 실제 적용 수치 초기화
            bool wasTriggered = false; // 발동 여부 초기화
            if (statusEffect.EffectType == BattleStatusEffectType.Poison) // 중독 상태 확인
            { // 중독 처리 시작
                BattleDamageResult damageResult = runtimeUnit.TakeDamage(statusEffect.EffectiveValue, BattleDamageType.None); // 방어 무시 피해 적용
                appliedAmount = damageResult.AppliedDamage; // 실제 중독 피해 저장
                wasTriggered = true; // 중독 발동 저장
                statusEffect.DecayPoison(); // 전체 중독 수치 한 점 감소
                statusStateChanged = true; // 중독 수치 변경 알림 예약
            } // 중독 처리 종료
            else if (statusEffect.EffectType == BattleStatusEffectType.Regeneration || statusEffect.EffectType == BattleStatusEffectType.StarReservedHealing || statusEffect.EffectType == BattleStatusEffectType.StarPassiveReservedHealing) // 재생과 두 종류의 예약 회복 확인
            { // 재생 처리 시작
                appliedAmount = runtimeUnit.RestoreHealth(statusEffect.EffectiveValue); // 실제 재생 회복 저장
                wasTriggered = true; // 재생 발동 저장
            } // 재생 처리 종료
            else if (statusEffect.EffectType == BattleStatusEffectType.BattlefieldResolve) // 다음 아군 턴의 예약 방어 확인
            { // 예약 방어 지급 시작
                appliedAmount = runtimeUnit.GainBlock(statusEffect.EffectiveValue); // 기본 방어도 초기화 뒤 예약 방어도 지급
                wasTriggered = true; // 이번 시작 한 번의 지급 기록
            } // 예약 방어 지급 종료
            else if (statusEffect.EffectType != BattleStatusEffectType.ChariotMark && statusEffect.EffectType != BattleStatusEffectType.PhysicalRiposte) // 턴 시작에 처리할 효과 확인
            { // 턴 종료 지속 상태 처리 시작
                continue; // 일반 상태의 지속 턴은 턴 종료에 처리
            } // 턴 종료 지속 상태 처리 종료
            if (runtimeUnit.IsDead) // 상태 피해 사망 확인
            { // 사망 결과 처리 시작
                processResults.Add(new BattleStatusEffectProcessResult(statusEffect.EffectType, round, previousValue, appliedAmount, previousRemainingTurns, previousRemainingTurns, statusEffect.StackCount, wasTriggered, BattleStatusEffectRemovalReason.UnitDied)); // 피해 이전 수치의 사망 결과 기록
                break; // 사망 이후 상태 처리 중단
            } // 사망 결과 처리 종료
            if (statusEffect.EffectType == BattleStatusEffectType.StarReservedHealing || statusEffect.EffectType == BattleStatusEffectType.StarPassiveReservedHealing || statusEffect.EffectType == BattleStatusEffectType.ChariotMark || statusEffect.EffectType == BattleStatusEffectType.PhysicalRiposte || statusEffect.EffectType == BattleStatusEffectType.BattlefieldResolve) // 다음 시작에 소모하는 전용 상태 확인
            { // 전용 상태 처리 시작
                statusEffect.AdvanceDuration(); // 전용 효과의 발동 횟수 감소
                statusStateChanged = true; // 상태 변경 여부 저장
            } // 전용 상태 처리 종료
            BattleStatusEffectRemovalReason removalReason = BattleStatusEffectRemovalReason.None; // 기본 제거 원인 생성
            if (statusEffect.IsExpired) // 지속시간 만료 확인
            { // 만료 처리 시작
                runtimeUnit.RemoveStatusEffectInstance(statusEffect); // 만료 상태 조용히 제거
                removalReason = BattleStatusEffectRemovalReason.DurationExpired; // 만료 제거 원인 저장
            } // 만료 처리 종료
            if (wasTriggered || removalReason != BattleStatusEffectRemovalReason.None) // 실제 효과와 만료 결과 확인
            { // 시작 결과 기록 시작
                processResults.Add(new BattleStatusEffectProcessResult(statusEffect.EffectType, round, previousValue, appliedAmount, previousRemainingTurns, statusEffect.RemainingTurns, statusEffect.StackCount, wasTriggered, removalReason)); // 처리 전 수치의 결과 기록
            } // 시작 결과 기록 종료
        } // 개별 상태 처리 종료
        if (statusStateChanged && !runtimeUnit.IsDead) // 상태 일괄 알림 필요 확인
        { // 상태 알림 시작
            runtimeUnit.NotifyStatusEffectsChanged(); // 상태 UI 일괄 갱신 알림
        } // 상태 알림 종료
        return processResults; // 전체 처리 결과 반환
    } // 상태 처리 종료
    public IReadOnlyList<BattleStatusEffectProcessResult> ProcessPhaseEnd(BattleUnitRuntime runtimeUnit, int round) // 진영 턴 종료 상태 처리
    { // 턴 종료 상태 처리 시작
        List<BattleStatusEffectProcessResult> results = new List<BattleStatusEffectProcessResult>(); // 처리 결과 목록 생성
        if (runtimeUnit == null || runtimeUnit.IsDead) // 유효한 생존 대상 확인
        { // 처리 불가 시작
            return results; // 빈 결과 반환
        } // 처리 불가 종료
        List<BattleStatusEffectInstance> snapshot = runtimeUnit.CreateStatusEffectSnapshot(); // 변경 대비 상태 목록 복사
        foreach (BattleStatusEffectInstance effect in snapshot) // 대상 상태 순회
        { // 턴 종료 효과 확인 시작
            if (runtimeUnit.IsDead) // 이전 효과로 사망 확인
            { // 사망 처리 시작
                break; // 나머지 효과 처리 중단
            } // 사망 처리 종료
            if (!runtimeUnit.ContainsStatusEffectInstance(effect) || effect.EffectType == BattleStatusEffectType.DuelMark || effect.EffectType == BattleStatusEffectType.BattlefieldResolve || effect.EffectType == BattleStatusEffectType.PhysicalRiposte || effect.WaitsForAction || effect.EffectType == BattleStatusEffectType.Evasion || effect.EffectType == BattleStatusEffectType.ShockAbsorption || effect.EffectType == BattleStatusEffectType.ResistanceStance || effect.EffectType == BattleStatusEffectType.ActionSpeedUp || effect.IsProtectedMentalState || effect.EffectType == BattleStatusEffectType.StarReservedHealing || effect.EffectType == BattleStatusEffectType.StarPassiveReservedHealing || effect.EffectType == BattleStatusEffectType.ChariotMark || effect.EffectType == BattleStatusEffectType.PublicMark) // 배지처럼 별도 시점에 해제하는 상태 제외
            { // 처리 제외 시작
                continue; // 다음 상태 확인
            } // 처리 제외 종료
            int previousTurns = effect.RemainingTurns; // 처리 전 지속 횟수 저장
            int previousValue = effect.EffectiveValue; // 처리 전 효과 수치 저장
            int applied = 0; // 실제 피해 초기화
            bool triggered = effect.EffectType == BattleStatusEffectType.Burn || effect.EffectType == BattleStatusEffectType.Bleed; // 화상과 출혈의 턴 종료 발동 확인
            if (effect.EffectType == BattleStatusEffectType.Bleed) // 방어도를 우회하는 고정 체력 손실 확인
            { // 출혈 처리 시작
                applied = runtimeUnit.TakeUnblockedDamage(previousValue, BattleDamageType.None, null); // 현재 체력만 감소시키고 사망 알림 처리
            } // 출혈 처리 종료
            else if (effect.EffectType == BattleStatusEffectType.Burn) // 기존 화상 피해 확인
            { // 화상 피해 처리 시작
                applied = runtimeUnit.TakeDamage(previousValue, BattleDamageType.None).AppliedDamage; // 화상 수치만큼 피해 적용
            } // 화상 피해 처리 종료
            if (runtimeUnit.IsDead) // 화상 피해로 사망 확인
            { // 사망 결과 처리 시작
                results.Add(new BattleStatusEffectProcessResult(effect.EffectType, round, previousValue, applied, previousTurns, previousTurns, effect.StackCount, triggered, BattleStatusEffectRemovalReason.UnitDied)); // 사망 처리 결과 기록
                break; // 나머지 상태 처리 중단
            } // 사망 결과 처리 종료
            effect.DecayBurn(); // 화상 수치 한 점 감소
            if (!effect.ConsumeDurationDecreaseSkip()) // 자기 턴 신규 상태의 첫 감소 생략 확인
            { // 지속 감소 처리 시작
                effect.AdvanceDuration(); // 남은 지속 턴 감소
            } // 지속 감소 처리 종료
            BattleStatusEffectRemovalReason reason = BattleStatusEffectRemovalReason.None; // 기본 제거 원인 초기화
            if (effect.IsExpired) // 지속 종료 또는 화상 소진 확인
            { // 상태 만료 처리 시작
                runtimeUnit.RemoveStatusEffectInstance(effect); // 만료 상태 제거
                reason = BattleStatusEffectRemovalReason.DurationExpired; // 만료 원인 기록
            } // 상태 만료 처리 종료
            results.Add(new BattleStatusEffectProcessResult(effect.EffectType, round, previousValue, applied, previousTurns, effect.RemainingTurns, effect.StackCount, triggered, reason)); // 턴 종료 처리 결과 기록
        } // 턴 종료 효과 확인 종료
        if (results.Count > 0 && !runtimeUnit.IsDead) // 실제 상태 변경 여부 확인
        { // 상태 변경 알림 시작
            runtimeUnit.NotifyStatusEffectsChanged(); // 상태 화면 갱신 알림
        } // 상태 변경 알림 종료
        return results; // 턴 종료 처리 결과 반환
    } // 턴 종료 상태 처리 종료
    public IReadOnlyList<BattleStatusEffectProcessResult> CleanseDebuffs(BattleUnitRuntime runtimeUnit, int maximumCount = int.MaxValue) // 지정 개수의 디버프 정화 처리
    { // 정화 처리 시작
        List<BattleStatusEffectProcessResult> processResults = new List<BattleStatusEffectProcessResult>(); // 빈 정화 결과 목록 생성
        if (runtimeUnit == null || runtimeUnit.IsDead || runtimeUnit.StatusEffects.Count < 1 || maximumCount <= 0) // 정화 대상과 개수 유효성 확인
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
            if (processResults.Count >= maximumCount) // 정화 개수 도달 확인
            { // 정화 종료 시작
                break; // 나머지 디버프 보존
            } // 정화 종료 완료
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
