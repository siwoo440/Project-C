# 3일차 개발일지 - 기본 게임 씬 전환 흐름 구현

---

## 개발 목표

게임 실행 후 Boot Scene에서 MainMenu로 자동 진입하고, MainMenu·Lobby·Settings 사이의 기본 화면 전환 흐름을 실제 UI 버튼과 연결하는 것을 목표로 진행했다.

3일차 완료 기준은 다음과 같다.

```text
Boot → MainMenu → Lobby
          ↕
       Settings

Lobby ↔ Settings
```

---

## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Project Type | 2D |
| Core Script 위치 | `Assets/_ProjectC/Scripts/Core` |
| UI Script 위치 | `Assets/_ProjectC/Scripts/UI` |
| 시작 Scene | `00_Boot` |

---

## 개발 내용

### 1. BootLoader 구현

게임 실행 시 공용 관리자 준비 상태를 확인한 뒤 MainMenu로 자동 이동하는 `BootLoader`를 구현했다.

주요 처리 내용:

- `GameManager.Instance` 존재 여부 확인
- `SceneFlowManager.Instance` 존재 여부 확인
- `GameManager.IsInitialized` 상태 확인
- 초기화 완료 후 `10_MainMenu` 자동 이동
- 관리자 누락 시 오류 로그 출력 후 Boot 진행 중단

`BootLoader`는 공용 관리자가 아니므로 `Systems` 밖에 배치하고 Boot Scene이 종료될 때 함께 제거되도록 구성했다.

```text
00_Boot
├─ Main Camera
├─ Global Light 2D
├─ Systems
│  ├─ GameManager
│  └─ SceneFlowManager
└─ BootLoader
```

---

### 2. SceneFlowManager 이전 Scene 복귀 기능 추가

기존 `SceneFlowManager`에 이전 Scene을 기억하는 기능을 추가했다.

추가된 주요 기능:

- `PreviousSceneName` 저장
- Scene 전환 전에 현재 Scene 이름 기록
- `LoadPreviousScene()` 구현
- Settings Scene에서 직전 Scene으로 복귀 가능

이를 통해 다음 흐름을 지원한다.

```text
10_MainMenu
↓
90_Settings
↓
10_MainMenu
```

그리고:

```text
20_Lobby
↓
90_Settings
↓
20_Lobby
```

흐름도 동일하게 처리할 수 있게 되었다.

---

### 3. MainMenuController 구현

MainMenu의 기본 버튼 동작을 담당하는 `MainMenuController`를 구현했다.

기능:

- `StartGame()` → `20_Lobby`
- `OpenSettings()` → `90_Settings`

모든 Scene 이동은 Unity의 `SceneManager`를 UI에서 직접 호출하지 않고 기존 `SceneFlowManager`를 통해 실행하도록 통일했다.

```text
MainMenu UI
↓
MainMenuController
↓
SceneFlowManager
↓
Scene 전환
```

---

### 4. MainMenu 기본 UI 구성

`10_MainMenu` Scene에 기본 화면 전환 테스트용 UI를 구성했다.

주요 구성:

```text
10_MainMenu
├─ Main Camera
├─ Global Light 2D
├─ Canvas
│  ├─ Title
│  ├─ StartButton
│  └─ SettingsButton
├─ EventSystem
└─ MainMenuController
```

연결:

- `StartButton` → `MainMenuController.StartGame()`
- `SettingsButton` → `MainMenuController.OpenSettings()`

Canvas는 기본 해상도 대응을 위해 `Scale With Screen Size` 방식으로 구성했다.

---

### 5. LobbyController 구현

세룰리온 거점의 임시 화면에서 Settings Scene으로 이동하기 위한 `LobbyController`를 구현했다.

기능:

- `OpenSettings()` → `90_Settings`

현재 단계에서는 실제 거점 콘텐츠를 구현하지 않고 기본 Scene 흐름 검증용 기능만 적용했다.

---

### 6. Lobby 기본 UI 구성

`20_Lobby` Scene에 기본 UI를 구성했다.

```text
20_Lobby
├─ Main Camera
├─ Global Light 2D
├─ Canvas
│  ├─ LobbyTitle
│  └─ SettingsButton
├─ EventSystem
└─ LobbyController
```

연결:

- `SettingsButton` → `LobbyController.OpenSettings()`

향후 거점 시스템 개발 단계에서 파티 편성, 덱 편성, 치료, 설비 강화, 탐사 출발 등의 기능을 확장할 예정이다.

---

### 7. SettingsController 구현

Settings Scene에서 이전 화면으로 돌아가기 위한 `SettingsController`를 구현했다.

