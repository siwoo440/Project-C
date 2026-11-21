using System.Collections; // 실제 전투 카드 입력과 모드 전환 대기
using System.Linq; // 감전을 전이받은 다른 생존 적 조회
using NUnit.Framework; // 원소 카드의 실제 피해와 상태 기간 확인
using UnityEngine; // 실제 적 원본의 검사 전용 저항 수치 설정
using UnityEngine.TestTools; // 실제 손패 조작을 위한 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 실제 전투와 손패 및 적 클릭의 공용 환경

public sealed class WinterfallWandElementSupportTests // 비전 보호와 화상 및 감전과 동결의 실제 카드 검사
{ // 지팡이 원소 카드 회귀 검사 시작
    [UnityTest] // 진짜 아군 파티의 마법 저항과 두 적 턴 확인
    public IEnumerator ArcaneProtectionReusesEnemyTurnResistanceForAllLivingAllies() // 비전 보호의 마법 피해에만 적용되는 원래 저항 규칙 확인
    { // 비전 보호의 실제 손패 적용 시작
        yield return new EnterPlayMode(); // 실제 카드 및 기존 상태 처리기가 연결되는 플레이 모드
        using (var fight = new Fight("ARCANE_PROTECTION")) // 비전 보호 원본과 아군 셋이 있는 실제 전투
        { // 생존 아군과 전투불능 대상 및 피해 예고 비교 시작
            var owner = fight.Allies[0]; // 원본 카드의 실제 사용 아군
            var other = fight.Allies[1]; // 원래 일괄 강화에 포함할 생존 아군
            var fallen = fight.Allies[2]; // 전체 생존 강화에서 제외할 전투불능 아군
            fallen.ApplyPersistentHealth(0); // 기존 생존 후보를 실제 두 명으로 설정
            var enemy = fight.Enemies[0]; // 정상 적 직접 마법 피해의 출처
            int magic = owner.PreviewDamage(30, BattleDamageType.Magical, enemy, enemyDirectAttack: true).FinalDamage; // 강화 전 실제 마법 피해 예고 수치
            int physical = owner.PreviewDamage(30, BattleDamageType.Physical, enemy, enemyDirectAttack: true).FinalDamage; // 마법 저항 보정 전 실제 물리 공격 예고
            Assert.AreEqual(2, fight.View("ARCANE_PROTECTION").RuntimeCard.ApCost); // 실제 시트의 행동력 두 점 비용 확인
            yield return fight.Use("ARCANE_PROTECTION"); // 실제로 모든 아군의 원래 저항 상태 부여
            Assert.AreEqual(15, owner.GetStatusEffect(BattleStatusEffectType.ResistanceStance).EffectiveValue); // 시전자에게 원래 저항 태세 수치 15 적용 확인
            Assert.AreEqual(15, other.GetStatusEffect(BattleStatusEffectType.ResistanceStance).EffectiveValue); // 다른 생존 아군에도 실제 저항이 동시에 붙는지 확인
            Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.ResistanceStance)); // 전투불능 아군이 상태를 받지 않음 확인
            Assert.Less(owner.PreviewDamage(30, BattleDamageType.Magical, enemy, enemyDirectAttack: true).FinalDamage, magic); // 기존 적 직접 마법 피해에 실제 경감 적용 확인
            Assert.AreEqual(physical, owner.PreviewDamage(30, BattleDamageType.Physical, enemy, enemyDirectAttack: true).FinalDamage); // 물리 직접 피해의 원래 경감 유지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 턴을 끝내고 남은 기간 확인
            Assert.AreEqual(1, owner.GetStatusEffect(BattleStatusEffectType.ResistanceStance).RemainingTurns); // 저항 태세가 적 턴을 한 번만 지나 한 턴 남음 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 정상 종료
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.ResistanceStance)); // 지정한 두 적 차례 뒤 원래 상태 만료 확인
        } // 생존 아군과 전투불능 대상 및 피해 예고 비교 종료
        yield return new ExitPlayMode(); // 비전 보호 실제 전투 환경 종료
    } // 비전 보호의 실제 손패 적용 종료

    [UnityTest] // 활성 화상만 연장하며 중첩과 피해량 무변경 검사
    public IEnumerator BurnAmplificationExtendsOnlyExistingActiveBurnsWithoutAddingStacksOrDamage() // 화상 없는 사용 차단과 기존 화상의 정확한 기간 증가 확인
    { // 실제 화상 연장 카드 검사 시작
        yield return new EnterPlayMode(); // 손패와 적 상태를 연결한 플레이 모드 진입
        using (var fight = new Fight("BURN_AMPLIFICATION")) // 원본 화상 연장 카드와 네 생존 적의 실제 전투
        { // 초기 무효 입력과 두 활성 화상의 상태 비교 시작
            var source = fight.View("BURN_AMPLIFICATION"); // 실제 손패에 있는 원화 연결 카드
            int points = fight.Points.CurrentActionPoints; // 상태 대상이 없을 때 유지될 공용 행동력
            source.OnPointerClick(null); // 어떤 적도 화상에 걸리지 않은 실제 카드 클릭
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 대상 없는 화상 연장의 결제 금지 확인
            CollectionAssert.Contains(fight.Deck.Hand, source.RuntimeCard); // 무효 입력에도 원래 카드가 손패에 남음 확인
            var first = fight.Enemies[0]; // 실제 화상 중첩과 이미 줄어든 피해량을 가진 대상
            var second = fight.Enemies[1]; // 한 턴의 독립 화상을 가진 다른 생존 대상
            var expired = fight.Enemies[2]; // 남은 시간이 이미 영인 화상 상태의 비교 적
            first.ApplyStatusEffect(BattleStatusEffectType.Burn, 4, 2, 3, fight.Allies[0]); // 기존 화상 피해 및 기간 부여
            first.ApplyStatusEffect(BattleStatusEffectType.Burn, 4, 2, 3, fight.Allies[0]); // 기존 화상 중첩을 실제 두 개로 준비
            second.ApplyStatusEffect(BattleStatusEffectType.Burn, 5, 1, 1, fight.Allies[0]); // 두 번째 실제 활성 화상 대상 추가
            expired.ApplyStatusEffect(BattleStatusEffectType.Burn, 6, 1, 1, fight.Allies[0]); // 만료될 비교 화상의 실제 상태 생성
            expired.GetStatusEffect(BattleStatusEffectType.Burn).AdvanceDuration(); // 목록에 남아 있어도 이미 만료된 비교 상태
            var burn = first.GetStatusEffect(BattleStatusEffectType.Burn); // 계속 유지될 실제 기존 화상의 참조
            burn.DecayBurn(); // 화상 값의 자연 감소를 재현해 기존 누적 손실 보존 확인
            int effective = burn.EffectiveValue; // 화상 연장 전 실제 남은 피해량 보관
            yield return fight.Use("BURN_AMPLIFICATION"); // 다른 대상 클릭 없이 활성 화상 대상들의 기간 자동 연장
            Assert.AreEqual(points - 2, fight.Points.CurrentActionPoints); // 성공한 화상 연장의 행동력 두 점만 차감 확인
            Assert.AreSame(burn, first.GetStatusEffect(BattleStatusEffectType.Burn)); // 새로운 화상 부여 없이 원래 상태 인스턴스 유지
            Assert.AreEqual(3, burn.RemainingTurns); // 중첩 화상의 실제 지속을 정확히 한 턴 연장
            Assert.AreEqual(2, burn.StackCount); // 처음부터 남은 기존 화상 중첩 수 보존
            Assert.AreEqual(effective, burn.EffectiveValue); // 자연 감소가 반영된 현재 화상 피해 수치 보존
            Assert.AreEqual(2, second.GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 다른 생존 대상의 한 턴도 두 턴으로 정확히 연장
            Assert.IsTrue(expired.GetStatusEffect(BattleStatusEffectType.Burn).IsExpired); // 이미 만료된 화상은 연장하지 않음 확인
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.Burn)); // 화상이 없던 적에게는 새 상태 미부여 확인
        } // 초기 무효 입력과 두 활성 화상의 상태 비교 종료
        yield return new ExitPlayMode(); // 실제 화상 연장 검사 종료
    } // 실제 화상 연장 카드 검사 종료

    [UnityTest] // 명중감전 전이와 최초의 기존 감전 보존 검사
    public IEnumerator ShockTransferRequiresAnActiveShockedPrimaryAndTransfersOnlyAfterALandedHit() // 감전된 첫 적과 전이될 다른 적 및 회피 경계 확인
    { // 감전 전이의 실제 대상과 명중 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 감전 지속을 구동하는 모드 진입
        using (var fight = new Fight("SHOCK_TRANSFER", "SHOCK_TRANSFER")) // 처음 빗나감과 다음 명중을 비교할 실제 두 장의 손패
        { // 시전 전 대상 확정과 후속 전이 비교 시작
            var first = fight.Enemies[0]; // 이미 감전 상태를 가지고 공격을 받을 첫 적
            var source = fight.View("SHOCK_TRANSFER"); // 실제 회수 가능한 감전 전이 카드 화면
            int points = fight.Points.CurrentActionPoints; // 무효 카드 시도 전 공용 행동력
            source.OnPointerClick(null); // 감전된 적이 없는 상태에서의 실제 선택 시도
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 유효한 최초 대상이 없을 때 결제 금지 확인
            first.ApplyStatusEffect(BattleStatusEffectType.Shock, 3, 2, 1); // 꼭 보존할 원래 감전 수치와 기간 설치
            var original = first.GetStatusEffect(BattleStatusEffectType.Shock); // 전이로 새로 만들어지지 않아야 하는 실제 최초 감전
            source.OnPointerClick(null); // 감전 상태가 있는 이제 카드의 정상 적 선택 시작
            fight.EnemyViews[1].OnPointerClick(null); // 감전이 없는 다른 적을 최초 대상으로 잘못 선택
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 비감전 적 클릭에 실제 비용이 차감되지 않음 확인
            first.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 첫 타격이 원래 명중 경계에서 빗나갈 높은 회피 준비
            fight.Controller.CancelSelection(); // 잘못 고른 적 입력을 정상 선택 상태로 복구
            yield return fight.Use("SHOCK_TRANSFER", 0); // 감전된 실제 대상에 원래 명중 실패 공격
            Assert.AreEqual(200, first.CurrentHealth); // 빗나간 첫 시도의 체력 피해 없음 확인
            Assert.IsFalse(fight.Enemies.Skip(1).Any(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Shock) != null)); // 빗나간 타격으로는 다른 적에게 전이되지 않음 확인
            first.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 다음 실제 감전 전이가 정상 명중하도록 상태 제거
            yield return fight.Use("SHOCK_TRANSFER", 0); // 감전 시작 대상에게 실제 마법 피해를 명중
            Assert.AreEqual(188, first.CurrentHealth); // 실제 감전 전이 기본 피해 열두 점 확인
            Assert.AreSame(original, first.GetStatusEffect(BattleStatusEffectType.Shock)); // 원래 감전을 지우거나 다시 부여하지 않음 확인
            Assert.AreEqual(3, original.EffectiveValue); // 시작 감전 피해 수치 그대로 유지 확인
            Assert.AreEqual(2, original.RemainingTurns); // 시작 감전의 원래 남은 적 턴 보존 확인
            var transfer = fight.Enemies.Skip(1).Select(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Shock)).Single(status => status != null); // 서로 다른 다른 생존 적 단 한 명의 실제 감전 조회
            Assert.AreEqual(1, transfer.EffectiveValue); // 전이한 감전에 새로 설정한 한 점 피해 수치 확인
            Assert.AreEqual(2, transfer.RemainingTurns); // 다른 적에게 기존 규칙의 두 실제 감전 발동 턴 확인
        } // 시전 전 대상 확정과 후속 전이 비교 종료
        yield return new ExitPlayMode(); // 감전 실제 카드 입력 검사 종료
    } // 감전 전이의 실제 대상과 명중 검사 종료

    [UnityTest] // 동결의 두 독립 상태와 실제 적 차례 기절 만료 검사
    public IEnumerator FateFreezeAppliesNativeStunAndMagicalResistanceDownForTheirOwnDurations() // 운명 동결의 기절 한 적 턴과 저항 약화 두 적 턴 확인
    { // 운명 동결의 실제 카드 정산 검사 시작
        yield return new EnterPlayMode(); // 실제 카드를 클릭하고 원래 상태를 적용할 플레이 모드
        var source = ScriptableObject.CreateInstance<EnemyData>(); // 실제 전투 생성 시 기본 저항을 읽을 검사 적 원본
        JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"magicalResistance\":20}", source); // 전투 유닛을 만들기 전 원본 마법 저항 설정
        try // 임시 검사 적의 사용과 정리 범위
        { // 일반 적과 약화 면역 적의 원래 규칙 확인 시작
            using var fight = Fight.WithEnemySources(new[] { source, source }, "FATE_FREEZE", "FATE_FREEZE"); // 동일한 시작 저항을 갖는 두 원본 카드의 실제 전투
            var target = fight.Enemies[0]; // 두 상태를 서로 다른 기간으로 받을 기본 적
            var immune = fight.Enemies[1]; // 두 기존 약화를 모두 저지할 면역 적
            Assert.AreEqual(20, target.MagicalResistance); // 전투 시작에 실제 유닛이 확보한 기본 저항 확인
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 정상 디버프 면역의 실제 사전 상태
            Assert.AreEqual(3, fight.View("FATE_FREEZE").RuntimeCard.ApCost); // 실제 시트의 운명 동결 비용 확인
            yield return fight.Use("FATE_FREEZE", 0); // 일반 적에 실제 동결 카드 사용
            Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.Stun).RemainingTurns); // 한 실제 적 턴 동안 기존 기절 적용 확인
            Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.MagicalResistanceDown).RemainingTurns); // 기존 마법 저항 약화의 별도 두 턴 확인
            Assert.AreEqual(10, target.MagicalResistance); // 대상의 실제 마법 저항이 이십에서 십으로 감소 확인
            yield return fight.Use("FATE_FREEZE", 1); // 상태 면역을 가진 다른 적에 실제 카드 사용
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Stun)); // 면역 적에게 기절이 걸리지 않는지 확인
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.MagicalResistanceDown)); // 독립 저항 감소에도 같은 기존 면역 존중 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 대상의 한 번 실제 적 턴 완료
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Stun)); // 기절은 첫 실제 적 차례 이후 제거 확인
            Assert.AreEqual(10, target.MagicalResistance); // 저항 약화는 독립된 두 번째 적 차례까지 유지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 다음 실제 적 턴 완료
            Assert.AreEqual(20, target.MagicalResistance); // 두 번째 적 턴이 끝난 뒤 원래 마법 저항 복원 확인
        } // 일반 적과 약화 면역 적의 원래 규칙 확인 종료
        finally // 검사 결과와 관계없이 임시 적 원본 정리
        { // 검사 장면 원본 정리 시작
            Object.Destroy(source); // 등록된 게임 에셋과 분리된 검사 적 제거
        } // 검사 장면 원본 정리 종료
        yield return new ExitPlayMode(); // 운명 동결의 실제 카드 검사 종료
    } // 운명 동결의 실제 카드 정산 검사 종료
} // 지팡이 원소 카드 회귀 검사 종료
