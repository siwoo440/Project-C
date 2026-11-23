using System.Collections; // 실제 카드 연출과 턴 진행 대기
using System.Linq; // 지정 아군 화면과 손패 확인
using NUnit.Framework; // 카드 효과와 턴 경계 검증
using TMPro; // 실제 툴팁 효과 문구 조회
using UnityEngine; // 임시 툴팁 객체 생성
using UnityEngine.TestTools; // 플레이 모드 전환과 프레임 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 카드 검증 환경 재사용
using Object = UnityEngine.Object; // 임시 화면 정리

public sealed class WinterfallLegendaryShieldCardTests // 특수 및 전설 방패 카드 실제 입력 검사
{ // 방패 카드 검사 시작
    [UnityTest] // 철갑 돌진의 독립 피해와 시전자 방어 확인
    public IEnumerator IroncladRushGrantsOneBlockBonusOnHitBlockOrMiss() // 직접 명중과 완전 방어 및 회피 비교
    { // 철갑 돌진 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 클릭과 충돌 환경 진입
        yield return IroncladRushGrantsOneBlockBonusOnHitBlockOrMissInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 철갑 돌진 검사 종료

    private IEnumerator IroncladRushGrantsOneBlockBonusOnHitBlockOrMissInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("IRONCLAD_RUSH", "IRONCLAD_RUSH", "IRONCLAD_RUSH")) // 실제 보물 카드 원본의 별개 사용 세 번
        { // 피해와 방어도 검증 시작
            var owner = fight.Allies[0]; // 철갑 돌진의 실제 카드 소유자
            int expected = owner.ModifyOutgoingDamage(16); // 소유자 정신 상태를 반영한 원래 타격 피해
            int initialBlock = owner.CurrentBlock; // 행동 전 소유자의 기준 방어도
            AssertTooltip(fight.View("IRONCLAD_RUSH").RuntimeCard, "물리 피해 16", "자신 방어도 +10"); // 동적 피해와 독립 방어 문구 확인
            yield return fight.Use("IRONCLAD_RUSH", 0); // 선택 적에게 첫 직접 명중
            Assert.AreEqual(fight.Enemies[0].MaxHealth - expected, fight.Enemies[0].CurrentHealth); // 생존 적에게 기본 물리 피해 적용
            Assert.AreEqual(initialBlock + 10, owner.CurrentBlock); // 첫 사용에서 소유자 방어도 한 번 증가
            fight.Enemies[1].GainBlock(30); // 다음 피해를 모두 흡수할 적 방어도
            yield return fight.Use("IRONCLAD_RUSH", 1); // 피해를 방어하는 적에 두 번째 사용
            Assert.AreEqual(fight.Enemies[1].MaxHealth, fight.Enemies[1].CurrentHealth); // 적 방어도가 전부 흡수한 체력 피해 확인
            Assert.AreEqual(30 - expected, fight.Enemies[1].CurrentBlock); // 실제 공격 피해량에 맞춘 적 방어도 소모
            Assert.AreEqual(initialBlock + 20, owner.CurrentBlock); // 막힌 타격과 독립적인 시전자 방어 지급 확인
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 미리 정한 회피 확률의 적 대상
            yield return fight.Use("IRONCLAD_RUSH", 2); // 회피 대상에게 세 번째 사용
            Assert.AreEqual(fight.Enemies[2].MaxHealth, fight.Enemies[2].CurrentHealth); // 빗나간 공격 피해 없음 확인
            Assert.AreEqual(initialBlock + 30, owner.CurrentBlock); // 빗나감과 관계없는 애초의 자체 방어 한 번 적용
        } // 피해와 방어도 검증 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 요새화의 세 적 턴과 불굴의 전열 공포 해제 검사
    public IEnumerator FortifyAndIndomitableFrontAffectLivingAlliesWithIndependentDurations() // 생존 아군 강화와 지정 디버프만 제거 확인
    { // 아군 전체 방어 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 클릭과 턴 진행 환경 진입
        yield return FortifyAndIndomitableFrontAffectLivingAlliesWithIndependentDurationsInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 아군 전체 방어 검사 종료

    private IEnumerator FortifyAndIndomitableFrontAffectLivingAlliesWithIndependentDurationsInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("FORTIFY", "INDOMITABLE_FRONT")) // 실제 전체 방어 카드 두 장으로 전투 구성
        { // 방어 강화와 공포 해제 검증 시작
            var owner = fight.Allies[0]; // 두 방패 카드의 실제 사용자
            var ally = fight.Allies[1]; // 공포와 기존 마법 피해 경감의 비교 대상
            var dead = fight.Allies[2]; // 전체 효과에서 제외할 전투불능 대상
            dead.ApplyPersistentHealth(0); // 제삼 아군의 전투불능 적용
            ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 4, 1); // 선택적으로 제거할 공포 적용
            ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 4, 1); // 공포 제거 후 남아야 할 별도 디버프 적용
            ally.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, 15, 1, 1); // 먼저 만료해야 할 마법 피해 경감 준비
            AssertTooltip(fight.View("FORTIFY").RuntimeCard, "물리 방어력 +3", "마법 피해 -10% (3턴)"); // 독립 저항과 실제 지속 툴팁 확인
            yield return fight.Use("FORTIFY"); // 전투 화면에서 실제 아군 전체 강화 사용
            Assert.AreEqual(owner.BasePhysicalDefense + 3, owner.PhysicalDefense); // 사용자 물리 방어력 증가 확인
            Assert.AreEqual(ally.BasePhysicalDefense + 3, ally.PhysicalDefense); // 동료 물리 방어력 증가 확인
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.FortifyResistance)); // 전투불능 대상 강화 제외 확인
            AssertTooltip(fight.View("INDOMITABLE_FRONT").RuntimeCard, "아군 각각 방어도 +10", "공포 해제"); // 전체 방어와 공포만 제거하는 문구 확인
            yield return fight.Use("INDOMITABLE_FRONT"); // 생존 아군에게 방어와 공포 제거 실행
            Assert.AreEqual(10, owner.CurrentBlock); // 소유자에게 개별 방어도 열 점 확인
            Assert.AreEqual(10, ally.CurrentBlock); // 동료에게 개별 방어도 열 점 확인
            Assert.AreEqual(0, dead.CurrentBlock); // 전투불능 대상 방어도 미적용 확인
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Fear)); // 공포 상태 제거 확인
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Bleed)); // 지정 외 디버프 보존 확인
            var enemy = fight.Enemies[0]; // 마법 피해 예고의 적 공격자
            int baseDamage = ally.PreviewDamage(100, BattleDamageType.Magical, enemy).FinalDamage; // 적 직접 경감 적용 전의 기존 마법 저항 반영 피해
            for (int turn = 0; turn < 3; turn++) // 실제 적 턴 세 번의 요새화 유지 확인
            { // 적 턴 보호 기간 검사 시작
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 이번 보호가 적용될 적 턴 시작
                Assert.AreEqual(ally.BasePhysicalDefense + 3, ally.PhysicalDefense); // 적 직접 피격 시점의 물리 방어력 증가 확인
                int magic = turn == 0 ? baseDamage * 90 * 85 / 10000 : baseDamage * 90 / 100; // 첫 적 턴에만 기존 저항 자세 추가
                Assert.AreEqual(magic, ally.PreviewDamage(100, BattleDamageType.Magical, enemy, enemyDirectAttack: true).FinalDamage); // 요새화와 일시 저항의 독립 수명 확인
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 한 적 턴 종료 처리
            } // 적 턴 보호 기간 검사 종료
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 재사용 없이 네 번째 적 턴 시작
            Assert.AreEqual(ally.BasePhysicalDefense, ally.PhysicalDefense); // 세 적 턴 이후 물리 방어 복원 확인
            Assert.AreEqual(baseDamage, ally.PreviewDamage(100, BattleDamageType.Magical, enemy, enemyDirectAttack: true).FinalDamage); // 세 적 턴 이후 마법 피해 경감 만료 확인
        } // 방어 강화와 공포 해제 검증 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 나머지 지원 카드의 대상과 다음 턴 행동력 검사
    public IEnumerator GuardAndBulwarkCardsUseExpectedTargetsAndGiantsShieldPaysNextTurn() // 실제 방어 대상과 가지별 발동량 검사
    { // 방패 지원 카드 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 턴 전환 환경 진입
        yield return GuardAndBulwarkCardsUseExpectedTargetsAndGiantsShieldPaysNextTurnInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 방패 지원 카드 검사 종료

    private IEnumerator GuardAndBulwarkCardsUseExpectedTargetsAndGiantsShieldPaysNextTurnInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("ABSOLUTE_GUARD", "RETRIBUTION_BULWARK", "DISTRIBUTED_DEFENSE", "IMPENETRABLE", "GIANTS_SHIELD")) // 남은 방패 카드의 정식 보상 원본 준비
        { // 아군 보호와 예약 효과 검증 시작
            var owner = fight.Allies[0]; // 모든 카드의 실제 사용 아군
            var ally = fight.Allies[1]; // 절대 가드로 선택할 동료
            var dead = fight.Allies[2]; // 전체 카드의 제외 아군
            dead.ApplyPersistentHealth(0); // 사망한 대상의 무효화 및 방어도 미적용 확인 준비
            var allyView = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == ally); // 선택 동료 화면 조회
            var absolute = fight.View("ABSOLUTE_GUARD").RuntimeCard; // 절대 가드의 실제 손패 카드 조회
            AssertTooltip(absolute, "다음 피해 1회 무효", "방어도 유지"); // 기간 없이 다음 피해의 표시 문구 확인
            yield return WinterfallCommonCardExpansionTests.Play(absolute.SourceData, fight.Hand, allyView, fight.Runner, fight.Deck, fight.Points); // 절대 가드와 동료 화면을 차례로 실제 클릭
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 선택하지 않은 시전자에게 보호 미적용
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 선택한 아군에게 한 번 피해 무효 적용
            AssertTooltip(fight.View("RETRIBUTION_BULWARK").RuntimeCard, "고정 피해 5"); // 응징 방벽의 현재 고정 반격 수치 표시
            yield return fight.Use("RETRIBUTION_BULWARK"); // 응징 방벽을 실제 전체 아군에게 적용
            AssertTooltip(fight.View("DISTRIBUTED_DEFENSE").RuntimeCard, "광역 공격 피해 -35% (2턴)"); // 분산 방어의 대상 공격과 기간 확인
            yield return fight.Use("DISTRIBUTED_DEFENSE"); // 분산 방어를 실제 전체 아군에게 적용
            AssertTooltip(fight.View("IMPENETRABLE").RuntimeCard, "아군 각각 방어도 +12", "다음 피해 1회 무효"); // 개별 방어도와 피해 무효 표시 확인
            yield return fight.Use("IMPENETRABLE"); // 난공불락 전체 방어 카드 사용
            foreach (var target in fight.Allies.Take(2)) // 두 생존 아군의 개별 상태와 방어도 확인
            { // 생존 아군 효과 검증 시작
                Assert.AreEqual(12, target.CurrentBlock); // 전체 공유가 아닌 한 아군당 열두 방어도 확인
                Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.DamageNullification)?.StackCount); // 절대 가드가 있던 대상도 보호 한 번 확인
                Assert.AreEqual(5, target.GetStatusEffect(BattleStatusEffectType.RetributionBulwark)?.EffectiveValue); // 생존 아군에게 응징 방벽 반격 값 확인
                Assert.AreEqual(35, target.GetStatusEffect(BattleStatusEffectType.DistributedDefense)?.EffectiveValue); // 생존 아군에게 광역 피해 경감 값 확인
            } // 생존 아군 효과 검증 종료
            Assert.AreEqual(0, dead.CurrentBlock); // 사망 아군에게 난공불락 방어도 미지급
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 사망 아군에게 난공불락 보호 미적용
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.RetributionBulwark)); // 사망 아군에게 응징 상태 미적용
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.DistributedDefense)); // 사망 아군에게 광역 방어 미적용
            int before = fight.Points.CurrentActionPoints; // 거인의 방패 사용 직전 공용 행동력
            AssertTooltip(fight.View("GIANTS_SHIELD").RuntimeCard, "자신 방어도 +40", "다음 아군 턴 공용 행동력 -1"); // 자체 방어와 지연 비용 표시 확인
            yield return fight.Use("GIANTS_SHIELD"); // 거인의 방패로 시전자 방어와 지연 행동력 처리
            Assert.AreEqual(52, owner.CurrentBlock); // 기존 방어도에 시전자 방어도 마흔 추가
            Assert.AreEqual(12, ally.CurrentBlock); // 거인의 방패가 지정 외 아군에게 적용되지 않음 확인
            Assert.AreEqual(before - 4, fight.Points.CurrentActionPoints); // 현재 턴에는 카드 사용 비용만 차감 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 다음 아군 턴까지 진행
            Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 다음 아군 턴에 공용 행동력 한 점 감소 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 지연 비용 사용 뒤 한 턴 진행
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 후속 아군 턴의 정상 회복 확인
        } // 아군 보호와 예약 효과 검증 종료
    } // 플레이 모드 본문 종료

    private static void AssertTooltip(CardInstance card, params string[] fragments) // 기존 전투 상세 화면의 실제 효과 문구 확인
    { // 카드 툴팁 비교 시작
        var root = new GameObject("ShieldTestTooltip", typeof(RectTransform), typeof(BattleCardTooltipView)); // 검사에 사용할 실제 카드 상세 화면
        try // 검사 화면 정리 보장
        { // 툴팁 문구 확인 시작
            root.GetComponent<BattleCardTooltipView>().Show(card); // 사용 전 손패 카드의 동적 상세 표시
            string text = root.transform.Find("DetailText").GetComponent<TMP_Text>().text; // 효과와 비용이 있는 실제 툴팁 문구 조회
            foreach (string fragment in fragments) // 기대한 효과와 발동 조건 순회
            { // 개별 상세 문구 비교 시작
                StringAssert.Contains(fragment, text); // 현재 실제 카드의 효과 설명 일치 확인
            } // 개별 상세 문구 비교 종료
        } // 툴팁 문구 확인 종료
        finally // 임시 상세 화면 제거 보장
        { // 툴팁 정리 시작
            Object.Destroy(root); // 독립 검사 화면 정리
        } // 툴팁 정리 종료
    } // 카드 툴팁 비교 종료
} // 방패 카드 검사 종료
