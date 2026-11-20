using System.Collections; // 실제 카드 애니메이션의 프레임 대기
using System.Linq; // 저장과 판매 목록에서 유물 조회
using NUnit.Framework; // 원본과 Unity 등록 상태 비교
using UnityEngine; // Resources에서 실제 게임 자산 불러오기
using UnityEngine.TestTools; // 실제 Play Mode 손패 검사
using Fight = WinterfallSwordCombatTests.Fight; // 현재 실제 검 카드 UI의 검증 환경 재사용

public sealed class WinterfallSupplyRelicTests // 236일차 유물 원본과 전투 효과 검사
{ // 유물 검사 시작
    [TestCase("SilverHeartPin", 50, 244, 293)] // 은빛 심장핀 원본 행과 구매 범위
    [TestCase("BlackHeartPin", 51, 233, 282)] // 검은 심장핀 원본 행과 구매 범위
    [TestCase("SpinningFlag", 52, 251, 300)] // 회전 깃발 원본 행과 구매 범위
    [TestCase("RedSpring", 53, 262, 309)] // 붉은 스프링 원본 행과 구매 범위
    [TestCase("GlassMarble", 54, 264, 312)] // 유리 구슬 원본 행과 구매 범위
    [TestCase("BlackIronMarble", 55, 271, 319)] // 흑철 구슬 원본 행과 구매 범위
    [TestCase("ThinMetalThread", 56, 262, 311)] // 얇은 금속 실 원본 행과 구매 범위
    [TestCase("OldAmmoBelt", 57, 271, 320)] // 구식 탄약띠 원본 행과 구매 범위
    public void RelicUsesTheSourceRowAndHasOneShopOfferAndSaveEntry(string file, int row, int low, int high) // 여덟 유물의 정상 게임 진입점 확인
    { // 원본 비교 시작
        var relic = Resources.Load<RelicData>("Relics/" + file); // 실제 게임 유물 자산 조회
        Assert.IsNotNull(relic, file); // 누락된 게임 유물 검사
        var sheet = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 시트 행과 가격 원본 조회
        Assert.AreEqual(row, sheet.SourceRow); // 계획된 시트 행 확인
        Assert.AreEqual(sheet.Description, relic.Description); // 카탈로그와 게임 효과 설명 일치 확인
        Assert.AreEqual(file, relic.Icon.texture.name); // 같은 이름의 개별 원화 참조 확인
        var offer = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(item => item.Relic == relic); // 유일한 상점 상품 확인
        Assert.AreEqual((low + high + 1) / 2, offer.Price); // 지정된 구간 중앙의 기본 가격 확인
        Assert.AreEqual(1, Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics.Count(item => item == relic)); // 중복 없는 회차 저장 후보 확인
    } // 원본 비교 종료

