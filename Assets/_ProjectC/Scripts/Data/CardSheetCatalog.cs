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
public sealed class CardSheetCatalog : ISerializationCallbackReceiver // 카드 시트 원본 목록
{ // 목록 시작
    [SerializeField] private List<CardSheetEntry> entries = new List<CardSheetEntry>(); // 원본 순서의 카드 시트 항목
    private static CardSheetCatalog cached; // 현재 플레이 실행에서 읽은 카드 시트
    [NonSerialized] private SheetNameIndex<CardSheetEntry> names; // 현재 원본 행의 이름 색인
    private SheetNameIndex<CardSheetEntry> Names => names ??= new SheetNameIndex<CardSheetEntry>(entries, entry => entry.Name); // 현재 목록의 첫 조회에서만 색인 구성
    public IReadOnlyList<CardSheetEntry> Entries => Names.Rows; // 읽기 전용 원본 행 조회

    public static CardSheetCatalog Load() // 카드 시트 목록 불러오기
    { // 불러오기 시작
        return SheetCatalogCache.Load("CardSheetCatalog", ref cached); // 성공한 카드 시트 재사용
    } // 불러오기 종료

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 유지 설정에서도 실행 시작마다 초기화
    private static void ResetCache() // 다음 실행의 카드 시트 새로 읽기
    { // 실행 초기화 시작
        cached = null; // 이전 실행의 원본과 색인 해제
    } // 실행 초기화 종료

    public CardSheetEntry FindByName(string cardName) // 이름으로 카드 조회
    { // 검색 시작
        return Names.Find(cardName); // 대소문자와 원래 순서가 일치하는 첫 행 반환
    } // 검색 종료

    void ISerializationCallbackReceiver.OnBeforeSerialize() // 원본 행만 직렬화
    { // 직렬화 전 처리 시작
    } // 직렬화 전 처리 종료

    void ISerializationCallbackReceiver.OnAfterDeserialize() // 동일 객체의 JSON 덮어쓰기 반영
    { // 시트 교체 처리 시작
        names = null; // 교체 전 행의 색인과 읽기 전용 목록 해제
    } // 시트 교체 처리 종료
} // 목록 종료
