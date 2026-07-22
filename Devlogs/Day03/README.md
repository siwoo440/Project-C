확인 결과, 최신 커밋 `6aef598`에 3일차 작업이 정상적으로 올라가 있습니다.

* 비동기 씬 전환과 중복 요청 방지 구현
* `Boot → MainMenu` 자동 이동 연결
* 메인 메뉴의 `START`, `SETTINGS`, `QUIT` 버튼 연결
* 설정 화면의 `BACK` 버튼 연결
* TextMesh Pro 필수 리소스 추가
* `Day03` 개발 일지는 아직 저장소에 없음

단, 실제 Play Mode 실행 결과와 Console 오류 여부는 GitHub 파일만으로 확인할 수 없습니다.

개발 일지 경로:

```text
Devlogs/Day03/README.md
```

아래 내용을 복사하여 사용합니다.

````markdown
---

# 프로젝트 C 개발 일지

---

## 3일차: 비동기 씬 전환 및 메인 메뉴 구현

---

### 개발 목표

게임 실행 시 부트 씬에서 메인 메뉴로 자동 이동하는 흐름을 구성하고, 메인 메뉴에서 로비와 설정 화면으로 이동할 수 있는 기본 UI를 구현한다.

---

### 구현 내용

---

#### 1. 비동기 씬 전환 기능 구현

`SceneFlowManager`에 `SceneManager.LoadSceneAsync`를 사용하는 비동기 씬 전환 기능을 추가했다.

주요 기능은 다음과 같다.

- 메인 메뉴 씬 이동
- 로비 씬 이동
- 설정 씬 이동
- 지정한 이름의 씬 이동
- 현재 씬과 동일한 씬의 중복 로딩 방지
- Build Profiles에 등록되지 않은 씬의 로딩 차단
- 씬 로딩 시작 및 완료 로그 출력

---

#### 2. 씬 전환 중 중복 요청 방지

씬을 불러오는 동안 추가 전환 요청이 실행되지 않도록 `IsLoading` 상태를 추가했다.

버튼을 빠르게 여러 번 누르더라도 첫 번째 요청만 처리되며, 이후 요청은 경고 로그를 출력하고 무시한다.

---

#### 3. 부트 씬 자동 이동 구현

`BootSceneController`를 생성하고 `00_Boot` 씬의 `Systems` 오브젝트에 연결했다.

게임이 시작되면 공용 관리자의 초기화 상태를 확인한 뒤 자동으로 `01_MainMenu` 씬을 불러오도록 구성했다.

최종 시작 흐름은 다음과 같다.

