using System.Collections; // 전투 연출 대기
using System.IO; // 원본 그림 파일 읽기
using System.Linq; // 손패와 보상 원본 조회
using System.Security.Cryptography; // 그림 파일 해시 계산
using NUnit.Framework; // 검사 결과 비교
using UnityEditor; // 불러온 그림 경로 조회
using UnityEngine; // 실제 전투 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 손패 입력

public sealed class WinterfallCommonCardExpansionTests // 윈터폴 공용 카드 확장 검사
{ // 검사 시작
    [TestCase(11, "BATTLE_CRY", "전장의 외침", "BattleCry", CardType.Sword, "8190BF1B5EC8C668771201A89CBD67BFD6B1A0B1A224AC25E880F4309D29FB60")] // 전장의 외침 원본
    [TestCase(60, "RAISE_SHIELD", "방패 들기", "RaiseShield", CardType.Shield, "4B6CBB3291BB37700E2002838EF1A68B682AAE4D84BBA153D038C7E584B9AE3A")] // 방패 들기 원본
    [TestCase(79, "ARMOR_REINFORCEMENT", "장갑 강화", "ArmorReinforcement", CardType.Shield, "6E2392BD0BF80A1C94C6514C44D5A01BAF087D7E1D6A8094978B4293A198B498")] // 장갑 강화 원본
    [TestCase(112, "CLEAR_WATER", "맑은 물", "ClearWater", CardType.Cup, "DEFBA101E731B038A42D22F107DF68908808E50A14B09B47D14EAD618BC82104")] // 맑은 물 원본
    [TestCase(123, "MENTAL_RECOVERY", "정신 회복", "MentalRecovery", CardType.Cup, "0217E420E8A03550258178240C70305A656544A47BC598AB412783684CF98FCC")] // 정신 회복 원본
    [TestCase(134, "GREATER_CLEANSE", "대정화", "GreaterCleanse", CardType.Cup, "29DD5ED23A5D5B9523E1F3A543615F44B55ED10C59B36A1CD1E18D238184B6B3")] // 대정화 원본
    [TestCase(154, "FLAME_ORB", "화염구", "FlameOrb", CardType.Wand, "D6196D250BA35C1DD2016B73B4B81D55D52AC2B4D6EA2A84F6BEB0D4E5F22AC3")] // 화염구 원본
    [TestCase(214, "QUICK_CALCULATION", "빠른 계산", "QuickCalculation", CardType.Pentacle, "E386AC5732604E9E466DE3D720586B494DF83C6CD007A16B1EB39573E51603E3")] // 빠른 계산 원본
    public void TreasureMatchesSheetArtworkAndSave(int rowNumber, string id, string title, string file, CardType type, string digest) // 시트와 압축 원본 및 지급 경로 대조
    { // 원본 검사 시작
        VerifyTreasureArtwork(rowNumber, id, title, file, type, digest); // 공용 카드의 시트와 원본 그림 검사
    } // 원본 검사 종료

