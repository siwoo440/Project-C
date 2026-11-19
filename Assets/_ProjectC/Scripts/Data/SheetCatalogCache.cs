using System; // 이름 비교와 원본 이름 선택 함수 사용
using System.Collections.Generic; // 이름 색인과 읽기 전용 행 사용
using UnityEngine; // 실제 시트 리소스와 JSON 사용

internal static class SheetCatalogCache // 카드와 유물 시트의 공통 리소스 로딩
{ // 원본 로더 시작
    internal static T Load<T>(string path, ref T cached) where T : class // 성공한 시트 자료만 실행 단위로 재사용
    { // 실행 중 원본 조회 시작
        if (cached == null) // 첫 요청 또는 앞선 로딩 실패 확인
        { // 원본 로딩 시작
            var source = Resources.Load<TextAsset>(path); // 해당 시트 원본 조회
            if (source != null) // 실제 자료 존재 확인
            { // 원본 파싱 시작
                cached = JsonUtility.FromJson<T>(source.text); // 정상 파싱이 끝난 자료만 보관
            } // 원본 파싱 종료
        } // 원본 로딩 종료
        return cached; // 준비된 시트 또는 누락 결과 반환
    } // 실행 중 원본 조회 종료
} // 원본 로더 종료

internal sealed class SheetNameIndex<T> where T : class // 첫 번째 원본 행을 보관하는 이름 색인
{ // 이름 색인 시작
    private readonly Dictionary<string, T> firstByName; // 대소문자를 구별하는 이름별 첫 행
    private readonly T firstUnnamed; // 이름이 null인 첫 행
    internal IReadOnlyList<T> Rows { get; } // 원본 순서를 유지하는 읽기 전용 행

    internal SheetNameIndex(List<T> entries, Func<T, string> getName) // 원본 순서대로 첫 행 색인 생성
    { // 색인 생성 시작
        Rows = entries?.AsReadOnly(); // 공유 시트의 원본 행 순서와 내용 보호
        firstByName = new Dictionary<string, T>(entries?.Count ?? 0, StringComparer.Ordinal); // 원본 수에 맞춘 완전 일치 색인 준비
        if (entries == null) // 직렬화된 행 목록 누락 확인
        { // 누락된 목록 처리 시작
            return; // 비어 있는 색인 유지
        } // 누락된 목록 처리 종료
        foreach (var entry in entries) // 시트 원본 순서로 한 번 순회
        { // 원본 행 검사 시작
            if (entry == null) // 누락된 행 확인
            { // 빈 행 제외 시작
                continue; // 다음 원본 행 확인
            } // 빈 행 제외 종료
            string name = getName(entry); // 공백과 대소문자를 보존한 이름 조회
            if (name == null) // 이름 없는 원본 행 확인
            { // 이름 없는 행 기록 시작
                firstUnnamed ??= entry; // 기존 순회와 같은 첫 번째 미지정 이름 보존
            } // 이름 없는 행 기록 종료
            else // 빈 문자열을 포함한 실제 이름 처리
            { // 이름 색인 기록 시작
                firstByName.TryAdd(name, entry); // 중복 이름에서는 먼저 나온 행 보존
            } // 이름 색인 기록 종료
        } // 원본 행 검사 종료
    } // 색인 생성 종료

    internal T Find(string name) // 정확한 이름의 첫 행 조회
    { // 이름 검색 시작
        return name == null ? firstUnnamed : firstByName.GetValueOrDefault(name); // 첫 일치 행 또는 누락 결과 반환
    } // 이름 검색 종료
} // 이름 색인 종료
