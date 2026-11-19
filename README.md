---

# 카오스폰즈 · ChaosPons

카오스폰즈는 타로의 메이저 아르카나를 모티브로 한 **PC 싱글플레이 2D 턴제 덱빌딩 로그라이크 RPG**다. 세룰리온 거점에서 최대 네 명을 편성해 윈터폴을 탐사한다. 누굴 데려가느냐에 따라 시작 덱이 달라지고, 탐사 중 얻은 카드를 누구에게 줄지에 따라 다음 전투의 선택지도 달라진다.

![윈터폴에서 네 명의 아르카나가 손패를 들고 재폭풍의 심핵과 대치하는 전투 화면](Devlogs/Day195/BossBattle.png)

---

## 한 번의 탐사

1. **거점에서 준비한다.** 캐릭터의 능력과 출전 상태를 확인하고 설비를 강화한다. **탐사 → 기본 탐사 → 편성하기**에서 1~4명을 배치한 뒤 출발한다. 캐릭터 22명 중 ‘세계’는 최종 탐사를 처음 클리어하면 열린다.
2. **길과 방을 고른다.** 층마다 새로 만들어지는 지도를 걸어 적·이벤트·보물·휴식·상점과 만난다. 휴식 방에서는 파티의 체력과 정신력을 회복하고 카드 한 장을 강화한다. 상점과 보물에서 공용 카드를 얻을 때는 소유할 파티원을 고른다.
3. **적의 다음 행동을 보고 카드를 낸다.** 파티가 행동력(AP)을 공유한다. 공격·방어·회복 카드와 캐릭터 전용 효과를 조합하고, 적이 노리는 아군과 남은 행동력을 확인한 뒤 턴을 끝낸다. 일반·정예전에서는 도주할 수 있다.
4. **10층 보스를 쓰러뜨리고 귀환한다.** 일반 층의 정예와 5층 중간 보스는 다음 층으로 가는 관문이다. 10층 보스를 이기면 탐사 결과와 보상을 확인한다. 전멸하면 이번 탐사에서 얻어 남아 있던 골드·나사·철판·전선을 잃고 거점으로 돌아간다.

---

## 실행과 조작

| 목적 | 방법 |
| --- | --- |
| 에디터 실행 | Unity Hub에서 저장소 루트를 프로젝트로 추가하고 **Unity 6000.3.21f1**로 연다. [00_Boot.unity](Assets/_ProjectC/Scenes/00_Boot.unity)를 열어 ▶ **Play**를 누른다. |
| Windows 빌드 | 같은 버전의 Windows 빌드 모듈을 설치한다. **File → Build Profiles**에서 **Windows**를 활성화하고 **Build And Run**을 선택한다. 공용 씬 목록은 부트부터 여섯 개가 등록돼 있다. [에디터 버전](ProjectSettings/ProjectVersion.txt) · [빌드 씬 목록](ProjectSettings/EditorBuildSettings.asset) |
| 탐사 이동 | **WASD** 또는 **방향키**로 움직인다. 적이나 이벤트 표식에 닿으면 전투 또는 선택 화면이 열린다. |
| 카드 사용 | 손패 위에 마우스를 올려 카드를 확대한다. 대상이 필요한 카드는 강조된 적이나 아군 위로 끌어다 놓는다. |
| 전투 정보 | 카드를 선택하지 않은 상태에서 아군이나 적을 클릭하면 상세 정보를 볼 수 있다. **턴 종료**를 누르면 적이 행동한다. |
| 저장과 재개 | 거점의 **저장 · 불러오기**에서 슬롯을 관리한다. 탐사 중에는 전투 진입이나 보상 처리 때 자동 저장된다. 게임을 다시 시작하면 메인 메뉴의 **이어하기**로 저장 슬롯을 연다. |

---

## 여섯 개 씬

