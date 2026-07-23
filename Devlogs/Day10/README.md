\# 10일차 개발 일지



\## 개발 목표



전투 카드의 단일 적 대상 선택 구조를 완성하고, New Input System 환경에서 적 클릭 후 카드가 정상적으로 사용되도록 수정한다.



\## 개발 환경



\* Unity 6

\* URP

\* New Input System

\* 2D 전투 씬: `20\_Battle`



\## 구현 내용



\### 1. 카드 대상 유형 구조 추가



\* `CardData`에 대상 유형 정보를 추가했다.

\* 공격 카드가 적 하나를 선택하는 `Single Enemy` 유형을 사용할 수 있도록 구성했다.

\* 테스트 공격 카드의 `Target Type`을 `Single Enemy`로 설정했다.



\### 2. 전투 유닛 진영 구분 추가



\* 전투 유닛에 아군과 적을 구분하는 `Unit Team` 정보를 추가했다.

\* 대상 선택 시 `Enemy` 진영이며 전투 불능 상태가 아닌 유닛만 선택하도록 구성했다.



\### 3. 대상 선택 관리자 구현



\* `BattleTargetSelectionRuntime`을 추가했다.

\* 카드 사용 버튼 클릭 후 대상 선택 모드로 진입하도록 구성했다.

\* 선택 가능한 적을 등록하고, 적 선택 후 카드 사용 처리를 실행하도록 연결했다.

\* 대상 선택 취소 시 카드와 AP가 유지되도록 구성했다.



\### 4. 적 대상 표시 구현



\* `BattleTargetView`를 추가했다.

\* 선택 가능한 적 주변에 사각형 윤곽선을 표시하도록 구성했다.

\* 마우스를 적 위에 올리면 윤곽선 색상이 변경되도록 구성했다.

\* 선택 모드 종료 시 윤곽선이 숨겨지도록 처리했다.



\### 5. New Input System 클릭 처리 수정



\* 기존 `OnMouseEnter`, `OnMouseExit`, `OnMouseDown` 방식을 Pointer 이벤트 방식으로 교체했다.

\* `IPointerEnterHandler`, `IPointerExitHandler`, `IPointerClickHandler`를 사용하도록 수정했다.

\* `Main Camera`에 `Physics 2D Raycaster`를 추가해 `BoxCollider2D`가 있는 적을 클릭할 수 있도록 구성했다.

\* 적 클릭 후 대상 선택 관리자에 선택 요청이 전달되도록 연결했다.



\### 6. 전투 씬 및 프리팹 설정 저장



\* `20\_Battle` 씬에 `BattleTargetSelectionRuntime`을 배치했다.

\* `BattleHandUI`, `BattleDeckRuntime`, `BattleActionPointRuntime`, `BattleTargetSelectionRuntime` 참조를 연결했다.

\* 적 프리팹에 `BattleUnit`, `BattleTargetView`, `BoxCollider2D`를 연결했다.

\* 적의 `Unit Team`을 `Enemy`로 설정했다.

\* `TargetOutline`의 Line Renderer 두께와 Material을 정리했다.



\## 테스트 결과



\* `Single Enemy` 카드 선택 가능

\* `USE CARD` 클릭 후 대상 선택 모드 진입 확인

\* 선택 가능한 적의 윤곽선 표시 확인

\* 적 마우스 진입 및 이탈 시 윤곽선 색상 변경 확인

\* 적 클릭 후 카드 사용 처리 확인

\* 카드 사용 후 AP 차감 확인

\* 카드 사용 후 Hand 감소 및 Discard 증가 확인

\* 대상 선택 취소 시 카드와 AP 유지 확인

\* 전투 불능 적 선택 불가 확인

\* Console 빨간색 오류 0개 확인



\## 현재 미구현 항목



\* 선택된 적에게 카드 피해량 적용

\* 카드 효과별 회복, 보호막, 상태 이상 처리

\* 적 체력 UI 갱신

\* 적 처치 및 전투 승패 판정