    [UnityTest] // 저체력 회복과 고체력 피해, 비용 및 단일 타격 검사
    public IEnumerator HeartPinsSpringAndBeltUseTheActualHealthAndHandCosts() // 두 체력 경계와 턴당 비용 및 연타 구분
    { // 체력과 비용 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 인벤토리와 전투 손패 진입
        Hold("SilverHeartPin", "BlackHeartPin", "RedSpring", "OldAmmoBelt"); // 효과를 적용할 네 유물 획득
        using (var fight = new Fight("THRUST", "THRUST", "DOUBLE_SLASH", "DOUBLE_SLASH")) // 단타와 연타를 같은 소유자로 구성
        { // 체력과 손패 준비 시작
            var actor = fight.Allies[0]; // 검의 실제 소유자
            int lowLimit = actor.MaxHealth * 3 / 10; // 회복 증가가 적용되는 최대 정수 체력
            actor.ApplyPersistentHealth(lowLimit + 1); // 은빛 심장핀의 체력 경계 바로 위
            Assert.AreEqual(3, actor.RestoreHealth(3)); // 경계 위에서 기존 회복량 유지
            actor.ApplyPersistentHealth(lowLimit); // 회복 증가가 가능한 체력으로 설정
            Assert.AreEqual(5, actor.RestoreHealth(3)); // 은빛 심장핀을 붙인 실제 회복량
            int highLimit = (actor.MaxHealth * 7 + 9) / 10; // 피해 가산이 적용되는 최소 정수 체력
            actor.ApplyPersistentHealth(highLimit - 1); // 검은 심장핀의 경계 바로 아래
            yield return fight.Use("THRUST", 0); // 먼저 행동력을 한 점 쓰는 단일 공격
            Assert.AreEqual(192, fight.Enemies[0].CurrentHealth); // 기본 칠과 탄약띠 한 점만 적용
            Assert.AreEqual(0, fight.View("THRUST").RuntimeCard.ApCost); // 다음 한 점 카드에 스프링 예약 확인
            actor.ApplyPersistentHealth(highLimit); // 검은 심장핀이 발동할 체력 경계 진입
            yield return fight.Use("THRUST", 1); // 할인된 카드가 첫 고체력 명중
            Assert.AreEqual(190, fight.Enemies[1].CurrentHealth); // 탄약띠와 검은 심장핀의 합산 피해
            Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 비용이 영으로 낮아진 두 번째 사용
            Assert.AreEqual(2, fight.View("DOUBLE_SLASH").RuntimeCard.ApCost); // 나머지 손패의 스프링 할인 해제
            yield return fight.Use("DOUBLE_SLASH", 2); // 같은 턴의 한 대상 두 타격 공격
            Assert.AreEqual(192, fight.Enemies[2].CurrentHealth); // 같은 턴 가산과 연타에 탄약띠 적용 차단
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴의 발동 제한 재설정
            yield return fight.Use("DOUBLE_SLASH", 3); // 다음 턴 고체력의 첫 두 타격
            Assert.AreEqual(190, fight.Enemies[3].CurrentHealth); // 첫 타격에만 심장핀 두 점 증가
            Assert.AreEqual(0, fight.View("THRUST").RuntimeCard.ApCost); // 다음 턴 첫 유료 카드가 스프링 재예약
        } // 체력과 손패 정리 종료
        yield return new ExitPlayMode(); // 독립 전투 환경 종료
    } // 체력과 비용 검사 종료

    [UnityTest] // 처치 드로우와 전투 손패 상한 검사
    public IEnumerator FlagDrawsAtMostOnceAfterTheUsedCardLeavesAndThreadRaisesTheLimit() // 실제 처치와 카드 이동 및 손패 확장 확인
    { // 처치 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 환경 준비
        Hold("SpinningFlag", "ThinMetalThread"); // 회전 깃발과 얇은 금속 실 획득
        using (var fight = new Fight("THRUST", "THRUST", "THRUST", "DOUBLE_SLASH")) // 세 번의 단일 처치와 별도 버림 카드 준비
        { // 처치 전투 시작
            Assert.AreEqual(5, fight.Deck.MaxHandSize); // 원래 네 장에 유물의 한 자리 가산 확인
            Assert.AreEqual(4, fight.Deck.HandCount); // 손패 상한만 증가하고 시작 드로우 유지
            Assert.IsTrue(fight.Deck.DiscardCard(fight.View("DOUBLE_SLASH").RuntimeCard)); // 되뽑을 수 있는 별도 버림 카드 준비
            foreach (var enemy in fight.Enemies.Take(3)) // 마지막 적은 살아 있는 상태에서만 드로우 확인
            { // 처치할 첫 세 적의 체력 준비 시작
                enemy.TakeDamage(195, BattleDamageType.None); // 각 적을 단일 카드로 처치할 수 있게 조절
            } // 처치 대상 준비 종료
            yield return UseLethalThrust(fight, 0); // 첫 직접 처치 후 사용 카드 정산
            Assert.AreEqual(3, fight.Deck.HandCount); // 한 장 사용 후 드로우 한 장 확인
            yield return UseLethalThrust(fight, 1); // 같은 턴에 두 번째 적 처치
            Assert.AreEqual(2, fight.Deck.HandCount); // 같은 아군 턴의 두 번째 드로우 차단
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 발동 횟수를 갱신하는 라운드 전환
            int before = fight.Deck.HandCount; // 새 턴의 드로우 전 기준 장수
            yield return UseLethalThrust(fight, 2); // 두 번째 턴의 첫 적 처치
            Assert.AreEqual(before, fight.Deck.HandCount); // 다음 아군 턴에 다시 한 장 획득
            Assert.IsTrue(fight.Deck.IsStateValid()); // 카드 더미 중복과 손패 상한 확인
        } // 처치 전투 정리
        yield return new ExitPlayMode(); // 독립 전투 종료
    } // 처치 유물 검사 종료

