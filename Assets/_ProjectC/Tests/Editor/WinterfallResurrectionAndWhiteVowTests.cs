using System.Collections; // 실제 카드 연출과 플레이 모드 전환 대기
using System.Collections.Generic; // 실제 전투 결과와 사망 순서 기록
using System.Linq; // 현재 전투 아군의 실제 대상 화면 조회
using NUnit.Framework; // 부활과 생존 및 결과 검증
using UnityEngine; // 전투 화면과 검사 적 데이터 사용
using UnityEngine.TestTools; // 도메인 전환 경계의 실제 조작 검사
using Fight = WinterfallSwordCombatTests.Fight; // 원본 카드 입출력이 연결된 전투 검사 환경

public sealed class WinterfallResurrectionAndWhiteVowTests // 부활의 씨앗과 순백의 맹세의 실제 카드 검사
{ // 성배 생존 검사 시작
    [UnityTest] // 먼저 피해를 막는 효과와 실제 전투불능 부활 검사
    public IEnumerator WhiteVowAndUndyingResolveBeforeDeathAndSeedRevivesOnce() // 기존 보호를 소모한 실제 사망만 한 번 되돌리는지 확인
    { // 실제 전투 검사 모드 시작
        yield return new EnterPlayMode(); // 카드와 전투 화면 초기화 완료 대기
        yield return VerifyWhiteVowAndSeed(); // 실제 선택과 피해 및 이벤트 흐름 확인
        yield return new ExitPlayMode(); // 테스트 환경 정리와 에디터 복귀
    } // 실제 전투 검사 모드 종료

