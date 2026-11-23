# 58일차 개발일지

## 개발 목표

Prototype v0.1의 탐사 파티 상태 유지 시스템을 실제 출전 제한 규칙과 연결한다.

사망하거나 회복 중인 캐릭터는 신규 편성에서 선택할 수 없도록 상태를 표준화하고, 탐사 도중 다음 전투에 진입할 때는 실제 출전 가능한 생존 캐릭터와 해당 캐릭터 소유 카드만 전투에 참여하도록 구성한다.

또한 저장된 파티 상태 기준 전멸을 탐사 실패와 연결해 사망·회복·전투 출전 흐름을 하나의 규칙으로 통합한다.

## 최신 커밋 검토

검토 기준 커밋:

```text
93a38cb885d2a3e0b8c4cd12313cb55f11a8a86a
```

검토 당시 커밋 제목:

```text
58
```

직전 57일차 커밋 `3591167e6b4723c2606f125fb412412bce21b0f4` 이후 Day58 Part 1~3에 해당하는 출전 검증, 편성 UI 상태, 실제 전투 명단 및 카드 필터 코드가 반영되어 있다.

변경 파일과 기존 `CharacterRecoveryManager`, `BattleResultManager`, `PartyData`, `RunDeckManager`, 탐사 전투 결과 복귀 흐름을 대조한 결과 이번 일차 개발을 막는 명확한 소스 구조 충돌은 확인되지 않았다.

다만 현재 GitHub 커밋에는 자동 CI 상태 검사가 등록되어 있지 않으므로 Unity Editor C# 컴파일, EditMode Test Runner, 실제 Play Mode 탐사·전투 전환은 로컬 Unity에서 최종 확인해야 한다.

## 주요 구현 내용

### 1. 파티 출전 검증 공통 규칙 추가

`PartyDeploymentValidator`를 추가해 캐릭터 출전 가능 여부를 한곳에서 판정한다.

현재 출전 차단 사유:

```text
InvalidParty
→ 파티 데이터 오류

InvalidLoadout
→ 전투 편성 데이터 오류

DeadMember
→ 사망 캐릭터 포함

RecoveringMember
→ 회복 중 캐릭터 포함

UnavailableMember
→ 기타 출전 불가 상태
```

`IPartyDeploymentStatusProvider`를 통해 실제 회복 시스템과 테스트용 상태 공급자를 같은 검증 코드에서 사용할 수 있도록 구성했다.

### 2. 기존 회복 시스템과 출전 상태 연결

`CharacterRecoveryDeploymentStatusProvider`가 기존 `CharacterRecoveryManager`의 다음 상태를 출전 검증 규칙으로 전달한다.

```text
IsDead()
IsRecovering()
CanDeploy()
```

이에 따라 저장 HP가 0 이하인 캐릭터와 회복 설비 진행 중인 캐릭터를 동일한 출전 규칙으로 판정할 수 있다.

### 3. BattleLoadoutData 출전 검증 확장

기존 `BattleLoadoutData.IsValidLoadout()`은 파티·덱 데이터 구조 자체를 검증한다.

58일차에서는 여기에 런타임 상태 검증을 별도로 추가했다.

```text
ValidateDeployment()
→ 구조 검증
→ 사망·회복 상태 검증
→ 차단 사유와 대상 캐릭터 반환

IsDeployableLoadout()
→ 현재 편성이 실제 출전 가능한지 bool 반환
```

구조적 유효성과 현재 캐릭터 상태를 분리해 기존 데이터 검증 흐름을 유지한다.

### 4. 탐사 파티 Loadout 출전 상태 노출

`ExplorationPartyLoadoutProvider`에 `DeploymentValidation`과 `CanStartNewExploration`을 추가했다.

탐사 Scene에서 파티를 등록한 뒤 저장 HP와 회복 상태를 포함해 신규 탐사 출전 가능 여부를 계산한다.

현재 Prototype에서는 출전 불가 편성이 확인되면 차단 사유와 캐릭터 이름을 Warning 로그로 남기며, 실제 신규 편성 UI에서는 이 결과를 이용해 선택과 출전 버튼을 차단할 수 있다.

### 5. 파티 편성 UI 상태 모델 추가

`PartyDeploymentViewState`를 추가해 편성 화면에서 사용할 캐릭터 상태를 표준화했다.

표시 상태:

| 상태 | 선택 | 초상화 | 문구 |
|---|---|---|---|
| 출전 가능 | 가능 | 정상 | `출전 가능` |
| 사망 | 불가 | 흐림 | `사망` |
| 회복 중 | 불가 | 흐림 | `회복 중 · N회` |
| 기타 출전 불가 | 불가 | 흐림 | `출전 불가` |

회복 중 캐릭터는 남은 회복 필요 탐사 횟수도 함께 표시한다.

