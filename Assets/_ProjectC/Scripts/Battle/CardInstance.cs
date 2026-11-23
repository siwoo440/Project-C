using System; // 고유 식별자 기능 사용
using UnityEngine; // 스프라이트와 수치 계산 사용

public sealed class CardInstance // 전투용 카드 인스턴스
{
    private const int FallbackUpgradeEffectPercent = 25; // 기존 테스트 호환 강화 효과 증가율
    internal int RelicApDiscount { get; set; } // 첫 턴 유물 비용 감소량
    internal int FirstDrawRelicApDiscount { get; set; } // 첫 드로우 카드의 턴 한정 유물 할인
    internal int OpeningRandomRelicApDiscount { get; set; } // 퇴색한 왕관이 선택한 카드의 독립 할인
    internal int NextCardRelicApDiscount { get; set; } // 다음 유료 카드에 적용할 유물 할인
    internal int FirstDefensiveRelicApDiscount { get; set; } // 전투의 첫 유료 방어 카드에 예약된 할인
    private int temporaryApDiscount; // 이번 턴 임시 행동력 할인
    internal bool IsBattleOnlySingleUse { get; private set; } // 원본 덱에 저장하지 않는 고정 비용 영의 전투 일회 카드
    internal bool HasBeenPlayed { get; set; } // 현재 전투에서 같은 실물 인스턴스가 실제 사용을 완료한 기록
    internal int AncientFormulaApSurcharge { get; set; } // 이번 아군 턴 실제 드로우한 물리 카드의 고대 계산식 기본 비용 증가

    public string InstanceId { get; }
    public CardData SourceData { get; }
    public BattleUnitRuntime OwnerUnit { get; }
    public int UpgradeLevel { get; }

    public string DisplayName =>
        UpgradeLevel > 0
            ? $"{SourceData.DisplayName} +{UpgradeLevel}"
            : SourceData.DisplayName; // 강화 단계 포함 카드 이름

    public Sprite Artwork => SourceData.Artwork; // 카드 일러스트 조회
    public CardType CardType => SourceData.CardType; // 카드 종류 조회
    public CardTargetType TargetType => SourceData.TargetType; // 카드 대상 종류 조회

