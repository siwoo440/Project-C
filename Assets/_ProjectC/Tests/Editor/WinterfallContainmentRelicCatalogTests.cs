using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallContainmentRelicCatalogTests // 격리실 유물의 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("SecurityEnforcerBadge", 245, 245, 288, 126, 162, RelicEffectType.SecurityEnforcerBadgeDebuffedDamage, 15, RelicTriggerType.CardAttackHit, 0)] // 보안 집행체 배지 원본
    [TestCase("RecoveryDroneHook", 246, 262, 298, 136, 177, RelicEffectType.RecoveryDroneHookMarkedActionLoss, 1, RelicTriggerType.CardAttackHit, 0)] // 회수 드론 갈고리 원본
    [TestCase("MagneticCompressionCore", 247, 279, 322, 145, 191, RelicEffectType.MagneticCompressionCoreEvasionDown, 10, RelicTriggerType.TurnStarted, 0)] // 자기장 압축핵 원본
    [TestCase("SurveillanceCoreFragment", 248, 296, 332, 151, 184, RelicEffectType.SurveillanceCoreFragmentResistanceTrade, 10, RelicTriggerType.BeforePlayerTurnDraw, 0)] // 감시 코어 조각 원본
    [TestCase("WasteHeatRadiator", 249, 313, 356, 161, 199, RelicEffectType.WasteHeatRadiatorBurnAttackUp, 1, RelicTriggerType.DamageApplied, 0)] // 폐열 방열판 원본
    [TestCase("EmergencyRebootSwitch", 250, 180, 216, 94, 137, RelicEffectType.EmergencyRebootSwitchLowHealthHeal, 12, RelicTriggerType.DamageApplied, 1)] // 긴급 재부팅 스위치 원본
    [TestCase("BrokenBlackBox", 251, 197, 240, 103, 151, RelicEffectType.BrokenBlackBoxAllyDefeatDraw, 2, RelicTriggerType.UnitDefeated, 0)] // 고장 난 블랙박스 원본
    [TestCase("IsolationRoomNumberTag", 252, 214, 250, 109, 144, RelicEffectType.IsolationRoomNumberTagDualOpeningMarks, 1, RelicTriggerType.BeforeBattleActions, 1)] // 격리실 번호표 원본
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
