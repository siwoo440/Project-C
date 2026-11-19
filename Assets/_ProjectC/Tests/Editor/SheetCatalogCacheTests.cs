using System; // 순서 비교와 측정 함수 사용
using System.Collections.Generic; // 읽기 전용 원본 행 사용
using System.Diagnostics; // 조회 경과 시간 측정
using System.Linq; // 시트 원본의 기대 결과 확인
using System.Reflection; // 실행 초기화와 실패 재시도 검사
using NUnit.Framework; // 카탈로그 회귀 검사
using UnityEngine; // 실제 시트와 JSON 읽기
using Debug = UnityEngine.Debug; // 에디터 측정 로그 기록

public sealed class SheetCatalogCacheTests // 카드와 유물 시트 재사용 및 검색 검사
{ // 검사 시작
    [Test] // 실제 시트 반복 로딩과 재시작 검사
    public void PublicLoadReusesEachSheetUntilNextPlaySession() // 같은 실행의 자료 재사용과 새 실행의 원본 재조회
    { // 실행 범위 검사 시작
        VerifyLifetime(CardSheetCatalog.Load, catalog => catalog.FindByName("강타")); // 실제 카드 조회의 재사용 확인
        VerifyLifetime(RelicSheetCatalog.Load, catalog => catalog.FindByName("잿빛 인장")); // 실제 유물 조회의 재사용 확인
    } // 실행 범위 검사 종료

    [Test] // 중복과 공백 및 대소문자 검색 검사
    public void FirstMatchingRowAndOrdinalNamesSurviveDuplicatesAndJsonOverwrite() // 원본 순서와 재직렬화 후 색인 교체 확인
    { // 이름 검색 검사 시작
        VerifyNames<CardSheetCatalog, CardSheetEntry>(catalog => catalog.Entries, (catalog, name) => catalog.FindByName(name), entry => entry.Name, entry => entry.SourceRow); // 카드 원본 첫 행 검색 확인
        VerifyNames<RelicSheetCatalog, RelicSheetEntry>(catalog => catalog.Entries, (catalog, name) => catalog.FindByName(name), entry => entry.Name, entry => entry.SourceRow); // 유물 원본 첫 행 검색 확인
    } // 이름 검색 검사 종료

    [Test] // 누락·손상 뒤 정상 로드와 성공 보관 검사
    public void MissingAndMalformedResourcesAreRetriedUntilAValidSheetLoads() // 기존 오류 처리와 다음 정상 자료 로딩 확인
    { // 실패 복구 검사 시작
        VerifyFailedLoads<CardSheetCatalog>("CardSheetCatalog"); // 실제 카드 시트로 재시도 확인
        VerifyFailedLoads<RelicSheetCatalog>("Relics/RelicSheetCatalog"); // 실제 유물 시트로 재시도 확인
    } // 실패 복구 검사 종료

    [Test] // 실제 원본 전체 이름 조회와 반복 작업 계측
    public void AllPublishedRowsMatchOriginalSearchAndReportRepeatedLookupTimes() // 원본 행 대조와 동일 입력의 검색 시간 측정
    { // 전체 행과 측정 검사 시작
        var cards = CardSheetCatalog.Load(); // 실제 카드 목록 준비
        Measure("cards", "CardSheetCatalog", CardSheetCatalog.Load, cards.Entries, row => row.Name, cards.FindByName); // 모든 카드 이름 결과와 반복 시간 기록
        var relics = RelicSheetCatalog.Load(); // 실제 유물 목록 준비
        Measure("relics", "Relics/RelicSheetCatalog", RelicSheetCatalog.Load, relics.Entries, row => row.Name, relics.FindByName); // 모든 유물 이름 결과와 반복 시간 기록
    } // 전체 행과 측정 검사 종료

    private static void VerifyLifetime<TCatalog, TEntry>(Func<TCatalog> load, Func<TCatalog, TEntry> find) where TCatalog : class where TEntry : class // 정상 로딩 함수의 실행 단위 확인
    { // 실행 단위 확인 시작
        var first = load(); // 첫 실행의 목록 확보
        var firstRow = find(first); // 첫 실행의 실제 행 확보
        Assert.IsNotNull(firstRow); // 실제 원본 행 존재 확인
        Assert.AreSame(first, load()); // 같은 실행의 JSON 재파싱 방지 확인
        Assert.AreSame(firstRow, find(load())); // 같은 실행의 행과 색인 재사용 확인
        var reset = typeof(TCatalog).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(method => method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>()?.loadType == RuntimeInitializeLoadType.SubsystemRegistration); // 도메인 재로드 설정과 무관한 실제 시작 훅 검색
        reset.Invoke(null, null); // 새 플레이 세션 시작 훅 실행
        var next = load(); // 새 플레이 세션의 첫 목록 확보
        Assert.AreNotSame(first, next); // 이전 실행에서 파싱한 목록 해제 확인
        Assert.AreNotSame(firstRow, find(next)); // 새 목록의 데이터로 색인 재구성 확인
        Assert.AreSame(next, load()); // 새 실행에서도 한 번만 로드 확인
    } // 실행 단위 확인 종료

