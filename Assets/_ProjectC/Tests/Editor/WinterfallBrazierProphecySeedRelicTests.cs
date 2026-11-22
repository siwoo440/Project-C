using System; // 테스트 전투 객체의 수명과 난수 오류 처리
using System.Collections; // 실제 플레이 모드 전환 대기
using System.Linq; // 전투 명단과 개막 상태의 실제 조회
using NUnit.Framework; // 전투 상태와 자원 변화 비교
using UnityEngine; // 원본 캐릭터와 유물 및 상태 환경
using UnityEngine.TestTools; // 실제 Unity 플레이 모드 실행
using Object = UnityEngine.Object; // 사용한 테스트 원본 데이터 정리
using Random = UnityEngine.Random; // 실제 게임의 공용 정수 난수

public sealed class WinterfallBrazierProphecySeedRelicTests // 화상 행동력과 아군 턴 주기 및 개막 재생 검사
{ // 실제 전투 세 유물 검사 시작
    [UnityTest] // 실제 체력 손실만 한 번 판정하고 두 종료 방향의 행동력을 이어서 적용
    public IEnumerator EmberlessBrazierRewardsOnlyActualBurnHealthLossAndCarriesAcrossCleanup() // 영 피해 및 기존 일반 공격과 상한 도달 비교
    { // 불씨 없는 화로의 행동력 검사 시작
        yield return new EnterPlayMode(); // 원래 소스와 네이티브 자원의 플레이 환경 준비
        var brazier = Acquire("EmberlessBrazier"); // 실제 양측 화상 반응 유물의 원본 획득
        Assert.AreEqual(20, brazier.TriggerChancePercent); // 정상 난수 성공의 마지막 경계 확인
        using (var trial = new Trial()) // 실제 화상 및 방어를 수신할 일반 전투
        { // 현재 행동력과 실제 후속 턴 예약 비교 시작
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 실제 첫 아군 차례와 기본 행동력 시작
            var blocked = trial.Enemies[0]; // 방어도가 화상을 모두 막는 실제 적
            blocked.GainBlock(10); // 현재 방어도 흡수만 일어나도록 충분한 방어 지급
            blocked.ApplyStatusEffect(BattleStatusEffectType.Burn, 3, 1, 1); // 첫 적용에서 상당한 피해를 주는 원래 화상
            var processor = new BattleStatusEffectProcessor(); // 실제 게임 상태 처리기를 독립 실행하는 비교
            var unchanged = Random.state; // 소모되지 않아야 할 현재 공용 난수 상태
            processor.ProcessPhaseEnd(blocked, trial.Turn.CurrentRound); // 원래 방어도에 흡수되는 실제 Burn 틱 처리
            Assert.AreEqual(unchanged, Random.state); // 양수 체력 손실이 없는 방어 흡수의 확률 미소비
            Assert.AreEqual(5, trial.Points.CurrentActionPoints); // 방어도 흡수만 받은 팀의 원래 행동력
            var target = trial.Enemies[1]; // 조건을 만족하는 별개 실제 생존 적
            target.ApplyStatusEffect(BattleStatusEffectType.Burn, 5, 1, 1); // 현재 전투의 실제 활성 화상
            var still = Random.state; // 화상 상태 자체와 실제 화상 피해를 구분한 확률 상태
            target.TakeDamage(2, BattleDamageType.None); // 화상 대상이 받아도 별개 발생자의 일반 피해
            Assert.AreEqual(still, Random.state); // 상태가 붙었어도 일반 피해는 화로 확률과 무관
            var expected = UseRoll(19); // 실제 정수 범위의 마지막 성공 값 준비
            processor.ProcessPhaseEnd(target, trial.Turn.CurrentRound); // 현재 실제 아군 조작 단계에서 일반 화상 피해 확인
            Assert.AreEqual(expected, Random.state); // 양수 체력 손실 한 건에 정확히 한 번만 판정
            Assert.AreEqual(6, trial.Points.MaxActionPoints); // 원래 상한을 넘어 실제 한 점 쓸 자리 확인
            Assert.AreEqual(6, trial.Points.CurrentActionPoints); // 이미 최대였던 실제 행동력에도 한 점 지급
            Assert.IsTrue(trial.Points.Spend(6)); // 지급된 임시 상한을 실제로 모두 쓸 수 있는지 확인
            var missed = trial.Enemies[2]; // 첫 실패 값을 검증할 별개 활성 화상 대상
            missed.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 1, 1); // 방어 없는 실제 양수 화상
            expected = UseRoll(20); // 기획 확률의 첫 실패 값 선택
            processor.ProcessPhaseEnd(missed, trial.Turn.CurrentRound); // 실제 손실은 있으나 확률 실패 처리
            Assert.AreEqual(expected, Random.state); // 실패도 정확히 한 번만 확률 적용
            Assert.AreEqual(0, trial.Points.CurrentActionPoints); // 실패한 경우 원래 행동력 유지
            trial.Allies[0].ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 1, 1); // 아군 차례 정리 뒤 실제 아군 화상
            trial.Enemies[3].ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 1, 1); // 적 종료에서 다음 아군에게 지급할 별개 화상
            expected = UseRoll(19); // 아군 종료의 실제 성공 확률 설정
            Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 실제 정리와 아군 화상을 거쳐 적 턴 진입
            Assert.AreEqual(expected, Random.state); // 아군 실제 종료의 화상 기회 한 번 확인
            Assert.AreEqual(0, trial.Points.CurrentActionPoints); // 이미 끝난 차례에는 사용할 수 없는 행동력 미지급
            Assert.AreEqual(5, trial.Points.MaxActionPoints); // 지난 차례 임시 상한의 원래 회수 확인
            expected = UseRoll(19); // 실제 적 턴 화상의 별개 성공 확률 설정
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 적 화상 종료와 새 아군 원본 행동력 회복 실행
            Assert.AreEqual(expected, Random.state); // 실제 적 종료의 화상 기회 한 번 확인
            Assert.AreEqual(7, trial.Points.CurrentActionPoints); // 이후 실제 차례까지 두 화상 보상 모두 각각 한 점 복원
            Assert.AreEqual(7, trial.Points.MaxActionPoints); // 사용할 수 있는 두 점의 이번 턴 임시 상한 확인
            Assert.IsTrue(trial.Turn.EndPlayerTurn() && trial.Turn.CompleteEnemyTurn()); // 화상의 이전 예약이 없는 다음 실제 아군 차례
            Assert.AreEqual(5, trial.Points.CurrentActionPoints); // 반복 지급 없이 기존 이번 회복값만 적용
        } // 현재 행동력과 실제 후속 턴 예약 비교 종료
        using (var trial = new Trial()) // 적이 먼저 행동하는 첫 아군 차례의 복원 경계
        { // 시초 적 선제 화상 판정 시작
            trial.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 1, 1); // 첫 실제 적 종료의 화상 원본 준비
            Assert.IsTrue(trial.Turn.StartBattle(0, enemyFirst: true)); // 실제 게임에서 아군이 아직 한 턴도 시작하지 않은 선제 설정
            UseRoll(19); // 적 선제의 첫 실제 화상 틱 성공 확률
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 첫 아군 턴 회복 이전에 쌓인 행동력 소모 확인
            Assert.AreEqual(6, trial.Points.CurrentActionPoints); // 초기 Restore 이후에도 첫 적 화상 보상 유지
        } // 시초 적 선제 화상 판정 종료
        yield return new ExitPlayMode(); // 실제 전투 상태와 원본 획득 환경 종료
    } // 불씨 없는 화로의 행동력 검사 종료

    [UnityTest] // 실제 네 번째 아군 시작에서 양쪽 피해 이후 전투 결과 확정
    public IEnumerator ProphecyCountsARealTimeCoinTurnAndSettlesBothSidesBeforeVictory() // 최초 선제와 별개인 아군 네 번째 차례 계산 확인
    { // 낙석의 예언석 검사 시작
        yield return new EnterPlayMode(); // 실제 일반 교전 환경 전환
        var stone = Acquire("RockfallProphecyStone"); // 정식 예언석의 실제 피해 수치
        Assert.AreEqual(16, stone.EffectValue); // 적 광역의 원본 십육 점
        foreach (bool allAlliesFall in new[] { false, true }) // 같은 마지막 적 피해의 생존과 동시 전멸 비교
        { // 독립 결과가 나오는 실제 전투 시작
            using (var trial = new Trial()) // 선제와 시간 동전을 공유하는 원래 전투
            { // 실제 넷째 아군 시작 검증 준비
                Assert.IsTrue(trial.Turn.StartBattle(0, enemyFirst: true) && trial.Turn.CompleteEnemyTurn()); // 적 선제는 세지 않고 첫 실제 아군 시작
                for (int turn = 1; turn < 3; turn++) // 첫 세 차례까지 기존 피해가 없는 실제 진영 전환
                { // 다음 실제 아군 시작으로 진행
                    Assert.IsTrue(trial.Turn.EndPlayerTurn() && trial.Turn.CompleteEnemyTurn()); // 원래 적 완료 뒤 다음 아군 차례 시작
                } // 다음 실제 아군 시작으로 진행 종료
                Assert.AreEqual(3, trial.Turn.CurrentRound); // 적 선제를 포함하지 않는 세 번째 아군 차례
                Assert.IsTrue(trial.Enemies.All(enemy => enemy.CurrentHealth == 100)); // 세 번째까지 전체 적의 원래 표시 체력
                foreach (var enemy in trial.Enemies) // 십육 점 피해로 동시에 쓰러질 원래 적 상태 준비
                { // 적의 실제 원래 피해를 넣어 체력 설정
                    enemy.TakeDamage(84, BattleDamageType.None); // 일반 피해에 의해 남은 체력 십육 점
                } // 적의 실제 원래 피해를 넣어 체력 설정 완료
                if (allAlliesFall) // 예언 피해 적용 뒤 전원 사망의 비교 조건
                { // 실제 아군에게 치명 피해가 되는 체력으로 준비 시작
                    foreach (var ally in trial.Allies) // 아군 모두의 실제 원래 체력 비교 시작
                    { // 아군별 네 점 남기기 시작
                        ally.TakeDamage(96, BattleDamageType.None); // 예언석의 네 점으로 쓰러질 실제 이전 손실
                    } // 아군별 네 점 남기기 종료
                } // 실제 아군에게 치명 피해가 되는 체력으로 준비 종료
                int allyHealthAtResult = -1; // 원래 전투 종료 신호가 본 아군 체력 기록
                trial.Turn.BattleCompleted += _ => allyHealthAtResult = trial.Allies[0].CurrentHealth; // 승리 처리보다 양측 피해가 먼저 적용되었는지 확인
                Assert.AreEqual(1, trial.Turn.QueueTimeCoinTurn(2)); // 새 실제 아군 턴 하나를 만드는 원래 시간 동전 예약
                Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 원래 적 턴 없이 시작한 실제 넷째 아군 차례
                Assert.AreEqual(4, trial.Turn.CurrentRound); // 시간 동전이 포함되는 네 번째 실제 시작 확인
                Assert.IsTrue(trial.Enemies.All(enemy => enemy.IsDead)); // 각 실제 적에게 중복 없이 고정 총 피해 기록
                Assert.AreEqual(allAlliesFall ? 0 : 96, allyHealthAtResult); // 원래 결과 신호 이전의 아군 고정 네 점 확인
                Assert.AreEqual(allAlliesFall ? BattleResult.Defeat : BattleResult.Victory, trial.Turn.Result); // 양쪽 동시 전멸에는 네이티브 패배 우선 확인
            } // 실제 넷째 아군 시작 검증 준비 종료
        } // 독립 결과가 나오는 실제 전투 종료
        yield return new ExitPlayMode(); // 실제 네이티브 전투와 임시 자료 정리
    } // 낙석의 예언석 검사 종료

    [UnityTest] // 적의 첫 행동 예고 이전에 살아 있는 모든 아군과 실제 세 턴 재생 적용
    public IEnumerator TreeSeedAppliesNativeThreeTurnRegenerationBeforeActualFirstIntent() // 각 생존자와 전투불능 및 선제의 수명 확인
    { // 수호목 씨앗의 실제 개막 재생 시작
        yield return new EnterPlayMode(); // 실제 네이티브 전투와 원본 유물 환경 준비
        var seed = Acquire("SwampGuardianTreeSeed"); // 출시 씨앗의 실제 수치와 개막 수명
        Assert.AreEqual(2, seed.EffectValue); // 원본의 턴당 재생 두 점 확인
        Assert.AreEqual(3, seed.StatusDuration); // 원본의 세 실제 아군 차례 확인
        using (var trial = new Trial()) // 플레이어 파티 세 명의 실제 생존과 상태 비교
        { // 개막 예고와 재생 진행 검사 시작
            var wounded = trial.Allies[0]; // 매번 두 점 회복을 볼 실제 부상 생존 아군
            wounded.ApplyPersistentHealth(85); // 정상 기존 회차 체력 복원 방식으로 부상 설정
            trial.Allies[2].TakeDamage(500, BattleDamageType.None); // 부여하지 말아야 할 실제 전투불능 아군
            bool hadRegenerationAtIntent = false; // 첫 적 예고의 실제 상태 확인 결과
            Assert.IsTrue(trial.Turn.StartBattle(0, enemyFirst: true, openingEffects: () => hadRegenerationAtIntent = trial.Allies.Take(2).All(unit => unit.GetStatusEffect(BattleStatusEffectType.Regeneration) is { EffectiveValue: 2, RemainingTurns: 3 }))); // 실제 첫 의도 준비 콜백 전에 모든 생존 재생 확인
            Assert.IsTrue(hadRegenerationAtIntent); // 살아 있는 모두의 네이티브 재생 상태 확인
            Assert.IsNull(trial.Allies[2].GetStatusEffect(BattleStatusEffectType.Regeneration)); // 고정된 사망 아군에 불필요한 상태 없음
            Assert.AreEqual(85, wounded.CurrentHealth); // 적이 먼저 행동하는 동안 아군 차례 이전 재생 피해 없음
            for (int count = 1; count <= 3; count++) // 실제 아군 자신의 세 차례에서 두 점 회복
            { // 네이티브 재생과 페이즈 수명 검사 시작
                Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 실제 적 완료에서 상태가 시작할 아군 턴 확인
                Assert.AreEqual(85 + count * 2, wounded.CurrentHealth); // 각 실제 아군 차례 시작의 독립 두 점 회복
                Assert.AreEqual(4 - count, wounded.GetStatusEffect(BattleStatusEffectType.Regeneration).RemainingTurns); // 아군에게만 적용되는 실제 남은 기간
                Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 해당 아군 차례 종료에 수명을 한 번 차감
            } // 네이티브 재생과 페이즈 수명 검사 종료
            Assert.IsNull(wounded.GetStatusEffect(BattleStatusEffectType.Regeneration)); // 세 아군 차례 종료 후 자연 소진 확인
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 보상이 사라진 실제 네 번째 아군 차례
            Assert.AreEqual(91, wounded.CurrentHealth); // 이후 추가 재생 없이 마지막 현 체력 유지
        } // 개막 예고와 재생 진행 검사 종료
        yield return new ExitPlayMode(); // 플레이 모드와 회차 효과 원래 환경으로 복원
    } // 수호목 씨앗의 실제 개막 재생 종료

    private static Random.State UseRoll(int target) // Unity 정수 난수에서 정확한 검증 경계 찾기
    { // 같은 시작 상태로 재현할 실제 정수 판정 설정
        for (int seed = 1; seed < 100000; seed++) // 실제 Unity 난수의 원하는 첫 수치 검색
        { // 고정 난수 후보 확인 시작
            Random.InitState(seed); // 이번 후보의 원래 첫 상태
            if (Random.Range(0, 100) == target) // 실제 유물과 같은 정수 범위의 첫 값 확인
            { // 실제 플레이에서 동일한 난수 횟수를 확인하는 준비
                var after = Random.state; // 해당 값 한 번 소비한 예상 상태
                Random.InitState(seed); // 실제 발동이 나중에 같은 값을 소비하도록 되돌림
                return after; // 검증할 한 번 소비한 예상 상태 반환
            } // 실제 플레이에서 동일한 난수 횟수를 확인하는 준비 종료
        } // 고정 난수 후보 확인 종료
        throw new InvalidOperationException("실제 경계 확인용 난수 없음"); // 시험의 실제 초기화 실패
    } // 같은 시작 상태로 재현할 실제 정수 판정 설정 종료

    private static RelicData Acquire(string file) // 출시 원본의 정식 자원과 유물 획득 경로
    { // 실제 교전 이전 유물 보관 시작
        var relic = Resources.Load<RelicData>("Relics/" + file); // 등록한 이름의 실제 유물 원본 조회
        Assert.IsNotNull(relic, file); // 정식 출시 리소스의 유일한 실제 파일 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 원래 회차 보유 상태에 지급
        return relic; // 실제 수치와 시점 검증에 반환
    } // 실제 교전 이전 유물 보관 종료

    private sealed class Trial : IDisposable // 동일한 승패 관리와 상태 처리를 가진 독립 전투
    { // 실제 조우를 구성하는 검사 환경 시작
        private readonly Random.State random = Random.state; // 이전 테스트와 실제 사용자 난수 상태 복원용
        private readonly CharacterData[] actors = new CharacterData[3]; // 실제 아군 세 명의 독립 카드 소유 원본
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 방어 없는 실제 기본 적 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 실제 조우를 승인하는 최소 덱 원본
        private readonly DeckData source; // 실제 카드 결제와 손패가 쓰는 덱 자료
        private readonly BattleStatusEffectController statuses; // 진영의 원래 시작 및 종료 상태 처리 구독
        internal readonly BattleUnitRuntime[] Allies; // 실제 상태를 받을 세 명의 출전 아군
        internal readonly BattleUnitRuntime[] Enemies; // 독립적으로 화상과 실제 피해를 받을 적
        internal readonly BattleTurnRuntime Turn; // 실제 페이즈와 원래 적 조우 관리자
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(5); // 실제 상한과 사용량이 달라지는 공용 행동력

        internal Trial() // 실제 전투의 적과 아군 및 상태 효과 진입 환경
        { // 양 진영의 원본 준비 시작
            for (int index = 0; index < actors.Length; index++) // 별개 원본 아군의 현재 전투 참가자 구성
            { // 실제 체력과 고유 카드 소유자 자료 준비
                actors[index] = ScriptableObject.CreateInstance<CharacterData>(); // 기존 아군 원형의 별개 복사 데이터
                JsonUtility.FromJsonOverwrite($"{{\"characterId\":\"day289_trial_{index}\",\"maxHealth\":100}}", actors[index]); // 실제 피해 비교의 백 점 체력 및 고유 식별자
            } // 실제 체력과 고유 카드 소유자 자료 준비 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", enemy); // 각 실제 적의 같은 체력과 기본 방어 적용
            Allies = actors.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 실제 생존 아군 명단과 소유 카드 연결
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemy)).ToArray(); // 네 실제 적의 별개 상태 생성
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, actors[0]) }); // 정상 첫 아군 소유의 실제 덱 열기
            var deck = BattleDeckRuntime.Create(source, Allies, 6, 289); // 원래 실제 전투 손패와 덱 초기화
            Turn = new BattleTurnRuntime(deck, Points, Allies, Enemies, 0, BattleType.Normal); // 실제 진영 시작과 패배 처리를 배치
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 화상과 재생 처리의 구독 연결
        } // 양 진영의 원본 준비 종료

        public void Dispose() // 독립 전투와 임시 데이터의 정리
        { // 실제 종료와 소유 상태 분리 시작
            statuses.Dispose(); // 정상 상태 처리기 구독 해제
            Turn.Dispose(); // 실제 교전 및 각 진영의 이벤트 연결 해제
            Object.Destroy(source); // 현재 조사에만 사용한 덱 원본 정리
            Object.Destroy(card); // 독립 카드 원본 정리
            Object.Destroy(enemy); // 독립 적 원본 정리
            foreach (var actor in actors) // 별개 아군 출전 원본 정리
            { // 현재 시험만의 자료 사용 종료
                Object.Destroy(actor); // 실제 임시 아군 원본 제거
            } // 현재 시험만의 자료 사용 종료 확인
            Random.state = random; // 다음 실제 검사로 넘어가기 전에 원래 난수 복원
        } // 실제 종료와 소유 상태 분리 종료
    } // 실제 조우를 구성하는 검사 환경 종료
} // 실제 전투 세 유물 검사 종료
