using System; // 저장 결과의 빈 적 목록과 값 비교
using System.Collections; // 실제 플레이 모드 검사 전환
using System.Collections.Generic; // 원래 손패와 정신 변화의 실제 순서 기록
using System.Linq; // 실제 저장 대상과 카드 인스턴스 조회
using System.Reflection; // 독립 편집기 어셈블리에서 게임 경계 호출
using NUnit.Framework; // 실제 전투 상태 검증
using UnityEngine; // 원본 보물 카드와 유물 에셋 조회
using UnityEngine.TestTools; // 게임의 실제 차례 흐름 실행
using Fight = WinterfallSwordCombatTests.Fight; // 원본 전투 카드와 유닛의 공통 검사 환경

public sealed class WinterfallSettlementAndCardCostRelicTests // 보존 캡슐과 AI 비용의 실제 전투 회귀
{ // 실제 유물 검사 시작
    [UnityTest] // 정상 종료 저장에 앞선 원래 회복과 정신력 변화
    public IEnumerator CapsuleSettlesNativeHealingAndMentalOnceOnVictoryOrEscape() // 승리와 도주의 결과 중복 저장 및 살아 있는 대상 확인
    { // 원본 유물 획득 및 두 종료 유형 비교 시작
        yield return new EnterPlayMode(); // 실제 Unity 게임 객체의 독립 실행 시작
        Acquire("SpecimenPreservationCapsule"); // 출시에 사용하는 보존 캡슐 소스 획득
        foreach (bool escape in new[] { true, false }) // 두 실제 정상 종료의 저장 시점 확인
        { // 개별 실제 전투 결산 시작
            using var fight = new Fight("THRUST"); // 살아 있는 아군과 실제 정식 카드가 있는 전투
            var wounded = fight.Allies[0]; // 일반 치유와 정신 변화를 받을 실제 부상자
            var full = fight.Allies[1]; // 체력 상한과 별도 정신 비용을 확인할 실제 동료
            var fallen = fight.Allies[2]; // 전투가 끝나도 부활하지 않는 이미 쓰러진 아군
            Assert.IsTrue(wounded.ApplyPersistentHealth(wounded.MaxHealth - 20)); // 일반 치유 열 점을 받을 원래 체력 설정
            Assert.IsTrue(wounded.ApplyPersistentMental(40) && full.ApplyPersistentMental(40)); // 원래 1점 치유 정신 보정과 고정 손실의 기준
            fallen.TakeDamage(fallen.MaxHealth * 3, BattleDamageType.None); // 원래 피해로 실제 전투불능 발생
            Assert.AreEqual(wounded.MaxHealth - 20, Capture(fight.Turns).AllyStates[0].CurrentHealth); // 아직 끝나지 않은 전투의 조기 캡슐 지급 없음
            if (escape) // 원래 두 종료 원인 중 도주 확인
            { // 실제 도주 결과 전달 시작
                Assert.IsTrue(fight.Turns.TryEscape()); // 일반 전투의 실제 종료 요청
            } // 실제 도주 결과 전달 종료
            else // 승리 결과 저장의 원래 경로
            { // 모든 생존 적을 실제로 쓰러뜨리는 처리 시작
                foreach (var enemy in fight.Enemies) // 현재 실제 네 적을 원래 게임 피해로 정리
                { // 실제 피해 한 번 적용 시작
                    enemy.TakeDamage(enemy.MaxHealth * 3, BattleDamageType.None); // 다른 이가 정신력 보상을 받지 않는 실제 죽음
                } // 실제 피해 한 번 적용 종료
            } // 모든 생존 적을 실제로 쓰러뜨리는 처리 종료
            Assert.AreEqual(escape ? BattleResult.Escape : BattleResult.Victory, fight.Turns.Result); // 실제로 확정한 정상 종료 종류 구분
            var mentalEvents = new List<BattleMentalChangeResult>(); // 전투 결과 직전에 들어온 원래 정신력 변화 기록
            wounded.MentalChanged += (_, result) => mentalEvents.Add(result); // 실제 정신력 이벤트로 회복과 비용 순서 기록
            int prior = wounded.CurrentMental; // 실제 결과 직전 정신력의 원래 수치
            int fullBefore = full.CurrentMental; // 체력이 가득 찬 아군의 적용 전 실제 정신력
            int fallenBefore = fallen.CurrentMental; // 쓰러진 유닛에 영향을 주지 않는지 확인할 원래 정신력
            var result = Capture(fight.Turns); // 실제 게임 저장이 사용하는 원래 결과 스냅샷 요청
            Assert.AreEqual(wounded.MaxHealth - 10, result.AllyStates[0].CurrentHealth); // 열 점의 일반 치유가 저장 데이터에 기록
            Assert.AreEqual(prior + 1 - 3, result.AllyStates[0].CurrentMental); // 원래 치유 정신 1 뒤 고정 3 감소 확인
            Assert.AreEqual(full.MaxHealth, result.AllyStates[1].CurrentHealth); // 체력 상한을 넘어가는 회복 없음
            Assert.AreEqual(fullBefore - 3, result.AllyStates[1].CurrentMental); // 체력 회복이 없는 생존자도 정신 감소 대상으로 처리
            Assert.AreEqual(fallenBefore, result.AllyStates[2].CurrentMental); // 쓰러진 아군의 정신력 유지
            Assert.IsTrue(result.AllyStates[2].IsDead); // 보존 캡슐이 아군을 부활시키지 않음
            CollectionAssert.AreEqual(new[] { BattleMentalChangeReason.HealingReceived, BattleMentalChangeReason.RelicEffect }, mentalEvents.Select(item => item.Reason)); // 일반 치유 이후 유물 정신 변화의 실제 알림 순서
            Assert.AreEqual(-3, mentalEvents[1].RequestedDelta); // 원본 정신력 세 점 감소의 실제 요청 확인
            var retry = Capture(fight.Turns); // 같은 결과를 다시 조회하거나 저장할 때의 원래 경로
            Assert.AreEqual(result.AllyStates[0].CurrentHealth, retry.AllyStates[0].CurrentHealth); // 보존 캡슐의 체력 중복 지급 방지
            Assert.AreEqual(result.AllyStates[0].CurrentMental, retry.AllyStates[0].CurrentMental); // 재저장에 따른 두 번째 정신 감소 없음
            Assert.AreEqual(2, mentalEvents.Count); // 실제 처리를 한 번만 발행한 확인
        } // 개별 실제 전투 결산 종료
        yield return new ExitPlayMode(); // 실제 결과 비교를 완료한 독립 환경 종료
    } // 원본 유물 획득 및 두 종료 유형 비교 종료

