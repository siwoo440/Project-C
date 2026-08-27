using System; // 고유 식별자 기능 사용
using UnityEngine; // 스프라이트와 수치 계산 사용

public sealed class CardInstance // 전투용 카드 인스턴스
{
    private const int FallbackUpgradeEffectPercent = 25; // 기존 테스트 호환 강화 효과 증가율

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

    public int ApCost =>
        CardUpgradeProfileCatalog.CalculateApCost(
            SourceData,
            SourceData.ApCost,
            UpgradeLevel); // 카드별 강화 AP 비용

    public CardEffectType EffectType => SourceData.EffectType; // 카드 효과 종류 조회
    public BattleDamageType DamageType => SourceData.DamageType; // 카드 피해 종류 조회

    public int EffectValue =>
        CardUpgradeProfileCatalog.CalculateEffectValue(
            SourceData,
            SourceData.EffectValue,
            UpgradeLevel); // 카드별 강화 효과 수치

    public int MentalChangeValue =>
        CardUpgradeProfileCatalog.CalculateSignedMentalValue(
            SourceData,
            SourceData.MentalChangeValue,
            UpgradeLevel); // 카드별 강화 정신력 변화값

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
