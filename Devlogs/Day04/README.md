# 4일차 개발일지 - ScriptableObject 기본 데이터 구조 구축

---

## 개발 목표

캐릭터·카드·적 데이터를 코드에 직접 작성하지 않고 Unity Asset으로 생성하고 관리할 수 있도록 ScriptableObject 기반의 기본 데이터 구조를 구축하는 것을 목표로 진행했다.

4일차 완료 기준은 다음과 같다.

```text
CharacterData
CardData
EnemyData
↓
Unity Create 메뉴에서 Asset 생성 가능
↓
Inspector에서 데이터 편집 가능
```

---

## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Project Type | 2D |
| Data Script 위치 | `Assets/_ProjectC/Scripts/Data` |
| ScriptableObject 위치 | `Assets/_ProjectC/ScriptableObjects` |

---

## 개발 내용

### 1. CharacterData 구현

캐릭터의 변하지 않는 원본 정보를 관리하기 위한 `CharacterData` ScriptableObject를 구현했다.

포함 데이터:

- 캐릭터 고유 ID
- 표시 이름
- 설명
- 역할군
- 초상화
- 최대 체력
- 초기 정신력

Inspector에서 값을 직접 편집할 수 있도록 `SerializeField`를 사용하고, 외부 시스템에서는 읽기 전용 Property를 통해 접근하도록 구성했다.

초기 정신력은 프로젝트 기본 기준에 맞춰 `50`을 기본값으로 설정했다.

---

### 2. CharacterRole 구현

캐릭터 역할을 문자열이 아닌 Enum으로 관리하도록 `CharacterRole`을 구현했다.

```text
Attack
Defense
Support
Strategy
```

이를 통해 Inspector에서 역할을 드롭다운으로 선택할 수 있고 문자열 오타를 방지할 수 있게 되었다.

---

### 3. CardData 구현

카드의 원본 정보를 관리하기 위한 `CardData` ScriptableObject를 구현했다.

포함 데이터:

- 카드 고유 ID
- 표시 이름
- 설명
- 카드 일러스트
- 카드 종류
- 대상 종류
- AP 비용

4일차에서는 카드의 공격력, 회복량, 상태효과 실행 등의 실제 효과 시스템은 구현하지 않고 기본 카드 데이터 구조만 구축했다.

---

### 4. CardType 구현

프로젝트 C의 카드 분류를 Enum으로 구현했다.

```text
Sword
Wand
Cup
Pentacle
Shield
```

카드마다 Inspector에서 종류를 선택할 수 있도록 구성했다.

---

### 5. CardTargetType 구현

카드 대상 분류를 Enum으로 구현했다.

```text
Self
SingleAlly
AllAllies
SingleEnemy
AllEnemies
```

현재는 대상 종류만 데이터로 저장하며 실제 대상 선택 및 적용 기능은 이후 전투 시스템에서 구현한다.

---

### 6. EnemyData 구현

적의 기본 원본 정보를 관리하기 위한 `EnemyData` ScriptableObject를 구현했다.

포함 데이터:

- 적 고유 ID
- 표시 이름
- 설명
- 적 이미지
- 최대 체력

적 행동 패턴, 행동 예고, 속도 기반 행동 순서 등은 이후 적 AI 개발 단계에서 확장하기 위해 현재 구조에는 포함하지 않았다.

---

## 생성된 데이터 스크립트

```text
Assets/_ProjectC/Scripts/Data
├─ CharacterData.cs
├─ CharacterRole.cs
├─ CardData.cs
├─ CardType.cs
├─ CardTargetType.cs
└─ EnemyData.cs
```

총 6개의 기본 데이터 스크립트를 추가했다.

---

## ScriptableObject 폴더 구조

```text
Assets/_ProjectC/ScriptableObjects
├─ Characters
├─ Cards
└─ Enemies
```

각 폴더는 해당 데이터 종류의 실제 ScriptableObject Asset을 저장하는 용도로 사용한다.

---

## 테스트 Character Asset 생성

CharacterData의 생성 및 Inspector 편집 기능을 확인하기 위해 테스트 Asset을 생성했다.

```text
Character_Test.asset
```

입력 데이터:

| 항목 | 값 |
|---|---|
| Character ID | `CHR_TEST` |
| Display Name | `Test Character` |
| Role | `Attack` |
| Max Health | `100` |
| Initial Mental | `50` |

