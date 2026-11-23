# Project C - 42일차 개발일지

## 작업 주제

탐사 층 진행에 따른 적 전투 난이도 및 클리어 보상 자동 상승 시스템 구축

## 개발 목표

- 현재 탐사 층을 기준으로 적 최대 체력 자동 증가
- 현재 탐사 층을 기준으로 적 공격력 자동 증가
- 기본 공격과 패턴 공격에 동일한 층 공격 배율 적용
- 상태 이상 수치는 층 난이도에서 제외
- 현재 탐사 층을 기준으로 클리어 보상 자동 증가
- EnemyData와 EncounterData의 직렬화 원본값 유지
- 1층을 기존 전투 밸런스 기준값으로 유지
- F9 맵 재생성에서는 같은 층 난이도 유지
- 계단을 통한 다음 층 이동에서 난이도 자동 상승
- 탐사 미니맵 디버그 UI에 현재 HP·ATK·보상 배율 표시
- 신규 난이도 계산기 컴파일 오류 Hotfix 적용

## 기본 난이도 규칙

42일차의 초기 층별 증가율은 다음 값을 사용한다.

```text
적 최대 체력 : 층마다 +12%
적 공격력    : 층마다 +8%
클리어 보상  : 층마다 +5%
```

1층은 기존 원본 데이터의 능력치와 보상을 그대로 사용하는 기준층이다.

예시:

| 층 | 적 HP | 적 공격력 | 보상 |
|---:|---:|---:|---:|
| 1F | x1.00 | x1.00 | x1.00 |
| 2F | x1.12 | x1.08 | x1.05 |
| 3F | x1.24 | x1.16 | x1.10 |
| 4F | x1.36 | x1.24 | x1.15 |
| 5F | x1.48 | x1.32 | x1.20 |

## 구현 내용

### 1. 층 난이도 계산 기능 구축

현재 탐사 층을 기준으로 HP, 공격력, 보상 배율을 계산하는 `ExplorationDifficultyCalculator`를 구현했다.

계산 기준은 다음과 같다.

```text
난이도 단계 = 현재 층 - 1
```

1층은 단계 0이므로 모든 배율이 x1.00이다.

### 2. 현재 탐사 층 조회

기존 `ExplorationSessionManager.CurrentFloor` 값을 사용하여 현재 층을 조회한다.

별도의 층 진행 데이터를 새로 만들지 않고 기존 탐사 세션의 층 정보를 난이도 시스템에 그대로 연결했다.

최소 층은 1층으로 보정한다.

### 3. 적 최대 체력 배율 계산

적 기본 최대 체력은 기존 `EnemyData.MaxHealth` 값을 사용한다.

계산 방식:

```text
최종 HP
=
EnemyData 기본 HP
×
현재 층 HP 배율
```

예를 들어 기본 HP가 50인 적이 3층에 등장한다면:

```text
50 × 1.24
≈ 62
```

의 최대 체력으로 전투에 등장한다.

### 4. 적 현재 체력 동기화

최대 체력만 증가하고 현재 체력이 기존 값으로 남지 않도록 증가한 최대 체력만큼 현재 체력도 함께 회복한다.

따라서 새 적은 다음과 같이 전투를 시작한다.

```text
62 / 62
```

### 5. 소환 적 난이도 적용

전투 시작 시 존재하는 적뿐만 아니라 전투 도중 새로 생성되는 적도 동일한 현재 층 HP 배율을 적용받도록 구성했다.

이미 배율이 적용된 적은 별도의 목록으로 기록하여 매 프레임 중복 적용되지 않도록 했다.

### 6. 기본 공격 층 배율 적용

`BattleEnemyAction`이 생성될 때 행동 종류가 공격이라면 현재 탐사 층의 공격력 배율을 적용한다.

예:

```text
기본 공격 10
3F 공격 배율 x1.16

10 × 1.16
≈ 12
```

### 7. 패턴 공격 층 배율 적용

적의 기본 공격뿐 아니라 순차 패턴 공격도 최종적으로 `BattleEnemyAction.Amount`를 사용하므로 같은 층 공격 배율을 적용한다.

따라서 별도의 패턴별 난이도 코드를 만들지 않고 일반 공격과 패턴 공격의 계산 경로를 통일했다.

### 8. 상태 이상 수치 제외

