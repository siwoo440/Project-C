using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallCanyonLavaRelicCatalogTests // 격리실 유물의 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("LeechFangNecklace", 277, 189, 232, 97, 131, RelicEffectType.LeechFangNecklaceSingleTargetHealing, 5, RelicTriggerType.CardAttackHit, 0)] // 거머리 이빨목걸이 원본
    [TestCase("MudCharm", 278, 206, 242, 107, 146, RelicEffectType.MudCharmActionPointLossBlock, 5, RelicTriggerType.ActionPointReductionApplied, 0)] // 진흙 부적 원본
    [TestCase("MistFlowerPollen", 279, 223, 266, 117, 161, RelicEffectType.MistFlowerPollenOpeningBlind, 25, RelicTriggerType.BeforeBattleActions, 1)] // 안개 꽃가루 원본
    [TestCase("BasaltGiantFinger", 280, 240, 276, 122, 153, RelicEffectType.BasaltGiantFingerLowHealthDefense, 4, RelicTriggerType.WhileOwned, 0)] // 화산암 거한의 손가락 원본
    [TestCase("ObsidianExecutionMask", 281, 257, 300, 132, 168, RelicEffectType.ObsidianExecutionMaskBleedingDefeatDamage, 6, RelicTriggerType.UnitDefeated, 0)] // 흑요석 참수면 원본
    [TestCase("LavaVeinGuardianCore", 282, 274, 310, 142, 183, RelicEffectType.LavaVeinGuardianCoreBurningEnemiesActionPoints, 1, RelicTriggerType.TurnStarted, 0)] // 용암맥 수호핵 파편 원본
    [TestCase("RedCanyonBulwarkStone", 283, 291, 334, 151, 197, RelicEffectType.RedCanyonBulwarkStoneOpeningBlockDrawTrade, 10, RelicTriggerType.BeforeBattleActions, 1)] // 붉은 협곡 방벽석 원본
    [TestCase("AshSealSigil", 284, 308, 344, 157, 190, RelicEffectType.AshSealSigilResistanceHealingTrade, 10, RelicTriggerType.WhileOwned, 0)] // 재의 봉인문양 원본
    public void MatchesSheetArtworkShopAndSave(string key, int row, int buyMin, int buyMax, int sellMin, int sellMax, RelicEffectType effect, int amount, RelicTriggerType trigger, int perBattle) // 원본 행과 모든 실행 등록 경로 대조
    { // 시트와 리소스 일치 확인 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 실제 게임에서 획득할 유물 원본
        Assert.IsNotNull(relic, key); // 여덟 리소스의 누락 여부 확인
        Assert.AreEqual(effect, relic.EffectType); // 저장 순서에 맞춘 실제 효과 연결 확인
        Assert.AreEqual(amount, relic.EffectValue); // 유물의 기본 효과 수치 확인
        Assert.AreEqual(trigger, relic.TriggerType); // 원본과 같은 실제 발동 시점 확인
        Assert.AreEqual(perBattle, relic.MaximumTriggersPerBattle); // 전투당 발동 한도 확인
        var sheet = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 원본 시트와 현지화 설명 조회
        Assert.AreEqual(row, sheet.SourceRow); // 예정 행 그대로의 유물 확인
        Assert.AreEqual(sheet.Description, relic.Description); // 실제 플레이어에게 표시할 정확한 설명 확인
        CollectionAssert.AreEqual(new[] { buyMin, buyMax, sellMin, sellMax }, new[] { sheet.BuyMin, sheet.BuyMax, sheet.SellMin, sheet.SellMax }); // 시트의 구매와 판매 범위 보존 확인
        Assert.AreEqual(key, relic.Icon.texture.name); // 유물의 독립 투명 원화 연결 확인
        var shop = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 판매와 보물 풀의 원본 조회
        Assert.AreEqual((buyMin + buyMax + 1) / 2, shop.Offers.Single(offer => offer.Relic == relic).Price); // 하나의 기본 가격 설정과 중복 등록 확인
        Assert.AreEqual(1, Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics.Count(item => item == relic)); // 하나의 저장 복원 식별자 연결 확인
    } // 시트와 리소스 일치 확인 종료
} // 유물 원본 검사 종료
