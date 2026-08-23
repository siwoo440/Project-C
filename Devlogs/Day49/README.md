# Project C - 49일차 개발일지

## 작업 주제

절차적 탐사 이벤트, 선택 결과 처리, 블러 이벤트 패널 UI 및 이벤트 월드 표시 시스템 구축

## 개발 목표

- 절차적으로 생성되는 탐사 맵에 이벤트 오브젝트 배치
- 시작 방·계단 방·적 조우 방을 피한 이벤트 배치
- 이벤트 접촉 시 탐사 화면 위에 전용 패널 표시
- 이벤트 패널 표시 중 뒤 탐사 화면 블러 및 어둡게 처리
- 이벤트 패널을 좌측 이미지, 우측 설명, 하단 선택지 구조로 구성
- 선택지에 따라 보상·위험·확률 결과 적용
- 이벤트 처리 완료 상태를 탐사 세션에 기록
- 처리한 이벤트가 같은 탐사 런에서 다시 나타나지 않도록 처리
- 이벤트 패널이 열린 동안 플레이어 이동 차단
- New Input System 기반 이벤트 선택지 버튼 클릭 지원
- 임시 이벤트 오브젝트를 청록색 사각형 + `?`로 표시
- 정식 이벤트 데이터가 생기면 같은 이벤트가 항상 같은 월드 스프라이트를 사용하도록 기반 마련

## 구현 내용

### 1. 탐사 이벤트 데이터 구조 추가

`ExplorationEventData`를 추가해 탐사 이벤트를 데이터 단위로 관리할 수 있도록 했다.

주요 데이터:

```text
EventId
DisplayName
Description
IllustrationSprite
WorldSprite
Category
Choices
```

이벤트 패널에서 사용하는 큰 그림은 `IllustrationSprite`, 탐사 맵에서 사용하는 고정 이벤트 표시는 `WorldSprite`로 분리했다.

이를 통해 같은 이벤트가 여러 층이나 여러 위치에서 등장하더라도 동일한 이벤트 데이터는 동일한 월드 스프라이트를 사용할 수 있다.

### 2. 이벤트 분류

현재 이벤트는 다음 세 종류를 지원한다.

```text
Reward
Risk
Choice
```

이벤트 종류와 실제 결과 처리를 분리해 이후 이벤트 콘텐츠 확장에 사용할 수 있도록 구성했다.

### 3. 이벤트 선택지 데이터

각 이벤트는 여러 선택지를 가질 수 있다.

선택지에는 다음 정보를 저장한다.

```text
선택 문구
즉시 결과 문구
확률 판정 사용 여부
성공 확률
성공 결과
실패 결과
자원 변화량
```

즉시 결과와 확률 결과를 모두 지원한다.

### 4. 기본 테스트 이벤트 3종

`Resources/ExplorationEvents`에 정식 이벤트 데이터가 없더라도 시스템을 테스트할 수 있도록 런타임 기본 이벤트를 제공한다.

현재 기본 이벤트:

```text
버려진 상자
손상된 단말기
보급품 더미
```

각 이벤트에는 서로 다른 선택지와 자원 결과가 포함되어 있다.

### 5. 절차적 이벤트 배치

현재 탐사 맵 Seed를 기반으로 이벤트를 배치한다.

이벤트 후보 방은 다음 조건을 사용한다.

```text
Normal 방
+
적 조우가 없는 방
```

따라서 시작 방, 계단 방, 적 조우가 이미 배치된 방과 이벤트가 겹치지 않도록 구성했다.

현재 층당 기본 이벤트 수는 2개다.

### 6. 이벤트 발견률

이벤트 배치에는 기본 발견 확률과 통신 기지국 설비 보너스를 반영한다.

현재 구조:

```text
기본 이벤트 발견 확률
+
Communication Event Discovery Bonus
↓
이벤트 생성 판정
```

개발 테스트 편의를 위해 한 층에 최소 하나의 이벤트가 생성될 수 있도록 구성했다.

### 7. 48일차 안전 위치 시스템 재사용

이벤트 오브젝트 위치는 기존 방 중심에 고정하지 않고 48일차에서 추가한 안전 위치 계산을 재사용한다.

```text
이벤트가 배치될 방 결정
↓
방 내부 안전 Floor 위치 조회
↓
Seed 기반 위치 선택
↓
이벤트 오브젝트 배치
```

안전 위치 계산에 실패한 경우 기존 방 중심 좌표를 Fallback으로 사용한다.

### 8. 이벤트 Runtime ID

이벤트는 층, 방 좌표, 맵 Seed를 조합해 런타임 ID를 만든다.

