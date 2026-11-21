using System.Collections; // 실제 손패 입력 및 플레이 모드 대기
using System.Collections.Generic; // 카드의 개별 정신력 비용 기록
using System.Linq; // 둘 이상의 손패 비교 대상 조회
using System.Reflection; // 실제 전투의 보스 페이즈 시험 구성
using NUnit.Framework; // 실제 피해와 상태 및 행동력 확인
using UnityEngine; // 실제 연출 환경의 대기 시간 조회
using UnityEngine.TestTools; // 플레이 모드의 입력과 전투 검증
using Fight = WinterfallSwordCombatTests.Fight; // 원본 카드가 연결된 실제 전투 검사 환경

public sealed class WinterfallWandTimeAndCollapseTests // 시간 정지와 공허 붕괴의 실전 회귀 검사
{ // 두 지팡이 카드의 실제 사용 검사 시작
    [UnityTest] // 전체 적의 한 행동 제어 및 실제 공용 손패 카드 수 검증
    public IEnumerator TimeStopCancelsOnlyOneEnemyActionAndDrawsEvenAgainstImmunity() // 경직과 독립 드로우의 실제 핸드 정산 확인
    { // 시간 정지의 실제 경직과 카드 이동 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 입력을 다루는 플레이 모드 진입
        using (var fight = new Fight("TIME_STOP", "THRUST", "KNEE_SLASH")) // 시간 정지와 드로우할 다른 진짜 카드 구성
        { // 선행 무효 사용과 실제 발동 및 경직 지속 비교 시작
            var card = fight.View("TIME_STOP"); // 실제 사용 가능한 원본 시간 정지
            Assert.AreEqual(5, card.RuntimeCard.ApCost); // 원본의 실제 행동력 다섯 점 확인
            var reference = fight.Deck.Hand.First(item => item != card.RuntimeCard); // 실제 덱에서 보충할 다른 카드 한 장
            Assert.IsTrue(fight.Deck.DiscardCard(reference)); // 사용 이전의 공용 드로우 후보 확보
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 실제 경직 면역 비교 적 준비
            fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 범위에서 제외할 실제 전투불능 적 준비
            int points = fight.Points.CurrentActionPoints; // 사용 전 실제 공용 행동력
            Assert.IsTrue(fight.Points.Spend(points - 4)); // 시전 비용보다 한 점 적은 실제 행동력 설정
            int handCount = fight.Deck.HandCount; // 무효 사용이 유지해야 할 공용 손패 수
            card.OnPointerClick(null); // 비용이 모자란 실제 원본 선택 시도
            Assert.AreEqual(handCount, fight.Deck.HandCount); // 거부한 시도에서 새 드로우 없음 확인
            Assert.IsTrue(fight.Enemies.All(unit => unit.GetStatusEffect(BattleStatusEffectType.Stagger) == null)); // 거부한 시도에서 전체 상태 부여 없음 확인
            fight.Points.Restore(); // 정상 시전 전 기존 행동력 복구
            yield return PlayTimeStop(fight); // 실제 사용 카드 이동과 공용 드로우 완료 대기
            Assert.AreEqual(handCount, fight.Deck.HandCount); // 사용 한 장을 보충하는 정확한 공용 카드 한 장 확인
            Assert.AreEqual(points - 5, fight.Points.CurrentActionPoints); // 승인한 카드 한 장의 실제 비용 확인
            Assert.IsNotNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Stagger)); // 실제 첫 생존 적에게 다음 행동 취소 부여 확인
            Assert.IsNotNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Stagger)); // 실제 두 번째 적에게도 상태 부여 확인
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Stagger)); // 기존 약화 면역이 실제 효과를 막는지 확인
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.Stagger)); // 전투불능 적 제외 확인
            var target = fight.Allies[0]; // 한 번 경직을 건너뛰는 실제 적 행동의 아군 대상
            int savedHealth = target.CurrentHealth; // 행동 한 번 취소에서 유지될 대상 체력
            var action = new BattleEnemyAction(fight.Enemies[0], target, EnemyActionType.Attack, BattleDamageType.Physical, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "정지 검사", 1, 1, 1, 0, percentRoll: () => 0); // 실제 적 행동에 고정 명중 난수 연결
            action.Execute(); // 원래 적 행동 진입에서 경직 한 번 소비
            Assert.AreEqual(savedHealth, target.CurrentHealth); // 경직이 정확히 첫 공격을 취소했는지 확인
            Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Stagger)); // 취소한 실제 행동 직후 해당 경직 제거 확인
            Assert.Greater(action.Execute().DamageResult.AppliedDamage, 0); // 두 번째 실제 공격 정상 수행 확인
            Assert.IsNotNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Stagger)); // 다른 적의 별도 취소는 그대로 보존 확인
        } // 선행 무효 사용과 실제 발동 및 경직 지속 비교 종료
        using (var fight = new Fight("TIME_STOP", "THRUST")) // 모든 적이 면역인 실제 두 번째 카드 전투
        { // 상태 성공 수 영과 실제 드로우 비교 시작
            foreach (var enemy in fight.Enemies) // 실제 적 전체에 정상 상태 면역 부여
            { // 개별 적 면역 설정 시작
                enemy.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 적 경직에 대한 기존 면역 보장
            } // 개별 적 면역 설정 종료
            int before = fight.Deck.HandCount; // 사용 전 공용 손패 카드 수
            yield return PlayTimeStop(fight); // 모든 경직이 막혀도 정상 카드 한 장 사용
            Assert.AreEqual(before, fight.Deck.HandCount); // 적용 성공 수 영에도 공용 손패에 한 장 드로우 확인
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Stagger) == null)); // 전체 면역의 실제 무효 상태 확인
        } // 상태 성공 수 영과 실제 드로우 비교 종료
        yield return new ExitPlayMode(); // 실제 시간 정지의 플레이 모드 종료
    } // 시간 정지의 실제 경직과 카드 이동 검사 종료

    [UnityTest] // 공허 붕괴의 원래 명중 실패와 실제 처치 비교
    public IEnumerator VoidCollapsePaysOnceOnMissOrSurvivingEnemyAndSkipsATrueKill() // 원본 45 피해와 한 번만 지불하는 실제 정신력 비용 확인
    { // 공허 붕괴 세 가지 정산 검사 시작
        yield return new EnterPlayMode(); // 카드 원본과 실제 적을 함께 구동할 모드 진입
        using (var fight = new Fight("VOID_COLLAPSE", "VOID_COLLAPSE", "VOID_COLLAPSE")) // 생존과 빗나감과 처치의 원본 세 장 준비
        { // 세 실제 사용의 정신력 비교 시작
            var owner = fight.Allies[0]; // 실제 공허 붕괴 카드 소유자
            Assert.IsTrue(owner.ApplyPersistentMental(50)); // 정확한 원본 비용용 중립 정신력 설정
            var costs = RecordCardMentalChanges(owner); // 명중과 처치의 기존 보상에서 분리한 카드 비용 기록
            var first = fight.Enemies[0]; // 먼저 맞아도 살아남을 실제 적
            var evasive = fight.Enemies[1]; // 정상 명중 판정을 빗나갈 실제 적
            var lethal = fight.Enemies[2]; // 실제 원본 카드로 쓰러뜨릴 적
            int shownRawDamage = 0; // 원본의 방어 이전 마법 피해 확인용
            first.DamageTaken += (_, damage) => shownRawDamage = damage.RawDamage; // 실제 원래 타격의 출력 수치 기록
            evasive.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 이 적에 대한 확정 회피 상태 설정
            lethal.TakeDamage(175, BattleDamageType.None); // 원래 카드의 한 명중으로 쓰러질 정확한 체력 설정
            Assert.AreEqual(6, fight.View("VOID_COLLAPSE").RuntimeCard.ApCost); // 원본 행동력 여섯 점 확인
            yield return fight.Use("VOID_COLLAPSE", 0); // 살아 있는 첫 원본 적에게 실제 마법 공격
            Assert.AreEqual(45, shownRawDamage); // 기존 마법 공격의 원본 피해 마흔다섯 확인
            Assert.IsFalse(first.IsDead); // 그 피해에도 생존한 적 확인
            CollectionAssert.AreEqual(new[] { -10 }, costs); // 생존 적을 때린 원본의 정확한 비용 확인
            int beforeMiss = evasive.CurrentHealth; // 정상 빗나감에서 유지될 적 체력
            yield return fight.Use("VOID_COLLAPSE", 1); // 선택한 적의 확정 회피로 원본 타격 빗나감
            Assert.AreEqual(beforeMiss, evasive.CurrentHealth); // 원래 빗나간 카드의 체력 피해 없음 확인
            CollectionAssert.AreEqual(new[] { -10, -10 }, costs); // 빗나간 원본에서 한 번 더 적용한 비용 확인
            yield return fight.Use("VOID_COLLAPSE", 2); // 실제 선택한 낮은 체력 적에게 원본 명중
            Assert.IsTrue(lethal.IsDead); // 해당 원본 공격에서 실제로 쓰러짐 확인
            CollectionAssert.AreEqual(new[] { -10, -10 }, costs); // 원본 타격의 실제 처치에 추가 비용 없음 확인
        } // 세 실제 사용의 정신력 비교 종료
        yield return new ExitPlayMode(); // 실제 원본 공격 검사 종료
    } // 공허 붕괴 세 가지 정산 검사 종료

    [UnityTest] // 독립 반향과 보스 전환에서 실제 원본 효과의 처치 범위 확인
    public IEnumerator VoidCollapseDoesNotRepeatForEchoOrTreatABossTransitionAsAKill() // 반향 처치와 보스 단계 변화의 원본 비용 확인
    { // 두 분리 피해 결과의 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 원본과 반향 및 보스 체력 감시 연결
        using (var fight = new Fight("VOID_COLLAPSE")) // 반향만 막타에 성공하는 실제 전투
        { // 원본 실패 대가와 반향 독립 비교 시작
            var owner = fight.Allies[0]; // 실제 체력 피해와 정신력 비용의 시전자
            Assert.IsTrue(owner.ApplyPersistentMental(50)); // 반향 검사 정신력 기준값 준비
            var costs = RecordCardMentalChanges(owner); // 실제 반향 앞뒤의 카드 정신력 비용 기록
            owner.ApplyStatusEffect(BattleStatusEffectType.WandEcho, 50, 1, 1, owner); // 원래 기본 피해 절반의 준비 반향 설정
            fight.Enemies[0].TakeDamage(140, BattleDamageType.None); // 원래 45에는 살고 이후 22 반향에는 죽는 적
            yield return fight.Use("VOID_COLLAPSE", 0); // 원본과 실제 반향이 차례로 적용되는 공격 실행
            Assert.IsTrue(fight.Enemies[0].IsDead); // 뒤이은 독립 반향의 실제 처치 확인
            CollectionAssert.AreEqual(new[] { -10 }, costs); // 반향으로 처치해도 실패한 원본 비용 한 번 확인
        } // 원본 실패 대가와 반향 독립 비교 종료
        using (var fight = new Fight("VOID_COLLAPSE")) // 절반 체력에서 살아남아 페이즈가 바뀌는 전투
        { // 실제 체력 감시형 보스 전환 비교 시작
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic; // 검사에서 기존 내부 참조를 조회할 범위
            typeof(BattleTurnRuntime).GetField("<BattleType>k__BackingField", flags).SetValue(fight.Turns, BattleType.Boss); // 재사용 전투를 보스 조우 규칙으로 설정
            var actions = (BattleEnemyActionRuntime)typeof(BattleCardActionController).GetField("enemyActions", flags).GetValue(fight.Controller); // 실제 전투에서 사용 중인 적 행동 관리자
            var boss = fight.Enemies[0]; // 원래 출전 적의 실제 보스 체력
            using var phase = new WinterfallBossPhaseRuntime(boss, fight.Turns, actions, 1); // 보스의 실제 피해와 단계 전환 관리자 연결
            boss.TakeDamage(90, BattleDamageType.None); // 원래 최대 체력의 절반을 넘는 보스 상태 준비
            Assert.IsFalse(phase.HasPendingTransition); // 카드 피해 전에는 아직 실제 단계 전환 없음 확인
            Assert.IsTrue(fight.Allies[0].ApplyPersistentMental(50)); // 원래 보스 공격의 비교 정신력 기준 설정
            var costs = RecordCardMentalChanges(fight.Allies[0]); // 보스 페이즈가 바뀔 때 카드 비용 기록
            yield return fight.Use("VOID_COLLAPSE", 0); // 생존 보스를 절반 아래로 만드는 실제 원래 마법 공격
            Assert.IsTrue(phase.HasPendingTransition); // 실제 감시에 따른 보스 단계 전환 예약 확인
            Assert.IsFalse(boss.IsDead); // 페이즈가 바뀌더라도 원래 적 생존 확인
            CollectionAssert.AreEqual(new[] { -10 }, costs); // 보스가 살아남은 실제 타격의 원본 비용 확인
        } // 실제 체력 감시형 보스 전환 비교 종료
        yield return new ExitPlayMode(); // 반향과 보스 전투 검사 종료
    } // 두 분리 피해 결과의 실제 전투 검사 종료

    private static List<int> RecordCardMentalChanges(BattleUnitRuntime owner) // 기존 보상과 별도로 정산하는 카드 정신력 내역
    { // 카드 효과 내역 수집 시작
        var costs = new List<int>(); // 한 번씩 기록할 실제 정신력 변화량
        owner.MentalChanged += (_, change) => // 시전자의 실제 정신력 변화 구독
        { // 정신력 변화 분류 시작
            if (change.Reason == BattleMentalChangeReason.CardEffect) // 명중과 처치의 기본 보상을 제외한 카드 효과 확인
            { // 카드 정신력 비용 기록 시작
                costs.Add(change.AppliedDelta); // 전투에 반영한 실제 정신력 변화 저장
            } // 카드 정신력 비용 기록 종료
        }; // 정신력 감시 연결 완료
        return costs; // 실제 전투 중 수집한 카드 변화량 제공
    } // 카드 효과 내역 수집 종료

    private static IEnumerator PlayTimeStop(Fight fight) // 공용 손패 드로우를 기다리는 실제 카드 입력
    { // 지정 없는 전체 사용의 화면 입력 시작
        fight.View("TIME_STOP").OnPointerClick(null); // 현재 손패의 실제 시간 정지 카드 클릭
        float deadline = Time.realtimeSinceStartup + 10f; // 기존 카드 연출의 검사 대기 기한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 상태 적용과 추가 드로우 완료 대기
        { // 카드 연출의 다음 프레임 처리 시작
            yield return null; // 유니티 게임의 실제 프레임 진행
        } // 카드 연출의 다음 프레임 처리 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 정상 카드 동작 완료 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 드로우 뒤 원래 카드 영역의 서로 다른 위치 확인
    } // 지정 없는 전체 사용의 화면 입력 종료
} // 두 지팡이 카드의 실제 사용 검사 종료
