using System; // 실제 저항과 상태 순서 수치 처리
using System.Collections; // 실제 카드 연출과 플레이 모드 전환 대기
using System.Linq; // 실전 유닛과 예정 행동 검색
using System.Reflection; // 테스트 어셈블리에서 실제 적 행동 관리자 조회
using NUnit.Framework; // 시트 효과의 실제 동작 확인
using UnityEngine; // 카드와 적 원본의 실제 사용자 환경
using UnityEngine.TestTools; // 플레이 모드에서 실제 카드 입력 실행
using Object = UnityEngine.Object; // 검사자가 만든 전투 원본 정리
using static WinterfallCommonCardExpansionTests; // 기존 카드 클릭과 소모 검사 재사용
using static WinterfallSwordCombatTests; // 실제 적과 손패 및 카드 행동 관리자 연결

public sealed class WinterfallIceWardAndMagicAmplificationTests // 세 지팡이 카드의 실제 명중과 상태 기간 검사
{ // 지팡이 카드 플레이 회귀 검사 시작
    [UnityTest] // 실제 카드 명중과 원래 생긴 예고 상태 확인
    public IEnumerator IceArrowReordersTheSamePlannedActionsOnAnActualHit() // 명중과 실패 및 다음 적 턴의 속도 경계 검사
    { // 얼음 화살의 플레이 검사 시작
        yield return new EnterPlayMode(); // 카드 입력과 실제 전투 원본이 연결되는 모드 시작
        yield return VerifyIceArrow(); // 속도 감소의 실제 예고와 턴 정산 확인
        yield return new ExitPlayMode(); // 플레이 회귀 검사 모드 종료
    } // 얼음 화살의 플레이 검사 종료

