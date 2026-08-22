# Project C - 40일차 개발일지

## 작업 주제

절차 탐사 논리 맵의 Tilemap 변환 기반 구축

## 개발 목표

- 기존 논리 맵 구조를 실제 Unity Tilemap 공간으로 변환
- 논리 셀마다 실제 탐사 방 생성
- 연결된 논리 셀 사이에 실제 통로 생성
- 기존 압축 월드 좌표 계산 방식을 Tilemap 기준 좌표로 교체
- 플레이어 시작 위치를 실제 Start 방 중심에 배치
- 조우를 실제 Encounter 방 중심에 배치
- 계단을 실제 Stairs 방 중심에 배치
- 넓어진 탐사 공간에 맞게 플레이어 고정 이동 범위 제거
- 카메라가 플레이어를 자동 추적하도록 구성
- 39일차 Seed 및 탐사 상태 복원 구조 유지
- F9 및 층 이동 시 Tilemap도 함께 재생성

## 구현 내용

### 1. ExplorationTilemapView 추가

논리 맵을 실제 Tilemap 공간으로 변환하는 `ExplorationTilemapView`를 추가했다.

런타임에서 다음 구조를 자동 생성한다.

```text
ExplorationMapRuntime
└─ ExplorationTilemapGrid
    ├─ Floor
    └─ WallPreview
```

Hierarchy 또는 Inspector에서 별도의 Tilemap 오브젝트를 수동 생성할 필요 없이 탐사 Scene 진입 시 자동 구성된다.

### 2. 런타임 Grid 생성

`Grid` 컴포넌트를 가진 `ExplorationTilemapGrid` 오브젝트를 생성한다.

현재 Cell Size는 1x1 기준으로 사용한다.

해당 Grid 아래에 다음 Tilemap을 생성한다.

- `Floor`
- `WallPreview`

### 3. 논리 셀을 실제 방으로 변환

36일차부터 사용하고 있는 `ExplorationMapCell` 하나를 실제 탐사 공간의 하나의 방으로 변환한다.

현재 기본 방 크기:

```text
7 x 7 Tile
```

각 논리 좌표는 고정 간격을 기준으로 Tilemap 방 중심 좌표에 대응된다.

### 4. 방 중심 간격 적용

논리 셀 사이의 실제 공간을 확보하기 위해 방 중심 간격을 적용했다.

현재 기본 간격:

```text
RoomSpacing = 10
```

예를 들어 논리 좌표가 다음과 같다면:

```text
(0, 0)
(1, 0)
```

실제 Tilemap에서는 두 방 중심이 10 Tile 간격으로 배치된다.

### 5. 연결 정보 기반 통로 생성

기존 `ExplorationMapCell`의 연결 정보를 그대로 사용하여 통로를 생성한다.

사용하는 연결 상태:

- `ConnectedUp`
- `ConnectedRight`

중복 생성을 피하기 위해 위쪽과 오른쪽 연결을 기준으로 실제 Floor 통로를 만든다.

현재 통로 폭은 3 Tile이다.

### 6. Floor Tilemap 생성

방과 통로에 해당하는 모든 Cell을 수집한 뒤 `Floor` Tilemap에 런타임 타일을 배치한다.

별도 그래픽 에셋 없이 `Texture2D.whiteTexture` 기반의 임시 Sprite와 Tile을 런타임 생성하여 사용한다.

현재 Floor는 탐사 구조 확인을 위한 임시 색상으로 표시한다.

### 7. WallPreview 생성

Floor 타일 주변의 비어 있는 셀을 탐색하여 `WallPreview`를 생성한다.

현재 WallPreview의 목적은 실제 충돌 구현이 아니라 탐사 공간의 외곽 형태를 시각적으로 확인하는 것이다.

따라서 현재 WallPreview Tile은 Collider를 사용하지 않는다.

실제 벽 충돌과 TilemapCollider2D 연결은 다음 Tilemap 완성 단계에서 처리한다.

