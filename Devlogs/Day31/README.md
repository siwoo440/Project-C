# 31일차 개발 일지

---
## 개발 목표

- 플레이어 Level/EXP를 게임 전체 진행이 아닌 전투 단위 성장 요소로 변경
- 새로운 몬스터 전투에 진입할 때마다 Level/EXP를 초기 상태로 시작
- 전투 중 획득한 마이너 카드 효과가 전투 종료 후 다음 전투로 이어지지 않도록 정리
- 기존 맵 진행에서 유지되는 HP·정신력·유물·소모품 구조는 그대로 유지
- 현재 전투에서 선택한 마이너 카드를 확인할 수 있는 강화 목록 UI 추가

---
## 구현 내용

### 1. Level/EXP 수명을 전투 단위로 변경

기존 `PlayerLevelRunManager`는 `DontDestroyOnLoad`를 사용해 Scene이 변경되어도 Level/EXP를 유지했다.

현재 게임 진행은 하나의 맵에서 몬스터를 만나 전투에 진입하고, 전투가 끝난 뒤 다시 맵 진행을 이어가며 다음 몬스터를 만나는 구조이므로 Level/EXP는 각 전투마다 새로 시작하도록 변경했다.

변경 후 기본 흐름:

`맵 진행 → 몬스터 조우 → 전투 진입 → Lv.1 / EXP 0 → 전투 성장 → 전투 종료 → 맵 복귀 → 다음 전투에서 다시 Lv.1 / EXP 0`

### 2. PlayerLevelRunManager의 Scene 유지 제거

`PlayerLevelRunManager`에서 `DontDestroyOnLoad` 사용을 제거했다.

따라서 전투 Scene이 종료되면 해당 레벨 관리자도 함께 제거된다.

또한 같은 전투 Scene을 다시 초기화하는 상황에서도 이전 전투 수치가 남지 않도록 `BeginBattle()`을 추가했다.

`BeginBattle()` 처리:

- 시작 레벨 적용
- 현재 EXP 0
- 대기 중 마이너 카드 선택권 0

### 3. 전투 시작 시 Level/EXP 강제 초기화

`BattleMinorCardBootstrap` 초기화 과정에서 다음 처리를 추가했다.

`PlayerLevelRunManager.EnsureInstance()`
`→ BeginBattle()`
`→ BattleMinorCardController 생성`
`→ 레벨 HUD 및 마이너 카드 UI 생성`

이를 통해 이전 전투의 Level/EXP 상태와 관계없이 매 전투를 동일한 초기 성장 상태에서 시작한다.

### 4. 마이너 카드 효과 추적 구조 추가

전투 중 마이너 카드가 실제 전투 유닛에게 적용한 효과를 추적하기 위해 `BattleMinorCardEffectRegistry`를 추가했다.

추적 대상:

- 공격력 증가
- 물리 방어 증가
- 물리 방어 감소
- 마법 저항 증가
- 마법 저항 감소
- 최대 체력 증가

전투 시작 시 `BeginBattle()`을 호출해 이전 추적 데이터를 비운다.

### 5. 마이너 카드 효과 적용 경로 통합

기존에는 `BattleMinorCardController`가 직접 상태효과와 최대 체력 증가를 적용했다.

31일차부터는:

`마이너 카드 선택`
`→ BattleMinorCardEffectRegistry.Apply()`
`→ 대상 유닛 효과 적용`
`→ 적용 대상과 최대 체력 변화량 기록`

구조로 변경했다.

이를 통해 전투 종료 시 어떤 유닛에 마이너 카드 효과가 적용되었는지 추적할 수 있다.

### 6. 전투 종료 시 마이너 카드 효과 제거

전투가 종료되면 `BattleMinorCardEffectRegistry.ClearBattleEffects()`를 호출한다.

처리 내용:

- 마이너 카드에서 사용한 공격력 증가 상태 제거
- 물리 방어 증가/감소 상태 제거
- 마법 저항 증가/감소 상태 제거
- 마이너 카드로 증가한 최대 체력만큼 다시 감소
- 적용 대상 기록 제거
- 최대 체력 변경 기록 제거

따라서 이번 전투에서 획득한 마이너 카드 강화는 다음 몬스터 전투로 전달되지 않는다.

### 7. 전투 결과 저장 전에 마이너 카드 효과 정리

아군 HP와 정신력은 맵 진행 동안 유지되어야 하므로 전투 결과를 저장하는 기존 구조는 유지했다.

단, 마이너 카드의 최대 체력 증가 등 임시 효과가 전투 결과에 섞이지 않도록 `BattleResultData` 생성 시 마이너 카드 효과를 먼저 정리한 뒤 아군 상태를 스냅샷으로 저장하도록 변경했다.

처리 순서:

`전투 종료`
`→ 마이너 카드 임시 효과 제거`
`→ 아군 HP/정신력 상태 저장`
`→ 전투 결과 전달`
`→ 맵 진행 복귀`

### 8. 전투 종료 시 Level 선택 상태 정리

`PlayerLevelRunManager.EndBattle()`을 추가했다.

전투가 끝나면 남아 있던 마이너 카드 선택권을 제거한다.

이를 통해 전투 종료 직전에 사용하지 못한 선택권이 다른 전투로 전달되지 않는다.

### 9. 마이너 카드 선택 기록 초기화

`BattleMinorCardController`에 전투 종료 상태를 추가했다.

전투 종료 시:

