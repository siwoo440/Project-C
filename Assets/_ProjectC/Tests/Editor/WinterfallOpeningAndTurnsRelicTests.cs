using System; // 원본 전투 해제와 난수 조건 사용
using System.Collections; // 실제 플레이 모드 프레임 사용
using System.Linq; // 실제 살아 있는 적과 손패 원본 목록 확인
using NUnit.Framework; // 개막과 실제 아군 턴 결과 확인
using UnityEngine; // 원본 자료와 네이티브 난수 사용
using UnityEngine.TestTools; // 실제 전투 플레이 모드 진입
using Object = UnityEngine.Object; // 임시 원본 데이터의 정리
using Random = UnityEngine.Random; // 기존 전투에서 쓰는 유니티 난수

public sealed class WinterfallOpeningAndTurnsRelicTests // 개막 실명과 방어도 및 실제 시작 행동력 검사
{ // 원래 전투 규칙 검사 시작
    [UnityTest] // 선제 첫 적 턴에서 실명을 적용할 전투당 한 번의 확률 검사
    public IEnumerator MistFlowerAppliesOneEnemyTurnAheadOfAmbushAndRespectsImmunity() // 살아 있는 한 적과 기존 면역의 독립 비교
    { // 꽃가루 원본의 실제 개막 검사 시작
        yield return new EnterPlayMode(); // 정식 전투 상태 효과와 원본 유물 환경 진입
        var mist = Acquire("MistFlowerPollen"); // 실제 배포한 안개꽃 꽃가루 원본 보유
        Assert.AreEqual(30, mist.TriggerChancePercent); // 원본의 전투 시작 한 번 확률 확인
        Assert.AreEqual(25, mist.EffectValue); // 네이티브 실명의 명중률 이십오 퍼센트포인트 확인
        Assert.AreEqual(1, mist.StatusDuration); // 실제 적의 자기 턴 한 차례 수명 확인
        using (var trial = new Trial()) // 전투불능이 섞인 적 선제 조우
        { // 정상 한 적의 실제 실명 검사 시작
            trial.Enemies[3].TakeDamage(500, BattleDamageType.None); // 무작위 실명에서 제외할 실제 전투불능 적
            UseSeed(29, 3, targetIndex: 1); // 마지막 성공과 두 번째 생존 적 선택의 실제 난수 준비
            int blindAtEnemyStart = 0; // 첫 적 페이즈의 발행 시점에서 본 활성 실명 수
            trial.Turn.PhaseStarted += (phase, _) => blindAtEnemyStart = phase == BattleTurnPhase.EnemyTurn ? BlindCount(trial) : blindAtEnemyStart; // 기존 상태 시작과 첫 적 행동 앞의 실제 상태 읽기
            int blindAtFirstIntent = 0; // 선제 행동 예고 콜백에서 본 활성 실명 수
            Assert.IsTrue(trial.Turn.StartBattle(0, enemyFirst: true, openingEffects: () => blindAtFirstIntent = BlindCount(trial))); // 실제 적 선제 조우와 행동 예고의 개막
            Assert.AreEqual(1, blindAtFirstIntent); // 첫 선제 예고 콜백 전에 실제 실명이 적용됐는지 확인
            Assert.AreEqual(1, blindAtEnemyStart); // 첫 적 행동보다 먼저 유효 실명 한 건 확인
            var blind = trial.Enemies[1].GetStatusEffect(BattleStatusEffectType.Blind); // 이번 균등 추첨에서 당첨된 실제 두 번째 적
            Assert.AreEqual(25, blind.EffectiveValue); // 기존 명중률 계산에 들어가는 실제 실명 값 확인
            Assert.AreEqual(1, blind.RemainingTurns); // 실제 첫 적 차례가 남아 있는 상태 확인
            var afterOpeningRoll = Random.state; // 개막에 사용한 실제 두 난수 뒤 상태 보관
            Assert.IsFalse(trial.Turn.StartBattle(0, enemyFirst: true)); // 중복 전투 시작 요청의 기존 차단 확인
            Assert.AreEqual(afterOpeningRoll, Random.state); // 중복 요청에서 새 확률 검사 없음 확인
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 실제 적 선제 턴 종료의 원래 상태 지속 처리
            Assert.AreEqual(0, BlindCount(trial)); // 정확히 한 적 턴 완료로 실명 소진 확인
            Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 실제 다음 적 턴까지 진입
            Assert.AreEqual(0, BlindCount(trial)); // 나중 적 턴에 개막 실명 재발동 없음 확인
        } // 정상 한 적의 실제 실명 검사 종료
        using (var trial = new Trial()) // 같은 성공 값을 가진 실명 면역 비교 조우
        { // 성공 후 한 적만 시도하는 원래 면역 검사 시작
            trial.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 동일 추첨 대상의 정상 약화 면역
            var expectedState = UseSeed(29, trial.Enemies.Length, targetIndex: 1); // 단일 성공 뒤 단일 대상까지 사용한 실제 난수
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 일반 아군 선제의 실제 시작
            Assert.AreEqual(0, BlindCount(trial)); // 면역 선택 뒤 나머지 적 재추첨 없음 확인
            Assert.AreEqual(expectedState, Random.state); // 면역으로 막힌 뒤 추가 확률 소비 없음 확인
        } // 성공 후 한 적만 시도하는 원래 면역 검사 종료
        using (var trial = new Trial()) // 첫 실패 확률 경계의 독립 전투
        { // 실패 시 생존 적 추첨을 건너뛰는 검사 시작
            var expectedState = UseSeed(30); // 원본 확률의 첫 실패와 난수 한 건 상태 준비
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 실패 값으로 실제 조우 개막
            Assert.AreEqual(0, BlindCount(trial)); // 실패한 전투의 실명 상태 없음 확인
            Assert.AreEqual(expectedState, Random.state); // 실패 시 한 번만 확률을 평가한 실제 난수 확인
        } // 실패 시 생존 적 추첨을 건너뛰는 검사 종료
        yield return new ExitPlayMode(); // 원래 플레이 모드 검사 환경 종료
    } // 꽃가루 원본의 실제 개막 검사 종료

