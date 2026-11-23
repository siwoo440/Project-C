---

# 237일차 방패 카드 확장 · 몸으로 막기부터 저항 태세까지

원본 카드 시트 61행과 63~69행의 방패 카드 8종을 추가했다. 보물에서 카드의 소유자를 선택하고, 탐사 저장에서 같은 소유자에게 복원한다.

| 카드 | 기본 AP | 전투 효과 |
| --- | ---: | --- |
| 몸으로 막기 | 1 | 다른 생존 아군을 지정한다. 적의 다음 직접 공격이 명중하면 시전자가 자신의 방어력과 방어도로 그 공격을 대신 받는다. |
| 충격 흡수 | 1 | 다음 적 턴이 끝날 때까지 직접 공격마다 피해를 최대 5 줄인다. 방어도가 피해를 흡수하기 전에 적용한다. |
| 수비 명령 | 1 | 선택한 생존 아군 한 명의 물리 방어력을 2턴 동안 2 높인다. |
| 견고한 자세 | 1 | 추가 행동력 1을 지불하고, 자신의 물리 방어력을 2턴 동안 3 높인다. 기본 비용만 할인할 수 있다. |
| 방패 밀치기 | 1 | 적 하나에게 물리 피해 5를 준다. 명중하면 20% 확률로 대상의 다음 행동을 한 번 취소한다. |
| 가드 전환 | 1 | 자신에게 방어도 5를 지급한다. 사용한 카드를 손패에서 뺀 뒤 한 장을 뽑는다. |
| 막아 세우기 | 1 | 선택한 적의 공격력을 두 번의 적 턴 동안 2 낮춘다. |
| 저항 태세 | 1 | 두 번의 적 턴 동안 자신의 직접 마법 피격 피해를 일반 저항 계산 후 15% 줄인다. |

적 행동은 공용 행동력 없이 예정 행동 단위로 실행된다. 방패 밀치기의 행동력 감소는 예정 행동 한 번 취소로 적용했다. 충격 흡수와 저항 태세는 적 턴이 끝날 때 지속 시간을 차감한다.

---

## 검증과 실제 화면

Unity 검사 75건 통과, 실패·건너뜀 0건. 여덟 원화의 SHA-256과 보물 보상, 고른 소유자의 저장 복원, 적 공격 보호·반복 경감·턴 만료 및 기존 카드 회귀를 검증했다.

Windows x64 플레이어에서 견고한 자세의 비용 배지 2와, 견고한 자세·충격 흡수·저항 태세 적용 후 남은 AP 2를 확인했다.

| 방패 카드 손패 | 실제 전투 방어 상태 |
| --- | --- |
| ![방패 카드 원화 여덟 장과 견고한 자세의 실제 비용](ShieldCardHand.png) | ![충격 흡수와 저항 태세의 적 턴 수 및 물리 방어 증가](ShieldStances.png) |

---

## 상태 아이콘 제작

전투 아이콘 2종은 기본 이미지 생성 도구로 만들었다. 카드 원화는 원본 압축 파일의 그림을 사용했다.

- [충격 흡수](../../Assets/_ProjectC/Resources/UI/Battle/ShockAbsorptionStatusIcon.png) 원화 프롬프트

  > One original readable dark fantasy RPG status token. a compact antique shield being struck by a cool blue impact ring, a shock wave collapsing harmlessly into the shield, brushed steel and warm gold trim. Painterly high-contrast metal, centered silhouette occupying 70 percent of a square, no frame, no text, no smoke, no background, true transparent alpha. Clear at 24 pixels. Use the supplied status icon as style reference only.

- [저항 태세](../../Assets/_ProjectC/Resources/UI/Battle/ResistanceStanceStatusIcon.png) 원화 프롬프트

  > One original readable dark fantasy RPG status token. a tall indigo steel shield blocking a radiant violet spell, clean violet arcane spark on the shield's face, brushed pale silver trim. Painterly high-contrast metal, centered silhouette occupying 70 percent of a square, no frame, no text, no smoke, no background, true transparent alpha. Clear at 24 pixels. Use the supplied status icon as style reference only.
