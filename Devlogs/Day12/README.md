---
# 12일차 개발일지 - 플레이어·적 턴 전환 및 턴 시작 처리 구축

---
## 개발 목표

11일차에서 구현한 전투 공용 AP와 카드 비용 제한을 실제 턴 흐름에 연결하는 것을 목표로 진행했다.

12일차 완료 기준은 다음과 같다.

- 플레이어 턴과 적 턴 상태 구분
- 현재 라운드 관리
- 턴 종료 버튼 코드 생성
- 적 턴 카드 입력 차단
- 다음 플레이어 턴 공용 AP 자동 회복
- 다음 플레이어 턴 카드 자동 드로우
- 아군·적 전멸에 따른 승패 판정
- 턴 상태에 따른 화면 자동 갱신

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| 턴당 기본 드로우 | 1장 |
| 임시 적 턴 대기 | 0.75초 |

---
## 1. 전투 턴 상태 정의

`BattleTurnPhase`를 추가하여 전투 상태를 다음 단계로 구분했다.

- `NotStarted`: 전투 시작 전
- `PlayerTurn`: 플레이어 카드 행동 가능
- `EnemyTurn`: 플레이어 카드 행동 차단
- `Victory`: 모든 적 사망
- `Defeat`: 모든 아군 사망

승리와 패배도 턴 상태에 포함하여 전투 종료 후 입력이 다시 활성화되지 않도록 구성했다.

---
## 2. BattleTurnRuntime 구현

`BattleTurnRuntime`이 전투 라운드와 턴 전환을 전담한다.

주요 기능:

- 전투 시작과 첫 플레이어 턴 진입
- 현재 라운드와 턴 단계 저장
- 플레이어 턴 종료
- 적 턴 완료와 다음 라운드 진입
- 플레이어 턴 시작 AP 회복
- 플레이어 턴 시작 카드 드로우
- 유닛 사망 이벤트 기반 승패 판정
- 턴 상태 변경 이벤트
- 이벤트 연결 해제

전투 시작 손패와 매 턴 드로우를 구분하여 첫 턴에 카드가 중복 지급되지 않도록 처리했다.

---
## 3. 플레이어 턴 시작 처리

적 턴이 끝나면 다음 순서로 플레이어 턴을 시작한다.

1. 현재 라운드 증가
2. 공용 AP 최대 회복
3. 설정된 수만큼 카드 드로우
4. 플레이어 턴 상태 적용
5. UI와 카드 사용 가능 상태 갱신

기본 턴당 드로우 수는 1장이며 `BattleSceneSetup`의 `cardsPerPlayerTurn`에서 변경할 수 있다.

---
## 4. 턴 종료 UI 자동 생성

`BattleHandView`가 카드 영역 상단에 턴 상태와 턴 종료 버튼을 코드로 생성한다.

상단 배치 순서:

`덱 상태 | 라운드·턴 | 턴 종료 | 공용 AP`

기존 손패 영역은 상단 상태 줄 아래에 유지하여 카드와 버튼이 겹치지 않도록 구성했다.

턴 종료 버튼은 플레이어 턴에만 활성화된다.

---
## 5. 카드 입력 잠금

`BattleCardActionController`와 `BattleCardView`가 현재 턴 상태를 확인하도록 변경했다.

입력 제한 규칙:

- 플레이어 턴에만 카드 클릭 가능
- 적 턴 진입 시 선택 카드와 대상 강조 초기화
- 적 턴과 전투 종료 상태에서 카드 사용 차단
- 사용 불가능한 카드 반투명 표시
- 실행 직전 턴 상태 재검사

화면 표시와 실제 카드 실행 양쪽에서 턴을 검사하여 잘못된 입력이 전투 상태를 변경하지 못하도록 막았다.

---
## 6. 임시 적 턴 처리

현재 `EnemyData`에는 공격력, 행동 종류, 대상 선택 규칙이 없다.

정해지지 않은 적 AI를 임의로 구현하지 않고 다음 흐름만 연결했다.

1. 플레이어 턴 종료
2. 적 턴 상태 표시
3. 플레이어 카드 입력 잠금
4. 0.75초 대기
5. 다음 플레이어 턴 진입

실제 적 행동은 적 행동 데이터와 예고 규칙을 구현한 뒤 이 대기 구간에 연결한다.

---
## 7. 승리와 패배 판정

`BattleTurnRuntime`이 아군과 적의 사망 이벤트를 구독한다.

- 모든 적 사망: `Victory`
- 모든 아군 사망: `Defeat`

전투가 끝나면 턴 종료 버튼과 카드 입력을 비활성화하고 결과를 Console에 출력한다.

---
## 생성·수정·삭제 파일

생성:

- `Assets/_ProjectC/Scripts/Battle/BattleTurnPhase.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnPhase.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs.meta`
- `Devlogs/Day12/README.md`

수정:

- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleHandView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`

삭제된 파일은 없다.

---
## 검증 결과

- Unity 참조 기반 C# 컴파일 오류 0건
- 컴파일 경고 0건
- Git 공백 오류 0건
- 신규 메타 GUID 중복 0건
- 플레이어 턴 외 카드 실행 차단 코드 확인
- 플레이어 턴 시작 AP 회복 연결 확인
- 플레이어 턴 시작 드로우 연결 확인
- 유닛 사망 이벤트 승패 판정 연결 확인
- UI 요소 코드 생성과 한글 폰트 연결 확인

Unity Play Mode의 실제 버튼 클릭과 해상도별 배치는 Unity Editor에서 최종 확인이 필요하다.

---
## 12일차 결과

플레이어 턴과 적 턴을 구분하고 턴 종료 버튼으로 전투 흐름을 전환하는 기본 턴 시스템을 구축했다.

다음 플레이어 턴이 시작되면 공용 AP와 손패가 자동으로 갱신되며, 적 턴과 전투 종료 상태에서는 카드 입력이 차단된다.

적 행동 데이터가 없는 현재 단계에서는 적 턴을 자동 통과하도록 구성하여 이후 적 AI 시스템을 연결할 자리를 확보했다.

---
## 다음 개발 방향

다음 단계에서는 기본 피해·회복 효과를 분리하고, 적 행동 데이터와 행동 예고를 구현하기 전에 현재 기획 일정과 실제 구현 순서를 재정렬한다.

---
## Commit

`12일차 : 플레이어·적 턴 전환 및 턴 시작 처리 구축`
