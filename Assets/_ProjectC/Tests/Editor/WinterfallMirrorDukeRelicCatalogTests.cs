using System.Linq; // 유물의 실제 상품과 복원 등록 검색
using NUnit.Framework; // 시트와 Unity 원본의 일치 검사
using UnityEngine; // 실제 게임의 리소스 조회

public sealed class WinterfallMirrorDukeRelicCatalogTests // 네크레이트 유물 원본과 등록 검사
{ // 장비 유물 검사 시작
    [TestCase("CollapsedThroneScrew", 186, 292, 328, 151, 198, RelicEffectType.ThroneScrewBossOpeningActionPoints, 2)] // 붕괴 왕좌의 나사 원본 행과 가격 범위
    [TestCase("RedEyeInPortrait", 187, 309, 352, 161, 195, RelicEffectType.PortraitEyeOpeningFear, 1)] // 초상화 속 붉은 눈 원본 행과 가격 범위
    [TestCase("LordsFinalOrder", 188, 326, 362, 166, 205, RelicEffectType.LordsFinalOrderAttackThenDefenseLoss, 3)] // 영주의 마지막 명령서 원본 행과 가격 범위
    [TestCase("BrokenCrownOfMirrorDuke", 189, 193, 236, 99, 143, RelicEffectType.MirrorDukeCrownDebuffReflection, 1)] // 거울 공작의 깨진 왕관 원본 행과 가격 범위
    [TestCase("BanquetHeartRoot", 190, 210, 246, 109, 140, RelicEffectType.BanquetHeartPoisonCountHealing, 2)] // 만찬 심장의 잔뿌리 원본 행과 가격 범위
    [TestCase("BlackChandelierCore", 191, 227, 270, 119, 155, RelicEffectType.ChandelierThirdTurnPulse, 10)] // 검은 샹들리에 핵 원본 행과 가격 범위
    [TestCase("HiddenNameOfConfessional", 192, 244, 280, 124, 165, RelicEffectType.HiddenNamePartyDefeatWard, 1)] // 고해실의 숨은 이름 원본 행과 가격 범위
    [TestCase("LastBeatOfBallroom", 193, 261, 304, 134, 180, RelicEffectType.BallroomLastBeatSecondEvade, 12)] // 무도회의 마지막 박자 원본 행과 가격 범위
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
