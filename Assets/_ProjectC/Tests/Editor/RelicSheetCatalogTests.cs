using System.Linq; // 목록 검색 사용
using NUnit.Framework; // 검사 도구 사용
using UnityEngine; // 리소스 사용

public sealed class RelicSheetCatalogTests // 유물 시트 목록 검사
{ // 검사 시작
    [Test] // 유물 목록 검사
    public void CatalogContainsEverySheetRelicWithConsistentPrices() // 유물과 가격 확인
    { // 검사 본문 시작
        RelicSheetCatalog catalog = RelicSheetCatalog.Load(); // 실제 목록 불러오기
        Assert.That(catalog, Is.Not.Null); // 목록 존재 확인
        Assert.That(catalog.Entries.Count, Is.EqualTo(298)); // 유물 수 확인
        Assert.That(catalog.Entries.Select(entry => entry.Name).Distinct().Count(), Is.EqualTo(298)); // 이름 중복 확인
        foreach (RelicSheetEntry entry in catalog.Entries) // 모든 유물 순회
        { // 유물 검사 시작
            Assert.That(entry.Name, Is.Not.Empty); // 이름 확인
            Assert.That(entry.Description, Is.Not.Empty); // 설명 확인
            Assert.That(entry.BuyMax - entry.BuyMin, Is.EqualTo(entry.BuyVariance)); // 구매 편차 확인
            Assert.That(entry.SellMax - entry.SellMin, Is.EqualTo(entry.SellVariance)); // 판매 편차 확인
        } // 유물 검사 종료
        RelicSheetEntry repaired = catalog.FindByName("검은 손수건"); // 수정한 유물 조회
        Assert.That(repaired.SellMin, Is.EqualTo(130)); // 임시 판매 최솟값 확인
        Assert.That(repaired.SellMax, Is.EqualTo(170)); // 임시 판매 최댓값 확인
        Assert.That(catalog.FindByName("없는 유물"), Is.Null); // 존재하지 않는 이름 확인
    } // 검사 본문 종료
} // 검사 종료
