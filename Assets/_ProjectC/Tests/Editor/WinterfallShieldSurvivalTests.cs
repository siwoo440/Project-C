using System; // 검사 난수 경계 조건
using NUnit.Framework; // 방패 상태의 실제 결과 검증
using UnityEngine; // 임시 전투 원본 생성
using Object = UnityEngine.Object; // 임시 원본 정리
using Random = UnityEngine.Random; // 게임과 같은 난수 사용

public sealed class WinterfallShieldSurvivalTests // 집중 방어와 출혈 저항 및 불굴 검사
{ // 방패 상태 검사 시작
    private CharacterData allySource; // 기본 방어력이 없는 아군 원본
    private EnemyData enemySource; // 직접 피격과 출혈의 적 원본
    private BattleUnitRuntime ally; // 방패 상태를 적용할 아군
    private BattleUnitRuntime enemy; // 피해와 디버프를 발생시킬 적
    private Random.State originalRandomState; // 다른 검사에 영향을 주지 않을 기존 난수 상태

    [SetUp] // 독립적인 피해와 난수 환경 준비
    public void SetUp() // 원본과 전투 유닛 설정
    { // 검사 환경 준비 시작
        originalRandomState = Random.state; // 기존 난수 상태 보관
        allySource = ScriptableObject.CreateInstance<CharacterData>(); // 방어력과 저항력이 없는 아군 원본 생성
        enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 적 공격 원본 생성
        ally = BattleUnitRuntime.CreateAlly(allySource); // 실제 아군 피해 런타임 생성
        enemy = BattleUnitRuntime.CreateEnemy(enemySource); // 실제 적 공격 발생자 생성
    } // 검사 환경 준비 종료

    [TearDown] // 검사 원본과 난수 복구
    public void TearDown() // 검사 환경 해제
    { // 정리 시작
        Random.state = originalRandomState; // 이전 난수 상태 복원
        Object.DestroyImmediate(allySource); // 검사 아군 원본 제거
        Object.DestroyImmediate(enemySource); // 검사 적 원본 제거
    } // 정리 종료

