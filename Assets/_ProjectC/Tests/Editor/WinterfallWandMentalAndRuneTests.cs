using System; // 실제 임시 전투 자원의 종료 처리
using System.Collections; // 전투 애니메이션 및 플레이 대기
using System.Collections.Generic; // 받은 정신 피해와 실제 발생자 기록
using System.Linq; // 기존 적 행동 목록에서 보존된 각 행동 조회
using System.Reflection; // 전투가 공유하는 준비 행동과 기본 피해 계산 경로 연결
using NUnit.Framework; // 지정된 효과와 대조 적의 실제 수치 검증
using UnityEngine; // 실제 전투 유물 원본 조회
using UnityEngine.TestTools; // 플레이 모드 카드 입력 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 손패와 전투 실행 검사 환경
using Object = UnityEngine.Object; // 검사에서 만든 실제 회차 관리자 제거

public sealed class WinterfallWandMentalAndRuneTests // 정신 충격과 냉기 폭발 및 룬 폭발 검사
{ // 지팡이 공격 상태 검사 시작
    [UnityTest] // 실제 카드 정신 피해와 고해실 표식의 정상 연결
    public IEnumerator MentalShockUsesSourcedCardMentalLossOnlyOnALivingLandedEnemy() // 체력 방어와 실제 정신 피해를 구분하는 전투 검사
    { // 정신 충격 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 클릭과 전투 사건 전달의 플레이 모드
        yield return CheckMentalShock(); // 실제 정신력 감소와 소유자 및 명중 결과 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 정신 충격 검사 종료

    private static IEnumerator CheckMentalShock() // 고해 연결과 빗나감 및 쓰러진 적의 실제 비교
    { // 정신 충격의 실제 전투 준비 시작
        using var owned = new ConfessionalRelic(); // 고해실의 못 원본과 실제 회차 인벤토리 사용
        using var fight = new Fight("MENTAL_SHOCK", "MENTAL_SHOCK", "MENTAL_SHOCK", "MENTAL_SHOCK"); // 실제 정신 충격 원본 네 장
        var dispatcher = new BattleEventDispatcher(); // 실제 피해와 정신력 이벤트 중계
        using var events = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 전투 유닛의 실제 사건 연결
        using var relics = new BattleRelicEffectController(dispatcher, owned.Manager.Inventory, owned.Manager.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 고해실의 실제 정신 피해 이벤트 구독
        var actor = fight.Allies[0]; // 명시 정신 피해에 기록할 실제 카드 소유자
        var hit = fight.Enemies[0]; // 실제 마법 피해와 정신 피해를 함께 받을 적
        var blocked = fight.Enemies[1]; // 방어도로 체력 손실을 흡수할 생존 적
        var evader = fight.Enemies[2]; // 확정 회피로 상태가 달라지지 않아야 할 적
        var fragile = fight.Enemies[3]; // 실제 카드 기본 피해로 바로 쓰러질 적
        var changes = new Dictionary<BattleUnitRuntime, BattleMentalChangeResult>(); // 실제 카드 효과 신호의 적별 기록
        foreach (var enemy in fight.Enemies) // 각 적의 명시 정신력 변화를 실제 유닛에서 추적
        { // 적 사고 사건 기록 시작
            enemy.MentalChanged += (target, change) => // 실제 카드 발동의 정신력 변화 확인
            { // 명시 정신 피해 판별 시작
                if (change.Reason == BattleMentalChangeReason.CardEffect) // 일반 타격의 피격 정신 감소와 구별
                { // 실제 카드 피해 기록 시작
                    changes.Add(target, change); // 실제 손실과 카드 소유자 참조 보관
                } // 실제 카드 피해 기록 종료
            }; // 명시 정신 피해 판별 종료
        } // 적 사고 사건 기록 종료
        blocked.GainBlock(30); // 체력 피해 전체를 기존 방어도로 막을 대조 적
        evader.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 카드가 확정 빗나가는 활성 회피
        fragile.TakeDamage(196, BattleDamageType.None); // 카드 첫 피해로 전투불능이 될 체력 넷
        yield return fight.Use("MENTAL_SHOCK", 0); // 실제 원본 카드의 일반 명중
        Assert.AreEqual(190, hit.CurrentHealth); // 기본 마법 열 피해의 실제 적용
        Assert.AreEqual(-5, changes[hit].RequestedDelta); // 정신 충격의 원본 요청 정신력 다섯
        Assert.AreEqual(-5, changes[hit].AppliedDelta); // 기존 정신력 범위에서 실제로 감소한 다섯
        Assert.AreSame(actor, changes[hit].SourceUnit); // 카드의 원래 소유자와 동일한 정신 피해 발생자
        Assert.AreEqual(3, hit.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)?.EffectiveValue); // 실제 정신 피해로 남긴 고해실 예약 표식
        yield return fight.Use("MENTAL_SHOCK", 1); // 기본 피해를 전부 흡수하는 살아 있는 적에게 명중
        Assert.AreEqual(200, blocked.CurrentHealth); // 방어도로 인해 체력 손실이 없었음 확인
        Assert.AreEqual(-5, changes[blocked].AppliedDelta); // 명중으로 독립된 실제 정신 피해 적용
        Assert.AreSame(actor, changes[blocked].SourceUnit); // 체력 피해가 없어도 기록되는 실제 카드 소유자
        Assert.AreEqual(3, blocked.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)?.EffectiveValue); // 명시 정신 효과만으로도 발동한 고해실 표식
        yield return fight.Use("MENTAL_SHOCK", 2); // 확정 빗나감 대상의 정상 카드 사용
        Assert.AreEqual(200, evader.CurrentHealth); // 빗나감으로 인한 실제 체력 피해 없음
        Assert.IsFalse(changes.ContainsKey(evader)); // 빗나감은 명시 정신력 손실도 없음 확인
        yield return fight.Use("MENTAL_SHOCK", 3); // 실제 기본 마법 피해로 쓰러지는 적 클릭
        Assert.IsTrue(fragile.IsDead); // 실제 피해에서 전투불능 여부 확인
        Assert.IsFalse(changes.ContainsKey(fragile)); // 전투불능 이후 정신 효과 미적용 확인
    } // 정신 충격의 실제 전투 준비 종료