다음과 같은 상태 이상 계열 행동에는 층 공격 배율을 적용하지 않는다.

```text
상태 이상 적용 수치
중첩 수치
지속 횟수
최대 중첩 수
```

42일차에서는 순수 공격 피해만 강화하여 밸런스 변화 원인을 명확하게 유지한다.

### 9. 클리어 보상 층 배율 적용

`EncounterData`가 가지고 있는 다음 기본 보상에 현재 층 보상 배율을 적용한다.

- 캐릭터 경험치
- Gold
- Screw
- IronPlate
- Wire

예:

```text
기본 Gold 50
3F 보상 배율 x1.10

50 × 1.10
= 55
```

### 10. 원본 EncounterData 값 유지

ScriptableObject에 저장된 기본 보상 수치를 직접 변경하지 않는다.

Inspector에 저장된 값은 기준값으로 유지하고 실제 보상 값을 조회할 때만 현재 층 배율을 계산한다.

따라서 탐사 진행 중 ScriptableObject 데이터가 누적 변경되지 않는다.

### 11. 원본 EnemyData 값 유지

적의 기본 최대 체력과 기본 공격력 역시 `EnemyData`를 직접 수정하지 않는다.

실제 전투 런타임에서만 현재 층에 맞는 최종 수치를 계산한다.

향후 43일차의 일반·엘리트·보스 추가 배율을 연결하기 쉬운 구조를 유지한다.

### 12. 탐사 미니맵 난이도 표시

기존 오른쪽 아래 탐사 디버그 미니맵에 현재 층 난이도 정보를 추가했다.

표시 정보:

```text
HP x현재배율
ATK x현재배율
보상 x현재배율
```

기존의 다음 미니맵 정보도 그대로 유지한다.

```text
S = 시작
E = 조우
▼ = 계단
P = 플레이어
```

### 13. 탐사 안내 HUD 갱신

탐사 화면의 개발용 안내 문구를 42일차 기준으로 변경했다.

현재 다음 내용을 화면에서 확인할 수 있다.

- 1층 기준 난이도
- 층당 HP 증가율
- 층당 공격 증가율
- 층당 보상 증가율
- 계단 이동 시 난이도 상승
- F9 사용 시 현재 층 난이도 유지

### 14. F9와 난이도 분리

F9는 현재 층의 맵 Seed만 변경한다.

따라서 예를 들어 3층에서 F9를 사용해도:

```text
변경 전
3F / HP x1.24

변경 후
3F / HP x1.24
```

처럼 난이도는 유지된다.

층 난이도의 기준은 Seed가 아니라 `CurrentFloor`이다.

### 15. 계단 층 이동 연동

계단을 통해 다음 층으로 진행하면 기존 `CurrentFloor` 값이 증가한다.

따라서 새로운 전투에서는 별도의 추가 처리 없이 새로운 층 배율이 자동 적용된다.

예:

```text
2F
HP x1.12
ATK x1.08
Reward x1.05

↓

3F
HP x1.24
ATK x1.16
Reward x1.10
```

### 16. 전투 복귀 난이도 유지

전투 승리 후 같은 탐사 층으로 돌아오는 경우 현재 층 값이 바뀌지 않으므로 다음 조우에서도 동일한 난이도 배율을 사용한다.

39~41일차에서 구축한 Seed, 맵, 조우 상태 보존 구조와 함께 동작한다.

### 17. 난이도 로그 추가

전투 진입 시 현재 탐사 층과 적용되는 배율을 Console에서 확인할 수 있도록 로그를 추가했다.

예:

```text
[Exploration][Day42] 층 난이도 적용 -
3F / HP x1.24 / ATK x1.16 / Reward x1.10
```

개별 적의 적용된 최대 체력도 별도 로그로 확인할 수 있다.

### 18. CS0103 컴파일 오류 확인

초기 42일차 구현에서는 다음 파일들이 새 `ExplorationDifficultyCalculator`를 참조했지만 Unity가 해당 신규 타입을 찾지 못하는 문제가 발생했다.

```text
BattleEnemyAction.cs
EncounterData.cs
ExplorationMapDebugView.cs
```

발생 오류:

```text
CS0103
The name 'ExplorationDifficultyCalculator' does not exist in the current context
```

### 19. 난이도 구현 통합 Hotfix

컴파일 인식 문제를 제거하기 위해 실제 다음 구현을 `EncounterData.cs` 컴파일 단위 안으로 통합했다.

