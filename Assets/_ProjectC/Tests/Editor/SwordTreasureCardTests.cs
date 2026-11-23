using System.Collections; // 전투 연출 대기
using System.Collections.Generic; // 타격별 피해 기록
using System.IO; // 독립 탐사 파일 검사
using System.Linq; // 카드와 보상 후보 조회
using NUnit.Framework; // 검사 결과 비교
using UnityEditor; // 독립 적 수치와 원본 그림 조회
using UnityEngine; // 실제 전투 화면
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 손패 입력

public sealed class SwordTreasureCardTests // 검 보물 카드 검사
{ // 검사 시작
    [TestCase("SLASH", "베기", "Slash", "일반", 1, 4, 1)] // 단일 베기 원본
    [TestCase("DOUBLE_SLASH", "연속 베기", "DoubleSlash", "희귀", 2, 4, 2)] // 연속 베기 원본
    [TestCase("TWIN_CUT", "이연참", "TwinCut", "희귀", 2, 6, 2)] // 이연참 원본
    public void TreasureCardsMatchSwordSheetAndSaveCatalog(string id, string title, string file, string rarity, int cost, int power, int hits) // 원본과 지급 경로 확인
    { // 원본 검사 시작
        ShopOfferData offer = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.SingleOrDefault(item => item.OfferId == "REWARD_SHEET_" + id); // 보물 등록 조회
        Assert.IsNotNull(offer, title); // 해당 보물 존재 확인
        Assert.IsTrue(offer.RequiresOwnerChoice && offer.RewardOnly); // 보물 전용과 소유자 선택 확인
        CardData card = offer.Card; // 지급 카드 조회
        CardSheetEntry row = CardSheetCatalog.Load().FindByName(title); // 원본 시트 행 조회
        Assert.AreEqual("CRD_SHEET_" + id, card.CardId); // 고유 저장 식별자 확인
        Assert.AreEqual(row.Name, card.DisplayName); // 원본 이름 확인
        Assert.AreEqual(row.Description, card.Description); // 원본 타격 설명 확인
        Assert.AreEqual(rarity, row.Rarity); // 희귀도 확인
        Assert.AreEqual(cost.ToString(), row.Cost); // 시트 행동력 확인
        Assert.AreEqual(cost, card.ApCost); // 실제 행동력 확인
        Assert.AreEqual(power, card.EffectValue); // 타격당 피해 확인
        Assert.AreEqual(hits, card.DamageHitCount); // 원본 타격 횟수 확인
        Assert.AreEqual(CardType.Sword, card.CardType); // 검 속성 확인
        Assert.AreEqual(CardTargetType.SingleEnemy, card.TargetType); // 단일 적 확인
        Assert.AreEqual(CardEffectType.Damage, card.EffectType); // 공격 효과 확인
        Assert.AreEqual(BattleDamageType.Physical, card.DamageType); // 물리 피해 확인
        Assert.IsNotNull(card.Artwork); // 카드 그림 연결 확인
        Assert.AreEqual("Assets/_ProjectC/Art/CardIllustrations/Sheet" + file + ".png", AssetDatabase.GetAssetPath(card.Artwork)); // 원본 그림 경로 확인
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards, card); // 저장 가능한 원본 확인
    } // 원본 검사 종료

    [Test] // 실제 희귀 보상 추첨 검사 지정
    public void AshstormEyeCanRewardBothRareSwordsAndHealingWave() // 기존과 새 희귀 보상 공존 확인
    { // 희귀 보상 검사 시작
        var expected = new HashSet<string> { "CRD_SHEET_DOUBLE_SLASH", "CRD_SHEET_TWIN_CUT", "CRD_SHEET_HEALING_WAVE" }; // 확인할 희귀 카드
        for (int i = 0; i < 256 && expected.Count > 0; i++) // 서로 다른 이벤트 시드 순회
        { // 보상 추첨 시작
            ShopOfferData offer = AshstormEyeEvent.GetRareReward("sword-202-" + i); // 실제 희귀 보상 선택
            Assert.IsNotNull(offer); // 지급 후보 존재 확인
            Assert.AreNotEqual("CRD_SHEET_SLASH", offer.Card.CardId); // 일반 베기 제외 확인
            expected.Remove(offer.Card.CardId); // 획득한 희귀 후보 표시
        } // 보상 추첨 종료
        CollectionAssert.IsEmpty(expected); // 세 희귀 카드 획득 가능 확인
    } // 희귀 보상 검사 종료

    [UnityTest] // 이전 카드 탐사 저장 검사 지정
    public IEnumerator OlderCardEntryRestoresOwnerAndUpgradeWithSingleHit() // 타격 수 없는 이전 저장 항목의 복원 확인
    { // 이전 저장 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyOlderCardEntry(); // 모드 전환 뒤 이전 카드 저장과 복원
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 이전 저장 검사 종료

    private static void VerifyOlderCardEntry() // 기존 카드만 담은 탐사 파일 복원
    { // 파일 복원 검사 시작
        string path = Path.Combine(Application.temporaryCachePath, "sword-legacy-" + System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 파일 경로
        try // 파일 정리 보장
        { // 탐사 구성 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 명단 조회
            var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 출전 조건 준비
            Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 파티원 선택
            CharacterData owner = roster.Characters[1]; // 카드 소유자 선택
            Assert.IsTrue(selection.TryAdd(owner)); // 둘째 파티원 추가
            Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 편성과 시작 덱 생성
            RunDeckManager deck = RunDeckManager.EnsureInstance(); // 회차 덱 조회
            deck.ResetToSource(PartyLoadoutRuntime.Active.Deck); // 출발 덱 준비
            var session = ExplorationSessionManager.EnsureInstance(); // 현재 탐사 조회
            session.ResetExploration(); // 이전 탐사 정리
            session.SetCurrentFloorSeed(202); // 탐사 저장용 층 설정
            Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 실제 탐사 파티 등록
            CardData legacy = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards.Single(card => card.CardId == "CRD_TEST_HEAVY_STRIKE"); // 타격 수 필드가 없는 기존 강타 원본
            Assert.IsTrue(deck.TryAddCard(legacy, owner)); // 기존 카드를 둘째 소유자에게 지급
            Assert.IsTrue(deck.TryUpgradeAt(deck.CardCount - 1)); // 기존 카드 한 단계 강화
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 기존 카드만 담긴 실제 탐사 파일 작성
            string json = File.ReadAllText(path); // 작성된 탐사 파일 조회
            StringAssert.Contains(legacy.CardId, json); // 이전 카드 식별자 저장 확인
            StringAssert.DoesNotContain("damageHitCount", json); // 이전 형식과 같은 카드 항목 확인
            deck.ResetToSource(null); // 저장 전 회차 덱 제거
            PartyLoadoutRuntime.Clear(); // 저장 전 편성 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 탐사 파일에서 실제 복원
            RunDeckCardEntry restored = deck.Cards.Last(); // 복원된 기존 카드 조회
            Assert.AreSame(legacy, restored.Card); // 기존 식별자와 원본 연결 확인
            Assert.AreSame(owner, restored.Owner); // 이전 카드 소유자 확인
            Assert.AreEqual(1, restored.UpgradeLevel); // 이전 강화 단계 확인
            Assert.AreEqual(1, restored.Card.DamageHitCount); // 기존 공격의 한 번 타격 확인
        } // 탐사 구성 종료
        finally // 임시 파일 정리
        { // 정리 시작
            ExplorationSaveService.Remove(path); // 독립 탐사 파일과 복구본 제거
        } // 정리 종료
    } // 파일 복원 검사 종료

    [UnityTest] // 실제 전투 검사 지정
    public IEnumerator HandAttacksResolveEachHitAndSpendOnce() // 같은 적 연타와 중간 중단 확인
    { // 전투 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        yield return UseSwordCards(); // 모드 전환 후 전투 시작
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 검사 종료

    private static IEnumerator UseSwordCards() // 검 카드 전투 구성과 조작
    { // 전투 구성 시작
        ShopCatalogData offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상품 목록 조회
        CardData basic = offers.Offers.Single(item => item.OfferId == "REWARD_SHEET_SLASH").Card; // 베기 원본 조회
        CardData repeated = offers.Offers.Single(item => item.OfferId == "REWARD_SHEET_DOUBLE_SLASH").Card; // 연속 베기 원본 조회
        CardData twin = offers.Offers.Single(item => item.OfferId == "REWARD_SHEET_TWIN_CUT").Card; // 이연참 원본 조회
        CardData legacy = offers.Offers.Single(item => item.OfferId == "SHOP_SHEET_HEAVY_STRIKE").Card; // 타격 수 없는 기존 강타 원본 조회
        CharacterData owner = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[7]; // 전차 원본 조회
        BattleUnitRuntime attacker = BattleUnitRuntime.CreateAlly(owner); // 검 카드 소유자 생성
        EnemyData enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 독립 시험 적 원본 생성
        var editor = new SerializedObject(enemySource); // 시험 적 속성 편집
        editor.FindProperty("physicalDefense").intValue = 50; // 물리 방어력 오십 설정
        SerializedProperty weak = editor.FindProperty("weaknessCardTypes"); // 약점 속성 조회
        weak.arraySize = 1; // 약점 한 칸 준비
        weak.GetArrayElementAtIndex(0).enumValueIndex = (int)CardType.Sword; // 검 약점 지정
        editor.ApplyModifiedPropertiesWithoutUndo(); // 시험 적 수치 적용
        BattleUnitRuntime target = BattleUnitRuntime.CreateEnemy(enemySource); // 지정 적 생성
        BattleUnitRuntime other = BattleUnitRuntime.CreateEnemy(enemySource); // 변경 대상 검사 적 생성
        var enemies = new[] { target, other }; // 독립된 적 두 명 편성
        var allies = new[] { attacker }; // 전차 단독 편성
        CardData[] sequence = { repeated, twin, basic, legacy, repeated, twin }; // 공격 검사 순서
        DeckData source = DeckData.CreateRuntime(sequence.Select(card => new DeckCardEntry(card, owner)).ToArray()); // 실제 원본으로 검 덱 구성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, allies, sequence.Length, 202); // 실전 덱 생성
        var points = new BattleActionPointRuntime(sequence.Sum(card => card.ApCost)); // 검사 카드 총 행동력 준비
        GameObject canvas = new GameObject("SwordBattle", typeof(RectTransform), typeof(Canvas)); // 전투 화면 생성
        var handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 화면 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 입력 조회
        BattleUnitView[] enemyViews = enemies.Select(unit => CreateView(canvas.transform, unit)).ToArray(); // 적 화면 연결
        BattleUnitView attackerView = CreateView(canvas.transform, attacker); // 공격자 화면 연결
        var runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 공격 연출 준비
        var hits = new List<BattleDamageResult>(); // 명중한 타격 기록
        target.DamageResolved += (actor, victim, hit) => hits.Add(hit); // 지정 적의 실제 피해 기록
        other.DamageResolved += (actor, victim, hit) => hits.Add(hit); // 두 번째 적의 실제 피해 기록
        var previousRandom = Random.state; // 기존 난수 상태 보관
        try // 독립 전투 상태 정리 보장
        { // 전투 입력 시작
            using (var turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Boss)) // 전환 예약 가능한 전투 생성
            using (var actions = new BattleEnemyActionRuntime(enemies, allies)) // 적 행동 연결
            { // 턴 검사 시작
                Assert.IsTrue(turns.StartBattle(sequence.Length)); // 모든 검사 카드 드로우
                Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 손패와 덱 연결
                using (var controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { attackerView }, enemyViews, actions)) // 실제 공격 처리 연결
                { // 카드 검사 시작
                    int plays = 0; // 카드 사용 신호 횟수
                    controller.CardUsed += BattleCardDamageContext.Begin; // 실전과 같은 약점 판정 연결
                    controller.CardUsed += (card, targets) => plays++; // 카드별 사용 신호 기록
                    target.GainBlock(5); // 두 타격 사이에 깨질 방어도 설정
                    yield return Play(repeated, hand, enemyViews[0], runner, points); // 실제 연속 베기 사용
                    CollectionAssert.AreEqual(new[] { 4, 4 }, hits.Select(hit => hit.FinalDamage)); // 각 타격의 방어력과 약점 반영 확인
                    CollectionAssert.AreEqual(new[] { 4, 1 }, hits.Select(hit => hit.AbsorbedDamage)); // 방어도 순차 소모 확인
                    CollectionAssert.AreEqual(new[] { 0, 3 }, hits.Select(hit => hit.AppliedDamage)); // 각 타격의 남은 체력 피해 확인
                    Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.ChariotMark)); // 명중 두 번에 전차 표식 확인
                    target.TakeDamage(target.CurrentHealth - 1, BattleDamageType.None); // 지정 적의 체력을 하나로 설정
                    hits.Clear(); // 처치 검사 기록 초기화
                    yield return Play(twin, hand, enemyViews[0], runner, points); // 빈사 상태 적에게 이연참 사용
                    Assert.IsTrue(target.IsDead); // 첫 타격 처치 확인
                    Assert.AreEqual(1, hits.Count); // 처치 후 남은 타격 중단 확인
                    Assert.AreEqual(other.MaxHealth, other.CurrentHealth); // 다른 적으로 타격 이동 방지 확인
                    hits.Clear(); // 단일 공격 검사 기록 초기화
                    yield return Play(basic, hand, enemyViews[1], runner, points); // 베기 한 장 사용
                    yield return Play(legacy, hand, enemyViews[1], runner, points); // 타격 수 필드 없는 기존 강타 사용
                    CollectionAssert.AreEqual(new[] { 4, legacy.EffectValue }, hits.Select(hit => hit.RawDamage)); // 신규 베기와 기존 강타의 단일 타격 확인
                    attacker.ApplyStatusEffect(BattleStatusEffectType.Blind, 50, 1, 1, other); // 독립 명중률 절반 설정
                    int seed = FindMissThenHitSeed(); // 첫 타격 실패와 두 번째 타격 성공 시드 조회
                    controller.CardUsed += (card, targets) => Random.InitState(seed); // 실제 공격 직전 난수 고정
                    hits.Clear(); // 명중 검사 기록 초기화
                    yield return Play(repeated, hand, enemyViews[1], runner, points); // 실명 중 연속 베기 사용
                    Assert.AreEqual(1, hits.Count); // 첫 빗나감 뒤 두 번째 명중 확인
                    attacker.RemoveStatusEffect(BattleStatusEffectType.Blind); // 전환 검사용 실명 해제
                    hits.Clear(); // 보스 전환 검사 기록 초기화
                    other.DamageResolved += (actor, victim, hit) => turns.QueueBossPhaseTransition(); // 첫 타격 직후 보스 전환 예약
                    yield return Play(twin, hand, enemyViews[1], runner, points); // 두 타격의 이연참 사용
                    CollectionAssert.AreEqual(new[] { 6 }, hits.Select(hit => hit.RawDamage)); // 첫 타격의 피해량과 후속 타격 중단 확인
                    Assert.AreEqual(sequence.Length, plays); // 타격 수와 무관한 카드당 한 번의 사용 신호 확인
                    Assert.AreEqual(sequence.Length, deck.DiscardPileCount); // 사용한 카드만 버린 더미로 이동 확인
                    Assert.AreEqual(0, points.CurrentActionPoints); // 카드마다 한 번의 행동력 지불 확인
                } // 카드 검사 종료
                hand.Unbind(); // 실제 손패 연결 해제
            } // 턴 검사 종료
        } // 전투 입력 종료
        finally // 독립 상태 정리
        { // 상태 정리 시작
            Random.state = previousRandom; // 이전 난수 상태 복원
            BattleCardDamageContext.Clear(); // 남은 약점 판정 제거
            Object.Destroy(canvas); // 시험 전투 화면 정리
            Object.Destroy(source); // 시험 덱 정리
            Object.Destroy(enemySource); // 시험 적 원본 정리
        } // 상태 정리 종료
    } // 전투 구성 종료

    private static int FindMissThenHitSeed() // 독립 명중 검사 시드 검색
    { // 시드 검색 시작
        for (int seed = 0; seed < 256; seed++) // 재현 가능한 난수 시드 순회
        { // 시드 확인 시작
            Random.InitState(seed); // 고정 시드 설정
            if (Random.Range(0, 100) >= 50 && Random.Range(0, 100) < 50) // 첫 실패 후 두 번째 성공 확인
            { // 적합한 시드 처리 시작
                return seed; // 검사 시드 반환
            } // 적합한 시드 처리 종료
        } // 시드 확인 종료
        throw new AssertionException("실패 후 명중하는 시드 누락"); // 재현 시드 누락 알림
    } // 시드 검색 종료

    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 공격 대상 화면 생성
    { // 화면 생성 시작
        var item = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 전투 유닛 화면 생성
        item.transform.SetParent(parent, false); // 전투 화면에 배치
        BattleUnitView view = item.GetComponent<BattleUnitView>(); // 대상 입력 조회
        view.Bind(unit); // 실제 유닛 연결
        return view; // 입력 화면 반환
    } // 화면 생성 종료

    private static IEnumerator Play(CardData card, BattleHandView hand, BattleUnitView target, BattleActionSequenceRunner runner, BattleActionPointRuntime points) // 손패와 적 클릭
    { // 카드 입력 시작
        BattleCardView view = hand.GetComponentsInChildren<BattleCardView>().First(item => item.RuntimeCard != null && item.RuntimeCard.SourceData == card); // 손패에서 사용할 카드 조회
        int before = points.CurrentActionPoints; // 입력 전 행동력 보관
        view.OnPointerClick(null); // 검 카드 선택
        target.OnPointerClick(null); // 같은 적 한 명 선택
        Assert.AreEqual(before - card.ApCost, points.CurrentActionPoints, card.DisplayName); // 행동력 한 번 차감 확인
        float deadline = Time.realtimeSinceStartup + 10f; // 실제 연출 대기 기한
        while (runner.IsBusy && Time.realtimeSinceStartup < deadline) // 타격과 복귀 연출 대기
        { // 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 연출 대기 종료
        Assert.IsFalse(runner.IsBusy, card.DisplayName); // 공격 연출 종료 확인
    } // 카드 입력 종료
} // 검사 종료
