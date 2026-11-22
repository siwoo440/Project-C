using System; // 검사 유물 자동 정리
using System.Collections; // 실제 싸움과 상태 프레임 확인
using NUnit.Framework; // 실제 전투 결과 검증
using UnityEngine; // 정식 유물 원본과 정수 난수 사용
using UnityEngine.TestTools; // Unity 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 입력과 턴 검사 환경
using Object = UnityEngine.Object; // 검사 전용 유물 관리자 정리
using Random = UnityEngine.Random; // 이전 난수 상태와 독립적인 경계 검사

public sealed class WinterfallBunkerAllyRelicTests // 벙커 아군 유물의 실제 전투 조건
{ // 유물 회귀 검사 시작
    [UnityTest] // 선제와 일반 전투의 첫 시작 드로우와 실제 저항 확인
    public IEnumerator SurveillanceResistanceAppliesBeforeEnemyFirstAndOnlyFirstBaseHandLosesOne() // 첫 손패 한 장과 전투 중 저항 평탄 수치
    { // 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 원래 유물 보관함과 게임 준비
        using (var owned = new OwnedRelic("SurveillanceCoreFragment")) // 게임의 감시 코어 조각 실제 획득
        { // 양쪽 선제 조건 검사 시작
            foreach (bool enemyFirst in new[] { false, true }) // 일반 시작과 적 선제 원래 경로 순회
            { // 새로운 교전의 첫 입력 검사 시작
                using var fight = new Fight(enemyFirst, "THRUST", "THRUST", "THRUST"); // 단순 원본 세 장으로 독립 전투 구성
                foreach (var ally in fight.Allies) // 실제 출전 아군 전체 확인
                { // 저항과 물리 방어 분리 검사 시작
                    Assert.AreEqual(ally.BaseMagicalResistance + 10, ally.MagicalResistance); // 아군의 전투용 현재 저항 증가
                    Assert.AreEqual(ally.BasePhysicalDefense, ally.PhysicalDefense); // 보존할 물리 방어 수치
                    Assert.AreEqual(ally.MagicalResistance, ally.PreviewDamage(100, BattleDamageType.Magical).DefenseValue); // 실제 예고에 전달된 같은 저항
                } // 저항과 물리 방어 분리 검사 종료
                Assert.AreEqual(fight.Enemies[0].BaseMagicalResistance, fight.Enemies[0].MagicalResistance); // 적에게 복제되지 않은 아군 저항
                if (enemyFirst) // 실제 적 선제로 아직 없는 첫 손패 확인
                { // 정상 첫 적 행동 종료 시작
                    Assert.AreEqual(0, fight.Deck.HandCount); // 시작 손패 미지급 확인
                    Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 적 차례 완료로 첫 아군 손패 지급
                } // 정상 첫 적 행동 종료 종료
                Assert.AreEqual(2, fight.Deck.HandCount); // 기본 세 장의 첫 한 장 감소
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 아군 종료 입력
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 아군 손패를 지급한 정상 적 종료
                Assert.AreEqual(3, fight.Deck.HandCount); // 다음 아군부터 원래 기본 세 장 복귀
            } // 새로운 교전의 첫 입력 검사 종료
        } // 양쪽 선제 조건 검사 종료
        yield return new ExitPlayMode(); // 플레이 검사 환경 종료
    } // 실제 전투 검사 종료

