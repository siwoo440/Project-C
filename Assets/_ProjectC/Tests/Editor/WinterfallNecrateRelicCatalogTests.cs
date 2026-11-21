using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallNecrateRelicCatalogTests // 네크레이트 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("NecrateClawDust", 146, 212, 248, 110, 155, RelicEffectType.SwordDamageAgainstPoison, 3)] // 네크레이트 손톱가루 원본 행과 가격 범위
    [TestCase("BottomlessBottle", 147, 229, 272, 120, 152, RelicEffectType.OpeningBottleActionPoints, 2)] // 바닥 없는 병 원본 행과 가격 범위
    [TestCase("BlackAbyssPlate", 148, 246, 282, 125, 162, RelicEffectType.NullifyFirstMentalDamage, 1)] // 검은 심연판 원본 행과 가격 범위
    [TestCase("InvertedStarCrest", 149, 263, 306, 135, 177, RelicEffectType.DrawOnThreeCardSuits, 1)] // 뒤집힌 별문장 원본 행과 가격 범위
    [TestCase("RiftEaterCage", 150, 280, 316, 145, 192, RelicEffectType.DebuffedEnemyTurnDamage, 5)] // 균열 먹는 새장 원본 행과 가격 범위
    [TestCase("AbyssResonanceStone", 151, 297, 340, 154, 188, RelicEffectType.WandResonanceDamage, 6)] // 어비스 공명석 원본 행과 가격 범위
    [TestCase("NecrateSealCore", 152, 314, 350, 160, 199, RelicEffectType.CleanseReceivedDebuff, 1)] // 네크레이트 봉인핵 원본 행과 가격 범위
    [TestCase("DarkenedBracelet", 153, 181, 224, 93, 137, RelicEffectType.StealthBreakDamage, 8)] // 어둠에 잠긴 팔찌 원본 행과 가격 범위
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
