# 57일차 개발일지

## 개발 목표

Prototype v0.1의 51~56일차 탐사 시스템을 확정된 10층 진행 규칙에 맞춰 통합하고, 임시 Reflection 연결을 정식 API로 교체한다.

또한 특수 방, 휴식, 보물, 상점, 카드 강화 시스템을 실제 탐사 흐름에서 사용할 수 있도록 정리한다.

## 최신 커밋 검토

검토 기준 커밋:

```text
d1ed2f452c8744803118428d6d89be8e8ca72bff
```

검토 당시 커밋 제목:

```text
a
```

직전 56일차 커밋 `2a520c06f0fa4139cc1b386dfdc44fb7c29386bb`보다 1개 커밋 앞선 상태이며, 57일차 소스 변경이 반영되어 있다.

소스 변경과 기존 호출부를 대조한 결과 이번 일차 개발을 막는 명확한 구조 충돌은 확인되지 않았다.

다만 GitHub에는 자동 CI 상태 검사가 등록되어 있지 않으므로 Unity Editor 컴파일, EditMode Test Runner, 실제 Play Mode 전체 탐사 진행은 로컬에서 최종 확인해야 한다.

## 주요 구현 내용

### 1. 10층 탐사 진행 규칙 확정

`ExplorationFloorRules`를 추가해 탐사 층 규칙을 한곳에서 관리한다.

현재 규칙:

```text
총 탐사 층수
→ 10층

1~4F
→ 마지막 진행 방 Elite

5F
→ 중간 Boss

6~9F
→ 마지막 진행 방 Elite

10F
→ 최종 Boss
```

`IsBossFloor()`과 `IsFinalBossFloor()`을 분리해 중간 보스와 최종 보스를 구분한다.

### 2. 5층 중간 보스와 10층 최종 보스 분리

기존에는 `BattleType.Boss` 승리만으로 탐사 성공 처리가 발생했다.

57일차에서는 현재 층이 10층인지 함께 확인한다.

```text
5F Boss 승리
→ 중간 보스 클리어
→ 탐사 계속
→ 6F 진행

10F Boss 승리
→ 최종 보스 클리어
→ 탐사 성공
```

이에 따라 5층 보스 처치 후에도 탐사가 종료되지 않는다.

### 3. 일반 층 마지막 Elite 관문 적용

기존 56일차에서는 계단 방을 모든 층에서 Boss 방으로 사용했다.

57일차에서는 현재 층 정보를 맵 생성기에 전달하고 마지막 진행 방을 층에 따라 결정한다.

```text
1~4F / 6~9F
→ ExplorationRoomType.Elite

5F / 10F
→ ExplorationRoomType.Boss
```

계단은 마지막 진행 방의 Elite 또는 Boss 조우가 남아 있으면 사용할 수 없다.

### 4. Rest와 Shop 한 개 생성 보장

확정 기획에 따라 한 층에 다음 특수 방이 반드시 생성되도록 변경한다.

```text
Rest
→ 정확히 1개

Shop
→ 정확히 1개
```

시작 방과 마지막 진행 방을 제외한 후보를 Seed 기반으로 섞은 뒤 Rest와 Shop을 먼저 예약한다.

나머지 방은 기존 가중치 기반으로 배정한다.

### 5. Event와 Treasure 생성 제한

한 층의 특수 방 최대 개수를 확정 규칙에 맞게 제한한다.

```text
Event
→ 최대 3개

Treasure
→ 최대 1개
```

최대 수를 초과한 추첨 결과는 일반 방으로 대체한다.

### 6. ExplorationMapRuntime 정식 API 공개

56일차의 `ExplorationRoomRoleRuntime`은 Reflection을 사용해 `ExplorationMapRuntime`의 private 메서드를 호출했다.

57일차에서는 다음 기능을 정식 공개 API로 연결한다.

```text
CreateEncounterObject()
CreateEventObject()
ClearEncounterObjects()
ClearEventObjects()
```

이를 통해 `System.Reflection`, `BindingFlags`, `MethodInfo` 기반 임시 연결을 제거했다.

### 7. ExplorationRoomRoleRuntime Reflection 제거

방 역할 런타임은 이제 `ExplorationMapRuntime`의 공개 API를 직접 사용한다.

```text
방 역할 적용
↓
기존 조우·이벤트 정리
↓
RoomType 확인
↓
정식 API로 Encounter / Event 생성
```

Seed와 현재 층을 함께 사용해 동일 층 콘텐츠 선택을 재현한다.

### 8. 보물 방 랜덤 보상 시스템 추가

기존 `Gold +100` 고정 보상을 제거하고 `ExplorationTreasureRewardService`를 추가했다.