    [Test] // 합성 비율의 한 번 내림과 방어도 순서 확인
    public void FocusedDefenseCombinesWithExistingShieldModifiersBeforeBlock() // 직접 피해에서만 반감과 고정 경감 적용 확인
    { // 집중 방어 검사 시작
        ally.ApplyStatusEffect(BattleStatusEffectType.FocusedDefense, 50, 1, 1); // 집중 방어의 직접 피격 반감
        ally.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, 30, 1, 1); // 마법 피해 저항 비율
        ally.ApplyStatusEffect(BattleStatusEffectType.GuardianCircle, 10, 1, 1); // 공통 직접 피해 경감 비율
        ally.ApplyStatusEffect(BattleStatusEffectType.DangerInterception, 70, 1, 1); // 한 번만 적용할 직접 피해 경감
        ally.ApplyStatusEffect(BattleStatusEffectType.ShockAbsorption, 1, 1, 1); // 비율 뒤의 고정 피해 경감
        ally.GainBlock(1); // 피해 비율을 적용한 뒤 흡수할 방어도
        Assert.AreEqual(31, ally.PreviewDamage(32, BattleDamageType.Magical, enemy).AppliedDamage); // 직접 적 공격 표시가 없는 피해의 방어도만 적용 확인
        int health = ally.CurrentHealth; // 직접 명중 전 기준 체력
        var predicted = ally.PreviewDamage(32, BattleDamageType.Magical, enemy, enemyDirectAttack: true); // 네 경감 비율과 방어도를 반영한 예고
        Assert.AreEqual(2, predicted.FinalDamage); // 비율 네 개를 곱한 뒤 한 번 내리고 고정 피해 경감 적용
        Assert.AreEqual(1, predicted.AppliedDamage); // 기존 방어도 한 점의 마지막 흡수 확인
        var applied = ally.TakeDamage(32, BattleDamageType.Magical, enemy, enemyDirectAttack: true); // 예고와 같은 실제 직접 명중
        Assert.AreEqual(predicted.AppliedDamage, applied.AppliedDamage); // 예고와 실제 피해 일치 확인
        Assert.AreEqual(health - 1, ally.CurrentHealth); // 합성 피해만큼 체력 손실 확인
        Assert.AreEqual(8, ally.PreviewDamage(20, BattleDamageType.Physical, enemyDirectAttack: true).AppliedDamage); // 공격자를 생략한 다음 예고에서도 집중 방어 유지 확인
    } // 집중 방어 검사 종료

    [TestCase(19, BattleStatusEffectApplyResult.BlockedByImmunity)] // 저항 성공 상한 난수
    [TestCase(20, BattleStatusEffectApplyResult.Applied)] // 저항 실패 하한 난수
    public void BleedResistanceOnlyRollsForIncomingEnemyBleed(int roll, BattleStatusEffectApplyResult expected) // 적 출혈 시도에서만 이십 퍼센트 판정 확인
    { // 출혈 저항 검사 시작
        ally.ApplyStatusEffect(BattleStatusEffectType.BleedResistance, 20, 2, 1); // 전열 보호의 출혈 저항 상태 부여
        int seed = FindSeed(value => value == roll); // 성공과 실패 경계의 고정 게임 난수
        Random.InitState(seed); // 실제 상태 적용에 같은 난수 사용
        int next = ReadSecondRoll(); // 저항 판정 뒤 기대하는 게임 난수
        Random.InitState(seed); // 실제 저항 판정의 난수 시작점 복원
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 3, 1, 1, enemy)); // 적 화상 시도의 저항 우회 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 1, 1, ally)); // 아군 출혈 시도의 저항 우회 확인
        Assert.IsTrue(ally.RemoveStatusEffect(BattleStatusEffectType.Bleed)); // 적의 출혈 상태를 독립 비교하기 위한 제거
        Assert.AreEqual(expected, ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 1, 1, enemy)); // 적 출혈 요청의 이십 퍼센트 경계 확인
        Assert.AreEqual(expected == BattleStatusEffectApplyResult.Applied, ally.GetStatusEffect(BattleStatusEffectType.Bleed) != null); // 저항 결과와 실제 출혈 상태 일치 확인
        Assert.AreEqual(next, Random.Range(0, 100)); // 유효한 적 출혈에서만 난수 한 번 소비 확인
    } // 출혈 저항 검사 종료

    [Test] // 일반 상태 면역의 판정 우선순위 확인
    public void FullImmunityBlocksEnemyBleedWithoutConsumingResistanceRoll() // 면역 차단과 기존 게임 난수 보존 확인
    { // 면역 우선순위 검사 시작
        ally.ApplyStatusEffect(BattleStatusEffectType.BleedResistance, 20, 2, 1); // 확률 저항을 함께 가진 아군 상태
        ally.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 출혈을 언제나 막는 일반 면역
        int seed = FindSeed(value => value >= 20); // 저항만 있을 때 적용될 출혈 난수
        Random.InitState(seed); // 면역 확인에 사용할 난수 지정
        Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 1, 1, enemy)); // 기존 확정 면역의 우선 발동 확인
        int actualNext = Random.Range(0, 100); // 면역 시도 뒤 실제 첫 난수
        Random.InitState(seed); // 비교할 원래 난수 재생
        Assert.AreEqual(Random.Range(0, 100), actualNext); // 확정 차단 중 저항 난수 미소비 확인
    } // 면역 우선순위 검사 종료

    [Test] // 방어와 경감 후 정확히 체력 한 점이 되는 피격 검사
    public void UndyingGuardWaitsForPositiveThresholdDamageAfterMitigation() // 완전 방어와 추가 피해 경감 뒤 발동 판정 확인
    { // 불굴 발동 기준 검사 시작
        ally.ApplyPersistentHealth(10); // 임계 피해 비교를 위한 체력 지정
        ally.ApplyStatusEffect(BattleStatusEffectType.UndyingGuard, 1, 2, 1); // 두 턴 안에 한 번 버티는 상태 적용
        ally.GainBlock(3); // 한 타격을 전부 흡수하는 방어도
        Assert.AreEqual(0, ally.TakeDamage(3, BattleDamageType.None, enemy).AppliedDamage); // 실제 체력 피해 없는 방어 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 완전 방어 시 불굴 기회 보존
        Assert.AreEqual(0, ally.TakeDamage(30, BattleDamageType.None, enemy, preventHealthDamage: () => true).AppliedDamage); // 먼저 발동한 체력 피해 무효 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 기존 피해 무효 뒤 불굴 보존
        Assert.AreEqual(8, ally.TakeDamage(9, BattleDamageType.None, enemy, reduceHealthDamage: incoming => incoming - 1).AppliedDamage); // 남은 체력이 두 점이면 상태 미발동
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 정확히 두 점인 경우 불굴 보존
        Assert.AreEqual(1, ally.TakeDamage(1, BattleDamageType.None, enemy).AppliedDamage); // 체력이 정확히 한 점으로 감소하는 피해
        Assert.AreEqual(1, ally.CurrentHealth); // 한 점의 생존 체력 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 정확한 한 점 임계 피해에서도 효과 소모 확인
        Assert.AreEqual(1, ApplySharedDamage(10)); // 실제 분담 공격으로 소모된 상태의 추가 피격 확인
        Assert.IsTrue(ally.IsDead); // 전투당 재생성 없이 다음 처치 허용 확인
    } // 불굴 발동 기준 검사 종료

    [TestCase(false)] // 방어 계산이 있는 일반 피해
    [TestCase(true)] // 출혈과 분담의 방어도 없는 피해
    public void UndyingGuardLeavesOneHealthAgainstLethalDamage(bool unblocked) // 일반 및 방어 불가 피해의 생존 확인
    { // 치사 피해 검사 시작
        ally.ApplyPersistentHealth(5); // 사망 피해와 실제 손실을 계산할 체력 지정
        ally.ApplyStatusEffect(BattleStatusEffectType.UndyingGuard, 1, 2, 1); // 불굴의 치사 피해 한 번 보호
        Assert.IsTrue(ally.TrySpendHealth(1)); // 피해와 구분하는 카드 체력 비용 지불
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 체력 비용으로 상태 미소모 확인
        if (unblocked) // 피해 분담이 대상의 방어도를 거치지 않는 경로 확인
        { // 대상의 기존 방어도 준비 시작
            ally.GainBlock(50); // 분담 경로에서는 소모하지 않을 충분한 방어도
        } // 대상의 기존 방어도 준비 종료
        int loss = unblocked ? ApplySharedDamage(30) : ally.TakeDamage(30, BattleDamageType.None, enemy).AppliedDamage; // 공개 전투 사용 경로별 실제 치사 피해 적용
        Assert.AreEqual(3, loss); // 남은 네 체력에서 한 점을 남긴 실제 피해 확인
        Assert.AreEqual(unblocked ? 50 : 0, ally.CurrentBlock); // 분담 피해의 기존 대상 방어도 우회 확인
        Assert.AreEqual(1, ally.CurrentHealth); // 치사 피해 뒤 최소 체력 확인
        Assert.IsFalse(ally.IsDead); // 적용 결과의 생존 상태 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.UndyingGuard)); // 치사 피해 방어 뒤 사용한 상태 제거 확인
    } // 치사 피해 검사 종료

    private int ApplySharedDamage(int rawAttack) // 공개 피해 처리에 기존 동료 피해 분담 연결
    { // 실제 분담 피해의 측정 시작
        var attackTarget = BattleUnitRuntime.CreateAlly(allySource); // 원래 공격을 맞아 분담자에게 전달할 생존 아군
        int previousHealth = ally.CurrentHealth; // 분담 전 보호 대상의 실제 체력
        attackTarget.TakeDamage(rawAttack, BattleDamageType.None, enemy, sharingAlly: ally); // 정식 조우의 피해 분담이 내부 방어 불가 경로를 호출
        return previousHealth - ally.CurrentHealth; // 내부 분담으로 감소한 실제 보호 대상 체력 반환
    } // 실제 분담 피해의 측정 종료

    private static int FindSeed(Func<int, bool> matches) // 기존 게임 난수의 지정 값 탐색
    { // 난수 시드 탐색 시작
        for (int seed = 0; seed < 10000; seed++) // 경계 난수의 재현 가능한 후보 순회
        { // 후보 검사 시작
            Random.InitState(seed); // 현재 후보 시드 초기화
            if (matches(Random.Range(0, 100))) // 원하는 첫 난수와 일치 확인
            { // 시드 확정 시작
                return seed; // 게임 난수 재현에 사용할 시드 반환
            } // 시드 확정 종료
        } // 후보 검사 종료
        throw new InvalidOperationException("출혈 저항 검사 난수 누락"); // 지정 값의 재현 실패 표시
    } // 난수 시드 탐색 종료

    private static int ReadSecondRoll() // 한 번 소비한 뒤의 게임 난수 조회
    { // 두 번째 난수 확인 시작
        Random.Range(0, 100); // 출혈 저항이 사용할 첫 난수 소비
        return Random.Range(0, 100); // 판정 이후 비교할 두 번째 난수 반환
    } // 두 번째 난수 확인 종료
} // 방패 상태 검사 종료
