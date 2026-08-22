# 32일차 개발 일지

---
## 개발 목표

- 탐사 Scene에서 플레이어가 직접 이동할 수 있는 기본 이동 구조 구축
- 탐사 맵 위 몬스터 조우 오브젝트와 접촉하면 전투 Scene으로 진입하도록 연결
- 조우별 적 구성을 전투 Scene에 전달
- 전투 종료 후 탐사 Scene으로 복귀하고 기존 위치에서 진행을 계속하도록 구성
- 승리한 조우는 탐사 맵에서 제거하고 도주·패배한 조우는 유지
- 빠른 진행 테스트를 위한 적 즉사 디버그 카드를 테스트 덱에 추가

---
## 구현 내용

### 1. 탐사 조우 데이터 구조 추가

탐사 맵의 몬스터 조우를 데이터로 관리하기 위해 `EncounterData`를 추가했다.

조우 데이터는 다음 정보를 가진다.

- Encounter ID
- 표시 이름
- 전투 종류
- 탐사 맵 배치 위치
- 전투에 등장할 적 목록

이를 통해 탐사 오브젝트와 실제 전투 적 구성을 하나의 데이터로 연결할 수 있도록 했다.

### 2. 테스트 조우 3종 추가

현재 프로젝트에 존재하는 테스트 적 데이터를 이용해 기본 조우 3종을 추가했다.

#### 테스트 조우 A

- Encounter ID: `ENC_TEST_A`
- 등장 적: `Enemy_Test` 1명

#### 테스트 조우 B

- Encounter ID: `ENC_TEST_B`
- 등장 적: `Enemy_TestPoison` 1명

#### 테스트 조우 C

- Encounter ID: `ENC_TEST_C`
- 등장 적:
  - `Enemy_Test`
  - `Enemy_TestPoison`

조우 데이터는 `Resources/Encounters`에서 런타임으로 불러오도록 구성했다.

### 3. 탐사 플레이어 자동 생성

`30_Exploration` Scene 진입 시 테스트용 탐사 플레이어를 자동 생성한다.

플레이어 기본 기능:

- WASD 이동
- 방향키 이동
- Rigidbody2D 기반 이동
- Collider를 이용한 조우 접촉 판정
- 테스트 맵 범위 밖으로 이동하지 않도록 위치 제한

플레이어는 현재 프로토타입 확인용 파란색 사각형으로 표시한다.

### 4. 탐사 몬스터 조우 자동 생성

`ExplorationPrototypeBootstrap`에서 `Resources/Encounters`의 조우 데이터를 읽고 탐사 Scene에 몬스터 조우 오브젝트를 자동 생성한다.

각 조우 오브젝트는 데이터에 지정된 위치에 배치된다.

현재는 실제 몬스터 그래픽 대신 색상 사각형을 사용한다.

이미 클리어한 Encounter ID는 다시 생성하지 않는다.

### 5. 플레이어와 몬스터 접촉 전투 시작

플레이어가 조우 오브젝트의 Trigger Collider에 접촉하면 `ExplorationEncounterView`가 조우를 시작한다.

처리 순서:

`플레이어와 몬스터 접촉`
`→ EncounterData 확인`
`→ 현재 플레이어 위치 저장`
`→ 현재 Encounter 저장`
`→ 40_Battle Scene 이동`

Scene 이동이 중복 실행되지 않도록 조우 오브젝트 내부에서 전환 요청 상태를 관리한다.

### 6. 탐사 진행 상태 관리자 추가

`ExplorationSessionManager`를 추가해 탐사와 전투 사이에서 필요한 진행 상태를 관리한다.

관리 정보:

- 현재 전투 중인 Encounter
- 전투 종료 후 돌아갈 플레이어 위치
- 클리어한 Encounter ID 목록

해당 관리자는 `DontDestroyOnLoad`로 유지되어 `30_Exploration → 40_Battle → 30_Exploration` Scene 이동 중에도 조우 상태를 보존한다.

### 7. 전투 후 복귀 위치 저장

몬스터와 접촉하면 전투에 진입하기 전 플레이어 위치를 저장한다.

복귀 위치는 몬스터 중심에서 약간 떨어진 방향으로 보정한다.

