---
# 9일차 개발일지 - 전투 손패 카드 UI 및 자동 갱신 구축

---
## 개발 목표

8일차에서 구현한 `BattleDeckRuntime`의 손패와 상태 변경 이벤트를 전투 화면에 연결하여, 현재 손패를 카드 UI로 확인할 수 있는 구조를 구축했다.

9일차 완료 기준은 다음과 같다.

- 하단 카드 전용 영역 유지
- 현재 손패 카드 자동 생성
- 카드 이름·설명·종류·소유자·AP 비용 표시
- 카드 일러스트 표시
- 덱·손패·버린 카드 수량 표시
- 덱 상태 변경에 따른 손패 자동 갱신
- 프리팹 참조 없이 코드 기반 Canvas UI 생성

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Project Type | 2D |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| Card Area | `CardAreaRoot` |

---
## 1. BattleCardView 구현

손패 카드 한 장을 화면에 표시하는 `BattleCardView`를 추가했다.

표시 정보:

- 카드 일러스트
- 카드 이름
- 카드 설명
- 카드 종류
- 카드 소유 캐릭터
- AP 비용

카드 배경, 일러스트 영역, 텍스트, 비용 배지는 코드에서 자동으로 생성한다.

---
## 2. BattleHandView 구현

현재 손패 전체를 관리하는 `BattleHandView`를 추가했다.

주요 기능:

- `BattleDeckRuntime` 연결
- 손패 카드 목록 순회
- 카드 화면 생성과 제거
- 카드 가로 정렬
- 덱·손패·버린 카드 수량 출력
- 기존 덱 이벤트 연결 해제

카드 영역의 배경, 상태 텍스트, 가로 레이아웃도 코드에서 자동 생성한다.

---
## 3. 덱 상태 자동 갱신

`BattleDeckRuntime.StateChanged` 이벤트를 구독하여 카드 상태가 변경될 때마다 손패 화면을 다시 구성하도록 연결했다.

다음 동작 이후 화면이 자동으로 갱신된다.

- 카드 드로우
- 손패 카드 버리기
- 카드 더미 셔플
- 버린 카드 더미 재구성

---
## 4. BattleSceneSetup 연결

전투 초기화 과정에 `BattleHandView` 연결을 추가했다.

변경된 초기화 순서:

1. 전투 설정 검사
2. 아군과 적 유닛 생성
3. 런타임 덱 생성
4. 손패 화면과 런타임 덱 연결
5. 시작 손패 드로우
6. 상태 변경 이벤트를 통한 카드 UI 생성

손패 화면이 누락되거나 덱 연결에 실패하면 오류를 출력하고 초기화를 중단한다.

---
## 5. Canvas 카드 영역 연결

`40_Battle` 씬의 `CardAreaRoot`에 `BattleHandView`를 직접 연결했다.

전투 유닛 영역과 하단 카드 영역은 기존 분리 배치를 유지하며, 생성되는 카드는 `CardAreaRoot` 내부에서만 가로로 정렬된다.

---
## 6. 프리팹 참조 오류 수정

초기 구현에서 카드 프리팹의 컴포넌트 참조가 실행 시 `null`로 해석되어 손패 화면 연결이 실패했다.

해당 프리팹 의존성을 제거하고 카드 오브젝트와 필수 UI 컴포넌트를 코드에서 직접 생성하도록 변경했다. 이에 따라 별도의 Inspector 프리팹 연결 없이 손패 카드가 생성된다.

---
## 생성·수정·삭제 파일

생성:

- `Assets/_ProjectC/Scripts/Battle/BattleCardView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardView.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleHandView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleHandView.cs.meta`
- `Devlogs/Day09/README.md`

수정:

- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/Scenes/40_Battle.unity`

최종 결과에서 삭제된 추적 파일은 없다. 구현 중 생성했던 미사용 카드 프리팹은 커밋 전에 제거했다.

---
## 검증 결과

- Unity 참조 기반 C# 컴파일 오류 0건
- 컴파일 경고 0건
- 씬 직렬화 ID 중복 0건
- `BattleHandView` 스크립트 GUID 연결 정상
- 미사용 카드 프리팹 참조 0건
- Git 공백 오류 0건

Unity Play Mode의 실제 카드 표시와 해상도별 배치는 자동 검증 환경에서 실행하지 못했으므로 Unity Editor에서 최종 확인이 필요하다.

---
## 9일차 결과

전투 시작 시 현재 손패를 하단 카드 영역에 표시하고, 카드 상태가 변경되면 화면이 자동으로 갱신되는 기본 손패 UI를 구축했다.

카드 UI를 코드에서 직접 생성하도록 구성하여 프리팹 연결 누락으로 인한 초기화 실패도 방지했다.

---
## 다음 개발 방향

다음 단계에서는 카드 선택, 대상 지정, AP 소비, 카드 사용 및 버린 카드 더미 이동을 연결하여 실제 전투 행동 흐름을 구현한다.

---
## Commit

`9일차 : 전투 손패 카드 UI 및 자동 갱신 구축`
