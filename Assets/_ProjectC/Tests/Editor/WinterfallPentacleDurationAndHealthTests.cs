using System.Collections; // 실제 선택과 완료 연출 대기
using System.Linq; // 선택한 실제 적과 아군 화면 조회
using NUnit.Framework; // 네이티브 피해 및 지속과 비용 비교
using UnityEngine; // 기존 유물 보유 자료 로딩
using UnityEngine.TestTools; // 실제 플레이 모드 전환 검사
using Fight = WinterfallSwordCombatTests.Fight; // 원래 전투 카드와 손패 경로의 공용 검사

public sealed class WinterfallPentacleDurationAndHealthTests // 275일차 펜타클 세 지원 카드 회귀 검사
{ // 지원 카드의 실제 전투 검사 시작
    [UnityTest] // 원래 취약의 수치와 새 카드의 적 턴 지속 검증
    public IEnumerator WeaknessAnalysisUsesNativeVulnerabilityWithoutOpeningRelicShorteningIt() // 기존 검은 왕관과 함께 적용한 취약 검사
    { // 실제 취약과 적 턴 검증 시작
        yield return new EnterPlayMode(); // 실제 보물 카드와 전투의 일반 입력 환경
        yield return VerifyWeaknessAnalysis(); // 첫 아군 턴에서 두 번의 적 턴 끝까지 추적
        yield return new ExitPlayMode(); // 유물과 임시 전투가 정리된 편집 모드 복귀
    } // 실제 취약과 적 턴 검증 종료

