# Project C - 34일차 개발일지

## 개발 주제

**영구 호감도 시스템 및 탐사 성공 보상 기반 구축 · 전투 종료 안정화**

이번 일차에서는 캐릭터의 전투 능력치와 별개로 관리되는 **영구 호감도 시스템**의 기반을 구축했다.

기존 영구 Level / EXP 시스템과 호감도를 서로 독립적으로 관리하도록 분리했으며, 일반 전투 승리만으로 호감도가 증가하지 않도록 구성했다.

또한 적용 과정에서 발견된 탐사 HUD API 참조 오류와 전투 Scene 종료 중 발생하던 `MissingReferenceException`을 수정하여 반복 전투 후 탐사 복귀 흐름을 안정화했다.

---

## 개발 목표

- 영구 Level / EXP와 별개의 호감도 시스템 구축
- Scene 이동 후에도 호감도 유지
- 일반 몬스터 전투 승리와 호감도 획득 분리
- 추후 탐사 성공 시 호감도를 지급할 수 있는 기반 마련
- 탐사 HUD에서 현재 호감도 확인
- 정식 탐사 성공 시스템 구현 전 임시 테스트 기능 추가
- 기존 33일차 탐사 HUD 및 자원 표시 구조 유지
- 전투 종료 후 탐사 복귀 과정의 View 정리 오류 수정

---

## 구현 내용

### 1. CharacterAffinityManager 추가

호감도를 전담 관리하는 `CharacterAffinityManager`를 추가했다.

주요 역할:

- 현재 호감도 저장
- 호감도 증가 처리
- 호감도 초기화
- 호감도 변경 이벤트 제공
- Scene 전환 이후에도 호감도 유지

`DontDestroyOnLoad` 구조를 사용하여 탐사와 전투 Scene을 이동해도 값이 유지된다.

---

### 2. 기존 Level / EXP 시스템과 호감도 분리

33일차에 구현한 영구 Level / EXP 시스템은 그대로 유지했다.

호감도는 별도의 Manager에서 관리한다.

```text
영구 성장
├─ CharacterProgressionManager
│  ├─ Level
│  └─ EXP
│
└─ CharacterAffinityManager
   └─ 호감도
```

호감도 변화는 캐릭터 Level / EXP에 영향을 주지 않는다.

캐릭터 Level 또한 현재 단계에서는 전투 능력치를 직접 증가시키지 않는다.

---

### 3. 일반 전투 승리와 호감도 획득 분리

현재 일반 몬스터 전투 승리 보상은 기존 구조를 유지한다.

```text
일반 전투 승리
→ 캐릭터 EXP
→ Gold
→ 나사
→ 철판
→ 전선
```

호감도는 일반 전투 한 번을 승리했다고 자동으로 증가하지 않는다.

호감도는 이후 정식 탐사 성공 시스템에서 별도로 지급한다.

---

### 4. 탐사 성공용 호감도 지급 기능 준비

`CharacterAffinityManager`에 탐사 성공 시 호출할 수 있는 호감도 지급 기능을 추가했다.

현재는 정식 탐사 성공 조건이 아직 구현되지 않았기 때문에 테스트 입력과 연결되어 있다.

향후 흐름:

```text
탐사 진행
↓
탐사 성공 조건 달성
↓
탐사 종료
↓
호감도 지급
↓
거점 복귀
```

정식 탐사 성공 시스템이 구현되면 임시 테스트 입력 대신 실제 성공 처리에서 같은 기능을 호출한다.

---

### 5. F8 임시 호감도 테스트 추가

`30_Exploration`에서 `F8`을 누르면 탐사 성공을 가정하여 호감도가 +1 증가하도록 했다.

```text
F8 입력
→ 탐사 성공 테스트
→ 호감도 +1
```

이 기능은 개발 확인용 임시 기능이며 추후 정식 탐사 성공 시스템 구현 후 제거한다.

---

### 6. 탐사 HUD에 호감도 표시 추가

기존 33일차 탐사 HUD의 정보를 유지하면서 호감도 표시를 추가했다.

현재 표시 정보:

```text
캐릭터 Level
캐릭터 EXP / 필요 EXP
호감도
Gold
나사
철판
전선
클리어 조우 수
```

예:

```text
캐릭터 Lv.1  EXP 10/20
호감도 1
Gold 50
나사 25  철판 20  전선 15
클리어 1 / 3
```

---

## 적용 중 발견된 문제 및 수정

### 7. ExplorationPrototypeBootstrap API 참조 오류 수정

초기 34일차 적용 파일에서 현재 프로젝트에 존재하지 않는 API 이름을 참조하여 컴파일 오류가 발생했다.

발생한 잘못된 참조:

```text
ExplorationPlayerController.Initialize()
ExplorationSessionManager.ClearedEncounterCount
CharacterProgressionManager.Experience
PlayerResourceManager.RelicShard
PlayerResourceManager.GrailStone
```

현재 실제 프로젝트 구조에 맞게 다음과 같이 수정했다.

```text
ExplorationPlayerController.Initialize()
→ 호출 제거

ClearedEncounterCount
→ ClearedEncounterIds.Count

Experience
→ CurrentExperience

RelicShard / GrailStone
→ Screw / IronPlate / Wire
```

