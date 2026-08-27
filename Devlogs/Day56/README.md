# 56일차 개발일지

## 개발 목표

Prototype v0.1 절차 탐사 맵에 방 콘텐츠 역할을 추가하고 일반 전투·엘리트·이벤트·보물·휴식·상점·보스 방을 기존 탐사 런타임과 연결한다.

## 최신 커밋 검토

검토 기준 커밋:

```text
ff6d33c362fcd8ab61bef4fd2c15979e29574fc1
```

검토 당시 커밋 제목:

```text
a
```

56일차 변경 파일과 기존 탐사·상점·휴식 API 연결을 확인했다.

GitHub 저장소에는 현재 자동 CI 상태 검사가 등록되어 있지 않으므로 Unity Editor 컴파일과 Play Mode 전체 동작은 로컬에서 최종 확인해야 한다.

### 검토 중 발견한 진행 차단 문제

56일차에서는 매 층의 최장거리 계단 방을 `Boss` 역할로 고정하고 보스 조우를 배치한다.

기존 `ExplorationSessionManager`는 `BattleType.Boss` 승리 시 즉시 탐사 전체 성공을 처리한다.

이 두 규칙을 그대로 함께 사용하면 다음 문제가 발생한다.

```text
1층 최장거리 보스 방 진입
↓
보스 승리
↓
탐사 전체 성공 처리
↓
IsExplorationCompleted = true
↓
계단의 다음 층 이동 차단
```

기존 탐사 구조는 5층 간격 보스 테스트 규칙을 사용하고 있었으므로 56일차에서는 보스 승리의 탐사 완료 판정을 5층 간격으로 제한하도록 보정한다.

보정 후:

```text
1~4층 보스 승리
→ 현재 층 보스 조우 클리어
→ 계단 활성화
→ 다음 층 이동 가능

5층 보스 승리
→ 탐사 성공 처리
```

## 주요 구현 내용

### 1. 방 콘텐츠 역할 구조 추가

`ExplorationMapCell`에 기존 셀 구조 종류와 별도로 `ExplorationRoomType`을 추가했다.

현재 역할:

```text
Normal
Elite
Event
Treasure
Rest
Shop
Boss
```

`ExplorationCellType`은 시작 방·일반 방·계단 방과 같은 구조 정보를 유지하고, `ExplorationRoomType`은 실제 방 콘텐츠를 담당한다.

### 2. Seed 기반 방 역할 생성

방 콘텐츠 역할은 맵 Seed를 기반으로 결정한다.

같은 Seed를 사용하면 같은 좌표에 같은 방 역할이 생성된다.

현재 Prototype 가중치:

| 방 역할 | 가중치 |
|---|---:|
| 일반 전투 | 45% |
| 엘리트 전투 | 15% |
| 이벤트 | 20% |
| 보물 | 10% |
| 휴식 | 5% |
| 상점 | 5% |

시작 방은 특수 콘텐츠 배치에서 제외한다.

### 3. 상점·휴식 방 생성 제한

한 층에서 다음 제한을 적용한다.

```text
상점 방 최대 1개
휴식 방 최대 1개
```

이미 해당 방이 생성된 뒤 같은 가중치가 다시 선택되면 일반 전투 방으로 대체한다.

### 4. 최장거리 보스 방

시작점에서 가장 먼 계단 좌표를 해당 층의 보스 방으로 지정한다.

보스 방에는 기존 `Boss` EncounterData를 사용한다.

보스 EncounterData가 없을 경우 기존 유효 조우 데이터를 Fallback으로 사용한다.

### 5. 기존 전투·이벤트 생성 흐름 재사용

`ExplorationRoomRoleRuntime`을 추가해 기존 `ExplorationMapRuntime`의 조우·이벤트 생성 흐름을 재사용한다.

현재 Prototype에서는 대형 기존 런타임 파일을 직접 크게 수정하지 않도록 Reflection 기반 연결 레이어를 사용한다.

현재 역할별 처리:

```text
Normal
→ Normal Encounter

Elite
→ Elite Encounter

Event
→ ExplorationEventData

Treasure
→ 보물 상호작용

Rest
→ 55일차 휴식 시스템

Shop
→ 54일차 상점 시스템

Boss
→ Boss Encounter
```

### 6. 방 역할 자동 연결

`ExplorationRoomRoleBootstrap`을 추가하고 `ExplorationSceneRuntimeRouter`에서 자동 등록한다.

탐사 Scene 실행 시:

```text
ExplorationRuntime 준비
↓
ExplorationMapRuntime 생성
↓
ExplorationRoomRoleBootstrap 연결
↓
ExplorationRoomRoleRuntime 추가
↓
현재 Seed 방 역할 콘텐츠 재배치
```

F9 재생성과 층 이동으로 Seed 또는 층 번호가 바뀌면 역할 콘텐츠를 다시 적용한다.

### 7. Prototype 보물 방

보물 방은 현재 임시 보상으로 다음 값을 사용한다.

```text
Gold +100
```

획득한 보물 방은 `ExplorationSessionManager.ResolvedEventIds` 흐름을 재사용해 같은 탐사 런에서 중복 획득할 수 없도록 처리한다.

정식 보물 테이블과 랜덤 보상은 이후 콘텐츠 데이터 확장 단계에서 교체한다.

### 8. 실제 휴식 방 연결

55일차의 우측 상단 테스트 버튼뿐 아니라 실제 생성된 `Rest` 방에 접촉해 휴식 UI를 열 수 있도록 연결했다.

현재 방이 퇴색 위험 방인지 `ExplorationMapRuntime.TryGetHazardAt()`으로 조회한다.

적용 규칙:

