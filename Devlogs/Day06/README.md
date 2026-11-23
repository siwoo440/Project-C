---
# 6일차 개발일지 - 전투 유닛 생성 및 HP 시스템 구축

---
## 개발 목표

5일차에서 완성한 `BattleLoadoutData`, `PartyData`, `EnemyData`를 이용해 전투 씬에 아군과 적 유닛을 생성하고, 전투 중 변경되는 HP와 사망 상태를 원본 ScriptableObject와 분리하여 관리하는 것을 목표로 진행했다.

6일차 완료 기준은 다음과 같다.

- 아군·적 전투 유닛 생성
- 현재 HP와 최대 HP 분리
- 피해 처리
- 사망 상태 처리
- HP UI 갱신
- 최대 아군 4명·적 4명 배치 영역 구성
- 이후 카드 UI를 위한 하단 영역 확보

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Project Type | 2D |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| Battle Unit Prefab | `Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab` |

---
## 개발 구조

전투 원본 데이터와 전투 중 변경되는 상태를 다음과 같이 분리했다.

```text
CharacterData / EnemyData
          ↓ 런타임 변환
BattleUnitRuntime
          ↓ 이벤트 연결
BattleUnitView
```

`CharacterData`와 `EnemyData`는 원본 데이터로 유지하고, 현재 HP와 사망 여부는 `BattleUnitRuntime`에서만 변경한다.

---
## 1. 전투 진영 구조

`BattleTeam` 열거형을 추가하여 전투 유닛을 다음 진영으로 구분했다.

- `Ally`: 아군
- `Enemy`: 적

진영 값은 유닛 UI 색상과 향후 카드 대상 지정에 사용할 수 있다.

---
## 2. BattleUnitRuntime 구현

전투 중 변경되는 유닛 상태를 관리하기 위해 `BattleUnitRuntime`을 구현했다.

포함 상태:

- 유닛 고유 ID
- 표시 이름
- 진영
- 초상화
- 최대 HP
- 현재 HP
- 사망 여부
- 아군 또는 적 원본 데이터 참조

생성 기능:

- `CharacterData`에서 아군 런타임 유닛 생성
- `EnemyData`에서 적 런타임 유닛 생성

피해 처리 규칙:

- 0 이하의 피해 무시
- 사망한 유닛의 추가 피해 무시
- 현재 HP를 0 이상으로 제한
- HP 변경 시 `HealthChanged` 이벤트 발생
- HP가 처음 0이 될 때 `Died` 이벤트 1회 발생
- 실제 적용된 피해량 반환

사망한 유닛 오브젝트는 즉시 제거하지 않고 상태로 유지한다. 이후 대상 지정, 처치 이벤트, 부활 시스템에서 해당 유닛을 참조할 수 있도록 하기 위한 구조다.

---
## 3. BattleUnitView 구현

`BattleUnitRuntime`의 상태를 화면에 표시하기 위해 `BattleUnitView`를 구현했다.

연결 UI:

- 유닛 초상화
- 유닛 이름
- 진영별 테두리 색상
- HP Slider
- 현재 HP / 최대 HP 텍스트
- 사망 표시

`HealthChanged` 이벤트가 발생하면 HP 게이지와 숫자를 갱신하고, `Died` 이벤트가 발생하면 사망 표시를 활성화한다.

오브젝트가 제거될 때 이벤트 구독을 해제하여 남은 런타임 참조를 방지한다.

---
## 4. BattleSceneSetup 구현

`40_Battle` 씬에서 전투 유닛을 생성하는 `BattleSceneSetup`을 구현했다.

초기화 순서:

1. `BattleLoadoutData` 유효성 검사
2. 유닛 프리팹과 배치 영역 확인
3. 파티 캐릭터를 아군 런타임 유닛으로 변환
4. 적 데이터를 적 런타임 유닛으로 변환
5. 공용 유닛 프리팹 생성
6. 런타임 상태와 UI 연결

잘못된 전투 편성, 누락된 프리팹, 빈 적 데이터가 발견되면 오류를 출력하고 초기화를 중단한다.