```text
00_Boot
↓
01_MainMenu
````

---

#### 4. 메인 메뉴 UI 구성

`01_MainMenu` 씬에 TextMesh Pro 기반의 임시 메인 메뉴 UI를 구성했다.

추가된 UI 요소는 다음과 같다.

| UI 요소            | 표시 문구       | 기능          |
| ---------------- | ----------- | ----------- |
| `TitleText`      | `CHAOSPONS` | 임시 게임 제목 표시 |
| `StartButton`    | `START`     | 로비 씬 이동     |
| `SettingsButton` | `SETTINGS`  | 설정 씬 이동     |
| `QuitButton`     | `QUIT`      | 게임 종료 요청    |

화면 크기에 대응할 수 있도록 `Canvas Scaler`를 사용하고, 버튼 입력 처리를 위한 `EventSystem`을 추가했다.

---

#### 5. 메인 메뉴 버튼 처리 구현

`MainMenuUI`를 생성하고 메인 메뉴의 `Systems` 오브젝트에 연결했다.

각 버튼에는 다음 메서드를 연결했다.

| 버튼               | 연결 메서드                    | 처리 결과            |
| ---------------- | ------------------------- | ---------------- |
| `StartButton`    | `OnStartButtonClicked`    | `02_Lobby` 이동    |
| `SettingsButton` | `OnSettingsButtonClicked` | `03_Settings` 이동 |
| `QuitButton`     | `OnQuitButtonClicked`     | 게임 종료 요청         |

씬 관리자를 찾을 수 없는 경우 오류를 출력하고 씬 이동을 중단하도록 예외 처리를 추가했다.

---

#### 6. 설정 화면 기본 UI 구성

`03_Settings` 씬에 임시 설정 화면을 구성했다.

현재 단계에서는 실제 그래픽, 음량, 해상도 설정 기능을 구현하지 않고 다음 요소만 추가했다.

* `SETTINGS` 제목
* 화면 배경
* `BACK` 버튼
* 버튼 입력용 `EventSystem`

---

#### 7. 설정 화면 복귀 기능 구현

`SettingsMenuUI`를 생성하고 설정 화면의 `Systems` 오브젝트에 연결했다.

`BACK` 버튼을 누르면 `SceneFlowManager`를 통해 `01_MainMenu` 씬으로 복귀하도록 구성했다.

---

#### 8. TextMesh Pro 리소스 추가

메인 메뉴와 설정 화면의 텍스트 및 버튼 문구 표시를 위해 TextMesh Pro 필수 리소스를 프로젝트에 추가했다.

추가된 주요 리소스는 다음과 같다.

* Liberation Sans 글꼴
* SDF 글꼴 에셋
* TextMesh Pro 기본 설정
* 기본 머티리얼과 셰이더
* 기본 스타일 및 스프라이트 리소스

---

### 구현된 씬 흐름

```text
00_Boot
↓ 자동 이동
01_MainMenu
├─ START → 02_Lobby
├─ SETTINGS → 03_Settings
│              └─ BACK → 01_MainMenu
└─ QUIT → 게임 종료 요청
```

---

### 확인 결과

GitHub에 저장된 코드와 Unity 씬 파일을 기준으로 다음 내용을 확인했다.

* `SceneFlowManager`의 비동기 씬 로딩 코드 저장
* 씬 로딩 중 중복 요청 방지 코드 저장
* `00_Boot` 씬에 `BootSceneController` 연결
* `01_MainMenu` 씬에 `MainMenuUI` 연결
* 메인 메뉴 버튼 세 개의 메서드 연결
* `03_Settings` 씬에 `SettingsMenuUI` 연결
* 설정 화면 `BACK` 버튼의 메서드 연결
* 메인 메뉴와 설정 화면의 `EventSystem` 추가
* TextMesh Pro 필수 리소스 추가

실제 Play Mode 동작과 Console 오류 여부는 Unity Editor에서 별도로 확인한다.

---

### 완료 결과

게임 실행부터 메인 메뉴까지 이어지는 기본 진입 흐름을 완성했다.

메인 메뉴에서 로비와 설정 화면으로 이동할 수 있으며, 설정 화면에서 다시 메인 메뉴로 복귀할 수 있다. 또한 비동기 로딩과 중복 요청 방지 기능을 통해 이후 다른 게임 씬에도 같은 전환 구조를 사용할 수 있는 기반을 마련했다.

---

### 다음 개발 방향

4일차에는 씬 전환 연출과 로비 화면의 기본 구조를 구현한다.

주요 작업은 다음과 같다.

* 씬 로딩 화면 구성
* 로딩 진행률 표시
* 화면 페이드 인·아웃
* 씬 전환 중 UI 입력 차단
* 게임 종료 확인 창 구현
* `02_Lobby` 기본 UI와 화면 구조 구성

```

---

## 검색 참고

- [Project C GitHub 저장소](https://github.com/siwoo440/Project-C)
- [3일차 커밋](https://github.com/siwoo440/Project-C/commit/6aef598c3aeac21502c44dbe613e2b565cf7196a)
- [SceneFlowManager.cs](https://github.com/siwoo440/Project-C/blob/main/Assets/_ProjectC/Scripts/Managers/SceneFlowManager.cs)
- [BootSceneController.cs](https://github.com/siwoo440/Project-C/blob/main/Assets/_ProjectC/Scripts/Scenes/BootSceneController.cs)
- [MainMenuUI.cs](https://github.com/siwoo440/Project-C/blob/main/Assets/_ProjectC/Scripts/UI/MainMenuUI.cs)
- [SettingsMenuUI.cs](https://github.com/siwoo440/Project-C/blob/main/Assets/_ProjectC/Scripts/UI/SettingsMenuUI.cs)
```
