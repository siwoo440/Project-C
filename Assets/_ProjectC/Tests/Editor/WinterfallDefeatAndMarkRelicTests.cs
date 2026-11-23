using System.Collections; // 실제 플레이 모드 전환의 프레임 대기
using System.Linq; // 생존 참가자와 실제 손패 조회
using NUnit.Framework; // 실제 드로우와 상태 수치 확인
using UnityEngine; // 원본 유물과 적 데이터 조회
using UnityEngine.TestTools; // 정식 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 공통 전투와 실제 원본 카드의 검사 환경

public sealed class WinterfallDefeatAndMarkRelicTests // 블랙박스 드로우와 번호표 표식의 실제 전투 회귀
{ // 전투 검사 시작
    [UnityTest] // 일반 패배와 부활 이후 실제 재패배 및 손패 상한 확인
    public IEnumerator BlackBoxDrawsOnEachActualAlliedDefeatWithNativeHandCapacity() // 같은 사망 처리 재진입과 독립 전투불능 구분
    { // 실제 전투불능 드로우 검사 시작
        yield return new EnterPlayMode(); // 기존 게임의 실제 원본 카드와 유물 사용
        Acquire("BrokenBlackBox"); // 출시 블랙박스의 정상 회차 보유
        using (var fight = new Fight("THRUST", "THRUST", "THRUST", "THRUST", "THRUST", "THRUST")) // 살아남을 시전자와 여섯 카드의 정상 공유 덱
        { // 실제 안타격 및 쓰러짐과 독립 손패 갱신 시작
            var fallen = fight.Allies[1]; // 카드 소유자와 다른 아군 전투원
            var enemy = fight.Enemies[0]; // 실제 치명 피해를 줄 원래 출처
            foreach (var card in fight.Deck.Hand.Take(4).ToArray()) // 실제 다시 뽑힐 카드 네 장의 버린 더미 준비
            { // 회차 카드 이동 시작
                Assert.IsTrue(fight.Deck.DiscardCard(card)); // 일반 검 카드의 원래 버리기 처리
            } // 회차 카드 이동 종료
            Assert.AreEqual(2, fight.Deck.HandCount); // 여유 네 칸이 있는 실제 손패 확인
            enemy.TakeDamage(enemy.MaxHealth * 3, BattleDamageType.None, fight.Allies[0]); // 적 전투불능 발생
            Assert.AreEqual(2, fight.Deck.HandCount); // 아군 전투불능만 블랙박스 발동
            fallen.TakeDamage(1, BattleDamageType.None); // 전투불능이 없는 실제 체력 손실
            Assert.AreEqual(2, fight.Deck.HandCount); // 일반 피해에서 손패 유지 확인
            fallen.ApplyStatusEffect(BattleStatusEffectType.ResurrectionSeed, 50, 2, 1); // 실제 씨앗 부활로 같은 아군의 다음 전투불능 준비
            fallen.TakeDamage(fallen.MaxHealth * 3, BattleDamageType.None, fight.Enemies[1]); // 치명 피해 후 실제 씨앗 정산
            Assert.IsFalse(fallen.IsDead); // 첫 전투불능이 별도 씨앗으로 복귀했는지 확인
            Assert.AreEqual(4, fight.Deck.HandCount); // 첫 전투불능에서 공유 두 장만 이동
            fallen.TakeDamage(fallen.MaxHealth * 3, BattleDamageType.None); // 출처를 지정하지 않은 실제 두 번째 전투불능
            Assert.IsTrue(fallen.IsDead); // 두 번째 독립 전투불능의 확정 확인
            Assert.AreEqual(6, fight.Deck.HandCount); // 실제 새 전투불능마다 한 번씩 두 장 추가
            Assert.IsTrue(fight.Deck.DiscardCard(fight.Deck.Hand[0])); // 같은 사망 재입력과 최대 한 장 남은 손패 준비
            fallen.TakeDamage(fallen.MaxHealth * 3, BattleDamageType.None); // 이미 쓰러진 같은 개체에 재입력
            Assert.AreEqual(5, fight.Deck.HandCount); // 이미 전투불능인 입력에서 추가 드로우 부재
            fight.Allies[2].TakeDamage(fight.Allies[2].MaxHealth * 3, BattleDamageType.None); // 다른 아군의 실제 독립 전투불능
            Assert.AreEqual(fight.Deck.MaxHandSize, fight.Deck.HandCount); // 정상 손패 상한까지만 실제 카드 이동
            Assert.IsTrue(fight.Deck.DiscardCard(fight.Deck.Hand[0])); // 마지막 아군을 잃기 전 빈자리 한 칸 확보
            fight.Allies[0].TakeDamage(fight.Allies[0].MaxHealth * 3, BattleDamageType.None); // 마지막 아군의 실제 전투불능으로 전투 종료
            Assert.AreEqual(BattleResult.Defeat, fight.Turns.Result); // 패배 우선순위와 정산이 정상 완료 확인
            Assert.IsEmpty(fight.Deck.PeekDrawTop(1)); // 전멸 뒤 사망 소유자의 카드가 재진입할 수 없음 확인
        } // 실제 안타격 및 쓰러짐과 독립 손패 갱신 종료
        yield return new ExitPlayMode(); // 원래 유물 회차와 실제 전투 검사 종료
    } // 실제 전투불능 드로우 검사 종료

