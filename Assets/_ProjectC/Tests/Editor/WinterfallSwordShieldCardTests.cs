using System.Collections; // 전투 연출 대기
using System.Collections.Generic; // 타격별 피해 기록
using System.Linq; // 원본과 전투 손패 조회
using NUnit.Framework; // 검사 결과 비교
using UnityEngine; // 실제 전투 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 손패 입력
using static WinterfallCommonCardExpansionTests; // 공용 카드의 원본 검사와 손패 입력 재사용

public sealed class WinterfallSwordShieldCardTests // 검과 방패 보물 카드의 전투 검사
{ // 검사 시작
    [TestCase(5, "DEFENSIVE_STANCE", "방어 태세", "DefensiveStance", CardType.Sword, "C486459D9EF55BF987C7B882898416A51FDE097B69CF3857B3439D821E00FF30")] // 방어 태세 원본
    [TestCase(46, "MERCILESS_FLURRY", "무자비한 연격", "MercilessFlurry", CardType.Sword, "6D2A013F2C3B7A6429D5094F54C40DDA2C7A3D9F2454B7C253F4E307641A1F92")] // 무자비한 연격 원본
    [TestCase(62, "DEFENSIVE_LINE", "방어선 구축", "DefensiveLine", CardType.Shield, "06BBA43CF5D33E30E816CA1A86381B9AF44D680D753E2A4F55620CA3A082A1E2")] // 방어선 구축 원본
    [TestCase(72, "IRON_WALL", "철벽", "IronWall", CardType.Shield, "5568850036173B42AD20504F85706EA22F4FD154E19C023FC1A4EA8EDEAA0DB6")] // 철벽 원본
    public void MatchesOriginalAndRestorableTreasure(int row, string id, string title, string file, CardType type, string digest) // 시트와 보물 및 저장 원본 확인
    { // 원본 검사 시작
        VerifyTreasureArtwork(row, id, title, file, type, digest); // 지급 설정과 원본 그림의 해시 대조
    } // 원본 검사 종료

