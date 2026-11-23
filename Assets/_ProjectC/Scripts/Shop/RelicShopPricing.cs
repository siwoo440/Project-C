public static class RelicShopPricing // 시트 기반 유물 상점 가격 계산
{ // 가격 계산 시작
    public static int Calculate(RelicSheetEntry entry, string roomId, string offerId) // 상점별 유물 가격 계산
    { // 가격 계산 본문 시작
        uint hash = 2166136261u; // 안정적인 해시 초기값
        unchecked // 정수 순환 연산 허용
        { // 해시 계산 시작
            foreach (char character in roomId) // 상점 방 문자 순회
            { // 상점 방 해시 시작
                hash = (hash ^ character) * 16777619u; // 상점 방 식별값 반영
            } // 상점 방 해시 종료
            hash = (hash ^ 0xffu) * 16777619u; // 식별자 경계 구분
            foreach (char character in offerId) // 상품 식별자 문자 순회
            { // 상품 해시 시작
                hash = (hash ^ character) * 16777619u; // 상품 식별값 반영
            } // 상품 해시 종료
        } // 해시 계산 종료
        uint range = (uint)(entry.BuyMax - entry.BuyMin + 1); // 양끝을 포함한 가격 개수
        return entry.BuyMin + (int)(hash % range); // 범위 내 재현 가능한 가격 반환
    } // 가격 계산 본문 종료
} // 가격 계산 종료
