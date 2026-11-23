using System.Collections; // 전투 연출 대기
using System.Linq; // 카드와 손패 조회
using NUnit.Framework; // 카드 검증
using UnityEditor; // 원본 그림 경로 조회
using UnityEngine; // 실제 전투 객체
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 손패 화면

public sealed class CupTreasureCardTests // 성배 보물 카드 검사
{ // 검사 시작
    [TestCase("HEAL", "치유", "Heal", 1, CardTargetType.SingleAlly, CardEffectType.Heal, 8)] // 단일 치유 카드
    [TestCase("REVITALIZE", "소생", "Revitalize", 2, CardTargetType.SingleAlly, CardEffectType.Heal, 15)] // 단일 소생 카드
    [TestCase("HEALING_WAVE", "회복의 물결", "HealingWave", 2, CardTargetType.AllAllies, CardEffectType.Heal, 10)] // 전체 회복 카드
    [TestCase("PRAYER", "기도", "Prayer", 1, CardTargetType.SingleAlly, CardEffectType.ChangeMental, 6)] // 정신력 회복 카드
    [TestCase("PROTECTIVE_CUP", "보호의 잔", "ProtectiveCup", 1, CardTargetType.SingleAlly, CardEffectType.GainBlock, 7)] // 단일 방어도 카드
    public void SheetValuesArtworkAndRewardRegistrationMatch(string id, string cardName, string artName, int cost, CardTargetType target, CardEffectType effect, int amount) // 시트 수치와 획득 등록 확인
    { // 원본 검사 시작
        ShopCatalogData catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 보물 후보 원본 조회
        ShopOfferData offer = catalog.Offers.SingleOrDefault(item => item.OfferId == "REWARD_SHEET_" + id); // 해당 보물 상품 조회
        Assert.IsNotNull(offer, cardName); // 보물 등록 확인
        Assert.IsTrue(offer.RewardOnly && offer.RequiresOwnerChoice); // 보상 전용과 소유자 선택 확인
        CardData card = offer.Card; // 실제 지급 카드 조회
        Assert.AreEqual("CRD_SHEET_" + id, card.CardId); // 저장용 카드 식별자 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards, card); // 저장 원본 등록 확인
        CardSheetEntry row = CardSheetCatalog.Load().FindByName(cardName); // 기획 시트 행 조회
        Assert.AreEqual(row.Name, card.DisplayName); // 시트 이름 확인
        Assert.AreEqual(row.Description, card.Description); // 시트 효과 설명 확인
        Assert.AreEqual(row.Cost, card.ApCost.ToString()); // 시트 비용 확인
        Assert.AreEqual(cost, card.ApCost); // 승인된 행동력 확인
        Assert.AreEqual(CardType.Cup, card.CardType); // 성배 속성 확인
        Assert.AreEqual(target, card.TargetType); // 적용 대상 확인
        Assert.AreEqual(effect, card.EffectType); // 회복 또는 방어 종류 확인
        Assert.AreEqual(amount, effect == CardEffectType.ChangeMental ? card.MentalChangeValue : card.EffectValue); // 효과 수치 확인
        Assert.IsNotNull(card.Artwork); // 그림 연결 확인
        Assert.AreEqual("Assets/_ProjectC/Art/CardIllustrations/Sheet" + artName + ".png", AssetDatabase.GetAssetPath(card.Artwork)); // 카드별 원본 그림 확인
    } // 원본 검사 종료

