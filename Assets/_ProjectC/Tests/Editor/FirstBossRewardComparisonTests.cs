using System.Collections; // 실제 탐사와 전투 씬 대기
using System.Collections.Generic; // 실제 보물 후보 목록 조회
using System.Linq; // 배치된 보물과 카드 소유자 조회
using System.Reflection; // 보물 추첨의 기존 내부 함수 연결
using NUnit.Framework; // 재화와 획득 결과 검증
using UnityEngine; // 실제 탐사 장면 조회
using UnityEngine.TestTools; // 플레이 모드 전투 측정
using UnityEngine.UI; // 상품과 소유자 버튼 입력

internal enum FirstBossScenario // 첫 보스 측정의 출전 조건
{ // 조건 목록 시작
    Original, // 기존 시작 덱 측정
    WithoutRewards, // 같은 세 층에서 보상을 얻지 않은 대조군
    WithRewards // 같은 세 층에서 보상을 받은 비교군
} // 조건 목록 종료

public sealed class FirstBossRewardComparisonTests // 같은 보스와 시드에서 확정된 탐사 보상 비교
{ // 보상 비교 시작
    internal const string Strike = "CRD_TEST_HEAVY_STRIKE"; // 실제 상점 공용 강타 식별자
    internal const string Fireball = "CRD_SHEET_FIREBALL"; // 실제 보물 공용 불덩이 식별자
    private static readonly int[] Seeds = FirstBossBalanceTests.Seeds; // 기존 측정과 같은 순서의 보스 시드
    private static readonly int[] FloorSeeds = { 21503, 21590, 21652 }; // 최초 측정에서 골드와 인장과 불덩이가 나온 지도 시드
    internal static int DepartureSeed => FloorSeeds[0]; // 탐사 씬 첫 프레임 전에 지정할 출발 층 시드

    [UnityTest] // 같은 세 층과 기본 덱 대조군
    public IEnumerator WithoutExpeditionRewards([ValueSource(nameof(Seeds))] int seed) // 보상을 받지 않은 실제 첫 보스전
    { // 대조군 시작
        return FirstBossBalanceTests.Run(seed, FirstBossScenario.WithoutRewards); // 동일한 턴과 카드 규칙으로 대조군 측정
    } // 대조군 종료

    [UnityTest] // 실제 세 층 보상 획득군
    public IEnumerator WithExpeditionRewards([ValueSource(nameof(Seeds))] int seed) // 보상을 획득한 실제 첫 보스전
    { // 보상군 시작
        return FirstBossBalanceTests.Run(seed, FirstBossScenario.WithRewards); // 동일한 턴과 카드 규칙으로 보상군 측정
    } // 보상군 종료

