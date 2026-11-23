using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용
[CreateAssetMenu(fileName = "Enemy_New", menuName = "Project C/Data/Enemy")] // 적 데이터 생성 메뉴
public sealed class EnemyData : ScriptableObject // 적 원본 데이터
{ // 클래스 시작
    [Header("기본 정보")] // 기본 정보 구역
    [SerializeField] private string enemyId; // 적 고유 ID
    [SerializeField] private string displayName; // 적 표시 이름
    [TextArea(3, 6)] // 여러 줄 설명 입력
    [SerializeField] private string description; // 적 설명
    [SerializeField] private Sprite portrait; // 적 표시 이미지
    [Header("기본 전투 수치")] // 기본 전투 수치 구역
    [Min(1)] // 최대 체력 최소값
    [SerializeField] private int maxHealth = 50; // 최대 체력
    [Min(0)] // 물리 방어력 최소값
    [SerializeField] private int physicalDefense; // 물리 방어력
    [Min(0)] // 마법 저항력 최소값
    [SerializeField] private int magicalResistance; // 마법 저항력
    [Header("기본 행동")] // 기본 행동 구역
    [SerializeField] private EnemyActionType actionType = EnemyActionType.Attack; // 기본 행동 종류
    [Min(0)] // 공격력 최소값
    [SerializeField] private int basicAttackPower = 8; // 기본 공격력
    [SerializeField] private BattleDamageType damageType = BattleDamageType.Physical; // 기본 피해 유형
    [SerializeField] private BattleStatusEffectType statusEffectType = BattleStatusEffectType.None; // 적용 상태 이상 종류
    [Min(1)] // 상태 효과 수치 최소값
    [SerializeField] private int statusEffectValue = 1; // 상태 효과 수치
    [Min(1)] // 상태 지속 횟수 최소값
    [SerializeField] private int statusEffectDuration = 1; // 상태 지속 횟수
    [Min(1)] // 상태 최대 중첩 최소값
    [SerializeField] private int statusEffectMaximumStacks = 1; // 상태 최대 중첩 수
    [SerializeField] private EnemyTargetRule targetRule = EnemyTargetRule.FirstLiving; // 대상 선택 규칙
    [Min(1)] // 최소 행동 속도 제한
    [SerializeField] private int minimumActionSpeed = 1; // 최소 행동 속도
    [Min(1)] // 최대 행동 속도 제한
    [SerializeField] private int maximumActionSpeed = 10; // 최대 행동 속도
    [Header("순차 행동 패턴")] // 순차 행동 패턴 구역
    [SerializeField] private List<EnemyActionPatternData> actionPatterns = new List<EnemyActionPatternData>(); // 순서대로 반복할 행동 목록
    public string EnemyId => enemyId; // 적 ID 조회
    public string DisplayName => displayName; // 적 이름 조회
    public string Description => description; // 적 설명 조회
    public Sprite Portrait => portrait; // 적 이미지 조회
    public int MaxHealth => maxHealth; // 최대 체력 조회
    public int PhysicalDefense => physicalDefense; // 물리 방어력 조회
    public int MagicalResistance => magicalResistance; // 마법 저항력 조회
    public EnemyActionType ActionType => actionType; // 기본 행동 종류 조회
    public int BasicAttackPower => basicAttackPower; // 기본 공격력 조회
    public BattleDamageType DamageType => damageType; // 기본 피해 유형 조회
    public BattleStatusEffectType StatusEffectType => statusEffectType; // 적용 상태 이상 종류 조회
    public int StatusEffectValue => Mathf.Max(1, statusEffectValue); // 보정된 상태 효과 수치 조회
    public int StatusEffectDuration => Mathf.Max(1, statusEffectDuration); // 보정된 상태 지속 횟수 조회
    public int StatusEffectMaximumStacks => Mathf.Max(1, statusEffectMaximumStacks); // 보정된 상태 최대 중첩 조회
    public EnemyTargetRule TargetRule => targetRule; // 대상 선택 규칙 조회
    public int MinimumActionSpeed => Mathf.Max(1, minimumActionSpeed); // 보정된 최소 행동 속도 조회
    public int MaximumActionSpeed => Mathf.Max(MinimumActionSpeed, maximumActionSpeed); // 보정된 최대 행동 속도 조회
    public int ActionPatternCount => actionPatterns == null ? 0 : actionPatterns.Count; // 전체 행동 패턴 수 조회
    public EnemyActionPatternData GetActionPattern(int patternIndex) // 지정 순번 행동 패턴 조회
    { // 패턴 조회 시작
        if (actionPatterns == null || patternIndex < 0 || patternIndex >= actionPatterns.Count) // 패턴 범위 확인
        { // 잘못된 순번 처리 시작
            return null; // 행동 패턴 없음 반환
        } // 잘못된 순번 처리 종료
        return actionPatterns[patternIndex]; // 지정 행동 패턴 반환
    } // 패턴 조회 종료
} // 클래스 종료
