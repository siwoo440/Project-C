using System.Collections.Generic; // 전투 대상 목록 사용
using UnityEngine; // 무작위 전투 대상 선택

public sealed partial class BattleCardActionController // 실제 카드 행동 관리자의 일부
{ // 부분 동작 시작
    private bool CanConfirmSelectedEnemyPair(CardInstance card, IReadOnlyList<BattleUnitRuntime> targets) // 결제 전에 확인할 골라 놓은 적 조합
    { // 선택을 시작한 순간의 적 수와 현재 조건 비교 시작
        if (card.EffectType != CardEffectType.Damage || card.OwnerUnit.IsDead || requiredSelectedEnemies < 1 || targets.Count != requiredSelectedEnemies || // 원본 효과와 소유자 및 처음 필요한 적 수 확인
            !turnRuntime.CanSelectLivingUnit(targets[0], BattleTeam.Enemy)) // 실제 전투에 남아 있는 첫 생존 적 확인
        { // 미확정 대상 처리 시작
            return false; // 행동력 결제와 카드 사용 거절 반환
        } // 미확정 대상 처리 종료
        return targets.Count == 1 || targets[0] != targets[1] && turnRuntime.CanSelectLivingUnit(targets[1], BattleTeam.Enemy); // 한 대상 허용 상태 또는 서로 다른 두 생존 적 확인
    } // 선택을 시작한 순간의 적 수와 현재 조건 비교 종료
    private bool IsValidTarget(CardInstance cardInstance, BattleUnitRuntime targetUnit) // 카드 대상 유효성 검사
    { // 대상 검사 시작
        if (cardInstance == null || targetUnit == null) // 카드와 대상 연결 확인
        { // 선택 불가 처리 시작
            return false; // 잘못된 대상 반환
        } // 선택 불가 처리 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.RuneAmplification) // 유효한 기간제 강화 아군을 고르는 실제 카드
        { // 선택과 드롭 양쪽의 같은 아군 대상 검사 시작
            return turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Ally) && targetUnit.HasRuneAmplifiableBuff(); // 현재 생존 아군의 연장할 수 있는 실제 강화 확인
        } // 선택과 드롭 양쪽의 같은 아군 대상 검사 종료
        if (cardInstance.SourceData.PentacleRule == PentacleCardRule.RunePower) // 실제 손패에 다른 유료 고정 카드가 있는 아군인지 확인
        { // 룬 카드의 정상 지정 아군 검사 시작
            return turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Ally) && GetRunePowerCandidates(cardInstance, targetUnit).Count > 0; // 실제 생존 아군과 비어 있지 않은 본인 손패 후보 확인
        } // 룬 카드의 정상 지정 아군 검사 종료
        if (cardInstance.TargetType == CardTargetType.FallenAlly || cardInstance.EffectType == CardEffectType.ReviveAlly) // 전투불능 아군 전용 카드 조합 확인
        { // 부활 대상 검사 시작
            return cardInstance.TargetType == CardTargetType.FallenAlly && cardInstance.EffectType == CardEffectType.ReviveAlly && turnRuntime.CanReviveFallenAlly(targetUnit); // 정확한 부활 효과와 현재 명단 검사 결과 반환
        } // 부활 대상 검사 종료
        if (targetUnit.IsDead) // 일반 카드 대상의 생존 여부 확인
        { // 일반 카드 전투불능 대상 차단 시작
            return false; // 일반 카드 대상으로 선택 불가
        } // 일반 카드 전투불능 대상 차단 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.EmberSpread) // 불씨 확산의 화상 적 전용 대상 확인
        { // 불씨 확산 대상 검사 시작
            return cardInstance.TargetType == CardTargetType.SingleEnemy && turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy) && // 실제 선택할 수 있는 생존 적 한 명 확인
                targetUnit.GetStatusEffect(BattleStatusEffectType.Burn) is { IsExpired: false }; // 만료되지 않은 화상 상태 확인
        } // 불씨 확산 대상 검사 종료
        var requiredState = RequiredWandEnemyStatus(cardInstance.SourceData.WandRule); // 선택과 드롭이 공유하는 원소 상태 종류
        if (requiredState != BattleStatusEffectType.None) // 기존 활성 상태 조건을 사용하는 카드 확인
        { // 원소 카드의 적 후보 검사 시작
            return turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy) && targetUnit.GetStatusEffect(requiredState) is { IsExpired: false }; // 현재 등록된 생존 적과 유지 중인 상태 확인
        } // 원소 카드의 적 후보 검사 종료
        if (cardInstance.SourceData.WandRule == WandCardRule.FateFreeze) // 운명 동결의 실제 단일 적 지정 확인
        { // 동결 가능한 적 확인 시작
            return turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy); // 현재 전투의 생존 적 한 명만 허용
        } // 동결 가능한 적 확인 종료
        if (cardInstance.TargetType == CardTargetType.BlessingSourceAndAlly) // 두 아군의 축복 이전 선택 확인
        { // 축복 이전 단계별 대상 검사 시작
            return firstSequentialTarget == null ? BattleBlessingTransfer.CanChooseSource(turnRuntime, targetUnit) : // 강화 보유자 선택 검사
                BattleBlessingTransfer.FindBlessing(turnRuntime, firstSequentialTarget, targetUnit) != null; // 두 번째로 고른 아군의 실제 강화 수령 가능 여부
        } // 축복 이전 단계별 대상 검사 종료
        if (cardInstance.TargetType == CardTargetType.SelectedTwoEnemies) // 서리 파편에서 지정할 생존 적 확인
        { // 현재 적 클릭 유효성 확인 시작
            return cardInstance.EffectType == CardEffectType.Damage && turnRuntime.CanSelectLivingUnit(targetUnit, BattleTeam.Enemy); // 실제 전투 명단의 생존 적만 입력 허용
        } // 현재 적 클릭 유효성 확인 종료
        if (cardInstance.TargetType == CardTargetType.EnemyAndAlly) // 순서가 정해진 적과 아군의 선택 검사
        { // 현재 선택 단계 검사 시작
            BattleTeam team = firstSequentialTarget == null ? BattleTeam.Enemy : BattleTeam.Ally; // 첫 적 지정 전후의 대상 진영 구분
            return cardInstance.EffectType == CardEffectType.Damage && turnRuntime.CanSelectLivingUnit(targetUnit, team); // 자기 자신을 포함한 해당 진영의 생존 참가자 확인
        } // 현재 선택 단계 검사 종료
        if ((cardInstance.SourceData.SwordRule == SwordCardRule.BloodTrail || cardInstance.SourceData.SwordRule == SwordCardRule.BloodBackflow) && targetUnit.GetStatusEffect(BattleStatusEffectType.Bleed) == null) // 대상의 현재 출혈 여부 확인
        { // 추적 대상 제한 시작
            return false; // 출혈 없는 적의 선택 차단
        } // 추적 대상 제한 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.JudgmentSlash && targetUnit.FirstRemovableBuff() == null) // 단죄로 제거할 일반 강화 확인
        { // 단죄 대상 제외 시작
            return false; // 제거할 강화 없는 대상 입력 차단
        } // 단죄 대상 제외 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.Finisher && !SwordCardCombat.IsFinisherTarget(targetUnit)) // 선택한 적의 현재 체력 재검사
        { // 처형 대상 제한 시작
            return false; // 체력 오분의 일을 넘는 적 선택 차단
        } // 처형 대상 제한 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.ExecutionSlash && !SwordCardCombat.IsFinisherTarget(targetUnit, 2)) // 처형 베기의 실제 대상 체력 확인
        { // 처형 베기 제한 시작
            return false; // 체력이 절반보다 많은 적 선택 차단
        } // 처형 베기 제한 종료
        if (DevilBloodPrice.IsBloodPriceCard(cardInstance) && !DevilBloodPrice.CanPay(cardInstance)) // 악마 전용 카드의 체력 비용 확인
        { // 체력 비용 지불 불가 시작
            return false; // 선택 불가 반환
        } // 체력 비용 지불 불가 종료
        if ((cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) && !targetUnit.HasRemovableDebuff && // 제거할 디버프가 없는 정화 대상 확인
            !(cardInstance.SourceData.CupRule == CupCardRule.PurifyingTouch && cardInstance.SourceData.SecondaryEffectValue > 0)) // 정화할 약화가 없어도 적용할 저항 효과 확인
        { // 정화 불필요 처리 시작
            return false; // 정화 대상 불가 반환
        } // 정화 불필요 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect && cardInstance.StatusEffectType == BattleStatusEffectType.None) // 상태 이상 카드 설정 확인
        { // 상태 설정 오류 처리 시작
            return false; // 상태 이상 대상 불가 반환
        } // 상태 설정 오류 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental && !targetUnit.CanChangeMental(cardInstance.MentalChangeValue) && // 정신력이 실제로 바뀌지 않는 대상 확인
            !(cardInstance.SourceData.FollowUpEffect == CardFollowUpEffect.DrawCards && runtimeDeck.HasDrawableCards) && // 기존 보조 드로우 가능 여부 확인
            !(cardInstance.SourceData.CupRule == CupCardRule.Calming && targetUnit.GetStatusEffect(BattleStatusEffectType.Fear) != null)) // 정신력 상한에서도 제거할 공포 확인
        { // 정신력 변경 불가 처리 시작
            return false; // 정신력 대상 불가 반환
        } // 정신력 변경 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.RecoverDiscard && runtimeDeck.DiscardPileCount == 0) // 회수할 버린 카드 존재 확인
        { // 회수 불가 처리 시작
            return false; // 선택 불가 반환
        } // 회수 불가 처리 종료
        if (!CanUseDrawInspection(cardInstance, runtimeDeck)) // 유물이 반영된 덱 위 확인 가능 여부 검사
        { // 선택 불가 처리 시작
            return false; // 선택 불가 반환
        } // 선택 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard && !runtimeDeck.HasDrawableCards && runtimeDeck.HandCount <= 1) // 드로우와 사용 후 남을 손패 확인
        { // 드로우와 버리기 불가 시작
            return false; // 선택 불가 반환
        } // 드로우와 버리기 불가 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack && !CanGuardAllyWithCard(cardInstance, targetUnit)) // 이 카드로 보호할 동료의 적격 여부 확인
        { // 동료 보호 불가 시작
            return false; // 선택 불가 반환
        } // 동료 보호 불가 종료
        if (cardInstance.EffectType == CardEffectType.RerollRandomAttack && !enemyActions.CanRerollRandomAttack(targetUnit)) // 적 무작위 공격 재굴림 가능 확인
        { // 재굴림 불가 처리 시작
            return false; // 선택 불가 반환
        } // 재굴림 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.ShareAllyAttack && !enemyActions.CanShareAllyAttack(cardInstance.OwnerUnit, targetUnit)) // 동료의 예정 공격 분담 가능 확인
        { // 피해 분담 불가 처리 시작
            return false; // 선택 불가 반환
        } // 피해 분담 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.TransferAllyDebuff && BattleDebuffTransfer.GetChoices(targetUnit, cardInstance.OwnerUnit).Count == 0) // 동료의 이전 가능한 디버프 확인
        { // 디버프 이전 불가 처리 시작
            return false; // 선택 불가 반환
        } // 디버프 이전 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.SelectPartner && !loversPartner.CanSetPartner(cardInstance.OwnerUnit, targetUnit)) // 생존한 다른 파트너 후보 확인
        { // 파트너 지정 불가 처리 시작
            return false; // 선택 불가 반환
        } // 파트너 지정 불가 처리 종료
        if (cardInstance.EffectType == CardEffectType.ProtectAttackTarget && (targetUnit.Team != BattleTeam.Ally || !HasIncomingAttack(targetUnit))) // 공격 예고 보호 대상 확인
        { // 보호 불가 대상 처리 시작
            return false; // 선택 불가 반환
        } // 보호 불가 대상 처리 종료
        if (cardInstance.EffectType == CardEffectType.RedirectAttackAndDefend && cardInstance.SourceData.ShieldRule != ShieldCardRule.Taunt) // 한 적의 예정 공격을 바꾸는 기존 황제 카드 확인
        { // 공격 유도 대상 확인 시작
            if (targetUnit.Team != BattleTeam.Enemy || !enemyActions.CanRedirectAttack(targetUnit, cardInstance.OwnerUnit)) // 실제 공격 유도 가능 여부 확인
            { // 공격 유도 불가 처리 시작
                return false; // 선택 불가 반환
            } // 공격 유도 불가 처리 종료
        } // 공격 유도 대상 확인 종료
        switch (cardInstance.TargetType) // 카드 대상 종류 분기
        { // 대상 분기 시작
            case CardTargetType.Self: // 자신 대상 종류
                return targetUnit == cardInstance.OwnerUnit; // 카드 소유자 여부 반환
            case CardTargetType.LowestHealthAlly: // 현재 체력과 파티 순서로 자동 결정한 대상
                return targetUnit == turnRuntime.GetLowestHealthLivingAlly(); // 같은 생존 자동 대상만 실제 사용 허용
            case CardTargetType.SingleAlly: // 단일 아군 대상 종류
            case CardTargetType.AllAllies: // 전체 아군 대상 종류
                return targetUnit.Team == BattleTeam.Ally; // 아군 여부 반환
            case CardTargetType.SingleEnemy: // 단일 적 대상 종류
            case CardTargetType.RandomTwoEnemies: // 서로 다른 생존 적을 최대 두 명 고르는 대상 종류
            case CardTargetType.AllEnemies: // 전체 적 대상 종류
                return targetUnit.Team == BattleTeam.Enemy; // 적 여부 반환
            default: // 알 수 없는 대상 종류
                return false; // 잘못된 대상 반환
        } // 대상 분기 종료
    } // 대상 검사 종료
    private static IReadOnlyList<BattleUnitRuntime> PickDistinctRandomEnemies(IReadOnlyList<BattleUnitRuntime> candidates) // 무작위 전격의 서로 다른 최대 두 대상 선택
    { // 두 명의 무작위 적 선택 시작
        if (candidates.Count <= 2) // 두 명 이하로 모든 후보가 결과에 확정되는 경우 확인
        { // 확정된 적 모두 사용 시작
            return candidates; // 같은 적 반복이나 불필요한 난수 소비 없는 기존 순서 보존
        } // 확정된 적 모두 사용 종료
        int first = RelicRandomCardTarget.PickIndex(candidates.Count, count => UnityEngine.Random.Range(0, count)); // 기존 무작위 대상 유물 규칙을 지키는 첫 후보 위치
        int second = RelicRandomCardTarget.PickIndex(candidates.Count - 1, count => UnityEngine.Random.Range(0, count)); // 첫 후보를 제외한 나머지 후보 위치 선택
        if (second >= first) // 첫 후보의 자리를 건너뛸 위치 확인
        { // 두 번째 실제 후보 위치 보정 시작
            second++; // 누락된 첫 후보를 넘어 원래 후보 목록의 위치 복원
        } // 두 번째 실제 후보 위치 보정 종료
        return new[] { candidates[first], candidates[second] }; // 두 번의 명중 판정을 받을 서로 다른 생존 적 확정
    } // 두 명의 무작위 적 선택 종료

    private bool HasIncomingAttack(BattleUnitRuntime allyUnit) // 예정 공격 대상 여부 확인
    { // 공격 예고 확인 시작
        foreach (BattleEnemyAction action in enemyActions.PlannedActions) // 예정 적 행동 순회
        { // 공격 대상 검사 시작
            if (action.IsExecutable && action.ActionType == EnemyActionType.Attack && action.Target == allyUnit) // 실행 가능한 공격 예고 확인
            { // 공격 예고 발견 시작
                return true; // 보호 대상 반환
            } // 공격 예고 발견 종료
        } // 공격 대상 검사 종료
        return false; // 공격 예고 없음 반환
    } // 공격 예고 확인 종료
    private void UpdateTargetHighlights() // 선택 가능 대상 강조
    { // 대상 강조 시작
        UpdateTargetHighlights(allyUnitViews); // 아군 대상 강조 갱신
        UpdateTargetHighlights(enemyUnitViews); // 적 대상 강조 갱신
    } // 대상 강조 종료
    private void UpdateTargetHighlights(IReadOnlyList<BattleUnitView> unitViews) // 유닛 목록 대상 강조
    { // 목록 강조 시작
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 유닛 강조 시작
            bool targetable = unitView != null && unitView.RuntimeUnit != firstSequentialTarget && IsValidTarget(selectedCard, unitView.RuntimeUnit); // 아직 고르지 않은 현재 단계의 유효 대상 확인
            if (unitView != null) // 유닛 화면 존재 확인
            { // 대상 강조 적용 시작
                unitView.SetTargetable(targetable); // 대상 강조 적용
            } // 대상 강조 적용 종료
        } // 유닛 강조 종료
    } // 목록 강조 종료
    private List<BattleUnitRuntime> CollectValidTargets(CardInstance cardInstance, IReadOnlyList<BattleUnitView> unitViews) // 유효 대상 목록 생성
    { // 유효 대상 생성 시작
        List<BattleUnitRuntime> targetUnits = new List<BattleUnitRuntime>(); // 빈 대상 목록 생성
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 순회
        { // 유효 대상 검사 시작
            if (unitView != null && IsValidTarget(cardInstance, unitView.RuntimeUnit)) // 유효 유닛 확인
            { // 유효 유닛 등록 시작
                targetUnits.Add(unitView.RuntimeUnit); // 유효 대상 목록 등록
            } // 유효 유닛 등록 종료
        } // 유효 대상 검사 종료
        return targetUnits; // 유효 대상 목록 반환
    } // 유효 대상 생성 종료
    private bool CanGuardAllyWithCard(CardInstance card, BattleUnitRuntime target) // 기존 힘 카드와 방패 카드의 다른 보호 조건 조회
    { // 보호 조건 조회 시작
        return card.SourceData.ShieldRule switch // 기본 보호와 방패의 직접 공격 보호 규칙 구분
        { // 보호 카드 종류 분기 시작
            ShieldCardRule.BodyGuard or ShieldCardRule.GuardianHeart => enemyActions.CanBodyGuard(card.OwnerUnit, target), // 이후 대신 받을 수 있는 다른 생존 동료 확인
            ShieldCardRule.ShieldOath => enemyActions.CanShieldOath(card.OwnerUnit, target), // 두 적 턴 동안 보호할 생존 동료 확인
            _ => enemyActions.CanProtectAlly(card.OwnerUnit, target) // 예정 공격을 바로 바꾸는 기존 힘 카드 확인
        }; // 보호 카드 종류 분기 종료
    } // 보호 조건 조회 종료
} // 부분 동작 종료
