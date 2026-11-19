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
    TwinBladeFlurry // 모든 생존 적에서 네 번 재추첨하는 검
} // 규칙 목록 종료

internal static class SwordCardCombat // 검 카드 비용과 대상 및 직접 타격 계산
{ // 검 카드 전투 규칙 시작
    internal static bool IsFinisherTarget(BattleUnitRuntime target, int healthDivisor = 5) // 남은 체력 오분의 일 이하의 적 확인
    { // 처형 대상 검사 시작
        return target != null && !target.IsDead && target.Team == BattleTeam.Enemy && (long)target.CurrentHealth * healthDivisor <= target.MaxHealth; // 반올림 없는 정확한 이십 퍼센트 이하 확인
    } // 처형 대상 검사 종료

    internal static bool CanUse(CardInstance card, BattleActionPointRuntime points, BattleTurnRuntime turn) // 검 카드 행동력과 체력 대가 확인
    { // 사용 가능 검사 시작
        if (card.SourceData.SwordRule == SwordCardRule.Finisher) // 남은 행동력을 쓰는 공격 확인
        { // 처형 조건 검사 시작
            return points.CurrentActionPoints > 0 && turn.HasFinisherTarget(); // 행동력과 생존한 저체력 적 확인
        } // 처형 조건 검사 종료
        if (card.SourceData.SwordRule == SwordCardRule.ExecutionSlash) // 체력 절반 이하 적에게만 사용하는 검 확인
        { // 처형 베기 사용 검사 시작
            return points.CanSpend(card.ApCost) && turn.HasFinisherTarget(2); // 행동력과 체력 절반 이하 적 확인
        } // 처형 베기 사용 검사 종료
        return points.CanSpend(card.ApCost) && (card.SourceData.SwordRule != SwordCardRule.DoomBlade || card.OwnerUnit.CanSpendHealth(card.SourceData.SecondaryEffectValue)); // 일반 비용과 파멸의 검 생존 비용 확인
    } // 사용 가능 검사 종료

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

    internal static BattleUnitRuntime PickDanceTarget(IReadOnlyList<BattleUnitRuntime> area) // 매 타격 직전 생존 대상 추첨
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

    internal static int BaseDamage(CardInstance card, int paid, BattleUnitRuntime target = null) // 각 검 공격의 방어 전 피해 계산
    { // 원본 피해 계산 시작
        switch (card.SourceData.SwordRule) // 카드의 가변 수치 조건 확인
        { // 피해 종류 선택 시작
            case SwordCardRule.BloodEdge: // 출혈 중인 적의 추가 피해
                int bonus = target?.GetStatusEffect(BattleStatusEffectType.Bleed) != null ? card.SourceData.SecondaryEffectValue : 0; // 타격 직전 출혈 유무 확인
                return (int)Math.Min(int.MaxValue, (long)card.EffectValue + card.OwnerUnit.AttackPowerBonus + bonus); // 기본 공격과 출혈 추가 수치 합산
            case SwordCardRule.Counterattack: // 직전 적 턴 이후 체력 손실 반격
                return card.OwnerUnit.HealthLostSinceEnemyTurn / Math.Max(1, card.SourceData.SecondaryEffectValue); // 실제 잃은 체력의 절반 내림
            case SwordCardRule.Finisher: // 실제 지불한 행동력에 연동한 피해
                return (int)Math.Min(int.MaxValue, (long)paid * card.EffectValue); // 지불한 X와 표시 배수 곱셈
            default: // 표기 피해의 일반 공격
                return (int)Math.Min(int.MaxValue, (long)card.EffectValue + card.OwnerUnit.AttackPowerBonus); // 기존 카드 공격력 보정 적용
        } // 피해 종류 선택 종료
    } // 원본 피해 계산 종료

    internal static BattleStatusEffectType ApplyHitStatus(CardInstance card, BattleUnitRuntime target, out BattleStatusEffectApplyResult result) // 명중 후 검 카드의 상태 적용
    { // 후속 상태 처리 시작
        result = BattleStatusEffectApplyResult.Invalid; // 추가 효과 없는 기본 결과
        int value = card.SourceData.SecondaryEffectValue; // 원본 카드의 확률 또는 상태 수치
        BattleStatusEffectType type; // 실제 부여할 상태
        switch (card.SourceData.SwordRule) // 명중한 카드의 고유 효과 확인
        { // 상태 선택 시작
            case SwordCardRule.Thrust: // 이십 퍼센트 출혈
            case SwordCardRule.KneeSlash: // 이십오 퍼센트 행동 취소
            case SwordCardRule.PommelStrike: // 십오 퍼센트 기절
                if (target.IsDead || UnityEngine.Random.Range(0, 100) >= value) // 사망하거나 확률 밖인 대상 확인
                { // 상태 미발동 시작
                    return BattleStatusEffectType.None; // 추가 상태 없음
                } // 상태 미발동 종료
                type = card.SourceData.SwordRule == SwordCardRule.Thrust ? BattleStatusEffectType.Bleed : card.SourceData.SwordRule == SwordCardRule.KneeSlash ? BattleStatusEffectType.Stagger : BattleStatusEffectType.Stun; // 확률에 당첨된 상태 선택
                value = type == BattleStatusEffectType.Bleed ? 2 : 1; // 고정 출혈 피해 또는 한 번의 행동 제한
                break; // 확률 상태 선택 완료
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
        result = target.ApplyStatusEffect(type, value, card.StatusDuration, 1, card.OwnerUnit); // 면역과 지속 갱신을 거친 실제 상태 적용
        return type; // 피격과 면역 피드백 종류 반환
    } // 후속 상태 처리 종료

    internal static int RollCritical(CardInstance card, int damage, out bool critical) // 명중한 직접 카드 타격의 치명타 판정
    { // 치명타 계산 시작
        int personal = card.SourceData.SwordRule == SwordCardRule.CriticalStrike ? card.SourceData.SecondaryEffectValue : 0; // 이번 카드 전용 추가 확률 조회
        int buff = card.OwnerUnit.GetStatusEffect(BattleStatusEffectType.CriticalChanceUp)?.EffectiveValue ?? 0; // 현재 소유자의 치명타 증가 상태 조회
        int chance = (int)Math.Min(100L, Math.Max(0L, (long)personal + buff)); // 기본 영 퍼센트에 추가 확률 합산
        critical = chance > 0 && UnityEngine.Random.Range(0, 100) < chance; // 추가 확률 없는 기존 공격의 난수 보존
        return critical ? (int)Math.Min(int.MaxValue, (long)damage * 3 / 2) : damage; // 치명타는 정수 피해의 일점오 배 내림
    } // 치명타 계산 종료
} // 검 카드 전투 규칙 종료
