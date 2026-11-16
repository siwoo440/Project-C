using System.Collections.Generic; // 가격 집합 사용
using NUnit.Framework; // 가격 검사 사용

public sealed class RelicShopPriceTests // 유물 상점 가격 검사
{ // 가격 검사 시작
    [Test] // 시트 범위 가격 검사
    public void ShopPriceIsInRangeAndStableForTheSameRoom() // 상점별 가격 재현 확인
    { // 가격 검사 본문 시작
        RelicSheetEntry entry = RelicSheetCatalog.Load().FindByName("봉쇄문 인증키"); // 실제 시트 유물 조회
        HashSet<int> prices = new HashSet<int>(); // 여러 상점의 가격 집합 생성
        for (int index = 0; index < 100; index++) // 여러 상점 순회
        { // 상점별 검사 시작
            string room = "floor_seed_room_" + index; // 고유 상점 방 식별자 생성
            int price = RelicShopPricing.Calculate(entry, room, "SHOP_RLC_GATE_KEY"); // 상점 유물 가격 계산
            Assert.That(price, Is.InRange(228, 264)); // 시트 구매 범위 확인
            Assert.That(RelicShopPricing.Calculate(entry, room, "SHOP_RLC_GATE_KEY"), Is.EqualTo(price)); // 동일 상점 재현 확인
            prices.Add(price); // 생성된 가격 기록
        } // 상점별 검사 종료
        Assert.That(prices.Count, Is.GreaterThan(1)); // 서로 다른 상점의 가격 변화 확인
    } // 가격 검사 본문 종료
} // 가격 검사 종료
