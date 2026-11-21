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
    [SerializeField, Min(0)] private int additionalActionPointCost; // 할인과 별개로 지불할 행동력 대가
    [SerializeField] private CardEffectType effectType; // 카드 효과 종류
    [SerializeField] private BattleDamageType damageType = BattleDamageType.Physical; // 카드 피해 종류
    [Min(0)]
    [SerializeField] private int effectValue = 10; // 카드 효과 수치
    [Min(1)] // 최소 타격 횟수
    [SerializeField] private int damageHitCount = 1; // 같은 대상에 대한 공격 횟수
    [SerializeField] private SwordCardRule swordRule; // 검 카드의 추가 공격 규칙
    [SerializeField] private ShieldCardRule shieldRule; // 방패 카드의 개별 전투 규칙
    [SerializeField] private CupCardRule cupRule; // 성배 카드의 개별 전투 규칙
    [SerializeField] private int secondaryEffectValue; // 보조 효과 수치
    [SerializeField, Min(0)] private int randomDamageHighValue; // 선택적으로 굴리는 두 번째 기본 피해 수치
    [SerializeField] private CardFollowUpEffect followUpEffect; // 기본 효과 뒤에 이어지는 효과 종류
    [SerializeField] private bool grantsBlockToEachAlly; // 아군 전원에게 표시된 방어도를 각각 지급할지 선택

    [SerializeField] private bool volatileInHand; // 턴 종료 손패에서 소멸하는 휘발 카드
    [SerializeField] private bool excludeOnUse; // 사용 후 전투 덱에서 제외하는 카드
    [SerializeField] private bool retainInHand; // 일반 턴 종료에 손패를 떠나지 않는 유지 카드
    public bool VolatileInHand => volatileInHand; // 손패 휘발 규칙 조회
    public bool ExcludeOnUse => excludeOnUse; // 사용 후 제외 규칙 조회
    public bool RetainInHand => retainInHand; // 카드 원본의 손패 유지 규칙 조회

    [Header("정신력 규칙")]
    [Range(-100, 100)]
    [SerializeField] private int mentalChangeValue; // 정신력 직접 변화값

    [Header("상태 이상 규칙")]
    [SerializeField] private BattleStatusEffectType statusEffectType; // 적용 상태 이상 종류
    [SerializeField] private BattleStatusEffectType statusRemovedByHealing; // 체력 회복 전에 제거할 상태
    [SerializeField] private bool isCorrosionStatus; // 이 카드가 실제 부여하는 상태의 명시적인 침식 계열 표시
    [SerializeField, Range(-1, 100)] private int optionalBlindChancePercent = -1; // 음수의 미설정과 명시 실명 확률의 구분
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
    public bool SpendsAllActionPoints => swordRule == SwordCardRule.Finisher || CupRule == CupCardRule.GrailMiracle; // 실제 사용 순간의 남은 행동력을 모두 지불하는 카드 여부
    public int AdditionalActionPointCost => Mathf.Max(0, additionalActionPointCost); // 카드 설명에서 지정한 별도 행동력 대가 조회
    public CardEffectType EffectType => effectType; // 카드 효과 종류 조회
    public BattleDamageType DamageType => damageType; // 카드 피해 종류 조회
    public int EffectValue => effectValue; // 카드 효과 수치 조회
    public int DamageHitCount => effectType == CardEffectType.Damage ? Mathf.Max(1, damageHitCount) : 1; // 이전 원본을 포함한 실제 타격 수
    public SwordCardRule SwordRule => swordRule; // 검 카드 추가 규칙 조회
    public ShieldCardRule ShieldRule => shieldRule; // 방패 카드 추가 규칙 조회
    public CupCardRule CupRule => cupRule; // 성배 카드 추가 규칙 조회
    public int SecondaryEffectValue => secondaryEffectValue; // 보조 효과 수치 조회
    public int RandomDamageHighValue => randomDamageHighValue; // 무작위 피해의 높은 기본값 조회
    public bool HasRandomDamage => effectType == CardEffectType.Damage && randomDamageHighValue > effectValue; // 특정 카드에 설정한 두 피해 여부
    public CardFollowUpEffect FollowUpEffect => followUpEffect; // 카드의 후속 효과 조회
    public bool GrantsBlockToEachAlly => grantsBlockToEachAlly; // 아군별 방어도 지급 여부 조회
    public int MentalChangeValue => mentalChangeValue; // 정신력 변화값 조회
    public BattleStatusEffectType StatusEffectType => statusEffectType; // 상태 이상 종류 조회
    public BattleStatusEffectType StatusRemovedByHealing => statusRemovedByHealing; // 회복과 함께 제거할 상태 조회
    public bool IsCorrosionStatus => isCorrosionStatus; // 원본에 명시된 침식 계열 상태 여부
    public int OptionalBlindChancePercent => Mathf.Clamp(optionalBlindChancePercent, -1, 100); // 보정된 명시 실명 확률 또는 일반 처리 값
    public int StatusDuration => statusDuration; // 상태 이상 지속 횟수 조회
    public int StatusMaximumStacks => statusMaximumStacks; // 상태 이상 최대 중첩 조회

    private CardTag InferDefaultTags() // 기존 카드용 기본 태그 추론
    {
        if (effectType == CardEffectType.Unplayable) // 사용 효과가 없는 저주 확인
        { // 행동 분류 제외 시작
            return CardTag.None; // 공격과 스킬 분류 제외
        } // 행동 분류 제외 종료
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

public enum CardFollowUpEffect // 기본 대상 처리 뒤 한 번 적용할 카드 효과
{ // 후속 효과 목록 시작
    None, // 기존 카드의 추가 효과 없음
    GainOwnerBlock, // 카드 소유자 방어도 획득
    HealAllAllies, // 생존 아군 전체 체력 회복
    GainTargetBlock, // 선택한 대상 방어도 획득
    DrawCards // 사용한 카드를 제외한 드로우
} // 후속 효과 목록 종료
