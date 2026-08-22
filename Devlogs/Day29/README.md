# 29일차 개발 일지

---
## 개발 목표

- 포션과 이후 추가될 다른 소비형 아이템이 함께 사용하는 공용 소모품 슬롯 구축
- 플레이어 UI 왼쪽 위에 가로 5칸 × 세로 2칸, 총 10칸의 고정 슬롯 추가
- 소모품 사용 후 빈 슬롯을 자동 정렬하지 않고 기존 위치 유지
- Alt 키를 누른 동안 마우스 커서를 활성화해 슬롯 간 아이템 이동 및 교환 지원
- 전투 중 포션 사용, 대상 선택, 효과 적용, 사용 성공 후 소비 처리 구현

---
## 구현 내용

### 1. 공용 소모품 데이터 기반 구축

포션만을 위한 전용 인벤토리가 아니라 이후 다른 소비형 아이템도 같은 슬롯을 사용할 수 있도록 공용 데이터 구조를 추가했다.

- `ConsumableItemData`를 공용 소모품 기본 데이터로 사용
- 고유 ID, 표시 이름, 설명, 아이콘, 아이템 분류 관리
- `PotionData`가 공용 소모품 데이터를 기반으로 포션 전용 효과 정보를 추가
- 이후 다른 소모형 아이템 종류를 같은 인벤토리에 확장할 수 있는 구조 마련

### 2. 고정 10칸 소모품 인벤토리 구축

- 가로 5칸 × 세로 2칸의 총 10칸 고정 슬롯 사용
- 내부적으로 길이 10의 고정 배열로 슬롯 상태 관리
- 아이템 획득 시 앞에서부터 첫 번째 빈 슬롯에 저장
- 아이템 사용 또는 제거 후 해당 위치만 빈칸 처리
- 뒤에 있는 아이템을 앞으로 자동 이동시키지 않음

예시:

`[포션 A] [빈칸] [포션 B]`

위 상태에서 빈칸이 발생해도 `포션 B`는 자동으로 앞으로 이동하지 않는다.

### 3. 슬롯 이동 및 교환 기능

- Alt 키를 누르고 있는 동안 소모품 정리 모드 활성화
- 정리 모드 시작 시 기존 커서 상태를 저장하고 마우스 커서 표시
- 이동할 아이템이 있는 슬롯을 첫 번째로 선택
- 이동할 대상 슬롯을 두 번째로 선택
- 대상이 빈칸이면 아이템 이동
- 대상 슬롯에 다른 아이템이 있으면 두 슬롯의 아이템을 서로 교환
- 같은 슬롯을 다시 누르면 이동 선택 취소
- Alt 키를 놓으면 미완료 이동 선택을 취소하고 기존 커서 상태 복구

### 4. 소모품 슬롯 UI 추가

전투 UI 왼쪽 위에 공용 소모품 패널을 코드로 생성한다.

배치 구조:

`[1] [2] [3] [4] [5]`
`[6] [7] [8] [9] [10]`

- 각 슬롯에 번호 표시
- 아이콘 표시
- 소모품 이름 표시
- 빈 슬롯과 점유 슬롯을 시각적으로 구분
- 이동 선택 슬롯과 포션 사용 선택 슬롯을 별도 색상으로 강조
- Alt 정리 모드 안내 문구 표시

### 5. 전투 중 포션 사용 시스템

포션은 플레이어 턴에만 사용할 수 있도록 제한했다.

지원하는 기본 포션 효과:

- 체력 회복
- 정신력 변경
- 피해 적용
- 디버프 정화
- 상태 효과 적용

단일 대상 포션은 포션 슬롯 선택 후 전투 유닛을 클릭해 대상을 결정한다.

### 6. 포션 대상 선택 처리

포션 대상 방식에 따라 다음 대상을 지원한다.

- 첫 번째 생존 아군
- 선택한 아군 1명
- 모든 아군
- 선택한 적 1명
- 모든 적

수동 대상이 필요한 포션은 포션 선택 상태를 유지하고 올바른 진영의 유닛을 클릭했을 때만 실행한다.

잘못된 대상을 선택하면 포션은 소비되지 않고 대상 선택 상태를 유지한다.

### 7. 사용 성공 후 포션 소비

포션은 슬롯을 먼저 비우지 않고 실제 효과 적용이 성공한 뒤 소비한다.

처리 순서:

`포션 선택 → 대상 확인 → 효과 적용 → 적용 성공 확인 → 해당 슬롯 비우기`

따라서 다음 상황에서는 포션을 소비하지 않는다.

- 유효한 대상이 없음
- 대상이 사망 상태
- 체력이 이미 가득 차 회복량이 0
- 정신력 변화가 적용되지 않음
- 제거할 디버프가 없음
- 상태 효과 적용 실패

### 8. 테스트 포션 데이터 추가

전투 기능 확인을 위해 테스트 포션 3종을 추가했다.

#### 테스트 체력 포션

- ID: `POTION_TEST_HEAL`
- 선택한 아군 대상
- 체력 30 회복

#### 테스트 정신 포션

- ID: `POTION_TEST_MENTAL`
- 선택한 아군 대상
- 정신력 10 증가

#### 테스트 공격 포션

- ID: `POTION_TEST_BOMB`
- 선택한 적 대상
- 물리 피해 20 적용

### 9. 전투 Scene 연결

