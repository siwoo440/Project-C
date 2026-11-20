---

# 238일차 유물 확장 · 녹슨 성배대부터 푸른 잉크병까지

원본 유물 시트 60~67행의 유물 8종을 원화, 상점과 회차 저장에 등록한다.

| 유물 | 발동 조건과 효과 |
| --- | --- |
| 녹슨 성배대 | 아군 턴에 처음 사용한 성배 카드의 소유자가 정신력 1을 얻는다. |
| 손때 묻은 계산기 | 아군 턴에 처음 사용한 펜타클 카드의 정산이 끝나면 카드를 한 장 뽑는다. |
| 바람개비 배지 | 전투 시작부터 첫 적 턴 종료까지 생존 아군 전원의 행동 속도가 1 증가한다. 현재 아군 입력은 수동으로 진행되므로 선공과 행동 순서는 기존 규칙을 따른다. |
| 불완전한 방독면 | 침식지대에서 시작한 전투의 기간제 아군 디버프를 1턴 줄인다. 최소 1턴이다. |
| 조난 신호탄 | 정상 도주에 성공하면 기본 보상의 25%를 골드로 조우당 한 번 받는다. 다음에 같은 조우에서 승리하면 먼저 받은 골드가 정산된다. |
| 초소형 드론 | 전투의 첫 공격 예고 전에 무작위 생존 적 한 명의 공격력을 2 낮춘다. 첫 적 턴이 끝나면 해제된다. |
| 가죽 수첩 | 최초로 손패에 들어온 드로우 카드 한 장의 고정 비용이 그 턴 동안 1 감소한다. |
| 푸른 잉크병 | 전투에서 처음 장수를 지정해 뽑는 카드 효과의 드로우를 1장 늘린다. 시작 손패, 선택형 드로우, 다른 유물의 추가 드로우는 기본 규칙을 따른다. |

조난 신호탄의 비율과 초소형 드론의 약화 종류·수치는 원본 시트에 상세 수치가 없어 이번 일차에 정했다. 도주 골드 지급 내역은 회차 저장에 포함된다.

---

## 유물 아이콘 제작

기본 이미지 생성 도구로 기존 붉은 스프링 아이콘의 채색과 크기감을 참고했다.

```text
One original item icon for a handcrafted dark fantasy inventory. Subject: {subject}. Detailed painted metal textures and bright edge highlights, authentic weathered RPG loot, warm rim light, legible at 48 px. Isolated centered object, square canvas, fill about 70% of frame. Truly transparent background and no backdrop, no border, no floor, no shadow outside the object, no mist or smoke, no text, no symbols outside the item. The reference only guides the dark fantasy painting style and scale.
```

| 저장 파일 | 개별 프롬프트의 주제 |
| --- | --- |
| [RustedChaliceStand.png](../../Assets/_ProjectC/Art/RelicIcons/RustedChaliceStand.png) | one antique rusted iron chalice stand, three bent feet and one ring cradle, beautiful reddish porous rust |
| [WornCalculator.png](../../Assets/_ProjectC/Art/RelicIcons/WornCalculator.png) | one small worn vintage brass field calculator with a few blank mechanical rectangular keys and rivets, canvas case, no text or numbers |
| [PinwheelBadge.png](../../Assets/_ProjectC/Art/RelicIcons/PinwheelBadge.png) | one small weathered brass military pinwheel badge, four silver wind vanes on a metal brooch clasp, playful single silhouette |
| [IncompleteGasMask.png](../../Assets/_ProjectC/Art/RelicIcons/IncompleteGasMask.png) | one battered dark olive industrial fantasy respirator mask with two cloudy eye lenses and one clearly missing round filter on one side, no wearer |
| [DistressFlare.png](../../Assets/_ProjectC/Art/RelicIcons/DistressFlare.png) | one capped, inert antique crimson signal flare cylinder with brassy base and scuffed paint, no sparks or flame or smoke, no markings |
| [MicroDrone.png](../../Assets/_ProjectC/Art/RelicIcons/MicroDrone.png) | one original tiny brass reconnaissance drone with a single teal optical lens and four petite metallic wings, compact clockwork fantasy relic silhouette |
| [LeatherNotebook.png](../../Assets/_ProjectC/Art/RelicIcons/LeatherNotebook.png) | one pocket-size closed weathered brown leather notebook, stitched edges, small plain clasp, embossed texture without letters |
| [BlueInkBottle.png](../../Assets/_ProjectC/Art/RelicIcons/BlueInkBottle.png) | one old squat transparent glass ink bottle half full of saturated cobalt ink, small cork stopper with wax seal, bright glass highlights |

---

## 검증 및 실제 화면

- Unity EditMode 통합 검사 75건 통과. 실패 0건, 건너뜀 0건. 상점 검사 2건과 유물 원본·효과·도주 저장 복원 검사를 포함한다.
- 실제 Windows Direct3D 11 플레이어에서 유물 8종의 최초 획득, 중복 변환과 보유 유물의 저장 복원을 확인했다.
- 첫 전투에서 아군 속도 +1, 무작위 적 공격력 −2, 가드 전환 사용 후 방어도 5와 드로우 2장을 확인했다.
- [유물 보관함 실제 화면](FieldRelics.png) · [유물 효과 적용 전투 실제 화면](FieldRelicBattle.png)
