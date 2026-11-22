using System; // 분리한 검증 파일의 고유 이름 사용
using System.Collections; // 실제 플레이 모드와 게임 프레임 대기
using System.Collections.Generic; // 실제 지역 사건 선택지 제작
using System.IO; // 별도 탐사 저장 파일의 실제 왕복
using System.Linq; // 실제 추가 보상 버튼과 선택 문구 조회
using System.Reflection; // 정상 사건 선택 콜백 호출
using NUnit.Framework; // 실제 지급과 저장 복원 수치 비교
using TMPro; // 실제 선택 버튼 표시 내용 조회
using UnityEngine; // 실제 지역 원본과 플레이어 생성
using UnityEngine.TestTools; // 현재 실제 게임 진행 환경 진입
using UnityEngine.UI; // 실제 복원 선택 창 지급 버튼 입력
using Object = UnityEngine.Object; // 격리 게임 원본 정리

public sealed class WinterfallRegionalRewardsRelicTests // 사막 보상과 벙커 함정 피해의 실제 탐사 검사
{ // 실제 상황의 한정 회귀 검사 시작
    [UnityTest] // 원래 사건 진행과 보상 선택을 저장 복원 후 실제 지갑에서 검사
    public IEnumerator DesertEventBonusKeepsItsNativeOutcomeAndIsClaimedOnlyOnceAcrossSaveReload() // 실제 이벤트 ID와 대기 및 지급 보존 확인
    { // 원래 게임의 격리 저장 검사 시작
        RequireVerificationProfile(); // 실제 사용자의 저장 폴더와 분리
        yield return new EnterPlayMode(); // 실제 탐사와 이벤트 버튼 재개 환경 진입
        string path = Path.Combine(Application.temporaryCachePath, "day278-" + Guid.NewGuid().ToString("N") + ".run"); // 독립 수동 검증 저장 위치
        var session = PrepareRun(); // 실제 파티와 덱이 구성된 지역 사건 회차 시작
        var resources = PlayerResourceManager.EnsureInstance(); // 실제 게임 골드와 제작 자원 조회
        var player = new GameObject("Day278Player", typeof(Rigidbody2D), typeof(ExplorationPlayerController)); // 실제 결과 자동 저장 위치의 탐사 플레이어
        var worldEvent = new GameObject("Day278Event", typeof(ExplorationEventView)); // 실제 탐사 이벤트 원본 연결 대상
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 시트에 신규 지역 사건을 추가하지 않는 런타임 검사 원본
        try // 분리한 테스트 데이터 정리를 보장
        { // 한 회차의 미지급과 지급 저장을 실제 경로로 왕복
            Acquire("BuriedCityCrest"); // 출시 묻힌 도시의 문장의 실제 보유
            const string runtimeId = "278-real-desert-reward"; // 한 번의 실제 방 진행을 나타낼 고정 식별자
            var originalChange = new ExplorationEventResourceChange(); // 실제 사건이 원래 한 번 주는 자원 정의
            originalChange.Initialize(7, 0, 0, 0); // 새 유물과 무관한 기존 사건 골드 지급 설정
            var choice = new ExplorationEventChoiceData(); // 실제 원본의 기존 행동 하나
            choice.Initialize("기존 보상 확정", "원래 사건 결과", originalChange); // 실제 결과 문구와 자원 지급 연결
            var choices = new List<ExplorationEventChoiceData> { choice }; // 플레이어가 실행할 실제 원본 선택지
            data.Initialize("278-desert-test", "지역 보상 검사", "지역 보상 원본 검사", ExplorationEventCategory.Reward, null, choices, ExplorationRegion.Fortress); // 잘못된 성채 지역의 비교 원본 생성
            Assert.IsNull(ExplorationRegionalRewards.BeginEvent(data, "278-fortress")); // 동일 보상 원본의 성채 지역은 문장에 비활성 확인
            data.Initialize("278-desert-test", "지역 보상 검사", "지역 보상 원본 검사", ExplorationEventCategory.Reward, null, choices, ExplorationRegion.Desert); // 명시적 사막을 가진 실제 사건 원본
            string emptyRoom = "278-desert-no-reward"; // 보상을 주지 않은 실제 완료 방의 검증용 식별자
            var beforeEmpty = ExplorationRegionalRewards.BeginEvent(data, emptyRoom); // 실제 사막이나 아직 보상을 지급하지 않은 상태
            Assert.IsTrue(session.MarkEventResolved(emptyRoom)); // 아무 지급 없이 끝난 원래 방 완료
            ExplorationRegionalRewards.FinishEvent(emptyRoom, beforeEmpty); // 보상이 실제로 없는 선택 결과 비교
            Assert.IsNull(session.PendingBuriedCityCrestEventId); // 완료된 장소만으로 허위 추가 보상 없음 확인
            var eventView = worldEvent.GetComponent<ExplorationEventView>(); // 실제 접촉한 사건의 표시 입력 원본
            eventView.Initialize(data, runtimeId); // 원래 런타임 방과 사건 원본의 정상 연결
            var panel = ExplorationEventPanelView.EnsureInstance(); // 실제 탐사 결과 및 보상 화면 조회
            panel.ShowEvent(eventView, data, runtimeId); // 정상 상태 초기화와 기존 게임 입력 잠금
            int goldBefore = resources.Gold; // 정상 선택 전에 현재 실제 지갑 보관
            var select = typeof(ExplorationEventPanelView).GetMethod("HandleChoiceSelected", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 선택 버튼이 실행하는 원래 콜백 조회
            Assert.IsNotNull(select); // 실제 런타임 콜백 유효성 확인
            select.Invoke(panel, new object[] { 0 }); // 원래 사건 선택 입력을 실제 처리 경로로 실행
            Assert.AreEqual(goldBefore + 7, resources.Gold); // 원래 사건의 골드만 먼저 입금 확인
            Assert.IsTrue(session.IsEventResolved(runtimeId)); // 같은 원래 식별자의 사건이 실제 완료됐는지 확인
            Assert.AreEqual(runtimeId, session.PendingBuriedCityCrestEventId); // 실제 원래 사건에 선택 기회 대기 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 정상 사건 결과와 추가 보상 대기를 같은 실제 파일로 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 열려 있던 원래 창을 닫으며 저장 상태 실제 복원
            Assert.AreEqual(runtimeId, session.PendingBuriedCityCrestEventId); // 같은 완료 사건의 선택 대기 복원 확인
            Assert.IsTrue(ExplorationEventPanelView.TryResumeRegionalReward()); // 탐사 씬 진입이 호출하는 실제 복원 모달 열기
            var rewardButton = panel.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<TMP_Text>().text == ExplorationRegionalRewards.GetOptionText(ExplorationRegionalRewards.Option.IronPlate)); // 실제 표시된 철판 선택 버튼 조회
            int ironBefore = resources.IronPlate; // 실제 기존 구성의 철판 수량 보관
            rewardButton.onClick.Invoke(); // 복원된 모달의 실제 보상 선택 입력
            Assert.AreEqual(ironBefore + ExplorationTreasureRewardService.ResourceRewardAmount, resources.IronPlate); // 지급된 실제 한 가지 철판 보상 확인
            Assert.AreEqual(goldBefore + 7, resources.Gold); // 선택하지 않은 별도 골드 보상 미지급 확인
            Assert.IsTrue(session.BuriedCityCrestRewardClaimed); // 현재 자원과 같은 시점의 일회 지급 기록 확인
            rewardButton.onClick.Invoke(); // 원래 버튼의 늦은 중복 이벤트를 다시 전달
            Assert.AreEqual(ironBefore + ExplorationTreasureRewardService.ResourceRewardAmount, resources.IronPlate); // 늦은 입력에 두 번째 지급 없음 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero) && ExplorationSaveService.Load(path)); // 현재 실제 입금과 사용 여부를 같은 파일로 왕복
            Assert.IsTrue(session.BuriedCityCrestRewardClaimed); // 저장 복원한 실제 사용 기록 확인
            Assert.IsFalse(ExplorationEventPanelView.TryResumeRegionalReward()); // 이미 지급한 모달은 다시 열리지 않음 확인
            Assert.IsFalse(ExplorationRegionalRewards.TryClaim(runtimeId, ExplorationRegionalRewards.Option.Gold)); // 복원 뒤 원래 방의 다른 보상 선택도 재지급 차단
            Assert.IsNull(ExplorationRegionalRewards.BeginEvent(data, "278-another-desert-reward")); // 같은 탐사의 다음 사막 방에서 추가 기회 없음 확인
            Assert.AreEqual(ironBefore + ExplorationTreasureRewardService.ResourceRewardAmount, resources.IronPlate); // 복원 뒤 두 번째 지급 없이 원래 실제 자원 확인
        } // 한 회차의 미지급과 지급 저장을 실제 경로로 왕복 종료
        finally // 실제 생성 자료와 창 정리를 보장
        { // 별개 검사 자료 정리 시작
            ExplorationEventPanelView.CloseForLoad(); // 실제 복원 창과 이동 잠금 정리
            ExplorationSaveService.Remove(path); // 검사 파일 및 해당 이전 정상본 정리
            Object.Destroy(data); // 검사에만 존재하는 사막 원본 정리
            Object.Destroy(worldEvent); // 검사 영역의 실제 상호작용 오브젝트 정리
            Object.Destroy(player); // 검사 영역의 실제 플레이어 정리
        } // 별개 검사 자료 정리 종료
        yield return new ExitPlayMode(); // 실제 사막 사건 선택과 저장 검사 환경 종료
    } // 원래 게임의 격리 저장 검사 종료

