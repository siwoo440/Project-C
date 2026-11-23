# Project C - 45일차 개발일지

## 작업 주제

탐사 성공 이후 거점 정산·시설 강화·다음 탐사로 이어지는 메타 진행 루프 구축 및 전투 진행용 공격 테스트 카드 확장

## 개발 목표

- 탐사 중 획득한 EXP·Gold·재료를 한 번의 런 단위로 누적 기록
- Boss 승리 후 탐사 성공 결과를 유지한 채 거점으로 복귀
- `20_Lobby`에서 지난 탐사의 완료 층·클리어 조우·획득 보상 확인
- 탐사 보상으로 기존 F7 시설 강화 기능 사용
- 다음 탐사 시작 시에만 탐사 런 상태 초기화
- 캐릭터 성장·호감도·보유 자원·시설 강화는 다음 탐사에서도 유지
- 전투 진행 검사를 빠르게 하기 위한 공격 카드 다수 추가
- 기존 `[DEBUG] 즉사` 카드를 테스트 덱에 충분히 배치
- 기존 카드·전투 로직을 변경하지 않고 ScriptableObject 데이터 중심으로 테스트 환경 확장

## 구현 내용

### 1. 탐사 런 단위 누적 보상 추가

`ExplorationSessionManager`에 현재 탐사에서 실제로 획득한 보상의 누적값을 추가했다.

```text
RunExperienceGained
RunGoldGained
RunScrewGained
RunIronPlateGained
RunWireGained
```

개별 조우 승리 시 기존처럼 즉시 EXP와 자원을 지급하면서 동시에 이번 탐사의 누적 결과에도 기록한다.

따라서 정산 데이터는 별도의 지급 대기 보상이 아니라 이미 지급된 실제 획득량을 보여주는 기록으로 사용한다.

### 2. 실제 지급 재료량 기준 정산

나사·철판·전선은 기존 `FacilityUpgradeManager`의 물자 창고 보너스가 적용될 수 있다.

45일차에서는 정산 결과와 실제 보유 자원이 서로 달라지지 않도록 다음 값에 시설 보너스를 적용한 실제 획득량을 계산해 런 합계에 기록한다.

```text
나사
철판
전선
```

실제 자원 지급 자체는 기존 `PlayerResourceManager.AddClearReward()` 경로를 그대로 사용한다.

### 3. 탐사 성공 정산 로그 확장

Boss 승리로 탐사 성공이 확정되면 기존 완료 정보와 함께 이번 탐사의 실제 획득량을 Console에 출력한다.

표시 항목:

```text
완료 층
클리어 조우 수
EXP
Gold
나사
철판
전선
호감도
```

이를 통해 탐사 결과가 거점으로 이동하기 전에 정상적으로 누적되었는지 빠르게 확인할 수 있다.

### 4. 거점 복귀 런타임 UI 추가

새로운 `ExplorationRunLoopDebugView`를 추가했다.

이 컴포넌트는 `RuntimeInitializeOnLoadMethod`를 통해 자동 생성되므로 Scene Hierarchy에 직접 배치할 필요가 없다.

탐사 성공 상태에서는 화면에 다음 버튼을 표시한다.

```text
거점으로 복귀
```

버튼을 누르면 탐사 결과를 초기화하지 않고 `20_Lobby`로 이동한다.

### 5. 탐사 결과를 유지한 채 Lobby 이동

거점 복귀 시에는 `ResetExploration()`을 호출하지 않는다.

따라서 다음 정보가 Lobby까지 유지된다.

```text
탐사 성공 여부
완료 층
클리어 조우 수
호감도 성공 보상
이번 탐사 EXP 합계
이번 탐사 Gold 합계
이번 탐사 재료 합계
```

Scene 이동은 기존 `SceneFlowManager`가 존재하면 해당 경로를 우선 사용하고, 직접 Scene 테스트에서는 `SceneManager.LoadScene()`을 대체 경로로 사용한다.