### 8. 논리 좌표 → Tilemap World 좌표 변환

기존 탐사 구조에서는 전체 논리 맵을 약 `-3.4 ~ 3.4` 범위 안에 압축하여 World Position을 계산했다.

40일차부터 해당 방식을 제거하고 다음 흐름으로 변경했다.

```text
논리 좌표
↓
Tilemap 방 중심 Cell
↓
Tilemap.GetCellCenterWorld()
↓
실제 World Position
```

이제 플레이어, 계단, 조우가 동일한 Tilemap 좌표 체계를 사용한다.

### 9. 계단 위치 Tilemap 연결

기존 `StairsCoordinate`를 실제 Tilemap의 해당 방 중심 위치로 변환한다.

노란색 계단 오브젝트와 Trigger 구조는 유지하면서 위치 계산만 Tilemap 기반으로 변경했다.

### 10. 절차 조우 위치 Tilemap 연결

38일차에서 만든 절차 조우 셀의 논리 좌표를 실제 Tilemap 방 중심 위치로 변환한다.

따라서 각 조우 오브젝트는 자신이 배정된 실제 탐사 방 안에 생성된다.

기존 런타임 조우 ID와 클리어 상태 관리 구조는 유지한다.

### 11. 플레이어 시작 위치 Tilemap 연결

`StartCoordinate`에 해당하는 실제 방 중심을 계산하여 탐사 플레이어의 시작 위치로 사용한다.

전투 후 복귀 위치가 존재하는 경우에는 기존 39일차 구조대로 해당 Return Position을 우선 유지한다.

### 12. 플레이어 고정 이동 범위 제거

기존 `ExplorationPlayerController`에는 다음과 같은 고정 이동 범위가 존재했다.

```text
X : -4.35 ~ 4.35
Y : -4.35 ~ 4.35
```

Tilemap 탐사 공간은 여러 방으로 확장되므로 해당 Clamp 제한을 제거했다.

이제 플레이어는 전체 Tilemap 공간을 자유롭게 이동할 수 있다.

현재 벽 Collider가 아직 없기 때문에 Floor 바깥쪽으로도 이동 가능한 상태이며 이는 41일차에서 제한한다.

### 13. 탐사 카메라 추적 추가

새로운 `ExplorationCameraFollow`를 추가했다.

탐사 Scene의 Camera를 자동으로 찾아 컴포넌트를 추가하고, `LateUpdate()`에서 현재 탐사 플레이어의 X/Y 위치를 추적한다.

카메라의 기존 Z 위치는 유지한다.

넓어진 Tilemap에서 플레이어가 현재 화면을 벗어나도 카메라가 함께 이동한다.

### 14. 기존 Seed 복원 구조 유지

39일차에서 구축한 현재 층 Seed 저장 및 복원 기능을 그대로 사용한다.

동일 Seed를 복원하면:

- 같은 논리 맵
- 같은 방 위치
- 같은 통로
- 같은 계단 위치
- 같은 조우 배치

를 다시 Tilemap으로 생성한다.

따라서 전투 Scene을 다녀와도 동일한 탐사 공간이 다시 생성되는 구조를 유지한다.

### 15. F9 Tilemap 재생성 연동

F9 사용 시 기존처럼 현재 층은 유지하면서 새로운 Seed로 논리 맵을 다시 생성한다.

이 과정에 Tilemap 생성도 연결했다.

```text
F9
↓
새 Seed
↓
새 논리 맵
↓
새 Floor Tilemap
↓
새 WallPreview
↓
새 계단 위치
↓
새 조우 배치
↓
플레이어 Start 위치 이동
```

### 16. 다음 층 Tilemap 생성 연동

계단을 사용하여 다음 층으로 이동하면 새로운 Seed와 논리 맵을 만든 뒤 해당 구조에 맞는 새로운 Tilemap을 생성한다.

예:

```text
1F / Seed A / Tilemap A
↓
계단
↓
2F / Seed B / Tilemap B
```

### 17. 디버그 화면 갱신

