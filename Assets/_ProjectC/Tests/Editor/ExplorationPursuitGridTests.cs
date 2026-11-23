using System.Collections.Generic; // 바닥 좌표 집합 사용
using NUnit.Framework; // 편집 모드 검증 사용
using UnityEngine; // 격자 셀 좌표 사용

public sealed class ExplorationPursuitGridTests // 추적 격자의 탐지와 이동 검증
{ // 격자 검증 시작
    [Test] // 직선 시야 검증 등록
    public void HasLineOfSight_StopsAtWall() // 중간 벽의 직선 시야 차단 확인
    { // 직선 시야 검증 시작
        Vector3Int start = Vector3Int.zero; // 출발 셀
        Vector3Int middle = new Vector3Int(1, 0, 0); // 중간 셀
        Vector3Int end = new Vector3Int(2, 0, 0); // 도착 셀
        HashSet<Vector3Int> floor = new HashSet<Vector3Int> { start, end }; // 중간 셀이 비어 있는 바닥

        Assert.IsFalse(ExplorationPursuitGrid.HasLineOfSight(start, end, floor.Contains)); // 중간 벽의 시야 차단 확인
        floor.Add(middle); // 중간 벽을 바닥으로 변경
        Assert.IsTrue(ExplorationPursuitGrid.HasLineOfSight(start, end, floor.Contains)); // 벽 제거 후 시야 확보 확인
    } // 직선 시야 검증 종료

    [Test] // 같은 시야 위치의 재사용과 바닥 변경 검증
    public void CheckLineOfSight_ReusesOnlyTheSameWalkableRevision() // 직선 시야 반복 확인과 새 맵 시야 복원
    { // 시야 재사용 검증 시작
        var grid = new ExplorationPursuitGrid(); // 같은 적이 사용하는 추적 탐색기
        Vector3Int start = Vector3Int.zero; // 적이 서 있는 바닥 셀
        Vector3Int middle = new Vector3Int(1, 0, 0); // 벽으로 바뀔 가운데 셀
        Vector3Int end = new Vector3Int(2, 0, 0); // 플레이어가 서 있는 바닥 셀
        var floor = new HashSet<Vector3Int> { start, middle, end }; // 중간 벽이 없는 처음 통로
        int visited = 0; // 실제 바닥 조회 반복 횟수
        bool HasFloor(Vector3Int position) // 실제로 사용하는 바닥과 조회 수 확인
        { // 바닥 판정 시작
            visited++; // 현재 시야가 조회한 바닥 수 증가
            return floor.Contains(position); // 현재 층의 유효 바닥 여부 반환
        } // 바닥 판정 종료
        Assert.IsTrue(grid.CheckLineOfSight(start, end, 1, HasFloor)); // 최초에 실제 열린 시야 확인
        Assert.Greater(visited, 0); // 처음 조회가 원래 바닥 규칙을 검사했는지 확인
        visited = 0; // 다음 물리 틱을 위한 판정 수 초기화
        Assert.IsTrue(grid.CheckLineOfSight(start, end, 1, HasFloor)); // 같은 셀과 같은 층의 시야 확인
        Assert.AreEqual(0, visited); // 이전에 처리한 직선 시야의 반복 탐색 생략 확인
        floor.Remove(middle); // 동일 좌표에 벽이 생긴 새 층 재현
        Assert.IsFalse(grid.CheckLineOfSight(start, end, 2, HasFloor)); // 새 바닥 버전에서 이전 시야 재사용 금지 확인
        Assert.Greater(visited, 0); // 변경한 맵의 실제 바닥 판정 호출 확인
    } // 시야 재사용 검증 종료

    [Test] // 대각선 시야 검증 등록
    public void HasLineOfSight_ChecksBothSidesOfAnExactCorner() // 모서리 양옆의 벽 차단 확인
    { // 모서리 시야 검증 시작
        Vector3Int start = Vector3Int.zero; // 출발 셀
        Vector3Int end = new Vector3Int(1, 1, 0); // 대각선 도착 셀
        Vector3Int horizontal = new Vector3Int(1, 0, 0); // 모서리 가로 셀
        Vector3Int vertical = new Vector3Int(0, 1, 0); // 모서리 세로 셀
        HashSet<Vector3Int> floor = new HashSet<Vector3Int> { start, end, horizontal }; // 한쪽 벽이 남은 바닥

        Assert.IsFalse(ExplorationPursuitGrid.HasLineOfSight(start, end, floor.Contains)); // 세로 쪽 벽으로 인한 정방향 차단 확인
        Assert.IsFalse(ExplorationPursuitGrid.HasLineOfSight(end, start, floor.Contains)); // 같은 벽으로 인한 역방향 차단 확인
        floor.Add(vertical); // 세로 쪽 벽 제거
        Assert.IsTrue(ExplorationPursuitGrid.HasLineOfSight(start, end, floor.Contains)); // 양옆 개방 후 대각선 시야 확보 확인
        floor.Remove(horizontal); // 가로 쪽 벽 추가
        Assert.IsFalse(ExplorationPursuitGrid.HasLineOfSight(start, end, floor.Contains)); // 가로 쪽 벽으로 인한 차단 확인
    } // 모서리 시야 검증 종료

