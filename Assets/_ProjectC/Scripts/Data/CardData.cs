using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "SO_Card_New", menuName = "Project C/Card Data")] // 카드 에셋 생성 메뉴
public sealed class CardData : ScriptableObject // 원본 카드 데이터
{
    [Header("기본 정보")] // 기본 정보 구분
    [SerializeField] private string cardId = "CARD_NEW"; // 카드 고유 ID
    [SerializeField] private string cardName = "NEW CARD"; // 카드 표시 이름
    [SerializeField, TextArea(2, 5)] private string description = "CARD DESCRIPTION"; // 카드 설명
    [SerializeField] private Sprite cardImage; // 카드 일러스트

    [Header("전투 정보")] // 전투 정보 구분
    [SerializeField, Min(0)] private int baseCost = 1; // 기본 AP 비용
    [SerializeField] private CardRank cardRank = CardRank.Common; // 카드 희귀도
    [SerializeField] private CardType cardType = CardType.Sword; // 카드 계열

    [Header("강화 정보")] // 강화 정보 구분
    [SerializeField] private bool upgradeable = true; // 강화 가능 여부
    [SerializeField, Min(0)] private int maxUpgradeLevel = 1; // 최대 강화 단계

    public string CardId => cardId; // 카드 ID 반환
    public string CardName => cardName; // 카드 이름 반환
    public string Description => description; // 카드 설명 반환
    public Sprite CardImage => cardImage; // 카드 이미지 반환
    public int BaseCost => baseCost; // 기본 비용 반환
    public CardRank CardRank => cardRank; // 카드 등급 반환
    public CardType CardType => cardType; // 카드 종류 반환
    public bool Upgradeable => upgradeable; // 강화 가능 여부 반환
    public int MaxUpgradeLevel => maxUpgradeLevel; // 최대 강화 단계 반환

    public CardInstance CreateRuntimeInstance() // 전투용 카드 인스턴스 생성
    {
        return new CardInstance(this); // 현재 원본 기반 인스턴스 반환
    }

    private void OnValidate() // Inspector 입력값 검증
    {
        baseCost = Mathf.Max(0, baseCost); // 기본 비용 음수 방지
        maxUpgradeLevel = Mathf.Max(0, maxUpgradeLevel); // 강화 단계 음수 방지

        if (upgradeable == false) // 강화 불가 카드 확인
        {
            maxUpgradeLevel = 0; // 최대 강화 단계 초기화
        }
    }
}
