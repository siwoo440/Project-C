using System.Collections; // 실제 플레이 모드의 카드 연출 대기
using System.Linq; // 현재 손패와 실제 저장 카드 후보 비교
using System.Reflection; // 별도 편집기 검사에서 원래 내부 카드 흐름을 공개 범위 변경 없이 호출
using NUnit.Framework; // 실제 카드 비용과 소유권 검증
using UnityEngine; // 현재 출시 원본과 검사 실물 생성
using UnityEngine.TestTools; // 전투 플레이의 실제 실행 환경
using Fight = WinterfallSwordCombatTests.Fight; // 정상 손패 입력과 실제 전투 도구

public sealed class WinterfallPentacleCardFlowTests // 고대 계산식과 운명의 재작성 카드 이동 검사
{ // 실제 카드 입력의 회귀 검사 시작
    [UnityTest] // 이미 뽑은 카드와 이번 효과 및 이후 실제 드로우의 비용 확인
    public IEnumerator AncientFormulaChargesPhysicalDrawsForOnlyTheCurrentAllyTurn() // 전체 회차 가격과 실물 표시의 동일한 증가 확인
    { // 실제 전투의 아군 턴과 손패 드로우 검사 시작
        yield return new EnterPlayMode(); // 정상 씬 기반 카드 사용 경로 진입
        using (var fight = new Fight("ANCIENT_FORMULA", "THRUST", "CALCULATED_INVESTMENT", "KNEE_SLASH", "CHARGE", "DOUBLE_SLASH", "BLOOD_EDGE")) // 앞선 기본 드로우와 남은 실제 덱을 구성할 원본
        { // 여러 차례의 물리 드로우 준비 시작
            var source = fight.View("ANCIENT_FORMULA").RuntimeCard; // 실제 사용할 고대 계산식 원본
            var early = fight.View("THRUST").RuntimeCard; // 계산식보다 먼저 같은 턴에 뽑은 일반 카드
            var variable = fight.View("CALCULATED_INVESTMENT").RuntimeCard; // 전액 지불과 기본 최소가 독립인 동일 손패
            int earlyCost = early.ApCost; // 기존 같은 실물의 실제 AP 수치
            int points = fight.Points.CurrentActionPoints; // 원래 카드 사용 전에 저장할 행동력
            int variableCost = variable.ApCost; // 원래 전액 카드의 표시 계산 기준
            foreach (var id in new[] { "KNEE_SLASH", "CHARGE", "DOUBLE_SLASH", "BLOOD_EDGE" }) // 정상 뽑을 더미를 구성할 기존 물리 카드 선정
            { // 원래 일반 손패 카드의 버리기 시작
                Assert.IsTrue(fight.Deck.DiscardCard(fight.View(id).RuntimeCard)); // 실제 기존 손패 하나를 정상 버림 위치로 이동
            } // 원래 일반 손패 카드의 버리기 종료
            var draw = fight.Deck.PrepareDrawChoices(4).ToArray(); // 실제 버린 더미를 재구성한 원래 뽑기 순서 조회
            var drawCosts = draw.ToDictionary(card => card, card => card.ApCost); // 기존 실제 물리 카드별 지불 비용 보관
            yield return fight.Use("ANCIENT_FORMULA"); // 정상 클릭과 연출 완료 뒤 세 점 행동력 지불
            Assert.AreEqual(points - 3, fight.Points.CurrentActionPoints); // 고대 계산식의 실제 고정 카드 지불 확인
            Assert.AreEqual(5, fight.Deck.HandCount); // 사용 원본을 빼고 정상 세 장만 뽑은 최종 손패 수
            Assert.AreEqual(earlyCost + 1, early.ApCost); // 카드 사용보다 앞서 실제 뽑은 현재 아군 턴의 비용 증가
            Assert.AreEqual((earlyCost + 1).ToString(), early.CostLabel); // 실제 손패 표시와 결제 AP 동일 확인
            Assert.AreEqual(variableCost, variable.ApCost); // 별도의 전액 카드 계산의 변동 없음 확인
            Assert.AreEqual("2+", variable.CostLabel); // 전액 카드의 실제 기존 AP2 하한 표시 유지
            CollectionAssert.IsSubsetOf(draw.Take(3).ToArray(), fight.Deck.Hand); // 오른쪽 첫 세 실제 물리 카드만 정상 드로우했는지 확인
            Assert.IsTrue(draw.Take(3).All(card => card.ApCost == drawCosts[card] + 1)); // 이번 효과로 받은 실제 기본 비용 증가 확인
            Assert.AreEqual(1, fight.Deck.DrawCards(1)); // 뒤에 실제로 새 드로우되는 남은 물리 카드
            Assert.AreEqual(drawCosts[draw[3]] + 1, draw[3].ApCost); // 사용 뒤 같은 턴의 드로우에도 같은 수치 확인
            Assert.IsTrue((bool)InvokeDeck(fight.Deck, "TryPlayAndGenerateCard", draw[0], early.SourceData)); // 정상 드로우를 거치지 않는 일반 비용의 전투 카드 직접 생성
            var generated = fight.Deck.Hand.Single(card => card != early && card.SourceData == early.SourceData); // 원래 실물과 이름만 같은 생성 실물 조회
            Assert.AreEqual(earlyCost, generated.ApCost); // 실제 드로우 이벤트 없는 일반 카드에는 가격 증가 미적용
            InvokeDeck(fight.Deck, "ApplyAncientFormulaSurcharge", 1); // 같은 턴의 두 번째 동일 효과가 중첩되지 않는 계약 확인
            Assert.AreEqual(earlyCost + 1, early.ApCost); // 실제 같은 실물을 두 번 더 높이지 않음 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, source); // 원래 사용 카드의 정상 정산 위치 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 아군 턴 경계와 다음 차례 진입
            Assert.AreEqual(earlyCost, early.ApCost); // 다음 실제 아군 턴에서 원래 카드 지불료 복원 확인
            Assert.IsTrue(draw.All(card => card.ApCost == drawCosts[card])); // 실제로 뽑은 카드의 원래 요금이 모든 위치에서 복원됐는지 확인
        } // 여러 차례의 물리 드로우 준비 종료
        yield return new ExitPlayMode(); // 실제 전투 및 드로우 검사 환경 종료
    } // 실제 전투의 아군 턴과 손패 드로우 검사 종료

