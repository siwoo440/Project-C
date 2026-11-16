using System.Collections.Generic; // 방문 좌표와 탐색 대기열 사용
using NUnit.Framework; // 맵 연결과 방 제한 검증 사용
using UnityEngine; // 격자 방향 좌표 사용
public sealed class ExplorationConnectivityTests // 절차 생성 맵의 실제 연결 검사
{ // 맵 연결 검사 시작
    [TestCase(1)] // 일반 층 검사 지정
    [TestCase(5)] // 중간 보스 층 검사 지정
    [TestCase(10)] // 마지막 보스 층 검사 지정
    public void GeneratedRooms_AreMutuallyConnectedAndReachable(int floor) // 다양한 시드의 전체 방 도달과 양방향 연결 확인
    { // 맵 도달 검사 시작
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }; // 방 연결 방향 순서
        for (int seed = -100; seed < 400; seed++) // 음수와 양수 시드 순회
        { // 개별 시드 검사 시작
            ExplorationMapData map = ExplorationMapGenerator.Generate(14, seed, floor); // 실제 크기의 시드별 맵 생성
            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { map.StartCoordinate }; // 시작 방 방문 기록 생성
            Queue<Vector2Int> pending = new Queue<Vector2Int>(); // 도달할 방 탐색 대기열 생성
            pending.Enqueue(map.StartCoordinate); // 시작 방을 탐색 대기열에 추가
            while (pending.Count > 0) // 방문 대기 방 순회
            { // 방문 방 탐색 시작
                Vector2Int coordinate = pending.Dequeue(); // 다음 방문 방 좌표 조회
                Assert.IsTrue(map.TryGetCell(coordinate, out ExplorationMapCell cell)); // 방문 좌표의 실제 방 확인
                bool[] connections = { cell.ConnectedUp, cell.ConnectedRight, cell.ConnectedDown, cell.ConnectedLeft }; // 실제 방의 네 방향 연결 조회
                for (int direction = 0; direction < directions.Length; direction++) // 네 방향 연결 순회
                { // 개별 연결 확인 시작
                    Vector2Int next = coordinate + directions[direction]; // 이웃 방 좌표 계산
                    bool exists = map.TryGetCell(next, out ExplorationMapCell neighbor); // 이웃 방의 실제 존재 확인
                    Assert.AreEqual(exists, connections[direction], $"층 {floor}, 시드 {seed}, 좌표 {coordinate}"); // 출입구와 이웃 방 일치 확인
                    if (!exists) // 이웃 방 없음 확인
                    { // 빈 방향 처리 시작
                        continue; // 다음 방향 확인
                    } // 빈 방향 처리 종료
                    bool[] opposite = { neighbor.ConnectedDown, neighbor.ConnectedLeft, neighbor.ConnectedUp, neighbor.ConnectedRight }; // 이웃 방의 반대쪽 출입구 조회
                    Assert.IsTrue(opposite[direction], $"층 {floor}, 시드 {seed}, 좌표 {next}"); // 출입구 양방향 연결 확인
                    if (visited.Add(next)) // 아직 방문하지 않은 방 확인
                    { // 새 이웃 방 처리 시작
                        pending.Enqueue(next); // 새 이웃 방 탐색 예약
                    } // 새 이웃 방 처리 종료
                } // 개별 연결 확인 종료
            } // 방문 방 탐색 종료
            Assert.AreEqual(map.Cells.Count, visited.Count, $"층 {floor}, 시드 {seed}"); // 전체 생성 방 도달 확인
            Assert.IsTrue(visited.Contains(map.StairsCoordinate), $"층 {floor}, 시드 {seed}"); // 관문 방 도달 확인
        } // 개별 시드 검사 종료
    } // 맵 도달 검사 종료
    [TestCase(2, 0, 0)] // 시작과 관문만 있는 최소 맵 지정
    [TestCase(3, 1, 0)] // 휴식 방만 추가 가능한 맵 지정
    [TestCase(4, 1, 1)] // 휴식과 상점이 가능한 최소 맵 지정
    [TestCase(14, 1, 1)] // 실제 기본 크기 맵 지정
    public void GeneratedRooms_RespectCapacityForGuaranteedSpecialRooms(int count, int expectedRest, int expectedShop) // 방 수에 따른 휴식·상점 배치 확인
    { // 특수 방 용량 검사 시작
        for (int seed = 0; seed < 200; seed++) // 다양한 시드 순회
        { // 개별 맵 방 역할 검사 시작
            ExplorationMapData map = ExplorationMapGenerator.Generate(count, seed, 1); // 지정 크기 맵 생성
            int rest = 0; // 휴식 방 개수 초기화
            int shop = 0; // 상점 방 개수 초기화
            foreach (ExplorationMapCell cell in map.Cells) // 생성된 전체 방 순회
            { // 특수 방 계산 시작
                rest += cell.RoomType == ExplorationRoomType.Rest ? 1 : 0; // 휴식 방 개수 누적
                shop += cell.RoomType == ExplorationRoomType.Shop ? 1 : 0; // 상점 방 개수 누적
            } // 특수 방 계산 종료
            Assert.AreEqual(expectedRest, rest, $"방 {count}, 시드 {seed}"); // 용량에 맞는 휴식 방 확인
            Assert.AreEqual(expectedShop, shop, $"방 {count}, 시드 {seed}"); // 용량에 맞는 상점 방 확인
        } // 개별 맵 방 역할 검사 종료
    } // 특수 방 용량 검사 종료
} // 맵 연결 검사 종료