### 6. 재사용 가능한 편성 캐릭터 버튼 추가

`PartyDeploymentMemberButton`을 추가해 향후 로비·출전 편성 화면에서 바로 사용할 수 있는 캐릭터 선택 버튼 기반을 구축했다.

버튼은 다음 요소를 자동 갱신한다.

```text
캐릭터 초상화
캐릭터 이름
출전 상태 문구
Button.interactable
출전 불가 초상화 흐림
```

`CharacterRecoveryManager.RecoveryStateChanged`와 `BattleResultManager.PartyStateChanged`를 구독해 HP·사망·회복 진행 상태가 변경되면 UI도 갱신한다.

현재 실제 로비 편성 화면 자체는 아직 구축되지 않았으므로, 이후 편성 UI에 이 컴포넌트를 연결하는 단계가 필요하다.

### 7. 전투 Scene 전용 실제 출전 명단 필터 추가

`BattleCombatRosterRuntime`은 현재 Scene이 `40_Battle`인지 판정한다.

`PartyData.Members`는 Scene에 따라 다음처럼 동작한다.

```text
탐사·거점
→ 기존 원본 편성 전체 반환

40_Battle
→ 실제 출전 가능한 캐릭터만 반환
```

따라서 탐사 중 캐릭터가 사망하거나 회복 불가 상태가 되더라도 원본 파티 편성 자체는 유지하면서, 다음 전투에서는 생존·출전 가능한 파티원만 생성할 수 있다.

`ContainsCharacter()`와 `IsValidParty()`는 원본 편성을 기준으로 유지해 ScriptableObject 파티 데이터 자체가 전투 Scene 필터 때문에 변형되지 않도록 했다.

### 8. 실제 전투 카드 소유자 필터 추가

사망 캐릭터만 전투 유닛 생성에서 제외하면 해당 캐릭터가 소유한 카드가 전투 덱에 남을 수 있다.

이를 방지하기 위해 `BattleCombatRosterBuilder`가 실제 출전 명단과 카드 소유자를 같은 규칙으로 필터링한다.

```text
출전 가능 캐릭터
→ 전투 유닛 생성 대상
→ 해당 캐릭터 소유 카드 유지

사망·회복 중·출전 불가 캐릭터
→ 전투 유닛 생성 제외
→ 해당 캐릭터 소유 카드도 제외
```

캐릭터 순서와 기존 회차 덱 카드 순서는 필터 후에도 유지한다.

### 9. RunDeckManager 전투 카드 필터 연동

`RunDeckManager.GetActiveCards()`는 Scene에 따라 반환 대상을 구분한다.

```text
탐사·상점
→ 현재 회차 카드 전체

40_Battle
→ 실제 출전 가능 캐릭터 소유 카드만 반환
```

상점에서 획득·제거·강화한 회차 덱 상태는 그대로 보존하면서 실제 전투에 사용할 카드만 일시적으로 제한한다.

### 10. 저장 상태 기준 파티 전멸 탐사 실패 보강

`ExplorationBattleResultReceiver`가 전투 결과를 탐사에 반영한 뒤 `BattleResultManager.IsActivePartyWiped()`를 추가 확인한다.

저장된 파티 HP 기준으로 전원이 사망했고 탐사가 아직 종료 처리되지 않았다면 탐사 실패를 확정한다.

이를 통해 전투 결과와 영속 파티 상태 사이의 전멸 판정 누락을 방지한다.

## 생성 및 수정 파일

### 생성

- `Assets/_ProjectC/Scripts/Battle/BattleCombatRosterBuilder.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCombatRosterBuilder.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleCombatRosterRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCombatRosterRuntime.cs.meta`
- `Assets/_ProjectC/Scripts/Characters/PartyDeploymentMemberButton.cs`
- `Assets/_ProjectC/Scripts/Characters/PartyDeploymentMemberButton.cs.meta`
- `Assets/_ProjectC/Scripts/Characters/PartyDeploymentValidator.cs`
- `Assets/_ProjectC/Scripts/Characters/PartyDeploymentValidator.cs.meta`
- `Assets/_ProjectC/Scripts/Characters/PartyDeploymentViewState.cs`
- `Assets/_ProjectC/Scripts/Characters/PartyDeploymentViewState.cs.meta`
- `Assets/_ProjectC/Tests/Editor/BattleCombatRosterBuilderTests.cs`
- `Assets/_ProjectC/Tests/Editor/BattleCombatRosterBuilderTests.cs.meta`
- `Assets/_ProjectC/Tests/Editor/PartyDeploymentValidatorTests.cs`
- `Assets/_ProjectC/Tests/Editor/PartyDeploymentValidatorTests.cs.meta`
- `Assets/_ProjectC/Tests/Editor/PartyDeploymentViewStateTests.cs`
- `Assets/_ProjectC/Tests/Editor/PartyDeploymentViewStateTests.cs.meta`

