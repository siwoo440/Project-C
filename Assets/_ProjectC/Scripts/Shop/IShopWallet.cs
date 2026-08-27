public interface IShopWallet // 상점 골드 지갑 규약
{ // 지갑 규약 시작
    int Gold { get; } // 현재 골드 조회
    bool TrySpend(int amount); // 골드 차감 시도
    void AddGold(int amount); // 골드 환불 지급
} // 지갑 규약 종료
