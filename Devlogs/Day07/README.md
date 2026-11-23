---
# 7일차 개발일지 - 전투용 카드 인스턴스 및 런타임 덱 생성 구조 구축

---
## 개발 목표

5일차에서 만든 `DeckData`와 카드 소유자 구조를 6일차의 `BattleUnitRuntime`과 연결하여, 전투에서 사용할 독립적인 카드 인스턴스를 생성하는 것을 목표로 진행했다.

원본 `CardData`와 `DeckData`는 전투 중 변경하지 않고 유지하며, 덱에 등록된 카드 한 장마다 별도의 `CardInstance`를 생성하도록 구성했다.

7일차 완료 기준은 다음과 같다.

- 카드별 고유 인스턴스 생성
- 카드 원본 데이터와 런타임 카드 분리
- 카드 소유자와 아군 전투 유닛 연결
- 원본 덱을 전투용 런타임 덱으로 변환
- 원본 덱과 런타임 덱의 카드 수 검증
- 전투 초기화 과정에 런타임 덱 생성 연결
- 카드 인스턴스 정보 출력 기능 추가

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Project Type | 2D |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| Battle Loadout | `Assets/_ProjectC/ScriptableObjects/BattleLoadouts/BattleLoadout_Test.asset` |

---
## 개발 구조

7일차 카드 생성 흐름은 다음과 같다.

1. `BattleLoadoutData`에서 출전 `DeckData` 조회
2. `DeckData`의 `DeckCardEntry` 목록 순회
3. 각 카드의 소유 `CharacterData` 확인
4. 소유 캐릭터와 일치하는 아군 `BattleUnitRuntime` 검색
5. 원본 카드와 소유 전투 유닛을 이용해 `CardInstance` 생성
6. 생성된 카드를 `BattleDeckRuntime`에 등록

동일한 `CardData`가 여러 번 등록되어 있어도 각 덱 항목은 서로 다른 카드 인스턴스로 생성된다.

---
## 1. CardInstance 구현

전투에서 카드 한 장을 독립적으로 구분하기 위해 `CardInstance`를 구현했다.

포함 정보:

- 카드 인스턴스 고유 ID
- 원본 `CardData`
- 카드 소유 `BattleUnitRuntime`
- 카드 표시 이름
- 카드 일러스트
- 카드 종류
- 카드 대상 종류
- AP 비용

인스턴스 ID에는 카드 ID, 덱 내부 순번, GUID를 함께 사용한다. 같은 카드 원본을 사용하는 카드가 여러 장 존재해도 서로 다른 인스턴스 ID를 갖는다.

카드 생성 시 다음 항목을 검사한다.

- 카드 원본 누락
- 카드 소유 전투 유닛 누락
- 적 유닛 또는 캐릭터 원본이 없는 유닛을 카드 소유자로 지정한 경우
- 음수 카드 순번

---
## 2. BattleDeckRuntime 구현

전투에 출전한 공용 덱을 런타임 카드 목록으로 변환하기 위해 `BattleDeckRuntime`을 구현했다.

포함 정보:

- 원본 `DeckData`
- 생성된 `CardInstance` 목록
- 현재 런타임 카드 수

외부 시스템에는 카드 목록을 `IReadOnlyList`로 제공하여, 덱 생성 이후 다른 코드가 목록을 직접 변경하지 않도록 구성했다.

---
## 3. 카드 소유자 연결

`DeckCardEntry`의 소유자는 원본 `CharacterData`를 참조하지만 전투에서는 실제 아군 `BattleUnitRuntime`을 사용해야 한다.

이를 위해 출전한 아군 목록으로 소유자 검색표를 만든 뒤, 각 카드의 `CharacterData`와 일치하는 전투 유닛을 연결했다.

소유자 검색표 생성 시 다음 항목을 검사한다.

