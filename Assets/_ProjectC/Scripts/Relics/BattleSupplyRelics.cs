using System; // 정수 범위와 비율 계산
using System.Linq; // 명중한 적의 활성 약화 확인

internal sealed class BattleSupplyRelics // 전투 단위의 공격과 무작위 유물 처리
{ // 전투 유물 처리 시작
    private const int RepeatHitGoal = 3; // 전투 수첩 보너스에 필요한 같은 적 명중 횟수
    private int lastBlackHeartRound; // 검은 심장핀의 마지막 아군 라운드
    private bool usedGlassMarble; // 이번 전투 유리 구슬의 사용 여부
    private bool usedFirstMechanicalHitBonus; // 이번 전투 첫 기계형 직접 명중의 회로핀 사용 여부
    private bool usedBlackDustVial; // 이번 전투의 활성 약화 명중에 가루병 사용 여부
    private BattleUnitRuntime repeatedHitTarget; // 전투 수첩에 마지막으로 기록한 적
    private int consecutiveTargetHits; // 같은 적에게 연속으로 명중한 횟수

    internal static RelicData Find(RelicEffectType effect) // 현재 회차의 보유 유물 조회
    { // 보유 유물 조회 시작
        return RelicRunManager.Instance?.Inventory?.FindEffect(effect); // 실제 보유 유물의 종류 조회
    } // 보유 유물 조회 종료

    internal int AddAttackDamage(CardInstance card, BattleUnitRuntime target, int damage, int round) // 실제 카드 직접 명중의 정수 피해 가산
    { // 유물 피해 계산 시작
        if (target.Team != BattleTeam.Enemy) // 적에 명중하는 카드만 대상 확인
        { // 유물 대상 제외 시작
            return damage; // 다른 진영의 피해 수치 유지
        } // 유물 대상 제외 종료
        long bonus = IsSingleAttack(card) ? Find(RelicEffectType.SingleAttackDamage)?.EffectValue ?? 0 : 0; // 탄약띠의 단일 공격 피해 조회
        var pin = lastBlackHeartRound != round && (long)card.OwnerUnit.CurrentHealth * 10 >= (long)card.OwnerUnit.MaxHealth * 7 ? Find(RelicEffectType.HighHealthCardDamage) : null; // 공격 직전 체력 칠십 퍼센트와 턴 발동 여부 확인
        if (pin != null) // 검은 심장핀의 실제 장착 확인
        { // 첫 적 명중 발동 시작
            lastBlackHeartRound = round; // 같은 아군 턴의 중복 적용 차단
            bonus += pin.EffectValue; // 검은 심장핀 피해 가산
        } // 첫 적 명중 발동 종료
        bonus += card.CardType == CardType.Wand && card.EffectType == CardEffectType.Damage ? Find(RelicEffectType.WandDirectHitBonus)?.EffectValue ?? 0 : 0; // 지팡이 피해 카드의 각 명중에 수정못 가산
        bonus += target.GetStatusEffect(BattleStatusEffectType.Blind) != null ? Find(RelicEffectType.BlindTargetDirectDamage)?.EffectValue ?? 0 : 0; // 실제 실명 상태인 적에게 안경 렌즈 가산
        bonus += target.GetStatusEffect(BattleStatusEffectType.Fear) is { IsExpired: false } ? Find(RelicEffectType.FearTargetDirectDamage)?.EffectValue ?? 0 : 0; // 현재 공포가 활성인 적의 실제 타격마다 피리구멍 가산
        bonus += card.CardType == CardType.Sword && target.GetStatusEffect(BattleStatusEffectType.Poison) is { IsExpired: false } ? Math.Max(0, Find(RelicEffectType.SwordDamageAgainstPoison)?.EffectValue ?? 0) : 0; // 활성 중독 적에게 명중한 검의 방어 판정 전 피해 가산
        if (!usedFirstMechanicalHitBonus && target.EnemySource?.IsMechanical == true) // 전투에서 아직 가산을 쓰지 않은 기계형 적의 직접 명중 확인
        { // 회로핀의 첫 기계형 명중 시작
            int circuit = Find(RelicEffectType.FirstMechanicalDirectHitBonus)?.EffectValue ?? 0; // 보유한 회로핀의 실제 가산 피해량 조회
            if (circuit > 0) // 회로핀이 실제로 주는 양의 피해 확인
            { // 첫 기계형 명중 가산 시작
                usedFirstMechanicalHitBonus = true; // 같은 전투의 다음 실제 명중에서 중복 지급 방지
                bonus += circuit; // 방어와 방어도 적용 전 회로핀 피해 가산
            } // 첫 기계형 명중 가산 종료
        } // 회로핀의 첫 기계형 명중 종료
        bonus += GetRepeatTargetHitBonus(target); // 전투 수첩의 연속 명중 기록 및 예약 피해 가산
        var dust = !usedBlackDustVial ? Find(RelicEffectType.FirstDebuffedTargetHitDamage) : null; // 처음 유효한 명중의 검은 가루병 조회
        if (dust != null && target.StatusEffects.Any(static effect => effect.IsDebuff && !effect.IsExpired)) // 적에게 남아 있는 실제 활성 약화 확인
        { // 검은 가루병 발동 시작
            usedBlackDustVial = true; // 방어도가 전부 흡수해도 이번 전투의 기회 소모
            bonus += dust.EffectValue; // 적 방어 계산 이전에 가루병 피해 가산
        } // 검은 가루병 발동 종료
        return (int)Math.Min(int.MaxValue, (long)damage + bonus); // 정수 범위의 최종 유물 피해 반환
    } // 유물 피해 계산 종료

