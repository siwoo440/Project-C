using System; // Unity 직렬화 지정
using UnityEngine; // 상품 원본 데이터 사용

[Serializable] // 카탈로그 내부 직렬화 지정
public sealed class ShopOfferData // 상점 단일 상품 데이터
{ // 상품 데이터 시작
    [SerializeField] private string offerId; // 상품 고유 ID
    [SerializeField] private ShopOfferType offerType; // 상품 종류
    [Min(0)] // 가격 음수 방지
    [SerializeField] private int price; // 임시 골드 가격
    [SerializeField] private CardData card; // 카드 상품 원본
    [SerializeField] private CharacterData cardOwner; // 구매 카드 소유자
    [SerializeField] private RelicData relic; // 유물 상품 원본
    [SerializeField] private PotionData potion; // 일반 포션 상품 원본

    public string OfferId => offerId; // 상품 ID 조회
    public ShopOfferType OfferType => offerType; // 상품 종류 조회
    public int Price => Mathf.Max(0, price); // 안전 가격 조회
    public CardData Card => card; // 카드 원본 조회
    public CharacterData CardOwner => cardOwner; // 카드 소유자 조회
    public RelicData Relic => relic; // 유물 원본 조회
    public PotionData Potion => potion; // 포션 원본 조회

    public Sprite Icon // 상품 이미지 조회
    { // 상품 이미지 조회 시작
        get // 상품 이미지 반환 시작
        { // 상품 이미지 반환 블록 시작
            switch (offerType) // 상품 종류 분기
            { // 상품 이미지 분기 시작
                case ShopOfferType.Card: // 카드 이미지 분기
                    return card != null ? card.Artwork : null; // 카드 일러스트 반환
                case ShopOfferType.Relic: // 유물 이미지 분기
                    return relic != null ? relic.Icon : null; // 유물 아이콘 반환
                case ShopOfferType.Potion: // 포션 이미지 분기
                    return potion != null ? potion.Icon : null; // 포션 아이콘 반환
                default: // 알 수 없는 상품 이미지 분기
                    return null; // 이미지 없음 반환
            } // 상품 이미지 분기 종료
        } // 상품 이미지 반환 블록 종료
    } // 상품 이미지 조회 종료

    public string DisplayName // 상품 표시 이름 조회
    { // 상품 이름 조회 시작
        get // 상품 이름 반환 시작
        { // 상품 이름 반환 블록 시작
            switch (offerType) // 상품 종류 분기
            { // 상품 종류 분기 시작
                case ShopOfferType.Card: // 카드 상품 분기
                    return card != null ? card.DisplayName : "카드 미지정"; // 카드 이름 반환
                case ShopOfferType.Relic: // 유물 상품 분기
                    return relic != null ? relic.DisplayName : "유물 미지정"; // 유물 이름 반환
                case ShopOfferType.Potion: // 포션 상품 분기
                    return potion != null ? potion.DisplayName : "포션 미지정"; // 포션 이름 반환
                default: // 알 수 없는 상품 분기
                    return "상품 미지정"; // 기본 이름 반환
            } // 상품 종류 분기 종료
        } // 상품 이름 반환 블록 종료
    } // 상품 이름 조회 종료

    public bool IsValidData() // 상품 데이터 유효성 검사
    { // 상품 검사 시작
        if (string.IsNullOrWhiteSpace(offerId) || price < 0) // ID와 가격 유효성 확인
        { // 공통 오류 처리 시작
            return false; // 잘못된 상품 반환
        } // 공통 오류 처리 종료

        switch (offerType) // 상품 종류별 원본 검사
        { // 상품 검사 분기 시작
            case ShopOfferType.Card: // 카드 상품 검사
                return card != null && cardOwner != null; // 카드와 소유자 존재 반환
            case ShopOfferType.Relic: // 유물 상품 검사
                return relic != null && relic.IsValidData(); // 유물 유효성 반환
            case ShopOfferType.Potion: // 포션 상품 검사
                return potion != null && potion.IsValidData(); // 포션 유효성 반환
            default: // 알 수 없는 상품 검사
                return false; // 잘못된 상품 종류 반환
        } // 상품 검사 분기 종료
    } // 상품 검사 종료
} // 상품 데이터 종료
