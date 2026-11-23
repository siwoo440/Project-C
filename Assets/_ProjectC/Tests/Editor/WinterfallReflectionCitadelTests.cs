using System.Collections; // 실제 전투 입력 환경 전환 대기
using System.Linq; // 생존 적과 반사 횟수 검사
using NUnit.Framework; // 실제 피해와 전투 결과 검증
using UnityEngine; // 임시 보스 전투 원본 생성
using UnityEngine.TestTools; // 플레이 모드 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드와 전투 이벤트 검사 환경

public sealed class WinterfallReflectionCitadelTests // 반사의 성채 합산과 결과 처리 검사
{ // 반사 검사 시작
    [UnityTest] // 실제 공용 피해 이벤트의 출처와 정산 순서 검사
    public IEnumerator CitadelFloorsOnceFromDirectAndLinkedSharedHealthLoss() // 방어도와 비용 및 비직접 피해를 뺀 파티 합산 확인
    { // 직접 피해 수집 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 이벤트 실행 환경 진입
        yield return CitadelFloorsOnceFromDirectAndLinkedSharedHealthLossInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 게임 전투 검사 종료
    } // 직접 피해 수집 검사 종료

    private IEnumerator CitadelFloorsOnceFromDirectAndLinkedSharedHealthLossInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("BALANCE_GUARD")) // 실제 아군과 예정 적 및 시작 손패 생성
        using (var dispatcher = new BattleEventDispatcher()) // 공용 전투 이벤트 발행기 연결
        using (var bridge = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 실제 유닛 피해와 턴 경계 이벤트 연결
        { // 반사 입력 검사 시작
            ApplyCitadel(fight); // 실제 카드가 주는 아군 전체 상태 적용
            var first = fight.Allies[0]; // 첫 피해와 분담 피해를 받을 아군
            var second = fight.Allies[1]; // 분담 공격의 직접 피격 아군
            var enemy = fight.Enemies[0]; // 직접 타격을 줄 실제 적
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 성채가 유지되는 첫 실제 적 턴 진입
            first.GainBlock(2); // 직접 피해를 모두 막을 방어도 준비
            Assert.AreEqual(0, first.TakeDamage(2, BattleDamageType.None, enemy, enemyDirectAttack: true).AppliedDamage); // 모두 막힌 직접 적중 확인
            Assert.IsTrue(first.TrySpendHealth(1)); // 전투 피해 이벤트 없이 실제 체력 비용 지불
            first.ApplyStatusEffect(BattleStatusEffectType.Bleed, 4, 1, 1, enemy); // 적이 남긴 지속 피해 준비
            new BattleStatusEffectProcessor().ProcessPhaseEnd(first, fight.Turns.CurrentRound); // 실제 출혈 체력 손실 처리
            first.TakeDamage(3, BattleDamageType.None, enemy); // 적이 출처이지만 직접 적중 경로가 아닌 피해
            first.TakeDamage(2, BattleDamageType.None, second, enemyDirectAttack: true); // 적 진영이 아닌 출처의 대조 피해
            var miss = new BattleEnemyAction(enemy, first, EnemyActionType.Attack, BattleDamageType.None, 12, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "검사", 1, 1, 1, 0, percentRoll: () => 0); // 빗나간 원래 공격의 실행 경로
            enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 1, 1); // 적 공격의 명중률 영 설정
            Assert.IsTrue(miss.Execute().WasMissed); // 실제 빗나간 적 직접 공격 확인
            enemy.RemoveStatusEffect(BattleStatusEffectType.Blind); // 정상 적 공격 검사를 위한 실명 정리
            Assert.AreEqual(1, first.TakeDamage(1, BattleDamageType.None, enemy, enemyDirectAttack: true).AppliedDamage); // 직접 아군 피해 한 점 기록
            int beforeShare = first.CurrentHealth; // 다음 공격의 분담 체력 손실 비교 기준
            Assert.AreEqual(2, second.TakeDamage(3, BattleDamageType.None, enemy, sharingAlly: first, enemyDirectAttack: true).AppliedDamage); // 두 번째 아군의 직접 손실 두 점 확인
            Assert.AreEqual(beforeShare - 1, first.CurrentHealth); // 같은 적 명중에 연결된 실제 아군 분담 한 점 확인
            foreach (var target in fight.Enemies) // 모든 적의 방어도와 체력 반사 비교 준비
            { // 적별 반사 검사 연결 시작
                target.GainBlock(20); // 반사 고정 피해가 우회할 기존 방어도
                target.DamageTaken += (unit, damage) => // 실제 반사 피해가 적용되는 프레임 조회
                { // 즉시 피해 결과 검증 시작
                    Assert.AreEqual(20, unit.CurrentBlock); // 반사가 기존 적 방어도를 소모하지 않는지 확인
                    Assert.AreEqual(BattleDamageType.Physical, damage.DamageType); // 고정 물리 반사로 기록되는지 확인
                }; // 즉시 피해 결과 검증 종료
            } // 적별 반사 검사 연결 종료
            Assert.IsTrue(fight.Enemies.All(target => target.CurrentHealth == target.MaxHealth)); // 적 턴 중 조기 반사 없는지 확인
            int phaseChecks = 0; // 첫 다음 아군 시작에서 정산 완료 확인 수
            fight.Turns.PhaseStarted += (phase, round) => // 실제 아군 시작 시의 적 상태 검증
            { // 다음 턴 정산 확인 시작
                if (phase == BattleTurnPhase.PlayerTurn && round == 2) // 반사를 끝낸 다음 실제 아군 턴 확인
                { // 아군 드로우 전 피해 조회 시작
                    phaseChecks++; // 반사 이후 턴 시작 한 번 기록
                    Assert.IsTrue(fight.Enemies.All(target => target.CurrentHealth == target.MaxHealth - 2)); // 한 점과 두 점 및 분담 한 점을 더해 한 번 내린 결과 확인
                } // 아군 드로우 전 피해 조회 종료
            }; // 다음 턴 정산 확인 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 턴 종료와 공용 반사 정산 실행
            Assert.AreEqual(1, phaseChecks); // 실제 다음 아군 턴 전에 정산 한 번 확인
            Assert.IsTrue(fight.Allies.All(ally => ally.GetStatusEffect(BattleStatusEffectType.ReflectionCitadel) == null)); // 처리한 적 턴의 성채 만료 확인
        } // 반사 입력 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 마지막 적들의 실제 처치와 결과 발행 검사
    public IEnumerator CitadelEndsBattleBeforeAnotherPlayerTurnAndSkipsDeadEnemies() // 현재 생존 적만 반사하고 새 아군 드로우 없는 승리 확인
    { // 반사 승리 검사 시작
        yield return new EnterPlayMode(); // 공용 전투 결과 이벤트 실행 환경 진입
        yield return CitadelEndsBattleBeforeAnotherPlayerTurnAndSkipsDeadEnemiesInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 실제 결과와 검사 전투 종료
    } // 반사 승리 검사 종료

    private IEnumerator CitadelEndsBattleBeforeAnotherPlayerTurnAndSkipsDeadEnemiesInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        using (var fight = new Fight("BALANCE_GUARD")) // 살아 있는 적과 실제 아군 손패 준비
        using (var dispatcher = new BattleEventDispatcher()) // 실제 전투 이벤트 발행기 생성
        using (var bridge = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 유닛 사망과 전투 결과 연결
        { // 반사 승리 경로 검사 시작
            var dead = fight.Enemies[0]; // 반사 이전부터 사망한 적
            dead.TakeDamage(dead.CurrentHealth, BattleDamageType.None, fight.Allies[0]); // 제외할 적 한 명을 먼저 실제 처치
            var survivors = fight.Enemies.Where(enemy => !enemy.IsDead).ToArray(); // 성채 발동 전 살아 있는 적 목록
            foreach (var enemy in survivors) // 모두 같은 반사로 쓰러질 체력 구성
            { // 처치 임계 준비 시작
                enemy.TakeDamage(enemy.CurrentHealth - 1, BattleDamageType.None, fight.Allies[0]); // 각 적의 체력을 정확히 한 점으로 조정
            } // 처치 임계 준비 종료
            ApplyCitadel(fight); // 반사의 성채를 살아 있는 아군 전체에 적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 성채가 집계하는 실제 적 턴 시작
            fight.Allies[0].TakeDamage(2, BattleDamageType.None, survivors[0], enemyDirectAttack: true); // 생존 적 한 명이 준 실제 체력 두 점
            int payouts = 0; // 적을 향한 실제 반사 이벤트 기록
            int victories = 0; // 게임에 발행된 실제 승리 결과 수
            using var damageSignal = dispatcher.Subscribe(BattleEventType.DamageApplied, context => // 반사만 발생하는 정산 구간의 피해 검사
            { // 반사 이벤트 검사 시작
                CollectionAssert.Contains(survivors, context.TargetUnit); // 기존 사망자는 반사를 받지 않는지 확인
                Assert.AreEqual(1, context.AppliedAmount); // 살아 있는 적마다 동일한 실제 반사 피해 확인
                payouts++; // 지금 반사한 적 한 명 기록
            }); // 반사 이벤트 검사 종료
            using var battleSignal = dispatcher.Subscribe(BattleEventType.BattleEnded, context => // 실제 전투 결과 게시 검사
            { // 전투 종료 검사 시작
                Assert.AreEqual(BattleResult.Victory, context.Result); // 반사가 마지막 적을 처치한 실제 승리 확인
                victories++; // 중복 없는 승리 게시 기록
            }); // 전투 종료 검사 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 준비 전에 반사 피해 지급
            Assert.AreEqual(survivors.Length, payouts); // 처음 생존한 적마다 반사 한 번 확인
            Assert.AreEqual(1, victories); // 실제 전투 결과의 게시 한 번 확인
            Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 전투의 최종 승리 단계 확인
            Assert.AreEqual(0, fight.Deck.HandCount); // 승리 이후 추가 아군 손패 드로우 금지 확인
        } // 반사 승리 경로 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [Test] // 보스 전환이 적 턴을 소비하지 않는 규칙 검사
    public void CitadelWaitsThroughBossTransitionUntilAnActualEnemyRound() // 보스 강제 전환 뒤 첫 실제 적 턴의 상태 정리 확인
    { // 실제 적 턴 대기 검사 시작
        var character = ScriptableObject.CreateInstance<CharacterData>(); // 검사용 아군 원본
        var enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 검사용 보스 원본
        var deckSource = ScriptableObject.CreateInstance<DeckData>(); // 빈 검사 덱 원본
        try // 검사 원본 정리 보장
        { // 성채 대기 비교 시작
            var ally = BattleUnitRuntime.CreateAlly(character); // 아직 살아 있는 실제 보호 아군
            var allies = new[] { ally }; // 정의된 하나의 아군 목록
            var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemySource) }; // 다음 실제 적 턴을 시작할 보스
            var deck = BattleDeckRuntime.Create(deckSource, allies); // 드로우 없이 턴을 진행할 전투 덱
            using var turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(5), allies, enemies, 0, BattleType.Boss); // 강제 턴 전환을 지원하는 실제 보스전
            using var statuses = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 일반 상태 지속 시간과 턴 종료 연결
            Assert.IsTrue(turns.StartBattle(0)); // 첫 실제 아군 턴에서 카드 효과 준비
            ally.ApplyStatusEffect(BattleStatusEffectType.ReflectionCitadel, 50, 1, 1, ally); // 첫 적 턴에만 적용할 실제 성채 상태
            turns.QueueBossPhaseTransition(); // 적 턴을 건너뛸 보스 전환 예약
            Assert.IsTrue(turns.CompleteBossPhaseTransition()); // 실제 아군부터 다시 시작하는 보스 전환 확인
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.ReflectionCitadel)); // 적 턴 없는 전환에서 성채 미소모 확인
            Assert.IsTrue(turns.EndPlayerTurn() && turns.CompleteEnemyTurn()); // 피해가 없더라도 실제 적 턴 종료
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.ReflectionCitadel)); // 실제 적 턴 정산 뒤 성채 한 번 제거 확인
        } // 성채 대기 비교 종료
        finally // 생성한 독립 원본 해제
        { // 검사 원본 정리 시작
            Object.DestroyImmediate(deckSource); // 빈 검사 덱 제거
            Object.DestroyImmediate(enemySource); // 검사 보스 원본 제거
            Object.DestroyImmediate(character); // 검사 아군 원본 제거
        } // 검사 원본 정리 종료
    } // 실제 적 턴 대기 검사 종료

    private static void ApplyCitadel(Fight fight) // 카드의 아군 전체 상태 적용 규칙 재현
    { // 검사 성채 적용 시작
        foreach (var ally in fight.Allies) // 실제 생존 아군 전원 확인
        { // 대상별 성채 부여 시작
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.ReflectionCitadel, 50, 1, 1, fight.Allies[0])); // 실제 단일 대상 상태 부여 경로 확인
        } // 대상별 성채 부여 종료
    } // 검사 성채 적용 종료
} // 반사 검사 종료
