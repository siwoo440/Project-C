# Project C - 44일차 개발일지

## 작업 주제

탐사 성공 판정 및 호감도 자동 지급 시스템 구축

## 개발 목표

- 개별 조우 승리와 탐사 전체 성공 상태 분리
- Boss 조우 승리를 실제 탐사 성공 조건으로 연결
- 탐사 성공 상태를 ExplorationSessionManager에서 관리
- 탐사 성공 시 CharacterAffinityManager를 통한 호감도 자동 지급
- 동일 탐사에서 성공 보상 중복 지급 방지
- 탐사 성공 시 완료 층과 클리어 조우 수 기록
- 탐사 성공 결과를 HUD에 표시
- 탐사 성공 후 추가 조우 진입 차단
- 탐사 성공 후 다음 층 이동 차단
- 탐사 성공 후 F9 맵 재생성 차단
- 기존 F8 직접 호감도 지급을 실제 탐사 성공 처리 테스트로 교체
- 새 탐사 초기화 시 탐사 성공 상태만 초기화하고 누적 호감도는 유지

## 구현 내용

### 1. 탐사 전체 성공 상태 추가

기존 ExplorationSessionManager는 현재 층, Seed, 활성 조우, 클리어 조우와 마지막 전투 보상을 관리하고 있었지만 탐사 전체가 끝났는지를 나타내는 상태는 없었다.

44일차부터 다음 상태를 추가했다.

```text
IsExplorationCompleted
IsExplorationSuccess
CompletedFloor
CompletedEncounterCount
LastExplorationSuccessAffinity
```

이를 통해 개별 전투 승리와 하나의 탐사 런 전체 성공을 별도의 상태로 관리한다.

### 2. 조우 승리와 탐사 성공 분리

Normal과 Elite 조우 승리는 기존과 동일하게 개별 전투 클리어로 처리한다.

```text
Normal 승리
→ 조우 보상 지급
→ 조우 클리어 기록
→ 탐사 계속

Elite 승리
→ 조우 보상 지급
→ 조우 클리어 기록
→ 탐사 계속
```

Boss 조우 승리만 탐사 전체 성공으로 연결한다.

```text
Boss 승리
→ Boss 조우 보상 지급
→ Boss 클리어 기록
→ 탐사 성공 확정
→ 호감도 지급
```

### 3. Boss 승리 타입 보관

전투 결과 처리 중 activeEncounter를 마지막에 초기화하기 전에 현재 조우의 BattleType을 별도 변수에 저장한다.

```text
BattleType clearedBattleType
```

이를 통해 전투 결과 처리 도중 현재 조우가 Boss인지 안전하게 판정한다.

### 4. Boss 승리 시 탐사 성공 자동 처리

`ResolveBattleResult()`에서 Victory가 발생하면 기존 조우 보상과 클리어 ID 등록을 먼저 처리한다.

그 후 클리어한 조우의 BattleType이 Boss인 경우:

```text
CompleteExplorationSuccess()
```

를 호출한다.

따라서 탐사 성공은 전투 결과와 직접 연결되며 별도의 수동 호감도 입력이 필요하지 않다.

### 5. 탐사 성공 전용 메서드 추가

`ExplorationSessionManager`에 다음 성공 처리 기능을 추가했다.

```text
CompleteExplorationSuccess()
```

이 메서드는 탐사 성공에 필요한 상태 기록과 보상 지급을 한 곳에서 처리한다.

처리 순서:

```text
이미 완료된 탐사인지 확인
↓
탐사 완료 상태 활성화
↓
탐사 성공 상태 활성화
↓
완료 층 기록
↓
클리어 조우 수 기록
↓
호감도 보상량 기록
↓
CharacterAffinityManager 호출
↓
호감도 지급
```

### 6. 탐사 성공 호감도 보상

현재 초기 테스트값은 다음과 같다.

```text
탐사 성공
→ 호감도 +1
```

보상 값은 `ExplorationSuccessAffinityReward` 상수로 관리한다.

현재는 기능 검증을 위한 기본값이며 이후 실제 탐사 난이도나 성공 조건에 따라 조정할 수 있다.

### 7. 기존 CharacterAffinityManager 재사용

새로운 호감도 시스템을 별도로 만들지 않고 기존 `CharacterAffinityManager`를 그대로 사용한다.

탐사 성공 시:

```text
CharacterAffinityManager.EnsureInstance()
↓
GrantExplorationSuccessAffinity(1)
```

순서로 기존 호감도 누적 시스템에 연결한다.

### 8. 성공 보상 중복 지급 방지

`CompleteExplorationSuccess()`는 이미 탐사가 완료된 경우 즉시 `false`를 반환한다.

따라서 동일 탐사에서 성공 처리가 여러 번 호출되어도:

```text
호감도 +1
호감도 +1
호감도 +1
```