    private static IEnumerator VerifyWhiteVowAndSeed() // 한 아군의 원래 보호 순서와 정확한 부활 체력 검사
    { // 부활 카드 효과 검사 시작
        using var fight = new Fight("RESURRECTION_SEED", "WHITE_VOW"); // 게임에서 얻는 성배 원본 두 장과 실제 손패 구성
        var target = fight.Allies[1]; // 씨앗과 맹세를 받을 생존 동료
        var enemy = fight.Enemies[0]; // 실제 피해와 처치의 적 발생자
        target.ModifyMaxHealth(101 - target.MaxHealth); // 최대 체력 비율의 소수점 올림 검사 값 설정
        Assert.IsTrue(target.ApplyPersistentHealth(101)); // 올림 검사 최대 체력까지 채우기
        Assert.IsTrue(target.ApplyPersistentMental(40)); // 맹세의 정확한 여덟 점 회복을 확인할 정신력 설정
        yield return UseOn(fight, "RESURRECTION_SEED", target); // 실제 손패 클릭으로 살아 있는 한 아군에 적용
        var seed = target.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed); // 실제 전투 상태에 부여된 씨앗 조회
        Assert.AreEqual(20, seed.EffectiveValue); // 원본이 지정한 부활 체력 비율 확인
        Assert.AreEqual(2, seed.RemainingTurns); // 두 실제 적 턴의 유효 기간 확인
        bool shieldPresentWhenMindChanges = false; // 정신력 변화 이전에 피해 무효 적용 여부 기록
        target.MentalChanged += (unit, change) => // 맹세의 실제 정신력 적용 이벤트 수신
        { // 정신력 변경 중 보호 기록 시작
            if (change.Reason == BattleMentalChangeReason.CardEffect) // 원본 카드가 올린 정신력만 확인
            { // 맹세 회복 순서 기록 시작
                shieldPresentWhenMindChanges = unit.GetStatusEffect(BattleStatusEffectType.DamageNullification) != null; // 실제 정신력 변화보다 먼저 적용된 보호 확인
            } // 맹세 회복 순서 기록 종료
        }; // 정신력 변경 중 보호 기록 종료
        yield return UseOn(fight, "WHITE_VOW", target); // 같은 아군에게 순백의 맹세 실제 사용
        Assert.AreEqual(48, target.CurrentMental); // 원본의 정신력 여덟 점 회복 확인
        Assert.IsTrue(shieldPresentWhenMindChanges); // 기존 피해 무효 상태 적용 순서 확인
        var nullified = target.TakeDamage(800, BattleDamageType.None, enemy, enemyDirectAttack: true); // 보호가 있는 아군에게 치명적인 실제 피해 시도
        Assert.IsTrue(nullified.WasDamageNullified); // 순백의 맹세가 첫 피해 한 번을 무효로 만든 결과 확인
        Assert.AreEqual(101, target.CurrentHealth); // 체력 피해와 사망을 막은 원래 체력 확인
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 맹세의 사용한 보호만 소모 확인
        Assert.AreSame(seed, target.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed)); // 피해 무효 동안 사용하지 않은 부활 씨앗 유지 확인
        target.ApplyStatusEffect(BattleStatusEffectType.UndyingGuard, 1, 1, 1); // 실제 치명 피해보다 먼저 발동할 불굴 적용
        target.TakeDamage(800, BattleDamageType.None, enemy, enemyDirectAttack: true); // 불굴이 살린 두 번째 치명 공격 적용
        Assert.AreEqual(1, target.CurrentHealth); // 전투불능 없이 불굴이 남긴 한 점 확인
        Assert.AreSame(seed, target.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed)); // 불굴로 살았을 때 씨앗 미발동 확인
        int beforeDeathMental = target.CurrentMental; // 실제 전투불능 직전 정신력 보관
        var order = new List<string>(); // 피해부터 체력 회복까지 실제 이벤트 순서 기록
        target.DamageTaken += (_, _) => order.Add("damage"); // 기존 실제 피해 신호 수신
        target.Defeated += (source, defeated) => // 발생자와 피격자의 실제 처치 알림 수신
        { // 처치 알림 기록 시작
            Assert.AreSame(enemy, source); // 최초 실제 피해를 준 적 보존 확인
            Assert.AreSame(target, defeated); // 복귀할 동일 기존 전투 유닛 확인
            order.Add("defeated"); // 처치 신호가 한 번만 발생하는지 기록
        }; // 처치 알림 기록 종료
        target.Died += unit => // 사망 신호 도중 실제 전투불능 확인
        { // 사망 상태 기록 시작
            Assert.IsTrue(unit.IsDead); // 부활 전에 실제 전투불능으로 관측되는지 확인
            Assert.Zero(unit.CurrentHealth); // 사망 알림에서 이전 피해의 실제 체력 영 확인
            order.Add("died"); // 실제 사망 신호 기록
        }; // 사망 상태 기록 종료
        target.HealthChanged += unit => // 기존 부활 경로의 체력 갱신 수신
        { // 부활 체력 기록 시작
            if (unit.CurrentHealth == 21) // 기존 체력 영의 뒤에 적용한 생존 체력 확인
            { // 부활 이벤트 순서 기록 시작
                order.Add("revived"); // 체력 복귀 순서 기록
            } // 부활 이벤트 순서 기록 종료
        }; // 부활 체력 기록 종료
        var lethal = new BattleEnemyAction(enemy, target, EnemyActionType.Attack, BattleDamageType.None, 800, BattleStatusEffectType.Burn, 1, 1, EnemyTargetRule.FirstLiving, "씨앗 검사", 1, 1, 1, 0, additionalStatusChancePercent: 100, percentRoll: () => 0, mentalChangeOnHit: -8); // 추가 상태와 정신력 압박이 있는 실제 치명 적 행동
        var attack = lethal.Execute(); // 불굴과 맹세가 끝난 뒤 실제 치명 피해 발생
        Assert.AreEqual(1, attack.DamageResult.AppliedDamage); // 부활과 독립인 실제 마지막 체력 한 점 피해 확인
        Assert.IsFalse(attack.HasAdditionalStatus); // 실제로 쓰러진 타격의 추가 상태 재적용 차단 확인
        CollectionAssert.AreEqual(new[] { "damage", "defeated", "died", "revived" }, order); // 중복 처치나 부활 없이 실제 사건 순서 확인
        Assert.IsFalse(target.IsDead); // 이전 유닛 참조가 실제 전투에 복귀하는지 확인
        Assert.AreEqual(21, target.CurrentHealth); // 최대 체력 백일의 이십 퍼센트 올림 확인
        Assert.AreEqual(beforeDeathMental, target.CurrentMental); // 실제 사망과 부활 뒤 원래 정신력 유지 확인
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed)); // 사망 시 사용한 씨앗의 한 번 소모 확인
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Burn)); // 끝난 타격의 화상이 부활을 넘어 붙지 않는지 확인
        target.TakeDamage(800, BattleDamageType.None, enemy); // 씨앗을 이미 사용한 아군이 다시 실제 사망
        Assert.IsTrue(target.IsDead); // 같은 씨앗의 두 번째 부활을 차단하는지 확인
        Assert.AreEqual(2, order.Count(item => item == "defeated")); // 두 실제 사망에 해당하는 처치만 각각 한 번 확인
        Assert.AreEqual(1, order.Count(item => item == "revived")); // 두 공격 사이에 정확히 한 번 부활 확인
        Assert.AreEqual(BattleResult.None, fight.Turns.Result); // 다른 생존 아군과 함께 전투 계속 확인
    } // 부활 카드 효과 검사 종료

    [UnityTest] // 실제 전원 광역 공격 뒤 승리와 패배 결산 검사
    public IEnumerator WholePartyLethalDoesNotEndBattleUntilSeedRevivalSettles() // 마지막 아군의 씨앗이 잘못된 파티 전멸 방지 확인
    { // 전원 광역 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 환경 초기화 완료 대기
        yield return VerifyPartyOutcome(true); // 광역 사망 후 부활한 아군의 최종 승리 확인
        yield return VerifyPartyOutcome(false); // 부활 뒤 다시 쓰러진 파티의 실제 패배 확인
        yield return new ExitPlayMode(); // 전투 환경 정리와 에디터 복귀
    } // 전원 광역 전투 검사 종료

    private static IEnumerator VerifyPartyOutcome(bool killEnemy) // 광역 치명타 뒤 승리 또는 패배가 한 번만 발생하는지 검사
    { // 파티 결산 확인 시작
        using var fight = new Fight("RESURRECTION_SEED"); // 동일한 성배 원본과 현재 전투 파티 준비
        using var actions = new BattleEnemyActionRuntime(fight.Enemies, fight.Allies, fight.Turns); // 실제 적 행동과 파티 전체 공격 경로 연결
        var seededAlly = fight.Allies[2]; // 광역 공격에서 마지막에 맞을 생존 아군
        var enemy = fight.Enemies[0]; // 실제 아군 전원을 공격할 마지막 적
        JsonUtility.FromJsonOverwrite($"{{\"targetRule\":{(int)EnemyTargetRule.AllLiving},\"basicAttackPower\":1000,\"damageType\":{(int)BattleDamageType.None}}}", enemy.EnemySource); // 실제 적 행동 자료로 치명적 전체 공격 설정
        foreach (var other in fight.Enemies.Skip(1)) // 한 번의 광역 공격만 남기는 전투 준비
        { // 다른 예정 행동 제거 시작
            other.TakeDamage(1000, BattleDamageType.None, fight.Allies[0]); // 남은 단일 공격자를 위한 기존 실제 적 처치
        } // 다른 예정 행동 제거 종료
        yield return UseOn(fight, "RESURRECTION_SEED", seededAlly); // 마지막 대상의 실제 부활 상태 예약
        var outcomes = new List<BattleResult>(); // 승패 이벤트 중복을 확인할 실제 결과 기록
        int snapshottedHealth = -1; // 결과 저장에 들어가는 마지막 아군 체력 초기값
        fight.Turns.BattleCompleted += result => // 실제 전투 결과 확정 신호 등록
        { // 전투 결과 저장 시작
            outcomes.Add(result); // 발생한 최종 결과와 횟수 기록
            snapshottedHealth = seededAlly.CurrentHealth; // 마지막 아군의 결과 확정 당시 체력 기록
        }; // 전투 결과 저장 종료
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 적의 실제 광역 공격을 받을 턴으로 이동
        Assert.AreEqual(1, actions.PrepareActions()); // 마지막 한 적의 실제 광역 행동 준비 확인
        var area = actions.PlannedActions.Single(); // 실제 실행할 전체 공격 조회
        Assert.IsTrue(area.IsAreaAttack); // 한 번의 공격으로 모든 아군을 대상으로 하는지 확인
        actions.ExecuteAction(area); // 실제 광역 피해와 각 유닛의 전투불능 및 씨앗 결산 실행
        Assert.AreEqual(3, area.AreaHitResults.Count); // 아군 전원의 실제 피해 결과가 한 번씩 남는지 확인
        Assert.IsTrue(fight.Allies[0].IsDead && fight.Allies[1].IsDead); // 씨앗이 없는 아군들이 실제로 쓰러졌는지 확인
        Assert.IsFalse(seededAlly.IsDead); // 광역 공격의 마지막 대상이 씨앗으로 실제 복귀 확인
        int revivedHealth = (seededAlly.MaxHealth + 4) / 5; // 마지막 생존자의 정확한 이십 퍼센트 올림 계산
        Assert.AreEqual(revivedHealth, seededAlly.CurrentHealth); // 파티 전멸 대신 복귀한 실제 체력 확인
        Assert.IsEmpty(outcomes); // 마지막 대상의 사망 중 잘못 확정된 패배 차단 확인
        Assert.AreEqual(BattleResult.None, fight.Turns.Result); // 실제 광역 결산 뒤에도 전투 진행 확인
        if (killEnemy) // 마지막 적 사망이 일어난 승리 분기 확인
        { // 최종 승리 실행 시작
            enemy.TakeDamage(1000, BattleDamageType.None, seededAlly); // 기존 복귀 유닛의 공격으로 마지막 적 실제 처치
        } // 최종 승리 실행 종료
        else // 씨앗을 소모한 아군의 최종 패배 확인
        { // 최종 패배 실행 시작
            seededAlly.TakeDamage(1000, BattleDamageType.None, enemy); // 사용한 씨앗이 되살릴 수 없는 실제 사망
        } // 최종 패배 실행 종료
        CollectionAssert.AreEqual(new[] { killEnemy ? BattleResult.Victory : BattleResult.Defeat }, outcomes); // 광역 이후 실제 승패 결산이 정확히 한 번만 발생 확인
        Assert.AreEqual(killEnemy ? revivedHealth : 0, snapshottedHealth); // 결과를 기록할 때의 최종 생존 체력 확인
    } // 파티 결산 확인 종료

    [UnityTest] // 실제 두 적 턴의 만료와 원래 출혈 경로 검사
    public IEnumerator SeedExpiresOnSecondEnemyTurnAndWorksForLethalBleedWhileActive() // 아군 턴의 예약 유지와 비공격 사망의 정확한 부활 확인
    { // 씨앗 기간 검사 모드 시작
        yield return new EnterPlayMode(); // 실제 카드와 턴 환경 준비 완료 대기
        yield return VerifyExpiryAndBleed(); // 지속과 출혈을 실제 진영 경계로 확인
        yield return new ExitPlayMode(); // 전투 환경 정리와 에디터 복귀
    } // 씨앗 기간 검사 모드 종료

    private static IEnumerator VerifyExpiryAndBleed() // 씨앗 기간과 지속 피해 사망 경로 비교
    { // 씨앗 기간 및 출혈 검사 시작
        using var fight = new Fight("RESURRECTION_SEED", "RESURRECTION_SEED"); // 실제 기간 두 번과 출혈용 원본 두 장 검사
        var expiredTarget = fight.Allies[1]; // 두 실제 적 턴이 지나 씨앗이 끝날 아군
        var bleedingTarget = fight.Allies[2]; // 유효한 상태에서 출혈로 쓰러질 다른 아군
        yield return UseOn(fight, "RESURRECTION_SEED", expiredTarget); // 만료를 관측할 첫 씨앗 적용
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 정리 이후 첫 적 턴 진입
        Assert.AreEqual(2, expiredTarget.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed).RemainingTurns); // 아군 턴 종료가 씨앗 기간을 줄이지 않는지 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴을 정상 완료하여 남은 기간 비교
        Assert.AreEqual(1, expiredTarget.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed).RemainingTurns); // 실제 적 턴 한 번의 기간 차감 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 두 번째 적 턴까지 완료
        Assert.IsNull(expiredTarget.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed)); // 두 번째 실제 적 턴 종료의 씨앗 만료 확인
        expiredTarget.TakeDamage(1000, BattleDamageType.None); // 씨앗 만료 이후의 실제 치명 피해 발생
        Assert.IsTrue(expiredTarget.IsDead); // 만료한 효과가 전투불능을 되돌리지 않는지 확인
        yield return UseOn(fight, "RESURRECTION_SEED", bleedingTarget); // 여전히 유효한 새 씨앗의 적용
        bleedingTarget.ApplyStatusEffect(BattleStatusEffectType.Bleed, bleedingTarget.CurrentHealth, 1, 1); // 출혈 처리기의 방어도 없는 치명적 피해 설정
        var processed = new BattleStatusEffectProcessor().ProcessPhaseEnd(bleedingTarget, fight.Turns.CurrentRound).Single(result => result.EffectType == BattleStatusEffectType.Bleed); // 출혈의 일반 실제 턴 종료 경로 실행
        Assert.AreEqual(BattleStatusEffectRemovalReason.UnitDied, processed.RemovalReason); // 같은 처리에서 부활해도 출혈 사망 기록 확인
        Assert.IsFalse(bleedingTarget.IsDead); // 실제 출혈이 쓰러뜨린 대상의 씨앗 복귀 확인
        Assert.AreEqual((bleedingTarget.MaxHealth + 4) / 5, bleedingTarget.CurrentHealth); // 비공격 사망에도 같은 체력 비율 사용 확인
    } // 씨앗 기간 및 출혈 검사 종료

    private static IEnumerator UseOn(Fight fight, string id, BattleUnitRuntime target) // 실제 손패와 아군 대상의 일반 조작
    { // 전투 카드 입력 시작
        var card = fight.View(id).RuntimeCard.SourceData; // 실제 보상 원본으로 만들어진 현재 카드 조회
        var view = fight.Hand.GetComponentInParent<Canvas>().GetComponentsInChildren<BattleUnitView>().Single(unit => unit.RuntimeUnit == target); // 현재 전투 아군의 실제 클릭 화면 조회
        return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 카드 선택과 실제 지불 및 버린 더미 이동 검사
    } // 전투 카드 입력 종료
} // 성배 생존 검사 종료