    [Test] // 기울어진 시야 검증 등록
    public void HasLineOfSight_ChecksEachCrossedCellOfAnObliqueRay() // 기울어진 시야의 통과 셀 검사 확인
    { // 통과 셀 검증 시작
        Vector3Int start = Vector3Int.zero; // 출발 셀
        Vector3Int end = new Vector3Int(3, 1, 0); // 기울어진 시야의 도착 셀
        Vector3Int crossed = new Vector3Int(2, 0, 0); // 시선이 모서리에서 닿는 셀
        HashSet<Vector3Int> floor = new HashSet<Vector3Int> // 시선 주변 바닥 좌표
        { // 바닥 집합 초기화 시작
            start, // 출발 바닥
            new Vector3Int(1, 0, 0), // 모서리 앞 바닥
            new Vector3Int(1, 1, 0), // 모서리 세로 바닥
            new Vector3Int(2, 1, 0), // 모서리 뒤 바닥
            end // 도착 바닥
        }; // 바닥 집합 초기화 종료

        Assert.IsFalse(ExplorationPursuitGrid.HasLineOfSight(start, end, floor.Contains)); // 비어 있는 가로 셀 차단 확인
        floor.Add(crossed); // 빠진 가로 셀을 바닥으로 변경
        Assert.IsTrue(ExplorationPursuitGrid.HasLineOfSight(start, end, floor.Contains)); // 정방향 시야 확보 확인
        Assert.IsTrue(ExplorationPursuitGrid.HasLineOfSight(end, start, floor.Contains)); // 역방향 시야 확보 확인
    } // 통과 셀 검증 종료

    [Test] // 최단 우회 검증 등록
    public void TryNextStep_FollowsAShortestRouteAroundTheWallDeterministically() // 벽 주변 최단 이동과 동일 경로 재현 확인
    { // 최단 우회 검증 시작
        ExplorationPursuitGrid grid = new ExplorationPursuitGrid(); // 재사용할 추적 탐색기
        Vector3Int spawn = Vector3Int.zero; // 추적 시작 셀
        Vector3Int goal = new Vector3Int(4, 0, 0); // 벽 너머 목표 셀
        Vector3Int wall = new Vector3Int(2, 0, 0); // 경로 가운데 벽 셀
        Vector3Int current = spawn; // 현재 위치 초기값
        Vector3Int first = new Vector3Int(1, 1, 0); // 같은 길이의 우회 경로 중 첫 이동 예상값

        for (int index = 0; index < 4; index++) // 목표까지 최단 이동 횟수만큼 반복
        { // 한 칸 이동 검증 시작
            Assert.IsTrue(grid.TryNextStep(current, goal, spawn, 4, cell => cell != wall, out Vector3Int step)); // 다음 이동 셀 조회 성공 확인
            Assert.AreNotEqual(wall, step); // 벽 셀 진입 방지 확인
            Assert.LessOrEqual(System.Math.Abs(step.x - current.x), 1); // 가로 이동 한 칸 제한 확인
            Assert.LessOrEqual(System.Math.Abs(step.y - current.y), 1); // 세로 이동 한 칸 제한 확인
            current = step; // 다음 탐색의 현재 위치 갱신
        } // 한 칸 이동 검증 종료

        Assert.AreEqual(goal, current); // 네 칸 이동 후 목표 도착 확인
        Assert.IsFalse(grid.TryNextStep(goal, goal, spawn, 4, cell => cell != wall, out _)); // 이미 도착한 경우 이동 없음 확인
        Assert.IsTrue(grid.TryNextStep(spawn, goal, spawn, 4, cell => cell != wall, out Vector3Int repeated)); // 같은 시작점으로 다시 탐색
        Assert.AreEqual(first, repeated); // 고정된 탐색 순서의 첫 이동 확인
    } // 최단 우회 검증 종료

