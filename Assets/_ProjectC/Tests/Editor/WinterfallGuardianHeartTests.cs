using System.Collections; // 실제 카드 사용과 적 턴 대기
using System.Linq; // 지정 아군 화면과 결과 조회
using System.Reflection; // 실제 피해 분담과 광역 대상 연결 호출
using NUnit.Framework; // 수호 발동 조건과 피해 전환 검증
using UnityEngine.TestTools; // 실제 전투 카드 클릭 환경
using Fight = WinterfallSwordCombatTests.Fight; // 전투 화면과 상태 처리 재사용

public sealed class WinterfallGuardianHeartTests // 수호자의 심장 치명 피해 대체 검사
{ // 수호자의 심장 검사 시작
    [UnityTest] // 치명 판정 전 기존 피해 방지와 대신 받기 검증
    public IEnumerator GuardianHeartTransfersOnlyTheLethalResolvedHit() // 원래 아군 보존과 실제 수호자 피해 확인
    { // 치명 피해 대체 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 적 행동 실행 환경
        yield return GuardianHeartTransfersOnlyTheLethalResolvedHitInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 치명 피해 대체 검사 종료

    private IEnumerator GuardianHeartTransfersOnlyTheLethalResolvedHitInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("GUARDIAN_HEART")) // 정식 보상 카드 한 장을 가진 전투
        { // 보호 발동 검사 시작
            var protector = fight.Allies[0]; // 피해를 대신 받을 실제 시전자
            var target = fight.Allies[1]; // 치명 피해를 막아 줄 지정 아군
            var other = fight.Allies[2]; // 원래 분담자와 지정 외 아군 확인 대상
            var enemy = fight.Enemies[0]; // 동일한 직접 공격의 실제 적 원본
            yield return Mark(fight, target); // 실제 카드 선택과 아군 클릭으로 보호 연결
            Assert.IsNull(protector.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 시전자 자신에게 대상 표식 미적용 확인
            Assert.IsNull(other.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 지정하지 않은 아군 보호 미적용 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 적 턴을 공격 없이 경과
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴을 공격 없이 경과
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 기간 대신 실제 한 타격을 기다리는 보호 확인
            target.ApplyPersistentHealth(8); // 원래 아군의 치명 여부 비교 체력
            target.GainBlock(4); // 피해 분담과 일반 방어로 생존 가능한 방어도
            target.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 모든 적 직접 타격을 회피하는 대조 상태
            Assert.IsTrue(Attack(enemy, target, 140).Execute().WasMissed); // 치명적인 원본 타격도 실제 회피하면 발동하지 않음 확인
            target.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 실제 명중 경로 복원
            target.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 먼저 발동할 기존 피해 무효화
            var canceled = Attack(enemy, target, 140).Execute(); // 기존 방어 효과가 적 타격을 먼저 막는 경우
            Assert.IsTrue(canceled.DamageResult.WasDamageNullified); // 기존 피해 무효가 수호자보다 먼저 처리됨 확인
            Assert.AreEqual(4, target.CurrentBlock); // 무효 피해의 기존 방어도 보존 확인
            var shared = Attack(enemy, target, 12); // 방어도와 분담 뒤 치명적이지 않은 직접 피해
            Assert.IsTrue(AssignShare(shared, other)); // 실제 배분할 제삼 아군 연결
            Assert.Greater(shared.Execute().DamageResult.AppliedDamage, 0); // 직접 명중으로 체력 피해가 있었다는 확인
            Assert.IsFalse(target.IsDead); // 분담 후 기존 대상 생존 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 비치명 실제 체력 손실로 보호 미소모 확인
            target.ApplyPersistentHealth(8); // 기존 치명 피해 방지의 비교 체력 복원
            target.ApplyStatusEffect(BattleStatusEffectType.UndyingGuard, 1, 2, 1); // 다음 한 번의 치명 피해를 먼저 버티는 기존 상태
            Assert.AreEqual(7, Attack(enemy, target, 140).Execute().DamageResult.AppliedDamage); // 기존 불굴로 보호 대상 체력 한 점 생존 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 기존 불굴이 살린 타격으로 수호자 미소모 확인
            target.ApplyPersistentHealth(8); // 실제 치명 타격 검증 체력 준비
            target.GainBlock(4); // 대신 맞는 타격에서 보호할 기존 방어도
            target.ApplyStatusEffect(BattleStatusEffectType.GuardPreparation, 2, 1, 1); // 보호 발동으로 보존할 대상의 방어 준비
            target.ApplyStatusEffect(BattleStatusEffectType.DangerInterception, 70, 1, 1); // 치명 판단에 반영하되 보존할 일회 경감
            protector.ApplyPersistentHealth(12); // 대신 받으면 체력 최소 한 점이 드러날 시전자 상태
            protector.GainBlock(5); // 대신 받은 실제 타격에서 사용할 시전자 방어도
            int otherHealth = other.CurrentHealth; // 취소할 원래 타격의 분담자 결과 비교
            int hitEvents = 0; // 실제 보호받은 아군의 거짓 피해 알림 계수
            int defeatEvents = 0; // 실제 보호받은 아군의 거짓 처치 알림 계수
            int guardianHits = 0; // 재공격 없이 대신 받은 실제 공격 횟수
            int reductionChecks = 0; // 원래 대상과 시전자 간 공용 경감 중복 소비 검사
            int ReduceOnce(int damage) // 실제 파티 공용 경감의 호출 횟수와 값 기록
            { // 한 번의 피해 경감 시작
                reductionChecks++; // 먼저 적용한 실제 경감 횟수 누적
                return damage - 1; // 장화와 같은 고정 체력 피해 한 점 경감
            } // 한 번의 피해 경감 종료
            target.DamageTaken += (_, _) => hitEvents++; // 가려진 아군의 피해 이벤트 기록
            target.Defeated += (_, _) => defeatEvents++; // 가려진 아군의 처치 이벤트 기록
            protector.DamageTaken += (_, _) => guardianHits++; // 실제 수호자의 피격 이벤트 기록
            var lethal = Attack(enemy, target, 140, status: BattleStatusEffectType.Bleed); // 남은 방어와 분담 이후에도 치명적인 직접 공격
            Assert.IsTrue(AssignShare(lethal, other)); // 원래 공격의 분담 연결로 치명 판정 순서 확인
            BattleEnemyActionResult transferred = lethal.Execute(reduceHealthDamage: ReduceOnce); // 기존 공용 고정 경감 뒤 한 번의 실제 피해 전환
            Assert.AreSame(protector, lethal.Target); // 적 행동의 최종 피격자와 후속 효과 대상 전환 확인
            Assert.AreEqual(1, reductionChecks); // 파티 공용 경감의 한 번 평가 확인
            Assert.AreEqual(8, target.CurrentHealth); // 보호 대상의 원래 체력 전부 보존
            Assert.AreEqual(4, target.CurrentBlock); // 보호 대상의 원래 방어도 전부 보존
            Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.GuardPreparation)?.EffectiveValue); // 대신 받은 타격에서 원래 방어 준비 보존 확인
            Assert.AreEqual(70, target.GetStatusEffect(BattleStatusEffectType.DangerInterception)?.EffectiveValue); // 대신 받은 타격에서 원래 위험 차단 보존 확인
            Assert.AreEqual(otherHealth, other.CurrentHealth); // 전환된 공격에서 원래 제삼 아군 분담 미실행 확인
            Assert.AreEqual(5, transferred.DamageResult.AbsorbedDamage); // 실제 수호자의 방어도부터 소모 확인
            Assert.AreEqual(11, transferred.DamageResult.AppliedDamage); // 수호자의 실제 체력 열한 점 손실 확인
            Assert.AreEqual(1, protector.CurrentHealth); // 대신 받은 바로 그 피해에서 생존 최저치 확인
            Assert.AreEqual(1, guardianHits); // 실제 수호자에게 같은 타격 한 번만 처리
            Assert.AreEqual(0, hitEvents + defeatEvents); // 보호 대상의 피격과 처치 알림 없음 확인
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Bleed)); // 원래 대상에게 후속 상태 미적용 확인
            Assert.IsNotNull(protector.GetStatusEffect(BattleStatusEffectType.Bleed)); // 실제 보호자에게 명중 뒤 출혈 적용 확인
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 전환한 한 번의 보호 소모 확인
            Assert.IsFalse(fight.Turns.IsBattleEnded); // 보호 대상과 시전자가 생존한 전투 계속 확인
        } // 보호 발동 검사 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 피해 전환이 광역의 별도 타격과 재귀하지 않음을 검증
    public IEnumerator GuardianHeartPreservesAreaOrderAndExpiresWhenProtectorFalls() // 대신 받은 한 타격의 생존과 별도 공격의 사망 분리
    { // 광역 타격 수호 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력과 사망 이벤트 연결
        yield return GuardianHeartPreservesAreaOrderAndExpiresWhenProtectorFallsInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 광역 타격 수호 검사 종료

    private IEnumerator GuardianHeartPreservesAreaOrderAndExpiresWhenProtectorFallsInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("GUARDIAN_HEART", "GUARDIAN_HEART")) // 서로 다른 두 아군에게 걸 수 있는 정식 카드
        { // 광역 피해 순서 검사 시작
            var protector = fight.Allies[0]; // 각각의 보호를 지탱하는 같은 시전자
            var first = fight.Allies[1]; // 먼저 받을 광역 타격을 대신 막아 줄 아군
            var second = fight.Allies[2]; // 시전자 사망과 함께 보호가 사라질 두 번째 아군
            var enemy = fight.Enemies[0]; // 전체 공격 의도를 가진 실제 적
            yield return Mark(fight, first); // 첫 아군에게 실제 수호자의 심장 사용
            yield return Mark(fight, second); // 두 번째 아군에게 별도의 한 번 보호 부여
            protector.ApplyPersistentHealth(6); // 대신 받을 타격과 자기 타격의 차이가 드러날 체력
            first.ApplyPersistentHealth(5); // 첫 광역 타격에서 치명 조건을 만족할 체력
            var area = Attack(enemy, first, 140, EnemyTargetRule.AllLiving); // 각 생존 아군을 대상으로 고정한 광역 직접 공격
            MethodInfo setAreaTargets = typeof(BattleEnemyAction).GetMethod("SetAreaTargets", BindingFlags.Instance | BindingFlags.NonPublic); // 전투 적 행동 런타임과 같은 광역 연결 메서드
            Assert.IsNotNull(setAreaTargets); // 실제 광역 대상 연결 경로 확인
            setAreaTargets.Invoke(area, new object[] { new[] { first, protector } }); // 보호 대상 뒤에 시전자의 별도 광역 타격 배치
            area.Execute(); // 같은 행동 내 두 개의 독립 광역 타격 실행
            CollectionAssert.AreEqual(new[] { protector, protector }, area.AreaHitResults.Select(hit => hit.Target)); // 두 타격의 실제 피격자 기록 확인
            Assert.AreEqual(5, area.AreaHitResults[0].Result.DamageResult.AppliedDamage); // 대체한 첫 광역 타격의 수호자 생존 제한 확인
            Assert.AreEqual(1, area.AreaHitResults[1].Result.DamageResult.AppliedDamage); // 시전자의 별도 타격에는 생존 제한 미전파 확인
            Assert.AreEqual(5, first.CurrentHealth); // 보호한 첫 아군의 체력 보존 확인
            Assert.IsTrue(protector.IsDead); // 별도 명중에 원래 사망 규칙 적용 확인
            Assert.IsNull(second.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 사망한 시전자의 남은 보호 즉시 정리 확인
        } // 광역 피해 순서 검사 종료
    } // 플레이 모드 본문 종료

    private static IEnumerator Mark(Fight fight, BattleUnitRuntime ally) // 수호 카드의 정상 손패와 아군 입력 연결
    { // 실제 카드 대상 선택 시작
        var view = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(candidate => candidate.RuntimeUnit == ally); // 기존 전투에 표시된 아군 유닛 칸 조회
        return WinterfallCommonCardExpansionTests.Play(fight.View("GUARDIAN_HEART").RuntimeCard.SourceData, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 실제 사용 비용과 손패 소모 및 지정 효과 확인
    } // 실제 카드 대상 선택 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime enemy, BattleUnitRuntime target, int damage, EnemyTargetRule rule = EnemyTargetRule.FirstLiving, BattleStatusEffectType status = BattleStatusEffectType.None) // 실제 명중과 피해 대체 검사용 적 행동 준비
    { // 적 직접 공격 준비 시작
        return new BattleEnemyAction(enemy, target, EnemyActionType.Attack, BattleDamageType.Physical, damage, status, 1, 1, rule, "검사 공격", 1, 1, 1, 0, additionalStatusChancePercent: status == BattleStatusEffectType.None ? 0 : 100, percentRoll: () => 0); // 기존 적 공격과 확정 후속 상태 및 고정 명중 난수 연결
    } // 적 직접 공격 준비 종료

    private static bool AssignShare(BattleEnemyAction action, BattleUnitRuntime other) // 실제 정의의 피해 분담과 같은 대상 연결
    { // 피해 분담 연결 검사 시작
        MethodInfo method = typeof(BattleEnemyAction).GetMethod("SetDamageShare", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 적 행동의 분담 상태 연결 조회
        Assert.IsNotNull(method); // 런타임 분담 경로 존재 확인
        return (bool)method.Invoke(action, new object[] { other }); // 생존한 제삼 아군을 실제 분담자로 지정
    } // 피해 분담 연결 검사 종료
} // 수호자의 심장 검사 종료
