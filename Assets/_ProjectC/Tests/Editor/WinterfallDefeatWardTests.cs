using System.Collections; // 실제 전투 화면 대기
using System.Linq; // 수호자의 심장 대상 화면 조회
using System.Reflection; // 방어도를 건너뛰는 실제 피해 호출
using NUnit.Framework; // 실제 생존과 소모 횟수 검증
using UnityEngine.TestTools; // 실제 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 손패와 전투 턴 검사 환경

public sealed class WinterfallDefeatWardTests // 성배의 심판 일회 생존 효과 검사
{ // 성배 효과 검사 시작
    [UnityTest] // 실제 카드 비용과 대상별 한 번 보호 확인
    public IEnumerator GrailWardPersistsAndOnlyConsumesOnEachAllysLethalDamage() // 일반 방어와 체력 비용 및 무방어 피해 구별
    { // 독립 생존 보호 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 입력과 상태 처리 시작
        yield return GrailWardPersistsAndOnlyConsumesOnEachAllysLethalDamageInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 독립 테스트 전투 종료
    } // 독립 생존 보호 검사 종료

    private IEnumerator GrailWardPersistsAndOnlyConsumesOnEachAllysLethalDamageInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("GRAIL_JUDGMENT")) // 정식 성배의 심판 보상 원본으로 전투 구성
        { // 생존 대상과 무방어 피해 확인 시작
            var caster = fight.Allies[0]; // 성배 시전자와 체력 비용 대조군
            var ally = fight.Allies[1]; // 개별 한 번 보호를 받을 생존 동료
            var dead = fight.Allies[2]; // 카드 전체 대상에서 제외할 전투불능 동료
            var enemy = fight.Enemies[0]; // 실제 피해 출처가 될 적
            dead.ApplyPersistentHealth(0); // 정상 대상 수집에서 제외할 전투불능 상태
            caster.ApplyPersistentHealth(8); // 체력 비용 뒤 한 점이 남는 상태
            ally.ApplyPersistentHealth(8); // 한 점 남는 피해와 치명 피해 비교 상태
            int before = fight.Points.CurrentActionPoints; // 카드 사용 직전 공용 행동력
            yield return fight.Use("GRAIL_JUDGMENT"); // 실제 손패를 통한 아군 전체 상태 부여
            Assert.AreEqual(before - 5, fight.Points.CurrentActionPoints); // 원본 카드의 고정 행동력 다섯 소모
            Assert.AreEqual(1, caster.GetStatusEffect(BattleStatusEffectType.DefeatWard)?.StackCount); // 정상 시전자의 독립 보호 한 번
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.DefeatWard)?.StackCount); // 생존 동료의 독립 보호 한 번
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 전투불능 동료의 상태 미적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 적 턴을 치명 피해 없이 진행
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 둘째 적 턴까지 소모되지 않는 보호 확인
            Assert.IsTrue(caster.TrySpendHealth(7)); // 시전자가 생존 체력 비용을 지불하는 실제 경로
            Assert.AreEqual(1, caster.CurrentHealth); // 체력 비용은 원래 크기대로 정산
            Assert.IsNotNull(caster.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 체력 비용에서 방지 횟수 미소모
            ally.GainBlock(500); // 치명적인 원본 공격을 모두 흡수할 방어도 준비
            Assert.AreEqual(0, Attack(enemy, ally, 140).Execute().DamageResult.AppliedDamage); // 방어도로 흡수된 타격의 체력 피해 없음
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 전량 막은 타격에서 보호 유지
            ally.ClearBlock(); // 방어도 없는 경로의 흡수 조건 제거
            int hitEvents = 0; // 개선 이후 무방어 피해의 실제 알림 횟수
            int defeatEvents = 0; // 동료의 실제 전투불능 알림 횟수
            ally.DamageResolved += (_, _, _) => hitEvents++; // 각 무방어 피격 알림 수집
            ally.Defeated += (_, _) => defeatEvents++; // 최종 사망 알림 수집
            Assert.AreEqual(7, Unblocked(ally, enemy, 7)); // 방어도를 거치지 않고 정확히 체력 한 점을 남긴 피해
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 치명적이지 않은 한 점 생존의 보호 유지
            Assert.AreEqual(0, Unblocked(ally, enemy, 1)); // 남은 체력과 같은 무방어 피해를 막아 생존
            Assert.AreEqual(1, ally.CurrentHealth); // 치명 피해를 받은 뒤 체력 한 점 확인
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 받은 동료의 한 번 보호만 소모
            Assert.IsNotNull(caster.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 다른 아군의 한 번 보호는 독립 유지
            Assert.AreEqual(2, hitEvents); // 두 번의 무방어 피해마다 한 번만 결과 알림
            Assert.AreEqual(0, defeatEvents); // 보호 중 거짓 사망 이벤트 차단
            Assert.AreEqual(1, Unblocked(ally, enemy, 1)); // 한 번 소모한 뒤 다음 피해를 원래대로 적용
            Assert.IsTrue(ally.IsDead); // 소모 뒤 재생존 없음 확인
            Assert.AreEqual(3, hitEvents); // 마지막 피해도 결과 알림 한 번
            Assert.AreEqual(1, defeatEvents); // 실제 사망에서만 전투불능 알림
        } // 생존 대상과 무방어 피해 확인 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 다른 생존 보호와 같은 타격에서 중복 소모 방지
    public IEnumerator GrailWardRunsAfterNullificationAndUndyingBeforeGuardianTransfer() // 기존 피해 보호의 우선순위 확인
    { // 피해 방지 우선순위 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 카드 입력 준비
        yield return GrailWardRunsAfterNullificationAndUndyingBeforeGuardianTransferInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 전투 환경 종료
    } // 피해 방지 우선순위 검사 종료

    private IEnumerator GrailWardRunsAfterNullificationAndUndyingBeforeGuardianTransferInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("GRAIL_JUDGMENT", "GUARDIAN_HEART")) // 정식 성배와 수호자 카드를 가진 전투
        { // 카드별 한 번 보호 우선순위 확인 시작
            var protector = fight.Allies[0]; // 치명 피격을 대신 받는 실제 시전자
            var target = fight.Allies[1]; // 성배와 수호자의 심장 동시 적용 대상
            var enemy = fight.Enemies[0]; // 실제 직접 피해 공격자
            protector.ApplyPersistentHealth(12); // 보호자 전환 피해의 체력 기준
            target.ApplyPersistentHealth(8); // 불굴과 성배의 생존 기준
            yield return fight.Use("GRAIL_JUDGMENT"); // 실제 아군 전체 성배 보호 부여
            var allyView = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == target); // 수호자의 심장 실제 아군 선택 화면
            yield return WinterfallCommonCardExpansionTests.Play(fight.View("GUARDIAN_HEART").RuntimeCard.SourceData, fight.Hand, allyView, fight.Runner, fight.Deck, fight.Points); // 후순위 치명 피격 대체 부여
            target.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 우선 발동하는 피해 무효 설치
            target.ApplyStatusEffect(BattleStatusEffectType.UndyingGuard, 1, 2, 1); // 성배보다 앞선 기존 불굴 설치
            Assert.IsTrue(Attack(enemy, target, 140).Execute().DamageResult.WasDamageNullified); // 피해 무효가 첫 치명 타격 전부 처리
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 피해 무효로 기존 불굴 보존
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 피해 무효로 성배 보호 보존
            Assert.AreEqual(7, Attack(enemy, target, 140).Execute().DamageResult.AppliedDamage); // 두 번째 타격을 기존 불굴로 생존
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 기존 불굴의 일회 소모 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 불굴 생존 시 성배 보호 보존
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 불굴 생존 시 수호 대체도 보존
            target.ApplyPersistentHealth(8); // 방어도 경유 치명 피해의 비교 체력 재설정
            target.GainBlock(3); // 실제 치명 타격에서 체력 이전에 흡수할 방어도
            int hitEvents = 0; // 보호받은 아군의 정상 피격 알림 기록
            int protectorHits = 0; // 수호자에게 옮겨간 실제 명중 횟수 기록
            target.DamageResolved += (_, _, _) => hitEvents++; // 보호 효과 소모 타격의 결과 구독
            protector.DamageResolved += (_, _, _) => protectorHits++; // 수호자 과잉 판정 확인용 구독
            var warded = Attack(enemy, target, 140); // 앞선 두 보호가 소모된 실제 세 번째 직접 타격
            var result = warded.Execute().DamageResult; // 방어도와 한 번 생존을 함께 정산
            Assert.AreSame(target, warded.Target); // 성배 발동으로 수호자 쪽 대상 전환 없음 확인
            Assert.AreEqual(3, result.AbsorbedDamage); // 실제 타격의 방어도 흡수 유지
            Assert.AreEqual(7, result.AppliedDamage); // 실제 체력 한 점을 남긴 일곱 피해 확인
            Assert.AreEqual(1, target.CurrentHealth); // 성배 보호로 생존 확인
            Assert.AreEqual(1, hitEvents); // 성배 보호가 일반 피해 이벤트를 중복 발생시키지 않음 확인
            Assert.AreEqual(0, protectorHits); // 성배 보호에서 수호자 대신 피격 미발생 확인
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 대상의 성배 보호 한 번 소모 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 성배 생존 시 수호자 대체 기회 보존
            var redirected = Attack(enemy, target, 140); // 성배 보호가 없어진 원래 대상의 치명 피격
            redirected.Execute(); // 수호자가 기존 규칙으로 한 번 대신 맞는 타격 실행
            Assert.AreSame(protector, redirected.Target); // 다음 치명 피해만 수호자에게 전환 확인
            Assert.AreEqual(1, protectorHits); // 수호자의 실제 피격 한 번 확인
            Assert.AreEqual(1, protector.CurrentHealth); // 수호자 효과 자체의 생존 상한 확인
            Assert.IsNotNull(protector.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 이미 보장된 생존에서 수호자 개인의 성배 미소모 확인
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 실제 대체 이후 지정 보호 한 번 소모 확인
            Assert.AreEqual(0, Attack(enemy, protector, 140).Execute().DamageResult.AppliedDamage); // 수호자의 별도 치명 피격에서 자기 성배 한 번 발동
            Assert.IsNull(protector.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 수호자 개인 보호의 독립 소모 확인
        } // 카드별 한 번 보호 우선순위 확인 종료
    } // 플레이 모드 본문 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime enemy, BattleUnitRuntime ally, int amount) // 실제 적 직접 타격 구성
    { // 적 공격 구성 시작
        return new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, amount, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "성배 검사 공격", 1, 1, 1, 0, percentRoll: () => 0); // 실제 적 직접 피격과 명중 난수 연결
    } // 적 공격 구성 종료

    private static int Unblocked(BattleUnitRuntime ally, BattleUnitRuntime enemy, int amount) // 출혈과 분담에 쓰이는 방어도 없는 실제 피해 호출
    { // 무방어 피해 호출 시작
        var method = typeof(BattleUnitRuntime).GetMethod("TakeUnblockedDamage", BindingFlags.Instance | BindingFlags.NonPublic); // 런타임 내부의 실제 무방어 피해 경로 확인
        Assert.IsNotNull(method); // 실제 피해 메서드 발견 확인
        return (int)method.Invoke(ally, new object[] { amount, BattleDamageType.None, enemy, false }); // 방어도에서 흡수되지 않는 확정 체력 피해 호출
    } // 무방어 피해 호출 종료
} // 성배 효과 검사 종료