    internal static IEnumerator VisitRewardFloors(ExplorationSessionManager session, FirstBossScenario scenario) // 고정한 지도와 실제 방에서 보상 준비
    { // 세 층 진행 시작
        bool rewards = scenario == FirstBossScenario.WithRewards; // 실제 보상을 받을 조건 확인
        Assert.AreEqual(0, PlayerResourceManager.EnsureInstance().Gold); // 외부 지원금 없는 출발 확인
        Assert.AreEqual(12, RunDeckManager.Instance.CardCount); // 양쪽의 동일한 시작 덱 확인
        Assert.AreEqual(0, RelicRunManager.EnsureInstance().Inventory.Count); // 양쪽의 무유물 출발 확인
        for (int index = 0; index < FloorSeeds.Length; index++) // 같은 세 층의 보물과 상점 확인
        { // 한 층 준비 시작
            int floor = index + 1; // 검사할 실제 층 번호
            if (index > 0) // 출발층 이후의 지도 변경 확인
            { // 다음 층 이동 시작
                Assert.AreEqual(floor, session.AdvanceFloor()); // 전투로 인한 피해 없이 실제 층 상태 갱신
            } // 다음 층 이동 종료
            int floorSeed = floor == 1 ? FloorSeeds[index] : FindRewardFloorSeed(floor, FloorSeeds[index]); // 보물 후보 추가 후에도 같은 실제 보상 조합이 나오는 지도 선택
            Debug.Log($"[Day215Route] condition={scenario} floor={floor} mapSeed={floorSeed}"); // 전투 시드와 구분한 실제 획득 경로 기록
            session.SetCurrentFloorSeed(floorSeed); // 실제 층 맵 생성 시드 지정
            if (index > 0) // 출발 시 예약된 첫 씬 중복 방지
            { // 후속 탐사 씬 이동 시작
                SceneFlowManager.Instance.LoadScene("30_Exploration"); // 다음 층의 실제 방 생성
            } // 후속 탐사 씬 이동 종료
            yield return FirstBossBalanceTests.WaitFor(() => !SceneFlowManager.Instance.IsLoadingScene && Object.FindFirstObjectByType<ExplorationMapRuntime>()?.CurrentMap?.Seed == floorSeed && Object.FindFirstObjectByType<ShopPrototypeView>() != null, "탐사 보상 " + floor + "층"); // 새 층과 상점 화면 구성 대기
            yield return null; // 방 역할 교체와 이전 표시 정리 대기
            var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 실제 배치된 층 조회
            Assert.AreEqual(floor, map.CurrentFloor); // 기대한 이동 층 확인
            var treasure = Room(map, ExplorationRoomType.Treasure); // 랜덤 추첨으로 생성된 실제 보물 방 조회
            if (!rewards) // 대조군 보상 미획득 확인
            { // 대조군 층 통과 시작
                continue; // 같은 보물 방에서 지급 없이 다음 층으로 이동
            } // 대조군 층 통과 종료
            treasure.SendMessage("OpenTreasure"); // 실제 보물 추첨과 지급 경로 실행
            if (floor == 1) // 골드 보물과 강타 구매 확인
            { // 첫 층 구매 시작
                Assert.AreEqual(150, PlayerResourceManager.Instance.Gold); // 실제 보물의 정상 골드 지급 확인
                Room(map, ExplorationRoomType.Shop).SendMessage("OpenShop"); // 같은 층 상점 상품 화면 열기
                Click("Offer_SHOP_SHEET_HEAVY_STRIKE"); // 실제 공용 강타 상품 선택
                Click("ConfirmPurchase"); // 실제 가격 확인 뒤 소유자 선택
                Click("ChooseCardOwner_ARC_00"); // 바보에게 강타 구매
                Assert.AreEqual(70, PlayerResourceManager.Instance.Gold); // 실제 상점 가격 팔십 골드 차감 확인
                AssertOwner(Strike, "ARC_00"); // 바보의 회차 덱에 강타 한 장 확인
                Object.FindFirstObjectByType<ShopPrototypeView>().SendMessage("CloseShop"); // 다음 층 진행을 위한 상점 닫기
            } // 첫 층 구매 종료
            else if (floor == 2) // 두 번째 보물 유물 확인
            { // 유물 획득 확인 시작
                Assert.IsTrue(RelicRunManager.Instance.Inventory.ContainsRelic("RLC_ASH_SEAL")); // 실제 보물로 받은 잿빛 인장 확인
            } // 유물 획득 확인 종료
            else // 세 번째 보물 공용 공격 확인
            { // 보물 소유자 결정 시작
                Click("Owner_ARC_02"); // 실제 보물 선택 창에서 여사제 지정
                AssertOwner(Fireball, "ARC_02"); // 여사제의 회차 덱에 불덩이 한 장 확인
            } // 보물 소유자 결정 종료
        } // 한 층 준비 종료
        Assert.AreEqual(rewards ? 70 : 0, PlayerResourceManager.Instance.Gold); // 추가 구매나 보너스 지급 없는 준비 확인
        Assert.AreEqual(rewards ? 14 : 12, RunDeckManager.Instance.CardCount); // 실제 보물과 상점의 두 카드 차이 확인
    } // 세 층 진행 종료

