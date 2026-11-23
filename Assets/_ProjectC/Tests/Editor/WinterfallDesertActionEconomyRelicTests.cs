using System; // 독립 전투와 유물 원본의 해제
using System.Collections; // 실제 플레이 모드와 카드 연출 대기
using System.Linq; // 동일 원본의 실제 카드 인스턴스 검색
using NUnit.Framework; // 비교 수치와 실제 효과 확인
using UnityEngine; // 게임 원본 리소스와 실제 직렬화
using UnityEngine.TestTools; // 플레이 모드 회귀 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 기존 원래 카드 입력을 사용하는 전투
using Object = UnityEngine.Object; // 검사 원본만 해제

public sealed class WinterfallDesertActionEconomyRelicTests // 사막 카드 비용과 실제 행동력 감소 회귀
{ // 두 유물의 실전 검사 시작
    [UnityTest] // 조우와 카드 인스턴스별 비용 유지 검사
    public IEnumerator CrownDiscountsEachPhysicalCardInDesertOnlyAndRecyclingDoesNotResetUsage() // 같은 원본 두 장과 실제 다른 지불 비용 비교
    { // 실제 카드 및 전투를 사용할 모드 시작
        yield return new EnterPlayMode(); // 실제 회차 상태와 보유 유물 초기화
        using (var owned = new OwnedRelic("WindWornCrown")) // 게임에서 사용하는 실제 바람 왕관 장착
        { // 두 지역의 실물 카드 비용 비교 시작
            var charge = LoadCard("CHARGE"); // 감소 가능한 일반 검 카드 원본
            var investment = LoadCard("CALCULATED_INVESTMENT"); // 인쇄된 최소 비용을 가진 실제 전액 카드
            using (var normal = new Trial(ExplorationRegion.Winterfall, charge)) // 지역을 지정하지 않은 기존 일반 전투
            { // 일반 조우 비용 확인 시작
                Assert.AreEqual(charge.ApCost, normal.Deck.Hand.Single().ApCost); // 사막이 아닌 조우에서 왕관 할인 없음
            } // 일반 조우 비용 확인 종료
            using (var desert = new Trial(ExplorationRegion.Desert, charge, charge, investment)) // 명시 사막의 실제 동일 카드 두 장과 투자 카드
            { // 사막에서 물리 카드의 첫 사용과 비용 확인
                var copies = desert.Deck.Hand.Where(card => card.SourceData == charge).ToArray(); // 원본은 같고 참조는 다른 두 실물
                var original = copies[0]; // 첫 번째로 확정할 실제 카드
                var second = copies[1]; // 아직 사용하지 않고 일반 버릴 별개 카드
                var xCost = desert.Deck.Hand.Single(card => card.SourceData == investment); // 처음부터 고정 기본 할인 대상이 아닌 전액 비용 카드
                Assert.AreNotSame(original, second); // 같은 원본의 고유 인스턴스 확인
                Assert.AreEqual(charge.ApCost - 1, original.ApCost); // 첫 사용 전 실제 비용 할인
                Assert.AreEqual(charge.ApCost - 1, second.ApCost); // 다른 사본의 독립 첫 할인
                Assert.AreEqual("2+", xCost.CostLabel); // 전액 지불의 원래 최소 표시 보존
                Assert.AreEqual(investment.ApCost, xCost.ApCost); // 전액 카드의 기본 경로와 왕관의 분리
                Assert.IsTrue(desert.Deck.DiscardCard(second)); // 선택 카드가 아닌 일반 효과로 실제 버림
                Assert.IsTrue(desert.Deck.TryRecoverDiscard(second)); // 같은 실물만 정상 회수
                Assert.AreEqual(charge.ApCost - 1, second.ApCost); // 버림과 회수가 첫 사용을 소비하지 않음
                Assert.IsTrue(desert.Points.Spend(original.ApCost)); // 표시한 것과 같은 공용 행동력 실제 지불
                Assert.IsTrue(desert.Deck.DiscardPlayedCard(original)); // 카드 사용이 확정된 기존 정산 (명중과 무관)
                Assert.AreEqual(charge.ApCost, original.ApCost); // 실제 사용의 첫 할인이 같은 인스턴스에서 소모
                Assert.AreEqual(charge.ApCost - 1, second.ApCost); // 다른 한 장의 미사용 할인 유지
                Assert.IsTrue(desert.Deck.TryRecoverDiscard(original)); // 실제 사용한 카드의 같은 물리 회수
                Assert.AreEqual(charge.ApCost, original.ApCost); // 같은 카드의 할인으로 재활용 불가
                var newCopy = CardInstance.Create(charge, original.OwnerUnit, 39, 0); // 일반 전투에 새로 생성되는 독립 물리 카드
                Assert.AreEqual(charge.ApCost - 1, newCopy.ApCost); // 독립 실물 카드에 첫 할인 자격
                CheckSeparateCost(desert.Allies[0]); // 고정 기본 비용과 별도 추가 비용의 경계 검증
            } // 사막에서 물리 카드의 첫 사용 비용 검사 종료
        } // 두 지역의 실물 카드 비용 비교 종료
        yield return new ExitPlayMode(); // 원본 게임 상태를 복원하는 검사 종료
    } // 사막 왕관의 조우와 실물 카드 회귀 종료

