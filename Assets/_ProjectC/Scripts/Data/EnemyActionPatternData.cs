using System; // 직렬화와 문자열 기능 사용
using UnityEngine; // 유니티 직렬화 기능 사용
[Serializable] // 인스펙터 저장 허용
public sealed class EnemyActionPatternData // 적 순차 행동 한 단계 데이터
{ // 클래스 시작
    [SerializeField] private string displayName = "행동"; // 행동 표시 이름
    [SerializeField] private EnemyActionType actionType = EnemyActionType.Attack; // 행동 종류
    [Min(0)] // 공격력 최소값
    [SerializeField] private int attackPower = 8; // 공격 피해 수치
    [SerializeField] private BattleDamageType damageType = BattleDamageType.Physical; // 공격 피해 유형
    [SerializeField] private BattleStatusEffectType statusEffectType = BattleStatusEffectType.None; // 상태 이상 종류
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
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? GetDefaultDisplayName() : displayName; // 보정된 행동 이름 조회
    public EnemyActionType ActionType => actionType; // 행동 종류 조회
    public int ActionAmount => actionType == EnemyActionType.Attack ? attackPower : statusEffectValue; // 행동 종류별 수치 조회
    public BattleDamageType DamageType => damageType; // 피해 유형 조회
    public BattleStatusEffectType StatusEffectType => statusEffectType; // 상태 이상 종류 조회
    public int StatusEffectDuration => Mathf.Max(1, statusEffectDuration); // 보정된 상태 지속 횟수 조회
    public int StatusEffectMaximumStacks => Mathf.Max(1, statusEffectMaximumStacks); // 보정된 상태 최대 중첩 조회
    public EnemyTargetRule TargetRule => targetRule; // 대상 선택 규칙 조회
    public int MinimumActionSpeed => Mathf.Max(1, minimumActionSpeed); // 보정된 최소 행동 속도 조회
    public int MaximumActionSpeed => Mathf.Max(MinimumActionSpeed, maximumActionSpeed); // 보정된 최대 행동 속도 조회
    public bool IsValid => IsConfigurationValid(); // 행동 설정 유효성 조회
    private bool IsConfigurationValid() // 행동 설정 유효성 확인
    { // 설정 확인 시작
        if (actionType == EnemyActionType.Attack) // 공격 행동 확인
        { // 공격 설정 처리 시작
            return attackPower > 0; // 공격 수치 유효성 반환
        } // 공격 설정 처리 종료
        return actionType == EnemyActionType.ApplyStatusEffect && statusEffectType != BattleStatusEffectType.None && statusEffectValue > 0 && statusEffectDuration > 0 && statusEffectMaximumStacks > 0; // 상태 행동 설정 유효성 반환
    } // 설정 확인 종료
    private string GetDefaultDisplayName() // 기본 행동 이름 조회
    { // 기본 이름 조회 시작
        if (actionType == EnemyActionType.ApplyStatusEffect) // 상태 행동 확인
        { // 상태 이름 처리 시작
            return "상태 이상"; // 기본 상태 행동 이름 반환
        } // 상태 이름 처리 종료
        return actionType == EnemyActionType.Attack ? "공격" : "행동 없음"; // 공격 또는 빈 행동 이름 반환
    } // 기본 이름 조회 종료
} // 클래스 종료
