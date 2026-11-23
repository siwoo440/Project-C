# Project C - 46일차 개발일지

## 작업 주제

카드 분류·속성 약점 판정 시스템 구축과 피해 계산 DEBUG UI 개선

## 개발 목표

- 카드에 Attack / Magic / Skill 태그 분류 추가
- 기존 카드 계열을 이용한 적 약점 데이터 구축
- 카드 속성과 적 약점을 비교해 약점 피해 적용
- 기존 물리 방어·마법 저항 계산과 약점 배율 연결
- 카드 사용 시 피해 계산 과정을 개발용 UI로 확인
- F6 키로 피해 계산 DEBUG UI 표시 ON/OFF
- DEBUG UI를 왼쪽 아래에 배치하고 가독성 개선
- 계산 결과를 다음 판정이 발생할 때까지 유지
- 다음 카드 판정 발생 시 기존 결과를 지우고 새 계산식으로 갱신
- 적 행동 예고 UI를 초상화에서 조금 위로 이동
- 전투에서 처리한 조우가 `30_Exploration` 복귀 후 화면에 남는 문제 수정

## 구현 내용

### 1. 카드 행동 태그 추가

새로운 `CardTag` 분류를 추가했다.

```text
Attack
Magic
Skill
```

카드 데이터에서 태그를 직접 지정할 수 있으며, 기존 카드 데이터와의 호환을 위해 태그가 지정되지 않은 카드에는 기본 분류를 추론한다.

기본 규칙:

```text
Damage 카드
→ Attack

Wand 또는 Magical 피해 카드
→ Magic 추가

비피해 효과 카드
→ Skill
```

이를 통해 기존 `CardType`, `BattleDamageType`과 별개로 카드가 어떤 행동 성격을 가지는지 확인할 수 있게 했다.

### 2. 적 약점 데이터 추가

`EnemyData`에 카드 계열 기반 약점 목록을 추가했다.

```text
WeaknessCardTypes
```

한 적이 여러 카드 계열을 약점으로 가질 수 있도록 목록 형태로 관리한다.

테스트용 `Enemy_Test`에는 다음 약점을 설정했다.

```text
Wand
```

따라서 지팡이 계열 카드로 공격할 때 약점 판정이 발생한다.

### 3. 약점 판정 계산기 구축

`BattleWeaknessCalculator`를 추가해 카드 계열과 적 약점의 비교를 공통 처리한다.

현재 46일차 기능 검사용 약점 배율:

```text
×1.50
```

이 값은 최종 밸런스 수치가 아니라 현재 시스템 검증용 값이다.

### 4. 기존 피해 계산에 약점 배율 연결

기존 피해 계산 흐름을 유지하면서 방어 계산 이후 약점 배율을 적용하도록 확장했다.

현재 계산 흐름:

```text
카드 기본 피해
↓
공격 보너스
↓
정신 상태 피해 보정
↓
Physical / Magical에 맞는 방어 계산
↓
카드 계열과 적 약점 비교
↓
약점이면 ×1.50
↓
최종 피해
```

약점이 아닌 공격은 기존과 동일하게 `×1.00`으로 처리한다.

### 5. 피해 결과 데이터 확장

`BattleDamageResult`에 약점 판정과 중간 계산을 확인할 수 있는 값을 추가했다.

```text
DefenseAdjustedDamage
IsWeakness
WeaknessMultiplier
WeaknessBonusDamage
```

이를 통해 최종 피해만 저장하는 것이 아니라 방어 적용 결과와 약점 보정 과정을 별도로 확인할 수 있다.

### 6. 카드 피해 문맥 연결

`BattleCardDamageContext`와 `BattleCardWeaknessRuntimeBridge`를 추가했다.

카드 사용 이벤트가 발생하면 현재 사용 카드와 대상 목록을 저장하고, 실제 피해 계산 시 해당 카드와 대상의 약점을 확인한다.

구조:

```text
CardUsed
↓
현재 카드 / 대상 목록 저장
↓
BattleDamageCalculator
↓
대상별 약점 판정
↓
피해 결과 생성
↓
DEBUG UI 기록
```

### 7. 피해 계산 DEBUG UI 구축

`BattleDamageDebugView`를 추가했다.

카드를 사용하면 실제 피해 계산 과정을 화면에서 확인할 수 있다.

표시 예시:

