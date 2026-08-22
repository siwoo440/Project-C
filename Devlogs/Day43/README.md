# Project C - 43일차 개발일지

## 작업 주제

일반·엘리트·보스 조우 등급 시스템 구축 및 층 난이도와의 결합

## 개발 목표

- 기존 `BattleType`에 Elite 등급 추가
- 기존 Boss 직렬화 값 유지
- EncounterData를 Normal / Elite / Boss 세 등급으로 구분
- 절차 탐사 조우를 등급별 데이터 목록에서 선택
- 필드 조우 오브젝트를 등급별 색상으로 표시
- 미니맵에서 N / E / B로 조우 등급 구분
- 42일차 층별 HP·공격·보상 배율에 조우 등급 배율 추가
- Elite와 Boss에 별도 HP·공격·보상 배율 적용
- 전투 진입 시 현재 ActiveEncounter의 BattleType을 기준으로 최종 난이도 계산
- 전투 종료 후 기존 조우 제거·Seed 복원 흐름 유지
- 테스트용 5층 간격 Boss 배치 규칙 추가

## 구현 내용

### 1. BattleType에 Elite 추가

기존 전투 유형:

```text
Normal
Boss
```

구조를 다음과 같이 확장했다.

```text
Normal
Elite
Boss
```

기존 Boss 데이터의 직렬화 값이 `1`이었기 때문에 기존 Asset 호환성을 유지하기 위해 명시적으로 다음 값을 사용한다.

```text
Normal = 0
Elite = 2
Boss = 1
```

이를 통해 기존 Boss EncounterData가 Elite로 잘못 변경되는 문제를 방지한다.

### 2. 테스트 EncounterData 등급 분리

기존 테스트 조우 세 개 중 다음과 같이 등급을 분리했다.

```text
Encounter_Test_A
→ Normal

Encounter_Test_B
→ Elite

Encounter_Test_C
→ Boss
```

`Encounter_Test_B`의 표시 이름도 테스트 엘리트 조우로 변경하고 `battleType`을 Elite 값인 `2`로 설정했다.

`Encounter_Test_C`는 테스트 보스 조우로 변경하고 기존 Boss 값인 `1`을 사용한다.

### 3. 층 난이도와 조우 등급 난이도 결합

42일차의 층 난이도 계산 구조를 유지하면서 조우 등급 배율을 추가했다.

최종 계산 구조:

```text
최종 HP
=
기본 HP
× 층 HP 배율
× 조우 등급 HP 배율

최종 공격력
=
기본 공격
× 층 공격 배율
× 조우 등급 공격 배율

최종 보상
=
기본 보상
× 층 보상 배율
× 조우 등급 보상 배율
```

### 4. Elite 난이도 배율

43일차 초기 테스트값은 다음과 같다.

```text
Elite HP     x1.50
Elite ATK    x1.20
Elite Reward x1.50
```

일반 조우보다 강하지만 Boss보다 낮은 위험도의 중간 조우로 사용한다.

### 5. Boss 난이도 배율

Boss 초기 테스트값은 다음과 같다.

```text
Boss HP     x2.50
Boss ATK    x1.35
Boss Reward x2.50
```

Boss 전용 AI나 페이즈는 이번 일차 범위에 포함하지 않고 전투 등급과 수치 배율만 구축한다.

### 6. 현재 전투 조우 등급 조회

`ExplorationDifficultyCalculator`에 현재 `ExplorationSessionManager.ActiveEncounter`의 `BattleType`을 조회하는 기능을 추가했다.

활성 탐사 조우가 없는 경우에는 안전하게 `Normal`을 반환한다.

이를 통해 기존 일반 전투나 별도 테스트 전투가 등급 정보 부족으로 깨지지 않도록 했다.

### 7. 적 최대 체력 등급 배율 적용

`ExplorationBattleDifficultyRuntime`이 적 최대 체력을 계산할 때 기존 층 정보뿐 아니라 현재 조우의 `BattleType`도 함께 사용하도록 변경했다.

예:

```text
기본 HP 100
3F 층 HP x1.24
Elite HP x1.50

최종 HP
= 186
```

적이 새로 소환되더라도 같은 현재 조우 등급 배율을 적용한다.

### 8. 공격 행동 등급 배율 적용

`BattleEnemyAction`의 공격 수치 계산에서 현재 탐사 층과 현재 조우 등급을 함께 조회하도록 수정했다.

기본 공격과 패턴 공격 모두 같은 최종 계산 흐름을 사용한다.

예:

```text
기본 공격 10
3F ATK x1.16
Elite ATK x1.20

최종 공격 배율
x1.392
```

정수 피해량은 최종 계산 시 반올림 처리한다.

