using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallRelicConditionsCatalogTests // 조건부 발동 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("OldHolyWaterStopper", 122, 254, 290, 132, 165, RelicEffectType.FirstCupHealingBonus, 3)] // 낡은 성수 마개 원본 행과 가격 범위
    [TestCase("WobblyNib", 123, 271, 314, 141, 179, RelicEffectType.PentacleResolvedChanceDraw, 1)] // 흔들리는 펜촉 원본 행과 가격 범위
    [TestCase("ExtinguishedLanternWick", 124, 288, 324, 147, 190, RelicEffectType.StealthEvasionBonus, 5)] // 꺼진 등불 심지 원본 행과 가격 범위
    [TestCase("ScratchedDie", 125, 305, 348, 157, 205, RelicEffectType.OpeningRandomAllyBoost, 3)] // 흠집 난 주사위 원본 행과 가격 범위
    [TestCase("WornCoinString", 126, 322, 358, 166, 201, RelicEffectType.AdditionalShopDiscount, 5)] // 손때 묻은 동전줄 원본 행과 가격 범위
    [TestCase("CrackedMapPin", 127, 189, 232, 99, 139, RelicEffectType.ExtraEventRewardChoice, 1)] // 갈라진 지도핀 원본 행과 가격 범위
    [TestCase("DarkWhistleHole", 128, 206, 242, 105, 150, RelicEffectType.FearTargetDirectDamage, 2)] // 어두운 피리구멍 원본 행과 가격 범위
    [TestCase("RustedCircuitPin", 129, 223, 266, 115, 147, RelicEffectType.FirstMechanicalDirectHitBonus, 4)] // 녹슨 회로핀 원본 행과 가격 범위
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
