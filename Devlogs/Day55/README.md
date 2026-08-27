# 55일차 개발일지

## 개발 목표

Prototype v0.1 휴식 방 기능을 구축하고 탐사 중 유지되는 파티 HP·정신력과 회차 덱의 카드 강화 상태를 연결한다.

## 주요 구현 내용

### 1. 휴식 회복 규칙 구현

- 일반 지역 휴식 시 생존 파티원의 최대 HP 기준 25% 회복
- 고위험 지역 휴식 시 생존 파티원의 최대 HP 기준 15% 회복
- 생존 파티원 정신력 +15 회복
- HP와 정신력은 각각 최대값을 넘지 않도록 제한
- 사망 캐릭터는 휴식으로 부활하지 않으며 HP·정신력 회복 대상에서 제외

### 2. 탐사 영속 파티 상태 연동

- 기존 `BattleResultManager`가 보관하는 탐사 중 HP·정신력 상태를 휴식 결과에 반영
- 휴식 이후 다음 전투에서도 회복된 상태를 이어서 사용할 수 있도록 기존 상태 영속 흐름을 재사용
- 파티 상태 변경 후 탐사 HUD 갱신 이벤트가 발생하도록 연결

### 3. 회차 카드 강화 상태 추가

- `RunDeckCardEntry`에 회차 전용 강화 단계 추가
- Prototype v0.1에서는 카드 한 장당 최대 1회 강화 가능
- 원본 `CardData` ScriptableObject는 수정하지 않고 현재 탐사 회차에만 강화 상태를 저장
- `RunDeckManager`에서 카드 강화 가능 여부와 강화 실행 기능 제공
- 상점에서 추가하거나 제거한 카드와 동일한 회차 덱 흐름을 유지

### 4. 전투 카드 강화 적용

- `CardInstance`가 회차 덱의 강화 단계를 전달받도록 확장
- 강화 카드의 기본 효과 수치를 Prototype 기준 25% 증가
- 정신력 변화 효과도 절대값 기준 25% 증가 후 원래 부호 유지
- 강화된 카드는 전투 표시 이름에 `+`를 추가하여 구분

### 5. Prototype 휴식 방 테스트 UI

- 탐사 Scene에서 휴식 테스트 UI를 런타임 자동 생성
- 화면 우측 상단의 `휴식 방 테스트` 버튼으로 접근
- 일반 지역 / 고위험 지역을 테스트용으로 전환 가능
- 현재 탐사 파티의 HP·정신력·생존 상태 표시
- 현재 회차 덱에서 미강화 카드 한 장을 선택하여 강화
- 휴식 사용 후 같은 Prototype 휴식을 다시 사용할 수 없도록 제한
- 반복 기능 확인을 위한 사용 상태 초기화 버튼 추가

### 6. 테스트 코드 추가

`RestRoomPrototypeTests`에 다음 규칙 검증 항목을 추가했다.

- 일반 휴식 HP 25% 회복
- 고위험 휴식 HP 15% 회복
- HP 최대치 초과 방지
- 사망 캐릭터 부활 방지
- 정신력 +15 및 최대치 제한
- 카드 강화 효과 수치 25% 증가

## 생성 및 수정 파일

### 수정

- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/Scripts/Shop/RunDeckCardEntry.cs`
- `Assets/_ProjectC/Scripts/Shop/RunDeckManager.cs`

### 생성

- `Assets/_ProjectC/Scripts/Rest/RestRoomRecoveryService.cs`
- `Assets/_ProjectC/Scripts/Rest/RestRoomRunManager.cs`
- `Assets/_ProjectC/Scripts/Rest/RestRoomPrototypeBootstrap.cs`
- `Assets/_ProjectC/Scripts/Rest/RestRoomPrototypeView.cs`
- `Assets/_ProjectC/Tests/Editor/RestRoomPrototypeTests.cs`

## 현재 Prototype 임시 처리

- 실제 탐사 지역의 고위험 판정 데이터가 아직 연결되지 않아 휴식 테스트 UI의 토글로 일반/고위험을 전환한다.
- 휴식 회복은 기존 `BattleResultManager`의 영속 상태 저장소에 연결해 동작하도록 구성했다.
- 휴식 방의 절차 생성 및 실제 맵 배치는 56일차 특수 방 생성 규칙 구현에서 연결한다.
- 카드 강화 수치 +25%는 Prototype 검증용 임시 규칙이며 이후 카드별 강화 데이터가 확정되면 교체한다.

## 테스트 방법

1. 탐사 Scene을 실행한다.
2. 화면 우측 상단의 `휴식 방 테스트` 버튼을 누른다.
3. 일반 또는 고위험 테스트 지역을 선택한다.
4. 미강화 카드 한 장을 선택한다.
5. `휴식 실행`을 누른다.
6. 생존 파티원의 HP와 정신력 회복을 확인한다.
7. 사망 캐릭터가 부활하지 않는지 확인한다.
8. 선택 카드가 강화 상태로 표시되는지 확인한다.
9. 다음 전투에 진입하여 강화 카드에 `+` 표시와 증가된 효과 수치가 적용되는지 확인한다.
10. Unity Test Runner의 Editor 테스트에서 `RestRoomPrototypeTests`를 실행한다.

## 다음 개발 연결

56일차에서는 휴식 방을 포함한 특수 방 생성 규칙을 절차 생성 시스템에 연결한다.
