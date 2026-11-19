using System; // 고유 저장 경로 생성
using System.Collections; // 플레이 모드 대기
using System.IO; // 독립 저장 파일 확인
using System.Linq; // 후보 목록과 보상 비교
using NUnit.Framework; // 자동 검사 사용
using UnityEngine; // 파티와 시트 원본 조회
using UnityEngine.TestTools; // 플레이 모드 검사 지정
using UnityEngine.UI; // 행동자 선택 버튼 사용

public sealed class AshPileEventTests // 잿더미 탐사 선택 검사
{ // 검사 시작
    [Test] // 목록 호환 검사 지정
    public void CatalogKeepsBothEarlierVersionsAndAddsAshPileOnlyToNewFloors() // 세 버전의 고정 후보 순서 확인
    { // 원본 검사 시작
        string[] original = { "event_abandoned_crate", "event_damaged_terminal", "event_supply_cache" }; // 최초 세 테스트 이벤트 목록
        CollectionAssert.AreEqual(original, ExplorationEventCatalog.LoadEvents(0).Select(item => item.EventId)); // 최초 층 후보 순서 확인
        CollectionAssert.AreEqual(original.Concat(new[] { AshstormEyeEvent.Id }), ExplorationEventCatalog.LoadEvents(1).Select(item => item.EventId)); // 재폭풍의 눈 버전의 네 후보 고정 확인
        var current = ExplorationEventCatalog.LoadEvents(2); // 잿더미가 추가된 시점의 층 후보 조회
        Assert.AreEqual(5, current.Count); // 204일차의 다섯 후보 확인
        Assert.IsTrue(ExplorationEventCatalog.LoadEvents().Any(item => item.EventId == AshPileEvent.Id)); // 최신 층에도 잿더미 포함 확인
        var ash = current.Single(item => item.EventId == AshPileEvent.Id); // 신규 잿더미 원본 확인
        Assert.IsNotNull(ash.IllustrationSprite); // 관련 이벤트 삽화 연결 확인
        CollectionAssert.AreEqual(new[] { "맨손으로 뒤진다", "도구를 사용한다", "덮어둔다" }, ash.Choices.Select(item => item.ChoiceText)); // 원문 행동과 순서 확인
    } // 원본 검사 종료