| 게임 시작 | 메인 메뉴 |
| --- | --- |
| ![시작 씬에서 메인 메뉴로 전환할 때의 로딩 화면](Devlogs/Day195/BootLoading.png) | ![새로하기·이어하기·설정·나가기가 있는 메인 메뉴](Devlogs/Day195/MainMenu.png) |
| [00_Boot](Assets/_ProjectC/Scenes/00_Boot.unity) · 게임 시스템을 준비하고 메인 메뉴로 이동한다. | [10_MainMenu](Assets/_ProjectC/Scenes/10_MainMenu.unity) · 새 게임과 저장한 게임을 시작한다. |
| **거점** | **탐사** |
| ![세룰리온 거점에서 캐릭터·탐사·설비를 선택하는 화면](Devlogs/Day195/Lobby.png) | ![검은 외곽선으로 바닥과 벽의 각 칸을 구분한 윈터폴 탐사 지도](Devlogs/Day196/DungeonGrid.png) |
| [20_Lobby](Assets/_ProjectC/Scenes/20_Lobby.unity) · 출전을 준비하고 자원과 캐릭터를 관리한다. | [30_Exploration](Assets/_ProjectC/Scenes/30_Exploration.unity) · 지도를 걸으며 방을 찾고 파티와 자원을 관리한다. |
| **전투** | **설정** |
| ![일반 몬스터가 노리는 아군, 파티의 행동력과 손패](Devlogs/Day195/NormalBattle.png) | ![설정의 그래픽 탭에서 해상도와 프레임 제한을 확인하는 화면](Devlogs/Day195/Settings.png) |
| [40_Battle](Assets/_ProjectC/Scenes/40_Battle.unity) · 적의 의도와 손패를 보고 행동을 정한다. | [90_Settings](Assets/_ProjectC/Scenes/90_Settings.unity) · 그래픽·사운드·게임 플레이·조작·언어 탭을 제공한다. 일부 항목은 표시 전용이다. |

---

## 상황별 화면

195일차 탐사·전투·설정 사진은 Windows 독립 실행 빌드에서 촬영했다. 부트·메인 메뉴·거점 사진은 에디터에서 실제 씬을 실행해 촬영했다. 이전 개발일지 사진에는 촬영 일차를 표시했다.

<details>
<summary><strong>게임 시작 · 새로하기 확인과 설정 탭</strong></summary>

| 새로하기 확인 | 사운드 설정 |
| --- | --- |
| ![초기화되는 게임 기록을 알리고 새로 시작 또는 취소를 선택하는 화면](Devlogs/Day195/NewGameWarning.png) | ![마스터 볼륨과 개별 음량 목록이 보이는 사운드 탭](Devlogs/Day195/SettingsSound.png) |
| **게임 플레이 설정** | **조작 설정** |
| ![난이도, 튜토리얼, 전투 속도 항목이 있는 게임 플레이 탭](Devlogs/Day195/SettingsGameplay.png) | ![키 설정과 입력 모드 항목을 표시하는 조작 탭](Devlogs/Day195/SettingsControls.png) |

언어 탭에는 현재 언어와 자막 관련 항목이 표시된다. [언어 설정 화면](Devlogs/Day195/SettingsLanguage.png)

</details>

<details>
<summary><strong>거점 · 안내, 출전 준비, 설비와 저장</strong></summary>

| 거점 튜토리얼 · 155일차 | 탐사 지역 선택 · 117일차 |
| --- | --- |
| ![거점 튜토리얼의 예시 화면과 건너뛰기 버튼](Devlogs/Day155/TutorialLobby.png) | ![기본 탐사 지역을 선택한 뒤 편성하기로 이동하는 화면](Devlogs/Day117/Map.png) |
| **캐릭터 목록 · 120일차** | **파티 편성 · 155일차** |
| ![아르카나 캐릭터 초상화 목록](Devlogs/Day120/CharacterList.png) | ![편성된 캐릭터와 해당 슬롯의 상세 정보](Devlogs/Day155/FormationSelection.png) |
| **도감 분류 · 121일차** | **설비 강화 · 133일차** |
| ![도감의 이벤트·포션·적·유물 등 분류 버튼](Devlogs/Day121/Categories.png) | ![강화할 설비의 단계와 재료 비용](Devlogs/Day133/FacilityPanel.png) |
| **출전 불가 시 긴급 대기 · 156일차** | **자동 저장 슬롯 · 125일차** |
| ![모든 출전 가능 캐릭터가 회복 중일 때 거점에 나타나는 긴급 대기 버튼](Devlogs/Day156/EmergencyWait.png) | ![전투 진입 전 탐사 자동 저장을 기록한 슬롯](Devlogs/Day125/AutoSave.png) |

