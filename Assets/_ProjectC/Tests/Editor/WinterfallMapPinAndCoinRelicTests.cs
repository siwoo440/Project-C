using System.Collections; // 실제 탐사 화면 전환 대기
using System.Linq; // 원본 사건과 실제 상품 선택
using System.Reflection; // 기존 시드 계산과 정상 선택지 검사
using NUnit.Framework; // 탐사 및 상점 회귀 검사 도구
using TMPro; // 실제 선택 버튼의 문구 확인
using UnityEngine; // 실제 씬 객체와 게임의 보관함 사용
using UnityEngine.TestTools; // 격리 플레이 모드 전환
using UnityEngine.UI; // 실제 선택 버튼 동작 실행

public sealed class WinterfallMapPinAndCoinRelicTests // 지도핀 선택지와 동전줄 가격의 실제 회귀 검사
{ // 신규 탐사 유물 검사 시작
    [UnityTest] // 실제 이벤트 패널의 추가 보상 검증
    public IEnumerator MapPinAddsOneDeterministicAlternativeAndPaysOnlyOnce() // 시드 경계와 기존 행동 및 중복 수령 확인
    { // 실제 게임 진입 시작
        Assert.AreEqual("ProjectCVerification", Application.productName); // 분리된 탐사 저장 환경 확인
        yield return new EnterPlayMode(); // 실제 이벤트 패널 초기화 가능 상태 진입
        VerifyEvent(); // 디자인 시트의 추가 보상과 회차 방 처리 확인
        yield return new ExitPlayMode(); // 다음 검사 전에 탐사 환경 종료
    } // 실제 게임 진입 종료

