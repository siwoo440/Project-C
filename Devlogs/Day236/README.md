---

# 236일차 유물 확장 · 은빛 심장핀부터 구식 탄약띠까지

원본 유물 시트 50~57행의 유물 8종을 추가했다. 각 유물의 개별 아이콘, 상점 상품, 회차 저장 원본을 연결했다.

| 유물 | 발동 조건과 효과 |
| --- | --- |
| 은빛 심장핀 | 전투에서 회복받는 아군의 현재 체력이 최대치의 30% 이하면 회복량이 2 증가한다. |
| 검은 심장핀 | 현재 체력이 최대치의 70% 이상인 아군이 적에게 직접 카드 공격을 명중시키면 피해가 2 증가한다. 아군 턴당 첫 유효 타격에 적용한다. |
| 회전 깃발 | 아군 턴에 아군이 적을 처치하면 손패 상한까지 한 장을 뽑는다. 카드로 처치하면 사용 카드가 손패에서 빠진 뒤 정산하며, 아군 턴당 한 번 발동한다. |
| 붉은 스프링 | 아군 턴에 처음 행동력을 소비한 뒤, 그 턴의 다음 유료 고정 비용 카드 한 장의 행동력 비용이 1 감소한다. 최소 비용은 0이다. |
| 유리 구슬 | 명중 후 확률로 부여하는 상태 효과의 첫 유효 판정을 전투당 한 번 성공으로 고정한다. 면역인 대상에게는 발동 기회를 쓰지 않는다. |
| 흑철 구슬 | 기본 피해 6·12의 두 값 중 12가 선택될 확률을 50%에서 75%로 높인다. |
| 얇은 금속 실 | 전투의 최대 손패를 한 장 늘린다. 시작 드로우 수는 기본 규칙을 따른다. |
| 구식 탄약띠 | 적 한 명에게 한 번만 공격하는 카드의 직접 명중 피해가 1 증가한다. |

흑철 구슬의 확률 증가폭은 원본 시트에 수치가 없어 25%p로 적용했다. 등록된 카드 중 기본 피해가 6·12인 카드가 없어, 같은 전투 경로에서 해당 값을 가진 검사 카드로 발동을 확인했다.

---

## 검증과 실제 화면

Unity 검사 124건이 통과했다. 실패와 건너뜀은 0건이다. 신규 유물 검사 12건, 상점 진열·구매 확인 검사 2건, 카드·유물 회귀 검사를 포함한다.

Windows x64 플레이어에서 여덟 유물의 획득·중복 골드 변환과 저장 후 원본·획득 순서 복원을 확인했다. 전투에서 첫 찌르기의 방어 전 피해는 10, 두 번째는 8이었다. 다음 카드의 표시 비용은 0이었고, 출혈 상세가 붙었다. 손패 상한은 21, 시작 드로우는 8장이었다.

| 보유 유물과 붉은 스프링 상세 | 다음 카드 비용 0과 출혈 |
| --- | --- |
| ![복원한 신규 유물 여덟 종](SupplyRelics.png) | ![붉은 스프링 비용 할인과 유리 구슬 출혈](SpringAndMarbleBattle.png) |

---

## 유물 아이콘 제작

기본 이미지 생성 도구로 기존 찢긴 우편봉투 아이콘의 채색과 크기감을 참고했다. 각 유물은 한 개의 투명 PNG로 저장했다.

공통 프롬프트

```text
One original item icon for a handcrafted dark fantasy inventory. Subject: {subject}. Detailed painted metal textures and bright edge highlights, authentic weathered RPG loot, warm rim light, legible at 48 px. Isolated centered object, square canvas, fill about 70% of frame. Truly transparent background and no backdrop, no border, no floor, no shadow outside the object, no mist or smoke, no text, no symbols outside the item. The reference only guides the dark fantasy painting style and scale.
```

| 저장 파일 | 개별 프롬프트의 주제 |
| --- | --- |
| [SilverHeartPin.png](../../Assets/_ProjectC/Art/RelicIcons/SilverHeartPin.png) | a vintage silver heart-shaped lapel pin, bright pewter bevel, one tiny red garnet set in the center |
| [BlackHeartPin.png](../../Assets/_ProjectC/Art/RelicIcons/BlackHeartPin.png) | a blackened iron heart-shaped lapel pin with luminous silver bevel, tiny warm violet inset, distinct black metal silhouette |
| [SpinningFlag.png](../../Assets/_ProjectC/Art/RelicIcons/SpinningFlag.png) | a single tattered scarlet and gold pennant coiled partway around its short brass swivel staff, visibly wind-swept silhouette |
| [RedSpring.png](../../Assets/_ProjectC/Art/RelicIcons/RedSpring.png) | one burnt-red painted metal coil spring with worn bright silver ends and realistic spiral ridges, diagonally upright |
| [GlassMarble.png](../../Assets/_ProjectC/Art/RelicIcons/GlassMarble.png) | one translucent pale cyan glass marble, bright blue and white swirling ribbon sealed inside, clear round silhouette |
| [BlackIronMarble.png](../../Assets/_ProjectC/Art/RelicIcons/BlackIronMarble.png) | one dark graphite and black iron ball, strong metallic highlights with a shallow red-brown etched seam across it, opaque |
| [ThinMetalThread.png](../../Assets/_ProjectC/Art/RelicIcons/ThinMetalThread.png) | one small spool of thin pale silver wire with one curved loose filament, plainly readable silhouette |
| [OldAmmoBelt.png](../../Assets/_ProjectC/Art/RelicIcons/OldAmmoBelt.png) | a compact loop of worn brown canvas bandolier with several old brass fantasy cartridges, buckle and rounded loop silhouette |
