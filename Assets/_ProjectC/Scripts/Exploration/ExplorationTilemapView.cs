using System.Collections.Generic; // Floor와 Wall 셀 집합 사용
using UnityEngine; // Grid와 2D 물리 기능 사용
using UnityEngine.Tilemaps; // Unity Tilemap 기능 사용

public sealed class ExplorationTilemapView : MonoBehaviour // 논리 맵을 충돌 가능한 Tilemap으로 표시
{
    public const int RoomSize = 7; // 한 논리 셀의 방 크기
    public const int RoomSpacing = 10; // 논리 셀 사이 방 중심 간격
    private const int CorridorHalfWidth = 1; // 통로 절반 폭

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

    private readonly HashSet<Vector3Int> floorCells =
        new HashSet<Vector3Int>(); // 현재 Floor 셀 집합

    private readonly HashSet<Vector3Int> wallCells =
        new HashSet<Vector3Int>(); // 현재 Wall 셀 집합

    private GameObject gridObject; // 런타임 Grid 오브젝트
    private Tilemap floorTilemap; // 실제 이동 공간 Floor Tilemap
    private Tilemap wallTilemap; // 실제 충돌 Wall Tilemap
    private TilemapCollider2D wallTilemapCollider; // Wall 타일 충돌 생성기
    private CompositeCollider2D wallCompositeCollider; // Wall 충돌 합성기
    private Tile floorTile; // 런타임 Floor Tile
    private Tile wallTile; // 런타임 Wall Tile
    private Sprite runtimeSquareSprite; // 런타임 타일 Sprite

    public int FloorTileCount => floorCells.Count; // 현재 Floor 타일 개수
    public int WallTileCount => wallCells.Count; // 현재 Wall 타일 개수

    public void Build(
        ExplorationMapData mapData) // 논리 맵을 실제 충돌 Tilemap으로 변환
    {
        EnsureTilemapHierarchy(); // Grid와 Tilemap 계층 준비
        EnsureRuntimeTiles(); // 런타임 타일 준비

        floorTilemap.ClearAllTiles(); // 이전 Floor 제거
        wallTilemap.ClearAllTiles(); // 이전 Wall 제거
        floorCells.Clear(); // Floor 셀 목록 초기화
        wallCells.Clear(); // Wall 셀 목록 초기화

        if (mapData == null ||
            mapData.Cells == null ||
            mapData.Cells.Count == 0)
        {
            RefreshWallCollider(); // 빈 맵 Collider 정리
            return;
        }

        foreach (ExplorationMapCell cell in mapData.Cells)
        {
            FillRoom(cell.Coordinate); // 각 논리 셀에 7x7 방 생성

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
            floorTilemap.SetTile(
                floorCell,
                floorTile); // Floor Tilemap에 실제 타일 배치
        }

        BuildWalls(); // 전체 Floor 외곽에 실제 Wall 생성

        foreach (Vector3Int wallCell in wallCells)
        {
            wallTilemap.SetTile(
                wallCell,
                wallTile); // Wall Tilemap에 충돌 타일 배치
        }

        floorTilemap.CompressBounds(); // Floor 사용 영역 정리
        wallTilemap.CompressBounds(); // Wall 사용 영역 정리
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

    private void FillRoom(
        Vector2Int logicalCoordinate) // 논리 셀 하나를 7x7 방으로 생성
    {
        Vector3Int center =
            GetRoomCenterCell(
                logicalCoordinate); // 방 중심 셀 계산

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
                floorCells.Add(
                    new Vector3Int(
                        center.x + x,
                        center.y + y,
                        0)); // 방 내부 Floor 등록
            }
        }
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
    }

    private void EnsureRuntimeTiles() // 런타임 Floor와 Wall 타일 생성
    {
        if (floorTile != null &&
            wallTile != null)
        {
            return;
        }

        if (runtimeSquareSprite == null)
        {
            runtimeSquareSprite =
                Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(
                        0f,
                        0f,
                        1f,
                        1f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    1f); // 런타임 1x1 흰색 Sprite 생성
        }

        floorTile =
            ScriptableObject.CreateInstance<Tile>(); // Floor Tile 생성

        floorTile.name =
            "RuntimeExplorationFloorTile"; // Floor Tile 이름 지정

        floorTile.sprite =
            runtimeSquareSprite; // Floor Sprite 지정

        floorTile.color =
            new Color(
                0.12f,
                0.20f,
                0.28f,
                1f); // Floor 임시 색상 지정

        floorTile.colliderType =
            Tile.ColliderType.None; // Floor 자체는 충돌하지 않음

        wallTile =
            ScriptableObject.CreateInstance<Tile>(); // Wall Tile 생성

        wallTile.name =
            "RuntimeExplorationWallTile"; // Wall Tile 이름 지정

        wallTile.sprite =
            runtimeSquareSprite; // Wall Sprite 지정

        wallTile.color =
            new Color(
                0.05f,
                0.07f,
                0.10f,
                1f); // 실제 Wall 임시 색상 지정

        wallTile.colliderType =
            Tile.ColliderType.Grid; // Grid 셀 전체를 실제 벽 충돌로 사용
    }

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
    {
        if (floorTile != null)
        {
            Destroy(floorTile); // 런타임 Floor Tile 제거
        }

        if (wallTile != null)
        {
            Destroy(wallTile); // 런타임 Wall Tile 제거
        }

        if (runtimeSquareSprite != null)
        {
            Destroy(runtimeSquareSprite); // 런타임 Sprite 제거
        }
    }
}
