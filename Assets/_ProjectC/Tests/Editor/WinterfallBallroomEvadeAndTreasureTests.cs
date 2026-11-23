using System; // 독립 보물 난수와 실제 화면 대기 조건
using System.Collections; // 플레이 모드 전환 및 카드 연출 대기
using System.Collections.Generic; // 실제 보물 후보 목록 조회
using System.IO; // 독립 저장 검사 경로 사용
using System.Linq; // 절차 보물방과 소유자 버튼 조회
using System.Reflection; // 기존 탐사 생성 및 보상 난수 경로 실행
using NUnit.Framework; // 실제 회피와 보물 지급 결과 검증
using UnityEngine; // 전투 엔진과 탐사 방 화면 사용
using UnityEngine.SceneManagement; // 실제 시작과 탐사 씬 진입
using UnityEngine.TestTools; // 실제 플레이 모드 테스트 실행
using UnityEngine.UI; // 실제 보상 소유자 버튼 확인
using Object = UnityEngine.Object; // 유니티의 현재 씬 원본 조회
using Fight = WinterfallSwordCombatTests.Fight; // 카드 조작의 기존 실제 전투 환경

public sealed class WinterfallBallroomEvadeAndTreasureTests // 실제 회피 보상과 절차 보물방 추가 보상 검사
{ // 무도화와 서랍 열쇠 플레이 회귀 검사 시작
    [UnityTest] // 기존 단일 명중 난수와 실제 후속 직접 피해 검사
    public IEnumerator DanceShoeOnlyRewardsAnEvasionAndWaitsForTheNextLandedDirectAttack() // 실명 빗나감과 피해 무효 및 유효 명중의 경계 확인
    { // 실제 적 공격의 회피와 다음 명중 검사 시작
        yield return new EnterPlayMode(); // 정식 카드 화면과 적 공격 상태를 사용하는 모드 진입
        Acquire("BrokenDanceShoe"); // 부서진 무도화 실제 원본 획득
        using (var fight = new Fight("THRUST", "THRUST", "THRUST")) // 빗나감과 첫 명중 및 이후 타격용 원본 카드 셋
        { // 실제 적 공격과 아군의 보너스 적용 비교 시작
            var ally = fight.Allies[0]; // 무도화의 회피와 공격을 하는 실제 아군
            var attacker = fight.Enemies[0]; // 실명 페널티와 일반 공격을 주는 실제 적
            var nextAttack = BattleStatusEffectType.DanceShoeDamageUp; // 다음 유효 명중을 기다리는 실제 무도화 상태 종류
            attacker.ApplyStatusEffect(BattleStatusEffectType.Blind, 20, 3, 1); // 적 자체 원인으로 실패하는 명중 구간 준비
            attacker.ApplyStatusEffect(BattleStatusEffectType.Disorientation, 10, 3, 1); // 실명과 별개의 원래 명중 감소 준비
            ally.ApplyStatusEffect(BattleStatusEffectType.Evasion, 30, 3, 1); // 아군 회피로만 빗나가는 구간 추가
            Assert.AreEqual(70, BattleHitChance.GetPercent(attacker)); // 아군 회피가 없는 적 자체 명중률 확인
            Assert.AreEqual(40, BattleHitChance.GetPercent(attacker, ally)); // 아군 회피까지 적용된 실제 명중률 확인
            int rolls = 0; // 적 행동의 실제 명중 굴림 횟수 기록
            BattleEnemyAction Attack(int roll) // 지정한 난수를 쓰는 실제 적 공격 준비
            { // 단일 물리 공격 생성 시작
                return new BattleEnemyAction(attacker, ally, EnemyActionType.Attack, BattleDamageType.Physical, 8, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "회피 검사", 1, 1, 1, 0, percentRoll: () => { rolls++; return roll; }); // 원래 적용할 난수와 실제 직접 공격 구성
            } // 단일 물리 공격 생성 종료
            Assert.IsTrue(Attack(70).Execute().WasMissed); // 적 자체 페널티만으로도 빗나가는 원래 실패 경계 확인
            Assert.IsNull(ally.GetStatusEffect(nextAttack)); // 적 자체 빗나감으로 무도화 강화가 붙지 않음 확인
            ally.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 회피와 구별할 원래 피해 무효 상태 적용
            Assert.IsTrue(Attack(39).Execute().DamageResult.WasDamageNullified); // 실제로 맞은 공격을 기존 피해 무효로 막았는지 확인
            Assert.IsNull(ally.GetStatusEffect(nextAttack)); // 유효 명중 후 피해 무효는 회피 보상이 없음 확인
            Assert.IsTrue(Attack(40).Execute().WasMissed); // 아군 회피 때문에 처음 빗나가는 경계에서 공격 확인
            var reserved = ally.GetStatusEffect(nextAttack); // 실제 회피가 새로 획득한 피해 예약 상태
            Assert.IsNotNull(reserved); // 정확한 명중 구간의 정상 회피 보상 확인
            Assert.AreEqual(2, reserved.EffectiveValue); // 다음 직접 명중의 가산 피해 두 점 확인
            Assert.IsTrue(Attack(69).Execute().WasMissed); // 적 자체 명중 범위 안의 마지막 회피 난수 확인
            Assert.AreSame(reserved, ally.GetStatusEffect(nextAttack)); // 추가 회피에도 같은 미사용 상태 유지 확인
            Assert.AreEqual(1, reserved.StackCount); // 사용 전 반복 회피가 중첩을 늘리지 않음 확인
            Assert.AreEqual(4, rolls); // 각 원래 적 공격은 하나의 기존 난수만 소모 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 서로 다른 아군 턴으로 진행
            Assert.AreSame(reserved, ally.GetStatusEffect(nextAttack)); // 기다리던 상태가 다음 턴에도 원래 인스턴스로 남음 확인
            var target = fight.Enemies[1]; // 아군 직접 타격이 들어갈 실제 생존 다른 적
            target.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 먼저 빗나갈 아군 공격의 대상 회피 준비
            yield return fight.Use("THRUST", 1); // 진짜 카드 경로에서 빗나간 공격 실행
            Assert.AreSame(reserved, ally.GetStatusEffect(nextAttack)); // 빗나간 아군 공격에 기존 예약을 소비하지 않음 확인
            target.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 같은 대상의 실제 명중 조건 복원
            target.GainBlock(100); // 체력 피해와 별개인 방어도 명중을 재현할 큰 방어도
            int printed = fight.View("THRUST").RuntimeCard.EffectValue; // 실제 카드 원래 피해 수치
            int raw = 0; // 피해 적용 이벤트가 받은 방어 전 실제 카드 피해
            target.DamageTaken += (_, result) => raw = result.RawDamage; // 실제 카드 충돌 뒤 보정 전 원본 수치 관찰
            yield return fight.Use("THRUST", 1); // 실제 방어도로 막혀도 접촉하는 직접 공격 실행
            Assert.AreEqual(printed + 2, raw); // 처음 맞힌 공격의 원본 수치에 두 점 추가 확인
            Assert.IsNull(ally.GetStatusEffect(nextAttack)); // 처음 유효한 직접 명중 후 한 번의 가산 소모 확인
            yield return fight.Use("THRUST", 1); // 같은 다음 타격에 다시 적용되지 않는지 확인
            Assert.AreEqual(printed, raw); // 사용한 보너스 뒤 원래 카드 피해 복귀 확인
        } // 실제 적 공격과 아군의 보너스 적용 비교 종료
        yield return new ExitPlayMode(); // 무도화의 실제 플레이 환경 종료
    } // 실제 적 공격의 회피와 다음 명중 검사 종료