### 6. Lobby 탐사 정산 패널 추가

`20_Lobby`에서는 개발용 탐사 정산 패널을 자동 표시한다.

완료된 탐사가 있다면 다음 내용을 확인할 수 있다.

```text
탐사 성공
최종 도달 층
클리어 조우 수

이번 탐사 실제 획득량
EXP
Gold
나사
철판
전선
호감도
```

정산할 완료 탐사가 없는 경우에는 새 탐사를 시작할 수 있는 상태임을 표시한다.

### 7. 현재 영구 진행 상태 동시 표시

Lobby 정산 패널에서 지난 탐사 결과와 함께 현재 영구 진행 상태도 확인할 수 있다.

표시 항목:

```text
캐릭터 레벨
현재 EXP
호감도
Gold
나사
철판
전선
```

이를 통해 이번 탐사에서 얻은 양과 전체 보유량을 함께 비교할 수 있다.

### 8. 기존 시설 강화 시스템 연결

45일차에서는 새로운 시설 강화 시스템을 만들지 않고 기존 F7 `FacilityUpgradeDebugView`를 그대로 사용한다.

전체 흐름:

```text
탐사에서 자원 획득
↓
거점 복귀
↓
정산 결과 확인
↓
F7
↓
시설 강화
↓
Gold / 재료 소비
```

이를 통해 지금까지 따로 존재하던 탐사 보상과 거점 시설 강화가 하나의 성장 루프로 연결된다.

### 9. 다음 탐사 시작 기능 추가

Lobby 정산 패널 아래에 다음 탐사 시작 버튼을 추가했다.

```text
정산 완료 · 다음 탐사 시작
```

완료된 탐사가 없는 경우에는:

```text
새 탐사 시작
```

으로 표시한다.

버튼을 누를 때만 `ExplorationSessionManager.ResetExploration()`을 호출한 뒤 `30_Exploration`으로 이동한다.

### 10. 새 탐사 런 상태 초기화

다음 탐사를 시작하면 다음 값이 초기화된다.

```text
CurrentFloor → 1
현재 층 Seed
클리어 조우 목록
현재 조우
복귀 위치
탐사 완료 상태
탐사 성공 상태
완료 층
완료 조우 수
마지막 성공 호감도 표시값
런 EXP 합계
런 Gold 합계
런 나사 합계
런 철판 합계
런 전선 합계
```

따라서 이전 탐사의 결과가 새로운 탐사의 정산 데이터에 섞이지 않는다.

### 11. 영구 성장 상태 유지

새 탐사를 시작하더라도 다음 진행 정보는 `ResetExploration()`에서 초기화하지 않는다.

```text
Character Level
Character EXP
Affinity
Gold
Screw
IronPlate
Wire
Facility Level
```

정상적인 핵심 루프는 다음과 같다.

```text
탐사
↓
보상 획득
↓
거점 정산
↓
시설 강화
↓
다음 탐사
↓
강화 상태 유지
```

### 12. 공격 카드 데이터 확장

전투와 탐사 진행 검사를 빠르게 하기 위해 공격용 CardData 15종을 추가했다.

추가 카드:

```text
빠른 베기
정밀 찌르기
강타
내려찍기
휩쓸기
검풍
마력 탄환
마력 폭발
마력 폭풍
집중 광선
주술 타격
주술 파동
방패 강타
성광 타격
성광 파동
```

Sword / Wand / Cup / Pentacle / Shield 계열과 물리·마법 피해, 단일·전체 적 대상을 섞어 현재 카드 전투 구조를 폭넓게 확인할 수 있도록 구성했다.

### 13. 공격 카드 AP와 피해량 차등 구성

공격 카드는 모두 기존 `CardEffectType.Damage`를 사용한다.

대략적인 테스트 범위:

```text
AP 1
→ 낮은 단일 공격

AP 2
→ 중간 공격

AP 3
→ 강한 단일 또는 전체 공격
```

