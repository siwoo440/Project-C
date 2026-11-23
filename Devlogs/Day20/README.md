# 20일차 : 전투 결과 전달 및 아군 상태 유지 시스템 구축

---
## 개발 목표

- 전투 종료 결과를 탐험 씬까지 안전하게 전달
- 승리·패배·도주 결과와 아군 상태를 하나의 데이터로 보관
- 도주 후에도 감소한 아군 체력을 다음 전투까지 유지
- 전투 결과 화면을 코드 기반 Canvas UI로 구성

---
## 구현 내용

### 1. 전투 결과 데이터 구조 추가

- `BattleUnitResultData`에 아군 ID, 이름, 현재 체력, 최대 체력, 사망 여부 저장
- `BattleResultData`에 전투 결과, 전투 유형, 종료 라운드, 보상 가능 여부 저장
- 처치한 적 ID 목록과 생존 아군 수 저장

### 2. 전투 결과 관리자 추가

- `BattleResultManager`를 씬 전환 후에도 유지되는 단일 관리자로 구성
- 탐험 씬에 전달할 대기 중 전투 결과를 한 번만 소비하도록 분리
- 아군 체력 저장소를 대기 중 결과와 별도로 관리
- 탐험 씬에서 결과를 소비해도 저장된 아군 체력은 유지

### 3. 도주 후 아군 체력 유지

- 전투 종료 시 모든 아군의 현재 체력을 ID 기준으로 저장
- 다음 전투의 아군 런타임 생성 직후 저장 체력을 적용
- 저장 체력을 최대 체력 범위로 제한
- 체력이 0인 아군은 사망 상태까지 함께 복원

### 4. 전투 결과 화면 추가

- `BattleResultView`가 전투 Canvas에 결과 오버레이를 자동 생성
- 전투 결과, 종료 라운드, 생존 아군 수, 보상 상태 표시
- 확인 버튼으로 탐험 씬 전환
- 정상적인 씬 전환 요청 이후 버튼 중복 입력 차단

### 5. 탐험 씬 결과 수신 추가

- `ExplorationBattleResultReceiver`를 탐험 씬에서 자동 생성
- 대기 중 전투 결과를 한 번만 가져와 결과와 아군 체력을 확인
- 별도 씬 또는 프리팹 수정 없이 실행되도록 구성

### 6. 전투 종료 처리 보강

- 승리·패배·도주 결과를 중복 저장하지 않도록 방지
- 처치되거나 소환 슬롯 재사용으로 제거된 적 ID 기록
- 전투 재진입 시 이전 대기 결과만 정리하고 저장 아군 체력은 유지
- 적 결과 이벤트 구독 해제 처리 추가

---
## 전투 결과 규칙

- 승리: 보상 수령 가능
- 패배: 보상 수령 불가
- 도주: 보상 수령 불가, 현재 아군 체력 유지

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleUnitResultData.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitResultData.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleResultData.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleResultData.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleResultManager.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleResultManager.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleResultView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleResultView.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationBattleResultReceiver.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationBattleResultReceiver.cs.meta`
- `Devlogs/Day20/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`

---
## 삭제 파일

- 없음

---
## 검증 결과

- C# 프로젝트 빌드: 경고 0건, 오류 0건
- Git 변경 형식 검사: 통과
- 신규 Unity 메타 GUID 중복 검사: 이상 없음
- 씬 및 프리팹 변경: 없음
- 전투 결과 중복 저장 방지 확인
- 탐험 씬 결과 1회 소비 구조 확인
- 결과 소비 후 아군 체력 저장 유지 구조 확인

Unity 실행 검증은 `00_Boot` 씬에서 시작해 전투 진입, 피해 발생, 도주, 탐험 복귀, 다음 전투 재진입 순서로 확인해야 한다.

---
## 다음 개발 방향

- 상태 이상 ID, 수치, 지속 시간 데이터 구조 설계
- 상태 이상 적용·갱신·해제 규칙 구현
- 턴 또는 라운드 종료 시 지속 시간 감소 처리
- 전투 UI에 상태 이상 표시 기반 추가

---
## 커밋 제목

`20일차 : 전투 결과 전달 및 아군 상태 유지 시스템 구축`
