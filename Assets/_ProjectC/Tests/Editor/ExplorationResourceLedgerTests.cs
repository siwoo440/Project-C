using System; // 상점 지갑 타입 생성 사용
using System.Collections; // 플레이 모드 대기 사용
using System.Reflection; // 실제 상점 지갑 조회 사용
using NUnit.Framework; // 재화 구분 검증 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용

public sealed class ExplorationResourceLedgerTests // 탐사 재화 구분 검사
{ // 재화 구분 검사 시작
    [UnityTest] // 플레이 모드 검사 지정
    public IEnumerator SpendingAndRefund_PreserveExistingResourcesFirst() // 기존 자원 우선 소비와 환불 확인
    { // 재화 소비 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 준비
        resources.ResetResources(); // 이전 재화 초기화
        resources.AddResources(100, 20, 20, 20); // 기존 재화 설정
        resources.BeginExplorationResourceTracking(); // 새 탐사 획득분 추적 시작
        resources.AddResources(60, 8, 9, 10); // 탐사 재화 획득
        Assert.IsTrue(resources.TrySpend(80, 25, 10, 0)); // 기존 재화 우선 소비
        Assert.AreEqual(80, resources.Gold); // 소비 후 총 골드 확인
        Assert.AreEqual(60, resources.ExplorationGold); // 기존 골드 우선 사용 확인
        Assert.AreEqual(3, resources.ExplorationScrew); // 기존 나사 소진 후 탐사 나사 소비 확인
        Assert.AreEqual(9, resources.ExplorationIronPlate); // 기존 철판 우선 소비 확인
        RelicRunManager.EnsureInstance().Gold.AddGold(15); // 유물의 직접 골드 획득 재현
        Assert.AreEqual(75, resources.ExplorationGold); // 직접 골드 획득분 구분 확인
        resources.DiscardExplorationResources(); // 전멸 시 남은 획득 재화 제거
        Assert.AreEqual(20, resources.Gold); // 기존 골드 잔액 보존 확인
        Assert.AreEqual(0, resources.Screw); // 소비한 기존 나사 잔액 확인
        Assert.AreEqual(10, resources.IronPlate); // 소비 후 기존 철판 보존 확인
        Assert.AreEqual(20, resources.Wire); // 미사용 기존 전선 보존 확인
        resources.ResetResources(); // 환불 검사 재화 초기화
        resources.AddResources(20, 0, 0, 0); // 환불 검사 기존 골드 설정
        resources.BeginExplorationResourceTracking(); // 환불 검사 탐사 구분 시작
        resources.AddResources(100, 0, 0, 0); // 환불 검사 탐사 골드 획득
        Type walletType = typeof(ShopRunManager).GetNestedType("PlayerResourceShopWallet", BindingFlags.NonPublic); // 실제 상점 지갑 타입 조회
        Assert.IsNotNull(walletType); // 실제 상점 지갑 존재 확인
        IShopWallet wallet = (IShopWallet)Activator.CreateInstance(walletType, true); // 실제 상점 지갑 생성
        ShopTransactionService shop = new ShopTransactionService(wallet); // 실제 지갑 연결 상점 거래 생성
        Assert.AreEqual(ShopPurchaseResult.DeliveryFailed, shop.TryPurchase("failed-delivery", 80, () => true, () => false)); // 지급 실패 시 실제 상점 환불 확인
        Assert.AreEqual(120, resources.Gold); // 환불 후 전체 골드 확인
        Assert.AreEqual(100, resources.ExplorationGold); // 환불 후 원래 탐사 골드 구분 확인
        resources.DiscardExplorationResources(); // 환불 후 전멸 처리
        Assert.AreEqual(20, resources.Gold); // 환불한 기존 골드 보존 확인
        resources.ResetResources(); // 성공 정산 검사 초기화
        resources.AddResources(30, 0, 0, 0); // 기존 골드 설정
        resources.BeginExplorationResourceTracking(); // 성공 탐사 구분 시작
        resources.AddResources(10, 0, 0, 0); // 성공 탐사 획득 골드 설정
        resources.EndExplorationResourceTracking(); // 성공 탐사 획득 재화 확정
        resources.DiscardExplorationResources(); // 종료된 탐사 재화 재제거 시도
        Assert.AreEqual(40, resources.Gold); // 성공 시 획득 골드 유지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 재화 소비 검사 종료
} // 재화 구분 검사 종료
