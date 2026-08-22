# Project C - 41일차 개발일지

## 작업 주제

절차 탐사 Tilemap 충돌 완성 및 미니맵 플레이어 위치 표시

## 개발 목표

- 40일차 WallPreview를 실제 충돌 Wall Tilemap으로 전환
- Wall Tilemap에 TilemapCollider2D 적용
- CompositeCollider2D를 이용한 벽 충돌 통합
- 방과 통로 외부로 플레이어가 이동하지 못하도록 실제 지형 충돌 완성
- F9 재생성 시 Wall Collider도 즉시 갱신
- 다음 층 이동 시 새로운 Wall과 Collider 생성
- 전투 복귀 시 동일 Seed 기반 Wall 구조 복원
- 미니맵에 플레이어 현재 위치 `P` 표시
- 방과 통로 이동 중 미니맵 플레이어 위치 갱신
- 전투 승리 후 클리어된 조우 오브젝트가 필드에 남는 문제 수정

## 구현 내용

### 1. WallPreview를 실제 Wall로 전환

40일차에서 외곽 형태 확인 용도로 사용하던 `WallPreview`를 실제 탐사 충돌을 담당하는 `Wall` Tilemap으로 변경했다.

런타임 생성 구조는 다음과 같다.

```text
ExplorationMapRuntime
└─ ExplorationTilemapGrid
    ├─ Floor
    └─ Wall
```

`Wall`은 더 이상 단순 시각 표시가 아니라 실제 플레이어 이동을 제한하는 지형으로 사용한다.

### 2. Wall 타일 생성 방식 유지 및 충돌 활성화

전체 방과 통로 Floor를 먼저 생성한 뒤 Floor 바깥쪽 8방향을 검사하여 Wall을 생성한다.

이 순서를 유지함으로써 연결된 방 사이의 통로 입구가 벽으로 막히지 않도록 했다.

Wall Tile은 `Tile.ColliderType.Grid`를 사용하여 각 Wall Cell이 실제 물리 충돌 영역을 가지도록 변경했다.

### 3. TilemapCollider2D 적용

Wall 오브젝트에 `TilemapCollider2D`를 런타임으로 자동 추가한다.

따라서 별도로 Scene이나 Inspector에서 Collider를 설정할 필요 없이 탐사 Scene 진입 시 자동으로 벽 충돌이 생성된다.

### 4. CompositeCollider2D 적용

Wall 오브젝트에 `CompositeCollider2D`를 추가하고 Tilemap Collider를 Merge 방식으로 연결했다.

인접한 다수의 Wall Collider를 하나의 복합 충돌 형태로 합성하여 큰 절차 맵에서도 개별 Collider를 그대로 유지하는 것보다 효율적인 구조를 사용한다.

### 5. 정적 Wall Rigidbody2D 적용

Wall 오브젝트에 `Rigidbody2D`를 추가하고 Body Type을 `Static`으로 설정했다.

벽 자체는 이동하지 않으며 플레이어 Rigidbody2D와의 실제 충돌만 담당한다.

### 6. Wall Collider 즉시 갱신

F9 재생성이나 층 이동으로 Tilemap의 Wall 타일이 변경되면 `TilemapCollider2D`의 변경 상태를 확인한 뒤 `ProcessTilemapChanges()`를 호출한다.

이를 통해 이전 맵의 보이지 않는 Collider가 남거나 새로운 Wall 충돌 반영이 늦어지는 문제를 방지한다.

### 7. 플레이어 이동 영역을 실제 벽으로 제한

40일차에서 기존 `±4.35` Clamp 이동 제한을 제거했기 때문에 플레이어 이동 가능 영역을 코드 좌표 제한이 아닌 실제 Wall Collider가 결정하도록 완성했다.

정상적인 탐사 이동은 다음과 같다.

```text
방 내부 이동 가능
통로 이동 가능
방 외벽 통과 불가
통로 외벽 통과 불가
맵 바깥 이동 불가
```

### 8. 미니맵 플레이어 위치 조회 기능 추가

`ExplorationMapRuntime`에 현재 탐사 플레이어의 논리 위치를 조회하는 기능을 추가했다.

