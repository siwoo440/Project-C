using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallSwampDeepRelicCatalogTests // 격리실 유물의 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("SwampLeechSinew", 285, 325, 368, 167, 205, RelicEffectType.SwampLeechSinewBleedingDamageHealing, 10, RelicTriggerType.CardAttackHit, 0)] // 늪지 거머리 심줄 원본
    [TestCase("VenomSapCore", 286, 192, 228, 100, 143, RelicEffectType.VenomSapCorePoisonedDefeatSpread, 1, RelicTriggerType.UnitDefeated, 0)] // 독수액 핵 원본
    [TestCase("MudGolemCoreMud", 287, 209, 252, 110, 158, RelicEffectType.MudGolemCoreMudBlockEndHealing, 3, RelicTriggerType.TurnEnded, 0)] // 진흙 골렘의 심토 원본
    [TestCase("SwampFairyWingDust", 288, 226, 262, 115, 150, RelicEffectType.SwampFairyWingDustHealingEvasion, 10, RelicTriggerType.DirectCardHealing, 0)] // 늪의 요정 날개가루 원본
    [TestCase("HollowPredatorStomach", 289, 243, 286, 125, 165, RelicEffectType.HollowPredatorStomachKillHealing, 10, RelicTriggerType.UnitDefeated, 0)] // 포식괴물의 빈 위장 원본
    [TestCase("ScorchedRavineHeart", 290, 260, 296, 135, 180, RelicEffectType.ScorchedRavineHeartBurningTargetDamage, 20, RelicTriggerType.WhileOwned, 0)] // 불타는 협곡의 심장편 원본
    [TestCase("MountainStatueShoulder", 291, 277, 320, 144, 176, RelicEffectType.MountainStatueShoulderPhysicalMagicTrade, 15, RelicTriggerType.WhileOwned, 0)] // 산맥석상의 어깨조각 원본
    [TestCase("LavaKingThroneShard", 292, 294, 330, 150, 187, RelicEffectType.LavaKingThroneShardOpeningBurn, 1, RelicTriggerType.BeforeBattleActions, 1)] // 용암왕의 왕좌파편 원본
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
