using System.Collections; // 실제 손패 연출 대기
using System.Linq; // 파티 값과 실제 카드 목록 비교
using NUnit.Framework; // 전투 결과의 기대 수치 비교
using UnityEngine.TestTools; // Unity 실제 플레이 모드 전환
using static WinterfallSwordCombatTests; // 기존 카드 입력 환경 사용

public sealed class WinterfallPentacleWardSupportTests // 방벽과 미끼 및 계약 파기의 실제 전투 검사
{ // 펜타클 지원 검사 시작
    [UnityTest] // 방벽과 다음 실제 아군의 행동력 검사
    public IEnumerator RuneBarrierGivesEachLivingAllyBlockAndQueuesOnlyNextAllyTurn() // 현재 파티 방어도와 다음 턴 행동력 분리
    { // 방벽 지원 검사 시작
        yield return new EnterPlayMode(); // 실제 리소스와 카드 입력 시작
        yield return PlayBarrier(); // 현재 방어도와 다음 턴 대기 확인
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 정리
    } // 방벽 지원 검사 종료

    private static IEnumerator PlayBarrier() // 룬 방벽의 일반 결제와 실제 턴 경계 확인
    { // 방벽의 실제 조우 시작
        using (var fight = new Fight("RUNE_BARRIER")) // 공식 보물 카드의 기존 전투 손패 등록
        { // 방벽의 실제 카드 사용 준비 시작
            var card = fight.View("RUNE_BARRIER").RuntimeCard; // 들어온 방벽의 실제 원본과 소유자
            Assert.AreEqual(3, card.ApCost); // 원본 방벽의 일반 고정 행동력 삼
            Assert.AreEqual(CardEffectType.GainBlock, card.EffectType); // 실제 방어 카드와 동일한 기존 분류 경로
            fight.Allies[2].ApplyPersistentHealth(0); // 체력 없는 파티원의 방벽 대상 제외
            yield return fight.Use("RUNE_BARRIER"); // 실제 아군 그룹 대상 카드를 클릭
            CollectionAssert.AreEqual(new[] { 10, 10, 0 }, fight.Allies.Select(ally => ally.CurrentBlock)); // 살아 있는 각자에게 기본 방어도 열 지급
            Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 현재 턴에는 원본 결제만 정산
            Assert.AreEqual(20, fight.Points.MaxActionPoints); // 방벽의 현재 최대 행동력 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 다음 아군 차례 전 적 턴 경계 이동
            Assert.AreEqual(20, fight.Points.MaxActionPoints); // 적 턴에 방벽 보상이 나오지 않음
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 차례 도착
            Assert.AreEqual(21, fight.Points.CurrentActionPoints); // 기본 회복에 더한 예약 행동력 한 점 지급
            Assert.AreEqual(21, fight.Points.MaxActionPoints); // 같은 차례의 임시 최대 행동력 증가
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 지급한 아군 차례 종료
            Assert.AreEqual(20, fight.Points.MaxActionPoints); // 종료에 원래 최대 행동력 복원
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 그다음 실제 아군 턴 도착
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 방벽 행동력의 반복 적용 없음
        } // 방벽의 실제 카드 사용 준비 종료
    } // 방벽의 실제 조우 종료

    [UnityTest] // 미끼의 표식과 직접 공격 약화가 독립 지속되는 검사
    public IEnumerator GoldenLureMarksAndWeakensActualEnemyAttacksForDifferentDurations() // 예고와 실피해 및 면역 비교
    { // 미끼 지원 검사 시작
        yield return new EnterPlayMode(); // 실제 적 예고와 카드 입력 준비
        yield return PlayLure(); // 두 상태의 실제 수명과 적 행동 비교
        yield return new ExitPlayMode(); // 실제 전투 검사 환경 정리
    } // 미끼 지원 검사 종료