    private static IEnumerator VerifyWeaknessAnalysis() // 같은 기존 취약의 보유 유물 연장과 상태 면역
    { // 실제 카드 취약과 출처 처리 검사 시작
        var relics = RelicRunManager.EnsureInstance(); // 실제 탐사 유물의 보유 관리자
        var crown = Resources.Load<RelicData>("Relics/BlackCrown"); // 기존 첫 아군 턴의 같은 취약 유물 원본
        Assert.IsNotNull(crown); // 원래 취약 유물의 출시 자료 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(crown)); // 기존 유물을 정상 경로로 실제 보유
        try // 현재 검사에서 추가한 실제 유물 제거 보장
        { // 같은 취약과 추가 면역 비교 시작
            using var fight = new Fight("WEAKNESS_ANALYSIS", "WEAKNESS_ANALYSIS"); // 실제 등록 두 카드와 생존 적
            int index = System.Array.FindIndex(fight.Enemies, enemy => enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable) != null); // 무작위 왕관의 실제 대상 위치
            Assert.GreaterOrEqual(index, 0); // 원래 유물의 같은 상태가 실제로 존재하는 경계
            var target = fight.Enemies[index]; // 새 카드도 같은 적에게 쓸 대상
            Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.Vulnerable)?.RemainingTurns); // 원래 아군 첫 턴 한정
            int immuneIndex = (index + 1) % fight.Enemies.Length; // 실제 다른 생존 적 위치
            var immune = fight.Enemies[immuneIndex]; // 취약을 차단해야 하는 실제 적
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 디버프 면역 부여
            yield return fight.Use("WEAKNESS_ANALYSIS", immuneIndex); // 상태 차단을 원래 입력으로 요청
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 기존 취약의 원래 디버프 면역 유지
            yield return fight.Use("WEAKNESS_ANALYSIS", index); // 검은 왕관이 적용된 적에게 같은 네이티브 취약 사용
            var vulnerability = target.GetStatusEffect(BattleStatusEffectType.Vulnerable); // 실제 적용된 네이티브 객체
            Assert.AreEqual(20, vulnerability?.EffectiveValue); // 기존 취약과 중첩하지 않는 원본 이십 퍼센트
            Assert.AreEqual(2, vulnerability.RemainingTurns); // 실제 두 적 턴의 초기 기간
            var preview = target.PreviewDamage(20, BattleDamageType.None, fight.Allies[0]); // 기존 공용 피해 계산의 순수 예고
            var actual = target.TakeDamage(20, BattleDamageType.None, fight.Allies[0]); // 같은 공용 피해의 실제 타격
            Assert.AreEqual(24, preview.AppliedDamage); // 이십의 이십 퍼센트 증가 정확히 적용
            Assert.AreEqual(preview.AppliedDamage, actual.AppliedDamage); // 현행 예고와 실제 피해 계산 일치
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 아군 턴 종료와 기존 검은 왕관의 정리
            Assert.AreSame(vulnerability, target.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 새 카드가 만든 실제 연장 보존
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 턴 끝
            Assert.AreEqual(1, vulnerability.RemainingTurns); // 원래 적 상태 시계에서 정확히 한 칸 차감
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 적 턴에 진입
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 두 적 턴 뒤 원래 취약 해제
        } // 같은 취약과 추가 면역 비교 종료
        finally // 실패 여부와 무관하게 보유 상태 원상 복구
        { // 현재 검사에 추가한 유물 정리
            relics.TryRemove(crown.RelicId); // 원래 탐사 보유에서 이번 검사용 왕관 제거
        } // 현재 검사에 추가한 유물 정리 종료
    } // 실제 카드 취약과 출처 처리 검사 종료

    [UnityTest] // 실제 턴제 강화의 기간만 변경하는 증폭 검사
    public IEnumerator RuneAmplificationExtendsOnlySelectedAlliesIndependentTimedBuffs() // 활성 기간제와 특수 소유자 상태 비교
    { // 증폭의 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 원래 아군 선택과 상태 등록 환경
        yield return VerifyRuneAmplification(); // 소모 없이 상태가 있는 실제 아군만 고르는지 검사
        yield return new ExitPlayMode(); // 증폭 검사 후 편집 모드 복귀
    } // 증폭의 실제 전투 검사 종료

    private static IEnumerator VerifyRuneAmplification() // 실제 선택과 두 네이티브 기간 및 제외 상태 검사
    { // 증폭 카드와 선택 대상 준비 시작
        using var fight = new Fight("RUNE_AMPLIFICATION"); // 실제 출시 원본 카드 한 장
        var first = fight.View("RUNE_AMPLIFICATION"); // 일반 지급으로 추가된 실제 손패 카드
        var target = fight.Allies[1]; // 증폭할 다른 생존 아군
        int before = fight.Points.CurrentActionPoints; // 후보 없는 상태의 최초 공용 행동력
        first.OnPointerClick(null); // 상태 강화가 하나도 없을 때 실제 손패 클릭
        yield return null; // 원래 손패 UI 갱신에 한 프레임 제공
        Assert.IsTrue(fight.Deck.Hand.Contains(first.RuntimeCard)); // 후보가 없으면 원래 카드 보존
        Assert.AreEqual(before, fight.Points.CurrentActionPoints); // 후보가 없는 선택 시점의 비용 없음
        target.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 3, 2, 2); // 첫 실제 기간제 공격력 강화
        target.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 4, 2, 2); // 기존 수치와 두 중첩 확보
        target.ApplyStatusEffect(BattleStatusEffectType.ArcaneWard, 2, 2, 1); // 기간제 특수 마력 보호막도 확인
        target.ApplyStatusEffect(BattleStatusEffectType.Blind, 5, 2, 1); // 실제 강화가 아닌 원래 약화
        target.ApplyStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp, 4, 1, 1); // 턴이 아니라 다음 실제 타격을 기다리는 강화
        target.ApplyStatusEffect(BattleStatusEffectType.DistributionAttackUp, 8, 1, 1); // 실제 전투 종료까지의 영속 강화
        var attack = target.GetStatusEffect(BattleStatusEffectType.AttackPowerUp); // 연장하는 동일 공격력 객체
        var ward = target.GetStatusEffect(BattleStatusEffectType.ArcaneWard); // 실제 적 턴에서 감소할 동일 방어 객체
        var view = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(item => item.RuntimeUnit == target); // 실제 선택 대상의 화면
        yield return WinterfallCommonCardExpansionTests.Play(first.RuntimeCard.SourceData, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 카드와 대상 유닛의 실제 입력
        Assert.AreSame(attack, target.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 기존 중첩 인스턴스 갱신 여부 확인
        Assert.AreEqual(3, attack.RemainingTurns); // 기존 두 턴에서 공격 강화 한 턴 연장
        Assert.AreEqual(8, attack.EffectiveValue); // 공격 수치와 두 중첩에 변화 없음
        Assert.AreEqual(2, attack.StackCount); // 재적용으로 중첩된 것이 아닌 기간 연장
        Assert.AreEqual(3, ward.RemainingTurns); // 실제 두 번째 기간제 강화도 각각 한 턴 연장
        Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.Blind)?.RemainingTurns); // 원래 적 약화에 연장 미적용
        Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)?.RemainingTurns); // 회수 조건의 원래 한 번 상태 값 유지
        Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.DistributionAttackUp)?.RemainingTurns); // 전투 수명에 영향을 받는 원래 유지 값 보존
    } // 증폭 카드와 선택 대상 준비 종료

    [UnityTest] // 실제 체력과 공용 손패 비용의 원래 경계
    public IEnumerator TemporaryContractRequiresSurvivingSixHealthAndDrawsTwoOnce() // 여섯과 일곱 체력의 실제 경계 확인
    { // 임시 계약의 비용과 드로우 검사 시작
        yield return new EnterPlayMode(); // 공용 손패와 시전자 체력 원본의 실제 적용
        yield return VerifyTemporaryContract(); // 초과 비용 방지와 두 카드 지급을 실제 입력으로 확인
        yield return new ExitPlayMode(); // 전투 후 편집 모드 복귀
    } // 임시 계약의 비용과 드로우 검사 종료

    private static IEnumerator VerifyTemporaryContract() // 결제와 순환되는 두 실물 카드의 독립 경계
    { // 실제 비용 지불 검사 시작
        using var fight = new Fight("TEMPORARY_CONTRACT", "EMERGENCY_FUNDS", "THRUST"); // 실제 계약 원본과 뽑을 두 카드
        var contractView = fight.View("TEMPORARY_CONTRACT"); // 현재 보유한 계약의 실제 카드 입력
        var contract = contractView.RuntimeCard; // 손패에서 정산 위치를 추적할 원래 카드
        var alternatives = fight.Deck.Hand.Where(card => card != contract).ToArray(); // 비용과 별개인 두 번의 실제 드로우 후보
        Assert.AreEqual(2, alternatives.Length); // 초기 손패의 계약 외 두 실물 확인
        foreach (var card in alternatives) // 정상 버림을 거쳐 뽑을 두 원래 후보
        { // 현재 손패에서 뽑기 원본을 준비하는 단계
            Assert.IsTrue(fight.Deck.DiscardCard(card)); // 실제 공용 버림과 재순환 준비
        } // 현재 손패에서 뽑기 원본 준비 끝
        var owner = fight.Allies[0]; // 두 비용 경계를 지나는 실제 소유 아군
        int points = fight.Points.CurrentActionPoints; // 기존 원래 공용 행동력
        Assert.IsTrue(owner.ApplyPersistentHealth(6)); // 생존을 남기지 않는 경계값
        fight.View("TEMPORARY_CONTRACT").OnPointerClick(null); // 실제 손패 클릭으로 비용 경계 확인
        yield return null; // 현재 턴의 입력 처리 후 보존 결과 조회
        Assert.AreEqual(6, owner.CurrentHealth); // 사용하지 않은 소유자의 실제 현재 체력
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 체력 부족 시 행동력도 소모하지 않는 경계
        Assert.IsTrue(fight.Deck.Hand.Contains(contract)); // 체력 부족 시 사용 카드도 원래 손패
        Assert.AreEqual(2, fight.Deck.DiscardPile.Count); // 첫 무효 입력이 드로우도 만들지 않는 조건
        Assert.AreEqual(1, owner.RestoreHealth(1)); // 회복 알림으로 실제 손패와 생존 경계 갱신
        yield return fight.Use("TEMPORARY_CONTRACT"); // 정상 카드 사용과 공용 드로우 진행
        Assert.AreEqual(1, owner.CurrentHealth); // 딱 여섯 점 지불하고 실제 생존
        Assert.AreEqual(points - 2, fight.Points.CurrentActionPoints); // 원본의 기본 행동력 두 점
        CollectionAssert.AreEquivalent(alternatives, fight.Deck.Hand); // 두 실제 카드 모두 기존 손패 원본으로 복원
        CollectionAssert.AreEqual(new[] { contract }, fight.Deck.DiscardPile); // 현재 사용 카드 자체가 새 드로우에 끼지 않는 결과
    } // 실제 비용 지불 검사 종료
} // 지원 카드의 실제 전투 검사 종료
