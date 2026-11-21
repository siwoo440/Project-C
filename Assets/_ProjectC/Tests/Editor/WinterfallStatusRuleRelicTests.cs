using System.Collections; // 플레이 모드와 카드 입력 대기
using System.Linq; // 아군 클릭 화면 조회
using NUnit.Framework; // 상태 수치와 난수 비교
using UnityEngine; // 실제 유물과 전투 상태 원본
using UnityEngine.TestTools; // 전투 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 입력 환경 재사용
using Random = UnityEngine.Random; // 실제 전투 확률의 시드 제어

public sealed class WinterfallStatusRuleRelicTests // 화상 지속과 회복 출혈 감소 유물 검사
{ // 유물 상태 검사 시작
    [UnityTest] // 면역과 실제 화상 적용의 확률 구별 확인
    public IEnumerator WetMatchstickOnlyRollsForValidEnemyBurnFromAnAlly() // 확률 경계와 화상 중첩 지속 확인
    { // 젖은 성냥개비 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 관리자 환경 진입
        VerifyWetMatchstick(); // 독립 전투에서 실제 상태 적용 검사
        yield return new ExitPlayMode(); // 전투 환경 종료
    } // 젖은 성냥개비 검사 종료

    private static void VerifyWetMatchstick() // 실제 상태 적용 경로와 결정적 난수 검증
    { // 화상 적용 검사 시작
        using var fight = new Fight("FLAME_SLASH"); // 실제 생존 아군과 화상 대상 전투 준비
        var relic = Resources.Load<RelicData>("Relics/WetMatchstick"); // 정식 젖은 성냥개비 원본
        Assert.IsNotNull(relic); // 실제 지급 또는 판매되는 원본 확인
        Assert.AreEqual(RelicTriggerType.BeforeStatusDuration, relic.TriggerType); // 일반 상태 적용 알림과 중복되지 않는 판정 시점
        Assert.AreEqual(20, relic.TriggerChancePercent); // 실제 화상 지속 증가 확률 확인
        Assert.AreEqual(1, relic.EffectValue); // 실제 성공 시 추가 턴 확인
        var relics = RelicRunManager.EnsureInstance(); // 실제 탐사 소유 유물 관리자
        var oldRandom = Random.state; // 다른 검사와 분리한 난수 상태
        relics.TryAcquire(relic); // 현재 회차에 유물 장착
        try // 여러 화상 결과에서 확률과 지속 검증
        { // 화상 경계 검사 시작
            var ally = fight.Allies[0]; // 화상을 부여하는 실제 아군
            var target = fight.Enemies[0]; // 일반 적용과 중첩 적
            var immune = fight.Enemies[1]; // 화상 면역으로 판정하지 않을 적
            var fallen = fight.Enemies[2]; // 전투불능으로 화상을 받지 않는 대상
            int passingSeed = SeedWithFirstRoll(19); // 이십 퍼센트의 마지막 성공 정수 시드
            int failingSeed = SeedWithFirstRoll(20); // 이십 퍼센트의 첫 실패 정수 시드
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 1, 1); // 피해 상태 적용을 막는 실제 면역
            fallen.TakeDamage(fallen.MaxHealth, BattleDamageType.None); // 화상 적용 요청 전에 전투불능 설정
            Random.InitState(passingSeed); // 무효 요청이 소비하지 않을 확정 성공 난수
            var unusedRoll = Random.state; // 유효한 첫 추첨이 그대로인지 비교할 상태
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, immune.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, ally)); // 실제 면역 적의 화상 거부
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, fallen.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, ally)); // 전투불능 적의 화상 요청 거부
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, target.ApplyStatusEffect(BattleStatusEffectType.Burn, 0, 2, 3, ally)); // 실제 화상 수치가 없는 요청 제외
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, target)); // 적에게 받은 아군 화상은 정상 적용
            Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 적이 가한 아군 화상의 원본 기간 보존
            Assert.AreEqual(unusedRoll, Random.state); // 무효와 반대 진영 화상이 전투 난수를 소비하지 않음 확인
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, target.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, ally)); // 실제 생존 적에게의 첫 화상 적용
            Assert.AreEqual(3, target.GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 성공 시 이번 원본 기간에만 한 턴 추가
            Assert.AreNotEqual(unusedRoll, Random.state); // 유효한 화상 요청에서만 실제 추첨 소비 확인
            Random.InitState(failingSeed); // 원본 기간을 연장하지 않는 실패 경계 난수
            Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, target.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 5, 3, ally)); // 실패한 화상도 기존 중첩대로 적용
            Assert.AreEqual(5, target.GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 실패 시 새 원본 기간 다섯 유지
            Random.InitState(passingSeed); // 더 짧은 원본을 연장하는 별도 성공 요청
            target.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 3, ally); // 기존 긴 화상에 짧은 원본 중첩
            var burn = target.GetStatusEffect(BattleStatusEffectType.Burn); // 실제 중첩과 기간 상태 확인
            Assert.AreEqual(3, burn.StackCount); // 원래 최대 중첩 수 보존 확인
            Assert.AreEqual(5, burn.RemainingTurns); // 기존 긴 기간을 추가 연장하지 않고 보존
            var limitRoll = Random.state; // 더 늘릴 수 없는 정수 상한 호출 전 난수
            target.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, int.MaxValue, 3, ally); // 정수 상한 기간의 안전한 직접 적용
            Assert.AreEqual(int.MaxValue, burn.RemainingTurns); // 기간 초과나 부호 변화 없음 확인
            Assert.AreEqual(limitRoll, Random.state); // 기간 상한에서 무의미한 난수 미소비 확인
        } // 화상 경계 검사 종료
        finally // 검사 전 난수와 보유 유물 복원
        { // 테스트 상태 정리 시작
            Random.state = oldRandom; // 전투 난수 복원
            relics.TryRemove(relic.RelicId); // 이번 검사 유물 제거
        } // 테스트 상태 정리 종료
    } // 화상 적용 검사 종료

    [UnityTest] // 회복 카드의 실제 생존 대상과 최대 체력 검사
    public IEnumerator YellowedBandageKnotUsesOneBleedTurnPerHealCardTarget() // 실제 회복 경로와 지정 상태 제거의 독립성 확인
    { // 붕대 매듭 검사 시작
        yield return new EnterPlayMode(); // 실제 회복 카드 사용 환경 진입
        yield return VerifyYellowedBandageKnot(); // 플레이 모드 안에서 손패와 전투 검사
        yield return new ExitPlayMode(); // 전투 입력 환경 종료
    } // 붕대 매듭 검사 종료

    private static IEnumerator VerifyYellowedBandageKnot() // 실제 회복 카드의 출혈 대상 조작 검사
    { // 회복 카드 검사 시작
        using var fight = new Fight("CELESTIAL_GRACE", "FIRST_AID"); // 아군 전체와 지정 회복 카드 원본
        var relic = Resources.Load<RelicData>("Relics/YellowedBandageKnot"); // 정식 붕대 매듭 유물 원본
        Assert.IsNotNull(relic); // 실제 유물 자산 표시 확인
        Assert.AreEqual(RelicTriggerType.DirectCardHealing, relic.TriggerType); // 실제 회복 카드에만 적용되는 효과 시점 확인
        var relics = RelicRunManager.EnsureInstance(); // 실제 회차 유물 관리자
        relics.TryAcquire(relic); // 회복 카드 사용 전 붕대 매듭 장착
        try // 실제 전투 카드 사용과 상태 유지 검사
        { // 생존 아군별 출혈 처리 시작
            var wounded = fight.Allies[0]; // 체력을 실제 회복할 출혈 아군
            var full = fight.Allies[1]; // 최대 체력에서도 회복 카드 대상인 아군
            var other = fight.Allies[2]; // 출혈이 없는 다른 약화 대조군
            wounded.TakeDamage(10, BattleDamageType.None); // 상처 대상의 실제 회복 여지 준비
            wounded.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 3, 3); // 오래 지속되는 첫 출혈 중첩
            wounded.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 3, 3); // 그대로 남을 두 번째 출혈 중첩
            full.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 1, 1); // 한 턴 감소로 해제될 출혈
            other.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 1); // 회복으로 해제되지 않을 화상
            full.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 3, 1); // 출혈과 함께 보존할 중독
            Assert.AreEqual(1, wounded.RestoreHealth(1)); // 회복 카드 외의 실제 체력 회복 사용
            Assert.AreEqual(3, wounded.GetStatusEffect(BattleStatusEffectType.Bleed).RemainingTurns); // 일반 회복에서 출혈 지속 유지
            var observer = new BleedObserver(wounded, full); // 기존 상태 변경 알림의 지속 값 기록
            yield return fight.Use("CELESTIAL_GRACE"); // 실제 아군 전체 회복 카드 사용
            Assert.AreEqual(wounded.MaxHealth, wounded.CurrentHealth); // 부상 아군의 정상 체력 회복 확인
            Assert.AreEqual(full.MaxHealth, full.CurrentHealth); // 체력이 가득 찬 실제 대상 확인
            Assert.AreEqual(2, wounded.GetStatusEffect(BattleStatusEffectType.Bleed).RemainingTurns); // 대상별 출혈 지속 한 턴 정확히 감소
            Assert.AreEqual(2, wounded.GetStatusEffect(BattleStatusEffectType.Bleed).StackCount); // 출혈 피해 중첩 수 유지
            Assert.IsNull(full.GetStatusEffect(BattleStatusEffectType.Bleed)); // 최대 체력의 한 턴 출혈 제거 확인
            Assert.AreEqual(3, full.GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 해당 대상의 중독 전체 유지
            Assert.AreEqual(3, other.GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 다른 대상의 화상 전체 유지
            Assert.AreEqual(1, observer.WoundedChanges); // 남은 기간의 상태 화면 갱신 한 번 확인
            Assert.AreEqual(1, observer.FullChanges); // 출혈 제거의 독립 상태 화면 갱신 한 번 확인
            var view = fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(candidate => candidate.RuntimeUnit == wounded); // 응급 처치의 실제 대상 클릭 화면
            yield return WinterfallCommonCardExpansionTests.Play(fight.View("FIRST_AID").RuntimeCard.SourceData, fight.Hand, view, fight.Runner, fight.Deck, fight.Points); // 원래 전체 출혈 해제 성배 카드 사용
            Assert.IsNull(wounded.GetStatusEffect(BattleStatusEffectType.Bleed)); // 지정 카드의 기존 전체 출혈 해제 보존
            Assert.AreEqual(2, observer.WoundedChanges); // 같은 카드에서 원본 해제와 유물 감소 중복 없음 확인
        } // 생존 아군별 출혈 처리 종료
        finally // 독립 유물 상태 정리
        { // 일시 보유 종료 시작
            relics.TryRemove(relic.RelicId); // 이 검사에서 장착한 유물 제거
        } // 일시 보유 종료 완료
    } // 회복 카드 검사 종료

    private static int SeedWithFirstRoll(int value) // 백분율 경계와 일치하는 실제 전투 난수 찾기
    { // 실제 난수 표본 찾기 시작
        for (int seed = 0; seed < 8_192; seed++) // 동일한 무작위 도메인에서 검사 시드 순회
        { // 후보 시드 확인 시작
            Random.InitState(seed); // 다음 추첨을 후보 시드로 고정
            if (Random.Range(0, 100) == value) // 필요한 경계 정수와 첫 결과 비교
            { // 재현 가능한 후보 발견 시작
                return seed; // 경계에 맞는 시드 반환
            } // 재현 가능한 후보 발견 종료
        } // 후보 시드 확인 종료
        throw new System.InvalidOperationException("검사 경계 난수 시드 없음"); // 찾기 실패의 명시적 검사 실패
    } // 실제 난수 표본 찾기 종료

    private sealed class BleedObserver // 원래 상태 변경 알림에서 실제 출혈 변화 관찰
    { // 출혈 변화 기록 시작
        public int WoundedChanges { get; private set; } // 부상 아군의 실제 기간과 제거 알림 수
        public int FullChanges { get; private set; } // 최대 체력 아군의 실제 해제 알림 수

        public BleedObserver(BattleUnitRuntime wounded, BattleUnitRuntime full) // 두 실제 아군의 개별 알림 구독
        { // 개별 상태 조회 시작
            Observe(wounded, () => WoundedChanges++); // 부상 대상의 상태 갱신 수집
            Observe(full, () => FullChanges++); // 최대 체력 대상의 상태 갱신 수집
        } // 개별 상태 조회 종료

        private static void Observe(BattleUnitRuntime target, System.Action changed) // 다른 상태 알림과 출혈 경계 구별
        { // 상태 변화 추적 시작
            int before = target.GetStatusEffect(BattleStatusEffectType.Bleed)?.RemainingTurns ?? 0; // 최초 출혈 지속 시간
            target.StatusEffectsChanged += unit => // 실제 전투 상태 화면 알림 구독
            { // 대상의 최신 출혈 조회 시작
                int after = unit.GetStatusEffect(BattleStatusEffectType.Bleed)?.RemainingTurns ?? 0; // 현재 출혈 지속 또는 제거 값
                if (after != before) // 출혈 외 강화 알림 제외
                { // 실제 출혈 변화 기록 시작
                    before = after; // 다음 알림의 비교 원본 갱신
                    changed(); // 출혈 변화 횟수 추가
                } // 실제 출혈 변화 기록 종료
            }; // 실제 상태 화면 알림 구독 종료
        } // 상태 변화 추적 종료
    } // 출혈 변화 기록 종료
} // 유물 상태 검사 종료
