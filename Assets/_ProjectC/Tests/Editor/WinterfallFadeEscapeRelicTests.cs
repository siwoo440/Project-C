using System.Collections; // 플레이 모드 전환 대기
using System.IO; // 독립 탐사 저장 파일 조회
using System.Linq; // 실제 조우와 유물 원본 조회
using System.Text.RegularExpressions; // 구버전 저장 형식 재현
using NUnit.Framework; // 실제 결과 대조
using UnityEngine; // 전투 원본과 탐사 오브젝트 생성
using UnityEngine.SceneManagement; // 프로젝트에 등록된 탐사 씬 로딩
using UnityEngine.TestTools; // 플레이 모드 검사 등록

public sealed class WinterfallFadeEscapeRelicTests // 퇴색 교전과 도주 골드 유물 검사
{ // 유물 검사 시작
    [UnityTest] // 실제 방의 퇴색 상태를 이용하는 검사 등록
    public IEnumerator GasMaskShortensOnlyTimedAllyDebuffsInFadeBattles() // 퇴색 지역과 일반 상태 적용 비교
    { // 방독면 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 탐사 장면 초기화에 사용할 격리 환경 확인
        { // 실제 플레이 저장 보호 시작
            Assert.Ignore("플레이 저장 경로가 격리된 검증 사본에서 실행"); // 일반 프로젝트의 탐사 저장 보호
        } // 실제 플레이 저장 보호 종료
        yield return new EnterPlayMode(); // 탐사 맵 구성 가능 상태 진입
        PrepareExpedition(); // 두 명의 캐릭터와 시드 준비
        yield return SceneManager.LoadSceneAsync("30_Exploration", LoadSceneMode.Single); // 실제 방과 타일 구성을 가진 탐사 씬 준비
        VerifyGasMaskInExploration(); // 실제 맵의 퇴색 방과 안전 방 및 미보유 대조
        yield return new ExitPlayMode(); // 실제 탐사 도메인 종료
    } // 방독면 검사 종료

