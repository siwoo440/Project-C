using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallCoreRelicCatalogTests // 원정장비 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("DamagedCoreShard", 78, 271, 320, 138, 186, RelicEffectType.ErosionCoreRelicChance, 20)] // 침식핵 최초 파괴 원본 행과 가격 범위
    [TestCase("AfterglowLantern", 79, 191, 239, 98, 136, RelicEffectType.EventTrapDamageReduction, 2)] // 잔광 등불의 함정 피해 원본 행과 가격 범위
    [TestCase("RupturedBell", 80, 301, 349, 153, 197, RelicEffectType.HealthDamageBlock, 3)] // 파열된 방울의 생존 피해 원본 행과 가격 범위
    [TestCase("FadedCrown", 81, 241, 289, 123, 165, RelicEffectType.OpeningRandomCardDiscount, 1)] // 퇴색한 왕관의 첫 손패 원본 행과 가격 범위
    [TestCase("BlackCrown", 82, 283, 332, 144, 191, RelicEffectType.OpeningVulnerable, 20)] // 검은 왕관의 취약 원본 행과 가격 범위
    [TestCase("TaintedSampleVial", 83, 263, 312, 134, 180, RelicEffectType.AberrationVictoryDraw, 1)] // 오염 표본병의 드로우 원본 행과 가격 범위
    [TestCase("RiftSampleVial", 84, 191, 239, 98, 136, RelicEffectType.AberrationVictoryActionPoint, 1)] // 균열 표본병의 행동력 원본 행과 가격 범위
    [TestCase("CarvedChalk", 85, 303, 352, 154, 199, RelicEffectType.CardRemovalDiscount, 20)] // 세공된 초크의 상점 비용 원본 행과 가격 범위
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
