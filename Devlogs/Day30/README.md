# 30일차 개발 일지

---
## 개발 목표

- 플레이어에게 게임 진행 동안 유지되는 레벨과 경험치 구조 추가
- 전투 중 카드 사용으로 플레이어 경험치 획득
- 레벨업 시 마이너 카드 선택권을 획득하고 다음 플레이어 턴 시작 시 선택 처리
- 선택한 마이너 카드의 효과를 현재 전투 동안 아군 또는 적군에게 적용
- 마이너 카드 테스트를 위한 더미 아군 추가
- 기존 아이템 슬롯과 유물 디버그 UI를 필요할 때만 열 수 있도록 전투 테스트 UI 개선

---
## 구현 내용

### 1. 플레이어 레벨 및 경험치 시스템 구축

카드 자체가 경험치를 가지는 구조 대신 플레이어가 공용 레벨과 경험치를 가지도록 구성했다.

기본 테스트 설정:

- 시작 레벨: Lv.1
- Lv.1 → Lv.2 필요 EXP: 5
- 레벨이 오를 때마다 다음 필요 EXP +3
- 카드 정상 사용 1회: EXP +1
- 레벨업 1회당 마이너 카드 선택권 +1
- 마이너 카드 선택지: 최대 3장

`PlayerLevelRunManager`를 `DontDestroyOnLoad` 방식으로 유지하여 전투 Scene이 변경되어도 현재 레벨과 경험치가 유지되도록 했다.

### 2. 카드 사용과 경험치 획득 연결

기존 전투 공용 이벤트의 `CardUsed` 이벤트를 사용해 카드가 정상적으로 사용됐을 때 플레이어 경험치를 증가시키도록 연결했다.

처리 흐름:

`카드 사용 → CardUsed 이벤트 → EXP 획득 → 필요 EXP 확인 → 레벨업 → 마이너 카드 선택권 획득`

한 번의 경험치 획득으로 여러 레벨 조건을 만족할 경우 연속 레벨업도 처리할 수 있도록 구성했다.

### 3. 마이너 카드 선택 시점 구축

레벨업 순간 선택 화면을 바로 표시하지 않고 마이너 카드 선택권만 저장한다.

이후 적 턴이 종료되고 다음 플레이어 턴이 시작될 때 대기 중인 선택권을 확인한다.

처리 흐름:

`플레이어 턴 → 레벨업 → 선택권 저장 → 적 턴 → 다음 플레이어 턴 → 마이너 카드 선택`

선택해야 할 마이너 카드가 있을 경우 전투 화면 위에 선택 UI를 표시하고 뒤쪽 전투 UI 입력을 차단한다.

### 4. 연속 마이너 카드 선택 처리

한 번에 여러 레벨이 상승해 선택권이 여러 개 쌓인 경우 선택권을 잃지 않도록 구성했다.

예시:

`대기 선택권 2회 → 마이너 카드 선택 → 다시 선택지 생성 → 두 번째 마이너 카드 선택 → 전투 진행`

현재 전투에서 이미 선택한 마이너 카드는 같은 전투의 다음 선택 후보에서 제외한다.

### 5. 마이너 카드 데이터 구조 추가

마이너 카드를 일반 손패 카드와 분리된 ScriptableObject 데이터로 관리하도록 구성했다.

주요 데이터:

- 고유 ID
- 표시 이름
- 설명
- 아이콘
- 적용 대상
- 효과 종류
- 효과 수치

현재 지원 대상:

- 모든 아군
- 모든 적

현재 지원 효과:

- 최대 체력 증가
- 공격력 증가
- 물리 방어 증가
- 물리 방어 감소
- 마법 저항 증가
- 마법 저항 감소

### 6. 테스트 마이너 카드 6종 추가

#### 강철의 의지

- 대상: 모든 아군
- 효과: 물리 방어 +3

#### 마력 장막

- 대상: 모든 아군
- 효과: 마법 저항 +3

#### 전투의 열기

- 대상: 모든 아군
- 효과: 공격력 +2

