using System.Collections; // 플레이 모드 검사 대기
using System.IO; // 독립 탐사 저장 경로
using System.Linq; // 카드와 파티 상태 조회
using System.Reflection; // 전투 관리자 입력 경계 검사
using NUnit.Framework; // 결과 비교
using TMPro; // 실제 선택 버튼 원문 조회
using UnityEngine; // 실제 탐사와 전투 객체 사용
using UnityEngine.TestTools; // 플레이 모드 진입과 종료
using UnityEngine.UI; // 선택 버튼 입력

public sealed class BurntScarecrowEventTests // 타버린 허수아비와 잿빛 저주 검사
{ // 검사 시작
    [Test] // 버전과 위험 보상 검사 지정
    public void NewFloorAddsScarecrowAndDrawsOnlySavedRareCardsOrCurse() // 이전 층 보존과 실제 두 보상 범위 확인
    { // 원본 검사 시작
        var old = ExplorationEventCatalog.LoadEvents(3); // 지도 조각까지의 이전 층 조회
        var added = ExplorationEventCatalog.LoadEvents(4); // 허수아비 도입 층 조회
        Assert.IsFalse(old.Any(item => item.EventId == BurntScarecrowEvent.Id)); // 구버전 현재 층 후보 보존
        CollectionAssert.AreEquivalent(old.Select(item => item.EventId).Append(BurntScarecrowEvent.Id), added.Select(item => item.EventId)); // 허수아비 한 가지 추가 확인
        var data = added.Single(item => item.EventId == BurntScarecrowEvent.Id); // 탐사에 배치되는 사건 원본 조회
        CollectionAssert.AreEqual(new[] { "조사한다", "불태운다", "지나친다" }, data.Choices.Select(item => item.ChoiceText)); // 선택지 원문 확인
        Assert.IsNotNull(data.IllustrationSprite); // 실제 허수아비 사건 삽화 확인
        var saved = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 가능한 카드 원본 목록
        var curse = saved.Cards.Single(card => card.CardId == WinterfallRiskCardReward.CurseCardId); // 유일한 저주 원본 조회
        Assert.AreEqual("잿빛 저주", curse.DisplayName); // 지명된 저주 이름 확인
        Assert.IsNotNull(curse.Artwork); // 저주 카드 그림 연결 확인
        Assert.AreEqual(CardEffectType.Unplayable, curse.EffectType); // 사용 불가 원본 설정 확인
        Assert.AreEqual(CardType.Curse, curse.CardType); // 사건 저주 전용 분류 확인
        Assert.IsFalse(Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Any(offer => offer.Card == curse)); // 일반 상품과 보물에서 저주 제외 확인
        CardData rare = WinterfallRiskCardReward.GetReward(FindRoom(false)); // 희귀 분기 실제 지급 원본
        Assert.AreEqual("희귀", CardSheetCatalog.Load().FindByName(rare.DisplayName).Rarity); // 시트 희귀 공용 카드 확인
        CollectionAssert.Contains(saved.Cards, rare); // 희귀 보상 저장 원본 확인
        Assert.AreSame(curse, WinterfallRiskCardReward.GetReward(FindRoom(true))); // 저주 분기 실제 원본 확인
        Assert.IsNull(WinterfallRiskCardReward.GetReward(string.Empty)); // 방 식별자 없는 추첨 차단
    } // 원본 검사 종료

