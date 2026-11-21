using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallBanquetRelicCatalogTests // 네크레이트 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("DustyLibraryBookmark", 178, 306, 342, 158, 201, RelicEffectType.LibraryBookmarkOpeningPick, 3)] // 서고 먼지갈피 원본 행과 가격 범위
    [TestCase("LaughBehindMirror", 179, 323, 366, 168, 216, RelicEffectType.LaughMirrorDebuffReflection, 1)] // 거울 뒤의 웃음 원본 행과 가격 범위
    [TestCase("LordsWaxSeal", 180, 190, 226, 97, 132, RelicEffectType.LordsWaxOpeningEnemyWeakness, 2)] // 영주의 봉랍 원본 행과 가격 범위
    [TestCase("BanquetOilLamp", 181, 207, 250, 107, 147, RelicEffectType.BanquetOilPoisonPulse, 2)] // 만찬장의 기름등 원본 행과 가격 범위
    [TestCase("BlackCutleryCase", 182, 224, 260, 116, 161, RelicEffectType.BlackCutleryBleedStackDraw, 1)] // 검은 식기함 원본 행과 가격 범위
    [TestCase("DamagedOperaMask", 183, 241, 284, 126, 158, RelicEffectType.OperaMaskAfraidKillMentalLoss, 4)] // 파손된 오페라 가면 원본 행과 가격 범위
    [TestCase("OldServantContract", 184, 258, 294, 132, 169, RelicEffectType.ServantContractOpeningMark, 5)] // 낡은 종복 계약서 원본 행과 가격 범위
    [TestCase("ColdWindowNail", 185, 275, 318, 141, 183, RelicEffectType.ColdWindowBlindEnemyProtection, 10)] // 서늘한 창문못 원본 행과 가격 범위
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
