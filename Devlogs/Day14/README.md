---
# 14일차 개발일지 - 적 기본 행동 및 행동 예고 시스템 구축

---
## 개발 목표

13일차까지 구축한 카드 효과와 턴 흐름에 실제 적 행동을 연결하는 것을 목표로 진행했다.

14일차 완료 기준은 다음과 같다.

- 적 행동 종류 정의
- 적 대상 선택 규칙 정의
- 적 공격 데이터 확장
- 다음 적 행동 사전 생성
- 적 행동 예고 UI 표시
- 적 턴 기본 공격 실행
- 사망한 행동자와 대상 예외 처리
- 전투 종료 시 남은 적 행동 중단

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Enemy Data 위치 | `Assets/_ProjectC/Scripts/Data` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| 적 행동 기본 대기 | 0.75초 |

---
## 1. 적 행동 종류 정의

`EnemyActionType`을 추가하여 적 행동을 다음과 같이 구분했다.

- `None`: 행동 없음
- `Attack`: 기본 공격

14일차에서는 기본 공격만 실행하며 방어, 회복, 강화와 약화는 이후 확장을 위한 별도 행동으로 남겨 두었다.

---
## 2. 대상 선택 규칙 정의

`EnemyTargetRule`을 추가하여 적마다 대상 선택 방법을 설정할 수 있게 했다.

- `FirstLiving`: 첫 번째 생존 아군
- `LowestHealth`: 현재 체력이 가장 낮은 아군
- `RandomLiving`: 무작위 생존 아군

테스트 적은 결과를 반복해서 확인하기 쉬운 `FirstLiving` 규칙을 사용한다.

---
## 3. EnemyData 확장

기존 `EnemyData`에 기본 행동 정보를 추가했다.

추가 항목:

- 기본 행동 종류
- 기본 공격력
- 물리·마법 피해 유형
- 대상 선택 규칙

`Enemy_Test`에는 물리 피해 8과 첫 번째 생존 아군 선택 규칙을 적용했다.

---
## 4. 적 예정 행동 데이터

`BattleEnemyAction`이 한 번의 예정 행동 정보를 저장한다.

저장 정보:

- 행동하는 적
- 대상 아군
- 행동 종류
- 피해 유형
- 예정 피해량

행동 실행 직전에 행동자와 대상의 생존 상태를 다시 확인한다. 유효한 기본 공격만 대상 체력에 피해를 적용한다.

---
## 5. 적 행동 런타임

`BattleEnemyActionRuntime`이 모든 적의 예정 행동을 관리한다.

주요 기능:

- 생존한 적별 다음 행동 생성
- 대상 규칙에 따른 생존 아군 선택
- 적별 예정 행동 조회
- 예정 행동 실행과 제거
- 사망한 적의 예정 행동 자동 취소
- 예정 대상 사망 시 다른 생존 아군 재선택
- 예정 행동 변경 이벤트
- 이벤트 연결 해제

플레이어 턴에는 다음 적 행동을 보관하고, 적 턴에는 복사된 실행 목록을 순서대로 처리한다.

---
## 6. 행동 예고 UI

`BattleUnitView`가 적 캐릭터 내부에 행동 예고 패널을 코드로 생성한다.

표시 형식:

```text
예고: 물리 8
→ 대상 이름
```

표시 정보:

- 물리·마법 피해 유형
- 예정 피해량
- 예정 대상 이름

행동 예고 패널은 적 유닛 영역의 상단에 생성되며 하단 카드 영역과 분리된다. 한글은 프로젝트 공용 동적 TMP 폰트를 사용한다.

별도의 Canvas, Scene 또는 Prefab 연결은 필요하지 않다.

---
## 7. 적 턴 실제 공격

기존의 단순 적 턴 대기를 실제 행동 실행 흐름으로 교체했다.

처리 순서:

1. 플레이어 턴 종료
2. 카드 입력 차단
3. 예정된 적 행동 목록 복사
4. 설정된 시간만큼 대기
5. 적 기본 공격 실행
6. 실행한 행동 예고 제거
7. 남은 적 행동 순차 실행
8. 승패 상태 확인
9. 다음 플레이어 턴 진입
10. 다음 적 행동 예고 생성

적 행동이 모두 끝나면 기존 턴 시스템이 라운드 증가, 공용 AP 회복과 카드 드로우를 처리한다.

---
## 8. 사망과 대상 예외 처리

다음 예외 상황을 처리했다.

- 사망한 적의 예정 행동 제거
- 사망한 적의 공격 실행 차단
- 예정 대상 사망 시 다른 생존 아군 선택
- 생존 아군이 없다면 행동 취소
- 전투 종료 시 실행 중인 적 턴 중단
- 전투 종료 시 남은 행동 예고 제거
- 공격력이 0이면 행동 생성 제외

여러 적이 같은 아군을 대상으로 지정한 상태에서 해당 아군이 먼저 사망하면 남은 행동만 새 대상으로 변경된다.

---
## 생성·수정·삭제 파일

생성:

- `Assets/_ProjectC/Scripts/Data/EnemyActionType.cs`
- `Assets/_ProjectC/Scripts/Data/EnemyActionType.cs.meta`
- `Assets/_ProjectC/Scripts/Data/EnemyTargetRule.cs`
- `Assets/_ProjectC/Scripts/Data/EnemyTargetRule.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAction.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAction.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyActionRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyActionRuntime.cs.meta`
- `Devlogs/Day14/README.md`

수정:

- `Assets/_ProjectC/Scripts/Data/EnemyData.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitView.cs`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_Test.asset`

삭제된 파일은 없다.

---
## 검증 결과

- Unity 참조 기반 C# 컴파일 오류 0건
- 컴파일 경고 0건
- Git 공백 오류 0건
- 신규 메타 GUID 중복 0건
- 테스트 적 행동 데이터 연결 확인
- 적 행동 생성과 실행 분리 확인
- 사망한 적 행동 제거 연결 확인
- 사망 대상 재선택 연결 확인
- 전투 종료 시 적 행동 중단 연결 확인
- 행동 예고 UI 코드 생성과 한글 폰트 연결 확인
- Scene과 Prefab 변경 없음

Unity Play Mode의 실제 턴 종료 클릭, HP 감소와 화면 배치는 Unity Editor에서 최종 확인이 필요하다.

---
## 14일차 결과

플레이어 턴에 적의 다음 행동과 대상을 확인하고, 턴 종료 후 적이 실제로 아군을 공격하는 기본 전투 순환을 구축했다.

적 행동은 데이터, 예정 행동, 실행 흐름과 화면 표시를 분리하여 이후 방어, 회복, 상태 효과와 복합 행동을 추가할 수 있게 구성했다.

적 행동 완료 후 기존 라운드 증가, 공용 AP 회복과 카드 드로우가 이어진다.

---
## 다음 개발 방향

다음 단계에서는 아군과 적 데이터에 물리 방어력과 마법 저항력을 추가하고, 공통 피해 계산식을 통해 카드 공격과 적 공격의 최종 피해량을 계산한다.

---
## Commit

`14일차 : 적 기본 행동 및 행동 예고 시스템 구축`