    [UnityTest] // 확률 비용과 보상 검사 지정
    public IEnumerator SearchesPayOnceOnSuccessAndFailureAndPersistTheRelic() // 확률 경계와 미보유 유물 및 비용 저장 확인
    { // 확률 선택 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string path = Path.Combine(Application.temporaryCachePath, "ash-find-" + Guid.NewGuid().ToString("N")); // 독립 탐사 저장 경로
        try // 독립 저장 정리 보장
        { // 확률 검증 시작
            var session = PrepareParty(); // 두 생존자와 덱 준비
            var allies = BattleResultManager.Instance; // 지속 상태 조회
            var actor = allies.ActiveParty.Members[0]; // 맨손 조사 생존자 선택
            var other = allies.ActiveParty.Members[1]; // 비행동자 조회
            allies.RestoreSavedAllyState(actor, 25, 40, 0); // 두 차례 피해 가능한 체력 지정
            allies.RestoreSavedAllyState(other, 16, 29, 0); // 비행동자 기준 상태 설정
            var resources = PlayerResourceManager.EnsureInstance(); // 자원 보관함 조회
            resources.ResetResources(); // 철판 없는 상태 준비
            var relics = RelicRunManager.EnsureInstance(); // 유물 보관함 준비
            var registered = RegisteredRelics(); // 실제 시트 유물 후보 조회
            Assert.Greater(registered.Length, 2); // 두 보상과 기존 보유 후보 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(registered[0])); // 기존 보유 시트 유물 한 개 지정
            string handWin = IdForRoll(39); // 맨손 성공 경계 방
            string handLoss = IdForRoll(40); // 맨손 실패 경계 방
            string toolWin = IdForRoll(69); // 도구 성공 경계 방
            string toolLoss = IdForRoll(70); // 도구 실패 경계 방
            Assert.IsFalse(AshPileEvent.TryResolve(toolWin, AshPileEvent.UseTool, null, out _, out _)); // 철판 부족 행동 차단 확인
            Assert.IsFalse(session.IsEventResolved(toolWin)); // 부족한 비용에서 미사용 상태 확인
            Assert.IsTrue(AshPileEvent.TryResolve(handLoss, AshPileEvent.BareHands, actor, out string failureStory, out string failure)); // 맨손 실패도 행동 완료 확인
            Assert.IsFalse(failureStory.Contains("쥐어져")); // 미획득 결과에서 발견 문장 제외 확인
            StringAssert.Contains("없음", failure); // 획득 실패 안내 확인
            AssertState(actor, 19, 40, 0); // 실패 시에도 체력 여섯 차감 확인
            Assert.IsTrue(AshPileEvent.TryResolve(handWin, AshPileEvent.BareHands, actor, out _, out _)); // 맨손 성공 경계 판정 확인
            AssertState(actor, 13, 40, 0); // 성공 시 체력 여섯 차감 확인
            AssertState(other, 16, 29, 0); // 비행동자 피해 없음 확인
            Assert.AreEqual(2, relics.Inventory.Count); // 맨손 성공에서만 미보유 유물 한 개 지급 확인
            resources.AddResources(0, 0, 2, 0); // 두 도구 조사 비용 지급
            Assert.IsTrue(AshPileEvent.TryResolve(toolLoss, AshPileEvent.UseTool, null, out _, out _)); // 도구 실패 경계 처리 확인
            Assert.AreEqual(1, resources.IronPlate); // 실패해도 철판 한 개 소모 확인
            Assert.IsTrue(AshPileEvent.TryResolve(toolWin, AshPileEvent.UseTool, null, out _, out _)); // 도구 성공 경계 처리 확인
            Assert.AreEqual(0, resources.IronPlate); // 성공 시 철판 한 개 소모 확인
            AssertState(actor, 13, 40, 0); // 도구 사용 후 첫째 체력 유지 확인
            AssertState(other, 16, 29, 0); // 도구 사용 후 둘째 체력 유지 확인
            Assert.AreEqual(3, relics.Inventory.Count); // 중복 없는 유물 두 개 지급 확인
            string[] awarded = relics.Inventory.OwnedRelics.Select(item => item.RelicId).ToArray(); // 지급 순서 보관
            Assert.AreEqual(3, awarded.Distinct().Count()); // 중복 유물 부재 확인
            Assert.IsTrue(relics.Inventory.OwnedRelics.All(registered.Contains)); // 시트와 저장에 등록된 실제 유물 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 확정된 상태 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이벤트 결과 복원
            CollectionAssert.AreEqual(awarded, relics.Inventory.OwnedRelics.Select(item => item.RelicId)); // 유물과 순서 복원 확인
            Assert.IsFalse(AshPileEvent.TryResolve(toolWin, AshPileEvent.UseTool, null, out _, out _)); // 복원 후 같은 방 재획득 차단 확인
            AssertState(actor, 13, 40, 0); // 저장된 맨손 비용 확인
            Assert.AreEqual(0, resources.IronPlate); // 저장된 도구 비용 확인
            resources.AddResources(0, 0, 1, 0); // 도구 비용 한 개 준비
            resources.BeginExplorationResourceTracking(); // 철판을 탐사 전 보유분으로 지정
            int gold = resources.Gold; // 중복 유물 환전 여부 확인용 기준 골드
            string lostReward = IdForRoll(68); // 지급 직전 후보가 바뀔 성공 방
            Action takeRemaining = () => // 비용 변경 알림 중 보관함 변경 재현
            { // 외부 유물 획득 시작
                foreach (var relic in registered.Where(item => !relics.Inventory.ContainsRelic(item.RelicId))) // 남은 실제 시트 유물 순회
                { // 보관함 채우기 시작
                    Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(relic)); // 후보를 다른 획득 경로에서 선점
                } // 보관함 채우기 종료
            }; // 외부 유물 획득 종료
            resources.ResourcesChanged += takeRemaining; // 비용 차감 순간 외부 보관함 변경 연결
            try // 검사 전용 알림 정리 보장
            { // 지급 충돌 검사 시작
                Assert.IsFalse(AshPileEvent.TryResolve(lostReward, AshPileEvent.UseTool, null, out _, out _)); // 이미 지급된 유물의 이중 획득 차단
            } // 지급 충돌 검사 종료
            finally // 검사 전용 연결 해제
            { // 알림 해제 시작
                resources.ResourcesChanged -= takeRemaining; // 외부 보관함 변경 검사 종료
            } // 알림 해제 종료
            Assert.AreEqual(1, resources.IronPlate); // 전달 실패 후 실제 철판 반환 확인
            Assert.AreEqual(0, resources.ExplorationIronPlate); // 환불한 기존 철판이 탐사 획득으로 바뀌지 않음 확인
            Assert.AreEqual(gold, resources.Gold); // 실패한 유물의 중복 골드 미지급 확인
            Assert.IsFalse(session.IsEventResolved(lostReward)); // 실제 지급 실패 시 선택 취소 확인
            Assert.IsFalse(AshPileEvent.TryResolve("all-owned", AshPileEvent.UseTool, null, out _, out _)); // 모든 후보 보유 시 비용 없는 선택 보류 확인
            Assert.AreEqual(1, resources.IronPlate); // 보상 후보 없음 상태의 철판 보존 확인
            Assert.IsFalse(session.IsEventResolved("all-owned")); // 유물 후보 소진 후 다른 선택 가능 확인
        } // 확률 검증 종료
        finally // 독립 저장 정리
        { // 파일 정리 시작
            ExplorationSaveService.Remove(path); // 독립 탐사 저장과 복구본 제거
        } // 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 확률 선택 검사 종료

