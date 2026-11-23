using NUnit.Framework; // 확률 유물 검사 사용
using UnityEngine; // 무작위 상태와 원본 사용

public sealed class ChanceRelicTests // 확률형 유물 검사
{ // 검사 시작
    [Test] // 실제 확률과 대상 검사
    public void HandkerchiefUsesTenPercentAndTargetsOneLivingEnemy() // 임시 확률과 디버프 확인
    { // 검사 본문 시작
        RelicData relic = Resources.Load<RelicData>("Relics/BlackHandkerchief"); // 실제 유물 원본 조회
        Assert.IsNotNull(relic); // 유물 원본 확인
        Assert.AreEqual(10, relic.TriggerChancePercent); // 사용자 지정 임시 확률 확인
        Assert.AreEqual(100, Resources.Load<RelicData>("Relics/AshSeal").TriggerChancePercent); // 기존 유물 확정 발동 유지 확인
        EnemyData source = ScriptableObject.CreateInstance<EnemyData>(); // 테스트 적 원본 생성
        Random.State oldRandom = Random.state; // 기존 무작위 상태 보관
        int activated = 0; // 실제 발동 횟수 초기화
        try // 무작위 상태 복원 보장
        { // 확률 검사 시작
            for (int index = 0; index < 500; index++) // 독립 전투 다섯백 회 반복
            { // 개별 전투 시작
                Random.InitState(index); // 재현 가능한 전투 난수 설정
                BattleUnitRuntime first = BattleUnitRuntime.CreateEnemy(source); // 첫 생존 적 생성
                BattleUnitRuntime second = BattleUnitRuntime.CreateEnemy(source); // 두 번째 생존 적 생성
                BattleUnitRuntime dead = BattleUnitRuntime.CreateEnemy(source); // 사망 적 생성
                dead.TakeDamage(100000, BattleDamageType.Physical); // 테스트 적 사망 처리
                Assert.IsTrue(dead.IsDead); // 사망 상태 확인
                RelicGoldRuntime wallet = new RelicGoldRuntime(); // 독립 지갑 생성
                RelicInventoryRuntime inventory = new RelicInventoryRuntime(wallet); // 유물 보관함 생성
                Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 실제 유물 획득 확인
                BattleEventDispatcher events = new BattleEventDispatcher(); // 독립 이벤트 발행기 생성
                BattleRelicEffectController controller = new BattleRelicEffectController(events, inventory, wallet, new BattleUnitRuntime[0], new[] { first, second, dead }); // 적 목록 연결
                controller.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 전투 시작 유물 판정
                BattleStatusEffectInstance firstStatus = first.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown); // 첫 적 디버프 조회
                BattleStatusEffectInstance secondStatus = second.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown); // 두 번째 적 디버프 조회
                int affected = (firstStatus == null ? 0 : 1) + (secondStatus == null ? 0 : 1); // 디버프 대상 수 계산
                Assert.LessOrEqual(affected, 1); // 생존 적 한 명 이하 확인
                Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 사망 적 제외 확인
                events.Publish(new BattleEventContext(BattleEventType.BattleStarted, 1, BattleTurnPhase.PlayerTurn)); // 중복 전투 시작 알림
                int afterRepeat = (first.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown) == null ? 0 : 1) + (second.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown) == null ? 0 : 1); // 중복 알림 후 대상 수 계산
                Assert.AreEqual(affected, afterRepeat); // 실패한 시작 판정의 재시도 방지 확인
                BattleStatusEffectInstance applied = firstStatus ?? secondStatus; // 실제 적용 상태 조회
                if (applied != null) // 유물 발동 확인
                { // 효과 수치 확인 시작
                    activated++; // 발동 횟수 증가
                    Assert.AreEqual(1, applied.EffectiveValue); // 임시 방어력 감소량 확인
                    Assert.AreEqual(1, applied.RemainingTurns); // 임시 지속 턴 확인
                } // 효과 수치 확인 종료
                controller.Dispose(); // 유물 관리자 정리
                events.Dispose(); // 이벤트 발행기 정리
            } // 개별 전투 종료
            Assert.That(activated, Is.InRange(25, 75)); // 임시 10퍼센트 발동 범위 확인
        } // 확률 검사 종료
        finally // 테스트 상태 복구
        { // 복구 시작
            Random.state = oldRandom; // 기존 무작위 상태 복원
            Object.DestroyImmediate(source); // 적 원본 정리
        } // 복구 종료
    } // 검사 본문 종료
} // 검사 종료
