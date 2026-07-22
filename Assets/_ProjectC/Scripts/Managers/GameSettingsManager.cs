using System; // 열거형 검사 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.Audio; // 오디오 믹서 기능

[DefaultExecutionOrder(-950)] // 게임 관리자 다음 실행 순서
public sealed class GameSettingsManager : MonoBehaviour // 게임 환경설정 관리자
{
    private const string MasterVolumeKey = "Settings.MasterVolume"; // 마스터 음량 저장 키
    private const string MusicVolumeKey = "Settings.MusicVolume"; // 배경음 음량 저장 키
    private const string SfxVolumeKey = "Settings.SFXVolume"; // 효과음 음량 저장 키
    private const string ResolutionWidthKey = "Settings.ResolutionWidth"; // 해상도 너비 저장 키
    private const string ResolutionHeightKey = "Settings.ResolutionHeight"; // 해상도 높이 저장 키
    private const string FullScreenModeKey = "Settings.FullScreenMode"; // 화면 모드 저장 키
    private const float MinimumDecibel = -80f; // 최소 데시벨
    private const float MinimumLinearVolume = 0.0001f; // 로그 계산 최소 음량

    [Header("오디오 설정")] // 오디오 설정 구분
    [SerializeField] private AudioMixer masterAudioMixer; // 전체 오디오 믹서

    public static GameSettingsManager Instance { get; private set; } // 공용 설정 관리자

    public float MasterVolume { get; private set; } = 1f; // 현재 마스터 음량
    public float MusicVolume { get; private set; } = 1f; // 현재 배경음 음량
    public float SfxVolume { get; private set; } = 1f; // 현재 효과음 음량
    public int ResolutionWidth { get; private set; } // 현재 해상도 너비
    public int ResolutionHeight { get; private set; } // 현재 해상도 높이
    public FullScreenMode CurrentFullScreenMode { get; private set; } // 현재 화면 모드

    private void Awake() // 설정 관리자 초기화
    {
        if (Instance != null && Instance != this) // 기존 관리자 확인
        {
            Debug.LogWarning("중복된 GameSettingsManager를 제거합니다.", this); // 중복 관리자 경고
            Destroy(this); // 중복 컴포넌트 제거
            return; // 중복 초기화 중단
        }

        Instance = this; // 현재 관리자 등록
        LoadSavedValues(); // 저장된 값 불러오기
    }

    private void Start() // 초기 설정 적용
    {
        ApplyCurrentSettings(); // 저장된 설정 실제 적용
    }

    public void PreviewAudio(float masterVolume, float musicVolume, float sfxVolume) // 오디오 설정 미리 듣기
    {
        MasterVolume = Mathf.Clamp01(masterVolume); // 마스터 음량 범위 제한
        MusicVolume = Mathf.Clamp01(musicVolume); // 배경음 음량 범위 제한
        SfxVolume = Mathf.Clamp01(sfxVolume); // 효과음 음량 범위 제한
        ApplyAudioSettings(); // 변경 음량 즉시 적용
    }

    public void ApplyAndSave( // 설정 적용 및 저장
        float masterVolume, // 마스터 음량 전달값
        float musicVolume, // 배경음 음량 전달값
        float sfxVolume, // 효과음 음량 전달값
        int resolutionWidth, // 해상도 너비 전달값
        int resolutionHeight, // 해상도 높이 전달값
        FullScreenMode fullScreenMode) // 화면 모드 전달값
    {
        MasterVolume = Mathf.Clamp01(masterVolume); // 마스터 음량 저장
        MusicVolume = Mathf.Clamp01(musicVolume); // 배경음 음량 저장
        SfxVolume = Mathf.Clamp01(sfxVolume); // 효과음 음량 저장
        ResolutionWidth = resolutionWidth; // 해상도 너비 저장
        ResolutionHeight = resolutionHeight; // 해상도 높이 저장
        CurrentFullScreenMode = fullScreenMode; // 화면 모드 저장
        ApplyCurrentSettings(); // 전체 설정 적용
        SaveCurrentValues(); // 전체 설정 저장
    }

    public void RestoreSavedSettings() // 저장된 설정 복구
    {
        LoadSavedValues(); // 저장값 다시 불러오기
        ApplyCurrentSettings(); // 저장값 실제 적용
    }

    private void LoadSavedValues() // PlayerPrefs 설정 불러오기
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f)); // 마스터 음량 불러오기
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f)); // 배경음 음량 불러오기
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f)); // 효과음 음량 불러오기
        ResolutionWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.currentResolution.width); // 해상도 너비 불러오기
        ResolutionHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.currentResolution.height); // 해상도 높이 불러오기

        int savedFullScreenMode = PlayerPrefs.GetInt(FullScreenModeKey, (int)FullScreenMode.FullScreenWindow); // 화면 모드 불러오기

        if (!Enum.IsDefined(typeof(FullScreenMode), savedFullScreenMode)) // 유효한 화면 모드 확인
        {
            savedFullScreenMode = (int)FullScreenMode.FullScreenWindow; // 기본 화면 모드 지정
        }

        CurrentFullScreenMode = (FullScreenMode)savedFullScreenMode; // 화면 모드 변환
    }

    private void SaveCurrentValues() // PlayerPrefs 설정 저장
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume); // 마스터 음량 저장
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume); // 배경음 음량 저장
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume); // 효과음 음량 저장
        PlayerPrefs.SetInt(ResolutionWidthKey, ResolutionWidth); // 해상도 너비 저장
        PlayerPrefs.SetInt(ResolutionHeightKey, ResolutionHeight); // 해상도 높이 저장
        PlayerPrefs.SetInt(FullScreenModeKey, (int)CurrentFullScreenMode); // 화면 모드 저장
        PlayerPrefs.Save(); // 저장값 디스크 반영
    }

    private void ApplyCurrentSettings() // 전체 환경설정 적용
    {
        ApplyAudioSettings(); // 오디오 설정 적용
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, CurrentFullScreenMode); // 화면 설정 적용
    }

    private void ApplyAudioSettings() // 전체 음량 설정 적용
    {
        SetMixerVolume("MasterVolume", MasterVolume); // 마스터 음량 적용
        SetMixerVolume("MusicVolume", MusicVolume); // 배경음 음량 적용
        SetMixerVolume("SFXVolume", SfxVolume); // 효과음 음량 적용
    }

    private void SetMixerVolume(string parameterName, float linearVolume) // 개별 믹서 음량 적용
    {
        if (masterAudioMixer == null) // 오디오 믹서 연결 확인
        {
            Debug.LogWarning("Master Audio Mixer가 연결되지 않았습니다.", this); // 믹서 누락 경고
            return; // 음량 적용 중단
        }

        float decibel = linearVolume <= 0f // 음소거 여부 확인
            ? MinimumDecibel // 음소거 데시벨 선택
            : Mathf.Log10(Mathf.Max(linearVolume, MinimumLinearVolume)) * 20f; // 선형 음량 데시벨 변환

        if (!masterAudioMixer.SetFloat(parameterName, decibel)) // 노출 매개변수 적용 확인
        {
            Debug.LogWarning($"Audio Mixer 매개변수를 찾을 수 없습니다: {parameterName}", this); // 매개변수 누락 경고
        }
    }

    private void OnDestroy() // 설정 관리자 제거 처리
    {
        if (Instance == this) // 현재 인스턴스 확인
        {
            Instance = null; // 공용 참조 해제
        }
    }
}