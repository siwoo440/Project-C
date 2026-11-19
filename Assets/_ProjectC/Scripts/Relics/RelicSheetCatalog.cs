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
public sealed class RelicSheetCatalog : ISerializationCallbackReceiver // 유물 시트 원본 목록
{ // 목록 시작
    [SerializeField] private List<RelicSheetEntry> entries = new List<RelicSheetEntry>(); // 원본 순서의 유물 시트 항목
    private static RelicSheetCatalog cached; // 현재 플레이 실행에서 읽은 유물 시트
    [NonSerialized] private SheetNameIndex<RelicSheetEntry> names; // 현재 원본 행의 이름 색인
    private SheetNameIndex<RelicSheetEntry> Names => names ??= new SheetNameIndex<RelicSheetEntry>(entries, entry => entry.Name); // 현재 목록의 첫 조회에서만 색인 구성
    public IReadOnlyList<RelicSheetEntry> Entries => Names.Rows; // 읽기 전용 원본 행 조회

    public static RelicSheetCatalog Load() // 유물 시트 목록 불러오기
    { // 불러오기 시작
        return SheetCatalogCache.Load("Relics/RelicSheetCatalog", ref cached); // 성공한 유물 시트 재사용
    } // 불러오기 종료

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 유지 설정에서도 실행 시작마다 초기화
    private static void ResetCache() // 다음 실행의 유물 시트 새로 읽기
    { // 실행 초기화 시작
        cached = null; // 이전 실행의 원본과 색인 해제
    } // 실행 초기화 종료

    public RelicSheetEntry FindByName(string relicName) // 이름으로 유물 조회
    { // 검색 시작
        return Names.Find(relicName); // 대소문자와 원래 순서가 일치하는 첫 행 반환
    } // 검색 종료

    void ISerializationCallbackReceiver.OnBeforeSerialize() // 원본 행만 직렬화
    { // 직렬화 전 처리 시작
    } // 직렬화 전 처리 종료

    void ISerializationCallbackReceiver.OnAfterDeserialize() // 동일 객체의 JSON 덮어쓰기 반영
    { // 시트 교체 처리 시작
        names = null; // 교체 전 행의 색인과 읽기 전용 목록 해제
    } // 시트 교체 처리 종료
} // 목록 종료