처럼 중복 지급되지 않는다.

정상 흐름:

```text
첫 성공 처리
→ 호감도 +1

두 번째 성공 처리
→ 무시
```

### 9. 완료 층 기록

탐사가 성공한 순간의 `CurrentFloor`를 `CompletedFloor`에 기록한다.

예:

```text
5F Boss 승리
→ CompletedFloor = 5
```

이 값은 탐사 성공 결과 HUD와 이후 정산 시스템에서 사용할 수 있다.

### 10. 완료 시점 클리어 조우 수 기록

성공 직전에 Boss 런타임 조우 ID까지 `ClearedEncounterIds`에 추가한 뒤 성공 처리를 실행한다.

따라서 `CompletedEncounterCount`에는 Boss를 포함하여 성공 시점까지 실제로 클리어한 조우 개수가 기록된다.

### 11. 탐사 성공 후 새 조우 진입 차단

`BeginEncounter()`에 탐사 완료 상태 검사를 추가했다.

```text
IsExplorationCompleted == true
→ BeginEncounter 실패
```

따라서 Boss를 쓰러뜨린 후 필드에 남아 있는 Normal이나 Elite 조우와 접촉해도 새로운 전투가 시작되지 않는다.

### 12. 탐사 성공 후 다음 층 이동 차단

`ExplorationMapRuntime.TryDescendFloor()`에서 현재 탐사가 완료됐는지 확인한다.

완료 상태라면 계단 접촉을 무시한다.

`ExplorationSessionManager.AdvanceFloor()`에도 동일한 방어 처리를 추가하여 다른 코드가 직접 층 진행을 요청하더라도 다음 층으로 이동하지 않도록 했다.

예:

```text
5F Boss 승리
↓
탐사 성공
↓
계단 접촉
↓
6F 이동 차단
```

### 13. 탐사 성공 후 F9 재생성 차단

기존 F9는 현재 층의 Seed를 변경하여 절차 맵을 새로 생성하는 개발용 기능이다.

44일차에서는 탐사가 완료된 이후 F9가 성공한 런의 상태를 변경하지 못하도록 차단했다.

```text
탐사 진행 중
→ F9 사용 가능

탐사 성공 후
→ F9 무시
```

Update 입력 단계와 실제 재생성 메서드 양쪽에서 완료 상태를 검사한다.

### 14. F8 디버그 기능 변경

기존 F8 기능:

```text
F8
→ CharacterAffinityManager에 직접 호감도 +1
```

방식은 제거했다.

44일차부터 F8은:

```text
F8
→ CompleteExplorationSuccess()
```

를 호출한다.

따라서 실제 Boss를 5층까지 진행하지 않아도 탐사 성공 시스템 전체를 빠르게 검증할 수 있다.

### 15. F8 중복 테스트 처리

F8로 탐사 성공 처리를 한 뒤 다시 F8을 눌러도 완료 상태 검사에 의해 추가 호감도가 지급되지 않는다.

Console에는 성공 여부에 따라 각각 다른 디버그 로그가 출력된다.

```text
첫 F8
→ 탐사 성공 처리 실행

두 번째 F8
→ 이미 완료되어 무시
```

### 16. 탐사 진행 HUD에 상태 표시

기존 오른쪽 위 진행 HUD의 현재 탐사 층 표시에 탐사 상태를 함께 출력한다.

진행 중:

```text
탐사 3F · 진행 중
```

성공 후:

```text
탐사 5F · 성공 완료
```

형태로 확인할 수 있다.

### 17. 탐사 성공 결과 HUD 추가

탐사 완료 상태가 성공이면 화면 중앙에 개발용 성공 결과를 표시한다.

예:

```text
탐사 성공

완료 층 5F
클리어 조우 11개
호감도 +1

추가 조우와 다음 층 이동이 종료되었습니다.
```

진행 중에는 해당 텍스트를 빈 문자열로 유지하여 화면에 표시하지 않는다.

### 18. 전투 복귀 시 성공 HUD 연결

Boss 전투가 끝나 탐사 Scene으로 돌아오면 `ExplorationBattleResultReceiver`가 기존 전투 결과를 ExplorationSessionManager에 먼저 반영한다.

이 과정에서 Boss 승리라면 이미 탐사 성공 상태와 호감도 지급이 끝난다.

그 후 탐사 HUD가 생성되므로 성공 결과와 변경된 호감도를 즉시 표시할 수 있다.

### 19. ResetExploration 성공 상태 초기화

새 탐사를 시작할 수 있도록 `ResetExploration()`에 다음 초기화를 추가했다.

```text
IsExplorationCompleted → false
IsExplorationSuccess → false
CompletedFloor → 0
CompletedEncounterCount → 0
LastExplorationSuccessAffinity → 0
```

