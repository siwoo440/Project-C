using UnityEngine; // 탐사 상점 부트스트랩 사용

[DefaultExecutionOrder(-300)] // 탐사 HUD보다 먼저 상점 데이터 준비
public sealed class ShopPrototypeBootstrap : MonoBehaviour // 54일차 상점 자동 연결기
{ // 상점 부트스트랩 시작
    private const string CatalogResourcePath = "Shop/ShopCatalog_Test"; // Prototype 카탈로그 리소스 경로

    private void Start() // 탐사 상점 시작 처리
    { // 상점 시작 처리 시작
        ExplorationPartyLoadoutProvider loadoutProvider = FindFirstObjectByType<ExplorationPartyLoadoutProvider>(); // 탐사 출전 데이터 제공자 조회
        if (loadoutProvider == null || loadoutProvider.BattleLoadout == null) // 출전 데이터 연결 확인
        { // 출전 데이터 누락 처리 시작
            Debug.LogError("[Shop][Day54] ExplorationPartyLoadoutProvider 또는 BattleLoadout이 없습니다.", this); // 출전 데이터 누락 로그
            return; // 상점 준비 중단
        } // 출전 데이터 누락 처리 종료

        ShopCatalogData catalog = Resources.Load<ShopCatalogData>(CatalogResourcePath); // Prototype 상점 카탈로그 로드
        ShopRunManager manager = ShopRunManager.EnsureInstance(); // 탐사 회차 상점 관리자 준비
        if (!manager.Prepare(catalog, loadoutProvider.BattleLoadout.Deck)) // 카탈로그와 덱 준비 시도
        { // 상점 준비 실패 처리 시작
            Debug.LogError("[Shop][Day54] 상점 카탈로그 또는 덱 데이터가 올바르지 않습니다.", this); // 상점 준비 실패 로그
            return; // 상점 화면 생성 중단
        } // 상점 준비 실패 처리 종료

        ShopPrototypeView.Create(manager); // 코드 기반 상점 Canvas 생성
        Debug.Log("[Shop][Day54] Prototype v0.1 상점 거래 화면을 준비했습니다.", this); // 상점 준비 완료 로그
    } // 상점 시작 처리 종료
} // 상점 부트스트랩 종료