```text
ExplorationDifficultyCalculator
ExplorationBattleDifficultyRuntime
ExplorationBattleDifficultyBootstrap
```

기존 신규 파일:

```text
ExplorationDifficultyCalculator.cs
ExplorationBattleDifficultyRuntime.cs
```

은 중복 클래스 정의를 만들지 않는 빈 컴파일 단위로 유지했다.

이를 통해 기존 참조 코드는 그대로 유지하면서 `ExplorationDifficultyCalculator` 타입이 항상 함께 컴파일되도록 수정했다.

## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAction.cs`
- `Assets/_ProjectC/Scripts/Exploration/EncounterData.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapDebugView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs`

## 생성 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationBattleDifficultyRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationBattleDifficultyRuntime.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationDifficultyCalculator.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationDifficultyCalculator.cs.meta`

Hotfix 이후 두 신규 `.cs` 파일의 실제 구현은 `EncounterData.cs` 안에 통합되어 있으며 해당 파일들은 중복 정의 방지를 위한 컴파일 단위로 유지한다.

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console에 CS0103 오류가 발생하지 않음
- [ ] 1층 적 최대 체력이 원본 EnemyData와 동일
- [ ] 1층 적 공격력이 원본 값과 동일
- [ ] 1층 보상이 원본 EncounterData와 동일
- [ ] 2층 진입 시 적 최대 체력 증가
- [ ] 2층 진입 시 적 공격 피해 증가
- [ ] 2층 진입 시 클리어 보상 증가
- [ ] 3층 이상에서도 층 증가에 맞춰 배율 누적
- [ ] 기본 공격에 공격 배율 적용
- [ ] 패턴 공격에 공격 배율 적용
- [ ] 상태 이상 수치는 층 배율에서 제외
- [ ] 적 최대 체력 증가 후 현재 체력도 최대치로 시작
- [ ] 전투 중 소환 적도 현재 층 HP 배율 적용
- [ ] 동일 적에게 HP 배율이 중복 적용되지 않음
- [ ] F9 사용 시 현재 층 난이도 유지
- [ ] 계단 사용 시 다음 층 난이도 자동 상승
- [ ] 전투 복귀 후 같은 층 난이도 유지
- [ ] 미니맵에 HP·ATK·보상 배율 정상 표시
- [ ] 기존 S/E/▼/P 미니맵 표시 정상 유지
- [ ] EnemyData ScriptableObject 원본 수치가 변경되지 않음
- [ ] EncounterData ScriptableObject 원본 보상 수치가 변경되지 않음

## 현재 단계의 제한 사항

42일차는 현재 탐사 층에 따른 공통 난이도 배율만 구현한다.

아직 다음 요소는 포함하지 않는다.

```text
일반 조우 추가 배율
엘리트 조우 추가 배율
보스 조우 추가 배율
층별 적 종류 변화
층별 적 수 변화
층별 AI 패턴 변화
```

이러한 조우 등급별 차이는 이후 조우 등급 시스템에서 별도로 처리한다.

현재 난이도 값인 HP +12%, 공격 +8%, 보상 +5%는 초기 테스트용 수치이며 실제 전투 밸런스 테스트를 거쳐 조정할 수 있다.

GitHub 저장소에는 Unity Editor 컴파일 및 Play Mode를 자동 검증하는 CI 상태 검사가 등록되어 있지 않으므로 최종 동작 확인은 로컬 Unity 환경에서 진행한다.

## 완료 결과

42일차를 통해 탐사 층 진행이 실제 전투 난이도와 보상에 연결되었다.

플레이어가 더 깊은 층으로 진행할수록 동일한 적도 더 높은 최대 체력과 공격 피해를 가지며, 위험 증가에 맞춰 경험치와 자원 보상도 함께 증가한다.

난이도 계산은 EnemyData와 EncounterData 원본을 직접 변경하지 않고 런타임에서만 적용되므로 기존 데이터 구조를 유지하면서 이후 조우 등급, 엘리트, 보스 시스템을 추가할 수 있는 기반을 확보했다.

또한 초기 구현에서 발생한 `ExplorationDifficultyCalculator` 참조 오류를 Hotfix로 정리하여 현재 42일차 난이도 계산 흐름을 하나의 안정된 컴파일 단위로 연결했다.
