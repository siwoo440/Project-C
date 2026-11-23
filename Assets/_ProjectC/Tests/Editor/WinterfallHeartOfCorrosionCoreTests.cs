using System; // 전투 자료 정리와 독립 저장 이름 사용
using System.Collections; // 실제 탐사 플레이 모드 전환 대기
using System.Collections.Generic; // 초기 적과 소환 적의 전투 목록 구성
using System.IO; // 실제 탐사 저장 파일 검증
using System.Linq; // 기존 조우 원본과 아군 자료 연결
using NUnit.Framework; // 처치 횟수와 저장 결과 검증
using UnityEngine; // 게임 원본과 전투 참가 자료 사용
using UnityEngine.TestTools; // 실제 탐사 관리자 활성화 검사
using Object = UnityEngine.Object; // 격리 검사 원본 자산 해제

public sealed class WinterfallHeartOfCorrosionCoreTests // 작은 침식핵의 심장 처치와 지속 공격력 검사
{ // 심장 유물 회귀 검사 시작
    [UnityTest] // 실제 등급과 전투 런타임 정산의 영구 공격력 검사
    public IEnumerator RankedDefeatsGrantOneAttackPowerToAllAlliesAcrossRunBattles() // 정상 처치와 임시 적 및 회차 결합 확인
    { // 정예와 보스 처치 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사와 전투 관리자가 동작하는 모드 시작
        VerifyDefeats(); // 정예·보스와 일반 적의 실제 피해 경로 검사
        yield return new ExitPlayMode(); // 전투 해제 뒤 편집 모드 복귀
    } // 정예와 보스 처치 검사 종료

