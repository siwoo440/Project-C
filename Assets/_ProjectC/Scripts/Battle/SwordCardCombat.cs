using System; // 피해 상한과 비율 계산
using System.Collections.Generic; // 실제 적 배치 범위 사용
using UnityEngine; // 게임 난수와 범위 제한 사용

public enum SwordCardRule // 검 카드 공격별 추가 규칙
{ // 규칙 목록 시작
    None, // 기존 일반 효과
    CriticalStrike, // 이번 공격의 치명타 확률 증가
    SwordDance, // 선택한 두 칸에서 세 번 추첨
    Charge, // 피격 대상의 다음 턴 방어 감소
    Counterattack, // 받은 실제 체력 손실의 절반 반격
    Finisher, // 남은 행동력으로 약해진 적 공격
    FlameSlash, // 명중한 적에게 화상 부여
    DoomBlade, // 소유자 체력 비용 지불
    Thrust, // 출혈을 일으키는 찌르기
    HorizontalSlash, // 이웃한 두 칸 가로베기
    KneeSlash, // 적 다음 행동 취소
    PommelStrike, // 적 한 턴 기절
    BloodEdge, // 출혈 중인 적 추가 피해
    ShieldSplitter, // 대상의 물리 방어 감소
    InterceptSlash, // 대상의 다음 공격 약화
    CheckSlash, // 소유자의 한 턴 회피
    ExecutionSlash, // 체력 절반 이하의 적에게만 사용하는 검
    PiercingLunge, // 대상의 임시 방어도 일부를 건너뛰는 검
    TwinBladeFlurry, // 모든 생존 적에서 네 번 재추첨하는 검
    BloodOath, // 생존 체력을 지불하는 검 피해 강화
    IntimidatingCharge, // 명중 후 확률로 공포를 주는 공격
    BladeWhirlwind, // 선택한 적과 좌우 한 칸 공격
    BloodTrail, // 출혈이 있는 적에게만 쓰는 공격
    SeveringStrike, // 대상이 받는 회복량 감소
    RageRelease, // 공격력 강화와 자신의 물리 방어 감소
    ExecutionerSword, // 직접 처치 뒤 공용 행동력 회복
    BattlefieldResolve, // 사용 직후와 다음 아군 턴 시작 방어도 지급
    BloodBackflow, // 출혈이 있는 생존 적 전체 즉시 공격
    JudgmentSlash, // 강화가 있는 적 공격과 강화 하나 해제
    KingsSlash, // 전체 적 타격 후 최대 체력이 가장 높은 생존 적 재공격
    BloodstainedDuel, // 시전자와 지정 적의 결투 표식
    LastBreakthrough // 직접 처치 후 다음 생존 적 한 번 공격
} // 규칙 목록 종료

internal static class SwordCardCombat // 검 카드 비용과 대상 및 직접 타격 계산
{ // 검 카드 전투 규칙 시작
    internal static bool IsFinisherTarget(BattleUnitRuntime target, int healthDivisor = 5) // 남은 체력 오분의 일 이하의 적 확인
    { // 처형 대상 검사 시작
        return target != null && !target.IsDead && target.Team == BattleTeam.Enemy && (long)target.CurrentHealth * healthDivisor <= target.MaxHealth; // 반올림 없는 정확한 이십 퍼센트 이하 확인
    } // 처형 대상 검사 종료