    private static void VerifyEvent() // 실제 사건의 배치와 선택 및 원래 데이터 보존 확인
    { // 지도핀 이벤트 검사 시작
        var session = ExplorationSessionManager.EnsureInstance(); // 현재 탐사의 사건 해결 기록
        session.ResetExploration(); // 이전 사건이 없는 탐사 상태 준비
        const int floorSeed = 252; // 고정 추가 보상 추첨의 층 시드
        session.SetCurrentFloorSeed(floorSeed); // 실제 층과 방을 함께 사용하는 시드 구성
        var relic = Resources.Load<RelicData>("Relics/CrackedMapPin"); // 실제 갈라진 지도핀 원본 준비
        Assert.IsNotNull(relic); // 추가 보상 유물 리소스 확인
        Assert.AreEqual(10, relic.TriggerChancePercent); // 시트의 방당 추가 선택 판정 비율
        string eligible = RoomAtRoll(floorSeed, 9); // 열 퍼센트 바로 아래의 고정 결과
        string rejected = RoomAtRoll(floorSeed, 10); // 열 퍼센트 기준과 같은 실패 결과
        var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == AshPileEvent.Id); // 기존 행동 세 개를 가진 실제 윈터폴 사건
        var marker = new GameObject("Day252EventMarker").AddComponent<ExplorationEventView>(); // 사건 위치의 실제 상호작용 표식
        var panel = ExplorationEventPanelView.EnsureInstance(); // 게임에서 같은 탐사 사건 UI 준비
        var inventory = RelicRunManager.EnsureInstance(); // 유물 획득에 사용할 실제 탐사 보관함
        var wallet = PlayerResourceManager.EnsureInstance(); // 선택 보상 골드를 받을 실제 탐사 지갑
        try // 선택과 실제 창을 검증한 뒤 독립 객체 정리
        { // 이벤트 보상 검증 시작
            Assert.AreEqual(data.Choices.Count, Show(panel, marker, data, eligible).Length); // 유물이 없는 방에 기존 원문만 표시
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 정상 유물 최초 획득 확인
            Assert.AreEqual(data.Choices.Count, Show(panel, marker, data, rejected).Length); // 열 퍼센트 경계부터 추가 행동 없음 확인
            var first = Show(panel, marker, data, eligible); // 지도핀이 발견한 방을 실제 화면에 열기
            Assert.AreEqual(data.Choices.Count + 1, first.Length); // 추가 항목이 정확히 한 개 표시되는지 확인
            Assert.AreEqual(first.Last().GetComponentInChildren<TMP_Text>().text, Show(panel, marker, data, eligible).Last().GetComponentInChildren<TMP_Text>().text); // 다시 연 같은 방의 고정된 추가 행동 확인
            var action = Show(panel, marker, data, eligible).Last(); // 다시 표시된 실제 선택 버튼 조회
            StringAssert.Contains("골드 +10", action.GetComponentInChildren<TMP_Text>().text); // 결정 전 공개된 실제 보상 문구
            int gold = wallet.Gold; // 선택 직전 정상 탐사 골드 기준
            action.onClick.Invoke(); // 기존 사건 선택처럼 실제 보상 버튼 입력
            action.onClick.Invoke(); // 첫 처리 뒤 이전 버튼 콜백 재호출로 중복 검증
            Assert.AreEqual(gold + 10, wallet.Gold); // 두 번 눌러도 한 차례만 보상 지급 확인
            Assert.IsTrue(session.IsEventResolved(eligible)); // 실제 탐사 진행에 완료 방 표시 확인
            Assert.AreEqual(data.Choices.Count, Show(panel, marker, data, eligible).Length); // 완료 방의 추가 선택 재등장 차단
            Assert.AreEqual(3, data.Choices.Count); // 다음 방의 원래 사건 원본은 고정 상태 유지
        } // 이벤트 보상 검증 종료
        finally // 다른 화면 검사에 사용한 캔버스와 표식 정리
        { // 실제 게임 객체 정리 시작
            Object.Destroy(marker.gameObject); // 독립 사건 표식 해제
            Object.Destroy(panel.gameObject); // 오래 유지되는 독립 탐사 패널 해제
        } // 실제 게임 객체 정리 종료
    } // 지도핀 이벤트 검사 종료

    [UnityTest] // 실제 상점의 고정 가격과 공통 할인 회귀 검사
    public IEnumerator CoinStringStacksWithGeneralDiscountForItemsAndRemoval() // 실제 구입 가격과 카드 제거 가격 확인
    { // 상점 검사 시작
        Assert.AreEqual("ProjectCVerification", Application.productName); // 사용자 저장과 분리된 환경 확인
        yield return new EnterPlayMode(); // 실제 상점과 회차 데이터 준비
        VerifyShop(); // 동전줄의 할인 보유 전후 비교
        yield return new ExitPlayMode(); // 상점 검사 회차 종료
    } // 상점 검사 종료

    private static void VerifyShop() // 가격이 공통 할인과 합쳐지는지 실제 상점에서 확인
    { // 실제 상점 가격 검사 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 회차의 실제 출전 원본 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 출전 가능 인물 편성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 시작 덱을 위한 두 인물 구성
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 상점 연결에 필요한 파티와 덱 생성
        ExplorationSessionManager.EnsureInstance().ResetExploration(); // 다른 보유 효과를 초기화한 실제 탐사
        var manager = ShopRunManager.EnsureInstance(); // 기존 상점 거래 원본을 사용할 실제 런타임
        var catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 기본 판매가와 서비스 데이터
        Assert.IsTrue(manager.Prepare(catalog, PartyLoadoutRuntime.Active.Deck)); // 실제 상품 목록과 출전 덱 연결 확인
        var item = catalog.Offers.Single(offer => offer.OfferId == "SHOP_SHEET_HEAVY_STRIKE"); // 시트 공용 카드의 고정 판매가 비교
        Assert.AreEqual(80, manager.GetOfferPrice(item)); // 보유 유물 이전의 원본 가격 확인
        var inventory = RelicRunManager.EnsureInstance(); // 현재 탐사 유물 보관함 조회
        var coins = Resources.Load<RelicData>("Relics/WornCoinString"); // 손때 묻은 동전줄의 실제 원본 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(coins)); // 동전줄 실제 획득 확인
        Assert.AreEqual(76, manager.GetOfferPrice(item)); // 공용 카드 팔십 골드에서 오 퍼센트 차감
        Assert.AreEqual(143, manager.GetCardRemovalPrice()); // 백오십 골드에서 할인액 일곱 골드 차감
        var badge = Resources.Load<RelicData>("Relics/CerullionBadge"); // 기존 상점 할인 십 퍼센트 원본 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(badge)); // 기존 공통 할인 유물 추가 획득 확인
        Assert.AreEqual(68, manager.GetOfferPrice(item)); // 두 할인율의 합인 십오 퍼센트 적용
        Assert.AreEqual(128, manager.GetCardRemovalPrice()); // 공통 할인율과 기존 정수 차감 방식 유지
        Assert.IsTrue(inventory.TryRemove(coins.RelicId)); // 탐사에서 동전줄을 잃는 실제 처리 확인
        Assert.AreEqual(72, manager.GetOfferPrice(item)); // 제거 뒤 기존 십 퍼센트 할인 유지
    } // 실제 상점 가격 검사 종료

    private static Button[] Show(ExplorationEventPanelView panel, ExplorationEventView marker, ExplorationEventData data, string id) // 기존 이벤트의 실제 목록 열기
    { // 사건 화면 표시 시작
        marker.Initialize(data, id); // 방 식별자와 사건 원본을 기존 탐사 표식에 연결
        panel.ShowEvent(marker, data, id); // 정상 탐사 사건 인터페이스 호출
        panel.SendMessage("PopulateEventContent"); // 배치 검사의 실제 화면 구성 사용
        var root = panel.transform.Find("EventPanel"); // 실제 본문과 선택 버튼의 부모 조회
        root.gameObject.SetActive(true); // 게임 화면에서 검사할 목록 표시
        return root.GetComponentInChildren<ScrollRect>().content.GetComponentsInChildren<Button>(); // 실제 목록의 선택 버튼 반환
    } // 사건 화면 표시 종료

    private static string RoomAtRoll(int floorSeed, int expected) // 실제 동일 알고리즘으로 난수 경계 고정
    { // 방 시드 조회 시작
        var method = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 프로덕션 방 해시 함수 조회
        Assert.IsNotNull(method); // 시드 계산을 재사용하는 실제 메서드 확인
        for (int index = 0; index < 3000; index++) // 정확한 난수 경계에 해당하는 방 후보 순회
        { // 고정 방 확인 시작
            string room = $"day252_map_pin_room_{index}"; // 독립 탐사 검사의 실제 방 식별자 후보
            int seed = (int)method.Invoke(null, new object[] { $"mapPin|{floorSeed}|{room}" }); // 실제 추가 보상 추첨 시드 계산
            if (new System.Random(seed).Next(100) == expected) // 지정한 난수 경계의 첫 방 확인
            { // 방 후보 결정 시작
                return room; // 보상 경계 또는 실패 경계의 실제 방 반환
            } // 방 후보 결정 종료
        } // 고정 방 확인 종료
        Assert.Fail($"판정 {expected}에 해당하는 방이 없습니다."); // 예상 난수 범위의 결정적 후보 누락 확인
        return null; // 검사 실패 경로 반환
    } // 방 시드 조회 종료
} // 신규 탐사 유물 검사 종료