    [UnityTest] // 실제 아군의 첫 턴과 추가 턴 및 적 턴에서 행동력 상한 확인
    public IEnumerator LavaGuardianGrantsUsableCapacityForLivingBurningEnemiesOnly() // 한 차례별 확률 및 추가 지급의 원래 정리 검사
    { // 용암 수호핵의 실제 행동력 검사 시작
        yield return new EnterPlayMode(); // 정식 전투의 턴과 행동력 환경 진입
        var core = Acquire("LavaVeinGuardianCore"); // 출시한 용암맥 수호핵 파편 원본 보유
        Assert.AreEqual(25, core.TriggerChancePercent); // 실제 원본의 한 턴 확률 확인
        Assert.AreEqual(1, core.EffectValue); // 실제 화상 적 한 명당 행동력 한 점 확인
        using (var trial = new Trial()) // 유효 및 만료와 이미 쓰러진 화상 적 조우
        { // 살아 있는 실제 화상 두 명과 추가 아군 턴 준비 시작
            trial.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.Burn, 4, 3, 1); // 첫 번째 생존 적의 활성 화상
            trial.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.Burn, 4, 3, 1); // 두 번째 생존 적의 활성 화상
            trial.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 3, 1); // 비교 대상에게 수치 한 점의 화상 적용
            trial.Enemies[2].GetStatusEffect(BattleStatusEffectType.Burn).DecayBurn(); // 제거 직전이지만 수치가 다 소진된 원래 화상 상태
            trial.Enemies[3].ApplyStatusEffect(BattleStatusEffectType.Burn, 4, 3, 1); // 세 번째 후보였던 적의 실제 화상 적용
            trial.Enemies[3].TakeDamage(500, BattleDamageType.None); // 쓰러진 적의 화상 미산입 확인용 실제 전투불능
            var expectedState = UseSeed(24); // 양쪽 적을 세되 확률을 한 번만 쓰는 마지막 성공 상태
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 첫 실제 아군 차례를 시작
            Assert.AreEqual(expectedState, Random.state); // 여러 활성 화상에도 전투 난수 한 번만 사용 확인
            using var events = new BattleEventDispatcher(); // 전투 장면의 공용 턴 시작 신호 발행기
            var run = RelicRunManager.EnsureInstance(); // 실제 출시 유물을 보유한 회차 관리자
            using var generic = new BattleRelicEffectController(events, run.Inventory, run.Gold, trial.Allies, trial.Enemies, trial.Deck, trial.Points); // 원래 장면의 공용 유물 처리기 연결
            generic.ProcessInitialBattleState(trial.Turn.CurrentPhase, trial.Turn.CurrentRound); // 원래 장면에서 연결 직후 호출하는 첫 턴 신호
            Assert.AreEqual(expectedState, Random.state); // 공용 최초 턴 처리로 원래 추첨을 반복하지 않는지 확인
            Assert.AreEqual(7, trial.Points.MaxActionPoints); // 기본 다섯 점 상한에 실제 두 점 추가 확인
            Assert.AreEqual(7, trial.Points.CurrentActionPoints); // 현재 손패에 실제 사용 가능한 두 점 추가 확인
            Assert.IsTrue(trial.Points.Spend(7)); // 기존 상한을 넘어 새로 받은 두 점까지 실제 소비
            Assert.AreEqual(1, trial.Turn.QueueTimeCoinTurn(2)); // 실제 한 아군 추가 턴을 원래 두 점 대가로 예약
            expectedState = UseSeed(24); // 적 턴을 건너뛰는 새 실제 아군 턴의 성공
            Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 원래 시간 동전의 중간 아군 턴을 실제 시작
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, trial.Turn.CurrentPhase); // 적 턴을 생략한 실제 아군 차례 확인
            Assert.AreEqual(expectedState, Random.state); // 추가 차례에서도 새로 한 번만 확률 적용 확인
            Assert.AreEqual(5, trial.Points.MaxActionPoints); // 기본 다섯에서 추가 차례 대가 두 점과 새 보상 두 점의 합 확인
            Assert.AreEqual(5, trial.Points.CurrentActionPoints); // 정산 직후 실제로 쓸 수 있는 다섯 점 확인
            Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 추가 턴을 실제 끝내고 정상 적 턴 진입
            var beforeGenericEnemyTurn = Random.state; // 실제 적 페이즈 공용 신호 직전 확률 상태
            events.Publish(new BattleEventContext(BattleEventType.TurnStarted, trial.Turn.CurrentRound, trial.Turn.CurrentPhase)); // 실제 전투 장면과 같은 적 턴 시작 신호
            Assert.AreEqual(beforeGenericEnemyTurn, Random.state); // 적 턴 공용 처리에서 원래 아군 전용 추첨 없음 확인
            Assert.AreEqual(5, trial.Points.MaxActionPoints); // 적 턴 진입의 원래 상한 회수 확인
            Assert.AreEqual(0, trial.Points.CurrentActionPoints); // 종료한 아군 턴의 미사용 지급 소거 확인
            expectedState = UseSeed(25); // 다음 아군 턴에서 첫 실패 원래 난수 상태
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 실제 적 턴 완료 뒤 새 아군 차례 시작
            Assert.AreEqual(expectedState, Random.state); // 여전히 활성 화상이 있어 한 번 실패한 실제 난수 확인
            Assert.AreEqual(5, trial.Points.CurrentActionPoints); // 새로 실패한 턴의 기본 행동력 확인
        } // 살아 있는 실제 화상 두 명과 추가 아군 턴 준비 종료
        using (var trial = new Trial()) // 화상 적이 한 명도 없는 새 조우
        { // 비대상 턴의 원래 난수 보존 시작
            Random.InitState(287); // 대상 없는 조우 비교의 고정 난수
            var unchanged = Random.state; // 확률을 사용하기 전의 실제 난수 상태
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 실제 기본 행동력으로 정상 아군 턴 시작
            Assert.AreEqual(unchanged, Random.state); // 활성 화상 적 없는 턴의 확률 소비 생략 확인
            Assert.AreEqual(5, trial.Points.CurrentActionPoints); // 보상 대상 없는 다음 행동력의 실제 기본값 확인
        } // 비대상 턴의 원래 난수 보존 종료
        yield return new ExitPlayMode(); // 실제 용암 수호핵 검사 환경 종료
    } // 용암 수호핵의 실제 행동력 검사 종료

    [UnityTest] // 개막 방어도와 첫 기본 손패 대가를 두 선제 방향에서 확인
    public IEnumerator RedCanyonShieldBeginsBeforeInitiativeAndPaysOnlyFirstBaseDraw() // 실제 선제 방어도 정리와 다른 첫 손패 감산의 합산 검사
    { // 붉은 협곡 방벽석의 실제 개막 검사 시작
        yield return new EnterPlayMode(); // 선제와 원본 손패의 실제 전투 환경 진입
        var stone = Acquire("RedCanyonBulwarkStone"); // 게임의 실제 붉은 협곡 방벽석 보유
        Acquire("SurveillanceCoreFragment"); // 기존 첫 실제 손패 한 장 대가를 가진 부품 보유
        Assert.AreEqual(10, stone.EffectValue); // 실제 아군 한 명당 방어도 열 점 확인
        foreach (bool enemyFirst in new[] { false, true }) // 아군 선제와 적 선제를 각각 두 원래 전투에서 확인
        { // 선제에 따른 실제 방어도 수명 비교 시작
            using (var trial = new Trial()) // 첫 손패와 다음 손패의 수가 같은 원래 전투
            { // 이번 선제 조건 실제 비교 시작
                trial.Allies[2].TakeDamage(500, BattleDamageType.None); // 방벽석을 받을 수 없는 전투불능 출전원 준비
                Assert.IsTrue(trial.Turn.StartBattle(3, enemyFirst)); // 실제 원래 선제 진영과 시작 드로우 요청
                Assert.AreEqual(10, trial.Allies[0].CurrentBlock); // 첫 실제 행동 전 생존 아군 하나의 방어도 확인
                Assert.AreEqual(10, trial.Allies[1].CurrentBlock); // 같은 실제 생존 아군 전원에게 방어도 적용 확인
                Assert.AreEqual(0, trial.Allies[2].CurrentBlock); // 이미 쓰러진 아군의 방어도 미지급 확인
                if (enemyFirst) // 적 선제에서 실제 첫 아군 차례가 아직 대기인지 확인
                { // 방어도가 선제 공격을 보호하고 끝나는 경로 시작
                    Assert.AreEqual(0, trial.Deck.HandCount); // 적 선제 동안 첫 아군 손패 요청 미지급
                    trial.Allies[0].TakeDamage(4, BattleDamageType.None); // 선제의 원래 방어도 소모 확인용 실제 피해
                    Assert.AreEqual(6, trial.Allies[0].CurrentBlock); // 네 점 흡수 뒤 실제 남은 방어도
                    Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 기존 적 선제 방어도 리셋과 첫 아군 턴 시작
                    Assert.AreEqual(0, trial.Allies[0].CurrentBlock); // 아래 첫 아군 턴의 방어도 중복 지급 없음 확인
                } // 방어도가 선제 공격을 보호하고 끝나는 경로 종료
                Assert.AreEqual(1, trial.Deck.HandCount); // 첫 기본 세 장에서 방벽석과 기존 감시 부품 각 한 장 차감 확인
                Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 다음 정상 손패를 위해 실제 적 턴까지 전환
                Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 두 번째 아군 차례의 실제 기본 손패 적용
                Assert.AreEqual(3, trial.Deck.HandCount); // 이후 기본 드로우의 원래 세 장 유지 확인
                Assert.AreEqual(0, trial.Allies[1].CurrentBlock); // 다음 차례 이전의 네이티브 방어도 회수 확인
            } // 이번 선제 조건 실제 비교 종료
        } // 선제에 따른 실제 방어도 수명 비교 종료
        yield return new ExitPlayMode(); // 실제 선제와 기본 손패 검사 환경 정리
    } // 붉은 협곡 방벽석의 실제 개막 검사 종료

    private static int BlindCount(Trial trial) // 실제 살아 있는 적에서 활성 실명 수 합산
    { // 공용 실명 검사 시작
        return trial.Enemies.Count(enemy => !enemy.IsDead && enemy.GetStatusEffect(BattleStatusEffectType.Blind) is { IsExpired: false }); // 실제 상태의 생존자 수 계산
    } // 공용 실명 검사 종료

    private static Random.State UseSeed(int roll, int count = 0, int targetIndex = 0) // 실제 Unity 난수의 정확한 확률 및 선택 위치 준비
    { // 고정 성공 및 경계 난수 검색 시작
        for (int seed = 0; seed < 100000; seed++) // Unity 난수의 유효한 경계값 재현
        { // 후보 난수의 실제 발행 확인 시작
            Random.InitState(seed); // 후보 시드의 실제 난수 상태 설정
            if (Random.Range(0, 100) == roll && (count == 0 || Random.Range(0, count) == targetIndex)) // 실제 확률 시험값과 대상 위치 확인
            { // 예상 상태 보관 및 동일 시작 시드 복구 시작
                var expected = Random.state; // 실제 판정 직후의 예상 Unity 상태 보관
                Random.InitState(seed); // 게임의 원래 호출에서 동일 경계 사용
                return expected; // 한 번 성공한 뒤 소비했을 실제 상태 반환
            } // 예상 상태 보관 및 동일 시작 시드 복구 종료
        } // 후보 난수의 실제 발행 확인 종료
        throw new InvalidOperationException("정확한 검사 난수 없음"); // 실제 경계값 검색 실패 시 명시 결과
    } // 고정 성공 및 경계 난수 검색 종료

    private static RelicData Acquire(string key) // 실제 출시 원본의 정상 유물 획득
    { // 게임 내 실제 회차 유물 보유 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 출시 유물의 원본 이름으로 조회
        Assert.IsNotNull(relic, key); // 실제 등록된 유물 원본 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 같은 정상 회차 보유 목록에 추가
        return relic; // 실제 지급 효과의 원본 반환
    } // 게임 내 실제 회차 유물 보유 종료

    private sealed class Trial : IDisposable // 네이티브 상태 효과와 턴을 연결한 독립 전투 검사
    { // 실제 전투 조우 검사 환경 시작
        private readonly Random.State originalRandom = Random.state; // 검사 전 전역 난수 상태 보관
        private readonly CharacterData[] actors = new CharacterData[3]; // 실제 전투의 서로 다른 기본 아군 원본
        private readonly EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 일반 네이티브 적 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 실제 기본 드로우를 채울 공용 원본
        private readonly DeckData source; // 독립 아군의 원래 회차 덱 원본
        private readonly BattleStatusEffectController statuses; // 실제 턴과 상태 지속이 연결된 처리기
        internal readonly BattleUnitRuntime[] Allies; // 실제 투입한 아군 세 명과 생존 여부
        internal readonly BattleUnitRuntime[] Enemies; // 실제 적 네 명의 독립 전투 객체
        internal readonly BattleDeckRuntime Deck; // 원래 손패와 버린 덱의 실제 전투 런타임
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(5); // 실제로 사용할 기본 공용 행동력
        internal readonly BattleTurnRuntime Turn; // 진영별 실제 시작과 종료를 반영할 관리자

        internal Trial() // 정상 전투 참가자와 원본 조우 덱 구성
        { // 검사 전투의 실제 시작 전 초기화
            for (int index = 0; index < actors.Length; index++) // 실제 카드 소유 구분을 위한 독립 원본 생성
            { // 검사 참가자의 개별 설정 시작
                actors[index] = ScriptableObject.CreateInstance<CharacterData>(); // 같은 종류의 별도 아군 원본 생성
                JsonUtility.FromJsonOverwrite($"{{\"maxHealth\":100,\"characterId\":\"opening_trial_{index}\"}}", actors[index]); // 일반 피해의 정확한 체력과 서로 다른 식별자 지정
            } // 검사 참가자의 개별 설정 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200}", enemyData); // 실제 상태 틱을 반복할 적 체력
            Allies = actors.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 생존과 전투불능을 비교할 출전원 생성
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemyData)).ToArray(); // 원래 상태를 개별 유지하는 실제 적 생성
            source = DeckData.CreateRuntime(Enumerable.Range(0, 9).Select(_ => new DeckCardEntry(card, actors[0])).ToArray()); // 생존한 첫 소유자의 실제 독립 카드 엔트리 준비
            Deck = BattleDeckRuntime.Create(source, Allies, 8, 287); // 검사 중 손패 상한과 덱 순서 준비
            Turn = new BattleTurnRuntime(Deck, Points, Allies, Enemies, 3, BattleType.Normal); // 기존 정상 매 차례 세 장 규칙 적용
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 모든 실제 페이즈의 상태 처리 연결
        } // 검사 전투의 실제 시작 전 초기화 종료

        public void Dispose() // 전투 상태와 임시 원본의 정상 정리
        { // 독립 전투 종료 시작
            statuses.Dispose(); // 종료와 시작의 지속 효과 구독 해제
            Turn.Dispose(); // 실제 전투의 양 진영 구독과 이벤트 해제
            Object.Destroy(source); // 현재 검사 덱의 독립 원본 정리
            Object.Destroy(card); // 독립 드로우 카드 원본 제거
            foreach (var actor in actors) // 각 실제 출전 아군 원본에 대해 순회
            { // 독립 아군 원본 정리 시작
                Object.Destroy(actor); // 독립 실제 출전 아군 원본 제거
            } // 독립 아군 원본 정리 종료
            Object.Destroy(enemyData); // 독립 실제 적 원본 제거
            Random.state = originalRandom; // 다음 검사로 넘어가기 전 난수 복원
        } // 독립 전투 종료 완료
    } // 실제 전투 조우 검사 환경 종료
} // 원래 전투 규칙 검사 종료
