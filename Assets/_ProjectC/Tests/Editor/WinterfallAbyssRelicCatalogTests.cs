using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallAbyssRelicCatalogTests // 어비스 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("BlackStardust", 138, 226, 262, 117, 158, RelicEffectType.MagicalHitMentalLoss, 2)] // 검은 별가루 원본 행과 가격 범위
    [TestCase("MisalignedHeartShard", 139, 243, 286, 127, 173, RelicEffectType.LowHealthOutgoingDamage, 15)] // 어긋난 심장파편 원본 행과 가격 범위
    [TestCase("AbyssWaxSeal", 140, 260, 296, 133, 166, RelicEffectType.ActionPreventingStatusDraw, 1)] // 어비스 밀랍인장 원본 행과 가격 범위
    [TestCase("SilenceCrystalBell", 141, 277, 320, 142, 180, RelicEffectType.AfraidEnemyAttackReduction, 1)] // 침묵 결정종 원본 행과 가격 범위
    [TestCase("RedRiftFiber", 142, 294, 330, 152, 195, RelicEffectType.EnemyBleedDamageBonus, 1)] // 붉은 균열섬유 원본 행과 가격 범위
    [TestCase("BlackenedStethoscope", 143, 311, 354, 162, 210, RelicEffectType.OpeningStethoscopeMark, 1)] // 검게 물든 청진기 원본 행과 가격 범위
    [TestCase("VoidRootlets", 144, 328, 364, 167, 202, RelicEffectType.FifthEnemyDebuffHeal, 3)] // 공허의 잔뿌리 원본 행과 가격 범위
    [TestCase("CorrosionChimeNecklace", 145, 195, 238, 100, 140, RelicEffectType.EnemyMentalLossActionPoint, 1)] // 침식 방울목걸이 원본 행과 가격 범위
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