피해 타입은 기존 규칙에 맞춰:

```text
Physical
Magical
```

만 사용한다.

아직 구현되지 않은 별도 즉사 효과나 신규 효과 타입은 추가하지 않았다.

### 14. 전체 적 공격 카드 추가

일부 카드는 `AllEnemies` 대상으로 구성했다.

예:

```text
휩쓸기
검풍
마력 폭풍
주술 파동
성광 파동
```

다수의 적이 등장하는 조우에서도 카드 대상 처리와 전체 피해 처리를 쉽게 테스트할 수 있다.

### 15. 즉사급 DEBUG 카드 유지

기존 테스트용:

```text
[DEBUG] 즉사
```

카드는 그대로 유지한다.

설정:

```text
AP 0
SingleEnemy
Damage
Physical
Damage 999999
```

별도의 강제 사망 로직이 아니라 기존 일반 피해 처리에 매우 큰 수치를 넣는 방식이다.

따라서 테스트 중에도:

```text
피해 계산
↓
HP 감소
↓
사망 판정
↓
전투 승리 판정
↓
탐사 복귀
```

라는 실제 전투 흐름을 그대로 검증할 수 있다.

### 16. 테스트 덱 공격 카드 확장

`Deck_Test.asset`에 신규 공격 카드 15종을 추가했다.

또한 `[DEBUG] 즉사` 카드를 총 5장 포함하도록 구성하여 셔플된 덱에서도 진행 검사용 카드를 비교적 쉽게 뽑을 수 있도록 했다.

최종 테스트 덱 카드 수:

```text
41장
```

기존 테스트 카드들은 삭제하지 않고 유지한다.

### 17. 카드 소유자 구조 유지

신규 공격 카드 역시 기존 `DeckCardEntry` 구조를 사용하고 기존 테스트 파티에 포함된 캐릭터를 Owner로 연결한다.

따라서 `DeckData.AreAllOwnersInParty()`의 기존 유효성 규칙을 우회하지 않는다.

### 18. 기존 카드 시스템 코드 미수정

이번 공격 카드 추가는 다음 전투 코드의 동작 방식을 변경하지 않는다.

```text
CardData
DeckData
BattleLoadoutData
BattleDeckRuntime
CardInstance
카드 피해 처리
```

ScriptableObject 카드 데이터와 `Deck_Test.asset`의 카드 목록만 확장했다.

이를 통해 테스트용 데이터가 실제 카드 시스템을 그대로 통과하도록 유지한다.

## 생성 파일

### 탐사·거점 루프

- `Assets/_ProjectC/Scripts/Exploration/ExplorationRunLoopDebugView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationRunLoopDebugView.cs.meta`

### 공격 카드

- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestQuickSlash.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestQuickSlash.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPiercingThrust.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPiercingThrust.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestHeavyStrike.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestHeavyStrike.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestOverheadSmash.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestOverheadSmash.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestSweepingSlash.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestSweepingSlash.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestBladeStorm.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestBladeStorm.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestArcaneBolt.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestArcaneBolt.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestArcaneBurst.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestArcaneBurst.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestArcaneStorm.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestArcaneStorm.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestFocusedRay.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestFocusedRay.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestHexStrike.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestHexStrike.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestHexWave.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestHexWave.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestShieldBash.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestShieldBash.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestRadiantShot.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestRadiantShot.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestRadiantWave.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestRadiantWave.asset.meta`

## 수정 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`
- `Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset`

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console 컴파일 오류 없음
- [ ] 조우 승리 시 EXP가 기존처럼 즉시 지급
- [ ] 조우 승리 시 Gold와 재료가 기존처럼 즉시 지급
- [ ] 각 조우의 실제 지급량이 런 합계에 누적
- [ ] 물자 창고 보너스가 적용된 실제 재료량이 정산 값과 일치
- [ ] Boss 승리 시 탐사 성공
- [ ] 탐사 성공 후 `거점으로 복귀` 버튼 표시
- [ ] 거점 복귀 시 `20_Lobby`로 이동
- [ ] Lobby 이동 후 지난 탐사 완료 층 유지
- [ ] Lobby 이동 후 클리어 조우 수 유지
- [ ] Lobby 이동 후 EXP·Gold·재료 런 합계 유지
- [ ] Lobby 정산 패널에서 호감도 성공 보상 표시
- [ ] Lobby에서 현재 캐릭터 레벨과 EXP 표시
- [ ] Lobby에서 현재 호감도·Gold·재료 표시
- [ ] F7 시설 강화 UI 정상 사용
- [ ] 시설 강화 시 실제 자원 차감
- [ ] 다음 탐사 시작 버튼 정상 동작
- [ ] 다음 탐사가 1F에서 시작
- [ ] 다음 탐사 시작 시 이전 Seed 초기화
- [ ] 다음 탐사 시작 시 클리어 조우 초기화
- [ ] 다음 탐사 시작 시 런 보상 합계 초기화
- [ ] 다음 탐사에서도 캐릭터 성장 유지
- [ ] 다음 탐사에서도 호감도 유지
- [ ] 다음 탐사에서도 보유 자원 유지
- [ ] 다음 탐사에서도 시설 강화 레벨 유지
- [ ] 신규 공격 카드 15종이 Unity에서 정상 로드
- [ ] 신규 공격 카드가 손패에 등장
- [ ] 단일 적 공격 카드 정상 사용
- [ ] 전체 적 공격 카드 정상 사용
- [ ] 물리 공격 카드 피해 정상 처리
- [ ] 마법 공격 카드 피해 정상 처리
- [ ] `[DEBUG] 즉사` 카드 AP 0 확인
- [ ] `[DEBUG] 즉사` 카드 피해 999999 적용
- [ ] DEBUG 카드로 적 처치 후 정상 Victory 처리
- [ ] DEBUG 카드로 Boss 처치 후 탐사 성공 흐름 정상 연결
- [ ] 테스트 덱 전체 카드 수 41장 확인

## 현재 단계의 제한 사항

45일차의 거점 정산 화면과 거점 복귀 버튼은 정식 UI가 아니라 기능 검증용 런타임 IMGUI다.

현재는 탐사 성공 루프만 정산과 거점으로 연결한다.

플레이어 사망과 탐사 실패, 부활, 실패 패널티는 이후 별도 단계에서 구현한다.

공격 카드 15종은 전투 기능 검증용 초기 데이터이며 최종 카드 밸런스와 이름, 아트, 효과 구성은 아니다.

`[DEBUG] 즉사` 카드는 개발 진행을 빠르게 확인하기 위한 테스트 전용 카드이며 실제 게임 밸런스 카드로 사용하지 않는다.

GitHub 저장소에는 Unity Editor 컴파일 및 Play Mode를 자동 검증하는 CI 상태 검사가 등록되어 있지 않으므로 실제 동작 확인은 로컬 Unity 환경에서 진행한다.

## 완료 결과

45일차를 통해 Project C의 핵심 메타 진행이 다음과 같은 하나의 순환 구조로 연결되었다.

```text
거점
↓
탐사
↓
전투
↓
Boss 승리
↓
탐사 성공
↓
거점 복귀
↓
탐사 정산
↓
시설 강화
↓
다음 탐사
```

탐사에서 획득한 실제 EXP와 자원을 런 단위로 기록하고, Lobby에서 결과와 현재 영구 진행을 함께 확인할 수 있게 되었다.

시설 강화를 마친 뒤 다음 탐사를 시작하면 런 데이터만 초기화되고 캐릭터 성장·호감도·자원·시설 강화는 유지된다.

또한 공격 카드 15종과 즉사급 DEBUG 카드를 포함한 41장 테스트 덱을 구성하여 이후 탐사·전투·보스·정산 루프를 빠르게 반복 검증할 수 있는 개발 환경도 마련했다.
