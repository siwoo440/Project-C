---
# 11일차 개발일지 - 전투 공용 AP 및 카드 비용 제한 시스템 구축

---
## 개발 목표

10일차에서 완성한 카드 사용 흐름에 모든 아군이 함께 사용하는 공용 AP를 연결하는 것을 목표로 진행했다.

11일차 완료 기준은 다음과 같다.

- 전투 공용 AP 생성
- 카드 사용 전 비용 검사
- 카드 사용 성공 시 공용 AP 차감
- AP 부족 시 카드 사용 차단
- AP 부족 카드 비활성 표시
- 카드 영역 오른쪽에 공용 AP 표시
- 공용 AP 회복 기능
- 공용 AP 변경에 따른 카드 상태 자동 갱신

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| 기본 공용 AP | 3 |
| Test Card Cost | 1 |

---
## 1. 공용 AP 설계

카드마다 소유 캐릭터는 유지하지만 AP는 캐릭터별로 분리하지 않고 모든 아군이 하나의 값을 공유하도록 구성했다.

전투 시작 시 공용 AP는 최대값으로 생성된다.

기본 설정:

| 항목 | 값 |
|---|---|
| 최대 공용 AP | 3 |
| 시작 공용 AP | 3 |
| 최소 최대 AP | 1 |

어떤 아군의 카드를 사용해도 같은 공용 AP에서 비용이 차감된다.

---
## 2. BattleActionPointRuntime 구현

공용 AP 상태를 전담하는 `BattleActionPointRuntime`을 추가했다.

포함 기능:

- 최대 AP 조회
- 현재 AP 조회
- 카드 비용 지불 가능 여부 확인
- 카드 비용 차감
- 최대 AP 회복
- AP 변경 이벤트
- 음수 비용 거부
- AP 부족 시 차감 거부

AP 값은 `CharacterData`나 `BattleUnitRuntime`에 저장하지 않는다.

---
## 3. 카드 비용 검사와 차감

`BattleCardActionController`가 카드 사용 전에 공용 AP를 검사하도록 변경했다.

처리 순서:

1. 카드가 손패에 있는지 확인
2. 카드 소유자가 생존했는지 확인
3. 공용 AP와 카드 비용 비교
4. 카드 대상 선택
5. 공용 AP 차감
6. 카드 효과 적용
7. 사용 카드를 버린 카드 더미로 이동
8. 카드와 대상 선택 상태 초기화

AP가 부족하면 피해와 카드 이동을 실행하지 않고 경고를 출력한다.

---
## 4. 카드 비활성 표시

현재 공용 AP로 비용을 지불할 수 없는 카드는 전체 투명도를 낮춰 반투명으로 표시한다.

비활성 조건:

- 카드 비용이 현재 공용 AP보다 큼
- 카드 소유자가 사망함
- 공용 AP가 연결되지 않음

비활성 카드도 클릭 이벤트는 유지하여 AP 부족 원인을 Console에서 확인할 수 있다.

---
## 5. 카드 영역 공용 AP UI

공용 AP 표시를 유닛 영역이 아닌 하단 카드 영역의 맨 오른쪽에 배치했다.

표시 형식:

`AP 현재 / 최대`

예시:

`AP 3 / 3`

기존 덱·손패·버린 카드 상태 텍스트의 최대 영역을 78%로 줄이고, 오른쪽 20%를 공용 AP 전용 영역으로 사용한다.

AP 텍스트와 배치는 `BattleHandView`가 코드로 자동 생성하므로 별도의 Canvas 연결이 필요하지 않다.

---
## 6. 상태 변경 자동 갱신

`BattleHandView`가 공용 AP의 `StateChanged` 이벤트를 구독한다.

AP가 변경되면 다음 화면이 즉시 갱신된다.

- 카드 영역 오른쪽 AP 수량
- 손패 카드 사용 가능 여부
- 비활성 카드 투명도

손패 카드가 다른 캐릭터 소유라도 동일한 공용 AP 상태를 기준으로 갱신된다.

---
## 7. 공용 AP 회복 테스트

`BattleSceneSetup`에 다음 Context Menu를 추가했다.

- `테스트/공용 AP 회복`

실행하면 현재 공용 AP를 최대값으로 회복하고 카드 활성 상태를 자동으로 다시 계산한다.

실제 턴 시작 시 자동 회복하는 기능은 이후 턴 시스템에서 이 메서드와 연결할 수 있다.

---
## 8. 전투 씬 설정

`40_Battle` 씬의 `BattleSceneSetup`에 최대 공용 AP 값을 직접 저장했다.

```text
sharedMaximumActionPoints: 3
```

전투 초기화 과정에서 공용 AP를 생성한 뒤 손패 화면과 카드 행동 관리자에 같은 인스턴스를 전달한다.

---
## 생성·수정·삭제 파일

생성:

- `Assets/_ProjectC/Scripts/Battle/BattleActionPointRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleActionPointRuntime.cs.meta`
- `Devlogs/Day11/README.md`

수정:

- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleHandView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scenes/40_Battle.unity`

삭제된 파일은 없다.

---
## 검증 결과

- Unity 참조 기반 C# 컴파일 오류 0건
- 컴파일 경고 0건
- Unity Editor 전투 초기화 공용 AP 3 확인
- 실제 카드 사용 후 공용 AP `3 → 2 → 1 → 0` 확인
- 카드 피해 적용과 버린 카드 이동 유지 확인
- AP 부족 상태의 추가 차감 차단 확인
- 공용 AP 독립 상태 전이 테스트 통과
- 공용 AP 최대 회복 확인
- 캐릭터별 AP 코드 잔존 0건
- 씬 직렬화 ID 중복 0건
- 신규·수정 코드 한글 주석 누락 0건
- Allman 스타일 위반 0건
- Git 공백 오류 0건

---
## 11일차 결과

모든 아군 카드가 하나의 공용 AP를 사용하도록 카드 비용 시스템을 구축했다.

카드 영역 오른쪽에서 현재 공용 AP를 확인할 수 있으며, 카드 사용과 회복에 맞춰 AP 수량과 카드 활성 상태가 자동으로 갱신된다.

카드 소유자는 대상과 사망 상태 검사에 계속 사용하지만 AP는 소유자와 분리하여 전투 전체가 공유한다.

---
## 다음 개발 방향

다음 단계에서는 플레이어 턴과 적 턴을 구분하는 턴 관리자를 구현하고, 플레이어 턴 시작 시 공용 AP 회복과 손패 드로우를 자동 실행하도록 연결한다.

---
## Commit

`11일차 : 전투 공용 AP 및 카드 비용 제한 시스템 구축`