- 아군 목록의 빈 전투 유닛
- 아군 진영이 아닌 전투 유닛
- `CharacterData`가 연결되지 않은 전투 유닛
- 동일 캐릭터의 전투 유닛 중복 생성
- 카드 소유자에 해당하는 전투 유닛 누락

---
## 4. 원본 데이터 보호

`CardInstance`와 `BattleDeckRuntime`은 원본 ScriptableObject를 읽기 전용 데이터로 사용한다.

7일차 과정에서는 다음 원본 데이터를 수정하지 않는다.

- `CardData`
- `DeckData`
- `DeckCardEntry`
- `CharacterData`
- `BattleLoadoutData`

전투 중 추가될 카드 상태는 이후 `CardInstance`에 저장하여 원본 데이터와 분리할 수 있다.

---
## 5. BattleSceneSetup 연결

기존 `BattleSceneSetup`의 초기화 과정에 런타임 덱 생성을 추가했다.

변경된 초기화 순서:

1. `BattleLoadoutData`와 전투 씬 설정 검사
2. 아군 `BattleUnitRuntime` 및 화면 오브젝트 생성
3. 적 `BattleUnitRuntime` 및 화면 오브젝트 생성
4. 출전 덱과 아군 목록으로 `BattleDeckRuntime` 생성
5. 전투 초기화 완료 상태 저장
6. 아군 수, 적 수, 카드 수 출력

외부 전투 시스템에서 생성된 런타임 덱을 조회할 수 있도록 `BattleDeck` 읽기 속성도 추가했다.

---
## 6. 카드 인스턴스 출력 기능

생성 결과를 확인하기 위해 `BattleSceneSetup`에 Context Menu 테스트 기능을 추가했다.

메뉴 이름:

- `테스트/카드 인스턴스 출력`

출력 정보:

- 카드 인스턴스 고유 ID
- 카드 표시 이름
- 카드 소유 전투 유닛 이름

Play Mode가 아니거나 런타임 덱이 생성되지 않은 상태에서는 경고를 출력하고 실행을 중단한다.

---
## 생성·수정된 파일

생성:

- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs.meta`
- `Assets/_ProjectC/Scripts/Battle/BattleDeckRuntime.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleDeckRuntime.cs.meta`

수정:

- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`

삭제된 파일과 Canvas 변경 사항은 없다.

---
## 검증 결과

- Unity 참조 기반 독립 C# 컴파일 오류 0건
- 컴파일 경고 0건
- 빌드 산출물에서 `CardInstance` 타입 확인
- 빌드 산출물에서 `BattleDeckRuntime` 타입 확인
- 신규 스크립트 메타 GUID 중복 0건
- 신규·수정 코드 한글 주석 누락 0건
- Git 공백 오류 0건
- 원본 카드·덱 ScriptableObject 변경 없음

Unity Play Mode에서의 카드 생성 로그와 Context Menu 출력은 자동 검증 환경에서 실행하지 못했으므로 실제 Editor에서 최종 확인이 필요하다.

---
## 7일차 결과

공용 `DeckData`에 등록된 카드를 전투용 `CardInstance`로 변환하고, 카드 소유 `CharacterData`를 실제 아군 `BattleUnitRuntime`과 연결하는 구조를 구축했다.

동일한 카드 원본이 여러 장 등록되어도 각 카드를 고유 인스턴스로 구분할 수 있으며, 생성된 카드 목록은 `BattleDeckRuntime`에서 관리한다.

이를 통해 다음 단계에서 원본 덱을 변경하지 않고 셔플, 드로우, 손패, 사용 카드, 버린 카드 상태를 구현할 수 있는 기반이 완성되었다.

---
## 다음 개발 방향

다음 단계에서는 `BattleDeckRuntime`을 기준으로 카드 더미를 섞고, 일정 수의 카드를 손패로 가져오며, 사용한 카드를 버린 카드 더미로 이동시키는 기본 드로우 구조를 구현한다.

---
## Commit

`7일차 : 전투용 카드 인스턴스 및 런타임 덱 생성 구조 구축`
