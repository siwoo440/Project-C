public static class DevilBloodPrice // 악마 전용 카드의 체력 비용 처리
{ // 체력 비용 처리 시작
    private const string CardId = "CRD_ARC_15_BLOOD_PRICE"; // 악마 전용 공격 카드 식별자
    public static bool IsBloodPriceCard(CardInstance card) // 악마 체력 비용 카드 여부 확인
    { // 전용 카드 확인 시작
        return card != null && card.SourceData != null && card.SourceData.CardId == CardId; // 전용 카드 식별자 일치 여부 반환
    } // 전용 카드 확인 종료
    public static bool CanPay(CardInstance card) // 체력 비용 지불 가능 확인
    { // 비용 지불 가능 확인 시작
        return IsBloodPriceCard(card) && card.OwnerUnit != null && card.OwnerUnit.CanSpendHealth(card.SourceData.SecondaryEffectValue); // 카드 소유자의 생존 비용 지불 가능 여부 반환
    } // 비용 지불 가능 확인 종료
    public static bool TryPay(CardInstance card) // 악마 전용 카드의 체력 비용 지불
    { // 비용 지불 시작
        return CanPay(card) && card.OwnerUnit.TrySpendHealth(card.SourceData.SecondaryEffectValue); // 체력 비용 지불 성공 여부 반환
    } // 비용 지불 종료
} // 체력 비용 처리 종료
