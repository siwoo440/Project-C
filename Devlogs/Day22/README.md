# 22일차 : 상태 이상 해제·면역 및 상세 UI 시스템 구축

---
## 개발 목표

- 상태 이상 적용 결과 세분화
- 디버프 전체 해제 규칙 추가
- 새 디버프를 차단하는 상태 면역 구현
- 상태 아이콘형 요약과 상세 툴팁 제공
- 적용·중첩·차단·해제 결과를 전투 화면에 표시

---
## 구현 내용

### 1. 상태 이상 적용 결과 추가

- `Invalid`: 잘못된 데이터 또는 적용 불가능한 대상
- `Applied`: 신규 상태 이상 적용
- `Stacked`: 기존 상태 이상 중첩 및 지속 시간 갱신
- `BlockedByImmunity`: 상태 면역에 의한 디버프 차단
- 기존 `bool` 결과를 명확한 적용 결과 열거형으로 교체

### 2. 디버프 정화 기능 추가

- `RemoveDebuffs` 카드 효과 종류 추가
- 대상에게 적용된 모든 디버프 제거
- 재생·공격 증가·상태 면역 같은 버프 유지
- 제거된 디버프 개수 반환
- 디버프가 없는 유닛은 정화 대상에서 제외
- 사망한 유닛은 정화 대상에서 제외

### 3. 상태 면역 기능 추가

- `StatusImmunity` 상태 이상 종류 추가
- 면역 상태에서 새로 들어오는 중독 차단
- 재생과 공격 증가 같은 버프는 정상 적용
- 면역 적용 전에 존재하던 디버프는 유지
- 면역 상태도 기존 지속 시간과 만료 규칙 사용
- 상태 면역 최대 중첩 1회 적용

### 4. 상태 이상 UI 개선

- 기존 긴 상태 문구를 `[독]`, `[재]`, `[공]`, `[면]` 아이콘형 문구로 변경
- 아이콘 옆에 현재 중첩과 남은 턴 표시
- 버프는 녹색, 디버프는 붉은색으로 구분
- 상태가 없는 경우 요약 UI와 상세 툴팁 자동 숨김

### 5. 상태 상세 툴팁 추가

- 유닛에 마우스를 올리면 상세 툴팁 표시
- 유닛 이름과 현재 상태 목록 표시
- 상태 이름과 버프·디버프 분류 표시
- 실제 적용 수치와 효과 설명 표시
- 현재·최대 중첩과 남은 턴 표시
- 전용 Canvas 정렬을 사용해 다른 유닛 UI보다 위에 표시

### 6. 전투 피드백 추가

- 신규 상태 적용 문구 표시
- 상태 중첩 수 문구 표시
- 디버프 차단 시 `면역` 문구 표시
- 정화 시 제거한 디버프 개수 표시
- 버프와 디버프 피드백 색상 구분
- 기존 플로팅 텍스트와 강조 연출 재사용

### 7. 테스트 카드 추가

- `정화`: 아군 한 명의 모든 디버프 제거
- `상태 면역`: 아군 한 명에게 2턴 동안 디버프 면역 부여
- `아군 중독 시험`: 정화와 면역 검증용 아군 대상 중독
- 신규 카드 3장을 기존 테스트 덱에 연결

---
## 상태 이상 상호작용 규칙

- 정화는 디버프만 제거하고 버프는 유지
- 상태 면역은 새로 들어오는 디버프만 차단
- 상태 면역 적용 전에 존재하던 중독은 유지
- 면역 상태에서도 버프 적용 가능
- 면역이 만료되면 디버프를 다시 적용 가능
- 상태 이상은 전투 종료 후 유지하지 않음

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectApplyResult.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectApplyResult.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectTooltipView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectTooltipView.cs.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestCleanse.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestCleanse.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestStatusImmunity.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestStatusImmunity.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAllyPoison.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAllyPoison.asset.meta`
- `Devlogs/Day22/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Data/BattleStatusEffectType.cs`
- `Assets/_ProjectC/Scripts/Data/CardEffectType.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleActionSequenceRunner.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardTooltipView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCombatFeedbackView.cs`
- `Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset`

---
## 삭제 파일

- 없음

---
## 검증 결과

- C# 전체 프로젝트 빌드: 경고 0건, 오류 0건
- Git 변경 형식 검사: 통과
- Unity 메타 GUID 중복 검사: 0건
- 변경 C# 파일의 한글 주석 규칙 검사: 통과
- 신규 테스트 카드의 테스트 덱 참조 확인
- Canvas·Scene·Prefab 변경: 없음

Unity 플레이 모드에서는 `00_Boot` 씬부터 시작해 아군 중독, 정화, 기존 디버프 유지, 면역 차단, 면역 만료 및 상태 상세 툴팁을 최종 확인해야 한다.

---
## 다음 개발 방향

- 물리·마법 방어 증가와 약화 상태 추가
- 상태 이상 해제 대상 선택 규칙 확장
- 적 행동의 상태 이상 부여 기능 추가
- 상태별 전용 시각 효과와 사운드 연결

---
## 커밋 제목

`22일차 : 상태 이상 해제·면역 및 상세 UI 시스템 구축`
