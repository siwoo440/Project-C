using System.Collections; // 실제 전투 프레임 대기
using System.Linq; // 소유자와 적 상태 조회
using NUnit.Framework; // 전투 결과 대조
using UnityEngine; // 게임 난수와 원본 조회
using UnityEngine.TestTools; // 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 검 카드의 실제 손패 입력 환경

public sealed class WinterfallSwordMasteryTests // 검 카드 여덟 종의 비용과 범위 및 상태 검사
{ // 검 카드 검사 시작
    [UnityTest] // 체력 비용과 두 턴 검 피해 증가 검사
    public IEnumerator BloodOathCostsFiveAndEndsAfterTwoPlayerTurns() // 생존 비용과 중복 없는 검 피해 확인
    { // 맹세 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 시작
        using (var fight = new Fight("BLOOD_OATH", "DOUBLE_SLASH", "BLOOD_OATH")) // 원본 맹세와 두 타격 검 준비
        { // 손패 검사 시작
            var actor = fight.Allies[0]; // 사용할 카드의 원래 소유자 조회
            actor.ApplyPersistentHealth(5); // 비용 지불 후 생존할 수 없는 체력
            var original = fight.View("BLOOD_OATH").RuntimeCard; // 입력 전 원래 맹세 손패
            fight.View("BLOOD_OATH").OnPointerClick(null); // 생존 조건을 만족하지 않는 맹세 사용
            Assert.IsTrue(fight.Deck.Hand.Contains(original)); // 원래 카드의 손패 보존 확인
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 실패한 행동력 지불 방지
            actor.ApplyPersistentHealth(11); // 두 번의 비용 뒤 생존할 체력 준비
            fight.Hand.RefreshCardAvailability(); // 바뀐 체력의 손패 입력 반영
            yield return fight.Use("BLOOD_OATH"); // 실제 체력 다섯 지불
            Assert.AreEqual(6, actor.CurrentHealth); // 방어도와 별도인 체력 비용 확인
            yield return fight.Use("BLOOD_OATH"); // 같은 맹세의 지속 갱신
            Assert.AreEqual(1, actor.CurrentHealth); // 마지막 한 점 생존 조건 확인
            Assert.AreEqual(5, actor.GetStatusEffect(BattleStatusEffectType.BloodOath).EffectiveValue); // 피해 중첩 없이 다섯 유지 확인
            yield return fight.Use("DOUBLE_SLASH", 0); // 같은 소유자의 독립 두 타격
            Assert.AreEqual(182, fight.Enemies[0].CurrentHealth); // 각 타격 사에 맹세 오 합산 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 아군 턴 진입
            Assert.AreEqual(1, actor.GetStatusEffect(BattleStatusEffectType.BloodOath).RemainingTurns); // 두 번째 턴에 맹세 잔여 한 턴 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 아군 턴 종료
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.BloodOath)); // 두 턴 뒤 맹세 소멸 확인
        } // 손패 검사 종료
        yield return new ExitPlayMode(); // 실제 전투 종료
    } // 맹세 검사 종료

    [UnityTest] // 세 칸 및 한 가로열 범위 비교
    public IEnumerator WhirlwindKeepsFixedSlotsAndBreakthroughHitsAllLivingInTheRow() // 죽은 인접 칸 너머와 전체 행 구별
    { // 범위 검사 시작
        yield return new EnterPlayMode(); // 실제 공격 입력 시작
        using (var fight = new Fight("BLADE_WHIRLWIND", "BLADE_WHIRLWIND", "BREAKTHROUGH_SLASH")) // 서로 다른 세 칸과 전체 관통 공격 준비
        { // 위치 검사 시작
            yield return fight.Use("BLADE_WHIRLWIND", 1); // 선택한 둘째 칸과 좌우 인접 칸 공격
            CollectionAssert.AreEqual(new[] { 193, 193, 193, 200 }, fight.Enemies.Select(unit => unit.CurrentHealth)); // 처음 세 칸만 각각 일곱 피해 확인
            fight.Enemies[1].TakeDamage(999, BattleDamageType.None); // 선택할 셋째 칸의 왼쪽 이웃 제거
            yield return fight.Use("BLADE_WHIRLWIND", 2); // 사망 칸을 건너뛰지 않는 범위 공격
            CollectionAssert.AreEqual(new[] { 193, 0, 186, 193 }, fight.Enemies.Select(unit => unit.CurrentHealth)); // 첫째 칸까지 범위가 확장되지 않음 확인
            yield return fight.Use("BREAKTHROUGH_SLASH"); // 가로열 전체 적 공격
            CollectionAssert.AreEqual(new[] { 179, 0, 172, 179 }, fight.Enemies.Select(unit => unit.CurrentHealth)); // 생존한 모든 칸에 정확히 열네 피해 확인
        } // 위치 검사 종료
        yield return new ExitPlayMode(); // 실제 공격 입력 종료
    } // 범위 검사 종료

    [UnityTest] // 타격별 절반 체력과 단일 출혈 대상 조건 검사
    public IEnumerator FinishingStanceRechecksEachHitAndBloodTrailRequiresRealBleed() // 체력과 출혈 변화에 따른 실제 손패 선택 확인
    { // 조건부 공격 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 화면 시작
        using (var fight = new Fight("FINISHING_STANCE", "DOUBLE_SLASH", "BLOOD_TRAIL")) // 마무리와 추적 대상 비교
        { // 대상 확인 시작
            var foe = fight.Enemies[0]; // 최대 체력 이백의 적 조회
            foe.TakeDamage(99, BattleDamageType.None); // 정확히 절반보다 한 점 많은 체력 준비
            yield return fight.Use("FINISHING_STANCE"); // 이번 턴 소유자의 마무리 자세 부여
            yield return fight.Use("DOUBLE_SLASH", 0); // 첫 타격 뒤 절반 이하로 내려가는 적 공격
            Assert.AreEqual(92, foe.CurrentHealth); // 첫 피해 사와 두 번째 강화 피해 오 확인
            var pursuit = fight.View("BLOOD_TRAIL").RuntimeCard; // 실제 피의 추적 카드 보관
            fight.View("BLOOD_TRAIL").OnPointerClick(null); // 출혈 대상이 하나도 없을 때 입력
            Assert.IsTrue(fight.Deck.Hand.Contains(pursuit)); // 대상 부재로 손패 보존 확인
            foe.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1); // 첫 적에게 실제 출혈 부여
            fight.View("BLOOD_TRAIL").OnPointerClick(null); // 출혈 후보가 생긴 실제 카드 선택
            fight.EnemyViews[1].OnPointerClick(null); // 출혈 없는 다른 적에 무효 입력
            Assert.IsTrue(fight.Deck.Hand.Contains(pursuit)); // 잘못된 대상에는 카드 미소모 확인
            fight.View("BLOOD_TRAIL").OnPointerClick(null); // 남은 선택 표시 해제
            yield return fight.Use("BLOOD_TRAIL", 0); // 출혈 상태인 실제 대상 공격
            Assert.AreEqual(72, foe.CurrentHealth); // 체력 절반과 출혈 조건의 피해 이십 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 효과를 사용한 아군 턴 종료
            Assert.IsNull(fight.Allies[0].GetStatusEffect(BattleStatusEffectType.FinishingStance)); // 마무리 자세의 한 턴 만료 확인
        } // 대상 확인 종료
        yield return new ExitPlayMode(); // 실제 손패 화면 종료
    } // 조건부 공격 검사 종료

    [UnityTest] // 공포의 원본 확률과 회피·실명 분리 검사
    public IEnumerator FearStopsTargetEvasionAndKeepsAttackerBlindness() // 적의 회피만 막히는 실제 카드 타격 확인
    { // 공포 검사 시작
        yield return new EnterPlayMode(); // 실제 명중 판정 시작
        using (var fight = new Fight("INTIMIDATING_CHARGE", "INTIMIDATING_CHARGE", "SLASH", "SLASH")) // 공포 성공과 실패 및 후속 공격 준비
        { // 공포 전투 시작
            var foe = fight.Enemies[0]; // 공포를 받을 실제 적 조회
            int roll = 35; // 삼십오 퍼센트의 첫 실패 경계
            fight.Controller.CardUsed += (card, targets) => Random.InitState(SeedFor(roll)); // 실제 충돌 전에 확률 경계 재현
            yield return fight.Use("INTIMIDATING_CHARGE", 0); // 실패 확률에서 일반 타격
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.Fear)); // 경계 삼십오에서 공포 미부여
            roll = 34; // 삼십오 퍼센트의 마지막 성공 경계
            yield return fight.Use("INTIMIDATING_CHARGE", 0); // 명중 뒤 공포 발동
            Assert.AreEqual(1, foe.GetStatusEffect(BattleStatusEffectType.Fear).RemainingTurns); // 한 턴 공포 부여 확인
            foe.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 공포가 막을 대상 회피 준비
            Assert.AreEqual(100, BattleHitChance.GetPercent(fight.Allies[0], foe)); // 대상의 회피 보정 무효 확인
            yield return fight.Use("SLASH", 0); // 공포 상태인 적의 회피를 무시한 카드
            Assert.AreEqual(178, foe.CurrentHealth); // 돌진 두 번과 베기 한 타격 확인
            fight.Allies[0].ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 1, 1); // 공격자 쪽의 독립된 실명 부여
            Assert.AreEqual(0, BattleHitChance.GetPercent(fight.Allies[0], foe)); // 공격자 실명은 그대로 적용
            yield return fight.Use("SLASH", 0); // 공포 대상이라도 실명으로 빗나가는 공격
            Assert.AreEqual(178, foe.CurrentHealth); // 실명 타격의 실제 체력 피해 없음 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 공포 대상의 자기 턴 한 번 종료
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.Fear)); // 공포 한 턴 만료 확인
        } // 공포 전투 종료
        yield return new ExitPlayMode(); // 실제 명중 판정 종료
    } // 공포 검사 종료

    [UnityTest] // 물리 방어 감소와 대상 회복량의 두 턴 검사
    public IEnumerator DefenseBreakAndSeveringStrikeUseTheTargetsActualStats() // 물리 방어와 받는 회복량의 시간 경계 확인
    { // 약화 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 시작
        using (var fight = new Fight("DEFENSE_BREAK", "SEVERING_STRIKE")) // 서로 다른 두 약화의 원본 준비
        { // 약화 전투 시작
            var foe = fight.Enemies[0]; // 단일 적 대상 조회
            foe.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 7, 5, 1); // 원래 물리 방어 일곱 준비
            yield return fight.Use("DEFENSE_BREAK", 0); // 공격 뒤 두 턴 물리 방어 하락
            Assert.AreEqual(4, foe.PhysicalDefense); // 일곱에서 셋 감소 확인
            yield return fight.Use("SEVERING_STRIKE", 0); // 공격 뒤 대상의 받는 회복 감소
            Assert.AreEqual(4, foe.RestoreHealth(9)); // 아홉 회복의 절반 내림 확인
            Assert.AreEqual(0, foe.RestoreHealth(1)); // 체력 한 점 회복의 내림 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료
            Assert.AreEqual(4, foe.RestoreHealth(9)); // 두 번째 적 턴에도 감소 적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
            Assert.AreEqual(7, foe.PhysicalDefense); // 물리 방어 약화 종료 확인
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.HealingReceivedDown)); // 받는 회복 감소 해제 확인
            Assert.AreEqual(9, foe.RestoreHealth(9)); // 약화 종료 후 원래 회복량 적용
        } // 약화 전투 종료
        yield return new ExitPlayMode(); // 실제 전투 종료
    } // 약화 검사 종료

    [Test] // 신규 네 상태의 도감과 전투 이미지 검사
    public void SwordStatusesHaveDistinctArtAndTheirSheetDescriptions() // 원본 컷오프와 효과의 실제 도감 연결 확인
    { // 상태 표시 검사 시작
        var entries = CodexData.Load(4); // 실제 상태 도감 열기
        var effects = new[] { BattleStatusEffectType.BloodOath, BattleStatusEffectType.Fear, BattleStatusEffectType.FinishingStance, BattleStatusEffectType.HealingReceivedDown }; // 새 네 상태 목록
        foreach (var effect in effects) // 실제 상태와 도감 연결 순회
        { // 상태별 검사 시작
            var entry = entries.Single(item => item.Name == BattleStatusEffectInstance.GetDisplayName(effect)); // 표시 이름에 맞는 실제 항목 조회
            Assert.AreSame(BattleStatusIconArt.Get(effect), entry.Image); // 같은 전투 상태 원화 확인
            Assert.AreEqual(effect + "StatusIcon", entry.Image.texture.name); // 네 상태의 서로 다른 파일명 확인
            Assert.IsNotEmpty(entry.Description); // 도감의 실제 효과 설명 확인
        } // 상태별 검사 종료
    } // 상태 표시 검사 종료
    private static int SeedFor(int value) // 실제 게임 난수에서 지정 경계 시드 조회
    { // 시드 검색 시작
        for (int seed = 0; seed < 5000; seed++) // 작은 양의 시드 후보 순회
        { // 후보 확인 시작
            Random.InitState(seed); // 해당 시드로 게임 난수 초기화
            if (Random.Range(0, 100) == value) // 첫 확률 값 일치 확인
            { // 일치 처리 시작
                return seed; // 충돌 전에 적용할 시드 반환
            } // 일치 처리 종료
        } // 후보 확인 종료
        Assert.Fail("공포 경계 검사 시드 없음"); // 불완전한 확률 검사 중단
        return 0; // 검사 실패 후 기본 반환
    } // 시드 검색 종료
} // 검 카드 검사 종료
