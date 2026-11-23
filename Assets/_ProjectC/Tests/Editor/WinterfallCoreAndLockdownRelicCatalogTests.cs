using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallCoreAndLockdownRelicCatalogTests // 격리실 유물의 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("CentralPowerFuse", 253, 231, 274, 119, 159, RelicEffectType.CentralPowerFuseShockedDefeatActionPoints, 2, RelicTriggerType.UnitDefeated, 0)] // 중앙 전력 퓨즈 원본
    [TestCase("CoolantValve", 254, 248, 284, 128, 173, RelicEffectType.CoolantValveInstantBurnCleanse, 1, RelicTriggerType.StatusApplied, 0)] // 냉각수 밸브 원본
    [TestCase("DeadlockBackupChip", 255, 265, 308, 138, 170, RelicEffectType.DeadlockBackupChipOpeningMarksTrade, 5, RelicTriggerType.BeforeBattleActions, 1)] // 데드락 백업칩 원본
    [TestCase("BasiliskEngineCore", 256, 282, 318, 144, 181, RelicEffectType.BasiliskEngineCoreOpeningGuard, 30, RelicTriggerType.BeforeBattleActions, 1)] // 바실리스크 엔진 코어 원본
    [TestCase("ScrapMotherReassemblyCore", 257, 299, 342, 153, 195, RelicEffectType.ScrapMotherReassemblyCoreMechanicalDefeatArmor, 5, RelicTriggerType.UnitDefeated, 0)] // 스크랩 마더의 재조립핵 원본
    [TestCase("CentralLockdownProtocol", 258, 316, 352, 163, 210, RelicEffectType.CentralLockdownProtocolActionLoss, 1, RelicTriggerType.TurnStarted, 0)] // 중앙 봉쇄 프로토콜 원본
    [TestCase("SpecimenPreservationCapsule", 259, 183, 226, 96, 130, RelicEffectType.SpecimenPreservationCapsuleBattleEndRecovery, 10, RelicTriggerType.BattleEnded, 1)] // 실험체 보존 캡슐 원본
    [TestCase("FaultyAiDecision", 260, 200, 236, 102, 141, RelicEffectType.FaultyAiDecisionRandomCardCost, 1, RelicTriggerType.TurnStarted, 0)] // 불량 AI의 판단식 원본
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
