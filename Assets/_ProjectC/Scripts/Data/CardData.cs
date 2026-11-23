using UnityEngine;

[CreateAssetMenu(fileName = "Card_New", menuName = "Project C/Data/Card")]
public sealed class CardData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string cardId; // 카드 고유 ID
    [SerializeField] private string displayName; // 카드 표시 이름
    [TextArea(3, 6)]
    [SerializeField] private string description; // 카드 설명
    [SerializeField] private Sprite artwork; // 카드 일러스트

    [Header("카드 규칙")]
    [SerializeField] private CardType cardType; // 카드 종류
    [SerializeField] private CardTargetType targetType; // 카드 대상 종류
    [Min(0)]
    [SerializeField] private int apCost = 1; // 카드 AP 비용

    public string CardId => cardId; // 카드 ID 조회
    public string DisplayName => displayName; // 카드 이름 조회
    public string Description => description; // 카드 설명 조회
    public Sprite Artwork => artwork; // 카드 일러스트 조회
    public CardType CardType => cardType; // 카드 종류 조회
    public CardTargetType TargetType => targetType; // 카드 대상 조회
    public int ApCost => apCost; // 카드 AP 비용 조회
}