이를 통해 전투 후 탐사 Scene으로 돌아왔을 때 즉시 같은 몬스터 Trigger와 다시 충돌하는 문제를 방지한다.

처리 흐름:

`몬스터 접촉`
`→ 현재 위치 저장`
`→ 전투`
`→ 탐사 복귀`
`→ 저장 위치로 플레이어 배치`

### 8. 탐사 조우 데이터를 전투 Scene에 적용

`ExplorationSceneRuntimeRouter`를 추가해 Scene 로드 시 현재 조우 상태에 따라 필요한 초기화를 처리한다.

탐사 Scene에서는:

- 탐사 진행 관리자 준비
- 테스트용 플레이어와 조우 생성
- 전투 결과 수신기 준비

전투 Scene에서는:

- 현재 `EncounterData` 조회
- 해당 조우의 적 목록 조회
- `BattleSceneSetup`의 적 목록에 적용
- 조우의 `BattleType` 적용

기존 `40_Battle` Scene을 직접 수정하지 않고 현재 `BattleSceneSetup` 구조에 런타임 조우 데이터를 전달하도록 구성했다.

### 9. 전투 결과와 조우 상태 연결

기존 `ExplorationBattleResultReceiver`를 확장해 전투 결과를 단순히 로그로 출력하는 것에서 실제 탐사 진행 상태에 반영하도록 변경했다.

#### 승리

`BattleResult.Victory`
`→ 현재 Encounter ID를 클리어 목록에 추가`
`→ 다음 탐사 Scene 생성 시 해당 몬스터 생성하지 않음`

#### 도주

`Escape`
`→ Encounter를 클리어하지 않음`
`→ 탐사 복귀 후 몬스터 유지`

#### 패배

`Defeat`
`→ Encounter를 클리어하지 않음`
`→ 몬스터 유지`

현재 조우는 결과 처리 후 해제되어 다음 몬스터와 정상적으로 새 조우를 시작할 수 있다.

### 10. 탐사 테스트 HUD 추가

현재 프로토타입 상태를 쉽게 확인할 수 있도록 탐사 화면에 간단한 테스트 HUD를 자동 생성한다.

표시 내용:

- 이동 방법
- 몬스터 접촉 시 전투 진입 안내
- 승리/도주/패배 시 조우 처리 규칙
- 현재 클리어한 조우 수

현재 HUD와 탐사 오브젝트는 최종 UI 및 아트가 아닌 시스템 검사용이다.

### 11. 직접 Scene 테스트 지원

`30_Exploration` Scene부터 직접 Play Mode를 시작하는 경우에도 조우 테스트가 가능하도록 런타임 `SceneFlowManager` 준비 처리를 추가했다.

기존 게임 정상 실행 과정에서 `SceneFlowManager`가 존재하면 기존 인스턴스를 그대로 사용한다.

### 12. 즉사 디버그 카드 추가

탐사와 반복 전투 흐름을 빠르게 확인하기 위해 테스트 전용 카드를 추가했다.

카드 정보:

- 카드 ID: `CRD_DEBUG_INSTANT_KILL`
- 표시 이름: `[DEBUG] 즉사`
- AP 비용: 0
- 대상: 단일 적
- 효과: Damage
- 피해 종류: 물리 피해
- 피해량: 999999

현재 전투의 일반 Damage 카드 처리 방식을 그대로 사용하므로 별도의 즉사 전용 전투 로직은 추가하지 않았다.

### 13. 테스트 덱에 즉사 카드 추가

`Deck_Test`에 `[DEBUG] 즉사` 카드를 3장 추가했다.

이를 통해 탐사 → 전투 → 탐사 복귀 루프 테스트 중 적을 빠르게 처치할 수 있도록 했다.

기존 테스트 덱 카드 구성은 유지하고 즉사 카드만 추가했다.

---
## 생성 파일

- `Assets/_ProjectC/Resources/Encounters.meta`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_A.asset`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_A.asset.meta`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_B.asset`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_B.asset.meta`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_C.asset`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_C.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_DebugInstantKill.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_DebugInstantKill.asset.meta`
- `Assets/_ProjectC/Scripts/Exploration/EncounterData.cs`
- `Assets/_ProjectC/Scripts/Exploration/EncounterData.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEncounterView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEncounterView.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPlayerController.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPlayerController.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSceneRuntimeRouter.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSceneRuntimeRouter.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs.meta`
- `Devlogs/Day32/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationBattleResultReceiver.cs`
- `Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset`

