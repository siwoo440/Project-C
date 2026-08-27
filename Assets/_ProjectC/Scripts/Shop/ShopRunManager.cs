using System; // 상점 상태 이벤트 사용
using UnityEngine; // 영구 상점 관리자 사용

public sealed class ShopRunManager : MonoBehaviour // 탐사 회차 상점 관리자
{ // 상점 관리자 시작
    private static ShopRunManager instance; // 현재 상점 관리자
    private ShopCatalogData catalog; // 현재 상점 카탈로그
    private RunDeckManager runDeckManager; // 탐사 회차 덱 관리자
    private ShopTransactionService transactionService; // 상점 거래 서비스

    public static ShopRunManager Instance => instance; // 현재 상점 관리자 조회
    public ShopCatalogData Catalog => catalog; // 현재 카탈로그 조회
    public RunDeckManager RunDeck => runDeckManager; // 현재 회차 덱 조회
    public int Gold => PlayerResourceManager.EnsureInstance().Gold; // 현재 상점 골드 조회
    public bool IsReady => catalog != null && runDeckManager != null && transactionService != null; // 상점 준비 상태 조회
    public event Action Changed; // 상점 화면 변경 이벤트
    public event Action<ShopPurchaseResult, string> TransactionCompleted; // 거래 완료 이벤트

    public static ShopRunManager EnsureInstance() // 상점 관리자 준비
    { // 상점 관리자 준비 시작
        if (instance != null) // 기존 관리자 확인
        { // 기존 관리자 처리 시작
            return instance; // 기존 관리자 반환
        } // 기존 관리자 처리 종료

        ShopRunManager existingManager = FindFirstObjectByType<ShopRunManager>(); // Scene 기존 관리자 탐색
        if (existingManager != null) // 기존 Scene 관리자 확인
        { // 기존 Scene 관리자 처리 시작
            instance = existingManager; // 기존 관리자 저장
            return instance; // 기존 Scene 관리자 반환
        } // 기존 Scene 관리자 처리 종료

        GameObject managerObject = new GameObject("ShopRunManager"); // 상점 관리자 오브젝트 생성
        instance = managerObject.AddComponent<ShopRunManager>(); // 상점 관리자 컴포넌트 추가
        return instance; // 새 관리자 반환
    } // 상점 관리자 준비 종료

    private void Awake() // 상점 관리자 초기화
    { // 상점 관리자 초기화 시작
        if (instance != null && instance != this) // 중복 관리자 확인
        { // 중복 관리자 처리 시작
            Destroy(gameObject); // 중복 관리자 제거
            return; // 중복 초기화 중단
        } // 중복 관리자 처리 종료

        instance = this; // 현재 관리자 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 상태 유지
    } // 상점 관리자 초기화 종료

    public bool Prepare(ShopCatalogData catalogData, DeckData deckData) // 카탈로그와 회차 덱 준비
    { // 상점 준비 시작
        if (catalogData == null || !catalogData.IsValidData() || deckData == null || !deckData.IsValidDeck()) // 필수 데이터 유효성 확인
        { // 상점 준비 실패 처리 시작
            return false; // 상점 준비 실패 반환
        } // 상점 준비 실패 처리 종료

        runDeckManager = RunDeckManager.EnsureInstance(); // 회차 덱 관리자 준비
        runDeckManager.GetActiveCards(deckData); // 원본 덱 기반 회차 카드 준비

        if (catalog != catalogData || transactionService == null) // 새 카탈로그 또는 거래 서비스 누락 확인
        { // 거래 서비스 갱신 시작
            catalog = catalogData; // 현재 카탈로그 저장
            transactionService = new ShopTransactionService(new PlayerResourceShopWallet()); // 실제 골드 연결 거래 서비스 생성
        } // 거래 서비스 갱신 종료

        Changed?.Invoke(); // 상점 준비 알림
        return true; // 상점 준비 성공 반환
    } // 상점 준비 종료