    private int GetRepeatTargetHitBonus(BattleUnitRuntime target) // 실제 적 명중 한 번의 연속 기록 처리
    { // 전투 수첩 확인 시작
        var notebook = Find(RelicEffectType.RepeatTargetHitBonus); // 현재 전투의 수첩 보유 확인
        if (notebook == null) // 수첩 미보유 확인
        { // 미보유 처리 시작
            return 0; // 명중 기록과 보너스 생략
        } // 미보유 처리 종료
        if (consecutiveTargetHits == RepeatHitGoal) // 이전 세 번의 명중으로 예약한 보너스 확인
        { // 예약 보너스 지급 시작
            repeatedHitTarget = null; // 사용한 연속 명중 대상 초기화
            consecutiveTargetHits = 0; // 보너스 타격을 다음 연속 기록에서 제외
            return notebook.EffectValue; // 대상에 관계없이 이번 실제 명중에 추가 피해
        } // 예약 보너스 지급 종료
        consecutiveTargetHits = repeatedHitTarget == target ? consecutiveTargetHits + 1 : 1; // 같은 대상은 누적하고 바뀐 대상은 첫 명중부터 기록
        repeatedHitTarget = target; // 실제 명중한 적의 개별 전투 객체 보관
        return 0; // 예약 전 명중의 기본 피해 유지
    } // 전투 수첩 확인 종료

    private static bool IsSingleAttack(CardInstance card) // 한 대상에 한 번만 타격하는 카드 확인
    { // 단일 공격 분류 시작
        return card.TargetType == CardTargetType.SingleEnemy && card.SourceData.DamageHitCount == 1 && !SunChainAttack.IsChainCard(card) && // 선택 적과 고정 한 타격 확인
            card.SourceData.SwordRule is not (SwordCardRule.BladeWhirlwind or SwordCardRule.HorizontalSlash or SwordCardRule.SwordDance or SwordCardRule.LastBreakthrough); // 인접 공격과 추가 공격 및 범위 연타 제외
    } // 단일 공격 분류 종료