현재 Prototype 보상 종류:

```text
Gold
Card
Relic
Potion
Resource
CardUpgrade
```

현재 가중치:

| 보상 | 확률 |
|---|---:|
| Gold | 20% |
| Card | 15% |
| Relic | 30% |
| Potion | 10% |
| Resource | 15% |
| CardUpgrade | 10% |

유물을 가장 높은 가중치의 핵심 보상으로 사용한다.

선택된 종류의 보상을 지급할 수 없는 경우 다른 보상 종류를 순차적으로 시도하고, 최종 Fallback으로 Gold를 지급한다.

보물 방은 보상 지급 후 사용 완료 상태로 기록되어 같은 런에서 다시 사용할 수 없다.

### 9. 휴식 방 위험도 Lv1~3 적용

기존 일반/고위험 2단계 휴식 규칙을 실제 퇴색 위험도에 맞춰 확장했다.

현재 Prototype HP 회복률:

| 위험도 | 최대 HP 기준 회복 |
|---|---:|
| 안전 | 25% |
| 퇴색 Lv1 | 20% |
| 퇴색 Lv2 | 15% |
| 퇴색 Lv3 | 10% |

공통 효과:

```text
정신력 +15
카드 1장 강화
사망자 부활 불가
```

기존 bool 기반 고위험 호출은 호환을 위해 Lv2로 변환한다.

### 10. 휴식 파티 상태 Reflection 제거

`RestRoomRunManager`에서 `BattleResultManager`의 private HP·정신력 Dictionary를 Reflection으로 읽던 구조를 제거했다.

`BattleResultManager.TryApplyRestRecovery()` 공개 API를 추가해 생존 파티원의 영속 HP와 정신력을 직접 갱신한다.

회복 적용 후 `PartyStateChanged`를 발생시켜 탐사 파티 HUD도 갱신한다.

### 11. 카드별 강화 단계 기반 추가

기존 카드 강화는 모든 카드가 최대 1강이며 효과값을 +25% 하는 Prototype 구조였다.

57일차에서는 `CardUpgradeProfileData`를 추가해 카드마다 별도의 강화 단계를 가질 수 있도록 기반을 확장했다.

강화 단계별 설정 가능 항목:

```text
효과 수치 증가율
정신력 변화 증가율
AP 비용 증감
상태 이상 지속 횟수 증감
상태 이상 최대 중첩 증감
```

카드 전용 프로필이 없는 경우 기존 Prototype 호환을 위해 최대 1강 / 효과 +25% 규칙을 유지한다.

### 12. 전투 CardInstance 강화 데이터 연동

`CardInstance`가 `CardUpgradeProfileCatalog`를 통해 현재 강화 단계에 따른 실제 전투 값을 계산하도록 변경했다.

연동 항목:

```text
DisplayName
AP Cost
Effect Value
Mental Change
Status Duration
Status Maximum Stacks
```

강화 카드 이름은 `카드명 +1`, `카드명 +2`처럼 현재 단계를 표시한다.

### 13. 상점 방 최초 1회 방문 처리

`ShopRunManager.BeginRoomVisit()`을 추가해 새 상점 방에 진입할 때 해당 방 전용 판매 상태를 준비한다.

상점 방을 처음 열면 해당 방의 Runtime ID를 사용 완료 상태로 기록하고 월드 표시를 제거한다.

따라서 같은 상점 방을 다시 방문할 수 없다.

상점 내부에서는 Gold가 허용하는 동안 기존 카드 제거 기능을 사용할 수 있다.

## 생성 및 수정 파일

### 생성

- `Assets/_ProjectC/Scripts/Exploration/ExplorationFloorRules.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationFloorRules.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationTreasureRewardService.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationTreasureRewardService.cs.meta`
- `Assets/_ProjectC/Scripts/Shop/CardUpgradeProfileData.cs`
- `Assets/_ProjectC/Scripts/Shop/CardUpgradeProfileData.cs.meta`
- `Assets/_ProjectC/Tests/Editor/RestRoomHazardLevelTests.cs`
- `Assets/_ProjectC/Tests/Editor/RestRoomHazardLevelTests.cs.meta`

### 수정

