# Project C - 50일차 개발일지

## 작업 주제

퇴색 지역·환경 위험 및 노출도 시스템 구축

## 개발 목표

- 절차적으로 생성된 일반 방 일부를 Seed 기반 퇴색 위험 지역으로 지정
- 시작 방과 계단 방은 안전 지역으로 유지
- 퇴색 위험도를 1~3단계로 구분
- 위험 방 Floor에 반투명 색상 오버레이 표시
- 플레이어가 실제로 어느 방의 Floor에 있는지 판정
- 퇴색 지역 체류 중 노출도 누적
- 노출도 100% 도달 시 체력·정신력 환경 피해 발생
- 환경 피해를 탐사 세션에 누적하고 다음 전투 시작 시 파티 상태에 반영
- 이벤트 패널이 열린 동안 환경 노출 일시 정지
- 전투 후 같은 Seed로 탐사에 복귀했을 때 같은 위험 방 재생성
- 이후 사망·부활 시스템과 연결할 수 있도록 환경 피해 기반 마련

## 구현 내용

### 1. 퇴색 환경 위험 데이터 구조

탐사 환경 위험의 종류와 방 단위 상태를 관리하는 구조를 추가했다.

현재 위험 종류:

```text
None
Fade
```

`ExplorationHazardRoomState`는 다음 값을 가진다.

```text
HazardType
Level
```

퇴색 지역의 위험도는 1~3 범위로 제한한다.

### 2. Seed 기반 위험 방 선정

현재 층의 `CurrentMap.Seed`를 기반으로 일반 방 중 일부를 퇴색 지역으로 선택한다.

현재 기본 위험 방 수:

```text
층당 3개
```

다음 방은 위험 지역 후보에서 제외한다.

```text
Start
Stairs
```

따라서 플레이어가 탐사를 시작하자마자 환경 피해를 받거나 계단 주변에서 강제로 위험에 노출되는 상황을 방지한다.

### 3. 위험도 1~3 배정

위험 방으로 선택된 방에는 1~3단계 위험도를 부여한다.

현재 테스트 가중치:

```text
Lv.1 : 약 55%
Lv.2 : 약 30%
Lv.3 : 약 15%
```

위험도는 같은 Map Seed에서 동일하게 재현된다.

### 4. 실제 Floor 기준 현재 방 판정

48일차에서 추가한 방별 실제 Floor 데이터를 확장해 플레이어 World Position이 어느 논리 방의 Floor에 포함되는지 확인할 수 있도록 했다.

추가 기능:

```text
TryGetRoomCoordinateAtWorldPosition()
TryGetRoomFloorWorldPositions()
```

이를 통해 정사각형뿐 아니라 L형, T형, 십자형 방에서도 실제 Floor 기준으로 위험 지역 진입 여부를 판정한다.

통로에 있을 때는 현재 위험 방 판정을 해제한다.

### 5. 퇴색 방 시각화

위험 방의 Floor 위에 반투명 보라색 오버레이를 생성한다.

현재 표시:

```text
Lv.1
→ 연한 보라색

Lv.2
→ 중간 보라색

Lv.3
→ 진한 자주색
```

각 위험 방 중앙 위에는 개발 확인용 표식을 표시한다.

```text
!1
!2
!3
```

현재 표시는 정식 환경 에셋이 아니라 런타임 SpriteRenderer와 TextMesh 기반 프로토타입이다.

### 6. 퇴색 노출도

플레이어가 퇴색 방에 머무르면 노출도가 증가한다.

최대 노출도:

```text
100
```

현재 테스트 증가 속도:

```text
Lv.1 : 초당 12
Lv.2 : 초당 18
Lv.3 : 초당 25
```

대략적인 피해 발생 주기:

```text
Lv.1 : 약 8.3초
Lv.2 : 약 5.6초
Lv.3 : 약 4초
```

안전 방이나 통로로 이동하면 현재 노출도는 유지되지만 추가 증가가 멈춘다.

### 7. 환경 피해

노출도가 100에 도달하면 위험도에 따라 체력과 정신력 피해를 계산한다.

현재 테스트 수치:

| 위험도 | 체력 피해 | 정신력 피해 |
| --- | ---: | ---: |
| Lv.1 | 2 | 1 |
| Lv.2 | 4 | 2 |
| Lv.3 | 6 | 3 |

피해가 발생하면 노출도에서 100을 차감하고 다음 노출 주기를 계속 계산한다.