기능:

- `Back()` → `SceneFlowManager.LoadPreviousScene()`

Settings Controller 자체는 실제 그래픽·사운드·게임플레이 설정 데이터를 아직 처리하지 않는다.

---

### 8. Settings 기본 UI 구성

`90_Settings` Scene에 기본 복귀 테스트용 UI를 구성했다.

```text
90_Settings
├─ Main Camera
├─ Global Light 2D
├─ Canvas
│  ├─ SettingsTitle
│  └─ BackButton
├─ EventSystem
└─ SettingsController
```

연결:

- `BackButton` → `SettingsController.Back()`

현재 Settings 제목 문자열은 임시 UI 상태이며 실제 Settings 기능 구현 단계에서 UI와 함께 정리한다.

---

### 9. TextMesh Pro 기본 리소스 추가

MainMenu, Lobby, Settings의 임시 UI 제작 과정에서 TextMesh Pro Essentials를 프로젝트에 추가했다.

이에 따라 기본 TMP 폰트, Material, Shader, Sprite 및 Settings 리소스가 프로젝트에 포함되었다.

---

## 생성 및 수정된 주요 스크립트

```text
Assets/_ProjectC/Scripts
├─ Core
│  ├─ BootLoader.cs
│  └─ SceneFlowManager.cs
└─ UI
   ├─ MainMenuController.cs
   ├─ LobbyController.cs
   └─ SettingsController.cs
```

구분:

| 파일 | 작업 |
|---|---|
| `BootLoader.cs` | 생성 |
| `SceneFlowManager.cs` | 수정 |
| `MainMenuController.cs` | 생성 |
| `LobbyController.cs` | 생성 |
| `SettingsController.cs` | 생성 |

---

## 수정된 Scene

```text
Assets/_ProjectC/Scenes
├─ 00_Boot.unity
├─ 10_MainMenu.unity
├─ 20_Lobby.unity
└─ 90_Settings.unity
```

`30_Exploration`과 `40_Battle`은 이번 일차에서 수정하지 않았다.

---

## 기본 게임 진입 흐름

3일차 작업을 통해 다음 기본 실행 흐름을 구축했다.

```text
게임 실행
   ↓
00_Boot
   ↓ 자동
10_MainMenu
   ├─ Game Start → 20_Lobby
   └─ Settings → 90_Settings
                    ↓
                   Back
                    ↓
                10_MainMenu

20_Lobby
   └─ Settings → 90_Settings
                    ↓
                   Back
                    ↓
                 20_Lobby
```

---

## 공용 관리자 유지 구조

Scene 전환 이후에도 2일차에 구현한 공용 관리자는 유지된다.

```text
DontDestroyOnLoad
└─ Systems
   ├─ GameManager
   └─ SceneFlowManager
```

UI Controller는 각 Scene에 종속되고, 공용 Manager만 Scene과 관계없이 유지되는 구조를 사용한다.

---

## 완료 확인

- [x] `BootLoader` 구현
- [x] Boot에서 MainMenu 자동 진입 구조 구현
- [x] `SceneFlowManager.PreviousSceneName` 추가
- [x] 이전 Scene 복귀 기능 구현
- [x] `MainMenuController` 구현
- [x] MainMenu → Lobby 연결
- [x] MainMenu → Settings 연결
- [x] `LobbyController` 구현
- [x] Lobby → Settings 연결
- [x] `SettingsController` 구현
- [x] Settings → 이전 Scene 복귀 연결
- [x] MainMenu 기본 UI 구성
- [x] Lobby 기본 UI 구성
- [x] Settings 기본 UI 구성
- [x] TextMesh Pro 기본 리소스 추가
- [x] 모든 기본 Scene 이동을 `SceneFlowManager`를 통해 통일
- [x] 기존 공용 관리자 유지 구조와 연동

---

## 3일차 결과

게임을 실행했을 때 Boot Scene에서 공용 시스템을 초기화한 뒤 MainMenu로 진입하고, MainMenu·Lobby·Settings 사이를 실제 UI 버튼을 통해 이동할 수 있는 기본 게임 흐름을 구축했다.

이를 통해 이후 기능 개발에서 각 Scene의 세부 시스템 구현에 집중할 수 있는 기본 진입 구조가 완성되었다.

---

## Commit

```text
3일차 : 기본 게임 씬 전환 흐름 구현
```

---

## 다음 개발 방향

다음 단계에서는 캐릭터·카드·적 데이터를 Unity Asset으로 생성하고 관리할 수 있도록 ScriptableObject 기반의 기본 데이터 구조를 구축한다.