    [UnityTest] // 실물 카드의 실제 다음 아군 턴 감소와 단일 유물 타격
    public IEnumerator AnkletDamageFollowsTheActualGiantsShieldReductionRatherThanItsPayment() // 카드 지불 및 다음 아군 턴의 실제 효과 구분
    { // 원래 거인의 방패의 실제 전투 입력 시작
        yield return new EnterPlayMode(); // 실제 보물 카탈로그와 턴 예약 준비
        using (var owned = new OwnedRelic("BlackSandAnklet")) // 실제 검은 모래 발목고리 장착
        using (var fight = new Fight("GIANTS_SHIELD")) // 정상 입력과 실제 방패 카드
        { // 알려진 지불자와 공용 감소 비교 시작
            var caster = fight.Allies[0]; // 공용 감소를 부여한 실제 원본 시전자
            var other = fight.Allies[1]; // 감소 대상으로 바뀌지 않아야 하는 동료
            int before = caster.CurrentHealth; // 실제 유물 타격의 시전자 기준
            int otherBefore = other.CurrentHealth; // 실제 비대상 생존 아군 기준
            yield return fight.Use("GIANTS_SHIELD"); // 실제 카드 행동력 지불과 자원 감소 예약
            Assert.AreEqual(before, caster.CurrentHealth); // 보통 카드 사용 비용에 즉시 피해 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 남은 행동력 소모와 실제 적 턴 시작
            Assert.AreEqual(before, caster.CurrentHealth); // 턴 종료의 일반 행동력 소모도 발동 없음
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 다음 아군 턴에 예약한 감소 적용
            Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 공용 현재 행동력 감소 한 점
            Assert.AreEqual(19, fight.Points.MaxActionPoints); // 공용 최대 행동력 감소 한 점
            Assert.AreEqual(before - 5, caster.CurrentHealth); // 실제 감소 당시 시전자에게만 고정 다섯 피해
            Assert.AreEqual(otherBefore, other.CurrentHealth); // 다른 파티원에게 피해를 전달하지 않음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 같은 예약의 다시 지급이 없어야 하는 다음 턴
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 정상 최대 행동력 회복 확인
            Assert.AreEqual(before - 5, caster.CurrentHealth); // 동일 예약이 두 번 발동하지 않음
        } // 알려진 지불자와 공용 감소 비교 종료
        yield return new ExitPlayMode(); // 해당 검사 범위의 실제 전투 정리
    } // 거인의 방패 원래 감소의 실전 검사 종료

