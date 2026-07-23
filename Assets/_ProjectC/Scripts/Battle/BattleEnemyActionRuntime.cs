using UnityEngine; // Unity 기본 기능

public sealed class BattleEnemyActionRuntime : MonoBehaviour // 적 행동 관리자
{
    [Header("행동 유닛 연결")] // 행동 유닛 연결 구분
    [SerializeField] private BattleUnit enemyUnit; // 행동할 적 유닛
    [SerializeField] private BattleUnit targetUnit; // 공격 대상 유닛

    [Header("공격 설정")] // 공격 설정 구분
    [SerializeField, Min(1)] private int damageAmount = 12; // 기본 공격 피해량

    public BattleUnit EnemyUnit => enemyUnit; // 행동 적 유닛 반환
    public BattleUnit TargetUnit => targetUnit; // 공격 대상 반환
    public int DamageAmount => damageAmount; // 공격 피해량 반환

    public bool CanExecute() // 행동 실행 가능 여부
    {
        if (enemyUnit == null) // 적 유닛 연결 확인
        {
            return false; // 실행 불가 반환
        }

        if (targetUnit == null) // 대상 유닛 연결 확인
        {
            return false; // 실행 불가 반환
        }

        if (enemyUnit.UnitTeam != BattleUnitTeam.Enemy) // 적 진영 확인
        {
            return false; // 실행 불가 반환
        }

        if (targetUnit.UnitTeam != BattleUnitTeam.Ally) // 아군 대상 확인
        {
            return false; // 실행 불가 반환
        }

        if (enemyUnit.IsDefeated) // 적 전투 불능 확인
        {
            return false; // 실행 불가 반환
        }

        if (targetUnit.IsDefeated) // 대상 전투 불능 확인
        {
            return false; // 실행 불가 반환
        }

        return damageAmount > 0; // 양수 피해량 여부 반환
    }

    public string GetIntentText() // 적 행동 예고 문구 생성
    {
        if (enemyUnit == null) // 적 유닛 연결 확인
        {
            return "UNKNOWN ENEMY"; // 적 누락 문구 반환
        }

        if (targetUnit == null) // 대상 유닛 연결 확인
        {
            return $"{enemyUnit.UnitName} : NO TARGET"; // 대상 누락 문구 반환
        }

        return $"{enemyUnit.UnitName} → {targetUnit.UnitName} : {damageAmount} DAMAGE"; // 공격 예고 문구 반환
    }

    public bool TryExecute() // 적 행동 실행 시도
    {
        if (CanExecute() == false) // 행동 실행 가능 여부 확인
        {
            Debug.LogWarning("Enemy action cannot be executed.", this); // 행동 실행 실패 경고
            return false; // 행동 실행 실패 반환
        }

        targetUnit.TakeDamage(damageAmount); // 공격 대상 피해 적용
        Debug.Log($"Enemy action executed: {enemyUnit.UnitName} → {targetUnit.UnitName} / Damage {damageAmount}", this); // 적 행동 결과 출력
        return true; // 행동 실행 성공 반환
    }

    private void OnValidate() // Inspector 입력값 검증
    {
        damageAmount = Mathf.Max(1, damageAmount); // 피해량 최소값 제한
    }
}