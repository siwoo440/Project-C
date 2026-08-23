# Project C - 47일차 개발일지

## 작업 주제

전투 행동 예고 UI 개편, 적 분석 UI, 타겟 화살표 표시 및 카드 드래그 타겟팅 시스템 구축

## 개발 목표

- 적 머리 위 장문 행동 예고를 작은 행동 아이콘으로 변경
- 행동 아이콘 Hover 시 행동 상세 정보 표시
- 행동 아이콘 클릭 시 상세 정보 고정
- 적 본체 클릭 시 적 능력치·약점 정보를 별도 패널로 표시
- 적이 노리는 대상을 화살표로 연결
- 화살표 전체 보기 / 하나씩 보기 옵션 추가
- 기존 클릭형 카드 사용을 드래그 타겟팅 방식으로 변경
- 유효하지 않은 위치에 카드를 놓으면 카드 사용 취소
- ESC 입력으로 카드 드래그 취소
- 기존 46일차 피해 계산 DEBUG UI와 함께 전투 판정 검증 유지

## 구현 내용

### 1. 적 행동 예고 아이콘화

기존 적 머리 위 장문 행동 예고를 숨기고 작은 행동 아이콘으로 교체했다.

```text
공격 행동
ATK
예상 피해

상태 행동
FX
효과 수치

보스 특수 패턴
!
예상 수치
```

행동 아이콘에는 현재 예정 행동의 핵심 정보만 표시한다.

### 2. 행동 상세 정보 Hover / Click

행동 아이콘에 마우스를 올리면 다음 행동 정보만 표시한다.

```text
행동 이름
패턴 순번
행동 순서
속도
피해 유형
예상 피해
상태 효과
지속 턴
현재 대상
보스 특수 패턴 경고
```

행동 아이콘을 클릭하면 상세 설명이 고정되며, 같은 아이콘을 다시 클릭하면 고정이 해제된다.

행동이 실행되어 예정 목록에서 사라지면 해당 상세 설명도 자동으로 닫힌다.

### 3. 적 정보와 행동 정보 분리

적 HP, 방어, 저항, 정신력, 약점은 행동 상세 창에서 제거했다.

적 본체를 클릭하면 별도의 적 분석 패널을 표시한다.

```text
적 이름
현재 HP / 최대 HP
물리 방어
마법 저항
현재 정신력
정신 상태
카드 계열 약점
적 설명
```

같은 적을 다시 클릭하면 패널이 닫히고, 다른 적을 클릭하면 해당 적 정보로 교체된다.

### 4. 적 행동 대상 화살표

각 적의 예정 행동과 실제 타겟을 화살표로 연결한다.

```text
Enemy A ─────────▶ Ally A
Enemy B ─────────▶ Ally B
Enemy C ─────────▶ Ally A
```

화살표 끝은 대상 캐릭터 머리 위를 가리킨다.

### 5. 화살표 전체 / 하나씩 보기

현재 강화 버튼 옆에 화살표 표시 방식 버튼을 추가했다.

```text
화살표 : 전체
↕
화살표 : 하나씩
```

전체 모드에서는 모든 예정 행동 화살표를 표시한다.

하나씩 모드에서는 기본적으로 하나의 화살표만 표시하며, 행동 아이콘 Hover 또는 Click 시 현재 확인 중인 행동의 화살표를 우선 표시한다.

이를 통해 적과 행동 수가 증가했을 때 화살표가 겹쳐 보이지 않는 문제를 줄였다.

### 6. 보스 특수 패턴 프로토타입

현재 별도의 특수 패턴 데이터가 없기 때문에 다음 임시 규칙으로 특수 패턴을 표시한다.

```text
Boss 전투
+
패턴이 2개 이상
+
현재 행동이 패턴 순환의 마지막 행동
```

특수 패턴은 일반 행동과 다른 아이콘 및 화살표 표시를 사용하고 상세 창에 경고 문구를 출력한다.

### 7. 카드 드래그 타겟팅

기존 카드 사용:

```text
카드 클릭
→ 대상 클릭
→ 카드 사용
```

47일차 카드 사용:

```text
카드 좌클릭 드래그
→ 대상까지 이동
→ 대상 위에서 마우스 버튼 해제
→ 카드 사용
```

드래그 중에는 반투명 카드 복제가 마우스를 따라 움직인다.

### 8. 유효 대상 강조

카드 드래그를 시작하면 현재 카드가 사용할 수 있는 대상만 강조한다.

```text
Self
→ 카드 소유자

SingleAlly / AllAllies
→ 생존 아군

SingleEnemy / AllEnemies
→ 생존 적
```

기존 `CardTargetType` 규칙을 그대로 사용한다.

### 9. 카드 드래그 타겟 화살표

카드를 드래그하는 동안 카드에서 현재 마우스 위치까지 별도의 타겟 화살표를 표시한다.

```text
[Card] ─────────▶ Mouse
```

유효한 대상 위에 마우스가 올라가면 표시가 변경되어 드롭 가능 여부를 쉽게 확인할 수 있다.

### 10. 유효 Drop 시 기존 카드 실행 시스템 재사용

유효 대상 위에 카드를 놓으면 기존 `BattleCardActionController` 흐름을 그대로 사용한다.

```text
카드 드래그
↓
유효 대상 Drop
↓
기존 카드 선택 처리
↓
대상 처리
↓
AP 검사
↓
행동 연출
↓
카드 효과 적용
↓
카드 버림
```