### 8. 탐사 세션 환경 피해 누적

탐사 화면에는 전투용 `BattleUnitRuntime`이 존재하지 않으므로 환경 피해를 `ExplorationSessionManager`에 우선 누적한다.

추가 상태:

```text
PendingHazardHealthDamage
PendingHazardMentalDamage
```

예:

```text
퇴색 피해 1회
→ HP -4 / 정신 -2 대기

퇴색 피해 추가 발생
→ HP -8 / 정신 -4 대기
```

이 누적값은 탐사 Scene과 전투 Scene 사이에서도 유지된다.

### 9. 다음 전투 시작 시 실제 파티 상태 반영

`BattleSceneSetup.CreateAllyUnits()`에서 기존 저장 체력·정신력을 복원한 직후 탐사 중 누적된 환경 피해를 추가 적용한다.

흐름:

```text
BattleUnitRuntime 생성
↓
이전 전투 저장 HP / 정신력 복원
↓
탐사 환경 피해 적용
↓
BattleUnitView 생성
↓
전투 시작
```

파티 전체에 환경 피해 처리가 끝나면 대기 중인 환경 피해 값은 초기화된다.

### 10. 환경 피해 최소 HP 보정

50일차에서는 환경 피해만으로 캐릭터가 사망하지 않도록 최소 체력을 1로 제한했다.

```text
현재 HP 5
환경 피해 8
↓
HP 1
```

환경 피해에 의한 실제 사망 판정은 이후 사망·부활 확장 단계에서 연결하기 위한 임시 안전장치다.

### 11. 사망한 아군 처리

이전 전투 결과로 이미 사망 상태인 아군에게는 추가 환경 피해를 적용하지 않는다.

다만 파티 처리 카운트에는 포함해 모든 파티원을 순회한 뒤 대기 환경 피해가 정상적으로 초기화되도록 구성했다.

### 12. 이벤트 UI와 환경 위험 연동

49일차 이벤트 패널이 열린 동안에는 `ExplorationPlayerController.InputBlocked`를 이용해 퇴색 노출 증가도 함께 일시 정지한다.

```text
이벤트 패널 Open
→ 플레이어 이동 정지
→ 퇴색 노출 증가 정지

이벤트 패널 Close
→ 플레이어 이동 복구
→ 위험 방이라면 노출 증가 재개
```

이벤트 내용을 읽는 시간 때문에 의도하지 않은 환경 피해가 누적되는 것을 방지한다.

### 13. 환경 위험 HUD

화면 왼쪽 위에 개발 확인용 위험 HUD를 추가했다.

퇴색 지역에 있을 때 표시:

```text
퇴색 위험 지역 Lv.2

노출도 42%
HP -4 / 정신 -2 다음 전투 반영
```

노출도 진행 바와 최근 환경 피해 결과도 함께 표시한다.

안전 지역으로 나간 뒤에도 다음 전투에 적용할 대기 피해가 남아 있으면 해당 정보는 계속 표시된다.

### 14. 전투 왕복과 Seed 재현

퇴색 방 선택은 현재 Map Seed를 사용한다.

따라서 전투 진입 후 `30_Exploration`으로 복귀해 동일 Seed의 층이 재생성되면 같은 좌표의 방이 다시 퇴색 위험 지역으로 지정된다.

환경 피해 누적값은 `ExplorationSessionManager`가 유지하고 있기 때문에 전투 시작 시 실제 아군 상태에 연결된다.

### 15. 기존 탐사 콘텐츠와 공존

퇴색 지역은 이벤트나 적 조우처럼 1회성 오브젝트가 아니라 방 자체의 환경 상태다.

따라서 다음 조합이 가능하다.

```text
퇴색 방 + 적 조우
퇴색 방 + 탐사 이벤트
퇴색 방 + 빈 방
```

조우·이벤트 완료 상태와 퇴색 지역 상태를 별도의 시스템으로 관리한다.

## 생성 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardOverlayView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardOverlayView.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardRoomState.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardRoomState.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardRuntime.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationHazardView.cs.meta`

## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationTilemapView.cs`

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console 컴파일 오류 없음
- [ ] `30_Exploration` 진입 시 맵 정상 생성
- [ ] 일반 방 중 일부가 보라색 퇴색 지역으로 표시
- [ ] 시작 방이 퇴색 지역으로 선택되지 않음
- [ ] 계단 방이 퇴색 지역으로 선택되지 않음
- [ ] 위험 방에 `!1`, `!2`, `!3` 중 하나 표시
- [ ] 같은 위험도에서 오버레이 색상이 일관됨
- [ ] 플레이어가 퇴색 방에 들어가면 위험 HUD 표시
- [ ] L형·T형 등 비정형 방에서도 위험 진입 판정 정상
- [ ] 퇴색 방 체류 중 노출도 증가
- [ ] 안전 방으로 이동하면 노출도 증가 중지
- [ ] 통로에서 노출도 증가 중지
- [ ] Lv.1 / Lv.2 / Lv.3에 따라 노출 증가 속도 차이 확인
- [ ] 노출도 100% 도달 시 환경 피해 발생
- [ ] 환경 피해가 세션의 대기 HP / 정신 피해에 누적
- [ ] 여러 번 피해 발생 시 누적값 증가
- [ ] 이벤트 패널이 열린 동안 노출도 증가 정지
- [ ] 이벤트 패널을 닫으면 위험 방에서 노출 증가 재개
- [ ] 적 조우 후 `40_Battle` 정상 진입
- [ ] 전투 시작 시 이전 저장 HP / 정신력 복원
- [ ] 복원된 상태에 누적 환경 피해 추가 반영
- [ ] 환경 피해 적용 후 대기 피해 초기화
- [ ] 환경 피해로 HP가 1 미만으로 내려가지 않음
- [ ] 전투 종료 후 탐사 복귀 정상 작동
- [ ] 동일 Seed 복귀 시 같은 방이 퇴색 지역으로 재생성
- [ ] F9 새 Seed 생성 시 위험 방 위치와 위험도가 변경
- [ ] 기존 적 조우·이벤트·계단 기능 정상 유지

## 현재 단계의 제한 사항

- 위험 방은 현재 층당 기본 3개로 고정된 프로토타입 수치다.
- 위험도 확률과 노출 속도, HP·정신력 피해량은 모두 테스트용 임시 밸런스다.
- 정식 퇴색 환경 Sprite, Shader, VFX, 사운드는 아직 적용하지 않았다.
- 위험 HUD는 최종 UI가 아닌 IMGUI 기반 개발 확인용 화면이다.
- 환경 피해는 탐사 화면에서 캐릭터 Runtime에 즉시 적용하는 방식이 아니라 세션에 누적한 뒤 다음 전투 시작 시 실제 아군 상태에 반영한다.
- 환경 피해만으로 캐릭터가 사망하지 않도록 현재 HP 최소값을 1로 제한한다.
- 같은 탐사 Scene 안에서 안전 지역으로 나가도 누적 노출도는 자동 감소하지 않는다.
- 새 층으로 이동할 때 현재 `ExplorationHazardRuntime`의 노출도가 명시적으로 초기화되지 않으므로 같은 탐사 Scene에서 층을 내려가면 남은 노출도가 이어진다.
- 전투 Scene을 왕복하면 탐사 Scene의 `ExplorationHazardRuntime`이 새로 만들어지므로 진행 중이던 노출도 자체는 다시 시작하지만, 이미 발생한 대기 환경 피해는 세션에 유지된다.
- 영구 저장은 아직 연결되지 않았기 때문에 게임 종료 후 환경 상태 복원은 이후 저장 시스템 단계에서 처리해야 한다.
- GitHub 저장소에는 Unity Editor 컴파일 또는 Play Mode 자동 CI 상태 검사가 등록되어 있지 않으므로 최종 동작 검증은 로컬 Unity 실행 결과를 기준으로 한다.

## 완료 결과

50일차를 통해 절차 탐사 공간에 단순한 시각 변화가 아니라 실제 플레이 판단에 영향을 주는 환경 위험 기반이 추가되었다.

```text
절차 맵 생성
↓
Seed 기반 퇴색 방 선정
↓
위험도 Lv.1~3 부여
↓
보라색 환경 표시
↓
플레이어 진입
↓
노출도 누적
↓
100% 도달
↓
HP / 정신력 환경 피해 누적
↓
다음 전투 시작
↓
실제 파티 상태에 환경 피해 반영
```

이번 구현은 이후 환경 저항, 퇴색 전용 이벤트, 환경 치료, 사망·부활, 정식 Fade VFX로 확장할 수 있는 기본 구조를 마련했다.