#### 생명의 불씨

- 대상: 모든 아군
- 효과: 최대 체력 +10

#### 갑옷 균열

- 대상: 모든 적
- 효과: 물리 방어 -2

#### 마력 침식

- 대상: 모든 적
- 효과: 마법 저항 -2

초기 구현 중 `강철의 의지`가 최대 체력 증가 효과를 가리키던 데이터 오류를 수정하여 최종적으로 `PhysicalDefenseUp` 효과를 사용하도록 정리했다.

### 7. 현재 전투 한정 마이너 카드 효과 적용

선택한 마이너 카드 효과는 현재 전투의 `BattleUnitRuntime`에 적용한다.

- 최대 체력 효과는 전투 유닛 최대 체력 직접 변경
- 공격력, 물리 방어, 마법 저항 효과는 기존 상태효과 시스템 활용
- 전투가 종료되고 유닛 런타임이 폐기되면 마이너 카드 효과도 다음 전투로 이어지지 않음

상태효과 기반 마이너 카드는 현재 테스트 단계에서 매우 긴 지속시간을 사용해 전투 종료 전까지 유지되도록 처리했다.

### 8. 플레이어 레벨 HUD 추가

전투 화면 상단에 현재 플레이어 진행 상태를 확인할 수 있는 HUD를 추가했다.

표시 정보:

- 현재 레벨
- 현재 EXP
- 다음 레벨 필요 EXP
- 남아 있는 마이너 카드 선택권

예시:

`LV 2   EXP 3 / 8 · 선택 1`

### 9. 마이너 카드 선택 UI 추가

레벨업 선택권이 존재할 때 전투 화면 중앙에 마이너 카드 선택 UI를 표시하도록 구성했다.

- 최대 3개의 선택 카드 표시
- 카드 이름 표시
- 설명 표시
- 적용 대상과 효과 요약 표시
- 한 장 선택 시 즉시 효과 적용
- 선택 중 전체 화면 Raycast 차단
- 선택 완료 후 전투 UI 입력 복구

### 10. PlayerLevelConfig 누락 안전 처리

`BattleMinorCardBootstrap`의 `PlayerLevelConfig`가 Inspector에 연결되지 않은 경우에도 전투가 중단되지 않도록 런타임 기본 설정을 생성한다.

자동 기본값:

- 시작 Lv.1
- 첫 필요 EXP 5
- 레벨당 필요 EXP +3
- 카드 사용 EXP +1
- 선택지 3장

현재 `40_Battle` Scene의 Level Config 슬롯은 비어 있어 런타임 기본 설정을 사용한다.

### 11. 버프 테스트용 더미 아군 추가

마이너 카드의 전체 아군 버프를 확인하기 위해 테스트용 더미 아군을 추가했다.

`Character_BuffDummy`

기본 수치:

- 최대 체력: 100
- 물리 방어: 0
- 마법 저항: 0
- 초기 정신력: 50

`Party_Test`의 두 번째 멤버로 등록해 기존 테스트 캐릭터와 함께 전투에 참가하도록 구성했다.

이를 통해 `모든 아군` 마이너 카드가 복수 아군에게 적용되는지 확인할 수 있게 했다.

### 12. 아이템 창 토글 기능 추가

29일차에 제작한 5 × 2 공용 소모품 슬롯을 항상 표시하지 않고 버튼으로 열고 닫을 수 있도록 변경했다.

- `아이템 열기` 버튼 추가
- 게임 시작 시 아이템 슬롯 창 숨김
- 버튼 클릭 시 아이템 슬롯 표시
- 열린 상태에서는 버튼 문구를 `아이템 닫기`로 변경
- 다시 클릭하면 패널 숨김
- 기존 Alt 슬롯 이동 및 교환 기능 유지

아이템 패널을 숨길 때 `CanvasGroup`을 사용해 화면 표시와 Raycast를 함께 차단하도록 처리했다.

### 13. 유물 디버그 창 초기 상태 변경