설비 사진에는 강화 비용을 확인할 수 있도록 자원을 지급했고, 긴급 대기 사진은 보유 캐릭터를 모두 회복 대기로 설정해 촬영했다. 도감과 저장 메뉴의 세부 화면도 볼 수 있다. [적 도감 목록](Devlogs/Day121/Entries.png) · [복구 가능한 슬롯](Devlogs/Day126/RecoverySlot.png) · [복구 확인창](Devlogs/Day126/RecoveryWarning.png)

</details>

<details>
<summary><strong>탐사 · 방의 선택, 보상과 유물</strong></summary>

| 버려진 상자의 선택지 | 상자를 연 결과 |
| --- | --- |
| ![상자를 열어 자원을 얻거나 무시하고 지나갈 수 있는 이벤트 선택 화면](Devlogs/Day195/Event.png) | ![상자를 열어 받은 골드와 부품이 표시된 이벤트 결과](Devlogs/Day195/EventOutcome.png) |
| **상점 방** | **휴식 방** |
| ![현재 골드와 판매 상품, 가격을 보여주는 상점](Devlogs/Day195/Shop.png) | ![휴식 위험도와 파티 상태, 강화할 카드를 고르는 휴식 화면](Devlogs/Day195/Rest.png) |
| **보물에서 카드 소유자 선택 · 166일차** | **보유 유물 확인 · 189일차** |
| ![보물로 나온 공용 카드를 받을 파티원을 고르는 창](Devlogs/Day166/TreasureOwnerSelection.png) | ![탐사 중 보유 유물 목록과 선택한 유물 정보](Devlogs/Day189/RelicWindow.png) |
| **상점에서 카드 소유자 선택 · 165일차** | **보유 유물 판매 · 162일차** |
| ![상점에서 공용 카드 구매 전 소유자를 고르는 화면](Devlogs/Day165/CardOwnerSelection.png) | ![상점의 유물 판매 목록](Devlogs/Day162/RelicSalesShop.png) |
| **탐사 이동 튜토리얼 · 200일차** | **보물 방 튜토리얼 · 200일차** |
| ![실제 던전 지도로 이동을 안내하는 첫 페이지](Devlogs/Day200/GuideMap.png) | ![보물 방에서 카드를 받을 파티원을 고르는 방법](Devlogs/Day200/GuideTreasure.png) |

이벤트·휴식·상점 안내도 게임에서 읽을 수 있다. [탐사 튜토리얼 다섯 화면](Devlogs/Day200/README.md) · [회복 유물 세 종류](Devlogs/Day198/HealingRelics.png) · [성배 카드 다섯 장](Devlogs/Day197/CupCards.png)

윈터폴의 ‘재폭풍의 눈’에서는 행동할 생존자와 획득한 희귀 카드를 받을 파티원을 각각 고른다. [세 가지 선택과 결과 화면](Devlogs/Day201/README.md) · [체력·정신력과 보상 처리 코드](Assets/_ProjectC/Scripts/Exploration/AshstormEyeEvent.cs)

| 잿더미 조사 방법 선택 | 도구로 유물을 찾은 결과 |
| --- | --- |
| ![맨손·철판·덮어두기의 비용과 확률이 표시된 확대 이벤트 화면](Devlogs/Day206/ExpandedChoices.png) | ![철판 한 개를 소비하고 유물을 얻은 확대 결과 화면](Devlogs/Day206/ExpandedOutcome.png) |

맨손 조사와 덮기는 생존 파티원 한 명을 직접 고른다. [행동자 선택 화면](Devlogs/Day204/AshPileActor.png) · [유물 추첨과 비용 처리 코드](Assets/_ProjectC/Scripts/Exploration/AshPileEvent.cs)

