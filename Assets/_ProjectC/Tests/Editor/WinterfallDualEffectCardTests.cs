using System.Collections; // 전투 연출 대기
using System.Linq; // 원본과 전투 손패 조회
using NUnit.Framework; // 검사 결과 비교
using UnityEngine; // 실제 전투 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 손패 입력
using static WinterfallCommonCardExpansionTests; // 공용 카드의 원본 검사와 손패 입력 재사용

public sealed class WinterfallDualEffectCardTests // 네 공용 카드의 두 효과 검사
{ // 검사 시작
    [TestCase(21, "ADVANCING_SLASH", "전진 베기", "AdvancingSlash", CardType.Sword, "75D95FA9A6815A855C286A14DD05F5BD3BA4F63577D88F15F7CC06FDA6A64A93")] // 전진 베기 원본
    [TestCase(15, "HOLY_STRIKE", "신성의 일격", "HolyStrike", CardType.Sword, "C28D7110F46C03AF64455300A8C9C22C8FD577FD331EB7F508B2A0B86AE085F8")] // 신성의 일격 원본
    [TestCase(129, "CUP_OF_GRACE", "은총의 잔", "CupOfGrace", CardType.Cup, "32FDD615F64A85D602D152C94B2A01633DC7C61846091B9F5D5351E5DF38EA69")] // 은총의 잔 원본
    [TestCase(220, "FOCUSED_BREATH", "집중 호흡", "FocusedBreath", CardType.Pentacle, "EDD3ED50A35B7D849DF23ACF2F90984899630B9765BBC51EB51C7DB06E73DB43")] // 집중 호흡 원본
    public void MatchesOriginalAndRestorableTreasure(int row, string id, string title, string file, CardType type, string digest) // 시트와 보물 및 저장 원본 확인
    { // 원본 검사 시작
        VerifyTreasureArtwork(row, id, title, file, type, digest); // 지급 설정과 원본 그림의 해시 대조
    } // 원본 검사 종료

