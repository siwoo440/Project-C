using System; // 낮은 저항의 영 하한 확인과 검증 객체 정리
using System.Collections; // 실제 Unity 전투 단계 대기
using System.Linq; // 실제 적 표시와 방어 획득 대상 확인
using NUnit.Framework; // 게임 상태와 기존 기대값 비교
using UnityEngine; // 명시한 원본과 기계형 검증 적 생성
using UnityEngine.TestTools; // 실제 플레이 모드의 전투 실행
using Fight = WinterfallSwordCombatTests.Fight; // 기존 적·아군과 전투 입력 환경 재사용
using Object = UnityEngine.Object; // 검사 객체의 원래 Unity 정리

public sealed class WinterfallFactoryOpeningAndArmorRelicTests // 공장 개막과 전투용 방어의 실제 효과
{ // 신규 유물의 통합 플레이 검사 시작
    [UnityTest] // 실제 선제 전에 적 전체 표식과 아군 저항 거래 확인
    public IEnumerator DeadlockStartsBeforeEnemyFirstAndKeepsLongerNativeMark() // 초반 공명 표식과 고정 동료 저항의 정확한 범위
    { // 실제 유물 조우 검사 시작
        yield return new EnterPlayMode(); // 기존 게임 환경과 유물 보관함 접근
        using (var deadlock = new OwnedRelic("DeadlockBackupChip")) // 정식 백업칩 획득
        using (var tracker = new OwnedRelic("BlackDesertTrackerPin")) // 먼저 주어지는 세 적 턴 표식
        using (var surveillance = new OwnedRelic("SurveillanceCoreFragment")) // 기존 아군 저항 보너스와 합산 확인
        using (var fight = new Fight(true, "THRUST")) // 적 선제 상태에서 실제 전투 시작
        { // 양쪽 마법 저항과 표식 확인 시작
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turns.CurrentPhase); // 실제 첫 행동 진영 확인
            Assert.That(fight.Allies.All(ally => ally.MagicalResistance == ally.BaseMagicalResistance + 5)); // 기존 저항 열 점과 백업칩 다섯 점 감소 합산
            Assert.That(fight.Enemies.All(enemy => enemy.MagicalResistance == enemy.BaseMagicalResistance)); // 아군만 받는 마법 저항 대가
            var wider = fight.Enemies.Single(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns == 3); // 기존 세 적 턴 표시가 보존된 적
            var marked = fight.Enemies.Where(enemy => enemy != wider).ToArray(); // 신규 표식이 발동한 나머지 적
            Assert.That(marked.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns == 2)); // 선제 전에 살아 있는 적 모두 지정
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 차례 종료에서 표식 한 번 차감
            Assert.That(marked.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns == 1)); // 첫 실제 적 차례 소비
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 종료는 적 표식 기간과 독립
            Assert.That(marked.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns == 1)); // 적 차례에만 유효 기간 감소
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 차례 종료
            Assert.That(marked.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark) == null)); // 새 표식은 정확히 두 적 턴 종료에 해제
            Assert.AreEqual(1, wider.GetStatusEffect(BattleStatusEffectType.PublicMark).RemainingTurns); // 긴 기존 표식 기간 유지
        } // 양쪽 마법 저항과 표식 확인 종료
        using (var deadlock = new OwnedRelic("DeadlockBackupChip")) // 고정 저항 대가 단독 확인
        using (var fight = new Fight("THRUST")) // 저항의 영 하한을 가진 독립 조우
        { // 아군 마법 저항 하한 검사 시작
            Assert.That(fight.Allies.All(ally => ally.MagicalResistance == Math.Max(0, ally.BaseMagicalResistance - 5))); // 실제 아군 마법 저항 다섯 점 감소와 영 하한
        } // 아군 마법 저항 하한 검사 종료
        yield return new ExitPlayMode(); // 독립 전투 원래 상태 복원
    } // 실제 유물 조우 검사 종료

    [UnityTest] // 시작 방어와 첫 실제 아군 행동 속도 대가의 검증
    public IEnumerator BasiliskBlockIsReadyBeforeEitherOpeningSideAndSpeedDebtUsesFirstAllyTurn() // 정상·적 선제 시작의 같은 방어 대상
    { // 실제 시작 방어 및 속도 확인 시작
        yield return new EnterPlayMode(); // Unity의 전투 초기화 흐름 시작
        using (var owned = new OwnedRelic("BasiliskEngineCore")) // 정식 엔진 코어 원본 획득
        { // 실제 두 개시 순서의 교전 검증 시작
            foreach (bool enemyFirst in new[] { false, true }) // 일반 시작과 적 선제의 각 전투 흐름
            { // 새 실제 조우의 첫 아군 확인 시작
                using var fight = new Fight(enemyFirst, "THRUST"); // 방어 유물만 보유한 실제 전투
                var ally = fight.Allies[0]; // 처음 살아 있는 실제 편성 아군
                Assert.AreEqual(30, ally.CurrentBlock); // 첫 행동 전에 원래 방어도 서른 점 확인
                Assert.AreEqual(0, fight.Allies[1].CurrentBlock); // 같은 조건의 두 번째 아군에는 방어 지급 없음
                if (enemyFirst) // 적 선제에서 아직 미지급인 속도 대가
                { // 실제 선제 공격과 그 뒤의 첫 아군 차례 시작
                    Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 아직 시작하지 않은 아군 차례의 약화 없음
                    int before = ally.CurrentHealth; // 적의 실제 개막 타격 이전 생존 체력
                    ally.TakeDamage(5, BattleDamageType.None, fight.Enemies[0]); // 개막 보호에 흡수되는 원래 적 타격
                    Assert.AreEqual(25, ally.CurrentBlock); // 먼저 지급된 방어도가 실제 선제 피해 흡수
                    Assert.AreEqual(before, ally.CurrentHealth); // 개막 방어로 실제 체력 보호
                    Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 종료된 실제 적 행동 뒤 첫 아군 시작
                    Assert.AreEqual(0, ally.CurrentBlock); // 정상 턴 경계에서 쓰고 남은 개시 방어도 정리
                } // 실제 선제 공격과 그 뒤의 첫 아군 차례 종료
                Assert.AreEqual(-1, ally.ActionSpeedModifier); // 정상 첫 실제 아군 턴의 원본 속도 대가
                Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown).RemainingTurns); // 해당 한 아군 턴의 실제 약화 기간
                Assert.IsNull(fight.Allies[1].GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 보호막 수령자에게만 적용하는 대가
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 첫 아군 차례의 종료 처리
                Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 첫 자기 턴에 정확히 끝난 대가
            } // 새 실제 조우의 첫 아군 확인 종료
        } // 실제 두 개시 순서의 교전 검증 종료
        yield return new ExitPlayMode(); // 기존 게임 데이터 보호와 검사 종료
    } // 실제 시작 방어 및 속도 확인 종료

    [UnityTest] // 실제 처치 소유자별 기계 방어 누적과 재지급 확인
    public IEnumerator ScrapMotherAccumulatesOnlyRealMechanicalDefeatsOnEachLivingOwner() // 원본 분류와 다음 아군 턴의 한 소유자 복구
    { // 원본 기계 분류의 실전 확인 시작
        yield return new EnterPlayMode(); // 실제 전투 맵과 상태 초기화
        using (var owned = new OwnedRelic("ScrapMotherReassemblyCore")) // 정식 스크랩 마더의 재조립핵 획득
        { // 독립 검증 적 자료의 실제 전투 시작
            var mechanical = MakeEnemy("crew-mechanical", true); // 실제 타입을 명시한 소형 기계 검증 원본
            var organic = MakeEnemy("crew-organic", false); // 상태가 같지만 기계 분류가 없는 원본
            try // 원래 Unity 검사 객체의 정리 보장
            { // 서로 다른 대상으로 실제 처치 적용 시작
                using var fight = Fight.WithEnemySources(new[] { mechanical, mechanical, mechanical, organic, mechanical, organic }, "THRUST"); // 살아 있는 마지막 적을 남기는 실제 조우
                var first = fight.Allies[0]; // 기계 적을 둘 제거할 실제 첫 아군
                var second = fight.Allies[1]; // 별도로 기계 적 하나를 제거할 둘째 아군
                fight.Enemies[0].TakeDamage(100, BattleDamageType.None, first); // 첫 아군의 첫 실제 기계형 처치
                Assert.AreEqual(5, first.CurrentBlock); // 유물의 즉시 방어도 지급
                fight.Enemies[0].TakeDamage(100, BattleDamageType.None, first); // 이미 쓰러진 같은 대상 추가 타격
                Assert.AreEqual(5, first.CurrentBlock); // 재타격이 실제 새 처치로 중복 집계되지 않음
                fight.Enemies[1].TakeDamage(100, BattleDamageType.None, first); // 또 다른 실제 기계형 처치
                fight.Enemies[2].TakeDamage(100, BattleDamageType.None, second); // 별도 아군의 실제 기계형 처치
                fight.Enemies[3].TakeDamage(100, BattleDamageType.None, first); // 같은 아군의 비기계형 처치
                fight.Enemies[4].TakeDamage(100, BattleDamageType.None); // 어떠한 실제 처치 소유자도 없는 피해
                Assert.AreEqual(10, first.CurrentBlock); // 양쪽 제외를 반영한 첫 아군의 누적 방어도
                Assert.AreEqual(5, second.CurrentBlock); // 둘째의 누적 방어도만 보존
                Assert.AreEqual(0, fight.Allies[2].CurrentBlock); // 실제로 처치하지 않은 아군 지급 없음
                first.ClearBlock(); // 적에게 현재 모든 방어도를 잃은 상황
                second.ClearBlock(); // 실제 소유자별 다음 시작 복구 준비
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 아군 종료
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 같은 교전의 다음 실제 아군 차례
                Assert.AreEqual(10, first.CurrentBlock); // 첫 아군에게만 이전 기계 처치 누적 복구
                Assert.AreEqual(5, second.CurrentBlock); // 둘째 아군에게만 이전 기계 처치 누적 복구
                second.ApplyPersistentHealth(0); // 이어지는 차례에 죽어서 방어를 받을 수 없는 아군
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 기존 상태와 공격 횟수 정리
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 같은 전투의 더 늦은 실제 아군 시작
                Assert.AreEqual(10, first.CurrentBlock); // 생존한 실제 소유자의 지속 누적 유지
                Assert.AreEqual(0, second.CurrentBlock); // 쓰러진 실제 소유자에게 해당 턴 복구 없음
            } // 서로 다른 대상으로 실제 처치 적용 종료
            finally // 기존 Unity 검사 객체 및 분류 복구
            { // 독립 적 원본 정리 시작
                Object.Destroy(mechanical); // 등록하지 않은 검증용 기계 원본 정리
                Object.Destroy(organic); // 등록하지 않은 검증용 일반 적 원본 정리
            } // 독립 적 원본 정리 종료
            using var next = new Fight("THRUST"); // 같은 유물을 가진 새로운 별도 전투
            Assert.That(next.Allies.All(ally => ally.CurrentBlock == 0)); // 이전 전투의 개별 처치 횟수 이월 금지
        } // 독립 검증 적 자료의 실제 전투 종료
        yield return new ExitPlayMode(); // 실제 유물 전투 검사 정리
    } // 원본 기계 분류의 실전 확인 종료

    private static EnemyData MakeEnemy(string id, bool mechanical) // 게임의 현재 명시적 기계 분류를 설정할 검증 원본 생성
    { // 기계 분류를 가진 독립 테스트 원본 설정 시작
        var enemy = ScriptableObject.CreateInstance<EnemyData>(); // 기존 Unity 직렬화형 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"enemyId\":\"{id}\",\"maxHealth\":50,\"isMechanical\":{(mechanical ? "true" : "false")}}}", enemy); // 실제 정식 기계 분류와 현재 체력 수치 지정
        return enemy; // 같은 정상 EnemyData 형식의 검사 원본 반환
    } // 기계 분류를 가진 독립 테스트 원본 설정 종료

    private sealed class OwnedRelic : IDisposable // 검사마다 유물의 실제 획득과 회수
    { // 독립 유물 보관함 처리 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 기존 게임 회차 보관함의 존재 확인
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 현재 실물 유물의 정상 보관소
        private readonly RelicData relic; // 현재 테스트만 소유하고 해제할 유물
        internal OwnedRelic(string stem) // 새로운 유물의 실제 리소스 이름 연결
        { // 실물 유물 획득 시작
            relic = Resources.Load<RelicData>("Relics/" + stem); // Unity에 이미 등록하는 표준 리소스 경로
            Assert.IsNotNull(relic, stem); // 실물 원본이 연결됐는지 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 게임의 일반 획득 처리 성공 확인
        } // 실물 유물 획득 종료
        public void Dispose() // 이 테스트가 소유한 유물과 객체의 정리
        { // 이후 실제 회차의 원래 상태 복구 시작
            manager.TryRemove(relic.RelicId); // 이번 검사에서 소유한 아이디만 제거
            if (previous == null) // 원래 게임에 유물 객체가 없었던 경우 확인
            { // 임시 실제 런타임 객체 정리 시작
                Object.Destroy(manager.gameObject); // 외부 게임 객체는 유지하고 검증용만 정리
            } // 임시 실제 런타임 객체 정리 종료
        } // 이후 실제 회차의 원래 상태 복구 종료
    } // 독립 유물 보관함 처리 종료
} // 신규 유물의 통합 플레이 검사 종료