    [UnityTest] // 취소 복원과 치명 피해 검사 지정
    public IEnumerator CancelCoverFatalSearchAndLegacySavePreserveTheCorrectState() // 취소와 정신력 상한 및 마지막 생존자 사망 확인
    { // 선택 상태 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string path = Path.Combine(Application.temporaryCachePath, "ash-choice-" + Guid.NewGuid().ToString("N")); // 독립 탐사 저장 경로
        string lobby = path + ".lobby"; // 독립 종료 거점 저장 경로
        try // 독립 저장 정리 보장
        { // 선택 검증 시작
            var session = PrepareParty(); // 실제 두 생존자 준비
            var allies = BattleResultManager.Instance; // 저장 파티 상태 조회
            var first = allies.ActiveParty.Members[0]; // 첫째 조사자 지정
            var second = allies.ActiveParty.Members[1]; // 둘째 조사자 지정
            allies.RestoreSavedAllyState(first, 5, 40, 0); // 첫째의 치명 피해 조건 설정
            allies.RestoreSavedAllyState(second, 6, 96, 0); // 둘째의 정신력 회복과 치명 피해 조건 설정
            string id = IdForRoll(40); // 맨손 실패 경계 고정 방
            var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == AshPileEvent.Id); // 실제 잿더미 원본 조회
            var marker = new GameObject("AshPileTest").AddComponent<ExplorationEventView>(); // 독립 이벤트 표식 생성
            marker.Initialize(data, id); // 원본과 방 식별자 연결
            var panel = ExplorationEventPanelView.EnsureInstance(); // 실제 탐사 이벤트 패널 준비
            panel.ShowEvent(marker, data, id); // 잿더미 열기
            panel.SendMessage("HandleChoiceSelected", (object)AshPileEvent.BareHands); // 맨손 행동자 선택 열기
            panel.transform.Find("TreasureOwnerCanvas/Panel/CancelButton").GetComponent<Button>().onClick.Invoke(); // 실제 취소 버튼 입력
            Assert.IsFalse(session.IsEventResolved(id)); // 취소한 잿더미 미사용 확인
            AssertState(first, 5, 40, 0); // 취소 후 체력과 정신력 보존 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 취소 직후 상태 저장
            panel.SendMessage("HandleChoiceSelected", (object)AshPileEvent.Cover); // 덮기 행동자 선택 진입
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 선택 중 저장 복원
            Assert.IsFalse(ExplorationPlayerController.InputBlocked); // 이전 대상 선택의 이동 잠금 해제 확인
            Assert.IsFalse(session.IsEventResolved(id)); // 복원 후 재선택 가능 확인
            Assert.IsTrue(AshPileEvent.TryResolve("cover", AshPileEvent.Cover, second, out _, out _)); // 선택 생존자 정신력 회복
            AssertState(second, 6, 99, 0); // 정신력 세 점 회복 확인
            AssertState(first, 5, 40, 0); // 비행동자 정신력 유지 확인
            Assert.IsTrue(AshPileEvent.TryResolve("cover-again", AshPileEvent.Cover, second, out _, out _)); // 상한 근처 두 번째 회복
            AssertState(second, 6, 100, 0); // 정신력 상한 확인
            Assert.IsTrue(AshPileEvent.TryResolve(id, AshPileEvent.BareHands, first, out _, out _)); // 첫째의 치명 피해 선택
            AssertState(first, 0, 40, 1); // 사망과 횟수 한 번 증가 확인
            Assert.IsFalse(session.IsExplorationCompleted); // 둘째 생존 중 탐사 지속 확인
            Assert.IsFalse(AshPileEvent.TryResolve("dead-actor", AshPileEvent.Cover, first, out _, out _)); // 사망자의 회복 행동 차단 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 사망과 잿더미 처리 기록 저장
            string modern = File.ReadAllText(path); // 현재 층 저장 읽기
            string legacy = modern.Replace("\"eventPoolVersion\":" + ExplorationEventCatalog.CurrentVersion + ",", "\"eventPoolVersion\":1,"); // 재폭풍의 눈 시점 목록 버전 재현
            Assert.AreNotEqual(modern, legacy); // 실제 버전 교체 확인
            File.WriteAllText(path, legacy); // 이전 층 저장 기록 지정
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 층 저장 복원
            Assert.AreEqual(1, session.EventPoolVersion); // 복원한 층의 네 후보 유지 확인
            Assert.IsTrue(session.IsEventResolved(id)); // 동일 방의 사용 완료 상태 보존 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 이전 층 다시 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 버전 재복원
            Assert.AreEqual(1, session.EventPoolVersion); // 재저장 후에도 이전 이벤트 고정 확인
            session.AdvanceFloor(); // 신규 층 진입
            Assert.AreEqual(ExplorationEventCatalog.CurrentVersion, session.EventPoolVersion); // 이후 층에서만 잿더미 허용 확인
            session.SetCurrentFloorSeed(205); // 새 층의 실제 탐사 시드 지정
            Assert.IsTrue(AshPileEvent.TryResolve(IdForRoll(41), AshPileEvent.BareHands, second, out _, out _)); // 마지막 생존자 치명 피해 처리
            AssertState(second, 0, 100, 1); // 마지막 생존자의 사망 횟수 확인
            Assert.IsTrue(session.IsExplorationCompleted && !session.IsExplorationSuccess); // 마지막 생존자 사망으로 탐사 실패 확인
            Assert.IsTrue(ExplorationAutoSave.SaveAfterBattle(Vector3.zero, path, lobby)); // 기존 전멸과 동일한 회복 대기 정산
            Assert.IsTrue(LobbySaveService.Load(lobby)); // 전멸 거점 저장 복원
            Assert.Greater(CharacterRecoveryManager.EnsureInstance().GetRemainingRecoveryExpeditions(second), 0); // 마지막 사망자의 거점 회복 대기 확인
            Assert.IsFalse(File.Exists(path)); // 완료 탐사 저장 제거 확인
            UnityEngine.Object.Destroy(marker.gameObject); // 독립 표식 정리
        } // 선택 검증 종료
        finally // 독립 파일 정리
        { // 파일 제거 시작
            ExplorationSaveService.Remove(path); // 탐사 저장과 복구본 제거
            ExplorationSaveService.Remove(lobby); // 거점 저장과 복구본 제거
        } // 파일 제거 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 선택 상태 검사 종료

