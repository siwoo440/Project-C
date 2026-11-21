using System; // 격리 전투 검사 개체 정리
using System.Collections; // 플레이 모드 전환 대기
using System.Linq; // 실제 선택된 아군과 적 목록 비교
using NUnit.Framework; // 전투 시작과 턴 경계 결과 검증
using UnityEngine; // 원본 유물과 독립 전투 데이터 생성
using UnityEngine.TestTools; // Unity 플레이 모드 회귀 검사
using Object = UnityEngine.Object; // 임시 전투 데이터 정리
using Random = UnityEngine.Random; // 실제 유물의 시작 추첨 재현

public sealed class WinterfallCorrosionAndTurnRelicTests // 시작과 주기의 세 가지 전투 유물 회귀 검사
{ // 전투 시작 유물 검사 시작
    [UnityTest] // 전투 시작 저항 약화와 실제 적 턴 만료 검사
    public IEnumerator FractureStoneReducesMagicalMitigationAcrossTwoEnemyTurns() // 선제 중 약화 적용과 기존 방어 계산 경계 확인
    { // 실금석 검사 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 데이터 생성 환경 진입
        VerifyFractureStone(); // 적 시작 상태 및 피해 계산 검증
        yield return new ExitPlayMode(); // 검사 전투 종료와 에디터 복귀
    } // 실금석 검사 모드 종료

