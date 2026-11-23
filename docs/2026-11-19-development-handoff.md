---
# 개발 중단 및 재개 기록

- 중단 요청: 2026-11-19. 사용자가 다시 지시할 때까지 개발을 진행하지 않는다.
- 마지막 완료 일차: 191일차 `탐사 화면 우측 하단 정리`.
- 마지막 완료 커밋: https://github.com/siwoo440/Project-C/commit/99a16e5f09e9dc00623f9f4b4a05bc7e232f9abe
- 기획서의 15. 개발 방향에서 191일차는 완료로 연결했고, 192일차는 `중단 · 재개 대기`로 표시했다.
- 사용자 기존 변경 사항인 `Assets/_ProjectC/Art/BattleCharacters.spriteatlas`와 `Project-C.slnx`는 건드리거나 커밋하지 않았다.

---
## 192일차: 탐사 재화 아이콘 UI — 미완료

- 우측 상단에 골드·나사·철판·전선을 각 아이콘과 숫자로 표시한다. 네 재화 이름 글자는 표시하지 않는다.
- 상단의 `상점 열기`, 호감도, 이번 탐사 클리어 문구를 제거한다. 기존 층·캐릭터 진행 정보는 사용자가 삭제를 요청하지 않았으므로 유지 여부를 확인하며 배치한다.
- 실제 상점 방 진입과 거래 기능은 유지한다. 현재 `ExplorationSpecialRoomView.OpenShop`이 `OpenShopButton`을 찾아 호출하므로, 버튼을 제거하기 전에 `ShopPrototypeView`에 상점 방 전용 공개 진입을 추가하고 방 연결을 변경해야 한다.
- `CommonCardShopTests`, `SheetRewardCardFlowTests`, `ShopDiscountRelicTests`, `ShopRelicSaleTests`는 기존 상점 버튼을 직접 찾으므로 새 진입 방식에 맞춰 검증해야 한다.
- 재화 획득·소비에 따른 수치 갱신, 상점 방 진입과 거래, 자동 저장 및 실제 탐사 화면을 검증한다.
- 계획: `docs/superpowers/plans/2026-11-19-day192-resource-hud.md`.
- 생성한 네 칸 무광 패널 이미지: `docs/superpowers/plans/drafts/day192-resource-frame.png`. 아직 게임에 적용하지 않았다.
- 검사 초안: `docs/superpowers/plans/drafts/day192-resource-hud-tests.cs.txt` 및 대응 `.meta.txt`. 아직 구현되지 않은 `ExplorationResourceHud`를 참조하므로 Unity가 읽지 않는 문서 폴더에 보관했다. 기존 검사 실행은 이 미구현 참조로 실패했으며 192일차 성공 검사 결과는 없다.

---
## 이후 계획

- 193일차: 구현된 카드·유물의 보상, 상점, 저장·복원 흐름 통합 점검.
- 194일차: UI·조작과 반복 탐사 로딩·메모리 상태 점검.
- 195일차: GitHub README에 게임 소개와 주요 코드 설명을 작성하고 빌드 씬 및 주요 화면의 실제 스크린샷을 추가.
- 각 일차를 실제로 끝낸 뒤 검증하고 커밋·푸시하며, 기획서 표의 제목을 커밋 제목과 맞추고 `완료`에 커밋 링크를 넣는다.

---
## 미완료 콘텐츠에 관한 주의

- 유물 시트 298종과 카드 시트 251종은 각각 카탈로그 데이터로 보관되어 있다. 모든 개별 효과와 이미지가 게임에서 구현된 것은 아니다.
- 193일차 점검은 현재 구현된 항목을 대상으로 하며, 미구현 콘텐츠를 완료된 것으로 표시하지 않는다.
- 실제 구현 상태는 `Assets/_ProjectC/Resources/Relics/RelicSheetCatalog.json`, `Assets/_ProjectC/Resources/CardSheetCatalog.json`과 각 런타임 원본을 대조한다.
