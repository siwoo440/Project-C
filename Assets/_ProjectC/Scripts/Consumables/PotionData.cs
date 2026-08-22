using UnityEngine; // 유니티 기본 기능 사용

[CreateAssetMenu(fileName = "Potion_New", menuName = "Project C/Data/Consumable/Potion")] // 포션 데이터 생성 메뉴
public sealed class PotionData : ConsumableItemData // 포션 원본 데이터
{
    [Header("포션 효과")] // 포션 효과 구역
    [SerializeField] private PotionEffectType effectType; // 포션 효과 종류
    [SerializeField] private ConsumableTargetType targetType = ConsumableTargetType.OneAlly; // 포션 대상 종류
    [SerializeField] private int effectValue = 10; // 포션 기본 효과 수치
    [SerializeField] private BattleDamageType damageType = BattleDamageType.Physical; // 피해 포션 종류
    [SerializeField] private BattleStatusEffectType statusEffectType = BattleStatusEffectType.None; // 상태 포션 종류
    [Min(1)] // 상태 지속 최소값
    [SerializeField] private int statusDuration = 1; // 상태 지속 턴
    [Min(1)] // 상태 중첩 최소값
    [SerializeField] private int statusMaximumStacks = 1; // 상태 최대 중첩

    public PotionEffectType EffectType => effectType; // 포션 효과 종류 조회
    public ConsumableTargetType TargetType => targetType; // 포션 대상 종류 조회
    public int EffectValue => effectValue; // 포션 효과 수치 조회
    public BattleDamageType DamageType => damageType; // 포션 피해 종류 조회
    public BattleStatusEffectType StatusEffectType => statusEffectType; // 포션 상태 종류 조회
    public int StatusDuration => statusDuration; // 포션 상태 지속 조회
    public int StatusMaximumStacks => statusMaximumStacks; // 포션 상태 최대 중첩 조회

    public override bool IsValidData() // 포션 데이터 유효성 검사
    {
        if (!base.IsValidData()) // 기본 정보 유효성 확인
        {
            return false; // 기본 데이터 오류 반환
        }

        if (effectType == PotionEffectType.None) // 효과 누락 확인
        {
            return false; // 효과 누락 반환
        }

        if (targetType == ConsumableTargetType.None && effectType != PotionEffectType.None) // 대상 누락 확인
        {
            return false; // 대상 누락 반환
        }

        return true; // 포션 데이터 정상 반환
    }
}