    [UnityTest] // 실제 조사와 파일 복원 검사 지정
    public IEnumerator InvestigationCancelsRetriesAndRestoresBothRewardsToActor() // 행동자 선택과 두 결과의 고정 지급 확인
    { // 조사 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyInvestigation(); // 실제 운영 화면과 파일 검사
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 조사 검사 종료

    private static void VerifyInvestigation() // 행동자 취소와 저장 왕복 확인
    { // 조사 흐름 시작
        string path = Path.Combine(Application.temporaryCachePath, "scarecrow-" + System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 파일 경로
        var session = PrepareParty(); // 출전과 덱 준비
        var party = BattleResultManager.Instance; // 탐사 파티 조회
        var actor = party.ActiveParty.Members[1]; // 조사할 둘째 파티원 선택
        var other = party.ActiveParty.Members[0]; // 비교할 첫 파티원 조회
        party.RestoreSavedAllyState(actor, 18, 30, 0); // 행동자 초기 체력과 정신력 설정
        party.RestoreSavedAllyState(other, 15, 40, 0); // 동료 초기 체력과 정신력 설정
        var deck = RunDeckManager.Instance; // 실제 회차 덱 조회
        int before = deck.CardCount; // 시작 덱 장수 보관
        string cursedRoom = FindRoom(true); // 저주가 나오는 고정 방 탐색
        string rareRoom = FindRoom(false); // 희귀 카드가 나오는 고정 방 탐색
        var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == BurntScarecrowEvent.Id); // 실제 최신 허수아비 원본 조회
        var marker = new GameObject("BurntScarecrowTest").AddComponent<ExplorationEventView>(); // 실제 사건 표식 생성
        var panel = ExplorationEventPanelView.EnsureInstance(); // 사건 화면 준비
        try // 임시 저장과 화면 정리 보장
        { // 실제 조사 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 조사 전 파일 작성
            marker.Initialize(data, cursedRoom); // 표식에 저주 분기 연결
            panel.ShowEvent(marker, data, cursedRoom); // 사건 선택 화면 열기
            ClickInvestigate(panel); // 조사 행동 선택
            panel.transform.Find("TreasureOwnerCanvas/Panel/CancelButton").GetComponent<Button>().onClick.Invoke(); // 최종 행동자 지정 취소
            Assert.IsFalse(session.IsEventResolved(cursedRoom)); // 취소된 방 미완료 확인
            Assert.AreEqual(before, deck.CardCount); // 취소 뒤 카드 미지급 확인
            ClickInvestigate(panel); // 같은 행동 다시 선택
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 행동자 선택 중 이전 저장 복원
            Assert.IsNull(panel.transform.Find("TreasureOwnerCanvas")); // 이전 선택 화면 닫힘 확인
            Assert.IsFalse(ExplorationPlayerController.InputBlocked); // 이전 선택 입력 잠금 해제 확인
            marker.Initialize(data, cursedRoom); // 복원한 방에 표식 재연결
            panel.ShowEvent(marker, data, cursedRoom); // 동일한 사건 재개
            ClickInvestigate(panel); // 복원 뒤 조사 선택
            panel.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + actor.CharacterId).GetComponent<Button>().onClick.Invoke(); // 실제 둘째 파티원 버튼 클릭
            Assert.AreEqual(before + 1, deck.CardCount); // 행동자 한 번 선택으로 한 장 지급 확인
            Assert.AreEqual(WinterfallRiskCardReward.CurseCardId, deck.Cards.Last().Card.CardId); // 조사 전과 같은 저주 지급 확인
            Assert.AreSame(actor, deck.Cards.Last().Owner); // 행동자 자신의 덱에 지급 확인
            Assert.IsFalse(BurntScarecrowEvent.TryResolve(cursedRoom, BurntScarecrowEvent.Investigate, other, out _)); // 다른 소유자로 중복 지급 차단 확인
            CardData rare = WinterfallRiskCardReward.GetReward(rareRoom); // 다른 방의 희귀 카드 보관
            Assert.IsTrue(BurntScarecrowEvent.TryResolve(rareRoom, BurntScarecrowEvent.Investigate, actor, out _)); // 별도 방의 희귀 결과 선택
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 두 결과를 얻은 탐사 파일 작성
            deck.ResetToSource(null); // 보유 카드와 소유자 제거
            session.ResetExploration(); // 처리한 방 정보 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 비운 상태에서 파일 복원
            CollectionAssert.AreEqual(new[] { WinterfallRiskCardReward.CurseCardId, rare.CardId }, deck.Cards.Skip(before).Select(entry => entry.Card.CardId)); // 두 결과별 원본과 순서 복원 확인
            Assert.IsTrue(deck.Cards.Skip(before).All(entry => entry.Owner == actor)); // 두 보상 모두 행동자 소유 확인
            Assert.IsTrue(session.IsEventResolved(cursedRoom) && session.IsEventResolved(rareRoom)); // 완료한 두 방 기록 복원 확인
            AssertState(actor, 18, 22); // 두 번의 정신력 비용만 복원 확인
            AssertState(other, 15, 40); // 동료에게 비용 미적용 확인
        } // 실제 조사 종료
        finally // 파일과 표식 정리
        { // 정리 시작
            ExplorationRewardOwnerView.ClosePending(); // 미완료 행동자 선택 종료
            ExplorationSaveService.Remove(path); // 독립 탐사 파일과 복구본 제거
            Object.Destroy(marker.gameObject); // 검사 표식 정리
        } // 정리 종료
    } // 조사 흐름 종료

