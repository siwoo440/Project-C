using System.Collections.Generic; // 생성된 Floor 셀 목록 사용
using UnityEngine; // Grid와 런타임 Sprite 기능 사용
using UnityEngine.Tilemaps; // Unity Tilemap 기능 사용

public sealed class ExplorationTilemapView : MonoBehaviour // 논리 맵을 실제 Tilemap으로 표시
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
    }; // Floor 주변 Wall Preview 탐색 방향

    private readonly HashSet<Vector3Int> floorCells =
        new HashSet<Vector3Int>(); // 현재 Floor 셀 집합

    private GameObject gridObject; // 런타임 Grid 오브젝트
    private Tilemap floorTilemap; // 실제 이동 공간 Floor Tilemap
    private Tilemap wallTilemap; // 외곽 표시용 Wall Preview Tilemap
    private Tile floorTile; // 런타임 Floor Tile
    private Tile wallTile; // 런타임 Wall Preview Tile
    private Sprite runtimeSquareSprite; // 런타임 타일 Sprite

    public int FloorTileCount => floorCells.Count; // 현재 Floor 타일 개수
    public int WallTileCount { get; private set; } // 현재 Wall Preview 타일 개수

    public void Build(
        ExplorationMapData mapData) // 논리 맵을 Tilemap으로 변환
    {
        EnsureTilemapHierarchy(); // Grid와 Tilemap 계층 준비
        EnsureRuntimeTiles(); // 런타임 타일 준비

        floorTilemap.ClearAllTiles(); // 이전 Floor 제거
        wallTilemap.ClearAllTiles(); // 이전 Wall Preview 제거
        floorCells.Clear(); // Floor 셀 목록 초기화
        WallTileCount = 0; // Wall Preview 개수 초기화

        if (mapData == null ||
            mapData.Cells == null ||
            mapData.Cells.Count == 0)
        {
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

        BuildWallPreview(); // Floor 바깥쪽 Wall Preview 생성
        floorTilemap.CompressBounds(); // Floor 사용 영역 정리
        wallTilemap.CompressBounds(); // Wall 사용 영역 정리
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

    private void BuildWallPreview() // Floor 외곽에 충돌 없는 Wall Preview 생성
    {
        HashSet<Vector3Int> wallCells =
            new HashSet<Vector3Int>(); // 중복 없는 Wall Preview 셀 목록

        foreach (Vector3Int floorCell in floorCells)
        {
            foreach (Vector3Int direction in WallDirections)
            {
                Vector3Int wallCell =
                    floorCell +
                    direction; // Floor 주변 후보 셀 계산

                if (!floorCells.Contains(wallCell))
                {
                    wallCells.Add(wallCell); // Floor가 아닌 주변 셀 Wall 등록
                }
            }
        }

        foreach (Vector3Int wallCell in wallCells)
        {
            wallTilemap.SetTile(
                wallCell,
                wallTile); // Wall Preview 타일 배치
        }

        WallTileCount =
            wallCells.Count; // Wall Preview 개수 저장
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
            wallTilemap != null)
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
                "WallPreview",
                typeof(Tilemap),
                typeof(TilemapRenderer)); // Wall Preview Tilemap 오브젝트 생성

        wallObject.transform.SetParent(
            gridObject.transform,
            false); // Grid 하위 배치

        wallTilemap =
            wallObject.GetComponent<Tilemap>(); // Wall Tilemap 조회

        TilemapRenderer wallRenderer =
            wallObject.GetComponent<TilemapRenderer>(); // Wall Renderer 조회

        wallRenderer.sortingOrder = -9; // Floor보다 위에 Wall Preview 표시
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
            Tile.ColliderType.None; // 40일차 Floor 충돌 비활성화

        wallTile =
            ScriptableObject.CreateInstance<Tile>(); // Wall Preview Tile 생성

        wallTile.name =
            "RuntimeExplorationWallPreviewTile"; // Wall Tile 이름 지정

        wallTile.sprite =
            runtimeSquareSprite; // Wall Sprite 지정

        wallTile.color =
            new Color(
                0.05f,
                0.07f,
                0.10f,
                1f); // Wall Preview 임시 색상 지정

        wallTile.colliderType =
            Tile.ColliderType.None; // 41일차 전까지 Wall 충돌 비활성화
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
