using System; // 독립 실제 전투 자료의 자원 해제
using System.Collections; // 플레이 모드 전환의 검사 대기열
using NUnit.Framework; // 실제 타격과 방어 상태 및 정신력 단언
using UnityEngine; // 원본 유물과 전투 자료의 실제 처리
using UnityEngine.TestTools; // 플레이 모드에서 실행하는 Unity 회귀 검사
using Object = UnityEngine.Object; // 이번 검사에 만든 원본만 해제

public sealed class WinterfallStormguardAndCompassTests // 폭풍막이와 순례자 나침판의 실제 방어 및 정신력 원인 검사
{ // 두 실제 보호 효과의 회귀 검사 시작
    [UnityTest] // 실제 직접 광역 명중과 수호 보호의 치명 경계 확인
    public IEnumerator StormguardMatchesPurePreviewAndOnlyActsOnResolvedLivingAreaTargets() // 예고와 실피격 및 광역 조건의 차이 검사
    { // 실제 광역 방어 검사 시작
        yield return new EnterPlayMode(); // 실제 상단 유물과 적 타격 진영을 제공하는 환경 진입
        VerifyAreaHits(); // 두 진영의 실제 피격과 기존 수호 및 단일 타격 확인
        yield return new ExitPlayMode(); // 원래 전투 자료 해제 뒤 편집 모드 복귀
    } // 실제 광역 방어 검사 종료

    [UnityTest] // 앞선 광역에서 남은 방어도가 다음 예고에 이어지는 경우
    public IEnumerator StormguardCarriesUnusedBlockIntoTheNextOrderedEnemyIntent() // 적 공격 순서와 남은 방어도의 순수 예고 검사
    { // 연속 적 의도의 실제 방어 순서 검사 시작
        yield return new EnterPlayMode(); // 실제 일정 공격과 순서 있는 예고를 사용할 모드 진입
        VerifyOrderedPreview(); // 앞선 광역의 잔여 방어도와 뒤따르는 단일 예고 비교
        yield return new ExitPlayMode(); // 두 적과 아군 검사 자료 정리 뒤 편집 모드 복귀
    } // 연속 적 의도의 실제 방어 순서 검사 종료

    [UnityTest] // 실제 적발 정신 피해의 정확한 비율과 원래 비용 검사
    public IEnumerator CompassReducesOnlyHostileLossesAtTheInclusiveThirtyPercentBoundary() // 생존 아군의 실제 정신 상태와 보호 우선순위
    { // 낮은 정신력 보호의 실제 조건 검사 시작
        yield return new EnterPlayMode(); // 보유 유물과 실제 정신력 흐름을 쓰는 검사 환경 진입
        VerifyMentalLoss(); // 삼십 경계와 적 출처 및 기존 실제 정신 비용 비교
        yield return new ExitPlayMode(); // 현재 독립 전투 자료 해제 뒤 편집 모드 복귀
    } // 낮은 정신력 보호의 실제 조건 검사 종료

