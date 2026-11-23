using System; // 실제 확률을 못 찾은 경우의 검사 오류
using System.Collections; // 카드 충돌과 플레이 모드의 코루틴
using System.Collections.Generic; // 원본과 반향의 실제 피해 기록
using System.Linq; // 현재 생존 적과 반복 피해의 원본 배열 비교
using NUnit.Framework; // 상태와 타격의 실제 확인
using UnityEngine.TestTools; // 실제 플레이 모드 시작과 종료
using Fight = WinterfallSwordCombatTests.Fight; // 기존 플레이어 카드 선택과 전투 검사 환경
using Random = UnityEngine.Random; // 실제 타격과 상태 확률의 고정 난수

public sealed class WinterfallWandDownpourAndVoidTests // 화염 폭우와 천둥 감옥 및 공허의 손 공격 검사
{ // 지팡이 연타와 정화 검사 시작
    [UnityTest] // 여섯 번의 명중과 반향의 상태 미복제 검사
    public IEnumerator FireDownpourEchoRepeatsDamageWithoutRepeatingBurnOrPayingAgain() // 실제 생존 적 연타와 상태 확률의 분리 검증
    { // 화염 폭우와 반향 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 원본 마법 반향의 플레이 모드
        yield return CheckDownpourAndEcho(); // 실제 화염 폭우 피해 열두 번과 원본 화상 검사
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 화염 폭우와 반향 검사 종료

    private static IEnumerator CheckDownpourAndEcho() // 실제 카드 두 장의 비용과 타격 및 화상 횟수 확인
    { // 살아 있는 적 한 명의 명중 추적 시작
        using var fight = new Fight("MAGIC_ECHO", "FIRE_DOWNPOUR"); // 실제 원본 마법 반향과 화염 폭우 카드 준비
        foreach (var enemy in fight.Enemies.Skip(1)) // 나머지 적 위치에서 생존 후보 제외
        { // 생존 후보 정리 시작
            enemy.TakeDamage(enemy.CurrentHealth, BattleDamageType.None); // 마지막 하나만 남기는 실제 처치
        } // 생존 후보 정리 종료
        var target = fight.Enemies[0]; // 모든 원본과 복제의 현재 살아 있는 적
        yield return fight.Use("MAGIC_ECHO"); // 기존 손패의 실제 마법 반향 먼저 사용
        Assert.AreEqual(50, fight.Allies[0].GetStatusEffect(BattleStatusEffectType.WandEcho)?.EffectiveValue); // 기존 카드의 피해 절반 반향 적용 확인
        var damages = new List<int>(); // 실제 피해 결과의 원본 수치 기록
        int burns = 0; // 실제로 성공한 화상 부여와 갱신 횟수
        target.DamageResolved += (source, unit, result) => damages.Add(result.RawDamage); // 각 타격의 원본 수치를 실제 피해 사건에서 수집
        target.StatusEffectResolved += (source, unit, type, result) => // 실제 상태 부여와 갱신 결과 관찰
        { // 화상 발생 여부 검사 시작
            if (type == BattleStatusEffectType.Burn && result is (BattleStatusEffectApplyResult.Applied or BattleStatusEffectApplyResult.Stacked)) // 성공한 새 화상과 실제 갱신 결과 확인
            { // 실제 화상 횟수 기록 시작
                burns++; // 원본 타격에서만 생길 성공 상태 한 번
            } // 실제 화상 횟수 기록 종료
        }; // 화상 발생 여부 검사 종료
        int expectedBurns = 0; // 고정 난수에서 실제 성공할 원본 타격 수
        int seed = FindSeed(() => // 낮은 확률이 서로 다른 여섯 타격에 섞이는 실제 난수 배열 조회
        { // 원본 여섯 타격 난수 비교 시작
            expectedBurns = 0; // 새 후보마다 원래 성공 횟수 초기화
            for (int index = 0; index < 6; index++) // 실제 여섯 타격 모두의 대상과 상태 난수 확인
            { // 현재 독립 타격의 원본 난수 시작
                Random.Range(0, 1); // 살아 있는 적 한 명의 기존 무작위 선택
                if (Random.Range(0, 100) < 25) // 원본 화상 확률의 성공 값 확인
                { // 실제 난수 성공 기록 시작
                    expectedBurns++; // 성공한 원본 타격의 상태 횟수
                } // 실제 난수 성공 기록 종료
            } // 현재 독립 타격의 원본 난수 종료
            return expectedBurns >= 2 && expectedBurns < 6; // 한 번 이상 갱신과 적어도 한 실패가 섞인 난수
        }); // 원본 여섯 타격 난수 비교 종료
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 실제 화염 폭우 효과 직전에 고정한 원본 난수
        int points = fight.Points.CurrentActionPoints; // 반향 다음 카드가 실제로 요구할 행동력 대조
        yield return fight.Use("FIRE_DOWNPOUR"); // 대상 지정 없이 현재 살아 있는 적에게 실제 무작위 연타
        Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 열두 타격이어도 원본 세 행동력만 한 번 결제
        CollectionAssert.AreEqual(Enumerable.Repeat(5, 6).Concat(Enumerable.Repeat(2, 6)).ToArray(), damages); // 원본 여섯 타격과 절반 내림의 독립 반향 여섯 타격
        Assert.AreEqual(158, target.CurrentHealth); // 정상 마법 방어 처리를 거친 전체 실제 피해 사십이
        Assert.AreEqual(expectedBurns, burns); // 실제 원본 화상 성공이나 갱신을 반향이 반복하지 않는지 확인
        Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.Burn)?.EffectiveValue); // 실제 원본의 화상 수치 둘
        Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.Burn)?.RemainingTurns); // 실제 원본 화상의 두 적 턴
    } // 살아 있는 적 한 명의 명중 추적 종료

    [UnityTest] // 살아 있는 적 전체 피해와 개별 감전 확률 검사
    public IEnumerator ThunderPrisonChecksEachLandedTargetAndKeepsShockForTwoEnemyStarts() // 실제 일반 면역과 두 적 턴 감전 피해 검증
    { // 천둥 감옥 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 적 턴 계층 실행
        yield return CheckThunderPrison(); // 실제 적별 광역 피해와 감전 수명 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 천둥 감옥 검사 종료

    private static IEnumerator CheckThunderPrison() // 적별 독립 확률과 감전 상태의 실제 적 진영 수명
    { // 두 번의 감전과 개별 적 조건 검사 시작
        using var fight = new Fight("THUNDER_PRISON"); // 천둥 감옥의 등록 원본 실제 카드
        var shocked = fight.Enemies[0]; // 정상 피해와 감전을 받을 첫 적
        var normal = fight.Enemies[1]; // 명중하고 감전 확률만 실패할 두 번째 적
        var immune = fight.Enemies[2]; // 감전 상태에 일반 면역이 있는 적
        var fragile = fight.Enemies[3]; // 마법 피해로 실제 쓰러질 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 감전을 막을 일반 상태 면역
        fragile.TakeDamage(194, BattleDamageType.None); // 광역 열 피해에 쓰러지는 실제 체력 여섯
        int seed = FindSeed(() => Random.Range(0, 100) < 40 && Random.Range(0, 100) >= 40); // 각자 성공과 실패를 재현하는 실제 난수 배열
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 실제 광역 명중 직전에 난수 재현
        int points = fight.Points.CurrentActionPoints; // 원본 카드 비용의 실제 행동력
        yield return fight.Use("THUNDER_PRISON"); // 생존한 적 전체에 실제 원본 카드 사용
        Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 광역 행동력 세 점 한 번 지불 확인
        Assert.AreEqual(190, shocked.CurrentHealth); // 감전을 받는 첫 적의 일반 마법 피해 열
        Assert.AreEqual(190, normal.CurrentHealth); // 상태 확률 실패와 별개인 기본 열 피해
        Assert.AreEqual(190, immune.CurrentHealth); // 면역인 적도 받는 정상 마법 피해 열
        Assert.IsTrue(fragile.IsDead); // 광역의 실제 피해로 쓰러진 적
        Assert.AreEqual(2, shocked.GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 두 적 턴의 실제 감전 상태
        Assert.IsNull(normal.GetStatusEffect(BattleStatusEffectType.Shock)); // 독립 감전 확률이 실패한 적
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Shock)); // 기본 약화 면역 대상의 감전 없음
        Assert.IsNull(fragile.GetStatusEffect(BattleStatusEffectType.Shock)); // 실제 전투불능 대상의 감전 없음
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 적 턴을 시작
        Assert.AreEqual(189, shocked.CurrentHealth); // 기존 첫 감전 틱의 실제 체력 한 점
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료로 별도 감전 피해가 없는 처리
        Assert.AreEqual(1, shocked.GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 실제 첫 발동 뒤 한 번 남은 상태
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 적 턴 시작
        Assert.AreEqual(188, shocked.CurrentHealth); // 두 번째 감전의 실제 체력 한 점
        Assert.IsNull(shocked.GetStatusEffect(BattleStatusEffectType.Shock)); // 두 번째 발동 뒤 감전 상태 제거
    } // 두 번의 감전과 개별 적 조건 검사 종료

    [UnityTest] // 최초 활성 강화 한 개와 피해 무효 및 빗나감 검증
    public IEnumerator VoidHandRemovesTheOldestActiveRemovableBuffOnlyAfterARealHit() // 원래 보호된 정신 상태를 유지하는 명중 후 제거 검증
    { // 공허의 손 실제 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 피해 및 상태 처리의 플레이 모드
        yield return CheckVoidHand(); // 실제 적의 강화와 피해 면역 조건 대조
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 공허의 손 실제 검사 종료

    private static IEnumerator CheckVoidHand() // 오래된 활성 강화와 기존 면역의 직접 제거 검사
    { // 보호 상태와 일반 강화의 순서 준비 시작
        using var fight = new Fight("VOID_HAND", "VOID_HAND", "VOID_HAND"); // 공허의 손의 실제 등록 원본 세 장
        var target = fight.Enemies[0]; // 일반 강화와 보호 정신 상태 및 약화 면역이 있는 생존 적
        var missed = fight.Enemies[1]; // 피해와 해제 모두 빗나가는 생존 적
        var nullified = fight.Enemies[2]; // 피해 무효 보호로 강화를 유지할 생존 적
        target.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedUp, 1, 1, 1); // 목록에 있으나 활성 아닌 가장 오래된 상태
        target.GetStatusEffect(BattleStatusEffectType.HealingReceivedUp).AdvanceDuration(); // 현재 활성 기간을 전부 소모
        target.ChangeMental(100, BattleMentalChangeReason.CardEffect); // 기존 코드로 만든 제거할 수 없는 정신 각성
        Assert.AreEqual(BattleMentalState.Awakening, target.MentalState); // 기존 자연 각성 상태의 실제 확인
        target.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 1, 2, 1); // 보호 사항을 지나 제거할 첫 실제 활성 일반 강화
        target.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 1, 2, 1); // 그 뒤 남아야 할 둘째 일반 강화
        target.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 새 약화만 막으며 아군 강화 해제는 막지 않는 비교 상태
        missed.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 1, 2, 1); // 빗나가면 남아 있어야 할 첫 일반 강화
        missed.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 실제 공격을 항상 피하는 기존 회피
        nullified.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 1, 2, 1); // 피해 무효 카드 공격에서도 남겨야 할 비교 강화
        nullified.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 실제 피해를 막는 기존 무효
        int points = fight.Points.CurrentActionPoints; // 공허의 손 실제 사용 전 행동력
        yield return fight.Use("VOID_HAND", 0); // 실제 양수 피해와 첫 활성 강화 해제
        Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 실제 원본 세 행동력의 정상 차감
        Assert.AreEqual(184, target.CurrentHealth); // 기본 마법 열여섯 피해
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Regeneration)); // 이미 끝난 상태와 정신 각성 다음의 최초 활성 강화 제거
        Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 두 번째 활성 일반 강화를 한 번 더 없애지 않는 확인
        Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.StatusImmunity)); // 이번 제거 대상이 아닌 일반 약화 면역 원본 유지
        Assert.AreEqual(BattleMentalState.Awakening, target.MentalState); // 보호되는 특수 정신 상태를 제거하지 않는 확인
        yield return fight.Use("VOID_HAND", 1); // 확정 회피가 적용된 적에게 실제 카드 사용
        Assert.AreEqual(200, missed.CurrentHealth); // 실제 빗나감의 피해 미적용
        Assert.IsNotNull(missed.GetStatusEffect(BattleStatusEffectType.Regeneration)); // 빗나간 공격의 일반 강화 보존
        yield return fight.Use("VOID_HAND", 2); // 다음 양수 피해를 실제 무효화하는 적 타격
        Assert.AreEqual(200, nullified.CurrentHealth); // 원래 피해 무효 상태 적용
        Assert.IsNull(nullified.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 실제 피해를 막은 보호 상태의 기본 소모
        Assert.IsNotNull(nullified.GetStatusEffect(BattleStatusEffectType.Regeneration)); // 별도 피해 무효의 일반 강화 제거 없음
    } // 보호 상태와 일반 강화의 순서 준비 종료

    private static int FindSeed(Func<bool> accepts) // 정해진 실제 난수 조건을 만족하는 재현 시드 조회
    { // 후보 난수 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 실제 게임의 제한된 후보 난수를 순서대로 확인
        { // 현재 후보의 난수 준비 시작
            Random.InitState(seed); // 게임의 원본 정수 난수 초기화
            if (accepts()) // 실제 상태 또는 독립 대상의 확률 검사
            { // 재현할 실제 시드 찾기 시작
                return seed; // 동일한 원본 카드 사용에 다시 사용할 난수
            } // 재현할 실제 시드 찾기 종료
        } // 현재 후보의 난수 준비 종료
        throw new InvalidOperationException("No matching combat seed found."); // 지정 범위 안에 있는 실제 확률 시드 미발견
    } // 후보 난수 검색 종료
} // 지팡이 연타와 정화 검사 종료
