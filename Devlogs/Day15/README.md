---
# 15일차 개발일지 - 물리·마법 방어 및 공통 피해 계산 시스템 구축

---
## 개발 목표

14일차에 구현한 카드 공격과 적 공격에 물리 방어력과 마법 저항력을 적용하고, 모든 공격이 하나의 피해 계산 규칙을 사용하도록 통합했다.

15일차 완료 기준은 다음과 같다.

- 아군·적 물리 방어력 추가
- 아군·적 마법 저항력 추가
- 공통 피해 계산기 구현
- 물리·마법 피해 유형별 방어 계산
- 카드 공격과 적 공격 계산 통합
- 적 행동 예고에 예상 피해 표시
- 원본·감소·최종·실제 피해 기록

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Data Script 위치 | `Assets/_ProjectC/Scripts/Data` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |

---
## 1. 방어 능력치 추가

`CharacterData`와 `EnemyData`에 다음 방어 능력치를 추가했다.

- `physicalDefense`: 물리 방어력
- `magicalResistance`: 마법 저항력

두 값은 0 이상으로 제한하고, `BattleUnitRuntime` 생성 시 원본 데이터에서 복사한다.

테스트 능력치는 다음과 같다.

| 유닛 | 물리 방어력 | 마법 저항력 |
|---|---:|---:|
| Test Character | 2 | 1 |
| Test Enemy | 3 | 2 |

---
## 2. 공통 피해 계산식

카드 공격과 적 공격이 다음 계산식을 함께 사용한다.

```text
최종 피해 = 최대값(1, 원본 피해 - 대응 방어력)
```

피해 유형별 규칙:

- 물리 피해: 대상의 물리 방어력 적용
- 마법 피해: 대상의 마법 저항력 적용
- 일반 피해: 방어 능력치 미적용
- 0 이하 원본 피해: 피해 없음
- 유효한 공격: 최소 피해 1

방어력이 공격력보다 높더라도 유효한 공격은 최소 1의 피해를 준다.

---
## 3. BattleDamageResult 구현

`BattleDamageResult`가 한 번의 피해 계산 결과를 저장한다.

저장 정보:

- 원본 피해
- 적용 방어값
- 방어로 감소한 피해
- 방어 계산 후 최종 피해
- 실제 HP에서 감소한 피해
- 피해 유형

대상의 남은 HP가 최종 피해보다 낮으면 실제 피해는 남은 HP까지만 기록한다.

---
## 4. BattleDamageCalculator 구현

`BattleDamageCalculator`가 피해 유형에 맞는 방어 능력치를 선택하고 최종 피해를 계산한다.

입력:

- 원본 피해량
- 피해 유형
- 물리 방어력
- 마법 저항력

출력은 `BattleDamageResult`로 반환하여 공격 종류와 관계없이 같은 결과 형식을 사용한다.

---
## 5. BattleUnitRuntime 피해 처리

`BattleUnitRuntime`이 물리 방어력과 마법 저항력을 보관하도록 확장했다.

피해 처리는 다음 두 단계로 구분한다.

- `PreviewDamage`: HP를 변경하지 않고 예상 결과 계산
- `TakeDamage`: 계산 결과의 실제 피해를 HP에 적용

예상 피해와 실제 피해가 같은 계산기를 사용하므로 적 행동 예고와 실제 HP 감소가 일치한다.

---
## 6. 카드 공격 연결

`BattleCardActionController`가 카드 피해량과 `BattleDamageType`을 대상에게 함께 전달한다.

테스트 결과 기준:

- 물리 카드 원본 피해 10
- 적 물리 방어력 3
- 최종 피해 7

- 마법 카드 원본 피해 8
- 적 마법 저항력 2
- 최종 피해 6

회복 카드는 기존 `RestoreHealth`를 사용하며 방어력 계산을 거치지 않는다.

---
## 7. 적 공격과 행동 예고 연결

`BattleEnemyAction`이 대상의 방어 능력치를 사용해 예상 피해를 계산하고 실제 공격에도 같은 피해 유형을 전달한다.

테스트 적 공격:

- 원본 물리 피해 8
- 아군 물리 방어력 2
- 최종 피해 6

행동 예고 표시 형식:

```text
예고: 물리 8 → 6
→ Test Character
```

예정 대상이 변경되면 새 대상의 방어 능력치와 현재 HP를 기준으로 예상 피해가 다시 계산된다.

---
## 8. 피해 상세 기록

카드와 적 공격은 Console에 다음 정보를 출력한다.

```text
원본 10 / 방어 3 / 감소 3 / 최종 7 / 실제 7
```

최종 피해는 방어 계산 결과이며 실제 피해는 남은 HP에 적용된 수치다.

---
## 생성·수정·삭제 파일

생성:

- `Assets/_ProjectC/Scripts/Battle/BattleDamageResult.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleDamageResult.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleDamageCalculator.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleDamageCalculator.cs.meta`
- `Devlogs/Day15/README.md`

수정:

- `Assets/_ProjectC/Scripts/Data/CharacterData.cs`
- `Assets/_ProjectC/Scripts/Data/EnemyData.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAction.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyActionRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/ScriptableObjects/Characters/Character_Test.asset`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_Test.asset`

삭제된 파일은 없다.

---
## 검증 결과

- Unity 참조 기반 C# 컴파일 오류 0건
- 컴파일 경고 0건
- Git 공백 오류 0건
- 신규 메타 GUID 중복 0건
- 물리 방어력 데이터와 런타임 연결 확인
- 마법 저항력 데이터와 런타임 연결 확인
- 카드와 적 공격의 공통 계산기 사용 확인
- 예상 피해와 실제 피해 계산 경로 일치 확인
- 회복 효과의 방어 계산 제외 확인
- 임시 컴파일 설정 원상 복구
- Scene과 Prefab 변경 없음

Unity Play Mode의 실제 카드 클릭, 적 턴 HP 감소와 행동 예고 표시는 Unity Editor에서 최종 확인이 필요하다.

---
## 15일차 결과

물리 피해와 마법 피해가 대상의 대응 방어 능력치에 따라 서로 다른 최종 피해를 적용하도록 구현했다.

카드 공격과 적 공격이 같은 계산기와 결과 구조를 사용하며, 적 행동 예고도 실제 전투 계산과 동일한 예상 피해를 표시한다.

상세 피해 기록을 통해 원본 피해부터 실제 HP 감소까지 계산 과정을 확인할 수 있다.

---
## 다음 개발 방향

다음 단계에서는 피해 결과를 화면에 표시하는 플로팅 숫자, 피격 강조와 간단한 전투 연출을 추가하여 Console 없이도 전투 결과를 확인할 수 있도록 구성한다.

---
## Commit

`15일차 : 물리·마법 방어 및 공통 피해 계산 시스템 구축`
