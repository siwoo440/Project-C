using System; // 독립 검사 전투 정리
using System.Collections; // 실제 전투 플레이 모드 대기
using System.Linq; // 생존 적과 전투 대상 및 실제 상태 결과 조회
using NUnit.Framework; // 실제 피해와 회복 및 난수 범위 확인
using UnityEngine; // 유물 원본과 실제 전투 자료 사용
using UnityEngine.TestTools; // 실제 전투 검사 모드 전환
using Object = UnityEngine.Object; // 테스트용 유니티 원본 정리
using Random = UnityEngine.Random; // 실제 적용점의 전역 무작위 상태 확인

public sealed class WinterfallDesertStatusRelicTests // 사막의 중독과 회복 및 화상 이전 검사
{ // 실제 전투 상태 세 유물 검사 시작
    [UnityTest] // 아군 중독의 실피해와 회복 비율의 기존 반올림 확인
    public IEnumerator CactusSapChangesOnlyAlliedPoisonAndActualAlliedHealing() // 실제 중독 발생자와 살아 있는 아군 회복 확인
    { // 선인장 수액의 중독과 회복 비교 시작
        yield return new EnterPlayMode(); // 실제 유물 보유와 피해 및 회복 런타임 시작
        Acquire("DriedCactusSap"); // 실제 선인장 수액을 현재 탐사 보유 목록에 등록
        using (var trial = new Trial()) // 두 출처와 생존 아군 회복의 원래 전투
        { // 같은 원본 피해와 회복의 실제 결과 비교 시작
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 실제 중독을 받을 정상 전투 시작
            var actor = trial.Allies[0]; // 충돌과 중독을 적용할 실제 아군 출처
            var alliedPoison = trial.Enemies[0]; // 아군 중독에서 피해 보너스를 받을 적
            var environmental = trial.Enemies[1]; // 출처 없는 중독의 기존 피해 대조
            var hostileSource = trial.Enemies[2]; // 적이 적용한 독 상태 비교 대상
            var mixed = trial.Enemies[3]; // 출처가 섞인 하나의 실제 활성 중독
            alliedPoison.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 1, actor); // 실제 아군에게서 받은 일반 중독
            environmental.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 1); // 출처 없는 환경 중독 준비
            hostileSource.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 1, environmental); // 실제 적 출처의 독 상태 준비
            mixed.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 1, actor); // 합산 상태의 처음 실제 아군 중독
            mixed.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 1); // 같은 상태를 환경이 갱신하더라도 남은 아군 기여
            Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 실제 적 턴 시작의 정상 중독 피해 처리
            Assert.AreEqual(196, alliedPoison.CurrentHealth); // 아군이 만든 중독의 기존 세 점과 가산 한 점 확인
            Assert.AreEqual(197, environmental.CurrentHealth); // 환경 독의 원래 세 점 피해 확인
            Assert.AreEqual(197, hostileSource.CurrentHealth); // 적이 만든 독의 원래 세 점 피해 확인
            Assert.AreEqual(196, mixed.CurrentHealth); // 같은 활성 중독에 실제 아군 기여가 남았을 때 가산 확인
            Assert.AreEqual(2, alliedPoison.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 피해 가산과 무관한 원래 독 수치 감소 확인
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 아군 회복을 받을 실제 아군 턴 시작
            actor.ApplyPersistentHealth(50); // 저장 체력 초기화는 원래 수치 그대로 준비
            Assert.AreEqual(0, actor.RestoreHealth(1)); // 원래 정수 내림을 따른 실제 회복량 영 경계 확인
            Assert.AreEqual(19, actor.RestoreHealth(20)); // 살아 있는 아군의 수신 회복 이십에서 오 퍼센트 감소 확인
            environmental.TakeDamage(30, BattleDamageType.None); // 적의 원래 이십 회복량을 검사할 충분한 체력 손실 준비
            Assert.AreEqual(20, environmental.RestoreHealth(20)); // 적의 기존 회복에는 선인장 감소가 없음 확인
        } // 같은 원본 피해와 회복의 실제 결과 비교 종료
        yield return new ExitPlayMode(); // 실제 선인장 상태 검사 종료
    } // 선인장 수액의 중독과 회복 비교 종료

    [UnityTest] // 실제 정규화 방어 하락 및 두 확률 경계와 확률 비소모 검증
    public IEnumerator MineBlackSandRollsOnlyWhenActualAlliedPhysicalDefenseFalls() // 물리 방어 영점과 면역 및 재적용 분리 확인
    { // 검은 모래의 실제 물리 방어와 확률 검사 시작
        yield return new EnterPlayMode(); // 실제 체력과 상태 면역 및 전역 난수 사용 환경 진입
        Acquire("MineBlackSand"); // 실제 폐광 검은 모래 원본을 현재 보유함에 추가
        using (var trial = new Trial()) // 각 확률 조건을 비교할 실제 여러 적
        { // 요건에 따른 한 번 판정과 방어값 확인 시작
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 원래 아군에게서 적 약화를 부여할 실제 전투 시작
            var actor = trial.Allies[0]; // 정상 아군 부여자
            var first = trial.Enemies[0]; // 실제 물리 방어가 내려갈 첫 적
            int defense = first.PhysicalDefense; // 현재 정상 방어 값 보관
            Random.InitState(SeedFor(19)); // 이십 퍼센트의 실제 마지막 성공 난수 설정
            first.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 4, 2, 1, actor); // 원래 방어를 실제로 내리는 정상 아군 약화
            Assert.AreEqual(defense - 4, first.PhysicalDefense); // 현재 실제 정규화 물리 방어 네 점 감소 확인
            Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 실제 성공 부여의 중독 한 점 확인
            Assert.AreEqual(2, first.GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 실제 상태의 원래 두 적 턴 확인
            var same = Random.state; // 이미 정규화 방어가 감소하지 않는 비교 추첨 진행 상태
            first.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 4, 2, 1, actor); // 같은 최대 중첩에서 수치가 유지되는 기존 갱신
            Assert.AreEqual(same, Random.state); // 물리 방어값이 같을 때 추가 난수 소비 없음 확인
            var charge = trial.Enemies[1]; // 같은 물리 방어를 내려가는 돌진의 별도 상태
            Random.InitState(SeedFor(20)); // 이십 퍼센트 첫 실패 난수 설정
            charge.ApplyStatusEffect(BattleStatusEffectType.ChargeDefenseDown, 2, 1, 1, actor); // 실제 돌진 약화의 물리 방어 감소 확인
            Assert.AreEqual(defense - 2, charge.PhysicalDefense); // 별도 상태여도 원래 물리 방어값이 실제 내려감 확인
            Assert.IsNull(charge.GetStatusEffect(BattleStatusEffectType.Poison)); // 경계 밖 실제 추첨에서 중독 없음 확인
            var zeroDefense = trial.Enemies[2]; // 물리 방어 감소 하한의 실제 대상
            same = Random.state; // 환경 및 영점 상태 이전의 실제 난수
            zeroDefense.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 100, 2, 1); // 환경 출처로 먼저 방어를 실제 영으로 낮추기
            zeroDefense.ApplyStatusEffect(BattleStatusEffectType.ChargeDefenseDown, 4, 1, 1, actor); // 이미 영인 물리 방어에는 실질 변화가 없는 아군 약화
            Assert.AreEqual(same, Random.state); // 환경과 영 하한의 실제 부적격에서 난수 비소모 확인
            var immune = trial.Enemies[3]; // 일반 약화 면역에 막히는 비교 적
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 상태 면역의 기존 유효 기간 준비
            immune.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 4, 2, 1, actor); // 실제 면역에 막혀 일반 방어도를 유지하는 약화
            Assert.AreEqual(same, Random.state); // 기존 상태 면역에서 추가 유물 판정 없음 확인
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Poison)); // 막힌 원래 약화의 추가 독 출처 없음 확인
        } // 요건에 따른 한 번 판정과 방어값 확인 종료
        yield return new ExitPlayMode(); // 실제 물리 방어 상태 검사 환경 종료
    } // 검은 모래의 실제 물리 방어와 확률 검사 종료

    [UnityTest] // 실제 화상 사망과 살아 있는 다른 적 및 보스 전환 제외 검사
    public IEnumerator AshPouchCopiesTheDyingActiveBurnAcrossSeparateDeathsOnly() // 감소한 실제 화상량과 터미널 추첨 제외 확인
    { // 재주머니의 실제 사망 상태 검사 시작
        yield return new EnterPlayMode(); // 상태 적용과 사망 해결의 실제 전투 모드 진입
        Acquire("AshstormAshPouch"); // 실제 재폭풍 재주머니 원본을 탐사에 획득
        using (var trial = new Trial()) // 원래 화상량과 두 번의 연속 적 전투불능을 볼 전투
        { // 실제 사망 시 다른 적 한 명에게 이전 확인 시작
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 실제 사망 및 원본 효과를 구동할 아군 턴 시작
            var actor = trial.Allies[0]; // 기존 실제 일반 피해를 적용할 아군
            trial.Enemies[3].TakeDamage(1000, BattleDamageType.None, actor); // 화상이 없는 사망 비교 및 생존 후보 제한
            var first = trial.Enemies[0]; // 실제 원래 화상이 있었던 적
            first.ApplyStatusEffect(BattleStatusEffectType.Burn, 4, 3, 1, actor); // 사망 전에 줄어들 비교 초기 화상 수치 준비
            first.GetStatusEffect(BattleStatusEffectType.Burn).DecayBurn(); // 남아 있는 실제 화상 수치 세 점으로 변경
            first.TakeDamage(1000, BattleDamageType.None, actor); // 일반 공격 결과로 실제 화상 적 전투불능
            var others = trial.Enemies.Skip(1).Take(2).ToArray(); // 실제 이후 화상을 받을 수 있던 두 생존 적
            var recipient = others.Single(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Burn) != null); // 둘 중 오직 한 명만 실제 화상 부여 확인
            var pending = recipient.GetStatusEffect(BattleStatusEffectType.Burn); // 다른 적으로 넘어간 실제 상태 참조
            Assert.AreEqual(3, pending.EffectiveValue); // 초기 네 점 대신 쓰러질 때 남은 세 점 이전 확인
            Assert.AreEqual(2, pending.RemainingTurns); // 기존 원본과 독립인 두 새로운 적 턴의 화상 확인
            recipient.TakeDamage(1000, BattleDamageType.None, actor); // 다음 별도의 실제 사망에서 연쇄 이전 분기
            var last = others.Single(enemy => !enemy.IsDead); // 선행 사망이 이어진 뒤 남은 마지막 적
            Assert.AreEqual(3, last.GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 별도 실제 사망에서 같은 화상으로 다음 생존 적에게 이전
            var before = Random.state; // 다른 생존 후보가 없는 마지막 사망의 기존 난수
            last.TakeDamage(1000, BattleDamageType.None, actor); // 마지막 화상 적에게 실제 치명타 적용
            Assert.AreEqual(before, Random.state); // 더 이상 후보가 없으면 실제 난수 소비 없음 확인
            Assert.AreEqual(BattleResult.Victory, trial.Turn.Result); // 실제 원래 최종 승리 유지 확인
        } // 실제 사망 시 다른 적 한 명에게 이전 확인 종료
        using (var trial = new Trial(BattleType.Boss)) // 실제 예약 전환을 가진 우두머리 전투
        { // 보스 단계 변경의 이전 중단 검사 시작
            Assert.IsTrue(trial.Turn.StartBattle(0)); // 활성 보스 전투와 실제 아군 턴 시작
            trial.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.Burn, 3, 2, 1, trial.Allies[0]); // 전환될 보스의 실제 화상 상태 준비
            trial.Turn.QueueBossPhaseTransition(); // 아직 처리하지 않은 실제 보스 단계 전환 예약
            var before = Random.state; // 전환 전 남겨야 할 실제 무작위 상태
            trial.Enemies[0].TakeDamage(1000, BattleDamageType.None, trial.Allies[0]); // 예약 전환 중 기존 피해 해결과 반응 호출
            Assert.AreEqual(before, Random.state); // 전환 중 이전 후보가 있어도 추첨 생략 확인
            Assert.IsFalse(trial.Enemies.Skip(1).Any(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Burn) != null)); // 남은 원래 적들의 화상 상태 불변 확인
        } // 보스 단계 변경의 이전 중단 검사 종료
        yield return new ExitPlayMode(); // 화상 이전의 실제 전투 검사 종료
    } // 재주머니의 실제 사망 상태 검사 종료

    private static int SeedFor(int expected) // 실제 원래 판정 경계의 전역 시드 조회
    { // 검사에서 필요한 실제 경계 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 결정적으로 찾을 유니티 시드 범위
        { // 실제 난수 한 번 확인 시작
            Random.InitState(seed); // 현재 검사 시드의 원래 무작위 상태 설정
            if (Random.Range(0, 100) == expected) // 실제 첫 확률이 지정한 값인지 확인
            { // 원래 실제 성공 시드 저장 시작
                return seed; // 실제 코드가 같은 난수를 사용할 검사 시드 반환
            } // 원래 실제 성공 시드 저장 종료
        } // 실제 난수 한 번 확인 종료
        throw new InvalidOperationException("실제 확률 검사용 시드 없음"); // 경계값을 재현하지 못한 명시 실패
    } // 검사에서 필요한 실제 경계 검색 종료

    private static void Acquire(string key) // 실제 출시 리소스 유물 획득
    { // 실제 현재 탐사 보유 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 실제 유물 원본 리소스 조회
        Assert.IsNotNull(relic, key); // 기획에 연결한 유효 원본 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 유물 획득 API로 현재 보유함에 추가
    } // 실제 현재 탐사 보유 종료

    private sealed class Trial : IDisposable // 실제 전투 상태와 턴을 구동할 독립 참가자 구성
    { // 검사 전투 원본 시작
        private readonly CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 실제 검사 아군 출전 자료
        private readonly EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 같은 방어 값을 가진 검사 적 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 기존 실제 전투 덱의 단일 카드 원본
        private readonly DeckData deckData; // 실제 전투 덱 원본
        private readonly BattleStatusEffectController statuses; // 실제 시작 및 종료의 상태 효과 처리기
        private readonly Random.State previousRandom = Random.state; // 검사 전의 실제 전역 난수 상태 보관
        internal readonly BattleUnitRuntime[] Allies; // 생존 아군의 실제 전투 명단
        internal readonly BattleUnitRuntime[] Enemies; // 현재 전투의 실제 생존 적과 전투불능 비교 명단
        internal readonly BattleTurnRuntime Turn; // 실제 전투와 페이즈 조우 관리자

        internal Trial(BattleType battleType = BattleType.Normal) // 기존 전투 규칙의 아군과 적 및 상태 구동 준비
        { // 실제 참가자와 현재 턴 초기화 시작
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", character); // 정확한 아군 회복 수치 비교용 체력 원본
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":20}", enemyData); // 실제 중독과 물리 방어 수치 비교용 적 원본
            Allies = new[] { BattleUnitRuntime.CreateAlly(character) }; // 정상 회복을 받을 실제 출전 아군
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemyData)).ToArray(); // 실제 한 전투의 네 독립 적 출전
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, character) }); // 정상 아군 카드 소유자가 연결된 원래 전투 덱 생성
            var deck = BattleDeckRuntime.Create(deckData, Allies, 3, 272); // 검사 전투의 원래 손패와 덱 구성
            Turn = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), Allies, Enemies, 0, battleType); // 조우 등급과 원래 페이즈의 전투 관리자 연결
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 중독과 이후 상태 기간을 처리할 원래 구독
        } // 실제 참가자와 현재 턴 초기화 종료

        public void Dispose() // 사용한 검사 전투 및 임시 원본 정리
        { // 전투 검사 정리 시작
            statuses.Dispose(); // 실제 상태 지속과 피해 연결 해제
            Turn.Dispose(); // 실제 전투의 모든 상태 및 사망 연결 해제
            Object.Destroy(deckData); // 왼쪽 손패의 임시 덱 원본 정리
            Object.Destroy(card); // 이전 전투와 관계없는 임시 카드 원본 제거
            Object.Destroy(character); // 독립 출전 아군 원본 제거
            Object.Destroy(enemyData); // 독립 출전 적 원본 제거
            Random.state = previousRandom; // 검사 전의 전역 원래 난수 복원
        } // 전투 검사 정리 종료
    } // 검사 전투 원본 종료
} // 실제 전투 상태 세 유물 검사 종료
