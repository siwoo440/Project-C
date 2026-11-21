using System; // 독립 저장 파일 식별자
using System.Collections; // 플레이 모드와 씬 대기
using System.IO; // 새 저장과 기존 저장 형식 비교
using System.Linq; // 실제 유물과 탐사 방 조회
using System.Text.RegularExpressions; // 재화 구분 없는 이전 저장 재현
using NUnit.Framework; // 유물 결과와 자원 수치 검증
using TMPro; // 수레 선택 버튼의 실제 확률 문구 조회
using UnityEngine; // 실제 씬과 전투 원본 연결
using UnityEngine.SceneManagement; // 실제 퇴색지대 씬 진입
using UnityEngine.TestTools; // 실제 플레이 모드 검사
using Object = UnityEngine.Object; // 임시 오브젝트 정리

public sealed class WinterfallExpeditionSettlementRelicTests // 강제 전투와 탐사 정산 및 퇴색 정신력 검사
{ // 탐사 유물 검사 시작
    [UnityTest] // 수레 상자의 기존 확률과 회피 구간 확인
    public IEnumerator GrayRopeChangesOnlyRolledAmbushAndShowsTheSameOdds() // 수레 회피의 확률 경계와 실제 선택 문구 확인
    { // 강제 매복 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 상태와 유물 인벤토리 실행
        yield return GrayRopeChangesOnlyRolledAmbushAndShowsTheSameOddsInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 플레이 모드 종료
    } // 강제 매복 유물 검사 종료

