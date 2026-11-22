using System; // 정수 범위와 반올림 계산

internal static class BattleSageRune // 현자의 룬이 강화할 원래 카드 수치
{ // 카드 수치 규칙 시작
    internal static int ScaleAmount(int value) // 기본 양수 효과의 오십 퍼센트 증가
    { // 원본 효과의 정수 반올림 시작
        return value > 0 ? (int)Math.Min(int.MaxValue, ((long)value * 3 + 1) / 2) : value; // 홀수 절반 올림과 정수 상한 적용
    } // 원본 효과의 정수 반올림 종료

    internal static bool ScalesPrimary(CardData card) // 원래 카드의 기본 효과 필드가 나타내는 수치 분류
    { // 카드 효과 종류별 의미 확인 시작
        return card.EffectType switch // 같은 칸에 기록되는 수치와 다른 용도 구분
        { // 기본 효과의 실제 의미 선택 시작
            CardEffectType.Damage => !card.SpendsAllActionPoints && card.PentacleRule != PentacleCardRule.RunawayRuneDevice, // 일반 고정 피해와 가변 산출량의 구분
            CardEffectType.Heal => !card.SpendsAllActionPoints && card.PentacleRule != PentacleCardRule.GoldenRule, // 일반 고정 회복과 가변 산출량의 구분
            CardEffectType.GainBlock or CardEffectType.GuardAllyAttack or CardEffectType.RedirectAttackAndDefend or CardEffectType.ProtectAttackTarget or // 실제 기본 방어도와 보호의 정량 수치
                CardEffectType.DrawThenDiscard or CardEffectType.DiscardWandForActionPoint => true, // 즉시 드로우와 사용 결과로 받는 행동력
            CardEffectType.ApplyStatusEffect => ScalesStatus(card.StatusEffectType), // 해당 상태가 실제 나타내는 고정 수치 확인
            CardEffectType.PentacleUtility => ScalesPentacle(card.PentacleRule), // 보조 카드의 원래 정액 지급 유형 확인
            _ => false // 대상 선택과 방어 비율 및 다른 상태의 원래 범주 유지
        }; // 기본 효과의 실제 의미 선택 종료
    } // 카드 효과 종류별 의미 확인 종료

    internal static bool ScalesStatus(BattleStatusEffectType type) // 비율과 지속 피해를 제외한 실제 고정 상태 효과
    { // 고정 상태 수치 확인 시작
        return type is BattleStatusEffectType.AttackPowerUp or BattleStatusEffectType.AttackPowerDown or BattleStatusEffectType.NextAttackDown or // 직접 공격의 고정 증가와 감소
            BattleStatusEffectType.PhysicalDefenseUp or BattleStatusEffectType.PhysicalDefenseDown or BattleStatusEffectType.ChargeDefenseDown or // 고정 물리 방어 수치
            BattleStatusEffectType.DuelMark or BattleStatusEffectType.BladeMaintenance or BattleStatusEffectType.ToolMaintenanceAttackUp or // 원래 카드가 저장하는 고정 직접 피해
            BattleStatusEffectType.IllusionHitReduction or BattleStatusEffectType.ShockAbsorption or BattleStatusEffectType.DelayedHealing; // 방어와 단일 예약 회복의 실제 수치
    } // 고정 상태 수치 확인 종료

    private static bool ScalesPentacle(PentacleCardRule rule) // 실제 정량 지원의 주 효과를 쓰는 펜타클 규칙
    { // 원래 정량 지급 분류 시작
        return rule is PentacleCardRule.GoldenCoin or PentacleCardRule.Focus or PentacleCardRule.TacticalExchange or PentacleCardRule.AncientRune or // 원래 공유 행동력 및 일반 드로우 지급
            PentacleCardRule.Contract or PentacleCardRule.Ritual or PentacleCardRule.FusionOfPower or PentacleCardRule.PickUpCoin or // 정액 행동력 또는 대상별 피해 지급
            PentacleCardRule.SecureSupplies or PentacleCardRule.TemporarySupplies or PentacleCardRule.ExchangeProposal or PentacleCardRule.EmergencyFunds or // 원래 보급과 직접 회복 및 예약 양
            PentacleCardRule.SeizeOpportunity or PentacleCardRule.TemporaryContract or PentacleCardRule.GambleMove or PentacleCardRule.SecretVault or // 원래 즉시 행동력과 확정한 일반 드로우
            PentacleCardRule.Redistribution or PentacleCardRule.PayThePrice or PentacleCardRule.AncientFormula or PentacleCardRule.CompletedRitual; // 원래 방어도와 행동력 및 판정 후 생성한 드로우
    } // 원래 정량 지급 분류 종료
} // 카드 수치 규칙 종료
