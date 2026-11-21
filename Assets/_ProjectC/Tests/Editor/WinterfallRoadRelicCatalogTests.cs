using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallRoadRelicCatalogTests // 원정장비 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("GrayRope", 86, 232, 279, 118, 160, RelicEffectType.ReducedForcedEncounterChance, 10)] // 회색 로프 원본 행과 가격 범위
    [TestCase("MetalDrawerHandle", 87, 222, 270, 113, 155, RelicEffectType.FirstShopPurchaseDiscount, 5)] // 금속 서랍손잡이 원본 행과 가격 범위
    [TestCase("BrokenShieldEmblem", 88, 264, 312, 134, 179, RelicEffectType.ShortenTwoPointDefenseLoss, 1)] // 파손된 방패문양 원본 행과 가격 범위
    [TestCase("CrudeMarkPin", 89, 221, 270, 113, 156, RelicEffectType.MarkedMagicalCardDamage, 1)] // 투박한 표식핀 원본 행과 가격 범위
    [TestCase("MetalHandbell", 90, 275, 324, 140, 187, RelicEffectType.RetainRandomOpeningCard, 1)] // 금속 핸드벨 원본 행과 가격 범위
    [TestCase("TravelerCharm", 91, 192, 239, 98, 136, RelicEffectType.ExplorationSuccessGoldPercent, 5)] // 여행자의 부적 원본 행과 가격 범위
    [TestCase("ErosionBellEarring", 92, 303, 352, 154, 199, RelicEffectType.FadeMentalLossReduction, 1)] // 침식 방울귀걸이 원본 행과 가격 범위
    [TestCase("AshStamp", 94, 241, 290, 123, 166, RelicEffectType.ExtraShopSpecialCard, 1)] // 잿빛 도장 원본 행과 가격 범위
    public void MatchesSheetArtworkShopAndSave(string key, int row, int buyMin, int buyMax, int sellMin, int sellMax, RelicEffectType effect, int amount) // 원본 행과 모든 실행 등록 경로 대조
    { // 시트와 리소스 일치 확인 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 실제 게임에서 획득할 유물 원본
        Assert.IsNotNull(relic, key); // 여덟 리소스의 누락 여부 확인
        Assert.AreEqual(effect, relic.EffectType); // 저장 순서에 맞춘 실제 효과 연결 확인
        Assert.AreEqual(amount, relic.EffectValue); // 유물의 기본 효과 수치 확인
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