### 9. 상태 이상 수치는 기존 규칙 유지

공격 행동이 아닌 상태 이상 적용 행동은 42일차와 동일하게 조우 등급 공격 배율을 적용하지 않는다.

따라서 다음 값은 이번 일차에서 자동 강화하지 않는다.

```text
상태 이상 중첩
지속 시간
최대 중첩
기타 상태 이상 수치
```

### 10. 보상에 조우 등급 배율 적용

`EncounterData`의 클리어 보상 조회 시 현재 층과 해당 EncounterData 자신의 `battleType`을 함께 사용하도록 변경했다.

적용 대상:

- 캐릭터 경험치
- Gold
- Screw
- IronPlate
- Wire

예:

```text
기본 Gold 50
3F 보상 x1.10
Elite 보상 x1.50

최종 Gold
≈ 83
```

### 11. 보상 계산 메서드 인스턴스화

보상 계산 과정에서 현재 EncounterData의 `battleType`을 직접 사용해야 하므로 `GetScaledReward()`를 static 메서드에서 인스턴스 메서드로 변경했다.

이를 통해 각 EncounterData가 자신의 조우 등급을 기준으로 보상을 계산할 수 있다.

### 12. 절차 조우 데이터 등급별 분리

탐사 맵 생성 시 `Resources/Encounters`의 모든 EncounterData를 한 목록으로만 사용하던 구조를 다음처럼 변경했다.

```text
전체 EncounterData
↓
Normal 목록
Elite 목록
Boss 목록
```

각 목록은 `EncounterId` 기준으로 정렬하여 같은 Seed에서 동일한 결과가 재현되도록 유지한다.

### 13. 조우 등급 배치 계획 생성

현재 층에서 필요한 조우 등급을 먼저 결정하고 그 뒤 해당 등급 목록에서 EncounterData를 선택하도록 변경했다.

기본 조우 수는 기존과 동일하게 3개를 유지한다.

테스트용 기본 구성:

```text
일반 층
Elite 1
Normal 2
```

### 14. 테스트용 Boss Floor 규칙

43일차 기능 테스트를 위해 현재는 다음 임시 규칙을 사용한다.

```text
5F 간격
→ Boss Floor
```

예:

```text
5F
10F
15F
...
```

Boss Floor에서는 기본 3개 조우 중 Boss 1개를 우선 배치한다.

예:

```text
Boss 1
Elite 1
Normal 1
```

이 값은 이후 실제 기획의 Boss 등장 규칙이 확정되면 교체할 테스트 규칙이다.

### 15. 등급 데이터가 없을 때 Fallback 처리

특정 등급의 EncounterData가 없는 경우에도 탐사 조우 생성 전체가 실패하지 않도록 기존 유효 EncounterData 목록을 Fallback으로 사용한다.

따라서 테스트 데이터 일부가 누락되어 있어도 가능한 범위에서 조우를 생성한다.

### 16. 조우 좌표 저장 구조 확장

기존에는 다음 구조로 조우 좌표 존재 여부만 저장했다.

```text
HashSet<Vector2Int>
```

43일차에서는 좌표별 조우 등급까지 필요하므로 다음 구조로 변경했다.

```text
Dictionary<Vector2Int, BattleType>
```

이제 런타임에서 다음 두 정보를 모두 조회할 수 있다.

```text
해당 좌표에 조우가 있는가
해당 조우는 어떤 등급인가
```

### 17. 미니맵 조우 등급 표시

기존 미니맵의 모든 조우 `E` 표시를 다음처럼 세분화했다.

```text
N = Normal
E = Elite
B = Boss
```

기존 표시도 그대로 유지한다.

```text
S = 시작
▼ = 계단
P = 플레이어
```

### 18. 필드 조우 색상 구분

기존에는 조우 생성 순서에 따라 색상이 바뀌었지만 43일차부터는 실제 `BattleType`을 기준으로 색상을 결정한다.

```text
Normal = 주황
Elite  = 보라
Boss   = 빨강
```

따라서 필드에서 접촉하기 전에도 현재 조우의 위험도를 구분할 수 있다.

### 19. Hierarchy 오브젝트 이름 등급 표시

런타임 조우 오브젝트 이름에 현재 BattleType을 포함하도록 변경했다.

예:

```text
Encounter_Normal_F1_...
Encounter_Elite_F1_...
Encounter_Boss_F5_...
```

Play Mode에서 Hierarchy만 확인해도 생성된 조우 등급을 빠르게 확인할 수 있다.

### 20. 전투 난이도 디버그 로그 확장

전투 진입 시 다음 정보를 Console에서 확인할 수 있도록 변경했다.