- `Assets/_ProjectC/Scripts/Battle/BattleResultManager.cs`
- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationFloorStairs.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapGenerator.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationRoomRoleRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSpecialRoomView.cs`
- `Assets/_ProjectC/Scripts/Rest/RestRoomPrototypeView.cs`
- `Assets/_ProjectC/Scripts/Rest/RestRoomRecoveryService.cs`
- `Assets/_ProjectC/Scripts/Rest/RestRoomRunManager.cs`
- `Assets/_ProjectC/Scripts/Shop/RunDeckCardEntry.cs`
- `Assets/_ProjectC/Scripts/Shop/ShopRunManager.cs`
- `Assets/_ProjectC/Tests/Editor/ExplorationRoomRoleTests.cs`
- `Project-C.slnx`

## 테스트 코드

### ExplorationRoomRoleTests

다음 규칙을 테스트한다.

- 동일 Seed와 동일 층에서 같은 방 역할 생성
- 시작 방 안전 역할 유지
- 1~4F / 6~9F 마지막 방 Elite
- 5F / 10F 마지막 방 Boss
- Rest 정확히 1개
- Shop 정확히 1개
- Event 최대 3개
- Treasure 최대 1개
- 5F는 최종 Boss가 아님
- 10F만 최종 Boss
- 방 역할 기본 가중치 경계

### RestRoomHazardLevelTests

다음 위험도별 휴식 규칙을 테스트한다.

- 안전 HP 25%
- 퇴색 Lv1 HP 20%
- 퇴색 Lv2 HP 15%
- 퇴색 Lv3 HP 10%
- 최대 HP 초과 방지

### 기존 RestRoomPrototypeTests 호환

기존 bool 기반 휴식 회복 메서드를 유지해 55일차 테스트 호출과 호환한다.

기존 카드 +25% 계산 메서드도 테스트 호환용으로 유지한다.

## 현재 Prototype 임시 처리

- Treasure 보상 가중치와 Gold/Resource 지급량은 Prototype 테스트값이다.
- 카드 강화 프로필이 없는 카드는 기존 최대 1강 / +25% 규칙을 Fallback으로 사용한다.
- 정식 `CardUpgradeProfileData` 에셋은 카드별 밸런스 작업 시 추가해야 한다.
- 특수 방 월드 표시는 아직 임시 색상 사각형과 문자 표시를 사용한다.
- 보물 보상의 카드·유물·포션 후보는 현재 상점 카탈로그 데이터를 재사용한다.
- Unity 자동 CI가 구성되어 있지 않아 실제 Unity 컴파일과 Play Mode 결과는 저장소 상태만으로 확인할 수 없다.

## Unity에서 확인할 항목

1. Unity Console에 C# 컴파일 오류가 없는지 확인한다.
2. Test Runner의 EditMode에서 전체 Editor 테스트를 실행한다.
3. 1~4층 마지막 진행 방이 Elite인지 확인한다.
4. 일반 층 Elite 처치 전 계단을 사용할 수 없는지 확인한다.
5. Elite 처치 후 다음 층으로 이동 가능한지 확인한다.
6. 5층 마지막 진행 방이 Boss인지 확인한다.
7. 5층 Boss 승리 후 탐사가 종료되지 않는지 확인한다.
8. 5층 Boss 처치 후 6층으로 진행 가능한지 확인한다.
9. 6~9층 마지막 진행 방이 Elite인지 확인한다.
10. 10층 마지막 진행 방이 Boss인지 확인한다.
11. 10층 Boss 처치 후 탐사 성공 처리되는지 확인한다.
12. 각 기본 14방 층에 Rest와 Shop이 각각 정확히 1개인지 확인한다.
13. Event가 한 층에 3개를 초과하지 않는지 확인한다.
14. Treasure가 한 층에 1개를 초과하지 않는지 확인한다.
15. Treasure 획득 시 랜덤 보상 한 종류가 지급되고 방이 소모되는지 확인한다.
16. Rest 방의 퇴색 Lv1/2/3에 따라 HP 20/15/10%가 적용되는지 확인한다.
17. 휴식으로 사망 캐릭터가 부활하지 않는지 확인한다.
18. 상점 방을 한 번 연 뒤 같은 방에 재진입할 수 없는지 확인한다.
19. 강화 카드가 실제 전투에서 강화 단계에 맞는 수치를 사용하는지 확인한다.

## 다음 개발 연결

58일차에서는 이번 10층 탐사 통합 구조를 기반으로 출전 파티 편성과 사망·회복 상태에 따른 출전 제한을 실제 탐사 시작 흐름에 연결한다.

우선 확인할 흐름:

```text
기지 파티 편성
↓
사망·회복 상태 확인
↓
출전 가능 캐릭터 제한
↓
탐사 파티 확정
↓
10층 탐사 진행
↓
귀환 후 HP·정신력·사망 상태 유지
```

57일차에서 정리한 카드 강화 데이터와 특수 방 보상 구조는 이후 실제 카드별 강화 에셋과 보물 밸런스 데이터를 추가하는 기반으로 사용한다.