    private static IEnumerator VerifyIceArrow() // 원래 정해진 행동과 상태 효과의 변화 분리
    { // 실제 얼음 화살 입력 준비 시작
        using var fight = new Fight("ICE_ARROW", "ICE_ARROW", "ICE_ARROW"); // 실제 원본 얼음 화살 세 장이 있는 손패
        var actions = ReadEnemyActions(fight); // 카드가 직접 알리는 실제 적 행동 관리자
        JsonUtility.FromJsonOverwrite("{\"minimumActionSpeed\":4,\"maximumActionSpeed\":4}", fight.Enemies[0].EnemySource); // 기존 적의 예고 순서가 모두 같은 속도인 경계
        Assert.AreEqual(fight.Enemies.Length, actions.PrepareActions()); // 실제 파티 대상과 무작위 행동을 한 번 준비
        var originalActions = actions.PlannedActions.ToArray(); // 원래 순서와 바로 그 의도 개체 보관
        var slowed = fight.Enemies[0]; // 처음에 같은 속도라 가장 먼저 서는 적
        var immune = fight.Enemies[1]; // 실제 약화 면역을 가진 두 번째 적
        var evasive = fight.Enemies[2]; // 회피로 실제 명중하지 않는 비교 적
        var originalAction = originalActions[0]; // 느려지기 전 바로 그 팀의 첫 의도
        var originalTarget = originalAction.IntendedTarget; // 얼음 화살이 바꾸지 않을 원래 계획 대상
        int originalPattern = originalAction.PatternIndex; // 원래 계획된 적의 패턴 순번 보관
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 정상 면역 적용 경로로 중첩 차단 준비
        evasive.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 얼음 화살의 실제 회피가 발생할 정수 상태
        Assert.AreSame(slowed, originalAction.Actor); // 같은 속도에서는 정확한 초기 배치 순 확인
        int health = slowed.CurrentHealth; // 실제 마법 피해 원본 비교의 시작 체력
        Assert.AreEqual(1, fight.View("ICE_ARROW").RuntimeCard.ApCost); // 실제 원본에 기록된 얼음 화살 행동력 비용
        yield return fight.Use("ICE_ARROW", 0); // 첫 적에 실제 카드와 대상 클릭으로 명중
        Assert.AreEqual(health - 6, slowed.CurrentHealth); // 첫 표적의 실제 마법 체력 피해 여섯 점 확인
        Assert.AreEqual(1, slowed.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown).RemainingTurns); // 다음 적 턴의 실제 속도 약화 상태 등록 확인
        Assert.AreEqual(3, originalAction.ActionSpeed); // 현재 준비되어 있던 적 예고 속도 네 점에서 한 점 차감
        Assert.AreSame(originalAction, actions.PlannedActions.Last()); // 속도 감소 시 바로 그 계획 의도가 뒤로 밀리는지 확인
        Assert.AreEqual(actions.PlannedActions.Count, originalAction.ActionOrder); // 시각 순서에도 느려진 마지막 적 순번 적용 확인
        Assert.AreSame(originalTarget, originalAction.IntendedTarget); // 다른 행동이나 대상으로 재추첨되지 않는지 확인
        Assert.AreEqual(originalPattern, originalAction.PatternIndex); // 이미 준비한 적 패턴이 바뀌지 않는지 확인
        CollectionAssert.AreEquivalent(originalActions, actions.PlannedActions); // 기존 다른 예정 공격까지 재생성하지 않았는지 확인
        yield return fight.Use("ICE_ARROW", 1); // 약화 면역으로 피해만 들어가는 실제 카드 선택
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 명중했어도 원래 약화 면역에서 상태 적용 제외
        yield return fight.Use("ICE_ARROW", 2); // 대상의 실제 완전 회피가 있는 상황에서 공격
        Assert.IsNull(evasive.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 실제로 명중하지 않은 적에게 속도 약화 없음 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 턴 끝에서 얼음 효과가 만료되지 않는지 확인할 전환
        Assert.AreEqual(3, originalAction.ActionSpeed); // 실제 이어진 적 턴의 행동 순서 감소 유지
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 턴 종료와 다음 아군 턴 진입
        Assert.IsNull(slowed.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 적 턴 하나 이후 속도 상태 만료 확인
        Assert.AreEqual(4, originalAction.ActionSpeed); // 동일 원본 속도가 상태 종료 뒤 원래 값으로 복구
    } // 실제 얼음 화살 입력 준비 종료

    [UnityTest] // 마력 보호막 손패 입력과 실제 적 턴 저항 유지 검사
    public IEnumerator ArcaneWardAddsFlatResistanceToTheCasterUntilTheEnemyTurnEnds() // 자신 지정 및 기존 마법 방어와 독립 합산 확인
    { // 마력 보호막의 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 원본과 씬 입력을 준비하는 모드 진입
        yield return VerifyArcaneWard(); // 사용자 카드 클릭과 적 턴 경계 확인
        yield return new ExitPlayMode(); // 다음 테스트 전에 플레이 모드 정리
    } // 마력 보호막의 플레이 검사 종료

    private static IEnumerator VerifyArcaneWard() // 실제 타격에서 플랫 저항이 먼저 합산리는지 확인
    { // 마력 보호막 수치 및 기간 대조 시작
        using var fight = new Fight("ARCANE_WARD"); // 시전할 시트 원본으로 실제 전투 생성
        var owner = fight.Allies[0]; // 손패 소유자와 회복 대상이 같은 아군
        var ownerView = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).Single(view => view.RuntimeUnit == owner); // 실제 시전자에 연결된 전투 화면 뷰
        int baseline = owner.MagicalResistance; // 원래 캐릭터의 플랫 마법 저항 기준
        int physical = owner.PhysicalDefense; // 마력 보호막이 변경하지 않을 물리 방어 기준
        owner.ApplyStatusEffect(BattleStatusEffectType.MagicalResistanceUp, 4, 3, 1); // 일반 마법 저항 상태와 별도의 신규 발동 대조
        owner.ApplyStatusEffect(BattleStatusEffectType.MagicalResistanceDown, baseline + 6, 3, 1); // 보정 전 마법 저항 합을 영 아래로 내리는 실제 약화
        Assert.AreEqual(0, owner.MagicalResistance); // 약화가 적용된 영 하한의 평소 마법 저항 확인
        var card = fight.View("ARCANE_WARD").RuntimeCard; // 마력 보호막 카드와 원래 소유자 조회
        Assert.AreEqual(1, card.ApCost); // 원본에 설정된 한 행동력 비용 확인
        yield return Play(card.SourceData, fight.Hand, ownerView, fight.Runner, fight.Deck, fight.Points); // 실제 단일 생존 아군 경로로 시전자 본인 지정
        Assert.AreEqual(4, owner.MagicalResistance); // 다른 증가 감소 효과 합산 뒤 전체를 한 번만 영 하한으로 제한 확인
        Assert.AreEqual(physical, owner.PhysicalDefense); // 마법 저항 증가가 물리 방어 값에 섞이지 않는지 확인
        Assert.AreEqual(6, owner.GetStatusEffect(BattleStatusEffectType.ArcaneWard).EffectiveValue); // 별도 마력 보호막 상태의 원본 수치 여섯 점 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 현재 아군 턴만 마치고 실제 다음 적 턴으로 전환
        Assert.AreEqual(4, owner.MagicalResistance); // 마력 보호막이 다음 적 턴에 여전히 살아 있는지 확인
        Assert.AreEqual(24, owner.PreviewDamage(25, BattleDamageType.Magical, fight.Enemies[0]).FinalDamage); // 실제 플랫 마법 저항 수치가 예고 공식에 포함되는지 확인
        Assert.AreEqual(24, owner.TakeDamage(25, BattleDamageType.Magical, fight.Enemies[0]).FinalDamage); // 실제 적용 타격이 방패의 플랫 저항을 그대로 사용 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 마력 보호막의 유일한 실제 적 턴 종료
        Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.ArcaneWard)); // 다음 아군 턴부터 별도 마력 보호막 만료 확인
        Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.MagicalResistanceUp)); // 기존 장기 마법 저항 강화까지 지우지 않는지 확인
    } // 마력 보호막 수치 및 기간 대조 종료

    [UnityTest] // 마법 전용 증가와 물리 비교 및 실제 아군 턴 검사
    public IEnumerator MagicAmplificationAddsFiveToMagicalDirectAndChainDamageOnly() // 마법 두 종류와 물리 타격 그리고 전체 생존 아군 적용 확인
    { // 마력 증폭의 플레이 검사 시작
        yield return new EnterPlayMode(); // 원본 공격 카드와 손패를 연결하는 실제 플레이지점 진입
        yield return VerifyMagicAmplification(); // 태양 고정 연쇄와 일반 직접 마법 피해의 공통 증가량 확인
        yield return new ExitPlayMode(); // 모든 실제 아군 턴 검사 뒤 편집 모드 복귀
    } // 마력 증폭의 플레이 검사 종료

    private static IEnumerator VerifyMagicAmplification() // 아군 전체 강화와 시전 턴 포함 기간 검증
    { // 마력 증폭의 출처 카드 원본 확보 시작
        var offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers; // 실제 카드 보물 자료 조회
        CardData LoadCard(string id) => offers.Single(item => item.OfferId == "REWARD_SHEET_" + id).Card; // 원본 보물 ID와 실제 카드 연결 반환
        var chain = Object.Instantiate(Resources.Load<CardData>("StarterCards/SunChain")); // 원본을 편집하지 않는 실제 연쇄 경로 테스트용 사본
        JsonUtility.FromJsonOverwrite("{\"damageType\":2}", chain); // 태양의 기존 연쇄가 마법으로도 계산되는 경계 지정
        try // 기존 태양 원본과 플레이어 저장을 보존할 임시 검사 보호
        { // 직접 피해 및 연쇄 실제 공격 시작
            using var fight = new Fight(new[] { LoadCard("MAGIC_AMPLIFICATION"), LoadCard("ICE_ARROW"), LoadCard("THRUST"), chain }); // 실제 시트 보물 원본과 독립 태양 카드로 실제 전투 시작
            var owner = fight.Allies[0]; // 이번 턴 마법과 일반 공격의 실제 시전자
            var other = fight.Allies[1]; // 생존한 두 번째 아군 전체 상태 적용 대조
            var fallen = fight.Allies[2]; // 전투불능이라 상태를 받지 않는 세 번째 아군
            fallen.ApplyPersistentHealth(0); // 생존 아군 전체와 전투불능 경로 구분
            owner.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 4, 1); // 기존 보통 공격력 증가와 마법 전용 증가 합산 확인
            Assert.AreEqual(2, fight.View("MAGIC_AMPLIFICATION").RuntimeCard.ApCost); // 실제 마력 증폭 카드의 행동력 두 점 확인
            yield return fight.Use("MAGIC_AMPLIFICATION"); // 실제 손패를 사용해 생존 아군 전체에 상태 부여
            Assert.AreEqual(5, owner.GetStatusEffect(BattleStatusEffectType.MagicAttackPowerUp).EffectiveValue); // 시전자의 실제 마법 공격력 다섯 점 증가
            Assert.AreEqual(5, other.GetStatusEffect(BattleStatusEffectType.MagicAttackPowerUp).EffectiveValue); // 살아 있는 다른 아군의 독립 마법 공격력 상태 확인
            Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.MagicAttackPowerUp)); // 전투불능 아군에게 기본 상태를 적용하지 않음 확인
            int raw = 0; // 실제 인게임 피해 기록에서 현재 카드의 최종 원본 수치 보관
            fight.Enemies[0].DamageTaken += (_, result) => raw = result.RawDamage; // 같은 한 적 대상의 실제 원본 피해 이벤트 구독
            yield return fight.Use("ICE_ARROW", 0); // 이번 아군 턴에 마법 피해 원본 카드 사용
            Assert.AreEqual(13, raw); // 마법 여섯과 일반 공격력 둘과 마법 공격력 다섯 합산 확인
            yield return fight.Use("THRUST", 0); // 같은 강화 상태에서 실제 물리 타격 카드 사용
            Assert.AreEqual(LoadCard("THRUST").EffectValue + 2, raw); // 일반 공격력만 물리 카드에 반영되는지 확인
            int chainRaw = 0; // 같은 상태의 별도 연쇄 피해에서 실제 확정된 원본 수치 기록
            foreach (var enemy in fight.Enemies.Skip(1)) // 실제 무작위 연쇄 대상 후보를 모두 감시
            { // 연쇄 후보의 실제 피해 기록 연결 시작
                enemy.DamageTaken += (_, result) => chainRaw = result.RawDamage; // 후속 마법 공격의 실제 적 방어 전 수치 추적
            } // 연쇄 후보의 실제 피해 기록 연결 종료
            yield return Play(chain, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 원래 태양 연쇄 입력으로 첫 적과 별도 다른 적 공격
            Assert.AreEqual(chain.EffectValue + 7, raw); // 같은 마법 일반 공격력 증가가 직접 태양 타격에 정확히 한 번 적용 확인
            Assert.AreEqual(chain.SecondaryEffectValue + 7, chainRaw); // 별도 태양 연쇄에도 같은 마법 전용 증가가 정확히 한 번 적용 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 증폭을 시전한 첫 아군 턴 및 이어지는 적 턴 한 번 종료
            Assert.AreEqual(1, owner.GetStatusEffect(BattleStatusEffectType.MagicAttackPowerUp).RemainingTurns); // 시전한 차례를 포함한 남은 아군 턴이 한 번인지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 아군 턴과 적 턴 정상 경계 진행
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.MagicAttackPowerUp)); // 세 번째 아군 턴까지 마법 공격력이 남지 않는지 확인
            Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 독립적인 일반 공격력 상태는 계속 유지 확인
        } // 직접 피해 및 연쇄 실제 공격 종료
        finally // 임시 연쇄 원본의 실제 정리 보장
        { // 연쇄 카드 정리 시작
            Object.Destroy(chain); // 원래 카드나 사용자 원화를 변경하지 않은 임시 적격 사본 제거
        } // 연쇄 카드 정리 종료
    } // 마력 증폭의 출처 카드 원본 확보 종료

    private static BattleEnemyActionRuntime ReadEnemyActions(Fight fight) // 카드 입력과 실제 적 예고가 공유하는 관리자 조회
    { // 적 행동 관리자 연결 확인 시작
        var field = typeof(BattleCardActionController).GetField("enemyActions", BindingFlags.Instance | BindingFlags.NonPublic); // 적 의도 데이터를 유지하는 실제 카드 필드 조회
        Assert.IsNotNull(field); // 전투 카드에 같은 적 행동 관리자가 있는지 확인
        return (BattleEnemyActionRuntime)field.GetValue(fight.Controller); // 실제 전투에서 별개로 만들지 않은 예정 행동 관리자 반환
    } // 적 행동 관리자 연결 확인 종료
} // 지팡이 카드 플레이 회귀 검사 종료
