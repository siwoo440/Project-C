using System; // 독립 검사와 임시 유물 정리
using System.Collections; // 실제 카드 사용과 플레이 모드 대기
using NUnit.Framework; // 전투 내 수치와 실제 상태 검증
using UnityEngine; // 실제 유물 로드와 상태 판정 난수
using UnityEngine.TestTools; // Unity의 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 기존 카드와 턴 및 피해 경로 사용
using Object = UnityEngine.Object; // 검사에서 발행한 유니티 객체 정리
using Random = UnityEngine.Random; // 실제 원본 정수 확률의 경계

public sealed class WinterfallFactoryPowerProtocolTests // 고철 공장의 퓨즈와 냉각 밸브 및 봉쇄의 실제 전투 검사
{ // 원본 전투 효과의 세 검사 시작
    [UnityTest] // 적의 실제 사망과 퓨즈의 공유 행동력 범위
    public IEnumerator FuseRewardsOnlyAttributedLiveShockAndUsesExistingActionPointCap() // 적용된 적 감전과 처치 출처 확인
    { // 기존 처치 경로 검증 시작
        yield return new EnterPlayMode(); // 실제 전투와 에셋 초기화
        using (var owned = new OwnedRelic("CentralPowerFuse")) // 중앙 전력 퓨즈의 실제 획득 경로
        using (var fight = new Fight("RUNE_BURST")) // 실제 적 넷과 독립 아군의 전투
        { // 같은 교전의 네 가지 사망 원인 검사 시작
            var actor = fight.Allies[0]; // 첫 감전 적을 실제로 쓰러뜨릴 출전 아군
            var first = fight.Enemies[0]; // 활성 감전과 명시 아군 처치 대상
            var unattributed = fight.Enemies[1]; // 아군 출처를 가진 피해가 없는 감전 적
            var expired = fight.Enemies[2]; // 목록에서 남은 시간이 끝난 감전 적
            var final = fight.Enemies[3]; // 두 번째 실제 출전 아군의 마지막 감전 적 처치
            Assert.IsTrue(fight.Points.Spend(10)); // 공유 행동력일부 소모로 첫 보상 자리 마련
            first.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 2, 1, actor); // 처치 직전에 활성인 원본 감전 등록
            first.TakeDamage(first.MaxHealth * 3, BattleDamageType.None, actor); // 실제 피해 및 사망 알림을 타는 아군 처치
            Assert.IsTrue(first.IsDead); // 첫 대상의 실제 사망 여부
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.Shock)); // 실제로 제거된 원본 감전 목록 확인
            Assert.AreEqual(12, fight.Points.CurrentActionPoints); // 감전 상태가 사라져도 사망 직전 상태로 두 행동력 보상
            unattributed.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 2, 1, actor); // 표식 부여자와 실제 피해 발생자의 독립 대조
            unattributed.TakeDamage(unattributed.MaxHealth * 3, BattleDamageType.None); // 피해 출처 없는 실제 사망
            Assert.AreEqual(12, fight.Points.CurrentActionPoints); // 귀속되지 않은 감전 적 사망의 보상 제외
            expired.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 1, 1, actor); // 만료 상태의 정상 감전 원본
            expired.GetStatusEffect(BattleStatusEffectType.Shock).AdvanceDuration(); // 원래 시간이 끝난 감전 상태 준비
            expired.TakeDamage(expired.MaxHealth * 3, BattleDamageType.None, actor); // 실제 아군 처치지만 유효한 감전 없음
            Assert.AreEqual(12, fight.Points.CurrentActionPoints); // 만료된 감전의 행동력 보상 제외
            Assert.AreEqual(7, fight.Points.Gain(7)); // 실제 행동력 최대치 직전으로 이동
            final.ApplyStatusEffect(BattleStatusEffectType.Shock, 2, 2, 1, actor); // 마지막 적의 피해 전 원래 감전 확인
            final.TakeDamage(final.MaxHealth * 3, BattleDamageType.None, fight.Allies[1]); // 별도의 출전 아군이 실제 마지막 처치
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 승패 확정 전 기존 공용 최대치에서 한 점만 증가
        } // 같은 교전의 네 가지 사망 원인 검사 종료
        yield return new ExitPlayMode(); // 실제 Unity 검사 상태 정리
    } // 기존 처치 경로 검증 종료

    [UnityTest] // 실제 상태 알림과 화상 전환에서 한 번의 밸브 판정
    public IEnumerator ValveRollsOnlyOnFreshAlliedBurnAndReportsInstantCleanse() // 50퍼센트의 실제 상태와 반복 중첩 보존
    { // 일반 화상과 즉시 정화 비교 시작
        yield return new EnterPlayMode(); // 실제 상태와 피해가 연결된 테스트 환경
        using (var owned = new OwnedRelic("CoolantValve")) // 냉각수 밸브의 실제 유물 장착
        using (var fight = new Fight("RUNE_BURST")) // 생존 아군 셋과 적 상태 부여자
        { // 실제 신규 화상의 성공과 실패 처리 검증
            var enemy = fight.Enemies[0]; // 아군 화상의 실제 적 발생자
            var cleansed = fight.Allies[0]; // 정확히 정화될 첫 아군
            var retained = fight.Allies[1]; // 실패 뒤 중첩할 두 번째 아군
            var immune = fight.Allies[2]; // 원래 상태 면역의 아군
            int success = FindSeed(49); // 50퍼센트 확률에서의 마지막 성공 난수
            int failure = FindSeed(50); // 같은 확률에서의 첫 실패 난수
            int retainedOutcome = 0; // 최종 실제 상태 정화 알림 관찰
            cleansed.StatusEffectResolved += (_, _, type, outcome) => // 원래 UI와 반사가 따르는 결과 알림 확인
            { // 실제 원본 상태의 적용 또는 정화 결과
                if (type == BattleStatusEffectType.Burn && outcome == BattleStatusEffectApplyResult.CleansedByRelic) // 즉시 정화한 원본 화상의 최종 결과만 기록
                { // 유물 정화 결과 확인 시작
                    retainedOutcome++; // 정상 원본 정화가 한 번 보고되는지 확인
                } // 유물 정화 결과 확인 종료
            }; // 실제 화상 결과 알림 관찰 종료
            Random.InitState(success); // 성공 범위의 실제 첫 정수 제공
            Assert.AreEqual(BattleStatusEffectApplyResult.CleansedByRelic, cleansed.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, enemy)); // 원래 판정 경로가 즉시 정화 결과 반환
            Assert.IsNull(cleansed.GetStatusEffect(BattleStatusEffectType.Burn)); // 알려지기 전에 실제 화상 전체 제거
            Assert.AreEqual(1, retainedOutcome); // 다른 반사 경로와 공유하는 최종 결과 한 번
            Random.InitState(failure); // 실패 범위의 실제 첫 정수 제공
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, retained.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, enemy)); // 확률 실패의 원래 화상 유지
            Random.InitState(success); // 새 난수 값이면 성공할 다음 일반 중첩을 재현
            Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, retained.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, enemy)); // 이미 있던 활성 화상은 재정화하지 않고 병합
            Assert.AreEqual(49, Random.Range(0, 100)); // 같은 화상의 반복 적용에서 난수 미소모 확인
            Assert.Greater(retained.GetStatusEffect(BattleStatusEffectType.Burn).StackCount, 1); // 기존 화상 중첩 원래 규칙 유지
            var expiring = retained.GetStatusEffect(BattleStatusEffectType.Burn); // 원래 실제 남은 화상을 가져옴
            expiring.AdvanceDuration(); // 아직 활성인 첫 적 턴 지속 감소
            expiring.AdvanceDuration(); // 목록에 남은 실제 만료 상태 생성
            Random.InitState(success); // 새 실제 전환의 성공 경계 제공
            Assert.AreEqual(BattleStatusEffectApplyResult.CleansedByRelic, retained.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, enemy)); // 만료 효과에서 화상으로 되돌아온 새 실제 전환
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 상태 면역 준비
            Random.InitState(success); // 면역 및 적 화상에서 난수 사용 여부 확인
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, immune.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, enemy)); // 원래 상태 면역을 그대로 반영
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, enemy.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, enemy)); // 적 화상에서 아군 밸브 효과 제외
            Assert.AreEqual(49, Random.Range(0, 100)); // 면역과 적 화상에서는 같은 정수 확률 미소모
        } // 실제 신규 화상의 성공과 실패 처리 종료
        yield return new ExitPlayMode(); // 독립 테스트 전투 정리
    } // 일반 화상과 즉시 정화 비교 종료

    [UnityTest] // 추가 아군 턴에서 발동하는 실제 봉쇄 전체 판정
    public IEnumerator LockdownRollsOnceForAllFoesAtThirdActualAllyTurn() // 실제 적 면역과 팀 판정의 적용 범위
    { // 전투의 실제 주기 판정 시작
        yield return new EnterPlayMode(); // Unity 실제 턴 처리 진입
        using (var owned = new OwnedRelic("CentralLockdownProtocol")) // 중앙 봉쇄 프로토콜의 실제 유물 보유
        using (var fight = new Fight(true, "TIME_COIN")) // 선제 적 턴과 실제 추가 아군 턴 원본
        { // 실제 아군 세 차례 검사 시작
            var first = fight.Enemies[0]; // 적용 확인 대상 한 명
            var second = fight.Enemies[1]; // 같은 팀 롤로 적용받을 별도의 적
            var immune = fight.Enemies[2]; // 원래 면역 원본의 대상
            var dead = fight.Enemies[3]; // 실제 쓰러진 적 제외
            dead.TakeDamage(dead.MaxHealth * 3, BattleDamageType.None, fight.Allies[0]); // 공식 출전 아군에 의한 한 적의 실제 죽음
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 먼저 나온 적 턴에 효과 없음
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 첫 아군 턴 진행
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 두 번째 아군 턴 진행
            Assert.AreEqual(2, fight.Turns.CurrentRound); // 선제에 의해 아군 턴이 하나 더 잡히지 않음 확인
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 실제 두 번째 차례까지 봉쇄 없음
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 정상 상태 면역이 살아 있는 기간
            yield return fight.Use("TIME_COIN"); // 추가 아군 차례가 원래 아군 차례를 마친 자리에서 열림
            Random.InitState(FindSuccessThenFailureSeed()); // 한 번 팀 판정 성공과 두 번째 개인 판정 실패가 다른 시드
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 세 번째 아군 턴 시작
            Assert.AreEqual(3, fight.Turns.CurrentRound); // 시간 동전 추가 턴도 같은 실제 전체 주기 반영
            Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.ActionPointDown)?.EffectiveValue); // 첫 생존 대상의 원본 행동력 한 점 약화
            Assert.AreEqual(1, second.GetStatusEffect(BattleStatusEffectType.ActionPointDown)?.EffectiveValue); // 두 번째 생존적에게 동일한 한 번의 팀 결과
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 원래 상태 면역 유지
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 전투불능 대상 제외
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 감전 아닌 실제 행동 약화의 다음 적 차례 진행
            var action = new BattleEnemyAction(first, fight.Allies[0], EnemyActionType.Attack, BattleDamageType.Physical, 15, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "봉쇄 검사", 1, 1, 1, 0); // 기존 적 공격의 원래 예정 행동
            Assert.AreEqual(BattleStatusEffectType.ActionPointDown, action.BlockingStatus); // 정상 전투 표시와 동일한 취소 원인
            Assert.AreEqual(0, action.Execute().DamageResult.AppliedDamage); // 실제 적 행동 한 번의 취소 확인
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 취소한 개별 적에서 실제 상태 소모
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 원래 적 차례의 종료
            Assert.IsNull(second.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 예정 행동을 못 쓴 적의 한 적 턴 뒤 자연 만료
        } // 실제 아군 세 차례 검사 종료
        yield return new ExitPlayMode(); // 별도 전투 검사 정리
    } // 전투의 실제 주기 판정 종료

    private static int FindSeed(int roll) // 원하는 Unity 실제 정수 난수 시드
    { // 원본 경계 확률 검색 시작
        for (int seed = 0; seed < 20000; seed++) // 실제 가능한 정수 초기값 순회
        { // 현재 시드의 첫 정수 확인 시작
            Random.InitState(seed); // 정상 Unity 난수 생성기 재초기화
            if (Random.Range(0, 100) == roll) // 원하는 실제 백분율 확인
            { // 확률 시드 찾기 시작
                return seed; // 체력 및 상태 검사에서 재현할 실제 값
            } // 확률 시드 찾기 종료
        } // 현재 시드의 첫 정수 확인 종료
        throw new InvalidOperationException("실제 화상 경계 난수 없음"); // 원본 난수의 잘못된 독립 테스트 환경
    } // 원본 경계 확률 검색 종료

    private static int FindSuccessThenFailureSeed() // 봉쇄 봉인에 쓸 한 번 성공 후 바로 실패하는 실제 시드
    { // 팀 단일 판정 검증용 시드 찾기 시작
        for (int seed = 0; seed < 20000; seed++) // 같은 실제 원본 Unity 시드 범위
        { // 이번 시드의 두 백분율 확인 시작
            Random.InitState(seed); // 실제 원본 시드 설정
            if (Random.Range(0, 100) == 24 && Random.Range(0, 100) >= 25) // 성공과 다른 실패를 같은 초기값에서 분리
            { // 팀 판정 검증에서 사용할 시드 확인 시작
                return seed; // 실제 성공 경계 한 번과 그 뒤의 별도 실패
            } // 팀 판정 검증에서 사용할 시드 확인 종료
        } // 이번 시드의 두 백분율 확인 종료
        throw new InvalidOperationException("실제 봉쇄 팀 판정용 난수 없음"); // 원래 Unity 런타임과 다른 검사 오류
    } // 팀 단일 판정 검증용 시드 찾기 종료

    private sealed class OwnedRelic : IDisposable // 원래 실제 획득과 테스트의 보유 상태 복구
    { // 테스트 소유 유물 등록 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 이전에 있던 실제 회차 관리 객체
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 현재 테스트의 원래 유물 획득 경로
        private readonly RelicData relic; // 이번 검사에서 얻은 실제 원본

        public OwnedRelic(string key) // 해당 Unity 리소스의 실제 소유 확보
        { // 실제 유물 로드 시작
            relic = Resources.Load<RelicData>("Relics/" + key); // 상점과 같은 실제 유물 원본 확인
            Assert.IsNotNull(relic, key); // 실제 신규 에셋 연결 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 현재 인벤토리에서 정상 유물 등록
        } // 실제 유물 로드 종료

        public void Dispose() // 실제 테스트에서 별도로 만든 보유 상태 정리
        { // 검사 실행 후 이전 유물 상태 복구 시작
            manager.TryRemove(relic.RelicId); // 이 실제 검사에서 얻은 유물만 제거
            if (previous == null) // 실제 전에 없던 관리 객체만 정리
            { // 새 임시 객체 제거 시작
                Object.Destroy(manager.gameObject); // 독립 Unity 객체 해제
            } // 새 임시 객체 제거 종료
        } // 검사 실행 후 이전 유물 상태 복구 종료
    } // 테스트 소유 유물 등록 종료
} // 원본 전투 효과의 세 검사 종료
