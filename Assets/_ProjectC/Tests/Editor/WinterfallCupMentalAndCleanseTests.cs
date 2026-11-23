using System.Collections; // 실제 카드 연출과 턴 전환 대기
using System.Linq; // 아군의 실제 대상 화면 조회
using NUnit.Framework; // 전투 적용 및 사용 조건 검증
using UnityEngine; // 실제 아군 화면과 검사 적 원본 사용
using UnityEngine.TestTools; // 도메인 전환과 실제 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 손패와 카드 입력 전투 환경 사용

public sealed class WinterfallCupMentalAndCleanseTests // 성배의 정신력과 정화 및 성수 효과 검사
{ // 성배 지원 검사 시작
    [UnityTest] // 진정의 두 독립 조건과 사용 제한 검사
    public IEnumerator CalmingUsesEitherFearRemovalOrMentalRecovery() // 공포 또는 정신력 변화가 있는 대상만 사용 확인
    { // 진정 검사 모드 시작
        yield return new EnterPlayMode(); // 실제 플레이 모드 준비 완료 대기
        yield return VerifyCalming(); // 실제 카드 클릭과 효과 확인
        yield return new ExitPlayMode(); // 검사 전투 정리와 에디터 복귀
    } // 진정 검사 모드 종료

    private static IEnumerator VerifyCalming() // 실제 손패에서 진정 두 장과 거부 대상 비교
    { // 진정 전투 검사 시작
        using var fight = new Fight("CALMING", "CALMING"); // 두 대상에게 사용할 실제 진정 원본 두 장
        var frightened = fight.Allies[1]; // 공포만 지울 정신력 최대 대상
        var recovering = fight.Allies[2]; // 정신력만 회복할 공포 없는 대상
        frightened.ApplyPersistentMental(BattleMentalRuntime.MaximumMental); // 정신력 추가 회복이 불가능한 비교 상태
        recovering.ApplyPersistentMental(50); // 정신력 네 점을 회복할 일반 상태
        var frightenedView = AllyView(fight, frightened); // 공포 대상의 실제 선택 화면
        var card = fight.View("CALMING"); // 실제 손패에 있는 진정 카드 화면
        int points = fight.Points.CurrentActionPoints; // 잘못된 대상 선택 전 행동력 보관
        card.OnPointerClick(null); // 사용 비용을 지불하기 전 진정 선택
        frightenedView.OnPointerClick(null); // 정신력 최대이며 공포가 없는 아군 클릭
        Assert.IsFalse(fight.Runner.IsBusy); // 유효한 효과가 없는 입력의 사용 차단 확인
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 사용 불가 대상의 행동력 보존 확인
        CollectionAssert.Contains(fight.Deck.Hand, card.RuntimeCard); // 거부된 카드의 실제 손패 유지 확인
        fight.Controller.CancelSelection(); // 무효 지정 뒤 다음 입력을 위한 선택 해제
        frightened.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1); // 정신력이 가득 찬 아군의 실제 공포 부여
        frightened.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 2, 1); // 진정이 보존할 별도 약화 부여
        yield return UseOn(fight, "CALMING", frightenedView); // 공포만 제거할 수 있는 대상의 실제 사용
        Assert.IsNull(frightened.GetStatusEffect(BattleStatusEffectType.Fear)); // 정신력 상한에서도 공포 해제 확인
        Assert.IsNotNull(frightened.GetStatusEffect(BattleStatusEffectType.Burn)); // 공포 이외의 상태 보존 확인
        Assert.AreEqual(BattleMentalRuntime.MaximumMental, frightened.CurrentMental); // 최대 정신력 유지 확인
        yield return UseOn(fight, "CALMING", AllyView(fight, recovering)); // 정신력만 올릴 수 있는 대상의 실제 사용
        Assert.AreEqual(54, recovering.CurrentMental); // 공포가 없어도 실제 정신력 네 점 회복 확인
    } // 진정 전투 검사 종료

    [UnityTest] // 성수의 명중과 명시적 언데드 분류 검사
    public IEnumerator HolyWaterAddsDamageOnlyWhenItHitsAnAuthoredUndeadEnemy() // 기본 적 피해와 언데드 피해 및 빗나감 비교
    { // 성수 검사 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입 대기
        yield return VerifyHolyWater(); // 서로 다른 적 원본의 실제 피해 검증
        yield return new ExitPlayMode(); // 검사 전투 정리와 에디터 복귀
    } // 성수 검사 모드 종료

    private static IEnumerator VerifyHolyWater() // 일반과 명시적 언데드 원본의 실제 체력 감소 비교
    { // 성수 전투 검사 시작
        using (var fight = new Fight("HOLY_WATER")) // 언데드 표시가 없는 기존 적 원본의 전투
        { // 일반 적 피해 비교 시작
            var target = fight.Enemies[0]; // 언데드로 지정하지 않은 검사 적
            Assert.IsFalse(target.EnemySource.IsUndead); // 기존 적 데이터의 거짓 기본값 확인
            yield return fight.Use("HOLY_WATER", 0); // 실제 적 선택과 한 번 명중
            Assert.AreEqual(target.MaxHealth - 5, target.CurrentHealth); // 일반 적의 기본 마법 피해 다섯 확인
        } // 일반 적 피해 비교 종료
        using (var fight = new Fight("HOLY_WATER", "HOLY_WATER")) // 명시적 언데드에게 성공 및 실패 공격을 실행할 전투
        { // 언데드 피해 비교 시작
            var target = fight.Enemies[0]; // 독립 전투의 언데드 검사 적
            JsonUtility.FromJsonOverwrite("{\"isUndead\":true}", target.EnemySource); // 기존 게임 적과 분리된 저작 속성 명시
            Assert.IsTrue(target.EnemySource.IsUndead); // 실제 적 원본의 언데드 속성 적용 확인
            target.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 언데드에게 확정 회피 부여
            yield return fight.Use("HOLY_WATER", 0); // 언데드이지만 실제로 빗나간 마법 공격
            Assert.AreEqual(target.MaxHealth, target.CurrentHealth); // 빗나간 언데드의 체력 유지 확인
            target.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 실제 명중 비교를 위한 회피 제거
            yield return fight.Use("HOLY_WATER", 0); // 언데드에게 명중한 정상 마법 피해
            Assert.AreEqual(target.MaxHealth - 10, target.CurrentHealth); // 기본 다섯과 언데드 추가 다섯 확인
        } // 언데드 피해 비교 종료
    } // 성수 전투 검사 종료

    [UnityTest] // 정화 손길과 상처 봉합의 서로 다른 약화 제거 검사
    public IEnumerator PurifyingTouchGrantsTemporaryMagicResistanceAndWoundStitchingClearsOnlyBleed() // 디버프 없는 대상과 적 턴 종료 및 체력 회복 확인
    { // 정화 지원 검사 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입 대기
        yield return VerifyPurification(); // 정화와 회복을 실제 카드 입력으로 실행
        yield return new ExitPlayMode(); // 검사 전투 정리와 에디터 복귀
    } // 정화 지원 검사 모드 종료

    private static IEnumerator VerifyPurification() // 정화 손길의 대상별 저항과 봉합의 지정 해제 확인
    { // 정화 및 회복 전투 시작
        using var fight = new Fight("PURIFYING_TOUCH", "PURIFYING_TOUCH", "WOUND_STITCHING"); // 정화 두 장과 봉합 원본이 있는 실제 손패
        var affected = fight.Allies[1]; // 두 약화 중 먼저 생긴 약화만 지울 아군
        var healthy = fight.Allies[2]; // 디버프 없이 저항만 받을 아군
        var attacker = fight.Enemies[0]; // 추가 저항 적용 전후를 비교할 같은 직접 공격자
        var normalMagic = healthy.PreviewDamage(40, BattleDamageType.Magical, attacker, enemyDirectAttack: true); // 기본 마법 저항을 포함한 기존 직접 마법 예고
        var normalPhysical = healthy.PreviewDamage(40, BattleDamageType.Physical, attacker, enemyDirectAttack: true); // 기본 물리 방어를 포함한 기존 직접 물리 예고
        var normalIndirect = healthy.PreviewDamage(40, BattleDamageType.Magical, attacker); // 기본 마법 저항을 포함한 기존 비직접 마법 예고
        affected.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 정화로 먼저 제거할 약화
        affected.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1); // 첫 정화가 보존하고 봉합이 지울 출혈
        yield return UseOn(fight, "PURIFYING_TOUCH", AllyView(fight, affected)); // 실제 아군 한 명에게 정화 적용
        Assert.IsNull(affected.GetStatusEffect(BattleStatusEffectType.Poison)); // 첫 디버프 한 종류 제거 확인
        Assert.IsNotNull(affected.GetStatusEffect(BattleStatusEffectType.Bleed)); // 두 번째 디버프 보존 확인
        Assert.AreEqual(5, affected.GetStatusEffect(BattleStatusEffectType.ResistanceStance).EffectiveValue); // 정화와 저항의 동시 적용 확인
        yield return UseOn(fight, "PURIFYING_TOUCH", AllyView(fight, healthy)); // 약화가 없는 대상에게 저항만 부여
        var stance = healthy.GetStatusEffect(BattleStatusEffectType.ResistanceStance); // 저항만 적용된 대상의 상태 조회
        Assert.AreEqual(5, stance.EffectiveValue); // 원본의 직접 마법 피해 감소율 확인
        Assert.AreEqual(1, stance.RemainingTurns); // 한 적 턴의 지속 시간 확인
        int expectedMagic = normalMagic.FinalDamage * 95 / 100; // 기본 저항 뒤 정화 카드의 별도 오 퍼센트 경감
        Assert.AreEqual(expectedMagic, healthy.PreviewDamage(40, BattleDamageType.Magical, attacker, enemyDirectAttack: true).FinalDamage); // 직접 마법 피해 예고의 추가 경감 확인
        Assert.AreEqual(normalPhysical.FinalDamage, healthy.PreviewDamage(40, BattleDamageType.Physical, attacker, enemyDirectAttack: true).FinalDamage); // 캐릭터의 기존 물리 방어 결과 보존 확인
        Assert.AreEqual(normalIndirect.FinalDamage, healthy.PreviewDamage(40, BattleDamageType.Magical, attacker).FinalDamage); // 캐릭터의 기존 비직접 마법 저항 보존 확인
        Assert.AreEqual(expectedMagic, healthy.TakeDamage(40, BattleDamageType.Magical, attacker, enemyDirectAttack: true).AppliedDamage); // 실제 한 번의 마법 타격에도 같은 피해 감소 확인
        affected.TakeDamage(20, BattleDamageType.None); // 실제 체력 회복량을 측정할 부상 상태
        affected.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 2, 1); // 봉합이 지우지 않을 다른 약화
        int before = affected.CurrentHealth; // 봉합 직전 실제 체력 보관
        yield return UseOn(fight, "WOUND_STITCHING", AllyView(fight, affected)); // 일반 치유 경로를 사용하는 실제 봉합 입력
        Assert.AreEqual(before + 14, affected.CurrentHealth); // 성배 카드가 회복한 실제 체력 열네 점 확인
        Assert.IsNull(affected.GetStatusEffect(BattleStatusEffectType.Bleed)); // 원본에서 지정한 출혈 상태 제거 확인
        Assert.IsNotNull(affected.GetStatusEffect(BattleStatusEffectType.Burn)); // 동일 대상의 다른 상태 보존 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 시전한 아군 턴 종료와 적 턴 시작
        Assert.IsNotNull(healthy.GetStatusEffect(BattleStatusEffectType.ResistanceStance)); // 다음 적의 실제 직접 공격까지 저항 유지 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 저항을 적용한 첫 적 턴 종료
        Assert.IsNull(healthy.GetStatusEffect(BattleStatusEffectType.ResistanceStance)); // 종료한 적 턴 뒤 저항 만료 확인
    } // 정화 및 회복 전투 종료

    private static BattleUnitView AllyView(Fight fight, BattleUnitRuntime target) // 실제 전투 화면의 아군 대상 조회
    { // 대상 조회 시작
        return fight.Hand.GetComponentInParent<Canvas>().GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == target); // 지정 아군 런타임을 참조하는 유일한 클릭 화면 반환
    } // 대상 조회 종료

    private static IEnumerator UseOn(Fight fight, string id, BattleUnitView target) // 지원 카드의 실제 손패 선택과 대상 클릭
    { // 카드 사용 연결 시작
        return WinterfallCommonCardExpansionTests.Play(fight.View(id).RuntimeCard.SourceData, fight.Hand, target, fight.Runner, fight.Deck, fight.Points); // 기존 카드 사용과 지불 및 버림 검증 재사용
    } // 카드 사용 연결 종료
} // 성배 지원 검사 종료
