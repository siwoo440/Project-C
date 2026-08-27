using System; // 거래 실행 함수 사용
using System.Collections.Generic; // 판매 완료 집합 사용

public sealed class ShopTransactionService // 상점 원자 거래 서비스
{ // 거래 서비스 시작
    private readonly IShopWallet wallet; // 연결 골드 지갑
    private readonly HashSet<string> purchasedOfferIds = new HashSet<string>(); // 판매 완료 상품 ID 집합

    public ShopTransactionService(IShopWallet shopWallet) // 거래 서비스 생성
    { // 거래 서비스 생성 시작
        wallet = shopWallet ?? throw new ArgumentNullException(nameof(shopWallet)); // 필수 지갑 저장
    } // 거래 서비스 생성 종료

    public bool IsPurchased(string offerId) // 상품 판매 완료 여부 조회
    { // 판매 완료 조회 시작
        return !string.IsNullOrWhiteSpace(offerId) && purchasedOfferIds.Contains(offerId); // 유효 ID 판매 완료 반환
    } // 판매 완료 조회 종료

    public ShopPurchaseResult TryPurchase(string offerId, int price, Func<bool> canDeliver, Func<bool> deliver) // 일반 상품 구매 시도
    { // 일반 상품 구매 시작
        if (string.IsNullOrWhiteSpace(offerId) || price < 0 || canDeliver == null || deliver == null) // 상품 입력 유효성 확인
        { // 잘못된 상품 처리 시작
            return ShopPurchaseResult.InvalidOffer; // 잘못된 상품 결과 반환
        } // 잘못된 상품 처리 종료

        if (purchasedOfferIds.Contains(offerId)) // 기존 판매 완료 확인
        { // 재구매 차단 시작
            return ShopPurchaseResult.AlreadyPurchased; // 판매 완료 결과 반환
        } // 재구매 차단 종료

        if (!canDeliver()) // 상품 지급 가능 여부 확인
        { // 지급 불가 처리 시작
            return ShopPurchaseResult.Unavailable; // 지급 불가 결과 반환
        } // 지급 불가 처리 종료

        if (wallet.Gold < price || !wallet.TrySpend(price)) // 잔액 확인과 실제 차감 시도
        { // 골드 부족 처리 시작
            return ShopPurchaseResult.InsufficientGold; // 골드 부족 결과 반환
        } // 골드 부족 처리 종료

        if (!deliver()) // 실제 상품 지급 시도
        { // 지급 실패 처리 시작
            wallet.AddGold(price); // 차감 골드 전액 환불
            return ShopPurchaseResult.DeliveryFailed; // 지급 실패 결과 반환
        } // 지급 실패 처리 종료

        purchasedOfferIds.Add(offerId); // 판매 완료 상품 등록
        return ShopPurchaseResult.Success; // 구매 성공 결과 반환
    } // 일반 상품 구매 종료

    public ShopPurchaseResult TryCardRemoval(int price, Func<bool> canRemove, Func<bool> remove) // 카드 제거 서비스 시도
    { // 카드 제거 거래 시작
        if (price < 0 || canRemove == null || remove == null) // 제거 입력 유효성 확인
        { // 잘못된 제거 처리 시작
            return ShopPurchaseResult.InvalidOffer; // 잘못된 상품 결과 반환
        } // 잘못된 제거 처리 종료

        if (!canRemove()) // 카드 제거 가능 여부 확인
        { // 제거 불가 처리 시작
            return ShopPurchaseResult.Unavailable; // 제거 불가 결과 반환
        } // 제거 불가 처리 종료

        if (wallet.Gold < price || !wallet.TrySpend(price)) // 제거 비용 보유와 차감 확인
        { // 제거 비용 부족 처리 시작
            return ShopPurchaseResult.InsufficientGold; // 골드 부족 결과 반환
        } // 제거 비용 부족 처리 종료

        if (!remove()) // 실제 카드 제거 시도
        { // 카드 제거 실패 처리 시작
            wallet.AddGold(price); // 제거 비용 전액 환불
            return ShopPurchaseResult.DeliveryFailed; // 제거 실패 결과 반환
        } // 카드 제거 실패 처리 종료

        return ShopPurchaseResult.Success; // 카드 제거 성공 반환
    } // 카드 제거 거래 종료
} // 거래 서비스 종료