    public ShopPurchaseResult TryPurchase(ShopOfferData offer) // 지정 상품 구매 시도
    { // 상품 구매 시작
        if (!IsReady || offer == null || !offer.IsValidData() || !catalog.ContainsOffer(offer.OfferId)) // 상점과 상품 유효성 확인
        { // 잘못된 상품 처리 시작
            return CompleteTransaction(ShopPurchaseResult.InvalidOffer, "상품 정보가 올바르지 않습니다."); // 잘못된 상품 완료 반환
        } // 잘못된 상품 처리 종료

        ShopPurchaseResult result = transactionService.TryPurchase(offer.OfferId, offer.Price, () => CanDeliver(offer), () => Deliver(offer)); // 원자 상품 거래 실행
        string message = BuildPurchaseMessage(result, offer.DisplayName); // 거래 결과 문구 생성
        return CompleteTransaction(result, message); // 거래 결과 알림과 반환
    } // 상품 구매 종료

    public ShopPurchaseResult TryRemoveCard(int cardIndex) // 지정 보유 카드 제거 시도
    { // 카드 제거 시작
        if (!IsReady) // 상점 준비 여부 확인
        { // 상점 미준비 처리 시작
            return CompleteTransaction(ShopPurchaseResult.InvalidOffer, "상점이 준비되지 않았습니다."); // 상점 미준비 결과 반환
        } // 상점 미준비 처리 종료

        string cardName = cardIndex >= 0 && cardIndex < runDeckManager.CardCount ? runDeckManager.Cards[cardIndex].Card.DisplayName : "카드"; // 제거 대상 카드 이름 결정
        ShopPurchaseResult result = transactionService.TryCardRemoval(catalog.CardRemovalPrice, () => runDeckManager.CanRemoveAt(cardIndex), () => runDeckManager.TryRemoveAt(cardIndex)); // 카드 제거 원자 거래 실행
        string message = result == ShopPurchaseResult.Success ? $"{cardName} 제거 완료" : BuildPurchaseMessage(result, cardName); // 카드 제거 결과 문구 생성
        return CompleteTransaction(result, message); // 카드 제거 결과 알림과 반환
    } // 카드 제거 종료

    public bool IsPurchased(ShopOfferData offer) // 지정 상품 판매 완료 여부
    { // 판매 완료 조회 시작
        return transactionService != null && offer != null && transactionService.IsPurchased(offer.OfferId); // 거래 서비스 판매 상태 반환
    } // 판매 완료 조회 종료

    public void AddDebugGold(int amount) // Prototype 테스트 골드 지급
    { // 테스트 골드 지급 시작
        int safeAmount = Mathf.Max(0, amount); // 음수 지급 방지
        PlayerResourceManager.EnsureInstance().AddResources(safeAmount, 0, 0, 0); // 실제 플레이어 골드 지급
        Changed?.Invoke(); // 상점 골드 변경 알림
    } // 테스트 골드 지급 종료

    private bool CanDeliver(ShopOfferData offer) // 상품 지급 가능 여부 검사
    { // 상품 지급 검사 시작
        switch (offer.OfferType) // 상품 종류 분기
        { // 상품 지급 분기 시작
            case ShopOfferType.Card: // 카드 상품 지급 검사
                return runDeckManager.ContainsOwner(offer.CardOwner); // 출전 소유자 포함 여부 반환
            case ShopOfferType.Relic: // 유물 상품 지급 검사
                return !RelicRunManager.EnsureInstance().Inventory.ContainsRelic(offer.Relic.RelicId); // 유물 중복 없음 반환
            case ShopOfferType.Potion: // 포션 상품 지급 검사
                return ConsumableRunManager.EnsureInstance().Inventory.Count < ConsumableInventoryRuntime.SlotCount; // 소모품 빈 슬롯 여부 반환
            default: // 알 수 없는 상품 검사
                return false; // 지급 불가 반환
        } // 상품 지급 분기 종료
    } // 상품 지급 검사 종료

