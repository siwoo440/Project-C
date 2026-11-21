using System; // 실제 전투 구성 정리
using System.Collections; // 플레이 모드와 실제 턴 검사 대기
using System.Linq; // 참가자와 반복 덱 원본 준비
using NUnit.Framework; // 실제 상태와 난수 및 카드 수 검증
using UnityEngine; // 실제 유물과 전투 자료 조회
using UnityEngine.TestTools; // 실제 아군과 적 턴의 플레이 모드 검사
using Object = UnityEngine.Object; // 실제 임시 전투 원본 해제
using Random = UnityEngine.Random; // 기름등의 실제 확률과 난수 상태 확인

public sealed class WinterfallBanquetTurnRelicTests // 만찬장 개막과 주기 중독 및 출혈 보상 검사
{ // 실제 유물 전투 검사 시작
    [UnityTest] // 실제 선제 적 턴 및 조우 종류별 개막 약화
    public IEnumerator LordsWaxWeakensLivingNormalAndEliteEnemiesBeforeTheAmbush() // 생존 적 약화와 우두머리 제외 검사
    { // 실제 조우 구분 검사 시작
        yield return new EnterPlayMode(); // 전투의 실제 유물 인벤토리 연결
        Acquire("LordsWaxSeal"); // 원본 영주의 봉랍을 정상 경로로 획득
        foreach (var type in new[] { BattleType.Normal, BattleType.Elite, BattleType.Boss }) // 조우 종류별 실제 전투를 한 번씩 구성
        { // 선제와 면역 및 전투불능 확인 시작
            using var battle = new Trial(type); // 선택한 조우 원본 종류의 실제 전투
            battle.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 4, 1); // 원래 약화 면역을 가진 생존 적 준비
            battle.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 개막 전에 쓰러진 네 번째 적 설정
            Assert.IsTrue(battle.Turn.StartBattle(0, true)); // 첫 아군 행동보다 먼저 오는 실제 적 턴 시작
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, battle.Turn.CurrentPhase); // 실제 선제 적 턴에서 바로 적용 확인
            var first = battle.Enemies[0]; // 실제 전투 개막의 첫 생존 적
            if (type == BattleType.Boss) // 우두머리 전투의 조건 제외 확인
            { // 우두머리의 원래 생존 적 상태 확인 시작
                Assert.IsTrue(battle.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown) == null)); // 우두머리 조우 전체의 공격력 약화 없음 확인
                continue; // 다음 독립 전투로 이동
            } // 우두머리의 원래 생존 적 상태 확인 종료
            Assert.AreEqual(2, first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown).EffectiveValue); // 선제 첫 적의 공격력 감소 두 점 확인
            Assert.AreEqual(2, battle.Enemies[1].GetStatusEffect(BattleStatusEffectType.AttackPowerDown).EffectiveValue); // 다른 적에게 함께 적용된 두 점 확인
            Assert.IsNull(battle.Enemies[2].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 실제 디버프 면역 유지 확인
            Assert.IsNull(battle.Enemies[3].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 전투불능 적 제외 확인
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 실제 선제 적 턴 하나 종료
            Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown).RemainingTurns); // 선제 포함 첫 적 턴 이후 남은 약화 기간
            Assert.IsTrue(battle.Turn.EndPlayerTurn() && battle.Turn.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 두 턴 이후 원래 공격력 회복 확인
        } // 선제와 면역 및 전투불능 확인 종료
        yield return new ExitPlayMode(); // 개막 검사 환경 종료
    } // 실제 조우 구분 검사 종료

    [UnityTest] // 지정 아군 턴에 한 번만 사용하는 기름등 난수
    public IEnumerator BanquetOilUsesOneGroupRollOnActualAllyRoundsFourAndEight() // 선제 보정 및 그룹 확률과 실제 중독 상태 검사
    { // 실제 주기 중독 검사 시작
        yield return new EnterPlayMode(); // 전투의 실제 진영 턴 구동 시작
        Acquire("BanquetOilLamp"); // 원본 기름등의 원래 확률과 주기 획득
        using (var battle = new Trial(BattleType.Normal)) // 적이 선제하는 실제 전투 구성
        { // 첫 선제 이후 지정 턴의 독립 난수 확인 시작
            battle.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 20, 1); // 장기 비교를 위한 정상 상태 면역 준비
            battle.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 중독에서 제외될 전투불능 적 준비
            Assert.IsTrue(battle.Turn.StartBattle(1, true)); // 실제 적 선제의 시작
            Random.InitState(71); // 아군 턴이 아니면 소비하지 않아야 할 비교 난수
            var unopened = Random.state; // 첫 실제 아군 턴 이전 난수 상태 보관
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 선제 이후 첫 실제 아군 턴 진입
            Assert.AreEqual(unopened, Random.state); // 선제와 첫 아군 턴의 확률 미사용 확인
            AdvanceTo(battle, 3); // 세 번째 실제 아군 턴까지 정상 왕복
            var failed = RollOnNextRound(battle, expectedRoll: 30); // 네 번째 턴의 실패 경계에서 난수 한 번 사용
            Assert.AreEqual(failed, Random.state); // 실패도 네 번째 턴 난수 한 번만 소비 확인
            Assert.IsTrue(battle.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Poison) == null)); // 공용 실패가 모든 적 대상에게 동일한지 확인
            AdvanceTo(battle, 7); // 두 번째 주기 직전의 실제 일곱 번째 턴 진입
            var passed = RollOnNextRound(battle, expectedRoll: 29); // 여덟 번째 턴의 성공 경계에서 공용 추첨
            Assert.AreEqual(passed, Random.state); // 생존 후보 수와 무관한 정확히 한 번의 확률 소비 확인
            Assert.AreEqual(2, battle.Enemies[0].GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 첫 적의 중독 두 점 확인
            Assert.AreEqual(2, battle.Enemies[1].GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 두 번째 적의 두 턴 중독 확인
            Assert.IsNull(battle.Enemies[2].GetStatusEffect(BattleStatusEffectType.Poison)); // 성공한 추첨에서도 기존 상태 면역 유지 확인
            Assert.IsNull(battle.Enemies[3].GetStatusEffect(BattleStatusEffectType.Poison)); // 성공한 추첨에서도 전투불능 적 제외 확인
            AdvanceTo(battle, 9); // 중독이 한 차례 진행된 실제 아군 턴 시작
            Assert.AreEqual(1, battle.Enemies[0].GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 실제 적 턴 한 번의 중독 기간 차감 확인
            AdvanceTo(battle, 10); // 두 중독 틱을 지난 실제 아군 턴 시작
            Assert.IsNull(battle.Enemies[0].GetStatusEffect(BattleStatusEffectType.Poison)); // 두 적 턴 이후 중독 자연 해제 확인
        } // 첫 선제 이후 지정 턴의 독립 난수 확인 종료
        yield return new ExitPlayMode(); // 주기 검사를 마친 실제 모드 종료
    } // 실제 주기 중독 검사 종료

    [UnityTest] // 실제 출혈 중첩과 사용 카드 및 다음 아군 손패 확인
    public IEnumerator BlackCutleryDrawsOnceAcrossLivingEnemyStacksAndWaitsForCardSettlement() // 실제 여러 적의 중첩 합과 손패 상한 확인
    { // 식기함의 생존과 출혈 조건 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 상태와 카드 정산 환경 진입
        Acquire("BlackCutleryCase"); // 원본 식기함을 정상 유물 보유 목록에 추가
        using (var battle = new Trial(BattleType.Normal)) // 견고한 손패 공간과 다수 적을 가진 실제 전투
        { // 출혈 합계 및 카드 해결 완료 비교 시작
            var first = battle.Enemies[0]; // 서로 다른 중첩 조건을 비교할 생존 적
            var second = battle.Enemies[1]; // 세 번째 중첩을 받을 실제 다른 생존 적
            var fallen = battle.Enemies[2]; // 쓰러진 동안 출혈에서 제외할 적
            var owner = battle.Allies[0]; // 실제 상태의 아군 발생자
            AddBleed(fallen, owner); // 개막 전 비교 대상에 실제 출혈 한 중첩 준비
            fallen.TakeDamage(1000, BattleDamageType.None); // 출혈이 있던 비교 적을 전투불능으로 설정
            Assert.IsTrue(battle.Turn.StartBattle(battle.Deck.MaxHandSize)); // 실제 손패 상한만큼 기본 카드 지급
            int limit = battle.Deck.HandCount; // 출혈 보상 전에 가득 찬 실제 손패
            AddBleed(first, owner); // 첫 생존 적에 원래 한 중첩 부여
            AddBleed(first, owner); // 동일 적에 두 번째 활성 중첩 부여
            int pile = battle.Deck.DrawPile.Count; // 실패 시 유지할 실제 덱 순서의 개수
            AddBleed(second, owner); // 실제 세 중첩이지만 손패가 가득 찬 상태
            Assert.AreEqual(limit, battle.Deck.HandCount); // 손패 상한에서 추가 카드 이동 금지 확인
            Assert.AreEqual(pile, battle.Deck.DrawPile.Count); // 초과 드로우가 덱을 버리지 않는지 확인
            Assert.AreEqual(0, battle.Deck.DiscardPile.Count); // 초과 드로우에 따른 새 버림 없음 확인
            InvokeResolution(battle, "BeginCardResolution", false); // 실제 카드 해결에서 원본을 움직이기 전의 범위 진입
            AddBleed(second, owner); // 카드 피해 직후의 실제 적 상태 신호
            Assert.AreEqual(limit, battle.Deck.HandCount); // 원본 이동 전 손패 보상 지연 확인
            Assert.IsTrue(battle.Deck.TryPlayAndDraw(battle.Deck.Hand[0], 0, out _)); // 정상 카드 사용으로 손패 한 자리 열기
            Assert.AreEqual(limit - 1, battle.Deck.HandCount); // 실제 카드 이동 직후 아직 남은 빈자리 확인
            InvokeResolution(battle, "EndCardResolution"); // 카드 사용 이후 실제 해결 범위를 마쳐 출혈 보상 처리
            Assert.AreEqual(limit, battle.Deck.HandCount); // 사용을 마친 손패 빈자리에 카드 한 장 지급 확인
            Assert.IsTrue(battle.Deck.DiscardCard(battle.Deck.Hand[0])); // 같은 턴의 추가 드로우 여부를 볼 손패 공간 준비
            AddBleed(second, owner); // 출혈이 계속되더라도 이 턴의 드로우는 이미 정산
            Assert.AreEqual(limit - 1, battle.Deck.HandCount); // 같은 턴의 중복 출혈 드로우 차단 확인
            Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 실제 적 턴으로 바꿔 다음 턴 지급 준비
            AddBleed(first, owner); // 적 턴에 나타난 출혈로 즉시 아군 카드 지급 금지
            Assert.AreEqual(0, battle.Deck.HandCount); // 적 턴의 손패 미지급 확인
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 남은 활성 출혈이 있는 다음 실제 아군 턴 진입
            Assert.AreEqual(2, battle.Deck.HandCount); // 다음 아군 턴 기본 한 장과 식기함 추가 한 장 확인
            Assert.IsTrue(battle.Deck.IsStateValid()); // 정상 사용 및 추가 드로우의 단일 카드 위치 확인
        } // 출혈 합계 및 카드 해결 완료 비교 종료
        yield return new ExitPlayMode(); // 실제 식기함 회귀 검사 종료
    } // 식기함의 생존과 출혈 조건 검사 종료

    private static void AddBleed(BattleUnitRuntime target, BattleUnitRuntime owner) // 실제 유지되는 출혈 중첩 한 개 추가
    { // 원래 기존 상태 적용과 이벤트 전달 시작
        target.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 5, 5, owner); // 다음 적 턴 비교까지 남길 기본 출혈 한 중첩
    } // 원래 기존 상태 적용과 이벤트 전달 종료

    private static void InvokeResolution(Trial battle, string method, params object[] arguments) // 정상 카드의 실제 해결 시작과 끝 연결
    { // 검사에서 원래 카드 정산 범위 호출 시작
        var resolve = typeof(BattleTurnRuntime).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 게임의 실제 비공개 카드 정산 진입점 조회
        Assert.IsNotNull(resolve, method); // 정확한 실제 메서드 연결 확인
        resolve.Invoke(battle.Turn, arguments); // 원래 카드 사용과 같은 실제 턴 상태 연결
    } // 검사에서 원래 카드 정산 범위 호출 종료

    private static void AdvanceTo(Trial battle, int round) // 명시한 실제 아군 턴까지 정상 진영 진행
    { // 테스트 라운드 이동 시작
        while (battle.Turn.CurrentRound < round) // 실제 아군 턴 번호가 목표 전인지 확인
        { // 다음 실제 라운드 시작
            Assert.IsTrue(battle.Turn.EndPlayerTurn() && battle.Turn.CompleteEnemyTurn()); // 정상 종료와 다음 아군 진입 확인
        } // 다음 실제 라운드 종료
    } // 테스트 라운드 이동 종료

    private static Random.State RollOnNextRound(Trial battle, int expectedRoll) // 실제 적 턴 뒤 확률 경계에 맞는 난수 상태 준비
    { // 결정적 실제 난수 비교 시작
        Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 지정한 다음 아군 차례 전의 실제 적 턴 진입
        for (int seed = 0; seed < 10000; seed++) // 원하는 경계 난수를 갖는 실제 시드 순회
        { // 시드별 한 번의 원래 난수 확인 시작
            Random.InitState(seed); // 현재 시드로 실제 전역 난수 설정
            var before = Random.state; // 원래 확인 전의 실제 난수 상태
            if (Random.Range(0, 100) == expectedRoll) // 정확한 성공 마지막과 실패 첫 난수 경계 확인
            { // 실제 단일 소비 결과 보관 시작
                var after = Random.state; // 한 번 소비한 실제 난수 상태 보관
                Random.state = before; // 실제 전투가 같은 순서로 확률을 사용하도록 복원
                Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 실제 다음 아군 턴에서 유물 확률 실행
                return after; // 정상 한 번 소비의 실제 기대 상태 반환
            } // 실제 단일 소비 결과 보관 종료
        } // 시드별 한 번의 원래 난수 확인 종료
        throw new InvalidOperationException("확률 경계 시드 없음"); // 재현할 실제 난수가 없는 검사 실패
    } // 결정적 실제 난수 비교 종료

    private static void Acquire(string name) // 실제 보유 유물 원본 획득
    { // 전투에 사용할 원본 리소스 연결 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 동일한 게임 리소스의 실제 유물 조회
        Assert.IsNotNull(relic, name); // 등록된 유물 파일 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 기존 회차 보유 경로에 직접 추가
    } // 전투에 사용할 원본 리소스 연결 종료

    private sealed class Trial : IDisposable // 실제 유닛과 턴 및 덱의 격리 환경
    { // 테스트 전투 구성 시작
        private readonly CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 아군 참가자 원본 생성
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 동일한 기본 적 참가자 원본 생성
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 손패 이동을 비교할 카드 원본
        private readonly DeckData source; // 원래 카드 소유자와 실제 전투 덱 연결
        private readonly BattleStatusEffectController statuses; // 실제 적 턴 종료의 상태 피해와 기간 처리기
        private readonly Random.State initialRandom = Random.state; // 검사 전에 보관한 전역 난수 상태
        internal readonly BattleUnitRuntime[] Allies; // 현재 전투의 실제 아군 배열
        internal readonly BattleUnitRuntime[] Enemies; // 현재 전투의 실제 적 배열
        internal readonly BattleDeckRuntime Deck; // 실제 손패 및 덱의 카드 영역
        internal readonly BattleTurnRuntime Turn; // 두 진영과 원래 상태를 연결하는 전투 턴

        internal Trial(BattleType battleType) // 테스트할 조우 등급과 정상 전투 상태 구성
        { // 실제 카드와 적 및 아군 초기화 시작
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", character); // 출혈과 무관한 충분한 아군 체력 설정
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200}", enemy); // 장기 주기 검사의 생존 적 체력 설정
            Allies = new[] { BattleUnitRuntime.CreateAlly(character) }; // 실제 전투에 배치된 생존 아군
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemy)).ToArray(); // 생존 면역 사망 비교용 실제 적 넷
            source = DeckData.CreateRuntime(Enumerable.Range(0, 8).Select(_ => new DeckCardEntry(card, character)).ToArray()); // 소유자는 같고 위치가 독립인 실제 여덟 장 구성
            Deck = BattleDeckRuntime.Create(source, Allies, 3, 266); // 손패 상한과 별도 섞기 시드를 가진 실제 덱
            Turn = new BattleTurnRuntime(Deck, new BattleActionPointRuntime(3), Allies, Enemies, 1, battleType); // 매 아군 턴에 기본 한 장을 받는 실제 전투
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 상태 피해와 턴 만료를 실제 진행에 연결
        } // 실제 카드와 적 및 아군 초기화 종료

        public void Dispose() // 검사 전투와 임시 원본 정리
        { // 검사를 마친 전투 연결 해제 시작
            statuses.Dispose(); // 진영별 상태 기간의 실제 구독 해제
            Turn.Dispose(); // 전투 참가자와 턴 상태 연결 해제
            Object.Destroy(source); // 임시 카드 덱 원본 정리
            Object.Destroy(card); // 임시 카드 원본 정리
            Object.Destroy(character); // 임시 아군 원본 정리
            Object.Destroy(enemy); // 임시 적 원본 정리
            Random.state = initialRandom; // 검사 전의 전역 난수 상태 복원
        } // 검사를 마친 전투 연결 해제 종료
    } // 테스트 전투 구성 종료
} // 실제 유물 전투 검사 종료
