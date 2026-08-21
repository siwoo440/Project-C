---
# 23일차 : 방어력 증감 상태 및 적 상태 이상 행동 시스템 구축

---
## 개발 목표

- 물리 방어력과 마법 저항력의 증가·약화 상태 구현
- 상태 중첩을 반영하는 실제 방어 수치 계산
- 적의 상태 이상 부여 행동과 행동 예고 추가
- 상태 면역·정화 시스템과 신규 디버프 연동
- 테스트 카드와 상태 행동 적을 전투 씬에 배치

---
## 구현 내용

---
### 1. 방어 증감 상태 추가

- `PhysicalDefenseUp`: 물리 방어력 증가
- `PhysicalDefenseDown`: 물리 방어력 감소
- `MagicalResistanceUp`: 마법 저항력 증가
- `MagicalResistanceDown`: 마법 저항력 감소
- 증가 상태는 버프, 감소 상태는 디버프로 분류
- 상태 아이콘·이름·상세 설명 추가

---
### 2. 기본 방어력과 현재 방어력 분리

- 기본 물리 방어력과 기본 마법 저항력 별도 보관
- 증가 상태 전체 수치에서 감소 상태 전체 수치를 차감
- 중첩 수를 실제 방어 수치에 반영
- 최종 방어력이 0보다 작아지지 않도록 제한
- 피해 계산과 적 피해 예고에서 현재 방어력 사용

---
### 3. 적 상태 이상 행동 추가

- 적 행동 종류에 `ApplyStatusEffect` 추가
- 상태 종류·수치·지속 시간·최대 중첩 데이터 지원
- 기존 적 속도와 대상 선택 규칙 재사용
- 공격과 상태 행동을 공통 실행 결과로 반환
- 행동 실행 후 적용·중첩·면역·실패 결과 표시

---
### 4. 적 행동 예고 확장

- 상태 이상 이름과 적용 수치 표시
- 상태 지속 턴과 대상 이름 표시
- 대상이 디버프 면역 상태인 경우 `면역 예상` 표시
- 아군 방어 상태와 면역 상태가 변하면 예고 즉시 갱신
- 기존 물리·마법 피해 예고 유지

---
### 5. 기존 상태 시스템 연동

- 물리 방어 약화와 마법 저항 약화를 디버프로 등록
- 상태 면역으로 신규 방어 약화 차단
- 정화 카드로 적용된 방어 약화 제거
- 기존 중첩·지속 시간·만료 규칙 재사용
- 상태 상세 툴팁에서 실제 증감 수치 확인 가능

---
### 6. 테스트 데이터 추가

- `물리 방어 강화`: 아군 물리 방어력 3 증가
- `물리 방어 약화`: 적 물리 방어력 3 감소
- `마법 저항 강화`: 아군 마법 저항력 3 증가
- `마법 저항 약화`: 적 마법 저항력 3 감소
- 신규 카드 4장을 기존 테스트 덱에 연결
- 무작위 아군에게 2턴 중독을 부여하는 `중독 실험체` 추가
- 전투 씬 적 목록에 중독 실험체 자동 배치

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleEnemyActionResult.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyActionResult.cs.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPhysicalDefenseUp.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPhysicalDefenseUp.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPhysicalDefenseDown.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestPhysicalDefenseDown.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMagicalResistanceUp.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMagicalResistanceUp.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMagicalResistanceDown.asset`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestMagicalResistanceDown.asset.meta`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_TestPoison.asset`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_TestPoison.asset.meta`
- `Devlogs/Day23/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Data/BattleStatusEffectType.cs`
- `Assets/_ProjectC/Scripts/Data/EnemyActionType.cs`
- `Assets/_ProjectC/Scripts/Data/EnemyData.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyAction.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleEnemyActionRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_Test.asset`
- `Assets/_ProjectC/ScriptableObjects/Decks/Deck_Test.asset`
- `Assets/_ProjectC/Scenes/40_Battle.unity`

---
## 삭제 파일

- 없음

---
## 검증 결과

- C# 전체 프로젝트 빌드: 경고 0건, 오류 0건
- Unity 스크립트 컴파일 및 어셈블리 재생성: 성공
- Git 변경 형식 검사: 통과
- Unity 메타 GUID 중복 검사: 0건
- 변경 C# 파일의 한글 주석 규칙 검사: 통과
- 신규 카드 4장의 테스트 덱 참조 확인
- 신규 적의 전투 씬 참조 확인

Unity 플레이 모드에서는 방어 증감에 따른 피해 예고 변화, 중독 실험체의 상태 행동, 상태 면역 차단 및 정화 결과를 최종 확인해야 한다.

---
## 다음 개발 방향

- 적 행동 패턴을 여러 행동 중 선택하는 구조로 확장
- 상태 이상 행동의 전용 연출과 색상 추가
- 유닛 상세 정보에 기본·현재 방어 수치 표시
- 전투 밸런스 조정용 수치 데이터 정리

---
## 커밋 제목

`23일차 : 방어력 증감 상태 및 적 상태 이상 행동 시스템 구축`