현재 플레이어 World Position을 `ExplorationTilemapView`로 전달하고 Tilemap Cell 위치를 `RoomSpacing` 기준 논리 위치로 변환한다.

이를 통해 미니맵에서 단순한 방 번호가 아니라 실제 플레이어 이동 위치를 추적할 수 있게 했다.

### 9. World 위치를 연속 논리 위치로 변환

`ExplorationTilemapView`에 World Position을 미니맵 좌표용 논리 위치로 변환하는 기능을 추가했다.

처리 흐름은 다음과 같다.

```text
Player World Position
↓
Tilemap.WorldToCell
↓
현재 Tile Cell
↓
RoomSpacing 기준 좌표 변환
↓
미니맵 논리 위치
```

방과 방 사이 통로를 이동할 때도 위치가 연속적으로 변하도록 구성했다.

### 10. 미니맵 `P` 오버레이 추가

기존 미니맵 표시는 다음과 같았다.

```text
S = 시작
E = 조우
▼ = 계단
· = 일반 방
```

41일차부터 다음 표시를 추가했다.

```text
P = 현재 플레이어 위치
```

`P`는 기존 셀의 `S`, `E`, `▼` 값을 교체하지 않고 별도의 오버레이로 그린다.

따라서 조우 방에 플레이어가 있어도 조우 정보와 플레이어 위치 정보를 함께 유지한다.

### 11. 통로 이동 중 P 위치 갱신

플레이어의 실제 Tile Cell 위치를 기준으로 미니맵 좌표를 계산하기 때문에 방 중심뿐 아니라 방과 방 사이 통로를 이동할 때도 `P`가 연결선 위를 따라 이동한다.

현재 Tilemap Cell 단위로 위치를 계산하므로 미니맵 이동은 1 Tile 단위로 갱신된다.

### 12. 미니맵 디버그 정보 갱신

41일차 디버그 맵에 다음 정보를 표시하도록 수정했다.

- 현재 층
- 신규 / 복원 상태
- Seed
- `S`, `E`, `▼`, `P` 범례
- 현재 조우 수
- Floor Tile 수
- 실제 Wall Tile 수
- 방 크기
- 방 간격
- Wall Collider 활성 상태
- F9 재생성 안내

### 13. F9 Wall 및 Collider 재생성

F9를 사용하면 기존 Tilemap을 비우고 새로운 Seed로 현재 층을 재생성한다.

처리 흐름:

```text
기존 Floor / Wall 제거
↓
새 Seed 생성
↓
새 논리 맵 생성
↓
새 Floor 생성
↓
새 Wall 생성
↓
Wall Collider 갱신
↓
새 조우 / 계단 생성
↓
플레이어 Start 위치 이동
```

### 14. 다음 층 Wall 충돌 연동

계단으로 다음 층에 진입했을 때도 새로운 Seed와 논리 맵을 기준으로 Floor, Wall, Collider를 모두 다시 생성한다.

이전 층 Wall Collider가 다음 층에 남지 않도록 Tilemap 변경 시 충돌 갱신을 함께 처리한다.

### 15. 전투 복귀 동일 Wall 복원

39일차부터 유지하고 있는 현재 층 Seed를 그대로 사용하여 전투 Scene에서 돌아왔을 때 같은 논리 맵을 다시 생성한다.

따라서 다음 요소가 전투 전후 동일하게 복원된다.

- 방 배치
- 통로 배치
- Wall 배치
- 계단 위치
- 남은 조우 위치

### 16. 클리어 조우 필드 잔존 문제 수정

전투에서 승리한 뒤 탐사 Scene으로 복귀했을 때 해당 조우는 더 이상 충돌하지 않지만 색상 사각형이 필드에 남는 문제가 있었다.

원인은 기존 처리 순서가 다음과 같았기 때문이다.

```text
탐사 Scene 로드
↓
맵과 조우 생성
↓
전투 결과 처리
↓
조우 클리어 ID 등록
```

이 구조에서는 전투 결과가 반영되기 전에 이미 클리어한 조우 오브젝트가 생성된다.

### 17. 전투 결과 처리 시점을 Awake로 변경

