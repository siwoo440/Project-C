using System; // 실제 유물 장착과 난수 경계 검사
using System.Collections; // 실제 전투와 플레이 모드 대기
using NUnit.Framework; // 네이티브 공격과 정신력 결과 수치 비교
using UnityEngine; // 실제 유물 데이터와 유니티 난수 상태
using UnityEngine.TestTools; // 네이티브 전투 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 조우와 등록된 참가자
using Object = UnityEngine.Object; // 현재 검사 시 만든 객체 정리
using Random = UnityEngine.Random; // 전투의 실제 유니티 무작위 판정

public sealed class BlackSwampHeartwaterAndWraithMistTests // 중독 공격과 안개 명중의 실제 규칙
{ // 같은 전투 내 두 유물의 독립 검증 시작
    [UnityTest] // 실제 중독 적이 쓰는 공격력과 예고를 같은 계산으로 비교
    public IEnumerator HeartwaterFloorsPoisonedEnemyPowerInIntentAndActualAttack() // 출전한 중독 적의 실제 17 공격을 15로 설정
    { // 네이티브 적 공격 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 등록 및 상태 면역 경로 준비
        using (var relic = new OwnedRelic("BlackSwampHeartwater")) // 검은 늪의 심장수 보유
        using (var fight = new Fight("DOUBLE_SLASH")) // 기존 실제 적 행동과 대상 배치
        { // 같은 적의 약화 이전과 실제 부여 비교
            var attacker = fight.Enemies[0]; // 실제 적 진영의 살아 있는 원래 공격자
            var defender = fight.Allies[0]; // 정상 물리 피해와 방어 계산의 실제 아군
            var attack = MakeAttack(attacker, defender); // 같은 계획의 원래 열일곱 피해
            Assert.AreEqual(17, attack.AttackAmount); // 정상 적의 공격력 날짜 보정 기준
            attacker.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1, defender); // 진짜 활성 중독을 일반 상태로 적용
            Assert.AreEqual(15, attack.AttackAmount); // 17에 0.90을 곱해 기존 체력 표시 전에 내림
            BattleDamageResult preview = attack.PreviewDamage(); // 실제 방어도와 공유 예고의 적용 피해
            BattleEnemyActionResult result = attack.Execute(); // 같은 공격의 명중 난수와 상태 판정 실행
            Assert.AreEqual(15, preview.RawDamage); // 원래 예정 피해가 기다린 실제 방어 전 수치
            Assert.AreEqual(preview.AppliedDamage, result.DamageResult.AppliedDamage); // 같은 조우에서 예고와 실제 체력 피해의 일치
            Assert.AreEqual(preview.RawDamage, result.DamageResult.RawDamage); // 실제 명중도 같은 공격력 감소 공유
            attacker.RemoveStatusEffect(BattleStatusEffectType.Poison); // 원래 상태 만료 또는 해제를 실제 목록에 적용
            Assert.AreEqual(17, attack.AttackAmount); // 중독이 없어진 동일 예고의 즉시 복원
            var outside = BattleUnitRuntime.CreateEnemy(attacker.EnemySource); // 같은 타입이지만 이 전투에 참여하지 않는 별도 적
            outside.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1); // 원본 전투 연결이 없는 활성 중독
            Assert.AreEqual(17, MakeAttack(outside, defender).AttackAmount); // 조우 외부는 같은 회차에서 들고 있는 유물의 영향 제외
        } // 같은 적의 약화 이전과 실제 부여 비교 종료
        yield return new ExitPlayMode(); // 유니티 실제 시험 모드 정리
    } // 네이티브 적 공격 검사 종료

    [UnityTest] // 살아 있는 적 정신 피해와 생존 아군의 예고 및 실제 굴림을 분리
    public IEnumerator WraithMistUsesNativeMentalClampsAndCostsFiveAccuracyPoints() // 조우 안에서만 적의 명시 정신 피해 증가와 아군 명중 감소
    { // 정신 피해와 공통 공격 명중의 양쪽 실제 검증 시작
        yield return new EnterPlayMode(); // 아군과 적이 등록된 원래 전투 시작
        using (var relic = new OwnedRelic("WraithMistLantern")) // 망령 안개의 등불 보유
        using (var fight = new Fight("DOUBLE_SLASH")) // 같은 진영의 실제 전투 참가자로 검사
        { // 아군 공통 명중 및 적 정신 감소 비교 시작
            var ally = fight.Allies[0]; // 실제 생존 아군 공격자
            var enemy = fight.Enemies[0]; // 현재 실제 정신력이 남은 적 대상
            Assert.AreEqual(95, BattleHitChance.GetPercent(ally, enemy)); // 현재 실제 명중 예고의 다섯 퍼센트포인트 감산
            Assert.AreEqual(100, BattleHitChance.GetPercent(enemy, ally)); // 적의 원래 명중 예고에는 불이익 없음
            Assert.IsTrue(BattleHitChance.Hits(ally, 94, enemy)); // 가장 높은 원래 실제 명중 난수
            Assert.IsFalse(BattleHitChance.Hits(ally, 95, enemy)); // 현재 원래 실제 빗나감 경계
            Random.InitState(FindSeed(95)); // 실제 다음 굴림이 빗나감 경계인 원래 게임 난수
            Assert.IsFalse(BattleHitChance.Roll(ally, enemy)); // 지정 수치의 보조 검사 외에도 실제 무작위 판정 동일
            ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 25, 2, 1); // 원래 실명이 있는 같은 살아 있는 아군
            Assert.AreEqual(70, BattleHitChance.GetPercent(ally, enemy)); // 원래 실명과 명중 감소가 각 실제 퍼센트포인트를 유지
            Assert.AreEqual(95, BattleHitChance.GetPercentWithBlindOverride(ally, enemy, true)); // 실명만 무시하는 경우에도 기존 등불 명중 감소 적용
            var spell = enemy.ChangeMental(-8, BattleMentalChangeReason.CardEffect, ally); // 동료 아군이 준 실제 음수 정신력 카드 피해
            Assert.AreEqual(-8, spell.RequestedDelta); // 원래 카드의 요청 기록은 같은 값
            Assert.AreEqual(-10, spell.AppliedDelta); // 실제 피해는 9.6을 기존 반올림으로 열 점
            Assert.AreEqual(-6, enemy.ChangeMental(-5, BattleMentalChangeReason.RelicEffect).AppliedDelta); // 발생 유닛이 없는 실제 전투 유물 정신 피해도 증가
            Assert.AreEqual(-5, enemy.ChangeMental(-5, BattleMentalChangeReason.LastSurvivor).AppliedDelta); // 생존자 정신력 감소는 원래 전투 피해가 아님
            Assert.AreEqual(-5, enemy.ChangeMental(-5, BattleMentalChangeReason.CardEffect, enemy).AppliedDelta); // 적이 자기 자신에게 내는 비용의 원래 크기
            Assert.AreEqual(-5, ally.ChangeMental(-5, BattleMentalChangeReason.CardEffect, enemy).AppliedDelta); // 아군이 받는 원래 정신력 피해는 기본값
            var isolated = BattleUnitRuntime.CreateEnemy(enemy.EnemySource); // 현재 교전에 등록되지 않은 동일 적 객체
            Assert.AreEqual(-5, isolated.ChangeMental(-5, BattleMentalChangeReason.RelicEffect).AppliedDelta); // 조우 외부의 중립 정신력 변경 제외
            int remaining = enemy.CurrentMental; // 언제나 영에서 멈출 마지막 실제 정신력
            var clamped = enemy.ChangeMental(-int.MaxValue, BattleMentalChangeReason.ConsumableEffect, ally); // 실제 정수 상한을 넘어갈 수 있는 보정 입력
            Assert.AreEqual(-remaining, clamped.AppliedDelta); // 기존 정신력 영 하한과 정상 특수 상태 유지
            Assert.AreEqual(0, enemy.CurrentMental); // 정수 뒤집힘 없는 실제 정신력 최솟값
        } // 아군 공통 명중 및 적 정신 감소 비교 종료
        yield return new ExitPlayMode(); // 실제 Unity 테스트 환경 정리
    } // 정신 피해와 공통 공격 명중의 양쪽 실제 검증 종료

    private static BattleEnemyAction MakeAttack(BattleUnitRuntime attacker, BattleUnitRuntime target) // 동일한 예고와 실제 정수 피해를 확인할 정상 적 공격
    { // 기존 적 행동 원본을 실제 경로로 구성 시작
        return new BattleEnemyAction(attacker, target, EnemyActionType.Attack, BattleDamageType.Physical, 17, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "중독 공격 검사", 1, 1, 1, 0, percentRoll: () => 0); // 조우에서 실제 적용할 고정 명중의 공격 행동
    } // 기존 적 행동 원본을 실제 경로로 구성 종료

    private static int FindSeed(int percentile) // 실제 Unity 난수의 목표 백분율 검색
    { // 기존에 쓰던 실제 난수 상태 복원 보장 시작
        var state = Random.state; // 다른 검사에 영향을 주기 전의 유니티 상태
        try // 실제 난수 탐색만 이 상태에서 수행
        { // 같은 백분율의 정확한 시작값 탐색 시작
            for (int seed = 0; seed < 10000; seed++) // 원래 실제 시작값의 유효 범위 탐색
            { // 현재 실제 난수의 원하는 값 검사 시작
                Random.InitState(seed); // 이 후보의 실제 첫 굴림 입력
                if (Random.Range(0, 100) == percentile) // 원래 유니티의 실제 백분율
                { // 정확한 경계값을 얻은 시드 처리 시작
                    return seed; // 정해진 실제 명중 확인용 설정
                } // 정확한 경계값을 얻은 시드 처리 종료
            } // 현재 실제 난수의 원하는 값 검사 종료
            throw new InvalidOperationException("명중 검사에 필요한 시드 없음"); // 원래 정해진 범위에서 완전하지 않은 검사 표시
        } // 같은 백분율의 정확한 시작값 탐색 종료
        finally // 이전 네이티브 상태 복구
        { // 원본의 게임 난수 보존 시작
            Random.state = state; // 검사 밖에서는 앞서 저장한 실제 원래 난수
        } // 원본의 게임 난수 보존 종료
    } // 기존에 쓰던 실제 난수 상태 복원 보장 종료

    private sealed class OwnedRelic : IDisposable // 실제 회차의 유물 장착과 독립 테스트 제거
    { // 실제 유물 원본 보관 시작
        private readonly RelicRunManager prior = RelicRunManager.Instance; // 원래 검사 시작 전에 있던 실제 보유 객체
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 유물과 현재 전투가 사용하는 실제 보유 객체
        private readonly RelicData relic; // 실제 유물 원본의 정식 Unity 데이터
        public OwnedRelic(string name) // 현재 사례에 쓰일 원본 유물 장착
        { // 유물 원본의 실제 지급 처리 시작
            relic = Resources.Load<RelicData>("Relics/" + name); // 실제 전투의 유물 등록 파일 검색
            Assert.IsNotNull(relic); // 등록된 실제 유물인지 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 동일 회차 인벤토리의 보유 확인
        } // 유물 원본의 실제 지급 처리 종료
        public void Dispose() // 이번 시험에서 만든 원래 참조 제거
        { // 실제 유물 정리 및 원래 상태 복원
            manager.TryRemove(relic.RelicId); // 이전 회차를 유지한 현재 검사용 유물만 제거
            if (prior == null) // 이번 검사에서 처음 보유 객체를 만든 경우 확인
            { // 검사에서 만든 객체만 정리
                Object.Destroy(manager.gameObject); // 실제 게임의 임시 회차 보유 객체 제거
            } // 검사에서 만든 객체만 정리 종료
        } // 실제 유물 정리 및 원래 상태 복원 종료
    } // 실제 유물 원본 보관 종료
} // 같은 전투 내 두 유물의 독립 검증 종료
