# 19일차 개발일지 - 승리·패배·도주 및 적 소환 규칙 구축

---
## 개발 목표

- 전투 종료 결과를 승리·패배·도주로 통합
- 일반 전투와 보스 전투의 도주 규칙 분리
- 코드 생성 도주 버튼과 입력 제한 연결
- 전투 중 적 소환과 최대 적 수 제한 구현
- 소환 적의 화면·사망·행동 시스템 연결

---
## 개발 환경

- Unity 6000.3.21f1
- 기본 전투 유형: Normal
- 최대 생존 적 수: 4명
- 동시 전멸 판정: 패배 우선

---
## 주요 구현 내용

### 1. 전투 결과 통합

- `BattleResult`에 진행 중, 승리, 패배, 도주 결과 정의
- `BattleTurnRuntime`에서 현재 결과와 전투 종료 여부 통합 관리
- 아군 전멸 시 패배, 적 전멸 시 승리 처리
- 동시 전멸 상황에서는 아군 전멸을 우선해 패배 처리

### 2. 일반전·보스전 유형 추가

- `BattleType`에 일반 전투와 보스 전투 정의
- `BattleSceneSetup`에서 현재 전투 유형 설정
- `40_Battle` Scene을 일반 전투로 명시
- 전투 유형을 턴 관리자에 전달해 도주 규칙 판단

### 3. 도주 규칙 구현

- 일반 전투의 플레이어 턴에서만 도주 허용
- 적 턴, 행동 연출 중, 전투 종료 후 도주 차단
- 보스 전투에서 도주 차단
- 도주 성공 시 전투 결과를 `Escape`로 확정
- 도주 후 카드, 턴 종료, 적 행동 입력 중단
- 도주 시 보상 없음과 현재 아군 HP 유지 로그 출력

### 4. 도주 UI 자동 생성

- 카드 영역 상단에 `도주` 버튼 코드 생성
- 기존 덱 상태, 턴 상태, 턴 종료, 공용 AP 영역과 겹치지 않도록 재배치
- 도주 가능 조건과 행동 연출 잠금 상태를 버튼에 자동 반영
- 도주 완료 시 턴 상태를 `전투 도주`로 표시

### 5. 적 소환 API 추가

- `BattleSceneSetup.TrySummonEnemy` 공개 메서드 추가
- 적 원본 데이터에서 런타임 적과 전투 UI 생성
- 소환 적을 승패 판정과 사망 이벤트에 등록
- 소환 적을 적 행동 흐름에 등록
- 소환된 적은 현재 행동 목록에 끼어들지 않고 다음 행동 준비부터 참여

### 6. 소환 슬롯 규칙 적용

- 생존 적 최대 4명 제한
- 초기 적도 최대 4명으로 제한
- 화면이 가득 찬 상태에서 사망 적이 있으면 해당 슬롯 정리
- 사망 적 화면과 이벤트 연결을 해제한 뒤 소환 적 배치
- 소환 연결 실패 시 생성된 런타임과 화면 복구

### 7. 적 소환 테스트 기능 추가

- `Battle Scene Setup` Context Menu에 `테스트/첫 번째 적 데이터 소환` 추가
- Play Mode에서 현재 첫 적 데이터를 사용해 소환 가능
- 소환 성공 여부와 현재 생존 적 수를 Console에 출력

---
## 전투 결과 규칙

```text
모든 아군 사망 → 패배
모든 적 사망 → 승리
일반전 플레이어 턴 도주 → 도주
보스전 도주 → 거부
동시 전멸 → 패배 우선
```

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleResult.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleResult.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleType.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleType.cs.meta`
- `Devlogs/Day19/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleTurnPhase.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyActionRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleHandView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scenes/40_Battle.unity`

---
## 삭제 파일

- 없음

---
## 검증 결과

- C# 프로젝트 빌드 경고 0개, 오류 0개
- `git diff --check` 통과
- 신규 Meta GUID 중복 없음
- 임시 C# 프로젝트 파일 변경 없음
- 일반전 도주 가능 조건 적용
- 보스전 도주 제한 적용
- 전투 종료 후 입력 및 적 행동 차단 적용
- 소환 적의 승패 판정과 행동 흐름 등록 적용
- 최대 생존 적 4명 제한 적용

Unity Play Mode에서는 일반전 도주 버튼, 보스전 도주 제한, Context Menu 적 소환, 소환 적의 다음 라운드 행동, 최대 적 수 제한, 소환 적을 포함한 승리 판정을 최종 확인한다.

---
## 개발 결과

승리와 패배만 존재하던 전투 종료 구조에 도주 결과를 추가하고 모든 결과를 하나의 런타임 상태로 관리하도록 개선했다. 일반전과 보스전의 도주 규칙을 분리했으며, 소환된 적이 기존 적과 동일하게 화면, 사망 판정, 속도 기반 행동 순서에 참여할 수 있는 기반을 구축했다.

---
## 다음 개발 방향

- 전투 결과 데이터 객체 구성
- 승리·패배·도주 결과의 다음 Scene 전달
- 승리 보상과 도주 무보상 분기
- 전투 종료 화면과 확인 버튼 구현
- 탐사 또는 로비 복귀 흐름 연결

---
## 커밋 제목

`19일차 : 승리·패배·도주 및 적 소환 규칙 구축`