    private static void VerifyDefeats() // 실제 전투에서 등급과 유물 보유 조건을 대조
    { // 처치와 유물 조건 확인 시작
        var session = ExplorationSessionManager.EnsureInstance(); // 실제 탐사 회차 상태 관리자
        session.ResetExploration(); // 앞선 탐사 효과와 무관한 새 회차 준비
        var relic = AcquireHeart(); // 정상 보유 절차로 침식핵 심장 획득
        var inventory = RelicRunManager.Instance; // 획득과 제거에 사용할 실제 유물 관리자
        using (var elite = new RunBattle(session, "Encounter_WF_AshSentinel", 2)) // 같은 원본의 정예 두 개체가 있는 실제 전투
        { // 정예 전투별 실제 개체 감시 시작
            var summon = elite.Summon(); // 전투 목표와 별개의 실제 소환 적 등록
            summon.TakeDamage(10000, BattleDamageType.None, elite.Allies[0]); // 정예 조우에서 생성된 일반 소환 적 실제 처치
            Assert.AreEqual(0, elite.Allies[0].AttackPowerBonus); // 소환 적의 처치에 영구 공격력 없음 확인
            elite.Enemies[0].TakeDamage(1, BattleDamageType.None, elite.Allies[0]); // 정예의 체력을 조금 줄이는 정상 비치명 피해
            Assert.AreEqual(0, elite.Allies[0].AttackPowerBonus); // 피격이나 처치하지 않은 피해의 보너스 제외 확인
            elite.Enemies[0].TakeDamage(10000, BattleDamageType.None, elite.Allies[0]); // 생존 정예의 첫 실제 처치
            Assert.AreEqual(1, elite.Allies[0].AttackPowerBonus); // 첫 정예 피해 발생자의 영구 공격력 확인
            Assert.AreEqual(1, elite.Allies[1].AttackPowerBonus); // 실제 가해자가 아닌 파티원에게도 같은 공격력 확인
            Assert.AreEqual(0, elite.Enemies[1].AttackPowerBonus); // 같은 전투의 적은 파티 공격력에서 제외 확인
            elite.Enemies[0].TakeDamage(10000, BattleDamageType.None, elite.Allies[0]); // 이미 처치한 같은 개체에 피해 요청
            Assert.AreEqual(1, elite.Allies[0].AttackPowerBonus); // 같은 개체 재타격으로 처치 보상이 반복되지 않음 확인
            elite.Enemies[1].TakeDamage(10000, BattleDamageType.None, elite.Allies[1]); // 같은 원본의 다른 정예 개체를 실제 처치
            Assert.AreEqual(0, elite.Allies[0].AttackPowerBonus); // 결과 확정 뒤 종료된 전투 수치에 탐사 공격력이 남지 않음 확인
            elite.ResolveExplorationResult(); // 실제 탐사 결과 정산으로 다음 조우 진입 준비
        } // 정예 전투별 실제 개체 감시 종료
        Assert.IsTrue(inventory.TryRemove(relic.RelicId)); // 실제 유물 제거 뒤 획득한 보너스 유지 확인 준비
        using (var ordinary = new RunBattle(session, "Encounter_WF_AshHunter")) // 누적 보너스를 사용하지만 일반 적만 존재하는 전투
        { // 일반 조우의 보너스와 보유 조건 확인 시작
            Assert.AreEqual(2, ordinary.Allies[0].AttackPowerBonus); // 새 전투와 유물 제거 뒤에도 이전 처치 두 점 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 같은 회차의 정상 재획득 절차
            ordinary.Enemies[0].TakeDamage(10000, BattleDamageType.None, ordinary.Allies[0]); // 유물을 들고 일반 적 실제 처치
            Assert.AreEqual(BattleResult.Victory, ordinary.Turns.Result); // 유물을 다시 보유한 일반 적의 실제 처치 성공 확인
            ordinary.ResolveExplorationResult(); // 실제 탐사의 일반 조우 결과 정산
        } // 일반 조우의 보너스와 보유 조건 확인 종료
        using (var boss = new RunBattle(session, "Encounter_WF_AshstormCore")) // 실제 조우 원본의 보스 등급 전투
        { // 보스 패배와 정상 피해 구분 시작
            var target = boss.Enemies[0]; // 실제 보스 적 한 명
            target.TakeDamage(target.MaxHealth / 2, BattleDamageType.None, boss.Allies[0]); // 처치 이전의 생존 보스 체력 경계
            Assert.AreEqual(2, boss.Allies[1].AttackPowerBonus); // 일반 적 처치와 살아 있는 보스 피해에 추가 보상 없음 확인
            target.TakeDamage(10000, BattleDamageType.None); // 별도 발생자가 없는 기존 지속 및 유물 피해 경로의 실제 보스 처치
            boss.ResolveExplorationResult(); // 실제 보스 종료 결과를 탐사에 전달
        } // 보스 패배와 정상 피해 구분 종료
        using (var afterBoss = new RunBattle(session, "Encounter_WF_AshTracker")) // 보스의 마지막 타격으로 받은 보상을 사용하는 실제 다음 조우
        { // 보스의 회차 정산 범위 확인 시작
            Assert.AreEqual(3, afterBoss.Allies[0].AttackPowerBonus); // 마지막 적인 보스의 확정 처치에서 세 번째 보상 확인
            Assert.AreEqual(3, afterBoss.Allies[1].AttackPowerBonus); // 다음 전투 파티에 남은 같은 공격력 확인
        } // 보스의 회차 정산 범위 확인 종료
        using var standalone = new RunBattle(null, "Encounter_WF_AshSentinel"); // 탐사 바인딩이 없는 독립 전투
        standalone.Enemies[0].TakeDamage(10000, BattleDamageType.None, standalone.Allies[0]); // 독립 전투의 정예 원본 실제 처치
        Assert.AreEqual(0, standalone.Allies[0].AttackPowerBonus); // 탐사 외부 전투에서 공격력 및 획득 경로 격리 확인
        session.ResetExploration(); // 실제 새 회차 출발의 상태 초기화
        using var next = new RunBattle(session, "Encounter_WF_AshHunter"); // 새 회차의 일반 전투 시작
        Assert.AreEqual(0, next.Allies[0].AttackPowerBonus); // 유물 보유와 별개인 새 회차 초기 영구 공격력 확인
    } // 처치와 유물 조건 확인 종료

    [UnityTest] // 실제 저장과 이전 버전 기본값의 호환 검사
    public IEnumerator ExplorationSaveRestoresEarnedBonusAndDefaultsOldSavesToZero() // 실제 탐사 저장 파일 왕복과 필드 누락 호환
    { // 저장 왕복 검사 시작
        yield return new EnterPlayMode(); // 실제 자산과 저장 관리자가 동작하는 모드 진입
        VerifySaveRoundTrip(); // 획득·제거 뒤 저장과 이전 파일 형식 로드
        yield return new ExitPlayMode(); // 독립 파일 정리 뒤 편집 모드 복귀
    } // 저장 왕복 검사 종료

