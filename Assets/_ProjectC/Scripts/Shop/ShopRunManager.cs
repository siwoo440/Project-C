using System; // 상점 상태 이벤트 사용
using System.Collections.Generic; // 저장 상품 목록 사용
using UnityEngine; // 영구 상점 관리자 사용

public sealed class ShopRunManager : MonoBehaviour // 탐사 회차 상점 관리자
{ // 상점 관리자 시작
    private static ShopRunManager instance; // 현재 상점 관리자
    private ShopCatalogData catalog; // 현재 상점 카탈로그
    private RunDeckManager runDeckManager; // 탐사 회차 덱 관리자
    private ShopTransactionService transactionService; // 상점 거래 서비스
    private string activeRoomRuntimeId; // 57일차 현재 상점 방 런타임 ID
    private RelicSheetCatalog relicSheetCatalog; // 시트 유물 가격 목록

    public static ShopRunManager Instance => instance; // 현재 상점 관리자 조회
    public ShopCatalogData Catalog => catalog; // 현재 카탈로그 조회
    public string ActiveRoomRuntimeId => activeRoomRuntimeId; // 현재 상점 방 식별자 조회
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

    public void ResetSession() // 이전 상점 회차 초기화
    { // 상점 회차 초기화 시작
        catalog = null; // 이전 상품 목록 해제
        runDeckManager = null; // 이전 회차 덱 연결 해제
        transactionService = null; // 이전 판매 상태 제거
        activeRoomRuntimeId = null; // 이전 상점 방 식별자 제거
        Changed?.Invoke(); // 상점 상태 변경 알림
    } // 상점 회차 초기화 종료

    internal void RestoreSession(ShopCatalogData catalogData, DeckData deckData, string roomId, IReadOnlyList<string> purchased) // 저장 상점 상태 복원
    { // 상점 상태 복원 시작
        ResetSession(); // 이전 상점 상태 제거
        if (string.IsNullOrEmpty(roomId)) // 저장 상점 방 존재 확인
        { // 상점 방 없음 처리 시작
            return; // 상점 상태 복원 종료
        } // 상점 방 없음 처리 종료
        Prepare(catalogData, deckData); // 저장 상점 카탈로그 연결
        BeginRoomVisit(roomId); // 저장 상점 방 연결
        foreach (string offerId in purchased) // 판매 완료 상품 순회
        { // 판매 완료 복원 시작
            transactionService.RestorePurchase(offerId); // 판매 완료 상태 등록
        } // 판매 완료 복원 종료
        Changed?.Invoke(); // 복원 상점 상태 알림
    } // 상점 상태 복원 종료
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

    public void BeginRoomVisit(string roomRuntimeId) // 57일차 생성된 상점 방 최초 방문 준비
    {
        if (string.IsNullOrWhiteSpace(roomRuntimeId) ||
            activeRoomRuntimeId == roomRuntimeId) // 잘못된 ID 또는 동일 상점 재호출 확인
        {
            return; // 상점 상태 재생성 중단
        }

        activeRoomRuntimeId = roomRuntimeId; // 현재 상점 방 ID 저장
        transactionService = new ShopTransactionService(new PlayerResourceShopWallet()); // 새 상점 전용 판매 상태 생성
        Changed?.Invoke(); // 상점 상품 표시 갱신 알림

        Debug.Log(
            $"[Shop][Day57] 새 상점 방 방문 준비 - {activeRoomRuntimeId}"); // 방별 상점 초기화 로그
    }

    public ShopPurchaseResult TryPurchase(ShopOfferData offer) // 지정 상품 구매 시도
    { // 상품 구매 시작
        if (!IsReady || offer == null || !offer.IsValidData() || !catalog.ContainsOffer(offer.OfferId)) // 상점과 상품 유효성 확인
        { // 잘못된 상품 처리 시작
            return CompleteTransaction(ShopPurchaseResult.InvalidOffer, "상품 정보가 올바르지 않습니다."); // 잘못된 상품 완료 반환
        } // 잘못된 상품 처리 종료

        ShopPurchaseResult result = transactionService.TryPurchase(offer.OfferId, GetOfferPrice(offer), () => CanDeliver(offer), () => Deliver(offer)); // 표시 가격으로 원자 상품 거래 실행
        string message = BuildPurchaseMessage(result, offer.DisplayName); // 거래 결과 문구 생성
        return CompleteTransaction(result, message); // 거래 결과 알림과 반환
    } // 상품 구매 종료

    public int GetOfferPrice(ShopOfferData offer) // 현재 상점의 상품 가격 조회
    { // 상품 가격 조회 시작
        if (offer == null) // 빈 상품 확인
        { // 빈 상품 처리 시작
            return 0; // 가격 없음 반환
        } // 빈 상품 처리 종료
        if (offer.OfferType != ShopOfferType.Relic || offer.Relic == null || string.IsNullOrWhiteSpace(activeRoomRuntimeId)) // 시트 유물 가격 적용 조건 확인
        { // 기존 가격 처리 시작
            return offer.Price; // 기존 상품 가격 반환
        } // 기존 가격 처리 종료
        if (relicSheetCatalog == null) // 시트 가격 목록 미로드 확인
        { // 시트 목록 로드 시작
            relicSheetCatalog = RelicSheetCatalog.Load(); // 유물 시트 목록 불러오기
        } // 시트 목록 로드 종료
        RelicSheetEntry entry = relicSheetCatalog?.FindByName(offer.Relic.DisplayName); // 유물 이름에 맞는 가격 조회
        if (entry == null || entry.BuyMin < 0 || entry.BuyMax < entry.BuyMin || (long)entry.BuyMax - entry.BuyMin >= int.MaxValue) // 시트 가격 유효성 확인
        { // 시트 가격 없음 처리 시작
            return offer.Price; // 기존 상품 가격 반환
        } // 시트 가격 없음 처리 종료
        return RelicShopPricing.Calculate(entry, activeRoomRuntimeId, offer.OfferId); // 상점별 시트 범위 가격 반환
    } // 상품 가격 조회 종료

