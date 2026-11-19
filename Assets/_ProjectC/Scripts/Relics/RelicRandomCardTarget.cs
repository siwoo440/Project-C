using System; // 카드 대상 난수 함수 사용

public static class RelicRandomCardTarget // 카드 무작위 대상의 추가 추첨 처리
{ // 대상 추첨 시작
    public static int PickIndex(int count, Func<int, int> draw) // 같은 후보 목록의 최종 추첨 위치 결정
    { // 대상 선택 시작
        int first = draw(count); // 카드의 원래 대상 한 번 추첨
        var relics = RelicRunManager.Instance?.Inventory; // 실제 회차의 보유 유물 조회
        if (count > 1 && relics != null && relics.ContainsRelic("RLC_WORN_DIE")) // 여러 후보와 낡은 주사위 보유 확인
        { // 추가 추첨 시작
            return draw(count); // 첫 대상도 포함한 같은 후보에서 한 번 더 추첨
        } // 추가 추첨 종료
        return first; // 유물 미보유 또는 단일 대상의 기존 결과 반환
    } // 대상 선택 종료
} // 대상 추첨 종료
