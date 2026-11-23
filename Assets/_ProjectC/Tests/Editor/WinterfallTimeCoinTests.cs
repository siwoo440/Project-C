using System.Collections; // 실제 플레이 모드 진입과 카드 연출 대기
using System.Collections.Generic; // 실제 적과 아군 단계 이벤트 순서 기록
using NUnit.Framework; // 턴별 실제 자원과 상태 비교
using UnityEngine.TestTools; // Unity의 실제 전투 환경 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 기존 손패와 실제 카드 사용 환경

public sealed class WinterfallTimeCoinTests // 시간의 동전의 실제 입력과 턴 수명 검사
{ // 카드의 추가 턴 검사 시작
    [UnityTest] // 적을 건너뛰는 실제 다음 아군 차례와 재예약 확인
    public IEnumerator TimeCoinRunsAFullAllyTurnThenAllowsOneNormalEnemyTurn() // 첫 추가 턴과 실제 적 차례 이후 재사용 비교
    { // 실제 턴 경계 검사 시작
        yield return new EnterPlayMode(); // 정식 배포 카드와 정상 전투 입력 시작
        using (var fight = new Fight("TIME_COIN", "TIME_COIN", "THRUST")) // 같은 손패의 두 동전과 추가 차례 확인용 카드 준비
        { // 원래 카드의 완료와 새 단계 알림 관찰 시작
            var ally = fight.Allies[0]; // 실제 카드 소유자와 방어도 변화 대상
            var enemy = fight.Enemies[0]; // 건너뛴 실제 적 차례의 상태와 방어도 비교 대상
            var phases = new List<(bool Started, BattleTurnPhase Phase, int Round)>(); // 실제 진영 이벤트의 정상 통과 순서
            fight.Turns.PhaseStarted += (phase, round) => phases.Add((true, phase, round)); // 시작 이벤트의 실제 단계와 턴 번호 기록
            fight.Turns.PhaseEnded += (phase, round) => phases.Add((false, phase, round)); // 끝 알림의 실제 단계와 턴 번호 기록
            var first = fight.View("TIME_COIN").RuntimeCard; // 원본 보물의 실제 첫 동전
            Assert.AreEqual(4, first.ApCost); // 원본 고정 행동력 네 점
            int initialRound = fight.Turns.CurrentRound; // 실제 시작 아군 차례의 번호
            int initialPoints = fight.Points.CurrentActionPoints; // 정상 최초 가용 행동력 기준
            ally.GainBlock(8); // 다음 아군 시작에 정리될 현재 아군 방어도
            enemy.GainBlock(7); // 아직 한 번도 행동하지 않은 적의 기존 차단 상태
            ally.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedUp, 10, 1, 1); // 현재 실제 아군 차례 종료에 만료되는 기존 강화
            enemy.ApplyStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown, 10, 2, 1); // 두 적 종료에서만 감소할 기존 약화