    [UnityTest] // 실제 비용 지불 후 기존 버림과 휘발 및 다른 전설 두 장 생성
    public IEnumerator RewriteFateUsesNativeDiscardAndExclusionThenCreatesTwoDistinctLegendaryCards() // 독립 전설 원본과 전투 전용 추적 확인
    { // 실제 전설 생성과 휘발 상태 검사 시작
        yield return new EnterPlayMode(); // 게임과 같은 정상 전투 상태 진입
        yield return VerifyRewriteFate(); // 모드 전환이 끝난 뒤 임시 카드와 실제 덱을 독립 구성
        yield return new ExitPlayMode(); // 전설 생성 검사에서 전투 환경 종료
    } // 실제 전설 생성과 휘발 상태 검사 종료

    private static IEnumerator VerifyRewriteFate() // 같은 전투에서 버림과 전설 생성 검증
    { // 모드 전환 이후 휘발 카드 테스트 준비 시작
        var volatileData = ScriptableObject.CreateInstance<CardData>(); // 위험 효과 테스트를 위한 실제 휘발 카드 원본
        Assert.IsNotNull(volatileData); // 테스트에 추가할 휘발 카드 데이터 확인
        JsonUtility.FromJsonOverwrite("{\"cardId\":\"DAY279_VOLATILE_TEST\",\"displayName\":\"휘발 검사\",\"volatileInHand\":true}", volatileData); // 사용자가 현재 손패에서 잃을 실제 휘발 규칙 지정
        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 전투 및 저장에 등록된 카드 원본 조회
        Assert.IsNotNull(catalog); // 출시 카드 목록 로드 확인
        CardData rewrite = catalog.Cards.Single(card => card.CardId == "CRD_SHEET_REWRITE_FATE"); // 실제 게임의 운명의 재작성 원본
        CardData thrust = catalog.Cards.Single(card => card.CardId == "CRD_SHEET_THRUST"); // 정상 일반 손패의 원래 카드
        CardData charge = catalog.Cards.Single(card => card.CardId == "CRD_SHEET_CHARGE"); // 두 번째 일반 버림 카드 원본
        try // 테스트에서 만든 휘발 카드 원본 정리 보장
        { // 실제 버림 이벤트와 제외 이벤트 측정 시작
            using var fight = new Fight(new[] { rewrite, thrust, volatileData, charge }); // 일반 버림과 휘발 카드가 섞인 가득 찬 실제 손패
            var source = fight.View("REWRITE_FATE").RuntimeCard; // 실제 카드 행동을 발생시킬 전설 카드 원본
            var earlier = fight.View("THRUST").RuntimeCard; // 기존 버림 경로에서 제거할 실제 실물 하나
            var volatilized = fight.Deck.Hand.Single(card => card.SourceData == volatileData); // 실제 현재 손패에서 제외할 휘발 카드
            int excluded = 0, discarding = 0; // 실제 휘발과 일반 효과 버림의 신호 집계
            ObserveDeck(fight.Deck, "CardExcluded", (System.Action<CardInstance, bool>)((card, fromVolatileHand) => excluded += card == volatilized && fromVolatileHand ? 1 : 0)); // 정확한 휘발 실물 원래 제외 신호만 집계
            ObserveDeck(fight.Deck, "BeforeHandDiscard", (System.Action<CardInstance>)(_ => discarding++)); // 원래 다른 카드의 실제 효과 버림 직전 신호 조회
            int points = fight.Points.CurrentActionPoints; // 실제 원래 전설 카드 사용 비용 기준
            yield return fight.Use("REWRITE_FATE"); // 정상 손패 입력과 행동력 지불 및 효과까지 실행
            Assert.AreEqual(points - 5, fight.Points.CurrentActionPoints); // 실제 비용을 한 번만 지불 확인
            Assert.AreEqual(2, fight.Deck.HandCount); // 두 실제 다른 전설 카드만 생성된 손패 확인
            Assert.AreEqual(2, fight.Deck.Hand.Select(card => card.SourceData.CardId).Distinct().Count()); // 서로 다른 두 원본 이름의 실제 카드 생성 확인
            foreach (var created in fight.Deck.Hand) // 선택한 두 전설의 원본과 소유자 비교
            { // 실제 저장 원본과 생성 카드 속성 확인 시작
                Assert.AreSame(source.OwnerUnit, created.OwnerUnit); // 실제 시전자의 원래 카드 소유권 부여 확인
                Assert.AreNotSame(rewrite, created.SourceData); // 같은 전설 주문 자신의 순환 생성 제외 확인
                Assert.AreEqual("전설", CardSheetCatalog.Load().FindByName(created.SourceData.DisplayName)?.Rarity); // 실제 시트에 있는 전설 카드인지 확인
                CollectionAssert.Contains(catalog.Cards, created.SourceData); // 저장에 등록한 기존 전설 원본에서 실제 생성 확인
                Assert.AreEqual(0, created.UpgradeLevel); // 독립 신규 실물의 기존 강화 없음 확인
            } // 실제 저장 원본과 생성 카드 속성 확인 종료
            Assert.AreEqual(2, discarding); // 원래 일반 두 카드의 별개 버림 신호 확인
            Assert.AreEqual(1, excluded); // 손패 휘발 카드의 원래 제외 신호 한 번 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, earlier); // 다른 일반 카드의 정상 효과 버림 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, source); // 사용 원본의 정상 사용 버림 위치 확인
            CollectionAssert.Contains(fight.Deck.ExcludedPile, volatilized); // 휘발의 일반 버림 아닌 실제 전투 제외 확인
            InvokeDeck(fight.Deck, "ClearBattleRelicCards"); // 실제 종료 메커니즘의 전투 전용 실물 제거 호출
            Assert.Zero(fight.Deck.HandCount); // 전설 생성 카드의 회차 덱 유출 방지 확인
            Assert.IsTrue(fight.Deck.IsStateValid()); // 모든 원래 실물이 정확히 한 위치에 있는지 확인
        } // 실제 버림 이벤트와 제외 이벤트 측정 종료
        finally // 테스트 휘발 카드 자료 해제
        { // 테스트 원본 정리 시작
            Object.Destroy(volatileData); // 새 회차에 필요 없는 검사 데이터 제거
        } // 테스트 원본 정리 종료
    } // 모드 전환 이후 휘발 카드 테스트 종료

