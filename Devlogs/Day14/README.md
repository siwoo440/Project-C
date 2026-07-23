# 14일차 : 전투 승패 결과 UI 및 재시도·탐사 복귀 구현

## 완료 내용

- 플레이어 체력이 0이 되었을 때 전투 패배를 확정하도록 구현
- 전투 턴 관리자에 전투 패배 이벤트 추가
- 승리와 패배에서 공통으로 사용하는 전투 결과 UI 구현
- 전투 결과에 따라 `VICTORY` 또는 `DEFEAT` 문구가 표시되도록 구현
- 전투 종료 후 카드 선택과 턴 진행 입력 차단
- `RETRY` 버튼으로 현재 전투 씬을 다시 시작하도록 구현
- `RETURN TO EXPEDITION` 버튼으로 탐사 씬에 복귀하도록 구현
- `SceneFlowManager`에 현재 씬 재시작 기능 추가
- 전투 결과 버튼 중복 입력 방지 처리
- 기존 승리 전용 UI를 승패 공통 결과 UI로 통합
- `AppRootLifetime`을 추가하여 공용 관리자 루트의 생명주기 관리
- `PF_AppRoot` 최상위 오브젝트에 `AppRootLifetime` 연결
- 씬 이동 중 `GameManager`와 `SceneFlowManager`가 제거되는 문제 수정
- 중복 관리자 감지 시 공용 루트 전체가 아닌 중복 컴포넌트만 제거하도록 수정
- `20_Battle` 씬 직접 실행 시에도 공용 관리자가 초기화되도록 구성
- `TEST STRIKE` 카드 피해량을 8로 복구
- 적 테스트 행동 피해량을 12로 유지
- 전투 재시작과 탐사 복귀 후에도 `SceneFlowManager`가 유지되는지 확인
- Console 오류 없이 승리·패배 결과 흐름 테스트 완료

## 확인한 전투 흐름

### 승리 흐름

플레이어 턴 → 카드 사용 → 적 처치 → 전투 승리 판정 → 결과 화면 표시 → 전투 재시작 또는 탐사 복귀

### 패배 흐름

플레이어 턴 종료 → 적 행동 예고 → 적 공격 → 플레이어 체력 0 → 전투 패배 판정 → 결과 화면 표시 → 전투 재시작 또는 탐사 복귀

## 공용 관리자 유지 흐름

`PF_AppRoot` 생성 → `AppRootLifetime` 초기화 → `DontDestroyOnLoad` 등록 → 씬 이동 → 공용 관리자 유지

## 변경 파일

- `Assets/_ProjectC/Data/Cards/SO_Card_TestStrike.asset`
- `Assets/_ProjectC/Prefabs/Common/PF_AppRoot.prefab`
- `Assets/_ProjectC/Scenes/20_Battle.unity`
- `Assets/_ProjectC/Scripts/Battle/BattleResultUI.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs`
- `Assets/_ProjectC/Scripts/Managers/AppRootLifetime.cs`
- `Assets/_ProjectC/Scripts/Managers/GameManager.cs`
- `Assets/_ProjectC/Scripts/Managers/SceneFlowManager.cs`
- `Devlogs/Day14/README.md`

## 다음 작업

- 상태이상 실제 효과 처리
- 적 여러 명의 행동 순서와 속도 규칙 구현
- 전투 종료 보상 데이터 연결
- 탐사에서 전투 진입과 복귀 상태 저장