이를 통해 기존 33일차 탐사 구조를 그대로 유지하면서 호감도 기능만 추가되도록 수정했다.

---

### 8. BattleUnitMotionView MissingReferenceException 수정

전투 종료 후 탐사 Scene으로 이동하는 과정에서 다음 오류가 발생했다.

```text
MissingReferenceException:
The object of type 'BattleUnitMotionView'
has been destroyed but you are still trying to access it.
```

원인은 Scene 종료 과정에서 `BattleUnitMotionView`가 먼저 파괴된 뒤에도 움직임 초기화 코드가 해당 컴포넌트를 다시 호출한 것이었다.

기존 흐름:

```text
전투 종료
↓
BattleUnitMotionView 파괴
↓
BattleActionSequenceRunner.OnDisable()
↓
CancelCurrentAction()
↓
ResetActiveMotion()
↓
BattleUnitView.ResetMotion()
↓
이미 파괴된 BattleUnitMotionView.ResetMotion() 호출
↓
MissingReferenceException
```

---

### 9. BattleUnitMotionView 종료 안전성 강화

`BattleUnitMotionView`가 이미 Destroy된 상태인지 확인한 뒤에만 네이티브 Unity 기능을 호출하도록 수정했다.

추가된 방어 처리:

- Destroy된 컴포넌트에서 `StopAllCoroutines()` 호출 방지
- `visualRect`가 이미 제거된 경우 연출 즉시 종료
- 공격 이동 중 대상이 사라진 경우 코루틴 종료
- 피격 흔들림 중 대상이 사라진 경우 종료
- 회복 확대 연출 중 대상이 사라진 경우 종료
- Scene 종료 중 위치와 크기 복원을 안전하게 건너뜀

전투 연출 수치나 속도 자체는 변경하지 않았다.

---

### 10. BattleActionSequenceRunner 종료 처리 보강

`BattleActionSequenceRunner`가 Scene 종료 시 이미 파괴된 `BattleUnitView`를 다시 초기화하지 않도록 수정했다.

기존의 null 조건 연산자 호출 대신 Unity Object의 실제 생존 여부를 검사한다.

```text
파괴된 View
→ ResetMotion 호출하지 않음

살아 있는 View
→ 기존처럼 원위치 복구
```

이를 통해 전투 도중 Scene이 종료되더라도 남아 있는 행동 연출 참조 때문에 오류가 발생하지 않도록 했다.

---

## 현재 34일차 전체 흐름

```text
탐사 시작
↓
캐릭터 영구 Level / EXP 유지
↓
호감도 유지
↓
맵 이동
↓
몬스터 조우
↓
전투 Scene 진입
↓
전투 진행
↓
승리
↓
캐릭터 EXP + 자원 보상
↓
전투 연출 안전 종료
↓
탐사 Scene 복귀
↓
처치한 조우 제거
↓
Level / EXP 유지
↓
호감도 유지
↓
다음 탐사 진행
```

호감도 획득은 아직 일반 전투와 연결하지 않는다.

```text
F8 테스트
→ 탐사 성공을 임시로 가정
→ 호감도 +1
```

---

## 생성 파일

```text
Assets/_ProjectC/Scripts/Progression/CharacterAffinityManager.cs
Assets/_ProjectC/Scripts/Progression/CharacterAffinityManager.cs.meta
```

---

## 수정 파일

```text
Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs
Assets/_ProjectC/Scripts/Battle/BattleUnitMotionView.cs
Assets/_ProjectC/Scripts/Battle/BattleActionSequenceRunner.cs
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

34일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- `30_Exploration` 실행 가능
- WASD / 방향키 이동 정상
- 몬스터 조우 정상
- 전투 Scene 진입 정상
- 전투 승리 후 탐사 복귀 정상
- 탐사 HUD의 Level / EXP 표시 정상
- Gold 표시 정상
- 나사 / 철판 / 전선 표시 정상
- 호감도 표시 정상
- F8 입력 시 호감도 +1
- F8 반복 입력 시 호감도 누적
- 전투 Scene을 이동해도 호감도 유지
- 일반 전투 승리만으로 호감도 증가하지 않음
- 전투 종료 중 `BattleUnitMotionView MissingReferenceException` 미발생
- 반복 전투 후에도 탐사 진행 가능

---

## 다음 개발 방향

다음 35일차에서는 기획서의 **4.3.2 설비 목록**을 기준으로 설비 영구 강화 시스템을 구축한다.

설비 강화는 특정 캐릭터 한 명이 아니라 **모든 캐릭터에게 영구적으로 적용되는 공통 강화 효과**로 구성한다.

또한 기존에 별도 일차로 계획했던 강화 UI까지 35일차에 함께 구현한다.

예정 흐름:

```text
설비 데이터
↓
설비 Level
↓
현재 효과
↓
다음 Level 효과
↓
Gold / 나사 / 철판 / 전선 비용 확인
↓
강화 실행
↓
자원 차감
↓
모든 캐릭터에게 영구 효과 적용
↓
강화 UI 갱신
```

이후에는 절차적 탐사 맵 생성 시스템 구현으로 진행한다.
