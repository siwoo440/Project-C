using System.Collections; // 실제 카드 연출과 모드 전환 대기
using System.Linq; // 실제 아군 클릭 대상 조회
using NUnit.Framework; // 치유와 피해 적용 결과 비교
using UnityEngine; // 유물 원본과 테스트 카드 작성
using UnityEngine.TestTools; // 에디터와 플레이 모드 경계 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 입력 환경 사용

public sealed class WinterfallConfessionAndOpeningDamageRelicTests // 고백패 및 첫 공격 유물 세 종류의 실제 입력 검사
{ // 유물 회귀 검사 시작
    [UnityTest] // 치유 시점 정신력 경계와 수신 보정 검사
    public IEnumerator ConfessionPlaqueUsesCastersMentalAndPreservesReceivingHealingModifiers() // 직접 회복의 가산과 기존 수신 회복 유지 확인
    { // 치유 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 상태 초기화 완료 대기
        yield return VerifyPlaque(); // 회복 카드와 지속 효과 실행
        yield return new ExitPlayMode(); // 전투 환경 정리와 에디터 복귀
    } // 치유 유물 검사 종료

    private static IEnumerator VerifyPlaque() // 실제 시전자와 대상의 서로 다른 정신력 경계 검사
    { // 치유 전투 검사 시작
        Acquire("SmallConfessionPlaque"); // 작은 고백패 원본 획득
        Acquire("WhiteRoseCorsage"); // 기존 높은 정신력 회복 유물 획득
        using var fight = new Fight("FIRST_AID", "FIRST_AID", "FIRST_AID"); // 동일한 실제 카드 세 장으로 경계값 비교
        var caster = fight.Allies[0]; // 카드와 직접 회복의 원래 시전자
        var recipient = fight.Allies[1]; // 서로 다른 정신력을 대조할 회복 대상
        var view = fight.Hand.GetComponentInParent<Canvas>().GetComponentsInChildren<BattleUnitView>().Single(unit => unit.RuntimeUnit == recipient); // 회복 대상의 실제 선택 화면
        recipient.TakeDamage(25, BattleDamageType.None); // 여러 회복과 재생을 비교할 체력 손실 준비
        caster.ApplyPersistentMental(49); // 고백패만 조건을 만족하는 시전자 정신력 설정
        recipient.ApplyPersistentMental(65); // 시전자와 반대인 수신자 정신력 설정
        yield return Heal(fight, view, 7); // 기본 다섯에 낮은 정신력의 고백패 두 점 가산
        caster.ApplyPersistentMental(51); // 흰 장미만 조건을 만족하는 시전자 정신력 설정
        recipient.ApplyPersistentMental(40); // 낮은 수신자 정신력으로 시전자 조건 검증
        yield return Heal(fight, view, 6); // 기본 다섯에 높은 정신력의 흰 장미 한 점 가산
        caster.ApplyPersistentMental(50); // 두 유물을 동시에 만족하는 정신력 경계 설정
        recipient.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 50, 2, 1); // 합산 뒤 적용될 수신 회복 절반 설정
        yield return Heal(fight, view, 4); // 기본 다섯과 두 보너스를 합친 여덟에 수신 감소 확인
        recipient.RemoveStatusEffect(BattleStatusEffectType.HealingReceivedDown); // 독립 재생 비교 전 수신 감소 해제
        recipient.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 4, 1, 1); // 직접 카드와 별도의 실제 재생 효과 부여
        int before = recipient.CurrentHealth; // 재생 발동 직전 실제 체력 기록
        new BattleStatusEffectProcessor().ProcessPhaseStart(recipient, fight.Turns.CurrentRound); // 기존 재생 상태 처리기 실행
        Assert.AreEqual(4, recipient.CurrentHealth - before); // 직접 카드 외의 재생은 원래 수치 유지 확인
    } // 치유 전투 검사 종료

    [UnityTest] // 검은 가루병의 실패 입력과 실제 첫 유효 명중 검사
    public IEnumerator BlackDustUsesFirstActiveDebuffHitEvenWhenBlockAbsorbsIt() // 만료 약화와 강화 및 회피는 발동 기회 보존 확인
    { // 가루병 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 상태 초기화 완료 대기
        yield return VerifyBlackDust(); // 실제 카드 피해와 적 방어도 확인
        yield return new ExitPlayMode(); // 전투 환경 정리와 에디터 복귀
    } // 가루병 검사 종료

    private static IEnumerator VerifyBlackDust() // 분리된 적의 활성 약화와 완전 흡수 비교
    { // 가루병 전투 검사 시작
        Acquire("BlackDustVial"); // 검은 가루병 원본 획득
        using var fight = new Fight("THRUST", "THRUST", "THRUST", "THRUST"); // 같은 기본 피해를 비교할 실제 찌르기 네 장
        var unaffected = fight.Enemies[0]; // 활성 약화가 없는 대조 적
        unaffected.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 1, 2, 1); // 유효 약화로 보지 않을 양의 강화 부여
        unaffected.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 1, 1); // 목록에만 남은 만료 약화 비교 준비
        unaffected.GetStatusEffect(BattleStatusEffectType.Poison).AdvanceDuration(); // 목록 정리 전 수명이 끝난 약화 재현
        yield return UseThrust(fight, 0, 7); // 활성 약화가 없는 명중의 원래 피해 확인
        var evasive = fight.Enemies[1]; // 먼저 회피 후 방어도로 받아낼 적
        evasive.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1); // 실제 지속 피해와 활성 약화 부여
        evasive.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 활성 약화가 있어도 확정 회피 설정
        yield return UseThrust(fight, 1, 0); // 빗나감으로 전투의 한 번 기회를 소모하지 않음 확인
        evasive.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 다음 찌르기를 받아낼 적의 회피 제거
        new BattleStatusEffectProcessor().ProcessPhaseStart(evasive, fight.Turns.CurrentRound); // 기존 중독 지속 피해 경로 실행
        evasive.GainBlock(10); // 기본 일곱과 유물 세 점을 전부 흡수할 방어도 설정
        yield return UseThrust(fight, 1, 0); // 실제 명중하되 체력에 닿지 않는 가루병 발동 확인
        Assert.Zero(evasive.CurrentBlock); // 방어도에서 실제 추가 피해 세 점 차감 확인
        fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Blind, 20, 1, 1); // 후속 명중 대상에게 다른 활성 약화 부여
        yield return UseThrust(fight, 2, 7); // 다른 대상과 다음 카드에 두 번째 피해 가산 차단 확인
    } // 가루병 전투 검사 종료

    [UnityTest] // 무딘 칼 장식의 첫 사용 소모와 녹슨 검집의 누적 검사
    public IEnumerator DulledBladeStacksOnFirstSwordHitAndAllMissStillSpendsTheCard() // 연타 첫 명중과 빗나간 첫 카드 및 새 턴 비교
    { // 칼 장식 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 상태 초기화 완료 대기
        yield return VerifyBladeCharm(); // 두 독립 전투에서 첫 카드의 사용과 명중 확인
        yield return new ExitPlayMode(); // 전투 환경 정리와 에디터 복귀
    } // 칼 장식 검사 종료

    private static IEnumerator VerifyBladeCharm() // 칼 장식의 전투 최초 제한과 검집의 턴 제한 비교
    { // 칼 장식 전투 검사 시작
        Acquire("DulledBladeCharm"); // 무딘 칼 장식 원본 획득
        Acquire("RustyScabbard"); // 기존 매 턴 첫 검 명중 유물 획득
        var attack = CreateSword("TEST_DULLED_DOUBLE", CardEffectType.Damage, CardTargetType.SingleEnemy, 7, 2); // 같은 적에게 두 번 명중하는 검 피해 원본 생성
        var defense = CreateSword("TEST_DULLED_DEFENSE", CardEffectType.GainBlock, CardTargetType.Self, 3, 1); // 피해가 없는 검 속성 대조 원본 생성
        try // 임시 원본의 정리 보장
        { // 전투 비교 시작
            using (var fight = new Fight(new[] { defense, attack, attack })) // 피해 없는 카드 이후 실제 공격 두 장 검사
            { // 첫 명중과 다음 턴 비교 시작
                yield return WinterfallCommonCardExpansionTests.Play(defense, fight.Hand, null, fight.Runner, fight.Deck, fight.Points); // 피해 없는 검 카드의 실제 소모
                yield return UseSword(fight, attack, 0, 19); // 첫 타격에만 검집 둘과 칼 장식 셋 합산 확인
                yield return UseSword(fight, attack, 0, 14); // 같은 턴 두 번째 연타의 추가 피해 없음 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 다음 아군 턴과 버린 덱 재드로우
                yield return UseSword(fight, attack, 0, 16); // 새 턴에 돌아온 검집만 첫 명중에 적용 확인
            } // 첫 명중과 다음 턴 비교 종료
            using (var fight = new Fight(new[] { attack, attack })) // 첫 피해 카드가 전부 빗나가는 별도 전투
            { // 빗나감 소모 비교 시작
                fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 첫 검 카드의 모든 타격에 확정 회피 설정
                yield return UseSword(fight, attack, 0, 0); // 사용이 완료되지만 두 타격 모두 빗나가는 첫 카드
                yield return UseSword(fight, attack, 1, 14); // 실제 명중이 없던 앞 카드에서 전투 기회 이미 소모 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 다음 턴으로 옮겨 첫 검 보정 비교
                yield return UseSword(fight, attack, 1, 16); // 빗나간 뒤 새 턴에도 칼 장식 재발동 차단 확인
            } // 빗나감 소모 비교 종료
        } // 전투 비교 종료
        finally // 전투 확인 뒤 임시 원본 정리
        { // 원본 정리 시작
            Object.Destroy(attack); // 임시 두 타격 카드 해제
            Object.Destroy(defense); // 임시 검 방어 카드 해제
        } // 원본 정리 종료
    } // 칼 장식 전투 검사 종료

    private static IEnumerator Heal(Fight fight, BattleUnitView target, int expected) // 실제 치유 카드 입력과 회복량 확인
    { // 회복량 검사 시작
        var card = fight.View("FIRST_AID").RuntimeCard.SourceData; // 실제 응급 처치 카드의 손패 원본
        int before = target.RuntimeUnit.CurrentHealth; // 카드 사용 직전 수신 체력 기록
        yield return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, target, fight.Runner, fight.Deck, fight.Points); // 실제 손패와 아군 클릭으로 회복 사용
        Assert.AreEqual(expected, target.RuntimeUnit.CurrentHealth - before); // 기존 수신 보정까지 마친 체력 회복 비교
    } // 회복량 검사 종료

    private static IEnumerator UseThrust(Fight fight, int target, int expected) // 기존 찌르기 원본의 실제 플레이 경로 재사용
    { // 찌르기 입력 시작
        return UseSword(fight, fight.View("THRUST").RuntimeCard.SourceData, target, expected); // 원래 손패 원본과 지정 적의 검사 연결
    } // 찌르기 입력 종료

    private static IEnumerator UseSword(Fight fight, CardData card, int target, int expected) // 실제 피해 카드 입력과 적용 체력 비교
    { // 검 공격 검사 시작
        var enemy = fight.Enemies[target]; // 실제 선택할 적의 전투 상태 조회
        int before = enemy.CurrentHealth; // 타격 전 적의 체력 기록
        yield return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, fight.EnemyViews[target], fight.Runner, fight.Deck, fight.Points); // 실제 손패와 적 클릭으로 피해 카드 사용
        Assert.AreEqual(expected, before - enemy.CurrentHealth); // 카드의 타격 전체가 적용한 실제 체력 피해 확인
    } // 검 공격 검사 종료

    private static CardData CreateSword(string id, CardEffectType effect, CardTargetType target, int value, int hits) // 필요한 타격과 피해 없는 검 원본 제작
    { // 원본 제작 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 실제 카드 원본 규칙을 쓰는 임시 검사 자산 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"{id}\",\"displayName\":\"{id}\",\"cardType\":{(int)CardType.Sword},\"targetType\":{(int)target},\"effectType\":{(int)effect},\"effectValue\":{value},\"damageHitCount\":{hits}}}", card); // 원본의 효과 수치와 타격 수 설정
        return card; // 실제 전투에 등록할 임시 원본 반환
    } // 원본 제작 종료

    private static void Acquire(string name) // 실제 카탈로그 유물 획득
    { // 원본 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 실제 게임이 사용하는 유물 원본 조회
        Assert.IsNotNull(relic, name); // 정확한 원본 리소스 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 보유 효과의 실제 획득 결과 확인
    } // 원본 유물 획득 종료
} // 유물 회귀 검사 종료
