---

# 240일차 유물 확장 · 붉은 잉크병부터 낡은 패치 노끈까지

원본 유물 시트 68~77행에서 예정된 여덟 유물의 원화와 상점 및 회차 저장을 연결한다.

| 유물 | 발동 조건과 효과 |
| --- | --- |
| 붉은 잉크병 | 전투당 첫 유효 카드 직접 명중의 피해가 2 증가한다. |
| 금박 책갈피 | 덱 위 확인 카드의 후보 수가 1 늘어난다. 지정된 드로우 장수는 그대로다. |
| 균열 봉인테이프 | 전투에서 처음 손패에 들어올 사용 불가 카드를 즉시 버린 더미로 보낸다. |
| 회수용 자석 | 아군 턴에 기본 카드를 뽑기 직전 빈자리가 있으면, 버린 더미의 무작위 카드 하나를 전투당 한 번 되찾는다. |
| 흰 깃털 펜던트 | 사용 중인 카드 외에 손패에 유지 카드가 있으면 그 턴 처음으로 다친 아군을 회복하는 카드의 회복량이 1 증가한다. |
| 검은 깃털 펜던트 | 사용 중인 카드 외에 손패에 유지 카드가 있으면 그 턴 최초의 유효 직접 카드 피해가 1 증가한다. |
| 접이식 침대 | 캠프에서 휴식하면 생존 아군의 최종 체력 회복량이 각자 3 증가한다. 최대 체력이 상한이다. |
| 낡은 패치 노끈 | 아군 턴 종료의 자동 정리 직전에 손패가 비어 있으면 다음 아군 턴에 2장을 더 뽑는다. |

현재 등록 카드에는 유지 속성이 없다. 깃털 펜던트의 발동은 실제 손패를 사용하는 검사 카드로 확인한다.

---

## 유물 아이콘 제작

기본 이미지 생성 도구로 기존 푸른 잉크병과 같은 원화 형식의 투명 아이콘을 제작했다.

```text
One original item icon for a handcrafted dark fantasy inventory. Subject: {subject}. Detailed painted metal textures and bright edge highlights, authentic weathered RPG loot, warm rim light, legible at 48 px. Isolated centered object, square canvas, fill about 70% of frame. Truly transparent background and no backdrop, no border, no floor, no shadow outside the object, no mist or smoke, no text, no symbols outside the item. The reference only guides the dark fantasy painting style and scale.
```

| 저장 파일 | 개별 프롬프트의 주제 |
| --- | --- |
| [RedInkBottle.png](../../Assets/_ProjectC/Art/RelicIcons/RedInkBottle.png) | one small bevelled transparent glass ink bottle containing rich crimson ink, with cork stopper and subtle brass ring |
| [GoldBookmark.png](../../Assets/_ProjectC/Art/RelicIcons/GoldBookmark.png) | one slim gold leaf metal bookmark with a violet tassel, exquisite antique pressed edging and an abstract filigree cutout |
| [RiftSealingTape.png](../../Assets/_ProjectC/Art/RelicIcons/RiftSealingTape.png) | one small worn roll of metallic industrial sealing tape with brown paper sides and a peeled strip curling out, copper diagonal markings without lettering |
| [RetrievalMagnet.png](../../Assets/_ProjectC/Art/RelicIcons/RetrievalMagnet.png) | one chunky worn blue steel horseshoe magnet, brass bindings on both poles and a small pale electric glint, antique salvage tool |
| [WhiteFeatherPendant.png](../../Assets/_ProjectC/Art/RelicIcons/WhiteFeatherPendant.png) | one warm silver necklace pendant holding a crisp ivory-white feather, curled simple copper suspension loop, gemstone-free |
| [BlackFeatherPendant.png](../../Assets/_ProjectC/Art/RelicIcons/BlackFeatherPendant.png) | one worn brass necklace pendant holding a dark charcoal black feather with blue iridescence, tiny metal suspension ring |
| [FoldingCot.png](../../Assets/_ProjectC/Art/RelicIcons/FoldingCot.png) | one compact collapsed camping cot with dark olive khaki rolled canvas strapped to a small X-braced folding metal frame, distinct horizontal silhouette |
| [WornPatchCord.png](../../Assets/_ProjectC/Art/RelicIcons/WornPatchCord.png) | one small coil of old cloth mending cord with a few fraying threads and one small weathered brown stitched burlap patch beside the coil |

---

## 검증 및 실제 화면

- Unity EditMode 통합 검사 55건 통과. 실패 0건, 건너뜀 0건. 캠프의 추가 회복, 상점 회귀, 실제 손패의 유지 조건과 후보 선택을 포함한다.
- Windows Direct3D 11 플레이어에서 여덟 유물의 최초 획득, 중복 골드 변환 및 보유 유물의 저장·복원을 확인했다.
- 같은 전투에서 첫 번째 찌르기의 기본 피해가 붉은 잉크병으로 7에서 9로 증가한 뒤, 두 번째 사용에서 7로 돌아왔다. 적 체력은 각각 42/50과 36/50으로 표시됐다. 전투 촬영에는 피해 계산 창이 포함돼 있다.
- [유물 보관함](FieldWearRelics.png) · [첫 타격](RedInkFirstHit.png) · [두 번째 타격](RedInkSecondHit.png)