기존 유물 디버그 창은 게임 시작 시 자동으로 열리지 않도록 변경했다.

- 게임 시작 시 유물 디버그 패널 숨김
- 기존 `유물 DEBUG` 버튼 유지
- 버튼을 눌렀을 때만 디버그 패널 표시

아이템 창과 유물 디버그 창 모두 기본적으로 닫힌 상태에서 전투를 시작하도록 통일했다.

### 14. 40_Battle Scene 연결

`40_Battle`의 `BattleSystems`에 `BattleMinorCardBootstrap`을 추가하고 테스트 마이너 카드 6종을 카드 풀에 등록했다.

현재 연결된 마이너 카드:

1. 갑옷 균열
2. 전투의 열기
3. 강철의 의지
4. 생명의 불씨
5. 마력 침식
6. 마력 장막

---
## 생성 파일

- `Assets/_ProjectC/Data/MinorCards.meta`
- `Assets/_ProjectC/Data/MinorCards/MINOR_ARMOR_CRACK.asset`
- `Assets/_ProjectC/Data/MinorCards/MINOR_ARMOR_CRACK.asset.meta`
- `Assets/_ProjectC/Data/MinorCards/MINOR_BATTLE_FERVOR.asset`
- `Assets/_ProjectC/Data/MinorCards/MINOR_BATTLE_FERVOR.asset.meta`
- `Assets/_ProjectC/Data/MinorCards/MINOR_IRON_WILL.asset`
- `Assets/_ProjectC/Data/MinorCards/MINOR_IRON_WILL.asset.meta`
- `Assets/_ProjectC/Data/MinorCards/MINOR_LIFE_EMBER.asset`
- `Assets/_ProjectC/Data/MinorCards/MINOR_LIFE_EMBER.asset.meta`
- `Assets/_ProjectC/Data/MinorCards/MINOR_MAGIC_EROSION.asset`
- `Assets/_ProjectC/Data/MinorCards/MINOR_MAGIC_EROSION.asset.meta`
- `Assets/_ProjectC/Data/MinorCards/MINOR_MAGIC_VEIL.asset`
- `Assets/_ProjectC/Data/MinorCards/MINOR_MAGIC_VEIL.asset.meta`
- `Assets/_ProjectC/Data/MinorCards/PLAYER_LEVEL_CONFIG.asset`
- `Assets/_ProjectC/Data/MinorCards/PLAYER_LEVEL_CONFIG.asset.meta`
- `Assets/_ProjectC/Scripts/MinorCards.meta`
- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardBootstrap.cs`
- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardBootstrap.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardController.cs`
- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardController.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardData.cs`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardData.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardEffectType.cs`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardEffectType.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardSelectionView.cs`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardSelectionView.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardTargetType.cs`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardTargetType.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/PlayerLevelConfig.cs`
- `Assets/_ProjectC/Scripts/MinorCards/PlayerLevelConfig.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/PlayerLevelHudView.cs`
- `Assets/_ProjectC/Scripts/MinorCards/PlayerLevelHudView.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/PlayerLevelRunManager.cs`
- `Assets/_ProjectC/Scripts/MinorCards/PlayerLevelRunManager.cs.meta`
- `Assets/_ProjectC/ScriptableObjects/Characters/Character_BuffDummy.asset`
- `Assets/_ProjectC/ScriptableObjects/Characters/Character_BuffDummy.asset.meta`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableWindowToggleView.cs`
- `Assets/_ProjectC/Scripts/Consumables/ConsumableWindowToggleView.cs.meta`
- `Devlogs/Day30/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scenes/40_Battle.unity`
- `Assets/_ProjectC/ScriptableObjects/Parties/Party_Test.asset`
- `Assets/_ProjectC/Scripts/Consumables/BattleConsumableBootstrap.cs`
- `Assets/_ProjectC/Scripts/Relics/BattleRelicBootstrap.cs`

---
## 삭제 파일

- 없음

---
## 검토 결과