재 속의 지도 조각은 다음 일반 전투에서 한 번 회피 기회를 준다. 태우면 선택한 동료의 정신력을 회복하고 나사를 얻는다. [세 가지 선택과 실제 조우 화면](Devlogs/Day208/README.md)

타버린 허수아비를 조사하면 행동자가 희귀 공용 카드 또는 사용할 수 없는 잿빛 저주를 받는다. [행동자 선택 결과와 전투 손패](Devlogs/Day210/README.md)

유리화된 들판을 조심히 통과하면 다음 전투의 생존 아군이 두 턴 동안 더 잘 피한다. 눈을 감고 달리면 첫 턴 명중률이 낮아진다. [들판 화면과 전투 상태 아이콘](Devlogs/Day211/README.md)

불탄 수레의 상자를 열면 물자를 얻거나 매복을 만난다. 표식을 남기면 다음 상점의 첫 상품을 10% 할인받는다. [수레의 세 선택과 매복 전투](Devlogs/Day212/README.md)

| 불탄 수레에서 행동 선택 | 같은 방에서 수레 해체 결과 확인 |
| --- | --- |
| ![세 가지 선택지와 수레 삽화](Devlogs/Day217/Cart16x9Choices.png) | ![확인 버튼과 같은 크기로 유지된 삽화](Devlogs/Day217/Cart16x9Outcome.png) |

선택지가 줄어들어도 삽화의 위치와 크기가 일정하다. [4대3·16대9 화면 비교](Devlogs/Day217/README.md)

이벤트의 삽화·설명·선택지는 얇은 황동색 직선으로 둘러싼 직사각형 패널에 표시한다. 선택지가 네 개 이상이면 목록 오른쪽의 손잡이나 마우스 휠로 아래쪽 행동을 볼 수 있다. [현재 이벤트 화면과 스크롤 예시](Devlogs/Day219/README.md)

검게 탄 우물에서는 행동자를 골라 물약을 얻거나 생존 피격 후 회복을 예약할 수 있다. 우물을 봉인하면 다음 층 첫 재폭풍의 눈 추첨에 영향을 준다. [선택과 결과 화면](Devlogs/Day213/README.md)

잿빛 방랑자의 목소리를 따라간 뒤 승리하면 공용 카드 세 장 중 하나를 골라 생존 파티원에게 준다. [탐사 원문·승리 카드·소유자 화면](Devlogs/Day214/README.md)

162·165일차 사진에는 당시의 상점 열기 버튼이 보인다. 현재는 지도에서 상점 방에 들어가 상품을 확인한다. [네 가지 자원의 수량 표시 · 192일차](Devlogs/Day192/ExplorationResources.png) · [네 명을 편성한 탐사 화면 · 195일차](Devlogs/Day195/Exploration.png)

</details>

<details>
<summary><strong>전투 · 카드 선택, 파티 상태와 결과</strong></summary>

첫 손패는 왼쪽 뽑기 더미에서 한 장씩 나와 부채꼴로 정렬된다. 상단의 책·표적·계급장·수정 아이콘으로 전투 도움말, 공격 화살표, 현재 강화, 유물 창을 조작한다. [카드 배분과 새 아이콘 화면](Devlogs/Day220/README.md)

생존 아군에게는 체력이 가득 찼을 때도 회복 카드와 물약을 사용할 수 있다. 체력은 최대치까지만 회복하고 비용과 물약은 정상 소비한다. [최대 체력에서의 사용 전후](Devlogs/Day221/README.md)

전장의 외침부터 빠른 계산까지 공용 카드 여덟 장을 보물에서 얻어 파티원의 덱에 넣을 수 있다. [전투 손패 화면](Devlogs/Day207/CommonCards.png) · [카드별 효과와 원화](Devlogs/Day207/README.md)