    private static IEnumerator UseLethalThrust(Fight fight, int target) // 재셔플 여부와 독립적으로 실제 처치와 비용 확인
    { // 처치 카드 입력 시작
        var view = fight.View("THRUST"); // 현재 사용 가능한 찌르기 조회
        int remaining = fight.Points.CurrentActionPoints - view.RuntimeCard.ApCost; // 실제로 지불하고 남을 행동력
        view.OnPointerClick(null); // 실제 손패 카드 선택
        fight.EnemyViews[target].OnPointerClick(null); // 처치할 적 클릭
        Assert.AreEqual(remaining, fight.Points.CurrentActionPoints); // 현재 공격의 비용 지불 확인
        float deadline = Time.realtimeSinceStartup + 10f; // 실제 카드 효과의 대기 기한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 처치 드로우와 카드 연출 종료 대기
        { // 프레임 대기 시작
            yield return null; // 다음 전투 프레임 대기
        } // 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 사용 카드 정산 종료 확인
        Assert.IsTrue(fight.Enemies[target].IsDead); // 실제 카드로 대상 처치 확인
    } // 처치 카드 입력 종료

    [UnityTest] // 유리 구슬의 면역 제외와 전투당 한 번 확인
    public IEnumerator GlassMarbleGuaranteesTheFirstEligibleOptionalStatusInEachFight() // 실제 출혈 판정에 유리 구슬 연결 확인
    { // 출혈 무작위 검사 시작
        yield return new EnterPlayMode(); // 실제 상태와 전투 입력 준비
        yield return VerifyGlassMarble(); // 플레이 모드 전환 이후에 난수 판정 실행
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 출혈 무작위 검사 종료

    private static IEnumerator VerifyGlassMarble() // 플레이 모드에서 시작하는 독립 난수 검사
    { // 유리 구슬 보정 비교 시작
        Hold("GlassMarble"); // 유리 구슬 실물 획득
        int seed = SeedWithRoll(80, 99); // 기본 출혈 확률이 실패하는 난수 준비
        for (int run = 0; run < 2; run++) // 새 전투에서 유물 횟수가 초기화되는지 비교
        { // 개별 전투 검사 시작
            using var fight = new Fight("THRUST", "THRUST", "THRUST"); // 실제 출혈 확률 카드 세 장
            fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 모든 명중의 같은 실패 난수 고정
            fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 1, 1); // 최초 대상의 약화 면역 준비
            yield return fight.Use("THRUST", 0); // 면역 대상은 유리 구슬 예약을 소비하지 않음
            Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Bleed)); // 면역 대상에게 출혈 미적용
            yield return fight.Use("THRUST", 1); // 실제 보정 대상에게 한 번 출혈 보장
            Assert.IsNotNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Bleed)); // 실패 범위에서도 보장된 상태 확인
            yield return fight.Use("THRUST", 2); // 같은 전투의 두 번째 유효 판정
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Bleed)); // 한 번 쓴 뒤 기존 확률 실패 확인
        } // 개별 전투 검사 종료
    } // 유리 구슬 보정 비교 종료

    [UnityTest] // 흑철 구슬의 정수 확률 구간을 실제 타격으로 검사
    public IEnumerator IronMarbleChangesOnlyTheEligibleDamageRoll() // 같은 굴림의 보유 전후 피해와 다른 조합 대조
    { // 무작위 피해 검사 시작
        yield return new EnterPlayMode(); // 실제 손패에서 검사 카드 사용
        var card = ScriptableObject.CreateInstance<CardData>(); // 아직 등록되지 않은 6·12 원본과 같은 자료형
        int seed = SeedWithRoll(50, 74); // 기본 확률 실패와 유물 확률 성공이 갈리는 정수 구간
        try // 임시 원본 정리 보장
        { // 보유 전후 타격 검사 시작
            JsonUtility.FromJsonOverwrite("{\"cardId\":\"CRD_DAY236_RANDOM\",\"displayName\":\"무작위 피해 검사\",\"targetType\":3,\"apCost\":1,\"damageType\":1,\"effectValue\":6,\"randomDamageHighValue\":12}", card); // 6·12 타격의 명시적인 원본 범위
            yield return UseRandomCard(card, seed, 6); // 유물 없이 같은 굴림의 낮은 피해
            Hold("BlackIronMarble"); // 시험 중간에 흑철 구슬 획득
            yield return UseRandomCard(card, seed, 12); // 유물 보유 시 같은 정수 굴림의 높은 피해
            yield return UseRandomCard(card, SeedWithRoll(75, 99), 6); // 보유해도 높은 피해가 확정은 아닌지 확인
            JsonUtility.FromJsonOverwrite("{\"randomDamageHighValue\":11}", card); // 6·12가 아닌 다른 조합 설정
            yield return UseRandomCard(card, seed, 6); // 다른 조합에는 확률 가산을 적용하지 않음
        } // 보유 전후 타격 검사 종료
        finally // 검사 중단 시에도 임시 원본 해제
        { // 임시 원본 해제 시작
            Object.Destroy(card); // 검사 카드 원본만 삭제
        } // 임시 원본 해제 종료
        yield return new ExitPlayMode(); // 실제 전투 검사 종료
    } // 무작위 피해 검사 종료

    private static IEnumerator UseRandomCard(CardData card, int seed, int expected) // 지정한 난수 구간의 실제 손패 피해 확인
    { // 무작위 피해 전투 시작
        using var fight = new Fight(new[] { card }); // 실제 전투 카드 더미와 대상 생성
        fight.Controller.CardUsed += (used, targets) => Random.InitState(seed); // 타격 직전 첫 난수 고정
        yield return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 실제 클릭과 정산 경로로 피해 적용
        Assert.AreEqual(200 - expected, fight.Enemies[0].CurrentHealth); // 같은 적에게 적용된 기대 피해 대조
    } // 무작위 피해 전투 종료

    private static int SeedWithRoll(int low, int high) // 테스트에서 필요한 닫힌 정수 구간의 시드 조회
    { // 난수 구간 확인 시작
        var previous = Random.state; // 기존 게임 난수 상태 보관
        for (int seed = 0; seed < 10000; seed++) // 재현 가능한 첫 번째 정수 굴림 조회
        { // 시드 후보 검사 시작
            Random.InitState(seed); // 후보 시드 적용
            int roll = Random.Range(0, 100); // 실제 게임과 같은 정수 굴림
            if (roll >= low && roll <= high) // 지정한 구간 적중 확인
            { // 후보 확정 시작
                Random.state = previous; // 첫 시드 확인 뒤 기존 난수 복원
                return seed; // 같은 정수 굴림을 생성할 시드 반환
            } // 후보 확정 종료
        } // 시드 후보 검사 종료
        Random.state = previous; // 시드를 못 찾은 경우에도 기존 난수 복원
        throw new System.InvalidOperationException("검사용 난수 시드 누락"); // 재현 환경 오류 보고
    } // 난수 구간 확인 종료

    private static void Hold(params string[] files) // 실제 게임 유물 원본 획득
    { // 유물 획득 시작
        var relics = RelicRunManager.EnsureInstance(); // 현재 전투용 회차 유물 보관함
        foreach (string file in files) // 지정한 신규 원본 확인
        { // 개별 획득 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(Resources.Load<RelicData>("Relics/" + file)), file); // 원본 획득 경로 검증
        } // 개별 획득 종료
    } // 유물 획득 종료
} // 유물 검사 종료
