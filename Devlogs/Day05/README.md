# 5일차: 환경설정 시스템 구현

## 개발 목표

게임의 음량, 화면 모드, 해상도를 변경하고 저장할 수 있는 환경설정 시스템을 구현한다.

## 구현 내용

### 1. Audio Mixer 구성

- `MX_Master` Audio Mixer 생성
- `Master` 그룹을 기준으로 `Music`, `SFX` 자식 그룹 생성
- 코드에서 제어할 수 있도록 다음 음량 매개변수 노출
  - `MasterVolume`
  - `MusicVolume`
  - `SFXVolume`

### 2. 환경설정 관리자 구현

- `GameSettingsManager.cs` 생성
- 마스터 음량, 배경음 음량, 효과음 음량 관리
- 화면 모드와 해상도 설정 관리
- `PlayerPrefs`를 이용한 설정값 저장 및 불러오기
- Slider 값을 Audio Mixer의 데시벨 값으로 변환
- 게임 실행 시 마지막으로 저장된 설정 자동 적용

### 3. 공통 관리자 프리팹 연결

- `PF_AppRoot` 프리팹에 `GameSettingsManager` 추가
- `MX_Master` Audio Mixer 연결
- 씬이 변경되어도 환경설정 관리자가 유지되도록 구성

### 4. 환경설정 UI 구성

- 오디오 설정 패널 제작
- 디스플레이 설정 패널 제작
- 마스터 음량 Slider 추가
- 배경음 음량 Slider 추가
- 효과음 음량 Slider 추가
- 화면 모드 Dropdown 추가
- 해상도 Dropdown 추가
- `APPLY`, `CANCEL`, `BACK` 버튼 구성
- 설정 변경 상태를 표시하는 `StatusText` 추가

### 5. 환경설정 UI 기능 구현

- Slider 조작 시 음량 값 즉시 표시
- 음량 변경 시 Audio Mixer에 실시간 반영
- 화면 모드 선택 기능 구현
- 사용 가능한 해상도 목록 자동 생성
- `APPLY` 버튼을 통한 설정 적용 및 저장
- `CANCEL` 버튼을 통한 마지막 저장 상태 복구
- `BACK` 버튼을 통한 미저장 설정 취소 및 메인 메뉴 복귀
- 저장하지 않은 변경 사항 표시 기능 구현

### 6. 화면 설정 구현

다음 화면 모드를 선택할 수 있도록 구성했다.

- Borderless Fullscreen
- Windowed
- Exclusive Fullscreen

현재 실행 환경에서 지원하는 해상도를 확인하여 Dropdown에 자동으로 표시하도록 구현했다.

## 추가 및 수정된 주요 파일

- `Assets/_ProjectC/Audio/Mixers/MX_Master.mixer`
- `Assets/_ProjectC/Scripts/Managers/GameSettingsManager.cs`
- `Assets/_ProjectC/Scripts/UI/SettingsMenuUI.cs`
- `Assets/_ProjectC/Prefabs/Common/PF_AppRoot.prefab`
- `Assets/_ProjectC/Scenes/03_Settings.unity`

## 최종 동작 흐름

1. 게임 실행 시 저장된 환경설정을 불러온다.
2. 설정 화면에서 음량, 화면 모드, 해상도를 변경한다.
3. 음량 변경은 즉시 미리 적용된다.
4. `APPLY`를 누르면 모든 설정을 적용하고 저장한다.
5. `CANCEL`을 누르면 마지막 저장값으로 복구한다.
6. `BACK`을 누르면 미저장 변경을 취소하고 메인 메뉴로 이동한다.
7. 게임을 다시 실행해도 저장된 설정이 유지된다.

## 테스트 항목

- [x] Master 음량 조절
- [x] Music 음량 조절
- [x] SFX 음량 조절
- [x] 음량 백분율 문구 갱신
- [x] 화면 모드 선택
- [x] 해상도 목록 생성
- [x] APPLY 설정 저장
- [x] CANCEL 설정 복구
- [x] BACK 미저장 변경 취소
- [x] 씬 이동 후 설정 유지
- [x] 게임 재실행 후 설정 유지
- [x] Audio Mixer 매개변수 연동
- [x] Console 오류 확인

## 완료 결과

게임의 기본 환경설정 시스템을 완성했다. 사용자는 음량, 화면 모드, 해상도를 변경할 수 있으며 적용한 설정은 게임을 종료한 뒤 다시 실행해도 유지된다.