    [Test] // 추적 범위 검증 등록
    public void TryNextStep_RejectsDetoursThatLeaveTheSpawnLeash() // 범위를 벗어나는 우회 경로 차단 확인
    { // 추적 범위 검증 시작
        ExplorationPursuitGrid grid = new ExplorationPursuitGrid(); // 재사용할 추적 탐색기
        Vector3Int spawn = new Vector3Int(-1, 0, 0); // 추적 범위의 기준 셀
        Vector3Int goal = new Vector3Int(1, 0, 0); // 벽 반대편의 범위 내 목표 셀

        bool IsFloor(Vector3Int cell) // 높이 3에서만 양옆이 이어지는 바닥 판정
        { // 테스트 바닥 확인 시작
            return cell.x >= -1 && cell.x <= 1 && cell.y >= 0 && cell.y <= 3 && (cell.x != 0 || cell.y == 3); // 바닥 영역과 중앙 벽 판정
        } // 테스트 바닥 확인 종료

        Assert.IsFalse(grid.TryNextStep(spawn, goal, spawn, 2, IsFloor, out _)); // 유일한 우회가 범위를 벗어나므로 이동 차단 확인
        Assert.IsTrue(grid.TryNextStep(spawn, goal, spawn, 3, IsFloor, out Vector3Int step)); // 같은 우회로가 넓어진 범위 안에서 연결됨을 확인
        Assert.AreEqual(new Vector3Int(-1, 1, 0), step); // 우회 경로 진입 방향 확인
        Assert.IsFalse(grid.TryNextStep(spawn, goal, spawn, 1, IsFloor, out _)); // 범위 밖 목표 추적 차단 확인
    } // 추적 범위 검증 종료

    [Test] // 대각선 이동 검증 등록
    public void TryNextStep_UsesCardinalDetourInsteadOfCuttingACorner() // 모서리에서 대각선 관통 방지 확인
    { // 모서리 이동 검증 시작
        ExplorationPursuitGrid grid = new ExplorationPursuitGrid(); // 재사용할 추적 탐색기
        Vector3Int start = Vector3Int.zero; // 출발 셀
        Vector3Int goal = new Vector3Int(1, 1, 0); // 대각선 목표 셀
        Vector3Int detour = new Vector3Int(1, 0, 0); // 가로 우회 셀
        HashSet<Vector3Int> floor = new HashSet<Vector3Int> { start, goal, detour }; // 한쪽 모서리가 막힌 바닥

        Assert.IsTrue(grid.TryNextStep(start, goal, start, 1, floor.Contains, out Vector3Int step)); // 가로 우회 경로 조회 확인
        Assert.AreEqual(detour, step); // 한쪽 벽이 막은 대각선 대신 가로 이동 확인
        floor.Remove(detour); // 유일한 우회 셀을 벽으로 변경
        Assert.IsFalse(grid.TryNextStep(start, goal, start, 1, floor.Contains, out _)); // 막힌 양쪽 구석 사이의 이동 차단 확인
        floor.Remove(goal); // 목표 셀을 벽으로 변경
        Assert.IsFalse(grid.TryNextStep(start, goal, start, 1, floor.Contains, out _)); // 바닥이 아닌 목표 이동 차단 확인
    } // 모서리 이동 검증 종료

    [TestCase(0, 1)] // 위 이동 검증 등록
    [TestCase(1, 1)] // 오른쪽 위 이동 검증 등록
    [TestCase(1, 0)] // 오른쪽 이동 검증 등록
    [TestCase(1, -1)] // 오른쪽 아래 이동 검증 등록
    [TestCase(0, -1)] // 아래 이동 검증 등록
    [TestCase(-1, -1)] // 왼쪽 아래 이동 검증 등록
    [TestCase(-1, 0)] // 왼쪽 이동 검증 등록
    [TestCase(-1, 1)] // 왼쪽 위 이동 검증 등록
    public void TryNextStep_AllowsEachClearNeighborWithinTheLeash(int x, int y) // 바닥이 열린 여덟 방향 직접 이동 확인
    { // 여덟 방향 검증 시작
        ExplorationPursuitGrid grid = new ExplorationPursuitGrid(); // 추적 탐색기 생성
        Vector3Int spawn = new Vector3Int(3, -2, 2); // 원점과 다른 층의 기준 셀
        Vector3Int goal = new Vector3Int(spawn.x + x, spawn.y + y, spawn.z); // 이웃 목표 셀

        Assert.IsTrue(grid.TryNextStep(spawn, goal, spawn, 1, cell => cell.z == spawn.z, out Vector3Int step)); // 추적 범위 경계까지 이동 확인
        Assert.AreEqual(goal, step); // 방향별 이웃 목표에 한 칸으로 도착 확인
    } // 여덟 방향 검증 종료
} // 격자 검증 종료