    [UnityTest] // 저주 소각과 지나침 검사 지정
    public IEnumerator BurningRemovesExactlyOneOwnedCurseAndLeavesPassingFree() // 소유자와 개수 및 소각의 전멸 처리 확인
    { // 소각 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyBurn(); // 실제 회차 덱의 저주 소각 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 소각 검사 종료

    private static void VerifyBurn() // 저주 없는 행동과 저주 있는 소각의 차이 확인
    { // 소각 흐름 시작
        var session = PrepareParty(); // 새 회차와 생존자 준비
        var party = BattleResultManager.Instance; // 현재 탐사 파티 조회
        var actor = party.ActiveParty.Members[0]; // 소각할 생존자 선택
        var other = party.ActiveParty.Members[1]; // 저주를 보존할 동료 선택
        party.RestoreSavedAllyState(actor, 18, 33, 0); // 행동자 초기 수치 설정
        party.RestoreSavedAllyState(other, 11, 42, 0); // 동료 초기 수치 설정
        var deck = RunDeckManager.Instance; // 파티 회차 덱 조회
        var curse = WinterfallRiskCardReward.GetReward(FindRoom(true)); // 저장 가능한 저주 원본 조회
        Assert.IsTrue(deck.TryAddCard(curse, other)); // 동료 단독 저주 지급
        int count = deck.CardCount; // 지급 후 덱 장수 보관
        Assert.IsFalse(BurntScarecrowEvent.TryResolve("invalid", BurntScarecrowEvent.Burn, null, out _)); // 무효 행동자 비용 차단 확인
        Assert.IsTrue(BurntScarecrowEvent.TryResolve("pass", BurntScarecrowEvent.Pass, null, out _)); // 지나침 선택 처리
        Assert.IsTrue(session.IsEventResolved("pass")); // 지나친 방 완료 확인
        AssertState(actor, 18, 33); // 지나침 뒤 자원 보존 확인
        Assert.AreEqual(count, deck.CardCount); // 지나침 뒤 덱 보존 확인
        Assert.IsTrue(BurntScarecrowEvent.TryResolve("empty", BurntScarecrowEvent.Burn, actor, out _)); // 자신의 저주가 없는 소각 실행
        AssertState(actor, 14, 33); // 저주 유무와 별개인 체력 비용 확인
        Assert.AreEqual(count, deck.CardCount); // 남의 저주를 제거하지 않는 규칙 확인
        Assert.IsTrue(deck.TryAddCard(curse, actor) && deck.TryAddCard(curse, actor)); // 행동자에게 저주 두 장 지급
        while (deck.CardCount > RunDeckManager.MinimumCardsAfterRemoval) // 상점 제거 하한까지 검사 덱 축소
        { // 기존 카드 제거 시작
            int index = deck.Cards.ToList().FindIndex(entry => entry.Card != curse); // 저주가 아닌 기존 카드 조회
            Assert.IsTrue(deck.TryRemoveAt(index)); // 일반 제거 규칙으로 카드 정리
        } // 기존 카드 제거 종료
        Assert.IsTrue(BurntScarecrowEvent.TryResolve("owned", BurntScarecrowEvent.Burn, actor, out _)); // 상점 하한에서도 행동자의 저주 소각
        AssertState(actor, 10, 33); // 정확히 네 점 소모 확인
        Assert.AreEqual(1, deck.Cards.Count(entry => entry.Card == curse && entry.Owner == actor)); // 행동자의 저주 딱 한 장만 제거 확인
        Assert.AreEqual(1, deck.Cards.Count(entry => entry.Card == curse && entry.Owner == other)); // 다른 소유자의 저주 보존 확인
        party.RestoreSavedAllyState(other, 0, 42, 1); // 남은 동료 사망 상태 설정
        Assert.IsFalse(BurntScarecrowEvent.TryResolve("dead", BurntScarecrowEvent.Investigate, other, out _)); // 사망자 조사와 비용 차단 확인
        party.RestoreSavedAllyState(actor, 4, 33, 0); // 마지막 생존자 체력 네 점 설정
        Assert.IsTrue(BurntScarecrowEvent.TryResolve("fatal", BurntScarecrowEvent.Burn, actor, out _)); // 마지막 생존자의 소각 실행
        Assert.IsTrue(session.IsExplorationCompleted); // 소각 비용으로 인한 전멸 정산 확인
    } // 소각 흐름 종료

    [UnityTest] // 실전 손패 저주 검사 지정
    public IEnumerator CurseRejectsHandAndControllerInputButDiscardsAndRedraws() // 사용 봉쇄와 턴 손패 순환 확인
    { // 전투 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyCurseInCombat(); // 손패와 전투 관리자 입력 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 검사 종료

    private static void VerifyCurseInCombat() // 저주가 있는 실제 전투 손패 조작
    { // 손패 흐름 시작
        var curse = WinterfallRiskCardReward.GetReward(FindRoom(true)); // 탐사 보상과 같은 저주 원본 조회
        var owner = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 출전 소유자 조회
        var ally = BattleUnitRuntime.CreateAlly(owner); // 아군 전투 상태 생성
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 독립 적 원본 생성
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData) }; // 살아 있는 적 한 명 준비
        var allies = new[] { ally }; // 아군 한 명 준비
        var source = DeckData.CreateRuntime(new[] { new DeckCardEntry(curse, owner) }); // 저주 카드가 있는 독립 덱 생성
        var deck = BattleDeckRuntime.Create(source, allies, 1, 210); // 손패 한 칸 전투 덱 생성
        var points = new BattleActionPointRuntime(3); // 사용 가능한 행동력 준비
        var canvas = new GameObject("CurseBattle", typeof(RectTransform), typeof(Canvas)); // 전투 화면 생성
        var handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 전투 화면과 손패 연결
        var hand = handObject.GetComponent<BattleHandView>(); // 실제 클릭 입력 조회
        var runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 사용 연출 조회
        try // 독립 전투 객체 정리 보장
        { // 카드 검사 시작
            using (var turns = new BattleTurnRuntime(deck, points, allies, enemies, 1, BattleType.Normal)) // 정상 전투 턴과 다음 드로우 구성
            using (var actions = new BattleEnemyActionRuntime(enemies, allies)) // 적 행동 연결
            { // 입력 검사 시작
                Assert.IsTrue(turns.StartBattle(1)); // 저주를 손에 든 플레이어 턴 시작
                Assert.IsTrue(hand.Bind(deck, points, turns)); // 카드 화면 입력 연결
                using (var controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new BattleUnitView[0], new BattleUnitView[0], actions)) // 실제 전투 행동 관리자 생성
                { // 저주 입력 검사 시작
                    CardInstance held = deck.Hand.Single(); // 손패 한 칸을 채운 저주 조회
                    var view = hand.GetComponentsInChildren<BattleCardView>().Single(item => item.RuntimeCard == held); // 표시된 저주 화면 조회
                    int clicks = 0; // 손패가 전달한 클릭 수
                    int plays = 0; // 실제 카드 사용 수
                    hand.CardClicked += item => clicks++; // 화면에서 전투에 전달한 클릭 집계
                    controller.CardUsed += (item, targets) => plays++; // 효과 처리로 간 사용 수 집계
                    view.OnPointerClick(null); // 저주를 화면에서 클릭
                    Assert.AreEqual(0, clicks); // 저주 클릭의 화면 차단 확인
                    typeof(BattleCardActionController).GetMethod("HandleCardClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { held }); // 관리자 입력 경계를 직접 호출
                    Assert.IsNull(controller.SelectedCard); // 직접 전달한 저주도 선택 해제 상태 확인
                    Assert.AreEqual(0, plays); // 직접 입력해도 카드 사용 신호 없음 확인
                    Assert.IsFalse(runner.IsBusy); // 저주 사용 연출 미실행 확인
                    Assert.AreEqual(3, points.CurrentActionPoints); // 비용 미소모 확인
                    Assert.AreSame(held, deck.Hand.Single()); // 사용 시도 후에도 손패 한 칸 차지 확인
                    Assert.IsTrue(deck.DiscardCard(held)); // 선택 버리기 원래 경로로 저주 이동
                    Assert.AreEqual(1, deck.DrawCards(1)); // 버린 저주를 다시 드로우
                    Assert.IsTrue(turns.EndPlayerTurn()); // 정상 턴 종료로 손패 정리
                    CollectionAssert.Contains(deck.DiscardPile, held); // 턴 종료 시에도 저주 버리기 확인
                    Assert.AreEqual(1, deck.DrawCards(1)); // 턴 종료 뒤 재셔플과 드로우 확인
                    Assert.AreSame(held, deck.Hand.Single()); // 같은 저주 인스턴스 보존 확인
                } // 저주 입력 검사 종료
                hand.Unbind(); // 손패 입력 연결 해제
            } // 입력 검사 종료
        } // 카드 검사 종료
        finally // 전투 객체 정리
        { // 정리 시작
            Object.Destroy(canvas); // 전투 UI 제거
            Object.Destroy(source); // 독립 덱 원본 제거
            Object.Destroy(enemyData); // 독립 적 원본 제거
        } // 정리 종료
    } // 손패 흐름 종료

