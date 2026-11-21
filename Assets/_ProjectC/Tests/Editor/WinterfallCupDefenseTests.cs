using System.Collections; // 플레이 모드와 카드 연출 대기
using System.Reflection; // 실제 무방어 피해와 분담 연결 조회
using NUnit.Framework; // 방어도와 피해 및 공포 판정 비교
using UnityEngine; // 정식 유물 원본과 전투 난수
using UnityEngine.TestTools; // 실제 카드 입력 검사 등록
using Random = UnityEngine.Random; // 재현할 공포 저항의 난수 입력
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드와 턴 검사 환경

public sealed class WinterfallCupDefenseTests // 수호 성가와 성역 및 고요한 찬가 검사
{ // 성배 방어 카드 검사 시작
    [UnityTest] // 카드의 전체 아군 대상과 실제 피해 감소 검사
    public IEnumerator GuardianHymnAndSanctuaryUseEveryLivingAllyAndBothDamagePaths() // 개별 방어도와 성역의 두 적 턴 검증
    { // 수호 성가와 성역 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 전투 환경 진입
        yield return VerifyHymnAndSanctuary(); // 플레이 모드에서 실제 카드와 피해 처리 실행
        yield return new ExitPlayMode(); // 독립 플레이 모드 검사 종료
    } // 수호 성가와 성역 검사 종료