초상화는 현재 테스트 단계이므로 비워두었다.

---

## 테스트 Card Asset 생성

CardData 구조를 확인하기 위해 다음 테스트 Asset을 생성했다.

```text
Card_TestAttack.asset
```

입력 데이터:

| 항목 | 값 |
|---|---|
| Card ID | `CRD_TEST_ATTACK` |
| Display Name | `Test Attack` |
| Card Type | `Sword` |
| Target Type | `SingleEnemy` |
| AP Cost | `1` |

카드 일러스트는 현재 테스트 단계이므로 비워두었다.

---

## 테스트 Enemy Asset 생성

EnemyData 구조를 확인하기 위해 다음 테스트 Asset을 생성했다.

```text
Enemy_Test.asset
```

입력 데이터:

| 항목 | 값 |
|---|---|
| Enemy ID | `ENM_TEST` |
| Display Name | `Test Enemy` |
| Max Health | `50` |

적 이미지는 현재 테스트 단계이므로 비워두었다.

---

## ID 규칙 기반 마련

데이터를 이름이 아닌 고유 ID로 구분할 수 있도록 기본 ID 체계를 적용했다.

```text
캐릭터 : CHR_...
카드   : CRD_...
적     : ENM_...
```

테스트 데이터:

```text
CHR_TEST
CRD_TEST_ATTACK
ENM_TEST
```

향후 저장, 도감, 데이터 검색 및 콘텐츠 확장 시 화면 표시 이름과 내부 데이터 식별자를 분리할 수 있는 기반을 마련했다.

---

## 원본 데이터와 런타임 데이터 분리 원칙

ScriptableObject는 캐릭터·카드·적의 원본 데이터를 저장하는 용도로만 사용한다.

예:

```text
CharacterData
└─ MaxHealth = 100
```

전투 중 변경되는 값은 이후 별도의 런타임 객체에서 관리한다.

```text
CharacterInstance
├─ CurrentHealth
└─ CurrentMental
```

따라서 ScriptableObject 원본의 체력이나 정신력 등을 전투 중 직접 수정하지 않는 방향으로 구조를 확정했다.

카드 역시 이후:

```text
CardData
↓
CardInstance
```

구조로 확장한다.

---

## 기존 시스템 영향 확인

4일차에서는 기존 Scene이나 공용 Manager를 수정하지 않았다.

수정되지 않은 주요 시스템:

```text
GameManager
SceneFlowManager
BootLoader
MainMenuController
LobbyController
SettingsController
```

따라서 3일차까지 구축한 기본 게임 진입 및 Scene 전환 구조와 독립적으로 데이터 기반을 추가했다.

---

## 완료 확인

- [x] `CharacterData` ScriptableObject 구현
- [x] `CharacterRole` Enum 구현
- [x] `CardData` ScriptableObject 구현
- [x] `CardType` Enum 구현
- [x] `CardTargetType` Enum 구현
- [x] `EnemyData` ScriptableObject 구현
- [x] Character Create 메뉴 생성
- [x] Card Create 메뉴 생성
- [x] Enemy Create 메뉴 생성
- [x] `Character_Test.asset` 생성
- [x] `Card_TestAttack.asset` 생성
- [x] `Enemy_Test.asset` 생성
- [x] Inspector 편집 가능한 데이터 구조 구축
- [x] 데이터 ID 규칙 기반 마련
- [x] 원본 데이터와 런타임 데이터 분리 방향 확정
- [x] 기존 Scene 및 Manager 시스템 변경 없음

---

## 4일차 결과

캐릭터·카드·적 데이터를 Unity ScriptableObject Asset으로 생성하고 Inspector에서 관리할 수 있는 기본 데이터 기반을 구축했다.

이를 통해 이후 파티 편성, 공용 덱, 전투 유닛, CardInstance, 적 AI 등의 시스템이 코드에 고정된 수치가 아닌 데이터 Asset을 참조하는 형태로 확장될 수 있게 되었다.

---

## Commit

```text
4일차 : 캐릭터·카드·적 ScriptableObject 기본 구조 구축
```

---

## 다음 개발 방향

다음 단계에서는 CharacterData와 CardData를 기반으로 파티 구성, 공용 덱, 카드 소유자 정보를 표현할 수 있는 기본 런타임 데이터 구조를 구축한다.
