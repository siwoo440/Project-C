using System; // 실제 검사 전투 원본 정리
using System.Collections; // 플레이 모드와 원래 턴 진행 대기
using System.Linq; // 실제 아군과 적 배치 준비
using NUnit.Framework; // 실제 상태와 피해 및 회복 수치 확인
using UnityEngine; // 유물 원본과 유닛 배치 검사
using UnityEngine.TestTools; // 원래 전투 흐름의 플레이 모드 검사
using Object = UnityEngine.Object; // 검사 원본의 유니티 메모리 해제

public sealed class WinterfallMirrorDukePeriodRelicTests // 명령서와 잔뿌리 및 샹들리에의 실제 턴 검사
{ // 세 턴 유물의 실전 회귀 검사 시작
    [UnityTest] // 두 완료 아군 턴과 바로 뒤 첫 적 행동의 실제 시간 관계 확인
    public IEnumerator LordOrderWaitsForTwoCompletedAlliedTurnsEvenAfterAmbushOrDispel() // 선제와 조기 해제 뒤에도 따로 적용되는 방어 대가 확인
    { // 명령서의 실제 두 진영 턴 비교 시작
        yield return new EnterPlayMode(); // 실제 상태 기간과 탐사 유물의 전투 환경 연결
        Acquire("LordsFinalOrder"); // 게임에서 사용하는 실제 명령서 획득
        foreach (bool ambush in new[] { false, true }) // 정상 전투와 선제 전투를 실제로 각각 시작
        { // 공격 강화 만료와 물리 방어 대가 검사 시작
            using var battle = new Trial(); // 실제 상태 지속 처리기로 연결할 독립 전투
            var first = battle.Allies[0]; // 조기 해제와 물리 방어 대가를 함께 확인할 아군
            var second = battle.Allies[1]; // 공격 강화가 끝까지 유지되는 다른 생존 아군
            var fallen = battle.Allies[2]; // 개막부터 전투불능인 비교 아군
            fallen.ApplyPersistentHealth(0); // 강화와 약화 양쪽에서 대상 제외 설정
            int defense = first.PhysicalDefense; // 다른 약화가 없는 원래 물리 방어 수치
            int magical = first.MagicalResistance; // 건드리지 않아야 할 실제 기본 마법 저항
            var enemy = battle.Enemies[0]; // 실제 적 직접 피해 예고의 발생자
            int basePreview = first.PreviewDamage(100, BattleDamageType.Physical, enemy, enemyDirectAttack: true).FinalDamage; // 강화 및 약화가 없을 때의 기준 물리 피해
            Assert.IsTrue(battle.Turn.StartBattle(0, ambush)); // 실제 선제 여부를 반영한 개막
            if (ambush) // 첫 아군 전의 적 선제 차례 확인
            { // 아직 어느 강화와 약화도 안 온 상태 비교 시작
                Assert.Zero(first.AttackPowerBonus); // 아군 차례가 오기 전 명령 공격 미발동 확인
                Assert.AreEqual(defense, first.PhysicalDefense); // 적 선제에는 방어 대가 미적용 확인
                Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 선제 뒤 첫 실제 아군 턴으로 진입
            } // 아직 어느 강화와 약화도 안 온 상태 비교 종료
            Assert.AreEqual(3, first.AttackPowerBonus); // 첫 아군 턴에 실제 공격력 세 점 확인
            Assert.AreEqual(2, second.GetStatusEffect(BattleStatusEffectType.LordFinalOrderAttack).RemainingTurns); // 끝까지 유지할 두 아군 턴의 초기 기간 확인
            Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.LordFinalOrderAttack)); // 전투불능 아군의 강화 제외 확인
            first.RemoveStatusEffect(BattleStatusEffectType.LordFinalOrderAttack); // 독립 시점 확인을 위해 첫 아군의 공격 강화 조기 제거
            Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 첫 번째 아군 턴 뒤의 첫 일반 적 턴 진입
            Assert.AreEqual(defense, first.PhysicalDefense); // 아직 아군 한 턴만 완료됐으므로 대가 없음 확인
            Assert.AreEqual(1, second.GetStatusEffect(BattleStatusEffectType.LordFinalOrderAttack).RemainingTurns); // 첫 아군 턴 종료부터 자연 차감 확인
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 두 번째 실제 아군 턴 시작
            Assert.AreEqual(3, second.AttackPowerBonus); // 두 번째 아군 턴에도 다른 아군의 원래 공격력 증가 유효 확인
            Assert.Zero(first.AttackPowerBonus); // 조기 해제된 강화가 임의로 다시 생기지 않음 확인
            Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 두 아군 턴 종료 직후 실제 적 차례 시작
            Assert.Zero(second.AttackPowerBonus); // 두 아군 턴을 마친 강화의 정확한 만료 확인
            Assert.AreEqual(defense - 2, first.PhysicalDefense); // 조기 강화 제거와 무관한 다음 적 턴의 물리 방어 하락 확인
            Assert.AreEqual(magical, first.MagicalResistance); // 물리 방어 대가에서 마법 저항 불변 확인
            Assert.Greater(first.PreviewDamage(100, BattleDamageType.Physical, enemy, enemyDirectAttack: true).FinalDamage, basePreview); // 실제 적 공격의 방어 예고에도 감소 적용 확인
            Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.LordFinalOrderWeakenedDefense)); // 전투불능 아군은 방어 약화에서도 제외
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 방어가 감소한 실제 적 턴 종료
            Assert.AreEqual(defense, first.PhysicalDefense); // 한 적 턴 직후 원래 물리 방어 복원 확인
            Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 이후 다른 실제 적 턴으로 진행
            Assert.AreEqual(defense, first.PhysicalDefense); // 같은 전투의 반복 방어 대가 차단 확인
        } // 공격 강화 만료와 물리 방어 대가 검사 종료
        yield return new ExitPlayMode(); // 실제 명령서 전투 검사 종료
    } // 명령서의 실제 두 진영 턴 비교 종료

    [UnityTest] // 살아 있고 중독이 남은 적의 실제 수에 따른 전체 회복 확인
    public IEnumerator BanquetHeartCountsLivingPoisonedEnemiesRatherThanStacks() // 중독 중첩과 전투불능 및 최대 체력의 회복 범위 확인
    { // 잔뿌리 실제 회복 검사 시작
        yield return new EnterPlayMode(); // 실제 중독과 진영 처리 환경 진입
        Acquire("BanquetHeartRoot"); // 원본 만찬 심장 잔뿌리 획득
        using (var battle = new Trial()) // 다양한 회복 상한을 가진 원래 파티 구성
        { // 살아 있는 중독 적 인원과 아군별 실제 회복 확인 시작
            var first = battle.Enemies[0]; // 여러 중첩을 갖는 실제 중독 적
            var second = battle.Enemies[1]; // 별개로 하나만 세어야 할 중독 적
            var expired = battle.Enemies[2]; // 목록에 남아도 만료 처리된 비교 적
            var fallenEnemy = battle.Enemies[3]; // 중독 뒤 쓰러진 원래 적
            foreach (var enemy in battle.Enemies) // 실제 적 전체의 원래 중독 상태 준비
            { // 적별 중독 기간 설정 시작
                enemy.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, enemy == expired ? 1 : 5, 4); // 실제 수명 비교용 각 적의 중독 상태
            } // 적별 중독 기간 설정 종료
            first.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 5, 4); // 첫 적의 추가 중첩을 인원에 더하지 않는지 검사
            expired.GetStatusEffect(BattleStatusEffectType.Poison).AdvanceDuration(); // 아직 목록에 있을 수 있는 만료 적 상태 준비
            fallenEnemy.TakeDamage(1000, BattleDamageType.None); // 중독 적 가운데 전투불능 비교 대상
            battle.Allies[0].ApplyPersistentHealth(90); // 두 중독 적의 회복 네 점을 전부 받을 아군
            battle.Allies[1].ApplyPersistentHealth(99); // 정상 최대 체력 상한 확인용 아군
            battle.Allies[2].ApplyPersistentHealth(0); // 일반 회복으로 부활하지 않아야 할 아군
            Assert.IsTrue(battle.Turn.StartBattle(0)); // 첫 실제 아군 턴의 회복 발동
            Assert.AreEqual(94, battle.Allies[0].CurrentHealth); // 중첩 수 대신 실제 중독 적 둘의 회복 네 점 확인
            Assert.AreEqual(100, battle.Allies[1].CurrentHealth); // 같은 회복 효과의 정상 최대 체력 제한 확인
            Assert.AreEqual(0, battle.Allies[2].CurrentHealth); // 회복으로 전투불능 부활 미발생 확인
            second.RemoveStatusEffect(BattleStatusEffectType.Poison); // 다음 실제 시작에서 중독 적 한 마리 감소
            Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 적 차례 진입에서 추가 아군 회복 없음 확인
            Assert.AreEqual(94, battle.Allies[0].CurrentHealth); // 적 차례에는 동일한 중독 개수 회복 없음 확인
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 다음 아군 턴의 실제 회복 처리
            Assert.AreEqual(96, battle.Allies[0].CurrentHealth); // 한 마리 중독에 다음 회복 두 점만 추가
        } // 살아 있는 중독 적 인원과 아군별 실제 회복 확인 종료
        yield return new ExitPlayMode(); // 원본 잔뿌리 회귀 검사 종료
    } // 잔뿌리 실제 회복 검사 종료

    [UnityTest] // 실제 세 번째 아군 턴의 광역 피해와 그 턴 명중 감소만 적용
    public IEnumerator ChandelierPulsesOnThirdAlliedTurnAndStopsWhenVictoryIsSettled() // 살아 있는 대상의 일반 피해와 그 아군 턴 뒤 약화 제거 확인
    { // 실제 광역 유물 주기 검사 시작
        yield return new EnterPlayMode(); // 실제 아군 및 적 상태의 전투 환경 시작
        Acquire("BlackChandelierCore"); // 원본 검은 샹들리에 핵 획득
        using (var battle = new Trial()) // 주기와 면역을 확인할 실제 전투
        { // 일반 피해와 정확한 약화 기간 비교 시작
            battle.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 광역에서 제외할 전투불능 적 준비
            battle.Allies[1].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 6, 1); // 원래 명중률 약화 저항 확인용 생존 아군
            var first = battle.Allies[0]; // 실제 명중률 약화를 받을 아군
            var enemy = battle.Enemies[0]; // 실제 피해와 피격 예고 비교용 적
            int hit = BattleHitChance.GetPercent(first, enemy); // 유물 발동 전 일반 원래 명중률
            Assert.IsTrue(battle.Turn.StartBattle(0, true)); // 적 선제와 관계없이 아군 주기를 세는 실제 전투
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 선제 뒤 첫 실제 아군 턴 시작
            Advance(battle); // 두 번째 실제 아군 턴 시작
            Assert.AreEqual(200, enemy.CurrentHealth); // 두 번째까지 정상 상태의 체력 보존 확인
            Advance(battle); // 세 번째 실제 아군 턴의 최초 광역 효과
            Assert.AreEqual(190, enemy.CurrentHealth); // 각 생존 적의 일반 피해 열 점 확인
            Assert.AreEqual(190, battle.Enemies[1].CurrentHealth); // 같이 살아 있는 다른 적도 같은 피해 확인
            Assert.Zero(battle.Enemies[3].CurrentHealth); // 이미 쓰러진 적 유지 확인
            Assert.AreEqual(hit - 5, BattleHitChance.GetPercent(first, enemy)); // 실제 공격 명중 계산에서 오 퍼센트포인트 감소 확인
            Assert.IsNull(battle.Allies[1].GetStatusEffect(BattleStatusEffectType.Disorientation)); // 기존 약화 면역으로 해당 아군의 정상 상태 보호 확인
            Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 샹들리에가 발동한 아군 턴 종료
            Assert.AreEqual(hit, BattleHitChance.GetPercent(first, enemy)); // 뒤 적 턴에는 실제 아군 명중률 복원 확인
        } // 일반 피해와 정확한 약화 기간 비교 종료
        using (var battle = new Trial()) // 세 번째 광역으로 마지막 적이 쓰러지는 별도 실제 전투
        { // 마지막 적의 전투 결과 처리 확인 시작
            foreach (var enemy in battle.Enemies.Skip(1)) // 첫 상대를 제외한 실제 배치 적 준비
            { // 마지막 상대를 남기는 실제 피해 시작
                enemy.TakeDamage(1000, BattleDamageType.None); // 실제 사망 경로로 다른 적 제거
            } // 마지막 상대를 남기는 실제 피해 종료
            battle.Enemies[0].TakeDamage(195, BattleDamageType.None); // 현재 세 번째 광역 피해에 쓰러질 마지막 적
            Assert.IsTrue(battle.Turn.StartBattle(0)); // 일반 전투의 첫 아군 턴 시작
            Advance(battle); // 두 번째 실제 아군 턴 시작
            Advance(battle); // 유물 피해로 승리가 확정되는 세 번째 차례
            Assert.AreEqual(BattleResult.Victory, battle.Turn.Result); // 실제 마지막 생존 적 제거에 따른 승리 확인
            Assert.IsNull(battle.Allies[0].GetStatusEffect(BattleStatusEffectType.Disorientation)); // 끝난 전투의 아군에게 명중 약화 미부여 확인
        } // 마지막 적의 전투 결과 처리 확인 종료
        yield return new ExitPlayMode(); // 실제 샹들리에 회귀 검사 종료
    } // 실제 광역 유물 주기 검사 종료

    private static void Advance(Trial battle) // 다음 실제 아군 턴까지 진행
    { // 원래 진영 종료와 시작 확인
        Assert.IsTrue(battle.Turn.EndPlayerTurn() && battle.Turn.CompleteEnemyTurn()); // 원본 전투의 실제 진영 전환
    } // 원래 진영 종료와 시작 확인 종료

    private static void Acquire(string key) // 실제 보유 유물 원본 획득
    { // 전투에서 사용할 원본 리소스 연결 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 등록된 실제 전투 유물 조회
        Assert.IsNotNull(relic, key); // 게임용 원본 파일 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 현재 탐사 유물 보관함에 원본 등록
    } // 전투에서 사용할 원본 리소스 연결 종료

    private sealed class Trial : IDisposable // 실제 참가자와 턴 지속 처리기를 사용한 독립 전투
    { // 검사 전투 구성 시작
        private readonly CharacterData[] characters = Enumerable.Range(0, 3).Select(_ => ScriptableObject.CreateInstance<CharacterData>()).ToArray(); // 실제로 출전하는 독립 아군 원본
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 실제 적 체력 확인용 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 기존 전투 덱 구성에 필요한 단일 카드 원본
        private readonly DeckData deckData; // 전투 자료의 덱 원본
        private readonly BattleStatusEffectController statuses; // 처음 아군 턴의 강화와 다음 적 상태 지속을 계산하는 실제 처리기
        internal readonly BattleUnitRuntime[] Allies; // 현재 전투의 실제 아군
        internal readonly BattleUnitRuntime[] Enemies; // 현재 전투의 실제 적
        internal readonly BattleTurnRuntime Turn; // 원래 진영 턴 및 결과 처리 관리자

        internal Trial() // 실제 초기 전투 배치와 턴 관리자 연결
        { // 전투 자료 초기화 시작
            foreach (var character in characters) // 같은 기본 방어를 가진 아군 원본 순회
            { // 실제 조회되는 기본 방어와 체력 설정 시작
                JsonUtility.FromJsonOverwrite("{\"maxHealth\":100,\"physicalDefense\":10}", character); // 실제 피해 예고 비교를 위한 기본 체력과 물리 방어 수치
            } // 실제 조회되는 기본 방어와 체력 설정 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200}", enemy); // 주기 피해 전에 비교하기 충분한 적 체력 설정
            Allies = characters.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 원래 출전 아군 런타임 구성
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemy)).ToArray(); // 실제 살아 있는 대상과 전투불능을 비교할 적 넷
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, characters[0]) }); // 원래 출전 카드 소유 관계가 유효한 덱 생성
            var deck = BattleDeckRuntime.Create(deckData, Allies, 3, 268); // 반복 가능한 원래 전투 덱 준비
            Turn = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), Allies, Enemies, 0, BattleType.Normal); // 기존 실제 피격 및 진영 단계 관리자 연결
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 지속 효과와 약화 만료 처리 연결
        } // 전투 자료 초기화 종료

        public void Dispose() // 실제 검사 원본 정리
        { // 검사 환경 종료 시작
            statuses.Dispose(); // 실제 상태와 진영 전환 구독 해제
            Turn.Dispose(); // 실제 전투의 참가자 이벤트 연결 해제
            Object.Destroy(deckData); // 임시 덱 원본 제거
            Object.Destroy(card); // 임시 카드 원본 제거
            Object.Destroy(enemy); // 임시 적 원본 제거
            foreach (var character in characters) // 현재 검사 아군의 독립 원본 순회
            { // 검사 아군 정리 시작
                Object.Destroy(character); // 기존 실제 캐릭터와 별개인 임시 원본 제거
            } // 검사 아군 정리 종료
        } // 검사 환경 종료 종료
    } // 검사 전투 구성 종료
} // 세 턴 유물의 실전 회귀 검사 종료
