# 28일차 개발 일지

---
## 개발 목표

- Slay the Spire 방식의 유물 획득 및 보유 구조 구축
- 같은 시점에 발동하는 유물을 획득 순서대로 순차 처리
- 동일 유물 재획득 시 중복 보유 대신 골드로 변환
- 유물 제거 시 뒤에 있는 유물의 순서를 자동으로 앞으로 당김
- 현재 보유 유물과 획득 순서를 확인할 수 있는 디버그 ScrollView 추가

---
## 구현 내용

### 1. 유물 데이터 구조 구축

- `RelicData` ScriptableObject 추가
- 유물 ID, 이름, 설명, 아이콘, 희귀도 데이터 분리
- 발동 시점과 실제 효과 종류를 데이터에서 설정
- 효과 대상과 효과 수치를 데이터에서 설정
- 턴당·전투당 최대 발동 횟수 설정 지원
- 동일 유물 재획득 시 변환할 골드 수치를 유물별로 설정

### 2. 획득 순서 기반 유물 보관함 구축

- 보유 유물을 `List` 순서대로 저장
- 먼저 획득한 유물이 목록 앞쪽에 유지되도록 구성
- 유물 효과 실행 시 현재 보유 목록을 앞에서부터 순서대로 처리
- 유물 제거 시 `RemoveAt`으로 목록을 정리해 뒤 유물의 순서 자동 당김
- 현재 유물 순번을 ID 기준으로 다시 조회할 수 있도록 구현

### 3. 중복 유물 골드 변환

- 동일 `RelicId`를 가진 유물을 이미 보유 중인지 검사
- 중복 유물은 보유 목록에 추가하지 않음
- 중복된 유물의 `DuplicateGoldValue`만큼 골드 지급
- 현재는 유물 시스템용 임시 골드 런타임을 사용
- 테스트 유물의 중복 변환 값은 50 Gold로 설정

### 4. 전투 공용 이벤트와 유물 연결

- 27일차에 구축한 `BattleEventDispatcher`를 유물 시스템에서 구독
- 전투 시작, 턴 시작·종료, 카드 사용, 피해, 회복, 상태 효과, 정신력 변화, 처치, 전투 종료 시점 지원
- 같은 발동 시점의 유물을 획득 순서대로 즉시 처리
- 유물 효과 처리 중 발생한 후속 발동은 대기열을 사용해 기존 순서가 끝난 뒤 처리
- 턴당·전투당 발동 횟수 제한을 유물별로 관리

### 5. 유물 효과 기본 구현

현재 기본 효과 종류를 추가했다.

- 최대 체력 증가
- 현재 체력 회복
- 골드 획득
- 피해 적용
- 상태 이상 적용

최대 체력 변경 유물을 지원하기 위해 `BattleUnitRuntime.MaxHealth`를 런타임에서 변경할 수 있도록 수정하고 `ModifyMaxHealth` 기능을 추가했다.

### 6. 유물 획득 순서에 따른 연속 효과 처리

예를 들어 다음 순서로 유물을 보유한 경우:

`최대 체력 +10 → 체력 회복 +10`

효과는 한꺼번에 계산하지 않고 다음 순서대로 즉시 반영한다.

1. 최대 체력 +10 적용
2. 변경된 최대 체력을 기준으로 체력 회복 +10 적용

따라서 같은 유물 조합이라도 획득 순서에 따라 최종 결과가 달라질 수 있다.

### 7. 유물 디버그 창 추가

- 전투 Canvas에 코드 기반 유물 디버그 창 생성
- 우측 상단 `유물 DEBUG` 버튼으로 표시 상태 전환
- 현재 보유 골드 표시
- ScrollView 내부에 유물을 가로 5개씩 배치
- 각 유물 칸 왼쪽 위에 현재 순서 번호 표시
- 유물 아이콘과 이름 표시
- 디버그용 제거 버튼 추가
- 중간 유물 제거 시 목록을 다시 그려 뒤 유물의 번호를 즉시 앞으로 당김

### 8. 전투 Scene 연결

