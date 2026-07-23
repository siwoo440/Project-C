\# 13일차 : 전투 턴 순환 및 적 행동 구현



\## 완료 내용



\- `TEST STRIKE` 카드의 피해량을 8로 복구

\- 적 처치 연출용 `Unit Sprite Renderer` 연결 확인

\- 전투 단계 열거형 추가

&#x20; - Battle Setup

&#x20; - Player Turn

&#x20; - Enemy Intent

&#x20; - Enemy Action

&#x20; - Status Processing

&#x20; - Battle Ended

\- `END TURN` 버튼으로 플레이어 턴 종료 기능 구현

\- 턴 종료 시 남은 손패를 버린 카드 더미로 이동하도록 구현

\- 적 행동 예고 문구 표시 기능 구현

\- 테스트 적이 아군에게 12 피해를 주는 행동 구현

\- 적 턴 동안 카드 선택, 카드 사용, 턴 종료 입력 차단

\- 적 행동 후 상태 처리 단계를 거쳐 다음 라운드로 진행하도록 구현

\- 다음 라운드 시작 시 AP를 최대치로 회복하고 카드 4장 드로우하도록 구현

\- 전투 승리 시 진행 중인 턴 처리를 중단하도록 연결

\- 아군 체력이 0이 되면 다음 라운드로 넘어가지 않도록 임시 패배 처리 구현

\- `CanUseCard()`의 접근 불가 코드 경고(CS0162) 수정

\- Console 빨간색 오류 없이 기본 턴 순환 테스트 완료



\## 확인한 전투 흐름



플레이어 턴 → 턴 종료 → 적 행동 예고 → 적 공격 → 상태 처리 → 다음 라운드 → AP 회복 및 카드 드로우



\## 변경 파일



\- `Assets/\_ProjectC/Data/Cards/SO\_Card\_TestStrike.asset`

\- `Assets/\_ProjectC/Scenes/20\_Battle.unity`

\- `Assets/\_ProjectC/Scripts/Battle/BattleTurnPhase.cs`

\- `Assets/\_ProjectC/Scripts/Battle/BattleEnemyActionRuntime.cs`

\- `Assets/\_ProjectC/Scripts/Battle/BattleTurnRuntime.cs`

\- `Assets/\_ProjectC/Scripts/Battle/BattleTurnUI.cs`

\- `Assets/\_ProjectC/Scripts/Battle/BattleDeckRuntime.cs`

\- `Assets/\_ProjectC/Scripts/Battle/BattleHandUI.cs`

\- `Assets/\_ProjectC/Scripts/Battle/BattleCardView.cs`



\## 다음 작업



\- 아군 패배 UI 및 재시작·전투 이탈 처리

\- 상태이상 실제 효과 처리

\- 적 여러 명의 행동 순서 및 속도 규칙 연결

