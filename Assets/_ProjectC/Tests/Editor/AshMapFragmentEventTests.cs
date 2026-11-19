using System; // 독립 파일 경로와 대기 조건 사용
using System.Collections; // 플레이 모드 대기
using System.IO; // 실제 저장 확인
using System.Linq; // 후보 목록과 조우 조회
using NUnit.Framework; // 자동 검사 사용
using UnityEngine; // 탐사 오브젝트와 실제 데이터 사용
using UnityEngine.SceneManagement; // 실제 탐사와 전투 씬 확인
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 행동자 선택 입력
using Object = UnityEngine.Object; // 유니티 객체 조회

public sealed class AshMapFragmentEventTests // 지도 조각 이벤트 통합 검사
{ // 검사 시작
    private const string WinningSource = "EV_F1_X86_Y1_S208"; // 고정 추첨값 삼십구의 지도 조각 방
    private const string LosingSource = "EV_F1_X196_Y1_S208"; // 고정 추첨값 사십의 지도 조각 방

    [Test] // 세대별 후보 목록 확인
    public void VersionThreeAddsTheAuthoredMapFragmentWithoutChangingEarlierPools() // 이전 층과 새 원문 이벤트 확인
    { // 원본 검사 시작
        Assert.AreEqual(3, ExplorationEventCatalog.LoadEvents(0).Count); // 최초 세 후보 확인
        Assert.AreEqual(4, ExplorationEventCatalog.LoadEvents(1).Count); // 재폭풍까지 네 후보 확인
        var old = ExplorationEventCatalog.LoadEvents(2); // 잿더미까지의 이전 층 후보
        Assert.AreEqual(5, old.Count); // 다섯 후보 유지 확인
        Assert.IsFalse(old.Any(item => item.EventId == AshMapFragmentEvent.Id)); // 이전 저장의 지도 조각 미배치 확인
        var added = ExplorationEventCatalog.LoadEvents(3); // 지도 조각 도입 버전 조회
        CollectionAssert.AreEqual(old.Select(item => item.EventId).Append(AshMapFragmentEvent.Id).OrderBy(id => id, StringComparer.Ordinal), added.Select(item => item.EventId)); // 새 이벤트 한 개와 정렬 순서 확인
        var map = added.Single(item => item.EventId == AshMapFragmentEvent.Id); // 실제 원본 조회
        Assert.IsNotNull(map.IllustrationSprite); // 지도 조각 전용 삽화 확인
        CollectionAssert.AreEqual(new[] { "줍는다", "태운다", "찢어버린다" }, map.Choices.Select(item => item.ChoiceText)); // 원문 선택지 확인
    } // 원본 검사 종료