    [UnityTest] // 첫 진영 선택과 무관한 개막 양쪽 표식 및 실제 자기 턴의 수명
    public IEnumerator IsolationNumberTagMarksBothSidesBeforeEitherSideActs() // 일반 및 선제 차례의 실제 상태 시계 대조
    { // 실제 개막 상태 검사 시작
        yield return new EnterPlayMode(); // 실제 유물과 턴 신호를 쓰는 플레이 모드
        Acquire("IsolationRoomNumberTag"); // 출시 격리실 번호표의 정상 획득
        using (var fight = new Fight("THRUST")) // 아군이 먼저인 실제 표준 전투
        { // 정상 선공의 양쪽 지속 시간 확인 시작
            var ally = Marked(fight.Allies); // 표식을 받은 생존 아군 한 명
            var enemy = Marked(fight.Enemies); // 표식을 받은 생존 적 한 명
            Assert.AreEqual(2, ally.RemainingTurns); // 첫 플레이어 턴의 온전한 두 아군 차례 확인
            Assert.AreEqual(2, enemy.RemainingTurns); // 적이 움직이기 전 원래 두 적 차례 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 아군 턴 완료
            Assert.AreEqual(1, ally.RemainingTurns); // 아군 표식만 첫 아군 종료에 한 번 소모
            Assert.AreEqual(2, enemy.RemainingTurns); // 적 표식의 적 턴 전용 유지 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 턴 완료
            Assert.AreEqual(1, ally.RemainingTurns); // 적 턴 종료에서 아군 표식 유지
            Assert.AreEqual(1, enemy.RemainingTurns); // 첫 적 턴에서 적 표식의 기간 소모
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 아군 턴 완료
            Assert.IsTrue(ally.IsExpired); // 두 번째 실제 아군 턴에서 아군 표식 만료
            Assert.IsFalse(enemy.IsExpired); // 적의 두 번째 차례를 아직 시작한 상태
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 완료
            Assert.IsTrue(enemy.IsExpired); // 두 적 턴을 마친 실제 적 표식 만료
            Assert.IsFalse(fight.Allies.Concat(fight.Enemies).Any(unit => unit.GetStatusEffect(BattleStatusEffectType.PublicMark) != null)); // 새 전투 중 한 번의 개막 발동만 존재
        } // 정상 선공의 양쪽 지속 시간 확인 종료
        using (var fight = new Fight(true, "THRUST")) // 적 선제로 시작하는 정상 전투
        { // 적 선제의 동일 개막 조건 확인 시작
            var ally = Marked(fight.Allies); // 선제 이전에 이미 들어간 아군 표식
            var enemy = Marked(fight.Enemies); // 선제 이전에 이미 들어간 적 표식
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turns.CurrentPhase); // 선제 적 턴에 실제로 진입했는지 확인
            Assert.AreEqual(2, ally.RemainingTurns); // 선제 중에도 두 아군 턴 전체 유지
            Assert.AreEqual(2, enemy.RemainingTurns); // 선제에서 행동하기 전 두 적 턴 유지
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 선제 턴의 실제 완료
            Assert.AreEqual(2, ally.RemainingTurns); // 첫 아군 차례 시작 전까지 아군 소모 부재
            Assert.AreEqual(1, enemy.RemainingTurns); // 처음 적 선제 턴도 정식 적 턴으로 한 번 차감
        } // 적 선제의 동일 개막 조건 확인 종료
        yield return new ExitPlayMode(); // 실제 두 개막 절차의 검사 종료
    } // 실제 개막 상태 검사 종료

    [UnityTest] // 감시 포드 렌즈와 같은 개체에 겹친 독립 개막 효과 확인
    public IEnumerator OpeningMarksShareOneNativeStackAndAreClearedWithTheFight() // 별도 표식 중첩 부재와 종료 시 양쪽 실제 회수
    { // 일반 공명 표식 공용 동작 검사 시작
        yield return new EnterPlayMode(); // 실제 시작 효과와 정식 상태 목록 사용
        Acquire("IsolationRoomNumberTag"); // 격리실 번호표 개막 효과 보유
        Acquire("WatchPodLens"); // 같은 실제 적에게 겹칠 기존 감시 포드 렌즈 보유
        var template = ScriptableObject.CreateInstance<EnemyData>(); // 단일 적의 정상 시작 원본
        try // 임시 적 자료 정리 보장
        { // 실제 한 명의 적과 개막 양쪽 표식 검사 시작
            var fight = Fight.WithEnemySources(new[] { template }, "THRUST"); // 두 유물이 반드시 같은 실제 적을 선정하는 혼합 전투
            using (fight) // 턴 관리자와 상태 처리를 정식으로 해제
            { // 동일 적의 실제 상태 검사 시작
                var mark = Marked(fight.Enemies); // 실제 단일 적의 현재 공명 표식
                Assert.AreEqual(1, mark.StackCount); // 중복 적중에도 단일 공명 표식 중첩 유지
                Assert.AreEqual(2, mark.RemainingTurns); // 리프레시에서 원래 두 적 턴 유지
                Assert.IsTrue(mark.PublicMarkCountsEnemyTurns); // 기존 감시 포드 렌즈와 API 호환 확인
                Assert.IsFalse(Marked(fight.Allies).PublicMarkCountsEnemyTurns); // 아군 표식의 실제 아군 차례 적용 확인
            } // 동일 적의 실제 상태 검사 종료
            Assert.IsFalse(fight.Allies.Concat(fight.Enemies).Any(unit => unit.GetStatusEffect(BattleStatusEffectType.PublicMark) != null)); // 전투 해제와 함께 번호표 및 렌즈 공명 표식 회수
        } // 실제 한 명의 적과 개막 양쪽 표식 검사 종료
        finally // 테스트가 만든 게임 자료 정리
        { // 검사 전용 원본의 메모리 해제 시작
            Object.Destroy(template); // 다른 전투에서 재사용하지 않는 임시 적 자료 제거
        } // 검사 전용 원본의 메모리 해제 종료
        yield return new ExitPlayMode(); // 기존 렌즈 중첩과 회수의 검증 환경 종료
    } // 일반 공명 표식 공용 동작 검사 종료

    private static BattleStatusEffectInstance Marked(BattleUnitRuntime[] units) // 해당 진영의 실제 네이티브 공명 표식 조회
    { // 단일 실제 생존 표식 대상 검사 시작
        var marks = units.Select(unit => unit.GetStatusEffect(BattleStatusEffectType.PublicMark)).Where(mark => mark != null).ToArray(); // 각 실제 참가자의 활성 공명 표식 수집
        Assert.AreEqual(1, marks.Length); // 같은 진영 한 명에게만 실제 표식 확인
        return marks[0]; // 실제 배치에서 찾은 유일한 활성 표식의 수치 반환
    } // 단일 실제 생존 표식 대상 검사 종료

    private static void Acquire(string resourceName) // 현재 회차에서 실제 원본 유물 획득
    { // 출시 원본의 획득 요청 시작
        var relic = Resources.Load<RelicData>("Relics/" + resourceName); // Unity에서 실제 가져온 유물 원본
        Assert.IsNotNull(relic, resourceName); // 배정된 실제 원본 경로 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 현재 테스트 회차 실제 보유에 추가
    } // 출시 원본의 획득 요청 종료
} // 전투 검사 종료