    private bool Deliver(ShopOfferData offer) // 상품 실제 지급
    { // 상품 지급 시작
        switch (offer.OfferType) // 상품 종류 분기
        { // 상품 지급 분기 시작
            case ShopOfferType.Card: // 카드 상품 지급
                return runDeckManager.TryAddCard(offer.Card, offer.CardOwner); // 회차 덱 카드 추가 결과 반환
            case ShopOfferType.Relic: // 유물 상품 지급
                return RelicRunManager.EnsureInstance().TryAcquire(offer.Relic) == RelicAcquireResult.Acquired; // 신규 유물 획득 결과 반환
            case ShopOfferType.Potion: // 포션 상품 지급
                return ConsumableRunManager.EnsureInstance().TryAcquire(offer.Potion, out int acquiredSlotIndex) && acquiredSlotIndex >= 0; // 포션 빈 슬롯 획득 결과 반환
            default: // 알 수 없는 상품 지급
                return false; // 지급 실패 반환
        } // 상품 지급 분기 종료
    } // 상품 지급 종료

    private ShopPurchaseResult CompleteTransaction(ShopPurchaseResult result, string message) // 공통 거래 완료 처리
    { // 거래 완료 시작
        TransactionCompleted?.Invoke(result, message); // 거래 결과 알림
        Changed?.Invoke(); // 상점 상태 변경 알림
        Debug.Log($"[Shop][Day54] {message} / 결과 {result} / Gold {Gold}"); // 거래 결과 로그
        return result; // 최종 거래 결과 반환
    } // 거래 완료 종료

    private static string BuildPurchaseMessage(ShopPurchaseResult result, string productName) // 거래 결과 표시 문구 생성
    { // 결과 문구 생성 시작
        switch (result) // 거래 결과 분기
        { // 거래 결과 분기 시작
            case ShopPurchaseResult.Success: // 구매 성공 분기
                return $"{productName} 구매 완료"; // 구매 성공 문구 반환
            case ShopPurchaseResult.AlreadyPurchased: // 판매 완료 분기
                return "이미 판매된 상품입니다."; // 판매 완료 문구 반환
            case ShopPurchaseResult.InsufficientGold: // 골드 부족 분기
                return "골드가 부족합니다."; // 골드 부족 문구 반환
            case ShopPurchaseResult.Unavailable: // 지급 불가 분기
                return "중복 보유 또는 보관 공간 부족입니다."; // 지급 불가 문구 반환
            case ShopPurchaseResult.DeliveryFailed: // 지급 실패 분기
                return "상품 지급에 실패해 골드를 환불했습니다."; // 지급 실패 문구 반환
            default: // 잘못된 상품 분기
                return "상품 정보가 올바르지 않습니다."; // 잘못된 상품 문구 반환
        } // 거래 결과 분기 종료
    } // 결과 문구 생성 종료

    private void OnDestroy() // 상점 관리자 제거 처리
    { // 관리자 제거 시작
        if (instance == this) // 현재 관리자 여부 확인
        { // 현재 관리자 처리 시작
            instance = null; // 정적 관리자 참조 해제
        } // 현재 관리자 처리 종료
    } // 관리자 제거 종료

    private sealed class PlayerResourceShopWallet : IShopWallet // 플레이어 자원 상점 지갑 연결기
    { // 상점 지갑 연결기 시작
        public int Gold => PlayerResourceManager.EnsureInstance().Gold; // 실제 플레이어 골드 조회

        public bool TrySpend(int amount) // 실제 플레이어 골드 차감
        { // 실제 골드 차감 시작
            return PlayerResourceManager.EnsureInstance().TrySpend(amount, 0, 0, 0); // 골드 단독 차감 결과 반환
        } // 실제 골드 차감 종료

        public void AddGold(int amount) // 실제 플레이어 골드 환불
        { // 실제 골드 환불 시작
            PlayerResourceManager.EnsureInstance().AddResources(amount, 0, 0, 0); // 골드 단독 환불 지급
        } // 실제 골드 환불 종료
    } // 상점 지갑 연결기 종료
} // 상점 관리자 종료