    private static ExplorationSessionManager PrepareParty() // 실제 출전과 탐사 저장 구성
    { // 편성 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 출전 조건 준비
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 두 생존 파티원 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출발 편성과 시작 덱 생성
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 실제 회차 덱 구성
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 진행 상태 준비
        session.ResetExploration(); // 이전 방과 입력 초기화
        session.SetCurrentFloorSeed(210); // 재현 가능한 층 시드 적용
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 출전 파티 지속 상태 등록
        return session; // 준비한 탐사 반환
    } // 편성 종료

    private static void AssertState(CharacterData member, int health, int mental) // 실제 저장된 캐릭터 수치 확인
    { // 상태 비교 시작
        Assert.IsTrue(BattleResultManager.Instance.TryGetSavedAllyState(member, out int hp, out int mind, out _)); // 파티원 지속 상태 조회
        Assert.AreEqual(health, hp); // 체력 확인
        Assert.AreEqual(mental, mind); // 정신력 확인
    } // 상태 비교 종료

    private static void ClickInvestigate(ExplorationEventPanelView panel) // 실제 조사 버튼 입력
    { // 선택 버튼 입력 시작
        panel.SendMessage("PopulateEventContent"); // 그래픽 프레임 대기 없이 실제 선택 버튼 구성
        Button button = panel.GetComponentsInChildren<Button>(true).Last(item => item.name == "ChoiceButton" && item.GetComponentInChildren<TMP_Text>(true).text.StartsWith("조사한다")); // 현재 방에 생성된 조사 행동 버튼 조회
        button.onClick.Invoke(); // 실제 조사 선택
    } // 선택 버튼 입력 종료

    private static string FindRoom(bool wantsCurse) // 두 결과의 재현 가능한 방 시드 조회
    { // 고정 방 검색 시작
        for (int i = 0; i < 128; i++) // 유한한 수의 서로 다른 방 확인
        { // 방 결과 비교 시작
            string id = "scarecrow-210-" + i; // 독립 탐사 방 식별자 생성
            var card = WinterfallRiskCardReward.GetReward(id); // 실제 위험 보상 조회
            if (card != null && (card.CardId == WinterfallRiskCardReward.CurseCardId) == wantsCurse) // 요청한 실제 분기 확인
            { // 해당 방 반환 시작
                return id; // 검증할 고정 방 반환
            } // 해당 방 반환 종료
        } // 방 결과 비교 종료
        throw new AssertionException("희귀 카드 또는 저주 분기 누락"); // 실제 후보 부재 알림
    } // 고정 방 검색 종료
} // 검사 종료
