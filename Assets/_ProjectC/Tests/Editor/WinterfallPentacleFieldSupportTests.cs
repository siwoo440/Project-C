using System.Collections; // 카드 연출 대기
using System.Linq; // 진영 전체의 실제 값 검사
using NUnit.Framework; // 기대 값과 실제 값 비교
using UnityEngine; // 실제 저항을 가진 적 원본 생성
using UnityEngine.TestTools; // 플레이 모드 전환
using static WinterfallSwordCombatTests; // 기존 화면 입력 검사 사용

public sealed class WinterfallPentacleFieldSupportTests // 펜타클의 진영 지원 카드 검사
{ // 지원 카드 검사 시작
    [UnityTest] // 화면에서 전장 지도 사용 검사
    public IEnumerator BattlefieldMapMarksOneEnemyTurnAndRaisesHitForTwoAllyTurns() // 두 진영의 독립 지속과 실제 명중 비교
    { // 전장 지도 검사 시작
        yield return new EnterPlayMode(); // 실제 입력과 상태 처리 시작
        yield return PlayBattlefieldMap(); // 표식 시계와 아군 명중률 검사
        yield return new ExitPlayMode(); // 플레이 모드 정리
    } // 전장 지도 검사 종료

    private static IEnumerator PlayBattlefieldMap() // 두 진영의 실제 사용과 만료 처리
    { // 지도 전투 검사 시작
        using (var fight = new Fight("BATTLEFIELD_MAP")) // 실제 보상 원본으로 손패 구성
        { // 지도 전투 준비 시작
            var ally = fight.Allies[0]; // 실제 카드 소유자
            var foe = fight.Enemies[0]; // 회피를 붙일 살아 있는 적
            ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 30, 4, 1); // 아군의 지속 명중 불이익
            foe.ApplyStatusEffect(BattleStatusEffectType.Evasion, 25, 4, 1); // 적의 기존 회피
            fight.Allies[2].ApplyPersistentHealth(0); // 전투불능 아군의 지원 제외
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 4, 1); // 기존 면역으로 표식 차단
            fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 전투불능 적의 표식 제외
            Assert.AreEqual(45, BattleHitChance.GetPercent(ally, foe)); // 기존 실명과 회피 합산 확인
            yield return fight.Use("BATTLEFIELD_MAP"); // 적 전체에 실제 카드 사용
            Assert.AreEqual(55, BattleHitChance.GetPercent(ally, foe)); // 같은 조건에서 실제 명중 십 점 상승
            Assert.IsTrue(BattleHitChance.Hits(ally, 54, foe)); // 강화된 명중 경계의 마지막 성공
            Assert.IsFalse(BattleHitChance.Hits(ally, 55, foe)); // 강화된 명중 경계의 첫 실패
            Assert.AreEqual(100, BattleHitChance.GetPercent(fight.Allies[1])); // 패널티 없는 아군의 백 상한 유지
            Assert.AreEqual(2, fight.Allies[1].GetStatusEffect(BattleStatusEffectType.BattlefieldMapAccuracyUp).RemainingTurns); // 현재 살아 있는 다른 아군도 강화
            Assert.IsNull(fight.Allies[2].GetStatusEffect(BattleStatusEffectType.BattlefieldMapAccuracyUp)); // 전투불능 대상에 강화 없음
            Assert.AreEqual(1, foe.GetStatusEffect(BattleStatusEffectType.PublicMark).RemainingTurns); // 첫 생존 적의 적 턴 표식
            Assert.AreEqual(1, fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.PublicMark).RemainingTurns); // 두 번째 생존 적의 적 턴 표식
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.PublicMark)); // 기존 면역을 가진 적 보호
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.PublicMark)); // 전투불능 적에 표식 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 아군 종료
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.BattlefieldMapAccuracyUp).RemainingTurns); // 시전 턴부터 두 차례 시작
            Assert.IsNotNull(foe.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 첫 적 턴 도중 표식 보존
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 첫 적 턴 뒤 표식 만료
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.BattlefieldMapAccuracyUp).RemainingTurns); // 적 턴 동안 아군 강화 남은 시간 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 아군 종료
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.BattlefieldMapAccuracyUp)); // 두 번째 아군 종료에 강화 만료
        } // 지도 전투 준비 종료
    } // 지도 전투 검사 종료

    [UnityTest] // 실제 적 한 명을 선택하는 룬 봉인 검사
    public IEnumerator RuneSealRemovesOldestActiveBuffAndIndependentlyWeakensResistance() // 강화 해제 여부와 별개인 마법 저항 확인
    { // 룬 봉인 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 시작
        yield return PlayRuneSeal(); // 고른 적과 기존 면역 및 두 종료 시계 검증
        yield return new ExitPlayMode(); // 플레이 모드 정리
    } // 룬 봉인 검사 종료

    private static IEnumerator PlayRuneSeal() // 실제 룬 봉인 선택 결과 검사
    { // 룬 봉인 전투 시작
        var enemy = ScriptableObject.CreateInstance<EnemyData>(); // 수치 있는 적의 독립 원본
        JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"magicalResistance\":30}", enemy); // 실제 저항 감소 이전 기준
        try // 원본 자산 정리 보장
        { // 적 원본 사용 시작
            using (var fight = Fight.WithEnemySources(Enumerable.Repeat(enemy, 4), "RUNE_SEAL", "RUNE_SEAL", "RUNE_SEAL")) // 원본 세 장과 충분한 적 구성
            { // 지정 적 검사 시작
                var foe = fight.Enemies[0]; // 정상 해제와 약화를 받을 적
                var unbuffed = fight.Enemies[1]; // 강화 없이 저항만 떨어질 적
                var immune = fight.Enemies[2]; // 제거와 면역이 별개인 적
                foe.ApplyStatusEffect(BattleStatusEffectType.Blind, 10, 4, 1); // 가장 오래된 상태이나 해제 대상이 아닌 약화
                foe.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 5, 4, 1); // 해제할 첫 활성 강화
                foe.ApplyStatusEffect(BattleStatusEffectType.Evasion, 15, 4, 1); // 첫 강화 뒤의 보존 대상
                immune.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 5, 4, 1); // 면역과 별개로 지울 가장 오래된 강화
                immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 4, 1); // 나중에 추가한 별도 디버프 면역
                yield return fight.Use("RUNE_SEAL", 0); // 첫 실제 적 한 명 선택
                Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 가장 오래된 해제 가능 강화 제거
                Assert.IsNotNull(foe.GetStatusEffect(BattleStatusEffectType.Blind)); // 약화 보존
                Assert.IsNotNull(foe.GetStatusEffect(BattleStatusEffectType.Evasion)); // 두 번째 강화 보존
                Assert.AreEqual(20, foe.MagicalResistance); // 기본 삼십에서 십 점 저항 감소
                Assert.AreEqual(30, unbuffed.MagicalResistance); // 아직 고르지 않은 적의 저항 보존
                yield return fight.Use("RUNE_SEAL", 1); // 버프가 없는 적도 선택 가능
                Assert.AreEqual(20, unbuffed.MagicalResistance); // 제거 실패 여부와 별개인 유효 저항 감소
                yield return fight.Use("RUNE_SEAL", 2); // 다른 버프가 먼저 들어간 면역 적 선택
                Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 면역을 우회하는 기존 강화 해제
                Assert.IsNotNull(immune.GetStatusEffect(BattleStatusEffectType.StatusImmunity)); // 두 번째 면역 강화 보존
                Assert.AreEqual(30, immune.MagicalResistance); // 남은 면역으로 새 저항 약화 차단
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 적 턴 시작
                Assert.AreEqual(2, foe.GetStatusEffect(BattleStatusEffectType.MagicalResistanceDown).RemainingTurns); // 첫 적 턴 진행 중 이 턴 유지
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료
                Assert.AreEqual(1, foe.GetStatusEffect(BattleStatusEffectType.MagicalResistanceDown).RemainingTurns); // 남은 한 적 턴 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 적 턴 시작
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
                Assert.AreEqual(30, foe.MagicalResistance); // 두 적 턴 뒤 원래 저항 복구
            } // 지정 적 검사 종료
        } // 적 원본 사용 종료
        finally // 검사 적 원본 정리 시작
        { // 판정 뒤 임시 객체 해제 시작
            Object.Destroy(enemy); // 검사에서 생성한 임시 적 원본 정리
        } // 판정 뒤 임시 객체 해제 종료
    } // 룬 봉인 전투 종료

    [UnityTest] // 실제 아군 전체 재분배와 방어도 검사
    public IEnumerator RedistributionConservesLivingHealthAndAvoidsDamageOrHealingTriggers() // 최대치와 정수 나머지 및 기존 방어도 경로 확인
    { // 재분배 검사 시작
        yield return new EnterPlayMode(); // 게임의 원래 카드 입력 시작
        yield return PlayRedistribution(); // 두 생존 명단에서 체력과 방어도를 확인
        yield return new ExitPlayMode(); // 플레이 모드 정리
    } // 재분배 검사 종료

    private static IEnumerator PlayRedistribution() // 실제 체력의 정수 재분배 검사
    { // 재분배 전투 검사 시작
        using (var fight = new Fight("REDISTRIBUTION", "REDISTRIBUTION")) // 같은 손패의 실제 원본 두 장
        { // 체력 검사 준비 시작
            int[] caps = { 8, 30, 40 }; // 최대치가 서로 다른 생존 동료
            int[] health = { 8, 30, 1 }; // 작은 상한과 나머지 한 점을 만드는 시작 체력
            for (int i = 0; i < caps.Length; i++) // 실제 파티 순서대로 초기 수치 구성
            { // 각 아군 수치 구성 시작
                fight.Allies[i].ModifyMaxHealth(caps[i] - fight.Allies[i].MaxHealth); // 원래 최대 체력에서 검사 상한으로 변경
                fight.Allies[i].ApplyPersistentHealth(health[i]); // 실제 음식이나 피해 없는 시작 체력 저장
            } // 각 아군 수치 구성 종료
            int total = 39; // 첫 상태 합계와 뷰 알림 때 검증할 값
            int changes = 0; // 실제 체력 표시 변경 횟수
            int healing = 0; // 일반 회복 효과가 발휘된 횟수
            int damage = 0; // 일반 피해 효과가 발휘된 횟수
            var mental = fight.Allies.Select(unit => unit.CurrentMental).ToArray(); // 원래 정신력 보관
            foreach (var ally in fight.Allies) // 실제 유닛의 화면과 효과 신호 연결
            { // 개별 유닛 검사 연결 시작
                ally.HealthChanged += _ => CheckHealthForAllies(); // 화면 알림 시점마다 완성된 체력 합계 검사
                ally.HealthRestored += (_, amount) => healing += amount; // 실제 일반 회복량이 생기면 누적
                ally.DamageTaken += (_, result) => damage += result.AppliedDamage; // 실제 일반 피해량이 생기면 누적
            } // 개별 유닛 검사 연결 종료
            yield return fight.Use("REDISTRIBUTION"); // 실제 아군 전체 카드 첫 사용
            CollectionAssert.AreEqual(new[] { 8, 16, 15 }, fight.Allies.Select(unit => unit.CurrentHealth)); // 상한과 파티 순서 나머지 규칙
            CollectionAssert.AreEqual(new[] { 6, 6, 6 }, fight.Allies.Select(unit => unit.CurrentBlock)); // 각 생존 아군의 네이티브 방어도
            fight.Allies[0].ApplyPersistentHealth(2); // 재분배를 사용할 생존 소유자의 시작 체력
            fight.Allies[1].ApplyPersistentHealth(0); // 다음 재분배에서 제외할 쓰러진 동료
            fight.Allies[2].ApplyPersistentHealth(21); // 새 생존 명단의 시작 체력 둘
            total = 23; // 두 번째 재분배의 보존 체력
            yield return fight.Use("REDISTRIBUTION"); // 실제 아군 전체 카드 두 번째 사용
            CollectionAssert.AreEqual(new[] { 8, 0, 15 }, fight.Allies.Select(unit => unit.CurrentHealth)); // 시전자 최대치와 쓰러진 동료 및 보존 체력 확인
            CollectionAssert.AreEqual(new[] { 12, 6, 12 }, fight.Allies.Select(unit => unit.CurrentBlock)); // 살아 있는 아군에게만 두 번째 방어도 지급
            CollectionAssert.AreEqual(mental, fight.Allies.Select(unit => unit.CurrentMental)); // 일반 회복 및 피해의 정신력 변화 없음
            Assert.AreEqual(4, changes); // 실제 달라진 체력이 있는 네 경우의 UI 알림
            Assert.AreEqual(0, healing); // 일반 회복 이벤트 미발행
            Assert.AreEqual(0, damage); // 일반 피해 이벤트 미발행

            void CheckHealthForAllies() // 어느 알림에서든 이미 확정된 파티 체력 검증
            { // 알림 중 파티 체력 검사 시작
                changes++; // 실제 화면 갱신 신호 집계
                Assert.AreEqual(total, fight.Allies.Sum(unit => unit.CurrentHealth)); // 부분 갱신이 보이지 않는 합계 보존
            } // 알림 중 파티 체력 검사 종료
        } // 체력 검사 준비 종료
    } // 재분배 전투 검사 종료
} // 지원 카드 검사 종료
