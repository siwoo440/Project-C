# 27일차 개발 일지

---
## 개발 목표

- 전투 기능이 공통으로 사용할 이벤트 시스템 구축
- 카드 사용부터 전투 종료까지 이벤트 발생 순서 통합
- 각성·붕괴 시 해당 캐릭터를 강조하는 컷인 연출 추가

---
## 구현 내용

### 1. 전투 공용 이벤트 정의

- 전투 시작, 턴 시작·종료, 카드 사용, 피해, 회복, 상태 효과, 정신력 변화, 사망, 전투 종료 이벤트 추가
- 이벤트 순번, 라운드, 전투 단계, 시전자, 대상, 카드, 적용 수치, 전투 결과를 하나의 컨텍스트로 전달
- 다중 대상 목록을 복사해 이벤트 발행 이후 외부 변경 방지

### 2. 이벤트 발행기 구축

- 전투마다 독립적인 이벤트 발행기 생성
- 우선순위가 높은 구독자부터 호출하고 같은 우선순위는 등록 순서 유지
- 이벤트 처리 중 발생한 후속 이벤트를 대기열에 넣어 순차 처리
- 한 구독자의 예외가 다른 구독자의 실행을 중단하지 않도록 예외 격리
- 구독 반환값을 해제하면 자동으로 구독 취소

### 3. 기존 전투 시스템 연결

- 카드 사용, 피해, 회복, 상태 효과, 정신력 변화, 사망 결과를 공용 이벤트로 변환
- 턴 시작·종료와 전투 시작·종료 신호 연결
- 전투 중 소환된 적도 이벤트 대상에 자동 등록하고 제거 시 연결 해제
- 전투 종료 결과 저장과 정신 상태 정리가 끝난 뒤 종료 이벤트 발행

### 4. 이벤트 순서 정리

`CardUsed → DamageApplied / HealingApplied / StatusApplied → MentalChanged → UnitDefeated → BattleEnded`

- 카드 효과 이전에 카드 사용 이벤트 발생
- 실제 적용 결과 이후 피해·회복·상태 이벤트 발생
- 정신력 변화 이후 각성·붕괴 상태 이벤트 발생
- 사망 판정 이후 유닛 사망 이벤트 발생

### 5. 이벤트 디버그 로그 추가

- 모든 공용 이벤트를 순번과 함께 Unity Console에 출력
- 라운드, 단계, 시전자, 대상, 카드, 적용 수치 확인 가능
- 가장 낮은 구독 우선순위를 사용해 실제 게임 로직 처리 뒤 기록

### 6. 각성·붕괴 컷인 연출 추가

- 정신 상태 시작 이벤트를 감지하는 전투 컷인 화면 추가
- 각성은 금색, 붕괴는 붉은색으로 구분
- 어두운 전체 화면 배경, 중앙 캐릭터 초상화, 이름, 상태 제목 표시
- 컷인 중 게임 시간을 일시 정지하고 페이드 인·유지·페이드 아웃을 비동기 처리
- 여러 캐릭터가 동시에 상태에 진입하면 순서대로 연출
- 전투 종료나 화면 해제 시 기존 시간 배율 복구
- 초상화가 없으면 `초상화 미지정` 안내 표시

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleEventType.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEventContext.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEventSubscription.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEventDispatcher.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEventController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEventDebugLogger.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleMentalStateCutInView.cs`
- 위 스크립트의 Unity `.meta` 파일
- `Devlogs/Day27/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`

---
## 삭제 파일

- 없음

---
## 검증 결과

- 원격 `main`과 작업 시작 기준 커밋 일치 확인
- `dotnet build Assembly-CSharp.csproj --no-restore` 경고 0개, 오류 0개
- `git diff --check` 통과
- Unity `.meta` GUID 중복 0개
- 신규 C# 코드의 줄별 한글 주석 규칙 확인
- Scene, Prefab, Canvas 에셋 직접 변경 없음
- Unity Play Mode 실행 검증은 미수행으로 최종 수동 확인 필요

---
## Unity에서 직접 확인할 부분

1. `CharacterData`와 `EnemyData`의 `Portrait`에 사용할 Sprite 연결
2. `Mental Focus` 카드로 아군 각성 발생 확인
3. `Mental Break` 카드로 적 붕괴 발생 확인
4. 각성 금색 컷인과 붕괴 붉은색 컷인의 초상화·이름·시간 정지 확인
5. Console에서 카드 사용부터 전투 종료까지 이벤트 순서 확인

현재 프로젝트의 캐릭터 데이터에 초상화 Sprite가 연결되지 않아, 연결 전에는 컷인에 `초상화 미지정`이 표시된다.

---
## 다음 개발 방향

- 공용 이벤트를 사용하는 유물 시스템의 기본 구조 구축
- 유물 데이터, 발동 조건, 효과 실행 구조 분리
- 턴당·전투당 발동 횟수 제한 추가
- 유물 발동 로그와 전투 UI 표시 연결

---
## 커밋 제목

`27일차 : 전투 공용 이벤트 및 정신 상태 컷인 시스템 구축`
