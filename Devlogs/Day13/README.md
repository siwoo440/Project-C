---
# 13일차 개발일지 - 카드 효과 확장 및 카드 정보 UI 보완

---
## 개발 목표

12일차에 구축한 턴 흐름과 카드 사용 구조에 피해 유형과 회복 효과를 추가하고, 11일차 카드 UI에서 부족했던 정보 확인 기능을 보완했다.

13일차 완료 기준은 다음과 같다.

- 물리 피해와 마법 피해 구분
- 자신 회복과 아군 회복 카드 처리
- 최대 체력을 넘지 않는 회복량 계산
- 유효하지 않은 회복 대상 차단
- 카드 Hover 확대 표시
- 카드 상세 툴팁 코드 생성
- 테스트 카드와 테스트 덱 구성

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Card Data 위치 | `Assets/_ProjectC/Scripts/Data` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| 테스트 덱 카드 수 | 8장 |

---
## 1. 물리·마법 피해 구분

`BattleDamageType`을 추가하여 카드 피해 유형을 다음과 같이 구분했다.

- `None`: 피해 유형 없음
- `Physical`: 물리 피해
- `Magical`: 마법 피해

`CardData`와 `CardInstance`가 피해 유형을 전달하고, `BattleCardActionController`가 카드 실행 결과에 실제 피해 유형을 함께 출력하도록 구성했다.

현재 단계에서는 물리·마법 방어력 계산이 없으므로 두 유형 모두 카드 효과 수치만큼 체력을 감소시킨다. 이후 방어력과 저항 시스템을 추가할 때 피해 계산식을 분리할 수 있는 구조를 확보했다.

---
## 2. 회복 효과 구현

`CardEffectType`에 `Heal`을 추가하고 카드 실행 컨트롤러가 피해와 회복을 구분해 처리하도록 변경했다.

회복 규칙:

- 0 이하의 회복량 거부
- 사망한 대상 회복 거부
- 최대 체력인 대상 회복 거부
- 최대 체력을 초과하지 않도록 회복량 제한
- 실제 회복량만 결과에 반영
- 유효한 대상에게 효과가 적용된 뒤 공용 AP 차감

`BattleUnitRuntime.RestoreHealth`가 실제 회복과 체력 변경 이벤트 발생을 담당한다.

---
## 3. 회복 대상 선택

자신 회복 카드는 카드 소유자를 대상으로 사용한다.

아군 회복 카드는 생존 상태이면서 체력이 감소한 아군만 유효한 대상으로 인정한다. 모든 아군의 체력이 최대라면 카드를 소비하거나 AP를 차감하지 않는다.

현재 테스트 전투에는 아군이 한 명이므로 아군 단일 회복 카드도 해당 아군을 대상으로 동작한다.

---
## 4. 카드 Hover 확대 보완

11일차 카드 UI 보완 작업으로 `BattleCardView`에 포인터 진입과 이탈 처리를 추가했다.

- 마우스 진입 시 카드 크기 1.08배 확대
- 마우스 이탈 시 원래 크기 복구
- 카드 사용 불가 또는 비활성화 시 확대 상태 초기화
- 기존 카드 레이아웃 크기는 유지
- 적 턴 진입 시 Hover 상태 해제

확대는 카드 자체의 `localScale`만 변경하므로 하단 카드 영역의 배치 간격을 다시 계산하지 않는다.

---
## 5. 카드 상세 툴팁

`BattleCardTooltipView`를 추가하여 카드 상세 정보를 코드로 생성한다.

표시 정보:

- 카드 이름
- 카드 소유자
- 공용 AP 비용
- 대상 유형
- 피해 또는 회복 효과
- 물리·마법 피해 유형
- 효과 수치
- 카드 설명

툴팁은 카드 영역 내부에 생성되며 별도의 Canvas 또는 Inspector 연결이 필요하지 않다. 카드에서 마우스가 벗어나거나 손패가 갱신되면 자동으로 숨겨진다.

---
## 6. 테스트 카드와 덱 구성

다음 테스트 카드를 추가했다.

| 카드 | 효과 | 대상 | 비용 |
|---|---|---|---|
| 테스트 공격 | 물리 피해 | 적 1명 | AP 1 |
| 마력탄 | 마법 피해 | 적 1명 | AP 1 |
| 응급 처치 | 회복 | 자신 | AP 1 |
| 치유 지원 | 회복 | 아군 1명 | AP 1 |

테스트 덱에는 각 카드가 2장씩 포함되어 총 8장으로 구성했다.

---
## 생성·수정·삭제 파일

생성:

- `Assets/_ProjectC/Scripts/Data/BattleDamageType.cs`
- `Assets/_ProjectC/Scripts/Data/BattleDamageType.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleCardTooltipView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardTooltipView.cs.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMagicAttack.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMagicAttack.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestSelfHeal.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestSelfHeal.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAllyHeal.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAllyHeal.asset.meta`
- `Devlogs/Day13/README.md`

수정:

- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAttack.asset`
- `Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleHandView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/Scripts/Data/CardData.cs`
- `Assets/_ProjectC/Scripts/Data/CardEffectType.cs`

삭제된 파일은 없다.

---
## 검증 결과

- Unity 참조 기반 C# 컴파일 오류 0건
- 컴파일 경고 0건
- Git 공백 오류 0건
- 신규 메타 GUID 중복 0건
- 테스트 덱 카드 참조 누락 0건
- 테스트 덱 카드 수 8장 확인
- 피해·회복 효과 분기 확인
- 공용 AP 차감 순서 확인
- 카드 Hover와 툴팁 이벤트 연결 확인

Unity Play Mode의 실제 포인터 동작과 해상도별 툴팁 배치는 Unity Editor에서 최종 확인이 필요하다.

---
## 13일차 결과

카드가 물리 공격, 마법 공격, 자신 회복, 아군 회복 효과를 구분하여 실행할 수 있게 되었다.

카드 Hover 확대와 상세 툴팁을 추가하여 카드의 대상, 비용, 효과 유형과 수치를 전투 화면에서 확인할 수 있게 되었다.

피해 유형은 향후 방어력과 마법 저항 계산을 연결할 수 있도록 데이터 구조까지 분리했다.

---
## 다음 개발 방향

다음 단계에서는 적 행동 데이터, 적 대상 선택 규칙, 행동 예고 UI를 구현하고 물리·마법 피해 유형을 방어력 계산에 연결한다.

---
## Commit

`13일차 : 카드 피해·회복 효과 및 상세 정보 UI 구축`
