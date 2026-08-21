using System; // 고유 식별자 기능 사용
using UnityEngine; // 스프라이트 자료형 사용
public sealed class CardInstance // 전투용 카드 인스턴스
{ // 클래스 시작
    public string InstanceId { get; } // 인스턴스 고유 ID
    public CardData SourceData { get; } // 카드 원본 데이터
    public BattleUnitRuntime OwnerUnit { get; } // 카드 소유 전투 유닛
    public string DisplayName => SourceData.DisplayName; // 카드 표시 이름 조회
    public Sprite Artwork => SourceData.Artwork; // 카드 일러스트 조회
    public CardType CardType => SourceData.CardType; // 카드 종류 조회
    public CardTargetType TargetType => SourceData.TargetType; // 카드 대상 종류 조회
    public int ApCost => SourceData.ApCost; // 카드 AP 비용 조회
    public CardEffectType EffectType => SourceData.EffectType; // 카드 효과 종류 조회
    public BattleDamageType DamageType => SourceData.DamageType; // 카드 피해 종류 조회
    public int EffectValue => SourceData.EffectValue; // 카드 효과 수치 조회
    public BattleStatusEffectType StatusEffectType => SourceData.StatusEffectType; // 상태 이상 종류 조회
    public int StatusDuration => SourceData.StatusDuration; // 상태 이상 지속 횟수 조회
    public int StatusMaximumStacks => SourceData.StatusMaximumStacks; // 상태 이상 최대 중첩 조회
    private CardInstance(string instanceId, CardData sourceData, BattleUnitRuntime ownerUnit) // 카드 인스턴스 생성자
    { // 생성자 시작
        InstanceId = instanceId; // 인스턴스 ID 저장
        SourceData = sourceData; // 카드 원본 저장
        OwnerUnit = ownerUnit; // 카드 소유자 저장
    } // 생성자 종료
    public static CardInstance Create(CardData cardData, BattleUnitRuntime ownerUnit, int sequence) // 카드 인스턴스 생성
    { // 카드 생성 시작
        if (cardData == null) // 카드 원본 누락 확인
        { // 카드 누락 처리 시작
            throw new ArgumentNullException(nameof(cardData)); // 카드 누락 예외
        } // 카드 누락 처리 종료
        if (ownerUnit == null) // 카드 소유자 누락 확인
        { // 소유자 누락 처리 시작
            throw new ArgumentNullException(nameof(ownerUnit)); // 소유자 누락 예외
        } // 소유자 누락 처리 종료
        if (ownerUnit.Team != BattleTeam.Ally || ownerUnit.CharacterSource == null) // 아군 소유자 여부 확인
        { // 잘못된 소유자 처리 시작
            throw new ArgumentException("카드 소유자는 아군 전투 유닛이어야 합니다.", nameof(ownerUnit)); // 잘못된 소유자 예외
        } // 잘못된 소유자 처리 종료
        if (sequence < 0) // 카드 순번 범위 확인
        { // 잘못된 순번 처리 시작
            throw new ArgumentOutOfRangeException(nameof(sequence)); // 잘못된 순번 예외
        } // 잘못된 순번 처리 종료
        string instanceId = $"{cardData.CardId}_{sequence:D3}_{Guid.NewGuid():N}"; // 카드별 고유 ID 생성
        return new CardInstance(instanceId, cardData, ownerUnit); // 카드 인스턴스 반환
    } // 카드 생성 종료
} // 클래스 종료
