# 21일차 : 상태 이상 적용 및 지속 시간 관리 시스템 구축

---
## 개발 목표

- 전투 유닛별 상태 이상 보관 구조 추가
- 중독·재생·공격력 증가 효과 구현
- 진영 턴 시작 기준 지속 효과 발동과 만료 처리
- 상태 이상 카드와 캐릭터 상태 UI 연결

---
## 구현 내용

### 1. 상태 이상 종류 정의

- `None`: 상태 이상 없음
- `Poison`: 방어력을 무시하는 지속 피해
- `Regeneration`: 지속 체력 회복
- `AttackPowerUp`: 카드 기본 피해 증가

### 2. 상태 이상 런타임 추가

- 상태 종류, 중첩당 수치, 남은 턴, 현재 중첩, 최대 중첩 관리
- 동일 효과 재적용 시 높은 효과 수치와 긴 지속 시간 유지
- 최대 중첩 범위에서 현재 중첩 증가
- 효과 수치와 중첩 수를 합산한 최종 적용값 제공

### 3. 유닛 상태 관리 추가

- 유닛별 상태 이상 목록 보관
- 상태 이상 적용·재적용·제거 기능 추가
- 상태 변경 이벤트를 통한 UI 자동 갱신
- 사망한 유닛의 모든 상태 이상 자동 제거
- 사망한 유닛에 새로운 상태 이상 적용 차단

### 4. 진영 턴 시작 발동 규칙 추가

- 플레이어 턴 시작 시 생존 아군 상태 이상 처리
- 적 턴 시작 시 생존 적 상태 이상 처리
- 중독과 재생 발동 후 남은 턴 감소
- 남은 턴이 0인 효과 자동 제거
- 중독으로 전멸하면 기존 승리·패배 판정 즉시 실행
- 상태 이상으로 전투가 종료되면 불필요한 행동과 카드 드로우 중단

### 5. 카드 효과 확장

- `ApplyStatusEffect` 카드 효과 종류 추가
- 카드 데이터에 상태 종류, 지속 턴, 최대 중첩 설정 추가
- 상태 이상 카드 대상 규칙을 기존 단일·전체 대상 시스템과 연결
- 공격력 증가 수치를 피해 카드 원본 피해에 합산
- 버프 카드는 지원 연출, 중독 카드는 공격 연출 사용

### 6. 상태 이상 UI 추가

- `BattleStatusEffectView`를 유닛 화면에 런타임으로 자동 추가
- 상태 이름, 최종 수치, 중첩 수, 남은 턴 표시
- 버프는 녹색, 디버프는 붉은색으로 구분
- 상태가 없으면 표시 영역 자동 숨김
- Canvas와 전투 유닛 프리팹의 수동 수정 제거

### 7. 테스트 카드 추가

- `중독 부여`: 적 1명에게 수치 4, 2턴, 최대 3중첩
- `재생 부여`: 아군 1명에게 수치 5, 2턴, 최대 3중첩
- `공격 증가`: 자신에게 수치 5, 2턴, 최대 1중첩
- 신규 카드 3장을 기존 테스트 덱에 연결

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Data/BattleStatusEffectType.cs`
- `Assets/_ProjectC/Scripts/Data/BattleStatusEffectType.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectInstance.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectController.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectView.cs.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPoison.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPoison.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestRegeneration.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestRegeneration.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAttackPowerUp.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAttackPowerUp.asset.meta`
- `Devlogs/Day21/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Data/CardEffectType.cs`
- `Assets/_ProjectC/Scripts/Data/CardData.cs`
- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleActionSequenceRunner.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardTooltipView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
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
- 신규 테스트 카드의 덱 참조 확인
- Canvas·Scene·Prefab 변경: 없음

Unity 플레이 모드에서는 `00_Boot` 씬부터 시작해 중독 피해, 재생 회복, 공격력 증가, 중첩, 만료 및 중독 사망 판정을 최종 확인해야 한다.

---
## 다음 개발 방향

- 상태 이상 아이콘과 상세 툴팁 추가
- 방어력 증가·약화·행동 제한 효과 확장
- 상태 이상 면역과 해제 카드 규칙 설계
- 효과 적용 및 해제 시 전용 전투 연출 추가

---
## 커밋 제목

`21일차 : 상태 이상 적용 및 지속 시간 관리 시스템 구축`
