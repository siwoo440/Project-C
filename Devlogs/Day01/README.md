# Project C - 카오스폰즈 (ChaosPons)

다크 판타지 세계관을 기반으로 한 2D 턴제 덱빌딩 로그라이크 RPG 프로젝트입니다.

---

## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Project Type | 2D |
| Render Pipeline | Universal Render Pipeline / 2D |
| Platform | PC |
| Distribution | Steam 예정 |
| Genre | 덱빌딩 / 카드 / 턴제 전투 / 로그라이크 / 싱글플레이 |
| Repository | siwoo440/Project-C |

---

## 프로젝트 기본 구조

```text
Assets
└─ _ProjectC
   ├─ Art
   ├─ Audio
   ├─ Data
   │  ├─ Characters
   │  ├─ Cards
   │  ├─ Enemies
   │  ├─ Relics
   │  └─ Potions
   ├─ Materials
   ├─ Prefabs
   │  ├─ Characters
   │  ├─ Enemies
   │  ├─ UI
   │  └─ Common
   ├─ Scenes
   ├─ Scripts
   │  ├─ Core
   │  ├─ Battle
   │  ├─ Cards
   │  ├─ Characters
   │  ├─ Enemies
   │  ├─ Exploration
   │  ├─ Base
   │  ├─ Data
   │  ├─ Save
   │  └─ UI
   ├─ ScriptableObjects
   ├─ Settings
   └─ UI
```

---

## 기본 Scene 구성

| Build Index | Scene | 역할 |
|---:|---|---|
| 0 | `00_Boot` | 게임 시작 및 공용 시스템 초기화 |
| 1 | `10_MainMenu` | 메인 메뉴 |
| 2 | `20_Lobby` | 세룰리온 거점 |
| 3 | `30_Exploration` | 탐사 |
| 4 | `40_Battle` | 카드 기반 전투 |
| 5 | `90_Settings` | 게임 설정 |

---

# 개발 일지

---

## 1일차 - 프로젝트 기반 구조 정리

### 개발 목표

프로젝트 C의 본격적인 시스템 구현에 앞서 이후 기능을 안정적으로 추가할 수 있도록 Unity 프로젝트의 기본 폴더, Scene, 공용 오브젝트 구조를 정리했습니다.

### 개발 내용

- Unity 프로젝트 버전을 `6000.3.21f1`로 확정
- 2D 프로젝트 구조 사용
- 프로젝트 전용 루트 폴더 `Assets/_ProjectC` 생성
- Art, Audio, Data, Prefabs, Scripts 등 기능별 폴더 분리
- 캐릭터, 카드, 적, 유물, 포션 데이터용 하위 폴더 구성
- Core, Battle, Cards, Characters, Enemies, Exploration, Base, Data, Save, UI 스크립트 폴더 구성
- 프로젝트의 기본 Scene 6개 생성
- `00_Boot` Scene을 Build Index 0으로 지정
- 나머지 Scene을 개발 흐름에 맞춰 Build Settings에 등록
- `00_Boot` Scene에 향후 공용 Manager를 배치할 `Systems` 오브젝트 생성
- Unity `.meta` 파일을 포함한 Git 관리 구조 확인
- `.gitignore`를 통한 Unity 자동 생성 파일 제외 구조 적용

### 생성한 Scene

```text
Assets/_ProjectC/Scenes
├─ 00_Boot.unity
├─ 10_MainMenu.unity
├─ 20_Lobby.unity
├─ 30_Exploration.unity
├─ 40_Battle.unity
└─ 90_Settings.unity
```

### 00_Boot 기본 구조

```text
00_Boot
├─ Main Camera
├─ Global Light 2D
└─ Systems
```

`Systems` 오브젝트에는 이후 GameManager, SceneFlowManager 등의 공용 시스템을 연결할 예정입니다.

### 네이밍 규칙

| 종류 | 규칙 | 예시 |
|---|---|---|
| Scene | 숫자_이름 | `40_Battle` |
| C# Class | PascalCase | `BattleManager` |
| Script | 클래스명과 동일 | `BattleManager.cs` |
| Prefab | 종류_이름 | `Enemy_AshWraith` |
| UI Prefab | UI_기능 | `UI_BattleResult` |
| Sprite | SPR_이름 | `SPR_Card_Back` |
| Material | MAT_이름 | `MAT_Card` |
| BGM | BGM_이름 | `BGM_Battle01` |
| SFX | SFX_이름 | `SFX_CardDraw` |
| ScriptableObject | 종류_이름 | `Card_BasicAttack` |

### 완료 확인

- [x] Unity 6000.3.21f1 적용
- [x] 2D 프로젝트 기반 구성
- [x] `_ProjectC` 전용 폴더 구조 생성
- [x] 6개 기본 Scene 생성
- [x] Build Settings Scene 순서 설정
- [x] `00_Boot`에 `Systems` 오브젝트 생성
- [x] Unity `.meta` 파일 Git 관리
- [x] `.gitignore` 적용
- [x] 1일차 기본 프로젝트 구조 완료

### Git Commit

```text
1일차 : 프로젝트 기반 구조 정리
```

검토 기준 Commit:

```text
eed599529f169aa32fb31766e226fb517f1ed236
```

---

## 다음 개발 방향

다음 일차부터 공용 시스템 구조를 구현하고 Scene 전환과 게임 전역 상태 관리를 위한 기반 코드를 작성합니다.
