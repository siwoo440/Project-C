using System.Collections; // 플레이 모드와 실제 카드 연출 대기
using NUnit.Framework; // 카드 사용의 실제 상태와 피해 검증
using UnityEngine; // 검사 대상의 독립 게임 상태
using UnityEngine.TestTools; // 실제 카드 화면 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 기존 카드 입력과 행동력 검사 전투

public sealed class WinterfallCupHealingAndJudgmentTests // 성배의 회복과 불사 제거 검사
{ // 성배 카드 검사 시작
    [UnityTest] // 적 전체 명중의 생존 보호 제거 검사
    public IEnumerator HolyJudgmentDispelRequiresAnActualHitAndDoesNotBypassDamageNullification() // 마법 타격과 빗나감 및 피해 무효 확인
    { // 심판 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입
        yield return VerifyHolyJudgment(); // 독립 전투의 명중과 보호 해제 검사
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 심판 검사 종료

    private static IEnumerator VerifyHolyJudgment() // 실제 전체 마법 카드 공격의 보호 해제 순서 검증
    { // 신성한 심판 전투 시작
        using var fight = new Fight("HOLY_JUDGMENT"); // 등록된 성배 보물 카드 준비
        var exposed = fight.Enemies[0]; // 두 불사 상태를 가진 명중 대상
        var evasive = fight.Enemies[1]; // 피격되지 않을 보호 대상
        var immune = fight.Enemies[2]; // 일반 피해 무효와 불사를 함께 가진 명중 대상
        exposed.TakeDamage(exposed.MaxHealth - 12, BattleDamageType.None); // 정확히 열두 점이면 처치되는 체력 설정
        exposed.ApplyStatusEffect(BattleStatusEffectType.UndyingGuard, 1, 1, 1); // 생존 대상의 기존 불굴 준비
        exposed.ApplyStatusEffect(BattleStatusEffectType.DefeatWard, 1, 1, 1); // 실제 치명 피해 한 번 보호 준비
        evasive.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 이 카드가 확실히 빗나갈 대상 준비
        evasive.ApplyStatusEffect(BattleStatusEffectType.DefeatWard, 1, 1, 1); // 빗나감에서 유지할 전투불능 방지
        immune.ApplyStatusEffect(BattleStatusEffectType.DefeatWard, 1, 1, 1); // 명중 때 제거할 보호 상태
        immune.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 정상 마법 피해 계산에서 소모할 무효 상태
        yield return fight.Use("HOLY_JUDGMENT"); // 실제 카드 입력과 세 행동력 및 사용 이동 검증
        Assert.IsTrue(exposed.IsDead); // 기존 불굴과 전투불능 방지를 제거한 뒤 치명 마법 피해 확인
        Assert.IsNotNull(evasive.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 빗나간 대상의 생존 보호 보존 확인
        Assert.IsNotNull(evasive.GetStatusEffect(BattleStatusEffectType.Evasion)); // 심판이 회피를 미리 제거하지 않는지 확인
        Assert.AreEqual(evasive.MaxHealth, evasive.CurrentHealth); // 빗나간 마법 타격의 체력 보존 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 피해 무효와 별개로 명중한 생존 보호 해제 확인
        Assert.AreEqual(immune.MaxHealth, immune.CurrentHealth); // 일반 피해 무효를 그대로 적용한 타격 확인
    } // 신성한 심판 전투 종료

    [UnityTest] // 생존 아군 전체의 회복과 기존 공격력 상태 보호 검사
    public IEnumerator CelestialGraceGrantsAttackPowerEvenAtFullHealthAndOnlyForItsCurrentTurn() // 체력 상한과 기존 공격력 지속 및 임시 증가 확인
    { // 은총 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 사용 환경 진입
        yield return VerifyCelestialGrace(); // 독립 전투의 전체 회복과 공격력 검증
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 은총 검사 종료

    private static IEnumerator VerifyCelestialGrace() // 현재 턴만의 추가 공격력과 독립 강화 검사
    { // 천상의 은총 전투 시작
        using var fight = new Fight("CELESTIAL_GRACE"); // 실제 성배 회복 카드 준비
        var wounded = fight.Allies[0]; // 먼저 부상당하고 다중 턴 공격력이 있는 아군
        var full = fight.Allies[1]; // 이미 체력이 가득 찬 생존 아군
        var fallen = fight.Allies[2]; // 새 회복과 강화에서 제외할 전투불능 아군
        fallen.TakeDamage(fallen.CurrentHealth, BattleDamageType.None); // 살아 있지 않은 전체 대상의 대조 조건
        wounded.TakeDamage(25, BattleDamageType.None); // 원본 회복량 스무 점을 확인할 부상 설정
        wounded.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 1, 3, 1); // 은총과 별도로 남아 있어야 할 기존 공격력
        yield return fight.Use("CELESTIAL_GRACE"); // 실제 카드 입력과 네 행동력 및 사용 이동 검증
        Assert.AreEqual(wounded.MaxHealth - 5, wounded.CurrentHealth); // 일반 회복 경로의 실제 체력 스무 점 확인
        Assert.AreEqual(full.MaxHealth, full.CurrentHealth); // 회복이 필요 없는 아군의 체력 상한 보존 확인
        Assert.AreEqual(3, wounded.AttackPowerBonus); // 기존 공격력 한 점에 이 턴 은총 두 점 가산 확인
        Assert.AreEqual(2, full.AttackPowerBonus); // 체력이 가득 찬 아군도 현재 턴 강화 확인
        Assert.AreEqual(0, fallen.AttackPowerBonus); // 죽은 아군의 강화 제외 확인
        full.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 4, 3, 1); // 은총 뒤 새로 적용한 장기 공격력
        Assert.AreEqual(6, full.AttackPowerBonus); // 이후 일반 강화와 이 턴 은총의 독립 합산 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 새 적 턴 전에 은총의 현재 턴 종료
        Assert.AreEqual(1, wounded.AttackPowerBonus); // 먼저 받은 장기 강화의 원래 값 보존 확인
        Assert.AreEqual(4, full.AttackPowerBonus); // 이후 받은 장기 강화의 원래 값 보존 확인
    } // 천상의 은총 전투 종료

    [UnityTest] // 응급 처치와 온기의 지정 적이 아닌 아군 효과 검사
    public IEnumerator FirstAidAndWarmthRemoveOnlyTheirOwnNamedStatus() // 회복과 방어도 및 다른 약화 보존 확인
    { // 정화 성배 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 모드 진입
        yield return VerifyFirstAidAndWarmth(); // 지정 대상의 실제 사용과 정화 확인
        yield return new ExitPlayMode(); // 실제 카드 검사 종료
    } // 정화 성배 검사 종료

    private static IEnumerator VerifyFirstAidAndWarmth() // 한 아군의 출혈과 다른 아군의 화상을 별도 해제
    { // 응급 처치와 온기 전투 시작
        using var fight = new Fight("FIRST_AID", "WARMTH"); // 실제 성배 보물 원본 두 종 준비
        var injured = fight.Allies[1]; // 응급 처치를 받을 아군
        var burning = fight.Allies[2]; // 온기를 받을 다른 아군
        injured.TakeDamage(10, BattleDamageType.None); // 회복량 다섯 점을 읽을 부상 준비
        injured.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 2); // 응급 처치로 지울 출혈 부여
        injured.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 2, 1); // 응급 처치가 지우지 않을 화상
        burning.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 2, 1); // 온기가 지울 다른 아군의 화상
        burning.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 온기가 보존할 중독
        var canvas = fight.Hand.GetComponentInParent<Canvas>(); // 실제 아군 화면이 달린 전투 화면
        var views = canvas.GetComponentsInChildren<BattleUnitView>(); // 클릭 가능한 아군 화면 조회
        var injuredView = System.Array.Find(views, view => view.RuntimeUnit == injured); // 지정 회복 아군의 실제 선택 화면
        var burningView = System.Array.Find(views, view => view.RuntimeUnit == burning); // 지정 온기 아군의 실제 선택 화면
        yield return WinterfallCommonCardExpansionTests.Play(fight.View("FIRST_AID").RuntimeCard.SourceData, fight.Hand, injuredView, fight.Runner, fight.Deck, fight.Points); // 실제 응급 처치 대상 클릭과 카드 정산
        Assert.AreEqual(injured.MaxHealth - 5, injured.CurrentHealth); // 실제 체력 다섯 점 회복 확인
        Assert.IsNull(injured.GetStatusEffect(BattleStatusEffectType.Bleed)); // 선택한 아군의 출혈 상태 하나 제거 확인
        Assert.IsNotNull(injured.GetStatusEffect(BattleStatusEffectType.Burn)); // 응급 처치의 화상 보존 확인
        yield return WinterfallCommonCardExpansionTests.Play(fight.View("WARMTH").RuntimeCard.SourceData, fight.Hand, burningView, fight.Runner, fight.Deck, fight.Points); // 실제 온기 대상 클릭과 카드 정산
        Assert.AreEqual(3, burning.CurrentBlock); // 온기가 선택한 아군에게 준 실제 방어도 세 점 확인
        Assert.IsNull(burning.GetStatusEffect(BattleStatusEffectType.Burn)); // 지정 아군의 화상 해제 확인
        Assert.IsNotNull(burning.GetStatusEffect(BattleStatusEffectType.Poison)); // 같은 아군의 다른 약화 보존 확인
        Assert.IsNotNull(injured.GetStatusEffect(BattleStatusEffectType.Burn)); // 선택하지 않은 아군의 화상 보존 확인
    } // 응급 처치와 온기 전투 종료
} // 성배 카드 검사 종료