기존 다음 탐사 런 상태 초기화도 그대로 유지한다.

```text
클리어 조우
현재 조우
복귀 위치
마지막 조우 보상
CurrentFloor
CurrentFloorSeed
```

### 20. 누적 호감도는 ResetExploration에서 유지

`ResetExploration()`은 한 번의 탐사 런 상태만 초기화한다.

CharacterAffinityManager가 관리하는 실제 누적 호감도는 초기화하지 않는다.

따라서 정상적인 누적 흐름은 다음과 같다.

```text
첫 탐사 성공
Affinity 0 → 1

새 탐사 시작
Affinity 1 유지

두 번째 탐사 성공
Affinity 1 → 2
```

### 21. 43일차 조우 시스템 유지

44일차에서는 43일차의 다음 기능을 변경하지 않는다.

```text
Normal / Elite / Boss 구분
층별 난이도 배율
조우 등급별 난이도 배율
조우 등급별 보상 배율
Seed 기반 조우 복원
미니맵 N / E / B
필드 조우 색상
```

이번 일차는 해당 전투·탐사 시스템의 끝에 실제 탐사 성공 상태를 추가하는 작업이다.

## 수정 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs`

## 생성 파일

없음

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console 컴파일 오류 없음
- [ ] Normal 승리 후 호감도 증가하지 않음
- [ ] Normal 승리 후 탐사 계속 가능
- [ ] Elite 승리 후 호감도 증가하지 않음
- [ ] Elite 승리 후 탐사 계속 가능
- [ ] Boss 승리 후 기존 Boss 조우 보상 정상 지급
- [ ] Boss 승리 후 탐사 성공 상태 활성화
- [ ] Boss 승리 후 호감도 정확히 +1
- [ ] 성공 층이 CompletedFloor에 기록
- [ ] Boss를 포함한 클리어 조우 수가 CompletedEncounterCount에 기록
- [ ] 성공 결과 HUD 정상 표시
- [ ] 오른쪽 위 HUD에 성공 완료 상태 표시
- [ ] 탐사 성공 후 남은 Normal/Elite 접촉 시 전투 진입하지 않음
- [ ] 탐사 성공 후 계단 접촉 시 다음 층으로 이동하지 않음
- [ ] 탐사 성공 후 F9로 맵을 재생성할 수 없음
- [ ] F8 입력 시 실제 탐사 성공 처리 실행
- [ ] F8을 두 번 눌러도 호감도가 중복 지급되지 않음
- [ ] ResetExploration 후 현재 층이 1F로 초기화
- [ ] ResetExploration 후 탐사 성공 상태 초기화
- [ ] ResetExploration 후 기존 누적 호감도 유지
- [ ] 새 탐사에서 다시 성공하면 호감도가 추가 누적

## 현재 단계의 제한 사항

현재 탐사 성공 조건은 43일차 테스트 구조에 맞춰 Boss 조우 승리로 설정되어 있다.

43일차에서 Boss는 테스트 목적으로 5층 간격으로 등장하므로 현재 기본 테스트 흐름은 5층 Boss 승리 시 탐사 성공이다.

향후 실제 탐사 구조가 확정되면 다음과 같은 별도 성공 조건으로 교체할 수 있다.

```text
최종 층 Boss 승리
특정 목표 달성
목표 아이템 확보 후 탈출
스토리 이벤트 완료
특정 지역 탐사 완료
```

44일차에서는 아직 성공 후 자동 거점 복귀나 정산 Scene 이동을 처리하지 않는다.

현재 성공 후에는 기존 탐사 Scene에 남은 상태에서 추가 전투, 층 이동, 맵 재생성만 차단한다.

거점 복귀와 정산, 시설 및 강화 루프 연결은 이후 단계에서 처리한다.

GitHub 저장소에는 Unity Editor 컴파일 및 Play Mode를 자동 검증하는 CI 상태 검사가 등록되어 있지 않으므로 실제 실행 확인은 로컬 Unity 환경에서 진행한다.

## 완료 결과

44일차를 통해 Project C의 탐사는 단순히 여러 층과 조우를 반복하는 구조에서 하나의 시작과 완료 상태를 가진 탐사 런으로 확장되었다.

Normal과 Elite는 탐사 과정의 개별 전투로 유지되고 Boss 승리가 탐사 전체 성공으로 연결된다.

탐사 성공 시 기존 호감도 시스템에 자동으로 보상이 지급되며 완료 층과 클리어 조우 수도 함께 기록된다.

또한 성공 이후 새로운 조우, 다음 층 이동, F9 맵 재생성을 차단하고 결과 HUD를 표시함으로써 탐사 런의 종료 상태를 명확하게 만들었다.

이제 다음 단계에서는 이 탐사 성공 결과를 거점 복귀, 정산, 시설 및 강화 시스템과 연결할 수 있다.
