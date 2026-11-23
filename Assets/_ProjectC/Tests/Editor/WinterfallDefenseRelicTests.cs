using System.Collections; // 플레이 모드 단계별 검사 사용
using System.Linq; // 실제 손패 카드와 비용 배지 검색 사용
using NUnit.Framework; // 테스트 결과 확인 사용
using TMPro; // 실제 손패의 행동력 배지 확인 사용
using UnityEngine; // 검사 카드 원본과 유물 사용
using UnityEngine.TestTools; // 검사 플레이 모드 전환 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 검사 구성 사용

public sealed class WinterfallDefenseRelicTests // 갈라진 방패끈과 빛바랜 메달 조각 검사
{ // 방어 카드 유물 검사 시작
    [UnityTest] // 실제 손패 비용과 첫 방어 카드 검사 지정
    public IEnumerator CrackedShieldStrapDiscountsOnePaidBaseDefenseInBattle() // 고정 기본 비용이 남은 방어 카드의 전투당 단일 할인 확인
    { // 방패끈 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 플레이 모드 진입
        yield return CheckCrackedShieldStrap(); // 플레이 모드에서 별도 비용 검사 실행
        yield return new ExitPlayMode(); // 전투 화면 플레이 모드 종료
    } // 방패끈 플레이 검사 종료

    [UnityTest] // 실제 검과 방패 카드의 다음 사용 연계 검사 지정
    public IEnumerator FadedMedalFragmentArmsFromSwordsAndDoesNotStackAcrossTurns() // 다음 턴까지 유지되는 단일 방패 보너스 확인
    { // 메달 조각 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 플레이 모드 진입
        yield return CheckFadedMedalFragment(); // 플레이 모드에서 별도 카드 연계 검사 실행
        yield return new ExitPlayMode(); // 전투 화면 플레이 모드 종료
    } // 메달 조각 플레이 검사 종료