전진 베기·신성의 일격·은총의 잔·집중 호흡은 카드 한 장으로 두 가지 효과를 낸다. 216일차 기준으로 시제품 네 명의 전용 카드와 시트 공용 카드를 합쳐 30종을 플레이할 수 있었다. [네 카드의 효과와 전투 화면](Devlogs/Day216/README.md)

222일차에 방어 태세·무자비한 연격·방어선 구축·철벽을 보물 보상에 추가했다. 시트 공용 카드 30종과 시제품 네 명의 전용 카드 4종을 플레이할 수 있다. [신규 카드와 222~290일차 개발 일정](Devlogs/Day222/README.md)

| 카드 확대 · 176일차 | 각성·붕괴 상태 · 173일차 |
| --- | --- |
| ![손패에서 가리킨 카드를 확대하고 효과를 확인하는 화면](Devlogs/Day176/CardPreview.png) | ![아르카나의 정신 상태를 표시한 전투 화면](Devlogs/Day173/MentalStatusBattle.png) |
| **아군 정보 · 190일차** | **적 정보 · 190일차** |
| ![선택한 아군 아르카나의 정보 창](Devlogs/Day190/AllyInformation.png) | ![선택한 적의 정보 창](Devlogs/Day190/EnemyInformation.png) |
| **일반 전투 도주 · 195일차** | **전투 패배 · 195일차** |
| ![아군 네 명이 도주해 전투 보상 없이 돌아가는 결과](Devlogs/Day195/BattleEscape.png) | ![모든 아군이 쓰러져 전투 패배로 기록된 결과](Devlogs/Day195/BattleDefeat.png) |
| **10층 보스 전투 승리 · 195일차** | **탐사 성공 정산 · 195일차** |
| ![보스를 처치한 뒤의 전투 승리와 생존자 수](Devlogs/Day195/BattleVictory.png) | ![10층 클리어 정산과 거점 복귀 버튼이 있는 탐사 성공 화면](Devlogs/Day195/ExplorationSuccess.png) |

전투 결과 사진은 검증용 파티로 조우한 뒤 도주를 실행하거나 피해를 적용해 게임의 승패 판정과 정산을 거쳐 촬영했다. 전멸 시에는 탐사 실패 안내를 확인하고 거점으로 복귀한다.

![전멸한 파티의 탐사 실패 안내와 거점으로 복귀 버튼](Devlogs/Day195/ExplorationFailure.png)

</details>

---

## 주요 코드

| 동작 | 데이터 흐름 | 시작 파일 |
| --- | --- | --- |
| 게임 시작과 화면 전환 | 게임 관리자 초기화가 끝나면 부트가 메인 메뉴의 씬 이름을 전달하고, 전환 관리자가 로딩 진행률을 표시한다. | [부트 초기화](Assets/_ProjectC/Scripts/Core/BootLoader.cs) · [로딩과 비동기 씬 전환](Assets/_ProjectC/Scripts/Core/SceneFlowManager.cs) |
| 캐릭터 선택과 출전 덱 | 출발할 때 선택한 멤버의 출전 가능 여부를 확인하고, 캐릭터 ID로 찾은 전용 카드와 역할별 기본 카드로 시작 덱을 구성한다. | [편성 검증](Assets/_ProjectC/Scripts/Characters/PartySelection.cs) · [파티와 시작 덱 준비](Assets/_ProjectC/Scripts/Characters/PartyLoadoutRuntime.cs) · [캐릭터 명단](Assets/_ProjectC/Resources/Characters/ArcanaRoster.asset) |
| 던전 생성과 탐사 정산 | 적과 만나면 조우 ID와 전투 후 복귀 좌표를 보관하고, 승리 결과를 받으면 해당 조우를 클리어 목록에 넣고 보상을 지급한다. | [지도 생성](Assets/_ProjectC/Scripts/Exploration/ExplorationMapGenerator.cs) · [방 역할 배치](Assets/_ProjectC/Scripts/Exploration/ExplorationRoomRoleRuntime.cs) · [전투 복귀·층 진행·보상](Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs) |
| 턴과 카드 사용 | 플레이어 턴 종료 시 남은 일반 손패를 버림 더미로 옮기고, 다음 플레이어 턴에 공용 행동력을 회복한 뒤 새 손패를 뽑는다. | [플레이어·적 턴 진행](Assets/_ProjectC/Scripts/Battle/BattleTurnRuntime.cs) · [뽑기·손패·버리기](Assets/_ProjectC/Scripts/Battle/BattleDeckRuntime.cs) · [카드 선택과 대상 처리](Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs) |
| 보물·상점·휴식 | 보물에서 공용 카드가 나오면 소유자를 고를 때까지 지급을 보류하고, 선택한 파티원에게 카드를 준 뒤 방의 사용 여부를 저장한다. | [보물 보상](Assets/_ProjectC/Scripts/Exploration/ExplorationTreasureRewardService.cs) · [방 상호작용](Assets/_ProjectC/Scripts/Exploration/ExplorationSpecialRoomView.cs) · [상점 구매·판매](Assets/_ProjectC/Scripts/Shop/ShopRunManager.cs) · [휴식 회복](Assets/_ProjectC/Scripts/Rest/RestRoomRecoveryService.cs) |
| 거점 성장과 저장 | 탐사가 끝나면 캐릭터 회복 상태를 반영하고, 거점 저장에 성공한 뒤에만 이전 탐사 자동 저장 파일을 정리한다. | [설비 강화](Assets/_ProjectC/Scripts/Progression/FacilityUpgradeManager.cs) · [자동 저장](Assets/_ProjectC/Scripts/Save/ExplorationAutoSave.cs) · [슬롯 저장·복원](Assets/_ProjectC/Scripts/Save/SaveSlotService.cs) |