    [UnityTest] // 명시 감소의 실제 조정값과 사망자 및 현재 보유 여부 확인
    public IEnumerator AnkletSkipsZeroClampedDeadAndRemovedRelicApplications() // 실제로 각 예약에 배정되는 수치의 경계
    { // 명시 감소의 실제 배정 비교 시작
        yield return new EnterPlayMode(); // 실제 파티와 보유 유물 상태 초기화
        var charge = LoadCard("CHARGE"); // 단순 원본 카드의 실제 덱 구성
        using (var owned = new OwnedRelic("BlackSandAnklet")) // 윈터폴 유물 원본과 실제 보유
        { // 현재 보유 유물의 상태별 비교 시작
            using (var trial = new Trial(ExplorationRegion.Winterfall, charge)) // 세 점 공용 행동력의 실제 최소 전투
            { // 감소가 먼저 모두 적용된 실제 예약과 넘친 예약 비교
                var first = trial.Allies[0]; // 채워진 실제 감소 배정
                var second = trial.Allies[1]; // 두 번째 요청에서 영으로 잘리는 대상
                int firstHp = first.CurrentHealth; // 실제 첫 참가자의 체력
                int secondHp = second.CurrentHealth; // 잘린 요청의 실제 두 번째 참가자 체력
                trial.Turn.QueueNextPlayerTurnActionPointLoss(first, 3); // 공용 행동력 전부를 쓸 유효 명시 감소
                trial.Turn.QueueNextPlayerTurnActionPointLoss(second, 2); // 남은 감소 가능량을 넘기는 유효 요청
                Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 같은 턴에 즉시 받지 않는 실제 예약
                Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 다음 아군 턴의 실제 감소 배분
                Assert.Zero(trial.Points.CurrentActionPoints); // 현재 행동력의 하한 유지
                Assert.AreEqual(firstHp - 5, first.CurrentHealth); // 실제 배정된 첫 요청 한 번의 타격
                Assert.AreEqual(secondHp, second.CurrentHealth); // 실제 감소가 영인 뒤 요청의 피해 없음
            } // 감소가 먼저 모두 적용된 실제 예약 비교 종료
            using (var trial = new Trial(ExplorationRegion.Winterfall, charge)) // 대상 사망의 별도 전투 준비
            { // 예전 자원 감소의 실제 보존과 사망자의 대상 제외 시작
                var former = trial.Allies[0]; // 예약 직전까지 생존했던 원래 대상
                var other = trial.Allies[1]; // 대체 대상으로 실수할 수 있는 동료
                int otherHp = other.CurrentHealth; // 원래 동료의 체력
                trial.Turn.QueueNextPlayerTurnActionPointLoss(former, 1); // 미리 확정한 원래 감소와 대상
                former.TakeDamage(former.CurrentHealth, BattleDamageType.None); // 적용 전에 원래 대상 전투불능
                Assert.IsTrue(former.IsDead); // 사망한 원래 대상 확인
                Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 동료가 남아 정상 전투 유지
                Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 일반 공용 감소는 취소하지 않고 처리
                Assert.AreEqual(2, trial.Points.CurrentActionPoints); // 사망 이후에도 공용 감소의 원래 적용
                Assert.AreEqual(otherHp, other.CurrentHealth); // 생존 동료에게 유물 피해로 바꾸어 적용하지 않음
            } // 사망한 원래 대상의 실제 제외 검사 종료
            using (var trial = new Trial(ExplorationRegion.Winterfall, charge)) // 실제 유물 보유 해제의 별도 전투
            { // 확정 시점에서 실제 보유 여부 확인 시작
                var caster = trial.Allies[0]; // 예약할 살아 있는 실제 참가자
                int health = caster.CurrentHealth; // 보유 해제 뒤 체력 기준
                trial.Turn.QueueNextPlayerTurnActionPointLoss(caster, 1); // 원래 보유 중 만들어 둔 예약 적용
                Assert.IsTrue(RelicRunManager.Instance.TryRemove(owned.Data.RelicId)); // 효과 정산 전에 실제 유물 제거
                Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 현재 보유하지 않는 적 턴 진입
                Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 제거 이후의 수치 적용
                Assert.AreEqual(2, trial.Points.CurrentActionPoints); // 유물과 무관한 명시 감소 유지
                Assert.AreEqual(health, caster.CurrentHealth); // 지급 실제 순간에 보유하지 않으면 피해 없음
            } // 현재 보유 여부 확인 종료
        } // 현재 보유 유물의 상태별 비교 종료
        yield return new ExitPlayMode(); // 개별 독립 전투 기록 정리
    } // 명시 행동력 감소의 실제 경계 검사 종료

