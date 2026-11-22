using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallCaravanMedicineRelicCatalogTests // 모래와 재 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("SaltLeatherFragment", 211, 267, 310, 139, 185, RelicEffectType.SaltLeatherFragmentPeriodicTrade, 1)] // 소금가죽 파편 원본 행과 가격 범위
    [TestCase("BlackDesertTrackerPin", 212, 284, 320, 145, 178, RelicEffectType.BlackDesertTrackerPinOpeningMark, 1)] // 흑사막 추적자의 표식핀 원본 행과 가격 범위
    [TestCase("SandQueenEggshell", 213, 301, 344, 155, 193, RelicEffectType.SandQueenEggshellPeriodicWeakness, 2)] // 모래여왕의 알껍질 원본 행과 가격 범위
    [TestCase("GlassDesertReflector", 214, 318, 354, 164, 207, RelicEffectType.GlassDesertReflectorBlind, 25)] // 유리사막 반사판 원본 행과 가격 범위
    [TestCase("BlackSunAsh", 215, 185, 228, 97, 145, RelicEffectType.BlackSunAshBurningEnemyDamageUp, 8)] // 검은 태양의 재 원본 행과 가격 범위
    [TestCase("DryOasisHeart", 216, 202, 238, 103, 138, RelicEffectType.DryOasisHeartOpeningHealAndMental, 6)] // 마른 오아시스 심장 원본 행과 가격 범위
    [TestCase("SandstormBottle", 217, 219, 262, 113, 153, RelicEffectType.SandstormBottleOpeningEvasionAndAccuracy, 10)] // 모래폭풍 병 원본 행과 가격 범위
    [TestCase("CaravanSecretLedger", 218, 236, 272, 122, 167, RelicEffectType.CaravanSecretLedgerVictoryTrade, 15)] // 카라반 비밀장부 원본 행과 가격 범위
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