    internal static bool CanUse(CardInstance card, BattleActionPointRuntime points, BattleTurnRuntime turn) // 검 카드 행동력과 체력 대가 확인
    { // 사용 가능 검사 시작
        if (card.SourceData.SpendsAllActionPoints) // 남은 행동력 전체를 쓰는 카드 확인
        { // 가변 행동력 조건 검사 시작
            return points.CurrentActionPoints > 0 && (card.SourceData.SwordRule != SwordCardRule.Finisher || turn.HasFinisherTarget()); // 행동력 양수와 처형의 전용 저체력 조건 확인
        } // 가변 행동력 조건 검사 종료
        if (card.SourceData.SwordRule == SwordCardRule.ExecutionSlash) // 체력 절반 이하 적에게만 사용하는 검 확인
        { // 처형 베기 사용 검사 시작
            return points.CanSpend(card.ApCost) && turn.HasFinisherTarget(2); // 행동력과 체력 절반 이하 적 확인
        } // 처형 베기 사용 검사 종료
        if ((card.SourceData.SwordRule == SwordCardRule.BloodTrail || card.SourceData.SwordRule == SwordCardRule.BloodBackflow) && !turn.HasBleedingTarget()) // 추적할 출혈 적 존재 여부 확인
        { // 추적 대상 없음 시작
            return false; // 카드 사용과 행동력 지불 차단
        } // 추적 대상 없음 종료
        if (card.SourceData.SwordRule == SwordCardRule.JudgmentSlash && !turn.HasBuffedTarget()) // 제거할 강화가 있는 생존 적 확인
        { // 단죄 대상 확인 시작
            return false; // 강화 없는 전투에서 카드 소비 차단
        } // 단죄 대상 확인 종료
        if (card.SourceData.ShieldRule == ShieldCardRule.SplitBarrier && !turn.HasOtherLivingAlly(card.OwnerUnit)) // 자신과 함께 선택할 생존 동료 확인
        { // 생존 아군 부족 처리 시작
            return false; // 살아 있는 아군 두 명이 없는 경우 카드 사용 차단
        } // 생존 아군 부족 처리 종료
        if (card.SourceData.ShieldRule == ShieldCardRule.FinalRampart && !turn.AreAllLivingAlliesAtOrBelowHealthPercent(card.SourceData.SecondaryEffectValue)) // 최후 방벽의 생존 아군별 체력 한도 확인
        { // 최후 방벽 체력 조건 불충족 처리 시작
            return false; // 카드 사용과 행동력 지불 차단
        } // 최후 방벽 체력 조건 불충족 처리 종료
        return points.CanSpend(card.ApCost) && (!HasUpfrontHealthCost(card) || card.OwnerUnit.CanSpendHealth(card.SourceData.SecondaryEffectValue)); // 행동력과 생존에 필요한 소유자 체력 비용 확인
    } // 사용 가능 검사 종료
    internal static bool HasUpfrontHealthCost(CardInstance card) // 효과 적용 전에 소유자 체력을 지불하는 카드 확인
    { // 체력 대가 카드 확인 시작
        return card.SourceData.SwordRule is SwordCardRule.DoomBlade or SwordCardRule.BloodOath || card.SourceData.CupRule == CupCardRule.SacrificialHealing; // 기존 검과 희생 치유의 공통 체력 지불 경로
    } // 체력 대가 카드 확인 종료

    internal static List<BattleUnitRuntime> GetDanceArea(BattleUnitRuntime anchor, IReadOnlyList<BattleUnitRuntime> order) // 선택한 적과 바로 옆 한 칸의 고정 범위
    { // 두 칸 범위 계산 시작
        var area = new List<BattleUnitRuntime>(2); // 실제 위치 기준 두 칸만 보관
        for (int index = 0; index < order.Count; index++) // 전투 화면과 같은 적 배치 순서 검색
        { // 선택 위치 확인 시작
            if (order[index] != anchor) // 클릭한 적 위치 일치 확인
            { // 다른 위치 제외 시작
                continue; // 다음 적 위치 확인
            } // 다른 위치 제외 종료
            area.Add(anchor); // 선택한 적의 칸 보관
            int neighbor = index + 1 < order.Count ? index + 1 : index - 1; // 오른쪽 끝에서만 왼쪽 칸 선택
            if (neighbor >= 0 && order[neighbor] != null && !order[neighbor].IsDead) // 인접 칸에 생존한 적 확인
            { // 인접 범위 추가 시작
                area.Add(order[neighbor]); // 사망 칸 너머의 적 제외
            } // 인접 범위 추가 종료
            break; // 선택한 두 칸에서 범위 확정
        } // 선택 위치 확인 종료
        return area; // 이번 카드가 공격할 수 있는 고정 범위 반환
    } // 두 칸 범위 계산 종료

