public enum ShopPurchaseResult // 상점 거래 결과
{ // 거래 결과 시작
    Success, // 거래 성공
    InvalidOffer, // 잘못된 상품
    AlreadyPurchased, // 판매 완료 상품
    InsufficientGold, // 골드 부족
    Unavailable, // 보관 공간 또는 조건 부족
    DeliveryFailed // 상품 지급 실패
} // 거래 결과 종료
