using UnityEngine; // 유니티 기본 기능 사용

[CreateAssetMenu(fileName = "Card_New", menuName = "Project C/Data/Card")]
public sealed class CardData : ScriptableObject // 카드 원본 데이터
{
    [Header("기본 정보")]
    [SerializeField] private string cardId; // 카드 고유 ID
    [SerializeField] private string displayName; // 카드 표시 이름
    [TextArea(3, 6)]
    [SerializeField] private string description; // 카드 설명
    [SerializeField] private Sprite artwork; // 카드 일러스트

    [Header("카드 규칙")]
    [SerializeField] private CardType cardType; // 카드 속성
    [SerializeField] private CardTag cardTags = CardTag.None; // 카드 행동 태그
    [SerializeField] private CardTargetType targetType; // 카드 대상 종류
    [Min(0)]
    [SerializeField] private int apCost = 1; // 카드 AP 비용
    [SerializeField] private CardEffectType effectType; // 카드 효과 종류
    [SerializeField] private BattleDamageType damageType = BattleDamageType.Physical; // 카드 피해 종류
    [Min(0)]
    [SerializeField] private int effectValue = 10; // 카드 효과 수치

    [Header("정신력 규칙")]
    [Range(-100, 100)]
    [SerializeField] private int mentalChangeValue; // 정신력 직접 변화값

    [Header("상태 이상 규칙")]
    [SerializeField] private BattleStatusEffectType statusEffectType; // 적용 상태 이상 종류
    [Min(1)]
    [SerializeField] private int statusDuration = 2; // 상태 이상 지속 횟수
    [Min(1)]
    [SerializeField] private int statusMaximumStacks = 1; // 상태 이상 최대 중첩 수

    public string CardId => cardId; // 카드 ID 조회
    public string DisplayName => displayName; // 카드 이름 조회
    public string Description => description; // 카드 설명 조회
    public Sprite Artwork => artwork; // 카드 일러스트 조회
    public CardType CardType => cardType; // 카드 속성 조회
    public CardTag CardTags => cardTags == CardTag.None ? InferDefaultTags() : cardTags; // 카드 태그 조회
    public CardTargetType TargetType => targetType; // 카드 대상 조회
    public int ApCost => apCost; // 카드 AP 비용 조회
    public CardEffectType EffectType => effectType; // 카드 효과 종류 조회
    public BattleDamageType DamageType => damageType; // 카드 피해 종류 조회
    public int EffectValue => effectValue; // 카드 효과 수치 조회
    public int MentalChangeValue => mentalChangeValue; // 정신력 변화값 조회
    public BattleStatusEffectType StatusEffectType => statusEffectType; // 상태 이상 종류 조회
    public int StatusDuration => statusDuration; // 상태 이상 지속 횟수 조회
    public int StatusMaximumStacks => statusMaximumStacks; // 상태 이상 최대 중첩 조회

    private CardTag InferDefaultTags() // 기존 카드용 기본 태그 추론
    {
        CardTag inferredTags = CardTag.None; // 기본 태그 초기화

        if (effectType == CardEffectType.Damage)
        {
            inferredTags |= CardTag.Attack; // 피해 카드를 공격으로 분류
        }
        else
        {
            inferredTags |= CardTag.Skill; // 비피해 카드를 스킬로 분류
        }

        if (cardType == CardType.Wand || damageType == BattleDamageType.Magical)
        {
            inferredTags |= CardTag.Magic; // 지팡이 또는 마법 피해 카드를 마법으로 분류
        }

        return inferredTags; // 추론 태그 반환
    }
}
