using UnityEngine; // 유니티 기본 기능 사용
[CreateAssetMenu(fileName = "Character_New", menuName = "Project C/Data/Character")] // 캐릭터 데이터 생성 메뉴
public sealed class CharacterData : ScriptableObject // 캐릭터 원본 데이터
{ // 클래스 시작
    [Header("기본 정보")] // 기본 정보 구역
    [SerializeField] private string characterId; // 캐릭터 고유 ID
    [SerializeField] private string displayName; // 캐릭터 표시 이름
    [TextArea(3, 6)] // 여러 줄 설명 입력
    [SerializeField] private string description; // 캐릭터 설명
    [SerializeField] private CharacterRole role; // 캐릭터 역할
    [SerializeField] private Sprite portrait; // 캐릭터 초상화
    [Header("기본 전투 수치")] // 기본 전투 수치 구역
    [Min(1)] // 최대 체력 최소값
    [SerializeField] private int maxHealth = 100; // 최대 체력
    [Min(0)] // 물리 방어력 최소값
    [SerializeField] private int physicalDefense; // 물리 방어력
    [Min(0)] // 마법 저항력 최소값
    [SerializeField] private int magicalResistance; // 마법 저항력
    [Range(0, 100)] // 정신력 입력 범위
    [SerializeField] private int initialMental = 50; // 초기 정신력
    [Header("정신 특수 상태 효과")] // 정신 특수 상태 효과 구역
    [Range(-100, 100)] // 각성 피해 변화율 범위
    [SerializeField] private int awakeningDamagePercent = 10; // 각성 피해 변화율
    [Range(-100, 100)] // 각성 회복 변화율 범위
    [SerializeField] private int awakeningHealingPercent = 10; // 각성 회복 변화율
    [Range(-100, 100)] // 붕괴 피해 변화율 범위
    [SerializeField] private int collapseDamagePercent = -10; // 붕괴 피해 변화율
    [Range(-100, 100)] // 붕괴 회복 변화율 범위
    [SerializeField] private int collapseHealingPercent = -10; // 붕괴 회복 변화율
    public string CharacterId => characterId; // 캐릭터 ID 조회
    public string DisplayName => displayName; // 캐릭터 이름 조회
    public string Description => description; // 캐릭터 설명 조회
    public CharacterRole Role => role; // 캐릭터 역할 조회
    public Sprite Portrait => portrait; // 캐릭터 초상화 조회
    public int MaxHealth => maxHealth; // 최대 체력 조회
    public int PhysicalDefense => physicalDefense; // 물리 방어력 조회
    public int MagicalResistance => magicalResistance; // 마법 저항력 조회
    public int InitialMental => initialMental; // 초기 정신력 조회
    public int AwakeningDamagePercent => awakeningDamagePercent; // 각성 피해 변화율 조회
    public int AwakeningHealingPercent => awakeningHealingPercent; // 각성 회복 변화율 조회
    public int CollapseDamagePercent => collapseDamagePercent; // 붕괴 피해 변화율 조회
    public int CollapseHealingPercent => collapseHealingPercent; // 붕괴 회복 변화율 조회
} // 클래스 종료