```text
[46일차 피해 계산 DEBUG]

카드 : 마력 탄환
태그 : Attack | Magic
속성 : 지팡이
피해 유형 : Magical / 마법
대상 : 1명

[1/1] Test Enemy
카드값 14 + 공격 보너스 0 = 14
정신 상태 보정 : 14 → 14
마법 저항 2
14 - 2 = 12
약점 지팡이 : YES ×1.50
최종 Round(12 × 1.50) = 18
적용 피해 min(현재 HP 50, 18) = 18
```

광역 공격은 대상별 계산 결과를 같은 창에 순서대로 표시한다.

### 8. F6 DEBUG UI 토글

피해 계산 DEBUG UI에 F6 단축키를 적용했다.

프로젝트가 New Input System을 사용하므로 구형 `UnityEngine.Input.GetKeyDown()`은 사용하지 않고 다음 입력 방식을 사용한다.

```text
Keyboard.current.f6Key.wasPressedThisFrame
```

동작:

```text
F6
→ DEBUG UI OFF

F6
→ DEBUG UI ON
```

DEBUG UI를 꺼도 실제 카드 피해와 약점 판정은 계속 작동한다.

### 9. DEBUG UI 위치와 가독성 개선

초기 중앙 배치에서는 전투 화면을 많이 가렸기 때문에 DEBUG UI를 왼쪽 아래로 이동했다.

현재 배치:

```text
화면 왼쪽 아래
X 여백 18
Y 아래 여백 18
```

글자 크기도 확대했다.

```text
제목 : 20
안내 : 16
본문 : 18
```

광역 공격 결과가 길어질 수 있으므로 기존 스크롤 기능은 유지한다.

### 10. DEBUG 결과 지속 표시 및 자동 갱신

초기 버전에서는 일정 시간이 지나면 DEBUG 창이 사라졌으나, 계산 검증에 불편해 자동 종료 시간을 제거했다.

현재 동작:

```text
첫 카드 판정
↓
계산 결과 표시
↓
다음 판정 전까지 계속 유지
↓
다음 카드 판정 발생
↓
기존 계산식 삭제
↓
새 카드 계산 결과로 갱신
```

F6으로 창을 숨겼다가 다시 켜면 현재 저장된 마지막 계산 결과를 다시 볼 수 있다.

### 11. 적 행동 예고 UI 위치 조정

적 행동 예고가 적 초상화와 지나치게 붙어 보이던 부분을 수정했다.

기존 기준:

```text
Y = -6
```

46일차 조정 기준:

```text
Y = +18
```

약 24px 위로 이동해 적 초상화와 행동 예고 정보 사이에 여백을 만들었다.

`BattleEnemyIntentOffsetRuntime`에서 전투 중 생성된 행동 예고 UI 위치를 보정한다.

### 12. 테스트 카드와 적 약점 연결

기존 테스트 카드 `마력 탄환`에 명시적인 카드 태그를 추가했다.

```text
Attack | Magic
```

`Enemy_Test`는 Wand 약점을 가지므로 해당 카드로 테스트할 때 약점 피해를 확인할 수 있다.

테스트 예시:

```text
마력 탄환 기본 피해 14
마법 저항 2

14 - 2 = 12
12 × 1.50 = 18
```

추가 전투 보정이 없다면 최종 피해는 18이 된다.

### 13. 탐사 조우 잔존 문제 수정

전투에서 적 조우를 클리어한 뒤 `30_Exploration`으로 복귀했을 때, 해당 조우 표시가 화면에 남아 있는 문제를 추가 수정했다.

`ExplorationMapRuntime`에서 런타임 조우 오브젝트와 조우 ID·좌표를 연결해 관리한다.

전투 승리 후 세션에 클리어된 조우 ID가 기록되면 현재 맵에서 이를 감지해 다음을 수행한다.

```text
클리어 조우 확인
↓
조우 좌표 정보 제거
↓
Trigger 즉시 비활성화
↓
조우 GameObject 제거
↓
현재 조우 목록에서 제거
```

따라서 처리한 적은 탐사 복귀 후 다시 화면에 남지 않으며 재접촉도 방지된다.

## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleCardDamageContext.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardDamageContext.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleCardWeaknessRuntimeBridge.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardWeaknessRuntimeBridge.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleDamageDebugView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleDamageDebugView.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentOffsetRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentOffsetRuntime.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleWeaknessCalculator.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleWeaknessCalculator.cs.meta`
- `Assets/_ProjectC/Scripts/Data/CardTag.cs`
- `Assets/_ProjectC/Scripts/Data/CardTag.cs.meta`

## 수정 파일

- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestArcaneBolt.asset`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_Test.asset`
- `Assets/_ProjectC/Scripts/Battle/BattleDamageCalculator.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleDamageResult.cs`
- `Assets/_ProjectC/Scripts/Data/CardData.cs`
- `Assets/_ProjectC/Scripts/Data/EnemyData.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console 컴파일 오류 없음
- [ ] F6 입력 시 Input System 관련 예외 없음
- [ ] F6으로 피해 계산 DEBUG UI ON/OFF 가능
- [ ] 피해 카드 사용 시 DEBUG UI 표시
- [ ] DEBUG UI가 화면 왼쪽 아래에 표시
- [ ] DEBUG UI 제목·본문 글자 크기가 충분히 읽기 쉬움
- [ ] DEBUG UI가 시간이 지나도 자동으로 사라지지 않음
- [ ] 다음 카드 판정 시 이전 계산 결과가 새 결과로 교체
- [ ] F6으로 숨긴 뒤 다시 켰을 때 마지막 계산 결과 확인 가능
- [ ] 카드 기본값과 공격 보너스 계산 표시
- [ ] 정신 상태 보정 전후 수치 표시
- [ ] Physical 카드에서 물리 방어 사용
- [ ] Magical 카드에서 마법 저항 사용
- [ ] Wand 약점 적에게 Wand 카드 사용 시 약점 YES 표시
- [ ] 약점 공격에 ×1.50 적용
- [ ] 비약점 공격에 ×1.00 적용
- [ ] 광역 공격 시 대상별 계산 결과 표시
- [ ] DEBUG UI를 꺼도 실제 피해와 약점 판정 정상 작동
- [ ] 적 행동 예고가 기존보다 위쪽에 표시
- [ ] 적 행동 예고와 초상화 사이에 적당한 간격 존재
- [ ] 전투 승리 후 `30_Exploration` 복귀
- [ ] 클리어한 조우 표시가 탐사 화면에서 제거
- [ ] 제거된 조우와 다시 접촉해 전투가 발생하지 않음
- [ ] 다른 미클리어 조우는 정상적으로 남아 있음
- [ ] 다음 층 이동과 Seed 복원이 기존처럼 정상 작동

## 현재 단계의 제한 사항

- 약점 피해 배율 `×1.50`은 46일차 기능 검사용 임시 수치다.
- 현재 약점은 기존 `CardType` 계열을 기준으로 판정하며, 별도의 원소 속성 체계는 아직 추가하지 않았다.
- 피해 계산 DEBUG UI는 개발 검증용 IMGUI이며 정식 전투 UI가 아니다.
- 적 행동 예고 위치 조정은 현재 런타임 보정 방식으로 적용된다.
- GitHub 저장소에는 현재 커밋에 대한 Unity Editor 컴파일 또는 Play Mode 자동 CI 검사가 등록되어 있지 않으므로 실제 동작 검증은 로컬 Unity에서 진행한다.

## 완료 결과

46일차를 통해 카드 전투는 단순한 물리·마법 피해 구분에서 한 단계 확장되어 카드 행동 태그와 적 약점 판정을 사용할 수 있게 되었다.

현재 전투 계산 구조는 다음과 같다.

```text
카드 사용
↓
카드 태그·계열 확인
↓
공격 보정
↓
정신 상태 보정
↓
물리 방어 / 마법 저항
↓
적 약점 판정
↓
약점 피해 배율
↓
최종 피해
```

또한 모든 계산 과정을 F6 개발용 DEBUG UI에서 확인할 수 있으며, 결과는 다음 판정이 발생할 때까지 유지된다.

전투 UI 측면에서는 적 행동 예고와 초상화의 간격을 조정했고, 탐사 측면에서는 이미 클리어한 조우 표시가 `30_Exploration`에 남는 문제를 수정했다.

이를 통해 다음 단계의 적 분석·보스 정보 표시와 전투 밸런스 검증에 사용할 수 있는 카드 속성·약점·계산 추적 기반이 마련되었다.
