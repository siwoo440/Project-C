using System; // 정수 범위와 비율 계산

internal sealed class BattleSupplyRelics // 전투 단위의 공격과 무작위 유물 처리
{ // 전투 유물 처리 시작
    private int lastBlackHeartRound; // 검은 심장핀의 마지막 아군 라운드
    private bool usedGlassMarble; // 이번 전투 유리 구슬의 사용 여부

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
        return (int)Math.Min(int.MaxValue, (long)damage + bonus); // 정수 범위의 최종 유물 피해 반환
    } // 유물 피해 계산 종료

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
