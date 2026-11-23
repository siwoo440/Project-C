using System; // 실제 유물과 전투 객체의 독립 수명
using System.Collections; // 원래 카드 연출의 실제 진행 대기
using System.Linq; // 실제 선택한 아군의 화면 검색
using NUnit.Framework; // 회복과 상태 수명 및 명중 경계 확인
using UnityEngine; // 실제 손패와 유물 리소스 조회
using UnityEngine.TestTools; // 원래 플레이 모드의 Unity 검증
using Fight = WinterfallSwordCombatTests.Fight; // 실제 손패와 성배 및 파티를 가진 기존 전투

public sealed class WinterfallSwampRecoveryRelicTests // 방어도 회복과 실제 카드 수신의 늪지 유물 검사
{ // 정식 당시 카드와 차례 처리의 회귀 검증 시작
    [UnityTest] // 아군 두 명의 실제 남은 방어도와 한 그룹 턴 종료 확인
    public IEnumerator MudGolemRestoresEachInjuredBlockingAllyBeforeTheRealTurnClear() // 적 턴과 시간 동전의 별개 경계 검사
    { // 실제 두 번의 아군 종료 및 일반 적 턴 확인 시작
        yield return new EnterPlayMode(); // 실제 Unity 유물과 전투 카드 환경 입장
        using (var owned = new OwnedRelic("MudGolemCoreMud")) // 준비된 원래 심토의 실제 획득
        using (var fight = new Fight("WATER_OF_LIFE")) // 실제 아군 세 명과 별개 적이 있는 교전
        { // 방어도가 다른 아군의 실제 체력 비교 시작
            var first = fight.Allies[0]; // 실제 양수 방어도가 남는 첫 부상 아군
            var second = fight.Allies[1]; // 방어도가 없는 다른 부상 아군
            var third = fight.Allies[2]; // 방어도는 있지만 최대 체력인 또 다른 아군
            var enemy = fight.Enemies[0]; // 같은 효과에서 제외되는 실제 부상 적
            first.ApplyPersistentHealth(first.MaxHealth - 10); // 실제 회복 세 점이 들어갈 첫 테스트 체력
            second.ApplyPersistentHealth(second.MaxHealth - 10); // 방어도 유무를 비교할 둘째 테스트 체력
            enemy.TakeDamage(10, BattleDamageType.None); // 다른 진영도 같은 실제 부상 조건 적용
            first.GainBlock(2); // 기존 아군의 실제 양수 방어도
            third.GainBlock(2); // 체력이 이미 가득 찬 실제 아군의 방어도
            enemy.GainBlock(2); // 적 차례까지 양수 방어도가 남는 대조 유닛
            int enemyBefore = enemy.CurrentHealth; // 실제 적의 원래 부상 저장
            int firstHealCount = 0; // 같은 종료에서 중복 치유가 없음을 확인할 횟수
            first.HealthRestored += (_, _) => firstHealCount++; // 다른 회복과 구분할 실제 회복 알림
            Assert.AreEqual(1, fight.Turns.QueueTimeCoinTurn(2)); // 적을 거치지 않는 실제 추가 아군 차례 예약
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 아군 종료와 별개 추가 차례 진입
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turns.CurrentPhase); // 적 차례를 생략해 새 실제 아군 차례 확인
            Assert.AreEqual(first.MaxHealth - 7, first.CurrentHealth); // 방어도 정리 전에 실제 세 점 회복
            Assert.AreEqual(1, firstHealCount); // 같은 아군 차례 종료와 추가 재시작 중복 없음
            Assert.AreEqual(0, first.CurrentBlock); // 새 실제 아군 차례에서 원래대로 방어도 제거
            Assert.AreEqual(second.MaxHealth - 10, second.CurrentHealth); // 기존 방어도 없는 부상 아군 회복 제외
            Assert.AreEqual(third.MaxHealth, third.CurrentHealth); // 최대 체력의 원래 상한 유지
            Assert.AreEqual(enemyBefore, enemy.CurrentHealth); // 같은 부상과 방어도 적에게 적용 제외
            first.GainBlock(1); // 실제 추가 아군 차례에 다시 얻은 양수 방어도
            second.GainBlock(1); // 다음 종료에서 함께 회복할 두 번째 부상 아군
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 새 실제 아군 차례의 한 번 종료
            Assert.AreEqual(first.MaxHealth - 4, first.CurrentHealth); // 두 번째 실제 종료에서 별도로 세 점 회복
            Assert.AreEqual(second.MaxHealth - 7, second.CurrentHealth); // 같은 아군 종료의 다른 생존 수신자도 세 점
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 원래 적 차례 종료 및 방어도 처리
            Assert.AreEqual(2, firstHealCount); // 적 차례까지 마친 뒤 추가 회복 없음
            Assert.AreEqual(enemyBefore, enemy.CurrentHealth); // 기존 부상 적의 원래 체력 보존
        } // 방어도가 다른 아군의 실제 체력 비교 종료
        yield return new ExitPlayMode(); // 원래 다음 테스트의 Unity 모드 복구
    } // 실제 두 번의 아군 종료 및 일반 적 턴 확인 종료

    [UnityTest] // 단일 및 전체 치유 카드와 순수 체력 회복의 실제 차이 검사
    public IEnumerator FairyDustFollowsValidSingleAndGroupRecipientsUntilTheEnemyFinishes() // 최대 체력과 전투불능 및 지속 회복을 포함한 판정
    { // 실제 성배 원본 두 종류의 아군 선택과 적 차례 확인 시작
        yield return new EnterPlayMode(); // 실제 원래 카드, 유물 및 아군 화면 준비
        using (var owned = new OwnedRelic("SwampFairyWingDust")) // 정식 늪의 요정 날개가루의 실제 획득
        using (var canyon = new OwnedRelic("CanyonWindWhistle")) // 전투 시작의 별개 여덟 퍼센트포인트 회피
        using (var fight = new Fight("WATER_OF_LIFE", "ARCHANGEL_BLESSING")) // 단일 및 여러 아군의 실제 원본 회복 카드
        { // 실제 세 아군의 생존과 치유 및 적 명중 검사 시작
            var full = fight.Allies[0]; // 처음 단일 회복을 받는 최대 체력 아군
            var hurt = fight.Allies[1]; // 일반 회복과 그룹 치유를 비교할 부상 아군
            var fallen = fight.Allies[2]; // 단일 선택 및 모든 치유에서 제외될 전투불능 아군
            var enemy = fight.Enemies[0]; // 실제 적 명중률의 원래 공격자
            var effect = (BattleStatusEffectType)98; // 데이터에 등록되는 늪 요정의 독립 실제 상태 번호
            fallen.ApplyPersistentHealth(0); // 실제 전투불능 상태인 치유 불가 수신자
            hurt.ApplyPersistentHealth(hurt.MaxHealth - 10); // 직접 카드 외의 기존 회복을 관찰할 실제 부상
            hurt.RestoreHealth(1); // 직접 카드가 아닌 원래 런타임 회복
            Assert.IsNull(hurt.GetStatusEffect(effect)); // 단순 체력 회복의 요정 강화 제외
            var cardView = fight.View("WATER_OF_LIFE"); // 실제 손패의 원본 회복 카드
            int beforeInvalid = fight.Points.CurrentActionPoints; // 잘못된 치유 선택 전에 원래 행동력 저장
            cardView.OnPointerClick(null); // 손패에서 지정 카드 선택
            FindView(fight, fallen).OnPointerClick(null); // 전투불능 아군에게 실제 지정 시도
            Assert.AreEqual(beforeInvalid, fight.Points.CurrentActionPoints); // 잘못된 대상에 비용이나 부여 미발생
            Assert.IsNull(fallen.GetStatusEffect(effect)); // 전투불능 아군에게 새 회피 없음
            fight.Controller.CancelSelection(); // 실패 선택 뒤 정상 카드 사용으로 진행
            yield return WinterfallCommonCardExpansionTests.Play(cardView.RuntimeCard.SourceData, fight.Hand, FindView(fight, full), fight.Runner, fight.Deck, fight.Points); // 사용이 확정된 최대 체력의 실제 단일 카드 수신자
            Assert.AreEqual(full.MaxHealth, full.CurrentHealth); // 원래 체력이 그대로여도 카드 대상은 유효
            Assert.AreEqual(10, full.GetStatusEffect(effect)?.EffectiveValue); // 대상별 십 퍼센트포인트 즉시 강화
            Assert.AreEqual(82, BattleHitChance.GetPercent(enemy, full)); // 실제 협곡의 여덟과 요정의 십을 한 번씩 합산
            Assert.IsNull(hurt.GetStatusEffect(effect)); // 단일 카드의 다른 생존 아군은 제외
            yield return fight.Use("ARCHANGEL_BLESSING"); // 실제 전체 아군 회복 원본 사용
            Assert.AreEqual(10, full.GetStatusEffect(effect)?.EffectiveValue); // 같은 유물 상태의 두 번째 대상 부여는 한 중첩
            Assert.AreEqual(10, hurt.GetStatusEffect(effect)?.EffectiveValue); // 전체 치유의 실제 다른 생존 대상도 부여
            Assert.IsNull(fallen.GetStatusEffect(effect)); // 부활하지 않은 대상은 그룹 치유에서도 제외
            Assert.AreEqual(1, fight.Turns.QueueTimeCoinTurn(2)); // 실제 적이 없는 추가 아군 차례 준비
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 실제 종료와 시간 동전 전환
            Assert.AreEqual(1, full.GetStatusEffect(effect)?.RemainingTurns); // 추가 아군 차례에서 적 기준 수명 보존
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 그 뒤 정상 적 차례 진입
            Assert.AreEqual(82, BattleHitChance.GetPercent(enemy, full)); // 실제 적이 행동할 동안 원래 두 회피 유지
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 차례 실제 종료
            Assert.IsNull(full.GetStatusEffect(effect)); // 한 실제 적 차례 뒤 요정 날개 회피 만료
            Assert.IsNull(hurt.GetStatusEffect(effect)); // 실제 모든 치유 대상의 같은 종료 경계
            Assert.AreEqual(92, BattleHitChance.GetPercent(enemy, full)); // 별개인 협곡의 두 번째 적 차례 회피 보존
        } // 실제 세 아군의 생존과 치유 및 적 명중 검사 종료
        yield return new ExitPlayMode(); // 현재 사용한 실제 전투 검사 환경 정리
    } // 실제 성배 원본 두 종류의 아군 선택과 적 차례 확인 종료

    private static BattleUnitView FindView(Fight fight, BattleUnitRuntime unit) // 원래 플레이어가 누르는 실제 아군 화면 조회
    { // 같은 런타임인지 확인하는 원래 화면 검색 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == unit); // 출전한 한 아군의 실제 기존 입력 컴포넌트
    } // 같은 런타임인지 확인하는 원래 화면 검색 종료

    private sealed class OwnedRelic : IDisposable // 실제 회차에서 유물을 일시 소유하는 검사 범위
    { // 원래 유물을 획득한 실제 상태 보관 시작
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 게임의 실제 회차 유물 관리
        private readonly RelicData relic; // 이 검사에 들어가는 원본 리소스

        internal OwnedRelic(string stem) // 정식 Unity 리소스 이름의 실제 기존 획득
        { // 실제 보유 인벤토리에 지급 시작
            relic = Resources.Load<RelicData>("Relics/" + stem); // 실제 게임에 등록된 유물 데이터 조회
            Assert.IsNotNull(relic, stem); // 누락된 리소스로 검사를 우회하지 않는 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 신규 카드 사용 전 유물 보유
        } // 실제 보유 인벤토리에 지급 종료

        public void Dispose() => manager.TryRemove(relic.RelicId); // 사용했던 실제 유물만 보유에서 정리
    } // 원래 유물을 획득한 실제 상태 보관 종료
} // 정식 당시 카드와 차례 처리의 회귀 검증 종료
