using System.Collections; // 실제 탐사 화면 대기 사용
using System.Collections.Generic; // 층 타일 이름과 좌표 보관
using System.Linq; // 재질과 좌표 배치의 차이 비교
using System.Reflection; // 검증 어셈블리에서 내부 벽 모양 계산 조회
using NUnit.Framework; // 탐사 타일과 충돌 검사 사용
using UnityEngine; // 유니티 맵 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.Tilemaps; // 실제 탐사 타일맵 검사 사용

public sealed class ExplorationDungeonArtTests // 던전 원화와 연결 벽 및 충돌 검사
{ // 탐사 타일 검사 시작
    [TestCase("DungeonFloorVariants", 4)] // 바닥 석판 편집용 조각 수
    [TestCase("DungeonWallVariants", 4)] // 석재 벽 편집용 조각 수
    [TestCase("DungeonDetails", 6)] // 벽과 바닥 환경 장식 조각 수
    [TestCase("DungeonWallEdges", 47)] // 연결 벽면과 무늬 없는 기본 조각 수
    public void AtlasExposesEachTileAtUnitSize(string assetName, int expected) // Unity 편집기 활용과 한 칸 너비 검증
    { // 타일 분할 검사 시작
        Sprite[] sprites = Resources.LoadAll<Sprite>("Exploration/" + assetName); // Unity가 읽는 실제 원화 조회
        Assert.AreEqual(expected, sprites.Length); // 개별 원화 누락 및 중복 검사
        Assert.AreEqual(expected, sprites.Select(sprite => sprite.name).Distinct().Count()); // 고유 스프라이트 이름 확인
        Assert.IsTrue(sprites.All(sprite => Mathf.Approximately(sprite.bounds.size.x, 1f) && Mathf.Approximately(sprite.bounds.size.y, 1f))); // 한 칸 크기 계약 확인
    } // 타일 분할 검사 종료

    [Test] // 방향과 모서리의 자동 연결 검증
    public void WallMaskSeparatesEdgesAndIndependentCorners() // 이웃 바닥이 벽면을 결정하는지 확인
    { // 벽 주변 셀 비교 시작
        var origin = Vector3Int.zero; // 가운데 벽 셀
        var type = typeof(ExplorationTilemapView).Assembly.GetType("ExplorationDungeonTiles"); // 실제 런타임의 내부 벽 계산 클래스
        var getMask = type?.GetMethod("GetWallMask", BindingFlags.Static | BindingFlags.NonPublic); // 내부 공개 범위를 유지한 실제 메서드 조회
        Assert.IsNotNull(getMask); // 실제 벽 모양 계산 메서드 연결 확인
        var floor = new HashSet<Vector3Int> { Vector3Int.up, Vector3Int.up + Vector3Int.right }; // 위쪽과 오른쪽 위의 바닥
        Assert.AreEqual(1, getMask.Invoke(null, new object[] { origin, floor })); // 위 직선과 겹치는 대각선 그림의 중복 방지
        floor.Add(Vector3Int.right); // 직각으로 이어지는 옆 바닥 추가
        Assert.AreEqual(3, getMask.Invoke(null, new object[] { origin, floor })); // 위와 오른쪽을 동시에 바라보는 벽면
        floor.Remove(Vector3Int.up); // 위쪽 직선 조건 해제
        floor.Remove(Vector3Int.right); // 오른쪽 직선 조건 해제
        Assert.AreEqual(16, getMask.Invoke(null, new object[] { origin, floor })); // 대각선에만 맞닿은 독립 벽 모서리
    } // 벽 주변 셀 비교 종료

    [UnityTest] // 실제 탐사 타일 배치와 충돌 검증
    public IEnumerator GeneratedMapUsesStableVariantsAndKeepsWallCollision() // 실제 시드와 이동 공간을 보존하는 여러 타일 검사
    { // 실제 탐사 장면 검사 시작
        yield return new EnterPlayMode(); // 렌더러와 물리를 실제 플레이 모드에서 실행
        VerifyGeneratedMap(); // 절차 맵 배치와 레이어의 부작용 검증
        yield return new ExitPlayMode(); // 검증용 탐사 종료
    } // 실제 탐사 장면 검사 종료

