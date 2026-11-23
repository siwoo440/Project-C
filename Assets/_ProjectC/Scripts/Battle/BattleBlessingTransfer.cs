internal static class BattleBlessingTransfer // 전투 참가 아군 간 강화 복사
{ // 축복 이전 처리 시작
    internal static bool IsTransferCard(CardInstance card) // 축복 이전에 필요한 원본 규칙과 대상 확인
    { // 복사 카드 종류 조회 시작
        return card.TargetType == CardTargetType.BlessingSourceAndAlly && card.EffectType == CardEffectType.CopyAllyBlessing && card.SourceData.CupRule == CupCardRule.BlessingTransfer; // 올바른 대상과 실제 복사 효과 조합 확인
    } // 복사 카드 종류 조회 종료

    internal static bool HasTargets(BattleTurnRuntime turns) // 카드를 사용할 실제 아군 두 명 존재 확인
    { // 축복 이전의 첫 대상 조회 시작
        foreach (var source in turns.BlessingAllies) // 현재 참가 아군 순서대로 확인
        { // 강화 복사 가능한 아군 확인 시작
            if (CanChooseSource(turns, source)) // 다른 아군에게 복사할 강화 여부 확인
            { // 유효한 대상 조합 확인 시작
                return true; // 축복 이전 사용 가능 반환
            } // 유효한 대상 조합 확인 종료
        } // 강화 복사 가능한 아군 확인 종료
        return false; // 강화와 수령자 조합 없음 반환
    } // 축복 이전의 첫 대상 조회 종료

    internal static bool CanChooseSource(BattleTurnRuntime turns, BattleUnitRuntime source) // 복사 가능한 강화를 보유한 첫 대상 검사
    { // 강화 보유자 선택 검사 시작
        if (!turns.CanSelectLivingUnit(source, BattleTeam.Ally)) // 현재 전투의 생존 아군 여부 확인
        { // 첫 대상 거절 시작
            return false; // 약화 대상 또는 전투 밖 유닛 선택 차단
        } // 첫 대상 거절 종료
        foreach (var recipient in turns.BlessingAllies) // 다른 수령 후보 차례로 확인
        { // 수령 대상 후보 검사 시작
            if (FindBlessing(turns, source, recipient) != null) // 손실 없이 복사할 실제 강화 확인
            { // 복사 가능한 후보 발견 시작
                return true; // 첫 대상 선택 허용
            } // 복사 가능한 후보 발견 종료
        } // 수령 대상 후보 검사 종료
        return false; // 받을 다른 생존 아군이 없는 첫 대상 차단
    } // 강화 보유자 선택 검사 종료

    internal static BattleStatusEffectInstance FindBlessing(BattleTurnRuntime turns, BattleUnitRuntime source, BattleUnitRuntime recipient) // 적용 순서의 첫 호환 강화 조회
    { // 강화 복사 조건 확인 시작
        if (source == recipient || !turns.CanSelectLivingUnit(source, BattleTeam.Ally) || !turns.CanSelectLivingUnit(recipient, BattleTeam.Ally)) // 같은 대상과 생존 및 참가 명단 검사
        { // 복사 불가능 대상 처리 시작
            return null; // 복사할 상태 없음 반환
        } // 복사 불가능 대상 처리 종료
        foreach (var status in source.StatusEffects) // 원본 강화 부여 순서 순회
        { // 개별 강화 검사 시작
            if (!status.IsExpired && !source.IsTransientTurnBuff(status) && IsCopyableType(status.EffectType) && recipient.CanReceiveBlessingCopy(status)) // 활성 독립 강화와 수령 가능 중첩 확인
            { // 첫 호환 강화 반환 시작
                return status; // 실제 원본을 변경 없이 복사 대상으로 지정
            } // 첫 호환 강화 반환 종료
        } // 개별 강화 검사 종료
        return null; // 두 아군이 공유할 수 있는 강화 없음 반환
    } // 강화 복사 조건 확인 종료

    internal static bool IsCopyableType(BattleStatusEffectType type) // 소유자 연결이 없는 일반 기간제 강화 분류
    { // 일반 강화 분류 시작
        return type is BattleStatusEffectType.Regeneration or BattleStatusEffectType.AttackPowerUp or BattleStatusEffectType.StatusImmunity // 지속 재생과 공격력 및 디버프 면역
            or BattleStatusEffectType.PhysicalDefenseUp or BattleStatusEffectType.MagicalResistanceUp or BattleStatusEffectType.CriticalChanceUp // 물리 방어와 마법 저항 및 치명타 확률
            or BattleStatusEffectType.Evasion or BattleStatusEffectType.ActionSpeedUp or BattleStatusEffectType.HealingReceivedUp or BattleStatusEffectType.Renewal // 회피와 속도 및 지속 회복 강화
            or BattleStatusEffectType.BloodOath or BattleStatusEffectType.FinishingStance or BattleStatusEffectType.BladeFrenzy or BattleStatusEffectType.SwordSaintStance // 실제 보유자에게 작동하는 지속 검 강화
            or BattleStatusEffectType.ShockAbsorption or BattleStatusEffectType.ResistanceStance or BattleStatusEffectType.FortifyResistance or BattleStatusEffectType.FocusedDefense // 독립 고정 피해 경감과 마법 저항 및 집중 방어
            or BattleStatusEffectType.GuardianCircle or BattleStatusEffectType.BleedResistance or BattleStatusEffectType.FearResistance or BattleStatusEffectType.DistributedDefense or BattleStatusEffectType.SanctuaryWard // 독립 보호와 부여 저항 및 광역 방어
            or BattleStatusEffectType.EternalShield or BattleStatusEffectType.EternalShieldResistance or BattleStatusEffectType.ShieldCounter or BattleStatusEffectType.RetributionBulwark; // 다회 사용 경감과 고정 반격
    } // 일반 강화 분류 종료
} // 축복 이전 처리 종료