기존 논리 맵 디버그 화면을 40일차 Tilemap 테스트 기준으로 갱신했다.

현재 다음 정보를 확인할 수 있다.

- 현재 층
- 신규 / 복원 상태
- Seed
- 조우 개수
- Floor Tile 개수
- Wall Preview Tile 개수
- 방 크기
- 방 간격
- F9 재생성 안내

논리 맵의 `S`, `E`, `▼`, `·` 표시는 기존처럼 유지한다.

### 18. 탐사 안내 HUD 갱신

탐사 화면의 테스트 안내를 Tilemap 변환 기준으로 수정했다.

현재 탐사 화면에서 방과 통로가 실제 공간에 생성되고 플레이어가 확장된 탐사 공간을 이동할 수 있음을 확인할 수 있도록 변경했다.

## 생성 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationCameraFollow.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationCameraFollow.cs.meta`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationTilemapView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationTilemapView.cs.meta`

## 수정 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapDebugView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPlayerController.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs`

## 삭제 파일

없음

## 테스트 항목

- [ ] `30_Exploration` 진입 시 `ExplorationTilemapGrid` 자동 생성
- [ ] `Floor` Tilemap 자동 생성
- [ ] `WallPreview` Tilemap 자동 생성
- [ ] 논리 셀마다 7x7 방 생성
- [ ] 연결된 셀 사이에 실제 통로 생성
- [ ] 연결되지 않은 방 사이에 불필요한 통로가 생성되지 않음
- [ ] 플레이어가 Start 방 중심에 생성
- [ ] 조우가 해당 E 방 중심에 생성
- [ ] 계단이 Stairs 방 중심에 생성
- [ ] 플레이어의 기존 ±4.35 이동 제한이 제거됨
- [ ] 플레이어가 여러 방을 이동할 수 있음
- [ ] 카메라가 플레이어 위치를 추적
- [ ] F9 사용 시 새로운 Tilemap 생성
- [ ] 다음 층 이동 시 새로운 Tilemap 생성
- [ ] 전투 후 복귀 시 동일 Seed 유지
- [ ] 전투 후 같은 방과 통로 구조 복원
- [ ] 전투 후 같은 계단 위치 복원
- [ ] 전투 후 남은 조우 위치 유지
- [ ] 승리한 조우만 제거

## 현재 단계의 제한 사항

40일차의 Tilemap은 실제 탐사 지형을 생성하는 첫 단계이다.

현재 `WallPreview`는 시각적 외곽 표시만 담당하며 실제 충돌 기능이 없다.

따라서 플레이어는 현재 Floor와 통로 밖으로 이동할 수 있다.

다음 Tilemap 완성 단계에서는 다음 기능을 연결해야 한다.

- Wall Tilemap 구조 정리
- TilemapCollider2D
- 벽 충돌
- 실제 이동 가능 영역 제한
- 방과 통로 경계 정리
- 층 이동 / F9 / 전투 복귀 전체 Tilemap 안정화

또한 현재 오른쪽 아래 논리 맵에는 시작점, 조우, 계단은 표시되지만 **플레이어의 현재 위치를 나타내는 미니맵 아이콘은 아직 구현되어 있지 않다.**

## 완료 결과

40일차를 통해 기존의 논리 격자 기반 탐사 맵이 실제 Unity Tilemap 공간으로 표현되기 시작했다.

각 논리 셀은 실제 7x7 방으로 변환되고 연결 관계에 따라 통로가 생성된다.

플레이어, 조우, 계단의 좌표 역시 기존 압축 월드 좌표 방식에서 Tilemap 방 중심 기준으로 전환했으며 넓어진 탐사 공간을 이동할 수 있도록 플레이어 이동 제한을 제거하고 카메라 추적을 추가했다.

39일차의 Seed 상태 보존 구조도 그대로 유지하여 전투 복귀 시 동일한 논리 맵에서 동일한 Tilemap을 다시 생성할 수 있는 기반을 마련했다.