예시:

```text
EV_F1_X2_Y-1_S123456
```

이 ID를 통해 같은 탐사 런에서 어떤 이벤트를 이미 처리했는지 추적한다.

### 9. 이벤트 접촉 처리

탐사 이벤트에는 Trigger 기반 `ExplorationEventView`가 연결된다.

플레이어가 이벤트에 접근하면:

```text
플레이어 이벤트 접촉
↓
처리 완료 여부 확인
↓
이벤트 패널 열기
↓
중복 상호작용 잠금
```

순서로 작동한다.

### 10. 이벤트 패널 UI

이벤트 패널은 런타임으로 생성된다.

기본 화면 구조:

```text
┌─────────────────────────────────────────┐
│                이벤트 제목              │
│                                         │
│ ┌──────────────┐  이벤트 설명           │
│ │              │                        │
│ │ 이미지 또는 ?│                        │
│ │              │                        │
│ └──────────────┘                        │
│                                         │
│ [선택지 1]                              │
│ [선택지 2]                              │
└─────────────────────────────────────────┘
```

정식 이벤트 일러스트가 없는 경우 패널 왼쪽에는 `?` 표시를 사용한다.

### 11. 배경 블러 처리

이벤트 패널이 열릴 때 현재 탐사 화면을 캡처한다.

캡처 이미지를 저해상도로 축소한 뒤 Bilinear 필터로 확대해 블러 효과를 만들고, 그 위에 어두운 오버레이를 추가한다.

```text
현재 탐사 화면 캡처
↓
저해상도 축소
↓
Bilinear 확대
↓
어두운 Overlay
↓
이벤트 패널 표시
```

이를 통해 이벤트 진행 중 배경과 이벤트 정보가 시각적으로 분리된다.

### 12. 이벤트 중 플레이어 이동 잠금

이벤트 패널이 열리면 `ExplorationPlayerController`의 전역 입력 차단 상태를 활성화한다.

```text
이벤트 패널 Open
→ 이동 입력 차단

이벤트 패널 Close
→ 이동 입력 복구
```

이벤트를 읽거나 선택하는 동안 플레이어가 계속 움직이지 않도록 처리했다.

### 13. New Input System 버튼 입력 수정

프로젝트가 New Input System을 사용하고 있기 때문에 이벤트 선택지 버튼도 해당 방식으로 입력을 받도록 수정했다.

이벤트 패널은 필요할 경우 런타임에서 다음 구조를 보장한다.

```text
EventSystem
+
InputSystemUIInputModule
```

`InputSystemUIInputModule.AssignDefaultActions()`를 사용해 마우스 Point와 Click 등 기본 UI 입력을 연결한다.

또한 이벤트 패널 배경 이미지의 Raycast를 비활성화해 자식 선택지 버튼의 클릭을 가로채지 않도록 수정했다.

### 14. 선택 결과 처리

선택지를 클릭하면 결과를 계산한다.

즉시 선택:

```text
선택
↓
자원 변화 적용
↓
결과 문구 표시
```

확률 선택:

```text
선택
↓
Runtime Event ID 기반 고정 난수 판정
↓
성공 / 실패
↓
해당 결과 자원 적용
↓
결과 문구 표시
```

확률 이벤트는 동일 Runtime Event ID와 동일 선택지에서는 동일한 판정 결과가 나오도록 구성했다.

### 15. 자원 결과 연동

현재 이벤트 결과는 다음 영구 자원과 연결된다.

```text
Gold
Screw
IronPlate
Wire
```

증가 결과는 `PlayerResourceManager.AddResources()`를 사용하고, 감소 결과는 현재 보유량을 넘지 않는 범위에서 `TrySpend()`를 사용한다.

### 16. 이벤트 처리 완료 상태

선택 결과가 확정되면 `ExplorationSessionManager`에 Runtime Event ID를 기록한다.

```text
선택 완료
↓
ResolvedEventIds 등록
↓
이벤트 오브젝트 제거
```

처리 완료 이벤트는 같은 탐사 런에서 다시 활성화되지 않는다.

### 17. 탐사 복귀 후 이벤트 상태 유지

전투 Scene을 다녀온 뒤 같은 탐사 Seed로 복원될 때도 이미 처리된 Runtime Event ID는 다시 생성하지 않는다.

현재 런 초기화 시에는 처리 이벤트 목록도 함께 초기화된다.

### 18. 이벤트 월드 표시 개선

정식 월드 스프라이트가 없는 이벤트는 탐사 맵에서 다음 형태로 표시한다.

