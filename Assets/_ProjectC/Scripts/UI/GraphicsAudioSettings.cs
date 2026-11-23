using System; // 설정 값 문자열 변환 사용
using System.Collections.Generic; // 설정 값 목록 사용
using UnityEngine; // Unity 설정과 저장 기능 사용

public interface ISettingsStore // 설정 값 저장소 계약
{ // 설정 저장소 시작
    int Read(string key, int fallback); // 저장된 설정 값 조회
    void Write(string key, int value); // 변경된 설정 값 저장
} // 설정 저장소 종료
public interface ISettingsApplier // 실제 설정 적용 계약
{ // 설정 적용기 시작
    void Apply(string key, GraphicsAudioSettings settings); // 지정 설정 값 적용
} // 설정 적용기 종료
public sealed class GraphicsAudioSettings // 지원하는 화면과 음량 설정
{ // 화면과 음량 설정 시작
    private sealed class Option // 변경 가능한 개별 설정
    { // 설정 항목 정의 시작
        public readonly string Key; // 설정 저장 식별자
        public readonly int Category; // 설정 탭 번호
        public readonly int Row; // 설정 항목 번호
        public readonly string[] Values; // 선택 가능한 값 목록
        public readonly int DefaultIndex; // 기획서 기본값 번호
        public Option(string key, int category, int row, string[] values, int defaultIndex) // 설정 선택값 구성
        { // 설정 선택값 구성 시작
            Key = key; // 설정 식별자 보관
            Category = category; // 설정 탭 번호 보관
            Row = row; // 설정 항목 번호 보관
            Values = values; // 설정 선택값 보관
            DefaultIndex = defaultIndex; // 기본값 번호 보관
        } // 설정 선택값 구성 종료
    } // 설정 항목 정의 종료
    private static readonly Option[] Options = CreateOptions(); // 지원하는 설정 목록
    private readonly ISettingsStore store; // 선택한 설정 저장소
    private readonly ISettingsApplier applier; // 실제 설정 적용기
    private readonly Dictionary<string, int> selected = new Dictionary<string, int>(); // 선택한 설정 값 목록
    public GraphicsAudioSettings(ISettingsStore settingsStore, ISettingsApplier settingsApplier) // 저장된 설정 불러오기
    { // 설정 관리자 생성 시작
        store = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore)); // 저장소 존재 확인
        applier = settingsApplier ?? throw new ArgumentNullException(nameof(settingsApplier)); // 적용기 존재 확인
        foreach (Option option in Options) // 지원 설정 순회
        { // 설정 값 복원 시작
            int value = store.Read(option.Key, option.DefaultIndex); // 저장 값 또는 기본값 조회
            selected[option.Key] = value >= 0 && value < option.Values.Length ? value : option.DefaultIndex; // 손상 값 기본 복구
        } // 설정 값 복원 종료
    } // 설정 관리자 생성 종료
    private static Option[] CreateOptions() // 기획서 기준 지원 선택값 생성
    { // 지원 선택값 생성 시작
        string[] volume = new string[101]; // 영부터 백까지 음량 선택값 준비
        for (int index = 0; index < volume.Length; index++) // 전체 음량 범위 순회
        { // 음량 선택값 생성 시작
            volume[index] = index.ToString(); // 음량 숫자 문구 생성
        } // 음량 선택값 생성 종료
        return new Option[] // 실제 지원 설정 목록 반환
        { // 지원 설정 목록 시작
            new Option("mode", 0, 0, new[] { "창모드", "전체화면", "무테창" }, 2), // 기획 화면 모드와 기본값
            new Option("resolution", 0, 1, new[] { "1280×720", "1600×900", "1920×1080", "2560×1440", "3840×2160" }, 2), // 기획 해상도와 기본값
            new Option("frame", 0, 2, new[] { "30", "60", "120", "무제한" }, 1), // 기획 프레임 제한과 기본값
            new Option("vsync", 0, 3, new[] { "켜기", "끄기" }, 1), // 기획 수직 동기화와 기본값
            new Option("quality", 0, 7, new[] { "낮음", "중간", "높음", "울트라" }, 2), // 기획 그래픽 품질과 기본값
            new Option("master", 1, 0, volume, 80) // 기획 마스터 음량과 기본값
        }; // 지원 설정 목록 종료
    } // 지원 선택값 생성 종료
    private static Option Find(int category, int row) // 설정 위치로 지원 항목 조회
    { // 지원 항목 조회 시작
        foreach (Option option in Options) // 지원 설정 목록 순회
        { // 지원 설정 비교 시작
            if (option.Category == category && option.Row == row) // 설정 위치 일치 확인
            { // 지원 설정 반환 시작
                return option; // 일치하는 지원 설정 반환
            } // 지원 설정 반환 종료
        } // 지원 설정 비교 종료
        return null; // 지원하지 않는 설정 반환
    } // 지원 항목 조회 종료
    public bool Supports(int category, int row) // 설정 변경 지원 여부 확인
    { // 지원 여부 확인 시작
        return Find(category, row) != null; // 실제 적용 가능한 설정 여부 반환
    } // 지원 여부 확인 종료
    public int GetIndex(string key) // 선택한 설정 번호 조회
    { // 선택 번호 조회 시작
        return selected.TryGetValue(key, out int value) ? value : -1; // 선택 번호 또는 미지원 표시 반환
    } // 선택 번호 조회 종료
    public string GetLabel(int category, int row) // 현재 설정의 표시값 조회
    { // 설정 표시값 조회 시작
        Option option = Find(category, row); // 설정 위치의 지원 항목 조회
        return option == null ? null : option.Values[selected[option.Key]]; // 현재 값 또는 미지원 표시 반환
    } // 설정 표시값 조회 종료
    public bool Change(int category, int row, int direction) // 설정 선택값 변경과 저장
    { // 설정 변경 시작
        Option option = Find(category, row); // 설정 위치의 지원 항목 조회
        if (option == null || direction == 0) // 지원 여부와 변경 방향 확인
        { // 변경 불가 처리 시작
            return false; // 설정 변경 거부
        } // 변경 불가 처리 종료
        int delta = direction > 0 ? 1 : -1; // 한 단계 변경 방향 계산
        int old = selected[option.Key]; // 기존 설정 번호 조회
        int next = option.Key == "master" ? Mathf.Clamp(old + delta, 0, option.Values.Length - 1) : (old + delta + option.Values.Length) % option.Values.Length; // 음량 범위 제한과 나머지 값 순환
        if (next == old) // 실제 값 변경 여부 확인
        { // 동일 설정 처리 시작
            return false; // 불필요한 설정 저장 중단
        } // 동일 설정 처리 종료
        selected[option.Key] = next; // 선택한 설정 값 갱신
        store.Write(option.Key, next); // 변경한 설정 값 저장
        applier.Apply(option.Key, this); // 변경한 설정 실제 적용
        return true; // 설정 변경 성공 반환
    } // 설정 변경 종료
    public void ApplyAll() // 시작 시 저장된 설정 전체 적용
    { // 저장 설정 적용 시작
        applier.Apply("quality", this); // 그래픽 품질 우선 적용
        applier.Apply("mode", this); // 화면 모드와 해상도 함께 적용
        applier.Apply("frame", this); // 프레임 제한 적용
        applier.Apply("vsync", this); // 품질 변경 후 수직 동기화 적용
        applier.Apply("master", this); // 마스터 음량 적용
    } // 저장 설정 적용 종료
} // 화면과 음량 설정 종료
public sealed class PlayerPrefsSettingsStore : ISettingsStore // 기기별 설정 저장소
{ // 기기 설정 저장소 시작
    private const string Prefix = "ProjectC.Option."; // 다른 저장 데이터와 구분할 접두어
    public int Read(string key, int fallback) // 기기 설정 값 조회
    { // 기기 설정 조회 시작
        return PlayerPrefs.GetInt(Prefix + key, fallback); // 저장 값 또는 기본값 반환
    } // 기기 설정 조회 종료
    public void Write(string key, int value) // 기기 설정 값 저장
    { // 기기 설정 저장 시작
        PlayerPrefs.SetInt(Prefix + key, value); // 기기 설정 값 기록
        PlayerPrefs.Save(); // 설정 변경 내용 보관
    } // 기기 설정 저장 종료
} // 기기 설정 저장소 종료
public sealed class UnitySettingsApplier : ISettingsApplier // Unity 화면과 음량 적용기
{ // Unity 설정 적용기 시작
    private static readonly int[] Widths = { 1280, 1600, 1920, 2560, 3840 }; // 기획 해상도 너비 목록
    private static readonly int[] Heights = { 720, 900, 1080, 1440, 2160 }; // 기획 해상도 높이 목록
    private static readonly int[] FrameRates = { 30, 60, 120, -1 }; // 기획 프레임 제한 목록
    private static readonly string[] QualityNames = { "Low", "Medium", "High", "Ultra" }; // 기획 품질에 대응하는 Unity 품질 이름
    public void Apply(string key, GraphicsAudioSettings settings) // 선택한 설정을 Unity에 반영
    { // Unity 설정 적용 시작
        if (key == "mode" || key == "resolution") // 화면 모드 또는 해상도 변경 확인
        { // 화면 설정 적용 시작
            int modeIndex = settings.GetIndex("mode"); // 선택한 화면 모드 조회
            int resolution = settings.GetIndex("resolution"); // 선택한 해상도 조회
            FullScreenMode mode = modeIndex == 0 ? FullScreenMode.Windowed : modeIndex == 1 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow; // 기획 화면 모드 변환
            if (!Application.isBatchMode) // 무인 테스트 환경 여부 확인
            { // 화면 크기 변경 시작
                Screen.SetResolution(Widths[resolution], Heights[resolution], mode); // 실제 화면 모드와 해상도 적용
            } // 화면 크기 변경 종료
        } // 화면 설정 적용 종료
        else if (key == "frame") // 프레임 제한 변경 확인
        { // 프레임 설정 적용 시작
            Application.targetFrameRate = FrameRates[settings.GetIndex("frame")]; // 실제 프레임 제한 적용
        } // 프레임 설정 적용 종료
        else if (key == "vsync") // 수직 동기화 변경 확인
        { // 수직 동기화 적용 시작
            QualitySettings.vSyncCount = settings.GetIndex("vsync") == 0 ? 1 : 0; // 실제 수직 동기화 적용
        } // 수직 동기화 적용 종료
        else if (key == "quality") // 그래픽 품질 변경 확인
        { // 그래픽 품질 적용 시작
            int index = Array.IndexOf(QualitySettings.names, QualityNames[settings.GetIndex("quality")]); // 실제 품질 이름 위치 조회
            if (index >= 0) // 선택 품질 존재 확인
            { // 선택 품질 적용 시작
                QualitySettings.SetQualityLevel(index, true); // 실제 그래픽 품질 변경
                QualitySettings.vSyncCount = settings.GetIndex("vsync") == 0 ? 1 : 0; // 품질 변경 후 수직 동기화 유지
            } // 선택 품질 적용 종료
        } // 그래픽 품질 적용 종료
        else if (key == "master") // 마스터 음량 변경 확인
        { // 마스터 음량 적용 시작
            AudioListener.volume = settings.GetIndex("master") / 100f; // 실제 전체 음량 적용
        } // 마스터 음량 적용 종료
    } // Unity 설정 적용 종료
} // Unity 설정 적용기 종료
public static class GraphicsAudioSettingsRuntime // 게임 전역 설정 관리자
{ // 전역 설정 관리자 시작
    private static GraphicsAudioSettings current; // 현재 설정 관리자
    public static GraphicsAudioSettings Current => current ??= new GraphicsAudioSettings(new PlayerPrefsSettingsStore(), new UnitySettingsApplier()); // 저장 설정 관리자 조회
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 세션 초기화 지정
    private static void Reset() // 이전 플레이 세션 참조 제거
    { // 설정 참조 초기화 시작
        current = null; // 전역 설정 관리자 초기화
    } // 설정 참조 초기화 종료
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 실행 지정
    private static void ApplySaved() // 게임 시작 시 저장된 설정 적용
    { // 시작 설정 적용 시작
        Current.ApplyAll(); // 저장된 화면과 음량 설정 적용
    } // 시작 설정 적용 종료
} // 전역 설정 관리자 종료