    internal static List<BattleUnitRuntime> GetWhirlwindArea(BattleUnitRuntime anchor, IReadOnlyList<BattleUnitRuntime> order) // 선택한 적과 좌우의 실제 칸 조회
    { // 소용돌이 범위 시작
        var area = new List<BattleUnitRuntime>(3); // 세 칸의 생존 적 목록
        for (int index = 0; index < order.Count; index++) // 전체 전투 배치에서 선택 위치 검색
        { // 선택 위치 검사 시작
            if (order[index] != anchor) // 다른 전투 위치 확인
            { // 다른 칸 처리 시작
                continue; // 선택 위치까지 이동
            } // 다른 칸 처리 종료
            for (int slot = Math.Max(0, index - 1); slot <= Math.Min(order.Count - 1, index + 1); slot++) // 양끝을 넘지 않는 바로 이웃한 세 칸 확인
            { // 실제 범위 수집 시작
                if (order[slot] != null && !order[slot].IsDead) // 선택한 세 칸의 생존자 확인
                { // 대상 등록 시작
                    area.Add(order[slot]); // 사망 칸을 넘어가지 않는 대상 추가
                } // 대상 등록 종료
            } // 실제 범위 수집 종료
            break; // 선택 범위 한 번만 사용
        } // 선택 위치 검사 종료
        return area; // 실제 타격할 생존 적 반환
    } // 소용돌이 범위 종료

    internal static int ApplyAttackStances(CardInstance card, BattleUnitRuntime target, int damage) // 타격 직전 공격자의 검 강화와 대상 체력 비율 적용
    { // 공격 자세 계산 시작
        long amount = damage; // 원래 카드의 타격 수치 보관
        if (card.CardType == CardType.Sword) // 검 카드 타격 확인
        { // 피의 맹세 계산 시작
            amount += card.OwnerUnit.GetStatusEffect(BattleStatusEffectType.SwordSaintStance)?.EffectiveValue ?? 0; // 검성의 자세의 타격당 검 피해 증가
            amount += card.OwnerUnit.GetStatusEffect(BattleStatusEffectType.BladeFrenzy)?.EffectiveValue ?? 0; // 이번 턴 소유자의 검 피해에 폭주 수치 가산
            amount += card.OwnerUnit.GetStatusEffect(BattleStatusEffectType.BloodOath)?.EffectiveValue ?? 0; // 보유한 검 강화 수치 가산
        } // 피의 맹세 계산 종료
        var stance = card.OwnerUnit.GetStatusEffect(BattleStatusEffectType.FinishingStance); // 해당 카드 소유자의 마무리 자세 조회
        if (stance != null && IsFinisherTarget(target, 2)) // 현재 타격 직전 적 체력이 절반 이하인지 확인
        { // 마무리 보정 시작
            amount = Math.Min(int.MaxValue, amount) * (100L + stance.EffectiveValue) / 100; // 체력 조건을 만족한 카드 타격의 비율 가산과 내림
        } // 마무리 보정 종료
        int duelPercent = card.OwnerUnit.ActiveDuel?.GetDamageBonus(card.OwnerUnit, target) ?? 0; // 실제 결투 상대에게만 적용할 피해 비율
        amount = Math.Min(int.MaxValue, amount) * (100L + duelPercent) / 100; // 직접 피해 비율 적용과 소수점 내림
        return (int)Math.Min(int.MaxValue, amount); // 정수 범위의 원본 카드 피해 반환
    } // 공격 자세 계산 종료

    internal static BattleUnitRuntime PickRandomLivingTarget(IReadOnlyList<BattleUnitRuntime> area) // 무작위 카드 연타의 실제 생존 후보 추첨
    { // 실제 타격 대상 결정 시작
        var living = new List<BattleUnitRuntime>(area.Count); // 이번 타격에서 선택할 수 있는 생존 적
        foreach (var enemy in area) // 카드에서 지정한 전체 대상 범위 확인
        { // 생존 후보 수집 시작
            if (enemy != null && !enemy.IsDead) // 실제로 공격할 생존 대상 확인
            { // 대상 추가 시작
                living.Add(enemy); // 기존 전투 위치 순서로 후보 수집
            } // 대상 추가 종료
        } // 생존 후보 수집 종료
        if (living.Count == 0) // 타격 사이에 후보가 모두 쓰러졌는지 확인
        { // 공격 종료 시작
            return null; // 범위 밖 추가 공격 차단
        } // 공격 종료 종료
        int selected = RelicRandomCardTarget.PickIndex(living.Count, size => UnityEngine.Random.Range(0, size)); // 유물 재추첨을 포함한 한 번의 대상 선택
        return living[selected]; // 이번 타격을 받을 생존 적 반환
    } // 실제 타격 대상 결정 종료