    private static IEnumerator VerifyHymnAndSanctuary() // 실제 전체 방어와 두 기간의 피해 적용
    { // 기존 전투 환경과 카드 조회 시작
        using var fight = new Fight("GUARDIAN_HYMN", "SANCTUARY"); // 실제 시트 보상에 등록한 두 성배 카드
        var caster = fight.Allies[0]; // 살아 있는 시전자와 증가량 대조
        var ally = fight.Allies[1]; // 수호와 지속 피해를 받을 생존 아군
        var fallen = fight.Allies[2]; // 전체 효과에서 제외할 전투불능 아군
        var enemy = fight.Enemies[0]; // 정상 피해 시험의 실제 적 원본
        caster.ModifyMaxHealth(100 - caster.MaxHealth); // 분담 피해를 비교할 충분한 체력값
        caster.ApplyPersistentHealth(100); // 원래 피격 대상의 실제 최대 체력
        ally.ModifyMaxHealth(100 - ally.MaxHealth); // 성역이 적용될 피해 비교 최대 체력
        ally.ApplyPersistentHealth(100); // 살아 있는 피해 대상의 초기 체력
        fallen.ApplyPersistentHealth(0); // 실제 전투불능 대상 설정
        caster.GainBlock(3); // 원래 방어도와 추가 방어도를 나누어 확인할 조건
        int before = fight.Points.CurrentActionPoints; // 두 정식 카드 사용 전 행동력
        yield return fight.Use("GUARDIAN_HYMN"); // 실제 총 포인트와 별도 개별 방어도 지급
        Assert.AreEqual(before - 2, fight.Points.CurrentActionPoints); // 수호 성가의 실제 행동력 두 점
        Assert.AreEqual(11, caster.CurrentBlock); // 시전자에게 개별 방어도 여덟 추가
        Assert.AreEqual(8, ally.CurrentBlock); // 생존 동료에게 별도 방어도 여덟 추가
        Assert.AreEqual(0, fallen.CurrentBlock); // 전투불능 아군의 방어도 미적용
        yield return fight.Use("SANCTUARY"); // 같은 실제 전체 생존 대상에 성역 부여
        Assert.AreEqual(before - 4, fight.Points.CurrentActionPoints); // 성역의 실제 행동력 두 점 추가 차감
        Assert.AreEqual(10, caster.GetStatusEffect(BattleStatusEffectType.SanctuaryWard)?.EffectiveValue); // 시전자 성역의 실제 감소율 십
        Assert.AreEqual(10, ally.GetStatusEffect(BattleStatusEffectType.SanctuaryWard)?.EffectiveValue); // 모든 대상에 독립적으로 적용된 감소율
        Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.SanctuaryWard)); // 전투불능 대상에 성역 미적용
        ally.ClearBlock(); // 한 번 내림과 상태 곱셈의 정량적인 피해 비교 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.GuardianCircle, 50, 1, 1); // 기존 직접 피해 비율 절반 보정
        var preview = ally.PreviewDamage(3, BattleDamageType.None, enemy, enemyDirectAttack: true); // 기존 직접 피해의 실제 화면 예고 경로
        Assert.AreEqual(1, preview.FinalDamage); // 비율을 함께 곱한 한 번 내림의 예상 피해
        Assert.AreEqual(preview.FinalDamage, ally.TakeDamage(3, BattleDamageType.None, enemy, enemyDirectAttack: true).FinalDamage); // 같은 직접 피해 예고와 실제 피해 일치
        ally.RemoveStatusEffect(BattleStatusEffectType.GuardianCircle); // 성역만의 직접과 지속 피해의 비교 조건
        ally.GainBlock(9); // 감소 뒤 피해에 정확히 맞는 방어도
        var blocked = ally.TakeDamage(10, BattleDamageType.None); // 출처 없는 일반 피해에도 적용하는 성역
        Assert.AreEqual(9, blocked.AbsorbedDamage); // 직접 공격 한정 없이 방어도 이전에 십 퍼센트 감소
        Assert.AreEqual(0, blocked.AppliedDamage); // 감소한 아홉 점의 전량 방어 확인
        Assert.IsTrue(ally.TrySpendHealth(2)); // 살아 있는 아군의 별도 체력 비용 지불
        Assert.AreEqual(97, ally.CurrentHealth); // 체력 비용에서 성역 경감 없이 두 점 지불 확인
        var unblocked = typeof(BattleUnitRuntime).GetMethod("TakeUnblockedDamage", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 분담과 출혈의 무방어 피해 경로
        Assert.IsNotNull(unblocked); // 무방어 실제 처리 메서드 확인
        Assert.AreEqual(9, unblocked.Invoke(ally, new object[] { 10, BattleDamageType.None, enemy, false })); // 방어도를 건너뛴 원본 피해도 성역 감소
        caster.ClearBlock(); // 뒤이은 실제 분담에서 같은 원본 피해를 사용할 조건
        var attack = new BattleEnemyAction(enemy, caster, EnemyActionType.Attack, BattleDamageType.None, 20, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "성역 분담 검사", 1, 1, 1, 0, percentRoll: () => 0); // 실제 적의 분담 가능한 직접 행동
        var share = typeof(BattleEnemyAction).GetMethod("SetDamageShare", BindingFlags.Instance | BindingFlags.NonPublic); // 정상 분담 연결 메서드 조회
        Assert.IsNotNull(share); // 정상 공격 분담 경로 존재 확인
        Assert.IsTrue((bool)share.Invoke(attack, new object[] { ally })); // 실제 다른 아군의 피해 분담 연결
        int sharePreview = attack.PreviewSharedDamage(); // 분담 받는 아군의 방어도 없는 실제 예고
        int expectedShare = (attack.PreviewDamage().FinalDamage / 2) * 90 / 100; // 원래 대상의 감소와 분담 대상 경감의 별도 계산
        Assert.AreEqual(expectedShare, sharePreview); // 분담 대상에게 적용한 실제 성역 예고 확인
        int healthBeforeShared = ally.CurrentHealth; // 실제 분담을 받기 전 체력 보관
        attack.Execute(); // 실제 아군별 직접 및 분담 피해 적용
        Assert.AreEqual(sharePreview, healthBeforeShared - ally.CurrentHealth); // 예고와 실제 무방어 분담 감소 일치
        Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.SanctuaryWard)?.RemainingTurns); // 시전한 적 턴 전의 초기 지속 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 현재 아군 턴 종료와 첫 실제 적 턴 진입
        Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.SanctuaryWard)?.RemainingTurns); // 플레이어 종료에 별도 차감 없음 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 턴 종료 진행
        Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.SanctuaryWard)?.RemainingTurns); // 첫 적 턴에 한 번 차감 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료까지 진행
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.SanctuaryWard)); // 두 적 턴 뒤 정상 만료 확인
        Assert.AreEqual(10, ally.TakeDamage(10, BattleDamageType.None).FinalDamage); // 만료한 뒤 다른 방어 없는 원래 피해 복원 확인
    } // 기존 전투 환경과 카드 조회 종료

    [UnityTest] // 기존 완전 면역과 신규 찬가 및 유물 확률 비교
    public IEnumerator QuietHymnAppliesFearResistanceBeforeTheRelicAndAfterImmunity() // 각각의 추첨과 유효 대상 비교
    { // 고요한 찬가 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 환경 진입
        yield return VerifyQuietHymn(); // 플레이 모드에서 공포 부여와 실제 턴 확인
        yield return new ExitPlayMode(); // 독립 플레이 모드 종료
    } // 고요한 찬가 검사 종료

    private static IEnumerator VerifyQuietHymn() // 유효한 대상의 독립 공포 확률 검증
    { // 실제 찬가 카드와 아군 조회 시작
        using var fight = new Fight("QUIET_HYMN"); // 실제 시트에 등록한 희귀 찬가 원본
        var caster = fight.Allies[0]; // 실제 저항이 적용될 시전자
        var ally = fight.Allies[1]; // 공포를 받는 두 번째 생존 대상
        var fallen = fight.Allies[2]; // 상태를 받을 수 없는 전투불능 대상
        var enemy = fight.Enemies[0]; // 기존 공포 부여의 적 출처
        fallen.ApplyPersistentHealth(0); // 실제 전투불능 상태 설정
        int before = fight.Points.CurrentActionPoints; // 최초 찬가 사용 직전 행동력
        yield return fight.Use("QUIET_HYMN"); // 실제 전체 아군 화면 상태 적용
        Assert.AreEqual(before - 2, fight.Points.CurrentActionPoints); // 고요한 찬가의 실제 행동력 두 점
        Assert.AreEqual(30, caster.GetStatusEffect(BattleStatusEffectType.FearResistance)?.EffectiveValue); // 시전자 공포 저항 삼십 확인
        Assert.AreEqual(30, ally.GetStatusEffect(BattleStatusEffectType.FearResistance)?.EffectiveValue); // 모든 생존 동료의 개별 공포 저항 확인
        Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.FearResistance)); // 전투불능 대상의 저항 미적용 확인
        var relics = RelicRunManager.EnsureInstance(); // 실제 탐사 유물 보유 관리자
        var cord = Resources.Load<RelicData>("Relics/QuietBellCord"); // Day250 무음 방울끈 실제 원본
        Assert.IsNotNull(cord); // 확률을 함께 검증할 유물 원본 확인
        var original = Random.state; // 나머지 전투 검사에 영향을 주지 않을 원래 난수
        try // 첫 번째 카드 저항과 두 번째 유물 저항의 검증
        { // 실제 공포 판정 비교 시작
            int cardPasses = FindSeed(true, false); // 찬가만 성공하고 유물은 실패할 난수
            int relicPasses = FindSeed(false, true); // 찬가가 실패하고 유물만 성공할 난수
            int bothFail = FindSeed(false, false); // 두 저항이 정상적으로 실패할 난수
            relics.TryAcquire(cord); // 찬가와 다른 독립 공포 방지 부여
            ally.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 두 확률 전에 확인할 정상 공포 완전 면역
            Random.InitState(cardPasses); // 소비하지 말아야 할 첫 상태 추첨 설정
            var startingRoll = Random.state; // 전체 면역과 무효 대상의 비교 난수
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1, enemy)); // 기존 완전 면역이 항상 먼저 차단
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, fallen.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1, enemy)); // 전투불능 아군에게 확률 부여 없음 확인
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 0, 1, enemy)); // 무효 지속의 확률 소모 없음 확인
            Assert.AreEqual(startingRoll, Random.state); // 면역과 무효 요청에서 두 확률 모두 미소비
            ally.RemoveStatusEffect(BattleStatusEffectType.StatusImmunity); // 기존 면역 제거로 독립 두 저항 경로 복원
            ExpectFearRoll(ally, enemy, cardPasses, 1, BattleStatusEffectApplyResult.BlockedByImmunity); // 찬가 성공에 유물 무작위 미소비 확인
            ExpectFearRoll(ally, enemy, relicPasses, 2, BattleStatusEffectApplyResult.BlockedByImmunity); // 찬가 실패 뒤 기존 유물로 취소 확인
            ExpectFearRoll(ally, enemy, bothFail, 2, BattleStatusEffectApplyResult.Applied); // 두 확률이 실패하면 정상 공포 적용 확인
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Fear)); // 실패한 두 확률 뒤 실제 공포 존재 확인
            ally.RemoveStatusEffect(BattleStatusEffectType.Fear); // 찬가 지속의 별도 턴 검증 준비
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 적 턴의 실제 종료까지 진행
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.FearResistance)?.RemainingTurns); // 첫 적 턴 뒤 남은 공포 저항 한 턴 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 둘째 적 턴의 실제 종료까지 진행
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.FearResistance)); // 두 적 턴 뒤 상태 만료 확인
        } // 실제 공포 판정 비교 종료
        finally // 독립 검사 자산과 난수 복구
        { // 실제 검사 상태 정리 시작
            Random.state = original; // 전투 검사 이전 난수 복원
            relics.TryRemove(cord.RelicId); // 테스트에서 장착한 공포 유물 제거
        } // 실제 검사 상태 정리 종료
    } // 실제 찬가 카드와 아군 조회 종료

    private static int FindSeed(bool firstPass, bool secondPass) // 두 독립 확률의 실제 무작위 시드 탐색
    { // 전투 난수 시드 탐색 시작
        for (int seed = 0; seed < 8_192; seed++) // 재현 가능한 실제 시드 순회
        { // 후보 시드의 두 번 추첨 시작
            Random.InitState(seed); // 첫 경계의 독립 후보 시드 지정
            bool first = Random.Range(0, 100) < 30; // 카드 저항의 실제 확률 결과
            bool second = Random.Range(0, 100) < 30; // 유물 저항의 실제 확률 결과
            if (first == firstPass && second == secondPass) // 필요한 두 결과와 후보 비교
            { // 정상 후보 찾기 시작
                return seed; // 해당 두 결과를 만드는 실제 시드 반환
            } // 정상 후보 찾기 종료
        } // 후보 시드의 두 번 추첨 종료
        throw new System.InvalidOperationException("공포 확률 검사 시드 없음"); // 결정적 검사에 필요한 경계 후보 부재 오류
    } // 전투 난수 시드 탐색 종료

    private static void ExpectFearRoll(BattleUnitRuntime target, BattleUnitRuntime enemy, int seed, int rolls, BattleStatusEffectApplyResult result) // 정상 공포 부여 결과와 무작위 소비 수 검증
    { // 난수 개수 대조 시작
        Random.InitState(seed); // 기대하는 초기 전투 난수 설정
        for (int index = 0; index < rolls; index++) // 기대하는 무작위 결과 개수만큼 추첨
        { // 실제 무작위 소비 샘플 시작
            Random.Range(0, 100); // 비교할 동일 범위의 한 번 추첨
        } // 실제 무작위 소비 샘플 종료
        var expected = Random.state; // 정확히 기대 횟수만 소비한 난수 상태
        Random.InitState(seed); // 실제 상태 부여에 같은 초기 난수 입력
        Assert.AreEqual(result, target.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1, enemy)); // 차단 또는 실제 공포 부여 결과 확인
        Assert.AreEqual(expected, Random.state); // 찬가와 유물의 실제 무작위 소비 횟수 확인
    } // 난수 개수 대조 종료
} // 성배 방어 카드 검사 종료