    private static void VerifySaveRoundTrip() // 원본 저장 서비스와 실제 전투의 보너스 대조
    { // 저장 호환 검사 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 저장 원본에서 유효한 캐릭터 목록
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 출전 파티 선택 준비
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 영입과 출전 조건에 맞는 실제 캐릭터 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 출전 파티와 기본 카드 덱 확정
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 정상 저장에 필요한 회차 덱 연결
        var session = ExplorationSessionManager.EnsureInstance(); // 현재 실제 탐사 회차 상태
        session.ResetExploration(); // 기존 보너스 없는 독립 저장 준비
        session.SetCurrentFloorSeed(260); // 정상 저장을 지원하는 실제 현재 층의 고정 시드
        var relic = AcquireHeart(); // 획득한 공격력을 남기기 위한 정상 보유 유물
        using (var fight = new RunBattle(session, "Encounter_WF_AshBellkeeper")) // 실제 정예 원본을 사용한 현재 회차 교전
        { // 저장할 실제 정예 처치 시작
            fight.Enemies[0].TakeDamage(10000, BattleDamageType.None, fight.Allies[0]); // 회차에 한 점을 줄 실제 처치
            fight.ResolveExplorationResult(); // 닫힌 전투에서 정상 탐사 저장 상태로 복귀
        } // 저장할 실제 정예 처치 종료
        Assert.IsTrue(RelicRunManager.Instance.TryRemove(relic.RelicId)); // 저장에서 유물 원본이 없어도 영구 수치 복원 확인 준비
        string directory = Path.Combine(Application.temporaryCachePath, "heart-of-core-" + Guid.NewGuid().ToString("N")); // 이번 검사에만 쓰는 독립 저장 폴더
        string current = Path.Combine(directory, "current.json"); // 새 결과를 기록할 실제 저장 경로
        string legacy = Path.Combine(directory, "legacy.json"); // 구형 저장 구조를 재현할 별도 경로
        try // 검사 파일의 종료 후 정리 보장
        { // 실제 저장과 복원 처리 시작
            Assert.IsTrue(ExplorationSaveService.Save(current, Vector3.zero)); // 실제 원본 저장 기능으로 회차 기록
            string json = File.ReadAllText(current); // 원본 서비스가 쓴 실제 JSON 조회
            StringAssert.Contains("\"permanentAttackPower\":1", json); // 보유 유물과 별개로 보관한 공격력 필드 확인
            session.ResetExploration(); // 가장 최근 런타임의 공격력 초기화
            Assert.IsTrue(ExplorationSaveService.Load(current)); // 실제 저장으로부터 현재 탐사 회차 복원
            using (var restored = new RunBattle(session, "Encounter_WF_AshHunter")) // 실제 복원 회차에서 싸우는 다음 일반 조우
            { // 저장에서 읽은 공격력 확인 시작
                Assert.AreEqual(1, restored.Allies[0].AttackPowerBonus); // 유물 보유와 무관한 실제 저장 수치 반영 확인
            } // 저장에서 읽은 공격력 확인 종료
            File.WriteAllText(legacy, json.Replace(",\"permanentAttackPower\":1", string.Empty)); // 영구 수치 필드가 없는 앞선 저장 형식 재현
            Assert.IsTrue(ExplorationSaveService.Load(legacy)); // 같은 버전의 기존 파일 구조도 읽을 수 있는지 확인
            using var old = new RunBattle(session, "Encounter_WF_AshHunter"); // 이전 형식으로 복원한 현재 회차의 실제 전투
            Assert.AreEqual(0, old.Allies[0].AttackPowerBonus); // 필드 없는 이전 저장의 영 기본값과 상태 누출 없음 확인
        } // 실제 저장과 복원 처리 종료
        finally // 생성한 테스트 파일의 독립 수명 관리
        { // 격리 저장 정리 시작
            ExplorationSaveService.Remove(legacy); // 이전 형식 검사본과 복구본 삭제
            ExplorationSaveService.Remove(current); // 실제 왕복 저장 검사본과 복구본 삭제
            if (Directory.Exists(directory)) // 이번 검사만의 원본 폴더 존재 확인
            { // 빈 검사 폴더 정리 시작
                Directory.Delete(directory); // 생성한 빈 독립 저장 폴더 정리
            } // 빈 검사 폴더 정리 종료
        } // 격리 저장 정리 종료
    } // 저장 호환 검사 종료

    private static RelicData AcquireHeart() // 실제 게임 리소스에서 심장 유물 보유
    { // 심장 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/SmallHeartOfCorrosionCore"); // 원본 시트의 작은 침식핵 심장 자료 읽기
        Assert.IsNotNull(relic); // 실제 적용된 유물 자산 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 기본 회차 보유 규칙으로 실제 획득
        return relic; // 이후에 제거와 재획득할 동일 원본 반환
    } // 심장 유물 획득 종료

