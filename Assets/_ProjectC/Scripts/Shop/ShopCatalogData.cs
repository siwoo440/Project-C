using System.Collections.Generic; // 상품 목록과 ID 집합 사용
using UnityEngine; // ScriptableObject 데이터 사용

[CreateAssetMenu(fileName = "ShopCatalog_New", menuName = "Project C/Data/Shop Catalog")] // 상점 카탈로그 생성 메뉴
public sealed class ShopCatalogData : ScriptableObject // Prototype 상점 카탈로그
{ // 상점 카탈로그 시작
    [SerializeField] private List<ShopOfferData> offers = new List<ShopOfferData>(); // 판매 상품 목록
    [Min(0)] // 제거 가격 음수 방지
    [SerializeField] private int cardRemovalPrice = 150; // 카드 제거 임시 가격

    public IReadOnlyList<ShopOfferData> Offers => offers; // 판매 상품 목록 조회
    public int CardRemovalPrice => Mathf.Max(0, cardRemovalPrice); // 카드 제거 가격 조회

    public bool ContainsOffer(string offerId) // 카탈로그 상품 포함 여부
    { // 상품 포함 검사 시작
        if (string.IsNullOrWhiteSpace(offerId)) // 빈 상품 ID 확인
        { // 빈 상품 ID 처리 시작
            return false; // 상품 없음 반환
        } // 빈 상품 ID 처리 종료

        foreach (ShopOfferData offer in offers) // 전체 상품 순회
        { // 상품 순회 시작
            if (offer != null && offer.OfferId == offerId) // 동일 상품 ID 확인
            { // 동일 상품 처리 시작
                return true; // 상품 포함 반환
            } // 동일 상품 처리 종료
        } // 상품 순회 종료

        return false; // 상품 없음 반환
    } // 상품 포함 검사 종료

    public bool IsValidData() // 카탈로그 유효성 검사
    { // 카탈로그 검사 시작
        if (offers.Count < 1 || cardRemovalPrice < 0) // 상품 수와 제거 가격 확인
        { // 기본 오류 처리 시작
            return false; // 잘못된 카탈로그 반환
        } // 기본 오류 처리 종료

        HashSet<string> offerIds = new HashSet<string>(); // 중복 검사 ID 집합 생성
        foreach (ShopOfferData offer in offers) // 전체 상품 순회
        { // 상품 검사 순회 시작
            if (offer == null || !offer.IsValidData() || !offerIds.Add(offer.OfferId)) // 상품 유효성과 ID 중복 확인
            { // 상품 오류 처리 시작
                return false; // 잘못된 카탈로그 반환
            } // 상품 오류 처리 종료
        } // 상품 검사 순회 종료

        return true; // 정상 카탈로그 반환
    } // 카탈로그 검사 종료
} // 상점 카탈로그 종료
