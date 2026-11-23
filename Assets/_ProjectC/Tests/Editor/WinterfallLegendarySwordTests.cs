using System.Collections; // 전투 입력과 화면 갱신 대기
using System.Linq; // 무작위 공격의 전체 피해 조회
using NUnit.Framework; // 실제 전투 결과 비교
using UnityEngine.TestTools; // 플레이 모드 전투 검사
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드와 전투 화면 검사 환경

public sealed class WinterfallLegendarySwordTests // 오랜 겨울 검 전설 다섯 종의 실전 규칙 검사
{ // 전설 검 검사 시작
    [UnityTest] // 표식 대상과 세 턴 지속 검사
    public IEnumerator DuelIncreasesDamageOnlyAgainstTheChosenEnemyForThreePlayerTurns() // 일반 피해와 결투 피해 및 양쪽 만료 비교
    { // 결투 피해 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 전투 입력 시작
        using (var fight = new Fight("BLOODSTAINED_DUEL", "DOUBLE_SLASH", "DOUBLE_SLASH")) // 비교 대상과 같은 소유자의 검 카드 준비
        { // 결투 전투 준비 시작
            var owner = fight.Allies[0]; // 결투 시전자 조회
            var target = fight.Enemies[0]; // 표식 대상 조회
            yield return fight.Use("BLOODSTAINED_DUEL", 0); // 실제 손패에서 적을 지정해 결투 시작
            Assert.IsNull(fight.Allies[1].GetStatusEffect(BattleStatusEffectType.DuelMark)); // 다른 아군의 표식 제외 확인
            Assert.AreEqual(3, owner.GetStatusEffect(BattleStatusEffectType.DuelMark).RemainingTurns); // 시전 직후 아군의 세 턴 표시
            Assert.AreEqual(3, target.GetStatusEffect(BattleStatusEffectType.DuelMark).RemainingTurns); // 시전 직후 적의 같은 세 턴 표시
            yield return fight.Use("DOUBLE_SLASH", 0); // 표식의 두 개별 타격 처리
            yield return fight.Use("DOUBLE_SLASH", 1); // 같은 검으로 직접 지정하지 않은 적 공격
            Assert.AreEqual(190, target.CurrentHealth); // 기본 사 피해의 사십 퍼센트 증가를 타격별 내림
            Assert.AreEqual(192, fight.Enemies[1].CurrentHealth); // 표식 없는 적의 기본 피해 유지
            for (int remaining = 2; remaining > 0; remaining--) // 남은 두 아군 턴의 표식 동기화 확인
            { // 턴 전환 검사 시작
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 양쪽 상태의 동시 지속 시간 처리
                Assert.AreEqual(remaining, owner.GetStatusEffect(BattleStatusEffectType.DuelMark).RemainingTurns); // 시전자의 남은 턴 수
                Assert.AreEqual(remaining, target.GetStatusEffect(BattleStatusEffectType.DuelMark).RemainingTurns); // 지정 적의 같은 남은 턴 수
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 시작
                Assert.AreEqual(remaining, target.GetStatusEffect(BattleStatusEffectType.DuelMark).RemainingTurns); // 적 턴 종료의 중복 차감 방지
            } // 턴 전환 검사 종료
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 셋째 아군 턴 종료
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.DuelMark)); // 시전자의 만료 상태 정리
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.DuelMark)); // 적의 만료 상태 정리
        } // 결투 전투 정리
        yield return new ExitPlayMode(); // 실제 손패 검사 종료
    } // 결투 피해 검사 종료

    [UnityTest] // 상태 면역과 결투 교체 및 제거 검사
    public IEnumerator DuelReplacementAndCleansingMaintainOnePair() // 새 결투의 원자적 적용과 두 참가자 상태 정리
    { // 결투 교체 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 검사 시작
        using (var fight = new Fight("BLOODSTAINED_DUEL", "BLOODSTAINED_DUEL", "BLOODSTAINED_DUEL")) // 성공과 면역 및 재시전 준비
        { // 결투 전투 구성 시작
            var owner = fight.Allies[0]; // 결투 시전자 지정
            var first = fight.Enemies[0]; // 현재 결투 대상 지정
            var second = fight.Enemies[1]; // 면역과 새 결투 대상 지정
            yield return fight.Use("BLOODSTAINED_DUEL", 0); // 첫 양방향 표식 적용
            second.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 1, 1); // 새 결투 대상의 상태 면역 설정
            yield return fight.Use("BLOODSTAINED_DUEL", 1); // 표식을 막는 적에게 사용
            Assert.IsNotNull(first.GetStatusEffect(BattleStatusEffectType.DuelMark)); // 면역 시 기존 결투 유지
            Assert.IsNull(second.GetStatusEffect(BattleStatusEffectType.DuelMark)); // 면역 대상의 표식 부여 차단
            second.RemoveStatusEffect(BattleStatusEffectType.StatusImmunity); // 새 결투를 위한 면역 해제
            yield return fight.Use("BLOODSTAINED_DUEL", 1); // 새로운 대상과 결투 성립
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.DuelMark)); // 이전 상대 표식 정리
            var mark = owner.GetStatusEffect(BattleStatusEffectType.DuelMark); // 새 결투의 아군 표식 조회
            Assert.IsFalse(BattleDebuffTransfer.CanTransfer(owner, fight.Allies[1], mark)); // 제삼자에게 연결 표식 이전 차단
            Assert.IsTrue(second.RemoveStatusEffect(BattleStatusEffectType.DuelMark)); // 연결 상대의 표식 해제
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.DuelMark)); // 지워진 표식의 반대쪽 정리
        } // 결투 전투 정리
        yield return new ExitPlayMode(); // 실제 손패 검사 종료
    } // 결투 교체 검사 종료

    [UnityTest] // 자세의 비용 할인과 개별 타격 검사
    public IEnumerator StanceDiscountsSwordCardsAndAddsFourDamageToEveryHitForThreeTurns() // 소유자의 실제 행동력과 피해량 및 만료 검사
    { // 검성의 자세 검사 시작
        yield return new EnterPlayMode(); // 실제 검 카드 손패 준비
        using (var fight = new Fight("SWORD_SAINT_STANCE", "DOUBLE_SLASH")) // 소유자의 강화와 연타 원본 지정
        { // 자세 전투 준비 시작
            var actor = fight.Allies[0]; // 강화 소유자 조회
            int normalCost = fight.View("DOUBLE_SLASH").RuntimeCard.ApCost; // 자세 적용 전 비용 저장
            yield return fight.Use("SWORD_SAINT_STANCE"); // 자신에게 자세 부여
            Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 자세 자체의 원래 비용 삼 지불
            Assert.IsNull(fight.Allies[1].GetStatusEffect(BattleStatusEffectType.SwordSaintStance)); // 강화 소유자의 동료 제외 확인
            Assert.AreEqual(normalCost - 1, fight.View("DOUBLE_SLASH").RuntimeCard.ApCost); // 손패 카드의 행동력 할인
            yield return fight.Use("DOUBLE_SLASH", 0); // 실제 할인 비용으로 두 타격 사용
            Assert.AreEqual(18 - normalCost, fight.Points.CurrentActionPoints); // 연타 카드의 게시된 비용 지불
            Assert.AreEqual(184, fight.Enemies[0].CurrentHealth); // 각 타격에 사 피해 가산
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 둘째 아군 턴 시작
            Assert.AreEqual(2, actor.GetStatusEffect(BattleStatusEffectType.SwordSaintStance).RemainingTurns); // 적용 턴의 직접 차감
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 셋째 아군 턴 시작
            Assert.AreEqual(1, actor.GetStatusEffect(BattleStatusEffectType.SwordSaintStance).RemainingTurns); // 셋째 턴의 마지막 할인 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 넷째 아군 턴 시작
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.SwordSaintStance)); // 세 턴 뒤 자세 만료
            Assert.AreEqual(normalCost, fight.View("DOUBLE_SLASH").RuntimeCard.ApCost); // 만료 뒤 실제 손패 비용 복구
        } // 자세 전투 정리
        yield return new ExitPlayMode(); // 실제 손패 검사 종료
    } // 검성의 자세 검사 종료

    [UnityTest] // 처치 후 추가 행동 한 번과 전역 연타 횟수 검사
    public IEnumerator BreakthroughContinuesOnceAndFlurryStrikesSevenTimes() // 적 배치와 추가 비용 및 무작위 독립 피해 확인
    { // 직접 검 공격 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 검사 시작
        using (var fight = new Fight("LAST_BREAKTHROUGH", "LAST_BREAKTHROUGH", "HEAVENLY_SWORD_FLURRY")) // 각 공격의 보물 원본 준비
        { // 직접 공격 전투 준비 시작
            yield return fight.Use("HEAVENLY_SWORD_FLURRY"); // 생존한 적들 사이에서 각 타격 다시 추첨
            Assert.AreEqual(49, fight.Enemies.Sum(enemy => 200 - enemy.CurrentHealth)); // 일곱 점 피해를 일곱 번 가한 합계
            Assert.IsTrue(fight.Enemies.All(enemy => (200 - enemy.CurrentHealth) % 7 == 0)); // 각 적의 독립 타격 단위 확인
            int previous = fight.Enemies[3].CurrentHealth; // 일반 명중을 비교할 적의 현재 체력
            yield return fight.Use("LAST_BREAKTHROUGH", 3); // 쓰러지지 않는 적에게 최초 공격
            Assert.AreEqual(previous - 35, fight.Enemies[3].CurrentHealth); // 공격의 기본 물리 피해 확인
            for (int index = 0; index < 2; index++) // 배치에서 바로 이어지는 두 적의 체력 정리
            { // 직접 처치 대상 준비 시작
                var enemy = fight.Enemies[index]; // 실제 전투 배치의 적 조회
                enemy.TakeDamage(enemy.CurrentHealth - 35, BattleDamageType.None); // 두 적을 같은 한 타격 체력으로 설정
            } // 직접 처치 대상 준비 종료
            int thirdHealth = fight.Enemies[2].CurrentHealth; // 두 번째 후속 타격의 금지 여부 비교용 체력
            yield return fight.Use("LAST_BREAKTHROUGH", 0); // 첫 적을 직접 처치해 바로 다음 적 추가 공격
            Assert.IsTrue(fight.Enemies[0].IsDead && fight.Enemies[1].IsDead); // 직접 처치와 배치 다음 적의 추가 처치 확인
            Assert.AreEqual(thirdHealth, fight.Enemies[2].CurrentHealth); // 두 번째 처치의 추가 연쇄 제한
            Assert.AreEqual(5, fight.Points.CurrentActionPoints); // 각 카드의 비용 다섯만 세 번 처리
        } // 직접 공격 전투 정리
        yield return new ExitPlayMode(); // 실제 손패 검사 종료
    } // 직접 검 공격 검사 종료

    [UnityTest] // 자신에게 치명적인 체력 비용 차단 검사
    public IEnumerator DoomSeveranceSpendsHealthThroughBlockOnlyWhenTheOwnerCanSurvive() // 체력 경계와 행동력 및 원래 피해 비교
    { // 체력 비용 검사 시작
        yield return new EnterPlayMode(); // 실제 파멸 절단 손패 준비
        using (var fight = new Fight("DOOM_SEVERANCE")) // 보물에서 획득하는 실제 카드 구성
        { // 체력 비용 전투 준비 시작
            var owner = fight.Allies[0]; // 체력을 지불할 소유자 조회
            owner.TakeDamage(owner.CurrentHealth - 15, BattleDamageType.None); // 실제 피격으로 사용 불가 경계 체력 설정
            fight.View("DOOM_SEVERANCE").OnPointerClick(null); // 체력이 부족한 상태에서 카드 입력
            fight.EnemyViews[0].OnPointerClick(null); // 대상 선택 시도
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 거부된 사용의 행동력 보존
            Assert.AreEqual(200, fight.Enemies[0].CurrentHealth); // 체력 부족 공격의 피해 차단
            Assert.AreEqual(1, owner.RestoreHealth(1)); // 실제 회복으로 사용 가능 상태 갱신
            owner.GainBlock(5); // 체력 비용이 방어도와 별개인지 확인
            yield return fight.Use("DOOM_SEVERANCE", 0); // 실제 비용을 내고 적 공격
            Assert.AreEqual(1, owner.CurrentHealth); // 체력 비용 정확히 열다섯 지불
            Assert.AreEqual(5, owner.CurrentBlock); // 체력 비용으로 소모되지 않은 방어도
            Assert.AreEqual(14, fight.Points.CurrentActionPoints); // 행동력 비용 육 지불
            Assert.AreEqual(150, fight.Enemies[0].CurrentHealth); // 물리 피해 오십 적용
        } // 체력 비용 전투 정리
        yield return new ExitPlayMode(); // 실제 손패 검사 종료
    } // 체력 비용 검사 종료
} // 전설 검 검사 종료
