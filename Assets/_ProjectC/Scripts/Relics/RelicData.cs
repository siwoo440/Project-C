using UnityEngine; // 유니티 기본 기능 사용

[CreateAssetMenu(fileName = "Relic_New", menuName = "Project C/Data/Relic")] // 유물 데이터 생성 메뉴
public sealed class RelicData : ScriptableObject // 유물 원본 데이터
{
    [Header("기본 정보")] // 기본 정보 구역
    [SerializeField] private string relicId; // 유물 고유 ID
    [SerializeField] private string displayName; // 유물 표시 이름
    [TextArea(3, 6)] // 여러 줄 설명 입력
    [SerializeField] private string description; // 유물 효과 설명
    [SerializeField] private Sprite icon; // 유물 아이콘
    [SerializeField] private RelicRarity rarity = RelicRarity.Common; // 유물 희귀도

    [Header("발동 규칙")] // 발동 규칙 구역
    [SerializeField] private RelicTriggerType triggerType = RelicTriggerType.BattleStarted; // 유물 발동 시점
    [SerializeField] private RelicEffectType effectType = RelicEffectType.None; // 유물 효과 종류
    [SerializeField] private RelicTargetType targetType = RelicTargetType.AllAllies; // 유물 효과 대상
    [Min(0)] // 효과 수치 최소값
    [SerializeField] private int effectValue; // 유물 기본 효과 수치
    [Min(0)] // 턴당 발동 제한 최소값
    [SerializeField] private int maximumTriggersPerTurn; // 턴당 최대 발동 횟수
    [Min(0)] // 전투당 발동 제한 최소값
    [SerializeField] private int maximumTriggersPerBattle; // 전투당 최대 발동 횟수

    [Header("피해 효과")] // 피해 효과 구역
    [SerializeField] private BattleDamageType damageType = BattleDamageType.Physical; // 유물 피해 유형

    [Header("상태 이상 효과")] // 상태 이상 효과 구역
    [SerializeField] private BattleStatusEffectType statusEffectType = BattleStatusEffectType.None; // 적용 상태 이상 종류
    [Min(1)] // 상태 지속 턴 최소값
    [SerializeField] private int statusDuration = 1; // 상태 지속 턴 수
    [Min(1)] // 상태 최대 중첩 최소값
    [SerializeField] private int statusMaximumStacks = 1; // 상태 최대 중첩 수

    [Header("중복 획득")] // 중복 획득 구역
    [Min(0)] // 중복 골드 최소값
    [SerializeField] private int duplicateGoldValue = 50; // 중복 획득 변환 골드

    public string RelicId => relicId; // 유물 ID 조회
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName; // 유물 표시 이름 조회
    public string Description => description; // 유물 설명 조회
    public Sprite Icon => icon; // 유물 아이콘 조회
    public RelicRarity Rarity => rarity; // 유물 희귀도 조회
    public RelicTriggerType TriggerType => triggerType; // 유물 발동 시점 조회
    public RelicEffectType EffectType => effectType; // 유물 효과 종류 조회
    public RelicTargetType TargetType => targetType; // 유물 대상 종류 조회
    public int EffectValue => Mathf.Max(0, effectValue); // 음수 없는 효과 수치 조회
    public int MaximumTriggersPerTurn => Mathf.Max(0, maximumTriggersPerTurn); // 턴당 발동 제한 조회
    public int MaximumTriggersPerBattle => Mathf.Max(0, maximumTriggersPerBattle); // 전투당 발동 제한 조회
    public BattleDamageType DamageType => damageType; // 피해 유형 조회
    public BattleStatusEffectType StatusEffectType => statusEffectType; // 상태 이상 종류 조회
    public int StatusDuration => Mathf.Max(1, statusDuration); // 상태 지속 턴 조회
    public int StatusMaximumStacks => Mathf.Max(1, statusMaximumStacks); // 상태 최대 중첩 조회
    public int DuplicateGoldValue => Mathf.Max(0, duplicateGoldValue); // 중복 변환 골드 조회

    public bool IsValidData() // 유물 데이터 유효성 검사
    {
        return !string.IsNullOrWhiteSpace(relicId); // 고유 ID 존재 여부 반환
    }
}