- 추가 EXP 획득 차단
- 추가 마이너 카드 선택 차단
- 현재 선택 화면 비활성화
- 현재 선택지 제거
- 선택한 마이너 카드 목록 제거
- 현재 전투에서 사용한 카드 ID 기록 제거
- 마이너 카드 효과 제거
- 대기 선택권 정리

를 수행한다.

### 10. 현재 강화 확인 UI 추가

현재 전투에서 획득한 마이너 카드를 확인할 수 있도록 `MinorCardBuffWindowView`를 추가했다.

전투 화면 우측 상단에 `현재 강화` 버튼을 표시한다.

마이너 카드를 선택하면 버튼에 현재 획득 수가 표시된다.

예:

`현재 강화 (2)`

버튼을 누르면 이번 전투에서 선택한 마이너 카드와 효과를 확인할 수 있다.

예:

- 강철의 의지
  - 모든 아군 · 물리 방어 +3
- 생명의 불씨
  - 모든 아군 · 최대 체력 +10

창은 게임 시작 시 닫힌 상태로 시작한다.

### 11. 기존 맵 진행용 데이터 유지

이번 변경에서는 기존 맵 진행에서 유지되어야 하는 데이터 구조를 변경하지 않았다.

전투가 끝난 뒤 다음 몬스터까지 유지되는 데이터:

- 아군 HP
- 아군 정신력
- 유물
- 골드
- 소모품

다음 전투로 유지되지 않는 데이터:

- 플레이어 Level
- 플레이어 EXP
- 대기 중 마이너 카드 선택권
- 선택한 마이너 카드 목록
- 마이너 카드 전투 효과

---
## 생성 파일

- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardEffectRegistry.cs`
- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardEffectRegistry.cs.meta`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardBuffWindowView.cs`
- `Assets/_ProjectC/Scripts/MinorCards/MinorCardBuffWindowView.cs.meta`
- `Devlogs/Day31/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleResultData.cs`
- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardBootstrap.cs`
- `Assets/_ProjectC/Scripts/MinorCards/BattleMinorCardController.cs`
- `Assets/_ProjectC/Scripts/MinorCards/PlayerLevelRunManager.cs`

---
## 삭제 파일

- 없음

---
## 검토 결과

- 최신 `main` 커밋이 30일차 커밋보다 정확히 1개 앞선 상태 확인
- Level/EXP의 `DontDestroyOnLoad` 유지가 제거된 것을 확인
- 전투 시작마다 `BeginBattle()`을 통해 Level/EXP가 초기화되는 구조 확인
- 전투 종료 후 남은 마이너 카드 선택권이 제거되는 구조 확인
- 마이너 카드 적용 효과를 별도 Registry에서 추적하는 구조 확인
- 최대 체력 증가량을 개별 유닛 기준으로 추적하고 전투 종료 시 되돌리는 구조 확인
- 전투 종료 시 상태 기반 마이너 카드 효과를 제거하는 구조 확인
- 전투 결과 상태 저장 전에 마이너 카드 효과가 정리되는 구조 확인
- 현재 전투의 선택 마이너 카드 목록을 확인하는 `현재 강화` UI 추가 확인
- 31일차 변경에서 삭제된 파일이 없는 것을 확인
- GitHub에 등록된 자동 CI 상태 검사는 없음
- 실제 Unity 컴파일 및 Play Mode 전체 동작은 GitHub 소스만으로 자동 확인할 수 없어 최종 수동 테스트 필요

---
## Unity에서 직접 확인할 부분

1. 맵에서 첫 번째 몬스터 전투 진입
2. 전투 시작 시 `LV 1 / EXP 0` 확인
3. 카드를 사용해 EXP 획득 확인
4. 레벨업 후 다음 플레이어 턴에 마이너 카드 선택창 표시 확인
5. 마이너 카드 선택 후 `현재 강화` 버튼의 숫자 증가 확인
6. `현재 강화` 버튼을 눌러 선택 카드와 효과 표시 확인
7. 강철의 의지 선택 시 아군 물리 방어 증가 확인
8. 생명의 불씨 선택 시 아군 최대 체력 증가 확인
9. 전투 승리 후 마이너 카드 효과가 제거되는지 확인
10. 전투 종료 후 맵으로 복귀
11. 아군 HP와 정신력이 기존 맵 진행 규칙대로 유지되는지 확인
12. 유물과 소모품이 그대로 유지되는지 확인
13. 다음 몬스터 전투 진입
14. Level이 다시 Lv.1인지 확인
15. EXP가 다시 0인지 확인
16. 이전 전투의 마이너 카드 목록이 없는지 확인
17. 이전 전투의 공격력·방어력·마법 저항·최대 체력 강화가 남지 않는지 확인
18. 두 번째 전투에서도 다시 정상적으로 EXP와 마이너 카드를 획득할 수 있는지 확인

---
## 다음 개발 방향

- 맵에서 몬스터 조우 정보를 전투 Scene에 전달하는 구조 정리
- 전투 종료 결과를 맵의 해당 몬스터 또는 노드 상태와 연결
- 처치한 몬스터가 맵에서 제거되거나 클리어 상태가 되도록 처리
- 맵에서 다음 몬스터 조우까지 이동 흐름 구축
- 일반 전투·엘리트 전투·보스 전투 등 조우 종류에 따른 전투 데이터 전달 구조 준비

---
## 커밋 제목

`31일차 : 전투 단위 레벨·경험치 및 마이너 카드 효과 수명 관리`
