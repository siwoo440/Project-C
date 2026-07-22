using System; // 이벤트 자료형 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleUnit : MonoBehaviour // 전투 유닛 체력 관리자
{
    [Header("기본 정보")] // 기본 정보 구분
    [SerializeField] private string unitName = "UNIT"; // 유닛 표시 이름
    [SerializeField, Min(1)] private int maxHealth = 100; // 최대 체력

    public event Action<int, int> HealthChanged; // 체력 변경 알림

    public string UnitName => unitName; // 유닛 이름 반환
    public int CurrentHealth { get; private set; } // 현재 체력
    public int MaxHealth => maxHealth; // 최대 체력 반환
    public bool IsDefeated => CurrentHealth <= 0; // 전투 불능 여부

    private void Awake() // 유닛 체력 초기화
    {
        CurrentHealth = maxHealth; // 현재 체력 최댓값 설정
    }

    private void Start() // 초기 체력 알림
    {
        NotifyHealthChanged(); // 초기 체력 UI 전달
    }

    public void TakeDamage(int amount) // 피해 적용
    {
        if (amount <= 0) // 유효한 피해량 확인
        {
            return; // 잘못된 피해 처리 중단
        }

        if (IsDefeated) // 전투 불능 상태 확인
        {
            return; // 추가 피해 처리 중단
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0, maxHealth); // 현재 체력 감소
        NotifyHealthChanged(); // 변경 체력 UI 전달
    }

    public void Heal(int amount) // 체력 회복
    {
        if (amount <= 0) // 유효한 회복량 확인
        {
            return; // 잘못된 회복 처리 중단
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth); // 현재 체력 증가
        NotifyHealthChanged(); // 변경 체력 UI 전달
    }

    public void RestoreFullHealth() // 체력 완전 회복
    {
        CurrentHealth = maxHealth; // 현재 체력 최댓값 설정
        NotifyHealthChanged(); // 변경 체력 UI 전달
    }

    private void NotifyHealthChanged() // 체력 변경 알림 전송
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth); // 현재 체력과 최대 체력 전달
    }

    private void OnValidate() // Inspector 입력값 검증
    {
        maxHealth = Mathf.Max(1, maxHealth); // 최대 체력 최소값 제한
    }
}