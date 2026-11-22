using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallCanyonSwampRelicCatalogTests // 격리실 유물의 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("RockfallWarningPlaque", 269, 203, 246, 105, 153, RelicEffectType.RockfallWarningPlaqueCanyonTrapReduction, 15, RelicTriggerType.BeforeEventTrapDamage, 0)] // 낙석 경고패 원본
    [TestCase("ObsidianBladeShard", 270, 220, 256, 114, 149, RelicEffectType.ObsidianBladeShardSwordBleed, 1, RelicTriggerType.CardAttackHit, 0)] // 흑요석 칼편 원본
    [TestCase("FlameVeinDebris", 271, 237, 280, 124, 164, RelicEffectType.FlameVeinDebrisBurnResistance, 5, RelicTriggerType.StatusApplied, 0)] // 화염 광맥 부스러기 원본
    [TestCase("AshDustStatueEye", 272, 254, 290, 130, 175, RelicEffectType.AshDustStatueEyeEnemyAccuracyDown, 5, RelicTriggerType.WhileOwned, 0)] // 재먼지 석상 눈 원본
    [TestCase("StoneThornBundle", 273, 271, 314, 139, 171, RelicEffectType.StoneThornBundleRetaliation, 3, RelicTriggerType.DamageApplied, 0)] // 돌가시 묶음 원본
    [TestCase("CanyonWindWhistle", 274, 288, 324, 149, 186, RelicEffectType.CanyonWindWhistleOpeningEvasion, 8, RelicTriggerType.BeforeBattleActions, 0)] // 협곡 바람피리 원본
    [TestCase("WetReedKnot", 275, 305, 348, 159, 201, RelicEffectType.WetReedKnotPoisonDamageReduction, 1, RelicTriggerType.BeforePoisonDamage, 0)] // 젖은 갈대매듭 원본
    [TestCase("BlackSwampWaterBottle", 276, 322, 358, 164, 211, RelicEffectType.BlackSwampWaterBottlePoisonChance, 10, RelicTriggerType.OptionalCardEffectRoll, 0)] // 검은 늪물병 원본
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