`ExplorationBattleResultReceiver`의 전투 결과 처리를 `Start()`에서 `Awake()`로 이동했다.

변경 후 흐름:

```text
탐사 Scene 로드
↓
전투 결과 우선 처리
↓
클리어 조우 ID 등록
↓
맵과 조우 생성
↓
클리어된 조우 생성 생략
```

이를 통해 승리한 조우가 다시 필드에 보이는 문제를 수정했다.

미니맵에서도 해당 조우의 `E`가 제거되고 다른 남은 조우만 유지된다.

## 수정 파일

- `Assets/_ProjectC/Scripts/Exploration/ExplorationBattleResultReceiver.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapDebugView.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationMapRuntime.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationPrototypeBootstrap.cs`
- `Assets/_ProjectC/Scripts/Exploration/ExplorationTilemapView.cs`

## 생성 파일

없음

## 삭제 파일

없음

## 테스트 항목

- [ ] `30_Exploration` 진입 시 Floor와 Wall 정상 생성
- [ ] Wall 오브젝트에 TilemapCollider2D 생성
- [ ] Wall 오브젝트에 CompositeCollider2D 생성
- [ ] Wall Rigidbody2D가 Static으로 동작
- [ ] 플레이어가 방 외벽을 통과하지 못함
- [ ] 플레이어가 통로 외벽을 통과하지 못함
- [ ] 연결된 방 사이 통로 이동 가능
- [ ] 방과 통로 입구가 Wall로 막히지 않음
- [ ] 맵 바깥으로 이동할 수 없음
- [ ] 미니맵에 `P` 표시
- [ ] 방 이동 시 `P` 위치 갱신
- [ ] 통로 이동 중에도 `P` 위치 갱신
- [ ] `P` 표시와 `S`, `E`, `▼` 정보가 함께 유지
- [ ] F9 후 새로운 Floor와 Wall 생성
- [ ] F9 후 이전 Wall Collider가 남지 않음
- [ ] 계단 이동 후 다음 층 Wall 충돌 정상 생성
- [ ] 조우 접촉 후 Battle 정상 진입
- [ ] 전투 승리 후 같은 Seed의 Tilemap 복원
- [ ] 승리한 조우 오브젝트가 필드에서 사라짐
- [ ] 승리한 조우 미니맵 `E` 제거
- [ ] 남은 조우는 기존 위치 유지

## 현재 단계의 제한 사항

현재 Tilemap은 테스트용 런타임 단색 Tile을 사용하고 있다.

방과 통로의 실제 그래픽, 장식, 지역별 타일셋, 오브젝트 배치 등 시각적인 탐사 환경 구성은 아직 포함하지 않는다.

또한 현재 미니맵 `P` 표시는 실제 Tile Cell 위치를 기준으로 하는 개발용 표시이며 이후 정식 UI 단계에서 전용 플레이어 아이콘과 스타일로 교체할 수 있다.

GitHub 저장소에는 자동 Unity 컴파일 또는 Play Mode 테스트를 수행하는 CI가 등록되어 있지 않으므로 실제 Editor 실행 테스트는 로컬 Unity 환경에서 확인한다.

## 완료 결과

41일차를 통해 36~40일차에서 구축한 절차 탐사 맵이 실제 이동 가능한 충돌 공간으로 완성되었다.

Floor와 통로의 외곽에는 실제 Wall Tilemap과 물리 Collider가 생성되고 플레이어 이동 가능 영역은 더 이상 코드 Clamp가 아니라 실제 지형 충돌로 결정된다.

동시에 미니맵에 현재 플레이어 위치 `P`를 추가하여 넓어진 탐사 공간에서 현재 위치를 확인할 수 있게 했다.

또한 전투 결과 처리 시점을 맵 생성보다 앞으로 이동하여 전투에서 승리한 조우가 탐사 필드에 시각적으로 남는 문제를 해결했다.

이를 통해 절차 맵 생성, 상태 복원, 실제 Tilemap, 벽 충돌, 조우 진행, 미니맵 위치 표시까지 하나의 탐사 공간 흐름으로 연결되었다.