    private static IEnumerator CheckCrackedShieldStrap() // 원본 유물 보유 후 실제 손패의 일반과 예외 비용 확인
    { // 첫 방어 카드 비용 검사 시작
        Hold(RelicEffectType.FirstDefensiveCardDiscount, RelicTriggerType.CardUsed, 1); // 게임에 등록된 첫 유료 방어 카드 할인 유물 확인
        CardData defense = MakeCard("Defense", CardType.Shield, CardEffectType.GainBlock, CardTargetType.Self, 1, extraAp: 2); // 기본 행동력 한 점과 의무 행동력 두 점의 방패
        CardData freeDefense = MakeCard("FreeDefense", CardType.Shield, CardEffectType.GainBlock, CardTargetType.Self, 0); // 원래 행동력이 필요 없는 방패
        CardData coveredDefense = MakeCard("CoveredDefense", CardType.Shield, CardEffectType.GainBlock, CardTargetType.Self, 1); // 다른 할인으로 기본 비용을 없앨 방패
        CardData buff = MakeCard("PositiveShield", CardType.Shield, CardEffectType.ApplyStatusEffect, CardTargetType.Self, 2, status: BattleStatusEffectType.PhysicalDefenseUp); // 직접 방어도 없이 방어 상태를 주는 방패
        CardData attack = MakeCard("OnlyAttack", CardType.Sword, CardEffectType.Damage, CardTargetType.SingleEnemy, 2); // 자체 보호가 없는 순수 검 공격
        CardData tankHit = MakeCard("BlockAttack", CardType.Sword, CardEffectType.Damage, CardTargetType.SingleEnemy, 2, followUp: CardFollowUpEffect.GainOwnerBlock); // 공격에 명시적 자기 방어도가 이어지는 검
        CardData finisher = MakeCard("VariableCost", CardType.Sword, CardEffectType.Damage, CardTargetType.SingleEnemy, 2, followUp: CardFollowUpEffect.GainOwnerBlock, swordRule: SwordCardRule.Finisher); // 명시 방어를 넣어도 제외할 가변 행동력 카드
        var cards = new[] { defense, freeDefense, coveredDefense, buff, attack, tankHit, finisher }; // 비용 비교에 쓰는 독립 검사 카드 원본
        try // 검사 원본 해제 보장
        { // 실제 손패 비용 검사 시작
            using var fight = new Fight(cards); // 같은 초기 손패로 모든 보호 규칙 비교
            AssertBadge(fight, defense, "2"); // 할인으로 기본 한 점만 줄고 의무 두 점은 유지되는 표시
            AssertBadge(fight, buff, "1"); // 양의 방패 상태 카드의 비용 할인
            AssertBadge(fight, tankHit, "1"); // 명시적 방어도 후속 공격의 비용 할인
            AssertBadge(fight, attack, "2"); // 순수 공격의 기본 비용 보존
            AssertBadge(fight, finisher, "X"); // 가변 비용 표기 보존
            Assert.AreEqual(2, View(fight, finisher).RuntimeCard.ApCost); // 가변 카드의 별도 수치에도 할인 없음 확인
            View(fight, tankHit).OnPointerClick(null); // 합법적인 대상이 필요한 카드 먼저 선택
            View(fight, tankHit).OnPointerClick(null); // 실행 전 입력을 다시 눌러 취소
            Assert.IsNull(fight.Controller.SelectedCard); // 취소한 카드가 미결 상태에서 빠졌는지 확인
            View(fight, coveredDefense).RuntimeCard.ApplyTemporaryApDiscount(1); // 첫 방패보다 앞서 기존 할인으로 기본 비용 전액 처리
            fight.Hand.RefreshCardAvailability(); // 손패의 실제 할인 숫자와 사용 가능 여부 갱신
            yield return Play(fight, freeDefense); // 기본 무료 방어 카드를 정상 사용
            yield return Play(fight, coveredDefense); // 기존 할인으로 무료인 방어 카드를 정상 사용
            yield return Play(fight, attack, fight.EnemyViews[0]); // 유료 순수 검 공격을 정상 사용
            AssertBadge(fight, defense, "2"); // 취소와 무료 방어 및 순수 유료 공격 이후 할인 보존
            int before = fight.Points.CurrentActionPoints; // 의무 비용을 가진 방어 카드 사용 전 잔액 기록
            yield return Play(fight, defense); // 남은 기본 한 점이 할인되는 실제 방어 카드 사용
            Assert.AreEqual(before - 2, fight.Points.CurrentActionPoints); // 의무 행동력 두 점 전액 결제 확인
            AssertBadge(fight, buff, "2"); // 최초 유료 방어 완료 뒤 같은 손패의 비용 복원
            AssertBadge(fight, tankHit, "2"); // 다른 방어 후속 카드의 비용도 복원
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 아군 턴 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴으로 진행
            AssertBadge(fight, buff, "2"); // 턴을 넘겨도 같은 전투의 할인 소모 유지
        } // 실제 손패 비용 검사 종료
        finally // 독립 원본 정리 시작
        { // 테스트 자료 삭제 시작
            foreach (CardData card in cards) // 검사 카드 원본 순회
            { // 카드별 정리 시작
                Object.Destroy(card); // 검사 원본 제거
            } // 카드별 정리 종료
        } // 독립 원본 정리 종료
    } // 첫 방어 카드 비용 검사 종료

    private static IEnumerator CheckFadedMedalFragment() // 실제 검 두 장과 방패 세 장으로 비중첩 예약 확인
    { // 메달 조각 연계 검사 시작
        Hold(RelicEffectType.SwordThenShieldBlock, RelicTriggerType.AfterCardResolved, 2); // 기사 유물의 실제 두 점 효과 확인
        using (var fight = new Fight("CRITICAL_STRIKE", "CRITICAL_STRIKE", "RETRIBUTION_BULWARK", "RETRIBUTION_BULWARK", "RETRIBUTION_BULWARK")) // 초기 손패와 다음 턴 재드로우 카드 구성
        { // 실제 전투 카드 연계 시작
            BattleUnitRuntime owner = fight.Allies[0]; // 실제 검 및 방패 카드 소유자 조회
            yield return fight.Use("RETRIBUTION_BULWARK"); // 자체 방어도 없는 첫 방패를 검보다 먼저 사용
            Assert.AreEqual(0, owner.CurrentBlock); // 검 예약 전의 방패 보너스 없음 확인
            yield return fight.Use("CRITICAL_STRIKE", 0); // 첫 번째 검 사용으로 다음 방패 예약
            fight.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 두 번째 검이 빗나갈 대상 준비
            yield return fight.Use("CRITICAL_STRIKE", 1); // 빗나가도 정상 정산하는 두 번째 검 사용
            Assert.AreEqual(0, owner.CurrentBlock); // 검 사용에서 즉시 어울리는 방어도가 없는지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 검을 사용한 아군 턴 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 남은 예약을 다음 아군 턴으로 넘기기
            yield return fight.Use("RETRIBUTION_BULWARK"); // 방어도가 없는 아군 전체 방패의 실제 사용
            Assert.AreEqual(2, owner.CurrentBlock); // 두 검의 예약이 한 번에 두 점으로 지급됨 확인
            Assert.AreEqual(0, fight.Allies[1].CurrentBlock); // 전체 효과라도 사용자만 받는 방어도 확인
            yield return fight.Use("RETRIBUTION_BULWARK"); // 새 검 없이 그 다음 방패 사용
            Assert.AreEqual(2, owner.CurrentBlock); // 이미 소비한 한 번의 예약을 다시 지급하지 않음 확인
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 빗나간 단독 검이 예약하는지 확인할 대상 준비
            yield return fight.Use("CRITICAL_STRIKE", 2); // 방패 소모 뒤 적에게 빗나간 새 검으로 다시 예약
            yield return fight.Use("RETRIBUTION_BULWARK"); // 새로 예약한 보호 방패 사용
            Assert.AreEqual(4, owner.CurrentBlock); // 새 검마다 다음 방패 한 번의 반복 가능 확인
        } // 실제 전투 카드 연계 종료
    } // 메달 조각 연계 검사 종료