    [UnityTest] // 실제 손패 조작 검사 지정
    public IEnumerator HandClicksApplySingleAndPartySupportAtSheetCost() // 실제 손패 사용과 아군 효과 확인
    { // 전투 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        yield return UseCupCards(); // 모드 전환 후 전투 상태와 손패 생성
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 검사 종료

    private static IEnumerator UseCupCards() // 다섯 카드의 실전 입력 검증
    { // 손패 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 명단 조회
        Assert.IsNotNull(roster); // 실제 명단 로드 확인
        BattleUnitRuntime caster = BattleUnitRuntime.CreateAlly(roster.Characters[2]); // 카드를 쓸 여사제 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(roster.Characters[3]); // 카드를 받을 여황제 생성
        var allies = new[] { caster, ally }; // 두 아군 편성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 독립 전투 적 원본 생성
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData) }; // 독립 전투 적 편성
        string[] ids = { "HEAL", "REVITALIZE", "HEALING_WAVE", "PRAYER", "PROTECTIVE_CUP" }; // 사용할 다섯 카드 순서
        SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 저장 카드 원본 조회
        CardData[] cards = ids.Select(id => catalog.Cards.Single(card => card.CardId == "CRD_SHEET_" + id)).ToArray(); // 성배 카드 다섯 종 조회
        DeckData source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, caster.CharacterSource)).ToArray()); // 여사제 소유 카드로 덱 구성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, allies, cards.Length, 197); // 고정 시드 실전 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(7); // 다섯 카드의 총 행동력 준비
        GameObject canvas = new GameObject("CupCardBattle", typeof(RectTransform), typeof(Canvas)); // 전투 화면 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 전투 화면에 손패 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 입력 조회
        BattleUnitView[] allyViews = allies.Select(unit => CreateUnitView(canvas.transform, unit)).ToArray(); // 아군 선택 화면 연결
        BattleUnitView enemyView = CreateUnitView(canvas.transform, enemies[0]); // 적 화면 연결
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 실제 전투 연출 준비
        using (var turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Normal)) // 일반 전투 턴 준비
        using (var actions = new BattleEnemyActionRuntime(enemies, allies)) // 적 행동 준비
        { // 전투 진행 시작
            Assert.IsTrue(turns.StartBattle(cards.Length)); // 다섯 장을 든 전투 시작
            Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패 입력과 실제 덱 연결
            using (var controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), allyViews, new[] { enemyView }, actions)) // 실제 카드 행동 처리 연결
            { // 카드 사용 검사 시작
                caster.ApplyPersistentHealth(20); // 시전자 회복 여유 확보
                ally.ApplyPersistentHealth(10); // 대상 회복 여유 확보
                yield return Play(cards[0], hand, allyViews[1], runner, points); // 치유를 동료에게 사용
                Assert.AreEqual(18, ally.CurrentHealth); // 동료 체력 여덟 회복 확인
                Assert.AreEqual(20, caster.CurrentHealth); // 시전자 체력 유지 확인
                yield return Play(cards[1], hand, allyViews[1], runner, points); // 소생을 동료에게 사용
                Assert.AreEqual(33, ally.CurrentHealth); // 동료 체력 열다섯 회복 확인
                Assert.AreEqual(20, caster.CurrentHealth); // 단일 대상 범위 확인
                yield return Play(cards[2], hand, null, runner, points); // 대상 클릭 없이 전체 회복 사용
                Assert.AreEqual(43, ally.CurrentHealth); // 동료 체력 열 회복 확인
                Assert.AreEqual(30, caster.CurrentHealth); // 시전자 체력 열 회복 확인
                int mental = ally.CurrentMental; // 동료 정신력 보관
                int casterMental = caster.CurrentMental; // 시전자 정신력 보관
                yield return Play(cards[3], hand, allyViews[1], runner, points); // 기도를 동료에게 사용
                Assert.AreEqual(mental + 6, ally.CurrentMental); // 동료 정신력 여섯 회복 확인
                Assert.AreEqual(casterMental, caster.CurrentMental); // 시전자 정신력 유지 확인
                yield return Play(cards[4], hand, allyViews[1], runner, points); // 보호의 잔을 동료에게 사용
                Assert.AreEqual(7, ally.CurrentBlock); // 동료 방어도 일곱 획득 확인
                Assert.AreEqual(0, caster.CurrentBlock); // 시전자 방어도 유지 확인
                Assert.AreEqual(0, points.CurrentActionPoints); // 다섯 장의 행동력 일곱 소모 확인
                Assert.AreEqual(5, deck.DiscardPileCount); // 사용 카드 다섯 장의 더미 이동 확인
            } // 카드 사용 검사 종료
            hand.Unbind(); // 손패 입력 연결 해제
        } // 전투 진행 종료
        Object.Destroy(canvas); // 전투 화면 정리
        Object.Destroy(source); // 독립 덱 정리
        Object.Destroy(enemyData); // 독립 적 원본 정리
    } // 손패 검사 종료

    private static BattleUnitView CreateUnitView(Transform parent, BattleUnitRuntime unit) // 전투 대상 클릭 화면 생성
    { // 유닛 화면 생성 시작
        GameObject item = new GameObject("CupCardUnit", typeof(RectTransform), typeof(BattleUnitView)); // 대상 화면 객체 생성
        item.transform.SetParent(parent, false); // 전투 화면에 배치
        BattleUnitView view = item.GetComponent<BattleUnitView>(); // 유닛 입력 조회
        view.Bind(unit); // 실제 유닛 연결
        return view; // 연결한 화면 반환
    } // 유닛 화면 생성 종료

    private static IEnumerator Play(CardData card, BattleHandView hand, BattleUnitView target, BattleActionSequenceRunner runner, BattleActionPointRuntime points) // 손패와 대상 클릭 처리
    { // 카드 입력 시작
        BattleCardView view = hand.GetComponentsInChildren<BattleCardView>().Single(item => item.RuntimeCard != null && item.RuntimeCard.SourceData == card); // 현재 손패의 해당 카드 조회
        int before = points.CurrentActionPoints; // 사용 전 행동력 보관
        view.OnPointerClick(null); // 실제 손패 카드 클릭
        if (target != null) // 단일 대상 카드 확인
        { // 단일 대상 선택 시작
            target.OnPointerClick(null); // 동료 유닛 클릭
        } // 단일 대상 선택 종료
        Assert.AreEqual(before - card.ApCost, points.CurrentActionPoints, card.DisplayName); // 카드별 행동력 차감 확인
        float deadline = Time.realtimeSinceStartup + 10f; // 전투 연출 최대 대기 시각
        while (runner.IsBusy && Time.realtimeSinceStartup < deadline) // 전투 효과 적용 대기
        { // 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 연출 대기 종료
        Assert.IsFalse(runner.IsBusy, card.DisplayName); // 카드 연출 종료 확인
    } // 카드 입력 종료
} // 검사 종료