    internal static void VerifyTreasureArtwork(int rowNumber, string id, string title, string file, CardType type, string digest) // 실제 보상 원본과 그림 대조
    { // 원본 비교 시작
        ShopOfferData item = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(offer => offer.OfferId == "REWARD_SHEET_" + id); // 실제 보물 상품 조회
        Assert.IsTrue(item.RewardOnly && item.RequiresOwnerChoice); // 일반 구매 제외와 소유자 선택 확인
        CardData card = item.Card; // 지급될 카드 원본 조회
        CardSheetEntry row = CardSheetCatalog.Load().FindByName(title); // 기획 시트 원본 조회
        Assert.AreEqual(rowNumber, row.SourceRow); // 원본 행 확인
        Assert.AreEqual(row.Name, card.DisplayName); // 원본 이름 확인
        Assert.AreEqual(row.Description, card.Description); // 효과 원문 확인
        Assert.AreEqual(row.Cost, card.ApCost.ToString()); // 행동력 비용 확인
        Assert.AreEqual(type, card.CardType); // 카드 속성 확인
        Assert.AreEqual("CRD_SHEET_" + id, card.CardId); // 고정 카드 식별자 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards, card); // 획득 카드 복원 원본 확인
        Assert.AreEqual(title + ".png", row.ImageFileName); // 원본 압축 내 그림 이름 확인
        string art = AssetDatabase.GetAssetPath(card.Artwork); // 실제 표시 그림 경로 조회
        Assert.AreEqual("Assets/_ProjectC/Art/CardIllustrations/Sheet" + file + ".png", art); // 카드별 그림 연결 확인
        using (var hash = SHA256.Create()) // 원본 압축 파일과 동일한 해시 계산 준비
        using (var stream = File.OpenRead(art)) // 연결된 PNG 파일 열기
        { // 그림 검사 시작
            Assert.AreEqual(digest, System.BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty)); // 원본 PNG와 바이트 일치 확인
        } // 그림 검사 종료
    } // 원본 검사 종료

    [UnityTest] // 전투 손패 검사 지정
    public IEnumerator HandPlaysSupportMagicAndDrawAtSheetCost() // 실제 카드 입력과 효과 및 소모 확인
    { // 전투 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        yield return UseRewardCards(); // 현재 실행 환경에서 전투 준비
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 검사 종료

    private static IEnumerator UseRewardCards() // 카드 여덟 종류의 실제 전투 입력
    { // 전투 구성 시작
        var sheet = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers; // 실제 획득 후보 조회
        string[] ids = { "FLAME_ORB", "BATTLE_CRY", "RAISE_SHIELD", "ARMOR_REINFORCEMENT", "CLEAR_WATER", "CLEAR_WATER", "CLEAR_WATER", "MENTAL_RECOVERY", "GREATER_CLEANSE", "QUICK_CALCULATION" }; // 전투 입력 순서
        CardData[] cards = ids.Select(id => sheet.Single(offer => offer.OfferId == "REWARD_SHEET_" + id).Card).ToArray(); // 실제 보물 원본으로 검사 덱 구성
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 명단 조회
        var owner = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 모든 검사 카드의 소유자 생성
        var ally = BattleUnitRuntime.CreateAlly(roster.Characters[1]); // 단일 아군 선택용 동료 생성
        var allies = new[] { owner, ally }; // 전투 아군 둘 구성
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 저항과 약점이 없는 독립 적 원본
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData), BattleUnitRuntime.CreateEnemy(enemyData) }; // 광역 마법 확인용 적 둘 생성
        var source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, owner.CharacterSource)).ToArray()); // 실제 원본과 소유자를 덱에 등록
        var deck = BattleDeckRuntime.Create(source, allies, cards.Length, 207); // 시드를 고정한 전투 덱 생성
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
                    int uses = 0; // 사용한 카드 수
                    controller.CardUsed += (card, targets) => uses++; // 카드별 사용 신호 합산
                    yield return Play(cards[0], hand, null, runner, deck, points); // 화염구로 적 전체 공격
                    Assert.IsTrue(enemies.All(enemy => enemy.CurrentHealth == enemy.MaxHealth - 10)); // 두 적이 각각 마법 피해 열 획득 확인
                    yield return Play(cards[1], hand, null, runner, deck, points); // 전장의 외침으로 아군 전체 강화
                    Assert.IsTrue(allies.All(unit => unit.AttackPowerBonus == 3 && unit.GetStatusEffect(BattleStatusEffectType.AttackPowerUp).RemainingTurns == 2)); // 아군별 공격력과 두 턴 확인
                    yield return Play(cards[2], hand, null, runner, deck, points); // 방패 들기로 소유자 보호
                    Assert.AreEqual(8, owner.CurrentBlock); // 소유자 방어도 확인
                    Assert.AreEqual(0, ally.CurrentBlock); // 다른 아군의 방어도 확인
                    yield return Play(cards[3], hand, allyViews[1], runner, deck, points); // 동료 한 명에게 장갑 강화
                    Assert.AreEqual(ally.BasePhysicalDefense + 5, ally.PhysicalDefense); // 지정한 동료의 물리 방어력 확인
                    Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseUp).RemainingTurns); // 장갑 강화 두 턴 확인
                    Assert.AreEqual(owner.BasePhysicalDefense, owner.PhysicalDefense); // 소유자의 기본 물리 방어력 확인
                    owner.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1, enemies[0]); // 최대 체력 소유자에게 중독 부여
                    yield return Play(cards[4], hand, allyViews[0], runner, deck, points); // 최대 체력에서 맑은 물 사용
                    Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.Poison)); // 최대 체력에서 중독 제거 확인
                    Assert.AreEqual(owner.MaxHealth, owner.CurrentHealth); // 최대 체력 유지 확인
                    ally.ApplyPersistentHealth(10); // 동료 회복 여유 확보
                    ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 2, 1, enemies[0]); // 제거하지 않을 화상 부여
                    ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 2, 1, enemies[0]); // 지정 제거할 중독 부여
                    yield return Play(cards[5], hand, allyViews[1], runner, deck, points); // 부상당한 중독 동료에게 맑은 물 사용
                    Assert.AreEqual(13, ally.CurrentHealth); // 체력 셋 회복 확인
                    Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 대상의 중독 제거 확인
                    Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Burn)); // 먼저 적용된 화상 유지 확인
                    yield return Play(cards[6], hand, allyViews[1], runner, deck, points); // 중독 없는 부상 동료에게 맑은 물 사용
                    Assert.AreEqual(16, ally.CurrentHealth); // 중독이 없어도 체력 셋 회복 확인
                    int ownerMental = owner.CurrentMental; // 소유자 정신력 보관
                    int allyMental = ally.CurrentMental; // 동료 정신력 보관
                    yield return Play(cards[7], hand, null, runner, deck, points); // 정신 회복으로 모든 아군 지원
                    Assert.AreEqual(ownerMental + 5, owner.CurrentMental); // 소유자 정신력 다섯 회복 확인
                    Assert.AreEqual(allyMental + 5, ally.CurrentMental); // 동료 정신력 다섯 회복 확인
                    owner.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1, enemies[0]); // 소유자의 첫 디버프 추가
                    owner.ApplyStatusEffect(BattleStatusEffectType.Blind, 20, 2, 1, enemies[0]); // 소유자의 둘째 디버프 추가
                    ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1, enemies[0]); // 동료의 화상 다음에 중독 추가
                    yield return Play(cards[8], hand, null, runner, deck, points); // 대정화로 각자 디버프 하나 해제
                    Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.Poison)); // 소유자의 첫 디버프 제거 확인
                    Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.Blind)); // 소유자의 둘째 디버프 보존 확인
                    Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Burn)); // 동료의 첫 디버프 제거 확인
                    Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 동료의 둘째 디버프 보존 확인
                    Assert.AreEqual(1, deck.HandCount); // 빠른 계산 한 장만 남은 손패 확인
                    yield return Play(cards[9], hand, null, runner, deck, points); // 행동력 영으로 빠른 계산 사용
                    Assert.AreEqual(1, deck.HandCount); // 이전에 사용한 카드 한 장 드로우 확인
                    CardInstance drawn = deck.Hand.Single(); // 실제 뽑은 카드 보관
                    Assert.AreNotSame(cards[9], drawn.SourceData); // 사용한 빠른 계산의 즉시 재드로우 방지 확인
                    var choices = canvas.GetComponentInChildren<BattleDrawChoiceView>(); // 버릴 손패 선택 화면 조회
                    Assert.IsNotNull(choices); // 버릴 카드 선택 화면 표시 확인
                    choices.transform.Find("Choice_0").GetComponent<Button>().onClick.Invoke(); // 뽑은 카드 한 장 선택해 버리기
                    Assert.AreEqual(0, deck.HandCount); // 선택한 카드가 손패에서 빠진 상태 확인
                    CollectionAssert.Contains(deck.DiscardPile, drawn); // 선택 카드의 버린 더미 이동 확인
                    Assert.AreEqual(cards.Length, uses); // 모든 입력의 카드당 한 번 사용 확인
                    Assert.AreEqual(0, points.CurrentActionPoints); // 총 행동력 소진 확인
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

    internal static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 연결
    { // 화면 생성 시작
        var item = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 클릭 대상 화면 생성
        item.transform.SetParent(parent, false); // 전투 화면 아래 배치
        var view = item.GetComponent<BattleUnitView>(); // 대상 입력 조회
        view.Bind(unit); // 실제 유닛 상태 연결
        return view; // 연결한 화면 반환
    } // 화면 생성 종료

    internal static IEnumerator Play(CardData card, BattleHandView hand, BattleUnitView target, BattleActionSequenceRunner runner, BattleDeckRuntime deck, BattleActionPointRuntime points) // 카드와 대상 클릭 후 소모 확인
    { // 카드 입력 시작
        var view = hand.GetComponentsInChildren<BattleCardView>().First(item => item.RuntimeCard != null && item.RuntimeCard.SourceData == card && deck.Hand.Contains(item.RuntimeCard)); // 실제 손패에서 사용할 카드 조회
        CardInstance used = view.RuntimeCard; // 사용할 카드의 개별 인스턴스
        int before = points.CurrentActionPoints; // 사용 전 행동력 보관
        view.OnPointerClick(null); // 실제 손패 클릭
        if (target != null) // 사용자가 고르는 대상 확인
        { // 대상 입력 시작
            target.OnPointerClick(null); // 아군 한 명 클릭
        } // 대상 입력 종료
        Assert.AreEqual(before - card.ApCost, points.CurrentActionPoints, card.DisplayName); // 카드별 행동력 차감 확인
        float deadline = Time.realtimeSinceStartup + 10f; // 연출 대기 기한
        while (runner.IsBusy && Time.realtimeSinceStartup < deadline) // 효과와 복귀 연출 대기
        { // 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 연출 대기 종료
        Assert.IsFalse(runner.IsBusy, card.DisplayName); // 연출 종료 확인
        CollectionAssert.Contains(deck.DiscardPile, used); // 사용한 카드 한 장의 버린 더미 이동 확인
    } // 카드 입력 종료
} // 검사 종료
