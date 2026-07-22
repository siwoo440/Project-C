# 프로젝트 C 개발 일지

---

## 2일차: 공용 관리자 기반 구성

### 개발 목표

게임 실행 중 계속 유지되는 공용 관리자 구조를 구성하고, 이후 씬 전환 기능에 사용할 씬 정보 관리 기반을 만든다.

### 구현 내용

#### 1. 1일차 프로젝트 구조 정리

- Unity 기본 `SampleScene` 삭제
- `.gitignore`에 `*.slnx` 규칙 추가
- Unity가 자동 생성하는 솔루션 파일을 Git 추적 대상에서 제외
- 1일차 개발 일지의 Markdown 표기 오류 정리
- 실제 Unity 프로젝트 버전을 `6000.3.9f1`로 통일

#### 2. GameManager 구현

`GameManager`를 공용 게임 관리자 스크립트로 구현했다.

주요 기능은 다음과 같다.

- 싱글턴 방식의 전역 인스턴스 제공
- 게임 시작 시 공용 초기화 실행
- 초기화 완료 상태 기록
- `PF_AppRoot` 전체를 `DontDestroyOnLoad`로 유지
- 중복된 공용 관리자 생성 시 중복 루트 제거
- Play 모드 종료 또는 오브젝트 제거 시 정적 참조 해제

#### 3. SceneFlowManager 구현

`SceneFlowManager`를 씬 흐름 관리 기반 스크립트로 구현했다.

주요 기능은 다음과 같다.

- 주요 씬 이름 상수 관리
- 현재 활성 씬 이름 확인
- Build Profiles에 등록된 씬인지 확인
- Inspector Context Menu를 통한 전체 씬 등록 상태 점검
- 중복된 씬 관리자 생성 방지
- 이후 비동기 씬 전환 기능을 추가할 수 있는 구조 준비

#### 4. PF_AppRoot 연결

`PF_AppRoot` 프리팹의 `Managers` 오브젝트에 다음 컴포넌트를 연결했다.

- `GameManager`
- `SceneFlowManager`

`PF_AppRoot`는 `00_Boot` 씬에만 배치하며, 다른 씬에는 별도로 배치하지 않는다.

### 주요 씬 목록

| 씬 이름 | 역할 |
|---|---|
| `00_Boot` | 공용 관리자 초기화 |
| `01_MainMenu` | 메인 메뉴 |
| `02_Lobby` | 세룰리온 거점 |
| `03_Settings` | 환경 설정 |
| `10_Expedition` | 탑다운 탐사 |
| `20_Battle` | 카드 전투 |

### 테스트 결과

- `00_Boot` 실행 시 `GameManager` 초기화 로그 확인
- `SceneFlowManager` 초기화 로그 확인
- 주요 씬 6개의 Build Profiles 등록 상태 확인
- 중복 `PF_AppRoot` 생성 시 중복 루트 제거 확인
- `PF_AppRoot`가 씬 전환 이후에도 유지되는 구조 확인
- Unity Console 빨간색 오류 없음 확인

### 완료 결과

공용 게임 관리자와 씬 정보 관리자 기반을 구성했다. 이제 다음 작업에서 `00_Boot`에서 `01_MainMenu`로 이동하는 실제 씬 전환 기능과 메인 메뉴 UI를 구현할 수 있다.

### 다음 개발 방향

3일차에는 `SceneFlowManager`에 비동기 씬 전환 기능을 추가하고, `00_Boot → 01_MainMenu` 자동 전환 및 메인 메뉴 기본 화면을 구현한다.