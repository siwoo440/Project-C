using System.Collections; // 실제 전투 검사의 코루틴
using System.Collections.Generic; // 실제 타격 순서 기록
using NUnit.Framework; // 피해와 상태 비교
using UnityEngine.TestTools; // 실제 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 실제 손패와 카드 실행 환경
using Random = UnityEngine.Random; // 재현 가능한 전투 확률

public sealed class WinterfallWandLegendaryStormTests // 전설 지팡이의 실제 실행 검사
{ // 전설 공격 검사 묶음 시작
    [UnityTest] // 폭풍의 눈의 독립 광역 순서 검사
    public IEnumerator EyeOfStormRechecksLivingEnemiesAndShockOnEachWave() // 각 타격 시점의 생존과 감전 확인
    { // 두 차례 광역 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 선택과 전투 실행
        yield return CheckStorm(false); // 기본 폭풍의 두 차례 타격
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 두 차례 광역 검사 종료

    [UnityTest] // 마법 반향의 광역 순서와 비용 검사
    public IEnumerator EyeOfStormEchoUsesTwoWavesAtHalfConditionalDamage() // 같은 순서와 감전 가산의 절반 피해 확인
    { // 원본 및 반향 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 반향 상태 실행
        yield return CheckStorm(true); // 원본과 반향의 네 차례 타격
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 원본 및 반향 검사 종료

    private static IEnumerator CheckStorm(bool echo) // 두 생존 적과 전투불능 및 회피 적 비교
    { // 실제 등록 카드 및 적 준비 시작
        using var fight = echo ? new Fight("MAGIC_ECHO", "EYE_OF_STORM") : new Fight("EYE_OF_STORM"); // 각 실행이 사용할 실제 등록 카드
        var early = fight.Enemies[0]; // 공격 전에 이미 감전 중인 적
        var late = fight.Enemies[1]; // 첫 피해 직후 감전이 생기는 적
        var fragile = fight.Enemies[2]; // 첫 차례에 실제로 쓰러지는 적
        var miss = fight.Enemies[3]; // 두 차례 모두 피하는 활성 회피 적
        early.ApplyStatusEffect(BattleStatusEffectType.Shock, 1, 2, 1); // 두 차례의 피해를 키우는 기존 감전
        fragile.TakeDamage(194, BattleDamageType.None); // 첫 방어 전 피해로 쓰러질 체력 여섯
        miss.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 각 차례에서 적용할 확정 회피
        var hits = new List<(int Target, int Damage)>(); // 적별 실제 명중 순서 및 원본 피해
        early.DamageResolved += (source, target, result) => hits.Add((0, result.RawDamage)); // 첫 적의 실제 차례별 타격 기록
        late.DamageResolved += (source, target, result) => // 둘째 적의 실제 피해와 시점별 감전
        { // 둘째 적의 명중 처리 시작
            hits.Add((1, result.RawDamage)); // 현재 타격에 사용된 실제 피해 기록
            late.ApplyStatusEffect(BattleStatusEffectType.Shock, 1, 2, 1); // 첫 피해가 끝난 다음에 들어오는 실제 감전
        }; // 둘째 적의 명중 처리 종료
        fragile.DamageResolved += (source, target, result) => hits.Add((2, result.RawDamage)); // 첫 차례에만 존재하는 적 기록
        if (echo) // 실제 카드 사용 전 반향 예약 여부
        { // 기존 마법 반향 준비 시작
            yield return fight.Use("MAGIC_ECHO"); // 반향 상태가 정상 손패에서 시작되는 경로
        } // 기존 마법 반향 준비 종료
        int points = fight.Points.CurrentActionPoints; // 공격 전에 남은 실제 행동력
        yield return fight.Use("EYE_OF_STORM"); // 생존 적 전체를 자동 선택하는 등록 원본 사용
        var expected = new List<(int, int)> { (0, 22), (1, 14), (2, 14), (0, 22), (1, 22) }; // 적 하나씩 반복하지 않는 두 차례 원본 타격 순서
        if (echo) // 실제 반향에서만 이어지는 두 차례 확인
        { // 절반 피해의 실제 타격 순서 추가 시작
            expected.AddRange(new[] { (0, 11), (1, 11), (0, 11), (1, 11) }); // 현재 감전을 포함하여 절반이 된 두 차례의 타격
        } // 절반 피해의 실제 타격 순서 추가 종료
        CollectionAssert.AreEqual(expected, hits); // 차례 사이의 생존 및 상태 갱신과 실제 타격 순서 확인
        Assert.AreEqual(points - 4, fight.Points.CurrentActionPoints); // 여러 명을 여러 번 때려도 원래 비용 한 번만 차감
        Assert.AreEqual(echo ? 134 : 156, early.CurrentHealth); // 처음부터 감전 중인 적의 실제 체력
        Assert.AreEqual(echo ? 142 : 164, late.CurrentHealth); // 첫 타격만 감전 전인 적의 실제 체력
        Assert.IsTrue(fragile.IsDead); // 실제 전멸 유닛의 두 번째 차례 제외
        Assert.AreEqual(200, miss.CurrentHealth); // 각각의 일반 회피 판정 적용
        Assert.IsNull(miss.GetStatusEffect(BattleStatusEffectType.Shock)); // 폭풍의 눈에서 별도 감전을 만들지 않는 확인
    } // 실제 등록 카드 및 적 준비 종료

    [UnityTest] // 심연 주문의 실제 정신력 피해 검사
    public IEnumerator AbyssSpellUsesNativeMentalLossOnlyOnLandedLivingTarget() // 살아 있는 명중 적과 빗나간 적 비교
    { // 심연 주문 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 정신력 이벤트 실행
        using (var fight = new Fight("ABYSS_SPELL", "ABYSS_SPELL")) // 실제 심연 주문 원본 두 장 준비
        { // 두 상대의 실제 카드 적용 시작
            var hit = fight.Enemies[0]; // 정상 명중하고 생존할 적
            var miss = fight.Enemies[1]; // 살아 있지만 공격을 피할 적
            var changes = new List<BattleMentalChangeResult>(); // 실제 카드 명시 정신력 변화 목록
            hit.MentalChanged += (target, change) => TrackCardMentalChange(changes, change); // 명중 대상의 실제 정신력 사건 기록
            miss.MentalChanged += (target, change) => TrackCardMentalChange(changes, change); // 회피 대상의 실제 정신력 사건 기록
            miss.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 기존 확정 회피
            int points = fight.Points.CurrentActionPoints; // 심연 주문 사용 전 행동력
            yield return fight.Use("ABYSS_SPELL", 0); // 명중 대상에게 실제 카드 사용
            yield return fight.Use("ABYSS_SPELL", 1); // 회피 대상에게 같은 카드 사용
            Assert.AreEqual(185, hit.CurrentHealth); // 마법 피해 열다섯의 실제 적용
            Assert.AreEqual(200, miss.CurrentHealth); // 빗나간 공격의 체력 보존
            Assert.AreEqual(points - 6, fight.Points.CurrentActionPoints); // 두 번 사용의 실제 비용 합계
            Assert.AreEqual(1, changes.Count); // 원래 피격 정신력과 분리된 명중 후 카드 효과 한 번
            Assert.AreEqual(-8, changes[0].AppliedDelta); // 기존 정신력 경로의 실제 여덟 감소
            Assert.AreSame(fight.Allies[0], changes[0].SourceUnit); // 카드 효과에 기록된 실제 공격자
        } // 두 상대의 실제 카드 적용 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 심연 주문 검사 종료

    private static void TrackCardMentalChange(List<BattleMentalChangeResult> changes, BattleMentalChangeResult change) // 기존 피격 이외의 카드 자체 정신력 기록
    { // 실제 카드 효과 판별 시작
        if (change.Reason == BattleMentalChangeReason.CardEffect) // 명중 뒤에 부여된 명시 정신 효과 확인
        { // 카드 정신력 변화 기록 시작
            changes.Add(change); // 적용량과 공격자 보관
        } // 카드 정신력 변화 기록 종료
    } // 실제 카드 효과 판별 종료

    [UnityTest] // 대폭발과 절대영도의 실제 상태 부여 검사
    public IEnumerator GreatExplosionAndAbsoluteZeroRollForEachLandedLivingEnemy() // 독립 확률, 면역 및 다음 행동 차단 확인
    { // 광역 상태 검사 시작
        yield return new EnterPlayMode(); // 실제 광역 카드와 행동 처리 실행
        yield return CheckAreaStatus("GREAT_EXPLOSION", 28, 60, BattleStatusEffectType.Burn); // 실제 마법 피해와 화상 부여 검사
        yield return CheckAreaStatus("ABSOLUTE_ZERO", 18, 50, BattleStatusEffectType.Stagger); // 실제 마법 피해와 경직 부여 검사
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 광역 상태 검사 종료

    private static IEnumerator CheckAreaStatus(string id, int damage, int chance, BattleStatusEffectType status) // 같은 실제 광역 피해 경로의 개별 확률 격리
    { // 네 상대의 전투 조건 준비 시작
        using var fight = new Fight(id); // 실제 등록된 광역 원본 한 장
        var affected = fight.Enemies[0]; // 상태 확률에 성공하는 첫 생존 적
        var resistedRoll = fight.Enemies[1]; // 실제 명중하지만 상태 확률이 실패하는 적
        var immune = fight.Enemies[2]; // 확률과 별개로 원래 상태 면역을 받는 적
        var fragile = fight.Enemies[3]; // 기본 피해로 실제 쓰러지는 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 일반적인 약화 차단 상태
        fragile.TakeDamage(194, BattleDamageType.None); // 원본 마법 피해를 견디지 못하는 체력 여섯
        int seed = FindSplitRoll(chance); // 처음 성공하고 다음 실패하는 실제 난수 상태
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 실제 명중 직전 원래 확률 흐름 재현
        int points = fight.Points.CurrentActionPoints; // 광역 주문 사용 직전 행동력
        yield return fight.Use(id); // 현재 생존 적 전체의 일반 명중과 독립 상태 적용
        Assert.AreEqual(points - 5, fight.Points.CurrentActionPoints); // 전체 대상이 공유하는 실제 비용 한 번
        Assert.AreEqual(200 - damage, affected.CurrentHealth); // 상태 성공 대상의 마법 피해
        Assert.AreEqual(200 - damage, resistedRoll.CurrentHealth); // 상태 확률 실패와 무관한 원본 피해
        Assert.AreEqual(200 - damage, immune.CurrentHealth); // 약화 면역 대상도 받는 원래 피해
        Assert.IsTrue(fragile.IsDead); // 쓰러진 대상에 후속 상태를 남기지 않는 조건
        Assert.IsNull(resistedRoll.GetStatusEffect(status)); // 대상별 실제 부여 확률 실패
        Assert.IsNull(immune.GetStatusEffect(status)); // 일반 약화 면역에서 실제 부여 실패
        Assert.IsNull(fragile.GetStatusEffect(status)); // 이미 쓰러진 대상의 상태 없음
        var applied = affected.GetStatusEffect(status); // 첫 대상의 실제 상태 조회
        Assert.IsNotNull(applied); // 명중 후 실제 첫 독립 상태 부여 확인
        if (status == BattleStatusEffectType.Burn) // 두 차례 화상과 다음 한 행동의 처리 구분
        { // 실제 화상 크기와 기간 확인 시작
            Assert.AreEqual(2, applied.EffectiveValue); // 대폭발의 화상 원본값 둘
            Assert.AreEqual(2, applied.RemainingTurns); // 대폭발의 원래 적 턴 둘
        } // 실제 화상 크기와 기간 확인 종료
        else // 기존 경직의 실제 다음 적 행동 검사
        { // 적 행동 한 번 차단 시작
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 경직 대상이 행동할 실제 적 단계 시작
            var action = new BattleEnemyAction(affected, fight.Allies[0], EnemyActionType.Attack, BattleDamageType.Physical, 8, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "절대영도", 1, 1, 5, 0); // 대상이 수행할 일반 적 공격
            int hp = fight.Allies[0].CurrentHealth; // 차단 대상 공격을 받기 전 아군 체력
            action.Execute(); // 실제 경직으로 현재 행동 차단과 상태 소모
            Assert.AreEqual(hp, fight.Allies[0].CurrentHealth); // 다음 적 행동에서 아군 피해 없음
            Assert.IsNull(affected.GetStatusEffect(status)); // 차단한 바로 그 한 행동에서 상태 소모
        } // 적 행동 한 번 차단 종료
    } // 네 상대의 전투 조건 준비 종료

    private static int FindSplitRoll(int chance) // 등록 확률의 첫 성공 및 둘째 실패 시드 선택
    { // 실제 난수 후보 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 정해진 범위의 실제 난수 후보 순회
        { // 시드별 상태 확률 확인 시작
            Random.InitState(seed); // 현재 후보로 Unity 전투 난수 초기화
            if (Random.Range(0, 100) < chance && Random.Range(0, 100) >= chance) // 첫 대상 성공과 둘째 대상 실패 동시 확인
            { // 지정 결과를 만드는 재현값 확인 시작
                return seed; // 실제 카드 사용 직전에 다시 적용할 값
            } // 지정 결과를 만드는 재현값 확인 종료
        } // 시드별 상태 확률 확인 종료
        throw new System.InvalidOperationException("No split status seed found."); // 실행 가능한 검사 시드 누락
    } // 실제 난수 후보 검색 종료
} // 전설 공격 검사 묶음 종료
