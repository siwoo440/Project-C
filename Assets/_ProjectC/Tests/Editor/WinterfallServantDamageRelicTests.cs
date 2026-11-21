using System; // 전투의 임시 유물 보관함 정리
using System.Collections; // 실제 전투와 카드 사용 대기
using NUnit.Framework; // 피해량과 상태의 실제 검증
using UnityEngine; // 등록 유물 원본과 보관함 생성
using UnityEngine.TestTools; // 플레이 모드 시작과 종료
using Fight = WinterfallSwordCombatTests.Fight; // 기존 게임 카드와 전투 테스트 환경
using Object = UnityEngine.Object; // 임시 회차 관리자 제거

public sealed class WinterfallServantDamageRelicTests // 고해실 및 초상 못의 실전 등록 검사
{ // 정신 및 지속 피해 검사 시작
    [UnityTest] // 실제 정신 피해 사건의 임시 표식과 직접 타격 확인
    public IEnumerator ConfessionalNailMarksActualMentalLossAndConsumesOnlyOnAValidDirectHit() // 일반 면역과 피해 예고 및 실제 사용 대조
    { // 고해실의 못 검사 시작
        yield return new EnterPlayMode(); // 실제 피해와 전투 상태 신호의 플레이 모드
        yield return CheckConfessionalNail(); // 실제 적 정신력과 차단된 직접 공격의 대조
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 고해실의 못 검사 종료

    private static IEnumerator CheckConfessionalNail() // 출처가 있는 정신력과 실제 표식 소비 시점 확인
    { // 정신력 손실 및 직접 피해 준비 시작
        using var owned = new OwnedRelic("ConfessionalNail"); // 실제 고해실의 못 원본을 회차에 획득
        using var fight = new Fight("ICE_ARROW"); // 보물에서 얻는 실제 지팡이 카드를 전투에 준비
        var dispatcher = new BattleEventDispatcher(); // 게임과 같은 공용 전투 사건 버스
        using var events = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 실제 적 정신력과 카드 사건 연결
        using var relics = new BattleRelicEffectController(dispatcher, owned.Manager.Inventory, owned.Manager.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 대상과 시점을 확인하는 실제 유물 관리자
        var actor = fight.Allies[0]; // 실제 카드를 사용하며 정신 피해를 줄 아군
        var enemy = fight.Enemies[0]; // 기본 정신력 손실 및 피해 예고의 실제 적
        var immune = fight.Enemies[1]; // 일반 약화 면역을 확인할 대조 적
        var empty = fight.Enemies[2]; // 정신력 하한 때문에 실제 손실이 없는 적
        enemy.ChangeMental(-3, BattleMentalChangeReason.RelicEffect); // 출처나 직접 효과가 없는 일반 비용 대조
        enemy.ChangeMental(-3, BattleMentalChangeReason.CardEffect); // 실제 카드 소유자가 없는 정신력 손실
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 무관한 비용과 출처 없는 손실의 표식 제외
        empty.ChangeMental(-100, BattleMentalChangeReason.RelicEffect); // 일반 직접 효과로 줄어들 수 있는 정신력을 소진
        Assert.AreEqual(0, empty.ChangeMental(-4, BattleMentalChangeReason.CardEffect, actor).AppliedDelta); // 남은 정신력이 없어 실제 변화 없음 확인
        Assert.IsNull(empty.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 영 정신 피해로는 표식 없음 확인
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 상태 약화 면역을 가진 생존 적
        Assert.Less(immune.ChangeMental(-4, BattleMentalChangeReason.CardEffect, actor).AppliedDelta, 0); // 상태 면역과 별개인 실제 정신력 감소 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 표식을 차단하는 기존 상태 면역 확인
        Assert.Less(enemy.ChangeMental(-5, BattleMentalChangeReason.CardEffect, actor).AppliedDelta, 0); // 실제 소유 아군 카드에서 생긴 첫 유효 정신 피해
        var mark = enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark); // 거래 중 실제 유물로 생긴 상태 참조
        Assert.AreEqual(3, mark?.EffectiveValue); // 다음 직접 피해에 쓸 원본 가산량 셋 확인
        enemy.ChangeMental(-2, BattleMentalChangeReason.CardEffect, actor); // 아직 같은 표식이 남은 적의 다른 정신력 손실
        Assert.AreSame(mark, enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 이미 살아 있는 표식의 기존 한 번 예약 보존
        Assert.AreEqual(1, mark.StackCount); // 새 정신력 손실에도 무한 중첩되지 않는 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 같은 실제 적 턴 사이에 표식 지속 여부 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴이 시작하도록 정산
        Assert.AreSame(mark, enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 실제 공격을 기다리는 턴 독립 표식
        Assert.AreEqual(8, enemy.PreviewDamage(5, BattleDamageType.Physical, actor, alliedDirectAttack: true).RawDamage); // 직접 피해 예고의 같은 추가 피해 계산
        Assert.AreSame(mark, enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 예고만으로 표식을 소모하지 않음 확인
        enemy.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 2, 1); // 다음 피해 전부를 무효화하는 원래 상태
        Assert.IsTrue(enemy.TakeDamage(5, BattleDamageType.Physical, actor, alliedDirectAttack: true).WasDamageNullified); // 직접 피해가 실제 무효 처리됨 확인
        Assert.AreSame(mark, enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 전부 무효 처리된 타격의 표식 보존
        enemy.GainBlock(30); // 일정 실제 방어도로 표식을 적용한 직접 피해 흡수
        Assert.AreEqual(5, enemy.TakeDamage(5, BattleDamageType.Physical, actor).RawDamage); // 같은 출처라도 고정 후속 피해는 표식 가산 불가
        Assert.AreSame(mark, enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 별도 고정 피해가 다음 직접 타격을 소비하지 않음 확인
        int block = enemy.CurrentBlock; // 실제 카드 명중 직전 피해 흡수량 비교값
        int printed = fight.View("ICE_ARROW").RuntimeCard.EffectValue; // 실제 얼음 화살의 원본 마법 피해
        yield return fight.Use("ICE_ARROW", 0); // 정상 카드 클릭과 적 지정으로 표식 소비
        Assert.AreEqual(block - printed - 3, enemy.CurrentBlock); // 고해 표식의 추가 피해가 실제 방어도에 도달한 결과
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 체력 피해가 없어 새 표식 없이 사용한 한 번 소비 확인
        var damaged = fight.Enemies[3]; // 실제 피격에서 정신력 감소 사건을 얻을 새로운 적
        damaged.TakeDamage(5, BattleDamageType.Magical, actor, alliedDirectAttack: true); // 실제 체력 피해의 정신 변화에서 현재 아군 발생자 전달
        Assert.AreEqual(3, damaged.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)?.EffectiveValue); // 출처가 있는 첫 실제 피격 정신 손실에서도 표식 부여
        Assert.IsTrue(fight.Turns.TryEscape()); // 일반 전투의 실제 종료 처리
        enemy.ChangeMental(-4, BattleMentalChangeReason.CardEffect, actor); // 결과 확정 뒤 기존 런타임의 재호출
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 끝난 전투의 정신력 변화로 표식 미부여
    } // 정신력 손실 및 직접 피해 준비 종료

    [UnityTest] // 실제 출혈 틱만 조건부로 증가하는 피해 확인
    public IEnumerator BloodstainedPortraitNailAddsDamageOnlyToBleedTicksOnActiveCardMarks() // 실제 상태 적용과 출혈 틱의 공용 피해 대조
    { // 피 묻은 초상 못 검사 시작
        yield return new EnterPlayMode(); // 전투와 실제 유물 인벤토리의 플레이 모드
        CheckPortraitNail(); // 표시 조건과 출혈의 방어도 우회 유지 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 피 묻은 초상 못 검사 종료

    private static void CheckPortraitNail() // 현재 대상 표식과 원래 출혈 피해 처리 확인
    { // 실제 표식 및 출혈 상황 시작
        using var owned = new OwnedRelic("BloodstainedPortraitNail"); // 피 묻은 초상 못의 실제 원본 획득
        using var fight = new Fight("ICE_ARROW"); // 상태 지속을 처리할 실제 적 진영 전투
        var actor = fight.Allies[0]; // 카드 표식과 출혈을 부여하는 실제 아군
        var marked = fight.Enemies[0]; // 적의 등록된 실제 카드 피해 표식 대조
        var plain = fight.Enemies[1]; // 표식이 없는 동일 출혈 대조
        var different = fight.Enemies[2]; // 고해실의 별도 예약이 있는 대조 적
        marked.ApplyStatusEffect(BattleStatusEffectType.StethoscopeMark, 3, 2, 1, actor); // 첫 아군 턴을 넘어 출혈 틱과 겹치는 청진기 표식
        marked.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 3, 1, actor); // 공격 턴 종료에 지워지는 전차 표식
        marked.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 3, 1, actor); // 공격 턴 종료에 지워지는 공명 표식
        different.ApplyStatusEffect(BattleStatusEffectType.ConfessionDamageMark, 3, 1, 1, actor); // 기존 세 카드 피해 표식과 구분할 특수 상태
        marked.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 2, 1, actor); // 표식이 있는 적에게 실제 출혈 부여
        plain.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 2, 1, actor); // 비교 적에게 동일한 수치와 기간의 출혈
        different.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 2, 1, actor); // 고해 표식만 있는 적의 별도 출혈 대조
        marked.GainBlock(20); // 출혈이 기존처럼 피해 방어도를 건너뛰는 비교 설정
        Assert.AreEqual(200, marked.CurrentHealth); // 출혈을 처음 부여했을 때 즉시 피해가 없는 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴 시작 이후 아직 출혈 틱 대기
        Assert.AreEqual(200, marked.CurrentHealth); // 적 턴 시작에는 출혈 피해가 생기지 않는 확인
        Assert.IsNull(marked.GetStatusEffect(BattleStatusEffectType.ChariotMark)); // 기존 전차 표식의 아군 종료 규칙 확인
        Assert.IsNull(marked.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 기존 공명 표식의 아군 종료 규칙 확인
        Assert.IsNotNull(marked.GetStatusEffect(BattleStatusEffectType.StethoscopeMark)); // 실제 틱을 받을 적의 남아 있는 표식 확인
        marked.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 1, 1, actor); // 동일 틱에 겹친 유효 표식의 중복 피해 검증 배열
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 적 턴 끝에 정상 출혈 피해 적용
        Assert.AreEqual(195, marked.CurrentHealth); // 동일 표식 두 개에도 출혈 셋과 가산 둘을 한 번만 적용
        Assert.AreEqual(197, plain.CurrentHealth); // 카드 표식 없는 적의 기본 출혈 피해 보존
        Assert.AreEqual(197, different.CurrentHealth); // 고해 표식에만 출혈 가산이 생기지 않는 확인
        Assert.IsNotNull(different.GetStatusEffect(BattleStatusEffectType.ConfessionDamageMark)); // 일반 출혈에 고해실 표식을 쓰지 않는 확인
        Assert.IsTrue(marked.RemoveStatusEffect(BattleStatusEffectType.ChariotMark)); // 다음 출혈 전에 실제 첫 표식 해제
        Assert.IsTrue(marked.RemoveStatusEffect(BattleStatusEffectType.StethoscopeMark)); // 다음 출혈 전에 두 번째 실제 표식 해제
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 다음 실제 적의 턴 시작
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 표식이 없는 직접 출혈의 다음 틱 정산
        Assert.AreEqual(192, marked.CurrentHealth); // 해제 뒤 원래 출혈 셋만 처리되는 체력
    } // 실제 표식 및 출혈 상황 종료

    private sealed class OwnedRelic : IDisposable // 실제 보유 효과 확인과 임시 회차 정리
    { // 원본 획득 정리 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 테스트 전 실제 회차 관리자 여부
        private readonly RelicData relic; // 획득한 실제 유물 원본
        internal RelicRunManager Manager { get; } = RelicRunManager.EnsureInstance(); // 기존 실제 전투 보유 유물 조회 연결

        internal OwnedRelic(string name) // 요청한 실제 인게임 원본을 한 번 획득
        { // 원본 유물 보관 시작
            relic = Resources.Load<RelicData>("Relics/" + name); // 원본 유물의 정상 리소스 조회
            Assert.IsNotNull(relic, name); // 런타임 원본이 등록되어 있는지 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, Manager.TryAcquire(relic)); // 실제 회차 보관함 기준의 신규 유물 획득
        } // 원본 유물 보관 종료

        public void Dispose() // 실제 테스트 보유 유물과 관리자 상태 복원
        { // 원본 유물 정리 시작
            Manager.TryRemove(relic.RelicId); // 이번 검사에서 새로 얻은 해당 원본만 제거
            if (previous == null) // 테스트 중 새로 만든 관리자만 확인
            { // 임시 회차 제거 시작
                Object.Destroy(Manager.gameObject); // 테스트 전 없던 임시 회차 객체 제거
            } // 임시 회차 제거 종료
        } // 원본 유물 정리 종료
    } // 원본 획득 정리 종료
} // 정신 및 지속 피해 검사 종료