    private sealed class RunBattle : IDisposable // 실제 조우와 피해 런타임을 연결한 회귀 검사 환경
    { // 실제 교전 검사 준비 시작
        private readonly ExplorationSessionManager session; // 현재 조우를 보관한 선택적 실제 탐사 관리자
        private readonly EncounterData encounter; // 일반, 정예, 보스의 실제 조우 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 실제 전투 시작에 연결할 기본 카드 원본
        private readonly DeckData source; // 전투 손패의 원본 덱 자료
        internal readonly BattleUnitRuntime[] Allies; // 공격력을 확인할 실제 독립 아군
        internal readonly List<BattleUnitRuntime> Enemies; // 초기 적과 나중에 들어올 실제 소환 적
        internal readonly BattleTurnRuntime Turns; // 처치와 승리의 실제 공통 턴 기능

        internal RunBattle(ExplorationSessionManager session, string asset, int copies = 1) // 실제 탐사 조우를 활성화한 독립 피해 전투 생성
        { // 조우와 전투 관리자 연결 시작
            this.session = session; // 현재 전투만의 탐사 연결 보관
            encounter = Resources.Load<EncounterData>("Encounters/" + asset); // 실제 조우 등급 및 적 원본 확인
            Assert.IsNotNull(encounter); // 원본 조우 자료 연결 검증
            if (session != null) // 실제 탐사 내부 전투 여부 확인
            { // 현재 탐사 조우 진입 시작
                Assert.IsTrue(session.BeginEncounter(encounter.EncounterId, encounter, Vector3.zero, Vector3.up)); // 기존 탐사 조우 접촉 흐름으로 실제 교전 상태 생성
            } // 현재 탐사 조우 진입 종료
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 생존 아군의 출전 원본 제공
            Allies = roster.Characters.Take(2).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 서로 다른 두 아군의 공격력 비교 자료 생성
            Enemies = Enumerable.Range(0, copies).Select(_ => BattleUnitRuntime.CreateEnemy(encounter.Enemies[0])).ToList(); // 같은 원본이라도 실제 개체별 서로 다른 적 생성
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, Allies[0].CharacterSource) }); // 정상 전투를 시작할 실제 카드 원본 덱 구성
            var deck = BattleDeckRuntime.Create(source, Allies, 4, 260); // 실제 전투 드로우 및 상태 엔진 생성
            Turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), Allies, Enemies, 0, encounter.BattleType, session); // 실제 탐사와 연결한 전투 흐름 구성
            Assert.IsTrue(Turns.StartBattle(0)); // 초기 적과 실제 개별 처치 연결을 시작
        } // 조우와 전투 관리자 연결 종료

        internal BattleUnitRuntime Summon() // 현재 조우와 같은 원본을 쓴 후속 소환 적의 실제 등록
        { // 후속 소환 적 생성 시작
            var summoned = BattleUnitRuntime.CreateEnemy(encounter.Enemies[0]); // 초기 목록에 없던 다음 적 개체 생성
            Enemies.Add(summoned); // 실제 전투 적 목록에 소환 개체 추가
            Assert.IsTrue(Turns.RegisterSummonedEnemy(summoned)); // 기존 전투 소환 참가의 정상 등록 확인
            return summoned; // 체력 피해를 줄 실제 별도 소환 적 반환
        } // 후속 소환 적 생성 종료

        internal void ResolveExplorationResult() // 기존 전투 승리 정보를 실제 탐사로 전달
        { // 탐사 귀환 처리 시작
            Assert.AreEqual(BattleResult.Victory, Turns.Result); // 실제 정예와 보스 피해로 전투가 종료되었는지 확인
            session.ResolveBattleResult(new BattleResultData(Turns.Result, encounter.BattleType, Turns.CurrentRound, Allies, Enemies.Select(enemy => enemy.UnitId).ToArray())); // 기존 조우 클리어와 다른 전투 진입 상태 정산
        } // 탐사 귀환 처리 종료

        public void Dispose() // 실제 전투 연결과 생성한 검사 자산 해제
        { // 실제 전투 제거 시작
            Turns.Dispose(); // 처치와 아군 공격력의 전투 연결 해제
            Object.Destroy(source); // 이번 교전의 임시 원본 덱 삭제
            Object.Destroy(card); // 검사에서만 생성한 기본 카드 자료 삭제
        } // 실제 전투 제거 종료
    } // 실제 교전 검사 준비 종료
} // 심장 유물 회귀 검사 종료
