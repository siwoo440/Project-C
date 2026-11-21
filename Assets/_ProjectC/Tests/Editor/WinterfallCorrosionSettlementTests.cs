using System; // 전투 도우미의 정리와 빈 결과 목록
using System.Collections; // 실제 플레이 모드 전환 대기
using System.Linq; // 정식 출전 명단과 종료 결과 조회
using System.Reflection; // 실제 스냅샷과 무방어 피해 경로 호출
using NUnit.Framework; // 침식 피해 출처와 최종 체력 검증
using UnityEngine; // 실제 유물과 전투 원본 조회
using UnityEngine.TestTools; // 실제 전투 결과 래퍼 등록

public sealed class WinterfallCorrosionSettlementTests // 균열 봉합실의 전투 결과 저장 검사
{ // 침식 회복 유물 검사 시작
    [UnityTest] // 실제 체력 피해와 결과 스냅샷 및 재시도 검사
    public IEnumerator EscapeSettlesOnlySurvivorsWithActualCorrosionDamage() // 경감된 피해, 죽음, 부활과 결과 체력 검증
    { // 도주 정산 검사 시작
        yield return new EnterPlayMode(); // 실제 전투와 결과 데이터 환경 진입
        VerifyEscapeSettlement(); // 플레이 모드 진입 뒤 실제 피해와 결과 재시도 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 도주 정산 검사 종료

    private static void VerifyEscapeSettlement() // 실제 교전의 도주 결과 확인
    { // 유물과 전투의 실제 도주 시험 시작
        var relic = LoadRelic(); // 정식 균열 봉합실 원본
        var relics = RelicRunManager.EnsureInstance(); // 실제 회차 인벤토리 조회
        relics.TryAcquire(relic); // 이번 교전의 실제 유물 보유 등록
        try // 여러 실제 피해 종류와 생존 결과 검증
        { // 실제 침식 대상 배열 검사 시작
            using var fight = new SettlementFight(6); // 여섯 가지 종료 대상 조건의 정식 출전 명단
            var exposed = fight.Allies[0]; // 일반 및 무방어의 실제 침식 피해를 모두 받는 아군
            var blocked = fight.Allies[1]; // 해당 침식은 방어도로 전량 막을 아군
            var ordinary = fight.Allies[2]; // 일반 및 마법 피해만 받을 대조 아군
            var dead = fight.Allies[3]; // 실제 침식으로 쓰러지고 부활하지 않는 아군
            var revived = fight.Allies[4]; // 실제 침식 사망 뒤 부활 씨앗으로 살아난 아군
            var capped = fight.Allies[5]; // 최대 체력까지 한 점만 부족한 실제 침식 대상
            exposed.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedUp, 20, 2, 1); // 기존 받는 회복 상승의 실제 곱 보정
            Assert.AreEqual(12, exposed.TakeDamage(12, BattleDamageType.Corrosion, fight.Enemy).AppliedDamage); // 실제 속성 침식으로 체력 감소 확인
            Assert.AreEqual(2, Unblocked(exposed, 2, BattleDamageType.Corrosion)); // 이미 기록한 같은 아군의 실제 무방어 침식 손실 확인
            blocked.GainBlock(10); // 침식 등급의 전체 피해를 흡수할 방어도
            var absorb = blocked.TakeDamage(8, BattleDamageType.Corrosion, fight.Enemy); // 방어도로 모두 막은 실제 침식 적용
            Assert.AreEqual(8, absorb.AbsorbedDamage); // 해당 침식의 전량 방어도 소모
            Assert.AreEqual(0, absorb.AppliedDamage); // 실제 체력을 잃지 않은 침식 확인
            blocked.ClearBlock(); // 별개 일반 피해의 실제 부상 조건 준비
            blocked.TakeDamage(9, BattleDamageType.None, fight.Enemy); // 나중에 부상을 남겨도 침식 대상은 아님 확인
            ordinary.TakeDamage(9, BattleDamageType.Magical, fight.Enemy); // 침식 대신 다른 실제 피해 흡수
            ordinary.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 침식 한 번을 완전히 막는 다른 보호
            Assert.IsTrue(ordinary.TakeDamage(10, BattleDamageType.Corrosion, fight.Enemy).WasDamageNullified); // 실제 체력이 사라지지 않는 침식 무효
            dead.TakeDamage(1_000, BattleDamageType.Corrosion, fight.Enemy); // 실제 침식으로 전투불능 처리
            revived.ApplyStatusEffect(BattleStatusEffectType.ResurrectionSeed, 20, 1, 1); // 실제 사망 알림 뒤 돌아올 부활 씨앗 보유
            revived.TakeDamage(1_000, BattleDamageType.Corrosion, fight.Enemy); // 실제 침식과 기존 부활 결과 확정
            Assert.IsFalse(revived.IsDead); // 전투 중 원래 씨앗이 실제 부활을 완료했는지 확인
            Assert.AreEqual(20, revived.CurrentHealth); // 기존 씨앗과 결과 회복을 별도로 확인할 초기 체력
            capped.TakeDamage(1, BattleDamageType.Corrosion, fight.Enemy); // 체력 상한에서 회복 한 점만 허용할 실제 손실
            Assert.AreEqual(86, exposed.CurrentHealth); // 전투가 계속되는 동안 기본 유물 회복 미발동
            int blockedHealth = blocked.CurrentHealth; // 전량 방어 뒤 일반 손실을 가진 대조 체력
            int ordinaryHealth = ordinary.CurrentHealth; // 침식 무효와 일반 피해를 받은 대조 체력
            Assert.IsTrue(fight.Turns.TryEscape()); // 실제 저장 대상인 전투 도주 결과 확정
            Assert.AreEqual(86, exposed.CurrentHealth); // 결과 스냅샷 전 공용 종료 신호에서 중복 회복 없음 확인
            BattleResultData saved = Capture(fight.Turns); // 실제 씬과 동일한 결과 체력 스냅샷 생성
            Assert.AreEqual(BattleResult.Escape, saved.Result); // 실제 결과 스냅샷의 도주 확인
            Assert.AreEqual(93, Health(saved, exposed)); // 기본 여섯에 기존 받는 회복 증가를 한 번 적용
            Assert.AreEqual(blockedHealth, Health(saved, blocked)); // 침식이 방어도에만 닿은 대상의 체력 보존
            Assert.AreEqual(ordinaryHealth, Health(saved, ordinary)); // 마법 피해 및 침식 무효 대상에 회복 없음 확인
            Assert.AreEqual(0, Health(saved, dead)); // 이 유물만으로 쓰러진 아군 부활 차단
            Assert.AreEqual(26, Health(saved, revived)); // 실제 씨앗이 이미 살린 아군에게 침식의 종료 회복 적용
            Assert.AreEqual(100, Health(saved, capped)); // 기본 여섯 회복의 실제 최대 체력 상한 확인
            BattleResultData retry = Capture(fight.Turns); // 첫 스냅샷이 저장되지 않은 것과 같은 두 번째 생성 요청
            Assert.AreEqual(93, Health(retry, exposed)); // 재시도에서 같은 전투 유물 미중복 발동
            Assert.AreEqual(26, Health(retry, revived)); // 다른 모든 생존 아군도 한 번만 회복 확인
        } // 실제 침식 대상 배열 검사 종료
        finally // 실제 유물 장착 상태 정리
        { // 테스트 회차 유물 정리 시작
            relics.TryRemove(relic.RelicId); // 독립 검사에 사용한 실제 유물 제거
        } // 테스트 회차 유물 정리 종료
    } // 유물과 전투의 실제 도주 시험 종료

