# 5일차 개발일지 - 파티·공용 덱·카드 소유자 구조 구축

---

## 개발 목표

4일차에서 만든 CharacterData와 CardData를 기반으로 실제 전투에 전달할 파티, 공용 덱, 카드 소유자 관계를 구성할 수 있는 데이터 구조를 구축하는 것을 목표로 진행했다.

5일차 완료 기준은 다음과 같다.

```text
CharacterData
↓
PartyData

CardData + CharacterData Owner
↓
DeckCardEntry
↓
DeckData

PartyData + DeckData
↓
BattleLoadoutData
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

### 1. PartyData 구현

출전 캐릭터 구성을 관리하기 위한 `PartyData` ScriptableObject를 구현했다.

포함 데이터:

- 파티 고유 ID
- 파티 표시 이름
- 출전 캐릭터 목록

프로젝트 C의 최대 출전 인원에 맞춰 최대 파티 인원을 4명으로 제한했다.

추가 기능:

- 현재 파티 인원 조회
- 특정 CharacterData의 파티 포함 여부 확인
- 파티 유효성 검사
- 빈 캐릭터 검사
- 동일 캐릭터 중복 검사
- Inspector에서 4명을 초과하면 초과 인원 제거

---

### 2. DeckCardEntry 구현

공용 덱에 들어가는 카드 한 장과 해당 카드의 소유 캐릭터를 함께 표현하기 위해 `DeckCardEntry` 구조를 구현했다.

```text
DeckCardEntry
├─ CardData
└─ CharacterData Owner
```

이를 통해 같은 CardData를 사용하더라도 카드마다 소유 캐릭터를 별도로 연결할 수 있는 구조를 마련했다.

`DeckCardEntry`에는 Card와 Owner가 모두 존재하는지 확인하는 기본 유효성 검사 기능도 추가했다.

---

### 3. DeckData 구현

파티 전체가 전투에서 공유하는 공용 덱을 관리하기 위한 `DeckData` ScriptableObject를 구현했다.

포함 데이터:

- 덱 고유 ID
- 덱 표시 이름
- DeckCardEntry 목록

추가 기능:

- 현재 카드 수 조회
- 덱 유효성 검사
- 빈 덱 검사
- Card 또는 Owner가 누락된 Entry 검사
- 모든 카드 소유자가 실제 출전 파티에 포함되어 있는지 검사

공용 덱에서 같은 종류의 CardData를 여러 장 등록할 수 있도록 각 카드를 별도의 Entry로 관리한다.

---

### 4. BattleLoadoutData 구현

전투에서 사용할 파티와 공용 덱을 하나의 데이터로 묶기 위해 `BattleLoadoutData` ScriptableObject를 구현했다.

```text
BattleLoadoutData
├─ PartyData
└─ DeckData
```

전투 시스템에서는 이후 BattleLoadoutData 하나를 통해 출전 파티와 공용 덱을 함께 전달받을 수 있다.

추가된 유효성 검사:

- Party 존재 확인
- Deck 존재 확인
- PartyData 유효성 확인
- DeckData 유효성 확인
- Deck의 모든 카드 소유자가 Party에 포함되어 있는지 확인

---

## 생성된 데이터 스크립트

```text
Assets/_ProjectC/Scripts/Data
├─ PartyData.cs
├─ DeckCardEntry.cs
├─ DeckData.cs
└─ BattleLoadoutData.cs
```

4일차의 CharacterData, CardData, EnemyData는 수정하지 않고 그대로 사용했다.

---

## ScriptableObject 폴더 추가

5일차 데이터를 저장하기 위해 다음 폴더를 추가했다.

```text
Assets/_ProjectC/ScriptableObjects
├─ Parties
├─ Decks
└─ BattleLoadouts
```

기존 데이터 폴더와 함께 전체 구조는 다음과 같다.

```text
ScriptableObjects
├─ Characters
├─ Cards
├─ Enemies
├─ Parties
├─ Decks
└─ BattleLoadouts
```

---

## 테스트 Party 데이터 생성

파티 구조를 확인하기 위해 테스트 파티 Asset을 생성했다.

```text
Party ID: PTY_TEST
Display Name: Test Party
Members:
- Character_Test
```

이를 통해 CharacterData를 실제 출전 파티에 등록할 수 있는 것을 확인했다.

---

## 테스트 공용 덱 생성

공용 덱과 카드 소유자 구조를 확인하기 위해 테스트 덱을 생성했다.

```text
Deck ID: DECK_TEST
Display Name: Test Deck

