using System.Collections; // 실제 카드와 턴 처리 대기
using System.Linq; // 실제 지정 대상 화면 조회
using System.Reflection; // 실제 분담과 무방어 피해 접근
using NUnit.Framework; // 확률과 피해 감소 비교
using UnityEngine; // 실제 유물과 전투 환경 조회
using UnityEngine.TestTools; // 플레이 모드 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드와 턴 입력 검사 환경
using Random = UnityEngine.Random; // 공포 피해 확률의 고정 시드 관리

public sealed class WinterfallIncomingRelicTests // 공포와 화상 및 저체력 피해 유물 검사
{ // 받는 상태와 피해 검사 시작
    [UnityTest] // 정상 면역 이후의 실제 공포 확률 검사
    public IEnumerator QuietBellCordChecksValidIncomingFearOnce() // 유효 대상과 확률 경계 확인
    { // 무음 방울끈 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 환경 진입
        VerifyFear(); // 플레이 모드에서 상태 적용과 난수 검사
        yield return new ExitPlayMode(); // 독립 전투 환경 종료
    } // 무음 방울끈 검사 종료

    private static void VerifyFear() // 실제 공포 적용의 면역과 추가 확률 확인
    { // 공포 확률 확인 시작
        using var fight = new Fight("HEALING_PRAYER"); // 이미 등록된 유효 전투 원본 준비
        var relic = Resources.Load<RelicData>("Relics/QuietBellCord"); // 실제 무음 방울끈 원본 조회
        Assert.IsNotNull(relic); // 정식 유물 자산 존재 확인
        Assert.AreEqual(30, relic.TriggerChancePercent); // 정식 공포 취소 확률 삼십 확인
        Assert.AreNotEqual(RelicTriggerType.StatusApplied, relic.TriggerType); // 실제 상태 알림의 불필요한 두 번째 추첨 차단
        var inventory = RelicRunManager.EnsureInstance(); // 탐사 유물 보유 상태 조회
        inventory.TryAcquire(relic); // 실제 유물 효과 장착
        var previousRandom = Random.state; // 다른 검사와 분리된 난수 기억
        try // 유효한 부여 전후의 난수 확인
        { // 실제 공포 적용 비교 시작
            var ally = fight.Allies[0]; // 정상 확률 판정의 생존 아군
            var immune = fight.Allies[1]; // 기존 완전 면역으로 건너뛸 동료
            var fallen = fight.Allies[2]; // 전투불능으로 상태를 받지 않는 동료
            var enemy = fight.Enemies[0]; // 적의 실제 공포 출처
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 기존 공포 완전 면역 설정
            fallen.ApplyPersistentHealth(0); // 실제 전투불능 아군 설정
            int passing = SeedWithRoll(29); // 삼십 퍼센트의 마지막 성공값
            int failing = SeedWithRoll(30); // 삼십 퍼센트의 첫 실패값
            Random.InitState(passing); // 무효 적용에 남겨 둘 첫 난수
            var before = Random.state; // 실제로 소비하지 않아야 할 난수 상태
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, immune.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1, enemy)); // 기존 면역으로 먼저 거부
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, fallen.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1, enemy)); // 전투불능인 대상에 적용 불가
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 0, 1, enemy)); // 실제 지속이 없는 요청 거부
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1, ally)); // 적이 받는 정상 공포에 유물 미적용
            Assert.AreEqual(before, Random.state); // 대상이나 원래 면역에 어긋난 요청의 난수 보존
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1)); // 출처 없는 유효한 환경 공포도 정상 취소
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Fear)); // 취소된 공포 미부여 확인
            Random.InitState(failing); // 실패 경계의 실제 공포 난수
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1, enemy)); // 확률 실패 시 실제 적 공포가 정상 적용
            Random.InitState(passing); // 이미 남은 상태에 성공 경계 다시 적용
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 5, 1, enemy)); // 취소된 갱신에서 기존 공포 유지
            Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.Fear).RemainingTurns); // 기존 공포를 새 기간으로 늘리지 않음 확인
        } // 실제 공포 적용 비교 종료
        finally // 게임 난수와 유물 복구
        { // 테스트 상태 복구 시작
            Random.state = previousRandom; // 원래 전투 난수 복원
            inventory.TryRemove(relic.RelicId); // 이번 검사 유물 제거
        } // 테스트 상태 복구 종료
    } // 공포 확률 확인 종료

    [UnityTest] // 실제 진영 턴 종료의 화상 피해 검사
    public IEnumerator HalfBurnedCharmReducesOnlyAllyBurnBeforeBlock() // 방어도와 양수 피해 무효 확인
    { // 반쯤 탄 부적 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 환경 진입
        VerifyBurn(); // 실제 아군과 적 턴에 발생한 화상 비교
        yield return new ExitPlayMode(); // 독립 검사 종료
    } // 반쯤 탄 부적 검사 종료

    private static void VerifyBurn() // 정상 턴 종료 화상과 다른 피해 구별
    { // 화상 피해 검사 시작
        using var fight = new Fight("HEALING_PRAYER"); // 실제 전투 턴과 피해 대상 준비
        var charm = Resources.Load<RelicData>("Relics/HalfBurnedCharm"); // 정식 반쯤 탄 부적 원본 조회
        Assert.IsNotNull(charm); // 정식 유물의 실제 등록 확인
        var relics = RelicRunManager.EnsureInstance(); // 회차 유물 보유 관리자 조회
        relics.TryAcquire(charm); // 실제 화상 경감 유물 장착
        try // 턴 종료 피해와 방어도 확인
        { // 실제 지속 화상 검사 시작
            var guarded = fight.Allies[0]; // 감소한 화상을 방어도로 흡수할 아군
            var zero = fight.Allies[1]; // 양수 피해가 없어 무효를 유지할 아군
            var enemy = fight.Enemies[0]; // 화상 감소 대상이 아닌 적
            guarded.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, enemy); // 감소 뒤 한 점 남을 아군 화상
            guarded.GainBlock(1); // 경감한 화상 한 점을 막을 아군 방어도
            zero.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 1, 1, enemy); // 감소 뒤 영이 될 아군 화상
            zero.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 양수 피해가 없으면 유지할 보호
            enemy.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, guarded); // 같은 피해량의 적 화상 대조
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 종료 화상 발생
            Assert.AreEqual(guarded.MaxHealth, guarded.CurrentHealth); // 두 점 중 한 점 경감 뒤 방어도 흡수 확인
            Assert.AreEqual(0, guarded.CurrentBlock); // 경감한 한 점만 흡수한 실제 방어도 확인
            Assert.AreEqual(zero.MaxHealth, zero.CurrentHealth); // 한 점 이하 화상의 영 하한 확인
            Assert.IsNotNull(zero.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 원본 화상이 영으로 경감되어 피해 무효 미소모 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 적의 턴 종료 화상 발생
            Assert.AreEqual(enemy.MaxHealth - 2, enemy.CurrentHealth); // 적 화상의 원래 두 점 피해 보존 확인
            Assert.AreEqual(1, guarded.TakeDamage(1, BattleDamageType.None).AppliedDamage); // 다른 속성 없는 피해의 원래 체력 손실 확인
        } // 실제 지속 화상 검사 종료
        finally // 유물 보유 검사 정리
        { // 유물 제거 시작
            relics.TryRemove(charm.RelicId); // 이번 검사 화상 유물 제거
        } // 유물 제거 종료
    } // 화상 피해 검사 종료

    [UnityTest] // 저체력 피해 감소의 실제 예고와 생존 보호 비교
    public IEnumerator LightlessNecklaceMatchesForecastAndDoesNotSpendUnusedProtection() // 실제 방어와 분담 및 수호자 전환 확인
    { // 빛 없는 목걸이 검사 시작
        yield return new EnterPlayMode(); // 실제 카드로 수호 대상을 준비하는 환경
        yield return VerifyNecklace(); // 플레이 모드의 예고와 실제 피해 비교
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 빛 없는 목걸이 검사 종료

    private static IEnumerator VerifyNecklace() // 현재 체력 경계와 기존 보호 처리 순서 검증
    { // 실제 저체력 피해 검사 시작
        using var fight = new Fight("GUARDIAN_HEART"); // 수호 대체를 연결할 실제 카드 원본
        var necklace = Resources.Load<RelicData>("Relics/LightlessNecklace"); // 정식 빛 없는 목걸이 원본 조회
        Assert.IsNotNull(necklace); // 실제 유물 등록 확인
        Assert.AreEqual(2, necklace.EffectValue); // 실제 감소량 두 점 확인
        var relics = RelicRunManager.EnsureInstance(); // 회차 유물 관리자
        relics.TryAcquire(necklace); // 실제 저체력 피해 감소 효과 장착
        try // 피해 계산과 수호 카드의 실제 적용 비교
        { // 예고와 실제 보호 확인 시작
            var protector = fight.Allies[0]; // 저체력 효과가 적용되지 않을 대신 맞는 시전자
            var target = fight.Allies[1]; // 경계에서 경감할 실제 생존 아군
            var primary = fight.Allies[2]; // 다른 피격의 실제 분담 원래 대상
            var enemy = fight.Enemies[0]; // 실제 공격 출처
            SetHealth(target, 30, 100); // 최대 체력의 정확한 삼십 퍼센트 경계
            primary.ModifyMaxHealth(100 - primary.MaxHealth); // 분담 공격에서 충분한 원래 대상 체력 확보
            primary.ApplyPersistentHealth(100); // 원래 피격자에 부착되지 않는 저체력 경감 확보
            target.ApplyStatusEffect(BattleStatusEffectType.GuardianCircle, 50, 1, 1); // 기존 백분율 경감 대조
            target.GainBlock(5); // 감소한 공격을 실제 흡수할 방어도
            var preview = target.PreviewDamage(20, BattleDamageType.None, enemy, enemyDirectAttack: true); // 적 직접 피해의 기존 예고 경로
            var actual = target.TakeDamage(20, BattleDamageType.None, enemy, enemyDirectAttack: true); // 같은 원본 직접 공격 실제 적용
            Assert.AreEqual(8, preview.FinalDamage); // 비율 절반 뒤 목걸이 두 점 적용 확인
            Assert.AreEqual(5, preview.AbsorbedDamage); // 목걸이 처리 뒤 방어도 우선 흡수 확인
            Assert.AreEqual(preview.FinalDamage, actual.FinalDamage); // 예고의 최종 피해와 실제 결과 일치
            Assert.AreEqual(preview.AppliedDamage, actual.AppliedDamage); // 방어도 후 실제 체력 손실 일치
            Assert.IsTrue(target.TrySpendHealth(2)); // 같은 저체력에서 기존 체력 비용 지불
            Assert.AreEqual(25, target.CurrentHealth); // 비용 경로의 두 점 차감 유지
            target.RemoveStatusEffect(BattleStatusEffectType.GuardianCircle); // 이어지는 고정 피해의 독립 비교 준비
            target.ApplyPersistentHealth(31); // 삼십 퍼센트 바로 위의 기존 체력
            Assert.AreEqual(2, target.TakeDamage(2, BattleDamageType.None).AppliedDamage); // 타격으로 경계를 넘을 때 먼저 경감하지 않음 확인
            Assert.AreEqual(0, target.TakeDamage(2, BattleDamageType.None).AppliedDamage); // 이미 기준 이하에서는 두 점 피해를 영으로 감소
            var unblocked = typeof(BattleUnitRuntime).GetMethod("TakeUnblockedDamage", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 출혈이 사용하는 무방어 피해 경로
            Assert.IsNotNull(unblocked); // 실제 무방어 처리 메서드 확인
            target.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 원본 피해의 고정 경감 뒤 판정할 피해 무효
            Assert.AreEqual(0, unblocked.Invoke(target, new object[] { 2, BattleDamageType.None, enemy, false })); // 방어도 없는 두 점 피해의 완전 경감
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 이미 영인 무방어 피해에서 무효 보호 보존
            Assert.AreEqual(0, unblocked.Invoke(target, new object[] { 3, BattleDamageType.None, enemy, false })); // 감소 이후에도 남은 무방어 피해의 무효 처리
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 양수 무방어 피해에서만 무효 한 번 소모
            int sharedHealth = target.CurrentHealth; // 분담 전 실제 저체력 동료의 체력
            var attack = new BattleEnemyAction(enemy, primary, EnemyActionType.Attack, BattleDamageType.None, 20, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "목걸이 검사 공격", 1, 1, 1, 0, percentRoll: () => 0); // 실제로 분담할 적 직접 공격
            var share = typeof(BattleEnemyAction).GetMethod("SetDamageShare", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 적 공격의 분담 연결 경로 조회
            Assert.IsNotNull(share); // 실제 전투의 분담 지정 메서드 확인
            Assert.IsTrue((bool)share.Invoke(attack, new object[] { target })); // 저체력인 실제 동료에게 피해 분담 배정
            int sharedPreview = attack.PreviewSharedDamage(); // 방어도를 거치지 않는 분담 대상의 예고
            Assert.AreEqual(attack.AttackAmount / 2 - 2, sharedPreview); // 현재 적의 피해 절반에서 분담자의 목걸이 경감 적용
            attack.Execute(); // 실제 분담과 주 대상 피해 처리
            Assert.AreEqual(sharedPreview, sharedHealth - target.CurrentHealth); // 분담 예고와 실제 체력 감소 일치
            SetHealth(target, 3, 10); // 세 점의 정확한 저체력 생존 경계
            SetHealth(protector, 6, 10); // 경감 자격이 없는 수호자의 체력 설정
            var view = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(candidate => candidate.RuntimeUnit == target); // 보호받을 생존 아군의 실제 카드 입력 화면
            yield return WinterfallCommonCardExpansionTests.Play(fight.View("GUARDIAN_HEART").RuntimeCard.SourceData, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 기존 수호 대체 카드 효과 정상 적용
            target.ApplyStatusEffect(BattleStatusEffectType.DefeatWard, 1, 1, 1); // 치명 피해만 막는 기존 성배 효과 연결
            Assert.AreEqual(2, target.TakeDamage(4, BattleDamageType.None, enemy, enemyDirectAttack: true).AppliedDamage); // 감소 뒤 치명적이지 않게 된 첫 타격 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 치명성 감소로 성배 보호 미소모 확인
            Assert.AreEqual(0, target.TakeDamage(3, BattleDamageType.None, enemy, enemyDirectAttack: true).AppliedDamage); // 두 번째 실제 치명 타격의 성배 발동 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.GuardianHeart)); // 성배가 막은 타격의 수호 대체 미소모 확인
            Assert.AreEqual(3, target.TakeDamage(3, BattleDamageType.None, enemy, enemyDirectAttack: true).AppliedDamage); // 목걸이 대상이 아닌 수호자의 원본 피해 적용
            Assert.AreEqual(3, protector.CurrentHealth); // 원래 대상 한정 경감을 수호자에게 넘기지 않음 확인
            Assert.AreEqual(1, target.CurrentHealth); // 실제로 전환한 원래 보호 대상 생존 확인
        } // 예고와 실제 보호 확인 종료
        finally // 목걸이 유물 검사 정리
        { // 실제 유물 제거 시작
            relics.TryRemove(necklace.RelicId); // 검사에서 장착한 목걸이 제거
        } // 실제 유물 제거 종료
    } // 실제 저체력 피해 검사 종료

    private static int SeedWithRoll(int expected) // 실제 무작위 함수의 경계 입력 준비
    { // 시드 검색 시작
        for (int seed = 0; seed < 8_192; seed++) // 실제 유니티 백분율 난수 후보 확인
        { // 시드별 경계 검사 시작
            Random.InitState(seed); // 후보의 첫 난수 설정
            if (Random.Range(0, 100) == expected) // 목표 경계 정수 일치 확인
            { // 실제 후보 발견 시작
                return seed; // 첫 시행이 재현되는 실제 시드 반환
            } // 실제 후보 발견 종료
        } // 시드별 경계 검사 종료
        throw new System.InvalidOperationException("검사에 필요한 난수 시드 없음"); // 기대 난수 경계 확인 실패 표시
    } // 시드 검색 종료

    private static void SetHealth(BattleUnitRuntime unit, int current, int maximum) // 정확한 비율 경계의 실제 체력 상태 준비
    { // 기준 체력 설정 시작
        unit.ModifyMaxHealth(maximum - unit.MaxHealth); // 시나리오의 실제 최대 체력 적용
        unit.ApplyPersistentHealth(current); // 시나리오의 실제 현재 체력 적용
    } // 기준 체력 설정 종료
} // 받는 상태와 피해 검사 종료
