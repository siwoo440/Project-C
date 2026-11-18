using System.Collections; // 실제 탐사 화면 대기 사용
using NUnit.Framework; // 탐사 타일과 충돌 검사 사용
using UnityEngine; // 유니티 맵 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.Tilemaps; // 실제 탐사 타일맵 검사 사용
public sealed class ExplorationDungeonArtTests // 던전 바닥과 벽 이미지 검사
{ // 탐사 타일 이미지 검사 시작
    [UnityTest] // 실제 타일맵 검사 지정
    public IEnumerator GeneratedMapUsesDistinctTilesAndKeepsWallCollision() // 생성 맵의 타일 이미지와 벽 충돌 확인
    { // 탐사 타일 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 플레이 시작
        ExplorationMapData map = ExplorationMapGenerator.Generate(14, 12345, 1); // 실제 절차 생성 탐사 맵 준비
        GameObject root = new GameObject("DungeonArtTest", typeof(ExplorationTilemapView)); // 실제 탐사 타일맵 생성
        ExplorationTilemapView view = root.GetComponent<ExplorationTilemapView>(); // 실제 타일맵 화면 조회
        view.Build(map); // 실제 탐사 바닥과 벽 생성
        Assert.Greater(view.FloorTileCount, 0); // 생성된 실제 바닥 존재 확인
        Assert.Greater(view.WallTileCount, 0); // 생성된 실제 벽 존재 확인
        Tilemap[] maps = root.GetComponentsInChildren<Tilemap>(); // 실제 바닥과 벽 타일맵 조회
        Assert.AreEqual(2, maps.Length); // 바닥과 벽 타일맵 두 개 확인
        Tilemap floor = System.Array.Find(maps, item => item.name == "Floor"); // 바닥 타일맵 조회
        Tilemap wall = System.Array.Find(maps, item => item.name == "Wall"); // 벽 타일맵 조회
        Assert.IsNotNull(floor); // 바닥 타일맵 존재 확인
        Assert.IsNotNull(wall); // 벽 타일맵 존재 확인
        Tile floorTile = FindTile(floor); // 실제 바닥 타일 조회
        Tile wallTile = FindTile(wall); // 실제 벽 타일 조회
        Assert.IsNotNull(floorTile); // 실제 바닥 타일 존재 확인
        Assert.IsNotNull(wallTile); // 실제 벽 타일 존재 확인
        Assert.AreEqual("DungeonFloor", floorTile.sprite.texture.name); // 생성한 바닥 이미지 적용 확인
        Assert.AreEqual("DungeonWall", wallTile.sprite.texture.name); // 생성한 벽 이미지 적용 확인
        Assert.AreEqual(Color.white, floorTile.color); // 바닥 이미지 원본 색상 유지 확인
        Assert.AreEqual(Color.white, wallTile.color); // 벽 이미지 원본 색상 유지 확인
        Assert.AreEqual(Tile.ColliderType.None, floorTile.colliderType); // 바닥 이동 허용 확인
        Assert.AreEqual(Tile.ColliderType.Grid, wallTile.colliderType); // 벽 전체 충돌 유지 확인
        Assert.IsNotNull(wall.GetComponent<TilemapCollider2D>()); // 실제 벽 타일 충돌기 확인
        Assert.IsNotNull(wall.GetComponent<CompositeCollider2D>()); // 실제 연결 벽 충돌기 확인
        Assert.AreEqual(1f, floorTile.sprite.bounds.size.x, 0.01f); // 바닥 이미지 한 칸 너비 확인
        Assert.AreEqual(1f, wallTile.sprite.bounds.size.y, 0.01f); // 벽 이미지 한 칸 높이 확인
        Object.DestroyImmediate(root); // 테스트 탐사 맵 제거
        yield return new ExitPlayMode(); // 실제 탐사 플레이 종료
    } // 탐사 타일 검사 종료
    private static Tile FindTile(Tilemap map) // 타일맵에서 실제 타일 하나 조회
    { // 실제 타일 조회 시작
        foreach (Vector3Int position in map.cellBounds.allPositionsWithin) // 사용 타일 범위 순회
        { // 개별 타일 조회 시작
            Tile tile = map.GetTile<Tile>(position); // 현재 좌표의 실제 타일 조회
            if (tile != null) // 유효 타일 존재 확인
            { // 유효 타일 반환 시작
                return tile; // 찾은 실제 타일 반환
            } // 유효 타일 반환 종료
        } // 개별 타일 조회 종료
        return null; // 실제 타일 없음 반환
    } // 실제 타일 조회 종료
} // 탐사 타일 이미지 검사 종료