    private static int FindRewardFloorSeed(int floor, int firstSeed) // 보상 후보 변경 후에도 같은 실제 방을 찾는 비교 조건 선택
    { // 조건을 충족하는 지도 검색 시작
        bool relic = floor == 2; // 잿빛 인장 또는 불덩이 구분
        const BindingFlags lookup = BindingFlags.Static | BindingFlags.NonPublic; // 보물 서비스 내부 조회 범위 지정
        var source = typeof(ExplorationTreasureRewardService); // 원래 보물 추첨 서비스 조회
        var offersMethod = source.GetMethod("GetValidOffers", lookup); // 현재 파티가 받을 수 있는 실제 후보 조회
        var hashMethod = source.GetMethod("BuildStableHash", lookup); // 실제 방 식별자의 고정 난수 함수 조회
        Assert.IsNotNull(offersMethod); // 보물 후보 함수 연결 확인
        Assert.IsNotNull(hashMethod); // 고정 방 난수 함수 연결 확인
        var offers = (List<ShopOfferData>)offersMethod.Invoke(null, new object[] { relic ? ShopOfferType.Relic : ShopOfferType.Card }); // 현재 원본 카탈로그와 파티로 후보 구성
        var hash = (System.Func<string, int>)System.Delegate.CreateDelegate(typeof(System.Func<string, int>), hashMethod); // 실제 보물 방과 같은 해시 사용
        int target = offers.FindIndex(item => relic ? item.Relic.RelicId == "RLC_ASH_SEAL" : item.Card.CardId == Fireball); // 현재 후보 목록에서 목표 보상 위치 조회
        Assert.GreaterOrEqual(target, 0); // 실제 지급 가능한 목표 보상 확인
        for (int seed = firstSeed; seed < firstSeed + 20000; seed++) // 최초 측정 시드부터 고정 순서로 조건 탐색
        { // 후보 지도와 보물 추첨 검사 시작
            var map = ExplorationMapGenerator.Generate(14, seed, floor); // 정식 층과 같은 열네 방 지도 생성
            var cell = map.Cells.SingleOrDefault(item => item.RoomType == ExplorationRoomType.Treasure); // 실제 보물 방 좌표 조회
            if (cell == null) // 보물이 없는 층 확인
            { // 보물 없는 지도 제외 시작
                continue; // 다음 지도 검사
            } // 보물 없는 지도 제외 종료
            string id = $"SR_Treasure_F{floor}_X{cell.Coordinate.x}_Y{cell.Coordinate.y}_S{seed}"; // 보물 서비스가 받는 실제 방 식별자 구성
            var random = new System.Random(hash(id)); // 보물 획득과 같은 방별 난수 생성
            var kind = ExplorationTreasureRewardService.RollRewardType(random.Next(100)); // 원래 종류별 가중치 판정 사용
            if (kind == (relic ? ExplorationTreasureRewardType.Relic : ExplorationTreasureRewardType.Card) && random.Next(offers.Count) == target) // 기본 추첨에서 목표 보상이 나오는 실제 방 확인
            { // 지도 확정 시작
                return seed; // 실제 탐사에서 같은 방을 열 지도 시드 반환
            } // 지도 확정 종료
        } // 후보 지도와 보물 추첨 검사 종료
        Assert.Fail("등록된 보상 후보에서 조건을 만족하는 지도를 찾지 못했습니다."); // 획득 경로 변경을 알려주는 검사 실패
        return firstSeed; // 컴파일용 반환
    } // 조건을 충족하는 지도 검색 종료

    private static ExplorationSpecialRoomView Room(ExplorationMapRuntime map, ExplorationRoomType type) // 탐사 지도에서 실제 특수 방 검색
    { // 방 검색 시작
        var cell = map.CurrentMap.Cells.Single(item => item.RoomType == type); // 정식 지도에서 배치 좌표 조회
        string name = $"SpecialRoom_{type}_{cell.Coordinate.x}_{cell.Coordinate.y}"; // 실제 생성된 방 이름 조합
        var room = GameObject.Find(name); // 지도에 표시된 오브젝트 조회
        Assert.IsNotNull(room, name); // 실제 보물 또는 상점 방 존재 확인
        return room.GetComponent<ExplorationSpecialRoomView>(); // 접촉과 같은 방 선택 경로 반환
    } // 방 검색 종료

    private static void Click(string name) // 실제 활성 상품이나 소유자 버튼 실행
    { // 버튼 입력 시작
        var button = GameObject.Find(name)?.GetComponent<Button>(); // 실제 화면에 표시된 버튼 조회
        Assert.IsNotNull(button, name); // 해당 상품이나 소유자 존재 확인
        Assert.IsTrue(button.interactable, name); // 정상 입력 가능 여부 확인
        button.onClick.Invoke(); // 게임의 실제 선택 처리 실행
    } // 버튼 입력 종료

    private static void AssertOwner(string cardId, string ownerId) // 실제 회차 덱 소유자 검증
    { // 소유자 확인 시작
        var card = RunDeckManager.Instance.Cards.Single(entry => entry.Card.CardId == cardId); // 획득한 카드 한 장 조회
        Assert.AreEqual(ownerId, card.Owner.CharacterId); // 선택한 파티원에게 들어간 카드 확인
    } // 소유자 확인 종료
} // 보상 비교 종료