    [UnityTest] // 일반과 추가 아군 차례 최초 손패의 실제 독립 비용
    public IEnumerator FaultyAiChangesOnePhysicalFixedCardUntilTheTrueTurnEnds() // 실물 한 장과 X 카드 및 실제 추가 차례의 가격 대조
    { // 실제 아군 시작과 원본 비용 비교 시작
        yield return new EnterPlayMode(); // 정식 카드 및 유물과 원래 Unity 전투 사용
        Acquire("FaultyAiDecision"); // 출시에 사용한 AI 비용 변경 유물 등록
        using (var fight = new Fight("THRUST", "THRUST", "TIME_COIN", "INFINITE_CONTRACT")) // 같은 카드 원본의 별도 실물과 고정 및 X 카드 준비
        { // 정상 전투 시작과 실제 시간 동전 사용의 원본 가격 확인
            Adjusted(fight.Deck); // 현재 완성 손패에서 실제 가격이 바뀐 유일한 카드
            Assert.AreEqual("X", fight.View("INFINITE_CONTRACT").RuntimeCard.CostLabel); // X 카드는 기존 결제 표시 유지
            yield return fight.Use("TIME_COIN"); // 원래 현재 비용으로 실제 동전을 사용해 추가 차례 예약
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 최초 효과를 회수한 뒤 실제 아군 차례 진행
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turns.CurrentPhase); // 적 턴과 별개인 실제 추가 아군 시작 확인
            Assert.AreEqual(2, fight.Turns.CurrentRound); // 실제 새 정식 차례로 진행한 동전 확인
            Adjusted(fight.Deck); // 추가 차례의 정상 드로우 뒤 실제 실물 한 장 변경 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 차례를 끝내 임시 가격 복원
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turns.CurrentPhase); // 임시 비용이 끝난 적 턴의 실제 경계
            Assert.IsTrue(fight.Deck.Cards.All(card => card.ApCost == card.SourceData.ApCost)); // 손패 밖을 포함한 모든 실물의 원래 가격 복원
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 정상 아군 차례의 새로운 드로우 완료
            Adjusted(fight.Deck); // 실제 정상 재시작에서도 카드 한 장의 독립 가격 변경
            Assert.IsTrue(fight.Turns.TryEscape()); // 다음 턴 도중의 실제 전투 종료
            Assert.IsTrue(fight.Deck.Cards.All(card => card.ApCost == card.SourceData.ApCost)); // 결과 화면 이전 모든 실물 비용 복원
        } // 정상 전투 시작과 실제 시간 동전 사용의 원본 가격 확인 종료
        yield return new ExitPlayMode(); // Unity의 실제 차례 수명 검사 종료
    } // 실제 아군 시작과 원본 비용 비교 종료

    [UnityTest] // 실제 첫 아군 손패를 완성하는 개막 선택에 이어 한 번만 변경
    public IEnumerator OpeningBookmarkSelectionFinishesHandBeforeOneAiDecision() // 후보 표시부터 실제 선제 해제와 중복 입력까지 검증
    { // 개막 원본 선택과 실제 비용 정산 시작
        yield return new EnterPlayMode(); // 라이브 원본과 전투 객체의 독립 Unity 환경 시작
        Acquire("FaultyAiDecision"); // 첫 완료 손패의 실제 가격 변경 유물
        Acquire("DustyLibraryBookmark"); // 원래 첫 턴에 선택할 덱 위 카드 원본 유물
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 손패 후보의 원래 출전 아군 자료
        var ally = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 실제 유효한 소유자로 첫 손패 구성
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 생존 적이 있는 일반 전투 자료
        var enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 선제 실제 상태와 후속 아군 턴을 구분할 적
        var thrust = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(item => item.OfferId == "REWARD_SHEET_THRUST").Card; // 실제 유지하는 같은 고정 카드 원본
        var source = DeckData.CreateRuntime(Enumerable.Repeat(new DeckCardEntry(thrust, ally.CharacterSource), 3).ToArray()); // 같은 원본의 독립 실물 세 장을 가진 정상 덱
        try // 실제 검사에 만든 Unity 카드 및 적 원본 정리
        { // 적 선제 뒤 첫 아군 손패의 원래 선택 검사 시작
            var deck = BattleDeckRuntime.Create(source, new[] { ally }, maxHandSize: 3, shuffleSeed: 19); // 먼저 한 장을 뽑고 두 장을 후보로 남긴 원래 덱
            using var turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { ally }, new[] { enemy }, 1, BattleType.Normal); // 실제 선제와 첫 사용 잠금 처리
            Assert.IsTrue(turns.StartBattle(1, enemyFirst: true)); // 플레이어 차례 이전의 실제 선제 상태
            Assert.AreEqual(0, deck.HandCount); // 선제 중에는 첫 드로우나 카드 가격 변경 없음
            Assert.IsTrue(turns.CompleteEnemyTurn()); // 첫 아군 턴의 실제 표준 드로우와 후보 표시 시작
            Assert.AreEqual(1, deck.HandCount); // 실제 첫 카드 한 장 도착 후 북마크 선택만 대기
            Assert.AreEqual(thrust.ApCost, deck.Hand[0].ApCost); // 손패가 완성되기 전에 AI 변경 없음
            var property = typeof(BattleTurnRuntime).GetProperty("OpeningBookmarkChoices", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 게임이 표시하는 실제 선택 후보 조회 메서드
            Assert.IsNotNull(property); // 게임의 원래 후보 속성을 정확히 확인
            var choice = ((IReadOnlyList<CardInstance>)property.GetValue(turns)).First(); // 실제 덱 위에서 표시된 현재 후보
            var resolve = typeof(BattleTurnRuntime).GetMethod("ResolveOpeningBookmark", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 선택의 접근 제한을 유지한 조회
            Assert.IsNotNull(resolve); // 원래 게임의 선택 처리가 존재하는지 확인
            resolve.Invoke(turns, new object[] { choice }); // 실제 후보 선택이 손패에 들어간 뒤 AI 추첨 처리
            CollectionAssert.Contains(deck.Hand, choice); // 후보 선택의 실제 실물이 원래 손패에 들어옴
            Assert.AreEqual(2, deck.HandCount); // 선택 뒤 실제로 최종 손패 두 장
            Adjusted(deck); // 완성 손패 중 실물 한 장만 가격 변경
            int[] current = deck.Hand.Select(card => card.ApCost).ToArray(); // 정상 선택 정산의 실제 비용 기록
            resolve.Invoke(turns, new object[] { choice }); // 이전 모달에서 중복 반환한 원래 처리 재시도
            CollectionAssert.AreEqual(current, deck.Hand.Select(card => card.ApCost)); // 같은 차례 중복 처리에 따른 두 번째 변경이나 재추첨 없음
        } // 적 선제 뒤 첫 아군 손패의 원래 선택 검사 종료
        finally // 테스트가 추가로 만든 원본의 정리
        { // 별도 스크립터블 게임 자료 해제 시작
            UnityEngine.Object.Destroy(source); // 게임과 구분되는 임시 검사 덱 제거
            UnityEngine.Object.Destroy(enemyData); // 검사에 연동한 임시 적 자료 제거
        } // 별도 스크립터블 게임 자료 해제 종료
        yield return new ExitPlayMode(); // 정식 선제 및 선택 정산의 Unity 검사 종료
    } // 개막 원본 선택과 실제 비용 정산 종료

    private static CardInstance Adjusted(BattleDeckRuntime deck) // 정상 손패의 독립 실제 가격 변경 카드 조회
    { // 현재 차례 소유 실물 확인 시작
        var changed = deck.Hand.Where(card => card.ApCost != card.SourceData.ApCost).ToArray(); // 다른 효과가 없는 검사 손패의 실제 변경 목록
        Assert.AreEqual(1, changed.Length); // 같은 실물의 한 장만 변한 실제 효과 확인
        Assert.IsFalse(changed[0].SourceData.SpendsAllActionPoints); // 모든 행동력 지불 원본 카드가 추첨되지 않는지 확인
        Assert.AreEqual(1, Math.Abs(changed[0].ApCost - changed[0].SourceData.ApCost)); // 원본 규칙의 양 또는 음 한 점만 변화
        return changed[0]; // 검사에서 선택한 실제 카드 인스턴스 사용
    } // 현재 차례 소유 실물 확인 종료

    private static BattleResultData Capture(BattleTurnRuntime turns) // 게임이 쓰는 실제 종료 결과 스냅샷 생성
    { // 원래 내부 함수 접근 시작
        var method = typeof(BattleTurnRuntime).GetMethod("CaptureBattleResult", BindingFlags.Instance | BindingFlags.NonPublic); // 별도 Unity 검사에서도 공개 범위를 늘리지 않고 조회
        Assert.IsNotNull(method); // 실제 게임 저장 함수 확인
        return (BattleResultData)method.Invoke(turns, new object[] { Array.Empty<string>() }); // 원래 실제 정산을 포함한 게임 데이터 반환
    } // 원래 내부 함수 접근 종료

    private static void Acquire(string name) // 실제 출시 유물의 원본 에셋 획득
    { // 원래 정상 유물 보유 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // Unity 편집기가 읽은 실제 유물 원본
        Assert.IsNotNull(relic, name); // 배정한 실제 유물 자료 여부 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 새 독립 회차의 표준 유물 획득 가치
    } // 원래 정상 유물 보유 종료
} // 실제 유물 검사 종료
