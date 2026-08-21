using System; // 기본 이벤트 기능 사용
using UnityEngine; // 유니티 자료형 사용
public sealed class BattleUnitRuntime // 전투 유닛 런타임 상태
{ // 클래스 시작
    public string UnitId { get; } // 유닛 고유 ID
    public string DisplayName { get; } // 유닛 표시 이름
    public BattleTeam Team { get; } // 유닛 진영
    public Sprite Portrait { get; } // 유닛 초상화
    public int MaxHealth { get; } // 최대 체력
    public int CurrentHealth { get; private set; } // 현재 체력
    public bool IsDead { get; private set; } // 사망 여부
    public CharacterData CharacterSource { get; } // 아군 원본 데이터
    public EnemyData EnemySource { get; } // 적 원본 데이터
    public event Action<BattleUnitRuntime> HealthChanged; // 체력 변경 이벤트
    public event Action<BattleUnitRuntime> Died; // 사망 이벤트
    private BattleUnitRuntime(string unitId, string displayName, BattleTeam team, Sprite portrait, int maxHealth, CharacterData characterSource, EnemyData enemySource) // 런타임 상태 생성자
    { // 생성자 시작
        UnitId = unitId; // 유닛 ID 저장
        DisplayName = displayName; // 표시 이름 저장
        Team = team; // 진영 저장
        Portrait = portrait; // 초상화 저장
        MaxHealth = Mathf.Max(1, maxHealth); // 최소 최대 체력 보정
        CurrentHealth = MaxHealth; // 현재 체력 초기화
        IsDead = false; // 생존 상태 초기화
        CharacterSource = characterSource; // 아군 원본 저장
        EnemySource = enemySource; // 적 원본 저장
    } // 생성자 종료
    public static BattleUnitRuntime CreateAlly(CharacterData characterData) // 아군 런타임 생성
    { // 아군 생성 시작
        if (characterData == null) // 아군 원본 누락 확인
        { // 누락 처리 시작
            throw new ArgumentNullException(nameof(characterData)); // 잘못된 인수 예외
        } // 누락 처리 종료
        return new BattleUnitRuntime(characterData.CharacterId, characterData.DisplayName, BattleTeam.Ally, characterData.Portrait, characterData.MaxHealth, characterData, null); // 아군 상태 반환
    } // 아군 생성 종료
    public static BattleUnitRuntime CreateEnemy(EnemyData enemyData) // 적 런타임 생성
    { // 적 생성 시작
        if (enemyData == null) // 적 원본 누락 확인
        { // 누락 처리 시작
            throw new ArgumentNullException(nameof(enemyData)); // 잘못된 인수 예외
        } // 누락 처리 종료
        return new BattleUnitRuntime(enemyData.EnemyId, enemyData.DisplayName, BattleTeam.Enemy, enemyData.Portrait, enemyData.MaxHealth, null, enemyData); // 적 상태 반환
    } // 적 생성 종료
    public int TakeDamage(int damageAmount) // 피해 처리
    { // 피해 처리 시작
        if (damageAmount <= 0 || IsDead) // 무효 피해 확인
        { // 무효 처리 시작
            return 0; // 적용 피해 없음
        } // 무효 처리 종료
        int previousHealth = CurrentHealth; // 변경 전 체력 저장
        CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount); // 현재 체력 감소
        int appliedDamage = previousHealth - CurrentHealth; // 실제 피해 계산
        bool diedNow = CurrentHealth == 0; // 이번 사망 여부 계산
        if (diedNow) // 사망 상태 확인
        { // 사망 처리 시작
            IsDead = true; // 사망 상태 저장
        } // 사망 처리 종료
        HealthChanged?.Invoke(this); // 체력 변경 알림
        if (diedNow) // 신규 사망 확인
        { // 사망 알림 시작
            Died?.Invoke(this); // 사망 이벤트 알림
        } // 사망 알림 종료
        return appliedDamage; // 실제 피해 반환
    } // 피해 처리 종료
    public int RestoreHealth(int healAmount) // 체력 회복 처리
    { // 체력 회복 시작
        if (healAmount <= 0 || IsDead || CurrentHealth >= MaxHealth) // 무효 회복 확인
        { // 무효 회복 처리 시작
            return 0; // 적용 회복 없음
        } // 무효 회복 처리 종료
        int previousHealth = CurrentHealth; // 변경 전 체력 저장
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healAmount); // 현재 체력 회복
        int appliedHealing = CurrentHealth - previousHealth; // 실제 회복량 계산
        HealthChanged?.Invoke(this); // 체력 변경 알림
        return appliedHealing; // 실제 회복량 반환
    } // 체력 회복 종료
} // 클래스 종료
