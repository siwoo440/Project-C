using System; // 이벤트 자료형 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleActionPointRuntime : MonoBehaviour // 전투 AP 관리자
{
    [Header("AP 설정")] // AP 설정 구분
    [SerializeField] private int maxActionPoints = 6; // 최대 AP
    [SerializeField] private int currentActionPoints; // 현재 AP

    public int MaxActionPoints => maxActionPoints; // 최대 AP 반환
    public int CurrentActionPoints => currentActionPoints; // 현재 AP 반환

    public event Action<int, int> ActionPointsChanged; // AP 변경 이벤트

    private void Awake() // AP 관리자 초기화
    {
        maxActionPoints = Mathf.Max(0, maxActionPoints); // 최대 AP 음수 방지
        ResetForPlayerTurn(); // 최초 AP 충전
    }

    public bool CanSpend(int amount) // 지정 AP 지불 가능 여부 확인
    {
        if (amount < 0) // 음수 비용 확인
        {
            return false; // 지불 불가 반환
        }

        return currentActionPoints >= amount; // 현재 AP 비교 결과 반환
    }

    public bool TrySpend(int amount) // 지정 AP 지불 시도
    {
        if (amount < 0) // 음수 비용 확인
        {
            Debug.LogWarning($"Action point cost cannot be negative: {amount}", this); // 음수 비용 경고
            return false; // 지불 실패 반환
        }

        if (CanSpend(amount) == false) // AP 부족 확인
        {
            Debug.LogWarning($"Not enough action points: Current {currentActionPoints} / Required {amount}", this); // AP 부족 경고
            return false; // 지불 실패 반환
        }

        currentActionPoints -= amount; // 현재 AP 차감
        NotifyActionPointsChanged(); // AP 변경 알림
        Debug.Log($"Action points spent: {amount} / Remaining {currentActionPoints}", this); // AP 차감 결과 출력
        return true; // 지불 성공 반환
    }

    [ContextMenu("Reset Action Points")] // AP 초기화 메뉴
    public void ResetForPlayerTurn() // 플레이어 턴 AP 초기화
    {
        currentActionPoints = maxActionPoints; // 현재 AP 최대치 설정
        NotifyActionPointsChanged(); // AP 변경 알림
        Debug.Log($"Action points reset: {currentActionPoints} / {maxActionPoints}", this); // AP 초기화 결과 출력
    }

    [ContextMenu("Spend One Action Point")] // AP 1 소비 메뉴
    private void SpendOneActionPoint() // AP 소비 테스트
    {
        TrySpend(1); // AP 1 지불 시도
    }

    private void NotifyActionPointsChanged() // AP 변경 이벤트 전달
    {
        ActionPointsChanged?.Invoke(currentActionPoints, maxActionPoints); // 현재 AP와 최대 AP 전달
    }
}