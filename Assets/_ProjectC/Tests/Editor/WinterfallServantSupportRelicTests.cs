using System; // 실제 씬 초기화 대기 조건과 카드 식별 비교
using System.Collections; // 플레이 모드 및 전투 화면 프레임 대기
using System.Linq; // 사용 가능한 카드를 선택하는 실제 버튼 조회
using System.Reflection; // 소환의 일부 연결 실패를 재현할 기존 관리자 접근
using NUnit.Framework; // 회복과 카드 사용 및 소환 효과 확인
using UnityEngine; // 실제 배포 전투 원본 조회와 테스트 객체 사용
using UnityEngine.SceneManagement; // 실제 게임 시작 및 전투 씬 진입
using UnityEngine.TestTools; // 실제 플레이 모드와 예상 로그 확인
using UnityEngine.UI; // 실제 선택 카드 버튼 입력 연결
using Object = UnityEngine.Object; // 실제 Unity 전투 화면과 임시 원본 접근
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드 및 전투를 만드는 공용 검사 구성

public sealed class WinterfallServantSupportRelicTests // 실제 회복과 성배 정산 및 소환 지원 유물 확인
{ // 세 지원 유물의 플레이 회귀 검사 시작
    [UnityTest] // 체력이 실제로 오른 바로 그 아군의 방어도 검사
    public IEnumerator ServantGloveGivesBlockToEachActuallyHealedLivingAlly() // 하인 장갑의 실제 체력 회복 이벤트 확인
    { // 공용 회복 신호와 방어도 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 유물 보유와 기본 상태 이벤트 연결 환경
        var relics = Acquire("OldServantGlove"); // 낡은 하인 장갑의 실제 자원 보유
        using (var fight = new Fight("THRUST")) // 정상 생존 아군과 적의 독립 전투 구성
        using (var dispatcher = new BattleEventDispatcher()) // 실제 전투 회복 이벤트의 공용 전달기 생성
        using (var bridge = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 유닛의 실제 회복과 공용 신호 연결
        using (var effects = new BattleRelicEffectController(dispatcher, relics.Inventory, relics.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points)) // 회차에서 보유한 유물의 실제 회복 발동 구독
        { // 양수 회복과 다른 대상의 원래 방어도 비교 시작
            var healer = fight.Allies[0]; // 다른 아군을 치유한 실제 회복 발생자
            var healed = fight.Allies[1]; // 방어도를 받을 실제 체력 회복 대상
            var fallen = fight.Allies[2]; // 회복할 수 없는 전투불능 아군 비교
            healed.ApplyPersistentHealth(healed.MaxHealth - 6); // 실제 치료 공간 여섯 점 준비
            fallen.ApplyPersistentHealth(0); // 대상이 이미 쓰러진 실제 무효 회복 준비
            Assert.AreEqual(2, healed.RestoreHealth(2, healer)); // 정상 회복이 받은 아군의 기존 체력을 올리는 경계 확인
            Assert.AreEqual(3, healed.CurrentBlock); // 실제 양수 회복 직후 대상만 방어도 세 점 획득 확인
            Assert.Zero(healer.CurrentBlock); // 회복량을 공급한 다른 아군에게 방어도가 가지 않음 확인
            Assert.AreEqual(4, healed.RestoreHealth(99, healer)); // 두 번째에는 실제로 남은 체력만 회복 확인
            Assert.AreEqual(6, healed.CurrentBlock); // 같은 턴의 추가 양수 회복도 독립 방어도 지급 확인
            Assert.Zero(healed.RestoreHealth(99, healer)); // 최대 체력인 대상의 시도는 양수 회복 없음 확인
            Assert.Zero(healed.RestoreHealth(0, healer)); // 잘못된 영 수치의 실제 회복 요청 차단 확인
            Assert.Zero(fallen.RestoreHealth(99, healer)); // 전투불능 아군에게 일반 회복이 성립하지 않음 확인
            Assert.AreEqual(6, healed.CurrentBlock); // 무효나 최대 체력 회복으로 추가 방어도를 얻지 않음 확인
            Assert.Zero(fallen.CurrentBlock); // 전투불능 대상의 방어도 제외 확인
            var enemy = fight.Enemies[0]; // 양수가 들어가도 방어도가 없어야 하는 상대 진영
            enemy.TakeDamage(5, BattleDamageType.None); // 적에게도 실제 회복 가능한 체력 손실 준비
            Assert.AreEqual(3, enemy.RestoreHealth(3)); // 적의 양수 회복 신호 발생 확인
            Assert.Zero(enemy.CurrentBlock); // 파티 밖 체력 회복은 하인 장갑 대상에서 제외 확인
        } // 양수 회복과 다른 대상의 원래 방어도 비교 종료
        yield return new ExitPlayMode(); // 실제 회복 검사 환경 종료
    } // 공용 회복 신호와 방어도 검사 종료

    [UnityTest] // 성배 사용 이후 필수 카드 선택 시점과 공용 행동의 분리 검사
    public IEnumerator ChapelClothWaitsForRequiredChoiceAndRewardsOnlyTheLivingCupOwner() // 검은 예배포의 성배 정산과 대상 소유자 검증
    { // 성배 후속 선택과 방어도 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 후속 선택 버튼이 있는 실행 모드 진입
        yield return CheckChapelCloth(); // 플레이 모드 재로딩 뒤 카드와 선택 콜백의 상태 묶음 준비
        yield return new ExitPlayMode(); // 실제 성배 검사 환경 종료
    } // 성배 후속 선택과 방어도 검사 종료

    private static IEnumerator CheckChapelCloth() // 필수 손패 선택과 생존 성배 소유자의 실제 방어도 대조
    { // 로딩이 끝난 성배 전투의 검사 준비 시작
        Acquire("BlackChapelCloth"); // 검은 예배포 실제 유물 보유
        var original = Resources.Load<CardData>("StarterCards/MoonHandChoice"); // 손패에서 반드시 한 장을 버리는 기존 원본 카드
        Assert.IsNotNull(original); // 실제 게임에 들어 있는 선택 카드의 리소스 확인
        var card = Object.Instantiate(original); // 원래 카드의 분류를 보존하는 격리 검사 사본
        JsonUtility.FromJsonOverwrite($"{{\"cardType\":{(int)CardType.Cup}}}", card); // 같은 원래 선택을 성배 속성으로 정산할 검사 조건
        try // 원본 게임 자료는 그대로 유지하는 임시 사본 정리 보장
        { // 실제 손패와 정산 분기 구성 시작
            var sword = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(offer => offer.OfferId == "REWARD_SHEET_THRUST").Card; // 정상 대조 검 원본 조회
            using var fight = new Fight(new[] { card, sword, sword }); // 필수 선택의 남은 손패가 있는 실제 카드 전투
            var owner = fight.Allies[0]; // 성배 카드의 원래 생존 소유자
            var ally = fight.Allies[1]; // 카드 사용자가 아닌 별도 생존 아군
            var source = fight.Hand.GetComponentsInChildren<BattleCardView>().Single(view => view.RuntimeCard?.SourceData == card); // 실제 성배 기본 카드의 손패 입력 화면 조회
            var enemy = fight.Enemies[0]; // 검 사용이 끝나도 예배포가 보상하지 않을 적
            int points = fight.Points.CurrentActionPoints; // 모달 선택 없이 검을 취소할 때 보존할 기존 행동력
            var swordView = fight.View("THRUST"); // 사용 전후 비교할 실제 검 카드 화면
            swordView.OnPointerClick(null); // 실제 검 대상 입력 개시
            swordView.OnPointerClick(null); // 같은 카드 클릭으로 아직 사용하지 않은 상태 취소
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 취소한 일반 카드가 원래 비용도 내지 않음 확인
            Assert.Zero(owner.CurrentBlock); // 취소된 어떤 카드에도 방어도가 없음을 확인
            source.OnPointerClick(null); // 실제 성배 카드의 자기 사용과 후속 선택 개시
            var parent = fight.Hand.transform.parent; // 기존 선택 창이 표시되는 실제 전투 캔버스
            yield return WaitFor(() => parent.GetComponentInChildren<BattleDrawChoiceView>() != null); // 충돌 정산 뒤 필수 손패 선택 창을 기다림
            Assert.Zero(owner.CurrentBlock); // 실제 성배가 사용되었어도 필수 선택 전에는 보상 없음 확인
            var popup = parent.GetComponentInChildren<BattleDrawChoiceView>(); // 실제 후속 손패 버리기 화면 조회
            popup.GetComponentsInChildren<Button>(true).First(button => button.name.StartsWith("Choice_", StringComparison.Ordinal)).onClick.Invoke(); // 실제 필수 후보의 버튼 입력으로 성배 사용 정산 완료
            Assert.AreEqual(4, owner.CurrentBlock); // 성배 소유자에게만 방어도 네 점 지급 확인
            Assert.Zero(ally.CurrentBlock); // 다른 아군에게 사전 또는 사후 부여가 없는지 확인
            int before = enemy.CurrentHealth; // 남은 검의 실제 체력 피해 확인용 시작 체력
            yield return fight.Use("THRUST", 0); // 검 카드의 실제 공격과 원래 최종 정산
            Assert.Less(enemy.CurrentHealth, before); // 정상적으로 해결된 다른 카드 속성의 실제 사용 확인
            Assert.AreEqual(4, owner.CurrentBlock); // 해결한 검 카드가 예배포 방어도까지 주지 않음 확인
        } // 실제 손패와 정산 분기 구성 종료
        finally // 임시 카드 원본의 실제 정리 보장
        { // 검사 전용 카드 사본 정리 시작
            Object.Destroy(card); // 실제 등록된 원본을 건드리지 않은 임시 객체 제거
        } // 검사 전용 카드 사본 정리 종료
    } // 로딩이 끝난 성배 전투의 검사 준비 종료

    [UnityTest] // 실제 전투의 완전한 소환 성공과 등록 실패 환원 비교
    public IEnumerator DiningBellWeakensAllLivingEnemiesOnlyAfterSuccessfulSummonBinding() // 은제 식탁종의 실제 소환 완료 경계 확인
    { // 실제 씬과 소환 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사에서 전투로 진입할 실행 모드
        SceneManager.LoadScene("00_Boot"); // 정식 게임 시작 씬을 거쳐 원래 런타임 확보
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null); // 실제 시작 화면 준비 대기
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 원래 거점 진입 버튼 경로 실행
        yield return WaitFor(() => Object.FindFirstObjectByType<LobbyLayoutView>() != null); // 실제 거점의 선택 입력 초기화 대기
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 정상 파티의 실제 탐사 출발로 원본 전투 환경 구성
        yield return WaitFor(() => Object.FindFirstObjectByType<ExplorationPlayerController>() != null && SceneFlowManager.Instance?.IsLoadingScene == false); // 실제 탐사 캐릭터와 씬 대기
        Acquire("SilverDiningBell"); // 새 회차가 초기화된 뒤 은제 식탁종 실제 자산 획득
        var origin = Object.FindFirstObjectByType<ExplorationPlayerController>().transform.position; // 실제 탐사 지점의 원래 위치
        var encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 시작 적 한 명의 실제 전투 원본
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("day262_dining_bell", encounter, origin, origin)); // 실제 탐사 조우에서 기존 전투 데이터 획득
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 정상 전투 씬 진입
        yield return WaitFor(() => Object.FindFirstObjectByType<BattleSceneSetup>()?.IsInitialized == true); // 원래 전투 유닛과 모든 신호 연결 대기
        var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 적 소환과 기존 예고를 담당하는 실제 씬 관리자
        var original = battle.EnemyUnits[0]; // 전투 개막의 생존 적이며 소환 보너스의 기존 대상
        var ally = battle.AllyUnits[0]; // 실제 명중률을 준비할 생존 아군
        Assert.IsNull(original.GetStatusEffect(BattleStatusEffectType.EvasionDown)); // 첫 전투 적 생성이 중간 소환으로 처리되지 않음 확인
        Assert.IsFalse(battle.TrySummonEnemy(null)); // 실제 소환 원본이 없는 기본 실패 입력
        Assert.IsNull(original.GetStatusEffect(BattleStatusEffectType.EvasionDown)); // 실패한 소환 직후 원래 적에 약화 없음 확인
        original.ApplyStatusEffect(BattleStatusEffectType.Evasion, 20, 3, 1); // 회피율에서 다섯 점 줄어드는 일반 계산 결과 확인 준비
        int hitChance = BattleHitChance.GetPercent(ally, original); // 새 적 등록 전의 진짜 명중률 기준 확인
        var summon = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter").Enemies[0]; // 실제 소환 가능한 새 적 원본
        Assert.IsTrue(battle.TrySummonEnemy(summon)); // 모든 승패와 상태 및 화면 등록을 마친 실제 새 적 소환
        var spawned = battle.EnemyUnits[battle.EnemyUnits.Count - 1]; // 정상 등록 경로로 끝난 새 생존 적 조회
        Assert.AreEqual(hitChance + 5, BattleHitChance.GetPercent(ally, original)); // 식탁종이 부여한 상태의 실제 회피 공식 연결 확인
        foreach (var enemy in new[] { original, spawned }) // 시작 적 및 실제 소환 적의 동시 약화 확인
        { // 생존 적별 실제 회피 약화 확인 시작
            var evasion = enemy.GetStatusEffect(BattleStatusEffectType.EvasionDown); // 기존 상태 규칙을 적용한 적 회피 약화
            Assert.IsNotNull(evasion); // 정상 소환 이후 생존 적 모두의 상태 생성 확인
            Assert.AreEqual(5, evasion.EffectiveValue); // 회피율 오 점 약화 원본 수치 확인
            Assert.AreEqual(2, evasion.RemainingTurns); // 정확한 두 적 턴 상태 지속 확인
            enemy.RemoveStatusEffect(BattleStatusEffectType.EvasionDown); // 뒤이은 소환 실패가 다시 상태를 주는지 비교할 초기화
        } // 생존 적별 실제 회피 약화 확인 종료
        var field = typeof(BattleSceneSetup).GetField("mentalController", BindingFlags.Instance | BindingFlags.NonPublic); // 일부 등록 실패를 재현할 실제 전투의 정신력 연결점
        Assert.IsNotNull(field); // 실제 등록 호출 대상 확인
        var originalController = field.GetValue(battle); // 복구할 원래 정상 정신력 관리자 저장
        int livingCount = battle.EnemyUnits.Count; // 부분 등록 실패 전의 실제 적 수 기록
        try // 시험용 관리자 연결 변경 이후 원래 씬 상태 복구 보장
        { // 부분 등록이 실패하는 실제 소환 실행 시작
            field.SetValue(battle, null); // 실제 소환 등록 한 단계만 고의로 누락
            LogAssert.Expect(LogType.Error, "[BattleSceneSetup] 소환 적의 전투 연결에 실패했습니다."); // 기존 소환 연결 복구 오류 문구 예상
            Assert.IsFalse(battle.TrySummonEnemy(summon)); // 부분 등록된 적의 기존 롤백 경로 확인
            Assert.AreEqual(livingCount, battle.EnemyUnits.Count); // 실패 소환의 적 슬롯이 원래 수로 돌아가는지 확인
            Assert.IsNull(original.GetStatusEffect(BattleStatusEffectType.EvasionDown)); // 등록 실패에 따라 식탁종이 발동하지 않음 확인
            Assert.IsNull(spawned.GetStatusEffect(BattleStatusEffectType.EvasionDown)); // 이미 소환된 생존 적에도 잘못된 추가 적용 없음 확인
        } // 부분 등록이 실패하는 실제 소환 실행 종료
        finally // 실제 등록 관리자의 원래 씬 연결 복원
        { // 정상 씬 참조 복구 시작
            field.SetValue(battle, originalController); // 후속 테스트 및 전투 종료 경로의 정상 관리자 복구
        } // 정상 씬 참조 복구 종료
        yield return new ExitPlayMode(); // 실제 전투 씬 검사 종료
    } // 실제 씬과 소환 유물 검사 종료

    private static RelicRunManager Acquire(string key) // 실제 회차 보관함에 등록 유물 획득
    { // 정상 유물 획득 경로 시작
        var asset = Resources.Load<RelicData>("Relics/" + key); // 실제 배포할 유물 리소스 읽기
        Assert.IsNotNull(asset, key); // 유물과 원본 자산 등록 여부 확인
        var run = RelicRunManager.EnsureInstance(); // 실제 탐사에 쓰는 회차 유물 관리자
        Assert.AreEqual(RelicAcquireResult.Acquired, run.TryAcquire(asset)); // 현재 실제 회차에서 유물 획득 확인
        return run; // 회복 이벤트에 연결할 현재 회차 관리자의 반환
    } // 정상 유물 획득 경로 종료

    private static IEnumerator WaitFor(Func<bool> condition) // 정해진 시간 안에 실제 전투 화면 또는 후속 버튼 준비 대기
    { // 프레임 대기 시작
        float end = Time.realtimeSinceStartup + 30f; // 일반 씬과 카드 연출의 실제 최대 대기 시간
        while (!condition() && Time.realtimeSinceStartup < end) // 현재 원본 화면이 준비될 때까지 실제 프레임 확인
        { // 다음 프레임 대기 시작
            yield return null; // 실제 전투 화면 한 프레임 진행
        } // 다음 프레임 대기 종료
        Assert.IsTrue(condition()); // 유한 시간 내 실제 초기화 완료 확인
    } // 프레임 대기 종료
} // 세 지원 유물의 플레이 회귀 검사 종료