    private static IEnumerator PlayLure() // 금빛 미끼의 적 전체 상태와 실제 공격 검증
    { // 금빛 미끼의 실제 조우 시작
        using (var fight = new Fight("GOLDEN_LURE")) // 실제 배포 카드 한 장 사용 준비
        { // 금빛 미끼의 대상과 예고 생성 시작
            var foe = fight.Enemies[0]; // 기존 고정 약화와 새 비율 상태의 대상
            var ally = fight.Allies[0]; // 실제로 적 직접 피해를 받을 아군
            ally.ModifyMaxHealth(250 - ally.MaxHealth); // 비교 대상이 충분한 체력으로 생존하도록 원래 최대치 조정
            ally.ApplyPersistentHealth(250); // 충분한 실제 체력 초기화
            foe.ApplyStatusEffect(BattleStatusEffectType.AttackPowerDown, 10, 4, 1); // 기존 가산 계산에 들어가는 고정 공격 감소
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 4, 1); // 두 약화를 따로 막을 기존 면역
            fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 이미 쓰러져 상태를 받을 수 없는 상대
            var attack = CreateAction(foe, ally, EnemyActionType.Attack); // 실제 공유 직접 피해 예고를 먼저 생성
            int raw = attack.AttackAmount; // 기존 고정 약화가 적용된 직접 피해 기준
            Assert.AreEqual(attack.Amount - 10, raw); // 실제 조우 배율을 반영한 기준에서 고정 열 점 감소 확인
            int reduced = UnityEngine.Mathf.RoundToInt(raw * 0.9f); // 기존 고정 약화 이후에 적용하는 원본 십 퍼센트
            yield return fight.Use("GOLDEN_LURE"); // 살아 있는 적 전체에 실제 금빛 미끼 사용
            Assert.AreEqual(reduced, attack.AttackAmount); // 고정 약화 다음의 십 퍼센트 감소를 적용한 수치
            Assert.AreEqual(1, foe.GetStatusEffect(BattleStatusEffectType.PublicMark).RemainingTurns); // 실제 공명 표식의 한 적 턴 기간
            Assert.AreEqual(2, foe.GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown).RemainingTurns); // 별도 직접 피해 약화의 두 적 턴 기간
            Assert.IsNotNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown)); // 다른 생존 적에게도 별개 상태 적용
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.PublicMark)); // 기존 상태 면역의 표식 보호 확인
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown)); // 기존 상태 면역의 피해 약화 보호 확인
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown)); // 전투불능 적에게 새 상태 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 먼저 끝난 아군 차례가 적의 표식 수명을 깎지 않음
            Assert.IsNotNull(foe.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 적 공격 시점의 활성 표식 확인
            int preview = attack.PreviewDamage().AppliedDamage; // 현재 방어력을 거치는 실제 체력 피해 예고
            Assert.Greater(preview, 0); // 예고와 실피해 비교를 위한 양수 피해 확인
            Assert.AreEqual(preview, attack.Execute().DamageResult.AppliedDamage); // 같은 직접 공격의 예고와 실피해 일치
            var nonAttack = CreateAction(foe, ally, EnemyActionType.ApplyStatusEffect); // 독립 상태 부여 행동의 기존 원본 설정
            nonAttack.Execute(); // 실제 상태만 주는 행동 실행
            Assert.AreEqual(nonAttack.Amount, ally.GetStatusEffect(BattleStatusEffectType.Poison)?.EffectiveValue); // 지속 약화의 원래 수치 보존
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 행동 종료
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 한 적 턴만 유지한 기존 표식 제거 확인
            Assert.AreEqual(1, foe.GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown).RemainingTurns); // 별도 피해 감소 수명 한 턴 남음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 적 턴 진입
            Assert.AreEqual(reduced, attack.AttackAmount); // 두 번째 적 턴에도 유지하는 비율 감소
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown)); // 두 적 턴 뒤 피해 약화 제거
            Assert.AreEqual(raw, attack.AttackAmount); // 기존 약화만 적용한 원래 직접 피해 복원
        } // 금빛 미끼의 대상과 예고 생성 종료
    } // 금빛 미끼의 실제 조우 종료

    private static BattleEnemyAction CreateAction(BattleUnitRuntime actor, BattleUnitRuntime target, EnemyActionType type) // 실제 직접 공격과 비피해 적 행동 생성
    { // 예정 행동 구성 시작
        bool attack = type == EnemyActionType.Attack; // 테스트에서 서로 다른 두 기본 행동 구분
        return new BattleEnemyAction(actor, target, type, attack ? BattleDamageType.Physical : BattleDamageType.None, attack ? 100 : 20, attack ? BattleStatusEffectType.None : BattleStatusEffectType.Poison, 3, 1, EnemyTargetRule.FirstLiving, "검사 행동", 0, 1, 3, 0, percentRoll: () => 0, battleType: BattleType.Normal); // 원래 예고 및 실행과 확정 난수 입력
    } // 예정 행동 구성 종료

    [UnityTest] // 기존 추가 비용 경로와 파티별 가장 오래된 약화 제거 검사
    public IEnumerator ContractBreachPaysBothCostsBeforeExistingGroupCleanse() // 행동력 부족과 정상 정화 처리 비교
    { // 계약 파기 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 클릭과 전투 실행 준비
        yield return PlayContractBreach(); // 실제 파티 약화와 지불 경계 검사
        yield return new ExitPlayMode(); // 실제 조우 및 손패 정리
    } // 계약 파기 검사 종료

    private static IEnumerator PlayContractBreach() // 기존 전체 정화의 실제 카드 사용과 결제 경계
    { // 계약 파기의 실제 조우 시작
        using (var fight = new Fight("CONTRACT_BREACH")) // 배포 계약 파기 한 장을 원래 손패에 등록
        { // 서로 다른 순서의 상태와 비용 준비 시작
            var owner = fight.Allies[0]; // 카드의 현재 시전자와 첫 제거 대상
            var partner = fight.Allies[1]; // 자기 순서의 첫 약화를 제거할 파티원
            var button = fight.View("CONTRACT_BREACH"); // 원래 클릭하는 실제 손패 화면
            var card = button.RuntimeCard; // 결제와 실제 이동을 판정할 카드 한 장
            Assert.AreEqual(4, card.ApCost); // 기존 고정 삼과 별도 행동력 일의 사용자 비용
            owner.ApplyStatusEffect(BattleStatusEffectType.Blind, 20, 4, 1); // 시전자의 가장 오래된 정화 가능 상태
            owner.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 4, 1); // 시전자의 나중에 적용된 상태 보존
            partner.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 20, 4, 1); // 동료의 가장 오래된 정화 가능 상태
            partner.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 4, 1); // 동료의 두 번째 독립 약화 보존
            Assert.IsTrue(fight.Points.Spend(17)); // 고정 원본만 결제 가능한 행동력 삼으로 시작
            button.OnPointerClick(null); // 부족한 총 행동력 상태에서 실제 클릭 시도
            Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 분리 결제로 기본 비용만 빠지는 경우 방지
            CollectionAssert.Contains(fight.Deck.Hand, card); // 지불하지 못한 실제 카드는 원래 손패에서 유지
            Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.Blind)); // 지불 전 효과 미발동 확인
            Assert.AreEqual(1, fight.Points.Gain(1)); // 추가 대가까지 한 번에 결제할 행동력 복구
            yield return fight.Use("CONTRACT_BREACH"); // 기존 전체 아군 정화 효과의 실제 카드 사용
            Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 고정 및 별도 비용의 전체 합 결제
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.Blind)); // 시전자의 첫 기존 약화 하나 제거
            Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.Bleed)); // 시전자의 이후 약화 보존
            Assert.IsNull(partner.GetStatusEffect(BattleStatusEffectType.HealingReceivedDown)); // 동료 각자의 첫 기존 약화 하나 제거
            Assert.IsNotNull(partner.GetStatusEffect(BattleStatusEffectType.Poison)); // 동료의 이후 약화 보존
            Assert.IsFalse(fight.Allies[2].HasRemovableDebuff); // 정화할 상태가 없던 파티원은 정상 상태 유지
        } // 서로 다른 순서의 상태와 비용 준비 종료
    } // 계약 파기의 실제 조우 종료
} // 펜타클 지원 검사 종료