```text
청록색 사각형
+
흰색 ?
```

이를 통해 현재 임시 조우·계단과 이벤트를 쉽게 구분할 수 있도록 했다.

### 19. 이벤트별 고정 월드 스프라이트 기반

`ExplorationEventData`에 `WorldSprite`를 추가했다.

표시 규칙:

```text
WorldSprite 없음
→ 청록색 사각형 + ?

WorldSprite 있음
→ 지정된 Sprite 사용
→ ? 표시 없음
→ Sprite 원래 색상 사용
```

향후 특정 고정 이벤트에 정식 에셋을 적용하면 같은 `ExplorationEventData`를 사용하는 모든 위치에서 동일한 스프라이트가 표시된다.

## 생성 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventCatalog.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventCatalog.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventData.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventData.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventPanelView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventPanelView.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationEventView.cs.meta`

## 수정 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPlayerController.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationSessionManager.cs`

## 삭제 파일

없음

## 테스트 항목

- [ ] Unity Console 컴파일 오류 없음
- [ ] `30_Exploration` 진입 시 이벤트 생성
- [ ] 시작 방에 이벤트가 배치되지 않음
- [ ] 계단 방에 이벤트가 배치되지 않음
- [ ] 적 조우 방과 이벤트가 겹치지 않음
- [ ] 이벤트가 방 내부 안전 위치에 생성
- [ ] 임시 이벤트가 청록색 사각형 + `?`로 표시
- [ ] 이벤트 접촉 시 패널 표시
- [ ] 패널 뒤 탐사 화면 블러 처리
- [ ] 이벤트 패널 중 플레이어 이동 차단
- [ ] 선택지 버튼 Hover 반응
- [ ] 선택지 버튼 좌클릭 정상 동작
- [ ] 선택 후 결과 문구 표시
- [ ] 선택 결과에 따라 자원 값 변경
- [ ] 확률 이벤트 성공·실패 판정 정상 작동
- [ ] 결과 확인 후 이벤트 패널 닫힘
- [ ] 패널을 닫은 뒤 플레이어 이동 재개
- [ ] 처리한 이벤트 오브젝트 제거
- [ ] 처리한 이벤트가 같은 런에서 재등장하지 않음
- [ ] 전투 후 탐사 복귀 시 처리 이벤트가 다시 생성되지 않음
- [ ] F9 새 Seed 생성 시 이벤트 배치 위치 변경
- [ ] `WorldSprite`가 없는 이벤트는 `?` 임시 표시 유지
- [ ] 추후 `WorldSprite`가 설정된 이벤트는 지정 스프라이트로 표시
- [ ] 같은 이벤트 데이터는 어디에 생성되어도 동일한 `WorldSprite` 사용

## 현재 단계의 제한 사항

- 현재 기본 이벤트 콘텐츠는 시스템 검증용 테스트 데이터다.
- 정식 이벤트 일러스트와 월드 스프라이트 에셋은 아직 적용하지 않았다.
- 배경 블러는 별도 Blur Shader가 아니라 화면 캡처 축소·확대 방식의 프로토타입이다.
- 이벤트 결과는 현재 Gold, Screw, IronPlate, Wire 자원 변화 중심으로 구현되어 있다.
- 체력, 정신력, 카드, 유물, 캐릭터 상태 등 더 다양한 이벤트 결과는 이후 확장이 필요하다.
- 현재 탐사 이벤트 완료 상태는 탐사 세션 동안 유지되며 영구 저장 시스템은 이후 저장 일차에서 연결해야 한다.
- GitHub에는 Unity Editor 컴파일과 Play Mode 자동 CI 검사가 등록되어 있지 않으므로 최종 런타임 검증은 로컬 Unity 실행 결과를 기준으로 한다.

## 완료 결과

49일차를 통해 탐사 맵에 전투 조우 외의 선택형 콘텐츠가 추가되었다.

```text
절차적 탐사 맵
↓
빈 일반 방 이벤트 배치
↓
청록색 ? 이벤트 발견
↓
이벤트 접촉
↓
배경 블러 + 이벤트 패널
↓
선택지 선택
↓
보상 / 위험 결과 처리
↓
이벤트 완료 상태 기록
↓
오브젝트 제거
↓
탐사 계속 진행
```

이벤트 표시 시스템은 임시 `?` 표시와 정식 `WorldSprite`를 구분해 두었기 때문에, 이후 실제 이벤트 콘텐츠와 에셋을 추가할 때 시스템 코드를 다시 변경하지 않고 이벤트 데이터 단위로 확장할 수 있는 기반을 마련했다.
