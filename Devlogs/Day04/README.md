# 4일차 : 로딩 화면·종료 확인 창·로비 기본 UI 구현

## 개발 목표

모든 씬 이동에 공통으로 적용되는 로딩 화면을 구현하고, 메인 메뉴의 게임 종료 확인 창과 로비의 기본 화면 구조를 구성한다.

## 구현 내용

### 1. 공용 씬 전환 화면 구현

`PF_AppRoot` 프리팹에 `PersistentUI`와 `TransitionCanvas`를 추가했다.

로딩 화면은 씬 전환 중 항상 최상단에 표시되도록 구성했으며, 다음 요소를 포함한다.

- 검은색 전환 배경
- `LOADING` 문구
- 진행률 바
- 진행률 백분율 문구
- 화면 페이드 인·아웃
- 로딩 중 하위 UI 입력 차단

`SceneTransitionUI` 스크립트를 생성하여 `CanvasGroup`의 투명도와 입력 차단 상태를 제어하도록 구성했다.

### 2. 비동기 씬 전환 기능 확장

`SceneFlowManager`의 비동기 씬 전환 기능을 확장했다.

추가된 처리 내용은 다음과 같다.

- 씬 전환 전 로딩 화면 페이드 인
- `AsyncOperation.progress` 기반 진행률 표시
- 진행률 0.9 상태를 100%로 변환하여 표시
- 최소 로딩 화면 표시 시간 적용
- 대상 씬 활성화 후 로딩 화면 페이드 아웃
- 씬 전환 중 입력 차단 유지
- 기존 중복 씬 전환 요청 방지 기능 유지

### 3. 메인 메뉴 게임 종료 확인 창 구현

`01_MainMenu` 씬에 `QuitConfirmPanel`을 추가했다.

게임 종료 버튼을 누르면 즉시 종료하지 않고 확인 창을 표시하도록 변경했다.

| 버튼 | 기능 |
|---|---|
| `QUIT` | 종료 확인 창 표시 |
| `YES` | 게임 종료 요청 |
| `NO` | 종료 확인 창 닫기 |

Unity Editor에서는 `Application.Quit()`이 실제 게임 종료를 실행하지 않으므로, Console 로그로 호출 여부를 확인한다.

### 4. 로비 기본 UI 구현

`02_Lobby` 씬에 `LobbyCanvas`를 추가하고 기본 화면 구조를 구성했다.

| UI 요소 | 표시 문구 | 상태 |
|---|---|---|
| 거점 제목 | `CERULION` | 표시 |
| 상태 문구 | `BASE STATUS` | 표시 |
| 탐사 버튼 | `EXPEDITION` | 기능 연결 |
| 덱 버튼 | `DECK` | 비활성화 |
| 시설 버튼 | `FACILITY` | 비활성화 |
| 메인 메뉴 버튼 | `MAIN MENU` | 기능 연결 |

`LobbyUI` 스크립트를 생성하여 다음 이동 기능을 연결했다.

- `EXPEDITION` → `10_Expedition`
- `MAIN MENU` → `01_MainMenu`

아직 구현하지 않은 `DECK`, `FACILITY` 버튼은 입력할 수 없도록 비활성화했다.

## 구현된 씬 흐름

```text
00_Boot
↓
로딩 화면
↓
01_MainMenu
├─ START → 로딩 화면 → 02_Lobby
│                         ├─ EXPEDITION → 10_Expedition
│                         └─ MAIN MENU → 01_MainMenu
├─ SETTINGS → 로딩 화면 → 03_Settings
│              └─ BACK → 01_MainMenu
└─ QUIT → 종료 확인 창
           ├─ YES → 게임 종료 요청
           └─ NO → 확인 창 닫기
확인 항목
00_Boot 실행 후 메인 메뉴까지 정상 이동
씬 전환 시 로딩 화면 표시
진행률 바와 백분율 표시
로딩 중 기존 화면 버튼 입력 차단
START 버튼으로 로비 이동
로비의 MAIN MENU 버튼으로 메인 메뉴 복귀
로비의 EXPEDITION 버튼으로 탐사 씬 이동
설정 화면 이동과 복귀 시 로딩 화면 적용
QUIT 버튼 클릭 시 종료 확인 창 표시
NO 버튼 클릭 시 종료 확인 창 닫힘
Console 빨간색 오류 없음
다음 개발 방향

5일차에는 설정 화면의 실제 기능을 구현한다.

마스터·배경음·효과음 음량 설정
전체 화면·창 모드 설정
해상도 선택
설정 적용 및 취소
PlayerPrefs 기반 저장·불러오기