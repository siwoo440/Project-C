# 33일차 개발 일지

---
## 개발 목표

- 전투용 Level/EXP와 별개의 캐릭터 영구 Level/EXP 시스템 구축
- 전투 승리 시 캐릭터 경험치를 획득하고 레벨업하도록 구성
- 전투 클리어 보상으로 골드와 강화용 자원 지급
- 강화용 자원으로 나사, 철판, 전선 추가
- 기존 유물 시스템의 골드와 클리어 보상 골드를 하나의 골드 값으로 통합
- 추후 강화 시스템에서 사용할 수 있도록 자원 보유 확인 및 소비 기능 준비
- 탐사 HUD에서 캐릭터 성장과 보유 자원을 확인할 수 있도록 확장

---
## 구현 내용

### 1. 캐릭터 영구 성장 시스템 추가

`CharacterProgressionManager`를 추가했다.

기존 전투용 `PlayerLevelRunManager`와는 별개의 시스템이며, 캐릭터 자체의 장기 성장 정보를 관리한다.

관리 정보:

- 캐릭터 Level
- 현재 EXP
- 다음 레벨 필요 EXP

캐릭터 영구 성장 관리자는 `DontDestroyOnLoad`를 사용해 탐사와 전투 Scene 사이에서도 유지된다.

따라서 진행 구조는 다음과 같이 분리된다.

`전투용 Level/EXP`
`→ 각 전투마다 Lv.1 / EXP 0에서 시작`
`→ 전투 중 마이너 카드 성장에 사용`
`→ 전투 종료 후 초기화`

`캐릭터 Level/EXP`
`→ 전투 승리 시 EXP 획득`
`→ 탐사와 다음 전투에서도 유지`
`→ 캐릭터 자체 성장에 사용`

### 2. 캐릭터 레벨업 규칙 추가

캐릭터는 Lv.1에서 시작한다.

초기 필요 경험치는 다음 규칙을 사용한다.

- Lv.1 → Lv.2: 20 EXP
- Lv.2 → Lv.3: 30 EXP
- Lv.3 → Lv.4: 40 EXP
- 이후 레벨마다 필요 EXP +10

한 번에 많은 경험치를 얻는 경우 여러 레벨을 연속으로 상승할 수 있도록 반복 레벨업 판정을 적용했다.

### 3. EncounterData에 클리어 보상 추가

기존 `EncounterData`에 전투 승리 보상 정보를 추가했다.

추가된 데이터:

- 캐릭터 EXP
- Gold
- 나사
- 철판
- 전선

각 Encounter가 서로 다른 경험치와 자원 보상량을 가질 수 있도록 구성했다.

### 4. 테스트 조우 보상 설정

현재 테스트 조우에는 다음 값을 적용했다.

#### 테스트 조우 A

- 캐릭터 EXP +10
- Gold +50
- 나사 +25
- 철판 +20
- 전선 +15

#### 테스트 조우 B

- 캐릭터 EXP +10
- Gold +50
- 나사 +25
- 철판 +20
- 전선 +15

#### 테스트 조우 C

- 캐릭터 EXP +20
- Gold +50
- 나사 +25
- 철판 +20
- 전선 +15

조우 A와 B를 차례대로 클리어하면 총 EXP 20이 되어 Lv.2 상승을 빠르게 확인할 수 있도록 했다.

### 5. 플레이어 자원 관리자 추가

`PlayerResourceManager`를 추가했다.

관리 자원:

- Gold
- 나사
- 철판
- 전선

나사, 철판, 전선은 이후 캐릭터 또는 장비 강화 시스템에 사용할 수 있도록 영구 진행 자원으로 관리한다.

### 6. 기존 골드 시스템과 통합

새로운 골드 지갑을 별도로 만들지 않고 기존 `RelicRunManager`의 `RelicGoldRuntime`을 그대로 사용한다.

따라서 다음 골드가 모두 동일한 Gold 값에 누적된다.

- 전투 클리어 골드
- 유물 중복 획득 시 골드 변환
- 이후 상점 또는 기타 골드 획득

이를 통해 골드가 여러 Manager에 분리되어 서로 다른 값으로 관리되는 문제를 방지했다.

### 7. 자원 추가 기능 구현