```text
일반 휴식 방
→ 최대 HP 기준 25% 회복

고위험 휴식 방
→ 최대 HP 기준 15% 회복

공통
→ 정신력 +15
→ 카드 1장 강화
```

실제 방에서 열린 경우 위험도 수동 전환과 테스트용 사용 상태 초기화 버튼은 차단한다.

### 9. 실제 상점 방 연결

생성된 `Shop` 방에 접촉하면 기존 54일차 `ShopPrototypeView`의 상점 열기 흐름을 재사용한다.

기존 상점 상품·Gold·회차 덱·유물·포션·카드 제거 시스템은 그대로 유지한다.

### 10. 특수 방 Prototype 월드 표시

정식 방 전용 에셋이 아직 없으므로 현재는 색상 사각형과 문자로 구분한다.

```text
T
→ Treasure

R
→ Rest

S
→ Shop
```

정식 방 프리팹과 환경 아트가 확정되면 해당 임시 표시를 교체한다.

### 11. 보스 클리어 전 계단 이동 차단

`ExplorationFloorStairs`에서 현재 계단 방의 보스 조우 존재 여부를 확인한다.

```text
보스 조우 존재
→ 계단 이동 차단

보스 조우 클리어
→ 계단 이동 허용
```

단, 5층 간격 보스는 기존 탐사 성공 조건에 따라 탐사 전체 성공으로 처리한다.

### 12. 보스 진행 충돌 보정

최신 커밋 검토에서 발견한 1층 탐사 즉시 종료 문제를 보정한다.

`ExplorationSessionManager`의 보스 승리 처리에서 단순히 `BattleType.Boss` 여부만 확인하지 않고 현재 층이 5층 간격인지 함께 확인한다.

최종 판정:

```text
Boss 승리
+
CurrentFloor % 5 == 0
↓
탐사 성공
```

그 외 층의 보스 승리는 현재 층 조우 클리어로만 처리한다.

## 생성 및 수정 파일

### 수정

- `Assets/_ProjectC/Scripts/Exploration/ExplorationFloorStairs.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapCell.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapGenerator.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSceneRuntimeRouter.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`
- `Assets/_ProjectC/Scripts/Rest/RestRoomPrototypeView.cs`

### 생성

- `Assets/_ProjectC/Scripts/Exploration/ExplorationRoomRoleBootstrap.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationRoomRoleRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSpecialRoomView.cs`
- `Assets/_ProjectC/Tests/Editor/ExplorationRoomRoleTests.cs`

## 테스트 코드

`ExplorationRoomRoleTests`에서 다음 규칙을 검증한다.

- 동일 Seed에서 동일 방 역할 생성
- 시작 방 특수 역할 제외
- 최장거리 계단 방 Boss 역할 지정
- 상점 방 최대 1개
- 휴식 방 최대 1개
- 45 / 15 / 20 / 10 / 5 / 5 가중치 경계

## 현재 Prototype 임시 처리

- 보물 보상은 임시로 Gold +100을 사용한다.
- 특수 방 표시는 정식 에셋 대신 색상 사각형과 `T / R / S` 문자를 사용한다.
- `ExplorationRoomRoleRuntime`은 기존 대형 `ExplorationMapRuntime` 수정량을 줄이기 위해 Reflection으로 기존 조우·이벤트 생성 메서드를 재사용한다.
- 방 역할 가중치는 Prototype 초기값이며 실제 플레이 테스트 후 조정할 수 있다.
- 보스 탐사 완료 판정은 기존 5층 간격 테스트 규칙과 맞춰 임시 연결한다.
- GitHub Actions 기반 Unity 자동 빌드·테스트는 아직 구성되어 있지 않다.

## Unity에서 확인할 항목

1. Unity Console 컴파일 오류가 없는지 확인한다.
2. `30_Exploration` Scene을 실행한다.
3. F9 재생성 시 방 역할이 새 Seed에 따라 변경되는지 확인한다.
4. 같은 Seed 복원 시 같은 역할 배치가 유지되는지 확인한다.
5. 시작 방에 특수 방이 배치되지 않는지 확인한다.
6. 일반·엘리트·이벤트·보물·휴식·상점 방이 역할에 맞게 생성되는지 확인한다.
7. 상점과 휴식이 한 층에 각각 최대 1개인지 확인한다.
8. `T` 접촉 시 Gold +100이 한 번만 지급되는지 확인한다.
9. `R` 접촉 시 실제 휴식 UI가 열리는지 확인한다.
10. 위험 방의 휴식에서 HP 15% 규칙이 적용되는지 확인한다.
11. 일반 방의 휴식에서 HP 25% 규칙이 적용되는지 확인한다.
12. `S` 접촉 시 기존 상점 UI가 열리는지 확인한다.
13. 1층 보스 승리 후 탐사 전체가 종료되지 않는지 확인한다.
14. 1층 보스 승리 후 계단으로 2층 이동 가능한지 확인한다.
15. 2~4층에서도 보스 승리 후 다음 층 이동 가능한지 확인한다.
16. 5층 보스 승리 시 탐사 성공 처리가 발생하는지 확인한다.
17. Unity Test Runner의 EditMode에서 `ExplorationRoomRoleTests`를 실행한다.

## 다음 개발 연결

57일차에서는 51~56일차에 구현한 파티 영속 상태, 사망·회복, 상점, 휴식, 특수 방 생성 규칙을 하나의 탐사 루프로 통합 테스트한다.

특히 다음 항목을 우선 확인한다.

```text
전투
→ 탐사 복귀
→ 특수 방
→ 회복·상점
→ 다음 층
→ 보스
→ 탐사 성공
```

또한 Prototype Reflection 연결부를 정식 공개 API 구조로 정리할지 검토한다.