    [UnityTest] // 플레이 모드의 네 카드 실제 입력 검사 지정
    public IEnumerator FourBlowsAndThreeGuardTargetsUseOneCardEach() // 네 번 타격과 세 가지 방어 대상 확인
    { // 전투 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        yield return PlayCards(); // 실제 손패와 대상 입력
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 검사 종료

    private static IEnumerator PlayCards() // 실제 획득 원본으로 독립 전투 구성
    { // 전투 구성 시작
        var offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers; // 보물 원본 조회
        string[] ids = { "DEFENSIVE_STANCE", "MERCILESS_FLURRY", "DEFENSIVE_LINE", "IRON_WALL" }; // 원본 검증 순서
        var cards = ids.Select(id => offers.Single(offer => offer.OfferId == "REWARD_SHEET_" + id).Card).Append(Resources.Load<CardData>("StarterCards/EmpressFeast")).ToArray(); // 신규 보물과 기존 전체 배분 카드 원본 조회
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 명단 조회
        var owner = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 모든 검사 카드의 소유자 생성
        var ally = BattleUnitRuntime.CreateAlly(roster.Characters[1]); // 단일 아군 선택용 동료 생성
        var fallen = BattleUnitRuntime.CreateAlly(roster.Characters[2]); // 방어 대상에서 제외할 전투불능 동료
        var allies = new[] { owner, ally, fallen }; // 생존자 둘과 전투불능 검사 동료 구성
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 저항과 약점이 없는 독립 적 원본
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData), BattleUnitRuntime.CreateEnemy(enemyData) }; // 단일 검 공격 비교용 적 둘 생성
        var source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, owner.CharacterSource)).ToArray()); // 실제 원본과 소유자를 덱에 등록
        var deck = BattleDeckRuntime.Create(source, allies, cards.Length, 222); // 시드를 고정한 전투 덱 생성
        var points = new BattleActionPointRuntime(cards.Sum(card => card.ApCost)); // 전체 사용 비용 준비
        var canvas = new GameObject("SharedCardBattle", typeof(RectTransform), typeof(Canvas)); // 전투 화면 생성
        var handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 입력 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패를 전투 화면에 연결
        var hand = handObject.GetComponent<BattleHandView>(); // 손패 컴포넌트 조회
        var allyViews = allies.Select(unit => CreateView(canvas.transform, unit)).ToArray(); // 아군 클릭 화면 연결
        var enemyViews = enemies.Select(unit => CreateView(canvas.transform, unit)).ToArray(); // 적 화면 연결
        var runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 전투 연출 준비
        var statuses = new BattleStatusEffectProcessor(); // 공통 상태 처리기 준비
        try // 전투 객체 정리 보장
        { // 전투 진행 시작
            using (var turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Normal)) // 전투 턴 생성
            using (var actions = new BattleEnemyActionRuntime(enemies, allies)) // 적 행동 연결
            { // 턴 검사 시작
                Assert.IsTrue(turns.StartBattle(cards.Length)); // 모든 검사 카드를 손에 든 전투 시작
                Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 전투 입력 연결
                using (var controller = new BattleCardActionController(deck, points, turns, hand, runner, statuses, allyViews, enemyViews, actions)) // 실제 카드 처리기 생성
                { // 카드 사용 검사 시작
                    fallen.ApplyPersistentHealth(0); // 방어도 지급에서 제외할 전투불능 동료 설정
                    int uses = 0; // 카드 사용 신호 횟수 저장
                    controller.CardUsed += (card, targets) => uses++; // 타격 수와 독립된 카드 사용 횟수 집계
                    yield return Play(cards[0], hand, allyViews[1], runner, deck, points); // 방어 태세로 살아 있는 동료 한 명 선택
                    CollectionAssert.AreEqual(new[] { 0, 6, 0 }, allies.Select(unit => unit.CurrentBlock)); // 선택 대상만 방어도 여섯 획득 확인
                    var hits = new List<BattleDamageResult>(); // 지정한 적의 실제 타격별 피해 기록
                    enemies[0].DamageResolved += (attacker, victim, hit) => hits.Add(hit); // 대상 적의 실제 피해 신호 수집
                    enemies[0].GainBlock(8); // 첫 두 타격에 걸쳐 소모할 적 방어도 설정
                    yield return Play(cards[1], hand, enemyViews[0], runner, deck, points); // 무자비한 연격으로 적 한 명 네 번 공격
                    CollectionAssert.AreEqual(new[] { 5, 5, 5, 5 }, hits.Select(hit => hit.RawDamage)); // 네 타격의 개별 피해 다섯 확인
                    CollectionAssert.AreEqual(new[] { 5, 3, 0, 0 }, hits.Select(hit => hit.AbsorbedDamage)); // 타격 순서대로 방어도 소모 확인
                    Assert.AreEqual(enemies[0].MaxHealth - 12, enemies[0].CurrentHealth); // 방어도 차감 후 실제 체력 피해 확인
                    Assert.AreEqual(enemies[1].MaxHealth, enemies[1].CurrentHealth); // 선택하지 않은 적의 체력 보존 확인
                    yield return Play(cards[2], hand, null, runner, deck, points); // 방어선 구축으로 생존 아군 전체 보호
                    CollectionAssert.AreEqual(new[] { 4, 10, 0 }, allies.Select(unit => unit.CurrentBlock)); // 생존자마다 방어도 네 점 획득 확인
                    yield return Play(cards[3], hand, null, runner, deck, points); // 철벽으로 소유자 자신만 보호
                    CollectionAssert.AreEqual(new[] { 22, 10, 0 }, allies.Select(unit => unit.CurrentBlock)); // 자신만 방어도 열여덟 획득 확인
                    yield return Play(cards[4], hand, null, runner, deck, points); // 이전 여황제의 전체 배분 규칙 대조
                    CollectionAssert.AreEqual(new[] { 24, 11, 0 }, allies.Select(unit => unit.CurrentBlock)); // 기존 전체 방어도 세 점을 둘과 하나로 배분 확인
                    Assert.AreEqual(cards.Length, uses); // 여러 타격에도 카드당 한 번 사용 확인
                    Assert.AreEqual(0, deck.HandCount); // 모든 사용 카드의 손패 이탈 확인
                    Assert.AreEqual(cards.Length, deck.DiscardPileCount); // 사용 카드마다 버린 더미 한 장 확인
                    Assert.AreEqual(0, points.CurrentActionPoints); // 실제 원본 비용 합계만큼 차감 확인
                } // 카드 사용 검사 종료
                hand.Unbind(); // 손패 연결 해제
            } // 턴 검사 종료
        } // 전투 진행 종료
        finally // 독립 전투 정리
        { // 정리 시작
            Object.Destroy(canvas); // 전투 화면 정리
            Object.Destroy(source); // 임시 덱 원본 정리
            Object.Destroy(enemyData); // 임시 적 원본 정리
        } // 정리 종료
    } // 전투 구성 종료

} // 검사 종료
