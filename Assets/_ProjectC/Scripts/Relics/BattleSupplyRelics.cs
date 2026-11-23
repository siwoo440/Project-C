using System; // 정수 범위와 비율 계산

internal sealed class BattleSupplyRelics // 전투 단위의 공격과 무작위 유물 처리
{ // 전투 유물 처리 시작
    private const int RepeatHitGoal = 3; // 전투 수첩 보너스에 필요한 같은 적 명중 횟수
    private int lastBlackHeartRound; // 검은 심장핀의 마지막 아군 라운드
    private bool usedGlassMarble; // 이번 전투 유리 구슬의 사용 여부
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
        bonus += GetRepeatTargetHitBonus(target); // 전투 수첩의 연속 명중 기록 및 예약 피해 가산
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

    internal bool RollOptionalStatus(BattleUnitRuntime target, int chance) // 명중 후 0회 또는 1회 적용되는 약화 굴림
    { // 명중 후 효과 굴림 시작
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
} // 전투 유물 처리 종료
