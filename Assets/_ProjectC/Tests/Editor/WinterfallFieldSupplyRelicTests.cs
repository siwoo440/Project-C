using System.Linq; // 저장과 상점 목록에서 유물 조회
using NUnit.Framework; // 실제 원본 시트 값과 가져온 자료 대조
using UnityEngine; // 게임에서 사용하는 Resources 원본 조회

public sealed class WinterfallFieldSupplyRelicTests // 238일차 유물의 획득 및 저장 등록 검사
{ // 원본 유물 검사 시작
    [TestCase("RustedChaliceStand", 60, 321, 369, 163, 212)] // 녹슨 성배대의 원본 행과 가격 범위
    [TestCase("WornCalculator", 61, 303, 352, 154, 201)] // 손때 묻은 계산기의 원본 행과 가격 범위
    [TestCase("PinwheelBadge", 62, 241, 289, 123, 165)] // 바람개비 배지의 원본 행과 가격 범위
    [TestCase("IncompleteGasMask", 63, 223, 272, 114, 158)] // 불완전한 방독면의 원본 행과 가격 범위
    [TestCase("DistressFlare", 64, 252, 301, 128, 172)] // 조난 신호탄의 원본 행과 가격 범위
    [TestCase("MicroDrone", 65, 252, 301, 128, 172)] // 초소형 드론의 원본 행과 가격 범위
    [TestCase("LeatherNotebook", 66, 271, 320, 138, 184)] // 가죽 수첩의 원본 행과 가격 범위
    [TestCase("BlueInkBottle", 67, 241, 290, 123, 166)] // 푸른 잉크병의 원본 행과 가격 범위
    public void MatchesOriginalSheetAndHasOneRestorableShopOffer(string name, int row, int buyMin, int buyMax, int sellMin, int sellMax) // 여덟 게임 에셋의 상점·루팅·복원 경로 확인
    { // 원본 시트 대조 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 각 이름으로 실제 게임 유물 원본 조회
        Assert.IsNotNull(relic, name); // 신규 유물이 실제로 등록됐는지 확인
        var source = RelicSheetCatalog.Load().FindByName(relic.DisplayName); // 게임에서 읽는 원본 유물 행
        Assert.AreEqual(row, source.SourceRow); // 작업표에서 지정한 원본 행 확인
        Assert.AreEqual(relic.Description, source.Description); // 발동 조건과 게임 설명의 일치 확인
        CollectionAssert.AreEqual(new[] { buyMin, buyMax, sellMin, sellMax }, new[] { source.BuyMin, source.BuyMax, source.SellMin, source.SellMax }); // 구매 및 판매 원본 가격 범위 확인
        Assert.AreEqual(name, relic.Icon.texture.name); // 동일한 이름의 투명 아이콘 연결 확인
        var offer = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(item => item.Relic == relic); // 상점에서 식별자가 중복되지 않는 원본 상품 찾기
        Assert.AreEqual((buyMin + buyMax + 1) / 2, offer.Price); // 구매 구간 중간값을 기본 상품가로 적용
        Assert.AreEqual(1, Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics.Count(item => item == relic)); // 탐사 복원에서 유물 ID를 한 번만 등록
    } // 원본 시트 대조 종료
} // 원본 유물 검사 종료
