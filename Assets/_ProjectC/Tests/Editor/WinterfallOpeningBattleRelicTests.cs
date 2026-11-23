using System.Collections; // 플레이 모드 전환 사용
using System.Linq; // 살아 있는 약화 대상 조회
using NUnit.Framework; // 전투 결과 비교
using UnityEngine; // 임시 유물과 전투 원본 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용

public sealed class WinterfallOpeningBattleRelicTests // 첫 전투 속도 및 공격 약화 검사
{ // 검사 시작
    [UnityTest] // 실제 유물 보관함과 전투 경계 검사
    public IEnumerator OpeningRelicsLastThroughOnlyTheFirstEnemyTurn() // 정상 시작과 적 선공의 첫 턴 확인
    { // 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 보관함 활성화
        var inventory = RelicRunManager.EnsureInstance(); // 현재 탐사 유물 관리자 조회
        var badge = MakeRelic("DAY238_TEST_BADGE", RelicEffectType.OpeningAllySpeed, BattleStatusEffectType.ActionSpeedUp, 1); // 속도 한 점의 검사 유물
        var drone = MakeRelic("DAY238_TEST_DRONE", RelicEffectType.OpeningDroneWeakness, BattleStatusEffectType.AttackPowerDown, 2); // 공격 두 점 약화의 검사 유물
        try // 검사 유물과 원본 정리 보장
        { // 보유 효과 검사 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(badge)); // 바람개비 유물 보유 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(drone)); // 드론 유물 보유 확인
            VerifyOpeningTurn(false); // 일반 전투의 전투 시작과 첫 적 턴 확인
            VerifyOpeningTurn(true); // 선공 전투의 같은 라운드 재사용 확인
        } // 보유 효과 검사 종료
        finally // 유물 보관함 원상 복구
        { // 유물 정리 시작
            inventory.TryRemove(badge.RelicId); // 검사 속도 유물 제거
            inventory.TryRemove(drone.RelicId); // 검사 약화 유물 제거
            Object.Destroy(badge); // 임시 배지 원본 정리
            Object.Destroy(drone); // 임시 드론 원본 정리
        } // 유물 정리 종료
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 플레이 모드 검사 종료

    private static RelicData MakeRelic(string id, RelicEffectType effect, BattleStatusEffectType status, int value) // 데이터가 들어 있는 임시 유물 구성
    { // 유물 원본 구성 시작
        var relic = ScriptableObject.CreateInstance<RelicData>(); // 유물 직렬화 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"relicId\":\"{id}\",\"triggerType\":{(int)RelicTriggerType.BeforeBattleActions},\"effectType\":{(int)effect},\"effectValue\":{value},\"statusEffectType\":{(int)status},\"statusDuration\":1,\"statusMaximumStacks\":1}}", relic); // 실제 유물 시점과 값 및 지속 시간 설정
        return relic; // 전투 보관함에서 사용할 원본 반환
    } // 유물 원본 구성 종료

    private static void VerifyOpeningTurn(bool enemyFirst) // 첫 턴 진영에 관계없는 두 유물 확인
    { // 격리 전투 구성 시작
        var characters = Enumerable.Range(0, 3).Select(_ => ScriptableObject.CreateInstance<CharacterData>()).ToArray(); // 서로 다른 아군 원본 세 개 생성
        var enemy = ScriptableObject.CreateInstance<EnemyData>(); // 고정 피해 적 원본 생성
        var card = ScriptableObject.CreateInstance<CardData>(); // 정상 덱 생성을 위한 검사 카드
        var source = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, characters[0]) }); // 전투 덱 원본 생성
        try // 전투 원본 정리 보장
        { // 격리 전투 시작
            JsonUtility.FromJsonOverwrite("{\"basicAttackPower\":10,\"minimumActionSpeed\":4,\"maximumActionSpeed\":4}", enemy); // 피해와 적 행동 순서 고정
            var allies = characters.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 서로 다른 아군 세 명 생성
            var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy), BattleUnitRuntime.CreateEnemy(enemy), BattleUnitRuntime.CreateEnemy(enemy) }; // 적 세 명 생성
            allies[2].TakeDamage(1000, BattleDamageType.None); // 배지의 전투불능 아군 준비
            enemies[2].TakeDamage(1000, BattleDamageType.None); // 드론의 전투불능 적 준비
            var deck = BattleDeckRuntime.Create(source, allies, 3, 238); // 실제 전투 덱 구성
            var points = new BattleActionPointRuntime(6); // 공용 행동력 준비
            using var turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Normal); // 실제 전투 턴 연결
            using var statuses = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 진영별 효과 만료 연결
            using var actions = new BattleEnemyActionRuntime(enemies, allies, turns); // 피해 예고와 실제 적 공격 연결
            void PrepareOpeningIntents() // 전투 씬과 동일한 시작 예고 검사
            { // 전투 시작 예고 준비
                actions.PrepareActions(); // 첫 진영 행동 전 적 공격 예고 생성
                var announcedDamage = actions.PlannedActions.Select(action => action.PreviewDamage().RawDamage).OrderBy(damage => damage); // 최초 예고의 원본 피해 수집
                CollectionAssert.AreEqual(new[] { 8, 10 }, announcedDamage); // 첫 예고부터 무작위 적 한 명만 약화 확인
                CollectionAssert.AreEqual(new[] { 1, 1, 0 }, allies.Select(ally => ally.ActionSpeedModifier)); // 최초 예고 시 생존 아군의 속도 증가 확인
            } // 전투 시작 예고 검사 종료
            Assert.IsTrue(turns.StartBattle(0, enemyFirst, PrepareOpeningIntents)); // 실제 전투 시작 경로의 첫 예고 준비
            Assert.AreEqual(enemyFirst ? BattleTurnPhase.EnemyTurn : BattleTurnPhase.PlayerTurn, turns.CurrentPhase); // 기존 선공 규칙 유지 확인
            CollectionAssert.AreEqual(new[] { 1, 1, 0 }, allies.Select(ally => ally.ActionSpeedModifier)); // 생존 아군의 전투 시작 속도 상태 확인
            Assert.AreEqual(2, actions.PlannedActions.Count); // 전투불능 적의 행동 제외 확인
            var weakened = actions.PlannedActions.Single(action => action.Actor.GetStatusEffect(BattleStatusEffectType.AttackPowerDown) != null); // 약화된 생존 적 한 명 확인
            var other = actions.PlannedActions.Single(action => action != weakened); // 약화되지 않은 둘째 생존 적 확인
            Assert.AreEqual(8, weakened.PreviewDamage().RawDamage); // 최초 예고에 공격 약화 두 점 반영 확인
            Assert.AreEqual(10, other.PreviewDamage().RawDamage); // 선택되지 않은 적의 피해 예고 확인
            Assert.IsNull(enemies[2].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 전투불능 적의 상태 제외 확인
            if (!enemyFirst) // 일반 시작의 첫 아군 턴 확인
            { // 일반 시작 경계 검사
                Assert.IsTrue(turns.EndPlayerTurn()); // 첫 아군 턴 종료
            } // 일반 시작 경계 검사 종료
            CollectionAssert.AreEqual(new[] { 1, 1, 0 }, allies.Select(ally => ally.ActionSpeedModifier)); // 첫 적 턴 중 속도 보존 확인
            Assert.AreEqual(8, actions.ExecuteAction(weakened).DamageResult.RawDamage); // 실제 첫 적 공격에도 약화 적용 확인
            Assert.IsTrue(turns.CompleteEnemyTurn()); // 첫 적 턴의 종료 처리
            Assert.IsTrue(allies.All(ally => ally.ActionSpeedModifier == 0)); // 첫 적 턴 종료에 속도 회수 확인
            Assert.IsNull(weakened.Actor.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 첫 적 턴 종료에 약화 만료 확인
            Assert.IsTrue(turns.EndPlayerTurn()); // 두 번째 적 턴으로 전환
            actions.PrepareActions(); // 후속 공격 예고 다시 생성
            Assert.IsTrue(allies.All(ally => ally.ActionSpeedModifier == 0)); // 후속 적 턴에서 속도 미재발동 확인
            Assert.IsTrue(actions.PlannedActions.All(action => action.PreviewDamage().RawDamage == 10)); // 후속 공격의 원래 피해량 복구 확인
        } // 격리 전투 종료
        finally // 독립 원본 정리
        { // 전투 자원 정리 시작
            Object.Destroy(source); // 임시 덱 원본 제거
            Object.Destroy(card); // 임시 카드 원본 제거
            Object.Destroy(enemy); // 임시 적 원본 제거
            foreach (var character in characters) // 생성한 아군 원본 순회
            { // 아군 원본 정리 시작
                Object.Destroy(character); // 임시 아군 원본 제거
            } // 아군 원본 정리 종료
        } // 전투 자원 정리 종료
    } // 첫 턴 검사 종료
} // 검사 종료
