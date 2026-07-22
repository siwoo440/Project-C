using UnityEngine; // Unity 기본 기능

public sealed class BattleTestController : MonoBehaviour // 전투 체력 테스트 관리자
{
    [Header("테스트 대상")] // 테스트 대상 구분
    [SerializeField] private BattleUnit allyUnit; // 아군 테스트 유닛
    [SerializeField] private BattleUnit enemyUnit; // 적 테스트 유닛

    [Header("테스트 수치")] // 테스트 수치 구분
    [SerializeField, Min(1)] private int damageAmount = 10; // 테스트 피해량
    [SerializeField, Min(1)] private int healAmount = 10; // 테스트 회복량

    public void OnDamageAllyButtonClicked() // 아군 피해 버튼 처리
    {
        ApplyDamage(allyUnit); // 아군 피해 적용
    }

    public void OnHealAllyButtonClicked() // 아군 회복 버튼 처리
    {
        ApplyHeal(allyUnit); // 아군 회복 적용
    }

    public void OnDamageEnemyButtonClicked() // 적 피해 버튼 처리
    {
        ApplyDamage(enemyUnit); // 적 피해 적용
    }

    public void OnHealEnemyButtonClicked() // 적 회복 버튼 처리
    {
        ApplyHeal(enemyUnit); // 적 회복 적용
    }

    private void ApplyDamage(BattleUnit targetUnit) // 지정 유닛 피해 처리
    {
        if (targetUnit == null) // 대상 유닛 연결 확인
        {
            Debug.LogError("피해 대상 BattleUnit이 연결되지 않았습니다.", this); // 대상 누락 오류
            return; // 피해 처리 중단
        }

        targetUnit.TakeDamage(damageAmount); // 테스트 피해 적용
    }

    private void ApplyHeal(BattleUnit targetUnit) // 지정 유닛 회복 처리
    {
        if (targetUnit == null) // 대상 유닛 연결 확인
        {
            Debug.LogError("회복 대상 BattleUnit이 연결되지 않았습니다.", this); // 대상 누락 오류
            return; // 회복 처리 중단
        }

        targetUnit.Heal(healAmount); // 테스트 회복 적용
    }
}