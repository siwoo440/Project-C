using System; // 직렬화 속성 사용
using System.Collections.Generic; // 목록 사용
using UnityEngine; // 리소스와 JSON 사용

[Serializable] // 시트 항목 직렬화
public sealed class RelicSheetEntry // 유물 시트 원본 항목
{ // 항목 시작
    [SerializeField] private int sourceRow; // 원본 행 번호
    [SerializeField] private string name; // 유물 이름
    [SerializeField] private string description; // 유물 설명
    [SerializeField] private int buyMin; // 구매 최솟값
    [SerializeField] private int buyMax; // 구매 최댓값
    [SerializeField] private int buyVariance; // 구매 가격 편차
    [SerializeField] private int sellMin; // 판매 최솟값
    [SerializeField] private int sellMax; // 판매 최댓값
    [SerializeField] private int sellVariance; // 판매 가격 편차
    public int SourceRow => sourceRow; // 원본 행 번호 조회
    public string Name => name; // 유물 이름 조회
    public string Description => description; // 유물 설명 조회
    public int BuyMin => buyMin; // 구매 최솟값 조회
    public int BuyMax => buyMax; // 구매 최댓값 조회
    public int BuyVariance => buyVariance; // 구매 가격 편차 조회
    public int SellMin => sellMin; // 판매 최솟값 조회
    public int SellMax => sellMax; // 판매 최댓값 조회
    public int SellVariance => sellVariance; // 판매 가격 편차 조회
} // 항목 종료

[Serializable] // 유물 목록 직렬화
public sealed class RelicSheetCatalog // 유물 시트 목록
{ // 목록 시작
    [SerializeField] private List<RelicSheetEntry> entries = new List<RelicSheetEntry>(); // 원본 항목 목록
    public IReadOnlyList<RelicSheetEntry> Entries => entries; // 읽기 전용 항목 조회

    public static RelicSheetCatalog Load() // 리소스 목록 불러오기
    { // 불러오기 시작
        TextAsset source = Resources.Load<TextAsset>("Relics/RelicSheetCatalog"); // 시트 자료 조회
        return source == null ? null : JsonUtility.FromJson<RelicSheetCatalog>(source.text); // 자료 변환 결과 반환
    } // 불러오기 종료

    public RelicSheetEntry FindByName(string relicName) // 이름으로 유물 조회
    { // 검색 시작
        foreach (RelicSheetEntry entry in entries) // 항목 순회
        { // 항목 검색 시작
            if (entry != null && string.Equals(entry.Name, relicName, StringComparison.Ordinal)) // 정확한 이름 확인
            { // 일치 처리 시작
                return entry; // 일치 항목 반환
            } // 일치 처리 종료
        } // 항목 검색 종료
        return null; // 일치 항목 없음 반환
    } // 검색 종료
} // 목록 종료
