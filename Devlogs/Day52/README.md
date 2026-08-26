# Project C - 52일차 개발일지

작성일: 2026-08-26  
기준 커밋: `1aa1c7830f3ac87b8d1f1d33bc54a0567be5d90c`

---

## 1. 개발 목표

52일차의 목표는 Prototype v0.1 상태 규칙을 현재 전투·탐사 구조에 맞게 정리하는 것이다.

핵심 기준은 다음과 같다.

- 전투 자원은 기존 AP 구조를 유지한다.
- 정신력과 HP 상태는 전투가 끝나도 유지한다.
- 탐사 중 사망한 아군은 즉시 부활하지 않는다.
- 사망 상태는 이후 전투와 탐사 진행에서도 유지한다.
- 탐사 상태 초기화 과정에서 저장된 파티 상태를 임의로 제거하지 않는다.
- 런타임 한글 UI가 Git LFS 폰트 상태와 관계없이 표시될 수 있도록 TMP 폰트 공급 경로를 보강한다.

---

## 2. 주요 변경 내용

### 2.1 전투 중 즉시 부활 제거

`BattleResultManager`에 존재하던 즉시 부활 기능을 제거했다.

삭제한 주요 요소는 다음과 같다.

- 기본 부활 HP 비율 상수
- 기본 부활 정신력 상수
- `ReviveSavedAlly()`
- `ReviveFirstDeadAlly()`
- 부활 대상 탐색용 `FindActiveCharacter()`

이제 탐사 중 HP가 0이 된 캐릭터는 일반적인 전투·탐사 흐름에서 즉시 부활하지 않는다.

---

### 2.2 탐사 HUD의 F7 부활 테스트 제거

`ExplorationPartyStatusView`에서 개발 테스트용 F7 즉시 부활 기능을 제거했다.

함께 제거한 요소는 다음과 같다.

- `UnityEngine.InputSystem` 부활 입력 의존
- `Keyboard.current` 기반 F7 입력 처리
- F7 입력 시 `ReviveFirstDeadAlly()` 호출
- 부활 입력에만 필요했던 탐사 완료 상태 참조

파티 HUD의 HP·정신력·사망 상태 표시는 그대로 유지한다.

---

### 2.3 파티 상태 영속 규칙 유지

`ExplorationSessionManager.ResetExploration()`에서
`BattleResultManager.ResetSavedPartyState()`를 호출하던 코드를 제거했다.

이에 따라 탐사 런 상태를 초기화할 때 다음 상태가 자동으로 지워지지 않는다.

- 아군 HP
- 아군 정신력
- 사망 상태
- 사망 횟수 기록

52일차에서는 상태 보관까지만 처리하며,
거점에서 사망 아군을 회복시키는 설비와 회복 진행 규칙은 다음 일차에서 구현한다.

---

## 3. 한글 TMP 폰트 안정화

52일차 적용 후 탐사 UI에서 한글 TMP 폰트 생성 실패가 확인되어
`ProjectCFontProvider`의 런타임 폰트 생성 방식을 보강했다.

프로젝트에 포함된 `NotoSansCJKkr-Regular.otf`가 Git LFS 포인터 상태인 경우
정상적인 폰트 데이터로 사용할 수 없으므로 다음 순서로 폰트를 준비한다.

1. 프로젝트 Resources의 NotoSans 폰트 사용 시도
2. 실제 한글 글리프 추가 가능 여부 검사
3. 실패하면 OS에 설치된 폰트 파일 경로 조회
4. 맑은 고딕, Noto Sans KR, 나눔 계열 등의 실제 폰트 파일 검색
5. 실제 파일 경로에서 동적 TMP SDF 폰트 생성
6. 한글 테스트 문자열을 직접 추가하여 출력 가능 여부 확인
7. 성공한 폰트를 TMP 전역 fallback에 등록
8. 기본 LiberationSans SDF의 fallback에도 연결

이를 통해 런타임 UI가 기본 LiberationSans에만 의존하면서 한글이 `□`로 표시되는 문제를 방지하도록 구성했다.

---

## 4. 수정 파일

| 파일 | 변경 내용 |
| --- | --- |
| `Assets/_ProjectC/Scripts/Battle/BattleResultManager.cs` | 전투·탐사 중 즉시 부활 기능 제거 |
| `Assets/_ProjectC/Scripts/Exploration/ExplorationPartyStatusView.cs` | F7 개발 부활 입력 제거 |
| `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs` | 탐사 초기화 시 저장 파티 상태 삭제 방지 |
| `Assets/_ProjectC/Scripts/UI/ProjectCFontProvider.cs` | Git LFS 및 OS 폰트 경로 대응 한글 TMP fallback 구축 |

---

## 5. 확인 항목

- Unity 프로젝트 컴파일 오류가 없는지 확인
- 탐사 Scene에서 한글 안내 문구가 정상 출력되는지 확인
- `LiberationSans SDF` 한글 누락 경고가 반복되지 않는지 확인
- 전투에서 감소한 정신력이 다음 전투에서도 유지되는지 확인
- 아군이 사망한 뒤 탐사 HUD에 사망 상태가 유지되는지 확인
- 다음 전투에서도 해당 캐릭터의 HP 0 상태가 유지되는지 확인
- F7 입력으로 사망 캐릭터가 부활하지 않는지 확인
- 살아 있는 파티원은 기존 방식대로 전투를 진행할 수 있는지 확인
- 파티 전멸 시 탐사 실패 처리가 유지되는지 확인
- 기존 카드의 AP 비용과 사용 흐름이 정상인지 확인

---

## 6. 52일차 결과

52일차에서는 51일차에 구축한 파티 상태 영속화 기반을
현재 기획의 사망·정신력 규칙에 맞게 정리했다.

즉시 부활 경로를 제거하고 탐사 초기화 과정에서도 저장 상태가 유지되도록 변경했으며,
런타임 한글 UI가 Git LFS 폰트 상태에 직접 의존하지 않도록 TMP 폰트 fallback 구조를 보강했다.

다음 개발 단계에서는 거점에서 사망 아군을 등록하고
일정 탐사 진행 동안 출전을 제한하는 회복 설비 구조를 구현한다.