    [UnityTest] // 성채와 벙커의 실제 파편 피해를 독립적으로 계산
    public IEnumerator BunkerTrapMatchesTheNativePreviewAndDamageWithoutTreatingFortressAsBunker() // 기존 지역 번호와 복합 감소 순서 확인
    { // 실제 벙커 위험 사건 검사 시작
        RequireVerificationProfile(); // 실제 탐사를 덮지 않는 검증 환경 확인
        yield return new EnterPlayMode(); // 실제 파티와 탐사 유물 지급 환경 진입
        PrepareRun(); // 게임의 실제 파티와 탐사 상태 초기화
        var chip = Acquire("BlackoutRecordChip"); // 출시 정전 기록칩을 실제 획득 경로로 보유
        Assert.AreEqual(15, chip.EffectValue); // 유물에 기록한 실제 피해 감소율 검증
        var party = BattleResultManager.EnsureInstance(); // 실제 탐사의 체력 정산
        var actor = party.ActiveParty.Members[0]; // 실제 위험 선택을 수행할 출전 아군
        Assert.AreEqual(2, (int)ExplorationRegion.Fortress); // 이전 회차 직렬화값의 실제 성채 보존
        Assert.AreEqual(3, (int)ExplorationRegion.Bunker); // 성채와 별개인 새 벙커 지역 값 확인
        Assert.AreEqual(7, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.Fortress)); // 원래 성채의 체력 피해 안내 확인
        Assert.AreEqual(7, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.Winterfall)); // 지역 없는 기존 사건의 기본 피해 안내 확인
        Assert.AreEqual(5, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.Bunker)); // 실제 벙커의 감소 후 미리보기 확인
        Assert.AreEqual(5, GlassFieldEvent.GetBreakGlassHealthDamage(ExplorationRegion.Bunker)); // 두 번째 기존 선택 UI도 같은 공용 비율 확인
        party.RestoreSavedAllyState(actor, 14, 30, 0); // 실제 파편 위험의 생존 체력과 정신력 준비
        Assert.IsTrue(AshstormEyeEvent.TryResolve("278-bunker-shatter", AshstormEyeEvent.Shatter, actor, null, out string summary, ExplorationRegion.Bunker)); // 실제 선택으로 줄어든 파편 피해 정산
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out int health, out _, out _)); // 실제 저장된 아군 체력 확인
        Assert.AreEqual(9, health); // 표시한 다섯 점만 실제 차감 확인
        StringAssert.Contains("14 → 9", summary); // 결과 원문 UI에도 저장된 실제 체력 표시 확인
        var lantern = Acquire("AfterglowLantern"); // 기존 고정 함정 피해 감소도 실제 획득
        Assert.AreEqual((20 - lantern.EffectValue) * 85 / 100, BattleResultManager.GetEventTrapHealthDamage(20, ExplorationRegion.Bunker)); // 기존 고정 감소 후 벙커 비율 내림의 정확한 순서
        yield return new ExitPlayMode(); // 벙커 파편의 실제 위험 선택 검사 정리
    } // 실제 벙커 위험 사건 검사 종료

    private static void RequireVerificationProfile() // 기존 저장과 경로가 분리된 Unity 작업 환경 확인
    { // 검증 앱 이름 비교 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 기존 사용자 자동 저장이 있는 기본 앱 정보 확인
        { // 사용자 기존 탐사 파일 보호 시작
            Assert.Ignore("저장 경로가 분리된 검증 환경에서 실행"); // 실제 사용자의 현재 회차 보호
        } // 사용자 기존 탐사 파일 보호 종료
    } // 검증 앱 이름 비교 종료

    private static ExplorationSessionManager PrepareRun() // 실제 출전 파티와 탐사 저장 기준 준비
    { // 원래 파티 및 회차의 정상 출전 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 게임의 실제 아군 명단 로드
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 회복 상태를 쓰는 기존 파티 편성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 두 아군의 편성 확인
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 원래 출전 덱과 파티 생성
        var party = BattleResultManager.EnsureInstance(); // 체력 상태 적용을 쓰는 실제 파티 관리자
        party.ResetSavedPartyState(); // 검사 시작의 아군 체력과 정신력 초기화
        Assert.IsTrue(party.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 실제 편성 파티 등록
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 실제 회차 출발 카드 덱 시작
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 중 실제 상태 저장 관리자
        session.ResetExploration(); // 원래 새 탐사의 방과 보상 상태 초기화
        session.SetCurrentFloorSeed(278); // 실제 저장 가능한 현재 층 시드 지정
        PlayerResourceManager.EnsureInstance().ResetResources(); // 이번 검사에서 지급된 실제 자원만 비교
        return session; // 실제 진행과 저장용 탐사 관리자 반환
    } // 원래 파티 및 회차의 정상 출전 준비 종료

    private static RelicData Acquire(string key) // 게임의 실제 자원에 등록된 유물 획득
    { // 실제 유물 원본과 지급 확인 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 출시 실제 유물 데이터 조회
        Assert.IsNotNull(relic, key); // 실제 출시 유물 등록 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 원래 회차 유물 획득 및 효과 연결
        return relic; // 데이터에 기록된 실제 표시 및 지급값 조회
    } // 실제 유물 원본과 지급 확인 종료
} // 실제 상황의 한정 회귀 검사 종료
