using System.Collections.Generic; // 목록 자료형 기능
using TMPro; // TextMesh Pro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class SettingsMenuUI : MonoBehaviour // 설정 화면 UI 관리자
{
    [Header("오디오 슬라이더")] // 오디오 슬라이더 구분
    [SerializeField] private Slider masterVolumeSlider; // 마스터 음량 슬라이더
    [SerializeField] private Slider musicVolumeSlider; // 배경음 음량 슬라이더
    [SerializeField] private Slider sfxVolumeSlider; // 효과음 음량 슬라이더

    [Header("오디오 값 문구")] // 오디오 문구 구분
    [SerializeField] private TMP_Text masterVolumeValueText; // 마스터 음량 문구
    [SerializeField] private TMP_Text musicVolumeValueText; // 배경음 음량 문구
    [SerializeField] private TMP_Text sfxVolumeValueText; // 효과음 음량 문구

    [Header("화면 설정")] // 화면 설정 구분
    [SerializeField] private TMP_Dropdown screenModeDropdown; // 화면 모드 드롭다운
    [SerializeField] private TMP_Dropdown resolutionDropdown; // 해상도 드롭다운

    [Header("상태 표시")] // 상태 표시 구분
    [SerializeField] private TMP_Text statusText; // 설정 상태 문구

    private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>(); // 사용 가능한 해상도 목록
    private GameSettingsManager settingsManager; // 공용 설정 관리자

    private void Start() // 설정 화면 초기화
    {
        settingsManager = GameSettingsManager.Instance; // 공용 설정 관리자 가져오기

        if (settingsManager == null) // 설정 관리자 존재 확인
        {
            Debug.LogError("GameSettingsManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            SetStatus("SETTINGS MANAGER NOT FOUND"); // 오류 상태 표시
            return; // 설정 화면 초기화 중단
        }

        ConfigureScreenModeDropdown(); // 화면 모드 목록 구성
        ConfigureResolutionDropdown(); // 해상도 목록 구성
        RefreshControlsFromManager(); // 저장값 UI 반영
        SetStatus(string.Empty); // 상태 문구 초기화
    }

    public void OnMasterVolumeChanged(float value) // 마스터 음량 변경
    {
        UpdateVolumeValueTexts(); // 음량 문구 갱신
        PreviewAudioSettings(); // 변경 음량 미리 적용
        SetStatus("UNSAVED CHANGES"); // 미저장 상태 표시
    }

    public void OnMusicVolumeChanged(float value) // 배경음 음량 변경
    {
        UpdateVolumeValueTexts(); // 음량 문구 갱신
        PreviewAudioSettings(); // 변경 음량 미리 적용
        SetStatus("UNSAVED CHANGES"); // 미저장 상태 표시
    }

    public void OnSfxVolumeChanged(float value) // 효과음 음량 변경
    {
        UpdateVolumeValueTexts(); // 음량 문구 갱신
        PreviewAudioSettings(); // 변경 음량 미리 적용
        SetStatus("UNSAVED CHANGES"); // 미저장 상태 표시
    }

    public void OnScreenModeChanged(int value) // 화면 모드 변경
    {
        SetStatus("UNSAVED CHANGES"); // 미저장 상태 표시
    }

    public void OnResolutionChanged(int value) // 해상도 변경
    {
        SetStatus("UNSAVED CHANGES"); // 미저장 상태 표시
    }

    public void OnApplyButtonClicked() // 설정 적용 버튼 처리
    {
        if (settingsManager == null) // 설정 관리자 존재 확인
        {
            Debug.LogError("GameSettingsManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 설정 적용 중단
        }

        int resolutionIndex = Mathf.Clamp( // 해상도 인덱스 범위 제한
            resolutionDropdown.value, // 현재 해상도 선택값
            0, // 최소 해상도 인덱스
            availableResolutions.Count - 1); // 최대 해상도 인덱스

        Vector2Int selectedResolution = availableResolutions[resolutionIndex]; // 선택 해상도 가져오기
        FullScreenMode selectedScreenMode = ConvertDropdownToScreenMode(screenModeDropdown.value); // 선택 화면 모드 변환

        settingsManager.ApplyAndSave( // 설정 적용과 저장 요청
            masterVolumeSlider.value, // 마스터 음량 전달
            musicVolumeSlider.value, // 배경음 음량 전달
            sfxVolumeSlider.value, // 효과음 음량 전달
            selectedResolution.x, // 해상도 너비 전달
            selectedResolution.y, // 해상도 높이 전달
            selectedScreenMode); // 화면 모드 전달

        RefreshControlsFromManager(); // 적용 결과 UI 갱신
        SetStatus("SAVED"); // 저장 완료 표시
    }

    public void OnCancelButtonClicked() // 설정 취소 버튼 처리
    {
        if (settingsManager == null) // 설정 관리자 존재 확인
        {
            Debug.LogError("GameSettingsManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 설정 취소 중단
        }

        settingsManager.RestoreSavedSettings(); // 저장된 설정 복구
        RefreshControlsFromManager(); // 복구 결과 UI 갱신
        SetStatus("RESTORED"); // 복구 완료 표시
    }

    public void OnBackButtonClicked() // 뒤로 가기 버튼 처리
    {
        if (settingsManager != null) // 설정 관리자 존재 확인
        {
            settingsManager.RestoreSavedSettings(); // 미적용 변경 취소
        }

        if (SceneFlowManager.Instance == null) // 씬 관리자 존재 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 메인 메뉴 이동 중단
        }

        SceneFlowManager.Instance.LoadMainMenu(); // 메인 메뉴 이동
    }

    private void ConfigureScreenModeDropdown() // 화면 모드 목록 생성
    {
        screenModeDropdown.ClearOptions(); // 기존 화면 모드 항목 제거

        List<string> screenModeOptions = new List<string> // 화면 모드 문구 목록
        {
            "BORDERLESS FULLSCREEN", // 테두리 없는 전체 화면
            "WINDOWED", // 창 모드
            "EXCLUSIVE FULLSCREEN" // 독점 전체 화면
        };

        screenModeDropdown.AddOptions(screenModeOptions); // 화면 모드 항목 추가
    }

    private void ConfigureResolutionDropdown() // 해상도 목록 생성
    {
        resolutionDropdown.ClearOptions(); // 기존 해상도 항목 제거
        availableResolutions.Clear(); // 기존 해상도 자료 제거

        Resolution[] supportedResolutions = Screen.resolutions; // 모니터 지원 해상도 가져오기

        foreach (Resolution resolution in supportedResolutions) // 지원 해상도 순회
        {
            Vector2Int resolutionSize = new Vector2Int(resolution.width, resolution.height); // 해상도 크기 생성

            if (!availableResolutions.Contains(resolutionSize)) // 중복 해상도 확인
            {
                availableResolutions.Add(resolutionSize); // 새 해상도 추가
            }
        }

        if (availableResolutions.Count == 0) // 해상도 목록 비어 있음 확인
        {
            availableResolutions.Add(new Vector2Int(Screen.width, Screen.height)); // 현재 화면 크기 추가
        }

        List<string> resolutionOptions = new List<string>(); // 해상도 표시 문구 목록

        foreach (Vector2Int resolution in availableResolutions) // 해상도 목록 순회
        {
            resolutionOptions.Add($"{resolution.x} × {resolution.y}"); // 해상도 문구 추가
        }

        resolutionDropdown.AddOptions(resolutionOptions); // 해상도 항목 추가
    }

    private void RefreshControlsFromManager() // 관리자 설정 UI 반영
    {
        masterVolumeSlider.SetValueWithoutNotify(settingsManager.MasterVolume); // 마스터 슬라이더 갱신
        musicVolumeSlider.SetValueWithoutNotify(settingsManager.MusicVolume); // 배경음 슬라이더 갱신
        sfxVolumeSlider.SetValueWithoutNotify(settingsManager.SfxVolume); // 효과음 슬라이더 갱신

        int screenModeIndex = ConvertScreenModeToDropdown(settingsManager.CurrentFullScreenMode); // 화면 모드 인덱스 변환
        screenModeDropdown.SetValueWithoutNotify(screenModeIndex); // 화면 모드 드롭다운 갱신

        int resolutionIndex = FindClosestResolutionIndex( // 저장 해상도 인덱스 검색
            settingsManager.ResolutionWidth, // 저장 해상도 너비
            settingsManager.ResolutionHeight); // 저장 해상도 높이

        resolutionDropdown.SetValueWithoutNotify(resolutionIndex); // 해상도 드롭다운 갱신
        resolutionDropdown.RefreshShownValue(); // 해상도 표시 문구 갱신
        screenModeDropdown.RefreshShownValue(); // 화면 모드 표시 문구 갱신
        UpdateVolumeValueTexts(); // 음량 표시 문구 갱신
    }

    private void PreviewAudioSettings() // 현재 슬라이더 음량 미리 적용
    {
        if (settingsManager == null) // 설정 관리자 존재 확인
        {
            return; // 미리 적용 중단
        }

        settingsManager.PreviewAudio( // 미리 듣기 요청
            masterVolumeSlider.value, // 마스터 음량 전달
            musicVolumeSlider.value, // 배경음 음량 전달
            sfxVolumeSlider.value); // 효과음 음량 전달
    }

    private void UpdateVolumeValueTexts() // 음량 백분율 문구 갱신
    {
        masterVolumeValueText.text = $"{Mathf.RoundToInt(masterVolumeSlider.value * 100f)}%"; // 마스터 백분율 표시
        musicVolumeValueText.text = $"{Mathf.RoundToInt(musicVolumeSlider.value * 100f)}%"; // 배경음 백분율 표시
        sfxVolumeValueText.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100f)}%"; // 효과음 백분율 표시
    }

    private int FindClosestResolutionIndex(int width, int height) // 가장 가까운 해상도 검색
    {
        int closestIndex = 0; // 기본 해상도 인덱스
        int smallestDifference = int.MaxValue; // 최소 크기 차이

        for (int index = 0; index < availableResolutions.Count; index++) // 해상도 목록 반복
        {
            Vector2Int resolution = availableResolutions[index]; // 현재 해상도 가져오기
            int difference = Mathf.Abs(resolution.x - width) + Mathf.Abs(resolution.y - height); // 해상도 크기 차이 계산

            if (difference < smallestDifference) // 기존 최소 차이 비교
            {
                smallestDifference = difference; // 최소 차이 갱신
                closestIndex = index; // 가장 가까운 인덱스 갱신
            }
        }

        return closestIndex; // 검색한 인덱스 반환
    }

    private int ConvertScreenModeToDropdown(FullScreenMode fullScreenMode) // 화면 모드 인덱스 변환
    {
        switch (fullScreenMode) // 현재 화면 모드 구분
        {
            case FullScreenMode.Windowed: // 창 모드 확인
                return 1; // 창 모드 인덱스 반환

            case FullScreenMode.ExclusiveFullScreen: // 독점 전체 화면 확인
                return 2; // 독점 전체 화면 인덱스 반환

            default: // 나머지 화면 모드 처리
                return 0; // 테두리 없는 전체 화면 반환
        }
    }

    private FullScreenMode ConvertDropdownToScreenMode(int dropdownIndex) // 드롭다운 화면 모드 변환
    {
        switch (dropdownIndex) // 선택 인덱스 구분
        {
            case 1: // 창 모드 인덱스 확인
                return FullScreenMode.Windowed; // 창 모드 반환

            case 2: // 독점 전체 화면 인덱스 확인
                return FullScreenMode.ExclusiveFullScreen; // 독점 전체 화면 반환

            default: // 나머지 인덱스 처리
                return FullScreenMode.FullScreenWindow; // 테두리 없는 전체 화면 반환
        }
    }

    private void SetStatus(string message) // 설정 상태 문구 변경
    {
        if (statusText != null) // 상태 문구 연결 확인
        {
            statusText.text = message; // 상태 문구 적용
        }
    }
}