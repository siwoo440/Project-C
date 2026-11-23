using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallFieldWearRelicTests // 원정장비 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("RedInkBottle", 68, 252, 301, 127, 171)] // 붉은 잉크병 원본 행과 가격 범위
    [TestCase("GoldBookmark", 69, 261, 309, 133, 179)] // 금박 책갈피 원본 행과 가격 범위
    [TestCase("RiftSealingTape", 70, 244, 293, 124, 167)] // 균열 봉인테이프 원본 행과 가격 범위
    [TestCase("RetrievalMagnet", 71, 271, 319, 138, 183)] // 회수용 자석 원본 행과 가격 범위
    [TestCase("WhiteFeatherPendant", 73, 283, 332, 144, 190)] // 흰 깃털 펜던트 원본 행과 가격 범위
    [TestCase("BlackFeatherPendant", 74, 263, 311, 134, 179)] // 검은 깃털 펜던트 원본 행과 가격 범위
    [TestCase("FoldingCot", 75, 272, 320, 138, 184)] // 접이식 침대 원본 행과 가격 범위
    [TestCase("WornPatchCord", 77, 261, 310, 133, 178)] // 낡은 패치 노끈 원본 행과 가격 범위
    public void MatchesSheetArtworkShopAndSave(string key, int row, int buyMin, int buyMax, int sellMin, int sellMax) // 원본 행과 모든 실행 등록 경로 대조
    { // 시트와 리소스 일치 확인 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 실제 게임에서 획득할 유물 원본
        Assert.IsNotNull(relic, key); // 여덟 리소스의 누락 여부 확인
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