    private IEnumerator GrayRopeChangesOnlyRolledAmbushAndShowsTheSameOddsInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var session = PrepareExpedition(); // 정상 출전 파티와 고정 층 준비
        var rope = Acquire(RelicEffectType.ReducedForcedEncounterChance); // 정식 회색 로프 획득
        var wallet = PlayerResourceManager.EnsureInstance(); // 실제 탐사 지갑 조회
        var panel = ExplorationEventPanelView.EnsureInstance(); // 실제 수레 선택 화면 조회
        var marker = new GameObject("RopeCartMarker").AddComponent<ExplorationEventView>(); // 고정된 상자 위치의 실물 이벤트 표식
        try // 선택 UI와 방 표식 정리 보장
        { // 수레 확률과 결과 준비 시작
            var cart = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == BurntCartEvent.Id); // 현재 층의 수레 원문 조회
            marker.Initialize(cart, "s44"); // 원래 강제 매복 확률의 첫 값이 나오는 방
            panel.ShowEvent(marker, cart, "s44"); // 실제 탐사 선택 화면 열기
            panel.SendMessage("PopulateEventContent"); // 캡처 대기와 분리한 실제 버튼 구성 호출
            Assert.IsTrue(panel.GetComponentsInChildren<TMP_Text>(true).Any(item => item.text.Contains("철판·나사 각 1개 60% / 사냥꾼 추가 매복 30% / 매복 회피 10%"))); // 적용 가능한 실제 상자 선택 확률 확인
            ExplorationEventPanelView.CloseForLoad(); // 전투나 자동 저장 없이 선택 화면 닫기
            int screw = wallet.Screw; // 상자 열기 전 나사 기준량
            int plate = wallet.IronPlate; // 상자 열기 전 철판 기준량
            Assert.IsTrue(BurntCartEvent.TryResolve("s17", BurntCartEvent.Box, null, out _)); // 원래 물자 마지막 난수 오십구
            Assert.AreEqual(screw + 1, wallet.Screw); // 보유 유물과 관계없는 물자 지급 확인
            Assert.AreEqual(plate + 1, wallet.IronPlate); // 기존 물자 두 종류와 수량 보존 확인
            Assert.IsTrue(BurntCartEvent.TryResolve("s44", BurntCartEvent.Box, null, out string avoided)); // 기존 매복 첫 난수 육십 회피
            Assert.IsTrue(BurntCartEvent.TryResolve("s23", BurntCartEvent.Box, null, out _)); // 회피 마지막 난수 육십구 확인
            StringAssert.Contains("매복을 피했습니다", avoided); // 강제 전투가 사라진 실제 사건 결말 확인
            Assert.IsNull(session.Cart.AmbushRoom); // 회피한 사건의 전투 미예약 확인
            Assert.IsTrue(session.IsEventResolved("s44") && session.IsEventResolved("s23")); // 매복 회피도 정상 사건 완료 처리 확인
            Assert.AreEqual(screw + 1, wallet.Screw); // 매복 회피를 물자 지급으로 바꾸지 않음 확인
            Assert.AreEqual(plate + 1, wallet.IronPlate); // 매복 회피의 별도 보상 미발생 확인
            Assert.IsTrue(BurntCartEvent.TryResolve("s124", BurntCartEvent.Box, null, out _)); // 유지된 강제 매복 첫 난수 칠십 확인
            Assert.AreEqual("s124", session.Cart.AmbushRoom); // 임계값 이후 원래 필수 전투 예약 확인
            Assert.IsTrue(RelicRunManager.Instance.Inventory.TryRemove(rope.RelicId)); // 기존 확률의 유물 미보유 대조 준비
            session.ResetExploration(); // 새 탐사에서 이전 강제 매복의 대기 상태 제거
            Assert.IsTrue(BurntCartEvent.TryResolve("s44", BurntCartEvent.Box, null, out _)); // 회색 로프 없이 같은 고정 난수 재실행
            Assert.AreEqual("s44", session.Cart.AmbushRoom); // 원래 확률에서 같은 상자의 강제 전투 진입 확인
        } // 수레 확률과 결과 준비 종료
        finally // 현재 이벤트 화면과 임시 표식 정리 보장
        { // 임시 오브젝트 정리 시작
            ExplorationEventPanelView.CloseForLoad(); // 대기 상태와 이동 잠금 해제
            Object.Destroy(marker.gameObject); // 새로 생성한 수레 사건 표식 정리
        } // 임시 오브젝트 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 남은 탐사 골드의 새 저장과 이전 저장 정산 비교
    public IEnumerator TravelerCharmRoundsAndPaysOnlyTrackedGoldOnceAcrossRestores() // 실제 보유 탐사 골드만 성공 시 한 번 지급 확인
    { // 성공 정산 검사 시작
        yield return new EnterPlayMode(); // 실제 저장과 지갑 상태 실행
        yield return TravelerCharmRoundsAndPaysOnlyTrackedGoldOnceAcrossRestoresInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 플레이 모드 종료
    } // 성공 정산 검사 종료

    private IEnumerator TravelerCharmRoundsAndPaysOnlyTrackedGoldOnceAcrossRestoresInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var session = PrepareExpedition(); // 저장 가능한 출발 파티와 회차 시작 준비
        Acquire(RelicEffectType.ExplorationSuccessGoldPercent); // 정식 여행자의 부적 획득
        var wallet = PlayerResourceManager.EnsureInstance(); // 공용 자원을 사용하는 실제 지갑 조회
        wallet.RestoreResources(200, 0, 0, 0); // 기존 소유 골드와 다른 탐사 획득분 구분
        wallet.BeginExplorationResourceTracking(); // 탐사 시작 뒤 실제 수입 추적 설정
        wallet.AddResources(61, 0, 0, 0); // 소수점 내림이 드러나는 탐사 획득 골드 지급
        Assert.IsTrue(wallet.TrySpend(220, 0, 0, 0)); // 최초 골드와 일부 탐사 획득분의 일반 소비
        Assert.AreEqual(41, wallet.ExplorationGold); // 성공 정산의 원금인 남은 탐사 골드 확인
        string key = "day244-charm-" + Guid.NewGuid().ToString("N"); // 다른 검사와 분리된 파일 키
        string runPath = Path.Combine(Application.temporaryCachePath, key + "-run.json"); // 독립 탐사 저장 파일
        string oldPath = Path.Combine(Application.temporaryCachePath, key + "-old.json"); // 재화 구분 없는 구버전 파일
        string lobbyPath = Path.Combine(Application.temporaryCachePath, key + "-lobby.json"); // 성공 후 실제 거점 파일
        try // 검사 파일과 복구본 정리 보장
        { // 저장 복원과 지급량 검증 시작
            Assert.IsTrue(ExplorationSaveService.Save(runPath, Vector3.zero)); // 현 탐사 지갑과 남은 획득분 동시 저장
            string json = File.ReadAllText(runPath); // 정상 탐사 저장 텍스트 준비
            string old = Regex.Replace(json, ",\"(?:hasResourceLedger|temporaryGold|temporaryScrew|temporaryIronPlate|temporaryWire)\":[^,}]+", string.Empty); // 이전 형식의 획득분 미기록 자료 생성
            Assert.AreNotEqual(json, old); // 이전 저장에 없는 필드가 실제로 제거됐는지 확인
            File.WriteAllText(oldPath, old); // 별도 이전 형식 검사 파일 작성
            Assert.IsTrue(ExplorationSaveService.Load(oldPath)); // 장부가 없던 탐사 저장 복원
            Assert.AreEqual(41, wallet.Gold); // 이전 저장의 전체 지갑 보존 확인
            Assert.AreEqual(0, wallet.ExplorationGold); // 출처를 구분할 수 없는 이전 지갑을 정산에서 제외
            Assert.IsTrue(session.CompleteExplorationSuccess()); // 구버전 복원 직후 성공 정산
            Assert.AreEqual(41, wallet.Gold); // 이전 지갑 전체에 부적을 곱하는 오류 방지
            Assert.IsTrue(ExplorationSaveService.Load(runPath)); // 장부가 있는 같은 출발점 재현
            Assert.AreEqual(41, wallet.ExplorationGold); // 남은 탐사 몫이 원래 정산 기준대로 복원 확인
            bool? reentrant = null; // 지갑 변경 콜백에서 중복 정산을 확인할 기록
            void AttemptDuplicate() // 실제 지갑 변경 알림에서 성공 정산 재진입
            { // 중복 호출 기록 시작
                reentrant = session.CompleteExplorationSuccess(); // 기존 종료 상태로 중복 지급 차단 확인
            } // 중복 호출 기록 종료
            wallet.ResourcesChanged += AttemptDuplicate; // 지급 이벤트와 동시에 호출되는 경우 설정
            try // 이벤트 연결 정리 보장
            { // 실제 성공 정산 시작
                Assert.IsTrue(session.CompleteExplorationSuccess()); // 지급 가능한 정상 탐사 종료
            } // 실제 성공 정산 종료
            finally // 지갑 콜백 검사 정리
            { // 중복 호출 연결 해제 시작
                wallet.ResourcesChanged -= AttemptDuplicate; // 이후 복원과 거래에 검사 콜백 미적용
            } // 중복 호출 연결 해제 종료
            Assert.AreEqual(false, reentrant); // 자원 알림에서도 성공 중복 진입 거부 확인
            Assert.IsFalse(session.CompleteExplorationSuccess()); // 명시적 반복 종료의 지급 차단 확인
            Assert.AreEqual(43, wallet.Gold); // 남은 탐사 골드 사십일의 오 퍼센트 내림만 추가
            Assert.IsFalse(wallet.IsTrackingExplorationResources); // 가산액이 정산 대상에 재계상되지 않는 종료 확인
            Assert.IsTrue(ExplorationAutoSave.SaveAfterBattle(Vector3.zero, runPath, lobbyPath)); // 정상 종료의 새 거점 상태 저장
            Assert.IsTrue(LobbySaveService.Load(lobbyPath)); // 실제 거점 파일에서 정산 후 지갑 복원
            Assert.AreEqual(43, wallet.Gold); // 거점 저장 복원 후 기존 지급액 유지 확인
        } // 저장 복원과 지급량 검증 종료
        finally // 검사 전용 저장 파일 정리
        { // 검사 파일 제거 시작
            ExplorationSaveService.Remove(runPath); // 독립 최신 탐사 저장과 복구본 정리
            ExplorationSaveService.Remove(oldPath); // 독립 이전 탐사 저장과 복구본 정리
            ExplorationSaveService.Remove(lobbyPath); // 독립 거점 저장과 복구본 정리
        } // 검사 파일 제거 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 파티 전멸과 진행 초기화의 지급 제외 검사
    public IEnumerator TravelerCharmPaysNothingOnFailureOrAbortedRun() // 성공 외 탐사 종료 방식의 골드 기준 확인
    { // 실패와 중단 정산 검사 시작
        yield return new EnterPlayMode(); // 탐사 보유 상태 실행
        yield return TravelerCharmPaysNothingOnFailureOrAbortedRunInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 플레이 모드 종료
    } // 실패와 중단 정산 검사 종료

    private IEnumerator TravelerCharmPaysNothingOnFailureOrAbortedRunInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var session = PrepareExpedition(); // 실제 회차와 파티 준비
        Acquire(RelicEffectType.ExplorationSuccessGoldPercent); // 실패 검사에서도 여행자의 부적 보유
        var wallet = PlayerResourceManager.EnsureInstance(); // 실제 탐사 지갑 조회
        wallet.RestoreResources(80, 0, 0, 0); // 전멸 전 기존 골드 기준 준비
        wallet.BeginExplorationResourceTracking(); // 첫 탐사 획득분 기록 시작
        wallet.AddResources(100, 0, 0, 0); // 부적 가산이 가능할 정도의 신규 수입 지급
        LogAssert.Expect(LogType.Warning, new Regex("탐사 실패")); // 기존 전멸 정산 경고 확인
        Assert.IsTrue(session.CompleteExplorationFailure("검사 실패")); // 실제 탐사 실패 처리 호출
        Assert.AreEqual(80, wallet.Gold); // 전멸 시 탐사 획득 골드 회수와 부적 미지급 확인
        Assert.IsFalse(session.CompleteExplorationSuccess()); // 실패 완료를 성공으로 다시 정산하지 않음 확인
        session.ResetExploration(); // 다음 독립 탐사 준비
        wallet.BeginExplorationResourceTracking(); // 진행 중단을 비교할 새 획득분 기록
        wallet.AddResources(100, 0, 0, 0); // 초기화 전에 보유한 현재 탐사 수입 준비
        session.ResetExploration(); // 성공 없이 현재 탐사 상태 초기화
        Assert.AreEqual(180, wallet.Gold); // 원래 중단 처리의 지갑 보존 및 부적 가산 없음 확인
        Assert.IsFalse(wallet.IsTrackingExplorationResources); // 중단 뒤 이전 회차 획득분 정산 기회 제거 확인
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 퇴색 환경과 실제 조우의 정신력 손실과 예고 확인
    public IEnumerator ErosionBellReducesFadePulseAndAllyMentalLossWithSamePreview() // 전투 밖 실제 환경과 전투 안 정신력 규칙 비교
    { // 퇴색 정신력 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 탐사 씬 초기화에 쓸 검증 전용 저장 경로 확인
        { // 일반 저장 환경 보호 시작
            Assert.Ignore("플레이 저장 경로가 격리된 검증 사본에서 실행"); // 실제 탐사 자동 저장이 없는 독립 검사 유지
        } // 일반 저장 환경 보호 종료
        yield return new EnterPlayMode(); // 실제 맵과 전투 자산 실행
        yield return ErosionBellReducesFadePulseAndAllyMentalLossWithSamePreviewInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 플레이 모드 종료
    } // 퇴색 정신력 검사 종료

    private IEnumerator ErosionBellReducesFadePulseAndAllyMentalLossWithSamePreviewInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var session = PrepareExpedition(); // 퇴색 방이 생성되는 검사 시드의 정상 탐사 준비
        yield return SceneManager.LoadSceneAsync("30_Exploration", LoadSceneMode.Single); // 실제 퇴색 방이 있는 탐사 씬 진입
        var deadline = Time.realtimeSinceStartup + 15f; // 절차 맵 생성과 퇴색 관리자 준비 기한
        while (Object.FindFirstObjectByType<ExplorationHazardRuntime>() == null && Time.realtimeSinceStartup < deadline) // 장면 로드 뒤 위험 관리자가 등록될 때까지 대기
        { // 실제 탐사 초기화 대기 시작
            yield return null; // 다음 플레이 프레임으로 이동
        } // 실제 탐사 초기화 대기 종료
        var relic = Acquire(RelicEffectType.FadeMentalLossReduction); // 정식 침식 방울귀걸이 획득
        var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 실제 탐사 맵 구조 조회
        var hazard = Object.FindFirstObjectByType<ExplorationHazardRuntime>(); // 실제 퇴색 환경 피해 처리기 조회
        Assert.IsNotNull(map?.CurrentMap, "퇴색 방이 포함된 실제 탐사 맵 생성"); // 탐사 자동 초기화 완료 확인
        Assert.IsNotNull(hazard, "실제 탐사 맵의 퇴색 관리자 연결"); // 환경 피해 발동 대상 확인
        var party = BattleResultManager.EnsureInstance(); // 탐사에 출전한 아군의 영구 수치 조회
        var member = party.ActiveParty.Members[0]; // 환경 피해 범위 확인용 생존 아군
        party.RestoreSavedAllyState(member, 30, 50, 0); // 위험도별 정신력 비교 시작 상태 복원
        hazard.SendMessage("ApplyHazardPulse", 1); // 실제 환경 피해 발동 경로의 일 점 정신력 입력
        Assert.AreEqual(0, hazard.LastMentalDamage); // 실제 위험 알림의 정신력 손실 영점 확인
        Assert.IsTrue(party.TryGetSavedAllyState(member, out int firstHealth, out int firstMental, out _)); // 첫 퇴색 피해가 저장된 출전 상태 확인
        Assert.AreEqual(28, firstHealth); // 정신력 완화와 별개인 퇴색 체력 피해 보존
        Assert.AreEqual(50, firstMental); // 위험도 일에서 실제 정신력 손실 차단 확인
        hazard.SendMessage("ApplyHazardPulse", 3); // 위험도 삼의 실제 환경 피해 한 번 적용
        Assert.AreEqual(2, hazard.LastMentalDamage); // 위험도 삼의 경감 후 실제 위험 알림 확인
        Assert.IsTrue(party.TryGetSavedAllyState(member, out _, out int hazardMental, out _)); // 환경 피해 반영 뒤 영구 정신력 조회
        Assert.AreEqual(48, hazardMental); // 환경 피해 정신력 손실 한 점만 감소 확인
        var cell = map.CurrentMap.Cells.First(item => map.TryGetHazardAt(item.Coordinate, out var room) && room.HazardType == ExplorationHazardType.Fade); // 실제 퇴색 표시가 있는 방 조회
        Vector3 fade = map.GetWorldPosition(cell.Coordinate); // 실제 퇴색 조우의 진입 좌표
        Vector3 safe = map.GetWorldPosition(map.CurrentMap.StartCoordinate); // 동일 층의 안전 방 중심 조회
        var encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshWanderer"); // 정상 조우 목록의 전투 원본
        Assert.IsTrue(session.BeginEncounter("day244-fade-mental", encounter, fade, fade)); // 실제 퇴색 방 좌표의 활성 조우 시작
        var ally = BattleUnitRuntime.CreateAlly(member); // 실제 출전 아군의 정신력 런타임 생성
        var enemy = BattleUnitRuntime.CreateEnemy(encounter.Enemies[0]); // 조우의 실제 적 행동자 생성
        ally.ApplyPersistentMental(50); // 정신력 편차 없는 직접 비교 기준 설정
        Assert.AreEqual(0, ally.PreviewMentalChange(-1)); // 완전히 상쇄되는 단일 정신력 손실 예고
        Assert.IsFalse(ally.CanChangeMental(-1)); // 실제 손실 없는 정신력 카드 대상 검증
        BattleMentalChangeResult prevented = ally.ChangeMental(-1, BattleMentalChangeReason.ReceiveDamage); // 피해 처리에서 발생한 단일 정신력 손실
        Assert.AreEqual(-1, prevented.RequestedDelta); // 원래 손실량 기록 보존 확인
        Assert.AreEqual(0, prevented.AppliedDelta); // 예고와 같은 실제 영점 손실 확인
        Assert.AreEqual(-3, ally.PreviewMentalChange(-4)); // 퇴색 조우의 적 추가 정신력 감소 예고
        var attack = new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, 1, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "검사", 1, 1, 1, 0, mentalChangeOnHit: -4); // 적중 시 정신력 감소가 있는 실제 적 예고 행동
        ally.GainBlock(100); // 체력 피해로 발생하는 다른 정신력 손실 분리
        attack.Execute(); // 실제 적 행동을 명중시켜 보정한 정신력 적용
        Assert.AreEqual(47, ally.CurrentMental); // 예고한 세 점의 정신력 손실 적용 확인
        Assert.AreEqual(2, ally.ChangeMental(2, BattleMentalChangeReason.CardEffect).AppliedDelta); // 같은 퇴색 조우에서 정신력 회복 보존 확인
        Assert.AreEqual(-4, enemy.ChangeMental(-4, BattleMentalChangeReason.CardEffect).AppliedDelta); // 같은 조우의 적 정신력 손실 보존 확인
        session.ResolveBattleResult(new BattleResultData(BattleResult.Escape, BattleType.Normal, 1, new[] { ally }, Array.Empty<string>())); // 끝난 퇴색 조우의 상태 정리
        Assert.IsTrue(session.BeginEncounter("day244-safe-mental", encounter, safe, safe)); // 같은 회차 안전 방의 활성 조우 시작
        Assert.AreEqual(-4, ally.PreviewMentalChange(-4)); // 일반 조우에는 귀걸이 경감 미적용 확인
        Assert.AreEqual(-4, ally.ChangeMental(-4, BattleMentalChangeReason.CardEffect).AppliedDelta); // 일반 조우의 원래 정신력 감소 적용 확인
        session.ResolveBattleResult(new BattleResultData(BattleResult.Escape, BattleType.Normal, 1, new[] { ally }, Array.Empty<string>())); // 안전 방 비교 조우 상태 정리
        Assert.IsTrue(RelicRunManager.Instance.Inventory.TryRemove(relic.RelicId)); // 동일 퇴색 방에서 유물 미보유 조건 준비
        Assert.IsTrue(session.BeginEncounter("day244-fade-no-bell", encounter, fade, fade)); // 귀걸이 없이 동일 퇴색 환경 진입
        Assert.AreEqual(-4, ally.PreviewMentalChange(-4)); // 지역만으로 손실 완화가 생기지 않음 확인
    } // 플레이 모드 본문 종료

    private static ExplorationSessionManager PrepareExpedition() // 게임의 실제 출발 파티 및 덱으로 검사 회차 구성
    { // 탐사 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 게임 파티 명단 조회
        var recovery = new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance()); // 정상 출전 조건 연결
        var selection = new PartySelection(recovery); // 독립 탐사 출전 파티 준비
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 두 아군으로 출전 조건 충족
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출발용 카드 덱과 파티 동시 준비
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 저장에 연결되는 실제 카드 덱 초기화
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 탐사 이벤트의 출전 생존자 상태 연결
        var session = ExplorationSessionManager.EnsureInstance(); // 실제 탐사 세션 관리자 조회
        session.ResetExploration(); // 이전 결과와 대기 회차 상태 제거
        session.SetCurrentFloorSeed(238); // 기존 검사와 같은 퇴색 방을 포함한 고정 시드
        return session; // 준비된 검사 탐사 세션 반환
    } // 탐사 준비 종료

    private static RelicData Acquire(RelicEffectType type) // 실제 저장 가능한 유물 효과의 원본 획득
    { // 유물 획득 확인 시작
        var relic = Resources.LoadAll<RelicData>("Relics").Single(item => item.EffectType == type); // 실제 도감과 저장 카탈로그의 유물 원본
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().Inventory.TryAcquire(relic)); // 실제 탐사 보유 상태 반영
        return relic; // 검사 종료 때 제거할 유물 원본 반환
    } // 유물 획득 확인 종료
} // 탐사 유물 검사 종료
