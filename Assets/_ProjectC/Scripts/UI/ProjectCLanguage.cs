using System; // 한영 문자열 비교와 로컬 자료 검색
using System.Collections.Generic; // 번역 캐시와 서식 규칙 목록
using System.Text; // 변수 붙은 실제 게임 문구 조합
using System.Text.RegularExpressions; // 태그와 동적 자리표시자 처리
using UnityEngine; // 내장 번역표의 리소스 불러오기

public static class ProjectCLanguage // 한국어 원문을 보존하는 실제 표시 언어
{ // 표시 언어 처리 시작
    private const int CacheLimit = 2048; // 전투 수치가 바뀔 때도 일정한 번역 캐시 크기
    private static readonly Regex Slots = new Regex("\\{(\\d+)\\}"); // 번역의 번호가 붙은 동적 자리표시자
    private static readonly Regex Tags = new Regex("(<[^<>]+>)"); // 이미 적용된 글자 색과 효과의 UI 태그
    private static readonly Regex Separators = new Regex("(\\r?\\n| · | \\| )"); // 이름과 수치를 합친 화면 구분 기호
    private static readonly Dictionary<string, string> exact = new Dictionary<string, string>(StringComparer.Ordinal); // 원래 문구로 찾는 영어 번역
    private static readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.Ordinal); // 현재 적용한 동적 문구의 반복 번역 방지
    private static readonly List<Template> templates = new List<Template>(); // 순간 수치와 이름이 붙는 문구의 원래 형식
    private static bool loaded; // 현재 실행에서 번역표를 한 번 불러왔는지 여부

    public static bool UseEnglish { get; private set; } // 현재 기기의 영어 표시 설정

    public static void Apply(bool useEnglish) // 설정에서 실제 표시 언어 적용
    { // 선호 언어 적용 시작
        if (UseEnglish == useEnglish) // 같은 언어의 불필요한 화면 갱신 확인
        { // 적용 대상 확인 시작
            return; // 이미 적용된 경우 종료
        } // 적용 대상 확인 종료
        UseEnglish = useEnglish; // 현재 표시할 쪽의 언어 저장
        ProjectCLanguageDisplay.RefreshCurrent(); // 열려 있는 화면의 표시 문자열 즉시 갱신
    } // 선호 언어 적용 종료

    public static string Translate(string source) // 플레이어에게 보이는 문자열의 현재 언어 선택
    { // 실제 표시 문구 변환 시작
        if (!UseEnglish || !ContainsKorean(source)) // 번역 대상의 현재 언어와 한글 확인
        { // 원래 표시 경로 시작
            return source; // 숫자와 원본 한국어의 그대로 표시
        } // 원래 표시 경로 종료
        LoadTable(); // 플레이 중 한 번만 읽는 로컬 영어 원문 표
        if (cache.TryGetValue(source, out string english)) // 같은 값으로 앞에서 번역한 동적 문구 확인
        { // 기존 표시 언어 사용 시작
            return english; // 똑같은 UI 갱신에 캐시된 번역 사용
        } // 기존 표시 언어 사용 종료
        english = TranslateText(source, 0); // 수치와 이름까지 보존한 실제 문구 변환
        if (cache.Count >= CacheLimit) // 오랜 전투의 숫자별 번역 캐시 상한 확인
        { // 오래된 문자열 재사용 정리 시작
            cache.Clear(); // 현재 장면에서 사용한 과거 수치 자료 정리
        } // 오래된 문자열 재사용 정리 종료
        cache[source] = english; // 같은 문구를 반복하는 UI의 이전 결과 저장
        return english; // 게임 화면이 현재 표시할 영어 또는 원문 반환
    } // 실제 표시 문구 변환 종료

    internal static bool ContainsKorean(string source) // 렌더링에 한글 원문이 있는지 확인
    { // 표시 문자 검사 시작
        if (string.IsNullOrEmpty(source)) // 빈 표시값 확인
        { // 빈 원문 처리 시작
            return false; // 번역할 문자가 없는 결과
        } // 빈 원문 처리 종료
        foreach (char value in source) // UI 코드와 숫자를 제외할 실제 한글 찾기
        { // 문자열의 각 문자 검사 시작
            if (value is >= '\uac00' and <= '\ud7af' or >= '\u1100' and <= '\u11ff' or >= '\u3130' and <= '\u318f') // 현대 한글 완성형과 자모 범위
            { // 실제 한글 발견 시작
                return true; // 번역이 필요한 문자 존재 확인
            } // 실제 한글 발견 종료
        } // 문자열의 각 문자 검사 종료
        return false; // 현재 표시에서 한글 없음 확인
    } // 표시 문자 검사 종료

    private static void LoadTable() // 게임에 포함된 UTF8 한영 대응표 사용
    { // 영어 번역 리소스 확인 시작
        if (loaded) // 이미 불러온 번역자료 사용 여부 확인
        { // 기존 테이블 경로 시작
            return; // 파일을 반복해서 읽지 않는 경로
        } // 기존 테이블 경로 종료
        loaded = true; // 현재 인스턴스의 한 번만 거치는 읽기 표시
        TextAsset asset = Resources.Load<TextAsset>("Localization/English"); // 실제 게임에 포함된 영어 표시 표
        if (asset == null) // 리소스 파일이 없는 설정의 원문 보존 확인
        { // 원문으로 표시하는 복구 시작
            return; // 번역표 없이도 게임 진행 유지
        } // 원문으로 표시하는 복구 종료
        var table = JsonUtility.FromJson<TranslationTable>(asset.text); // 실제 프로젝트 한국어와 영어 쌍 읽기
        foreach (var entry in table.entries) // 고정 원문 또는 수치로 바뀌는 원문 분기
        { // 영어 표시 규칙 등록 시작
            if (entry.template) // 자리표시자가 있는 게임 문구 확인
            { // 동적 문구 규칙 등록 시작
                templates.Add(new Template(entry.source, entry.english)); // 한영 자리표시자 위치를 보존한 원문 서식
            } // 동적 문구 규칙 등록 종료
            else // 고정된 게임 가이드와 데이터 이름
            { // 원문 고정 번역 등록 시작
                exact[entry.source] = entry.english; // 기존 데이터 ID와 다르게 화면 원문만 사용
            } // 원문 고정 번역 등록 종료
        } // 영어 표시 규칙 등록 종료
        templates.Sort((left, right) => right.FixedLength.CompareTo(left.FixedLength)); // 구체적인 동적 안내가 짧은 조각보다 먼저 맞도록 정렬
    } // 영어 번역 리소스 확인 종료

    private static string TranslateText(string raw, int depth) // 리치 텍스트와 동적 값의 재귀적 표시 변환
    { // 표시 원문의 추가 구조 처리 시작
        if (depth > 5 || !ContainsKorean(raw)) // 반복 서식이나 번역이 필요 없는 코드 확인
        { // 추가 대조 없이 표시 시작
            return raw; // 변수의 숫자와 내용 보존
        } // 추가 대조 없이 표시 종료
        string source = raw.Trim(); // 리치 텍스트 주변 원래 여백 분리
        int leading = raw.IndexOf(source, StringComparison.Ordinal); // 앞쪽에 남겨야 하는 공백 일치 길이
        if (exact.TryGetValue(source, out string localized)) // 카드와 원문 버튼의 고정 번역 확인
        { // 정확한 한영 표 사용 시작
            return raw.Substring(0, leading) + localized + raw.Substring(leading + source.Length); // 원래 줄바꿈 여백을 보존한 실제 영어
        } // 정확한 한영 표 사용 종료
        foreach (Template template in templates) // 실제 렌더 직전의 수치와 이름 문구 후보 검사
        { // 현재 문구가 사용하는 템플릿 확인 시작
            if (template.TryTranslate(source, depth, out localized)) // 원문에 대응하는 정확한 변수 위치 조회
            { // 원문 서식의 영어 사용 시작
                return raw.Substring(0, leading) + localized + raw.Substring(leading + source.Length); // 현재 수치와 원래 여백을 가진 영어
            } // 원문 서식의 영어 사용 종료
        } // 현재 문구가 사용하는 템플릿 확인 종료
        string[] segments = Tags.Split(raw); // TMP 글자색 또는 굵기 마크와 본문 분리
        if (segments.Length > 1) // 색상이나 굵기가 있는 본문인지 확인
        { // 태그를 유지한 영어 적용 시작
            for (int index = 0; index < segments.Length; index += 2) // 원래 리치 텍스트의 태그 바깥 문구 순회
            { // 일반 텍스트 조각 변환 시작
                segments[index] = TranslateText(segments[index], depth + 1); // 태그 사이의 실제 표시 문구 확인
            } // 일반 텍스트 조각 변환 종료
            return string.Concat(segments); // 영어 뒤에도 글자 색과 굵기의 순서 유지
        } // 태그를 유지한 영어 적용 종료
        string[] lines = Separators.Split(raw); // 리치 텍스트의 내부 문장 확인 뒤 구분 기호 분리
        if (lines.Length > 1) // 개별 줄 단위로 다시 확인할 문구 존재
        { // 여러 줄 영어 표시 시작
            for (int index = 0; index < lines.Length; index += 2) // 실제 보이는 원래 항목만 순회
            { // 한 줄의 영어 표시 확인 시작
                lines[index] = TranslateText(lines[index], depth + 1); // 현재 줄만 번역해 원래 줄 개수 보존
            } // 한 줄의 영어 표시 확인 종료
            return string.Concat(lines); // 원래 행 구조와 구분 기호 사이에 영어 문구 조합
        } // 여러 줄 영어 표시 종료
        return raw; // 등록되지 않은 동적 원문 안전 표시
    } // 표시 원문의 추가 구조 처리 종료

    [Serializable] // Unity의 실제 로컬 JSON 역직렬화 등록
    private sealed class TranslationTable // 영어 자료 최상위 항목
    { // 번역 배열 저장 시작
        public Entry[] entries; // 게임에 포함된 고정 번역과 서식 목록
    } // 번역 배열 저장 종료

    [Serializable] // 게임리소스에 있는 하나의 한영 대응 항목
    private sealed class Entry // 번역 대응표 필드
    { // 파일에 담긴 한영 문자열 필드 시작
        public string source; // 현재 원래 한국어
        public string english; // 해당 문구의 영어 표시
        public bool template; // 자리표시자와 수치가 있는 동적 규칙 여부
    } // 파일에 담긴 한영 문자열 필드 종료

    private sealed class Template // 정확한 한국어 서식에서 실제 인수를 잡는 규칙
    { // 동적 캡처 규칙 시작
        private readonly Regex pattern; // 고정 한국어와 숫자 자리의 실제 비교 규칙
        private readonly string english; // 영어에 남긴 같은 인수 순서
        internal int FixedLength { get; } // 겹치는 동적 표현을 비교할 고정 문구 길이

        internal Template(string source, string english) // 원래 한국어 서식에 현재 값을 넣는 규칙 구성
        { // 구조화된 정규 표현식 작성 시작
            this.english = english; // 같은 번호에 영어 쪽에서 사용할 템플릿 기록
            MatchCollection slots = Slots.Matches(source); // 원문에 있는 변동 값 자리 검색
            var seen = new HashSet<string>(); // 같은 변수의 여러 출현을 확인할 집합
            var expression = new StringBuilder("^"); // 모든 현재 문구와 일치하는 기준점
            int start = 0; // 마지막 고정 조각의 시작점
            int markerLength = 0; // 자리표시자를 제외한 실제 원문 길이 기준
            foreach (Match slot in slots) // 번호가 붙은 자리마다 구체적인 실제 문구 캡처
            { // 동적 자리표시자 캡처 추가 시작
                string id = "v" + slot.Groups[1].Value; // 현재 변수의 안전한 정규식 캡처 이름
                expression.Append(Regex.Escape(source.Substring(start, slot.Index - start))); // 고정문을 정규식 특수 문자 없이 입력
                expression.Append(seen.Add(id) ? "(?<" + id + ">.*?)" : "\\k<" + id + ">"); // 첫 등장의 원래 값과 반복 값 일치 규칙
                start = slot.Index + slot.Length; // 현재 값 다음 고정 부분의 시작점 기록
                markerLength += slot.Length; // 고정문 길이에서 별도로 제외할 표식 길이
            } // 동적 자리표시자 캡처 추가 종료
            expression.Append(Regex.Escape(source.Substring(start))).Append('$'); // 마지막 고정 내용과 문자열 끝 기준
            FixedLength = source.Length - markerLength; // 여러 규칙의 일치 순서에 사용할 원문 길이
            pattern = new Regex(expression.ToString(), RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(20)); // 자리표시자가 이웃 표시 줄을 섞지 않는 실제 비교
        } // 구조화된 정규 표현식 작성 종료

        internal bool TryTranslate(string source, int depth, out string result) // 실제 동적 문자열이 맞으면 영어에 원래 값을 삽입
        { // 동적 문구 대조 시작
            result = null; // 현재 규칙이 맞지 않을 때의 출력
            if (source.Length < FixedLength) // 고정 문구보다 짧은 현재 이름이나 수치 확인
            { // 불가능한 템플릿 제외 시작
                return false; // 다른 화면 문구의 빠른 비교 진행
            } // 불가능한 템플릿 제외 종료
            try // 긴 사용자 문구에서 규칙 검사를 제한
            { // 변수 추출 및 현지 표시 시작
                Match match = pattern.Match(source); // 실제 한국어 문구와 번호별 변수 읽기
                if (!match.Success) // 바뀐 변수가 맞는 템플릿인지 확인
                { // 다른 템플릿 진행 시작
                    return false; // 해당 문구의 원래 서식이 아닌 경우
                } // 다른 템플릿 진행 종료
                result = Slots.Replace(english, slot => TranslateText(match.Groups["v" + slot.Groups[1].Value].Value, depth + 1)); // 값의 현지화와 원래 수치를 영어 구문에 재배치
                return true; // 실제 영어 동적 문구 생성 성공
            } // 변수 추출 및 현지 표시 종료
            catch (RegexMatchTimeoutException) // 유달리 긴 원래 입력과 서식의 시간 제한 확인
            { // 원래 문구 보존 경로 시작
                return false; // 프레임을 지연시키지 않는 다음 비교 또는 원문
            } // 원래 문구 보존 경로 종료
        } // 동적 문구 대조 종료
    } // 동적 캡처 규칙 종료
} // 표시 언어 처리 종료