    private static void VerifyGasMaskInExploration() // 플레이 모드 전환 이후의 실제 탐사 상태 대조
    { // 실제 방독면 효과 검사 시작
        var session = ExplorationSessionManager.Instance; // 전환 후 현재 탐사 관리자 조회
        Assert.IsNotNull(session); // 탐사 관리자와 출전 상태 확인
        var scene = SceneManager.GetActiveScene(); // 실제 탐사 장면 조회
        Assert.AreEqual("30_Exploration", scene.name); // 올바른 탐사 장면 로딩 확인
        var map = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ExplorationMapRuntime>()).SingleOrDefault(); // 현재 탐사 씬 안의 실제 맵 확인
        Assert.IsNotNull(map); // 맵 런타임 초기화 검증
        Assert.IsNotNull(map.CurrentMap); // 지정 시드로 생성한 퇴색 탐사 맵 검증
        RelicData mask = Acquire(RelicEffectType.ShorterFadeDebuffs); // 정식 불완전한 방독면 보유
        EncounterData encounter = NormalEncounter(); // 실제 일반 조우 원본 복사
        try // 임시 조우 원본 정리 보장
        { // 퇴색 조우 검사 시작
            var fade = map.CurrentMap.Cells.First(cell => map.TryGetHazardAt(cell.Coordinate, out var hazard) && hazard.HazardType == ExplorationHazardType.Fade); // 실제 생성된 퇴색 방 조회
            Vector3 fadePosition = map.GetWorldPosition(fade.Coordinate); // 타일맵 퇴색 방 중심 조회
            Assert.IsTrue(session.BeginEncounter("fade-mask", encounter, fadePosition, fadePosition)); // 퇴색 방 좌표로 실제 조우 진입
            var ally = CreateAlly(); // 실제 출전 아군 전투 유닛 생성
            var enemy = BattleUnitRuntime.CreateEnemy(encounter.Enemies[0]); // 같은 전투 적 유닛 생성
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 2, enemy)); // 퇴색 지역에서 다중 턴 중독 부여
            Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 받은 일반 디버프 지속 한 턴 단축 확인
            Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 4, 2, enemy)); // 같은 상태의 더 긴 재적용
            Assert.AreEqual(3, ally.GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 재적용 지속만 한 턴 단축 확인
            Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.Poison).StackCount); // 기존 중첩 규칙 유지 확인
            AssertDuration(ally, BattleStatusEffectType.Burn, 1, 1); // 한 턴 상태의 최소값 유지 확인
            AssertDuration(ally, BattleStatusEffectType.AttackPowerUp, 3, 3); // 아군 강화 효과 지속 유지 확인
            AssertDuration(ally, BattleStatusEffectType.Stagger, 3, 3); // 다음 예고 행동을 기다리는 상태 제외 확인
            AssertDuration(enemy, BattleStatusEffectType.Poison, 3, 3); // 적에게 적용된 디버프 지속 유지 확인
            session.ResolveBattleResult(Result(BattleResult.Escape, ally)); // 퇴색 교전 종료와 지역 정보 정리
            Vector3 safePosition = map.GetWorldPosition(map.CurrentMap.StartCoordinate); // 같은 층의 실제 안전 방 중심 조회
            Assert.IsTrue(session.BeginEncounter("safe-mask", encounter, safePosition, safePosition)); // 방독면을 보유한 안전 방 진입
            AssertDuration(CreateAlly(), BattleStatusEffectType.Poison, 3, 3); // 지역 밖에서 지속 보정 없음 확인
            session.ResolveBattleResult(Result(BattleResult.Escape, ally)); // 안전 방 비교 조우 종료
            Assert.IsTrue(RelicRunManager.Instance.Inventory.TryRemove(mask.RelicId)); // 유물 미보유 비교 조건 준비
            Assert.IsTrue(session.BeginEncounter("fade-no-mask", encounter, fadePosition, fadePosition)); // 방독면 없는 퇴색 방 진입
            AssertDuration(CreateAlly(), BattleStatusEffectType.Poison, 3, 3); // 지역만으로는 지속 단축되지 않음 확인
        } // 퇴색 조우 검사 종료
        finally // 임시 원본 정리
        { // 원본 정리 시작
            Object.Destroy(encounter); // 복제 조우 원본 제거
        } // 원본 정리 종료
    } // 실제 방독면 효과 검사 종료

    [UnityTest] // 지갑과 저장에 반영된 도주 보상 검사 등록
    public IEnumerator FlareRetainsGoldOnceAcrossSaveAndCreditsItTowardVictory() // 도주 선지급과 승리 차액 대조
    { // 신호탄 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실제 거점 재화를 보호할 검증 환경 확인
        { // 격리 확인 시작
            Assert.Ignore("플레이 저장 경로가 격리된 검증 사본에서 실행"); // 실제 탐사 저장과 지갑 보존
        } // 격리 확인 종료
        yield return new EnterPlayMode(); // 탐사 상태와 골드 지갑 생성 가능 상태 진입
        var session = PrepareExpedition(); // 실제 저장 가능한 파티와 시작 덱 생성
        Acquire(RelicEffectType.EscapePartialGold); // 정식 조난 신호탄 보유
        var wallet = PlayerResourceManager.EnsureInstance(); // 탐사 골드 지갑 조회
        wallet.BeginExplorationResourceTracking(); // 실제 탐사 획득분 추적 시작
        int originalGold = wallet.Gold; // 도주 전 지갑 골드 보관
        int originalScrew = wallet.Screw; // 도주 전 지갑 나사 보관
        var encounter = NormalEncounter(); // 일반 조우 원본 복사
        JsonUtility.FromJsonOverwrite("{\"goldReward\":57}", encounter); // 소수점 절사를 볼 수 있는 기본 골드 설정
        var ally = CreateAlly(); // 생존 아군 전투 상태 생성
        var escape = Result(BattleResult.Escape, ally); // 실제 도주 결과 생성
        const string room = "day238-flare-claim"; // 동일 조우의 반복 여부를 대조할 배치 식별자
        string path = Path.Combine(Application.temporaryCachePath, "flare-" + System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 파일 경로
        try // 임시 원본과 파일 정리 보장
        { // 도주 보상 경로 검사 시작
            Assert.IsTrue(session.BeginEncounter(room, encounter, Vector3.zero, Vector3.zero)); // 첫 일반 조우 진입
            session.ResolveBattleResult(escape); // 첫 성공 도주 정산
            session.ResolveBattleResult(escape); // 중복 결과 입력 방어 확인
            Assert.AreEqual(originalGold + 14, wallet.Gold); // 기본 골드의 사분의 일 내림 지급 확인
            Assert.AreEqual(14, wallet.ExplorationGold); // 도주 골드의 탐사 획득분 반영 확인
            Assert.AreEqual(14, session.RunGoldGained); // 실제 입금과 같은 탐사 골드 합계 확인
            Assert.AreEqual(originalScrew, wallet.Screw); // 도주에서 제작 재료 미지급 확인
            Assert.AreEqual(0, session.RunExperienceGained); // 도주에서 승리 경험치 미지급 확인
            Assert.IsNull(session.LastClearReward); // 승리 보상 표시 미발동 확인
            Assert.IsFalse(session.IsEncounterCleared(room)); // 같은 조우의 재도전 허용 확인
            Assert.IsFalse(escape.CanReceiveReward); // 기존 승리 보상 이벤트 보호 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 지급량과 수령 기록 동시 저장
            string json = File.ReadAllText(path); // 실제 새 탐사 파일 내용 조회
            string legacy = Regex.Replace(json, ",\"escapeGoldClaims\":\\[[^\\]]*\\]", string.Empty); // 지급 기록 필드가 없던 구버전 저장 재현
            Assert.AreNotEqual(json, legacy); // 실제 파일에 신규 기록이 들어갔는지 확인
            Assert.IsTrue(ExplorationSaveService.CanLoadJson(legacy)); // 필드가 없는 저장도 복원 가능 확인
            session.ResetExploration(); // 메모리의 도주 기록 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 파일에서 지급량과 조우별 기록 복원
            Assert.IsTrue(session.BeginEncounter(room, encounter, Vector3.zero, Vector3.zero)); // 복원 후 같은 조우 재진입
            session.ResolveBattleResult(escape); // 같은 조우에서 도주 반복
            Assert.AreEqual(originalGold + 14, wallet.Gold); // 저장 복원 후 중복 지급 차단 확인
            Assert.IsTrue(session.BeginEncounter(room, encounter, Vector3.zero, Vector3.zero)); // 동일 조우의 두 번째 재진입
            session.ResolveBattleResult(Result(BattleResult.Victory, ally)); // 나중에 도전한 같은 조우 승리 정산
            Assert.AreEqual(originalGold + 57, wallet.Gold); // 도주와 승리로 받은 골드 총액 확인
            Assert.AreEqual(43, session.LastClearReward.Gold); // 이미 받은 금액을 차감한 승리 골드 확인
            Assert.AreEqual(57, session.RunGoldGained); // 입금 합계에 중복 차감이 없는지 확인
            Assert.IsTrue(session.IsEncounterCleared(room)); // 정상 승리 진행 확인
        } // 도주 보상 경로 검사 종료
        finally // 검사 자산 정리
        { // 검사 자산 정리 시작
            ExplorationSaveService.Remove(path); // 독립 저장 및 복구 파일 제거
            Object.Destroy(encounter); // 임시 조우 원본 제거
        } // 검사 자산 정리 종료
        yield return new ExitPlayMode(); // 실제 탐사 도메인 종료
    } // 신호탄 검사 종료

    private static ExplorationSessionManager PrepareExpedition() // 실제 출전 파티와 탐사 초기화
    { // 탐사 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 게임 파티 원본 명단 조회
        var recovery = new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance()); // 출전 가능 상태 확인 준비
        var selection = new PartySelection(recovery); // 독립 파티 편성 생성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 출전 캐릭터 두 명 편성
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 탐사 시작 덱과 파티 반영
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 저장 가능한 회차 덱 준비
        var session = ExplorationSessionManager.EnsureInstance(); // 현재 탐사 관리자 조회
        session.ResetExploration(); // 새로운 탐사 상태 초기화
        session.SetCurrentFloorSeed(238); // 고정된 시험용 층 시드 설정
        return session; // 준비한 탐사 관리자 반환
    } // 탐사 준비 종료

    private static RelicData Acquire(RelicEffectType effect) // 해당 효과의 실제 유물 획득
    { // 유물 준비 시작
        var relic = Resources.LoadAll<RelicData>("Relics").Single(item => item.EffectType == effect); // 실제 저장 카탈로그의 유물 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().Inventory.TryAcquire(relic)); // 정상 유물 보관함 지급 확인
        return relic; // 획득한 유물 원본 반환
    } // 유물 준비 종료

    private static EncounterData NormalEncounter() // 검사에서 사용할 실제 일반 조우 복제
    { // 조우 준비 시작
        return Object.Instantiate(Resources.Load<EncounterData>("Encounters/Encounter_WF_AshWanderer")); // 실제 유효한 일반 조우 원본 복사
    } // 조우 준비 종료

    private static BattleUnitRuntime CreateAlly() // 전투 결과와 상태 적용 생존 아군 생성
    { // 아군 준비 시작
        return BattleUnitRuntime.CreateAlly(PartyLoadoutRuntime.Active.Party.Members[0]); // 출전 아군의 실제 전투 유닛 반환
    } // 아군 준비 종료

    private static BattleResultData Result(BattleResult outcome, BattleUnitRuntime ally) // 정산용 일반 전투 결과 생성
    { // 전투 결과 준비 시작
        return new BattleResultData(outcome, BattleType.Normal, 1, new[] { ally }, System.Array.Empty<string>()); // 실제 생존 아군과 전투 결과 전달
    } // 전투 결과 준비 종료

    private static void AssertDuration(BattleUnitRuntime unit, BattleStatusEffectType state, int requested, int expected) // 적용 직후 실제 지속 턴 확인
    { // 지속 턴 대조 시작
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, unit.ApplyStatusEffect(state, 2, requested, 1)); // 대상에게 지정 상태 적용 확인
        Assert.AreEqual(expected, unit.GetStatusEffect(state).RemainingTurns); // 실제 상태 지속 시간 대조
    } // 지속 턴 대조 종료
} // 유물 검사 종료
