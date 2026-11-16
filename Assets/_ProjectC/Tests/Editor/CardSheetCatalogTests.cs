using System.Linq; // 카드 목록 검색 사용
using NUnit.Framework; // 시트 자료 검사 사용

public sealed class CardSheetCatalogTests // 카드 시트 자료 검사
{ // 검사 시작
    [Test] // 실제 카드 목록 검사
    public void CatalogPreservesAllCardsAndIncompleteRows() // 카드 정보와 미완성 행 확인
    { // 검사 본문 시작
        CardSheetCatalog catalog = CardSheetCatalog.Load(); // 실제 카드 시트 자료 조회
        Assert.IsNotNull(catalog); // 카드 목록 존재 확인
        Assert.AreEqual(251, catalog.Entries.Count); // 시트 카드 수 확인
        Assert.AreEqual(251, catalog.Entries.Select(entry => entry.Name).Distinct().Count()); // 카드 이름 중복 확인
        CardSheetEntry strike = catalog.FindByName("강타"); // 실제 카드 정보 조회
        Assert.IsTrue(strike.HasCompleteSheetValues); // 완성된 시트 항목 확인
        Assert.AreEqual("검", strike.Attribute); // 카드 속성 확인
        Assert.AreEqual("1", strike.Cost); // 카드 비용 확인
        Assert.AreEqual("적에게 물리 피해 8", strike.Description); // 카드 설명 확인
        Assert.AreEqual("강타.png", strike.ImageFileName); // 원본 이미지 이름 확인
        Assert.AreEqual("X", catalog.FindByName("최후의 일격").Cost); // 가변 비용 원문 보존 확인
        Assert.IsFalse(catalog.FindByName("막기").HasCompleteSheetValues); // 비어 있는 방패 카드 구분
        Assert.IsFalse(catalog.FindByName("흘려내기").HasCompleteSheetValues); // 두 번째 미완성 카드 구분
        Assert.IsFalse(catalog.FindByName("밀어 막기").HasCompleteSheetValues); // 세 번째 미완성 카드 구분
        Assert.AreEqual(3, catalog.Entries.Count(entry => !entry.HasCompleteSheetValues)); // 미완성 시트 행 수 확인
        Assert.IsNull(catalog.FindByName("없는 카드")); // 존재하지 않는 카드 검색 확인
    } // 검사 본문 종료
} // 검사 종료
