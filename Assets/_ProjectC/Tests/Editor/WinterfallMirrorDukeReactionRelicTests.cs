using System; // 실제 전투 유물 정리와 시드 오류
using System.Collections; // 격리 전투 코루틴
using System.Collections.Generic; // 실제 장착 유물 추적
using NUnit.Framework; // 피해와 확률 상태 검증
using UnityEngine; // 실제 전투 리소스 조회
using UnityEngine.TestTools; // 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 등록 카드와 실제 전투 환경
using Object = UnityEngine.Object; // 임시 유물 보관함 정리
using Random = UnityEngine.Random; // 정수 반사 확률 재현

public sealed class WinterfallMirrorDukeReactionRelicTests // 두 유물의 실제 전투 반응 검사
{ // 거울 공작 유물 검사 시작
    [UnityTest] // 기존 두 거울 이후 공작 왕관의 실제 부여 검사
    public IEnumerator MirrorCrownUsesExistingPriorityAndCopiesTheOriginalDebuffOnce() // 정확한 확률 횟수와 원본 상태 검증
    { // 세 거울의 실제 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 상태 및 보유 유물 실행
        CheckMirrorCrown(); // 기존 우선순위와 새 반사 확인
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 세 거울의 실제 플레이 모드 종료

    private static void CheckMirrorCrown() // 기존 반사와 새 복사 경로 확인
    { // 실제 세 유물 및 대상 준비 시작
        using var owned = new OwnedRelics("CloudedMirrorNail", "LaughBehindMirror", "BrokenCrownOfMirrorDuke"); // 원본 세 거울 동시 장착
        using var fight = new Fight("RUNE_BURST"); // 실제 살아 있는 아군과 적 전투 준비
        var ally = fight.Allies[0]; // 실제 약화를 받는 생존 아군
        var immune = fight.Allies[1]; // 원래 약화 면역의 대조 수신자
        var enemy = fight.Enemies[0]; // 실제로 약화를 부여한 생존 적
        var first = FindRolls((20, true)); // 처음 보유 흐린 거울못의 성공만 사용
        Random.InitState(first.Seed); // 실제 약화 직전 성공 확률 재현
        ally.ApplyStatusEffect(BattleStatusEffectType.AttackPowerDown, 1, 2, 1, enemy); // 기존 거울못의 최초 적 약화 반사
        Assert.IsNotNull(enemy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 실제 첫 우선순위의 적 반사
        Assert.AreEqual(first.After, Random.state); // 최초 성공 뒤 다른 유물의 추가 굴림 없음
        var second = FindRolls((40, true)); // 웃는 거울 성공 시 왕관 굴림 제외
        Random.InitState(second.Seed); // 실제 둘째 적 약화의 확률 재현
        ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 2, 2, 1, enemy); // 전투당 거울못 소모 뒤 새 약화
        Assert.AreEqual(2, enemy.GetStatusEffect(BattleStatusEffectType.Blind)?.EffectiveValue); // 원본 웃는 거울의 다음 우선순위
        Assert.AreEqual(second.After, Random.state); // 둘째 성공 뒤 왕관 난수 없음
        ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 3); // 적 발생자 없는 초기 화상 원본
        ally.GetStatusEffect(BattleStatusEffectType.Burn).DecayBurn(); // 실제 먼저 진행된 화상 감쇠
        var crown = FindRolls((40, false), (50, true)); // 웃는 거울 실패 뒤 왕관이 성공하는 독립 확률
        Random.InitState(crown.Seed); // 같은 실제 적의 화상 갱신 전 확률 재현
        Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 3, enemy)); // 실제 남아 있는 화상의 새 중첩 부여
        var original = ally.GetStatusEffect(BattleStatusEffectType.Burn); // 활성 원본 화상과 실제 감쇠 자료
        var reflected = enemy.GetStatusEffect(BattleStatusEffectType.Burn); // 실제 왕관의 독립 복사
        Assert.IsNotNull(reflected); // 성공한 왕관이 실제 원래 적에게 부여한 화상
        Assert.AreNotSame(original, reflected); // 원본 아군과 별개인 상태 개체
        Assert.AreEqual(original.StackCount, reflected.StackCount); // 원래 중첩수 보존
        Assert.AreEqual(original.RemainingTurns, reflected.RemainingTurns); // 현재 실제 지속 기간 보존
        Assert.AreEqual(original.EffectiveValue, reflected.EffectiveValue); // 이미 적용한 화상 감소까지 보존
        Assert.AreEqual(crown.After, Random.state); // 성공 후보까지 실제 난수 두 번만 소모
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원본 약화를 막는 일반 면역
        var beforeImmune = Random.state; // 차단 결과의 난수 대조
        Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, immune.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1, enemy)); // 실제로 부여되지 않은 약화
        Assert.AreEqual(beforeImmune, Random.state); // 원래 아군 면역 뒤 반사 굴림 없음
    } // 실제 세 유물 및 대상 준비 종료

    [UnityTest] // 같은 적 턴의 실제 회피 합계와 새 적 턴 초기화 검사
    public IEnumerator BallroomLastBeatCountsOnlyTrueEvadesAcrossAlliesAndResetsAfterEnemyTurn() // 실명 빗나감과 두 번째 회피 비교
    { // 실제 적 턴 실행 시작
        yield return new EnterPlayMode(); // 현재 적 전투 효과 실행
        CheckBallroomCounter(); // 전투 안의 회피 집계 및 초기화 대조
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 실제 적 턴 실행 종료

    private static void CheckBallroomCounter() // 회피 원인과 실제 피해 및 한 턴 집계 검사
    { // 실제 아군 회피 환경 준비 시작
        using var owned = new OwnedRelics("LastBeatOfBallroom"); // 등록된 마지막 박자 유물 장착
        using var fight = new Fight("RUNE_BURST"); // 파티와 여러 적의 실제 전투 구성
        var first = fight.Allies[0]; // 같은 턴에 먼저 회피할 아군
        var second = fight.Allies[1]; // 다음으로 회피해서 유물을 발동할 다른 아군
        var one = fight.Enemies[0]; // 방어도를 가진 첫 공격 적
        var two = fight.Enemies[1]; // 두 번째 공격을 담당할 적
        first.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 8, 1); // 대상 자체 회피의 확정 적용
        second.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 8, 1); // 다른 아군 대상 자체 회피의 확정 적용
        Assert.IsTrue(Attack(one, first).Execute().WasMissed); // 아군 턴에는 피해를 피하더라도 적 턴 횟수 미집계
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 적 턴 진입
        one.GainBlock(5); // 일반 피해가 통과할 기존 방어도 다섯
        one.ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 2, 1); // 대상과 관계없이 적이 항상 빗나갈 조건
        Assert.IsTrue(Attack(one, first).Execute().WasMissed); // 실명만으로도 실패하는 원래 공격
        one.RemoveStatusEffect(BattleStatusEffectType.Blind); // 다음 공격의 실제 대상 회피만 남김
        Assert.IsTrue(Attack(one, first).Execute().WasMissed); // 첫 실제 회피
        Assert.AreEqual(200, two.CurrentHealth); // 실명 빗나감을 포함한 첫 실패 두 건의 유물 피해 없음
        Assert.IsTrue(Attack(two, second).Execute().WasMissed); // 다른 아군의 두 번째 실제 회피로 광역 일반 피해
        Assert.AreEqual(193, one.CurrentHealth); // 일반 피해 열둘에서 실제 방어도 다섯 차감
        Assert.AreEqual(188, two.CurrentHealth); // 방어 없는 다른 적의 실제 열둘 피해
        Assert.AreEqual(188, fight.Enemies[2].CurrentHealth); // 공격하지 않았어도 같은 생존 명단의 전체 피해
        Assert.IsTrue(Attack(two, second).Execute().WasMissed); // 같은 적 턴의 세 번째 실제 회피
        Assert.AreEqual(188, two.CurrentHealth); // 한 적 턴에 한 번만 발동
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 현재 적 턴 완료와 다음 아군 턴 전환
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 다음 실제 적 턴에 회피 집계 초기화
        Assert.IsTrue(Attack(one, first).Execute().WasMissed); // 새 적 턴의 첫 회피
        Assert.AreEqual(188, two.CurrentHealth); // 새 적 턴에도 첫 회피에서는 대기
        Assert.IsTrue(Attack(two, second).Execute().WasMissed); // 새 적 턴의 두 번째 회피
        Assert.AreEqual(176, two.CurrentHealth); // 새 적 턴에 한 번 더 받은 일반 피해
    } // 실제 아군 회피 환경 준비 종료

    [UnityTest] // 회피 반응에서 실제 쓰러진 광역 공격자 검사
    public IEnumerator BallroomLastBeatCanDefeatTheAreaAttackerBeforeAnotherAllyIsHit() // 앞선 두 회피와 뒤 아군 공격 취소 확인
    { // 실제 광역 적 행동 검사 시작
        yield return new EnterPlayMode(); // 피해와 처치가 연결된 실제 전투 환경
        using (var owned = new OwnedRelics("LastBeatOfBallroom")) // 실제 보유 유물 등록
        using (var fight = new Fight("RUNE_BURST")) // 독립된 실제 세 아군 전투
        { // 실제 광역 행동 준비 시작
            var enemy = fight.Enemies[0]; // 반격을 버티지 못할 광역 공격자
            enemy.TakeDamage(194, BattleDamageType.None); // 살아 있지만 유물 한 번에 쓰러지는 체력 여섯
            fight.Allies[0].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 8, 1); // 첫 대상의 확정 회피
            fight.Allies[1].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 8, 1); // 둘째 대상의 확정 회피
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴에서 광역을 시도하는 진영 조건
            var area = Attack(enemy, fight.Allies[0], EnemyTargetRule.AllLiving); // 실제 생존 아군 전체를 노리는 적 행동
            area.SetAreaTargets(fight.Allies); // 기존 적 전투 계획의 대상 순서 설정
            var unhit = fight.Allies[2]; // 유물로 인해 공격이 닿기 전에 남는 세 번째 아군
            int health = unhit.CurrentHealth; // 세 번째 아군의 원래 실제 체력
            area.Execute(); // 첫 두 회피를 포함한 실제 광역 적 행동 실행
            Assert.IsTrue(enemy.IsDead); // 실제 광역 공격자의 첫 유물 피해 처치
            Assert.AreEqual(health, unhit.CurrentHealth); // 쓰러진 공격자가 남은 적 공격 피해를 주지 않는 확인
            Assert.AreEqual(188, fight.Enemies[1].CurrentHealth); // 공격자 사망과 별개로 적용된 다른 생존 적 피해
        } // 실제 광역 행동 준비 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 실제 광역 적 행동 검사 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime enemy, BattleUnitRuntime ally, EnemyTargetRule target = EnemyTargetRule.FirstLiving) // 같은 전투 난수로 셈하는 실제 적 공격 생성
    { // 단일 적 행동 준비 시작
        return new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, 10, BattleStatusEffectType.None, 1, 1, target, "회피 검사", 1, 1, 5, 0, percentRoll: () => 50, battleType: BattleType.Normal); // 양수 공격과 실제 회피 구분에 사용할 같은 난수
    } // 단일 적 행동 준비 종료

    private static (int Seed, Random.State After) FindRolls(params (int Chance, bool Passed)[] expected) // 실제 한 번 또는 두 번의 유물 굴림을 재현할 상태
    { // 실제 난수 후보 조회 시작
        for (int seed = 0; seed < 10000; seed++) // 제한된 실제 전투 난수 후보 비교
        { // 후보별 실제 굴림 검사 시작
            Random.InitState(seed); // 현재 후보의 실제 시작 난수 초기화
            bool matches = true; // 요청한 순서와 모두 같은지 기록
            foreach (var roll in expected) // 유물의 실제 시도 순서 조회
            { // 지정한 확률과 난수 비교 시작
                matches &= (Random.Range(0, 100) < roll.Chance) == roll.Passed; // 지금 굴림의 성공 여부 확인
            } // 지정한 확률과 난수 비교 종료
            if (matches) // 실제 필요한 모든 순서의 일치 확인
            { // 재현 가능한 최초 후보 선택 시작
                return (seed, Random.state); // 사건 전 시작점과 정확한 종료 난수 상태
            } // 재현 가능한 최초 후보 선택 종료
        } // 후보별 실제 굴림 검사 종료
        throw new InvalidOperationException("No matching reflection rolls found."); // 제한된 후보의 실제 확률 재현 실패
    } // 실제 난수 후보 조회 종료

    private sealed class OwnedRelics : IDisposable // 실제 회차의 유물 획득과 정리
    { // 임시 장착 범위 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 검사 이전 실제 회차 보관함
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 검사에 사용할 실제 회차 유물 관리자
        private readonly List<RelicData> acquired = new List<RelicData>(); // 이 공간에서 실제 획득한 유물

        internal OwnedRelics(params string[] names) // 지정한 정식 원본의 실제 장착
        { // 유물 장착 시작
            foreach (string name in names) // 요청한 실제 유물 리소스 조회
            { // 현재 유물 원본 장착 시작
                var relic = Resources.Load<RelicData>("Relics/" + name); // 획득시키는 실제 등록 자료
                Assert.IsNotNull(relic, name); // 정식 리소스로 불러온 유물 확인
                Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 보유 목록의 원본 추가 확인
                acquired.Add(relic); // 검사 완료 뒤 삭제할 실제 참조 기록
            } // 현재 유물 원본 장착 종료
        } // 유물 장착 종료

        public void Dispose() // 이전 보유 상태로 되돌리는 검사 정리
        { // 장착한 원본 정리 시작
            foreach (var relic in acquired) // 이 검사에서 획득한 참조 확인
            { // 현재 유물 실제 제거 시작
                manager.TryRemove(relic.RelicId); // 검사 대상 장착만 제거
            } // 현재 유물 실제 제거 종료
            if (previous == null) // 검사 시작 시 기존 보관함이 없던 경우
            { // 이 검사에서 만든 관리자 정리 시작
                Object.Destroy(manager.gameObject); // 임시 회차 오브젝트 해제
            } // 이 검사에서 만든 관리자 정리 종료
        } // 장착한 원본 정리 종료
    } // 임시 장착 범위 종료
} // 거울 공작 유물 검사 종료