    private static void VerifyFractureStone() // 실제 마법 방어 분수와 음의 저항 결과 확인
    { // 실금석 효과 검사 시작
        Acquire("NecrateFractureStone"); // 정식 네크레이트 실금석 원본 획득
        using var battle = new OpeningBattle(); // 서로 다른 실제 저항 수치가 있는 전투 준비
        var plain = battle.Enemies[0]; // 마법 저항이 영인 첫 대상
        var resistant = battle.Enemies[1]; // 마법 저항의 기존 플랫 수치가 이십오인 둘째 적
        var immune = battle.Enemies[2]; // 정상 약화 면역을 확인할 적
        var fallen = battle.Enemies[3]; // 전투 시작 전에 제외할 전투불능 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 정상적인 적 상태 면역 설정
        fallen.TakeDamage(1000, BattleDamageType.None); // 생존 대상이 아닌 실제 쓰러진 적 준비
        Assert.IsTrue(battle.Turns.StartBattle(0, true)); // 실제 적 선제를 포함한 전투 시작
        Assert.AreEqual(BattleTurnPhase.EnemyTurn, battle.Turns.CurrentPhase); // 첫 아군 카드 이전부터 약화가 있는 선제 확인
        Assert.AreEqual(2, plain.GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown).RemainingTurns); // 선제 적 턴 시작의 실제 저항 약화 기간 확인
        Assert.AreEqual(5, resistant.GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown).EffectiveValue); // 서브 상태에 있는 원본 퍼센트포인트 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown)); // 일반 약화 면역 적용 확인
        Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown)); // 시작에 쓰러진 적 제외 확인
        Assert.AreEqual(105, plain.PreviewDamage(100, BattleDamageType.Magical).FinalDamage); // 마법 저항 영에서 음의 경감으로 원본 피해보다 다섯 증가 확인
        Assert.AreEqual(85, resistant.PreviewDamage(100, BattleDamageType.Magical).FinalDamage); // 기존 마법 피해 팔십에 원본 기준 다섯 추가 확인
        Assert.AreEqual(85, resistant.TakeDamage(100, BattleDamageType.Magical).AppliedDamage); // 마법 피해 예고와 실제 계산 일치 확인
        Assert.AreEqual(100, resistant.PreviewDamage(100, BattleDamageType.Physical).FinalDamage); // 물리 피해가 별도 마법 약화로 바뀌지 않는지 확인
        Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 선제 적 턴 실제 완료
        Assert.AreEqual(1, plain.GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown).RemainingTurns); // 첫 번째 실제 적 턴에서 한 번 감소 확인
        Assert.IsTrue(battle.Turns.EndPlayerTurn()); // 다음 실제 적 턴 시작
        Assert.AreEqual(1, plain.GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown).RemainingTurns); // 아군 턴 종료로 줄어들지 않는 적 상태 확인
        Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 정상 완료
        Assert.IsNull(plain.GetStatusEffect(BattleStatusEffectType.CorrosionResistanceDown)); // 두 적 턴 이후 저항 약화 상태 제거 확인
        Assert.AreEqual(80, resistant.PreviewDamage(100, BattleDamageType.Magical).FinalDamage); // 저항 약화 만료 뒤 기존 마법 피해 비율 복귀 확인
    } // 실금석 효과 검사 종료

    [UnityTest] // 실제 세 번째 및 여섯 번째 아군 턴 발동 검사
    public IEnumerator DustClockUsesEveryThirdActualAllyTurnWithOrWithoutAmbush() // 선제와 무관한 아군 턴 수 및 정신력 하한 확인
    { // 먼지시계 검사 모드 시작
        yield return new EnterPlayMode(); // 유물과 턴 관리자 생성 환경 진입
        VerifyDustClock(); // 두 시작 방식의 실제 반복 턴 검증
        yield return new ExitPlayMode(); // 검사 전투 종료와 에디터 복귀
    } // 먼지시계 검사 모드 종료

    private static void VerifyDustClock() // 실제 전투의 세 번째와 여섯 번째 아군 턴 비교
    { // 먼지시계 주기 검사 시작
        Acquire("AbyssDustClock"); // 정식 어비스 먼지시계 보유
        foreach (bool ambush in new[] { false, true }) // 일반과 선제 전투의 같은 아군 턴 순회
        { // 시작 방식별 검사 시작
            using var battle = new OpeningBattle(); // 독립 턴과 원본 정신력의 실제 전투 구성
            var low = battle.Enemies[1]; // 정신력 하한을 확인할 두 번째 생존 적
            low.ChangeMental(-49, BattleMentalChangeReason.RelicEffect); // 낮은 적의 원래 정신력 한 점 준비
            var fallen = battle.Enemies[3]; // 시작과 반복에서 제외할 쓰러진 적
            fallen.TakeDamage(1000, BattleDamageType.None); // 다른 적을 보존하면서 네 번째 적만 처치
            Assert.IsTrue(battle.Turns.StartBattle(0, ambush)); // 다르게 시작해도 첫 실제 아군 턴 준비
            if (ambush) // 적 선제 턴을 먼저 사용하는 경우 확인
            { // 선제와 첫 아군 턴 경계 검사 시작
                Assert.AreEqual(50, battle.Enemies[0].CurrentMental); // 적 선제 턴 시작에 먼지시계 발동 없음 확인
                Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 같은 첫 라운드의 실제 아군 턴 시작
            } // 선제와 첫 아군 턴 경계 검사 종료
            for (int round = 1; round <= 6; round++) // 첫 시작부터 두 번째 주기까지 실제 진행
            { // 실제 아군 턴 확인 시작
                Assert.AreEqual(round, battle.Turns.CurrentRound); // 적 선제를 별도 아군 턴으로 세지 않는지 확인
                int expected = 50 - 3 * (round / 3); // 실제 세 번째마다 세 점을 내리는 기대 정신력
                Assert.AreEqual(expected, battle.Enemies[0].CurrentMental); // 실제 아군 턴의 첫 유효 적 정신력 확인
                Assert.AreEqual(expected, battle.Enemies[2].CurrentMental); // 같은 아군 턴마다 두 번째 유효 적도 동일하게 적용 확인
                Assert.AreEqual(round < 3 ? 1 : 0, low.CurrentMental); // 정신력 영 하한과 이하로 감소 방지 확인
                Assert.AreEqual(50, fallen.CurrentMental); // 전투불능 적의 원래 정신력 확인
                Assert.IsTrue(battle.Turns.EndPlayerTurn()); // 방금 발동한 아군 턴 종료
                Assert.AreEqual(expected, battle.Enemies[0].CurrentMental); // 이어진 같은 라운드의 적 턴에 중복 감소 없음 확인
                Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴 정상 시작
            } // 실제 아군 턴 확인 종료
            Assert.AreEqual(44, battle.Enemies[0].CurrentMental); // 일곱 번째 아군 턴에서 같은 주기의 중복 발동 없음 확인
        } // 시작 방식별 검사 종료
    } // 먼지시계 주기 검사 종료

    [UnityTest] // 맥박줄의 실제 시작 추첨과 기존 공격력 중첩 검사
    public IEnumerator PulseCordUsesTheSameOpeningAllyAndStacksWithScratchedDie() // 선택한 아군 정신력과 두 실제 아군 턴의 공격력 확인
    { // 맥박줄 검사 모드 시작
        yield return new EnterPlayMode(); // 시작 유물과 전투 대상 준비 환경 진입
        VerifyPulseCord(); // 실제 선제 및 대상 사망의 시작 보상 검증
        yield return new ExitPlayMode(); // 검사 전투 종료와 에디터 복귀
    } // 맥박줄 검사 모드 종료

    private static void VerifyPulseCord() // 선택 원본과 같은 아군의 독립 공격력 검사
    { // 맥박줄 효과 검증 시작
        Acquire("ScratchedDie"); // 기존 흠집 난 주사위 시작 유물 획득
        Acquire("CorrodedPulseCord"); // 정식 침식된 맥박줄 원본 획득
        using (var battle = new OpeningBattle()) // 실제 선제와 이후 턴 공유 시작 효과 검사
        { // 양쪽 시작 유물의 같은 대상 검증 시작
            battle.Allies[0].TakeDamage(1000, BattleDamageType.None); // 쓰러진 아군을 두 추첨의 후보에서 제외
            int chosenIndex = PrepareMatchingOpeningRoll(); // 두 시작 유물이 같은 생존자를 고르는 실제 난수 준비
            var chosen = battle.Allies[chosenIndex + 1]; // 쓰러진 첫 후보를 제외한 실제 선택 아군
            var other = battle.Allies[2 - chosenIndex]; // 추첨에서 고르지 않은 다른 생존 아군
            chosen.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 4, 4, 1); // 기존 일반 공격력 강화의 별도 중첩 대조
            Assert.IsTrue(battle.Turns.StartBattle(0, true)); // 기존 주사위와 맥박줄 모두 같은 실제 전투 시작에 적용
            Assert.AreEqual(47, chosen.CurrentMental); // 선제 이전 무작위 한 아군의 정신력 세 점 감소 확인
            Assert.AreEqual(50, other.CurrentMental); // 선택하지 않은 생존자의 정신력 보존 확인
            Assert.AreEqual(50, battle.Allies[0].CurrentMental); // 사망자의 기존 정신력과 효과 제외 확인
            Assert.IsNull(chosen.GetStatusEffect(BattleStatusEffectType.PulseCordAttackPowerUp)); // 적 선제 동안 실제 아군 턴 공격력 미부여 확인
            Assert.AreEqual(4, chosen.AttackPowerBonus); // 적 선제 중 기존 일반 강화만 유지 확인
            Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 선제 이후 첫 실제 아군 턴 시작
            Assert.AreEqual(7, chosen.AttackPowerBonus); // 일반 네 점과 주사위 한 점 및 맥박줄 두 점 합산 확인
            Assert.AreEqual(2, chosen.GetStatusEffect(BattleStatusEffectType.PulseCordAttackPowerUp).RemainingTurns); // 첫 실제 아군 턴의 독립 공격력 기간 확인
            Assert.IsTrue(battle.Turns.EndPlayerTurn() && battle.Turns.CompleteEnemyTurn()); // 두 번째 실제 아군 턴까지 진행
            Assert.AreEqual(6, chosen.AttackPowerBonus); // 한 턴 주사위만 만료되고 일반과 맥박줄 유지 확인
            Assert.IsTrue(battle.Turns.EndPlayerTurn() && battle.Turns.CompleteEnemyTurn()); // 두 번째 실제 아군 턴 종료와 다음 턴 시작
            Assert.IsNull(chosen.GetStatusEffect(BattleStatusEffectType.PulseCordAttackPowerUp)); // 세 번째 아군 턴의 맥박줄 만료 확인
            Assert.AreEqual(4, chosen.AttackPowerBonus); // 기존 일반 공격력 강화가 독립적으로 유지되는지 확인
            Assert.AreEqual(47, chosen.CurrentMental); // 이후 아군 턴에 시작 비용 재적용 차단 확인
        } // 양쪽 시작 유물의 같은 대상 검증 종료
        using (var battle = new OpeningBattle()) // 첫 실제 아군 턴 전에 선택자가 사망하는 전투
        { // 사망 시 보상 이전 차단 검증 시작
            Assert.IsTrue(battle.Turns.StartBattle(0, true)); // 살아 있는 실제 무작위 대상의 적 선제 시작
            var chosen = battle.Allies.Single(ally => ally.CurrentMental == 47); // 비용이 실제로 반영된 시작 선택자 확인
            chosen.TakeDamage(1000, BattleDamageType.None); // 기존 선택자의 적 선제 중 실제 사망 처리
            Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 새 실제 아군 턴의 남은 생존자를 확인
            Assert.IsTrue(battle.Allies.All(ally => ally.GetStatusEffect(BattleStatusEffectType.PulseCordAttackPowerUp) == null)); // 사망 선택자의 공격력을 다른 아군에게 재추첨하지 않음 확인
            Assert.IsTrue(battle.Allies.Where(ally => !ally.IsDead).All(ally => ally.CurrentMental == 50)); // 두 번째 대상에게 비용을 새로 적용하지 않음 확인
        } // 사망 시 보상 이전 차단 검증 종료
    } // 맥박줄 효과 검증 종료

    private static int PrepareMatchingOpeningRoll() // 같은 실제 생존자에게 주사위와 맥박줄을 적용할 난수 찾기
    { // 시작 난수 조회 시작
        for (int seed = 0; seed < 1000; seed++) // 한정된 범위의 재현 가능한 난수 상태 검사
        { // 시작 추첨 비교 시작
            Random.InitState(seed); // 이번 시작 유물 비교의 엔진 난수 설정
            int dieAlly = Random.Range(0, 2); // 현재 주사위의 첫 번째 생존자 추첨 재현
            int dieReward = Random.Range(0, 2); // 기존 주사위의 공격력 보상 선택 재현
            int pulseAlly = Random.Range(0, 2); // 이어지는 독립 맥박줄 대상 재현
            if (dieReward == 0 && dieAlly == pulseAlly) // 두 유물이 같은 아군의 공격력을 올리는 난수 확인
            { // 일치 난수 준비 시작
                Random.InitState(seed); // 실제 전투 시작에서 같은 추첨 순서 재현
                return dieAlly; // 배열에서 기대할 실제 생존자 순번 반환
            } // 일치 난수 준비 종료
        } // 시작 추첨 비교 종료
        Assert.Fail("공격력 보상과 맥박줄의 동일 대상 난수 미발견"); // 준비 단계에서 비교 난수를 구하지 못한 결과 기록
        return -1; // 단언 실패 뒤 반환 계약 유지
    } // 시작 난수 조회 종료

    private static void Acquire(string key) // 실제 회차에 보유할 원본 유물 획득
    { // 원본 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 실제 등록한 유물 카탈로그 원본 조회
        Assert.IsNotNull(relic, key); // 정상적인 실제 유물 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 합법적인 회차 보유 유물 획득 확인
    } // 원본 유물 획득 종료

    private sealed class OpeningBattle : IDisposable // 시작 전에 참가자 상태를 조정할 수 있는 독립 전투
    { // 독립 전투 준비 시작
        private readonly Random.State previousRandom = Random.state; // 검사 전에 쓰던 전역 난수 상태 보관
        private readonly StartRelicTestAssets assets = new StartRelicTestAssets(); // 테스트용 임시 원본 모음
        private readonly DeckData deckSource; // 생성한 아군 카드 원본 덱
        private readonly BattleStatusEffectController statuses; // 실제 진영 턴 시작과 종료 효과 연결
        internal BattleUnitRuntime[] Allies { get; } // 현재 전투에 참가한 실제 생존 아군 목록
        internal BattleUnitRuntime[] Enemies { get; } // 실제 저항을 별도로 설정한 검사 적 목록
        internal BattleTurnRuntime Turns { get; } // 실제 전투의 진영 턴 관리자

        internal OpeningBattle() // 실제 원본과 상태 처리로 독립 시작 전투 구성
        { // 전투 원본 연결 시작
            Allies = assets.Characters.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 독립 소유자 원본 세 개의 실제 아군 생성
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(assets.Standard), BattleUnitRuntime.CreateEnemy(assets.Resistant), BattleUnitRuntime.CreateEnemy(assets.Standard), BattleUnitRuntime.CreateEnemy(assets.Standard) }; // 두 저항 수치와 비교할 실제 적 네 명 구성
            deckSource = DeckData.CreateRuntime(new[] { new DeckCardEntry(assets.Card, assets.Characters[0]) }); // 첫 번째 실제 소유자의 기본 전투 덱 구성
            var deck = BattleDeckRuntime.Create(deckSource, Allies, 4, 254); // 고정 셔플의 실제 덱 관리자 생성
            Turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), Allies, Enemies, 0, BattleType.Normal); // 덱 사용 없이도 전투 시작과 턴 경계를 그대로 실행
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 아군 및 적 상태의 유효 기간 연결
        } // 전투 원본 연결 종료

        public void Dispose() // 실제 전투 연결과 임시 원본 정리
        { // 전투 정리 시작
            statuses.Dispose(); // 상태 효과의 턴 감시 해제
            Turns.Dispose(); // 실제 전투 및 참가자 이벤트 해제
            Object.Destroy(deckSource); // 사용한 임시 덱 원본 해제
            assets.Dispose(); // 시작 카드 및 적과 아군 생성 데이터 해제
            Random.state = previousRandom; // 다음 검사에 기존 난수 상태 복원
        } // 전투 정리 종료
    } // 독립 전투 준비 종료

    private sealed class StartRelicTestAssets : IDisposable // 원래 게임 원본을 바꾸지 않는 독립 검사 자료
    { // 검사 원본 자료 시작
        internal readonly CharacterData[] Characters = Enumerable.Range(0, 3).Select(_ => ScriptableObject.CreateInstance<CharacterData>()).ToArray(); // 중복 소유자를 피한 아군 원본 세 개
        internal readonly EnemyData Standard = ScriptableObject.CreateInstance<EnemyData>(); // 마법 저항 영의 실제 적 원본
        internal readonly EnemyData Resistant = ScriptableObject.CreateInstance<EnemyData>(); // 마법 저항 이십오의 실제 적 원본
        internal readonly CardData Card = ScriptableObject.CreateInstance<CardData>(); // 정상 덱 구성에 사용할 한 카드 원본

        internal StartRelicTestAssets() // 시작 정신력과 서로 다른 실제 저항 설정
        { // 전투 데이터 설정 시작
            foreach (var character in Characters) // 별도 소유자의 같은 초기 능력치 준비
            { // 개별 시작 능력치 설정 시작
                JsonUtility.FromJsonOverwrite("{\"maxHealth\":100,\"initialMental\":50}", character); // 각 아군의 공통 시작 체력과 정신력 설정
            } // 개별 시작 능력치 설정 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"initialMental\":50}", Standard); // 저항 없는 다수 적에 독립 체력 설정
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"initialMental\":50,\"magicalResistance\":25}", Resistant); // 저항 수치가 있는 적의 기존 방어 계산 대조
        } // 전투 데이터 설정 종료

        public void Dispose() // 검사 원본 객체 제거
        { // 임시 원본 정리 시작
            foreach (var character in Characters) // 개별 임시 데이터 조회
            { // 아군 데이터 해제 시작
                Object.Destroy(character); // 시작 유물 검증의 독립 아군 원본 해제
            } // 아군 데이터 해제 종료
            Object.Destroy(Standard); // 낮은 저항의 적 원본 해제
            Object.Destroy(Resistant); // 높은 저항의 적 원본 해제
            Object.Destroy(Card); // 임시 카드 원본 해제
        } // 임시 원본 정리 종료
    } // 검사 원본 자료 종료
} // 전투 시작 유물 검사 종료