    [UnityTest] // 플레이 모드의 네 카드 실제 입력 검사 지정
    public IEnumerator OriginalCardsApplyTheirTwoEffectsAndSpendOnce() // 행동력과 대상별 두 효과 확인
    { // 전투 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        yield return PlayCards(); // 실제 손패와 대상 입력
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 검사 종료

    private static IEnumerator PlayCards() // 실제 획득 원본으로 독립 전투 구성
    { // 전투 구성 시작
        var offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers; // 보물 원본 조회
        string[] ids = { "ADVANCING_SLASH", "HOLY_STRIKE", "CUP_OF_GRACE", "CUP_OF_GRACE", "FOCUSED_BREATH", "FOCUSED_BREATH" }; // 원본 검증 순서
        var cards = ids.Select(id => offers.Single(offer => offer.OfferId == "REWARD_SHEET_" + id).Card).ToArray(); // 사용 가능한 원본 카드 조회
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 명단 조회
        var owner = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 모든 검사 카드의 소유자 생성
        var ally = BattleUnitRuntime.CreateAlly(roster.Characters[1]); // 단일 아군 선택용 동료 생성
        var fallen = BattleUnitRuntime.CreateAlly(roster.Characters[2]); // 회복 대상에서 제외할 전투불능 동료
        var allies = new[] { owner, ally, fallen }; // 생존자 둘과 전투불능 검사 동료 구성
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 저항과 약점이 없는 독립 적 원본
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData), BattleUnitRuntime.CreateEnemy(enemyData) }; // 단일 검 공격 비교용 적 둘 생성
        var source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, owner.CharacterSource)).ToArray()); // 실제 원본과 소유자를 덱에 등록
        var deck = BattleDeckRuntime.Create(source, allies, cards.Length, 216); // 시드를 고정한 전투 덱 생성
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
                    fallen.ApplyPersistentHealth(0); // 사망한 동료의 부활 금지 조건 설정
                    owner.ApplyPersistentHealth(10); // 소유자의 회복 여유 확보
                    ally.ApplyPersistentHealth(8); // 선택한 아군의 회복 여유 확보
                    int firstHealth = enemies[0].CurrentHealth; // 첫 대상의 기준 체력 저장
                    int secondHealth = enemies[1].CurrentHealth; // 둘째 대상의 기준 체력 저장
                    enemies[0].GainBlock(3); // 실제 방어도 흡수 검증 준비
                    yield return Play(cards[0], hand, enemyViews[0], runner, deck, points); // 전진 베기로 첫 적 공격
                    Assert.AreEqual(firstHealth - 5, enemies[0].CurrentHealth); // 물리 피해 여덟 중 방어도 세 점 흡수 확인
                    Assert.AreEqual(secondHealth, enemies[1].CurrentHealth); // 선택하지 않은 적 체력 보존 확인
                    Assert.AreEqual(2, owner.CurrentBlock); // 소유자 자신에게만 방어도 두 점 확인
                    Assert.AreEqual(0, ally.CurrentBlock); // 동료 방어도 보존 확인
                    yield return Play(cards[1], hand, enemyViews[1], runner, deck, points); // 신성의 일격으로 다른 적 공격
                    Assert.AreEqual(secondHealth - 15, enemies[1].CurrentHealth); // 둘째 적 피해 열다섯 확인
                    Assert.AreEqual(firstHealth - 5, enemies[0].CurrentHealth); // 첫 적의 추가 피해 방지 확인
                    Assert.AreEqual(15, owner.CurrentHealth); // 소유자 체력 다섯 회복 확인
                    Assert.AreEqual(13, ally.CurrentHealth); // 다른 생존 아군 체력 다섯 회복 확인
                    Assert.IsTrue(fallen.IsDead); // 전투불능 동료 부활 방지 확인
                    yield return Play(cards[2], hand, allyViews[1], runner, deck, points); // 은총의 잔으로 부상한 동료 선택
                    Assert.AreEqual(25, ally.CurrentHealth); // 선택한 동료 체력 열두 점 회복 확인
                    Assert.AreEqual(6, ally.CurrentBlock); // 선택한 동료 방어도 여섯 확인
                    Assert.AreEqual(15, owner.CurrentHealth); // 선택하지 않은 소유자의 체력 보존
                    ally.ApplyPersistentHealth(ally.MaxHealth); // 최대 체력 동료 설정
                    yield return Play(cards[3], hand, allyViews[1], runner, deck, points); // 최대 체력에서도 은총의 잔 사용
                    Assert.AreEqual(12, ally.CurrentBlock); // 회복 없이 방어도 여섯 추가 확인
                    owner.ChangeMental(50 - owner.CurrentMental, BattleMentalChangeReason.CardEffect); // 소유자 정신력 오십 설정
                    int startingHand = deck.HandCount; // 사용 전 손패 수 기록
                    yield return Play(cards[4], hand, null, runner, deck, points); // 집중 호흡으로 정신 회복과 드로우
                    Assert.AreEqual(55, owner.CurrentMental); // 소유자 정신력 다섯 회복 확인
                    Assert.AreEqual(startingHand, deck.HandCount); // 한 장 사용 후 한 장 보충 확인
                    Assert.IsNull(canvas.GetComponentInChildren<BattleDrawChoiceView>()); // 추가 버리기 선택 없음 확인
                    owner.ChangeMental(100, BattleMentalChangeReason.CardEffect); // 최대 정신력 조건 설정
                    int fullMental = owner.CurrentMental; // 최대 정신력 보관
                    yield return Play(cards[5], hand, null, runner, deck, points); // 최대 정신력에서 드로우만 적용
                    Assert.AreEqual(fullMental, owner.CurrentMental); // 정신력 상한 보존 확인
                    Assert.AreEqual(startingHand, deck.HandCount); // 최대 정신력에서도 한 장 드로우 확인
                    Assert.AreEqual(0, points.CurrentActionPoints); // 모든 사용 비용 차감 확인
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