    public string CostLabel => SourceData.PentacleRule == PentacleCardRule.CalculatedInvestment ? $"{SourceData.ApCost}+" : // 전액 투자 카드의 원래 AP2 하한 표시
        SourceData.SpendsAllActionPoints ? "X" : ApCost.ToString(); // 다른 가변 카드와 일반 실제 행동력 비용 표시
    public int ApCost => IsBattleOnlySingleUse ? 0 : (int)Math.Min(int.MaxValue, RemainingFixedApCost - EffectiveFirstDefenseDiscount - EffectiveRuneRecordingDiscount + SourceData.AdditionalActionPointCost); // 무료 전투 카드와 일반 비용 할인 및 별도 필수 비용의 실제 결제
    internal int EffectiveRuneRecordingDiscount => SourceData.SpendsAllActionPoints || IsBattleOnlySingleUse ? 0 : // 가변 비용과 전투 임시 무료 카드의 기록 할인 소모 제외
        (int)Math.Min(OwnerUnit.RuneRecordingApDiscount, RemainingFixedApCost - EffectiveFirstDefenseDiscount); // 다른 기본 할인을 거치고도 지불할 양수 일반 비용만 감소
    private long RemainingFixedApCost => Math.Max(0L, (long)ApCostBeforeNextCardDiscount - NextCardRelicApDiscount - ShieldBalanceDiscount); // 기존 할인을 먼저 적용한 고정 기본 비용
    internal int EffectiveFirstDefenseDiscount => FirstDefensiveRelicApDiscount > 0 && !SourceData.SpendsAllActionPoints && HasAuthoredDefense ? (int)Math.Min(FirstDefensiveRelicApDiscount, RemainingFixedApCost) : 0; // 지불할 기본 비용 안에서만 실제 방패끈 할인 적용
    internal bool HasDefensivePrimaryEffect => EffectType is CardEffectType.GainBlock or CardEffectType.GuardAllyAttack || // 직접 방어도와 동료 보호 효과 확인
        SourceData.PentacleRule is PentacleCardRule.SecureSupplies or PentacleCardRule.Redistribution || // 보급 확보와 재분배의 실제 기본 방어도 확인
        EffectType == CardEffectType.ApplyStatusEffect && CardType == CardType.Shield && StatusEffectType != BattleStatusEffectType.None && !BattleStatusEffectInstance.IsDebuffType(StatusEffectType); // 방패 카드의 양의 상태 효과 확인
    private bool HasFriendlyTarget => TargetType is CardTargetType.Self or CardTargetType.SingleAlly or CardTargetType.AllAllies; // 본인과 일반 아군 대상 확인
    private bool HasAuthoredDefense => HasFriendlyTarget && HasDefensivePrimaryEffect || // 아군에게 주는 기본 방어 효과 확인
        SourceData.SecondaryEffectValue > 0 && (SourceData.FollowUpEffect == CardFollowUpEffect.GainOwnerBlock || // 실제 사용자의 명시적 후속 방어도 확인
        HasFriendlyTarget && (SourceData.FollowUpEffect == CardFollowUpEffect.GainTargetBlock || SourceData.SwordRule == SwordCardRule.BattlefieldResolve)); // 아군 대상의 후속 방어도와 전장의 결의 확인
    internal int ApCostBeforeNextCardDiscount // 다음 카드 할인 적용 전의 고정 비용
    { // 고정 비용 계산 시작
        get // 현재 카드 비용 조회
        { // 지속 할인 계산 시작
            if (IsBattleOnlySingleUse) // 턴마다 할인으로 계산하지 않는 실제 무료 전투 카드 확인
            { // 고정 무료 기본 비용 적용 시작
                return 0; // 다음 유료 카드 할인의 발동 조건에서도 이미 무료인 비용 반환
            } // 고정 무료 기본 비용 적용 종료
            int upgradedCost = HasRunePower ? 0 : (int)Math.Max(0L, (long)UnmodifiedFixedBaseCost - strategicStorageApDiscount); // 다음 실제 사용까지 유지하며 별도 행동력은 건드리지 않는 기본 비용 면제
            int efficientUse = SourceData.SpendsAllActionPoints ? 0 : OwnerUnit.EfficientUseApDiscount; // X 비용에는 적용하지 않는 이번 아군 턴의 별도 기본 할인
            int calculation = CardType == CardType.Pentacle && !SourceData.SpendsAllActionPoints ? OwnerUnit.MechanicalCalculationApDiscount : 0; // 고정 펜타클의 일반 기본 비용에만 기계식 할인 포함
            int crown = !HasBeenPlayed && !SourceData.SpendsAllActionPoints ? OwnerUnit.WindWornCrownApDiscount : 0; // 실제 사막에서 아직 쓰지 않은 일반 고정 비용의 독립 첫 할인
            long discount = (long)temporaryApDiscount + RelicApDiscount + FirstDrawRelicApDiscount + OpeningRandomRelicApDiscount + SwordSaintDiscount + efficientUse + calculation + crown; // X 및 별도 추가 지불의 원래 경계 아래 기본 할인만 합산
            int formula = SourceData.SpendsAllActionPoints || HasRunePower ? 0 : AncientFormulaApSurcharge; // 전액 비용과 명시적인 기본 행동력 면제의 원래 계산 보존
            return (int)Math.Clamp((long)upgradedCost + formula - discount, 0L, int.MaxValue); // 이번 턴의 일반 기본 비용 증가와 기존 할인 계산
        } // 지속 할인 계산 종료
    } // 고정 비용 계산 종료
    internal int ShieldBalanceDiscount => CardType == CardType.Shield && ApCostBeforeNextCardDiscount > 0 ? OwnerUnit.GetStatusEffect(BattleStatusEffectType.ShieldBalance)?.EffectiveValue ?? 0 : 0; // 비용이 남는 방패 카드에 예약된 기본 할인
    private int SwordSaintDiscount => CardType == CardType.Sword && OwnerUnit.GetStatusEffect(BattleStatusEffectType.SwordSaintStance) != null ? 1 : 0; // 검성 상태 소유자의 검 카드 비용 감소
    public void ApplyTemporaryApDiscount(int amount) // 이번 턴 행동력 할인 적용
    { // 할인 적용 시작
        temporaryApDiscount = Mathf.Max(temporaryApDiscount, Mathf.Max(0, amount)); // 높은 할인값 유지
    } // 할인 적용 종료
    public bool ClearTemporaryApDiscount() // 임시 행동력 할인 제거
    { // 할인 제거 시작
        if (temporaryApDiscount == 0) // 할인 존재 여부 확인
        { // 할인 없음 처리 시작
            return false; // 변경 없음 반환
        } // 할인 없음 처리 종료
        temporaryApDiscount = 0; // 할인값 초기화
        return true; // 변경 완료 반환
    } // 할인 제거 종료