    [UnityTest] // 모든 적의 개별 명중과 행동 예고 속도 확인
    public IEnumerator FrostBurstDamagesAllLivingEnemiesAndSlowsOnlyLandedNonimmuneSurvivors() // 실제 광역 타격과 한 적 턴의 속도 감소 검증
    { // 냉기 폭발 실제 검사 시작
        yield return new EnterPlayMode(); // 실제 대상 목록과 광역 투사체 처리 준비
        yield return CheckFrostBurst(); // 정상 적과 면역 및 회피의 별도 효과 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 냉기 폭발 실제 검사 종료

    private static IEnumerator CheckFrostBurst() // 실제 생존 적 전체의 마법 피해와 각자 별도 상태 비교
    { // 서리 상태 별도 검사 시작
        using var fight = new Fight("FROST_BURST"); // 보물 원본의 실제 광역 마법 피해 카드
        var normal = fight.Enemies[0]; // 서리 피해와 속도 감소를 함께 받을 적
        var immune = fight.Enemies[1]; // 피해만 받고 느려지지 않는 상태 면역 적
        var evading = fight.Enemies[2]; // 광역 안에서도 정상 명중 판정을 피하는 적
        var fragile = fight.Enemies[3]; // 서리 피해로 쓰러져 상태가 남지 않을 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 일반 약화 전부를 막는 기존 면역
        evading.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 광역 타격도 정상 빗나갈 회피
        fragile.TakeDamage(197, BattleDamageType.None); // 첫 실제 광역 피해로 사라질 체력 셋
        var field = typeof(Fight).GetField("actions", BindingFlags.Instance | BindingFlags.NonPublic); // 카드와 같은 실제 예정 적 행동 관리자 조회
        Assert.IsNotNull(field); // 공용 전투 환경의 원래 예정 행동 조회 경로 확인
        var actions = (BattleEnemyActionRuntime)field.GetValue(fight); // 실제 카드 명중이 갱신하는 원본 행동 목록
        Assert.AreEqual(4, actions.PrepareActions()); // 네 적의 원래 행동 속도와 대상을 먼저 준비
        var planned = actions.PlannedActions.Single(action => action.Actor == normal); // 같은 예정 행동 인스턴스의 원래 속도 비교
        int originalSpeed = planned.ActionSpeed; // 느려지기 전에 한 번 굴린 기존 값
        var originalTarget = planned.Target; // 속도 갱신에서 보존할 원래 대상
        int points = fight.Points.CurrentActionPoints; // 광역 카드의 실제 한 번 비용 대조
        Assert.AreEqual(2, fight.View("FROST_BURST").RuntimeCard.ApCost); // 실제 손패 원본이 요구하는 행동력 두 점
        yield return fight.Use("FROST_BURST"); // 적 전체를 클릭 없이 고르는 실제 광역 지팡이
        Assert.AreEqual(points - 2, fight.Points.CurrentActionPoints); // 원본 카드의 실제 비용 지불 확인
        Assert.AreEqual(193, normal.CurrentHealth); // 첫 적의 독립 마법 피해 일곱
        Assert.AreEqual(193, immune.CurrentHealth); // 기존 상태 면역과 별개인 광역 피해 일곱
        Assert.AreEqual(200, evading.CurrentHealth); // 빗나간 개별 적의 체력 보존
        Assert.IsTrue(fragile.IsDead); // 맞아 쓰러진 적의 실제 광역 처리
        var slow = normal.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown); // 기존 적 행동 순서가 읽는 실제 속도 약화
        Assert.AreEqual(1, slow?.EffectiveValue); // 보장된 속도 감소 한 점
        Assert.AreEqual(1, slow?.RemainingTurns); // 원래 효과의 한 적 턴 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 상태 면역의 실제 속도 약화 차단
        Assert.IsNull(evading.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 빗나간 개별 대상의 속도 효과 없음
        Assert.IsNull(fragile.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 새로 쓰러진 적에게 남지 않는 속도 효과
        CollectionAssert.Contains(actions.PlannedActions, planned); // 기존 행동을 새로 난수 생성하지 않는 순서 갱신 확인
        Assert.AreSame(originalTarget, planned.Target); // 기존 행동이 유지한 실제 원래 공격 대상
        Assert.AreEqual(Math.Max(1, originalSpeed - 1), planned.ActionSpeed); // 원래 속도를 유지하며 한 점만 낮은 예고 수치
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적의 행동 시점 진입
        Assert.IsNotNull(normal.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 적이 실제 행동하는 턴에 남은 약화 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 한 적 턴 종료와 지속 소모
        Assert.IsNull(normal.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 다음 턴을 넘어가지 않는 실제 기간
        Assert.AreEqual(originalSpeed, planned.ActionSpeed); // 활성 속도 약화 해제 뒤 기존 계획의 원래 속도
    } // 서리 상태 별도 검사 종료

    [UnityTest] // 동일한 표식 판정의 실제 타격과 기본 피해 예고 검증
    public IEnumerator RuneBurstAddsSixOnceForAnActiveNativeMarkInPreviewAndActualDamage() // 여러 활성 표식 및 만료와 별개 고해 표식의 비교
    { // 룬 폭발 실제 검사 시작
        yield return new EnterPlayMode(); // 실제 원본 카드와 마법 피해 계산의 플레이 모드
        yield return CheckRuneBurst(); // 표식별 실제 피해 수치와 예고 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 룬 폭발 실제 검사 종료

    private static IEnumerator CheckRuneBurst() // 실제 표식 적과 기본 피해의 현재 기획 수치 대조
    { // 룬 피해 조건 검사 시작
        using var fight = new Fight("RUNE_BURST", "RUNE_BURST", "RUNE_BURST"); // 세 조건에 사용할 실제 룬 폭발 카드
        var marked = fight.Enemies[0]; // 기존 카드 피해를 받는 활성 표식 대상
        var expired = fight.Enemies[1]; // 비활성 표식을 아직 지우지 않은 적
        var confession = fight.Enemies[2]; // 직접 피해에 별도 효과인 고해 표식 대상
        marked.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 2, 1); // 기존 공명 표식이 줄 수 있는 룬 피해 조건
        marked.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 2, 1); // 두 개여도 룬이 한 번만 보정되는 다른 실제 표식
        expired.ApplyStatusEffect(BattleStatusEffectType.StethoscopeMark, 1, 1, 1); // 만료를 비교할 정상 표식 원본
        expired.GetStatusEffect(BattleStatusEffectType.StethoscopeMark).AdvanceDuration(); // 아직 목록에 남아 있으나 비활성인 상태
        confession.ApplyStatusEffect(BattleStatusEffectType.ConfessionDamageMark, 3, 1, 1); // 기존 카드 표식과 구별되는 독립 피해 예약
        var card = fight.View("RUNE_BURST").RuntimeCard; // 실제 카드를 읽을 공용 피해 계산 입력
        var rules = typeof(BattleUnitRuntime).Assembly.GetType("SwordCardCombat"); // 일반 타격이 이미 사용 중인 기본 피해 계산 타입
        var preview = rules?.GetMethod("BaseDamage", BindingFlags.Static | BindingFlags.NonPublic); // 대상과 같은 기본 피해를 계산하는 공유 경로
        Assert.IsNotNull(preview); // 실제 마법 피해와 피해 예고의 공통 진입점 확인
        Assert.AreEqual(17, (int)preview.Invoke(null, new object[] { card, 2, marked })); // 두 표식에도 한 번 더한 열일곱 기본 피해
        Assert.AreEqual(11, (int)preview.Invoke(null, new object[] { card, 2, expired })); // 만료된 표식의 추가 피해 없음
        Assert.AreEqual(11, (int)preview.Invoke(null, new object[] { card, 2, confession })); // 룬과 별개인 고해의 다음 피해 표시
        yield return fight.Use("RUNE_BURST", 0); // 실제 두 표식이 있는 적에게 직접 명중
        Assert.AreEqual(183, marked.CurrentHealth); // 기존 일반 피해 계산을 지나는 열일곱 마법 피해
        yield return fight.Use("RUNE_BURST", 1); // 만료된 표식 상태의 적에게 직접 명중
        Assert.AreEqual(189, expired.CurrentHealth); // 원본 마법 피해 열하나
        yield return fight.Use("RUNE_BURST", 2); // 고해의 독립 가산만 실제로 있는 적에게 명중
        Assert.AreEqual(186, confession.CurrentHealth); // 룬 추가는 없고 기존 고해만 소비한 열넷 피해
    } // 룬 피해 조건 검사 종료

    private sealed class ConfessionalRelic : IDisposable // 실제 고해실 소유와 기존 회차 복원
    { // 실제 유물 획득 준비 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 이전 회차 관리자 확인
        private readonly RelicData relic; // 실제 고해실의 못 등록 원본
        internal RelicRunManager Manager { get; } = RelicRunManager.EnsureInstance(); // 실제 회차 유물 보관함

        internal ConfessionalRelic() // 실제 정신 피해와 연동하는 유물 획득
        { // 고해실 보유 처리 시작
            relic = Resources.Load<RelicData>("Relics/ConfessionalNail"); // 원본 유물과 실제 효과 수치 연결
            Assert.IsNotNull(relic); // 이전 일차의 필수 유물 원본 존재 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, Manager.TryAcquire(relic)); // 실제 회차의 유물 획득 사용
        } // 고해실 보유 처리 종료

        public void Dispose() // 테스트가 획득한 유물과 임시 관리 객체 정리
        { // 실제 임시 인벤토리 정리 시작
            Manager.TryRemove(relic.RelicId); // 이번 테스트에서 추가한 유물만 제거
            if (previous == null) // 기존 회차가 없는 테스트에서만 객체 제거
            { // 임시 관리자 제거 시작
                Object.Destroy(Manager.gameObject); // 실제 테스트에서 만든 임시 회차 제거
            } // 임시 관리자 제거 종료
        } // 실제 임시 인벤토리 정리 종료
    } // 실제 유물 획득 준비 종료
} // 지팡이 공격 상태 검사 종료