    internal static int GetConditionalWandDamageBonus(CardInstance card, BattleUnitRuntime target) // 실제 지팡이의 대상별 원본 추가 피해 계산
    { // 표식 또는 감전 가산 확인 시작
        bool matches = card.SourceData.WandRule switch // 카드마다 사용할 활성 조건 선택
        { // 대상 약화 조건 선택 시작
            WandCardRule.RuneBurst => target?.HasActiveCardDamageMark == true, // 기존 룬 폭발의 활성 표식 한 개 이상
            WandCardRule.EyeOfStorm => target?.GetStatusEffect(BattleStatusEffectType.Shock) is { IsExpired: false }, // 각 타격 직전 현재 활성 감전
            _ => false // 조건부 추가 피해가 없는 일반 지팡이
        }; // 대상 약화 조건 선택 종료
        return matches ? Math.Max(0, card.SourceData.SecondaryEffectValue) : 0; // 조건별 음수 없는 피해 가산량 반환
    } // 표식 또는 감전 가산 확인 종료

    internal static int BaseDamage(CardInstance card, int paid, BattleUnitRuntime target = null) // 카드 공격의 방어 전 기본 피해 계산
    { // 원본 피해 계산 시작
        if (card.SourceData.SpendsAllActionPoints && card.EffectType == CardEffectType.Damage) // 실제 지불량에 연동하는 피해 카드 확인
        { // 가변 피해 배수 계산 시작
            return (int)Math.Clamp((long)paid * card.EffectValue, 0L, int.MaxValue); // 비용과 원본 배수의 곱을 실제 피해 상한에 맞춤
        } // 가변 피해 배수 계산 종료
        switch (card.SourceData.SwordRule) // 카드별 일반 공격의 조건부 수치 확인
        { // 피해 종류 선택 시작
            case SwordCardRule.BloodEdge: // 출혈 중인 적의 추가 피해
                int bonus = target?.GetStatusEffect(BattleStatusEffectType.Bleed) != null ? card.SourceData.SecondaryEffectValue : 0; // 타격 직전 출혈 유무 확인
                return (int)Math.Min(int.MaxValue, (long)card.EffectValue + card.OwnerUnit.AttackPowerBonus + bonus); // 기본 공격과 출혈 추가 수치 합산
            case SwordCardRule.Counterattack: // 직전 적 턴 이후 체력 손실 반격
                return card.OwnerUnit.HealthLostSinceEnemyTurn / Math.Max(1, card.SourceData.SecondaryEffectValue); // 실제 잃은 체력의 절반 내림
            default: // 표기 피해의 일반 공격
                return (int)Math.Min(int.MaxValue, (long)card.EffectValue + card.OwnerUnit.AttackPowerBonus + GetConditionalWandDamageBonus(card, target)); // 타격과 미리보기의 원본 및 대상별 상태 가산 합계
        } // 피해 종류 선택 종료
    } // 원본 피해 계산 종료

