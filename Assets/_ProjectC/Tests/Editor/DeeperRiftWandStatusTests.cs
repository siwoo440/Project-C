using System.Collections; // 실제 전투 카드 사용의 코루틴
using System.Reflection; // 비용 직전의 오래된 대상 검증 진입점
using NUnit.Framework; // 피해와 확률 및 전투 카드 이동 검증
using UnityEngine; // 전투 유물의 실제 원본 조회
using UnityEngine.TestTools; // 전투 플레이 모드 시작과 종료
using Fight = WinterfallSwordCombatTests.Fight; // 공용 실제 카드 입력 검사 환경
using Object = UnityEngine.Object; // 임시 회차 유물 관리자 삭제
using Random = UnityEngine.Random; // 의도한 성공과 실패의 실제 정수 난수

public sealed class DeeperRiftWandStatusTests // 깊은 균열 지팡이의 명중 상태 실전 검사
{ // 지팡이 상태 검사 시작
    [UnityTest] // 확률 화상과 명중 결과의 경계 확인
    public IEnumerator SmallFlameRollsOnlyAfterALandedHitOnASurvivingNonimmuneEnemy() // 빗나감과 면역 및 처치에서 추가 난수 미소모 확인
    { // 작은 불꽃 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 공격 연출을 사용할 플레이 모드 진입
        yield return CheckSmallFlame(); // 실제 카드로 화상 성공과 실패 및 판정 제외 확인
        yield return new ExitPlayMode(); // 독립 플레이 모드 종료
    } // 작은 불꽃 검사 종료

    private static IEnumerator CheckSmallFlame() // 실제 작은 불꽃 다섯 장의 결과 비교
    { // 화상 판정 대조 시작
        using var fight = new Fight("SMALL_FLAME", "SMALL_FLAME", "SMALL_FLAME", "SMALL_FLAME", "SMALL_FLAME"); // 작은 불꽃 원본 다섯 장과 생존 적 네 명
        int success = FindSeed(roll => roll < 25); // 원본 확률 범위 안의 실제 정수 난수
        int failure = FindSeed(roll => roll >= 25); // 원본 확률 범위 밖의 실제 정수 난수
        int activeSeed = success; // 현재 카드가 적용할 고정 난수 상태
        fight.Controller.CardUsed += (card, targets) => Random.InitState(activeSeed); // 비용 지급 뒤 실제 타격 바로 전 난수 초기화
        var torched = fight.Enemies[0]; // 정상 명중에서 화상을 받을 대상
        yield return fight.Use("SMALL_FLAME", 0); // 실제 원본 카드로 확률 성공 명중
        Assert.AreEqual(194, torched.CurrentHealth); // 작은 불꽃의 기본 마법 피해 여섯 확인
        var burn = torched.GetStatusEffect(BattleStatusEffectType.Burn); // 직접 명중 뒤 실제 생긴 화상
        Assert.AreEqual(2, burn?.EffectiveValue); // 발동한 화상의 최초 피해량 둘 확인
        Assert.AreEqual(2, burn?.RemainingTurns); // 화상 상태의 원래 두 적 턴 확인
        activeSeed = failure; // 두 번째 명중에는 확률 실패 값 적용
        yield return fight.Use("SMALL_FLAME", 1); // 정상 명중하되 별도 상태 확률이 실패
        var normal = fight.Enemies[1]; // 기본 피해만 받을 실제 적
        Assert.AreEqual(194, normal.CurrentHealth); // 상태 판정 실패와 별개인 기본 마법 피해
        Assert.IsNull(normal.GetStatusEffect(BattleStatusEffectType.Burn)); // 평범한 확률 실패의 화상 없음 확인
        activeSeed = success; // 원래대로라면 화상이 생길 확률 값
        var immune = fight.Enemies[2]; // 정상 공격과 상태 면역을 비교할 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 기존 능력으로 일반 화상을 차단
        var unchanged = StateAfterRolls(success, 0); // 명중이 확정되면 추가 난수 없는 상태
        yield return fight.Use("SMALL_FLAME", 2); // 실제 상태 면역 적에게 명중
        Assert.AreEqual(194, immune.CurrentHealth); // 상태 면역은 공격 피해를 막지 않는지 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Burn)); // 기존 면역의 화상 미부여
        Assert.AreEqual(unchanged, Random.state); // 부여 불가능한 면역 대상의 난수 미소모
        normal.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 기존 명중 굴림으로 항상 빗나가는 대상
        var onlyHitRoll = StateAfterRolls(success, 1); // 빗나감 판정 한 번만 소비했을 상태
        yield return fight.Use("SMALL_FLAME", 1); // 회피가 활성인 같은 적에게 두 번째 공격
        Assert.AreEqual(194, normal.CurrentHealth); // 빗나간 공격에 체력 피해 없음 확인
        Assert.IsNull(normal.GetStatusEffect(BattleStatusEffectType.Burn)); // 빗나감의 화상 미부여
        Assert.AreEqual(onlyHitRoll, Random.state); // 명중 확인 외 화상 난수 미소모
        var fragile = fight.Enemies[3]; // 실제 기본 피해로 바로 쓰러지는 적
        fragile.TakeDamage(195, BattleDamageType.None); // 공격 직전 체력 다섯 설정
        yield return fight.Use("SMALL_FLAME", 3); // 적 체력을 소진할 여섯 마법 피해
        Assert.IsTrue(fragile.IsDead); // 카드 기본 피해로 실제 전투불능 확인
        Assert.IsNull(fragile.GetStatusEffect(BattleStatusEffectType.Burn)); // 쓰러진 적의 화상 미부여 확인
        Assert.AreEqual(unchanged, Random.state); // 쓰러진 대상의 추가 화상 난수 미소모
    } // 화상 판정 대조 종료

