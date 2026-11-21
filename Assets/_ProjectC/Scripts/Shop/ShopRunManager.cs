using System; // 상점 상태 이벤트 사용
using System.Collections.Generic; // 저장 상품 목록 사용
using UnityEngine; // 영구 상점 관리자 사용

public sealed class ShopRunManager : MonoBehaviour // 탐사 회차 상점 관리자
{ // 상점 관리자 시작
    [Serializable] // 탐사 저장용 진열 결과
    internal sealed class RelicReserve // 상점 방 하나의 첫 진열 상태
    { // 진열 기록 시작
        public string roomId; // 상점 방 식별자
        public string offerId; // 분리해 둔 유물 상품 식별자
        public bool revealed; // 열쇠로 추가 진열한 여부
        public string specialCardOfferId; // 잿빛 도장으로 그 방에서 추가한 카드 상품
        public bool specialCardSold; // 해당 방에서 특수 카드의 실제 판매 완료 여부
    } // 진열 기록 종료

    private readonly Dictionary<string, RelicReserve> relicReserves = new Dictionary<string, RelicReserve>(StringComparer.Ordinal); // 재방문과 복원에 쓰는 방별 진열 기록
    internal RelicReserve[] ExportRelicReserves() => new List<RelicReserve>(relicReserves.Values).ToArray(); // 현재 회차의 추가 진열 기록
    private static ShopRunManager instance; // 현재 상점 관리자
    private ShopCatalogData catalog; // 현재 상점 카탈로그
    private RunDeckManager runDeckManager; // 탐사 회차 덱 관리자
    private ShopTransactionService transactionService; // 상점 거래 서비스
    private bool purchasing; // 지갑과 보관함 알림 중 상품 재구매 방지
    private bool drawerDiscountUsed; // 금속 손잡이 할인을 사용한 탐사 회차 상태
    internal bool DrawerDiscountUsed => drawerDiscountUsed; // 탐사 저장용 첫 구매 할인 상태
    private const int SpecialStockPrice = 100; // 기존 100골드 상점 카드 가격을 따르는 특수 카드 가격
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
        relicReserves.Clear(); // 이전 탐사의 유물 진열 결과 제거
        drawerDiscountUsed = false; // 새 탐사의 금속 손잡이 할인 사용 여부 초기화
        catalog = null; // 이전 상품 목록 해제
        runDeckManager = null; // 이전 회차 덱 연결 해제
        transactionService = null; // 이전 판매 상태 제거
        activeRoomRuntimeId = null; // 이전 상점 방 식별자 제거
        Changed?.Invoke(); // 상점 상태 변경 알림
    } // 상점 회차 초기화 종료

    internal void RestoreSession(ShopCatalogData catalogData, DeckData deckData, string roomId, IReadOnlyList<string> purchased) // 이전 저장의 상점 상태 복원
    { // 이전 형식 복원 시작
        RestoreSessionWithRelicReserves(catalogData, deckData, roomId, purchased, null); // 진열 정보 없이 판매 기록 복원
    } // 이전 형식 복원 종료

    internal void RestoreSessionWithRelicReserves(ShopCatalogData catalogData, DeckData deckData, string roomId, IReadOnlyList<string> purchased, IReadOnlyList<RelicReserve> reserves) // 저장 상점 상태와 방별 진열 복원
    { // 기존 상점 저장 호환 시작
        RestoreSessionWithShopStock(catalogData, deckData, roomId, purchased, reserves, false); // 할인 정보가 없던 기존 복원 호출 지원
    } // 기존 상점 저장 호환 종료

    internal void RestoreSessionWithShopStock(ShopCatalogData catalogData, DeckData deckData, string roomId, IReadOnlyList<string> purchased, IReadOnlyList<RelicReserve> reserves, bool usedDrawerDiscount) // 상점 진열과 첫 구매 혜택 저장 복원
    { // 상점 상태 복원 시작
        ResetSession(); // 이전 상점 상태 제거
        drawerDiscountUsed = usedDrawerDiscount; // 남은 첫 상품 구매 혜택의 저장값 복원
        if (reserves != null) // 새 형식의 방별 기록 존재 확인
        { // 진열 기록 복원 시작
            foreach (var reserve in reserves) // 저장된 방문 방 순회
            { // 기록 적용 시작
                if (reserve != null && !string.IsNullOrWhiteSpace(reserve.roomId)) // 실제 방 식별자 확인
                { // 방별 결과 등록 시작
                    relicReserves[reserve.roomId] = reserve; // 고정된 상품과 성공 여부 복원
                } // 방별 결과 등록 종료
            } // 기록 적용 종료
        } // 진열 기록 복원 종료
        if (!Prepare(catalogData, deckData) || string.IsNullOrEmpty(roomId)) // 방문 전 저장도 다음 방문에 쓸 카탈로그 준비
        { // 상점 방 없음 처리 시작
            return; // 상점 상태 복원 종료
        } // 상점 방 없음 처리 종료
        if (!relicReserves.ContainsKey(roomId)) // 이전 저장의 상점 진열 확인
        { // 이전 저장 호환 시작
            relicReserves.Add(roomId, new RelicReserve { roomId = roomId }); // 기존 상점의 모든 상품 유지
        } // 이전 저장 호환 종료
        SetRoom(roomId); // 실제 방문 처리 없이 저장 상점 복원
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
    { // 실제 상점 방문 시작
        ExplorationSessionManager.Instance?.Cart.VisitShop(roomRuntimeId); // 표식 획득 후 첫 실제 방문 방 지정
        SetRoom(roomRuntimeId); // 해당 방의 판매 상태 준비
    } // 실제 상점 방문 종료

    private void SetRoom(string roomRuntimeId) // 신규 또는 저장 상점 방 준비
    {
        if (string.IsNullOrWhiteSpace(roomRuntimeId) ||
            activeRoomRuntimeId == roomRuntimeId) // 잘못된 ID 또는 동일 상점 재호출 확인
        {
            return; // 상점 상태 재생성 중단
        }

        if (!relicReserves.ContainsKey(roomRuntimeId) && catalog != null) // 새 상점의 첫 유물 진열 확인
        { // 상점 유물 진열 확정 시작
            relicReserves.Add(roomRuntimeId, CreateRelicReserve(roomRuntimeId)); // 방마다 한 번만 결정한 결과 보관
        } // 상점 유물 진열 확정 종료
        activeRoomRuntimeId = roomRuntimeId; // 현재 상점 방 ID 저장
        transactionService = new ShopTransactionService(new PlayerResourceShopWallet()); // 새 상점 전용 판매 상태 생성
        if (relicReserves.TryGetValue(roomRuntimeId, out var stock) && stock.specialCardSold && !string.IsNullOrEmpty(stock.specialCardOfferId)) // 방별 특수 카드 판매 저장 상태 확인
        { // 방별 특수 상품 품절 복원 시작
            transactionService.RestorePurchase(stock.specialCardOfferId); // 실제로 판매한 특수 카드 품절 유지
        } // 방별 특수 상품 품절 복원 종료
        Changed?.Invoke(); // 상점 상품 표시 갱신 알림

        Debug.Log(
            $"[Shop][Day57] 새 상점 방 방문 준비 - {activeRoomRuntimeId}"); // 방별 상점 초기화 로그
    }

    private RelicReserve CreateRelicReserve(string roomId) // 상점의 추가 진열 후보와 열쇠 판정
    { // 첫 진열 계산 시작
        var inventory = RelicRunManager.Instance?.Inventory; // 입장 시 유물 보유 상태
        var candidates = new List<ShopOfferData>(); // 추가 진열이 가능한 미보유 유물
        foreach (var offer in catalog.Offers) // 실제 카탈로그 진열 후보 순회
        { // 후보 확인 시작
            if (offer.CanBeReservedRelic && inventory?.ContainsRelic(offer.Relic.RelicId) != true) // 판매 후보이면서 미보유 유물 확인
            { // 진열 후보 추가 시작
                candidates.Add(offer); // 판매할 수 있는 실제 상품 등록
            } // 진열 후보 추가 종료
        } // 후보 확인 종료
        var reserve = new RelicReserve { roomId = roomId, specialCardOfferId = SelectSpecialCardOffer(roomId, inventory) }; // 첫 방문 즉시 일반 유물과 특수 카드 진열 확정
        if (candidates.Count == 0) // 추가 진열 후보 존재 확인
        { // 후보 없음 처리 시작
            return reserve; // 평소 재고 유지
        } // 후보 없음 처리 종료
        int index = RelicShopPricing.CalculateRange(0, candidates.Count - 1, roomId, "EXTRA_RELIC_OFFER"); // 같은 방에서 재현되는 유물 선택
        reserve.offerId = candidates[index].OfferId; // 추가 진열에 사용할 유물 한 종 확정
        var key = inventory?.FindEffect(RelicEffectType.ExtraShopRelic); // 최초 입장 시 보유한 금고 열쇠
        reserve.revealed = key != null && RelicShopPricing.CalculateRange(0, 99, roomId, "EXTRA_RELIC_CHANCE") < key.TriggerChancePercent; // 방마다 한 번만 열쇠 확률 판정
        return reserve; // 저장 가능한 진열 결과 반환
    } // 첫 진열 계산 종료

    private string SelectSpecialCardOffer(string roomId, RelicInventoryRuntime inventory) // 잿빛 도장으로 첫 상점 진열 특수 카드 선택
    { // 신규 카드 보상 진열 계산 시작
        RelicData seal = inventory?.FindEffect(RelicEffectType.ExtraShopSpecialCard); // 최초 방문 당시 잿빛 도장 확인
        if (seal == null || RelicShopPricing.CalculateRange(0, 99, roomId, "EXTRA_SPECIAL_CARD_CHANCE") >= seal.TriggerChancePercent) // 동일 방의 고정 추가 진열 확률 비교
        { // 추가 카드가 없는 판정 시작
            return null; // 기존 상점 상품만 판매
        } // 추가 카드가 없는 판정 종료
        var sheet = CardSheetCatalog.Load(); // 원본 카드 종류와 희귀도 색인 조회
        var candidates = new List<ShopOfferData>(); // 실제 구현되고 소유자를 정하는 특수 카드 후보
        var seen = new HashSet<string>(StringComparer.Ordinal); // 중복된 카드 판매 후보 제외
        foreach (ShopOfferData offer in catalog.Offers) // 지금 구현된 보상 원본 확인
        { // 특수 카드 후보 확인 시작
            if (IsSpecialCandidate(offer, sheet) && seen.Add(offer.Card.CardId)) // 원본 희귀도가 특수인 실제 구현 카드 확인
            { // 출시된 특수 카드 추가 시작
                candidates.Add(offer); // 소유자를 고를 수 있는 특수 카드 후보 보관
            } // 출시된 특수 카드 추가 종료
        } // 특수 카드 후보 확인 종료
        return candidates.Count == 0 ? null : candidates[RelicShopPricing.CalculateRange(0, candidates.Count - 1, roomId, "EXTRA_SPECIAL_CARD_OFFER")].OfferId; // 같은 방을 다시 열어도 하나의 동일 원본 선택
    } // 신규 카드 보상 진열 계산 종료

    internal static bool IsSpecialCandidate(ShopOfferData offer, CardSheetCatalog sheet) // 구매 또는 저장 복원이 가능한 특수 카드 원본 확인
    { // 특수 카드 원본 조건 확인 시작
        return offer?.RewardOnly == true && offer.RequiresOwnerChoice && offer.Card != null && sheet?.FindByName(offer.Card.DisplayName)?.Rarity == "특수"; // 출시된 특수 보상과 직접 소유자 선택 확인
    } // 특수 카드 원본 조건 확인 종료

    private bool IsSpecialStock(ShopOfferData offer) // 현재 방에서만 판매하는 추가 특수 원본 확인
    { // 특수 재고 조회 시작
        return offer?.RewardOnly == true && activeRoomRuntimeId != null && relicReserves.TryGetValue(activeRoomRuntimeId, out var stock) && string.Equals(stock.specialCardOfferId, offer.OfferId, StringComparison.Ordinal); // 실제 상점 첫 방문에서 확정된 판매 식별자 일치 확인
    } // 특수 재고 조회 종료

    private bool CanSellOffer(ShopOfferData offer) // 일반 상품과 선택된 한 특수 카드만 판매 확인
    { // 판매 가능한 원본 확인 시작
        return offer != null && (!offer.RewardOnly || IsSpecialStock(offer)); // 일반 재고 또는 잿빛 도장 특수 재고 확인
    } // 판매 가능한 원본 확인 종료

    private bool IsHiddenReserve(ShopOfferData offer) // 첫 진열에서 보류된 유물 확인
    { // 추가 진열 여부 확인 시작
        return activeRoomRuntimeId != null && relicReserves.TryGetValue(activeRoomRuntimeId, out var reserve) && !reserve.revealed && reserve.offerId == offer.OfferId; // 미공개 상태의 등록 상품 확인
    } // 추가 진열 여부 확인 종료

    public ShopPurchaseResult TryPurchase(ShopOfferData offer) // 지정 상품 구매 시도
    { // 상품 구매 시작
        return TryPurchase(offer, null); // 기존 고정 소유자 상품 구매 연결
    } // 기존 상품 구매 종료

    public ShopPurchaseResult TryPurchase(ShopOfferData offer, CharacterData chosenOwner) // 소유자를 지정한 상품 구매 시도
    { // 지정 상품 구매 시작
        if (!IsReady || !CanSellOffer(offer) || !offer.IsValidData() || !catalog.ContainsOffer(offer.OfferId)) // 상점 판매와 상품 유효성 확인
        { // 잘못된 상품 처리 시작
            return CompleteTransaction(ShopPurchaseResult.InvalidOffer, "상품 정보가 올바르지 않습니다."); // 잘못된 상품 완료 반환
        } // 잘못된 상품 처리 종료
        if (purchasing) // 같은 거래의 알림 중 재진입 확인
        { // 중복 거래 방지 시작
            return ShopPurchaseResult.Unavailable; // 진행 중인 구매 완료 대기
        } // 중복 거래 방지 종료
        if (offer.OfferType == ShopOfferType.Relic) // 같은 유물의 별도 상품 번호 확인
        { // 유물 재고 확인 시작
            ShopPurchaseResult stock = CheckRelicStock(offer); // 방의 실제 판매 기록과 보유 여부 조회
            if (stock != ShopPurchaseResult.Success) // 판매했거나 표시하지 않는 유물 확인
            { // 유물 구매 차단 시작
                return CompleteTransaction(stock, BuildPurchaseMessage(stock, offer.DisplayName)); // 차감 없이 재구매 차단
            } // 유물 구매 차단 종료
        } // 유물 재고 확인 종료
        CharacterData owner = offer.RequiresOwnerChoice ? chosenOwner : offer.CardOwner; // 공용 또는 고정 소유자 결정
        if (offer.RequiresOwnerChoice && (PartyLoadoutRuntime.Active == null || PartyLoadoutRuntime.Active.Party == null || !PartyLoadoutRuntime.Active.Party.ContainsCharacter(owner))) // 공용 카드 소유 파티원 확인
        { // 소유자 선택 오류 시작
            return CompleteTransaction(ShopPurchaseResult.Unavailable, "현재 파티 캐릭터를 선택하세요."); // 소유자 선택 필요 알림
        } // 소유자 선택 오류 종료
        ShopPurchaseResult result; // 실제 거래 결과 보관
        purchasing = true; // 상품 지급과 할인 소모까지 거래 잠금
        try // 외부 보관함 알림에도 거래 잠금 정리
        { // 원자 구매 시작
            int markedPrice = GetOfferPriceBeforeDrawer(offer); // 기존 영구 할인과 상점 표식을 적용한 금액
            int drawerPercent = GetDrawerDiscountPercent(markedPrice); // 상품을 받기 직전 손잡이 보유와 회차 할인 여부 보관
            int charged = ApplyDrawerDiscount(markedPrice, drawerPercent); // 실제 골드 결제 금액 확정
            result = transactionService.TryPurchase(offer.OfferId, charged, () => CanDeliver(offer, owner), () => Deliver(offer, owner)); // 현재 표시 가격으로 거래 실행
            if (result == ShopPurchaseResult.Success) // 실제 첫 상품 구매 성공 확인
            { // 할인 소모 시작
                drawerDiscountUsed |= drawerPercent > 0 && charged > 0; // 손잡이를 적용하고 지불한 첫 상품에서 회차 혜택 소모
                if (IsSpecialStock(offer)) // 잿빛 도장이 진열한 특수 카드 실제 결제 확인
                { // 방별 특수 상품 구매 완료 기록 시작
                    relicReserves[activeRoomRuntimeId].specialCardSold = true; // 방과 저장에서 품절 상태 유지
                } // 방별 특수 상품 구매 완료 기록 종료
                ExplorationSessionManager.Instance?.Cart.FinishPurchase(activeRoomRuntimeId); // 자동 저장과 표시 갱신 전에 표식 할인 소모
            } // 할인 소모 종료
        } // 원자 구매 종료
        finally // 구매 잠금 해제
        { // 잠금 정리 시작
            purchasing = false; // 다음 정상 거래 허용
        } // 잠금 정리 종료
        string message = BuildPurchaseMessage(result, offer.DisplayName); // 거래 결과 문구 생성
        return CompleteTransaction(result, message); // 거래 결과 알림과 반환
    } // 상품 구매 종료

    internal void CollectStock(out List<ShopOfferData> available, out List<ShopOfferData> sold) // 현재 방의 구매 가능 목록과 품절 목록 생성
    { // 진열 계산 시작
        available = new List<ShopOfferData>(); // 판매 가능한 실제 상품
        sold = new List<ShopOfferData>(); // 이 방에서 팔린 실제 상품
        var listedRelics = new HashSet<string>(StringComparer.Ordinal); // 품절 또는 진열된 유물 식별자
        foreach (ShopOfferData offer in catalog.Offers) // 기존 저장의 실제 판매 번호 우선 확인
        { // 판매 기록 확인 시작
            if (IsPurchased(offer) && CanSellOffer(offer) && offer.CanOfferToDeck(runDeckManager) && (offer.OfferType != ShopOfferType.Relic || listedRelics.Add(offer.Relic.RelicId))) // 실제 판매한 원본에서 유물당 한 칸 선택
            { // 품절 상품 보관 시작
                sold.Add(offer); // 카탈로그 순서대로 판매 완료 목록에 추가
            } // 품절 상품 보관 종료
        } // 판매 기록 확인 종료
        var inventory = RelicRunManager.Instance?.Inventory; // 현재 보유 유물 조회
        foreach (ShopOfferData offer in catalog.Offers) // 원래 순서대로 구매 가능 상품 확인
        { // 구매 목록 확인 시작
            if (!CanSellOffer(offer) || !offer.CanOfferToDeck(runDeckManager) || IsPurchased(offer) || IsHiddenReserve(offer)) // 보상 전용과 미출전 및 미공개 또는 품절 상품 제외
            { // 구매 목록 제외 시작
                continue; // 다음 원본 확인
            } // 구매 목록 제외 종료
            if (offer.OfferType == ShopOfferType.Relic && (inventory?.ContainsRelic(offer.Relic.RelicId) == true || !listedRelics.Add(offer.Relic.RelicId))) // 소지한 유물과 이미 표시한 유물 제외
            { // 유물 중복 제외 시작
                continue; // 유물 칸 하나만 유지
            } // 유물 중복 제외 종료
            available.Add(offer); // 기존 가격의 실제 상품을 원래 순서로 추가
        } // 구매 목록 확인 종료
    } // 진열 계산 종료

    private ShopPurchaseResult CheckRelicStock(ShopOfferData offer) // 표시된 유물 원본의 실제 거래 가능 여부
    { // 유물 거래 확인 시작
        ShopOfferData first = null; // 해당 유물의 첫 판매 원본
        foreach (ShopOfferData entry in catalog.Offers) // 현재 방의 같은 유물 원본 확인
        { // 유물 원본 확인 시작
            if (entry.OfferType == ShopOfferType.Relic && entry.Relic.RelicId == offer.Relic.RelicId) // 이름과 무관한 유물 식별자 비교
            { // 동일 유물 확인 시작
                first ??= entry; // 기본 진열 원본 보관
                if (IsPurchased(entry)) // 이전 저장의 다른 상품 번호까지 판매 여부 확인
                { // 품절 반환 시작
                    return ShopPurchaseResult.AlreadyPurchased; // 판매한 뒤 되판 유물의 중복 결제 방지
                } // 품절 반환 종료
            } // 동일 유물 확인 종료
        } // 유물 원본 확인 종료
        return first == offer && !IsHiddenReserve(offer) && RelicRunManager.Instance?.Inventory.ContainsRelic(offer.Relic.RelicId) != true ? ShopPurchaseResult.Success : ShopPurchaseResult.Unavailable; // 실제 진열 원본과 미보유 조건 확인
    } // 유물 거래 확인 종료

    internal ShopPurchaseResult PreviewPurchase(ShopOfferData offer) // 결제 전 표시할 현재 상품 상태 확인
    { // 구매 상태 조회 시작
        if (!IsReady || !CanSellOffer(offer) || !offer.CanOfferToDeck(runDeckManager)) // 준비 중인 상점과 지급 가능한 상품 확인
        { // 상품 오류 처리 시작
            return ShopPurchaseResult.InvalidOffer; // 거래 대상 없음
        } // 상품 오류 처리 종료
        if (IsPurchased(offer)) // 현재 방 판매 완료 여부 확인
        { // 품절 처리 시작
            return ShopPurchaseResult.AlreadyPurchased; // 같은 상품 차감 차단
        } // 품절 처리 종료
        if (offer.OfferType == ShopOfferType.Relic) // 유물 고유 재고 확인
        { // 유물 구매 조건 시작
            var stock = CheckRelicStock(offer); // 보유와 다른 상품 번호의 품절 상태 조회
            if (stock != ShopPurchaseResult.Success) // 구매할 수 없는 유물 확인
            { // 숨은 유물 처리 시작
                return stock; // 차감 없는 현재 상태 반환
            } // 숨은 유물 처리 종료
        } // 유물 구매 조건 종료
        if (offer.OfferType == ShopOfferType.Potion && !CanDeliver(offer, null)) // 포션을 담을 빈 슬롯 확인
        { // 보관함 포화 처리 시작
            return ShopPurchaseResult.Unavailable; // 소모품 지급 불가 반환
        } // 보관함 포화 처리 종료
        return Gold < GetOfferPrice(offer) ? ShopPurchaseResult.InsufficientGold : ShopPurchaseResult.Success; // 현재 할인 가격과 실제 지갑 비교
    } // 구매 상태 조회 종료

    public int GetOfferPrice(ShopOfferData offer) // 현재 상점의 상품 가격 조회
    { // 표식 포함 가격 조회 시작
        int price = GetOfferPriceBeforeDrawer(offer); // 영구 유물과 표식 할인 가격 조회
        return ApplyDrawerDiscount(price, GetDrawerDiscountPercent(price)); // 실제 결제와 같은 첫 상품 할인 적용
    } // 상품 가격 조회 종료

    private static int ApplyDrawerDiscount(int price, int percent) // 첫 상품 구매 화면과 실제 결제의 공통 할인 계산
    { // 첫 상품 구매 금액 계산 시작
        return price - (int)((long)price * percent / 100L); // 기존 표식과 같은 할인액 내림 규칙
    } // 첫 상품 구매 금액 계산 종료

    private int GetDrawerDiscountPercent(int price) // 사용하지 않은 회차 한 번의 손잡이 할인 조회
    { // 보유 중인 손잡이 할인 계산 시작
        if (drawerDiscountUsed || string.IsNullOrEmpty(activeRoomRuntimeId) || price <= 0) // 이미 사용했거나 정상 방 상품 가격이 아닌 경우 확인
        { // 추가 할인가 없는 경우 처리 시작
            return 0; // 기존 가격 보존
        } // 추가 할인가 없는 경우 처리 종료
        int percent = RelicRunManager.Instance?.Inventory.FindEffect(RelicEffectType.FirstShopPurchaseDiscount)?.EffectValue ?? 0; // 회차에 보유한 금속 손잡이의 할인율
        return Mathf.Clamp(percent, 0, 100); // 보유 유물의 안전한 골드 할인율 반환
    } // 보유 중인 손잡이 할인 계산 종료

    private int GetOfferPriceBeforeDrawer(ShopOfferData offer) // 기존 시트 가격과 영구 그리고 상점 표식 할인 반영
    { // 첫 결제 할인 전 상품 가격 조회 시작
        int price = GetPriceBeforeMark(offer); // 시트 가격과 영구 유물 할인 적용
        return ExplorationSessionManager.Instance?.Cart.OfferPrice(activeRoomRuntimeId, price) ?? price; // 다음 상점의 첫 상품만 추가 할인
    } // 표식 포함 가격 조회 종료

    private int GetPriceBeforeMark(ShopOfferData offer) // 기존 상품 가격과 유물 할인 계산
    { // 상품 가격 조회 시작
        if (offer == null) // 빈 상품 확인
        { // 빈 상품 처리 시작
            return 0; // 가격 없음 반환
        } // 빈 상품 처리 종료
        if (IsSpecialStock(offer)) // 무료 보물로도 쓰이는 특수 카드의 실제 상점 가격 확인
        { // 추가 카드 상점 금액 처리 시작
            return ApplyShopDiscount(SpecialStockPrice); // 소유자 선택을 포함한 특수 상품의 실제 골드 판매 가격
        } // 추가 카드 상점 금액 처리 종료
        if (offer.OfferType != ShopOfferType.Relic || offer.Relic == null || string.IsNullOrWhiteSpace(activeRoomRuntimeId)) // 시트 유물 가격 적용 조건 확인
        { // 기존 가격 처리 시작
            return ApplyShopDiscount(offer.Price); // 기존 상품의 할인 적용 가격 반환
        } // 기존 가격 처리 종료
        if (relicSheetCatalog == null) // 시트 가격 목록 미로드 확인
        { // 시트 목록 로드 시작
            relicSheetCatalog = RelicSheetCatalog.Load(); // 유물 시트 목록 불러오기
        } // 시트 목록 로드 종료
        RelicSheetEntry entry = relicSheetCatalog?.FindByName(offer.Relic.DisplayName); // 유물 이름에 맞는 가격 조회
        if (entry == null || entry.BuyMin < 0 || entry.BuyMax < entry.BuyMin || (long)entry.BuyMax - entry.BuyMin >= int.MaxValue) // 시트 가격 유효성 확인
        { // 시트 가격 없음 처리 시작
            return ApplyShopDiscount(offer.Price); // 대체 상품의 할인 적용 가격 반환
        } // 시트 가격 없음 처리 종료
        return ApplyShopDiscount(RelicShopPricing.Calculate(entry, activeRoomRuntimeId, offer.OfferId)); // 시트 범위 가격에 보유 할인 적용
    } // 상품 가격 조회 종료

    public int GetCardRemovalPrice() // 현재 카드 제거 비용 조회
    { // 카드 제거 비용 조회 시작
        var inventory = RelicRunManager.Instance?.Inventory; // 현재 회차의 제거 유물 보관함 조회
        if (catalog == null || inventory?.FindEffect(RelicEffectType.FreeCardRemoval) != null) // 미준비 상점 또는 무료 제거 교환권 확인
        { // 비용 없는 서비스 시작
            return 0; // 무료 제거 가격 반환
        } // 비용 없는 서비스 종료
        int price = ApplyShopDiscount(catalog.CardRemovalPrice); // 기존 공통 할인 적용 카드 제거 가격
        int chalkPercent = inventory?.FindEffect(RelicEffectType.CardRemovalDiscount)?.EffectValue ?? 0; // 현재 보유한 세공된 초크의 제거 할인율
        return price - (int)((long)price * Mathf.Clamp(chalkPercent, 0, 100) / 100L); // 공통 할인 뒤 초크 할인액을 내림해 차감
    } // 카드 제거 비용 조회 종료

    private static int ApplyShopDiscount(int price) // 보유 유물의 상점 할인 계산
    { // 상점 할인 계산 시작
        RelicInventoryRuntime inventory = RelicRunManager.Instance != null ? RelicRunManager.Instance.Inventory : null; // 현재 유물 보관함 조회
        if (inventory == null) // 유물 보관함 존재 확인
        { // 보관함 없음 처리 시작
            return price; // 원래 가격 반환
        } // 보관함 없음 처리 종료
        foreach (RelicData relic in inventory.OwnedRelics) // 보유 유물 순회
        { // 할인 유물 확인 시작
            if (relic != null && relic.EffectType == RelicEffectType.ShopDiscount) // 상점 할인 효과 확인
            { // 상점 할인 적용 시작
                int percent = Mathf.Clamp(relic.EffectValue, 0, 100); // 유효 할인율 설정
                return price - (int)((long)price * percent / 100); // 할인액 소수점 버림 후 가격 반환
            } // 상점 할인 적용 종료
        } // 할인 유물 확인 종료
        return price; // 할인 유물 미보유 가격 반환
    } // 상점 할인 계산 종료

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
        if (purchasing) // 제거 거래의 지갑 알림 중 교환권 판매 확인
        { // 거래 중 판매 차단 시작
            return ShopPurchaseResult.Unavailable; // 결제 중인 유물 상태 보존
        } // 거래 중 판매 차단 종료
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
        if (relic.EffectType == RelicEffectType.ExtraPotionSlot && !ConsumableRunManager.EnsureInstance().Inventory.CanRemovePotionRack) // 추가칸에서 옮길 빈 기본칸 확인
        { // 보관대 판매 중단 시작
            return CompleteTransaction(ShopPurchaseResult.Unavailable, "기본 소모품 칸을 하나 비워야 포션 보관대를 팔 수 있습니다."); // 포션 보존 조건 안내
        } // 보관대 판매 중단 종료
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

        if (purchasing) // 이전 구매나 제거의 변경 알림 중 재진입 확인
        { // 중복 제거 차단 시작
            return ShopPurchaseResult.Unavailable; // 같은 교환권으로 여러 장 제거 방지
        } // 중복 제거 차단 종료
        string cardName = cardIndex >= 0 && cardIndex < runDeckManager.CardCount ? runDeckManager.Cards[cardIndex].Card.DisplayName : "카드"; // 제거 대상 카드 이름 결정
        var inventory = RelicRunManager.Instance?.Inventory; // 같은 거래 안에서 사용할 보관함 보관
        var voucher = inventory?.FindEffect(RelicEffectType.FreeCardRemoval); // 실제로 사용할 교환권 원본 고정
        ShopPurchaseResult result; // 제거와 교환권 사용 결과 보관
        purchasing = true; // 지갑과 덱 알림 중 중복 제거 방지
        try // 거래 실패나 종료 뒤 입력 복구 보장
        { // 제거 결제 시작
            result = transactionService.TryCardRemoval(voucher != null ? 0 : GetCardRemovalPrice(), () => runDeckManager.CanRemoveAt(cardIndex), () => runDeckManager.TryRemoveAt(cardIndex)); // 실제 성공 여부를 반환하는 정상 제거 거래
            if (result == ShopPurchaseResult.Success && voucher != null) // 실제 카드 제거 성공과 결제 교환권 확인
            { // 무료 제거 교환권 소비 시작
                inventory.TryRemove(voucher.RelicId); // 성공한 거래에서만 교환권 한 개 소모
            } // 무료 제거 교환권 소비 종료
        } // 제거 결제 종료
        finally // 거래 입력 복구
        { // 거래 잠금 해제 시작
            purchasing = false; // 다음 정상 구매와 제거 및 판매 허용
        } // 거래 잠금 해제 종료
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

    private bool CanDeliver(ShopOfferData offer, CharacterData owner) // 상품 지급 가능 여부 검사
    { // 상품 지급 검사 시작
        switch (offer.OfferType) // 상품 종류 분기
        { // 상품 지급 분기 시작
            case ShopOfferType.Card: // 카드 상품 지급 검사
                return runDeckManager.ContainsOwner(owner); // 출전 소유자 포함 여부 반환
            case ShopOfferType.Relic: // 유물 상품 지급 검사
                return !RelicRunManager.EnsureInstance().Inventory.ContainsRelic(offer.Relic.RelicId); // 유물 중복 없음 반환
            case ShopOfferType.Potion: // 포션 상품 지급 검사
                return ConsumableRunManager.EnsureInstance().Inventory.CanAcquire(offer.Potion); // 포션 추가칸을 포함한 실제 구매 공간 확인
            default: // 알 수 없는 상품 검사
                return false; // 지급 불가 반환
        } // 상품 지급 분기 종료
    } // 상품 지급 검사 종료

    private bool Deliver(ShopOfferData offer, CharacterData owner) // 상품 실제 지급
    { // 상품 지급 시작
        switch (offer.OfferType) // 상품 종류 분기
        { // 상품 지급 분기 시작
            case ShopOfferType.Card: // 카드 상품 지급
                return runDeckManager.TryAddCard(offer.Card, owner); // 선택 소유자의 회차 덱 카드 추가 결과 반환
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
