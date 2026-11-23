using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallCorrosionRelicCatalogTests // 침식 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("NecrateFractureStone", 130, 240, 276, 124, 161, RelicEffectType.OpeningCorrosionResistanceDown, 5)] // 네크레이트 실금석 원본 행과 가격 범위
    [TestCase("VioletPulsingCore", 131, 257, 300, 134, 176, RelicEffectType.CorrosionDebuffDamage, 3)] // 보랏빛 맥동핵 원본 행과 가격 범위
    [TestCase("AbyssDustClock", 132, 274, 310, 140, 187, RelicEffectType.PeriodicEnemyMentalLoss, 3)] // 어비스 먼지시계 원본 행과 가격 범위
    [TestCase("BlackRiftNeedle", 133, 291, 334, 149, 183, RelicEffectType.DebuffedTargetCriticalChance, 10)] // 검은 균열침 원본 행과 가격 범위
    [TestCase("VoidTouchedLens", 134, 308, 344, 159, 198, RelicEffectType.BlindApplicationChanceBonus, 15)] // 공허에 닿은 렌즈 원본 행과 가격 범위
    [TestCase("CorrodedPulseCord", 135, 325, 368, 169, 213, RelicEffectType.OpeningPulseCord, 2)] // 침식된 맥박줄 원본 행과 가격 범위
    [TestCase("PurpleCrystalEye", 136, 192, 228, 98, 129, RelicEffectType.MarkPhysicalDefenseLoss, 1)] // 보라색 결정눈 원본 행과 가격 범위
    [TestCase("RiftSutureThread", 137, 209, 252, 108, 144, RelicEffectType.SutureCorrosionHealing, 6)] // 균열 봉합실 원본 행과 가격 범위
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