    private static RelicData[] RegisteredRelics() // 저장과 원본 시트가 일치하는 실제 상품 목록
    { // 유물 원본 조회 시작
        var saved = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 가능한 원본 목록
        var shop = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상품 조회
        var sheet = RelicSheetCatalog.Load(); // 유물 기획 원본 조회
        return shop.Offers.Where(item => item.Relic != null && saved.Relics.Contains(item.Relic) && sheet.FindByName(item.Relic.DisplayName) != null).Select(item => item.Relic).Distinct().ToArray(); // 실제 보상 검증 원본 반환
    } // 유물 원본 조회 종료

    private static string IdForRoll(int wanted) // 확률 경계를 재현하는 고정 방 식별자 조회
    { // 검증 방 검색 시작
        for (int i = 0; i < 10000; i++) // 독립 탐사 식별자 후보 순회
        { // 시드 검사 시작
            string id = "EV_F1_X" + i + "_Y2_S204"; // 실제 방과 같은 형식의 식별자 생성
            int hash = 17; // 기존 보물 고정 해시 시작값
            foreach (char letter in id) // 식별자 문자 순회
            { // 해시 누적 시작
                hash = unchecked(hash * 31 + letter); // 문자열 고정 해시 계산
            } // 해시 누적 종료
            if (new System.Random(hash).Next(100) == wanted) // 원하는 확률 경계 확인
            { // 경계 방 선택 시작
                return id; // 재현 가능한 경계 방 반환
            } // 경계 방 선택 종료
        } // 시드 검사 종료
        throw new AssertionException("확률 경계에 해당하는 방을 찾지 못했습니다."); // 검증 입력 누락 표시
    } // 검증 방 검색 종료

    private static ExplorationSessionManager PrepareParty() // 생존 파티 두 명과 회차 덱 준비
    { // 탐사 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 정상 출전 조건 적용
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 출전 캐릭터 등록
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 둘째 출전 캐릭터 등록
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출발 파티와 덱 준비
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 덱 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 세션 준비
        session.ResetExploration(); // 이전 방 처리 상태 정리
        session.SetCurrentFloorSeed(204); // 고정 층 시드 설정
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 지속 아군 상태 연결
        return session; // 새 탐사 세션 반환
    } // 탐사 준비 종료

    private static void AssertState(CharacterData member, int health, int mental, int deaths) // 선택한 파티원의 지속 상태 확인
    { // 지속 상태 확인 시작
        Assert.IsTrue(BattleResultManager.Instance.TryGetSavedAllyState(member, out int hp, out int mind, out int count)); // 체력과 정신력 및 사망 횟수 조회
        Assert.AreEqual(health, hp, member.DisplayName); // 실제 체력 확인
        Assert.AreEqual(mental, mind, member.DisplayName); // 실제 정신력 확인
        Assert.AreEqual(deaths, count, member.DisplayName); // 실제 사망 횟수 확인
    } // 지속 상태 확인 종료
} // 검사 종료