    public CardEffectType EffectType => SourceData.EffectType; // 카드 효과 종류 조회
    public BattleDamageType DamageType => SourceData.DamageType; // 카드 피해 종류 조회

    internal int StrategicStorageRound { get; private set; } // 어느 아군 턴에 한 번의 자동 보관을 받았는지 기록
    private int strategicStorageApDiscount; // 이번 카드의 고정 기본 비용에만 남긴 독립 할인
    internal void ApplyStrategicStorage(int round, int discount) // 지정 실물의 한 번 보관과 기본 비용 할인
    { // 현재 아군 턴과 고정 할인 저장 시작
        StrategicStorageRound = round; // 같은 실물의 이어지는 보관만 새로 갱신
        strategicStorageApDiscount = SourceData.SpendsAllActionPoints ? 0 : Math.Max(0, discount); // 가변 행동력과 별도 대가를 변경하지 않는 고정 할인
    } // 현재 아군 턴과 고정 할인 저장 종료
    internal void ClearStrategicStorage() // 사용 및 이탈과 정리 때의 보관 할인 회수
    { // 별도 사용 기한 회수 시작
        StrategicStorageRound = 0; // 현재 실물의 남은 보관 차례 초기화
        strategicStorageApDiscount = 0; // 다음 카드 사용에 할인 잔류 방지
    } // 별도 사용 기한 회수 종료
    public bool HasRunePower { get; private set; } // 다음 실제 사용까지 실물 카드에 남는 기본 비용 면제
    private int UnmodifiedFixedBaseCost => CardUpgradeProfileCatalog.CalculateApCost(SourceData, SourceData.ApCost, UpgradeLevel); // 별도 대가와 다른 일시 할인 이전의 기본 행동력
    internal bool CanReceiveRunePower => !HasRunePower && !IsBattleOnlySingleUse && !SourceData.SpendsAllActionPoints && EffectType != CardEffectType.Unplayable && UnmodifiedFixedBaseCost > 0; // 실제 유료 고정 기본 비용 카드 여부
    internal void ApplyRunePower() => HasRunePower = true; // 지정 실물의 다음 한 번 기본 행동력 면제
    internal void ConsumeRunePower() => HasRunePower = false; // 실제 사용한 실물의 기본 비용 복원
    private int enchantmentPercent; // 이 실물 카드의 다음 사용에만 남는 강화 백분율
    public int EnchantmentPercent => enchantmentPercent; // 현재 카드 상세 화면의 일회 강화 표시
    public int EffectValue => BattleSageRune.ScalesPrimary(SourceData) ? ScaleQuantitativeEffect(EffectValueWithoutSageRune) : EffectValueWithoutSageRune; // 실제 카드 기본 수치와 현재 룬의 적용 범위
    internal int EffectValueWithoutSageRune => EnchantmentPercent == 0 ? EffectValueBeforeEnchantment : ApplyEnchantment(EffectValueBeforeEnchantment, EnchantmentPercent); // 기존 영구 강화와 원래 일회 강화의 기본 효과
    internal int ScaleQuantitativeEffect(int value) // 이 카드를 쓰는 실제 아군의 고정 효과에만 공용 룬 적용
    { // 현재 전투의 원래 소유자 범위 확인 시작
        return OwnerUnit.Team == BattleTeam.Ally && OwnerUnit.DamageRelicBattle?.HasActiveSageRune == true ? BattleSageRune.ScaleAmount(value) : value; // 이번 아군 턴의 수치를 한 번 올리고 다른 경우 보존
    } // 현재 전투의 원래 소유자 범위 확인 종료
    internal int ScaleStatusAmount(BattleStatusEffectType type, int value) => BattleSageRune.ScalesStatus(type) ? ScaleQuantitativeEffect(value) : value; // 실제 상태의 고정 수치만 현재 룬 적용
    internal static int ApplyEnchantment(int value, int percent) => (int)Math.Clamp((long)value + (long)value * percent / 100, int.MinValue, int.MaxValue); // 정수 내림과 상한을 지킨 숫자 효과 강화
    internal void SetEffectEnchantment(int percent) => enchantmentPercent = Math.Max(enchantmentPercent, percent); // 여러 번 강화해도 가장 높은 단일 예약만 유지
    internal void ConsumeEffectEnchantment() => enchantmentPercent = 0; // 실제 사용한 카드의 한 번 강화 제거
    internal int EffectValueBeforeEnchantment => // 원래 인스턴스의 영구 강화와 고정 원문 수치
        StatusEffectType == BattleStatusEffectType.ResurrectionSeed || SourceData.CupRule == CupCardRule.WhiteVow || // 씨앗의 회복 체력 비율과 맹세의 무효 횟수 고정
        StatusEffectType == BattleStatusEffectType.SwordSaintStance || StatusEffectType == BattleStatusEffectType.DuelMark || StatusEffectType == BattleStatusEffectType.BladeFrenzy || StatusEffectType == BattleStatusEffectType.BloodOath || StatusEffectType == BattleStatusEffectType.FinishingStance || StatusEffectType == BattleStatusEffectType.BladeMaintenance || StatusEffectType == BattleStatusEffectType.PhysicalRiposte // 원문 고정 보너스와 효과 비율 확인
            ? SourceData.EffectValue // 고정된 준비 수치와 반격 비율 유지
            : CardUpgradeProfileCatalog.CalculateEffectValue(SourceData, SourceData.EffectValue, UpgradeLevel); // 일반 카드의 강화 효과 수치