`PlayerResourceManager.AddClearReward()`를 추가했다.

전투 승리 후 다음 값을 한 번에 지급할 수 있다.

- Gold
- 나사
- 철판
- 전선

음수 보상값은 0으로 보정한다.

### 8. 강화 시스템용 자원 확인 기능 준비

추후 강화 비용 검사를 위해 `CanAfford()`를 추가했다.

예를 들어 강화 비용이 다음과 같을 경우:

- Gold 100
- 나사 50
- 철판 30
- 전선 20

현재 보유량으로 해당 비용을 지불할 수 있는지 한 번에 확인할 수 있다.

### 9. 강화 시스템용 자원 소비 기능 준비

`PlayerResourceManager.TrySpend()`를 추가했다.

모든 자원이 충분한 경우에만 Gold, 나사, 철판, 전선을 함께 소비한다.

하나라도 부족하면 아무 자원도 소비하지 않고 실패를 반환한다.

이를 통해 이후 강화 시스템에서 부분 소비가 발생하지 않도록 사용할 수 있다.

### 10. 기존 골드 Runtime에 소비 기능 추가

`RelicGoldRuntime`에 다음 기능을 추가했다.

- `CanAfford()`
- `TrySpend()`

기존 골드 획득과 중복 유물 골드 변환 구조는 유지하면서 이후 상점과 강화 시스템에서도 동일한 골드를 사용할 수 있도록 확장했다.

### 11. 전투 승리 보상 자동 지급

`ExplorationSessionManager`의 전투 결과 처리에 승리 보상 지급을 연결했다.

승리 처리:

`BattleResult.Victory`
`→ 캐릭터 EXP 지급`
`→ 캐릭터 레벨업 판정`
`→ Gold 지급`
`→ 나사 지급`
`→ 철판 지급`
`→ 전선 지급`
`→ Encounter 클리어 처리`

도주와 패배:

`→ 캐릭터 EXP 없음`
`→ 자원 보상 없음`
`→ Encounter 유지`

따라서 클리어 보상은 전투 승리에서만 받을 수 있다.

### 12. 최근 클리어 보상 결과 저장

`ExplorationClearRewardResult`를 추가했다.

마지막 전투에서 획득한 다음 정보를 저장한다.

- Encounter 이름
- 획득 캐릭터 EXP
- 획득 Gold
- 획득 나사
- 획득 철판
- 획득 전선
- 보상 지급 전 캐릭터 Level
- 보상 지급 후 캐릭터 Level
- 레벨업 발생 여부

이를 이용해 탐사 화면에서 직전 전투 보상을 표시한다.

### 13. 탐사 HUD 확장

기존 탐사 테스트 HUD에 영구 진행 정보를 추가했다.

우측 상단 표시:

- 캐릭터 Level
- 현재 EXP / 다음 레벨 필요 EXP
- Gold
- 나사
- 철판
- 전선
- 현재 클리어한 Encounter 수

전투 승리 후 탐사로 복귀하면 최근 클리어 보상도 화면 하단에 표시한다.

예:

`테스트 조우 A 클리어 보상`
`캐릭터 EXP +10`
`Gold +50 · 나사 +25 · 철판 +20 · 전선 +15`

레벨업이 발생한 경우 `LEVEL UP!` 문구와 상승한 캐릭터 Level을 함께 표시한다.

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationClearRewardResult.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationClearRewardResult.cs.meta`
- `Assets/_ProjectC/Scripts/Progression.meta`
- `Assets/_ProjectC/Scripts/Progression/CharacterProgressionManager.cs`
- `Assets/_ProjectC/Scripts/Progression/CharacterProgressionManager.cs.meta`
- `Assets/_ProjectC/Scripts/Progression/PlayerResourceManager.cs`
- `Assets/_ProjectC/Scripts/Progression/PlayerResourceManager.cs.meta`
- `Devlogs/Day33/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_A.asset`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_B.asset`
- `Assets/_ProjectC/Resources/Encounters/Encounter_Test_C.asset`
- `Assets/_ProjectC/Scripts/Exploration/EncounterData.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`
- `Assets/_ProjectC/Scripts/Relics/RelicGoldRuntime.cs`

---
## 삭제 파일

- 없음

