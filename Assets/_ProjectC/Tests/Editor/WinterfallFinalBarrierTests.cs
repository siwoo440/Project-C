using System.Collections; // 실제 손패와 연출 대기
using NUnit.Framework; // 전설 방패 효과 대조
using UnityEngine; // 손패 사용 가능 표시 조회
using UnityEngine.TestTools; // 실제 전투 입력 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 기존 카드 입력 검사 환경 재사용

public sealed class WinterfallFinalBarrierTests // 최후 방벽과 영원의 방패 실제 입력 검사
{ // 전설 방패 검사 시작
    [UnityTest] // 파티의 개별 체력과 실제 행동력 확인
    public IEnumerator FinalRampartRequiresEveryLivingAllyAtOrBelowThirtyPercent() // 체력 경계와 각각의 방어도 적용 확인
    { // 최후 방벽 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력이 작동하는 전투 환경
        yield return FinalRampartRequiresEveryLivingAllyAtOrBelowThirtyPercentInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 최후 방벽 검사 환경 종료
    } // 최후 방벽 검사 종료

    private IEnumerator FinalRampartRequiresEveryLivingAllyAtOrBelowThirtyPercentInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("FINAL_RAMPART")) // 실제 보상 카드 원본으로 검사 덱 구성
        { // 체력 조건과 입력 검사 시작
            var caster = fight.Allies[0]; // 최후 방벽의 실제 소유자
            var partner = fight.Allies[1]; // 서로 다른 최대 체력의 검사 동료
            var dead = fight.Allies[2]; // 효과에서 제외할 전투불능 아군
            caster.ModifyMaxHealth(100 - caster.MaxHealth); // 정확한 삼십 퍼센트 경계를 갖춘 소유자
            partner.ModifyMaxHealth(120 - partner.MaxHealth); // 자기 최대 체력에 따라 다른 동료 경계
            caster.ApplyPersistentHealth(30); // 소유자의 정확한 체력 경계 준비
            partner.ApplyPersistentHealth(37); // 동료 기준 삼십 퍼센트보다 한 점 높은 체력
            dead.ApplyPersistentHealth(0); // 사망한 동료의 실제 전투 제외
            var view = fight.View("FINAL_RAMPART"); // 실제 손패에 놓인 전설 카드 화면
            var card = view.RuntimeCard; // 실패한 사용 뒤에도 남아야 할 개별 손패 카드
            var state = view.GetComponent<CanvasGroup>(); // 손패 카드의 실제 활성 상태 표시
            int before = fight.Points.CurrentActionPoints; // 시도 전 실제 공용 행동력 보관
            Assert.AreEqual(5, card.ApCost); // 원본 카드의 실제 기본 행동력 비용
            fight.Hand.RefreshCardAvailability(); // 개별 현재 체력에 맞는 활성 상태 갱신
            Assert.Less(state.alpha, 1f); // 단 한 명이 체력 경계를 넘을 때 손패 사용 불가 표시
            view.SetInteractable(true); // 지연된 화면 입력으로 컨트롤러 재검사를 직접 유도
            view.OnPointerClick(null); // 실제 카드 입력 호출로 조건 재검사
            Assert.AreEqual(before, fight.Points.CurrentActionPoints); // 조건 미달 사용에서 행동력 보존
            CollectionAssert.Contains(fight.Deck.Hand, card); // 조건 미달 사용에서 손패 카드 보존
            Assert.AreEqual(0, caster.CurrentBlock); // 사용 실패에서 방어도 미지급 확인
            partner.ApplyPersistentHealth(36); // 둘째 아군도 자기 체력의 정확한 삼십 퍼센트
            fight.Hand.RefreshCardAvailability(); // 두 생존자 경계 체력의 상태 갱신
            Assert.AreEqual(1f, state.alpha); // 정확한 경계에서 손패 사용 가능 표시
            partner.RestoreHealth(int.MaxValue); // 초과 회복을 요청하는 실제 체력 회복 경로
            Assert.AreEqual(120, partner.CurrentHealth); // 원래 최대 체력에서 초과 회복 제한 확인
            Assert.Less(state.alpha, 1f); // 실제 회복 직후 조건 해제와 손패 갱신 확인
            partner.ApplyPersistentHealth(36); // 방어도 지급 비교의 생존 체력 재설정
            fight.Hand.RefreshCardAvailability(); // 정확한 경계의 최종 손패 사용 상태 확인
            yield return fight.Use("FINAL_RAMPART"); // 유효한 현재 파티에서 실제 카드 사용
            Assert.AreEqual(before - 5, fight.Points.CurrentActionPoints); // 성공한 한 번에만 행동력 다섯 차감
            Assert.AreEqual(25, caster.CurrentBlock); // 시전자에게 독립 방어도 스물다섯
            Assert.AreEqual(25, partner.CurrentBlock); // 생존 동료에게 독립 방어도 스물다섯
            Assert.AreEqual(0, dead.CurrentBlock); // 전투불능 아군의 방어도 보존
            Assert.AreEqual(30, caster.CurrentHealth); // 방어도 카드 사용 후 소유자 체력 보존
            Assert.AreEqual(36, partner.CurrentHealth); // 방어도 카드 사용 후 동료 체력 보존
        } // 체력 조건과 입력 검사 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 기존 경감과 별도 지속 시간을 사용한 단일 계산 검증
    public IEnumerator EternalShieldUsesTwoIndependentFactorsAcrossThreeEnemyTurns() // 광역 마법과 단일 물리의 예고 및 실제 경감 확인
    { // 영원의 방패 검사 시작
        yield return new EnterPlayMode(); // 실제 모든 생존 아군을 대상으로 하는 입력 환경
        yield return EternalShieldUsesTwoIndependentFactorsAcrossThreeEnemyTurnsInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 영원의 방패 검사 환경 종료
    } // 영원의 방패 검사 종료

    private IEnumerator EternalShieldUsesTwoIndependentFactorsAcrossThreeEnemyTurnsInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("ETERNAL_SHIELD")) // 실제 영원의 방패 보상 원본 준비
        { // 삼 턴 방어 검사 시작
            var first = fight.Allies[0]; // 실제 카드 소유자와 물리 방어 비교 대상
            var ally = fight.Allies[1]; // 다른 피해 경감도 보유할 생존 대상
            var dead = fight.Allies[2]; // 대상에서 제외할 전투불능 아군
            var enemy = fight.Enemies[0]; // 실제 적 진영 공격자
            dead.ApplyPersistentHealth(0); // 전투불능 상태에 방어 상태가 없도록 준비
            int initialPoints = fight.Points.CurrentActionPoints; // 카드 사용 전 실제 행동력 조회
            Assert.AreEqual(6, fight.View("ETERNAL_SHIELD").RuntimeCard.ApCost); // 원본 카드의 행동력 여섯 확인
            yield return fight.Use("ETERNAL_SHIELD"); // 실제 아군 전체 방어 카드 입력
            Assert.AreEqual(initialPoints - 6, fight.Points.CurrentActionPoints); // 실제 한 번의 사용 비용 확인
            Assert.AreEqual(30, first.GetStatusEffect(BattleStatusEffectType.EternalShield)?.EffectiveValue); // 생존 시전자에게 직접 피해 경감 확인
            Assert.AreEqual(20, first.GetStatusEffect(BattleStatusEffectType.EternalShieldResistance)?.EffectiveValue); // 생존 시전자에게 독립 마법 경감 확인
            Assert.AreEqual(30, ally.GetStatusEffect(BattleStatusEffectType.EternalShield)?.EffectiveValue); // 생존 동료에게 별개 보호 상태 확인
            Assert.AreEqual(20, ally.GetStatusEffect(BattleStatusEffectType.EternalShieldResistance)?.EffectiveValue); // 생존 동료에게 별개 저항 상태 확인
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.EternalShield)); // 사망 아군의 직접 피해 상태 미적용 확인
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.EternalShieldResistance)); // 사망 아군의 마법 저항 상태 미적용 확인
            ally.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, 15, 1, 1); // 한 적 턴만 적용할 기존 마법 자세
            ally.ApplyStatusEffect(BattleStatusEffectType.DistributedDefense, 35, 2, 1); // 두 적 턴만 적용할 기존 광역 방어
            int originalPhysical = first.PreviewDamage(100, BattleDamageType.Physical, enemy).FinalDamage; // 직접 경감 전 기초 물리 피해
            Assert.AreEqual(originalPhysical * 70 / 100, first.PreviewDamage(100, BattleDamageType.Physical, enemy, enemyDirectAttack: true).FinalDamage); // 마법과 별개의 물리 직접 피해 삼십 퍼센트 경감
            for (int round = 0; round < 4; round++) // 보호되는 세 적 턴과 해제 뒤 다음 적 턴 진행
            { // 한 적 턴의 예고와 실제 공격 대조 시작
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 광역 공격이 실행될 실제 적 턴 진행
                var attack = new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Magical, 100, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.AllLiving, "영원 방어 검사", 1, 1, 1, 0); // 마법 광역 의도를 갖는 실제 공격 구성
                int basis = ally.PreviewDamage(attack.AttackAmount, BattleDamageType.Magical, enemy).FinalDamage; // 기존 저항력만 적용한 기초 마법 피해
                int directFactor = round < 3 ? 70 : 100; // 영원의 방패의 남은 직접 피해 비율
                int magicFactor = round < 3 ? 80 : 100; // 영원의 방패의 남은 독립 마법 피해 비율
                int stanceFactor = round == 0 ? 85 : 100; // 저항 자세의 한 적 턴 기간 반영
                int areaFactor = round < 2 ? 65 : 100; // 분산 방어의 두 적 턴 기간 반영
                int expected = (int)((long)basis * directFactor * magicFactor * stanceFactor * areaFactor / 100_000_000); // 각 효과를 곱한 뒤 한 번만 내린 실제 예상 피해
                ally.GainBlock(200); // 실제 방어도 흡수 전에 모든 경감이 적용되는 비교 조건
                Assert.AreEqual(expected, attack.PreviewDamage(ally).FinalDamage); // 실제 광역 예정 공격의 경감 피해 검증
                var result = attack.Execute().DamageResult; // 같은 광역 공격의 실제 피격 결과
                Assert.AreEqual(expected, result.AbsorbedDamage); // 실제 피해가 경감된 뒤 방어도에서 흡수 확인
                Assert.AreEqual(0, result.AppliedDamage); // 시험 방어도에 의한 전량 흡수 확인
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 기존 방어 상태의 독립 적 턴 종료 처리
            } // 한 적 턴의 예고와 실제 공격 대조 종료
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.EternalShield)); // 세 적 턴 뒤 직접 피해 경감 만료 확인
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.EternalShieldResistance)); // 세 적 턴 뒤 별도 마법 경감 만료 확인
        } // 삼 턴 방어 검사 종료
    } // 플레이 모드 본문 종료
} // 전설 방패 검사 종료