개발 과정과 검증 기록은 [일차별 개발일지](Devlogs/)에서 볼 수 있다. [탐사 자원 UI](Devlogs/Day192/README.md) · [카드·유물 획득과 저장 복원](Devlogs/Day193/README.md) · [거점과 탐사 왕복 검사](Devlogs/Day194/README.md)

금이 간 나침반으로 발견한 사건과 네 유물의 실제 화면은 [223일차 유물 개발일지](Devlogs/Day223/README.md)에 정리했다.

검의 춤은 이웃한 두 칸 안에서 타격마다 대상을 다시 뽑는다. 같은 적에게 세 번 맞힌 전투 화면과 검 카드 8종의 효과는 [224일차 개발일지](Devlogs/Day224/README.md)에서 볼 수 있다.

상점에서는 상품을 눌러 효과와 결제 후 잔액을 확인하고 구매하거나 취소한다. 구매를 마친 상품은 목록 맨 아래로 이동한다. [상점 구매와 전투 튜토리얼 화면](Devlogs/Day225/README.md)

파편화된 거울이 복사한 카드는 해당 전투에서만 사용한다. 검은 실타래는 효과나 턴 종료로 손패를 처음 버릴 때 한 장을 지킨다. [유물 8종의 효과와 실제 전투 화면](Devlogs/Day226/README.md)

찌르기로 출혈을 걸거나 무릎 베기로 적의 다음 행동을 취소할 수 있다. 취소된 적의 공격 예고선은 전장에서 사라진다. [검 카드 8종과 상태 표시 화면](Devlogs/Day227/README.md)

실험용 포션보관대를 소유하면 전용 칸이 한 칸 늘어난다. 기본 칸에 자리가 있으면 보관대를 팔 때 그 칸으로 포션을 옮긴다. [포션·전투 유물 8종과 탐사 화면](Devlogs/Day228/README.md)

칼날 정비의 피해 증가량은 이어서 쓰는 검 카드의 타격마다 적용한다. 검격 반사는 적의 직접 물리 피해에 반응하며, 파고들기는 해당 타격에서 임시 방어도의 일부를 건너뛴다. [검 카드 8종과 실제 전투 화면](Devlogs/Day229/README.md)

[윈터폴 시제품 점검표](Devlogs/Day205/README.md)에서 실제로 획득할 수 있는 카드와 유물 수, 구현한 원문 이벤트, 아직 측정할 플레이 시간과 난이도를 확인할 수 있다.