    [UnityTest] // 실제 승리 결과 저장의 독립 회차 확인
    public IEnumerator VictoryAlsoCapturesTheRecoveredHealth() // 승리 타이밍에서 침식 회복의 실제 체력 저장 검증
    { // 전투 승리 결과 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 턴과 사망 이벤트 환경 진입
        VerifyVictorySettlement(); // 승리 이후의 실제 종료 스냅샷 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 전투 승리 결과 검사 종료

    private static void VerifyVictorySettlement() // 실제 적 처치의 승리 결과와 회복 검증
    { // 승리에서 침식 회복 시작
        var relic = LoadRelic(); // 정식 균열 봉합실 원본
        var relics = RelicRunManager.EnsureInstance(); // 실제 회차 유물 관리자
        relics.TryAcquire(relic); // 이번 승리의 실제 유물 보유
        try // 실제 피해와 승리 흐름 검증
        { // 승리 상태 검사 시작
            using var fight = new SettlementFight(2); // 실제로 적 하나와 생존 아군 둘을 시작한 전투
            var exposed = fight.Allies[0]; // 확실하게 실제 침식으로 다친 아군
            Unblocked(exposed, 15, BattleDamageType.Corrosion); // 실제 방어도 없는 침식의 체력 손실 적용
            fight.Enemy.TakeDamage(1_000, BattleDamageType.None, exposed); // 실제 적 전투불능 신호로 승리 확정
            Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 실제 전투 턴의 정상 승리 결과
            Assert.AreEqual(91, Health(Capture(fight.Turns), exposed)); // 정산 스냅샷에 저장된 실제 여섯 점 회복
        } // 승리 상태 검사 종료
        finally // 이번 회차의 보유 유물 정리
        { // 유물 정리 시작
            relics.TryRemove(relic.RelicId); // 다른 검사에 영향을 주지 않는 실제 장착 해제
        } // 유물 정리 종료
    } // 승리에서 침식 회복 종료

    private static RelicData LoadRelic() // 실제 시트에 등록된 종료 유물의 수치 확인
    { // 원본 유물 확인 시작
        var relic = Resources.Load<RelicData>("Relics/RiftSutureThread"); // 실제 균열 봉합실 등록 원본
        Assert.IsNotNull(relic); // 씬이 조회할 실제 유물 존재 확인
        Assert.AreEqual(6, relic.EffectValue); // 실제 기본 회복 여섯 점 확인
        Assert.AreEqual(RelicTriggerType.BattleEnded, relic.TriggerType); // 실제 전투 종료의 발동 정보 확인
        Assert.AreEqual(RelicEffectType.SutureCorrosionHealing, relic.EffectType); // 결과 정산에서 조회할 원본 효과 종류 확인
        Assert.AreEqual(RelicTargetType.AllAllies, relic.TargetType); // 실제 침식 이력이 있는 모든 생존 아군 검증
        return relic; // 현재 회차에 지급할 원본 유물
    } // 원본 유물 확인 종료

    private static int Unblocked(BattleUnitRuntime ally, int amount, BattleDamageType type) // 기존 무방어 실제 피해 메서드로 적용
    { // 해당 피해의 실제 진입점 조회 시작
        var method = typeof(BattleUnitRuntime).GetMethod("TakeUnblockedDamage", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 무방어 체력 소모 메서드 확인
        Assert.IsNotNull(method); // 일반 체력 비용 대신 실제 무방어 피해 경로 확인
        return (int)method.Invoke(ally, new object[] { amount, type, null, false }); // 실제 입력 타입을 선별하는 체력 피해 호출
    } // 해당 피해의 실제 진입점 조회 종료

    private static BattleResultData Capture(BattleTurnRuntime turns) // 실제 결과 씬에서 사용하는 단일 스냅샷 진입점 호출
    { // 실제 결과 저장 데이터 생성 시작
        var method = typeof(BattleTurnRuntime).GetMethod("CaptureBattleResult", BindingFlags.Instance | BindingFlags.NonPublic); // 게임 저장 호출이 쓰는 실제 스냅샷 메서드 확인
        Assert.IsNotNull(method); // 실제 결과 씬의 스냅샷 함수 연결 확인
        return (BattleResultData)method.Invoke(turns, new object[] { Array.Empty<string>() }); // 정식 전투 결과 데이터 캡처
    } // 실제 결과 저장 데이터 생성 종료

    private static int Health(BattleResultData result, BattleUnitRuntime ally) // 원본 배열에서 실제 아군의 저장 체력 조회
    { // 저장 아군 확인 시작
        return result.AllyStates.Single(state => state.UnitId == ally.UnitId).CurrentHealth; // 화면과 저장이 같은 방식으로 읽는 최종 체력
    } // 저장 아군 확인 종료

    private sealed class SettlementFight : IDisposable // 실제 종료 경로의 독립 전투 도우미
    { // 실제 종료 검사 환경 시작
        private readonly EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 실제 적의 전투 원본
        private readonly CardData filler = ScriptableObject.CreateInstance<CardData>(); // 정상 출전 덱에 필요한 최소 카드
        private readonly DeckData source; // 실제 전투 덱 생성 원본
        public BattleUnitRuntime[] Allies { get; } // 정상 결과 저장에 포함되는 출전 파티
        public BattleUnitRuntime Enemy { get; } // 실제 전투에 등록된 적
        public BattleTurnRuntime Turns { get; } // 실제 종료와 도주가 처리되는 전투 객체

        public SettlementFight(int count) // 실제 전투 시작과 생존 아군 조건 준비
        { // 실제 파티 초기화 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 표식이 다른 실제 캐릭터 명단
            Allies = roster.Characters.Take(count).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 실제 종료 후보의 순서와 원본 ID
            Assert.AreEqual(count, Allies.Length); // 각 회복 대상의 실제 캐릭터가 있는지 확인
            foreach (var ally in Allies) // 피해 전 동일 체력 상한의 정상 전투 대상 순회
            { // 실제 체력 정량 조건 준비 시작
                ally.ModifyMaxHealth(100 - ally.MaxHealth); // 정량적인 피해와 회복 수치의 최대 체력
                ally.ApplyPersistentHealth(100); // 실제 원래 만전 제어 상태
            } // 실제 체력 정량 조건 준비 종료
            Enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 대응 공격과 승리를 위한 적 등록
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(filler, roster.Characters[0]) }); // 원래 손패 경로의 고정 카드 구성
            var deck = BattleDeckRuntime.Create(source, Allies, 4, 254); // 실제 전투 덱 연결
            Turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), Allies, new[] { Enemy }, 0, BattleType.Normal); // 실제 적 처치와 도주 결과 경로 연결
            Assert.IsTrue(Turns.StartBattle(0)); // 아군이 실제 초기 전투 턴에 도착했는지 확인
        } // 실제 파티 초기화 종료

        public void Dispose() // 전투 시작 객체와 원본 목록 정리
        { // 검사 상태 정리 시작
            Turns.Dispose(); // 이전 전투의 실제 신호를 모두 해제
            UnityEngine.Object.Destroy(source); // 검사 덱 원본 해제
            UnityEngine.Object.Destroy(filler); // 검사 카드 원본 해제
            UnityEngine.Object.Destroy(enemyData); // 검사 적 원본 해제
        } // 검사 상태 정리 종료
    } // 실제 종료 검사 환경 종료
} // 침식 회복 유물 검사 종료