Cards:
- Card: Card_TestAttack
  Owner: Character_Test
```

이를 통해 공용 덱 안에서 카드 원본과 카드 소유 캐릭터를 함께 연결할 수 있는 구조를 확인했다.

---

## 테스트 BattleLoadout 생성

파티와 덱을 전투 입력 데이터로 묶기 위해 테스트 BattleLoadout Asset을 생성했다.

```text
BattleLoadout_Test
├─ Party
│  └─ PTY_TEST
└─ Deck
   └─ DECK_TEST
```

최종적으로 다음 관계가 연결되었다.

```text
Character_Test
      │
      ├──────────────┐
      ▼              ▼
  PartyData      DeckCardEntry
                     ▲
                     │
              Card_TestAttack
                     │
                     ▼
                  DeckData

PartyData + DeckData
        │
        ▼
BattleLoadoutData
```

---

## 데이터 유효성 검사 기반 구축

잘못된 전투 편성을 이후 시스템에서 사전에 검사할 수 있도록 다음 기준을 코드에 포함했다.

### Party

- 1명 이상
- 최대 4명
- 빈 CharacterData 없음
- 동일 CharacterData 중복 없음

### Deck

- 카드 최소 1장
- 빈 DeckCardEntry 없음
- CardData 누락 없음
- Owner 누락 없음

### Battle Loadout

- Party와 Deck 존재
- 유효한 Party
- 유효한 Deck
- 모든 카드 Owner가 출전 Party에 포함

---

## 기존 시스템 영향

5일차에서는 기존 Scene 및 공용 Manager를 수정하지 않았다.

수정되지 않은 주요 시스템:

```text
GameManager
SceneFlowManager
BootLoader
MainMenuController
LobbyController
SettingsController
```

4일차의 원본 데이터 구조도 수정하지 않고 새로운 편성 데이터 구조를 독립적으로 추가했다.

---

## 완료 확인

- [x] `PartyData` 구현
- [x] 최대 파티 4명 제한
- [x] 파티 중복 캐릭터 검사
- [x] `DeckCardEntry` 구현
- [x] CardData와 Owner CharacterData 연결
- [x] `DeckData` 구현
- [x] 공용 덱 카드 목록 구축
- [x] 덱 유효성 검사
- [x] 카드 소유자의 Party 포함 여부 검사
- [x] `BattleLoadoutData` 구현
- [x] Party와 Deck 통합
- [x] BattleLoadout 유효성 검사
- [x] 테스트 Party 데이터 생성
- [x] 테스트 Deck 데이터 생성
- [x] 테스트 BattleLoadout 데이터 생성
- [x] 전투에 전달 가능한 파티·덱 데이터 관계 구축
- [x] 기존 Scene 및 Manager 시스템 변경 없음

---

## 5일차 결과

캐릭터 원본 데이터를 출전 파티로 구성하고, 카드 원본과 카드 소유자를 공용 덱에 연결한 뒤, 파티와 덱을 하나의 BattleLoadoutData로 묶을 수 있는 데이터 기반을 구축했다.

이를 통해 다음 단계부터 BattleLoadoutData를 기준으로 실제 아군 전투 유닛을 생성하고 EnemyData를 이용해 적 전투 유닛을 생성할 수 있는 준비가 완료되었다.

---

## Commit

```text
5일차 : 파티·공용 덱·카드 소유자 구조 구축
```

---

## 다음 개발 방향

다음 단계에서는 CharacterData와 EnemyData를 실제 전투 중 변경 가능한 전투 유닛으로 변환하고, 현재 HP·피해·사망 처리를 포함하는 기본 HP 시스템을 구축한다.