- `40_Battle`의 `BattleSystems`에 `BattleRelicBootstrap` 연결
- 기존 전투 초기화가 완료된 뒤 유물 시스템을 연결하도록 처리
- 전투 이벤트 연결 전 이미 지나간 전투 시작과 첫 턴 이벤트를 별도로 보정
- 전투 Scene을 벗어날 때 유물 전투 이벤트와 디버그 UI 연결 해제

### 9. Unity 최신 API 경고 수정

- `FindObjectOfType<T>()`를 `FindFirstObjectByType<T>()`로 교체
- `TMP_Text.enableWordWrapping`을 `textWrappingMode`로 교체
- Unity 6.3에서 발생한 obsolete API 경고 제거

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Relics/BattleRelicBootstrap.cs`
- `Assets/_ProjectC/Scripts/Relics/BattleRelicEffectController.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicAcquireResult.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicData.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicDebugWindow.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicEffectType.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicGoldRuntime.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicInventoryRuntime.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicRarity.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicRunManager.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicTargetType.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicTriggerType.cs`
- 위 스크립트와 폴더의 Unity `.meta` 파일
- `Assets/_ProjectC/Data/Relics/RELIC_TEST_MAX_HP.asset`
- `Assets/_ProjectC/Data/Relics/RELIC_TEST_HEAL.asset`
- 위 테스트 데이터의 Unity `.meta` 파일
- `Devlogs/Day28/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scenes/40_Battle.unity`

---
## 삭제 파일

- 없음

---
## 검토 결과

- 최신 `main` 커밋이 27일차 커밋에서 1개 커밋 앞선 상태임을 확인
- 최신 커밋에 28일차 유물 시스템 파일과 Battle Scene 연결이 포함된 것을 확인
- 유물 보유 목록이 획득 순서를 유지하도록 구현된 것을 확인
- 중복 유물이 골드로 변환되고 보유 목록에는 추가되지 않는 것을 확인
- 유물 제거 시 뒤 유물의 현재 순번이 자동으로 다시 계산되는 구조 확인
- ScrollView의 GridLayout이 고정 5열로 설정된 것을 확인
- `FindFirstObjectByType`와 `textWrappingMode`를 사용해 기존 obsolete API 경고 수정 확인
- GitHub에 등록된 자동 CI 검사 결과는 없음
- Unity Play Mode 실제 실행 검증은 GitHub만으로 확인할 수 없어 수동 확인 필요

---
## Unity에서 직접 확인할 부분

1. `40_Battle` Scene의 `BattleSystems` 오브젝트 선택
2. `BattleRelicBootstrap`의 `Debug Starting Relics` 크기를 3으로 설정
3. 다음 순서로 테스트 유물 연결
   - Element 0: `RELIC_TEST_MAX_HP`
   - Element 1: `RELIC_TEST_HEAL`
   - Element 2: `RELIC_TEST_MAX_HP`
4. Play Mode 실행
5. 첫 번째 유물로 최대 체력이 먼저 증가하는지 확인
6. 두 번째 유물의 체력 회복이 증가된 최대 체력을 기준으로 적용되는지 확인
7. 세 번째 중복 유물이 추가되지 않고 50 Gold로 변환되는지 확인
8. 유물 디버그 창에서 유물이 가로 5개씩 배치되는지 확인
9. 각 유물 왼쪽 위에 `1`, `2` 등의 현재 순번이 표시되는지 확인
10. 중간 유물을 제거했을 때 뒤 유물의 번호가 앞으로 당겨지는지 확인
11. Console에 CS0618 경고가 다시 발생하지 않는지 확인

현재 GitHub의 `40_Battle` Scene에는 `debugStartingRelics`가 빈 목록으로 저장되어 있으므로 위 테스트를 위해 Inspector에서 직접 연결해야 한다.

---
## 다음 개발 방향

- 포션 기본 시스템 구축
- 포션 데이터와 보유 슬롯 구조 분리
- 전투 중 포션 사용 및 대상 선택 처리
- 포션 사용 후 소비 처리
- 유물 시스템과 동일한 전투 공용 이벤트 구조를 활용할 수 있도록 연결 기반 준비

---
## 커밋 제목

`28일차 : 획득 순서 기반 유물 및 중복 골드 변환 시스템 구축`