    public int MentalChangeValue =>
        ScaleQuantitativeEffect(CardUpgradeProfileCatalog.CalculateSignedMentalValue( // 계산한 실제 정신력의 양수 카드 효과
            SourceData, // 실제 정신력 회복 원본
            SourceData.MentalChangeValue, // 회복이나 별도 기존 비용의 부호
            UpgradeLevel)); // 양수 카드 회복만 올리고 기존 음수 대가 유지

    public BattleStatusEffectType StatusEffectType => SourceData.StatusEffectType; // 상태 이상 종류 조회

    public int StatusDuration =>
        CardUpgradeProfileCatalog.CalculateStatusDuration(
            SourceData,
            SourceData.StatusDuration,
            UpgradeLevel); // 카드별 강화 상태 지속 횟수

    public int StatusMaximumStacks =>
        CardUpgradeProfileCatalog.CalculateStatusMaximumStacks(
            SourceData,
            SourceData.StatusMaximumStacks,
            UpgradeLevel); // 카드별 강화 상태 최대 중첩

    private CardInstance(
        string instanceId,
        CardData sourceData,
        BattleUnitRuntime ownerUnit,
        int upgradeLevel) // 카드 인스턴스 생성자
    {
        InstanceId = instanceId; // 인스턴스 ID 저장
        SourceData = sourceData; // 카드 원본 저장
        OwnerUnit = ownerUnit; // 카드 소유자 저장

        UpgradeLevel = Mathf.Clamp(
            upgradeLevel,
            0,
            CardUpgradeProfileCatalog.GetMaximumUpgradeLevel(sourceData)); // 카드별 최대 강화 단계 범위 적용
    }