    [UnityTest] // 화상의 양수 체력 피해와 실제 발동 확률 및 수명 확인
    public IEnumerator RadiatorRollsOnlyAfterRealBurnHealthLossAndGrantsOneForTwoAllyTurns() // 방어와 직접 피해 제외 및 두 아군 턴 강화
    { // 화상 회귀 검사 시작
        yield return new EnterPlayMode(); // 실제 아군 공격력과 턴 진행 준비
        using (var owned = new OwnedRelic("WasteHeatRadiator")) // 게임의 폐열 방열판 실제 획득
        using (var fight = new Fight("THRUST")) // 원래 턴 종료 상태를 사용하는 독립 교전
        { // 아군 화상과 정수 판정 검사 시작
            var ally = fight.Allies[0]; // 실제로 화상과 기존 강화가 있는 아군
            ally.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 3, 8, 1); // 유물 강화와 독립해야 할 원래 카드 강화
            ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 5, 8, 1); // 여러 차례 양수 피해가 생길 원래 화상
            ally.TakeDamage(1, BattleDamageType.None); // 기존 화상이 있어도 직접 피해에서 유물 발동 금지
            Assert.AreEqual(3, ally.AttackPowerBonus); // 직접 피격 뒤 원래 강화만 유지
            ally.GainBlock(20); // 첫 화상 피해를 전부 막는 실제 방어도
            Random.InitState(SeedForRoll(0)); // 피해 조건을 잘못 통과하면 확정될 정수 난수
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 방어된 화상의 턴 종료
            Assert.AreEqual(3, ally.AttackPowerBonus); // 방어도만 줄어든 화상에서 독립 강화 없음
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 기존 방어도를 없앤 다음 아군 차례
            int before = ally.CurrentHealth; // 실패 경계 직전 아군 체력
            Random.InitState(SeedForRoll(40)); // 사십 퍼센트에 포함되지 않는 첫 정수
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 피해가 있는 첫 확률 판정
            Assert.Less(ally.CurrentHealth, before); // 실제 체력 손실이 있는 화상 확인
            Assert.AreEqual(3, ally.AttackPowerBonus); // 실패 경계에서 기존 공격력 유지
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 성공 경계의 다음 실제 아군 차례
            Random.InitState(SeedForRoll(39)); // 사십 퍼센트에 포함되는 마지막 정수
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 화상의 성공 판정
            Assert.AreEqual(4, ally.AttackPowerBonus); // 기존 강화에 정확히 한 점 독립 가산
            var effect = ally.GetStatusEffect(BattleStatusEffectType.WasteHeatAttackPowerUp); // 이번 유물의 별도 지속 상태
            Assert.AreEqual(2, effect.RemainingTurns); // 다음 두 아군 차례를 위한 실제 원본 기간
            ally.RemoveStatusEffect(BattleStatusEffectType.Burn); // 수명 검사에서 반복 화상 갱신 제외
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 턴 동안 공격력 강화 유지
            Assert.AreEqual(2, effect.RemainingTurns); // 적 턴 종료로 아군 지속 차감 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 강화 아군 차례 종료
            Assert.AreEqual(1, effect.RemainingTurns); // 첫 아군 차례 후 남은 한 번
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 강화 아군 차례 진입
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 강화 아군 차례 종료
            Assert.AreEqual(3, ally.AttackPowerBonus); // 원래 공격력만 남고 유물 효과 만료
        } // 아군 화상과 정수 판정 검사 종료
        yield return new ExitPlayMode(); // 실제 턴 검사 환경 종료
    } // 화상 회귀 검사 종료

    [UnityTest] // 치명 및 차단 피해를 건너뛰고 전투당 첫 저체력 회복 확인
    public IEnumerator RebootHealsOneLivingAllyAtInclusiveThresholdUsingActualHealingRules() // 공용 한 번과 받은 회복 보정의 실제 결과
    { // 체력 회귀 검사 시작
        yield return new EnterPlayMode(); // 실제 런타임 체력과 공유 자원 준비
        using (var owned = new OwnedRelic("EmergencyRebootSwitch")) // 게임의 긴급 재부팅 스위치 실제 획득
        using (var fight = new Fight("THRUST")) // 서로 다른 아군이 있는 실제 한 교전
        { // 실제 양수 피해와 회복 검사 시작
            var ally = fight.Allies[0]; // 체력 삼십 퍼센트 경계의 실제 지급 대상
            var other = fight.Allies[1]; // 같은 전투에서 두 번째 소비를 막을 실제 아군
            var fallen = fight.Allies[2]; // 실제 사망에도 소비되지 않는 대상
            ally.ModifyMaxHealth(100 - ally.MaxHealth); // 테스트 체력 비율의 정확한 백 분모
            other.ModifyMaxHealth(100 - other.MaxHealth); // 다음 아군의 동일 체력 비율
            fallen.ApplyPersistentHealth(1); // 치명 피해를 받을 초기 체력
            fallen.TakeDamage(1, BattleDamageType.None, fight.Enemies[0]); // 스위치 사용 전 실제 쓰러진 아군
            Assert.IsTrue(fallen.IsDead); // 기존 패배 경로에서 사망 대상 회복 금지
            ally.ApplyPersistentHealth(32); // 곧 양수 피해를 받아도 삼십을 넘기는 체력
            ally.TakeDamage(1, BattleDamageType.None, fight.Enemies[0]); // 경계를 충족하지 않는 실제 피해
            Assert.AreEqual(31, ally.CurrentHealth); // 아직 발동하지 않은 상태
            ally.ApplyPersistentHealth(30); // 실제 임계값의 방어도 소모 비교 준비
            ally.GainBlock(1); // 이번 한 점 전부 막도록 실제 방어 적용
            ally.TakeDamage(1, BattleDamageType.None, fight.Enemies[0]); // 체력 손실 없는 실제 피격
            Assert.AreEqual(30, ally.CurrentHealth); // 방어도 피해의 회복 미발동
            other.ApplyPersistentHealth(31); // 회복량 영 차단의 먼저 받을 경계 조건
            other.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 100, 3, 1); // 원래 상태로 실제 회복량 완전 차단
            other.TakeDamage(1, BattleDamageType.None, fight.Enemies[0]); // 발동 자격이 생기는 첫 실제 임계값
            Assert.AreEqual(30, other.CurrentHealth); // 회복되지 않은 상태에서 공용 횟수 보존
            other.RemoveStatusEffect(BattleStatusEffectType.HealingReceivedDown); // 후속 공용 한 번의 효과 검사를 위한 상태 해제
            ally.ApplyPersistentHealth(31); // 실제 한 점으로 경계를 통과하는 체력
            ally.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 50, 3, 1); // 실제 수신 회복량 절반 감소
            Assert.AreEqual(fight.Points.MaxActionPoints, fight.Points.CurrentActionPoints); // 공용 행동력 상한과 독립 발동 준비
            ally.TakeDamage(1, BattleDamageType.None, fight.Enemies[0]); // 정확히 삼십 퍼센트가 되는 양수 피해
            Assert.AreEqual(36, ally.CurrentHealth); // 기본 회복 열둘에 절반 감소를 적용한 여섯
            other.ApplyPersistentHealth(31); // 두 번째 생존 아군의 같은 체력 경계
            other.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1); // 방어도 우회 피해의 원래 출혈 상태 준비
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 같은 교전의 실제 출혈 체력 피해와 공용 횟수 확인
            Assert.AreEqual(30, other.CurrentHealth); // 같은 교전에서 두 번째 회복 없음
        } // 실제 양수 피해와 회복 검사 종료
        yield return new ExitPlayMode(); // 플레이 검사 환경 종료
    } // 체력 회귀 검사 종료

    private static int SeedForRoll(int expected) // Unity 원래 백분율 난수의 경계값 조회
    { // 실제 정수 난수 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 원래 난수에서 경계값을 재현할 초기 시드 순회
        { // 다음 시드의 실제 첫 판정 시작
            Random.InitState(seed); // 이번 난수 시드 검증 설정
            if (Random.Range(0, 100) == expected) // 필요한 정확한 정수인지 확인
            { // 정수 판정 재현 시작
                return seed; // 같은 첫 판정을 만드는 실제 시드 반환
            } // 정수 판정 재현 종료
        } // 다음 시드의 실제 첫 판정 종료
        throw new InvalidOperationException("필요한 Unity 난수 경계값 없음"); // 원래 난수 조건 없음 오류
    } // 실제 정수 난수 검색 종료

    private sealed class OwnedRelic : IDisposable // 실제 게임 유물 획득과 검사 종료 정리
    { // 독립 회차 유물 처리 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 기존 회차 보관함의 생성 여부
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 이번에 획득할 실제 유물 보관함
        private readonly RelicData relic; // 실제 원본 Unity 유물 자료
        internal OwnedRelic(string key) // 정식 리소스 경로에서 검사할 유물 생성
        { // 정식 유물 획득 시작
            relic = Resources.Load<RelicData>("Relics/" + key); // 실제 리소스에 등록한 Unity 유물 조회
            Assert.IsNotNull(relic, key); // 정식 원본 존재 조건 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 게임의 유물 획득 성공 확인
        } // 정식 유물 획득 종료
        public void Dispose() // 이전 게임 상태로 돌아가는 검사 종료
        { // 독립 회차 유물 처리 종료 시작
            manager.TryRemove(relic.RelicId); // 이번 검사에서 획득한 유물만 제거
            if (previous == null) // 원래 게임에 유물 보관함이 없었는지 확인
            { // 이번 검사 객체의 실제 제거 시작
                Object.Destroy(manager.gameObject); // 원래 없던 테스트 전용 보관함 정리
            } // 이번 검사 객체의 실제 제거 종료
        } // 독립 회차 유물 처리 종료 완료
    } // 독립 회차 유물 처리 종료
} // 유물 회귀 검사 종료