    private static void VerifyGeneratedMap() // 층을 여러 번 만드는 실제 타일맵 검사
    { // 같은 시드 배치 및 충돌 검사 시작
        const int seed = 12345; // 저장된 층을 재현할 시드
        var root = new GameObject("DungeonArtTest", typeof(ExplorationTilemapView)); // 독립된 실제 Tilemap 화면
        var view = root.GetComponent<ExplorationTilemapView>(); // 원본 게임의 타일맵 처리기
        Random.State state = Random.state; // 다른 게임 판정의 원래 난수 상태 기록
        try // 검사 결과와 무관하게 객체 정리 보장
        { // 배치와 충돌 검사 시작
            int next = Random.Range(0, 1000000); // 게임 판정의 다음 난수 저장
            Random.state = state; // 타일 생성 전 같은 난수 상태 복원
            view.Build(ExplorationMapGenerator.Generate(14, seed, 1)); // 실제 절차 맵 생성 및 원화 배치
            Assert.AreEqual(next, Random.Range(0, 1000000)); // 타일 배치가 게임 난수에 영향을 주지 않는지 확인
            var layers = root.GetComponentsInChildren<Tilemap>().ToDictionary(layer => layer.name); // 실제 그림과 충돌 레이어 조회
            CollectionAssert.AreEquivalent(new[] { "Floor", "Wall", "WallEdges", "Decoration" }, layers.Keys); // 네 가지 타일맵의 존재 확인
            var floors = ReadTiles(layers["Floor"]); // 바닥 좌표와 무늬 기록
            var walls = ReadTiles(layers["Wall"]); // 벽 좌표와 무늬 기록
            var edges = ReadTiles(layers["WallEdges"]); // 벽 직선과 모서리 좌표 기록
            var decor = ReadTiles(layers["Decoration"]); // 환경 장식 좌표 기록
            Assert.AreEqual(view.FloorTileCount, floors.Count); // 기존 논리 바닥과 실제 그림의 수 일치
            Assert.AreEqual(view.WallTileCount, walls.Count); // 기존 논리 벽과 실제 그림의 수 일치
            Assert.AreEqual(5, floors.Values.Distinct().Count()); // 기존 바닥 한 가지와 새 바닥 네 가지 확인
            Assert.AreEqual(5, walls.Values.Distinct().Count()); // 기존 벽 한 가지와 새 석재 네 가지 확인
            var originalFloor = layers["Floor"].GetTile<Tile>(floors.First(pair => pair.Value == "DungeonFloor").Key).sprite; // 원본 바닥의 실제 타일 재질 조회
            Assert.Less(originalFloor.rect.width, originalFloor.texture.width); // 검은 원화 여백이 실제 맵에 반복되지 않는지 확인
            Assert.GreaterOrEqual(edges.Values.Distinct().Count(), 8); // 직선과 여러 연결 모서리 확인
            CollectionAssert.AreEquivalent(walls.Keys, edges.Keys); // 테두리가 실제 벽 셀에만 존재하는지 확인
            Assert.IsTrue(decor.Keys.Any(floors.ContainsKey) && decor.Keys.Any(walls.ContainsKey)); // 벽과 바닥 모두에 장식 배치 확인
            Assert.IsFalse(floors.Keys.Any(walls.ContainsKey)); // 실제 출입로와 이동 칸에 새 벽이 생기는지 확인
            Assert.IsTrue(floors.Keys.All(cell => layers["Floor"].GetColliderType(cell) == Tile.ColliderType.None)); // 모든 바닥 무늬의 통행 가능 확인
            Assert.IsTrue(walls.Keys.All(cell => layers["Wall"].GetColliderType(cell) == Tile.ColliderType.Grid)); // 모든 벽 무늬의 기존 충돌 확인
            Assert.IsNotNull(layers["Wall"].GetComponent<CompositeCollider2D>()); // 기존 합성 벽 충돌 유지 확인
            Assert.IsNull(layers["WallEdges"].GetComponent<TilemapCollider2D>()); // 벽 테두리의 추가 충돌 없음 확인
            Assert.IsNull(layers["Decoration"].GetComponent<TilemapCollider2D>()); // 바닥 장식이 움직임을 방해하지 않는지 확인
            Assert.IsTrue(walls.Keys.All(cell => layers["Wall"].GetTransformMatrix(cell) == Matrix4x4.identity)); // 벽 충돌과 단위 타일 배율 유지 확인
            var detailTransform = Matrix4x4.Scale(new Vector3(2f, 2f, 1f)); // 바닥 격자와 분리된 장식 그림의 배율
            Assert.IsTrue(decor.Keys.All(cell => layers["Decoration"].GetTransformMatrix(cell) == detailTransform)); // 실제 벽과 바닥 장식의 두 배 확대 확인
            view.Build(ExplorationMapGenerator.Generate(14, seed, 1)); // 같은 층을 다시 생성해 동일 배치 비교
            CollectionAssert.AreEquivalent(floors, ReadTiles(layers["Floor"])); // 바닥 배치 재현
            CollectionAssert.AreEquivalent(walls, ReadTiles(layers["Wall"])); // 벽 배치 재현
            CollectionAssert.AreEquivalent(edges, ReadTiles(layers["WallEdges"])); // 연결 모서리 재현
            CollectionAssert.AreEquivalent(decor, ReadTiles(layers["Decoration"])); // 장식 배치 재현
            view.Build(ExplorationMapGenerator.Generate(14, seed + 1, 1)); // 다른 층 시드의 섞인 바닥 무늬 확인
            Assert.IsTrue(ReadTiles(layers["Floor"]).Any(pair => floors.TryGetValue(pair.Key, out string old) && old != pair.Value)); // 같은 좌표에서도 층이 바뀌면 다른 바닥 무늬 확인
            view.Build(null); // 사용하지 않는 층의 원화 초기화
            Assert.IsTrue(layers.Values.All(layer => layer.GetUsedTilesCount() == 0)); // 새 층에 이전 그림이 남지 않는지 확인
        } // 배치와 충돌 검사 종료
        finally // 테스트의 화면과 난수 복구
        { // 임시 자료 정리 시작
            Random.state = state; // 다른 게임 판정의 난수 복원
            Object.DestroyImmediate(root); // 검증에서 만든 실제 맵 객체 정리
        } // 임시 자료 정리 종료
    } // 같은 시드 배치 및 충돌 검사 종료

    private static Dictionary<Vector3Int, string> ReadTiles(Tilemap layer) // 실제 배치의 타일 이름과 좌표 수집
    { // 원화 조사 시작
        var result = new Dictionary<Vector3Int, string>(); // 비교할 타일 이름과 좌표
        foreach (Vector3Int cell in layer.cellBounds.allPositionsWithin) // 등록된 그리드 범위 조사
        { // 타일 확인 시작
            if (layer.GetTile(cell) is Tile tile) // 실제 그림이 있는 칸만 수집
            { // 원화 이름 기록 시작
                result.Add(cell, tile.sprite.name); // 좌표와 확정된 그림 이름 저장
            } // 원화 이름 기록 종료
        } // 타일 확인 종료
        return result; // 발견한 모든 그림 타일 반환
    } // 원화 조사 종료
} // 탐사 타일 검사 종료
