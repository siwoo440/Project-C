using System.Collections; // 실제 전투 검사 모드 전환
using System.Reflection; // 검사 어셈블리에서 무방어 실제 피해 호출
using NUnit.Framework; // 유물의 전투 결과 검증
using UnityEngine; // 게임 유물 원본 조회
using UnityEngine.TestTools; // 플레이 모드의 실제 턴 검사
using Random = UnityEngine.Random; // 실제 유물 확률 재현
using static WinterfallSwordCombatTests; // 기존 파티와 카드 입력 검사 환경 재사용

public sealed class WinterfallDamageAndFearRelicTests // 체력 피해와 공포 및 출혈 유물의 실제 전투 회귀 검사
{ // 새 유물 회귀 검사 시작
    [UnityTest] // 마법 체력 피해와 실제 전투 중계기 검사
    public IEnumerator BlackStardustRollsOnlyForLivingEnemiesAfterAlliedMagicHealthDamage() // 실제 피해 출처와 체력 감소 조건 검증
    { // 검은 별가루의 플레이 검사 시작
        yield return new EnterPlayMode(); // 전투 자원과 공용 이벤트가 연결되는 모드 진입
        VerifyMagicalHit(); // 실제 피해와 확률 경계를 검사
        yield return new ExitPlayMode(); // 격리 검사 모드 종료
    } // 검은 별가루의 플레이 검사 종료

