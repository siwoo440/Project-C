---
# 25일차 : 상태효과 발동·지속시간·정화 처리 통합

---
## 개발 목표

- 상태효과 발동과 제거 과정을 하나의 공통 처리기로 통합
- 턴 시작 효과와 카드 정화가 같은 처리 결과 구조를 사용하도록 정리
- 상태효과 지속시간 감소와 자연 만료 순서 명확화
- 상태 변경 알림과 UI 갱신 횟수 최소화
- 처리 결과를 로그와 화면 피드백으로 확인 가능하도록 구성

---
## 구현 내용

---
### 1. 상태 처리 결과 데이터 추가

- `BattleStatusEffectProcessResult` 추가
- 처리 상태 종류와 라운드 저장
- 처리 시점 수치와 중첩 수 저장
- 실제 피해 또는 회복량 저장
- 처리 전후 남은 지속 횟수 저장
- 지속시간 만료·정화·사망 제거 원인 구분

---
### 2. 공통 상태 처리기 추가

- `BattleStatusEffectProcessor` 추가
- 중독의 방어 무시 피해 처리
- 재생의 체력 회복 처리
- 공격·방어 증감과 면역 상태의 지속시간 감소
- 지속시간이 0이 된 상태의 자연 만료 처리
- 상태 피해로 사망하면 남은 상태 처리 중단

---
### 3. 정화 처리 통합

- 카드 정화를 공통 상태 처리기로 연결
- 버프는 유지하고 디버프만 제거
- 제거된 디버프별 처리 결과 생성
- 정화 대상이 없을 때 기존 실패 피드백 유지
- 정화된 상태 이름·중첩·남은 횟수 로그 출력

---
### 4. 턴 시작 처리 연결

- 플레이어 턴 시작에 아군 상태 처리
- 적 턴 시작에 적 상태 처리
- 효과 발동 후 지속시간 감소
- 지속시간 감소 후 만료 상태 제거
- 상태 처리 완료 결과를 전투 화면에 이벤트로 전달

---
### 5. UI와 전투 로그 연동

- 기존 피해·회복 플로팅 숫자 유지
- 자연 만료 시 상태 이름과 만료 문구 표시
- 상태 목록 변경을 유닛당 한 번만 알림
- 상태 처리 라운드·발동량·지속시간·제거 원인 출력
- 방어력과 면역 변경 시 적 행동 예고 갱신 유지

---
## 상태 처리 규칙

- 아군 상태는 플레이어 턴 시작에 처리
- 적 상태는 적 턴 시작에 처리
- 중독과 재생은 지속시간 감소 전에 발동
- 능력치 상태는 현재 수치를 유지한 뒤 지속시간 감소
- 지속시간이 0이면 같은 처리 단계에서 제거
- 면역은 새 디버프 적용만 차단하고 기존 디버프를 제거하지 않음
- 정화는 디버프만 제거
- 상태 피해로 유닛이 사망하면 해당 유닛의 남은 상태 처리 중단

---
## 생성 파일

- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectProcessResult.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectProcessResult.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectProcessor.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectProcessor.cs.meta`
- `Devlogs/Day25/README.md`

---
## 수정 파일

- `Assets/_ProjectC/Scripts/Battle/BattleUnitRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleStatusEffectController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`

---
## 삭제 파일

- 없음

---
## 검증 결과

- GitHub 최신 커밋과 로컬 기준 커밋 일치 확인
- C# 런타임 프로젝트 빌드: 경고 0건, 오류 0건
- Git 변경 형식 검사: 통과
- Unity 메타 GUID 중복 검사: 0건
- 신규 C# 파일의 한글 주석 규칙 검사: 통과
- 상태 처리기 생성과 턴·카드 연결 참조 확인
- Scene·Prefab·Canvas 직접 변경: 없음

Unity 플레이 모드에서는 중독·재생의 진영 턴 시작 발동, 지속시간 `2 → 1 → 0` 감소, 자연 만료 피드백, 정화의 디버프 선택 제거, 상태 피해 사망 시 전투 종료를 최종 확인해야 한다.

---
## 다음 개발 방향

- 캐릭터별 정신력 0~100 런타임 데이터 추가
- 정신력 최소·최대 범위 제한
- 전투 결과와 Scene 전환 시 정신력 상태 유지 기준 정의
- 정신력 UI 표시 기반 준비

---
## 커밋 제목

`25일차 : 상태효과 발동·지속시간·정화 처리 통합`
