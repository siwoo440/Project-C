using System; // 별도 피해의 실제 실행 동작
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

    internal static bool IsPendingHit(BattleUnitRuntime source, BattleUnitRuntime target) // 문맥을 소비하지 않고 현재 직접 카드 타격의 출처와 대상 대조
    { // 원래 직접 카드 문맥 확인 시작
        return activeCard != null && activeCard.OwnerUnit == source && activeTargets != null && // 실제 카드 소유자와 원래 대상 목록 확인
            targetIndex < activeTargets.Count && activeTargets[targetIndex] == target; // 다음 현재 대상과 일치할 때만 직접 타격 인정
    } // 원래 직접 카드 문맥 확인 종료

    internal static CardType? GetPendingCardType(BattleUnitRuntime source, BattleUnitRuntime target) // 다음 직접 카드 타격의 원본 계열 확인
    { // 타입을 조회할 때 원래 대상 인덱스 유지
        return IsPendingHit(source, target) ? activeCard.CardType : null; // 정확히 일치하는 소유자와 대상의 원본 카드 계열만 반환
    } // 타입을 조회할 때 원래 대상 인덱스 유지 종료

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

    internal static void WithoutActiveCard(Action action) // 별도 원본 피해에 카드 약점과 다음 대상 소비가 없도록 분리
    { // 기존 카드 문맥 저장 시작
        var card = activeCard; // 남은 카드 원본
        var targets = activeTargets; // 남은 대상 순서
        int nextIndex = targetIndex; // 다음 원래 타격의 위치
        Clear(); // 별도 피해에서 카드 문맥 차단
        try // 원래 문맥의 복구 보장
        { // 문맥 없는 원본 피해 실행 시작
            action(); // 원래 카드와 관계없는 실제 피해 실행
        } // 문맥 없는 원본 피해 실행 종료
        finally // 원래 카드 피해 문맥 복구
        { // 보관한 카드와 대상의 재개 시작
            activeCard = card; // 원래 진행하던 카드 복원
            activeTargets = targets; // 원래 남아 있던 대상 목록 복원
            targetIndex = nextIndex; // 앞서 소비하고 남은 원래 인덱스 복원
        } // 보관한 카드와 대상의 재개 종료
    } // 기존 카드 문맥 저장 종료

    public static void Clear() // 카드 피해 문맥 초기화
    {
        activeCard = null; // 현재 카드 제거
        activeTargets = null; // 대상 목록 제거
        targetIndex = 0; // 대상 순번 초기화
    }
}
