using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallFoundryLavaRelicCatalogTests // 격리실 유물의 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("ReactorHeartShard", 261, 217, 260, 112, 156, RelicEffectType.ReactorHeartShardBurnOrShockDamage, 15, RelicTriggerType.CardAttackHit, 0)] // 원자로 심장편 원본
    [TestCase("HangarSeal", 262, 234, 270, 121, 152, RelicEffectType.HangarSealBossOpeningDamageReduction, 20, RelicTriggerType.WhileOwned, 0)] // 격납고 봉인장치 원본
    [TestCase("ScrapyardCrown", 263, 251, 294, 131, 167, RelicEffectType.ScrapyardCrownEnemyDefeatBlock, 6, RelicTriggerType.UnitDefeated, 0)] // 폐기장 왕관 원본
    [TestCase("LastSafetyLamp", 264, 268, 304, 137, 178, RelicEffectType.LastSafetyLampMentalRecovery, 10, RelicTriggerType.MentalChanged, 1)] // 마지막 안전등 원본
    [TestCase("CharredStoneCord", 265, 285, 328, 146, 192, RelicEffectType.CharredStoneCordBurnDamageReduction, 1, RelicTriggerType.BeforePeriodicDamage, 0)] // 그을린 돌끈 원본
    [TestCase("BlackBasaltShard", 266, 302, 338, 156, 189, RelicEffectType.BlackBasaltShardFirstHitDamageReduction, 3, RelicTriggerType.BeforeHealthDamage, 1)] // 검은 현무암 조각 원본
    [TestCase("AshStainedGloves", 267, 319, 362, 166, 204, RelicEffectType.AshStainedGlovesBurningSwordDamage, 2, RelicTriggerType.CardAttackHit, 0)] // 재 묻은 장갑 원본
    [TestCase("LavaMossSporeBottle", 268, 186, 222, 95, 138, RelicEffectType.LavaMossSporeBottlePeriodicHealing, 4, RelicTriggerType.TurnStarted, 0)] // 용암 이끼 포자병 원본
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
