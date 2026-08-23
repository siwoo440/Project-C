using System.Collections.Generic; // 대상 목록 자료형 사용

public static class BattleCardDamageContext // 카드 피해 계산 문맥
{
    private static CardInstance activeCard; // 현재 피해 카드
    private static IReadOnlyList<BattleUnitRuntime> activeTargets; // 현재 카드 대상 목록
    private static int targetIndex; // 다음 계산 대상 순번

    public static void Begin(
        CardInstance card,
        IReadOnlyList<BattleUnitRuntime> targets) // 카드 피해 문맥 시작
    {
        activeCard = card; // 현재 카드 저장
        activeTargets = targets; // 대상 목록 저장
        targetIndex = 0; // 대상 순번 초기화
    }

    public static bool TryConsume(
        out CardInstance card,
        out BattleUnitRuntime target) // 다음 카드 피해 대상 조회
    {
        card = null; // 반환 카드 초기화
        target = null; // 반환 대상 초기화

        if (activeCard == null || activeTargets == null)
        {
            Clear(); // 잘못된 문맥 정리
            return false;
        }

        while (targetIndex < activeTargets.Count)
        {
            CardInstance currentCard = activeCard; // 현재 카드 임시 저장
            BattleUnitRuntime currentTarget =
                activeTargets[targetIndex]; // 현재 대상 임시 저장

            targetIndex += 1; // 다음 대상 순번 이동

            if (targetIndex >= activeTargets.Count)
            {
                Clear(); // 마지막 대상 이후 문맥 정리
            }

            if (currentTarget == null)
            {
                continue; // 비어 있는 대상 건너뛰기
            }

            card = currentCard; // 카드 반환
            target = currentTarget; // 대상 반환
            return true; // 카드 피해 문맥 사용 성공
        }

        Clear(); // 대상 소진 문맥 정리
        return false;
    }

    public static void Clear() // 카드 피해 문맥 초기화
    {
        activeCard = null; // 현재 카드 제거
        activeTargets = null; // 대상 목록 제거
        targetIndex = 0; // 대상 순번 초기화
    }
}
