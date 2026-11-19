using System; // 독립 저장 경로 생성
using System.Collections; // 플레이 모드 대기
using System.IO; // 탐사 저장 파일 확인
using System.Linq; // 이벤트와 카드 목록 검사
using NUnit.Framework; // 결과 비교
using UnityEngine; // 실제 캐릭터와 탐사 씬 데이터 사용
using UnityEngine.TestTools; // 플레이 모드 전환
using UnityEngine.UI; // 파티원 선택 버튼 입력

public sealed class AshstormEyeEventTests // 재폭풍의 눈 통합 검사
{ // 이벤트 검사 시작
    [Test] // 이벤트 원본 검사 지정
    public void CatalogPreservesOriginalChoicesAndSelectsRegisteredRareReward() // 기존 이벤트와 신규 원문 및 희귀 보상 확인
    { // 원본 검사 시작
        var previous = ExplorationEventCatalog.LoadEvents(0); // 이전 저장에서 사용할 이벤트 조회
        CollectionAssert.AreEqual(new[] { "event_abandoned_crate", "event_damaged_terminal", "event_supply_cache" }, previous.Select(item => item.EventId)); // 이전 세 종류와 난수 선택 순서 확인
        var current = ExplorationEventCatalog.LoadEvents(1); // 재폭풍의 눈이 추가된 당시의 네 이벤트 조회
        Assert.AreEqual(previous.Count + 1, current.Count); // 기존 이벤트와 신규 이벤트 공존 확인
        ExplorationEventData eye = current.Single(item => item.EventId == AshstormEyeEvent.Id); // 실제 생성 가능한 재폭풍의 눈 조회
        Assert.IsNotNull(eye.IllustrationSprite); // 전용 삽화 연결 확인
        CollectionAssert.AreEqual(new[] { "응시한다", "돌로 깨뜨린다", "눈을 피한다" }, eye.Choices.Select(choice => choice.ChoiceText)); // 세 선택의 순서와 원문 확인
        ShopOfferData reward = AshstormEyeEvent.GetRareReward("EV_F1_X2_Y3_S201"); // 실제 희귀 카드 한 장 선택
        Assert.IsNotNull(reward); // 지급 가능한 카드 존재 확인
        Assert.IsTrue(reward.RequiresOwnerChoice); // 공용 카드 소유자 직접 선택 확인
        Assert.AreEqual("희귀", CardSheetCatalog.Load().FindByName(reward.Card.DisplayName).Rarity); // 원본 시트 희귀도 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards, reward.Card); // 저장 가능한 실물 카드 확인
        Assert.AreSame(reward, AshstormEyeEvent.GetRareReward("EV_F1_X2_Y3_S201")); // 재시도 시 동일 카드 확인
    } // 원본 검사 종료

