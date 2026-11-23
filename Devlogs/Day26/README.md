---
# 26일차 : 정신력·각성·붕괴 전투 시스템 통합

---
## 개발 목표

- 모든 아군과 적에게 독립적인 정신력 `0~100` 적용
- 공격·피격·처치·회복·사망·교란·카드 효과에 따른 정신력 증감 통합
- 정신력 `100`의 각성과 `0`의 붕괴 상태 구현
- 각성·붕괴의 3턴 지속과 종료 후 정신력 `50` 복귀 처리
- 정신 상태를 카드·적 행동·상태효과·전투 결과 저장과 연결
- 캐릭터 화면에 정신력 게이지와 상태 피드백 자동 생성

---
## 구현 내용

---
### 1. 개별 정신력 런타임 추가

- 아군과 적 모두 초기 정신력 `50` 적용
- 최소 `0`, 최대 `100` 범위 제한
- 정신력 변화 전후 값과 변화 원인 기록
- 특수 상태와 남은 턴을 변화 결과에 포함
- 큰 피해·회복·교란의 턴당 적용 횟수 제한

---
### 2. 전투 행동 정신력 연동

- 피해를 가한 유닛 정신력 `+1`
- 피해를 받은 유닛 정신력 `-1`
- 최대 체력의 10% 이상 피해를 받으면 추가 `-5`
- 적 처치 시 공격자 정신력 `+5`
- 실제 체력 회복 시 대상 정신력 `+1`
- 아군 사망 시 생존 아군 정신력 `-6`
- 적 사망 시 생존 적 정신력 `-2`
- 진영의 최후 생존자는 턴 시작에 정신력 `-3`

---
### 3. 각성·붕괴 상태 구현

- 정신력 `100` 도달 직후 각성 발동
- 정신력 `0` 도달 직후 붕괴 발동
- 각성·붕괴 지속시간 `3턴`
- 해당 진영 턴 종료마다 남은 턴 감소
- 지속시간 종료 후 일반 상태와 정신력 `50` 복귀
- 전투 종료 시 남아 있는 각성·붕괴 즉시 해제
- 특수 상태 중 추가 정신력 증감을 잠가 반복 발동 방지

---
### 4. 정신 상태 전투 수치 연결

- 각성 상태의 피해량과 회복량 `10%` 증가
- 붕괴 상태의 피해량과 회복량 `10%` 감소
- 캐릭터와 적 데이터에서 상태별 변화율 개별 설정 가능
- 카드 피해·카드 회복·적 공격에 동일한 상태 보정 적용
- 적 행동 예고에도 정신 상태가 반영된 피해량 표시
- 붕괴 상태 자체는 카드 사용과 대상 선택을 차단하지 않음

---
### 5. 카드와 상태효과 연동

- 정신력 직접 변화 카드 효과 종류 추가
- 교란 상태효과 추가
- 교란 대상 정신력 `-4`
- 교란 적용자 정신력 `+2`, 턴당 최대 2회
- 정신력 변화 카드의 대상 유효성 검사와 툴팁 표시 연결
- 정신력 테스트 카드 3종을 테스트 덱에 등록

---
### 6. 정신력 UI와 저장 처리

- 유닛 화면 아래 정신력 게이지 런타임 자동 생성
- 붕괴 방향은 붉은색, 중립은 흰색, 각성 방향은 금색으로 표시
- 일반 상태는 현재 정신력 수치 표시
- 각성·붕괴 상태는 이름과 남은 턴 표시
- 정신력 증감과 상태 발동·종료 플로팅 문구 표시
- 전투 결과에 아군 정신력 저장
- 승리·패배·도주 이후 체력과 정신력 상태 유지

---
## 정신력 규칙

- 모든 정신력 변화는 하나의 런타임 처리 경로 사용
- 임계값 도달 직후 각성 또는 붕괴를 한 번 판정
- 큰 피해 감소는 턴당 최대 2회
- 회복 정신력 증가는 턴당 최대 3회
- 같은 진영 사망 감소는 한 단계당 최대 1회
- 교란 적용자 증가는 턴당 최대 2회
- 사망 유닛은 추가 정신력 변화 대상에서 제외
- 전투 종료 시 특수 상태만 해제하고 일반 상태 정신력은 유지

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleMentalState.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleMentalChangeReason.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleMentalChangeResult.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleMentalRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleMentalController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleMentalView.cs`
- 위 C# 파일의 Unity 메타 파일 6개
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMentalFocus.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMentalBreak.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestDisrupt.asset`
- 위 테스트 카드의 Unity 메타 파일 3개
- `Devlogs/Day26/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Data/CharacterData.cs`
- `Assets/_ProjectC/Scripts/Data/EnemyData.cs`
- `Assets/_ProjectC/Scripts/Data/CardData.cs`
- `Assets/_ProjectC/Scripts/Data/CardEffectType.cs`
- `Assets/_ProjectC/Scripts/Data/BattleStatusEffectType.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardTooltipView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAction.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleResultManager.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitResultData.cs`
- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/ScriptableObjects/Characters/Character_Test.asset`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_Test.asset`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_TestPoison.asset`
- `Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset`

---
## 삭제 파일

- 없음

---
## 검증 결과

- GitHub 원격 `main`과 작업 기준 커밋 일치 확인
- C# 런타임 프로젝트 빌드: 경고 0건, 오류 0건
- Git 변경 형식 검사: 통과
- Unity 메타 GUID 중복 검사: 0건
- 테스트 덱 카드 참조 누락 검사: 0건
- 신규·추가 C# 코드의 한글 주석 규칙 검사: 통과
- 런타임 생성 UI 사용으로 Scene·Prefab·Canvas 직접 변경 없음

Unity 플레이 모드에서는 정신력 증감, 각성·붕괴 `3T → 2T → 1T → 50` 복귀, 상태별 피해·회복 보정, 교란 증감, 도주 후 체력·정신력 유지를 최종 확인해야 한다.

---
## 다음 개발 방향

- 캐릭터별 고유 각성·붕괴 효과 데이터 구조 확장
- 각성·붕괴 전용 화면 색상·애니메이션·효과음 추가
- 이후 구현될 치명타·보호막·보스·정예 판정과 정신력 규칙 연결
- 정신력 변화 이력과 전투 디버그 화면 추가

---
## 커밋 제목

`26일차 : 정신력·각성·붕괴 전투 시스템 통합`