- `40_Battle`의 `BattleSystems`에 `BattleConsumableBootstrap` 추가
- 기존 전투 초기화 완료 후 소모품 시스템 초기화
- 테스트 포션 3종을 시작 소모품으로 연결
- 전투 Canvas를 자동으로 찾아 소모품 UI 생성
- Scene 종료 시 슬롯 UI와 전투 소모품 이벤트 연결 해제

### 10. 정신력 변화 원인 확장

포션 등 소모품으로 정신력이 변경된 경우를 기존 카드 효과와 구분하기 위해 다음 원인을 추가했다.

- `ConsumableEffect`

기존 `BattleMentalChangeReason` 값 뒤에 추가해 기존 값 순서를 유지했다.

### 11. Scene 종료 NullReferenceException 수정

초기 구현에서 Play Mode 종료 시 다음 경로에서 `NullReferenceException`이 발생했다.

`BattleConsumableController.Dispose()`
`→ BattleConsumableBootstrap.OnDestroy()`

원인은 `BattleSceneSetup`이 먼저 종료되면서 `BattleTurn` 참조를 정리한 뒤 소모품 컨트롤러가 해당 값을 다시 조회한 것이었다.

수정 후에는:

- `BattleConsumableController` 생성 시 `BattleTurnRuntime`을 별도로 저장
- 턴 상태 확인도 저장된 참조 사용
- `Dispose()`에서도 저장된 `battleTurn`이 존재하는 경우에만 이벤트 연결 해제

하도록 변경했다.

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Consumables/BattleConsumableBootstrap.cs`
- `Assets/_ProjectC/Scripts/Consumables/BattleConsumableController.cs`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableInventoryRuntime.cs`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableItemCategory.cs`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableItemData.cs`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableRunManager.cs`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableSlotBarView.cs`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableTargetType.cs`
- `Assets/_ProjectC/Scripts/Consumables/PotionData.cs`
- `Assets/_ProjectC/Scripts/Consumables/PotionEffectType.cs`
- 위 스크립트와 폴더의 Unity `.meta` 파일
- `Assets/_ProjectC/Data/Consumables/POTION_TEST_HEAL.asset`
- `Assets/_ProjectC/Data/Consumables/POTION_TEST_MENTAL.asset`
- `Assets/_ProjectC/Data/Consumables/POTION_TEST_BOMB.asset`
- 위 테스트 데이터와 폴더의 Unity `.meta` 파일
- `Devlogs/Day29/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scenes/40_Battle.unity`
- `Assets/_ProjectC/Scripts/Battle/BattleMentalChangeReason.cs`

---
## 삭제 파일

- 없음

---
## 검토 결과

- 최신 `main` 커밋이 28일차 커밋에서 정확히 1개 커밋 앞선 상태 확인
- 최신 커밋에 29일차 소모품 시스템과 테스트 포션 데이터가 포함된 것을 확인
- 고정 슬롯이 가로 5 × 세로 2, 총 10칸으로 정의된 것을 확인
- 소모품 사용 후 슬롯을 자동 정렬하지 않는 구조 확인
- 새 소모품은 첫 번째 빈 슬롯에 획득되는 구조 확인
- Alt 정리 모드에서 빈칸 이동과 점유 슬롯 교환을 지원하는 구조 확인
- Alt 활성화 시 커서 잠금 해제 및 표시, 종료 시 기존 상태 복구 구조 확인
- 포션 효과 성공 후에만 해당 슬롯을 소비하도록 처리된 것을 확인
- `40_Battle`에 테스트 포션 3종과 `BattleConsumableBootstrap` 연결 확인
- Scene 종료 시 발생했던 `Dispose()` NullReferenceException 방지 코드가 최신 `main`에 반영된 것을 확인
- GitHub에 등록된 자동 CI 상태 검사는 없음
- 실제 Unity Play Mode 전체 동작은 GitHub 소스만으로 자동 검증할 수 없어 최종 수동 확인 필요

---
## Unity에서 직접 확인할 부분

1. `40_Battle` Scene 실행
2. 화면 왼쪽 위에 5 × 2 소모품 슬롯이 표시되는지 확인
3. 테스트 포션 3종이 슬롯 1~3에 표시되는지 확인
4. 체력 포션을 사용하면 해당 슬롯만 빈칸으로 남는지 확인
5. 뒤쪽 아이템이 빈 슬롯 앞으로 자동 이동하지 않는지 확인
6. Alt 키를 누르면 마우스 커서가 활성화되는지 확인
7. Alt 상태에서 아이템 슬롯 → 빈 슬롯 클릭 시 이동하는지 확인
8. Alt 상태에서 아이템 슬롯 → 점유 슬롯 클릭 시 서로 교환되는지 확인
9. Alt 키를 놓으면 이동 선택과 커서 상태가 정상 복구되는지 확인
10. 체력 포션이 선택한 아군을 회복시키는지 확인
11. 정신 포션이 선택한 아군의 정신력을 변경하는지 확인
12. 공격 포션이 선택한 적에게 피해를 주는지 확인
13. 효과가 적용되지 않은 포션이 소비되지 않는지 확인
14. Play Mode 종료 시 `BattleConsumableController.Dispose()` 관련 `NullReferenceException`이 발생하지 않는지 확인

---
## 다음 개발 방향

- 마이너 카드 경험치 시스템 기본 구조 구축
- 카드별 경험치 데이터와 전투 중 경험치 획득 규칙 분리
- 경험치 누적과 레벨 또는 성장 단계 연결을 위한 런타임 구조 준비

---
## 커밋 제목

`29일차 : 공용 소모품 슬롯·포션 사용 및 Alt 정리 시스템 구축`
