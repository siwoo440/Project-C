using System; // 독립 시작 전투의 자원 정리
using System.Collections; // 플레이 모드 검사 래퍼
using System.Linq; // 실제 파티와 개별 보너스 조회
using NUnit.Framework; // 명중률과 시작 보상 검증
using UnityEngine; // 실제 유물과 난수 상태
using UnityEngine.TestTools; // 플레이 모드 검사 등록
using Random = UnityEngine.Random; // 실제 엔진 시작 난수
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투의 명중과 대상 상태

public sealed class WinterfallStealthAndOpeningDieTests // 은신 회피와 시작 주사위 검사
{ // 전투 시작 유물 검사 시작
    [UnityTest] // 실제 은신 상태가 있을 때만 회피 적용 검사
    public IEnumerator WickUsesRealStealthAndTheSharedHitBoundary() // 아군 은신과 기존 회피의 같은 명중 경계 확인
    { // 꺼진 등불 심지 검사 시작
        yield return new EnterPlayMode(); // 유물과 전투 개체가 있는 실제 실행 환경
        VerifyWick(); // 플레이 모드에서 명중·회피와 상태 확인
        yield return new ExitPlayMode(); // 독립 검사 종료
    } // 꺼진 등불 심지 검사 종료

    private static void VerifyWick() // 실제 은신 공개 상태와 회피 발동 범위
    { // 명중 대상 비교 시작
        using var fight = new Fight("HEALING_PRAYER"); // 기존 보상 원본의 실제 생존 아군과 적
        var ally = fight.Allies[0]; // 은신과 회피의 대상인 생존 아군
        var enemy = fight.Enemies[0]; // 같은 상태의 적과 실제 공격 출처
        var wick = Resources.Load<RelicData>("Relics/ExtinguishedLanternWick"); // 정식 꺼진 등불 심지 원본 조회
        Assert.IsNotNull(wick); // 실제 유물 등록 확인
        Assert.AreEqual(5, wick.EffectValue); // 추가 회피의 실제 오 퍼센트포인트 값
        var relics = RelicRunManager.EnsureInstance(); // 실제 보유 유물 관리자
        relics.TryAcquire(wick); // 실제 시험 회차에 유물 장착
        try // 명중률 공통 경로와 상태 부여 검증
        { // 기존과 추가 회피 비교 시작
            Assert.AreEqual(100, BattleHitChance.GetPercent(enemy, ally)); // 실제 은신이 없는 기본 명중률
            ally.ApplyStatusEffect(BattleStatusEffectType.Evasion, 15, 2, 1); // 은신과 구분되는 기존 회피 부여
            Assert.AreEqual(85, BattleHitChance.GetPercent(enemy, ally)); // 일반 회피만으로 은신 유물 비활성
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 2, 1)); // 이름이 아닌 실제 은신 분류의 정상 부여
            Assert.AreEqual(80, BattleHitChance.GetPercent(enemy, ally)); // 기존 회피에 오 퍼센트포인트 합산
            Assert.IsTrue(BattleHitChance.Hits(enemy, 79, ally)); // 실제 공격의 마지막 명중 정수
            Assert.IsFalse(BattleHitChance.Hits(enemy, 80, ally)); // 실제 공격의 첫 회피 정수
            enemy.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 2, 1); // 적에게도 비교용 실제 은신 부여
            Assert.AreEqual(100, BattleHitChance.GetPercent(ally, enemy)); // 적에게 적용하지 않는 아군 유물 효과
            ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1); // 기존 회피 제한을 준수할 정상 공포
            Assert.AreEqual(100, BattleHitChance.GetPercent(enemy, ally)); // 공포 중 기존 회피 제한 보존
            ally.RemoveStatusEffect(BattleStatusEffectType.Fear); // 실제 공포 해제 뒤 회피 범위 검증
            ally.ApplyStatusEffect(BattleStatusEffectType.Evasion, 98, 2, 1); // 합산에서 백에 닿을 기존 회피
            Assert.AreEqual(0, BattleHitChance.GetPercent(enemy, ally)); // 회피의 백 퍼센트 상한 확인
            ally.RemoveStatusEffect(BattleStatusEffectType.Stealth); // 실제 은신 상태 해제
            Assert.AreEqual(2, BattleHitChance.GetPercent(enemy, ally)); // 은신 종료 즉시 원래 회피만 남음 확인
        } // 기존과 추가 회피 비교 종료
        finally // 실제 보유 유물 정리
        { // 이번 시험 회차 보유 해제 시작
            relics.TryRemove(wick.RelicId); // 독립 시험에 장착한 심지 제거
        } // 이번 시험 회차 보유 해제 종료
    } // 명중 대상 비교 종료

    [UnityTest] // 선제와 정상 시작에서 무작위 아군 한 명과 두 결과 확인
    public IEnumerator DieRollsOnceAndUsesTheFirstActualAllyTurn() // 시작 방어도와 선제 뒤 공격력 타이밍 검사
    { // 흠집 난 주사위 검사 시작
        yield return new EnterPlayMode(); // 실제 전투와 회차 유물 초기화 환경 진입
        VerifyDie(); // 같은 실행 환경의 독립 전투 두 번 검사
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 흠집 난 주사위 검사 종료

    private static void VerifyDie() // 얼마를 어느 아군에게 언제 주는지 검증
    { // 시작 유물 검사 설정 시작
        var die = Resources.Load<RelicData>("Relics/ScratchedDie"); // 정식 흠집 난 주사위 자산
        Assert.IsNotNull(die); // 실제 시트 원본의 등록 여부 확인
        Assert.AreEqual(3, die.EffectValue); // 방어도 결과의 정식 고정값
        Assert.AreEqual(RelicTriggerType.BeforeBattleActions, die.TriggerType); // 일반 전투 신호의 별도 발동 방지 확인
        var relics = RelicRunManager.EnsureInstance(); // 현재 회차 소유 보관함 조회
        var original = Random.state; // 다른 검사에 영향 없는 난수 보관
        relics.TryAcquire(die); // 실제 시작 두 결과의 유물 장착
        try // 앞서 사망한 대상과 두 생존 후보의 실제 난수 검증
        { // 실제 두 전투 시작 비교
            using (var attack = new OpeningFight()) // 적이 먼저 행동하는 공격력 결과 검사
            { // 공격력 시작 검사
                var chosen = attack.Allies[2]; // 생존 후보 둘 가운데 실제 두 번째 후보
                chosen.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 3, 1); // 주사위와 독립인 기존 장기 공격력
                var expected = RepeatTwoRolls(FindSeed(1, 0)); // 고른 생존 아군과 공격력 결과의 실제 두 추첨
                Assert.IsTrue(attack.Turns.StartBattle(0, enemyFirst: true)); // 실제 적 선제 전투 초기화
                Assert.AreEqual(expected, Random.state); // 정확히 생존자와 보상 추첨 한 번씩 소비
                Assert.AreEqual(BattleTurnPhase.EnemyTurn, attack.Turns.CurrentPhase); // 선제 진영 턴 확인
                Assert.AreEqual(2, chosen.AttackPowerBonus); // 선제 중에 주사위 공격력 아직 대기
                Assert.AreEqual(0, attack.Allies.Sum(unit => unit.CurrentBlock)); // 공격력 결과에서 방어도 미지급
                Assert.IsFalse(attack.Turns.StartBattle(0)); // 이미 초기화된 전투의 중복 시작 요청 거부
                Assert.AreEqual(expected, Random.state); // 중복 시작에 새 확률 미소비
                Assert.IsTrue(attack.Turns.CompleteEnemyTurn()); // 적 선제 뒤 다음 실제 아군 턴 진입
                Assert.AreEqual(1, attack.Turns.CurrentRound); // 선제 뒤에도 최초 아군 라운드 확인
                Assert.AreEqual(3, chosen.AttackPowerBonus); // 기존 공격력 두 점에 이번 첫 아군 턴의 한 점 가산
                Assert.AreEqual(0, attack.Allies[1].AttackPowerBonus); // 대상이 아닌 다른 생존 아군의 공격력 보존
                Assert.IsTrue(attack.Turns.EndPlayerTurn()); // 첫 실제 아군 턴 종료
                Assert.AreEqual(2, chosen.AttackPowerBonus); // 한 턴 주사위 공격력만 정확히 만료
                Assert.IsTrue(attack.Turns.CompleteEnemyTurn()); // 다음 정상 아군 턴 준비
                Assert.AreEqual(2, chosen.AttackPowerBonus); // 새 턴에 주사위 공격력 중복 발동 없음
            } // 공격력 시작 검사 종료
            using (var block = new OpeningFight()) // 먼저 움직이는 적에게도 적용되는 시작 방어도 검사
            { // 방어도 시작 검사
                var expected = RepeatTwoRolls(FindSeed(0, 1)); // 생존 첫 후보와 방어도 결과의 두 추첨
                Assert.IsTrue(block.Turns.StartBattle(0, enemyFirst: true)); // 실제 적 선제 앞의 초기화
                Assert.AreEqual(expected, Random.state); // 방어도에서도 보상 추첨 정확히 한 번 확인
                Assert.AreEqual(3, block.Allies[1].CurrentBlock); // 실제 첫 생존 아군에게 방어도 세 점 즉시 지급
                Assert.AreEqual(0, block.Allies[0].CurrentBlock + block.Allies[2].CurrentBlock); // 사망 대상과 다른 생존 아군의 방어 미지급
                Assert.AreEqual(0, block.Allies.Sum(unit => unit.AttackPowerBonus)); // 방어도 결과에서 공격력 미지급
                Assert.AreEqual(2, block.Allies[1].TakeDamage(2, BattleDamageType.None).AbsorbedDamage); // 실제 선제 피해에서 시작 방어도 정상 사용
                Assert.IsTrue(block.Turns.CompleteEnemyTurn()); // 선제 끝과 첫 아군 턴 진입
                Assert.AreEqual(0, block.Allies.Sum(unit => unit.CurrentBlock)); // 기존 라운드 방어도 정리 뒤 중복 지급 없음
                Assert.AreEqual(0, block.Allies.Sum(unit => unit.AttackPowerBonus)); // 방어도 결과에 예약된 공격력 없음 확인
            } // 방어도 시작 검사 종료
            using (var normal = new OpeningFight()) // 선제 없이 시작하는 실제 아군 공격력 검사
            { // 정상 전투 시작 검사
                var expected = RepeatTwoRolls(FindSeed(0, 0)); // 첫 생존 아군의 공격력 결과 선택
                Assert.IsTrue(normal.Turns.StartBattle(0)); // 실제 게임의 일반 시작 처리
                Assert.AreEqual(expected, Random.state); // 일반 전투의 시작 선택도 두 번만 소비
                Assert.AreEqual(1, normal.Allies[1].AttackPowerBonus); // 일반 시작에서도 첫 실제 아군 턴에만 즉시 공격력 지급
                Assert.IsTrue(normal.Turns.EndPlayerTurn()); // 선제 없는 첫 실제 아군 턴 종료
                Assert.AreEqual(0, normal.Allies[1].AttackPowerBonus); // 실제 첫 턴에만 유지되는 한 점 공격력 확인
            } // 정상 전투 시작 검사 종료
        } // 실제 두 전투 시작 비교 종료
        finally // 사용한 테스트 유물 및 난수 정리
        { // 실제 회차와 난수 복원 시작
            relics.TryRemove(die.RelicId); // 검사 회차의 주사위 제거
            Random.state = original; // 원래 엔진 난수 상태 복원
        } // 실제 회차와 난수 복원 종료
    } // 시작 유물 검사 설정 종료

    private static int FindSeed(int allyIndex, int outcome) // 실제 엔진의 생존자와 보상 두 추첨 준비
    { // 두 실제 추첨 시드 탐색 시작
        for (int seed = 0; seed < 8_192; seed++) // 재현 가능한 후보의 첫 두 난수 비교
        { // 후보 난수 조회 시작
            Random.InitState(seed); // 두 추첨의 시드 초기화
            if (Random.Range(0, 2) == allyIndex && Random.Range(0, 2) == outcome) // 생존자 번호와 두 보상 결과 일치 확인
            { // 실제 후보 발견 처리 시작
                return seed; // 바로 재현할 목표 난수 시드
            } // 실제 후보 발견 처리 종료
        } // 후보 난수 조회 종료
        throw new InvalidOperationException("주사위 시작 검사 시드 없음"); // 테스트 난수 탐색 오류
    } // 두 실제 추첨 시드 탐색 종료

    private static Random.State RepeatTwoRolls(int seed) // 실제 전투가 소비해야 하는 두 난수의 상태
    { // 두 추첨의 기대 상태 준비 시작
        Random.InitState(seed); // 비교 대상의 실제 시작 시드 지정
        Random.Range(0, 2); // 실제 생존 아군 한 명의 예상 추첨
        Random.Range(0, 2); // 실제 두 보상 가운데 한 분기 예상 추첨
        Random.State expected = Random.state; // 정확히 두 번 소비한 비교 상태
        Random.InitState(seed); // 다음 실제 전투의 시작 시드 복원
        return expected; // 실제 시작 뒤 확인할 기대 상태
    } // 두 추첨의 기대 상태 준비 종료

    private sealed class OpeningFight : IDisposable // 실제 시작 전투의 적 선제와 만료 검사 환경
    { // 시작 전투 도우미 시작
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 검사에 필요한 실제 생존 적 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 손패가 없는 시작의 최소 덱 카드
        private readonly DeckData source; // 검사 전투에 넣을 실제 원본 덱
        private readonly BattleStatusEffectController statuses; // 실제 턴 지속 시간의 적용 처리
        public BattleUnitRuntime[] Allies { get; } // 하나의 사망자와 두 명의 생존 후보
        public BattleTurnRuntime Turns { get; } // 실제 시작과 진영 턴 처리

        public OpeningFight() // 등록된 기존 파티와 두 진영으로 시작 전투 생성
        { // 실제 전투 객체 준비 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 정식 출전 명단 준비
            Allies = roster.Characters.Take(3).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 실제 출전 순서의 세 아군
            Allies[0].ApplyPersistentHealth(0); // 전투 시작 이전의 첫 배치 사망자 표시
            var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy) }; // 전투를 유지할 한 생존 적 등록
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, roster.Characters[1]) }); // 정상 전투 연결을 위한 최소 덱 구성
            var deck = BattleDeckRuntime.Create(source, Allies, 3, 252); // 실제 고정 셔플의 정식 전투 덱
            Turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), Allies, enemies, 0, BattleType.Normal); // 첫 손패 없이 실제 선제와 아군 턴 처리
            statuses = new BattleStatusEffectController(Turns, Allies, enemies, new BattleStatusEffectProcessor()); // 공통 상태 수명과 실제 턴의 정상 연결
        } // 실제 전투 객체 준비 종료

        public void Dispose() // 실제 상태 구독과 임시 원본 제거
        { // 독립 검사 정리 시작
            statuses.Dispose(); // 실제 상태 관리자 구독 해제
            Turns.Dispose(); // 이전 테스트 턴의 신호 해제
            UnityEngine.Object.Destroy(source); // 임시 덱 원본 제거
            UnityEngine.Object.Destroy(card); // 임시 기본 카드 제거
            UnityEngine.Object.Destroy(enemy); // 임시 적 원본 제거
        } // 독립 검사 정리 종료
    } // 시작 전투 도우미 종료
} // 전투 시작 유물 검사 종료