    [UnityTest] // 두 장을 담을 수 없는 손패 상한의 사전 거절
    public IEnumerator RewriteFateRejectsInsufficientCapacityBeforePayingOrMovingCards() // 진입 실패에서 실제 비용 및 카드 보존
    { // 행동력 지불 전 공간 검사 시작
        yield return new EnterPlayMode(); // 원래 일반 손패 입력에서 지불 가능 여부 확인
        using (var fight = new Fight("REWRITE_FATE")) // 실제 상한이 한 장인 최소 검사 전투
        { // 전설 카드 두 장이 들어갈 수 없는 원래 상태 유지 비교 시작
            var source = fight.View("REWRITE_FATE").RuntimeCard; // 실제 사용할 수 없는 물리 원본
            int points = fight.Points.CurrentActionPoints; // 성공 결제 전 보존할 현재 행동력
            fight.View("REWRITE_FATE").OnPointerClick(null); // 기존 카드의 실제 사용 입력 전달
            yield return null; // 취소된 일반 클릭 정리 완료 대기
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 생성 불가능 상태에는 실제 비용 지불 없음 확인
            Assert.AreSame(source, fight.Deck.Hand.Single()); // 다른 원본이나 카드의 이동이 생기지 않았는지 확인
            Assert.IsFalse(fight.Runner.IsBusy); // 비용 없이 실제 카드 연출 시작도 거절 확인
        } // 전설 카드 두 장이 들어갈 수 없는 원래 상태 유지 비교 종료
        yield return new ExitPlayMode(); // 검증한 고유 한 장 손패 환경 정리
    } // 행동력 지불 전 공간 검사 종료

