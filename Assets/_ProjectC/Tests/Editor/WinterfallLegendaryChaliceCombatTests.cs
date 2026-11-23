using System.Collections; // 실제 손패와 턴 실행의 프레임 대기
using System.Linq; // 생존 아군과 실제 대상 화면 검색
using NUnit.Framework; // 전설 성배의 실제 전투 수치 검증
using UnityEngine; // 실제 전투의 런타임 유닛과 입력 객체 조회
using UnityEngine.TestTools; // 독립 Unity 플레이 모드 검사
using static WinterfallCommonCardExpansionTests; // 실제 게임 카드 입력의 공용 방법 재사용
using static WinterfallSwordCombatTests; // 기존 실제 전투 검사 환경 재사용

public sealed class WinterfallLegendaryChaliceCombatTests // 대천사와 불멸의 보호 및 부활의 경계 검사
{ // 전설 성배 실제 전투 검사 시작
    [UnityTest] // 전체 디버프 해제와 치명 보호의 실제 카드 사용 확인
    public IEnumerator ArchangelCleanseAndImmortalWardApplyToEachLivingAlly() // 회복 감소 해제 시점과 아군별 추가 보호 수치 확인
    { // 실제 전투 입력 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 조작의 유니티 환경 시작
        yield return VerifyArchangelAndImmortal(); // 원본 카드의 정화와 방어도 및 버티기 확인
        yield return new ExitPlayMode(); // 독립 검사 환경 종료
    } // 실제 전투 입력 검사 종료

    private static IEnumerator VerifyArchangelAndImmortal() // 실제 손패의 생존 아군 대상 카드 사용
    { // 각 아군 효과 검증 시작
        using var fight = new Fight("ARCHANGEL_BLESSING", "IMMORTAL_CHALICE"); // 전설 성배 원본 두 장으로 전투 구성
        var active = fight.Allies[0]; // 시전자이자 실제 부상과 약화 대상
        var friend = fight.Allies[1]; // 같은 광역 효과를 받을 생존 동료
        var down = fight.Allies[2]; // 일반 효과가 적용되지 않을 전투불능 동료
        down.ApplyPersistentHealth(0); // 초기에 쓰러진 아군 상태 준비
        Assert.IsTrue(active.TrySpendHealth(41) && friend.TrySpendHealth(41)); // 시트 회복량을 수용할 두 생존 아군 체력 차이
        active.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 50, 3, 1); // 정화 전에 남으면 회복을 절반 줄이는 약화 상태
        active.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 1); // 동시에 없어질 두 번째 일반 약화
        friend.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 3, 1); // 다른 아군에게도 정화 적용 확인
        friend.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 3, 3, 1); // 정화 후에도 보존될 강화 상태
        int activeBefore = active.CurrentHealth; // 시전자 회복 전 실제 체력 기록
        int friendBefore = friend.CurrentHealth; // 두 번째 대상의 회복 전 실제 체력 기록
        yield return fight.Use("ARCHANGEL_BLESSING"); // 시트의 전체 정화와 체력 회복 카드 사용
        Assert.AreEqual(activeBefore + 25, active.CurrentHealth); // 회복 감소를 먼저 제거해 전체 회복량 지급 확인
        Assert.AreEqual(friendBefore + 25, friend.CurrentHealth); // 둘째 대상의 개별 실제 회복량 확인
        Assert.False(active.HasDebuff || friend.HasDebuff); // 두 아군에서 제거 가능한 약화가 모두 해제됐는지 확인
        Assert.IsNotNull(friend.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseUp)); // 강화는 광역 정화 후 유지
        Assert.True(down.IsDead); // 쓰러진 아군이 정상 회복과 구분되는지 확인
        yield return fight.Use("IMMORTAL_CHALICE"); // 실제 손패에서 아군 전체 치명 보호 카드 사용
        Assert.AreEqual(10, active.CurrentBlock); // 시전자의 정식 방어도 확인
        Assert.AreEqual(10, friend.CurrentBlock); // 동료가 개별 방어도 전체를 받은 결과 확인
        Assert.AreEqual(0, down.CurrentBlock); // 전투불능에서 광역 보호와 방어도 미수신 확인
        Assert.IsNotNull(active.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 시전자의 개별 첫 치명 피해 보호 확인
        Assert.IsNotNull(friend.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 둘째 대상의 독립 보호 확인
        friend.TakeDamage(friend.MaxHealth * 3, BattleDamageType.None, fight.Enemies[0]); // 실제 치명 피해에 원래 보호 적용
        Assert.AreEqual(1, friend.CurrentHealth); // 받은 피해가 치명적이어도 체력 하나로 생존 확인
        Assert.IsNull(friend.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 첫 피해 후 쓴 보호만 해제 확인
        Assert.IsNotNull(active.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 첫 아군의 별도 보호 여유 보존
    } // 각 아군 효과 검증 종료

    [UnityTest] // 부활 성가의 실제 쓰러진 대상 선택 검증
    public IEnumerator ResurrectionChantSelectsFallenAndRoundsHalfHealthUp() // 최대 체력의 50퍼센트 올림과 일반 보호 예약 구분 확인
    { // 실제 쓰러진 아군 선택 시작
        yield return new EnterPlayMode(); // 실제 전투 버튼 입력 상태 진입
        yield return VerifyResurrectionChant(); // 시트 부활 효과의 정상 게임 입력 검증
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 실제 쓰러진 아군 선택 종료

    private static IEnumerator VerifyResurrectionChant() // 쓰러진 아군을 다시 출전시키는 실제 조작
    { // 전설 부활 검증 시작
        using var fight = new Fight("RESURRECTION_CHANT"); // 원본 보물 카드로 독립 실제 전투 시작
        var target = fight.Allies[1]; // 홀수 최대 체력을 만들 실제 아군
        if (target.MaxHealth % 2 == 0) // 이미 홀수 최대 체력이 아닌지 확인
        { // 홀수 최대 체력 준비 시작
            target.ModifyMaxHealth(1); // 반 퍼센트 경계의 올림 검사를 위한 체력 상한 증가
        } // 홀수 최대 체력 준비 종료
        target.TakeDamage(target.MaxHealth * 3, BattleDamageType.None, fight.Enemies[0]); // 실제 전투에서 쓰러뜨려 손패와 대상 입력을 함께 갱신
        Assert.True(target.IsDead); // 부활 성가를 선택하기 전 전투불능 확인
        var targetView = UnityEngine.Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).Single(view => view.RuntimeUnit == target); // 기존 전투 대상 검증에서 같은 런타임의 실제 객체 검색
        var card = fight.View("RESURRECTION_CHANT").RuntimeCard; // 정상 손패의 실제 부활 성가
        yield return Play(card.SourceData, fight.Hand, targetView, fight.Runner, fight.Deck, fight.Points); // 실제 손패 클릭과 전투불능 아군 클릭
        Assert.False(target.IsDead); // 정상 대상이 동일한 게임 객체에서 다시 생존 확인
        Assert.AreEqual((target.MaxHealth + 1) / 2, target.CurrentHealth); // 최대 체력 홀수의 절반을 올림해 복원 확인
        Assert.AreEqual(15, fight.Points.CurrentActionPoints); // 실제 행동력 다섯 점만 결제 확인
    } // 전설 부활 검증 종료
} // 전설 성배 실제 전투 검사 종료
