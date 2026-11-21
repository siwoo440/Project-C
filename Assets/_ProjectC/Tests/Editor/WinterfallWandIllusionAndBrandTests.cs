using System.Collections; // 실제 카드 입력과 모드 전환 대기
using System.Linq; // 실제 예정 행동과 대상 화면 조회
using System.Reflection; // 전투 검사에 연결된 적 행동 관리자 조회
using NUnit.Framework; // 카드 수치와 피해 정산 확인
using UnityEngine; // 실제 아군 화면과 검사 원본 사용
using UnityEngine.TestTools; // 게임 전투의 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 실제 전투와 카드 사용의 공용 검사 환경

public sealed class WinterfallWandIllusionAndBrandTests // 별빛 낙인과 환영 분열의 실제 전투 검사
{ // 두 지팡이 카드 회귀 검사 시작
    [UnityTest] // 실제 전체 적 낙인과 두 적 턴 기간 확인
    public IEnumerator StarlightBrandAppliesNativeVulnerabilityForTwoEnemyTurns() // 생존 적의 취약 수치와 피해 예고 확인
    { // 별빛 낙인의 원본 카드 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 전투 턴을 구동하는 모드 진입
        using (var fight = new Fight("STARLIGHT_BRAND")) // 카드 원본과 네 적이 있는 독립 전투
        { // 면역과 전투불능을 포함한 실제 전체 대상 검사 시작
            var owner = fight.Allies[0]; // 아군 원본 카드 소유자
            var enemy = fight.Enemies[0]; // 실제 피해 예고를 비교할 생존 적
            var other = fight.Enemies[1]; // 함께 낙인받을 다른 생존 적
            var immune = fight.Enemies[2]; // 일반 디버프 면역이 있는 생존 적
            var fallen = fight.Enemies[3]; // 실제 전투불능이 될 네 번째 적
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 취약 거부 경계 설정
            fallen.TakeDamage(1000, BattleDamageType.None); // 실제 전투불능 처리를 거친 비교 적
            Assert.IsTrue(fallen.IsDead); // 살아 있는 적만 남은 비교 구성 확인
            Assert.AreEqual(3, fight.View("STARLIGHT_BRAND").RuntimeCard.ApCost); // 원본의 행동력 세 점 확인
            Assert.AreEqual(20, enemy.PreviewDamage(20, BattleDamageType.Physical, owner).RawDamage); // 일반 물리 피해의 원래 수치 확인
            yield return fight.Use("STARLIGHT_BRAND"); // 적을 따로 고르지 않는 실제 전체 낙인 사용
            Assert.AreEqual(10, enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable).EffectiveValue); // 첫 생존 적의 받는 피해 증가율 확인
            Assert.AreEqual(2, other.GetStatusEffect(BattleStatusEffectType.Vulnerable).RemainingTurns); // 두 번째 생존 적의 두 적 턴 지속 확인
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 기존 부정 상태 면역 적용 확인
            Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 전투불능 적 제외 확인
            Assert.AreEqual(22, enemy.PreviewDamage(20, BattleDamageType.Physical, owner).RawDamage); // 방어 이전 실제 피해 열 퍼센트 증가 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 턴 종료
            Assert.AreEqual(1, enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable).RemainingTurns); // 첫 적 턴 이후 한 턴 남음 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
            Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 지속 시간 이후 원래 취약 제거 확인
            Assert.IsNull(other.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 동시 적용된 다른 대상의 기간 만료 확인
        } // 면역과 전투불능을 포함한 실제 전체 대상 검사 종료
        yield return new ExitPlayMode(); // 실제 전투 검사 정리
    } // 별빛 낙인의 원본 카드 검사 종료

    [UnityTest] // 회피와 전체 피해 거부 및 방어도가 있는 실제 명중 확인
    public IEnumerator IllusionOnlySpendsAfterAcceptedPositiveDirectEnemyDamage() // 실제 다음 적 명중의 한 번 경감과 독립 회피의 만료 확인
    { // 환영 분열의 실제 카드 사용 검사 시작
        yield return new EnterPlayMode(); // 실제 단일 아군 카드 입력 모드 진입
        using (var fight = new Fight("ILLUSION_SPLIT", "ILLUSION_SPLIT")) // 두 아군을 독립 비교할 실제 환영 분열 손패
        { // 두 상태와 실제 피해 종류 비교 시작
            var owner = fight.Allies[0]; // 카드 소유자이자 두 번째 보호 대상
            var target = fight.Allies[1]; // 첫 보호를 선택한 다른 아군
            var enemy = fight.Enemies[0]; // 한 번 명중할 실제 적 출처 준비
            var action = Attack(enemy, target, 30, 0); // 확정 명중과 기존 실제 보호를 쓰는 공격
            Assert.AreEqual(2, fight.View("ILLUSION_SPLIT").RuntimeCard.ApCost); // 원본의 단일 사용 비용 확인
            int originalDamage = action.PreviewDamage().FinalDamage; // 카드 적용 이전의 보정된 기준 피해
            yield return PlayOnAlly(fight, target); // 다른 아군을 눌러 실제로 환영 분열 시전
            var illusion = target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction); // 이전에 적용된 것과 비교할 실제 일회성 상태
            Assert.AreEqual(5, illusion.EffectiveValue); // 환영의 고정 경감 수치 확인
            Assert.AreEqual(25, target.GetStatusEffect(BattleStatusEffectType.Evasion).EffectiveValue); // 회피율 이십오 퍼센트포인트 확인
            Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.Evasion).RemainingTurns); // 회피의 두 적 턴 초기값 확인
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 한 번의 카드로 지정한 아군만 보호 확인
            Assert.AreEqual(originalDamage - 5, action.PreviewDamage().FinalDamage); // 순수 피해 예고의 고정 다섯 점 반영 확인
            Assert.AreSame(illusion, target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 예고를 보고도 환영이 소모되지 않음 확인
            Assert.IsTrue(Attack(enemy, target, 30, 99).Execute().WasMissed); // 실제 회피 판정이 멈춘 적 타격 확인
            Assert.AreSame(illusion, target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 실제로 빗나간 타격 이후 환영 보존 확인
            target.GainBlock(100); // 이후 직접 명중이 모두 방어도를 차감하게 설정
            target.TakeDamage(1, BattleDamageType.Physical, enemy); // 직접 명중 표식이 없는 일반 피해 비교
            Assert.AreSame(illusion, target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 양수 일반 피해에서도 직접 피격 환영 보존 확인
            target.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 먼저 양수 피해 전체를 거부할 기존 보호 준비
            Assert.IsTrue(action.Execute().DamageResult.WasDamageNullified); // 기존 완전 피해 무효의 우선 적용 확인
            Assert.AreSame(illusion, target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 피해 무효 뒤에도 다음 명중용 환영 보존 확인
            target.ApplyStatusEffect(BattleStatusEffectType.ShockAbsorption, 100, 1, 1); // 남은 피해 전부를 먼저 감당하는 기존 강한 보호
            Assert.AreEqual(0, action.Execute().DamageResult.FinalDamage); // 먼저 적용되는 고정 방어의 전량 경감 확인
            Assert.AreSame(illusion, target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 양수 잔여 피해가 없을 때 환영 보존 확인
            target.RemoveStatusEffect(BattleStatusEffectType.ShockAbsorption); // 다음 양수 직접 피격에 선행 경감 제거
            Assert.AreEqual(originalDamage - 5, action.Execute().DamageResult.AbsorbedDamage); // 실제 방어도 차감에서만 경감이 결제되는 명중 확인
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 양수 직접 명중의 한 번 소모 확인
            Assert.AreEqual(originalDamage, action.Execute().DamageResult.AbsorbedDamage); // 이후 직접 명중의 원래 피해 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.Evasion)); // 먼저 환영을 써도 회피 기간 독립 확인
            yield return PlayOnAlly(fight, owner); // 두 번째 카드로 소유자에게 별도 보호 적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 회피가 적용된 첫 실제 적 턴 종료
            Assert.AreEqual(1, owner.GetStatusEffect(BattleStatusEffectType.Evasion).RemainingTurns); // 첫 적 턴 이후 회피 한 턴 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.Evasion)); // 회피만 지정한 두 턴 후 만료 확인
            Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 실제 적 명중이 없으면 두 턴 이후 환영 보존 확인
        } // 두 상태와 실제 피해 종류 비교 종료
        yield return new ExitPlayMode(); // 직접 피격 검사 환경 종료
    } // 환영 분열의 실제 카드 사용 검사 종료

    [UnityTest] // 현재 전투에서 동시에 정렬된 적 예정 행동의 방어 순서 검사
    public IEnumerator OrderedIntentsUseTheIllusionOnceAfterExistingNullification() // 실제 명중 예고의 무효와 일회성 경감 순서 확인
    { // 여러 적 행동의 순서별 예고 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 예정 행동을 구동하는 모드 진입
        using (var fight = new Fight("ILLUSION_SPLIT")) // 동일한 대상에게 이어지는 예정 공격이 있는 전투
        { // 실제 적 행동 구성을 이용한 순서 확인 시작
            var target = fight.Allies[0]; // 적 예정 공격이 공통으로 노리는 실제 생존자
            fight.Allies[1].ApplyPersistentHealth(0); // 다른 선택 후보를 전투불능으로 설정
            fight.Allies[2].ApplyPersistentHealth(0); // 단일 실제 선택 후보로 구성
            JsonUtility.FromJsonOverwrite("{\"basicAttackPower\":24,\"minimumActionSpeed\":4,\"maximumActionSpeed\":4}", fight.Enemies[0].EnemySource); // 모든 검사 적이 공유하는 실제 원본 공격과 속도 지정
            var actionsField = typeof(BattleCardActionController).GetField("enemyActions", BindingFlags.Instance | BindingFlags.NonPublic); // 전투 화면의 기존 예정 행동 관리자 참조 위치
            var actions = (BattleEnemyActionRuntime)actionsField.GetValue(fight.Controller); // 현재 카드 선택과 연결된 실제 예정 행동 관리자
            Assert.AreEqual(4, actions.PrepareActions()); // 실제 생존 적 넷의 예정 공격 정렬 확인
            Assert.IsTrue(actions.PlannedActions.All(action => action.Target == target)); // 정렬된 모든 행동의 실제 대상 확인
            int[] baseline = actions.PlannedActions.Select(action => action.PreviewDamage().FinalDamage).ToArray(); // 일회성 상태가 없는 원래 공격별 피해
            Assert.IsTrue(baseline.All(damage => damage > 5)); // 비교에 필요한 양수 공격 피해 확인
            yield return PlayOnAlly(fight, target); // 실제 손패로 예정 피해에 환영 보호 추가
            Assert.AreEqual(baseline[0] - 5, actions.PlannedActions[0].PreviewDamage().FinalDamage); // 첫 양수 명중 예정에서만 환영 사용
            Assert.AreEqual(baseline[1], actions.PlannedActions[1].PreviewDamage().FinalDamage); // 예고 순서에서 같은 환영의 두 번째 반영 방지
            target.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 첫 적 공격 전체를 먼저 거부하도록 기존 상태 설정
            Assert.AreEqual(0, actions.PlannedActions[0].PreviewDamage().FinalDamage); // 기존 완전 무효를 표시하는 첫 적 행동
            Assert.AreEqual(baseline[1] - 5, actions.PlannedActions[1].PreviewDamage().FinalDamage); // 실제 다음 양수 공격만 받는 환영 경감
            Assert.AreEqual(baseline[2], actions.PlannedActions[2].PreviewDamage().FinalDamage); // 무효와 환영이 모두 쓰인 뒤 원래 순서값 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.IllusionHitReduction)); // 적 행동 예고가 실제 환영을 지우지 않음 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 예고가 기존 피해 무효도 소모하지 않음 확인
        } // 실제 적 행동 구성을 이용한 순서 확인 종료
        yield return new ExitPlayMode(); // 예정 피해 검사 환경 종료
    } // 여러 적 행동의 순서별 예고 검사 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime enemy, BattleUnitRuntime target, int amount, int roll) // 실제 적 명중 경로의 고정 난수 공격
    { // 개별 적 공격 생성 시작
        return new BattleEnemyAction(enemy, target, EnemyActionType.Attack, BattleDamageType.Physical, amount, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "환영 검사", 1, 1, 1, 0, percentRoll: () => roll); // 실제 적 행동에 명중 비교 난수 전달
    } // 개별 적 공격 생성 종료

    private static IEnumerator PlayOnAlly(Fight fight, BattleUnitRuntime ally) // 원본 단일 아군 카드의 화면 입력
    { // 실제 대상 클릭 준비 시작
        var target = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).Single(view => view.RuntimeUnit == ally); // 정확한 아군 런타임의 전투 화면 선택
        return WinterfallCommonCardExpansionTests.Play(fight.View("ILLUSION_SPLIT").RuntimeCard.SourceData, fight.Hand, target, fight.Runner, fight.Deck, fight.Points); // 실제 손패 결제와 대상 선택 및 카드 버림 확인
    } // 실제 대상 클릭 준비 종료
} // 두 지팡이 카드 회귀 검사 종료
