using System.Collections; // 실제 전투의 손패 사용과 대기
using System.Collections.Generic; // 광역 다단의 실제 피해 기록
using System.Linq; // 선택 아군 화면 조회
using NUnit.Framework; // 실제 상태 수명과 피해 검증
using UnityEngine.TestTools; // 플레이 모드 실행
using Fight = WinterfallSwordCombatTests.Fight; // 실제 등록 원본의 전투 검사 환경

public sealed class WinterfallPentacleTacticalTests // 전술 표식과 도구 손질의 실제 전투 검사
{ // 펜타클 전술 검사 시작
    [UnityTest] // 이전 공명 표식과 새 적 턴 표식의 동시 검사
    public IEnumerator TacticalMarkUsesTheNativeMarkForTwoEnemyTurnsWithoutExtendingLegacyMark() // 정상 면역과 룬 폭발의 표식 공유 확인
    { // 전술 표식 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 원본 카드 및 상태 턴 계층 실행
        yield return CheckTacticalMark(); // 룬 피해와 두 지속 시계 및 면역 확인
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 전술 표식 플레이 모드 종료

    private static IEnumerator CheckTacticalMark() // 같은 원래 상태의 두 실제 종료 경계 검사
    { // 표식이 적용되거나 차단될 적 준비 시작
        using var fight = new Fight("TACTICAL_MARK", "TACTICAL_MARK", "RUNE_BURST"); // 실제 전술 표식 두 장과 기존 결합 공격 원본
        var target = fight.Enemies[0]; // 카드로 실제 적 턴 두 번 표식을 받을 대상
        var legacy = fight.Enemies[1]; // 기존 수신기 방식의 먼저 끝나는 표식 비교
        var immune = fight.Enemies[2]; // 일반 상태 면역을 가진 생존 적
        legacy.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 1, 1); // 원래 공명 표식의 한 아군 턴 설정
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 기존 상태 적용을 차단하는 실제 면역
        yield return fight.Use("TACTICAL_MARK", 0); // 생존 적 한 명의 전술 네이티브 표식 적용
        var mark = target.GetStatusEffect(BattleStatusEffectType.PublicMark); // 같은 기존 표식 종류의 현재 상태 조회
        Assert.IsTrue(mark.PublicMarkCountsEnemyTurns); // 전술 카드가 사용하는 적 턴 전용 수명 확인
        Assert.AreEqual(1, mark.EffectiveValue); // 기존 네이티브 표식의 기준 수치 한 점
        Assert.AreEqual(2, mark.RemainingTurns); // 현재 두 실제 적 턴의 초기 수명
        Assert.AreEqual(200, target.CurrentHealth); // 지원 카드의 즉시 피해 없음
        Assert.IsTrue(target.HasActiveCardDamageMark); // 기존 룬 특수 타격과 같은 표식 판정
        yield return fight.Use("TACTICAL_MARK", 2); // 원래 면역인 적의 실제 카드 약화 요청
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 일반 약화 면역 유지
        yield return fight.Use("RUNE_BURST", 0); // 기존 카드를 사용해 표식 공유 피해 확인
        Assert.AreEqual(183, target.CurrentHealth); // 원래 마법 열하나와 활성 표식의 추가 여섯
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴 종료와 첫 적 진영 시작
        Assert.IsNull(legacy.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 기존 원래 공명 표식은 이 아군 턴 끝에 그대로 만료
        Assert.AreEqual(2, mark.RemainingTurns); // 전술 표식은 아군 턴마다 줄지 않는 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 첫 적 턴 종료
        Assert.AreEqual(1, mark.RemainingTurns); // 한 적 턴만 지난 현재 수명
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 적 턴 시작
        Assert.AreSame(mark, target.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 기존 표식 제거에서 두 번째 적 턴 동안의 원본 유지
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 정확히 두 적 턴 종료로 표식 만료
    } // 표식이 적용되거나 차단될 적 준비 종료

    [UnityTest] // 선택한 아군만 보유하는 다음 한 번 실제 명중 강화 검사
    public IEnumerator ToolMaintenanceWaitsThroughMissAndImmunityThenBoostsOneRealWaveHit() // 순수 예고와 실제 유효 타격 및 종료 정리 확인
    { // 도구 손질 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 아군 선택 카드와 직접 피해 조합 실행
        yield return CheckToolMaintenance(); // 잘못된 소유자와 무효 뒤의 실제 광역 피해 비교
        yield return new ExitPlayMode(); // 실제 격리 전투 종료
    } // 도구 손질 플레이 모드 종료

    private static IEnumerator CheckToolMaintenance() // 카드 실제 선택과 한 타격 효과 소모 비교
    { // 실제 등록 카드와 네 적의 비교 준비 시작
        using var fight = new Fight("TOOL_MAINTENANCE", "TOOL_MAINTENANCE", "TOOL_MAINTENANCE", "RUNE_BURST", "RUNE_BURST", "RUNE_BURST", "EYE_OF_STORM"); // 실제 지원과 단일 및 광역 카드 원본
        var actor = fight.Allies[0]; // 실제 카드의 원래 시전자
        var other = fight.Allies[1]; // 자신의 타격까지 별도로 강화를 보유할 동료
        var evade = fight.Enemies[0]; // 원래 선택의 다음 타격을 전부 피할 적
        var protectedEnemy = fight.Enemies[1]; // 다음 실제 타격 한 번을 무효화할 적
        var witness = fight.Enemies[2]; // 잘못된 소유자의 일반 원본 피해 대조
        yield return CastMaintenance(fight, other); // 다른 생존 아군을 실제 선택하여 등록 효과 적용
        Assert.IsNotNull(other.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)); // 실제 동료의 강화 상태 보유 확인
        yield return fight.Use("RUNE_BURST", 2); // 강화가 없고 소유자가 다른 일반 공격
        Assert.AreEqual(189, witness.CurrentHealth); // 다른 아군에게 걸린 가산을 빼앗지 않는 실제 피해
        yield return CastMaintenance(fight, actor); // 실제 시전자 자신을 선택한 첫 강화
        yield return CastMaintenance(fight, actor); // 같은 소유자의 중첩 없는 갱신
        var prepared = actor.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp); // 첫 유효 직접 피해를 기다리는 실제 강화
        Assert.IsTrue(prepared.WaitsForAction); // 일반 턴이 아니라 다음 실제 공격 한 번의 수명
        Assert.AreEqual(1, prepared.StackCount); // 원래 한 번 제한의 재시전
        Assert.AreEqual(15, witness.PreviewDamage(11, BattleDamageType.Magical, actor, alliedDirectAttack: true).RawDamage); // 같은 순수 예고에서 실제 가산 네 점
        evade.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 3, 1); // 현재 적의 원래 확정 회피
        yield return fight.Use("RUNE_BURST", 0); // 맞지 않는 첫 실제 공격
        Assert.AreSame(prepared, actor.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)); // 빗나감이 이후 유효 타격 기회를 지우지 않는 확인
        protectedEnemy.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 2, 1); // 원래 한 차례 피해 무효화 상태
        yield return fight.Use("RUNE_BURST", 1); // 맞아도 체력과 방어도 모두 피해가 없는 타격
        Assert.AreSame(prepared, actor.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)); // 유효 피해가 없는 실제 타격 뒤 강화 유지
        var waves = new List<int>(); // 피해 기록으로 확인할 동일 적의 독립 광역 두 번
        protectedEnemy.DamageResolved += (source, unit, result) => waves.Add(result.RawDamage); // 실제 피해 전 수치의 원본 사건 구독
        yield return fight.Use("EYE_OF_STORM"); // 첫 적은 피하고 다른 적들에게 두 차례 실제 피해
        CollectionAssert.AreEqual(new[] { 18, 14 }, waves); // 최초 실제 명중의 네 점 추가와 같은 카드 두 번째 정상 피해
        Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)); // 한 번의 유효 타격 뒤 실제 상태 해제
        Assert.IsNotNull(other.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)); // 다른 아군의 남은 독립 강화 보존
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴 종료
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 턴 종료로 다음 실제 아군 턴 진입
        Assert.IsNotNull(other.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)); // 미사용 강화가 여러 진영 턴에 유지되는 확인
        Assert.IsTrue(fight.Turns.TryEscape()); // 현재 전투의 실제 종료 결과 확정
        Assert.IsNull(other.GetStatusEffect(BattleStatusEffectType.ToolMaintenanceAttackUp)); // 미사용 강화의 전투 종료 정리
    } // 실제 등록 카드와 네 적의 비교 준비 종료

    private static IEnumerator CastMaintenance(Fight fight, BattleUnitRuntime unit) // 지정한 생존 아군에게 실제 손패 지원 카드 선택
    { // 실제 아군 대상 화면 조회 시작
        var view = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(item => item.RuntimeUnit == unit); // 소유자와 별개인 실제 선택 아군 화면
        var card = fight.View("TOOL_MAINTENANCE").RuntimeCard.SourceData; // 등록된 현재 손패 원본 조회
        return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 실제 카드 이동과 비용 및 대상 확인
    } // 실제 아군 대상 화면 조회 종료
} // 펜타클 전술 검사 종료