    [UnityTest] // 이전 저장 호환 검사 지정
    public IEnumerator OlderSaveKeepsItsFloorEventsUntilDescent() // 구버전 저장의 현재 층과 다음 층 구분 확인
    { // 저장 호환 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string path = Path.Combine(Application.temporaryCachePath, "eye-legacy-" + Guid.NewGuid().ToString("N")); // 독립 탐사 저장 경로
        try // 독립 저장 정리 보장
        { // 구버전 복원 시작
            ExplorationSessionManager session = PrepareParty(); // 실제 파티와 회차 덱 설정
            string eventId = "EV_F1_X2_Y3_S201"; // 처리된 기존 이벤트 식별자
            Assert.IsTrue(session.MarkEventResolved(eventId)); // 이전 이벤트의 처리 상태 기록
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 실제 회차 저장 생성
            string modern = File.ReadAllText(path); // 최신 저장 내용 읽기
            string legacy = modern.Replace("\"eventPoolVersion\":" + ExplorationEventCatalog.CurrentVersion + ",", string.Empty); // 최신 저장에서 과거 형식처럼 버전 필드 생략
            Assert.AreNotEqual(modern, legacy); // 실제 버전 필드 제거 확인
            File.WriteAllText(path, legacy); // 이전 탐사 저장 재현
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 저장 복원 확인
            Assert.AreEqual(0, session.EventPoolVersion); // 이전 층의 이벤트 목록 보존 확인
            Assert.AreEqual(201, session.CurrentFloorSeed); // 기존 층 시드 보존 확인
            Assert.IsTrue(session.IsEventResolved(eventId)); // 기존 보상 중복 방지 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 이전 층 재저장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 재저장한 이전 층 복원 확인
            Assert.AreEqual(0, session.EventPoolVersion); // 재저장 후 목록 유지 확인
            session.AdvanceFloor(); // 신규 층으로 이동
            Assert.AreEqual(ExplorationEventCatalog.CurrentVersion, session.EventPoolVersion); // 새 층에서 재폭풍의 눈 허용 확인
        } // 구버전 복원 종료
        finally // 독립 파일 정리
        { // 파일 제거 시작
            ExplorationSaveService.Remove(path); // 독립 저장과 복구본 제거
        } // 파일 제거 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 저장 호환 검사 종료

    [UnityTest] // 비용과 보상 원자성 검사 지정
    public IEnumerator GazeCancelReloadAndChooseDifferentOwnerWithoutDoubleClaim() // 행동자와 소유자 분리 및 취소와 재지급 방지 확인
    { // 응시 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string path = Path.Combine(Application.temporaryCachePath, "eye-reward-" + Guid.NewGuid().ToString("N")); // 독립 탐사 저장 경로
        try // 독립 저장 정리 보장
        { // 응시 선택 검사 시작
            var session = PrepareParty(); // 출전 파티 두 명과 덱 준비
            var allies = BattleResultManager.Instance; // 현재 파티 상태 조회
            var actor = allies.ActiveParty.Members[0]; // 응시할 파티원 선택
            var owner = allies.ActiveParty.Members[1]; // 카드를 받을 다른 파티원 선택
            allies.RestoreSavedAllyState(actor, 12, 40, 0); // 행동자 초기 수치 지정
            allies.RestoreSavedAllyState(owner, 11, 30, 0); // 소유자 초기 수치 지정
            string id = "EV_F1_X2_Y3_S201"; // 독립 응시 방 식별자
            var card = AshstormEyeEvent.GetRareReward(id).Card; // 이 방의 보상 카드 조회
            int count = RunDeckManager.Instance.CardCount; // 보상 전 덱 장수 보관
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 선택 전 상태 저장
            var marker = new GameObject("AshstormEyeTest").AddComponent<ExplorationEventView>(); // 독립 이벤트 표식 생성
            var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == AshstormEyeEvent.Id); // 실제 배치 원본 조회
            marker.Initialize(data, id); // 표식에 재폭풍의 눈 연결
            var panel = ExplorationEventPanelView.EnsureInstance(); // 실제 이벤트 화면 준비
            panel.ShowEvent(marker, data, id); // 실제 이벤트 표시 요청
            panel.SendMessage("HandleChoiceSelected", (object)AshstormEyeEvent.Gaze); // 응시 선택 번호를 인수로 전달
            Canvas.ForceUpdateCanvases(); // 선택 창의 실제 배치 치수 갱신
            RectTransform selector = (RectTransform)panel.transform.Find("TreasureOwnerCanvas"); // 열린 행동자 선택 화면 조회
            Assert.AreEqual(((RectTransform)panel.transform).rect.size, selector.rect.size); // 행동자 선택 창과 배경의 화면 전체 배치 확인
            ClickMember(panel, actor); // 행동자 확정 후 보상 소유자 선택 진입
            ClickCancel(panel); // 카드 소유자 선택 취소
            AssertState(actor, 12, 40, 0); // 취소 후 정신력 유지 확인
            Assert.AreEqual(count, RunDeckManager.Instance.CardCount); // 취소 후 카드 미지급 확인
            Assert.IsFalse(session.IsEventResolved(id)); // 취소 후 미처리 유지 확인
            panel.SendMessage("HandleChoiceSelected", (object)AshstormEyeEvent.Gaze); // 응시 행동 번호로 재선택
            ClickMember(panel, actor); // 행동자 재선택
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 소유자 선택 중 이전 저장 복원
            Assert.IsFalse(ExplorationPlayerController.InputBlocked); // 불러온 탐사의 이동 잠금 해제 확인
            Assert.IsNull(panel.transform.Find("TreasureOwnerCanvas")); // 소유자 선택 자동 종료 확인
            AssertState(actor, 12, 40, 0); // 불러오기 후 미확정 비용 없음 확인
            Assert.IsFalse(session.IsEventResolved(id)); // 이전 저장의 미처리 상태 확인
            RunDeckManager.Instance.ResetToSource(null); // 지급할 수 없는 덱 상태 재현
            Assert.IsFalse(AshstormEyeEvent.TryResolve(id, AshstormEyeEvent.Gaze, actor, owner, out _)); // 카드 추가 실패 확인
            AssertState(actor, 12, 40, 0); // 카드 실패 시 정신력 보존 확인
            Assert.IsFalse(session.IsEventResolved(id)); // 카드 실패 시 재선택 가능 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 정상 덱 복원
            panel.ShowEvent(marker, data, id); // 복원 후 같은 이벤트 다시 열기
            panel.SendMessage("HandleChoiceSelected", (object)AshstormEyeEvent.Gaze); // 복원한 방에서 응시 행동 선택
            ClickMember(panel, actor); // 첫째를 행동자로 확정
            ClickMember(panel, owner); // 둘째를 보상 소유자로 확정
            Assert.AreEqual(count + 1, RunDeckManager.Instance.CardCount); // 카드 한 장 지급 확인
            Assert.AreSame(card, RunDeckManager.Instance.Cards.Last().Card); // 약속한 희귀 카드 확인
            Assert.AreSame(owner, RunDeckManager.Instance.Cards.Last().Owner); // 별도 소유자 확인
            AssertState(actor, 12, 32, 0); // 행동자만 정신력 여덟 감소 확인
            AssertState(owner, 11, 30, 0); // 보상 소유자 상태 유지 확인
            Assert.IsTrue(session.IsEventResolved(id)); // 이벤트 사용 완료 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 비용과 카드 및 완료 함께 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 지급 후 탐사 복원
            Assert.IsFalse(AshstormEyeEvent.TryResolve(id, AshstormEyeEvent.Gaze, actor, owner, out _)); // 복원 직후 중복 실행 차단
            Assert.AreEqual(count + 1, RunDeckManager.Instance.CardCount); // 카드 중복 미지급 확인
            Assert.AreEqual(owner.CharacterId, RunDeckManager.Instance.Cards.Last().Owner.CharacterId); // 획득 카드 소유자 복원 확인
            AssertState(actor, 12, 32, 0); // 복원 후 비용 중복 차감 방지 확인
            UnityEngine.Object.Destroy(marker.gameObject); // 독립 표식 제거
        } // 응시 선택 검사 종료
        finally // 독립 파일 정리
        { // 파일 제거 시작
            ExplorationSaveService.Remove(path); // 독립 저장과 복구본 제거
        } // 파일 제거 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 응시 검사 종료

    [UnityTest] // 생존과 전멸 결과 검사 지정
    public IEnumerator ShatterAndLookAwayAffectOneSurvivorAndSettleLastDeath() // 행동자 피해와 회복 및 마지막 사망 정산 확인
    { // 파괴와 회피 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string path = Path.Combine(Application.temporaryCachePath, "eye-shards-" + Guid.NewGuid().ToString("N")); // 독립 진행 저장 경로
        string lobby = path + ".lobby"; // 독립 종료 저장 경로
        try // 독립 파일 정리 보장
        { // 선택 결과 확인 시작
            var session = PrepareParty(); // 실제 두 명 파티 준비
            var allies = BattleResultManager.Instance; // 아군 상태 조회
            var first = allies.ActiveParty.Members[0]; // 돌을 던질 파티원 조회
            var second = allies.ActiveParty.Members[1]; // 다른 행동을 할 파티원 조회
            var resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 조회
            resources.ResetResources(); // 이전 자원 제거
            resources.AddResources(0, 4, 3, 0); // 탐사 전 거점 재화 지정
            resources.BeginExplorationResourceTracking(); // 탐사 획득 자원 별도 추적
            allies.RestoreSavedAllyState(first, 6, 33, 0); // 피해로 사망할 체력 설정
            allies.RestoreSavedAllyState(second, 10, 99, 0); // 생존자 상태 설정
            Assert.IsTrue(AshstormEyeEvent.TryResolve("break-first", AshstormEyeEvent.Shatter, first, null, out _)); // 첫째로 파괴 선택
            AssertState(first, 0, 33, 1); // 피해로 인한 사망 한 번 확인
            AssertState(second, 10, 99, 0); // 비행동자 상태 유지 확인
            Assert.AreEqual(5, resources.Screw); // 나사 한 개 지급 확인
            Assert.AreEqual(4, resources.IronPlate); // 철판 한 개 지급 확인
            Assert.IsFalse(session.IsExplorationCompleted); // 다른 생존자의 탐사 계속 확인
            Assert.IsFalse(AshstormEyeEvent.TryResolve("avoid", AshstormEyeEvent.LookAway, first, null, out _)); // 사망자 행동 차단 확인
            Assert.IsTrue(AshstormEyeEvent.TryResolve("avoid", AshstormEyeEvent.LookAway, second, null, out _)); // 둘째로 시선 회피
            AssertState(second, 10, 100, 0); // 행동자의 정신력 한 점 회복 확인
            Assert.IsTrue(AshstormEyeEvent.TryResolve("avoid-at-cap", AshstormEyeEvent.LookAway, second, null, out _)); // 정신력 상한에서 회피 선택
            AssertState(second, 10, 100, 0); // 최대 정신력 초과 방지 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 생존 중 탐사 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 자원과 사망 및 완료 상태 복원
            Assert.IsTrue(session.IsEventResolved("avoid")); // 회피 완료 상태 복원 확인
            Assert.IsFalse(AshstormEyeEvent.TryResolve("break-first", AshstormEyeEvent.Shatter, second, null, out _)); // 파괴 보상 반복 수령 차단 확인
            AssertState(first, 0, 33, 1); // 사망 수치 복원 확인
            AssertState(second, 10, 100, 0); // 회복 수치 복원 확인
            Assert.AreEqual(5, resources.Screw); // 획득 나사 복원 확인
            Assert.AreEqual(4, resources.IronPlate); // 획득 철판 복원 확인
            Assert.IsTrue(AshstormEyeEvent.TryResolve("break-second", AshstormEyeEvent.Shatter, second, null, out _)); // 둘째의 정상 피해 선택
            AssertState(second, 3, 100, 0); // 체력 일곱 감소 확인
            Assert.IsTrue(AshstormEyeEvent.TryResolve("break-last", AshstormEyeEvent.Shatter, second, null, out _)); // 마지막 생존자의 치명적 파괴 선택
            Assert.IsTrue(session.IsExplorationCompleted && !session.IsExplorationSuccess); // 실제 전멸 정산 확인
            AssertState(second, 0, 100, 1); // 마지막 사망 횟수 한 번 확인
            Assert.AreEqual(4, resources.Screw); // 전멸 시 탐사 나사 회수와 거점 나사 보존 확인
            Assert.AreEqual(3, resources.IronPlate); // 전멸 시 탐사 철판 회수와 거점 철판 보존 확인
            Assert.IsTrue(ExplorationAutoSave.SaveAfterBattle(Vector3.zero, path, lobby)); // 전멸 회복 대기와 거점 상태 저장
            Assert.IsFalse(File.Exists(path)); // 종료된 탐사 저장 제거 확인
            Assert.IsTrue(LobbySaveService.Load(lobby)); // 종료 거점 상태 복원
            Assert.Greater(CharacterRecoveryManager.EnsureInstance().GetRemainingRecoveryExpeditions(second), 0); // 마지막 사망자의 거점 회복 대기 확인
        } // 선택 결과 확인 종료
        finally // 독립 파일 정리
        { // 파일 제거 시작
            ExplorationSaveService.Remove(path); // 탐사 저장과 복구본 정리
            ExplorationSaveService.Remove(lobby); // 거점 저장과 복구본 정리
        } // 파일 제거 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 파괴와 회피 검사 종료

    private static void ClickMember(ExplorationEventPanelView panel, CharacterData character) // 실제 파티원 버튼 클릭
    { // 버튼 클릭 시작
        panel.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + character.CharacterId).GetComponent<Button>().onClick.Invoke(); // 선택 화면의 파티원 확정
    } // 버튼 클릭 종료

    private static void ClickCancel(ExplorationEventPanelView panel) // 실제 대상 선택 취소
    { // 취소 클릭 시작
        panel.transform.Find("TreasureOwnerCanvas/Panel/CancelButton").GetComponent<Button>().onClick.Invoke(); // 선택 화면에서 뒤로 이동
    } // 취소 클릭 종료

    private static void AssertState(CharacterData member, int health, int mental, int deaths) // 파티원 지속 상태 비교
    { // 상태 검사 시작
        Assert.IsTrue(BattleResultManager.Instance.TryGetSavedAllyState(member, out int savedHealth, out int savedMental, out int savedDeaths)); // 현재 지속 상태 읽기
        Assert.AreEqual(health, savedHealth, member.DisplayName); // 실제 체력 확인
        Assert.AreEqual(mental, savedMental, member.DisplayName); // 실제 정신력 확인
        Assert.AreEqual(deaths, savedDeaths, member.DisplayName); // 실제 사망 횟수 확인
    } // 상태 검사 종료

    private static ExplorationSessionManager PrepareParty() // 실제 파티 두 명과 탐사 상태 준비
    { // 파티 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 출전 제한 적용
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 출전 캐릭터 선택
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 둘째 출전 캐릭터 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출발 파티와 시작 덱 생성
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 덱 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 세션 준비
        session.ResetExploration(); // 이전 층과 완료 기록 정리
        session.SetCurrentFloorSeed(201); // 고정 층 시드 지정
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 아군 지속 상태 등록
        return session; // 준비된 탐사 세션 반환
    } // 파티 준비 종료
} // 이벤트 검사 종료
