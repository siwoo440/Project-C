using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallFactorySensorRelicCatalogTests // 모래와 재 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("WatchPodLens", 236, 242, 278, 123, 168, RelicEffectType.WatchPodLensOpeningMark, 1, RelicTriggerType.BeforeBattleActions, 1)] // 감시 포드 렌즈 원본
    [TestCase("LeakageCoil", 237, 259, 302, 133, 165, RelicEffectType.LeakageCoilShockedWandDamage, 4, RelicTriggerType.CardAttackHit, 0)] // 누전 코일 원본
    [TestCase("CompressionPincerHandle", 238, 276, 312, 143, 180, RelicEffectType.CompressionPincerHandleReducedActionDamage, 10, RelicTriggerType.CardAttackHit, 0)] // 압착 집게 손잡이 원본
    [TestCase("HeatSeeker", 239, 293, 336, 152, 194, RelicEffectType.HeatSeekerIgnoreFirstBlind, 1, RelicTriggerType.WhileOwned, 1)] // 열원 탐색기 원본
    [TestCase("ConveyorTooth", 240, 310, 346, 158, 205, RelicEffectType.ConveyorToothMechanicalBleedConversion, 2, RelicTriggerType.OptionalCardEffectRoll, 0)] // 컨베이어 톱니 원본
    [TestCase("LeakageTurretCasing", 241, 327, 370, 168, 202, RelicEffectType.LeakageTurretCasingShieldAttackShock, 1, RelicTriggerType.AfterCardResolved, 0)] // 누전 포탑 탄피 원본
    [TestCase("ScrapSyringe", 242, 194, 230, 101, 140, RelicEffectType.ScrapSyringeHealingTrade, 20, RelicTriggerType.DirectCardHealing, 0)] // 스크랩 주사기 원본
    [TestCase("MechanicalBugShell", 243, 211, 254, 111, 155, RelicEffectType.MechanicalBugShellAreaReduction, 10, RelicTriggerType.WhileOwned, 0)] // 기계벌레 껍질 원본
    public void MatchesSheetArtworkShopAndSave(string key, int row, int buyMin, int buyMax, int sellMin, int sellMax, RelicEffectType effect, int amount, RelicTriggerType trigger, int perBattle) // 원본 행과 모든 실행 등록 경로 대조
    { // 시트와 리소스 일치 확인 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 실제 게임에서 획득할 유물 원본
        Assert.IsNotNull(relic, key); // 여덟 리소스의 누락 여부 확인
        Assert.AreEqual(effect, relic.EffectType); // 저장 순서에 맞춘 실제 효과 연결 확인
        Assert.AreEqual(amount, relic.EffectValue); // 유물의 기본 효과 수치 확인
        Assert.AreEqual(trigger, relic.TriggerType); // 원본과 같은 실제 발동 시점 확인
        Assert.AreEqual(perBattle, relic.MaximumTriggersPerBattle); // 전투당 발동 한도 확인
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
