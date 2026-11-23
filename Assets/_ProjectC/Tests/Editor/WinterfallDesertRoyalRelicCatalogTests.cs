using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallDesertRoyalRelicCatalogTests // 모래와 재 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("RuinedSunGlyph", 219, 253, 296, 132, 164, RelicEffectType.RuinedSunGlyphChaliceBurnDamage, 5)] // 폐허의 태양문양 원본 행과 가격 범위
    [TestCase("BlackSandAnklet", 220, 270, 306, 138, 175, RelicEffectType.BlackSandAnkletActionLossDamage, 5)] // 검은 모래의 발목고리 원본 행과 가격 범위
    [TestCase("DesertWraithHood", 221, 287, 330, 147, 189, RelicEffectType.DesertWraithHoodStealthMental, 3)] // 사막 망령의 빈 후드 원본 행과 가격 범위
    [TestCase("WindWornCrown", 222, 304, 340, 157, 204, RelicEffectType.WindWornCrownDesertFirstCardDiscount, 1)] // 바람에 깎인 왕관 원본 행과 가격 범위
    [TestCase("BlackSunColdCore", 223, 321, 364, 167, 201, RelicEffectType.BlackSunColdCoreOpeningBurn, 2)] // 검은 태양의 식은 핵 원본 행과 가격 범위
    [TestCase("SandQueenBanner", 224, 188, 224, 96, 135, RelicEffectType.SandQueenBannerEnemySummonAttack, 1)] // 모래여왕의 명령패 원본 행과 가격 범위
    [TestCase("GlassDesertBrokenHeart", 225, 205, 248, 106, 150, RelicEffectType.GlassDesertBrokenHeartReflectTrade, 10)] // 유리사막의 깨진 심장 원본 행과 가격 범위
    [TestCase("EndlessPilgrimBell", 226, 222, 258, 115, 146, RelicEffectType.EndlessPilgrimBellLateTurnPulse, 5)] // 끝없는 순례자의 종 원본 행과 가격 범위
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
