using System.Collections; // 전투 입력과 연출 대기
using System.Linq; // 실제 카드와 대상 조회
using NUnit.Framework; // 전투 결과 비교
using UnityEngine; // 실물 유물과 검 카드 조회
using UnityEngine.TestTools; // 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 손패 환경

public sealed class WinterfallRoyalSwordTests // 특수 검 일곱 장과 전설 검 한 장의 규칙 검사
{ // 검 카드 검사 시작
    [UnityTest] // 분노 해방과 칼날 폭주의 턴별 수치 확인
    public IEnumerator RageAndFrenzyApplyToTheOwnerForTheirPrintedTurns() // 두 턴 공격과 한 턴 검 피해 구별
    { // 강화 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 손패 시작
        using (var fight = new Fight("RAGE_RELEASE", "BLADE_FRENZY", "DOUBLE_SLASH")) // 실제 세 장의 보물 카드 구성
        { // 소유자 검사 시작
            var actor = fight.Allies[0]; // 검 소유자 조회
            actor.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 7, 6, 1); // 방어 감소 폭 비교 준비
            int defense = actor.PhysicalDefense; // 강화 전 실제 물리 방어 기록
            yield return fight.Use("RAGE_RELEASE"); // 분노 해방 사용
            Assert.AreEqual(6, actor.AttackPowerBonus); // 소유자 공격 여섯 증가 확인
            Assert.AreEqual(defense - 2, actor.PhysicalDefense); // 소유자 방어 둘 감소 확인
            Assert.AreEqual(0, fight.Allies[1].AttackPowerBonus); // 동료에게 강화 미지급 확인
            yield return fight.Use("BLADE_FRENZY"); // 이번 턴 검 피해 네 점 추가
            yield return fight.Use("DOUBLE_SLASH", 0); // 소유자의 실물 연타 카드 사용
            Assert.AreEqual(172, fight.Enemies[0].CurrentHealth); // 기본 사와 분노 육 및 폭주 사를 두 번 적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 진입
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.BladeFrenzy)); // 첫 턴 종료에 폭주 해제 확인
            Assert.AreEqual(6, actor.AttackPowerBonus); // 둘째 턴 분노 공격 유지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 둘째 아군 턴 종료
            Assert.AreEqual(0, actor.AttackPowerBonus); // 두 턴 뒤 공격 강화 종료
            Assert.AreEqual(defense, actor.PhysicalDefense); // 두 턴 뒤 방어 페널티 복구
        } // 소유자 검사 종료
        yield return new ExitPlayMode(); // 실제 전투 손패 종료
    } // 강화 검사 종료

    [UnityTest] // 결의의 즉시 지급과 다음 턴 예약 확인
    public IEnumerator ResolveGivesBlockNowAndAfterTheNormalBlockReset() // 사용 수량별 방어 지급과 전투불능 대상 제외
    { // 결의 검사 시작
        yield return new EnterPlayMode(); // 실제 파티 전투 시작
        using (var fight = new Fight("BATTLEFIELD_RESOLVE", "BATTLEFIELD_RESOLVE")) // 두 차례 사용으로 예약과 즉시 지급 비교
        { // 전투 방어 검사 시작
            var actor = fight.Allies[0]; // 생존 기준 아군 지정
            var inactive = fight.Allies[2]; // 전투불능 기준 아군 지정
            inactive.ApplyPersistentHealth(0); // 결의 사용 전 세 번째 아군 제외
            yield return fight.Use("BATTLEFIELD_RESOLVE"); // 첫 결의 사용
            Assert.AreEqual(4, actor.CurrentBlock); // 시전 직후 방어 네 점 지급
            Assert.AreEqual(2, actor.AttackPowerBonus); // 생존 아군 공격 둘 증가
            Assert.AreEqual(4, fight.Allies[1].CurrentBlock); // 다른 생존 아군에도 동일 방어 지급
            Assert.AreEqual(0, inactive.CurrentBlock); // 전투불능 아군의 즉시 지급 제외
            yield return fight.Use("BATTLEFIELD_RESOLVE"); // 두 번째 카드의 독립 지급 예약
            Assert.AreEqual(8, actor.CurrentBlock); // 사용 횟수에 따른 즉시 방어 여덟 확인
            actor.RemoveBlock(3); // 이번 방어도의 일부를 사용
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 시작
            Assert.AreEqual(8, actor.CurrentBlock); // 이전 방어도 초기화 뒤 예약한 네 점 두 번 지급
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.BattlefieldResolve)); // 다음 턴 지급 뒤 예약 제거
            Assert.AreEqual(2, actor.AttackPowerBonus); // 둘째 턴 공격 강화 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 셋째 아군 턴 시작
            Assert.AreEqual(0, actor.CurrentBlock); // 셋째 턴 반복 방어 지급 없음
            Assert.AreEqual(0, actor.AttackPowerBonus); // 두 턴 이후 공격 강화 해제
        } // 전투 방어 검사 종료
        yield return new ExitPlayMode(); // 실제 파티 전투 종료
    } // 결의 검사 종료

    [UnityTest] // 직접 처치와 행동력 회복 검사
    public IEnumerator ExecutionerRefundsExactlyOneOnlyForItsOwnLethalHit() // 일반 명중과 직접 처치 행동력 정산 비교
    { // 처치 검사 시작
        yield return new EnterPlayMode(); // 실제 검 입력 시작
        using (var fight = new Fight("EXECUTIONER_SWORD", "EXECUTIONER_SWORD")) // 비치명 타격과 직접 처치 두 장
        { // 행동력 검사 시작
            yield return fight.Use("EXECUTIONER_SWORD", 0); // 체력이 충분한 적에게 피해 이십
            Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 일반 명중에는 비용 셋 전부 소모
            var foe = fight.Enemies[1]; // 정확한 처치 피해를 받을 다음 적
            foe.TakeDamage(foe.CurrentHealth - 20, BattleDamageType.None); // 한 타격으로 쓰러질 적 체력 준비
            Assert.AreEqual(20, foe.CurrentHealth); // 적 체력 준비 확인
            var card = fight.View("EXECUTIONER_SWORD").RuntimeCard; // 이번에 사용할 원래 손패 객체
            fight.View("EXECUTIONER_SWORD").OnPointerClick(null); // 처단의 검 선택
            fight.EnemyViews[1].OnPointerClick(null); // 실제 적에게 공격 입력
            float deadline = Time.realtimeSinceStartup + 8f; // 연출 완료 대기 한도
            while (!fight.Deck.DiscardPile.Contains(card) && Time.realtimeSinceStartup < deadline) // 실제 카드 사용 정산 대기
            { // 프레임 대기 시작
                yield return null; // 다음 전투 프레임 대기
            } // 프레임 대기 종료
            Assert.IsTrue(foe.IsDead && fight.Deck.DiscardPile.Contains(card)); // 직접 처치와 한 장 소모 확인
            Assert.AreEqual(15, fight.Points.CurrentActionPoints); // 비용 셋 지불 후 직접 처치 행동력 하나 환급
        } // 행동력 검사 종료
        yield return new ExitPlayMode(); // 실제 검 입력 종료
    } // 처치 검사 종료

    [UnityTest] // 일반 광역과 출혈 대상 광역 구별
    public IEnumerator RiftAndBloodBackflowHitOnlyTheirLivingEligibleTargets() // 방어 감소와 출혈 선별 실제 피해 확인
    { // 광역 검사 시작
        yield return new EnterPlayMode(); // 실제 적 배치 시작
        using (var fight = new Fight("RIFT_SLASH", "BLOOD_BACKFLOW")) // 조건 없는 광역과 출혈 광역 비교
        { // 대상별 검사 시작
            foreach (var foe in fight.Enemies) // 실제 적 넷 순회
            { // 방어 설정 시작
                foe.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 7, 6, 1); // 방어 감소 검증을 위한 물리 방어 설정
            } // 방어 설정 종료
            var blood = fight.View("BLOOD_BACKFLOW").RuntimeCard; // 선택할 출혈 전용 광역 검
            fight.View("BLOOD_BACKFLOW").OnPointerClick(null); // 출혈이 없는 적 배치에 입력
            Assert.IsTrue(fight.Deck.Hand.Contains(blood)); // 조건 미충족 시 카드 미소모
            yield return fight.Use("RIFT_SLASH"); // 전체 적에게 피해 후 물리 방어 이점 감소
            Assert.IsTrue(fight.Enemies.All(unit => unit.CurrentHealth == 193 && unit.PhysicalDefense == 5)); // 각 적에게 현재 방어 반영 후 약화 확인
            fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1); // 첫 적 출혈
            fight.Enemies[3].ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1); // 마지막 적 출혈
            yield return fight.Use("BLOOD_BACKFLOW"); // 출혈 생존 적에게만 즉시 공격
            CollectionAssert.AreEqual(new[] { 184, 193, 193, 184 }, fight.Enemies.Select(unit => unit.CurrentHealth)); // 출혈 적에게만 방어 후 구점 피해 확인
            Assert.AreEqual(2, fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Bleed).RemainingTurns); // 즉시 공격의 출혈 지속 미소모
        } // 대상별 검사 종료
        yield return new ExitPlayMode(); // 실제 적 배치 종료
    } // 광역 검사 종료

    [UnityTest] // 일반 강화 하나만 해제하는 실제 단죄 검사
    public IEnumerator JudgmentRequiresARemovableBuffAndStripsOneAfterHitting() // 디버프만 가진 대상과 복수 강화 대상 비교
    { // 단죄 검사 시작
        yield return new EnterPlayMode(); // 실제 공격 대상 입력 시작
        using (var fight = new Fight("JUDGMENT_SLASH")) // 버프 있는 적 전용 공격 준비
        { // 강화 해제 검사 시작
            var target = fight.Enemies[1]; // 강화 두 개를 가질 적
            var card = fight.View("JUDGMENT_SLASH").RuntimeCard; // 단죄 원본 손패
            target.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1); // 디버프만 있는 적 준비
            fight.View("JUDGMENT_SLASH").OnPointerClick(null); // 제거할 강화가 없는 상태에서 입력
            Assert.IsTrue(fight.Deck.Hand.Contains(card)); // 디버프만으로 사용 불가 확인
            target.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 3, 1); // 오래된 일반 강화 등록
            target.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 6, 3, 1); // 두 번째 일반 강화 등록
            yield return fight.Use("JUDGMENT_SLASH", 1); // 대상 방어력을 먼저 적용하고 강화 제거
            Assert.AreEqual(180, target.CurrentHealth); // 해제 전 물리 방어로 감소한 피해 이십 확인
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 먼저 신청된 일반 강화 제거
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseUp)); // 두 번째 강화 보존
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.Bleed)); // 디버프 보존
        } // 강화 해제 검사 종료
        yield return new ExitPlayMode(); // 실제 공격 대상 입력 종료
    } // 단죄 검사 종료

    [UnityTest] // 왕의 참격의 후속 생존 대상과 동률 비교
    public IEnumerator KingChoosesMaxHealthAmongSurvivorsAfterTheFirstWave() // 최대 체력과 현재 체력의 구별 및 전투 앞순 확인
    { // 왕의 참격 검사 시작
        yield return new EnterPlayMode(); // 실제 전체 공격 시작
        using (var fight = new Fight("KINGS_SLASH")) // 원본 전설 검 카드 준비
        { // 대상 재선택 검사 시작
            var enemies = fight.Enemies; // 실제 화면 위치의 적 네 명
            enemies[0].ModifyMaxHealth(120); // 처음에는 가장 높은 최대 체력 설정
            enemies[0].TakeDamage(enemies[0].CurrentHealth - 15, BattleDamageType.None); // 전체 공격에서 쓰러질 적 체력 준비
            enemies[1].ModifyMaxHealth(50); // 동률의 앞쪽 적 설정
            enemies[2].ModifyMaxHealth(50); // 동률의 뒤쪽 적 설정
            enemies[1].TakeDamage(enemies[1].CurrentHealth - 50, BattleDamageType.None); // 앞쪽의 현재 체력을 뒤쪽보다 낮게 준비
            Assert.AreEqual(15, enemies[0].CurrentHealth); // 첫 적의 처치 조건 확인
            Assert.AreEqual(50, enemies[1].CurrentHealth); // 앞쪽 동률 적의 현재 체력 확인
            yield return fight.Use("KINGS_SLASH"); // 전체 공격 뒤 살아 있는 적 중 재선택
            CollectionAssert.AreEqual(new[] { 0, 20, 182, 182 }, enemies.Select(enemy => enemy.CurrentHealth)); // 사망 제외 후 최대 체력 동률의 앞순 적만 추가 십이 피해
        } // 대상 재선택 검사 종료
        yield return new ExitPlayMode(); // 실제 전체 공격 종료
    } // 왕의 참격 검사 종료

    [Test] // 두 신규 상태의 독립 원화와 도감 검사
    public void ResolveAndFrenzyUseTheirActualStatusIcons() // 원본 방어도와 검 피해 수치 확인
    { // 도감 확인 시작
        var entries = CodexData.Load(4); // 실제 도감 상태 분류 조회
        foreach (var effect in new[] { BattleStatusEffectType.BattlefieldResolve, BattleStatusEffectType.BladeFrenzy }) // 두 신규 상태 순회
        { // 원화 확인 시작
            var entry = entries.Single(item => item.Name == BattleStatusEffectInstance.GetDisplayName(effect)); // 공식 화면 이름의 상태 항목 조회
            Assert.AreSame(BattleStatusIconArt.Get(effect), entry.Image); // 전투와 도감의 같은 원화 확인
            Assert.AreEqual(effect + "StatusIcon", entry.Image.texture.name); // 각각의 독립 PNG 이름 확인
            StringAssert.Contains("4", entry.Description); // 실제 예약 방어도 또는 피해 네 점 확인
        } // 원화 확인 종료
    } // 도감 확인 종료
} // 검 카드 검사 종료
