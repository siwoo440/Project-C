internal static class ShieldCardCombat // 겨울몰락 방패 카드의 복합 전투 효과
{ // 방패 효과 시작
    internal static int ApplyFortify(CardInstance card, BattleUnitRuntime ally, BattleUnitView view) // 요새화의 물리 방어와 독립 마법 피해 경감 적용
    { // 요새화 적용 시작
        if (!IsLivingAlly(ally)) // 생존 아군의 실제 피격 상태 확인
        { // 유효 대상 없음 처리 시작
            return 0; // 적용할 요새화 없음
        } // 유효 대상 없음 처리 종료
        bool fortified = ApplyStatus(card, ally, view, BattleStatusEffectType.PhysicalDefenseUp, card.EffectValue); // 기존 물리 방어력 강화 적용
        ApplyStatus(card, ally, view, BattleStatusEffectType.FortifyResistance, card.SourceData.SecondaryEffectValue); // 한 턴 저항 자세와 구분한 마법 피해 경감
        return fortified ? card.EffectValue : 0; // 실제 적용한 물리 방어력 기록
    } // 요새화 적용 종료

    internal static int ApplyIndomitableFront(CardInstance card, BattleUnitRuntime ally, BattleUnitView view) // 불굴의 전열의 방어도와 공포 해제 적용
    { // 불굴의 전열 적용 시작
        if (!IsLivingAlly(ally)) // 생존 아군만 방어 효과 적용 확인
        { // 유효 대상 없음 처리 시작
            return 0; // 적용할 방어도 없음
        } // 유효 대상 없음 처리 종료
        int block = ally.GainBlock(card.EffectValue); // 각 생존 아군의 실제 방어도 획득
        if (ally.RemoveStatusEffect(BattleStatusEffectType.Fear)) // 대상의 공포 상태만 해제 성공 확인
        { // 공포 정화 표시 시작
            view?.ShowStatusFeedback(BattleStatusEffectInstance.GetDisplayName(BattleStatusEffectType.Fear) + " 해제", false); // 실제 제거한 공포의 피드백
        } // 공포 정화 표시 종료
        return block; // 실제 적용한 개별 방어도 기록
    } // 불굴의 전열 적용 종료

    internal static int ApplyImpenetrable(CardInstance card, BattleUnitRuntime ally, BattleUnitView view) // 난공불락의 아군별 방어도와 피해 무효 적용
    { // 난공불락 적용 시작
        if (!IsLivingAlly(ally)) // 살아 있는 개별 보호 대상 확인
        { // 유효 대상 없음 처리 시작
            return 0; // 적용할 난공불락 없음
        } // 유효 대상 없음 처리 종료
        int block = ally.GainBlock(card.EffectValue); // 아군별(공용이 아닌) 방어도 획득
        ApplyStatus(card, ally, view, BattleStatusEffectType.DamageNullification, card.SourceData.SecondaryEffectValue); // 절대 가드와 동일한 다음 피해 무효 한 번
        return block; // 아군의 실제 방어도 획득량 기록
    } // 난공불락 적용 종료

    internal static int ApplyEternalShield(CardInstance card, BattleUnitRuntime ally, BattleUnitView view) // 영원의 방패 직접 피해 경감과 마법 저항 적용
    { // 영원의 방패 적용 시작
        if (!IsLivingAlly(ally)) // 생존 아군 대상 여부 확인
        { // 유효한 보호 대상 없음 처리 시작
            return 0; // 적용 가능한 경감 없음 반환
        } // 유효한 보호 대상 없음 처리 종료
        bool applied = ApplyStatus(card, ally, view, BattleStatusEffectType.EternalShield, card.EffectValue); // 직접 피해 경감과 개별 적용 결과
        ApplyStatus(card, ally, view, BattleStatusEffectType.EternalShieldResistance, card.SourceData.SecondaryEffectValue); // 마법 피해 경감의 별도 상태 적용
        return applied ? card.EffectValue : 0; // 직접 피해 경감의 적용 결과 기록
    } // 영원의 방패 적용 종료

    private static bool IsLivingAlly(BattleUnitRuntime unit) // 효과 정산 순간의 아군 생존 여부
    { // 아군 대상 확인 시작
        return unit != null && !unit.IsDead && unit.Team == BattleTeam.Ally; // 살아 있는 아군만 유효
    } // 아군 대상 확인 종료

    private static bool ApplyStatus(CardInstance card, BattleUnitRuntime ally, BattleUnitView view, BattleStatusEffectType status, int amount) // 복합 방패 카드의 개별 상태 적용
    { // 상태 적용 시작
        BattleStatusEffectApplyResult result = ally.ApplyStatusEffect(status, amount, card.StatusDuration, card.StatusMaximumStacks, card.OwnerUnit); // 카드 기간과 중첩 한도 및 시전자 연결
        view?.ShowStatusApplyFeedback(status, result); // 기존 상태 적용 결과 피드백 표시
        return result == BattleStatusEffectApplyResult.Applied || result == BattleStatusEffectApplyResult.Stacked; // 실제 상태 적용 성공 여부
    } // 상태 적용 종료

    internal static string GetEffectDetail(CardInstance card) // 동적 방패 수치와 실제 발동 범위의 상세 문구
    { // 방패 상세 문구 시작
        int value = card.EffectValue; // 강화 단계를 반영한 실제 기본 수치
        int secondary = card.SourceData.SecondaryEffectValue; // 원본에 설정된 실제 후속 수치
        switch (card.SourceData.ShieldRule) // 복합 방패 카드 종류별 실제 효과 표시
        { // 복합 방패 문구 시작
            case ShieldCardRule.Fortify: // 물리 방어력과 마법 피해 경감 효과 확인
                return $"아군 각각 물리 방어력 +{value} · 적 직접 마법 피해 -{secondary}% ({card.StatusDuration}턴)"; // 적용 대상과 두 수치의 공통 기간 표시
            case ShieldCardRule.IndomitableFront: // 각 아군의 방어도와 공포 제거
                return $"아군 각각 방어도 +{value} · 공포 해제"; // 방어도 분배와 제거 대상 표시
            case ShieldCardRule.Impenetrable: // 각 아군의 방어도와 피해 무효
                return $"아군 각각 방어도 +{value} · 다음 피해 1회 무효"; // 공용이 아닌 개별 보호 횟수 표시
            case ShieldCardRule.GiantsShield: // 소유자의 방어도와 다음 공용 행동력
                return $"자신 방어도 +{value} · 다음 아군 턴 공용 행동력 -{secondary}"; // 실제 방어도와 다음 턴 불이익 표시
        } // 복합 방패 문구 종료
        return card.StatusEffectType switch // 기존 상태로 적용되는 방패 카드 상세
        { // 개별 방어 상태 문구 시작
            BattleStatusEffectType.DamageNullification => "다음 피해 1회 무효 · 방어도 유지", // 실제 피해 없이 무효화되는 보호 표시
            BattleStatusEffectType.RetributionBulwark => $"이번 적 턴에 방어도나 체력 손실을 일으킨 공격자에게 고정 피해 {value}", // 반격 조건과 실제 고정 피해 표시
            BattleStatusEffectType.DistributedDefense => $"적 광역 공격 피해 -{value}% ({card.StatusDuration}턴)", // 실제 광역 공격 조건과 지속 표시
            _ => null // 다른 카드의 기존 상세 처리 유지
        }; // 개별 방어 상태 문구 종료
    } // 방패 상세 문구 종료
} // 방패 효과 종료
