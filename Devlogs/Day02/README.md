# 2일차 개발일지 - 공용 관리자 및 씬 전환 시스템 구현

---

## 개발 목표

게임 전체에서 공통으로 사용할 관리자 구조를 만들고, 씬이 변경되어도 공용 관리자가 유지되도록 구성하는 것을 목표로 진행했다.

---

## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Project Type | 2D |
| 주요 작업 위치 | `Assets/_ProjectC/Scripts/Core` |
| 기준 Scene | `00_Boot` |

---

## 개발 내용

### 1. GameManager 구현

`GameManager`를 생성하고 게임 전체에서 하나의 인스턴스만 사용하도록 Singleton 구조를 적용했다.

주요 기능은 다음과 같다.

- `GameManager.Instance`를 통한 전역 접근 구조 구현
- 중복 `GameManager` 생성 방지
- 같은 `Systems` 내부에 중복 Manager가 생성된 경우 중복 오브젝트만 제거
- 다른 `Systems`가 중복 생성된 경우 새 `Systems` 전체 제거
- `DontDestroyOnLoad`를 이용해 씬 변경 후에도 `Systems` 유지
- 초기화 완료 상태를 확인할 수 있는 `IsInitialized` 추가
- 초기화 완료 로그 출력

---

### 2. SceneFlowManager 구현

씬 이동을 전담하는 `SceneFlowManager`를 생성했다.

주요 기능은 다음과 같다.

- `SceneFlowManager.Instance`를 통한 전역 접근
- Scene 이름을 이용한 씬 전환
- Build Index를 이용한 씬 전환
- 현재 Scene 이름 확인
- 현재 Scene Build Index 확인
- 씬 전환 중복 호출 방지
- 잘못된 Scene 이름 검사
- 잘못된 Build Index 검사
- Scene 로드 완료 이벤트 처리
- Scene 로드 완료 후 전환 상태 초기화

---

### 3. Unity Editor용 씬 전환 테스트 기능 추가

별도의 테스트 UI 없이 `SceneFlowManager` Inspector에서 씬 이동을 확인할 수 있도록 Editor 전용 Context Menu를 추가했다.

테스트 가능한 Scene은 다음과 같다.

```text
00_Boot
10_MainMenu
20_Lobby
30_Exploration
40_Battle
90_Settings
```

실제 빌드에는 테스트용 Context Menu 코드가 포함되지 않도록 `UNITY_EDITOR` 조건부 컴파일을 사용했다.

---

### 4. 00_Boot 공용 관리자 구조 구성

`00_Boot` Scene의 `Systems` 아래에 공용 관리자 오브젝트를 배치했다.

```text
00_Boot
├─ Main Camera
├─ Global Light 2D
└─ Systems
   ├─ GameManager
   └─ SceneFlowManager
```

`GameManager`가 `Systems` 루트 전체에 `DontDestroyOnLoad`를 적용하기 때문에 씬을 이동해도 두 Manager가 함께 유지된다.

---

### 5. 씬 전환 및 중복 방지 테스트

다음 순서로 Scene 이동을 반복하며 공용 관리자가 정상적으로 유지되는지 확인했다.

```text
00_Boot
↓
10_MainMenu
↓
20_Lobby
↓
30_Exploration
↓
40_Battle
↓
90_Settings
↓
00_Boot
```

Boot Scene으로 다시 돌아왔을 때 새 `Systems`가 생성되더라도 기존 공용 관리자와 중복되지 않도록 처리했다.

최종적으로 런타임에서는 다음 구조가 하나만 유지되는 것을 목표로 검증했다.

```text
DontDestroyOnLoad
└─ Systems
   ├─ GameManager
   └─ SceneFlowManager
```

---

### 6. Git 관리 설정 정리

Unity 프로젝트에 맞게 `.gitignore`와 `.gitattributes`를 정리했다.

`.gitignore`에서는 다음과 같은 Unity 및 IDE 자동 생성 파일을 제외하도록 구성했다.

- `Library`
- `Temp`
- `Logs`
- `obj`
- `UserSettings`
- Visual Studio / Rider / VS Code 캐시
- 빌드 결과물
- 기타 임시 파일

`.gitattributes`에서는 Unity YAML과 C# 파일은 Git에서 비교 가능한 텍스트로 유지하고, 대용량 바이너리 에셋은 Git LFS 대상으로 구성했다.

Git LFS 대상에는 다음 종류가 포함된다.

- 이미지
- 오디오
- 영상
- 폰트
- 3D 모델
- 압축 파일
- UnityPackage
- 일부 바이너리 파일

---

## 생성 및 수정된 주요 파일

```text
Assets/_ProjectC/Scripts/Core
├─ GameManager.cs
├─ GameManager.cs.meta
├─ SceneFlowManager.cs
└─ SceneFlowManager.cs.meta
```

수정된 주요 항목:

```text
Assets/_ProjectC/Scenes/00_Boot.unity
.gitignore
.gitattributes
```

---

## 완료 확인

- [x] `GameManager` 구현
- [x] `GameManager` Singleton 적용
- [x] 중복 Manager 생성 방지
- [x] `Systems`에 `DontDestroyOnLoad` 적용
- [x] `SceneFlowManager` 구현
- [x] Scene 이름 기반 전환 구현
- [x] Build Index 기반 전환 구현
- [x] Scene 유효성 검사 구현
- [x] 중복 Scene 전환 방지
- [x] Scene 로드 완료 이벤트 처리
- [x] Editor용 Scene 이동 테스트 기능 추가
- [x] `00_Boot`에 공용 관리자 연결
- [x] Scene 변경 후 공용 관리자 유지 구조 구축
- [x] `.gitignore` 정리
- [x] `.gitattributes` 프로젝트 기준으로 재구성
- [x] Git LFS 적용 기반 준비

---

## 2일차 결과

2일차 작업을 통해 게임의 모든 Scene에서 공통으로 사용할 수 있는 관리자 기반을 구축했다.

이후 시스템은 `GameManager`와 `SceneFlowManager`를 기반으로 확장할 수 있으며, Scene이 변경되어도 공용 시스템이 유지되는 구조가 마련되었다.

---

## Commit

```text
2일차 : 공용 관리자 및 씬 전환 시스템 구현
```

---

## 다음 개발 방향

다음 단계에서는 `Boot → MainMenu → Lobby → Settings` 기본 Scene 흐름을 실제 게임 진입 구조로 연결하고, 각 Scene이 공용 관리자와 연동되어 정상적으로 전환되는지 구현한다.