    internal static BattleStatusEffectType ApplyHitStatus(CardInstance card, BattleUnitRuntime target, BattleSupplyRelics supplyRelics, out BattleStatusEffectApplyResult result) // 명중 후 검 카드의 상태 적용
    { // 후속 상태 처리 시작
        result = BattleStatusEffectApplyResult.Invalid; // 추가 효과 없는 기본 결과
        int value = card.SourceData.SecondaryEffectValue; // 원본 카드의 확률 또는 상태 수치
        BattleStatusEffectType type; // 실제 부여할 상태
        switch (card.SourceData.SwordRule) // 명중한 카드의 고유 효과 확인
        { // 상태 선택 시작
            case SwordCardRule.Thrust: // 이십 퍼센트 출혈
            case SwordCardRule.KneeSlash: // 이십오 퍼센트 행동 취소
            case SwordCardRule.PommelStrike: // 십오 퍼센트 기절
                type = card.SourceData.SwordRule == SwordCardRule.Thrust ? BattleStatusEffectType.Bleed : card.SourceData.SwordRule == SwordCardRule.KneeSlash ? BattleStatusEffectType.Stagger : BattleStatusEffectType.Stun; // 실제 명중 후 적용할 출혈과 기존 행동 제어 분리
                if (target.IsDead || !supplyRelics.RollOptionalStatus(target, value, type, card.OwnerUnit)) // 생존 대상과 실제 상태 종류 및 발생자를 포함한 확률 확인
                { // 상태 미발동 시작
                    return BattleStatusEffectType.None; // 추가 상태 없음 반환
                } // 상태 미발동 종료
                value = type == BattleStatusEffectType.Bleed ? 2 : 1; // 고정 출혈 피해 또는 한 번의 행동 제한
                break; // 확률 상태 선택 완료
            case SwordCardRule.IntimidatingCharge: // 위협 돌진의 공포 확률
                if (target.IsDead || !supplyRelics.RollOptionalStatus(target, value)) // 생존과 유리 구슬을 반영한 공포 확률 확인
                { // 공포 미적용 시작
                    return BattleStatusEffectType.None; // 공포 없는 일반 명중 처리
                } // 공포 미적용 종료
                type = BattleStatusEffectType.Fear; // 대상 회피를 차단하는 공포 선택
                value = 1; // 한 개의 공포 상태 지정
                break; // 공포 선택 종료
            case SwordCardRule.SeveringStrike: // 절단 일격의 받는 회복량 감소
                type = BattleStatusEffectType.HealingReceivedDown; // 다음 두 턴의 회복 감소 선택
                break; // 회복 감소 선택 종료
            case SwordCardRule.ShieldSplitter: // 두 턴 물리 방어 감소
                type = BattleStatusEffectType.PhysicalDefenseDown; // 기존 물리 방어 약화 사용
                break; // 물리 약화 선택 완료
            case SwordCardRule.InterceptSlash: // 다음 실제 공격의 피해 감소
                type = BattleStatusEffectType.NextAttackDown; // 공격 소비형 약화 선택
                break; // 다음 공격 약화 선택 완료
            default: // 추가 상태 없는 검 카드
                return BattleStatusEffectType.None; // 기존 공격 규칙 유지
        } // 상태 선택 종료
        if (target.IsDead) // 후속 상태 대상 생존 확인
        { // 대상 사망 처리 시작
            return BattleStatusEffectType.None; // 전투불능 대상의 신규 상태 차단
        } // 대상 사망 처리 종료
        result = target.ApplyStatusEffect(type, value, card.StatusDuration, 1, card.OwnerUnit, corrosionFamily: card.SourceData.IsCorrosionStatus && card.StatusEffectType == type); // 원본에 같은 침식 상태가 명시된 경우의 실제 상태 적용
        return type; // 피격과 면역 피드백 종류 반환
    } // 후속 상태 처리 종료

    internal static int RollCritical(CardInstance card, BattleUnitRuntime target, int damage, out bool critical) // 명중한 직접 카드 타격의 대상별 치명타 판정
    { // 치명타 계산 시작
        int personal = card.SourceData.SwordRule == SwordCardRule.CriticalStrike ? card.SourceData.SecondaryEffectValue : 0; // 이번 카드 전용 추가 확률 조회
        int buff = card.OwnerUnit.GetStatusEffect(BattleStatusEffectType.CriticalChanceUp)?.EffectiveValue ?? 0; // 현재 소유자의 치명타 증가 상태 조회
        int relicBonus = target?.Team == BattleTeam.Enemy && target.HasActiveRemovableDebuff ? BattleSupplyRelics.Find(RelicEffectType.DebuffedTargetCriticalChance)?.EffectValue ?? 0 : 0; // 실제 적 대상의 현재 정화 가능 약화와 보유한 균열침 확인
        int chance = (int)Math.Clamp((long)personal + buff + relicBonus, 0L, 100L); // 카드와 소유자 및 피격 상태의 치명타 확률 합산
        critical = chance > 0 && UnityEngine.Random.Range(0, 100) < chance; // 추가 확률 없는 기존 공격의 난수 보존
        return critical ? (int)Math.Min(int.MaxValue, (long)damage * 3 / 2) : damage; // 치명타는 정수 피해의 일점오 배 내림
    } // 치명타 계산 종료
} // 검 카드 전투 규칙 종료
