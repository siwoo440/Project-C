using System.Collections; // 플레이 모드와 실제 카드 연출 대기
using System.Linq; // 실제 예고의 고정 분담 순서 비교
using System.Reflection; // 실제 적 공격의 전투 명단 연결
using NUnit.Framework; // 피해 분배와 지속 턴 검증
using UnityEngine.TestTools; // 실제 전투 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 입력과 선제 검사 환경

public sealed class WinterfallPainDistributionTests // 고통 분산의 실제 전투와 피해 예고 검사
{ // 고통 분산 검사 시작
    [UnityTest] // 정식 카드 비용과 개별 분담 결과 검사
    public IEnumerator DistributesBeforePrimaryBlockWithRosterRemainderAndLinkedSharePriority() // 실제 방어와 기존 연결 분담 우선 확인
    { // 고통 분산 카드 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 상태와 카드 환경 진입
        yield return VerifyDamageAndPreview(); // 플레이 모드에서 고통 분산과 실제 예고 대조
        yield return new ExitPlayMode(); // 독립 실행 환경 종료
    } // 고통 분산 카드 검사 종료

    private static IEnumerator VerifyDamageAndPreview() // 실제 카드와 적 공격의 피해량 정산
    { // 실제 전투 구성 시작
        using var fight = new Fight("PAIN_DISTRIBUTION"); // 정식 성배 카드 한 장으로 전투 준비
        var primary = fight.Allies[0]; // 원래 방어도를 쓸 아군
        var first = fight.Allies[1]; // 나머지 한 점이 먼저 배정될 분담자
        var second = fight.Allies[2]; // 뒤에 배정할 생존 분담자
        var enemy = fight.Enemies[0]; // 실제 적 직접 타격의 출처
        foreach (BattleUnitRuntime ally in fight.Allies) // 배정 뒤의 양수 피해를 견딜 대상들
        { // 검사 체력 설정 시작
            ally.ModifyMaxHealth(100 - ally.MaxHealth); // 일정 최대 체력의 실제 유닛 준비
            ally.ApplyPersistentHealth(100); // 경계와 보호 피해 차이를 볼 시작 체력
        } // 검사 체력 설정 종료
        int points = fight.Points.CurrentActionPoints; // 카드 사용 전 실제 공용 행동력
        yield return fight.Use("PAIN_DISTRIBUTION"); // 실제 자동 전체 아군 고통 분산 사용
        Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 카드 원본의 실제 행동력 세 점 소모
        foreach (BattleUnitRuntime ally in fight.Allies) // 대상마다 독립 분산 상태 적용 확인
        { // 전체 상태 확인 시작
            Assert.AreEqual(25, ally.GetStatusEffect(BattleStatusEffectType.PainDistribution)?.EffectiveValue); // 살아 있는 아군의 실제 사분의 일 비율
            Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.PainDistribution)?.RemainingTurns); // 해당 아군의 실제 두 적 턴 보호
        } // 전체 상태 확인 종료
        BattleEnemyAction attack = Attack(fight, primary, 20); // 방어 전에 분담량을 나눌 실제 적 행동
        int total = attack.AttackAmount / 4; // 원래 적 피해를 기준으로 한 이론 분담 총량
        Assert.IsTrue(total > 1 && total % 2 == 1); // 앞 분담자에게 추가 한 점이 가는 정량 검사 조건
        primary.GainBlock(attack.AttackAmount - total); // 원래 대상의 피해 몫에 정확히 맞출 방어도
        first.GainBlock(9); // 기존 분담과 같은 방어도 우회 피해의 대조
        first.ApplyStatusEffect(BattleStatusEffectType.SanctuaryWard, 10, 2, 1); // 무방어 대상에게 적용할 기존 수신 감소
        var planned = attack.PreviewPainDistribution(primary); // 정상 적 예고가 사용하는 파티 분담 결과
        Assert.AreEqual(2, planned.Count); // 원래 대상을 뺀 두 살아 있는 분담자의 예고
        Assert.AreSame(first, planned[0].Target); // 원래 출전 순서의 첫 분담 대상 확인
        Assert.AreEqual((total / 2 + 1) * 90 / 100, planned[0].Damage); // 나머지와 성역을 모두 적용한 첫 예상 피해
        Assert.AreSame(second, planned[1].Target); // 두 번째 고정 출전 아군의 분담 위치
        Assert.AreEqual(total / 2, planned[1].Damage); // 두 번째 분담자의 원래 균등 몫
        var preview = attack.PreviewDamage(); // 원래 피격자의 방어도와 파티 분담 예고
        Assert.AreEqual(attack.AttackAmount - total, preview.AbsorbedDamage); // 파티 분배 뒤 원래 몫만 방어도에 적용
        var actual = attack.Execute().DamageResult; // 실제 명중과 조각 피해 적용
        Assert.AreEqual(preview.AbsorbedDamage, actual.AbsorbedDamage); // 기본 예고의 방어도 소모와 실제 공격 일치
        Assert.AreEqual(preview.AppliedDamage, actual.AppliedDamage); // 원래 아군의 예고와 실제 체력 피해 일치
        Assert.AreEqual(planned[0].Damage, 100 - first.CurrentHealth); // 첫 분담자의 실제 성역 반영 체력 손실
        Assert.AreEqual(planned[1].Damage, 100 - second.CurrentHealth); // 둘째 분담자의 실제 나머지 순서와 체력 손실
        Assert.AreEqual(9, first.CurrentBlock); // 분담 피해가 기존 방어도를 소모하지 않음 확인
        Assert.AreEqual(100, primary.CurrentHealth); // 원래 대상에 분담 조각이 돌아오지 않음 확인
        var linked = Attack(fight, primary, 20); // 기존 단일 분담이 설정된 추가 공격 확인
        var setShare = typeof(BattleEnemyAction).GetMethod("SetDamageShare", BindingFlags.Instance | BindingFlags.NonPublic); // 원래 단일 분담의 실제 연결 진입점 조회
        Assert.IsNotNull(setShare); // 기존 단일 연결의 설정 메서드 확인
        Assert.IsTrue((bool)setShare.Invoke(linked, new object[] { second })); // 같은 파티에서 단일 분담 연결 선택
        Assert.IsEmpty(linked.PreviewPainDistribution(primary)); // 단일 분담에서 파티 상태의 추가 몫 차단
        int sharedPreview = linked.PreviewSharedDamage(); // 기존 한 명의 분담 예고 유지
        Assert.AreEqual(linked.AttackAmount / 2, sharedPreview); // 기존 절반 분담이 고통 분산에 우선
        int healthBefore = second.CurrentHealth; // 기존 링크 대상의 실제 체력 저장
        linked.Execute(); // 원래 단일 분담 피해 실행
        Assert.AreEqual(sharedPreview, healthBefore - second.CurrentHealth); // 기존 단일 링크의 예상 피해와 실제 일치
        first.RemoveStatusEffect(BattleStatusEffectType.PainDistribution); // 첫 동료의 실제 분담 상태만 제거
        second.ApplyPersistentHealth(0); // 두 번째 동료가 이미 쓰러진 타격 조건
        var solo = Attack(fight, primary, 6); // 혼자만 남은 분담 대상의 추가 직접 타격
        Assert.IsEmpty(solo.PreviewPainDistribution(primary)); // 살아 있는 영향 대상이 없으면 분담하지 않음 확인
        Assert.AreEqual(solo.AttackAmount, solo.Execute().DamageResult.AppliedDamage); // 대상 없음 때문에 원래 피해가 유실되지 않음 확인
    } // 실제 전투 구성 종료

    [UnityTest] // 이미 걸린 상태의 실제 선제 턴과 정상 적 턴의 독립 만료
    public IEnumerator AmbushCountsOnlyOneActualEnemyPhase() // 선제 앞뒤의 정확한 두 적 턴 지속 검사
    { // 선제 지속 검사 시작
        yield return new EnterPlayMode(); // 적이 실제로 먼저 행동하는 모드 진입
        VerifyAmbushExpiry(); // 현재 아군 상태의 진영 종료 경계 검증
        yield return new ExitPlayMode(); // 독립 테스트 종료
    } // 선제 지속 검사 종료

    private static void VerifyAmbushExpiry() // 기존 선제와 정상 아군 종료의 차이 확인
    { // 선제 전투의 상태 검사 시작
        using var fight = new Fight(true, "PAIN_DISTRIBUTION"); // 적 선제 실제 첫 진영으로 시작
        var primary = fight.Allies[0]; // 기존 같이 걸린 상태의 원래 공격 대상
        foreach (BattleUnitRuntime ally in fight.Allies) // 테스트로 먼저 걸어 둔 일반 상태 대상 준비
        { // 이미 걸린 상태 생성 시작
            ally.ApplyStatusEffect(BattleStatusEffectType.PainDistribution, 25, 2, 1); // 선제 진행 중 실제 상태 데이터 부여
        } // 이미 걸린 상태 생성 종료
        Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turns.CurrentPhase); // 실제 선제 단계 시작 확인
        Assert.IsNotEmpty(Attack(fight, primary, 20).PreviewPainDistribution(primary)); // 첫 선제 공격도 실제 상태가 있으면 분담
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 선제의 실제 적 턴 종료
        Assert.AreEqual(1, primary.GetStatusEffect(BattleStatusEffectType.PainDistribution)?.RemainingTurns); // 선제에서 한 번만 지속 차감
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 첫 아군 턴 종료
        Assert.AreEqual(1, primary.GetStatusEffect(BattleStatusEffectType.PainDistribution)?.RemainingTurns); // 아군 단계에서 추가 차감 없음 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴을 종료
        Assert.IsNull(primary.GetStatusEffect(BattleStatusEffectType.PainDistribution)); // 두 번의 실제 적 종료에서 정상 만료
        Assert.IsEmpty(Attack(fight, primary, 20).PreviewPainDistribution(primary)); // 만료 뒤 기존 정상 피해로 돌아옴 확인
    } // 선제 전투의 상태 검사 종료

    private static BattleEnemyAction Attack(Fight fight, BattleUnitRuntime primary, int amount) // 실제 피해 예고와 실행이 연결된 고정 공격 생성
    { // 이전 적과 실제 보유 명단 준비 시작
        var action = new BattleEnemyAction(fight.Enemies[0], primary, EnemyActionType.Attack, BattleDamageType.None, amount, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "고통 분산 검사", 1, 1, 1, 0, percentRoll: () => 0); // 직접 적 공격과 실제 명중 경로 생성
        var setParty = typeof(BattleEnemyAction).GetMethod("SetPainDistributionParty", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 적 행동 준비가 사용하는 파티 명단 연결
        Assert.IsNotNull(setParty); // 적 공격의 실제 파티 연결 경로 존재 확인
        setParty.Invoke(action, new object[] { fight.Allies }); // 정상 게임과 같은 출전 순서의 아군 목록 연결
        return action; // 실제 예고와 실행 메서드를 쓰는 직접 공격 반환
    } // 이전 적과 실제 보유 명단 준비 종료
} // 고통 분산 검사 종료
