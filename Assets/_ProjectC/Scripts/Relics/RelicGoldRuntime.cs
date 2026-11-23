using System; // 기본 이벤트 기능 사용
using UnityEngine; // 수치 보정 기능 사용

public sealed class RelicGoldRuntime // 유물 시스템 임시 골드 지갑
{
    public int Gold { get; private set; } // 현재 골드 보유량
    public event Action<int> GoldChanged; // 골드 변경 이벤트

    public RelicGoldRuntime(int initialGold = 0) // 임시 골드 지갑 생성
    {
        Gold = Mathf.Max(0, initialGold); // 음수 없는 시작 골드 저장
    }

    public int AddGold(int amount) // 골드 추가
    {
        int safeAmount = Mathf.Max(0, amount); // 음수 없는 추가량 계산
        if (safeAmount == 0) // 추가량 존재 여부 확인
        {
            return 0; // 추가 없음 반환
        }

        Gold += safeAmount; // 현재 골드 증가
        GoldChanged?.Invoke(Gold); // 골드 변경 알림
        return safeAmount; // 실제 추가량 반환
    }

    public void ResetGold() // 골드 초기화
    {
        if (Gold == 0) // 이미 초기 상태 확인
        {
            return; // 초기화 중단
        }

        Gold = 0; // 골드 영으로 초기화
        GoldChanged?.Invoke(Gold); // 골드 변경 알림
    }
}