    internal bool RollOptionalStatus(BattleUnitRuntime target, int chance, BattleStatusEffectType type = BattleStatusEffectType.None, BattleUnitRuntime source = null) // 실제 상태 종류와 출처를 포함한 명중 후 약화 굴림
    { // 명중 후 효과 굴림 시작
        if (type == BattleStatusEffectType.Bleed && source?.Team == BattleTeam.Ally && target?.Team == BattleTeam.Enemy) // 실제 아군이 적에게 시도한 확률형 출혈 확인
        { // 녹슨 포크 장식의 출혈 확률 처리 시작
            if (target.IsDead || chance <= 0) // 생존 적과 실제 선택 출혈 확률 여부 확인
            { // 무효 출혈 처리 시작
                return false; // 무효 상태에 유물 및 난수 사용 제외
            } // 무효 출혈 처리 종료
            if (target.HasStatusImmunity) // 정상 상태 부여의 출혈 면역 확인
            { // 원래 면역 결과 처리 시작
                return true; // 확률을 소비하지 않고 원래 상태 부여에서 면역 결과 반환
            } // 원래 면역 결과 처리 종료
            int bonus = Find(RelicEffectType.BleedApplicationChanceBonus)?.EffectValue ?? 0; // 원본에 더할 현재 포크 장식의 오 퍼센트포인트 조회
            chance = (int)Math.Clamp((long)chance + bonus, 0L, 100L); // 기존 확률의 수치와 원본 보정을 합친 정수 범위 확인
            if (chance == 100) // 기존 확률과 유물 보정으로 완전히 보장된 출혈 확인
            { // 확정 출혈 처리 시작
                return true; // 다른 확률 유물과 무작위 없이 확정 적용 반환
            } // 확정 출혈 처리 종료
        } // 녹슨 포크 장식의 출혈 확률 처리 종료
        if (!usedGlassMarble && chance > 0 && chance < 100 && !target.HasStatusImmunity && Find(RelicEffectType.GuaranteeOptionalCardEffect) != null) // 발동 가능한 확률 효과와 전투 최초 유물 확인
        { // 유리 구슬 발동 시작
            usedGlassMarble = true; // 같은 전투의 두 번째 보정 차단
            return true; // 해당 상태 효과의 한 번 적용 보장
        } // 유리 구슬 발동 종료
        return UnityEngine.Random.Range(0, 100) < chance; // 기존의 정수 확률 판정
    } // 명중 후 효과 굴림 종료

    internal static int RollDamage(CardInstance card) // 명시된 두 기본 피해 중 하나 선택
    { // 두 피해 굴림 시작
        int high = card.SourceData.RandomDamageHighValue; // 원본의 상위 피해 수치 조회
        int bonus = card.SourceData.EffectValue == 6 && high == 12 ? Find(RelicEffectType.FavorTwelveDamage)?.EffectValue ?? 0 : 0; // 피해 6·12 조합에만 흑철 확률 가산
        int chance = Math.Clamp(50 + bonus, 0, 100); // 백분율 정수 범위의 높은 피해 확률
        int damage = UnityEngine.Random.Range(0, 100) < chance ? high : card.SourceData.EffectValue; // 높은 피해 또는 기본 피해 결정
        return CardUpgradeProfileCatalog.CalculateEffectValue(card.SourceData, damage, card.UpgradeLevel); // 실제 선택 피해에 카드 강화 반영
    } // 두 피해 굴림 종료
    internal bool RollAuthoredBlind(BattleUnitRuntime target, int baseChance) // 명시한 아군 카드의 선택적 실명 확률 판정
    { // 원본 실명 확률 확인 시작
        if (baseChance < 0) // 확률 효과로 명시하지 않은 카드 확인
        { // 기존 처리 유지 시작
            return false; // 이 경로의 신규 실명과 난수 사용 차단
        } // 기존 처리 유지 종료
        if (baseChance >= 100) // 원래 보장된 실명 확인
        { // 원본 보장 효과 시작
            return true; // 새 난수 없이 원래 확정 결과 반환
        } // 원본 보장 효과 종료
        int bonus = Find(RelicEffectType.BlindApplicationChanceBonus)?.EffectValue ?? 0; // 실제 보유한 공허 렌즈의 추가 조건 조회
        int chance = (int)Math.Clamp((long)baseChance + bonus, 0L, 100L); // 명시된 실명 확률에 렌즈의 수치 가산
        return chance == 100 || chance > 0 && RollOptionalStatus(target, chance); // 전체 성공과 무효 경계에서 난수를 사용하지 않는 기존 선택 판정
    } // 원본 실명 확률 확인 종료
} // 전투 유물 처리 종료