    private static void CheckSeparateCost(BattleUnitRuntime owner) // 일반 고정 비용과 별도 행동력의 실제 독립 확인
    { // 인스턴스 비용 검사용 원본 준비
        var source = ScriptableObject.CreateInstance<CardData>(); // 시험 전용 고정 비용 카드 원본
        try // 시험에서 생성한 원본 정리 보장
        { // 기본 둘과 추가 셋의 분리 시작
            JsonUtility.FromJsonOverwrite("{\"apCost\":2,\"additionalActionPointCost\":3}", source); // 실제 카드 직렬화에 기본과 별도 비용 적용
            var card = CardInstance.Create(source, owner, 40, 0); // 실제 사막 아군의 미사용 인스턴스
            Assert.AreEqual(4, card.ApCost); // 일반 비용만 한 점 줄고 별도 행동력 셋의 전부 유지
        } // 기본 둘과 추가 셋의 분리 종료
        finally // 실제 게임 원본과 분리한 시험 자료 삭제
        { // 임시 원본 해제 시작
            Object.Destroy(source); // 현재 시험에서 작성한 카드만 해제
        } // 임시 원본 해제 종료
    } // 인스턴스 비용 검사용 원본 준비 종료

    private static CardData LoadCard(string key) // 실제 보물 카탈로그의 같은 사용 원본 조회
    { // 등록된 원본 조회 시작
        return Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(offer => offer.OfferId == "REWARD_SHEET_" + key).Card; // 플레이어가 획득하는 실제 카드
    } // 등록된 원본 조회 종료

    private sealed class OwnedRelic : IDisposable // 실제 장착과 분리된 시험 범위의 유물 관리
    { // 회차 유물 시험의 설정 시작
        internal readonly RelicData Data; // 실제 원본을 정리할 동일 참조
        internal OwnedRelic(string key) // 실제 시트에 연결된 원본 획득
        { // 게임 원본 유물 획득 시작
            Data = Resources.Load<RelicData>("Relics/" + key); // 실제 플레이어 획득 원본
            Assert.IsNotNull(Data, key); // 새 유물의 실제 에셋 연결 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(Data)); // 원래 회차의 실제 획득 처리
        } // 게임 원본 유물 획득 종료
        public void Dispose() // 이 쪽에서 취득한 실제 한 종만 해제
        { // 보유 상태 정리 시작
            RelicRunManager.Instance?.TryRemove(Data.RelicId); // 아직 보유한 경우에만 이번 획득 제거
        } // 보유 상태 정리 종료
    } // 회차 유물 시험의 설정 종료

    private sealed class Trial : IDisposable // 실제 조우의 지역 및 공용 행동력을 제어하는 전투
    { // 일반 원본과 별개의 실제 시험 전투 준비
        private readonly DeckData source; // 실제 물리 카드 생성에 사용하는 원본 덱
        private readonly EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 실제 하나의 적 원본
        internal readonly BattleUnitRuntime[] Allies; // 실제 두 아군 전투 참가자
        internal readonly BattleDeckRuntime Deck; // 실제 손패와 버림 및 회수 비용
        internal readonly BattleActionPointRuntime Points = new(3); // 실제 공용 행동력의 하한
        internal readonly BattleTurnRuntime Turn; // 실제 확정 조우와 게임 턴
        internal Trial(ExplorationRegion region, params CardData[] cards) // 조우 지역과 검사할 실제 원본 세트 전달
        { // 실제 조우 입력과 실행 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 표준 캐릭터의 실제 원본 명단
            Allies = roster.Characters.Take(2).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 두 생존 역할의 실제 아군 런타임
            var enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 단일 적 상태 구성
            source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, Allies[0].CharacterSource)).ToArray()); // 실제 물리 덱과 카드 소유자
            Deck = BattleDeckRuntime.Create(source, Allies, cards.Length, 276); // 현재 실제 손패의 인스턴스 생성
            Turn = new BattleTurnRuntime(Deck, Points, Allies, new[] { enemy }, cards.Length, BattleType.Normal, region: region); // 실제 전투에서 명시한 조우의 지역
            Assert.IsTrue(Turn.StartBattle(cards.Length)); // 실제 시작 유물과 기본 드로우 완료
        } // 실제 조우 입력과 실행 종료
        public void Dispose() // 이번 검사 전투 및 만든 원본 정리
        { // 실제 전투 상태 회수 시작
            Turn.Dispose(); // 다른 회차에 남을 비용과 명시 감소 경로 정리
            Object.Destroy(source); // 생성한 시험 덱 원본 해제
            Object.Destroy(enemyData); // 생성한 시험 적 원본 해제
        } // 실제 전투 상태 회수 종료
    } // 별도 실제 시험 전투 종료
} // 두 유물의 실전 검사 종료