            yield return fight.Use("TIME_COIN"); // 원래 카드를 정상 지불하고 손패에서 버림으로 이동
            Assert.AreEqual(initialPoints - 4, fight.Points.CurrentActionPoints); // 현재 턴에는 실제 카드 비용만 정산
            var second = fight.View("TIME_COIN"); // 아직 사용하지 않은 다른 물리 동전
            second.OnPointerClick(null); // 같은 아군 차례의 원래 두 번째 예약 시도
            yield return null; // 실제 거절된 입력이 화면과 행동 상태에 반영될 프레임 대기
            Assert.AreEqual(initialPoints - 4, fight.Points.CurrentActionPoints); // 중복 예약에서 정상 카드 비용 미소모
            CollectionAssert.Contains(fight.Deck.Hand, second.RuntimeCard); // 지불 전 거절된 원래 손패 실물 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 원래 아군 차례 종료 입력 실행

            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turns.CurrentPhase); // 적이 아직 시작하지 않은 실제 추가 아군 차례
            Assert.AreEqual(initialRound + 1, fight.Turns.CurrentRound); // 실제 시작과 종료가 구분되는 새 아군 차례 번호
            Assert.AreEqual(initialPoints - 2, fight.Points.MaxActionPoints); // 추가 차례의 원래 최대 행동력 두 점 감소
            Assert.AreEqual(initialPoints - 2, fight.Points.CurrentActionPoints); // 기존 최대 회복 뒤 실제 현재 행동력 두 점 감소
            Assert.AreEqual(3, fight.Turns.LastDrawnCardCount); // 원래 손패를 정리하고 실제로 새 기본 세 장 드로우
            Assert.AreEqual(3, fight.Deck.HandCount); // 실제 같은 덱의 새 손패와 드로우 수 일치
            Assert.AreEqual(0, ally.CurrentBlock); // 실제 다음 아군 차례의 원래 남은 방어도 정리
            Assert.AreEqual(7, enemy.CurrentBlock); // 실제 적 차례가 건너뛰어 아직 남은 적 방어도
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.HealingReceivedUp)); // 기존 아군 종료 상태의 정확한 한 차례 진행
            Assert.AreEqual(2, enemy.GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown)?.RemainingTurns); // 적 차례 없음으로 적 상태의 기존 수명 유지
            CollectionAssert.AreEqual(new[] { (false, BattleTurnPhase.PlayerTurn, initialRound), (true, BattleTurnPhase.PlayerTurn, initialRound + 1) }, phases); // 정상 아군 종료와 새 아군 시작 사이에 적 이벤트 없음

            var extraCoin = fight.View("TIME_COIN"); // 추가 차례에 되돌아온 실제 동전 원본
            extraCoin.OnPointerClick(null); // 추가 차례에서 무한 예약을 시도하는 기존 손패 입력
            yield return null; // 실물과 비용이 남았는지 확인할 원래 프레임
            Assert.AreEqual(initialPoints - 2, fight.Points.CurrentActionPoints); // 추가 아군 턴의 재예약에 행동력 지급 없음
            CollectionAssert.Contains(fight.Deck.Hand, extraCoin.RuntimeCard); // 재예약 거부로 실제 동전 보존
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 추가 아군 차례의 실제 종료 입력
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turns.CurrentPhase); // 다음 차례는 원래 실제 적 턴
            Assert.AreEqual(initialPoints, fight.Points.MaxActionPoints); // 임시 최대치가 적 차례 이후까지 이월되지 않음
            CollectionAssert.AreEqual(new[] { (false, BattleTurnPhase.PlayerTurn, initialRound), (true, BattleTurnPhase.PlayerTurn, initialRound + 1), (false, BattleTurnPhase.PlayerTurn, initialRound + 1), (true, BattleTurnPhase.EnemyTurn, initialRound + 1) }, phases); // 추가 차례 종료 뒤에만 자연 적 시작
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 시작과 종료가 실제로 완료된 상태
            Assert.AreEqual(initialPoints, fight.Points.CurrentActionPoints); // 이후 정상 아군 차례 기본 행동력 회복
            Assert.AreEqual(1, enemy.GetStatusEffect(BattleStatusEffectType.GoldenLureAttackPercentDown)?.RemainingTurns); // 한 번의 실제 적 종료에만 기간 감소
            yield return fight.Use("TIME_COIN"); // 실제 적 턴 완료 이후 새 동전의 정상 결제 및 예약
            Assert.AreEqual(initialPoints - 4, fight.Points.CurrentActionPoints); // 전투 전체 한 번으로 잘못 제한하지 않는 새 카드 사용
        } // 원래 카드의 완료와 새 단계 알림 관찰 종료
        yield return new ExitPlayMode(); // 실제 게임 검사와 사본 손패 상태 정리
    } // 실제 턴 경계 검사 종료

    [UnityTest] // 원래 다음 아군 턴 보상과 동전의 독립 비용 적용
    public IEnumerator TimeCoinUsesExistingDrawAndActionPointReservations() // 룬 장벽이 예약한 다음 실제 아군 차례의 보상 비교
    { // 별개 예약의 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 원본 카드와 풀 턴 처리 사용
        using (var fight = new Fight("TIME_COIN", "RUNE_BARRIER")) // 시간 동전과 독립 다음 아군 턴의 룬 장벽 준비
        { // 원래 예약 두 종류의 출발 상태 확인 시작
            int startingPoints = fight.Points.CurrentActionPoints; // 현재 공용 최대치의 기본값
            yield return fight.Use("RUNE_BARRIER"); // 다음 실제 아군 차례의 한 점을 주는 놀이 원본 사용
            yield return fight.Use("TIME_COIN"); // 현재 차례 완료 이후 추가 차례의 독립 두 점 차감 예약
            Assert.AreEqual(startingPoints - 7, fight.Points.CurrentActionPoints); // 먼저 사용한 두 실제 카드의 합산 기본 비용
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 다음 실제 아군 차례 시작 및 두 예약 정상 정산
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turns.CurrentPhase); // 룬 장벽의 실제 보상이 받은 턴은 동전 추가 차례
            Assert.AreEqual(startingPoints - 1, fight.Points.MaxActionPoints); // 원래 장벽 증가 하나와 동전 두 감소의 독립 합산
            Assert.AreEqual(startingPoints - 1, fight.Points.CurrentActionPoints); // 현재 실제 가용 행동력에도 같은 보정
            Assert.AreEqual(2, fight.Turns.LastDrawnCardCount); // 일반 손패 초기화를 거치는 실제 추가 차례의 카드 두 장
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 예약을 실행한 추가 턴과 실제 적 차례 종료
            Assert.AreEqual(startingPoints, fight.Points.MaxActionPoints); // 사용한 두 임시 효과의 이후 턴 이월 없음
        } // 원래 예약 두 종류의 출발 상태 확인 종료
        using (var fight = new Fight("TIME_COIN")) // 결과 없이 전투 객체를 해제할 별도 실제 교전
        { // 추가 차례의 임시 상한 해제 검사 시작
            int regularCapacity = fight.Points.MaxActionPoints; // 일반 전투의 원래 공용 최대치
            yield return fight.Use("TIME_COIN"); // 카드 실제 사용으로 별개 추가 차례 예약
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 추가 아군 차례를 활성화
            Assert.AreEqual(regularCapacity - 2, fight.Points.MaxActionPoints); // 전투 해제 직전 원래 임시 대가 확인
            fight.Turns.Dispose(); // 승패 전 실제 장면 종료와 같은 전투 객체 해제
            Assert.AreEqual(regularCapacity, fight.Points.MaxActionPoints); // 끝난 교전의 시간 동전 상한 회수
        } // 추가 차례의 임시 상한 해제 검사 종료
        yield return new ExitPlayMode(); // 원래 게임 자료와 실제 검사 정리
    } // 별개 예약의 실제 전투 검사 종료
} // 카드의 추가 턴 검사 종료
