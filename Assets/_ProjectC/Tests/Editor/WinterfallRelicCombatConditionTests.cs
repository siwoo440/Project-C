using System; // 실제 보유 유물과 전투 객체 해제
using System.Collections; // Unity 플레이 모드와 실제 카드 사용 대기
using System.Linq; // 적의 실제 전체 체력 손실 합산
using NUnit.Framework; // 실제 판정과 피해 예고 검증
using UnityEngine; // 정식 유물 원본과 적 생성을 위한 Unity 자료
using UnityEngine.TestTools; // 격리 플레이 모드의 실제 전투 검사
using Fight = WinterfallSwordCombatTests.Fight; // 일반 카드 입력과 적 조우의 공통 검사 환경
using Object = UnityEngine.Object; // 임시 원본의 Unity 정리
using Random = UnityEngine.Random; // 원하는 경계의 실제 전투 확률 재현

public sealed class WinterfallRelicCombatConditionTests // 회피와 감전 및 기계 공격과 표식 명중 회귀
{ // 기본 전투 유물 검사 시작
    [UnityTest] // 정상 적의 실제 회피 사건과 고정 일반 피해 확인
    public IEnumerator StormRingRewardsEachActualEvasionOnce() // 실제 회피만 적 하나에게 피해 여섯 적용
    { // 회전 고리 검사 시작
        yield return new EnterPlayMode(); // 실제 적 행동과 턴 이벤트를 사용하는 환경 진입
        using (var owned = new OwnedRelic("StormKingSpinningRing")) // 원본 회전 고리의 실제 유물 보유
        using (var fight = new Fight("THRUST")) // 실제 살아 있는 아군과 일반 적 구성
        { // 유효 회피와 일반 빗나감 대조 시작
            var ally = fight.Allies[0]; // 원래 적 공격을 받을 실제 아군
            var attacker = fight.Enemies[0]; // 같은 명중 굴림을 쓰는 정상 적 공격자
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 공격을 처리할 적 턴에 진입
            attacker.ApplyStatusEffect(BattleStatusEffectType.Blind, 20, 3, 1); // 적 자체 실패 구간의 실제 실명
            ally.ApplyStatusEffect(BattleStatusEffectType.Evasion, 30, 3, 1); // 실제 명중에서 회피로 변하는 난수 구간
            int total = fight.Enemies.Sum(enemy => enemy.CurrentHealth); // 회피 전 후보 전체의 체력 합
            Assert.IsTrue(Attack(attacker, ally, 9, 80).Execute().WasMissed); // 적 자체의 실명만으로 빗나간 공격 확인
            Assert.AreEqual(total, fight.Enemies.Sum(enemy => enemy.CurrentHealth)); // 적 자체 빗나감에는 보상 없음 확인
            ally.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 회피와 구별되는 기존 피해 무효 적용
            Assert.IsTrue(Attack(attacker, ally, 9, 49).Execute().DamageResult.WasDamageNullified); // 회피가 아닌 원래 명중과 피해 무효 확인
            Assert.AreEqual(total, fight.Enemies.Sum(enemy => enemy.CurrentHealth)); // 실제 명중의 피해 무효에는 보상 없음 확인
            for (int index = 1; index <= 3; index++) // 한 적 턴의 세 진짜 회피를 순서대로 확인
            { // 개별 회피의 실제 피해 정산 시작
                Assert.IsTrue(Attack(attacker, ally, 9, 50).Execute().WasMissed); // 동일 원본 굴림으로 회피 경계의 정상 공격 확인
                Assert.AreEqual(total - 6 * index, fight.Enemies.Sum(enemy => enemy.CurrentHealth)); // 무도회 이회 제한과 별개인 실제 회피당 피해 여섯
            } // 개별 회피의 실제 피해 정산 종료
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.CurrentHealth >= 182)); // 실제 회피 횟수보다 많은 대상 피해 없음 확인
        } // 유효 회피와 일반 빗나감 대조 종료
        yield return new ExitPlayMode(); // 검사 전투와 유물 해제
    } // 회전 고리 검사 종료

    [UnityTest] // 실제 정전기 명중 뒤 원본과 보정 확률의 경계 확인
    public IEnumerator CopperWireAddsExactlyFivePointsToTheNativeShockRoll() // 감전 가능 구간의 하한과 상한 비교
    { // 실제 정전기 확률 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 카드의 공격과 상태 정산 준비
        using (var owned = new OwnedRelic("BrokenCopperWire")) // 현재 회차의 구리선 원본 획득
        using (var fight = new Fight("STATIC_CHARGE", "STATIC_CHARGE", "STATIC_CHARGE")) // 실제 같은 정전기 원본으로 성공과 실패 대조
        { // 기존 확률 상태 성공과 실패 준비 시작
            int baseChance = fight.View("STATIC_CHARGE").RuntimeCard.SourceData.WandHitStatusChancePercent; // 게임 카드의 현재 실제 확률 조회
            Assert.Greater(baseChance, 0); // 실제 감전 시도가 있는 원본 카드 확인
            Assert.Less(baseChance + 5, 100); // 두 결과 경계를 비교할 확률 카드 확인
            int seed = FindSeed(baseChance + 4); // 원본은 실패하고 구리선 적용으로 성공하는 마지막 난수
            fight.Controller.CardUsed += (_, __) => Random.InitState(seed); // 기존 명중 상태 판정 직전의 재현 난수 설정
            yield return fight.Use("STATIC_CHARGE", 0); // 정확한 성공 경계에서 실제 정전기 명중
            Assert.IsNotNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Shock)); // 함부로 올림 없이 다섯 퍼센트포인트만 반영
            seed = FindSeed(baseChance + 5); // 유물까지 적용해도 정상적으로 실패할 첫 난수
            yield return fight.Use("STATIC_CHARGE", 1); // 정확한 실패 경계에서 실제 정전기 명중
            Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Shock)); // 기준 이상에서 원래 상태 실패 확인
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 1, 1); // 기존 감전 면역을 가진 살아 있는 적
            seed = FindSeed(baseChance + 4); // 면역이 없다면 감전될 성공 난수
            yield return fight.Use("STATIC_CHARGE", 2); // 실제 상태 면역을 가진 적에게 정전기 명중
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Shock)); // 기존 상태 면역 보존 확인
        } // 기존 확률 상태 성공과 실패 준비 종료
        yield return new ExitPlayMode(); // 검사 전투와 유물 해제
    } // 실제 정전기 확률 검사 종료

    [UnityTest] // 적 직접 피해를 계산하는 실제 예고와 방어도 및 체력 확인
    public IEnumerator ScrewBundleMatchesMechanicalDirectPreviewAndRealHit() // 기계 분류와 일반 피해의 대조
    { // 기계형 방어 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 적 원본과 플레이 모드의 유물 보관함 활성화
        using (var owned = new OwnedRelic("RustedScrewBundle")) // 나사 묶음 원본의 실제 보유
        { // 실제 적 원본 두 분류 준비 시작
            var mechanical = MakeEnemy("SCREW_MECHANICAL", true); // 기계형으로 명시한 실제 EnemyData
            var organic = MakeEnemy("SCREW_ORGANIC", false); // 같은 능력치의 기계형이 아닌 실제 EnemyData
            try // 현재 검사에서 만든 두 적 원본의 해제 보장
            { // 두 분류의 피해와 도달 경계 확인 시작
                using var fight = Fight.WithEnemySources(new[] { mechanical, organic }, "THRUST"); // 동일한 검사 전투의 두 실제 공격자
                var ally = fight.Allies[0]; // 직접 맞을 살아 있는 피해 대상
                var source = fight.Enemies[0]; // 실제 기계형 공격 적
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 정상 적 공격을 실행할 실제 적 턴 진입
                ally.GainBlock(3); // 기계 피해 경감 뒤 소모할 독립 방어도
                var regular = ally.PreviewDamage(20, BattleDamageType.Physical, fight.Enemies[1], enemyDirectAttack: true); // 일반 적의 동일한 직접 피해 예고
                var reduced = ally.PreviewDamage(20, BattleDamageType.Physical, source, enemyDirectAttack: true); // 기계형의 동일한 실제 예고
                Assert.AreEqual(regular.AppliedDamage - 2, reduced.AppliedDamage); // 방어도 이전 계산에서 나사 묶음이 차감한 실제 두 점
                Assert.AreEqual(3, ally.CurrentBlock); // 순수 피해 예고가 원래 방어도를 그대로 두는지 확인
                var real = Attack(source, ally, 20, 0).Execute().DamageResult; // 원래 적의 실제 물리 타격 실행
                Assert.AreEqual(reduced.AppliedDamage, real.AppliedDamage); // 실제 카드가 아닌 적 직접 공격의 예고와 체력 피해 일치
                Assert.AreEqual(0, ally.CurrentBlock); // 남은 직접 피해가 원래 방어도를 소모한 결과
                Assert.AreEqual(3, ally.TakeDamage(3, BattleDamageType.None, source).AppliedDamage); // 같은 기계가 준 일반 피해에는 차감 없음 확인
                Assert.AreEqual(0, ally.PreviewDamage(1, BattleDamageType.None, source, enemyDirectAttack: true).AppliedDamage); // 저점 직접 피해의 기존 영 하한 유지
            } // 두 분류의 피해와 도달 경계 확인 종료
            finally // 검사 적 원본의 확실한 제거
            { // 실제 자료 해제 시작
                Object.Destroy(mechanical); // 현재 검사에서만 쓴 기계형 원본 삭제
                Object.Destroy(organic); // 현재 검사에서만 쓴 일반형 원본 삭제
            } // 실제 자료 해제 종료
        } // 실제 적 원본 두 분류 준비 종료
        yield return new ExitPlayMode(); // 검사 전투와 유물 해제
    } // 기계형 방어 유물 검사 종료

    [UnityTest] // 적 표식과 공유 명중 계산의 실제 확률 확인
    public IEnumerator LensCapRecognizesActiveNativeMarksOnly() // 각 표식에서 예고 백분율과 실제 굴림 경계 확인
    { // 활성 표식의 명중 검사 시작
        yield return new EnterPlayMode(); // 표식과 실제 전투 유물 보관함 준비
        using (var owned = new OwnedRelic("BrokenLensCap")) // 깨진 렌즈캡의 정식 원본 장착
        using (var fight = new Fight("THRUST")) // 생존 아군과 적 네 명의 실제 전투
        { // 각 표식과 원본 회피의 대조 시작
            var ally = fight.Allies[0]; // 표식 적을 공격하는 원래 아군
            var target = fight.Enemies[0]; // 각 실제 표식을 번갈아 받을 살아 있는 적
            target.ApplyStatusEffect(BattleStatusEffectType.Evasion, 25, 4, 1); // 백 퍼센트 상한에 가려지지 않을 실제 회피
            Assert.AreEqual(75, BattleHitChance.GetPercent(ally, target)); // 표식 없는 원래의 명중률
            foreach (var mark in new[] { BattleStatusEffectType.PublicMark, BattleStatusEffectType.ChariotMark, BattleStatusEffectType.StethoscopeMark, BattleStatusEffectType.DuelMark }) // 기존 전투가 사용하는 네 이름 붙은 적 표식
            { // 실제 개별 표식의 명중 계산 확인 시작
                target.ApplyStatusEffect(mark, 1, 2, 1, ally); // 현재 적의 유효 표식 부여
                Assert.AreEqual(85, BattleHitChance.GetPercent(ally, target), mark.ToString()); // 공유 예고의 정확한 열 퍼센트포인트 증가
                Assert.IsTrue(BattleHitChance.Hits(ally, 84, target)); // 같은 계산을 쓰는 실제 마지막 성공 굴림
                Assert.IsFalse(BattleHitChance.Hits(ally, 85, target)); // 정확히 다음 굴림은 실패 확인
                Assert.AreEqual(75, BattleHitChance.GetPercent(fight.Enemies[1], target)); // 아군이 아닌 공격자에게 보정 없음 확인
                target.RemoveStatusEffect(mark); // 다음 표식의 독립 조건을 위해 현재 표식 제거
            } // 실제 개별 표식의 명중 계산 확인 종료
            target.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 1, 1, ally); // 상태 저장에 잠시 남을 만료 표식 설정
            target.GetStatusEffect(BattleStatusEffectType.PublicMark).AdvanceDuration(); // 제거 처리 전 실제 기간을 모두 소모
            Assert.AreEqual(75, BattleHitChance.GetPercent(ally, target)); // 만료 값이 현재 명중을 올리지 않는지 확인
        } // 각 표식과 원본 회피의 대조 종료
        yield return new ExitPlayMode(); // 검사 전투와 유물 해제
    } // 활성 표식의 명중 검사 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime source, BattleUnitRuntime target, int damage, int roll) // 실제 적 행동의 원래 물리 피해 생성
    { // 재현할 직접 타격 생성 시작
        return new BattleEnemyAction(source, target, EnemyActionType.Attack, BattleDamageType.Physical, damage, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "전투 유물 검사", 1, 1, 1, 0, percentRoll: () => roll); // 정해진 굴림의 실제 적 공격 결과
    } // 재현할 직접 타격 생성 종료

    private static EnemyData MakeEnemy(string id, bool mechanical) // 기존 데이터 필드의 실제 분류 원본 준비
    { // 임시 적 원본 기록 시작
        var enemy = ScriptableObject.CreateInstance<EnemyData>(); // 실제 기계 분류가 있는 적 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"enemyId\":\"{id}\",\"maxHealth\":200,\"isMechanical\":{(mechanical ? "true" : "false")}}}", enemy); // 분류 외 조건이 동일한 적 데이터 설정
        return enemy; // 실제 전투에 전달할 적 원본 반환
    } // 임시 적 원본 기록 종료

    private static int FindSeed(int roll) // Unity 기존 정수 난수의 정확한 경계 찾기
    { // 현재 재현 난수 준비 시작
        for (int seed = 0; seed < 10000; seed++) // 유한한 Unity 난수 시드 후보 비교
        { // 후보의 첫 기존 확률 판정 시작
            Random.InitState(seed); // 검사할 현재 원래 시드 설정
            if (Random.Range(0, 100) == roll) // 필요한 실제 백분율 정수 굴림 확인
            { // 재현 가능한 기존 시드 반환 시작
                return seed; // 현재 실제 카드 판정에 사용할 시드
            } // 재현 가능한 기존 시드 반환 종료
        } // 후보의 첫 기존 확률 판정 종료
        throw new InvalidOperationException("정전기 검사에 필요한 난수 미발견"); // 검사 경계 재현 실패
    } // 현재 재현 난수 준비 종료

    private sealed class OwnedRelic : IDisposable // 실제 자산을 쓰는 검사마다 전투 인벤토리 복원
    { // 임시 유물 보유 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 원래 존재한 회차 관리자 확인
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 현재 실제 유물 인벤토리 관리자
        private readonly RelicData relic; // 현재 검사에서 등록한 정식 자산
        internal OwnedRelic(string key) // 실제 게임 유물 리소스 획득
        { // 원본 유물 보유 준비 시작
            relic = Resources.Load<RelicData>("Relics/" + key); // 시트에 연결된 유물 원본 조회
            Assert.IsNotNull(relic, key); // 실제 유물 원본 등록 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 현재 검사 회차의 정상 유물 획득
        } // 원본 유물 보유 준비 종료
        public void Dispose() // 현재 검사에서 쓴 실제 회차 자료 복원
        { // 독립 유물 해제 시작
            manager.TryRemove(relic.RelicId); // 현재 검사에서 직접 얻은 유물만 제거
            if (previous == null) // 원래 회차 관리자가 없는 경우 확인
            { // 새로 생성한 실제 관리자 해제 시작
                Object.Destroy(manager.gameObject); // 검사 중 설치한 유물 인벤토리 제거
            } // 새로 생성한 실제 관리자 해제 종료
        } // 독립 유물 해제 종료
    } // 임시 유물 보유 종료
} // 기본 전투 유물 검사 종료