    [UnityTest] // 태우기와 찢기 및 이전 저장 확인
    public IEnumerator BurnCancelDeadOwnerTearAndLegacySaveKeepExactRewards() // 태우기 대상과 일회 지급 및 이전 저장 복원 확인
    { // 선택 결과 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 독립 저장 경로
        try // 독립 저장 정리 보장
        { // 태우기와 찢기 검사 시작
            var session = PrepareParty(); // 실제 두 명 파티와 탐사 덱 준비
            var allies = BattleResultManager.Instance; // 파티 지속 상태 조회
            var first = allies.ActiveParty.Members[0]; // 행동자 조회
            var other = allies.ActiveParty.Members[1]; // 비행동자 조회
            allies.RestoreSavedAllyState(first, 15, 97, 0); // 첫째 정신력 회복 여유 설정
            allies.RestoreSavedAllyState(other, 0, 30, 1); // 사망 파티원 지정
            var resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 조회
            resources.ResetResources(); // 획득 자원 기준 초기화
            var panel = ExplorationEventPanelView.EnsureInstance(); // 실제 이벤트 화면 생성
            var marker = new GameObject("MapFragmentTest").AddComponent<ExplorationEventView>(); // 독립 이벤트 표식 생성
            var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == AshMapFragmentEvent.Id); // 실제 원본 조회
            marker.Initialize(data, "burn"); // 태울 방 식별자 설정
            panel.ShowEvent(marker, data, "burn"); // 실제 선택지 열기
            panel.SendMessage("HandleChoiceSelected", (object)AshMapFragmentEvent.Burn); // 행동자 선택으로 이동
            Transform chooser = panel.transform.Find("TreasureOwnerCanvas/Panel"); // 실제 파티원 버튼 조회
            Assert.IsFalse(chooser.Find("Owner_" + other.CharacterId).GetComponent<Button>().interactable); // 사망자 선택 비활성 확인
            chooser.Find("CancelButton").GetComponent<Button>().onClick.Invoke(); // 선택 취소
            Assert.IsFalse(session.IsEventResolved("burn")); // 취소 후 방 미처리 확인
            Assert.AreEqual(0, resources.Screw); // 취소 후 나사 미지급 확인
            Assert.IsFalse(AshMapFragmentEvent.TryResolve("burn", AshMapFragmentEvent.Burn, other, out _)); // 사망자 직접 입력 차단
            panel.SendMessage("HandleChoiceSelected", (object)AshMapFragmentEvent.Burn); // 태우기 재선택
            panel.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + first.CharacterId).GetComponent<Button>().onClick.Invoke(); // 생존자 한 명에게 적용
            Assert.IsTrue(allies.TryGetSavedAllyState(first, out int hp, out int mental, out _)); // 실제 행동자 수치 읽기
            Assert.AreEqual(15, hp); // 행동자 체력 유지 확인
            Assert.AreEqual(99, mental); // 행동자 정신력 두 점 회복 확인
            Assert.AreEqual(1, resources.Screw); // 나사 한 개 지급 확인
            Assert.AreEqual(0, resources.IronPlate); // 철판 지급 없음 확인
            Assert.IsTrue(AshMapFragmentEvent.TryResolve("tear", AshMapFragmentEvent.Tear, null, out _)); // 대상 없는 찢기 선택
            Assert.IsTrue(string.IsNullOrEmpty(session.PendingMapFragmentId)); // 찢기 후 조각 보관 없음 확인
            Assert.AreEqual(1, resources.Screw); // 찢기 후 추가 보상 없음 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 선택 결과 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 태우기와 찢기 복원
            Assert.IsFalse(AshMapFragmentEvent.TryResolve("burn", AshMapFragmentEvent.Burn, first, out _)); // 동일 방 중복 지급 차단
            Assert.IsTrue(session.IsEventResolved("tear")); // 찢은 방 재등장 차단
            string modern = File.ReadAllText(path); // 최신 탐사 저장 읽기
            string legacy = modern.Replace("\"eventPoolVersion\":" + ExplorationEventCatalog.CurrentVersion + ",", "\"eventPoolVersion\":2,"); // 204일차 목록으로 변경
            legacy = System.Text.RegularExpressions.Regex.Replace(legacy, "\"(?:pendingMapFragment|avoidedEncounters)\":(?:null|\"\"|\\[\\]),", string.Empty); // 구버전에 없는 두 필드 제거
            Assert.AreNotEqual(modern, legacy); // 이전 형식 재현 확인
            File.WriteAllText(path, legacy); // 구버전 저장 파일 재현
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 버전 저장 복원 확인
            Assert.AreEqual(2, session.EventPoolVersion); // 이전 층의 다섯 후보 유지 확인
            Assert.IsTrue(string.IsNullOrEmpty(session.PendingMapFragmentId)); // 이전 저장의 미보유 기본 상태 확인
            Assert.IsEmpty(session.AvoidedEncounterIds); // 이전 저장의 미회피 상태 확인
            session.AdvanceFloor(); // 새 층 진입
            Assert.AreEqual(ExplorationEventCatalog.CurrentVersion, session.EventPoolVersion); // 새 층에서 지도 조각 허용 확인
            Object.Destroy(marker.gameObject); // 독립 표식 정리
        } // 태우기와 찢기 검사 종료
        finally // 독립 저장 파일 정리
        { // 파일 정리 시작
            ExplorationSaveService.Remove(path); // 탐사 저장과 복구본 제거
        } // 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 선택 결과 검사 종료

    [UnityTest] // 확률 경계와 복원 검사
    public IEnumerator NormalContactConsumesOnceAndOlderCheckpointsCannotReroll() // 특수 방 보존과 결과 고정 및 저장 실패 복구 검사
    { // 회피 판정 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        string before = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 조우 전 독립 저장
        string after = before + ".result"; // 조우 후 독립 저장
        try // 독립 저장 정리 보장
        { // 일회성 효과 검증 시작
            var session = PrepareParty(); // 새 회차 준비
            var normal = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 일반 조우 원본
            var elite = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshSentinel"); // 실제 정예 원본
            var boss = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 실제 보스 원본
            Assert.IsTrue(AshMapFragmentEvent.TryResolve(WinningSource, AshMapFragmentEvent.PickUp, null, out _)); // 성공 경계 조각 보관
            Assert.IsFalse(AshMapFragmentEvent.TryResolve("duplicate", AshMapFragmentEvent.PickUp, null, out _)); // 보관 중 추가 조각 중첩 차단
            Assert.IsFalse(session.IsEventResolved("duplicate")); // 다른 조각 방의 선택 보존 확인
            Assert.IsTrue(AshMapFragmentEvent.TryResolve("duplicate", AshMapFragmentEvent.Tear, null, out _)); // 기존 조각 유지하고 새 방 찢기
            Assert.AreEqual(MapFragmentContact.NoEffect, session.ResolveMapFragmentContact("elite", elite, Vector3.zero, after)); // 정예 접촉에서 효과 보존
            Assert.AreEqual(MapFragmentContact.NoEffect, session.ResolveMapFragmentContact("boss", boss, Vector3.zero, after)); // 보스 접촉에서 효과 보존
            Assert.AreEqual(WinningSource, session.PendingMapFragmentId); // 정예와 보스 뒤에도 같은 조각 보관
            Assert.IsTrue(ExplorationSaveService.Save(before, Vector3.right)); // 미사용 조각 수동 저장
            Assert.AreEqual(MapFragmentContact.SaveFailed, session.ResolveMapFragmentContact("normal", normal, Vector3.right, string.Empty)); // 잘못된 경로에서 접촉 저장 실패
            Assert.AreEqual(WinningSource, session.PendingMapFragmentId); // 저장 실패 시 미사용 조각 복구
            Assert.IsFalse(session.IsEncounterRemoved("normal")); // 저장 실패 시 조우 유지
            Assert.AreEqual(MapFragmentContact.Avoided, session.ResolveMapFragmentContact("normal", normal, Vector3.right, after)); // 삼십구에서 회피 성공
            Assert.IsTrue(ExplorationSaveService.Load(after)); // 회피 결과 자동 저장 복원
            Assert.IsTrue(string.IsNullOrEmpty(session.PendingMapFragmentId)); // 회피 후 조각 재생성 방지
            Assert.IsTrue(session.IsEncounterRemoved("normal")); // 통과한 조우 재등장 방지
            Assert.IsFalse(session.CanBeginEncounter("normal", normal)); // 동일 적과 재접촉 차단
            Assert.IsEmpty(session.ClearedEncounterIds); // 회피에 전투 승수 미부여 확인
            Assert.AreEqual(0, session.RunGoldGained); // 회피에 전투 골드 미지급 확인
            Assert.AreEqual(0, session.RunExperienceGained); // 회피에 전투 경험치 미지급 확인
            Assert.AreEqual(MapFragmentContact.NoEffect, session.ResolveMapFragmentContact("next", normal, Vector3.zero, after)); // 성공한 조각을 두 번째 조우에 사용 불가
            Assert.IsTrue(ExplorationSaveService.Load(before)); // 조우 전 보관 상태 복원
            Assert.AreEqual(MapFragmentContact.Avoided, session.ResolveMapFragmentContact("other-normal", normal, Vector3.right, after)); // 다른 경로에서도 같은 조각 재추첨 방지
            Assert.IsTrue(AshMapFragmentEvent.TryResolve(LosingSource, AshMapFragmentEvent.PickUp, null, out _)); // 실패 경계 조각 보관
            Assert.AreEqual(MapFragmentContact.Battle, session.ResolveMapFragmentContact("fight", normal, Vector3.right, after)); // 사십에서 회피 실패
            Assert.IsTrue(ExplorationSaveService.Load(after)); // 실패 직후 전투 전 저장 복원
            Assert.IsTrue(string.IsNullOrEmpty(session.PendingMapFragmentId)); // 실패한 조각 소비 유지
            Assert.IsTrue(session.CanBeginEncounter("fight", normal)); // 실패한 조우는 실제 전투 가능
            Assert.AreEqual(MapFragmentContact.NoEffect, session.ResolveMapFragmentContact("fight", normal, Vector3.right, after)); // 실패한 조우 재접촉에서 재추첨 차단
        } // 일회성 효과 검증 종료
        finally // 독립 저장 파일 정리
        { // 파일 정리 시작
            ExplorationSaveService.Remove(before); // 조우 전 저장 정리
            ExplorationSaveService.Remove(after); // 조우 후 저장 정리
        } // 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 회피 판정 검사 종료

    [UnityTest] // 실제 탐사 충돌과 전투 전환 검사
    public IEnumerator GeneratedNormalEncountersDisappearOnSuccessAndOpenBattleOnFailure() // 생성된 적의 물리 접촉으로 회피와 전투 확인
    { // 실제 접촉 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실제 기본 자동 저장 경로 격리 확인
        { // 사용자 저장 보호 시작
            Assert.Ignore("실제 조우 자동 저장이 분리된 ProjectCVerification 프로필에서 실행하세요."); // 일반 사용자 저장에 쓰는 검사 제외
        } // 사용자 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 실행 시작
        yield return CheckGeneratedEncounters(); // 도메인 재시작 뒤 실제 접촉 검사 생성
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 실제 접촉 검사 종료

    private static IEnumerator CheckGeneratedEncounters() // 플레이 모드에서 생성한 물리 접촉 검사
    { // 실제 방 이동 검사 시작
        var session = PrepareParty(); // 실제 출전 파티와 고정 층 시드 준비
        if (SceneFlowManager.Instance == null) // 실제 씬 전환 관리자 존재 확인
        { // 전환 관리자 준비 시작
            new GameObject("FragmentSceneFlow").AddComponent<SceneFlowManager>(); // 전투 전환을 담당할 원본 관리자 생성
        } // 전환 관리자 준비 종료
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 게임의 일반 경로로 실제 탐사 씬 이동
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == "30_Exploration" && Object.FindFirstObjectByType<ExplorationEncounterView>() != null && Object.FindFirstObjectByType<ExplorationPlayerController>() != null && !SceneFlowManager.Instance.IsLoadingScene, "탐사 씬과 실제 조우·플레이어 배치"); // 페이드와 방 생성 완료 대기
        var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 현재 실제 맵 조회
        var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 물리 이동 캐릭터 조회
        Assert.IsNotNull(map); // 맵 생성 확인
        Assert.IsNotNull(player); // 플레이어 생성 확인
        var rooms = map.CurrentMap.Cells.Where(cell => map.TryGetEncounterTypeAt(cell.Coordinate, out BattleType type) && type == BattleType.Normal).Take(2).ToArray(); // 실제 생성된 서로 다른 일반 전투 방 조회
        Assert.AreEqual(2, rooms.Length); // 두 번 접촉할 정상 방 확인
        string firstId = EncounterId(map, rooms[0]); // 첫 실제 조우 식별자
        string nextId = EncounterId(map, rooms[1]); // 둘째 실제 조우 식별자
        var firstMarker = GameObject.Find("Encounter_" + firstId); // 게임 맵에 생성된 첫 적 조회
        Assert.IsNotNull(firstMarker); // 첫 접촉 표식 존재 확인
        Assert.IsTrue(AshMapFragmentEvent.TryResolve(WinningSource, AshMapFragmentEvent.PickUp, null, out _)); // 첫 일반 전투 회피 성공 지도 준비
        player.Teleport(firstMarker.transform.position); // 플레이어 물리 몸체를 실제 적 표식으로 이동
        yield return WaitUntil(() => session.IsEncounterRemoved(firstId) && !map.HasEncounterAt(rooms[0].Coordinate), "첫 적과의 실제 회피 접촉"); // 물리 충돌과 맵 표식 정리 대기
        Assert.AreEqual("30_Exploration", SceneManager.GetActiveScene().name); // 회피 후 탐사 씬 유지 확인
        Assert.IsNull(session.ActiveEncounter); // 회피한 적의 전투 진입 없음 확인
        Assert.IsEmpty(session.ClearedEncounterIds); // 회피한 적 승수 미지급 확인
        Assert.IsNotNull(GameObject.Find("MapFragmentNotice")); // 실제 회피 화면 안내 확인
        map.CreateEncounterObject(rooms[0], Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter")); // 회피한 좌표에서 재생성 시도
        Assert.IsFalse(map.HasEncounterAt(rooms[0].Coordinate)); // 같은 적 재생성 차단 확인
        Assert.IsTrue(ExplorationSaveService.CanLoad(ExplorationSaveService.DefaultPath)); // 표식 제거 전 실제 자동 저장 확인
        var nextMarker = GameObject.Find("Encounter_" + nextId); // 두 번째 실제 적 표식 조회
        Assert.IsNotNull(nextMarker); // 두 번째 접촉 표식 존재 확인
        Assert.IsTrue(AshMapFragmentEvent.TryResolve(LosingSource, AshMapFragmentEvent.PickUp, null, out _)); // 실패 경계 지도 준비
        player.Teleport(nextMarker.transform.position); // 두 번째 실제 일반 전투에 접촉
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == "40_Battle", "실패 후 실제 전투 진입"); // 일반 전투 씬 로드 대기
        Assert.AreEqual(nextId, session.ActiveRuntimeEncounterId); // 회피에 실패한 실제 적과 전투 확인
        Assert.IsTrue(string.IsNullOrEmpty(session.PendingMapFragmentId)); // 실제 실패 접촉 후 조각 소비 확인
        Assert.IsFalse(session.IsEncounterRemoved(nextId)); // 전투가 필요한 적 승리 처리 없음 확인
    } // 실제 방 이동 검사 종료

    private static string EncounterId(ExplorationMapRuntime map, ExplorationMapCell room) // 층과 좌표 및 시드의 조우 식별자
    { // 식별자 조립 시작
        return $"F{map.CurrentFloor}_X{room.Coordinate.x}_Y{room.Coordinate.y}_S{map.CurrentMap.Seed}"; // 실제 맵이 사용하는 조우 식별자 반환
    } // 식별자 조립 종료

    private static IEnumerator WaitUntil(Func<bool> ready, string step) // 물리 접촉과 씬 전환 시간 제한
    { // 화면 대기 시작
        float limit = Time.realtimeSinceStartup + 15f; // 대기 종료 시각
        while (!ready() && Time.realtimeSinceStartup < limit) // 실제 게임 진행 조건 확인
        { // 다음 프레임 대기 시작
            yield return null; // 물리와 씬 전환 진행
        } // 다음 프레임 대기 종료
        Assert.IsTrue(ready(), step); // 실제 접촉 또는 씬 전환 완료 확인
    } // 화면 대기 종료

    private static ExplorationSessionManager PrepareParty() // 독립 탐사 편성과 덱 준비
    { // 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 정상 출전 조건 적용
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 출전자 선택
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 둘째 출전자 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 캐릭터 파티와 덱 생성
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 덱 연결
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 세션 조회
        session.ResetExploration(); // 이전 완료 및 회피 기록 초기화
        session.SetCurrentFloorSeed(208); // 지도 조각 검증 층 시드 지정
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 파티 지속 상태 등록
        return session; // 정상 탐사 세션 반환
    } // 준비 종료
} // 검사 종료
