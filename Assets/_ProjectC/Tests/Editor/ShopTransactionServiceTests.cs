#if UNITY_EDITOR // 에디터 테스트 한정
using NUnit.Framework; // NUnit 검증 기능 사용
using UnityEngine; // 테스트 오브젝트 사용
using UnityEngine.UI; // 상점 그리드 검증 사용

public sealed class ShopTransactionServiceTests // 상점 거래 규칙 테스트
{
    [Test] // 단위 테스트 지정
    public void ProductGridUsesFourFixedColumns() // 상품 4열 고정 배치 검증
    {
        GameObject gridObject = new GameObject("ShopGridTest", typeof(RectTransform), typeof(GridLayoutGroup)); // 테스트 그리드 생성
        GridLayoutGroup gridLayout = gridObject.GetComponent<GridLayoutGroup>(); // 테스트 그리드 조회

        ShopPrototypeView.ConfigureProductGrid(gridLayout); // 상점 상품 그리드 설정 실행

        Assert.That(gridLayout.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount)); // 고정 열 방식 검증
        Assert.That(gridLayout.constraintCount, Is.EqualTo(4)); // 한 줄 네 칸 검증
        Assert.That(gridLayout.cellSize, Is.EqualTo(new Vector2(286f, 400f))); // 상품 카드 크기 검증
        Assert.That(gridLayout.spacing, Is.EqualTo(new Vector2(20f, 24f))); // 카드 사이 간격 검증
        Assert.That(gridLayout.padding.left, Is.EqualTo(20)); // 왼쪽 내부 여백 검증
        Assert.That(gridLayout.padding.right, Is.EqualTo(20)); // 오른쪽 내부 여백 검증

