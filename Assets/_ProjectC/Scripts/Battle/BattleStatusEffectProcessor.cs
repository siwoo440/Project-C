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
            if (statusEffect.EffectType == BattleStatusEffectType.Shock) // 실제 적 턴 시작마다 한 번 피해를 주는 감전 확인
            { // 감전의 피해와 지속 차감 시작
                if (runtimeUnit.Team == BattleTeam.Enemy && !statusEffect.IsExpired) // 현재 적 진영과 활성 감전 확인
                { // 감전 효과 적용 시작
                    int previousTurns = statusEffect.RemainingTurns; // 이번 발동 직전의 남은 적 턴 기록
                    statusEffect.AdvanceDuration(); // 피해 알림 전에 이번 적 턴의 발동 횟수 차감
                    var reason = BattleStatusEffectRemovalReason.None; // 다음 적 턴까지 남기는 감전의 기본 제거 사유
                    if (statusEffect.IsExpired) // 마지막 발동으로 감전이 소모되었는지 확인
                    { // 마지막 감전 제거 시작
                        runtimeUnit.RemoveStatusEffectInstance(statusEffect); // 다시 체력이 바뀌기 전에 마지막 감전 제거
                        reason = BattleStatusEffectRemovalReason.DurationExpired; // 지속 횟수 소진의 제거 사유 기록
                    } // 마지막 감전 제거 종료
                    statusStateChanged = true; // 남은 감전 횟수와 아이콘 갱신 예약
                    int applied = runtimeUnit.TakeUnblockedDamage(statusEffect.EffectiveValue, BattleDamageType.None, null); // 직접 카드 효과를 재호출하지 않는 방어도 우회 감전 피해
                    processResults.Add(CreateResult(statusEffect, round, applied, previousTurns, statusEffect.RemainingTurns, true, reason)); // 이번 감전 피해와 남은 적 턴 기록
                } // 감전 효과 적용 종료
                continue; // 일반 턴 시작 분기와 중복 처리 방지
            } // 감전의 피해와 지속 차감 종료
            if (statusEffect.EffectType == BattleStatusEffectType.DelayedHealing) // 진영 시작 때 소모되는 생명수의 예약 회복 확인
            { // 예약 효과 소모 및 회복 시작
                if (runtimeUnit.Team == BattleTeam.Ally && runtimeUnit.RemoveStatusEffectInstance(statusEffect)) // 실제 아군 턴의 생존 대상과 예약 제거 확인
                { // 소모한 예약의 지급 시작
                    int applied = runtimeUnit.RestoreHealth(statusEffect.EffectiveValue); // 직접 카드 보정과 구분한 예약 체력 회복
                    processResults.Add(CreateResult(statusEffect, round, applied, statusEffect.RemainingTurns, 0, true, BattleStatusEffectRemovalReason.DurationExpired)); // 상태 소모와 실제 회복 결과 기록
                    statusStateChanged = true; // 예약 상태 아이콘 제거 알림
                } // 소모한 예약의 지급 종료
                continue; // 같은 턴의 일반 상태 처리에서 재발동 차단
            } // 예약 효과 소모 및 회복 종료
            if (statusEffect.EffectType == BattleStatusEffectType.StethoscopeMark || statusEffect.EffectType == BattleStatusEffectType.PublicMark || statusEffect.EffectType == BattleStatusEffectType.Burn || statusEffect.EffectType == BattleStatusEffectType.Blind || statusEffect.EffectType == BattleStatusEffectType.AttackPowerDown) // 턴 종료에 처리하는 상태 확인
            { // 턴 종료 상태 유지 시작
                continue; // 턴 시작 지속 감소 생략
            } // 턴 종료 상태 유지 종료
            int previousRemainingTurns = statusEffect.RemainingTurns; // 처리 전 지속 횟수 저장
            int previousValue = statusEffect.EffectiveValue; // 처리 전 상태 전체 수치 저장
            int appliedAmount = 0; // 실제 적용 수치 초기화
            bool wasTriggered = false; // 발동 여부 초기화
            bool wasDefeated = false; // 같은 시점의 부활과 구분할 실제 전투불능 초기값
            if (statusEffect.EffectType == BattleStatusEffectType.Poison) // 중독 상태 확인
            { // 중독 처리 시작
                int bonus = !statusEffect.IsExpired && statusEffect.HasAlliedPoisonSource ? BattleSupplyRelics.Find(RelicEffectType.DriedCactusSapPoisonDamageBonus)?.EffectValue ?? 0 : 0; // 실제 활성 아군 중독 중첩에만 보유 수액의 고정 피해 가산
                var battle = runtimeUnit.DamageRelicBattle; // 중독 피해를 받는 유닛의 실제 진행 전투
                var reed = !statusEffect.IsExpired && runtimeUnit.Team == BattleTeam.Ally && battle is { CurrentRound: > 0, IsBattleEnded: false } // 활성 중독을 받는 실제 교전의 생존 아군 확인
                    ? battle.DamageRelicInventory?.FindEffect(RelicEffectType.WetReedKnotPoisonDamageReduction) : null; // 현재 전투에 보유한 젖은 갈대매듭 조회
                int reduction = System.Math.Max(0, reed?.EffectValue ?? 0); // 중독 피해에서 실제로 뺄 음수 없는 고정값
                int damage = (int)System.Math.Clamp((long)statusEffect.EffectiveValue + bonus - reduction, 0L, int.MaxValue); // 중독 수명과 수치를 유지한 실제 피해의 영점과 정수 상한
                BattleDamageResult damageResult = runtimeUnit.TakeDamage(damage, BattleDamageType.None); // 기존 일반 피해와 실제 방어도 정산
                appliedAmount = damageResult.AppliedDamage; // 실제 중독 피해 저장
                wasDefeated = damageResult.WasTargetDefeated; // 실제 중독 타격으로 쓰러진 결과 저장
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
            if (runtimeUnit.IsDead || wasDefeated) // 부활과 별개인 이번 지속 피해의 실제 전투불능 확인
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
    private static int ResolveSaltLeatherPeriodicDamage(int amount, BattleUnitRuntime target, BattleStatusEffectType type, int previousAdjustment) // 같은 양수 출혈과 화상의 서로 다른 유물 조정
    { // 소금가죽 지속 피해 보정 시작
        var battle = target.DamageRelicBattle; // 피해 상태의 실제 조우 확인
        int trade = amount > 0 && target.Team == BattleTeam.Ally && battle is { IsBattleEnded: false, HasPendingBossPhaseTransition: false } // 진행 중인 전투에서 양수 상태가 실제 발생한 아군 확인
            ? battle.DamageRelicInventory?.FindEffect(RelicEffectType.SaltLeatherFragmentPeriodicTrade)?.EffectValue ?? 0 : 0; // 같은 유물 원본의 하나뿐인 보정량
        int cord = type == BattleStatusEffectType.Burn && amount > 0 && target.Team == BattleTeam.Ally && battle is { IsBattleEnded: false } // 실제 진행 중의 아군 화상 원본 양수만 확인
            ? battle.DamageRelicInventory?.FindEffect(RelicEffectType.CharredStoneCordBurnDamageReduction)?.EffectValue ?? 0 : 0; // 같은 조우의 그을린 돌끈이 뺄 고정 양
        long adjustment = type == BattleStatusEffectType.Burn ? (long)trade - cord : -trade; // 소금가죽의 실제 화상·출혈 보정과 독립 화상 고정 감소
        return (int)System.Math.Clamp((long)amount + previousAdjustment + adjustment, 0L, int.MaxValue); // 원래 유물 보정과 합친 영 하한과 정수 상한
    } // 소금가죽 지속 피해 보정 종료

    public IReadOnlyList<BattleStatusEffectProcessResult> ProcessPhaseEnd(BattleUnitRuntime runtimeUnit, int round) // 진영 턴 종료 상태 처리
    { // 턴 종료 상태 처리 시작
        List<BattleStatusEffectProcessResult> results = new List<BattleStatusEffectProcessResult>(); // 처리 결과 목록 생성
        if (runtimeUnit == null || runtimeUnit.IsDead) // 유효한 생존 대상 확인
        { // 처리 불가 시작
            return results; // 빈 결과 반환
        } // 처리 불가 종료
        List<BattleStatusEffectInstance> snapshot = runtimeUnit.CreateStatusEffectSnapshot(); // 변경 대비 상태 목록 복사
        var healingIncrease = runtimeUnit.GetStatusEffect(BattleStatusEffectType.HealingReceivedUp); // 턴 종료 회복까지 적용할 잔잔한 축복 확보
        if (healingIncrease != null && snapshot.Remove(healingIncrease)) // 이번 턴의 회복 증가가 아직 적용 중인지 확인
        { // 실제 지속 회복 뒤에 종료할 상태 재배치 시작
            snapshot.Add(healingIncrease); // 기존 효과 순서를 유지한 마지막 회복 증가 만료
        } // 실제 지속 회복 뒤에 종료할 상태 재배치 종료
        RelicData radiatorReward = null; // 현재 유닛의 실제 화상에 맞춰 이번 턴 종료 뒤 부여할 강화
        foreach (BattleStatusEffectInstance effect in snapshot) // 대상 상태 순회
        { // 턴 종료 효과 확인 시작
            if (runtimeUnit.IsDead) // 이전 효과로 사망 확인
            { // 사망 처리 시작
                break; // 나머지 효과 처리 중단
            } // 사망 처리 종료
            if (effect.EffectType == BattleStatusEffectType.EternalShield || effect.EffectType == BattleStatusEffectType.EternalShieldResistance || // 적 턴 종료에만 차감하는 기존 영원의 방패
                effect.EffectType == BattleStatusEffectType.SanctuaryWard || effect.EffectType == BattleStatusEffectType.FearResistance || // 성역과 찬가의 실제 적 턴 종료 차감
                effect.EffectType == BattleStatusEffectType.PainDistribution || // 고통 분산의 두 실제 적 턴 종료만 계산
                effect.EffectType == BattleStatusEffectType.CanyonWindEvasionUp) // 아군 개별 종료와 무관한 실제 적 완료 차례 전용 회피
            { // 개별 유닛 턴의 지속 시간 처리 제외 시작
                continue; // 다음 상태 확인
            } // 개별 유닛 턴의 지속 시간 처리 제외 종료
            if (!runtimeUnit.ContainsStatusEffectInstance(effect) || effect.EffectType == BattleStatusEffectType.DuelMark || effect.EffectType == BattleStatusEffectType.BattlefieldResolve || effect.EffectType == BattleStatusEffectType.PhysicalRiposte || effect.WaitsForAction || effect.EffectType == BattleStatusEffectType.Evasion || effect.EffectType == BattleStatusEffectType.ShockAbsorption || effect.EffectType == BattleStatusEffectType.ResistanceStance || effect.EffectType == BattleStatusEffectType.ActionSpeedUp || effect.EffectType == BattleStatusEffectType.ShieldCounter || effect.EffectType == BattleStatusEffectType.RetributionBulwark || effect.EffectType == BattleStatusEffectType.GuardianCircle || effect.EffectType == BattleStatusEffectType.FocusedDefense || effect.EffectType == BattleStatusEffectType.BleedResistance || effect.EffectType == BattleStatusEffectType.UndyingGuard || effect.EffectType == BattleStatusEffectType.DistributedDefense || effect.IsProtectedMentalState || effect.EffectType == BattleStatusEffectType.StarReservedHealing || effect.EffectType == BattleStatusEffectType.StarPassiveReservedHealing || effect.EffectType == BattleStatusEffectType.ChariotMark || (effect.EffectType == BattleStatusEffectType.PublicMark && !effect.PublicMarkTurnTeam.HasValue) || effect.EffectType == BattleStatusEffectType.StethoscopeMark) // 기존 수신기는 아군 종료에만 해제하고 전술 공명 표식은 실제 대상의 자기 턴에 차감
            { // 처리 제외 시작
                continue; // 다음 상태 확인
            } // 처리 제외 종료
            if (effect.EffectType == BattleStatusEffectType.ReflectionCitadel) // 실제 적 턴 정산이 끝날 때 유지할 반사 상태 확인
            { // 반사의 성채 일반 지속 처리 제외 시작
                continue; // 다음 아군 턴 전에 반사 지급과 함께 제거
            } // 반사의 성채 일반 지속 처리 제외 종료
            if (effect.EffectType == BattleStatusEffectType.ArcaneWard) // 아군 마력 보호막의 적 턴 전용 지속 규칙 확인
            { // 일반 아군 턴 정리에서 마력 보호막 제외 시작
                continue; // 적이 공격을 마친 뒤 한 번만 기간 차감
            } // 일반 아군 턴 정리에서 마력 보호막 제외 종료
            if (effect.EffectType == BattleStatusEffectType.Shock && runtimeUnit.Team == BattleTeam.Enemy) // 실제 적 턴 시작에서 이미 횟수를 차감한 감전 확인
            { // 적 턴 종료의 감전 제외 시작
                continue; // 같은 적 턴에 두 번째 지속 감소 방지
            } // 적 턴 종료의 감전 제외 종료
            if (effect.EffectType == BattleStatusEffectType.ResurrectionSeed) // 적 턴 종료에서만 차감하는 씨앗 상태 확인
            { // 일반 부활 씨앗 기간 처리 제외 시작
                continue; // 아군 턴 정리에서는 씨앗 기간 유지
            } // 일반 부활 씨앗 기간 처리 제외 종료
            int previousTurns = effect.RemainingTurns; // 처리 전 지속 횟수 저장
            int previousValue = effect.EffectiveValue; // 처리 전 효과 수치 저장
            int applied = 0; // 실제 피해 초기화
            int healthBefore = runtimeUnit.CurrentHealth; // 같은 처리 시점에 체력 영이 되었는지 확인할 시작 체력
            bool renew = effect.EffectType == BattleStatusEffectType.Renewal && runtimeUnit.Team == BattleTeam.Ally; // 실제 아군 종료에 회복하는 별도 재생 상태 확인
            bool triggered = renew || effect.EffectType == BattleStatusEffectType.Burn || effect.EffectType == BattleStatusEffectType.Bleed; // 재생과 지속 피해의 턴 종료 발동 확인
            if (effect.EffectType == BattleStatusEffectType.Bleed) // 방어도를 우회하는 고정 체력 손실 확인
            { // 출혈 처리 시작
                int bonus = runtimeUnit.Team == BattleTeam.Enemy && previousValue > 0 ? BattleSupplyRelics.Find(RelicEffectType.EnemyBleedDamageBonus)?.EffectValue ?? 0 : 0; // 적이 실제로 받는 양수 출혈 틱의 유물 가산량
                bonus += previousValue > 0 && runtimeUnit.HasActiveCardDamageMark ? BattleSupplyRelics.Find(RelicEffectType.PortraitNailMarkedBleedDamage)?.EffectValue ?? 0 : 0; // 공통 표식이 남은 실제 출혈 틱의 추가 피해
                int damage = ResolveSaltLeatherPeriodicDamage(previousValue, runtimeUnit, BattleStatusEffectType.Bleed, bonus); // 기존 출혈 가산량과 받는 아군 피해 감소의 합계
                applied = runtimeUnit.TakeUnblockedDamage(damage, BattleDamageType.None, null); // 원래 출혈의 방어도 우회 체력 피해
            } // 출혈 처리 종료
            else if (effect.EffectType == BattleStatusEffectType.Burn) // 기존 화상 피해 확인
            { // 화상 피해 처리 시작
                int reduction = runtimeUnit.Team == BattleTeam.Ally ? BattleSupplyRelics.Find(RelicEffectType.ReducePeriodicBurnDamage)?.EffectValue ?? 0 : 0; // 실제 생존 아군 화상의 보유 유물 경감량
                int damage = ResolveSaltLeatherPeriodicDamage(previousValue, runtimeUnit, BattleStatusEffectType.Burn, -reduction); // 기존 화상 경감량과 받는 아군 피해 증가의 합계
                applied = runtimeUnit.TakeDamage(damage, BattleDamageType.None).AppliedDamage; // 실제 방어도와 피해 무효가 반영된 화상 체력 손실
                if (applied > 0 && applied < healthBefore && runtimeUnit.Team == BattleTeam.Ally && !runtimeUnit.IsDead && // 화상 피격 당시 생존 및 실제 양수 체력 손실 확인
                    runtimeUnit.DamageRelicBattle is { IsBattleEnded: false } battle) // 실제 화상을 받아 살아남은 같은 조우 아군의 양수 체력 손실
                { // 이번 유닛의 독립적인 방열판 확률 처리 시작
                    var relic = battle.DamageRelicInventory?.FindEffect(RelicEffectType.WasteHeatRadiatorBurnAttackUp); // 현재 전투에 등록한 폐열 방열판 확인
                    int chance = relic?.TriggerChancePercent ?? 0; // 기존 정수 확률 규칙과 같은 원본 범위
                    if (relic?.EffectValue > 0 && chance > 0 && (chance >= 100 || UnityEngine.Random.Range(0, 100) < chance)) // 한 번의 양수 화상마다 독립 성공 판정
                    { // 실제 기존 효과의 지속시간 처리 뒤 강화 예약 시작
                        radiatorReward = relic; // 이번 종료에 새 강화의 첫 기간 차감 방지
                    } // 실제 기존 효과의 지속시간 처리 뒤 강화 예약 종료
                } // 이번 유닛의 독립적인 방열판 확률 처리 종료
            } // 화상 피해 처리 종료
            else if (renew) // 생존 아군 자신의 실제 턴 종료인 경우
            { // 재생 지속 회복 시작
                applied = runtimeUnit.RestoreHealth(previousValue); // 받는 회복 보정과 체력 상한이 적용된 실제 재생
            } // 재생 지속 회복 종료
            if (runtimeUnit.IsDead || !renew && applied >= healthBefore) // 부활 이전 지속 피해의 실제 체력 영 확인
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
                runtimeUnit.RemoveStatusEffectInstance(effect, expiredFromActive: previousTurns > 0); // 활성 상태에서 실제로 만료되었는지 포함한 제거
                reason = BattleStatusEffectRemovalReason.DurationExpired; // 만료 원인 기록
            } // 상태 만료 처리 종료
            results.Add(new BattleStatusEffectProcessResult(effect.EffectType, round, previousValue, applied, previousTurns, effect.RemainingTurns, effect.StackCount, triggered, reason)); // 턴 종료 처리 결과 기록
        } // 턴 종료 효과 확인 종료
        if (radiatorReward != null && !runtimeUnit.IsDead) // 이번 종료에 실제 화상을 받은 뒤 아직 생존한 대상 확인
        { // 다음 두 아군 차례의 독립 공격력 강화 시작
            runtimeUnit.ApplyStatusEffect(BattleStatusEffectType.WasteHeatAttackPowerUp, radiatorReward.EffectValue, radiatorReward.StatusDuration, radiatorReward.StatusMaximumStacks); // 기존 공격력 강화와 겹쳐도 자기 자신만 한 중첩 갱신
        } // 다음 두 아군 차례의 독립 공격력 강화 종료
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