---
## 검토 결과

- 최신 `main` 커밋이 32일차 커밋보다 정확히 1개 앞선 상태 확인
- 캐릭터 영구 Level/EXP 관리자 추가 확인
- 캐릭터 성장 관리자가 Scene 전환 후에도 유지되는 구조 확인
- 기존 전투용 Level/EXP 시스템과 별도의 클래스에서 관리되는 구조 확인
- Encounter별 캐릭터 EXP와 자원 보상 데이터 추가 확인
- 테스트 조우 A EXP +10 설정 확인
- 테스트 조우 B EXP +10 설정 확인
- 테스트 조우 C EXP +20 설정 확인
- 모든 테스트 조우 Gold +50 설정 확인
- 모든 테스트 조우 나사 +25 설정 확인
- 모든 테스트 조우 철판 +20 설정 확인
- 모든 테스트 조우 전선 +15 설정 확인
- 승리 시에만 캐릭터 EXP와 자원이 지급되는 구조 확인
- 도주 및 패배에서는 클리어 보상이 지급되지 않는 구조 확인
- Gold가 기존 `RelicGoldRuntime`과 공유되는 구조 확인
- 기존 유물 중복 골드 변환과 전투 클리어 골드가 같은 지갑을 사용하는 구조 확인
- Gold의 보유량 검사 및 소비 기능 추가 확인
- 나사·철판·전선의 보유량 검사 및 소비 기능 추가 확인
- 탐사 HUD에 영구 캐릭터 성장과 자원 표시 추가 확인
- 최근 전투 클리어 보상 표시 구조 확인
- 33일차 변경에서 삭제된 파일이 없는 것을 확인
- 정적 코드 검토에서 진행을 차단하는 문제는 발견하지 못함
- GitHub에 등록된 자동 CI 상태 검사는 없음
- 실제 Unity 컴파일 및 Play Mode 전체 동작은 GitHub 소스만으로 자동 확인할 수 없어 최종 수동 테스트 필요

---
## Unity에서 직접 확인할 부분

1. `30_Exploration` Scene 실행
2. 우측 상단에 캐릭터 Lv.1 / EXP 0/20 확인
3. Gold 0 확인
4. 나사 0 / 철판 0 / 전선 0 확인
5. 테스트 조우 A와 접촉
6. 전투용 Level이 Lv.1 / EXP 0에서 시작되는지 확인
7. `[DEBUG] 즉사` 카드로 전투 승리
8. 탐사 Scene으로 복귀
9. 테스트 조우 A가 제거되었는지 확인
10. 캐릭터 EXP 10/20 확인
11. Gold 50 확인
12. 나사 25 확인
13. 철판 20 확인
14. 전선 15 확인
15. 하단에 최근 클리어 보상이 표시되는지 확인
16. 테스트 조우 B와 전투
17. 두 번째 전투에서도 전투용 Level/EXP가 다시 Lv.1 / EXP 0인지 확인
18. 조우 B 승리 후 캐릭터 Lv.2 확인
19. 캐릭터 EXP가 0/30으로 변경되는지 확인
20. Gold 100 확인
21. 나사 50 확인
22. 철판 40 확인
23. 전선 30 확인
24. 조우 C 승리 후 캐릭터 EXP 20/30 확인
25. Gold 150 확인
26. 나사 75 확인
27. 철판 60 확인
28. 전선 45 확인
29. 도주 테스트 시 캐릭터 EXP와 모든 자원이 증가하지 않는지 확인
30. 패배 테스트 시 캐릭터 EXP와 모든 자원이 증가하지 않는지 확인

---
## 다음 개발 방향

- 캐릭터 Level에 따른 실제 능력치 성장 규칙 추가
- 나사·철판·전선을 사용하는 강화 시스템 구축
- 강화 항목별 비용 데이터 분리
- 강화 전후 능력치 비교 UI 추가
- 강화 비용 부족 상태 표시
- 탐사 중 캐릭터 성장 및 자원 정보를 확인하는 정식 UI 구조 준비
- 일반·엘리트·보스 조우별 EXP와 자원 보상 차등화

---
## 커밋 제목

`33일차 : 캐릭터 영구 레벨·경험치 및 강화 자원 보상 시스템 구축`