        Object.DestroyImmediate(gridObject); // 테스트 오브젝트 제거
    }

    [Test] // 단위 테스트 지정
    public void ProductGridFitsFourCardsInsideReferenceViewport() // 네 카드 겹침 없는 기준 폭 검증
    {
        GameObject gridObject = new GameObject("ShopGridWidthTest", typeof(RectTransform), typeof(GridLayoutGroup)); // 테스트 그리드 생성
        GridLayoutGroup gridLayout = gridObject.GetComponent<GridLayoutGroup>(); // 테스트 그리드 조회

        ShopPrototypeView.ConfigureProductGrid(gridLayout); // 상점 상품 그리드 설정 실행

        float cardsWidth = gridLayout.cellSize.x * gridLayout.constraintCount; // 네 카드 전체 폭 계산
        float spacingWidth = gridLayout.spacing.x * (gridLayout.constraintCount - 1); // 카드 사이 전체 간격 계산
        float paddingWidth = gridLayout.padding.left + gridLayout.padding.right; // 좌우 내부 여백 계산
        float requiredWidth = cardsWidth + spacingWidth + paddingWidth; // 네 카드 필요 전체 폭 계산

        Assert.That(requiredWidth, Is.LessThanOrEqualTo(1280f)); // 기준 스크롤 영역 내부 배치 검증
        Assert.That(gridLayout.spacing.x, Is.GreaterThan(0f)); // 카드 가로 겹침 방지 간격 검증
        Assert.That(gridLayout.spacing.y, Is.GreaterThan(0f)); // 카드 세로 겹침 방지 간격 검증

        Object.DestroyImmediate(gridObject); // 테스트 오브젝트 제거
    }

    [Test] // 단위 테스트 지정
    public void PurchaseSpendsGoldAndMarksOfferSold() // 정상 구매 골드 차감과 판매 완료 검증
    {
        FakeShopWallet wallet = new FakeShopWallet(300); // 초기 골드 지갑 준비
        ShopTransactionService service = new ShopTransactionService(wallet); // 거래 서비스 준비

        ShopPurchaseResult result = service.TryPurchase("CARD_A", 120, () => true, () => true); // 정상 상품 구매 실행

        Assert.That(result, Is.EqualTo(ShopPurchaseResult.Success)); // 성공 결과 검증
        Assert.That(wallet.Gold, Is.EqualTo(180)); // 골드 차감 검증
        Assert.That(service.IsPurchased("CARD_A"), Is.True); // 판매 완료 상태 검증
    }

    [Test] // 단위 테스트 지정
    public void InsufficientGoldKeepsGoldAndOfferAvailable() // 골드 부족 상태 보존 검증
    {
        FakeShopWallet wallet = new FakeShopWallet(50); // 부족 골드 지갑 준비
        ShopTransactionService service = new ShopTransactionService(wallet); // 거래 서비스 준비

        ShopPurchaseResult result = service.TryPurchase("RELIC_A", 200, () => true, () => true); // 부족 골드 구매 실행

        Assert.That(result, Is.EqualTo(ShopPurchaseResult.InsufficientGold)); // 골드 부족 결과 검증
        Assert.That(wallet.Gold, Is.EqualTo(50)); // 골드 유지 검증
        Assert.That(service.IsPurchased("RELIC_A"), Is.False); // 상품 유지 검증
    }

    [Test] // 단위 테스트 지정
    public void UnavailableInventoryDoesNotSpendGold() // 인벤토리 공간 부족 선검사 검증
    {
        FakeShopWallet wallet = new FakeShopWallet(300); // 충분한 골드 지갑 준비
        ShopTransactionService service = new ShopTransactionService(wallet); // 거래 서비스 준비

        ShopPurchaseResult result = service.TryPurchase("POTION_A", 80, () => false, () => true); // 공간 없는 상품 구매 실행

        Assert.That(result, Is.EqualTo(ShopPurchaseResult.Unavailable)); // 구매 불가 결과 검증
        Assert.That(wallet.Gold, Is.EqualTo(300)); // 골드 미차감 검증
        Assert.That(service.IsPurchased("POTION_A"), Is.False); // 상품 유지 검증
    }

    [Test] // 단위 테스트 지정
    public void DeliveryFailureRefundsGoldAndKeepsOfferAvailable() // 지급 실패 환불 검증
    {
        FakeShopWallet wallet = new FakeShopWallet(300); // 충분한 골드 지갑 준비
        ShopTransactionService service = new ShopTransactionService(wallet); // 거래 서비스 준비

        ShopPurchaseResult result = service.TryPurchase("CARD_B", 100, () => true, () => false); // 지급 실패 구매 실행

        Assert.That(result, Is.EqualTo(ShopPurchaseResult.DeliveryFailed)); // 지급 실패 결과 검증
        Assert.That(wallet.Gold, Is.EqualTo(300)); // 전액 환불 검증
        Assert.That(service.IsPurchased("CARD_B"), Is.False); // 상품 유지 검증
    }

    [Test] // 단위 테스트 지정
    public void CardRemovalSpendsGoldOnlyWhenRemovalSucceeds() // 카드 제거 거래 원자성 검증
    {
        FakeShopWallet wallet = new FakeShopWallet(250); // 제거 비용 지갑 준비
        ShopTransactionService service = new ShopTransactionService(wallet); // 거래 서비스 준비
        bool removed = false; // 카드 제거 상태 준비

        ShopPurchaseResult result = service.TryCardRemoval(150, () => true, () => removed = true); // 카드 제거 거래 실행

        Assert.That(result, Is.EqualTo(ShopPurchaseResult.Success)); // 제거 성공 결과 검증
        Assert.That(removed, Is.True); // 실제 카드 제거 검증
        Assert.That(wallet.Gold, Is.EqualTo(100)); // 제거 비용 차감 검증
    }

    private sealed class FakeShopWallet : IShopWallet // 테스트용 골드 지갑
    {
        public int Gold // 현재 테스트 골드
        {
            get; // 골드 값 조회
            private set; // 골드 값 내부 변경
        }

        public FakeShopWallet(int initialGold) // 테스트 지갑 생성
        {
            Gold = initialGold; // 초기 골드 저장
        }

        public bool TrySpend(int amount) // 테스트 골드 차감
        {
            if (amount < 0 || Gold < amount) // 잘못된 금액 또는 잔액 부족 확인
            {
                return false; // 차감 실패 반환
            }

            Gold -= amount; // 테스트 골드 차감
            return true; // 차감 성공 반환
        }

        public void AddGold(int amount) // 테스트 골드 지급
        {
            if (amount > 0) // 양수 지급 확인
            {
                Gold += amount; // 테스트 골드 증가
            }
        }
    }
}
#endif // 에디터 테스트 종료