    private static void VerifyNames<TCatalog, TEntry>(Func<TCatalog, IReadOnlyList<TEntry>> rows, Func<TCatalog, string, TEntry> find, Func<TEntry, string> name, Func<TEntry, int> number) where TCatalog : class where TEntry : class, new() // 합성 시트의 이름과 행 검증
    { // 원본 순서 검사 시작
        const string fixture = "{\"entries\":[{\"name\":\"A\",\"sourceRow\":4},{\"name\":\"A\",\"sourceRow\":2},{\"name\":\"a\",\"sourceRow\":7},{\"name\":\" A\",\"sourceRow\":9},{\"name\":\"\",\"sourceRow\":10}]}"; // 중복·대소문자·공백이 있는 고정 시트
        var catalog = JsonUtility.FromJson<TCatalog>(fixture); // 실제 직렬화 방식으로 합성 시트 생성
        var raw = (List<TEntry>)typeof(TCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog); // 색인 생성 전 원본 행 조회
        raw.Add(null); // 누락된 원본 행 재현
        raw.Add(new TEntry()); // 이름이 null인 원본 행 재현
        var data = rows(catalog); // 공개된 원본 순서 조회
        Assert.AreEqual(7, data.Count); // 중복과 빈 이름 및 누락 행을 포함한 원본 길이 확인
        CollectionAssert.AreEqual(new[] { 4, 2, 7, 9, 10 }, data.Take(5).Select(number)); // 이름 정렬 없이 원본 행 순서 확인
        foreach (string key in new string[] { "A", "a", " A", "", null, "A ", "없는 이름" }) // 기존 완전 일치 검색 조건 순회
        { // 이름별 검사 시작
            var first = data.FirstOrDefault(row => row != null && string.Equals(name(row), key, StringComparison.Ordinal)); // 기존 검색과 같은 첫 일치 행 선택
            Assert.AreSame(first, find(catalog, key), key); // 중복과 빈 이름 및 대소문자 조회 호환 확인
        } // 이름별 검사 종료
        JsonUtility.FromJsonOverwrite("{\"entries\":[{\"name\":\"교체\",\"sourceRow\":22}]}", catalog); // 같은 인스턴스에 새 JSON 자료 적용
        Assert.IsNull(find(catalog, "A")); // 교체 전 이름 색인 제거 확인
        Assert.AreEqual(22, number(find(catalog, "교체"))); // 교체 후 이름과 원본 행 조회 확인
        Assert.AreEqual(1, rows(catalog).Count); // 교체 후 공개 목록 길이 확인
    } // 원본 순서 검사 종료

