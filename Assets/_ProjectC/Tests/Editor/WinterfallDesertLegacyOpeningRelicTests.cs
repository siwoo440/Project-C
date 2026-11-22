using System; // 임시 전투 생성과 실제 씬 대기 도구
using System.Collections; // 기존 Unity 플레이 모드 전환과 프레임 대기
using System.Linq; // 실제 전투와 원본 카드 조회
using System.Reflection; // 일부 소환 단계의 실패 롤백 재현
using NUnit.Framework; // 전투 효과와 실제 상태를 함께 비교
using UnityEngine; // 배포 리소스와 테스트 전용 자료 준비
using UnityEngine.SceneManagement; // 실제 부트와 전투 씬 진입
using UnityEngine.TestTools; // 원래 플레이 모드 테스트 등록
using Object = UnityEngine.Object; // 실제 Unity 객체와 임시 에셋 해제
using Random = UnityEngine.Random; // 독립 검사 전 난수 상태 저장

public sealed class WinterfallDesertLegacyOpeningRelicTests // 후반 사막 개막과 소환 및 아군의 실제 턴 효과
{ // 네 유물의 플레이 회귀 검사 시작
    [UnityTest] // 선제 전에 시작한 기존 화상의 실제 두 가지 수명 확인
    public IEnumerator ColdCoreAppliesNativeBurnBeforeAmbushWithNormalDecayAndImmunity() // 양 진영의 개막 화상과 지속 피해 경계
    { // 실제 화상과 처음 턴의 적용 검사 시작
        yield return new EnterPlayMode(); // 실제 리소스와 두 진영의 전투 생성 환경 진입
        Acquire("BlackSunColdCore"); // 배포 원본 흑태양 핵의 보유 확인
        using (var fight = new OpeningFight(BattleType.Normal)) // 양 진영의 기존 상태 끝 처리까지 연결
        { // 두 진영의 실제 개막 처리 시작
            var ally = fight.Allies[0]; // 먼저 살아 있는 실제 화상 아군
            var enemy = fight.Enemies[0]; // 두 차례 끝에 화상을 받는 생존 적
            var immune = fight.Enemies[1]; // 실제 상태 면역으로 보호된 적
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 정식 상태 면역의 활성 대상 준비
            Assert.IsTrue(fight.Allies[2].ApplyPersistentHealth(0)); // 처음에 쓰러져 있는 아군의 대상 제외 준비
            fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 살아 있지 않은 적의 대상 제외 준비
            int allyHealth = ally.CurrentHealth; // 아군의 첫 화상 전에 체력 확인
            int enemyHealth = enemy.CurrentHealth; // 적 첫 화상 피해의 대조 수치
            Random.State expected = Random.state; // 실제 새 유물의 불필요한 무작위 여부 확인
            Assert.IsTrue(fight.Turn.StartBattle(1, true)); // 첫 적 행동보다 앞선 개막 화상 적용
            Assert.AreEqual(expected, Random.state); // 별도의 유물 확률과 대상 선택 난수 미사용
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.Burn)?.RemainingTurns); // 처음 살아 있는 아군의 한 차례 화상
            Assert.AreEqual(2, enemy.GetStatusEffect(BattleStatusEffectType.Burn)?.RemainingTurns); // 살아 있는 적의 두 차례 화상
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Burn)); // 정식 상태 면역에 의해 화상 없음
            Assert.IsNull(fight.Allies[2].GetStatusEffect(BattleStatusEffectType.Burn)); // 쓰러진 아군에게 효과 없음
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.Burn)); // 쓰러진 적에게 효과 없음
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 실제 적 선제 종료에서 첫 피해 틱 적용
            Assert.AreEqual(enemyHealth - 2, enemy.CurrentHealth); // 최초 적 화상 두 점 피해
            Assert.AreEqual(1, enemy.GetStatusEffect(BattleStatusEffectType.Burn)?.EffectiveValue); // 기본 감쇠 뒤 남은 한 점 화상
            Assert.AreEqual(allyHealth, ally.CurrentHealth); // 아군은 자신의 첫 턴이 끝날 때까지 피해 없음
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 첫 실제 아군 턴 종료와 화상 피해
            Assert.AreEqual(allyHealth - 2, ally.CurrentHealth); // 아군 최초 한 번만 실제 피해 처리
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Burn)); // 아군 화상은 다음 턴으로 저장되지 않음
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 적의 두 번째 실제 턴 종료
            Assert.AreEqual(enemyHealth - 3, enemy.CurrentHealth); // 적 둘째 감쇠 피해 한 점까지 실제 적용
            Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.Burn)); // 두 적 실제 턴 이후 네이티브 상태 제거
        } // 두 진영의 실제 개막 처리 종료
        yield return new ExitPlayMode(); // 등록 원본과 결과 이후 플레이 검사 종료
    } // 실제 화상과 처음 턴의 적용 검사 종료

    [UnityTest] // 다섯 번 완결된 실제 아군 턴 이후만 적용하는 양 진영 효과
    public IEnumerator EndlessBellStartsOnSixthAllyTurnAndContinuesAfterAmbush() // 적 선제와 실제 턴 도착의 차이 및 반복
    { // 늦은 실제 전투 턴 유물 검사 시작
        yield return new EnterPlayMode(); // 양 진영 실제 턴 처리 사용
        Acquire("EndlessPilgrimBell"); // 배포 원본 순례자의 종 획득
        foreach (bool enemyFirst in new[] { false, true }) // 일반 개막과 적 선제 조우 모두 대조
        { // 실제 독립 전투의 첫 다섯 턴 준비 시작
            using var fight = new OpeningFight(BattleType.Normal); // 고정 덱과 실제 양 진영 종료 처리
            var ally = fight.Allies[0]; // 회복 여유가 있는 생존 아군
            var enemy = fight.Enemies[0]; // 정신 피해를 받을 생존 적
            Assert.IsTrue(ally.ApplyPersistentHealth(ally.MaxHealth - 20)); // 두 번 회복을 정량 검증할 체력 여유
            Assert.IsTrue(fight.Allies[2].ApplyPersistentHealth(0)); // 일반 회복에 대상이 아닌 전투불능 아군
            int health = ally.CurrentHealth; // 처음 정상 체력의 비교 수치
            int mental = enemy.CurrentMental; // 처음 적 정신력의 비교 수치
            Assert.IsTrue(fight.Turn.StartBattle(1, enemyFirst)); // 지정된 첫 진영과 실제 처음 라운드
            if (enemyFirst) // 선제 중 아직 어느 아군 차례도 마치지 않은 경우
            { // 적 선제를 실제 완료한 다음 첫 아군 진입 시작
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 첫 적 진영 이후 동일한 첫 아군 차례
            } // 적 선제를 실제 완료한 다음 첫 아군 진입 종료
            for (int actual = 1; actual <= 5; actual++) // 완료되어야 할 첫 다섯 실제 아군 차례 확인
            { // 지금 아군 차례와 적 차례의 변화 없음 검사 시작
                Assert.AreEqual(health, ally.CurrentHealth); // 늦은 효과의 초기 아군 체력 보존
                Assert.AreEqual(mental, enemy.CurrentMental); // 늦은 효과의 초기 적 정신력 보존
                Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 현재 아군 차례 실제 완료
                Assert.AreEqual(health, ally.CurrentHealth); // 적 차례에 같은 효과를 반복하지 않음
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 다음 실제 아군 시작으로 이동
            } // 지금 아군 차례와 적 차례의 변화 없음 검사 종료
            Assert.AreEqual(health + 5, ally.CurrentHealth); // 여섯 번째 실제 아군 시작 회복 다섯 점
            Assert.AreEqual(mental - 3, enemy.CurrentMental); // 실제 생존 적에게 독립 정신력 감소 세 점
            Assert.Zero(fight.Allies[2].CurrentHealth); // 일반 회복으로 쓰러진 아군 부활 없음
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 여섯 번째 아군 턴 종료
            Assert.AreEqual(health + 5, ally.CurrentHealth); // 적 행동에 효과를 중복 적용하지 않음
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 일곱 번째 실제 아군 시작
            Assert.AreEqual(health + 10, ally.CurrentHealth); // 첫 한 번만이 아닌 다음 아군 턴에도 실제 회복
            Assert.AreEqual(mental - 6, enemy.CurrentMental); // 다음 실제 생존 적 정신력 감소의 반복 확인
        } // 실제 독립 전투의 첫 다섯 턴 준비 종료
        yield return new ExitPlayMode(); // 실제 게임 플레이 검사 종료
    } // 늦은 실제 전투 턴 유물 검사 종료

    [UnityTest] // 이전 실제 활성 은신에서 인스턴스 진입이 달라진 시점만 적용
    public IEnumerator WraithHoodRestoresMentalOnlyOnNewLivingAllyStealth() // 전투 밖과 아군 전투 중 갱신 및 적 제외
    { // 새로운 네이티브 은신 효과 검사 시작
        yield return new EnterPlayMode(); // 실제 상태 변화 이벤트와 등록 유물 환경
        Acquire("DesertWraithHood"); // 배포된 사막 망령 후드의 실제 보유
        using (var fight = new OpeningFight(BattleType.Normal)) // 정상 전투에서 실제 상태 효과 적용
        { // 생존 아군과 적의 신규 상태 경계 비교 시작
            var ally = fight.Allies[0]; // 실제 정신력 회복을 받을 아군
            var enemy = fight.Enemies[0]; // 같은 상태라도 혜택을 받지 않아야 할 적
            int baseline = ally.CurrentMental; // 실제 증감량 기준의 아군 정신력
            int enemyMental = enemy.CurrentMental; // 혜택이 없는 적의 정신력 기준
            ally.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 2, 1); // 전투 시작 전의 상태 적용
            Assert.AreEqual(baseline, ally.CurrentMental); // 실제 시작 이전에는 후드 정신력 효과 없음
            Assert.IsTrue(ally.RemoveStatusEffect(BattleStatusEffectType.Stealth)); // 활성 전투의 새 은신을 준비할 이전 상태 제거
            Assert.IsTrue(fight.Turn.StartBattle(1)); // 아군이 먼저 행동하는 실제 전투 시작
            ally.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 2, 1); // 정상 생존 아군의 새 은신 진입
            Assert.AreEqual(baseline + 3, ally.CurrentMental); // 기존 정신력 경로를 따른 한 번의 실제 회복
            ally.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 3, 1); // 이미 살아 있는 같은 인스턴스 지속 갱신
            Assert.AreEqual(baseline + 3, ally.CurrentMental); // 단순 지속 연장에 새 혜택 중복 없음
            enemy.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 1, 1); // 같은 네이티브 은신이 걸린 생존 적
            Assert.AreEqual(enemyMental, enemy.CurrentMental); // 실제 아군 후드 효과를 적에게 지급하지 않음
            Assert.IsTrue(ally.RemoveStatusEffect(BattleStatusEffectType.Stealth)); // 실제 은신 종료를 통해 두 번째 진입 경계 준비
            ally.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 2, 1); // 동일 아군의 두 번째 실제 진입
            Assert.AreEqual(baseline + 6, ally.CurrentMental); // 새로운 진입마다 한 번씩만 실제 정신력 회복
        } // 생존 아군과 적의 신규 상태 경계 비교 종료
        yield return new ExitPlayMode(); // 테스트 전용 전투 종료
    } // 새로운 네이티브 은신 효과 검사 종료

    [UnityTest] // 정식 전투 씬에서 소환 완료에만 전투 누적 강화 검사
    public IEnumerator SandQueenBannerGrowsOnlyAfterFullEnemySummonRegistration() // 초기 배치와 부분 실패 및 실제 두 번의 소환
    { // 실제 조우의 소환 전후 및 상태 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사에서 전투 씬을 여는 정상 진입
        SceneManager.LoadScene("00_Boot"); // 게임의 실제 부트 및 메뉴 불러오기
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null); // 실제 메인 메뉴 준비
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 메인 메뉴에서 거점으로 이동
        yield return WaitFor(() => Object.FindFirstObjectByType<LobbyLayoutView>() != null); // 정상 파티 입력이 있는 거점 화면
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 정상 파티를 배치하고 탐사 출발
        yield return WaitFor(() => Object.FindFirstObjectByType<ExplorationPlayerController>() != null && SceneFlowManager.Instance?.IsLoadingScene == false); // 실제 탐사의 표시 준비
        Acquire("SandQueenBanner"); // 실제 원본 명령패를 정상 탐사 회차에서 획득
        var origin = Object.FindFirstObjectByType<ExplorationPlayerController>().transform.position; // 실제 시작 지점
        var encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 적 하나가 있는 실제 열린 조우 원본
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("day276_sand_queen_banner", encounter, origin, origin)); // 실제 탐사 대기 조우와 씬 연결
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 게임의 실제 전투 화면 진입
        yield return WaitFor(() => Object.FindFirstObjectByType<BattleSceneSetup>()?.IsInitialized == true); // 원래 전투의 진영 배치와 신호 생성 대기
        var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 게임의 적과 소환 상태 연결 관리자
        var ally = battle.AllyUnits[0]; // 정상 소환에 따라 공격력 올라갈 생존 아군
        var fallen = battle.AllyUnits[1]; // 전투불능 시 받지 말아야 할 아군
        Assert.IsTrue(fallen.ApplyPersistentHealth(0)); // 기존에 쓰러진 실제 아군 상태
        int attack = ally.AttackPowerBonus; // 아군의 기존 실제 공격력 값
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.SandQueenSummonAttackGrow)); // 처음 나오는 전투 적의 일반 배치에는 미발동
        Assert.IsFalse(battle.TrySummonEnemy(null)); // 첫 실제 무효 소환에서 상태 유지
        Assert.AreEqual(attack, ally.AttackPowerBonus); // 무효 원본 사용 후 공격력의 실제 불변
        var summon = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter").Enemies[0]; // 실제 소환할 게임 적 원본
        Assert.IsTrue(battle.TrySummonEnemy(summon)); // 기존 전투 관리자 등록 전부가 성공하는 정상 소환
        Assert.AreEqual(attack + 1, ally.AttackPowerBonus); // 생존 아군의 기존 일반 공격력에 실제 한 점 합산
        Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.SandQueenSummonAttackGrow)); // 이미 쓰러진 아군 제외
        var field = typeof(BattleSceneSetup).GetField("mentalController", BindingFlags.Instance | BindingFlags.NonPublic); // 소환 중 일부 단계 실패를 재현할 실제 필드
        Assert.IsNotNull(field); // 정상 스크립트의 등록 필드 확인
        var mental = field.GetValue(battle); // 이후 진짜 성공에 복구할 원래 정신력 관리자
        try // 검증을 위한 한 단계 실패에서 원래 참조 복구 보장
        { // 실제 실패 소환과 롤백 실행 시작
            field.SetValue(battle, null); // 전투 소환 등록이 일부만 성공하는 입력
            LogAssert.Expect(LogType.Error, "[BattleSceneSetup] 소환 적의 전투 연결에 실패했습니다."); // 기존 롤백의 실제 메시지
            Assert.IsFalse(battle.TrySummonEnemy(summon)); // 실제 소환 실패와 생성했던 적의 제거
            Assert.AreEqual(attack + 1, ally.AttackPowerBonus); // 실패한 등록은 실제 누적을 늘리지 않음
        } // 실제 실패 소환과 롤백 실행 종료
        finally // 실패를 재현한 뒤 기존 실제 상태 복구
        { // 정상 정신력 관리 복구 시작
            field.SetValue(battle, mental); // 이전 정식 참조 복구
        } // 정상 정신력 관리 복구 종료
        Assert.IsTrue(battle.TrySummonEnemy(summon)); // 롤백과 별개인 두 번째 진짜 소환 성공
        Assert.AreEqual(attack + 2, ally.AttackPowerBonus); // 이전과 다른 실제 성공에서 전투 중 누적 적용
        Assert.IsTrue(ally.GetStatusEffect(BattleStatusEffectType.SandQueenSummonAttackGrow).LastsUntilBattleEnd); // 게임 UI와 지속 처리에 쓸 전투 전용 상태 계약
        yield return new ExitPlayMode(); // 원래 게임 씬에서의 검증을 끝낸 뒤 모드 종료
    } // 실제 조우의 소환 전후 및 상태 검사 종료

    private static IEnumerator WaitFor(Func<bool> condition) // 정해진 시간 안에 실제 전투 화면 또는 후속 버튼 준비 대기
    { // 프레임 대기 시작
        float end = Time.realtimeSinceStartup + 30f; // 일반 씬과 카드 연출의 실제 최대 대기 시간
        while (!condition() && Time.realtimeSinceStartup < end) // 현재 원본 화면이 준비될 때까지 실제 프레임 확인
        { // 다음 프레임 대기 시작
            yield return null; // 실제 전투 화면 한 프레임 진행
        } // 다음 프레임 대기 종료
        Assert.IsTrue(condition()); // 유한 시간 내 실제 초기화 완료 확인
    } // 프레임 대기 종료

    private static void Acquire(string key) // 실제 Unity 원본 유물의 일반 보유 등록
    { // 유물 획득 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 원본 유물의 실제 고유 리소스
        Assert.IsNotNull(relic, key); // 실제 메타와 리소스 연결 검증
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 유효 탐사 보유의 실제 획득 확인
    } // 유물 획득 준비 종료

    private sealed class OpeningFight : IDisposable // 실제 진영 시작과 상태 만료를 사용하는 독립 전투
    { // 테스트 전투 생성 시작
        private readonly Random.State randomState = Random.state; // 독립 전투 외부의 원래 난수 복원을 위한 상태
        private readonly DeckData source; // 원래 카드 지급을 위한 실제 전투 원본 덱
        private readonly EnemyData enemyData; // 독립 충돌에 사용할 일시 적 원본
        private readonly BattleStatusEffectController statuses; // 턴에 따라 상태를 자연스럽게 제거하는 실제 관리자
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(3); // 고정 기본값과 임시 한도를 보여 주는 실제 행동력
        internal readonly BattleUnitRuntime[] Allies; // 개별 치명 보호의 실제 생존 검사 대상
        internal readonly BattleUnitRuntime[] Enemies; // 독립 공포 장면을 나타내는 실제 네 적
        internal readonly BattleTurnRuntime Turn; // 실제 시작과 선제 및 상태 수명을 처리하는 관리자

        internal OpeningFight(BattleType type) // 기존 일반 및 정예와 보스 전투 분류 연결
        { // 실제 턴 검사 구성 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 평소 캐릭터의 실제 명단
            Allies = roster.Characters.Take(3).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 서로 다른 실제 아군 목록
            enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 다른 게임 원본을 수정하지 않는 검사 자료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200}", enemyData); // 비교 임계치를 위한 적 체력 설정
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemyData)).ToArray(); // 모두 개별 상태 슬롯을 가진 실제 적
            var thrust = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards.Single(card => card.CardId == "CRD_SHEET_THRUST"); // 실제 손패에 들어가는 표준 원본 카드
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(thrust, Allies[0].CharacterSource) }); // 같은 진영의 실제 카드 원본 덱
            var deck = BattleDeckRuntime.Create(source, Allies, 4, 276); // 일정한 초기 덱으로 재생할 실제 카드 더미 생성
            Turn = new BattleTurnRuntime(deck, Points, Allies, Enemies, 1, type); // 요청한 조우 분류의 실제 이전 턴 처리
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 원래 턴 시작과 만료 상태 계산 연결
        } // 실제 턴 검사 구성 종료

        public void Dispose() // 실제 전투의 자원 및 검사 전 난수 복구
        { // 원본 없는 테스트 객체 해제 시작
            statuses.Dispose(); // 실제 상태의 진영 이벤트 등록 해제
            Turn.Dispose(); // 개막 전투 및 런타임 덱 연결 정리
            Object.Destroy(source); // 생성한 테스트 전용 덱 해제
            Object.Destroy(enemyData); // 만든 검사 전용 적 원본 해제
            Random.state = randomState; // 다음 독립 검사에 영향을 주지 않는 난수 초기화
        } // 원본 없는 테스트 객체 해제 종료
    } // 테스트 전투 생성 종료
} // 네 유물의 플레이 회귀 검사 종료