    [UnityTest] // 확정 속도 약화와 유리 구슬의 확률 감전 비교
    public IEnumerator StaticChargeUsesTheGlassMarbleAndTicksOnExactlyTwoEnemyStarts() // 정상 화상 확률 보정과 두 실제 감전 턴의 호환 확인
    { // 정전기 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 적 진영 턴 연결
        yield return CheckStaticCharge(); // 정전기의 확률 유물과 정확한 감전 기간 확인
        yield return new ExitPlayMode(); // 독립 플레이 모드 종료
    } // 정전기 검사 종료

    private static IEnumerator CheckStaticCharge() // 확정 냉기와 선택 감전의 기존 난수 및 유물 비교
    { // 적 턴 상태 검사 시작
        using var fight = new Fight("COLD_TOUCH", "SMALL_FLAME", "STATIC_CHARGE", "STATIC_CHARGE"); // 실제 확정 냉기와 두 확률 상태 원본 준비
        var previous = RelicRunManager.Instance; // 기존 회차 보관함의 존재 여부
        var manager = RelicRunManager.EnsureInstance(); // 실제 장신구 전투 보관함 생성
        var marble = Resources.Load<RelicData>("Relics/GlassMarble"); // 최초 선택 효과를 보장하는 기존 유리 구슬 원본
        Assert.IsNotNull(marble); // 기존 유물 경로 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(marble)); // 실제 회차에서 최초 확률 효과를 보정하도록 획득
        try // 실제 유물 획득 상태의 검사 뒤 복원 보장
        { // 유물 보정과 지속 검사 시작
            int failure = FindSeed(roll => roll >= 25); // 보정 없이는 화상과 감전 확률이 실패할 난수
            var unchanged = StateAfterRolls(failure, 0); // 모든 확정 또는 면역 카드의 기준 난수 상태
            fight.Controller.CardUsed += (card, targets) => Random.InitState(failure); // 카드 명중 직전 공통 실패 난수 준비
            yield return fight.Use("COLD_TOUCH", 0); // 확률 필드 없이 기존 확정 냉기 명중
            var slow = fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.ActionSpeedDown); // 실제 적 행동력 순서의 기존 속도 약화
            Assert.AreEqual(1, slow?.EffectiveValue); // 냉기가 줄이는 실제 속도 한 점
            Assert.AreEqual(2, slow?.RemainingTurns); // 냉기 두 턴 규칙 확인
            Assert.AreEqual(unchanged, Random.state); // 확정 상태가 추가 난수를 소비하지 않는지 확인
            var immune = fight.Enemies[3]; // 최초 보장을 소비시키지 않아야 하는 면역 적
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 화상에 대한 일반 면역 사용
            yield return fight.Use("SMALL_FLAME", 3); // 보정 대상처럼 보이지만 상태를 받을 수 없는 적 명중
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Burn)); // 실제 상태 면역 처리 확인
            Assert.AreEqual(unchanged, Random.state); // 차단된 화상에도 확률 난수 미소모 확인
            var shocked = fight.Enemies[1]; // 유리 구슬을 실제로 사용할 첫 유효 확률 대상
            yield return fight.Use("STATIC_CHARGE", 1); // 기존 보장 덕분에 실패 난수를 건너뛰는 정전기 사용
            Assert.AreEqual(195, shocked.CurrentHealth); // 정전기의 독립 마법 피해 다섯 확인
            Assert.AreEqual(2, shocked.GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 보장된 감전이 두 적 턴 대기
            Assert.AreEqual(unchanged, Random.state); // 유리 구슬이 확률 난수를 소비하지 않는지 확인
            yield return fight.Use("STATIC_CHARGE", 2); // 유리 구슬을 이미 소모한 두 번째 실제 확률 명중
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Shock)); // 보정 뒤 남은 평범한 확률 실패 확인
            shocked.GainBlock(50); // 실제 감전이 우회해 체력에 도달할 방어도 준비
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 적 턴 시작
            Assert.AreEqual(194, shocked.CurrentHealth); // 방어도와 별개인 첫 감전 한 점 확인
            Assert.AreEqual(1, shocked.GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 첫 적 턴 이후 감전 한 번 잔존
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴의 정상 종료 처리
            Assert.AreEqual(1, shocked.GetStatusEffect(BattleStatusEffectType.Shock)?.RemainingTurns); // 적 턴 종료에 감전 횟수 두 번 감소 방지
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 적 턴 시작
            Assert.AreEqual(193, shocked.CurrentHealth); // 두 번째 감전도 실제 체력 한 점 차감
            Assert.IsNull(shocked.GetStatusEffect(BattleStatusEffectType.Shock)); // 두 번째 실제 발동 전에 마지막 감전 소모
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 세 번째 실제 적 턴 시작
            Assert.AreEqual(193, shocked.CurrentHealth); // 감전 횟수가 끝난 뒤 새로운 피해 없음 확인
        } // 유물 보정과 지속 검사 종료
        finally // 기존 테스트나 실제 회차의 유물 상태 복구
        { // 유물 검사 정리 시작
            manager.TryRemove(marble.RelicId); // 검사에서 획득한 유리 구슬 제거
            if (previous == null) // 검사에서 새로 만든 회차 관리자인지 확인
            { // 임시 보관함 정리 시작
                Object.Destroy(manager.gameObject); // 실제 검사에 사용한 임시 보관함 정리
            } // 임시 보관함 정리 종료
        } // 유물 검사 정리 종료
    } // 적 턴 상태 검사 종료

    [UnityTest] // 현재 화상 상태를 요구하는 실제 선택과 비용 보존 확인
    public IEnumerator EmberSpreadRejectsUnburnedAndStaleTargetsBeforePaying() // 실제 현재 화상 적만 공격하는 원본 카드 검증
    { // 불씨 확산 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 클릭과 적 선택용 플레이 모드 진입
        yield return CheckEmberSpread(); // 화상 없는 적과 비용 직전 만료 확인
        yield return new ExitPlayMode(); // 독립 플레이 모드 종료
    } // 불씨 확산 검사 종료

    private static IEnumerator CheckEmberSpread() // 화상 대상의 선택 및 지불 시점 안전 검사
    { // 불씨 대상 검증 시작
        using var fight = new Fight("EMBER_SPREAD"); // 실제 불씨 확산 원본 한 장 준비
        var active = fight.Enemies[1]; // 화상을 유지하거나 해제할 비교 적
        active.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, fight.Allies[0]); // 선택할 수 있는 실제 활성 화상 준비
        var view = fight.View("EMBER_SPREAD"); // 원본이 들어 있는 실제 손패 카드
        var card = view.RuntimeCard; // 유지하거나 버린 더미로 이동할 실제 인스턴스
        int points = fight.Points.CurrentActionPoints; // 지불 전 원래 아군 행동력
        view.OnPointerClick(null); // 실제 불씨 확산 대상 선택 상태 진입
        fight.EnemyViews[0].OnPointerClick(null); // 화상 없는 적을 실제로 선택 시도
        Assert.AreSame(card, fight.Controller.SelectedCard); // 잘못 고른 적 때문에 카드가 해결되지 않는지 확인
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 화상 없는 적의 행동력 결제 없음 확인
        Assert.AreEqual(200, fight.Enemies[0].CurrentHealth); // 화상 없는 적에게 실제 피해 없음 확인
        Assert.IsTrue(active.RemoveStatusEffect(BattleStatusEffectType.Burn)); // 클릭 이후 정산 직전에 화상이 해제된 대조 조건
        var execute = typeof(BattleCardActionController).GetMethod("ExecuteCard", BindingFlags.Instance | BindingFlags.NonPublic); // 대상 입력 이후의 기존 결제 진입점
        Assert.IsNotNull(execute); // 실제 결제 직전 방어 코드 확인 경로
        execute.Invoke(fight.Controller, new object[] { card, new[] { active } }); // 예전 선택 대상이 결제에 전달되는 도중의 상태 변경 재현
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 방금 화상을 잃은 대상에게 결제되지 않는지 확인
        Assert.IsNull(fight.Controller.SelectedCard); // 남아 있던 잘못된 대상 확정 입력 정리
        CollectionAssert.Contains(fight.Deck.Hand, card); // 실패한 시도의 실제 손패 카드 보존
        active.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, fight.Allies[0]); // 결제 시점까지 실제 활성 화상 복원
        yield return fight.Use("EMBER_SPREAD", 1); // 현재 화상을 가진 적에게 실제 불씨 확산 사용
        Assert.AreEqual(points - 1, fight.Points.CurrentActionPoints); // 실제 유효한 대상에게만 비용 한 점 결제
        Assert.AreEqual(192, active.CurrentHealth); // 활성 화상 대상의 기본 마법 여덟 피해
    } // 불씨 대상 검증 종료

    private static int FindSeed(System.Predicate<int> matches) // 정확한 정수 확률을 재현할 게임 난수 시드 검색
    { // 난수 경계 탐색 시작
        for (int seed = 0; seed < 1000; seed++) // 제한된 범위의 시드 비교
        { // 이번 난수 준비 시작
            Random.InitState(seed); // 실제 게임 난수 상태 기준값 설정
            if (matches(Random.Range(0, 100))) // 실제 첫 정수 난수의 원하는 범위 확인
            { // 적합한 시드 반환 시작
                return seed; // 다음 실제 명중에서 사용할 고정 시드
            } // 적합한 시드 반환 종료
        } // 이번 난수 준비 종료
        throw new System.InvalidOperationException("No matching seed was found."); // 제한 범위에서 필요한 정수 난수가 없는 검사 오류
    } // 난수 경계 탐색 종료

    private static Random.State StateAfterRolls(int seed, int count) // 원본 처리에서 정상적으로 소비할 난수의 기대 상태
    { // 난수 소비 상태 기록 시작
        Random.InitState(seed); // 실제 명중에서 사용할 시드로 초기화
        for (int index = 0; index < count; index++) // 예정된 정상 판정 수만큼 난수 계산
        { // 정상 난수 소비 시작
            Random.Range(0, 100); // 실제 공격과 같은 정수 값 소비
        } // 정상 난수 소비 종료
        return Random.state; // 실행 뒤와 비교할 실제 난수 상태
    } // 난수 소비 상태 기록 종료
} // 지팡이 상태 검사 종료
