using System.Collections.Generic; // Floor와 Wall 셀 집합 사용
using UnityEngine; // Grid와 2D 물리 기능 사용
using UnityEngine.Tilemaps; // Unity Tilemap 기능 사용

public sealed class ExplorationTilemapView : MonoBehaviour // 논리 맵을 충돌 가능한 Tilemap으로 표시
{
    public const int RoomSize = 7; // 한 논리 셀의 최대 방 크기
    public const int RoomSpacing = 10; // 논리 셀 사이 방 중심 간격
    private const int CorridorHalfWidth = 1; // 통로 절반 폭
    private const int SpawnDoorClearance = 2; // 출입구 주변 조우 배치 금지 거리
    private const int EncounterPositionSalt = 486187739; // 조우 위치 난수 분리값

    private enum RoomShape // 절차 생성 방 형태
    {
        Square, // 정사각형
        Horizontal, // 가로 직사각형
        Vertical, // 세로 직사각형
        LShape, // L자형
        TShape, // T자형
        Cross // 십자형
    }

    private static readonly Vector3Int[] WallDirections =
    {
        new Vector3Int(-1, -1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0)
    }; // Floor 주변 8방향 Wall 탐색 방향

    private static readonly Vector3Int[] CardinalDirections =
    {
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0)
    }; // 안전한 배치 위치 확인용 상하좌우 방향

    private readonly HashSet<Vector3Int> floorCells =
        new HashSet<Vector3Int>(); // 현재 Floor 셀 집합

    private readonly HashSet<Vector3Int> wallCells =
        new HashSet<Vector3Int>(); // 현재 Wall 셀 집합

    private readonly Dictionary<Vector2Int, HashSet<Vector3Int>> roomFloorCells =
        new Dictionary<Vector2Int, HashSet<Vector3Int>>(); // 논리 방별 실제 Floor 셀 집합

    private readonly Dictionary<Vector2Int, ExplorationMapCell> roomData =
        new Dictionary<Vector2Int, ExplorationMapCell>(); // 논리 방별 연결 정보

    private GameObject gridObject; // 런타임 Grid 오브젝트
    private Tilemap floorTilemap; // 실제 이동 공간 Floor Tilemap
    private Tilemap wallTilemap; // 실제 충돌 Wall Tilemap
    private Tilemap wallEdgeTilemap; // 벽과 바닥에 맞닿는 돌 테두리
    private Tilemap decorationTilemap; // 이동 판정과 분리된 벽·바닥 장식
    private TilemapCollider2D wallTilemapCollider; // Wall 타일 충돌 생성기
    private CompositeCollider2D wallCompositeCollider; // Wall 충돌 합성기
    private ExplorationDungeonTiles dungeonTiles; // 바닥과 벽 및 장식용 타일 풀
    private ExpeditionZoneId? renderedZone; // 이 타일 풀을 만들었던 실제 출정 지역

    public int FloorTileCount => floorCells.Count; // 현재 Floor 타일 개수
    public int WallTileCount => wallCells.Count; // 현재 Wall 타일 개수
    internal int WalkableRevision { get; private set; } // 현재 바닥 전체가 다시 만들어진 횟수

    public void Build(ExplorationMapData mapData, ExpeditionZoneId? zone = null) // 실제 충돌과 선택한 지역 원화를 가진 타일맵 생성
    { // 지정 맵과 지역 타일 재구성 시작
        EnsureTilemapHierarchy(); // Grid와 Tilemap 계층 준비
        WalkableRevision++; // 새 바닥에 이전 탐사 적 시야가 쓰이지 않도록 버전 변경

        floorTilemap.ClearAllTiles(); // 이전 Floor 제거
        wallTilemap.ClearAllTiles(); // 이전 Wall 제거
        wallEdgeTilemap.ClearAllTiles(); // 이전 층의 벽 테두리 제거
        decorationTilemap.ClearAllTiles(); // 이전 층의 환경 장식 제거
        floorCells.Clear(); // Floor 셀 목록 초기화
        wallCells.Clear(); // Wall 셀 목록 초기화
        roomFloorCells.Clear(); // 방별 Floor 목록 초기화
        roomData.Clear(); // 방별 연결 정보 초기화

        if (mapData == null ||
            mapData.Cells == null ||
            mapData.Cells.Count == 0)
        {
            RefreshWallCollider(); // 빈 맵 Collider 정리
            return;
        }

        if (dungeonTiles == null || renderedZone != zone) // 지금 표시해야 하는 지역의 바닥과 벽 변경 확인
        { // 이전 지역의 임시 타일과 실제 지역 원화 교체 시작
            dungeonTiles?.Release(); // 빈 레이어에서 이전 지역의 생성 자료만 정리
            dungeonTiles = new ExplorationDungeonTiles(zone); // 현재 저장 지역 키에 맞는 렌더링 자료
            renderedZone = zone; // 같은 지역과 다른 시드에서도 타일 세트 재사용
        } // 이전 지역의 임시 타일과 실제 지역 원화 교체 종료
        foreach (ExplorationMapCell cell in mapData.Cells)
        {
            FillRoom(
                cell,
                mapData.Seed); // Seed와 셀 좌표에 따라 다양한 방 형태 생성

            if (cell.ConnectedRight)
            {
                FillHorizontalCorridor(cell.Coordinate); // 오른쪽 연결 통로 생성
            }

            if (cell.ConnectedUp)
            {
                FillVerticalCorridor(cell.Coordinate); // 위쪽 연결 통로 생성
            }
        }

        foreach (Vector3Int floorCell in floorCells)
        {
            floorTilemap.SetTile(floorCell, dungeonTiles.GetFloor(mapData.Seed, floorCell)); // 같은 좌표에 같은 바닥 재질 배치
            if (ExplorationDungeonTiles.GetCardinalMask(floorCell, floorCells) is 7 or 11 or 13 or 14) // 한 면이 벽과 닿는 바닥 가장자리 확인
            { // 통행로 외곽 장식 선택 시작
                Tile detail = dungeonTiles.GetDetail(mapData.Seed, floorCell, false); // 시드로 정한 작은 잔해 조회
                if (detail != null) // 실제 장식이 있는 바닥 좌표 확인
                { // 바닥 장식 적용 시작
                    decorationTilemap.SetTile(floorCell, detail); // 이동 칸 위의 별도 장식 배치
                } // 바닥 장식 적용 종료
            } // 통행로 외곽 장식 선택 종료
        }

        BuildWalls(); // 전체 Floor 외곽에 실제 Wall 생성

        foreach (Vector3Int wallCell in wallCells)
        {
            wallTilemap.SetTile(wallCell, dungeonTiles.GetWall(mapData.Seed, wallCell)); // 전체 셀 충돌이 있는 벽 배치
            int mask = ExplorationDungeonTiles.GetWallMask(wallCell, floorCells); // 직선과 대각 바닥으로 벽면 모양 결정
            Tile edge = dungeonTiles.GetWallEdge(mask); // 벽과 바닥이 닿는 테두리 조회
            if (edge != null) // 표시할 테두리 원화 확인
            { // 실제 벽면의 테두리 표시 시작
                wallEdgeTilemap.SetTile(wallCell, edge); // 벽 충돌 칸 위에 테두리만 배치
            } // 실제 벽면의 테두리 표시 종료
            if (mask is 1 or 2 or 4 or 8) // 모서리를 피한 직선 벽에만 장식 허용
            { // 벽 장식 선택 시작
                Tile detail = dungeonTiles.GetDetail(mapData.Seed, wallCell, true); // 벽등과 격자 및 사슬 조회
                if (detail != null) // 실제 벽 장식 원화 확인
                { // 벽 장식 적용 시작
                    decorationTilemap.SetTile(wallCell, detail); // 물리 판정 없는 별도 장식 배치
                } // 벽 장식 적용 종료
            } // 벽 장식 선택 종료
        }

        floorTilemap.CompressBounds(); // Floor 사용 영역 정리
        wallTilemap.CompressBounds(); // Wall 사용 영역 정리
        wallEdgeTilemap.CompressBounds(); // 벽 테두리의 실제 사용 영역 정리
        decorationTilemap.CompressBounds(); // 장식 원화의 실제 사용 영역 정리
        RefreshWallCollider(); // 변경된 Wall Collider 즉시 반영
    }

    public Vector2 GetWorldPosition(
        Vector2Int logicalCoordinate) // 논리 셀의 실제 방 중심 위치 반환
    {
        EnsureTilemapHierarchy(); // Tilemap 존재 보장

        Vector3Int centerCell =
            GetRoomCenterCell(
                logicalCoordinate); // 논리 좌표를 방 중심 셀로 변환

        Vector3 worldPosition =
            floorTilemap.GetCellCenterWorld(
                centerCell); // 방 중심 셀의 실제 World 좌표 계산

        return new Vector2(
            worldPosition.x,
            worldPosition.y); // 2D World 좌표 반환
    }

    public bool TryGetRandomEncounterPosition(
        Vector2Int logicalCoordinate,
        int mapSeed,
        out Vector2 worldPosition) // 방 내부의 안전한 Seed 기반 조우 위치 조회
    {
        worldPosition =
            GetWorldPosition(
                logicalCoordinate); // 실패 시 방 중심 위치 기본값 지정

        if (!roomFloorCells.TryGetValue(
                logicalCoordinate,
                out HashSet<Vector3Int> sourceCells) ||
            sourceCells == null ||
            sourceCells.Count == 0)
        {
            return false;
        }

        List<Vector3Int> candidates =
            BuildSafeSpawnCandidates(
                logicalCoordinate,
                sourceCells); // 벽과 출입구에서 떨어진 후보 계산

        if (candidates.Count == 0)
        {
            candidates =
                BuildFallbackSpawnCandidates(
                    logicalCoordinate,
                    sourceCells); // 안전 후보가 없으면 완화 후보 계산
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        candidates.Sort(
            CompareTileCells); // HashSet 순서와 무관하게 Seed 재현 순서 고정

        int randomSeed =
            CreateDeterministicSeed(
                mapSeed,
                logicalCoordinate,
                EncounterPositionSalt); // 맵 Seed와 방 좌표 기반 위치 Seed 생성

        System.Random random =
            new System.Random(
                randomSeed); // 동일 Seed에서 동일 위치 선택

        Vector3Int selectedCell =
            candidates[
                random.Next(
                    candidates.Count)]; // 안전한 방 내부 타일 선택

        Vector3 selectedWorldPosition =
            floorTilemap.GetCellCenterWorld(
                selectedCell); // 선택 타일 중심 World 좌표 계산

        worldPosition =
            new Vector2(
                selectedWorldPosition.x,
                selectedWorldPosition.y); // 조우 World 위치 반환

        return true;
    }

    internal bool TryGetWalkableCell(Vector2 worldPosition, out Vector3Int cell) // 실측 월드 위치를 이동 가능한 바닥 셀로 변환
    { // 시야와 추격의 바닥 셀 조회 시작
        cell = floorTilemap == null ? Vector3Int.zero : floorTilemap.WorldToCell(worldPosition); // 현재 층의 실제 타일 좌표
        return floorTilemap != null && IsWalkableCell(cell); // 현재 타일과 실제 바닥 여부 확인
    } // 시야와 추격의 바닥 셀 조회 종료

    internal bool IsWalkableCell(Vector3Int cell) // 시야와 경로의 공용 통행 가능 범위
    { // 바닥 타일 셀 확인 시작
        return floorCells.Contains(cell); // 자동 연결 통로를 포함하는 실제 이동 바닥 확인
    } // 바닥 타일 셀 확인 종료

    internal bool CanUseBattleReturnPosition(Vector2 playerPosition, Vector2 candidate) // 전투 뒤 발판과 플레이어 폭 확인
    { // 복귀 지점 충돌 검사 시작
        const float playerHalfWidth = 0.275f; // 탐사 플레이어 상자 충돌 범위의 절반
        Vector2 diagonal = new Vector2(playerHalfWidth, playerHalfWidth); // 왼쪽 아래와 오른쪽 위 검사 오프셋
        Vector2 opposite = new Vector2(playerHalfWidth, -playerHalfWidth); // 왼쪽 위와 오른쪽 아래 검사 오프셋
        return TryGetWalkableCell(playerPosition, out Vector3Int from) // 접촉 직전에 있던 실제 바닥 셀
            && TryGetWalkableCell(candidate, out Vector3Int to) // 조우를 피한 복귀 바닥 셀
            && ExplorationPursuitGrid.HasLineOfSight(from, to, IsWalkableCell) // 모서리와 벽을 넘지 않는 복귀 이동
            && TryGetWalkableCell(candidate + diagonal, out _) && TryGetWalkableCell(candidate - diagonal, out _) // 첫 대각 모서리의 바닥 확인
            && TryGetWalkableCell(candidate + opposite, out _) && TryGetWalkableCell(candidate - opposite, out _); // 나머지 대각 모서리의 바닥 확인
    } // 복귀 지점 충돌 검사 종료

    internal Vector2 GetWalkableCellCenter(Vector3Int cell) // 추격이 가로지를 다음 바닥 타일의 중심
    { // 실제 월드 타일 중심 계산 시작
        return floorTilemap.GetCellCenterWorld(cell); // 맵의 실제 그리드 및 변환을 반영한 세계 좌표
    } // 실제 월드 타일 중심 계산 종료

    public Vector2 GetLogicalPosition(
        Vector3 worldPosition) // World 위치를 미니맵용 연속 논리 위치로 변환
    {
        EnsureTilemapHierarchy(); // Tilemap 존재 보장

        Vector3Int tileCell =
            floorTilemap.WorldToCell(
                worldPosition); // 플레이어 World 위치의 Tile 셀 계산

        return new Vector2(
            tileCell.x /
            (float)RoomSpacing,
            tileCell.y /
            (float)RoomSpacing); // 방과 통로 사이 이동을 포함한 논리 위치 반환
    }

    public bool TryGetRoomCoordinateAtWorldPosition(
        Vector3 worldPosition,
        out Vector2Int logicalCoordinate) // World 위치가 실제로 속한 방 좌표 조회
    {
        logicalCoordinate = Vector2Int.zero; // 실패 기본 방 좌표 지정

        EnsureTilemapHierarchy(); // Tilemap 존재 보장

        Vector3Int tileCell =
            floorTilemap.WorldToCell(
                worldPosition); // 현재 World 위치의 Tile 셀 계산

        foreach (KeyValuePair<Vector2Int, HashSet<Vector3Int>> pair in roomFloorCells)
        {
            if (pair.Value == null ||
                !pair.Value.Contains(tileCell))
            {
                continue;
            }

            logicalCoordinate =
                pair.Key; // 실제 방 Floor에 포함된 논리 좌표 반환

            return true;
        }

        return false; // 통로나 방 외부 위치 반환
    }

    public bool TryGetRoomFloorWorldPositions(
        Vector2Int logicalCoordinate,
        List<Vector2> worldPositions) // 지정 방 Floor 타일 중심 World 위치 목록 조회
    {
        if (worldPositions == null)
        {
            return false;
        }

        worldPositions.Clear(); // 기존 결과 목록 초기화

        EnsureTilemapHierarchy(); // Tilemap 존재 보장

        if (!roomFloorCells.TryGetValue(
                logicalCoordinate,
                out HashSet<Vector3Int> sourceCells) ||
            sourceCells == null ||
            sourceCells.Count == 0)
        {
            return false;
        }

        List<Vector3Int> orderedCells =
            new List<Vector3Int>(
                sourceCells); // 안정된 출력 순서용 복사

        orderedCells.Sort(
            CompareTileCells); // 타일 좌표 순서 고정

        foreach (Vector3Int cell in orderedCells)
        {
            Vector3 worldPosition =
                floorTilemap.GetCellCenterWorld(
                    cell); // Floor 타일 중심 World 위치 계산

            worldPositions.Add(
                new Vector2(
                    worldPosition.x,
                    worldPosition.y)); // 2D World 위치 목록 등록
        }

        return worldPositions.Count > 0; // 실제 Floor 위치 존재 여부 반환
    }

    private void FillRoom(
        ExplorationMapCell cell,
        int mapSeed) // 논리 셀 하나를 Seed 기반 다양한 형태의 방으로 생성
    {
        Vector3Int center =
            GetRoomCenterCell(
                cell.Coordinate); // 방 중심 셀 계산

        RoomShape shape =
            SelectRoomShape(
                cell,
                mapSeed,
                out int rotation); // 방 종류와 방향 결정

        HashSet<Vector3Int> localRoomCells =
            new HashSet<Vector3Int>(); // 현재 방 전용 Floor 셀 집합

        int halfSize =
            RoomSize / 2; // 방 절반 크기 계산

        for (int x = -halfSize;
             x <= halfSize;
             x++)
        {
            for (int y = -halfSize;
                 y <= halfSize;
                 y++)
            {
                Vector2Int rotatedCoordinate =
                    RotateLocalCoordinate(
                        new Vector2Int(x, y),
                        rotation); // 형태 방향에 맞춰 로컬 좌표 회전

                if (!ShouldFillRoomCell(
                        shape,
                        rotatedCoordinate.x,
                        rotatedCoordinate.y,
                        halfSize))
                {
                    continue;
                }

                localRoomCells.Add(
                    new Vector3Int(
                        center.x + x,
                        center.y + y,
                        0)); // 형태 내부 Floor 등록
            }
        }

        EnsureRoomCenter(
            center,
            localRoomCells); // 플레이어·계단 배치를 위한 중앙 Floor 보장

        EnsureConnectionFloors(
            cell,
            center,
            localRoomCells); // 연결된 통로 방향의 방 내부 이동로 보장

        foreach (Vector3Int roomCell in localRoomCells)
        {
            floorCells.Add(
                roomCell); // 전체 Floor 집합에 현재 방 등록
        }

        roomFloorCells[cell.Coordinate] =
            localRoomCells; // 방별 실제 Floor 셀 저장

        roomData[cell.Coordinate] =
            cell; // 방별 연결 정보 저장

    }

    private static RoomShape SelectRoomShape(
        ExplorationMapCell cell,
        int mapSeed,
        out int rotation) // 셀 종류별 Seed 기반 방 형태 선택
    {
        int shapeSeed =
            CreateDeterministicSeed(
                mapSeed,
                cell.Coordinate,
                104729); // 방 형태 전용 Seed 생성

        System.Random random =
            new System.Random(
                shapeSeed); // 방 형태 난수 생성기 준비

        rotation =
            random.Next(4); // L·T형 방향용 0~3 회전 선택

        if (cell.Type == ExplorationCellType.Start)
        {
            rotation = 0; // 시작 방 회전 미사용
            return RoomShape.Square; // 시작 방은 안정적인 정사각형 사용
        }

        if (cell.Type == ExplorationCellType.Stairs)
        {
            rotation = 0; // 계단 방은 단순 형태만 사용

            int stairsShape =
                random.Next(3); // 계단 방용 안전 형태 선택

            if (stairsShape == 1)
            {
                return RoomShape.Horizontal; // 가로형 계단 방 반환
            }

            if (stairsShape == 2)
            {
                return RoomShape.Vertical; // 세로형 계단 방 반환
            }

            return RoomShape.Square; // 기본 정사각형 계단 방 반환
        }

        return (RoomShape)random.Next(6); // 일반 방은 여섯 형태 중 하나 선택
    }

    private static bool ShouldFillRoomCell(
        RoomShape shape,
        int x,
        int y,
        int halfSize) // 방 형태별 로컬 Floor 여부 판정
    {
        switch (shape)
        {
            case RoomShape.Horizontal:
                return Mathf.Abs(x) <= halfSize &&
                       Mathf.Abs(y) <= halfSize - 1; // 가로 직사각형 판정

            case RoomShape.Vertical:
                return Mathf.Abs(x) <= halfSize - 1 &&
                       Mathf.Abs(y) <= halfSize; // 세로 직사각형 판정

            case RoomShape.LShape:
                return x <= 0 ||
                       y <= 0; // 한 사분면이 비어 있는 L자형 판정

            case RoomShape.TShape:
                return y >= 1 ||
                       Mathf.Abs(x) <= 1; // 상단 가로부와 중앙 세로부 T자형 판정

            case RoomShape.Cross:
                return Mathf.Abs(x) <= 1 ||
                       Mathf.Abs(y) <= 1; // 중앙 십자형 판정

            default:
                return Mathf.Abs(x) <= halfSize &&
                       Mathf.Abs(y) <= halfSize; // 정사각형 판정
        }
    }

    private static Vector2Int RotateLocalCoordinate(
        Vector2Int coordinate,
        int rotation) // 로컬 방 좌표 90도 단위 회전
    {
        switch (rotation & 3)
        {
            case 1:
                return new Vector2Int(
                    coordinate.y,
                    -coordinate.x); // 시계 방향 90도 회전

            case 2:
                return new Vector2Int(
                    -coordinate.x,
                    -coordinate.y); // 180도 회전

            case 3:
                return new Vector2Int(
                    -coordinate.y,
                    coordinate.x); // 시계 방향 270도 회전

            default:
                return coordinate; // 회전 없음 반환
        }
    }

    private static void EnsureRoomCenter(
        Vector3Int center,
        HashSet<Vector3Int> roomCells) // 방 중앙의 최소 이동 공간 보장
    {
        for (int x = -1;
             x <= 1;
             x++)
        {
            for (int y = -1;
                 y <= 1;
                 y++)
            {
                roomCells.Add(
                    new Vector3Int(
                        center.x + x,
                        center.y + y,
                        0)); // 중앙 3x3 이동 공간 보장
            }
        }
    }

    private static void EnsureConnectionFloors(
        ExplorationMapCell cell,
        Vector3Int center,
        HashSet<Vector3Int> roomCells) // 연결된 방향의 방 내부 출입로 보장
    {
        if (cell.ConnectedUp)
        {
            CarveRoomConnection(
                center,
                Vector2Int.up,
                roomCells); // 위쪽 연결로 생성
        }

        if (cell.ConnectedRight)
        {
            CarveRoomConnection(
                center,
                Vector2Int.right,
                roomCells); // 오른쪽 연결로 생성
        }

        if (cell.ConnectedDown)
        {
            CarveRoomConnection(
                center,
                Vector2Int.down,
                roomCells); // 아래쪽 연결로 생성
        }

        if (cell.ConnectedLeft)
        {
            CarveRoomConnection(
                center,
                Vector2Int.left,
                roomCells); // 왼쪽 연결로 생성
        }
    }

    private static void CarveRoomConnection(
        Vector3Int center,
        Vector2Int direction,
        HashSet<Vector3Int> roomCells) // 방 중심에서 가장자리까지 3칸 폭 출입로 생성
    {
        int halfSize =
            RoomSize / 2; // 방 가장자리 거리 계산

        Vector2Int perpendicular =
            new Vector2Int(
                -direction.y,
                direction.x); // 통로 너비 방향 계산

        for (int distance = 0;
             distance <= halfSize;
             distance++)
        {
            for (int width = -CorridorHalfWidth;
                 width <= CorridorHalfWidth;
                 width++)
            {
                Vector2Int localOffset =
                    direction * distance +
                    perpendicular * width; // 출입로 로컬 위치 계산

                roomCells.Add(
                    new Vector3Int(
                        center.x + localOffset.x,
                        center.y + localOffset.y,
                        0)); // 출입로 Floor 등록
            }
        }
    }

    private List<Vector3Int> BuildSafeSpawnCandidates(
        Vector2Int logicalCoordinate,
        HashSet<Vector3Int> sourceCells) // 벽·문·방 중앙을 피한 조우 배치 후보 생성
    {
        List<Vector3Int> candidates =
            new List<Vector3Int>(); // 안전 후보 목록 생성

        if (!roomData.TryGetValue(
                logicalCoordinate,
                out ExplorationMapCell cell))
        {
            return candidates;
        }

        Vector3Int center =
            GetRoomCenterCell(
                logicalCoordinate); // 방 중심 셀 계산

        foreach (Vector3Int candidate in sourceCells)
        {
            if (!HasCardinalFloorPadding(
                    candidate,
                    sourceCells))
            {
                continue; // 벽 바로 옆 타일 제외
            }

            Vector3Int local =
                candidate - center; // 방 중심 기준 로컬 위치 계산

            if (Mathf.Abs(local.x) +
                Mathf.Abs(local.y) <= 1)
            {
                continue; // 방 중앙 주변 고정 배치 방지
            }

            if (IsNearConnectedDoor(
                    local,
                    cell))
            {
                continue; // 출입구 근처 후보 제외
            }

            candidates.Add(
                candidate); // 안전 후보 등록
        }

        return candidates; // 안전 후보 반환
    }

    private List<Vector3Int> BuildFallbackSpawnCandidates(
        Vector2Int logicalCoordinate,
        HashSet<Vector3Int> sourceCells) // 좁은 방용 완화 조우 후보 생성
    {
        List<Vector3Int> candidates =
            new List<Vector3Int>(); // 완화 후보 목록 생성

        Vector3Int center =
            GetRoomCenterCell(
                logicalCoordinate); // 방 중심 셀 계산

        foreach (Vector3Int candidate in sourceCells)
        {
            Vector3Int local =
                candidate - center; // 방 중심 기준 로컬 위치 계산

            if (local == Vector3Int.zero)
            {
                continue; // 가능하면 정확한 중앙 제외
            }

            candidates.Add(
                candidate); // 좁은 형태의 일반 Floor 후보 등록
        }

        if (candidates.Count == 0)
        {
            candidates.AddRange(
                sourceCells); // 최후에는 방 내부 Floor 전체 사용
        }

        return candidates; // 완화 후보 반환
    }

    private static bool HasCardinalFloorPadding(
        Vector3Int candidate,
        HashSet<Vector3Int> sourceCells) // 후보 주변 상하좌우 Floor 여유 확인
    {
        foreach (Vector3Int direction in CardinalDirections)
        {
            if (!sourceCells.Contains(
                    candidate + direction))
            {
                return false; // 한 방향이라도 벽이면 안전 후보 제외
            }
        }

        return true; // 상하좌우 한 칸 이상 Floor 보장
    }

    private static bool IsNearConnectedDoor(
        Vector3Int local,
        ExplorationMapCell cell) // 연결 통로 근처 조우 배치 금지 판정
    {
        int halfSize =
            RoomSize / 2; // 방 반경 계산

        int doorStart =
            halfSize - SpawnDoorClearance; // 출입구 보호 시작 거리 계산

        if (cell.ConnectedUp &&
            local.y >= doorStart &&
            Mathf.Abs(local.x) <= CorridorHalfWidth + 1)
        {
            return true; // 위쪽 출입구 주변 제외
        }

        if (cell.ConnectedRight &&
            local.x >= doorStart &&
            Mathf.Abs(local.y) <= CorridorHalfWidth + 1)
        {
            return true; // 오른쪽 출입구 주변 제외
        }

        if (cell.ConnectedDown &&
            local.y <= -doorStart &&
            Mathf.Abs(local.x) <= CorridorHalfWidth + 1)
        {
            return true; // 아래쪽 출입구 주변 제외
        }

        if (cell.ConnectedLeft &&
            local.x <= -doorStart &&
            Mathf.Abs(local.y) <= CorridorHalfWidth + 1)
        {
            return true; // 왼쪽 출입구 주변 제외
        }

        return false; // 출입구 보호 구역 아님 반환
    }

    private static int CompareTileCells(
        Vector3Int left,
        Vector3Int right) // 조우 후보 순서를 좌표 기준으로 고정
    {
        int xComparison =
            left.x.CompareTo(
                right.x); // X 좌표 우선 비교

        if (xComparison != 0)
        {
            return xComparison; // X가 다르면 비교 결과 반환
        }

        int yComparison =
            left.y.CompareTo(
                right.y); // Y 좌표 비교

        if (yComparison != 0)
        {
            return yComparison; // Y가 다르면 비교 결과 반환
        }

        return left.z.CompareTo(
            right.z); // 마지막 Z 좌표 비교
    }

    private static int CreateDeterministicSeed(
        int mapSeed,
        Vector2Int coordinate,
        int salt) // 맵 Seed와 방 좌표 조합 Seed 생성
    {
        return unchecked(
            mapSeed * 397 ^
            coordinate.x * 73856093 ^
            coordinate.y * 19349663 ^
            salt); // 프로세스와 무관한 고정 정수 조합 반환
    }

    private void FillHorizontalCorridor(
        Vector2Int logicalCoordinate) // 오른쪽 방으로 이어지는 통로 생성
    {
        Vector3Int start =
            GetRoomCenterCell(
                logicalCoordinate); // 현재 방 중심 계산

        for (int x = 0;
             x <= RoomSpacing;
             x++)
        {
            for (int y = -CorridorHalfWidth;
                 y <= CorridorHalfWidth;
                 y++)
            {
                floorCells.Add(
                    new Vector3Int(
                        start.x + x,
                        start.y + y,
                        0)); // 가로 통로 Floor 등록
            }
        }
    }

    private void FillVerticalCorridor(
        Vector2Int logicalCoordinate) // 위쪽 방으로 이어지는 통로 생성
    {
        Vector3Int start =
            GetRoomCenterCell(
                logicalCoordinate); // 현재 방 중심 계산

        for (int y = 0;
             y <= RoomSpacing;
             y++)
        {
            for (int x = -CorridorHalfWidth;
                 x <= CorridorHalfWidth;
                 x++)
            {
                floorCells.Add(
                    new Vector3Int(
                        start.x + x,
                        start.y + y,
                        0)); // 세로 통로 Floor 등록
            }
        }
    }

    private void BuildWalls() // Floor 전체 외곽에 실제 Wall 생성
    {
        wallCells.Clear(); // 이전 Wall 셀 초기화

        foreach (Vector3Int floorCell in floorCells)
        {
            foreach (Vector3Int direction in WallDirections)
            {
                Vector3Int wallCell =
                    floorCell +
                    direction; // Floor 주변 Wall 후보 계산

                if (!floorCells.Contains(wallCell))
                {
                    wallCells.Add(wallCell); // Floor가 아닌 외곽 셀 Wall 등록
                }
            }
        }
    }

    private static Vector3Int GetRoomCenterCell(
        Vector2Int logicalCoordinate) // 논리 좌표를 Tilemap 방 중심 셀로 변환
    {
        return new Vector3Int(
            logicalCoordinate.x *
            RoomSpacing,
            logicalCoordinate.y *
            RoomSpacing,
            0); // 고정 간격 기준 방 중심 셀 반환
    }

    private void EnsureTilemapHierarchy() // Grid와 Tilemap 런타임 계층 생성
    {
        if (floorTilemap != null &&
            wallTilemap != null &&
            wallEdgeTilemap != null && // 같은 벽 테두리 오브젝트 재사용 확인
            decorationTilemap != null && // 같은 장식 오브젝트 재사용 확인
            wallTilemapCollider != null &&
            wallCompositeCollider != null)
        {
            return;
        }

        gridObject =
            new GameObject(
                "ExplorationTilemapGrid",
                typeof(Grid)); // Grid 오브젝트 생성

        gridObject.transform.SetParent(
            transform,
            false); // 맵 런타임 하위 배치

        Grid grid =
            gridObject.GetComponent<Grid>(); // Grid 컴포넌트 조회

        grid.cellSize =
            Vector3.one; // 1x1 타일 단위 설정

        GameObject floorObject =
            new GameObject(
                "Floor",
                typeof(Tilemap),
                typeof(TilemapRenderer)); // Floor Tilemap 오브젝트 생성

        floorObject.transform.SetParent(
            gridObject.transform,
            false); // Grid 하위 배치

        floorTilemap =
            floorObject.GetComponent<Tilemap>(); // Floor Tilemap 조회

        TilemapRenderer floorRenderer =
            floorObject.GetComponent<TilemapRenderer>(); // Floor Renderer 조회

        floorRenderer.sortingOrder = -10; // 플레이어와 조우 뒤에 Floor 표시

        GameObject wallObject =
            new GameObject(
                "Wall",
                typeof(Rigidbody2D),
                typeof(Tilemap),
                typeof(TilemapRenderer),
                typeof(TilemapCollider2D),
                typeof(CompositeCollider2D)); // 실제 충돌 Wall Tilemap 생성

        wallObject.transform.SetParent(
            gridObject.transform,
            false); // Grid 하위 배치

        Rigidbody2D wallBody =
            wallObject.GetComponent<Rigidbody2D>(); // Wall Rigidbody2D 조회

        wallBody.bodyType =
            RigidbodyType2D.Static; // 움직이지 않는 정적 벽 설정

        wallTilemap =
            wallObject.GetComponent<Tilemap>(); // Wall Tilemap 조회

        TilemapRenderer wallRenderer =
            wallObject.GetComponent<TilemapRenderer>(); // Wall Renderer 조회

        wallRenderer.sortingOrder = -9; // Floor보다 위에 Wall 표시

        wallTilemapCollider =
            wallObject.GetComponent<TilemapCollider2D>(); // Wall TilemapCollider2D 조회

        wallTilemapCollider.compositeOperation =
            Collider2D.CompositeOperation.Merge; // 인접 Wall 충돌을 Composite로 합성

        wallCompositeCollider =
            wallObject.GetComponent<CompositeCollider2D>(); // Wall CompositeCollider2D 조회

        wallCompositeCollider.geometryType =
            CompositeCollider2D.GeometryType.Polygons; // 벽을 채워진 충돌 영역으로 생성

        wallCompositeCollider.generationType =
            CompositeCollider2D.GenerationType.Synchronous; // Tile 변경 시 Collider 즉시 갱신

        wallCompositeCollider.isTrigger =
            false; // 실제 물리 충돌 활성화
        wallEdgeTilemap = CreateVisualLayer("WallEdges", -8); // 재질 벽 위에 이어지는 돌 테두리
        decorationTilemap = CreateVisualLayer("Decoration", -7); // 플레이어와 조우 뒤쪽의 환경 장식
    }

    private Tilemap CreateVisualLayer(string layerName, int sortOrder) // 물리 충돌 없는 탐사 그림 레이어 생성
    { // 장식 타일맵 생성 시작
        var layer = new GameObject(layerName, typeof(Tilemap), typeof(TilemapRenderer)); // 그림과 렌더러만 있는 타일맵 객체
        layer.transform.SetParent(gridObject.transform, false); // 바닥과 벽이 쓰는 같은 격자에 배치
        layer.GetComponent<TilemapRenderer>().sortingOrder = sortOrder; // 통로와 캐릭터 사이의 표시 순서 지정
        return layer.GetComponent<Tilemap>(); // 실제 테두리나 장식에 사용할 타일맵 반환
    } // 장식 타일맵 생성 종료

    private void RefreshWallCollider() // Wall Tile 변경 사항을 물리에 즉시 반영
    {
        if (wallTilemapCollider == null)
        {
            return;
        }

        if (wallTilemapCollider.hasTilemapChanges)
        {
            wallTilemapCollider.ProcessTilemapChanges(); // 추가·삭제 Wall Collider 즉시 재생성
        }
    }

    private void OnDestroy() // 런타임 Tile 리소스 정리
    { // 씬 종료 시 타일 정리 시작
        dungeonTiles?.Release(); // 이 씬에서 만든 타일과 스프라이트만 제거
    } // 씬 종료 시 타일 정리 종료
}