    public int GetRelicSellPrice(RelicData relic) // 현재 상점의 유물 판매 가격 조회
    { // 유물 판매 가격 조회 시작
        if (!IsReady || relic == null || !relic.IsValidData() || string.IsNullOrWhiteSpace(activeRoomRuntimeId) || relic.TriggerType == RelicTriggerType.OnAcquire) // 판매 지원 조건 확인
        { // 판매 불가 조건 시작
            return -1; // 판매 불가 가격 반환
        } // 판매 불가 조건 종료
        if (relicSheetCatalog == null) // 시트 가격 목록 미로드 확인
        { // 시트 목록 로드 시작
            relicSheetCatalog = RelicSheetCatalog.Load(); // 유물 시트 목록 불러오기
        } // 시트 목록 로드 종료
        RelicSheetEntry entry = relicSheetCatalog?.FindByName(relic.DisplayName); // 유물 이름에 맞는 가격 조회
        if (entry == null || entry.SellMin < 0 || entry.SellMax < entry.SellMin || (long)entry.SellMax - entry.SellMin >= int.MaxValue) // 시트 판매 범위 확인
        { // 판매 범위 없음 처리 시작
            return -1; // 판매 불가 가격 반환
        } // 판매 범위 없음 처리 종료
        return RelicShopPricing.CalculateSale(entry, activeRoomRuntimeId, relic.RelicId); // 상점별 시트 판매 가격 반환
    } // 유물 판매 가격 조회 종료

    public ShopPurchaseResult TrySellRelic(RelicData relic) // 보유 유물 판매 시도
    { // 유물 판매 시작
        int price = GetRelicSellPrice(relic); // 실제 판매 가격 조회
        if (price < 0) // 판매 불가 유물 확인
        { // 판매 불가 처리 시작
            return CompleteTransaction(ShopPurchaseResult.InvalidOffer, "판매할 수 없는 유물입니다."); // 판매 불가 결과 알림
        } // 판매 불가 처리 종료
        RelicRunManager relicManager = RelicRunManager.EnsureInstance(); // 실제 유물 보관함 조회
        bool owned = false; // 실제 원본 보유 여부 초기화
        foreach (RelicData item in relicManager.Inventory.OwnedRelics) // 현재 보유 유물 순회
        { // 소유 원본 확인 시작
            owned |= ReferenceEquals(item, relic); // 동일 원본 보유 여부 확인
        } // 소유 원본 확인 종료
        if (!owned || (long)Gold + price > int.MaxValue) // 보유 여부와 골드 범위 확인
        { // 판매 조건 미충족 시작
            return CompleteTransaction(ShopPurchaseResult.Unavailable, "판매할 수 없거나 골드 한도를 초과합니다."); // 판매 조건 실패 알림
        } // 판매 조건 미충족 종료
        if (!relicManager.TryRemove(relic.RelicId)) // 유물 제거 결과 확인
        { // 유물 제거 실패 시작
            return CompleteTransaction(ShopPurchaseResult.Unavailable, "보유한 유물을 찾을 수 없습니다."); // 제거 실패 알림
        } // 유물 제거 실패 종료
        PlayerResourceManager.EnsureInstance().AddResources(price, 0, 0, 0); // 판매 대금을 실제 탐사 골드에 지급
        return CompleteTransaction(ShopPurchaseResult.Success, $"{relic.DisplayName} 판매 완료"); // 판매 완료 알림
    } // 유물 판매 종료

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
        private int explorationGoldSpent; // 직전 거래의 탐사 골드 사용량
        public int Gold => PlayerResourceManager.EnsureInstance().Gold; // 실제 플레이어 골드 조회

        public bool TrySpend(int amount) // 실제 플레이어 골드 차감
        { // 실제 골드 차감 시작
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 조회
            int previousExplorationGold = resources.ExplorationGold; // 차감 전 탐사 골드 보관
            bool spent = resources.TrySpend(amount, 0, 0, 0); // 실제 골드 차감 시도
            explorationGoldSpent = spent ? previousExplorationGold - resources.ExplorationGold : 0; // 직전 거래의 탐사 골드 차감량 저장
            return spent; // 골드 단독 차감 결과 반환
        } // 실제 골드 차감 종료

        public void AddGold(int amount) // 실제 플레이어 골드 환불
        { // 실제 골드 환불 시작
            PlayerResourceManager.EnsureInstance().RefundShopGold(amount, explorationGoldSpent); // 원래 재화 구분을 유지한 골드 환불
            explorationGoldSpent = 0; // 직전 거래 환불 기록 초기화
        } // 실제 골드 환불 종료
    } // 상점 지갑 연결기 종료
} // 상점 관리자 종료
