using System.Collections; // Unity 게임 상태와 실제 카드 연출 대기
using System.Linq; // 실제 생존 적과 상태 확인
using NUnit.Framework; // 원래 회복과 상태 검증
using UnityEngine; // 원본 유물과 재현 가능한 게임 난수 사용
using UnityEngine.TestTools; // 실제 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 검증에 쓰는 실제 손패와 카드 입력
using Random = UnityEngine.Random; // 원본 게임의 독립 상태 굴림

public sealed class WinterfallBunkerRelicFinalEffectsTests // 포탑 탄피와 스크랩 주사기 전투 회귀
{ // 두 실제 유물 검사 시작
    [UnityTest] // 방패를 연속 사용해도 다음 광역 첫 적중만 처리
    public IEnumerator TurretCasingArmsOneChargeAndShocksOnlyFirstOfAnAreaAttack() // 실제 방패 및 광역 사용과 독립 감전 수명 확인
    { // 포탑 탄피의 첫 명중 검사 시작
        yield return new EnterPlayMode(); // 실제 자산과 카드 입력 환경 진입
        Acquire("LeakageTurretCasing"); // 공식 누전 포탑 탄피 원본 획득
        using (var fight = new Fight("RAISE_SHIELD", "RAISE_SHIELD", "RUNAWAY_RUNE_DEVICE", "DOUBLE_SLASH")) // 방패 두 장과 광역 및 후속 공격 준비
        { // 전투의 실제 방패 완료와 피해 검증 시작
            yield return fight.Use("RAISE_SHIELD"); // 첫 방패 카드 실제 사용
            yield return fight.Use("RAISE_SHIELD"); // 똑같은 실제 방패 카드로 두 번째 준비 시도
            SeedTwoRolls(firstSucceeds: true, secondSucceeds: true); // 잘못 중첩되면 두 적에게도 들어갈 수 있는 실제 RNG 지정
            yield return fight.Use("RUNAWAY_RUNE_DEVICE"); // 아군 전체 대상 아닌 생존 적 전체의 한 실제 공격 정산
            Assert.AreEqual(2, fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 첫 대상의 두 적 턴 네이티브 감전
            Assert.AreEqual(1, fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Shock)?.EffectiveValue); // 기존 감전 원본 수치 한 점 확인
            Assert.IsTrue(fight.Enemies.Skip(1).All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Shock) == null)); // 같은 광역 뒤따르는 적에 중복 감전 없음
            SeedTwoRolls(firstSucceeds: true, secondSucceeds: true); // 청구권이 남았다면 발동하는 다음 비교 난수
            yield return fight.Use("DOUBLE_SLASH", 1); // 두 번째 생존 적을 향한 별도 실제 다타 공격
            Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Shock)); // 이전 방패를 두 번 사용해도 두 번째 공격에는 감전 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 감전의 첫 실제 적 턴 진입
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료 뒤 한 턴 남음 확인
            Assert.AreEqual(1, fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 원래 감전의 적 턴 시계 적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 적 턴 도착
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
            Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Shock)); // 감전이 두 실제 적 차례 뒤 종료
        } // 전투의 실제 방패 완료와 피해 검증 종료
        yield return new ExitPlayMode(); // 게임 전투와 테스트용 보유 상태 정리
    } // 포탑 탄피의 첫 명중 검사 종료

    [UnityTest] // 실제 공격 빗나감 또는 첫 명중 부적격 대상의 예외
    public IEnumerator TurretCasingDoesNotRetryAfterMissOrIneligibleFirstContact() // 명중하지 않았거나 첫 적이 면역 및 쓰러짐이면 유물 낭비 확인
    { // 포탑 탄피의 무효 공격 검사 시작
        yield return new EnterPlayMode(); // 실제 리소스와 전투 시작
        Acquire("LeakageTurretCasing"); // 보물 원본 유물 등록
        for (int scenario = 0; scenario < 3; scenario++) // 전체 빗나감과 면역 및 첫 실제 처치 대조
        { // 각 독립 교전의 조건 구성 시작
            using var fight = new Fight("RAISE_SHIELD", "RUNAWAY_RUNE_DEVICE", "DOUBLE_SLASH"); // 원본 카드 입력과 적 네 명
            yield return fight.Use("RAISE_SHIELD"); // 실제로 끝난 방패의 탄피 준비
            if (scenario == 0) // 공격 전체가 빗나가는 원래 명중 상태 확인
            { // 공격 전체 실명 처리 시작
                fight.Allies[0].ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 3, 1); // 같은 광역의 모든 적에게 원래 명중률 영 적용
            } // 공격 전체 실명 처리 종료
            else if (scenario == 1) // 첫 실제 피격 적의 상태 면역 지정
            { // 기존 상태 면역 처리 시작
                fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 새 감전 시도의 네이티브 면역
            } // 기존 상태 면역 처리 종료
            else // 첫 타격으로 쓰러져 상태를 받을 수 없는 적
            { // 생존 상태에서 즉시 처치되는 첫 대상 준비 시작
                fight.Enemies[0].TakeDamage(fight.Enemies[0].CurrentHealth - 1, BattleDamageType.None); // 원래 체력 한 점만 남은 첫 생존 적 구성
            } // 생존 상태에서 즉시 처치되는 첫 대상 준비 종료
            SeedTwoRolls(firstSucceeds: true, secondSucceeds: true); // 기회가 있다면 새 감전이 성공할 실제 판정
            yield return fight.Use("RUNAWAY_RUNE_DEVICE"); // 광역의 첫 대상 처리 이후 나머지 적 결과 확인
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Shock) == null)); // 무효 첫 접촉을 두 번째 대상에 넘기지 않음
            fight.Allies[0].RemoveStatusEffect(BattleStatusEffectType.Blind); // 이후 명중을 위한 실명 해제
            SeedTwoRolls(firstSucceeds: true, secondSucceeds: true); // 원래 가능한 독립 다음 공격의 판정 준비
            yield return fight.Use("DOUBLE_SLASH", 2); // 다른 생존 적을 향한 실제 정상 타격
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Shock)); // 무효였어도 직전 공격이 실제 유물 사용 기회 소비
        } // 각 독립 교전의 조건 구성 종료
        yield return new ExitPlayMode(); // 실제 플레이 검사 정리
    } // 포탑 탄피의 무효 공격 검사 종료

    [UnityTest] // 실제 카드 회복에만 수치 증가와 대상별 약화
    public IEnumerator ScrapSyringeBoostsDirectCardHealingAndRollsForEachActuallyHealedAlly() // 광역 수신 회복과 독립 디버프 확률 확인
    { // 스크랩 주사기의 실제 치유 검사 시작
        yield return new EnterPlayMode(); // 공식 카드와 접촉하는 원래 게임 준비
        Acquire("ScrapSyringe"); // 스크랩 주사기의 실제 원본 보유
        using (var fight = new Fight("GOLDEN_RULE")) // 여러 생존 아군을 실제로 회복할 원본 카드
        { // 시작 행동력과 부상 파티 준비 시작
            var first = fight.Allies[0]; // 정상 독립 약화가 성공할 부상 아군
            var second = fight.Allies[1]; // 두 번째 독립 약화가 실패할 부상 아군
            var healthy = fight.Allies[2]; // 회복이 없는 정상 최대 체력 아군
            first.ApplyPersistentHealth(10); // 일반 회복 비교의 낮은 체력
            SeedTwoRolls(firstSucceeds: true, secondSucceeds: false); // 유물 외 일반 회복에 굴림 없음을 확인할 실제 난수
            Assert.AreEqual(10, first.RestoreHealth(10)); // 카드 밖의 기존 자동과 직접 유닛 회복 보존
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 일반 회복으로 주사기 부작용 없음
            first.ApplyPersistentHealth(10); // 원래 광역 카드의 독립 회복 이전 기준으로 복원
            second.ApplyPersistentHealth(10); // 두 번째 대상도 같은 원래 부상 상태
            Assert.IsTrue(fight.Points.Spend(14)); // 황금률 비용 네 점 뒤 남은 두 행동력 기준 설정
            SeedTwoRolls(firstSucceeds: true, secondSucceeds: false); // 회복 대상별 독립 20퍼센트 성공과 실패 채우기
            yield return fight.Use("GOLDEN_RULE"); // 실제 손패에서 선택한 아군 광역 회복 카드 처리
            Assert.AreEqual(22, first.CurrentHealth); // 실제 기본 회복 열 점의 스무 퍼센트 가산
            Assert.AreEqual(22, second.CurrentHealth); // 독립 약화의 실패 대상에도 같은 실제 회복 보정
            Assert.AreEqual(healthy.MaxHealth, healthy.CurrentHealth); // 원래 상한 때문에 실제 회복하지 않는 아군 확인
            Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)?.EffectiveValue); // 회복 대상에게만 원래 공격력 감소 한 점
            Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)?.RemainingTurns); // 이번 실제 아군 턴의 기본 감소 기간
            Assert.IsNull(second.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 첫 성공과 별개인 두 번째 실제 굴림 실패
            Assert.IsNull(healthy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 양수 실제 회복이 없었던 대상에는 부작용 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 유물 부작용의 한 아군 턴 실제 종료
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 원래 감소 상태의 현재 아군 턴 종료에서 만료
        } // 시작 행동력과 부상 파티 준비 종료
        yield return new ExitPlayMode(); // 원래 게임과 사용한 보유 유물 정리
    } // 스크랩 주사기의 실제 치유 검사 종료

    private static void SeedTwoRolls(bool firstSucceeds, bool secondSucceeds) // 원래 Unity의 독립 20퍼센트 판정을 재현할 시드 검색
    { // 실제 RNG 두 번의 경계 준비 시작
        for (int seed = 1; seed < 2000; seed++) // 정상 결과를 재생할 범위에서 고정 시드 찾기
        { // 후보 시드 확인 시작
            Random.InitState(seed); // 원래 게임에서 사용하는 RNG 상태 설정
            if ((Random.Range(0, 100) < 20) == firstSucceeds && (Random.Range(0, 100) < 20) == secondSucceeds) // 원하는 두 독립 롤의 현재 시드 확인
            { // 실제로 사용할 유효 시드 복원 시작
                Random.InitState(seed); // 판정 전 같은 상태로 복구
                return; // 실제 회귀 검사 입력 준비 완료
            } // 실제로 사용할 유효 시드 복원 종료
        } // 후보 시드 확인 종료
        Assert.Fail("고정 확률 판정 시드 없음"); // 기대 두 값이 없는 비교의 정상 실패
    } // 실제 RNG 두 번의 경계 준비 종료

    private static void Acquire(string stem) // 원래 회차 유물 등록
    { // 공식 유물 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + stem); // 제공된 리소스 경로의 실제 날짜별 원본
        Assert.IsNotNull(relic, stem); // 실제 유물 에셋과 메타의 정상 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 게임 내 동일한 실제 유물 획득
    } // 공식 유물 준비 종료
} // 두 실제 유물 검사 종료