    [UnityTest] // 실제 생성한 보물방의 기본 카드 선택과 독립 추가 골드 검사
    public IEnumerator DrawerPaysOnceOnlyAfterARealTreasureCardIsClaimedAndPersistsOnReload() // 실제 방에서 취소와 소유자 승인 및 저장 재진입 비교
    { // 절차 생성과 기본 보물 지급 비교 검사 시작
        yield return new EnterPlayMode(); // 원래 탐사 루프와 저장을 쓰는 실행 환경
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 사용자 주 저장과 별개인 독립 복원 파일
        try // 선택 화면과 독립 저장의 정리 보장
        { // 정식 게임 경로로 실제 보물방 생성 시작
            SceneManager.LoadScene("00_Boot"); // 실제 게임 초기 시스템으로 시작
            yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null); // 실제 시작 메뉴의 버튼 준비 대기
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 정식 거점으로 이동 요청
            yield return WaitFor(() => Object.FindFirstObjectByType<LobbyLayoutView>() != null); // 원래 거점의 출발 UI 대기
            PartyDepartureTestSupport.StartExploration(0, 1); // 실제 보물의 카드 소유자를 선택할 두 명 파티 출발
            yield return WaitFor(() => Object.FindFirstObjectByType<ExplorationPlayerController>() != null && SceneFlowManager.Instance?.IsLoadingScene == false); // 실제 탐사 지도와 플레이어 초기화 대기
            Acquire("LockedDrawerTreasureKey"); // 현재 회차에 잠긴 서랍 열쇠의 실제 원본 추가
            var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 실제 절차 방을 생성하는 현재 탐사 맵
            var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 독립 저장 위치의 실제 탐사 캐릭터
            var session = ExplorationSessionManager.Instance; // 보물 사용과 맵 시드를 관리하는 실제 회차
            var shop = ShopRunManager.EnsureInstance(); // 원본 카드 보물 후보를 보관하는 관리자
            Assert.IsTrue(shop.Prepare(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"), PartyLoadoutRuntime.Active.Deck)); // 정상 보물의 기존 카드 보상 원본 준비
            var candidates = typeof(ExplorationTreasureRewardService).GetMethod("GetValidOffers", BindingFlags.Static | BindingFlags.NonPublic); // 실제 보상 난수에 연결된 현재 후보 순서 조회
            var offers = (List<ShopOfferData>)candidates.Invoke(null, new object[] { ShopOfferType.Card }); // 공용 소유자 선택 가능한 실제 카드 목록
            var selected = FindTreasureForCardAndGold(session.CurrentFloor, offers); // 보상 시드와 독립 추가 보상이 함께 성공할 절차 방 선택
            session.SetCurrentFloorSeed(selected.Seed); // 실제 저장된 층 시드와 생성 시드를 동일하게 유지
            var build = typeof(ExplorationMapRuntime).GetMethod("BuildCurrentFloor", BindingFlags.Instance | BindingFlags.NonPublic); // 실전 지도 생성과 특수 방 생성이 사용하는 기존 메서드
            build.Invoke(map, new object[] { selected.Seed, false }); // 지정한 실제 절차 맵을 현재 탐사 장면에 설치
            string viewName = $"SpecialRoom_Treasure_{selected.Cell.Coordinate.x}_{selected.Cell.Coordinate.y}"; // 원래 생성기가 붙이는 보물방 화면 이름
            yield return WaitFor(() => GameObject.Find(viewName) != null); // 실제 보물 접촉과 카드 선택 화면을 만드는 방 대기
            var room = GameObject.Find(viewName).GetComponent<ExplorationSpecialRoomView>(); // 실제 절차 생성 결과의 보물 상호작용 조회
            var resources = PlayerResourceManager.EnsureInstance(); // 실제 탐사 골드 지갑 조회
            int gold = resources.Gold; // 기본 카드 보물 전 원래 골드 보관
            int cards = RunDeckManager.Instance.CardCount; // 소유자 지정 전 실제 회차 카드 수
            Assert.IsTrue(ExplorationTreasureRewardService.TryGrantReward(selected.Id, out _, out var preview)); // 일반 보물 API가 같은 원래 카드 선택을 준비
            Assert.AreSame(selected.Offer, preview); // 추가 판정이 원래 보물 종류와 후보 난수에 영향을 주지 않음 확인
            Assert.AreEqual(gold, resources.Gold); // 일반 보물 API만 직접 호출해도 서랍 골드 없음 확인
            room.SendMessage("OpenTreasure"); // 절차 생성한 실제 방의 원래 접촉 보상 처리 실행
            var popup = room.transform.Find("TreasureOwnerCanvas/Panel"); // 실제 보물의 파티 카드 소유자 선택 화면
            Assert.IsNotNull(popup); // 카드 보물의 추가 선택 상태 확인
            Assert.AreEqual(gold, resources.Gold); // 보상 소유자를 고르기 전에는 추가 골드 없음 확인
            popup.Find("CancelButton").GetComponent<Button>().onClick.Invoke(); // 실제 취소 버튼으로 보물 미완료 상태 복구
            Assert.AreEqual(gold, resources.Gold); // 취소가 의도치 않은 별도 골드를 주지 않음 확인
            Assert.IsFalse(session.IsEventResolved(selected.Id)); // 선택 취소 뒤 실제 방은 다시 열 수 있음 확인
            room.SendMessage("OpenTreasure"); // 취소한 방을 원래 입력 경로로 다시 열기
            var owner = PartyLoadoutRuntime.Active.Party.Members[1]; // 실제로 카드 보상을 받을 소유자
            room.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + owner.CharacterId).GetComponent<Button>().onClick.Invoke(); // 실제 소유자 버튼으로 카드 보상 확정
            Assert.AreEqual(cards + 1, RunDeckManager.Instance.CardCount); // 원래 카드가 실제 회차 덱에 한 장 입장
            Assert.AreSame(selected.Offer.Card, RunDeckManager.Instance.Cards[cards].Card); // 보물 기본 난수가 고른 동일 카드 보존 확인
            Assert.AreEqual(gold + 150, resources.Gold); // 실제 선택 완료 뒤 같은 방의 추가 골드 정확히 지급
            Assert.IsTrue(session.IsEventResolved(selected.Id)); // 한 번만 지급한 원래 방의 완료 상태 확인
            var open = typeof(ExplorationSpecialRoomView).GetMethod("OpenTreasure", BindingFlags.Instance | BindingFlags.NonPublic); // 완료돼 비활성인 원래 방의 진입 검사
            open.Invoke(room, null); // 같은 방의 즉시 반복 방문 시도
            Assert.AreEqual(gold + 150, resources.Gold); // 기존 완료 방에는 추가 골드 중복 없음 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 결과와 실제 골드를 독립 파일에 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 같은 맵 결과와 골드 복원
            Assert.IsTrue(session.IsEventResolved(selected.Id)); // 불러오기 후 실제 완료 표시 유지 확인
            Assert.AreEqual(gold + 150, PlayerResourceManager.EnsureInstance().Gold); // 다시 들어온 탐사에 같은 한 번의 지급액 복원 확인
        } // 정식 게임 경로로 실제 보물방 생성 종료
        finally // 독립 파일과 열려 있을 경우의 선택 창 정리
        { // 실제 보상 검사 환경 정리 시작
            ExplorationRewardOwnerView.ClosePending(); // 후속 테스트를 위한 원래 선택 창 및 시간 복원
            ExplorationSaveService.Remove(path); // 독립 테스트 저장과 복구 파일 정리
        } // 실제 보상 검사 환경 정리 종료
        yield return new ExitPlayMode(); // 절차 생성 보물방의 실제 실행 검사 종료
    } // 절차 생성과 기본 보물 지급 비교 검사 종료