### 수정

- `Assets/_ProjectC/Scripts/Data/BattleLoadoutData.cs`
- `Assets/_ProjectC/Scripts/Data/PartyData.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationBattleResultReceiver.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPartyLoadoutProvider.cs`
- `Assets/_ProjectC/Scripts/Shop/RunDeckManager.cs`

## 테스트 코드

### PartyDeploymentValidatorTests

다음 규칙을 테스트한다.

- 정상 생존 파티 전체 출전 가능
- 사망 캐릭터 포함 시 파티 출전 차단
- 회복 중 상태의 차단 사유 우선 처리
- 기타 출전 불가 상태 Fallback 처리
- 탐사 파티 재등록 시 기존 HP·정신력 유지
- 실제 저장 사망 상태와 회복 상태 차단
- 잘못된 파티를 런타임 상태 검사보다 먼저 차단

### PartyDeploymentViewStateTests

다음 UI 표시 규칙을 테스트한다.

- 정상 캐릭터 선택 가능과 정상 초상화 상태
- 사망 캐릭터 선택 차단과 흐림 표시
- 회복 중 캐릭터의 남은 탐사 횟수 문구
- 기타 출전 불가 캐릭터의 Fallback 상태

### BattleCombatRosterBuilderTests

다음 실제 전투 필터 규칙을 테스트한다.

- 출전 불가 캐릭터 제외 후 기존 파티 순서 유지
- 제외 캐릭터가 소유한 카드 제거
- 파티와 카드가 동일한 출전 판정 규칙 사용
- 전원 출전 불가 시 빈 전투 명단 반환

## 현재 Prototype 임시 처리

- 실제 로비·출전 파티 편성 화면은 아직 없으며 `PartyDeploymentMemberButton`은 이후 UI에 연결할 재사용 컴포넌트다.
- 신규 탐사 출전 검증 결과는 `ExplorationPartyLoadoutProvider`가 제공하지만 최종 출전 버튼 UI와의 직접 연결은 이후 편성 화면 구축 단계에서 처리해야 한다.
- 전투 전용 필터는 Scene 이름 `40_Battle`을 기준으로 활성화된다.
- 회복 필요 탐사 횟수와 회복 완료 HP 규칙은 기존 `CharacterRecoveryManager`의 Prototype 값을 사용한다.
- GitHub 자동 CI가 구성되어 있지 않아 실제 Unity 컴파일과 Test Runner 결과는 저장소 상태만으로 확인할 수 없다.

## Unity에서 확인할 항목

1. Unity Console에 C# 컴파일 오류가 없는지 확인한다.
2. Test Runner의 EditMode에서 `PartyDeploymentValidatorTests`를 실행한다.
3. `PartyDeploymentViewStateTests`를 실행한다.
4. `BattleCombatRosterBuilderTests`를 실행한다.
5. 정상 캐릭터가 편성 UI에서 `출전 가능` 상태로 표시되는지 확인한다.
6. 사망 캐릭터가 `사망` 문구와 함께 선택 불가 상태가 되는지 확인한다.
7. 회복 중 캐릭터가 `회복 중 · N회` 문구와 함께 선택 불가 상태가 되는지 확인한다.
8. 탐사 중 한 캐릭터가 사망한 뒤 다음 전투에서 해당 캐릭터 유닛이 생성되지 않는지 확인한다.
9. 제외된 캐릭터가 소유한 카드가 해당 전투 덱에 생성되지 않는지 확인한다.
10. 생존 캐릭터와 그 소유 카드는 기존 순서를 유지하는지 확인한다.
11. 탐사·상점에서는 원본 파티와 전체 회차 덱 상태가 유지되는지 확인한다.
12. 저장 HP 기준 파티 전멸 시 탐사가 실패로 종료되는지 확인한다.
13. 회복 완료 후 캐릭터가 다시 출전 가능 상태로 변경되는지 확인한다.
14. 전투 종료 후 탐사 복귀 과정에서 HP·정신력·사망 상태가 계속 유지되는지 확인한다.

## 다음 개발 연결

다음 단계에서는 현재 구축한 출전 상태 규칙을 실제 로비·탐사 출발 편성 화면에 연결하는 것이 우선이다.

```text
캐릭터 목록 UI
→ PartyDeploymentMemberButton 연결
→ 사망·회복 상태 실시간 표시
→ 선택 불가 처리
→ 전체 PartyDeploymentValidation 확인
→ 유효한 편성만 신규 탐사 시작
```

이후 실제 편성 교체, 빈 슬롯 처리, 출전 확정 버튼, 캐릭터 상태 상세 정보까지 확장하면 58일차의 런타임 출전 제한 시스템을 플레이어가 직접 조작할 수 있는 편성 흐름으로 완성할 수 있다.