    private static object InvokeDeck(BattleDeckRuntime deck, string name, params object[] arguments) // 편집기 검사에서 원래 내부 전투 메서드 호출
    { // 별도 검사 어셈블리의 실제 게임 메서드 연결 시작
        var method = typeof(BattleDeckRuntime).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic); // 공개 범위 변경 없이 이름과 원래 인자 조회
        Assert.IsNotNull(method, name); // 실제 규칙의 내부 함수 변경이나 누락 확인
        return method.Invoke(deck, arguments); // 정상 덱 원본과 인자로 실제 메서드 실행
    } // 별도 검사 어셈블리의 실제 게임 메서드 연결 종료

    private static void ObserveDeck(BattleDeckRuntime deck, string eventName, System.Delegate handler) // 원래 내부 덱 알림에 실제 검사 콜백 연결
    { // 별도 검사 어셈블리의 실제 이벤트 구독 시작
        var signal = typeof(BattleDeckRuntime).GetEvent(eventName, BindingFlags.Instance | BindingFlags.NonPublic); // 원래 네이티브 손패 이벤트 조회
        var subscribe = signal?.GetAddMethod(true); // 공개 범위 변경 없이 실제 구독자를 추가하는 메서드 조회
        Assert.IsNotNull(subscribe, eventName); // 게임 이벤트 이름과 구독 경로의 실제 유효성 확인
        subscribe.Invoke(deck, new object[] { handler }); // 실제 덱의 원래 알림을 독립 검사에서 관찰
    } // 별도 검사 어셈블리의 실제 이벤트 구독 종료
} // 실제 카드 입력의 회귀 검사 종료