    private static (int Seed, ExplorationMapCell Cell, string Id, ShopOfferData Offer) FindTreasureForCardAndGold(int floor, List<ShopOfferData> offers) // 실제 두 독립 난수가 맞는 절차 방 탐색
    { // 재현 가능한 방 시드 탐색 시작
        var hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 기본과 추가 판정이 같이 쓰는 원본 고정 시드 함수
        for (int seed = 0; seed < 10000; seed++) // 일정 범위의 실제 절차 맵 시드 순회
        { // 현재 시드의 실제 방 생성 시작
            var map = ExplorationMapGenerator.Generate(14, seed, floor); // 실전과 같은 현재 층 셀 수 및 역할 생성
            var cell = map.Cells.FirstOrDefault(room => room.RoomType == ExplorationRoomType.Treasure); // 절차 생성된 유일한 보물방 후보
            if (cell == null) // 시드에 따라 보물방이 없는 경우 확인
            { // 후보 없는 시드 제외 시작
                continue; // 다음 실제 지도 시드 확인
            } // 후보 없는 시드 제외 종료
            string id = $"SR_Treasure_F{floor}_X{cell.Coordinate.x}_Y{cell.Coordinate.y}_S{seed}"; // 실제 방 생성기의 원본 런타임 식별자와 같은 값
            var primary = new System.Random((int)hash.Invoke(null, new object[] { id })); // 방의 기본 보물 종류를 재현하는 독립 난수
            var bonus = new System.Random((int)hash.Invoke(null, new object[] { id + ":drawer-bonus" })); // 열쇠만 사용하는 별도 고정 난수
            if (ExplorationTreasureRewardService.RollRewardType(primary.Next(100)) != ExplorationTreasureRewardType.Card || bonus.Next(100) >= 15) // 카드 종류와 추가 골드 모두 가능한 방인지 확인
            { // 원하는 경계와 다른 방 제외 시작
                continue; // 충족할 다음 실제 생성 시드 선택
            } // 원하는 경계와 다른 방 제외 종료
            var offer = offers[primary.Next(offers.Count)]; // 기본 카드를 골랐을 때의 실제 원래 카드 후보
            if (offer.RequiresOwnerChoice) // 파티원 선택으로 완료되는 보물인지 확인
            { // 정확한 절차 테스트 방 발견 시작
                return (seed, cell, id, offer); // 원본 지도와 카드 선택 및 추가 보상 시드 확정
            } // 정확한 절차 테스트 방 발견 종료
        } // 현재 시드의 실제 방 생성 종료
        Assert.Fail("카드 선택과 추가 골드가 함께 발생하는 절차 보물방 없음"); // 실제 생성 지도에서 실패한 검사 전제
        return default; // 단언 실패 뒤의 반환 계약
    } // 재현 가능한 방 시드 탐색 종료

    private static void Acquire(string key) // 현재 실제 회차에서 등록 유물 원본 획득
    { // 회차 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 배포된 실제 유물 원본 로드
        Assert.IsNotNull(relic, key); // 유물 등록 여부 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 기존 회차 유물 획득 규칙으로 보유 확인
    } // 회차 유물 획득 종료

    private static IEnumerator WaitFor(Func<bool> condition) // 테스트 화면의 실제 유한 초기화 대기
    { // 일반 씬 전환 대기 시작
        float end = Time.realtimeSinceStartup + 30f; // 화면 초기화에 허용할 최대 실제 시간
        while (!condition() && Time.realtimeSinceStartup < end) // 실제 런타임이 초기화할 프레임 반복
        { // 다음 전투 또는 탐사 프레임 시작
            yield return null; // 정상 씬 처리 진행
        } // 다음 전투 또는 탐사 프레임 종료
        Assert.IsTrue(condition()); // 제한 시간 내 실제 검사 화면 준비 확인
    } // 일반 씬 전환 대기 종료
} // 무도화와 서랍 열쇠 플레이 회귀 검사 종료