    public static CardInstance Create(
        CardData cardData,
        BattleUnitRuntime ownerUnit,
        int sequence) // 카드 인스턴스 생성
    {
        int upgradeLevel = ResolveRunUpgradeLevel(cardData, ownerUnit, sequence); // 회차 덱 강화 단계 조회
        return Create(cardData, ownerUnit, sequence, upgradeLevel); // 강화 단계 포함 카드 생성
    }

    public static CardInstance Create(
        CardData cardData,
        BattleUnitRuntime ownerUnit,
        int sequence,
        int upgradeLevel) // 강화 단계 포함 카드 인스턴스 생성
    {
        if (cardData == null) // 카드 원본 누락 확인
        {
            throw new ArgumentNullException(nameof(cardData)); // 카드 누락 예외
        }

        if (ownerUnit == null) // 카드 소유자 누락 확인
        {
            throw new ArgumentNullException(nameof(ownerUnit)); // 소유자 누락 예외
        }

        if (ownerUnit.Team != BattleTeam.Ally ||
            ownerUnit.CharacterSource == null) // 아군 소유자 여부 확인
        {
            throw new ArgumentException(
                "카드 소유자는 아군 전투 유닛이어야 합니다.",
                nameof(ownerUnit)); // 잘못된 소유자 예외
        }

        if (sequence < 0) // 카드 순번 범위 확인
        {
            throw new ArgumentOutOfRangeException(nameof(sequence)); // 잘못된 순번 예외
        }

        string instanceId =
            $"{cardData.CardId}_{sequence:D3}_{Guid.NewGuid():N}"; // 카드별 고유 ID 생성

        return new CardInstance(
            instanceId,
            cardData,
            ownerUnit,
            upgradeLevel); // 카드 인스턴스 반환
    }

    internal static CardInstance CreateBattleOnlySingleUse(CardData cardData, BattleUnitRuntime owner, int sequence) // 실제 원본에서 강화 없는 전투 일회 카드 생성
    { // 전투 한정 카드 생성 시작
        var instance = Create(cardData, owner, sequence, 0); // 원본 효과와 소유권은 유지하면서 회차 강화 없이 생성
        instance.IsBattleOnlySingleUse = true; // 실제 사용 시 제외되고 전투에서만 보관하는 카드 표시
        return instance; // 이 전투의 별도 카드 한 장 반환
    } // 전투 한정 카드 생성 종료

    public static int CalculateUpgradedEffectValue(
        int baseValue,
        int upgradeLevel) // 기존 Editor 테스트 호환 Prototype 효과 강화 계산
    {
        if (baseValue <= 0 || upgradeLevel <= 0) // 강화 미적용 조건 확인
        {
            return Mathf.Max(0, baseValue); // 기본 효과 수치 반환
        }

        float multiplier =
            1f +
            FallbackUpgradeEffectPercent *
            Mathf.Clamp(upgradeLevel, 0, 1) /
            100f; // 프로필 없는 기존 한 단계 강화 배율 계산

        return Mathf.CeilToInt(baseValue * multiplier); // 기존 +25% 테스트 결과 반환
    }

    private static int ResolveRunUpgradeLevel(
        CardData cardData,
        BattleUnitRuntime ownerUnit,
        int sequence) // 회차 카드 강화 단계 조회
    {
        RunDeckManager runDeckManager = RunDeckManager.Instance; // 현재 회차 덱 관리자 조회

        if (runDeckManager == null ||
            sequence < 0 ||
            sequence >= runDeckManager.Cards.Count) // 회차 덱과 순번 유효성 확인
        {
            return 0; // 강화 없음 반환
        }

        RunDeckCardEntry runEntry = runDeckManager.Cards[sequence]; // 동일 순번 회차 카드 조회

        if (runEntry == null ||
            runEntry.Card != cardData ||
            runEntry.Owner != ownerUnit.CharacterSource) // 카드와 소유자 일치 여부 확인
        {
            return 0; // 다른 덱 경로 카드 강화 제외
        }

        return runEntry.UpgradeLevel; // 현재 회차 강화 단계 반환
    }
}