    private static void VerifyFailedLoads<TCatalog>(string path) where TCatalog : class // 실제 리소스 누락과 JSON 실패 검사
    { // 실패 재시도 검사 시작
        var cache = typeof(TCatalog).Assembly.GetType("SheetCatalogCache", true); // 두 원본이 공유하는 로더 조회
        var load = cache.GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeof(TCatalog)); // 실제 자료형의 리소스 로더 선택
        object[] args = { "Day222/MissingSheet", null }; // 누락 리소스와 독립 캐시 준비
        Assert.IsNull(load.Invoke(null, args)); // 없는 리소스의 기존 null 결과 유지
        Assert.IsNull(args[1]); // 자료 누락을 성공으로 보관하지 않는지 확인
        args[0] = "Fonts/NotoSansCJK-OFL"; // JSON이 아닌 실제 글꼴 라이선스 텍스트 지정
        var error = Assert.Throws<TargetInvocationException>(() => load.Invoke(null, args)); // 기존 JSON 파서 오류 유지 확인
        Assert.IsInstanceOf<ArgumentException>(error.InnerException); // 실제 JSON 형식 오류 확인
        Assert.IsNull(args[1]); // 잘못된 자료로 성공 캐시 오염 방지 확인
        args[0] = path; // 정상 카드 또는 유물 원본으로 재시도
        var valid = load.Invoke(null, args); // 실제 정상 JSON 복원
        Assert.IsNotNull(valid); // 실패 뒤 재시도 성공 확인
        args[0] = "Day222/MissingSheet"; // 재로딩 시 실패할 경로로 교체
        Assert.AreSame(valid, load.Invoke(null, args)); // 성공 후 리소스 재조회 생략 확인
    } // 실패 재시도 검사 종료

    private static void Measure<TCatalog, TEntry>(string label, string path, Func<TCatalog> load, IReadOnlyList<TEntry> rows, Func<TEntry, string> name, Func<string, TEntry> find) where TCatalog : class where TEntry : class // 같은 실제 이름으로 이전과 현재 조회 측정
    { // 반복 조회 측정 시작
        const int passes = 64; // 전체 시트 반복 조회 횟수
        var data = rows.ToArray(); // 동일 순서로 순회할 원본 행 준비
        var keys = data.Select(name).ToArray(); // 실제 시트 이름을 한 번 추출
        var expected = keys.Select(key => Array.FindIndex(data, row => string.Equals(name(row), key, StringComparison.Ordinal))).Select(index => data[index]).ToArray(); // 기존 첫 일치 행의 기준 목록 생성
        CollectionAssert.AreEqual(expected, keys.Select(find)); // 전체 실제 이름에 대한 결과 일치 확인
        var timer = Stopwatch.StartNew(); // 기존 반복 리소스 로딩과 파싱 측정 시작
        for (int pass = 0; pass < passes; pass++) // 기존 로딩 횟수 재현
        { // 원본 재파싱 시작
            JsonUtility.FromJson<TCatalog>(Resources.Load<TextAsset>(path).text); // 기존 Load 경로와 같은 리소스 재조회와 JSON 파싱
        } // 원본 재파싱 종료
        double parseMs = timer.Elapsed.TotalMilliseconds; // 반복 로딩 기준 시간 기록
        timer.Restart(); // 캐시 적용 로딩 측정 시작
        for (int pass = 0; pass < passes; pass++) // 같은 횟수로 실제 로더 호출
        { // 현재 로딩 측정 시작
            load(); // 실제 성공 캐시 경로 실행
        } // 현재 로딩 측정 종료
        double loadMs = timer.Elapsed.TotalMilliseconds; // 캐시 적용 로딩 시간 기록
        timer.Restart(); // 기존 첫 일치 선형 검색 시간 측정
        int linearHits = 0; // 기존 검색 적중 횟수
        for (int pass = 0; pass < passes; pass++) // 원본 이름 목록 반복
        { // 기존 이름 검색 시작
            foreach (string key in keys) // 같은 순서로 실제 이름 조회
            { // 한 이름 검사 시작
                foreach (var row in data) // 원본 행 앞에서부터 조회
                { // 원본 행 검사 시작
                    if (string.Equals(name(row), key, StringComparison.Ordinal)) // 기존 검색과 같은 첫 완전 일치 확인
                    { // 일치 이름 처리 시작
                        linearHits++; // 기존 조회 성공 건수 누적
                        break; // 기존 동작에 맞춰 첫 행에서 종료
                    } // 일치 이름 처리 종료
                } // 원본 행 검사 종료
            } // 한 이름 검사 종료
        } // 기존 이름 검색 종료
        double linearMs = timer.Elapsed.TotalMilliseconds; // 선형 검색 기준 시간 기록
        timer.Restart(); // 같은 이름의 색인 조회 시간 측정
        int indexedHits = 0; // 색인 검색 적중 횟수
        for (int pass = 0; pass < passes; pass++) // 같은 횟수로 색인 조회
        { // 색인 이름 검색 시작
            foreach (string key in keys) // 같은 실제 이름 순회
            { // 색인 행 검색 시작
                indexedHits += find(key) != null ? 1 : 0; // 실제 색인 적중 횟수 누적
            } // 색인 행 검색 종료
        } // 색인 이름 검색 종료
        timer.Stop(); // 색인 검색 시간 측정 종료
        Assert.AreEqual(keys.Length * passes, indexedHits); // 전체 이름 반복 검색 성공 확인
        Assert.AreEqual(linearHits, indexedHits); // 같은 입력에 대한 검색 수 일치 확인
        Debug.Log($"[Day222Catalog] kind={label} rows={keys.Length} passes={passes} hits={indexedHits} reparseMs={parseMs:F3} cachedLoadMs={loadMs:F3} linearMs={linearMs:F3} indexedMs={timer.Elapsed.TotalMilliseconds:F3}"); // 같은 에디터 실행에서 관찰한 시간과 검색 수 기록
    } // 반복 조회 측정 종료
} // 검사 종료