기존 전투 판정 로직은 유지하고 입력 방식만 드래그 방식으로 확장했다.

### 11. 잘못된 Drop 취소

다음 경우 카드 사용을 취소한다.

```text
빈 공간
잘못된 진영
사망한 대상
전투 대상과 관계없는 UI
```

취소 시:

```text
AP 소비 없음
카드 버림 없음
효과 발생 없음
손패 유지
```

### 12. ESC 드래그 취소

드래그 도중 ESC를 누르면 현재 드래그를 취소한다.

New Input System 방식인 `Keyboard.current`를 사용한다.

### 13. 기존 카드 클릭 입력 차단

새로운 드래그 방식과 기존 클릭 방식이 동시에 작동하지 않도록 런타임 설치기에서 기존 직접 카드 클릭 입력을 차단한다.

Hover 확대와 기존 카드 툴팁은 재사용한다.

### 14. TextMeshPro 경고 수정

폐기 예정 API였던:

```text
enableWordWrapping
```

사용을 제거하고 최신 방식인:

```text
textWrappingMode = TextWrappingModes.Normal
```

으로 변경했다.

## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleCardDragHandler.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardDragHandler.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleCardDragRuntimeInstaller.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardDragRuntimeInstaller.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAnalysisClickHandler.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAnalysisClickHandler.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAnalysisView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAnalysisView.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentDetailView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentDetailView.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentIconView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentIconView.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentRuntimePresenter.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyIntentRuntimePresenter.cs.meta`

## 수정 파일

없음

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console 컴파일 오류 없음
- [ ] `enableWordWrapping` 폐기 경고 없음
- [ ] 기존 장문 적 행동 예고가 숨겨짐
- [ ] 적 머리 위 ATK / FX 행동 아이콘 표시
- [ ] 행동 아이콘 Hover 시 행동 정보만 표시
- [ ] 행동 정보에 적 HP·방어·약점이 나오지 않음
- [ ] 행동 아이콘 클릭 시 상세 설명 고정
- [ ] 같은 행동 아이콘 재클릭 시 고정 해제
- [ ] 행동 실행 후 이전 고정 설명 제거
- [ ] 적 본체 클릭 시 적 정보 패널 표시
- [ ] 적 정보에 HP·방어·저항·정신력·약점 표시
- [ ] 같은 적 재클릭 시 적 정보 닫힘
- [ ] 다른 적 클릭 시 해당 적 정보로 교체
- [ ] 적 행동 대상 화살표 표시
- [ ] `화살표 : 전체`에서 모든 예정 화살표 표시
- [ ] `화살표 : 하나씩`에서 하나의 화살표만 표시
- [ ] 하나씩 모드에서 행동 아이콘 Hover 시 해당 화살표 표시
- [ ] 행동 아이콘 고정 시 해당 화살표 유지
- [ ] 화살표 옵션 버튼이 현재 강화 버튼 옆에 표시
- [ ] 카드 좌클릭 드래그 가능
- [ ] 드래그 중 반투명 카드 표시
- [ ] 드래그 중 유효 대상 강조
- [ ] 카드 드래그 타겟 화살표 표시
- [ ] 유효 대상 Drop 시 카드 정상 사용
- [ ] 빈 공간 Drop 시 카드 사용 취소
- [ ] 잘못된 대상 Drop 시 카드 사용 취소
- [ ] 취소 시 AP 감소 없음
- [ ] 취소 시 카드 버림 없음
- [ ] ESC로 드래그 취소 가능
- [ ] 기존 카드 행동 연출 정상 동작
- [ ] 46일차 F6 피해 계산 DEBUG UI 정상 동작

## 현재 단계의 제한 사항

- 행동 아이콘은 정식 이미지 에셋이 아닌 런타임 문자 기반 프로토타입이다.
- 적 행동 및 카드 드래그 화살표는 현재 IMGUI 기반 프로토타입 표시 방식이다.
- 보스 특수 패턴은 아직 전용 데이터가 없어 패턴 순환 마지막 행동을 임시 특수 패턴으로 사용한다.
- 향후 `IsSpecialPattern`, 경고 문구, 패턴 아이콘 등의 전용 데이터를 추가하는 것이 필요하다.
- GitHub에는 Unity Editor 컴파일 및 Play Mode 자동 CI 검사가 연결되어 있지 않으므로 최종 동작 확인은 로컬 Unity 실행 결과를 기준으로 한다.

## 완료 결과

47일차를 통해 적 행동 정보와 적 자체 정보를 분리하고, 적 행동의 적용 대상을 화살표로 직접 확인할 수 있게 했다.

```text
적 머리 위 행동 아이콘
→ 행동 요약

행동 아이콘 Hover / Click
→ 다음 행동 상세 정보

적 본체 Click
→ 적 능력치·약점 정보
```

화살표는 전체 보기와 하나씩 보기 모드를 지원해 적 수가 늘어났을 때도 전투 화면의 가독성을 유지할 수 있다.

카드 조작은 클릭 후 대상 선택 방식에서 드래그 후 대상 Drop 방식으로 변경되었으며, 잘못된 Drop이나 ESC 취소에서는 AP와 카드가 소비되지 않도록 처리했다.

이번 작업으로 이후 적 행동 종류와 보스 패턴이 늘어나더라도 행동 정보, 적 정보, 타겟 정보를 분리해서 확인할 수 있는 전투 UI 기반을 구축했다.
