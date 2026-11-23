using UnityEngine; // 유니티 기본 기능 사용

[CreateAssetMenu(fileName = "Card_New", menuName = "Project C/Data/Card")] // 카드 데이터 생성 메뉴
public sealed class CardData : ScriptableObject // 카드 원본 데이터
{ // 클래스 시작
    [Header("기본 정보")] // 기본 정보 구역
    [SerializeField] private string cardId; // 카드 고유 ID
    [SerializeField] private string displayName; // 카드 표시 이름
    [TextArea(3, 6)] // 카드 설명 입력 영역
    [SerializeField] private string description; // 카드 설명
    [SerializeField] private Sprite artwork; // 카드 일러스트

    [Header("카드 규칙")] // 카드 규칙 구역
    [SerializeField] private CardType cardType; // 카드 종류
    [SerializeField] private CardTargetType targetType; // 카드 대상 종류
    [Min(0)] // 카드 AP 최소값
    [SerializeField] private int apCost = 1; // 카드 AP 비용
    [SerializeField] private CardEffectType effectType; // 카드 효과 종류
    [SerializeField] private BattleDamageType damageType = BattleDamageType.Physical; // 카드 피해 종류
    [Min(0)] // 카드 효과값 최소값
    [SerializeField] private int effectValue = 10; // 카드 효과 수치

    [Header("상태 이상 규칙")] // 상태 이상 규칙 구역
    [SerializeField] private BattleStatusEffectType statusEffectType; // 적용 상태 이상 종류
    [Min(1)] // 지속 횟수 최소값
    [SerializeField] private int statusDuration = 2; // 상태 이상 지속 횟수
    [Min(1)] // 최대 중첩 최소값
    [SerializeField] private int statusMaximumStacks = 1; // 상태 이상 최대 중첩 수

    public string CardId => cardId; // 카드 ID 조회
    public string DisplayName => displayName; // 카드 이름 조회
    public string Description => description; // 카드 설명 조회
    public Sprite Artwork => artwork; // 카드 일러스트 조회
    public CardType CardType => cardType; // 카드 종류 조회
    public CardTargetType TargetType => targetType; // 카드 대상 조회
    public int ApCost => apCost; // 카드 AP 비용 조회
    public CardEffectType EffectType => effectType; // 카드 효과 종류 조회
    public BattleDamageType DamageType => damageType; // 카드 피해 종류 조회
    public int EffectValue => effectValue; // 카드 효과 수치 조회
    public BattleStatusEffectType StatusEffectType => statusEffectType; // 상태 이상 종류 조회
    public int StatusDuration => statusDuration; // 상태 이상 지속 횟수 조회
    public int StatusMaximumStacks => statusMaximumStacks; // 상태 이상 최대 중첩 조회
} // 클래스 종료