    private static void VerifyAreaHits() // 현재 실제 광역 대상과 예고 및 기존 수호의 조합
    { // 실제 광역 적 명중 검사 시작
        Acquire("StormguardCloth"); // 폭풍막이 천조각의 원래 유물 보유
        using var fight = new Targets(); // 방어도 영인 실제 생존 아군 세 명과 독립 적 구성
        var marked = fight.Allies[0]; // 광역 원본의 진짜 치명 피해를 판정할 첫 대상
        var other = fight.Allies[1]; // 같은 광역을 맞는 독립 아군
        var guardian = fight.Allies[2]; // 이번 실제 광역에 포함되지 않은 수호 시전자
        Assert.IsTrue(marked.ApplyPersistentHealth(8)); // 추가 방어도 유무에 따라 치명 피해가 갈리는 실제 체력
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, marked.ApplyGuardianHeart(guardian)); // 기존 수호자의 실제 다음 치명 피해 예약 연결
        other.GainBlock(2); // 새 방어도와 이전 방어도가 함께 적용되는 대상
        var area = Attack(fight.First, marked, EnemyTargetRule.AllLiving, 10); // 적의 현재 대상 모양이 전체 생존인 실제 의도
        area.SetAreaTargets(new[] { marked, other }); // 실제 이번 전투에서 공격받는 두 참가 아군만 연결
        Assert.AreEqual(7, area.PreviewDamage(marked).AppliedDamage); // 수호를 발동시키지 않는 새 방어도의 실제 예정 체력 손실
        Assert.AreEqual(5, area.PreviewDamage(other).AbsorbedDamage); // 원래 방어도 둘과 바로 받는 셋의 예상 피해 합계
        Assert.Zero(area.PreviewDamage(guardian).AppliedDamage); // 실제 대상 목록 밖 아군에게 광역 예고 없음 확인
        Assert.Zero(marked.CurrentBlock); // 미리보기에서 실제 보호 상태를 부여하지 않는지 확인
        Assert.AreEqual(2, other.CurrentBlock); // 미리보기 중 기존 실제 방어도도 보존 확인
        area.Execute(); // 현재 대상 목록을 지나는 실제 적 광역 공격 실행
        Assert.AreEqual(1, marked.CurrentHealth); // 새 방어도 때문에 치명 피해가 되지 않은 정상 체력 확인
        Assert.IsNotNull(marked.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 실제 치명 피해 전에 기존 수호 토큰을 소비하지 않음 확인
        Assert.AreEqual(other.MaxHealth - 5, other.CurrentHealth); // 두 실제 방어를 거친 기존 광역 피해 확인
        Assert.AreEqual(guardian.MaxHealth, guardian.CurrentHealth); // 원래 광역 대상 밖 수호자는 이번에 타격되지 않음 확인
        var single = Attack(fight.First, other, EnemyTargetRule.FirstLiving, 10); // 대상이 한 명인 실제 직접 공격
        Assert.Zero(single.PreviewDamage(other).AbsorbedDamage); // 단일 공격에는 신규 방어도의 예고가 없는지 확인
        single.Execute(); // 다음 단일 적 직접 공격 실행
        Assert.AreEqual(other.MaxHealth - 15, other.CurrentHealth); // 단일 명중에 신규 방어도가 생기지 않는 결과 확인
        other.ApplyStatusEffect(BattleStatusEffectType.Evasion, 50, 2, 1); // 실제 광역을 회피시킬 상태 준비
        var miss = Attack(fight.First, other, EnemyTargetRule.AllLiving, 10, 99); // 명중 치에 실패할 실제 대상인 광역 공격
        miss.SetAreaTargets(new[] { other }); // 살아 있는 한 명만 남아도 전체 유형의 실제 대상으로 구성
        miss.Execute(); // 가장 최근 광역의 원래 빗나감과 방어도 효과 확인
        Assert.Zero(other.CurrentBlock); // 실제 빗나간 대상에게 사전 방어도 미부여
        Assert.AreEqual(other.MaxHealth - 15, other.CurrentHealth); // 빗나간 광역에서 원래 체력 유지
        other.TakeDamage(6, BattleDamageType.None, fight.First); // 적 출처이지만 의도상 광역 명중이 아닌 실제 간접 피해
        Assert.AreEqual(other.MaxHealth - 21, other.CurrentHealth); // 간접 효과로 인해 신규 방어도가 생기지 않음 확인
    } // 실제 광역 적 명중 검사 종료

    private static void VerifyOrderedPreview() // 현재 방어도와 앞선 광역으로 남는 수량의 실제 비교
    { // 방어도 순수 예고 누적 검사 시작
        Acquire("StormguardCloth"); // 실제 폭풍막이 원본 유물 보유
        using var fight = new Targets(); // 앞선 작은 광역과 뒤따르는 실제 적 단일 공격 준비
        using var actions = new BattleEnemyActionRuntime(new[] { fight.First, fight.Second }, fight.Allies); // 정상 순서 기준을 쓰는 실제 적 의도 관리자
        Assert.AreEqual(2, actions.PrepareActions()); // 기존 적 행동 원본에서 두 실제 의도 생성
        var first = actions.PlannedActions[0]; // 가장 빠른 실제 적 광역 공격 의도
        var second = actions.PlannedActions[1]; // 더 느린 실제 적 단일 공격 의도
        var ally = fight.Allies[0]; // 두 예정 공격을 실제로 맞는 아군
        Assert.IsTrue(first.IsAreaAttack); // 실제 순서의 첫 공격이 명시 광역인지 확인
        Assert.IsFalse(second.IsAreaAttack); // 실제 다음 공격이 단일 대상인지 확인
        Assert.Zero(ally.CurrentBlock); // 실제 원래 방어도가 없는 선행 예고 준비
        Assert.AreEqual(2, second.PreviewDamage(ally).AbsorbedDamage); // 방어도 셋 가운데 먼저 쓴 한 점을 빼고 남은 실제 예상치
        Assert.Zero(ally.CurrentBlock); // 연속 예고에서도 실제로 방어도 이벤트 상태 변경 없음
        actions.ExecuteAction(first); // 먼저 예정됐던 낮은 광역 공격의 실타격 실행
        Assert.AreEqual(2, ally.CurrentBlock); // 실제 방어도 셋 가운데 한 점을 쓴 정상 상태 확인
        Assert.AreEqual(2, second.PreviewDamage(ally).AbsorbedDamage); // 실제 공격 전후에 같은 두 점 흡수 예고 확인
        actions.ExecuteAction(second); // 순서상 다음인 일반 단일 공격 실행
        Assert.AreEqual(ally.MaxHealth - 6, ally.CurrentHealth); // 실제 이전 광역 방어도가 두 번째 타격에 맞게 이어짐 확인
    } // 방어도 순수 예고 누적 검사 종료