```text
현재 층
현재 BattleType
층 HP / ATK / Reward 배율
등급 HP / ATK / Reward 배율
최종 HP / ATK / Reward 배율
```

이를 통해 층 스케일링과 조우 등급 스케일링이 중복 또는 누락 없이 곱해지는지 확인할 수 있다.

### 21. 탐사 디버그 미니맵 정보 갱신

탐사 디버그 패널을 43일차 기준으로 변경했다.

현재 표시 정보:

```text
현재 층
Seed
S / N / E / B / ▼ / P 범례
층 난이도 HP / ATK / 보상 배율
Elite HP 배율
Boss HP 배율
현재 조우 개수
Floor / Wall Tile 수
5층 간격 Boss 테스트 규칙
```

### 22. 기존 Seed 복원 구조 유지

같은 Seed를 사용하는 경우 조우 셀 순서와 EncounterData 목록 순서가 결정적으로 유지되도록 구성했다.

따라서 전투 후 탐사로 복귀했을 때 남아 있는 조우는 기존 위치와 등급을 유지한다.

클리어한 조우는 기존 시스템대로 필드와 미니맵에서 제거된다.

## 수정 파일

- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_B.asset`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_C.asset`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAction.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleType.cs`
- `Assets/_ProjectC/Scripts/Exploration/EncounterData.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapDebugView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs`

## 생성 파일

없음

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console 컴파일 오류 없음
- [ ] BattleType에서 Normal / Elite / Boss 사용 가능
- [ ] 기존 Boss 직렬화 값이 Boss로 유지
- [ ] Encounter_Test_A가 Normal로 유지
- [ ] Encounter_Test_B가 Elite로 인식
- [ ] Encounter_Test_C가 Boss로 인식
- [ ] 일반 층에서 Normal 2개와 Elite 1개 생성
- [ ] 5층에서 Normal / Elite / Boss 각각 생성
- [ ] Normal 필드 오브젝트 주황색 표시
- [ ] Elite 필드 오브젝트 보라색 표시
- [ ] Boss 필드 오브젝트 빨간색 표시
- [ ] 미니맵에서 N / E / B 정상 표시
- [ ] 기존 S / ▼ / P 표시 유지
- [ ] Elite HP x1.50 적용
- [ ] Elite ATK x1.20 적용
- [ ] Elite Reward x1.50 적용
- [ ] Boss HP x2.50 적용
- [ ] Boss ATK x1.35 적용
- [ ] Boss Reward x2.50 적용
- [ ] 42일차 층 난이도와 등급 난이도가 곱연산
- [ ] 상태 이상 수치는 등급 ATK 배율에서 제외
- [ ] F9 재생성 후 같은 층의 조우 등급 규칙 유지
- [ ] 같은 Seed 복원 시 남은 조우 위치와 등급 유지
- [ ] Elite/Boss 클리어 후 해당 조우만 필드에서 제거
- [ ] Elite/Boss 클리어 후 해당 N/E/B 미니맵 표시 제거
- [ ] 클리어 보상에 층 배율과 등급 배율 모두 적용

## 현재 단계의 제한 사항

43일차의 Boss Floor 규칙인 5층 간격은 시스템 검증을 위한 테스트 규칙이다.

현재 다음 기능은 아직 포함하지 않는다.

```text
Elite 전용 AI
Elite 전용 패턴
Boss 전용 AI
Boss 페이즈
Boss 전용 UI
Boss 전용 연출
Boss BGM
고유 Boss 보상 테이블
```

현재 Normal / Elite / Boss의 차이는 전투 등급, 전투력 배율, 보상 배율, 필드 및 미니맵 표시를 중심으로 구성되어 있다.

GitHub 저장소에는 Unity Editor 컴파일 및 Play Mode를 자동 검증하는 CI 상태 검사가 등록되어 있지 않으므로 최종 실행 확인은 로컬 Unity 환경에서 진행한다.

## 완료 결과

43일차를 통해 동일한 탐사 층 안에서도 서로 다른 위험도의 조우가 존재할 수 있는 기반을 구축했다.

42일차의 층별 난이도 상승 위에 Normal / Elite / Boss 조우 등급 배율을 추가하여 최종 전투 능력치와 보상이 두 단계의 난이도 규칙으로 계산된다.

탐사 필드와 미니맵에서도 조우 등급을 직접 확인할 수 있으며, 기존 Seed 기반 맵 복원과 조우 클리어 상태 유지 구조도 그대로 이어진다.

이제 다음 단계에서는 탐사 전체 성공 조건과 성공 결과를 호감도·자원·거점 진행으로 연결할 수 있는 상태가 되었다.