---
## 삭제 파일

- 없음

---
## 검토 결과

- 최신 `main` 커밋이 31일차 커밋보다 정확히 1개 앞선 상태 확인
- 탐사 조우 데이터 3종이 추가된 것을 확인
- 탐사 플레이어 이동 스크립트 추가 확인
- 플레이어와 조우 Trigger 접촉 시 전투 Scene으로 이동하는 구조 확인
- `ExplorationSessionManager`에서 현재 조우, 복귀 위치, 클리어 조우 목록을 관리하는 구조 확인
- 조우별 적 목록과 전투 종류가 `40_Battle`의 `BattleSceneSetup`에 적용되는 구조 확인
- 전투 승리 시 현재 Encounter ID가 클리어 목록에 등록되는 구조 확인
- 도주 및 패배 시 Encounter가 클리어되지 않는 구조 확인
- 탐사 복귀 시 이전 조우 근처 위치로 플레이어가 복원되는 구조 확인
- 이미 클리어한 Encounter가 탐사 Scene에서 다시 생성되지 않는 구조 확인
- `[DEBUG] 즉사` 카드가 AP 0, 단일 적 대상, 물리 피해 999999로 추가된 것을 확인
- `Deck_Test`에 즉사 디버그 카드 3장이 추가된 것을 확인
- 32일차 변경에서 삭제된 파일이 없는 것을 확인
- GitHub에 등록된 자동 CI 상태 검사는 없음
- 정적 코드 검토 기준으로 진행을 차단하는 문제는 발견하지 못함
- 실제 Unity 컴파일 및 Play Mode 전체 동작은 GitHub 소스만으로 자동 확인할 수 없어 최종 수동 테스트 필요

---
## Unity에서 직접 확인할 부분

1. `30_Exploration`과 `40_Battle`이 Build Profile의 Scene 목록에 등록되어 있는지 확인
2. `30_Exploration` Scene 실행
3. 파란색 테스트 플레이어가 생성되는지 확인
4. 색상 조우 오브젝트 3개가 생성되는지 확인
5. WASD 또는 방향키로 플레이어 이동 확인
6. 첫 번째 몬스터 조우와 접촉
7. `40_Battle` Scene으로 자동 이동하는지 확인
8. 접촉한 조우 데이터에 맞는 적이 생성되는지 확인
9. 전투가 Lv.1 / EXP 0에서 시작되는지 확인
10. `[DEBUG] 즉사` 카드가 테스트 덱에서 등장하는지 확인
11. 즉사 카드를 적에게 사용했을 때 적이 빠르게 처치되는지 확인
12. 승리 결과 화면에서 확인 버튼 사용
13. `30_Exploration`으로 복귀하는지 확인
14. 플레이어가 이전 조우 근처 위치에서 다시 생성되는지 확인
15. 승리한 몬스터 조우가 사라졌는지 확인
16. 다른 조우는 그대로 남아 있는지 확인
17. 두 번째 몬스터와 접촉하여 다시 전투 가능한지 확인
18. 새 전투에서 Level/EXP와 마이너 카드 상태가 초기화되는지 확인
19. 기존 HP·정신력·유물·소모품 진행 데이터가 기존 규칙대로 유지되는지 확인
20. 도주 테스트 시 해당 몬스터가 탐사 맵에서 유지되는지 확인

---
## 다음 개발 방향

- 현재 런타임 사각형 기반 탐사 프로토타입을 실제 맵 오브젝트 및 프리팹 구조로 전환
- 맵별 Encounter 배치 데이터를 별도 탐사 맵 데이터로 분리
- 몬스터 조우 전 표시, 이름, 위험도 등의 탐사 정보 UI 추가
- 전투 승리 후 실제 보상 선택 또는 드롭 시스템을 탐사 진행과 연결
- 일반·엘리트·보스 조우에 따른 외형과 전투 진입 연출 구분
- 탐사 진행 종료 및 다음 지역 이동 조건 구축

---
## 커밋 제목

`32일차 : 탐사 이동·몬스터 조우 및 반복 전투 루프 구축`
