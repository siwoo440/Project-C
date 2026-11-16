using System; // 직렬화와 문자열 비교 사용
using System.Collections.Generic; // 카드 목록 사용
using UnityEngine; // 리소스와 JSON 사용

[Serializable] // 시트 항목 직렬화
public sealed class CardSheetEntry // 카드 시트 원본 항목
{ // 항목 시작
    [SerializeField] private int sourceRow; // 원본 행 번호
    [SerializeField] private string name; // 카드 이름
    [SerializeField] private string attribute; // 카드 속성
    [SerializeField] private string rarity; // 카드 희귀도
    [SerializeField] private string cost; // 비용 원문
    [SerializeField] private string description; // 효과 설명
    [SerializeField] private string area; // 적용 범위 원문
    [SerializeField] private string targets; // 적용 대상 원문
    [SerializeField] private string imageFileName; // 원본 이미지 파일명
    public int SourceRow => sourceRow; // 원본 행 번호 조회
    public string Name => name; // 카드 이름 조회
    public string Attribute => attribute; // 카드 속성 조회
    public string Rarity => rarity; // 카드 희귀도 조회
    public string Cost => cost; // 카드 비용 원문 조회
    public string Description => description; // 카드 설명 조회
    public string Area => area; // 적용 범위 원문 조회
    public string Targets => targets; // 적용 대상 원문 조회
    public string ImageFileName => imageFileName; // 원본 이미지 파일명 조회
    public bool HasCompleteSheetValues => !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(attribute) && !string.IsNullOrWhiteSpace(rarity) && !string.IsNullOrWhiteSpace(cost) && !string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(area) && !string.IsNullOrWhiteSpace(targets); // 필수 시트 값 존재 확인
} // 항목 종료

[Serializable] // 카드 목록 직렬화
public sealed class CardSheetCatalog // 카드 시트 원본 목록
{ // 목록 시작
    [SerializeField] private List<CardSheetEntry> entries = new List<CardSheetEntry>(); // 카드 시트 항목 목록
    public IReadOnlyList<CardSheetEntry> Entries => entries; // 읽기 전용 항목 조회

    public static CardSheetCatalog Load() // 카드 시트 목록 불러오기
    { // 불러오기 시작
        TextAsset source = Resources.Load<TextAsset>("CardSheetCatalog"); // 카드 시트 리소스 조회
        return source == null ? null : JsonUtility.FromJson<CardSheetCatalog>(source.text); // 자료 변환 결과 반환
    } // 불러오기 종료

    public CardSheetEntry FindByName(string cardName) // 이름으로 카드 조회
    { // 검색 시작
        foreach (CardSheetEntry entry in entries) // 카드 항목 순회
        { // 항목 검색 시작
            if (entry != null && string.Equals(entry.Name, cardName, StringComparison.Ordinal)) // 정확한 이름 확인
            { // 일치 처리 시작
                return entry; // 일치 항목 반환
            } // 일치 처리 종료
        } // 항목 검색 종료
        return null; // 일치 항목 없음 반환
    } // 검색 종료
} // 목록 종료