    private static void VerifyMentalLoss() // 적발 실제 정신력 피해와 비용 및 감소 한계
    { // 나침판 정신력 경계 검사 시작
        Acquire("PilgrimBrokenCompass"); // 실제 순례자의 깨진 나침판 보유
        using var fight = new Targets(); // 같은 정신력에 서로 다른 적 및 아군 출처를 대조할 전투
        var victim = fight.Allies[0]; // 정신력 변화가 들어갈 살아 있는 아군
        var enemy = fight.First; // 정신 피해를 주는 실제 적
        var friend = fight.Allies[1]; // 명시 정신력 원가가 적의 효과가 아닌 실제 아군
        Assert.IsTrue(victim.ApplyPersistentMental(30)); // 현재 최대 정신력의 정확히 삼십 퍼센트
        var first = victim.ChangeMental(-10, BattleMentalChangeReason.DisruptReceived, enemy); // 적의 실제 교란 피해 열 점 요청
        Assert.AreEqual(-10, first.RequestedDelta); // 원래 사건의 정신 피해 요청 수치를 그대로 남기는지 확인
        Assert.AreEqual(23, victim.CurrentMental); // 삼십 포함 범위에서 적발 열 점이 일곱으로 줄어드는 실제 결과
        Assert.IsTrue(victim.ApplyPersistentMental(31)); // 임계 바로 위인 실제 정신력 준비
        victim.ChangeMental(-10, BattleMentalChangeReason.DisruptReceived, enemy); // 같은 적의 같은 양 정신 피해 요청
        Assert.AreEqual(21, victim.CurrentMental); // 임계 밖에서 원래 열 점을 전부 받는지 확인
        Assert.IsTrue(victim.ApplyPersistentMental(30)); // 같은 나침판 범위에서 비적대 조건을 재설정
        victim.ChangeMental(-5, BattleMentalChangeReason.CardEffect, friend); // 아군 출처가 직접 준 실제 원래 카드 효과
        Assert.AreEqual(25, victim.CurrentMental); // 적이 아닌 카드 비용 및 팀 효과를 줄이지 않는지 확인
        victim.SpendBattleMentalCost(5, BattleMentalChangeReason.CardEffect); // 네이티브 명시 카드 정신력 대가 실제 지불
        Assert.AreEqual(20, victim.CurrentMental); // 현재 정신력과 별개로 원래 다섯 점이 결제되는지 확인
        victim.ChangeMental(-3, BattleMentalChangeReason.DisruptReceived); // 적 출처가 없는 실제 외부 변화 호출
        Assert.AreEqual(17, victim.CurrentMental); // 적 출처가 없는 환경 및 별도 변화의 수치 유지
        Assert.IsTrue(victim.ApplyPersistentMental(30)); // 한 점 절삭과 기존 공용 보호의 순서를 비교할 경계
        int prevented = 0; // 감소가 남아 있을 때만 호출되어야 하는 실제 후속 보호
        victim.PreventIncomingMentalLoss = _ => { prevented++; return true; }; // 남은 실제 피해를 한 번 막는 기존 보호 단계 대역
        victim.ChangeMental(-1, BattleMentalChangeReason.ReceiveDamage, enemy); // 내림 뒤 실제 손실이 영인 적의 피격 정신 피해
        Assert.Zero(prevented); // 전액 줄인 한 점이 기존 첫 실제 정신 피해 보호를 소비하지 않음 확인
        victim.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage, enemy); // 실제 남는 적발 정신 피해의 공용 보호 차례
        Assert.AreEqual(1, prevented); // 원래 발생한 양수 정신 손실만 기존 공용 보호에 전달
        Assert.AreEqual(30, victim.CurrentMental); // 원래 정신력에서 양쪽 보호가 적용된 최종 수치 확인
    } // 나침판 정신력 경계 검사 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime actor, BattleUnitRuntime target, EnemyTargetRule rule, int amount, int roll = 0) // 실제 적 행동의 대상 규칙과 명중 경계 생성
    { // 일반 직접 적 행동 원본 설정 시작
        return new BattleEnemyAction(actor, target, EnemyActionType.Attack, BattleDamageType.Physical, amount, BattleStatusEffectType.None, 1, 1, rule, "검사 공격", 0, 1, 5, 0, percentRoll: () => roll, battleType: BattleType.Normal); // 실제 원래 조우의 적 공격과 정해진 난수 제공
    } // 일반 직접 적 행동 원본 설정 종료

    private static void Acquire(string name) // 실제 유물의 기존 회차 획득 절차
    { // 보호 유물 보유 처리 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 원본 리소스의 실제 유물 조회
        Assert.IsNotNull(relic, name); // 유효한 새 유물 원본 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 보유함의 정상 유물 획득
    } // 보호 유물 보유 처리 종료

    private sealed class Targets : IDisposable // 원본 피해량 비교를 위한 독립 실제 아군과 적 데이터
    { // 실제 전투 대상 구성 시작
        private readonly CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 다른 게임 자료에 영향 없는 아군 자료
        private readonly EnemyData area = ScriptableObject.CreateInstance<EnemyData>(); // 첫 광역 적의 실제 데이터
        private readonly EnemyData single = ScriptableObject.CreateInstance<EnemyData>(); // 뒤의 단일 적 실제 데이터
        internal BattleUnitRuntime[] Allies { get; } // 별개 체력과 방어도를 가진 실제 세 생존 아군
        internal BattleUnitRuntime First { get; } // 실제 순서에서 앞서는 광역 적
        internal BattleUnitRuntime Second { get; } // 실제 순서에서 뒤따르는 단일 적

        internal Targets() // 실제 아군과 적에 검사할 원본 저항 및 속도 설정
        { // 체력과 순서가 고정된 전투 참가자 생성 시작
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100,\"initialMental\":50,\"physicalDefense\":0}", character); // 원본 피해 비교가 명확한 공통 실제 아군 초기 수치
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"initialMental\":50,\"basicAttackPower\":1,\"targetRule\":3,\"minimumActionSpeed\":20,\"maximumActionSpeed\":20}", area); // 먼저 나가는 실제 생존 전체 광역 공격 자료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"initialMental\":50,\"basicAttackPower\":8,\"targetRule\":0,\"minimumActionSpeed\":1,\"maximumActionSpeed\":1}", single); // 뒤에 실행되는 실제 단일 직접 공격 자료
            Allies = new[] { BattleUnitRuntime.CreateAlly(character), BattleUnitRuntime.CreateAlly(character), BattleUnitRuntime.CreateAlly(character) }; // 같은 수치지만 별개 피해 기록을 가진 실제 아군 배열
            First = BattleUnitRuntime.CreateEnemy(area); // 원래 광역 행동을 계획할 첫 실제 적
            Second = BattleUnitRuntime.CreateEnemy(single); // 원래 단일 행동을 계획할 다음 실제 적
        } // 체력과 순서가 고정된 전투 참가자 생성 종료

        public void Dispose() // 이번에 만든 실제 전투 원본만 해제
        { // 자원 해제 시작
            Object.Destroy(character); // 새로 만든 원본 아군 자료 제거
            Object.Destroy(area); // 새로 만든 검사 광역 적 자료 제거
            Object.Destroy(single); // 새로 만든 검사 단일 적 자료 제거
        } // 자원 해제 종료
    } // 실제 전투 대상 구성 종료
} // 두 실제 보호 효과의 회귀 검사 종료