    private static void VerifyMagicalHit() // 마법 체력 피해마다 실제 난수와 정신력 변화 확인
    { // 실제 피해 이벤트 중계 준비 시작
        var inventory = Acquire("BlackStardust"); // 게임에서 사용하는 검은 별가루 원본 획득
        using var fight = new Fight("COUNTERATTACK"); // 실제 체력과 피해 신호를 생성할 전투 구성
        using var dispatcher = new BattleEventDispatcher(); // 공용 이벤트의 전투별 배포기 준비
        using var signals = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 실제 피해 완료 이벤트 중계 연결
        using var effects = new BattleRelicEffectController(dispatcher, inventory.Inventory, inventory.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 실제 회차 유물의 자동 발동 처리
        var source = fight.Allies[0]; // 마법 피해의 실제 아군 출처
        var target = fight.Enemies[0]; // 체력이 남는 반복 피격 대상
        var dying = fight.Enemies[1]; // 실제 치명 피해로 생존하지 않는 적 대조
        int relicDelta = 0; // 이 유물로 바뀐 정신력만 따로 기록
        target.MentalChanged += (_, result) => relicDelta += result.Reason == BattleMentalChangeReason.RelicEffect ? result.AppliedDelta : 0; // 기존 피격 정신력과 유물 수치의 분리 기록
        int success = SeedForFirstRoll(19); // 실제 이십 퍼센트 판정의 마지막 성공 난수
        int failure = SeedForFirstRoll(20); // 실제 이십 퍼센트 판정의 첫 실패 난수
        Random.InitState(success); // 분기 전 같은 전역 난수 상태 설정
        var unchanged = Random.state; // 무관한 피해 전의 전역 난수 보관
        target.GainBlock(8); // 마법 피해를 완전히 흡수할 방어도 준비
        Assert.AreEqual(0, target.TakeDamage(8, BattleDamageType.Magical, source).AppliedDamage); // 방어도만 쓰는 실제 아군 마법 피해
        target.TakeDamage(5, BattleDamageType.Physical, source); // 실제 체력 손실이 있어도 물리 피해 제외
        target.TakeDamage(5, BattleDamageType.Magical); // 출처 없는 실제 마법 피해 제외
        target.TakeDamage(5, BattleDamageType.Magical, fight.Enemies[2]); // 아군이 아닌 공격자의 마법 피해 제외
        source.TakeDamage(5, BattleDamageType.Magical, source); // 아군 대상에게 적용된 마법 피해 제외
        dying.TakeDamage(dying.CurrentHealth - 1, BattleDamageType.None); // 출처 없는 실제 피해로 적의 체력을 한 점까지 낮춤
        Assert.AreEqual(1, dying.CurrentHealth); // 마지막 마법 타격으로 쓰러질 적 체력 확인
        dying.TakeDamage(5, BattleDamageType.Magical, source); // 실제 치명 타격 시 생존 정신력 대상 없음
        Assert.True(dying.IsDead); // 실제로 쓰러진 적이 발동과 난수를 소비하지 않음을 확인
        Assert.AreEqual(unchanged, Random.state); // 모든 유효하지 않은 피해가 전역 난수를 보존했는지 확인
        Assert.AreEqual(0, relicDelta); // 잘못된 통계 반응의 유물 정신력 변화 없음 확인
        target.TakeDamage(5, BattleDamageType.Magical, source); // 성공 경계의 실제 양수 마법 체력 피해
        Assert.AreEqual(-2, relicDelta); // 생존 적에게 검은 별가루 정신력 감소량 두 점 확인
        Random.InitState(failure); // 실패 경계의 정확한 엔진 난수 설정
        target.TakeDamage(5, BattleDamageType.Magical, source); // 실패 경계의 실제 마법 체력 피해
        Assert.AreEqual(-2, relicDelta); // 용인 범위 바로 밖의 난수가 감소시키지 않는지 확인
        Random.InitState(success); // 같은 턴의 두 번째 성공 조건 복원
        target.TakeDamage(5, BattleDamageType.Magical, source); // 후속 실제 마법 체력 피해의 독립 적용
        Assert.AreEqual(-4, relicDelta); // 턴당 한 번 제한 없이 명중마다 적용 확인
    } // 실제 피해 이벤트 중계 준비 종료

    [UnityTest] // 원래 공격자와 정수 피해 계산의 공통 경로 검사
    public IEnumerator HeartShardBoostsOnlyCreditedOutgoingDamageAtHalfHealth() // 절반 체력 경계와 기본·무방어 피해의 정확한 적용
    { // 심장파편 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 자원과 유물 초기화
        VerifyHeartShard(); // 피해 예고와 실제 출처의 동일 조건 확인
        yield return new ExitPlayMode(); // 플레이 모드 검사 종료
    } // 심장파편 플레이 검사 종료

    private static void VerifyHeartShard() // 실제 공격자의 체력 비율과 고정 피해 분기 대조
    { // 어긋난 심장파편 피해 검사 시작
        Acquire("MisalignedHeartShard"); // 정식 어긋난 심장파편 보유
        using var fight = new Fight("COUNTERATTACK"); // 알려진 적과 아군으로 실제 전투 구성
        var source = fight.Allies[0]; // 실제 아군 피해 출처 선택
        var target = fight.Enemies[0]; // 기존 방어와 약점 없는 비교 적 선택
        int half = source.MaxHealth / 2; // 정수로 실제 최대 체력 절반 이하인 최대값 조회
        source.ApplyPersistentHealth(half + 1); // 절반을 초과한 경계로 공격자 상태 설정
        Assert.AreEqual(19, target.PreviewDamage(19, BattleDamageType.Physical, source).RawDamage); // 자산 보유 중에도 절반 위의 순수 예고 유지 확인
        source.ApplyPersistentHealth(half); // 현재 체력이 최대 체력의 절반 이하로 낮은 실제 공격자
        Assert.AreEqual(21, target.PreviewDamage(19, BattleDamageType.Physical, source).RawDamage); // 순수 알려진 출처 예고에도 십오 퍼센트 내림 가산
        target.GainBlock(4); // 가산 피해가 기존 방어도 계산 전에 적용되는지 확인할 방어도
        var applied = target.TakeDamage(19, BattleDamageType.Physical, source); // 실제 공격자의 일반 타격 계산 실행
        Assert.AreEqual(21, applied.RawDamage); // 실제 일반 피해가 예고 원본의 한 번만 증가 확인
        Assert.AreEqual(17, applied.AppliedDamage); // 증가 원본에서 실제 방어도 네 점 감소 확인
        Assert.AreEqual(19, target.PreviewDamage(19, BattleDamageType.None).RawDamage); // 출혈 같은 비지정 출처의 피해 예고 보존
        Assert.AreEqual(19, target.PreviewDamage(19, BattleDamageType.Magical, fight.Enemies[2]).RawDamage); // 적의 공격 출처 효과 제외 확인
        Assert.AreEqual(21, InvokeUnblockedDamage(target, source, 19)); // 아군 출처가 있는 실제 반사 무방어 피해도 같은 한 번의 가산
        source.ApplyPersistentHealth(0); // 체력은 낮지만 행동할 수 없는 사망 출처 준비
        Assert.AreEqual(19, target.PreviewDamage(19, BattleDamageType.None, source).RawDamage); // 죽은 출처의 피해 수치 변화 없음 확인
    } // 어긋난 심장파편 피해 검사 종료

    [UnityTest] // 실제 적의 행동 예고와 양 진영 턴 종료 검사
    public IEnumerator FearChangesIntentWhileActiveAndBleedRaisesOnlyEnemyTicks() // 공포 중 공격력과 적 출혈 피해만 증가 확인
    { // 결정종과 균열섬유 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 상태 적용과 턴 관리자 접근
        VerifyFearAndBleed(); // 적 예고와 실제 타격 및 양쪽 출혈 정산 확인
        yield return new ExitPlayMode(); // 상태 검사 이후 에디터 모드 복원
    } // 결정종과 균열섬유 플레이 검사 종료

    private static void VerifyFearAndBleed() // 활성 상태의 공격량과 정상 출혈 수명 확인
    { // 공포와 적 출혈 검사 시작
        Acquire("SilenceCrystalBell"); // 실제 침묵 결정종 유물 원본 획득
        Acquire("RedRiftFiber"); // 실제 붉은 균열섬유 유물 원본 획득
        using var fight = new Fight("COUNTERATTACK"); // 적 행동과 양 진영 턴 종료가 연결된 전투 시작
        var ally = fight.Allies[0]; // 양쪽 출혈 비교의 실제 아군 대상
        var enemy = fight.Enemies[0]; // 공포와 적 출혈을 받을 실제 적
        var action = new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.None, 12, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "결정종 검사", 0, 1, 0, 0, percentRoll: () => 0); // 실제 공격 예고와 실행을 함께 쓰는 적 행동
        int before = action.AttackAmount; // 평소에 표시될 공격 원본의 정확한 기준 수치
        enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1, ally); // 적을 실제 활성 공포로 전환
        Assert.AreEqual(before - 1, action.AttackAmount); // 공포 중 적 원본 공격력이 한 점 감소 확인
        var preview = action.PreviewDamage(); // 실제 유저가 보는 공포 중 적 공격 피해 예고
        Assert.AreEqual(before - 1, preview.RawDamage); // 공포 공격력 약화가 예고 원본까지 반영되는지 확인
        Assert.AreEqual(preview.FinalDamage, action.Execute().DamageResult.FinalDamage); // 공포 중 실제 타격과 예고가 일치하는지 확인
        enemy.RemoveStatusEffect(BattleStatusEffectType.Fear); // 정상 정화 규칙으로 공포 해제
        Assert.AreEqual(before, action.AttackAmount); // 해제 뒤 별도 감소를 계속 적용하지 않는지 확인
        enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1, ally); // 사라지는 기간 상태의 끝 경계 준비
        enemy.GetStatusEffect(BattleStatusEffectType.Fear).AdvanceDuration(); // 일반 정리 직전의 남은 기간 영 상태 표현
        Assert.AreEqual(before, action.AttackAmount); // 객체가 남아 있어도 만료 공포는 감소를 발동하지 않음 확인
        int allyHealth = ally.CurrentHealth; // 출혈 피해가 적용되기 전 아군의 실제 체력 기준
        int enemyHealth = enemy.CurrentHealth; // 출혈 피해가 적용되기 전 적의 실제 체력 기준
        ally.GainBlock(8); // 원래 출혈이 방어도를 우회하는지 확인할 보호 수치
        enemy.GainBlock(8); // 적의 추가 출혈 피해도 같은 무방어 경로인지 확인할 보호 수치
        int enemyBlockAtTick = -1; // 다음 라운드 정리 이전의 적 방어도 보관
        enemy.DamageTaken += (_, _) => enemyBlockAtTick = enemy.CurrentBlock; // 출혈이 실제로 적용되는 시점의 방어도 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 2, 1); // 아군의 비교 출혈 원래 수치 준비
        enemy.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 2, 1); // 적의 증가 여부를 볼 같은 출혈 준비
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 실제 턴 종료에서 원래 출혈 실행
        Assert.AreEqual(allyHealth - 3, ally.CurrentHealth); // 아군의 출혈이 증가하지 않는지 확인
        Assert.AreEqual(8, ally.CurrentBlock); // 아군 출혈의 기존 방어도 우회 보존 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 실제 턴 종료에서 추가 출혈 실행
        Assert.AreEqual(enemyHealth - 4, enemy.CurrentHealth); // 적 출혈의 기본 세 점에 한 점 가산 확인
        Assert.AreEqual(8, enemyBlockAtTick); // 새 라운드 정리 직전 적 출혈이 방어도를 소모하지 않는지 확인
        var bleed = enemy.GetStatusEffect(BattleStatusEffectType.Bleed); // 적에게 남은 실제 출혈 상태 확인
        Assert.AreEqual(1, bleed.RemainingTurns); // 추가 피해로 기간이 더 줄지 않는지 확인
        Assert.AreEqual(3, bleed.EffectiveValue); // 턴 마무리 뒤 상태의 원래 출혈 수치 보존 확인
    } // 공포와 적 출혈 검사 종료

    private static int InvokeUnblockedDamage(BattleUnitRuntime target, BattleUnitRuntime source, int amount) // 실제 반사와 출혈이 사용하는 무방어 처리 호출
    { // 내부 피해 경로 실행 시작
        var method = typeof(BattleUnitRuntime).GetMethod("TakeUnblockedDamage", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 무방어 피해 처리의 내부 진입점 찾기
        Assert.IsNotNull(method); // 해당 공격 경로에 실제로 접근 가능한지 확인
        return (int)method.Invoke(target, new object[] { amount, BattleDamageType.None, source, false }); // 아군이 직접 준 무방어 피해 결과 반환
    } // 내부 피해 경로 실행 종료

    private static RelicRunManager Acquire(string key) // 배포용 시트 자산을 실제 전투용 보유 유물로 등록
    { // 실제 유물 획득 경로 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 현재 배포할 실제 유물 원본 조회
        Assert.IsNotNull(relic, key); // 유물 리소스의 등록 주소 확인
        var inventory = RelicRunManager.EnsureInstance(); // 실제 탐사 회차 유물 관리자 준비
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 정상 획득에 성공했는지 확인
        return inventory; // 전투 이벤트에 연결할 실제 보유 목록 반환
    } // 실제 유물 획득 경로 종료

    private static int SeedForFirstRoll(int expected) // 실제 게임 난수의 지정된 첫 값 재현
    { // 재현 시드 검색 시작
        for (int seed = 0; seed < 5000; seed++) // 유한한 검사 시드 후보 순회
        { // 각 난수 후보 확인 시작
            Random.InitState(seed); // 현재 후보의 실제 게임 난수 상태 준비
            if (Random.Range(0, 100) == expected) // 유물이 읽을 정확한 첫 난수 비교
            { // 기대 경계 시드 확인 시작
                return seed; // 다음 실제 피해에 사용할 동일 사전 상태 반환
            } // 기대 경계 시드 확인 종료
        } // 각 난수 후보 확인 종료
        Assert.Fail("기대한 유물 확률 경계 시드 미발견"); // 엔진 난수에서 실패한 검사 전제 보고
        return 0; // 단언 실패 이후 기본 반환 계약
    } // 재현 시드 검색 종료
} // 새 유물 회귀 검사 종료
