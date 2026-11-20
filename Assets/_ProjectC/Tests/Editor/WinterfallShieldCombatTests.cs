using System.Collections; // 플레이 모드 검사 대기
using System.Linq; // 대상 화면과 카드 위치 조회
using NUnit.Framework; // 전투 결과 비교
using UnityEngine.TestTools; // 실제 손패 실행 환경
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 환경 재사용
using Random = UnityEngine.Random; // 카드 상태 확률 재현

public sealed class WinterfallShieldCombatTests // 겨울몰락 방패 보물 카드의 실제 사용 검사
{ // 방패 전투 검사 시작
    [UnityTest] // 충격 흡수 사용과 적 턴 경계 검사
    public IEnumerator ShockAbsorptionReducesEveryDirectHitUntilEnemyTurnEnds() // 두 공격 유형과 다음 턴 만료 확인
    { // 충격 흡수 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 환경 진입
        yield return CheckShockAbsorption(); // 피해 예고와 실제 피해 비교
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 충격 흡수 검사 종료

    private static IEnumerator CheckShockAbsorption() // 충격 흡수의 피격별 고정 경감
    { // 피격 경감 검사 시작
        using (var fight = new Fight("SHOCK_ABSORPTION")) // 지급되는 충격 흡수 카드 준비
        { // 여러 적의 타격 준비 시작
            var owner = fight.Allies[0]; // 충격 흡수 카드 소유자
            var physical = CreateAttack(fight.Enemies[0], owner, BattleDamageType.Physical, 10); // 방어 적용 후 피해 아홉의 물리 공격
            var magical = CreateAttack(fight.Enemies[1], owner, BattleDamageType.Magical, 10); // 저항 적용 후 피해 아홉의 마법 공격
            var small = CreateAttack(fight.Enemies[2], owner, BattleDamageType.Physical, 3); // 경감보다 작은 직접 공격
            int healthBefore = owner.CurrentHealth; // 피해 전 체력 보관
            yield return fight.Use("SHOCK_ABSORPTION"); // 소유자 자신에게 카드 사용
            Assert.IsNull(fight.Allies[1].GetStatusEffect(BattleStatusEffectType.ShockAbsorption)); // 다른 아군에게 보호 미적용 확인
            owner.GainBlock(6); // 두 타격에서 나누어 소모할 방어도
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 보호가 적용되는 첫 적 턴 시작
            Assert.AreEqual(9, owner.PreviewDamage(10, BattleDamageType.Physical, fight.Enemies[0]).FinalDamage); // 적 직접 공격으로 표시되지 않은 피해 보존
            Assert.AreEqual(0, small.PreviewDamage().FinalDamage); // 고정 경감 뒤 피해 영 하한 확인
            Assert.AreEqual(4, physical.PreviewDamage().AbsorbedDamage); // 경감한 뒤 방어도에 닿는 물리 피해 네 점 확인
            var first = physical.Execute().DamageResult; // 같은 적 턴의 첫 직접 공격 실행
            Assert.AreEqual(4, first.AbsorbedDamage); // 첫 타격에 방어도 네 점 소모
            Assert.AreEqual(0, first.AppliedDamage); // 첫 타격의 체력 손실 없음 확인
            Assert.AreEqual(2, owner.CurrentBlock); // 두 번째 명중 전 잔여 방어도 확인
            Assert.AreEqual(2, magical.PreviewDamage().AppliedDamage); // 두 번째 공격에도 고정 경감과 잔여 방어도 적용 확인
            var second = magical.Execute().DamageResult; // 같은 적 턴의 두 번째 직접 공격 실행
            Assert.AreEqual(2, second.AbsorbedDamage); // 두 번째 타격에 잔여 방어도 소모
            Assert.AreEqual(healthBefore - 2, owner.CurrentHealth); // 두 타격의 실제 체력 손실 합계 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 턴 종료로 충격 흡수 만료
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.ShockAbsorption)); // 첫 적 턴 뒤 보호 제거 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 카드 재사용 없이 다음 적 턴 시작
            Assert.AreEqual(9, physical.PreviewDamage().AppliedDamage); // 다음 적 턴에 일반 피해 복원 확인
        } // 여러 적의 타격 준비 종료
    } // 피격 경감 검사 종료

    [UnityTest] // 방어 지령 단일 아군 선택 검사
    public IEnumerator DefenseOrderRaisesOnlyTheSelectedAllysDefenseForTwoEnemyTurns() // 동료 단독 강화와 만료 확인
    { // 방어 지령 검사 시작
        yield return new EnterPlayMode(); // 실제 대상 입력 환경 진입
        yield return CheckDefenseOrder(); // 지정한 생존 동료의 두 턴 방어 비교
        yield return new ExitPlayMode(); // 실제 대상 입력 환경 종료
    } // 방어 지령 검사 종료

    private static IEnumerator CheckDefenseOrder() // 아군 화면을 통한 단일 강화 적용
    { // 동료 방어 검사 시작
        using (var fight = new Fight("DEFENSE_ORDER")) // 지급되는 방어 지령 카드 준비
        { // 단일 아군 대상 준비 시작
            var owner = fight.Allies[0]; // 카드를 사용하는 소유자
            var ally = fight.Allies[1]; // 실제로 클릭할 다른 아군
            var other = fight.Allies[2]; // 선택하지 않은 동료
            var targetView = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == ally); // 전투가 이미 연결한 아군 화면
            var card = fight.View("DEFENSE_ORDER").RuntimeCard.SourceData; // 손패의 방어 지령 원본
            var attack = CreateAttack(fight.Enemies[0], ally, BattleDamageType.Physical, 100); // 방어력 두 점 차이가 드러나는 피해 예고
            yield return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, targetView, fight.Runner, fight.Deck, fight.Points); // 실제 카드와 동료 화면 클릭
            Assert.AreEqual(ally.BasePhysicalDefense + 2, ally.PhysicalDefense); // 지정한 동료의 물리 방어력 두 점 증가
            Assert.AreEqual(owner.BasePhysicalDefense, owner.PhysicalDefense); // 사용자의 물리 방어력 보존 확인
            Assert.AreEqual(other.BasePhysicalDefense, other.PhysicalDefense); // 다른 동료의 물리 방어력 보존 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 적 턴 시작
            Assert.AreEqual(97, attack.PreviewDamage().FinalDamage); // 첫 적 턴에 강화된 물리 방어 적용 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 적 턴 시작
            Assert.AreEqual(97, attack.PreviewDamage().FinalDamage); // 두 번째 적 턴에도 강화된 물리 방어 적용 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 세 번째 적 턴 시작
            Assert.AreEqual(99, attack.PreviewDamage().FinalDamage); // 세 번째 적 턴의 원래 물리 방어 확인
        } // 단일 아군 대상 준비 종료
    } // 동료 방어 검사 종료

    [UnityTest] // 굳건한 자세의 비용 경계 검사
    public IEnumerator SturdyStanceRequiresAndSpendsBothActionPoints() // 표시 비용과 실제 추가 행동력 확인
    { // 굳건한 자세 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 환경 진입
        yield return CheckSturdyStance(); // 행동력 한 점과 두 점 경계 비교
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 굳건한 자세 검사 종료

    private static IEnumerator CheckSturdyStance() // 행동력 부족 차단과 정확한 차감
    { // 행동력 경계 검사 시작
        using (var fight = new Fight("STURDY_STANCE")) // 지급되는 굳건한 자세 카드 준비
        { // 행동력 경계 조건 준비 시작
            var owner = fight.Allies[0]; // 방어 상승 대상인 소유자
            var view = fight.View("STURDY_STANCE"); // 클릭할 굳건한 자세 손패 화면
            var card = view.RuntimeCard; // 사용 전 손패 인스턴스 보관
            Assert.AreEqual(1, card.SourceData.ApCost); // 원본의 기본 행동력 한 점 확인
            Assert.AreEqual(2, card.ApCost); // 필수 추가 비용을 합한 실사용 행동력 두 점 확인
            Assert.IsTrue(fight.Points.Spend(fight.Points.CurrentActionPoints - 1)); // 행동력이 한 점만 남는 상황 준비
            view.OnPointerClick(null); // 비용이 부족한 카드 클릭 시도
            Assert.IsNull(fight.Controller.SelectedCard); // 선택 상태 진입 차단 확인
            Assert.IsFalse(fight.Runner.IsBusy); // 실행 연출 시작 차단 확인
            Assert.AreEqual(1, fight.Points.CurrentActionPoints); // 사용 실패 시 남은 행동력 보존 확인
            CollectionAssert.Contains(fight.Deck.Hand, card); // 사용 실패 시 카드 손패 보존 확인
            Assert.AreEqual(owner.BasePhysicalDefense, owner.PhysicalDefense); // 사용 실패 시 물리 방어 변화 없음 확인
            Assert.AreEqual(1, fight.Points.Gain(1)); // 정확히 두 점의 행동력 확보
            yield return fight.Use("STURDY_STANCE"); // 정확한 비용으로 굳건한 자세 사용
            Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 기본과 추가 비용의 실제 차감 확인
            Assert.AreEqual(owner.BasePhysicalDefense + 3, owner.PhysicalDefense); // 소유자의 물리 방어력 세 점 증가
            Assert.AreEqual(2, owner.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseUp).RemainingTurns); // 물리 방어력 증가의 지속 턴 확인
        } // 행동력 경계 조건 준비 종료
    } // 행동력 경계 검사 종료

    [UnityTest] // 방패 강타의 명중과 확률 경계 검사
    public IEnumerator ShieldBashDealsPhysicalDamageAndStaggersOnlyOnSuccessfulRollAfterHit() // 빗나감과 확률 성공 및 실패 비교
    { // 방패 강타 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 환경 진입
        yield return CheckShieldBash(); // 세 방패 강타의 결과 비교
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 방패 강타 검사 종료

    private static IEnumerator CheckShieldBash() // 명중 후 이십 퍼센트 행동 취소
    { // 행동 취소 검사 시작
        using (var fight = new Fight("SHIELD_BASH", "SHIELD_BASH", "SHIELD_BASH")) // 세 조건을 비교할 같은 원본 카드 준비
        { // 재현 가능한 타격 조건 준비 시작
            var owner = fight.Allies[0]; // 방패 강타 사용 아군
            var failed = fight.Enemies[0]; // 정확히 이십으로 확률에 실패할 적
            var staggered = fight.Enemies[1]; // 십구로 행동 취소에 걸릴 적
            var missed = fight.Enemies[2]; // 공격자가 빗맞힐 적
            BattleDamageResult hit = default; // 성공한 물리 타격의 결과 보관
            staggered.DamageResolved += (source, target, damage) => hit = damage; // 실제 타격 결과 기록
            int seed = SeedFor(20); // 이십 퍼센트의 첫 실패 값 준비
            fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 카드 충돌 직전 난수 복원
            yield return fight.Use("SHIELD_BASH", 0); // 확률 경계의 첫 실패 공격
            Assert.AreEqual(195, failed.CurrentHealth); // 행동 취소 실패와 별개인 다섯 피해 확인
            Assert.IsNull(failed.GetStatusEffect(BattleStatusEffectType.Stagger)); // 정확히 이십에서 행동 취소 미적용 확인
            seed = SeedFor(19); // 이십 퍼센트의 마지막 성공 값 준비
            yield return fight.Use("SHIELD_BASH", 1); // 확률 경계의 마지막 성공 공격
            Assert.AreEqual(BattleDamageType.Physical, hit.DamageType); // 실제 타격의 물리 유형 확인
            Assert.AreEqual(5, hit.AppliedDamage); // 성공 타격의 물리 피해 다섯 점 확인
            Assert.IsNotNull(staggered.GetStatusEffect(BattleStatusEffectType.Stagger)); // 명중 후 행동 취소 상태 확인
            owner.ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 1, 1); // 다음 강타의 빗나감 강제 조건
            seed = SeedFor(19, true); // 빗나감 굴림 뒤에도 행동 취소가 당첨되는 난수 준비
            yield return fight.Use("SHIELD_BASH", 2); // 성공 난수에도 빗나가는 강타
            Assert.AreEqual(200, missed.CurrentHealth); // 빗나간 대상의 체력 보존 확인
            Assert.IsNull(missed.GetStatusEffect(BattleStatusEffectType.Stagger)); // 빗나간 대상의 행동 취소 미적용 확인
            var attack = CreateAttack(staggered, owner, BattleDamageType.Physical, 10); // 행동 취소 상태의 적 공격 준비
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 적 행동이 진행되는 실제 턴 시작
            Assert.AreEqual(0, attack.Execute().DamageResult.AppliedDamage); // 행동 취소로 피해 없음 확인
            Assert.IsNull(staggered.GetStatusEffect(BattleStatusEffectType.Stagger)); // 예정 행동 한 번에 취소 상태 소모 확인
            Assert.AreEqual(9, attack.PreviewDamage().AppliedDamage); // 취소 소모 뒤의 정상 공격 가능 확인
        } // 재현 가능한 타격 조건 준비 종료
    } // 행동 취소 검사 종료

    [UnityTest] // 방어 전환의 방어도와 드로우 검사
    public IEnumerator GuardSwitchGivesFiveBlockAndDrawsExactlyTheTopCard() // 자신만 방어하고 덱 맨 위 한 장을 뽑는 효과 확인
    { // 방어 전환 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 환경 진입
        yield return CheckGuardSwitch(); // 손패와 덱의 실제 카드 이동 확인
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 방어 전환 검사 종료

    private static IEnumerator CheckGuardSwitch() // 실제 뽑을 더미 두 장의 위치 구분
    { // 방어와 드로우 검사 시작
        using (var fight = new Fight("GUARD_SWITCH", "IRON_WALL", "IRON_WALL")) // 방어 전환과 드로우용 원본 두 장 준비
        { // 드로우 시작 위치 준비 시작
            var owner = fight.Allies[0]; // 방어 전환 카드 소유자
            var used = fight.View("GUARD_SWITCH").RuntimeCard; // 사용 뒤 버릴 원본 인스턴스
            foreach (var filler in fight.Deck.Hand.Where(card => card != used).ToArray()) // 드로우용 카드 두 장 순회
            { // 뽑을 후보 이동 시작
                Assert.IsTrue(fight.Deck.DiscardCard(filler)); // 사용 카드 외의 두 장을 버린 더미로 이동
            } // 뽑을 후보 이동 종료
            var candidates = fight.Deck.PrepareDrawChoices(2); // 두 장을 실제 뽑을 더미의 위부터 조회
            Assert.AreEqual(2, candidates.Count); // 두 드로우 가능 후보 확인
            owner.GainBlock(2); // 방어도 증가량 검사용 기존 방어도
            yield return fight.Use("GUARD_SWITCH"); // 방어도 획득과 드로우가 함께 일어나는 실제 카드 사용
            Assert.AreEqual(7, owner.CurrentBlock); // 기존 방어도에 다섯 점 추가 확인
            Assert.AreEqual(0, fight.Allies[1].CurrentBlock); // 다른 아군 방어도 보존 확인
            CollectionAssert.AreEqual(new[] { candidates[0] }, fight.Deck.Hand); // 사용 카드 대신 맨 위 원본 한 장만 손패에 진입
            CollectionAssert.AreEqual(new[] { candidates[1] }, fight.Deck.DrawPile); // 두 번째 원본이 뽑을 더미에 남는 결과 확인
            CollectionAssert.AreEqual(new[] { used }, fight.Deck.DiscardPile); // 방어 전환 카드만 버린 더미로 이동 확인
        } // 드로우 시작 위치 준비 종료
    } // 방어와 드로우 검사 종료

    [UnityTest] // 저지의 표적과 지속 기간 검사
    public IEnumerator HoldBackWeakensTheSelectedEnemysAttacksForTwoTurns() // 적 한 명의 물리와 마법 공격 약화 확인
    { // 저지 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 환경 진입
        yield return CheckHoldBack(); // 적 공격 실행과 약화 만료 확인
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 저지 검사 종료

    private static IEnumerator CheckHoldBack() // 약화된 적의 턴별 직접 공격 실행
    { // 적 공격 약화 검사 시작
        using (var fight = new Fight("HOLD_BACK")) // 지급되는 저지 카드 준비
        { // 비교할 공격 준비 시작
            var owner = fight.Allies[0]; // 여러 차례 공격을 받을 아군
            var target = fight.Enemies[1]; // 저지가 적용되는 단일 적
            var physical = CreateAttack(target, owner, BattleDamageType.Physical, 10); // 약화 적용 물리 공격
            var magical = CreateAttack(target, owner, BattleDamageType.Magical, 10); // 약화 적용 마법 공격
            var control = CreateAttack(fight.Enemies[0], owner, BattleDamageType.Physical, 10); // 선택하지 않은 적의 대조 공격
            yield return fight.Use("HOLD_BACK", 1); // 저지 카드와 선택한 적 화면 클릭
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 적 턴 시작
            Assert.AreEqual(9, control.PreviewDamage().AppliedDamage); // 다른 적의 피해 보존 확인
            Assert.AreEqual(7, physical.PreviewDamage().AppliedDamage); // 첫 공격의 피해 두 점 경감 예고
            Assert.AreEqual(7, physical.Execute().DamageResult.AppliedDamage); // 첫 물리 공격의 실제 체력 피해 확인
            Assert.AreEqual(7, magical.Execute().DamageResult.AppliedDamage); // 첫 공격 뒤에도 이어지는 마법 공격 약화 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 적 턴 시작
            Assert.AreEqual(7, physical.Execute().DamageResult.AppliedDamage); // 두 번째 적 턴의 물리 피해 경감 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 두 적 턴 뒤 공격 약화 제거 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 세 번째 적 턴 시작
            Assert.AreEqual(9, physical.PreviewDamage().AppliedDamage); // 세 번째 적 턴에 표적 적의 일반 피해 복원
        } // 비교할 공격 준비 종료
    } // 적 공격 약화 검사 종료

    [UnityTest] // 저항 태세의 적용 순서와 기간 검사
    public IEnumerator ResistanceStanceReducesOnlyDirectMagicAfterResistanceForTwoEnemyTurns() // 일반 저항과 방어도 사이의 비율 경감 확인
    { // 저항 태세 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 환경 진입
        yield return CheckResistanceStance(); // 피해 유형과 두 적 턴의 경감 검증
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 저항 태세 검사 종료

    private static IEnumerator CheckResistanceStance() // 마법 저항 뒤 십오 퍼센트 경감 검증
    { // 마법 피해 경계 검사 시작
        using (var fight = new Fight("RESISTANCE_STANCE")) // 지급되는 저항 태세 카드 준비
        { // 서로 다른 피해 경로 준비 시작
            var owner = fight.Allies[0]; // 마법 직접 공격의 저항 태세 대상
            var actor = fight.Enemies[0]; // 물리와 마법 대조 타격의 발생자
            owner.ApplyStatusEffect(BattleStatusEffectType.MagicalResistanceUp, 30 - owner.BaseMagicalResistance, 5, 1); // 피해 내림 순서가 구분되는 마법 저항 준비
            var magical = CreateAttack(actor, owner, BattleDamageType.Magical, 28); // 일반 마법 저항 뒤 피해 스물한 점의 직접 공격
            var physical = CreateAttack(actor, owner, BattleDamageType.Physical, 28); // 비율 경감에서 제외할 물리 직접 공격
            yield return fight.Use("RESISTANCE_STANCE"); // 소유자에게 저항 태세 사용
            Assert.IsNull(fight.Allies[1].GetStatusEffect(BattleStatusEffectType.ResistanceStance)); // 다른 아군에게 저항 태세 미적용 확인
            owner.GainBlock(16); // 태세의 경감 뒤 피해 한 점만 남기는 방어도
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 적 턴 시작
            Assert.AreEqual(27, physical.PreviewDamage().FinalDamage); // 물리 직접 공격의 일반 피해 보존 확인
            Assert.AreEqual(21, owner.PreviewDamage(28, BattleDamageType.Magical, actor).FinalDamage); // 적 직접 공격이 아닌 마법 피해의 일반 저항 확인
            var preview = magical.PreviewDamage(); // 일반 저항과 태세 및 방어도가 반영된 직접 공격 예고
            Assert.AreEqual(21, preview.DefenseAdjustedDamage); // 일반 저항의 독립된 내림 계산 확인
            Assert.AreEqual(17, preview.FinalDamage); // 일반 저항 피해 스물한 점의 십오 퍼센트 경감 후 내림
            Assert.AreEqual(1, preview.AppliedDamage); // 경감 이후 방어도가 적용된 체력 피해 확인
            var first = magical.Execute().DamageResult; // 첫 적 턴의 마법 직접 공격 실행
            Assert.AreEqual(16, first.AbsorbedDamage); // 비율 경감 뒤 방어도 열여섯 점 흡수
            Assert.AreEqual(1, first.AppliedDamage); // 첫 적 턴의 실제 체력 피해 한 점 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 적 턴 시작
            Assert.AreEqual(17, magical.Execute().DamageResult.AppliedDamage); // 두 번째 적 턴에도 마법 피해 경감 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.ResistanceStance)); // 두 적 턴 뒤 새로운 태세 제거 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 세 번째 적 턴 시작
            Assert.AreEqual(21, magical.PreviewDamage().AppliedDamage); // 세 번째 적 턴의 일반 마법 저항만 확인
        } // 서로 다른 피해 경로 준비 종료
    } // 마법 피해 경계 검사 종료

    private static BattleEnemyAction CreateAttack(BattleUnitRuntime actor, BattleUnitRuntime target, BattleDamageType damageType, int amount) // 지정 피해 유형과 표적의 실제 적 공격 생성
    { // 적 공격 생성 시작
        return new BattleEnemyAction(actor, target, EnemyActionType.Attack, damageType, amount, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "기본 공격", 1, 1, 1, 0); // 추가 효과 없는 고정 공격 반환
    } // 적 공격 생성 종료

    private static int SeedFor(int percent, bool nextRollMustSucceed = false) // 지정된 첫 확률과 다음 성공 조건의 시드 검색
    { // 난수 시드 검색 시작
        for (int seed = 0; seed < 5000; seed++) // 기존 검사와 같은 재현 시드 범위 순회
        { // 후보 난수 확인 시작
            Random.InitState(seed); // 후보 시드로 난수 초기화
            if (Random.Range(0, 100) == percent && (!nextRollMustSucceed || Random.Range(0, 100) < 20)) // 첫 경계 값과 선택적인 다음 성공 값 확인
            { // 경계 시드 확인 시작
                return seed; // 재현 가능한 시드 반환
            } // 경계 시드 확인 종료
        } // 후보 난수 확인 종료
        Assert.Fail("방패 강타 확률 경계 시드 없음"); // 확률 경계 비교를 할 수 없는 검사 실패
        return 0; // 검사 실패 뒤의 기본 반환값
    } // 난수 시드 검색 종료
} // 방패 전투 검사 종료