피해 동작을 확인할 수 있도록 다음 Context Menu 테스트 기능을 추가했다.

- 첫 번째 아군 피해
- 첫 번째 적 피해

---
## 5. 전투 유닛 프리팹

아군과 적이 공통으로 사용하는 `PF_BattleUnit` 프리팹을 제작했다.

```text
PF_BattleUnit
├─ Panel
├─ Portrait
├─ NameText
├─ HealthSlider
├─ HealthText
└─ DeadMarker
   └─ DeadText
```

프리팹 기본 크기는 `175×250`이며, 진영에 따라 테두리 색상이 변경된다.

---
## 6. 전투 화면 배치

참고 화면 구조에 맞춰 상단을 전투 유닛 영역으로 사용하고 하단을 카드 영역으로 분리했다.

| 영역 | 화면 비율 |
|---|---|
| 아군 1~4 | X 3~49%, Y 47~96% |
| 적 1~4 | X 51~97%, Y 47~96% |
| 카드 예약 영역 | X 3~97%, Y 3~42% |
| 완충 영역 | Y 42~47% |

아군과 적은 각각 `HorizontalLayoutGroup`으로 정렬하며, 최대 4명씩 배치할 수 있다.

하단에는 `CardAreaRoot`를 추가하여 이후 손패와 카드 선택 UI가 전투 유닛 영역을 침범하지 않도록 했다.

---
## 7. Editor 자동 구성 도구

`Day06BattleSetupEditor`를 추가하여 다음 작업을 자동화했다.

- BattleCanvas 생성
- 아군·적 배치 영역 생성
- 카드 예약 영역 생성
- 전투 유닛 프리팹 생성
- BattleSystems 생성
- 테스트 BattleLoadout과 Enemy 데이터 연결
- 씬과 프리팹 저장

자동 구성 이후에도 동일 메뉴를 다시 실행하면 현재 6일차 배치 기준으로 씬과 프리팹을 갱신할 수 있다.

---
## 생성·수정된 주요 파일

```text
Assets/_ProjectC
├─ Editor
│  └─ Day06BattleSetupEditor.cs
├─ Prefabs/Common
│  └─ PF_BattleUnit.prefab
├─ Scenes
│  └─ 40_Battle.unity
└─ Scripts/Battle
   ├─ BattleTeam.cs
   ├─ BattleUnitRuntime.cs
   ├─ BattleUnitView.cs
   └─ BattleSceneSetup.cs
```

기존 `CharacterData`, `EnemyData`, `PartyData`, `DeckData`, `BattleLoadoutData`는 수정하지 않고 그대로 사용했다.

---
## 검증 결과

- C# 구문 오류 0건
- 코드 주석 누락 0건
- `Assembly-CSharp.dll`에서 전투 런타임 타입 확인
- `Assembly-CSharp-Editor.dll`에서 자동 구성 도구 타입 확인
- 씬 직렬화 ID 중복 0건
- 프리팹 직렬화 ID 중복 0건
- 프리팹 UI 참조 누락 0건
- BattleLoadout 테스트 데이터 GUID 일치
- Enemy 테스트 데이터 GUID 일치
- 전투 유닛 프리팹 GUID 일치
- 아군·적 영역과 카드 영역 사이 5% 완충 구간 확인

Unity Play Mode에서의 최종 시각 배치와 Context Menu 피해 동작은 자동 검증 환경에서 실행하지 못했으므로 실제 Editor에서 최종 확인이 필요하다.

---
## 6일차 결과

5일차의 전투 편성 데이터를 기반으로 아군과 적을 런타임 전투 유닛으로 생성하고, 원본 데이터를 변경하지 않는 HP·피해·사망 상태 구조를 구축했다.

HP 변경과 사망 상태가 UI에 반영되는 이벤트 구조를 추가했으며, 화면 상단에는 최대 아군 4명과 적 4명이 마주 보는 전투 영역을 구성했다. 화면 하단에는 이후 카드 시스템에서 사용할 독립적인 카드 영역을 확보했다.

다음 단계에서는 원본 `CardData`를 변경하지 않는 전투용 `CardInstance` 생성 구조를 구현한다.
