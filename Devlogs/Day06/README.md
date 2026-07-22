# 6일차: 전투 유닛 및 체력 UI 구현

## 개발 목표

전투 씬에 아군과 적 유닛을 배치하고, 체력 변화가 HP 바와 수치에 즉시 표시되는 기본 전투 구조를 구현한다.

## 구현 내용

### 1. 전투 유닛 체력 시스템

- `BattleUnit.cs` 생성
- 유닛 이름, 최대 체력, 현재 체력 관리
- 피해 적용 기능 구현
- 체력 회복 기능 구현
- 체력이 최대 체력을 넘지 않도록 제한
- 체력이 0 아래로 내려가지 않도록 제한
- 체력 변경 시 UI에 알림을 전달하는 이벤트 구성

### 2. 아군·적 테스트 유닛 생성

- 아군 테스트 유닛 `THE FOOL` 생성
- 적 테스트 유닛 `TEST MUTATION` 생성
- 아군 최대 체력 `100` 설정
- 적 최대 체력 `120` 설정
- 각 유닛에 `BattleUnit` 컴포넌트 연결
- 아군·적 유닛 프리팹 생성

### 3. 체력 UI 구현

- `BattleUnitHealthUI.cs` 생성
- 유닛 이름 표시
- 현재 체력과 최대 체력 표시
- 체력 비율에 따라 HP Slider 갱신
- 아군 상태 패널 구성
- 적 상태 패널 구성
- 아군과 적의 체력 UI를 각각 연결

### 4. 전투 테스트 기능 구현

- `BattleTestController.cs` 생성
- 아군 피해 버튼 구현
- 아군 회복 버튼 구현
- 적 피해 버튼 구현
- 적 회복 버튼 구현
- 버튼 클릭 시 대상 유닛의 HP와 HP 바가 함께 변경되도록 연결

## 추가 및 수정된 주요 파일

- `Assets/_ProjectC/Scripts/Battle/BattleUnit.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTestController.cs`
- `Assets/_ProjectC/Scripts/UI/BattleUnitHealthUI.cs`
- `Assets/_ProjectC/Prefabs/Battle/PF_BattleUnit_Ally.prefab`
- `Assets/_ProjectC/Prefabs/Battle/PF_BattleUnit_Enemy.prefab`
- `Assets/_ProjectC/Scenes/20_Battle.unity`

## 테스트 결과

- [x] 아군 이름과 HP 표시
- [x] 적 이름과 HP 표시
- [x] 아군 피해 적용
- [x] 아군 회복 적용
- [x] 적 피해 적용
- [x] 적 회복 적용
- [x] HP Slider 동기화
- [x] 최대 체력 초과 방지
- [x] 0 미만 체력 방지
- [x] Console 빨간색 오류 확인

## 완료 결과

`20_Battle` 씬에서 아군과 적 전투 유닛의 기본 체력 관리 및 체력 UI 표시 기능을 완성했다. 테스트 버튼으로 피해와 회복을 적용할 수 있으며, HP 수치와 Slider가 즉시 함께 갱신된다.