- 최신 `main` 커밋이 29일차 커밋보다 정확히 1개 커밋 앞선 상태 확인
- 최신 커밋에 플레이어 레벨·경험치·마이너 카드 시스템이 포함된 것을 확인
- 카드 사용 시 `CardUsed` 이벤트를 통해 EXP를 획득하는 구조 확인
- 레벨업 시 마이너 카드 선택권이 누적되는 구조 확인
- 플레이어 턴 시작 시 대기 선택권을 처리하는 구조 확인
- 여러 선택권이 존재하면 연속 선택하는 구조 확인
- 이미 선택한 마이너 카드가 현재 전투에서 다시 등장하지 않는 구조 확인
- 마이너 카드 6종의 대상과 효과 타입이 설명과 일치하는 것을 확인
- `강철의 의지`가 최종적으로 물리 방어 증가 효과를 사용하도록 수정된 것을 확인
- 버프 테스트용 `Character_BuffDummy`가 `Party_Test`에 추가된 것을 확인
- 아이템 창이 버튼 방식으로 열리고 시작 시 숨겨지는 구조 확인
- 유물 디버그 패널이 시작 시 숨겨지는 구조 확인
- 30일차에서 삭제된 파일이 없는 것을 확인
- GitHub에 등록된 자동 CI 상태 검사는 없음
- 실제 Unity 컴파일 및 Play Mode 전체 동작은 GitHub 소스만으로 자동 검증할 수 없어 최종 수동 확인 필요

---
## Unity에서 직접 확인할 부분

1. `40_Battle` Scene 실행
2. 플레이어 레벨 HUD가 `LV 1 / EXP 0 / 5` 기준으로 표시되는지 확인
3. 테스트 캐릭터와 `Buff Dummy`가 모두 아군으로 등장하는지 확인
4. 카드 5장을 정상 사용했을 때 Lv.2가 되는지 확인
5. 레벨업 직후가 아니라 적 턴 종료 후 다음 플레이어 턴에 마이너 카드 선택창이 표시되는지 확인
6. 선택 화면이 표시되는 동안 뒤쪽 전투 UI가 클릭되지 않는지 확인
7. 마이너 카드 3장 중 하나를 선택하면 즉시 효과가 적용되는지 확인
8. `강철의 의지` 선택 시 두 아군의 물리 방어가 증가하는지 확인
9. `생명의 불씨` 선택 시 두 아군의 최대 체력이 증가하는지 확인
10. 적 대상 마이너 카드 선택 시 모든 생존 적에게 효과가 적용되는지 확인
11. 선택 완료 후 정상적으로 플레이어 턴을 진행할 수 있는지 확인
12. 화면 왼쪽 위에서 아이템 창이 시작 시 닫혀 있는지 확인
13. `아이템 열기`와 `아이템 닫기` 버튼이 정상 작동하는지 확인
14. 아이템 창을 연 뒤 기존 Alt 슬롯 이동 기능이 유지되는지 확인
15. 유물 디버그 패널이 시작 시 닫혀 있는지 확인
16. 기존 `유물 DEBUG` 버튼으로 패널을 열고 닫을 수 있는지 확인
17. 다음 전투로 넘어갔을 때 플레이어 Level/EXP가 유지되는지 확인
18. 이전 전투에서 선택한 마이너 카드 효과가 다음 전투 유닛에게 남지 않는지 확인

---
## 다음 개발 방향

- 플레이어 레벨과 경험치를 실제 런 시작/종료 흐름에 연결
- 마이너 카드 선택 결과를 전투 HUD에서 확인할 수 있는 현재 강화 목록 추가
- 마이너 카드 종류와 효과 데이터 확장
- 전투 종료 및 새 게임 시작 시 플레이어 레벨 진행 상태 초기화 시점 정리
- 테스트용 장기 지속 상태효과를 전투 전용 Modifier 구조로 분리하는 방향 검토

---
## 커밋 제목

`30일차 : 플레이어 레벨·마이너 카드 성장 및 전투 테스트 UI 개선`