    private static void Hold(RelicEffectType effect, RelicTriggerType trigger, int value) // 테스트의 실제 보유 유물 준비
    { // 원본 유물 조회 시작
        RelicData relic = Resources.LoadAll<RelicData>("Relics").Single(item => item.EffectType == effect); // 실제 카탈로그에 등록된 효과의 유물 원본 조회
        Assert.AreEqual(trigger, relic.TriggerType); // 문서대로 카드 사용 또는 정산 시점 확인
        Assert.AreEqual(value, relic.EffectValue); // 실제 유물 원본의 고정 효과 수치 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 현재 회차 보유 유물로 원본 지급
    } // 원본 유물 조회 종료

    private static CardData MakeCard(string name, CardType type, CardEffectType effect, CardTargetType target, int ap, int extraAp = 0, BattleStatusEffectType status = BattleStatusEffectType.None, CardFollowUpEffect followUp = CardFollowUpEffect.None, SwordCardRule swordRule = SwordCardRule.None) // 실제 시전에 사용할 비용 경계 카드 원본 구성
    { // 검사 카드 준비 시작
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 새 검사 카드 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"TEST_{name}\",\"displayName\":\"{name}\",\"cardType\":{(int)type},\"effectType\":{(int)effect},\"targetType\":{(int)target},\"apCost\":{ap},\"additionalActionPointCost\":{extraAp},\"effectValue\":6,\"secondaryEffectValue\":3,\"statusEffectType\":{(int)status},\"followUpEffect\":{(int)followUp},\"swordRule\":{(int)swordRule}}}", card); // 카드별 고정 비용과 주효과 및 후속 효과 지정
        return card; // 게임의 실제 카드 판정을 실행할 원본 반환
    } // 검사 카드 준비 종료

    private static IEnumerator Play(Fight fight, CardData card, BattleUnitView target = null) // 손패 입력과 행동력 및 카드 이동 검증 실행
    { // 실제 카드 사용 연결 시작
        return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, target, fight.Runner, fight.Deck, fight.Points); // 기존 실제 카드 사용 검증으로 연결
    } // 실제 카드 사용 연결 종료

    private static BattleCardView View(Fight fight, CardData source) // 실제 손패에 남은 해당 원본 카드 조회
    { // 사용 가능한 카드 검색 시작
        return fight.Hand.GetComponentsInChildren<BattleCardView>().Single(view => view.RuntimeCard?.SourceData == source && fight.Deck.Hand.Contains(view.RuntimeCard)); // 버린 더미 카드와 파기 대기 화면을 제외한 실제 손패 반환
    } // 사용 가능한 카드 검색 종료

    private static void AssertBadge(Fight fight, CardData card, string expected) // 비용 계산과 실제 지급 배지의 일치 확인
    { // 행동력 배지 검사 시작
        BattleCardView view = View(fight, card); // 실제 손패 카드 화면 조회
        Assert.AreEqual(expected, view.RuntimeCard.CostLabel); // 카드 인스턴스의 현재 비용 표시 확인
        Assert.AreEqual(expected, view.GetComponentsInChildren<TMP_Text>().Single(text => text.name == "CostText").text); // 실제 손패에서 표시하는 비용 배지 확인
    } // 행동력 배지 검사 종료
} // 방어 카드 유물 검사 종료
