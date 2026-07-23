using System; // 기본 자료형과 GUID 기능
using UnityEngine; // Unity 직렬화 기능

[Serializable] // Inspector 직렬화 허용
public sealed class CardInstance // 전투용 카드 인스턴스
{
    [SerializeField] private CardData originalCard; // 원본 카드 데이터
    [SerializeField] private string instanceId; // 전투 카드 고유 ID
    [SerializeField] private int currentCost; // 현재 AP 비용
    [SerializeField] private int upgradeLevel; // 현재 강화 단계

    public CardData OriginalCard => originalCard; // 원본 카드 반환
    public string InstanceId => instanceId; // 인스턴스 ID 반환
    public string CardId => originalCard == null ? "MISSING_CARD" : originalCard.CardId; // 원본 카드 ID 반환
    public string CardName => originalCard == null ? "MISSING CARD" : originalCard.CardName; // 카드 이름 반환
    public string Description => originalCard == null ? string.Empty : originalCard.Description; // 카드 설명 반환
    public Sprite CardImage => originalCard == null ? null : originalCard.CardImage; // 카드 이미지 반환
    public CardRank CardRank => originalCard == null ? CardRank.Common : originalCard.CardRank; // 카드 등급 반환
    public CardType CardType => originalCard == null ? CardType.Sword : originalCard.CardType; // 카드 종류 반환
    public int CurrentCost => currentCost; // 현재 비용 반환
    public int UpgradeLevel => upgradeLevel; // 강화 단계 반환

    public CardInstance(CardData sourceCard) // 원본 카드 기반 생성자
    {
        if (sourceCard == null) // 원본 카드 누락 확인
        {
            throw new ArgumentNullException(nameof(sourceCard)); // 잘못된 생성 요청 알림
        }

        originalCard = sourceCard; // 원본 카드 저장
        instanceId = Guid.NewGuid().ToString("N"); // 고유 인스턴스 ID 생성
        currentCost = sourceCard.BaseCost; // 원본 비용 복사
        upgradeLevel = 0; // 초기 강화 단계 설정
    }

    public void SetTemporaryCost(int newCost) // 전투 중 임시 비용 변경
    {
        currentCost = Mathf.Max(0, newCost); // 비용 음수 방지
    }

    public void ResetBattleValues() // 전투 임시 상태 초기화
    {
        if (originalCard == null) // 원본 카드 누락 확인
        {
            return; // 초기화 중단
        }

        currentCost = originalCard.BaseCost; // 원본 비용으로 복구
    }
}